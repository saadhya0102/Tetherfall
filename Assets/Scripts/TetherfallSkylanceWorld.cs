using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tetherfall
{
    public enum SkylanceZone
    {
        AlpineApproach,
        AerieKingdom
    }

    public enum SkylanceMaterialSlot
    {
        Snow,
        BlueIce,
        DarkRock,
        WeatheredStone,
        Copper,
        Cloudglass,
        Timber,
        WindGlow
    }

    public enum SkylanceRouteKind
    {
        GroundedSwitchback,
        GatedAscent,
        AerialCircuit,
        SafeDescent
    }

    public enum SkylanceRouteAccess
    {
        Open,
        FlightPactOrAuthoredAscent,
        AlwaysReturn
    }

    public enum SkylanceRouteNodeKind
    {
        Waypoint,
        Launch,
        LiftRing,
        LandingPerch,
        ReturnPerch
    }

    public enum SkylanceAccessReason
    {
        MountainRouteOpen,
        FlightCapablePact,
        AuthoredAscent,
        ReturnRouteAlwaysOpen,
        AscentRouteUnavailable,
        FlightCapabilityRequired,
        InvalidState
    }

    public enum SkylanceLandingFailure
    {
        None,
        InvalidInput,
        NoSurface,
        SurfaceTooSteep,
        EdgeClearanceTooSmall,
        Occupied,
        ExcessiveDrop
    }

    public enum SkylanceSocketKind
    {
        Vendor,
        Quest,
        Boss,
        Encounter,
        Landing,
        Respawn
    }

    public readonly struct SkylanceAccessState
    {
        public SkylanceAccessState(
            bool hasFlightCapablePact,
            bool hasAuthoredAscent,
            bool ascentRouteAvailable,
            bool isReturnTravel)
        {
            HasFlightCapablePact = hasFlightCapablePact;
            HasAuthoredAscent = hasAuthoredAscent;
            AscentRouteAvailable = ascentRouteAvailable;
            IsReturnTravel = isReturnTravel;
        }

        public bool HasFlightCapablePact { get; }
        public bool HasAuthoredAscent { get; }
        public bool AscentRouteAvailable { get; }
        public bool IsReturnTravel { get; }
    }

    public readonly struct SkylanceAccessDecision
    {
        public SkylanceAccessDecision(bool allowed, SkylanceAccessReason reason)
        {
            Allowed = allowed;
            Reason = reason;
        }

        public bool Allowed { get; }
        public SkylanceAccessReason Reason { get; }
    }

    /// <summary>
    /// Pure access rules. Every method is deterministic and performs no per-call allocation.
    /// The roster adapter is intentionally separate from the rule evaluation so save, quest,
    /// test, and network layers can all present the same compact state.
    /// </summary>
    public static class SkylanceAccessRules
    {
        public static SkylanceAccessDecision EvaluateKingdomAccess(
            in SkylanceAccessState state)
        {
            if (state.IsReturnTravel)
            {
                return new SkylanceAccessDecision(
                    true,
                    SkylanceAccessReason.ReturnRouteAlwaysOpen);
            }

            if (!state.AscentRouteAvailable)
            {
                return new SkylanceAccessDecision(
                    false,
                    SkylanceAccessReason.AscentRouteUnavailable);
            }

            if (state.HasFlightCapablePact)
            {
                return new SkylanceAccessDecision(
                    true,
                    SkylanceAccessReason.FlightCapablePact);
            }

            if (state.HasAuthoredAscent)
            {
                return new SkylanceAccessDecision(
                    true,
                    SkylanceAccessReason.AuthoredAscent);
            }

            return new SkylanceAccessDecision(
                false,
                SkylanceAccessReason.FlightCapabilityRequired);
        }

        public static SkylanceAccessDecision EvaluateRouteAccess(
            SkylanceRouteAccess access,
            in SkylanceAccessState state)
        {
            switch (access)
            {
                case SkylanceRouteAccess.Open:
                    return new SkylanceAccessDecision(
                        true,
                        SkylanceAccessReason.MountainRouteOpen);
                case SkylanceRouteAccess.AlwaysReturn:
                    return new SkylanceAccessDecision(
                        true,
                        SkylanceAccessReason.ReturnRouteAlwaysOpen);
                case SkylanceRouteAccess.FlightPactOrAuthoredAscent:
                    return EvaluateKingdomAccess(in state);
                default:
                    return new SkylanceAccessDecision(
                        false,
                        SkylanceAccessReason.InvalidState);
            }
        }

        public static bool HasFlightCapablePact(
            IReadOnlyList<CapturedPactSaveData> roster)
        {
            if (roster == null)
            {
                return false;
            }

            for (var index = 0; index < roster.Count; index++)
            {
                var record = roster[index];
                if (record == null || string.IsNullOrEmpty(record.speciesId))
                {
                    continue;
                }

                var species = TetherfallAccordSpeciesCatalog.Find(record.speciesId);
                if (species?.movement?.usesFlyingMotor == true)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasUnfoldedFlightSupport(
            IReadOnlyList<PactProgressSaveData> progress)
        {
            if (progress == null)
            {
                return false;
            }

            for (var index = 0; index < progress.Count; index++)
            {
                var record = progress[index];
                if (record != null &&
                    string.Equals(
                        record.unfoldedFormId,
                        TetherfallPactCatalog.SuncrestFormId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static SkylanceAccessState CaptureGameState(
            TetherfallGame game,
            bool authoredAscent,
            bool ascentRouteAvailable,
            bool isReturnTravel)
        {
            if (game == null)
            {
                return new SkylanceAccessState(
                    false,
                    authoredAscent,
                    ascentRouteAvailable,
                    isReturnTravel);
            }

            var flightCapability =
                HasFlightCapablePact(game.PactRoster) ||
                HasUnfoldedFlightSupport(game.PactProgressRecords);
            return new SkylanceAccessState(
                flightCapability,
                authoredAscent,
                ascentRouteAvailable,
                isReturnTravel);
        }
    }

    public readonly struct SkylanceRouteNode
    {
        public SkylanceRouteNode(
            string stableId,
            Vector3 position,
            float captureRadius,
            float liftVelocity,
            SkylanceRouteNodeKind kind)
        {
            StableId = stableId ?? string.Empty;
            Position = position;
            CaptureRadius = Mathf.Max(0f, captureRadius);
            LiftVelocity = Mathf.Max(0f, liftVelocity);
            Kind = kind;
        }

        public string StableId { get; }
        public Vector3 Position { get; }
        public float CaptureRadius { get; }
        public float LiftVelocity { get; }
        public SkylanceRouteNodeKind Kind { get; }
    }

    public sealed class SkylanceFlightRouteDefinition
    {
        private readonly SkylanceRouteNode[] nodes;

        internal SkylanceFlightRouteDefinition(
            string stableId,
            string displayName,
            SkylanceRouteKind kind,
            SkylanceRouteAccess access,
            bool closedCircuit,
            SkylanceRouteNode[] routeNodes)
        {
            StableId = stableId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Kind = kind;
            Access = access;
            ClosedCircuit = closedCircuit;
            nodes = routeNodes ?? Array.Empty<SkylanceRouteNode>();
        }

        public string StableId { get; }
        public string DisplayName { get; }
        public SkylanceRouteKind Kind { get; }
        public SkylanceRouteAccess Access { get; }
        public bool ClosedCircuit { get; }
        public IReadOnlyList<SkylanceRouteNode> Nodes => nodes;
        public int NodeCount => nodes.Length;

        public bool TryGetNode(int index, out SkylanceRouteNode node)
        {
            if (index < 0 || index >= nodes.Length)
            {
                node = default;
                return false;
            }

            node = nodes[index];
            return true;
        }
    }

    public readonly struct SkylanceRouteSample
    {
        public SkylanceRouteSample(
            Vector3 position,
            Vector3 tangent,
            int segmentIndex,
            float segmentProgress)
        {
            Position = position;
            Tangent = tangent;
            SegmentIndex = segmentIndex;
            SegmentProgress = segmentProgress;
        }

        public Vector3 Position { get; }
        public Vector3 Tangent { get; }
        public int SegmentIndex { get; }
        public float SegmentProgress { get; }
    }

    public readonly struct SkylanceLandingPolicy
    {
        public SkylanceLandingPolicy(
            float footprintRadius,
            float bodyHeight,
            float requiredEdgeClearance,
            float maximumSlopeDegrees,
            float maximumDrop,
            float probeLift,
            float searchRadius)
        {
            FootprintRadius = Mathf.Max(0.05f, footprintRadius);
            BodyHeight = Mathf.Max(FootprintRadius * 2f, bodyHeight);
            RequiredEdgeClearance = Mathf.Max(0f, requiredEdgeClearance);
            MaximumSlopeDegrees = Mathf.Clamp(maximumSlopeDegrees, 0f, 60f);
            MaximumDrop = Mathf.Max(0.1f, maximumDrop);
            ProbeLift = Mathf.Max(0.1f, probeLift);
            SearchRadius = Mathf.Max(0f, searchRadius);
        }

        public float FootprintRadius { get; }
        public float BodyHeight { get; }
        public float RequiredEdgeClearance { get; }
        public float MaximumSlopeDegrees { get; }
        public float MaximumDrop { get; }
        public float ProbeLift { get; }
        public float SearchRadius { get; }

        public static SkylanceLandingPolicy SafeMount =>
            new SkylanceLandingPolicy(
                0.82f,
                2.65f,
                0.42f,
                34f,
                8f,
                4f,
                3.2f);
    }

    public readonly struct SkylanceLandingProbe
    {
        public SkylanceLandingProbe(
            Vector3 desiredPosition,
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            bool hasSurface,
            bool occupied,
            float edgeClearance,
            float dropDistance)
        {
            DesiredPosition = desiredPosition;
            SurfacePoint = surfacePoint;
            SurfaceNormal = surfaceNormal;
            HasSurface = hasSurface;
            Occupied = occupied;
            EdgeClearance = edgeClearance;
            DropDistance = dropDistance;
        }

        public Vector3 DesiredPosition { get; }
        public Vector3 SurfacePoint { get; }
        public Vector3 SurfaceNormal { get; }
        public bool HasSurface { get; }
        public bool Occupied { get; }
        public float EdgeClearance { get; }
        public float DropDistance { get; }
    }

    /// <summary>
    /// Pure landing acceptance plus a bounded physics adapter that requires caller-owned
    /// NonAlloc buffers. Candidate and support order are fixed, making selection deterministic.
    /// PlayerAvatar and PactCreature can adopt this helper later without changing this module.
    /// </summary>
    public static class SkylanceLandingRules
    {
        private static readonly Vector2[] CandidateOffsets =
        {
            Vector2.zero,
            new Vector2(0f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, -1f),
            new Vector2(-1f, 0f),
            new Vector2(0.7071068f, 0.7071068f),
            new Vector2(0.7071068f, -0.7071068f),
            new Vector2(-0.7071068f, -0.7071068f),
            new Vector2(-0.7071068f, 0.7071068f)
        };

        private static readonly Vector2[] SupportDirections =
        {
            new Vector2(0f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, -1f),
            new Vector2(-1f, 0f),
            new Vector2(0.7071068f, 0.7071068f),
            new Vector2(0.7071068f, -0.7071068f),
            new Vector2(-0.7071068f, -0.7071068f),
            new Vector2(-0.7071068f, 0.7071068f)
        };

        public static SkylanceLandingFailure Evaluate(
            in SkylanceLandingProbe probe,
            in SkylanceLandingPolicy policy)
        {
            if (!IsFinite(probe.DesiredPosition) ||
                !IsFinite(probe.SurfacePoint) ||
                !IsFinite(probe.SurfaceNormal) ||
                !IsFinite(probe.EdgeClearance) ||
                !IsFinite(probe.DropDistance) ||
                !IsFinite(policy.FootprintRadius) ||
                !IsFinite(policy.BodyHeight) ||
                !IsFinite(policy.RequiredEdgeClearance) ||
                !IsFinite(policy.MaximumSlopeDegrees) ||
                !IsFinite(policy.MaximumDrop))
            {
                return SkylanceLandingFailure.InvalidInput;
            }

            if (!probe.HasSurface || probe.SurfaceNormal.sqrMagnitude < 0.5f)
            {
                return SkylanceLandingFailure.NoSurface;
            }

            var minimumNormalY =
                Mathf.Cos(policy.MaximumSlopeDegrees * Mathf.Deg2Rad);
            if (probe.SurfaceNormal.normalized.y < minimumNormalY)
            {
                return SkylanceLandingFailure.SurfaceTooSteep;
            }

            if (probe.DropDistance < 0f ||
                probe.DropDistance > policy.MaximumDrop)
            {
                return SkylanceLandingFailure.ExcessiveDrop;
            }

            var requiredClearance =
                policy.FootprintRadius + policy.RequiredEdgeClearance;
            if (probe.EdgeClearance < requiredClearance)
            {
                return SkylanceLandingFailure.EdgeClearanceTooSmall;
            }

            return probe.Occupied
                ? SkylanceLandingFailure.Occupied
                : SkylanceLandingFailure.None;
        }

        public static bool TryResolveSafeLandingNonAlloc(
            Vector3 desiredPosition,
            Transform ignoredActorRoot,
            float footOffset,
            in SkylanceLandingPolicy policy,
            int layerMask,
            RaycastHit[] groundHits,
            Collider[] occupancyHits,
            out Vector3 landingPosition,
            out SkylanceLandingFailure failure)
        {
            landingPosition = desiredPosition;
            failure = SkylanceLandingFailure.InvalidInput;
            if (!IsFinite(desiredPosition) ||
                !IsFinite(footOffset) ||
                groundHits == null ||
                groundHits.Length == 0 ||
                occupancyHits == null ||
                occupancyHits.Length == 0)
            {
                return false;
            }

            var latestFailure = SkylanceLandingFailure.NoSurface;
            for (var candidateIndex = 0;
                 candidateIndex < CandidateOffsets.Length;
                 candidateIndex++)
            {
                var offset = CandidateOffsets[candidateIndex] * policy.SearchRadius;
                var candidate = desiredPosition +
                                new Vector3(offset.x, 0f, offset.y);
                if (!TryProbeSurfaceNonAlloc(
                        candidate,
                        ignoredActorRoot,
                        in policy,
                        layerMask,
                        groundHits,
                        out var surfaceHit))
                {
                    latestFailure = SkylanceLandingFailure.NoSurface;
                    continue;
                }

                var supportRadius =
                    policy.FootprintRadius + policy.RequiredEdgeClearance;
                var supported = HasRadialSupportNonAlloc(
                    surfaceHit.point,
                    surfaceHit.normal,
                    supportRadius,
                    ignoredActorRoot,
                    in policy,
                    layerMask,
                    groundHits);
                var occupied = IsCapsuleOccupiedNonAlloc(
                    surfaceHit.point,
                    surfaceHit.collider,
                    ignoredActorRoot,
                    in policy,
                    layerMask,
                    occupancyHits);
                var probe = new SkylanceLandingProbe(
                    desiredPosition,
                    surfaceHit.point,
                    surfaceHit.normal,
                    true,
                    occupied,
                    supported ? supportRadius + 0.01f : 0f,
                    Mathf.Abs(desiredPosition.y - surfaceHit.point.y));
                latestFailure = Evaluate(in probe, in policy);
                if (latestFailure != SkylanceLandingFailure.None)
                {
                    continue;
                }

                landingPosition =
                    surfaceHit.point + Vector3.up * Mathf.Max(0f, footOffset);
                failure = SkylanceLandingFailure.None;
                return IsFinite(landingPosition);
            }

            failure = latestFailure;
            return false;
        }

        private static bool TryProbeSurfaceNonAlloc(
            Vector3 candidate,
            Transform ignoredActorRoot,
            in SkylanceLandingPolicy policy,
            int layerMask,
            RaycastHit[] hits,
            out RaycastHit selected)
        {
            selected = default;
            var origin = candidate + Vector3.up * policy.ProbeLift;
            var count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                hits,
                policy.ProbeLift + policy.MaximumDrop,
                layerMask,
                QueryTriggerInteraction.Ignore);
            var selectedDistance = float.PositiveInfinity;
            for (var index = 0; index < count; index++)
            {
                var hit = hits[index];
                if (hit.collider == null ||
                    IsIgnoredActorCollider(hit.collider, ignoredActorRoot) ||
                    !IsFinite(hit.point) ||
                    !IsFinite(hit.normal) ||
                    hit.distance < 0f ||
                    hit.distance >= selectedDistance)
                {
                    continue;
                }

                selected = hit;
                selectedDistance = hit.distance;
            }

            return selected.collider != null;
        }

        private static bool HasRadialSupportNonAlloc(
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            float radius,
            Transform ignoredActorRoot,
            in SkylanceLandingPolicy policy,
            int layerMask,
            RaycastHit[] hits)
        {
            var minimumNormalY =
                Mathf.Cos(policy.MaximumSlopeDegrees * Mathf.Deg2Rad);
            for (var index = 0; index < SupportDirections.Length; index++)
            {
                var direction = SupportDirections[index];
                var probe = surfacePoint +
                            new Vector3(direction.x * radius, 0.35f, direction.y * radius);
                var count = Physics.RaycastNonAlloc(
                    probe,
                    Vector3.down,
                    hits,
                    1.25f,
                    layerMask,
                    QueryTriggerInteraction.Ignore);
                var foundSupport = false;
                for (var hitIndex = 0; hitIndex < count; hitIndex++)
                {
                    var hit = hits[hitIndex];
                    if (hit.collider == null ||
                        IsIgnoredActorCollider(hit.collider, ignoredActorRoot) ||
                        hit.normal.y < minimumNormalY ||
                        Mathf.Abs(hit.point.y - surfacePoint.y) > 0.7f)
                    {
                        continue;
                    }

                    foundSupport = true;
                    break;
                }

                if (!foundSupport)
                {
                    return false;
                }
            }

            return surfaceNormal.y >= minimumNormalY;
        }

        private static bool IsCapsuleOccupiedNonAlloc(
            Vector3 surfacePoint,
            Collider supportCollider,
            Transform ignoredActorRoot,
            in SkylanceLandingPolicy policy,
            int layerMask,
            Collider[] hits)
        {
            var radius = policy.FootprintRadius;
            var bottom = surfacePoint + Vector3.up * (radius + 0.08f);
            var top = surfacePoint +
                      Vector3.up * Mathf.Max(radius + 0.08f, policy.BodyHeight - radius);
            var count = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                radius,
                hits,
                layerMask,
                QueryTriggerInteraction.Ignore);
            for (var index = 0; index < count; index++)
            {
                var collider = hits[index];
                if (collider == null ||
                    collider == supportCollider ||
                    IsIgnoredActorCollider(collider, ignoredActorRoot))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool IsIgnoredActorCollider(
            Collider collider,
            Transform ignoredActorRoot)
        {
            if (collider == null)
            {
                return true;
            }

            if (ignoredActorRoot != null &&
                collider.transform.IsChildOf(ignoredActorRoot))
            {
                return true;
            }

            return collider.GetComponentInParent<PlayerAvatar>() != null ||
                   collider.GetComponentInParent<PactCreature>() != null ||
                   collider.GetComponentInParent<CapturedPactActor>() != null ||
                   collider.GetComponentInParent<HumanCompanion>() != null ||
                   collider.GetComponentInParent<Enemy>() != null ||
                   collider.GetComponentInParent<DungeonBoss>() != null;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public readonly struct SkylanceTerrainPieceDefinition
    {
        public SkylanceTerrainPieceDefinition(
            string stableId,
            string displayName,
            SkylanceZone zone,
            PrimitiveType visualPrimitive,
            Vector3 position,
            Vector3 scale,
            Vector3 eulerAngles,
            SkylanceMaterialSlot material,
            bool solid,
            bool climbable)
        {
            StableId = stableId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Zone = zone;
            VisualPrimitive = visualPrimitive;
            Position = position;
            Scale = scale;
            EulerAngles = eulerAngles;
            Material = material;
            Solid = solid;
            Climbable = climbable;
        }

        public string StableId { get; }
        public string DisplayName { get; }
        public SkylanceZone Zone { get; }
        public PrimitiveType VisualPrimitive { get; }
        public Vector3 Position { get; }
        public Vector3 Scale { get; }
        public Vector3 EulerAngles { get; }
        public SkylanceMaterialSlot Material { get; }
        public bool Solid { get; }
        public bool Climbable { get; }
    }

    public readonly struct SkylanceIslandDefinition
    {
        public SkylanceIslandDefinition(
            string stableId,
            string displayName,
            string districtId,
            Vector3 surfaceCenter,
            Vector2 footprint,
            float depth)
        {
            StableId = stableId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            DistrictId = districtId ?? string.Empty;
            SurfaceCenter = surfaceCenter;
            Footprint = footprint;
            Depth = Mathf.Max(1f, depth);
        }

        public string StableId { get; }
        public string DisplayName { get; }
        public string DistrictId { get; }
        public Vector3 SurfaceCenter { get; }
        public Vector2 Footprint { get; }
        public float Depth { get; }
    }

    public readonly struct SkylanceBridgeDefinition
    {
        public SkylanceBridgeDefinition(
            string stableId,
            string fromIslandId,
            string toIslandId,
            float width,
            SkylanceMaterialSlot material)
        {
            StableId = stableId ?? string.Empty;
            FromIslandId = fromIslandId ?? string.Empty;
            ToIslandId = toIslandId ?? string.Empty;
            Width = Mathf.Max(1f, width);
            Material = material;
        }

        public string StableId { get; }
        public string FromIslandId { get; }
        public string ToIslandId { get; }
        public float Width { get; }
        public SkylanceMaterialSlot Material { get; }
    }

    public readonly struct SkylanceDistrictDefinition
    {
        public SkylanceDistrictDefinition(
            string stableId,
            string displayName,
            string islandId,
            int elevationTier,
            Vector3 plazaPosition)
        {
            StableId = stableId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            IslandId = islandId ?? string.Empty;
            ElevationTier = Mathf.Max(0, elevationTier);
            PlazaPosition = plazaPosition;
        }

        public string StableId { get; }
        public string DisplayName { get; }
        public string IslandId { get; }
        public int ElevationTier { get; }
        public Vector3 PlazaPosition { get; }
    }

    public readonly struct SkylanceContentSocketDefinition
    {
        public SkylanceContentSocketDefinition(
            string stableId,
            string displayName,
            SkylanceSocketKind kind,
            string districtId,
            Vector3 position,
            Vector3 forward)
        {
            StableId = stableId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Kind = kind;
            DistrictId = districtId ?? string.Empty;
            Position = position;
            Forward = forward.sqrMagnitude > 0.001f
                ? forward.normalized
                : Vector3.forward;
        }

        public string StableId { get; }
        public string DisplayName { get; }
        public SkylanceSocketKind Kind { get; }
        public string DistrictId { get; }
        public Vector3 Position { get; }
        public Vector3 Forward { get; }
    }

    public readonly struct SkylanceEncounterSocketDefinition
    {
        public SkylanceEncounterSocketDefinition(
            string stableId,
            string displayName,
            string encounterCatalogId,
            string persistenceId,
            Vector3 position,
            bool spawnAtBuild)
        {
            StableId = stableId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            EncounterCatalogId = encounterCatalogId ?? string.Empty;
            PersistenceId = persistenceId ?? string.Empty;
            Position = position;
            SpawnAtBuild = spawnAtBuild;
        }

        public string StableId { get; }
        public string DisplayName { get; }
        public string EncounterCatalogId { get; }
        public string PersistenceId { get; }
        public Vector3 Position { get; }
        public bool SpawnAtBuild { get; }
    }

    public readonly struct SkylanceBudgetEstimate
    {
        public SkylanceBudgetEstimate(
            int renderers,
            int colliders,
            int lights,
            int updateDrivers,
            int sharedMaterials)
        {
            Renderers = renderers;
            Colliders = colliders;
            Lights = lights;
            UpdateDrivers = updateDrivers;
            SharedMaterials = sharedMaterials;
        }

        public int Renderers { get; }
        public int Colliders { get; }
        public int Lights { get; }
        public int UpdateDrivers { get; }
        public int SharedMaterials { get; }
    }

    /// <summary>
    /// Additive mountain and aerial-city vertical slice. Nothing in this file auto-runs:
    /// integration must call Build after the core world exists and before save restoration.
    /// </summary>
    public static class TetherfallSkylanceWorld
    {
        public const string RootName = "Skylance World · Mount Vael and Aerie Kingdom";
        public const string MountainRegionId = "region.skylance.mount_vael";
        public const string MountainRegionName = "Mount Vael Skylance";
        public const string KingdomRegionId = "region.skylance.aerie_kingdom";
        public const string KingdomRegionName = "The Aerie Kingdom";
        public const string GroundRouteId = "route.skylance.alpine.switchbacks";
        public const string AscentRouteId = "route.skylance.ascent.sunhook";
        public const string CircuitRouteId = "route.skylance.aerie.crown_circuit";
        public const string DescentRouteId = "route.skylance.descent.lee_return";

        public const float HighAltitudeFloor = 170f;
        public const float KingdomBoundaryAltitude = 132f;
        public const float MountainDetailDistance = 245f;
        public const float KingdomDetailDistance = 270f;
        public const float KingdomDetailMinimumAltitude = 96f;
        public const float DirectorRefreshSeconds = 0.2f;
        public const int RingSegmentCount = 8;

        public const int MaximumRenderers = 360;
        public const int MaximumColliders = 96;
        public const int MaximumLights = 0;
        public const int MaximumUpdateDrivers = 24;
        public const int MaximumSharedMaterials = 12;
        public const int MaximumActiveKingdomDetailRenderers = 128;

        public static readonly Vector3 MountainOrigin = new Vector3(480f, 0f, 420f);
        public static readonly Vector3 MountainEntryPosition = new Vector3(492f, 1.2f, 330f);
        public static readonly Vector3 MountainRespawnPosition = new Vector3(492f, 2.2f, 342f);
        public static readonly Vector3 MountainSummitPosition = new Vector3(510f, 54.2f, 493f);
        public static readonly Vector3 KingdomArrivalPosition = new Vector3(500f, 177.2f, 470f);
        public static readonly Vector3 KingdomCenter = new Vector3(490f, 196f, 485f);

        private static readonly Rect MountainBounds = new Rect(390f, 315f, 190f, 205f);
        private static readonly Rect KingdomBounds = new Rect(405f, 405f, 175f, 175f);
        private static readonly Material[] CachedMaterials = new Material[8];
        private static readonly Dictionary<string, GameObject> PrefabCache =
            new Dictionary<string, GameObject>(StringComparer.Ordinal);

        private static TetherfallGame materialOwner;
        private static SkylanceWorldRuntime lastRuntime;

        private static readonly SkylanceTerrainPieceDefinition[] TerrainPieces =
        {
            Terrain(
                "terrain.skylance.alpine.rimecoil_connector",
                "Rimecoil to Skylance ice road",
                PrimitiveType.Cube,
                new Vector3(496f, 0.2f, 321f),
                new Vector3(14f, 0.8f, 30f),
                new Vector3(0f, -8f, 0f),
                SkylanceMaterialSlot.DarkRock,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.southwest_foot",
                "Southwest mountain foot",
                PrimitiveType.Cube,
                new Vector3(443f, 3.5f, 360f),
                new Vector3(62f, 7f, 58f),
                new Vector3(0f, 7f, 0f),
                SkylanceMaterialSlot.DarkRock,
                true,
                true),
            Terrain(
                "terrain.skylance.alpine.southeast_foot",
                "Southeast mountain foot",
                PrimitiveType.Cube,
                new Vector3(522f, 5.5f, 366f),
                new Vector3(72f, 11f, 66f),
                new Vector3(0f, -9f, 0f),
                SkylanceMaterialSlot.WeatheredStone,
                true,
                true),
            Terrain(
                "terrain.skylance.alpine.west_massif",
                "West avalanche massif",
                PrimitiveType.Cube,
                new Vector3(425f, 13f, 421f),
                new Vector3(48f, 26f, 92f),
                new Vector3(0f, -11f, 0f),
                SkylanceMaterialSlot.DarkRock,
                true,
                true),
            Terrain(
                "terrain.skylance.alpine.east_massif",
                "East blue-ice massif",
                PrimitiveType.Cube,
                new Vector3(535f, 18f, 427f),
                new Vector3(58f, 36f, 102f),
                new Vector3(0f, 8f, 0f),
                SkylanceMaterialSlot.BlueIce,
                true,
                true),
            Terrain(
                "terrain.skylance.alpine.north_crown",
                "North crown shelf",
                PrimitiveType.Cube,
                new Vector3(482f, 28f, 481f),
                new Vector3(106f, 56f, 62f),
                new Vector3(0f, 3f, 0f),
                SkylanceMaterialSlot.WeatheredStone,
                true,
                true),
            Terrain(
                "terrain.skylance.alpine.ravine_west_wall",
                "West ravine climb face",
                PrimitiveType.Cube,
                new Vector3(462f, 18f, 410f),
                new Vector3(13f, 36f, 80f),
                new Vector3(0f, -13f, -4f),
                SkylanceMaterialSlot.BlueIce,
                true,
                true),
            Terrain(
                "terrain.skylance.alpine.ravine_east_wall",
                "East ravine climb face",
                PrimitiveType.Cube,
                new Vector3(498f, 21f, 416f),
                new Vector3(14f, 42f, 82f),
                new Vector3(0f, 11f, 5f),
                SkylanceMaterialSlot.DarkRock,
                true,
                true),
            Terrain(
                "terrain.skylance.alpine.switchback_lower_west",
                "Lower west switchback",
                PrimitiveType.Cube,
                new Vector3(474f, 3.1f, 343f),
                new Vector3(46f, 1f, 9f),
                new Vector3(0f, 6f, -7f),
                SkylanceMaterialSlot.Snow,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.switchback_lower_east",
                "Lower east switchback",
                PrimitiveType.Cube,
                new Vector3(503f, 6.8f, 361f),
                new Vector3(43f, 1f, 9f),
                new Vector3(0f, -10f, 8f),
                SkylanceMaterialSlot.Snow,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.switchback_middle_west",
                "Middle west switchback",
                PrimitiveType.Cube,
                new Vector3(472f, 11.2f, 384f),
                new Vector3(44f, 1f, 9f),
                new Vector3(0f, 9f, -9f),
                SkylanceMaterialSlot.BlueIce,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.switchback_middle_east",
                "Middle east switchback",
                PrimitiveType.Cube,
                new Vector3(512f, 16.5f, 401f),
                new Vector3(45f, 1f, 9f),
                new Vector3(0f, -8f, 9f),
                SkylanceMaterialSlot.Snow,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.switchback_high_west",
                "High west switchback",
                PrimitiveType.Cube,
                new Vector3(470f, 23.1f, 432f),
                new Vector3(48f, 1f, 9f),
                new Vector3(0f, 10f, -10f),
                SkylanceMaterialSlot.BlueIce,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.switchback_high_east",
                "High east switchback",
                PrimitiveType.Cube,
                new Vector3(513f, 30.3f, 449f),
                new Vector3(46f, 1f, 9f),
                new Vector3(0f, -7f, 10f),
                SkylanceMaterialSlot.Snow,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.crown_switchback",
                "Crown switchback",
                PrimitiveType.Cube,
                new Vector3(489f, 39.2f, 472f),
                new Vector3(48f, 1.1f, 10f),
                new Vector3(0f, 8f, -11f),
                SkylanceMaterialSlot.BlueIce,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.lower_village_terrace",
                "Nettlehook cliff village terrace",
                PrimitiveType.Cube,
                new Vector3(425f, 17.5f, 405f),
                new Vector3(34f, 2f, 27f),
                new Vector3(0f, -8f, 0f),
                SkylanceMaterialSlot.WeatheredStone,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.east_village_terrace",
                "Rimebell cliff village terrace",
                PrimitiveType.Cube,
                new Vector3(541f, 27.5f, 433f),
                new Vector3(38f, 2f, 30f),
                new Vector3(0f, 9f, 0f),
                SkylanceMaterialSlot.WeatheredStone,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.upper_village_terrace",
                "Sunhook cliff village terrace",
                PrimitiveType.Cube,
                new Vector3(468f, 40.5f, 472f),
                new Vector3(42f, 2f, 28f),
                new Vector3(0f, -4f, 0f),
                SkylanceMaterialSlot.WeatheredStone,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.lower_ravine_bridge",
                "Lower avalanche ravine bridge",
                PrimitiveType.Cube,
                new Vector3(480f, 13.8f, 401f),
                new Vector3(35f, 0.9f, 6f),
                new Vector3(0f, 18f, 0f),
                SkylanceMaterialSlot.Timber,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.upper_ravine_bridge",
                "Upper avalanche ravine bridge",
                PrimitiveType.Cube,
                new Vector3(489f, 29.8f, 449f),
                new Vector3(39f, 0.9f, 6f),
                new Vector3(0f, -16f, 0f),
                SkylanceMaterialSlot.Copper,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.summit_shelf",
                "Sunhook summit shelf",
                PrimitiveType.Cube,
                new Vector3(510f, 51f, 493f),
                new Vector3(43f, 6f, 35f),
                new Vector3(0f, 4f, 0f),
                SkylanceMaterialSlot.DarkRock,
                true,
                true),
            Terrain(
                "terrain.skylance.alpine.ascent_apron",
                "Sunhook ascent apron",
                PrimitiveType.Cube,
                new Vector3(524f, 54f, 497f),
                new Vector3(18f, 0.8f, 15f),
                new Vector3(0f, -12f, 0f),
                SkylanceMaterialSlot.Cloudglass,
                true,
                false),
            Terrain(
                "terrain.skylance.alpine.avalanche_shard_west",
                "West avalanche shard",
                PrimitiveType.Cube,
                new Vector3(447f, 8f, 387f),
                new Vector3(4f, 16f, 9f),
                new Vector3(19f, -27f, 22f),
                SkylanceMaterialSlot.BlueIce,
                false,
                false),
            Terrain(
                "terrain.skylance.alpine.avalanche_shard_center",
                "Center avalanche shard",
                PrimitiveType.Cube,
                new Vector3(481f, 12f, 394f),
                new Vector3(5f, 23f, 8f),
                new Vector3(-16f, 12f, -19f),
                SkylanceMaterialSlot.Snow,
                false,
                false),
            Terrain(
                "terrain.skylance.alpine.avalanche_shard_east",
                "East avalanche shard",
                PrimitiveType.Cube,
                new Vector3(518f, 15f, 408f),
                new Vector3(4f, 27f, 10f),
                new Vector3(21f, 32f, 17f),
                SkylanceMaterialSlot.BlueIce,
                false,
                false),
            Terrain(
                "terrain.skylance.alpine.ravine_scree_lower",
                "Lower ravine scree",
                PrimitiveType.Sphere,
                new Vector3(480f, 0.8f, 374f),
                new Vector3(19f, 2f, 24f),
                new Vector3(0f, 15f, 0f),
                SkylanceMaterialSlot.DarkRock,
                false,
                false),
            Terrain(
                "terrain.skylance.alpine.ravine_scree_upper",
                "Upper ravine scree",
                PrimitiveType.Sphere,
                new Vector3(484f, 5f, 430f),
                new Vector3(16f, 2.5f, 21f),
                new Vector3(0f, -11f, 0f),
                SkylanceMaterialSlot.WeatheredStone,
                false,
                false)
        };

        private static readonly SkylanceIslandDefinition[] Islands =
        {
            Island(
                "island.skylance.aerie.windward_gate",
                "Windward Gate",
                "district.skylance.aerie.windward_docks",
                new Vector3(500f, 176f, 470f),
                new Vector2(38f, 34f),
                12f),
            Island(
                "island.skylance.aerie.kite_market",
                "Kite Market",
                "district.skylance.aerie.kite_market",
                new Vector3(452f, 184f, 432f),
                new Vector2(48f, 43f),
                15f),
            Island(
                "island.skylance.aerie.cloud_archive",
                "Cloud Archive",
                "district.skylance.aerie.cloud_archive",
                new Vector3(516f, 190f, 425f),
                new Vector2(42f, 38f),
                14f),
            Island(
                "island.skylance.aerie.copper_rookery",
                "Copper Rookery",
                "district.skylance.aerie.copper_rookery",
                new Vector3(544f, 201f, 478f),
                new Vector2(43f, 40f),
                17f),
            Island(
                "island.skylance.aerie.lee_gardens",
                "Lee Gardens",
                "district.skylance.aerie.lee_gardens",
                new Vector3(432f, 196f, 488f),
                new Vector2(38f, 36f),
                13f),
            Island(
                "island.skylance.aerie.threadcrown",
                "Threadcrown",
                "district.skylance.aerie.threadcrown",
                new Vector3(490f, 214f, 523f),
                new Vector2(54f, 49f),
                21f),
            Island(
                "island.skylance.aerie.storm_throne",
                "Storm Throne",
                "district.skylance.aerie.storm_throne",
                new Vector3(544f, 222f, 552f),
                new Vector2(47f, 43f),
                19f)
        };

        private static readonly SkylanceBridgeDefinition[] Bridges =
        {
            Bridge(
                "bridge.skylance.aerie.windward_to_market",
                "island.skylance.aerie.windward_gate",
                "island.skylance.aerie.kite_market",
                5.8f,
                SkylanceMaterialSlot.Copper),
            Bridge(
                "bridge.skylance.aerie.windward_to_archive",
                "island.skylance.aerie.windward_gate",
                "island.skylance.aerie.cloud_archive",
                5.4f,
                SkylanceMaterialSlot.Cloudglass),
            Bridge(
                "bridge.skylance.aerie.market_to_gardens",
                "island.skylance.aerie.kite_market",
                "island.skylance.aerie.lee_gardens",
                5.2f,
                SkylanceMaterialSlot.Timber),
            Bridge(
                "bridge.skylance.aerie.archive_to_rookery",
                "island.skylance.aerie.cloud_archive",
                "island.skylance.aerie.copper_rookery",
                5.1f,
                SkylanceMaterialSlot.Copper),
            Bridge(
                "bridge.skylance.aerie.gardens_to_threadcrown",
                "island.skylance.aerie.lee_gardens",
                "island.skylance.aerie.threadcrown",
                5.6f,
                SkylanceMaterialSlot.Cloudglass),
            Bridge(
                "bridge.skylance.aerie.rookery_to_threadcrown",
                "island.skylance.aerie.copper_rookery",
                "island.skylance.aerie.threadcrown",
                5.6f,
                SkylanceMaterialSlot.Copper),
            Bridge(
                "bridge.skylance.aerie.threadcrown_to_storm_throne",
                "island.skylance.aerie.threadcrown",
                "island.skylance.aerie.storm_throne",
                6.2f,
                SkylanceMaterialSlot.WeatheredStone)
        };

        private static readonly SkylanceDistrictDefinition[] Districts =
        {
            District(
                "district.skylance.aerie.windward_docks",
                "Windward Docks",
                "island.skylance.aerie.windward_gate",
                0,
                new Vector3(500f, 177f, 470f)),
            District(
                "district.skylance.aerie.kite_market",
                "Kite Market",
                "island.skylance.aerie.kite_market",
                1,
                new Vector3(452f, 185f, 432f)),
            District(
                "district.skylance.aerie.cloud_archive",
                "Cloud Archive",
                "island.skylance.aerie.cloud_archive",
                2,
                new Vector3(516f, 191f, 425f)),
            District(
                "district.skylance.aerie.copper_rookery",
                "Copper Rookery",
                "island.skylance.aerie.copper_rookery",
                3,
                new Vector3(544f, 202f, 478f)),
            District(
                "district.skylance.aerie.lee_gardens",
                "Lee Gardens",
                "island.skylance.aerie.lee_gardens",
                2,
                new Vector3(432f, 197f, 488f)),
            District(
                "district.skylance.aerie.threadcrown",
                "Threadcrown",
                "island.skylance.aerie.threadcrown",
                4,
                new Vector3(490f, 215f, 523f)),
            District(
                "district.skylance.aerie.storm_throne",
                "Storm Throne",
                "island.skylance.aerie.storm_throne",
                5,
                new Vector3(544f, 223f, 552f))
        };

        private static readonly SkylanceContentSocketDefinition[] ContentSockets =
        {
            Socket(
                "socket.skylance.vendor.windwright_exchange",
                "Windwright Exchange vendor",
                SkylanceSocketKind.Vendor,
                "district.skylance.aerie.kite_market",
                new Vector3(445f, 185.2f, 429f),
                Vector3.forward),
            Socket(
                "socket.skylance.vendor.cloudglass_cooperative",
                "Cloudglass Cooperative vendor",
                SkylanceSocketKind.Vendor,
                "district.skylance.aerie.kite_market",
                new Vector3(460f, 185.2f, 438f),
                Vector3.left),
            Socket(
                "socket.skylance.quest.archive_weather_ledger",
                "Weather Ledger quest",
                SkylanceSocketKind.Quest,
                "district.skylance.aerie.cloud_archive",
                new Vector3(516f, 191.2f, 418f),
                Vector3.back),
            Socket(
                "socket.skylance.quest.lee_garden_reseeding",
                "Lee Garden reseeding quest",
                SkylanceSocketKind.Quest,
                "district.skylance.aerie.lee_gardens",
                new Vector3(428f, 197.2f, 489f),
                Vector3.right),
            Socket(
                "socket.skylance.quest.rookery_accord",
                "Copper Rookery Accord quest",
                SkylanceSocketKind.Quest,
                "district.skylance.aerie.copper_rookery",
                new Vector3(538f, 202.2f, 477f),
                Vector3.right),
            Socket(
                "socket.skylance.boss.storm_throne",
                "Storm Throne boss",
                SkylanceSocketKind.Boss,
                "district.skylance.aerie.storm_throne",
                new Vector3(544f, 223.2f, 552f),
                Vector3.back),
            Socket(
                "socket.skylance.landing.windward_gate",
                "Windward Gate landing",
                SkylanceSocketKind.Landing,
                "district.skylance.aerie.windward_docks",
                KingdomArrivalPosition,
                Vector3.forward),
            Socket(
                "socket.skylance.respawn.sunhook_summit",
                "Sunhook summit recovery",
                SkylanceSocketKind.Respawn,
                "district.skylance.alpine.sunhook",
                MountainSummitPosition,
                Vector3.back)
        };

        private static readonly SkylanceEncounterSocketDefinition[] EncounterSockets =
        {
            Encounter(
                "socket.skylance.encounter.copperquill.rime_nest",
                "Rime Nest Copperquills",
                "encounter.wild.alderwake.copperquill_nesters",
                "encounter.skylance.alpine.rime_nest",
                new Vector3(425f, 19.1f, 405f),
                true),
            Encounter(
                "socket.skylance.encounter.prismtail.glass_ledge",
                "Glass Ledge Prismtails",
                "encounter.wild.cinderwold.prismtail_kitelizards",
                "encounter.skylance.alpine.glass_ledge",
                new Vector3(541f, 29.1f, 433f),
                true),
            Encounter(
                "socket.skylance.encounter.gloamreed.storm_hollow",
                "Storm Hollow Gloamreeds",
                "encounter.wild.morrowfen.gloamreed_lanternbacks",
                "encounter.skylance.alpine.storm_hollow",
                new Vector3(468f, 42.1f, 472f),
                true),
            Encounter(
                "socket.skylance.encounter.copperquill.crown_rookery",
                "Crown Rookery Copperquills",
                "encounter.wild.alderwake.copperquill_nesters",
                "encounter.skylance.aerie.crown_rookery",
                new Vector3(551f, 202.1f, 483f),
                false)
        };

        private static readonly SkylanceFlightRouteDefinition[] Routes =
            CreateRoutes();

        public static IReadOnlyList<SkylanceTerrainPieceDefinition> Terrain => TerrainPieces;
        public static IReadOnlyList<SkylanceIslandDefinition> FloatingIslands => Islands;
        public static IReadOnlyList<SkylanceBridgeDefinition> IslandBridges => Bridges;
        public static IReadOnlyList<SkylanceDistrictDefinition> CityDistricts => Districts;
        public static IReadOnlyList<SkylanceContentSocketDefinition> ServiceSockets => ContentSockets;
        public static IReadOnlyList<SkylanceEncounterSocketDefinition> FlyingPactEncounterSockets =>
            EncounterSockets;
        public static IReadOnlyList<SkylanceFlightRouteDefinition> FlightRoutes => Routes;
        public static bool IsBuilt => lastRuntime != null;

        public static void Build(TetherfallGame game)
        {
            if (game == null)
            {
                return;
            }

            var existing = FindDirectChild(game.transform, RootName);
            if (existing != null)
            {
                lastRuntime = existing.GetComponent<SkylanceWorldRuntime>();
                return;
            }

            PrepareCaches(game);
            var root = new GameObject(RootName);
            root.transform.SetParent(game.transform, false);
            var semanticRoot = root.AddComponent<SkylanceSemanticId>();
            semanticRoot.Initialize(KingdomRegionId);

            var collisionRoot = CreateGroup(root.transform, "Collision · always loaded");
            var artRoot = CreateGroup(root.transform, "Art · separated from collision");
            var mountainShellArt = CreateGroup(artRoot, "Alpine shell art");
            var mountainDetailArt = CreateGroup(artRoot, "Alpine proximity detail");
            var kingdomShellArt = CreateGroup(artRoot, "Aerie silhouette shell");
            var kingdomDetailArt = CreateGroup(artRoot, "Aerie altitude detail");
            var distantArt = CreateGroup(artRoot, "Distant sky silhouettes");
            var gameplayRoot = CreateGroup(root.transform, "Gameplay sockets");
            var encounterRoot = CreateGroup(gameplayRoot, "Capturable flying Pact encounters");
            var accessRoot = CreateGroup(gameplayRoot, "Access and fail-safe volumes");

            BuildTerrain(game, collisionRoot, mountainShellArt);
            BuildFloatingKingdom(game, collisionRoot, kingdomShellArt);
            BuildCliffVillages(game, mountainDetailArt);
            BuildEncounterSockets(game, encounterRoot, mountainDetailArt, kingdomDetailArt);
            BuildDistrictArt(game, kingdomDetailArt);
            BuildContentSockets(game, kingdomDetailArt);
            BuildDistantSilhouettes(game, distantArt);

            var runtime = root.AddComponent<SkylanceWorldRuntime>();
            BuildAccessRoute(game, runtime, accessRoot, mountainDetailArt, kingdomDetailArt);
            BuildFailSafeVolumes(game, runtime, accessRoot);
            runtime.Initialize(
                game,
                mountainDetailArt.gameObject,
                kingdomDetailArt.gameObject);
            lastRuntime = runtime;
        }

        public static bool SetAuthoredAscentUnlocked(
            TetherfallGame game,
            bool unlocked)
        {
            if (game == null)
            {
                return false;
            }

            var root = FindDirectChild(game.transform, RootName);
            var runtime = root != null
                ? root.GetComponent<SkylanceWorldRuntime>()
                : null;
            if (runtime == null)
            {
                return false;
            }

            runtime.SetAuthoredAscentUnlocked(unlocked);
            lastRuntime = runtime;
            return true;
        }

        public static bool TryGetRoute(
            string stableId,
            out SkylanceFlightRouteDefinition route)
        {
            if (!string.IsNullOrEmpty(stableId))
            {
                for (var index = 0; index < Routes.Length; index++)
                {
                    if (string.Equals(
                            Routes[index].StableId,
                            stableId,
                            StringComparison.Ordinal))
                    {
                        route = Routes[index];
                        return true;
                    }
                }
            }

            route = null;
            return false;
        }

        public static float GetRouteLength(SkylanceFlightRouteDefinition route)
        {
            if (route == null || route.NodeCount < 2)
            {
                return 0f;
            }

            var length = 0f;
            for (var index = 1; index < route.NodeCount; index++)
            {
                route.TryGetNode(index - 1, out var previous);
                route.TryGetNode(index, out var current);
                length += Vector3.Distance(previous.Position, current.Position);
            }

            return length;
        }

        public static bool TrySampleRoute(
            SkylanceFlightRouteDefinition route,
            float normalizedProgress,
            out SkylanceRouteSample sample)
        {
            sample = default;
            if (route == null ||
                route.NodeCount < 2 ||
                float.IsNaN(normalizedProgress) ||
                float.IsInfinity(normalizedProgress))
            {
                return false;
            }

            var totalLength = GetRouteLength(route);
            if (totalLength <= 0.001f)
            {
                return false;
            }

            var targetDistance = Mathf.Clamp01(normalizedProgress) * totalLength;
            var traversed = 0f;
            for (var index = 1; index < route.NodeCount; index++)
            {
                route.TryGetNode(index - 1, out var previous);
                route.TryGetNode(index, out var current);
                var delta = current.Position - previous.Position;
                var segmentLength = delta.magnitude;
                if (segmentLength <= 0.001f)
                {
                    continue;
                }

                var reachesTarget =
                    targetDistance <= traversed + segmentLength ||
                    index == route.NodeCount - 1;
                if (!reachesTarget)
                {
                    traversed += segmentLength;
                    continue;
                }

                var segmentProgress = Mathf.Clamp01(
                    (targetDistance - traversed) / segmentLength);
                sample = new SkylanceRouteSample(
                    Vector3.LerpUnclamped(
                        previous.Position,
                        current.Position,
                        segmentProgress),
                    delta / segmentLength,
                    index - 1,
                    segmentProgress);
                return true;
            }

            return false;
        }

        public static bool TryResolveRegion(
            Vector3 worldPosition,
            out string regionId,
            out string displayName)
        {
            if (!IsFinite(worldPosition))
            {
                regionId = string.Empty;
                displayName = string.Empty;
                return false;
            }

            if (worldPosition.y >= KingdomBoundaryAltitude &&
                ContainsHorizontal(KingdomBounds, worldPosition, 24f))
            {
                regionId = KingdomRegionId;
                displayName = KingdomRegionName;
                return true;
            }

            if (ContainsHorizontal(MountainBounds, worldPosition, 12f))
            {
                regionId = MountainRegionId;
                displayName = MountainRegionName;
                return true;
            }

            regionId = string.Empty;
            displayName = string.Empty;
            return false;
        }

        public static SkylanceIslandDefinition GetIsland(string stableId)
        {
            if (string.IsNullOrEmpty(stableId))
            {
                return default;
            }

            for (var index = 0; index < Islands.Length; index++)
            {
                if (string.Equals(
                        Islands[index].StableId,
                        stableId,
                        StringComparison.Ordinal))
                {
                    return Islands[index];
                }
            }

            return default;
        }

        public static bool IsSemanticStableId(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Length > 160 ||
                value[0] < 'a' ||
                value[0] > 'z')
            {
                return false;
            }

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                var valid =
                    character >= 'a' && character <= 'z' ||
                    character >= '0' && character <= '9' ||
                    character == '.' ||
                    character == '_';
                if (!valid)
                {
                    return false;
                }
            }

            return value.IndexOf("..", StringComparison.Ordinal) < 0 &&
                   value[value.Length - 1] != '.';
        }

        public static SkylanceBudgetEstimate EstimateBudget()
        {
            var solidTerrain = 0;
            for (var index = 0; index < TerrainPieces.Length; index++)
            {
                if (TerrainPieces[index].Solid)
                {
                    solidTerrain++;
                }
            }

            var routeRingCount = 0;
            for (var routeIndex = 0; routeIndex < Routes.Length; routeIndex++)
            {
                var route = Routes[routeIndex];
                for (var nodeIndex = 0; nodeIndex < route.NodeCount; nodeIndex++)
                {
                    route.TryGetNode(nodeIndex, out var node);
                    if (node.Kind == SkylanceRouteNodeKind.LiftRing)
                    {
                        routeRingCount++;
                    }
                }
            }

            var spawnedEncounterCount = 0;
            for (var index = 0; index < EncounterSockets.Length; index++)
            {
                if (EncounterSockets[index].SpawnAtBuild)
                {
                    spawnedEncounterCount++;
                }
            }

            var rendererEstimate =
                TerrainPieces.Length +
                Islands.Length * 2 +
                Bridges.Length * 3 +
                Districts.Length * 5 +
                3 * 7 +
                EncounterSockets.Length * 6 +
                routeRingCount * RingSegmentCount +
                ContentSockets.Length * 3 +
                16 +
                spawnedEncounterCount * 12;
            var colliderEstimate =
                solidTerrain +
                Islands.Length +
                Bridges.Length +
                routeRingCount +
                2 +
                8;
            var updateEstimate =
                1 +
                spawnedEncounterCount * 3 +
                2;
            return new SkylanceBudgetEstimate(
                rendererEstimate,
                colliderEstimate,
                0,
                updateEstimate,
                CachedMaterials.Length);
        }

        private static SkylanceFlightRouteDefinition[] CreateRoutes()
        {
            return new[]
            {
                new SkylanceFlightRouteDefinition(
                    GroundRouteId,
                    "Mount Vael switchback road",
                    SkylanceRouteKind.GroundedSwitchback,
                    SkylanceRouteAccess.Open,
                    false,
                    new[]
                    {
                        Node(
                            "route_node.skylance.alpine.entry",
                            MountainEntryPosition,
                            5f,
                            0f,
                            SkylanceRouteNodeKind.Waypoint),
                        Node(
                            "route_node.skylance.alpine.lower_turn",
                            new Vector3(503f, 7.5f, 361f),
                            5f,
                            0f,
                            SkylanceRouteNodeKind.Waypoint),
                        Node(
                            "route_node.skylance.alpine.middle_turn",
                            new Vector3(472f, 12f, 384f),
                            5f,
                            0f,
                            SkylanceRouteNodeKind.Waypoint),
                        Node(
                            "route_node.skylance.alpine.ravine_crossing",
                            new Vector3(489f, 30.5f, 449f),
                            5f,
                            0f,
                            SkylanceRouteNodeKind.Waypoint),
                        Node(
                            "route_node.skylance.alpine.sunhook_summit",
                            MountainSummitPosition,
                            6f,
                            0f,
                            SkylanceRouteNodeKind.LandingPerch)
                    }),
                new SkylanceFlightRouteDefinition(
                    AscentRouteId,
                    "Sunhook authored ascent",
                    SkylanceRouteKind.GatedAscent,
                    SkylanceRouteAccess.FlightPactOrAuthoredAscent,
                    false,
                    new[]
                    {
                        Node(
                            "route_node.skylance.ascent.sunhook_launch",
                            new Vector3(524f, 55f, 497f),
                            4.5f,
                            24f,
                            SkylanceRouteNodeKind.Launch),
                        Node(
                            "route_node.skylance.ascent.east_ring",
                            new Vector3(548f, 78f, 476f),
                            7f,
                            24f,
                            SkylanceRouteNodeKind.LiftRing),
                        Node(
                            "route_node.skylance.ascent.highwake_ring",
                            new Vector3(570f, 104f, 458f),
                            7f,
                            24f,
                            SkylanceRouteNodeKind.LiftRing),
                        Node(
                            "route_node.skylance.ascent.cloudbreak_ring",
                            new Vector3(582f, 131f, 484f),
                            7f,
                            23f,
                            SkylanceRouteNodeKind.LiftRing),
                        Node(
                            "route_node.skylance.ascent.gate_ring",
                            new Vector3(569f, 154f, 510f),
                            7f,
                            22f,
                            SkylanceRouteNodeKind.LiftRing),
                        Node(
                            "route_node.skylance.ascent.windward_approach",
                            new Vector3(544f, 169f, 493f),
                            7f,
                            20f,
                            SkylanceRouteNodeKind.LiftRing),
                        Node(
                            "route_node.skylance.ascent.windward_landing",
                            KingdomArrivalPosition,
                            8f,
                            0f,
                            SkylanceRouteNodeKind.LandingPerch)
                    }),
                new SkylanceFlightRouteDefinition(
                    CircuitRouteId,
                    "Aerie crown circuit",
                    SkylanceRouteKind.AerialCircuit,
                    SkylanceRouteAccess.FlightPactOrAuthoredAscent,
                    true,
                    new[]
                    {
                        Node(
                            "route_node.skylance.circuit.windward",
                            new Vector3(500f, 185f, 470f),
                            6f,
                            13f,
                            SkylanceRouteNodeKind.LiftRing),
                        Node(
                            "route_node.skylance.circuit.archive",
                            new Vector3(522f, 205f, 416f),
                            6f,
                            13f,
                            SkylanceRouteNodeKind.LiftRing),
                        Node(
                            "route_node.skylance.circuit.rookery",
                            new Vector3(559f, 217f, 480f),
                            6f,
                            13f,
                            SkylanceRouteNodeKind.LiftRing),
                        Node(
                            "route_node.skylance.circuit.threadcrown",
                            new Vector3(490f, 232f, 523f),
                            6f,
                            13f,
                            SkylanceRouteNodeKind.LiftRing),
                        Node(
                            "route_node.skylance.circuit.lee_gardens",
                            new Vector3(416f, 210f, 490f),
                            6f,
                            13f,
                            SkylanceRouteNodeKind.LiftRing),
                        Node(
                            "route_node.skylance.circuit.market",
                            new Vector3(440f, 198f, 425f),
                            6f,
                            13f,
                            SkylanceRouteNodeKind.LiftRing),
                        Node(
                            "route_node.skylance.circuit.return",
                            new Vector3(500f, 185f, 470f),
                            6f,
                            13f,
                            SkylanceRouteNodeKind.LiftRing)
                    }),
                new SkylanceFlightRouteDefinition(
                    DescentRouteId,
                    "Lee-side safe descent",
                    SkylanceRouteKind.SafeDescent,
                    SkylanceRouteAccess.AlwaysReturn,
                    false,
                    new[]
                    {
                        Node(
                            "route_node.skylance.descent.windward_perch",
                            new Vector3(488f, 177f, 463f),
                            6f,
                            0f,
                            SkylanceRouteNodeKind.ReturnPerch),
                        Node(
                            "route_node.skylance.descent.lee_cloud",
                            new Vector3(449f, 146f, 452f),
                            7f,
                            0f,
                            SkylanceRouteNodeKind.Waypoint),
                        Node(
                            "route_node.skylance.descent.ravine_air",
                            new Vector3(468f, 99f, 470f),
                            7f,
                            0f,
                            SkylanceRouteNodeKind.Waypoint),
                        Node(
                            "route_node.skylance.descent.sunhook_return",
                            MountainSummitPosition,
                            7f,
                            0f,
                            SkylanceRouteNodeKind.LandingPerch)
                    })
            };
        }

        private static void BuildTerrain(
            TetherfallGame game,
            Transform collisionRoot,
            Transform artRoot)
        {
            for (var index = 0; index < TerrainPieces.Length; index++)
            {
                var piece = TerrainPieces[index];
                if (piece.Solid)
                {
                    CreateCollisionBox(
                        collisionRoot,
                        piece.StableId,
                        piece.DisplayName,
                        piece.Position,
                        piece.Scale,
                        Quaternion.Euler(piece.EulerAngles),
                        piece.Climbable);
                }

                CreateVisualPrimitive(
                    game,
                    artRoot,
                    piece.StableId,
                    piece.DisplayName,
                    piece.VisualPrimitive,
                    piece.Position,
                    piece.Scale,
                    Quaternion.Euler(piece.EulerAngles),
                    GetMaterial(game, piece.Material));
            }

            var title = CreateGroup(artRoot, "Mount Vael title");
            title.position = new Vector3(480f, 58f, 493f);
            WorldVisuals.AddLabel(
                title,
                "MOUNT VAEL SKYLANCE",
                Vector3.up * 2f,
                new Color(0.78f, 0.92f, 1f),
                0.12f);
        }

        private static void BuildFloatingKingdom(
            TetherfallGame game,
            Transform collisionRoot,
            Transform artRoot)
        {
            for (var index = 0; index < Islands.Length; index++)
            {
                var island = Islands[index];
                var collisionScale =
                    new Vector3(island.Footprint.x, island.Depth, island.Footprint.y);
                var collisionCenter =
                    island.SurfaceCenter - Vector3.up * island.Depth * 0.5f;
                CreateCollisionBox(
                    collisionRoot,
                    island.StableId,
                    island.DisplayName,
                    collisionCenter,
                    collisionScale,
                    Quaternion.identity,
                    false);

                CreateVisualPrimitive(
                    game,
                    artRoot,
                    island.StableId + ".upper_mass",
                    island.DisplayName + " upper mass",
                    PrimitiveType.Cylinder,
                    island.SurfaceCenter - Vector3.up * island.Depth * 0.5f,
                    new Vector3(
                        island.Footprint.x * 0.5f,
                        island.Depth * 0.5f,
                        island.Footprint.y * 0.5f),
                    Quaternion.identity,
                    GetMaterial(game, SkylanceMaterialSlot.WeatheredStone));
                CreateVisualPrimitive(
                    game,
                    artRoot,
                    island.StableId + ".cloud_keel",
                    island.DisplayName + " cloud keel",
                    PrimitiveType.Sphere,
                    island.SurfaceCenter - Vector3.up * (island.Depth + 6f),
                    new Vector3(
                        island.Footprint.x * 0.36f,
                        island.Depth * 0.72f,
                        island.Footprint.y * 0.36f),
                    Quaternion.identity,
                    GetMaterial(game, SkylanceMaterialSlot.DarkRock));
            }

            for (var index = 0; index < Bridges.Length; index++)
            {
                BuildBridge(game, collisionRoot, artRoot, Bridges[index]);
            }

            var title = CreateGroup(artRoot, "Aerie Kingdom title");
            title.position = new Vector3(490f, 242f, 523f);
            WorldVisuals.AddLabel(
                title,
                "THE AERIE KINGDOM",
                Vector3.up * 2f,
                new Color(0.92f, 0.94f, 1f),
                0.13f);
        }

        private static void BuildBridge(
            TetherfallGame game,
            Transform collisionRoot,
            Transform artRoot,
            SkylanceBridgeDefinition bridge)
        {
            var from = GetIsland(bridge.FromIslandId);
            var to = GetIsland(bridge.ToIslandId);
            if (string.IsNullOrEmpty(from.StableId) ||
                string.IsNullOrEmpty(to.StableId))
            {
                return;
            }

            var start = from.SurfaceCenter + Vector3.up * 0.45f;
            var end = to.SurfaceCenter + Vector3.up * 0.45f;
            var delta = end - start;
            var length = delta.magnitude;
            if (length <= 0.01f)
            {
                return;
            }

            var midpoint = (start + end) * 0.5f;
            var rotation = Quaternion.LookRotation(delta / length, Vector3.up);
            var scale = new Vector3(bridge.Width, 0.72f, length);
            CreateCollisionBox(
                collisionRoot,
                bridge.StableId,
                bridge.StableId,
                midpoint,
                scale,
                rotation,
                false);
            CreateVisualPrimitive(
                game,
                artRoot,
                bridge.StableId + ".deck",
                bridge.StableId + " deck",
                PrimitiveType.Cube,
                midpoint,
                scale,
                rotation,
                GetMaterial(game, bridge.Material));

            var right = rotation * Vector3.right;
            for (var side = -1; side <= 1; side += 2)
            {
                CreateVisualPrimitive(
                    game,
                    artRoot,
                    bridge.StableId + (side < 0 ? ".rail_left" : ".rail_right"),
                    bridge.StableId + " rail",
                    PrimitiveType.Cube,
                    midpoint + right * side * (bridge.Width * 0.47f) + Vector3.up * 0.7f,
                    new Vector3(0.16f, 1.1f, length),
                    rotation,
                    GetMaterial(game, SkylanceMaterialSlot.Copper));
            }
        }

        private static void BuildCliffVillages(
            TetherfallGame game,
            Transform detailRoot)
        {
            BuildVillage(
                game,
                detailRoot,
                "village.skylance.alpine.nettlehook",
                new Vector3(425f, 18.6f, 405f),
                -8f,
                "Wall_UnevenBrick_Door_Round");
            BuildVillage(
                game,
                detailRoot,
                "village.skylance.alpine.rimebell",
                new Vector3(541f, 28.6f, 433f),
                171f,
                "Wall_Plaster_Door_Round");
            BuildVillage(
                game,
                detailRoot,
                "village.skylance.alpine.sunhook",
                new Vector3(468f, 41.6f, 472f),
                96f,
                "Wall_Plaster_WoodGrid");
        }

        private static void BuildVillage(
            TetherfallGame game,
            Transform parent,
            string stableId,
            Vector3 center,
            float yaw,
            string primaryResource)
        {
            var root = CreateSemanticGroup(parent, stableId, stableId);
            root.position = center;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            CreateAssetOrFallback(
                game,
                root,
                stableId + ".hall",
                primaryResource,
                PrimitiveType.Cube,
                new Vector3(-5f, 1.6f, 0f),
                new Vector3(1.25f, 1.25f, 1.25f),
                Quaternion.identity,
                SkylanceMaterialSlot.WeatheredStone);
            CreateAssetOrFallback(
                game,
                root,
                stableId + ".window_house",
                "Wall_UnevenBrick_Window_Wide_Round",
                PrimitiveType.Cube,
                new Vector3(4.5f, 1.6f, 1.8f),
                new Vector3(1.15f, 1.15f, 1.15f),
                Quaternion.Euler(0f, 180f, 0f),
                SkylanceMaterialSlot.WeatheredStone);
            CreateAssetOrFallback(
                game,
                root,
                stableId + ".bench",
                "Bench",
                PrimitiveType.Cube,
                new Vector3(0f, 0.5f, -5f),
                Vector3.one,
                Quaternion.Euler(0f, 90f, 0f),
                SkylanceMaterialSlot.Timber);
            CreateVisualPrimitiveLocal(
                game,
                root,
                stableId + ".roof_west",
                "Snow roof west",
                PrimitiveType.Cube,
                new Vector3(-5f, 4.2f, 0f),
                new Vector3(7f, 0.45f, 5f),
                Quaternion.Euler(0f, 0f, 10f),
                GetMaterial(game, SkylanceMaterialSlot.BlueIce));
            CreateVisualPrimitiveLocal(
                game,
                root,
                stableId + ".roof_east",
                "Snow roof east",
                PrimitiveType.Cube,
                new Vector3(4.5f, 4.1f, 1.8f),
                new Vector3(7f, 0.45f, 5f),
                Quaternion.Euler(0f, 0f, -10f),
                GetMaterial(game, SkylanceMaterialSlot.Snow));
            CreateVisualPrimitiveLocal(
                game,
                root,
                stableId + ".wind_pole",
                "Village wind pole",
                PrimitiveType.Cylinder,
                new Vector3(0f, 3.5f, 4.8f),
                new Vector3(0.16f, 3.5f, 0.16f),
                Quaternion.identity,
                GetMaterial(game, SkylanceMaterialSlot.Copper));
            CreateVisualPrimitiveLocal(
                game,
                root,
                stableId + ".wind_vane",
                "Village wind vane",
                PrimitiveType.Cube,
                new Vector3(0f, 6.6f, 4.8f),
                new Vector3(3.2f, 0.16f, 0.42f),
                Quaternion.Euler(0f, 24f, 0f),
                GetMaterial(game, SkylanceMaterialSlot.WindGlow));
        }

        private static void BuildEncounterSockets(
            TetherfallGame game,
            Transform encounterRoot,
            Transform mountainDetailRoot,
            Transform kingdomDetailRoot)
        {
            for (var index = 0; index < EncounterSockets.Length; index++)
            {
                var definition = EncounterSockets[index];
                var artParent = definition.SpawnAtBuild
                    ? mountainDetailRoot
                    : kingdomDetailRoot;
                BuildNest(
                    game,
                    artParent,
                    definition.StableId + ".nest",
                    definition.Position);

                if (!definition.SpawnAtBuild)
                {
                    var socketRoot = CreateSemanticGroup(
                        kingdomDetailRoot,
                        definition.StableId,
                        definition.DisplayName);
                    socketRoot.position = definition.Position;
                    var socket = socketRoot.gameObject.AddComponent<SkylanceContentSocket>();
                    socket.Initialize(
                        new SkylanceContentSocketDefinition(
                            definition.StableId,
                            definition.DisplayName,
                            SkylanceSocketKind.Encounter,
                            "district.skylance.aerie.copper_rookery",
                            definition.Position,
                            Vector3.back));
                    continue;
                }

                var camp = EnemyCamp.Create(
                    game,
                    definition.EncounterCatalogId,
                    definition.Position,
                    definition.PersistenceId);
                if (camp != null)
                {
                    camp.gameObject.name = definition.DisplayName;
                    camp.transform.SetParent(encounterRoot, true);
                    var semantic = camp.gameObject.AddComponent<SkylanceSemanticId>();
                    semantic.Initialize(definition.StableId);
                }
            }
        }

        private static void BuildNest(
            TetherfallGame game,
            Transform parent,
            string stableId,
            Vector3 position)
        {
            var root = CreateSemanticGroup(parent, stableId, stableId);
            root.position = position + Vector3.up * 0.2f;
            for (var segment = 0; segment < 6; segment++)
            {
                var angle = segment * 60f;
                var radians = angle * Mathf.Deg2Rad;
                CreateVisualPrimitiveLocal(
                    game,
                    root,
                    stableId + ".rib_" + (segment + 1),
                    "Nest rib",
                    PrimitiveType.Capsule,
                    new Vector3(
                        Mathf.Cos(radians) * 2.2f,
                        0.35f,
                        Mathf.Sin(radians) * 2.2f),
                    new Vector3(0.22f, 1.8f, 0.22f),
                    Quaternion.Euler(72f, -angle, 0f),
                    GetMaterial(
                        game,
                        segment % 2 == 0
                            ? SkylanceMaterialSlot.Timber
                            : SkylanceMaterialSlot.Copper));
            }
        }

        private static void BuildDistrictArt(
            TetherfallGame game,
            Transform detailRoot)
        {
            for (var index = 0; index < Districts.Length; index++)
            {
                var district = Districts[index];
                var root = CreateSemanticGroup(
                    detailRoot,
                    district.StableId,
                    district.DisplayName);
                root.position = district.PlazaPosition;
                var angle = index * 37f;
                CreateAssetOrFallback(
                    game,
                    root,
                    district.StableId + ".facade",
                    index % 2 == 0
                        ? "Wall_Plaster_Window_Wide_Round"
                        : "Wall_UnevenBrick_Window_Wide_Round",
                    PrimitiveType.Cube,
                    new Vector3(-5f, 1.7f, 1f),
                    new Vector3(1.2f, 1.2f, 1.2f),
                    Quaternion.Euler(0f, angle, 0f),
                    SkylanceMaterialSlot.WeatheredStone);
                CreateAssetOrFallback(
                    game,
                    root,
                    district.StableId + ".arch",
                    "Wall_Arch",
                    PrimitiveType.Cube,
                    new Vector3(5f, 1.7f, -1f),
                    new Vector3(1.15f, 1.15f, 1.15f),
                    Quaternion.Euler(0f, angle + 180f, 0f),
                    SkylanceMaterialSlot.WeatheredStone);
                CreateVisualPrimitiveLocal(
                    game,
                    root,
                    district.StableId + ".vertical_spire",
                    "Vertical district spire",
                    PrimitiveType.Cylinder,
                    new Vector3(0f, 5f + district.ElevationTier * 0.8f, 4f),
                    new Vector3(0.8f, 5f + district.ElevationTier * 0.8f, 0.8f),
                    Quaternion.Euler(4f, angle, 6f),
                    GetMaterial(game, SkylanceMaterialSlot.Copper));
                CreateVisualPrimitiveLocal(
                    game,
                    root,
                    district.StableId + ".sky_sail",
                    "District sky sail",
                    PrimitiveType.Cube,
                    new Vector3(0f, 8f + district.ElevationTier, 4f),
                    new Vector3(5f, 0.16f, 2.4f),
                    Quaternion.Euler(15f, angle + 24f, 18f),
                    GetMaterial(game, SkylanceMaterialSlot.Cloudglass));
                CreateVisualPrimitiveLocal(
                    game,
                    root,
                    district.StableId + ".route_beacon",
                    "District route beacon",
                    PrimitiveType.Sphere,
                    new Vector3(0f, 10f + district.ElevationTier, 4f),
                    Vector3.one * 0.55f,
                    Quaternion.identity,
                    GetMaterial(game, SkylanceMaterialSlot.WindGlow));
            }
        }

        private static void BuildContentSockets(
            TetherfallGame game,
            Transform parent)
        {
            for (var index = 0; index < ContentSockets.Length; index++)
            {
                var definition = ContentSockets[index];
                var root = CreateSemanticGroup(
                    parent,
                    definition.StableId,
                    definition.DisplayName);
                root.position = definition.Position;
                root.rotation = Quaternion.LookRotation(
                    definition.Forward,
                    Vector3.up);
                var socket = root.gameObject.AddComponent<SkylanceContentSocket>();
                socket.Initialize(definition);

                var material = definition.Kind == SkylanceSocketKind.Boss
                    ? SkylanceMaterialSlot.DarkRock
                    : definition.Kind == SkylanceSocketKind.Vendor
                        ? SkylanceMaterialSlot.Copper
                        : SkylanceMaterialSlot.Cloudglass;
                CreateVisualPrimitiveLocal(
                    game,
                    root,
                    definition.StableId + ".plinth",
                    "Content socket plinth",
                    PrimitiveType.Cylinder,
                    new Vector3(0f, 0.25f, 0f),
                    new Vector3(1.5f, 0.25f, 1.5f),
                    Quaternion.identity,
                    GetMaterial(game, material));
                CreateVisualPrimitiveLocal(
                    game,
                    root,
                    definition.StableId + ".mast",
                    "Content socket mast",
                    PrimitiveType.Cylinder,
                    new Vector3(0f, 1.6f, 0f),
                    new Vector3(0.12f, 1.6f, 0.12f),
                    Quaternion.identity,
                    GetMaterial(game, SkylanceMaterialSlot.Copper));
                CreateVisualPrimitiveLocal(
                    game,
                    root,
                    definition.StableId + ".signal",
                    "Content socket signal",
                    PrimitiveType.Sphere,
                    new Vector3(0f, 3.15f, 0f),
                    Vector3.one * 0.35f,
                    Quaternion.identity,
                    GetMaterial(game, SkylanceMaterialSlot.WindGlow));
            }
        }

        private static void BuildDistantSilhouettes(
            TetherfallGame game,
            Transform parent)
        {
            var centers = new[]
            {
                new Vector3(350f, 220f, 645f),
                new Vector3(420f, 246f, 690f),
                new Vector3(510f, 232f, 720f),
                new Vector3(610f, 260f, 665f),
                new Vector3(655f, 228f, 560f),
                new Vector3(330f, 240f, 555f),
                new Vector3(690f, 205f, 430f),
                new Vector3(300f, 214f, 430f)
            };
            for (var index = 0; index < centers.Length; index++)
            {
                var id = "silhouette.skylance.sky_island." + (index + 1);
                var root = CreateSemanticGroup(parent, id, "Distant sky island");
                root.position = centers[index];
                var scale = 18f + index % 3 * 5f;
                CreateVisualPrimitiveLocal(
                    game,
                    root,
                    id + ".mass",
                    "Distant island mass",
                    PrimitiveType.Sphere,
                    Vector3.zero,
                    new Vector3(scale, 4f + index % 2 * 2f, scale * 0.72f),
                    Quaternion.Euler(0f, index * 31f, 0f),
                    GetMaterial(game, SkylanceMaterialSlot.DarkRock));
                CreateVisualPrimitiveLocal(
                    game,
                    root,
                    id + ".needle",
                    "Distant island needle",
                    PrimitiveType.Cylinder,
                    new Vector3(0f, 13f + index % 3 * 3f, 0f),
                    new Vector3(0.55f, 11f + index % 3 * 3f, 0.55f),
                    Quaternion.Euler(index % 2 == 0 ? 5f : -6f, 0f, 4f),
                    GetMaterial(game, SkylanceMaterialSlot.Cloudglass));
            }
        }

        private static void BuildAccessRoute(
            TetherfallGame game,
            SkylanceWorldRuntime runtime,
            Transform accessRoot,
            Transform mountainDetailRoot,
            Transform kingdomDetailRoot)
        {
            if (!TryGetRoute(AscentRouteId, out var ascent))
            {
                return;
            }

            for (var index = 0; index < ascent.NodeCount; index++)
            {
                ascent.TryGetNode(index, out var node);
                if (node.Kind != SkylanceRouteNodeKind.LiftRing)
                {
                    continue;
                }

                var ringParent = node.Position.y >= KingdomDetailMinimumAltitude
                    ? kingdomDetailRoot
                    : mountainDetailRoot;
                BuildAerialRing(
                    game,
                    ringParent,
                    node.StableId + ".visual",
                    node.Position,
                    node.CaptureRadius,
                    index * 19f);
                var volumeRoot = new GameObject(node.StableId + " gated lift");
                volumeRoot.transform.SetParent(accessRoot, false);
                volumeRoot.transform.position = node.Position;
                var collider = volumeRoot.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = Vector3.one * node.CaptureRadius * 1.7f;
                var lift = volumeRoot.AddComponent<SkylanceGatedLiftVolume>();
                lift.Initialize(game, runtime, node.StableId, node.LiftVelocity);
                var semantic = volumeRoot.AddComponent<SkylanceSemanticId>();
                semantic.Initialize(node.StableId);
            }

            if (TryGetRoute(CircuitRouteId, out var circuit))
            {
                for (var index = 0; index < circuit.NodeCount - 1; index++)
                {
                    circuit.TryGetNode(index, out var node);
                    BuildAerialRing(
                        game,
                        kingdomDetailRoot,
                        node.StableId + ".visual",
                        node.Position,
                        node.CaptureRadius,
                        40f + index * 23f);
                }
            }

            var consoleRoot = CreateSemanticGroup(
                accessRoot,
                "access.skylance.sunhook_console",
                "Sunhook ascent console");
            consoleRoot.position = new Vector3(524f, 55.1f, 497f);
            var console = consoleRoot.gameObject.AddComponent<SkylanceAscentConsole>();
            ascent.TryGetNode(0, out var launchNode);
            console.Initialize(game, runtime, launchNode.LiftVelocity);
            game.RegisterInteractable(console);
            CreateVisualPrimitiveLocal(
                game,
                consoleRoot,
                "art.skylance.sunhook_console.plinth",
                "Sunhook console plinth",
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.45f, 0f),
                new Vector3(1.8f, 0.45f, 1.8f),
                Quaternion.identity,
                GetMaterial(game, SkylanceMaterialSlot.Copper));
            CreateVisualPrimitiveLocal(
                game,
                consoleRoot,
                "art.skylance.sunhook_console.lens",
                "Sunhook console lens",
                PrimitiveType.Sphere,
                new Vector3(0f, 1.55f, 0f),
                Vector3.one * 0.52f,
                Quaternion.identity,
                GetMaterial(game, SkylanceMaterialSlot.WindGlow));
            WorldVisuals.AddLabel(
                consoleRoot,
                "Sunhook ascent",
                new Vector3(0f, 2.4f, 0f),
                new Color(0.62f, 0.94f, 1f),
                0.09f);

            var descentRoot = CreateSemanticGroup(
                accessRoot,
                "access.skylance.windward_descent",
                "Windward safe descent");
            descentRoot.position = new Vector3(488f, 177.2f, 463f);
            var descent = descentRoot.gameObject.AddComponent<SkylanceDescentPerch>();
            descent.Initialize(game, MountainSummitPosition);
            game.RegisterInteractable(descent);
            CreateVisualPrimitiveLocal(
                game,
                descentRoot,
                "art.skylance.windward_descent.perch",
                "Windward descent perch",
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.25f, 0f),
                new Vector3(2.4f, 0.25f, 2.4f),
                Quaternion.identity,
                GetMaterial(game, SkylanceMaterialSlot.Cloudglass));
            WorldVisuals.AddLabel(
                descentRoot,
                "Lee descent",
                new Vector3(0f, 1.3f, 0f),
                new Color(0.78f, 0.92f, 1f),
                0.08f);
        }

        private static void BuildAerialRing(
            TetherfallGame game,
            Transform parent,
            string stableId,
            Vector3 position,
            float radius,
            float yaw)
        {
            var root = CreateSemanticGroup(parent, stableId, "Aerial route ring");
            root.position = position;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            var segmentLength =
                2f * Mathf.PI * radius / RingSegmentCount * 0.78f;
            for (var index = 0; index < RingSegmentCount; index++)
            {
                var angle = index * 360f / RingSegmentCount;
                var radians = angle * Mathf.Deg2Rad;
                CreateVisualPrimitiveLocal(
                    game,
                    root,
                    stableId + ".segment_" + (index + 1),
                    "Aerial ring segment",
                    PrimitiveType.Cube,
                    new Vector3(
                        Mathf.Cos(radians) * radius,
                        Mathf.Sin(radians) * radius,
                        0f),
                    new Vector3(segmentLength, 0.22f, 0.22f),
                    Quaternion.Euler(0f, 0f, angle + 90f),
                    GetMaterial(game, SkylanceMaterialSlot.WindGlow));
            }
        }

        private static void BuildFailSafeVolumes(
            TetherfallGame game,
            SkylanceWorldRuntime runtime,
            Transform parent)
        {
            CreateFailSafeVolume(
                game,
                runtime,
                parent,
                "volume.skylance.alpine.ravine_recovery",
                new Vector3(480f, -6f, 418f),
                new Vector3(200f, 12f, 220f),
                MountainRespawnPosition,
                "Threadglass catches the fall and returns you to the lower switchback.");
            CreateFailSafeVolume(
                game,
                runtime,
                parent,
                "volume.skylance.aerie.cloud_recovery",
                new Vector3(490f, 117f, 490f),
                new Vector3(150f, 32f, 160f),
                MountainSummitPosition,
                "The lee current returns you safely to Sunhook summit.");
        }

        private static void CreateFailSafeVolume(
            TetherfallGame game,
            SkylanceWorldRuntime runtime,
            Transform parent,
            string stableId,
            Vector3 position,
            Vector3 size,
            Vector3 recoveryPosition,
            string feedback)
        {
            var root = new GameObject(stableId);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
            var volume = root.AddComponent<SkylanceFailSafeVolume>();
            volume.Initialize(
                game,
                runtime,
                stableId,
                recoveryPosition,
                feedback);
            var semantic = root.AddComponent<SkylanceSemanticId>();
            semantic.Initialize(stableId);
        }

        private static void PrepareCaches(TetherfallGame game)
        {
            if (materialOwner == game)
            {
                return;
            }

            materialOwner = game;
            for (var index = 0; index < CachedMaterials.Length; index++)
            {
                CachedMaterials[index] = null;
            }

            PrefabCache.Clear();
        }

        private static Material GetMaterial(
            TetherfallGame game,
            SkylanceMaterialSlot slot)
        {
            var index = (int)slot;
            if (index < 0 || index >= CachedMaterials.Length)
            {
                index = 0;
            }

            if (CachedMaterials[index] != null)
            {
                return CachedMaterials[index];
            }

            Material material;
            switch (slot)
            {
                case SkylanceMaterialSlot.Snow:
                    material = RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.78f, 0.86f, 0.91f),
                        0.02f,
                        0.38f);
                    break;
                case SkylanceMaterialSlot.BlueIce:
                    material = RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.31f, 0.5f, 0.69f),
                        0.12f,
                        0.68f);
                    break;
                case SkylanceMaterialSlot.DarkRock:
                    material = RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.075f, 0.09f, 0.14f),
                        0.04f,
                        0.22f);
                    break;
                case SkylanceMaterialSlot.WeatheredStone:
                    material = RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.3f, 0.34f, 0.42f),
                        0.06f,
                        0.3f);
                    break;
                case SkylanceMaterialSlot.Copper:
                    material = RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.7f, 0.39f, 0.18f),
                        0.42f,
                        0.56f);
                    break;
                case SkylanceMaterialSlot.Cloudglass:
                    material = RuntimeEffectMaterialCache.GetTransparent(
                        game,
                        new Color(0.51f, 0.78f, 0.9f, 0.66f),
                        0.74f);
                    break;
                case SkylanceMaterialSlot.Timber:
                    material = RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.25f, 0.17f, 0.12f),
                        0.02f,
                        0.24f);
                    break;
                default:
                    material = RuntimeEffectMaterialCache.GetGlow(
                        game,
                        new Color(0.33f, 0.91f, 0.9f),
                        1.15f,
                        0.66f);
                    break;
            }

            CachedMaterials[index] = material;
            return material;
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            return root;
        }

        private static Transform CreateSemanticGroup(
            Transform parent,
            string stableId,
            string name)
        {
            var root = CreateGroup(parent, name);
            var semantic = root.gameObject.AddComponent<SkylanceSemanticId>();
            semantic.Initialize(stableId);
            return root;
        }

        private static void CreateCollisionBox(
            Transform parent,
            string stableId,
            string name,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            bool climbable)
        {
            var root = new GameObject(name + " Collision");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = rotation;
            root.transform.localScale = SanitizeScale(scale);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
            if (climbable)
            {
                root.AddComponent<ClimbableSurface>();
            }

            var semantic = root.AddComponent<SkylanceSemanticId>();
            semantic.Initialize(stableId);
        }

        private static GameObject CreateVisualPrimitive(
            TetherfallGame game,
            Transform parent,
            string stableId,
            string name,
            PrimitiveType primitive,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Material material)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name + " Art";
            part.transform.SetParent(parent, false);
            part.transform.position = position;
            part.transform.rotation = rotation;
            part.transform.localScale = SanitizeScale(scale);
            RemoveComponent(part.GetComponent<Collider>());
            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            var semantic = part.AddComponent<SkylanceSemanticId>();
            semantic.Initialize(stableId);
            return part;
        }

        private static GameObject CreateVisualPrimitiveLocal(
            TetherfallGame game,
            Transform parent,
            string stableId,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name + " Art";
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = SanitizeScale(localScale);
            RemoveComponent(part.GetComponent<Collider>());
            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            var semantic = part.AddComponent<SkylanceSemanticId>();
            semantic.Initialize(stableId);
            return part;
        }

        private static GameObject CreateAssetOrFallback(
            TetherfallGame game,
            Transform parent,
            string stableId,
            string resourceName,
            PrimitiveType fallbackPrimitive,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            SkylanceMaterialSlot fallbackMaterial)
        {
            var prefab = LoadPrefab(resourceName);
            GameObject instance;
            if (prefab != null)
            {
                instance = Object.Instantiate(prefab, parent, false);
                instance.name = stableId + " Existing Asset Art";
                instance.transform.localPosition = localPosition;
                instance.transform.localRotation = localRotation;
                instance.transform.localScale = SanitizeScale(localScale);
                StripArtGameplayComponents(instance);
                var semantic = instance.AddComponent<SkylanceSemanticId>();
                semantic.Initialize(stableId);
                return instance;
            }

            instance = CreateVisualPrimitiveLocal(
                game,
                parent,
                stableId,
                stableId + " procedural fallback",
                fallbackPrimitive,
                localPosition,
                localScale,
                localRotation,
                GetMaterial(game, fallbackMaterial));
            return instance;
        }

        private static GameObject LoadPrefab(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                return null;
            }

            if (PrefabCache.TryGetValue(resourceName, out var cached))
            {
                return cached;
            }

            var prefab = Resources.Load<GameObject>(
                TetherfallQuaterniusAssetCatalog.ResourcePrefix + resourceName);
            PrefabCache[resourceName] = prefab;
            return prefab;
        }

        private static void StripArtGameplayComponents(GameObject root)
        {
            RemoveComponents<Collider>(root);
            RemoveComponents<Collider2D>(root);
            RemoveComponents<Light>(root);
            RemoveComponents<Camera>(root);
            RemoveComponents<Animator>(root);
            RemoveComponents<Animation>(root);
            RemoveComponents<Rigidbody>(root);
            RemoveComponents<CharacterController>(root);
        }

        private static void RemoveComponents<T>(GameObject root)
            where T : Component
        {
            var components = root.GetComponentsInChildren<T>(true);
            for (var index = 0; index < components.Length; index++)
            {
                RemoveComponent(components[index]);
            }
        }

        private static void RemoveComponent(Object component)
        {
            if (component == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(component);
            }
            else
            {
                Object.DestroyImmediate(component);
            }
        }

        private static Transform FindDirectChild(
            Transform parent,
            string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child != null &&
                    string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static Vector3 SanitizeScale(Vector3 value)
        {
            if (!IsFinite(value))
            {
                return Vector3.one;
            }

            return new Vector3(
                Mathf.Max(0.02f, Mathf.Abs(value.x)),
                Mathf.Max(0.02f, Mathf.Abs(value.y)),
                Mathf.Max(0.02f, Mathf.Abs(value.z)));
        }

        private static bool ContainsHorizontal(
            Rect bounds,
            Vector3 position,
            float margin)
        {
            return position.x >= bounds.xMin - margin &&
                   position.x <= bounds.xMax + margin &&
                   position.z >= bounds.yMin - margin &&
                   position.z <= bounds.yMax + margin;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static SkylanceTerrainPieceDefinition Terrain(
            string id,
            string name,
            PrimitiveType primitive,
            Vector3 position,
            Vector3 scale,
            Vector3 euler,
            SkylanceMaterialSlot material,
            bool solid,
            bool climbable)
        {
            return new SkylanceTerrainPieceDefinition(
                id,
                name,
                SkylanceZone.AlpineApproach,
                primitive,
                position,
                scale,
                euler,
                material,
                solid,
                climbable);
        }

        private static SkylanceIslandDefinition Island(
            string id,
            string name,
            string districtId,
            Vector3 center,
            Vector2 footprint,
            float depth)
        {
            return new SkylanceIslandDefinition(
                id,
                name,
                districtId,
                center,
                footprint,
                depth);
        }

        private static SkylanceBridgeDefinition Bridge(
            string id,
            string from,
            string to,
            float width,
            SkylanceMaterialSlot material)
        {
            return new SkylanceBridgeDefinition(
                id,
                from,
                to,
                width,
                material);
        }

        private static SkylanceDistrictDefinition District(
            string id,
            string name,
            string island,
            int tier,
            Vector3 plaza)
        {
            return new SkylanceDistrictDefinition(
                id,
                name,
                island,
                tier,
                plaza);
        }

        private static SkylanceContentSocketDefinition Socket(
            string id,
            string name,
            SkylanceSocketKind kind,
            string district,
            Vector3 position,
            Vector3 forward)
        {
            return new SkylanceContentSocketDefinition(
                id,
                name,
                kind,
                district,
                position,
                forward);
        }

        private static SkylanceEncounterSocketDefinition Encounter(
            string id,
            string name,
            string encounter,
            string persistence,
            Vector3 position,
            bool spawn)
        {
            return new SkylanceEncounterSocketDefinition(
                id,
                name,
                encounter,
                persistence,
                position,
                spawn);
        }

        private static SkylanceRouteNode Node(
            string id,
            Vector3 position,
            float radius,
            float lift,
            SkylanceRouteNodeKind kind)
        {
            return new SkylanceRouteNode(
                id,
                position,
                radius,
                lift,
                kind);
        }
    }

    [DisallowMultipleComponent]
    public sealed class SkylanceSemanticId : MonoBehaviour
    {
        public string StableId { get; private set; } = string.Empty;

        public void Initialize(string stableId)
        {
            StableId = stableId ?? string.Empty;
        }
    }

    [DisallowMultipleComponent]
    public sealed class SkylanceContentSocket : MonoBehaviour
    {
        public string StableId { get; private set; } = string.Empty;
        public string DisplayName { get; private set; } = string.Empty;
        public string DistrictId { get; private set; } = string.Empty;
        public SkylanceSocketKind Kind { get; private set; }
        public Vector3 AuthoredPosition { get; private set; }
        public Vector3 AuthoredForward { get; private set; }

        public void Initialize(SkylanceContentSocketDefinition definition)
        {
            StableId = definition.StableId;
            DisplayName = definition.DisplayName;
            DistrictId = definition.DistrictId;
            Kind = definition.Kind;
            AuthoredPosition = definition.Position;
            AuthoredForward = definition.Forward;
        }
    }

    [DisallowMultipleComponent]
    public sealed class SkylanceWorldRuntime : MonoBehaviour
    {
        private TetherfallGame game;
        private GameObject mountainDetailRoot;
        private GameObject kingdomDetailRoot;
        private bool authoredAscentUnlocked;
        private bool ascentRouteAvailable = true;
        private bool accessDeniedNotified;
        private float nextRefreshAt;

        public bool AuthoredAscentUnlocked => authoredAscentUnlocked;
        public bool AscentRouteAvailable => ascentRouteAvailable;
        public bool MountainDetailActive =>
            mountainDetailRoot != null && mountainDetailRoot.activeSelf;
        public bool KingdomDetailActive =>
            kingdomDetailRoot != null && kingdomDetailRoot.activeSelf;
        public SkylanceAccessState CurrentAccessState { get; private set; }
        public SkylanceAccessDecision CurrentAccessDecision { get; private set; }

        public void Initialize(
            TetherfallGame owner,
            GameObject mountainDetails,
            GameObject kingdomDetails)
        {
            game = owner;
            mountainDetailRoot = mountainDetails;
            kingdomDetailRoot = kingdomDetails;
            nextRefreshAt = 0f;
            RefreshNow();
        }

        public void SetAuthoredAscentUnlocked(bool unlocked)
        {
            authoredAscentUnlocked = unlocked;
            RefreshNow();
        }

        public void SetAscentRouteAvailable(bool available)
        {
            ascentRouteAvailable = available;
            RefreshNow();
        }

        public SkylanceAccessDecision EvaluateAccess(bool returnTravel)
        {
            var state = SkylanceAccessRules.CaptureGameState(
                game,
                authoredAscentUnlocked,
                ascentRouteAvailable,
                returnTravel);
            return SkylanceAccessRules.EvaluateKingdomAccess(in state);
        }

        public void RefreshNow()
        {
            var state = SkylanceAccessRules.CaptureGameState(
                game,
                authoredAscentUnlocked,
                ascentRouteAvailable,
                false);
            CurrentAccessState = state;
            CurrentAccessDecision =
                SkylanceAccessRules.EvaluateKingdomAccess(
                    in state);
            RefreshStreamingAndBoundary();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshAt)
            {
                return;
            }

            nextRefreshAt =
                Time.unscaledTime + TetherfallSkylanceWorld.DirectorRefreshSeconds;
            RefreshNow();
        }

        private void RefreshStreamingAndBoundary()
        {
            if (game == null || game.Player == null)
            {
                SetActive(mountainDetailRoot, false);
                SetActive(kingdomDetailRoot, false);
                return;
            }

            var position = game.PlayerTransform.position;
            var mountainDelta = position - TetherfallSkylanceWorld.MountainOrigin;
            mountainDelta.y = 0f;
            var kingdomDelta = position - TetherfallSkylanceWorld.KingdomCenter;
            kingdomDelta.y = 0f;
            var mountainVisible =
                mountainDelta.sqrMagnitude <=
                TetherfallSkylanceWorld.MountainDetailDistance *
                TetherfallSkylanceWorld.MountainDetailDistance;
            var kingdomVisible =
                CurrentAccessDecision.Allowed &&
                position.y >= TetherfallSkylanceWorld.KingdomDetailMinimumAltitude &&
                kingdomDelta.sqrMagnitude <=
                TetherfallSkylanceWorld.KingdomDetailDistance *
                TetherfallSkylanceWorld.KingdomDetailDistance;
            SetActive(mountainDetailRoot, mountainVisible);
            SetActive(kingdomDetailRoot, kingdomVisible);

            if (CurrentAccessDecision.Allowed ||
                position.y < TetherfallSkylanceWorld.KingdomBoundaryAltitude ||
                !TetherfallSkylanceWorld.TryResolveRegion(
                    position,
                    out var regionId,
                    out _) ||
                !string.Equals(
                    regionId,
                    TetherfallSkylanceWorld.KingdomRegionId,
                    StringComparison.Ordinal))
            {
                accessDeniedNotified = false;
                return;
            }

            game.Player.Teleport(TetherfallSkylanceWorld.MountainSummitPosition);
            if (!accessDeniedNotified)
            {
                game.Notify(
                    "The upper wind refuses the crossing. Earn a flight-capable Pact " +
                    "or restore an authored ascent first.");
                accessDeniedNotified = true;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class SkylanceGatedLiftVolume : MonoBehaviour
    {
        private TetherfallGame game;
        private SkylanceWorldRuntime runtime;
        private string stableId = string.Empty;
        private float liftVelocity;

        public string StableId => stableId;
        public float LiftVelocity => liftVelocity;

        public void Initialize(
            TetherfallGame owner,
            SkylanceWorldRuntime worldRuntime,
            string id,
            float velocity)
        {
            game = owner;
            runtime = worldRuntime;
            stableId = id ?? string.Empty;
            liftVelocity = Mathf.Max(0f, velocity);
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other != null
                ? other.GetComponent<PlayerAvatar>()
                : null;
            if (player == null || game == null || player != game.Player)
            {
                return;
            }

            var decision = runtime != null
                ? runtime.EvaluateAccess(false)
                : new SkylanceAccessDecision(
                    false,
                    SkylanceAccessReason.AscentRouteUnavailable);
            if (!decision.Allowed)
            {
                return;
            }

            player.ApplyWindLift(liftVelocity);
        }
    }

    [DisallowMultipleComponent]
    public sealed class SkylanceAscentConsole : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private SkylanceWorldRuntime runtime;
        private float launchVelocity;

        public string InteractionPrompt
        {
            get
            {
                var decision = runtime != null
                    ? runtime.EvaluateAccess(false)
                    : new SkylanceAccessDecision(
                        false,
                        SkylanceAccessReason.AscentRouteUnavailable);
                return decision.Allowed
                    ? "Launch into the Sunhook aerial route"
                    : "Read the silent Sunhook flight gate";
            }
        }

        public void Initialize(
            TetherfallGame owner,
            SkylanceWorldRuntime worldRuntime,
            float velocity)
        {
            game = owner;
            runtime = worldRuntime;
            launchVelocity = Mathf.Max(0f, velocity);
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null && owner == game && owner.Player != null;
        }

        public void Interact(TetherfallGame owner)
        {
            if (!CanInteract(owner))
            {
                return;
            }

            var decision = runtime != null
                ? runtime.EvaluateAccess(false)
                : new SkylanceAccessDecision(
                    false,
                    SkylanceAccessReason.AscentRouteUnavailable);
            if (!decision.Allowed)
            {
                owner.Notify(
                    "Sunhook remains still. A flight-capable Pact or restored ascent " +
                    "must answer the route first.");
                return;
            }

            owner.Player.ApplyWindLift(launchVelocity);
            owner.Notify(
                "Sunhook catches your glider. Follow the turquoise rings to Windward Gate.");
        }
    }

    [DisallowMultipleComponent]
    public sealed class SkylanceDescentPerch : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private Vector3 returnPosition;

        public string InteractionPrompt => "Take the always-open lee descent";

        public void Initialize(TetherfallGame owner, Vector3 destination)
        {
            game = owner;
            returnPosition = destination;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null && owner == game && owner.Player != null;
        }

        public void Interact(TetherfallGame owner)
        {
            if (!CanInteract(owner))
            {
                return;
            }

            owner.Player.Teleport(returnPosition);
            owner.Notify("The lee current returns the expedition to Sunhook summit.");
        }
    }

    [DisallowMultipleComponent]
    public sealed class SkylanceFailSafeVolume : MonoBehaviour
    {
        private TetherfallGame game;
        private SkylanceWorldRuntime runtime;
        private string stableId = string.Empty;
        private Vector3 recoveryPosition;
        private string feedback = string.Empty;
        private float nextRecoveryAt;

        public string StableId => stableId;
        public Vector3 RecoveryPosition => recoveryPosition;

        public void Initialize(
            TetherfallGame owner,
            SkylanceWorldRuntime worldRuntime,
            string id,
            Vector3 destination,
            string recoveryFeedback)
        {
            game = owner;
            runtime = worldRuntime;
            stableId = id ?? string.Empty;
            recoveryPosition = destination;
            feedback = recoveryFeedback ?? string.Empty;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.unscaledTime < nextRecoveryAt)
            {
                return;
            }

            var player = other != null
                ? other.GetComponent<PlayerAvatar>()
                : null;
            if (player == null || game == null || player != game.Player)
            {
                return;
            }

            nextRecoveryAt = Time.unscaledTime + 0.75f;
            player.Teleport(recoveryPosition);
            runtime?.RefreshNow();
            if (!string.IsNullOrEmpty(feedback))
            {
                game.Notify(feedback);
            }
        }
    }
}
