using System;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallOrbitCameraMathVerification
    {
        private const string MenuPath = "Tetherfall/Verify Orbit Camera Math";
        private const float Tolerance = 0.001f;
        private static int assertionCount;

        private static readonly TetherfallOrbitTraversalMode[] TraversalModes =
        {
            TetherfallOrbitTraversalMode.Default,
            TetherfallOrbitTraversalMode.Mounted,
            TetherfallOrbitTraversalMode.Climb,
            TetherfallOrbitTraversalMode.Swim,
            TetherfallOrbitTraversalMode.Glide,
            TetherfallOrbitTraversalMode.Mantle
        };

        [MenuItem(MenuPath, false, 2215)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Orbit camera math verification is edit-mode only.");
            }

            VerifyInternal(true);
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        public static void VerifyForFullGame()
        {
            VerifyInternal(false);
        }

        private static void VerifyInternal(bool logSuccess)
        {
            assertionCount = 0;
            VerifyMouseAndStickDeltaInvariance();
            VerifyCursorRelockSuppression();
            VerifyConstantBoomGeometry();
            VerifyFiniteMath();
            VerifyFollowAndHitstopSeparation();
            VerifyCollisionHysteresis();
            VerifyFocusAssist();
            VerifyTeleportReset();
            VerifyShakeIsolation();
            VerifyTraversalProfiles();
            VerifyImmediateAimBasis();
            VerifyFarClipBounds();

            if (logSuccess)
            {
                Debug.Log(
                    $"Tetherfall orbit camera math verification passed: {assertionCount} assertions; " +
                    "delta invariance, relock suppression, clean boom geometry, finite damping, " +
                    "collision hysteresis, focus break, teleport reset, shake isolation, traversal " +
                    "profiles, immediate aim basis, hitstop separation, and far-clip bounds are valid.");
            }
        }

        private static void VerifyMouseAndStickDeltaInvariance()
        {
            var profile = TetherfallOrbitCameraMath.GetProfile(
                TetherfallOrbitTraversalMode.Default);
            var settings = new TetherfallOrbitLookSettings(
                2.4f,
                1.8f,
                160f,
                120f,
                false,
                false);
            var initial = TetherfallOrbitCameraMath.CreateState(
                new Vector3(2f, 3f, 4f),
                17f,
                profile);

            var oneMouseDelta = TetherfallOrbitCameraMath.IntegrateMouseLook(
                initial,
                new Vector2(1.2f, -0.8f),
                settings,
                profile);
            var splitMouseDelta = initial;
            for (var index = 0; index < 4; index++)
            {
                splitMouseDelta = TetherfallOrbitCameraMath.IntegrateMouseLook(
                    splitMouseDelta,
                    new Vector2(0.3f, -0.2f),
                    settings,
                    profile);
            }

            Check(
                ApproximatelyAngle(oneMouseDelta.Yaw, splitMouseDelta.Yaw) &&
                Approximately(oneMouseDelta.Pitch, splitMouseDelta.Pitch),
                "Mouse integration changed when the same raw delta was split across frames.");

            var oneStickStep = TetherfallOrbitCameraMath.IntegrateStickLook(
                initial,
                new Vector2(0.4f, -0.3f),
                1f / 30f,
                settings,
                profile);
            var splitStickStep = initial;
            for (var index = 0; index < 4; index++)
            {
                splitStickStep = TetherfallOrbitCameraMath.IntegrateStickLook(
                    splitStickStep,
                    new Vector2(0.4f, -0.3f),
                    1f / 120f,
                    settings,
                    profile);
            }

            Check(
                ApproximatelyAngle(oneStickStep.Yaw, splitStickStep.Yaw) &&
                Approximately(oneStickStep.Pitch, splitStickStep.Pitch),
                "Stick integration is not invariant for equal elapsed time.");

            var inverted = new TetherfallOrbitLookSettings(
                2.4f,
                1.8f,
                160f,
                120f,
                true,
                true);
            var normalDelta = TetherfallOrbitCameraMath.GetMouseLookDelta(
                Vector2.one,
                settings);
            var invertedDelta = TetherfallOrbitCameraMath.GetMouseLookDelta(
                Vector2.one,
                inverted);
            Check(
                Approximately(normalDelta.x, -invertedDelta.x) &&
                Approximately(normalDelta.y, -invertedDelta.y),
                "Mouse sensitivity inversion does not flip both configured axes.");

            var normalStickDelta = TetherfallOrbitCameraMath.GetStickLookDelta(
                Vector2.one.normalized,
                0.25f,
                settings);
            var halfTimeStickDelta = TetherfallOrbitCameraMath.GetStickLookDelta(
                Vector2.one.normalized,
                0.125f,
                settings);
            Check(
                Approximately(normalStickDelta, halfTimeStickDelta * 2f),
                "Stick look is not scaled linearly by deltaTime.");
        }

        private static void VerifyCursorRelockSuppression()
        {
            var profile = TetherfallOrbitCameraMath.GetProfile(
                TetherfallOrbitTraversalMode.Default);
            var settings = TetherfallOrbitLookSettings.Default;
            var state = TetherfallOrbitCameraMath.CreateState(
                Vector3.zero,
                12f,
                profile);
            state = TetherfallOrbitCameraMath.RegisterCursorRelock(
                state,
                25f,
                0.08f);

            Check(
                TetherfallOrbitCameraMath.ShouldSuppressLook(state, true, 25f),
                "Cursor-lock acquisition did not suppress look on its current frame.");
            Check(
                TetherfallOrbitCameraMath.ShouldSuppressLook(state, false, 25.04f),
                "Cursor relock did not suppress the short post-lock spike window.");
            Check(
                !TetherfallOrbitCameraMath.ShouldSuppressLook(state, false, 25.081f),
                "Cursor relock suppression outlived its configured short window.");

            var suppressed = TetherfallOrbitCameraMath.IntegrateMouseLook(
                state,
                new Vector2(1000f, -1000f),
                settings,
                profile,
                true);
            Check(
                ApproximatelyAngle(suppressed.Yaw, state.Yaw) &&
                Approximately(suppressed.Pitch, state.Pitch),
                "A suppressed cursor-lock spike still changed orbit angles.");
        }

        private static void VerifyConstantBoomGeometry()
        {
            var target = new Vector3(9f, 4f, -6f);
            for (var index = 0; index < TraversalModes.Length; index++)
            {
                var profile = TetherfallOrbitCameraMath.GetProfile(
                    TraversalModes[index]);
                var expectedLength = Mathf.Sqrt(
                    profile.ShoulderOffset * profile.ShoulderOffset +
                    profile.BoomDistance * profile.BoomDistance);
                var lowOffset = TetherfallOrbitCameraMath.GetOrbitOffset(
                    37f,
                    profile.MinimumPitch,
                    profile.ShoulderOffset,
                    profile.BoomDistance);
                var highOffset = TetherfallOrbitCameraMath.GetOrbitOffset(
                    37f,
                    profile.MaximumPitch,
                    profile.ShoulderOffset,
                    profile.BoomDistance);
                var pivot = TetherfallOrbitCameraMath.GetTargetPivot(target, profile);

                Check(
                    Approximately(lowOffset.magnitude, expectedLength) &&
                    Approximately(highOffset.magnitude, expectedLength),
                    $"{profile.Mode} pitch changed the shoulder/boom vector length.");
                Check(
                    Approximately(pivot.y, target.y + profile.PivotHeight),
                    $"{profile.Mode} target pivot height was folded into the rotated boom.");
            }
        }

        private static void VerifyFiniteMath()
        {
            var profile = TetherfallOrbitCameraMath.GetProfile(
                TetherfallOrbitTraversalMode.Default);
            var state = new TetherfallOrbitCameraState
            {
                Yaw = float.NaN,
                Pitch = float.PositiveInfinity,
                FollowPivot = new Vector3(float.NaN, 2f, 3f),
                CollisionBoomDistance = float.NegativeInfinity,
                CollisionBoomInitialized = true,
                CursorRelockSuppressionUntil = float.PositiveInfinity
            };

            state = TetherfallOrbitCameraMath.IntegrateMouseLook(
                state,
                new Vector2(float.NaN, float.PositiveInfinity),
                TetherfallOrbitLookSettings.Default,
                profile);
            state = TetherfallOrbitCameraMath.UpdateFollowPivot(
                state,
                new Vector3(float.PositiveInfinity, 1f, 2f),
                float.NaN,
                profile,
                false);
            state = TetherfallOrbitCameraMath.UpdateCollisionBoom(
                state,
                float.NaN,
                float.NegativeInfinity,
                float.PositiveInfinity,
                profile);
            state = TetherfallOrbitCameraMath.RegisterCursorRelock(
                state,
                float.NaN,
                float.PositiveInfinity);
            var pose = TetherfallOrbitCameraMath.BuildPose(state, profile);
            var shaken = TetherfallOrbitCameraMath.ApplyAdditiveShake(
                new Vector3(float.NaN, 0f, 0f),
                new Quaternion(float.NaN, 0f, 0f, 1f),
                new Vector3(0f, float.PositiveInfinity, 0f));

            Check(
                TetherfallOrbitCameraMath.IsFinite(state),
                "Invalid input escaped orbit state sanitization.");
            Check(
                TetherfallOrbitCameraMath.IsFinite(pose.Pivot) &&
                TetherfallOrbitCameraMath.IsFinite(pose.Position) &&
                TetherfallOrbitCameraMath.IsFinite(pose.Rotation),
                "Invalid input produced a non-finite orbit pose.");
            Check(
                TetherfallOrbitCameraMath.IsFinite(shaken),
                "Invalid shake input produced a non-finite position.");
            Check(
                TetherfallOrbitCameraMath.IsFinite(
                    TetherfallOrbitCameraMath.DeriveRecommendedFarClip(
                        float.NaN,
                        float.PositiveInfinity,
                        0f,
                        1f,
                        400f,
                        64f)),
                "Invalid world bounds produced a non-finite far clip.");
        }

        private static void VerifyFollowAndHitstopSeparation()
        {
            var profile = TetherfallOrbitCameraMath.GetProfile(
                TetherfallOrbitTraversalMode.Default);
            var initial = TetherfallOrbitCameraMath.CreateState(
                Vector3.zero,
                0f,
                profile);
            var targetPivot = new Vector3(10f, 5f, -3f);

            var oneStep = TetherfallOrbitCameraMath.UpdateFollowPivot(
                initial,
                targetPivot,
                1f / 30f,
                profile,
                false);
            var splitStep = initial;
            for (var index = 0; index < 4; index++)
            {
                splitStep = TetherfallOrbitCameraMath.UpdateFollowPivot(
                    splitStep,
                    targetPivot,
                    1f / 120f,
                    profile,
                    false);
            }

            Check(
                Approximately(oneStep.FollowPivot, splitStep.FollowPivot),
                "Exponential follow damping changed with frame subdivision.");

            var hitstopPolicy = TetherfallOrbitCameraMath.ResolveHitstopPolicy(
                true,
                true);
            Check(
                hitstopPolicy.IntegrateLookIntent && hitstopPolicy.HoldFollowPivot,
                "Hitstop did not preserve look intent while holding target follow.");

            var duringHitstop = TetherfallOrbitCameraMath.IntegrateMouseLook(
                initial,
                new Vector2(2f, 0f),
                TetherfallOrbitLookSettings.Default,
                profile,
                !hitstopPolicy.IntegrateLookIntent);
            duringHitstop = TetherfallOrbitCameraMath.UpdateFollowPivot(
                duringHitstop,
                targetPivot,
                0.2f,
                profile,
                hitstopPolicy.HoldFollowPivot);
            Check(
                !ApproximatelyAngle(duringHitstop.Yaw, initial.Yaw) &&
                Approximately(duringHitstop.FollowPivot, initial.FollowPivot),
                "Hitstop coupled look integration to the held follow pivot.");

            var blockedInputPolicy = TetherfallOrbitCameraMath.ResolveHitstopPolicy(
                true,
                false);
            Check(
                !blockedInputPolicy.IntegrateLookIntent &&
                blockedInputPolicy.HoldFollowPivot,
                "Gameplay input permission is not independent from hitstop follow hold.");
        }

        private static void VerifyCollisionHysteresis()
        {
            var profile = TetherfallOrbitCameraMath.GetProfile(
                TetherfallOrbitTraversalMode.Default);
            var desired = profile.BoomDistance;
            var state = TetherfallOrbitCameraMath.CreateState(
                Vector3.zero,
                0f,
                profile);

            for (var index = 0; index < 12; index++)
            {
                var subDeadbandGap = profile.CollisionDeadband *
                                     (index % 2 == 0 ? 0.45f : 0.8f);
                state = TetherfallOrbitCameraMath.UpdateCollisionBoom(
                    state,
                    desired,
                    desired - subDeadbandGap,
                    1f / 60f,
                    profile);
            }

            Check(
                !state.CollisionConstrained &&
                Approximately(state.CollisionBoomDistance, desired),
                "Sub-deadband obstruction noise caused collision boom chatter.");

            var safeObstructedDistance = 3f;
            var compressionStart = state.CollisionBoomDistance;
            state = TetherfallOrbitCameraMath.UpdateCollisionBoom(
                state,
                desired,
                safeObstructedDistance + profile.CollisionSkin,
                0.05f,
                profile);
            var compressionChange = compressionStart - state.CollisionBoomDistance;
            Check(
                state.CollisionConstrained && compressionChange > 0f,
                "A material obstruction did not engage and compress the boom.");
            Check(
                state.CollisionBoomDistance >= profile.MinimumCollisionDistance,
                "Collision compression crossed the configured minimum distance.");

            var releaseState = TetherfallOrbitCameraMath.ResetCollisionBoom(
                state,
                safeObstructedDistance,
                profile);
            releaseState = TetherfallOrbitCameraMath.UpdateCollisionBoom(
                releaseState,
                desired,
                desired,
                0.05f,
                profile);
            var releaseChange =
                releaseState.CollisionBoomDistance - safeObstructedDistance;
            var fullRange = desired - safeObstructedDistance;
            Check(
                compressionChange / fullRange > releaseChange / fullRange,
                "Collision release is not slower than collision compression.");

            for (var index = 0; index < 120; index++)
            {
                state = TetherfallOrbitCameraMath.UpdateCollisionBoom(
                    state,
                    desired,
                    safeObstructedDistance + profile.CollisionSkin,
                    1f / 60f,
                    profile);
            }

            var settledDistance = state.CollisionBoomDistance;
            for (var index = 0; index < 20; index++)
            {
                var noise = index % 2 == 0
                    ? profile.CollisionDeadband * 0.1f
                    : -profile.CollisionDeadband * 0.1f;
                state = TetherfallOrbitCameraMath.UpdateCollisionBoom(
                    state,
                    desired,
                    safeObstructedDistance + profile.CollisionSkin + noise,
                    1f / 60f,
                    profile);
            }

            Check(
                Mathf.Abs(state.CollisionBoomDistance - settledDistance) <=
                profile.CollisionDeadband,
                "Settled collision boom amplified sub-deadband obstruction noise.");

            for (var index = 0; index < 120; index++)
            {
                state = TetherfallOrbitCameraMath.UpdateCollisionBoom(
                    state,
                    desired,
                    desired,
                    1f / 60f,
                    profile);
            }

            Check(
                !state.CollisionConstrained &&
                Mathf.Abs(state.CollisionBoomDistance - desired) <
                profile.CollisionDeadband,
                "Collision boom did not release cleanly after obstruction cleared.");
        }

        private static void VerifyFocusAssist()
        {
            var profile = TetherfallOrbitCameraMath.GetProfile(
                TetherfallOrbitTraversalMode.Default);
            var initial = TetherfallOrbitCameraMath.CreateState(
                new Vector3(3f, 2f, 1f),
                0f,
                profile);
            var engaged = TetherfallOrbitCameraMath.UpdateFocusAssistEngagement(
                initial,
                true,
                0f,
                profile);
            Check(
                engaged.FocusAssistEngaged,
                "Focus assist did not engage below its look threshold.");

            var firstAssist = TetherfallOrbitCameraMath.ApplyFocusYawAssist(
                engaged,
                Vector3.right,
                0.1f,
                profile);
            var secondAssist = TetherfallOrbitCameraMath.ApplyFocusYawAssist(
                engaged,
                Vector3.right,
                0.1f,
                profile);
            Check(
                ApproximatelyAngle(firstAssist.Yaw, secondAssist.Yaw) &&
                firstAssist.Yaw > engaged.Yaw,
                "Focus yaw assist is not deterministic for identical inputs.");
            Check(
                Approximately(firstAssist.FollowPivot, engaged.FollowPivot),
                "Focus yaw assist pulled the follow pivot toward the enemy.");

            var hysteresisBand = TetherfallOrbitCameraMath.UpdateFocusAssistEngagement(
                firstAssist,
                true,
                (profile.FocusEngageLookMagnitude +
                 profile.FocusBreakLookMagnitude) * 0.5f,
                profile);
            Check(
                hysteresisBand.FocusAssistEngaged,
                "Focus assist broke inside its engage/break hysteresis band.");

            var broken = TetherfallOrbitCameraMath.UpdateFocusAssistEngagement(
                hysteresisBand,
                true,
                profile.FocusBreakLookMagnitude,
                profile);
            Check(
                !broken.FocusAssistEngaged,
                "Manual look at the break threshold did not disengage focus assist.");

            var remainsBroken = TetherfallOrbitCameraMath.UpdateFocusAssistEngagement(
                broken,
                true,
                (profile.FocusEngageLookMagnitude +
                 profile.FocusBreakLookMagnitude) * 0.5f,
                profile);
            Check(
                !remainsBroken.FocusAssistEngaged,
                "Focus assist re-engaged inside the hysteresis band.");
        }

        private static void VerifyTeleportReset()
        {
            var profile = TetherfallOrbitCameraMath.GetProfile(
                TetherfallOrbitTraversalMode.Glide);
            var state = TetherfallOrbitCameraMath.CreateState(
                Vector3.zero,
                35f,
                profile);
            state = TetherfallOrbitCameraMath.UpdateCollisionBoom(
                state,
                profile.BoomDistance,
                2f,
                0.1f,
                profile);
            state = TetherfallOrbitCameraMath.UpdateFocusAssistEngagement(
                state,
                true,
                0f,
                profile);
            state = TetherfallOrbitCameraMath.RegisterCursorRelock(state, 20f, 1f);

            var snappedPivot = new Vector3(400f, 28f, -350f);
            var reset = TetherfallOrbitCameraMath.ResetAfterTeleport(
                state,
                snappedPivot,
                profile.BoomDistance,
                profile);
            Check(
                Approximately(reset.FollowPivot, snappedPivot),
                "Teleport reset retained stale follow damping history.");
            Check(
                reset.CollisionBoomInitialized &&
                !reset.CollisionConstrained &&
                Approximately(reset.CollisionBoomDistance, profile.BoomDistance),
                "Teleport reset retained stale collision compression history.");
            Check(
                !reset.FocusAssistEngaged &&
                Approximately(reset.CursorRelockSuppressionUntil, 0f),
                "Teleport reset retained stale focus or relock state.");
            Check(
                ApproximatelyAngle(reset.Yaw, state.Yaw) &&
                Approximately(reset.Pitch, state.Pitch),
                "Teleport reset unexpectedly discarded the player's view angles.");
        }

        private static void VerifyShakeIsolation()
        {
            var profile = TetherfallOrbitCameraMath.GetProfile(
                TetherfallOrbitTraversalMode.Mounted);
            var state = TetherfallOrbitCameraMath.CreateState(
                new Vector3(5f, 1f, -8f),
                63f,
                profile);
            state = TetherfallOrbitCameraMath.IntegrateMouseLook(
                state,
                new Vector2(0.5f, -0.25f),
                TetherfallOrbitLookSettings.Default,
                profile);
            var pose = TetherfallOrbitCameraMath.BuildPose(state, profile);
            var yawBefore = state.Yaw;
            var pitchBefore = state.Pitch;
            var forwardBefore = state.PlanarForward;
            var rightBefore = state.PlanarRight;
            var shaken = TetherfallOrbitCameraMath.ApplyAdditiveShake(
                pose.Position,
                pose.Rotation,
                new Vector3(0.12f, -0.08f, 0f));

            Check(
                !Approximately(shaken, pose.Position) &&
                TetherfallOrbitCameraMath.IsFinite(shaken),
                "Additive shake did not produce a finite presentation offset.");
            Check(
                ApproximatelyAngle(state.Yaw, yawBefore) &&
                Approximately(state.Pitch, pitchBefore) &&
                Approximately(state.PlanarForward, forwardBefore) &&
                Approximately(state.PlanarRight, rightBefore),
                "Additive shake mutated yaw, pitch, or the aim basis.");
        }

        private static void VerifyTraversalProfiles()
        {
            for (var index = 0; index < TraversalModes.Length; index++)
            {
                var mode = TraversalModes[index];
                var profile = TetherfallOrbitCameraMath.GetProfile(mode);
                Check(
                    profile.Mode == mode,
                    $"{mode} resolved to the wrong traversal profile.");
                Check(
                    profile.PivotHeight > 0f &&
                    profile.BoomDistance > profile.MinimumCollisionDistance &&
                    profile.MinimumPitch < profile.DefaultPitch &&
                    profile.DefaultPitch < profile.MaximumPitch &&
                    profile.FollowSharpness > 0f,
                    $"{mode} has invalid pivot, boom, pitch, or follow tuning.");
                Check(
                    profile.CollisionCompressionSharpness >
                    profile.CollisionReleaseSharpness &&
                    profile.CollisionDeadband > 0f &&
                    profile.FocusEngageLookMagnitude <
                    profile.FocusBreakLookMagnitude,
                    $"{mode} lost collision or focus hysteresis tuning.");
                Check(
                    TetherfallOrbitCameraMath.IsFinite(profile.PivotHeight) &&
                    TetherfallOrbitCameraMath.IsFinite(profile.BoomDistance) &&
                    TetherfallOrbitCameraMath.IsFinite(profile.ShoulderOffset) &&
                    TetherfallOrbitCameraMath.IsFinite(profile.MinimumPitch) &&
                    TetherfallOrbitCameraMath.IsFinite(profile.MaximumPitch),
                    $"{mode} contains non-finite tuning.");
            }
        }

        private static void VerifyImmediateAimBasis()
        {
            var profile = TetherfallOrbitCameraMath.GetProfile(
                TetherfallOrbitTraversalMode.Default);
            var settings = new TetherfallOrbitLookSettings(
                30f,
                1f,
                1f,
                1f,
                false,
                false);
            var state = TetherfallOrbitCameraMath.CreateState(
                Vector3.zero,
                0f,
                profile);
            var before = state.PlanarForward;
            state = TetherfallOrbitCameraMath.IntegrateMouseLook(
                state,
                new Vector2(1f, 0f),
                settings,
                profile);

            var expectedForward = new Vector3(0.5f, 0f, 0.8660254f);
            Check(
                !Approximately(state.PlanarForward, before) &&
                Approximately(state.PlanarForward, expectedForward),
                "PlanarForward did not update immediately from the integrated yaw.");
            Check(
                Approximately(state.PlanarForward.magnitude, 1f) &&
                Approximately(state.PlanarRight.magnitude, 1f) &&
                Mathf.Abs(Vector3.Dot(state.PlanarForward, state.PlanarRight)) <
                Tolerance,
                "Immediate movement/attack aim basis is not orthonormal.");
        }

        private static void VerifyFarClipBounds()
        {
            var current = TetherfallOrbitCameraMath.DeriveRecommendedFarClipFromRegionPacks();
            var width =
                TetherfallRegionPacks.WorldMaximumX -
                TetherfallRegionPacks.WorldMinimumX;
            var depth =
                TetherfallRegionPacks.WorldMaximumZ -
                TetherfallRegionPacks.WorldMinimumZ;
            var required =
                Mathf.Sqrt(width * width + depth * depth) +
                TetherfallOrbitCameraMath.DefaultFarClipMargin;
            Check(
                current >= TetherfallOrbitCameraMath.DefaultFarClipMinimum &&
                current + Tolerance >= required,
                "Region-pack far clip does not cover the bounded minimum and world diagonal margin.");

            var expanded = TetherfallOrbitCameraMath.DeriveRecommendedFarClip(
                TetherfallRegionPacks.WorldMinimumX - 1000f,
                TetherfallRegionPacks.WorldMaximumX + 1000f,
                TetherfallRegionPacks.WorldMinimumZ - 800f,
                TetherfallRegionPacks.WorldMaximumZ + 800f,
                TetherfallOrbitCameraMath.DefaultFarClipMinimum,
                TetherfallOrbitCameraMath.DefaultFarClipMargin);
            Check(
                expanded > current,
                "Future annex-expanded bounds did not increase the recommended far clip.");

            var forwardBounds = TetherfallOrbitCameraMath.DeriveRecommendedFarClip(
                -10f,
                30f,
                -20f,
                50f,
                256f,
                32f);
            var reversedBounds = TetherfallOrbitCameraMath.DeriveRecommendedFarClip(
                30f,
                -10f,
                50f,
                -20f,
                256f,
                32f);
            Check(
                Approximately(forwardBounds, reversedBounds) &&
                Approximately(forwardBounds, 256f),
                "Far-clip derivation does not tolerate reversed or small bounds.");
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= Tolerance;
        }

        private static bool ApproximatelyAngle(float left, float right)
        {
            return Mathf.Abs(Mathf.DeltaAngle(left, right)) <= Tolerance;
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return (left - right).sqrMagnitude <= Tolerance * Tolerance;
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return (left - right).sqrMagnitude <= Tolerance * Tolerance;
        }

        private static void Check(bool condition, string message)
        {
            assertionCount++;
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
