using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Tetherfall
{
    /// <summary>
    /// Stable, serialized identities for the Alderwake Threadheart settlement art family.
    /// Explicit values keep saved references stable when the catalog grows.
    /// </summary>
    public enum TetherfallSettlementArtId
    {
        ThreadheartCore = 100,

        TimberFoundation = 200,
        StoneFoundation = 201,
        StraightWall = 210,
        WindowWall = 211,
        Doorway = 212,
        Gate = 213,
        Floor = 220,
        Stair = 221,
        Ramp = 222,
        PitchedRoof = 223,

        PactBed = 300,
        WayfinderBed = 301,
        StorageChest = 310,
        ResourcePallet = 311,

        Workbench = 400,
        Forge = 401,
        Cookpot = 402,
        Loom = 403,
        MedicineTable = 404,

        Planter = 500,
        FarmPlot = 501,
        WaterCollector = 502,
        FeedingTrough = 503,

        WatchPost = 600,
        LanternPost = 601,
        Fence = 602,
        Banner = 603
    }

    public enum TetherfallSettlementArtBudgetClass
    {
        Structural,
        ProductionStation,
        HeroCore
    }

    /// <summary>
    /// Immutable gameplay-facing description of one visual build piece.
    /// Footprints are metres, use a ground-contact pivot, and face +Z.
    /// </summary>
    public sealed class TetherfallSettlementArtDefinition
    {
        public TetherfallSettlementArtId ArtId { get; }
        public string StableId { get; }
        public string DisplayName { get; }
        public TetherfallSettlementArtBudgetClass BudgetClass { get; }
        public Vector3 Footprint { get; }
        public int MaxRendererCount { get; }
        public int MaxSharedMaterialCount { get; }
        public string PreferredResourceName { get; }
        public bool HasPreferredResource => !string.IsNullOrEmpty(PreferredResourceName);
        public string PreferredResourcePath => HasPreferredResource
            ? TetherfallQuaterniusAssetCatalog.ResourcePrefix + PreferredResourceName
            : string.Empty;
        public bool HasSnapSockets { get; }
        public bool HasWorkSocket { get; }
        public float WorkRadius { get; }

        internal TetherfallSettlementArtDefinition(
            TetherfallSettlementArtId artId,
            string stableId,
            string displayName,
            TetherfallSettlementArtBudgetClass budgetClass,
            Vector3 footprint,
            int maxRendererCount,
            int maxSharedMaterialCount,
            string preferredResourceName,
            bool hasSnapSockets,
            bool hasWorkSocket,
            float workRadius)
        {
            ArtId = artId;
            StableId = stableId;
            DisplayName = displayName;
            BudgetClass = budgetClass;
            Footprint = footprint;
            MaxRendererCount = maxRendererCount;
            MaxSharedMaterialCount = maxSharedMaterialCount;
            PreferredResourceName = preferredResourceName;
            HasSnapSockets = hasSnapSockets;
            HasWorkSocket = hasWorkSocket;
            WorkRadius = workRadius;
        }
    }

    /// <summary>
    /// Single source of truth for stable IDs, budgets, footprints, sockets, and optional source art.
    /// </summary>
    public static class TetherfallSettlementArtCatalog
    {
        public const int StructuralRendererBudget = 3;
        public const int StructuralMaterialBudget = 3;
        public const int StationRendererBudget = 8;
        public const int StationMaterialBudget = 6;
        public const int HeroRendererBudget = 16;
        public const int HeroMaterialBudget = 8;

        private static readonly TetherfallSettlementArtDefinition[] Definitions =
        {
            Hero(
                TetherfallSettlementArtId.ThreadheartCore,
                "alderwake.threadheart_core",
                "Threadheart settlement core",
                new Vector3(4f, 5.4f, 4f),
                "CommonTree_3",
                true,
                true,
                1.5f),

            Structural(
                TetherfallSettlementArtId.TimberFoundation,
                "alderwake.foundation.timber",
                "Timber foundation",
                new Vector3(4f, 0.5f, 4f),
                null,
                true),
            Structural(
                TetherfallSettlementArtId.StoneFoundation,
                "alderwake.foundation.stone",
                "Stone foundation",
                new Vector3(4f, 0.6f, 4f),
                null,
                true),
            Structural(
                TetherfallSettlementArtId.StraightWall,
                "alderwake.wall.straight",
                "Straight wall",
                new Vector3(4f, 3f, 0.4f),
                "Wall_Plaster_Straight",
                true),
            Structural(
                TetherfallSettlementArtId.WindowWall,
                "alderwake.wall.window",
                "Window wall",
                new Vector3(4f, 3f, 0.4f),
                "Wall_Plaster_Window_Wide_Round",
                true),
            Structural(
                TetherfallSettlementArtId.Doorway,
                "alderwake.wall.doorway",
                "Doorway",
                new Vector3(4f, 3f, 0.5f),
                "Wall_Plaster_Door_Round",
                true),
            Structural(
                TetherfallSettlementArtId.Gate,
                "alderwake.wall.gate",
                "Settlement gate",
                new Vector3(4f, 3.5f, 0.6f),
                "Wall_Arch",
                true),
            Structural(
                TetherfallSettlementArtId.Floor,
                "alderwake.floor",
                "Timber floor",
                new Vector3(4f, 0.2f, 4f),
                null,
                true),
            Structural(
                TetherfallSettlementArtId.Stair,
                "alderwake.stair",
                "Timber stair",
                new Vector3(4f, 0.9f, 4f),
                null,
                true),
            Structural(
                TetherfallSettlementArtId.Ramp,
                "alderwake.ramp",
                "Timber ramp",
                new Vector3(4f, 1f, 4f),
                null,
                true),
            Structural(
                TetherfallSettlementArtId.PitchedRoof,
                "alderwake.roof.pitched",
                "Pitched roof",
                new Vector3(4f, 1.4f, 4f),
                null,
                true),

            Station(
                TetherfallSettlementArtId.PactBed,
                "alderwake.bed.pact",
                "Pact bed",
                new Vector3(2.4f, 1.1f, 3f),
                "Bench",
                true,
                1.5f),
            Station(
                TetherfallSettlementArtId.WayfinderBed,
                "alderwake.bed.wayfinder",
                "Wayfinder bed",
                new Vector3(1.6f, 1.2f, 2.4f),
                "Bench",
                true,
                1.5f),
            Station(
                TetherfallSettlementArtId.StorageChest,
                "alderwake.storage.chest",
                "Storage chest",
                new Vector3(1.5f, 1.1f, 1f),
                "Crate_Wooden",
                true,
                1.5f),
            Station(
                TetherfallSettlementArtId.ResourcePallet,
                "alderwake.storage.pallet",
                "Resource pallet",
                new Vector3(2.2f, 1.3f, 1.8f),
                "FarmCrate_Empty",
                true,
                1.5f),

            Station(
                TetherfallSettlementArtId.Workbench,
                "alderwake.station.workbench",
                "Threadwright workbench",
                new Vector3(2.6f, 1.5f, 1.5f),
                "Workbench",
                true,
                1.5f),
            Station(
                TetherfallSettlementArtId.Forge,
                "alderwake.station.forge",
                "Copper repair forge",
                new Vector3(3f, 2.4f, 2.6f),
                "Workbench",
                true,
                1.5f),
            Station(
                TetherfallSettlementArtId.Cookpot,
                "alderwake.station.cookpot",
                "Pact cookpot",
                new Vector3(2.4f, 2f, 2.4f),
                "Cauldron",
                true,
                1.5f),
            Station(
                TetherfallSettlementArtId.Loom,
                "alderwake.station.loom",
                "Threadglass loom",
                new Vector3(2.8f, 2.4f, 1.8f),
                "Table_Large",
                true,
                1.5f),
            Station(
                TetherfallSettlementArtId.MedicineTable,
                "alderwake.station.medicine",
                "Medicine table",
                new Vector3(2.6f, 1.8f, 1.6f),
                "Table_Large",
                true,
                1.5f),

            Station(
                TetherfallSettlementArtId.Planter,
                "alderwake.growing.planter",
                "Moss planter",
                new Vector3(2.2f, 1f, 1.3f),
                "FarmCrate_Empty",
                true,
                1.5f),
            Station(
                TetherfallSettlementArtId.FarmPlot,
                "alderwake.growing.farm_plot",
                "Farm plot",
                new Vector3(4f, 0.8f, 4f),
                null,
                true,
                1.5f),
            Station(
                TetherfallSettlementArtId.WaterCollector,
                "alderwake.growing.water_collector",
                "Rainwater collector",
                new Vector3(2.2f, 2.4f, 2.2f),
                "Barrel",
                true,
                1.5f),
            Station(
                TetherfallSettlementArtId.FeedingTrough,
                "alderwake.growing.feeding_trough",
                "Pact feeding trough",
                new Vector3(2.8f, 1f, 1.4f),
                "Barrel_Holder",
                true,
                1.5f),

            Station(
                TetherfallSettlementArtId.WatchPost,
                "alderwake.utility.watch_post",
                "Watch post",
                new Vector3(3f, 4.5f, 3f),
                "Stall_Empty",
                true,
                1.5f),
            Structural(
                TetherfallSettlementArtId.LanternPost,
                "alderwake.utility.lantern_post",
                "Threadglass lantern post",
                new Vector3(0.8f, 3.2f, 0.8f),
                null,
                false),
            Structural(
                TetherfallSettlementArtId.Fence,
                "alderwake.utility.fence",
                "Timber fence",
                new Vector3(4f, 1.4f, 0.35f),
                null,
                true),
            Structural(
                TetherfallSettlementArtId.Banner,
                "alderwake.utility.banner",
                "Alderwake banner",
                new Vector3(1.4f, 3.4f, 0.5f),
                null,
                false)
        };

        private static readonly Dictionary<TetherfallSettlementArtId, TetherfallSettlementArtDefinition>
            DefinitionsById;
        private static readonly Dictionary<string, TetherfallSettlementArtDefinition>
            DefinitionsByStableId;

        static TetherfallSettlementArtCatalog()
        {
            DefinitionsById =
                new Dictionary<TetherfallSettlementArtId, TetherfallSettlementArtDefinition>();
            DefinitionsByStableId =
                new Dictionary<string, TetherfallSettlementArtDefinition>(StringComparer.Ordinal);

            foreach (var definition in Definitions)
            {
                DefinitionsById.Add(definition.ArtId, definition);
                DefinitionsByStableId.Add(definition.StableId, definition);
            }
        }

        public static IReadOnlyList<TetherfallSettlementArtDefinition> All => Definitions;

        public static TetherfallSettlementArtDefinition Get(TetherfallSettlementArtId artId)
        {
            if (!DefinitionsById.TryGetValue(artId, out var definition))
            {
                throw new ArgumentOutOfRangeException(nameof(artId), artId, "Unknown settlement art ID.");
            }

            return definition;
        }

        public static bool TryGet(
            string stableId,
            out TetherfallSettlementArtDefinition definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(stableId) &&
                   DefinitionsByStableId.TryGetValue(stableId, out definition);
        }

        public static bool TryParse(string stableId, out TetherfallSettlementArtId artId)
        {
            if (TryGet(stableId, out var definition))
            {
                artId = definition.ArtId;
                return true;
            }

            artId = default;
            return false;
        }

        public static string GetStableId(TetherfallSettlementArtId artId)
        {
            return Get(artId).StableId;
        }

        private static TetherfallSettlementArtDefinition Structural(
            TetherfallSettlementArtId artId,
            string stableId,
            string displayName,
            Vector3 footprint,
            string preferredResourceName,
            bool hasSnapSockets)
        {
            return new TetherfallSettlementArtDefinition(
                artId,
                stableId,
                displayName,
                TetherfallSettlementArtBudgetClass.Structural,
                footprint,
                StructuralRendererBudget,
                StructuralMaterialBudget,
                preferredResourceName,
                hasSnapSockets,
                false,
                0f);
        }

        private static TetherfallSettlementArtDefinition Station(
            TetherfallSettlementArtId artId,
            string stableId,
            string displayName,
            Vector3 footprint,
            string preferredResourceName,
            bool hasWorkSocket,
            float workRadius)
        {
            return new TetherfallSettlementArtDefinition(
                artId,
                stableId,
                displayName,
                TetherfallSettlementArtBudgetClass.ProductionStation,
                footprint,
                StationRendererBudget,
                StationMaterialBudget,
                preferredResourceName,
                false,
                hasWorkSocket,
                workRadius);
        }

        private static TetherfallSettlementArtDefinition Hero(
            TetherfallSettlementArtId artId,
            string stableId,
            string displayName,
            Vector3 footprint,
            string preferredResourceName,
            bool hasSnapSockets,
            bool hasWorkSocket,
            float workRadius)
        {
            return new TetherfallSettlementArtDefinition(
                artId,
                stableId,
                displayName,
                TetherfallSettlementArtBudgetClass.HeroCore,
                footprint,
                HeroRendererBudget,
                HeroMaterialBudget,
                preferredResourceName,
                hasSnapSockets,
                hasWorkSocket,
                workRadius);
        }
    }

    /// <summary>
    /// Build behavior for normal use, explicit refreshes, and deterministic fallback verification.
    /// </summary>
    public readonly struct TetherfallSettlementArtBuildOptions
    {
        public bool ForceProceduralFallback { get; }
        public bool RebuildExisting { get; }

        public static TetherfallSettlementArtBuildOptions Default =>
            new TetherfallSettlementArtBuildOptions(false, false);

        public static TetherfallSettlementArtBuildOptions Rebuild =>
            new TetherfallSettlementArtBuildOptions(false, true);

        public static TetherfallSettlementArtBuildOptions ProceduralFallback =>
            new TetherfallSettlementArtBuildOptions(true, true);

        public TetherfallSettlementArtBuildOptions(
            bool forceProceduralFallback,
            bool rebuildExisting)
        {
            ForceProceduralFallback = forceProceduralFallback;
            RebuildExisting = rebuildExisting;
        }
    }

    /// <summary>
    /// Collider-free piece metadata. This component has no per-frame callbacks.
    /// Runtime construction code may put deliberate gameplay colliders on the supplied parent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TetherfallSettlementArtPiece : MonoBehaviour
    {
        [SerializeField] private TetherfallSettlementArtId artId;
        [SerializeField] private string stableId;
        [SerializeField] private int rendererCount;
        [SerializeField] private int sharedMaterialCount;
        [SerializeField] private bool usedPreferredResource;
        [SerializeField] private Bounds localVisualBounds;

        public TetherfallSettlementArtId ArtId => artId;
        public string StableId => stableId;
        public int RendererCount => rendererCount;
        public int SharedMaterialCount => sharedMaterialCount;
        public bool UsedPreferredResource => usedPreferredResource;
        public bool UsedProceduralFallback => !usedPreferredResource;
        public Bounds LocalVisualBounds => localVisualBounds;
        public string CullingGroupKey => "alderwake.threadheart";
        public float NearDistance => TetherfallSettlementPresentationController.NearDistance;
        public float CullDistance => TetherfallSettlementPresentationController.CullDistance;

        internal void Configure(
            TetherfallSettlementArtDefinition definition,
            bool usedResource,
            int renderers,
            int materials,
            Bounds bounds)
        {
            artId = definition.ArtId;
            stableId = definition.StableId;
            usedPreferredResource = usedResource;
            rendererCount = renderers;
            sharedMaterialCount = materials;
            localVisualBounds = bounds;
        }
    }

    /// <summary>
    /// One optional CullingGroup for a complete settlement hierarchy. Call RebuildGroups after
    /// placing a batch. It owns no lights and has no Update/LateUpdate/FixedUpdate loop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TetherfallSettlementPresentationController : MonoBehaviour
    {
        public const float NearDistance = 70f;
        public const float CullDistance = 110f;

        private CullingGroup cullingGroup;
        private Renderer[][] rendererGroups = Array.Empty<Renderer[]>();
        private Camera targetCamera;

        public int RegisteredPieceCount { get; private set; }
        public int RegisteredRendererCount { get; private set; }
        public bool IsCullingActive => cullingGroup != null;
        public Camera TargetCamera => targetCamera;

        public static TetherfallSettlementPresentationController Ensure(
            Transform settlementParent,
            Camera camera = null)
        {
            if (settlementParent == null)
            {
                throw new ArgumentNullException(nameof(settlementParent));
            }

            var controller =
                settlementParent.GetComponent<TetherfallSettlementPresentationController>();
            if (controller == null)
            {
                controller =
                    settlementParent.gameObject.AddComponent<TetherfallSettlementPresentationController>();
            }

            controller.RebuildGroups(camera);
            return controller;
        }

        public void RebuildGroups(Camera camera = null)
        {
            DisposeCullingGroup();
            targetCamera = camera != null ? camera : targetCamera;

            var pieces = GetComponentsInChildren<TetherfallSettlementArtPiece>(true);
            RegisteredPieceCount = pieces.Length;
            RegisteredRendererCount = 0;
            rendererGroups = new Renderer[pieces.Length][];
            var spheres = new BoundingSphere[pieces.Length];

            for (var index = 0; index < pieces.Length; index++)
            {
                var renderers = pieces[index].GetComponentsInChildren<Renderer>(true);
                rendererGroups[index] = renderers;
                RegisteredRendererCount += renderers.Length;
                spheres[index] = CalculateBoundingSphere(renderers, pieces[index].transform.position);
            }

            if (targetCamera == null || pieces.Length == 0)
            {
                SetAllVisible(true);
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

        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
            RebuildGroups(camera);
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

            foreach (var visualRenderer in rendererGroups[index])
            {
                if (visualRenderer != null)
                {
                    visualRenderer.enabled = visible;
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

        private static BoundingSphere CalculateBoundingSphere(
            IReadOnlyList<Renderer> renderers,
            Vector3 fallbackCenter)
        {
            var hasBounds = false;
            var combined = new Bounds(fallbackCenter, Vector3.one);
            foreach (var visualRenderer in renderers)
            {
                if (visualRenderer == null || !IsFinite(visualRenderer.bounds))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combined = visualRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(visualRenderer.bounds);
                }
            }

            return hasBounds
                ? new BoundingSphere(combined.center, Mathf.Max(0.5f, combined.extents.magnitude))
                : new BoundingSphere(fallbackCenter, 0.5f);
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
    }

    /// <summary>
    /// Runtime presentation API for the first production settlement pack.
    /// Every build is deterministic, collider-free, material-cached, and rooted under the caller.
    /// </summary>
    public static class TetherfallSettlementArt
    {
        public const string RootPrefix = "Settlement Art · ";
        public const string VisualRootName = "Visuals";
        public const string SocketRootName = "Sockets";

        private static readonly Dictionary<SettlementMaterialKey, Material> SharedMaterials =
            new Dictionary<SettlementMaterialKey, Material>();

        public static int CachedMaterialCount => SharedMaterials.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetMaterialCache()
        {
            foreach (var material in SharedMaterials.Values)
            {
                DestroyObject(material);
            }

            SharedMaterials.Clear();
        }

        public static GameObject Build(
            TetherfallSettlementArtId artId,
            Transform parent)
        {
            return Build(artId, parent, TetherfallSettlementArtBuildOptions.Default);
        }

        public static GameObject Build(
            string stableId,
            Transform parent)
        {
            if (!TetherfallSettlementArtCatalog.TryParse(stableId, out var artId))
            {
                throw new ArgumentException(
                    $"Unknown settlement art stable ID '{stableId}'.",
                    nameof(stableId));
            }

            return Build(artId, parent, TetherfallSettlementArtBuildOptions.Default);
        }

        public static bool TryBuild(
            string stableId,
            Transform parent,
            out GameObject visualRoot)
        {
            visualRoot = null;
            if (!TetherfallSettlementArtCatalog.TryParse(stableId, out var artId))
            {
                return false;
            }

            visualRoot = Build(artId, parent, TetherfallSettlementArtBuildOptions.Default);
            return visualRoot != null;
        }

        public static GameObject Rebuild(
            TetherfallSettlementArtId artId,
            Transform parent)
        {
            return Build(artId, parent, TetherfallSettlementArtBuildOptions.Rebuild);
        }

        public static GameObject BuildProceduralFallback(
            TetherfallSettlementArtId artId,
            Transform parent)
        {
            return Build(artId, parent, TetherfallSettlementArtBuildOptions.ProceduralFallback);
        }

        public static GameObject Build(
            TetherfallSettlementArtId artId,
            Transform parent,
            TetherfallSettlementArtBuildOptions options)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var definition = TetherfallSettlementArtCatalog.Get(artId);
            var piece = FindDirectPiece(parent, artId);
            if (piece != null && !options.RebuildExisting)
            {
                return piece.gameObject;
            }

            var createdRoot = piece == null;
            if (createdRoot)
            {
                var rootObject = new GameObject(RootPrefix + definition.StableId);
                rootObject.transform.SetParent(parent, false);
                piece = rootObject.AddComponent<TetherfallSettlementArtPiece>();
            }
            else
            {
                ClearChildren(piece.transform);
                piece.gameObject.name = RootPrefix + definition.StableId;
            }

            try
            {
                var visualRoot = new GameObject(VisualRootName).transform;
                visualRoot.SetParent(piece.transform, false);
                var builder = new PieceBuilder(
                    definition,
                    visualRoot,
                    options.ForceProceduralFallback);
                BuildVisual(builder);
                builder.ValidateFinalBudget();
                BuildSockets(definition, piece.transform);

                piece.Configure(
                    definition,
                    builder.UsedPreferredResource,
                    builder.RendererCount,
                    builder.SharedMaterialCount,
                    CalculateLocalBounds(piece.transform, builder.Renderers));
                return piece.gameObject;
            }
            catch
            {
                if (createdRoot)
                {
                    DestroyObject(piece.gameObject);
                }
                else
                {
                    ClearChildren(piece.transform);
                }

                throw;
            }
        }

        public static bool Clear(
            Transform parent,
            TetherfallSettlementArtId artId)
        {
            if (parent == null)
            {
                return false;
            }

            var removed = false;
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);
                var piece = child.GetComponent<TetherfallSettlementArtPiece>();
                if (piece == null || piece.ArtId != artId)
                {
                    continue;
                }

                removed = true;
                DestroyObject(child.gameObject);
            }

            return removed;
        }

        public static bool Clear(Transform parent, string stableId)
        {
            return TetherfallSettlementArtCatalog.TryParse(stableId, out var artId) &&
                   Clear(parent, artId);
        }

        public static int ClearAll(Transform parent)
        {
            if (parent == null)
            {
                return 0;
            }

            var removed = 0;
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);
                if (child.GetComponent<TetherfallSettlementArtPiece>() == null)
                {
                    continue;
                }

                removed++;
                DestroyObject(child.gameObject);
            }

            return removed;
        }

        public static TetherfallSettlementPresentationController EnsurePresentationController(
            Transform settlementParent,
            Camera camera = null)
        {
            return TetherfallSettlementPresentationController.Ensure(settlementParent, camera);
        }

        private static void BuildVisual(PieceBuilder builder)
        {
            switch (builder.Definition.ArtId)
            {
                case TetherfallSettlementArtId.ThreadheartCore:
                    BuildThreadheartCore(builder);
                    break;
                case TetherfallSettlementArtId.TimberFoundation:
                    BuildTimberFoundation(builder);
                    break;
                case TetherfallSettlementArtId.StoneFoundation:
                    BuildStoneFoundation(builder);
                    break;
                case TetherfallSettlementArtId.StraightWall:
                    BuildStraightWall(builder);
                    break;
                case TetherfallSettlementArtId.WindowWall:
                    BuildWindowWall(builder);
                    break;
                case TetherfallSettlementArtId.Doorway:
                    BuildDoorway(builder);
                    break;
                case TetherfallSettlementArtId.Gate:
                    BuildGate(builder);
                    break;
                case TetherfallSettlementArtId.Floor:
                    BuildFloor(builder);
                    break;
                case TetherfallSettlementArtId.Stair:
                    BuildStair(builder);
                    break;
                case TetherfallSettlementArtId.Ramp:
                    BuildRamp(builder);
                    break;
                case TetherfallSettlementArtId.PitchedRoof:
                    BuildPitchedRoof(builder);
                    break;
                case TetherfallSettlementArtId.PactBed:
                    BuildPactBed(builder);
                    break;
                case TetherfallSettlementArtId.WayfinderBed:
                    BuildWayfinderBed(builder);
                    break;
                case TetherfallSettlementArtId.StorageChest:
                    BuildStorageChest(builder);
                    break;
                case TetherfallSettlementArtId.ResourcePallet:
                    BuildResourcePallet(builder);
                    break;
                case TetherfallSettlementArtId.Workbench:
                    BuildWorkbench(builder);
                    break;
                case TetherfallSettlementArtId.Forge:
                    BuildForge(builder);
                    break;
                case TetherfallSettlementArtId.Cookpot:
                    BuildCookpot(builder);
                    break;
                case TetherfallSettlementArtId.Loom:
                    BuildLoom(builder);
                    break;
                case TetherfallSettlementArtId.MedicineTable:
                    BuildMedicineTable(builder);
                    break;
                case TetherfallSettlementArtId.Planter:
                    BuildPlanter(builder);
                    break;
                case TetherfallSettlementArtId.FarmPlot:
                    BuildFarmPlot(builder);
                    break;
                case TetherfallSettlementArtId.WaterCollector:
                    BuildWaterCollector(builder);
                    break;
                case TetherfallSettlementArtId.FeedingTrough:
                    BuildFeedingTrough(builder);
                    break;
                case TetherfallSettlementArtId.WatchPost:
                    BuildWatchPost(builder);
                    break;
                case TetherfallSettlementArtId.LanternPost:
                    BuildLanternPost(builder);
                    break;
                case TetherfallSettlementArtId.Fence:
                    BuildFence(builder);
                    break;
                case TetherfallSettlementArtId.Banner:
                    BuildBanner(builder);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(builder.Definition.ArtId),
                        builder.Definition.ArtId,
                        "Settlement art ID has no visual recipe.");
            }
        }

        private static void BuildThreadheartCore(PieceBuilder builder)
        {
            builder.AddCylinder(
                "Warm-stone heart plinth",
                new Vector3(0f, 0.18f, 0f),
                new Vector3(2.1f, 0.18f, 2.1f),
                Quaternion.identity,
                SettlementMaterialKey.Stone);

            if (!builder.TryPreferredResource(
                    new Vector3(0f, 0.36f, 0f),
                    Quaternion.identity,
                    Vector3.one * 0.78f,
                    8,
                    3))
            {
                builder.AddCylinder(
                    "Threadheart trunk",
                    new Vector3(0f, 2.05f, 0f),
                    new Vector3(0.43f, 1.7f, 0.43f),
                    Quaternion.Euler(0f, 12f, -3f),
                    SettlementMaterialKey.Timber);
                builder.AddSphere(
                    "Threadheart crown",
                    new Vector3(0f, 4.45f, 0f),
                    new Vector3(1.45f, 0.9f, 1.35f),
                    Quaternion.Euler(0f, 18f, 0f),
                    SettlementMaterialKey.Moss);
            }

            var bracePositions = new[]
            {
                new Vector3(-1.42f, 1.35f, 0f),
                new Vector3(1.42f, 1.35f, 0f),
                new Vector3(0f, 1.35f, -1.42f),
                new Vector3(0f, 1.35f, 1.42f)
            };
            for (var index = 0; index < bracePositions.Length; index++)
            {
                builder.AddBox(
                    "Weathered timber heart brace",
                    bracePositions[index],
                    new Vector3(0.18f, 2.25f, 0.18f),
                    Quaternion.Euler(
                        index < 2 ? 0f : (index == 2 ? 90f : -90f),
                        index * 90f,
                        index % 2 == 0 ? -8f : 8f),
                    SettlementMaterialKey.Timber);
            }

            builder.AddCylinder(
                "Copper civic repair collar",
                new Vector3(0f, 0.52f, 0f),
                new Vector3(1.62f, 0.07f, 1.62f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
            builder.AddSphere(
                "Restrained Threadglass heart",
                new Vector3(0f, 1.18f, 0f),
                Vector3.one * 0.34f,
                Quaternion.identity,
                SettlementMaterialKey.Teal);
            builder.AddCylinder(
                "Moss apron",
                new Vector3(0f, 0.39f, 0f),
                new Vector3(1.82f, 0.035f, 1.82f),
                Quaternion.identity,
                SettlementMaterialKey.Moss);
        }

        private static void BuildTimberFoundation(PieceBuilder builder)
        {
            builder.AddBox(
                "Weathered timber deck",
                new Vector3(0f, 0.24f, 0f),
                new Vector3(4f, 0.32f, 4f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Warm-stone footing",
                new Vector3(0f, 0.055f, 0f),
                new Vector3(4.2f, 0.11f, 4.2f),
                Quaternion.identity,
                SettlementMaterialKey.Stone);
            builder.AddBox(
                "Copper center tie",
                new Vector3(0f, 0.425f, 0f),
                new Vector3(3.6f, 0.05f, 0.12f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
        }

        private static void BuildStoneFoundation(PieceBuilder builder)
        {
            builder.AddBox(
                "Warm-stone foundation",
                new Vector3(0f, 0.24f, 0f),
                new Vector3(4f, 0.48f, 4f),
                Quaternion.identity,
                SettlementMaterialKey.Stone);
            builder.AddBox(
                "Timber north sill",
                new Vector3(0f, 0.53f, 1.65f),
                new Vector3(3.8f, 0.18f, 0.18f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Timber south sill",
                new Vector3(0f, 0.53f, -1.65f),
                new Vector3(3.8f, 0.18f, 0.18f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
        }

        private static void BuildStraightWall(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    1,
                    1))
            {
                builder.AddBox(
                    "Warm-stone wall infill",
                    new Vector3(0f, 1.5f, 0f),
                    new Vector3(4f, 3f, 0.24f),
                    Quaternion.identity,
                    SettlementMaterialKey.Stone);
            }

            builder.AddBox(
                "Weathered timber wall header",
                new Vector3(0f, 2.83f, 0f),
                new Vector3(4f, 0.28f, 0.34f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Copper repair seam",
                new Vector3(-1.62f, 1.5f, -0.15f),
                new Vector3(0.08f, 2.35f, 0.045f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
        }

        private static void BuildWindowWall(PieceBuilder builder)
        {
            if (builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    1,
                    1))
            {
                builder.AddBox(
                    "Timber window sill",
                    new Vector3(0f, 1.02f, -0.16f),
                    new Vector3(2.2f, 0.18f, 0.12f),
                    Quaternion.identity,
                    SettlementMaterialKey.Timber);
                builder.AddBox(
                    "Threadglass window",
                    new Vector3(0f, 1.72f, 0f),
                    new Vector3(1.5f, 1.15f, 0.055f),
                    Quaternion.identity,
                    SettlementMaterialKey.Teal);
                return;
            }

            builder.AddBox(
                "Left stone window pier",
                new Vector3(-1.47f, 1.5f, 0f),
                new Vector3(1.06f, 3f, 0.26f),
                Quaternion.identity,
                SettlementMaterialKey.Stone);
            builder.AddBox(
                "Right stone window pier",
                new Vector3(1.47f, 1.5f, 0f),
                new Vector3(1.06f, 3f, 0.26f),
                Quaternion.identity,
                SettlementMaterialKey.Stone);
            builder.AddBox(
                "Timber window lintel",
                new Vector3(0f, 2.83f, 0f),
                new Vector3(4f, 0.28f, 0.34f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
        }

        private static void BuildDoorway(PieceBuilder builder)
        {
            if (builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    1,
                    1))
            {
                builder.AddBox(
                    "Copper doorway lintel",
                    new Vector3(0f, 2.65f, -0.16f),
                    new Vector3(2.1f, 0.13f, 0.08f),
                    Quaternion.identity,
                    SettlementMaterialKey.Copper);
                builder.AddBox(
                    "Threadglass threshold",
                    new Vector3(0f, 0.035f, 0f),
                    new Vector3(1.55f, 0.07f, 0.42f),
                    Quaternion.identity,
                    SettlementMaterialKey.Teal);
                return;
            }

            builder.AddBox(
                "Left timber doorway post",
                new Vector3(-1.34f, 1.5f, 0f),
                new Vector3(0.42f, 3f, 0.42f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Right timber doorway post",
                new Vector3(1.34f, 1.5f, 0f),
                new Vector3(0.42f, 3f, 0.42f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Copper doorway lintel",
                new Vector3(0f, 2.78f, 0f),
                new Vector3(3.1f, 0.28f, 0.48f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
        }

        private static void BuildGate(PieceBuilder builder)
        {
            if (builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    1,
                    1))
            {
                builder.AddBox(
                    "Copper gate crown",
                    new Vector3(0f, 3.18f, -0.18f),
                    new Vector3(3.25f, 0.16f, 0.1f),
                    Quaternion.identity,
                    SettlementMaterialKey.Copper);
                builder.AddBox(
                    "Threadglass route threshold",
                    new Vector3(0f, 0.04f, 0f),
                    new Vector3(2.75f, 0.08f, 0.5f),
                    Quaternion.identity,
                    SettlementMaterialKey.Teal);
                return;
            }

            builder.AddBox(
                "Left gate post",
                new Vector3(-1.68f, 1.65f, 0f),
                new Vector3(0.48f, 3.3f, 0.5f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Right gate post",
                new Vector3(1.68f, 1.65f, 0f),
                new Vector3(0.48f, 3.3f, 0.5f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Copper gate beam",
                new Vector3(0f, 3.16f, 0f),
                new Vector3(3.6f, 0.3f, 0.55f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
        }

        private static void BuildFloor(PieceBuilder builder)
        {
            for (var index = 0; index < 3; index++)
            {
                builder.AddBox(
                    "Grouped timber floor planks",
                    new Vector3((index - 1) * 1.32f, 0.1f, 0f),
                    new Vector3(1.25f, 0.2f, 4f),
                    Quaternion.identity,
                    index == 1 ? SettlementMaterialKey.Copper : SettlementMaterialKey.Timber);
            }
        }

        private static void BuildStair(PieceBuilder builder)
        {
            for (var index = 0; index < 3; index++)
            {
                var height = 0.28f * (index + 1);
                builder.AddBox(
                    "Broad timber stair tread",
                    new Vector3(0f, height * 0.5f, -1.25f + index * 1.25f),
                    new Vector3(4f, height, 1.18f),
                    Quaternion.identity,
                    index == 2 ? SettlementMaterialKey.Copper : SettlementMaterialKey.Timber);
            }
        }

        private static void BuildRamp(PieceBuilder builder)
        {
            var rotation = Quaternion.Euler(-11f, 0f, 0f);
            builder.AddBox(
                "Timber ramp deck",
                new Vector3(0f, 0.45f, 0f),
                new Vector3(3.7f, 0.2f, 4.05f),
                rotation,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Left copper ramp rail",
                new Vector3(-1.84f, 0.72f, 0f),
                new Vector3(0.1f, 0.18f, 4.05f),
                rotation,
                SettlementMaterialKey.Copper);
            builder.AddBox(
                "Right copper ramp rail",
                new Vector3(1.84f, 0.72f, 0f),
                new Vector3(0.1f, 0.18f, 4.05f),
                rotation,
                SettlementMaterialKey.Copper);
        }

        private static void BuildPitchedRoof(PieceBuilder builder)
        {
            builder.AddBox(
                "West moss-canvas roof plane",
                new Vector3(-0.92f, 0.62f, 0f),
                new Vector3(2.25f, 0.16f, 4.15f),
                Quaternion.Euler(0f, 0f, -28f),
                SettlementMaterialKey.Canvas);
            builder.AddBox(
                "East moss-canvas roof plane",
                new Vector3(0.92f, 0.62f, 0f),
                new Vector3(2.25f, 0.16f, 4.15f),
                Quaternion.Euler(0f, 0f, 28f),
                SettlementMaterialKey.Moss);
            builder.AddBox(
                "Copper roof ridge",
                new Vector3(0f, 1.22f, 0f),
                new Vector3(0.12f, 0.14f, 4.25f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
        }

        private static void BuildPactBed(PieceBuilder builder)
        {
            var usedSource = builder.TryPreferredResource(
                new Vector3(0f, 0f, -0.25f),
                Quaternion.Euler(0f, 90f, 0f),
                Vector3.one,
                5,
                3);
            if (!usedSource)
            {
                builder.AddBox(
                    "Low Pact bed frame",
                    new Vector3(0f, 0.28f, 0f),
                    new Vector3(2.2f, 0.35f, 2.75f),
                    Quaternion.identity,
                    SettlementMaterialKey.Timber);
            }

            builder.AddBox(
                "Wide moss bedding",
                new Vector3(0f, 0.57f, 0f),
                new Vector3(2.05f, 0.28f, 2.55f),
                Quaternion.identity,
                SettlementMaterialKey.Moss);
            builder.AddBox(
                "Canvas shelter roll",
                new Vector3(0f, 0.82f, -0.92f),
                new Vector3(1.85f, 0.32f, 0.38f),
                Quaternion.identity,
                SettlementMaterialKey.Canvas);
            builder.AddSphere(
                "Pact Threadglass knot",
                new Vector3(0f, 0.8f, 1.02f),
                Vector3.one * 0.18f,
                Quaternion.identity,
                SettlementMaterialKey.Teal);
        }

        private static void BuildWayfinderBed(PieceBuilder builder)
        {
            var usedSource = builder.TryPreferredResource(
                new Vector3(0f, 0f, -0.2f),
                Quaternion.Euler(0f, 90f, 0f),
                Vector3.one * 0.9f,
                5,
                3);
            if (!usedSource)
            {
                builder.AddBox(
                    "Wayfinder bed frame",
                    new Vector3(0f, 0.3f, 0f),
                    new Vector3(1.5f, 0.38f, 2.25f),
                    Quaternion.identity,
                    SettlementMaterialKey.Timber);
            }

            builder.AddBox(
                "Wayfinder canvas bedroll",
                new Vector3(0f, 0.58f, 0f),
                new Vector3(1.38f, 0.25f, 2.08f),
                Quaternion.identity,
                SettlementMaterialKey.Canvas);
            builder.AddBox(
                "Copper pack rail",
                new Vector3(0f, 0.84f, -0.88f),
                new Vector3(1.42f, 0.12f, 0.16f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
            builder.AddSphere(
                "Threadglass route token",
                new Vector3(0f, 0.78f, 0.84f),
                Vector3.one * 0.16f,
                Quaternion.identity,
                SettlementMaterialKey.Teal);
        }

        private static void BuildStorageChest(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    5,
                    4))
            {
                builder.AddBox(
                    "Timber storage chest",
                    new Vector3(0f, 0.52f, 0f),
                    new Vector3(1.4f, 1.02f, 0.92f),
                    Quaternion.identity,
                    SettlementMaterialKey.Timber);
            }

            builder.AddBox(
                "Copper chest band",
                new Vector3(0f, 0.55f, -0.48f),
                new Vector3(0.12f, 0.78f, 0.08f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
            builder.AddSphere(
                "Threadglass chest latch",
                new Vector3(0f, 0.6f, -0.55f),
                Vector3.one * 0.14f,
                Quaternion.identity,
                SettlementMaterialKey.Teal);
        }

        private static void BuildResourcePallet(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    4,
                    3))
            {
                builder.AddBox(
                    "Timber resource pallet",
                    new Vector3(0f, 0.15f, 0f),
                    new Vector3(2.1f, 0.3f, 1.7f),
                    Quaternion.identity,
                    SettlementMaterialKey.Timber);
            }

            builder.AddSphere(
                "Canvas supply bundle left",
                new Vector3(-0.48f, 0.72f, 0.05f),
                new Vector3(0.65f, 0.62f, 0.55f),
                Quaternion.Euler(0f, 18f, -8f),
                SettlementMaterialKey.Canvas);
            builder.AddSphere(
                "Canvas supply bundle right",
                new Vector3(0.48f, 0.67f, -0.08f),
                new Vector3(0.6f, 0.56f, 0.62f),
                Quaternion.Euler(0f, -15f, 7f),
                SettlementMaterialKey.Canvas);
            builder.AddBox(
                "Copper route manifest",
                new Vector3(0f, 1.12f, -0.56f),
                new Vector3(0.52f, 0.4f, 0.05f),
                Quaternion.Euler(5f, 0f, 0f),
                SettlementMaterialKey.Copper);
        }

        private static void BuildWorkbench(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    5,
                    3))
            {
                builder.AddBox(
                    "Timber workbench body",
                    new Vector3(0f, 0.72f, 0f),
                    new Vector3(2.5f, 0.22f, 1.35f),
                    Quaternion.identity,
                    SettlementMaterialKey.Timber);
            }

            builder.AddBox(
                "Copper tool rail",
                new Vector3(0f, 1.17f, 0.52f),
                new Vector3(2.1f, 0.12f, 0.12f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
            builder.AddBox(
                "Timber cutting block",
                new Vector3(-0.72f, 1.02f, 0f),
                new Vector3(0.62f, 0.25f, 0.62f),
                Quaternion.Euler(0f, 8f, 0f),
                SettlementMaterialKey.Timber);
            builder.AddSphere(
                "Threadglass work index",
                new Vector3(0.84f, 1.04f, -0.35f),
                Vector3.one * 0.15f,
                Quaternion.identity,
                SettlementMaterialKey.Teal);
        }

        private static void BuildForge(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    new Vector3(-0.45f, 0f, 0.2f),
                    Quaternion.identity,
                    Vector3.one * 0.9f,
                    4,
                    2))
            {
                builder.AddBox(
                    "Forge timber service deck",
                    new Vector3(-0.45f, 0.48f, 0.15f),
                    new Vector3(1.7f, 0.2f, 1.45f),
                    Quaternion.identity,
                    SettlementMaterialKey.Timber);
            }

            builder.AddCylinder(
                "Warm-stone forge hearth",
                new Vector3(0.78f, 0.45f, 0.08f),
                new Vector3(0.72f, 0.45f, 0.72f),
                Quaternion.identity,
                SettlementMaterialKey.Stone);
            builder.AddBox(
                "Copper anvil body",
                new Vector3(-0.45f, 1.06f, -0.18f),
                new Vector3(0.88f, 0.3f, 0.4f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
            builder.AddBox(
                "Timber forge hood frame",
                new Vector3(0.78f, 1.48f, 0.08f),
                new Vector3(0.18f, 1.65f, 0.18f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddSphere(
                "Threadglass temperature seam",
                new Vector3(0.78f, 0.92f, -0.58f),
                Vector3.one * 0.17f,
                Quaternion.identity,
                SettlementMaterialKey.Teal);
        }

        private static void BuildCookpot(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    new Vector3(0f, 0.36f, 0f),
                    Quaternion.identity,
                    Vector3.one,
                    3,
                    3))
            {
                builder.AddCylinder(
                    "Warm-stone cook bowl",
                    new Vector3(0f, 0.62f, 0f),
                    new Vector3(0.72f, 0.38f, 0.72f),
                    Quaternion.identity,
                    SettlementMaterialKey.Stone);
            }

            for (var index = 0; index < 3; index++)
            {
                var angle = index * 120f;
                var radians = angle * Mathf.Deg2Rad;
                builder.AddBox(
                    "Timber cookpot tripod leg",
                    new Vector3(Mathf.Cos(radians) * 0.72f, 0.9f, Mathf.Sin(radians) * 0.72f),
                    new Vector3(0.12f, 1.75f, 0.12f),
                    Quaternion.Euler(10f, -angle, index % 2 == 0 ? -13f : 13f),
                    SettlementMaterialKey.Timber);
            }

            builder.AddCylinder(
                "Copper cookpot collar",
                new Vector3(0f, 1.58f, 0f),
                new Vector3(0.92f, 0.055f, 0.92f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
            builder.AddSphere(
                "Threadglass meal marker",
                new Vector3(0f, 1.72f, 0f),
                Vector3.one * 0.16f,
                Quaternion.identity,
                SettlementMaterialKey.Teal);
        }

        private static void BuildLoom(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    new Vector3(0f, 0f, 0.25f),
                    Quaternion.identity,
                    Vector3.one * 0.78f,
                    3,
                    3))
            {
                builder.AddBox(
                    "Timber loom bench",
                    new Vector3(0f, 0.55f, 0.3f),
                    new Vector3(2.5f, 0.18f, 1.15f),
                    Quaternion.identity,
                    SettlementMaterialKey.Timber);
            }

            builder.AddBox(
                "Left loom post",
                new Vector3(-1.05f, 1.25f, 0f),
                new Vector3(0.16f, 2.25f, 0.16f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Right loom post",
                new Vector3(1.05f, 1.25f, 0f),
                new Vector3(0.16f, 2.25f, 0.16f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Loom crown beam",
                new Vector3(0f, 2.27f, 0f),
                new Vector3(2.25f, 0.16f, 0.18f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Restrained Threadglass warp",
                new Vector3(0f, 1.5f, 0f),
                new Vector3(1.7f, 1.15f, 0.045f),
                Quaternion.identity,
                SettlementMaterialKey.Teal);
            builder.AddBox(
                "Copper shuttle",
                new Vector3(0.36f, 1.05f, -0.1f),
                new Vector3(0.82f, 0.1f, 0.1f),
                Quaternion.Euler(0f, 0f, -8f),
                SettlementMaterialKey.Copper);
        }

        private static void BuildMedicineTable(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one * 0.8f,
                    3,
                    1))
            {
                builder.AddBox(
                    "Timber medicine table",
                    new Vector3(0f, 0.75f, 0f),
                    new Vector3(2.45f, 0.18f, 1.45f),
                    Quaternion.identity,
                    SettlementMaterialKey.Timber);
            }

            builder.AddBox(
                "Clean canvas workcloth",
                new Vector3(0f, 1.02f, 0f),
                new Vector3(2.1f, 0.06f, 1.2f),
                Quaternion.identity,
                SettlementMaterialKey.Canvas);
            builder.AddSphere(
                "Threadglass tincture",
                new Vector3(-0.65f, 1.25f, 0.12f),
                new Vector3(0.18f, 0.28f, 0.18f),
                Quaternion.identity,
                SettlementMaterialKey.Teal);
            builder.AddSphere(
                "Moss poultice jar",
                new Vector3(0f, 1.2f, 0.15f),
                new Vector3(0.2f, 0.22f, 0.2f),
                Quaternion.identity,
                SettlementMaterialKey.Moss);
            builder.AddSphere(
                "Copper measure",
                new Vector3(0.62f, 1.18f, 0.08f),
                new Vector3(0.17f, 0.2f, 0.17f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
            builder.AddBox(
                "Medicine shelf",
                new Vector3(0f, 0.48f, 0.52f),
                new Vector3(2.05f, 0.12f, 0.18f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
        }

        private static void BuildPlanter(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    4,
                    3))
            {
                builder.AddBox(
                    "Timber planter box",
                    new Vector3(0f, 0.42f, 0f),
                    new Vector3(2.1f, 0.82f, 1.25f),
                    Quaternion.identity,
                    SettlementMaterialKey.Timber);
            }

            builder.AddBox(
                "Planter soil",
                new Vector3(0f, 0.78f, 0f),
                new Vector3(1.85f, 0.12f, 1f),
                Quaternion.identity,
                SettlementMaterialKey.Soil);
            builder.AddSphere(
                "Left moss crop",
                new Vector3(-0.48f, 1.02f, 0f),
                new Vector3(0.42f, 0.32f, 0.38f),
                Quaternion.identity,
                SettlementMaterialKey.Moss);
            builder.AddSphere(
                "Right moss crop",
                new Vector3(0.48f, 1.02f, 0f),
                new Vector3(0.42f, 0.32f, 0.38f),
                Quaternion.identity,
                SettlementMaterialKey.Moss);
            builder.AddBox(
                "Copper planter repair",
                new Vector3(0f, 0.46f, -0.66f),
                new Vector3(1.75f, 0.09f, 0.06f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
        }

        private static void BuildFarmPlot(PieceBuilder builder)
        {
            builder.AddBox(
                "Raised farm soil",
                new Vector3(0f, 0.12f, 0f),
                new Vector3(3.7f, 0.24f, 3.7f),
                Quaternion.identity,
                SettlementMaterialKey.Soil);
            builder.AddBox(
                "West timber plot edge",
                new Vector3(-1.88f, 0.28f, 0f),
                new Vector3(0.18f, 0.35f, 4f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "East timber plot edge",
                new Vector3(1.88f, 0.28f, 0f),
                new Vector3(0.18f, 0.35f, 4f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            for (var index = 0; index < 3; index++)
            {
                builder.AddSphere(
                    "Readable moss crop row",
                    new Vector3(-1.05f + index * 1.05f, 0.54f, 0f),
                    new Vector3(0.38f, 0.28f, 1.55f),
                    Quaternion.identity,
                    SettlementMaterialKey.Moss);
            }
        }

        private static void BuildWaterCollector(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    new Vector3(0f, 0f, 0f),
                    Quaternion.identity,
                    Vector3.one,
                    4,
                    3))
            {
                builder.AddCylinder(
                    "Timber rain barrel",
                    new Vector3(0f, 0.75f, 0f),
                    new Vector3(0.72f, 0.75f, 0.72f),
                    Quaternion.identity,
                    SettlementMaterialKey.Timber);
            }

            builder.AddCylinder(
                "Canvas rain funnel",
                new Vector3(0f, 1.72f, 0f),
                new Vector3(1.02f, 0.12f, 1.02f),
                Quaternion.identity,
                SettlementMaterialKey.Canvas);
            builder.AddCylinder(
                "Threadglass water surface",
                new Vector3(0f, 1.83f, 0f),
                new Vector3(0.7f, 0.025f, 0.7f),
                Quaternion.identity,
                SettlementMaterialKey.Water);
            builder.AddBox(
                "Copper collector spout",
                new Vector3(0.88f, 0.82f, 0f),
                new Vector3(0.72f, 0.12f, 0.12f),
                Quaternion.Euler(0f, 0f, -8f),
                SettlementMaterialKey.Copper);
        }

        private static void BuildFeedingTrough(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.Euler(0f, 90f, 0f),
                    Vector3.one,
                    4,
                    3))
            {
                builder.AddBox(
                    "Timber feeding trough",
                    new Vector3(0f, 0.48f, 0f),
                    new Vector3(2.7f, 0.72f, 1.25f),
                    Quaternion.identity,
                    SettlementMaterialKey.Timber);
            }

            builder.AddBox(
                "Trough feed bed",
                new Vector3(0f, 0.76f, 0f),
                new Vector3(2.35f, 0.12f, 0.86f),
                Quaternion.identity,
                SettlementMaterialKey.Soil);
            builder.AddBox(
                "Copper trough brace",
                new Vector3(0f, 0.52f, -0.68f),
                new Vector3(0.11f, 0.52f, 0.08f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
            builder.AddSphere(
                "Pact feeding marker",
                new Vector3(0f, 0.9f, 0.5f),
                Vector3.one * 0.15f,
                Quaternion.identity,
                SettlementMaterialKey.Teal);
        }

        private static void BuildWatchPost(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    new Vector3(0f, 2.25f, 0f),
                    Quaternion.identity,
                    Vector3.one * 0.8f,
                    3,
                    2))
            {
                builder.AddBox(
                    "Timber watch shelter",
                    new Vector3(0f, 3.55f, 0f),
                    new Vector3(2.6f, 0.18f, 2.6f),
                    Quaternion.identity,
                    SettlementMaterialKey.Timber);
            }

            builder.AddBox(
                "Raised watch platform",
                new Vector3(0f, 2.7f, 0f),
                new Vector3(2.65f, 0.22f, 2.65f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Left watch ladder rail",
                new Vector3(-0.38f, 1.35f, 1.12f),
                new Vector3(0.12f, 2.65f, 0.12f),
                Quaternion.Euler(-8f, 0f, 0f),
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Right watch ladder rail",
                new Vector3(0.38f, 1.35f, 1.12f),
                new Vector3(0.12f, 2.65f, 0.12f),
                Quaternion.Euler(-8f, 0f, 0f),
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Copper watch crown",
                new Vector3(0f, 4.15f, 0f),
                new Vector3(1.8f, 0.12f, 0.12f),
                Quaternion.identity,
                SettlementMaterialKey.Copper);
            builder.AddSphere(
                "Threadglass watch marker",
                new Vector3(0f, 3.2f, -1.22f),
                Vector3.one * 0.2f,
                Quaternion.identity,
                SettlementMaterialKey.Teal);
        }

        private static void BuildLanternPost(PieceBuilder builder)
        {
            builder.AddBox(
                "Weathered lantern post",
                new Vector3(0f, 1.45f, 0f),
                new Vector3(0.18f, 2.9f, 0.18f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Copper lantern cage",
                new Vector3(0f, 2.82f, 0f),
                new Vector3(0.52f, 0.62f, 0.52f),
                Quaternion.Euler(0f, 45f, 0f),
                SettlementMaterialKey.Copper);
            builder.AddSphere(
                "Threadglass lantern lens",
                new Vector3(0f, 2.82f, 0f),
                Vector3.one * 0.29f,
                Quaternion.identity,
                SettlementMaterialKey.Teal);
        }

        private static void BuildFence(PieceBuilder builder)
        {
            builder.AddBox(
                "Left timber fence post",
                new Vector3(-1.82f, 0.7f, 0f),
                new Vector3(0.24f, 1.4f, 0.24f),
                Quaternion.Euler(0f, 0f, -2f),
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Right timber fence post",
                new Vector3(1.82f, 0.7f, 0f),
                new Vector3(0.24f, 1.4f, 0.24f),
                Quaternion.Euler(0f, 0f, 2f),
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Copper-mended fence rail",
                new Vector3(0f, 0.78f, 0f),
                new Vector3(3.55f, 0.22f, 0.18f),
                Quaternion.Euler(0f, 0f, 1.5f),
                SettlementMaterialKey.Copper);
        }

        private static void BuildBanner(PieceBuilder builder)
        {
            builder.AddBox(
                "Weathered banner mast",
                new Vector3(-0.48f, 1.65f, 0f),
                new Vector3(0.16f, 3.3f, 0.16f),
                Quaternion.identity,
                SettlementMaterialKey.Timber);
            builder.AddBox(
                "Alderwake canvas banner",
                new Vector3(0.18f, 2.38f, 0f),
                new Vector3(1.18f, 1.3f, 0.06f),
                Quaternion.Euler(0f, 0f, -3f),
                SettlementMaterialKey.Canvas);
            builder.AddBox(
                "Threadglass banner stitch",
                new Vector3(0.18f, 2.38f, -0.04f),
                new Vector3(0.1f, 0.92f, 0.025f),
                Quaternion.Euler(0f, 0f, -3f),
                SettlementMaterialKey.Teal);
        }

        private static void BuildSockets(
            TetherfallSettlementArtDefinition definition,
            Transform parent)
        {
            if (!definition.HasSnapSockets && !definition.HasWorkSocket)
            {
                return;
            }

            var sockets = new GameObject(SocketRootName).transform;
            sockets.SetParent(parent, false);
            var halfX = definition.Footprint.x * 0.5f;
            var halfZ = definition.Footprint.z * 0.5f;

            if (definition.HasSnapSockets)
            {
                CreateSocket(sockets, "Snap_N", new Vector3(0f, 0f, halfZ), 0f);
                CreateSocket(sockets, "Snap_E", new Vector3(halfX, 0f, 0f), 90f);
                CreateSocket(sockets, "Snap_S", new Vector3(0f, 0f, -halfZ), 180f);
                CreateSocket(sockets, "Snap_W", new Vector3(-halfX, 0f, 0f), 270f);
                CreateSocket(
                    sockets,
                    "Snap_Up",
                    new Vector3(0f, definition.Footprint.y, 0f),
                    0f);
                CreateSocket(sockets, "Snap_Down", Vector3.zero, 0f);
            }

            if (definition.HasWorkSocket)
            {
                CreateSocket(
                    sockets,
                    "Socket_Work",
                    new Vector3(0f, 0f, halfZ + definition.WorkRadius),
                    180f);
                CreateSocket(
                    sockets,
                    "Socket_VFX",
                    new Vector3(0f, Mathf.Max(0.5f, definition.Footprint.y * 0.7f), 0f),
                    0f);
            }
        }

        private static void CreateSocket(
            Transform parent,
            string name,
            Vector3 localPosition,
            float yaw)
        {
            var socket = new GameObject(name).transform;
            socket.SetParent(parent, false);
            socket.localPosition = localPosition;
            socket.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private static TetherfallSettlementArtPiece FindDirectPiece(
            Transform parent,
            TetherfallSettlementArtId artId)
        {
            for (var index = 0; index < parent.childCount; index++)
            {
                var piece = parent.GetChild(index).GetComponent<TetherfallSettlementArtPiece>();
                if (piece != null && piece.ArtId == artId)
                {
                    return piece;
                }
            }

            return null;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                DestroyObject(parent.GetChild(index).gameObject);
            }
        }

        private static Bounds CalculateLocalBounds(
            Transform root,
            IReadOnlyList<Renderer> renderers)
        {
            var hasPoint = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var visualRenderer in renderers)
            {
                if (visualRenderer == null || !IsFinite(visualRenderer.bounds))
                {
                    continue;
                }

                var worldBounds = visualRenderer.bounds;
                for (var x = -1; x <= 1; x += 2)
                {
                    for (var y = -1; y <= 1; y += 2)
                    {
                        for (var z = -1; z <= 1; z += 2)
                        {
                            var worldPoint = worldBounds.center + Vector3.Scale(
                                worldBounds.extents,
                                new Vector3(x, y, z));
                            var localPoint = root.InverseTransformPoint(worldPoint);
                            if (!hasPoint)
                            {
                                bounds = new Bounds(localPoint, Vector3.zero);
                                hasPoint = true;
                            }
                            else
                            {
                                bounds.Encapsulate(localPoint);
                            }
                        }
                    }
                }
            }

            return hasPoint ? bounds : new Bounds(Vector3.zero, Vector3.one * 0.01f);
        }

        private static Material GetSharedMaterial(SettlementMaterialKey key)
        {
            if (SharedMaterials.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var shader =
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard") ??
                Shader.Find("Unlit/Color");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "No supported Unity color shader is available for settlement art.");
            }

            var color = GetMaterialColor(key);
            var material = new Material(shader)
            {
                name = "Tetherfall Settlement · " + key,
                hideFlags = HideFlags.HideAndDontSave,
                color = color
            };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", key == SettlementMaterialKey.Copper ? 0.42f : 0.02f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat(
                    "_Smoothness",
                    key == SettlementMaterialKey.Copper ? 0.48f :
                    key == SettlementMaterialKey.Water ? 0.6f : 0.24f);
            }

            if (key == SettlementMaterialKey.Teal && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.55f);
            }

            SharedMaterials[key] = material;
            return material;
        }

        private static Color GetMaterialColor(SettlementMaterialKey key)
        {
            switch (key)
            {
                case SettlementMaterialKey.Timber:
                    return new Color(0.24f, 0.135f, 0.075f);
                case SettlementMaterialKey.Stone:
                    return new Color(0.43f, 0.4f, 0.31f);
                case SettlementMaterialKey.Copper:
                    return new Color(0.68f, 0.35f, 0.15f);
                case SettlementMaterialKey.Teal:
                    return new Color(0.19f, 0.72f, 0.65f);
                case SettlementMaterialKey.Canvas:
                    return new Color(0.62f, 0.53f, 0.36f);
                case SettlementMaterialKey.Moss:
                    return new Color(0.31f, 0.49f, 0.25f);
                case SettlementMaterialKey.Soil:
                    return new Color(0.19f, 0.115f, 0.07f);
                case SettlementMaterialKey.Water:
                    return new Color(0.15f, 0.45f, 0.49f);
                default:
                    return Color.magenta;
            }
        }

        private static void ConfigureRenderer(Renderer visualRenderer)
        {
            visualRenderer.enabled = true;
            visualRenderer.shadowCastingMode = ShadowCastingMode.On;
            visualRenderer.receiveShadows = true;
            visualRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            visualRenderer.allowOcclusionWhenDynamic = true;
        }

        private static bool IsSafeVisualPrefab(GameObject prefab)
        {
            return prefab != null &&
                   prefab.GetComponentsInChildren<Collider>(true).Length == 0 &&
                   prefab.GetComponentsInChildren<Collider2D>(true).Length == 0 &&
                   prefab.GetComponentsInChildren<Light>(true).Length == 0 &&
                   prefab.GetComponentsInChildren<Camera>(true).Length == 0 &&
                   prefab.GetComponentsInChildren<AudioListener>(true).Length == 0 &&
                   prefab.GetComponentsInChildren<Animator>(true).Length == 0 &&
                   prefab.GetComponentsInChildren<Animation>(true).Length == 0 &&
                   prefab.GetComponentsInChildren<Rigidbody>(true).Length == 0 &&
                   prefab.GetComponentsInChildren<Rigidbody2D>(true).Length == 0 &&
                   prefab.GetComponentsInChildren<MonoBehaviour>(true).Length == 0;
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

        private enum SettlementMaterialKey
        {
            Timber,
            Stone,
            Copper,
            Teal,
            Canvas,
            Moss,
            Soil,
            Water
        }

        private sealed class PieceBuilder
        {
            private readonly Transform parent;
            private readonly bool forceProceduralFallback;
            private readonly List<Renderer> renderers = new List<Renderer>();
            private readonly HashSet<Material> materials = new HashSet<Material>();

            public TetherfallSettlementArtDefinition Definition { get; }
            public bool UsedPreferredResource { get; private set; }
            public int RendererCount => renderers.Count;
            public int SharedMaterialCount => materials.Count;
            public IReadOnlyList<Renderer> Renderers => renderers;

            public PieceBuilder(
                TetherfallSettlementArtDefinition definition,
                Transform parent,
                bool forceProceduralFallback)
            {
                Definition = definition;
                this.parent = parent;
                this.forceProceduralFallback = forceProceduralFallback;
            }

            public bool TryPreferredResource(
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale,
                int maxRendererContribution,
                int maxMaterialContribution)
            {
                if (forceProceduralFallback || !Definition.HasPreferredResource)
                {
                    return false;
                }

                var prefab = Resources.Load<GameObject>(Definition.PreferredResourcePath);
                if (!IsSafeVisualPrefab(prefab))
                {
                    return false;
                }

                var sourceRenderers = prefab.GetComponentsInChildren<Renderer>(true);
                var sourceMaterials = new HashSet<Material>();
                foreach (var sourceRenderer in sourceRenderers)
                {
                    foreach (var sourceMaterial in sourceRenderer.sharedMaterials)
                    {
                        if (sourceMaterial == null)
                        {
                            return false;
                        }

                        sourceMaterials.Add(sourceMaterial);
                    }
                }

                if (sourceRenderers.Length == 0 ||
                    sourceRenderers.Length > maxRendererContribution ||
                    sourceMaterials.Count == 0 ||
                    sourceMaterials.Count > maxMaterialContribution ||
                    RendererCount + sourceRenderers.Length > Definition.MaxRendererCount ||
                    CountNewMaterials(sourceMaterials) + SharedMaterialCount >
                    Definition.MaxSharedMaterialCount)
                {
                    return false;
                }

                var instance = Object.Instantiate(prefab, parent, false);
                instance.name = "Quaternius source · " + Definition.PreferredResourceName;
                instance.transform.localPosition = localPosition;
                instance.transform.localRotation = localRotation;
                instance.transform.localScale = SanitizeScale(localScale);

                var instanceRenderers = instance.GetComponentsInChildren<Renderer>(true);
                if (instanceRenderers.Length != sourceRenderers.Length)
                {
                    DestroyObject(instance);
                    return false;
                }

                foreach (var visualRenderer in instanceRenderers)
                {
                    ConfigureRenderer(visualRenderer);
                    renderers.Add(visualRenderer);
                    foreach (var material in visualRenderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            materials.Add(material);
                        }
                    }
                }

                UsedPreferredResource = true;
                return true;
            }

            public GameObject AddBox(
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                SettlementMaterialKey material)
            {
                return AddPrimitive(
                    PrimitiveType.Cube,
                    name,
                    localPosition,
                    localScale,
                    localRotation,
                    material);
            }

            public GameObject AddCylinder(
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                SettlementMaterialKey material)
            {
                return AddPrimitive(
                    PrimitiveType.Cylinder,
                    name,
                    localPosition,
                    localScale,
                    localRotation,
                    material);
            }

            public GameObject AddSphere(
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                SettlementMaterialKey material)
            {
                return AddPrimitive(
                    PrimitiveType.Sphere,
                    name,
                    localPosition,
                    localScale,
                    localRotation,
                    material);
            }

            public void ValidateFinalBudget()
            {
                if (RendererCount == 0)
                {
                    throw new InvalidOperationException(
                        $"Settlement art '{Definition.StableId}' produced no renderers.");
                }

                if (RendererCount > Definition.MaxRendererCount)
                {
                    throw new InvalidOperationException(
                        $"Settlement art '{Definition.StableId}' produced {RendererCount} renderers; " +
                        $"budget is {Definition.MaxRendererCount}.");
                }

                if (SharedMaterialCount == 0 ||
                    SharedMaterialCount > Definition.MaxSharedMaterialCount)
                {
                    throw new InvalidOperationException(
                        $"Settlement art '{Definition.StableId}' uses {SharedMaterialCount} shared " +
                        $"materials; budget is {Definition.MaxSharedMaterialCount}.");
                }
            }

            private GameObject AddPrimitive(
                PrimitiveType primitiveType,
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                SettlementMaterialKey materialKey)
            {
                var material = GetSharedMaterial(materialKey);
                var newMaterialCount = materials.Contains(material) ? 0 : 1;
                if (RendererCount + 1 > Definition.MaxRendererCount ||
                    SharedMaterialCount + newMaterialCount > Definition.MaxSharedMaterialCount)
                {
                    throw new InvalidOperationException(
                        $"Recipe for '{Definition.StableId}' exceeded its declared renderer/material " +
                        "budget while adding '" + name + "'.");
                }

                var visual = GameObject.CreatePrimitive(primitiveType);
                visual.name = name;
                visual.transform.SetParent(parent, false);
                visual.transform.localPosition = IsFinite(localPosition)
                    ? localPosition
                    : Vector3.zero;
                visual.transform.localScale = SanitizeScale(localScale);
                visual.transform.localRotation = IsFinite(localRotation)
                    ? localRotation
                    : Quaternion.identity;

                var visualCollider = visual.GetComponent<Collider>();
                if (visualCollider != null)
                {
                    visualCollider.enabled = false;
                    DestroyObject(visualCollider);
                }

                var visualRenderer = visual.GetComponent<Renderer>();
                visualRenderer.sharedMaterial = material;
                ConfigureRenderer(visualRenderer);
                renderers.Add(visualRenderer);
                materials.Add(material);
                return visual;
            }

            private int CountNewMaterials(IEnumerable<Material> candidates)
            {
                var count = 0;
                foreach (var candidate in candidates)
                {
                    if (candidate != null && !materials.Contains(candidate))
                    {
                        count++;
                    }
                }

                return count;
            }

            private static Vector3 SanitizeScale(Vector3 value)
            {
                if (!IsFinite(value))
                {
                    return Vector3.one;
                }

                return new Vector3(
                    Mathf.Clamp(Mathf.Abs(value.x), 0.01f, 24f),
                    Mathf.Clamp(Mathf.Abs(value.y), 0.01f, 24f),
                    Mathf.Clamp(Mathf.Abs(value.z), 0.01f, 24f));
            }

            private static bool IsFinite(Vector3 value)
            {
                return TetherfallSettlementArt.IsFinite(value);
            }

            private static bool IsFinite(float value)
            {
                return TetherfallSettlementArt.IsFinite(value);
            }

            private static bool IsFinite(Quaternion value)
            {
                return IsFinite(value.x) &&
                       IsFinite(value.y) &&
                       IsFinite(value.z) &&
                       IsFinite(value.w);
            }
        }
    }
}
