using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallFollowerMotorVerification
    {
        private const float FixtureX = 4200f;
        private const float FixtureZ = 4200f;

        [MenuItem("Tetherfall/Verify Follower Motor", false, 2214)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Follower-motor verification is edit-mode only.");
            }

            var fixture = new GameObject("Follower Motor Verification");
            try
            {
                var motor = new ExpeditionFollowerMotor();
                var groundBuffer = ReadField<RaycastHit[]>(motor, "groundHits");
                var movementBuffer = ReadField<RaycastHit[]>(motor, "movementHits");
                var occupancyBuffer = ReadField<Collider[]>(motor, "occupancyHits");
                Assert(
                    groundBuffer != null && groundBuffer.Length >= 16,
                    "Follower ground projection is missing a reusable NonAlloc hit buffer.");
                Assert(
                    movementBuffer != null && movementBuffer.Length >= 16,
                    "Follower obstacle sweeps are missing a reusable NonAlloc hit buffer.");
                Assert(
                    occupancyBuffer != null && occupancyBuffer.Length >= 16,
                    "Follower warp clearance is missing a reusable NonAlloc overlap buffer.");

                VerifyActorIntegration(fixture.transform);
                VerifyGroundProjection(fixture.transform, motor);
                VerifyObstacleBlocking(fixture.transform, motor);
                VerifyActorColliderIgnoring(fixture.transform, motor);
                VerifySlopeCatchUp(fixture.transform, motor);
                VerifyElevationCatchUp(fixture.transform, motor);
                VerifyStackedFloorCatchUp(fixture.transform, motor);
                VerifySafeWarp(fixture.transform, motor);
                VerifyFlyingHeightAndPause(fixture.transform, motor);
                VerifyFlyingVerticalCatchUp(fixture.transform, motor);

                Assert(
                    ReferenceEquals(groundBuffer, ReadField<RaycastHit[]>(motor, "groundHits")) &&
                    ReferenceEquals(movementBuffer, ReadField<RaycastHit[]>(motor, "movementHits")) &&
                    ReferenceEquals(occupancyBuffer, ReadField<Collider[]>(motor, "occupancyHits")),
                    "Follower queries replaced their buffers instead of reusing them.");
                Assert(IsFinite(motor.Velocity), "Follower motor retained a non-finite velocity.");

                Debug.Log(
                    "Follower-motor verification passed: walkable ground projection, obstacle blocking, " +
                    "actor-collider filtering, slope and elevation catch-up, collision-free bounded warp, " +
                    "flying height, pause safety, finite transforms, component safety, and reusable NonAlloc " +
                    "buffers are valid.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fixture);
            }
        }

        private static void VerifyActorIntegration(Transform fixture)
        {
            Assert(
                !typeof(Component).IsAssignableFrom(typeof(ExpeditionFollowerMotor)),
                "Follower motor must remain a plain runtime object, not a per-follower physics component.");

            var pactObject = new GameObject("Pact follower integration");
            pactObject.transform.SetParent(fixture, false);
            var pact = pactObject.AddComponent<PactCreature>();
            var humanObject = new GameObject("Human follower integration");
            humanObject.transform.SetParent(fixture, false);
            var human = humanObject.AddComponent<HumanCompanion>();

            Assert(
                ReadField<ExpeditionFollowerMotor>(pact, "followerMotor") != null,
                "PactCreature is not integrated with the reusable follower motor.");
            Assert(
                ReadField<ExpeditionFollowerMotor>(human, "followerMotor") != null,
                "HumanCompanion is not integrated with the reusable follower motor.");
            AssertNoPhysicsOrLights(pactObject, "PactCreature");
            AssertNoPhysicsOrLights(humanObject, "HumanCompanion");
        }

        private static void VerifyGroundProjection(Transform fixture, ExpeditionFollowerMotor motor)
        {
            var center = new Vector3(FixtureX, 0f, FixtureZ);
            CreateBox(
                fixture,
                "Projection lower floor",
                center + Vector3.down * 0.25f,
                new Vector3(10f, 0.5f, 10f));
            CreateBox(
                fixture,
                "Projection upper floor",
                center + Vector3.up * 2.75f,
                new Vector3(6f, 0.5f, 6f));
            Physics.SyncTransforms();

            Assert(
                motor.TryProjectDestination(
                    center + Vector3.up * 3f,
                    3f,
                    ExpeditionFollowerMotorProfile.Human,
                    out var projected),
                "Follower formation destination did not project to walkable ground.");
            Assert(
                Mathf.Abs(projected.y - (3f + ExpeditionFollowerMotorProfile.Human.GroundOffset)) < 0.02f,
                $"Ground projection selected the wrong floor (projected y={projected.y:0.###}).");
            Assert(IsFinite(projected), "Ground projection produced a non-finite position.");
        }

        private static void VerifyObstacleBlocking(Transform fixture, ExpeditionFollowerMotor motor)
        {
            motor.ResetMotion();
            var center = new Vector3(FixtureX + 40f, 0f, FixtureZ);
            CreateBox(
                fixture,
                "Obstacle floor",
                center + Vector3.down * 0.25f,
                new Vector3(18f, 0.5f, 18f));
            var wall = CreateBox(
                fixture,
                "Follower blocking wall",
                center + Vector3.up * 1.5f,
                new Vector3(0.5f, 3f, 14f));
            var follower = CreateFollower(
                fixture,
                "Obstacle follower",
                center + new Vector3(-3.4f, ExpeditionFollowerMotorProfile.Human.GroundOffset, 0f));
            Physics.SyncTransforms();

            var blocked = false;
            for (var index = 0; index < 12; index++)
            {
                var result = motor.Move(
                    follower,
                    center + new Vector3(3.4f, 0f, 0f),
                    0f,
                    0.1f,
                    false,
                    ExpeditionFollowerMotorProfile.Human);
                blocked |= result.Blocked;
            }

            Assert(blocked, "Follower obstacle sweep never reported the intervening wall.");
            Assert(
                follower.position.x <= wall.transform.position.x - 0.5f,
                "Follower motor crossed or clipped through a solid wall.");
            Assert(IsFinite(follower.position), "Obstacle response produced a non-finite transform.");
        }

        private static void VerifyActorColliderIgnoring(Transform fixture, ExpeditionFollowerMotor motor)
        {
            motor.ResetMotion();
            var center = new Vector3(FixtureX + 80f, 0f, FixtureZ);
            CreateBox(
                fixture,
                "Actor-ignore floor",
                center + Vector3.down * 0.25f,
                new Vector3(14f, 0.5f, 14f));
            var actorCollider = CreateBox(
                fixture,
                "Ignored player collider",
                center + Vector3.up * 1.2f,
                new Vector3(0.6f, 2.4f, 2.4f));
            actorCollider.AddComponent<PlayerAvatar>();
            var follower = CreateFollower(
                fixture,
                "Actor-ignore follower",
                center + new Vector3(-3f, ExpeditionFollowerMotorProfile.Human.GroundOffset, 0f));
            Physics.SyncTransforms();

            for (var index = 0; index < 18; index++)
            {
                motor.Move(
                    follower,
                    center + new Vector3(3f, 0f, 0f),
                    0f,
                    0.1f,
                    false,
                    ExpeditionFollowerMotorProfile.Human);
            }

            Assert(
                follower.position.x > center.x + 0.6f,
                "Player/party/hostile actor filtering treated an actor collider as a world obstacle.");
            Assert(IsFinite(follower.position), "Actor-collider filtering produced a non-finite transform.");
        }

        private static void VerifySlopeCatchUp(Transform fixture, ExpeditionFollowerMotor motor)
        {
            motor.ResetMotion();
            var center = new Vector3(FixtureX + 120f, 0.55f, FixtureZ);
            var ramp = CreateBox(
                fixture,
                "Walkable follower ramp",
                center,
                new Vector3(8f, 0.5f, 4f));
            ramp.transform.rotation = Quaternion.Euler(0f, 0f, 8f);
            Physics.SyncTransforms();

            var startProbe = center + new Vector3(-3f, -0.2f, 0f);
            var targetProbe = center + new Vector3(3f, 0.65f, 0f);
            Assert(
                motor.TryProjectDestination(
                    startProbe,
                    startProbe.y,
                    ExpeditionFollowerMotorProfile.Human,
                    out var start),
                "Follower ramp start did not project to walkable ground.");
            Assert(
                motor.TryProjectDestination(
                    targetProbe,
                    targetProbe.y,
                    ExpeditionFollowerMotorProfile.Human,
                    out var target),
                "Follower ramp destination did not project to walkable ground.");

            var follower = CreateFollower(fixture, "Slope follower", start);
            var startingY = follower.position.y;
            for (var index = 0; index < 30; index++)
            {
                motor.Move(
                    follower,
                    targetProbe,
                    targetProbe.y,
                    0.1f,
                    false,
                    ExpeditionFollowerMotorProfile.Human);
            }

            Assert(
                follower.position.x > center.x + 1f &&
                follower.position.y > startingY + 0.45f,
                "Follower did not climb a modest walkable slope toward its formation destination.");
            Assert(
                follower.position.y <= target.y + 0.1f,
                "Slope catch-up overshot the grounded formation height.");
            Assert(IsFinite(follower.position), "Slope catch-up produced a non-finite transform.");
        }

        private static void VerifyElevationCatchUp(Transform fixture, ExpeditionFollowerMotor motor)
        {
            motor.ResetMotion();
            var center = new Vector3(FixtureX + 160f, 0f, FixtureZ);
            CreateBox(
                fixture,
                "Elevation lower floor",
                center + new Vector3(-4f, -0.25f, 0f),
                new Vector3(7f, 0.5f, 8f));
            CreateBox(
                fixture,
                "Elevation upper floor",
                center + new Vector3(4f, 3.75f, 0f),
                new Vector3(7f, 0.5f, 8f));
            var follower = CreateFollower(
                fixture,
                "Elevation follower",
                center + new Vector3(-4f, ExpeditionFollowerMotorProfile.Human.GroundOffset, 0f));
            Physics.SyncTransforms();

            var result = motor.Move(
                follower,
                center + new Vector3(4f, 4f, 0f),
                4f,
                0.1f,
                false,
                ExpeditionFollowerMotorProfile.Human);
            Assert(
                result.Warped,
                "A follower stranded several vertical metres away did not use bounded elevation catch-up.");
            Assert(
                Mathf.Abs(
                    follower.position.y -
                    (4f + ExpeditionFollowerMotorProfile.Human.GroundOffset)) < 0.03f,
                "Elevation catch-up did not land on the player's walkable floor.");
            Assert(
                motor.Velocity.sqrMagnitude < 0.0001f,
                "Elevation catch-up did not reset follower velocity after warping.");
        }

        private static void VerifySafeWarp(Transform fixture, ExpeditionFollowerMotor motor)
        {
            motor.ResetMotion();
            var center = new Vector3(FixtureX + 210f, 0f, FixtureZ);
            CreateBox(
                fixture,
                "Warp floor",
                center + Vector3.down * 0.25f,
                new Vector3(60f, 0.5f, 24f));
            var follower = CreateFollower(
                fixture,
                "Warp follower",
                center + new Vector3(-22f, ExpeditionFollowerMotorProfile.Human.GroundOffset, 0f));
            var blocker = CreateBox(
                fixture,
                "Unsafe warp volume",
                center + Vector3.up * 1.5f,
                new Vector3(6f, 3f, 6f));
            Physics.SyncTransforms();

            var rejected = motor.Move(
                follower,
                center,
                0f,
                0.1f,
                false,
                ExpeditionFollowerMotorProfile.Human);
            Assert(
                !rejected.Warped && follower.position.x < center.x - 15f,
                "Follower warped into or through an occupied destination.");

            UnityEngine.Object.DestroyImmediate(blocker);
            follower.position = center +
                                new Vector3(
                                    -22f,
                                    ExpeditionFollowerMotorProfile.Human.GroundOffset,
                                    0f);
            Physics.SyncTransforms();
            var accepted = motor.Move(
                follower,
                center,
                0f,
                0.1f,
                false,
                ExpeditionFollowerMotorProfile.Human);
            Assert(accepted.Warped, "A far follower did not use its safe bounded catch-up warp.");
            Assert(
                Vector3.Distance(
                    follower.position,
                    center + Vector3.up * ExpeditionFollowerMotorProfile.Human.GroundOffset) < 0.05f,
                "Safe follower warp did not use the grounded formation destination.");
            Assert(
                motor.Velocity.sqrMagnitude < 0.0001f,
                "Safe follower warp did not clear pre-warp velocity.");
            Assert(IsFinite(follower.position), "Safe follower warp produced a non-finite transform.");
        }

        private static void VerifyStackedFloorCatchUp(
            Transform fixture,
            ExpeditionFollowerMotor motor)
        {
            motor.ResetMotion();
            var center = new Vector3(FixtureX + 190f, 0f, FixtureZ);
            CreateBox(
                fixture,
                "Stacked-floor lower level",
                center + Vector3.down * 0.25f,
                new Vector3(10f, 0.5f, 10f));
            CreateBox(
                fixture,
                "Stacked-floor upper level",
                center + Vector3.up * 2.75f,
                new Vector3(8f, 0.5f, 8f));
            var follower = CreateFollower(
                fixture,
                "Stacked-floor follower",
                center + Vector3.up * ExpeditionFollowerMotorProfile.Human.GroundOffset);
            Physics.SyncTransforms();

            var result = motor.Move(
                follower,
                center + Vector3.up * 3f,
                3f,
                0.1f,
                false,
                ExpeditionFollowerMotorProfile.Human);
            Assert(
                result.Warped,
                "A follower directly below its formation slot remained stranded on the lower floor.");
            Assert(
                Mathf.Abs(
                    follower.position.y -
                    (3f + ExpeditionFollowerMotorProfile.Human.GroundOffset)) < 0.03f,
                "Stacked-floor catch-up did not land on the formation's walkable level.");
        }

        private static void VerifyFlyingHeightAndPause(Transform fixture, ExpeditionFollowerMotor motor)
        {
            motor.ResetMotion();
            var center = new Vector3(FixtureX + 260f, 0f, FixtureZ);
            CreateBox(
                fixture,
                "Flying Pact floor",
                center + Vector3.down * 0.25f,
                new Vector3(16f, 0.5f, 16f));
            Physics.SyncTransforms();

            Assert(
                motor.TryProjectDestination(
                    center,
                    0f,
                    ExpeditionFollowerMotorProfile.FlyingPact,
                    out var flyingPosition),
                "Flying Pact destination did not retain grounded terrain context.");
            Assert(
                flyingPosition.y >= 1.4f,
                "Flying-form Pact was not kept visibly airborne above projected ground.");

            var follower = CreateFollower(
                fixture,
                "Paused flying follower",
                center + new Vector3(-3f, flyingPosition.y, 0f));
            motor.Move(
                follower,
                center + new Vector3(3f, 0f, 0f),
                0f,
                0.1f,
                false,
                ExpeditionFollowerMotorProfile.FlyingPact);
            var positionBeforePause = follower.position;
            var velocityBeforePause = motor.Velocity;
            var paused = motor.Move(
                follower,
                center + new Vector3(3f, 0f, 0f),
                0f,
                0.1f,
                true,
                ExpeditionFollowerMotorProfile.FlyingPact);
            Assert(
                !paused.Moved &&
                follower.position == positionBeforePause &&
                motor.Velocity == velocityBeforePause,
                "Gameplay pause advanced or reset follower movement.");
            Assert(IsFinite(follower.position), "Paused follower retained a non-finite transform.");
        }

        private static void VerifyFlyingVerticalCatchUp(
            Transform fixture,
            ExpeditionFollowerMotor motor)
        {
            motor.ResetMotion();
            var center = new Vector3(FixtureX + 320f, 0f, FixtureZ);
            CreateBox(
                fixture,
                "Flying vertical lower floor",
                center + Vector3.down * 0.25f,
                new Vector3(10f, 0.5f, 10f));
            CreateBox(
                fixture,
                "Flying vertical upper floor",
                center + Vector3.up * 2.15f,
                new Vector3(8f, 0.5f, 8f));
            var follower = CreateFollower(
                fixture,
                "Flying vertical follower",
                center + Vector3.up * ExpeditionFollowerMotorProfile.FlyingPact.GroundOffset);
            Physics.SyncTransforms();

            var startingHeight = follower.position.y;
            var result = motor.Move(
                follower,
                center + Vector3.up * 2.4f,
                2.4f,
                0.1f,
                false,
                ExpeditionFollowerMotorProfile.FlyingPact);
            Assert(
                result.Moved && !result.Warped && follower.position.y > startingHeight + 0.2f,
                "A flying Pact did not smoothly rise toward a vertically separated formation slot.");
            Assert(IsFinite(follower.position), "Flying vertical catch-up produced a non-finite transform.");
        }

        private static Transform CreateFollower(
            Transform fixture,
            string name,
            Vector3 position)
        {
            var follower = new GameObject(name);
            follower.transform.SetParent(fixture, false);
            follower.transform.position = position;
            return follower.transform;
        }

        private static GameObject CreateBox(
            Transform fixture,
            string name,
            Vector3 position,
            Vector3 scale)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(fixture, false);
            box.transform.position = position;
            box.transform.localScale = scale;
            return box;
        }

        private static void AssertNoPhysicsOrLights(GameObject root, string owner)
        {
            Assert(
                root.GetComponentsInChildren<Collider>(true).Length == 0,
                $"{owner} follower integration added a collider.");
            Assert(
                root.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                $"{owner} follower integration added a Rigidbody.");
            Assert(
                root.GetComponentsInChildren<CharacterController>(true).Length == 0,
                $"{owner} follower integration added a CharacterController.");
            Assert(
                root.GetComponentsInChildren<Light>(true).Length == 0,
                $"{owner} follower integration added a light.");
        }

        private static T ReadField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException(
                    $"{instance.GetType().Name} field '{fieldName}' is missing.");
            }

            return (T)field.GetValue(instance);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
