using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Tetherfall
{
    public enum TetherfallEmbercrownSocketKind
    {
        Resource,
        Encounter
    }

    public enum TetherfallEmbercrownSafeSurfaceKind
    {
        Bridge,
        Island
    }

    public enum TetherfallEmbercrownTraversalKind
    {
        Bridge,
        Island,
        Climb,
        WindLift,
        GliderLaunch
    }

    public sealed class TetherfallEmbercrownLavaChannelDefinition
    {
        private readonly IReadOnlyList<Vector3> points;

        public string Id { get; }
        public string DisplayName { get; }
        public float Width { get; }
        public Vector3 SafeEjectionPoint { get; }
        public IReadOnlyList<Vector3> Points => points;

        public TetherfallEmbercrownLavaChannelDefinition(
            string id,
            string displayName,
            float width,
            Vector3 safeEjectionPoint,
            params Vector3[] channelPoints)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Width = width;
            SafeEjectionPoint = safeEjectionPoint;
            points = Array.AsReadOnly(
                channelPoints == null ? Array.Empty<Vector3>() : (Vector3[])channelPoints.Clone());
        }
    }

    public sealed class TetherfallEmbercrownSafeRouteDefinition
    {
        private readonly IReadOnlyList<Vector3> points;

        public string Id { get; }
        public string DisplayName { get; }
        public float MinimumWidth { get; }
        public bool IsAlternate { get; }
        public IReadOnlyList<Vector3> Points => points;

        public TetherfallEmbercrownSafeRouteDefinition(
            string id,
            string displayName,
            float minimumWidth,
            bool isAlternate,
            params Vector3[] routePoints)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            MinimumWidth = minimumWidth;
            IsAlternate = isAlternate;
            points = Array.AsReadOnly(
                routePoints == null ? Array.Empty<Vector3>() : (Vector3[])routePoints.Clone());
        }
    }

    public sealed class TetherfallEmbercrownSafeSurfaceDefinition
    {
        public string Id { get; }
        public TetherfallEmbercrownSafeSurfaceKind Kind { get; }
        public Vector3 Center { get; }
        public Vector2 Size { get; }
        public float YawDegrees { get; }

        public TetherfallEmbercrownSafeSurfaceDefinition(
            string id,
            TetherfallEmbercrownSafeSurfaceKind kind,
            Vector3 center,
            Vector2 size,
            float yawDegrees)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            Center = center;
            Size = size;
            YawDegrees = yawDegrees;
        }

        public bool ContainsHorizontal(Vector3 worldPosition, float margin = 0f)
        {
            var delta = worldPosition - Center;
            var radians = -YawDegrees * Mathf.Deg2Rad;
            var cosine = Mathf.Cos(radians);
            var sine = Mathf.Sin(radians);
            var localX = delta.x * cosine - delta.z * sine;
            var localZ = delta.x * sine + delta.z * cosine;
            var safeMargin = Mathf.Max(0f, margin);
            return Mathf.Abs(localX) <= Size.x * 0.5f + safeMargin &&
                   Mathf.Abs(localZ) <= Size.y * 0.5f + safeMargin;
        }
    }

    public sealed class TetherfallEmbercrownSocketDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public TetherfallEmbercrownSocketKind Kind { get; }
        public Vector3 Position { get; }
        public float YawDegrees { get; }
        public ResourceKind Resource { get; }
        public string EncounterId { get; }

        public TetherfallEmbercrownSocketDefinition(
            string id,
            string displayName,
            TetherfallEmbercrownSocketKind kind,
            Vector3 position,
            float yawDegrees,
            ResourceKind resource,
            string encounterId)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Kind = kind;
            Position = position;
            YawDegrees = yawDegrees;
            Resource = resource;
            EncounterId = encounterId ?? string.Empty;
        }
    }

    public sealed class TetherfallEmbercrownTraversalDefinition
    {
        public string Id { get; }
        public TetherfallEmbercrownTraversalKind Kind { get; }
        public Vector3 Position { get; }
        public Vector3 SafeLandingPoint { get; }

        public TetherfallEmbercrownTraversalDefinition(
            string id,
            TetherfallEmbercrownTraversalKind kind,
            Vector3 position,
            Vector3 safeLandingPoint)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            Position = position;
            SafeLandingPoint = safeLandingPoint;
        }
    }

    public readonly struct TetherfallEmbercrownWorldBudget
    {
        public readonly int Renderers;
        public readonly int Colliders;
        public readonly int SharedMaterials;
        public readonly int Lights;
        public readonly int ParticleSystems;
        public readonly int UpdateDrivers;
        public readonly int TotalParticleCapacity;
        public readonly int ProximityGatedRoots;

        public TetherfallEmbercrownWorldBudget(
            int renderers,
            int colliders,
            int sharedMaterials,
            int lights,
            int particleSystems,
            int updateDrivers,
            int totalParticleCapacity,
            int proximityGatedRoots)
        {
            Renderers = renderers;
            Colliders = colliders;
            SharedMaterials = sharedMaterials;
            Lights = lights;
            ParticleSystems = particleSystems;
            UpdateDrivers = updateDrivers;
            TotalParticleCapacity = totalParticleCapacity;
            ProximityGatedRoots = proximityGatedRoots;
        }
    }

    public readonly struct TetherfallEmbercrownWorldMetrics
    {
        public readonly int Renderers;
        public readonly int Colliders;
        public readonly int SharedMaterials;
        public readonly int Lights;
        public readonly int ParticleSystems;
        public readonly int UpdateDrivers;
        public readonly int TotalParticleCapacity;
        public readonly int ImportedPlacements;
        public readonly int ProceduralFallbackPlacements;

        public TetherfallEmbercrownWorldMetrics(
            int renderers,
            int colliders,
            int sharedMaterials,
            int lights,
            int particleSystems,
            int updateDrivers,
            int totalParticleCapacity,
            int importedPlacements,
            int proceduralFallbackPlacements)
        {
            Renderers = renderers;
            Colliders = colliders;
            SharedMaterials = sharedMaterials;
            Lights = lights;
            ParticleSystems = particleSystems;
            UpdateDrivers = updateDrivers;
            TotalParticleCapacity = totalParticleCapacity;
            ImportedPlacements = importedPlacements;
            ProceduralFallbackPlacements = proceduralFallbackPlacements;
        }
    }

    /// <summary>
    /// Additive, self-contained volcanic world slice. Nothing invokes Build automatically:
    /// the owning bootstrap decides when Embercrown joins the live world.
    /// </summary>
    public static class TetherfallEmbercrownWorld
    {
        public const string RootName = "Embercrown Caldera World";
        public const string RegionId = "region.embercrown.caldera";
        public const string RegionDisplayName = "Embercrown Caldera";

        public const int MaxRendererCount = 180;
        public const int MaxColliderCount = 64;
        public const int MaxSharedMaterialCount = 12;
        public const int MaxLightCount = 1;
        public const int MaxParticleSystemCount = 2;
        public const int MaxUpdateDriverCount = 2;
        public const int EmberParticleCapacity = 42;
        public const int HeatShimmerParticleCapacity = 18;
        public const int MaxTotalParticleCapacity =
            EmberParticleCapacity + HeatShimmerParticleCapacity;
        public const int ProximityGatedRootCount = 3;
        public const int MaxImportedRenderersPerPlacement = 2;

        public const float ActivationPollSeconds = 0.25f;
        public const float TerrainPrewakeDistance = 105f;
        public const float DetailPrewakeDistance = 52f;
        public const float ForgeActivationDistance = 118f;
        public const float ActivationHysteresis = 28f;

        public const int LavaDamagePerTick = 8;
        public const float LavaDamageTickSeconds = 0.75f;
        public const int LavaTicksBeforeEjection = 4;
        public const float LavaHazardPollSeconds = 0.1f;
        public const float LavaReentryGraceSeconds = 1.35f;
        public const int MaximumContinuousLavaDamage =
            LavaDamagePerTick * LavaTicksBeforeEjection;

        private static readonly Vector3 RegionCenterValue = new Vector3(-455f, 0f, -410f);
        private static readonly Vector3 CalderaCenterValue =
            new Vector3(-490f, 0f, -365f);
        private static readonly Vector3 SafeArrivalValue =
            new Vector3(-335f, 0.34f, -368f);
        private static readonly Vector3 ForgeCenterValue =
            new Vector3(-525f, 0f, -372f);

        private static readonly IReadOnlyList<TetherfallEmbercrownLavaChannelDefinition>
            LavaChannelCatalog = Array.AsReadOnly(CreateLavaChannels());
        private static readonly IReadOnlyList<TetherfallEmbercrownSafeRouteDefinition>
            SafeRouteCatalog = Array.AsReadOnly(CreateSafeRoutes());
        private static readonly IReadOnlyList<TetherfallEmbercrownSafeSurfaceDefinition>
            SafeSurfaceCatalog = Array.AsReadOnly(CreateSafeSurfaces());
        private static readonly IReadOnlyList<TetherfallEmbercrownSocketDefinition>
            SocketCatalog = Array.AsReadOnly(CreateSockets());
        private static readonly IReadOnlyList<TetherfallEmbercrownTraversalDefinition>
            TraversalCatalog = Array.AsReadOnly(CreateTraversals());

        public static Vector3 RegionCenter => RegionCenterValue;
        public static Vector3 CalderaCenter => CalderaCenterValue;
        public static Vector3 SafeArrival => SafeArrivalValue;
        public static Vector3 ForgeCenter => ForgeCenterValue;
        public static Rect WorldBounds => new Rect(-580f, -535f, 250f, 250f);
        public static IReadOnlyList<TetherfallEmbercrownLavaChannelDefinition> LavaChannels =>
            LavaChannelCatalog;
        public static IReadOnlyList<TetherfallEmbercrownSafeRouteDefinition> SafeRoutes =>
            SafeRouteCatalog;
        public static IReadOnlyList<TetherfallEmbercrownSafeSurfaceDefinition> SafeSurfaces =>
            SafeSurfaceCatalog;
        public static IReadOnlyList<TetherfallEmbercrownSocketDefinition> Sockets =>
            SocketCatalog;
        public static IReadOnlyList<TetherfallEmbercrownTraversalDefinition> Traversal =>
            TraversalCatalog;

        public static TetherfallEmbercrownWorldBudget BudgetLimits =>
            new TetherfallEmbercrownWorldBudget(
                MaxRendererCount,
                MaxColliderCount,
                MaxSharedMaterialCount,
                MaxLightCount,
                MaxParticleSystemCount,
                MaxUpdateDriverCount,
                MaxTotalParticleCapacity,
                ProximityGatedRootCount);

        /// <summary>
        /// Idempotent integration entrypoint. Repeated calls for the same game return the same root.
        /// </summary>
        public static GameObject Build(TetherfallGame game)
        {
            return BuildInternal(game, false);
        }

        /// <summary>
        /// Deterministic verification/low-resource path using only procedural art.
        /// </summary>
        public static GameObject BuildProceduralFallback(TetherfallGame game)
        {
            return BuildInternal(game, true);
        }

        public static bool Cleanup(TetherfallGame game)
        {
            if (game == null)
            {
                return false;
            }

            var existing = FindDirectChild(game.transform, RootName);
            if (existing == null)
            {
                return false;
            }

            DestroyObject(existing.gameObject);
            return true;
        }

        public static bool TryGetSocket(
            string stableId,
            out TetherfallEmbercrownSocketDefinition definition)
        {
            if (!string.IsNullOrEmpty(stableId))
            {
                for (var index = 0; index < SocketCatalog.Count; index++)
                {
                    var candidate = SocketCatalog[index];
                    if (string.Equals(candidate.Id, stableId, StringComparison.Ordinal))
                    {
                        definition = candidate;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }

        public static int GetLavaTickCountForExposure(float exposureSeconds)
        {
            if (!IsFinite(exposureSeconds) || exposureSeconds < LavaDamageTickSeconds)
            {
                return 0;
            }

            return Mathf.Clamp(
                Mathf.FloorToInt(
                    (exposureSeconds + 0.0001f) / LavaDamageTickSeconds),
                0,
                LavaTicksBeforeEjection);
        }

        public static int GetBoundedLavaDamage(float exposureSeconds)
        {
            return GetLavaTickCountForExposure(exposureSeconds) * LavaDamagePerTick;
        }

        public static bool ShouldEjectAfterLavaTicks(int appliedTicks)
        {
            return appliedTicks >= LavaTicksBeforeEjection;
        }

        public static bool IsInsideRawLava(Vector3 worldPosition)
        {
            for (var channelIndex = 0;
                 channelIndex < LavaChannelCatalog.Count;
                 channelIndex++)
            {
                var channel = LavaChannelCatalog[channelIndex];
                var halfWidthSquared = channel.Width * channel.Width * 0.25f;
                for (var pointIndex = 0;
                     pointIndex < channel.Points.Count - 1;
                     pointIndex++)
                {
                    if (HorizontalDistanceSquaredToSegment(
                            worldPosition,
                            channel.Points[pointIndex],
                            channel.Points[pointIndex + 1]) <= halfWidthSquared)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsProtectedByTraversalSurface(Vector3 worldPosition)
        {
            for (var index = 0; index < SafeSurfaceCatalog.Count; index++)
            {
                if (SafeSurfaceCatalog[index].ContainsHorizontal(worldPosition, 0.08f))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsHazardousPoint(Vector3 worldPosition)
        {
            return IsInsideRawLava(worldPosition) &&
                   !IsProtectedByTraversalSurface(worldPosition);
        }

        public static bool TryGetSafeEjectionPoint(
            Vector3 worldPosition,
            out Vector3 safePoint)
        {
            var bestDistanceSquared = float.PositiveInfinity;
            safePoint = SafeArrivalValue;
            var found = false;
            for (var channelIndex = 0;
                 channelIndex < LavaChannelCatalog.Count;
                 channelIndex++)
            {
                var channel = LavaChannelCatalog[channelIndex];
                for (var pointIndex = 0;
                     pointIndex < channel.Points.Count - 1;
                     pointIndex++)
                {
                    var distanceSquared = HorizontalDistanceSquaredToSegment(
                        worldPosition,
                        channel.Points[pointIndex],
                        channel.Points[pointIndex + 1]);
                    if (distanceSquared < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        safePoint = channel.SafeEjectionPoint;
                        found = true;
                    }
                }
            }

            return found;
        }

        public static float HorizontalDistanceToWorldBounds(Vector3 worldPosition)
        {
            var bounds = WorldBounds;
            var deltaX = worldPosition.x < bounds.xMin
                ? bounds.xMin - worldPosition.x
                : worldPosition.x > bounds.xMax
                    ? worldPosition.x - bounds.xMax
                    : 0f;
            var deltaZ = worldPosition.z < bounds.yMin
                ? bounds.yMin - worldPosition.z
                : worldPosition.z > bounds.yMax
                    ? worldPosition.z - bounds.yMax
                    : 0f;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        public static TetherfallEmbercrownWorldMetrics InspectBuiltWorld(GameObject root)
        {
            if (root == null)
            {
                return default;
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var colliders = root.GetComponentsInChildren<Collider>(true);
            var lights = root.GetComponentsInChildren<Light>(true);
            var particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
            var materials = new HashSet<Material>();
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (renderer == null)
                {
                    continue;
                }

                var sharedMaterials = renderer.sharedMaterials;
                for (var materialIndex = 0;
                     materialIndex < sharedMaterials.Length;
                     materialIndex++)
                {
                    if (sharedMaterials[materialIndex] != null)
                    {
                        materials.Add(sharedMaterials[materialIndex]);
                    }
                }
            }

            var particleCapacity = 0;
            for (var index = 0; index < particleSystems.Length; index++)
            {
                if (particleSystems[index] != null)
                {
                    particleCapacity += particleSystems[index].main.maxParticles;
                }
            }

            var updateDrivers =
                root.GetComponentsInChildren<TetherfallEmbercrownProximityDirector>(true).Length +
                root.GetComponentsInChildren<TetherfallEmbercrownLavaHazardDirector>(true).Length;
            return new TetherfallEmbercrownWorldMetrics(
                renderers.Length,
                colliders.Length,
                materials.Count,
                lights.Length,
                particleSystems.Length,
                updateDrivers,
                particleCapacity,
                root.GetComponentsInChildren<TetherfallEmbercrownImportedArt>(true).Length,
                root.GetComponentsInChildren<TetherfallEmbercrownProceduralFallback>(true).Length);
        }

        private static GameObject BuildInternal(TetherfallGame game, bool forceProceduralFallback)
        {
            if (game == null)
            {
                Debug.LogWarning("Embercrown build deferred because TetherfallGame is not ready.");
                return null;
            }

            var existing = FindDirectChild(game.transform, RootName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var root = new GameObject(RootName);
            root.transform.SetParent(game.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            try
            {
                var palette = EmbercrownPalette.Create(game);
                var silhouetteRoot = CreateArtRoot(root.transform, "Always On Caldera Silhouette");
                var connectorRoot = CreateGroup(root.transform, "Always On Supported Connector");
                var connectorArt = CreateArtRoot(connectorRoot, "Connector Art");
                var connectorColliders = CreateCollisionRoot(connectorRoot, "Connector Colliders");
                var terrainRoot = CreateGroup(root.transform, "Near Terrain Activation");
                var terrainArt = CreateArtRoot(terrainRoot, "Terrain Art");
                var terrainColliders = CreateCollisionRoot(terrainRoot, "Terrain Colliders");
                var detailRoot = CreateGroup(root.transform, "Near Detail Activation");
                var detailArt = CreateArtRoot(detailRoot, "Detail Art");
                var detailVfx = CreateArtRoot(detailRoot, "Bounded Ember And Heat VFX");
                var socketRoot = CreateGroup(detailRoot, "Stable Gameplay Sockets");
                var forgeRoot = CreateGroup(root.transform, "Forge Route Activation");
                var forgeArt = CreateArtRoot(forgeRoot, "Crownforge Art");
                var forgeColliders = CreateCollisionRoot(forgeRoot, "Crownforge Colliders");

                BuildCalderaSilhouette(silhouetteRoot, palette);
                BuildConnector(connectorArt, connectorColliders, palette);

                var hazardVolumes =
                    new List<TetherfallEmbercrownLavaHazardVolume>(16);
                BuildTerrain(
                    terrainArt,
                    terrainColliders,
                    hazardVolumes,
                    palette);
                BuildDetails(
                    detailArt,
                    detailVfx,
                    socketRoot,
                    palette,
                    forceProceduralFallback);
                BuildForge(
                    forgeArt,
                    forgeColliders,
                    palette,
                    forceProceduralFallback);

                var hazardDirector =
                    terrainRoot.gameObject.AddComponent<TetherfallEmbercrownLavaHazardDirector>();
                hazardDirector.Initialize(game, hazardVolumes.ToArray());

                var proximityDirector =
                    root.AddComponent<TetherfallEmbercrownProximityDirector>();
                proximityDirector.Initialize(
                    game,
                    terrainRoot.gameObject,
                    detailRoot.gameObject,
                    forgeRoot.gameObject);

                var marker = root.AddComponent<TetherfallEmbercrownWorldRoot>();
                marker.Initialize(
                    silhouetteRoot.gameObject,
                    connectorRoot.gameObject,
                    terrainRoot.gameObject,
                    detailRoot.gameObject,
                    forgeRoot.gameObject,
                    forceProceduralFallback);
                var metrics = InspectBuiltWorld(root);
                marker.SetMetrics(metrics);

                if (ExceedsBudget(metrics))
                {
                    Debug.LogWarning(
                        $"Embercrown built above a published budget: {metrics.Renderers}/" +
                        $"{MaxRendererCount} renderers, {metrics.Colliders}/{MaxColliderCount} " +
                        $"colliders, {metrics.SharedMaterials}/{MaxSharedMaterialCount} materials, " +
                        $"{metrics.Lights}/{MaxLightCount} lights, " +
                        $"{metrics.TotalParticleCapacity}/{MaxTotalParticleCapacity} particles.",
                        root);
                }
                else
                {
                    Debug.Log(
                        $"Embercrown ready: four contiguous lava channels, " +
                        $"{SocketCatalog.Count} stable sockets, {metrics.Renderers} renderers, " +
                        $"{metrics.Colliders} separated colliders, {metrics.Lights} bounded light, " +
                        $"{metrics.UpdateDrivers} update drivers.",
                        root);
                }

                return root;
            }
            catch
            {
                DestroyObject(root);
                throw;
            }
        }

        private static void BuildCalderaSilhouette(
            Transform parent,
            EmbercrownPalette palette)
        {
            for (var index = 0; index < 10; index++)
            {
                var angle = index * 36f;
                var radians = angle * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
                var scale = new Vector3(
                    36f + index % 3 * 4f,
                    22f + index % 4 * 3.5f,
                    31f + (index + 1) % 3 * 4f);
                CreateArtPrimitive(
                    parent,
                    $"Embercrown outer faceted skirt {index + 1}",
                    PrimitiveType.Sphere,
                    CalderaCenterValue +
                    direction * (48f + index % 2 * 3f) +
                    Vector3.up * (13f + index % 3 * 2f),
                    scale,
                    new Vector3(index % 2 * 4f, angle, index % 3 * 2f),
                    index % 3 == 0 ? palette.BasaltHighlight : palette.Basalt);
            }

            for (var index = 0; index < 10; index++)
            {
                var angle = index * 36f + 18f;
                var radians = angle * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
                CreateArtPrimitive(
                    parent,
                    $"Embercrown jagged caldera rim {index + 1}",
                    PrimitiveType.Cylinder,
                    CalderaCenterValue +
                    direction * 34f +
                    Vector3.up * (37f + index % 4 * 4f),
                    new Vector3(
                        14f + index % 2 * 2.5f,
                        20f + index % 3 * 3.5f,
                        17f + (index + 1) % 2 * 2.5f),
                    new Vector3(index % 2 == 0 ? -5f : 4f, -angle, index % 3 - 1f),
                    index % 4 == 0 ? palette.Obsidian : palette.Basalt);
            }

            CreateArtPrimitive(
                parent,
                "Embercrown visible molten caldera",
                PrimitiveType.Cylinder,
                CalderaCenterValue + Vector3.up * 37.2f,
                new Vector3(22f, 0.34f, 22f),
                Vector3.zero,
                palette.Lava);

            for (var index = 0; index < 4; index++)
            {
                var angle = (index * 77f + 20f) * Mathf.Deg2Rad;
                var start =
                    CalderaCenterValue +
                    new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 31f +
                    Vector3.up * (33f + index * 1.8f);
                var end =
                    CalderaCenterValue +
                    new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (58f + index * 3f) +
                    Vector3.up * (5f + index);
                CreateArtBeam(
                    parent,
                    $"Embercrown distant lava scar {index + 1}",
                    start,
                    end,
                    2.1f + index * 0.24f,
                    0.22f,
                    palette.Lava);
            }

            for (var index = 0; index < 3; index++)
            {
                CreateArtPrimitive(
                    parent,
                    $"Embercrown static ash plume {index + 1}",
                    PrimitiveType.Sphere,
                    CalderaCenterValue +
                    new Vector3(index * 3.5f - 2f, 75f + index * 22f, index * 2f),
                    new Vector3(22f + index * 10f, 17f + index * 8f, 19f + index * 9f),
                    new Vector3(index * 9f, index * 31f, index * 4f),
                    palette.Smoke);
            }
        }

        private static void BuildConnector(
            Transform artRoot,
            Transform colliderRoot,
            EmbercrownPalette palette)
        {
            var start = new Vector3(-290f, 0.04f, -380f);
            var end = SafeArrivalValue;
            CreateHorizontalSegmentArt(
                artRoot,
                "Emberstep-to-Embercrown supported span",
                start,
                end,
                8f,
                0.42f,
                -0.16f,
                palette.Obsidian);
            CreateHorizontalSegmentCollider(
                colliderRoot,
                "Embercrown connector support collider",
                start,
                end,
                7.6f,
                0.42f,
                -0.16f,
                false);

            var delta = end - start;
            delta.y = 0f;
            var length = delta.magnitude;
            var forward = length > 0.001f ? delta / length : Vector3.right;
            var perpendicular = new Vector3(-forward.z, 0f, forward.x);
            for (var side = -1; side <= 1; side += 2)
            {
                CreateHorizontalSegmentArt(
                    artRoot,
                    $"Embercrown connector copper rail {side}",
                    start + perpendicular * 3.45f + Vector3.up * 0.62f,
                    end + perpendicular * 3.45f + Vector3.up * 0.62f,
                    0.18f,
                    0.72f,
                    0f,
                    palette.Copper);
            }

            CreateArtPrimitive(
                artRoot,
                "Embercrown safe arrival plate",
                PrimitiveType.Cube,
                SafeArrivalValue + Vector3.down * 0.18f,
                new Vector3(12f, 0.36f, 12f),
                Vector3.zero,
                palette.BasaltHighlight);
            CreateColliderBox(
                colliderRoot,
                "Embercrown safe arrival collider",
                SafeArrivalValue + Vector3.down * 0.18f,
                new Vector3(11.6f, 0.36f, 11.6f),
                Vector3.zero,
                false,
                false);
        }

        private static void BuildTerrain(
            Transform artRoot,
            Transform colliderRoot,
            List<TetherfallEmbercrownLavaHazardVolume> hazardVolumes,
            EmbercrownPalette palette)
        {
            CreateArtPrimitive(
                artRoot,
                "Embercrown ash plain",
                PrimitiveType.Cube,
                RegionCenterValue + Vector3.down,
                new Vector3(250f, 2f, 250f),
                Vector3.zero,
                palette.AshGround);
            CreateColliderBox(
                colliderRoot,
                "Embercrown ash plain collider",
                RegionCenterValue + Vector3.down,
                new Vector3(250f, 2f, 250f),
                Vector3.zero,
                false,
                false);

            var ridgePositions = new[]
            {
                Local(-105f, 4f, 75f),
                Local(-102f, 6f, 10f),
                Local(-93f, 5f, -55f),
                Local(84f, 4f, 88f),
                Local(101f, 5f, -72f),
                Local(-35f, 4f, -108f)
            };
            for (var index = 0; index < ridgePositions.Length; index++)
            {
                CreateArtPrimitive(
                    artRoot,
                    $"Embercrown perimeter basalt shelf {index + 1}",
                    PrimitiveType.Cube,
                    ridgePositions[index],
                    new Vector3(34f + index % 2 * 8f, 8f + index % 3 * 2f, 18f),
                    new Vector3(index % 2 * 4f, index * 31f, index % 3 - 1f),
                    index % 2 == 0 ? palette.Basalt : palette.Obsidian);
            }

            BuildCalderaGameplayColliders(colliderRoot);
            BuildLavaChannels(artRoot, colliderRoot, hazardVolumes, palette);
            BuildSafeSurfaces(artRoot, colliderRoot, palette);
            BuildOpenWorldTraversal(artRoot, colliderRoot, palette);
        }

        private static void BuildCalderaGameplayColliders(Transform colliderRoot)
        {
            for (var index = 0; index < 8; index++)
            {
                if (index == 5)
                {
                    continue;
                }

                var angle = index * 45f;
                var radians = angle * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
                CreateColliderBox(
                    colliderRoot,
                    $"Embercrown simplified caldera mass {index + 1}",
                    CalderaCenterValue + direction * 43f + Vector3.up * 11f,
                    new Vector3(24f, 22f, 20f),
                    new Vector3(0f, angle, 0f),
                    false,
                    false);
            }
        }

        private static void BuildLavaChannels(
            Transform artRoot,
            Transform colliderRoot,
            List<TetherfallEmbercrownLavaHazardVolume> hazardVolumes,
            EmbercrownPalette palette)
        {
            for (var channelIndex = 0;
                 channelIndex < LavaChannelCatalog.Count;
                 channelIndex++)
            {
                var channel = LavaChannelCatalog[channelIndex];
                for (var pointIndex = 0;
                     pointIndex < channel.Points.Count - 1;
                     pointIndex++)
                {
                    var start = channel.Points[pointIndex];
                    var end = channel.Points[pointIndex + 1];
                    CreateHorizontalSegmentArt(
                        artRoot,
                        $"{channel.DisplayName} cooling crust {pointIndex + 1}",
                        start,
                        end,
                        channel.Width + 2.2f,
                        0.1f,
                        0.015f,
                        palette.HotCrust,
                        2.2f);
                    CreateHorizontalSegmentArt(
                        artRoot,
                        $"{channel.DisplayName} molten channel {pointIndex + 1}",
                        start,
                        end,
                        channel.Width * 0.82f,
                        0.13f,
                        0.095f,
                        palette.Lava,
                        2.4f);

                    var volume = CreateLavaHazardVolume(
                        colliderRoot,
                        channel,
                        pointIndex,
                        start,
                        end,
                        channel.Width * 0.76f);
                    hazardVolumes.Add(volume);
                }
            }
        }

        private static TetherfallEmbercrownLavaHazardVolume CreateLavaHazardVolume(
            Transform parent,
            TetherfallEmbercrownLavaChannelDefinition channel,
            int segmentIndex,
            Vector3 start,
            Vector3 end,
            float width)
        {
            var delta = end - start;
            delta.y = 0f;
            var length = Mathf.Max(0.1f, delta.magnitude + 2.1f);
            var center = (start + end) * 0.5f + Vector3.up * 0.18f;
            var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            var root = new GameObject($"{channel.Id}.hazard.{segmentIndex + 1:00}");
            root.transform.SetParent(parent, false);
            root.transform.position = center;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(width, 1.8f, length);
            var volume = root.AddComponent<TetherfallEmbercrownLavaHazardVolume>();
            volume.Initialize(
                $"{channel.Id}.hazard.{segmentIndex + 1:00}",
                channel.Id,
                center,
                new Vector3(width, 1.8f, length),
                yaw,
                channel.SafeEjectionPoint);
            return volume;
        }

        private static void BuildSafeSurfaces(
            Transform artRoot,
            Transform colliderRoot,
            EmbercrownPalette palette)
        {
            for (var index = 0; index < SafeSurfaceCatalog.Count; index++)
            {
                var surface = SafeSurfaceCatalog[index];
                if (surface.Kind == TetherfallEmbercrownSafeSurfaceKind.Bridge)
                {
                    CreateArtPrimitive(
                        artRoot,
                        $"{surface.Id} basalt bridge deck",
                        PrimitiveType.Cube,
                        surface.Center,
                        new Vector3(surface.Size.x, 0.32f, surface.Size.y),
                        new Vector3(0f, surface.YawDegrees, 0f),
                        palette.BasaltHighlight);
                    var radians = surface.YawDegrees * Mathf.Deg2Rad;
                    var localRight = new Vector3(Mathf.Cos(radians), 0f, -Mathf.Sin(radians));
                    for (var side = -1; side <= 1; side += 2)
                    {
                        CreateArtPrimitive(
                            artRoot,
                            $"{surface.Id} copper guard {side}",
                            PrimitiveType.Cube,
                            surface.Center +
                            localRight * (surface.Size.x * 0.5f - 0.22f) +
                            Vector3.up * 0.46f,
                            new Vector3(0.16f, 0.62f, surface.Size.y),
                            new Vector3(0f, surface.YawDegrees, 0f),
                            palette.Copper);
                    }

                    var collider = CreateColliderBox(
                        colliderRoot,
                        $"{surface.Id} gameplay collider",
                        surface.Center,
                        new Vector3(surface.Size.x - 0.3f, 0.32f, surface.Size.y - 0.2f),
                        new Vector3(0f, surface.YawDegrees, 0f),
                        false,
                        false);
                    collider.AddComponent<TetherfallEmbercrownTraversalAffordance>().Initialize(
                        surface.Id,
                        TetherfallEmbercrownTraversalKind.Bridge);
                }
                else
                {
                    CreateArtPrimitive(
                        artRoot,
                        $"{surface.Id} basalt refuge island",
                        PrimitiveType.Cylinder,
                        surface.Center,
                        new Vector3(surface.Size.x, 0.14f, surface.Size.y),
                        new Vector3(0f, surface.YawDegrees, 0f),
                        palette.Obsidian);
                    CreateArtPrimitive(
                        artRoot,
                        $"{surface.Id} emberglass island crown",
                        PrimitiveType.Cylinder,
                        surface.Center + Vector3.up * 0.17f,
                        new Vector3(surface.Size.x * 0.68f, 0.035f, surface.Size.y * 0.68f),
                        new Vector3(0f, surface.YawDegrees, 0f),
                        palette.BasaltHighlight);
                    var collider = CreateColliderBox(
                        colliderRoot,
                        $"{surface.Id} gameplay collider",
                        surface.Center,
                        new Vector3(surface.Size.x * 0.82f, 0.28f, surface.Size.y * 0.82f),
                        new Vector3(0f, surface.YawDegrees, 0f),
                        false,
                        false);
                    collider.AddComponent<TetherfallEmbercrownTraversalAffordance>().Initialize(
                        surface.Id,
                        TetherfallEmbercrownTraversalKind.Island);
                }
            }
        }

        private static void BuildOpenWorldTraversal(
            Transform artRoot,
            Transform colliderRoot,
            EmbercrownPalette palette)
        {
            var climbPositions = new[]
            {
                Local(-5f, 7f, 72f),
                Local(-78f, 6f, 62f)
            };
            var climbIds = new[]
            {
                "traversal.embercrown.climb.cinder-rib",
                "traversal.embercrown.climb.obsidian-rib"
            };
            for (var index = 0; index < climbPositions.Length; index++)
            {
                var yaw = index == 0 ? -18f : 22f;
                CreateArtPrimitive(
                    artRoot,
                    $"{climbIds[index]} visible handhold face",
                    PrimitiveType.Cube,
                    climbPositions[index],
                    new Vector3(7f, 14f, 2.2f),
                    new Vector3(0f, yaw, index == 0 ? -4f : 4f),
                    palette.BasaltHighlight);
                var climb = CreateColliderBox(
                    colliderRoot,
                    $"{climbIds[index]} climb collider",
                    climbPositions[index],
                    new Vector3(6.6f, 13.8f, 1.8f),
                    new Vector3(0f, yaw, 0f),
                    false,
                    true);
                climb.AddComponent<TetherfallEmbercrownTraversalAffordance>().Initialize(
                    climbIds[index],
                    TetherfallEmbercrownTraversalKind.Climb);
            }

            var liftPosition = Local(12f, 1.25f, 75f);
            CreateArtPrimitive(
                artRoot,
                "traversal.embercrown.lift.crownwind visible vent",
                PrimitiveType.Cylinder,
                liftPosition + Vector3.down * 1.05f,
                new Vector3(4.8f, 0.12f, 4.8f),
                Vector3.zero,
                palette.Teal);
            var liftRoot = new GameObject("traversal.embercrown.lift.crownwind");
            liftRoot.transform.SetParent(colliderRoot, false);
            liftRoot.transform.position = liftPosition;
            var liftCollider = liftRoot.AddComponent<CapsuleCollider>();
            liftCollider.isTrigger = true;
            liftCollider.radius = 2.4f;
            liftCollider.height = 4.8f;
            liftRoot.AddComponent<WindLift>().Initialize(8.2f);
            liftRoot.AddComponent<TetherfallEmbercrownTraversalAffordance>().Initialize(
                "traversal.embercrown.lift.crownwind",
                TetherfallEmbercrownTraversalKind.WindLift);

            var launchPosition = Local(2f, 3.2f, 88f);
            CreateArtPrimitive(
                artRoot,
                "traversal.embercrown.launch.obsidian-lip",
                PrimitiveType.Cube,
                launchPosition,
                new Vector3(10f, 0.42f, 7f),
                new Vector3(0f, -12f, 0f),
                palette.Obsidian);
            var launch = CreateColliderBox(
                colliderRoot,
                "traversal.embercrown.launch.obsidian-lip collider",
                launchPosition,
                new Vector3(9.6f, 0.42f, 6.6f),
                new Vector3(0f, -12f, 0f),
                false,
                false);
            launch.AddComponent<TetherfallEmbercrownTraversalAffordance>().Initialize(
                "traversal.embercrown.launch.obsidian-lip",
                TetherfallEmbercrownTraversalKind.GliderLaunch);
        }

        private static void BuildDetails(
            Transform artRoot,
            Transform vfxRoot,
            Transform socketRoot,
            EmbercrownPalette palette,
            bool forceProceduralFallback)
        {
            BuildRouteInlays(artRoot, palette);
            BuildSocketMarkers(artRoot, socketRoot, palette);

            var placements = new[]
            {
                new ImportedPlacement(
                    "Art/Quaternius/Rock_Medium_1",
                    "Quaternius ember shelf rock",
                    Local(58f, 0.5f, 54f),
                    new Vector3(3.4f, 2.8f, 3.1f),
                    31f,
                    PrimitiveType.Cube,
                    palette.Basalt),
                new ImportedPlacement(
                    "Art/Quaternius/Rock_Medium_2",
                    "Quaternius obsidian spur",
                    Local(88f, 0.4f, -28f),
                    new Vector3(3.1f, 2.5f, 3.1f),
                    112f,
                    PrimitiveType.Cube,
                    palette.Obsidian),
                new ImportedPlacement(
                    "Art/Quaternius/Rock_Medium_3",
                    "Quaternius caldera talus",
                    Local(-74f, 0.5f, -18f),
                    new Vector3(3.5f, 2.8f, 3.2f),
                    208f,
                    PrimitiveType.Sphere,
                    palette.BasaltHighlight),
                new ImportedPlacement(
                    "Art/KenneyNature/rock_tallA",
                    "Kenney tall cinder rock",
                    Local(97f, 0f, 62f),
                    new Vector3(2.7f, 3.1f, 2.7f),
                    18f,
                    PrimitiveType.Cylinder,
                    palette.Basalt),
                new ImportedPlacement(
                    "Art/KenneyNature/rock_largeA",
                    "Kenney large ash rock",
                    Local(-93f, 0f, -64f),
                    new Vector3(2.8f, 2.4f, 2.8f),
                    167f,
                    PrimitiveType.Sphere,
                    palette.Obsidian),
                new ImportedPlacement(
                    "Art/KenneyNature/rock_smallA",
                    "Kenney bridgehead rock",
                    Local(64f, 0f, -78f),
                    new Vector3(2.2f, 1.8f, 2.2f),
                    284f,
                    PrimitiveType.Cube,
                    palette.BasaltHighlight)
            };
            for (var index = 0; index < placements.Length; index++)
            {
                CreatePreferredArt(artRoot, placements[index], forceProceduralFallback);
            }

            CreateEmberParticles(vfxRoot, palette.ParticleMaterial);
            CreateHeatShimmer(vfxRoot, palette.ParticleMaterial);
        }

        private static void BuildRouteInlays(
            Transform parent,
            EmbercrownPalette palette)
        {
            for (var routeIndex = 0; routeIndex < SafeRouteCatalog.Count; routeIndex++)
            {
                var route = SafeRouteCatalog[routeIndex];
                for (var pointIndex = 0;
                     pointIndex < route.Points.Count - 1;
                     pointIndex++)
                {
                    CreateArtBeam(
                        parent,
                        $"{route.Id} copper route inlay {pointIndex + 1}",
                        route.Points[pointIndex] + Vector3.up * 0.045f,
                        route.Points[pointIndex + 1] + Vector3.up * 0.045f,
                        route.IsAlternate ? 0.36f : 0.48f,
                        0.055f,
                        route.IsAlternate ? palette.Teal : palette.Copper);
                }
            }

            var waymarks = new[]
            {
                SafeArrivalValue + new Vector3(-5f, 1.2f, 4f),
                Local(74f, 1.25f, 22f),
                Local(22f, 1.25f, -43f),
                Local(-58f, 1.25f, 11f)
            };
            for (var index = 0; index < waymarks.Length; index++)
            {
                CreateArtPrimitive(
                    parent,
                    $"Embercrown route fork waymark {index + 1}",
                    PrimitiveType.Cylinder,
                    waymarks[index],
                    new Vector3(0.18f, 1.25f, 0.18f),
                    Vector3.zero,
                    index == 2 ? palette.Copper : palette.Teal);
            }
        }

        private static void BuildSocketMarkers(
            Transform artRoot,
            Transform socketRoot,
            EmbercrownPalette palette)
        {
            for (var index = 0; index < SocketCatalog.Count; index++)
            {
                var definition = SocketCatalog[index];
                var socket = new GameObject(definition.Id);
                socket.transform.SetParent(socketRoot, false);
                socket.transform.position = definition.Position;
                socket.transform.rotation = Quaternion.Euler(0f, definition.YawDegrees, 0f);
                socket.AddComponent<TetherfallEmbercrownWorldSocket>().Initialize(definition);

                CreateArtPrimitive(
                    artRoot,
                    $"{definition.Id} semantic marker",
                    PrimitiveType.Cylinder,
                    definition.Position + Vector3.up * 0.05f,
                    new Vector3(1.6f, 0.035f, 1.6f),
                    new Vector3(0f, definition.YawDegrees, 0f),
                    definition.Kind == TetherfallEmbercrownSocketKind.Resource
                        ? palette.Copper
                        : palette.Teal);
            }
        }

        private static void BuildForge(
            Transform artRoot,
            Transform colliderRoot,
            EmbercrownPalette palette,
            bool forceProceduralFallback)
        {
            var cavePath = new[]
            {
                Local(-62f, 0.08f, 10f),
                Local(-70f, 0.1f, 18f),
                Local(-77f, 0.12f, 29f),
                Local(-70f, 0.14f, 38f)
            };
            for (var index = 0; index < cavePath.Length - 1; index++)
            {
                BuildTunnelSegment(
                    artRoot,
                    colliderRoot,
                    cavePath[index],
                    cavePath[index + 1],
                    index,
                    palette);
            }

            var chamberCenter = Local(-70f, 0.1f, 40f);
            CreateArtPrimitive(
                artRoot,
                "Crownforge chamber floor",
                PrimitiveType.Cylinder,
                chamberCenter,
                new Vector3(12f, 0.12f, 12f),
                Vector3.zero,
                palette.Obsidian);
            CreateColliderBox(
                colliderRoot,
                "Crownforge chamber floor collider",
                chamberCenter,
                new Vector3(11.2f, 0.24f, 11.2f),
                Vector3.zero,
                false,
                false);

            var wallOffsets = new[]
            {
                new Vector3(-6f, 2.8f, 0f),
                new Vector3(6f, 2.8f, 0f),
                new Vector3(0f, 2.8f, 6f)
            };
            var wallScales = new[]
            {
                new Vector3(0.8f, 5.6f, 12f),
                new Vector3(0.8f, 5.6f, 12f),
                new Vector3(12f, 5.6f, 0.8f)
            };
            for (var index = 0; index < wallOffsets.Length; index++)
            {
                CreateArtPrimitive(
                    artRoot,
                    $"Crownforge chamber basalt wall {index + 1}",
                    PrimitiveType.Cube,
                    chamberCenter + wallOffsets[index],
                    wallScales[index],
                    Vector3.zero,
                    palette.Basalt);
                CreateColliderBox(
                    colliderRoot,
                    $"Crownforge chamber wall collider {index + 1}",
                    chamberCenter + wallOffsets[index],
                    wallScales[index],
                    Vector3.zero,
                    false,
                    false);
            }

            var daisPosition = chamberCenter + new Vector3(0f, 0.22f, 1.2f);
            CreateArtPrimitive(
                artRoot,
                "Crownforge quenched dais",
                PrimitiveType.Cube,
                daisPosition,
                new Vector3(5.8f, 0.44f, 4.8f),
                Vector3.zero,
                palette.BasaltHighlight);
            CreateColliderBox(
                colliderRoot,
                "Crownforge dais collider",
                daisPosition,
                new Vector3(5.6f, 0.44f, 4.6f),
                Vector3.zero,
                false,
                false);

            CreateArtPrimitive(
                artRoot,
                "Crownforge guarded lava trough",
                PrimitiveType.Cube,
                chamberCenter + new Vector3(0f, 0.28f, 4.4f),
                new Vector3(5.4f, 0.46f, 1.4f),
                Vector3.zero,
                palette.HotCrust);
            CreateArtPrimitive(
                artRoot,
                "Crownforge guarded molten pour",
                PrimitiveType.Cube,
                chamberCenter + new Vector3(0f, 0.54f, 4.4f),
                new Vector3(4.5f, 0.12f, 0.72f),
                Vector3.zero,
                palette.Lava);
            CreateColliderBox(
                colliderRoot,
                "Crownforge trough safety rail",
                chamberCenter + new Vector3(0f, 0.9f, 3.45f),
                new Vector3(6f, 1.6f, 0.24f),
                Vector3.zero,
                false,
                false);

            CreatePreferredArt(
                artRoot,
                new ImportedPlacement(
                    "Art/Quaternius/Workbench",
                    "Quaternius Crownforge workbench",
                    chamberCenter + new Vector3(-2.6f, 0.25f, -1f),
                    new Vector3(1.35f, 1.35f, 1.35f),
                    18f,
                    PrimitiveType.Cube,
                    palette.Copper),
                forceProceduralFallback);
            CreatePreferredArt(
                artRoot,
                new ImportedPlacement(
                    "Art/Quaternius/Cauldron",
                    "Quaternius Crownforge quench cauldron",
                    chamberCenter + new Vector3(2.4f, 0.25f, -0.8f),
                    new Vector3(1.3f, 1.3f, 1.3f),
                    -24f,
                    PrimitiveType.Cylinder,
                    palette.Obsidian),
                forceProceduralFallback);

            var chimneyPosition = chamberCenter + new Vector3(-5.45f, 3.1f, 3.2f);
            CreateArtPrimitive(
                artRoot,
                "traversal.embercrown.climb.forge-chimney",
                PrimitiveType.Cube,
                chimneyPosition,
                new Vector3(0.7f, 6.2f, 4.2f),
                Vector3.zero,
                palette.BasaltHighlight);
            var chimney = CreateColliderBox(
                colliderRoot,
                "traversal.embercrown.climb.forge-chimney collider",
                chimneyPosition,
                new Vector3(0.62f, 6f, 4f),
                Vector3.zero,
                false,
                true);
            chimney.AddComponent<TetherfallEmbercrownTraversalAffordance>().Initialize(
                "traversal.embercrown.climb.forge-chimney",
                TetherfallEmbercrownTraversalKind.Climb);

            var forgeLight = new GameObject("Crownforge bounded firelight");
            forgeLight.transform.SetParent(artRoot, false);
            forgeLight.transform.position = chamberCenter + new Vector3(0f, 3.2f, 2f);
            var light = forgeLight.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.3f, 0.08f);
            light.intensity = 1.55f;
            light.range = 18f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
        }

        private static void BuildTunnelSegment(
            Transform artRoot,
            Transform colliderRoot,
            Vector3 start,
            Vector3 end,
            int index,
            EmbercrownPalette palette)
        {
            var delta = end - start;
            delta.y = 0f;
            var length = Mathf.Max(0.1f, delta.magnitude + 0.8f);
            var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            var center = (start + end) * 0.5f;
            CreateArtPrimitive(
                artRoot,
                $"Crownforge tunnel floor {index + 1}",
                PrimitiveType.Cube,
                center,
                new Vector3(6.4f, 0.18f, length),
                new Vector3(0f, yaw, 0f),
                palette.Obsidian);
            CreateColliderBox(
                colliderRoot,
                $"Crownforge tunnel floor collider {index + 1}",
                center,
                new Vector3(6.1f, 0.18f, length - 0.2f),
                new Vector3(0f, yaw, 0f),
                false,
                false);

            var radians = yaw * Mathf.Deg2Rad;
            var localRight = new Vector3(Mathf.Cos(radians), 0f, -Mathf.Sin(radians));
            for (var side = -1; side <= 1; side += 2)
            {
                var wallCenter = center + localRight * 3.45f + Vector3.up * 2.15f;
                CreateArtPrimitive(
                    artRoot,
                    $"Crownforge tunnel wall {index + 1}.{side}",
                    PrimitiveType.Cube,
                    wallCenter,
                    new Vector3(0.65f, 4.3f, length),
                    new Vector3(0f, yaw, 0f),
                    index % 2 == 0 ? palette.Basalt : palette.BasaltHighlight);
                CreateColliderBox(
                    colliderRoot,
                    $"Crownforge tunnel wall collider {index + 1}.{side}",
                    wallCenter,
                    new Vector3(0.58f, 4.2f, length - 0.2f),
                    new Vector3(0f, yaw, 0f),
                    false,
                    false);
            }

            CreateArtPrimitive(
                artRoot,
                $"Crownforge tunnel cap {index + 1}",
                PrimitiveType.Cube,
                center + Vector3.up * 4.45f,
                new Vector3(7.4f, 0.7f, length),
                new Vector3(0f, yaw, 0f),
                palette.Basalt);
        }

        private static void CreateEmberParticles(Transform parent, Material material)
        {
            var root = new GameObject("Embercrown bounded ember drift");
            root.transform.SetParent(parent, false);
            root.transform.position = RegionCenterValue + new Vector3(0f, 1f, 0f);
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.useAutoRandomSeed = false;
            particles.randomSeed = 0xEC041u;
            var main = particles.main;
            main.duration = 10f;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = false;
            main.maxParticles = EmberParticleCapacity;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4.8f, 7.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.17f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.24f, 0.045f, 0.72f),
                new Color(1f, 0.7f, 0.18f, 0.88f));

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 5.1f;
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(0f, 2.4f, 0f);
            shape.scale = new Vector3(205f, 4.8f, 205f);
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.16f, 0.16f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.24f, 0.48f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
            var noise = particles.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Low;
            noise.strength = 0.2f;
            noise.frequency = 0.32f;
            noise.octaveCount = 1;
            var collision = particles.collision;
            collision.enabled = false;
            var lights = particles.lights;
            lights.enabled = false;
            var trails = particles.trails;
            trails.enabled = false;

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            ConfigureParticleRenderer(renderer, material);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 0.045f;
            renderer.localBounds = new Bounds(
                new Vector3(0f, 5f, 0f),
                new Vector3(220f, 14f, 220f));
        }

        private static void CreateHeatShimmer(Transform parent, Material material)
        {
            var root = new GameObject("Embercrown bounded heat shimmer");
            root.transform.SetParent(parent, false);
            root.transform.position = RegionCenterValue + new Vector3(-6f, 0.32f, -14f);
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.useAutoRandomSeed = false;
            particles.randomSeed = 0xECA71u;
            var main = particles.main;
            main.duration = 9f;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = false;
            main.maxParticles = HeatShimmerParticleCapacity;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 4.4f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(2.8f, 5.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.34f, 0.1f, 0.045f),
                new Color(1f, 0.72f, 0.3f, 0.075f));

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 2.1f;
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(0f, 0.35f, 0f);
            shape.scale = new Vector3(185f, 0.7f, 175f);
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            var noise = particles.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Low;
            noise.strength = 0.12f;
            noise.frequency = 0.18f;
            noise.octaveCount = 1;
            var collision = particles.collision;
            collision.enabled = false;
            var lights = particles.lights;
            lights.enabled = false;
            var trails = particles.trails;
            trails.enabled = false;

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            ConfigureParticleRenderer(renderer, material);
            renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard;
            renderer.maxParticleSize = 0.11f;
            renderer.localBounds = new Bounds(
                new Vector3(0f, 2f, 0f),
                new Vector3(198f, 7f, 188f));
        }

        private static void ConfigureParticleRenderer(
            ParticleSystemRenderer renderer,
            Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = true;
            renderer.sortMode = ParticleSystemSortMode.None;
        }

        private static void CreatePreferredArt(
            Transform parent,
            ImportedPlacement placement,
            bool forceProceduralFallback)
        {
            if (!forceProceduralFallback)
            {
                var prefab = Resources.Load<GameObject>(placement.ResourcePath);
                if (prefab != null)
                {
                    var instance = Object.Instantiate(prefab, parent, false);
                    instance.name = placement.Name;
                    instance.transform.position = placement.Position;
                    instance.transform.rotation = Quaternion.Euler(0f, placement.YawDegrees, 0f);
                    instance.transform.localScale = SanitizeScale(placement.Scale);
                    StripImportedGameplayComponents(instance);
                    var renderers = instance.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length > 0 &&
                        renderers.Length <= MaxImportedRenderersPerPlacement)
                    {
                        for (var index = 0; index < renderers.Length; index++)
                        {
                            ConfigureRenderer(renderers[index]);
                            ReplaceAllSharedMaterials(renderers[index], placement.Material);
                        }

                        instance.AddComponent<TetherfallEmbercrownImportedArt>().Initialize(
                            placement.ResourcePath);
                        return;
                    }

                    DestroyObject(instance);
                }
            }

            var fallback = CreateArtPrimitive(
                parent,
                placement.Name + " procedural fallback",
                placement.FallbackPrimitive,
                placement.Position + Vector3.up * 0.6f,
                new Vector3(
                    Mathf.Max(1f, placement.Scale.x * 1.2f),
                    Mathf.Max(1f, placement.Scale.y * 1.1f),
                    Mathf.Max(1f, placement.Scale.z * 1.2f)),
                new Vector3(
                    placement.FallbackPrimitive == PrimitiveType.Cube ? 12f : 0f,
                    placement.YawDegrees,
                    placement.FallbackPrimitive == PrimitiveType.Cube ? -7f : 0f),
                placement.Material);
            fallback.AddComponent<TetherfallEmbercrownProceduralFallback>().Initialize(
                placement.ResourcePath);
        }

        private static void ReplaceAllSharedMaterials(Renderer renderer, Material material)
        {
            if (renderer == null)
            {
                return;
            }

            var slotCount = Mathf.Max(1, renderer.sharedMaterials.Length);
            var replacements = new Material[slotCount];
            for (var index = 0; index < replacements.Length; index++)
            {
                replacements[index] = material;
            }

            renderer.sharedMaterials = replacements;
        }

        private static void StripImportedGameplayComponents(GameObject root)
        {
            DestroyComponents<Collider>(root);
            DestroyComponents<Collider2D>(root);
            DestroyComponents<Rigidbody>(root);
            DestroyComponents<Rigidbody2D>(root);
            DestroyComponents<Light>(root);
            DestroyComponents<Camera>(root);
            DestroyComponents<Animator>(root);
            DestroyComponents<Animation>(root);
        }

        private static void DestroyComponents<T>(GameObject root) where T : Component
        {
            var components = root.GetComponentsInChildren<T>(true);
            for (var index = 0; index < components.Length; index++)
            {
                if (components[index] != null)
                {
                    DestroyObject(components[index]);
                }
            }
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return root.transform;
        }

        private static Transform CreateArtRoot(Transform parent, string name)
        {
            var root = CreateGroup(parent, name);
            root.gameObject.AddComponent<TetherfallEmbercrownArtRoot>();
            return root;
        }

        private static Transform CreateCollisionRoot(Transform parent, string name)
        {
            var root = CreateGroup(parent, name);
            root.gameObject.AddComponent<TetherfallEmbercrownCollisionRoot>();
            return root;
        }

        private static GameObject CreateArtPrimitive(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 worldPosition,
            Vector3 scale,
            Vector3 eulerAngles,
            Material material)
        {
            var piece = GameObject.CreatePrimitive(primitive);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.position = worldPosition;
            piece.transform.rotation = Quaternion.Euler(eulerAngles);
            piece.transform.localScale = SanitizeScale(scale);
            var collider = piece.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyObject(collider);
            }

            var renderer = piece.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                ConfigureRenderer(renderer);
            }

            return piece;
        }

        private static void ConfigureRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = true;
        }

        private static GameObject CreateColliderBox(
            Transform parent,
            string name,
            Vector3 worldPosition,
            Vector3 size,
            Vector3 eulerAngles,
            bool trigger,
            bool climbable)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = worldPosition;
            root.transform.rotation = Quaternion.Euler(eulerAngles);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(
                Mathf.Max(0.05f, Mathf.Abs(size.x)),
                Mathf.Max(0.05f, Mathf.Abs(size.y)),
                Mathf.Max(0.05f, Mathf.Abs(size.z)));
            collider.isTrigger = trigger;
            if (climbable)
            {
                root.AddComponent<ClimbableSurface>();
            }

            return root;
        }

        private static void CreateHorizontalSegmentArt(
            Transform parent,
            string name,
            Vector3 start,
            Vector3 end,
            float width,
            float height,
            float yOffset,
            Material material,
            float lengthOverlap = 0f)
        {
            var delta = end - start;
            delta.y = 0f;
            var length = Mathf.Max(0.1f, delta.magnitude + Mathf.Max(0f, lengthOverlap));
            var center = (start + end) * 0.5f + Vector3.up * yOffset;
            var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            CreateArtPrimitive(
                parent,
                name,
                PrimitiveType.Cube,
                center,
                new Vector3(width, height, length),
                new Vector3(0f, yaw, 0f),
                material);
        }

        private static void CreateHorizontalSegmentCollider(
            Transform parent,
            string name,
            Vector3 start,
            Vector3 end,
            float width,
            float height,
            float yOffset,
            bool trigger)
        {
            var delta = end - start;
            delta.y = 0f;
            var length = Mathf.Max(0.1f, delta.magnitude);
            var center = (start + end) * 0.5f + Vector3.up * yOffset;
            var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            CreateColliderBox(
                parent,
                name,
                center,
                new Vector3(width, height, length),
                new Vector3(0f, yaw, 0f),
                trigger,
                false);
        }

        private static void CreateArtBeam(
            Transform parent,
            string name,
            Vector3 start,
            Vector3 end,
            float width,
            float thickness,
            Material material)
        {
            var delta = end - start;
            var length = delta.magnitude;
            if (length <= 0.001f)
            {
                return;
            }

            var piece = CreateArtPrimitive(
                parent,
                name,
                PrimitiveType.Cube,
                (start + end) * 0.5f,
                new Vector3(width, thickness, length),
                Vector3.zero,
                material);
            piece.transform.rotation = Quaternion.LookRotation(delta / length, Vector3.up);
        }

        private static bool ExceedsBudget(TetherfallEmbercrownWorldMetrics metrics)
        {
            return metrics.Renderers > MaxRendererCount ||
                   metrics.Colliders > MaxColliderCount ||
                   metrics.SharedMaterials > MaxSharedMaterialCount ||
                   metrics.Lights > MaxLightCount ||
                   metrics.ParticleSystems > MaxParticleSystemCount ||
                   metrics.UpdateDrivers > MaxUpdateDriverCount ||
                   metrics.TotalParticleCapacity > MaxTotalParticleCapacity;
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            if (parent == null)
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

        private static Vector3 SanitizeScale(Vector3 scale)
        {
            if (!IsFinite(scale))
            {
                return Vector3.one;
            }

            return new Vector3(
                Mathf.Clamp(Mathf.Abs(scale.x), 0.02f, 80f),
                Mathf.Clamp(Mathf.Abs(scale.y), 0.02f, 80f),
                Mathf.Clamp(Mathf.Abs(scale.z), 0.02f, 80f));
        }

        private static float HorizontalDistanceSquaredToSegment(
            Vector3 point,
            Vector3 start,
            Vector3 end)
        {
            var segmentX = end.x - start.x;
            var segmentZ = end.z - start.z;
            var lengthSquared = segmentX * segmentX + segmentZ * segmentZ;
            if (lengthSquared <= 0.000001f)
            {
                var pointX = point.x - start.x;
                var pointZ = point.z - start.z;
                return pointX * pointX + pointZ * pointZ;
            }

            var projection = Mathf.Clamp01(
                ((point.x - start.x) * segmentX + (point.z - start.z) * segmentZ) /
                lengthSquared);
            var closestX = start.x + segmentX * projection;
            var closestZ = start.z + segmentZ * projection;
            var deltaX = point.x - closestX;
            var deltaZ = point.z - closestZ;
            return deltaX * deltaX + deltaZ * deltaZ;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static Vector3 Local(float x, float y, float z)
        {
            return RegionCenterValue + new Vector3(x, y, z);
        }

        private static TetherfallEmbercrownLavaChannelDefinition[] CreateLavaChannels()
        {
            return new[]
            {
                new TetherfallEmbercrownLavaChannelDefinition(
                    "lava.embercrown.crownspill",
                    "Crownspill trunk",
                    16f,
                    Local(-42f, 0.2f, 13f),
                    Local(-28f, 0.08f, 30f),
                    Local(-16f, 0.07f, 18f),
                    Local(-4f, 0.06f, 8f),
                    Local(8f, 0.05f, 0f)),
                new TetherfallEmbercrownLavaChannelDefinition(
                    "lava.embercrown.east-vein",
                    "East Vein",
                    13f,
                    Local(66f, 0.2f, 23f),
                    Local(8f, 0.05f, 0f),
                    Local(38f, 0.04f, 5f),
                    Local(72f, 0.03f, 8f),
                    Local(108f, 0.02f, -2f)),
                new TetherfallEmbercrownLavaChannelDefinition(
                    "lava.embercrown.southfall",
                    "Southfall River",
                    15f,
                    Local(34f, 0.2f, -46f),
                    Local(8f, 0.05f, 0f),
                    Local(16f, 0.04f, -34f),
                    Local(5f, 0.03f, -70f),
                    Local(22f, 0.02f, -110f)),
                new TetherfallEmbercrownLavaChannelDefinition(
                    "lava.embercrown.west-emberglass",
                    "West Emberglass",
                    11f,
                    Local(-48f, 0.2f, -11f),
                    Local(0f, 0.05f, 4f),
                    Local(-30f, 0.04f, -18f),
                    Local(-63f, 0.03f, -47f),
                    Local(-102f, 0.02f, -84f))
            };
        }

        private static TetherfallEmbercrownSafeRouteDefinition[] CreateSafeRoutes()
        {
            return new[]
            {
                new TetherfallEmbercrownSafeRouteDefinition(
                    "route.embercrown.copper-spine",
                    "Copper Spine to Crownforge",
                    5.5f,
                    false,
                    SafeArrivalValue,
                    Local(100f, 0.05f, 32f),
                    Local(82f, 0.05f, 22f),
                    Local(78f, 0.34f, 10f),
                    Local(76f, 0.34f, 16f),
                    Local(55f, 0.05f, 17f),
                    Local(35f, 0.05f, 19f),
                    Local(18f, 0.05f, 23f),
                    Local(4f, 0.05f, 25f),
                    Local(-8f, 0.34f, 30f),
                    Local(-35f, 0.34f, 35f),
                    Local(-52f, 0.05f, 20f),
                    Local(-62f, 0.08f, 10f)),
                new TetherfallEmbercrownSafeRouteDefinition(
                    "route.embercrown.north-rim",
                    "North Rim alternate",
                    4.8f,
                    true,
                    SafeArrivalValue,
                    Local(110f, 0.05f, 68f),
                    Local(85f, 0.05f, 92f),
                    Local(45f, 0.05f, 108f),
                    Local(5f, 0.05f, 115f),
                    Local(-35f, 0.05f, 110f),
                    Local(-75f, 0.05f, 92f),
                    Local(-100f, 0.05f, 65f),
                    Local(-95f, 0.05f, 35f),
                    Local(-80f, 0.05f, 18f),
                    Local(-62f, 0.08f, 10f)),
                new TetherfallEmbercrownSafeRouteDefinition(
                    "route.embercrown.south-switchback",
                    "South basin alternate",
                    5f,
                    true,
                    SafeArrivalValue,
                    Local(122f, 0.05f, 10f),
                    Local(112f, 0.05f, -20f),
                    Local(90f, 0.05f, -40f),
                    Local(62f, 0.05f, -50f),
                    Local(35f, 0.05f, -48f),
                    Local(25f, 0.34f, -46f),
                    Local(14f, 0.34f, -45f),
                    Local(2f, 0.34f, -44f),
                    Local(-10f, 0.05f, -60f),
                    Local(-20f, 0.05f, -90f))
            };
        }

        private static TetherfallEmbercrownSafeSurfaceDefinition[] CreateSafeSurfaces()
        {
            return new[]
            {
                new TetherfallEmbercrownSafeSurfaceDefinition(
                    "traversal.embercrown.bridge.east-vein",
                    TetherfallEmbercrownSafeSurfaceKind.Bridge,
                    Local(78f, 0.18f, 8f),
                    new Vector2(7f, 32f),
                    0f),
                new TetherfallEmbercrownSafeSurfaceDefinition(
                    "traversal.embercrown.bridge.crownspill",
                    TetherfallEmbercrownSafeSurfaceKind.Bridge,
                    Local(-21f, 0.18f, 32f),
                    new Vector2(7f, 35f),
                    -90f),
                new TetherfallEmbercrownSafeSurfaceDefinition(
                    "traversal.embercrown.bridge.southfall",
                    TetherfallEmbercrownSafeSurfaceKind.Bridge,
                    Local(14f, 0.18f, -45f),
                    new Vector2(7f, 32f),
                    90f),
                new TetherfallEmbercrownSafeSurfaceDefinition(
                    "traversal.embercrown.island.south-emberglass",
                    TetherfallEmbercrownSafeSurfaceKind.Island,
                    Local(6f, 0.14f, -70f),
                    new Vector2(9f, 9f),
                    12f),
                new TetherfallEmbercrownSafeSurfaceDefinition(
                    "traversal.embercrown.island.west-emberglass",
                    TetherfallEmbercrownSafeSurfaceKind.Island,
                    Local(-63f, 0.14f, -47f),
                    new Vector2(8.5f, 8.5f),
                    -18f)
            };
        }

        private static TetherfallEmbercrownSocketDefinition[] CreateSockets()
        {
            return new[]
            {
                ResourceSocket(
                    "socket.embercrown.resource.obsidian-shelf.01",
                    "Obsidian Shelf Ore",
                    Local(70f, 0.05f, 62f),
                    ResourceKind.Ore),
                ResourceSocket(
                    "socket.embercrown.resource.cinder-salt.01",
                    "Cinder Salt Bloom",
                    Local(41f, 0.05f, -84f),
                    ResourceKind.Chimesalt),
                ResourceSocket(
                    "socket.embercrown.resource.forge-ore.01",
                    "Crownforge Ore Seam",
                    Local(-58f, 0.05f, 4f),
                    ResourceKind.Ore),
                ResourceSocket(
                    "socket.embercrown.resource.ash-fiber.01",
                    "Ashsilk Fiber",
                    Local(101f, 0.05f, -68f),
                    ResourceKind.Fiber),
                ResourceSocket(
                    "socket.embercrown.resource.basalt.01",
                    "Basalt Survey Stone",
                    Local(-86f, 0.05f, 84f),
                    ResourceKind.Stone),
                EncounterSocket(
                    "socket.embercrown.encounter.crownwatch",
                    "Crownwatch Patrol",
                    Local(-3f, 0.05f, 92f),
                    182f,
                    "encounter.cinderwold.ember_shelf"),
                EncounterSocket(
                    "socket.embercrown.encounter.bridge-ambush",
                    "East Vein Bridge Ambush",
                    Local(58f, 0.05f, 19f),
                    248f,
                    "encounter.cinderwold.relay_breakers"),
                EncounterSocket(
                    "socket.embercrown.encounter.forge-guard",
                    "Crownforge Guard",
                    Local(-52f, 0.05f, 17f),
                    330f,
                    "encounter.cinderwold.ashjaw")
            };
        }

        private static TetherfallEmbercrownTraversalDefinition[] CreateTraversals()
        {
            return new[]
            {
                TraversalFromSurface(
                    SafeSurfaceCatalog[0],
                    TetherfallEmbercrownTraversalKind.Bridge,
                    Local(70f, 0.34f, 20f)),
                TraversalFromSurface(
                    SafeSurfaceCatalog[1],
                    TetherfallEmbercrownTraversalKind.Bridge,
                    Local(-42f, 0.34f, 34f)),
                TraversalFromSurface(
                    SafeSurfaceCatalog[2],
                    TetherfallEmbercrownTraversalKind.Bridge,
                    Local(31f, 0.34f, -45f)),
                TraversalFromSurface(
                    SafeSurfaceCatalog[3],
                    TetherfallEmbercrownTraversalKind.Island,
                    Local(-10f, 0.2f, -60f)),
                TraversalFromSurface(
                    SafeSurfaceCatalog[4],
                    TetherfallEmbercrownTraversalKind.Island,
                    Local(-48f, 0.2f, -11f)),
                new TetherfallEmbercrownTraversalDefinition(
                    "traversal.embercrown.climb.cinder-rib",
                    TetherfallEmbercrownTraversalKind.Climb,
                    Local(-5f, 7f, 72f),
                    Local(2f, 3.4f, 88f)),
                new TetherfallEmbercrownTraversalDefinition(
                    "traversal.embercrown.climb.obsidian-rib",
                    TetherfallEmbercrownTraversalKind.Climb,
                    Local(-78f, 6f, 62f),
                    Local(-75f, 0.2f, 92f)),
                new TetherfallEmbercrownTraversalDefinition(
                    "traversal.embercrown.lift.crownwind",
                    TetherfallEmbercrownTraversalKind.WindLift,
                    Local(12f, 1.25f, 75f),
                    Local(2f, 3.4f, 88f)),
                new TetherfallEmbercrownTraversalDefinition(
                    "traversal.embercrown.launch.obsidian-lip",
                    TetherfallEmbercrownTraversalKind.GliderLaunch,
                    Local(2f, 3.2f, 88f),
                    Local(45f, 0.2f, 108f)),
                new TetherfallEmbercrownTraversalDefinition(
                    "traversal.embercrown.climb.forge-chimney",
                    TetherfallEmbercrownTraversalKind.Climb,
                    Local(-75.45f, 3.2f, 43.2f),
                    Local(-80f, 0.2f, 18f))
            };
        }

        private static TetherfallEmbercrownSocketDefinition ResourceSocket(
            string id,
            string displayName,
            Vector3 position,
            ResourceKind resource)
        {
            return new TetherfallEmbercrownSocketDefinition(
                id,
                displayName,
                TetherfallEmbercrownSocketKind.Resource,
                position,
                0f,
                resource,
                string.Empty);
        }

        private static TetherfallEmbercrownSocketDefinition EncounterSocket(
            string id,
            string displayName,
            Vector3 position,
            float yawDegrees,
            string encounterId)
        {
            return new TetherfallEmbercrownSocketDefinition(
                id,
                displayName,
                TetherfallEmbercrownSocketKind.Encounter,
                position,
                yawDegrees,
                ResourceKind.Stone,
                encounterId);
        }

        private static TetherfallEmbercrownTraversalDefinition TraversalFromSurface(
            TetherfallEmbercrownSafeSurfaceDefinition surface,
            TetherfallEmbercrownTraversalKind kind,
            Vector3 safeLandingPoint)
        {
            return new TetherfallEmbercrownTraversalDefinition(
                surface.Id,
                kind,
                surface.Center,
                safeLandingPoint);
        }

        private static void DestroyObject(Object value)
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

        private readonly struct ImportedPlacement
        {
            public readonly string ResourcePath;
            public readonly string Name;
            public readonly Vector3 Position;
            public readonly Vector3 Scale;
            public readonly float YawDegrees;
            public readonly PrimitiveType FallbackPrimitive;
            public readonly Material Material;

            public ImportedPlacement(
                string resourcePath,
                string name,
                Vector3 position,
                Vector3 scale,
                float yawDegrees,
                PrimitiveType fallbackPrimitive,
                Material material)
            {
                ResourcePath = resourcePath;
                Name = name;
                Position = position;
                Scale = scale;
                YawDegrees = yawDegrees;
                FallbackPrimitive = fallbackPrimitive;
                Material = material;
            }
        }

        private readonly struct EmbercrownPalette
        {
            public readonly Material AshGround;
            public readonly Material Basalt;
            public readonly Material BasaltHighlight;
            public readonly Material Obsidian;
            public readonly Material HotCrust;
            public readonly Material Lava;
            public readonly Material Copper;
            public readonly Material Teal;
            public readonly Material Smoke;
            public readonly Material ParticleMaterial;

            private EmbercrownPalette(
                Material ashGround,
                Material basalt,
                Material basaltHighlight,
                Material obsidian,
                Material hotCrust,
                Material lava,
                Material copper,
                Material teal,
                Material smoke,
                Material particleMaterial)
            {
                AshGround = ashGround;
                Basalt = basalt;
                BasaltHighlight = basaltHighlight;
                Obsidian = obsidian;
                HotCrust = hotCrust;
                Lava = lava;
                Copper = copper;
                Teal = teal;
                Smoke = smoke;
                ParticleMaterial = particleMaterial;
            }

            public static EmbercrownPalette Create(TetherfallGame game)
            {
                return new EmbercrownPalette(
                    RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.12f, 0.075f, 0.07f),
                        0.01f,
                        0.2f),
                    RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.105f, 0.055f, 0.065f),
                        0.03f,
                        0.27f),
                    RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.25f, 0.105f, 0.08f),
                        0.06f,
                        0.34f),
                    RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.035f, 0.025f, 0.035f),
                        0.16f,
                        0.52f),
                    RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.34f, 0.085f, 0.045f),
                        0.04f,
                        0.3f),
                    RuntimeEffectMaterialCache.GetGlow(
                        game,
                        new Color(1f, 0.18f, 0.025f),
                        2.45f,
                        0.58f),
                    RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.68f, 0.29f, 0.12f),
                        0.38f,
                        0.55f),
                    RuntimeEffectMaterialCache.GetGlow(
                        game,
                        new Color(0.12f, 0.86f, 0.74f),
                        1.15f,
                        0.48f),
                    RuntimeEffectMaterialCache.GetTransparent(
                        game,
                        new Color(0.12f, 0.075f, 0.09f, 0.24f),
                        0.08f),
                    RuntimeEffectMaterialCache.GetSoftParticleMaterial(game));
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallEmbercrownWorldRoot : MonoBehaviour
    {
        public GameObject SilhouetteRoot { get; private set; }
        public GameObject ConnectorRoot { get; private set; }
        public GameObject TerrainActivationRoot { get; private set; }
        public GameObject DetailActivationRoot { get; private set; }
        public GameObject ForgeActivationRoot { get; private set; }
        public bool ForcedProceduralFallback { get; private set; }
        public TetherfallEmbercrownWorldMetrics Metrics { get; private set; }

        internal void Initialize(
            GameObject silhouetteRoot,
            GameObject connectorRoot,
            GameObject terrainRoot,
            GameObject detailRoot,
            GameObject forgeRoot,
            bool forcedProceduralFallback)
        {
            SilhouetteRoot = silhouetteRoot;
            ConnectorRoot = connectorRoot;
            TerrainActivationRoot = terrainRoot;
            DetailActivationRoot = detailRoot;
            ForgeActivationRoot = forgeRoot;
            ForcedProceduralFallback = forcedProceduralFallback;
        }

        internal void SetMetrics(TetherfallEmbercrownWorldMetrics metrics)
        {
            Metrics = metrics;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallEmbercrownArtRoot : MonoBehaviour
    {
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallEmbercrownCollisionRoot : MonoBehaviour
    {
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallEmbercrownImportedArt : MonoBehaviour
    {
        public string ResourcePath { get; private set; }

        internal void Initialize(string resourcePath)
        {
            ResourcePath = resourcePath ?? string.Empty;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallEmbercrownProceduralFallback : MonoBehaviour
    {
        public string MissingResourcePath { get; private set; }

        internal void Initialize(string resourcePath)
        {
            MissingResourcePath = resourcePath ?? string.Empty;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallEmbercrownWorldSocket : MonoBehaviour
    {
        public string StableId { get; private set; }
        public string DisplayName { get; private set; }
        public TetherfallEmbercrownSocketKind Kind { get; private set; }
        public ResourceKind Resource { get; private set; }
        public string EncounterId { get; private set; }

        internal void Initialize(TetherfallEmbercrownSocketDefinition definition)
        {
            StableId = definition != null ? definition.Id : string.Empty;
            DisplayName = definition != null ? definition.DisplayName : string.Empty;
            Kind = definition != null
                ? definition.Kind
                : TetherfallEmbercrownSocketKind.Resource;
            Resource = definition != null ? definition.Resource : ResourceKind.Stone;
            EncounterId = definition != null ? definition.EncounterId : string.Empty;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallEmbercrownTraversalAffordance : MonoBehaviour
    {
        public string StableId { get; private set; }
        public TetherfallEmbercrownTraversalKind Kind { get; private set; }

        internal void Initialize(
            string stableId,
            TetherfallEmbercrownTraversalKind kind)
        {
            StableId = stableId ?? string.Empty;
            Kind = kind;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallEmbercrownLavaHazardVolume : MonoBehaviour
    {
        private Vector3 center;
        private Vector3 size;
        private float yawDegrees;

        public string StableId { get; private set; }
        public string ChannelId { get; private set; }
        public Vector3 SafeEjectionPoint { get; private set; }

        internal void Initialize(
            string stableId,
            string channelId,
            Vector3 worldCenter,
            Vector3 worldSize,
            float worldYawDegrees,
            Vector3 safeEjectionPoint)
        {
            StableId = stableId ?? string.Empty;
            ChannelId = channelId ?? string.Empty;
            center = worldCenter;
            size = worldSize;
            yawDegrees = worldYawDegrees;
            SafeEjectionPoint = safeEjectionPoint;
        }

        public bool Contains(Vector3 worldPosition)
        {
            var delta = worldPosition - center;
            if (Mathf.Abs(delta.y) > size.y * 0.5f + 0.2f)
            {
                return false;
            }

            var radians = -yawDegrees * Mathf.Deg2Rad;
            var cosine = Mathf.Cos(radians);
            var sine = Mathf.Sin(radians);
            var localX = delta.x * cosine - delta.z * sine;
            var localZ = delta.x * sine + delta.z * cosine;
            return Mathf.Abs(localX) <= size.x * 0.5f &&
                   Mathf.Abs(localZ) <= size.z * 0.5f;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallEmbercrownLavaHazardDirector : MonoBehaviour
    {
        private const string LavaDamageSource = "Embercrown lava";
        private const string EjectionNotice =
            "Threadglass ejects you onto cooled basalt before the current takes hold.";

        private TetherfallGame game;
        private Transform player;
        private TetherfallEmbercrownLavaHazardVolume[] volumes =
            Array.Empty<TetherfallEmbercrownLavaHazardVolume>();
        private TetherfallEmbercrownLavaHazardVolume activeVolume;
        private float nextPollAt;
        private float nextDamageAt;
        private float reentryAllowedAt;
        private int appliedTicks;

        internal void Initialize(
            TetherfallGame owner,
            TetherfallEmbercrownLavaHazardVolume[] hazardVolumes)
        {
            game = owner;
            volumes = hazardVolumes ?? Array.Empty<TetherfallEmbercrownLavaHazardVolume>();
            player = owner != null && owner.Player != null ? owner.PlayerTransform : null;
            ResetExposure();
        }

        private void OnDisable()
        {
            ResetExposure();
        }

        private void Update()
        {
            if (Time.time < nextPollAt)
            {
                return;
            }

            nextPollAt = Time.time + TetherfallEmbercrownWorld.LavaHazardPollSeconds;
            if (game == null || game.Player == null)
            {
                player = null;
                ResetExposure();
                return;
            }

            if (player == null)
            {
                player = game.PlayerTransform;
            }

            if (player == null || Time.time < reentryAllowedAt)
            {
                return;
            }

            var playerPosition = player.position;
            if (TetherfallEmbercrownWorld.IsProtectedByTraversalSurface(playerPosition))
            {
                ResetExposure();
                return;
            }

            var currentVolume = ResolveVolume(playerPosition);
            if (currentVolume == null)
            {
                ResetExposure();
                return;
            }

            if (activeVolume != currentVolume)
            {
                activeVolume = currentVolume;
                appliedTicks = 0;
                nextDamageAt =
                    Time.time + TetherfallEmbercrownWorld.LavaDamageTickSeconds;
                return;
            }

            if (Time.time < nextDamageAt)
            {
                return;
            }

            nextDamageAt = Time.time + TetherfallEmbercrownWorld.LavaDamageTickSeconds;
            var healthBefore = game.Health;
            var positionBefore = player.position;
            game.DamagePlayer(
                TetherfallEmbercrownWorld.LavaDamagePerTick,
                LavaDamageSource);
            appliedTicks = Mathf.Min(
                TetherfallEmbercrownWorld.LavaTicksBeforeEjection,
                appliedTicks + 1);

            var respawned =
                game.Health > healthBefore ||
                (player.position - positionBefore).sqrMagnitude > 64f;
            if (respawned)
            {
                reentryAllowedAt =
                    Time.time + TetherfallEmbercrownWorld.LavaReentryGraceSeconds;
                ResetExposure();
                return;
            }

            if (!TetherfallEmbercrownWorld.ShouldEjectAfterLavaTicks(appliedTicks))
            {
                return;
            }

            var ejectionPoint = activeVolume.SafeEjectionPoint + Vector3.up * 0.18f;
            game.Player.Teleport(ejectionPoint);
            game.Notify(EjectionNotice, 4.8f);
            reentryAllowedAt =
                Time.time + TetherfallEmbercrownWorld.LavaReentryGraceSeconds;
            ResetExposure();
        }

        private TetherfallEmbercrownLavaHazardVolume ResolveVolume(Vector3 position)
        {
            for (var index = 0; index < volumes.Length; index++)
            {
                var volume = volumes[index];
                if (volume != null && volume.Contains(position))
                {
                    return volume;
                }
            }

            return null;
        }

        private void ResetExposure()
        {
            activeVolume = null;
            appliedTicks = 0;
            nextDamageAt = float.PositiveInfinity;
        }
    }

    [DefaultExecutionOrder(-9985)]
    [DisallowMultipleComponent]
    public sealed class TetherfallEmbercrownProximityDirector : MonoBehaviour
    {
        private TetherfallGame game;
        private Transform observer;
        private GameObject terrainRoot;
        private GameObject detailRoot;
        private GameObject forgeRoot;
        private float nextRefreshAt;

        internal void Initialize(
            TetherfallGame owner,
            GameObject terrainActivationRoot,
            GameObject detailActivationRoot,
            GameObject forgeActivationRoot)
        {
            game = owner;
            terrainRoot = terrainActivationRoot;
            detailRoot = detailActivationRoot;
            forgeRoot = forgeActivationRoot;
            SetActive(terrainRoot, false);
            SetActive(detailRoot, false);
            SetActive(forgeRoot, false);
            if (owner != null && owner.Player != null)
            {
                observer = owner.PlayerTransform;
                Refresh(observer.position);
            }

            nextRefreshAt =
                Time.unscaledTime + TetherfallEmbercrownWorld.ActivationPollSeconds;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshAt)
            {
                return;
            }

            nextRefreshAt =
                Time.unscaledTime + TetherfallEmbercrownWorld.ActivationPollSeconds;
            if (game == null || game.Player == null)
            {
                observer = null;
                return;
            }

            if (observer == null)
            {
                observer = game.PlayerTransform;
            }

            if (observer != null)
            {
                Refresh(observer.position);
            }
        }

        private void Refresh(Vector3 worldPosition)
        {
            var boundsDistance =
                TetherfallEmbercrownWorld.HorizontalDistanceToWorldBounds(worldPosition);
            var terrainThreshold =
                TetherfallEmbercrownWorld.TerrainPrewakeDistance +
                (terrainRoot != null && terrainRoot.activeSelf
                    ? TetherfallEmbercrownWorld.ActivationHysteresis
                    : 0f);
            var detailThreshold =
                TetherfallEmbercrownWorld.DetailPrewakeDistance +
                (detailRoot != null && detailRoot.activeSelf
                    ? TetherfallEmbercrownWorld.ActivationHysteresis
                    : 0f);
            var forgeDistance = HorizontalDistance(
                worldPosition,
                TetherfallEmbercrownWorld.ForgeCenter);
            var forgeThreshold =
                TetherfallEmbercrownWorld.ForgeActivationDistance +
                (forgeRoot != null && forgeRoot.activeSelf
                    ? TetherfallEmbercrownWorld.ActivationHysteresis
                    : 0f);

            SetActive(terrainRoot, boundsDistance <= terrainThreshold);
            SetActive(detailRoot, boundsDistance <= detailThreshold);
            SetActive(forgeRoot, forgeDistance <= forgeThreshold);
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right)
        {
            var deltaX = left.x - right.x;
            var deltaZ = left.z - right.z;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
