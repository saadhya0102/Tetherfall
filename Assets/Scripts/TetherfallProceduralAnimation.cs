using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    /// <summary>
    /// Persistent motion beneath a Pact strike. Strike motion is triggered separately.
    /// </summary>
    public enum PactProceduralMotion
    {
        Idle,
        Fly,
        Ride
    }

    public enum WayfinderTraversalMotion
    {
        Grounded,
        Airborne,
        Gliding,
        Climbing,
        Swimming,
        Mounted,
        Mantling
    }

    /// <summary>
    /// Allocation-free runtime animation entry points for Tetherfall's procedural actors.
    /// Attach is idempotent, accepts an optional pause owner, and never adds an Animator.
    /// </summary>
    public static class TetherfallProceduralAnimation
    {
        public static WayfinderProceduralAnimator AttachWayfinder(
            Transform actorRoot,
            TetherfallGame owner = null)
        {
            if (actorRoot == null)
            {
                return null;
            }

            var animator = actorRoot.GetComponent<WayfinderProceduralAnimator>();
            if (animator == null)
            {
                animator = actorRoot.gameObject.AddComponent<WayfinderProceduralAnimator>();
            }

            animator.Configure(owner, actorRoot);
            return animator;
        }

        public static WayfinderProceduralAnimator AttachWayfinder(
            TetherfallGame owner,
            Transform actorRoot)
        {
            return AttachWayfinder(actorRoot, owner);
        }

        public static PactProceduralAnimator AttachPact(
            Transform actorRoot,
            TetherfallGame owner = null)
        {
            if (actorRoot == null)
            {
                return null;
            }

            var animator = actorRoot.GetComponent<PactProceduralAnimator>();
            if (animator == null)
            {
                animator = actorRoot.gameObject.AddComponent<PactProceduralAnimator>();
            }

            animator.Configure(owner, actorRoot);
            return animator;
        }

        public static PactProceduralAnimator AttachPact(
            TetherfallGame owner,
            Transform actorRoot)
        {
            return AttachPact(actorRoot, owner);
        }

        public static BossProceduralAnimator AttachBoss(
            Transform actorRoot,
            TetherfallGame owner = null)
        {
            if (actorRoot == null)
            {
                return null;
            }

            var animator = actorRoot.GetComponent<BossProceduralAnimator>();
            if (animator == null)
            {
                animator = actorRoot.gameObject.AddComponent<BossProceduralAnimator>();
            }

            animator.Configure(owner, actorRoot);
            return animator;
        }

        public static BossProceduralAnimator AttachBoss(
            TetherfallGame owner,
            Transform actorRoot)
        {
            return AttachBoss(actorRoot, owner);
        }
    }

    /// <summary>
    /// Adds traversal poses, gait, and an anticipation/strike/follow-through layer to a Wayfinder rig.
    /// State setters only cache value types; all hierarchy discovery happens during Attach/Rebind.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    public sealed class WayfinderProceduralAnimator : MonoBehaviour
    {
        private TetherfallGame owner;
        private Transform bindingRoot;

        private TetherfallLocalPose pelvis;
        private TetherfallLocalPose chest;
        private TetherfallLocalPose head;
        private TetherfallLocalPose leftShoulder;
        private TetherfallLocalPose rightShoulder;
        private TetherfallLocalPose leftElbow;
        private TetherfallLocalPose rightElbow;
        private TetherfallLocalPose leftHip;
        private TetherfallLocalPose rightHip;
        private TetherfallLocalPose leftKnee;
        private TetherfallLocalPose rightKnee;
        private TetherfallLocalPose leftFoot;
        private TetherfallLocalPose rightFoot;
        private TetherfallLocalPose weaponSocket;
        private TetherfallLocalPose fallbackBody;

        private float targetForward;
        private float targetStrafe;
        private float forward;
        private float strafe;
        private float gaitPhase;
        private float lifeTime;
        private bool grounded = true;
        private WayfinderTraversalMotion traversalMotion =
            WayfinderTraversalMotion.Grounded;
        private float traversalPrimary;
        private float traversalSecondary;
        private float traversalProgress;
        private float airborneBlend;
        private float glidingBlend;
        private float climbingBlend;
        private float swimmingBlend;
        private float mountedBlend;
        private float mantlingBlend;

        private float attackElapsed = -1f;
        private float attackAnticipation = 0.1f;
        private float attackFollowThrough = 0.24f;
        private float attackStrength = 1f;
        private float attackSide = 1f;
        private bool hasBinding;

        public TetherfallGame Owner => owner;
        public bool IsAttacking => attackElapsed >= 0f;
        public WayfinderTraversalMotion TraversalMotion => traversalMotion;

        public static WayfinderProceduralAnimator Attach(
            Transform actorRoot,
            TetherfallGame owner = null)
        {
            return TetherfallProceduralAnimation.AttachWayfinder(actorRoot, owner);
        }

        public void SetOwner(TetherfallGame newOwner)
        {
            owner = newOwner;
        }

        /// <summary>
        /// Supplies normalized local movement. Forward and strafe are clamped to [-1, 1].
        /// Call when movement changes or once per gameplay frame.
        /// </summary>
        public void SetLocomotion(
            float forwardAmount,
            float strafeAmount,
            bool isGrounded = true)
        {
            targetForward = Mathf.Clamp(forwardAmount, -1f, 1f);
            targetStrafe = Mathf.Clamp(strafeAmount, -1f, 1f);
            grounded = isGrounded;
            if (traversalMotion == WayfinderTraversalMotion.Grounded ||
                traversalMotion == WayfinderTraversalMotion.Airborne)
            {
                traversalMotion = isGrounded
                    ? WayfinderTraversalMotion.Grounded
                    : WayfinderTraversalMotion.Airborne;
            }
        }

        /// <summary>
        /// Vector convenience overload: x is strafe and z is forward.
        /// </summary>
        public void SetLocomotion(Vector3 normalizedLocalMotion, bool isGrounded = true)
        {
            SetLocomotion(normalizedLocalMotion.z, normalizedLocalMotion.x, isGrounded);
        }

        public void StopLocomotion(bool isGrounded = true)
        {
            SetLocomotion(0f, 0f, isGrounded);
        }

        public void SetGrounded(bool isGrounded)
        {
            grounded = isGrounded;
            if (traversalMotion == WayfinderTraversalMotion.Grounded ||
                traversalMotion == WayfinderTraversalMotion.Airborne)
            {
                traversalMotion = isGrounded
                    ? WayfinderTraversalMotion.Grounded
                    : WayfinderTraversalMotion.Airborne;
            }
        }

        public void SetGroundedLocomotion(Vector3 normalizedLocalMotion)
        {
            SetLocomotion(normalizedLocalMotion, true);
            SetTraversalMotion(WayfinderTraversalMotion.Grounded);
        }

        public void SetAirborne(
            Vector3 normalizedLocalMotion,
            float normalizedVerticalVelocity)
        {
            SetLocomotion(normalizedLocalMotion, false);
            SetTraversalMotion(
                WayfinderTraversalMotion.Airborne,
                normalizedVerticalVelocity);
        }

        public void SetGliding(Vector3 normalizedLocalMotion)
        {
            SetLocomotion(normalizedLocalMotion, false);
            SetTraversalMotion(
                WayfinderTraversalMotion.Gliding,
                normalizedLocalMotion.z,
                normalizedLocalMotion.x);
        }

        public void SetClimbing(float verticalAmount, float lateralAmount)
        {
            SetLocomotion(0f, lateralAmount, false);
            SetTraversalMotion(
                WayfinderTraversalMotion.Climbing,
                verticalAmount,
                lateralAmount);
        }

        public void SetSwimming(
            Vector3 normalizedLocalMotion,
            float verticalAmount)
        {
            SetLocomotion(normalizedLocalMotion, false);
            SetTraversalMotion(
                WayfinderTraversalMotion.Swimming,
                verticalAmount,
                normalizedLocalMotion.z);
        }

        public void SetMounted(Vector3 normalizedLocalMotion)
        {
            SetLocomotion(normalizedLocalMotion, false);
            SetTraversalMotion(
                WayfinderTraversalMotion.Mounted,
                normalizedLocalMotion.z,
                normalizedLocalMotion.x);
        }

        public void SetMantling(float normalizedProgress)
        {
            StopLocomotion(false);
            SetTraversalMotion(
                WayfinderTraversalMotion.Mantling,
                0f,
                0f,
                normalizedProgress);
        }

        public void SetTraversalMotion(
            WayfinderTraversalMotion motion,
            float primaryAmount = 0f,
            float secondaryAmount = 0f,
            float normalizedProgress = 0f)
        {
            traversalMotion = motion;
            traversalPrimary = Mathf.Clamp(primaryAmount, -1f, 1f);
            traversalSecondary = Mathf.Clamp(secondaryAmount, -1f, 1f);
            traversalProgress = Mathf.Clamp01(normalizedProgress);
            grounded = motion == WayfinderTraversalMotion.Grounded;
        }

        /// <summary>
        /// Starts one complete weapon action. Mirrored attacks lead with the left side.
        /// </summary>
        public void TriggerWeaponAttack(
            float strength = 1f,
            bool mirrored = false,
            float anticipationSeconds = 0.1f,
            float followThroughSeconds = 0.24f)
        {
            attackElapsed = 0f;
            attackStrength = Mathf.Clamp(strength, 0.1f, 2f);
            attackSide = mirrored ? -1f : 1f;
            attackAnticipation = Mathf.Max(0.02f, anticipationSeconds);
            attackFollowThrough = Mathf.Max(0.05f, followThroughSeconds);
        }

        public void TriggerAttack(
            float strength = 1f,
            bool mirrored = false,
            float anticipationSeconds = 0.1f,
            float followThroughSeconds = 0.24f)
        {
            TriggerWeaponAttack(strength, mirrored, anticipationSeconds, followThroughSeconds);
        }

        public void CancelAttack()
        {
            attackElapsed = -1f;
        }

        /// <summary>
        /// Re-scans named sockets after a procedural art hierarchy has been rebuilt.
        /// Never call this every frame.
        /// </summary>
        public void RebindSockets()
        {
            ResetPose();
            BindSockets();
        }

        public void ResetPose()
        {
            pelvis.Restore();
            chest.Restore();
            head.Restore();
            leftShoulder.Restore();
            rightShoulder.Restore();
            leftElbow.Restore();
            rightElbow.Restore();
            leftHip.Restore();
            rightHip.Restore();
            leftKnee.Restore();
            rightKnee.Restore();
            leftFoot.Restore();
            rightFoot.Restore();
            weaponSocket.Restore();
            fallbackBody.Restore();
        }

        internal void Configure(TetherfallGame newOwner, Transform actorRoot)
        {
            owner = newOwner;
            if (bindingRoot != actorRoot)
            {
                ResetPose();
                bindingRoot = actorRoot;
                BindSockets();
            }
            else if (!hasBinding)
            {
                BindSockets();
            }
        }

        private void OnEnable()
        {
            if (bindingRoot == null)
            {
                bindingRoot = transform;
                BindSockets();
            }
        }

        private void OnDisable()
        {
            ResetPose();
        }

        private void Update()
        {
            if (!hasBinding || (owner != null && owner.GameplayPaused))
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            lifeTime += deltaTime;
            var locomotionBlend = TetherfallAnimationMath.Damp(14f, deltaTime);
            forward = Mathf.Lerp(forward, targetForward, locomotionBlend);
            strafe = Mathf.Lerp(strafe, targetStrafe, locomotionBlend);

            var traversalBlend = TetherfallAnimationMath.Damp(20f, deltaTime);
            airborneBlend = Mathf.Lerp(
                airborneBlend,
                traversalMotion == WayfinderTraversalMotion.Airborne ? 1f : 0f,
                traversalBlend);
            glidingBlend = Mathf.Lerp(
                glidingBlend,
                traversalMotion == WayfinderTraversalMotion.Gliding ? 1f : 0f,
                traversalBlend);
            climbingBlend = Mathf.Lerp(
                climbingBlend,
                traversalMotion == WayfinderTraversalMotion.Climbing ? 1f : 0f,
                traversalBlend);
            swimmingBlend = Mathf.Lerp(
                swimmingBlend,
                traversalMotion == WayfinderTraversalMotion.Swimming ? 1f : 0f,
                traversalBlend);
            mountedBlend = Mathf.Lerp(
                mountedBlend,
                traversalMotion == WayfinderTraversalMotion.Mounted ? 1f : 0f,
                traversalBlend);
            mantlingBlend = Mathf.Lerp(
                mantlingBlend,
                traversalMotion == WayfinderTraversalMotion.Mantling ? 1f : 0f,
                traversalBlend);

            var speed = Mathf.Clamp01(Mathf.Sqrt(forward * forward + strafe * strafe));
            if (grounded && traversalMotion == WayfinderTraversalMotion.Grounded)
            {
                gaitPhase += deltaTime * Mathf.Lerp(3f, 11.5f, speed);
                if (gaitPhase > Mathf.PI * 2f)
                {
                    gaitPhase -= Mathf.PI * 2f;
                }
            }

            var stride = Mathf.Sin(gaitPhase) * speed;
            var counterStride = Mathf.Cos(gaitPhase) * speed;
            var breathing = Mathf.Sin(lifeTime * 1.65f);
            var specialBlend = Mathf.Clamp01(
                airborneBlend +
                glidingBlend +
                climbingBlend +
                swimmingBlend +
                mountedBlend +
                mantlingBlend);
            stride *= 1f - specialBlend;
            counterStride *= 1f - specialBlend;

            var climbIntensity = Mathf.Lerp(
                0.35f,
                1f,
                Mathf.Abs(traversalPrimary));
            var climbWave =
                Mathf.Sin(lifeTime * 7.6f) * climbingBlend * climbIntensity;
            var swimWave =
                Mathf.Sin(lifeTime * 5.2f) * swimmingBlend;
            var rideWave =
                Mathf.Sin(lifeTime * 8.4f) * mountedBlend * speed;
            var mantleEase =
                TetherfallAnimationMath.Smooth01(traversalProgress);
            var mantlePull =
                Mathf.Sin(traversalProgress * Mathf.PI) * mantlingBlend;
            var mantleReach = (1f - mantleEase) * mantlingBlend;
            var mantleStep = Mathf.Sin(
                                  Mathf.Clamp01(
                                      (traversalProgress - 0.18f) / 0.74f) *
                                  Mathf.PI) *
                              mantlingBlend;
            var verticalAir = traversalPrimary * airborneBlend;

            var attackSwing = 0f;
            var attackDrive = 0f;
            if (attackElapsed >= 0f)
            {
                attackElapsed += deltaTime;
                if (attackElapsed < attackAnticipation)
                {
                    var progress = TetherfallAnimationMath.Smooth01(
                        attackElapsed / attackAnticipation);
                    attackSwing = Mathf.Lerp(0f, -34f, progress);
                    attackDrive = -0.2f * progress;
                }
                else
                {
                    var progress = (attackElapsed - attackAnticipation) / attackFollowThrough;
                    if (progress >= 1f)
                    {
                        attackElapsed = -1f;
                    }
                    else if (progress < 0.3f)
                    {
                        var strike = TetherfallAnimationMath.Smooth01(progress / 0.3f);
                        attackSwing = Mathf.Lerp(-34f, 58f, strike);
                        attackDrive = Mathf.Lerp(-0.2f, 1f, strike);
                    }
                    else
                    {
                        var recovery = TetherfallAnimationMath.Smooth01(
                            (progress - 0.3f) / 0.7f);
                        attackSwing = Mathf.Lerp(58f, 0f, recovery);
                        attackDrive = 1f - recovery;
                    }
                }
            }

            attackSwing *= attackStrength;
            attackDrive *= attackStrength;

            var bob =
                Mathf.Abs(counterStride) * 0.045f -
                airborneBlend * 0.035f +
                climbWave * 0.018f +
                swimWave * 0.026f +
                Mathf.Abs(rideWave) * 0.035f +
                mantlePull * 0.1f -
                mantleReach * 0.055f;
            var lean =
                -forward * 5f +
                airborneBlend * (7f - verticalAir * 4f) -
                glidingBlend * 13f -
                swimmingBlend * 18f +
                climbingBlend * 6f +
                mountedBlend * 3f -
                mantlePull * 14f +
                mantleReach * 8f;
            var pelvisYaw =
                -stride * 4f -
                strafe * 2.5f +
                attackSwing * attackSide * 0.12f +
                climbWave * 3f +
                swimWave * 2f;
            var chestYaw =
                stride * 5f +
                attackSwing * attackSide * 0.34f -
                climbWave * 4f -
                swimWave * 3f;
            var chestPitch =
                lean +
                breathing * 0.65f -
                attackDrive * 9f +
                rideWave * 2.5f;
            var bodyRoll =
                strafe * -5f -
                glidingBlend * traversalSecondary * 11f -
                climbingBlend * traversalSecondary * 5f +
                swimWave * 3f +
                rideWave * 2f;
            var armSwing = stride * 25f;
            var legSwing = stride * 29f;

            pelvis.ApplyPositionRotation(
                new Vector3(
                    0f,
                    bob - mountedBlend * 0.07f,
                    -mantleReach * 0.055f + swimmingBlend * 0.025f),
                new Vector3(
                    mountedBlend * 4f,
                    pelvisYaw,
                    bodyRoll * 0.55f));
            chest.ApplyRotation(new Vector3(chestPitch, chestYaw, bodyRoll));
            head.ApplyRotation(new Vector3(
                -chestPitch * 0.38f,
                -chestYaw * 0.35f,
                -bodyRoll * 0.35f));

            leftShoulder.ApplyRotation(new Vector3(
                -armSwing -
                attackSwing * 0.24f -
                airborneBlend * (10f + verticalAir * 6f) -
                glidingBlend * 18f -
                climbingBlend * 58f -
                climbWave * 36f -
                swimmingBlend * 26f -
                swimWave * 34f -
                mountedBlend * 24f +
                (-108f + mantleEase * 86f) * mantlingBlend -
                mantlePull * 12f,
                -attackSwing * attackSide * 0.12f,
                -glidingBlend * 68f -
                swimmingBlend * 48f -
                mountedBlend * 7f -
                mantleReach * 18f));
            rightShoulder.ApplyRotation(new Vector3(
                armSwing -
                attackSwing * 0.82f -
                airborneBlend * (7f + verticalAir * 4f) -
                glidingBlend * 18f -
                climbingBlend * 58f +
                climbWave * 36f -
                swimmingBlend * 26f +
                swimWave * 34f -
                mountedBlend * 24f +
                (-102f + mantleEase * 80f) * mantlingBlend +
                mantlePull * 8f,
                attackSwing * attackSide * 0.17f,
                glidingBlend * 68f +
                swimmingBlend * 48f +
                mountedBlend * 7f +
                mantleReach * 18f));
            leftElbow.ApplyRotation(new Vector3(
                Mathf.Max(0f, armSwing) * -0.32f -
                attackDrive * 12f -
                glidingBlend * 9f -
                climbingBlend * 31f +
                climbWave * 12f -
                swimmingBlend * 18f -
                mantleReach * 42f -
                mantlePull * 18f,
                0f,
                0f));
            rightElbow.ApplyRotation(new Vector3(
                Mathf.Max(0f, -armSwing) * -0.32f -
                attackDrive * 28f -
                glidingBlend * 9f -
                climbingBlend * 31f -
                climbWave * 12f -
                swimmingBlend * 18f -
                mantleReach * 38f -
                mantlePull * 14f,
                0f,
                0f));

            leftHip.ApplyRotation(new Vector3(
                legSwing -
                airborneBlend * 14f +
                glidingBlend * 9f -
                climbWave * 22f +
                swimWave * 19f +
                mountedBlend * 48f -
                mantleStep * 34f,
                0f,
                -mountedBlend * 9f));
            rightHip.ApplyRotation(new Vector3(
                -legSwing -
                airborneBlend * 6f +
                glidingBlend * 13f +
                climbWave * 22f -
                swimWave * 19f +
                mountedBlend * 48f -
                mantleStep * 10f,
                0f,
                mountedBlend * 9f));
            leftKnee.ApplyRotation(new Vector3(
                Mathf.Max(0f, -stride) * 34f +
                airborneBlend * 24f +
                glidingBlend * 14f +
                Mathf.Max(0f, climbWave) * 38f +
                Mathf.Max(0f, -swimWave) * 28f +
                mountedBlend * 68f +
                mantleStep * 72f,
                0f,
                0f));
            rightKnee.ApplyRotation(new Vector3(
                Mathf.Max(0f, stride) * 34f +
                airborneBlend * 18f +
                glidingBlend * 18f +
                Mathf.Max(0f, -climbWave) * 38f +
                Mathf.Max(0f, swimWave) * 28f +
                mountedBlend * 68f +
                mantleStep * 28f,
                0f,
                0f));
            leftFoot.ApplyRotation(new Vector3(
                -counterStride * 8f -
                glidingBlend * 10f +
                swimWave * 8f -
                mountedBlend * 18f,
                0f,
                0f));
            rightFoot.ApplyRotation(new Vector3(
                counterStride * 8f -
                glidingBlend * 10f -
                swimWave * 8f -
                mountedBlend * 18f,
                0f,
                0f));

            weaponSocket.ApplyPositionRotation(
                new Vector3(
                    attackSide * attackDrive * 0.075f,
                    attackDrive * 0.025f + mantleReach * 0.045f,
                    attackDrive * 0.08f - climbingBlend * 0.04f),
                new Vector3(
                    -attackSwing * 0.9f -
                    glidingBlend * 10f -
                    climbingBlend * 18f -
                    mantleReach * 35f,
                    attackSide * attackSwing * 0.48f,
                    attackSide * attackSwing * 0.24f));

            fallbackBody.ApplyPositionRotation(
                new Vector3(
                    0f,
                    bob - mountedBlend * 0.07f,
                    -mantleReach * 0.055f + swimmingBlend * 0.025f),
                new Vector3(chestPitch, chestYaw, bodyRoll));
        }

        private void BindSockets()
        {
            if (bindingRoot == null)
            {
                hasBinding = false;
                return;
            }

            pelvis.Capture(TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Rig Root - Pelvis",
                "Socket - Pelvis",
                "Pelvis"));
            chest.Capture(TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Rig Root - Chest",
                "Socket - Chest",
                "Chest"));
            head.Capture(TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Rig Root - Head",
                "Socket - Head",
                "Head"));
            leftShoulder.Capture(TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Rig Root - Left Shoulder",
                "Socket - Left Shoulder",
                "Left Shoulder"));
            rightShoulder.Capture(TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Rig Root - Right Shoulder",
                "Socket - Right Shoulder",
                "Right Shoulder"));
            leftElbow.Capture(TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Rig Root - Left Elbow",
                "Socket - Left Elbow",
                "Left Elbow"));
            rightElbow.Capture(TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Rig Root - Right Elbow",
                "Socket - Right Elbow",
                "Right Elbow"));
            leftHip.Capture(TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Rig Root - Left Hip",
                "Socket - Left Hip",
                "Left Hip"));
            rightHip.Capture(TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Rig Root - Right Hip",
                "Socket - Right Hip",
                "Right Hip"));
            leftKnee.Capture(TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Rig Root - Left Knee",
                "Socket - Left Knee",
                "Left Knee"));
            rightKnee.Capture(TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Rig Root - Right Knee",
                "Socket - Right Knee",
                "Right Knee"));
            leftFoot.Capture(TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Rig Root - Left Foot",
                "Socket - Left Foot",
                "Left Foot"));
            rightFoot.Capture(TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Rig Root - Right Foot",
                "Socket - Right Foot",
                "Right Foot"));

            var weapon = TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Socket - Weapon Motion",
                "Wayfinder weapon loadout",
                "Socket - Weapon",
                "Weapon Socket");
            if (weapon == null)
            {
                weapon = TetherfallAnimationSockets.FindNamed(
                    bindingRoot,
                    "Socket - Right Hand Grip",
                    "Right Hand Grip");
            }

            weaponSocket.Capture(weapon);
            ProtectGameplayRoot();

            if (!pelvis.IsBound && !chest.IsBound)
            {
                fallbackBody.Capture(TetherfallAnimationSockets.FindNamed(
                    bindingRoot,
                    "Wayfinder Body Socket",
                    "Wayfinder Coat",
                    "Body"));
            }
            else
            {
                fallbackBody.Clear();
            }

            ProtectGameplayRoot();
            hasBinding =
                pelvis.IsBound ||
                chest.IsBound ||
                head.IsBound ||
                leftShoulder.IsBound ||
                rightShoulder.IsBound ||
                leftHip.IsBound ||
                rightHip.IsBound ||
                weaponSocket.IsBound ||
                fallbackBody.IsBound;
        }

        private void ProtectGameplayRoot()
        {
            if (bindingRoot.GetComponent<Collider>() == null &&
                bindingRoot.GetComponent<CharacterController>() == null &&
                bindingRoot.GetComponent<Rigidbody>() == null)
            {
                return;
            }

            // Presentation may animate child sockets, but never the gameplay physics root.
            pelvis.ClearIfTarget(bindingRoot);
            chest.ClearIfTarget(bindingRoot);
            head.ClearIfTarget(bindingRoot);
            leftShoulder.ClearIfTarget(bindingRoot);
            rightShoulder.ClearIfTarget(bindingRoot);
            leftElbow.ClearIfTarget(bindingRoot);
            rightElbow.ClearIfTarget(bindingRoot);
            leftHip.ClearIfTarget(bindingRoot);
            rightHip.ClearIfTarget(bindingRoot);
            leftKnee.ClearIfTarget(bindingRoot);
            rightKnee.ClearIfTarget(bindingRoot);
            leftFoot.ClearIfTarget(bindingRoot);
            rightFoot.ClearIfTarget(bindingRoot);
            weaponSocket.ClearIfTarget(bindingRoot);
            fallbackBody.ClearIfTarget(bindingRoot);
        }
    }

    /// <summary>
    /// Procedural idle, flight, mounted gait, and transient strike motion for Pact art.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    public sealed class PactProceduralAnimator : MonoBehaviour
    {
        private static readonly string[] WingTokens =
        {
            "wing",
            "swept feather",
            "copper quill",
            "veil fin"
        };

        private static readonly string[] ForelegTokens =
        {
            "foreleg",
            "fore leg",
            "fore-root",
            "fore root",
            "soot talon"
        };

        private static readonly string[] HindlegTokens =
        {
            "hindleg",
            "hind leg",
            "hind-root",
            "hind root"
        };

        private TetherfallGame owner;
        private Transform bindingRoot;
        private PactProceduralMotion motion;
        private float motionIntensity = 1f;
        private float phase;
        private float lifeTime;

        private TetherfallLocalPose motionRoot;
        private TetherfallLocalPose body;
        private TetherfallLocalPose head;
        private TetherfallLocalPose[] leftWings = Array.Empty<TetherfallLocalPose>();
        private TetherfallLocalPose[] rightWings = Array.Empty<TetherfallLocalPose>();
        private TetherfallLocalPose[] forelegs = Array.Empty<TetherfallLocalPose>();
        private TetherfallLocalPose[] hindlegs = Array.Empty<TetherfallLocalPose>();

        private float strikeElapsed = -1f;
        private float strikeAnticipation = 0.08f;
        private float strikeFollowThrough = 0.24f;
        private float strikeStrength = 1f;
        private bool hasBinding;

        public TetherfallGame Owner => owner;
        public PactProceduralMotion Motion => motion;
        public bool IsStriking => strikeElapsed >= 0f;

        public static PactProceduralAnimator Attach(
            Transform actorRoot,
            TetherfallGame owner = null)
        {
            return TetherfallProceduralAnimation.AttachPact(actorRoot, owner);
        }

        public void SetOwner(TetherfallGame newOwner)
        {
            owner = newOwner;
        }

        public void SetMotion(PactProceduralMotion newMotion, float intensity = 1f)
        {
            motion = newMotion == PactProceduralMotion.Fly ||
                     newMotion == PactProceduralMotion.Ride
                ? newMotion
                : PactProceduralMotion.Idle;
            motionIntensity = Mathf.Clamp01(intensity);
        }

        public void SetIdle()
        {
            SetMotion(PactProceduralMotion.Idle);
        }

        public void SetFlying(bool flying, float intensity = 1f)
        {
            SetMotion(flying ? PactProceduralMotion.Fly : PactProceduralMotion.Idle, intensity);
        }

        public void SetRiding(bool riding, float normalizedSpeed = 1f)
        {
            SetMotion(riding ? PactProceduralMotion.Ride : PactProceduralMotion.Idle, normalizedSpeed);
        }

        public void SetRideMotion(float normalizedSpeed)
        {
            SetMotion(PactProceduralMotion.Ride, normalizedSpeed);
        }

        public void TriggerStrike(
            float strength = 1f,
            float anticipationSeconds = 0.08f,
            float followThroughSeconds = 0.24f)
        {
            strikeElapsed = 0f;
            strikeStrength = Mathf.Clamp(strength, 0.1f, 2f);
            strikeAnticipation = Mathf.Max(0.02f, anticipationSeconds);
            strikeFollowThrough = Mathf.Max(0.05f, followThroughSeconds);
        }

        public void CancelStrike()
        {
            strikeElapsed = -1f;
        }

        public void RebindSockets()
        {
            ResetPose();
            BindSockets();
        }

        public void ResetPose()
        {
            motionRoot.Restore();
            body.Restore();
            head.Restore();
            TetherfallAnimationSockets.Restore(leftWings);
            TetherfallAnimationSockets.Restore(rightWings);
            TetherfallAnimationSockets.Restore(forelegs);
            TetherfallAnimationSockets.Restore(hindlegs);
        }

        internal void Configure(TetherfallGame newOwner, Transform actorRoot)
        {
            owner = newOwner;
            if (bindingRoot != actorRoot)
            {
                ResetPose();
                bindingRoot = actorRoot;
                BindSockets();
            }
            else if (!hasBinding)
            {
                BindSockets();
            }
        }

        private void OnEnable()
        {
            if (bindingRoot == null)
            {
                bindingRoot = transform;
                BindSockets();
            }
        }

        private void OnDisable()
        {
            ResetPose();
        }

        private void Update()
        {
            if (!hasBinding || (owner != null && owner.GameplayPaused))
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            lifeTime += deltaTime;

            var phaseRate = 1.8f;
            var bob = 0f;
            var pitch = 0f;
            var roll = 0f;
            var wingFlap = 0f;
            var forelegSwing = 0f;
            var hindlegSwing = 0f;

            switch (motion)
            {
                case PactProceduralMotion.Fly:
                    phaseRate = Mathf.Lerp(7.5f, 13f, motionIntensity);
                    break;
                case PactProceduralMotion.Ride:
                    phaseRate = Mathf.Lerp(2.2f, 9.5f, motionIntensity);
                    break;
            }

            phase += deltaTime * phaseRate;
            if (phase > Mathf.PI * 2f)
            {
                phase -= Mathf.PI * 2f;
            }

            var wave = Mathf.Sin(phase);
            var counterWave = Mathf.Cos(phase);
            switch (motion)
            {
                case PactProceduralMotion.Fly:
                    bob = counterWave * Mathf.Lerp(0.025f, 0.085f, motionIntensity);
                    pitch = -6f + wave * 2.5f;
                    roll = counterWave * 3.5f;
                    wingFlap = wave * Mathf.Lerp(16f, 34f, motionIntensity);
                    forelegSwing = 15f;
                    hindlegSwing = 20f;
                    break;

                case PactProceduralMotion.Ride:
                    bob = Mathf.Abs(counterWave) * 0.07f * motionIntensity;
                    pitch = wave * 3.5f * motionIntensity;
                    roll = counterWave * 2f * motionIntensity;
                    forelegSwing = wave * 17f * motionIntensity;
                    hindlegSwing = -wave * 17f * motionIntensity;
                    break;

                default:
                    bob = Mathf.Sin(lifeTime * 1.7f) * 0.022f;
                    pitch = Mathf.Sin(lifeTime * 1.25f) * 1.25f;
                    roll = Mathf.Sin(lifeTime * 0.75f) * 0.8f;
                    wingFlap = Mathf.Sin(lifeTime * 1.4f) * 2.2f;
                    break;
            }

            var strikePitch = 0f;
            var strikeLunge = 0f;
            if (strikeElapsed >= 0f)
            {
                strikeElapsed += deltaTime;
                if (strikeElapsed < strikeAnticipation)
                {
                    var progress = TetherfallAnimationMath.Smooth01(
                        strikeElapsed / strikeAnticipation);
                    strikePitch = Mathf.Lerp(0f, 14f, progress);
                    strikeLunge = Mathf.Lerp(0f, -0.08f, progress);
                }
                else
                {
                    var progress = (strikeElapsed - strikeAnticipation) / strikeFollowThrough;
                    if (progress >= 1f)
                    {
                        strikeElapsed = -1f;
                    }
                    else if (progress < 0.3f)
                    {
                        var release = TetherfallAnimationMath.Smooth01(progress / 0.3f);
                        strikePitch = Mathf.Lerp(14f, -18f, release);
                        strikeLunge = Mathf.Lerp(-0.08f, 0.24f, release);
                    }
                    else
                    {
                        var recovery = TetherfallAnimationMath.Smooth01(
                            (progress - 0.3f) / 0.7f);
                        strikePitch = Mathf.Lerp(-18f, 0f, recovery);
                        strikeLunge = Mathf.Lerp(0.24f, 0f, recovery);
                    }
                }
            }

            strikePitch *= strikeStrength;
            strikeLunge *= strikeStrength;

            motionRoot.ApplyPositionRotation(
                new Vector3(0f, bob, strikeLunge),
                new Vector3(pitch + strikePitch, 0f, roll));
            body.ApplyRotation(new Vector3(
                pitch * 0.42f + strikePitch * 0.38f,
                0f,
                -roll * 0.4f));
            head.ApplyRotation(new Vector3(
                -pitch * 0.55f - strikePitch * 0.22f,
                0f,
                roll * 0.3f));

            TetherfallAnimationSockets.ApplyRotation(
                leftWings,
                new Vector3(strikePitch * -0.16f, 0f, wingFlap + strikePitch * 0.22f));
            TetherfallAnimationSockets.ApplyRotation(
                rightWings,
                new Vector3(strikePitch * -0.16f, 0f, -wingFlap - strikePitch * 0.22f));
            TetherfallAnimationSockets.ApplyRotation(
                forelegs,
                new Vector3(forelegSwing - strikePitch * 0.35f, 0f, 0f));
            TetherfallAnimationSockets.ApplyRotation(
                hindlegs,
                new Vector3(hindlegSwing + strikePitch * 0.18f, 0f, 0f));
        }

        private void BindSockets()
        {
            if (bindingRoot == null)
            {
                hasBinding = false;
                return;
            }

            var rootSocket = TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Socket - Pact Motion",
                "Pact Motion Socket",
                "Socket - Body Motion");
            if (rootSocket == null)
            {
                rootSocket = TetherfallAnimationSockets.FindStartingWith(
                    bindingRoot,
                    "Tetherfall Echosworn Art Overhaul");
            }

            if (rootSocket == null && TetherfallAnimationSockets.IsPresentationRoot(bindingRoot))
            {
                rootSocket = bindingRoot;
            }

            motionRoot.Capture(rootSocket);

            var bodyTarget = TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Socket - Pact Body",
                "Pact Body Socket",
                "Bramblet split-seed body",
                "Slateback low carrying body",
                "Mistling rain-seed core",
                "Cinderwing cinder body");
            if (bodyTarget == null)
            {
                bodyTarget = TetherfallAnimationSockets.FindContaining(bindingRoot, "body");
            }

            body.Capture(bodyTarget);

            var headTarget = TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Socket - Pact Head",
                "Pact Head Socket",
                "Slateback broad head",
                "Cinderwing hooded head",
                "Bramblet seed face",
                "Mistling faceted veil");
            if (headTarget == null)
            {
                headTarget = TetherfallAnimationSockets.FindContaining(bindingRoot, "head");
            }

            head.Capture(headTarget);

            leftWings = TetherfallAnimationSockets.CapturePreferredGroup(
                TetherfallAnimationSockets.FindNamed(
                    bindingRoot,
                    "Socket - Left Wing",
                    "Left Wing Socket"),
                bindingRoot,
                "left",
                WingTokens);
            rightWings = TetherfallAnimationSockets.CapturePreferredGroup(
                TetherfallAnimationSockets.FindNamed(
                    bindingRoot,
                    "Socket - Right Wing",
                    "Right Wing Socket"),
                bindingRoot,
                "right",
                WingTokens);
            forelegs = TetherfallAnimationSockets.CapturePreferredGroup(
                TetherfallAnimationSockets.FindNamed(
                    bindingRoot,
                    "Socket - Forelegs",
                    "Forelegs Socket"),
                bindingRoot,
                null,
                ForelegTokens);
            hindlegs = TetherfallAnimationSockets.CapturePreferredGroup(
                TetherfallAnimationSockets.FindNamed(
                    bindingRoot,
                    "Socket - Hindlegs",
                    "Hindlegs Socket"),
                bindingRoot,
                null,
                HindlegTokens);

            if (body.Target == motionRoot.Target)
            {
                body.Clear();
            }

            if (head.Target == motionRoot.Target)
            {
                head.Clear();
            }

            hasBinding =
                motionRoot.IsBound ||
                body.IsBound ||
                head.IsBound ||
                leftWings.Length > 0 ||
                rightWings.Length > 0 ||
                forelegs.Length > 0 ||
                hindlegs.Length > 0;
        }
    }

    /// <summary>
    /// Boss presentation layer with an externally triggered windup and impact response.
    /// Gameplay roots with colliders are never moved; named visual sockets/accessories are used instead.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    public sealed class BossProceduralAnimator : MonoBehaviour
    {
        private TetherfallGame owner;
        private Transform bindingRoot;
        private TetherfallLocalPose motionRoot;
        private TetherfallLocalPose core;
        private TetherfallLocalPose crown;
        private TetherfallLocalPose leftLimb;
        private TetherfallLocalPose rightLimb;

        private float lifeTime;
        private bool windingUp;
        private bool manualWindup;
        private float windupElapsed;
        private float windupDuration = 0.65f;
        private float windupProgress;
        private float windupStrength = 1f;

        private bool impacting;
        private float impactElapsed;
        private float impactDuration = 0.28f;
        private float impactStrength = 1f;
        private bool hasBinding;

        public TetherfallGame Owner => owner;
        public bool IsWindingUp => windingUp;
        public bool IsImpacting => impacting;

        public static BossProceduralAnimator Attach(
            Transform actorRoot,
            TetherfallGame owner = null)
        {
            return TetherfallProceduralAnimation.AttachBoss(actorRoot, owner);
        }

        public void SetOwner(TetherfallGame newOwner)
        {
            owner = newOwner;
        }

        /// <summary>
        /// Starts a timed windup and holds its final pose until impact or cancellation.
        /// </summary>
        public void BeginWindup(float durationSeconds = 0.65f, float strength = 1f)
        {
            windingUp = true;
            manualWindup = false;
            windupElapsed = 0f;
            windupDuration = Mathf.Max(0.05f, durationSeconds);
            windupProgress = 0f;
            windupStrength = Mathf.Clamp(strength, 0.1f, 2f);
            impacting = false;
        }

        /// <summary>
        /// Directly drives a telegraph pose when gameplay already owns the timeline.
        /// </summary>
        public void SetWindupProgress(float normalizedProgress, float strength = 1f)
        {
            windingUp = true;
            manualWindup = true;
            windupProgress = Mathf.Clamp01(normalizedProgress);
            windupStrength = Mathf.Clamp(strength, 0.1f, 2f);
            impacting = false;
        }

        public void CancelWindup()
        {
            windingUp = false;
            manualWindup = false;
            windupProgress = 0f;
        }

        public void TriggerImpact(float strength = 1f, float durationSeconds = 0.28f)
        {
            CancelWindup();
            impacting = true;
            impactElapsed = 0f;
            impactDuration = Mathf.Max(0.08f, durationSeconds);
            impactStrength = Mathf.Clamp(strength, 0.1f, 2f);
        }

        public void ResetState()
        {
            CancelWindup();
            impacting = false;
            impactElapsed = 0f;
            ResetPose();
        }

        public void RebindSockets()
        {
            ResetPose();
            BindSockets();
        }

        public void ResetPose()
        {
            motionRoot.Restore();
            core.Restore();
            crown.Restore();
            leftLimb.Restore();
            rightLimb.Restore();
        }

        internal void Configure(TetherfallGame newOwner, Transform actorRoot)
        {
            owner = newOwner;
            if (bindingRoot != actorRoot)
            {
                ResetPose();
                bindingRoot = actorRoot;
                BindSockets();
            }
            else if (!hasBinding)
            {
                BindSockets();
            }
        }

        private void OnEnable()
        {
            if (bindingRoot == null)
            {
                bindingRoot = transform;
                BindSockets();
            }
        }

        private void OnDisable()
        {
            ResetPose();
        }

        private void Update()
        {
            if (!hasBinding || (owner != null && owner.GameplayPaused))
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            lifeTime += deltaTime;
            if (windingUp && !manualWindup)
            {
                windupElapsed += deltaTime;
                windupProgress = Mathf.Clamp01(windupElapsed / windupDuration);
            }

            var impactProgress = 0f;
            if (impacting)
            {
                impactElapsed += deltaTime;
                impactProgress = Mathf.Clamp01(impactElapsed / impactDuration);
                if (impactElapsed >= impactDuration)
                {
                    impacting = false;
                }
            }

            var idle = Mathf.Sin(lifeTime * 1.15f);
            var windup = windingUp
                ? TetherfallAnimationMath.Smooth01(windupProgress) * windupStrength
                : 0f;

            var slam = 0f;
            var rebound = 0f;
            if (impacting)
            {
                if (impactProgress < 0.2f)
                {
                    slam = TetherfallAnimationMath.Smooth01(impactProgress / 0.2f);
                }
                else
                {
                    slam = 1f - TetherfallAnimationMath.Smooth01(
                        (impactProgress - 0.2f) / 0.8f);
                }

                rebound = Mathf.Sin(impactProgress * Mathf.PI * 5f) *
                          (1f - impactProgress) *
                          impactStrength;
                slam *= impactStrength;
            }

            motionRoot.ApplyPositionRotationScale(
                new Vector3(0f, idle * 0.015f - windup * 0.08f - slam * 0.14f, 0f),
                new Vector3(windup * 11f - slam * 15f + rebound * 3.5f, 0f, rebound * 2f),
                new Vector3(
                    1f + windup * 0.025f + slam * 0.07f,
                    1f - windup * 0.055f - slam * 0.11f,
                    1f + windup * 0.025f + slam * 0.07f));
            core.ApplyPositionRotationScale(
                new Vector3(0f, windup * 0.035f - slam * 0.055f, 0f),
                new Vector3(rebound * -3f, windup * 18f, rebound * 3f),
                Vector3.one * (1f + windup * 0.12f + slam * 0.16f));
            crown.ApplyPositionRotation(
                new Vector3(0f, windup * 0.04f + rebound * 0.018f, 0f),
                new Vector3(-windup * 13f + rebound * 5f, windup * 7f, rebound * 4f));
            leftLimb.ApplyPositionRotation(
                new Vector3(-windup * 0.045f - slam * 0.08f, 0f, 0f),
                new Vector3(windup * -24f + slam * 28f, 0f, windup * 12f + rebound * 5f));
            rightLimb.ApplyPositionRotation(
                new Vector3(windup * 0.045f + slam * 0.08f, 0f, 0f),
                new Vector3(windup * -24f + slam * 28f, 0f, -windup * 12f - rebound * 5f));
        }

        private void BindSockets()
        {
            if (bindingRoot == null)
            {
                hasBinding = false;
                return;
            }

            var rootSocket = TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Socket - Boss Motion",
                "Boss Motion Socket",
                "Socket - Boss Body",
                "Boss Visual");
            if (rootSocket == null && TetherfallAnimationSockets.IsPresentationRoot(bindingRoot))
            {
                rootSocket = bindingRoot;
            }

            motionRoot.Capture(rootSocket);

            var coreTarget = TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Socket - Boss Core",
                "Boss Core Socket",
                "Warden core",
                "Boss Core");
            if (coreTarget == null)
            {
                coreTarget = TetherfallAnimationSockets.FindContaining(bindingRoot, "core");
            }

            core.Capture(coreTarget);

            var crownTarget = TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Socket - Boss Crown",
                "Boss Crown Socket",
                "Warden crown",
                "Boss Crown");
            if (crownTarget == null)
            {
                crownTarget = TetherfallAnimationSockets.FindContaining(bindingRoot, "crown");
            }

            crown.Capture(crownTarget);

            var leftTarget = TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Socket - Boss Left Limb",
                "Socket - Left Arm",
                "Warden left prism",
                "Boss Left Limb");
            if (leftTarget == null)
            {
                leftTarget = TetherfallAnimationSockets.FindContaining(
                    bindingRoot,
                    "left",
                    "prism");
            }

            leftLimb.Capture(leftTarget);

            var rightTarget = TetherfallAnimationSockets.FindNamed(
                bindingRoot,
                "Socket - Boss Right Limb",
                "Socket - Right Arm",
                "Warden right prism",
                "Boss Right Limb");
            if (rightTarget == null)
            {
                rightTarget = TetherfallAnimationSockets.FindContaining(
                    bindingRoot,
                    "right",
                    "prism");
            }

            rightLimb.Capture(rightTarget);

            if (core.Target == motionRoot.Target)
            {
                core.Clear();
            }

            if (crown.Target == motionRoot.Target)
            {
                crown.Clear();
            }

            if (leftLimb.Target == motionRoot.Target)
            {
                leftLimb.Clear();
            }

            if (rightLimb.Target == motionRoot.Target)
            {
                rightLimb.Clear();
            }

            hasBinding =
                motionRoot.IsBound ||
                core.IsBound ||
                crown.IsBound ||
                leftLimb.IsBound ||
                rightLimb.IsBound;
        }
    }

    internal struct TetherfallLocalPose
    {
        private Transform target;
        private Vector3 localPosition;
        private Quaternion localRotation;
        private Vector3 localScale;

        public Transform Target => target;
        public bool IsBound => target != null;

        public void Capture(Transform newTarget)
        {
            target = newTarget;
            if (target == null)
            {
                localPosition = Vector3.zero;
                localRotation = Quaternion.identity;
                localScale = Vector3.one;
                return;
            }

            localPosition = target.localPosition;
            localRotation = target.localRotation;
            localScale = target.localScale;
        }

        public void Clear()
        {
            target = null;
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
            localScale = Vector3.one;
        }

        public void ClearIfTarget(Transform unsafeTarget)
        {
            if (target == unsafeTarget)
            {
                Clear();
            }
        }

        public void Restore()
        {
            if (target == null)
            {
                return;
            }

            target.localPosition = localPosition;
            target.localRotation = localRotation;
            target.localScale = localScale;
        }

        public void ApplyRotation(Vector3 eulerOffset)
        {
            if (target != null)
            {
                target.localRotation = localRotation * Quaternion.Euler(eulerOffset);
            }
        }

        public void ApplyPositionRotation(Vector3 positionOffset, Vector3 eulerOffset)
        {
            if (target == null)
            {
                return;
            }

            target.localPosition = localPosition + positionOffset;
            target.localRotation = localRotation * Quaternion.Euler(eulerOffset);
        }

        public void ApplyPositionRotationScale(
            Vector3 positionOffset,
            Vector3 eulerOffset,
            Vector3 scaleMultiplier)
        {
            if (target == null)
            {
                return;
            }

            target.localPosition = localPosition + positionOffset;
            target.localRotation = localRotation * Quaternion.Euler(eulerOffset);
            target.localScale = Vector3.Scale(localScale, scaleMultiplier);
        }
    }

    internal static class TetherfallAnimationMath
    {
        public static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        public static float Damp(float sharpness, float deltaTime)
        {
            return 1f - Mathf.Exp(-sharpness * deltaTime);
        }
    }

    internal static class TetherfallAnimationSockets
    {
        public static Transform FindNamed(Transform root, params string[] names)
        {
            if (root == null || names == null || names.Length == 0)
            {
                return null;
            }

            for (var nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                if (string.Equals(root.name, names[nameIndex], StringComparison.OrdinalIgnoreCase))
                {
                    return root;
                }
            }

            for (var childIndex = 0; childIndex < root.childCount; childIndex++)
            {
                var found = FindNamed(root.GetChild(childIndex), names);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public static Transform FindStartingWith(Transform root, string prefix)
        {
            if (root == null || string.IsNullOrEmpty(prefix))
            {
                return null;
            }

            if (root.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            for (var childIndex = 0; childIndex < root.childCount; childIndex++)
            {
                var found = FindStartingWith(root.GetChild(childIndex), prefix);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public static Transform FindContaining(
            Transform root,
            string required,
            string alsoRequired = null)
        {
            if (root == null || string.IsNullOrEmpty(required))
            {
                return null;
            }

            if (Contains(root.name, required) &&
                (string.IsNullOrEmpty(alsoRequired) || Contains(root.name, alsoRequired)))
            {
                return root;
            }

            for (var childIndex = 0; childIndex < root.childCount; childIndex++)
            {
                var found = FindContaining(root.GetChild(childIndex), required, alsoRequired);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public static TetherfallLocalPose[] CapturePreferredGroup(
            Transform preferredSocket,
            Transform root,
            string required,
            string[] alternatives)
        {
            if (preferredSocket != null)
            {
                var preferred = new TetherfallLocalPose[1];
                preferred[0].Capture(preferredSocket);
                return preferred;
            }

            var matches = new List<TetherfallLocalPose>(4);
            CaptureMatches(root, required, alternatives, matches);
            return matches.ToArray();
        }

        public static void Restore(TetherfallLocalPose[] poses)
        {
            if (poses == null)
            {
                return;
            }

            for (var index = 0; index < poses.Length; index++)
            {
                poses[index].Restore();
            }
        }

        public static void ApplyRotation(
            TetherfallLocalPose[] poses,
            Vector3 eulerOffset)
        {
            if (poses == null)
            {
                return;
            }

            for (var index = 0; index < poses.Length; index++)
            {
                poses[index].ApplyRotation(eulerOffset);
            }
        }

        public static bool IsPresentationRoot(Transform root)
        {
            if (root == null ||
                root.GetComponent<Collider>() != null ||
                root.GetComponent<CharacterController>() != null ||
                root.GetComponent<Rigidbody>() != null)
            {
                return false;
            }

            var objectName = root.name;
            return Contains(objectName, "visual") ||
                   Contains(objectName, "art") ||
                   Contains(objectName, "model") ||
                   Contains(objectName, "rig") ||
                   Contains(objectName, "socket");
        }

        private static void CaptureMatches(
            Transform root,
            string required,
            string[] alternatives,
            List<TetherfallLocalPose> matches)
        {
            if (root == null)
            {
                return;
            }

            var requiredMatches = string.IsNullOrEmpty(required) || Contains(root.name, required);
            if (requiredMatches && ContainsAny(root.name, alternatives))
            {
                var pose = new TetherfallLocalPose();
                pose.Capture(root);
                matches.Add(pose);
            }

            for (var childIndex = 0; childIndex < root.childCount; childIndex++)
            {
                CaptureMatches(root.GetChild(childIndex), required, alternatives, matches);
            }
        }

        private static bool ContainsAny(string value, string[] candidates)
        {
            if (candidates == null)
            {
                return false;
            }

            for (var index = 0; index < candidates.Length; index++)
            {
                if (Contains(value, candidates[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(string value, string fragment)
        {
            return !string.IsNullOrEmpty(value) &&
                   !string.IsNullOrEmpty(fragment) &&
                   value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
