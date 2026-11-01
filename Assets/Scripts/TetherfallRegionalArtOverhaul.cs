using UnityEngine;

namespace Tetherfall
{
    /// <summary>
    /// Adds a compact, non-solid regional dressing pass without changing gameplay geometry.
    /// Every renderer uses one of the cached palette materials created during Build.
    /// </summary>
    public static class TetherfallRegionalArtOverhaul
    {
        public const int CachedMaterialCount = 12;
        public const int RenderersPerComposition = 14;
        public const int RenderersPerRegion = RenderersPerComposition * 2;
        public const int TotalRendererCount = RenderersPerRegion * 3;
        public const int AddedLightCount = 0;

        private static bool built;
        private static int rendererCount;
        private static RegionalPalette palette;

        public static bool IsBuilt => built;
        public static int BuiltRendererCount { get; private set; }
        public static int BuiltMaterialCount { get; private set; }
        public static int AlderwakeRendererCount { get; private set; }
        public static int MorrowfenRendererCount { get; private set; }
        public static int CinderwoldRendererCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            built = false;
            rendererCount = 0;
            palette = null;
            BuiltRendererCount = 0;
            BuiltMaterialCount = 0;
            AlderwakeRendererCount = 0;
            MorrowfenRendererCount = 0;
            CinderwoldRendererCount = 0;
        }

        /// <summary>
        /// Builds two bounded visual compositions in each base region.
        /// Call once after the world and expansion builders have placed gameplay sites.
        /// </summary>
        public static void Build(TetherfallGame game)
        {
            if (built)
            {
                return;
            }

            if (game == null)
            {
                Debug.LogError("Tetherfall regional art overhaul requires a TetherfallGame owner.");
                return;
            }

            palette = new RegionalPalette(game);
            rendererCount = 0;

            var root = new GameObject("Tetherfall Regional Art Overhaul");
            root.transform.SetParent(game.transform, false);

            var regionStart = rendererCount;
            BuildAlderwake(root.transform);
            AlderwakeRendererCount = rendererCount - regionStart;

            regionStart = rendererCount;
            BuildMorrowfen(root.transform);
            MorrowfenRendererCount = rendererCount - regionStart;

            regionStart = rendererCount;
            BuildCinderwold(root.transform);
            CinderwoldRendererCount = rendererCount - regionStart;

            BuiltRendererCount = rendererCount;
            BuiltMaterialCount = CachedMaterialCount;
            built = true;

            Debug.Assert(
                AlderwakeRendererCount == RenderersPerRegion &&
                MorrowfenRendererCount == RenderersPerRegion &&
                CinderwoldRendererCount == RenderersPerRegion &&
                BuiltRendererCount == TotalRendererCount,
                "Tetherfall regional art exceeded its fixed renderer composition.");
            Debug.Log(
                $"Tetherfall regional art ready: {BuiltRendererCount} non-solid renderers, " +
                $"{BuiltMaterialCount} cached shared materials, {AddedLightCount} added lights.");
        }

        private static void BuildAlderwake(Transform parent)
        {
            var region = CreateRoot("Alderwake · mended contour gardens", parent, Vector3.zero, 0f);

            // These edge pockets sit outside Hearthmarket, both route events, the Hearthgate,
            // Northwake, Southroot, encounter footprints, and the central traversal corridor.
            CreateAlderwakeFold(region, new Vector3(-71f, 0f, -48f), 28f, 1f);
            CreateAlderwakeFold(region, new Vector3(71f, 0f, -21f), -32f, -1f);
        }

        private static void CreateAlderwakeFold(
            Transform parent,
            Vector3 position,
            float yaw,
            float mirror)
        {
            var root = CreateRoot("Repaired moss contour", parent, position, yaw);

            for (var index = 0; index < 3; index++)
            {
                var offset = index - 1f;
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Warm set-stone contour",
                    root,
                    new Vector3(offset * 2.25f * mirror, 0.12f + index % 2 * 0.035f, -0.5f + Mathf.Abs(offset) * 0.55f),
                    new Vector3(2.45f, 0.16f, 1.12f),
                    Quaternion.Euler(0f, offset * 11f * mirror, offset * 1.8f),
                    palette.alderStone);
            }

            for (var index = 0; index < 3; index++)
            {
                var offset = index - 1f;
                var x = offset * 2.15f * mirror;
                var tilt = offset * -6f * mirror;
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Weathered contour rib",
                    root,
                    new Vector3(x, 1.32f + index % 2 * 0.16f, 0.12f + Mathf.Abs(offset) * 0.18f),
                    new Vector3(0.14f, 1.22f + index % 2 * 0.12f, 0.14f),
                    Quaternion.Euler(0f, index * 27f, tilt),
                    palette.alderTimber);
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Clipped moss crown",
                    root,
                    new Vector3(x - tilt * 0.018f, 2.72f + index % 2 * 0.28f, 0.1f),
                    new Vector3(1.42f, 0.58f + index % 2 * 0.08f, 1.02f),
                    Quaternion.Euler(0f, index * 31f, tilt * 0.35f),
                    palette.alderMoss);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Copper repair collar",
                    root,
                    new Vector3(x, 1.1f + index % 2 * 0.12f, 0.1f),
                    new Vector3(0.22f, 0.075f, 0.22f),
                    Quaternion.Euler(0f, index * 27f, tilt),
                    palette.copper);
            }

            for (var index = 0; index < 2; index++)
            {
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Restrained Threadglass route stitch",
                    root,
                    new Vector3((index == 0 ? -1.2f : 1.2f) * mirror, 0.32f, -1.46f),
                    Vector3.one * 0.18f,
                    Quaternion.identity,
                    palette.routeTeal);
            }
        }

        private static void BuildMorrowfen(Transform parent)
        {
            var region = CreateRoot("Morrowfen · drowned reed fans", parent, Vector3.zero, 0f);

            // The west dry pocket and far-east silt shelf avoid Veyrwater, Fenlight,
            // Skyvault, Siltward Cairns, route signals, camps, and bounty arenas.
            CreateMorrowfenFan(region, new Vector3(91f, 0f, 62f), 18f, 1f);
            CreateMorrowfenFan(region, new Vector3(229f, 0f, 8f), -22f, -1f);
        }

        private static void CreateMorrowfenFan(
            Transform parent,
            Vector3 position,
            float yaw,
            float mirror)
        {
            var root = CreateRoot("Drowned survey fan", parent, position, yaw);

            for (var index = 0; index < 2; index++)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Peat stepping eye",
                    root,
                    new Vector3((index == 0 ? -1.65f : 1.55f) * mirror, 0.055f, index == 0 ? 0.42f : -0.32f),
                    new Vector3(2.5f, 0.055f, 1.55f),
                    Quaternion.Euler(0f, (index == 0 ? -11f : 14f) * mirror, 0f),
                    palette.fenPeat);
            }

            for (var index = 0; index < 4; index++)
            {
                var offset = index - 1.5f;
                var x = offset * 1.72f * mirror;
                var lean = (index % 2 == 0 ? -8f : 10f) * mirror;
                var height = 1.02f + index % 3 * 0.16f;
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Lantern-reed mast",
                    root,
                    new Vector3(x, height, index % 2 == 0 ? 0.18f : -0.18f),
                    new Vector3(0.065f, height, 0.065f),
                    Quaternion.Euler(lean * 0.2f, index * 23f, lean),
                    palette.fenReed);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Wet reed vane",
                    root,
                    new Vector3(x + lean * 0.018f, 1.86f + index % 3 * 0.19f, index % 2 == 0 ? 0.16f : -0.16f),
                    new Vector3(0.2f, 0.88f + index % 2 * 0.16f, 0.045f),
                    Quaternion.Euler(8f, index * 37f, lean * 1.35f),
                    palette.fenWater);
            }

            for (var index = 0; index < 2; index++)
            {
                var side = index == 0 ? -1f : 1f;
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Copper survey tie",
                    root,
                    new Vector3(side * 1.45f * mirror, 1.26f + index * 0.16f, -0.34f),
                    new Vector3(1.65f, 0.07f, 0.08f),
                    Quaternion.Euler(0f, side * 9f * mirror, side * 7f),
                    palette.copper);
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Fen route dew",
                    root,
                    new Vector3(side * 1.38f * mirror, 0.29f, -1.12f + index * 0.08f),
                    Vector3.one * 0.17f,
                    Quaternion.identity,
                    palette.routeTeal);
            }
        }

        private static void BuildCinderwold(Transform parent)
        {
            var region = CreateRoot("Cinderwold · wind-cut repair fins", parent, Vector3.zero, 0f);

            // These outer shelves avoid Cinder Station, the Ash-Copper Spine, both
            // signal defenses, the Skyfire Relay, Ashwake gate, camps, and caches.
            CreateCinderwoldFin(region, new Vector3(-229f, 0f, 8f), 16f, 1f);
            CreateCinderwoldFin(region, new Vector3(-91f, 0f, -89f), -26f, -1f);
        }

        private static void CreateCinderwoldFin(
            Transform parent,
            Vector3 position,
            float yaw,
            float mirror)
        {
            var root = CreateRoot("Wind-cut copper repair", parent, position, yaw);

            for (var index = 0; index < 3; index++)
            {
                var offset = index - 1f;
                var x = offset * 2.12f * mirror;
                var tilt = offset * -8f * mirror;
                var height = 2.35f + index % 2 * 0.42f;
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Ash shelf plate",
                    root,
                    new Vector3(x, 0.09f + index % 2 * 0.025f, 0.28f + Mathf.Abs(offset) * 0.22f),
                    new Vector3(2.2f, 0.13f, 1.18f),
                    Quaternion.Euler(0f, offset * 13f * mirror, offset * 2.2f),
                    palette.cinderStone);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Wind-split basalt tooth",
                    root,
                    new Vector3(x, height * 0.5f + 0.14f, 0.16f),
                    new Vector3(0.62f + index % 2 * 0.12f, height, 0.58f),
                    Quaternion.Euler(0f, index * 17f, tilt),
                    palette.basalt);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Copper tension rib",
                    root,
                    new Vector3(x + 0.5f * mirror, 1.45f + index % 2 * 0.18f, -0.42f),
                    new Vector3(0.105f, 2.1f + index % 2 * 0.28f, 0.105f),
                    Quaternion.Euler(0f, index * 17f, tilt - 13f * mirror),
                    palette.copper);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Banked ember seam",
                    root,
                    new Vector3(x, height * 0.5f + 0.12f, -0.145f),
                    new Vector3(0.105f, height * 0.66f, 0.035f),
                    Quaternion.Euler(0f, index * 17f, tilt),
                    palette.ember);
            }

            for (var index = 0; index < 2; index++)
            {
                var side = index == 0 ? -1f : 1f;
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Broken-copper wind fin",
                    root,
                    new Vector3(side * 1.12f * mirror, 3.08f + index * 0.18f, 0.08f),
                    new Vector3(1.35f, 0.16f, 0.66f),
                    Quaternion.Euler(17f, side * 31f * mirror, side * 12f),
                    palette.cinderCopper);
            }
        }

        private static Transform CreateRoot(
            string name,
            Transform parent,
            Vector3 localPosition,
            float localYaw)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = localPosition;
            root.localRotation = Quaternion.Euler(0f, localYaw, 0f);
            return root;
        }

        private static GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var visual = GameObject.CreatePrimitive(primitiveType);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
            visual.transform.localRotation = localRotation;

            var visualRenderer = visual.GetComponent<Renderer>();
            visualRenderer.sharedMaterial = material;

            var visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                visualCollider.enabled = false;
                if (Application.isPlaying)
                {
                    Object.Destroy(visualCollider);
                }
                else
                {
                    Object.DestroyImmediate(visualCollider);
                }
            }

            rendererCount++;
            return visual;
        }

        private sealed class RegionalPalette
        {
            public readonly Material copper;
            public readonly Material routeTeal;
            public readonly Material alderStone;
            public readonly Material alderTimber;
            public readonly Material alderMoss;
            public readonly Material fenPeat;
            public readonly Material fenReed;
            public readonly Material fenWater;
            public readonly Material basalt;
            public readonly Material cinderStone;
            public readonly Material cinderCopper;
            public readonly Material ember;

            public RegionalPalette(TetherfallGame game)
            {
                copper = game.CreateMaterial(new Color(0.66f, 0.34f, 0.14f), 0.34f, 0.52f);
                routeTeal = game.CreateGlowMaterial(new Color(0.25f, 0.78f, 0.7f), 1.32f, 0.62f);

                alderStone = game.CreateMaterial(new Color(0.39f, 0.39f, 0.3f), 0.03f, 0.3f);
                alderTimber = game.CreateMaterial(new Color(0.2f, 0.12f, 0.075f), 0.02f, 0.23f);
                alderMoss = game.CreateMaterial(new Color(0.29f, 0.49f, 0.25f), 0f, 0.25f);

                fenPeat = game.CreateMaterial(new Color(0.12f, 0.22f, 0.22f), 0.02f, 0.3f);
                fenReed = game.CreateMaterial(new Color(0.28f, 0.48f, 0.37f), 0f, 0.22f);
                fenWater = game.CreateMaterial(new Color(0.13f, 0.34f, 0.39f), 0.08f, 0.58f);

                basalt = game.CreateMaterial(new Color(0.11f, 0.06f, 0.075f), 0.06f, 0.26f);
                cinderStone = game.CreateMaterial(new Color(0.27f, 0.13f, 0.12f), 0.08f, 0.31f);
                cinderCopper = game.CreateMaterial(new Color(0.49f, 0.2f, 0.1f), 0.3f, 0.46f);
                ember = game.CreateGlowMaterial(new Color(0.92f, 0.2f, 0.07f), 1.5f, 0.56f);
            }
        }
    }
}
