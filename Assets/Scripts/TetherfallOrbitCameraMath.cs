using System;
using UnityEngine;

namespace Tetherfall
{
    public enum TetherfallOrbitTraversalMode
    {
        Default,
        Mounted,
        Climb,
        Swim,
        Glide,
        Mantle
    }

    /// <summary>
    /// Immutable tuning for one traversal branch of the shared orbit pipeline.
    /// Pivot height is intentionally independent from the rotated shoulder/boom offset.
    /// </summary>
    public readonly struct TetherfallOrbitCameraProfile
    {
        public TetherfallOrbitTraversalMode Mode { get; }
        public float PivotHeight { get; }
        public float BoomDistance { get; }
        public float ShoulderOffset { get; }
        public float DefaultPitch { get; }
        public float MinimumPitch { get; }
        public float MaximumPitch { get; }
        public float FollowSharpness { get; }
        public float CollisionCompressionSharpness { get; }
        public float CollisionReleaseSharpness { get; }
        public float MinimumCollisionDistance { get; }
        public float CollisionSkin { get; }
        public float CollisionDeadband { get; }
        public float FocusEngageLookMagnitude { get; }
        public float FocusBreakLookMagnitude { get; }
        public float FocusYawDegreesPerSecond { get; }

        public TetherfallOrbitCameraProfile(
            TetherfallOrbitTraversalMode mode,
            float pivotHeight,
            float boomDistance,
            float shoulderOffset,
            float defaultPitch,
            float minimumPitch,
            float maximumPitch,
            float followSharpness,
            float collisionCompressionSharpness,
            float collisionReleaseSharpness,
            float minimumCollisionDistance,
            float collisionSkin,
            float collisionDeadband,
            float focusEngageLookMagnitude,
            float focusBreakLookMagnitude,
            float focusYawDegreesPerSecond)
        {
            Mode = mode;
            PivotHeight = FiniteOr(pivotHeight, 1.15f);
            MinimumCollisionDistance = Mathf.Max(
                0.01f,
                FiniteOr(minimumCollisionDistance, 0.58f));
            BoomDistance = Mathf.Max(
                MinimumCollisionDistance,
                FiniteOr(boomDistance, 7.4f));
            ShoulderOffset = FiniteOr(shoulderOffset, 0f);

            var safeMinimumPitch = FiniteOr(minimumPitch, -8f);
            var safeMaximumPitch = FiniteOr(maximumPitch, 54f);
            if (safeMaximumPitch < safeMinimumPitch)
            {
                var swap = safeMinimumPitch;
                safeMinimumPitch = safeMaximumPitch;
                safeMaximumPitch = swap;
            }

            MinimumPitch = safeMinimumPitch;
            MaximumPitch = safeMaximumPitch;
            DefaultPitch = Mathf.Clamp(
                FiniteOr(defaultPitch, 19f),
                MinimumPitch,
                MaximumPitch);
            FollowSharpness = Mathf.Max(0f, FiniteOr(followSharpness, 13f));
            CollisionCompressionSharpness = Mathf.Max(
                0f,
                FiniteOr(collisionCompressionSharpness, 32f));
            CollisionReleaseSharpness = Mathf.Max(
                0f,
                FiniteOr(collisionReleaseSharpness, 7.5f));
            CollisionSkin = Mathf.Max(0f, FiniteOr(collisionSkin, 0.24f));
            CollisionDeadband = Mathf.Max(
                0.0001f,
                FiniteOr(collisionDeadband, 0.1f));

            var safeEngageMagnitude = Mathf.Max(
                0f,
                FiniteOr(focusEngageLookMagnitude, 0.06f));
            FocusEngageLookMagnitude = safeEngageMagnitude;
            FocusBreakLookMagnitude = Mathf.Max(
                safeEngageMagnitude,
                FiniteOr(focusBreakLookMagnitude, 0.28f));
            FocusYawDegreesPerSecond = Mathf.Max(
                0f,
                FiniteOr(focusYawDegreesPerSecond, 46f));
        }

        private static float FiniteOr(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
        }
    }

    /// <summary>
    /// User look tuning. Mouse sensitivities are degrees per mouse delta; stick
    /// sensitivities are degrees per second and therefore require deltaTime.
    /// </summary>
    public readonly struct TetherfallOrbitLookSettings
    {
        public float MouseYawSensitivity { get; }
        public float MousePitchSensitivity { get; }
        public float StickYawDegreesPerSecond { get; }
        public float StickPitchDegreesPerSecond { get; }
        public bool InvertHorizontal { get; }
        public bool InvertVertical { get; }

        public TetherfallOrbitLookSettings(
            float mouseYawSensitivity,
            float mousePitchSensitivity,
            float stickYawDegreesPerSecond,
            float stickPitchDegreesPerSecond,
            bool invertHorizontal,
            bool invertVertical)
        {
            MouseYawSensitivity = NonNegativeFiniteOr(mouseYawSensitivity, 3.5f);
            MousePitchSensitivity = NonNegativeFiniteOr(mousePitchSensitivity, 2.8f);
            StickYawDegreesPerSecond = NonNegativeFiniteOr(
                stickYawDegreesPerSecond,
                180f);
            StickPitchDegreesPerSecond = NonNegativeFiniteOr(
                stickPitchDegreesPerSecond,
                135f);
            InvertHorizontal = invertHorizontal;
            InvertVertical = invertVertical;
        }

        public static TetherfallOrbitLookSettings Default =>
            new TetherfallOrbitLookSettings(3.5f, 2.8f, 180f, 135f, false, false);

        private static float NonNegativeFiniteOr(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : Mathf.Max(0f, value);
        }
    }

    /// <summary>
    /// All temporal state required by the pure orbit helpers. It contains no
    /// Transform, Camera, target, physics, input, or clock reference.
    /// </summary>
    public struct TetherfallOrbitCameraState
    {
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public Vector3 FollowPivot { get; set; }
        public float CollisionBoomDistance { get; set; }
        public bool CollisionBoomInitialized { get; set; }
        public bool CollisionConstrained { get; set; }
        public bool FocusAssistEngaged { get; set; }
        public float CursorRelockSuppressionUntil { get; set; }

        public Vector3 PlanarForward => TetherfallOrbitCameraMath.PlanarForward(Yaw);
        public Vector3 PlanarRight => TetherfallOrbitCameraMath.PlanarRight(Yaw);
    }

    public readonly struct TetherfallOrbitCameraPose
    {
        public Vector3 Pivot { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 PlanarForward { get; }
        public Vector3 PlanarRight { get; }

        public TetherfallOrbitCameraPose(
            Vector3 pivot,
            Vector3 position,
            Quaternion rotation,
            Vector3 planarForward,
            Vector3 planarRight)
        {
            Pivot = pivot;
            Position = position;
            Rotation = rotation;
            PlanarForward = planarForward;
            PlanarRight = planarRight;
        }
    }

    /// <summary>
    /// Hitstop freezes target following without discarding valid look intent.
    /// A broader gameplay pause can independently disable look input.
    /// </summary>
    public readonly struct TetherfallOrbitHitstopPolicy
    {
        public bool IntegrateLookIntent { get; }
        public bool HoldFollowPivot { get; }

        public TetherfallOrbitHitstopPolicy(
            bool integrateLookIntent,
            bool holdFollowPivot)
        {
            IntegrateLookIntent = integrateLookIntent;
            HoldFollowPivot = holdFollowPivot;
        }
    }

    /// <summary>
    /// Allocation-free, deterministic math for the single third-person orbit pipeline.
    /// Callers own input polling, target selection, physics casts, and transform writes.
    /// </summary>
    public static class TetherfallOrbitCameraMath
    {
        public const float DefaultCursorRelockSuppressionSeconds = 0.075f;
        public const float DefaultFarClipMinimum = 512f;
        public const float DefaultFarClipMargin = 128f;

        private const float DirectionEpsilon = 0.000001f;

        public static TetherfallOrbitCameraProfile GetProfile(
            TetherfallOrbitTraversalMode mode)
        {
            switch (mode)
            {
                case TetherfallOrbitTraversalMode.Mounted:
                    return new TetherfallOrbitCameraProfile(
                        mode,
                        1.6f,
                        9.2f,
                        0.72f,
                        16f,
                        -10f,
                        48f,
                        10f,
                        36f,
                        6.5f,
                        0.72f,
                        0.28f,
                        0.12f,
                        0.06f,
                        0.28f,
                        42f);
                case TetherfallOrbitTraversalMode.Climb:
                    return new TetherfallOrbitCameraProfile(
                        mode,
                        1.3f,
                        6.2f,
                        0.28f,
                        22f,
                        2f,
                        58f,
                        17f,
                        36f,
                        7f,
                        0.58f,
                        0.22f,
                        0.09f,
                        0.05f,
                        0.26f,
                        48f);
                case TetherfallOrbitTraversalMode.Swim:
                    return new TetherfallOrbitCameraProfile(
                        mode,
                        1.45f,
                        6.5f,
                        0.48f,
                        14f,
                        -4f,
                        44f,
                        9f,
                        32f,
                        6f,
                        0.62f,
                        0.24f,
                        0.11f,
                        0.06f,
                        0.28f,
                        42f);
                case TetherfallOrbitTraversalMode.Glide:
                    return new TetherfallOrbitCameraProfile(
                        mode,
                        1.35f,
                        8.4f,
                        0.58f,
                        12f,
                        -12f,
                        46f,
                        8f,
                        34f,
                        5.5f,
                        0.68f,
                        0.26f,
                        0.12f,
                        0.06f,
                        0.3f,
                        38f);
                case TetherfallOrbitTraversalMode.Mantle:
                    return new TetherfallOrbitCameraProfile(
                        mode,
                        1.25f,
                        6.4f,
                        0.35f,
                        20f,
                        -2f,
                        54f,
                        20f,
                        40f,
                        8f,
                        0.58f,
                        0.22f,
                        0.08f,
                        0.05f,
                        0.26f,
                        50f);
                default:
                    return new TetherfallOrbitCameraProfile(
                        TetherfallOrbitTraversalMode.Default,
                        1.15f,
                        7.4f,
                        0.55f,
                        19f,
                        -8f,
                        54f,
                        13f,
                        34f,
                        7.5f,
                        0.58f,
                        0.24f,
                        0.1f,
                        0.06f,
                        0.28f,
                        46f);
            }
        }

        public static TetherfallOrbitCameraState CreateState(
            Vector3 targetPosition,
            float yaw,
            TetherfallOrbitCameraProfile profile)
        {
            var state = new TetherfallOrbitCameraState
            {
                Yaw = NormalizeAngle(yaw),
                Pitch = profile.DefaultPitch,
                FollowPivot = GetTargetPivot(targetPosition, profile),
                CursorRelockSuppressionUntil = 0f
            };
            return ResetAfterTeleport(
                state,
                state.FollowPivot,
                profile.BoomDistance,
                profile);
        }

        public static Vector2 GetMouseLookDelta(
            Vector2 mouseDelta,
            TetherfallOrbitLookSettings settings)
        {
            var safeInput = Sanitize(mouseDelta, Vector2.zero);
            var horizontalSign = settings.InvertHorizontal ? -1f : 1f;
            var verticalSign = settings.InvertVertical ? 1f : -1f;
            return new Vector2(
                safeInput.x * settings.MouseYawSensitivity * horizontalSign,
                safeInput.y * settings.MousePitchSensitivity * verticalSign);
        }

        public static Vector2 GetStickLookDelta(
            Vector2 stickInput,
            float deltaTime,
            TetherfallOrbitLookSettings settings)
        {
            var safeInput = Vector2.ClampMagnitude(
                Sanitize(stickInput, Vector2.zero),
                1f);
            var safeDeltaTime = NonNegativeFiniteOr(deltaTime, 0f);
            var horizontalSign = settings.InvertHorizontal ? -1f : 1f;
            var verticalSign = settings.InvertVertical ? 1f : -1f;
            return new Vector2(
                safeInput.x *
                settings.StickYawDegreesPerSecond *
                safeDeltaTime *
                horizontalSign,
                safeInput.y *
                settings.StickPitchDegreesPerSecond *
                safeDeltaTime *
                verticalSign);
        }

        public static TetherfallOrbitCameraState IntegrateMouseLook(
            TetherfallOrbitCameraState state,
            Vector2 mouseDelta,
            TetherfallOrbitLookSettings settings,
            TetherfallOrbitCameraProfile profile)
        {
            return IntegrateMouseLook(state, mouseDelta, settings, profile, false);
        }

        public static TetherfallOrbitCameraState IntegrateMouseLook(
            TetherfallOrbitCameraState state,
            Vector2 mouseDelta,
            TetherfallOrbitLookSettings settings,
            TetherfallOrbitCameraProfile profile,
            bool suppressLook)
        {
            return suppressLook
                ? SanitizeLookState(state, profile)
                : IntegrateLook(
                    state,
                    GetMouseLookDelta(mouseDelta, settings),
                    profile);
        }

        public static TetherfallOrbitCameraState IntegrateStickLook(
            TetherfallOrbitCameraState state,
            Vector2 stickInput,
            float deltaTime,
            TetherfallOrbitLookSettings settings,
            TetherfallOrbitCameraProfile profile)
        {
            return IntegrateStickLook(
                state,
                stickInput,
                deltaTime,
                settings,
                profile,
                false);
        }

        public static TetherfallOrbitCameraState IntegrateStickLook(
            TetherfallOrbitCameraState state,
            Vector2 stickInput,
            float deltaTime,
            TetherfallOrbitLookSettings settings,
            TetherfallOrbitCameraProfile profile,
            bool suppressLook)
        {
            return suppressLook
                ? SanitizeLookState(state, profile)
                : IntegrateLook(
                    state,
                    GetStickLookDelta(stickInput, deltaTime, settings),
                    profile);
        }

        public static TetherfallOrbitCameraState IntegrateLook(
            TetherfallOrbitCameraState state,
            Vector2 angularDelta,
            TetherfallOrbitCameraProfile profile)
        {
            var safeDelta = Sanitize(angularDelta, Vector2.zero);
            state.Yaw = NormalizeAngle(FiniteOr(state.Yaw, 0f) + safeDelta.x);
            state.Pitch = Mathf.Clamp(
                FiniteOr(state.Pitch, profile.DefaultPitch) + safeDelta.y,
                profile.MinimumPitch,
                profile.MaximumPitch);
            return state;
        }

        public static Vector3 PlanarForward(float yaw)
        {
            var radians = NormalizeAngle(yaw) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        }

        public static Vector3 PlanarRight(float yaw)
        {
            var forward = PlanarForward(yaw);
            return new Vector3(forward.z, 0f, -forward.x);
        }

        public static Vector3 GetTargetPivot(
            Vector3 targetPosition,
            TetherfallOrbitCameraProfile profile)
        {
            var safeTarget = Sanitize(targetPosition, Vector3.zero);
            return safeTarget + Vector3.up * profile.PivotHeight;
        }

        public static Quaternion GetOrbitRotation(float yaw, float pitch)
        {
            return Quaternion.Euler(
                FiniteOr(pitch, 0f),
                NormalizeAngle(yaw),
                0f);
        }

        public static Vector3 GetOrbitOffset(
            float yaw,
            float pitch,
            float shoulderOffset,
            float boomDistance)
        {
            var safeShoulder = FiniteOr(shoulderOffset, 0f);
            var safeBoom = Mathf.Max(0f, FiniteOr(boomDistance, 0f));
            return GetOrbitRotation(yaw, pitch) *
                   new Vector3(safeShoulder, 0f, -safeBoom);
        }

        public static Vector3 GetOrbitOffset(
            TetherfallOrbitCameraState state,
            TetherfallOrbitCameraProfile profile)
        {
            var boomDistance =
                state.CollisionBoomInitialized &&
                IsFinite(state.CollisionBoomDistance)
                    ? Mathf.Max(
                        profile.MinimumCollisionDistance,
                        state.CollisionBoomDistance)
                    : profile.BoomDistance;
            return GetOrbitOffset(
                state.Yaw,
                state.Pitch,
                profile.ShoulderOffset,
                boomDistance);
        }

        public static TetherfallOrbitCameraPose BuildPose(
            TetherfallOrbitCameraState state,
            TetherfallOrbitCameraProfile profile)
        {
            state = SanitizeLookState(state, profile);
            var pivot = Sanitize(state.FollowPivot, Vector3.zero);
            var position = pivot + GetOrbitOffset(state, profile);
            var toPivot = pivot - position;
            var rotation = toPivot.sqrMagnitude > DirectionEpsilon
                ? Quaternion.LookRotation(toPivot.normalized, Vector3.up)
                : GetOrbitRotation(state.Yaw, state.Pitch);
            return new TetherfallOrbitCameraPose(
                pivot,
                position,
                rotation,
                state.PlanarForward,
                state.PlanarRight);
        }

        public static float ExponentialDampingFactor(float sharpness, float deltaTime)
        {
            var safeSharpness = NonNegativeFiniteOr(sharpness, 0f);
            var safeDeltaTime = NonNegativeFiniteOr(deltaTime, 0f);
            var exponent = safeSharpness * safeDeltaTime;
            if (!IsFinite(exponent) || exponent >= 50f)
            {
                return exponent > 0f ? 1f : 0f;
            }

            return 1f - Mathf.Exp(-exponent);
        }

        public static Vector3 Damp(
            Vector3 current,
            Vector3 target,
            float sharpness,
            float deltaTime)
        {
            var safeTarget = Sanitize(target, Vector3.zero);
            var safeCurrent = Sanitize(current, safeTarget);
            return Vector3.LerpUnclamped(
                safeCurrent,
                safeTarget,
                ExponentialDampingFactor(sharpness, deltaTime));
        }

        public static float Damp(
            float current,
            float target,
            float sharpness,
            float deltaTime)
        {
            var safeTarget = FiniteOr(target, 0f);
            var safeCurrent = FiniteOr(current, safeTarget);
            return Mathf.LerpUnclamped(
                safeCurrent,
                safeTarget,
                ExponentialDampingFactor(sharpness, deltaTime));
        }

        public static TetherfallOrbitCameraState UpdateFollowPivot(
            TetherfallOrbitCameraState state,
            Vector3 targetPivot,
            float deltaTime,
            TetherfallOrbitCameraProfile profile,
            bool holdFollowPivot)
        {
            var safeTarget = Sanitize(targetPivot, Vector3.zero);
            state.FollowPivot = holdFollowPivot
                ? Sanitize(state.FollowPivot, safeTarget)
                : Damp(
                    state.FollowPivot,
                    safeTarget,
                    profile.FollowSharpness,
                    deltaTime);
            return state;
        }

        public static TetherfallOrbitCameraState ResetCollisionBoom(
            TetherfallOrbitCameraState state,
            float desiredDistance,
            TetherfallOrbitCameraProfile profile)
        {
            state.CollisionBoomDistance = SanitizeDesiredBoomDistance(
                desiredDistance,
                profile);
            state.CollisionBoomInitialized = true;
            state.CollisionConstrained = false;
            return state;
        }

        /// <summary>
        /// Consumes scalar boom-axis distances supplied by the caller's non-alloc cast.
        /// Pass desiredDistance as obstructedDistance when no blocking hit exists.
        /// </summary>
        public static TetherfallOrbitCameraState UpdateCollisionBoom(
            TetherfallOrbitCameraState state,
            float desiredDistance,
            float obstructedDistance,
            float deltaTime,
            TetherfallOrbitCameraProfile profile)
        {
            var desired = SanitizeDesiredBoomDistance(desiredDistance, profile);
            if (!state.CollisionBoomInitialized ||
                !IsFinite(state.CollisionBoomDistance))
            {
                state = ResetCollisionBoom(state, desired, profile);
            }

            var clearDistance = Mathf.Clamp(
                FiniteOr(obstructedDistance, desired),
                0f,
                desired);
            var obstructionGap = desired - clearDistance;
            var releaseThreshold = profile.CollisionDeadband * 0.25f;
            if (state.CollisionConstrained)
            {
                if (obstructionGap <= releaseThreshold)
                {
                    state.CollisionConstrained = false;
                }
            }
            else if (obstructionGap > profile.CollisionDeadband)
            {
                state.CollisionConstrained = true;
            }

            var targetDistance = state.CollisionConstrained
                ? Mathf.Clamp(
                    clearDistance - profile.CollisionSkin,
                    profile.MinimumCollisionDistance,
                    desired)
                : desired;
            var currentDistance = Mathf.Clamp(
                state.CollisionBoomDistance,
                profile.MinimumCollisionDistance,
                desired);
            var difference = targetDistance - currentDistance;
            if (Mathf.Abs(difference) > releaseThreshold)
            {
                var sharpness = difference < 0f
                    ? profile.CollisionCompressionSharpness
                    : profile.CollisionReleaseSharpness;
                currentDistance = Damp(
                    currentDistance,
                    targetDistance,
                    sharpness,
                    deltaTime);
            }

            state.CollisionBoomDistance = Mathf.Clamp(
                currentDistance,
                profile.MinimumCollisionDistance,
                desired);
            return state;
        }

        /// <summary>
        /// Snaps follow and collision history after a discontinuous target move.
        /// Yaw/pitch are retained, while stale focus and relock state are cleared.
        /// </summary>
        public static TetherfallOrbitCameraState ResetAfterTeleport(
            TetherfallOrbitCameraState state,
            Vector3 snappedPivot,
            float desiredBoomDistance,
            TetherfallOrbitCameraProfile profile)
        {
            state = SanitizeLookState(state, profile);
            state.FollowPivot = Sanitize(snappedPivot, Vector3.zero);
            state = ResetCollisionBoom(state, desiredBoomDistance, profile);
            state.FocusAssistEngaged = false;
            state.CursorRelockSuppressionUntil = 0f;
            return state;
        }

        public static TetherfallOrbitCameraState UpdateFocusAssistEngagement(
            TetherfallOrbitCameraState state,
            bool focusAvailable,
            float lookMagnitude,
            TetherfallOrbitCameraProfile profile)
        {
            if (!focusAvailable)
            {
                state.FocusAssistEngaged = false;
                return state;
            }

            var safeLookMagnitude = Mathf.Max(0f, FiniteOr(lookMagnitude, 0f));
            if (state.FocusAssistEngaged)
            {
                if (safeLookMagnitude >= profile.FocusBreakLookMagnitude)
                {
                    state.FocusAssistEngaged = false;
                }
            }
            else if (safeLookMagnitude <= profile.FocusEngageLookMagnitude)
            {
                state.FocusAssistEngaged = true;
            }

            return state;
        }

        public static TetherfallOrbitCameraState ApplyFocusYawAssist(
            TetherfallOrbitCameraState state,
            Vector3 planarFocusDirection,
            float deltaTime,
            TetherfallOrbitCameraProfile profile)
        {
            state = SanitizeLookState(state, profile);
            if (!state.FocusAssistEngaged)
            {
                return state;
            }

            var direction = Sanitize(planarFocusDirection, Vector3.zero);
            direction.y = 0f;
            if (direction.sqrMagnitude <= DirectionEpsilon)
            {
                return state;
            }

            direction.Normalize();
            var desiredYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            var maximumDelta =
                profile.FocusYawDegreesPerSecond *
                NonNegativeFiniteOr(deltaTime, 0f);
            state.Yaw = NormalizeAngle(
                Mathf.MoveTowardsAngle(state.Yaw, desiredYaw, maximumDelta));
            return state;
        }

        public static TetherfallOrbitCameraState RegisterCursorRelock(
            TetherfallOrbitCameraState state,
            float unscaledTime)
        {
            return RegisterCursorRelock(
                state,
                unscaledTime,
                DefaultCursorRelockSuppressionSeconds);
        }

        public static TetherfallOrbitCameraState RegisterCursorRelock(
            TetherfallOrbitCameraState state,
            float unscaledTime,
            float suppressionWindowSeconds)
        {
            var now = FiniteOr(unscaledTime, 0f);
            var window = NonNegativeFiniteOr(suppressionWindowSeconds, 0f);
            var currentUntil = FiniteOr(state.CursorRelockSuppressionUntil, now);
            state.CursorRelockSuppressionUntil = Mathf.Max(
                currentUntil,
                now + window);
            return state;
        }

        public static bool ShouldSuppressLook(
            TetherfallOrbitCameraState state,
            bool cursorLockAcquiredThisFrame,
            float unscaledTime)
        {
            return ShouldSuppressLook(
                cursorLockAcquiredThisFrame,
                unscaledTime,
                state.CursorRelockSuppressionUntil);
        }

        public static bool ShouldSuppressLook(
            bool cursorLockAcquiredThisFrame,
            float unscaledTime,
            float suppressionUntil)
        {
            if (cursorLockAcquiredThisFrame)
            {
                return true;
            }

            return IsFinite(unscaledTime) &&
                   IsFinite(suppressionUntil) &&
                   unscaledTime < suppressionUntil;
        }

        public static TetherfallOrbitHitstopPolicy ResolveHitstopPolicy(
            bool hitstopActive,
            bool lookInputAllowed)
        {
            return new TetherfallOrbitHitstopPolicy(
                lookInputAllowed,
                hitstopActive);
        }

        /// <summary>
        /// Applies presentation-only local shake. The method cannot change camera
        /// state, yaw, pitch, pivot, or the planar aim basis.
        /// </summary>
        public static Vector3 ApplyAdditiveShake(
            Vector3 unshakenPosition,
            Quaternion unshakenRotation,
            Vector3 localShakeOffset)
        {
            var safePosition = Sanitize(unshakenPosition, Vector3.zero);
            var safeRotation = IsFinite(unshakenRotation)
                ? unshakenRotation
                : Quaternion.identity;
            var safeOffset = Sanitize(localShakeOffset, Vector3.zero);
            return safePosition + safeRotation * safeOffset;
        }

        public static float DeriveRecommendedFarClipFromRegionPacks()
        {
            return DeriveRecommendedFarClip(
                TetherfallRegionPacks.WorldMinimumX,
                TetherfallRegionPacks.WorldMaximumX,
                TetherfallRegionPacks.WorldMinimumZ,
                TetherfallRegionPacks.WorldMaximumZ,
                DefaultFarClipMinimum,
                DefaultFarClipMargin);
        }

        /// <summary>
        /// Uses the full horizontal diagonal so a camera anywhere inside the
        /// supplied bounds can see the opposite corner. Bounds are parameters to
        /// support future annex registries that expand beyond today's constants.
        /// </summary>
        public static float DeriveRecommendedFarClip(
            float minimumX,
            float maximumX,
            float minimumZ,
            float maximumZ,
            float boundedMinimum,
            float margin)
        {
            var safeMinimum = Mathf.Max(
                1f,
                FiniteOr(boundedMinimum, DefaultFarClipMinimum));
            var safeMargin = Mathf.Max(0f, FiniteOr(margin, DefaultFarClipMargin));
            if (!IsFinite(minimumX) ||
                !IsFinite(maximumX) ||
                !IsFinite(minimumZ) ||
                !IsFinite(maximumZ))
            {
                return safeMinimum;
            }

            var width = Math.Abs((double)maximumX - minimumX);
            var depth = Math.Abs((double)maximumZ - minimumZ);
            var diagonal = Math.Sqrt(width * width + depth * depth);
            var candidate = Math.Max(safeMinimum, diagonal + safeMargin);
            if (double.IsNaN(candidate))
            {
                return safeMinimum;
            }

            if (double.IsInfinity(candidate) || candidate > float.MaxValue)
            {
                return float.MaxValue;
            }

            return (float)candidate;
        }

        public static float NormalizeAngle(float angle)
        {
            var safeAngle = FiniteOr(angle, 0f);
            return Mathf.Repeat(safeAngle + 180f, 360f) - 180f;
        }

        public static bool IsFinite(TetherfallOrbitCameraState state)
        {
            return IsFinite(state.Yaw) &&
                   IsFinite(state.Pitch) &&
                   IsFinite(state.FollowPivot) &&
                   IsFinite(state.CollisionBoomDistance) &&
                   IsFinite(state.CursorRelockSuppressionUntil);
        }

        public static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        public static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        public static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z) &&
                   IsFinite(value.w);
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static TetherfallOrbitCameraState SanitizeLookState(
            TetherfallOrbitCameraState state,
            TetherfallOrbitCameraProfile profile)
        {
            state.Yaw = NormalizeAngle(state.Yaw);
            state.Pitch = Mathf.Clamp(
                FiniteOr(state.Pitch, profile.DefaultPitch),
                profile.MinimumPitch,
                profile.MaximumPitch);
            return state;
        }

        private static float SanitizeDesiredBoomDistance(
            float desiredDistance,
            TetherfallOrbitCameraProfile profile)
        {
            return Mathf.Max(
                profile.MinimumCollisionDistance,
                FiniteOr(desiredDistance, profile.BoomDistance));
        }

        private static Vector2 Sanitize(Vector2 value, Vector2 fallback)
        {
            return IsFinite(value) ? value : fallback;
        }

        private static Vector3 Sanitize(Vector3 value, Vector3 fallback)
        {
            return IsFinite(value) ? value : fallback;
        }

        private static float NonNegativeFiniteOr(float value, float fallback)
        {
            return Mathf.Max(0f, FiniteOr(value, fallback));
        }

        private static float FiniteOr(float value, float fallback)
        {
            return IsFinite(value) ? value : fallback;
        }
    }
}
