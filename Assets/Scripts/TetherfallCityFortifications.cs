using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Tetherfall
{
    public enum TetherfallCityFortificationStyle
    {
        AlderwakeTerracedCitadel,
        VeyrwaterFloodwallMooring
    }

    public enum TetherfallCityFortificationColliderRole
    {
        WallBody,
        WallWalk,
        Railing,
        Gatehouse,
        Ascent,
        ClimbableFace,
        TowerOverlook,
        SafeCrossing
    }

    public readonly struct TetherfallCityFortificationBuildOptions
    {
        public bool ForceMissingResources { get; }
        public bool RebuildExisting { get; }

        public static TetherfallCityFortificationBuildOptions Default =>
            new TetherfallCityFortificationBuildOptions(false, false);

        public static TetherfallCityFortificationBuildOptions Rebuild =>
            new TetherfallCityFortificationBuildOptions(false, true);

        public static TetherfallCityFortificationBuildOptions ForcedFallback =>
            new TetherfallCityFortificationBuildOptions(true, false);

        public static TetherfallCityFortificationBuildOptions RebuildForcedFallback =>
            new TetherfallCityFortificationBuildOptions(true, true);

        public TetherfallCityFortificationBuildOptions(
            bool forceMissingResources,
            bool rebuildExisting)
        {
            ForceMissingResources = forceMissingResources;
            RebuildExisting = rebuildExisting;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallCityFortificationRoot : MonoBehaviour
    {
        public bool ForcedMissingResources { get; private set; }
        public int CityCount { get; private set; }
        public int RendererCount { get; private set; }
        public int DeliberateColliderCount { get; private set; }
        public int ProceduralMaterialCount { get; private set; }
        public int PreferredResourcePlacementCount { get; private set; }
        public int UsedPreferredResourceCount { get; private set; }
        public int ProceduralFallbackCount { get; private set; }
        public int CullingClusterCount { get; private set; }
        public float NearDistance => TetherfallCityFortificationDistanceCulling.NearDistance;
        public float CullDistance => TetherfallCityFortificationDistanceCulling.CullDistance;

        internal void Configure(
            bool forcedMissingResources,
            int cityCount,
            int rendererCount,
            int deliberateColliderCount,
            int proceduralMaterialCount,
            int preferredResourcePlacementCount,
            int usedPreferredResourceCount,
            int proceduralFallbackCount,
            int cullingClusterCount)
        {
            ForcedMissingResources = forcedMissingResources;
            CityCount = cityCount;
            RendererCount = rendererCount;
            DeliberateColliderCount = deliberateColliderCount;
            ProceduralMaterialCount = proceduralMaterialCount;
            PreferredResourcePlacementCount = preferredResourcePlacementCount;
            UsedPreferredResourceCount = usedPreferredResourceCount;
            ProceduralFallbackCount = proceduralFallbackCount;
            CullingClusterCount = cullingClusterCount;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallCityFortificationCity : MonoBehaviour
    {
        public string CityId { get; private set; }
        public TetherfallCityFortificationStyle Style { get; private set; }
        public string GatehouseSignature { get; private set; }
        public int WallSegmentCount { get; private set; }
        public int WalkableSegmentCount { get; private set; }
        public int TowerCount { get; private set; }
        public int GuardAlcoveCount { get; private set; }
        public float SkylineHeight { get; private set; }
        public Transform VisualRoot { get; private set; }
        public Transform GameplayRoot { get; private set; }

        internal void Configure(
            CityDefinition city,
            TetherfallCityFortificationStyle style,
            string gatehouseSignature,
            int wallSegmentCount,
            int walkableSegmentCount,
            int towerCount,
            int guardAlcoveCount,
            float skylineHeight,
            Transform visualRoot,
            Transform gameplayRoot)
        {
            CityId = city != null ? city.id : string.Empty;
            Style = style;
            GatehouseSignature = gatehouseSignature ?? string.Empty;
            WallSegmentCount = wallSegmentCount;
            WalkableSegmentCount = walkableSegmentCount;
            TowerCount = towerCount;
            GuardAlcoveCount = guardAlcoveCount;
            SkylineHeight = skylineHeight;
            VisualRoot = visualRoot;
            GameplayRoot = gameplayRoot;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallCityFortificationTraversal : MonoBehaviour
    {
        private Vector3[] worldNodes = Array.Empty<Vector3>();

        public string CityId { get; private set; }
        public string RouteId { get; private set; }
        public string OverlookId { get; private set; }
        public bool IsWallAscent { get; private set; }
        public bool HasAlternateExit { get; private set; }
        public IReadOnlyList<Vector3> WorldNodes => worldNodes;

        public float PeakWorldHeight
        {
            get
            {
                var peak = float.NegativeInfinity;
                for (var index = 0; index < worldNodes.Length; index++)
                {
                    peak = Mathf.Max(peak, worldNodes[index].y);
                }

                return worldNodes.Length > 0 ? peak : 0f;
            }
        }

        public bool StartsAtGround =>
            worldNodes.Length > 0 && worldNodes[0].y <= 0.65f;

        public bool ReturnsToGround =>
            worldNodes.Length > 1 && worldNodes[worldNodes.Length - 1].y <= 0.65f;

        internal void Configure(
            string cityId,
            string routeId,
            string overlookId,
            bool isWallAscent,
            bool hasAlternateExit,
            Vector3[] nodes)
        {
            CityId = cityId ?? string.Empty;
            RouteId = routeId ?? string.Empty;
            OverlookId = overlookId ?? string.Empty;
            IsWallAscent = isWallAscent;
            HasAlternateExit = hasAlternateExit;
            worldNodes = nodes ?? Array.Empty<Vector3>();
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallCityFortificationCollider : MonoBehaviour
    {
        public string CityId { get; private set; }
        public string RouteId { get; private set; }
        public TetherfallCityFortificationColliderRole Role { get; private set; }

        internal void Configure(
            string cityId,
            TetherfallCityFortificationColliderRole role,
            string routeId)
        {
            CityId = cityId ?? string.Empty;
            Role = role;
            RouteId = routeId ?? string.Empty;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TetherfallCityFortificationCullingCluster : MonoBehaviour
    {
        private Renderer[] renderers = Array.Empty<Renderer>();

        public string CityId { get; private set; }
        public IReadOnlyList<Renderer> Renderers => renderers;
        public Bounds WorldBounds { get; private set; }
        public int RendererCount => renderers.Length;

        internal void Configure(string cityId, Renderer[] clusterRenderers)
        {
            CityId = cityId ?? string.Empty;
            renderers = clusterRenderers ?? Array.Empty<Renderer>();
            WorldBounds = CalculateBounds(renderers, transform.position);
        }

        private static Bounds CalculateBounds(
            IReadOnlyList<Renderer> clusterRenderers,
            Vector3 fallbackCenter)
        {
            var found = false;
            var bounds = new Bounds(fallbackCenter, Vector3.one);
            for (var index = 0; index < clusterRenderers.Count; index++)
            {
                var visualRenderer = clusterRenderers[index];
                if (visualRenderer == null || !TetherfallCityFortifications.IsFinite(visualRenderer.bounds))
                {
                    continue;
                }

                if (!found)
                {
                    bounds = visualRenderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(visualRenderer.bounds);
                }
            }

            return bounds;
        }
    }

    /// <summary>
    /// One CullingGroup governs both city clusters. Gameplay colliders stay active while the
    /// collider-free presentation renderers are distance culled.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TetherfallCityFortificationDistanceCulling : MonoBehaviour
    {
        public const float NearDistance = 85f;
        public const float CullDistance = 170f;

        private CullingGroup cullingGroup;
        private Renderer[][] rendererGroups = Array.Empty<Renderer[]>();

        public int ClusterCount { get; private set; }
        public int RegisteredRendererCount { get; private set; }
        public Camera TargetCamera { get; private set; }
        public bool IsCullingActive => cullingGroup != null;

        internal void Initialize(
            Camera targetCamera,
            IReadOnlyList<TetherfallCityFortificationCullingCluster> clusters)
        {
            DisposeCullingGroup();
            TargetCamera = targetCamera;
            ClusterCount = clusters != null ? clusters.Count : 0;
            RegisteredRendererCount = 0;
            rendererGroups = new Renderer[ClusterCount][];
            var spheres = new BoundingSphere[ClusterCount];

            for (var clusterIndex = 0; clusterIndex < ClusterCount; clusterIndex++)
            {
                var source = clusters[clusterIndex];
                var sourceRenderers = source != null
                    ? source.Renderers
                    : Array.Empty<Renderer>();
                rendererGroups[clusterIndex] = new Renderer[sourceRenderers.Count];
                for (var rendererIndex = 0; rendererIndex < sourceRenderers.Count; rendererIndex++)
                {
                    rendererGroups[clusterIndex][rendererIndex] = sourceRenderers[rendererIndex];
                    RegisteredRendererCount++;
                }

                var bounds = source != null
                    ? source.WorldBounds
                    : new Bounds(transform.position, Vector3.one);
                spheres[clusterIndex] = new BoundingSphere(
                    bounds.center,
                    Mathf.Max(1f, bounds.extents.magnitude));
            }

            SetAllVisible(true);
            if (targetCamera == null || ClusterCount == 0)
            {
                return;
            }

            cullingGroup = new CullingGroup
            {
                targetCamera = targetCamera,
                onStateChanged = HandleCullingStateChanged
            };
            cullingGroup.SetDistanceReferencePoint(targetCamera.transform);
            cullingGroup.SetBoundingDistances(new[] { NearDistance, CullDistance });
            cullingGroup.SetBoundingSpheres(spheres);
            cullingGroup.SetBoundingSphereCount(spheres.Length);

            for (var index = 0; index < spheres.Length; index++)
            {
                var distance =
                    Vector3.Distance(targetCamera.transform.position, spheres[index].position) -
                    spheres[index].radius;
                SetGroupVisible(index, distance <= CullDistance);
            }
        }

        private void HandleCullingStateChanged(CullingGroupEvent state)
        {
            SetGroupVisible(state.index, state.currentDistance < 2);
        }

        private void SetAllVisible(bool visible)
        {
            for (var index = 0; index < rendererGroups.Length; index++)
            {
                SetGroupVisible(index, visible);
            }
        }

        private void SetGroupVisible(int index, bool visible)
        {
            if (index < 0 || index >= rendererGroups.Length)
            {
                return;
            }

            var group = rendererGroups[index];
            for (var rendererIndex = 0; rendererIndex < group.Length; rendererIndex++)
            {
                if (group[rendererIndex] != null)
                {
                    group[rendererIndex].enabled = visible;
                }
            }
        }

        private void DisposeCullingGroup()
        {
            if (cullingGroup == null)
            {
                return;
            }

            cullingGroup.onStateChanged = null;
            cullingGroup.Dispose();
            cullingGroup = null;
        }

        private void OnDestroy()
        {
            DisposeCullingGroup();
        }
    }

    /// <summary>
    /// Additive, deterministic fortification architecture for both shipped cities. Visual meshes
    /// and gameplay collision are deliberately separated, and every optional Resources placement
    /// has a complete procedural fallback.
    /// </summary>
    public static class TetherfallCityFortifications
    {
        public const string RootName = "Tetherfall City Fortifications";
        public const string VisualRootName = "Visuals · Collider Free";
        public const string GameplayRootName = "Gameplay · Deliberate Simple Colliders";
        public const int ExpectedCityCount = 2;
        public const int ExpectedPreferredResourcePlacementCount = 8;
        public const int ExpectedAscentRouteCount = 4;
        public const int MaxRendererCount = 220;
        public const int MaxDeliberateColliderCount = 70;
        public const int MaxProceduralMaterialCount = 16;
        public const int AddedRealtimeLightCount = 0;
        public const int CullingDriverCount = 1;
        public const int PerPieceUpdateLoopCount = 0;

        private static readonly Dictionary<FortificationMaterialKey, Material> SharedMaterials =
            new Dictionary<FortificationMaterialKey, Material>();

        public static int BuiltRendererCount { get; private set; }
        public static int BuiltColliderCount { get; private set; }
        public static int BuiltPreferredResourceCount { get; private set; }
        public static int BuiltFallbackCount { get; private set; }
        public static int CachedProceduralMaterialCount => SharedMaterials.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            foreach (var material in SharedMaterials.Values)
            {
                DestroyObject(material);
            }

            SharedMaterials.Clear();
            BuiltRendererCount = 0;
            BuiltColliderCount = 0;
            BuiltPreferredResourceCount = 0;
            BuiltFallbackCount = 0;
        }

        public static GameObject Build(TetherfallGame game, Transform parent)
        {
            return Build(game, parent, TetherfallCityFortificationBuildOptions.Default);
        }

        public static GameObject Rebuild(TetherfallGame game, Transform parent)
        {
            return Build(game, parent, TetherfallCityFortificationBuildOptions.Rebuild);
        }

        public static GameObject BuildForcedMissingResources(
            TetherfallGame game,
            Transform parent)
        {
            return Build(game, parent, TetherfallCityFortificationBuildOptions.ForcedFallback);
        }

        public static GameObject RebuildForcedMissingResources(
            TetherfallGame game,
            Transform parent)
        {
            return Build(game, parent, TetherfallCityFortificationBuildOptions.RebuildForcedFallback);
        }

        public static GameObject Build(
            TetherfallGame game,
            Transform parent,
            TetherfallCityFortificationBuildOptions options)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var existing = FindDirectRoot(parent);
            if (existing != null &&
                !options.RebuildExisting &&
                existing.ForcedMissingResources == options.ForceMissingResources)
            {
                RestoreMetrics(existing);
                return existing.gameObject;
            }

            var root = existing != null
                ? existing.gameObject
                : new GameObject(RootName);
            if (existing == null)
            {
                root.transform.SetParent(parent, false);
            }
            else
            {
                ClearChildren(root.transform);
                root.name = RootName;
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;
            }

            var marker = root.GetComponent<TetherfallCityFortificationRoot>();
            if (marker == null)
            {
                marker = root.AddComponent<TetherfallCityFortificationRoot>();
            }

            try
            {
                var context = new BuildContext(game, root.transform, options.ForceMissingResources);
                BuildAlderwake(context);
                BuildVeyrwater(context);
                context.ValidateFinalBudget();

                var culling = root.GetComponent<TetherfallCityFortificationDistanceCulling>();
                if (culling == null)
                {
                    culling = root.AddComponent<TetherfallCityFortificationDistanceCulling>();
                }

                culling.Initialize(Camera.main, context.Clusters);
                marker.Configure(
                    options.ForceMissingResources,
                    context.Clusters.Count,
                    context.RendererCount,
                    context.ColliderCount,
                    SharedMaterials.Count,
                    context.ResourcePlacementCount,
                    context.UsedResourceCount,
                    context.FallbackCount,
                    context.Clusters.Count);

                BuiltRendererCount = context.RendererCount;
                BuiltColliderCount = context.ColliderCount;
                BuiltPreferredResourceCount = context.UsedResourceCount;
                BuiltFallbackCount = context.FallbackCount;
                return root;
            }
            catch
            {
                root.transform.SetParent(null, true);
                DestroyObject(root);
                throw;
            }
        }

        public static bool Cleanup(Transform parent)
        {
            if (parent == null)
            {
                return false;
            }

            var removed = false;
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);
                if (child == null ||
                    child.GetComponent<TetherfallCityFortificationRoot>() == null)
                {
                    continue;
                }

                removed = true;
                child.SetParent(null, true);
                DestroyObject(child.gameObject);
            }

            if (removed)
            {
                BuiltRendererCount = 0;
                BuiltColliderCount = 0;
                BuiltPreferredResourceCount = 0;
                BuiltFallbackCount = 0;
            }

            return removed;
        }

        public static bool IsBuilt(Transform parent)
        {
            return parent != null && FindDirectRoot(parent) != null;
        }

        private static void BuildAlderwake(BuildContext context)
        {
            var cityRoot = CreateCityRoot(
                context,
                CityCatalog.AlderwakeHearthmarket,
                TetherfallCityFortificationStyle.AlderwakeTerracedCitadel,
                out var visual,
                out var gameplay);

            var wallSegments = new[]
            {
                new WallSegmentSpec(new Vector3(-34f, 2.8f, -34f), new Vector3(50f, 5.6f, 3f)),
                new WallSegmentSpec(new Vector3(34f, 2.8f, -34f), new Vector3(50f, 5.6f, 3f)),
                new WallSegmentSpec(new Vector3(-59f, 2.8f, -13f), new Vector3(3f, 5.6f, 42f)),
                new WallSegmentSpec(new Vector3(-59f, 2.8f, 29f), new Vector3(3f, 5.6f, 42f)),
                new WallSegmentSpec(new Vector3(59f, 2.8f, -13f), new Vector3(3f, 5.6f, 42f)),
                new WallSegmentSpec(new Vector3(59f, 2.8f, 29f), new Vector3(3f, 5.6f, 42f)),
                new WallSegmentSpec(new Vector3(-39f, 2.8f, 50f), new Vector3(40f, 5.6f, 3f)),
                new WallSegmentSpec(new Vector3(0f, 2.8f, 50f), new Vector3(38f, 5.6f, 3f)),
                new WallSegmentSpec(new Vector3(39f, 2.8f, 50f), new Vector3(40f, 5.6f, 3f))
            };
            for (var index = 0; index < wallSegments.Length; index++)
            {
                var wall = wallSegments[index];
                context.AddBox(
                    visual,
                    $"Alderwake warm-stone curtain wall {index + 1}",
                    wall.Position,
                    wall.Scale,
                    Quaternion.identity,
                    FortificationMaterialKey.WarmStone);
                context.AddBox(
                    visual,
                    $"Alderwake timber wall-walk cap {index + 1}",
                    new Vector3(wall.Position.x, 5.72f, wall.Position.z),
                    new Vector3(wall.Scale.x + 0.2f, 0.28f, wall.Scale.z + 0.2f),
                    Quaternion.identity,
                    FortificationMaterialKey.WeatheredTimber);
            }

            context.AddPreferredFacade(
                visual,
                "Wall_UnevenBrick_Straight",
                new Vector3(-24f, 0f, -35.58f),
                Quaternion.identity,
                Vector3.one * 1.25f,
                new Vector3(-24f, 2.2f, -35.58f),
                new Vector3(7f, 4.4f, 0.24f),
                FortificationMaterialKey.WarmStone);
            context.AddPreferredFacade(
                visual,
                "Wall_Plaster_WoodGrid",
                new Vector3(-60.58f, 0f, -8f),
                Quaternion.Euler(0f, 90f, 0f),
                Vector3.one * 1.2f,
                new Vector3(-60.58f, 2.25f, -8f),
                new Vector3(7f, 4.5f, 0.24f),
                FortificationMaterialKey.WeatheredTimber);
            context.AddPreferredFacade(
                visual,
                "Wall_UnevenBrick_Window_Wide_Round",
                new Vector3(-40f, 0f, 51.58f),
                Quaternion.Euler(0f, 180f, 0f),
                Vector3.one * 1.2f,
                new Vector3(-40f, 2.25f, 51.58f),
                new Vector3(7f, 4.5f, 0.24f),
                FortificationMaterialKey.WarmStone);
            context.AddPreferredFacade(
                visual,
                "Wall_Arch",
                new Vector3(0f, 5.1f, -35.7f),
                Quaternion.identity,
                Vector3.one * 0.9f,
                new Vector3(0f, 7.35f, -35.7f),
                new Vector3(7.2f, 1.25f, 0.28f),
                FortificationMaterialKey.Copper);

            AddParapetPair(
                context,
                visual,
                gameplay,
                CityCatalog.AlderwakeHearthmarket.id,
                "Alderwake west wall",
                new Vector3(-59f, 6.45f, 8f),
                84f,
                false,
                FortificationMaterialKey.WeatheredTimber);
            AddParapetPair(
                context,
                visual,
                gameplay,
                CityCatalog.AlderwakeHearthmarket.id,
                "Alderwake east wall",
                new Vector3(59f, 6.45f, 8f),
                84f,
                false,
                FortificationMaterialKey.WeatheredTimber);
            AddParapetPair(
                context,
                visual,
                gameplay,
                CityCatalog.AlderwakeHearthmarket.id,
                "Alderwake northwest wall",
                new Vector3(-39f, 6.45f, 50f),
                40f,
                true,
                FortificationMaterialKey.WeatheredTimber);
            AddParapetPair(
                context,
                visual,
                gameplay,
                CityCatalog.AlderwakeHearthmarket.id,
                "Alderwake northeast wall",
                new Vector3(39f, 6.45f, 50f),
                40f,
                true,
                FortificationMaterialKey.WeatheredTimber);

            AddAlderwakeAlcoves(context, visual);
            AddAlderwakeTerraces(context, visual);

            var towerPositions = new[]
            {
                new Vector3(-59f, 0f, -34f),
                new Vector3(-12f, 0f, -34f),
                new Vector3(12f, 0f, -34f),
                new Vector3(-59f, 0f, 50f),
                new Vector3(59f, 0f, 50f),
                new Vector3(59f, 0f, -34f)
            };
            var towerHeights = new[] { 8.2f, 9.4f, 10.1f, 9.1f, 10f, 8.7f };
            for (var index = 0; index < towerPositions.Length; index++)
            {
                AddAlderwakeTower(
                    context,
                    visual,
                    towerPositions[index],
                    towerHeights[index],
                    index);
            }

            AddAlderwakeGatehouse(context, visual, gameplay);
            AddAlderwakeRoutes(context, cityRoot, visual, gameplay, towerHeights[3], towerHeights[4]);

            AddBanner(
                context,
                visual,
                "Alderwake west gate banner",
                new Vector3(-15.5f, 8.7f, -35.6f),
                FortificationMaterialKey.Moss);
            AddBanner(
                context,
                visual,
                "Alderwake east gate banner",
                new Vector3(15.5f, 9.1f, -35.6f),
                FortificationMaterialKey.Copper);
            AddBanner(
                context,
                visual,
                "Alderwake northwest watch banner",
                new Vector3(-55f, 8.4f, 48.3f),
                FortificationMaterialKey.Moss);
            AddBanner(
                context,
                visual,
                "Alderwake northeast watch banner",
                new Vector3(55f, 8.8f, 48.3f),
                FortificationMaterialKey.WeatheredTimber);

            AddAlderwakeGameplayShell(context, gameplay);
            FinishCity(
                context,
                cityRoot,
                visual,
                gameplay,
                CityCatalog.AlderwakeHearthmarket,
                TetherfallCityFortificationStyle.AlderwakeTerracedCitadel,
                "repaired-copper skyline gate",
                wallSegments.Length,
                6,
                towerPositions.Length,
                3,
                15.8f);
        }

        private static void BuildVeyrwater(BuildContext context)
        {
            var cityRoot = CreateCityRoot(
                context,
                CityCatalog.VeyrwaterMooring,
                TetherfallCityFortificationStyle.VeyrwaterFloodwallMooring,
                out var visual,
                out var gameplay);

            var wallSegments = new[]
            {
                new WallSegmentSpec(new Vector3(-21.5f, 3f, -40f), new Vector3(25f, 6f, 3f)),
                new WallSegmentSpec(new Vector3(22.5f, 3f, -40f), new Vector3(27f, 6f, 3f)),
                new WallSegmentSpec(new Vector3(-34f, 3f, -17.5f), new Vector3(3f, 6f, 45f)),
                new WallSegmentSpec(new Vector3(-34f, 3f, 27.5f), new Vector3(3f, 6f, 45f)),
                new WallSegmentSpec(new Vector3(36f, 3f, -17.5f), new Vector3(3f, 6f, 45f)),
                new WallSegmentSpec(new Vector3(36f, 3f, 27.5f), new Vector3(3f, 6f, 45f)),
                new WallSegmentSpec(new Vector3(-17f, 3f, 50f), new Vector3(34f, 6f, 3f)),
                new WallSegmentSpec(new Vector3(18f, 3f, 50f), new Vector3(36f, 6f, 3f))
            };
            for (var index = 0; index < wallSegments.Length; index++)
            {
                var wall = wallSegments[index];
                context.AddBox(
                    visual,
                    $"Veyrwater dark floodwall {index + 1}",
                    wall.Position,
                    wall.Scale,
                    Quaternion.identity,
                    index % 3 == 0
                        ? FortificationMaterialKey.DarkTimber
                        : FortificationMaterialKey.DarkStone);
                context.AddBox(
                    visual,
                    $"Veyrwater raised wall-walk cap {index + 1}",
                    new Vector3(wall.Position.x, 6.12f, wall.Position.z),
                    new Vector3(wall.Scale.x + 0.2f, 0.3f, wall.Scale.z + 0.2f),
                    Quaternion.identity,
                    FortificationMaterialKey.DarkTimber);
            }

            context.AddPreferredFacade(
                visual,
                "Wall_UnevenBrick_Straight",
                new Vector3(-21f, 0f, -41.58f),
                Quaternion.identity,
                Vector3.one * 1.2f,
                new Vector3(-21f, 2.35f, -41.58f),
                new Vector3(7f, 4.7f, 0.24f),
                FortificationMaterialKey.DarkStone);
            context.AddPreferredFacade(
                visual,
                "Wall_Plaster_WoodGrid",
                new Vector3(-35.58f, 0f, -7f),
                Quaternion.Euler(0f, 90f, 0f),
                Vector3.one * 1.15f,
                new Vector3(-35.58f, 2.35f, -7f),
                new Vector3(7f, 4.7f, 0.24f),
                FortificationMaterialKey.DarkTimber);
            context.AddPreferredFacade(
                visual,
                "Wall_UnevenBrick_Window_Wide_Round",
                new Vector3(18f, 0f, 51.58f),
                Quaternion.Euler(0f, 180f, 0f),
                Vector3.one * 1.15f,
                new Vector3(18f, 2.35f, 51.58f),
                new Vector3(7f, 4.7f, 0.24f),
                FortificationMaterialKey.DarkStone);
            context.AddPreferredFacade(
                visual,
                "Wall_Arch",
                new Vector3(34.4f, 4.4f, 26f),
                Quaternion.Euler(0f, 90f, 0f),
                Vector3.one,
                new Vector3(34.4f, 6.8f, 26f),
                new Vector3(7.5f, 1.4f, 0.28f),
                FortificationMaterialKey.FloodIron);

            AddParapetPair(
                context,
                visual,
                gameplay,
                CityCatalog.VeyrwaterMooring.id,
                "Veyrwater west floodwall",
                new Vector3(-34f, 6.85f, 5f),
                90f,
                false,
                FortificationMaterialKey.FloodIron);
            AddParapetPair(
                context,
                visual,
                gameplay,
                CityCatalog.VeyrwaterMooring.id,
                "Veyrwater east sluice walk",
                new Vector3(36f, 6.85f, 30f),
                40f,
                false,
                FortificationMaterialKey.FloodIron);
            AddParapetPair(
                context,
                visual,
                gameplay,
                CityCatalog.VeyrwaterMooring.id,
                "Veyrwater northwest floodwall",
                new Vector3(-17f, 6.85f, 50f),
                34f,
                true,
                FortificationMaterialKey.FloodIron);
            AddParapetPair(
                context,
                visual,
                gameplay,
                CityCatalog.VeyrwaterMooring.id,
                "Veyrwater northeast floodwall",
                new Vector3(18f, 6.85f, 50f),
                36f,
                true,
                FortificationMaterialKey.FloodIron);

            AddCanalBattlements(context, visual);
            AddVeyrwaterAlcoves(context, visual);
            AddVeyrwaterButtresses(context, visual);

            var towerPositions = new[]
            {
                new Vector3(-34f, 0f, -40f),
                new Vector3(36f, 0f, -40f),
                new Vector3(-34f, 0f, 5f),
                new Vector3(-34f, 0f, 50f),
                new Vector3(36f, 0f, 50f)
            };
            var towerHeights = new[] { 9f, 10f, 11f, 11.8f, 12.5f };
            for (var index = 0; index < towerPositions.Length; index++)
            {
                AddVeyrwaterTower(
                    context,
                    visual,
                    towerPositions[index],
                    towerHeights[index],
                    index);
            }

            AddVeyrwaterSluice(context, visual, gameplay);
            AddVeyrwaterRoutes(context, cityRoot, visual, gameplay, towerHeights[3], towerHeights[4]);
            AddVeyrwaterSafeMarkers(context, visual);
            AddVeyrwaterGameplayShell(context, gameplay);

            FinishCity(
                context,
                cityRoot,
                visual,
                gameplay,
                CityCatalog.VeyrwaterMooring,
                TetherfallCityFortificationStyle.VeyrwaterFloodwallMooring,
                "sluice-chain bridge gate",
                wallSegments.Length + 4,
                5,
                towerPositions.Length,
                3,
                16.9f);
        }

        private static Transform CreateCityRoot(
            BuildContext context,
            CityDefinition city,
            TetherfallCityFortificationStyle style,
            out Transform visual,
            out Transform gameplay)
        {
            var root = new GameObject(city.displayName + " Fortifications").transform;
            root.SetParent(context.Root, false);
            root.position = city.center;
            root.rotation = Quaternion.identity;
            root.localScale = Vector3.one;
            root.gameObject.AddComponent<TetherfallCityFortificationCity>();

            visual = new GameObject(VisualRootName).transform;
            visual.SetParent(root, false);
            gameplay = new GameObject(GameplayRootName).transform;
            gameplay.SetParent(root, false);
            return root;
        }

        private static void FinishCity(
            BuildContext context,
            Transform cityRoot,
            Transform visual,
            Transform gameplay,
            CityDefinition city,
            TetherfallCityFortificationStyle style,
            string gatehouseSignature,
            int wallSegmentCount,
            int walkableSegmentCount,
            int towerCount,
            int guardAlcoveCount,
            float skylineHeight)
        {
            var cityMarker = cityRoot.GetComponent<TetherfallCityFortificationCity>();
            cityMarker.Configure(
                city,
                style,
                gatehouseSignature,
                wallSegmentCount,
                walkableSegmentCount,
                towerCount,
                guardAlcoveCount,
                skylineHeight,
                visual,
                gameplay);

            var cluster = visual.gameObject.AddComponent<TetherfallCityFortificationCullingCluster>();
            cluster.Configure(city.id, visual.GetComponentsInChildren<Renderer>(true));
            context.Clusters.Add(cluster);
        }

        private static void AddAlderwakeAlcoves(BuildContext context, Transform visual)
        {
            var positions = new[]
            {
                new Vector3(-56.9f, 4.3f, -2f),
                new Vector3(56.9f, 4.3f, 18f),
                new Vector3(-22f, 4.3f, 47.9f)
            };
            for (var index = 0; index < positions.Length; index++)
            {
                context.AddBox(
                    visual,
                    $"Alderwake guard alcove timber back {index + 1}",
                    positions[index],
                    new Vector3(index == 2 ? 5f : 0.7f, 2.3f, index == 2 ? 0.7f : 5f),
                    Quaternion.identity,
                    FortificationMaterialKey.WeatheredTimber);
                context.AddBox(
                    visual,
                    $"Alderwake guard alcove moss hood {index + 1}",
                    positions[index] + Vector3.up * 1.45f,
                    new Vector3(index == 2 ? 5.5f : 1.1f, 0.34f, index == 2 ? 1.1f : 5.5f),
                    Quaternion.Euler(index == 2 ? 3f : 0f, 0f, index == 2 ? 0f : 3f),
                    FortificationMaterialKey.Moss);
            }
        }

        private static void AddAlderwakeTerraces(BuildContext context, Transform visual)
        {
            var positions = new[]
            {
                new Vector3(-53f, 1.2f, -29f),
                new Vector3(-54f, 1.5f, 34f),
                new Vector3(-33f, 1.1f, 46f),
                new Vector3(53f, 1.3f, -27f),
                new Vector3(54f, 1.6f, 30f),
                new Vector3(34f, 1.2f, 46f)
            };
            for (var index = 0; index < positions.Length; index++)
            {
                var alongSide = Mathf.Abs(positions[index].x) > 45f;
                context.AddBox(
                    visual,
                    $"Alderwake moss defensive terrace {index + 1}",
                    positions[index],
                    alongSide
                        ? new Vector3(8f, 2.4f + index % 2 * 0.5f, 5.5f)
                        : new Vector3(6.5f, 2.2f + index % 2 * 0.45f, 7f),
                    Quaternion.Euler(0f, index % 2 == 0 ? -4f : 4f, 0f),
                    index % 2 == 0
                        ? FortificationMaterialKey.Moss
                        : FortificationMaterialKey.WeatheredTimber);
            }
        }

        private static void AddAlderwakeTower(
            BuildContext context,
            Transform visual,
            Vector3 basePosition,
            float height,
            int variant)
        {
            var bodyType = variant % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Cylinder;
            var bodyScale = bodyType == PrimitiveType.Cylinder
                ? new Vector3(3.4f, height * 0.5f, 3.4f)
                : new Vector3(6.6f, height, 6.6f);
            context.AddPrimitive(
                visual,
                bodyType,
                $"Alderwake varied watch tower {variant + 1}",
                basePosition + Vector3.up * (height * 0.5f),
                bodyScale,
                Quaternion.identity,
                variant % 3 == 0
                    ? FortificationMaterialKey.WeatheredTimber
                    : FortificationMaterialKey.WarmStone);
            context.AddPrimitive(
                visual,
                variant % 2 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube,
                $"Alderwake watch platform {variant + 1}",
                basePosition + Vector3.up * (height + 0.12f),
                variant % 2 == 0
                    ? new Vector3(4.2f, 0.18f, 4.2f)
                    : new Vector3(8f, 0.36f, 8f),
                Quaternion.identity,
                FortificationMaterialKey.Copper);
            context.AddBox(
                visual,
                $"Alderwake pitched moss roof left {variant + 1}",
                basePosition + new Vector3(-1.55f, height + 1.25f, 0f),
                new Vector3(4.1f, 0.42f, 8.4f),
                Quaternion.Euler(0f, 0f, -24f),
                FortificationMaterialKey.Moss);
            context.AddBox(
                visual,
                $"Alderwake pitched moss roof right {variant + 1}",
                basePosition + new Vector3(1.55f, height + 1.25f, 0f),
                new Vector3(4.1f, 0.42f, 8.4f),
                Quaternion.Euler(0f, 0f, 24f),
                variant % 2 == 0
                    ? FortificationMaterialKey.Moss
                    : FortificationMaterialKey.WeatheredTimber);
        }

        private static void AddAlderwakeGatehouse(
            BuildContext context,
            Transform visual,
            Transform gameplay)
        {
            const string cityId = "city.alderwake.hearthmarket";
            for (var side = -1; side <= 1; side += 2)
            {
                context.AddBox(
                    visual,
                    "Alderwake repaired gate pier",
                    new Vector3(side * 7.5f, 4f, -34f),
                    new Vector3(3f, 8f, 4f),
                    Quaternion.identity,
                    FortificationMaterialKey.WarmStone);
                context.AddBox(
                    visual,
                    "Alderwake copper gate repair band",
                    new Vector3(side * 5.85f, 4.5f, -35.95f),
                    new Vector3(0.42f, 7f, 0.24f),
                    Quaternion.identity,
                    FortificationMaterialKey.Copper);
                context.AddBox(
                    visual,
                    "Alderwake gatehouse pitched roof",
                    new Vector3(side * 7.5f, 9.1f, -34f),
                    new Vector3(5.5f, 0.55f, 6.3f),
                    Quaternion.Euler(0f, 0f, side * 14f),
                    FortificationMaterialKey.Moss);
                context.AddCollider(
                    gameplay,
                    "Alderwake gate pier collision",
                    new Vector3(side * 7.5f, 4f, -34f),
                    new Vector3(3f, 8f, 4f),
                    Quaternion.identity,
                    cityId,
                    TetherfallCityFortificationColliderRole.Gatehouse);
            }

            context.AddBox(
                visual,
                "Alderwake repaired copper gate lintel",
                new Vector3(0f, 8.05f, -34f),
                new Vector3(18f, 1.7f, 4f),
                Quaternion.identity,
                FortificationMaterialKey.Copper);
            for (var side = -1; side <= 1; side += 2)
            {
                context.AddBox(
                    visual,
                    "Alderwake skyline focal frame post",
                    new Vector3(side * 4.7f, 12.05f, -34f),
                    new Vector3(0.46f, 6.8f, 0.55f),
                    Quaternion.identity,
                    FortificationMaterialKey.Copper);
            }

            context.AddBox(
                visual,
                "Alderwake skyline focal frame crown",
                new Vector3(0f, 15.45f, -34f),
                new Vector3(10f, 0.52f, 0.68f),
                Quaternion.identity,
                FortificationMaterialKey.Copper);
            context.AddSphere(
                visual,
                "Alderwake restrained teal gate bearing",
                new Vector3(0f, 13.85f, -34.38f),
                Vector3.one * 0.62f,
                Quaternion.identity,
                FortificationMaterialKey.SafeTeal);
        }

        private static void AddAlderwakeRoutes(
            BuildContext context,
            Transform cityRoot,
            Transform visual,
            Transform gameplay,
            float westTowerHeight,
            float eastTowerHeight)
        {
            const string cityId = "city.alderwake.hearthmarket";
            const string westRoute = "fortification.alderwake.west_rain_stair";
            const string eastRoute = "fortification.alderwake.east_copper_ramp";

            AddStairFlight(
                context,
                visual,
                gameplay,
                cityId,
                westRoute,
                "Alderwake west rain stair",
                new Vector3(-49f, 0f, -24f),
                Vector3.left,
                9f,
                2.8f,
                3.2f,
                FortificationMaterialKey.WeatheredTimber);
            AddRamp(
                context,
                visual,
                gameplay,
                cityId,
                westRoute,
                "Alderwake west wall switchback",
                new Vector3(-58f, 2.8f, -24f),
                new Vector3(-59f, 5.85f, -14f),
                3.2f,
                FortificationMaterialKey.Copper);
            AddRamp(
                context,
                visual,
                gameplay,
                cityId,
                westRoute,
                "Alderwake northwest tower rise",
                new Vector3(-59f, 5.85f, 39f),
                new Vector3(-59f, westTowerHeight + 0.24f, 48f),
                3.1f,
                FortificationMaterialKey.WeatheredTimber);
            AddRamp(
                context,
                visual,
                gameplay,
                cityId,
                westRoute,
                "Alderwake northwest return ramp",
                new Vector3(-39f, 5.85f, 49f),
                new Vector3(-31f, 0.12f, 39f),
                3.2f,
                FortificationMaterialKey.WeatheredTimber);

            AddStairFlight(
                context,
                visual,
                gameplay,
                cityId,
                eastRoute,
                "Alderwake east timber stair",
                new Vector3(49f, 0f, -21f),
                Vector3.right,
                9f,
                2.8f,
                3.2f,
                FortificationMaterialKey.WeatheredTimber);
            AddRamp(
                context,
                visual,
                gameplay,
                cityId,
                eastRoute,
                "Alderwake east wall copper ramp",
                new Vector3(58f, 2.8f, -21f),
                new Vector3(59f, 5.85f, -11f),
                3.2f,
                FortificationMaterialKey.Copper);
            AddRamp(
                context,
                visual,
                gameplay,
                cityId,
                eastRoute,
                "Alderwake northeast tower rise",
                new Vector3(59f, 5.85f, 39f),
                new Vector3(59f, eastTowerHeight + 0.24f, 48f),
                3.1f,
                FortificationMaterialKey.WeatheredTimber);
            AddRamp(
                context,
                visual,
                gameplay,
                cityId,
                eastRoute,
                "Alderwake northeast return ramp",
                new Vector3(39f, 5.85f, 49f),
                new Vector3(31f, 0.12f, 39f),
                3.2f,
                FortificationMaterialKey.WeatheredTimber);

            context.AddClimbableFace(
                gameplay,
                "Alderwake west authored climb face",
                new Vector3(-57.38f, 2.7f, 6f),
                new Vector3(0.3f, 5.4f, 4.2f),
                Quaternion.identity,
                cityId,
                westRoute);
            context.AddClimbableFace(
                gameplay,
                "Alderwake east authored climb face",
                new Vector3(57.38f, 2.7f, 8f),
                new Vector3(0.3f, 5.4f, 4.2f),
                Quaternion.identity,
                cityId,
                eastRoute);
            context.AddCollider(
                gameplay,
                "Alderwake northwest tower overlook",
                new Vector3(-59f, westTowerHeight + 0.16f, 50f),
                new Vector3(7f, 0.35f, 7f),
                Quaternion.identity,
                cityId,
                TetherfallCityFortificationColliderRole.TowerOverlook,
                westRoute);
            context.AddCollider(
                gameplay,
                "Alderwake northeast tower overlook",
                new Vector3(59f, eastTowerHeight + 0.16f, 50f),
                new Vector3(7f, 0.35f, 7f),
                Quaternion.identity,
                cityId,
                TetherfallCityFortificationColliderRole.TowerOverlook,
                eastRoute);

            AddRouteMetadata(
                gameplay,
                cityRoot,
                cityId,
                westRoute,
                "overlook.alderwake.northwest_watch",
                new[]
                {
                    new Vector3(-49f, 0.1f, -24f),
                    new Vector3(-53.5f, 1.4f, -24f),
                    new Vector3(-58f, 2.8f, -24f),
                    new Vector3(-59f, 5.85f, -14f),
                    new Vector3(-59f, 5.85f, 4f),
                    new Vector3(-59f, 5.85f, 22f),
                    new Vector3(-59f, 5.85f, 39f),
                    new Vector3(-59f, westTowerHeight + 0.24f, 48f),
                    new Vector3(-59f, westTowerHeight + 0.24f, 50f),
                    new Vector3(-50f, 5.85f, 50f),
                    new Vector3(-39f, 5.85f, 49f),
                    new Vector3(-35f, 2.95f, 44f),
                    new Vector3(-31f, 0.12f, 39f)
                });
            AddRouteMetadata(
                gameplay,
                cityRoot,
                cityId,
                eastRoute,
                "overlook.alderwake.northeast_watch",
                new[]
                {
                    new Vector3(49f, 0.1f, -21f),
                    new Vector3(53.5f, 1.4f, -21f),
                    new Vector3(58f, 2.8f, -21f),
                    new Vector3(59f, 5.85f, -11f),
                    new Vector3(59f, 5.85f, 7f),
                    new Vector3(59f, 5.85f, 24f),
                    new Vector3(59f, 5.85f, 39f),
                    new Vector3(59f, eastTowerHeight + 0.24f, 48f),
                    new Vector3(59f, eastTowerHeight + 0.24f, 50f),
                    new Vector3(50f, 5.85f, 50f),
                    new Vector3(39f, 5.85f, 49f),
                    new Vector3(35f, 2.95f, 44f),
                    new Vector3(31f, 0.12f, 39f)
                });
        }

        private static void AddAlderwakeGameplayShell(
            BuildContext context,
            Transform gameplay)
        {
            const string cityId = "city.alderwake.hearthmarket";
            var bodies = new[]
            {
                new WallSegmentSpec(new Vector3(-34f, 2.8f, -34f), new Vector3(50f, 5.6f, 3f)),
                new WallSegmentSpec(new Vector3(34f, 2.8f, -34f), new Vector3(50f, 5.6f, 3f)),
                new WallSegmentSpec(new Vector3(-59f, 2.8f, 8f), new Vector3(3f, 5.6f, 84f)),
                new WallSegmentSpec(new Vector3(59f, 2.8f, 8f), new Vector3(3f, 5.6f, 84f)),
                new WallSegmentSpec(new Vector3(-39f, 2.8f, 50f), new Vector3(40f, 5.6f, 3f)),
                new WallSegmentSpec(new Vector3(0f, 2.8f, 50f), new Vector3(38f, 5.6f, 3f)),
                new WallSegmentSpec(new Vector3(39f, 2.8f, 50f), new Vector3(40f, 5.6f, 3f))
            };
            for (var index = 0; index < bodies.Length; index++)
            {
                context.AddCollider(
                    gameplay,
                    $"Alderwake deliberate wall body {index + 1}",
                    bodies[index].Position,
                    bodies[index].Scale,
                    Quaternion.identity,
                    cityId,
                    TetherfallCityFortificationColliderRole.WallBody);
            }

            var walks = new[]
            {
                new WallSegmentSpec(new Vector3(-59f, 5.75f, 8f), new Vector3(3.1f, 0.3f, 84f)),
                new WallSegmentSpec(new Vector3(59f, 5.75f, 8f), new Vector3(3.1f, 0.3f, 84f)),
                new WallSegmentSpec(new Vector3(-39f, 5.75f, 50f), new Vector3(40f, 0.3f, 3.1f)),
                new WallSegmentSpec(new Vector3(39f, 5.75f, 50f), new Vector3(40f, 0.3f, 3.1f))
            };
            for (var index = 0; index < walks.Length; index++)
            {
                context.AddCollider(
                    gameplay,
                    $"Alderwake walkable wall deck {index + 1}",
                    walks[index].Position,
                    walks[index].Scale,
                    Quaternion.identity,
                    cityId,
                    TetherfallCityFortificationColliderRole.WallWalk);
            }
        }

        private static void AddCanalBattlements(BuildContext context, Transform visual)
        {
            var positions = new[]
            {
                new Vector3(-13f, 2.2f, 18f),
                new Vector3(13f, 2.2f, 18f),
                new Vector3(-13f, 2.2f, 34f),
                new Vector3(13f, 2.2f, 34f)
            };
            for (var index = 0; index < positions.Length; index++)
            {
                context.AddBox(
                    visual,
                    $"Veyrwater canal-edge battlement {index + 1}",
                    positions[index],
                    new Vector3(14f, 4.4f, 1.2f),
                    Quaternion.identity,
                    index % 2 == 0
                        ? FortificationMaterialKey.DarkStone
                        : FortificationMaterialKey.DarkTimber);
                context.AddBox(
                    visual,
                    $"Veyrwater canal battlement reed cap {index + 1}",
                    positions[index] + Vector3.up * 2.35f,
                    new Vector3(14.4f, 0.3f, 1.6f),
                    Quaternion.identity,
                    FortificationMaterialKey.ReedRoof);
            }
        }

        private static void AddVeyrwaterAlcoves(BuildContext context, Transform visual)
        {
            var positions = new[]
            {
                new Vector3(-31.9f, 4.7f, 16f),
                new Vector3(33.9f, 4.7f, 11f),
                new Vector3(-8f, 4.7f, 47.9f)
            };
            for (var index = 0; index < positions.Length; index++)
            {
                context.AddBox(
                    visual,
                    $"Veyrwater floodwatch alcove back {index + 1}",
                    positions[index],
                    new Vector3(index == 2 ? 5.2f : 0.72f, 2.5f, index == 2 ? 0.72f : 5.2f),
                    Quaternion.identity,
                    FortificationMaterialKey.DarkTimber);
                context.AddBox(
                    visual,
                    $"Veyrwater reed alcove roof {index + 1}",
                    positions[index] + Vector3.up * 1.55f,
                    new Vector3(index == 2 ? 5.8f : 1.15f, 0.35f, index == 2 ? 1.15f : 5.8f),
                    Quaternion.Euler(index == 2 ? 4f : 0f, 0f, index == 2 ? 0f : -4f),
                    FortificationMaterialKey.ReedRoof);
            }
        }

        private static void AddVeyrwaterButtresses(BuildContext context, Transform visual)
        {
            var positions = new[]
            {
                new Vector3(-30f, 1.5f, -35f),
                new Vector3(-30f, 1.8f, 34f),
                new Vector3(-19f, 1.45f, 46f),
                new Vector3(32f, 1.55f, -34f),
                new Vector3(32f, 1.85f, 32f),
                new Vector3(21f, 1.5f, 46f)
            };
            for (var index = 0; index < positions.Length; index++)
            {
                context.AddBox(
                    visual,
                    $"Veyrwater flood-defense buttress {index + 1}",
                    positions[index],
                    new Vector3(5.8f, 3f + index % 2 * 0.6f, 5.2f),
                    Quaternion.Euler(0f, index % 2 == 0 ? 8f : -8f, 0f),
                    index % 2 == 0
                        ? FortificationMaterialKey.DarkStone
                        : FortificationMaterialKey.DarkTimber);
            }
        }

        private static void AddVeyrwaterTower(
            BuildContext context,
            Transform visual,
            Vector3 basePosition,
            float height,
            int variant)
        {
            context.AddPrimitive(
                visual,
                variant % 2 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube,
                $"Veyrwater lantern watch tower {variant + 1}",
                basePosition + Vector3.up * (height * 0.5f),
                variant % 2 == 0
                    ? new Vector3(3.2f, height * 0.5f, 3.2f)
                    : new Vector3(6.2f, height, 6.2f),
                Quaternion.identity,
                variant % 2 == 0
                    ? FortificationMaterialKey.DarkStone
                    : FortificationMaterialKey.DarkTimber);
            context.AddPrimitive(
                visual,
                variant % 2 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube,
                $"Veyrwater lantern watch platform {variant + 1}",
                basePosition + Vector3.up * (height + 0.12f),
                variant % 2 == 0
                    ? new Vector3(4f, 0.18f, 4f)
                    : new Vector3(7.4f, 0.36f, 7.4f),
                Quaternion.identity,
                FortificationMaterialKey.FloodIron);
            context.AddBox(
                visual,
                $"Veyrwater reed watch roof {variant + 1}",
                basePosition + new Vector3(0f, height + 1.2f, 0f),
                new Vector3(7.8f, 0.42f, 7.8f),
                Quaternion.Euler(0f, variant * 7f, variant % 2 == 0 ? 7f : -7f),
                FortificationMaterialKey.ReedRoof);
            context.AddPrimitive(
                visual,
                PrimitiveType.Cylinder,
                $"Veyrwater lantern mast {variant + 1}",
                basePosition + new Vector3(0f, height + 2.3f, 0f),
                new Vector3(0.09f, 1.8f, 0.09f),
                Quaternion.identity,
                FortificationMaterialKey.FloodIron);
            context.AddSphere(
                visual,
                $"Veyrwater shielded lantern {variant + 1}",
                basePosition + new Vector3(0f, height + 4.15f, 0f),
                Vector3.one * (0.48f + variant * 0.025f),
                Quaternion.identity,
                FortificationMaterialKey.LanternAmber);
        }

        private static void AddVeyrwaterSluice(
            BuildContext context,
            Transform visual,
            Transform gameplay)
        {
            const string cityId = "city.morrowfen.veyrwater";
            for (var side = -1; side <= 1; side += 2)
            {
                var z = 26f + side * 6f;
                context.AddBox(
                    visual,
                    "Veyrwater sluice gatehouse pier",
                    new Vector3(36f, 5f, z),
                    new Vector3(5f, 10f, 4f),
                    Quaternion.identity,
                    FortificationMaterialKey.DarkStone);
                context.AddBox(
                    visual,
                    "Veyrwater sluice reed roof",
                    new Vector3(36f, 10.7f, z),
                    new Vector3(7f, 0.5f, 6f),
                    Quaternion.Euler(side * 6f, 0f, 0f),
                    FortificationMaterialKey.ReedRoof);
                context.AddBox(
                    visual,
                    "Veyrwater hanging sluice chain",
                    new Vector3(33f, 5.6f, z),
                    new Vector3(0.22f, 7.5f, 0.22f),
                    Quaternion.identity,
                    FortificationMaterialKey.FloodIron);
                context.AddBox(
                    visual,
                    "Veyrwater sluice counterweight",
                    new Vector3(33f, 2.1f, z),
                    new Vector3(1.1f, 2.2f, 1.1f),
                    Quaternion.identity,
                    FortificationMaterialKey.DarkStone);
                context.AddCollider(
                    gameplay,
                    "Veyrwater sluice pier collision",
                    new Vector3(36f, 4f, z),
                    new Vector3(3.5f, 8f, 4f),
                    Quaternion.identity,
                    cityId,
                    TetherfallCityFortificationColliderRole.Gatehouse);
            }

            context.AddBox(
                visual,
                "Veyrwater sluice gatehouse lintel",
                new Vector3(36f, 10f, 26f),
                new Vector3(5f, 1.7f, 16f),
                Quaternion.identity,
                FortificationMaterialKey.FloodIron);
            context.AddPrimitive(
                visual,
                PrimitiveType.Cylinder,
                "Veyrwater sluice wheel",
                new Vector3(32.7f, 7.1f, 26f),
                new Vector3(1.9f, 0.2f, 1.9f),
                Quaternion.Euler(0f, 0f, 90f),
                FortificationMaterialKey.Copper);
            context.AddBox(
                visual,
                "Veyrwater sluice wheel vertical spoke",
                new Vector3(32.45f, 7.1f, 26f),
                new Vector3(0.22f, 3.8f, 0.22f),
                Quaternion.identity,
                FortificationMaterialKey.Copper);
            context.AddBox(
                visual,
                "Veyrwater sluice wheel cross spoke",
                new Vector3(32.45f, 7.1f, 26f),
                new Vector3(0.22f, 0.22f, 3.8f),
                Quaternion.identity,
                FortificationMaterialKey.Copper);
            context.AddBox(
                visual,
                "Veyrwater raised chain bridge",
                new Vector3(30f, 6.25f, 26f),
                new Vector3(12f, 0.35f, 4f),
                Quaternion.Euler(0f, 0f, 2f),
                FortificationMaterialKey.DarkTimber);
            context.AddCollider(
                gameplay,
                "Veyrwater safe sluice bridge deck",
                new Vector3(30f, 6.25f, 26f),
                new Vector3(12f, 0.35f, 4f),
                Quaternion.Euler(0f, 0f, 2f),
                cityId,
                TetherfallCityFortificationColliderRole.SafeCrossing);
        }

        private static void AddVeyrwaterRoutes(
            BuildContext context,
            Transform cityRoot,
            Transform visual,
            Transform gameplay,
            float westTowerHeight,
            float eastTowerHeight)
        {
            const string cityId = "city.morrowfen.veyrwater";
            const string westRoute = "fortification.veyrwater.west_flood_stair";
            const string northRoute = "fortification.veyrwater.north_reed_ramp";

            AddStairFlight(
                context,
                visual,
                gameplay,
                cityId,
                westRoute,
                "Veyrwater west flood stair",
                new Vector3(-25f, 0f, -15f),
                Vector3.left,
                9f,
                3f,
                3.2f,
                FortificationMaterialKey.DarkTimber);
            AddRamp(
                context,
                visual,
                gameplay,
                cityId,
                westRoute,
                "Veyrwater west floodwall switchback",
                new Vector3(-34f, 3f, -15f),
                new Vector3(-34f, 6.2f, -5f),
                3.2f,
                FortificationMaterialKey.DarkTimber);
            AddRamp(
                context,
                visual,
                gameplay,
                cityId,
                westRoute,
                "Veyrwater northwest lantern rise",
                new Vector3(-34f, 6.2f, 38f),
                new Vector3(-34f, westTowerHeight + 0.24f, 48f),
                3.1f,
                FortificationMaterialKey.ReedRoof);
            AddRamp(
                context,
                visual,
                gameplay,
                cityId,
                westRoute,
                "Veyrwater northwest return ramp",
                new Vector3(-24f, 6.2f, 49f),
                new Vector3(-27f, 0.12f, 37f),
                3.2f,
                FortificationMaterialKey.DarkTimber);

            AddStairFlight(
                context,
                visual,
                gameplay,
                cityId,
                northRoute,
                "Veyrwater north reed stair",
                new Vector3(23f, 0f, 41f),
                Vector3.forward,
                9f,
                3f,
                3.2f,
                FortificationMaterialKey.ReedRoof);
            AddRamp(
                context,
                visual,
                gameplay,
                cityId,
                northRoute,
                "Veyrwater north floodwall ramp",
                new Vector3(23f, 3f, 50f),
                new Vector3(30f, 6.2f, 50f),
                3.2f,
                FortificationMaterialKey.DarkTimber);
            AddRamp(
                context,
                visual,
                gameplay,
                cityId,
                northRoute,
                "Veyrwater northeast lantern rise",
                new Vector3(20f, 6.2f, 50f),
                new Vector3(35f, eastTowerHeight + 0.24f, 50f),
                3.1f,
                FortificationMaterialKey.ReedRoof);
            AddRamp(
                context,
                visual,
                gameplay,
                cityId,
                northRoute,
                "Veyrwater northeast return ramp",
                new Vector3(16f, 6.2f, 49f),
                new Vector3(24f, 0.12f, 38f),
                3.2f,
                FortificationMaterialKey.DarkTimber);

            context.AddClimbableFace(
                gameplay,
                "Veyrwater west authored climb face",
                new Vector3(-32.38f, 2.9f, 10f),
                new Vector3(0.3f, 5.8f, 4.2f),
                Quaternion.identity,
                cityId,
                westRoute);
            context.AddClimbableFace(
                gameplay,
                "Veyrwater north authored climb face",
                new Vector3(18f, 2.9f, 48.38f),
                new Vector3(4.2f, 5.8f, 0.3f),
                Quaternion.identity,
                cityId,
                northRoute);
            context.AddCollider(
                gameplay,
                "Veyrwater northwest tower overlook",
                new Vector3(-34f, westTowerHeight + 0.16f, 50f),
                new Vector3(7f, 0.35f, 7f),
                Quaternion.identity,
                cityId,
                TetherfallCityFortificationColliderRole.TowerOverlook,
                westRoute);
            context.AddCollider(
                gameplay,
                "Veyrwater northeast tower overlook",
                new Vector3(36f, eastTowerHeight + 0.16f, 50f),
                new Vector3(7f, 0.35f, 7f),
                Quaternion.identity,
                cityId,
                TetherfallCityFortificationColliderRole.TowerOverlook,
                northRoute);

            AddRouteMetadata(
                gameplay,
                cityRoot,
                cityId,
                westRoute,
                "overlook.veyrwater.northwest_lantern",
                new[]
                {
                    new Vector3(-25f, 0.1f, -15f),
                    new Vector3(-29.5f, 1.5f, -15f),
                    new Vector3(-34f, 3f, -15f),
                    new Vector3(-34f, 6.2f, -5f),
                    new Vector3(-34f, 6.2f, 12f),
                    new Vector3(-34f, 6.2f, 28f),
                    new Vector3(-34f, 6.2f, 38f),
                    new Vector3(-34f, westTowerHeight + 0.24f, 48f),
                    new Vector3(-34f, westTowerHeight + 0.24f, 50f),
                    new Vector3(-24f, 6.2f, 49f),
                    new Vector3(-25.5f, 3.1f, 43f),
                    new Vector3(-27f, 0.12f, 37f)
                });
            AddRouteMetadata(
                gameplay,
                cityRoot,
                cityId,
                northRoute,
                "overlook.veyrwater.northeast_lantern",
                new[]
                {
                    new Vector3(23f, 0.1f, 41f),
                    new Vector3(23f, 1.5f, 45.5f),
                    new Vector3(23f, 3f, 50f),
                    new Vector3(30f, 6.2f, 50f),
                    new Vector3(20f, 6.2f, 50f),
                    new Vector3(28f, 9.3f, 50f),
                    new Vector3(35f, eastTowerHeight + 0.24f, 50f),
                    new Vector3(36f, eastTowerHeight + 0.24f, 50f),
                    new Vector3(27f, 6.2f, 50f),
                    new Vector3(16f, 6.2f, 49f),
                    new Vector3(20f, 3.1f, 43.5f),
                    new Vector3(24f, 0.12f, 38f)
                });
        }

        private static void AddVeyrwaterSafeMarkers(BuildContext context, Transform visual)
        {
            var positions = new[]
            {
                new Vector3(-4.5f, 1.2f, -38.1f),
                new Vector3(4.5f, 1.2f, -38.1f),
                new Vector3(27f, 7.05f, 24.3f),
                new Vector3(27f, 7.05f, 27.7f)
            };
            for (var index = 0; index < positions.Length; index++)
            {
                context.AddSphere(
                    visual,
                    $"Veyrwater safe-crossing teal marker {index + 1}",
                    positions[index],
                    Vector3.one * 0.34f,
                    Quaternion.identity,
                    FortificationMaterialKey.SafeTeal);
            }
        }

        private static void AddVeyrwaterGameplayShell(
            BuildContext context,
            Transform gameplay)
        {
            const string cityId = "city.morrowfen.veyrwater";
            var bodies = new[]
            {
                new WallSegmentSpec(new Vector3(-21.5f, 3f, -40f), new Vector3(25f, 6f, 3f)),
                new WallSegmentSpec(new Vector3(22.5f, 3f, -40f), new Vector3(27f, 6f, 3f)),
                new WallSegmentSpec(new Vector3(-34f, 3f, 5f), new Vector3(3f, 6f, 90f)),
                new WallSegmentSpec(new Vector3(36f, 3f, 5f), new Vector3(3f, 6f, 90f)),
                new WallSegmentSpec(new Vector3(-17f, 3f, 50f), new Vector3(34f, 6f, 3f)),
                new WallSegmentSpec(new Vector3(18f, 3f, 50f), new Vector3(36f, 6f, 3f))
            };
            for (var index = 0; index < bodies.Length; index++)
            {
                context.AddCollider(
                    gameplay,
                    $"Veyrwater deliberate floodwall body {index + 1}",
                    bodies[index].Position,
                    bodies[index].Scale,
                    Quaternion.identity,
                    cityId,
                    TetherfallCityFortificationColliderRole.WallBody);
            }

            var walks = new[]
            {
                new WallSegmentSpec(new Vector3(-34f, 6.15f, 5f), new Vector3(3.1f, 0.3f, 90f)),
                new WallSegmentSpec(new Vector3(36f, 6.15f, 30f), new Vector3(3.1f, 0.3f, 40f)),
                new WallSegmentSpec(new Vector3(-17f, 6.15f, 50f), new Vector3(34f, 0.3f, 3.1f)),
                new WallSegmentSpec(new Vector3(18f, 6.15f, 50f), new Vector3(36f, 0.3f, 3.1f))
            };
            for (var index = 0; index < walks.Length; index++)
            {
                context.AddCollider(
                    gameplay,
                    $"Veyrwater elevated wall walk {index + 1}",
                    walks[index].Position,
                    walks[index].Scale,
                    Quaternion.identity,
                    cityId,
                    TetherfallCityFortificationColliderRole.WallWalk);
            }
        }

        private static void AddParapetPair(
            BuildContext context,
            Transform visual,
            Transform gameplay,
            string cityId,
            string label,
            Vector3 center,
            float length,
            bool alongX,
            FortificationMaterialKey material)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                var offset = alongX
                    ? new Vector3(0f, 0f, side * 1.32f)
                    : new Vector3(side * 1.32f, 0f, 0f);
                var scale = alongX
                    ? new Vector3(length, 1.15f, 0.34f)
                    : new Vector3(0.34f, 1.15f, length);
                context.AddBox(
                    visual,
                    label + " safe parapet",
                    center + offset,
                    scale,
                    Quaternion.identity,
                    material);
                context.AddCollider(
                    gameplay,
                    label + " railing collision",
                    center + offset,
                    scale,
                    Quaternion.identity,
                    cityId,
                    TetherfallCityFortificationColliderRole.Railing);
            }
        }

        private static void AddBanner(
            BuildContext context,
            Transform visual,
            string label,
            Vector3 position,
            FortificationMaterialKey fabricMaterial)
        {
            context.AddPrimitive(
                visual,
                PrimitiveType.Cylinder,
                label + " pole",
                position,
                new Vector3(0.09f, 2.2f, 0.09f),
                Quaternion.identity,
                FortificationMaterialKey.Copper);
            context.AddPrimitive(
                visual,
                PrimitiveType.Quad,
                label + " cloth",
                position + new Vector3(0.68f, 0.45f, 0f),
                new Vector3(1.35f, 1.8f, 1f),
                Quaternion.Euler(0f, 180f, 0f),
                fabricMaterial);
        }

        private static void AddStairFlight(
            BuildContext context,
            Transform visual,
            Transform gameplay,
            string cityId,
            string routeId,
            string label,
            Vector3 start,
            Vector3 direction,
            float run,
            float rise,
            float width,
            FortificationMaterialKey material)
        {
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            var yaw = Quaternion.LookRotation(direction, Vector3.up);
            const int stepCount = 3;
            for (var index = 0; index < stepCount; index++)
            {
                var height = rise * (index + 1) / stepCount;
                context.AddBox(
                    visual,
                    $"{label} step {index + 1}",
                    start +
                    direction * (run * (index + 0.5f) / stepCount) +
                    Vector3.up * (height * 0.5f),
                    new Vector3(width, height, run / stepCount * 0.96f),
                    yaw,
                    material);
            }

            var angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            context.AddCollider(
                gameplay,
                label + " ascent collision",
                start + direction * (run * 0.5f) + Vector3.up * (rise * 0.5f),
                new Vector3(width, 0.42f, Mathf.Sqrt(run * run + rise * rise)),
                yaw * Quaternion.Euler(-angle, 0f, 0f),
                cityId,
                TetherfallCityFortificationColliderRole.Ascent,
                routeId);
        }

        private static void AddRamp(
            BuildContext context,
            Transform visual,
            Transform gameplay,
            string cityId,
            string routeId,
            string label,
            Vector3 start,
            Vector3 end,
            float width,
            FortificationMaterialKey material)
        {
            var delta = end - start;
            var horizontal = new Vector3(delta.x, 0f, delta.z);
            var direction = horizontal.sqrMagnitude > 0.001f
                ? horizontal.normalized
                : Vector3.forward;
            var yaw = Quaternion.LookRotation(direction, Vector3.up);
            var angle = Mathf.Atan2(delta.y, Mathf.Max(0.01f, horizontal.magnitude)) * Mathf.Rad2Deg;
            var rotation = yaw * Quaternion.Euler(-angle, 0f, 0f);
            var scale = new Vector3(width, 0.38f, Mathf.Max(0.25f, delta.magnitude));
            var center = (start + end) * 0.5f;
            context.AddBox(
                visual,
                label,
                center,
                scale,
                rotation,
                material);
            context.AddCollider(
                gameplay,
                label + " collision",
                center,
                new Vector3(width, 0.45f, scale.z),
                rotation,
                cityId,
                TetherfallCityFortificationColliderRole.Ascent,
                routeId);
        }

        private static void AddRouteMetadata(
            Transform gameplay,
            Transform cityRoot,
            string cityId,
            string routeId,
            string overlookId,
            Vector3[] localNodes)
        {
            var routeObject = new GameObject(routeId);
            routeObject.transform.SetParent(gameplay, false);
            var worldNodes = new Vector3[localNodes.Length];
            for (var index = 0; index < localNodes.Length; index++)
            {
                worldNodes[index] = cityRoot.TransformPoint(localNodes[index]);
            }

            routeObject.AddComponent<TetherfallCityFortificationTraversal>().Configure(
                cityId,
                routeId,
                overlookId,
                true,
                true,
                worldNodes);
        }

        private static TetherfallCityFortificationRoot FindDirectRoot(Transform parent)
        {
            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child == null || !string.Equals(child.name, RootName, StringComparison.Ordinal))
                {
                    continue;
                }

                var marker = child.GetComponent<TetherfallCityFortificationRoot>();
                if (marker == null)
                {
                    throw new InvalidOperationException(
                        $"A direct child named '{RootName}' exists without fortification metadata.");
                }

                return marker;
            }

            return null;
        }

        private static void RestoreMetrics(TetherfallCityFortificationRoot marker)
        {
            BuiltRendererCount = marker.RendererCount;
            BuiltColliderCount = marker.DeliberateColliderCount;
            BuiltPreferredResourceCount = marker.UsedPreferredResourceCount;
            BuiltFallbackCount = marker.ProceduralFallbackCount;
        }

        private static Material GetSharedMaterial(
            TetherfallGame game,
            FortificationMaterialKey key)
        {
            if (SharedMaterials.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var color = GetMaterialColor(key);
            var metallic = key == FortificationMaterialKey.Copper ||
                           key == FortificationMaterialKey.FloodIron
                ? 0.32f
                : 0.04f;
            var smoothness = key == FortificationMaterialKey.Copper
                ? 0.5f
                : key == FortificationMaterialKey.SafeTeal ||
                  key == FortificationMaterialKey.LanternAmber
                    ? 0.42f
                    : 0.24f;
            var material = game.CreateMaterial(color, metallic, smoothness);
            if (material == null)
            {
                throw new InvalidOperationException(
                    $"Could not create shared fortification material '{key}'.");
            }

            material.name = "Tetherfall Fortifications · " + key;
            if ((key == FortificationMaterialKey.SafeTeal ||
                 key == FortificationMaterialKey.LanternAmber) &&
                material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.7f);
            }

            SharedMaterials[key] = material;
            return material;
        }

        private static Color GetMaterialColor(FortificationMaterialKey key)
        {
            switch (key)
            {
                case FortificationMaterialKey.WarmStone:
                    return new Color(0.36f, 0.31f, 0.22f);
                case FortificationMaterialKey.WeatheredTimber:
                    return new Color(0.24f, 0.15f, 0.085f);
                case FortificationMaterialKey.Moss:
                    return new Color(0.27f, 0.38f, 0.2f);
                case FortificationMaterialKey.Copper:
                    return new Color(0.67f, 0.3f, 0.13f);
                case FortificationMaterialKey.SafeTeal:
                    return new Color(0.27f, 0.76f, 0.69f);
                case FortificationMaterialKey.DarkStone:
                    return new Color(0.15f, 0.2f, 0.21f);
                case FortificationMaterialKey.DarkTimber:
                    return new Color(0.105f, 0.075f, 0.055f);
                case FortificationMaterialKey.ReedRoof:
                    return new Color(0.31f, 0.35f, 0.2f);
                case FortificationMaterialKey.LanternAmber:
                    return new Color(0.82f, 0.53f, 0.21f);
                case FortificationMaterialKey.FloodIron:
                    return new Color(0.23f, 0.31f, 0.3f);
                default:
                    return Color.magenta;
            }
        }

        private static bool IsApprovedResource(string resourceName)
        {
            var definitions = TetherfallQuaterniusAssetCatalog.All;
            for (var index = 0; index < definitions.Count; index++)
            {
                if (string.Equals(
                        definitions[index].ResourceName,
                        resourceName,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSafePreferredResource(GameObject prefab)
        {
            if (prefab == null ||
                prefab.GetComponentsInChildren<Collider>(true).Length != 0 ||
                prefab.GetComponentsInChildren<Collider2D>(true).Length != 0 ||
                prefab.GetComponentsInChildren<Light>(true).Length != 0 ||
                prefab.GetComponentsInChildren<Camera>(true).Length != 0 ||
                prefab.GetComponentsInChildren<AudioListener>(true).Length != 0 ||
                prefab.GetComponentsInChildren<Animator>(true).Length != 0 ||
                prefab.GetComponentsInChildren<Animation>(true).Length != 0 ||
                prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0 ||
                prefab.GetComponentsInChildren<Rigidbody2D>(true).Length != 0 ||
                prefab.GetComponentsInChildren<ParticleSystem>(true).Length != 0 ||
                prefab.GetComponentsInChildren<LODGroup>(true).Length != 0 ||
                prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
            {
                return false;
            }

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 1)
            {
                return false;
            }

            var materials = renderers[0].sharedMaterials;
            if (materials.Length == 0)
            {
                return false;
            }

            for (var index = 0; index < materials.Length; index++)
            {
                if (materials[index] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static void ConfigureRenderer(Renderer visualRenderer)
        {
            visualRenderer.enabled = true;
            visualRenderer.shadowCastingMode = ShadowCastingMode.On;
            visualRenderer.receiveShadows = true;
            visualRenderer.lightProbeUsage = LightProbeUsage.Off;
            visualRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            visualRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            visualRenderer.allowOcclusionWhenDynamic = true;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);
                child.SetParent(null, true);
                DestroyObject(child.gameObject);
            }
        }

        private static void DestroyObject(Object value)
        {
            if (value == null)
            {
                return;
            }

            if (value is GameObject gameObject)
            {
                gameObject.SetActive(false);
            }
            else if (value is Behaviour behaviour)
            {
                behaviour.enabled = false;
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

        internal static bool IsFinite(Bounds value)
        {
            return IsFinite(value.center) && IsFinite(value.size);
        }

        internal static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        internal static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z) &&
                   IsFinite(value.w);
        }

        internal static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private enum FortificationMaterialKey
        {
            WarmStone,
            WeatheredTimber,
            Moss,
            Copper,
            SafeTeal,
            DarkStone,
            DarkTimber,
            ReedRoof,
            LanternAmber,
            FloodIron
        }

        private readonly struct WallSegmentSpec
        {
            public Vector3 Position { get; }
            public Vector3 Scale { get; }

            public WallSegmentSpec(Vector3 position, Vector3 scale)
            {
                Position = position;
                Scale = scale;
            }
        }

        private sealed class BuildContext
        {
            private readonly TetherfallGame game;
            private readonly bool forceMissingResources;

            public Transform Root { get; }
            public List<TetherfallCityFortificationCullingCluster> Clusters { get; } =
                new List<TetherfallCityFortificationCullingCluster>(ExpectedCityCount);
            public int RendererCount { get; private set; }
            public int ColliderCount { get; private set; }
            public int ResourcePlacementCount { get; private set; }
            public int UsedResourceCount { get; private set; }
            public int FallbackCount { get; private set; }

            public BuildContext(
                TetherfallGame game,
                Transform root,
                bool forceMissingResources)
            {
                this.game = game;
                Root = root;
                this.forceMissingResources = forceMissingResources;
            }

            public GameObject AddBox(
                Transform parent,
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                FortificationMaterialKey material)
            {
                return AddPrimitive(
                    parent,
                    PrimitiveType.Cube,
                    name,
                    localPosition,
                    localScale,
                    localRotation,
                    material);
            }

            public GameObject AddSphere(
                Transform parent,
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                FortificationMaterialKey material)
            {
                return AddPrimitive(
                    parent,
                    PrimitiveType.Sphere,
                    name,
                    localPosition,
                    localScale,
                    localRotation,
                    material);
            }

            public GameObject AddPrimitive(
                Transform parent,
                PrimitiveType primitiveType,
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                FortificationMaterialKey material)
            {
                if (RendererCount + 1 > MaxRendererCount)
                {
                    throw new InvalidOperationException(
                        $"Fortification renderer budget was exceeded while adding '{name}'.");
                }

                var visual = GameObject.CreatePrimitive(primitiveType);
                visual.name = name;
                visual.transform.SetParent(parent, false);
                visual.transform.localPosition = SanitizePosition(localPosition);
                visual.transform.localScale = SanitizeScale(localScale);
                visual.transform.localRotation = IsFinite(localRotation)
                    ? localRotation
                    : Quaternion.identity;

                var collider = visual.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroyObject(collider);
                }

                var visualRenderer = visual.GetComponent<Renderer>();
                if (visualRenderer == null)
                {
                    DestroyObject(visual);
                    throw new InvalidOperationException(
                        $"Primitive '{name}' did not create a Renderer.");
                }

                visualRenderer.sharedMaterial = GetSharedMaterial(game, material);
                ConfigureRenderer(visualRenderer);
                RendererCount++;
                return visual;
            }

            public void AddPreferredFacade(
                Transform parent,
                string resourceName,
                Vector3 sourceLocalPosition,
                Quaternion sourceLocalRotation,
                Vector3 sourceLocalScale,
                Vector3 fallbackLocalPosition,
                Vector3 fallbackLocalScale,
                FortificationMaterialKey fallbackMaterial)
            {
                ResourcePlacementCount++;
                if (!IsApprovedResource(resourceName))
                {
                    throw new InvalidOperationException(
                        $"Fortification composition references unapproved Resource '{resourceName}'.");
                }

                if (!forceMissingResources)
                {
                    var prefab = Resources.Load<GameObject>(
                        TetherfallQuaterniusAssetCatalog.ResourcePrefix + resourceName);
                    if (IsSafePreferredResource(prefab) && RendererCount + 1 <= MaxRendererCount)
                    {
                        var instance = Object.Instantiate(prefab, parent, false);
                        instance.name = "Quaternius source · " + resourceName;
                        instance.transform.localPosition = SanitizePosition(sourceLocalPosition);
                        instance.transform.localRotation = IsFinite(sourceLocalRotation)
                            ? sourceLocalRotation
                            : Quaternion.identity;
                        instance.transform.localScale = SanitizeScale(sourceLocalScale);
                        var renderer = instance.GetComponentInChildren<Renderer>(true);
                        ConfigureRenderer(renderer);
                        RendererCount++;
                        UsedResourceCount++;
                        return;
                    }
                }

                FallbackCount++;
                AddBox(
                    parent,
                    "Procedural fallback · " + resourceName,
                    fallbackLocalPosition,
                    fallbackLocalScale,
                    sourceLocalRotation,
                    fallbackMaterial);
            }

            public BoxCollider AddCollider(
                Transform parent,
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                string cityId,
                TetherfallCityFortificationColliderRole role,
                string routeId = null)
            {
                if (ColliderCount + 1 > MaxDeliberateColliderCount)
                {
                    throw new InvalidOperationException(
                        $"Fortification collider budget was exceeded while adding '{name}'.");
                }

                var colliderObject = new GameObject(name);
                colliderObject.transform.SetParent(parent, false);
                colliderObject.transform.localPosition = SanitizePosition(localPosition);
                colliderObject.transform.localScale = SanitizeScale(localScale);
                colliderObject.transform.localRotation = IsFinite(localRotation)
                    ? localRotation
                    : Quaternion.identity;
                var collider = colliderObject.AddComponent<BoxCollider>();
                collider.size = Vector3.one;
                collider.center = Vector3.zero;
                colliderObject.AddComponent<TetherfallCityFortificationCollider>().Configure(
                    cityId,
                    role,
                    routeId);
                ColliderCount++;
                return collider;
            }

            public BoxCollider AddClimbableFace(
                Transform parent,
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                string cityId,
                string routeId)
            {
                var collider = AddCollider(
                    parent,
                    name,
                    localPosition,
                    localScale,
                    localRotation,
                    cityId,
                    TetherfallCityFortificationColliderRole.ClimbableFace,
                    routeId);
                collider.gameObject.AddComponent<ClimbableSurface>();
                return collider;
            }

            public void ValidateFinalBudget()
            {
                if (Clusters.Count != ExpectedCityCount)
                {
                    throw new InvalidOperationException(
                        $"Fortification build produced {Clusters.Count} city clusters; " +
                        $"expected {ExpectedCityCount}.");
                }

                if (RendererCount <= 0 || RendererCount > MaxRendererCount)
                {
                    throw new InvalidOperationException(
                        $"Fortification build produced {RendererCount} renderers; " +
                        $"budget is 1..{MaxRendererCount}.");
                }

                if (ColliderCount <= 0 || ColliderCount > MaxDeliberateColliderCount)
                {
                    throw new InvalidOperationException(
                        $"Fortification build produced {ColliderCount} colliders; " +
                        $"budget is 1..{MaxDeliberateColliderCount}.");
                }

                if (SharedMaterials.Count <= 0 ||
                    SharedMaterials.Count > MaxProceduralMaterialCount)
                {
                    throw new InvalidOperationException(
                        $"Fortification build uses {SharedMaterials.Count} procedural materials; " +
                        $"budget is 1..{MaxProceduralMaterialCount}.");
                }

                if (ResourcePlacementCount != ExpectedPreferredResourcePlacementCount ||
                    UsedResourceCount + FallbackCount != ResourcePlacementCount)
                {
                    throw new InvalidOperationException(
                        $"Fortification source/fallback accounting is " +
                        $"{UsedResourceCount}+{FallbackCount}/{ResourcePlacementCount}; expected " +
                        $"{ExpectedPreferredResourcePlacementCount} complete placements.");
                }
            }

            private static Vector3 SanitizePosition(Vector3 value)
            {
                return IsFinite(value) ? value : Vector3.zero;
            }

            private static Vector3 SanitizeScale(Vector3 value)
            {
                if (!IsFinite(value))
                {
                    return Vector3.one;
                }

                return new Vector3(
                    Mathf.Clamp(Mathf.Abs(value.x), 0.01f, 200f),
                    Mathf.Clamp(Mathf.Abs(value.y), 0.01f, 200f),
                    Mathf.Clamp(Mathf.Abs(value.z), 0.01f, 200f));
            }
        }
    }
}
