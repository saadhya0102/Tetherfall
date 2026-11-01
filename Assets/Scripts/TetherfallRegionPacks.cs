using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public enum RegionPackStatus
    {
        Playable,
        AuthoringSlot
    }

    public enum RegionPackMaterialSlot
    {
        Ground,
        Terrain,
        Accent,
        Dark
    }

    public enum RegionPackLandmarkStyle
    {
        ChoirglassArch,
        RimecoilNeedle,
        HollowCrown,
        SplitSunArray,
        KiteboneCrown,
        LumenBeacon,
        BrassrootCrucible,
        CloudbarrowCairn,
        TideglassNeedles,
        NightloomLattice
    }

    public enum RegionPackEncounterFootprint
    {
        MirrorPalisade,
        CoilRedoubt,
        RootSnare,
        PrismTrench,
        KiteRing,
        LensWalk,
        FoundryTerrace,
        BarrowGate,
        TidebreakSpines,
        LoomAmbush
    }

    [Serializable]
    public sealed class RegionPackTerrainPiece
    {
        public string id;
        public PrimitiveType primitive = PrimitiveType.Cube;
        public Vector3 localPosition;
        public Vector3 scale = Vector3.one;
        public Vector3 eulerAngles;
        public RegionPackMaterialSlot materialSlot;
        public bool solid = true;
        public bool climbable;
    }

    [Serializable]
    public sealed class RegionPackResourcePlacement
    {
        public ResourceKind kind;
        public Vector3 localPosition;
        public Color color;
    }

    [Serializable]
    public sealed class RegionPackDefinition
    {
        public string id;
        public string displayName;
        public RegionPackStatus status;
        public string biome;
        public string silhouette;
        public string traversalNote;
        public string discoveryText;
        public string authoringBrief;
        public string mapSummary;
        public Vector3 center;
        public Vector2 size;
        public float activationRadius;
        public Color mapColor;
        public Color groundColor;
        public Color terrainColor;
        public Color accentColor;
        public Color darkColor;
        public Color ambientColor;
        public Color fogColor;
        public Color sunColor;
        public Color skyColor;
        public float fogDensity;
        public Vector3 connectorStart;
        public Vector3 connectorEnd;
        public float connectorWidth;
        public RegionPackTerrainPiece[] terrainPieces = Array.Empty<RegionPackTerrainPiece>();
        public bool hasWater;
        public Vector3 waterLocalPosition;
        public Vector3 waterSize;
        public Color waterColor;
        public bool hasWindLift;
        public Vector3 windLiftLocalPosition;
        public Vector3 windLiftSize;
        public float windLiftVelocity;
        public RegionPackLandmarkStyle landmarkStyle;
        public string landmarkName;
        public string landmarkSubtitle;
        public Vector3 landmarkLocalPosition;
        public string secretId;
        public string secretName;
        public string secretInscription;
        public Vector3 secretLocalPosition;
        public ResourceKind secretResource;
        public int secretResourceAmount;
        public string encounterId;
        public string encounterPersistenceId;
        public Vector3 encounterLocalPosition;
        public RegionPackEncounterFootprint encounterFootprint;
        public RegionPackResourcePlacement[] resources = Array.Empty<RegionPackResourcePlacement>();
        public ThreadwayDefinition threadway;

        public Rect WorldBounds => new Rect(
            center.x - size.x * 0.5f,
            center.z - size.y * 0.5f,
            size.x,
            size.y);

        public bool Contains(Vector3 position, float margin = 0f)
        {
            var halfWidth = size.x * 0.5f + Mathf.Max(0f, margin);
            var halfDepth = size.y * 0.5f + Mathf.Max(0f, margin);
            return Mathf.Abs(position.x - center.x) <= halfWidth &&
                   Mathf.Abs(position.z - center.z) <= halfDepth;
        }
    }

    public readonly struct RegionPackBudgetEstimate
    {
        public readonly int renderers;
        public readonly int colliders;
        public readonly int lights;
        public readonly int activeUpdateDrivers;

        public RegionPackBudgetEstimate(
            int rendererCount,
            int colliderCount,
            int lightCount,
            int updateDriverCount)
        {
            renderers = rendererCount;
            colliders = colliderCount;
            lights = lightCount;
            activeUpdateDrivers = updateDriverCount;
        }
    }

    public static class TetherfallRegionPacks
    {
        public const int AdditionalRegionTarget = 10;
        public const int PlayableRegionPackCount = 10;
        public const int AuthoringSlotCount = 0;
        public const int MaxRenderersPerActivePack = 160;
        public const int MaxCollidersPerActivePack = 64;
        public const int MaxLightsPerActivePack = 2;
        public const int MaxUpdateDriversPerActivePack = 20;
        public const int MaxSimultaneouslyActivePacks = 2;
        public const float ActivationRefreshSeconds = 0.25f;
        public const float ActivationMargin = 24f;
        public const float WorldMinimumX = -601f;
        public const float WorldMaximumX = 581f;
        public const float WorldMinimumZ = -631f;
        public const float WorldMaximumZ = 731f;

        private const string WorldRootName = "Regional Expansion Packs";
        private static readonly List<RegionPackDefinition> Definitions = CreateDefinitions();
        private static readonly List<RegionPackDefinition> PlayableDefinitions = CreatePlayableDefinitions();
        private static readonly Dictionary<string, Texture2D> OverviewTextures =
            new Dictionary<string, Texture2D>(StringComparer.Ordinal);

        public static IReadOnlyList<RegionPackDefinition> All => Definitions;
        public static IReadOnlyList<RegionPackDefinition> Playable => PlayableDefinitions;

        public static void Build(TetherfallGame game)
        {
            if (game == null)
            {
                return;
            }

            InstallThreadways();
            TetherfallRegionalAnnexes.Build(game);
            if (game.transform.Find(WorldRootName) != null)
            {
                return;
            }

            var worldRoot = new GameObject(WorldRootName);
            worldRoot.transform.SetParent(game.transform, false);
            var runtimes = new List<RegionPackRuntime>(PlayableDefinitions.Count);
            foreach (var definition in PlayableDefinitions)
            {
                runtimes.Add(BuildPlayableRegion(game, worldRoot.transform, definition));
            }

            var director = worldRoot.AddComponent<RegionPackDirector>();
            director.Initialize(game, runtimes);
        }

        public static RegionPackDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (var definition in Definitions)
            {
                if (string.Equals(definition.id, id, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public static RegionPackDefinition GetByDisplayName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                return null;
            }

            foreach (var definition in Definitions)
            {
                if (string.Equals(definition.displayName, displayName, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public static RegionPackDefinition FindPlayable(Vector3 position)
        {
            foreach (var definition in PlayableDefinitions)
            {
                if (definition.Contains(position))
                {
                    return definition;
                }
            }

            return null;
        }

        public static string ResolveRegionName(Vector3 position)
        {
            var annexName = TetherfallRegionalAnnexes.ResolveParentRegionName(position);
            if (!string.IsNullOrEmpty(annexName))
            {
                return annexName;
            }

            var definition = FindPlayable(position);
            return definition != null ? definition.displayName : ResolveLegacyRegionName(position);
        }

        public static bool TryGetPresentation(string region, out RegionPresentation.Definition presentation)
        {
            var definition = GetByDisplayName(region);
            if (definition == null || definition.status != RegionPackStatus.Playable)
            {
                presentation = default;
                return false;
            }

            presentation = new RegionPresentation.Definition(
                definition.ambientColor,
                definition.fogColor,
                definition.sunColor,
                definition.skyColor,
                definition.groundColor,
                definition.discoveryText);
            return true;
        }

        public static float GetFogDensity(string region, float fallback)
        {
            var definition = GetByDisplayName(region);
            return definition != null && definition.status == RegionPackStatus.Playable
                ? definition.fogDensity
                : fallback;
        }

        public static Texture2D GetOverviewTexture(string region)
        {
            var definition = GetByDisplayName(region);
            if (definition == null || definition.status != RegionPackStatus.Playable)
            {
                return null;
            }

            if (OverviewTextures.TryGetValue(definition.id, out var cached) && cached != null)
            {
                return cached;
            }

            var texture = CreateOverviewTexture(definition);
            OverviewTextures[definition.id] = texture;
            return texture;
        }

        public static RegionPackBudgetEstimate EstimateBudget(RegionPackDefinition definition)
        {
            if (definition == null || definition.status != RegionPackStatus.Playable)
            {
                return new RegionPackBudgetEstimate(0, 0, 0, 0);
            }

            var terrainCount = definition.terrainPieces != null ? definition.terrainPieces.Length : 0;
            var solidTerrainCount = 0;
            if (definition.terrainPieces != null)
            {
                foreach (var piece in definition.terrainPieces)
                {
                    if (piece != null && piece.solid)
                    {
                        solidTerrainCount++;
                    }
                }
            }

            var resourceCount = definition.resources != null ? definition.resources.Length : 0;
            var waterRenderers = definition.hasWater ? 1 : 0;
            var waterColliders = definition.hasWater ? 1 : 0;
            var windColliders = definition.hasWindLift ? 1 : 0;
            var landmarkRenderers = GetLandmarkRendererCount(definition.landmarkStyle);
            var landmarkColliders = GetLandmarkColliderCount(definition.landmarkStyle);
            const int footprintRenderers = 5;
            const int footprintColliders = 4;
            const int baseCampRenderers = 7;
            const int looseCampColliders = 2;
            const int threadwayRenderers = 7;
            const int threadwayColliders = 1;
            const int secretRenderers = 2;
            const int secretColliders = 1;

            return new RegionPackBudgetEstimate(
                2 +
                terrainCount +
                waterRenderers +
                landmarkRenderers +
                footprintRenderers +
                resourceCount +
                secretRenderers +
                TetherfallCampPresentation.MaxRenderersPerCamp +
                baseCampRenderers +
                threadwayRenderers,
                2 +
                solidTerrainCount +
                waterColliders +
                windColliders +
                landmarkColliders +
                footprintColliders +
                resourceCount +
                secretColliders +
                TetherfallCampPresentation.MaxCollidersPerCamp +
                looseCampColliders +
                threadwayColliders,
                2,
                resourceCount + 10);
        }

        internal static string ResolveLegacyRegionName(Vector3 position)
        {
            return position.x > 80f
                ? "Morrowfen"
                : position.x < -80f
                    ? "Cinderwold Verge"
                    : "Alderwake Basin";
        }

        private static void InstallThreadways()
        {
            if (!(ThreadwayCatalog.All is List<ThreadwayDefinition> mutableCatalog))
            {
                Debug.LogError(
                    "Tetherfall region packs could not install Threadway metadata because " +
                    "ThreadwayCatalog.All is not backed by a mutable list.");
                return;
            }

            foreach (var definition in PlayableDefinitions)
            {
                var threadway = definition.threadway;
                if (threadway != null && ThreadwayCatalog.Get(threadway.id) == null)
                {
                    mutableCatalog.Add(threadway);
                }
            }
        }

        private static RegionPackRuntime BuildPlayableRegion(
            TetherfallGame game,
            Transform worldRoot,
            RegionPackDefinition definition)
        {
            var regionRoot = new GameObject($"{definition.displayName} Region Pack");
            regionRoot.transform.SetParent(worldRoot, false);
            regionRoot.transform.position = definition.center;

            var staticRoot = new GameObject("Always-on traversal shell");
            staticRoot.transform.SetParent(regionRoot.transform, false);
            var detailRoot = new GameObject("Proximity-activated ecology");
            detailRoot.transform.SetParent(regionRoot.transform, false);

            CreatePiece(
                game,
                definition,
                staticRoot.transform,
                $"{definition.displayName} ground",
                PrimitiveType.Cube,
                new Vector3(0f, -1f, 0f),
                new Vector3(definition.size.x, 2f, definition.size.y),
                Vector3.zero,
                RegionPackMaterialSlot.Ground,
                true,
                false);
            CreateConnector(game, definition, staticRoot.transform);

            foreach (var piece in definition.terrainPieces)
            {
                if (piece == null)
                {
                    continue;
                }

                CreatePiece(
                    game,
                    definition,
                    staticRoot.transform,
                    piece.id,
                    piece.primitive,
                    piece.localPosition,
                    piece.scale,
                    piece.eulerAngles,
                    piece.materialSlot,
                    piece.solid,
                    piece.climbable);
            }

            if (definition.hasWater)
            {
                BuildWater(game, definition, staticRoot.transform);
            }

            if (definition.hasWindLift)
            {
                BuildWindLift(definition, staticRoot.transform);
            }

            BuildLandmark(game, definition, detailRoot.transform);
            BuildEncounterFootprint(game, definition, detailRoot.transform);
            var campPosition = definition.center + definition.encounterLocalPosition;
            var camp = EnemyCamp.Create(
                game,
                definition.encounterId,
                campPosition,
                definition.encounterPersistenceId);
            camp.transform.SetParent(detailRoot.transform, true);
            AdoptLooseCampProps(detailRoot.transform, campPosition);

            foreach (var placement in definition.resources)
            {
                if (placement == null)
                {
                    continue;
                }

                var node = ResourceNode.Create(
                    game,
                    placement.kind,
                    definition.center + placement.localPosition,
                    placement.color);
                var renderer = node.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var instanceMaterial = renderer.sharedMaterial;
                    var sharedMaterial = RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        placement.color,
                        placement.kind == ResourceKind.Chimesalt ? 0.35f : 0.04f,
                        0.5f);
                    renderer.sharedMaterial = sharedMaterial;
                    if (instanceMaterial != null && instanceMaterial != sharedMaterial)
                    {
                        UnityEngine.Object.Destroy(instanceMaterial);
                    }
                }

                node.transform.SetParent(detailRoot.transform, true);
            }

            var secret = RegionPackSecret.Create(game, definition);
            secret.transform.SetParent(detailRoot.transform, true);
            detailRoot.SetActive(false);
            return new RegionPackRuntime(definition, staticRoot, detailRoot);
        }

        private static void CreateConnector(
            TetherfallGame game,
            RegionPackDefinition definition,
            Transform staticRoot)
        {
            var delta = definition.connectorEnd - definition.connectorStart;
            var length = new Vector2(delta.x, delta.z).magnitude;
            var midpoint = (definition.connectorStart + definition.connectorEnd) * 0.5f - definition.center;
            midpoint.y = -0.35f;
            var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            CreatePiece(
                game,
                definition,
                staticRoot,
                $"{definition.displayName} route connector",
                PrimitiveType.Cube,
                midpoint,
                new Vector3(definition.connectorWidth, 0.7f, length + 2f),
                new Vector3(0f, yaw, 0f),
                RegionPackMaterialSlot.Dark,
                true,
                false);
        }

        private static void BuildWater(
            TetherfallGame game,
            RegionPackDefinition definition,
            Transform staticRoot)
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = $"{definition.displayName} stillwater";
            water.transform.SetParent(staticRoot, false);
            water.transform.localPosition = definition.waterLocalPosition;
            water.transform.localScale = new Vector3(
                definition.waterSize.x,
                Mathf.Max(0.08f, definition.waterSize.y * 0.08f),
                definition.waterSize.z);
            water.GetComponent<Renderer>().sharedMaterial = RuntimeEffectMaterialCache.GetTransparent(
                game,
                definition.waterColor,
                0.72f);
            UnityEngine.Object.Destroy(water.GetComponent<Collider>());

            var volume = WaterVolume.Create(
                definition.center + definition.waterLocalPosition,
                definition.waterSize);
            volume.transform.SetParent(staticRoot, true);
        }

        private static void BuildWindLift(RegionPackDefinition definition, Transform staticRoot)
        {
            var liftRoot = new GameObject($"{definition.displayName} bounded updraft");
            liftRoot.transform.SetParent(staticRoot, false);
            liftRoot.transform.localPosition = definition.windLiftLocalPosition;
            var trigger = liftRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = definition.windLiftSize;
            var lift = liftRoot.AddComponent<WindLift>();
            lift.Initialize(definition.windLiftVelocity);
        }

        private static void BuildLandmark(
            TetherfallGame game,
            RegionPackDefinition definition,
            Transform detailRoot)
        {
            var root = new GameObject(definition.landmarkName);
            root.transform.SetParent(detailRoot, false);
            root.transform.localPosition = definition.landmarkLocalPosition;

            switch (definition.landmarkStyle)
            {
                case RegionPackLandmarkStyle.ChoirglassArch:
                    CreateLandmarkPiece(game, definition, root.transform, "West glass pier",
                        PrimitiveType.Cube, new Vector3(-2.5f, 2.4f, 0f), new Vector3(0.8f, 4.8f, 1.2f),
                        new Vector3(0f, 0f, -8f), RegionPackMaterialSlot.Terrain, true);
                    CreateLandmarkPiece(game, definition, root.transform, "East glass pier",
                        PrimitiveType.Cube, new Vector3(2.5f, 2.4f, 0f), new Vector3(0.8f, 4.8f, 1.2f),
                        new Vector3(0f, 0f, 8f), RegionPackMaterialSlot.Terrain, true);
                    CreateLandmarkPiece(game, definition, root.transform, "Choirglass lintel",
                        PrimitiveType.Cube, new Vector3(0f, 4.75f, 0f), new Vector3(5.8f, 0.6f, 1f),
                        Vector3.zero, RegionPackMaterialSlot.Accent, false);
                    for (var index = 0; index < 5; index++)
                    {
                        CreateLandmarkPiece(game, definition, root.transform, $"Choirglass overtone {index + 1}",
                            PrimitiveType.Cube,
                            new Vector3(-1.8f + index * 0.9f, 5.45f + (index % 2) * 0.45f, 0f),
                            new Vector3(0.22f, 1.2f + index % 3 * 0.24f, 0.22f),
                            new Vector3(index * 7f, index * 29f, index % 2 == 0 ? -18f : 18f),
                            RegionPackMaterialSlot.Accent,
                            false);
                    }

                    break;

                case RegionPackLandmarkStyle.RimecoilNeedle:
                    CreateLandmarkPiece(game, definition, root.transform, "Rimecoil plinth",
                        PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(3.8f, 0.45f, 3.8f),
                        Vector3.zero, RegionPackMaterialSlot.Dark, true);
                    for (var index = 0; index < 6; index++)
                    {
                        var angle = index * 48f;
                        var radians = angle * Mathf.Deg2Rad;
                        CreateLandmarkPiece(game, definition, root.transform, $"Rimecoil blade {index + 1}",
                            PrimitiveType.Cube,
                            new Vector3(
                                Mathf.Sin(radians) * (1.35f - index * 0.11f),
                                1.35f + index * 1.1f,
                                Mathf.Cos(radians) * (1.35f - index * 0.11f)),
                            new Vector3(0.42f, 2.5f, 0.6f),
                            new Vector3(12f, angle, 22f),
                            index == 5 ? RegionPackMaterialSlot.Accent : RegionPackMaterialSlot.Terrain,
                            index < 2);
                    }

                    break;

                case RegionPackLandmarkStyle.HollowCrown:
                    CreateLandmarkPiece(game, definition, root.transform, "Hollow crown heart",
                        PrimitiveType.Cylinder, new Vector3(0f, 2.5f, 0f), new Vector3(2.3f, 2.5f, 2.3f),
                        Vector3.zero, RegionPackMaterialSlot.Dark, true);
                    for (var index = 0; index < 5; index++)
                    {
                        var angle = index * 72f;
                        var radians = angle * Mathf.Deg2Rad;
                        CreateLandmarkPiece(game, definition, root.transform, $"Crown root {index + 1}",
                            PrimitiveType.Cylinder,
                            new Vector3(Mathf.Sin(radians) * 2.8f, 3.8f, Mathf.Cos(radians) * 2.8f),
                            new Vector3(0.5f, 3.3f, 0.5f),
                            new Vector3(Mathf.Cos(radians) * 28f, angle, -Mathf.Sin(radians) * 28f),
                            RegionPackMaterialSlot.Terrain,
                            index < 3);
                    }

                    CreateLandmarkPiece(game, definition, root.transform, "Amber canopy",
                        PrimitiveType.Sphere, new Vector3(0f, 6.2f, 0f), new Vector3(5.6f, 1.1f, 5.6f),
                        Vector3.zero, RegionPackMaterialSlot.Accent, false);
                    break;

                case RegionPackLandmarkStyle.SplitSunArray:
                    for (var index = 0; index < 7; index++)
                    {
                        var offset = index - 3;
                        CreateLandmarkPiece(game, definition, root.transform, $"Split-sun vane {index + 1}",
                            PrimitiveType.Cube,
                            new Vector3(offset * 0.92f, 2.1f + Mathf.Abs(offset) * 0.42f, offset % 2 * 0.45f),
                            new Vector3(0.48f, 4.2f + Mathf.Abs(offset) * 0.65f, 0.65f),
                            new Vector3(offset * 4f, offset * 13f, offset * 9f),
                            index == 3 ? RegionPackMaterialSlot.Accent : RegionPackMaterialSlot.Terrain,
                            index == 1 || index == 3 || index == 5);
                    }

                    break;

                case RegionPackLandmarkStyle.KiteboneCrown:
                    CreateLandmarkPiece(game, definition, root.transform, "Kitebone mast",
                        PrimitiveType.Cylinder, new Vector3(0f, 3.8f, 0f), new Vector3(0.65f, 3.8f, 0.65f),
                        Vector3.zero, RegionPackMaterialSlot.Dark, true);
                    for (var index = 0; index < 4; index++)
                    {
                        var yaw = index * 90f;
                        CreateLandmarkPiece(game, definition, root.transform, $"Kitebone rib {index + 1}",
                            PrimitiveType.Cube,
                            new Vector3(0f, 5.4f + index % 2 * 0.9f, 0f),
                            new Vector3(6.8f - index * 0.55f, 0.22f, 0.48f),
                            new Vector3(index % 2 == 0 ? 11f : -11f, yaw, 0f),
                            index % 2 == 0 ? RegionPackMaterialSlot.Accent : RegionPackMaterialSlot.Terrain,
                            false);
                    }

                    for (var index = 0; index < 4; index++)
                    {
                        var angle = index * Mathf.PI * 0.5f;
                        CreateLandmarkPiece(game, definition, root.transform, $"Kitebone sail {index + 1}",
                            PrimitiveType.Cube,
                            new Vector3(Mathf.Cos(angle) * 3.1f, 5.6f, Mathf.Sin(angle) * 3.1f),
                            new Vector3(0.18f, 1.9f, 1.1f),
                            new Vector3(0f, index * 90f, index % 2 == 0 ? 24f : -24f),
                            RegionPackMaterialSlot.Accent,
                            false);
                    }

                    break;

                case RegionPackLandmarkStyle.LumenBeacon:
                    CreateLandmarkPiece(game, definition, root.transform, "Lumen beacon plinth",
                        PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(3.5f, 0.35f, 3.5f),
                        Vector3.zero, RegionPackMaterialSlot.Dark, true);
                    CreateLandmarkPiece(game, definition, root.transform, "Lumen beacon spine",
                        PrimitiveType.Cube, new Vector3(0f, 3.2f, 0f), new Vector3(0.58f, 6.4f, 0.58f),
                        new Vector3(0f, 18f, -4f), RegionPackMaterialSlot.Terrain, true);
                    for (var index = 0; index < 5; index++)
                    {
                        var offset = index - 2;
                        CreateLandmarkPiece(game, definition, root.transform, $"Walking lens {index + 1}",
                            PrimitiveType.Cube,
                            new Vector3(offset * 0.72f, 2.2f + index * 0.92f, offset % 2 * 0.38f),
                            new Vector3(2.5f - Mathf.Abs(offset) * 0.26f, 0.18f, 1.25f),
                            new Vector3(index * 6f, index * 31f, offset * 9f),
                            RegionPackMaterialSlot.Accent,
                            false);
                    }

                    break;

                case RegionPackLandmarkStyle.BrassrootCrucible:
                    CreateLandmarkPiece(game, definition, root.transform, "Brassroot crucible hearth",
                        PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(3.6f, 0.45f, 3.6f),
                        Vector3.zero, RegionPackMaterialSlot.Dark, true);
                    for (var index = 0; index < 6; index++)
                    {
                        var angle = index * 60f * Mathf.Deg2Rad;
                        CreateLandmarkPiece(game, definition, root.transform, $"Foundry root {index + 1}",
                            PrimitiveType.Cylinder,
                            new Vector3(Mathf.Cos(angle) * 2.1f, 2.6f + index * 0.38f,
                                Mathf.Sin(angle) * 2.1f),
                            new Vector3(0.42f, 3.1f + index * 0.28f, 0.42f),
                            new Vector3(Mathf.Sin(angle) * 27f, index * 60f, -Mathf.Cos(angle) * 27f),
                            index == 5 ? RegionPackMaterialSlot.Accent : RegionPackMaterialSlot.Terrain,
                            index < 2);
                    }

                    break;

                case RegionPackLandmarkStyle.CloudbarrowCairn:
                    CreateLandmarkPiece(game, definition, root.transform, "Cloudbarrow cairn bed",
                        PrimitiveType.Cube, new Vector3(0f, 0.7f, 0f), new Vector3(6.8f, 1.4f, 4.8f),
                        Vector3.zero, RegionPackMaterialSlot.Dark, true);
                    for (var index = 0; index < 5; index++)
                    {
                        var offset = index - 2;
                        CreateLandmarkPiece(game, definition, root.transform, $"Barrow sailstone {index + 1}",
                            PrimitiveType.Cube,
                            new Vector3(offset * 1.35f, 2.4f + Mathf.Abs(offset) * 0.48f,
                                index % 2 == 0 ? -0.5f : 0.5f),
                            new Vector3(0.62f, 3.3f + index * 0.34f, 1.1f),
                            new Vector3(offset * 5f, offset * 11f, offset * 8f),
                            index == 2 ? RegionPackMaterialSlot.Accent : RegionPackMaterialSlot.Terrain,
                            index < 3);
                    }

                    CreateLandmarkPiece(game, definition, root.transform, "Cloudbarrow capstone",
                        PrimitiveType.Cube, new Vector3(0f, 5.4f, 0f), new Vector3(6.1f, 0.5f, 1.1f),
                        new Vector3(0f, -8f, 3f), RegionPackMaterialSlot.Accent, false);
                    break;

                case RegionPackLandmarkStyle.TideglassNeedles:
                    CreateLandmarkPiece(game, definition, root.transform, "Tideglass needle reef",
                        PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(3.7f, 0.35f, 3.7f),
                        Vector3.zero, RegionPackMaterialSlot.Dark, true);
                    for (var index = 0; index < 6; index++)
                    {
                        var angle = index * 60f * Mathf.Deg2Rad;
                        CreateLandmarkPiece(game, definition, root.transform, $"Tideglass needle {index + 1}",
                            PrimitiveType.Cube,
                            new Vector3(Mathf.Cos(angle) * (1.2f + index * 0.2f),
                                2.2f + index * 0.58f,
                                Mathf.Sin(angle) * (1.2f + index * 0.2f)),
                            new Vector3(0.38f, 4.1f + index * 0.65f, 0.48f),
                            new Vector3(Mathf.Sin(angle) * 13f, index * 60f, Mathf.Cos(angle) * 18f),
                            index == 5 ? RegionPackMaterialSlot.Accent : RegionPackMaterialSlot.Terrain,
                            index < 2);
                    }

                    break;

                case RegionPackLandmarkStyle.NightloomLattice:
                    CreateLandmarkPiece(game, definition, root.transform, "Nightloom heart",
                        PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(3.3f, 0.4f, 3.3f),
                        Vector3.zero, RegionPackMaterialSlot.Dark, true);
                    for (var index = 0; index < 4; index++)
                    {
                        var angle = index * Mathf.PI * 0.5f;
                        CreateLandmarkPiece(game, definition, root.transform, $"Nightloom post {index + 1}",
                            PrimitiveType.Cylinder,
                            new Vector3(Mathf.Cos(angle) * 2.7f, 3.3f, Mathf.Sin(angle) * 2.7f),
                            new Vector3(0.38f, 3.3f, 0.38f),
                            new Vector3(Mathf.Sin(angle) * 12f, index * 90f, -Mathf.Cos(angle) * 12f),
                            RegionPackMaterialSlot.Terrain,
                            index < 2);
                    }

                    for (var index = 0; index < 4; index++)
                    {
                        CreateLandmarkPiece(game, definition, root.transform, $"Nightloom weave {index + 1}",
                            PrimitiveType.Cube,
                            new Vector3(0f, 4.2f + index * 0.72f, 0f),
                            new Vector3(6.4f - index * 0.54f, 0.17f, 0.42f),
                            new Vector3(index % 2 == 0 ? 7f : -7f, 45f + index * 43f, 0f),
                            RegionPackMaterialSlot.Accent,
                            false);
                    }

                    break;
            }

            WorldVisuals.AddLabel(
                root.transform,
                definition.landmarkName,
                new Vector3(0f, 8.1f, 0f),
                Color.Lerp(definition.accentColor, Color.white, 0.24f),
                0.11f);
            WorldVisuals.AddLabel(
                root.transform,
                definition.landmarkSubtitle,
                new Vector3(0f, 7.72f, 0f),
                Color.Lerp(definition.mapColor, Color.white, 0.2f),
                0.075f);
        }

        private static void BuildEncounterFootprint(
            TetherfallGame game,
            RegionPackDefinition definition,
            Transform detailRoot)
        {
            var root = new GameObject($"{definition.displayName} encounter footprint");
            root.transform.SetParent(detailRoot, false);
            root.transform.localPosition = definition.encounterLocalPosition;

            switch (definition.encounterFootprint)
            {
                case RegionPackEncounterFootprint.MirrorPalisade:
                    for (var index = 0; index < 5; index++)
                    {
                        var side = index % 2 == 0 ? -1f : 1f;
                        CreateFootprintPiece(game, definition, root.transform, index,
                            new Vector3(side * (8f + index), 1.2f, -8f + index * 4f),
                            new Vector3(0.75f, 2.4f + index * 0.2f, 3.4f),
                            new Vector3(0f, side * (18f + index * 7f), side * 8f),
                            index < 4);
                    }

                    break;

                case RegionPackEncounterFootprint.CoilRedoubt:
                    for (var index = 0; index < 5; index++)
                    {
                        var angle = index * 58f * Mathf.Deg2Rad;
                        var radius = 8.5f + index * 0.75f;
                        CreateFootprintPiece(game, definition, root.transform, index,
                            new Vector3(Mathf.Cos(angle) * radius, 1f + index * 0.16f, Mathf.Sin(angle) * radius),
                            new Vector3(3.6f, 2f + index * 0.32f, 1.1f),
                            new Vector3(0f, -angle * Mathf.Rad2Deg + 18f, 0f),
                            index < 4);
                    }

                    break;

                case RegionPackEncounterFootprint.RootSnare:
                    for (var index = 0; index < 5; index++)
                    {
                        var angle = index * 72f * Mathf.Deg2Rad + 0.4f;
                        CreateFootprintPiece(game, definition, root.transform, index,
                            new Vector3(Mathf.Cos(angle) * 9.5f, 1.25f, Mathf.Sin(angle) * 7.5f),
                            new Vector3(0.65f, 2.5f + index % 2, 0.65f),
                            new Vector3(Mathf.Sin(angle) * 24f, index * 72f, Mathf.Cos(angle) * 24f),
                            index < 4,
                            PrimitiveType.Cylinder);
                    }

                    break;

                case RegionPackEncounterFootprint.PrismTrench:
                    for (var index = 0; index < 5; index++)
                    {
                        CreateFootprintPiece(game, definition, root.transform, index,
                            new Vector3(-10f + index * 5f, 0.85f, index % 2 == 0 ? -8f : 8f),
                            new Vector3(4.2f, 1.7f, 1.25f),
                            new Vector3(0f, index % 2 == 0 ? 18f : -18f, index % 2 == 0 ? 6f : -6f),
                            index < 4);
                    }

                    break;

                case RegionPackEncounterFootprint.KiteRing:
                    for (var index = 0; index < 5; index++)
                    {
                        var angle = index * 72f * Mathf.Deg2Rad;
                        CreateFootprintPiece(game, definition, root.transform, index,
                            new Vector3(Mathf.Cos(angle) * 10f, 1.6f, Mathf.Sin(angle) * 10f),
                            new Vector3(0.42f, 3.2f, 1.8f),
                            new Vector3(0f, -angle * Mathf.Rad2Deg, index % 2 == 0 ? 18f : -18f),
                            index < 4);
                    }

                    break;

                case RegionPackEncounterFootprint.LensWalk:
                    for (var index = 0; index < 5; index++)
                    {
                        var side = index % 2 == 0 ? -1f : 1f;
                        CreateFootprintPiece(game, definition, root.transform, index,
                            new Vector3(-10f + index * 5f, 1f + index * 0.16f, side * (5f + index)),
                            new Vector3(3.8f, 2f + index * 0.28f, 0.75f),
                            new Vector3(side * 7f, index * 19f, side * 13f),
                            index < 4);
                    }

                    break;

                case RegionPackEncounterFootprint.FoundryTerrace:
                    for (var index = 0; index < 5; index++)
                    {
                        CreateFootprintPiece(game, definition, root.transform, index,
                            new Vector3(-9f + index * 4.5f, 0.65f + index * 0.42f,
                                index % 2 == 0 ? -7f : 7f),
                            new Vector3(4.1f, 1.3f + index * 0.52f, 2.1f),
                            new Vector3(0f, index % 2 == 0 ? 12f : -12f, 0f),
                            index < 4);
                    }

                    break;

                case RegionPackEncounterFootprint.BarrowGate:
                    for (var index = 0; index < 5; index++)
                    {
                        var side = index % 2 == 0 ? -1f : 1f;
                        CreateFootprintPiece(game, definition, root.transform, index,
                            new Vector3(side * (6f + index), 1.25f + index * 0.18f, -9f + index * 4.5f),
                            new Vector3(1.35f, 2.5f + index * 0.35f, 1.35f),
                            new Vector3(side * 8f, index * 34f, side * 12f),
                            index < 4,
                            PrimitiveType.Cylinder);
                    }

                    break;

                case RegionPackEncounterFootprint.TidebreakSpines:
                    for (var index = 0; index < 5; index++)
                    {
                        var offset = index - 2;
                        CreateFootprintPiece(game, definition, root.transform, index,
                            new Vector3(offset * 4.6f, 1.5f + Mathf.Abs(offset) * 0.3f,
                                offset % 2 == 0 ? -7f : 7f),
                            new Vector3(0.52f, 3f + Mathf.Abs(offset) * 0.72f, 1.4f),
                            new Vector3(offset * 5f, offset * 17f, offset * 11f),
                            index < 4);
                    }

                    break;

                case RegionPackEncounterFootprint.LoomAmbush:
                    for (var index = 0; index < 5; index++)
                    {
                        var angle = (index * 72f + 36f) * Mathf.Deg2Rad;
                        CreateFootprintPiece(game, definition, root.transform, index,
                            new Vector3(Mathf.Cos(angle) * 9f, 1.45f, Mathf.Sin(angle) * 7f),
                            new Vector3(0.62f, 2.9f + index * 0.28f, 0.62f),
                            new Vector3(Mathf.Sin(angle) * 18f, index * 72f, Mathf.Cos(angle) * 18f),
                            index < 4,
                            PrimitiveType.Cylinder);
                    }

                    break;
            }
        }

        private static void CreateFootprintPiece(
            TetherfallGame game,
            RegionPackDefinition definition,
            Transform parent,
            int index,
            Vector3 localPosition,
            Vector3 scale,
            Vector3 eulerAngles,
            bool solid,
            PrimitiveType primitive = PrimitiveType.Cube)
        {
            CreatePiece(
                game,
                definition,
                parent,
                $"{definition.encounterFootprint} cover {index + 1}",
                primitive,
                localPosition,
                scale,
                eulerAngles,
                index == 4 ? RegionPackMaterialSlot.Accent : RegionPackMaterialSlot.Dark,
                solid,
                false);
        }

        private static void CreateLandmarkPiece(
            TetherfallGame game,
            RegionPackDefinition definition,
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 scale,
            Vector3 eulerAngles,
            RegionPackMaterialSlot materialSlot,
            bool solid)
        {
            CreatePiece(
                game,
                definition,
                parent,
                name,
                primitive,
                localPosition,
                scale,
                eulerAngles,
                materialSlot,
                solid,
                solid);
        }

        private static GameObject CreatePiece(
            TetherfallGame game,
            RegionPackDefinition definition,
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 scale,
            Vector3 eulerAngles,
            RegionPackMaterialSlot materialSlot,
            bool solid,
            bool climbable)
        {
            var piece = GameObject.CreatePrimitive(primitive);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = scale;
            piece.transform.localRotation = Quaternion.Euler(eulerAngles);
            piece.GetComponent<Renderer>().sharedMaterial = GetMaterial(game, definition, materialSlot);
            var collider = piece.GetComponent<Collider>();
            if (!solid && collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }
            else if (solid && climbable && piece.GetComponent<ClimbableSurface>() == null)
            {
                piece.AddComponent<ClimbableSurface>();
            }

            return piece;
        }

        private static Material GetMaterial(
            TetherfallGame game,
            RegionPackDefinition definition,
            RegionPackMaterialSlot slot)
        {
            var color = slot switch
            {
                RegionPackMaterialSlot.Ground => definition.groundColor,
                RegionPackMaterialSlot.Terrain => definition.terrainColor,
                RegionPackMaterialSlot.Accent => definition.accentColor,
                _ => definition.darkColor
            };
            var metallic = slot == RegionPackMaterialSlot.Accent ? 0.2f : 0.04f;
            var smoothness = slot == RegionPackMaterialSlot.Accent ? 0.62f : 0.28f;
            return RuntimeEffectMaterialCache.GetOpaque(game, color, metallic, smoothness);
        }

        private static void AdoptLooseCampProps(Transform detailRoot, Vector3 campPosition)
        {
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (var candidate in transforms)
            {
                if (candidate == null ||
                    candidate.parent != null ||
                    !string.Equals(candidate.name, "Kenney tent_detailedOpen", StringComparison.Ordinal) ||
                    Vector3.Distance(candidate.position, campPosition) > 18f)
                {
                    continue;
                }

                candidate.SetParent(detailRoot, true);
            }
        }

        private static Texture2D CreateOverviewTexture(RegionPackDefinition definition)
        {
            const int width = 256;
            const int height = 72;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"{definition.displayName} generated regional study",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                var v = y / (float)(height - 1);
                for (var x = 0; x < width; x++)
                {
                    var u = x / (float)(width - 1);
                    var color = Color.Lerp(definition.fogColor, definition.skyColor, v);
                    var silhouetteHeight = SampleOverviewSilhouette(definition.landmarkStyle, u);
                    if (v <= silhouetteHeight)
                    {
                        var terrainBlend = Mathf.InverseLerp(0f, Mathf.Max(0.01f, silhouetteHeight), v);
                        color = Color.Lerp(definition.darkColor, definition.terrainColor, terrainBlend * 0.64f);
                    }

                    var routeLine = Mathf.Abs(v - (0.12f + Mathf.Sin(u * 7.4f) * 0.018f));
                    if (routeLine < 0.012f)
                    {
                        color = Color.Lerp(color, definition.accentColor, 0.72f);
                    }

                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static float SampleOverviewSilhouette(RegionPackLandmarkStyle style, float u)
        {
            var broad = 0.18f + Mathf.Sin(u * Mathf.PI * 3f) * 0.035f;
            return style switch
            {
                RegionPackLandmarkStyle.ChoirglassArch =>
                    broad + Mathf.Max(0f, 0.34f - Mathf.Abs(u - 0.5f) * 2.6f),
                RegionPackLandmarkStyle.RimecoilNeedle =>
                    broad + Mathf.Max(0f, 0.56f - Mathf.Abs(u - 0.57f) * 3.7f),
                RegionPackLandmarkStyle.HollowCrown =>
                    broad + Mathf.Max(0f, 0.31f - Mathf.Abs(u - 0.46f) * 1.5f) +
                    Mathf.Sin(u * Mathf.PI * 9f) * 0.025f,
                RegionPackLandmarkStyle.SplitSunArray =>
                    broad + Mathf.Max(0f, 0.42f - Mathf.Abs(u - 0.5f) * 2f) *
                    (0.66f + Mathf.Abs(Mathf.Sin(u * Mathf.PI * 13f)) * 0.34f),
                RegionPackLandmarkStyle.KiteboneCrown =>
                    broad + Mathf.Max(0f, 0.36f - Mathf.Abs(u - 0.52f) * 2.2f) +
                    Mathf.Abs(Mathf.Sin(u * Mathf.PI * 5f)) * 0.04f,
                RegionPackLandmarkStyle.LumenBeacon =>
                    broad + Mathf.Max(0f, 0.5f - Mathf.Abs(u - 0.44f) * 4f) +
                    Mathf.Abs(Mathf.Sin(u * Mathf.PI * 11f)) * 0.025f,
                RegionPackLandmarkStyle.BrassrootCrucible =>
                    broad + Mathf.Max(0f, 0.39f - Mathf.Abs(u - 0.5f) * 1.8f) +
                    Mathf.Abs(Mathf.Cos(u * Mathf.PI * 7f)) * 0.045f,
                RegionPackLandmarkStyle.CloudbarrowCairn =>
                    broad + Mathf.Max(0f, 0.33f - Mathf.Abs(u - 0.5f) * 1.25f) +
                    Mathf.Abs(Mathf.Sin(u * Mathf.PI * 6f)) * 0.032f,
                RegionPackLandmarkStyle.TideglassNeedles =>
                    broad + Mathf.Max(0f, 0.46f - Mathf.Abs(u - 0.52f) * 2.4f) *
                    (0.62f + Mathf.Abs(Mathf.Sin(u * Mathf.PI * 15f)) * 0.38f),
                _ =>
                    broad + Mathf.Max(0f, 0.37f - Mathf.Abs(u - 0.5f) * 1.7f) +
                    Mathf.Abs(Mathf.Sin(u * Mathf.PI * 8f)) * 0.052f
            };
        }

        private static int GetLandmarkRendererCount(RegionPackLandmarkStyle style)
        {
            return style switch
            {
                RegionPackLandmarkStyle.ChoirglassArch => 10,
                RegionPackLandmarkStyle.RimecoilNeedle => 9,
                RegionPackLandmarkStyle.HollowCrown => 9,
                RegionPackLandmarkStyle.SplitSunArray => 9,
                RegionPackLandmarkStyle.KiteboneCrown => 11,
                RegionPackLandmarkStyle.LumenBeacon => 9,
                RegionPackLandmarkStyle.BrassrootCrucible => 9,
                RegionPackLandmarkStyle.CloudbarrowCairn => 9,
                RegionPackLandmarkStyle.TideglassNeedles => 9,
                _ => 11
            };
        }

        private static int GetLandmarkColliderCount(RegionPackLandmarkStyle style)
        {
            return style switch
            {
                RegionPackLandmarkStyle.ChoirglassArch => 2,
                RegionPackLandmarkStyle.RimecoilNeedle => 3,
                RegionPackLandmarkStyle.HollowCrown => 4,
                RegionPackLandmarkStyle.SplitSunArray => 3,
                RegionPackLandmarkStyle.KiteboneCrown => 1,
                RegionPackLandmarkStyle.LumenBeacon => 2,
                RegionPackLandmarkStyle.BrassrootCrucible => 3,
                RegionPackLandmarkStyle.CloudbarrowCairn => 4,
                RegionPackLandmarkStyle.TideglassNeedles => 3,
                _ => 3
            };
        }

        private static List<RegionPackDefinition> CreatePlayableDefinitions()
        {
            var result = new List<RegionPackDefinition>(PlayableRegionPackCount);
            foreach (var definition in Definitions)
            {
                if (definition.status == RegionPackStatus.Playable)
                {
                    result.Add(definition);
                }
            }

            return result;
        }

        private static List<RegionPackDefinition> CreateDefinitions()
        {
            return new List<RegionPackDefinition>
            {
                CreateGlassmere(),
                CreateRimecoil(),
                CreateSunkenWeald(),
                CreatePrismscar(),
                CreateGalecradle(),
                CreateLumenwaste(),
                CreateBrassroot(),
                CreateCloudbarrow(),
                CreateTideglass(),
                CreateNightloom()
            };
        }

        private static RegionPackDefinition CreateGlassmere()
        {
            var center = new Vector3(0f, 0f, 220f);
            return new RegionPackDefinition
            {
                id = "region.glassmere",
                displayName = "Glassmere Steppe",
                status = RegionPackStatus.Playable,
                biome = "rain-polished saltglass terraces",
                silhouette = "low mirrored shelves gathered around a tuning arch",
                traversalNote = "Three offset terraces and shallow glass ramps reward lateral climbing.",
                discoveryText = "Saltglass shelves hold the rain like a second sky. The Choirglass Arch answers careful footsteps.",
                authoringBrief = "Playable first-wave saltglass district.",
                mapSummary = "Mirrored terraces, choir arch, and refraction cache.",
                center = center,
                size = new Vector2(140f, 140f),
                activationRadius = 112f,
                mapColor = new Color(0.38f, 0.78f, 0.86f),
                groundColor = new Color(0.23f, 0.38f, 0.42f),
                terrainColor = new Color(0.5f, 0.72f, 0.75f),
                accentColor = new Color(0.74f, 0.96f, 0.92f),
                darkColor = new Color(0.09f, 0.17f, 0.2f),
                ambientColor = new Color(0.3f, 0.46f, 0.49f),
                fogColor = new Color(0.3f, 0.5f, 0.55f),
                sunColor = new Color(0.84f, 0.96f, 0.91f),
                skyColor = new Color(0.38f, 0.67f, 0.73f),
                fogDensity = 0.0036f,
                connectorStart = new Vector3(0f, 0f, 128f),
                connectorEnd = new Vector3(0f, 0f, 152f),
                connectorWidth = 14f,
                terrainPieces = new[]
                {
                    Piece("West mirror shelf", new Vector3(-32f, 0.9f, -8f), new Vector3(34f, 1.8f, 28f),
                        Vector3.zero, RegionPackMaterialSlot.Terrain),
                    Piece("Choir middle shelf", new Vector3(4f, 2.1f, 5f), new Vector3(32f, 4.2f, 30f),
                        Vector3.zero, RegionPackMaterialSlot.Ground),
                    Piece("East high shelf", new Vector3(36f, 3.5f, 24f), new Vector3(26f, 7f, 24f),
                        Vector3.zero, RegionPackMaterialSlot.Terrain, true),
                    Piece("West glass ramp", new Vector3(-13f, 1.2f, -4f), new Vector3(22f, 0.9f, 10f),
                        new Vector3(0f, 0f, -9f), RegionPackMaterialSlot.Accent),
                    Piece("East glass ramp", new Vector3(22f, 3.1f, 16f), new Vector3(22f, 0.9f, 9f),
                        new Vector3(0f, 0f, -12f), RegionPackMaterialSlot.Accent),
                    Piece("North mirror ridge", new Vector3(-24f, 1.45f, 38f), new Vector3(38f, 2.9f, 9f),
                        new Vector3(0f, -8f, 0f), RegionPackMaterialSlot.Dark),
                    Piece("Fallen glass fin", PrimitiveType.Cube, new Vector3(48f, 2.8f, -30f),
                        new Vector3(1.1f, 7f, 9f), new Vector3(16f, 24f, 18f),
                        RegionPackMaterialSlot.Accent, false, false),
                    Piece("Glassmere horizon fin", PrimitiveType.Cube, new Vector3(-48f, 3.6f, 32f),
                        new Vector3(0.9f, 9f, 5f), new Vector3(-9f, -18f, -14f),
                        RegionPackMaterialSlot.Terrain, false, false)
                },
                landmarkStyle = RegionPackLandmarkStyle.ChoirglassArch,
                landmarkName = "Choirglass Arch",
                landmarkSubtitle = "Rain carries five remembered notes",
                landmarkLocalPosition = new Vector3(35f, 7f, 24f),
                secretId = "secret.glassmere.refraction_niche",
                secretName = "Refraction Niche",
                secretInscription = "A routewright hid this chimesalt where the arch's fifth reflection breaks.",
                secretLocalPosition = new Vector3(47f, 0.45f, 38f),
                secretResource = ResourceKind.Chimesalt,
                secretResourceAmount = 3,
                encounterId = "encounter.morrowfen.drowned_choir",
                encounterPersistenceId = "encounter.region.glassmere.mirror_palisade",
                encounterLocalPosition = new Vector3(-38f, 0f, 34f),
                encounterFootprint = RegionPackEncounterFootprint.MirrorPalisade,
                resources = new[]
                {
                    Resource(ResourceKind.Chimesalt, -52f, 0.75f, -28f, new Color(0.65f, 0.9f, 0.94f)),
                    Resource(ResourceKind.Chimesalt, 52f, 0.75f, -12f, new Color(0.65f, 0.9f, 0.94f)),
                    Resource(ResourceKind.Stone, -48f, 0.75f, 9f, new Color(0.52f, 0.66f, 0.69f)),
                    Resource(ResourceKind.Stone, 14f, 0.75f, -46f, new Color(0.52f, 0.66f, 0.69f)),
                    Resource(ResourceKind.Fiber, 48f, 0.75f, 49f, new Color(0.68f, 0.84f, 0.7f)),
                    Resource(ResourceKind.Fiber, -5f, 0.75f, 50f, new Color(0.68f, 0.84f, 0.7f))
                },
                threadway = Threadway(
                    "threadway.glassmere",
                    "Glassmere Tuning Fork",
                    "Glassmere Steppe",
                    "A saltglass fork aligned to the Choirglass Arch.",
                    center + new Vector3(0f, 0f, -50f),
                    center + new Vector3(0f, 0.05f, -46f),
                    2,
                    new Color(0.53f, 0.92f, 0.9f))
            };
        }

        private static RegionPackDefinition CreateRimecoil()
        {
            var center = new Vector3(170f, 0f, 220f);
            return new RegionPackDefinition
            {
                id = "region.rimecoil",
                displayName = "Rimecoil Range",
                status = RegionPackStatus.Playable,
                biome = "wind-scoured blue-ice switchbacks",
                silhouette = "a spiral stair of dark shelves beneath one pale needle",
                traversalNote = "A four-stage switchback climbs to a needle with marked climb faces.",
                discoveryText = "Blue ice coils around old basalt. Every switchback turns the wind into a different instrument.",
                authoringBrief = "Playable first-wave alpine switchback district.",
                mapSummary = "Ice switchbacks, rime needle, and a sheltered coil cache.",
                center = center,
                size = new Vector2(140f, 140f),
                activationRadius = 112f,
                mapColor = new Color(0.31f, 0.45f, 0.86f),
                groundColor = new Color(0.18f, 0.25f, 0.34f),
                terrainColor = new Color(0.42f, 0.57f, 0.72f),
                accentColor = new Color(0.76f, 0.9f, 1f),
                darkColor = new Color(0.07f, 0.1f, 0.17f),
                ambientColor = new Color(0.29f, 0.37f, 0.5f),
                fogColor = new Color(0.38f, 0.5f, 0.64f),
                sunColor = new Color(0.77f, 0.88f, 1f),
                skyColor = new Color(0.3f, 0.47f, 0.68f),
                fogDensity = 0.0045f,
                connectorStart = new Vector3(170f, 0f, 128f),
                connectorEnd = new Vector3(170f, 0f, 152f),
                connectorWidth = 12f,
                terrainPieces = new[]
                {
                    Piece("Rimecoil first turn", new Vector3(-35f, 1f, -25f), new Vector3(32f, 2f, 24f),
                        Vector3.zero, RegionPackMaterialSlot.Ground),
                    Piece("Rimecoil second turn", new Vector3(-10f, 2.3f, -4f), new Vector3(28f, 4.6f, 22f),
                        Vector3.zero, RegionPackMaterialSlot.Terrain),
                    Piece("Rimecoil third turn", new Vector3(15f, 4.1f, 16f), new Vector3(25f, 8.2f, 22f),
                        Vector3.zero, RegionPackMaterialSlot.Ground, true),
                    Piece("Rimecoil crown shelf", new Vector3(39f, 6.2f, 34f), new Vector3(24f, 12.4f, 23f),
                        Vector3.zero, RegionPackMaterialSlot.Terrain, true),
                    Piece("Lower ice switchback", new Vector3(-22f, 1.65f, -15f), new Vector3(21f, 0.9f, 8f),
                        new Vector3(0f, 39f, -10f), RegionPackMaterialSlot.Accent),
                    Piece("Middle ice switchback", new Vector3(3f, 3.4f, 6f), new Vector3(20f, 0.9f, 8f),
                        new Vector3(0f, 38f, -13f), RegionPackMaterialSlot.Accent),
                    Piece("Upper ice switchback", new Vector3(28f, 5.45f, 26f), new Vector3(20f, 0.9f, 8f),
                        new Vector3(0f, 38f, -14f), RegionPackMaterialSlot.Accent),
                    Piece("Rimecoil windbreak", new Vector3(-49f, 2.8f, 28f), new Vector3(7f, 5.6f, 28f),
                        new Vector3(0f, 13f, 0f), RegionPackMaterialSlot.Dark, true)
                },
                landmarkStyle = RegionPackLandmarkStyle.RimecoilNeedle,
                landmarkName = "Rimecoil Needle",
                landmarkSubtitle = "The high route turns around it",
                landmarkLocalPosition = new Vector3(39f, 12.4f, 34f),
                secretId = "secret.rimecoil.blue_cleft",
                secretName = "Blue Cleft Cache",
                secretInscription = "A survey coil remains warm beneath ice that has never seen direct sun.",
                secretLocalPosition = new Vector3(-52f, 0.45f, 42f),
                secretResource = ResourceKind.Ore,
                secretResourceAmount = 3,
                encounterId = "encounter.alderwake.ridge",
                encounterPersistenceId = "encounter.region.rimecoil.coil_redoubt",
                encounterLocalPosition = new Vector3(-36f, 0f, 34f),
                encounterFootprint = RegionPackEncounterFootprint.CoilRedoubt,
                resources = new[]
                {
                    Resource(ResourceKind.Ore, -54f, 0.75f, -18f, new Color(0.5f, 0.61f, 0.78f)),
                    Resource(ResourceKind.Ore, 53f, 0.75f, -34f, new Color(0.5f, 0.61f, 0.78f)),
                    Resource(ResourceKind.Fiber, -12f, 0.75f, 49f, new Color(0.62f, 0.77f, 0.82f)),
                    Resource(ResourceKind.Fiber, 47f, 0.75f, 54f, new Color(0.62f, 0.77f, 0.82f)),
                    Resource(ResourceKind.Chimesalt, -49f, 0.75f, 8f, new Color(0.76f, 0.86f, 1f)),
                    Resource(ResourceKind.Chimesalt, 24f, 0.75f, -49f, new Color(0.76f, 0.86f, 1f))
                },
                threadway = Threadway(
                    "threadway.rimecoil",
                    "Rimecoil Turn",
                    "Rimecoil Range",
                    "A frost-locked turnstone at the foot of the switchbacks.",
                    center + new Vector3(0f, 0f, -50f),
                    center + new Vector3(0f, 0.05f, -46f),
                    3,
                    new Color(0.63f, 0.82f, 1f))
            };
        }

        private static RegionPackDefinition CreateSunkenWeald()
        {
            var center = new Vector3(-170f, 0f, 220f);
            return new RegionPackDefinition
            {
                id = "region.sunken_weald",
                displayName = "Sunken Weald",
                status = RegionPackStatus.Playable,
                biome = "amber-canopy floodwood basin",
                silhouette = "wide root crowns over broken water and narrow causeways",
                traversalNote = "Raised root roads cross a swimmable basin and climb onto two dry islands.",
                discoveryText = "The weald drowned standing up. Amber crowns still braid a dry road above the rootwater.",
                authoringBrief = "Playable first-wave flooded root district.",
                mapSummary = "Swimmable rootwater, crown causeways, and a hollow-heart cache.",
                center = center,
                size = new Vector2(140f, 140f),
                activationRadius = 112f,
                mapColor = new Color(0.62f, 0.7f, 0.36f),
                groundColor = new Color(0.18f, 0.24f, 0.17f),
                terrainColor = new Color(0.36f, 0.31f, 0.17f),
                accentColor = new Color(0.86f, 0.67f, 0.24f),
                darkColor = new Color(0.08f, 0.13f, 0.1f),
                ambientColor = new Color(0.34f, 0.4f, 0.24f),
                fogColor = new Color(0.27f, 0.36f, 0.26f),
                sunColor = new Color(0.96f, 0.74f, 0.36f),
                skyColor = new Color(0.39f, 0.53f, 0.43f),
                fogDensity = 0.005f,
                connectorStart = new Vector3(-170f, 0f, 128f),
                connectorEnd = new Vector3(-170f, 0f, 152f),
                connectorWidth = 13f,
                terrainPieces = new[]
                {
                    Piece("West dry island", new Vector3(-38f, 1.1f, -18f), new Vector3(30f, 2.2f, 29f),
                        Vector3.zero, RegionPackMaterialSlot.Ground),
                    Piece("Crown island", new Vector3(34f, 2.1f, 24f), new Vector3(31f, 4.2f, 30f),
                        Vector3.zero, RegionPackMaterialSlot.Terrain, true),
                    Piece("South root causeway", new Vector3(-10f, 1.1f, -34f), new Vector3(46f, 1.1f, 6f),
                        new Vector3(0f, 9f, 0f), RegionPackMaterialSlot.Terrain),
                    Piece("Middle root causeway", new Vector3(2f, 1.55f, -4f), new Vector3(54f, 1.1f, 6f),
                        new Vector3(0f, -28f, 0f), RegionPackMaterialSlot.Terrain),
                    Piece("Crown root rise", new Vector3(22f, 2f, 12f), new Vector3(26f, 0.9f, 7f),
                        new Vector3(-8f, -32f, 0f), RegionPackMaterialSlot.Accent),
                    Piece("North root bridge", new Vector3(-15f, 2.2f, 35f), new Vector3(58f, 1f, 6f),
                        new Vector3(0f, 16f, 0f), RegionPackMaterialSlot.Dark),
                    Piece("Drowned trunk west", PrimitiveType.Cylinder, new Vector3(-53f, 4.2f, 31f),
                        new Vector3(1.4f, 5.4f, 1.4f), new Vector3(12f, -18f, 19f),
                        RegionPackMaterialSlot.Dark, false, false),
                    Piece("Drowned trunk east", PrimitiveType.Cylinder, new Vector3(51f, 4.8f, -27f),
                        new Vector3(1.1f, 6.2f, 1.1f), new Vector3(-15f, 24f, -21f),
                        RegionPackMaterialSlot.Terrain, false, false)
                },
                hasWater = true,
                waterLocalPosition = new Vector3(0f, 0.04f, 3f),
                waterSize = new Vector3(92f, 2.2f, 86f),
                waterColor = new Color(0.08f, 0.24f, 0.23f, 0.74f),
                landmarkStyle = RegionPackLandmarkStyle.HollowCrown,
                landmarkName = "The Hollow Crown",
                landmarkSubtitle = "Dry roots remember the drowned road",
                landmarkLocalPosition = new Vector3(34f, 4.2f, 24f),
                secretId = "secret.sunken_weald.hollow_heart",
                secretName = "Hollow-Heart Cache",
                secretInscription = "Inside the crown, a grower sealed resin before the flood reached the roots.",
                secretLocalPosition = new Vector3(44f, 4.65f, 31f),
                secretResource = ResourceKind.Resin,
                secretResourceAmount = 3,
                encounterId = "encounter.morrowfen.fen_tithe",
                encounterPersistenceId = "encounter.region.sunken_weald.root_snare",
                encounterLocalPosition = new Vector3(-40f, 2.2f, -18f),
                encounterFootprint = RegionPackEncounterFootprint.RootSnare,
                resources = new[]
                {
                    Resource(ResourceKind.Herbs, -51f, 0.75f, 48f, new Color(0.52f, 0.78f, 0.36f)),
                    Resource(ResourceKind.Herbs, 52f, 0.75f, -46f, new Color(0.52f, 0.78f, 0.36f)),
                    Resource(ResourceKind.Resin, -54f, 0.75f, -46f, new Color(0.88f, 0.62f, 0.2f)),
                    Resource(ResourceKind.Resin, 49f, 0.75f, 49f, new Color(0.88f, 0.62f, 0.2f)),
                    Resource(ResourceKind.Timber, -9f, 0.75f, 54f, new Color(0.42f, 0.28f, 0.14f)),
                    Resource(ResourceKind.Timber, 56f, 0.75f, 2f, new Color(0.42f, 0.28f, 0.14f))
                },
                threadway = Threadway(
                    "threadway.sunken_weald",
                    "Weald Root-Loom",
                    "Sunken Weald",
                    "A root-bound loom kept above the flood by an old causeway.",
                    center + new Vector3(0f, 0f, -50f),
                    center + new Vector3(0f, 0.05f, -46f),
                    2,
                    new Color(0.74f, 0.79f, 0.34f))
            };
        }

        private static RegionPackDefinition CreatePrismscar()
        {
            var center = new Vector3(-90f, 0f, -220f);
            return new RegionPackDefinition
            {
                id = "region.prismscar",
                displayName = "Prismscar Downs",
                status = RegionPackStatus.Playable,
                biome = "violet flint downs and split-light trenches",
                silhouette = "sawtooth ridges cut by a tilted solar array",
                traversalNote = "Zigzag trenches, two ridge ramps, and a climbable scar split the downs.",
                discoveryText = "Broken prisms comb the downs into violet shadows. The Split-Sun Array still points below the horizon.",
                authoringBrief = "Playable first-wave prism trench district.",
                mapSummary = "Sawtooth ridges, split-sun vanes, and a shadowline cache.",
                center = center,
                size = new Vector2(140f, 140f),
                activationRadius = 112f,
                mapColor = new Color(0.72f, 0.48f, 0.84f),
                groundColor = new Color(0.29f, 0.2f, 0.32f),
                terrainColor = new Color(0.48f, 0.34f, 0.52f),
                accentColor = new Color(0.95f, 0.69f, 0.3f),
                darkColor = new Color(0.12f, 0.07f, 0.16f),
                ambientColor = new Color(0.4f, 0.29f, 0.43f),
                fogColor = new Color(0.39f, 0.27f, 0.44f),
                sunColor = new Color(1f, 0.68f, 0.33f),
                skyColor = new Color(0.5f, 0.32f, 0.59f),
                fogDensity = 0.0038f,
                connectorStart = new Vector3(-90f, 0f, -128f),
                connectorEnd = new Vector3(-90f, 0f, -152f),
                connectorWidth = 13f,
                terrainPieces = new[]
                {
                    Piece("West prism ridge", new Vector3(-35f, 1.8f, -14f), new Vector3(28f, 3.6f, 42f),
                        new Vector3(0f, -12f, 0f), RegionPackMaterialSlot.Terrain, true),
                    Piece("East prism ridge", new Vector3(31f, 3.4f, 18f), new Vector3(28f, 6.8f, 40f),
                        new Vector3(0f, 14f, 0f), RegionPackMaterialSlot.Ground, true),
                    Piece("South scar shelf", new Vector3(2f, 1.1f, -44f), new Vector3(52f, 2.2f, 19f),
                        Vector3.zero, RegionPackMaterialSlot.Dark),
                    Piece("West scar ramp", new Vector3(-17f, 1.25f, -17f), new Vector3(24f, 1f, 9f),
                        new Vector3(0f, -12f, -11f), RegionPackMaterialSlot.Accent),
                    Piece("East scar ramp", new Vector3(17f, 2.7f, 7f), new Vector3(25f, 1f, 9f),
                        new Vector3(0f, 14f, -14f), RegionPackMaterialSlot.Accent),
                    Piece("North prism traverse", new Vector3(-2f, 4.1f, 43f), new Vector3(55f, 1.3f, 8f),
                        new Vector3(0f, -5f, 0f), RegionPackMaterialSlot.Terrain),
                    Piece("Prismscar west fin", PrimitiveType.Cube, new Vector3(-51f, 4.7f, 35f),
                        new Vector3(1f, 10f, 5f), new Vector3(8f, -24f, -17f),
                        RegionPackMaterialSlot.Accent, false, false),
                    Piece("Prismscar east fin", PrimitiveType.Cube, new Vector3(51f, 5.5f, -31f),
                        new Vector3(1.1f, 12f, 4f), new Vector3(-7f, 26f, 19f),
                        RegionPackMaterialSlot.Terrain, false, false)
                },
                landmarkStyle = RegionPackLandmarkStyle.SplitSunArray,
                landmarkName = "Split-Sun Array",
                landmarkSubtitle = "Seven vanes, one buried bearing",
                landmarkLocalPosition = new Vector3(31f, 6.8f, 18f),
                secretId = "secret.prismscar.shadowline",
                secretName = "Shadowline Cache",
                secretInscription = "The array's shortest shadow marks a surveyor's sealed ore case.",
                secretLocalPosition = new Vector3(48f, 0.45f, 42f),
                secretResource = ResourceKind.Ore,
                secretResourceAmount = 3,
                encounterId = "encounter.cinderwold.relay_breakers",
                encounterPersistenceId = "encounter.region.prismscar.prism_trench",
                encounterLocalPosition = new Vector3(-39f, 3.6f, -14f),
                encounterFootprint = RegionPackEncounterFootprint.PrismTrench,
                resources = new[]
                {
                    Resource(ResourceKind.Stone, -53f, 0.75f, -47f, new Color(0.57f, 0.44f, 0.62f)),
                    Resource(ResourceKind.Stone, 51f, 0.75f, 51f, new Color(0.57f, 0.44f, 0.62f)),
                    Resource(ResourceKind.Ore, -50f, 0.75f, 45f, new Color(0.7f, 0.5f, 0.79f)),
                    Resource(ResourceKind.Ore, 49f, 0.75f, -49f, new Color(0.7f, 0.5f, 0.79f)),
                    Resource(ResourceKind.Chimesalt, -5f, 0.75f, 53f, new Color(0.91f, 0.66f, 0.37f)),
                    Resource(ResourceKind.Chimesalt, 8f, 0.75f, -51f, new Color(0.91f, 0.66f, 0.37f))
                },
                threadway = Threadway(
                    "threadway.prismscar",
                    "Prismscar Meridian",
                    "Prismscar Downs",
                    "A meridian stone that catches the Split-Sun Array's lowest ray.",
                    center + new Vector3(0f, 0f, 50f),
                    center + new Vector3(0f, 0.05f, 46f),
                    2,
                    new Color(0.87f, 0.56f, 0.88f))
            };
        }

        private static RegionPackDefinition CreateGalecradle()
        {
            var center = new Vector3(100f, 0f, -220f);
            return new RegionPackDefinition
            {
                id = "region.galecradle",
                displayName = "Galecradle Mesa",
                status = RegionPackStatus.Playable,
                biome = "ochre wind mesa and turquoise updraft bowls",
                silhouette = "paired mesas bridged beneath a kitebone crown",
                traversalNote = "A bounded updraft opens the high mesa, with a ramp route as the grounded alternative.",
                discoveryText = "Wind pools in the ochre bowls, then climbs. Kitebone ribs mark a high path across the cradle.",
                authoringBrief = "Playable first-wave wind-mesa district.",
                mapSummary = "Mesa ramps, bounded updraft, kitebone crown, and high cache.",
                center = center,
                size = new Vector2(140f, 140f),
                activationRadius = 112f,
                mapColor = new Color(0.3f, 0.76f, 0.7f),
                groundColor = new Color(0.36f, 0.25f, 0.15f),
                terrainColor = new Color(0.64f, 0.42f, 0.2f),
                accentColor = new Color(0.25f, 0.85f, 0.78f),
                darkColor = new Color(0.12f, 0.12f, 0.11f),
                ambientColor = new Color(0.43f, 0.38f, 0.28f),
                fogColor = new Color(0.48f, 0.43f, 0.34f),
                sunColor = new Color(1f, 0.76f, 0.4f),
                skyColor = new Color(0.35f, 0.62f, 0.68f),
                fogDensity = 0.0032f,
                connectorStart = new Vector3(100f, 0f, -128f),
                connectorEnd = new Vector3(100f, 0f, -152f),
                connectorWidth = 14f,
                terrainPieces = new[]
                {
                    Piece("West gale mesa", new Vector3(-31f, 3f, -8f), new Vector3(32f, 6f, 34f),
                        Vector3.zero, RegionPackMaterialSlot.Terrain, true),
                    Piece("East gale mesa", new Vector3(34f, 5.2f, 22f), new Vector3(34f, 10.4f, 32f),
                        Vector3.zero, RegionPackMaterialSlot.Ground, true),
                    Piece("South wind bowl rim", new Vector3(0f, 1.4f, -43f), new Vector3(56f, 2.8f, 16f),
                        Vector3.zero, RegionPackMaterialSlot.Dark),
                    Piece("West mesa ramp", new Vector3(-13f, 2.1f, -11f), new Vector3(25f, 1f, 10f),
                        new Vector3(0f, 0f, -14f), RegionPackMaterialSlot.Accent),
                    Piece("East mesa ramp", new Vector3(17f, 4f, 9f), new Vector3(25f, 1f, 10f),
                        new Vector3(0f, 0f, -16f), RegionPackMaterialSlot.Accent),
                    Piece("Kitebone sky bridge", new Vector3(3f, 6.2f, 12f), new Vector3(43f, 1.1f, 7f),
                        new Vector3(0f, 25f, 0f), RegionPackMaterialSlot.Terrain),
                    Piece("West wind fang", PrimitiveType.Cube, new Vector3(-52f, 5.8f, 37f),
                        new Vector3(3f, 12f, 4f), new Vector3(6f, 13f, 16f),
                        RegionPackMaterialSlot.Dark, false, false),
                    Piece("East wind fang", PrimitiveType.Cube, new Vector3(53f, 6.8f, -28f),
                        new Vector3(3f, 14f, 4f), new Vector3(-7f, -18f, -14f),
                        RegionPackMaterialSlot.Terrain, false, false)
                },
                hasWindLift = true,
                windLiftLocalPosition = new Vector3(-4f, 4f, -31f),
                windLiftSize = new Vector3(10f, 8f, 10f),
                windLiftVelocity = 8.4f,
                landmarkStyle = RegionPackLandmarkStyle.KiteboneCrown,
                landmarkName = "Kitebone Crown",
                landmarkSubtitle = "The mesa keeps what the wind can lift",
                landmarkLocalPosition = new Vector3(34f, 10.4f, 22f),
                secretId = "secret.galecradle.high_perch",
                secretName = "High-Perch Cache",
                secretInscription = "A courier lashed fiber here for anyone who could read the updraft.",
                secretLocalPosition = new Vector3(45f, 10.85f, 30f),
                secretResource = ResourceKind.Fiber,
                secretResourceAmount = 3,
                encounterId = "encounter.cinderwold.ashjaw",
                encounterPersistenceId = "encounter.region.galecradle.kite_ring",
                encounterLocalPosition = new Vector3(-35f, 6f, -8f),
                encounterFootprint = RegionPackEncounterFootprint.KiteRing,
                resources = new[]
                {
                    Resource(ResourceKind.Fiber, -51f, 0.75f, -48f, new Color(0.48f, 0.79f, 0.62f)),
                    Resource(ResourceKind.Fiber, 51f, 0.75f, 48f, new Color(0.48f, 0.79f, 0.62f)),
                    Resource(ResourceKind.Ore, -52f, 0.75f, 45f, new Color(0.62f, 0.58f, 0.52f)),
                    Resource(ResourceKind.Ore, 52f, 0.75f, -45f, new Color(0.62f, 0.58f, 0.52f)),
                    Resource(ResourceKind.Resin, -4f, 0.75f, 52f, new Color(0.84f, 0.57f, 0.22f)),
                    Resource(ResourceKind.Resin, 8f, 0.75f, -52f, new Color(0.84f, 0.57f, 0.22f))
                },
                threadway = Threadway(
                    "threadway.galecradle",
                    "Galecradle Yoke",
                    "Galecradle Mesa",
                    "A wind-yoke sunk into the mesa's sheltered southern lip.",
                    center + new Vector3(0f, 0f, 50f),
                    center + new Vector3(0f, 0.05f, 46f),
                    3,
                    new Color(0.35f, 0.88f, 0.78f))
            };
        }

        private static RegionPackDefinition CreateLumenwaste()
        {
            var center = new Vector3(-380f, 0f, 120f);
            return new RegionPackDefinition
            {
                id = "region.lumenwaste",
                displayName = "Lumenwaste Reach",
                status = RegionPackStatus.Playable,
                biome = "phosphor dunes and lens storms",
                silhouette = "low luminous dunes split by a walking beacon line",
                traversalNote = "A dark eastern approach feeds two lens ramps and a climbable beacon dune.",
                discoveryText = "Phosphor sand remembers every passing shadow. A walking line of lenses still points west.",
                authoringBrief = "Playable bounded phosphor-dune route with static lens shadows.",
                mapSummary = "Phosphor dunes, walking beacon, and a lens-shadow cache.",
                center = center,
                size = new Vector2(140f, 140f),
                activationRadius = 112f,
                mapColor = new Color(0.92f, 0.86f, 0.18f),
                groundColor = new Color(0.29f, 0.27f, 0.12f),
                terrainColor = new Color(0.65f, 0.58f, 0.18f),
                accentColor = new Color(1f, 0.94f, 0.38f),
                darkColor = new Color(0.13f, 0.11f, 0.05f),
                ambientColor = new Color(0.48f, 0.44f, 0.22f),
                fogColor = new Color(0.58f, 0.53f, 0.28f),
                sunColor = new Color(1f, 0.91f, 0.5f),
                skyColor = new Color(0.58f, 0.63f, 0.44f),
                fogDensity = 0.0031f,
                connectorStart = new Vector3(-238f, 0f, 120f),
                connectorEnd = new Vector3(-308f, 0f, 120f),
                connectorWidth = 14f,
                terrainPieces = new[]
                {
                    Piece("South phosphor dune", new Vector3(-28f, 1.2f, -31f),
                        new Vector3(36f, 2.4f, 24f), Vector3.zero, RegionPackMaterialSlot.Ground),
                    Piece("Central lens dune", new Vector3(0f, 2.6f, -3f),
                        new Vector3(38f, 5.2f, 27f), Vector3.zero, RegionPackMaterialSlot.Terrain),
                    Piece("North beacon dune", new Vector3(34f, 4.5f, 30f),
                        new Vector3(28f, 9f, 26f), Vector3.zero, RegionPackMaterialSlot.Ground, true),
                    Piece("Western shadow ridge", new Vector3(-45f, 3.1f, 24f),
                        new Vector3(12f, 6.2f, 38f), new Vector3(0f, -8f, 0f),
                        RegionPackMaterialSlot.Dark, true),
                    Piece("Lower lens ramp", new Vector3(-14f, 1.8f, -17f),
                        new Vector3(24f, 1f, 9f), new Vector3(0f, 0f, -10f),
                        RegionPackMaterialSlot.Accent),
                    Piece("Upper lens ramp", new Vector3(18f, 3.7f, 12f),
                        new Vector3(25f, 1f, 9f), new Vector3(0f, 0f, -14f),
                        RegionPackMaterialSlot.Accent),
                    Piece("East beacon causeway", new Vector3(25f, 0.35f, 0f),
                        new Vector3(28f, 0.7f, 7f), Vector3.zero, RegionPackMaterialSlot.Dark),
                    Piece("Fallen walking lens", PrimitiveType.Cube, new Vector3(-48f, 5.8f, -42f),
                        new Vector3(1f, 12f, 6f), new Vector3(9f, 24f, -17f),
                        RegionPackMaterialSlot.Accent, false, false)
                },
                landmarkStyle = RegionPackLandmarkStyle.LumenBeacon,
                landmarkName = "The Walking Beacon",
                landmarkSubtitle = "Five lenses keep one westward bearing",
                landmarkLocalPosition = new Vector3(34f, 9f, 30f),
                secretId = "secret.lumenwaste.lens_shadow",
                secretName = "Lens-Shadow Cache",
                secretInscription = "A routewright buried chimesalt where the noon lens cannot cast light.",
                secretLocalPosition = new Vector3(-52f, 0.45f, 48f),
                secretResource = ResourceKind.Chimesalt,
                secretResourceAmount = 3,
                encounterId = "encounter.cinderwold.relay_breakers",
                encounterPersistenceId = "encounter.region.lumenwaste.lens_walk",
                encounterLocalPosition = new Vector3(-28f, 2.4f, -31f),
                encounterFootprint = RegionPackEncounterFootprint.LensWalk,
                resources = new[]
                {
                    Resource(ResourceKind.Chimesalt, -58f, 0.75f, -5f, new Color(0.94f, 0.85f, 0.3f)),
                    Resource(ResourceKind.Chimesalt, 52f, 0.75f, -42f, new Color(0.94f, 0.85f, 0.3f)),
                    Resource(ResourceKind.Stone, -58f, 0.75f, -48f, new Color(0.58f, 0.53f, 0.31f)),
                    Resource(ResourceKind.Stone, 53f, 0.75f, 50f, new Color(0.58f, 0.53f, 0.31f)),
                    Resource(ResourceKind.Fiber, 5f, 0.75f, 54f, new Color(0.73f, 0.76f, 0.35f)),
                    Resource(ResourceKind.Fiber, -4f, 0.75f, -54f, new Color(0.73f, 0.76f, 0.35f))
                },
                threadway = Threadway(
                    "threadway.lumenwaste",
                    "Lumenwaste Walking Line",
                    "Lumenwaste Reach",
                    "A shaded route fork aligned to the five walking lenses.",
                    center + new Vector3(50f, 0f, 0f),
                    center + new Vector3(46f, 0.05f, 0f),
                    2,
                    new Color(0.96f, 0.88f, 0.28f))
            };
        }

        private static RegionPackDefinition CreateBrassroot()
        {
            var center = new Vector3(360f, 0f, 110f);
            return new RegionPackDefinition
            {
                id = "region.brassroot",
                displayName = "Brassroot Deeps",
                status = RegionPackStatus.Playable,
                biome = "subterranean root foundry",
                silhouette = "hanging brass roots over stepped cavern mouths",
                traversalNote = "Foundry terraces climb east through two cast-metal ramps and marked rootfaces.",
                discoveryText = "The foundry roof is gone, but its brass roots still hang over warm terraced stone.",
                authoringBrief = "Playable bounded root-foundry terraces with an ore ecology.",
                mapSummary = "Foundry terraces, brassroot crucible, and a quenched cache.",
                center = center,
                size = new Vector2(140f, 140f),
                activationRadius = 112f,
                mapColor = new Color(0.86f, 0.32f, 0.1f),
                groundColor = new Color(0.24f, 0.14f, 0.09f),
                terrainColor = new Color(0.54f, 0.29f, 0.13f),
                accentColor = new Color(0.9f, 0.58f, 0.2f),
                darkColor = new Color(0.1f, 0.07f, 0.06f),
                ambientColor = new Color(0.42f, 0.27f, 0.18f),
                fogColor = new Color(0.42f, 0.3f, 0.23f),
                sunColor = new Color(1f, 0.62f, 0.28f),
                skyColor = new Color(0.48f, 0.35f, 0.28f),
                fogDensity = 0.0042f,
                connectorStart = new Vector3(238f, 0f, 110f),
                connectorEnd = new Vector3(292f, 0f, 110f),
                connectorWidth = 14f,
                terrainPieces = new[]
                {
                    Piece("South smelter shelf", new Vector3(-28f, 1.1f, -31f),
                        new Vector3(34f, 2.2f, 24f), Vector3.zero, RegionPackMaterialSlot.Ground),
                    Piece("Casting terrace", new Vector3(3f, 2.7f, -8f),
                        new Vector3(34f, 5.4f, 27f), Vector3.zero, RegionPackMaterialSlot.Terrain),
                    Piece("High root dais", new Vector3(35f, 4.9f, 27f),
                        new Vector3(28f, 9.8f, 28f), Vector3.zero, RegionPackMaterialSlot.Ground, true),
                    Piece("North ore gallery", new Vector3(-32f, 3.3f, 37f),
                        new Vector3(30f, 6.6f, 20f), new Vector3(0f, 8f, 0f),
                        RegionPackMaterialSlot.Dark, true),
                    Piece("Lower brass ramp", new Vector3(-12f, 1.9f, -20f),
                        new Vector3(24f, 1f, 9f), new Vector3(0f, 0f, -12f),
                        RegionPackMaterialSlot.Accent),
                    Piece("Upper brass ramp", new Vector3(20f, 3.9f, 10f),
                        new Vector3(25f, 1f, 9f), new Vector3(0f, 0f, -15f),
                        RegionPackMaterialSlot.Accent),
                    Piece("West foundry causeway", new Vector3(-23f, 0.35f, 0f),
                        new Vector3(30f, 0.7f, 7f), Vector3.zero, RegionPackMaterialSlot.Dark),
                    Piece("Hanging outer root", PrimitiveType.Cylinder, new Vector3(52f, 6f, -38f),
                        new Vector3(1.2f, 7f, 1.2f), new Vector3(18f, -24f, 22f),
                        RegionPackMaterialSlot.Terrain, false, false)
                },
                landmarkStyle = RegionPackLandmarkStyle.BrassrootCrucible,
                landmarkName = "Brassroot Crucible",
                landmarkSubtitle = "The old furnace grew downward",
                landmarkLocalPosition = new Vector3(35f, 9.8f, 27f),
                secretId = "secret.brassroot.quenched_ledger",
                secretName = "Quenched Ledger",
                secretInscription = "The last foundry tally hides ore beneath a root that never cooled.",
                secretLocalPosition = new Vector3(52f, 0.45f, -49f),
                secretResource = ResourceKind.Ore,
                secretResourceAmount = 3,
                encounterId = "encounter.cinderwold.ashjaw",
                encounterPersistenceId = "encounter.region.brassroot.foundry_terrace",
                encounterLocalPosition = new Vector3(-28f, 2.2f, -31f),
                encounterFootprint = RegionPackEncounterFootprint.FoundryTerrace,
                resources = new[]
                {
                    Resource(ResourceKind.Ore, -55f, 0.75f, -47f, new Color(0.62f, 0.48f, 0.37f)),
                    Resource(ResourceKind.Ore, 52f, 0.75f, 49f, new Color(0.62f, 0.48f, 0.37f)),
                    Resource(ResourceKind.Stone, -54f, 0.75f, 11f, new Color(0.47f, 0.39f, 0.34f)),
                    Resource(ResourceKind.Stone, 51f, 0.75f, -11f, new Color(0.47f, 0.39f, 0.34f)),
                    Resource(ResourceKind.Resin, -8f, 0.75f, 53f, new Color(0.86f, 0.49f, 0.16f)),
                    Resource(ResourceKind.Resin, 8f, 0.75f, -54f, new Color(0.86f, 0.49f, 0.16f))
                },
                threadway = Threadway(
                    "threadway.brassroot",
                    "Brassroot Intake",
                    "Brassroot Deeps",
                    "A cool intake fork at the western lip of the ruined foundry.",
                    center + new Vector3(-50f, 0f, 0f),
                    center + new Vector3(-46f, 0.05f, 0f),
                    3,
                    new Color(0.92f, 0.55f, 0.2f))
            };
        }

        private static RegionPackDefinition CreateCloudbarrow()
        {
            var center = new Vector3(-230f, 0f, 390f);
            return new RegionPackDefinition
            {
                id = "region.cloudbarrow",
                displayName = "Cloudbarrow Expanse",
                status = RegionPackStatus.Playable,
                biome = "high grass burial shelves",
                silhouette = "long barrows cut by cloud-height causeways",
                traversalNote = "A southern processional way branches across three wind shelves and two rising causeways.",
                discoveryText = "Grass leans north across the burial shelves. Every cairn leaves a lane for the clouds.",
                authoringBrief = "Playable bounded wind-shelf route with sightline-led barrow navigation.",
                mapSummary = "Burial shelves, cloud cairn, and a lee-side cache.",
                center = center,
                size = new Vector2(140f, 140f),
                activationRadius = 112f,
                mapColor = new Color(0.54f, 0.88f, 0.56f),
                groundColor = new Color(0.24f, 0.32f, 0.19f),
                terrainColor = new Color(0.48f, 0.63f, 0.38f),
                accentColor = new Color(0.79f, 0.91f, 0.68f),
                darkColor = new Color(0.1f, 0.16f, 0.12f),
                ambientColor = new Color(0.4f, 0.49f, 0.35f),
                fogColor = new Color(0.55f, 0.64f, 0.52f),
                sunColor = new Color(0.91f, 0.93f, 0.69f),
                skyColor = new Color(0.55f, 0.72f, 0.72f),
                fogDensity = 0.0035f,
                connectorStart = new Vector3(-230f, 0f, 288f),
                connectorEnd = new Vector3(-230f, 0f, 322f),
                connectorWidth = 14f,
                terrainPieces = new[]
                {
                    Piece("West burial shelf", new Vector3(-37f, 1.4f, -8f),
                        new Vector3(30f, 2.8f, 34f), Vector3.zero, RegionPackMaterialSlot.Ground),
                    Piece("East grass shelf", new Vector3(31f, 3.2f, 9f),
                        new Vector3(31f, 6.4f, 31f), Vector3.zero, RegionPackMaterialSlot.Terrain, true),
                    Piece("North cloud shelf", new Vector3(-5f, 5.1f, 38f),
                        new Vector3(42f, 10.2f, 24f), Vector3.zero, RegionPackMaterialSlot.Ground, true),
                    Piece("South wind berm", new Vector3(40f, 1.2f, -37f),
                        new Vector3(26f, 2.4f, 20f), new Vector3(0f, -9f, 0f),
                        RegionPackMaterialSlot.Dark),
                    Piece("West barrow ramp", new Vector3(-20f, 2.1f, -1f),
                        new Vector3(24f, 1f, 9f), new Vector3(0f, 0f, -12f),
                        RegionPackMaterialSlot.Accent),
                    Piece("North processional rise", new Vector3(7f, 4.2f, 22f),
                        new Vector3(30f, 1f, 8f), new Vector3(0f, 0f, -14f),
                        RegionPackMaterialSlot.Accent),
                    Piece("South processional way", new Vector3(0f, 0.35f, -25f),
                        new Vector3(8f, 0.7f, 30f), Vector3.zero, RegionPackMaterialSlot.Dark),
                    Piece("Cloudbarrow witness stone", PrimitiveType.Cube, new Vector3(-52f, 6.5f, 44f),
                        new Vector3(2f, 13f, 5f), new Vector3(7f, -18f, -12f),
                        RegionPackMaterialSlot.Terrain, false, false)
                },
                landmarkStyle = RegionPackLandmarkStyle.CloudbarrowCairn,
                landmarkName = "Cloudbarrow Cairn",
                landmarkSubtitle = "The capstone leaves room for weather",
                landmarkLocalPosition = new Vector3(-5f, 10.2f, 38f),
                secretId = "secret.cloudbarrow.lee_vault",
                secretName = "Lee-Side Vault",
                secretInscription = "A grasskeeper sealed fiber where the cairn's wind cannot reach.",
                secretLocalPosition = new Vector3(52f, 0.45f, 48f),
                secretResource = ResourceKind.Fiber,
                secretResourceAmount = 3,
                encounterId = "encounter.alderwake.ridge",
                encounterPersistenceId = "encounter.region.cloudbarrow.barrow_gate",
                encounterLocalPosition = new Vector3(-37f, 2.8f, -8f),
                encounterFootprint = RegionPackEncounterFootprint.BarrowGate,
                resources = new[]
                {
                    Resource(ResourceKind.Herbs, -55f, 0.75f, -48f, new Color(0.54f, 0.76f, 0.42f)),
                    Resource(ResourceKind.Herbs, 54f, 0.75f, -46f, new Color(0.54f, 0.76f, 0.42f)),
                    Resource(ResourceKind.Fiber, -51f, 0.75f, 51f, new Color(0.72f, 0.82f, 0.55f)),
                    Resource(ResourceKind.Fiber, 52f, 0.75f, 51f, new Color(0.72f, 0.82f, 0.55f)),
                    Resource(ResourceKind.Stone, -2f, 0.75f, 55f, new Color(0.52f, 0.58f, 0.48f)),
                    Resource(ResourceKind.Stone, 11f, 0.75f, -53f, new Color(0.52f, 0.58f, 0.48f))
                },
                threadway = Threadway(
                    "threadway.cloudbarrow",
                    "Cloudbarrow Processional",
                    "Cloudbarrow Expanse",
                    "A grass-bound processional fork below the first burial shelf.",
                    center + new Vector3(0f, 0f, -50f),
                    center + new Vector3(0f, 0.05f, -46f),
                    2,
                    new Color(0.68f, 0.9f, 0.58f))
            };
        }

        private static RegionPackDefinition CreateTideglass()
        {
            var center = new Vector3(0f, 0f, -410f);
            return new RegionPackDefinition
            {
                id = "region.tideglass",
                displayName = "Tideglass Strand",
                status = RegionPackStatus.Playable,
                biome = "black-sand tidal mirrors",
                silhouette = "knife-thin sea stacks reflected in shallow glasswater",
                traversalNote = "A dry northern causeway reaches two island ramps and a climbable outer tidebreak.",
                discoveryText = "Black sand holds a motionless tide. Thin stacks double themselves in the glasswater.",
                authoringBrief = "Playable bounded strand using static shallow-water volumes and raised crossings.",
                mapSummary = "Tidal mirrors, needle reef, and a stranded survey cache.",
                center = center,
                size = new Vector2(140f, 140f),
                activationRadius = 112f,
                mapColor = new Color(0.12f, 0.28f, 0.58f),
                groundColor = new Color(0.08f, 0.09f, 0.13f),
                terrainColor = new Color(0.2f, 0.3f, 0.4f),
                accentColor = new Color(0.34f, 0.76f, 0.88f),
                darkColor = new Color(0.035f, 0.045f, 0.07f),
                ambientColor = new Color(0.18f, 0.28f, 0.4f),
                fogColor = new Color(0.24f, 0.38f, 0.5f),
                sunColor = new Color(0.7f, 0.88f, 0.96f),
                skyColor = new Color(0.23f, 0.45f, 0.64f),
                fogDensity = 0.0048f,
                connectorStart = new Vector3(-21f, 0f, -288f),
                connectorEnd = new Vector3(-21f, 0f, -342f),
                connectorWidth = 14f,
                terrainPieces = new[]
                {
                    Piece("North strand causeway", new Vector3(-21f, 0.35f, 25f),
                        new Vector3(8f, 0.7f, 30f), Vector3.zero, RegionPackMaterialSlot.Dark),
                    Piece("West black-sand island", new Vector3(-40f, 1.3f, 4f),
                        new Vector3(25f, 2.6f, 25f), Vector3.zero, RegionPackMaterialSlot.Ground),
                    Piece("East mirror shelf", new Vector3(32f, 2.7f, -9f),
                        new Vector3(30f, 5.4f, 28f), Vector3.zero, RegionPackMaterialSlot.Terrain, true),
                    Piece("South needle stack", new Vector3(-6f, 4.6f, -41f),
                        new Vector3(25f, 9.2f, 20f), Vector3.zero, RegionPackMaterialSlot.Dark, true),
                    Piece("West tide ramp", new Vector3(-20f, 1.9f, -3f),
                        new Vector3(25f, 1f, 9f), new Vector3(0f, 0f, -11f),
                        RegionPackMaterialSlot.Accent),
                    Piece("East tide ramp", new Vector3(17f, 3.9f, -22f),
                        new Vector3(25f, 1f, 9f), new Vector3(0f, 0f, -14f),
                        RegionPackMaterialSlot.Accent),
                    Piece("Outer tidebreak", new Vector3(49f, 3.2f, 34f),
                        new Vector3(12f, 6.4f, 27f), new Vector3(0f, -7f, 0f),
                        RegionPackMaterialSlot.Terrain, true),
                    Piece("Leaning sea needle", PrimitiveType.Cube, new Vector3(-53f, 7f, -34f),
                        new Vector3(1.2f, 14f, 4f), new Vector3(-8f, 19f, 17f),
                        RegionPackMaterialSlot.Accent, false, false)
                },
                hasWater = true,
                waterLocalPosition = new Vector3(2f, 0.04f, -2f),
                waterSize = new Vector3(92f, 2.2f, 52f),
                waterColor = new Color(0.06f, 0.25f, 0.36f, 0.72f),
                landmarkStyle = RegionPackLandmarkStyle.TideglassNeedles,
                landmarkName = "Tideglass Needle Reef",
                landmarkSubtitle = "Six stacks hold the same horizon",
                landmarkLocalPosition = new Vector3(49f, 6.4f, 34f),
                secretId = "secret.tideglass.stranded_survey",
                secretName = "Stranded Survey",
                secretInscription = "A tide-reader left chimesalt above the waterline for the route's next witness.",
                secretLocalPosition = new Vector3(-6f, 9.65f, -41f),
                secretResource = ResourceKind.Chimesalt,
                secretResourceAmount = 3,
                encounterId = "encounter.morrowfen.drowned_choir",
                encounterPersistenceId = "encounter.region.tideglass.tidebreak_spines",
                encounterLocalPosition = new Vector3(-40f, 2.6f, 4f),
                encounterFootprint = RegionPackEncounterFootprint.TidebreakSpines,
                resources = new[]
                {
                    Resource(ResourceKind.Chimesalt, -40f, 3.05f, 4f, new Color(0.38f, 0.8f, 0.9f)),
                    Resource(ResourceKind.Chimesalt, 49f, 6.85f, 34f, new Color(0.38f, 0.8f, 0.9f)),
                    Resource(ResourceKind.Stone, 32f, 5.85f, -9f, new Color(0.34f, 0.45f, 0.55f)),
                    Resource(ResourceKind.Stone, -55f, 0.75f, 45f, new Color(0.34f, 0.45f, 0.55f)),
                    Resource(ResourceKind.Ore, -18f, 9.65f, -43f, new Color(0.3f, 0.56f, 0.7f)),
                    Resource(ResourceKind.Ore, 55f, 0.75f, -48f, new Color(0.3f, 0.56f, 0.7f))
                },
                threadway = Threadway(
                    "threadway.tideglass",
                    "Tideglass Drymark",
                    "Tideglass Strand",
                    "A drymark fork above the northern reach of the glasswater.",
                    center + new Vector3(-21f, 0f, 50f),
                    center + new Vector3(-21f, 0.05f, 46f),
                    2,
                    new Color(0.32f, 0.74f, 0.9f))
            };
        }

        private static RegionPackDefinition CreateNightloom()
        {
            var center = new Vector3(245f, 0f, 390f);
            return new RegionPackDefinition
            {
                id = "region.nightloom",
                displayName = "Nightloom Hollow",
                status = RegionPackStatus.Playable,
                biome = "bioluminescent ravine forest",
                silhouette = "woven canopy bridges above a dark central cleft",
                traversalNote = "Two sloped canopy bridges braid west and east ravine shelves into a high northern loom.",
                discoveryText = "The ravine keeps its own night. Woven roots carry violet light between the shelves.",
                authoringBrief = "Playable bounded canopy-and-ravine route with material glow and no added light field.",
                mapSummary = "Ravine shelves, nightloom lattice, and a hidden glow cache.",
                center = center,
                size = new Vector2(140f, 140f),
                activationRadius = 112f,
                mapColor = new Color(0.56f, 0.22f, 0.69f),
                groundColor = new Color(0.11f, 0.1f, 0.16f),
                terrainColor = new Color(0.24f, 0.28f, 0.31f),
                accentColor = new Color(0.66f, 0.38f, 0.88f),
                darkColor = new Color(0.045f, 0.035f, 0.08f),
                ambientColor = new Color(0.26f, 0.22f, 0.36f),
                fogColor = new Color(0.25f, 0.24f, 0.39f),
                sunColor = new Color(0.72f, 0.62f, 0.94f),
                skyColor = new Color(0.24f, 0.25f, 0.45f),
                fogDensity = 0.0052f,
                connectorStart = new Vector3(225f, 0f, 288f),
                connectorEnd = new Vector3(225f, 0f, 322f),
                connectorWidth = 14f,
                terrainPieces = new[]
                {
                    Piece("South woven approach", new Vector3(-20f, 0.35f, -25f),
                        new Vector3(8f, 0.7f, 30f), Vector3.zero, RegionPackMaterialSlot.Dark),
                    Piece("West ravine shelf", new Vector3(-39f, 1.5f, -4f),
                        new Vector3(27f, 3f, 40f), Vector3.zero, RegionPackMaterialSlot.Ground),
                    Piece("East ravine shelf", new Vector3(35f, 3.3f, 8f),
                        new Vector3(30f, 6.6f, 38f), Vector3.zero, RegionPackMaterialSlot.Terrain, true),
                    Piece("North canopy shelf", new Vector3(0f, 5.2f, 39f),
                        new Vector3(43f, 10.4f, 20f), Vector3.zero, RegionPackMaterialSlot.Ground, true),
                    Piece("Lower canopy bridge", new Vector3(-8f, 2.4f, -8f),
                        new Vector3(38f, 1f, 7f), new Vector3(0f, 20f, -10f),
                        RegionPackMaterialSlot.Accent),
                    Piece("Upper canopy bridge", new Vector3(9f, 4.4f, 21f),
                        new Vector3(35f, 1f, 7f), new Vector3(0f, -18f, -13f),
                        RegionPackMaterialSlot.Accent),
                    Piece("East loom root", new Vector3(53f, 4f, -34f),
                        new Vector3(5f, 8f, 18f), new Vector3(0f, -8f, 0f),
                        RegionPackMaterialSlot.Dark, true),
                    Piece("Woven outer canopy", PrimitiveType.Sphere, new Vector3(-50f, 7.5f, 42f),
                        new Vector3(14f, 2f, 10f), new Vector3(0f, 12f, 0f),
                        RegionPackMaterialSlot.Accent, false, false)
                },
                landmarkStyle = RegionPackLandmarkStyle.NightloomLattice,
                landmarkName = "Nightloom Lattice",
                landmarkSubtitle = "The hollow braids light without flame",
                landmarkLocalPosition = new Vector3(0f, 10.4f, 39f),
                secretId = "secret.nightloom.glow_knot",
                secretName = "Glow-Knot Cache",
                secretInscription = "A canopy tender knotted resin into the one strand the hollow cannot illuminate.",
                secretLocalPosition = new Vector3(48f, 7.05f, 15f),
                secretResource = ResourceKind.Resin,
                secretResourceAmount = 3,
                encounterId = "encounter.morrowfen.fen_tithe",
                encounterPersistenceId = "encounter.region.nightloom.loom_ambush",
                encounterLocalPosition = new Vector3(-39f, 3f, -4f),
                encounterFootprint = RegionPackEncounterFootprint.LoomAmbush,
                resources = new[]
                {
                    Resource(ResourceKind.Herbs, -53f, 0.75f, -48f, new Color(0.48f, 0.68f, 0.5f)),
                    Resource(ResourceKind.Herbs, 54f, 0.75f, -49f, new Color(0.48f, 0.68f, 0.5f)),
                    Resource(ResourceKind.Resin, -51f, 0.75f, 51f, new Color(0.66f, 0.39f, 0.72f)),
                    Resource(ResourceKind.Resin, 30f, 7.05f, 18f, new Color(0.66f, 0.39f, 0.72f)),
                    Resource(ResourceKind.Fiber, -8f, 0.75f, 55f, new Color(0.54f, 0.54f, 0.76f)),
                    Resource(ResourceKind.Fiber, 8f, 0.75f, -54f, new Color(0.54f, 0.54f, 0.76f))
                },
                threadway = Threadway(
                    "threadway.nightloom",
                    "Nightloom Low Weave",
                    "Nightloom Hollow",
                    "A violet route fork beneath the hollow's southern canopy.",
                    center + new Vector3(-20f, 0f, -50f),
                    center + new Vector3(-20f, 0.05f, -46f),
                    3,
                    new Color(0.67f, 0.41f, 0.9f))
            };
        }

        private static RegionPackTerrainPiece Piece(
            string id,
            Vector3 localPosition,
            Vector3 scale,
            Vector3 eulerAngles,
            RegionPackMaterialSlot materialSlot,
            bool climbable = false)
        {
            return Piece(
                id,
                PrimitiveType.Cube,
                localPosition,
                scale,
                eulerAngles,
                materialSlot,
                true,
                climbable);
        }

        private static RegionPackTerrainPiece Piece(
            string id,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 scale,
            Vector3 eulerAngles,
            RegionPackMaterialSlot materialSlot,
            bool solid,
            bool climbable)
        {
            return new RegionPackTerrainPiece
            {
                id = id,
                primitive = primitive,
                localPosition = localPosition,
                scale = scale,
                eulerAngles = eulerAngles,
                materialSlot = materialSlot,
                solid = solid,
                climbable = climbable
            };
        }

        private static RegionPackResourcePlacement Resource(
            ResourceKind kind,
            float x,
            float y,
            float z,
            Color color)
        {
            return new RegionPackResourcePlacement
            {
                kind = kind,
                localPosition = new Vector3(x, y, z),
                color = color
            };
        }

        private static ThreadwayDefinition Threadway(
            string id,
            string title,
            string region,
            string description,
            Vector3 position,
            Vector3 arrival,
            int cost,
            Color accent)
        {
            return new ThreadwayDefinition
            {
                id = id,
                title = title,
                region = region,
                description = description,
                position = position,
                arrivalPosition = arrival,
                activationChimesaltCost = cost,
                accentColor = accent
            };
        }
    }

    internal sealed class RegionPackRuntime
    {
        public RegionPackDefinition Definition { get; }
        public GameObject StaticRoot { get; }
        public GameObject DetailRoot { get; }
        public bool ThreadwayAttached { get; set; }

        public RegionPackRuntime(
            RegionPackDefinition definition,
            GameObject staticRoot,
            GameObject detailRoot)
        {
            Definition = definition;
            StaticRoot = staticRoot;
            DetailRoot = detailRoot;
        }
    }

    [DefaultExecutionOrder(-10000)]
    public sealed class RegionPackDirector : MonoBehaviour
    {
        private readonly List<RegionPackRuntime> runtimes = new List<RegionPackRuntime>();
        private TetherfallGame game;
        private RegionPackDefinition frameRegion;
        private string activeRegionId;
        private float nextActivationRefreshAt;

        internal void Initialize(TetherfallGame owner, IEnumerable<RegionPackRuntime> regionRuntimes)
        {
            game = owner;
            runtimes.Clear();
            if (regionRuntimes != null)
            {
                runtimes.AddRange(regionRuntimes);
            }

            nextActivationRefreshAt = 0f;
        }

        private void Update()
        {
            if (game == null || game.Player == null)
            {
                frameRegion = null;
                return;
            }

            var playerPosition = game.PlayerTransform.position;
            frameRegion = TetherfallRegionPacks.FindPlayable(playerPosition);
            if (frameRegion != null)
            {
                game.PrimeLegacyRegionForPack(playerPosition);
            }

            var requiresImmediateActivation = frameRegion != null && !IsDetailActive(frameRegion.id);
            if (!requiresImmediateActivation && Time.unscaledTime < nextActivationRefreshAt)
            {
                return;
            }

            nextActivationRefreshAt = Time.unscaledTime + TetherfallRegionPacks.ActivationRefreshSeconds;
            RefreshActivation(playerPosition);
        }

        private void LateUpdate()
        {
            if (game == null)
            {
                return;
            }

            if (frameRegion == null)
            {
                activeRegionId = null;
                return;
            }

            if (!string.Equals(activeRegionId, frameRegion.id, StringComparison.Ordinal))
            {
                activeRegionId = frameRegion.id;
                game.EnterRegionPack(frameRegion);
            }
            else
            {
                game.HoldRegionPackName(frameRegion.displayName);
            }
        }

        private void RefreshActivation(Vector3 playerPosition)
        {
            RegionPackRuntime nearest = null;
            RegionPackRuntime secondNearest = null;
            var nearestScore = float.PositiveInfinity;
            var secondNearestScore = float.PositiveInfinity;
            foreach (var runtime in runtimes)
            {
                if (runtime == null || runtime.Definition == null || runtime.DetailRoot == null)
                {
                    continue;
                }

                AttachThreadway(runtime);
                var delta = playerPosition - runtime.Definition.center;
                delta.y = 0f;
                var inActivationRadius =
                    delta.sqrMagnitude <= runtime.Definition.activationRadius * runtime.Definition.activationRadius;
                var shouldBeActive =
                    inActivationRadius ||
                    runtime.Definition.Contains(playerPosition, TetherfallRegionPacks.ActivationMargin);
                if (!shouldBeActive)
                {
                    continue;
                }

                var score = delta.sqrMagnitude;
                if (runtime.Definition.Contains(playerPosition))
                {
                    score -= 1000000f;
                }

                if (score < nearestScore)
                {
                    secondNearest = nearest;
                    secondNearestScore = nearestScore;
                    nearest = runtime;
                    nearestScore = score;
                }
                else if (score < secondNearestScore)
                {
                    secondNearest = runtime;
                    secondNearestScore = score;
                }
            }

            foreach (var runtime in runtimes)
            {
                if (runtime == null || runtime.DetailRoot == null)
                {
                    continue;
                }

                var shouldBeActive =
                    ReferenceEquals(runtime, nearest) ||
                    (TetherfallRegionPacks.MaxSimultaneouslyActivePacks > 1 &&
                     ReferenceEquals(runtime, secondNearest));
                if (runtime.DetailRoot.activeSelf != shouldBeActive)
                {
                    runtime.DetailRoot.SetActive(shouldBeActive);
                }
            }
        }

        private void AttachThreadway(RegionPackRuntime runtime)
        {
            if (runtime.ThreadwayAttached ||
                runtime.Definition.threadway == null ||
                runtime.DetailRoot == null)
            {
                return;
            }

            var anchor = game.GetRegionPackThreadway(runtime.Definition.threadway.id);
            if (anchor == null)
            {
                return;
            }

            anchor.transform.SetParent(runtime.DetailRoot.transform, true);
            runtime.ThreadwayAttached = true;
        }

        private bool IsDetailActive(string regionId)
        {
            foreach (var runtime in runtimes)
            {
                if (runtime != null &&
                    runtime.Definition != null &&
                    string.Equals(runtime.Definition.id, regionId, StringComparison.Ordinal))
                {
                    return runtime.DetailRoot != null && runtime.DetailRoot.activeSelf;
                }
            }

            return false;
        }
    }

    public sealed class RegionPackSecret : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private RegionPackDefinition definition;
        private Renderer shellRenderer;
        private Renderer crestRenderer;
        private bool recovered;

        public string InteractionPrompt => recovered
            ? $"{definition.secretName} recovered"
            : $"Recover {definition.secretName}";

        public static RegionPackSecret Create(TetherfallGame game, RegionPackDefinition definition)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = definition.secretName;
            root.transform.position = definition.center + definition.secretLocalPosition;
            root.transform.localScale = new Vector3(1.05f, 0.78f, 0.86f);
            var secret = root.AddComponent<RegionPackSecret>();
            secret.game = game;
            secret.definition = definition;
            secret.shellRenderer = root.GetComponent<Renderer>();
            secret.shellRenderer.sharedMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                Color.Lerp(definition.darkColor, definition.accentColor, 0.18f),
                0.12f,
                0.38f);

            var crest = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crest.name = "Secret Threadglass crest";
            crest.transform.SetParent(root.transform, false);
            crest.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            crest.transform.localScale = new Vector3(0.32f, 0.12f, 0.32f);
            secret.crestRenderer = crest.GetComponent<Renderer>();
            secret.crestRenderer.sharedMaterial = RuntimeEffectMaterialCache.GetGlow(
                game,
                definition.accentColor,
                1.15f,
                0.62f);
            UnityEngine.Object.Destroy(crest.GetComponent<Collider>());
            game.RegisterInteractable(secret);
            secret.RefreshRecoveredState();
            return secret;
        }

        private void OnEnable()
        {
            RefreshRecoveredState();
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null && !recovered;
        }

        public void Interact(TetherfallGame owner)
        {
            if (owner == null ||
                recovered ||
                definition == null ||
                !owner.TryRecoverRegionPackSecret(definition))
            {
                return;
            }

            recovered = true;
            RefreshVisuals();
        }

        private void RefreshRecoveredState()
        {
            if (game == null || definition == null)
            {
                return;
            }

            recovered = game.IsRegionPackSecretRecovered(definition.secretId);
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (shellRenderer != null)
            {
                shellRenderer.enabled = true;
                shellRenderer.sharedMaterial = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    recovered
                        ? Color.Lerp(definition.darkColor, Color.gray, 0.24f)
                        : Color.Lerp(definition.darkColor, definition.accentColor, 0.18f),
                    0.12f,
                    0.38f);
            }

            if (crestRenderer != null)
            {
                crestRenderer.enabled = !recovered;
            }
        }
    }

    public sealed partial class TetherfallGame
    {
        internal void PrimeLegacyRegionForPack(Vector3 position)
        {
            CurrentRegion = TetherfallRegionPacks.ResolveLegacyRegionName(position);
        }

        internal void HoldRegionPackName(string displayName)
        {
            if (!string.IsNullOrEmpty(displayName))
            {
                CurrentRegion = displayName;
            }
        }

        internal void EnterRegionPack(RegionPackDefinition definition)
        {
            if (definition == null || definition.status != RegionPackStatus.Playable)
            {
                return;
            }

            CurrentRegion = definition.displayName;
            var firstDiscovery = discoveredRegions.Add(definition.displayName);
            PresentRegion(definition.displayName, false);
            Notify($"Entered {definition.displayName}.");
            if (firstDiscovery)
            {
                GainExperience(20);
            }
            else
            {
                RequestSave();
            }
        }

        internal ThreadwayAnchor GetRegionPackThreadway(string id)
        {
            return !string.IsNullOrEmpty(id) && threadways.TryGetValue(id, out var anchor)
                ? anchor
                : null;
        }

        internal bool IsRegionPackSecretRecovered(string id)
        {
            return !string.IsNullOrEmpty(id) && recoveredFieldCacheIds.Contains(id);
        }

        internal bool TryRecoverRegionPackSecret(RegionPackDefinition definition)
        {
            if (definition == null ||
                string.IsNullOrEmpty(definition.secretId) ||
                !recoveredFieldCacheIds.Add(definition.secretId))
            {
                return false;
            }

            AddResource(definition.secretResource, Mathf.Max(1, definition.secretResourceAmount));
            Notify(definition.secretInscription, 6f);
            RequestSave();
            return true;
        }
    }
}
