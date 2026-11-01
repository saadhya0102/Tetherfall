using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Tetherfall
{
    public enum RegionalAnnexLandmarkStyle
    {
        CanopyCompass,
        ReedwakeOrrery,
        AshbellGantry,
        RainDial,
        SplitIceSextant,
        DrownedLanternCrown,
        FacetHarp,
        WindYoke,
        DawnLens,
        PouringRoot,
        SkyCairn,
        TidalComb,
        MoonLattice
    }

    [Serializable]
    public sealed class RegionalAnnexTerrainPiece
    {
        public string id;
        public string displayName;
        public PrimitiveType primitive = PrimitiveType.Cube;
        public Vector3 localPosition;
        public Vector3 scale = Vector3.one;
        public Vector3 eulerAngles;
        public RegionPackMaterialSlot materialSlot;
        public bool solid = true;
        public bool climbable;
        public bool isRamp;
    }

    [Serializable]
    public sealed class RegionalAnnexResourcePlacement
    {
        public string persistenceId;
        public ResourceKind kind;
        public Vector3 localPosition;
        public Color color;
    }

    [Serializable]
    public sealed class RegionalAnnexDefinition
    {
        public string id;
        public string parentRegionId;
        public string parentDisplayName;
        public string frontierName;
        public string biomeBrief;
        public string routeBrief;
        public Rect parentBounds;
        public Vector3 center;
        public Vector2 size;
        public Color mapColor;
        public Color groundColor;
        public Color terrainColor;
        public Color accentColor;
        public Color darkColor;
        public Color atmosphereColor;
        public Vector3[] connectorPoints = Array.Empty<Vector3>();
        public float connectorWidth;
        public RegionalAnnexTerrainPiece[] terrainPieces = Array.Empty<RegionalAnnexTerrainPiece>();
        public RegionalAnnexResourcePlacement[] resources = Array.Empty<RegionalAnnexResourcePlacement>();
        public RegionalAnnexLandmarkStyle landmarkStyle;
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
        public ThreadwayDefinition threadway;
        public int routeDressingPieceCount;
        public bool hasProceduralGroundVariation;
        public bool hasBoundedAtmosphereField;

        public Rect WorldBounds => new Rect(
            center.x - size.x * 0.5f,
            center.z - size.y * 0.5f,
            size.x,
            size.y);

        public float Area => size.x * size.y;
        public float ParentArea => parentBounds.width * parentBounds.height;

        public bool Contains(Vector3 position, float margin = 0f)
        {
            var halfWidth = size.x * 0.5f + Mathf.Max(0f, margin);
            var halfDepth = size.y * 0.5f + Mathf.Max(0f, margin);
            return Mathf.Abs(position.x - center.x) <= halfWidth &&
                   Mathf.Abs(position.z - center.z) <= halfDepth;
        }
    }

    public readonly struct RegionalAnnexBudgetEstimate
    {
        public readonly int renderers;
        public readonly int colliders;
        public readonly int lights;
        public readonly int activeUpdateDrivers;

        public RegionalAnnexBudgetEstimate(
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

    public static class TetherfallRegionalAnnexes
    {
        public const int ParentRegionCount = 13;
        public const int AnnexCount = 13;
        public const int MinimumTerrainPieces = 8;
        public const int MinimumResourceNodes = 6;
        public const int MaxActiveStaticRoots = 4;
        public const int MaxActiveDetailRoots = 2;
        public const int MaxRenderersPerAnnex = 180;
        public const int MaxCollidersPerAnnex = 64;
        public const int MaxLightsPerAnnex = 2;
        public const int MaxUpdateDriversPerAnnex = 16;
        public const float StaticPrewakeDistance = 128f;
        public const float StaticHysteresisDistance = 52f;
        public const float DetailPrewakeDistance = 96f;
        public const float DetailHysteresisDistance = 48f;
        public const float CampPopulationWakeDistance = 78f;
        public const float CampPopulationSoftResetDistance = 140f;
        public const float ActivationRefreshSeconds = 0.25f;

        private const string WorldRootName = "Regional Frontier Annexes";
        private const int CampBaseRendererBudget = 7;
        private const int CampLooseColliderBudget = 2;
        private const int ThreadwayRendererBudget = 7;
        private const int ThreadwayUpdateDriverBudget = 6;
        private const int SecretRendererBudget = 2;
        private const int ArrivalRendererBudget = 2;
        private const int RouteDressingRendererBudget = 8;
        private const int AtmosphereRendererBudget = 1;
        private static readonly Color NavigationTeal = new Color(0.25f, 0.84f, 0.78f);
        private static readonly Color RouteworkCopper = new Color(0.76f, 0.43f, 0.2f);
        private static readonly List<RegionalAnnexDefinition> Definitions = CreateDefinitions();
        private static readonly Dictionary<string, RegionalAnnexRuntime> RuntimeByThreadwayId =
            new Dictionary<string, RegionalAnnexRuntime>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Material> GroundMaterials =
            new Dictionary<string, Material>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Texture2D> GroundTextures =
            new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private static RegionalAnnexDirector activeDirector;

        public static IReadOnlyList<RegionalAnnexDefinition> All => Definitions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeReferences()
        {
            RuntimeByThreadwayId.Clear();
            GroundMaterials.Clear();
            GroundTextures.Clear();
            activeDirector = null;
        }

        public static RegionalAnnexDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (var definition in Definitions)
            {
                if (definition != null && string.Equals(definition.id, id, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public static RegionalAnnexDefinition Find(Vector3 position)
        {
            foreach (var definition in Definitions)
            {
                if (definition != null && definition.Contains(position))
                {
                    return definition;
                }
            }

            return null;
        }

        public static string ResolveParentRegionName(Vector3 position)
        {
            foreach (var definition in Definitions)
            {
                if (definition != null &&
                    (definition.Contains(position) || ContainsConnector(definition, position)))
                {
                    return definition.parentDisplayName;
                }
            }

            return null;
        }

        public static void Build(TetherfallGame game)
        {
            if (game == null)
            {
                return;
            }

            InstallThreadways();
            if (game.transform.Find(WorldRootName) != null)
            {
                return;
            }

            RuntimeByThreadwayId.Clear();
            var worldRoot = new GameObject(WorldRootName);
            worldRoot.transform.SetParent(game.transform, false);
            var runtimes = new List<RegionalAnnexRuntime>(Definitions.Count);
            foreach (var definition in Definitions)
            {
                var runtime = BuildAnnex(game, worldRoot.transform, definition);
                runtimes.Add(runtime);
                if (definition.threadway != null)
                {
                    RuntimeByThreadwayId[definition.threadway.id] = runtime;
                }
            }

            var director = worldRoot.AddComponent<RegionalAnnexDirector>();
            director.Initialize(game, runtimes);
            activeDirector = director;
        }

        internal static void InstallThreadways()
        {
            if (!(ThreadwayCatalog.All is List<ThreadwayDefinition> mutableCatalog))
            {
                Debug.LogError(
                    "Tetherfall regional annexes could not install Threadway metadata because " +
                    "ThreadwayCatalog.All is not backed by a mutable list.");
                return;
            }

            foreach (var definition in Definitions)
            {
                var threadway = definition.threadway;
                if (threadway != null && ThreadwayCatalog.Get(threadway.id) == null)
                {
                    mutableCatalog.Add(threadway);
                }
            }
        }

        internal static bool AttachThreadwayShell(ThreadwayAnchor anchor)
        {
            if (anchor == null ||
                anchor.Definition == null ||
                !RuntimeByThreadwayId.TryGetValue(anchor.Definition.id, out var runtime) ||
                runtime == null ||
                runtime.StaticRoot == null)
            {
                return false;
            }

            anchor.transform.SetParent(runtime.StaticRoot.transform, true);
            runtime.ThreadwayAttached = true;
            return true;
        }

        internal static void PrepareArrival(Vector3 arrivalPosition)
        {
            activeDirector?.PrimeForArrival(arrivalPosition);
        }

        public static RegionalAnnexBudgetEstimate EstimateBudget(RegionalAnnexDefinition definition)
        {
            if (definition == null)
            {
                return new RegionalAnnexBudgetEstimate(0, 0, 0, 0);
            }

            var connectorPointCount = definition.connectorPoints != null
                ? definition.connectorPoints.Length
                : 0;
            var connectorSegmentCount = Mathf.Max(0, connectorPointCount - 1);
            var connectorRenderers = connectorSegmentCount * 3 + connectorPointCount;
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
            const int landmarkRenderers = 8;
            return new RegionalAnnexBudgetEstimate(
                connectorRenderers +
                ArrivalRendererBudget +
                1 +
                terrainCount +
                landmarkRenderers +
                resourceCount * 2 +
                SecretRendererBudget +
                ThreadwayRendererBudget +
                CampBaseRendererBudget +
                TetherfallCampPresentation.MaxRenderersPerCamp +
                RouteDressingRendererBudget +
                AtmosphereRendererBudget,
                connectorSegmentCount +
                1 +
                1 +
                solidTerrainCount +
                resourceCount +
                1 +
                1 +
                CampLooseColliderBudget +
                TetherfallCampPresentation.MaxCollidersPerCamp,
                2,
                resourceCount + ThreadwayUpdateDriverBudget + 3);
        }

        private static RegionalAnnexRuntime BuildAnnex(
            TetherfallGame game,
            Transform worldRoot,
            RegionalAnnexDefinition definition)
        {
            var regionRoot = new GameObject($"{definition.frontierName} Annex");
            regionRoot.transform.SetParent(worldRoot, false);
            regionRoot.transform.position = definition.center;

            var connectorRoot = new GameObject("Always-on connector");
            connectorRoot.transform.SetParent(regionRoot.transform, false);
            var arrivalRoot = new GameObject("Always-on arrival support");
            arrivalRoot.transform.SetParent(regionRoot.transform, false);
            var staticRoot = new GameObject("Distance-streamed terrain");
            staticRoot.transform.SetParent(regionRoot.transform, false);
            var detailRoot = new GameObject("Distance-streamed detail");
            detailRoot.transform.SetParent(regionRoot.transform, false);

            BuildConnector(game, definition, connectorRoot.transform);
            BuildArrivalSupport(game, definition, arrivalRoot.transform);
            BuildStaticTerrain(game, definition, staticRoot.transform);
            var camp = BuildDetail(game, definition, detailRoot.transform);

            staticRoot.SetActive(false);
            detailRoot.SetActive(false);
            return new RegionalAnnexRuntime(
                definition,
                connectorRoot,
                arrivalRoot,
                staticRoot,
                detailRoot,
                camp);
        }

        private static void BuildConnector(
            TetherfallGame game,
            RegionalAnnexDefinition definition,
            Transform parent)
        {
            if (definition.connectorPoints == null || definition.connectorPoints.Length < 2)
            {
                return;
            }

            var floorMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                Color.Lerp(definition.darkColor, definition.groundColor, 0.32f),
                0.04f,
                0.28f);
            var copperMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                RouteworkCopper,
                0.32f,
                0.52f);
            for (var index = 0; index < definition.connectorPoints.Length - 1; index++)
            {
                var start = definition.connectorPoints[index];
                var end = definition.connectorPoints[index + 1];
                var delta = end - start;
                var length = new Vector2(delta.x, delta.z).magnitude;
                var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                var midpoint = (start + end) * 0.5f - definition.center;
                midpoint.y = -0.28f;
                CreatePrimitive(
                    parent,
                    $"{definition.frontierName} bridge span {index + 1}",
                    PrimitiveType.Cube,
                    midpoint,
                    new Vector3(definition.connectorWidth, 0.56f, length + 1.2f),
                    new Vector3(0f, yaw, 0f),
                    floorMaterial,
                    true,
                    false);

                var perpendicular = length > 0.01f
                    ? new Vector3(-delta.z, 0f, delta.x).normalized
                    : Vector3.right;
                var railOffset = perpendicular * (definition.connectorWidth * 0.5f - 0.45f);
                for (var side = -1; side <= 1; side += 2)
                {
                    var railPosition = midpoint + railOffset * side;
                    railPosition.y = 0.38f;
                    CreatePrimitive(
                        parent,
                        $"{definition.frontierName} copper route rail {index + 1}.{side}",
                        PrimitiveType.Cube,
                        railPosition,
                        new Vector3(0.22f, 0.72f, length + 0.9f),
                        new Vector3(0f, yaw, 0f),
                        copperMaterial,
                        false,
                        false);
                }
            }

            var supportMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                Color.Lerp(definition.darkColor, Color.black, 0.24f),
                0.02f,
                0.2f);
            for (var index = 0; index < definition.connectorPoints.Length; index++)
            {
                var local = definition.connectorPoints[index] - definition.center;
                local.y = -1.28f;
                CreatePrimitive(
                    parent,
                    $"{definition.frontierName} bridge support {index + 1}",
                    PrimitiveType.Cylinder,
                    local,
                    new Vector3(
                        definition.connectorWidth * 0.16f,
                        1f,
                        definition.connectorWidth * 0.16f),
                    Vector3.zero,
                    supportMaterial,
                    false,
                    false);
            }
        }

        private static void BuildArrivalSupport(
            TetherfallGame game,
            RegionalAnnexDefinition definition,
            Transform parent)
        {
            var arrival = definition.threadway.arrivalPosition - definition.center;
            arrival.y = -0.25f;
            CreatePrimitive(
                parent,
                $"{definition.frontierName} Threadway arrival pad",
                PrimitiveType.Cube,
                arrival,
                new Vector3(12f, 0.5f, 12f),
                Vector3.zero,
                RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    Color.Lerp(definition.groundColor, definition.darkColor, 0.24f),
                    0.04f,
                    0.32f),
                true,
                false);

            var marker = arrival + new Vector3(4.7f, 1.2f, 4.7f);
            CreatePrimitive(
                parent,
                $"{definition.frontierName} arrival navigation marker",
                PrimitiveType.Cylinder,
                marker,
                new Vector3(0.14f, 1.2f, 0.14f),
                Vector3.zero,
                RuntimeEffectMaterialCache.GetGlow(game, NavigationTeal, 0.75f, 0.5f),
                false,
                false);
        }

        private static void BuildStaticTerrain(
            TetherfallGame game,
            RegionalAnnexDefinition definition,
            Transform parent)
        {
            CreatePrimitive(
                parent,
                $"{definition.frontierName} procedural ground",
                PrimitiveType.Cube,
                new Vector3(0f, -1f, 0f),
                new Vector3(definition.size.x, 2f, definition.size.y),
                Vector3.zero,
                GetGroundMaterial(game, definition),
                true,
                false);

            foreach (var piece in definition.terrainPieces)
            {
                if (piece == null)
                {
                    continue;
                }

                CreatePrimitive(
                    parent,
                    piece.displayName,
                    piece.primitive,
                    piece.localPosition,
                    piece.scale,
                    piece.eulerAngles,
                    GetMaterial(game, definition, piece.materialSlot),
                    piece.solid,
                    piece.climbable);
            }
        }

        private static EnemyCamp BuildDetail(
            TetherfallGame game,
            RegionalAnnexDefinition definition,
            Transform parent)
        {
            BuildLandmark(game, definition, parent);

            var campPosition = definition.center + definition.encounterLocalPosition;
            var camp = EnemyCamp.Create(
                game,
                definition.encounterId,
                campPosition,
                definition.encounterPersistenceId);
            FreezeBillboardLabels(camp.transform);
            camp.transform.SetParent(parent, true);
            AdoptLooseCampProps(parent, campPosition);

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
                    placement.color,
                    placement.persistenceId);
                FreezeBillboardLabels(node.transform);
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

                node.transform.SetParent(parent, true);
            }

            var secret = RegionalAnnexSecret.Create(game, definition);
            secret.transform.SetParent(parent, true);
            BuildRouteDressing(game, definition, parent);
            BuildAtmosphereField(game, definition, parent);
            return camp;
        }

        private static void BuildLandmark(
            TetherfallGame game,
            RegionalAnnexDefinition definition,
            Transform parent)
        {
            var landmarkRoot = new GameObject(definition.landmarkName);
            landmarkRoot.transform.SetParent(parent, false);
            landmarkRoot.transform.localPosition = definition.landmarkLocalPosition;
            var count = GetLandmarkPrimitiveCount(definition.landmarkStyle);
            var mode = (int)definition.landmarkStyle % 5;
            var accent = GetMaterial(game, definition, RegionPackMaterialSlot.Accent);
            var terrain = GetMaterial(game, definition, RegionPackMaterialSlot.Terrain);
            var dark = GetMaterial(game, definition, RegionPackMaterialSlot.Dark);
            var maximumHeight = 0f;
            for (var index = 0; index < count; index++)
            {
                var centered = index - (count - 1) * 0.5f;
                var height = 4.5f + ((index * 3 + (int)definition.landmarkStyle) % 5) * 1.25f;
                maximumHeight = Mathf.Max(maximumHeight, height);
                Vector3 position;
                Vector3 rotation;
                switch (mode)
                {
                    case 0:
                    {
                        var angle = (210f + index * 120f / Mathf.Max(1, count - 1)) * Mathf.Deg2Rad;
                        position = new Vector3(Mathf.Cos(angle) * 6f, height * 0.5f, Mathf.Sin(angle) * 4.5f);
                        rotation = new Vector3(0f, -35f + index * 16f, centered * 4f);
                        break;
                    }
                    case 1:
                        position = new Vector3(centered * 2.1f, height * 0.5f, Mathf.Abs(centered) * 1.35f);
                        rotation = new Vector3(centered * 3f, centered * 12f, centered * -5f);
                        break;
                    case 2:
                        position = new Vector3(
                            Mathf.Sin(index * 1.37f) * 4.6f,
                            height * 0.5f,
                            centered * 1.75f);
                        rotation = new Vector3(index % 2 == 0 ? 8f : -8f, index * 29f, centered * 3f);
                        break;
                    case 3:
                    {
                        var angle = index * Mathf.PI * 2f / count;
                        position = new Vector3(Mathf.Cos(angle) * 4.8f, height * 0.5f, Mathf.Sin(angle) * 4.8f);
                        rotation = new Vector3(centered * 2f, index * 360f / count, index % 2 == 0 ? 12f : -12f);
                        break;
                    }
                    default:
                        position = new Vector3(centered * 1.8f, height * 0.5f, (index % 2 == 0 ? -1f : 1f) * 2.8f);
                        rotation = new Vector3(index % 2 == 0 ? 18f : -18f, centered * 18f, centered * 5f);
                        break;
                }

                var primitive = index % 3 == 0
                    ? PrimitiveType.Cylinder
                    : index % 3 == 1
                        ? PrimitiveType.Cube
                        : PrimitiveType.Sphere;
                var scale = primitive == PrimitiveType.Sphere
                    ? new Vector3(1.25f + index * 0.08f, height * 0.18f, 0.72f)
                    : new Vector3(0.72f + index * 0.06f, height * 0.5f, 0.72f);
                CreatePrimitive(
                    landmarkRoot.transform,
                    $"{definition.landmarkName} silhouette {index + 1}",
                    primitive,
                    position,
                    scale,
                    rotation,
                    index % 3 == 0 ? accent : index % 3 == 1 ? terrain : dark,
                    false,
                    false);
            }

            WorldVisuals.AddLabel(
                landmarkRoot.transform,
                definition.landmarkName,
                new Vector3(0f, maximumHeight + 1.1f, 0f),
                Color.Lerp(definition.accentColor, Color.white, 0.3f),
                0.1f);
        }

        private static void BuildRouteDressing(
            TetherfallGame game,
            RegionalAnnexDefinition definition,
            Transform parent)
        {
            var root = new GameObject($"{definition.frontierName} route dressing");
            root.transform.SetParent(parent, false);
            var entry = definition.connectorPoints[definition.connectorPoints.Length - 1] - definition.center;
            entry.y = 0f;
            var inward = -new Vector3(entry.x, 0f, entry.z).normalized;
            if (inward.sqrMagnitude < 0.01f)
            {
                inward = Vector3.forward;
            }

            var perpendicular = new Vector3(-inward.z, 0f, inward.x);
            var tealMaterial = RuntimeEffectMaterialCache.GetGlow(game, NavigationTeal, 0.62f, 0.48f);
            var copperMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                RouteworkCopper,
                0.32f,
                0.52f);
            var yaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
            for (var index = 0; index < 4; index++)
            {
                var routePosition =
                    entry +
                    inward * (12f + index * 11f) +
                    perpendicular * (index % 2 == 0 ? -4f : 4f);
                routePosition.y = 1.35f;
                CreatePrimitive(
                    root.transform,
                    $"{definition.frontierName} teal waymark {index + 1}",
                    PrimitiveType.Cylinder,
                    routePosition,
                    new Vector3(0.13f, 1.35f, 0.13f),
                    Vector3.zero,
                    tealMaterial,
                    false,
                    false);

                var routeworkPosition = entry + inward * (18f + index * 10f);
                routeworkPosition.y = 0.28f;
                CreatePrimitive(
                    root.transform,
                    $"{definition.frontierName} copper routework {index + 1}",
                    PrimitiveType.Cube,
                    routeworkPosition,
                    new Vector3(3.8f, 0.18f, 0.32f),
                    new Vector3(0f, yaw, 0f),
                    copperMaterial,
                    false,
                    false);
            }
        }

        private static void BuildAtmosphereField(
            TetherfallGame game,
            RegionalAnnexDefinition definition,
            Transform parent)
        {
            var field = CreatePrimitive(
                parent,
                $"{definition.frontierName} bounded atmosphere field",
                PrimitiveType.Sphere,
                new Vector3(0f, 3.2f, 8f),
                new Vector3(definition.size.x * 0.34f, 3.8f, definition.size.y * 0.34f),
                Vector3.zero,
                RuntimeEffectMaterialCache.GetTransparent(
                    game,
                    new Color(
                        definition.atmosphereColor.r,
                        definition.atmosphereColor.g,
                        definition.atmosphereColor.b,
                        0.1f),
                    0.16f),
                false,
                false);
            field.AddComponent<RegionalAnnexAtmosphereField>().Initialize(
                definition.id,
                definition.WorldBounds);
        }

        private static GameObject CreatePrimitive(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 scale,
            Vector3 eulerAngles,
            Material material,
            bool solid,
            bool climbable)
        {
            var piece = GameObject.CreatePrimitive(primitive);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = scale;
            piece.transform.localRotation = Quaternion.Euler(eulerAngles);
            piece.GetComponent<Renderer>().sharedMaterial = material;
            var collider = piece.GetComponent<Collider>();
            if (!solid && collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            if (climbable)
            {
                piece.AddComponent<ClimbableSurface>();
            }

            return piece;
        }

        private static Material GetMaterial(
            TetherfallGame game,
            RegionalAnnexDefinition definition,
            RegionPackMaterialSlot slot)
        {
            if (slot == RegionPackMaterialSlot.Ground)
            {
                return GetGroundMaterial(game, definition);
            }

            var color = slot switch
            {
                RegionPackMaterialSlot.Terrain => definition.terrainColor,
                RegionPackMaterialSlot.Accent => definition.accentColor,
                _ => definition.darkColor
            };
            return RuntimeEffectMaterialCache.GetOpaque(
                game,
                color,
                slot == RegionPackMaterialSlot.Accent ? 0.22f : 0.04f,
                slot == RegionPackMaterialSlot.Accent ? 0.58f : 0.28f);
        }

        private static Material GetGroundMaterial(
            TetherfallGame game,
            RegionalAnnexDefinition definition)
        {
            if (GroundMaterials.TryGetValue(definition.id, out var cached) && cached != null)
            {
                return cached;
            }

            var material = game.CreateMaterial(definition.groundColor, 0.02f, 0.24f);
            var texture = GetGroundTexture(definition);
            if (material != null && texture != null)
            {
                material.mainTexture = texture;
                material.mainTextureScale = new Vector2(
                    Mathf.Max(8f, definition.size.x / 10f),
                    Mathf.Max(8f, definition.size.y / 10f));
            }

            GroundMaterials[definition.id] = material;
            return material;
        }

        private static Texture2D GetGroundTexture(RegionalAnnexDefinition definition)
        {
            if (GroundTextures.TryGetValue(definition.id, out var cached) && cached != null)
            {
                return cached;
            }

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"{definition.frontierName} procedural ground variation",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var seed = StableHash(definition.id);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var hash = seed ^ (uint)(x * 73856093) ^ (uint)(y * 19349663);
                    hash ^= hash >> 13;
                    hash *= 1274126177u;
                    var grain = (hash & 255u) / 255f;
                    var band = Mathf.Sin((x + y * 0.63f + (seed & 31u)) * 0.55f) * 0.035f;
                    var value = Mathf.Clamp(0.78f + grain * 0.2f + band, 0.7f, 1f);
                    pixels[y * size + x] = new Color(value, value, value, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            GroundTextures[definition.id] = texture;
            return texture;
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

        private static void FreezeBillboardLabels(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var billboards = root.GetComponentsInChildren<BillboardLabel>(true);
            foreach (var billboard in billboards)
            {
                if (billboard != null)
                {
                    UnityEngine.Object.Destroy(billboard);
                }
            }
        }

        private static int GetLandmarkPrimitiveCount(RegionalAnnexLandmarkStyle style)
        {
            return 5 + (int)style % 3;
        }

        private static List<RegionalAnnexDefinition> CreateDefinitions()
        {
            return new List<RegionalAnnexDefinition>
            {
                CreateLegacy(
                    "alderwake",
                    "Alderwake Basin",
                    new Rect(-80f, -130f, 160f, 260f),
                    new Vector3(0f, 0f, 590f),
                    "Rainroot Canopy",
                    "rainroot",
                    "Rain-fed canopy shelves braided around a high survey spur.",
                    "A dogleg bridge threads the gap between the northern region roads.",
                    ResourceKind.Timber,
                    ResourceKind.Resin,
                    ResourceKind.Herbs,
                    new Color(0.3f, 0.72f, 0.46f),
                    new Color(0.2f, 0.36f, 0.24f),
                    new Color(0.35f, 0.54f, 0.31f),
                    new Color(0.32f, 0.86f, 0.72f),
                    new Color(0.08f, 0.14f, 0.1f),
                    RegionalAnnexLandmarkStyle.CanopyCompass,
                    "Canopy Compass",
                    "Four rainroots hold one northward bearing",
                    "greenwake_survey",
                    "Greenwake Survey Cache",
                    "A route survey sleeps beneath the root that points beyond the rain.",
                    ResourceKind.Resin,
                    "encounter.alderwake.mossglass",
                    "briar_toll",
                    "Rainroot Verge Loom",
                    "A teal-tuned loom beyond the basin's last rainroot.",
                    new[]
                    {
                        new Vector3(72f, 0f, 122f),
                        new Vector3(72f, 0f, 140f),
                        new Vector3(86f, 0f, 140f),
                        new Vector3(86f, 0f, 248f),
                        new Vector3(86f, 0f, 356f),
                        new Vector3(86f, 0f, 440f),
                        new Vector3(72f, 0f, 468f)
                    }),
                CreateLegacy(
                    "morrowfen",
                    "Morrowfen",
                    new Rect(80f, -130f, 160f, 260f),
                    new Vector3(320f, 0f, -140f),
                    "Siltglass Reach",
                    "siltglass",
                    "Stillwater ledges, reedstone ramps, and a raised lantern bank.",
                    "A copper marsh bridge leaves the fen's southeastern shelf.",
                    ResourceKind.Resin,
                    ResourceKind.Herbs,
                    ResourceKind.Chimesalt,
                    new Color(0.21f, 0.65f, 0.68f),
                    new Color(0.13f, 0.28f, 0.29f),
                    new Color(0.27f, 0.46f, 0.45f),
                    new Color(0.31f, 0.87f, 0.79f),
                    new Color(0.05f, 0.12f, 0.13f),
                    RegionalAnnexLandmarkStyle.ReedwakeOrrery,
                    "Reedwake Orrery",
                    "A marsh calendar written in leaning reeds",
                    "stillwater_ledger",
                    "Stillwater Ledger",
                    "Lantern keepers sealed this tally above the last quiet tide.",
                    ResourceKind.Chimesalt,
                    "encounter.morrowfen.fen_tithe",
                    "lantern_levy",
                    "Siltglass Mooring Fork",
                    "A low fork tuned to the stillwater beyond Morrowfen.",
                    new[]
                    {
                        new Vector3(232f, 0f, -70f),
                        new Vector3(248f, 0f, -70f)
                    }),
                CreateLegacy(
                    "cinderwold",
                    "Cinderwold Verge",
                    new Rect(-240f, -130f, 160f, 260f),
                    new Vector3(-320f, 0f, -140f),
                    "Emberstep Shelf",
                    "emberstep",
                    "Cooling basalt steps climb toward a copper ash gantry.",
                    "A braced slag bridge leaves the verge's southwestern shelf.",
                    ResourceKind.Stone,
                    ResourceKind.Ore,
                    ResourceKind.Chimesalt,
                    new Color(0.82f, 0.34f, 0.2f),
                    new Color(0.25f, 0.14f, 0.13f),
                    new Color(0.48f, 0.24f, 0.19f),
                    new Color(0.93f, 0.48f, 0.24f),
                    new Color(0.1f, 0.055f, 0.05f),
                    RegionalAnnexLandmarkStyle.AshbellGantry,
                    "Ashbell Gantry",
                    "Cooling wind rings the suspended slag bells",
                    "cooling_vault",
                    "Cooling Vault",
                    "Relay smiths banked clean ore here before the shelf went dark.",
                    ResourceKind.Ore,
                    "encounter.cinderwold.relay_breakers",
                    "slagwatch",
                    "Emberstep Spur",
                    "A heat-scored spur anchored beneath the ashbell gantry.",
                    new[]
                    {
                        new Vector3(-232f, 0f, -70f),
                        new Vector3(-248f, 0f, -70f)
                    }),
                CreatePack(
                    "glassmere",
                    "region.glassmere",
                    new Vector3(-90f, 0f, 390f),
                    "Rainprism March",
                    "rainprism",
                    "Rain-dark prism shelves gather around an offset glass climb.",
                    "A short bridge rises through the western gap above Glassmere.",
                    ResourceKind.Chimesalt,
                    ResourceKind.Stone,
                    ResourceKind.Fiber,
                    RegionalAnnexLandmarkStyle.RainDial,
                    "Rain Dial",
                    "Seven wet vanes divide one reflected horizon",
                    "vanishing_reflection",
                    "Vanishing Reflection Cache",
                    "A lenswright hid salt where the rain erases its own image.",
                    ResourceKind.Chimesalt,
                    "mirror_ward",
                    "Rainprism Fork",
                    "A glass fork aligned to the march's rain dial.",
                    new[]
                    {
                        new Vector3(-50f, 0f, 282f),
                        new Vector3(-50f, 0f, 328f)
                    }),
                CreatePack(
                    "rimecoil",
                    "region.rimecoil",
                    new Vector3(500f, 0f, 240f),
                    "Hoarfold Shelf",
                    "hoarfold",
                    "Blue-ice folds step east toward a split survey needle.",
                    "A supported high bridge clears the Brassroot latitude.",
                    ResourceKind.Ore,
                    ResourceKind.Fiber,
                    ResourceKind.Chimesalt,
                    RegionalAnnexLandmarkStyle.SplitIceSextant,
                    "Split-Ice Sextant",
                    "Two needles measure the wind between them",
                    "warm_sextant_case",
                    "Warm Sextant Case",
                    "A coil tender wrapped this ore where the blue ice cannot close.",
                    ResourceKind.Ore,
                    "whitecoil_redoubt",
                    "Hoarfold Bearing",
                    "A frost bearing at the eastern end of the coil bridge.",
                    new[]
                    {
                        new Vector3(232f, 0f, 220f),
                        new Vector3(280f, 0f, 220f),
                        new Vector3(340f, 0f, 220f),
                        new Vector3(400f, 0f, 220f),
                        new Vector3(438f, 0f, 220f)
                    }),
                CreatePack(
                    "sunken_weald",
                    "region.sunken_weald",
                    new Vector3(-380f, 0f, 300f),
                    "Rootwake Fen",
                    "rootwake",
                    "Dry root shelves lift a narrow route above amber mud.",
                    "A two-span root bridge reaches west without crossing Lumenwaste.",
                    ResourceKind.Herbs,
                    ResourceKind.Resin,
                    ResourceKind.Timber,
                    RegionalAnnexLandmarkStyle.DrownedLanternCrown,
                    "Drowned Lantern Crown",
                    "A dry flame shape rises from waterless roots",
                    "amber_seedcase",
                    "Amber Seedcase",
                    "A grower sealed resin in the crown's one unwetted chamber.",
                    ResourceKind.Resin,
                    "rootwake_snare",
                    "Rootwake Shuttle",
                    "A root shuttle kept dry above the western fen.",
                    new[]
                    {
                        new Vector3(-232f, 0f, 260f),
                        new Vector3(-275f, 0f, 260f),
                        new Vector3(-318f, 0f, 260f)
                    }),
                CreatePack(
                    "prismscar",
                    "region.prismscar",
                    new Vector3(-230f, 0f, -380f),
                    "Afterlight Scar",
                    "afterlight",
                    "Dark flint terraces catch thin copper afterimages.",
                    "A corner bridge descends beyond the scar's southwest seam.",
                    ResourceKind.Stone,
                    ResourceKind.Ore,
                    ResourceKind.Chimesalt,
                    RegionalAnnexLandmarkStyle.FacetHarp,
                    "Facet Harp",
                    "Nine flint strings hold the sunset after dark",
                    "last_ray_case",
                    "Last-Ray Case",
                    "The array surveyors kept ore under the facet that never brightens.",
                    ResourceKind.Ore,
                    "afterlight_trench",
                    "Afterlight Meridian",
                    "A low meridian fork beneath the Facet Harp.",
                    new[]
                    {
                        new Vector3(-152f, 0f, -282f),
                        new Vector3(-170f, 0f, -300f),
                        new Vector3(-178f, 0f, -318f)
                    }),
                CreatePack(
                    "galecradle",
                    "region.galecradle",
                    new Vector3(240f, 0f, -380f),
                    "Kitefall Table",
                    "kitefall",
                    "Ochre shelves climb through a grounded wind-cut switchback.",
                    "A braced corner bridge leaves the mesa's southeast lip.",
                    ResourceKind.Fiber,
                    ResourceKind.Ore,
                    ResourceKind.Resin,
                    RegionalAnnexLandmarkStyle.WindYoke,
                    "Wind Yoke",
                    "A grounded frame makes the crosswind visible",
                    "lee_courier_box",
                    "Lee Courier Box",
                    "A courier tied fiber where the yoke divides the gust.",
                    ResourceKind.Fiber,
                    "kitefall_ring",
                    "Kitefall Groundline",
                    "A sheltered groundline that does not depend on the updraft.",
                    new[]
                    {
                        new Vector3(162f, 0f, -282f),
                        new Vector3(180f, 0f, -300f),
                        new Vector3(178f, 0f, -318f)
                    }),
                CreatePack(
                    "lumenwaste",
                    "region.lumenwaste",
                    new Vector3(-520f, 0f, 120f),
                    "Dusklens Margin",
                    "dusklens",
                    "Muted phosphor shelves climb toward a west-facing lens line.",
                    "A coppered lens bridge extends directly from the western margin.",
                    ResourceKind.Chimesalt,
                    ResourceKind.Stone,
                    ResourceKind.Fiber,
                    RegionalAnnexLandmarkStyle.DawnLens,
                    "Dawn Lens",
                    "The western glass keeps a sunrise no traveler saw",
                    "unlit_aperture",
                    "Unlit Aperture Cache",
                    "Chimesalt rests behind the lens that refuses the noon glare.",
                    ResourceKind.Chimesalt,
                    "dusklens_walk",
                    "Dusklens Aperture",
                    "A shaded aperture at the world's western lens margin.",
                    new[]
                    {
                        new Vector3(-442f, 0f, 100f),
                        new Vector3(-458f, 0f, 100f)
                    }),
                CreatePack(
                    "brassroot",
                    "region.brassroot",
                    new Vector3(500f, 0f, 40f),
                    "Quenchroot Gallery",
                    "quenchroot",
                    "Cooled casting steps descend beneath forked brass roots.",
                    "A short intake bridge extends from the foundry's eastern wall.",
                    ResourceKind.Ore,
                    ResourceKind.Stone,
                    ResourceKind.Resin,
                    RegionalAnnexLandmarkStyle.PouringRoot,
                    "Pouring Root",
                    "A frozen metal stream branches before touching stone",
                    "cold_tally",
                    "Cold Tally Cache",
                    "The final intake tally marks ore beneath a root that cooled whole.",
                    ResourceKind.Ore,
                    "quenchroot_terrace",
                    "Quenchroot Intake",
                    "A cool intake fork below the Pouring Root.",
                    new[]
                    {
                        new Vector3(422f, 0f, 70f),
                        new Vector3(438f, 0f, 70f)
                    }),
                CreatePack(
                    "cloudbarrow",
                    "region.cloudbarrow",
                    new Vector3(-230f, 0f, 530f),
                    "Highwake Barrows",
                    "highwake",
                    "Wind shelves step through grass toward a narrow sky cairn.",
                    "A processional bridge continues north from the existing barrows.",
                    ResourceKind.Herbs,
                    ResourceKind.Fiber,
                    ResourceKind.Stone,
                    RegionalAnnexLandmarkStyle.SkyCairn,
                    "Sky Cairn",
                    "Three open capstones leave a road for weather",
                    "weatherward_bundle",
                    "Weatherward Bundle",
                    "A grasskeeper packed fiber in the cairn's stillest lee.",
                    ResourceKind.Fiber,
                    "highwake_gate",
                    "Highwake Processional",
                    "A grass-bound fork beyond the last cloud shelf.",
                    new[]
                    {
                        new Vector3(-250f, 0f, 452f),
                        new Vector3(-250f, 0f, 468f)
                    }),
                CreatePack(
                    "tideglass",
                    "region.tideglass",
                    new Vector3(0f, 0f, -550f),
                    "Ebbmirror Flats",
                    "ebbmirror",
                    "Dry black shelves rise above a field of mineral mirrors.",
                    "A drymark bridge continues south beyond the old strand.",
                    ResourceKind.Chimesalt,
                    ResourceKind.Stone,
                    ResourceKind.Ore,
                    RegionalAnnexLandmarkStyle.TidalComb,
                    "Tidal Comb",
                    "Eight dry teeth divide the reflected horizon",
                    "ebb_reader_case",
                    "Ebb-Reader Case",
                    "A tide reader left salt above the mirror that never fills.",
                    ResourceKind.Chimesalt,
                    "ebbmirror_spines",
                    "Ebbmirror Dryline",
                    "A dryline fork beyond the strand's southern mirror.",
                    new[]
                    {
                        new Vector3(0f, 0f, -472f),
                        new Vector3(0f, 0f, -488f)
                    }),
                CreatePack(
                    "nightloom",
                    "region.nightloom",
                    new Vector3(245f, 0f, 530f),
                    "Starroot Cleft",
                    "starroot",
                    "Violet root shelves braid upward around a narrow dark seam.",
                    "A woven bridge continues north from the existing canopy.",
                    ResourceKind.Herbs,
                    ResourceKind.Resin,
                    ResourceKind.Fiber,
                    RegionalAnnexLandmarkStyle.MoonLattice,
                    "Moon Lattice",
                    "A root grid holds thirteen pieces of borrowed night",
                    "darkstrand_knot",
                    "Darkstrand Knot",
                    "A canopy tender hid resin in the lattice's only unlit crossing.",
                    ResourceKind.Resin,
                    "starroot_ambush",
                    "Starroot High Weave",
                    "A violet fork above the hollow's northern canopy.",
                    new[]
                    {
                        new Vector3(245f, 0f, 452f),
                        new Vector3(245f, 0f, 468f)
                    })
            };
        }

        private static RegionalAnnexDefinition CreateLegacy(
            string slug,
            string parentDisplayName,
            Rect parentBounds,
            Vector3 center,
            string frontierName,
            string terrainToken,
            string biomeBrief,
            string routeBrief,
            ResourceKind firstResource,
            ResourceKind secondResource,
            ResourceKind thirdResource,
            Color mapColor,
            Color groundColor,
            Color terrainColor,
            Color accentColor,
            Color darkColor,
            RegionalAnnexLandmarkStyle landmarkStyle,
            string landmarkName,
            string landmarkSubtitle,
            string secretToken,
            string secretName,
            string secretInscription,
            ResourceKind secretResource,
            string encounterId,
            string campToken,
            string threadwayTitle,
            string threadwayDescription,
            Vector3[] connectorPoints)
        {
            return CreateDefinition(
                slug,
                $"region.{slug}",
                parentDisplayName,
                parentBounds,
                center,
                new Vector2(160f, 260f),
                frontierName,
                terrainToken,
                biomeBrief,
                routeBrief,
                firstResource,
                secondResource,
                thirdResource,
                mapColor,
                groundColor,
                terrainColor,
                accentColor,
                darkColor,
                Color.Lerp(mapColor, groundColor, 0.42f),
                landmarkStyle,
                landmarkName,
                landmarkSubtitle,
                secretToken,
                secretName,
                secretInscription,
                secretResource,
                encounterId,
                campToken,
                threadwayTitle,
                threadwayDescription,
                connectorPoints);
        }

        private static RegionalAnnexDefinition CreatePack(
            string slug,
            string parentRegionId,
            Vector3 center,
            string frontierName,
            string terrainToken,
            string biomeBrief,
            string routeBrief,
            ResourceKind firstResource,
            ResourceKind secondResource,
            ResourceKind thirdResource,
            RegionalAnnexLandmarkStyle landmarkStyle,
            string landmarkName,
            string landmarkSubtitle,
            string secretToken,
            string secretName,
            string secretInscription,
            ResourceKind secretResource,
            string campToken,
            string threadwayTitle,
            string threadwayDescription,
            Vector3[] connectorPoints)
        {
            var parent = TetherfallRegionPacks.Get(parentRegionId);
            if (parent == null)
            {
                throw new InvalidOperationException(
                    $"Regional annex '{slug}' references missing parent region '{parentRegionId}'.");
            }

            return CreateDefinition(
                slug,
                parentRegionId,
                parent.displayName,
                parent.WorldBounds,
                center,
                new Vector2(140f, 140f),
                frontierName,
                terrainToken,
                biomeBrief,
                routeBrief,
                firstResource,
                secondResource,
                thirdResource,
                parent.mapColor,
                parent.groundColor,
                parent.terrainColor,
                parent.accentColor,
                parent.darkColor,
                parent.fogColor,
                landmarkStyle,
                landmarkName,
                landmarkSubtitle,
                secretToken,
                secretName,
                secretInscription,
                secretResource,
                parent.encounterId,
                campToken,
                threadwayTitle,
                threadwayDescription,
                connectorPoints);
        }

        private static RegionalAnnexDefinition CreateDefinition(
            string slug,
            string parentRegionId,
            string parentDisplayName,
            Rect parentBounds,
            Vector3 center,
            Vector2 size,
            string frontierName,
            string terrainToken,
            string biomeBrief,
            string routeBrief,
            ResourceKind firstResource,
            ResourceKind secondResource,
            ResourceKind thirdResource,
            Color mapColor,
            Color groundColor,
            Color terrainColor,
            Color accentColor,
            Color darkColor,
            Color atmosphereColor,
            RegionalAnnexLandmarkStyle landmarkStyle,
            string landmarkName,
            string landmarkSubtitle,
            string secretToken,
            string secretName,
            string secretInscription,
            ResourceKind secretResource,
            string encounterId,
            string campToken,
            string threadwayTitle,
            string threadwayDescription,
            Vector3[] connectorPoints)
        {
            var entry = connectorPoints[connectorPoints.Length - 1];
            var inward = center - entry;
            inward.y = 0f;
            inward = inward.sqrMagnitude > 0.01f ? inward.normalized : Vector3.forward;
            var yaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
            var threadwayPosition = entry + inward * 14f;
            threadwayPosition.y = 0f;
            var arrivalPosition = threadwayPosition + inward * 5f;
            arrivalPosition.y = 0.05f;
            var variant = (int)landmarkStyle;

            return new RegionalAnnexDefinition
            {
                id = $"annex.{slug}.frontier",
                parentRegionId = parentRegionId,
                parentDisplayName = parentDisplayName,
                frontierName = frontierName,
                biomeBrief = biomeBrief,
                routeBrief = routeBrief,
                parentBounds = parentBounds,
                center = center,
                size = size,
                mapColor = mapColor,
                groundColor = groundColor,
                terrainColor = terrainColor,
                accentColor = accentColor,
                darkColor = darkColor,
                atmosphereColor = atmosphereColor,
                connectorPoints = connectorPoints,
                connectorWidth = 12f,
                terrainPieces = CreateTerrain(slug, frontierName, terrainToken, yaw, variant),
                resources = CreateResources(
                    slug,
                    terrainToken,
                    firstResource,
                    secondResource,
                    thirdResource,
                    terrainColor,
                    accentColor,
                    groundColor,
                    yaw),
                landmarkStyle = landmarkStyle,
                landmarkName = landmarkName,
                landmarkSubtitle = landmarkSubtitle,
                landmarkLocalPosition = Rotate(new Vector3(28f, 0f, 32f), yaw),
                secretId = $"secret.annex.{slug}.{secretToken}",
                secretName = secretName,
                secretInscription = secretInscription,
                secretLocalPosition = Rotate(new Vector3(-48f, 0.45f, -42f), yaw),
                secretResource = secretResource,
                secretResourceAmount = 3,
                encounterId = encounterId,
                encounterPersistenceId = $"encounter.annex.{slug}.{campToken}",
                encounterLocalPosition = Rotate(new Vector3(48f, 0f, 2f), yaw),
                threadway = new ThreadwayDefinition
                {
                    id = $"threadway.annex.{slug}",
                    title = threadwayTitle,
                    region = parentDisplayName,
                    description = threadwayDescription,
                    position = threadwayPosition,
                    arrivalPosition = arrivalPosition,
                    activationChimesaltCost = 2 + variant % 2,
                    accentColor = accentColor
                },
                routeDressingPieceCount = RouteDressingRendererBudget,
                hasProceduralGroundVariation = true,
                hasBoundedAtmosphereField = true
            };
        }

        private static RegionalAnnexTerrainPiece[] CreateTerrain(
            string slug,
            string frontierName,
            string terrainToken,
            float yaw,
            int variant)
        {
            var lateral = (variant % 3 - 1) * 2f;
            var longitudinal = (variant % 4 - 1.5f) * 1.5f;
            var mirror = variant % 2 == 0 ? 1f : -1f;
            var middleOffset = (variant % 5 - 2) * 3.5f;
            var reliefBonus = variant % 3 * 0.35f;
            var shelfYaw = (variant % 5 - 2) * 4f;
            return new[]
            {
                Terrain(
                    slug,
                    terrainToken,
                    "lower_shelf",
                    $"{frontierName} lower shelf",
                    PrimitiveType.Cube,
                    new Vector3((-32f + lateral) * mirror, 1f, -10f + longitudinal),
                    new Vector3(26f + variant % 3 * 2f, 2f, 22f + variant % 2 * 3f),
                    new Vector3(0f, shelfYaw, 0f),
                    RegionPackMaterialSlot.Ground,
                    true,
                    false,
                    false,
                    yaw),
                Terrain(
                    slug,
                    terrainToken,
                    "middle_step",
                    $"{frontierName} middle step",
                    PrimitiveType.Cube,
                    new Vector3(middleOffset, 2.4f + reliefBonus, 6f + variant % 4 * 1.5f),
                    new Vector3(24f + variant % 3 * 2f, 4.8f + reliefBonus * 2f, 20f + variant % 2 * 3f),
                    new Vector3(0f, -shelfYaw * 0.5f, 0f),
                    RegionPackMaterialSlot.Terrain,
                    true,
                    false,
                    false,
                    yaw),
                Terrain(
                    slug,
                    terrainToken,
                    "high_shelf",
                    $"{frontierName} high shelf",
                    PrimitiveType.Cube,
                    new Vector3((27f + variant % 4 * 2.5f) * mirror, 4.2f + reliefBonus, 24f + variant % 3 * 3f),
                    new Vector3(22f + variant % 2 * 2f, 8.4f + reliefBonus * 2f, 20f + variant % 4),
                    new Vector3(0f, shelfYaw * 0.75f, 0f),
                    RegionPackMaterialSlot.Ground,
                    true,
                    true,
                    false,
                    yaw),
                Terrain(
                    slug,
                    terrainToken,
                    "lower_ramp",
                    $"{frontierName} lower ramp",
                    PrimitiveType.Cube,
                    new Vector3((-16f + middleOffset * 0.25f) * mirror, 1.55f, -2f + variant % 3),
                    new Vector3(20f + variant % 3, 0.9f, 8f),
                    new Vector3(0f, shelfYaw * 0.4f, (-11f - variant % 3) * mirror),
                    RegionPackMaterialSlot.Accent,
                    true,
                    false,
                    true,
                    yaw),
                Terrain(
                    slug,
                    terrainToken,
                    "upper_ramp",
                    $"{frontierName} upper ramp",
                    PrimitiveType.Cube,
                    new Vector3((15f + variant % 3) * mirror, 3.45f + reliefBonus, 15f + variant % 4 * 1.5f),
                    new Vector3(21f + variant % 2 * 2f, 0.9f, 8f),
                    new Vector3(0f, -shelfYaw * 0.35f, (-14f - variant % 2) * mirror),
                    RegionPackMaterialSlot.Accent,
                    true,
                    false,
                    true,
                    yaw),
                Terrain(
                    slug,
                    terrainToken,
                    "overlook",
                    $"{frontierName} overlook",
                    PrimitiveType.Cube,
                    new Vector3((-28f - variant % 3 * 2f) * mirror, 3.2f + reliefBonus, 36f - longitudinal),
                    new Vector3(25f + variant % 3, 6.4f + reliefBonus * 2f, 15f + variant % 2 * 2f),
                    new Vector3(0f, shelfYaw + (variant % 2 == 0 ? -7f : 7f), 0f),
                    RegionPackMaterialSlot.Terrain,
                    true,
                    true,
                    false,
                    yaw),
                Terrain(
                    slug,
                    terrainToken,
                    "route_span",
                    $"{frontierName} route span",
                    PrimitiveType.Cube,
                    new Vector3(-20f * mirror, 0.35f, -30f - variant % 3 * 2f),
                    new Vector3(7f + variant % 2, 0.7f, 23f + variant % 3),
                    new Vector3(0f, shelfYaw * 0.25f, 0f),
                    RegionPackMaterialSlot.Dark,
                    true,
                    false,
                    false,
                    yaw),
                Terrain(
                    slug,
                    terrainToken,
                    "witness_spire",
                    $"{frontierName} witness spire",
                    variant % 2 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube,
                    new Vector3((39f + variant % 4 * 2f) * mirror, 6f + reliefBonus, 34f + variant % 3 * 2f),
                    new Vector3(2.2f + variant % 3 * 0.2f, 6f + reliefBonus, 2.2f + variant % 2 * 0.3f),
                    new Vector3(variant % 2 == 0 ? 8f : -8f, variant * 17f, variant % 3 == 0 ? 9f : -9f),
                    RegionPackMaterialSlot.Dark,
                    false,
                    false,
                    false,
                    yaw)
            };
        }

        private static RegionalAnnexTerrainPiece Terrain(
            string slug,
            string terrainToken,
            string role,
            string displayName,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 scale,
            Vector3 eulerAngles,
            RegionPackMaterialSlot materialSlot,
            bool solid,
            bool climbable,
            bool isRamp,
            float yaw)
        {
            return new RegionalAnnexTerrainPiece
            {
                id = $"terrain.annex.{slug}.{terrainToken}_{role}",
                displayName = displayName,
                primitive = primitive,
                localPosition = Rotate(localPosition, yaw),
                scale = scale,
                eulerAngles = new Vector3(
                    eulerAngles.x,
                    eulerAngles.y + yaw,
                    eulerAngles.z),
                materialSlot = materialSlot,
                solid = solid,
                climbable = climbable,
                isRamp = isRamp
            };
        }

        private static RegionalAnnexResourcePlacement[] CreateResources(
            string slug,
            string resourceToken,
            ResourceKind first,
            ResourceKind second,
            ResourceKind third,
            Color terrainColor,
            Color accentColor,
            Color groundColor,
            float yaw)
        {
            return new[]
            {
                Resource(slug, resourceToken, first, "a", new Vector3(-52f, 0.75f, -18f),
                    Color.Lerp(terrainColor, accentColor, 0.16f), yaw),
                Resource(slug, resourceToken, first, "b", new Vector3(52f, 0.75f, -18f),
                    Color.Lerp(terrainColor, accentColor, 0.16f), yaw),
                Resource(slug, resourceToken, second, "a", new Vector3(-50f, 0.75f, 18f),
                    Color.Lerp(groundColor, accentColor, 0.28f), yaw),
                Resource(slug, resourceToken, second, "b", new Vector3(50f, 0.75f, 18f),
                    Color.Lerp(groundColor, accentColor, 0.28f), yaw),
                Resource(slug, resourceToken, third, "a", new Vector3(-34f, 0.75f, 48f),
                    Color.Lerp(terrainColor, Color.white, 0.18f), yaw),
                Resource(slug, resourceToken, third, "b", new Vector3(34f, 0.75f, 48f),
                    Color.Lerp(terrainColor, Color.white, 0.18f), yaw)
            };
        }

        private static RegionalAnnexResourcePlacement Resource(
            string slug,
            string resourceToken,
            ResourceKind kind,
            string suffix,
            Vector3 localPosition,
            Color color,
            float yaw)
        {
            return new RegionalAnnexResourcePlacement
            {
                persistenceId =
                    $"resource.annex.{slug}.{resourceToken}_{kind.ToString().ToLowerInvariant()}_{suffix}",
                kind = kind,
                localPosition = Rotate(localPosition, yaw),
                color = color
            };
        }

        private static Vector3 Rotate(Vector3 value, float yaw)
        {
            return Quaternion.Euler(0f, yaw, 0f) * value;
        }

        private static uint StableHash(string value)
        {
            var hash = 2166136261u;
            if (value == null)
            {
                return hash;
            }

            for (var index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= 16777619u;
            }

            return hash;
        }

        private static bool ContainsConnector(
            RegionalAnnexDefinition definition,
            Vector3 position)
        {
            if (definition.connectorPoints == null ||
                definition.connectorPoints.Length < 2)
            {
                return false;
            }

            var maximumDistance = definition.connectorWidth * 0.5f + 0.5f;
            for (var index = 0; index < definition.connectorPoints.Length - 1; index++)
            {
                var start = definition.connectorPoints[index];
                var end = definition.connectorPoints[index + 1];
                var segment = new Vector2(end.x - start.x, end.z - start.z);
                var offset = new Vector2(position.x - start.x, position.z - start.z);
                var lengthSquared = segment.sqrMagnitude;
                var amount = lengthSquared > 0.0001f
                    ? Mathf.Clamp01(Vector2.Dot(offset, segment) / lengthSquared)
                    : 0f;
                var nearest = new Vector2(start.x, start.z) + segment * amount;
                var delta = new Vector2(position.x - nearest.x, position.z - nearest.y);
                if (delta.sqrMagnitude <= maximumDistance * maximumDistance)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class RegionalAnnexRuntime
    {
        public RegionalAnnexDefinition Definition { get; }
        public GameObject ConnectorRoot { get; }
        public GameObject ArrivalRoot { get; }
        public GameObject StaticRoot { get; }
        public GameObject DetailRoot { get; }
        public EnemyCamp Camp { get; }
        public bool ThreadwayAttached { get; set; }

        public RegionalAnnexRuntime(
            RegionalAnnexDefinition definition,
            GameObject connectorRoot,
            GameObject arrivalRoot,
            GameObject staticRoot,
            GameObject detailRoot,
            EnemyCamp camp)
        {
            Definition = definition;
            ConnectorRoot = connectorRoot;
            ArrivalRoot = arrivalRoot;
            StaticRoot = staticRoot;
            DetailRoot = detailRoot;
            Camp = camp;
        }
    }

    [DefaultExecutionOrder(-9990)]
    public sealed class RegionalAnnexDirector : MonoBehaviour
    {
        private const float CampWakePriorityDistance =
            TetherfallRegionalAnnexes.CampPopulationWakeDistance + 2f;
        private const float CampSoftResetDistance =
            TetherfallRegionalAnnexes.CampPopulationSoftResetDistance;
        private const float CampOwnershipRefreshSeconds = 0.1f;
        private static readonly FieldInfo EnemyCampPopulationField =
            typeof(EnemyCamp).GetField(
                "aliveEnemies",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly List<RegionalAnnexRuntime> runtimes = new List<RegionalAnnexRuntime>(13);
        private readonly List<ActivationCandidate> staticCandidates = new List<ActivationCandidate>(13);
        private readonly List<ActivationCandidate> detailCandidates = new List<ActivationCandidate>(13);
        private TetherfallGame game;
        private RegionalAnnexDefinition frameAnnex;
        private float nextRefreshAt;
        private float nextCampOwnershipRefreshAt;

        internal void Initialize(
            TetherfallGame owner,
            IEnumerable<RegionalAnnexRuntime> annexRuntimes)
        {
            game = owner;
            runtimes.Clear();
            if (annexRuntimes != null)
            {
                runtimes.AddRange(annexRuntimes);
            }

            nextRefreshAt = 0f;
            nextCampOwnershipRefreshAt = 0f;
        }

        internal void PrimeForArrival(Vector3 arrivalPosition)
        {
            frameAnnex = TetherfallRegionalAnnexes.Find(arrivalPosition);
            if (frameAnnex == null)
            {
                return;
            }

            RefreshActivation(arrivalPosition);
            nextRefreshAt =
                Time.unscaledTime + TetherfallRegionalAnnexes.ActivationRefreshSeconds;
        }

        private void Update()
        {
            if (game == null || game.Player == null || game.PlayerTransform == null)
            {
                frameAnnex = null;
                return;
            }

            var playerPosition = game.PlayerTransform.position;
            frameAnnex = TetherfallRegionalAnnexes.Find(playerPosition);
            var needsImmediateWake =
                frameAnnex != null &&
                (!IsStaticActive(frameAnnex.id) || !IsDetailActive(frameAnnex.id));
            if (!needsImmediateWake && Time.unscaledTime < nextRefreshAt)
            {
                return;
            }

            nextRefreshAt =
                Time.unscaledTime + TetherfallRegionalAnnexes.ActivationRefreshSeconds;
            RefreshActivation(playerPosition);
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < nextCampOwnershipRefreshAt)
            {
                return;
            }

            nextCampOwnershipRefreshAt = Time.unscaledTime + CampOwnershipRefreshSeconds;
            AdoptLooseCampEnemies();
        }

        private void RefreshActivation(Vector3 playerPosition)
        {
            staticCandidates.Clear();
            detailCandidates.Clear();
            foreach (var runtime in runtimes)
            {
                if (runtime == null ||
                    runtime.Definition == null ||
                    runtime.StaticRoot == null ||
                    runtime.DetailRoot == null)
                {
                    continue;
                }

                AttachThreadway(runtime);
                var distance = HorizontalDistanceToBounds(
                    playerPosition,
                    runtime.Definition.WorldBounds);
                var contains = runtime.Definition.Contains(playerPosition);
                var staticThreshold =
                    TetherfallRegionalAnnexes.StaticPrewakeDistance +
                    (runtime.StaticRoot.activeSelf
                        ? TetherfallRegionalAnnexes.StaticHysteresisDistance
                        : 0f);
                if (contains || distance <= staticThreshold)
                {
                    var staticScore = contains ? distance - 10000f : distance;
                    if (runtime.Camp != null &&
                        runtime.DetailRoot.activeSelf &&
                        runtime.Camp.IsCombatLocked)
                    {
                        staticScore -= 20000f;
                    }
                    else if (runtime.Camp != null &&
                             HorizontalDistance(
                                 playerPosition,
                                 runtime.Camp.transform.position) <=
                             CampWakePriorityDistance)
                    {
                        staticScore -= 12000f;
                    }

                    staticCandidates.Add(new ActivationCandidate(
                        runtime,
                        staticScore,
                        runtime.StaticRoot.activeSelf));
                }

                var detailThreshold =
                    TetherfallRegionalAnnexes.DetailPrewakeDistance +
                    (runtime.DetailRoot.activeSelf
                        ? TetherfallRegionalAnnexes.DetailHysteresisDistance
                        : 0f);
                if (contains || distance <= detailThreshold)
                {
                    var detailScore = contains ? distance - 10000f : distance;
                    if (runtime.Camp != null)
                    {
                        var campDistance = HorizontalDistance(
                            playerPosition,
                            runtime.Camp.transform.position);
                        if (runtime.DetailRoot.activeSelf && runtime.Camp.IsCombatLocked)
                        {
                            detailScore -= 20000f;
                        }
                        else if (campDistance <= CampWakePriorityDistance)
                        {
                            detailScore -= 12000f;
                        }
                    }

                    detailCandidates.Add(new ActivationCandidate(
                        runtime,
                        detailScore,
                        runtime.DetailRoot.activeSelf));
                }
            }

            staticCandidates.Sort(ActivationCandidateComparer.Instance);
            detailCandidates.Sort(ActivationCandidateComparer.Instance);
            for (var index = 0; index < runtimes.Count; index++)
            {
                var runtime = runtimes[index];
                if (runtime == null)
                {
                    continue;
                }

                var staticActive = IsSelected(
                    staticCandidates,
                    runtime,
                    TetherfallRegionalAnnexes.MaxActiveStaticRoots);
                var detailActive = IsSelected(
                    detailCandidates,
                    runtime,
                    TetherfallRegionalAnnexes.MaxActiveDetailRoots);
                if (runtime.StaticRoot.activeSelf != staticActive)
                {
                    runtime.StaticRoot.SetActive(staticActive);
                }

                if (runtime.DetailRoot.activeSelf != detailActive)
                {
                    if (!detailActive &&
                        runtime.Camp != null &&
                        HorizontalDistance(
                            playerPosition,
                            runtime.Camp.transform.position) > CampSoftResetDistance)
                    {
                        runtime.Camp.EvaluatePopulation(playerPosition, true);
                    }

                    runtime.DetailRoot.SetActive(detailActive);
                }
            }
        }

        private void AdoptLooseCampEnemies()
        {
            foreach (var runtime in runtimes)
            {
                if (runtime == null ||
                    runtime.Camp == null ||
                    runtime.DetailRoot == null ||
                    !runtime.Camp.IsPopulated)
                {
                    continue;
                }

                var enemies =
                    EnemyCampPopulationField?.GetValue(runtime.Camp) as List<Enemy>;
                if (enemies == null)
                {
                    continue;
                }

                foreach (var enemy in enemies)
                {
                    if (enemy == null ||
                        enemy.transform.IsChildOf(runtime.DetailRoot.transform))
                    {
                        continue;
                    }

                    enemy.transform.SetParent(runtime.DetailRoot.transform, true);
                }
            }
        }

        private void AttachThreadway(RegionalAnnexRuntime runtime)
        {
            if (runtime.ThreadwayAttached ||
                runtime.Definition.threadway == null ||
                runtime.StaticRoot == null)
            {
                return;
            }

            var anchor = game.GetRegionPackThreadway(runtime.Definition.threadway.id);
            if (anchor != null)
            {
                TetherfallRegionalAnnexes.AttachThreadwayShell(anchor);
            }
        }

        private bool IsStaticActive(string id)
        {
            foreach (var runtime in runtimes)
            {
                if (runtime != null &&
                    runtime.Definition != null &&
                    string.Equals(runtime.Definition.id, id, StringComparison.Ordinal))
                {
                    return runtime.StaticRoot != null && runtime.StaticRoot.activeSelf;
                }
            }

            return false;
        }

        private bool IsDetailActive(string id)
        {
            foreach (var runtime in runtimes)
            {
                if (runtime != null &&
                    runtime.Definition != null &&
                    string.Equals(runtime.Definition.id, id, StringComparison.Ordinal))
                {
                    return runtime.DetailRoot != null && runtime.DetailRoot.activeSelf;
                }
            }

            return false;
        }

        private static bool IsSelected(
            List<ActivationCandidate> candidates,
            RegionalAnnexRuntime runtime,
            int ceiling)
        {
            var count = Mathf.Min(ceiling, candidates.Count);
            for (var index = 0; index < count; index++)
            {
                if (ReferenceEquals(candidates[index].Runtime, runtime))
                {
                    return true;
                }
            }

            return false;
        }

        private static float HorizontalDistanceToBounds(Vector3 position, Rect bounds)
        {
            var deltaX = position.x < bounds.xMin
                ? bounds.xMin - position.x
                : position.x > bounds.xMax
                    ? position.x - bounds.xMax
                    : 0f;
            var deltaZ = position.z < bounds.yMin
                ? bounds.yMin - position.z
                : position.z > bounds.yMax
                    ? position.z - bounds.yMax
                    : 0f;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right)
        {
            var deltaX = left.x - right.x;
            var deltaZ = left.z - right.z;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        private readonly struct ActivationCandidate
        {
            public RegionalAnnexRuntime Runtime { get; }
            public float Score { get; }
            public bool WasActive { get; }

            public ActivationCandidate(
                RegionalAnnexRuntime runtime,
                float score,
                bool wasActive)
            {
                Runtime = runtime;
                Score = score;
                WasActive = wasActive;
            }
        }

        private sealed class ActivationCandidateComparer : IComparer<ActivationCandidate>
        {
            public static readonly ActivationCandidateComparer Instance =
                new ActivationCandidateComparer();

            public int Compare(ActivationCandidate left, ActivationCandidate right)
            {
                var score = left.Score.CompareTo(right.Score);
                if (score != 0)
                {
                    return score;
                }

                if (left.WasActive != right.WasActive)
                {
                    return left.WasActive ? -1 : 1;
                }

                return string.CompareOrdinal(
                    left.Runtime.Definition.id,
                    right.Runtime.Definition.id);
            }
        }
    }

    public sealed class RegionalAnnexAtmosphereField : MonoBehaviour
    {
        public string AnnexId { get; private set; }
        public Rect WorldBounds { get; private set; }

        public void Initialize(string annexId, Rect worldBounds)
        {
            AnnexId = annexId;
            WorldBounds = worldBounds;
        }
    }

    public sealed class RegionalAnnexSecret : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private RegionalAnnexDefinition definition;
        private Renderer shellRenderer;
        private Renderer crestRenderer;
        private bool recovered;

        public string PersistenceId => definition != null ? definition.secretId : string.Empty;
        public string InteractionPrompt => recovered
            ? $"{definition.secretName} recovered"
            : $"Recover {definition.secretName}";

        public static RegionalAnnexSecret Create(
            TetherfallGame game,
            RegionalAnnexDefinition definition)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = definition.secretName;
            root.transform.position = definition.center + definition.secretLocalPosition;
            root.transform.localScale = new Vector3(1.05f, 0.78f, 0.86f);
            var secret = root.AddComponent<RegionalAnnexSecret>();
            secret.game = game;
            secret.definition = definition;
            secret.shellRenderer = root.GetComponent<Renderer>();

            var crest = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crest.name = "Annex secret Threadglass crest";
            crest.transform.SetParent(root.transform, false);
            crest.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            crest.transform.localScale = new Vector3(0.32f, 0.12f, 0.32f);
            secret.crestRenderer = crest.GetComponent<Renderer>();
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
                !owner.TryRecoverRegionalAnnexSecret(definition))
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

            recovered = game.IsRegionalAnnexSecretRecovered(definition.secretId);
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (shellRenderer != null)
            {
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
                crestRenderer.sharedMaterial = RuntimeEffectMaterialCache.GetGlow(
                    game,
                    definition.accentColor,
                    1.15f,
                    0.62f);
            }
        }
    }

    public sealed partial class TetherfallGame
    {
        internal bool IsRegionalAnnexSecretRecovered(string id)
        {
            return !string.IsNullOrEmpty(id) && recoveredFieldCacheIds.Contains(id);
        }

        internal bool TryRecoverRegionalAnnexSecret(RegionalAnnexDefinition definition)
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
