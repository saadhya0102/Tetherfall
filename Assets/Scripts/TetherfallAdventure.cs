using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public static class WorldExpansionBuilder
    {
        public static void Build(TetherfallGame game)
        {
            BuildAlderwakeServices(game);
            CityBuilder.Build(game);
            BountyBuilder.Build(game);
            ThreadwayBuilder.Build(game);
            BuildRegionalServices(game);
            BuildExtendedRoutes(game);
            RestorationSiteBuilder.Build(game);
            FenlightCausewayPresentationBuilder.Build(game);
            BuildTraversalRoutes(game);
            BuildDungeon(game);
            ModularDungeonBuilder.Build(game);
            TrialBuilder.Build(game);
            ScatterResourceExtensions(game);
            DressRegions(game);
            RouteEventBuilder.Build(game);
            FieldCacheBuilder.Build(game);
            CinderwoldArcBuilder.Build(game);
            CinderwoldArtBuilder.Build(game);
            TetherfallMainStoryBuilder.Build(game);
        }

        private static void BuildAlderwakeServices(TetherfallGame game)
        {
            SettlementRestorationNode.Create(game, new Vector3(-8f, 0f, 4f));
            VendorNpc.Create(game, "vendor.alderwake.broker", "Marn Kest", "route broker", new Vector3(-4f, 0.9f, 7f),
                "Bring Favors from the road. I keep tools that do not quit in the rain.");
            CraftingStation.Create(game, "Alderwake routework bench", new Vector3(-1f, 0f, 6f));

            WorldArt.SpawnKenney(game, "tent_detailedOpen", new Vector3(-13f, 0f, 3f), new Vector3(1.5f, 1.5f, 1.5f), new Color(0.24f, 0.45f, 0.42f), true);
            WorldArt.SpawnKenney(game, "campfire_stones", new Vector3(-10f, 0f, 1f), Vector3.one, new Color(0.8f, 0.49f, 0.25f));
            WorldArt.SpawnKenney(game, "fence_simple", new Vector3(-11f, 0f, 8f), new Vector3(1.25f, 1.25f, 1.25f), new Color(0.37f, 0.23f, 0.14f), true);
            WorldVisuals.AddLabel(game.transform, "Alderwake route station", new Vector3(-8f, 4f, 4f), new Color(0.93f, 0.81f, 0.58f));
        }

        private static void BuildRegionalServices(TetherfallGame game)
        {
            VendorNpc.Create(game, "vendor.morrowfen.provisioner", "Nemi Rill", "fen provisioner", new Vector3(102f, 0.9f, 19f),
                "The fen takes what travelers forget. Fiber and herbs are cheaper than rescue.");
            VendorNpc.Create(game, "vendor.cinderwold.salvager", "Daro Quell", "relay salvager", new Vector3(-108f, 0.9f, -18f),
                "Ore comes back from the ash shelves scarred, but routework does not mind scars.");
            CraftingStation.Create(game, "Cinderwold salvage bench", new Vector3(-104f, 0f, -21f));

            EnemyCamp.Create(game, "encounter.alderwake.mossglass", new Vector3(48f, 0f, 36f));
            EnemyCamp.Create(game, "encounter.morrowfen.fen_tithe", new Vector3(118f, 0f, 43f));
            EnemyCamp.Create(game, "encounter.cinderwold.relay_breakers", new Vector3(-126f, 0f, -41f));

            WorldArt.SpawnKenney(game, "bridge_wood", new Vector3(126f, 0.3f, 9f), new Vector3(1.7f, 1.2f, 1.7f), new Color(0.28f, 0.2f, 0.14f), true);
            WorldArt.SpawnKenney(game, "tent_detailedOpen", new Vector3(103f, 0f, 22f), Vector3.one * 1.25f, new Color(0.2f, 0.43f, 0.44f), true);
            WorldArt.SpawnKenney(game, "tent_detailedOpen", new Vector3(-111f, 0f, -21f), Vector3.one * 1.25f, new Color(0.46f, 0.23f, 0.18f), true);
        }

        private static void BuildExtendedRoutes(TetherfallGame game)
        {
            foreach (var placement in EncounterCatalog.ExtendedPlacements)
            {
                EnemyCamp.Create(game, placement.encounterId, placement.position, placement.persistenceId);
            }

            CreateRouteLandmark(game, "Northwake Observatory", new Vector3(8f, 0f, 109f), new Color(0.35f, 0.78f, 0.62f));
            CreateRouteLandmark(game, "Southroot Marker", new Vector3(-4f, 0f, -110f), new Color(0.45f, 0.75f, 0.5f));

            CreateResources(game, ResourceKind.Timber, new Color(0.48f, 0.31f, 0.15f),
                new Vector3(-61f, 0.7f, 84f), new Vector3(53f, 0.7f, 107f), new Vector3(67f, 0.7f, -83f),
                new Vector3(-52f, 0.7f, -111f));
            CreateResources(game, ResourceKind.Herbs, new Color(0.45f, 0.86f, 0.4f),
                new Vector3(108f, 0.7f, 91f), new Vector3(213f, 0.7f, 110f), new Vector3(141f, 0.7f, -103f),
                new Vector3(223f, 0.7f, -88f));
            CreateResources(game, ResourceKind.Ore, new Color(0.62f, 0.62f, 0.79f),
                new Vector3(-107f, 0.7f, 105f), new Vector3(-218f, 0.7f, 88f), new Vector3(-136f, 0.7f, -101f),
                new Vector3(-221f, 0.7f, -111f));
        }

        private static void CreateRouteLandmark(TetherfallGame game, string label, Vector3 position, Color color)
        {
            var root = new GameObject(label);
            root.transform.position = position;
            var baseStone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseStone.name = $"{label} base";
            baseStone.transform.SetParent(root.transform, false);
            baseStone.transform.localPosition = Vector3.up * 1.4f;
            baseStone.transform.localScale = new Vector3(1.35f, 1.4f, 1.35f);
            baseStone.GetComponent<Renderer>().material = game.CreateMaterial(Color.Lerp(color, Color.black, 0.55f), 0.08f, 0.28f);

            var beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = $"{label} Threadglass";
            beacon.transform.SetParent(root.transform, false);
            beacon.transform.localPosition = Vector3.up * 3.3f;
            beacon.transform.localScale = Vector3.one * 0.72f;
            beacon.GetComponent<Renderer>().material = game.CreateMaterial(color, 0.18f, 0.74f);
            Object.Destroy(beacon.GetComponent<Collider>());
            WorldVisuals.AddLabel(root.transform, label, new Vector3(0f, 4.25f, 0f), Color.Lerp(color, Color.white, 0.45f), 0.11f);
        }

        private static void BuildTraversalRoutes(TetherfallGame game)
        {
            var cliffPositions = new[]
            {
                new Vector3(-56f, 4f, -14f),
                new Vector3(58f, 5f, 73f),
                new Vector3(-52f, 5.5f, -86f),
                new Vector3(108f, 4.5f, -101f),
                new Vector3(214f, 6f, 79f),
                new Vector3(-214f, 5.5f, 73f)
            };
            for (var index = 0; index < cliffPositions.Length; index++)
            {
                var cliff = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cliff.name = "Climbable Rootface";
                cliff.transform.position = cliffPositions[index];
                cliff.transform.localScale = new Vector3(7f + index % 2 * 2f, 8f + index % 3 * 1.5f, 2.2f);
                cliff.GetComponent<Renderer>().material = game.CreateMaterial(
                    index < 3 ? new Color(0.32f, 0.38f, 0.25f) :
                    index < 5 ? new Color(0.23f, 0.36f, 0.38f) :
                    new Color(0.39f, 0.2f, 0.17f),
                    0f,
                    0.25f);
                cliff.AddComponent<ClimbableSurface>();
                WorldVisuals.AddLabel(
                    cliff.transform,
                    index % 2 == 0 ? "Rootface route" : "Survey shelf",
                    new Vector3(0f, 0.62f, 0f),
                    new Color(0.7f, 0.84f, 0.74f),
                    0.06f);
            }

            WaterVolume.Create(new Vector3(126f, 1f, 9f), new Vector3(88f, 3f, 31f));
            WaterVolume.Create(new Vector3(165f, 1f, -34f), new Vector3(33f, 3f, 22f));

            CreateWindLift(game, new Vector3(-131f, 2.5f, 8f), "Cinder Threadwind");
            CreateWindLift(game, new Vector3(62f, 2.5f, 98f), "Northwake Threadwind");
            CreateWindLift(game, new Vector3(203f, 2.5f, -98f), "Fen Threadwind");
        }

        private static void CreateWindLift(TetherfallGame game, Vector3 position, string label)
        {
            var updraft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            updraft.name = label;
            updraft.transform.position = position;
            updraft.transform.localScale = new Vector3(2.2f, 2.5f, 2.2f);
            updraft.GetComponent<Renderer>().material = game.CreateTransparentMaterial(new Color(0.39f, 0.82f, 0.75f, 0.32f), 0.62f);
            updraft.AddComponent<WindLift>();
            updraft.AddComponent<RouteGlow>().Initialize(0.08f, position.x + position.z);
            WorldVisuals.AddLabel(updraft.transform, label, new Vector3(0f, 3.2f, 0f), new Color(0.65f, 0.95f, 0.88f), 0.08f);
        }

        private static void BuildDungeon(TetherfallGame game)
        {
            var directorRoot = new GameObject("Skyvault Dungeon Director");
            var director = directorRoot.AddComponent<SkyvaultDungeon>();
            director.Initialize(game, new Vector3(0f, 2f, 206f), new Vector3(0f, 2f, 34f));
            DungeonGate.Create(game, director, new Vector3(121f, 0.9f, 31f));

            WorldArt.SpawnKenney(game, "rock_tallA", new Vector3(116f, 0f, 34f), new Vector3(2.2f, 2.2f, 2.2f), new Color(0.25f, 0.37f, 0.4f), true);
            WorldArt.SpawnKenney(game, "mushroom_redGroup", new Vector3(126f, 0f, 35f), Vector3.one * 1.4f, new Color(0.58f, 0.23f, 0.38f));
        }

        private static void ScatterResourceExtensions(TetherfallGame game)
        {
            CreateResources(game, ResourceKind.Herbs, new Color(0.45f, 0.86f, 0.4f),
                new Vector3(-9f, 0.7f, 29f), new Vector3(31f, 0.7f, -4f), new Vector3(96f, 0.7f, 5f),
                new Vector3(147f, 0.7f, 13f), new Vector3(-111f, 0.7f, 12f));
            CreateResources(game, ResourceKind.Ore, new Color(0.62f, 0.62f, 0.79f),
                new Vector3(119f, 0.7f, -8f), new Vector3(177f, 0.7f, 11f), new Vector3(-129f, 0.7f, -4f),
                new Vector3(-164f, 0.7f, 19f));
            CreateResources(game, ResourceKind.Fiber, new Color(0.74f, 0.66f, 0.37f),
                new Vector3(-22f, 0.7f, 8f), new Vector3(49f, 0.7f, 28f), new Vector3(109f, 0.7f, 42f),
                new Vector3(-91f, 0.7f, -17f));
        }

        private static void DressRegions(TetherfallGame game)
        {
            var alderwakeProps = new[]
            {
                new Vector3(-30f, 0f, -23f), new Vector3(22f, 0f, 30f), new Vector3(43f, 0f, -8f)
            };
            foreach (var position in alderwakeProps)
            {
                WorldArt.SpawnKenney(game, "tree_oak", position, Vector3.one * 1.6f, new Color(0.31f, 0.58f, 0.35f), true);
                WorldArt.SpawnKenney(game, "plant_bushLarge", position + new Vector3(2f, 0f, 1f), Vector3.one * 1.1f, new Color(0.35f, 0.68f, 0.4f));
            }

            var fenProps = new[]
            {
                new Vector3(92f, 0f, -8f), new Vector3(133f, 0f, 28f), new Vector3(173f, 0f, 16f)
            };
            foreach (var position in fenProps)
            {
                WorldArt.SpawnKenney(game, "rock_largeA", position, Vector3.one * 1.4f, new Color(0.27f, 0.43f, 0.45f), true);
                WorldArt.SpawnKenney(game, "plant_bushDetailed", position + new Vector3(2f, 0f, -1f), Vector3.one * 0.9f, new Color(0.28f, 0.52f, 0.51f));
            }

            var cinderProps = new[]
            {
                new Vector3(-93f, 0f, 4f), new Vector3(-139f, 0f, 20f), new Vector3(-177f, 0f, 14f)
            };
            foreach (var position in cinderProps)
            {
                WorldArt.SpawnKenney(game, "rock_tallA", position, Vector3.one * 1.7f, new Color(0.34f, 0.19f, 0.19f), true);
                WorldArt.SpawnKenney(game, "rock_smallA", position + new Vector3(-2f, 0f, 2f), Vector3.one * 0.9f, new Color(0.5f, 0.22f, 0.18f), true);
            }

            ScatterRegionProps(
                game,
                0f,
                "tree_tall",
                "tree_oak",
                "plant_bushLarge",
                new Color(0.28f, 0.56f, 0.34f),
                new Color(0.37f, 0.7f, 0.42f));
            ScatterRegionProps(
                game,
                160f,
                "tree_default",
                "rock_largeA",
                "plant_bushDetailed",
                new Color(0.24f, 0.43f, 0.45f),
                new Color(0.25f, 0.56f, 0.54f));
            ScatterRegionProps(
                game,
                -160f,
                "rock_tallA",
                "rock_largeA",
                "rock_smallA",
                new Color(0.38f, 0.18f, 0.16f),
                new Color(0.59f, 0.25f, 0.17f));
        }

        private static void ScatterRegionProps(
            TetherfallGame game,
            float regionCenterX,
            string primaryAsset,
            string secondaryAsset,
            string accentAsset,
            Color primaryTint,
            Color accentTint)
        {
            for (var index = 0; index < 18; index++)
            {
                var x = regionCenterX + Mathf.Sin(index * 2.31f + regionCenterX * 0.01f) * 61f;
                var z = -113f + index * 13.1f;
                var position = new Vector3(x, 0f, z);
                var asset = index % 3 == 0 ? secondaryAsset : primaryAsset;
                var prop = WorldArt.SpawnKenney(
                    game,
                    asset,
                    position,
                    Vector3.one * (1.05f + index % 4 * 0.16f),
                    primaryTint,
                    true);
                if (prop != null)
                {
                    prop.transform.rotation = Quaternion.Euler(0f, index * 47f % 360f, 0f);
                }

                WorldArt.SpawnKenney(
                    game,
                    accentAsset,
                    position + new Vector3(index % 2 == 0 ? 2.4f : -2.1f, 0f, 1.5f),
                    Vector3.one * (0.72f + index % 3 * 0.12f),
                    accentTint);
            }
        }

        private static void CreateResources(TetherfallGame game, ResourceKind kind, Color color, params Vector3[] positions)
        {
            foreach (var position in positions)
            {
                ResourceNode.Create(game, kind, position, color);
            }
        }
    }

    public static class WorldArt
    {
        public static GameObject SpawnKenney(
            TetherfallGame game,
            string assetName,
            Vector3 position,
            Vector3 scale,
            Color tint,
            bool solid = false)
        {
            var prefab = Resources.Load<GameObject>($"Art/KenneyNature/{assetName}");
            if (prefab == null)
            {
                return null;
            }

            var instance = Object.Instantiate(prefab, position, Quaternion.identity);
            instance.name = $"Kenney {assetName}";
            instance.transform.localScale = scale;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                ApplyRegionalTint(game, renderer, tint);
            }

            foreach (var collider in instance.GetComponentsInChildren<Collider>())
            {
                Object.Destroy(collider);
            }

            if (solid)
            {
                AddSimpleCollider(instance, assetName);
            }

            if (assetName.Contains("tree") ||
                assetName.Contains("plant") ||
                assetName.Contains("mushroom"))
            {
                FoliageSway.Attach(instance, assetName.Contains("tree") ? 1.15f : 2.4f);
            }

            return instance;
        }

        private static void ApplyRegionalTint(TetherfallGame game, Renderer renderer, Color tint)
        {
            var material = renderer.sharedMaterial;
            if (material == null || material.mainTexture == null)
            {
                renderer.material = game.CreateMaterial(tint, 0f, 0.32f);
                return;
            }

            var propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            var tintMultiplier = Color.Lerp(Color.white, tint, 0.72f);
            if (material.HasProperty("_Color"))
            {
                propertyBlock.SetColor("_Color", material.color * tintMultiplier);
            }
            else if (material.HasProperty("_BaseColor"))
            {
                propertyBlock.SetColor("_BaseColor", material.color * tintMultiplier);
            }

            renderer.SetPropertyBlock(propertyBlock);
        }

        private static void AddSimpleCollider(GameObject instance, string assetName)
        {
            if (assetName.Contains("tree"))
            {
                var capsule = instance.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0f, 1.45f, 0f);
                capsule.height = 2.9f;
                capsule.radius = 0.34f;
                return;
            }

            var box = instance.AddComponent<BoxCollider>();
            if (assetName.Contains("bridge"))
            {
                box.center = new Vector3(0f, 0.14f, 0f);
                box.size = new Vector3(2.4f, 0.28f, 1.2f);
            }
            else if (assetName.Contains("fence"))
            {
                box.center = new Vector3(0f, 0.45f, 0f);
                box.size = new Vector3(2f, 0.9f, 0.2f);
            }
            else if (assetName.Contains("tent"))
            {
                box.center = new Vector3(0f, 0.65f, 0f);
                box.size = new Vector3(1.45f, 1.3f, 1.45f);
            }
            else
            {
                box.center = new Vector3(0f, 0.45f, 0f);
                box.size = new Vector3(1.15f, 0.9f, 1.15f);
            }
        }
    }

    public sealed class CraftingStation : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private string stationName;

        public string StationName => stationName;
        public string InteractionPrompt => $"Use {stationName}";

        public static CraftingStation Create(TetherfallGame game, string name, Vector3 position)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = name;
            root.transform.position = position + Vector3.up * 0.65f;
            root.transform.localScale = new Vector3(2.1f, 1.3f, 1.1f);
            var station = root.AddComponent<CraftingStation>();
            station.game = game;
            station.stationName = name;
            root.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.36f, 0.2f, 0.12f), 0.1f, 0.3f);
            game.RegisterInteractable(station);
            game.RegisterCraftingStation(station);
            WorldVisuals.AddLabel(root.transform, "Routework bench", new Vector3(0f, 1.25f, 0f), new Color(0.91f, 0.77f, 0.57f), 0.08f);
            return station;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            owner.OpenCrafting(stationName);
        }
    }

    public sealed class VendorNpc : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private Transform visualRoot;
        private HumanProceduralAnimator proceduralAnimator;

        public string VendorId { get; private set; }
        public string VendorName { get; private set; }
        public string Greeting { get; private set; }
        public string InteractionPrompt => $"Trade with {VendorName}";

        public static VendorNpc Create(
            TetherfallGame game,
            string vendorId,
            string name,
            string role,
            Vector3 position,
            string greeting,
            bool highDetail = true)
        {
            var root = new GameObject(name);
            root.name = name;
            root.transform.position = position;
            root.transform.localScale = new Vector3(0.78f, 1f, 0.78f);
            var vendor = root.AddComponent<VendorNpc>();
            vendor.game = game;
            vendor.VendorId = vendorId;
            vendor.VendorName = name;
            vendor.Greeting = greeting;
            vendor.visualRoot = highDetail
                ? TetherfallCityResidentArt.Build(
                    game,
                    root.transform,
                    new Color(0.68f, 0.43f, 0.25f),
                    role,
                    $"{vendorId}.{name}")
                : TetherfallCityResidentArt.BuildCrowd(
                    game,
                    root.transform,
                    new Color(0.68f, 0.43f, 0.25f),
                    $"{vendorId}.{name}");
            vendor.proceduralAnimator = TetherfallHumanAnimation.Attach(vendor.visualRoot, game);
            vendor.proceduralAnimator?.SetIdleVariant(HumanIdleVariant.Working);
            game.RegisterInteractable(vendor);
            WorldVisuals.AddLabel(root.transform, name, new Vector3(0f, 1.5f, 0f), new Color(0.95f, 0.84f, 0.67f));
            WorldVisuals.AddLabel(root.transform, role, new Vector3(0f, 1.26f, 0f), new Color(0.68f, 0.8f, 0.76f), 0.08f);
            return vendor;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            proceduralAnimator?.TriggerVendorGesture();
            owner.OpenVendor(this);
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            var playerDistanceSquared = game.PlayerTransform != null
                ? (game.PlayerTransform.position - transform.position).sqrMagnitude
                : float.PositiveInfinity;
            var playerInAnimationRange = playerDistanceSquared <= 55f * 55f;
            var playerInPresentationRange = playerDistanceSquared <= 95f * 95f;
            if (visualRoot != null &&
                visualRoot.gameObject.activeSelf != playerInPresentationRange)
            {
                visualRoot.gameObject.SetActive(playerInPresentationRange);
            }

            if (proceduralAnimator != null &&
                proceduralAnimator.enabled != playerInAnimationRange)
            {
                proceduralAnimator.enabled = playerInAnimationRange;
            }

            if (game.GameplayPaused || !playerInAnimationRange)
            {
                return;
            }

            var toPlayer = game.PlayerTransform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.1f && toPlayer.sqrMagnitude <= 12.25f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(toPlayer.normalized),
                    Time.deltaTime * 4.5f);
            }
        }
    }

    public sealed class SettlementRestorationNode : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private readonly List<GameObject> repairPieces = new List<GameObject>();
        private int tier;

        public string InteractionPrompt => tier >= 3 ? "Visit restored Alderwake" : "Plan Alderwake restoration";

        public static SettlementRestorationNode Create(TetherfallGame game, Vector3 position)
        {
            var root = new GameObject("Alderwake Restoration");
            root.transform.position = position;
            var node = root.AddComponent<SettlementRestorationNode>();
            node.game = game;
            game.RegisterInteractable(node);
            game.RegisterSettlement(node);

            var damaged = GameObject.CreatePrimitive(PrimitiveType.Cube);
            damaged.name = "Frayed route shelter";
            damaged.transform.SetParent(root.transform, false);
            damaged.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            damaged.transform.localScale = new Vector3(5.5f, 2.4f, 2.7f);
            damaged.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.19f, 0.16f, 0.15f), 0f, 0.21f);
            Object.Destroy(damaged.GetComponent<Collider>());
            WorldVisuals.AddLabel(root.transform, "Frayed station", new Vector3(0f, 3.4f, 0f), new Color(0.88f, 0.7f, 0.52f));
            return node;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            owner.OpenSettlement();
        }

        public void RestoreToTier(int restoredTier)
        {
            tier = Mathf.Clamp(restoredTier, 0, 3);
            while (repairPieces.Count < tier)
            {
                var index = repairPieces.Count;
                var piece = GameObject.CreatePrimitive(index == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                piece.name = "Alderwake repair";
                piece.transform.SetParent(transform, false);
                piece.transform.localPosition = index switch
                {
                    0 => new Vector3(-2.4f, 1.8f, 0.8f),
                    1 => new Vector3(2.4f, 1.2f, -0.5f),
                    _ => new Vector3(0f, 3.1f, 0f)
                };
                piece.transform.localScale = index switch
                {
                    0 => new Vector3(0.3f, 2.2f, 0.3f),
                    1 => new Vector3(3.4f, 0.35f, 0.35f),
                    _ => new Vector3(0.7f, 0.7f, 0.7f)
                };
                piece.GetComponent<Renderer>().material = game.CreateMaterial(
                    index == 2 ? new Color(0.33f, 0.9f, 0.75f) : new Color(0.71f, 0.43f, 0.22f),
                    index == 2 ? 0.2f : 0.1f,
                    0.5f);
                Object.Destroy(piece.GetComponent<Collider>());
                repairPieces.Add(piece);
            }
        }
    }

    public sealed class WaterVolume : MonoBehaviour
    {
        private readonly HashSet<PlayerAvatar> occupants = new HashSet<PlayerAvatar>();

        public static WaterVolume Create(Vector3 position, Vector3 size)
        {
            var root = new GameObject("Swimmable Stillwater");
            root.transform.position = position;
            var collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
            return root.AddComponent<WaterVolume>();
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerAvatar>();
            if (player != null)
            {
                occupants.Add(player);
                player.EnterWaterVolume(this);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            var player = other.GetComponent<PlayerAvatar>();
            if (player != null)
            {
                occupants.Add(player);
                player.EnterWaterVolume(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponent<PlayerAvatar>();
            if (player != null)
            {
                occupants.Remove(player);
                player.ExitWaterVolume(this);
            }
        }

        private void OnDisable()
        {
            foreach (var player in occupants)
            {
                if (player != null)
                {
                    player.ExitWaterVolume(this);
                }
            }

            occupants.Clear();
        }
    }

    public sealed class ClimbableSurface : MonoBehaviour
    {
    }

    public sealed class WindLift : MonoBehaviour
    {
        private float launchVelocity = 5.6f;

        public void Initialize(float baseLaunchVelocity)
        {
            launchVelocity = Mathf.Max(0f, baseLaunchVelocity);
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerAvatar>();
            if (player != null)
            {
                player.ApplyWindLift(launchVelocity);
            }
        }

        private void Awake()
        {
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CapsuleCollider>();
            }

            collider.isTrigger = true;
        }
    }

    public sealed class DungeonGate : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private SkyvaultDungeon dungeon;

        public string InteractionPrompt => "Enter the Skyvault";

        public static DungeonGate Create(TetherfallGame game, SkyvaultDungeon director, Vector3 position)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "Skyvault gate";
            root.transform.position = position;
            root.transform.localScale = new Vector3(2.2f, 3.1f, 2.2f);
            var gate = root.AddComponent<DungeonGate>();
            gate.game = game;
            gate.dungeon = director;
            root.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.21f, 0.62f, 0.65f), 0.2f, 0.65f);
            game.RegisterInteractable(gate);
            game.RegisterDungeonGate(gate);
            WorldVisuals.AddLabel(root.transform, "Skyvault gate", new Vector3(0f, 3.6f, 0f), new Color(0.71f, 0.95f, 0.9f));
            return gate;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            dungeon.Enter();
        }
    }

    public sealed class SkyvaultDungeon : MonoBehaviour, IDungeonBossArena
    {
        private TetherfallGame game;
        private Vector3 entrance;
        private Vector3 returnPoint;
        private readonly List<ResonancePylon> pylons = new List<ResonancePylon>();
        private GameObject sealDoor;
        private DungeonBoss boss;
        private int activePylons;
        private int swarmEnemies;
        private bool waitingForSwarm;
        private bool exitCreated;
        private bool built;

        public void Initialize(TetherfallGame owner, Vector3 entrancePosition, Vector3 returnPosition)
        {
            game = owner;
            entrance = entrancePosition;
            returnPoint = returnPosition;
            BuildDungeon();
            game.RegisterSkyvault(this);
        }

        public void Enter()
        {
            game.Player.Teleport(entrance);
            game.Notify("The Skyvault shifts behind you. Wake its resonance pylons.");
        }

        public void Exit()
        {
            game.Player.Teleport(returnPoint);
            game.Notify("You return to Morrowfen.");
        }

        public void ActivatePylon(ResonancePylon pylon)
        {
            if (pylon.IsActive)
            {
                return;
            }

            if (waitingForSwarm)
            {
                game.Notify("The current room is still under pressure. Disperse the swarm.");
                return;
            }

            var pylonIndex = pylons.IndexOf(pylon);
            if (pylonIndex != activePylons)
            {
                var expected = pylons[Mathf.Clamp(activePylons, 0, pylons.Count - 1)].PylonName;
                game.Notify($"The sequence rejects that tone. Listen for {expected} first.");
                return;
            }

            pylon.SetActiveState(true);
            activePylons++;
            game.OnDungeonPylonActivated();
            SpawnRoomSwarm(2 + activePylons, pylon.transform.position);
        }

        public string GetPylonPrompt(ResonancePylon pylon)
        {
            if (pylon.IsActive)
            {
                return "Pylon stabilized";
            }

            if (waitingForSwarm)
            {
                return "Pylon held by the active swarm";
            }

            return pylons.IndexOf(pylon) == activePylons
                ? $"Stabilize {pylon.PylonName}"
                : $"Read the {pylon.PylonName} sequence";
        }

        public Transform GetGuidanceTarget()
        {
            if (activePylons < pylons.Count)
            {
                return pylons[activePylons].transform;
            }

            return boss != null && boss.gameObject.activeInHierarchy ? boss.transform : null;
        }

        public bool WaitingForSwarm => waitingForSwarm;
        public int RemainingSwarmEnemies => swarmEnemies;

        public void RestoreState(
            int restoredPylons,
            bool restoredBossDefeated,
            bool restoredWaitingForSwarm = false,
            int restoredSwarmEnemies = 0)
        {
            activePylons = Mathf.Clamp(restoredPylons, 0, pylons.Count);
            waitingForSwarm = false;
            swarmEnemies = 0;
            for (var index = 0; index < pylons.Count; index++)
            {
                pylons[index].SetActiveState(index < activePylons);
            }

            if (restoredBossDefeated)
            {
                sealDoor.SetActive(false);
                boss.SetRestoredDefeated();
                CreateExit();
                return;
            }

            if (restoredWaitingForSwarm &&
                restoredSwarmEnemies > 0 &&
                activePylons > 0)
            {
                sealDoor.SetActive(true);
                boss.gameObject.SetActive(false);
                var pylonIndex = Mathf.Clamp(activePylons - 1, 0, pylons.Count - 1);
                SpawnRoomSwarm(restoredSwarmEnemies, pylons[pylonIndex].transform.position);
                return;
            }

            if (activePylons >= pylons.Count)
            {
                OpenBossRoom();
                return;
            }

            sealDoor.SetActive(true);
            boss.gameObject.SetActive(false);
        }

        private void SpawnRoomSwarm(int count, Vector3 center)
        {
            waitingForSwarm = true;
            swarmEnemies = count;
            game.Notify($"The pylon answers with a swarm of {count} vault echoes.");
            for (var index = 0; index < count; index++)
            {
                var angle = index * Mathf.PI * 2f / count;
                Enemy.Create(
                    game,
                    null,
                    activePylons == 3
                        ? "enemy.cinder_echo"
                        : activePylons == 2
                            ? "enemy.vault_wisp"
                            : "enemy.mossglass_stalker",
                    center + new Vector3(Mathf.Cos(angle) * 4.5f, 0f, Mathf.Sin(angle) * 4.5f),
                    OnSwarmEnemyDefeated);
            }
        }

        private void OnSwarmEnemyDefeated(Enemy enemy)
        {
            swarmEnemies = Mathf.Max(0, swarmEnemies - 1);
            if (swarmEnemies > 0)
            {
                return;
            }

            waitingForSwarm = false;
            if (activePylons >= pylons.Count)
            {
                OpenBossRoom();
                return;
            }

            game.Notify($"Room stable. {pylons.Count - activePylons} resonance puzzle(s) remain.");
        }

        private void OpenBossRoom()
        {
            sealDoor.SetActive(false);
            boss.ResetForEncounter();
            boss.gameObject.SetActive(true);
            game.RegisterDungeonBoss(boss);
            game.Notify("The vault seal opens. The Warden notices your intrusion.");
        }

        public void OnBossDefeated()
        {
            game.OnDungeonBossDefeated();
            CreateExit();
        }

        public bool CanDamageBoss(DungeonBoss activeBoss)
        {
            return true;
        }

        public void OnBossPhaseThreshold(DungeonBoss activeBoss, int phase)
        {
        }

        void IDungeonBossArena.OnBossDefeated(DungeonBoss activeBoss)
        {
            OnBossDefeated();
        }

        private void CreateExit()
        {
            if (exitCreated)
            {
                return;
            }

            exitCreated = true;
            var exit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            exit.name = "Skyvault exit";
            exit.transform.position = new Vector3(0f, 1.4f, 262f);
            exit.transform.localScale = new Vector3(2f, 1.4f, 2f);
            exit.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.49f, 0.93f, 0.76f), 0.2f, 0.7f);
            var exitGate = exit.AddComponent<DungeonExit>();
            exitGate.Initialize(game, this);
            game.RegisterInteractable(exitGate);
            WorldVisuals.AddLabel(exit.transform, "Return route", new Vector3(0f, 2.3f, 0f), new Color(0.77f, 0.96f, 0.84f));
        }

        private void BuildDungeon()
        {
            if (built)
            {
                return;
            }

            built = true;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Skyvault Floor";
            ground.transform.position = new Vector3(0f, -1f, 225f);
            ground.transform.localScale = new Vector3(62f, 2f, 96f);
            ground.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.13f, 0.16f, 0.24f), 0.15f, 0.4f);

            CreateWall(new Vector3(0f, 5f, 177f), new Vector3(62f, 10f, 2f));
            CreateWall(new Vector3(0f, 5f, 273f), new Vector3(62f, 10f, 2f));
            CreateWall(new Vector3(-31f, 5f, 225f), new Vector3(2f, 10f, 96f));
            CreateWall(new Vector3(31f, 5f, 225f), new Vector3(2f, 10f, 96f));

            CreatePylon(new Vector3(-16f, 0f, 206f), "Root Resonance");
            CreatePylon(new Vector3(15f, 0f, 222f), "Current Resonance");
            CreatePylon(new Vector3(-8f, 0f, 237f), "Cinder Resonance");
            CreateRoomDivider(214f);
            CreateRoomDivider(230f);
            CreateRoomClue(new Vector3(0f, 0.05f, 199f), "Root wakes Current. Current carries Cinder.");
            CreateRoomClue(new Vector3(0f, 0.05f, 219f), "Hold the room until the answering echoes are quiet.");
            CreateRoomClue(new Vector3(0f, 0.05f, 235f), "The final tone opens the Warden's seal.");

            sealDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sealDoor.name = "Skyvault Seal";
            sealDoor.transform.position = new Vector3(0f, 4.5f, 247f);
            sealDoor.transform.localScale = new Vector3(20f, 9f, 1.2f);
            sealDoor.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.31f, 0.18f, 0.39f), 0.2f, 0.35f);

            boss = DungeonBoss.Create(
                game,
                this,
                BossCatalog.Get("boss.skyvault_warden"),
                new Vector3(0f, 1.6f, 257f));
            boss.gameObject.SetActive(false);
            WorldVisuals.AddLabel(transform, "The Skyvault", new Vector3(0f, 7f, 206f), new Color(0.75f, 0.87f, 0.97f), 0.16f);
        }

        private void CreatePylon(Vector3 position, string pylonName)
        {
            var pylon = ResonancePylon.Create(game, this, position, pylonName);
            pylons.Add(pylon);
        }

        private void CreateWall(Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.12f, 0.11f, 0.17f), 0.08f, 0.2f);
        }

        private void CreateRoomDivider(float z)
        {
            CreateWall(new Vector3(-19f, 3.2f, z), new Vector3(24f, 6.4f, 1f));
            CreateWall(new Vector3(19f, 3.2f, z), new Vector3(24f, 6.4f, 1f));
        }

        private void CreateRoomClue(Vector3 position, string clue)
        {
            var tablet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tablet.name = "Skyvault clue tablet";
            tablet.transform.position = position;
            tablet.transform.localScale = new Vector3(8f, 0.12f, 2.3f);
            tablet.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.19f, 0.28f, 0.38f), 0.2f, 0.5f);
            Object.Destroy(tablet.GetComponent<Collider>());
            WorldVisuals.AddLabel(tablet.transform, clue, new Vector3(0f, 1.1f, 0f), new Color(0.65f, 0.9f, 0.9f), 0.055f);
        }
    }

    public sealed class ResonancePylon : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private SkyvaultDungeon dungeon;
        private Renderer body;

        public bool IsActive { get; private set; }
        public string PylonName { get; private set; }
        public string InteractionPrompt => dungeon.GetPylonPrompt(this);

        public static ResonancePylon Create(TetherfallGame game, SkyvaultDungeon dungeon, Vector3 position, string label)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = label;
            root.transform.position = position + Vector3.up * 2.4f;
            root.transform.localScale = new Vector3(0.8f, 2.4f, 0.8f);
            var pylon = root.AddComponent<ResonancePylon>();
            pylon.game = game;
            pylon.dungeon = dungeon;
            pylon.PylonName = label;
            pylon.body = root.GetComponent<Renderer>();
            pylon.body.material = game.CreateMaterial(new Color(0.24f, 0.31f, 0.46f), 0.15f, 0.5f);
            game.RegisterInteractable(pylon);
            WorldVisuals.AddLabel(root.transform, label, new Vector3(0f, 2.6f, 0f), new Color(0.78f, 0.9f, 0.95f), 0.08f);
            return pylon;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return !IsActive;
        }

        public void Interact(TetherfallGame owner)
        {
            dungeon.ActivatePylon(this);
        }

        public void SetActiveState(bool active)
        {
            IsActive = active;
            body.material.color = active ? new Color(0.32f, 0.91f, 0.75f) : new Color(0.24f, 0.31f, 0.46f);
        }
    }

    public sealed class DungeonExit : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private SkyvaultDungeon dungeon;

        public string InteractionPrompt => "Leave the Skyvault";

        public void Initialize(TetherfallGame owner, SkyvaultDungeon director)
        {
            game = owner;
            dungeon = director;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            dungeon.Exit();
        }
    }

    public interface IDungeonBossArena
    {
        bool CanDamageBoss(DungeonBoss activeBoss);
        void OnBossPhaseThreshold(DungeonBoss activeBoss, int phase);
        void OnBossDefeated(DungeonBoss activeBoss);
    }

    public sealed class DungeonBoss : MonoBehaviour, IExpeditionDamageable
    {
        private TetherfallGame game;
        private IDungeonBossArena arena;
        private BossDefinition definition;
        private CombatPoiseProfile poiseProfile;
        private Renderer body;
        private BossProceduralAnimator proceduralAnimator;
        private int health;
        private int maxHealth;
        private readonly List<Enemy> summonedMinions = new List<Enemy>();
        private float nextActionAt;
        private float strikeAt;
        private float staggerUntil;
        private float exposedUntil;
        private float poisePressure;
        private float lastPoiseImpactAt = float.NegativeInfinity;
        private float outgoingDamageMultiplier = 1f;
        private float nextShieldNoticeAt;
        private bool telegraphing;
        private bool defeated;
        private bool exposeVisualActive;
        private bool repeatEncounterScaling;
        private int phase = 1;
        private int threatBand = 1;
        private int phaseThresholdIndex;
        private int actionIndex;
        private BossActionKind queuedAction;
        private readonly List<Vector3> cinderfallTargets = new List<Vector3>();
        private readonly List<GameObject> activeTelegraphs = new List<GameObject>();
        private readonly RaycastHit[] movementHitBuffer = new RaycastHit[32];
        private readonly RaycastHit[] attackPathHitBuffer = new RaycastHit[24];
        private Vector3 spawnPosition;

        public bool IsDefeated => defeated;
        public string BossId => definition != null ? definition.id : string.Empty;
        public string BossName => definition != null ? definition.displayName : "Unknown boss";
        public int Phase => Mathf.Max(1, phase);
        public int ThreatBand => Mathf.Clamp(threatBand, 1, 5);
        public int Health => Mathf.Max(0, health);
        public int MaxHealth => Mathf.Max(
            1,
            maxHealth > 0
                ? maxHealth
                : definition != null
                    ? definition.maxHealth
                    : 1);
        public bool IsStaggered => Time.time < staggerUntil;
        public bool IsExposed => Time.time < exposedUntil;
        public bool IsWindupPoiseProtected => telegraphing;
        public float Poise => Mathf.Clamp(poisePressure, 0f, PoiseThreshold);
        public float PoiseThreshold => Mathf.Max(1f, poiseProfile.Threshold);
        public float StaggerRemaining => Mathf.Max(0f, staggerUntil - Time.time);
        public float ExposeRemaining => Mathf.Max(0f, exposedUntil - Time.time);

        public static DungeonBoss Create(
            TetherfallGame game,
            IDungeonBossArena arena,
            BossDefinition definition,
            Vector3 position)
        {
            return Create(game, arena, definition, position, false);
        }

        public static DungeonBoss Create(
            TetherfallGame game,
            IDungeonBossArena arena,
            BossDefinition definition,
            Vector3 position,
            bool repeatEncounter)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = definition != null ? definition.displayName : "Dungeon boss";
            root.transform.position = position;
            root.transform.localScale = new Vector3(2.8f, 3.3f, 2.8f);
            var boss = root.AddComponent<DungeonBoss>();
            boss.game = game;
            boss.arena = arena;
            boss.definition = definition;
            boss.repeatEncounterScaling = repeatEncounter;
            boss.ApplyEncounterScaling();
            boss.health = boss.MaxHealth;
            boss.poiseProfile = TetherfallCombatQualityRules.GetBossPoiseProfile();
            boss.spawnPosition = position;
            boss.body = root.GetComponent<Renderer>();
            boss.body.material = game.CreateMaterial(
                definition != null ? definition.baseColor : new Color(0.34f, 0.2f, 0.5f),
                0.18f,
                0.55f);
            TetherfallBossArtOverhaul.Build(game, root.transform, definition);
            boss.proceduralAnimator = TetherfallProceduralAnimation.AttachBoss(game, root.transform);
            return boss;
        }

        private void Update()
        {
            if (defeated || game == null || game.Player == null)
            {
                return;
            }

            if (game.GameplayPaused)
            {
                PauseCombatTimers();
                return;
            }

            RecoverPoisePressure();
            if (exposeVisualActive && !IsExposed)
            {
                exposeVisualActive = false;
                RecoverBodyColor();
            }

            var playerPosition = game.PlayerTransform.position;
            var horizontal = playerPosition - transform.position;
            horizontal.y = 0f;
            var distance = horizontal.magnitude;
            if (distance > 60f && !telegraphing)
            {
                return;
            }

            if (Time.time < staggerUntil)
            {
                return;
            }

            if (!telegraphing && distance > 5.5f)
            {
                MoveCollisionAware(horizontal.normalized * GetMoveSpeed() * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(horizontal.normalized),
                    Time.deltaTime * 5f);
            }

            if (!telegraphing && Time.time >= nextActionAt)
            {
                queuedAction = ChooseAction(distance);
                telegraphing = true;
                strikeAt = Time.time + GetActionTelegraphSeconds(queuedAction);
                proceduralAnimator?.BeginWindup(
                    GetActionTelegraphSeconds(queuedAction),
                    phase == 1 ? 1f : 1.3f);
                body.material.color = queuedAction == BossActionKind.ShardVolley
                    ? new Color(0.48f, 0.74f, 0.96f)
                    : queuedAction == BossActionKind.Threadquake || queuedAction == BossActionKind.Cinderfall
                        ? new Color(0.96f, 0.42f, 0.26f)
                        : new Color(0.91f, 0.49f, 0.63f);
                CreateTelegraph(queuedAction);
                return;
            }

            if (telegraphing && Time.time >= strikeAt)
            {
                telegraphing = false;
                RecoverBodyColor();
                proceduralAnimator?.TriggerImpact(phase == 1 ? 1f : 1.35f);
                ResolveAction(distance, horizontal);
                activeTelegraphs.Clear();
                nextActionAt = Time.time + (phase == 1 ? 2.8f : 1.9f);
            }
        }

        private BossActionKind ChooseAction(float distance)
        {
            var index = actionIndex++;
            if (definition != null && definition.id == "boss.skyvault_warden")
            {
                if (phase == 1)
                {
                    return distance > 7.5f || index % 3 == 1
                        ? BossActionKind.ShardVolley
                        : BossActionKind.Sweep;
                }

                return (index % 3) switch
                {
                    0 => BossActionKind.Threadquake,
                    1 => BossActionKind.ShardVolley,
                    _ => BossActionKind.Sweep
                };
            }

            if (definition == null)
            {
                return BossActionKind.Sweep;
            }

            var actions = phase == 1 || definition.phaseTwoActions.Count == 0
                ? definition.phaseOneActions
                : definition.phaseTwoActions;
            if (actions.Count == 0)
            {
                return BossActionKind.Sweep;
            }

            if (distance > 10f && actions.Contains(BossActionKind.RailCharge))
            {
                return BossActionKind.RailCharge;
            }

            return actions[index % actions.Count];
        }

        private void ResolveAction(float distance, Vector3 horizontal)
        {
            switch (queuedAction)
            {
                case BossActionKind.Sweep:
                    if (distance < 8f && HasClearAttackPath())
                    {
                        game.DamagePlayer(ScaleOutgoingDamage(18 + phase * 3), BossName);
                    }

                    break;

                case BossActionKind.ShardVolley:
                {
                    var origin = transform.position + Vector3.up * 2.1f;
                    var target = game.PlayerTransform.position + Vector3.up * 0.9f;
                    var direction = (target - origin).normalized;
                    var shotCount = phase == 1 ? 3 : 5;
                    for (var index = 0; index < shotCount; index++)
                    {
                        var spread = (index - (shotCount - 1) * 0.5f) * 6.5f;
                        EnemyProjectile.Create(
                            game,
                            origin,
                            Quaternion.AngleAxis(spread, Vector3.up) * direction,
                            ScaleOutgoingDamage(9 + phase * 3),
                            18f,
                            new Color(0.55f, 0.47f, 0.95f),
                            BossName);
                    }

                    break;
                }

                case BossActionKind.Threadquake:
                {
                    EnemyShockwave.Create(
                        game,
                        transform.position,
                        9.5f,
                        ScaleOutgoingDamage(20 + phase * 3),
                        new Color(0.92f, 0.35f, 0.26f),
                        BossName);
                    SummonSwarm();

                    if (horizontal.sqrMagnitude > 0.01f)
                    {
                        game.Player.ApplyImpact(horizontal.normalized * 1.5f);
                    }

                    break;
                }

                case BossActionKind.RailCharge:
                {
                    if (horizontal.sqrMagnitude < 0.01f)
                    {
                        break;
                    }

                    var chargeDirection = horizontal.normalized;
                    var chargeDistance = Mathf.Min(12f, distance);
                    MoveCollisionAware(chargeDirection * chargeDistance);
                    if (Vector3.Distance(game.PlayerTransform.position, transform.position) < 3.4f &&
                        HasClearAttackPath())
                    {
                        game.DamagePlayer(ScaleOutgoingDamage(20 + phase * 3), BossName);
                        game.Player.ApplyImpact(chargeDirection * 2.4f);
                    }

                    break;
                }

                case BossActionKind.Cinderfall:
                {
                    foreach (var target in cinderfallTargets)
                    {
                        EnemyShockwave.Create(
                            game,
                            target,
                            2.65f,
                            ScaleOutgoingDamage(14 + phase * 3),
                            new Color(0.98f, 0.31f, 0.08f),
                            BossName);
                    }

                    cinderfallTargets.Clear();
                    break;
                }
            }
        }

        public void TakeDamage(int amount, string source)
        {
            TakeDamage(amount, source, CombatImpactKind.Light);
        }

        public void TakeDamage(
            int amount,
            string source,
            CombatImpactKind impactKind)
        {
            if (defeated || amount <= 0)
            {
                return;
            }

            if (arena != null && !arena.CanDamageBoss(this))
            {
                if (Time.time >= nextShieldNoticeAt)
                {
                    nextShieldNoticeAt = Time.time + 1.2f;
                    game.Notify($"{BossName}'s core is sealed. Find the relief latch.");
                }

                return;
            }

            if (IsExposed)
            {
                amount = Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        amount *
                        TetherfallCombatQualityRules.BossExposeDamageMultiplier));
            }

            health -= amount;
            if (health > 0)
            {
                ApplyPoiseDamage(amount, impactKind);
            }

            game.PlayBossHitFeedback(transform.position, amount);
            AdvancePhaseThresholds();

            if (health > 0)
            {
                return;
            }

            defeated = true;
            exposeVisualActive = false;
            exposedUntil = 0f;
            poisePressure = 0f;
            proceduralAnimator?.ResetState();
            CancelTelegraphs();
            ClearSummonedMinions();
            game.Notify($"{BossName} fractures into quiet light.");
            arena?.OnBossDefeated(this);
            Destroy(gameObject);
        }

        public void SetRestoredDefeated()
        {
            defeated = true;
            health = 0;
            exposeVisualActive = false;
            exposedUntil = 0f;
            poisePressure = 0f;
            proceduralAnimator?.ResetState();
            CancelTelegraphs();
            ClearSummonedMinions();
            game.ClearDungeonBoss(this);
            gameObject.SetActive(false);
        }

        public void ResetForEncounter()
        {
            defeated = false;
            ApplyEncounterScaling();
            health = MaxHealth;
            transform.position = spawnPosition;
            ClearSummonedMinions();
            CancelTelegraphs();
            nextActionAt = Time.time + 0.7f;
            strikeAt = 0f;
            staggerUntil = 0f;
            exposedUntil = 0f;
            exposeVisualActive = false;
            poisePressure = 0f;
            lastPoiseImpactAt = float.NegativeInfinity;
            nextShieldNoticeAt = 0f;
            telegraphing = false;
            phase = 1;
            phaseThresholdIndex = 0;
            actionIndex = 0;
            cinderfallTargets.Clear();
            proceduralAnimator?.ResetState();
            CancelInvoke(nameof(RecoverBodyColor));
            if (body != null)
            {
                body.material.color = GetBaseColor();
            }
        }

        public bool ApplyPoiseDamage(int damage, CombatImpactKind impactKind)
        {
            if (defeated || damage <= 0f || IsStaggered)
            {
                return false;
            }

            var protectedWindup = IsWindupPoiseProtected;
            var contribution =
                TetherfallCombatQualityRules.CalculatePoiseContribution(
                    damage,
                    MaxHealth,
                    PoiseThreshold,
                    impactKind,
                    protectedWindup,
                    true);
            if (contribution <= 0f)
            {
                return false;
            }

            lastPoiseImpactAt = Time.time;
            poisePressure = Mathf.Min(PoiseThreshold, poisePressure + contribution);
            if (protectedWindup &&
                !TetherfallCombatQualityRules.CanBreakProtectedWindup(impactKind))
            {
                poisePressure = Mathf.Min(
                    poisePressure,
                    Mathf.Max(0f, PoiseThreshold - 0.01f));
                return false;
            }

            if (poisePressure + 0.001f < PoiseThreshold)
            {
                return false;
            }

            poisePressure = 0f;
            ApplyStagger(
                TetherfallCombatQualityRules.GetThresholdStaggerSeconds(
                    impactKind,
                    true));
            return true;
        }

        public void ApplyStagger(float duration)
        {
            if (defeated || duration <= 0f)
            {
                return;
            }

            duration = Mathf.Min(
                duration,
                TetherfallCombatQualityRules.BossMaximumStaggerSeconds);
            poisePressure = 0f;
            staggerUntil = Mathf.Max(staggerUntil, Time.time + duration);
            if (telegraphing)
            {
                telegraphing = false;
                CancelTelegraphs();
            }

            proceduralAnimator?.CancelWindup();
            nextActionAt = Mathf.Max(nextActionAt, Time.time + duration + 0.45f);
            body.material.color = new Color(0.47f, 0.91f, 0.82f);
            Invoke(nameof(RecoverBodyColor), duration);
        }

        public void ApplyExpose(float duration)
        {
            if (defeated || duration <= 0f)
            {
                return;
            }

            var boundedDuration =
                TetherfallCombatQualityRules.ClampBossExposeDuration(duration);
            exposedUntil = Mathf.Min(
                Time.time + TetherfallCombatQualityRules.BossMaximumExposeSeconds,
                Mathf.Max(exposedUntil, Time.time) + boundedDuration);
            exposeVisualActive = true;
            if (!telegraphing && !IsStaggered && body != null)
            {
                body.material.color = GetExposeColor();
            }
        }

        private void CreateTelegraph(BossActionKind action)
        {
            var color = action == BossActionKind.ShardVolley
                ? new Color(0.44f, 0.68f, 0.98f, 0.62f)
                : action == BossActionKind.Threadquake || action == BossActionKind.Cinderfall
                    ? new Color(0.96f, 0.32f, 0.18f, 0.7f)
                    : new Color(0.91f, 0.42f, 0.58f, 0.62f);
            var scale = action == BossActionKind.Threadquake
                ? 19f
                : action == BossActionKind.ShardVolley
                    ? 8.5f
                    : action == BossActionKind.RailCharge
                        ? 17f
                        : 13f;
            activeTelegraphs.Add(TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                $"{BossName} telegraph",
                new Vector3(transform.position.x, 0.08f, transform.position.z),
                new Vector3(1f, 0.06f, 1f),
                RuntimeEffectMaterialCache.GetTransparent(game, color, 0.6f),
                GetActionTelegraphSeconds(action),
                scale));

            if (action == BossActionKind.Cinderfall)
            {
                cinderfallTargets.Clear();
                var target = game.PlayerTransform.position;
                target.y = 0.08f;
                var lateral = game.PlayerTransform.right;
                lateral.y = 0f;
                if (lateral.sqrMagnitude < 0.01f)
                {
                    lateral = Vector3.right;
                }

                cinderfallTargets.Add(target);
                cinderfallTargets.Add(target + lateral.normalized * 3.7f);
                cinderfallTargets.Add(target - lateral.normalized * 3.7f);
                foreach (var impactTarget in cinderfallTargets)
                {
                    activeTelegraphs.Add(TimedPulse.Spawn(
                        PrimitiveType.Cylinder,
                        "Cinderfall impact",
                        impactTarget,
                        new Vector3(0.22f, 0.035f, 0.22f),
                        RuntimeEffectMaterialCache.GetTransparent(
                            game,
                            new Color(1f, 0.3f, 0.08f, 0.7f),
                            0.65f),
                        GetActionTelegraphSeconds(action),
                        5.3f));
                }
            }
        }

        private void PauseCombatTimers()
        {
            nextActionAt += Time.deltaTime;
            strikeAt += Time.deltaTime;
            staggerUntil += Time.deltaTime;
            exposedUntil += Time.deltaTime;
            nextShieldNoticeAt += Time.deltaTime;
            if (!float.IsNegativeInfinity(lastPoiseImpactAt))
            {
                lastPoiseImpactAt += Time.deltaTime;
            }
        }

        private void MoveCollisionAware(Vector3 displacement)
        {
            var distance = displacement.magnitude;
            if (distance <= 0.001f)
            {
                return;
            }

            var direction = displacement / distance;
            var allowedDistance = distance;
            var origin = transform.position + Vector3.up * 1.45f;
            var hitCount = Physics.SphereCastNonAlloc(
                origin,
                1.08f,
                direction,
                movementHitBuffer,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var hit = movementHitBuffer[hitIndex];
                var hitBoss = hit.collider.GetComponentInParent<DungeonBoss>();
                if (hitBoss == this ||
                    hit.collider.GetComponentInParent<PlayerAvatar>() != null ||
                    hit.collider.GetComponentInParent<Enemy>() != null ||
                    hit.collider.GetComponentInParent<PactCreature>() != null ||
                    hit.collider.GetComponentInParent<HumanCompanion>() != null)
                {
                    continue;
                }

                allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, hit.distance - 0.06f));
            }

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                movementHitBuffer[hitIndex] = default;
            }

            if (allowedDistance > 0f)
            {
                transform.position += direction * allowedDistance;
            }
        }

        private bool HasClearAttackPath()
        {
            if (game == null || game.PlayerTransform == null)
            {
                return false;
            }

            var origin = transform.position + Vector3.up * 1.45f;
            var target = game.PlayerTransform.position + Vector3.up * 0.9f;
            var path = target - origin;
            var distance = path.magnitude;
            if (distance <= 0.01f)
            {
                return true;
            }

            var hitCount = Physics.SphereCastNonAlloc(
                origin,
                0.18f,
                path / distance,
                attackPathHitBuffer,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            var blocked = false;
            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var collider = attackPathHitBuffer[hitIndex].collider;
                if (collider == null ||
                    collider.GetComponentInParent<DungeonBoss>() == this ||
                    collider.GetComponentInParent<PlayerAvatar>() != null ||
                    collider.GetComponentInParent<Enemy>() != null ||
                    collider.GetComponentInParent<PactCreature>() != null ||
                    collider.GetComponentInParent<HumanCompanion>() != null)
                {
                    continue;
                }

                blocked = true;
                break;
            }

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                attackPathHitBuffer[hitIndex] = default;
            }

            return !blocked;
        }

        private void CancelTelegraphs()
        {
            foreach (var telegraph in activeTelegraphs)
            {
                if (telegraph == null || !telegraph.activeInHierarchy)
                {
                    continue;
                }

                var pulse = telegraph.GetComponent<TimedPulse>();
                if (pulse != null)
                {
                    pulse.Cancel();
                }
                else
                {
                    Object.Destroy(telegraph);
                }
            }

            activeTelegraphs.Clear();
        }

        private float GetMoveSpeed()
        {
            if (definition == null)
            {
                return phase == 1 ? 1.45f : 2.15f;
            }

            return phase == 1 ? definition.phaseOneMoveSpeed : definition.phaseTwoMoveSpeed;
        }

        private float GetActionTelegraphSeconds(BossActionKind action)
        {
            return action == BossActionKind.Threadquake || action == BossActionKind.Cinderfall
                ? 1.22f
                : action == BossActionKind.RailCharge
                    ? 1.05f
                    : 0.92f;
        }

        private Color GetBaseColor()
        {
            if (definition == null)
            {
                return phase == 1 ? new Color(0.34f, 0.2f, 0.5f) : new Color(0.58f, 0.19f, 0.34f);
            }

            return phase == 1 ? definition.baseColor : definition.phaseColor;
        }

        private void AdvancePhaseThresholds()
        {
            if (definition == null)
            {
                return;
            }

            var threshold = phaseThresholdIndex == 0
                ? definition.phaseThresholdOne
                : phaseThresholdIndex == 1
                    ? definition.phaseThresholdTwo
                    : 0f;
            if (threshold <= 0f || health > MaxHealth * threshold)
            {
                return;
            }

            phaseThresholdIndex++;
            phase++;
            arena?.OnBossPhaseThreshold(this, phase);
        }

        private void RecoverBodyColor()
        {
            if (body != null && !defeated && !telegraphing)
            {
                body.material.color = IsExposed
                    ? GetExposeColor()
                    : GetBaseColor();
            }
        }

        private void RecoverPoisePressure()
        {
            if (poisePressure <= 0f ||
                Time.time < lastPoiseImpactAt + poiseProfile.RecoveryDelaySeconds)
            {
                return;
            }

            poisePressure = Mathf.MoveTowards(
                poisePressure,
                0f,
                poiseProfile.RecoveryPerSecond * Time.deltaTime);
        }

        private void ApplyEncounterScaling()
        {
            var scaling = TetherfallCombatQualityRules.GetBossScalingBand(
                game != null ? game.Level : 1,
                repeatEncounterScaling);
            threatBand =
                TetherfallCombatQualityRules.GetThreatBand(scaling, true);
            maxHealth = Mathf.Max(
                1,
                Mathf.RoundToInt(
                    (definition != null ? definition.maxHealth : 1) *
                    scaling.HealthMultiplier));
            outgoingDamageMultiplier = scaling.DamageMultiplier;
        }

        private int ScaleOutgoingDamage(int amount)
        {
            return Mathf.Max(
                1,
                Mathf.RoundToInt(amount * outgoingDamageMultiplier));
        }

        private Color GetExposeColor()
        {
            return Color.Lerp(
                GetBaseColor(),
                new Color(1f, 0.72f, 0.24f),
                0.58f);
        }

        private void SummonSwarm()
        {
            if (definition == null || string.IsNullOrEmpty(definition.summonedEnemyId))
            {
                return;
            }

            PruneSummonedMinions();
            var spawnCount = Mathf.Min(2, 4 - summonedMinions.Count);
            for (var index = 0; index < spawnCount; index++)
            {
                var minion = Enemy.Create(
                    game,
                    null,
                    definition.summonedEnemyId,
                    transform.position + new Vector3(index == 0 ? -4f : 4f, 1f, -2f),
                    OnMinionDefeated);
                if (minion != null)
                {
                    summonedMinions.Add(minion);
                }
            }
        }

        private void OnMinionDefeated(Enemy enemy)
        {
            summonedMinions.Remove(enemy);
        }

        private void ClearSummonedMinions()
        {
            foreach (var minion in summonedMinions)
            {
                if (minion != null)
                {
                    Destroy(minion.gameObject);
                }
            }

            summonedMinions.Clear();
        }

        private void PruneSummonedMinions()
        {
            for (var index = summonedMinions.Count - 1; index >= 0; index--)
            {
                if (summonedMinions[index] == null || summonedMinions[index].IsDefeated)
                {
                    summonedMinions.RemoveAt(index);
                }
            }
        }

        private void OnDestroy()
        {
            ClearSummonedMinions();
        }
    }
}
