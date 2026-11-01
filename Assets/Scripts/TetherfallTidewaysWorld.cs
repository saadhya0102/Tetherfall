using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tetherfall
{
    public enum TidewaysContentSocketKind
    {
        Quest,
        Enemy,
        Cargo,
        Vendor,
        Activity,
        SeaBoss
    }

    public enum TidewaysDryRouteStyle
    {
        Quay,
        ArchedBridge
    }

    public enum TidewaysShipClass
    {
        CourierSkiff,
        CanalPacket,
        CargoBarge,
        OceanGalleon
    }

    [Serializable]
    public sealed class TidewaysWaterZoneDefinition
    {
        public string Id { get; }
        public Vector3 Center { get; }
        public Vector3 Size { get; }

        public TidewaysWaterZoneDefinition(string id, Vector3 center, Vector3 size)
        {
            Id = id;
            Center = center;
            Size = size;
        }

        public bool ContainsHorizontal(Vector3 point, float margin = 0f)
        {
            return Mathf.Abs(point.x - Center.x) <= Size.x * 0.5f + margin &&
                   Mathf.Abs(point.z - Center.z) <= Size.z * 0.5f + margin;
        }
    }

    [Serializable]
    public sealed class TidewaysWaterwayDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public float HalfWidth { get; }
        public IReadOnlyList<Vector3> Points => points;

        private readonly Vector3[] points;

        public TidewaysWaterwayDefinition(
            string id,
            string displayName,
            float halfWidth,
            params Vector3[] routePoints)
        {
            Id = id;
            DisplayName = displayName;
            HalfWidth = halfWidth;
            points = routePoints ?? Array.Empty<Vector3>();
        }
    }

    [Serializable]
    public sealed class TidewaysDryIslandDefinition
    {
        public string Id { get; }
        public string DistrictId { get; }
        public Vector3 Center { get; }
        public Vector3 Size { get; }

        public TidewaysDryIslandDefinition(
            string id,
            string districtId,
            Vector3 center,
            Vector3 size)
        {
            Id = id;
            DistrictId = districtId;
            Center = center;
            Size = size;
        }
    }

    [Serializable]
    public sealed class TidewaysDryNodeDefinition
    {
        public string Id { get; }
        public Vector3 Position { get; }

        public TidewaysDryNodeDefinition(string id, Vector3 position)
        {
            Id = id;
            Position = position;
        }
    }

    [Serializable]
    public sealed class TidewaysDryEdgeDefinition
    {
        public string Id { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public TidewaysDryRouteStyle Style { get; }
        public float Width { get; }

        public TidewaysDryEdgeDefinition(
            string id,
            string fromNodeId,
            string toNodeId,
            TidewaysDryRouteStyle style,
            float width)
        {
            Id = id;
            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
            Style = style;
            Width = width;
        }
    }

    [Serializable]
    public sealed class TidewaysParkourRouteDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<Vector3> Points => points;

        private readonly Vector3[] points;

        public TidewaysParkourRouteDefinition(
            string id,
            string displayName,
            params Vector3[] routePoints)
        {
            Id = id;
            DisplayName = displayName;
            points = routePoints ?? Array.Empty<Vector3>();
        }
    }

    [Serializable]
    public sealed class TidewaysDistrictDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Character { get; }
        public Vector3 Center { get; }
        public Vector3 LandmarkPosition { get; }

        public TidewaysDistrictDefinition(
            string id,
            string displayName,
            string character,
            Vector3 center,
            Vector3 landmarkPosition)
        {
            Id = id;
            DisplayName = displayName;
            Character = character;
            Center = center;
            LandmarkPosition = landmarkPosition;
        }
    }

    [Serializable]
    public sealed class TidewaysContentSocketDefinition
    {
        public string Id { get; }
        public TidewaysContentSocketKind Kind { get; }
        public string DisplayName { get; }
        public string InteractionPrompt { get; }
        public string InteractionText { get; }
        public Vector3 Position { get; }

        public TidewaysContentSocketDefinition(
            string id,
            TidewaysContentSocketKind kind,
            string displayName,
            string interactionPrompt,
            string interactionText,
            Vector3 position)
        {
            Id = id;
            Kind = kind;
            DisplayName = displayName;
            InteractionPrompt = interactionPrompt;
            InteractionText = interactionText;
            Position = position;
        }
    }

    [Serializable]
    public sealed class TidewaysDockDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public Vector3 RoutePoint { get; }
        public Vector3 DeckCenter { get; }
        public Vector3 DeckSize { get; }
        public float Yaw { get; }

        public TidewaysDockDefinition(
            string id,
            string displayName,
            Vector3 routePoint,
            Vector3 deckCenter,
            Vector3 deckSize,
            float yaw)
        {
            Id = id;
            DisplayName = displayName;
            RoutePoint = routePoint;
            DeckCenter = deckCenter;
            DeckSize = deckSize;
            Yaw = yaw;
        }
    }

    [Serializable]
    public sealed class TidewaysShipWaypointDefinition
    {
        public string Id { get; }
        public Vector3 Position { get; }
        public string DockId { get; }
        public float DockSeconds { get; }

        public TidewaysShipWaypointDefinition(
            string id,
            Vector3 position,
            string dockId = "",
            float dockSeconds = 0f)
        {
            Id = id;
            Position = position;
            DockId = dockId ?? string.Empty;
            DockSeconds = Mathf.Max(0f, dockSeconds);
        }
    }

    [Serializable]
    public sealed class TidewaysShipRouteDefinition
    {
        public string Id { get; }
        public string ShipId { get; }
        public string DisplayName { get; }
        public TidewaysShipClass ShipClass { get; }
        public float Speed { get; }
        public float InitialOffsetSeconds { get; }
        public Vector2 DeckHalfExtents { get; }
        public float DeckSurfaceY { get; }
        public float VisualDistance { get; }
        public float SimulationDistance { get; }
        public IReadOnlyList<TidewaysShipWaypointDefinition> Waypoints => waypoints;

        private readonly TidewaysShipWaypointDefinition[] waypoints;

        public TidewaysShipRouteDefinition(
            string id,
            string shipId,
            string displayName,
            TidewaysShipClass shipClass,
            float speed,
            float initialOffsetSeconds,
            Vector2 deckHalfExtents,
            float deckSurfaceY,
            float visualDistance,
            float simulationDistance,
            params TidewaysShipWaypointDefinition[] routeWaypoints)
        {
            Id = id;
            ShipId = shipId;
            DisplayName = displayName;
            ShipClass = shipClass;
            Speed = speed;
            InitialOffsetSeconds = initialOffsetSeconds;
            DeckHalfExtents = deckHalfExtents;
            DeckSurfaceY = deckSurfaceY;
            VisualDistance = visualDistance;
            SimulationDistance = simulationDistance;
            waypoints = routeWaypoints ?? Array.Empty<TidewaysShipWaypointDefinition>();
        }
    }

    public struct TidewaysRouteState
    {
        public int SegmentStartIndex;
        public float DistanceAlongSegment;
        public float DockRemaining;

        public TidewaysRouteState(
            int segmentStartIndex,
            float distanceAlongSegment,
            float dockRemaining)
        {
            SegmentStartIndex = segmentStartIndex;
            DistanceAlongSegment = distanceAlongSegment;
            DockRemaining = dockRemaining;
        }
    }

    /// <summary>
    /// Pure route math shared by runtime ships and the edit-mode verifier. It does not touch
    /// transforms, physics, clocks, or collections and therefore allocates nothing per update.
    /// </summary>
    public static class TetherfallTidewaysRouteMath
    {
        private const float SegmentEpsilon = 0.0001f;
        private const int MaximumAdvanceIterations = 512;

        public static TidewaysRouteState CreateInitial(TidewaysShipRouteDefinition route)
        {
            if (!HasUsableRoute(route))
            {
                return default;
            }

            return new TidewaysRouteState(
                0,
                0f,
                route.Waypoints[0].DockSeconds);
        }

        public static bool HasUsableRoute(TidewaysShipRouteDefinition route)
        {
            return route != null &&
                   route.Speed > SegmentEpsilon &&
                   IsFinite(route.Speed) &&
                   route.Waypoints != null &&
                   route.Waypoints.Count >= 2;
        }

        public static float CalculateLoopDuration(TidewaysShipRouteDefinition route)
        {
            if (!HasUsableRoute(route))
            {
                return 0f;
            }

            var duration = 0f;
            for (var index = 0; index < route.Waypoints.Count; index++)
            {
                var current = route.Waypoints[index];
                var next = route.Waypoints[(index + 1) % route.Waypoints.Count];
                if (current == null || next == null)
                {
                    return 0f;
                }

                var length = Vector3.Distance(current.Position, next.Position);
                if (!IsFinite(length) || length <= SegmentEpsilon)
                {
                    return 0f;
                }

                duration += current.DockSeconds + length / route.Speed;
            }

            return IsFinite(duration) ? duration : 0f;
        }

        public static TidewaysRouteState Advance(
            TidewaysShipRouteDefinition route,
            TidewaysRouteState state,
            float deltaTime,
            out Vector3 position,
            out Vector3 forward)
        {
            if (!HasUsableRoute(route))
            {
                position = Vector3.zero;
                forward = Vector3.forward;
                return default;
            }

            state = SanitizeState(route, state);
            var remaining = IsFinite(deltaTime) ? Mathf.Max(0f, deltaTime) : 0f;
            var loopDuration = CalculateLoopDuration(route);
            if (loopDuration > SegmentEpsilon && remaining > loopDuration * 2f)
            {
                remaining %= loopDuration;
            }

            var iteration = 0;
            while (remaining > SegmentEpsilon && iteration++ < MaximumAdvanceIterations)
            {
                if (state.DockRemaining > SegmentEpsilon)
                {
                    var consumedPause = Mathf.Min(state.DockRemaining, remaining);
                    state.DockRemaining -= consumedPause;
                    remaining -= consumedPause;
                    if (remaining <= SegmentEpsilon)
                    {
                        break;
                    }
                }

                var nextIndex = (state.SegmentStartIndex + 1) % route.Waypoints.Count;
                var start = route.Waypoints[state.SegmentStartIndex].Position;
                var end = route.Waypoints[nextIndex].Position;
                var segmentLength = Vector3.Distance(start, end);
                if (!IsFinite(segmentLength) || segmentLength <= SegmentEpsilon)
                {
                    state = RecoverToNearestWaypoint(route, start);
                    break;
                }

                state.DistanceAlongSegment = Mathf.Clamp(
                    state.DistanceAlongSegment,
                    0f,
                    segmentLength);
                var distanceRemaining = segmentLength - state.DistanceAlongSegment;
                var availableDistance = remaining * route.Speed;
                if (availableDistance + SegmentEpsilon < distanceRemaining)
                {
                    state.DistanceAlongSegment += availableDistance;
                    remaining = 0f;
                    break;
                }

                var travelSeconds = distanceRemaining / route.Speed;
                remaining = Mathf.Max(0f, remaining - travelSeconds);
                state.SegmentStartIndex = nextIndex;
                state.DistanceAlongSegment = 0f;
                state.DockRemaining = route.Waypoints[nextIndex].DockSeconds;
            }

            Evaluate(route, state, out position, out forward);
            return state;
        }

        public static void Evaluate(
            TidewaysShipRouteDefinition route,
            TidewaysRouteState state,
            out Vector3 position,
            out Vector3 forward)
        {
            if (!HasUsableRoute(route))
            {
                position = Vector3.zero;
                forward = Vector3.forward;
                return;
            }

            state = SanitizeState(route, state);
            var nextIndex = (state.SegmentStartIndex + 1) % route.Waypoints.Count;
            var start = route.Waypoints[state.SegmentStartIndex].Position;
            var end = route.Waypoints[nextIndex].Position;
            var delta = end - start;
            var length = delta.magnitude;
            if (!IsFinite(length) || length <= SegmentEpsilon)
            {
                position = start;
                forward = Vector3.forward;
                return;
            }

            forward = delta / length;
            position = start + forward * Mathf.Clamp(state.DistanceAlongSegment, 0f, length);
        }

        public static TidewaysRouteState RecoverToNearestWaypoint(
            TidewaysShipRouteDefinition route,
            Vector3 referencePosition)
        {
            if (!HasUsableRoute(route))
            {
                return default;
            }

            if (!IsFinite(referencePosition))
            {
                referencePosition = route.Waypoints[0].Position;
            }

            var nearestIndex = 0;
            var nearestDistance = float.PositiveInfinity;
            for (var index = 0; index < route.Waypoints.Count; index++)
            {
                var waypoint = route.Waypoints[index];
                if (waypoint == null || !IsFinite(waypoint.Position))
                {
                    continue;
                }

                var distance = (waypoint.Position - referencePosition).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = index;
                }
            }

            return new TidewaysRouteState(
                nearestIndex,
                0f,
                route.Waypoints[nearestIndex].DockSeconds);
        }

        public static Vector3 SanitizePlatformDelta(Vector3 delta, float maximumMagnitude)
        {
            if (!IsFinite(delta) ||
                !IsFinite(maximumMagnitude) ||
                maximumMagnitude <= 0f)
            {
                return Vector3.zero;
            }

            var squared = delta.sqrMagnitude;
            var maximumSquared = maximumMagnitude * maximumMagnitude;
            if (squared <= maximumSquared)
            {
                return delta;
            }

            return squared > SegmentEpsilon
                ? delta * (maximumMagnitude / Mathf.Sqrt(squared))
                : Vector3.zero;
        }

        public static float CalculateWorstCasePlatformDelta(
            TidewaysShipRouteDefinition route)
        {
            if (route == null)
            {
                return float.PositiveInfinity;
            }

            var deckRadius = route.DeckHalfExtents.magnitude;
            var translation =
                route.Speed * TetherfallTidewaysWorld.MaximumNearSimulationStep;
            var turnRadians =
                TetherfallTidewaysWorld.MaximumShipTurnDegreesPerSecond *
                TetherfallTidewaysWorld.MaximumNearSimulationStep *
                Mathf.Deg2Rad;
            var rotation = 2f * deckRadius * Mathf.Sin(turnRadians * 0.5f);
            return translation + rotation;
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        public static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static TidewaysRouteState SanitizeState(
            TidewaysShipRouteDefinition route,
            TidewaysRouteState state)
        {
            if (state.SegmentStartIndex < 0 ||
                state.SegmentStartIndex >= route.Waypoints.Count)
            {
                state.SegmentStartIndex = 0;
            }

            if (!IsFinite(state.DistanceAlongSegment) || state.DistanceAlongSegment < 0f)
            {
                state.DistanceAlongSegment = 0f;
            }

            if (!IsFinite(state.DockRemaining) || state.DockRemaining < 0f)
            {
                state.DockRemaining = 0f;
            }

            return state;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallTidewaysWorldRoot : MonoBehaviour
    {
        private Material[] ownedMaterials = Array.Empty<Material>();

        public string WorldId { get; private set; }
        public int RendererCount { get; private set; }
        public int ColliderCount { get; private set; }
        public int SharedMaterialCount { get; private set; }
        public int LightCount { get; private set; }
        public int WaterVolumeCount { get; private set; }
        public int ShipCount { get; private set; }
        public int ContentSocketCount { get; private set; }
        public int ImportedPlacementCount { get; private set; }
        public int ImportedFallbackCount { get; private set; }
        public int UpdateDriverCount { get; private set; }

        internal void Configure(
            string worldId,
            int rendererCount,
            int colliderCount,
            int sharedMaterialCount,
            int lightCount,
            int waterVolumeCount,
            int shipCount,
            int contentSocketCount,
            int importedPlacementCount,
            int importedFallbackCount,
            int updateDriverCount,
            Material[] materials)
        {
            WorldId = worldId;
            RendererCount = rendererCount;
            ColliderCount = colliderCount;
            SharedMaterialCount = sharedMaterialCount;
            LightCount = lightCount;
            WaterVolumeCount = waterVolumeCount;
            ShipCount = shipCount;
            ContentSocketCount = contentSocketCount;
            ImportedPlacementCount = importedPlacementCount;
            ImportedFallbackCount = importedFallbackCount;
            UpdateDriverCount = updateDriverCount;
            ownedMaterials = materials ?? Array.Empty<Material>();
        }

        private void OnDestroy()
        {
            for (var index = 0; index < ownedMaterials.Length; index++)
            {
                TetherfallTidewaysWorld.DestroyObject(ownedMaterials[index]);
            }

            ownedMaterials = Array.Empty<Material>();
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallTidewaysVisualCluster : MonoBehaviour
    {
        public string ClusterId { get; private set; }
        public Vector3 Center { get; private set; }
        public float VisibleDistance { get; private set; }

        internal void Initialize(string clusterId, Vector3 center, float visibleDistance)
        {
            ClusterId = clusterId;
            Center = center;
            VisibleDistance = Mathf.Max(1f, visibleDistance);
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallTidewaysStreaming : MonoBehaviour
    {
        private TetherfallGame game;
        private TetherfallTidewaysVisualCluster[] clusters =
            Array.Empty<TetherfallTidewaysVisualCluster>();
        private Renderer[][] rendererGroups = Array.Empty<Renderer[]>();
        private bool[] visibility = Array.Empty<bool>();
        private float nextRefreshAt;

        public int ClusterCount => clusters.Length;

        internal void Initialize(
            TetherfallGame owner,
            List<TetherfallTidewaysVisualCluster> sources)
        {
            game = owner;
            var count = sources != null ? sources.Count : 0;
            clusters = new TetherfallTidewaysVisualCluster[count];
            rendererGroups = new Renderer[count][];
            visibility = new bool[count];
            for (var index = 0; index < count; index++)
            {
                clusters[index] = sources[index];
                rendererGroups[index] = sources[index] != null
                    ? sources[index].GetComponentsInChildren<Renderer>(true)
                    : Array.Empty<Renderer>();
                visibility[index] = true;
            }

            RefreshVisibility(true);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshAt)
            {
                return;
            }

            nextRefreshAt =
                Time.unscaledTime + TetherfallTidewaysWorld.StreamingRefreshSeconds;
            RefreshVisibility(false);
        }

        private void RefreshVisibility(bool force)
        {
            var target = game != null ? game.PlayerTransform : null;
            if (target == null)
            {
                SetAllVisible(true, force);
                return;
            }

            var targetPosition = target.position;
            for (var index = 0; index < clusters.Length; index++)
            {
                var cluster = clusters[index];
                if (cluster == null)
                {
                    continue;
                }

                var delta = targetPosition - cluster.Center;
                var threshold = cluster.VisibleDistance +
                                (visibility[index]
                                    ? TetherfallTidewaysWorld.StreamingHysteresisDistance
                                    : 0f);
                var visible = delta.sqrMagnitude <= threshold * threshold;
                SetVisible(index, visible, force);
            }
        }

        private void SetAllVisible(bool visible, bool force)
        {
            for (var index = 0; index < clusters.Length; index++)
            {
                SetVisible(index, visible, force);
            }
        }

        private void SetVisible(int index, bool visible, bool force)
        {
            if (index < 0 ||
                index >= rendererGroups.Length ||
                (!force && visibility[index] == visible))
            {
                return;
            }

            visibility[index] = visible;
            var group = rendererGroups[index];
            for (var rendererIndex = 0; rendererIndex < group.Length; rendererIndex++)
            {
                if (group[rendererIndex] != null)
                {
                    group[rendererIndex].enabled = visible;
                }
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallTidewaysContentSocket : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private TidewaysContentSocketDefinition definition;

        public string PersistenceId => definition?.Id ?? string.Empty;
        public TidewaysContentSocketKind Kind =>
            definition != null ? definition.Kind : TidewaysContentSocketKind.Activity;
        public TidewaysContentSocketDefinition Definition => definition;
        public string InteractionPrompt =>
            definition?.InteractionPrompt ?? "Inspect Tideways berth";

        internal void Initialize(
            TetherfallGame owner,
            TidewaysContentSocketDefinition socketDefinition)
        {
            game = owner;
            definition = socketDefinition;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null && owner == game && definition != null;
        }

        public void Interact(TetherfallGame owner)
        {
            if (CanInteract(owner))
            {
                owner.Notify(definition.InteractionText);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallTidewaysWaterVolumeIdentity : MonoBehaviour
    {
        public string WaterId { get; private set; }

        internal void Initialize(string waterId)
        {
            WaterId = waterId;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallTidewaysDeliberateCollider : MonoBehaviour
    {
        public string ColliderId { get; private set; }
        public bool IsMovingPlatform { get; private set; }

        internal void Initialize(string colliderId, bool movingPlatform)
        {
            ColliderId = colliderId;
            IsMovingPlatform = movingPlatform;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallTidewaysBridgeIdentity : MonoBehaviour
    {
        public string BridgeId { get; private set; }

        internal void Initialize(string bridgeId)
        {
            BridgeId = bridgeId;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallTidewaysImportedIdentity : MonoBehaviour
    {
        public string PlacementId { get; private set; }
        public string ResourceName { get; private set; }
        public bool UsedFallback { get; private set; }

        internal void Initialize(string placementId, string resourceName, bool usedFallback)
        {
            PlacementId = placementId;
            ResourceName = resourceName;
            UsedFallback = usedFallback;
        }
    }

    /// <summary>
    /// Transform-driven ship movement runs before PlayerAvatar. A standing CharacterController is
    /// carried by the exact bounded platform delta; no Rigidbody participates in ship motion.
    /// </summary>
    [DefaultExecutionOrder(-120)]
    [DisallowMultipleComponent]
    public sealed class TetherfallTidewaysShip : MonoBehaviour
    {
        private TetherfallGame game;
        private TidewaysShipRouteDefinition route;
        private Transform visualRoot;
        private Renderer[] renderers = Array.Empty<Renderer>();
        private BoxCollider deckCollider;
        private TidewaysRouteState routeState;
        private float farSimulationAccumulator;
        private bool presentationActive = true;

        public string ShipId => route?.ShipId ?? string.Empty;
        public string RouteId => route?.Id ?? string.Empty;
        public TidewaysShipRouteDefinition Route => route;
        public TidewaysRouteState RouteState => routeState;
        public int RecoveryCount { get; private set; }
        public bool PresentationActive => presentationActive;

        internal void Initialize(
            TetherfallGame owner,
            TidewaysShipRouteDefinition definition,
            Transform shipVisualRoot,
            BoxCollider platformCollider)
        {
            game = owner;
            route = definition;
            visualRoot = shipVisualRoot;
            deckCollider = platformCollider;
            renderers = visualRoot != null
                ? visualRoot.GetComponentsInChildren<Renderer>(true)
                : Array.Empty<Renderer>();
            routeState = TetherfallTidewaysRouteMath.CreateInitial(route);
            if (route != null && route.InitialOffsetSeconds > 0f)
            {
                routeState = TetherfallTidewaysRouteMath.Advance(
                    route,
                    routeState,
                    route.InitialOffsetSeconds,
                    out _,
                    out _);
            }

            TetherfallTidewaysRouteMath.Evaluate(
                route,
                routeState,
                out var initialPosition,
                out var initialForward);
            transform.position = initialPosition;
            if (initialForward.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(initialForward, Vector3.up);
            }

            SetPresentation(true);
        }

        private void Update()
        {
            if (game == null || route == null || game.GameplayPaused)
            {
                return;
            }

            var player = game.Player;
            var playerPosition = player != null
                ? player.transform.position
                : game.PlayerTransform != null
                    ? game.PlayerTransform.position
                    : transform.position + Vector3.one * 10000f;
            var distanceSquared = (playerPosition - transform.position).sqrMagnitude;
            var supported = IsPlayerSupported(player);
            var visualDistance = route.VisualDistance;
            SetPresentation(
                supported || distanceSquared <= visualDistance * visualDistance);

            var deltaTime = Time.deltaTime;
            var simulationDistance = route.SimulationDistance;
            if (!supported &&
                distanceSquared > simulationDistance * simulationDistance)
            {
                farSimulationAccumulator += deltaTime;
                if (farSimulationAccumulator <
                    TetherfallTidewaysWorld.FarSimulationStepSeconds)
                {
                    return;
                }

                deltaTime = Mathf.Min(
                    farSimulationAccumulator,
                    TetherfallTidewaysWorld.MaximumFarSimulationCatchup);
                farSimulationAccumulator = 0f;
            }
            else
            {
                farSimulationAccumulator = 0f;
                deltaTime = Mathf.Min(
                    deltaTime,
                    TetherfallTidewaysWorld.MaximumNearSimulationStep);
            }

            MoveShip(deltaTime, player, supported);
        }

        private void MoveShip(float deltaTime, PlayerAvatar player, bool supported)
        {
            TetherfallTidewaysRouteMath.Evaluate(
                route,
                routeState,
                out var expectedPosition,
                out _);
            var currentPosition = transform.position;
            if (!TetherfallTidewaysRouteMath.IsFinite(currentPosition) ||
                (currentPosition - expectedPosition).sqrMagnitude >
                TetherfallTidewaysWorld.RouteRecoveryDistance *
                TetherfallTidewaysWorld.RouteRecoveryDistance)
            {
                routeState = TetherfallTidewaysRouteMath.RecoverToNearestWaypoint(
                    route,
                    currentPosition);
                TetherfallTidewaysRouteMath.Evaluate(
                    route,
                    routeState,
                    out expectedPosition,
                    out var recoveryForward);
                transform.SetPositionAndRotation(
                    expectedPosition,
                    recoveryForward.sqrMagnitude > 0.001f
                        ? Quaternion.LookRotation(recoveryForward, Vector3.up)
                        : Quaternion.identity);
                RecoveryCount++;
                return;
            }

            var oldPosition = transform.position;
            var oldRotation = transform.rotation;
            var playerLocal = supported && player != null
                ? Quaternion.Inverse(oldRotation) * (player.transform.position - oldPosition)
                : Vector3.zero;

            routeState = TetherfallTidewaysRouteMath.Advance(
                route,
                routeState,
                deltaTime,
                out var newPosition,
                out var routeForward);
            var targetRotation = routeForward.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(routeForward, Vector3.up)
                : oldRotation;
            var newRotation = Quaternion.RotateTowards(
                oldRotation,
                targetRotation,
                TetherfallTidewaysWorld.MaximumShipTurnDegreesPerSecond * deltaTime);
            transform.SetPositionAndRotation(newPosition, newRotation);

            if (!supported || player == null)
            {
                return;
            }

            var controller = player.GetComponent<CharacterController>();
            if (controller == null || !controller.enabled)
            {
                return;
            }

            var carriedPosition = newPosition + newRotation * playerLocal;
            var platformDelta =
                TetherfallTidewaysRouteMath.SanitizePlatformDelta(
                    carriedPosition - player.transform.position,
                    TetherfallTidewaysWorld.MaximumSafePlatformDelta);
            controller.Move(platformDelta);
        }

        private bool IsPlayerSupported(PlayerAvatar player)
        {
            if (player == null ||
                player.IsSwimming ||
                deckCollider == null ||
                !deckCollider.enabled)
            {
                return false;
            }

            var local = transform.InverseTransformPoint(player.transform.position);
            return Mathf.Abs(local.x) <= route.DeckHalfExtents.x + 0.22f &&
                   Mathf.Abs(local.z) <= route.DeckHalfExtents.y + 0.22f &&
                   local.y >= route.DeckSurfaceY - 0.12f &&
                   local.y <= route.DeckSurfaceY + 0.46f;
        }

        private void SetPresentation(bool active)
        {
            if (presentationActive == active &&
                (deckCollider == null || deckCollider.enabled == active))
            {
                return;
            }

            presentationActive = active;
            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null)
                {
                    renderers[index].enabled = active;
                }
            }

            if (deckCollider != null)
            {
                deckCollider.enabled = active;
            }
        }
    }

    /// <summary>
    /// Optional, additive Tideglass extension. Nothing invokes Build automatically; integration
    /// remains an explicit call from the owning world bootstrap.
    /// </summary>
    public static class TetherfallTidewaysWorld
    {
        public const string WorldId = "world.tideways.tideglass_maritime";
        public const string RootName = "Tetherfall Tideways World";
        public const string VisualRootName = "Visuals · Distance Streamed";
        public const string GameplayRootName = "Gameplay · Always Available";
        public const string ShipRootName = "Ships · Deterministic Moving Platforms";

        public const float WaterSurfaceY = 0.08f;
        public const float WaterVolumeCenterY = -0.72f;
        public const float WaterVolumeHeight = 3f;
        public const float MaximumNearSimulationStep = 0.05f;
        public const float FarSimulationStepSeconds = 0.5f;
        public const float MaximumFarSimulationCatchup = 0.75f;
        public const float MaximumShipTurnDegreesPerSecond = 18f;
        public const float MaximumSafePlatformDelta = 0.4f;
        public const float RouteRecoveryDistance = 18f;
        public const float StreamingRefreshSeconds = 0.25f;
        public const float StreamingHysteresisDistance = 24f;
        public const float MaximumParkourSpan = 32f;

        public const int ExpectedWaterZoneCount = 5;
        public const int ExpectedWaterwayCount = 4;
        public const int ExpectedDryIslandCount = 6;
        public const int ExpectedDistrictCount = 4;
        public const int ExpectedArchedBridgeCount = 11;
        public const int ExpectedParkourRouteCount = 2;
        public const int ExpectedShipCount = 4;
        public const int ExpectedDockCount = 8;
        public const int ExpectedContentSocketCount = 10;
        public const int ExpectedImportedPlacementCount = 12;
        public const int MaxWaterVolumeCount = 10;

        public const int MaxRendererCount = 560;
        public const int MaxDeliberateColliderCount = 152;
        public const int MaxSharedMaterialCount = 40;
        public const int AddedRealtimeLightCount = 0;
        public const int MaxActiveUpdateDrivers = 5;
        public const int PerPieceUpdateLoopCount = 0;

        public static readonly Vector3 OceanSurfaceCenter =
            new Vector3(0f, WaterSurfaceY - 0.04f, -540f);
        public static readonly Vector3 OceanSurfaceSize =
            new Vector3(360f, 0.08f, 180f);

        private const string RialtoDistrictId = "district.tideways.saffron_quays";
        private const string BellfoundryDistrictId = "district.tideways.bellfoundry";
        private const string ArsenalDistrictId = "district.tideways.arsenal_reach";
        private const string SaltgardenDistrictId = "district.tideways.saltgarden";

        private static readonly TidewaysWaterZoneDefinition[] WaterZoneDefinitions =
        {
            new TidewaysWaterZoneDefinition(
                "waterzone.tideways.ocean.west",
                new Vector3(-117.5f, WaterVolumeCenterY, -540f),
                new Vector3(125f, WaterVolumeHeight, 180f)),
            new TidewaysWaterZoneDefinition(
                "waterzone.tideways.ocean.east",
                new Vector3(157.5f, WaterVolumeCenterY, -540f),
                new Vector3(45f, WaterVolumeHeight, 180f)),
            new TidewaysWaterZoneDefinition(
                "waterzone.tideways.ocean.north",
                new Vector3(40f, WaterVolumeCenterY, -462.5f),
                new Vector3(190f, WaterVolumeHeight, 25f)),
            new TidewaysWaterZoneDefinition(
                "waterzone.tideways.ocean.south",
                new Vector3(40f, WaterVolumeCenterY, -612.5f),
                new Vector3(190f, WaterVolumeHeight, 35f)),
            new TidewaysWaterZoneDefinition(
                "waterzone.tideways.harbor.apron",
                new Vector3(40f, WaterVolumeCenterY, -590.5f),
                new Vector3(190f, WaterVolumeHeight, 9f))
        };

        private static readonly TidewaysWaterwayDefinition[] WaterwayDefinitions =
        {
            new TidewaysWaterwayDefinition(
                "waterway.tideways.tideglass_gate",
                "Tideglass Gate",
                13f,
                new Vector3(2f, WaterSurfaceY, -438f),
                new Vector3(2f, WaterSurfaceY, -462f),
                new Vector3(4f, WaterSurfaceY, -486f)),
            new TidewaysWaterwayDefinition(
                "waterway.tideways.grand_serein",
                "Grand Serein Canal",
                7f,
                new Vector3(4f, WaterSurfaceY, -482f),
                new Vector3(4f, WaterSurfaceY, -590f)),
            new TidewaysWaterwayDefinition(
                "waterway.tideways.lantern_cut",
                "Lantern Cut",
                6f,
                new Vector3(66f, WaterSurfaceY, -482f),
                new Vector3(66f, WaterSurfaceY, -590f)),
            new TidewaysWaterwayDefinition(
                "waterway.tideways.rialto_crosscurrent",
                "Rialto Crosscurrent",
                7f,
                new Vector3(-55f, WaterSurfaceY, -536f),
                new Vector3(135f, WaterSurfaceY, -536f))
        };

        private static readonly TidewaysDryIslandDefinition[] DryIslandDefinitions =
        {
            new TidewaysDryIslandDefinition(
                "island.tideways.rialto_north",
                RialtoDistrictId,
                new Vector3(-28f, 0f, -505f),
                new Vector3(44f, 2f, 46f)),
            new TidewaysDryIslandDefinition(
                "island.tideways.bellfoundry_north",
                BellfoundryDistrictId,
                new Vector3(36f, 0f, -505f),
                new Vector3(44f, 2f, 46f)),
            new TidewaysDryIslandDefinition(
                "island.tideways.arsenal_north",
                ArsenalDistrictId,
                new Vector3(99f, 0f, -505f),
                new Vector3(50f, 2f, 46f)),
            new TidewaysDryIslandDefinition(
                "island.tideways.saltgarden_south",
                SaltgardenDistrictId,
                new Vector3(-28f, 0f, -565f),
                new Vector3(44f, 2f, 42f)),
            new TidewaysDryIslandDefinition(
                "island.tideways.basilica_south",
                BellfoundryDistrictId,
                new Vector3(36f, 0f, -565f),
                new Vector3(44f, 2f, 42f)),
            new TidewaysDryIslandDefinition(
                "island.tideways.shipyard_south",
                ArsenalDistrictId,
                new Vector3(99f, 0f, -565f),
                new Vector3(50f, 2f, 42f))
        };

        private static readonly TidewaysDryNodeDefinition[] DryNodeDefinitions =
        {
            new TidewaysDryNodeDefinition(
                "drynode.tideways.tideglass_causeway",
                new Vector3(-21f, 1.18f, -456f)),
            new TidewaysDryNodeDefinition(
                "drynode.tideways.rialto_north",
                new Vector3(-28f, 1.18f, -486f)),
            new TidewaysDryNodeDefinition(
                "drynode.tideways.bellfoundry_north",
                new Vector3(37f, 1.18f, -486f)),
            new TidewaysDryNodeDefinition(
                "drynode.tideways.arsenal_north",
                new Vector3(99f, 1.18f, -486f)),
            new TidewaysDryNodeDefinition(
                "drynode.tideways.rialto_market",
                new Vector3(-28f, 1.18f, -518f)),
            new TidewaysDryNodeDefinition(
                "drynode.tideways.bellfoundry_square",
                new Vector3(37f, 1.18f, -518f)),
            new TidewaysDryNodeDefinition(
                "drynode.tideways.arsenal_market",
                new Vector3(99f, 1.18f, -518f)),
            new TidewaysDryNodeDefinition(
                "drynode.tideways.saltgarden_walk",
                new Vector3(-28f, 1.18f, -548f)),
            new TidewaysDryNodeDefinition(
                "drynode.tideways.basilica_walk",
                new Vector3(37f, 1.18f, -548f)),
            new TidewaysDryNodeDefinition(
                "drynode.tideways.shipyard_walk",
                new Vector3(99f, 1.18f, -548f)),
            new TidewaysDryNodeDefinition(
                "drynode.tideways.saltgarden_dock",
                new Vector3(-28f, 1.18f, -584f)),
            new TidewaysDryNodeDefinition(
                "drynode.tideways.basilica_dock",
                new Vector3(37f, 1.18f, -584f)),
            new TidewaysDryNodeDefinition(
                "drynode.tideways.arsenal_dock",
                new Vector3(99f, 1.18f, -584f))
        };

        private static readonly TidewaysDryEdgeDefinition[] DryEdgeDefinitions =
        {
            DryEdge(
                "dryedge.tideways.strand_approach",
                "drynode.tideways.tideglass_causeway",
                "drynode.tideways.rialto_north",
                TidewaysDryRouteStyle.Quay),
            DryEdge(
                "dryedge.tideways.north_grand_bridge",
                "drynode.tideways.rialto_north",
                "drynode.tideways.bellfoundry_north",
                TidewaysDryRouteStyle.ArchedBridge),
            DryEdge(
                "dryedge.tideways.north_lantern_bridge",
                "drynode.tideways.bellfoundry_north",
                "drynode.tideways.arsenal_north",
                TidewaysDryRouteStyle.ArchedBridge),
            DryEdge(
                "dryedge.tideways.rialto_quay",
                "drynode.tideways.rialto_north",
                "drynode.tideways.rialto_market",
                TidewaysDryRouteStyle.Quay),
            DryEdge(
                "dryedge.tideways.bellfoundry_quay",
                "drynode.tideways.bellfoundry_north",
                "drynode.tideways.bellfoundry_square",
                TidewaysDryRouteStyle.Quay),
            DryEdge(
                "dryedge.tideways.arsenal_quay",
                "drynode.tideways.arsenal_north",
                "drynode.tideways.arsenal_market",
                TidewaysDryRouteStyle.Quay),
            DryEdge(
                "dryedge.tideways.market_grand_bridge",
                "drynode.tideways.rialto_market",
                "drynode.tideways.bellfoundry_square",
                TidewaysDryRouteStyle.ArchedBridge),
            DryEdge(
                "dryedge.tideways.market_lantern_bridge",
                "drynode.tideways.bellfoundry_square",
                "drynode.tideways.arsenal_market",
                TidewaysDryRouteStyle.ArchedBridge),
            DryEdge(
                "dryedge.tideways.saltgarden_cross_bridge",
                "drynode.tideways.rialto_market",
                "drynode.tideways.saltgarden_walk",
                TidewaysDryRouteStyle.ArchedBridge),
            DryEdge(
                "dryedge.tideways.basilica_cross_bridge",
                "drynode.tideways.bellfoundry_square",
                "drynode.tideways.basilica_walk",
                TidewaysDryRouteStyle.ArchedBridge),
            DryEdge(
                "dryedge.tideways.shipyard_cross_bridge",
                "drynode.tideways.arsenal_market",
                "drynode.tideways.shipyard_walk",
                TidewaysDryRouteStyle.ArchedBridge),
            DryEdge(
                "dryedge.tideways.south_grand_bridge",
                "drynode.tideways.saltgarden_walk",
                "drynode.tideways.basilica_walk",
                TidewaysDryRouteStyle.ArchedBridge),
            DryEdge(
                "dryedge.tideways.south_lantern_bridge",
                "drynode.tideways.basilica_walk",
                "drynode.tideways.shipyard_walk",
                TidewaysDryRouteStyle.ArchedBridge),
            DryEdge(
                "dryedge.tideways.saltgarden_dockwalk",
                "drynode.tideways.saltgarden_walk",
                "drynode.tideways.saltgarden_dock",
                TidewaysDryRouteStyle.Quay),
            DryEdge(
                "dryedge.tideways.basilica_dockwalk",
                "drynode.tideways.basilica_walk",
                "drynode.tideways.basilica_dock",
                TidewaysDryRouteStyle.Quay),
            DryEdge(
                "dryedge.tideways.arsenal_dockwalk",
                "drynode.tideways.shipyard_walk",
                "drynode.tideways.arsenal_dock",
                TidewaysDryRouteStyle.Quay),
            DryEdge(
                "dryedge.tideways.dock_grand_bridge",
                "drynode.tideways.saltgarden_dock",
                "drynode.tideways.basilica_dock",
                TidewaysDryRouteStyle.ArchedBridge),
            DryEdge(
                "dryedge.tideways.dock_lantern_bridge",
                "drynode.tideways.basilica_dock",
                "drynode.tideways.arsenal_dock",
                TidewaysDryRouteStyle.ArchedBridge)
        };

        private static readonly TidewaysParkourRouteDefinition[] ParkourDefinitions =
        {
            new TidewaysParkourRouteDefinition(
                "parkour.tideways.copper_eaves",
                "Copper Eaves Run",
                new Vector3(-48f, 1.25f, -512f),
                new Vector3(-48f, 3.2f, -507f),
                new Vector3(-42f, 6.5f, -502f),
                new Vector3(-15f, 6.2f, -502f),
                new Vector3(-2f, 4.6f, -510f),
                new Vector3(20f, 6.4f, -502f),
                new Vector3(51f, 7.8f, -502f)),
            new TidewaysParkourRouteDefinition(
                "parkour.tideways.arsenal_lanternline",
                "Arsenal Lanternline",
                new Vector3(77f, 1.25f, -550f),
                new Vector3(80f, 3.4f, -556f),
                new Vector3(82f, 6.5f, -562f),
                new Vector3(112f, 6.7f, -562f),
                new Vector3(119f, 8.8f, -558f),
                new Vector3(105f, 10.8f, -550f),
                new Vector3(99f, 8.2f, -532f),
                new Vector3(92f, 6.7f, -514f),
                new Vector3(82f, 6.5f, -502f))
        };

        private static readonly TidewaysDistrictDefinition[] DistrictDefinitions =
        {
            new TidewaysDistrictDefinition(
                RialtoDistrictId,
                "Saffron Quays",
                "arcaded markets and courier bridges",
                new Vector3(-28f, 1f, -510f),
                new Vector3(-44f, 1f, -502f)),
            new TidewaysDistrictDefinition(
                BellfoundryDistrictId,
                "Bellfoundry",
                "campaniles, civic loggias, and tide clocks",
                new Vector3(37f, 1f, -510f),
                new Vector3(51f, 1f, -502f)),
            new TidewaysDistrictDefinition(
                ArsenalDistrictId,
                "Arsenal Reach",
                "shipwright courts, ropewalks, and chain towers",
                new Vector3(99f, 1f, -542f),
                new Vector3(116f, 1f, -562f)),
            new TidewaysDistrictDefinition(
                SaltgardenDistrictId,
                "Saltgarden",
                "quiet cloisters, roof gardens, and glass conservatories",
                new Vector3(-28f, 1f, -565f),
                new Vector3(-44f, 1f, -562f))
        };

        private static readonly TidewaysDockDefinition[] DockDefinitions =
        {
            Dock(
                "dock.tideways.tideglass_customs",
                "Tideglass Customs",
                new Vector3(2f, 0.28f, -444f),
                new Vector3(-6f, 0.95f, -444f),
                new Vector3(8f, 0.3f, 7f),
                0f),
            Dock(
                "dock.tideways.north_quay",
                "North Serein Quay",
                new Vector3(4f, 0.28f, -490f),
                new Vector3(-3f, 0.95f, -490f),
                new Vector3(7f, 0.3f, 9f),
                0f),
            Dock(
                "dock.tideways.south_quay",
                "Basilica South Quay",
                new Vector3(4f, 0.28f, -586f),
                new Vector3(-3f, 0.95f, -584f),
                new Vector3(7f, 0.3f, 10f),
                0f),
            Dock(
                "dock.tideways.west_rialto",
                "West Rialto Landing",
                new Vector3(-47f, 0.28f, -536f),
                new Vector3(-47f, 0.95f, -528f),
                new Vector3(9f, 0.3f, 7f),
                90f),
            Dock(
                "dock.tideways.arsenal",
                "Arsenal Watergate",
                new Vector3(118f, 0.28f, -536f),
                new Vector3(118f, 0.95f, -528f),
                new Vector3(9f, 0.3f, 7f),
                90f),
            Dock(
                "dock.tideways.deepwater_south",
                "Deepwater Roads",
                new Vector3(5f, 0.28f, -610f),
                new Vector3(5f, 0.95f, -601f),
                new Vector3(12f, 0.3f, 8f),
                90f),
            Dock(
                "dock.tideways.deepwater_west",
                "West Sea Gate",
                new Vector3(-110f, 0.28f, -465f),
                new Vector3(-101f, 0.95f, -465f),
                new Vector3(8f, 0.3f, 12f),
                0f),
            Dock(
                "dock.tideways.deepwater_east",
                "East Sea Gate",
                new Vector3(150f, 0.28f, -465f),
                new Vector3(141f, 0.95f, -465f),
                new Vector3(8f, 0.3f, 12f),
                0f)
        };

        private static readonly TidewaysContentSocketDefinition[] ContentSocketDefinitions =
        {
            Socket(
                "quest.tideways.glasswake_manifest",
                TidewaysContentSocketKind.Quest,
                "Glasswake Manifest",
                "Read the Glasswake manifest",
                "The manifest reserves a quest berth for Tideglass convoy work.",
                new Vector3(-20f, 1.32f, -518f)),
            Socket(
                "quest.tideways.bellfoundry_compact",
                TidewaysContentSocketKind.Quest,
                "Bellfoundry Compact",
                "Inspect the Bellfoundry compact",
                "The compact reserves a civic quest handoff beneath the tide clock.",
                new Vector3(37f, 1.32f, -486f)),
            Socket(
                "enemy.tideways.arsenal_ambush",
                TidewaysContentSocketKind.Enemy,
                "Arsenal Ambush",
                "Inspect the quiet chain court",
                "The chain court is a stable enemy encounter socket with clear retreat lanes.",
                new Vector3(99f, 1.32f, -518f)),
            Socket(
                "enemy.tideways.saltgarden_intrusion",
                TidewaysContentSocketKind.Enemy,
                "Saltgarden Intrusion",
                "Inspect the disturbed salt beds",
                "The cloister garden reserves a bounded enemy socket away from the dry route.",
                new Vector3(-44f, 1.32f, -562f)),
            Socket(
                "cargo.tideways.saffron_ledger",
                TidewaysContentSocketKind.Cargo,
                "Saffron Cargo Ledger",
                "Inspect the saffron cargo marks",
                "The ledger names a stable cargo handoff beside the south quay.",
                new Vector3(-9f, 1.32f, -583f)),
            Socket(
                "cargo.tideways.arsenal_chain",
                TidewaysContentSocketKind.Cargo,
                "Arsenal Chain Cargo",
                "Inspect the chain cargo berth",
                "The Arsenal berth reserves heavy cargo without blocking its boarding edge.",
                new Vector3(108f, 1.32f, -583f)),
            Socket(
                "vendor.tideways.rialto_chandler",
                TidewaysContentSocketKind.Vendor,
                "Rialto Chandler",
                "Browse the chandler's sample board",
                "This staffed counter is ready for a vendor profile and keeps a semantic identity.",
                new Vector3(-15f, 1.32f, -516f)),
            Socket(
                "vendor.tideways.arsenal_ropewright",
                TidewaysContentSocketKind.Vendor,
                "Arsenal Ropewright",
                "Inspect the ropewright's counter",
                "This counter is the stable vendor socket for ocean-shipping supplies.",
                new Vector3(82f, 1.32f, -516f)),
            Socket(
                "activity.tideways.copper_eaves",
                TidewaysContentSocketKind.Activity,
                "Copper Eaves Start",
                "Inspect the courier bell",
                "The courier bell reserves the Copper Eaves timed parkour activity.",
                new Vector3(-48f, 1.32f, -512f)),
            Socket(
                "boss.tideways.abyssal_choir",
                TidewaysContentSocketKind.SeaBoss,
                "Abyssal Choir Anchorage",
                "Sound the silent deepwater buoy",
                "The outer anchorage is the stable sea-boss socket, clear of shipping lanes.",
                new Vector3(-145f, 0.42f, -548f))
        };

        private static readonly TidewaysShipRouteDefinition[] ShipRouteDefinitions =
        {
            new TidewaysShipRouteDefinition(
                "route.tideways.strand_courier",
                "ship.tideways.glassfin",
                "Glassfin",
                TidewaysShipClass.CourierSkiff,
                3.2f,
                5f,
                new Vector2(1.6f, 3.5f),
                0.82f,
                190f,
                225f,
                Waypoint(
                    "waypoint.tideways.glassfin.customs_departure",
                    2f,
                    -444f,
                    "dock.tideways.tideglass_customs",
                    4f),
                Waypoint("waypoint.tideways.glassfin.gate_outbound", 2f, -468f),
                Waypoint(
                    "waypoint.tideways.glassfin.north_quay",
                    4f,
                    -490f,
                    "dock.tideways.north_quay",
                    3f),
                Waypoint("waypoint.tideways.glassfin.rialto_turn", 4f, -520f),
                Waypoint("waypoint.tideways.glassfin.gate_inbound", 4f, -490f),
                Waypoint("waypoint.tideways.glassfin.customs_return", 2f, -468f)),
            new TidewaysShipRouteDefinition(
                "route.tideways.grand_serein_packet",
                "ship.tideways.morrowwake",
                "Morrowwake",
                TidewaysShipClass.CanalPacket,
                3.6f,
                19f,
                new Vector2(2f, 4.5f),
                0.9f,
                205f,
                240f,
                Waypoint(
                    "waypoint.tideways.morrowwake.north_departure",
                    4f,
                    -490f,
                    "dock.tideways.north_quay",
                    4f),
                Waypoint("waypoint.tideways.morrowwake.rialto_bend", 4f, -525f),
                Waypoint("waypoint.tideways.morrowwake.basilica_bend", 4f, -570f),
                Waypoint(
                    "waypoint.tideways.morrowwake.south_quay",
                    4f,
                    -586f,
                    "dock.tideways.south_quay",
                    5f),
                Waypoint("waypoint.tideways.morrowwake.south_return", 4f, -550f),
                Waypoint("waypoint.tideways.morrowwake.north_return", 4f, -515f)),
            new TidewaysShipRouteDefinition(
                "route.tideways.rialto_crosscurrent",
                "ship.tideways.saffron_gull",
                "Saffron Gull",
                TidewaysShipClass.CargoBarge,
                3.1f,
                27f,
                new Vector2(2.4f, 5f),
                0.92f,
                210f,
                245f,
                Waypoint(
                    "waypoint.tideways.saffron_gull.west_landing",
                    -47f,
                    -536f,
                    "dock.tideways.west_rialto",
                    5f),
                Waypoint("waypoint.tideways.saffron_gull.grand_crossing", 10f, -536f),
                Waypoint("waypoint.tideways.saffron_gull.lantern_crossing", 65f, -536f),
                Waypoint(
                    "waypoint.tideways.saffron_gull.arsenal_landing",
                    118f,
                    -536f,
                    "dock.tideways.arsenal",
                    6f),
                Waypoint("waypoint.tideways.saffron_gull.lantern_return", 65f, -536f),
                Waypoint("waypoint.tideways.saffron_gull.grand_return", 10f, -536f)),
            new TidewaysShipRouteDefinition(
                "route.tideways.outer_sea_convoy",
                "ship.tideways.crown_of_reeds",
                "Crown of Reeds",
                TidewaysShipClass.OceanGalleon,
                4.5f,
                61f,
                new Vector2(3.2f, 8.4f),
                1.05f,
                260f,
                300f,
                Waypoint(
                    "waypoint.tideways.crown.deepwater_roads",
                    5f,
                    -610f,
                    "dock.tideways.deepwater_south",
                    8f),
                Waypoint("waypoint.tideways.crown.southwest_turn", -110f, -610f),
                Waypoint(
                    "waypoint.tideways.crown.west_sea_gate",
                    -110f,
                    -465f,
                    "dock.tideways.deepwater_west",
                    7f),
                Waypoint("waypoint.tideways.crown.northwest_turn", -45f, -465f),
                Waypoint("waypoint.tideways.crown.northeast_turn", 125f, -465f),
                Waypoint(
                    "waypoint.tideways.crown.east_sea_gate",
                    150f,
                    -465f,
                    "dock.tideways.deepwater_east",
                    7f),
                Waypoint("waypoint.tideways.crown.southeast_turn", 150f, -610f),
                Waypoint("waypoint.tideways.crown.south_return", 40f, -610f))
        };

        private static readonly BuildingSpec[] BuildingDefinitions =
        {
            new BuildingSpec("building.tideways.rialto.outer_north", RialtoDistrictId,
                new Vector3(-42f, 1f, -502f), new Vector2(14f, 15f), 4.8f, 0),
            new BuildingSpec("building.tideways.rialto.inner_north", RialtoDistrictId,
                new Vector3(-14f, 1f, -502f), new Vector2(14f, 15f), 5.2f, 1),
            new BuildingSpec("building.tideways.saltgarden.outer_south", SaltgardenDistrictId,
                new Vector3(-42f, 1f, -562f), new Vector2(14f, 16f), 5f, 1),
            new BuildingSpec("building.tideways.saltgarden.inner_south", SaltgardenDistrictId,
                new Vector3(-14f, 1f, -562f), new Vector2(14f, 16f), 4.6f, 0),
            new BuildingSpec("building.tideways.bellfoundry.west_north", BellfoundryDistrictId,
                new Vector3(22f, 1f, -502f), new Vector2(14f, 15f), 5.4f, 0),
            new BuildingSpec("building.tideways.bellfoundry.east_north", BellfoundryDistrictId,
                new Vector3(51f, 1f, -502f), new Vector2(14f, 15f), 6.4f, 1),
            new BuildingSpec("building.tideways.basilica.west_south", BellfoundryDistrictId,
                new Vector3(22f, 1f, -562f), new Vector2(14f, 16f), 5.2f, 1),
            new BuildingSpec("building.tideways.basilica.east_south", BellfoundryDistrictId,
                new Vector3(51f, 1f, -562f), new Vector2(14f, 16f), 5.8f, 0),
            new BuildingSpec("building.tideways.arsenal.west_north", ArsenalDistrictId,
                new Vector3(82f, 1f, -502f), new Vector2(14f, 15f), 5.1f, 0),
            new BuildingSpec("building.tideways.arsenal.east_north", ArsenalDistrictId,
                new Vector3(115f, 1f, -502f), new Vector2(14f, 15f), 5.5f, 1),
            new BuildingSpec("building.tideways.shipyard.west_south", ArsenalDistrictId,
                new Vector3(82f, 1f, -562f), new Vector2(14f, 16f), 5.4f, 1),
            new BuildingSpec("building.tideways.shipyard.east_south", ArsenalDistrictId,
                new Vector3(115f, 1f, -562f), new Vector2(14f, 16f), 6.1f, 0)
        };

        private static readonly ResourcePlacementSpec[] ImportedPlacements =
        {
            new ResourcePlacementSpec(
                "dressing.tideways.rialto_stall",
                "Stall_Empty",
                new Vector3(-17f, 1.02f, -520f),
                90f,
                1f),
            new ResourcePlacementSpec(
                "dressing.tideways.arsenal_stall",
                "Stall_Cart_Empty",
                new Vector3(82f, 1.02f, -520f),
                -90f,
                1f),
            new ResourcePlacementSpec(
                "dressing.tideways.rialto_crates",
                "Crate_Wooden",
                new Vector3(-10f, 1.02f, -581f),
                18f,
                1f),
            new ResourcePlacementSpec(
                "dressing.tideways.arsenal_crates",
                "FarmCrate_Empty",
                new Vector3(108f, 1.02f, -581f),
                -16f,
                1f),
            new ResourcePlacementSpec(
                "dressing.tideways.north_barrel",
                "Barrel",
                new Vector3(-8f, 1.02f, -489f),
                12f,
                1f),
            new ResourcePlacementSpec(
                "dressing.tideways.south_barrel",
                "Barrel_Holder",
                new Vector3(75f, 1.02f, -581f),
                -8f,
                1f),
            new ResourcePlacementSpec(
                "dressing.tideways.saltgarden_bench",
                "Bench",
                new Vector3(-35f, 1.02f, -548f),
                90f,
                1f),
            new ResourcePlacementSpec(
                "dressing.tideways.bellfoundry_table",
                "Table_Large",
                new Vector3(45f, 1.02f, -518f),
                0f,
                1f),
            new ResourcePlacementSpec(
                "dressing.tideways.arsenal_workbench",
                "Workbench",
                new Vector3(108f, 1.02f, -548f),
                180f,
                1f),
            new ResourcePlacementSpec(
                "dressing.tideways.rialto_arch",
                "Wall_Arch",
                new Vector3(-28f, 1.02f, -524f),
                0f,
                1.05f),
            new ResourcePlacementSpec(
                "dressing.tideways.bellfoundry_arch",
                "Wall_Arch",
                new Vector3(37f, 1.02f, -524f),
                0f,
                1.05f),
            new ResourcePlacementSpec(
                "dressing.tideways.arsenal_arch",
                "Wall_Arch",
                new Vector3(99f, 1.02f, -524f),
                0f,
                1.05f)
        };

        public static IReadOnlyList<TidewaysWaterZoneDefinition> WaterZones =>
            WaterZoneDefinitions;
        public static IReadOnlyList<TidewaysWaterwayDefinition> Waterways =>
            WaterwayDefinitions;
        public static IReadOnlyList<TidewaysDryIslandDefinition> DryIslands =>
            DryIslandDefinitions;
        public static IReadOnlyList<TidewaysDryNodeDefinition> DryNodes =>
            DryNodeDefinitions;
        public static IReadOnlyList<TidewaysDryEdgeDefinition> DryEdges =>
            DryEdgeDefinitions;
        public static IReadOnlyList<TidewaysParkourRouteDefinition> ParkourRoutes =>
            ParkourDefinitions;
        public static IReadOnlyList<TidewaysDistrictDefinition> Districts =>
            DistrictDefinitions;
        public static IReadOnlyList<TidewaysDockDefinition> Docks =>
            DockDefinitions;
        public static IReadOnlyList<TidewaysContentSocketDefinition> ContentSockets =>
            ContentSocketDefinitions;
        public static IReadOnlyList<TidewaysShipRouteDefinition> ShipRoutes =>
            ShipRouteDefinitions;

        public static void Build(TetherfallGame game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            var existing = FindDirectChild(game.transform, RootName);
            if (existing != null)
            {
                if (existing.GetComponent<TetherfallTidewaysWorldRoot>() == null)
                {
                    throw new InvalidOperationException(
                        $"A direct child named '{RootName}' exists without Tideways metadata.");
                }

                return;
            }

            var root = new GameObject(RootName);
            root.transform.SetParent(game.transform, false);
            var palette = new TidewaysPalette(game);
            var ownershipTransferred = false;
            try
            {
                var context = new BuildContext(game, root.transform, palette);
                BuildOceanAndWater(context);
                BuildDryLand(context);
                BuildDryNetwork(context);
                BuildArchitecture(context);
                BuildLandmarks(context);
                BuildParkourRoutes(context);
                BuildDocks(context);
                BuildContentSockets(context);
                BuildImportedDressing(context);
                BuildShips(context);

                var streaming = root.AddComponent<TetherfallTidewaysStreaming>();
                streaming.Initialize(game, context.Clusters);

                var marker = root.AddComponent<TetherfallTidewaysWorldRoot>();
                ConfigureMetrics(
                    marker,
                    root,
                    palette.Materials,
                    context.ImportedPlacementCount,
                    context.ImportedFallbackCount);
                ValidateBuiltBudget(marker);
                ownershipTransferred = true;
            }
            catch
            {
                root.transform.SetParent(null, true);
                DestroyObject(root);
                throw;
            }
            finally
            {
                if (!ownershipTransferred)
                {
                    palette.DestroyMaterials();
                }
            }
        }

        public static bool IsBuilt(TetherfallGame game)
        {
            return game != null &&
                   FindDirectChild(game.transform, RootName)?
                       .GetComponent<TetherfallTidewaysWorldRoot>() != null;
        }

        public static TetherfallTidewaysWorldRoot GetBuiltRoot(TetherfallGame game)
        {
            return game != null
                ? FindDirectChild(game.transform, RootName)?
                    .GetComponent<TetherfallTidewaysWorldRoot>()
                : null;
        }

        public static bool IsNavigableWater(Vector3 point, float margin = 0.05f)
        {
            for (var zoneIndex = 0;
                 zoneIndex < WaterZoneDefinitions.Length;
                 zoneIndex++)
            {
                if (WaterZoneDefinitions[zoneIndex].ContainsHorizontal(point, margin))
                {
                    return true;
                }
            }

            for (var waterwayIndex = 0;
                 waterwayIndex < WaterwayDefinitions.Length;
                 waterwayIndex++)
            {
                var waterway = WaterwayDefinitions[waterwayIndex];
                for (var pointIndex = 0;
                     pointIndex + 1 < waterway.Points.Count;
                     pointIndex++)
                {
                    if (HorizontalDistanceToSegment(
                            point,
                            waterway.Points[pointIndex],
                            waterway.Points[pointIndex + 1]) <=
                        waterway.HalfWidth + margin)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void BuildOceanAndWater(BuildContext context)
        {
            CreatePrimitive(
                "Tideways sea floor",
                context.OceanCluster.transform,
                PrimitiveType.Cube,
                new Vector3(OceanSurfaceCenter.x, -3.25f, OceanSurfaceCenter.z),
                new Vector3(OceanSurfaceSize.x, 1f, OceanSurfaceSize.z),
                Quaternion.identity,
                context.Palette.Seabed,
                true,
                "collider.tideways.sea_floor");
            CreatePrimitive(
                "Tideways ocean surface",
                context.OceanCluster.transform,
                PrimitiveType.Cube,
                OceanSurfaceCenter,
                OceanSurfaceSize,
                Quaternion.identity,
                context.Palette.Water,
                false);

            for (var index = 0; index < WaterZoneDefinitions.Length; index++)
            {
                var zone = WaterZoneDefinitions[index];
                CreateWaterVolume(
                    context.GameplayRoot,
                    zone.Id,
                    zone.Center,
                    zone.Size,
                    Quaternion.identity);
            }

            for (var waterwayIndex = 0;
                 waterwayIndex < WaterwayDefinitions.Length;
                 waterwayIndex++)
            {
                var waterway = WaterwayDefinitions[waterwayIndex];
                for (var pointIndex = 0;
                     pointIndex + 1 < waterway.Points.Count;
                     pointIndex++)
                {
                    var from = waterway.Points[pointIndex];
                    var to = waterway.Points[pointIndex + 1];
                    var delta = to - from;
                    var horizontal = new Vector3(delta.x, 0f, delta.z);
                    var length = horizontal.magnitude;
                    if (length <= 0.01f)
                    {
                        continue;
                    }

                    var rotation = Quaternion.LookRotation(horizontal / length, Vector3.up);
                    var midpoint = (from + to) * 0.5f;
                    midpoint.y = WaterSurfaceY;
                    CreatePrimitive(
                        $"{waterway.DisplayName} water {pointIndex + 1}",
                        context.OceanCluster.transform,
                        PrimitiveType.Cube,
                        midpoint,
                        new Vector3(waterway.HalfWidth * 2f, 0.09f, length + 0.2f),
                        rotation,
                        context.Palette.Water,
                        false);

                    var volumeCenter = midpoint;
                    volumeCenter.y = WaterVolumeCenterY;
                    CreateWaterVolume(
                        context.GameplayRoot,
                        $"{waterway.Id}.segment.{SegmentWord(pointIndex)}",
                        volumeCenter,
                        new Vector3(
                            waterway.HalfWidth * 2f,
                            WaterVolumeHeight,
                            length + 0.2f),
                        rotation);
                }
            }
        }

        private static void BuildDryLand(BuildContext context)
        {
            for (var index = 0; index < DryIslandDefinitions.Length; index++)
            {
                var definition = DryIslandDefinitions[index];
                CreatePrimitive(
                    definition.Id,
                    context.GetDistrictCluster(definition.Center.x).transform,
                    PrimitiveType.Cube,
                    definition.Center,
                    definition.Size,
                    Quaternion.identity,
                    index % 2 == 0
                        ? context.Palette.IslandStone
                        : context.Palette.WarmStone,
                    true,
                    $"collider.{definition.Id}");
            }
        }

        private static void BuildDryNetwork(BuildContext context)
        {
            for (var index = 0; index < DryEdgeDefinitions.Length; index++)
            {
                var edge = DryEdgeDefinitions[index];
                var from = FindDryNode(edge.FromNodeId);
                var to = FindDryNode(edge.ToNodeId);
                if (from == null || to == null)
                {
                    throw new InvalidOperationException(
                        $"Dry route '{edge.Id}' references a missing endpoint.");
                }

                if (edge.Style == TidewaysDryRouteStyle.ArchedBridge)
                {
                    BuildArchedBridge(context, edge, from.Position, to.Position);
                }
                else
                {
                    CreateRouteSegment(
                        context,
                        context.SkylineCluster.transform,
                        edge.Id,
                        from.Position,
                        to.Position,
                        edge.Width,
                        context.Palette.WarmStone,
                        true);
                }
            }
        }

        private static void BuildArchedBridge(
            BuildContext context,
            TidewaysDryEdgeDefinition edge,
            Vector3 from,
            Vector3 to)
        {
            const int segmentCount = 5;
            const float archRise = 2.65f;
            var root = new GameObject(edge.Id);
            root.transform.SetParent(context.SkylineCluster.transform, false);
            root.AddComponent<TetherfallTidewaysBridgeIdentity>().Initialize(edge.Id);
            var previous = from;
            for (var index = 1; index <= segmentCount; index++)
            {
                var progress = index / (float)segmentCount;
                var current = Vector3.Lerp(from, to, progress);
                current.y += Mathf.Sin(progress * Mathf.PI) * archRise;
                var deck = CreateRouteSegment(
                    context,
                    root.transform,
                    $"{edge.Id}.deck.{SegmentWord(index - 1)}",
                    previous,
                    current,
                    edge.Width,
                    context.Palette.WarmStone,
                    true);
                BuildBridgeRails(
                    root.transform,
                    deck,
                    edge.Width,
                    context.Palette.Copper,
                    index);
                previous = current;
            }
        }

        private static void BuildBridgeRails(
            Transform parent,
            GameObject deck,
            float width,
            Material material,
            int index)
        {
            if (deck == null)
            {
                return;
            }

            var length = deck.transform.localScale.z;
            for (var side = -1; side <= 1; side += 2)
            {
                var rail = CreatePrimitive(
                    $"arched bridge rail {index} {side}",
                    parent,
                    PrimitiveType.Cube,
                    deck.transform.position +
                    deck.transform.right * side * (width * 0.48f) +
                    Vector3.up * 0.42f,
                    new Vector3(0.12f, 0.5f, length),
                    deck.transform.rotation,
                    material,
                    false);
                rail.transform.localScale = new Vector3(0.12f, 0.5f, length);
            }
        }

        private static void BuildArchitecture(BuildContext context)
        {
            for (var index = 0; index < BuildingDefinitions.Length; index++)
            {
                BuildHouse(context, BuildingDefinitions[index], index);
            }
        }

        private static void BuildHouse(
            BuildContext context,
            BuildingSpec definition,
            int index)
        {
            var parent = context.GetDistrictCluster(definition.Position.x).transform;
            var root = new GameObject(definition.Id).transform;
            root.SetParent(parent, false);
            root.position = definition.Position;
            var wallMaterial = index % 3 == 0
                ? context.Palette.PlasterGold
                : index % 3 == 1
                    ? context.Palette.PlasterRose
                    : context.Palette.WarmStone;
            var roofMaterial = definition.RoofVariant == 0
                ? context.Palette.RoofTerracotta
                : context.Palette.RoofTeal;

            CreatePrimitive(
                $"{definition.Id}.walls",
                root,
                PrimitiveType.Cube,
                definition.Position + Vector3.up * (definition.Height * 0.5f),
                new Vector3(
                    definition.Size.x,
                    definition.Height,
                    definition.Size.y),
                Quaternion.identity,
                wallMaterial,
                true,
                $"collider.{definition.Id}.shell");

            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    $"{definition.Id}.roof.{(side < 0 ? "west" : "east")}",
                    root,
                    PrimitiveType.Cube,
                    definition.Position +
                    new Vector3(
                        side * definition.Size.x * 0.23f,
                        definition.Height + 0.62f,
                        0f),
                    new Vector3(
                        definition.Size.x * 0.57f,
                        0.32f,
                        definition.Size.y + 0.7f),
                    Quaternion.Euler(0f, 0f, side * 24f),
                    roofMaterial,
                    false);
            }

            CreatePrimitive(
                $"{definition.Id}.door",
                root,
                PrimitiveType.Cube,
                definition.Position +
                new Vector3(0f, 1.15f, definition.Size.y * 0.505f),
                new Vector3(1.35f, 2.3f, 0.16f),
                Quaternion.identity,
                context.Palette.Timber,
                false);
            for (var window = 0; window < 4; window++)
            {
                var side = window < 2 ? -1f : 1f;
                var floor = window % 2;
                CreatePrimitive(
                    $"{definition.Id}.window.{window + 1}",
                    root,
                    PrimitiveType.Cube,
                    definition.Position +
                    new Vector3(
                        side * definition.Size.x * 0.28f,
                        1.8f + floor * 1.85f,
                        definition.Size.y * 0.51f),
                    new Vector3(1.15f, 1.05f, 0.12f),
                    Quaternion.identity,
                    context.Palette.Window,
                    false);
            }

            CreatePrimitive(
                $"{definition.Id}.chimney",
                root,
                PrimitiveType.Cube,
                definition.Position +
                new Vector3(
                    definition.Size.x * 0.27f,
                    definition.Height + 1.3f,
                    -definition.Size.y * 0.2f),
                new Vector3(0.75f, 2.2f, 0.75f),
                Quaternion.identity,
                context.Palette.WarmStone,
                false);
        }

        private static void BuildLandmarks(BuildContext context)
        {
            BuildRialtoLandmark(context, DistrictDefinitions[0]);
            BuildBellfoundryLandmark(context, DistrictDefinitions[1]);
            BuildArsenalLandmark(context, DistrictDefinitions[2]);
            BuildSaltgardenLandmark(context, DistrictDefinitions[3]);
        }

        private static void BuildRialtoLandmark(
            BuildContext context,
            TidewaysDistrictDefinition district)
        {
            var root = CreateLandmarkRoot(context, district);
            for (var level = 0; level < 3; level++)
            {
                CreatePrimitive(
                    $"Saffron loggia tier {level + 1}",
                    root,
                    PrimitiveType.Cube,
                    district.LandmarkPosition +
                    new Vector3(0f, 1.45f + level * 1.2f, 0f),
                    new Vector3(8f - level * 1.3f, 0.55f, 6f - level * 0.8f),
                    Quaternion.identity,
                    level == 2
                        ? context.Palette.Copper
                        : context.Palette.PlasterGold,
                    level == 0,
                    level == 0 ? "collider.tideways.rialto_loggia" : null);
            }

            CreatePrimitive(
                "Saffron loggia canopy",
                root,
                PrimitiveType.Cylinder,
                district.LandmarkPosition + Vector3.up * 5.5f,
                new Vector3(3.3f, 0.45f, 3.3f),
                Quaternion.identity,
                context.Palette.RoofTerracotta,
                false);
        }

        private static void BuildBellfoundryLandmark(
            BuildContext context,
            TidewaysDistrictDefinition district)
        {
            var root = CreateLandmarkRoot(context, district);
            CreatePrimitive(
                "Bellfoundry campanile",
                root,
                PrimitiveType.Cube,
                district.LandmarkPosition + Vector3.up * 7.5f,
                new Vector3(5.2f, 15f, 5.2f),
                Quaternion.identity,
                context.Palette.PlasterRose,
                true,
                "collider.tideways.bellfoundry_campanile",
                true);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    $"Bellfoundry belfry opening {side}",
                    root,
                    PrimitiveType.Cube,
                    district.LandmarkPosition +
                    new Vector3(side * 1.35f, 11.7f, 2.64f),
                    new Vector3(1.15f, 2.1f, 0.12f),
                    Quaternion.identity,
                    context.Palette.Window,
                    false);
            }

            CreatePrimitive(
                "Bellfoundry copper crown",
                root,
                PrimitiveType.Cylinder,
                district.LandmarkPosition + Vector3.up * 15.6f,
                new Vector3(3.5f, 0.8f, 3.5f),
                Quaternion.identity,
                context.Palette.Copper,
                false);
        }

        private static void BuildArsenalLandmark(
            BuildContext context,
            TidewaysDistrictDefinition district)
        {
            var root = CreateLandmarkRoot(context, district);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    $"Arsenal chain tower {side}",
                    root,
                    PrimitiveType.Cylinder,
                    district.LandmarkPosition +
                    new Vector3(side * 3.2f, 5.2f, 0f),
                    new Vector3(2.15f, 5.2f, 2.15f),
                    Quaternion.identity,
                    context.Palette.WarmStone,
                    true,
                    $"collider.tideways.arsenal_tower.{(side < 0 ? "west" : "east")}",
                    side < 0);
            }

            CreatePrimitive(
                "Arsenal chainhouse lintel",
                root,
                PrimitiveType.Cube,
                district.LandmarkPosition + Vector3.up * 7.4f,
                new Vector3(8f, 0.55f, 1f),
                Quaternion.identity,
                context.Palette.Copper,
                false);
            for (var link = 0; link < 5; link++)
            {
                CreatePrimitive(
                    $"Arsenal hanging chain {link + 1}",
                    root,
                    PrimitiveType.Cylinder,
                    district.LandmarkPosition +
                    new Vector3(-2.4f + link * 1.2f, 5.5f, 0f),
                    new Vector3(0.12f, 1.7f, 0.12f),
                    Quaternion.identity,
                    context.Palette.Copper,
                    false);
            }
        }

        private static void BuildSaltgardenLandmark(
            BuildContext context,
            TidewaysDistrictDefinition district)
        {
            var root = CreateLandmarkRoot(context, district);
            CreatePrimitive(
                "Saltgarden conservatory base",
                root,
                PrimitiveType.Cylinder,
                district.LandmarkPosition + Vector3.up * 1.2f,
                new Vector3(4.8f, 1.2f, 4.8f),
                Quaternion.identity,
                context.Palette.PlasterGold,
                true,
                "collider.tideways.saltgarden_conservatory");
            CreatePrimitive(
                "Saltgarden glass dome",
                root,
                PrimitiveType.Sphere,
                district.LandmarkPosition + Vector3.up * 3.35f,
                new Vector3(4.5f, 2.25f, 4.5f),
                Quaternion.identity,
                context.Palette.Window,
                false);
            for (var rib = 0; rib < 6; rib++)
            {
                var angle = rib * 60f;
                var radians = angle * Mathf.Deg2Rad;
                CreatePrimitive(
                    $"Saltgarden copper rib {rib + 1}",
                    root,
                    PrimitiveType.Cylinder,
                    district.LandmarkPosition +
                    new Vector3(
                        Mathf.Sin(radians) * 3.2f,
                        3.4f,
                        Mathf.Cos(radians) * 3.2f),
                    new Vector3(0.09f, 2.1f, 0.09f),
                    Quaternion.Euler(18f, angle, 0f),
                    context.Palette.Copper,
                    false);
            }
        }

        private static Transform CreateLandmarkRoot(
            BuildContext context,
            TidewaysDistrictDefinition district)
        {
            var root = new GameObject(
                $"{district.DisplayName} landmark · {district.Id}").transform;
            root.SetParent(context.SkylineCluster.transform, false);
            return root;
        }

        private static void BuildParkourRoutes(BuildContext context)
        {
            for (var routeIndex = 0;
                 routeIndex < ParkourDefinitions.Length;
                 routeIndex++)
            {
                var definition = ParkourDefinitions[routeIndex];
                var root = new GameObject(
                    $"{definition.DisplayName} · {definition.Id}").transform;
                root.SetParent(context.SkylineCluster.transform, false);
                for (var pointIndex = 0;
                     pointIndex < definition.Points.Count;
                     pointIndex++)
                {
                    var point = definition.Points[pointIndex];
                    CreatePrimitive(
                        $"{definition.Id}.landing.{SegmentWord(pointIndex)}",
                        root,
                        PrimitiveType.Cube,
                        point,
                        new Vector3(2.3f, 0.28f, 2.3f),
                        Quaternion.identity,
                        pointIndex % 2 == 0
                            ? context.Palette.RoofTerracotta
                            : context.Palette.RoofTeal,
                        true,
                        $"collider.{definition.Id}.landing.{SegmentWord(pointIndex)}");
                    if (pointIndex + 1 < definition.Points.Count)
                    {
                        var beam = CreateRouteSegment(
                            context,
                            root,
                            $"{definition.Id}.run.{SegmentWord(pointIndex)}",
                            point,
                            definition.Points[pointIndex + 1],
                            1.25f,
                            context.Palette.Copper,
                            true);
                        if (pointIndex == 0)
                        {
                            beam.AddComponent<ClimbableSurface>();
                        }
                    }
                }
            }
        }

        private static void BuildDocks(BuildContext context)
        {
            for (var index = 0; index < DockDefinitions.Length; index++)
            {
                var dock = DockDefinitions[index];
                var parent = context.GetDistrictCluster(dock.DeckCenter.x).transform;
                var root = new GameObject($"{dock.DisplayName} · {dock.Id}").transform;
                root.SetParent(parent, false);
                CreatePrimitive(
                    $"{dock.Id}.deck",
                    root,
                    PrimitiveType.Cube,
                    dock.DeckCenter,
                    dock.DeckSize,
                    Quaternion.Euler(0f, dock.Yaw, 0f),
                    context.Palette.Timber,
                    true,
                    $"collider.{dock.Id}.deck");
                var right = Quaternion.Euler(0f, dock.Yaw, 0f) * Vector3.right;
                for (var side = -1; side <= 1; side += 2)
                {
                    CreatePrimitive(
                        $"{dock.Id}.mooring.{(side < 0 ? "port" : "starboard")}",
                        root,
                        PrimitiveType.Cylinder,
                        dock.DeckCenter +
                        right * side * (dock.DeckSize.x * 0.42f) +
                        Vector3.up * 0.9f,
                        new Vector3(0.2f, 1.05f, 0.2f),
                        Quaternion.identity,
                        context.Palette.Copper,
                        false);
                }
            }
        }

        private static void BuildContentSockets(BuildContext context)
        {
            for (var index = 0; index < ContentSocketDefinitions.Length; index++)
            {
                var definition = ContentSocketDefinitions[index];
                var parent = context.GetDistrictCluster(definition.Position.x).transform;
                var root = new GameObject(
                    $"{definition.DisplayName} socket · {definition.Id}");
                root.transform.SetParent(parent, false);
                root.transform.position = definition.Position;
                var socket = root.AddComponent<TetherfallTidewaysContentSocket>();
                socket.Initialize(context.Game, definition);
                context.Game.RegisterInteractable(socket);

                var primitive = definition.Kind == TidewaysContentSocketKind.SeaBoss
                    ? PrimitiveType.Cylinder
                    : definition.Kind == TidewaysContentSocketKind.Cargo
                        ? PrimitiveType.Cube
                        : PrimitiveType.Sphere;
                CreatePrimitive(
                    $"{definition.Id}.marker",
                    root.transform,
                    primitive,
                    definition.Position + Vector3.up * 0.42f,
                    definition.Kind == TidewaysContentSocketKind.SeaBoss
                        ? new Vector3(0.48f, 1.2f, 0.48f)
                        : Vector3.one * 0.42f,
                    Quaternion.identity,
                    definition.Kind == TidewaysContentSocketKind.Enemy ||
                    definition.Kind == TidewaysContentSocketKind.SeaBoss
                        ? context.Palette.RoofTerracotta
                        : context.Palette.Window,
                    false);
            }
        }

        private static void BuildImportedDressing(BuildContext context)
        {
            for (var index = 0; index < ImportedPlacements.Length; index++)
            {
                var placement = ImportedPlacements[index];
                var parent =
                    context.GetDistrictCluster(placement.Position.x).transform;
                if (TryBuildImportedPlacement(parent, placement))
                {
                    context.ImportedPlacementCount++;
                    continue;
                }

                var fallback = CreatePrimitive(
                    $"{placement.Id}.procedural_fallback",
                    parent,
                    index % 3 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube,
                    placement.Position + Vector3.up * 0.42f,
                    index % 3 == 0
                        ? new Vector3(0.42f, 0.55f, 0.42f)
                        : new Vector3(0.8f, 0.8f, 0.8f),
                    Quaternion.Euler(0f, placement.Yaw, 0f),
                    index % 2 == 0
                        ? context.Palette.Timber
                        : context.Palette.WarmStone,
                    false);
                fallback.AddComponent<TetherfallTidewaysImportedIdentity>().Initialize(
                    placement.Id,
                    placement.ResourceName,
                    true);
                context.ImportedPlacementCount++;
                context.ImportedFallbackCount++;
            }
        }

        private static bool TryBuildImportedPlacement(
            Transform parent,
            ResourcePlacementSpec placement)
        {
            var prefab = Resources.Load<GameObject>(
                TetherfallQuaterniusAssetCatalog.ResourcePrefix +
                placement.ResourceName);
            if (prefab == null)
            {
                return false;
            }

            var instance = Object.Instantiate(prefab, parent, false);
            instance.name = $"{placement.Id}.quaternius.{placement.ResourceName}";
            instance.transform.position = placement.Position;
            instance.transform.rotation = Quaternion.Euler(0f, placement.Yaw, 0f);
            instance.transform.localScale = Vector3.one * placement.Scale;
            StripImportedComponents(instance);
            if (instance.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                DestroyObject(instance);
                return false;
            }

            instance.AddComponent<TetherfallTidewaysImportedIdentity>().Initialize(
                placement.Id,
                placement.ResourceName,
                false);
            return true;
        }

        private static void BuildShips(BuildContext context)
        {
            for (var index = 0; index < ShipRouteDefinitions.Length; index++)
            {
                var route = ShipRouteDefinitions[index];
                var root = new GameObject($"{route.DisplayName} · {route.ShipId}");
                root.transform.SetParent(context.ShipRoot, false);
                var ship = root.AddComponent<TetherfallTidewaysShip>();
                var visualRoot = new GameObject("Ship visuals").transform;
                visualRoot.SetParent(root.transform, false);
                BuildShipVisual(context, route, visualRoot, index);

                var deck = root.AddComponent<BoxCollider>();
                deck.center = new Vector3(
                    0f,
                    route.DeckSurfaceY - 0.12f,
                    0f);
                deck.size = new Vector3(
                    route.DeckHalfExtents.x * 2f,
                    0.24f,
                    route.DeckHalfExtents.y * 2f);
                root.AddComponent<TetherfallTidewaysDeliberateCollider>().Initialize(
                    $"collider.{route.ShipId}.deck",
                    true);
                ship.Initialize(context.Game, route, visualRoot, deck);
            }
        }

        private static void BuildShipVisual(
            BuildContext context,
            TidewaysShipRouteDefinition route,
            Transform parent,
            int index)
        {
            var half = route.DeckHalfExtents;
            var hullMaterial = index % 2 == 0
                ? context.Palette.Hull
                : context.Palette.Timber;
            CreateLocalPrimitive(
                $"{route.ShipId}.hull",
                parent,
                PrimitiveType.Cube,
                new Vector3(0f, 0.18f, 0f),
                new Vector3(half.x * 2f + 0.55f, 0.78f, half.y * 2f + 0.8f),
                Quaternion.identity,
                hullMaterial);
            CreateLocalPrimitive(
                $"{route.ShipId}.deck",
                parent,
                PrimitiveType.Cube,
                new Vector3(0f, route.DeckSurfaceY - 0.17f, 0f),
                new Vector3(half.x * 2f, 0.28f, half.y * 2f),
                Quaternion.identity,
                context.Palette.WarmStone);
            for (var side = -1; side <= 1; side += 2)
            {
                CreateLocalPrimitive(
                    $"{route.ShipId}.rail.{(side < 0 ? "port" : "starboard")}",
                    parent,
                    PrimitiveType.Cube,
                    new Vector3(
                        side * (half.x + 0.02f),
                        route.DeckSurfaceY + 0.36f,
                        0f),
                    new Vector3(0.12f, 0.58f, half.y * 2f),
                    Quaternion.identity,
                    context.Palette.Copper);
            }

            CreateLocalPrimitive(
                $"{route.ShipId}.prow",
                parent,
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.42f, half.y + 0.55f),
                new Vector3(half.x * 0.72f, 0.46f, half.x * 0.72f),
                Quaternion.Euler(90f, 0f, 0f),
                hullMaterial);

            if (route.ShipClass == TidewaysShipClass.CourierSkiff)
            {
                CreateLocalPrimitive(
                    $"{route.ShipId}.awning",
                    parent,
                    PrimitiveType.Cube,
                    new Vector3(0f, route.DeckSurfaceY + 1.3f, -0.7f),
                    new Vector3(half.x * 1.5f, 0.14f, 2.4f),
                    Quaternion.Euler(0f, 0f, 4f),
                    context.Palette.Canvas);
                return;
            }

            var mastHeight =
                route.ShipClass == TidewaysShipClass.OceanGalleon ? 8.2f : 4.8f;
            CreateLocalPrimitive(
                $"{route.ShipId}.mast",
                parent,
                PrimitiveType.Cylinder,
                new Vector3(0f, route.DeckSurfaceY + mastHeight * 0.5f, 0f),
                new Vector3(0.16f, mastHeight * 0.5f, 0.16f),
                Quaternion.identity,
                context.Palette.Timber);
            CreateLocalPrimitive(
                $"{route.ShipId}.yard",
                parent,
                PrimitiveType.Cube,
                new Vector3(0f, route.DeckSurfaceY + mastHeight * 0.72f, 0f),
                new Vector3(half.x * 2.3f, 0.14f, 0.14f),
                Quaternion.identity,
                context.Palette.Timber);
            CreateLocalPrimitive(
                $"{route.ShipId}.sail",
                parent,
                PrimitiveType.Cube,
                new Vector3(0f, route.DeckSurfaceY + mastHeight * 0.5f, 0.08f),
                new Vector3(
                    half.x * 1.85f,
                    mastHeight * 0.42f,
                    0.08f),
                Quaternion.Euler(0f, 0f, -4f),
                context.Palette.Canvas);

            if (route.ShipClass != TidewaysShipClass.OceanGalleon)
            {
                return;
            }

            CreateLocalPrimitive(
                $"{route.ShipId}.sterncastle",
                parent,
                PrimitiveType.Cube,
                new Vector3(0f, route.DeckSurfaceY + 1f, -half.y * 0.62f),
                new Vector3(half.x * 1.7f, 1.8f, 3.2f),
                Quaternion.identity,
                context.Palette.PlasterGold);
            for (var side = -1; side <= 1; side += 2)
            {
                CreateLocalPrimitive(
                    $"{route.ShipId}.lantern_tower.{side}",
                    parent,
                    PrimitiveType.Cylinder,
                    new Vector3(
                        side * half.x * 0.72f,
                        route.DeckSurfaceY + 1.8f,
                        -half.y * 0.7f),
                    new Vector3(0.28f, 1.25f, 0.28f),
                    Quaternion.identity,
                    context.Palette.Copper);
            }
        }

        private static void ConfigureMetrics(
            TetherfallTidewaysWorldRoot marker,
            GameObject root,
            Material[] ownedMaterials,
            int importedPlacementCount,
            int importedFallbackCount)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var colliders = root.GetComponentsInChildren<Collider>(true);
            var materials = new HashSet<Material>();
            for (var rendererIndex = 0;
                 rendererIndex < renderers.Length;
                 rendererIndex++)
            {
                var shared = renderers[rendererIndex].sharedMaterials;
                for (var materialIndex = 0;
                     materialIndex < shared.Length;
                     materialIndex++)
                {
                    if (shared[materialIndex] != null)
                    {
                        materials.Add(shared[materialIndex]);
                    }
                }
            }

            marker.Configure(
                WorldId,
                renderers.Length,
                colliders.Length,
                materials.Count,
                root.GetComponentsInChildren<Light>(true).Length,
                root.GetComponentsInChildren<WaterVolume>(true).Length,
                root.GetComponentsInChildren<TetherfallTidewaysShip>(true).Length,
                root.GetComponentsInChildren<TetherfallTidewaysContentSocket>(true).Length,
                importedPlacementCount,
                importedFallbackCount,
                root.GetComponentsInChildren<TetherfallTidewaysShip>(true).Length +
                root.GetComponentsInChildren<TetherfallTidewaysStreaming>(true).Length,
                ownedMaterials);
        }

        private static void ValidateBuiltBudget(TetherfallTidewaysWorldRoot marker)
        {
            if (marker.RendererCount > MaxRendererCount ||
                marker.ColliderCount > MaxDeliberateColliderCount ||
                marker.SharedMaterialCount > MaxSharedMaterialCount ||
                marker.LightCount != AddedRealtimeLightCount ||
                marker.WaterVolumeCount > MaxWaterVolumeCount ||
                marker.ShipCount != ExpectedShipCount ||
                marker.ContentSocketCount != ExpectedContentSocketCount ||
                marker.UpdateDriverCount > MaxActiveUpdateDrivers)
            {
                throw new InvalidOperationException(
                    "Tideways build exceeded its published runtime budget: " +
                    $"{marker.RendererCount}/{MaxRendererCount} renderers, " +
                    $"{marker.ColliderCount}/{MaxDeliberateColliderCount} colliders, " +
                    $"{marker.SharedMaterialCount}/{MaxSharedMaterialCount} shared materials, " +
                    $"{marker.LightCount}/{AddedRealtimeLightCount} lights, " +
                    $"{marker.WaterVolumeCount}/{MaxWaterVolumeCount} water volumes, " +
                    $"{marker.UpdateDriverCount}/{MaxActiveUpdateDrivers} update drivers.");
            }
        }

        private static GameObject CreateRouteSegment(
            BuildContext context,
            Transform parent,
            string id,
            Vector3 from,
            Vector3 to,
            float width,
            Material material,
            bool solid)
        {
            var delta = to - from;
            var length = delta.magnitude;
            if (length <= 0.01f)
            {
                throw new InvalidOperationException(
                    $"Tideways route segment '{id}' has zero length.");
            }

            return CreatePrimitive(
                id,
                parent,
                PrimitiveType.Cube,
                (from + to) * 0.5f,
                new Vector3(width, 0.28f, length),
                Quaternion.LookRotation(delta / length, Vector3.up),
                material,
                solid,
                solid ? $"collider.{id}" : null);
        }

        private static GameObject CreatePrimitive(
            string name,
            Transform parent,
            PrimitiveType primitive,
            Vector3 worldPosition,
            Vector3 scale,
            Quaternion rotation,
            Material material,
            bool solid,
            string colliderId = null,
            bool climbable = false)
        {
            var instance = GameObject.CreatePrimitive(primitive);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(worldPosition, rotation);
            instance.transform.localScale = scale;
            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            var collider = instance.GetComponent<Collider>();
            if (!solid)
            {
                DestroyObject(collider);
            }
            else
            {
                instance.AddComponent<TetherfallTidewaysDeliberateCollider>().Initialize(
                    colliderId ?? $"collider.{name}",
                    false);
                if (climbable)
                {
                    instance.AddComponent<ClimbableSurface>();
                }
            }

            return instance;
        }

        private static GameObject CreateLocalPrimitive(
            string name,
            Transform parent,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 scale,
            Quaternion localRotation,
            Material material)
        {
            var instance = GameObject.CreatePrimitive(primitive);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = scale;
            instance.GetComponent<Renderer>().sharedMaterial = material;
            DestroyObject(instance.GetComponent<Collider>());
            return instance;
        }

        private static void CreateWaterVolume(
            Transform parent,
            string id,
            Vector3 center,
            Vector3 size,
            Quaternion rotation)
        {
            var volume = WaterVolume.Create(center, size);
            volume.name = $"Swimmable · {id}";
            volume.transform.SetParent(parent, true);
            volume.transform.rotation = rotation;
            volume.gameObject
                .AddComponent<TetherfallTidewaysWaterVolumeIdentity>()
                .Initialize(id);
        }

        private static TidewaysDryNodeDefinition FindDryNode(string id)
        {
            for (var index = 0; index < DryNodeDefinitions.Length; index++)
            {
                if (string.Equals(
                        DryNodeDefinitions[index].Id,
                        id,
                        StringComparison.Ordinal))
                {
                    return DryNodeDefinitions[index];
                }
            }

            return null;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child != null &&
                    string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static float HorizontalDistanceToSegment(
            Vector3 point,
            Vector3 start,
            Vector3 end)
        {
            var segment = new Vector2(end.x - start.x, end.z - start.z);
            var fromStart = new Vector2(point.x - start.x, point.z - start.z);
            var squaredLength = segment.sqrMagnitude;
            if (squaredLength <= 0.0001f)
            {
                return fromStart.magnitude;
            }

            var progress = Mathf.Clamp01(Vector2.Dot(fromStart, segment) / squaredLength);
            return (fromStart - segment * progress).magnitude;
        }

        private static void StripImportedComponents(GameObject instance)
        {
            var colliders = instance.GetComponentsInChildren<Collider>(true);
            for (var index = 0; index < colliders.Length; index++)
            {
                DestroyObject(colliders[index]);
            }

            var rigidbodies = instance.GetComponentsInChildren<Rigidbody>(true);
            for (var index = 0; index < rigidbodies.Length; index++)
            {
                DestroyObject(rigidbodies[index]);
            }

            var lights = instance.GetComponentsInChildren<Light>(true);
            for (var index = 0; index < lights.Length; index++)
            {
                DestroyObject(lights[index]);
            }

            var cameras = instance.GetComponentsInChildren<Camera>(true);
            for (var index = 0; index < cameras.Length; index++)
            {
                DestroyObject(cameras[index]);
            }

            var animators = instance.GetComponentsInChildren<Animator>(true);
            for (var index = 0; index < animators.Length; index++)
            {
                DestroyObject(animators[index]);
            }

            var animations = instance.GetComponentsInChildren<Animation>(true);
            for (var index = 0; index < animations.Length; index++)
            {
                DestroyObject(animations[index]);
            }
        }

        private static TidewaysDryEdgeDefinition DryEdge(
            string id,
            string from,
            string to,
            TidewaysDryRouteStyle style)
        {
            return new TidewaysDryEdgeDefinition(
                id,
                from,
                to,
                style,
                style == TidewaysDryRouteStyle.ArchedBridge ? 3f : 3.4f);
        }

        private static TidewaysDockDefinition Dock(
            string id,
            string displayName,
            Vector3 routePoint,
            Vector3 deckCenter,
            Vector3 deckSize,
            float yaw)
        {
            return new TidewaysDockDefinition(
                id,
                displayName,
                routePoint,
                deckCenter,
                deckSize,
                yaw);
        }

        private static TidewaysContentSocketDefinition Socket(
            string id,
            TidewaysContentSocketKind kind,
            string displayName,
            string prompt,
            string interactionText,
            Vector3 position)
        {
            return new TidewaysContentSocketDefinition(
                id,
                kind,
                displayName,
                prompt,
                interactionText,
                position);
        }

        private static TidewaysShipWaypointDefinition Waypoint(
            string id,
            float x,
            float z,
            string dockId = "",
            float dockSeconds = 0f)
        {
            return new TidewaysShipWaypointDefinition(
                id,
                new Vector3(x, 0.28f, z),
                dockId,
                dockSeconds);
        }

        private static string SegmentWord(int index)
        {
            switch (index)
            {
                case 0:
                    return "alpha";
                case 1:
                    return "bravo";
                case 2:
                    return "charlie";
                case 3:
                    return "delta";
                case 4:
                    return "echo";
                case 5:
                    return "foxtrot";
                case 6:
                    return "golf";
                case 7:
                    return "harbor";
                default:
                    return "continuation";
            }
        }

        internal static void DestroyObject(Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(value);
            }
            else
            {
                Object.DestroyImmediate(value);
            }
        }

        private sealed class BuildContext
        {
            public readonly TetherfallGame Game;
            public readonly Transform GameplayRoot;
            public readonly Transform ShipRoot;
            public readonly TidewaysPalette Palette;
            public readonly List<TetherfallTidewaysVisualCluster> Clusters =
                new List<TetherfallTidewaysVisualCluster>(5);
            public readonly TetherfallTidewaysVisualCluster OceanCluster;
            public readonly TetherfallTidewaysVisualCluster WestCluster;
            public readonly TetherfallTidewaysVisualCluster CentralCluster;
            public readonly TetherfallTidewaysVisualCluster EastCluster;
            public readonly TetherfallTidewaysVisualCluster SkylineCluster;

            public int ImportedPlacementCount;
            public int ImportedFallbackCount;

            public BuildContext(
                TetherfallGame game,
                Transform root,
                TidewaysPalette palette)
            {
                Game = game;
                Palette = palette;
                var visualRoot = new GameObject(VisualRootName).transform;
                visualRoot.SetParent(root, false);
                GameplayRoot = new GameObject(GameplayRootName).transform;
                GameplayRoot.SetParent(root, false);
                ShipRoot = new GameObject(ShipRootName).transform;
                ShipRoot.SetParent(root, false);

                OceanCluster = CreateCluster(
                    visualRoot,
                    "cluster.tideways.ocean",
                    "Ocean and connected waterways",
                    new Vector3(0f, 0f, -540f),
                    410f);
                WestCluster = CreateCluster(
                    visualRoot,
                    "cluster.tideways.west",
                    "Saffron Quays and Saltgarden",
                    new Vector3(-28f, 1f, -535f),
                    205f);
                CentralCluster = CreateCluster(
                    visualRoot,
                    "cluster.tideways.central",
                    "Bellfoundry and Basilica",
                    new Vector3(36f, 1f, -535f),
                    205f);
                EastCluster = CreateCluster(
                    visualRoot,
                    "cluster.tideways.east",
                    "Arsenal Reach",
                    new Vector3(99f, 1f, -535f),
                    215f);
                SkylineCluster = CreateCluster(
                    visualRoot,
                    "cluster.tideways.skyline",
                    "Bridges towers and parkour skyline",
                    new Vector3(36f, 7f, -535f),
                    245f);
            }

            public TetherfallTidewaysVisualCluster GetDistrictCluster(float x)
            {
                return x < 6f ? WestCluster : x < 70f ? CentralCluster : EastCluster;
            }

            private TetherfallTidewaysVisualCluster CreateCluster(
                Transform parent,
                string id,
                string displayName,
                Vector3 center,
                float visibleDistance)
            {
                var root = new GameObject($"{displayName} · {id}");
                root.transform.SetParent(parent, false);
                var cluster = root.AddComponent<TetherfallTidewaysVisualCluster>();
                cluster.Initialize(id, center, visibleDistance);
                Clusters.Add(cluster);
                return cluster;
            }
        }

        private sealed class TidewaysPalette
        {
            public readonly Material Water;
            public readonly Material Seabed;
            public readonly Material IslandStone;
            public readonly Material WarmStone;
            public readonly Material PlasterGold;
            public readonly Material PlasterRose;
            public readonly Material Timber;
            public readonly Material Copper;
            public readonly Material RoofTeal;
            public readonly Material RoofTerracotta;
            public readonly Material Canvas;
            public readonly Material Window;
            public readonly Material Hull;
            public readonly Material[] Materials;

            public TidewaysPalette(TetherfallGame game)
            {
                Water = game.CreateTransparentMaterial(
                    new Color(0.035f, 0.29f, 0.4f, 0.72f),
                    0.78f);
                Seabed = game.CreateMaterial(
                    new Color(0.025f, 0.075f, 0.1f),
                    0f,
                    0.18f);
                IslandStone = game.CreateMaterial(
                    new Color(0.31f, 0.34f, 0.31f),
                    0.02f,
                    0.3f);
                WarmStone = game.CreateMaterial(
                    new Color(0.56f, 0.49f, 0.38f),
                    0.02f,
                    0.34f);
                PlasterGold = game.CreateMaterial(
                    new Color(0.76f, 0.61f, 0.38f),
                    0f,
                    0.31f);
                PlasterRose = game.CreateMaterial(
                    new Color(0.66f, 0.37f, 0.32f),
                    0f,
                    0.3f);
                Timber = game.CreateMaterial(
                    new Color(0.25f, 0.14f, 0.09f),
                    0f,
                    0.24f);
                Copper = game.CreateMaterial(
                    new Color(0.45f, 0.64f, 0.58f),
                    0.58f,
                    0.56f);
                RoofTeal = game.CreateMaterial(
                    new Color(0.13f, 0.4f, 0.43f),
                    0.08f,
                    0.42f);
                RoofTerracotta = game.CreateMaterial(
                    new Color(0.61f, 0.2f, 0.12f),
                    0.02f,
                    0.33f);
                Canvas = game.CreateMaterial(
                    new Color(0.88f, 0.73f, 0.45f),
                    0f,
                    0.38f);
                Window = game.CreateMaterial(
                    new Color(0.28f, 0.77f, 0.82f),
                    0.16f,
                    0.76f);
                Hull = game.CreateMaterial(
                    new Color(0.065f, 0.12f, 0.16f),
                    0.04f,
                    0.3f);
                Materials = new[]
                {
                    Water,
                    Seabed,
                    IslandStone,
                    WarmStone,
                    PlasterGold,
                    PlasterRose,
                    Timber,
                    Copper,
                    RoofTeal,
                    RoofTerracotta,
                    Canvas,
                    Window,
                    Hull
                };

                for (var index = 0; index < Materials.Length; index++)
                {
                    if (Materials[index] == null)
                    {
                        throw new InvalidOperationException(
                            "Tideways could not create its bounded shared-material palette.");
                    }

                    Materials[index].name = $"Tideways shared material {index + 1}";
                }
            }

            public void DestroyMaterials()
            {
                for (var index = 0; index < Materials.Length; index++)
                {
                    DestroyObject(Materials[index]);
                }
            }
        }

        private readonly struct BuildingSpec
        {
            public readonly string Id;
            public readonly string DistrictId;
            public readonly Vector3 Position;
            public readonly Vector2 Size;
            public readonly float Height;
            public readonly int RoofVariant;

            public BuildingSpec(
                string id,
                string districtId,
                Vector3 position,
                Vector2 size,
                float height,
                int roofVariant)
            {
                Id = id;
                DistrictId = districtId;
                Position = position;
                Size = size;
                Height = height;
                RoofVariant = roofVariant;
            }
        }

        private readonly struct ResourcePlacementSpec
        {
            public readonly string Id;
            public readonly string ResourceName;
            public readonly Vector3 Position;
            public readonly float Yaw;
            public readonly float Scale;

            public ResourcePlacementSpec(
                string id,
                string resourceName,
                Vector3 position,
                float yaw,
                float scale)
            {
                Id = id;
                ResourceName = resourceName;
                Position = position;
                Yaw = yaw;
                Scale = scale;
            }
        }
    }
}
