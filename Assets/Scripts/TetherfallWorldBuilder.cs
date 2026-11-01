using DateTime = System.DateTime;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public static class WorldBuilder
    {
        private static TetherfallGame game;
        private static Material basinGround;
        private static Material fenGround;
        private static Material cinderGround;
        private static Material trunkMaterial;
        private static Material copperMaterial;
        private static Material reedMaterial;
        private static Material cinderRockMaterial;
        private static Material alderCanopyMaterial;
        private static Material alderCanopyHighlightMaterial;
        private static Material reedTipMaterial;
        private static Material cinderGlowMaterial;
        private static readonly Dictionary<string, Texture2D> GeneratedGroundTextures =
            new Dictionary<string, Texture2D>();

        public static void Build(TetherfallGame owner)
        {
            game = owner;
            basinGround = CreateGroundMaterial("Art/Original/tetherfall_alderwake_ground", new Color(0.25f, 0.42f, 0.3f));
            fenGround = CreateGroundMaterial("Art/Original/tetherfall_morrowfen_ground", new Color(0.16f, 0.3f, 0.32f));
            cinderGround = CreateGroundMaterial("Art/Original/tetherfall_cinderwold_ground", new Color(0.28f, 0.18f, 0.18f));
            trunkMaterial = game.CreateMaterial(new Color(0.21f, 0.13f, 0.1f), 0f, 0.28f);
            copperMaterial = game.CreateMaterial(new Color(0.76f, 0.43f, 0.2f), 0.42f, 0.55f);
            reedMaterial = game.CreateMaterial(new Color(0.37f, 0.67f, 0.53f), 0f, 0.26f);
            cinderRockMaterial = game.CreateMaterial(new Color(0.19f, 0.1f, 0.12f), 0.08f, 0.25f);
            alderCanopyMaterial = game.CreateMaterial(new Color(0.23f, 0.55f, 0.29f), 0f, 0.24f);
            alderCanopyHighlightMaterial = game.CreateMaterial(new Color(0.42f, 0.7f, 0.31f), 0f, 0.3f);
            reedTipMaterial = game.CreateGlowMaterial(new Color(0.37f, 0.86f, 0.7f), 0.54f, 0.42f);
            cinderGlowMaterial = game.CreateGlowMaterial(new Color(0.94f, 0.29f, 0.12f), 0.92f, 0.48f);

            CreateGround("Alderwake Ground", new Vector3(0f, -1f, 0f), new Vector3(160f, 2f, 260f), basinGround);
            CreateGround("Morrowfen Ground", new Vector3(160f, -1f, 0f), new Vector3(160f, 2f, 260f), fenGround);
            CreateGround("Cinderwold Ground", new Vector3(-160f, -1f, 0f), new Vector3(160f, 2f, 260f), cinderGround);

            BuildAlderwake();
            BuildMorrowfen();
            BuildCinderwold();
            TetherfallRegionPacks.Build(game);
            TetherfallExplorationLandmarks.Build(game);
            BuildBoundaries();
        }

        private static void BuildAlderwake()
        {
            AddRegionSign("ALDERWAKE BASIN", "Greenwater terraces and first routes", new Vector3(0f, 0f, -7f), new Color(0.65f, 0.91f, 0.76f));
            Hearthgate.Create(game, new Vector3(-20f, 0f, 14f));
            CreateSeedspindle(new Vector3(4f, 0f, 19f));

            var trees = new[]
            {
                new Vector3(-42f, 0f, -30f), new Vector3(-34f, 0f, 27f), new Vector3(-12f, 0f, -39f),
                new Vector3(19f, 0f, -31f), new Vector3(37f, 0f, 29f), new Vector3(48f, 0f, -15f),
                new Vector3(27f, 0f, 40f), new Vector3(-49f, 0f, 7f), new Vector3(7f, 0f, 41f)
            };
            foreach (var position in trees)
            {
                CreateCanopyTree(position, 1.1f + Mathf.Abs(position.x % 3f) * 0.1f);
            }

            CreateWater(new Vector3(-8f, 0.05f, -11f), new Vector3(27f, 0.12f, 15f), new Color(0.14f, 0.48f, 0.55f, 0.72f));
            CreateBridge(new Vector3(-8f, 0.5f, -10f), 14f);
            CreateFarmTerrace(new Vector3(24f, 0f, 10f));

            CreateResources(ResourceKind.Timber, new Color(0.48f, 0.31f, 0.15f),
                new Vector3(-31f, 0.75f, -11f), new Vector3(-28f, 0.75f, -7f), new Vector3(18f, 0.75f, -24f),
                new Vector3(35f, 0.75f, 18f), new Vector3(10f, 0.75f, 34f));
            CreateResources(ResourceKind.Stone, new Color(0.46f, 0.49f, 0.51f),
                new Vector3(-40f, 0.7f, 16f), new Vector3(-2f, 0.7f, -41f), new Vector3(40f, 0.7f, -31f),
                new Vector3(47f, 0.7f, 7f));
            CreateResources(ResourceKind.Resin, new Color(0.93f, 0.56f, 0.2f),
                new Vector3(-19f, 0.75f, 33f), new Vector3(16f, 0.75f, 26f), new Vector3(40f, 0.75f, 37f));

            PactCreature.Create(game, "Bramblet", new Vector3(11f, 0.8f, 21f), new Color(0.28f, 0.74f, 0.48f), false, "seed-and-vine Echosworn", true);
            PactCreature.Create(game, "Slateback", new Vector3(-33f, 1.1f, -26f), new Color(0.39f, 0.44f, 0.63f), true, "ride-ready stonehide");
            HumanCompanion.Create(game, "Sera Venn", "river scout", new Vector3(26f, 0.95f, -12f), new Color(0.82f, 0.51f, 0.28f));
            EnemyCamp.Create(
                game,
                "encounter.alderwake.hollow_reed",
                new Vector3(-38f, 0f, 33f),
                TetherfallGame.HollowReedMilestoneId);
        }

        private static void BuildMorrowfen()
        {
            AddRegionSign("MORROWFEN", "Drowned paths and lantern reeds", new Vector3(93f, 0f, -8f), new Color(0.6f, 0.85f, 0.82f));
            CreateWater(new Vector3(126f, 0.04f, 9f), new Vector3(88f, 0.14f, 31f), new Color(0.08f, 0.26f, 0.32f, 0.84f));
            CreateWater(new Vector3(165f, 0.04f, -34f), new Vector3(33f, 0.14f, 22f), new Color(0.06f, 0.22f, 0.3f, 0.84f));

            var reeds = new[]
            {
                new Vector3(84f, 0f, 21f), new Vector3(96f, 0f, 29f), new Vector3(110f, 0f, -17f),
                new Vector3(129f, 0f, 34f), new Vector3(146f, 0f, -12f), new Vector3(171f, 0f, 29f),
                new Vector3(180f, 0f, -28f), new Vector3(158f, 0f, 40f), new Vector3(93f, 0f, -37f)
            };
            foreach (var position in reeds)
            {
                CreateReedCluster(position, 1.1f + Mathf.Abs(position.z % 4f) * 0.08f);
            }

            CreateFenLantern(new Vector3(124f, 0f, -22f));
            CreateFenLantern(new Vector3(151f, 0f, 22f));
            CreateFenLantern(new Vector3(177f, 0f, 4f));
            CreateSiltcallerShrine(new Vector3(118f, 0f, 43f));
            CreateSiltcallerShrine(new Vector3(192f, 0f, -76f));
            CreateResources(ResourceKind.Resin, new Color(0.23f, 0.78f, 0.7f),
                new Vector3(89f, 0.75f, 15f), new Vector3(104f, 0.75f, -27f), new Vector3(139f, 0.75f, 34f),
                new Vector3(164f, 0.75f, 20f), new Vector3(176f, 0.75f, -8f));
            CreateResources(ResourceKind.Chimesalt, new Color(0.64f, 0.69f, 0.96f),
                new Vector3(93f, 0.75f, -41f), new Vector3(132f, 0.75f, -31f), new Vector3(169f, 0.75f, 42f));

            PactCreature.Create(game, "Mistling", new Vector3(138f, 0.8f, 28f), new Color(0.45f, 0.74f, 0.84f), false, "lantern-breath Echosworn");
            HumanCompanion.Create(game, "Corin Ashfold", "field medic", new Vector3(106f, 0.95f, -23f), new Color(0.48f, 0.67f, 0.79f));
            EnemyCamp.Create(game, "encounter.morrowfen.sunken_watch", new Vector3(169f, 0f, -28f));
        }

        private static void BuildCinderwold()
        {
            AddRegionSign("CINDERWOLD VERGE", "Ash shelves and broken relay stone", new Vector3(-95f, 0f, -8f), new Color(0.93f, 0.65f, 0.52f));
            var rocks = new[]
            {
                new Vector3(-80f, 0f, -31f), new Vector3(-101f, 0f, 19f), new Vector3(-124f, 0f, -25f),
                new Vector3(-148f, 0f, 31f), new Vector3(-172f, 0f, -15f), new Vector3(-183f, 0f, 33f),
                new Vector3(-111f, 0f, 42f), new Vector3(-149f, 0f, -42f)
            };
            foreach (var position in rocks)
            {
                CreateCinderPillar(position, 1.1f + Mathf.Abs(position.x % 5f) * 0.12f);
            }

            CreateAshSpire(new Vector3(-132f, 0f, 7f));
            CreateAshSpire(new Vector3(-178f, 0f, -18f));
            CreateResources(ResourceKind.Stone, new Color(0.55f, 0.43f, 0.42f),
                new Vector3(-84f, 0.75f, 24f), new Vector3(-104f, 0.75f, -31f), new Vector3(-139f, 0.75f, 35f),
                new Vector3(-160f, 0.75f, -27f), new Vector3(-183f, 0.75f, 8f));
            CreateResources(ResourceKind.Chimesalt, new Color(0.95f, 0.53f, 0.31f),
                new Vector3(-94f, 0.75f, 38f), new Vector3(-127f, 0.75f, -39f), new Vector3(-172f, 0.75f, 33f));

            PactCreature.Create(game, "Cinderwing", new Vector3(-146f, 0.85f, 17f), new Color(0.9f, 0.38f, 0.22f), false, "ember-feather Echosworn");
            HumanCompanion.Create(game, "Tovan Mire", "ruin breaker", new Vector3(-101f, 0.95f, 26f), new Color(0.67f, 0.38f, 0.28f));
            EnemyCamp.Create(game, "encounter.cinderwold.ashjaw", new Vector3(-171f, 0f, -31f));
        }

        private static void CreateGround(string name, Vector3 position, Vector3 scale, Material material)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = name;
            ground.transform.position = position;
            ground.transform.localScale = scale;
            ground.GetComponent<Renderer>().material = material;
        }

        private static Material CreateGroundMaterial(string resourcePath, Color fallbackTint)
        {
            var material = game.CreateMaterial(fallbackTint, 0f, 0.2f);
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                texture = CreateProceduralGroundTexture(resourcePath);
            }

            if (material != null && texture != null && material.HasProperty("_MainTex"))
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                texture.filterMode = FilterMode.Bilinear;
                material.mainTexture = texture;
                material.mainTextureScale = new Vector2(22f, 22f);
            }

            return material;
        }

        private static Texture2D CreateProceduralGroundTexture(string resourcePath)
        {
            if (GeneratedGroundTextures.TryGetValue(resourcePath, out var cachedTexture) && cachedTexture != null)
            {
                return cachedTexture;
            }

            const int size = 192;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = $"Generated {resourcePath} terrain",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            var alderwake = resourcePath.Contains("alderwake");
            var morrowfen = resourcePath.Contains("morrowfen");
            var seed = alderwake ? 17 : morrowfen ? 43 : 79;
            for (var y = 0; y < size; y++)
            {
                var v = y / (float)(size - 1);
                for (var x = 0; x < size; x++)
                {
                    var u = x / (float)(size - 1);
                    pixels[y * size + x] = alderwake
                        ? SampleAlderwakeGround(u, v, seed)
                        : morrowfen
                            ? SampleMorrowfenGround(u, v, seed)
                            : SampleCinderwoldGround(u, v, seed);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            GeneratedGroundTextures[resourcePath] = texture;
            return texture;
        }

        private static Color SampleAlderwakeGround(float u, float v, int seed)
        {
            var broad = TileNoise(u, v, 5, seed);
            var fine = TileNoise(u, v, 18, seed + 3);
            var moss = Color.Lerp(new Color(0.14f, 0.22f, 0.13f), new Color(0.31f, 0.5f, 0.2f), broad);
            moss = Color.Lerp(moss, new Color(0.42f, 0.58f, 0.24f), Mathf.Clamp01((fine - 0.55f) * 0.7f));
            var soil = TileNoise(u, v, 9, seed + 7);
            if (soil > 0.74f)
            {
                moss = Color.Lerp(moss, new Color(0.19f, 0.16f, 0.1f), Mathf.InverseLerp(0.74f, 0.94f, soil));
            }

            var stones = TileNoise(u, v, 23, seed + 11);
            if (stones > 0.89f)
            {
                moss = Color.Lerp(moss, new Color(0.36f, 0.39f, 0.34f), Mathf.InverseLerp(0.89f, 1f, stones));
            }

            return moss;
        }

        private static Color SampleMorrowfenGround(float u, float v, int seed)
        {
            var broad = TileNoise(u, v, 6, seed);
            var wet = Color.Lerp(new Color(0.08f, 0.16f, 0.18f), new Color(0.19f, 0.35f, 0.35f), broad);
            var channel = Mathf.Abs(Mathf.Sin((u * 5.7f + v * 7.4f + TileNoise(u, v, 10, seed + 5) * 0.6f) * Mathf.PI));
            if (channel < 0.18f)
            {
                wet = Color.Lerp(wet, new Color(0.075f, 0.1f, 0.12f), Mathf.InverseLerp(0.18f, 0f, channel));
            }

            var reedShadow = TileNoise(u, v, 19, seed + 9);
            if (reedShadow > 0.76f)
            {
                wet = Color.Lerp(wet, new Color(0.22f, 0.42f, 0.34f), Mathf.InverseLerp(0.76f, 1f, reedShadow) * 0.45f);
            }

            return wet;
        }

        private static Color SampleCinderwoldGround(float u, float v, int seed)
        {
            var ash = TileNoise(u, v, 7, seed);
            var ground = Color.Lerp(new Color(0.12f, 0.08f, 0.11f), new Color(0.29f, 0.15f, 0.18f), ash);
            var shelf = TileNoise(u, v, 15, seed + 4);
            if (shelf > 0.7f)
            {
                ground = Color.Lerp(ground, new Color(0.23f, 0.2f, 0.24f), Mathf.InverseLerp(0.7f, 1f, shelf));
            }

            var cinderSeam = Mathf.Abs(Mathf.Sin((u * 8.6f - v * 6.3f + TileNoise(u, v, 9, seed + 8) * 0.85f) * Mathf.PI));
            if (cinderSeam < 0.075f)
            {
                ground = Color.Lerp(ground, new Color(0.72f, 0.2f, 0.1f), Mathf.InverseLerp(0.075f, 0f, cinderSeam) * 0.76f);
            }

            return ground;
        }

        private static float TileNoise(float u, float v, int cells, int seed)
        {
            var x = u * cells;
            var y = v * cells;
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var tx = Smooth01(x - x0);
            var ty = Smooth01(y - y0);
            var a = HashTile(x0, y0, cells, seed);
            var b = HashTile(x0 + 1, y0, cells, seed);
            var c = HashTile(x0, y0 + 1, cells, seed);
            var d = HashTile(x0 + 1, y0 + 1, cells, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float HashTile(int x, int y, int cells, int seed)
        {
            x = ((x % cells) + cells) % cells;
            y = ((y % cells) + cells) % cells;
            var value = Mathf.Sin(x * 12.9898f + y * 78.233f + seed * 37.719f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        private static float Smooth01(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private static void CreateCanopyTree(Vector3 position, float scale)
        {
            var tree = new GameObject("Alderwake Threadwood");
            tree.transform.position = position;
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Threadwood tapered trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 2.15f * scale, 0f);
            trunk.transform.localScale = new Vector3(0.46f * scale, 2.15f * scale, 0.46f * scale);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;

            for (var branchIndex = 0; branchIndex < 3; branchIndex++)
            {
                var branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                branch.name = "Threadwood branch";
                branch.transform.SetParent(tree.transform, false);
                var angle = branchIndex * 120f;
                var radians = angle * Mathf.Deg2Rad;
                branch.transform.localPosition = new Vector3(
                    Mathf.Sin(radians) * 0.42f * scale,
                    (3.45f + branchIndex * 0.16f) * scale,
                    Mathf.Cos(radians) * 0.42f * scale);
                branch.transform.localScale = new Vector3(0.17f * scale, 1.05f * scale, 0.17f * scale);
                branch.transform.localRotation = Quaternion.Euler(
                    Mathf.Cos(radians) * 49f,
                    angle,
                    -Mathf.Sin(radians) * 49f);
                branch.GetComponent<Renderer>().sharedMaterial = trunkMaterial;
                Object.Destroy(branch.GetComponent<Collider>());
            }

            var canopyOffsets = new[]
            {
                new Vector3(0f, 4.95f, 0f),
                new Vector3(-1.16f, 4.55f, 0.52f),
                new Vector3(1.08f, 4.62f, 0.35f),
                new Vector3(0.18f, 4.48f, -1.02f),
                new Vector3(-0.12f, 5.7f, -0.12f)
            };
            for (var index = 0; index < canopyOffsets.Length; index++)
            {
                var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                canopy.name = index == canopyOffsets.Length - 1
                    ? "Threadwood sunlit crown"
                    : "Threadwood canopy lobe";
                canopy.transform.SetParent(tree.transform, false);
                canopy.transform.localPosition = canopyOffsets[index] * scale;
                var width = (index == 0 ? 2.2f : index == canopyOffsets.Length - 1 ? 1.42f : 1.66f) * scale;
                canopy.transform.localScale = new Vector3(width, width * 0.72f, width * 0.9f);
                canopy.GetComponent<Renderer>().sharedMaterial = index == canopyOffsets.Length - 1
                    ? alderCanopyHighlightMaterial
                    : alderCanopyMaterial;
                Object.Destroy(canopy.GetComponent<Collider>());
                FoliageSway.Attach(canopy, 0.72f + index * 0.08f);
            }
        }

        private static void CreateReedCluster(Vector3 position, float scale)
        {
            var cluster = new GameObject("Lantern Reeds");
            cluster.transform.position = position;
            for (var index = 0; index < 7; index++)
            {
                var reed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                reed.name = "Lantern reed stem";
                reed.transform.SetParent(cluster.transform, false);
                var angle = index * 2.14f;
                var height = (0.82f + index % 4 * 0.16f) * scale;
                reed.transform.localPosition = new Vector3(
                    Mathf.Sin(angle) * (0.35f + index % 3 * 0.18f),
                    height,
                    Mathf.Cos(angle) * (0.3f + index % 2 * 0.2f));
                reed.transform.localScale = new Vector3(0.045f, height, 0.045f);
                reed.transform.localRotation = Quaternion.Euler(
                    Mathf.Sin(angle) * 5f,
                    index * 31f,
                    Mathf.Cos(angle) * 6f);
                reed.GetComponent<Renderer>().sharedMaterial = reedMaterial;
                Object.Destroy(reed.GetComponent<Collider>());

                var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tip.name = "Lantern reed seedlight";
                tip.transform.SetParent(reed.transform, false);
                tip.transform.localPosition = new Vector3(0f, 1.03f, 0f);
                tip.transform.localScale = new Vector3(2.4f, 0.52f, 2.4f);
                tip.GetComponent<Renderer>().sharedMaterial = reedTipMaterial;
                Object.Destroy(tip.GetComponent<Collider>());
                FoliageSway.Attach(reed, 1.35f);
            }
        }

        private static void CreateSiltcallerShrine(Vector3 position)
        {
            var shrine = new GameObject("Siltward Cairn");
            shrine.transform.position = position;
            var stone = game.CreateMaterial(new Color(0.13f, 0.28f, 0.3f), 0.08f, 0.42f);
            var accent = new Color(0.27f, 0.88f, 0.82f);

            var basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basin.name = "Siltward cairn basin";
            basin.transform.SetParent(shrine.transform, false);
            basin.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            basin.transform.localScale = new Vector3(3.2f, 0.1f, 3.2f);
            basin.GetComponent<Renderer>().material = stone;
            Object.Destroy(basin.GetComponent<Collider>());

            var pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pool.name = "Siltward resonance pool";
            pool.transform.SetParent(shrine.transform, false);
            pool.transform.localPosition = new Vector3(0f, 0.21f, 0f);
            pool.transform.localScale = new Vector3(2.45f, 0.025f, 2.45f);
            var poolMaterial = game.CreateTransparentMaterial(new Color(accent.r, accent.g, accent.b, 0.52f), 0.64f);
            pool.GetComponent<Renderer>().material = poolMaterial;
            Object.Destroy(pool.GetComponent<Collider>());
            pool.AddComponent<RouteGlow>().Initialize(0.07f, position.x * 0.09f + position.z * 0.12f);
            WaterSurface.Attach(pool, poolMaterial, new Color(accent.r, accent.g, accent.b, 0.52f));

            for (var index = 0; index < 3; index++)
            {
                var angle = index * Mathf.PI * 2f / 3f + 0.28f;
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = "Siltward cairn pillar";
                pillar.transform.SetParent(shrine.transform, false);
                pillar.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 2.45f,
                    1.1f,
                    Mathf.Sin(angle) * 2.45f);
                pillar.transform.localScale = new Vector3(0.34f, 1.1f, 0.34f);
                pillar.transform.localRotation = Quaternion.Euler(8f, index * 120f, 11f);
                pillar.GetComponent<Renderer>().material = stone;
                Object.Destroy(pillar.GetComponent<Collider>());

                var glyph = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                glyph.name = "Siltward cairn glyph";
                glyph.transform.SetParent(pillar.transform, false);
                glyph.transform.localPosition = new Vector3(0f, 0.74f, 0f);
                glyph.transform.localScale = Vector3.one * 0.2f;
                glyph.GetComponent<Renderer>().material = game.CreateGlowMaterial(accent, 1.35f, 0.66f);
                Object.Destroy(glyph.GetComponent<Collider>());
                glyph.AddComponent<RouteGlow>().Initialize(0.1f, index * 0.8f + position.x * 0.04f);
            }

            for (var index = 0; index < 8; index++)
            {
                var mote = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mote.name = "Siltward drifting mote";
                mote.transform.SetParent(shrine.transform, false);
                var angle = index * Mathf.PI * 2f / 8f;
                mote.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * (0.65f + index % 3 * 0.22f),
                    0.55f + index % 2 * 0.38f,
                    Mathf.Sin(angle) * (0.65f + index % 3 * 0.22f));
                mote.transform.localScale = Vector3.one * (0.045f + index % 2 * 0.018f);
                mote.GetComponent<Renderer>().material = game.CreateGlowMaterial(
                    Color.Lerp(accent, Color.white, 0.24f),
                    1.2f,
                    0.62f);
                Object.Destroy(mote.GetComponent<Collider>());
                mote.AddComponent<AmbientMote>().Initialize(
                    mote.transform.localPosition,
                    0.14f + index % 3 * 0.03f,
                    0.8f + index % 4 * 0.12f,
                    index * 0.71f);
            }

            var light = shrine.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = accent;
            light.range = 5.8f;
            light.intensity = 0.72f;
            light.shadows = LightShadows.None;
            WorldVisuals.AddLabel(shrine.transform, "Siltward Cairn", new Vector3(0f, 2.65f, 0f), accent, 0.08f);
        }

        private static void CreateCinderPillar(Vector3 position, float scale)
        {
            var root = new GameObject("Cinder Shelf Cluster");
            root.transform.position = position;
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Cinder Shelf";
            pillar.transform.SetParent(root.transform, false);
            pillar.transform.localPosition = new Vector3(0f, 1.35f * scale, 0f);
            pillar.transform.localScale = new Vector3(1.75f * scale, 2.7f * scale, 1.42f * scale);
            pillar.transform.localRotation = Quaternion.Euler(2f, position.x * 1.7f, 4f);
            pillar.GetComponent<Renderer>().sharedMaterial = cinderRockMaterial;

            for (var index = 0; index < 3; index++)
            {
                var shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelf.name = "Cinder shelf shard";
                shelf.transform.SetParent(root.transform, false);
                var side = index == 0 ? -1f : 1f;
                shelf.transform.localPosition = new Vector3(
                    side * (0.86f + index * 0.22f) * scale,
                    (0.72f + index * 0.56f) * scale,
                    (index - 1) * 0.38f * scale);
                shelf.transform.localScale = new Vector3(
                    (0.78f + index * 0.18f) * scale,
                    (0.56f + index * 0.18f) * scale,
                    (0.9f - index * 0.09f) * scale);
                shelf.transform.localRotation = Quaternion.Euler(
                    index * 11f - 8f,
                    index * 47f + position.z,
                    side * (14f + index * 4f));
                shelf.GetComponent<Renderer>().sharedMaterial = cinderRockMaterial;
                Object.Destroy(shelf.GetComponent<Collider>());
            }

            var seam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seam.name = "Cinder ember seam";
            seam.transform.SetParent(root.transform, false);
            seam.transform.localPosition = new Vector3(0.18f * scale, 1.48f * scale, 0.73f * scale);
            seam.transform.localScale = new Vector3(0.12f * scale, 1.18f * scale, 0.05f * scale);
            seam.transform.localRotation = Quaternion.Euler(0f, -8f, 12f);
            seam.GetComponent<Renderer>().sharedMaterial = cinderGlowMaterial;
            Object.Destroy(seam.GetComponent<Collider>());
            seam.AddComponent<RouteGlow>().Initialize(0.055f, position.x * 0.04f);
        }

        private static void CreateWater(Vector3 position, Vector3 scale, Color color)
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "Stillwater";
            water.transform.position = position;
            water.transform.localScale = scale;
            var waterMaterial = game.CreateTransparentMaterial(color, 0.76f);
            water.GetComponent<Renderer>().material = waterMaterial;
            Object.Destroy(water.GetComponent<Collider>());
            WaterSurface.Attach(water, waterMaterial, color);
        }

        private static void CreateBridge(Vector3 position, float length)
        {
            var bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridge.name = "Threadwood Crossing";
            bridge.transform.position = position;
            bridge.transform.localScale = new Vector3(3f, 0.45f, length);
            bridge.GetComponent<Renderer>().material = trunkMaterial;
        }

        private static void CreateSeedspindle(Vector3 position)
        {
            var root = new GameObject("Seedspindle");
            root.transform.position = position;
            for (var index = 0; index < 3; index++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shard.transform.SetParent(root.transform, false);
                shard.transform.localPosition = new Vector3(Mathf.Sin(index * 2.1f) * 1.4f, 1.8f, Mathf.Cos(index * 2.1f) * 1.4f);
                shard.transform.localRotation = Quaternion.Euler(index * 20f, index * 120f, 18f);
                shard.transform.localScale = new Vector3(0.24f, 1.8f, 0.24f);
                shard.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.35f, 0.88f, 0.71f), 0.22f, 0.7f);
                Object.Destroy(shard.GetComponent<Collider>());
            }
            WorldVisuals.AddLabel(root.transform, "Seedspindle", new Vector3(0f, 3.8f, 0f), new Color(0.77f, 0.95f, 0.83f));
        }

        private static void CreateFenLantern(Vector3 position)
        {
            var lantern = new GameObject("Fen Lantern");
            lantern.transform.position = position;
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Fen lantern crooked post";
            pole.transform.SetParent(lantern.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            pole.transform.localScale = new Vector3(0.12f, 1.8f, 0.12f);
            pole.transform.localRotation = Quaternion.Euler(0f, position.x * 2f, 3.5f);
            pole.GetComponent<Renderer>().sharedMaterial = trunkMaterial;
            Object.Destroy(pole.GetComponent<Collider>());

            var bracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bracket.name = "Fen lantern copper bracket";
            bracket.transform.SetParent(lantern.transform, false);
            bracket.transform.localPosition = new Vector3(0.28f, 3.18f, 0f);
            bracket.transform.localScale = new Vector3(0.65f, 0.08f, 0.1f);
            bracket.transform.localRotation = Quaternion.Euler(0f, 0f, -7f);
            bracket.GetComponent<Renderer>().sharedMaterial = copperMaterial;
            Object.Destroy(bracket.GetComponent<Collider>());

            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "Fen lantern heart";
            glow.transform.SetParent(lantern.transform, false);
            glow.transform.localPosition = new Vector3(0.58f, 2.92f, 0f);
            glow.transform.localScale = new Vector3(0.32f, 0.48f, 0.32f);
            glow.GetComponent<Renderer>().sharedMaterial = reedTipMaterial;
            Object.Destroy(glow.GetComponent<Collider>());
            glow.AddComponent<RouteGlow>().Initialize(0.09f, position.x + position.z);

            for (var index = 0; index < 4; index++)
            {
                var cage = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cage.name = "Fen lantern cage";
                cage.transform.SetParent(lantern.transform, false);
                var angle = index * Mathf.PI * 0.5f;
                cage.transform.localPosition = new Vector3(
                    0.58f + Mathf.Cos(angle) * 0.24f,
                    2.92f,
                    Mathf.Sin(angle) * 0.24f);
                cage.transform.localScale = new Vector3(0.025f, 0.42f, 0.025f);
                cage.GetComponent<Renderer>().sharedMaterial = copperMaterial;
                Object.Destroy(cage.GetComponent<Collider>());
            }

            var light = lantern.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.29f, 0.85f, 0.75f);
            light.range = 7f;
            light.intensity = 1.08f;
            light.shadows = LightShadows.None;
        }

        private static void CreateAshSpire(Vector3 position)
        {
            var root = new GameObject("Broken Relay Spire");
            root.transform.position = position;
            var spire = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spire.name = "Broken Relay Spire";
            spire.transform.SetParent(root.transform, false);
            spire.transform.localPosition = new Vector3(0f, 3.3f, 0f);
            spire.transform.localScale = new Vector3(0.56f, 6.6f, 0.56f);
            spire.transform.localRotation = Quaternion.Euler(0f, position.z, 7f);
            spire.GetComponent<Renderer>().sharedMaterial = copperMaterial;
            for (var index = 0; index < 3; index++)
            {
                var prong = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prong.name = "Broken relay prong";
                prong.transform.SetParent(root.transform, false);
                var side = index % 2 == 0 ? -1f : 1f;
                prong.transform.localPosition = new Vector3(side * 0.5f, 5.5f - index * 0.72f, index * 0.18f);
                prong.transform.localScale = new Vector3(0.2f, 1.35f, 0.2f);
                prong.transform.localRotation = Quaternion.Euler(index * 8f, index * 37f, side * (28f + index * 7f));
                prong.GetComponent<Renderer>().sharedMaterial = index == 1 ? cinderGlowMaterial : copperMaterial;
                Object.Destroy(prong.GetComponent<Collider>());
            }

            WorldVisuals.AddLabel(root.transform, "Broken relay", new Vector3(0f, 7.1f, 0f), new Color(0.91f, 0.62f, 0.44f), 0.08f);
        }

        private static void CreateFarmTerrace(Vector3 position)
        {
            var terrace = new GameObject("Hearthgarden Terrace");
            terrace.transform.position = position;
            var foundation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foundation.transform.SetParent(terrace.transform, false);
            foundation.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            foundation.transform.localScale = new Vector3(13f, 0.32f, 8f);
            foundation.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.25f, 0.18f, 0.12f), 0f, 0.22f);
            WorldVisuals.AddLabel(terrace.transform, "Hearthgarden Terrace", new Vector3(0f, 1.2f, -4.1f), new Color(0.92f, 0.82f, 0.61f));
            FarmPlot.Create(game, position + new Vector3(-3.4f, 0f, 0f));
            FarmPlot.Create(game, position + new Vector3(0f, 0f, 0f));
            FarmPlot.Create(game, position + new Vector3(3.4f, 0f, 0f));
        }

        private static void CreateResources(ResourceKind kind, Color color, params Vector3[] positions)
        {
            foreach (var position in positions)
            {
                ResourceNode.Create(game, kind, position, color);
            }
        }

        private static void AddRegionSign(string title, string subtitle, Vector3 position, Color color)
        {
            var sign = new GameObject($"{title} Landmark");
            sign.transform.position = position;
            var monolith = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monolith.transform.SetParent(sign.transform, false);
            monolith.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            monolith.transform.localScale = new Vector3(0.35f, 3f, 0.35f);
            monolith.GetComponent<Renderer>().material = copperMaterial;
            WorldVisuals.AddLabel(sign.transform, title, new Vector3(0f, 3.5f, 0f), color, 0.15f);
            WorldVisuals.AddLabel(sign.transform, subtitle, new Vector3(0f, 3.08f, 0f), new Color(0.67f, 0.78f, 0.76f), 0.08f);
        }

        private static void BuildBoundaries()
        {
            var boundaryMaterial = game.CreateMaterial(new Color(0.12f, 0.25f, 0.28f), 0.1f, 0.4f);
            var boundaries = new[]
            {
                new Vector3(0f, 6f, TetherfallRegionPacks.WorldMaximumZ),
                new Vector3(0f, 6f, TetherfallRegionPacks.WorldMinimumZ),
                new Vector3(TetherfallRegionPacks.WorldMaximumX, 6f, 0f),
                new Vector3(TetherfallRegionPacks.WorldMinimumX, 6f, 0f)
            };
            foreach (var position in boundaries)
            {
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = "World Boundary";
                block.transform.position = position;
                block.transform.localScale = position.z == 0f
                    ? new Vector3(
                        2f,
                        12f,
                        TetherfallRegionPacks.WorldMaximumZ - TetherfallRegionPacks.WorldMinimumZ + 2f)
                    : new Vector3(
                        TetherfallRegionPacks.WorldMaximumX - TetherfallRegionPacks.WorldMinimumX + 2f,
                        12f,
                        2f);
                var renderer = block.GetComponent<Renderer>();
                renderer.sharedMaterial = boundaryMaterial;
                renderer.enabled = false;
            }
        }
    }

    public sealed class ResourceNode : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private ResourceKind kind;
        private int yield;
        private long respawnAtUtcTicks;
        private Renderer[] visualRenderers;
        private Collider[] colliders;
        private bool available = true;

        public string PersistenceId { get; private set; }
        public bool IsAvailable => available;
        public string InteractionPrompt
        {
            get
            {
                if (available)
                {
                    return $"Gather {NameOf(kind)}";
                }

                var remainingTicks = System.Math.Max(0L, respawnAtUtcTicks - DateTime.UtcNow.Ticks);
                var remainingSeconds = Mathf.Max(
                    1,
                    Mathf.CeilToInt((float)(remainingTicks / (double)System.TimeSpan.TicksPerSecond)));
                return $"{NameOf(kind)} regrows in {remainingSeconds}s";
            }
        }

        public static ResourceNode Create(TetherfallGame game, ResourceKind kind, Vector3 position, Color color)
        {
            var shape = kind == ResourceKind.Timber ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            var root = GameObject.CreatePrimitive(shape);
            root.name = $"{NameOf(kind)} Node";
            root.transform.position = position;
            root.transform.localScale = kind == ResourceKind.Timber
                ? new Vector3(0.48f, 1.5f, 0.48f)
                : new Vector3(1.05f, 1.05f, 1.05f);

            var node = root.AddComponent<ResourceNode>();
            node.game = game;
            node.kind = kind;
            node.PersistenceId = $"resource.{kind}@{Mathf.RoundToInt(position.x)}:{Mathf.RoundToInt(position.z)}";
            node.yield = kind == ResourceKind.Chimesalt ? 2 : 1;
            root.GetComponent<Renderer>().material = game.CreateMaterial(color, kind == ResourceKind.Chimesalt ? 0.35f : 0.04f, 0.5f);
            node.visualRenderers = root.GetComponentsInChildren<Renderer>();
            node.colliders = root.GetComponentsInChildren<Collider>();
            game.RegisterInteractable(node);
            game.RegisterResourceNode(node);

            WorldVisuals.AddLabel(root.transform, NameOf(kind), new Vector3(0f, 1.55f, 0f), new Color(0.76f, 0.88f, 0.83f), 0.08f);
            return node;
        }

        public static ResourceNode Create(
            TetherfallGame game,
            ResourceKind kind,
            Vector3 position,
            Color color,
            string persistenceId)
        {
            var node = Create(game, kind, position, color);
            if (!string.IsNullOrWhiteSpace(persistenceId))
            {
                node.PersistenceId = persistenceId;
            }

            return node;
        }

        private void Update()
        {
            if (!available && DateTime.UtcNow.Ticks >= respawnAtUtcTicks)
            {
                available = true;
                SetVisible(true);
                game.Notify($"{NameOf(kind)} has regrown.");
                game.OnResourceNodeStateChanged();
            }

            if (available)
            {
                transform.Rotate(0f, 17f * Time.deltaTime, 0f);
            }
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return available;
        }

        public void Interact(TetherfallGame owner)
        {
            if (!available)
            {
                return;
            }

            available = false;
            respawnAtUtcTicks = DateTime.UtcNow.AddSeconds(28f).Ticks;
            SetVisible(false);
            owner.RecordResourceNodeGathered();
            owner.AddResource(
                kind,
                yield,
                ResourceAcquisitionSource.WorldGather);
            owner.OnResourceNodeStateChanged();
        }

        public ResourceNodeSaveData WriteSaveData()
        {
            return new ResourceNodeSaveData
            {
                id = PersistenceId,
                respawnAtUtcTicks = respawnAtUtcTicks
            };
        }

        public void RestoreSaveData(ResourceNodeSaveData data)
        {
            respawnAtUtcTicks = System.Math.Max(0, data.respawnAtUtcTicks);
            available = respawnAtUtcTicks <= DateTime.UtcNow.Ticks;
            SetVisible(available);
        }

        private void SetVisible(bool isVisible)
        {
            foreach (var renderer in visualRenderers)
            {
                renderer.enabled = isVisible;
            }

            foreach (var itemCollider in colliders)
            {
                itemCollider.enabled = isVisible;
            }
        }

        private static string NameOf(ResourceKind resource)
        {
            return resource switch
            {
                ResourceKind.Timber => "Timber",
                ResourceKind.Stone => "Stone",
                ResourceKind.Resin => "Resin",
                ResourceKind.Chimesalt => "Chimesalt",
                ResourceKind.Herbs => "Herbs",
                ResourceKind.Ore => "Ore",
                ResourceKind.Fiber => "Fiber",
                _ => resource.ToString()
            };
        }
    }

    public sealed class FarmPlot : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private Renderer soilRenderer;
        private GameObject crop;
        private long matureAtUtcTicks;
        private bool growing;
        private bool ready;

        public string PersistenceId { get; private set; }

        public string InteractionPrompt
        {
            get
            {
                if (ready)
                {
                    return "Harvest glowreed";
                }

                if (!growing)
                {
                    return "Plant glowreed";
                }

                var remainingTicks = System.Math.Max(0L, matureAtUtcTicks - DateTime.UtcNow.Ticks);
                var remainingSeconds = Mathf.Max(
                    1,
                    Mathf.CeilToInt((float)(remainingTicks / (double)System.TimeSpan.TicksPerSecond)));
                return $"Glowreed matures in {remainingSeconds}s";
            }
        }

        public static FarmPlot Create(TetherfallGame game, Vector3 position)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "Glowreed Plot";
            root.transform.position = position + new Vector3(0f, 0.18f, 0f);
            root.transform.localScale = new Vector3(2.55f, 0.35f, 2.15f);
            var plot = root.AddComponent<FarmPlot>();
            plot.game = game;
            plot.PersistenceId = $"hearthgarden@{Mathf.RoundToInt(position.x)}:{Mathf.RoundToInt(position.z)}";
            plot.soilRenderer = root.GetComponent<Renderer>();
            plot.soilRenderer.material = game.CreateMaterial(new Color(0.19f, 0.12f, 0.07f), 0f, 0.12f);
            game.RegisterInteractable(plot);
            game.RegisterFarmPlot(plot);
            return plot;
        }

        private void Update()
        {
            if (growing && DateTime.UtcNow.Ticks >= matureAtUtcTicks)
            {
                FinishGrowth();
                game.Notify("A Hearthgarden glowreed plot is ready to harvest.");
                game.OnFarmPlotStateChanged();
            }
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return !growing;
        }

        public void Interact(TetherfallGame owner)
        {
            if (ready)
            {
                ready = false;
                matureAtUtcTicks = 0;
                RefreshVisuals();
                owner.AddResource(
                    ResourceKind.Herbs,
                    2,
                    ResourceAcquisitionSource.SettlementTransfer);
                owner.AddResource(
                    ResourceKind.Fiber,
                    1,
                    ResourceAcquisitionSource.SettlementTransfer);
                owner.OnFarmHarvested();
                owner.OnFarmPlotStateChanged();
                owner.Notify("Glowreed harvested. Plant again when you wish.");
                return;
            }

            if (growing)
            {
                return;
            }

            growing = true;
            matureAtUtcTicks = DateTime.UtcNow.AddSeconds(32).Ticks;
            RefreshVisuals();
            owner.OnFarmPlotStateChanged();
            owner.Notify("Glowreed planted. It will mature while the expedition continues.");
        }

        public FarmPlotSaveData WriteSaveData()
        {
            return new FarmPlotSaveData
            {
                id = PersistenceId,
                matureAtUtcTicks = matureAtUtcTicks
            };
        }

        public void RestoreSaveData(FarmPlotSaveData data)
        {
            matureAtUtcTicks = System.Math.Max(0, data.matureAtUtcTicks);
            growing = matureAtUtcTicks > DateTime.UtcNow.Ticks;
            ready = matureAtUtcTicks > 0 && !growing;
            RefreshVisuals();
        }

        private void FinishGrowth()
        {
            growing = false;
            ready = true;
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (growing || ready)
            {
                EnsureCrop();
            }

            if (crop != null)
            {
                crop.SetActive(ready);
            }

            soilRenderer.material.color = ready
                ? new Color(0.25f, 0.32f, 0.13f)
                : growing
                    ? new Color(0.28f, 0.39f, 0.16f)
                    : new Color(0.19f, 0.12f, 0.07f);
        }

        private void EnsureCrop()
        {
            if (crop != null)
            {
                return;
            }

            crop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crop.name = "Growing Glowreed";
            crop.transform.SetParent(transform, false);
            crop.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            crop.transform.localScale = new Vector3(0.45f, 0.95f, 0.45f);
            crop.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.55f, 0.89f, 0.29f), 0.08f, 0.5f);
            Object.Destroy(crop.GetComponent<Collider>());
            crop.SetActive(false);
        }
    }
}
