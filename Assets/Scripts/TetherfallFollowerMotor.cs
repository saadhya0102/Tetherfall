using UnityEngine;

namespace Tetherfall
{
    public struct ExpeditionFollowerMotorProfile
    {
        public static readonly ExpeditionFollowerMotorProfile Human = new ExpeditionFollowerMotorProfile(
            radius: 0.4f,
            height: 2f,
            groundOffset: 0.96f,
            stoppingDistance: 1.1f,
            moveSpeed: 5.9f,
            catchUpDistance: 6f,
            catchUpSpeed: 8.8f,
            acceleration: 30f,
            maximumMovePerTick: 0.86f,
            maximumStepUp: 0.68f,
            maximumStepDown: 0.9f,
            maximumTargetGroundDelta: 5.5f,
            minimumGroundNormalY: 0.64f,
            warpHorizontalDistance: 18f,
            warpVerticalDistance: 3.25f,
            warpSearchRadius: 2.2f,
            flying: false);

        public static readonly ExpeditionFollowerMotorProfile GroundedPact = new ExpeditionFollowerMotorProfile(
            radius: 0.44f,
            height: 1.3f,
            groundOffset: 0.74f,
            stoppingDistance: 1f,
            moveSpeed: 6.4f,
            catchUpDistance: 6.5f,
            catchUpSpeed: 9.2f,
            acceleration: 32f,
            maximumMovePerTick: 0.9f,
            maximumStepUp: 0.7f,
            maximumStepDown: 0.94f,
            maximumTargetGroundDelta: 5.5f,
            minimumGroundNormalY: 0.62f,
            warpHorizontalDistance: 20f,
            warpVerticalDistance: 3.5f,
            warpSearchRadius: 2.25f,
            flying: false);

        public static readonly ExpeditionFollowerMotorProfile MountedPact = new ExpeditionFollowerMotorProfile(
            radius: 0.68f,
            height: 2.2f,
            groundOffset: 1.04f,
            stoppingDistance: 1.15f,
            moveSpeed: 6.8f,
            catchUpDistance: 7f,
            catchUpSpeed: 9.8f,
            acceleration: 30f,
            maximumMovePerTick: 0.94f,
            maximumStepUp: 0.72f,
            maximumStepDown: 0.96f,
            maximumTargetGroundDelta: 5.8f,
            minimumGroundNormalY: 0.62f,
            warpHorizontalDistance: 22f,
            warpVerticalDistance: 3.8f,
            warpSearchRadius: 2.5f,
            flying: false);

        public static readonly ExpeditionFollowerMotorProfile FlyingPact = new ExpeditionFollowerMotorProfile(
            radius: 0.46f,
            height: 0.92f,
            groundOffset: 1.48f,
            stoppingDistance: 1f,
            moveSpeed: 7f,
            catchUpDistance: 6.5f,
            catchUpSpeed: 10f,
            acceleration: 34f,
            maximumMovePerTick: 0.96f,
            maximumStepUp: 0.78f,
            maximumStepDown: 1.05f,
            maximumTargetGroundDelta: 6f,
            minimumGroundNormalY: 0.6f,
            warpHorizontalDistance: 22f,
            warpVerticalDistance: 4.1f,
            warpSearchRadius: 2.4f,
            flying: true);

        private ExpeditionFollowerMotorProfile(
            float radius,
            float height,
            float groundOffset,
            float stoppingDistance,
            float moveSpeed,
            float catchUpDistance,
            float catchUpSpeed,
            float acceleration,
            float maximumMovePerTick,
            float maximumStepUp,
            float maximumStepDown,
            float maximumTargetGroundDelta,
            float minimumGroundNormalY,
            float warpHorizontalDistance,
            float warpVerticalDistance,
            float warpSearchRadius,
            bool flying)
        {
            Radius = radius;
            Height = height;
            GroundOffset = groundOffset;
            StoppingDistance = stoppingDistance;
            MoveSpeed = moveSpeed;
            CatchUpDistance = catchUpDistance;
            CatchUpSpeed = catchUpSpeed;
            Acceleration = acceleration;
            MaximumMovePerTick = maximumMovePerTick;
            MaximumStepUp = maximumStepUp;
            MaximumStepDown = maximumStepDown;
            MaximumTargetGroundDelta = maximumTargetGroundDelta;
            MinimumGroundNormalY = minimumGroundNormalY;
            WarpHorizontalDistance = warpHorizontalDistance;
            WarpVerticalDistance = warpVerticalDistance;
            WarpSearchRadius = warpSearchRadius;
            Flying = flying;
        }

        public float Radius { get; }
        public float Height { get; }
        public float GroundOffset { get; }
        public float StoppingDistance { get; }
        public float MoveSpeed { get; }
        public float CatchUpDistance { get; }
        public float CatchUpSpeed { get; }
        public float Acceleration { get; }
        public float MaximumMovePerTick { get; }
        public float MaximumStepUp { get; }
        public float MaximumStepDown { get; }
        public float MaximumTargetGroundDelta { get; }
        public float MinimumGroundNormalY { get; }
        public float WarpHorizontalDistance { get; }
        public float WarpVerticalDistance { get; }
        public float WarpSearchRadius { get; }
        public bool Flying { get; }
    }

    public struct ExpeditionFollowerMotorResult
    {
        internal ExpeditionFollowerMotorResult(
            bool hasGroundedTarget,
            bool moved,
            bool warped,
            bool blocked,
            Vector3 targetPosition)
        {
            HasGroundedTarget = hasGroundedTarget;
            Moved = moved;
            Warped = warped;
            Blocked = blocked;
            TargetPosition = targetPosition;
        }

        public bool HasGroundedTarget { get; }
        public bool Moved { get; }
        public bool Warped { get; }
        public bool Blocked { get; }
        public Vector3 TargetPosition { get; }
    }

    /// <summary>
    /// Allocation-free transform motor for expedition followers. It deliberately owns no
    /// physics component: reusable queries provide grounding, clearance, and obstacle response.
    /// </summary>
    public sealed class ExpeditionFollowerMotor
    {
        private const int QueryCapacity = 32;
        private const float GroundProbeLift = 6f;
        private const float GroundProbeDistance = 14f;
        private const float ContactSkin = 0.045f;
        private const float MinimumMoveDistance = 0.008f;
        private const float MaximumDeltaTime = 0.1f;
        private const int WarpDirectionsPerRing = 8;
        private const int WarpRingCount = 2;

        private readonly RaycastHit[] groundHits = new RaycastHit[QueryCapacity];
        private readonly RaycastHit[] movementHits = new RaycastHit[QueryCapacity];
        private readonly Collider[] occupancyHits = new Collider[QueryCapacity];

        private Vector3 velocity;

        public Vector3 Velocity => velocity;

        public void ResetMotion()
        {
            velocity = Vector3.zero;
        }

        public bool TryProjectDestination(
            Vector3 formationDestination,
            float groundReferenceY,
            ExpeditionFollowerMotorProfile profile,
            out Vector3 projectedPosition)
        {
            return TryProjectSurface(
                formationDestination,
                groundReferenceY,
                profile.MaximumTargetGroundDelta,
                profile.MaximumTargetGroundDelta,
                profile,
                out projectedPosition,
                out _);
        }

        public ExpeditionFollowerMotorResult Move(
            Transform follower,
            Vector3 formationDestination,
            float groundReferenceY,
            float deltaTime,
            bool gameplayPaused,
            ExpeditionFollowerMotorProfile profile)
        {
            var currentPosition = follower != null ? follower.position : Vector3.zero;
            if (follower == null ||
                gameplayPaused ||
                !IsFinite(currentPosition) ||
                !IsFinite(formationDestination) ||
                !IsFinite(groundReferenceY) ||
                !IsFinite(deltaTime) ||
                deltaTime <= 0f)
            {
                return new ExpeditionFollowerMotorResult(
                    false,
                    false,
                    false,
                    false,
                    currentPosition);
            }

            var stepDeltaTime = Mathf.Min(deltaTime, MaximumDeltaTime);
            if (!TryProjectSurface(
                    formationDestination,
                    groundReferenceY,
                    profile.MaximumTargetGroundDelta,
                    profile.MaximumTargetGroundDelta,
                    profile,
                    out var groundedTarget,
                    out _))
            {
                velocity = Vector3.MoveTowards(
                    velocity,
                    Vector3.zero,
                    profile.Acceleration * stepDeltaTime);
                return new ExpeditionFollowerMotorResult(
                    false,
                    false,
                    false,
                    false,
                    currentPosition);
            }

            var targetDelta = groundedTarget - currentPosition;
            var planarTargetDelta = new Vector3(targetDelta.x, 0f, targetDelta.z);
            var planarDistance = planarTargetDelta.magnitude;
            var verticalDistance = Mathf.Abs(targetDelta.y);
            var strandedOnDifferentFloor =
                !profile.Flying &&
                planarDistance <= profile.StoppingDistance &&
                verticalDistance > Mathf.Max(profile.MaximumStepUp, profile.MaximumStepDown) + ContactSkin;
            if ((planarDistance >= profile.WarpHorizontalDistance ||
                 verticalDistance >= profile.WarpVerticalDistance ||
                 strandedOnDifferentFloor) &&
                TryFindWarpDestination(
                    formationDestination,
                    groundReferenceY,
                    follower,
                    profile,
                    out var warpDestination))
            {
                follower.position = warpDestination;
                velocity = Vector3.zero;
                return new ExpeditionFollowerMotorResult(
                    true,
                    true,
                    true,
                    false,
                    groundedTarget);
            }

            if (planarDistance <= profile.StoppingDistance)
            {
                velocity = Vector3.MoveTowards(
                    velocity,
                    Vector3.zero,
                    profile.Acceleration * stepDeltaTime);
                var settled = currentPosition;
                var settleTarget = groundedTarget;
                var currentGroundY = currentPosition.y - profile.GroundOffset;
                if (!profile.Flying &&
                    TryProjectSurface(
                        currentPosition,
                        currentGroundY,
                        profile.MaximumStepUp,
                        profile.MaximumStepDown,
                        profile,
                        out var localGround,
                        out _))
                {
                    settleTarget = localGround;
                }

                var verticalDelta = settleTarget.y - settled.y;
                if (profile.Flying ||
                    verticalDelta <= profile.MaximumStepUp &&
                    verticalDelta >= -profile.MaximumStepDown)
                {
                    settled.y = Mathf.MoveTowards(
                        settled.y,
                        settleTarget.y,
                        profile.CatchUpSpeed * stepDeltaTime);
                    if (IsFinite(settled))
                    {
                        follower.position = settled;
                    }
                }

                return new ExpeditionFollowerMotorResult(
                    true,
                    (settled - currentPosition).sqrMagnitude > MinimumMoveDistance * MinimumMoveDistance,
                    false,
                    false,
                    groundedTarget);
            }

            var targetSpeed = planarDistance > profile.CatchUpDistance
                ? profile.CatchUpSpeed
                : profile.MoveSpeed;
            var desiredVelocity = planarTargetDelta / planarDistance * targetSpeed;
            var planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
            planarVelocity = Vector3.MoveTowards(
                planarVelocity,
                desiredVelocity,
                profile.Acceleration * stepDeltaTime);
            var requestedMotion = planarVelocity * stepDeltaTime;
            var maximumDistance = Mathf.Min(
                profile.MaximumMovePerTick,
                Mathf.Max(0f, planarDistance - profile.StoppingDistance));
            if (requestedMotion.magnitude > maximumDistance)
            {
                requestedMotion = requestedMotion.normalized * maximumDistance;
            }

            if (requestedMotion.sqrMagnitude <= MinimumMoveDistance * MinimumMoveDistance)
            {
                velocity = Vector3.zero;
                return new ExpeditionFollowerMotorResult(
                    true,
                    false,
                    false,
                    false,
                    groundedTarget);
            }

            var requestedDistance = requestedMotion.magnitude;
            var requestedDirection = requestedMotion / requestedDistance;
            TryMoveSegment(
                currentPosition,
                requestedDirection,
                requestedDistance,
                follower,
                profile,
                out var resolvedPosition,
                out var blocked,
                out var blockingNormal);

            var directDistance = PlanarDistance(currentPosition, resolvedPosition);
            var remainingDistance = Mathf.Max(0f, requestedDistance - directDistance);
            if (blocked && remainingDistance > MinimumMoveDistance)
            {
                var horizontalNormal = new Vector3(blockingNormal.x, 0f, blockingNormal.z);
                if (horizontalNormal.sqrMagnitude > 0.001f)
                {
                    horizontalNormal.Normalize();
                    var tangent = Vector3.Cross(Vector3.up, horizontalNormal);
                    var tangentAlignment = Vector3.Dot(tangent, requestedDirection);
                    if (tangentAlignment < -0.02f ||
                        (Mathf.Abs(tangentAlignment) <= 0.02f &&
                         tangent.x + tangent.z < 0f))
                    {
                        tangent = -tangent;
                    }

                    TryMoveSegment(
                        resolvedPosition,
                        tangent,
                        remainingDistance * 0.82f,
                        follower,
                        profile,
                        out var slidPosition,
                        out _,
                        out _);
                    if (PlanarDistance(resolvedPosition, slidPosition) > MinimumMoveDistance)
                    {
                        resolvedPosition = slidPosition;
                    }
                }
            }

            var displacement = resolvedPosition - currentPosition;
            if (!IsFinite(resolvedPosition) || !IsFinite(displacement))
            {
                velocity = Vector3.zero;
                return new ExpeditionFollowerMotorResult(
                    true,
                    false,
                    false,
                    true,
                    groundedTarget);
            }

            follower.position = resolvedPosition;
            velocity = displacement / stepDeltaTime;
            if (!IsFinite(velocity))
            {
                velocity = Vector3.zero;
            }

            return new ExpeditionFollowerMotorResult(
                true,
                displacement.sqrMagnitude > MinimumMoveDistance * MinimumMoveDistance,
                false,
                blocked,
                groundedTarget);
        }

        private bool TryFindWarpDestination(
            Vector3 formationDestination,
            float groundReferenceY,
            Transform follower,
            ExpeditionFollowerMotorProfile profile,
            out Vector3 warpDestination)
        {
            if (TryValidateWarpCandidate(
                    formationDestination,
                    groundReferenceY,
                    follower,
                    profile,
                    out warpDestination))
            {
                return true;
            }

            for (var ring = 1; ring <= WarpRingCount; ring++)
            {
                var radius = profile.WarpSearchRadius * ring / WarpRingCount;
                for (var directionIndex = 0;
                     directionIndex < WarpDirectionsPerRing;
                     directionIndex++)
                {
                    var angle = directionIndex * Mathf.PI * 2f / WarpDirectionsPerRing;
                    var candidate = formationDestination + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius);
                    if (TryValidateWarpCandidate(
                            candidate,
                            groundReferenceY,
                            follower,
                            profile,
                            out warpDestination))
                    {
                        return true;
                    }
                }
            }

            warpDestination = Vector3.zero;
            return false;
        }

        private bool TryValidateWarpCandidate(
            Vector3 candidate,
            float groundReferenceY,
            Transform follower,
            ExpeditionFollowerMotorProfile profile,
            out Vector3 warpDestination)
        {
            if (!TryProjectSurface(
                    candidate,
                    groundReferenceY,
                    profile.MaximumTargetGroundDelta,
                    profile.MaximumTargetGroundDelta,
                    profile,
                    out warpDestination,
                    out var supportCollider))
            {
                return false;
            }

            return IsPositionClear(
                warpDestination,
                supportCollider,
                follower,
                profile);
        }

        private bool TryMoveSegment(
            Vector3 startPosition,
            Vector3 planarDirection,
            float requestedDistance,
            Transform follower,
            ExpeditionFollowerMotorProfile profile,
            out Vector3 resolvedPosition,
            out bool blocked,
            out Vector3 blockingNormal)
        {
            resolvedPosition = startPosition;
            blocked = false;
            blockingNormal = Vector3.zero;
            planarDirection.y = 0f;
            if (requestedDistance <= MinimumMoveDistance ||
                planarDirection.sqrMagnitude <= 0.001f)
            {
                return false;
            }

            planarDirection.Normalize();
            var currentGroundY = startPosition.y - profile.GroundOffset;
            var requestedPlanarPosition =
                startPosition + planarDirection * requestedDistance;
            if (!TryProjectSurface(
                    requestedPlanarPosition,
                    currentGroundY,
                    profile.MaximumStepUp,
                    profile.MaximumStepDown,
                    profile,
                    out var requestedPosition,
                    out var destinationSupport))
            {
                blocked = true;
                return false;
            }

            var castStart = startPosition +
                            Vector3.up * Mathf.Max(0f, requestedPosition.y - startPosition.y);
            var allowedDistance = GetAllowedMovementDistance(
                castStart,
                planarDirection,
                requestedDistance,
                destinationSupport,
                follower,
                profile,
                out blockingNormal);
            blocked = allowedDistance + MinimumMoveDistance < requestedDistance;
            if (allowedDistance <= MinimumMoveDistance)
            {
                return false;
            }

            if (blocked)
            {
                var allowedPlanarPosition =
                    startPosition + planarDirection * allowedDistance;
                if (!TryProjectSurface(
                        allowedPlanarPosition,
                        currentGroundY,
                        profile.MaximumStepUp,
                        profile.MaximumStepDown,
                        profile,
                        out requestedPosition,
                        out destinationSupport))
                {
                    return false;
                }
            }

            if (!IsPositionClear(
                    requestedPosition,
                    destinationSupport,
                    follower,
                    profile))
            {
                blocked = true;
                return false;
            }

            resolvedPosition = requestedPosition;
            return true;
        }

        private float GetAllowedMovementDistance(
            Vector3 rootPosition,
            Vector3 direction,
            float distance,
            Collider destinationSupport,
            Transform follower,
            ExpeditionFollowerMotorProfile profile,
            out Vector3 blockingNormal)
        {
            int hitCount;
            if (profile.Flying)
            {
                hitCount = Physics.SphereCastNonAlloc(
                    rootPosition,
                    profile.Radius,
                    direction,
                    movementHits,
                    distance + ContactSkin,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);
            }
            else
            {
                GetCapsulePoints(
                    rootPosition,
                    profile,
                    out var bottom,
                    out var top);
                hitCount = Physics.CapsuleCastNonAlloc(
                    bottom,
                    top,
                    profile.Radius,
                    direction,
                    movementHits,
                    distance + ContactSkin,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);
            }

            var saturated = hitCount >= movementHits.Length;
            var closestDistance = saturated ? 0f : distance + ContactSkin;
            blockingNormal = Vector3.zero;
            for (var index = 0; index < hitCount; index++)
            {
                var hit = movementHits[index];
                if (hit.collider == null ||
                    hit.collider == destinationSupport ||
                    IsActorCollider(hit.collider) ||
                    IsOwnedCollider(hit.collider, follower) ||
                    hit.normal.y >= profile.MinimumGroundNormalY)
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    blockingNormal = hit.normal;
                }
            }

            ClearHits(movementHits, hitCount);
            return Mathf.Clamp(closestDistance - ContactSkin, 0f, distance);
        }

        private bool IsPositionClear(
            Vector3 rootPosition,
            Collider supportCollider,
            Transform follower,
            ExpeditionFollowerMotorProfile profile)
        {
            GetCapsulePoints(
                rootPosition,
                profile,
                out var bottom,
                out var top);
            var overlapCount = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                profile.Radius,
                occupancyHits,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            if (overlapCount >= occupancyHits.Length)
            {
                ClearColliders(occupancyHits, overlapCount);
                return false;
            }

            var groundY = rootPosition.y - profile.GroundOffset;
            var clear = true;
            for (var index = 0; index < overlapCount; index++)
            {
                var collider = occupancyHits[index];
                if (collider == null ||
                    collider == supportCollider ||
                    IsActorCollider(collider) ||
                    IsOwnedCollider(collider, follower) ||
                    collider.bounds.max.y <= groundY + ContactSkin * 2f)
                {
                    continue;
                }

                clear = false;
                break;
            }

            ClearColliders(occupancyHits, overlapCount);
            return clear;
        }

        private bool TryProjectSurface(
            Vector3 desiredPosition,
            float referenceGroundY,
            float maximumRise,
            float maximumDrop,
            ExpeditionFollowerMotorProfile profile,
            out Vector3 projectedRootPosition,
            out Collider supportCollider)
        {
            projectedRootPosition = desiredPosition;
            supportCollider = null;
            if (!IsFinite(desiredPosition) ||
                !IsFinite(referenceGroundY) ||
                maximumRise < 0f ||
                maximumDrop < 0f)
            {
                return false;
            }

            var origin = new Vector3(
                desiredPosition.x,
                referenceGroundY + GroundProbeLift,
                desiredPosition.z);
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                GroundProbeDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            if (hitCount >= groundHits.Length)
            {
                ClearHits(groundHits, hitCount);
                return false;
            }

            var bestDifference = float.PositiveInfinity;
            var bestDistance = float.PositiveInfinity;
            var bestPoint = Vector3.zero;
            for (var index = 0; index < hitCount; index++)
            {
                var hit = groundHits[index];
                if (hit.collider == null ||
                    IsActorCollider(hit.collider) ||
                    hit.normal.y < profile.MinimumGroundNormalY ||
                    !IsFinite(hit.point))
                {
                    continue;
                }

                var heightDelta = hit.point.y - referenceGroundY;
                if (heightDelta > maximumRise || heightDelta < -maximumDrop)
                {
                    continue;
                }

                var difference = Mathf.Abs(heightDelta);
                if (difference < bestDifference - 0.001f ||
                    (Mathf.Abs(difference - bestDifference) <= 0.001f &&
                     hit.distance < bestDistance))
                {
                    bestDifference = difference;
                    bestDistance = hit.distance;
                    bestPoint = hit.point;
                    supportCollider = hit.collider;
                }
            }

            ClearHits(groundHits, hitCount);
            if (supportCollider == null)
            {
                return false;
            }

            projectedRootPosition = bestPoint + Vector3.up * profile.GroundOffset;
            return IsFinite(projectedRootPosition);
        }

        private static void GetCapsulePoints(
            Vector3 rootPosition,
            ExpeditionFollowerMotorProfile profile,
            out Vector3 bottom,
            out Vector3 top)
        {
            if (profile.Flying)
            {
                bottom = rootPosition;
                top = rootPosition;
                return;
            }

            var groundY = rootPosition.y - profile.GroundOffset;
            var bottomY = groundY + profile.Radius + ContactSkin;
            var topY = groundY + Mathf.Max(
                profile.Radius + ContactSkin,
                profile.Height - profile.Radius - ContactSkin);
            bottom = new Vector3(rootPosition.x, bottomY, rootPosition.z);
            top = new Vector3(rootPosition.x, topY, rootPosition.z);
        }

        private static bool IsActorCollider(Collider collider)
        {
            return collider.GetComponentInParent<PlayerAvatar>() != null ||
                   collider.GetComponentInParent<PactCreature>() != null ||
                   collider.GetComponentInParent<HumanCompanion>() != null ||
                   collider.GetComponentInParent<Enemy>() != null ||
                   collider.GetComponentInParent<DungeonBoss>() != null;
        }

        private static bool IsOwnedCollider(Collider collider, Transform owner)
        {
            return owner != null &&
                   (collider.transform == owner ||
                    collider.transform.IsChildOf(owner));
        }

        private static float PlanarDistance(Vector3 first, Vector3 second)
        {
            var deltaX = second.x - first.x;
            var deltaZ = second.z - first.z;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        private static void ClearHits(RaycastHit[] buffer, int count)
        {
            var clearCount = Mathf.Min(count, buffer.Length);
            for (var index = 0; index < clearCount; index++)
            {
                buffer[index] = default;
            }
        }

        private static void ClearColliders(Collider[] buffer, int count)
        {
            var clearCount = Mathf.Min(count, buffer.Length);
            for (var index = 0; index < clearCount; index++)
            {
                buffer[index] = null;
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
