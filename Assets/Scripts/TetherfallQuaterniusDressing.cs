using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Tetherfall
{
    public sealed class TetherfallQuaterniusAssetDefinition
    {
        public string ResourceName { get; }
        public string SourceAssetPath { get; }
        public string ResourcePath => TetherfallQuaterniusAssetCatalog.ResourcePrefix + ResourceName;
        public string OutputAssetPath =>
            TetherfallQuaterniusAssetCatalog.OutputFolder + "/" + ResourceName + ".prefab";

        public TetherfallQuaterniusAssetDefinition(string resourceName, string sourceFolder)
        {
            ResourceName = resourceName;
            SourceAssetPath = sourceFolder + resourceName + ".fbx";
        }
    }

    /// <summary>
    /// The deliberately small runtime-facing slice of the larger raw Quaternius import.
    /// Source paths are retained here so the Editor builder and runtime loader share one manifest.
    /// </summary>
    public static class TetherfallQuaterniusAssetCatalog
    {
        public const string OutputFolder = "Assets/Resources/Art/Quaternius";
        public const string ResourcePrefix = "Art/Quaternius/";

        private const string VillageFolder =
            "Assets/ThirdParty/Quaternius/MedievalVillageMegaKit_Standard/";
        private const string PropsFolder =
            "Assets/ThirdParty/Quaternius/FantasyPropsMegaKit_Standard/";
        private const string NatureFolder =
            "Assets/ThirdParty/Quaternius/StylizedNatureMegaKit_Standard/";

        private static readonly IReadOnlyList<TetherfallQuaterniusAssetDefinition> Definitions =
            new List<TetherfallQuaterniusAssetDefinition>
            {
                new TetherfallQuaterniusAssetDefinition("Wall_Arch", VillageFolder),
                new TetherfallQuaterniusAssetDefinition("Wall_Plaster_Door_Round", VillageFolder),
                new TetherfallQuaterniusAssetDefinition("Wall_Plaster_Straight", VillageFolder),
                new TetherfallQuaterniusAssetDefinition("Wall_Plaster_Window_Wide_Round", VillageFolder),
                new TetherfallQuaterniusAssetDefinition("Wall_Plaster_WoodGrid", VillageFolder),
                new TetherfallQuaterniusAssetDefinition("Wall_UnevenBrick_Door_Round", VillageFolder),
                new TetherfallQuaterniusAssetDefinition("Wall_UnevenBrick_Straight", VillageFolder),
                new TetherfallQuaterniusAssetDefinition("Wall_UnevenBrick_Window_Wide_Round", VillageFolder),
                new TetherfallQuaterniusAssetDefinition("Prop_Wagon", VillageFolder),

                new TetherfallQuaterniusAssetDefinition("Bag", PropsFolder),
                new TetherfallQuaterniusAssetDefinition("Barrel", PropsFolder),
                new TetherfallQuaterniusAssetDefinition("Barrel_Apples", PropsFolder),
                new TetherfallQuaterniusAssetDefinition("Barrel_Holder", PropsFolder),
                new TetherfallQuaterniusAssetDefinition("Bench", PropsFolder),
                new TetherfallQuaterniusAssetDefinition("Bucket_Wooden_1", PropsFolder),
                new TetherfallQuaterniusAssetDefinition("Cauldron", PropsFolder),
                new TetherfallQuaterniusAssetDefinition("Crate_Wooden", PropsFolder),
                new TetherfallQuaterniusAssetDefinition("FarmCrate_Empty", PropsFolder),
                new TetherfallQuaterniusAssetDefinition("Stall_Cart_Empty", PropsFolder),
                new TetherfallQuaterniusAssetDefinition("Stall_Empty", PropsFolder),
                new TetherfallQuaterniusAssetDefinition("Table_Large", PropsFolder),
                new TetherfallQuaterniusAssetDefinition("Workbench", PropsFolder),

                new TetherfallQuaterniusAssetDefinition("Bush_Common", NatureFolder),
                new TetherfallQuaterniusAssetDefinition("CommonTree_3", NatureFolder),
                new TetherfallQuaterniusAssetDefinition("DeadTree_1", NatureFolder),
                new TetherfallQuaterniusAssetDefinition("Grass_Common_Short", NatureFolder),
                new TetherfallQuaterniusAssetDefinition("Grass_Wispy_Tall", NatureFolder),
                new TetherfallQuaterniusAssetDefinition("Mushroom_Common", NatureFolder),
                new TetherfallQuaterniusAssetDefinition("Rock_Medium_1", NatureFolder),
                new TetherfallQuaterniusAssetDefinition("Rock_Medium_2", NatureFolder),
                new TetherfallQuaterniusAssetDefinition("Rock_Medium_3", NatureFolder)
            };

        public static IReadOnlyList<TetherfallQuaterniusAssetDefinition> All => Definitions;
    }

    public enum TetherfallQuaterniusDressingZone
    {
        CityFacade,
        CityService,
        EnemyCamp,
        OriginalRegion
    }

    public readonly struct TetherfallQuaterniusPlannedPlacement
    {
        public string ClusterName { get; }
        public string ResourceName { get; }
        public TetherfallQuaterniusDressingZone Zone { get; }
        public Vector3 WorldPosition { get; }

        public TetherfallQuaterniusPlannedPlacement(
            string clusterName,
            string resourceName,
            TetherfallQuaterniusDressingZone zone,
            Vector3 worldPosition)
        {
            ClusterName = clusterName;
            ResourceName = resourceName;
            Zone = zone;
            WorldPosition = worldPosition;
        }
    }

    /// <summary>
    /// Collider-free Quaternius set dressing. The module is additive, deterministic, and does
    /// not alter gameplay geometry or the core world builders.
    /// </summary>
    public static class TetherfallQuaterniusDressing
    {
        public const string RootName = "Tetherfall Quaternius Dressing";
        public const int ExpectedStaticPlacementCount = 48;
        public const int ExpectedCampPlacementCount = 18;
        public const int ExpectedPlacementCount =
            ExpectedStaticPlacementCount + ExpectedCampPlacementCount;
        public const int ExpectedClusterCount = 16;
        public const int MaxPlacementCount = 72;
        public const int MaxRendererCount = 80;
        public const int MaxSharedMaterialCount = 72;
        public const int MaxRenderersPerCluster = 12;
        public const float ClearCombatRadius = 10f;
        public const float NearDistance = 70f;
        public const float CullDistance = 145f;
        public const int AddedColliderCount = 0;
        public const int AddedLightCount = 0;
        public const int PerInstanceUpdateLoopCount = 0;

        private static readonly StaticClusterSpec[] StaticClusters =
        {
            new StaticClusterSpec(
                "City · Hearthmarket facade overlays",
                TetherfallQuaterniusDressingZone.CityFacade,
                new Vector3(0f, 0f, 72f),
                new PlacementSpec("Wall_Plaster_Door_Round", new Vector3(-21f, 0f, -10.08f), 0f),
                new PlacementSpec("Wall_Plaster_Window_Wide_Round", new Vector3(-21f, 0f, 9.92f), 0f),
                new PlacementSpec("Wall_UnevenBrick_Door_Round", new Vector3(17f, 0f, -11.08f), 0f),
                new PlacementSpec("Wall_UnevenBrick_Window_Wide_Round", new Vector3(18.5f, 0f, 12.92f), 0f)),
            new StaticClusterSpec(
                "City · Hearthmarket service dressing",
                TetherfallQuaterniusDressingZone.CityService,
                new Vector3(0f, 0f, 72f),
                new PlacementSpec("Stall_Empty", new Vector3(-25f, 0f, 21f), 90f),
                new PlacementSpec("Stall_Cart_Empty", new Vector3(29f, 0f, 19f), -90f),
                new PlacementSpec("Workbench", new Vector3(17.5f, 0f, 18.5f), 180f),
                new PlacementSpec("Crate_Wooden", new Vector3(21f, 0f, 20.5f), 13f),
                new PlacementSpec("Barrel_Apples", new Vector3(-14.5f, 0f, 14f), -18f),
                new PlacementSpec("Bench", new Vector3(-31.5f, 0f, -8f), 90f),
                new PlacementSpec("Prop_Wagon", new Vector3(-34f, 0f, 20f), 32f, 0.9f),
                new PlacementSpec("FarmCrate_Empty", new Vector3(-19f, 0f, 24f), -11f)),
            new StaticClusterSpec(
                "City · Veyrwater facade overlays",
                TetherfallQuaterniusDressingZone.CityFacade,
                new Vector3(203f, 0f, 72f),
                new PlacementSpec("Wall_Plaster_Door_Round", new Vector3(-16f, 0f, -10.08f), 0f),
                new PlacementSpec("Wall_Plaster_Window_Wide_Round", new Vector3(-16f, 0f, 9.92f), 0f),
                new PlacementSpec("Wall_UnevenBrick_Door_Round", new Vector3(17f, 0f, -10.08f), 0f),
                new PlacementSpec("Wall_UnevenBrick_Window_Wide_Round", new Vector3(17f, 0f, 13.92f), 0f)),
            new StaticClusterSpec(
                "City · Veyrwater service dressing",
                TetherfallQuaterniusDressingZone.CityService,
                new Vector3(203f, 0f, 72f),
                new PlacementSpec("Stall_Empty", new Vector3(10f, 0f, 19.5f), 180f),
                new PlacementSpec("Stall_Cart_Empty", new Vector3(23f, 0f, 25f), 165f),
                new PlacementSpec("Workbench", new Vector3(13f, 0f, 18f), 180f),
                new PlacementSpec("Crate_Wooden", new Vector3(17f, 0f, 22f), -14f),
                new PlacementSpec("Barrel", new Vector3(-13f, 0f, 21f), 8f),
                new PlacementSpec("Bench", new Vector3(-21f, 0f, 16f), 90f),
                new PlacementSpec("Bucket_Wooden_1", new Vector3(31f, 0f, 22f), -9f),
                new PlacementSpec("Cauldron", new Vector3(31f, 0f, -11f), 21f)),

            new StaticClusterSpec(
                "Region · Alderwake west contour",
                TetherfallQuaterniusDressingZone.OriginalRegion,
                new Vector3(-71f, 0f, -48f),
                new PlacementSpec("CommonTree_3", new Vector3(-4f, 0f, 3f), 20f, 0.85f),
                new PlacementSpec("Bush_Common", new Vector3(2.8f, 0f, -2f), 115f, 0.9f),
                new PlacementSpec("Rock_Medium_1", new Vector3(3.5f, 0f, 3.5f), 32f, 1.25f),
                new PlacementSpec("Grass_Common_Short", new Vector3(-1.5f, 0f, -3.2f), 73f, 1.1f)),
            new StaticClusterSpec(
                "Region · Alderwake east contour",
                TetherfallQuaterniusDressingZone.OriginalRegion,
                new Vector3(71f, 0f, -21f),
                new PlacementSpec("CommonTree_3", new Vector3(4f, 0f, -2f), -28f, 0.95f),
                new PlacementSpec("Bush_Common", new Vector3(-3f, 0f, 2.4f), 47f),
                new PlacementSpec("Rock_Medium_2", new Vector3(-3.8f, 0f, -3.5f), -19f, 1.15f),
                new PlacementSpec("Mushroom_Common", new Vector3(1.8f, 0f, 3.2f), 92f, 1.2f)),
            new StaticClusterSpec(
                "Region · Morrowfen west silt shelf",
                TetherfallQuaterniusDressingZone.OriginalRegion,
                new Vector3(91f, 0f, 62f),
                new PlacementSpec("DeadTree_1", new Vector3(-3.5f, 0f, 2.5f), 18f, 0.75f),
                new PlacementSpec("Bush_Common", new Vector3(3f, 0f, -2f), -35f, 0.85f),
                new PlacementSpec("Rock_Medium_3", new Vector3(2.8f, 0f, 3.8f), 61f),
                new PlacementSpec("Grass_Wispy_Tall", new Vector3(-1.4f, 0f, -3f), 122f, 0.9f)),
            new StaticClusterSpec(
                "Region · Morrowfen east silt shelf",
                TetherfallQuaterniusDressingZone.OriginalRegion,
                new Vector3(229f, 0f, 8f),
                new PlacementSpec("DeadTree_1", new Vector3(3.6f, 0f, -2f), -23f, 0.8f),
                new PlacementSpec("Rock_Medium_2", new Vector3(-3f, 0f, 2.5f), 11f, 1.2f),
                new PlacementSpec("Grass_Wispy_Tall", new Vector3(-2.5f, 0f, -2.7f), 84f),
                new PlacementSpec("Mushroom_Common", new Vector3(2f, 0f, 3f), -51f, 1.1f)),
            new StaticClusterSpec(
                "Region · Cinderwold west ash shelf",
                TetherfallQuaterniusDressingZone.OriginalRegion,
                new Vector3(-229f, 0f, 8f),
                new PlacementSpec("DeadTree_1", new Vector3(4f, 0f, 1f), 37f, 0.8f),
                new PlacementSpec("Rock_Medium_1", new Vector3(-3f, 0f, -3f), -17f, 1.35f),
                new PlacementSpec("Rock_Medium_3", new Vector3(-2f, 0f, 3f), 72f, 1.1f),
                new PlacementSpec("Grass_Common_Short", new Vector3(2.5f, 0f, -3f), 141f, 0.75f)),
            new StaticClusterSpec(
                "Region · Cinderwold east ash shelf",
                TetherfallQuaterniusDressingZone.OriginalRegion,
                new Vector3(-91f, 0f, -89f),
                new PlacementSpec("DeadTree_1", new Vector3(-4f, 0f, -1f), -31f, 0.85f),
                new PlacementSpec("Rock_Medium_2", new Vector3(3.2f, 0f, 3f), 29f, 1.2f),
                new PlacementSpec("Rock_Medium_3", new Vector3(3.8f, 0f, -2.5f), -76f),
                new PlacementSpec("Mushroom_Common", new Vector3(-1.8f, 0f, 3f), 108f, 0.85f))
        };

        private static readonly CampClusterSpec[] CampClusters =
        {
            new CampClusterSpec(
                "Camp · Hollow Reed periphery",
                "encounter.alderwake.hollow_reed",
                new Vector3(-38f, 0f, 33f),
                new PlacementSpec("Prop_Wagon", new Vector3(-11.5f, 0f, 5f), 70f, 0.9f),
                new PlacementSpec("Crate_Wooden", new Vector3(10.5f, 0f, -6.5f), 13f),
                new PlacementSpec("Barrel", new Vector3(8.5f, 0f, 9f), -18f)),
            new CampClusterSpec(
                "Camp · Mossglass salvage periphery",
                "encounter.alderwake.mossglass",
                new Vector3(48f, 0f, 36f),
                new PlacementSpec("Stall_Cart_Empty", new Vector3(-11f, 0f, -5.5f), 106f),
                new PlacementSpec("FarmCrate_Empty", new Vector3(10f, 0f, 7f), -22f),
                new PlacementSpec("Barrel_Holder", new Vector3(7.5f, 0f, -9f), 17f)),
            new CampClusterSpec(
                "Camp · Sunken Watch periphery",
                "encounter.morrowfen.sunken_watch",
                new Vector3(169f, 0f, -28f),
                new PlacementSpec("Prop_Wagon", new Vector3(-11f, 0f, 5f), 64f, 0.9f),
                new PlacementSpec("Crate_Wooden", new Vector3(10f, 0f, -7f), -14f),
                new PlacementSpec("Cauldron", new Vector3(8.5f, 0f, 9f), 33f)),
            new CampClusterSpec(
                "Camp · Fen Tithe periphery",
                "encounter.morrowfen.fen_tithe",
                new Vector3(118f, 0f, 43f),
                new PlacementSpec("Stall_Cart_Empty", new Vector3(-10.5f, 0f, -6f), 118f),
                new PlacementSpec("Barrel_Holder", new Vector3(10f, 0f, 6.5f), -31f),
                new PlacementSpec("Bucket_Wooden_1", new Vector3(7.5f, 0f, -9f), 9f)),
            new CampClusterSpec(
                "Camp · Ashjaw periphery",
                "encounter.cinderwold.ashjaw",
                new Vector3(-171f, 0f, -31f),
                new PlacementSpec("Prop_Wagon", new Vector3(-11.5f, 0f, 4f), 81f, 0.9f),
                new PlacementSpec("Crate_Wooden", new Vector3(10f, 0f, -7f), 16f),
                new PlacementSpec("Workbench", new Vector3(8f, 0f, 9f), -28f)),
            new CampClusterSpec(
                "Camp · Relay Breakers periphery",
                "encounter.cinderwold.relay_breakers",
                new Vector3(-126f, 0f, -41f),
                new PlacementSpec("Stall_Empty", new Vector3(-11f, 0f, -5f), 101f),
                new PlacementSpec("Barrel", new Vector3(10f, 0f, 7f), -12f),
                new PlacementSpec("Table_Large", new Vector3(8f, 0f, -9f), 26f))
        };

        private static GameObject lastRoot;

        public static bool IsBuilt => lastRoot != null;
        public static int BuiltPlacementCount { get; private set; }
        public static int BuiltClusterCount { get; private set; }
        public static int BuiltRendererCount { get; private set; }
        public static int BuiltSharedMaterialCount { get; private set; }
        public static int MissingPrefabCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            lastRoot = null;
            BuiltPlacementCount = 0;
            BuiltClusterCount = 0;
            BuiltRendererCount = 0;
            BuiltSharedMaterialCount = 0;
            MissingPrefabCount = 0;
        }

        public static GameObject Build(TetherfallGame game)
        {
            if (game == null)
            {
                Debug.LogWarning("Quaternius dressing deferred because TetherfallGame is not ready.");
                return null;
            }

            var existing = FindDirectChild(game.transform, RootName);
            if (existing != null)
            {
                lastRoot = existing.gameObject;
                RestoreMetrics(existing.gameObject);
                return existing.gameObject;
            }

            BuiltPlacementCount = 0;
            BuiltClusterCount = 0;
            BuiltRendererCount = 0;
            BuiltSharedMaterialCount = 0;
            MissingPrefabCount = 0;

            var root = new GameObject(RootName);
            root.transform.SetParent(game.transform, false);
            var loadedPrefabs = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            var materials = new HashSet<Material>();
            var runtimeClusters = new List<RuntimeCluster>(ExpectedClusterCount);

            foreach (var cluster in StaticClusters)
            {
                CreateCluster(
                    root.transform,
                    cluster.Name,
                    cluster.Center,
                    cluster.Placements,
                    loadedPrefabs,
                    materials,
                    runtimeClusters);
            }

            var camps = Object.FindObjectsByType<EnemyCamp>(FindObjectsInactive.Include);
            foreach (var cluster in CampClusters)
            {
                CreateCluster(
                    root.transform,
                    cluster.Name,
                    ResolveCampCenter(cluster, camps),
                    cluster.Placements,
                    loadedPrefabs,
                    materials,
                    runtimeClusters);
            }

            BuiltClusterCount = runtimeClusters.Count;
            BuiltSharedMaterialCount = materials.Count;
            var marker = root.AddComponent<TetherfallQuaterniusDressingRoot>();
            marker.SetMetrics(
                BuiltPlacementCount,
                BuiltClusterCount,
                BuiltRendererCount,
                BuiltSharedMaterialCount,
                MissingPrefabCount);

            var distanceBudget = root.AddComponent<TetherfallQuaterniusDistanceBudget>();
            distanceBudget.Initialize(Camera.main, runtimeClusters);
            lastRoot = root;

            if (BuiltPlacementCount != ExpectedPlacementCount ||
                BuiltClusterCount != ExpectedClusterCount ||
                BuiltRendererCount > MaxRendererCount ||
                BuiltSharedMaterialCount > MaxSharedMaterialCount)
            {
                Debug.LogWarning(
                    $"Quaternius dressing built with {BuiltPlacementCount}/{ExpectedPlacementCount} placements, " +
                    $"{BuiltClusterCount}/{ExpectedClusterCount} clusters, {BuiltRendererCount}/{MaxRendererCount} " +
                    $"renderers, {BuiltSharedMaterialCount}/{MaxSharedMaterialCount} shared materials, and " +
                    $"{MissingPrefabCount} missing prefabs.",
                    root);
            }
            else
            {
                Debug.Log(
                    $"Quaternius dressing ready: {BuiltPlacementCount} collider-free placements in " +
                    $"{BuiltClusterCount} distance-capped clusters, {BuiltRendererCount} renderers, " +
                    $"{BuiltSharedMaterialCount} shared source materials, zero lights.",
                    root);
            }

            return root;
        }

        public static IReadOnlyList<TetherfallQuaterniusPlannedPlacement> GetPlannedPlacements()
        {
            var placements = new List<TetherfallQuaterniusPlannedPlacement>(ExpectedPlacementCount);
            foreach (var cluster in StaticClusters)
            {
                AddPlannedPlacements(placements, cluster.Name, cluster.Zone, cluster.Center, cluster.Placements);
            }

            foreach (var cluster in CampClusters)
            {
                AddPlannedPlacements(
                    placements,
                    cluster.Name,
                    TetherfallQuaterniusDressingZone.EnemyCamp,
                    cluster.FallbackCenter,
                    cluster.Placements);
            }

            return placements;
        }

        public static IReadOnlyList<Vector3> GetPlannedCampCenters()
        {
            var centers = new List<Vector3>(CampClusters.Length);
            foreach (var cluster in CampClusters)
            {
                centers.Add(cluster.FallbackCenter);
            }

            return centers;
        }

        private static void AddPlannedPlacements(
            List<TetherfallQuaterniusPlannedPlacement> result,
            string clusterName,
            TetherfallQuaterniusDressingZone zone,
            Vector3 center,
            PlacementSpec[] placements)
        {
            foreach (var placement in placements)
            {
                result.Add(new TetherfallQuaterniusPlannedPlacement(
                    clusterName,
                    placement.ResourceName,
                    zone,
                    center + placement.LocalPosition));
            }
        }

        private static void CreateCluster(
            Transform parent,
            string clusterName,
            Vector3 center,
            PlacementSpec[] placements,
            Dictionary<string, GameObject> loadedPrefabs,
            HashSet<Material> materials,
            List<RuntimeCluster> runtimeClusters)
        {
            var clusterObject = new GameObject(clusterName);
            clusterObject.transform.SetParent(parent, false);
            clusterObject.transform.position = IsFinite(center) ? center : Vector3.zero;
            var clusterRenderers = new List<Renderer>(placements.Length);

            foreach (var placement in placements)
            {
                if (BuiltPlacementCount >= MaxPlacementCount)
                {
                    Debug.LogWarning("Quaternius dressing reached its hard placement budget.", parent);
                    break;
                }

                var prefab = LoadPrefab(placement.ResourceName, loadedPrefabs);
                if (prefab == null)
                {
                    MissingPrefabCount++;
                    continue;
                }

                var instance = Object.Instantiate(prefab, clusterObject.transform, false);
                instance.name = "Quaternius · " + placement.ResourceName;
                instance.transform.localPosition = IsFinite(placement.LocalPosition)
                    ? placement.LocalPosition
                    : Vector3.zero;
                instance.transform.localRotation = Quaternion.Euler(
                    0f,
                    IsFinite(placement.Yaw) ? placement.Yaw : 0f,
                    0f);
                instance.transform.localScale = SanitizeScale(placement.Scale);
                StripForbiddenComponents(instance);

                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0 ||
                    renderers.Length > MaxRenderersPerCluster ||
                    BuiltRendererCount + renderers.Length > MaxRendererCount ||
                    clusterRenderers.Count + renderers.Length > MaxRenderersPerCluster)
                {
                    DestroyObject(instance);
                    Debug.LogWarning(
                        $"Quaternius placement '{placement.ResourceName}' exceeded a renderer budget.",
                        clusterObject);
                    continue;
                }

                foreach (var renderer in renderers)
                {
                    ConfigureRenderer(renderer);
                    clusterRenderers.Add(renderer);
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            materials.Add(material);
                        }
                    }
                }

                BuiltRendererCount += renderers.Length;
                BuiltPlacementCount++;
            }

            if (clusterRenderers.Count == 0)
            {
                DestroyObject(clusterObject);
                return;
            }

            runtimeClusters.Add(new RuntimeCluster(
                clusterRenderers.ToArray(),
                CalculateBoundingSphere(clusterRenderers, clusterObject.transform.position)));
        }

        private static GameObject LoadPrefab(
            string resourceName,
            Dictionary<string, GameObject> loadedPrefabs)
        {
            if (loadedPrefabs.TryGetValue(resourceName, out var cached))
            {
                return cached;
            }

            var prefab = Resources.Load<GameObject>(
                TetherfallQuaterniusAssetCatalog.ResourcePrefix + resourceName);
            loadedPrefabs[resourceName] = prefab;
            return prefab;
        }

        private static Vector3 ResolveCampCenter(CampClusterSpec definition, EnemyCamp[] camps)
        {
            EnemyCamp nearest = null;
            var nearestDistanceSquared = float.PositiveInfinity;
            foreach (var camp in camps)
            {
                if (camp == null ||
                    !string.Equals(camp.EncounterId, definition.EncounterId, StringComparison.Ordinal))
                {
                    continue;
                }

                var distanceSquared = (camp.AnchorPosition - definition.FallbackCenter).sqrMagnitude;
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearest = camp;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            return nearest != null && IsFinite(nearest.AnchorPosition)
                ? nearest.AnchorPosition
                : definition.FallbackCenter;
        }

        private static void ConfigureRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.enabled = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = true;
        }

        private static BoundingSphere CalculateBoundingSphere(
            List<Renderer> renderers,
            Vector3 fallbackCenter)
        {
            var hasBounds = false;
            var combined = new Bounds(fallbackCenter, Vector3.one);
            foreach (var renderer in renderers)
            {
                if (renderer == null || !IsFinite(renderer.bounds))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combined = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds
                ? new BoundingSphere(combined.center, Mathf.Max(1f, combined.extents.magnitude))
                : new BoundingSphere(fallbackCenter, 1f);
        }

        private static void StripForbiddenComponents(GameObject root)
        {
            DestroyComponents<Collider>(root);
            DestroyComponents<Collider2D>(root);
            DestroyComponents<Light>(root);
            DestroyComponents<Camera>(root);
            DestroyComponents<Animator>(root);
            DestroyComponents<Animation>(root);
        }

        private static void DestroyComponents<T>(GameObject root) where T : Component
        {
            foreach (var component in root.GetComponentsInChildren<T>(true))
            {
                if (component != null)
                {
                    DestroyObject(component);
                }
            }
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

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child != null && string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static void RestoreMetrics(GameObject root)
        {
            var marker = root.GetComponent<TetherfallQuaterniusDressingRoot>();
            if (marker == null)
            {
                BuiltRendererCount = root.GetComponentsInChildren<Renderer>(true).Length;
                return;
            }

            BuiltPlacementCount = marker.PlacementCount;
            BuiltClusterCount = marker.ClusterCount;
            BuiltRendererCount = marker.RendererCount;
            BuiltSharedMaterialCount = marker.SharedMaterialCount;
            MissingPrefabCount = marker.MissingPrefabCount;
        }

        private static Vector3 SanitizeScale(Vector3 value)
        {
            if (!IsFinite(value))
            {
                return Vector3.one;
            }

            return new Vector3(
                Mathf.Clamp(Mathf.Abs(value.x), 0.05f, 8f),
                Mathf.Clamp(Mathf.Abs(value.y), 0.05f, 8f),
                Mathf.Clamp(Mathf.Abs(value.z), 0.05f, 8f));
        }

        private static bool IsFinite(Bounds value)
        {
            return IsFinite(value.center) && IsFinite(value.size);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal sealed class RuntimeCluster
        {
            public Renderer[] Renderers { get; }
            public BoundingSphere Bounds { get; }

            public RuntimeCluster(Renderer[] renderers, BoundingSphere bounds)
            {
                Renderers = renderers;
                Bounds = bounds;
            }
        }

        private sealed class StaticClusterSpec
        {
            public string Name { get; }
            public TetherfallQuaterniusDressingZone Zone { get; }
            public Vector3 Center { get; }
            public PlacementSpec[] Placements { get; }

            public StaticClusterSpec(
                string name,
                TetherfallQuaterniusDressingZone zone,
                Vector3 center,
                params PlacementSpec[] placements)
            {
                Name = name;
                Zone = zone;
                Center = center;
                Placements = placements;
            }
        }

        private sealed class CampClusterSpec
        {
            public string Name { get; }
            public string EncounterId { get; }
            public Vector3 FallbackCenter { get; }
            public PlacementSpec[] Placements { get; }

            public CampClusterSpec(
                string name,
                string encounterId,
                Vector3 fallbackCenter,
                params PlacementSpec[] placements)
            {
                Name = name;
                EncounterId = encounterId;
                FallbackCenter = fallbackCenter;
                Placements = placements;
            }
        }

        private readonly struct PlacementSpec
        {
            public string ResourceName { get; }
            public Vector3 LocalPosition { get; }
            public float Yaw { get; }
            public Vector3 Scale { get; }

            public PlacementSpec(
                string resourceName,
                Vector3 localPosition,
                float yaw,
                float uniformScale = 1f)
            {
                ResourceName = resourceName;
                LocalPosition = localPosition;
                Yaw = yaw;
                Scale = Vector3.one * uniformScale;
            }
        }
    }

    [DisallowMultipleComponent]
    internal sealed class TetherfallQuaterniusDressingRoot : MonoBehaviour
    {
        public int PlacementCount { get; private set; }
        public int ClusterCount { get; private set; }
        public int RendererCount { get; private set; }
        public int SharedMaterialCount { get; private set; }
        public int MissingPrefabCount { get; private set; }

        public void SetMetrics(
            int placements,
            int clusters,
            int renderers,
            int sharedMaterials,
            int missingPrefabs)
        {
            PlacementCount = placements;
            ClusterCount = clusters;
            RendererCount = renderers;
            SharedMaterialCount = sharedMaterials;
            MissingPrefabCount = missingPrefabs;
        }
    }

    /// <summary>
    /// One CullingGroup governs every dressing cluster. There are no per-prop behaviours or
    /// Update loops.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class TetherfallQuaterniusDistanceBudget : MonoBehaviour
    {
        private CullingGroup cullingGroup;
        private Renderer[][] rendererGroups = Array.Empty<Renderer[]>();

        public void Initialize(
            Camera targetCamera,
            IReadOnlyList<TetherfallQuaterniusDressing.RuntimeCluster> clusters)
        {
            rendererGroups = new Renderer[clusters.Count][];
            var spheres = new BoundingSphere[clusters.Count];
            for (var index = 0; index < clusters.Count; index++)
            {
                rendererGroups[index] = clusters[index].Renderers;
                spheres[index] = clusters[index].Bounds;
            }

            if (targetCamera == null || clusters.Count == 0)
            {
                return;
            }

            cullingGroup = new CullingGroup
            {
                targetCamera = targetCamera,
                onStateChanged = OnStateChanged
            };
            cullingGroup.SetDistanceReferencePoint(targetCamera.transform);
            cullingGroup.SetBoundingDistances(new[]
            {
                TetherfallQuaterniusDressing.NearDistance,
                TetherfallQuaterniusDressing.CullDistance
            });
            cullingGroup.SetBoundingSpheres(spheres);
            cullingGroup.SetBoundingSphereCount(spheres.Length);

            for (var index = 0; index < spheres.Length; index++)
            {
                var distance = Vector3.Distance(targetCamera.transform.position, spheres[index].position) -
                               spheres[index].radius;
                SetGroupVisible(index, distance <= TetherfallQuaterniusDressing.CullDistance);
            }
        }

        private void OnStateChanged(CullingGroupEvent state)
        {
            SetGroupVisible(state.index, state.currentDistance < 2);
        }

        private void SetGroupVisible(int index, bool visible)
        {
            if (index < 0 || index >= rendererGroups.Length)
            {
                return;
            }

            foreach (var renderer in rendererGroups[index])
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        private void OnDestroy()
        {
            if (cullingGroup == null)
            {
                return;
            }

            cullingGroup.onStateChanged = null;
            cullingGroup.Dispose();
            cullingGroup = null;
        }
    }

    internal sealed class TetherfallQuaterniusDressingBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Object.FindAnyObjectByType<TetherfallQuaterniusDressingBootstrap>() != null)
            {
                return;
            }

            new GameObject("Tetherfall Quaternius Dressing Bootstrap")
                .AddComponent<TetherfallQuaterniusDressingBootstrap>();
        }

        private IEnumerator Start()
        {
            while (TetherfallGame.Instance == null)
            {
                yield return null;
            }

            while (TetherfallGame.Instance.Player == null)
            {
                yield return null;
            }

            // The game's Start method constructs the world synchronously. One additional frame
            // keeps this additive pass behind all world, city, camp, and service builders.
            yield return null;
            TetherfallQuaterniusDressing.Build(TetherfallGame.Instance);
            Destroy(gameObject);
        }
    }
}
