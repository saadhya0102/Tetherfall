using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed class CityDistrictDefinition
    {
        public string id;
        public string cityId;
        public string displayName;
        public string character;
        public Vector3 markerPosition;
        public float markerYaw;
    }

    public sealed class CityMarketStallDefinition
    {
        public string id;
        public string cityId;
        public string displayName;
        public string waresLabel;
        public string merchantName;
        public string merchantRole;
        public string vendorId;
        public string greeting;
        public Vector3 position;
        public float yaw;
    }

    public sealed class CityTraversalRouteDefinition
    {
        public string id;
        public string cityId;
        public string displayName;
        public string routeCharacter;
        public Vector3 startPosition;
        public Vector3[] checkpoints = Array.Empty<Vector3>();
        public float timeLimitSeconds;
        public CityActivityDefinition activity;
    }

    public static class CityDistrictCatalog
    {
        private static readonly CityDistrictDefinition[] AlderwakeDistricts =
        {
            District(
                "district.alderwake.growers_arcade",
                CityCatalog.AlderwakeHearthmarket.id,
                "Growers' Arcade",
                "Public kitchens, awning stalls, and growers' tallies",
                new Vector3(-15f, 0f, 59f),
                0f),
            District(
                "district.alderwake.guild_terrace",
                CityCatalog.AlderwakeHearthmarket.id,
                "Guild Terrace",
                "Wayfarer contracts and Threadwright craft courts",
                new Vector3(26f, 0f, 92f),
                180f),
            District(
                "district.alderwake.manor_row",
                CityCatalog.AlderwakeHearthmarket.id,
                "Manor Row",
                "Council petitions, courier homes, and roof gardens",
                new Vector3(-9f, 0f, 98f),
                180f)
        };

        private static readonly CityDistrictDefinition[] VeyrwaterDistricts =
        {
            District(
                "district.veyrwater.lantern_commons",
                CityCatalog.VeyrwaterMooring.id,
                "Lantern Commons",
                "Ferry stores, dry-goods canopies, and tide tallies",
                new Vector3(203f, 0f, 59f),
                0f),
            District(
                "district.veyrwater.lowtide_walk",
                CityCatalog.VeyrwaterMooring.id,
                "Lowtide Walk",
                "Auction lofts, service alleys, and courier rooflines",
                new Vector3(181f, 0f, 89f),
                90f),
            District(
                "district.veyrwater.council_rise",
                CityCatalog.VeyrwaterMooring.id,
                "Flood Council Rise",
                "Raised bridge courts and the living channel map",
                new Vector3(211f, 0f, 101f),
                180f)
        };

        private static readonly CityMarketStallDefinition[] AlderwakeStalls =
        {
            Stall(
                "stall.alderwake.rootbasket",
                CityCatalog.AlderwakeHearthmarket.id,
                "Rootbasket Growers' Stall",
                "herbs · fiber · kitchen bundles",
                "Sori Vell",
                "growers' broker",
                "vendor.alderwake.hearthmarket",
                "The co-op prices every basket in public. If a bundle is bruised, I tell you before the ledger does.",
                new Vector3(-9f, 0.9f, 65f),
                180f),
            Stall(
                "stall.alderwake.coppercord",
                CityCatalog.AlderwakeHearthmarket.id,
                "Coppercord Route Stall",
                "resin · cord · route fittings",
                "Bram Coil",
                "route-goods factor",
                "vendor.alderwake.hearthmarket",
                "Kite keeps the common ledger; I keep the roof crews from discovering they packed three left buckles.",
                new Vector3(14f, 0.9f, 75f),
                0f)
        };

        private static readonly CityMarketStallDefinition[] VeyrwaterStalls =
        {
            Stall(
                "stall.veyrwater.dryreed",
                CityCatalog.VeyrwaterMooring.id,
                "Dryreed Ferry Stall",
                "oilcloth · fiber · ferry packs",
                "Orris Wake",
                "dry-goods ferrier",
                "vendor.veyrwater.ferrier",
                "Everything here spent one full tide above the floodline. Luma signs the stock; I test the knots.",
                new Vector3(196f, 1.15f, 72f),
                90f),
            Stall(
                "stall.veyrwater.fogpantry",
                CityCatalog.VeyrwaterMooring.id,
                "Fogpantry Counter",
                "herbs · resin · channel meals",
                "Mey Sedge",
                "channel provisioner",
                "vendor.veyrwater.ferrier",
                "A ferry meal should be warm, quiet, and edible with one hand on the gunwale.",
                new Vector3(210f, 1.15f, 73f),
                -90f)
        };

        private static readonly CityTraversalRouteDefinition[] Routes =
        {
            Route(
                "route.alderwake.guild_roofs",
                CityCatalog.AlderwakeHearthmarket.id,
                "Guildroof Courier Circuit",
                "Copper ramps climb from a service alley across the Wayfarers roofs.",
                new Vector3(-26f, 0.18f, 98f),
                68f,
                Activity(
                    "city.alderwake.guildroof_circuit",
                    "Guildroof courier circuit",
                    "Run the marked service alley and guild roofline in order before the courier bell.",
                    8,
                    14,
                    240f),
                new Vector3(-31f, 0.65f, 98f),
                new Vector3(-36f, 1.35f, 95f),
                new Vector3(-41f, 2.15f, 92f),
                new Vector3(-43f, 3.15f, 88f),
                new Vector3(-39f, 4.05f, 84f),
                new Vector3(-34f, 3.65f, 82f),
                new Vector3(-29f, 2.75f, 86f),
                new Vector3(-25f, 1.6f, 91f),
                new Vector3(-23f, 0.25f, 96f)),
            Route(
                "route.veyrwater.lowtide_roofs",
                CityCatalog.VeyrwaterMooring.id,
                "Lowtide Loft Run",
                "Raised boardwalks cross auction eaves and descend beside the mooring canal.",
                new Vector3(180f, 0.25f, 71f),
                70f,
                Activity(
                    "city.veyrwater.lowtide_loft_run",
                    "Lowtide loft run",
                    "Follow the dry markers over the auction lofts before the tide bell changes.",
                    9,
                    15,
                    240f),
                new Vector3(181f, 0.65f, 76f),
                new Vector3(183f, 1.4f, 81f),
                new Vector3(186f, 2.3f, 85f),
                new Vector3(188f, 3.4f, 88f),
                new Vector3(193f, 4f, 90f),
                new Vector3(197f, 3.1f, 93f),
                new Vector3(194f, 2.1f, 97f),
                new Vector3(190f, 1.1f, 101f),
                new Vector3(187f, 0.25f, 105f))
        };

        private static readonly CityDistrictDefinition[] AllDistricts =
            Combine(AlderwakeDistricts, VeyrwaterDistricts);
        private static readonly CityMarketStallDefinition[] AllStalls =
            Combine(AlderwakeStalls, VeyrwaterStalls);
        private static readonly CityDistrictDefinition[] NoDistricts =
            Array.Empty<CityDistrictDefinition>();
        private static readonly CityMarketStallDefinition[] NoStalls =
            Array.Empty<CityMarketStallDefinition>();

        public static IReadOnlyList<CityDistrictDefinition> Districts => AllDistricts;
        public static IReadOnlyList<CityMarketStallDefinition> MarketStalls => AllStalls;
        public static IReadOnlyList<CityTraversalRouteDefinition> TraversalRoutes => Routes;

        public static IReadOnlyList<CityDistrictDefinition> GetDistrictsForCity(string cityId)
        {
            if (string.Equals(cityId, CityCatalog.AlderwakeHearthmarket.id, StringComparison.Ordinal))
            {
                return AlderwakeDistricts;
            }

            if (string.Equals(cityId, CityCatalog.VeyrwaterMooring.id, StringComparison.Ordinal))
            {
                return VeyrwaterDistricts;
            }

            return NoDistricts;
        }

        public static IReadOnlyList<CityMarketStallDefinition> GetMarketStallsForCity(string cityId)
        {
            if (string.Equals(cityId, CityCatalog.AlderwakeHearthmarket.id, StringComparison.Ordinal))
            {
                return AlderwakeStalls;
            }

            if (string.Equals(cityId, CityCatalog.VeyrwaterMooring.id, StringComparison.Ordinal))
            {
                return VeyrwaterStalls;
            }

            return NoStalls;
        }

        public static CityTraversalRouteDefinition GetTraversalRouteForCity(string cityId)
        {
            for (var index = 0; index < Routes.Length; index++)
            {
                if (string.Equals(Routes[index].cityId, cityId, StringComparison.Ordinal))
                {
                    return Routes[index];
                }
            }

            return null;
        }

        public static CityActivityDefinition FindActivity(string activityId)
        {
            if (string.IsNullOrEmpty(activityId))
            {
                return null;
            }

            for (var index = 0; index < Routes.Length; index++)
            {
                var activity = Routes[index]?.activity;
                if (activity != null &&
                    string.Equals(activity.id, activityId, StringComparison.Ordinal))
                {
                    return activity;
                }
            }

            return null;
        }

        private static CityDistrictDefinition District(
            string id,
            string cityId,
            string displayName,
            string character,
            Vector3 markerPosition,
            float markerYaw)
        {
            return new CityDistrictDefinition
            {
                id = id,
                cityId = cityId,
                displayName = displayName,
                character = character,
                markerPosition = markerPosition,
                markerYaw = markerYaw
            };
        }

        private static CityMarketStallDefinition Stall(
            string id,
            string cityId,
            string displayName,
            string waresLabel,
            string merchantName,
            string merchantRole,
            string vendorId,
            string greeting,
            Vector3 position,
            float yaw)
        {
            return new CityMarketStallDefinition
            {
                id = id,
                cityId = cityId,
                displayName = displayName,
                waresLabel = waresLabel,
                merchantName = merchantName,
                merchantRole = merchantRole,
                vendorId = vendorId,
                greeting = greeting,
                position = position,
                yaw = yaw
            };
        }

        private static CityTraversalRouteDefinition Route(
            string id,
            string cityId,
            string displayName,
            string routeCharacter,
            Vector3 startPosition,
            float timeLimitSeconds,
            CityActivityDefinition activity,
            params Vector3[] checkpoints)
        {
            return new CityTraversalRouteDefinition
            {
                id = id,
                cityId = cityId,
                displayName = displayName,
                routeCharacter = routeCharacter,
                startPosition = startPosition,
                timeLimitSeconds = timeLimitSeconds,
                activity = activity,
                checkpoints = checkpoints ?? Array.Empty<Vector3>()
            };
        }

        private static CityActivityDefinition Activity(
            string id,
            string title,
            string description,
            int favorReward,
            int experienceReward,
            float cooldownSeconds)
        {
            return new CityActivityDefinition
            {
                id = id,
                title = title,
                description = description,
                kind = CityActivityKind.Archive,
                favorReward = favorReward,
                experienceReward = experienceReward,
                cooldownSeconds = cooldownSeconds
            };
        }

        private static T[] Combine<T>(T[] first, T[] second)
        {
            var result = new T[first.Length + second.Length];
            Array.Copy(first, 0, result, 0, first.Length);
            Array.Copy(second, 0, result, first.Length, second.Length);
            return result;
        }
    }

    public static class CityDistrictBuilder
    {
        private const string DistrictRootName = "City explorable districts";

        public static CityTraversalCourse Build(
            TetherfallGame game,
            CityDirector city,
            CityVisualBuilder.CityArtPalette palette)
        {
            if (game == null || city?.Definition == null || palette == null)
            {
                return null;
            }

            var existingRoot = city.transform.Find(DistrictRootName);
            if (existingRoot != null)
            {
                return existingRoot.GetComponentInChildren<CityTraversalCourse>(true);
            }

            var root = new GameObject(DistrictRootName).transform;
            root.SetParent(city.transform, false);
            BuildDistrictMarkers(city.Definition, palette, root);
            BuildMarketStalls(game, city.Definition, palette, root);

            var route = CityDistrictCatalog.GetTraversalRouteForCity(city.Definition.id);
            if (route == null)
            {
                return null;
            }

            var course = CityTraversalCourse.Create(game, city, route, palette, root);
            game.RegisterInteractable(course);
            return course;
        }

        private static void BuildDistrictMarkers(
            CityDefinition city,
            CityVisualBuilder.CityArtPalette palette,
            Transform parent)
        {
            var harbor = string.Equals(
                city.id,
                CityCatalog.VeyrwaterMooring.id,
                StringComparison.Ordinal);
            var districts = CityDistrictCatalog.GetDistrictsForCity(city.id);
            for (var index = 0; index < districts.Count; index++)
            {
                var definition = districts[index];
                var marker = new GameObject(definition.displayName).transform;
                marker.SetParent(parent, false);
                marker.position = definition.markerPosition;
                marker.rotation = Quaternion.Euler(0f, definition.markerYaw, 0f);

                CityInstitutionBuilder.CreatePrimitive(
                    harbor ? PrimitiveType.Cylinder : PrimitiveType.Cube,
                    $"{definition.displayName} wayfinding post",
                    marker,
                    new Vector3(0f, 1.25f, 0f),
                    harbor
                        ? new Vector3(0.13f, 1.25f, 0.13f)
                        : new Vector3(0.22f, 1.25f, 0.22f),
                    harbor ? palette.timber : palette.stone,
                    false);
                CityInstitutionBuilder.CreatePrimitive(
                    PrimitiveType.Cube,
                    $"{definition.displayName} district blade",
                    marker,
                    new Vector3(0f, 2.35f, 0f),
                    new Vector3(2.9f, 0.38f, 0.18f),
                    palette.copper,
                    false);
                CityInstitutionBuilder.CreatePrimitive(
                    harbor ? PrimitiveType.Sphere : PrimitiveType.Cylinder,
                    $"{definition.displayName} district crest",
                    marker,
                    new Vector3(0f, 2.92f, 0f),
                    harbor
                        ? Vector3.one * 0.3f
                        : new Vector3(0.42f, 0.12f, 0.42f),
                    palette.marker,
                    false);
                WorldVisuals.AddLabel(
                    marker,
                    definition.displayName,
                    new Vector3(0f, 3.36f, 0f),
                    city.accentColor,
                    0.064f);
                WorldVisuals.AddLabel(
                    marker,
                    definition.character,
                    new Vector3(0f, 3.08f, 0f),
                    new Color(0.68f, 0.82f, 0.77f),
                    0.042f);
            }
        }

        private static void BuildMarketStalls(
            TetherfallGame game,
            CityDefinition city,
            CityVisualBuilder.CityArtPalette palette,
            Transform parent)
        {
            var stalls = CityDistrictCatalog.GetMarketStallsForCity(city.id);
            var harbor = string.Equals(
                city.id,
                CityCatalog.VeyrwaterMooring.id,
                StringComparison.Ordinal);
            for (var index = 0; index < stalls.Count; index++)
            {
                var definition = stalls[index];
                var stall = new GameObject(definition.displayName).transform;
                stall.SetParent(parent, false);
                stall.position = definition.position;
                stall.rotation = Quaternion.Euler(0f, definition.yaw, 0f);

                CityInstitutionBuilder.CreatePrimitive(
                    PrimitiveType.Cube,
                    $"{definition.displayName} counter",
                    stall,
                    new Vector3(0f, 0.55f, 1.15f),
                    new Vector3(3.5f, 1.05f, 1.05f),
                    palette.timber,
                    false);
                var canopy = CityInstitutionBuilder.CreatePrimitive(
                    PrimitiveType.Cube,
                    $"{definition.displayName} canopy",
                    stall,
                    new Vector3(0f, 2.85f, 0.85f),
                    new Vector3(4.6f, 0.2f, 2.9f),
                    index % 2 == 0 ? palette.canopy : palette.roof,
                    false);
                canopy.transform.localRotation = Quaternion.Euler(0f, 0f, index % 2 == 0 ? -3f : 3f);
                for (var side = -1; side <= 1; side += 2)
                {
                    CityInstitutionBuilder.CreatePrimitive(
                        PrimitiveType.Cylinder,
                        $"{definition.displayName} canopy post",
                        stall,
                        new Vector3(side * 1.9f, 1.45f, 1.55f),
                        new Vector3(0.08f, 1.4f, 0.08f),
                        harbor ? palette.copper : palette.timber,
                        false);
                }

                for (var ware = 0; ware < 3; ware++)
                {
                    CityInstitutionBuilder.CreatePrimitive(
                        harbor && ware == 1 ? PrimitiveType.Cube : PrimitiveType.Cylinder,
                        $"{definition.displayName} sorted wares",
                        stall,
                        new Vector3(-1.15f + ware * 1.15f, 1.18f, 1.02f),
                        harbor && ware == 1
                            ? new Vector3(0.62f, 0.4f, 0.55f)
                            : new Vector3(0.3f, 0.24f + ware * 0.06f, 0.3f),
                        ware == 2 ? palette.marker : palette.stone,
                        false);
                }

                var merchantPosition = definition.position +
                                       Quaternion.Euler(0f, definition.yaw, 0f) *
                                       new Vector3(0f, 0f, -0.7f);
                var merchant = VendorNpc.Create(
                    game,
                    definition.vendorId,
                    definition.merchantName,
                    definition.merchantRole,
                    merchantPosition,
                    definition.greeting,
                    false);
                if (merchant != null)
                {
                    merchant.transform.SetParent(stall, true);
                    merchant.transform.rotation = Quaternion.Euler(0f, definition.yaw, 0f);
                    var collider = merchant.GetComponent<Collider>();
                    if (collider != null)
                    {
                        CityInstitutionBuilder.RemoveCollider(collider);
                    }

                    var renderer = merchant.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        var generatedMaterial = renderer.sharedMaterial;
                        var sharedMaterial = index % 2 == 0 ? palette.wall : palette.canopy;
                        renderer.sharedMaterial = sharedMaterial;
                        if (generatedMaterial != null && generatedMaterial != sharedMaterial)
                        {
                            if (Application.isPlaying)
                            {
                                UnityEngine.Object.Destroy(generatedMaterial);
                            }
                            else
                            {
                                UnityEngine.Object.DestroyImmediate(generatedMaterial);
                            }
                        }
                    }

                    merchant.gameObject.AddComponent<CityMarketStallIdentity>().Initialize(definition);
                }

                WorldVisuals.AddLabel(
                    stall,
                    definition.displayName,
                    new Vector3(0f, 3.5f, 0.85f),
                    city.accentColor,
                    0.061f);
                WorldVisuals.AddLabel(
                    stall,
                    definition.waresLabel,
                    new Vector3(0f, 3.24f, 0.85f),
                    new Color(0.72f, 0.82f, 0.74f),
                    0.043f);
            }
        }
    }

    public sealed class CityMarketStallIdentity : MonoBehaviour
    {
        public string StallId => Definition?.id ?? string.Empty;
        public CityMarketStallDefinition Definition { get; private set; }

        public void Initialize(CityMarketStallDefinition definition)
        {
            Definition = definition;
        }
    }

    public sealed class CityTraversalCourse : MonoBehaviour, IWorldInteractable
    {
        private const float CheckpointRadiusSquared = 6.25f;

        private TetherfallGame game;
        private CityDirector city;
        private CityTraversalRouteDefinition definition;
        private int nextCheckpointIndex;
        private float startedAt;
        private bool active;

        public string RouteId => definition?.id ?? string.Empty;
        public CityTraversalRouteDefinition Definition => definition;
        public bool IsActive => active;
        public int NextCheckpointIndex => nextCheckpointIndex;

        public string InteractionPrompt
        {
            get
            {
                if (definition?.activity == null)
                {
                    return "Inspect city traversal route";
                }

                if (active)
                {
                    return $"{definition.displayName}: marker {Mathf.Min(nextCheckpointIndex + 1, definition.checkpoints.Length)}/" +
                           $"{definition.checkpoints.Length}";
                }

                if (game != null && !game.IsCityActivityAvailable(definition.activity))
                {
                    return $"{definition.displayName} reopens {game.GetCityActivityCooldownText(definition.activity)}";
                }

                return $"Run {definition.displayName}";
            }
        }

        public static CityTraversalCourse Create(
            TetherfallGame game,
            CityDirector city,
            CityTraversalRouteDefinition definition,
            CityVisualBuilder.CityArtPalette palette,
            Transform parent)
        {
            if (game == null ||
                city?.Definition == null ||
                definition == null ||
                definition.activity == null ||
                definition.checkpoints == null ||
                definition.checkpoints.Length < 2 ||
                palette == null ||
                parent == null)
            {
                return null;
            }

            var root = new GameObject(definition.displayName);
            root.transform.SetParent(parent, false);
            root.transform.position = definition.startPosition;
            var course = root.AddComponent<CityTraversalCourse>();
            course.game = game;
            course.city = city;
            course.definition = definition;
            course.BuildGeometry(palette);
            return course;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null &&
                   owner == game &&
                   city?.Definition != null &&
                   definition?.activity != null;
        }

        public void Interact(TetherfallGame owner)
        {
            if (!CanInteract(owner))
            {
                return;
            }

            if (active)
            {
                var remaining = Mathf.Max(0f, definition.timeLimitSeconds - (Time.time - startedAt));
                owner.Notify(
                    $"{definition.displayName}: marker {nextCheckpointIndex + 1}/" +
                    $"{definition.checkpoints.Length}, {Mathf.CeilToInt(remaining)} sec remain.");
                return;
            }

            if (!owner.IsCityActivityAvailable(definition.activity))
            {
                owner.Notify(
                    $"{definition.displayName} reopens {owner.GetCityActivityCooldownText(definition.activity)}.");
                return;
            }

            active = true;
            nextCheckpointIndex = 0;
            startedAt = Time.time;
            owner.Notify(
                $"{definition.displayName} started. Follow {definition.checkpoints.Length} markers in order " +
                $"within {Mathf.CeilToInt(definition.timeLimitSeconds)} seconds.");
        }

        public void RestoreRuntimeState()
        {
            active = false;
            nextCheckpointIndex = 0;
            startedAt = 0f;
        }

        private void Update()
        {
            if (!active ||
                game == null ||
                game.GameplayPaused ||
                game.PlayerTransform == null ||
                definition?.checkpoints == null)
            {
                return;
            }

            if (Time.time - startedAt > definition.timeLimitSeconds)
            {
                RestoreRuntimeState();
                game.Notify($"{definition.displayName} timed out. The courier bell resets immediately.");
                return;
            }

            if ((game.PlayerTransform.position - city.Definition.center).sqrMagnitude > 10000f)
            {
                RestoreRuntimeState();
                game.Notify($"{definition.displayName} reset when you left the city route.");
                return;
            }

            if (nextCheckpointIndex < 0 ||
                nextCheckpointIndex >= definition.checkpoints.Length)
            {
                RestoreRuntimeState();
                return;
            }

            var target = definition.checkpoints[nextCheckpointIndex];
            if ((game.PlayerTransform.position - target).sqrMagnitude > CheckpointRadiusSquared)
            {
                return;
            }

            nextCheckpointIndex++;
            if (nextCheckpointIndex < definition.checkpoints.Length)
            {
                game.Notify(
                    $"{definition.displayName}: marker {nextCheckpointIndex}/" +
                    $"{definition.checkpoints.Length} cleared.");
                return;
            }

            active = false;
            startedAt = 0f;
            nextCheckpointIndex = 0;
            game.CompleteCityActivity(definition.activity);
        }

        private void BuildGeometry(CityVisualBuilder.CityArtPalette palette)
        {
            var harbor = string.Equals(
                city.Definition.id,
                CityCatalog.VeyrwaterMooring.id,
                StringComparison.Ordinal);
            var points = new Vector3[definition.checkpoints.Length + 1];
            points[0] = definition.startPosition;
            Array.Copy(definition.checkpoints, 0, points, 1, definition.checkpoints.Length);

            BuildStartGate(palette, harbor);
            for (var index = 0; index < points.Length; index++)
            {
                BuildRoutePlatform(points[index], index, palette, harbor);
                if (index + 1 < points.Length)
                {
                    BuildRouteSegment(points[index], points[index + 1], index, palette, harbor);
                }
            }

            BuildFinishGate(points[points.Length - 1], palette, harbor);
            BuildServiceAlley(points, palette, harbor);
            WorldVisuals.AddLabel(
                transform,
                definition.displayName,
                new Vector3(0f, 4.25f, 0f),
                city.Definition.accentColor,
                0.071f);
            WorldVisuals.AddLabel(
                transform,
                definition.routeCharacter,
                new Vector3(0f, 3.9f, 0f),
                new Color(0.68f, 0.83f, 0.78f),
                0.045f);
        }

        private void BuildStartGate(
            CityVisualBuilder.CityArtPalette palette,
            bool harbor)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                CityInstitutionBuilder.CreatePrimitive(
                    PrimitiveType.Cylinder,
                    $"{definition.displayName} start post",
                    transform,
                    new Vector3(side * 1.35f, 1.5f, 0f),
                    new Vector3(0.12f, 1.5f, 0.12f),
                    harbor ? palette.timber : palette.copper,
                    false);
            }

            CityInstitutionBuilder.CreatePrimitive(
                PrimitiveType.Cube,
                $"{definition.displayName} start lintel",
                transform,
                new Vector3(0f, 3f, 0f),
                new Vector3(3f, 0.18f, 0.24f),
                palette.marker,
                false);
        }

        private void BuildFinishGate(
            Vector3 worldPosition,
            CityVisualBuilder.CityArtPalette palette,
            bool harbor)
        {
            var local = transform.InverseTransformPoint(worldPosition);
            for (var side = -1; side <= 1; side += 2)
            {
                CityInstitutionBuilder.CreatePrimitive(
                    PrimitiveType.Cylinder,
                    $"{definition.displayName} finish post",
                    transform,
                    local + new Vector3(side * 1.2f, 1.25f, 0f),
                    new Vector3(0.11f, 1.25f, 0.11f),
                    harbor ? palette.copper : palette.timber,
                    false);
            }

            CityInstitutionBuilder.CreatePrimitive(
                PrimitiveType.Cube,
                $"{definition.displayName} finish lintel",
                transform,
                local + new Vector3(0f, 2.52f, 0f),
                new Vector3(2.7f, 0.16f, 0.22f),
                palette.marker,
                false);
        }

        private void BuildRoutePlatform(
            Vector3 worldPosition,
            int index,
            CityVisualBuilder.CityArtPalette palette,
            bool harbor)
        {
            var local = transform.InverseTransformPoint(worldPosition);
            CityInstitutionBuilder.CreatePrimitive(
                PrimitiveType.Cube,
                $"{definition.displayName} traversal landing",
                transform,
                local + Vector3.down * 0.14f,
                new Vector3(2.25f, 0.28f, 2.25f),
                harbor
                    ? (index % 2 == 0 ? palette.timber : palette.stone)
                    : (index % 2 == 0 ? palette.stone : palette.timber),
                true);
            CityInstitutionBuilder.CreatePrimitive(
                harbor ? PrimitiveType.Sphere : PrimitiveType.Cylinder,
                $"{definition.displayName} ordered route marker",
                transform,
                local + Vector3.up * 0.42f,
                harbor
                    ? Vector3.one * 0.24f
                    : new Vector3(0.25f, 0.08f, 0.25f),
                palette.marker,
                false);

            if (index > 1 && index < definition.checkpoints.Length && index % 2 == 0)
            {
                var supportHeight = Mathf.Max(0.2f, worldPosition.y);
                CityInstitutionBuilder.CreatePrimitive(
                    PrimitiveType.Cylinder,
                    harbor
                        ? $"{definition.displayName} boardwalk stilt"
                        : $"{definition.displayName} roof brace",
                    transform,
                    new Vector3(local.x + 0.78f, local.y - supportHeight * 0.5f, local.z),
                    new Vector3(0.1f, supportHeight * 0.5f, 0.1f),
                    harbor ? palette.copper : palette.stone,
                    false);
            }
        }

        private void BuildRouteSegment(
            Vector3 from,
            Vector3 to,
            int index,
            CityVisualBuilder.CityArtPalette palette,
            bool harbor)
        {
            var delta = to - from;
            var distance = delta.magnitude;
            if (distance <= 0.05f)
            {
                return;
            }

            var segment = CityInstitutionBuilder.CreatePrimitive(
                PrimitiveType.Cube,
                harbor
                    ? $"{definition.displayName} raised boardwalk"
                    : $"{definition.displayName} copper roof ramp",
                transform,
                transform.InverseTransformPoint((from + to) * 0.5f),
                new Vector3(1.45f, 0.18f, distance),
                harbor
                    ? (index % 2 == 0 ? palette.timber : palette.canopy)
                    : (index % 2 == 0 ? palette.copper : palette.timber),
                true);
            segment.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }

        private void BuildServiceAlley(
            Vector3[] points,
            CityVisualBuilder.CityArtPalette palette,
            bool harbor)
        {
            var first = transform.InverseTransformPoint(points[0]);
            var second = transform.InverseTransformPoint(points[1]);
            var direction = second - first;
            direction.y = 0f;
            var forward = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.forward;
            var side = Vector3.Cross(Vector3.up, forward);

            for (var index = 0; index < 4; index++)
            {
                var position = first - forward * (1.7f + index * 1.35f) +
                               side * (index % 2 == 0 ? -1.55f : 1.55f);
                CityInstitutionBuilder.CreatePrimitive(
                    index % 3 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube,
                    harbor
                        ? $"{definition.displayName} mooring service freight"
                        : $"{definition.displayName} courier alley crate",
                    transform,
                    position + Vector3.up * (0.35f + index * 0.04f),
                    index % 3 == 0
                        ? new Vector3(0.32f, 0.34f, 0.32f)
                        : new Vector3(0.68f, 0.62f, 0.62f),
                    index == 3 ? palette.marker : (harbor ? palette.stone : palette.timber),
                    false);
            }

            for (var index = 0; index < 2; index++)
            {
                var servicePost = CityInstitutionBuilder.CreatePrimitive(
                    PrimitiveType.Cylinder,
                    harbor
                        ? $"{definition.displayName} tide pipe"
                        : $"{definition.displayName} rain chain",
                    transform,
                    first - forward * (2.4f + index * 2.4f) +
                    side * (index == 0 ? -2.2f : 2.2f) +
                    Vector3.up * 1.35f,
                    new Vector3(0.08f, 1.35f, 0.08f),
                    palette.copper,
                    false);
                servicePost.transform.localRotation = Quaternion.Euler(0f, 0f, index == 0 ? -4f : 4f);
            }
        }
    }
}
