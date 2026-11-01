using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed class PlayerAvatar : MonoBehaviour
    {
        private const float WayfinderJumpVelocity = 7.1f;
        private const float MountedJumpVelocity = 6.8f;
        private const float WayfinderRiseGravity = 21f;
        private const float WayfinderFallGravity = 28f;
        private const float CoyoteTime = 0.1f;
        private const float JumpBufferTime = 0.12f;
        private const float GroundedVerticalVelocity = -1.2f;
        private const float GlideGravityScale = 0.16f;
        private const float GlideTerminalVelocity = -2.15f;
        private const float VoidRecoveryHeight = -14f;
        private const float MantleDuration = 0.28f;
        private const float MantleCooldown = 0.38f;
        private const float MantleWallReach = 1.05f;
        private const float MantleMinHeight = 0.55f;
        private const float MantleMaxHeight = 1.48f;
        private const float MantleArcClearance = 0.18f;
        private const int MantleQueryCapacity = 24;
        private const int CombatQueryCapacity = 96;
        private const int CombatLineQueryCapacity = 32;

        private TetherfallGame game;
        private CharacterController controller;
        private Camera gameplayCamera;
        private readonly RaycastHit[] mantleHitBuffer = new RaycastHit[MantleQueryCapacity];
        private readonly Collider[] mantleOverlapBuffer = new Collider[MantleQueryCapacity];
        private readonly Collider[] combatHitBuffer = new Collider[CombatQueryCapacity];
        private readonly RaycastHit[] combatLineHitBuffer = new RaycastHit[CombatLineQueryCapacity];
        private readonly HashSet<Enemy> struckEnemies = new HashSet<Enemy>();
        private readonly HashSet<DungeonBoss> struckBosses = new HashSet<DungeonBoss>();
        private readonly HashSet<WaterVolume> waterVolumes = new HashSet<WaterVolume>();
        private Transform visual;
        private PactCreature mountedCreature;
        private float verticalVelocity;
        private float lastGroundedAt = float.NegativeInfinity;
        private float jumpBufferedUntil = float.NegativeInfinity;
        private float nextAttackTime;
        private float lastAttackAt;
        private float nextAbilityTime;
        private float nextDodgeTime;
        private float dodgeEndsAt;
        private float guardStartedAt;
        private int comboStage;
        private Vector3 dodgeDirection;
        private bool isMounted;
        private bool swimming;
        private bool gliding;
        private bool climbing;
        private bool dodging;
        private bool guarding;
        private bool externalLaunchPending;
        private bool externalLaunchActive;
        private bool jumpConsumed;
        private bool glideFreshPressReady;
        private bool sprintToggleState;
        private bool guardToggleState;
        private bool glideToggleState;
        private bool pactThermalLiftUsed;
        private bool mantling;
        private float externalLaunchGraceUntil;
        private float mantleStartedAt;
        private float mantleProgress;
        private float nextMantleTime;
        private Transform gliderVisual;
        private Transform weaponVisual;
        private Transform fieldGearVisual;
        private Renderer bodyRenderer;
        private WayfinderProceduralAnimator proceduralAnimator;
        private Color baseCoatColor;
        private Vector3 lastMovementInput;
        private float lastTraversalVerticalInput;
        private float lastTraversalLateralInput;
        private Vector3 lastSafePosition;
        private Vector3 mantleStart;
        private Vector3 mantleLift;
        private Vector3 mantleOver;
        private Vector3 mantleLanding;
        private Vector3 mantleForward;
        private float mantleSurfaceY;
        private bool hasSafePosition;
        private float attackVisualUntil;
        private float abilityVisualUntil;

        public bool IsMounted => isMounted;
        public bool IsDodging => dodging;
        public bool IsGuarding => guarding;
        public bool IsMantling => mantling;
        public bool IsSwimming => swimming;
        public bool IsClimbing => climbing;
        public bool IsGliding => gliding;

        public void Initialize(TetherfallGame owner)
        {
            game = owner;
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.36f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            lastSafePosition = transform.position;
            hasSafePosition = true;
            CreateVisual();
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    game.AcquireGameplayCursor();
                }

                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    PausePlayerTimers();
                    return;
                }
            }

            if (game.GameplayPaused)
            {
                PausePlayerTimers();
                return;
            }

            Move();
            UpdateVisualMotion();
            UpdateProceduralAnimation();
        }

        private void PausePlayerTimers()
        {
            var delta = Time.deltaTime;
            ShiftTimer(ref nextAttackTime, delta);
            ShiftTimer(ref lastAttackAt, delta);
            ShiftTimer(ref nextAbilityTime, delta);
            ShiftTimer(ref nextDodgeTime, delta);
            ShiftTimer(ref dodgeEndsAt, delta);
            ShiftTimer(ref guardStartedAt, delta);
            ShiftTimer(ref externalLaunchGraceUntil, delta);
            ShiftTimer(ref nextMantleTime, delta);
            ShiftTimer(ref mantleStartedAt, delta);
            ShiftTimer(ref attackVisualUntil, delta);
            ShiftTimer(ref abilityVisualUntil, delta);
            if (!float.IsNegativeInfinity(lastGroundedAt))
            {
                lastGroundedAt += delta;
            }

            if (!float.IsNegativeInfinity(jumpBufferedUntil))
            {
                jumpBufferedUntil += delta;
            }
        }

        private static void ShiftTimer(ref float timer, float delta)
        {
            if (timer > 0f && !float.IsInfinity(timer))
            {
                timer += delta;
            }
        }

        public void TryAttack()
        {
            if (game == null ||
                game.GameplayPaused ||
                dodging ||
                guarding ||
                mantling ||
                Time.time < nextAttackTime)
            {
                return;
            }

            game.RecordTutorialEvent(TutorialEvent.Attack);
            nextAttackTime = Time.time + (isMounted ? 0.58f : game.ActiveWeapon == WeaponDiscipline.Riftbow ? 0.54f : 0.42f);
            if (!isMounted && game.ActiveWeapon == WeaponDiscipline.Pathblade)
            {
                comboStage = Time.time - lastAttackAt <= 0.95f ? (comboStage + 1) % 3 : 0;
            }
            else
            {
                comboStage = 0;
            }

            lastAttackAt = Time.time;
            attackVisualUntil = Time.time + 0.2f;
            proceduralAnimator?.TriggerWeaponAttack(
                1f + comboStage * 0.12f,
                comboStage % 2 == 1,
                0.08f,
                0.22f + comboStage * 0.03f);
            var camera = GetGameplayCamera();
            var direction = camera != null ? camera.transform.forward : transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = transform.forward;
            }

            direction = game.GetCombatAimDirection(transform.position, direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.8f);
            var damage = isMounted
                ? Mathf.RoundToInt(
                    34f *
                    GetMountedDisciplineDamageMultiplier() *
                    game.MountedPactDamageMultiplier)
                : game.GetBaseDamage() + comboStage * 5;
            var attackPitch = GetWeaponOvertonePitch();
            var crossweaveRelayToken =
                game.CaptureCrossweaveRelayToken(game.ActiveWeapon);
            var attackName = isMounted
                ? MountedWeaponAttackName()
                : WeaponAttackName();
            game.PlayWeaponSwing(game.ActiveWeapon, transform.position, direction, comboStage);
            if (!isMounted &&
                game.ActiveWeapon != WeaponDiscipline.Riftbow &&
                controller != null)
            {
                controller.Move(direction * (0.34f + comboStage * 0.1f));
            }

            if (game.ActiveWeapon == WeaponDiscipline.Riftbow)
            {
                ThreadglassProjectile.Create(
                    game,
                    transform.position + Vector3.up * 1.05f + direction * 0.7f,
                    direction,
                    damage,
                    attackPitch,
                    false,
                    crossweaveRelayToken);
                CreateAttackPulse(transform.position + direction * 1.2f, 1.15f);
                return;
            }

            var radius = isMounted
                ? game.ActiveWeapon == WeaponDiscipline.Threadstaff
                    ? 3.65f
                    : 2.7f
                : game.ActiveWeapon == WeaponDiscipline.Threadstaff
                    ? 4.05f
                    : 2.15f;
            var offset = game.ActiveWeapon == WeaponDiscipline.Threadstaff ? 1.1f : 1.55f;
            CreateAttackPulse(
                transform.position + direction * offset,
                game.ActiveWeapon == WeaponDiscipline.Threadstaff ? 4.8f : isMounted ? 2.9f : 2.25f + comboStage * 0.34f);

            var hitCount = Physics.OverlapSphereNonAlloc(
                transform.position + direction * offset,
                radius,
                combatHitBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);
            struckEnemies.Clear();
            struckBosses.Clear();
            var strikeOrigin = transform.position + Vector3.up * 0.95f;
            var wideArc = game.ActiveWeapon == WeaponDiscipline.Threadstaff;
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = combatHitBuffer[hitIndex];
                if (hit == null)
                {
                    continue;
                }

                var enemy = hit.GetComponentInParent<Enemy>();
                if (enemy != null &&
                    struckEnemies.Add(enemy) &&
                    CanStrikeTarget(enemy.transform, strikeOrigin, direction, wideArc))
                {
                    enemy.TakeDamage(
                        damage,
                        attackName,
                        isMounted
                            ? CombatImpactKind.Heavy
                            : CombatImpactKind.Light);
                    game.RegisterOvertone(enemy, attackPitch, attackName);
                    game.TryResolveCrossweave(enemy, crossweaveRelayToken);
                }

                var boss = hit.GetComponentInParent<DungeonBoss>();
                if (boss != null &&
                    struckBosses.Add(boss) &&
                    CanStrikeTarget(boss.transform, strikeOrigin, direction, wideArc))
                {
                    boss.TakeDamage(
                        damage,
                        attackName,
                        isMounted
                            ? CombatImpactKind.Heavy
                            : CombatImpactKind.Light);
                    game.RegisterOvertone(boss, attackPitch, attackName);
                    game.TryResolveCrossweave(boss, crossweaveRelayToken);
                }
            }

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                combatHitBuffer[hitIndex] = null;
            }

            struckEnemies.Clear();
            struckBosses.Clear();
        }

        public void TryResonanceAbility()
        {
            if (game == null ||
                game.GameplayPaused ||
                isMounted ||
                swimming ||
                climbing ||
                mantling ||
                dodging)
            {
                return;
            }

            if (Time.time < nextAbilityTime)
            {
                game.Notify($"Resonance returns in {Mathf.CeilToInt(nextAbilityTime - Time.time)} seconds.");
                return;
            }

            if (!game.TrySpendStamina(30f * game.CombatStaminaCostMultiplier, true))
            {
                return;
            }

            nextAbilityTime = Time.time + 8f;
            abilityVisualUntil = Time.time + 0.38f;
            proceduralAnimator?.TriggerWeaponAttack(1.45f, false, 0.14f, 0.34f);
            var camera = GetGameplayCamera();
            var direction = camera != null ? camera.transform.forward : transform.forward;
            direction.y = 0f;
            direction = game.GetCombatAimDirection(transform.position, direction);
            game.ArmThreadlinkFromPlayerAbility();
            var damage = game.ActiveWeapon switch
            {
                WeaponDiscipline.Pathblade => 54 + game.CombatDamageBonus,
                WeaponDiscipline.Riftbow => 46 + game.CombatDamageBonus,
                WeaponDiscipline.Threadstaff => 38 + game.CombatDamageBonus,
                _ => 40
            };
            switch (game.ActiveWeapon)
            {
                case WeaponDiscipline.Pathblade:
                    if (controller != null)
                    {
                        controller.Move(direction * 3.4f);
                    }

                    StrikeTargets(
                        transform.position + direction * 2.15f,
                        3.1f,
                        damage,
                        "Threadstep cleave",
                        transform.position + Vector3.up * 0.95f,
                        direction,
                        false,
                        0.9f,
                        OvertonePitch.Cut,
                        true);
                    CreateAttackPulse(transform.position + direction * 2.3f, 4.2f);
                    game.PlayWeaponSwing(game.ActiveWeapon, transform.position, direction, 2);
                    game.Notify("Threadstep cleave released.");
                    break;

                case WeaponDiscipline.Riftbow:
                    for (var index = -1; index <= 1; index++)
                    {
                        var boltDirection = Quaternion.AngleAxis(index * 6f, Vector3.up) * direction;
                        ThreadglassProjectile.Create(
                            game,
                            transform.position + Vector3.up * 1.05f + boltDirection * 0.7f,
                            boltDirection,
                            Mathf.RoundToInt(damage * (index == 0 ? 0.9f : 0.62f)),
                            OvertonePitch.Line,
                            true);
                    }

                    CreateAttackPulse(transform.position + direction * 1.1f, 2.1f);
                    game.Notify("Riftbow triad released.");
                    break;

                case WeaponDiscipline.Threadstaff:
                    StrikeTargets(
                        transform.position,
                        7f,
                        damage,
                        "Resonance ward",
                        transform.position + Vector3.up * 0.95f,
                        direction,
                        true,
                        0.62f,
                        OvertonePitch.Ring,
                        true);
                    game.HealPlayer(18);
                    CreateAttackPulse(transform.position + Vector3.up * 0.25f, 8f);
                    game.Notify("Resonance ward released.");
                    break;
            }
        }

        public void SetMounted(bool mounted, PactCreature creature)
        {
            if (mantling && mounted)
            {
                return;
            }

            if (mounted == isMounted)
            {
                return;
            }

            CancelMantle();
            var previousMount = mountedCreature;
            isMounted = mounted;
            mountedCreature = mounted ? creature : null;
            if (!isMounted && previousMount != null)
            {
                previousMount.SetMounted(false, null);
            }
            if (isMounted)
            {
                swimming = false;
                gliding = false;
                climbing = false;
                externalLaunchPending = false;
                externalLaunchActive = false;
                glideFreshPressReady = false;
                glideToggleState = false;
                pactThermalLiftUsed = false;
                guardToggleState = false;
                jumpBufferedUntil = float.NegativeInfinity;
                lastTraversalVerticalInput = 0f;
                lastTraversalLateralInput = 0f;
                SetGliderVisible(false);
            }
            if (mountedCreature != null)
            {
                mountedCreature.SetMounted(true, transform);
                game.Notify($"Riding {mountedCreature.CreatureName}. Sprint has greater reach.");
            }
            else
            {
                RefreshWaterState();
                game.Notify("You dismount and continue on foot.");
            }

            visual.localPosition = isMounted ? new Vector3(0f, 0.85f, 0f) : Vector3.zero;
            UpdateWeaponVisibility();
        }

        public void SetSwimming(bool isSwimming)
        {
            swimming = isSwimming && !isMounted;
            if (swimming)
            {
                CancelMantle();
                gliding = false;
                climbing = false;
                externalLaunchPending = false;
                externalLaunchActive = false;
                glideFreshPressReady = false;
                glideToggleState = false;
                pactThermalLiftUsed = false;
                guardToggleState = false;
                jumpBufferedUntil = float.NegativeInfinity;
                lastTraversalLateralInput = 0f;
                SetGliderVisible(false);
            }
        }

        internal void EnterWaterVolume(WaterVolume volume)
        {
            if (volume == null || !waterVolumes.Add(volume))
            {
                return;
            }

            RefreshWaterState();
        }

        internal void ExitWaterVolume(WaterVolume volume)
        {
            if (volume == null || !waterVolumes.Remove(volume))
            {
                return;
            }

            RefreshWaterState();
        }

        private void RefreshWaterState()
        {
            SetSwimming(!isMounted && waterVolumes.Count > 0);
        }

        public void ApplyWindLift()
        {
            ApplyWindLift(5.6f);
        }

        public void ApplyWindLift(float launchVelocity)
        {
            if (isMounted || swimming || mantling)
            {
                return;
            }

            verticalVelocity = Mathf.Max(
                verticalVelocity,
                Mathf.Max(0f, launchVelocity) + game.WindLiftVelocityBonus);
            externalLaunchPending = true;
            externalLaunchActive = true;
            externalLaunchGraceUntil = Time.time + 0.3f;
            climbing = false;
            gliding = true;
            glideFreshPressReady = false;
            jumpBufferedUntil = float.NegativeInfinity;
        }

        public bool TryUsePactThermalLift()
        {
            if (game == null || !game.HasBoundedPactThermalLift)
            {
                return false;
            }

            if (controller == null ||
                controller.isGrounded ||
                isMounted ||
                swimming ||
                climbing ||
                mantling ||
                !gliding)
            {
                return false;
            }

            if (pactThermalLiftUsed)
            {
                game.Notify("Cinderwing's thermal lift returns after you touch stable ground.");
                return true;
            }

            if (!game.TrySpendStamina(18f, true))
            {
                return true;
            }

            pactThermalLiftUsed = true;
            verticalVelocity = Mathf.Max(verticalVelocity, game.BoundedPactThermalLiftVelocity);
            gliding = true;
            glideToggleState = true;
            glideFreshPressReady = false;
            jumpBufferedUntil = float.NegativeInfinity;
            game.Notify("Cinderwing shares one bounded thermal lift.");
            return true;
        }

        public void ApplyImpact(Vector3 displacement)
        {
            if (controller == null || dodging || mantling)
            {
                return;
            }

            controller.Move(displacement);
        }

        public int ModifyIncomingDamage(int amount)
        {
            if (dodging)
            {
                return 0;
            }

            if (guarding)
            {
                if (Time.time - guardStartedAt <= 0.28f)
                {
                    game.TriggerPerfectGuard(transform.position);
                    return 0;
                }

                return Mathf.Max(1, Mathf.RoundToInt(amount * 0.34f) - game.GuardBonus - game.ExpeditionWardReduction);
            }

            return Mathf.Max(1, amount - game.ExpeditionWardReduction);
        }

        private Camera GetGameplayCamera()
        {
            if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled)
            {
                gameplayCamera = Camera.main;
            }

            return gameplayCamera;
        }

        public void Teleport(Vector3 position)
        {
            CancelMantle();
            var restoreController = controller != null && controller.enabled;
            try
            {
                if (restoreController)
                {
                    controller.enabled = false;
                }

                transform.position = position;
            }
            finally
            {
                if (controller != null && restoreController)
                {
                    controller.enabled = true;
                }
            }

            verticalVelocity = 0f;
            climbing = false;
            waterVolumes.Clear();
            swimming = false;
            gliding = false;
            externalLaunchPending = false;
            externalLaunchActive = false;
            jumpConsumed = false;
            glideFreshPressReady = false;
            sprintToggleState = false;
            guardToggleState = false;
            glideToggleState = false;
            pactThermalLiftUsed = false;
            lastGroundedAt = float.NegativeInfinity;
            jumpBufferedUntil = float.NegativeInfinity;
            lastTraversalVerticalInput = 0f;
            lastTraversalLateralInput = 0f;
            lastSafePosition = position;
            hasSafePosition = true;
            SetGliderVisible(false);
            game?.RefreshEnemyCampActivation(true);
            game?.NotifyCameraTeleport();
            game?.RecallPactRosterToFormation();
        }

        private void Move()
        {
            if (transform.position.y < VoidRecoveryHeight)
            {
                var recoveryPosition = hasSafePosition
                    ? lastSafePosition + Vector3.up * 0.2f
                    : new Vector3(0f, 2f, -18f);
                Teleport(recoveryPosition);
                game.Notify("Threadglass recalls you to the last stable ground.");
                return;
            }

            if (mantling)
            {
                UpdateMantle();
                return;
            }

            var camera = GetGameplayCamera();
            var forward = camera != null ? camera.transform.forward : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = transform.forward;
                forward.y = 0f;
            }

            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward);

            var inputBindings = TetherfallInputBindings.Shared;
            var moveInput = inputBindings.GetMove();
            var horizontal = moveInput.x;
            var vertical = moveInput.y;
            var input = forward * vertical + right * horizontal;
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            lastMovementInput = input;
            lastTraversalVerticalInput = 0f;
            lastTraversalLateralInput = 0f;
            if (!isMounted &&
                !swimming &&
                !climbing &&
                inputBindings.WasPressedThisFrame(TetherfallInputAction.DodgeDescendDrop) &&
                Time.time >= nextDodgeTime)
            {
                BeginDodge(input.sqrMagnitude > 0.01f ? input : transform.forward);
            }

            if (isMounted || swimming || climbing)
            {
                guardToggleState = false;
            }

            var wantsGuard = !isMounted &&
                             !swimming &&
                             !climbing &&
                             TetherfallAccessibilityRuntime.ResolveInput(
                                 TetherfallAccessibilityInputAction.Guard,
                                 inputBindings.IsHeld(TetherfallInputAction.Guard),
                                 inputBindings.WasPressedThisFrame(TetherfallInputAction.Guard),
                                 ref guardToggleState);
            if (wantsGuard &&
                !game.TrySpendStamina(
                    8f * Time.deltaTime * game.CombatStaminaCostMultiplier,
                    true))
            {
                wantsGuard = false;
                guardToggleState = false;
            }

            if (wantsGuard && !guarding)
            {
                guardStartedAt = Time.time;
                game.RecordTutorialEvent(TutorialEvent.Guard);
            }

            guarding = wantsGuard;
            if (dodging)
            {
                controller.Move(dodgeDirection * 15f * Time.deltaTime);
                if (Time.time >= dodgeEndsAt)
                {
                    dodging = false;
                }

                return;
            }

            if (swimming)
            {
                var swimVertical = inputBindings.IsHeld(TetherfallInputAction.JumpGlideClimb)
                    ? 1f
                    : inputBindings.IsHeld(TetherfallInputAction.DodgeDescendDrop)
                        ? -1f
                        : 0f;
                lastTraversalVerticalInput = swimVertical;
                var swimMotion = input * (4f + game.SwimSpeedBonus) + Vector3.up * swimVertical * 3.6f;
                controller.Move(swimMotion * Time.deltaTime);
                return;
            }

            if (!isMounted && TryUpdateClimb(input))
            {
                return;
            }

            var sprinting = TetherfallAccessibilityRuntime.ResolveInput(
                TetherfallAccessibilityInputAction.Sprint,
                inputBindings.IsHeld(TetherfallInputAction.Sprint),
                inputBindings.WasPressedThisFrame(TetherfallInputAction.Sprint),
                ref sprintToggleState);
            if (sprinting &&
                !isMounted &&
                input.sqrMagnitude > 0.01f &&
                !game.TrySpendStamina(17f * Time.deltaTime))
            {
                sprinting = false;
                sprintToggleState = false;
            }
            var speed = isMounted
                ? (sprinting ? 13f : 9.5f) * game.MountedPactSpeedMultiplier
                : sprinting ? 8.4f : 5.9f;
            speed *= 1f + game.MoveSpeedBonus;
            if (guarding)
            {
                speed *= 0.43f;
            }
            if (input.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(input), Time.deltaTime * 13f);
            }

            var grounded = controller.isGrounded;
            if (grounded)
            {
                lastGroundedAt = Time.time;
                lastSafePosition = transform.position;
                hasSafePosition = true;
                jumpConsumed = false;
                glideFreshPressReady = false;
                glideToggleState = false;
                pactThermalLiftUsed = false;
                if (!externalLaunchPending)
                {
                    externalLaunchActive = false;
                    gliding = false;
                    verticalVelocity = GroundedVerticalVelocity;
                }
            }

            var jumpPressed = inputBindings.WasPressedThisFrame(TetherfallInputAction.JumpGlideClimb);
            var jumpHeld = inputBindings.IsHeld(TetherfallInputAction.JumpGlideClimb);
            var glideUsesToggle =
                TetherfallAccessibilityRuntime.GetInputPreference(TetherfallAccessibilityInputAction.Glide) ==
                TetherfallAccessibilityInputPreference.Toggle;
            if (jumpPressed)
            {
                jumpBufferedUntil = Time.time + JumpBufferTime;
            }

            if (!jumpHeld)
            {
                if (!grounded)
                {
                    glideFreshPressReady = true;
                }

                if (!glideUsesToggle)
                {
                    glideToggleState = false;
                    gliding = false;
                }
            }

            var canStartJump = !externalLaunchActive &&
                               (grounded ||
                                (!isMounted &&
                                 !jumpConsumed &&
                                 Time.time - lastGroundedAt <= CoyoteTime));
            var startedJump = Time.time <= jumpBufferedUntil && canStartJump;
            if (startedJump)
            {
                verticalVelocity = isMounted
                    ? MountedJumpVelocity + game.MountedPactJumpVelocityBonus
                    : WayfinderJumpVelocity;
                jumpConsumed = true;
                gliding = false;
                glideToggleState = false;
                glideFreshPressReady = false;
                jumpBufferedUntil = float.NegativeInfinity;
            }
            else if (!grounded &&
                     !isMounted &&
                     jumpPressed &&
                     glideFreshPressReady &&
                     (verticalVelocity >= 0f || !HasLandingSurfaceBelow(0.3f)))
            {
                glideToggleState = glideUsesToggle ? !glideToggleState : true;
                gliding = glideToggleState;
                glideFreshPressReady = false;
                jumpBufferedUntil = float.NegativeInfinity;
            }

            if (!startedJump && (!grounded || externalLaunchPending))
            {
                if (gliding && verticalVelocity < 0f)
                {
                    gliding = game.TrySpendStamina(
                        12f * Time.deltaTime * game.GlideStaminaCostMultiplier);
                    if (!gliding)
                    {
                        glideToggleState = false;
                    }
                }

                if (gliding && verticalVelocity < 0f)
                {
                    verticalVelocity += Physics.gravity.y * GlideGravityScale * Time.deltaTime;
                    verticalVelocity = Mathf.Max(verticalVelocity, GlideTerminalVelocity);
                    speed *= 1.32f;
                }
                else
                {
                    var gravity = isMounted || externalLaunchActive
                        ? Physics.gravity.y
                        : verticalVelocity > 0f
                            ? -WayfinderRiseGravity
                            : -WayfinderFallGravity;
                    verticalVelocity += gravity * Time.deltaTime;
                }
            }

            var motion = input * speed;
            motion.y = verticalVelocity;
            var collisionFlags = controller.Move(motion * Time.deltaTime);
            if ((collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            {
                verticalVelocity = 0f;
            }
            else if ((collisionFlags & CollisionFlags.Below) != 0 &&
                     verticalVelocity < GroundedVerticalVelocity)
            {
                verticalVelocity = GroundedVerticalVelocity;
            }

            externalLaunchPending = false;
            SetGliderVisible(gliding);

            if (mountedCreature != null && !isMounted)
            {
                mountedCreature.SetMounted(false, null);
                mountedCreature = null;
            }
        }

        private void UpdateVisualMotion()
        {
            if (visual == null)
            {
                return;
            }

            var moving = lastMovementInput.sqrMagnitude > 0.01f;
            var basePosition = isMounted ? new Vector3(0f, 0.85f, 0f) : Vector3.zero;
            var bob = moving && !climbing && !swimming && !gliding && !mantling
                ? Mathf.Sin(Time.time * (isMounted ? 10f : 13.5f)) * (isMounted ? 0.035f : 0.052f)
                : 0f;
            var attackProgress = Mathf.Clamp01((attackVisualUntil - Time.time) / 0.2f);
            var abilityProgress = Mathf.Clamp01((abilityVisualUntil - Time.time) / 0.38f);
            var attackPitch = Mathf.Sin(attackProgress * Mathf.PI) * -26f;
            var abilityRoll = Mathf.Sin(abilityProgress * Mathf.PI) * 11f;
            var movementRoll = moving ? -lastMovementInput.x * 4.5f : 0f;

            visual.localPosition = Vector3.Lerp(
                visual.localPosition,
                basePosition + Vector3.up * bob,
                1f - Mathf.Exp(-14f * Time.deltaTime));
            visual.localRotation = Quaternion.Slerp(
                visual.localRotation,
                Quaternion.Euler(attackPitch, 180f, movementRoll + abilityRoll),
                1f - Mathf.Exp(-18f * Time.deltaTime));
        }

        private void UpdateProceduralAnimation()
        {
            if (proceduralAnimator == null || controller == null)
            {
                return;
            }

            var localMotion = transform.InverseTransformDirection(lastMovementInput);
            if (mantling)
            {
                proceduralAnimator.SetMantling(mantleProgress);
            }
            else if (isMounted)
            {
                proceduralAnimator.SetMounted(localMotion);
            }
            else if (swimming)
            {
                proceduralAnimator.SetSwimming(localMotion, lastTraversalVerticalInput);
            }
            else if (climbing)
            {
                proceduralAnimator.SetClimbing(
                    lastTraversalVerticalInput,
                    lastTraversalLateralInput);
            }
            else if (gliding)
            {
                proceduralAnimator.SetGliding(localMotion);
            }
            else if (controller.isGrounded)
            {
                proceduralAnimator.SetGroundedLocomotion(localMotion);
            }
            else
            {
                proceduralAnimator.SetAirborne(
                    localMotion,
                    Mathf.Clamp(verticalVelocity / WayfinderJumpVelocity, -1f, 1f));
            }
        }

        private int StrikeTargets(
            Vector3 center,
            float radius,
            int damage,
            string source,
            Vector3 strikeOrigin,
            Vector3 direction,
            bool wideArc,
            float staggerSeconds,
            OvertonePitch pitch = OvertonePitch.None,
            bool resonancePulse = false)
        {
            struckEnemies.Clear();
            struckBosses.Clear();
            var strikeCount = 0;
            var colliderCount = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                combatHitBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < colliderCount; hitIndex++)
            {
                var hit = combatHitBuffer[hitIndex];
                if (hit == null)
                {
                    continue;
                }

                var enemy = hit.GetComponentInParent<Enemy>();
                if (enemy != null &&
                    !enemy.IsDefeated &&
                    struckEnemies.Add(enemy) &&
                    CanStrikeTarget(enemy.transform, strikeOrigin, direction, wideArc))
                {
                    enemy.TakeDamage(damage, source);
                    game.RegisterOvertone(enemy, pitch, source);
                    if (resonancePulse)
                    {
                        game.RegisterOvertone(enemy, OvertonePitch.Pulse, source);
                    }

                    if (staggerSeconds > 0f)
                    {
                        enemy.ApplyStagger(staggerSeconds);
                    }

                    strikeCount++;
                }

                var boss = hit.GetComponentInParent<DungeonBoss>();
                if (boss != null &&
                    !boss.IsDefeated &&
                    struckBosses.Add(boss) &&
                    CanStrikeTarget(boss.transform, strikeOrigin, direction, wideArc))
                {
                    boss.TakeDamage(damage, source);
                    game.RegisterOvertone(boss, pitch, source);
                    if (resonancePulse)
                    {
                        game.RegisterOvertone(boss, OvertonePitch.Pulse, source);
                    }

                    if (staggerSeconds > 0f)
                    {
                        boss.ApplyStagger(staggerSeconds * 0.58f);
                    }

                    strikeCount++;
                }
            }

            for (var hitIndex = 0; hitIndex < colliderCount; hitIndex++)
            {
                combatHitBuffer[hitIndex] = null;
            }

            struckEnemies.Clear();
            struckBosses.Clear();
            return strikeCount;
        }

        private bool CanStrikeTarget(Transform target, Vector3 origin, Vector3 direction, bool wideArc)
        {
            if (target == null)
            {
                return false;
            }

            var targetPoint = target.position + Vector3.up * 0.9f;
            var horizontal = targetPoint - origin;
            horizontal.y = 0f;
            if (!wideArc &&
                horizontal.sqrMagnitude > 0.01f &&
                Vector3.Dot(direction, horizontal.normalized) < 0.18f)
            {
                return false;
            }

            var path = targetPoint - origin;
            var distance = path.magnitude;
            if (distance <= 0.01f)
            {
                return true;
            }

            var closestDistance = float.PositiveInfinity;
            Collider closestCollider = null;
            var lineHitCount = Physics.RaycastNonAlloc(
                origin,
                path / distance,
                combatLineHitBuffer,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < lineHitCount; hitIndex++)
            {
                var hit = combatLineHitBuffer[hitIndex];
                if (hit.collider.GetComponentInParent<PlayerAvatar>() != null)
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestCollider = hit.collider;
                }
            }

            for (var hitIndex = 0; hitIndex < lineHitCount; hitIndex++)
            {
                combatLineHitBuffer[hitIndex] = default;
            }

            if (closestCollider == null)
            {
                return true;
            }

            var closestTransform = closestCollider.transform;
            return closestTransform == target ||
                   closestTransform.IsChildOf(target) ||
                   target.IsChildOf(closestTransform);
        }

        private void BeginDodge(Vector3 direction)
        {
            if (!game.TrySpendStamina(24f * game.CombatStaminaCostMultiplier, true))
            {
                return;
            }

            dodging = true;
            dodgeDirection = direction.normalized;
            dodgeEndsAt = Time.time + 0.28f;
            nextDodgeTime = Time.time + 0.72f;
            guarding = false;
            game.RecordTutorialEvent(TutorialEvent.Dodge);
            game.Notify("Clean dodge.");
        }

        private bool TryUpdateClimb(Vector3 input)
        {
            if (TryBeginMantle(input, climbing))
            {
                return true;
            }

            if (!climbing &&
                Time.time >= externalLaunchGraceUntil &&
                !controller.isGrounded &&
                TetherfallInputBindings.Shared.IsHeld(TetherfallInputAction.JumpGlideClimb))
            {
                var origin = transform.position + Vector3.up * 1.15f;
                if (TryGetClosestRaycastHit(origin, transform.forward, 1.15f, out var hit) &&
                    hit.collider.GetComponentInParent<ClimbableSurface>() != null)
                {
                    climbing = true;
                    verticalVelocity = 0f;
                    externalLaunchActive = false;
                    jumpConsumed = true;
                    gliding = false;
                    glideFreshPressReady = false;
                    jumpBufferedUntil = float.NegativeInfinity;
                    SetGliderVisible(false);
                }
            }

            if (!climbing)
            {
                return false;
            }

            if (TetherfallInputBindings.Shared.IsHeld(TetherfallInputAction.DodgeDescendDrop))
            {
                climbing = false;
                lastTraversalVerticalInput = 0f;
                lastTraversalLateralInput = 0f;
                return false;
            }

            var wallOrigin = transform.position + Vector3.up * 1.15f;
            if (!TryGetClosestRaycastHit(
                    wallOrigin,
                    transform.forward,
                    1.35f,
                    out var wallHit) ||
                wallHit.collider.GetComponentInParent<ClimbableSurface>() == null)
            {
                climbing = false;
                verticalVelocity = 0f;
                lastTraversalVerticalInput = 0f;
                lastTraversalLateralInput = 0f;
                return false;
            }

            var climbSpeed = 3.2f * (1f + game.MoveSpeedBonus);
            var moveInput = TetherfallInputBindings.Shared.GetMove();
            var verticalInput = moveInput.y;
            if (verticalInput > 0f &&
                !game.TrySpendStamina(
                    13f * Time.deltaTime * game.ClimbStaminaCostMultiplier,
                    true))
            {
                climbing = false;
                lastTraversalVerticalInput = 0f;
                lastTraversalLateralInput = 0f;
                return false;
            }

            var lateralInput = moveInput.x;
            lastTraversalVerticalInput = verticalInput;
            lastTraversalLateralInput = lateralInput;
            var lateral = transform.right * lateralInput * 1.2f;
            controller.Move((Vector3.up * verticalInput * climbSpeed + lateral) * Time.deltaTime);
            return true;
        }

        private bool TryBeginMantle(Vector3 input, bool fromClimb)
        {
            if (mantling ||
                controller == null ||
                !controller.enabled ||
                Time.time < nextMantleTime ||
                isMounted ||
                swimming ||
                gliding ||
                dodging ||
                guarding ||
                externalLaunchActive ||
                !TetherfallInputBindings.Shared.IsHeld(TetherfallInputAction.JumpGlideClimb))
            {
                return false;
            }

            if (fromClimb)
            {
                if (TetherfallInputBindings.Shared.GetMove().y <= 0.1f)
                {
                    return false;
                }
            }
            else if (controller.isGrounded || Time.time < externalLaunchGraceUntil)
            {
                return false;
            }

            var approach = fromClimb || input.sqrMagnitude < 0.01f
                ? transform.forward
                : input;
            approach.y = 0f;
            if (approach.sqrMagnitude < 0.01f)
            {
                return false;
            }

            approach.Normalize();
            if (!TryBuildMantlePath(approach, out var landingForward))
            {
                return false;
            }

            mantling = true;
            climbing = false;
            gliding = false;
            guarding = false;
            dodging = false;
            externalLaunchPending = false;
            externalLaunchActive = false;
            jumpConsumed = true;
            glideFreshPressReady = false;
            jumpBufferedUntil = float.NegativeInfinity;
            verticalVelocity = 0f;
            lastMovementInput = Vector3.zero;
            lastTraversalVerticalInput = 0f;
            lastTraversalLateralInput = 0f;
            mantleStartedAt = Time.time;
            mantleProgress = 0f;
            mantleForward = landingForward;
            nextMantleTime = Time.time + MantleDuration + MantleCooldown;
            proceduralAnimator?.CancelAttack();
            SetGliderVisible(false);
            return true;
        }

        private bool TryBuildMantlePath(Vector3 approach, out Vector3 landingForward)
        {
            landingForward = Vector3.zero;
            var capsuleRadius = GetControllerWorldRadius();
            var chestHeight = Mathf.Clamp(
                controller.center.y + controller.height * 0.04f,
                0.82f,
                1.05f);
            var wallOrigin = transform.position + Vector3.up * chestHeight;
            if (!TryGetClosestSphereCastHit(
                    wallOrigin,
                    0.1f,
                    approach,
                    MantleWallReach,
                    out var wallHit))
            {
                return false;
            }

            var walkableY = GetWalkableNormalY();
            if (Mathf.Abs(wallHit.normal.y) > 0.32f ||
                Vector3.Dot(wallHit.normal, approach) > -0.35f)
            {
                return false;
            }

            landingForward = Vector3.ProjectOnPlane(-wallHit.normal, Vector3.up);
            if (landingForward.sqrMagnitude < 0.01f)
            {
                return false;
            }

            landingForward.Normalize();
            if (Vector3.Dot(landingForward, approach) < 0.45f)
            {
                return false;
            }

            var landingInset = capsuleRadius +
                               Mathf.Max(0.16f, controller.skinWidth + 0.08f);
            var landingProbe = wallHit.point + landingForward * landingInset;
            landingProbe.y = transform.position.y + MantleMaxHeight + 0.22f;
            var landingProbeDistance =
                MantleMaxHeight - MantleMinHeight + 0.48f;
            if (!TryGetClosestRaycastHit(
                    landingProbe,
                    Vector3.down,
                    landingProbeDistance,
                    out var landingHit) ||
                landingHit.normal.y < walkableY)
            {
                return false;
            }

            // The top must actually cap the chest-high wall, not be a floor behind it.
            if (landingHit.point.y <= wallOrigin.y + 0.025f)
            {
                return false;
            }

            var footOffset = GetControllerFootOffset();
            var landingClearance = Mathf.Max(0.035f, controller.skinWidth * 0.45f);
            var landing = landingHit.point - Vector3.up * footOffset;
            landing.y += landingClearance;
            var rise = landing.y - transform.position.y;
            if (rise < MantleMinHeight || rise > MantleMaxHeight)
            {
                return false;
            }

            var supportOffset = capsuleRadius * 0.55f;
            var right = Vector3.Cross(Vector3.up, landingForward);
            if (!HasMantleSupport(
                    landing,
                    landingForward * supportOffset,
                    landingHit.point.y) ||
                !HasMantleSupport(
                    landing,
                    -landingForward * supportOffset,
                    landingHit.point.y) ||
                !HasMantleSupport(
                    landing,
                    right * supportOffset,
                    landingHit.point.y) ||
                !HasMantleSupport(
                    landing,
                    -right * supportOffset,
                    landingHit.point.y))
            {
                return false;
            }

            mantleStart = transform.position;
            mantleLanding = landing;
            mantleSurfaceY = landingHit.point.y;
            mantleLift = mantleStart;
            mantleLift.y = mantleLanding.y + MantleArcClearance;
            mantleOver = mantleLanding + Vector3.up * MantleArcClearance;

            // Validate the full raised-then-forward arc so ceilings and wall caps block it.
            if (!IsMantleCapsuleClear(mantleLift) ||
                !IsMantleCapsuleClear(mantleOver) ||
                !IsMantleCapsuleClear(mantleLanding) ||
                !IsMantleSweepClear(mantleStart, mantleLift) ||
                !IsMantleSweepClear(mantleLift, mantleOver) ||
                !IsMantleSweepClear(mantleOver, mantleLanding))
            {
                return false;
            }

            return true;
        }

        private void UpdateMantle()
        {
            if (controller == null || !controller.enabled)
            {
                CancelMantle();
                return;
            }

            lastMovementInput = Vector3.zero;
            lastTraversalVerticalInput = 0f;
            lastTraversalLateralInput = 0f;
            mantleProgress = Mathf.Clamp01(
                (Time.time - mantleStartedAt) / MantleDuration);
            var desired = EvaluateMantlePosition(mantleProgress);
            if (!IsMantleSweepClear(transform.position, desired))
            {
                CancelMantle();
                return;
            }

            var targetRotation = Quaternion.LookRotation(mantleForward, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                1f - Mathf.Exp(-22f * Time.deltaTime));

            controller.Move(desired - transform.position);
            if ((transform.position - desired).sqrMagnitude > 0.0324f)
            {
                CancelMantle();
                return;
            }

            if (mantleProgress < 1f)
            {
                return;
            }

            if (!IsMantleLandingValid())
            {
                CancelMantle();
                return;
            }

            var settleDistance = Mathf.Max(
                0.14f,
                controller.stepOffset + controller.skinWidth + 0.05f);
            var settleFlags = controller.Move(Vector3.down * settleDistance);
            if ((settleFlags & CollisionFlags.Below) == 0 && !controller.isGrounded)
            {
                CancelMantle();
                return;
            }

            mantling = false;
            mantleProgress = 1f;
            verticalVelocity = GroundedVerticalVelocity;
            lastGroundedAt = Time.time;
            lastSafePosition = transform.position;
            hasSafePosition = true;
            nextMantleTime = Mathf.Max(nextMantleTime, Time.time + MantleCooldown);
        }

        private Vector3 EvaluateMantlePosition(float progress)
        {
            if (progress < 0.45f)
            {
                return Vector3.Lerp(
                    mantleStart,
                    mantleLift,
                    SmoothTraversal01(progress / 0.45f));
            }

            if (progress < 0.82f)
            {
                return Vector3.Lerp(
                    mantleLift,
                    mantleOver,
                    SmoothTraversal01((progress - 0.45f) / 0.37f));
            }

            return Vector3.Lerp(
                mantleOver,
                mantleLanding,
                SmoothTraversal01((progress - 0.82f) / 0.18f));
        }

        private bool IsMantleLandingValid()
        {
            if (!IsMantleCapsuleClear(mantleLanding))
            {
                return false;
            }

            var radius = GetControllerWorldRadius() * 0.55f;
            var right = Vector3.Cross(Vector3.up, mantleForward);
            return HasMantleSupport(
                       mantleLanding,
                       mantleForward * radius,
                       mantleSurfaceY) &&
                   HasMantleSupport(
                       mantleLanding,
                       -mantleForward * radius,
                       mantleSurfaceY) &&
                   HasMantleSupport(
                       mantleLanding,
                       right * radius,
                       mantleSurfaceY) &&
                   HasMantleSupport(
                       mantleLanding,
                       -right * radius,
                       mantleSurfaceY);
        }

        private bool HasMantleSupport(
            Vector3 rootPosition,
            Vector3 horizontalOffset,
            float expectedSurfaceY)
        {
            var probe = rootPosition + horizontalOffset;
            probe.y = expectedSurfaceY + 0.28f;
            if (!TryGetClosestRaycastHit(probe, Vector3.down, 0.64f, out var hit) ||
                hit.normal.y < GetWalkableNormalY())
            {
                return false;
            }

            return Mathf.Abs(hit.point.y - expectedSurfaceY) <=
                   Mathf.Max(0.2f, GetControllerWorldRadius() * 0.9f);
        }

        private bool IsMantleCapsuleClear(Vector3 rootPosition)
        {
            GetControllerCapsule(
                rootPosition,
                Mathf.Max(0.01f, controller.skinWidth * 0.25f),
                out var top,
                out var bottom,
                out var radius);
            var hitCount = Physics.OverlapCapsuleNonAlloc(
                top,
                bottom,
                radius,
                mantleOverlapBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            if (hitCount >= mantleOverlapBuffer.Length)
            {
                return false;
            }

            for (var index = 0; index < hitCount; index++)
            {
                if (!IsSelfCollider(mantleOverlapBuffer[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsMantleSweepClear(Vector3 start, Vector3 end)
        {
            var displacement = end - start;
            var distance = displacement.magnitude;
            if (distance <= 0.0001f)
            {
                return true;
            }

            GetControllerCapsule(
                start,
                Mathf.Max(0.02f, controller.skinWidth + 0.01f),
                out var top,
                out var bottom,
                out var radius);
            var hitCount = Physics.CapsuleCastNonAlloc(
                top,
                bottom,
                radius,
                displacement / distance,
                mantleHitBuffer,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            if (hitCount >= mantleHitBuffer.Length)
            {
                return false;
            }

            for (var index = 0; index < hitCount; index++)
            {
                var hit = mantleHitBuffer[index];
                if (!IsSelfCollider(hit.collider) && hit.distance < distance - 0.01f)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryGetClosestRaycastHit(
            Vector3 origin,
            Vector3 direction,
            float distance,
            out RaycastHit closestHit)
        {
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                mantleHitBuffer,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            return TrySelectClosestHit(hitCount, out closestHit);
        }

        private bool TryGetClosestSphereCastHit(
            Vector3 origin,
            float radius,
            Vector3 direction,
            float distance,
            out RaycastHit closestHit)
        {
            var hitCount = Physics.SphereCastNonAlloc(
                origin,
                radius,
                direction,
                mantleHitBuffer,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            return TrySelectClosestHit(hitCount, out closestHit);
        }

        private bool TrySelectClosestHit(int hitCount, out RaycastHit closestHit)
        {
            closestHit = default;
            if (hitCount >= mantleHitBuffer.Length)
            {
                return false;
            }

            var closestDistance = float.PositiveInfinity;
            var found = false;
            for (var index = 0; index < hitCount; index++)
            {
                var hit = mantleHitBuffer[index];
                if (IsSelfCollider(hit.collider) || hit.distance >= closestDistance)
                {
                    continue;
                }

                closestHit = hit;
                closestDistance = hit.distance;
                found = true;
            }

            return found;
        }

        private bool IsSelfCollider(Collider candidate)
        {
            return candidate != null &&
                   (candidate == controller ||
                    candidate.transform == transform ||
                    candidate.transform.IsChildOf(transform));
        }

        private void GetControllerCapsule(
            Vector3 rootPosition,
            float radiusInset,
            out Vector3 top,
            out Vector3 bottom,
            out float radius)
        {
            var scale = transform.lossyScale;
            var verticalScale = Mathf.Abs(scale.y);
            var horizontalScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            var fullRadius = controller.radius * horizontalScale;
            radius = Mathf.Max(0.03f, fullRadius - radiusInset);
            var height = Mathf.Max(controller.height * verticalScale, fullRadius * 2f);
            var centerOffset = transform.rotation *
                               Vector3.Scale(controller.center, scale);
            var center = rootPosition + centerOffset;
            var halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            top = center + Vector3.up * halfSegment;
            bottom = center - Vector3.up * halfSegment;
        }

        private float GetControllerWorldRadius()
        {
            var scale = transform.lossyScale;
            return controller.radius *
                   Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        }

        private float GetControllerFootOffset()
        {
            var scale = transform.lossyScale;
            var centerOffset = transform.rotation *
                               Vector3.Scale(controller.center, scale);
            return centerOffset.y - controller.height * Mathf.Abs(scale.y) * 0.5f;
        }

        private float GetWalkableNormalY()
        {
            return Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);
        }

        private void CancelMantle()
        {
            if (!mantling)
            {
                return;
            }

            mantling = false;
            mantleProgress = 0f;
            verticalVelocity = Mathf.Min(0f, verticalVelocity);
            nextMantleTime = Mathf.Max(nextMantleTime, Time.time + MantleCooldown);
        }

        private static float SmoothTraversal01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private bool HasLandingSurfaceBelow(float distance)
        {
            var origin = transform.position + Vector3.up * 0.15f;
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                mantleHitBuffer,
                Mathf.Max(0f, distance) + 0.15f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            var hasSurface = false;
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = mantleHitBuffer[hitIndex];
                if (hit.collider.GetComponentInParent<PlayerAvatar>() == this)
                {
                    continue;
                }

                if (hit.normal.y >= 0.45f)
                {
                    hasSurface = true;
                    break;
                }
            }

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                mantleHitBuffer[hitIndex] = default;
            }

            return hasSurface;
        }

        private void SetGliderVisible(bool visible)
        {
            if (gliderVisual == null && visible)
            {
                gliderVisual = new GameObject("Threadwing Glider").transform;
                gliderVisual.SetParent(visual, false);
                for (var index = 0; index < 2; index++)
                {
                    var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wing.transform.SetParent(gliderVisual, false);
                    wing.transform.localPosition = new Vector3(index == 0 ? -0.9f : 0.9f, 1.25f, 0.05f);
                    wing.transform.localScale = new Vector3(1.6f, 0.05f, 0.6f);
                    wing.transform.localRotation = Quaternion.Euler(0f, 0f, index == 0 ? 17f : -17f);
                    wing.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.2f, 0.67f, 0.61f), 0.12f, 0.45f);
                    Object.Destroy(wing.GetComponent<Collider>());
                }
            }

            if (gliderVisual != null)
            {
                gliderVisual.gameObject.SetActive(visible);
            }
        }

        private string WeaponAttackName()
        {
            return game.ActiveWeapon switch
            {
                WeaponDiscipline.Riftbow => "Riftbow bolt",
                WeaponDiscipline.Threadstaff => "Threadstaff resonance",
                _ => "Pathblade strike"
            };
        }

        private string MountedWeaponAttackName()
        {
            return game.ActiveWeapon switch
            {
                WeaponDiscipline.Riftbow => "Mounted Riftbow line",
                WeaponDiscipline.Threadstaff => "Mounted Threadstaff sweep",
                _ => "Mounted Pathblade charge"
            };
        }

        private float GetMountedDisciplineDamageMultiplier()
        {
            return game.ActiveWeapon switch
            {
                WeaponDiscipline.Riftbow => 0.92f,
                WeaponDiscipline.Pathblade => 1.08f,
                _ => 1f
            };
        }

        private OvertonePitch GetWeaponOvertonePitch()
        {
            return game.ActiveWeapon switch
            {
                WeaponDiscipline.Riftbow => OvertonePitch.Line,
                WeaponDiscipline.Threadstaff => OvertonePitch.Ring,
                _ => OvertonePitch.Cut
            };
        }

        public void RefreshWeaponVisual()
        {
            if (weaponVisual == null || game == null)
            {
                return;
            }

            ClearVisualChildren(weaponVisual);
            CombatVisualBuilder.DressWayfinderWeapon(game, weaponVisual, game.ActiveWeapon);
            UpdateWeaponVisibility();
        }

        public void RefreshEquipmentVisuals()
        {
            if (game == null)
            {
                return;
            }

            var coatId = game.GetEquippedFieldGearId(EquipmentSlot.Coat);
            var charmId = game.GetEquippedFieldGearId(EquipmentSlot.Charm);
            var toolId = game.GetEquippedFieldGearId(EquipmentSlot.Tool);
            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = coatId == "coat.mosscoat"
                    ? new Color(0.18f, 0.49f, 0.31f)
                    : baseCoatColor;
            }

            if (fieldGearVisual != null)
            {
                ClearVisualChildren(fieldGearVisual);
                CombatVisualBuilder.DressWayfinderFieldGear(game, fieldGearVisual, coatId, charmId, toolId);
            }

            RefreshWeaponVisual();
        }

        private void UpdateWeaponVisibility()
        {
            if (weaponVisual != null)
            {
                weaponVisual.gameObject.SetActive(!isMounted);
            }
        }

        private static void ClearVisualChildren(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                var child = root.GetChild(index);
                child.gameObject.SetActive(false);
                Object.Destroy(child.gameObject);
            }
        }

        private void CreateVisual()
        {
            visual = new GameObject("Wayfinder Visual").transform;
            visual.SetParent(transform, false);
            visual.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Wayfinder Coat";
            body.transform.SetParent(visual, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.72f, 0.98f, 0.72f);
            Object.Destroy(body.GetComponent<Collider>());
            bodyRenderer = body.GetComponent<Renderer>();
            baseCoatColor = new Color(0.15f, 0.41f, 0.43f);
            bodyRenderer.material = game.CreateMaterial(baseCoatColor, 0.05f, 0.42f);

            var frame = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            frame.name = "Threadglass";
            frame.transform.SetParent(visual, false);
            frame.transform.localPosition = new Vector3(0.27f, 1.22f, -0.12f);
            frame.transform.localScale = new Vector3(0.12f, 0.45f, 0.12f);
            Object.Destroy(frame.GetComponent<Collider>());
            frame.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.82f, 0.5f, 0.24f), 0.5f, 0.6f);
            CombatVisualBuilder.DressWayfinder(game, visual);
            TetherfallWayfinderArtOverhaul.Attach(game, visual);
            weaponVisual = new GameObject("Wayfinder weapon loadout").transform;
            weaponVisual.SetParent(visual, false);
            fieldGearVisual = new GameObject("Wayfinder field gear").transform;
            fieldGearVisual.SetParent(visual, false);
            RefreshEquipmentVisuals();
            proceduralAnimator = TetherfallProceduralAnimation.AttachWayfinder(game, visual);
        }

        private void CreateAttackPulse(Vector3 position, float scale)
        {
            TimedPulse.Spawn(
                PrimitiveType.Sphere,
                "Threadglass Strike",
                position,
                Vector3.one * 0.25f,
                RuntimeEffectMaterialCache.GetTransparent(game, new Color(0.42f, 0.9f, 0.78f, 0.72f), 0.72f),
                0.22f,
                scale);
        }
    }

    public sealed class ThreadglassProjectile : MonoBehaviour
    {
        private readonly RaycastHit[] hitBuffer = new RaycastHit[16];
        private TetherfallGame game;
        private Vector3 direction;
        private int damage;
        private float expiresAt;
        private OvertonePitch pitch;
        private bool resonancePulse;
        private int crossweaveRelayToken;

        public static ThreadglassProjectile Create(
            TetherfallGame game,
            Vector3 position,
            Vector3 direction,
            int damage,
            OvertonePitch overtonePitch = OvertonePitch.None,
            bool addResonancePulse = false,
            int relayToken = 0)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "Riftbow Threadglass Bolt";
            root.transform.position = position;
            root.transform.localScale = Vector3.one * 0.24f;
            Object.Destroy(root.GetComponent<Collider>());
            root.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.48f, 0.94f, 0.86f), 0.24f, 0.74f);
            var projectile = root.AddComponent<ThreadglassProjectile>();
            projectile.game = game;
            projectile.direction = direction.normalized;
            projectile.damage = damage;
            projectile.expiresAt = Time.time + 2.1f;
            projectile.pitch = overtonePitch;
            projectile.resonancePulse = addResonancePulse;
            projectile.crossweaveRelayToken = relayToken;
            return projectile;
        }

        private void Update()
        {
            if (game == null)
            {
                Destroy(gameObject);
                return;
            }

            if (game.GameplayPaused)
            {
                expiresAt += Time.deltaTime;
                return;
            }

            var start = transform.position;
            var distance = 28f * Time.deltaTime;
            Collider closestCollider = null;
            var closestDistance = float.PositiveInfinity;
            var hitCount = Physics.SphereCastNonAlloc(
                start,
                0.13f,
                direction,
                hitBuffer,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = hitBuffer[hitIndex];
                if (hit.collider.GetComponentInParent<PlayerAvatar>() != null)
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestCollider = hit.collider;
                }
            }

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                hitBuffer[hitIndex] = default;
            }

            if (closestCollider != null)
            {
                var enemy = closestCollider.GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage, "Riftbow bolt");
                    game.RegisterOvertone(enemy, pitch, "Riftbow bolt");
                    game.TryResolveCrossweave(enemy, crossweaveRelayToken);
                    if (resonancePulse)
                    {
                        game.RegisterOvertone(enemy, OvertonePitch.Pulse, "Riftbow triad");
                    }

                    Destroy(gameObject);
                    return;
                }

                var boss = closestCollider.GetComponentInParent<DungeonBoss>();
                if (boss != null)
                {
                    boss.TakeDamage(damage, "Riftbow bolt");
                    game.RegisterOvertone(boss, pitch, "Riftbow bolt");
                    game.TryResolveCrossweave(boss, crossweaveRelayToken);
                    if (resonancePulse)
                    {
                        game.RegisterOvertone(boss, OvertonePitch.Pulse, "Riftbow triad");
                    }

                    Destroy(gameObject);
                    return;
                }

                Destroy(gameObject);
                return;
            }

            transform.position += direction * distance;
            if (Time.time >= expiresAt)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        private const int ObstructionCapacity = 24;
        private const float ObstructionRadius = 0.28f;
        private const float MinimumDistance = 0.58f;
        private const float DefaultBackDistance = 7.4f;
        private const float DefaultHeight = 2.4f;
        private const float MountedBackDistance = 9.2f;
        private const float MountedHeight = 3.1f;

        private readonly RaycastHit[] obstructionHits = new RaycastHit[ObstructionCapacity];
        private Transform target;
        private Transform combatFocus;
        private PlayerAvatar player;
        private float yaw;
        private float pitch = 19f;
        private float currentDistance;
        private float focusWeight;
        private Vector3 lastFocusPosition;
        private float impulseStartedAt;
        private float impulseEndsAt;
        private float impulseMagnitude;

        public void Initialize(Transform newTarget)
        {
            target = newTarget;
            player = target != null ? target.GetComponent<PlayerAvatar>() : null;
            yaw = target != null ? target.eulerAngles.y : 0f;
            pitch = 19f;
            currentDistance = new Vector3(0f, DefaultHeight, -DefaultBackDistance).magnitude;
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            UpdateCameraPose(true);
        }

        public void AddImpulse(float magnitude, float duration)
        {
            magnitude = TetherfallAccessibilityRuntime.ScaleCameraShakeMagnitude(magnitude);
            if (magnitude <= 0f || duration <= 0f)
            {
                return;
            }

            var now = Time.time;
            var remainingDuration = Mathf.Max(0f, impulseEndsAt - now);
            var currentAmplitude = now < impulseEndsAt
                ? impulseMagnitude * (1f - Mathf.InverseLerp(impulseStartedAt, impulseEndsAt, now))
                : 0f;
            impulseMagnitude = Mathf.Max(currentAmplitude, magnitude);
            impulseStartedAt = now;
            impulseEndsAt = now + Mathf.Max(duration, remainingDuration);
        }

        public void SetCombatFocus(Transform focus)
        {
            combatFocus = focus;
            if (focus != null)
            {
                lastFocusPosition = focus.position + Vector3.up * 0.9f;
            }
        }

        private void LateUpdate()
        {
            if (target == null || TetherfallGame.Instance == null)
            {
                return;
            }

            if (TetherfallGame.Instance.GameplayPaused)
            {
                if (impulseEndsAt > Time.time)
                {
                    impulseStartedAt += Time.deltaTime;
                    impulseEndsAt += Time.deltaTime;
                }

                return;
            }

            UpdateCameraPose(false);
        }

        private void UpdateCameraPose(bool snap)
        {
            var lookX = 0f;
            var lookY = 0f;
            if (!snap && Cursor.lockState == CursorLockMode.Locked)
            {
                var lookInput = TetherfallInputBindings.Shared.GetLook();
                lookX = lookInput.x;
                lookY = lookInput.y;
                yaw += lookX * 3.5f;
                pitch -= lookY * 2.8f;
            }

            var climbing = player != null && player.IsClimbing;
            var swimming = player != null && player.IsSwimming;
            var mounted = player != null && player.IsMounted;
            pitch = Mathf.Clamp(
                pitch,
                climbing ? 2f : swimming ? -4f : -8f,
                climbing ? 58f : swimming ? 44f : 54f);

            var deltaTime = snap ? 1f : Time.deltaTime;
            var focusPresent = combatFocus != null;
            focusWeight = snap
                ? (focusPresent ? 1f : 0f)
                : Mathf.MoveTowards(focusWeight, focusPresent ? 1f : 0f, deltaTime * 5.5f);
            if (focusPresent)
            {
                lastFocusPosition = combatFocus.position + Vector3.up * 0.9f;
                if (!snap &&
                    Mathf.Abs(lookX) + Mathf.Abs(lookY) < 0.01f &&
                    focusWeight > 0.05f)
                {
                    var focusDirection = lastFocusPosition - target.position;
                    focusDirection.y = 0f;
                    if (focusDirection.sqrMagnitude > 0.01f)
                    {
                        var desiredYaw = Mathf.Atan2(focusDirection.x, focusDirection.z) * Mathf.Rad2Deg;
                        yaw = Mathf.MoveTowardsAngle(yaw, desiredYaw, 46f * focusWeight * deltaTime);
                    }
                }
            }

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var targetPivot = target.position + Vector3.up * (mounted ? 1.6f : swimming ? 1.45f : 1.15f);
            var pivot = focusWeight > 0.001f
                ? Vector3.Lerp(targetPivot, lastFocusPosition, focusWeight * 0.48f)
                : targetPivot;
            var backDistance = mounted
                ? MountedBackDistance
                : climbing
                    ? 6.2f
                    : swimming
                        ? 6.5f
                        : DefaultBackDistance;
            var cameraHeight = mounted
                ? MountedHeight
                : climbing
                    ? 2.85f
                    : swimming
                        ? 2.7f
                        : DefaultHeight;
            var desired = pivot + rotation * new Vector3(0f, cameraHeight, -backDistance);
            var toDesired = desired - pivot;
            var distance = toDesired.magnitude;
            if (distance > 0.01f)
            {
                var direction = toDesired / distance;
                var closestDistance = distance;
                var hitCount = Physics.SphereCastNonAlloc(
                    pivot,
                    ObstructionRadius,
                    direction,
                    obstructionHits,
                    distance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);
                for (var index = 0; index < hitCount; index++)
                {
                    var hit = obstructionHits[index];
                    if (hit.collider == null || IsIgnoredCameraCollider(hit.collider))
                    {
                        continue;
                    }

                    closestDistance = Mathf.Min(closestDistance, hit.distance);
                }

                var collisionDistance = closestDistance < distance - 0.001f
                    ? Mathf.Clamp(closestDistance - 0.24f, MinimumDistance, distance)
                    : distance;
                currentDistance = snap
                    ? collisionDistance
                    : Mathf.MoveTowards(
                        currentDistance <= 0f ? distance : currentDistance,
                        collisionDistance,
                        (collisionDistance < currentDistance ? 28f : 7.5f) * deltaTime);
                desired = pivot + direction * currentDistance;
            }

            transform.position = snap
                ? desired
                : Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-13f * deltaTime));
            if (Time.time < impulseEndsAt)
            {
                var remaining = Mathf.InverseLerp(impulseStartedAt, impulseEndsAt, Time.time);
                var fade = 1f - remaining;
                var time = Time.time * 42f;
                var shake = new Vector3(
                    Mathf.PerlinNoise(time, 0.37f) - 0.5f,
                    Mathf.PerlinNoise(0.73f, time) - 0.5f,
                    0f) * impulseMagnitude * fade;
                transform.position += transform.right * shake.x + transform.up * shake.y;
            }
            else
            {
                impulseMagnitude = 0f;
            }

            var lookDirection = pivot - transform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                var desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                transform.rotation = snap
                    ? desiredRotation
                    : Quaternion.Slerp(
                        transform.rotation,
                        desiredRotation,
                        1f - Mathf.Exp(-18f * deltaTime));
            }
        }

        private static bool IsIgnoredCameraCollider(Collider collider)
        {
            return collider.GetComponentInParent<PlayerAvatar>() != null ||
                   collider.GetComponentInParent<PactCreature>() != null ||
                   collider.GetComponentInParent<HumanCompanion>() != null ||
                   collider.GetComponentInParent<Enemy>() != null ||
                   collider.GetComponentInParent<DungeonBoss>() != null;
        }
    }

    public sealed class PactCreature : MonoBehaviour, IWorldInteractable
    {
        private readonly Collider[] tacticalHitBuffer = new Collider[64];
        private readonly HashSet<Enemy> tacticalStruckEnemies = new HashSet<Enemy>();
        private readonly ExpeditionFollowerMotor followerMotor = new ExpeditionFollowerMotor();
        private TetherfallGame game;
        private Renderer bodyRenderer;
        private float nextAssist;
        private Transform mountTarget;
        private bool mounted;
        private bool requiresHelp;
        private bool helpCompleted;
        private GameObject rootKnot;
        private GameObject activeSupportMarker;
        private Vector3 baseScale;
        private Vector3 originalBaseScale;
        private float strikeVisualUntil;
        private PactProceduralAnimator proceduralAnimator;
        private string rosterInstanceId = string.Empty;
        private int fieldSlot = -1;
        private float assistStagger;
        private bool fieldActive;

        public string PactId { get; private set; }
        public string CreatureName { get; private set; }
        public bool IsMount { get; private set; }
        public bool IsPacted { get; private set; }
        public bool IsFieldActive => fieldActive && IsPacted && !IsMount;
        public int FieldSlot => fieldSlot;
        public string RosterInstanceId => rosterInstanceId;
        public string UnfoldedFormId { get; private set; } = string.Empty;
        public string TacticalAbilityShortLabel
        {
            get
            {
                var baseLabel = CreatureName == "Cinderwing"
                    ? "Flare"
                    : CreatureName == "Mistling"
                        ? "Veil"
                        : "Snare";
                return game != null
                    ? game.GetPactTacticalLabel(this, baseLabel)
                    : baseLabel;
            }
        }

        public bool CanAnswerTacticalCommand()
        {
            if (!IsPacted ||
                IsMount ||
                !fieldActive ||
                !isActiveAndEnabled ||
                game == null ||
                game.Player == null)
            {
                return false;
            }

            var rangeMultiplier = game.GetPactFormTacticalRangeMultiplier(this);
            return CreatureName == "Mistling" ||
                   game.HasTacticalAreaHostile(
                       game.Player.transform.position,
                       8.2f * rangeMultiplier,
                       9f * rangeMultiplier);
        }
        public string InteractionPrompt
        {
            get
            {
                if (IsPacted)
                {
                    return $"{CreatureName} is listening";
                }

                return requiresHelp && !helpCompleted
                    ? $"Untangle {CreatureName}'s root-knot"
                    : $"Accept {CreatureName}'s offered Pact";
            }
        }

        public static PactCreature Create(
            TetherfallGame game,
            string creatureName,
            Vector3 position,
            Color color,
            bool rideReady,
            string habitatNote,
            bool requiresHelp = false)
        {
            var root = GameObject.CreatePrimitive(rideReady ? PrimitiveType.Capsule : PrimitiveType.Sphere);
            root.name = creatureName;
            root.transform.position = position;
            root.transform.localScale = rideReady ? new Vector3(1.55f, 1.3f, 2.05f) : new Vector3(1.15f, 0.88f, 1.35f);
            Object.Destroy(root.GetComponent<Collider>());

            var creature = root.AddComponent<PactCreature>();
            var definition = TetherfallPactCatalog.FindByCreatureName(creatureName);
            creature.game = game;
            creature.PactId = definition?.id ?? $"pact.legacy.{creatureName.ToLowerInvariant()}";
            creature.CreatureName = creatureName;
            creature.IsMount = definition?.rideReady ?? rideReady;
            creature.assistStagger = StableAssistStagger(creature.PactId);
            creature.requiresHelp = requiresHelp;
            creature.baseScale = root.transform.localScale;
            creature.originalBaseScale = root.transform.localScale;
            creature.bodyRenderer = root.GetComponent<Renderer>();
            creature.bodyRenderer.material = game.CreateMaterial(color, 0.05f, 0.36f);
            game.RegisterInteractable(creature);
            game.RegisterPactCreature(creature);

            var seam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seam.name = "Resonance Seam";
            seam.transform.SetParent(root.transform, false);
            seam.transform.localPosition = new Vector3(0f, rideReady ? 0.2f : 0.1f, -0.1f);
            seam.transform.localScale = new Vector3(0.09f, rideReady ? 1.4f : 1.05f, 0.09f);
            Object.Destroy(seam.GetComponent<Collider>());
            seam.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.35f, 0.92f, 0.78f), 0.35f, 0.8f);
            CombatVisualBuilder.DressPact(game, root.transform, creatureName, rideReady, color);
            TetherfallEchoswornArtOverhaul.Build(root.transform, creatureName);
            creature.proceduralAnimator = TetherfallProceduralAnimation.AttachPact(game, root.transform);

            WorldVisuals.AddLabel(root.transform, creatureName, new Vector3(0f, rideReady ? 1.65f : 1.25f, 0f), new Color(0.82f, 0.96f, 0.88f));
            WorldVisuals.AddLabel(root.transform, habitatNote, new Vector3(0f, rideReady ? 1.3f : 0.95f, 0f), new Color(0.62f, 0.79f, 0.76f), 0.08f);
            if (requiresHelp)
            {
                creature.CreateRootKnot();
            }

            return creature;
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            if (game.GameplayPaused)
            {
                nextAssist += Time.deltaTime;
                return;
            }

            transform.Rotate(0f, IsPacted ? 20f * Time.deltaTime : 9f * Time.deltaTime, 0f, Space.World);
            UpdateCombatVisual();
            UpdateProceduralAnimation();
            if (mounted && mountTarget != null)
            {
                transform.position = mountTarget.position + mountTarget.forward * 0.12f + Vector3.down * 0.62f;
                transform.rotation = mountTarget.rotation;
                return;
            }

            if (!IsPacted || game.Player == null)
            {
                return;
            }

            var playerTransform = game.Player.transform;
            if (!IsMount && !fieldActive)
            {
                followerMotor.ResetMotion();
                return;
            }

            var target = IsMount
                ? playerTransform.position -
                  playerTransform.right * 2.1f -
                  playerTransform.forward * 2.5f
                : game.GetPactFormationPosition(fieldSlot);
            if (!IsMount &&
                game.IsInsidePactSafeSettlement(playerTransform.position) &&
                (target - transform.position).sqrMagnitude >
                CapturedPactActor.SafeSettlementNoWarpDistance *
                CapturedPactActor.SafeSettlementNoWarpDistance)
            {
                followerMotor.ResetMotion();
                return;
            }

            var airborneSpecies =
                CreatureName == "Cinderwing" ||
                CreatureName == "Mistling";
            var followProfile = IsMount
                ? ExpeditionFollowerMotorProfile.MountedPact
                : airborneSpecies
                    ? ExpeditionFollowerMotorProfile.FlyingPact
                    : ExpeditionFollowerMotorProfile.GroundedPact;
            followerMotor.Move(
                transform,
                target,
                playerTransform.position.y,
                Time.deltaTime,
                false,
                followProfile);
            var direction = followerMotor.Velocity;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    Time.deltaTime * 7f);
            }

            if (!IsMount && fieldActive && Time.time >= nextAssist)
            {
                if (CreatureName == "Mistling" && game.Health < game.MaxHealth - 10)
                {
                    game.HealPlayer(ScalePactPotency(
                        12 + game.Level,
                        true));
                    game.Notify("Mistling releases a cooling restorative veil.");
                    nextAssist = Time.time + 6.2f + assistStagger;
                    return;
                }

                var enemy = game.GetExpeditionEnemy(8.5f);
                if (enemy != null)
                {
                    var damage = CreatureName == "Cinderwing" ? 17 + game.Level * 2 : 12 + game.Level * 2;
                    damage = ScalePactPotency(
                        Mathf.RoundToInt(damage * game.ExpeditionDamageMultiplier),
                        true);
                    TriggerCombatVisual(enemy.transform);
                    enemy.TakeDamage(
                        damage,
                        $"{CreatureName} resonance",
                        CombatImpactKind.PassiveAlly);
                    game.OnExpeditionAllyStrike(enemy, transform, $"{CreatureName} resonance", GetCombatAccent());
                    game.RegisterPactAllyOvertone(
                        enemy,
                        GetRosterAffinity(),
                        $"{CreatureName} resonance");
                    nextAssist = Time.time + 2.35f + assistStagger;
                    return;
                }

                var boss = game.GetExpeditionBoss(10f);
                if (boss != null)
                {
                    var damage = CreatureName == "Cinderwing" ? 20 + game.Level * 2 : 13 + game.Level * 2;
                    damage = ScalePactPotency(
                        Mathf.RoundToInt(damage * game.ExpeditionDamageMultiplier),
                        true);
                    TriggerCombatVisual(boss.transform);
                    boss.TakeDamage(
                        damage,
                        $"{CreatureName} resonance",
                        CombatImpactKind.PassiveAlly);
                    game.OnExpeditionAllyStrike(boss, transform, $"{CreatureName} resonance", GetCombatAccent());
                    game.RegisterPactAllyOvertone(
                        boss,
                        GetRosterAffinity(),
                        $"{CreatureName} resonance");
                    nextAssist = Time.time + 2.6f + assistStagger;
                }
            }
        }

        public bool TriggerTacticalCommand()
        {
            return TriggerTacticalCommand(true);
        }

        internal bool TriggerTacticalCommand(bool allowAreaEffect)
        {
            if (!IsPacted ||
                IsMount ||
                !fieldActive ||
                !isActiveAndEnabled ||
                game == null ||
                game.Player == null)
            {
                return false;
            }

            var center = game.Player.transform.position;
            var tacticalPotency = game.GetPactFormTacticalPotencyMultiplier(this);
            var tacticalRange = game.GetPactFormTacticalRangeMultiplier(this);
            var tacticalStagger =
                game.GetPactFormTacticalStaggerMultiplier(this) *
                game.PactConcordMultiplier;
            if (CreatureName == "Mistling")
            {
                TriggerCombatVisual(game.Player.transform);
                game.HealPlayer(ScalePactPotency(
                    Mathf.RoundToInt((28 + game.Level * 2) * tacticalPotency),
                    true));
                game.RestoreStamina(
                    32f *
                    tacticalPotency *
                    game.PactConcordMultiplier);
                TimedPulse.Spawn(
                    PrimitiveType.Sphere,
                    "Mistling restorative veil",
                    transform.position + Vector3.up * 0.9f,
                    Vector3.one * 0.28f,
                    RuntimeEffectMaterialCache.GetTransparent(game, new Color(0.43f, 0.85f, 0.96f, 0.56f), 0.72f),
                    0.32f,
                    5.2f);
                game.RecordPactTacticalResponse(this);
                return true;
            }

            if (!allowAreaEffect)
            {
                return TriggerSingleTargetTacticalCommand(
                    tacticalPotency,
                    tacticalStagger);
            }

            var affected = 0;
            tacticalStruckEnemies.Clear();
            var hitCount = Physics.OverlapSphereNonAlloc(
                center,
                8.2f * tacticalRange,
                tacticalHitBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = tacticalHitBuffer[hitIndex];
                if (hit == null)
                {
                    continue;
                }

                var enemy = hit.GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDefeated || !tacticalStruckEnemies.Add(enemy))
                {
                    continue;
                }

                if (CreatureName == "Cinderwing")
                {
                    TriggerCombatVisual(enemy.transform);
                    enemy.TakeDamage(
                        ScalePactPotency(
                            Mathf.RoundToInt(
                                (25 + game.Level * 2) *
                                game.ExpeditionDamageMultiplier *
                                tacticalPotency),
                            true),
                        "Cinderwing flare");
                    enemy.ApplyStagger(0.46f * tacticalStagger);
                }
                else
                {
                    TriggerCombatVisual(enemy.transform);
                    enemy.ApplyStagger(1.18f * tacticalStagger);
                }

                game.OnExpeditionAllyStrike(enemy, transform, $"{CreatureName} tactical response", GetCombatAccent());
                affected++;
            }

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                tacticalHitBuffer[hitIndex] = null;
            }

            tacticalStruckEnemies.Clear();
            var boss = game.ActiveDungeonBoss;
            if (boss != null &&
                !boss.IsDefeated &&
                Vector3.Distance(center, boss.transform.position) <= 9f * tacticalRange)
            {
                TriggerCombatVisual(boss.transform);
                if (CreatureName == "Cinderwing")
                {
                    boss.TakeDamage(
                        ScalePactPotency(
                            Mathf.RoundToInt(
                                (31 + game.Level * 2) *
                                game.ExpeditionDamageMultiplier *
                                tacticalPotency),
                            true),
                        "Cinderwing flare");
                }

                boss.ApplyStagger(
                    (CreatureName == "Cinderwing" ? 0.32f : 0.7f) *
                    tacticalStagger);
                game.OnExpeditionAllyStrike(boss, transform, $"{CreatureName} tactical response", GetCombatAccent());
                affected++;
            }

            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                $"{CreatureName} tactical pulse",
                transform.position + Vector3.up * 0.06f,
                new Vector3(0.34f, 0.035f, 0.34f),
                RuntimeEffectMaterialCache.GetTransparent(
                    game,
                    CreatureName == "Cinderwing"
                        ? new Color(0.96f, 0.4f, 0.21f, 0.6f)
                        : new Color(0.32f, 0.86f, 0.58f, 0.58f),
                    0.66f),
                0.3f,
                8.6f * tacticalRange);
            if (affected <= 0)
            {
                return false;
            }

            game.RecordPactTacticalResponse(this);
            return true;
        }

        private bool TriggerSingleTargetTacticalCommand(
            float tacticalPotency,
            float tacticalStagger)
        {
            var enemy = game.GetExpeditionEnemy(16f);
            if (enemy != null)
            {
                TriggerCombatVisual(enemy.transform);
                if (CreatureName == "Cinderwing")
                {
                    enemy.TakeDamage(
                        ScalePactPotency(
                            Mathf.RoundToInt(
                                (19 + game.Level * 2) *
                                game.ExpeditionDamageMultiplier *
                                tacticalPotency),
                            true),
                        "Cinderwing focused flare");
                    enemy.ApplyStagger(0.34f * tacticalStagger);
                }
                else
                {
                    enemy.ApplyStagger(0.78f * tacticalStagger);
                }

                game.OnExpeditionAllyStrike(
                    enemy,
                    transform,
                    $"{CreatureName} focused tactical response",
                    GetCombatAccent());
                game.RecordPactTacticalResponse(this);
                return true;
            }

            var boss = game.GetExpeditionBoss(18f);
            if (boss == null)
            {
                return false;
            }

            TriggerCombatVisual(boss.transform);
            if (CreatureName == "Cinderwing")
            {
                boss.TakeDamage(
                    ScalePactPotency(
                        Mathf.RoundToInt(
                            (22 + game.Level * 2) *
                            game.ExpeditionDamageMultiplier *
                            tacticalPotency),
                        true),
                    "Cinderwing focused flare");
                boss.ApplyStagger(0.2f * tacticalStagger);
            }
            else
            {
                boss.ApplyStagger(0.42f * tacticalStagger);
            }

            game.OnExpeditionAllyStrike(
                boss,
                transform,
                $"{CreatureName} focused tactical response",
                GetCombatAccent());
            game.RecordPactTacticalResponse(this);
            return true;
        }

        public bool TriggerCrossweaveResponse(Enemy target)
        {
            if (!IsPacted ||
                IsMount ||
                !fieldActive ||
                !isActiveAndEnabled ||
                game == null ||
                target == null ||
                target.IsDefeated)
            {
                return false;
            }

            TriggerCombatVisual(target.transform);
            if (CreatureName == "Cinderwing")
            {
                target.TakeDamage(ScalePactPotency(10 + game.Level), "Cinderwing Crossweave flare");
                target.ApplyExpose(4.6f);
            }
            else if (CreatureName == "Mistling")
            {
                game.HealPlayer(ScalePactPotency(8 + game.Level));
                game.RestoreStamina(12f);
            }
            else
            {
                target.TakeDamage(ScalePactPotency(6 + game.Level), "Bramblet Crossweave root-call");
                target.ApplyStagger(0.56f);
            }

            game.OnExpeditionAllyStrike(
                target,
                transform,
                $"{CreatureName} Crossweave response",
                GetCombatAccent());
            game.RecordPactCrossweaveResponse(this);
            return true;
        }

        public bool TriggerCrossweaveResponse(DungeonBoss target)
        {
            if (!IsPacted ||
                IsMount ||
                !fieldActive ||
                !isActiveAndEnabled ||
                game == null ||
                target == null ||
                target.IsDefeated)
            {
                return false;
            }

            TriggerCombatVisual(target.transform);
            if (CreatureName == "Cinderwing")
            {
                target.TakeDamage(ScalePactPotency(11 + game.Level), "Cinderwing Crossweave flare");
                target.ApplyStagger(0.18f);
            }
            else if (CreatureName == "Mistling")
            {
                game.HealPlayer(ScalePactPotency(8 + game.Level));
                game.RestoreStamina(12f);
            }
            else
            {
                target.TakeDamage(ScalePactPotency(7 + game.Level), "Bramblet Crossweave root-call");
                target.ApplyStagger(0.3f);
            }

            game.OnExpeditionAllyStrike(
                target,
                transform,
                $"{CreatureName} Crossweave response",
                GetCombatAccent());
            game.RecordPactCrossweaveResponse(this);
            return true;
        }

        public bool TriggerThreadlinkResponse(Enemy target, int beat)
        {
            if (!IsPacted ||
                IsMount ||
                !fieldActive ||
                !isActiveAndEnabled ||
                game == null ||
                target == null ||
                target.IsDefeated)
            {
                return false;
            }

            TriggerCombatVisual(target.transform);
            if (CreatureName == "Cinderwing")
            {
                target.TakeDamage(ScalePactPotency(8 + game.Level), "Cinderwing Threadlink weave");
            }
            else if (CreatureName == "Mistling")
            {
                game.HealPlayer(ScalePactPotency(4 + game.Level));
                game.RestoreStamina(7f);
            }
            else
            {
                target.ApplyStagger(0.34f);
            }

            game.OnThreadlinkRallyResponse(
                target,
                transform,
                $"{CreatureName} Threadlink weave {beat}",
                GetCombatAccent());
            game.RecordPactThreadlinkResponse(this);
            return true;
        }

        public bool TriggerThreadlinkResponse(DungeonBoss target, int beat)
        {
            if (!IsPacted ||
                IsMount ||
                !fieldActive ||
                !isActiveAndEnabled ||
                game == null ||
                target == null ||
                target.IsDefeated)
            {
                return false;
            }

            TriggerCombatVisual(target.transform);
            if (CreatureName == "Cinderwing")
            {
                target.TakeDamage(ScalePactPotency(9 + game.Level), "Cinderwing Threadlink weave");
            }
            else if (CreatureName == "Mistling")
            {
                game.HealPlayer(ScalePactPotency(4 + game.Level));
                game.RestoreStamina(7f);
            }
            else
            {
                target.ApplyStagger(0.16f);
            }

            game.OnThreadlinkRallyResponse(
                target,
                transform,
                $"{CreatureName} Threadlink weave {beat}",
                GetCombatAccent());
            game.RecordPactThreadlinkResponse(this);
            return true;
        }

        private void TriggerCombatVisual(Transform target)
        {
            strikeVisualUntil = Time.time + 0.22f;
            proceduralAnimator?.TriggerStrike(
                CreatureName == "Cinderwing" ? 1.25f : 1f,
                0.08f,
                0.24f);
            if (target == null)
            {
                return;
            }

            var direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    0.72f);
            }
        }

        private void UpdateCombatVisual()
        {
            if (baseScale == Vector3.zero)
            {
                baseScale = transform.localScale;
            }

            var progress = Mathf.Clamp01((strikeVisualUntil - Time.time) / 0.22f);
            var pulse = 1f + Mathf.Sin(progress * Mathf.PI) * 0.1f;
            transform.localScale = new Vector3(
                baseScale.x * (1f + (pulse - 1f) * 1.25f),
                baseScale.y * (1f - (pulse - 1f) * 0.45f),
                baseScale.z * pulse);
        }

        private void UpdateProceduralAnimation()
        {
            if (proceduralAnimator == null)
            {
                return;
            }

            if (mounted)
            {
                proceduralAnimator.SetRiding(true, 0.8f);
                return;
            }

            var airborneSpecies = CreatureName == "Cinderwing" || CreatureName == "Mistling";
            proceduralAnimator.SetFlying(airborneSpecies && IsPacted, airborneSpecies ? 0.72f : 0f);
        }

        private int ScalePactPotency(
            int amount,
            bool applyConcord = false)
        {
            var concord = applyConcord && game != null
                ? game.PactConcordMultiplier
                : 1f;
            return Mathf.Max(
                1,
                Mathf.RoundToInt(
                    amount *
                    (game != null ? game.PactAssistPotencyMultiplier : 1f) *
                    (game != null ? game.GetPactFormAssistPotencyMultiplier(this) : 1f) *
                    concord));
        }

        private PactResonanceAffinity GetRosterAffinity()
        {
            var definition =
                TetherfallAccordSpeciesCatalog.FindByLegendaryPactId(PactId);
            return definition != null
                ? definition.resonanceAffinity
                : PactResonanceAffinity.Grove;
        }

        private Color GetCombatAccent()
        {
            return CreatureName == "Cinderwing"
                ? new Color(0.96f, 0.4f, 0.21f)
                : CreatureName == "Mistling"
                    ? new Color(0.43f, 0.85f, 0.96f)
                    : new Color(0.42f, 0.88f, 0.48f);
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return !IsPacted;
        }

        public void Interact(TetherfallGame owner)
        {
            if (IsPacted)
            {
                return;
            }

            if (requiresHelp && !helpCompleted)
            {
                helpCompleted = true;
                if (rootKnot != null)
                {
                    rootKnot.SetActive(false);
                }

                owner.Notify($"{CreatureName} is free. It offers a Pact without obligation.");
                return;
            }

            owner.FormPact(this);
        }

        public void BeginPact(TetherfallGame owner)
        {
            IsPacted = true;
            bodyRenderer.material.color = Color.Lerp(bodyRenderer.material.color, new Color(0.34f, 0.9f, 0.76f), 0.35f);
        }

        public bool ApplyUnfoldedForm(string formId)
        {
            var form = TetherfallPactCatalog.FindForm(formId);
            var definition = TetherfallPactCatalog.Find(PactId);
            if (form == null ||
                definition == null ||
                !definition.greaterForms.Contains(form))
            {
                return false;
            }

            if (originalBaseScale == Vector3.zero)
            {
                originalBaseScale = baseScale == Vector3.zero
                    ? transform.localScale
                    : baseScale;
            }

            baseScale = originalBaseScale * Mathf.Clamp(form.visualScale, 1f, 1.3f);
            transform.localScale = baseScale;
            var artRoot = TetherfallPactFormArt.Apply(game, transform, form);
            if (artRoot == null)
            {
                baseScale = originalBaseScale;
                transform.localScale = baseScale;
                return false;
            }

            UnfoldedFormId = form.id;
            proceduralAnimator?.RebindSockets();
            return true;
        }

        public void ConfigureFieldRosterState(
            string instanceId,
            int slot,
            bool active)
        {
            if (IsMount)
            {
                return;
            }

            var wasActive = fieldActive;
            rosterInstanceId = instanceId ?? string.Empty;
            fieldSlot = active
                ? Mathf.Clamp(
                    slot,
                    0,
                    PactRosterRules.AbsoluteMaximumFieldUnits - 1)
                : -1;
            fieldActive = active && IsPacted;

            if (fieldActive && !wasActive)
            {
                followerMotor.ResetMotion();
                nextAssist =
                    Time.time +
                    0.45f +
                    assistStagger +
                    fieldSlot * 0.08f;
            }

            if (!fieldActive)
            {
                followerMotor.ResetMotion();
                if (activeSupportMarker != null)
                {
                    activeSupportMarker.SetActive(false);
                }

                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void SetFieldSimulationActive(bool active)
        {
            if (IsMount || !fieldActive || gameObject.activeSelf == active)
            {
                return;
            }

            gameObject.SetActive(active);
            if (!active)
            {
                followerMotor.ResetMotion();
            }
        }

        public bool TryRecallToFormation(
            Vector3 formationPosition,
            bool immediate)
        {
            if (IsMount ||
                !fieldActive ||
                game == null ||
                game.PlayerTransform == null)
            {
                return false;
            }

            if (immediate)
            {
                transform.position = formationPosition;
                followerMotor.ResetMotion();
                return true;
            }

            var airborneSpecies =
                CreatureName == "Cinderwing" ||
                CreatureName == "Mistling";
            var profile = airborneSpecies
                ? ExpeditionFollowerMotorProfile.FlyingPact
                : ExpeditionFollowerMotorProfile.GroundedPact;
            var result = followerMotor.Move(
                transform,
                formationPosition,
                game.PlayerTransform.position.y,
                0.1f,
                false,
                profile);
            return result.Moved || result.Warped;
        }

        public void SetActiveSupport(bool isActive)
        {
            if (IsMount)
            {
                return;
            }

            if (isActive && IsPacted)
            {
                EnsureActiveSupportMarker();
            }

            if (activeSupportMarker != null)
            {
                activeSupportMarker.SetActive(isActive && IsPacted);
            }
        }

        public void SetMounted(bool shouldMount, Transform target)
        {
            if (mounted != shouldMount)
            {
                followerMotor.ResetMotion();
            }

            mounted = shouldMount;
            mountTarget = target;
        }

        private void EnsureActiveSupportMarker()
        {
            if (activeSupportMarker != null || game == null)
            {
                return;
            }

            activeSupportMarker = new GameObject("Active Pact sigil");
            activeSupportMarker.name = "Active Pact sigil";
            activeSupportMarker.transform.SetParent(transform, false);
            activeSupportMarker.transform.localPosition = new Vector3(0f, -0.46f, 0f);
            var material = game.CreateGlowMaterial(GetCombatAccent(), 1.15f, 0.5f);
            CreateMarkerPiece(
                activeSupportMarker.transform,
                PrimitiveType.Cylinder,
                "Pact sigil ring",
                Vector3.zero,
                new Vector3(0.88f, 0.018f, 0.88f),
                Quaternion.identity,
                material);
            for (var index = 0; index < 3; index++)
            {
                var angle = index * 120f * Mathf.Deg2Rad;
                CreateMarkerPiece(
                    activeSupportMarker.transform,
                    PrimitiveType.Cube,
                    "Pact sigil shard",
                    new Vector3(Mathf.Cos(angle) * 0.59f, 0.035f, Mathf.Sin(angle) * 0.59f),
                    new Vector3(0.1f, 0.026f, 0.2f),
                    Quaternion.Euler(0f, index * 120f, 18f),
                    material);
            }

            activeSupportMarker.AddComponent<ExpeditionFieldMarker>().Initialize(0.88f);
        }

        internal static void CreateMarkerPiece(
            Transform parent,
            PrimitiveType primitive,
            string pieceName,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var piece = GameObject.CreatePrimitive(primitive);
            piece.name = pieceName;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            piece.transform.localRotation = localRotation;
            Object.Destroy(piece.GetComponent<Collider>());
            piece.GetComponent<Renderer>().material = material;
        }

        private void CreateRootKnot()
        {
            rootKnot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rootKnot.name = "Braided Root-knot";
            rootKnot.transform.SetParent(transform, false);
            rootKnot.transform.localPosition = new Vector3(0.5f, -0.38f, 0.2f);
            rootKnot.transform.localRotation = Quaternion.Euler(16f, 25f, 28f);
            rootKnot.transform.localScale = new Vector3(1.15f, 0.23f, 0.8f);
            rootKnot.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.34f, 0.18f, 0.11f), 0f, 0.18f);
            Object.Destroy(rootKnot.GetComponent<Collider>());
            WorldVisuals.AddLabel(rootKnot.transform, "Root-knot", new Vector3(0f, 0.45f, 0f), new Color(0.91f, 0.68f, 0.46f), 0.07f);
        }

        private static float StableAssistStagger(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                if (value != null)
                {
                    for (var index = 0; index < value.Length; index++)
                    {
                        hash ^= value[index];
                        hash *= 16777619u;
                    }
                }

                return (hash % 7u) * 0.09f;
            }
        }
    }

    public sealed class HumanCompanion : MonoBehaviour, IWorldInteractable
    {
        private readonly Collider[] tacticalHitBuffer = new Collider[64];
        private readonly HashSet<Enemy> tacticalStruckEnemies = new HashSet<Enemy>();
        private readonly ExpeditionFollowerMotor followerMotor = new ExpeditionFollowerMotor();
        private TetherfallGame game;
        private Renderer bodyRenderer;
        private float nextAssist;
        private bool reserve;
        private int activeSlot;
        private GameObject fieldMarker;
        private Vector3 baseScale;
        private float strikeVisualUntil;
        private HumanProceduralAnimator proceduralAnimator;

        public string CompanionName { get; private set; }
        public string Role { get; private set; }
        public bool IsRecruited { get; private set; }
        public bool IsReserve => reserve;
        public string TacticalAbilityShortLabel => Role.Contains("medic")
            ? "Aid"
            : Role.Contains("scout")
                ? "Mark"
                : "Breach";

        public bool CanAnswerTacticalCommand()
        {
            if (!IsRecruited || reserve || game == null || game.Player == null)
            {
                return false;
            }

            if (Role.Contains("medic"))
            {
                return true;
            }

            return Role.Contains("scout")
                ? game.HasTacticalFocusHostile(16f)
                : game.HasTacticalAreaHostile(game.Player.transform.position, 7.4f, 8f);
        }
        public string InteractionPrompt => IsRecruited ? $"{CompanionName} is in your expedition" : $"Recruit {CompanionName}";

        public static HumanCompanion Create(TetherfallGame game, string name, string role, Vector3 position, Color color)
        {
            var root = new GameObject(name);
            root.name = name;
            root.transform.position = position;
            var companion = root.AddComponent<HumanCompanion>();
            companion.game = game;
            companion.CompanionName = name;
            companion.Role = role;
            companion.baseScale = root.transform.localScale;

            var visualRoot = new GameObject("Human Visual").transform;
            visualRoot.SetParent(root.transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Companion body";
            body.transform.SetParent(visualRoot, false);
            body.transform.localScale = new Vector3(0.82f, 1.08f, 0.82f);
            Object.Destroy(body.GetComponent<Collider>());
            companion.bodyRenderer = body.GetComponent<Renderer>();
            companion.bodyRenderer.material = game.CreateMaterial(color, 0.02f, 0.38f);
            CombatVisualBuilder.DressCompanion(game, visualRoot, role, color);
            companion.proceduralAnimator = TetherfallHumanAnimation.Attach(game, visualRoot);
            companion.proceduralAnimator?.SetIdleVariant(
                role.Contains("medic")
                    ? HumanIdleVariant.Working
                    : role.Contains("breaker")
                        ? HumanIdleVariant.Alert
                        : HumanIdleVariant.Relaxed);
            game.RegisterInteractable(companion);
            game.RegisterCompanion(companion);
            WorldVisuals.AddLabel(root.transform, name, new Vector3(0f, 1.46f, 0f), new Color(0.94f, 0.88f, 0.73f));
            WorldVisuals.AddLabel(root.transform, role, new Vector3(0f, 1.22f, 0f), new Color(0.61f, 0.8f, 0.8f), 0.08f);
            return companion;
        }

        private void Update()
        {
            if (!IsRecruited || game == null || game.Player == null || reserve)
            {
                return;
            }

            if (game.GameplayPaused)
            {
                nextAssist += Time.deltaTime;
                return;
            }

            var playerTransform = game.Player.transform;
            var offset = playerTransform.right * (activeSlot == 0 ? 1.45f : -1.45f) -
                         playerTransform.forward * 1.5f;
            var target = playerTransform.position + offset;
            var followResult = followerMotor.Move(
                transform,
                target,
                playerTransform.position.y,
                Time.deltaTime,
                false,
                ExpeditionFollowerMotorProfile.Human);
            var followVelocity = followerMotor.Velocity;
            var followDirection = followVelocity;
            followDirection.y = 0f;
            if (followDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(followDirection),
                    1f - Mathf.Exp(-10f * Time.deltaTime));
            }

            if (followResult.Warped)
            {
                proceduralAnimator?.StopLocomotion();
            }
            else
            {
                var localVelocity = transform.InverseTransformDirection(followVelocity);
                proceduralAnimator?.SetLocomotion(
                    Mathf.Clamp(localVelocity.z / 5.5f, -1f, 1f),
                    Mathf.Clamp(localVelocity.x / 5.5f, -1f, 1f),
                    Mathf.Clamp01(followVelocity.magnitude / 5.5f));
            }

            UpdateCombatVisual();
            if (Role.Contains("medic") && game.Health < game.MaxHealth - 8 && Time.time >= nextAssist)
            {
                TriggerCombatVisual(game.Player.transform);
                game.HealPlayer(8 + game.Level * 2);
                game.Notify($"{CompanionName} applies field medicine.");
                nextAssist = Time.time + 6.5f;
                return;
            }

            var enemy = game.GetExpeditionEnemy(7.5f);
            if (enemy != null && Time.time >= nextAssist)
            {
                var damage = Role.Contains("breaker") ? 16 + game.Level * 2 : 9 + game.Level;
                damage = Mathf.RoundToInt(damage * game.ExpeditionDamageMultiplier);
                TriggerCombatVisual(enemy.transform);
                var source = GetAssistSource();
                enemy.TakeDamage(damage, source, CombatImpactKind.PassiveAlly);
                game.OnExpeditionAllyStrike(enemy, transform, source, GetCombatAccent());
                nextAssist = Time.time + (Role.Contains("scout") ? 2.15f : Role.Contains("breaker") ? 3.25f : 2.8f);
                return;
            }

            var boss = game.GetExpeditionBoss(9f);
            if (boss != null &&
                Time.time >= nextAssist)
            {
                var damage = Role.Contains("breaker") ? 18 + game.Level * 2 : 10 + game.Level;
                damage = Mathf.RoundToInt(damage * game.ExpeditionDamageMultiplier);
                TriggerCombatVisual(boss.transform);
                var source = GetAssistSource();
                boss.TakeDamage(damage, source, CombatImpactKind.PassiveAlly);
                game.OnExpeditionAllyStrike(boss, transform, source, GetCombatAccent());
                nextAssist = Time.time + (Role.Contains("scout") ? 2.15f : Role.Contains("breaker") ? 3.25f : 2.8f);
            }
        }

        public bool TriggerTacticalCommand()
        {
            if (!IsRecruited || reserve || game == null || game.Player == null)
            {
                return false;
            }

            var center = game.Player.transform.position;
            if (Role.Contains("medic"))
            {
                TriggerCombatVisual(game.Player.transform);
                game.HealPlayer(24 + game.Level * 2);
                game.RestoreStamina(24f);
                TimedPulse.Spawn(
                    PrimitiveType.Sphere,
                    "Medic field veil",
                    transform.position + Vector3.up,
                    Vector3.one * 0.24f,
                    RuntimeEffectMaterialCache.GetTransparent(game, new Color(0.43f, 0.83f, 0.94f, 0.5f), 0.7f),
                    0.3f,
                    4.6f);
                return true;
            }

            if (Role.Contains("scout"))
            {
                var target = game.GetExpeditionEnemy(16f);
                if (target != null)
                {
                    TriggerCombatVisual(target.transform);
                    target.ApplyExpose(8f);
                    game.OnExpeditionAllyStrike(target, transform, $"{CompanionName} focus mark", GetCombatAccent());
                    TimedPulse.Spawn(
                        PrimitiveType.Sphere,
                        "Scout focus mark",
                        target.transform.position + Vector3.up * 0.9f,
                        Vector3.one * 0.18f,
                        RuntimeEffectMaterialCache.GetTransparent(game, new Color(0.95f, 0.76f, 0.34f, 0.58f), 0.7f),
                        0.28f,
                        3.3f);
                    return true;
                }

                var boss = game.GetExpeditionBoss(16f);
                if (boss == null)
                {
                    return false;
                }

                TriggerCombatVisual(boss.transform);
                boss.ApplyExpose(8f);
                game.OnExpeditionAllyStrike(boss, transform, $"{CompanionName} focus mark", GetCombatAccent());
                TimedPulse.Spawn(
                    PrimitiveType.Sphere,
                    "Scout focus mark",
                    boss.transform.position + Vector3.up * 1.5f,
                    Vector3.one * 0.18f,
                    RuntimeEffectMaterialCache.GetTransparent(game, new Color(0.95f, 0.76f, 0.34f, 0.58f), 0.7f),
                    0.28f,
                    3.3f);
                return true;
            }

            tacticalStruckEnemies.Clear();
            var hitCount = Physics.OverlapSphereNonAlloc(
                center,
                7.4f,
                tacticalHitBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = tacticalHitBuffer[hitIndex];
                if (hit == null)
                {
                    continue;
                }

                var enemy = hit.GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDefeated || !tacticalStruckEnemies.Add(enemy))
                {
                    continue;
                }

                TriggerCombatVisual(enemy.transform);
                enemy.TakeDamage(
                    Mathf.RoundToInt((22 + game.Level * 2) * game.ExpeditionDamageMultiplier),
                    $"{CompanionName} breach strike");
                enemy.ApplyStagger(0.95f);
                game.OnExpeditionAllyStrike(enemy, transform, $"{CompanionName} breach strike", GetCombatAccent());
            }

            var struckCount = tacticalStruckEnemies.Count;
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                tacticalHitBuffer[hitIndex] = null;
            }

            tacticalStruckEnemies.Clear();
            var nearbyBoss = game.GetExpeditionBoss(8f);
            var struckBoss = false;
            if (nearbyBoss != null &&
                Vector3.Distance(center, nearbyBoss.transform.position) <= 8f)
            {
                TriggerCombatVisual(nearbyBoss.transform);
                nearbyBoss.TakeDamage(
                    Mathf.RoundToInt((24 + game.Level * 2) * game.ExpeditionDamageMultiplier),
                    $"{CompanionName} breach strike");
                nearbyBoss.ApplyStagger(0.46f);
                game.OnExpeditionAllyStrike(nearbyBoss, transform, $"{CompanionName} breach strike", GetCombatAccent());
                struckBoss = true;
            }

            if (struckCount == 0 && !struckBoss)
            {
                return false;
            }

            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                "Breaker breach pulse",
                transform.position + Vector3.up * 0.05f,
                new Vector3(0.32f, 0.03f, 0.32f),
                RuntimeEffectMaterialCache.GetTransparent(game, new Color(0.93f, 0.61f, 0.32f, 0.58f), 0.65f),
                0.28f,
                7.8f);
            return true;
        }

        public bool TriggerCrossweaveResponse(Enemy target)
        {
            if (!IsRecruited || reserve || game == null || target == null || target.IsDefeated)
            {
                return false;
            }

            TriggerCombatVisual(target.transform);
            if (Role.Contains("breaker"))
            {
                target.TakeDamage(10 + game.Level, $"{CompanionName} Crossweave breach");
                target.ApplyStagger(0.6f);
            }
            else if (Role.Contains("scout"))
            {
                target.ApplyExpose(5.2f);
            }
            else
            {
                game.HealPlayer(12 + game.Level);
                game.RestoreStamina(16f);
            }

            game.OnExpeditionAllyStrike(
                target,
                transform,
                $"{CompanionName} {GetThreadlinkRoleName()} Crossweave response",
                GetCombatAccent());
            return true;
        }

        public bool TriggerCrossweaveResponse(DungeonBoss target)
        {
            if (!IsRecruited || reserve || game == null || target == null || target.IsDefeated)
            {
                return false;
            }

            TriggerCombatVisual(target.transform);
            if (Role.Contains("breaker"))
            {
                target.TakeDamage(11 + game.Level, $"{CompanionName} Crossweave breach");
                target.ApplyStagger(0.24f);
            }
            else if (Role.Contains("scout"))
            {
                target.ApplyExpose(5.2f);
            }
            else
            {
                game.HealPlayer(12 + game.Level);
                game.RestoreStamina(16f);
            }

            game.OnExpeditionAllyStrike(
                target,
                transform,
                $"{CompanionName} {GetThreadlinkRoleName()} Crossweave response",
                GetCombatAccent());
            return true;
        }

        public bool TriggerThreadlinkResponse(Enemy target, int beat)
        {
            if (!IsRecruited || reserve || game == null || target == null || target.IsDefeated)
            {
                return false;
            }

            TriggerCombatVisual(target.transform);
            if (Role.Contains("breaker"))
            {
                target.TakeDamage(7 + game.Level, $"{CompanionName} Threadlink breach");
                target.ApplyStagger(0.3f);
            }
            else if (Role.Contains("scout"))
            {
                target.ApplyExpose(2.8f);
            }
            else
            {
                game.HealPlayer(4 + game.Level);
                game.RestoreStamina(6f);
            }

            game.OnThreadlinkRallyResponse(
                target,
                transform,
                $"{CompanionName} {GetThreadlinkRoleName()} Threadlink weave {beat}",
                GetCombatAccent());
            return true;
        }

        public bool TriggerThreadlinkResponse(DungeonBoss target, int beat)
        {
            if (!IsRecruited || reserve || game == null || target == null || target.IsDefeated)
            {
                return false;
            }

            TriggerCombatVisual(target.transform);
            if (Role.Contains("breaker"))
            {
                target.TakeDamage(8 + game.Level, $"{CompanionName} Threadlink breach");
                target.ApplyStagger(0.13f);
            }
            else if (Role.Contains("scout"))
            {
                target.ApplyExpose(2.8f);
            }
            else
            {
                game.HealPlayer(4 + game.Level);
                game.RestoreStamina(6f);
            }

            game.OnThreadlinkRallyResponse(
                target,
                transform,
                $"{CompanionName} {GetThreadlinkRoleName()} Threadlink weave {beat}",
                GetCombatAccent());
            return true;
        }

        private void TriggerCombatVisual(Transform target)
        {
            strikeVisualUntil = Time.time + 0.2f;
            if (Role.Contains("medic"))
            {
                proceduralAnimator?.TriggerSupport(1f);
            }
            else
            {
                proceduralAnimator?.TriggerAttack(activeSlot % 2 != 0, Role.Contains("breaker") ? 1.2f : 0.92f);
            }

            if (target == null)
            {
                return;
            }

            var direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    0.7f);
            }
        }

        private void UpdateCombatVisual()
        {
            if (baseScale == Vector3.zero)
            {
                baseScale = transform.localScale;
            }

            var progress = Mathf.Clamp01((strikeVisualUntil - Time.time) / 0.2f);
            var pulse = Mathf.Sin(progress * Mathf.PI) * 0.09f;
            transform.localScale = new Vector3(
                baseScale.x * (1f + pulse),
                baseScale.y * (1f - pulse * 0.38f),
                baseScale.z * (1f + pulse * 0.72f));
        }

        private Color GetCombatAccent()
        {
            return Role.Contains("medic")
                ? new Color(0.43f, 0.86f, 0.78f)
                : Role.Contains("breaker")
                    ? new Color(0.94f, 0.44f, 0.24f)
                    : new Color(0.92f, 0.72f, 0.32f);
        }

        private string GetAssistSource()
        {
            return Role.Contains("scout")
                ? $"{CompanionName} scout assist"
                : Role.Contains("breaker")
                    ? $"{CompanionName} breaker assist"
                    : $"{CompanionName} medic assist";
        }

        private string GetThreadlinkRoleName()
        {
            return Role.Contains("scout")
                ? "scout"
                : Role.Contains("breaker")
                    ? "breaker"
                    : "medic";
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return !IsRecruited;
        }

        public void Interact(TetherfallGame owner)
        {
            proceduralAnimator?.TriggerQuestHandoff(0.9f);
            owner.Recruit(this);
        }

        public void JoinParty(TetherfallGame owner)
        {
            IsRecruited = true;
            proceduralAnimator?.TriggerTalk(1f);
            bodyRenderer.material.color = Color.Lerp(bodyRenderer.material.color, new Color(0.77f, 0.54f, 0.25f), 0.28f);
        }

        public void SetReserve(bool isReserve, int slot = -1)
        {
            if (reserve != isReserve)
            {
                followerMotor.ResetMotion();
            }

            reserve = isReserve;
            activeSlot = Mathf.Max(0, slot);
            gameObject.SetActive(!isReserve);
            if (!isReserve)
            {
                EnsureFieldMarker();
            }

            if (fieldMarker != null)
            {
                fieldMarker.SetActive(!isReserve);
            }
        }

        private void EnsureFieldMarker()
        {
            if (fieldMarker != null || game == null)
            {
                return;
            }

            fieldMarker = new GameObject("Field companion sigil");
            fieldMarker.name = "Field companion sigil";
            fieldMarker.transform.SetParent(transform, false);
            fieldMarker.transform.localPosition = new Vector3(0f, -0.78f, 0f);
            var material = game.CreateGlowMaterial(GetCombatAccent(), 1f, 0.46f);
            PactCreature.CreateMarkerPiece(
                fieldMarker.transform,
                PrimitiveType.Cylinder,
                "Field sigil ring",
                Vector3.zero,
                new Vector3(0.66f, 0.016f, 0.66f),
                Quaternion.identity,
                material);
            for (var index = 0; index < 2; index++)
            {
                PactCreature.CreateMarkerPiece(
                    fieldMarker.transform,
                    PrimitiveType.Cube,
                    "Field sigil shard",
                    new Vector3(index == 0 ? -0.44f : 0.44f, 0.026f, 0f),
                    new Vector3(0.16f, 0.024f, 0.075f),
                    Quaternion.Euler(0f, index * 180f, 16f),
                    material);
            }

            fieldMarker.AddComponent<ExpeditionFieldMarker>().Initialize(activeSlot * 0.9f + 0.25f);
        }
    }

    public sealed class ExpeditionFieldMarker : MonoBehaviour
    {
        private Vector3 baseScale;
        private float phase;
        private bool initialized;

        public void Initialize(float phaseOffset)
        {
            baseScale = transform.localScale;
            phase = phaseOffset;
            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            var pulse = 0.92f + Mathf.Sin(Time.time * 2.7f + phase) * 0.08f;
            transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y, baseScale.z * pulse);
            transform.Rotate(0f, 28f * Time.deltaTime, 0f, Space.Self);
        }
    }

    public sealed class Enemy : MonoBehaviour, IExpeditionDamageable
    {
        public const int MaximumActiveEnemyCopyCount = 128;

        private const int MaxSimultaneousAttackers = 3;
        private const float EnemyVoidRecoveryHeight = -14f;
        private static readonly List<Enemy> ActiveEnemies = new List<Enemy>();
        private static int activeAttackers;

        private readonly RaycastHit[] attackPathHitBuffer = new RaycastHit[24];
        private readonly RaycastHit[] groundTargetHitBuffer = new RaycastHit[16];
        private readonly Collider[] separationHitBuffer = new Collider[16];
        private TetherfallGame game;
        private EnemyCamp camp;
        private EnemyDefinition definition;
        private CombatPoiseProfile poiseProfile;
        private CharacterController movementController;
        private Renderer bodyRenderer;
        private Material bodyMaterial;
        private TetherfallHostileAnimation hostileAnimation;
        private CapturedPactArtRig wildPactArtRig;
        private Color baseColor;
        private Vector3 bodyBaseScale;
        private Vector3 bodyBasePosition;
        private float nextAttack;
        private float strikeAt;
        private float staggerUntil;
        private float exposedUntil;
        private float poisePressure;
        private float lastPoiseImpactAt = float.NegativeInfinity;
        private float verticalVelocity;
        private float visualImpactUntil;
        private float lastDamagedAt = float.NegativeInfinity;
        private int health;
        private int maxHealth;
        private int attackDamage;
        private int threatBand = 1;
        private bool telegraphing;
        private bool defeated;
        private bool holdsAttackToken;
        private bool exposeVisualActive;
        private int avoidanceSide;
        private GameObject activeTelegraph;
        private Transform ashguardVisual;
        private Vector3 spawnPosition;
        private bool ashguardActive;
        private float ashguardEndsAt;
        private bool threadbindEligibleSpawn;
        private System.Action<Enemy> onDefeated;
        private Vector3 groundBurstTarget;
        private bool hasGroundBurstTarget;

        public string EnemyId => definition != null ? definition.id : string.Empty;
        public string EnemyName { get; private set; }
        public EnemyAttackStyle AttackStyle =>
            definition != null ? definition.attackStyle : EnemyAttackStyle.Melee;
        public bool IsElite => IsEliteDefinition(definition);
        public int ThreatBand => Mathf.Clamp(threatBand, 1, 5);
        public float LastDamagedAt => lastDamagedAt;
        public int ExperienceReward { get; private set; }
        public int Health => Mathf.Max(0, health);
        public int MaxHealth => Mathf.Max(
            1,
            maxHealth > 0
                ? maxHealth
                : definition != null
                    ? definition.maxHealth
                    : 1);
        public bool IsDefeated => defeated;
        public bool OffersThreadbindOnDefeat => threadbindEligibleSpawn;
        public bool IsStaggered => Time.time < staggerUntil;
        public bool IsExposed => Time.time < exposedUntil;
        public bool IsAshguarded => ashguardActive && Time.time < ashguardEndsAt;
        public bool IsWindupPoiseProtected =>
            telegraphing &&
            definition != null &&
            (IsEliteDefinition(definition) ||
             definition.attackStyle == EnemyAttackStyle.Heavy ||
             definition.specialMove != EnemySpecialMove.None);
        public float Poise => Mathf.Clamp(poisePressure, 0f, PoiseThreshold);
        public float PoiseThreshold => Mathf.Max(1f, poiseProfile.Threshold);
        public float StaggerRemaining => Mathf.Max(0f, staggerUntil - Time.time);
        public float ExposeRemaining => Mathf.Max(0f, exposedUntil - Time.time);
        public float AshguardRemaining => Mathf.Max(0f, ashguardEndsAt - Time.time);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ActiveEnemies.Clear();
            activeAttackers = 0;
        }

        public static Enemy Create(
            TetherfallGame game,
            EnemyCamp camp,
            string enemyDefinitionId,
            Vector3 position,
            System.Action<Enemy> defeatedCallback = null)
        {
            return CreateFromDefinition(game, camp, EnemyCatalog.Get(enemyDefinitionId), position, defeatedCallback);
        }

        public static Enemy Create(
            TetherfallGame game,
            EnemyCamp camp,
            string enemyName,
            Vector3 position,
            Color color,
            int maxHealth,
            System.Action<Enemy> defeatedCallback = null)
        {
            var definition = new EnemyDefinition
            {
                id = $"runtime.{enemyName.ToLowerInvariant().Replace(" ", "_")}",
                displayName = enemyName,
                attackStyle = EnemyAttackStyle.Melee,
                maxHealth = maxHealth,
                attackDamage = 8,
                experienceReward = 20,
                moveSpeed = 2.35f,
                attackRange = 2.2f,
                detectionRange = 27f,
                attackCooldown = 1.35f,
                telegraphSeconds = 0.44f,
                scale = 0.95f,
                color = color
            };
            return CreateFromDefinition(game, camp, definition, position, defeatedCallback);
        }

        private static Enemy CreateFromDefinition(
            TetherfallGame game,
            EnemyCamp camp,
            EnemyDefinition definition,
            Vector3 position,
            System.Action<Enemy> defeatedCallback)
        {
            var root = new GameObject(definition.displayName);
            root.transform.position = position;
            var enemy = root.AddComponent<Enemy>();
            enemy.game = game;
            enemy.camp = camp;
            enemy.definition = definition;
            enemy.EnemyName = definition.displayName;
            var elite = IsEliteDefinition(definition);
            var scaling = TetherfallCombatQualityRules.GetEnemyScalingBand(
                game != null ? game.Level : 1,
                ResolveScalingContext(camp, definition),
                elite);
            enemy.maxHealth = Mathf.Max(
                1,
                Mathf.RoundToInt(definition.maxHealth * scaling.HealthMultiplier));
            enemy.attackDamage = Mathf.Max(
                1,
                Mathf.RoundToInt(definition.attackDamage * scaling.DamageMultiplier));
            enemy.threatBand =
                TetherfallCombatQualityRules.GetThreatBand(scaling, false);
            enemy.health = enemy.maxHealth;
            enemy.poiseProfile = TetherfallCombatQualityRules.GetEnemyPoiseProfile(
                definition.attackStyle,
                elite,
                definition.specialMove != EnemySpecialMove.None);
            enemy.ExperienceReward = definition.experienceReward;
            enemy.onDefeated = defeatedCallback;
            enemy.baseColor = definition.color;
            enemy.spawnPosition = position;
            enemy.threadbindEligibleSpawn =
                camp != null &&
                camp.IsWildAccordEncounter &&
                TetherfallAccordSpeciesCatalog.IsEligibleWildEnemyId(
                    definition.id);
            enemy.avoidanceSide = Mathf.Abs(definition.id.GetHashCode()) % 2 == 0 ? 1 : -1;

            enemy.movementController = root.AddComponent<CharacterController>();
            enemy.movementController.radius = 0.38f * definition.scale;
            enemy.movementController.height = 1.8f * definition.scale;
            enemy.movementController.center = Vector3.up * enemy.movementController.height * 0.5f;
            enemy.movementController.stepOffset = Mathf.Min(0.42f, enemy.movementController.height * 0.22f);
            enemy.movementController.slopeLimit = 48f;

            var visual = GameObject.CreatePrimitive(
                definition.attackStyle == EnemyAttackStyle.Ranged ? PrimitiveType.Sphere : PrimitiveType.Capsule);
            visual.name = $"{definition.displayName} Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.up * definition.scale * 0.92f;
            visual.transform.localScale = definition.attackStyle == EnemyAttackStyle.Ranged
                ? new Vector3(definition.scale, definition.scale * 0.82f, definition.scale)
                : new Vector3(definition.scale, definition.scale, definition.scale);
            Object.Destroy(visual.GetComponent<Collider>());
            enemy.bodyRenderer = visual.GetComponent<Renderer>();
            enemy.bodyMaterial =
                game.CreateMaterial(definition.color, 0.08f, 0.25f);
            enemy.bodyRenderer.sharedMaterial = enemy.bodyMaterial;
            enemy.bodyBaseScale = visual.transform.localScale;
            enemy.bodyBasePosition = visual.transform.localPosition;
            var accordSpecies =
                TetherfallAccordSpeciesCatalog.FindByEligibleEnemyId(
                    definition.id);
            if (accordSpecies != null &&
                TetherfallAccordSpeciesCatalog.IsEligibleWildEnemyId(
                    definition.id))
            {
                enemy.bodyRenderer.enabled = false;
                enemy.wildPactArtRig = TetherfallCapturedPactArt.Build(
                    game,
                    root.transform,
                    accordSpecies);
            }
            else
            {
                CombatVisualBuilder.DressEnemy(
                    game,
                    root.transform,
                    definition);
                TetherfallHostileArtOverhaul.Build(enemy, definition);
                enemy.hostileAnimation =
                    root.GetComponentInChildren<TetherfallHostileAnimation>(
                        true);
            }
            enemy.CreateAshguardVisual();
            ActiveEnemies.Add(enemy);
            return enemy;
        }

        private void Update()
        {
            if (game == null || game.Player == null || defeated)
            {
                return;
            }

            if (game.GameplayPaused)
            {
                PauseCombatTimers();
                return;
            }

            RecoverPoisePressure();
            AnimateVisual();
            wildPactArtRig?.Tick(
                Time.time,
                movementController != null
                    ? movementController.velocity
                    : Vector3.zero,
                false);
            if (ashguardActive && !IsAshguarded)
            {
                ClearAshguard();
            }

            if (exposeVisualActive && Time.time >= exposedUntil)
            {
                exposeVisualActive = false;
                if (!telegraphing)
                {
                    RecoverColor();
                }
            }

            var toPlayer = game.Player.transform.position - transform.position;
            toPlayer.y = 0f;
            var distance = toPlayer.magnitude;
            if (distance > definition.detectionRange ||
                Mathf.Abs(game.Player.transform.position.y - transform.position.y) > 8f)
            {
                if (telegraphing)
                {
                    telegraphing = false;
                    CancelAttackTelegraph();
                    ReleaseAttackToken();
                    nextAttack = Mathf.Max(nextAttack, Time.time + 0.55f);
                    RecoverColor();
                }

                ApplyGravity();
                if (transform.position.y < EnemyVoidRecoveryHeight)
                {
                    movementController.enabled = false;
                    transform.position = spawnPosition + Vector3.up * 0.15f;
                    movementController.enabled = true;
                    verticalVelocity = 0f;
                    nextAttack = Time.time + 1.2f;
                }

                return;
            }

            if (telegraphing)
            {
                if (Time.time >= strikeAt)
                {
                    ResolveAttack(distance, toPlayer);
                }

                return;
            }

            if (Time.time < staggerUntil)
            {
                ApplyGravity();
                return;
            }

            var desiredRange = definition.attackRange;
            if (definition.attackStyle == EnemyAttackStyle.Ranged && distance < desiredRange * 0.52f)
            {
                MoveCollisionAware(-toPlayer.normalized, definition.moveSpeed * 0.72f);
                return;
            }

            if (distance > desiredRange)
            {
                MoveCollisionAware(toPlayer.normalized, definition.moveSpeed);
                return;
            }

            ApplyGravity();
            if (Time.time >= nextAttack && TryClaimAttackToken())
            {
                telegraphing = true;
                strikeAt = Time.time + definition.telegraphSeconds;
                if (definition.specialMove == EnemySpecialMove.GroundBurst)
                {
                    groundBurstTarget = GetGroundBurstTarget();
                    hasGroundBurstTarget = true;
                }

                bodyMaterial.color = definition.attackStyle == EnemyAttackStyle.Ranged
                    ? new Color(0.48f, 0.86f, 0.92f)
                    : new Color(0.96f, 0.54f, 0.3f);
                hostileAnimation?.NotifyAttackAnticipation(definition.telegraphSeconds);
                ActivateAshguardForShockwave();
                CreateAttackTelegraph();
            }
        }

        private void MoveCollisionAware(Vector3 desiredDirection, float speed)
        {
            var separation = Vector3.zero;
            var separationHitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                1.7f,
                separationHitBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < separationHitCount; hitIndex++)
            {
                var collider = separationHitBuffer[hitIndex];
                if (collider == null)
                {
                    continue;
                }

                var other = collider.GetComponentInParent<Enemy>();
                if (other == null || other == this || other.defeated)
                {
                    continue;
                }

                var away = transform.position - other.transform.position;
                away.y = 0f;
                var distance = away.magnitude;
                if (distance > 0.01f && distance < 1.7f)
                {
                    separation += away.normalized * (1.7f - distance);
                }
            }

            for (var hitIndex = 0; hitIndex < separationHitCount; hitIndex++)
            {
                separationHitBuffer[hitIndex] = null;
            }

            var direction = desiredDirection + separation * 0.72f;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                ApplyGravity();
                return;
            }

            direction.Normalize();
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 8f);

            verticalVelocity = movementController.isGrounded
                ? -1.1f
                : verticalVelocity + Physics.gravity.y * Time.deltaTime;
            var motion = direction * speed;
            motion.y = verticalVelocity;
            var flags = movementController.Move(motion * Time.deltaTime);
            if ((flags & CollisionFlags.Sides) != 0)
            {
                var sidestep = Vector3.Cross(Vector3.up, direction) * avoidanceSide;
                movementController.Move(sidestep * speed * 0.68f * Time.deltaTime);
            }
        }

        private void ApplyGravity()
        {
            verticalVelocity = movementController.isGrounded
                ? -1.1f
                : verticalVelocity + Physics.gravity.y * Time.deltaTime;
            movementController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        private void PauseCombatTimers()
        {
            nextAttack += Time.deltaTime;
            strikeAt += Time.deltaTime;
            staggerUntil += Time.deltaTime;
            exposedUntil += Time.deltaTime;
            ashguardEndsAt += Time.deltaTime;
            visualImpactUntil += Time.deltaTime;
            if (!float.IsNegativeInfinity(lastPoiseImpactAt))
            {
                lastPoiseImpactAt += Time.deltaTime;
            }
        }

        private bool TryClaimAttackToken()
        {
            if (holdsAttackToken)
            {
                return true;
            }

            if (activeAttackers >= MaxSimultaneousAttackers)
            {
                nextAttack = Mathf.Max(nextAttack, Time.time + 0.18f);
                return false;
            }

            holdsAttackToken = true;
            activeAttackers++;
            return true;
        }

        private void ReleaseAttackToken()
        {
            if (!holdsAttackToken)
            {
                return;
            }

            holdsAttackToken = false;
            activeAttackers = Mathf.Max(0, activeAttackers - 1);
        }

        private void ResolveAttack(float distance, Vector3 toPlayer)
        {
            telegraphing = false;
            hostileAnimation?.NotifyAttackRelease(
                definition.attackStyle == EnemyAttackStyle.Heavy ? 0.42f : 0.3f);
            if (definition.specialMove != EnemySpecialMove.Shockwave)
            {
                ClearAshguard();
            }

            activeTelegraph = null;
            ReleaseAttackToken();
            nextAttack = Time.time + definition.attackCooldown;
            RecoverColor();

            if (definition.specialMove == EnemySpecialMove.GroundBurst)
            {
                var burstTarget = hasGroundBurstTarget
                    ? groundBurstTarget
                    : GetGroundBurstTarget();
                hasGroundBurstTarget = false;
                if (!HasClearAttackPath(burstTarget + Vector3.up * 0.08f))
                {
                    return;
                }

                EnemyShockwave.Create(
                    game,
                    burstTarget,
                    Mathf.Max(0.5f, definition.groundBurstRadius),
                    attackDamage,
                    definition.color,
                    EnemyName);
                CombatFeedback.CreateImpact(
                    game,
                    burstTarget + Vector3.up * 0.14f,
                    definition.color,
                    0.72f);
                return;
            }

            if (!HasClearAttackPath())
            {
                return;
            }

            if (definition.attackStyle == EnemyAttackStyle.Ranged)
            {
                var origin = transform.position + Vector3.up * definition.scale * 1.15f;
                var target = game.Player.transform.position + Vector3.up * 0.9f;
                var direction = (target - origin).normalized;
                var shotCount = definition.specialMove == EnemySpecialMove.TripleShot ? 3 : 1;
                for (var index = 0; index < shotCount; index++)
                {
                    var spread = shotCount == 1 ? 0f : (index - 1) * 8f;
                    var shotDirection = Quaternion.AngleAxis(spread, Vector3.up) * direction;
                    EnemyProjectile.Create(
                        game,
                        origin,
                        shotDirection,
                        Mathf.RoundToInt(attackDamage * (index == 1 || shotCount == 1 ? 1f : 0.72f)),
                        definition.projectileSpeed,
                        definition.color,
                        EnemyName);
                }

                return;
            }

            if (definition.specialMove == EnemySpecialMove.Shockwave)
            {
                EnemyShockwave.Create(
                    game,
                    transform.position,
                    definition.attackRange + 2.55f,
                    attackDamage,
                    definition.color,
                    EnemyName,
                    this);
                return;
            }

            if (definition.specialMove == EnemySpecialMove.QuickLunge &&
                toPlayer.sqrMagnitude > 0.01f)
            {
                movementController.Move(toPlayer.normalized * 2.05f);
                distance = Vector3.Distance(transform.position, game.Player.transform.position);
            }

            if (distance <= definition.attackRange + 1.15f)
            {
                game.DamagePlayer(attackDamage, EnemyName);
                if (definition.attackStyle == EnemyAttackStyle.Heavy && toPlayer.sqrMagnitude > 0.01f)
                {
                    game.Player.ApplyImpact(toPlayer.normalized * 3.2f);
                }
            }
        }

        public void TakeDamage(int amount, string source)
        {
            TakeDamage(amount, source, CombatImpactKind.Light);
        }

        public void TakeDamage(
            int amount,
            string source,
            CombatImpactKind impactKind)
        {
            if (defeated || amount <= 0)
            {
                return;
            }

            if (IsAshguarded)
            {
                var multiplier = definition.ashguardDamageMultiplier > 0f
                    ? definition.ashguardDamageMultiplier
                    : 1f;
                amount = Mathf.Max(1, Mathf.CeilToInt(amount * multiplier));
            }

            if (IsExposed)
            {
                amount = Mathf.CeilToInt(
                    amount *
                    TetherfallCombatQualityRules.EnemyExposeDamageMultiplier);
            }

            lastDamagedAt = Time.time;
            health -= amount;
            if (health > 0)
            {
                ApplyPoiseDamage(amount, impactKind);
            }

            hostileAnimation?.NotifyHit(Mathf.Clamp(amount / (float)Mathf.Max(1, MaxHealth) * 4f, 0.45f, 1.35f));
            visualImpactUntil = Time.time + 0.14f;
            bodyMaterial.color = new Color(0.96f, 0.82f, 0.61f);
            game.PlayEnemyHitFeedback(this, amount);
            if (health > 0)
            {
                Invoke(nameof(RecoverColor), 0.12f);
                return;
            }

            defeated = true;
            CancelInvoke();
            CancelAttackTelegraph();
            ReleaseAttackToken();
            if (threadbindEligibleSpawn)
            {
                game.OnEligibleWildEchoswornDefeated(
                    EnemyId,
                    transform.position,
                    BuildThreadbindOriginId());
            }

            if (camp != null)
            {
                camp.ReportDeath(this);
            }

            onDefeated?.Invoke(this);
            game.OnEnemyDefeated(this);
            hostileAnimation?.NotifyDeath(0.32f);
            ActiveEnemies.Remove(this);
            if (movementController != null)
            {
                movementController.enabled = false;
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject, 0.32f);
            }
            else
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        public void DespawnSilent()
        {
            if (defeated)
            {
                return;
            }

            defeated = true;
            CancelInvoke();
            CancelAttackTelegraph();
            ReleaseAttackToken();
            camp = null;
            onDefeated = null;
            ActiveEnemies.Remove(this);
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        public bool ApplyPoiseDamage(int damage, CombatImpactKind impactKind)
        {
            if (defeated || damage <= 0 || IsAshguarded || IsStaggered)
            {
                return false;
            }

            var protectedWindup = IsWindupPoiseProtected;
            var contribution =
                TetherfallCombatQualityRules.CalculatePoiseContribution(
                    damage,
                    MaxHealth,
                    PoiseThreshold,
                    impactKind,
                    protectedWindup,
                    false);
            if (contribution <= 0f)
            {
                return false;
            }

            lastPoiseImpactAt = Time.time;
            poisePressure = Mathf.Min(PoiseThreshold, poisePressure + contribution);
            if (protectedWindup &&
                !TetherfallCombatQualityRules.CanBreakProtectedWindup(impactKind))
            {
                poisePressure = Mathf.Min(
                    poisePressure,
                    Mathf.Max(0f, PoiseThreshold - 0.01f));
                return false;
            }

            if (poisePressure + 0.001f < PoiseThreshold)
            {
                return false;
            }

            poisePressure = 0f;
            return ApplyStagger(
                TetherfallCombatQualityRules.GetThresholdStaggerSeconds(
                    impactKind,
                    false));
        }

        public bool ApplyStagger(float duration)
        {
            if (defeated || duration <= 0f)
            {
                return false;
            }

            if (IsAshguarded)
            {
                return false;
            }

            duration = Mathf.Min(
                duration,
                TetherfallCombatQualityRules.EnemyMaximumStaggerSeconds);
            poisePressure = 0f;
            var wasStaggered = Time.time < staggerUntil;
            staggerUntil = Mathf.Max(staggerUntil, Time.time + duration);
            if (telegraphing)
            {
                telegraphing = false;
                CancelAttackTelegraph();
                ReleaseAttackToken();
                nextAttack = Mathf.Max(nextAttack, Time.time + 0.55f);
                RecoverColor();
            }

            return !wasStaggered;
        }

        private void RecoverPoisePressure()
        {
            if (poisePressure <= 0f ||
                Time.time < lastPoiseImpactAt + poiseProfile.RecoveryDelaySeconds)
            {
                return;
            }

            poisePressure = Mathf.MoveTowards(
                poisePressure,
                0f,
                poiseProfile.RecoveryPerSecond * Time.deltaTime);
        }

        private static EncounterScalingContext ResolveScalingContext(
            EnemyCamp sourceCamp,
            EnemyDefinition sourceDefinition)
        {
            if (sourceCamp == null)
            {
                return IsEliteDefinition(sourceDefinition)
                    ? EncounterScalingContext.Elite
                    : EncounterScalingContext.Ambient;
            }

            if (sourceCamp.IsWildAccordEncounter)
            {
                return sourceCamp.AwardedFirstClear
                    ? EncounterScalingContext.WildAccordRepeat
                    : EncounterScalingContext.WildAccordFirstClear;
            }

            return sourceCamp.AwardedFirstClear
                ? EncounterScalingContext.CampRepeat
                : EncounterScalingContext.CampFirstClear;
        }

        private static bool IsEliteDefinition(EnemyDefinition sourceDefinition)
        {
            return sourceDefinition != null &&
                   ((!string.IsNullOrEmpty(sourceDefinition.id) &&
                     sourceDefinition.id.StartsWith(
                         "enemy.elite.",
                         System.StringComparison.Ordinal)) ||
                    sourceDefinition.maxHealth >= 220);
        }

        public void ApplyExpose(float duration)
        {
            if (defeated || duration <= 0f)
            {
                return;
            }

            exposedUntil = Mathf.Max(exposedUntil, Time.time + duration);
            exposeVisualActive = true;
            if (!telegraphing && bodyRenderer != null)
            {
                bodyMaterial.color = Color.Lerp(baseColor, new Color(0.96f, 0.76f, 0.3f), 0.64f);
            }
        }

        public bool TryBreakAshguardFromCrossweave(string responderName)
        {
            return BreakAshguard(
                definition.ashguardBreakExposeSeconds,
                definition.ashguardBreakStaggerSeconds,
                $"{responderName} Crossweave");
        }

        public bool TryBreakAshguardFromPerfectGuard()
        {
            return BreakAshguard(
                definition.ashguardBreakExposeSeconds + 1.4f,
                definition.ashguardBreakStaggerSeconds + 0.38f,
                "Perfect guard");
        }

        private void CreateAshguardVisual()
        {
            if (definition == null || !definition.ashguardDuringShockwave)
            {
                return;
            }

            ashguardVisual = new GameObject("Ashguard shards").transform;
            ashguardVisual.SetParent(transform, false);
            ashguardVisual.localPosition = Vector3.up * definition.scale * 0.95f;
            var material = game.CreateGlowMaterial(new Color(1f, 0.34f, 0.12f), 1.75f, 0.66f);
            for (var index = 0; index < 3; index++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Ashguard shard";
                shard.transform.SetParent(ashguardVisual, false);
                var angle = (index * 120f + 20f) * Mathf.Deg2Rad;
                shard.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * definition.scale * 0.78f,
                    0.1f + index % 2 * definition.scale * 0.14f,
                    Mathf.Sin(angle) * definition.scale * 0.78f);
                shard.transform.localScale = new Vector3(0.14f, 0.52f, 0.14f) * definition.scale;
                shard.transform.localRotation = Quaternion.Euler(16f, index * 120f, 22f);
                shard.GetComponent<Renderer>().sharedMaterial = material;
                Object.Destroy(shard.GetComponent<Collider>());
            }

            ashguardVisual.gameObject.SetActive(false);
        }

        private void ActivateAshguardForShockwave()
        {
            if (definition == null ||
                !definition.ashguardDuringShockwave ||
                definition.specialMove != EnemySpecialMove.Shockwave ||
                !telegraphing)
            {
                return;
            }

            ashguardActive = true;
            ashguardEndsAt = strikeAt + 0.72f;
            if (ashguardVisual != null)
            {
                ashguardVisual.gameObject.SetActive(true);
                ashguardVisual.localScale = Vector3.one;
            }

            if (bodyRenderer != null)
            {
                bodyMaterial.color = new Color(1f, 0.43f, 0.16f);
            }
        }

        private void ClearAshguard()
        {
            ashguardActive = false;
            ashguardEndsAt = 0f;
            if (ashguardVisual != null)
            {
                ashguardVisual.gameObject.SetActive(false);
            }
        }

        private bool BreakAshguard(float exposeSeconds, float staggerSeconds, string source)
        {
            if (!IsAshguarded)
            {
                return false;
            }

            telegraphing = false;
            ClearAshguard();
            CancelAttackTelegraph();
            ReleaseAttackToken();
            nextAttack = Mathf.Max(nextAttack, Time.time + 0.78f);
            ApplyExpose(exposeSeconds > 0f ? exposeSeconds : 4.6f);
            ApplyStagger(staggerSeconds > 0f ? staggerSeconds : 0.48f);
            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                "Ashguard fracture",
                transform.position + Vector3.up * 0.08f,
                new Vector3(0.42f, 0.045f, 0.42f),
                RuntimeEffectMaterialCache.GetTransparent(
                    game,
                    new Color(1f, 0.58f, 0.22f, 0.7f),
                    0.72f),
                0.34f,
                definition.scale * 3.6f);
            game.Notify($"{EnemyName}'s Ashguard fractures under {source}.");
            return true;
        }

        public static int CopyActiveEnemiesNonAlloc(Enemy[] destination)
        {
            if (destination == null || destination.Length == 0)
            {
                return 0;
            }

            for (var index = ActiveEnemies.Count - 1; index >= 0; index--)
            {
                if (ActiveEnemies[index] == null)
                {
                    ActiveEnemies.RemoveAt(index);
                }
            }

            var copied = 0;
            var limit = Mathf.Min(
                destination.Length,
                MaximumActiveEnemyCopyCount);
            for (var index = 0;
                 index < ActiveEnemies.Count && copied < limit;
                 index++)
            {
                var enemy = ActiveEnemies[index];
                if (enemy == null || enemy.defeated)
                {
                    continue;
                }

                destination[copied++] = enemy;
            }

            return copied;
        }

        public static Enemy FindClosest(Vector3 point, float range)
        {
            Enemy closest = null;
            var bestDistance = range;
            for (var index = ActiveEnemies.Count - 1; index >= 0; index--)
            {
                var enemy = ActiveEnemies[index];
                if (enemy == null)
                {
                    ActiveEnemies.RemoveAt(index);
                    continue;
                }

                var distance = Vector3.Distance(point, enemy.transform.position);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    closest = enemy;
                }
            }

            return closest;
        }

        private void RecoverColor()
        {
            if (bodyRenderer != null)
            {
                bodyMaterial.color = IsAshguarded
                    ? new Color(1f, 0.43f, 0.16f)
                    : IsExposed
                        ? Color.Lerp(baseColor, new Color(0.96f, 0.76f, 0.3f), 0.64f)
                        : baseColor;
            }
        }

        private void AnimateVisual()
        {
            wildPactArtRig?.Tick(
                Time.time,
                movementController != null
                    ? movementController.velocity
                    : Vector3.zero,
                false);
            if (bodyRenderer == null)
            {
                return;
            }

            var breathing = 1f + Mathf.Sin(Time.time * (definition.attackStyle == EnemyAttackStyle.Heavy ? 2.4f : 3.6f)) * 0.025f;
            var telegraphScale = telegraphing ? 1.1f : 1f;
            var impactScale = Time.time < visualImpactUntil ? 1.13f : 1f;
            var scale = breathing * telegraphScale * impactScale;
            bodyRenderer.transform.localScale = Vector3.Lerp(
                bodyRenderer.transform.localScale,
                bodyBaseScale * scale,
                1f - Mathf.Exp(-16f * Time.deltaTime));

            if (definition.attackStyle == EnemyAttackStyle.Ranged)
            {
                var bob = Mathf.Sin(Time.time * 2.6f) * 0.045f;
                bodyRenderer.transform.localPosition = bodyBasePosition + Vector3.up * bob;
            }

            if (ashguardVisual != null && IsAshguarded)
            {
                ashguardVisual.Rotate(0f, 156f * Time.deltaTime, 0f, Space.Self);
                var guardPulse = 1f + Mathf.Sin(Time.time * 8.4f) * 0.08f;
                ashguardVisual.localScale = Vector3.one * guardPulse;
            }
        }

        private void CreateAttackTelegraph()
        {
            if (definition.specialMove == EnemySpecialMove.GroundBurst && hasGroundBurstTarget)
            {
                var radius = Mathf.Max(0.5f, definition.groundBurstRadius);
                activeTelegraph = TimedPulse.Spawn(
                    PrimitiveType.Cylinder,
                    "Ground burst telegraph",
                    groundBurstTarget + Vector3.up * 0.05f,
                    new Vector3(radius * 2f, 0.04f, radius * 2f),
                    RuntimeEffectMaterialCache.GetTransparent(
                        game,
                        TetherfallAccessibilityRuntime.ResolveTelegraphColor(
                            new Color(definition.color.r, definition.color.g, definition.color.b, 0.58f),
                            TetherfallAccessibilityColorCue.Warning),
                        0.52f),
                    definition.telegraphSeconds + 0.05f,
                    1f);
                return;
            }

            var heavyScale = definition.attackStyle == EnemyAttackStyle.Heavy ? 1.45f : 1f;
            var color = definition.attackStyle == EnemyAttackStyle.Ranged
                ? new Color(0.33f, 0.78f, 0.88f, 0.55f)
                : new Color(0.94f, 0.38f, 0.24f, 0.58f);
            color = TetherfallAccessibilityRuntime.ResolveTelegraphColor(
                color,
                TetherfallAccessibilityColorCue.Warning);
            activeTelegraph = TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                "Enemy attack telegraph",
                transform.position + Vector3.up * 0.05f,
                new Vector3(0.4f * heavyScale, 0.04f, 0.4f * heavyScale),
                RuntimeEffectMaterialCache.GetTransparent(game, color, 0.42f),
                definition.telegraphSeconds + 0.05f,
                3.8f * heavyScale);
        }

        private void CancelAttackTelegraph()
        {
            hasGroundBurstTarget = false;
            hostileAnimation?.CancelAction();
            ClearAshguard();
            if (activeTelegraph == null)
            {
                return;
            }

            var pulse = activeTelegraph.GetComponent<TimedPulse>();
            if (pulse != null)
            {
                pulse.Cancel();
            }
            else
            {
                Object.Destroy(activeTelegraph);
            }

            activeTelegraph = null;
        }

        private bool HasClearAttackPath()
        {
            if (game == null || game.Player == null)
            {
                return false;
            }

            var origin = transform.position + Vector3.up * definition.scale * 1.1f;
            var target = game.Player.transform.position + Vector3.up * 0.9f;
            var path = target - origin;
            var distance = path.magnitude;
            if (distance <= 0.01f)
            {
                return true;
            }

            Collider closestCollider = null;
            var closestDistance = float.PositiveInfinity;
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                path / distance,
                attackPathHitBuffer,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = attackPathHitBuffer[hitIndex];
                if (hit.collider.GetComponentInParent<Enemy>() == this)
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestCollider = hit.collider;
                }
            }

            ClearRaycastHits(attackPathHitBuffer, hitCount);
            return closestCollider != null &&
                   closestCollider.GetComponentInParent<PlayerAvatar>() != null;
        }

        private bool HasClearAttackPath(Vector3 target)
        {
            if (game == null)
            {
                return false;
            }

            var origin = transform.position + Vector3.up * definition.scale * 1.1f;
            var path = target - origin;
            var distance = path.magnitude;
            if (distance <= 0.01f)
            {
                return true;
            }

            Collider closestCollider = null;
            var closestDistance = float.PositiveInfinity;
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                path / distance,
                attackPathHitBuffer,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = attackPathHitBuffer[hitIndex];
                if (hit.collider.GetComponentInParent<Enemy>() == this)
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestCollider = hit.collider;
                }
            }

            ClearRaycastHits(attackPathHitBuffer, hitCount);
            return closestCollider == null || closestDistance >= distance - 0.16f ||
                   closestCollider.GetComponentInParent<PlayerAvatar>() != null;
        }

        private Vector3 GetGroundBurstTarget()
        {
            if (game == null || game.Player == null)
            {
                return transform.position;
            }

            var playerTransform = game.Player.transform;
            var origin = playerTransform.position + Vector3.up * 3.2f;
            var closestPoint = Vector3.zero;
            var closestDistance = float.PositiveInfinity;
            var hasClosest = false;
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundTargetHitBuffer,
                7f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = groundTargetHitBuffer[hitIndex];
                if (hit.collider.GetComponentInParent<PlayerAvatar>() != null)
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestPoint = hit.point;
                    hasClosest = true;
                }
            }

            ClearRaycastHits(groundTargetHitBuffer, hitCount);
            return hasClosest
                ? closestPoint + Vector3.up * 0.02f
                : new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        }

        private static void ClearRaycastHits(RaycastHit[] buffer, int count)
        {
            for (var index = 0; index < count; index++)
            {
                buffer[index] = default;
            }
        }

        private string BuildThreadbindOriginId()
        {
            return $"{EnemyId}@{Mathf.RoundToInt(spawnPosition.x)}:" +
                   $"{Mathf.RoundToInt(spawnPosition.y)}:" +
                   $"{Mathf.RoundToInt(spawnPosition.z)}";
        }

        private void OnDestroy()
        {
            ReleaseAttackToken();
            ActiveEnemies.Remove(this);
            wildPactArtRig?.Dispose();
            wildPactArtRig = null;
            if (bodyMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(bodyMaterial);
                }
                else
                {
                    DestroyImmediate(bodyMaterial);
                }

                bodyMaterial = null;
            }
        }
    }

    public sealed class EnemyProjectile : MonoBehaviour
    {
        private readonly RaycastHit[] hitBuffer = new RaycastHit[16];
        private TetherfallGame game;
        private Vector3 direction;
        private float speed;
        private float expiresAt;
        private int damage;
        private string source;

        public static EnemyProjectile Create(
            TetherfallGame game,
            Vector3 origin,
            Vector3 direction,
            int damage,
            float speed,
            Color color,
            string source)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = $"{source} projectile";
            root.transform.position = origin + direction * 1.05f;
            root.transform.localScale = Vector3.one * 0.34f;
            Object.Destroy(root.GetComponent<Collider>());
            root.GetComponent<Renderer>().material = game.CreateMaterial(color, 0.12f, 0.7f);
            var projectile = root.AddComponent<EnemyProjectile>();
            projectile.game = game;
            projectile.direction = direction.normalized;
            projectile.damage = damage;
            projectile.speed = Mathf.Max(8f, speed);
            projectile.source = source;
            projectile.expiresAt = Time.time + 4f;
            return projectile;
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            if (game.GameplayPaused)
            {
                expiresAt += Time.deltaTime;
                return;
            }

            var distance = speed * Time.deltaTime;
            var hitCount = Physics.SphereCastNonAlloc(
                transform.position,
                0.16f,
                direction,
                hitBuffer,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            Collider closestCollider = null;
            var closestDistance = float.PositiveInfinity;
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = hitBuffer[hitIndex];
                if (hit.collider.GetComponentInParent<Enemy>() != null)
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestCollider = hit.collider;
                }
            }

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                hitBuffer[hitIndex] = default;
            }

            if (closestCollider != null)
            {
                var player = closestCollider.GetComponentInParent<PlayerAvatar>();
                if (player != null)
                {
                    game.DamagePlayer(damage, source);
                }

                Destroy(gameObject);
                return;
            }

            transform.position += direction * distance;
            if (Time.time >= expiresAt)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class EnemyShockwave : MonoBehaviour
    {
        private static readonly RaycastHit[] GroundHits = new RaycastHit[16];
        private static readonly RaycastHit[] PathHits = new RaycastHit[16];

        private TetherfallGame game;
        private float radius;
        private int damage;
        private string source;
        private Enemy sourceEnemy;
        private float startedAt;
        private float expiresAt;
        private bool impacted;

        public static EnemyShockwave Create(
            TetherfallGame game,
            Vector3 position,
            float radius,
            int damage,
            Color color,
            string source,
            Enemy sourceEnemy = null)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = $"{source} shockwave";
            root.transform.position = position + Vector3.up * 0.05f;
            root.transform.localScale = new Vector3(0.28f, 0.035f, 0.28f);
            root.GetComponent<Renderer>().material = RuntimeEffectMaterialCache.GetTransparent(
                game,
                TetherfallAccessibilityRuntime.ResolveTelegraphColor(
                    new Color(color.r, color.g, color.b, 0.58f),
                    TetherfallAccessibilityColorCue.Warning),
                0.5f);
            Object.Destroy(root.GetComponent<Collider>());

            var shockwave = root.AddComponent<EnemyShockwave>();
            shockwave.game = game;
            shockwave.radius = radius;
            shockwave.damage = damage;
            shockwave.source = source;
            shockwave.sourceEnemy = sourceEnemy;
            shockwave.startedAt = Time.time;
            shockwave.expiresAt = Time.time + 0.62f;
            return shockwave;
        }

        private void Update()
        {
            if (game == null)
            {
                Destroy(gameObject);
                return;
            }

            if (game.GameplayPaused)
            {
                startedAt += Time.deltaTime;
                expiresAt += Time.deltaTime;
                return;
            }

            var progress = Mathf.InverseLerp(startedAt, expiresAt, Time.time);
            var currentRadius = Mathf.Lerp(0.14f, radius, progress);
            var scale = currentRadius * 2f;
            transform.localScale = new Vector3(scale, 0.035f, scale);

            if (!impacted && CanReachPlayer(currentRadius))
            {
                impacted = true;
                game.DamagePlayer(damage, source, sourceEnemy);
            }

            if (Time.time >= expiresAt)
            {
                Destroy(gameObject);
            }
        }

        private bool CanReachPlayer(float currentRadius)
        {
            if (game.Player == null)
            {
                return false;
            }

            var playerPosition = game.Player.transform.position;
            var horizontal = playerPosition - transform.position;
            horizontal.y = 0f;
            if (horizontal.magnitude > currentRadius ||
                Mathf.Abs(playerPosition.y - transform.position.y) > 1.45f ||
                !HasSupportedGroundPath(playerPosition))
            {
                return false;
            }

            var origin = transform.position + Vector3.up * 0.45f;
            var target = playerPosition + Vector3.up * 0.9f;
            var path = target - origin;
            var distance = path.magnitude;
            if (distance <= 0.01f)
            {
                return true;
            }

            Collider closestCollider = null;
            var closestDistance = float.PositiveInfinity;
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                path / distance,
                PathHits,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = PathHits[hitIndex];
                if (hit.collider.GetComponentInParent<Enemy>() != null)
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestCollider = hit.collider;
                }
            }

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                PathHits[hitIndex] = default;
            }

            return closestCollider != null &&
                   closestCollider.GetComponentInParent<PlayerAvatar>() != null;
        }

        private bool HasSupportedGroundPath(Vector3 playerPosition)
        {
            var start = transform.position;
            var horizontal = playerPosition - start;
            horizontal.y = 0f;
            var distance = horizontal.magnitude;
            if (distance <= 0.05f)
            {
                return true;
            }

            var sampleCount = Mathf.Clamp(Mathf.CeilToInt(distance / 1.1f), 2, 32);
            var expectedHeight = start.y;
            for (var index = 0; index <= sampleCount; index++)
            {
                var progress = index / (float)sampleCount;
                var sample = Vector3.Lerp(start, playerPosition, progress);
                var rayOrigin = new Vector3(sample.x, expectedHeight + 1.35f, sample.z);
                var hitCount = Physics.RaycastNonAlloc(
                    rayOrigin,
                    Vector3.down,
                    GroundHits,
                    2.8f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);
                var foundGround = false;
                var groundHeight = float.NegativeInfinity;
                for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
                {
                    var hit = GroundHits[hitIndex];
                    if (hit.collider == null ||
                        hit.collider.GetComponentInParent<PlayerAvatar>() != null ||
                        hit.collider.GetComponentInParent<Enemy>() != null ||
                        hit.collider.GetComponentInParent<PactCreature>() != null ||
                        hit.collider.GetComponentInParent<HumanCompanion>() != null)
                    {
                        continue;
                    }

                    if (!foundGround || hit.point.y > groundHeight)
                    {
                        foundGround = true;
                        groundHeight = hit.point.y;
                    }
                }

                if (!foundGround || Mathf.Abs(groundHeight - expectedHeight) > 0.72f)
                {
                    return false;
                }

                expectedHeight = groundHeight;
            }

            return Mathf.Abs(playerPosition.y - expectedHeight) <= 1.35f;
        }
    }

    public sealed class EnemyCamp : MonoBehaviour
    {
        private const float WakeDistance = 78f;
        private const float KeepDistance = 96f;
        private const float SoftResetDistance = 140f;

        private readonly List<Enemy> aliveEnemies = new List<Enemy>();
        private TetherfallGame game;
        private EncounterDefinition encounter;
        private float clearedAt = -1f;
        private float nextWaveAt = -1f;
        private int currentWave;
        private bool awardedFirstClear;
        private bool populationActive;

        public string EncounterId => encounter != null ? encounter.id : string.Empty;
        public string PersistenceId { get; private set; }
        public string CampName { get; private set; }
        public bool AwardedFirstClear => awardedFirstClear;
        public bool IsPopulated => populationActive;
        public bool IsWildAccordEncounter =>
            encounter != null &&
            encounter.id.StartsWith(
                "encounter.wild.",
                System.StringComparison.Ordinal);
        public bool IsCombatLocked
        {
            get
            {
                PruneMissingEnemies();
                return aliveEnemies.Count > 0 || nextWaveAt > 0f;
            }
        }
        public Vector3 AnchorPosition => transform.position;

        public static EnemyCamp Create(
            TetherfallGame game,
            string encounterId,
            Vector3 position,
            string persistenceId = null)
        {
            var definition = EncounterCatalog.Get(encounterId);
            var root = new GameObject(definition.displayName);
            root.transform.position = position;
            var camp = root.AddComponent<EnemyCamp>();
            camp.game = game;
            camp.encounter = definition;
            camp.CampName = definition.displayName;
            camp.PersistenceId = string.IsNullOrEmpty(persistenceId)
                ? $"{encounterId}@{Mathf.RoundToInt(position.x)}:{Mathf.RoundToInt(position.z)}"
                : persistenceId;
            camp.CreateVisuals();
            if (!camp.IsWildAccordEncounter)
            {
                TetherfallCampPresentation.Build(game, camp, definition);
            }
            camp.enabled = false;
            game.RegisterEnemyCamp(camp);
            return camp;
        }

        private void Update()
        {
            if (!populationActive)
            {
                return;
            }

            if (game != null && game.GameplayPaused)
            {
                if (nextWaveAt > 0f)
                {
                    nextWaveAt += Time.deltaTime;
                }

                if (clearedAt > 0f)
                {
                    clearedAt += Time.deltaTime;
                }

                return;
            }

            var removedMissingEnemy = PruneMissingEnemies();
            if (removedMissingEnemy &&
                aliveEnemies.Count == 0 &&
                nextWaveAt < 0f &&
                clearedAt < 0f)
            {
                FinishCurrentWave();
            }

            if (aliveEnemies.Count == 0 &&
                nextWaveAt > 0f &&
                Time.time >= nextWaveAt)
            {
                nextWaveAt = -1f;
                currentWave++;
                SpawnWave();
            }

            if (aliveEnemies.Count == 0 &&
                clearedAt > 0f &&
                Time.time >= clearedAt + encounter.respawnSeconds)
            {
                clearedAt = -1f;
                currentWave = 0;
                SpawnWave();
                game.Notify(
                    IsWildAccordEncounter
                        ? $"{CampName} Echosworn have returned to their habitat."
                        : $"{CampName} has gathered new hostile scouts.");
            }
        }

        public void EvaluatePopulation(Vector3 playerPosition, bool forceSoftReset = false)
        {
            var delta = playerPosition - transform.position;
            delta.y = 0f;
            var distanceSquared = delta.sqrMagnitude;

            if (forceSoftReset && distanceSquared > SoftResetDistance * SoftResetDistance)
            {
                DeactivatePopulation(true);
                return;
            }

            if (populationActive)
            {
                if (!IsCombatLocked && distanceSquared > KeepDistance * KeepDistance)
                {
                    DeactivatePopulation(false);
                }

                return;
            }

            if (distanceSquared <= WakeDistance * WakeDistance)
            {
                ActivatePopulation();
            }
        }

        private void ActivatePopulation()
        {
            if (populationActive || encounter == null)
            {
                return;
            }

            populationActive = true;
            enabled = true;
            if (clearedAt > 0f && Time.time < clearedAt + encounter.respawnSeconds)
            {
                return;
            }

            clearedAt = -1f;
            nextWaveAt = -1f;
            currentWave = 0;
            SpawnWave();
        }

        private void DeactivatePopulation(bool softResetCombat)
        {
            if (!populationActive)
            {
                return;
            }

            if (IsCombatLocked && !softResetCombat)
            {
                return;
            }

            DespawnAliveSilent();
            if (softResetCombat)
            {
                currentWave = 0;
            }

            nextWaveAt = -1f;
            populationActive = false;
            enabled = false;
        }

        private void DespawnAliveSilent()
        {
            for (var index = aliveEnemies.Count - 1; index >= 0; index--)
            {
                var enemy = aliveEnemies[index];
                aliveEnemies.RemoveAt(index);
                if (enemy != null)
                {
                    enemy.DespawnSilent();
                }
            }
        }

        private bool PruneMissingEnemies()
        {
            var removedAny = false;
            for (var index = aliveEnemies.Count - 1; index >= 0; index--)
            {
                if (aliveEnemies[index] == null)
                {
                    aliveEnemies.RemoveAt(index);
                    removedAny = true;
                }
            }

            return removedAny;
        }

        public void ReportDeath(Enemy enemy)
        {
            aliveEnemies.Remove(enemy);
            if (aliveEnemies.Count != 0)
            {
                return;
            }

            FinishCurrentWave();
        }

        private void FinishCurrentWave()
        {
            if (currentWave + 1 < encounter.WaveCount)
            {
                nextWaveAt = Time.time + 1.35f;
                game.Notify($"{CampName}: reinforcements incoming. Wave {currentWave + 2}/{encounter.WaveCount}.");
                return;
            }

            clearedAt = Time.time;
            if (!awardedFirstClear)
            {
                awardedFirstClear = true;
                game.OnCampCleared(this);
            }
            else
            {
                game.Notify($"{CampName} is quiet for now.");
            }
        }

        private void SpawnWave()
        {
            var enemies = encounter.GetWave(currentWave);
            for (var index = 0; index < enemies.Length; index++)
            {
                var angle = index * Mathf.PI * 2f / Mathf.Max(1, enemies.Length);
                var radius = index % 2 == 0 ? 4.2f : 6.1f;
                var enemy = Enemy.Create(
                    game,
                    this,
                    enemies[index],
                    transform.position + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0.15f,
                        Mathf.Sin(angle) * radius));
                aliveEnemies.Add(enemy);
            }
        }

        public void RestoreFirstClear(bool wasAwarded)
        {
            awardedFirstClear = wasAwarded;
        }

        private void CreateVisuals()
        {
            if (IsWildAccordEncounter)
            {
                CreateWildAccordHabitatVisuals();
                return;
            }

            var fire = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fire.name = "Hostile campfire";
            fire.transform.SetParent(transform, false);
            fire.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            fire.transform.localScale = new Vector3(0.55f, 0.66f, 0.55f);
            fire.GetComponent<Renderer>().material = game.CreateGlowMaterial(
                Color.Lerp(encounter.campColor, new Color(0.96f, 0.3f, 0.12f), 0.54f),
                1.25f,
                0.55f);
            Object.Destroy(fire.GetComponent<Collider>());
            fire.AddComponent<RouteGlow>().Initialize(0.12f, transform.position.x + transform.position.z);

            var fireLight = fire.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = Color.Lerp(encounter.campColor, new Color(1f, 0.38f, 0.18f), 0.62f);
            fireLight.intensity = 1.05f;
            fireLight.range = 7.5f;
            fireLight.shadows = LightShadows.None;

            for (var index = 0; index < 2; index++)
            {
                var tentPosition = transform.position + new Vector3(index == 0 ? -8.2f : 8.2f, 0f, -5.4f);
                var tent = WorldArt.SpawnKenney(
                    game,
                    "tent_detailedOpen",
                    tentPosition,
                    Vector3.one * 1.25f,
                    Color.Lerp(encounter.campColor, new Color(0.08f, 0.07f, 0.09f), 0.46f),
                    true);
                if (tent != null)
                {
                    tent.transform.rotation = Quaternion.Euler(0f, index == 0 ? 42f : -38f, 0f);
                }
            }

            for (var index = 0; index < 3; index++)
            {
                var angle = index * Mathf.PI * 2f / 3f + 0.48f;
                var stake = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stake.name = "Hostile camp marker";
                stake.transform.SetParent(transform, false);
                stake.transform.localPosition = new Vector3(Mathf.Cos(angle) * 6.4f, 0.7f, Mathf.Sin(angle) * 4.8f);
                stake.transform.localScale = new Vector3(0.08f, 0.7f, 0.08f);
                stake.GetComponent<Renderer>().material = game.CreateMaterial(
                    Color.Lerp(encounter.campColor, new Color(0.78f, 0.3f, 0.2f), 0.4f),
                    0.1f,
                    0.42f);
                Object.Destroy(stake.GetComponent<Collider>());
            }

            WorldVisuals.AddLabel(transform, CampName, new Vector3(0f, 2.4f, 0f), new Color(0.95f, 0.62f, 0.52f));
        }

        private void CreateWildAccordHabitatVisuals()
        {
            for (var index = 0; index < 3; index++)
            {
                var angle = index * Mathf.PI * 2f / 3f + 0.36f;
                var marker = GameObject.CreatePrimitive(
                    index == 1
                        ? PrimitiveType.Cylinder
                        : PrimitiveType.Sphere);
                marker.name = "Wild Accord habitat marker";
                marker.transform.SetParent(transform, false);
                marker.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 3.1f,
                    0.18f,
                    Mathf.Sin(angle) * 2.6f);
                marker.transform.localScale =
                    index == 1
                        ? new Vector3(0.16f, 0.36f, 0.16f)
                        : new Vector3(0.44f, 0.16f, 0.36f);
                marker.GetComponent<Renderer>().material =
                    game.CreateMaterial(
                        Color.Lerp(
                            encounter.campColor,
                            new Color(0.18f, 0.25f, 0.2f),
                            0.42f),
                        0.06f,
                        0.3f);
                Object.Destroy(marker.GetComponent<Collider>());
            }

            WorldVisuals.AddLabel(
                transform,
                CampName,
                new Vector3(0f, 1.45f, 0f),
                Color.Lerp(encounter.campColor, Color.white, 0.28f),
                0.075f);
        }
    }

    public sealed class CampSite : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private Light hearthLight;
        private int tier;

        public string InteractionPrompt => "Open portable camp services";

        public static CampSite Create(TetherfallGame game, Vector3 position)
        {
            var root = new GameObject("Portable Camp");
            root.transform.position = position;
            var camp = root.AddComponent<CampSite>();
            camp.game = game;
            game.RegisterInteractable(camp);

            var fire = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fire.name = "Hearthfire";
            fire.transform.SetParent(root.transform, false);
            fire.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            fire.transform.localScale = new Vector3(0.75f, 0.6f, 0.75f);
            fire.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.95f, 0.48f, 0.16f), 0.18f, 0.7f);
            Object.Destroy(fire.GetComponent<Collider>());

            var tent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tent.name = "Knotcloth Tent";
            tent.transform.SetParent(root.transform, false);
            tent.transform.localPosition = new Vector3(1.45f, 0.72f, -0.8f);
            tent.transform.localScale = new Vector3(2.2f, 1.45f, 1.75f);
            tent.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.14f, 0.33f, 0.37f), 0f, 0.3f);
            Object.Destroy(tent.GetComponent<Collider>());

            camp.hearthLight = root.AddComponent<Light>();
            camp.hearthLight.type = LightType.Point;
            camp.hearthLight.color = new Color(1f, 0.5f, 0.22f);
            camp.hearthLight.intensity = 2.2f;
            camp.hearthLight.range = 11f;
            WorldVisuals.AddLabel(root.transform, "Portable Camp", new Vector3(0f, 2.1f, 0f), new Color(0.94f, 0.88f, 0.73f));
            return camp;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            owner.OpenCampPanel();
        }

        public void Rest(TetherfallGame owner)
        {
            owner.HealPlayer(owner.MaxHealth);
            owner.RestoreAllStamina();
            owner.Notify("The expedition rests. Resource nodes and enemy camps keep their own rhythms.");
        }

        public void SetTier(int upgradedTier)
        {
            var targetTier = Mathf.Clamp(upgradedTier, 0, 2);
            if (targetTier <= tier)
            {
                return;
            }

            while (tier < targetTier)
            {
                tier++;
                CreateTierVisual(tier);
            }

            if (hearthLight != null)
            {
                hearthLight.range = 11f + tier * 3f;
                hearthLight.intensity = 2.2f + tier * 0.6f;
            }
        }

        private void CreateTierVisual(int visualTier)
        {
            var upgrade = GameObject.CreatePrimitive(visualTier == 1 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            upgrade.name = visualTier == 1 ? "Camp drying rack" : "Camp route beacon";
            upgrade.transform.SetParent(transform, false);
            upgrade.transform.localPosition = visualTier == 1 ? new Vector3(-1.6f, 0.82f, 0.8f) : new Vector3(-1.8f, 1.55f, 0.1f);
            upgrade.transform.localScale = visualTier == 1 ? new Vector3(0.55f, 0.82f, 0.55f) : new Vector3(0.24f, 1.55f, 0.24f);
            upgrade.GetComponent<Renderer>().material = game.CreateMaterial(
                visualTier == 1 ? new Color(0.66f, 0.44f, 0.23f) : new Color(0.32f, 0.91f, 0.76f),
                visualTier == 1 ? 0.04f : 0.18f,
                0.52f);
            Object.Destroy(upgrade.GetComponent<Collider>());
        }
    }

    public sealed class Hearthgate : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private Renderer[] anchorRenderers;
        private Light gateLight;
        private bool restored;

        public string InteractionPrompt => restored ? "Listen to the restored Hearthgate" : "Stabilize the Hearthgate";

        public static Hearthgate Create(TetherfallGame game, Vector3 position)
        {
            var root = new GameObject("Alderwake Hearthgate");
            root.transform.position = position;
            var gate = root.AddComponent<Hearthgate>();
            gate.game = game;
            game.RegisterInteractable(gate);
            game.RegisterHearthgate(gate);

            gate.anchorRenderers = new Renderer[4];
            for (var index = 0; index < 4; index++)
            {
                var angle = index * 90f * Mathf.Deg2Rad;
                var anchor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                anchor.name = "Hearthgate Anchor";
                anchor.transform.SetParent(root.transform, false);
                anchor.transform.localPosition = new Vector3(Mathf.Cos(angle) * 3.1f, 1.05f, Mathf.Sin(angle) * 3.1f);
                anchor.transform.localScale = new Vector3(0.34f, 2.1f, 0.34f);
                anchor.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.34f, 0.25f, 0.19f), 0.35f, 0.4f);
                Object.Destroy(anchor.GetComponent<Collider>());
                gate.anchorRenderers[index] = anchor.GetComponent<Renderer>();
            }

            gate.gateLight = root.AddComponent<Light>();
            gate.gateLight.type = LightType.Point;
            gate.gateLight.color = new Color(0.23f, 0.72f, 0.69f);
            gate.gateLight.range = 13f;
            gate.gateLight.intensity = 0.5f;
            WorldVisuals.AddLabel(root.transform, "Frayed Hearthgate", new Vector3(0f, 3.3f, 0f), new Color(0.86f, 0.79f, 0.66f));
            return gate;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            if (restored)
            {
                owner.HealPlayer(owner.MaxHealth);
                owner.Notify("The Hearthgate holds an open road between the regions.");
                return;
            }

            owner.RestoreHearthgate();
        }

        public void SetRestored(bool isRestored)
        {
            restored = isRestored;
            gateLight.intensity = isRestored ? 3.3f : 0.5f;
            for (var index = 0; index < anchorRenderers.Length; index++)
            {
                anchorRenderers[index].material.color = isRestored
                    ? new Color(0.35f, 0.88f, 0.76f)
                    : new Color(0.34f, 0.25f, 0.19f);
            }
        }
    }

    public sealed class TimedPulse : MonoBehaviour
    {
        private static readonly Dictionary<PrimitiveType, Queue<GameObject>> Pools =
            new Dictionary<PrimitiveType, Queue<GameObject>>();

        private float expiresAt;
        private Vector3 startScale;
        private Vector3 targetScale;
        private Renderer visual;
        private Color baseColor;
        private MaterialPropertyBlock propertyBlock;
        private PrimitiveType primitive;
        private bool pooled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPools()
        {
            Pools.Clear();
        }

        public static GameObject Spawn(
            PrimitiveType primitiveType,
            string effectName,
            Vector3 position,
            Vector3 scale,
            Material material,
            float lifetime,
            float targetScale)
        {
            var root = Acquire(primitiveType, effectName);
            root.transform.position = position;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = scale;
            root.GetComponent<Renderer>().sharedMaterial = material;
            var effect = root.GetComponent<TimedPulse>() ?? root.AddComponent<TimedPulse>();
            effect.Initialize(lifetime, targetScale, primitiveType, true);
            return root;
        }

        public void Initialize(float lifetime, float scale, PrimitiveType primitiveType = PrimitiveType.Sphere, bool shouldPool = false)
        {
            expiresAt = Time.time + lifetime;
            startScale = transform.localScale;
            var preserveHeight = startScale.y < 0.18f ? 1f : scale;
            targetScale = new Vector3(startScale.x * scale, startScale.y * preserveHeight, startScale.z * scale);
            visual = GetComponent<Renderer>();
            baseColor = visual != null && visual.sharedMaterial != null ? visual.sharedMaterial.color : Color.white;
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            propertyBlock.Clear();
            propertyBlock.SetColor("_Color", baseColor);
            if (visual != null)
            {
                visual.SetPropertyBlock(propertyBlock);
            }

            primitive = primitiveType;
            pooled = shouldPool;
        }

        public void Cancel()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            Release();
        }

        private void Update()
        {
            var game = TetherfallGame.Instance;
            if (game != null && game.GameplayPaused)
            {
                expiresAt += Time.deltaTime;
                return;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 22f);
            if (visual != null)
            {
                var alpha = Mathf.Clamp01((expiresAt - Time.time) * 4f);
                propertyBlock.SetColor("_Color", new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha));
                visual.SetPropertyBlock(propertyBlock);
            }

            if (Time.time >= expiresAt)
            {
                Release();
            }
        }

        private void Release()
        {
            if (!pooled)
            {
                Destroy(gameObject);
                return;
            }

            if (!Pools.TryGetValue(primitive, out var pool))
            {
                pool = new Queue<GameObject>();
                Pools[primitive] = pool;
            }

            gameObject.SetActive(false);
            pool.Enqueue(gameObject);
        }

        private static GameObject Acquire(PrimitiveType primitiveType, string effectName)
        {
            if (Pools.TryGetValue(primitiveType, out var pool))
            {
                while (pool.Count > 0)
                {
                    var pooled = pool.Dequeue();
                    if (pooled == null)
                    {
                        continue;
                    }

                    pooled.name = effectName;
                    pooled.SetActive(true);
                    return pooled;
                }
            }

            var root = GameObject.CreatePrimitive(primitiveType);
            root.name = effectName;
            Object.Destroy(root.GetComponent<Collider>());
            return root;
        }
    }

    public static class WorldVisuals
    {
        public static void AddLabel(Transform parent, string text, Vector3 localPosition, Color color, float characterSize = 0.12f)
        {
            var labelObject = new GameObject($"Label {text}");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            var label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = characterSize;
            label.fontSize = 54;
            label.color = color;
            labelObject.AddComponent<BillboardLabel>();
        }
    }

    public sealed class BillboardLabel : MonoBehaviour
    {
        private const float VisibilityPollInterval = 0.16f;
        private static Camera cachedMainCamera;
        private static int cachedCameraFrame = -1;

        private TextMesh label;
        private Renderer labelRenderer;
        private Color baseColor;
        private float visibleDistance;
        private float visibleDistanceSquared;
        private float nextVisibilityPollAt;
        private bool isVisible = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCameraCache()
        {
            cachedMainCamera = null;
            cachedCameraFrame = -1;
        }

        private void Awake()
        {
            label = GetComponent<TextMesh>();
            labelRenderer = GetComponent<Renderer>();
            baseColor = label != null ? label.color : Color.white;
            visibleDistance = label != null && label.characterSize >= 0.12f
                ? 54f
                : label != null && label.characterSize >= 0.1f
                    ? 38f
                    : 24f;
            visibleDistanceSquared = visibleDistance * visibleDistance;
            nextVisibilityPollAt =
                Time.unscaledTime + Mathf.Abs(GetInstanceID() % 11) * (VisibilityPollInterval / 11f);
        }

        private void LateUpdate()
        {
            var mainCamera = GetMainCamera();
            if (mainCamera == null || label == null || labelRenderer == null)
            {
                return;
            }

            var toCamera = mainCamera.transform.position - transform.position;
            if (Time.unscaledTime >= nextVisibilityPollAt)
            {
                isVisible = toCamera.sqrMagnitude <= visibleDistanceSquared;
                labelRenderer.enabled = isVisible;
                nextVisibilityPollAt = Time.unscaledTime + VisibilityPollInterval;
            }

            if (!isVisible)
            {
                return;
            }

            var distance = Mathf.Sqrt(toCamera.sqrMagnitude);
            var fade = Mathf.InverseLerp(visibleDistance, visibleDistance * 0.55f, distance);
            label.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(fade));
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(toCamera);
            }
        }

        private static Camera GetMainCamera()
        {
            if (cachedCameraFrame != Time.frameCount)
            {
                cachedCameraFrame = Time.frameCount;
                if (cachedMainCamera == null || !cachedMainCamera.isActiveAndEnabled)
                {
                    cachedMainCamera = Camera.main;
                }
            }

            return cachedMainCamera;
        }
    }
}
