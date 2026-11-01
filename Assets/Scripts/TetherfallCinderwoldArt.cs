using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public static class CinderwoldArtBuilder
    {
        private static readonly Color Basalt = new Color(0.105f, 0.055f, 0.065f);
        private static readonly Color BasaltHighlight = new Color(0.23f, 0.1f, 0.095f);
        private static readonly Color Copper = new Color(0.56f, 0.22f, 0.11f);
        private static readonly Color Ember = new Color(1f, 0.25f, 0.075f);
        private static readonly Color RelayTeal = new Color(0.1f, 0.82f, 0.72f);

        public static void Build(TetherfallGame game)
        {
            var basaltMaterial = game.CreateMaterial(Basalt, 0.1f, 0.32f);
            var basaltHighlightMaterial = game.CreateMaterial(BasaltHighlight, 0.08f, 0.35f);
            var copperMaterial = game.CreateMaterial(Copper, 0.38f, 0.58f);
            var emberMaterial = game.CreateGlowMaterial(Ember, 2.25f, 0.58f);
            var relayMaterial = game.CreateGlowMaterial(RelayTeal, 1.85f, 0.64f);
            var ashMaterial = game.CreateTransparentMaterial(new Color(0.33f, 0.09f, 0.06f, 0.2f), 0.22f);

            var relay = CreateRelayTower(
                game,
                CinderwoldArcCatalog.RelayPosition,
                basaltMaterial,
                basaltHighlightMaterial,
                copperMaterial,
                emberMaterial,
                relayMaterial);
            CreateRelayPylons(
                game,
                CinderwoldArcCatalog.RelayPosition,
                basaltMaterial,
                copperMaterial,
                emberMaterial,
                relayMaterial,
                relay);
            CreateRelayApproach(
                game,
                CinderwoldArcCatalog.RelayPosition,
                basaltMaterial,
                basaltHighlightMaterial,
                copperMaterial,
                emberMaterial,
                relayMaterial,
                ashMaterial,
                relay);
            CreateFissureField(game, emberMaterial, basaltHighlightMaterial);
            CreateAshHorizon(game, basaltMaterial, ashMaterial);
            CreateMuralStation(game, new Vector3(-115f, 0f, -25f), copperMaterial, basaltMaterial, emberMaterial);
        }

        private static CinderwoldSkyfireRelay CreateRelayTower(
            TetherfallGame game,
            Vector3 position,
            Material basalt,
            Material basaltHighlight,
            Material copper,
            Material ember,
            Material relay)
        {
            var root = new GameObject("Cinderwold Skyfire Relay");
            root.transform.position = position;

            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Skyfire relay basalt foundation",
                root.transform,
                new Vector3(0f, 0.5f, 0f),
                new Vector3(4.5f, 0.5f, 4.5f),
                basalt,
                true);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Skyfire relay stepped plinth",
                root.transform,
                new Vector3(0f, 1.15f, 0f),
                new Vector3(3.55f, 0.18f, 3.55f),
                basaltHighlight,
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Skyfire relay copper dais",
                root.transform,
                new Vector3(0f, 1.55f, 0f),
                new Vector3(2.55f, 0.58f, 2.55f),
                copper,
                true);

            for (var index = 0; index < 4; index++)
            {
                var angle = (45f + index * 90f) * Mathf.Deg2Rad;
                var pillar = CreatePrimitive(
                    PrimitiveType.Cube,
                    "Skyfire relay buttress",
                    root.transform,
                    new Vector3(Mathf.Cos(angle) * 1.32f, 4.1f, Mathf.Sin(angle) * 1.32f),
                    new Vector3(0.42f, 2.7f, 0.42f),
                    basaltHighlight,
                    true);
                pillar.transform.localRotation = Quaternion.Euler(0f, -index * 90f, 0f);

                var band = CreatePrimitive(
                    PrimitiveType.Cube,
                    "Skyfire relay copper band",
                    root.transform,
                    new Vector3(Mathf.Cos(angle) * 1.36f, 5.15f, Mathf.Sin(angle) * 1.36f),
                    new Vector3(0.52f, 0.14f, 0.52f),
                    copper,
                    false);
                band.transform.localRotation = Quaternion.Euler(0f, -index * 90f, 0f);
            }

            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Skyfire relay crown",
                root.transform,
                new Vector3(0f, 6.65f, 0f),
                new Vector3(1.3f, 0.22f, 1.3f),
                copper,
                false);
            var lens = CreatePrimitive(
                PrimitiveType.Sphere,
                "Skyfire relay lens",
                root.transform,
                new Vector3(0f, 7.28f, 0f),
                Vector3.one * 0.92f,
                ember,
                false);
            lens.AddComponent<RouteGlow>().Initialize(0.1f, position.x);

            var spire = CreatePrimitive(
                PrimitiveType.Cylinder,
                "Skyfire relay spire",
                root.transform,
                new Vector3(0f, 9.25f, 0f),
                new Vector3(0.24f, 1.8f, 0.24f),
                copper,
                false);
            spire.AddComponent<RouteGlow>().Initialize(0.04f, position.z);

            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = RelayTeal;
            light.range = 15f;
            light.intensity = 2.1f;
            light.shadows = LightShadows.None;

            var relayState = root.AddComponent<CinderwoldSkyfireRelay>();
            relayState.Initialize(
                game,
                lens.GetComponent<Renderer>(),
                light,
                ember,
                relay);
            CreateResolutionAccents(root.transform, relayState, ember, relay);
            WorldVisuals.AddLabel(
                root.transform,
                "Ember Relay",
                new Vector3(0f, 10.8f, 0f),
                Color.Lerp(Ember, Color.white, 0.3f),
                0.12f);
            return relayState;
        }

        private static void CreateRelayPylons(
            TetherfallGame game,
            Vector3 towerPosition,
            Material basalt,
            Material copper,
            Material ember,
            Material relay,
            CinderwoldSkyfireRelay relayState)
        {
            var pylonPositions = new[]
            {
                towerPosition + new Vector3(-12f, 0f, -7f),
                towerPosition + new Vector3(12f, 0f, -5f),
                towerPosition + new Vector3(8f, 0f, 12f)
            };
            var cableStart = towerPosition + Vector3.up * 7.4f;

            for (var index = 0; index < pylonPositions.Length; index++)
            {
                var root = new GameObject("Skyfire relay cable pylon");
                root.transform.position = pylonPositions[index];
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Relay cable pylon base",
                    root.transform,
                    new Vector3(0f, 0.62f, 0f),
                    new Vector3(1.25f, 0.62f, 1.25f),
                    basalt,
                    true);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Relay cable pylon copper head",
                    root.transform,
                    new Vector3(0f, 3.2f, 0f),
                    new Vector3(0.23f, 2.4f, 0.23f),
                    copper,
                    false);
                var flame = CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Relay cable ember flare",
                    root.transform,
                    new Vector3(0f, 5.55f, 0f),
                    Vector3.one * 0.42f,
                    ember,
                    false);
                flame.AddComponent<RouteGlow>().Initialize(0.13f, index * 2.7f);
                relayState?.RegisterPylonFlare(flame.GetComponent<Renderer>());

                var cableEnd = pylonPositions[index] + Vector3.up * 5.55f;
                CreateCable("Skyfire relay cable", cableStart, cableEnd, copper, 0.075f);
            }
        }

        private static void CreateRelayApproach(
            TetherfallGame game,
            Vector3 relayPosition,
            Material basalt,
            Material basaltHighlight,
            Material copper,
            Material ember,
            Material relay,
            Material ash,
            CinderwoldSkyfireRelay relayState)
        {
            var root = new GameObject("Ash-Copper Spine approach");
            var route = new[]
            {
                new Vector3(-110f, 0f, -18f),
                new Vector3(-118f, 0f, -4f),
                new Vector3(-128f, 0f, 18f),
                new Vector3(-150f, 0f, 45f),
                new Vector3(-176f, 0f, 67f),
                new Vector3(-205f, 0f, 76f),
                relayPosition
            };
            CreateRoadSegments(root.transform, route, basaltHighlight, copper);
            CreateShelfMasses(game);

            var gantries = new[]
            {
                new Vector3(-128f, 0f, 18f),
                new Vector3(-150f, 0f, 45f),
                new Vector3(-176f, 0f, 67f)
            };
            var cablePoints = new List<Vector3>();
            for (var index = 0; index < gantries.Length; index++)
            {
                cablePoints.Add(CreateRelayGantry(
                    root.transform,
                    gantries[index],
                    index,
                    basalt,
                    copper,
                    ember,
                    relay,
                    relayState));
            }

            cablePoints.Add(relayPosition + Vector3.up * 7.35f);
            for (var index = 0; index < cablePoints.Count - 1; index++)
            {
                CreateCable(
                    "Ash-Copper Spine cable",
                    cablePoints[index],
                    cablePoints[index + 1],
                    copper,
                    0.06f,
                    5);
            }

            CreateBranchPin(root.transform, new Vector3(-160f, 0f, 75f), copper, ember, relay, relayState);
            CreateBranchPin(root.transform, new Vector3(-196f, 0f, 88f), copper, ember, relay, relayState);
            CreateApproachAsh(root.transform, ash);
        }

        private static void CreateRoadSegments(
            Transform root,
            Vector3[] route,
            Material basalt,
            Material copper)
        {
            var roadIndex = 0;
            for (var routeIndex = 0; routeIndex < route.Length - 1; routeIndex++)
            {
                var start = route[routeIndex];
                var end = route[routeIndex + 1];
                var direction = end - start;
                direction.y = 0f;
                var distance = direction.magnitude;
                if (distance <= 0.01f)
                {
                    continue;
                }

                direction /= distance;
                var segmentCount = Mathf.Max(1, Mathf.CeilToInt(distance / 8f));
                for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++, roadIndex++)
                {
                    var t = (segmentIndex + 0.5f) / segmentCount;
                    var position = Vector3.Lerp(start, end, t) + Vector3.up * 0.06f;
                    var segment = CreatePrimitive(
                        PrimitiveType.Cube,
                        "Ash-Copper Spine basalt slab",
                        root,
                        position,
                        new Vector3(4.55f, 0.07f, distance / segmentCount + 0.28f),
                        basalt,
                        false);
                    segment.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                    if (roadIndex % 4 != 0)
                    {
                        continue;
                    }

                    var seam = CreatePrimitive(
                        PrimitiveType.Cube,
                        "Ash-Copper Spine copper seam",
                        root,
                        position + Vector3.up * 0.085f,
                        new Vector3(4.62f, 0.018f, 0.14f),
                        copper,
                        false);
                    seam.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                }
            }
        }

        private static Vector3 CreateRelayGantry(
            Transform parent,
            Vector3 position,
            int index,
            Material basalt,
            Material copper,
            Material ember,
            Material relay,
            CinderwoldSkyfireRelay relayState)
        {
            var root = new GameObject("Ash-Copper Spine gantry");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            var yaw = index == 0 ? 24f : index == 1 ? -12f : 18f;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Ash-Copper Spine basalt gantry post",
                    root.transform,
                    new Vector3(side * 1.75f, 2.55f, 0f),
                    new Vector3(0.22f, 2.55f, 0.22f),
                    basalt,
                    false);
            }

            CreatePrimitive(
                PrimitiveType.Cube,
                "Ash-Copper Spine copper gantry arm",
                root.transform,
                new Vector3(0f, 5.1f, 0f),
                new Vector3(3.95f, 0.14f, 0.16f),
                copper,
                false);
            var flare = CreatePrimitive(
                PrimitiveType.Sphere,
                "Ash-Copper Spine signal flare",
                root.transform,
                new Vector3(0f, 5.45f, 0f),
                Vector3.one * 0.28f,
                ember,
                false);
            flare.AddComponent<RouteGlow>().Initialize(0.1f, index * 1.37f);
            relayState?.RegisterPylonFlare(flare.GetComponent<Renderer>());

            if (index != 1)
            {
                var routeNode = CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Ash-Copper Spine routebound inlay",
                    root.transform,
                    new Vector3(0f, 0.14f, -0.7f),
                    new Vector3(0.28f, 0.04f, 0.28f),
                    relay,
                    false);
                relayState?.RegisterRouteboundVisual(routeNode);
                var ashShard = CreatePrimitive(
                    PrimitiveType.Cube,
                    "Ash-Copper Spine ashbound shard",
                    root.transform,
                    new Vector3(0f, 4.15f, 0.5f),
                    new Vector3(0.12f, 0.58f, 0.12f),
                    ember,
                    false);
                ashShard.transform.localRotation = Quaternion.Euler(18f, 0f, 26f);
                FoliageSway.Attach(ashShard, 1.5f);
                relayState?.RegisterAshboundVisual(ashShard);
            }

            return root.transform.position + Vector3.up * 5.45f;
        }

        private static void CreateShelfMasses(TetherfallGame game)
        {
            var masses = new[]
            {
                new Vector3(-139f, 0f, 31f),
                new Vector3(-162f, 0f, 58f),
                new Vector3(-187f, 0f, 61f),
                new Vector3(-218f, 0f, 65f),
                new Vector3(-218f, 0f, 93f),
                new Vector3(-174f, 0f, 91f)
            };
            for (var index = 0; index < masses.Length; index++)
            {
                var asset = index % 3 == 0 ? "rock_tallA" : index % 3 == 1 ? "rock_largeA" : "rock_smallA";
                var scale = index % 3 == 0 ? 2f : index % 3 == 1 ? 1.7f : 1.15f;
                var rock = WorldArt.SpawnKenney(
                    game,
                    asset,
                    masses[index],
                    Vector3.one * scale,
                    Color.Lerp(BasaltHighlight, Copper, 0.14f));
                if (rock != null)
                {
                    rock.transform.rotation = Quaternion.Euler(0f, index * 41f, index % 2 * 7f);
                }
            }
        }

        private static void CreateBranchPin(
            Transform parent,
            Vector3 position,
            Material copper,
            Material ember,
            Material relay,
            CinderwoldSkyfireRelay relayState)
        {
            var root = new GameObject("Ash-Copper Spine branch pin");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Ash-Copper Spine branch post",
                root.transform,
                new Vector3(0f, 0.72f, 0f),
                new Vector3(0.12f, 0.72f, 0.12f),
                copper,
                false);
            var emberMarker = CreatePrimitive(
                PrimitiveType.Sphere,
                "Ash-Copper Spine branch ember",
                root.transform,
                new Vector3(0f, 1.58f, 0f),
                Vector3.one * 0.16f,
                ember,
                false);
            emberMarker.AddComponent<RouteGlow>().Initialize(0.08f, position.x);
            relayState?.RegisterPylonFlare(emberMarker.GetComponent<Renderer>());
            var routeMarker = CreatePrimitive(
                PrimitiveType.Sphere,
                "Ash-Copper Spine branch route",
                root.transform,
                new Vector3(0.24f, 1.32f, 0f),
                Vector3.one * 0.1f,
                relay,
                false);
            routeMarker.AddComponent<RouteGlow>().Initialize(0.08f, position.z);
            relayState?.RegisterRouteboundVisual(routeMarker);
        }

        private static void CreateApproachAsh(Transform parent, Material ash)
        {
            var motes = new List<Transform>();
            for (var index = 0; index < 6; index++)
            {
                var mote = CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Ash-Copper Spine drifting ash",
                    parent,
                    new Vector3(-130f - index * 11f, 2.4f + index % 3 * 0.7f, 18f + index * 10f),
                    Vector3.one * (0.17f + index % 2 * 0.05f),
                    ash,
                    false);
                motes.Add(mote.transform);
            }

            var drift = new GameObject("Ash-Copper Spine ash drift").AddComponent<CinderwoldAshDrift>();
            drift.Initialize(motes);
        }

        private static void CreateResolutionAccents(
            Transform root,
            CinderwoldSkyfireRelay relay,
            Material ember,
            Material route)
        {
            for (var index = 0; index < 3; index++)
            {
                var angle = (index * 120f + 30f) * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var routeNode = CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Routebound relay marker",
                    root,
                    direction * 2.2f + Vector3.up * 0.34f,
                    new Vector3(0.22f, 0.34f, 0.22f),
                    route,
                    false);
                routeNode.AddComponent<RouteGlow>().Initialize(0.08f, index * 1.7f);
                relay.RegisterRouteboundVisual(routeNode);

                var ashShard = CreatePrimitive(
                    PrimitiveType.Cube,
                    "Ashbound relay shard",
                    root,
                    direction * 1.7f + Vector3.up * 4.8f,
                    new Vector3(0.16f, 0.72f, 0.16f),
                    ember,
                    false);
                ashShard.transform.localRotation = Quaternion.Euler(18f, index * 120f, 24f);
                FoliageSway.Attach(ashShard, 2.1f);
                relay.RegisterAshboundVisual(ashShard);
            }
        }

        private static void CreateFissureField(TetherfallGame game, Material ember, Material darkStone)
        {
            var fissures = new[]
            {
                new Vector4(-202f, 88f, 9f, 13f),
                new Vector4(-197f, 81f, -21f, 11f),
                new Vector4(-186f, 83f, 28f, 10f),
                new Vector4(-177f, 96f, -37f, 14f),
                new Vector4(-212f, 104f, 14f, 8f),
                new Vector4(-172f, 109f, -15f, 12f),
                new Vector4(-206f, 70f, 42f, 9f),
                new Vector4(-161f, 88f, -18f, 13f)
            };

            for (var index = 0; index < fissures.Length; index++)
            {
                var line = fissures[index];
                var segment = CreatePrimitive(
                    PrimitiveType.Cube,
                    "Cinderwold molten fissure",
                    null,
                    new Vector3(line.x, 0.022f, line.y),
                    new Vector3(0.2f, 0.025f, line.w),
                    ember,
                    false);
                segment.transform.rotation = Quaternion.Euler(0f, line.z, 0f);
                segment.AddComponent<RouteGlow>().Initialize(0.07f, index * 1.77f);

                var crust = CreatePrimitive(
                    PrimitiveType.Cube,
                    "Cinderwold fissure crust",
                    null,
                    new Vector3(line.x + Mathf.Sin(index * 1.9f) * 0.34f, 0.015f, line.y),
                    new Vector3(0.55f, 0.018f, line.w * 1.06f),
                    darkStone,
                    false);
                crust.transform.rotation = Quaternion.Euler(0f, line.z + 3f, 0f);
            }
        }

        private static void CreateAshHorizon(TetherfallGame game, Material basalt, Material ash)
        {
            var spires = new[]
            {
                new Vector3(-224f, 0f, 118f), new Vector3(-210f, 0f, 122f), new Vector3(-196f, 0f, 126f),
                new Vector3(-176f, 0f, 124f), new Vector3(-151f, 0f, 116f), new Vector3(-221f, 0f, 49f),
                new Vector3(-199f, 0f, 56f), new Vector3(-168f, 0f, 51f), new Vector3(-145f, 0f, 72f)
            };

            var ashBits = new List<Transform>();
            for (var index = 0; index < spires.Length; index++)
            {
                var height = 7f + (index * 13 % 9);
                var spire = CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Distant obsidian needle",
                    null,
                    spires[index] + Vector3.up * height * 0.5f,
                    new Vector3(1.6f + index % 3 * 0.42f, height * 0.5f, 1.6f + index % 2 * 0.34f),
                    basalt,
                    false);
                spire.transform.rotation = Quaternion.Euler(index % 2 * 7f, index * 27f, index % 3 * 9f);

                for (var particle = 0; particle < 4; particle++)
                {
                    var ashBit = CreatePrimitive(
                        PrimitiveType.Sphere,
                        "Cinderwold ash mote",
                        null,
                        spires[index] + new Vector3(
                            Mathf.Sin((index + 1) * (particle + 2)) * 2.8f,
                            height + 1.4f + particle * 1.25f,
                            Mathf.Cos((index + 3) * (particle + 1)) * 2.6f),
                        Vector3.one * (0.26f + particle * 0.045f),
                        ash,
                        false);
                    ashBits.Add(ashBit.transform);
                }
            }

            var ashDrift = new GameObject("Cinderwold ash drift").AddComponent<CinderwoldAshDrift>();
            ashDrift.Initialize(ashBits);
        }

        private static void CreateMuralStation(
            TetherfallGame game,
            Vector3 position,
            Material copper,
            Material basalt,
            Material ember)
        {
            var root = new GameObject("Daro's Relay Mural");
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            var texture = CreateRelayMuralTexture();
            var muralMaterial = game.CreateMaterial(Color.white, 0.04f, 0.42f);
            muralMaterial.mainTexture = texture;

            var mural = CreatePrimitive(
                PrimitiveType.Quad,
                "Cinderwold relay mural art",
                root.transform,
                new Vector3(0f, 3.25f, 0f),
                new Vector3(6.4f, 3.6f, 1f),
                muralMaterial,
                false);
            mural.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Cinderwold mural lower frame",
                root.transform,
                new Vector3(0f, 1.42f, 0.07f),
                new Vector3(6.9f, 0.18f, 0.16f),
                copper,
                false);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Cinderwold mural upper frame",
                root.transform,
                new Vector3(0f, 5.08f, 0.07f),
                new Vector3(6.9f, 0.18f, 0.16f),
                copper,
                false);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Cinderwold mural side frame",
                    root.transform,
                    new Vector3(side * 3.36f, 3.25f, 0.07f),
                    new Vector3(0.18f, 3.82f, 0.16f),
                    copper,
                    false);
            }

            var emberLamp = CreatePrimitive(
                PrimitiveType.Sphere,
                "Cinderwold mural ember lamp",
                root.transform,
                new Vector3(0f, 5.45f, 0.1f),
                Vector3.one * 0.24f,
                ember,
                false);
            emberLamp.AddComponent<RouteGlow>().Initialize(0.1f, position.x);
            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.32f, 0.13f);
            light.range = 6f;
            light.intensity = 1.25f;
            light.shadows = LightShadows.None;
            WorldVisuals.AddLabel(
                root.transform,
                "Relay witness mural",
                new Vector3(0f, 5.9f, 0f),
                new Color(0.96f, 0.55f, 0.3f),
                0.07f);
        }

        private static GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool solid)
        {
            var visual = GameObject.CreatePrimitive(primitiveType);
            visual.name = name;
            if (parent != null)
            {
                visual.transform.SetParent(parent, false);
                visual.transform.localPosition = localPosition;
            }
            else
            {
                visual.transform.position = localPosition;
            }

            visual.transform.localScale = localScale;
            visual.GetComponent<Renderer>().material = material;
            if (!solid)
            {
                var collider = visual.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.Destroy(collider);
                }
            }

            return visual;
        }

        private static void CreateCable(
            string name,
            Vector3 start,
            Vector3 end,
            Material material,
            float thickness,
            int segmentCount = 9)
        {
            var segments = Mathf.Max(1, segmentCount);
            var previous = start;
            for (var segment = 1; segment <= segments; segment++)
            {
                var t = segment / (float)segments;
                var sag = 4f * 1.15f * t * (1f - t);
                var current = Vector3.Lerp(start, end, t) - Vector3.up * sag;
                CreateCableSegment($"{name} {segment}", previous, current, material, thickness);
                previous = current;
            }
        }

        private static void CreateCableSegment(
            string name,
            Vector3 start,
            Vector3 end,
            Material material,
            float thickness)
        {
            var difference = end - start;
            var cable = CreatePrimitive(
                PrimitiveType.Cylinder,
                name,
                null,
                (start + end) * 0.5f,
                new Vector3(thickness, difference.magnitude * 0.5f, thickness),
                material,
                false);
            cable.transform.rotation = Quaternion.FromToRotation(Vector3.up, difference.normalized);
        }

        private static Texture2D CreateRelayMuralTexture()
        {
            const int width = 384;
            const int height = 216;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "Cinderwold Relay Mural Texture"
            };

            for (var y = 0; y < height; y++)
            {
                var horizon = Mathf.InverseLerp(0f, height, y);
                var sky = Color.Lerp(new Color(0.07f, 0.015f, 0.025f), new Color(0.44f, 0.08f, 0.045f), horizon);
                for (var x = 0; x < width; x++)
                {
                    var smoke = Mathf.Sin(x * 0.045f + y * 0.082f) * 0.025f;
                    texture.SetPixel(x, y, sky + new Color(smoke, smoke * 0.24f, smoke * 0.18f, 0f));
                }
            }

            DrawTriangle(texture, new Vector2Int(10, 70), new Vector2Int(116, 70), new Vector2Int(61, 164), new Color(0.08f, 0.025f, 0.03f));
            DrawTriangle(texture, new Vector2Int(90, 66), new Vector2Int(238, 66), new Vector2Int(158, 156), new Color(0.11f, 0.03f, 0.035f));
            DrawTriangle(texture, new Vector2Int(224, 68), new Vector2Int(379, 68), new Vector2Int(306, 160), new Color(0.075f, 0.02f, 0.026f));
            DrawRect(texture, 0, 0, width, 72, new Color(0.05f, 0.012f, 0.02f));
            DrawRect(texture, 53, 65, 61, 88, new Color(0.18f, 0.06f, 0.045f));
            DrawRect(texture, 67, 65, 32, 116, new Color(0.3f, 0.12f, 0.06f));
            DrawRect(texture, 74, 113, 19, 46, new Color(0.13f, 0.64f, 0.56f));
            DrawRect(texture, 79, 154, 9, 38, new Color(0.5f, 0.18f, 0.08f));
            DrawLine(texture, new Vector2Int(90, 145), new Vector2Int(318, 104), new Color(0.94f, 0.34f, 0.1f), 2);
            DrawLine(texture, new Vector2Int(92, 133), new Vector2Int(305, 79), new Color(0.94f, 0.34f, 0.1f), 2);
            DrawLine(texture, new Vector2Int(89, 124), new Vector2Int(289, 145), new Color(0.1f, 0.72f, 0.65f), 2);
            DrawCircle(texture, new Vector2Int(84, 171), 13, new Color(0.18f, 0.92f, 0.75f));
            DrawCircle(texture, new Vector2Int(84, 171), 5, Color.white);
            texture.Apply(true, true);
            return texture;
        }

        private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (var pixelY = Mathf.Max(0, y); pixelY < Mathf.Min(texture.height, y + height); pixelY++)
            {
                for (var pixelX = Mathf.Max(0, x); pixelX < Mathf.Min(texture.width, x + width); pixelX++)
                {
                    texture.SetPixel(pixelX, pixelY, color);
                }
            }
        }

        private static void DrawTriangle(Texture2D texture, Vector2Int a, Vector2Int b, Vector2Int c, Color color)
        {
            var minX = Mathf.Clamp(Mathf.Min(a.x, Mathf.Min(b.x, c.x)), 0, texture.width - 1);
            var maxX = Mathf.Clamp(Mathf.Max(a.x, Mathf.Max(b.x, c.x)), 0, texture.width - 1);
            var minY = Mathf.Clamp(Mathf.Min(a.y, Mathf.Min(b.y, c.y)), 0, texture.height - 1);
            var maxY = Mathf.Clamp(Mathf.Max(a.y, Mathf.Max(b.y, c.y)), 0, texture.height - 1);
            var area = Cross(b - a, c - a);
            if (Mathf.Abs(area) < 0.001f)
            {
                return;
            }

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var point = new Vector2Int(x, y);
                    var first = Cross(b - a, point - a);
                    var second = Cross(c - b, point - b);
                    var third = Cross(a - c, point - c);
                    if ((first >= 0f && second >= 0f && third >= 0f) ||
                        (first <= 0f && second <= 0f && third <= 0f))
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static float Cross(Vector2Int left, Vector2Int right)
        {
            return left.x * right.y - left.y * right.x;
        }

        private static void DrawLine(Texture2D texture, Vector2Int start, Vector2Int end, Color color, int thickness)
        {
            var length = Mathf.Max(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));
            for (var index = 0; index <= length; index++)
            {
                var point = Vector2Int.RoundToInt(Vector2.Lerp(start, end, index / (float)Mathf.Max(1, length)));
                DrawRect(texture, point.x - thickness / 2, point.y - thickness / 2, thickness, thickness, color);
            }
        }

        private static void DrawCircle(Texture2D texture, Vector2Int center, int radius, Color color)
        {
            var radiusSquared = radius * radius;
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radiusSquared)
                    {
                        var pixelX = center.x + x;
                        var pixelY = center.y + y;
                        if (pixelX >= 0 && pixelX < texture.width && pixelY >= 0 && pixelY < texture.height)
                        {
                            texture.SetPixel(pixelX, pixelY, color);
                        }
                    }
                }
            }
        }
    }

    public sealed partial class TetherfallGame
    {
        private CinderwoldSkyfireRelay cinderwoldSkyfireRelay;

        internal void RegisterCinderwoldSkyfireRelay(CinderwoldSkyfireRelay relay)
        {
            cinderwoldSkyfireRelay = relay;
        }

        internal void SetCinderwoldRelayResolution(string resolutionId)
        {
            cinderwoldSkyfireRelay?.SetResolution(resolutionId);
        }
    }

    public sealed class CinderwoldSkyfireRelay : MonoBehaviour
    {
        private readonly List<Renderer> pylonFlares = new List<Renderer>();
        private readonly List<GameObject> routeboundVisuals = new List<GameObject>();
        private readonly List<GameObject> ashboundVisuals = new List<GameObject>();
        private Renderer lensRenderer;
        private Light beacon;
        private Material emberMaterial;
        private Material routeboundMaterial;
        private string resolutionId;

        public void Initialize(
            TetherfallGame owner,
            Renderer lens,
            Light beaconLight,
            Material ember,
            Material routebound)
        {
            lensRenderer = lens;
            beacon = beaconLight;
            emberMaterial = ember;
            routeboundMaterial = routebound;
            owner.RegisterCinderwoldSkyfireRelay(this);
            ApplyResolution();
        }

        public void RegisterPylonFlare(Renderer flare)
        {
            if (flare == null)
            {
                return;
            }

            pylonFlares.Add(flare);
            ApplySignalMaterial(flare);
        }

        public void RegisterRouteboundVisual(GameObject visual)
        {
            if (visual == null)
            {
                return;
            }

            routeboundVisuals.Add(visual);
            visual.SetActive(IsRoutebound);
        }

        public void RegisterAshboundVisual(GameObject visual)
        {
            if (visual == null)
            {
                return;
            }

            ashboundVisuals.Add(visual);
            visual.SetActive(IsAshbound);
        }

        public void SetResolution(string value)
        {
            resolutionId = value ?? string.Empty;
            ApplyResolution();
        }

        private bool IsRoutebound => resolutionId == CinderwoldArcCatalog.RouteboundResolutionId;
        private bool IsAshbound => resolutionId == CinderwoldArcCatalog.AshboundResolutionId;

        private void ApplyResolution()
        {
            if (lensRenderer != null)
            {
                lensRenderer.sharedMaterial = IsRoutebound ? routeboundMaterial : emberMaterial;
            }

            foreach (var flare in pylonFlares)
            {
                ApplySignalMaterial(flare);
            }

            if (beacon != null)
            {
                beacon.color = IsRoutebound
                    ? new Color(0.1f, 0.82f, 0.72f)
                    : new Color(1f, 0.25f, 0.075f);
                beacon.intensity = IsRoutebound ? 2.1f : IsAshbound ? 1.45f : 0.72f;
            }

            foreach (var visual in routeboundVisuals)
            {
                if (visual != null)
                {
                    visual.SetActive(IsRoutebound);
                }
            }

            foreach (var visual in ashboundVisuals)
            {
                if (visual != null)
                {
                    visual.SetActive(IsAshbound);
                }
            }
        }

        private void ApplySignalMaterial(Renderer flare)
        {
            if (flare != null)
            {
                flare.sharedMaterial = IsRoutebound ? routeboundMaterial : emberMaterial;
            }
        }
    }

    public sealed class CinderwoldAshDrift : MonoBehaviour
    {
        private readonly List<Transform> particles = new List<Transform>();
        private readonly List<Vector3> anchors = new List<Vector3>();

        public void Initialize(List<Transform> sourceParticles)
        {
            foreach (var source in sourceParticles)
            {
                if (source == null)
                {
                    continue;
                }

                particles.Add(source);
                anchors.Add(source.position);
            }
        }

        private void Update()
        {
            for (var index = particles.Count - 1; index >= 0; index--)
            {
                if (particles[index] == null)
                {
                    particles.RemoveAt(index);
                    anchors.RemoveAt(index);
                    continue;
                }

                var phase = Time.time * (0.32f + index * 0.013f) + index * 1.71f;
                var rise = 1.15f + Mathf.Sin(phase * 0.42f) * 1.15f;
                particles[index].position = anchors[index] + new Vector3(
                    Mathf.Sin(phase) * 0.75f,
                    rise,
                    Mathf.Cos(phase * 0.74f) * 0.48f);
            }
        }
    }
}
