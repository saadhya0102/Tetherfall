using System;
using UnityEngine;

namespace Tetherfall
{
    /// <summary>
    /// A hostile can combine a base silhouette with the elite modifier.
    /// </summary>
    [Flags]
    public enum TetherfallHostileArchetype
    {
        None = 0,
        Creature = 1 << 0,
        WeaponWielder = 1 << 1,
        Flyer = 1 << 2,
        Caster = 1 << 3,
        Elite = 1 << 4
    }

    public enum TetherfallHostileActionPhase
    {
        Idle,
        Anticipation,
        Release,
        Hit,
        Death
    }

    /// <summary>
    /// Allocation-free procedural motion for a generated hostile silhouette.
    /// Named parts are classified and captured once during Configure/RebindNamedParts.
    /// The gameplay root and its collider are never modified.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class TetherfallHostileAnimation : MonoBehaviour
    {
        private const float Tau = Mathf.PI * 2f;
        private static readonly Color RangedTelegraphColor = new Color(0.48f, 0.86f, 0.92f);
        private static readonly Color PhysicalTelegraphColor = new Color(0.96f, 0.54f, 0.3f);

        private enum PartRole : byte
        {
            Accent,
            Core,
            Head,
            Limb,
            Wing,
            Weapon,
            Shield,
            Focus,
            Orbit,
            Tail
        }

        private struct PartPose
        {
            public Transform target;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public PartRole role;
            public float side;
            public float pairPhase;
            public float actionWeight;
            public float phase;

            public void Restore()
            {
                if (target == null)
                {
                    return;
                }

                target.localPosition = position;
                target.localRotation = rotation;
                target.localScale = scale;
            }
        }

        private Enemy enemy;
        private Transform bindingRoot;
        private Transform gameplayRoot;
        private Transform bodyVisual;
        private Material bodySignalMaterial;
        private PartPose[] parts;
        private int partCount;

        private TetherfallHostileArchetype archetype;
        private EnemyAttackStyle attackStyle;
        private EnemySpecialMove specialMove;
        private float definitionScale = 1f;
        private float expectedMoveSpeed = 2f;
        private float expectedTelegraphSeconds = 0.45f;
        private Vector3 bodyReferenceScale = Vector3.one;

        private Vector3 rootPosition;
        private Quaternion rootRotation;
        private Vector3 rootScale;
        private bool rootPoseCaptured;
        private bool configured;

        private Vector3 headPivot;
        private Vector3 weaponPivot;
        private Vector3 shieldPivot;
        private Vector3 focusPivot;
        private Vector3 orbitPivot;
        private float weaponSide = 1f;

        private Vector3 previousWorldPosition;
        private Vector3 previousForward;
        private float forwardMotion;
        private float strafeMotion;
        private float turnMotion;
        private float gaitPhase;
        private float orbitAngle;
        private float lifeTime;
        private float idlePhase;
        private bool motionBurst;

        private int lastHealth;
        private bool lastStaggered;
        private bool lastDefeated;
        private bool lastAttackSignal;
        private bool lastTelegraphColorSignal;
        private float attackInferenceCooldown;

        private TetherfallHostileActionPhase actionPhase;
        private float actionElapsed;
        private float actionDuration;
        private float hitStrength = 1f;
        private float recoilSide = 1f;

        private float currentAttackPose;
        private float currentAttackCharge;
        private float currentRelease;
        private float currentHit;
        private float currentDeath;

        public Enemy Enemy => enemy;
        public Transform BindingRoot => bindingRoot;
        public TetherfallHostileArchetype Archetype => archetype;
        public TetherfallHostileActionPhase ActionPhase => actionPhase;
        public int BoundPartCount => partCount;
        public bool IsBound => configured && bindingRoot != null && partCount > 0;

        public static TetherfallHostileAnimation Attach(
            Enemy hostile,
            Transform visualRoot,
            EnemyDefinition definition)
        {
            if (hostile == null || visualRoot == null || definition == null)
            {
                return null;
            }

            var animation = visualRoot.GetComponent<TetherfallHostileAnimation>();
            if (animation == null)
            {
                animation = visualRoot.gameObject.AddComponent<TetherfallHostileAnimation>();
            }

            animation.Configure(hostile, visualRoot, definition);
            return animation;
        }

        /// <summary>
        /// Starts and holds a readable wind-up until release is observed or notified.
        /// Enemy calls this when its gameplay telegraph begins.
        /// </summary>
        public void NotifyAttackAnticipation(float anticipationSeconds = 0f)
        {
            if (actionPhase == TetherfallHostileActionPhase.Death)
            {
                return;
            }

            actionPhase = TetherfallHostileActionPhase.Anticipation;
            actionElapsed = 0f;
            actionDuration = anticipationSeconds > 0f
                ? Mathf.Max(0.05f, anticipationSeconds)
                : expectedTelegraphSeconds;
        }

        /// <summary>
        /// Starts the strike/release and follow-through.
        /// Enemy calls this when its gameplay strike resolves.
        /// </summary>
        public void NotifyAttackRelease(float followThroughSeconds = 0.34f)
        {
            if (actionPhase == TetherfallHostileActionPhase.Death)
            {
                return;
            }

            actionPhase = TetherfallHostileActionPhase.Release;
            actionElapsed = 0f;
            actionDuration = Mathf.Max(0.12f, followThroughSeconds);
            attackInferenceCooldown = 0.2f;
        }

        /// <summary>
        /// Adds recoil and stagger without moving the gameplay root.
        /// Enemy calls this after damage and telegraph interruption are resolved.
        /// </summary>
        public void NotifyHit(float severity = 1f)
        {
            if (actionPhase == TetherfallHostileActionPhase.Death)
            {
                return;
            }

            actionPhase = TetherfallHostileActionPhase.Hit;
            actionElapsed = 0f;
            actionDuration = Mathf.Lerp(0.24f, 0.4f, Mathf.Clamp01(severity));
            hitStrength = Mathf.Clamp(severity, 0.35f, 1.5f);
            recoilSide = -recoilSide;
            lastAttackSignal = false;
            lastTelegraphColorSignal = false;
            attackInferenceCooldown = 0.22f;
        }

        /// <summary>
        /// Starts the visual wind-down during Enemy's short collider-free delayed destroy.
        /// </summary>
        public void NotifyDeath(float windDownSeconds = 0.8f)
        {
            if (actionPhase == TetherfallHostileActionPhase.Death)
            {
                return;
            }

            actionPhase = TetherfallHostileActionPhase.Death;
            actionElapsed = 0f;
            actionDuration = Mathf.Max(0.15f, windDownSeconds);
            currentAttackPose = 0f;
            currentAttackCharge = 0f;
            currentRelease = 0f;
        }

        public void CancelAction()
        {
            if (actionPhase == TetherfallHostileActionPhase.Death)
            {
                return;
            }

            actionPhase = TetherfallHostileActionPhase.Idle;
            actionElapsed = 0f;
            currentAttackPose = 0f;
            currentAttackCharge = 0f;
            currentRelease = 0f;
            currentHit = 0f;
        }

        /// <summary>
        /// Restores every captured local pose. This is also called automatically on disable.
        /// </summary>
        public void ResetPose()
        {
            if (rootPoseCaptured && bindingRoot != null)
            {
                bindingRoot.localPosition = rootPosition;
                bindingRoot.localRotation = rootRotation;
                bindingRoot.localScale = rootScale;
            }

            for (var index = 0; index < partCount; index++)
            {
                parts[index].Restore();
            }
        }

        /// <summary>
        /// Re-captures named art after an intentional hierarchy rebuild. Never call per frame.
        /// </summary>
        public void RebindNamedParts()
        {
            if (bindingRoot == null)
            {
                return;
            }

            ResetPose();
            CaptureRootPose();
            BindParts();
            BindGameplaySignals();
            ResetMotionSample();
        }

        private void Configure(Enemy hostile, Transform visualRoot, EnemyDefinition definition)
        {
            ResetPose();

            enemy = hostile;
            bindingRoot = visualRoot;
            gameplayRoot = hostile.transform;
            attackStyle = definition.attackStyle;
            specialMove = definition.specialMove;
            definitionScale = Mathf.Max(0.1f, definition.scale);
            expectedMoveSpeed = Mathf.Max(0.1f, definition.moveSpeed);
            expectedTelegraphSeconds = Mathf.Max(0.05f, definition.telegraphSeconds);
            archetype = ClassifyArchetype(definition.id, definition.attackStyle);
            idlePhase = Mathf.Repeat(
                hostile.transform.position.x * 0.137f +
                hostile.transform.position.z * 0.173f +
                definitionScale,
                Tau);

            CaptureRootPose();
            BindParts();
            BindGameplaySignals();

            lastHealth = hostile.Health;
            lastStaggered = hostile.IsStaggered;
            lastDefeated = hostile.IsDefeated;
            lastAttackSignal = false;
            lastTelegraphColorSignal = false;
            actionPhase = hostile.IsDefeated
                ? TetherfallHostileActionPhase.Death
                : TetherfallHostileActionPhase.Idle;
            actionElapsed = 0f;
            actionDuration = 0f;
            currentAttackPose = 0f;
            currentAttackCharge = 0f;
            currentRelease = 0f;
            currentHit = 0f;
            currentDeath = hostile.IsDefeated ? 1f : 0f;
            attackInferenceCooldown = 0f;
            configured = true;
            ResetMotionSample();
        }

        private void OnEnable()
        {
            if (configured)
            {
                ResetMotionSample();
            }
        }

        private void OnDisable()
        {
            ResetPose();
        }

        private void Update()
        {
            if (!configured || enemy == null || bindingRoot == null || gameplayRoot == null)
            {
                return;
            }

            var game = TetherfallGame.Instance;
            if (game != null && game.GameplayPaused)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            lifeTime += deltaTime;
            attackInferenceCooldown = Mathf.Max(0f, attackInferenceCooldown - deltaTime);
            ObserveMovement(deltaTime);
            ObserveCombatState();
            AdvanceAction(deltaTime);

            var movement = Mathf.Clamp01(Mathf.Sqrt(
                forwardMotion * forwardMotion + strafeMotion * strafeMotion));
            var cadence = Is(TetherfallHostileArchetype.Flyer)
                ? Mathf.Lerp(5.5f, 12.5f, movement)
                : Is(TetherfallHostileArchetype.Creature)
                    ? Mathf.Lerp(2.2f, 12.8f, movement)
                    : Mathf.Lerp(1.6f, 9.8f, movement);
            gaitPhase = Mathf.Repeat(gaitPhase + deltaTime * cadence, Tau);
            orbitAngle = Mathf.Repeat(
                orbitAngle + deltaTime * (Is(TetherfallHostileArchetype.Flyer) ? 76f : 34f),
                360f);

            var stride = Mathf.Sin(gaitPhase);
            var step = Mathf.Abs(Mathf.Cos(gaitPhase));
            var breathRate = Is(TetherfallHostileArchetype.Elite) ? 1.35f : 1.8f;
            var breath = Mathf.Sin(lifeTime * breathRate + idlePhase);

            ApplyRootMotion(movement, stride, step, breath);
            ApplyPartMotion(movement, stride, step, breath);
        }

        private void ObserveMovement(float deltaTime)
        {
            var currentPosition = gameplayRoot.position;
            var displacement = currentPosition - previousWorldPosition;
            displacement.y = 0f;
            var distance = displacement.magnitude;
            motionBurst = distance > Mathf.Max(0.32f, expectedMoveSpeed * deltaTime * 4.5f) &&
                          distance <= 3.1f;

            var targetForward = 0f;
            var targetStrafe = 0f;
            if (distance <= 3.1f && distance > 0.00001f)
            {
                var localVelocity = gameplayRoot.InverseTransformDirection(displacement / deltaTime);
                targetForward = Mathf.Clamp(localVelocity.z / expectedMoveSpeed, -1f, 1f);
                targetStrafe = Mathf.Clamp(localVelocity.x / expectedMoveSpeed, -1f, 1f);
            }

            var currentForward = gameplayRoot.forward;
            var turnAngle = Vector3.Angle(previousForward, currentForward);
            var turnSign = Mathf.Sign(Vector3.Cross(previousForward, currentForward).y);
            var targetTurn = Mathf.Clamp(turnAngle * turnSign / (180f * deltaTime), -1f, 1f);
            var blend = 1f - Mathf.Exp(-11f * deltaTime);
            forwardMotion = Mathf.Lerp(forwardMotion, targetForward, blend);
            strafeMotion = Mathf.Lerp(strafeMotion, targetStrafe, blend);
            turnMotion = Mathf.Lerp(turnMotion, targetTurn, blend);

            previousWorldPosition = currentPosition;
            previousForward = currentForward;
        }

        private void ObserveCombatState()
        {
            var health = enemy.Health;
            var defeated = enemy.IsDefeated;
            var staggered = enemy.IsStaggered;
            var tookDamage = health < lastHealth;

            if (defeated && !lastDefeated)
            {
                NotifyDeath();
            }
            else if (tookDamage)
            {
                var loss = Mathf.Max(1, lastHealth - health);
                var severity = 0.45f + loss / (float)Mathf.Max(1, enemy.MaxHealth) * 3.2f;
                NotifyHit(severity);
            }
            else if (staggered && !lastStaggered)
            {
                NotifyHit(0.62f);
            }

            var canInferAttack =
                actionPhase == TetherfallHostileActionPhase.Idle ||
                actionPhase == TetherfallHostileActionPhase.Anticipation;
            var colorSignal = false;
            var attackSignal = canInferAttack &&
                               !staggered &&
                               ReadAttackSignal(out colorSignal);

            if (canInferAttack)
            {
                if (motionBurst &&
                    (actionPhase == TetherfallHostileActionPhase.Anticipation ||
                     lastAttackSignal))
                {
                    NotifyAttackRelease();
                    attackSignal = false;
                }
                else if (attackSignal && !lastAttackSignal)
                {
                    NotifyAttackAnticipation(expectedTelegraphSeconds);
                }
                else if (!attackSignal && lastAttackSignal)
                {
                    NotifyAttackRelease();
                }
                else if (lastTelegraphColorSignal && !colorSignal &&
                         actionPhase == TetherfallHostileActionPhase.Anticipation)
                {
                    NotifyAttackRelease();
                    attackSignal = false;
                }
            }

            lastHealth = health;
            lastStaggered = staggered;
            lastDefeated = defeated;
            lastAttackSignal = attackSignal;
            lastTelegraphColorSignal = colorSignal;
        }

        private bool ReadAttackSignal(out bool colorSignal)
        {
            colorSignal = false;
            if (bodySignalMaterial != null)
            {
                var expectedColor = attackStyle == EnemyAttackStyle.Ranged
                    ? RangedTelegraphColor
                    : PhysicalTelegraphColor;
                colorSignal = ColorDistanceSquared(bodySignalMaterial.color, expectedColor) < 0.006f;
            }

            var scaleSignal = false;
            if (bodyVisual != null)
            {
                var currentScale = bodyVisual.localScale;
                var scaleRatio =
                    SafeRatio(currentScale.x, bodyReferenceScale.x) +
                    SafeRatio(currentScale.y, bodyReferenceScale.y) +
                    SafeRatio(currentScale.z, bodyReferenceScale.z);
                scaleSignal = scaleRatio / 3f > 1.055f;
            }

            if (lastTelegraphColorSignal && !colorSignal)
            {
                return false;
            }

            return colorSignal || (attackInferenceCooldown <= 0f && scaleSignal);
        }

        private void AdvanceAction(float deltaTime)
        {
            currentAttackPose = 0f;
            currentAttackCharge = 0f;
            currentRelease = 0f;
            currentHit = 0f;

            switch (actionPhase)
            {
                case TetherfallHostileActionPhase.Anticipation:
                {
                    actionElapsed += deltaTime;
                    var progress = Smooth01(actionElapsed / Mathf.Max(0.05f, actionDuration));
                    currentAttackCharge = progress;
                    currentAttackPose = -progress;
                    break;
                }

                case TetherfallHostileActionPhase.Release:
                {
                    actionElapsed += deltaTime;
                    var progress = Mathf.Clamp01(actionElapsed / Mathf.Max(0.12f, actionDuration));
                    if (progress < 0.22f)
                    {
                        var strike = Smooth01(progress / 0.22f);
                        currentAttackPose = Mathf.Lerp(-1f, 1.28f, strike);
                    }
                    else
                    {
                        var recovery = Smooth01((progress - 0.22f) / 0.78f);
                        currentAttackPose = Mathf.Lerp(1.28f, 0f, recovery);
                    }

                    currentAttackCharge = 1f - Smooth01(progress);
                    currentRelease = Mathf.Sin(progress * Mathf.PI);
                    if (specialMove == EnemySpecialMove.TripleShot)
                    {
                        currentRelease = Mathf.Max(
                            currentRelease,
                            Mathf.Abs(Mathf.Sin(progress * Mathf.PI * 3f)) * (1f - progress));
                    }

                    if (progress >= 1f)
                    {
                        actionPhase = TetherfallHostileActionPhase.Idle;
                        currentAttackPose = 0f;
                        currentAttackCharge = 0f;
                        currentRelease = 0f;
                    }

                    break;
                }

                case TetherfallHostileActionPhase.Hit:
                {
                    actionElapsed += deltaTime;
                    var progress = Mathf.Clamp01(actionElapsed / Mathf.Max(0.1f, actionDuration));
                    currentHit = progress < 0.18f
                        ? Smooth01(progress / 0.18f)
                        : 1f - Smooth01((progress - 0.18f) / 0.82f);
                    currentHit *= hitStrength;
                    if (progress >= 1f)
                    {
                        actionPhase = TetherfallHostileActionPhase.Idle;
                        currentHit = 0f;
                    }

                    break;
                }

                case TetherfallHostileActionPhase.Death:
                    actionElapsed += deltaTime;
                    currentDeath = Smooth01(
                        actionElapsed / Mathf.Max(0.15f, actionDuration));
                    break;

                default:
                    currentDeath = 0f;
                    break;
            }
        }

        private void ApplyRootMotion(
            float movement,
            float stride,
            float step,
            float breath)
        {
            var elite = Is(TetherfallHostileArchetype.Elite) ? 1.22f : 1f;
            var creature = Is(TetherfallHostileArchetype.Creature);
            var flyer = Is(TetherfallHostileArchetype.Flyer);
            var caster = Is(TetherfallHostileArchetype.Caster);
            var stagger = enemy.IsStaggered ? 1f : 0f;
            var staggerWave = Mathf.Sin(lifeTime * 24f + idlePhase) * stagger;

            var bob = flyer
                ? Mathf.Sin(lifeTime * 2.7f + idlePhase) * 0.065f * definitionScale
                : step * movement * (creature ? 0.045f : 0.032f) * definitionScale;
            if (caster)
            {
                bob += Mathf.Sin(lifeTime * 2.1f + idlePhase) * 0.022f * definitionScale;
            }

            var attackTravel = specialMove == EnemySpecialMove.QuickLunge ? 0.18f : 0.085f;
            var shockwaveCrouch = specialMove == EnemySpecialMove.Shockwave
                ? currentAttackCharge * 0.1f
                : 0f;
            var positionOffset = new Vector3(
                -currentHit * recoilSide * 0.075f + strafeMotion * movement * 0.012f,
                bob - shockwaveCrouch * definitionScale - currentDeath * 0.42f * definitionScale,
                currentAttackPose * attackTravel * definitionScale -
                currentHit * 0.12f * definitionScale);

            var pitch =
                -forwardMotion * movement * (creature ? 5.5f : 4f) -
                currentAttackPose * (creature ? 9f : 5.5f) -
                currentHit * 17f;
            var yaw =
                -stride * movement * (creature ? 2.8f : 1.8f) +
                currentAttackPose * weaponSide * 3.5f +
                staggerWave * 4f;
            var roll =
                -strafeMotion * movement * 5f -
                turnMotion * (flyer ? 12f : 4f) +
                currentHit * recoilSide * 12f +
                staggerWave * 3f +
                currentDeath * recoilSide * 72f;

            var breathe = breath * 0.009f * elite;
            var width = 1f + breathe;
            var height = 1f + breathe * 0.75f;
            var depth = 1f + breathe;
            if (specialMove == EnemySpecialMove.Shockwave)
            {
                width += currentAttackCharge * 0.045f + currentRelease * 0.035f;
                depth += currentAttackCharge * 0.045f + currentRelease * 0.035f;
                height -= currentAttackCharge * 0.055f + currentRelease * 0.025f;
            }
            else if (caster || flyer)
            {
                width += currentAttackCharge * 0.018f;
                height += currentAttackCharge * 0.035f;
                depth += currentAttackCharge * 0.018f;
            }

            width += currentHit * 0.035f;
            height -= currentHit * 0.055f + currentDeath * 0.16f;
            depth += currentHit * 0.025f + currentDeath * 0.045f;

            bindingRoot.localPosition = rootPosition + positionOffset;
            bindingRoot.localRotation = rootRotation * Quaternion.Euler(pitch, yaw, roll);
            bindingRoot.localScale = Vector3.Scale(
                rootScale,
                new Vector3(width, Mathf.Max(0.72f, height), depth));
        }

        private void ApplyPartMotion(
            float movement,
            float stride,
            float step,
            float breath)
        {
            var creature = Is(TetherfallHostileArchetype.Creature);
            var flyer = Is(TetherfallHostileArchetype.Flyer);
            var caster = Is(TetherfallHostileArchetype.Caster);
            var elite = Is(TetherfallHostileArchetype.Elite);
            var staggerWave = enemy.IsStaggered
                ? Mathf.Sin(lifeTime * 26f + idlePhase)
                : 0f;

            var weaponPower = attackStyle == EnemyAttackStyle.Heavy ? 58f : 72f;
            if (caster)
            {
                weaponPower = specialMove == EnemySpecialMove.GroundBurst ? 54f : 38f;
            }

            var weaponMotion = Quaternion.Euler(
                stride * movement * 5f + currentAttackPose * weaponPower,
                currentAttackPose * weaponSide * (caster ? 9f : 24f),
                currentAttackPose * weaponSide * (caster ? 12f : 20f));
            var shieldMotion = Quaternion.Euler(
                -currentAttackPose * 8f,
                -currentAttackCharge * weaponSide * 12f,
                currentAttackPose * weaponSide * 5f);
            var headPitch = breath * 0.8f - currentHit * 10f;
            headPitch += creature
                ? -currentAttackPose * 21f
                : -currentAttackPose * (caster ? 11f : 7f);
            var headMotion = Quaternion.Euler(
                headPitch,
                -turnMotion * 4f + staggerWave * 3f,
                staggerWave * 2.5f + currentHit * recoilSide * 5f);
            var orbitMotion = Quaternion.AngleAxis(orbitAngle, Vector3.up);

            for (var index = 0; index < partCount; index++)
            {
                var part = parts[index];
                if (part.target == null)
                {
                    continue;
                }

                var position = part.position;
                var rotation = part.rotation;
                var scale = part.scale;

                switch (part.role)
                {
                    case PartRole.Core:
                    {
                        var corePulse = breath * (elite ? 0.018f : 0.012f);
                        var actionPulse = currentAttackCharge * (caster ? 0.055f : 0.02f);
                        scale = Vector3.Scale(
                            scale,
                            new Vector3(
                                1f + corePulse + actionPulse,
                                1f + corePulse * 0.65f - currentAttackCharge * 0.018f,
                                1f + corePulse + actionPulse));
                        rotation = Quaternion.Euler(
                            breath * 0.45f,
                            stride * movement * (creature ? 3.2f : 2f),
                            -strafeMotion * movement * 2f) * rotation;
                        break;
                    }

                    case PartRole.Head:
                    {
                        position = headPivot + headMotion * (position - headPivot);
                        rotation = headMotion * rotation;
                        if (part.actionWeight > 0.5f)
                        {
                            var jawOpen =
                                currentAttackCharge * 7f +
                                currentRelease * (creature ? 18f : 8f);
                            rotation = Quaternion.Euler(jawOpen, 0f, 0f) * rotation;
                        }

                        break;
                    }

                    case PartRole.Limb:
                    {
                        var limbSwing =
                            stride * movement * part.side * part.pairPhase *
                            (creature ? 24f : 18f);
                        if (creature && part.pairPhase > 0f)
                        {
                            limbSwing += currentAttackPose * 20f;
                        }
                        else if (!creature)
                        {
                            limbSwing += currentAttackPose *
                                         (part.side == weaponSide ? 33f : 9f);
                        }

                        limbSwing += staggerWave * part.side * 5f;
                        rotation = Quaternion.Euler(
                            limbSwing,
                            0f,
                            part.side * step * movement * 3.5f) * rotation;
                        position.y += step * movement * 0.012f * definitionScale;
                        break;
                    }

                    case PartRole.Wing:
                    {
                        var flap = flyer
                            ? Mathf.Sin(gaitPhase * 1.35f + idlePhase) *
                              Mathf.Lerp(11f, 34f, movement)
                            : Mathf.Sin(lifeTime * 2.2f + idlePhase) *
                              Mathf.Lerp(2f, 7f, movement);
                        var actionFold =
                            currentAttackCharge * 22f -
                            currentRelease * 18f;
                        rotation = Quaternion.Euler(
                            -movement * 4f,
                            currentAttackPose * part.side * 7f,
                            part.side * (flap + actionFold)) * rotation;
                        break;
                    }

                    case PartRole.Weapon:
                    {
                        position = weaponPivot +
                                   weaponMotion * (position - weaponPivot) +
                                   new Vector3(
                                       currentAttackPose * weaponSide * 0.025f,
                                       currentAttackCharge * 0.025f,
                                       currentAttackPose * 0.055f) * definitionScale;
                        rotation = weaponMotion * rotation;
                        break;
                    }

                    case PartRole.Shield:
                    {
                        position = shieldPivot +
                                   shieldMotion * (position - shieldPivot) +
                                   Vector3.forward * currentAttackCharge * 0.035f * definitionScale;
                        rotation = shieldMotion * rotation;
                        break;
                    }

                    case PartRole.Focus:
                    {
                        var focusPulse =
                            1f +
                            Mathf.Sin(lifeTime * 4.4f + part.phase) * 0.035f +
                            currentAttackCharge * (caster || flyer ? 0.22f : 0.1f) +
                            currentRelease * 0.1f;
                        scale *= focusPulse;
                        position.y += Mathf.Sin(lifeTime * 3.1f + part.phase) *
                                      0.012f * definitionScale;
                        rotation = Quaternion.Euler(
                            0f,
                            lifeTime * (flyer ? 18f : 7f) + part.phase * 10f,
                            currentAttackPose * part.side * 7f) * rotation;
                        break;
                    }

                    case PartRole.Orbit:
                    {
                        var radius = 1f - currentAttackCharge * 0.22f + currentRelease * 0.16f;
                        position = orbitPivot +
                                   orbitMotion * ((position - orbitPivot) * radius);
                        rotation = orbitMotion *
                                   Quaternion.Euler(orbitAngle * 0.35f, 0f, orbitAngle * 0.2f) *
                                   rotation;
                        scale *= 1f + currentAttackCharge * 0.12f;
                        break;
                    }

                    case PartRole.Tail:
                    {
                        var tailWave =
                            Mathf.Sin(lifeTime * (flyer ? 4.4f : 3.2f) + part.phase) *
                            (4f + movement * 8f);
                        rotation = Quaternion.Euler(
                            movement * 3f,
                            tailWave,
                            tailWave * part.side * 0.45f) * rotation;
                        position.x += tailWave * 0.0015f * definitionScale;
                        break;
                    }

                    default:
                    {
                        var accentPulse =
                            1f +
                            breath * 0.008f +
                            currentAttackCharge * part.actionWeight * 0.08f;
                        scale *= accentPulse;
                        rotation = Quaternion.Euler(
                            0f,
                            stride * movement * part.side * 2f,
                            breath * part.side * 0.7f) * rotation;
                        break;
                    }
                }

                if (currentHit > 0f)
                {
                    rotation = Quaternion.Euler(
                        -currentHit * (4f + part.actionWeight * 5f),
                        currentHit * recoilSide * part.side * 4f,
                        currentHit * recoilSide * 3f) * rotation;
                    position += new Vector3(
                        recoilSide * part.side * 0.012f,
                        0.006f,
                        -0.018f) * (currentHit * definitionScale);
                }

                if (currentDeath > 0f)
                {
                    position += new Vector3(
                        part.side * 0.035f,
                        -0.08f - Mathf.Abs(part.position.x) * 0.018f,
                        -0.025f) * (currentDeath * definitionScale);
                    rotation = Quaternion.Euler(
                        currentDeath * (4f + part.actionWeight * 8f),
                        currentDeath * part.side * 7f,
                        currentDeath * recoilSide * (3f + part.phase % 6f)) * rotation;
                    scale *= 1f - currentDeath * 0.065f;
                }

                part.target.localPosition = position;
                part.target.localRotation = rotation;
                part.target.localScale = scale;
            }
        }

        private void CaptureRootPose()
        {
            if (bindingRoot == null)
            {
                rootPoseCaptured = false;
                return;
            }

            rootPosition = bindingRoot.localPosition;
            rootRotation = bindingRoot.localRotation;
            rootScale = bindingRoot.localScale;
            rootPoseCaptured = true;
        }

        private void BindParts()
        {
            if (bindingRoot == null)
            {
                partCount = 0;
                return;
            }

            var childCount = bindingRoot.childCount;
            if (parts == null || parts.Length != childCount)
            {
                parts = new PartPose[childCount];
            }

            partCount = childCount;
            var headSum = Vector3.zero;
            var weaponSum = Vector3.zero;
            var shieldSum = Vector3.zero;
            var focusSum = Vector3.zero;
            var orbitSum = Vector3.zero;
            var headCount = 0;
            var weaponCount = 0;
            var shieldCount = 0;
            var focusCount = 0;
            var orbitCount = 0;

            for (var index = 0; index < childCount; index++)
            {
                var target = bindingRoot.GetChild(index);
                var partName = target.name;
                var role = ClassifyPart(partName);
                var side = NameContains(partName, "left")
                    ? -1f
                    : NameContains(partName, "right")
                        ? 1f
                        : Mathf.Abs(target.localPosition.x) > 0.015f
                            ? Mathf.Sign(target.localPosition.x)
                            : index % 2 == 0 ? -1f : 1f;
                var pairPhase = NameContains(partName, "hind") ? -1f : 1f;
                var actionWeight =
                    NameContains(partName, "jaw") ||
                    NameContains(partName, "muzzle") ||
                    NameContains(partName, "nozzle") ||
                    NameContains(partName, "beak") ||
                    NameContains(partName, "fang") ||
                    NameContains(partName, "tooth")
                        ? 1f
                        : role == PartRole.Accent ? 0.65f : 0.35f;

                parts[index] = new PartPose
                {
                    target = target,
                    position = target.localPosition,
                    rotation = target.localRotation,
                    scale = target.localScale,
                    role = role,
                    side = side,
                    pairPhase = pairPhase,
                    actionWeight = actionWeight,
                    phase = index * 1.371f + idlePhase
                };

                switch (role)
                {
                    case PartRole.Head:
                        headSum += target.localPosition;
                        headCount++;
                        break;
                    case PartRole.Weapon:
                        weaponSum += target.localPosition;
                        weaponCount++;
                        break;
                    case PartRole.Shield:
                        shieldSum += target.localPosition;
                        shieldCount++;
                        break;
                    case PartRole.Focus:
                        focusSum += target.localPosition;
                        focusCount++;
                        break;
                    case PartRole.Orbit:
                        orbitSum += target.localPosition;
                        orbitCount++;
                        break;
                }
            }

            headPivot = headCount > 0
                ? headSum / headCount
                : Vector3.up * definitionScale * 1.45f;
            weaponPivot = weaponCount > 0
                ? weaponSum / weaponCount
                : new Vector3(0.5f, 1f, 0.1f) * definitionScale;
            shieldPivot = shieldCount > 0
                ? shieldSum / shieldCount
                : new Vector3(-0.5f, 1f, 0.2f) * definitionScale;
            focusPivot = focusCount > 0
                ? focusSum / focusCount
                : Vector3.up * definitionScale;
            orbitPivot = orbitCount > 0 ? orbitSum / orbitCount : focusPivot;
            weaponSide = Mathf.Abs(weaponPivot.x) > 0.02f ? Mathf.Sign(weaponPivot.x) : 1f;
        }

        private void BindGameplaySignals()
        {
            bodyVisual = null;
            bodySignalMaterial = null;
            if (gameplayRoot == null)
            {
                return;
            }

            Renderer fallbackRenderer = null;
            for (var index = 0; index < gameplayRoot.childCount; index++)
            {
                var child = gameplayRoot.GetChild(index);
                if (child == bindingRoot)
                {
                    continue;
                }

                var childRenderer = child.GetComponent<Renderer>();
                if (childRenderer == null)
                {
                    continue;
                }

                if (fallbackRenderer == null)
                {
                    fallbackRenderer = childRenderer;
                }

                if (child.name.EndsWith(" Visual", StringComparison.Ordinal))
                {
                    bodyVisual = child;
                    bodySignalMaterial = childRenderer.sharedMaterial;
                    break;
                }
            }

            if (bodyVisual == null && fallbackRenderer != null)
            {
                bodyVisual = fallbackRenderer.transform;
                bodySignalMaterial = fallbackRenderer.sharedMaterial;
            }

            bodyReferenceScale = attackStyle == EnemyAttackStyle.Ranged
                ? new Vector3(definitionScale, definitionScale * 0.82f, definitionScale)
                : Vector3.one * definitionScale;
        }

        private void ResetMotionSample()
        {
            if (gameplayRoot == null)
            {
                return;
            }

            previousWorldPosition = gameplayRoot.position;
            previousForward = gameplayRoot.forward;
            forwardMotion = 0f;
            strafeMotion = 0f;
            turnMotion = 0f;
            motionBurst = false;
        }

        private bool Is(TetherfallHostileArchetype value)
        {
            return (archetype & value) != 0;
        }

        private static TetherfallHostileArchetype ClassifyArchetype(
            string enemyId,
            EnemyAttackStyle style)
        {
            TetherfallHostileArchetype result;
            switch (enemyId ?? string.Empty)
            {
                case "enemy.mossglass_stalker":
                case "enemy.fen_spitter":
                case "enemy.ashjaw_brute":
                    result = TetherfallHostileArchetype.Creature;
                    break;
                case "enemy.vault_wisp":
                    result = TetherfallHostileArchetype.Flyer;
                    break;
                case "enemy.fen_siltcaller":
                    result = TetherfallHostileArchetype.Caster;
                    break;
                case "enemy.hollow_raider":
                case "enemy.hollow_scout":
                case "enemy.relay_breaker":
                case "enemy.cinder_echo":
                    result = TetherfallHostileArchetype.WeaponWielder;
                    break;
                case "enemy.elite.mossfang_matriarch":
                    result = TetherfallHostileArchetype.Creature |
                             TetherfallHostileArchetype.Elite;
                    break;
                case "enemy.elite.siltglass_heron":
                    result = TetherfallHostileArchetype.Creature |
                             TetherfallHostileArchetype.Flyer |
                             TetherfallHostileArchetype.Elite;
                    break;
                case "enemy.elite.cindermaw":
                case "enemy.elite.mirewarden":
                    result = TetherfallHostileArchetype.WeaponWielder |
                             TetherfallHostileArchetype.Elite;
                    break;
                default:
                    result = style == EnemyAttackStyle.Ranged
                        ? TetherfallHostileArchetype.Caster
                        : TetherfallHostileArchetype.WeaponWielder;
                    break;
            }

            if (!string.IsNullOrEmpty(enemyId) &&
                enemyId.StartsWith("enemy.elite.", StringComparison.Ordinal))
            {
                result |= TetherfallHostileArchetype.Elite;
            }

            return result;
        }

        private static PartRole ClassifyPart(string partName)
        {
            if (NameContains(partName, "orbit"))
            {
                return PartRole.Orbit;
            }

            if (NameContains(partName, "dorsal"))
            {
                return PartRole.Accent;
            }

            if (NameContains(partName, "wing") ||
                NameContains(partName, "vane") ||
                NameContains(partName, "fin") ||
                NameContains(partName, "cape") ||
                NameContains(partName, "mantle"))
            {
                return PartRole.Wing;
            }

            if (NameContains(partName, "tail") ||
                NameContains(partName, "filament") ||
                NameContains(partName, "quill") ||
                NameContains(partName, "pennant") ||
                NameContains(partName, "crest"))
            {
                return PartRole.Tail;
            }

            if (NameContains(partName, "shield") ||
                NameContains(partName, "buckler"))
            {
                return PartRole.Shield;
            }

            if (NameContains(partName, "cleaver") ||
                NameContains(partName, "blade") ||
                NameContains(partName, "bow") ||
                NameContains(partName, "string") ||
                NameContains(partName, "nock") ||
                NameContains(partName, "arrow") ||
                NameContains(partName, "quiver") ||
                NameContains(partName, "staff") ||
                NameContains(partName, "maul") ||
                NameContains(partName, "haft") ||
                NameContains(partName, "grip") ||
                NameContains(partName, "anchor") ||
                NameContains(partName, "fluke") ||
                NameContains(partName, "crossbar"))
            {
                return PartRole.Weapon;
            }

            if (NameContains(partName, "head") ||
                NameContains(partName, "skull") ||
                NameContains(partName, "hood") ||
                NameContains(partName, "cowl") ||
                NameContains(partName, "helm") ||
                NameContains(partName, "mask") ||
                NameContains(partName, "visor") ||
                NameContains(partName, "muzzle") ||
                NameContains(partName, "jaw") ||
                NameContains(partName, "nozzle") ||
                NameContains(partName, "beak") ||
                NameContains(partName, "tooth") ||
                NameContains(partName, "fang") ||
                NameContains(partName, "neck") ||
                NameContains(partName, "antler") ||
                NameContains(partName, "horn") ||
                NameContains(partName, "tine"))
            {
                return PartRole.Head;
            }

            if (NameContains(partName, "foreleg") ||
                NameContains(partName, "hindleg") ||
                NameContains(partName, "forelimb") ||
                NameContains(partName, "hindlimb") ||
                NameContains(partName, "claw") ||
                NameContains(partName, "stilt leg") ||
                NameContains(partName, "foot") ||
                NameContains(partName, "gauntlet") ||
                NameContains(partName, "shoulder") ||
                NameContains(partName, "pauldron"))
            {
                return PartRole.Limb;
            }

            if (NameContains(partName, "focus") ||
                NameContains(partName, "reservoir") ||
                NameContains(partName, "lens") ||
                NameContains(partName, "coal") ||
                NameContains(partName, "prism") ||
                NameContains(partName, "relay") ||
                NameContains(partName, "bead") ||
                NameContains(partName, "eye") ||
                NameContains(partName, "seam"))
            {
                return PartRole.Focus;
            }

            if (NameContains(partName, "body") ||
                NameContains(partName, "torso") ||
                NameContains(partName, "chest") ||
                NameContains(partName, "belly") ||
                NameContains(partName, "cuirass") ||
                NameContains(partName, "coat") ||
                NameContains(partName, "robe") ||
                NameContains(partName, "shell") ||
                NameContains(partName, "breast") ||
                NameContains(partName, "haunch"))
            {
                return PartRole.Core;
            }

            return PartRole.Accent;
        }

        private static bool NameContains(string value, string fragment)
        {
            return value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static float SafeRatio(float value, float reference)
        {
            return Mathf.Abs(reference) > 0.0001f ? Mathf.Abs(value / reference) : 1f;
        }

        private static float ColorDistanceSquared(Color left, Color right)
        {
            var red = left.r - right.r;
            var green = left.g - right.g;
            var blue = left.b - right.b;
            return red * red + green * green + blue * blue;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Edit-mode smoke verifier for all currently catalogued hostile silhouettes.
    /// Run Tetherfall/Verify Hostile Animation Layer from the Unity menu.
    /// </summary>
    public static class TetherfallHostileAnimationVerifier
    {
        private static readonly string[] EnemyIds =
        {
            "enemy.hollow_raider",
            "enemy.hollow_scout",
            "enemy.mossglass_stalker",
            "enemy.fen_spitter",
            "enemy.fen_siltcaller",
            "enemy.relay_breaker",
            "enemy.cinder_echo",
            "enemy.ashjaw_brute",
            "enemy.vault_wisp",
            "enemy.elite.mossfang_matriarch",
            "enemy.elite.siltglass_heron",
            "enemy.elite.cindermaw",
            "enemy.elite.mirewarden"
        };

        [UnityEditor.MenuItem("Tetherfall/Verify Hostile Animation Layer")]
        private static void VerifyFromMenu()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Hostile animation verification must run outside Play Mode.");
                return;
            }

            if (VerifyAll(out var report))
            {
                Debug.Log(report);
            }
            else
            {
                Debug.LogError(report);
            }
        }

        public static void VerifyBatch()
        {
            if (!VerifyAll(out var report))
            {
                throw new InvalidOperationException(report);
            }

            Debug.Log(report);
        }

        public static bool VerifyAll(out string report)
        {
            var suiteRoot = new GameObject("Hostile Animation Verification");
            var failures = 0;
            var details = string.Empty;

            try
            {
                for (var idIndex = 0; idIndex < EnemyIds.Length; idIndex++)
                {
                    var enemyId = EnemyIds[idIndex];
                    var definition = EnemyCatalog.Get(enemyId);
                    var hostileObject = new GameObject("Verifier " + enemyId);
                    hostileObject.transform.SetParent(suiteRoot.transform, false);
                    var hostile = hostileObject.AddComponent<Enemy>();
                    var artRoot = TetherfallHostileArtOverhaul.Build(hostile, definition);
                    if (artRoot == null)
                    {
                        AddFailure(ref failures, ref details, enemyId, "Build returned null");
                        continue;
                    }

                    var animation = artRoot.GetComponent<TetherfallHostileAnimation>();
                    if (animation == null || !animation.IsBound)
                    {
                        AddFailure(ref failures, ref details, enemyId, "animation did not bind");
                        continue;
                    }

                    if (animation.Archetype == TetherfallHostileArchetype.None)
                    {
                        AddFailure(ref failures, ref details, enemyId, "archetype was not classified");
                    }

                    animation.NotifyAttackAnticipation(0.2f);
                    var hooksValid =
                        animation.ActionPhase == TetherfallHostileActionPhase.Anticipation;
                    animation.NotifyAttackRelease();
                    hooksValid &= animation.ActionPhase == TetherfallHostileActionPhase.Release;
                    animation.NotifyHit();
                    hooksValid &= animation.ActionPhase == TetherfallHostileActionPhase.Hit;
                    animation.NotifyDeath();
                    hooksValid &= animation.ActionPhase == TetherfallHostileActionPhase.Death;
                    if (!hooksValid)
                    {
                        AddFailure(ref failures, ref details, enemyId, "public action hooks did not transition");
                    }

                    var directPositions = new Vector3[artRoot.childCount];
                    var directRotations = new Quaternion[artRoot.childCount];
                    var directScales = new Vector3[artRoot.childCount];
                    for (var childIndex = 0; childIndex < artRoot.childCount; childIndex++)
                    {
                        var child = artRoot.GetChild(childIndex);
                        directPositions[childIndex] = child.localPosition;
                        directRotations[childIndex] = child.localRotation;
                        directScales[childIndex] = child.localScale;
                    }

                    artRoot.localPosition += new Vector3(0.17f, 0.11f, -0.09f);
                    artRoot.localRotation *= Quaternion.Euler(7f, 11f, -5f);
                    if (artRoot.childCount > 0)
                    {
                        var firstPart = artRoot.GetChild(0);
                        firstPart.localPosition += Vector3.one * 0.08f;
                        firstPart.localRotation *= Quaternion.Euler(9f, -4f, 6f);
                        firstPart.localScale *= 1.09f;
                    }

                    animation.ResetPose();
                    var rebuiltRoot = TetherfallHostileArtOverhaul.Build(hostile, definition);
                    if (rebuiltRoot != artRoot)
                    {
                        AddFailure(ref failures, ref details, enemyId, "rebuild replaced the art root");
                    }

                    if (!PoseMatches(
                            artRoot,
                            directPositions,
                            directRotations,
                            directScales))
                    {
                        AddFailure(ref failures, ref details, enemyId, "rebuild introduced pose drift");
                    }

                    var animations =
                        hostileObject.GetComponentsInChildren<TetherfallHostileAnimation>(true);
                    if (animations.Length != 1 ||
                        animations[0].BindingRoot != artRoot ||
                        animations[0].transform != artRoot)
                    {
                        AddFailure(
                            ref failures,
                            ref details,
                            enemyId,
                            "expected one component with one art binding root");
                    }

                    if (animation.BoundPartCount != artRoot.childCount)
                    {
                        AddFailure(ref failures, ref details, enemyId, "not every named part was bound");
                    }

                    if (hostileObject.GetComponentsInChildren<Collider>(true).Length != 0)
                    {
                        AddFailure(ref failures, ref details, enemyId, "generated art retained a collider");
                    }

                    if (hostileObject.GetComponentsInChildren<Light>(true).Length != 0)
                    {
                        AddFailure(ref failures, ref details, enemyId, "generated art added a light");
                    }

                    if (hostileObject.GetComponentsInChildren<Animator>(true).Length != 0)
                    {
                        AddFailure(ref failures, ref details, enemyId, "generated art added an Animator");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(suiteRoot);
            }

            report = failures == 0
                ? "Hostile animation verifier passed: 13/13 definitions, one binding each, " +
                  "all action hooks, safe rebuilds, restored poses, and no colliders, lights, or Animators."
                : "Hostile animation verifier failed with " + failures + " issue(s):" + details;
            return failures == 0;
        }

        private static bool PoseMatches(
            Transform root,
            Vector3[] positions,
            Quaternion[] rotations,
            Vector3[] scales)
        {
            if (root.childCount != positions.Length)
            {
                return false;
            }

            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if ((child.localPosition - positions[index]).sqrMagnitude > 0.0000001f ||
                    Mathf.Abs(Quaternion.Dot(child.localRotation, rotations[index])) < 0.999999f ||
                    (child.localScale - scales[index]).sqrMagnitude > 0.0000001f)
                {
                    return false;
                }
            }

            return root.localPosition.sqrMagnitude < 0.0000001f &&
                   Mathf.Abs(Quaternion.Dot(root.localRotation, Quaternion.identity)) > 0.999999f &&
                   (root.localScale - Vector3.one).sqrMagnitude < 0.0000001f;
        }

        private static void AddFailure(
            ref int failures,
            ref string details,
            string enemyId,
            string issue)
        {
            failures++;
            details += "\n- " + enemyId + ": " + issue;
        }
    }
#endif
}
