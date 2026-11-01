using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public enum ExplorationLandmarkKind
    {
        Elevation,
        Cave,
        Tower
    }

    /// <summary>
    /// Authored regional terrain, caves, towers, and one-shot exploration rewards.
    /// The kit is intentionally primitive-based: every gameplay surface has a simple,
    /// predictable collider and every decorative piece shares one cached palette.
    /// </summary>
    public static class TetherfallExplorationLandmarks
    {
        public const string RootName = "Tetherfall Exploration Landmarks";
        public const int RegionCount = 3;
        public const int CaveCount = 3;
        public const int TowerCount = 3;
        public const int SecretCount = 3;
        public const int RewardCount = 9;
        public const int CachedMaterialCount = 13;
        public const int MaxRendererBudget = 300;
        public const int MaxColliderBudget = 180;
        public const int MaxSharedMaterialBudget = 32;
        public const int AddedLightBudget = 0;

        private const float CaveRouteLength = 35f;
        private const string TowerRampName = "Walkable tower switchback ramp";
        private const string CaveFloorName = "Traversable cave route floor";

        private static readonly RegionSpec[] Regions =
        {
            new RegionSpec
            {
                key = "alderwake",
                regionId = "Alderwake Basin",
                elevationId = "landmark.alderwake.rootcrown_rise",
                elevationName = "Rootcrown Rise",
                elevationOrigin = new Vector3(68f, 0f, -20f),
                elevationYaw = 90f,
                caveId = "landmark.alderwake.mossvault_grotto",
                caveName = "Mossvault Grotto",
                caveEntrance = new Vector3(40f, 0f, -39f),
                caveYaw = 90f,
                towerId = "landmark.alderwake.greenwake_tower",
                towerName = "Greenwake Survey Tower",
                towerOrigin = new Vector3(72f, 0f, 8f),
                towerYaw = 0f,
                secret = new RewardSpec(
                    "secret.alderwake.raincut_overlook",
                    "Raincut Overlook Stone",
                    "Read the raincut route stone",
                    ResourceKind.Fiber,
                    2,
                    25,
                    "The old routewrights left the ridge open so every traveler could see the safe way down."),
                caveReward = new RewardSpec(
                    "reward.alderwake.mossvault_cache",
                    "Mossvault Survey Cache",
                    "Recover the Mossvault survey cache",
                    ResourceKind.Resin,
                    3,
                    30,
                    "A dry route case survived beneath the rootstone."),
                towerReward = new RewardSpec(
                    "reward.alderwake.greenwake_summit",
                    "Greenwake Summit Cache",
                    "Survey Alderwake from the summit",
                    ResourceKind.Timber,
                    3,
                    35,
                    "The high terraces resolve into one continuous green road.")
            },
            new RegionSpec
            {
                key = "morrowfen",
                regionId = "Morrowfen",
                elevationId = "landmark.morrowfen.siltstep_terraces",
                elevationName = "Siltstep Terraces",
                elevationOrigin = new Vector3(228f, 0f, 8f),
                elevationYaw = 90f,
                caveId = "landmark.morrowfen.reed_hollow",
                caveName = "Drowned Reed Hollow",
                caveEntrance = new Vector3(198f, 0f, -4f),
                caveYaw = 90f,
                towerId = "landmark.morrowfen.lanternwatch_spire",
                towerName = "Lanternwatch Spire",
                towerOrigin = new Vector3(209f, 0f, 34f),
                towerYaw = 0f,
                secret = new RewardSpec(
                    "secret.morrowfen.listening_stone",
                    "Stillwater Listening Stone",
                    "Listen at the stillwater stone",
                    ResourceKind.Herbs,
                    3,
                    25,
                    "Beneath the reed-noise, an old ferryman's cadence still marks the dry crossings."),
                caveReward = new RewardSpec(
                    "reward.morrowfen.reed_hollow_cache",
                    "Reed Hollow Field Case",
                    "Recover the drowned field case",
                    ResourceKind.Chimesalt,
                    2,
                    35,
                    "Waxed survey notes record a path the flood could not erase."),
                towerReward = new RewardSpec(
                    "reward.morrowfen.lanternwatch_summit",
                    "Lanternwatch Summit Gauge",
                    "Read the Lanternwatch gauge",
                    ResourceKind.Fiber,
                    3,
                    35,
                    "Every living lantern in the fen answers from this height.")
            },
            new RegionSpec
            {
                key = "cinderwold",
                regionId = "Cinderwold Verge",
                elevationId = "landmark.cinderwold.ashglass_ridges",
                elevationName = "Ashglass Ridges",
                elevationOrigin = new Vector3(-228f, 0f, 8f),
                elevationYaw = -90f,
                caveId = "landmark.cinderwold.embervein_refuge",
                caveName = "Embervein Refuge",
                caveEntrance = new Vector3(-198f, 0f, -4f),
                caveYaw = -90f,
                towerId = "landmark.cinderwold.cinderwatch_relay",
                towerName = "Cinderwatch Relay",
                towerOrigin = new Vector3(-209f, 0f, 34f),
                towerYaw = 0f,
                hasCombatCave = true,
                secret = new RewardSpec(
                    "secret.cinderwold.windscar_cairn",
                    "Windscar Witness Cairn",
                    "Read the windscar witness marks",
                    ResourceKind.Ore,
                    2,
                    30,
                    "The marks count rescues, not victories. Someone kept returning through the ash."),
                caveReward = new RewardSpec(
                    "reward.cinderwold.embervein_cache",
                    "Embervein Salvage Case",
                    "Recover the Embervein salvage case",
                    ResourceKind.Ore,
                    3,
                    40,
                    "Heat-scored fittings remain usable after the refuge falls quiet."),
                towerReward = new RewardSpec(
                    "reward.cinderwold.cinderwatch_summit",
                    "Cinderwatch Summit Coil",
                    "Survey Cinderwold from the relay",
                    ResourceKind.Chimesalt,
                    2,
                    45,
                    "The ash shelves reveal their switchbacks when viewed against the ember wind.")
            }
        };

        private static readonly Vector3[] AuthoredScales =
        {
            new Vector3(18f, 0.65f, 24f),
            new Vector3(13f, 1.4f, 14f),
            new Vector3(8.5f, 1.6f, 7f),
            new Vector3(3f, 3f, 12f),
            new Vector3(5f, 0.32f, 8f),
            new Vector3(7.5f, 0.18f, 24f),
            new Vector3(0.7f, 4.7f, 24f),
            new Vector3(7.5f, 0.5f, 24f),
            new Vector3(14f, 0.18f, 12f),
            new Vector3(0.7f, 4.7f, 12f),
            new Vector3(14f, 0.5f, 12f),
            new Vector3(8f, 0.36f, 16f),
            new Vector3(3.2f, 0.34f, 11f),
            new Vector3(0.16f, 0.8f, 11f)
        };

        public static GameObject Build(TetherfallGame game)
        {
            if (game == null)
            {
                Debug.LogError("Exploration landmarks require a TetherfallGame owner.");
                return null;
            }

            var existing = FindExistingRoot(game.transform);
            if (existing != null)
            {
                return existing;
            }

            if (!ValidateAuthoring(out var authoringReport))
            {
                Debug.LogError("Exploration landmark authoring is invalid: " + authoringReport);
                return null;
            }

            var root = new GameObject(RootName);
            root.transform.SetParent(game.transform, false);
            var context = new BuildContext(game, new LandmarkPalette(game));

            for (var index = 0; index < Regions.Length; index++)
            {
                BuildRegion(context, root.transform, Regions[index], index);
            }

            var buildRoot = root.AddComponent<ExplorationLandmarkBuildRoot>();
            buildRoot.Initialize(context.primitiveRendererCount, context.solidColliderCount);

            if (!ValidateBuiltRoot(root, out var builtReport))
            {
                Debug.LogError("Exploration landmark build verification failed: " + builtReport);
            }
            else
            {
                Debug.Log(
                    "Exploration landmarks ready: " + builtReport +
                    ". Authored routes remain on the untouched foundation.");
            }

            return root;
        }

        public static bool ValidateAuthoring(out string report)
        {
            var failures = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var regions = new HashSet<string>(StringComparer.Ordinal);
            var combatCaves = 0;

            if (Regions.Length != RegionCount)
            {
                failures.Add($"expected {RegionCount} regional compositions, found {Regions.Length}");
            }

            foreach (var spec in Regions)
            {
                if (spec == null)
                {
                    failures.Add("a regional landmark specification is null");
                    continue;
                }

                regions.Add(spec.regionId);
                RegisterId(ids, spec.elevationId, failures);
                RegisterId(ids, spec.caveId, failures);
                RegisterId(ids, spec.towerId, failures);
                RegisterId(ids, spec.secret?.id, failures);
                RegisterId(ids, spec.caveReward?.id, failures);
                RegisterId(ids, spec.towerReward?.id, failures);
                if (spec.hasCombatCave)
                {
                    combatCaves++;
                }

                ValidatePosition(spec.elevationOrigin, spec.elevationId, failures);
                ValidatePosition(spec.caveEntrance, spec.caveId, failures);
                ValidatePosition(spec.towerOrigin, spec.towerId, failures);

                var caveEnd = spec.caveEntrance +
                              Quaternion.Euler(0f, spec.caveYaw, 0f) *
                              Vector3.forward * CaveRouteLength;
                ValidatePosition(caveEnd, spec.caveId + " route end", failures);
                if (!IsInsideRegion(spec.regionId, spec.elevationOrigin.x) ||
                    !IsInsideRegion(spec.regionId, spec.caveEntrance.x) ||
                    !IsInsideRegion(spec.regionId, caveEnd.x) ||
                    !IsInsideRegion(spec.regionId, spec.towerOrigin.x))
                {
                    failures.Add($"{spec.regionId} landmarks leave their authored regional x bounds");
                }
            }

            if (regions.Count != RegionCount)
            {
                failures.Add($"expected {RegionCount} unique regions, found {regions.Count}");
            }

            if (combatCaves != 1)
            {
                failures.Add($"expected one optional combat cave, found {combatCaves}");
            }

            for (var index = 0; index < AuthoredScales.Length; index++)
            {
                var scale = AuthoredScales[index];
                if (!IsFinite(scale) ||
                    scale.x <= 0f ||
                    scale.y <= 0f ||
                    scale.z <= 0f ||
                    Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)) > 40f)
                {
                    failures.Add($"authored scale {index} is invalid: {scale}");
                }
            }

            report = failures.Count == 0
                ? $"{RegionCount} regions, {CaveCount} caves, {TowerCount} towers, " +
                  $"{SecretCount} secrets, one combat cave, and {RewardCount} one-shot rewards validated"
                : string.Join("; ", failures);
            return failures.Count == 0;
        }

        public static bool ValidateBuiltRoot(GameObject root, out string report)
        {
            var failures = new List<string>();
            if (root == null)
            {
                report = "landmark root is missing";
                return false;
            }

            var markers = root.GetComponentsInChildren<ExplorationLandmarkMarker>(true);
            var rewards = root.GetComponentsInChildren<ExplorationReward>(true);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var regionKinds = new HashSet<string>(StringComparer.Ordinal);
            var elevationCount = 0;
            var caveCount = 0;
            var towerCount = 0;

            foreach (var marker in markers)
            {
                RegisterId(ids, marker.LandmarkId, failures);
                ValidatePosition(marker.transform.position, marker.LandmarkId, failures);
                regionKinds.Add(marker.RegionId + "|" + marker.Kind);
                switch (marker.Kind)
                {
                    case ExplorationLandmarkKind.Elevation:
                        elevationCount++;
                        break;
                    case ExplorationLandmarkKind.Cave:
                        caveCount++;
                        break;
                    case ExplorationLandmarkKind.Tower:
                        towerCount++;
                        break;
                }
            }

            foreach (var reward in rewards)
            {
                RegisterId(ids, reward.RewardId, failures);
                ValidatePosition(reward.transform.position, reward.RewardId, failures);
            }

            if (elevationCount != RegionCount ||
                caveCount != CaveCount ||
                towerCount != TowerCount)
            {
                failures.Add(
                    $"logical marker counts are elevations={elevationCount}, caves={caveCount}, " +
                    $"towers={towerCount}");
            }

            if (rewards.Length != RewardCount)
            {
                failures.Add($"expected {RewardCount} one-shot rewards, found {rewards.Length}");
            }

            foreach (var spec in Regions)
            {
                foreach (ExplorationLandmarkKind kind in Enum.GetValues(typeof(ExplorationLandmarkKind)))
                {
                    if (!regionKinds.Contains(spec.regionId + "|" + kind))
                    {
                        failures.Add($"{spec.regionId} is missing its {kind} marker");
                    }
                }
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var colliders = root.GetComponentsInChildren<Collider>(true);
            var lights = root.GetComponentsInChildren<Light>(true);
            var materials = new HashSet<Material>();
            var enabledColliderCount = 0;

            foreach (var renderer in renderers)
            {
                ValidateTransform(renderer.transform, failures);
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        materials.Add(material);
                    }
                }
            }

            foreach (var itemCollider in colliders)
            {
                ValidateTransform(itemCollider.transform, failures);
                if (itemCollider.enabled)
                {
                    enabledColliderCount++;
                }
            }

            if (renderers.Length > MaxRendererBudget)
            {
                failures.Add($"renderer budget exceeded ({renderers.Length}/{MaxRendererBudget})");
            }

            if (enabledColliderCount > MaxColliderBudget)
            {
                failures.Add($"collider budget exceeded ({enabledColliderCount}/{MaxColliderBudget})");
            }

            if (materials.Count > MaxSharedMaterialBudget)
            {
                failures.Add(
                    $"shared-material budget exceeded ({materials.Count}/{MaxSharedMaterialBudget})");
            }

            if (lights.Length != AddedLightBudget)
            {
                failures.Add($"expected no Light components, found {lights.Length}");
            }

            var towerRamps = 0;
            var caveFloors = 0;
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name == TowerRampName)
                {
                    towerRamps++;
                    if (item.localScale.x < 3f)
                    {
                        failures.Add("a tower ramp is narrower than the three-unit safe-route minimum");
                    }
                }

                if (item.name == CaveFloorName)
                {
                    caveFloors++;
                    if (item.localScale.x < 7f)
                    {
                        failures.Add("a cave route is narrower than the seven-unit cave minimum");
                    }
                }
            }

            if (towerRamps != TowerCount * 4)
            {
                failures.Add($"expected {TowerCount * 4} tower ramps, found {towerRamps}");
            }

            if (caveFloors != CaveCount)
            {
                failures.Add($"expected {CaveCount} continuous cave route floors, found {caveFloors}");
            }

            var activeLabels = root.GetComponentsInChildren<TextMesh>(false);
            if (activeLabels.Length != CaveCount)
            {
                failures.Add(
                    $"expected one entrance label per cave and no interior labels, found {activeLabels.Length}");
            }

            foreach (var label in activeLabels)
            {
                var nearestEntrance = float.MaxValue;
                foreach (var marker in markers)
                {
                    if (marker.Kind == ExplorationLandmarkKind.Cave)
                    {
                        nearestEntrance = Mathf.Min(
                            nearestEntrance,
                            Vector3.Distance(label.transform.position, marker.transform.position));
                    }
                }

                if (nearestEntrance > 8f)
                {
                    failures.Add($"{label.name} is not positioned at a cave entrance");
                }
            }

            report = failures.Count == 0
                ? $"{renderers.Length}/{MaxRendererBudget} renderers, " +
                  $"{enabledColliderCount}/{MaxColliderBudget} colliders, " +
                  $"{materials.Count}/{MaxSharedMaterialBudget} shared materials, " +
                  $"{lights.Length} lights"
                : string.Join("; ", failures);
            return failures.Count == 0;
        }

        private static void BuildRegion(
            BuildContext context,
            Transform parent,
            RegionSpec spec,
            int regionIndex)
        {
            var region = new GameObject(spec.regionId + " Exploration Composition").transform;
            region.SetParent(parent, false);
            var materials = context.palette.regions[regionIndex];

            BuildElevation(context, region, spec, materials);
            var cave = BuildCave(context, region, spec, materials);
            BuildTower(context, region, spec, materials, regionIndex);

            if (spec.hasCombatCave && Application.isPlaying)
            {
                var spawnPosition = cave.TransformPoint(new Vector3(-2.2f, 0.2f, 29f));
                var caveGuardian = Enemy.Create(
                    context.game,
                    null,
                    "enemy.ashjaw_brute",
                    spawnPosition);
                if (caveGuardian != null)
                {
                    caveGuardian.name = "Embervein Ashjaw Guardian";
                    caveGuardian.transform.SetParent(cave, true);
                    RemoveInteriorLabels(caveGuardian.transform);
                }
            }
        }

        private static void BuildElevation(
            BuildContext context,
            Transform parent,
            RegionSpec spec,
            RegionMaterials materials)
        {
            var root = CreateLandmarkRoot(
                parent,
                spec.elevationName,
                spec.elevationOrigin,
                spec.elevationYaw,
                spec.elevationId,
                spec.regionId,
                ExplorationLandmarkKind.Elevation);

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Broad lower foothill shelf",
                root,
                new Vector3(0f, 0.325f, 0f),
                new Vector3(18f, 0.65f, 24f),
                Quaternion.identity,
                materials.terrain,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Broad middle route terrace",
                root,
                new Vector3(-1.5f, 1.4f, 5.5f),
                new Vector3(13f, 1.4f, 14f),
                Quaternion.identity,
                materials.stone,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Walkable regional ridge crown",
                root,
                new Vector3(1f, 2.9f, 8.5f),
                new Vector3(8.5f, 1.6f, 7f),
                Quaternion.identity,
                materials.terrain,
                true);

            CreateRamp(
                context,
                root,
                "Wide foothill approach ramp",
                new Vector3(0f, -0.16f, -18f),
                new Vector3(0f, 0.49f, -11.5f),
                5f,
                0.32f,
                materials.stone);
            CreateRamp(
                context,
                root,
                "Wide terrace switchback ramp",
                new Vector3(-3f, 0.54f, -8.5f),
                new Vector3(-3f, 1.94f, -1.5f),
                4.6f,
                0.32f,
                materials.stone);
            CreateRamp(
                context,
                root,
                "Wide ridge ascent ramp",
                new Vector3(1f, 1.94f, 0f),
                new Vector3(1f, 3.54f, 5f),
                4.2f,
                0.32f,
                materials.stone);

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Regional ridge shoulder",
                root,
                new Vector3(-7f, 1.5f, 5f),
                new Vector3(3f, 3f, 12f),
                Quaternion.Euler(0f, -4f, 2f),
                materials.terrain,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Regional ridge shoulder",
                root,
                new Vector3(7f, 1.35f, 6.5f),
                new Vector3(2.6f, 2.7f, 10f),
                Quaternion.Euler(0f, 5f, -2f),
                materials.terrain,
                true);

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Overlook left parapet",
                root,
                new Vector3(-3.08f, 4.15f, 8.5f),
                new Vector3(0.32f, 0.9f, 7f),
                Quaternion.identity,
                materials.structure,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Overlook right parapet",
                root,
                new Vector3(5.08f, 4.15f, 8.5f),
                new Vector3(0.32f, 0.9f, 7f),
                Quaternion.identity,
                materials.structure,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Overlook outer parapet",
                root,
                new Vector3(1f, 4.15f, 11.84f),
                new Vector3(8.5f, 0.9f, 0.32f),
                Quaternion.identity,
                materials.structure,
                true);

            for (var index = 0; index < 4; index++)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    "Inset route stitch",
                    root,
                    new Vector3(-1.9f + index * 1.25f, 0.69f, -6.2f + index * 1.1f),
                    new Vector3(0.65f, 0.035f, 1.25f),
                    Quaternion.Euler(0f, -8f + index * 5f, 0f),
                    materials.accent,
                    false);
            }

            CreateReward(
                context,
                root,
                new Vector3(1f, 3.73f, 9.25f),
                spec.secret,
                materials);
        }

        private static Transform BuildCave(
            BuildContext context,
            Transform parent,
            RegionSpec spec,
            RegionMaterials materials)
        {
            var root = CreateLandmarkRoot(
                parent,
                spec.caveName,
                spec.caveEntrance,
                spec.caveYaw,
                spec.caveId,
                spec.regionId,
                ExplorationLandmarkKind.Cave);

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                CaveFloorName,
                root,
                new Vector3(0f, 0.09f, 12f),
                new Vector3(7.5f, 0.18f, 24f),
                Quaternion.identity,
                materials.stone,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Collision-safe cave left wall",
                root,
                new Vector3(-3.75f, 2.35f, 12f),
                new Vector3(0.7f, 4.7f, 24f),
                Quaternion.identity,
                materials.terrain,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Collision-safe cave right wall",
                root,
                new Vector3(3.75f, 2.35f, 12f),
                new Vector3(0.7f, 4.7f, 24f),
                Quaternion.identity,
                materials.terrain,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Collision-safe cave route ceiling",
                root,
                new Vector3(0f, 4.8f, 12f),
                new Vector3(7.5f, 0.5f, 24f),
                Quaternion.identity,
                materials.terrain,
                true);

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Cave chamber floor",
                root,
                new Vector3(0f, 0.09f, 29f),
                new Vector3(14f, 0.18f, 12f),
                Quaternion.identity,
                materials.stone,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Cave chamber left wall",
                root,
                new Vector3(-7f, 2.35f, 29f),
                new Vector3(0.7f, 4.7f, 12f),
                Quaternion.identity,
                materials.terrain,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Cave chamber right wall",
                root,
                new Vector3(7f, 2.35f, 29f),
                new Vector3(0.7f, 4.7f, 12f),
                Quaternion.identity,
                materials.terrain,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Cave chamber back wall",
                root,
                new Vector3(0f, 2.35f, 35f),
                new Vector3(14f, 4.7f, 0.7f),
                Quaternion.identity,
                materials.terrain,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Cave chamber front left shoulder",
                root,
                new Vector3(-5.25f, 2.35f, 23f),
                new Vector3(3.5f, 4.7f, 0.7f),
                Quaternion.identity,
                materials.terrain,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Cave chamber front right shoulder",
                root,
                new Vector3(5.25f, 2.35f, 23f),
                new Vector3(3.5f, 4.7f, 0.7f),
                Quaternion.identity,
                materials.terrain,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Collision-safe cave chamber ceiling",
                root,
                new Vector3(0f, 4.8f, 29f),
                new Vector3(14f, 0.5f, 12f),
                Quaternion.identity,
                materials.terrain,
                true);

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Readable cave entrance left pier",
                root,
                new Vector3(-3.75f, 2.3f, -0.15f),
                new Vector3(0.9f, 4.6f, 1.2f),
                Quaternion.Euler(0f, 0f, -3f),
                materials.stone,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Readable cave entrance right pier",
                root,
                new Vector3(3.75f, 2.3f, -0.15f),
                new Vector3(0.9f, 4.6f, 1.2f),
                Quaternion.Euler(0f, 0f, 3f),
                materials.stone,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                "Readable cave entrance lintel",
                root,
                new Vector3(0f, 4.72f, -0.15f),
                new Vector3(8.4f, 0.65f, 1.2f),
                Quaternion.identity,
                materials.stone,
                true);

            var shellPositions = new[]
            {
                new Vector3(-4.5f, 2.8f, 2f),
                new Vector3(4.5f, 2.65f, 3.2f),
                new Vector3(-4.45f, 2.9f, 13f),
                new Vector3(4.4f, 3.05f, 18f)
            };
            for (var index = 0; index < shellPositions.Length; index++)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Sphere,
                    "Faceted cave shell mass",
                    root,
                    shellPositions[index],
                    new Vector3(2.35f, 3.15f, 4.5f),
                    Quaternion.Euler(index * 7f, index * 23f, index % 2 == 0 ? 8f : -7f),
                    materials.terrain,
                    false);
            }

            for (var index = 0; index < 6; index++)
            {
                var side = index % 2 == 0 ? -1f : 1f;
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    "Ambient cave mineral seam",
                    root,
                    new Vector3(side * 3.3f, 1.05f + index % 3 * 0.42f, 5f + index * 4.4f),
                    new Vector3(0.14f, 0.72f + index % 2 * 0.25f, 0.14f),
                    Quaternion.Euler(side * 13f, index * 31f, side * 17f),
                    materials.accent,
                    false);
            }

            for (var sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                var side = sideIndex == 0 ? -1f : 1f;
                CreatePrimitive(
                    context,
                    PrimitiveType.Cylinder,
                    "Cave entrance waymark stem",
                    root,
                    new Vector3(side * 3.05f, 1.05f, -0.78f),
                    new Vector3(0.11f, 1.05f, 0.11f),
                    Quaternion.identity,
                    materials.structure,
                    false);
                CreatePrimitive(
                    context,
                    PrimitiveType.Sphere,
                    "Cave entrance emissive waymark",
                    root,
                    new Vector3(side * 3.05f, 2.18f, -0.78f),
                    new Vector3(0.32f, 0.46f, 0.32f),
                    Quaternion.identity,
                    materials.accent,
                    false);
            }

            CreateReward(
                context,
                root,
                new Vector3(3f, 0.12f, 29.5f),
                spec.caveReward,
                materials);
            WorldVisuals.AddLabel(
                root,
                spec.caveName,
                new Vector3(0f, 5.55f, -0.9f),
                materials.labelColor,
                0.1f);
            return root;
        }

        private static void BuildTower(
            BuildContext context,
            Transform parent,
            RegionSpec spec,
            RegionMaterials materials,
            int regionIndex)
        {
            var root = CreateLandmarkRoot(
                parent,
                spec.towerName,
                spec.towerOrigin,
                spec.towerYaw,
                spec.towerId,
                spec.regionId,
                ExplorationLandmarkKind.Tower);

            for (var xIndex = 0; xIndex < 2; xIndex++)
            {
                for (var zIndex = 0; zIndex < 2; zIndex++)
                {
                    CreatePrimitive(
                        context,
                        PrimitiveType.Cube,
                        "Tower load-bearing pier",
                        root,
                        new Vector3(xIndex == 0 ? -3.55f : 3.55f, 6f, zIndex == 0 ? -6f : 6f),
                        new Vector3(0.5f, 12f, 0.5f),
                        Quaternion.identity,
                        materials.structure,
                        true);
                }
            }

            CreateTowerDeck(
                context,
                root,
                "Tower south landing",
                new Vector3(0f, 2.82f, -6f),
                new Vector3(8f, 0.36f, 4f),
                materials.stone);
            CreateTowerDeck(
                context,
                root,
                "Tower north landing",
                new Vector3(0f, 5.82f, 6f),
                new Vector3(8f, 0.36f, 4f),
                materials.stone);
            CreateTowerDeck(
                context,
                root,
                "Tower high south landing",
                new Vector3(0f, 8.82f, -6f),
                new Vector3(8f, 0.36f, 4f),
                materials.stone);
            CreateTowerDeck(
                context,
                root,
                "Tower summit viewpoint",
                new Vector3(0f, 11.82f, 0f),
                new Vector3(8f, 0.36f, 16f),
                materials.stone);

            CreateGuardedTowerRamp(
                context,
                root,
                new Vector3(-1.65f, -0.17f, -17f),
                new Vector3(-1.65f, 2.83f, -7f),
                materials);
            CreateGuardedTowerRamp(
                context,
                root,
                new Vector3(1.65f, 2.83f, -5f),
                new Vector3(1.65f, 5.83f, 5f),
                materials);
            CreateGuardedTowerRamp(
                context,
                root,
                new Vector3(-1.65f, 5.83f, 5f),
                new Vector3(-1.65f, 8.83f, -5f),
                materials);
            CreateGuardedTowerRamp(
                context,
                root,
                new Vector3(1.65f, 8.83f, -5f),
                new Vector3(1.65f, 11.83f, 5f),
                materials);

            CreateRail(
                context,
                root,
                "Lower landing outer railing",
                new Vector3(0f, 3.55f, -7.82f),
                new Vector3(8f, 0.9f, 0.18f),
                materials.structure);
            CreateRail(
                context,
                root,
                "Middle landing outer railing",
                new Vector3(0f, 6.55f, 7.82f),
                new Vector3(8f, 0.9f, 0.18f),
                materials.structure);
            CreateRail(
                context,
                root,
                "High landing outer railing",
                new Vector3(0f, 9.55f, -7.82f),
                new Vector3(8f, 0.9f, 0.18f),
                materials.structure);

            CreateRail(
                context,
                root,
                "Summit west railing",
                new Vector3(-3.88f, 12.55f, 0f),
                new Vector3(0.18f, 1.1f, 16f),
                materials.structure);
            CreateRail(
                context,
                root,
                "Summit east railing",
                new Vector3(3.88f, 12.55f, 0f),
                new Vector3(0.18f, 1.1f, 16f),
                materials.structure);
            CreateRail(
                context,
                root,
                "Summit south railing",
                new Vector3(0f, 12.55f, -7.88f),
                new Vector3(8f, 1.1f, 0.18f),
                materials.structure);
            CreateRail(
                context,
                root,
                "Summit north railing",
                new Vector3(0f, 12.55f, 7.88f),
                new Vector3(8f, 1.1f, 0.18f),
                materials.structure);

            CreateTowerCrown(context, root, materials, regionIndex);
            CreateReward(
                context,
                root,
                new Vector3(0f, 12.03f, 0f),
                spec.towerReward,
                materials);
        }

        private static void CreateTowerDeck(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name,
                parent,
                localPosition,
                localScale,
                Quaternion.identity,
                material,
                true);
        }

        private static void CreateGuardedTowerRamp(
            BuildContext context,
            Transform parent,
            Vector3 start,
            Vector3 end,
            RegionMaterials materials)
        {
            const float width = 3.2f;
            const float thickness = 0.34f;
            var difference = end - start;
            var rotation = Quaternion.LookRotation(difference.normalized, Vector3.up);
            var center = (start + end) * 0.5f;
            var length = difference.magnitude;

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                TowerRampName,
                parent,
                center,
                new Vector3(width, thickness, length),
                rotation,
                materials.stone,
                true);

            for (var sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                var side = sideIndex == 0 ? -1f : 1f;
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    "Tower ramp continuous safety railing",
                    parent,
                    center + rotation * new Vector3(side * 1.52f, 0.58f, 0f),
                    new Vector3(0.16f, 0.8f, length),
                    rotation,
                    materials.structure,
                    true);
            }
        }

        private static void CreateRail(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name,
                parent,
                localPosition,
                localScale,
                Quaternion.identity,
                material,
                true);
        }

        private static void CreateTowerCrown(
            BuildContext context,
            Transform parent,
            RegionMaterials materials,
            int regionIndex)
        {
            var mastType = regionIndex == 1 ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            var crownName = regionIndex == 0
                ? "Greenwake branch crown"
                : regionIndex == 1
                    ? "Lanternwatch reed crown"
                    : "Cinderwatch relay crown";

            CreatePrimitive(
                context,
                mastType,
                crownName,
                parent,
                new Vector3(0f, 14f, -2.8f),
                new Vector3(0.34f, 2f, 0.34f),
                Quaternion.identity,
                materials.structure,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                crownName + " left vane",
                parent,
                new Vector3(-0.62f, 14.6f, -2.8f),
                new Vector3(0.16f, 2.8f, 0.56f),
                Quaternion.Euler(0f, -18f, -24f),
                materials.accent,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                crownName + " right vane",
                parent,
                new Vector3(0.62f, 14.6f, -2.8f),
                new Vector3(0.16f, 2.8f, 0.56f),
                Quaternion.Euler(0f, 18f, 24f),
                materials.accent,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Sphere,
                crownName + " summit lens",
                parent,
                new Vector3(0f, 16.15f, -2.8f),
                new Vector3(0.68f, 0.86f, 0.68f),
                Quaternion.identity,
                materials.accent,
                false);
        }

        private static void CreateRamp(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 start,
            Vector3 end,
            float width,
            float thickness,
            Material material)
        {
            var difference = end - start;
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name,
                parent,
                (start + end) * 0.5f,
                new Vector3(width, thickness, difference.magnitude),
                Quaternion.LookRotation(difference.normalized, Vector3.up),
                material,
                true);
        }

        private static ExplorationReward CreateReward(
            BuildContext context,
            Transform parent,
            Vector3 localPosition,
            RewardSpec spec,
            RegionMaterials materials)
        {
            var root = new GameObject(spec.displayName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            var body = CreatePrimitive(
                context,
                PrimitiveType.Cube,
                spec.displayName + " case",
                root.transform,
                new Vector3(0f, 0.42f, 0f),
                new Vector3(1.25f, 0.78f, 0.92f),
                Quaternion.identity,
                materials.structure,
                true);
            var lid = CreatePrimitive(
                context,
                PrimitiveType.Cube,
                spec.displayName + " fitted lid",
                root.transform,
                new Vector3(0f, 0.86f, 0f),
                new Vector3(1.34f, 0.14f, 1f),
                Quaternion.identity,
                context.palette.commonMetal,
                false);
            var glyph = CreatePrimitive(
                context,
                PrimitiveType.Sphere,
                spec.displayName + " survey glyph",
                root.transform,
                new Vector3(0f, 1.08f, 0f),
                new Vector3(0.28f, 0.18f, 0.28f),
                Quaternion.identity,
                materials.accent,
                false);

            var reward = root.AddComponent<ExplorationReward>();
            reward.Initialize(
                context.game,
                spec.id,
                spec.displayName,
                spec.prompt,
                spec.resourceKind,
                spec.resourceAmount,
                spec.experienceReward,
                spec.message,
                new[] { body.GetComponent<Renderer>(), lid.GetComponent<Renderer>(), glyph.GetComponent<Renderer>() },
                body.GetComponent<Collider>());
            return reward;
        }

        private static Transform CreateLandmarkRoot(
            Transform parent,
            string name,
            Vector3 position,
            float yaw,
            string id,
            string regionId,
            ExplorationLandmarkKind kind)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            root.gameObject.AddComponent<ExplorationLandmarkMarker>().Initialize(id, regionId, kind);
            return root;
        }

        private static GameObject CreatePrimitive(
            BuildContext context,
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material,
            bool solid)
        {
            var item = GameObject.CreatePrimitive(primitiveType);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localScale = localScale;
            item.transform.localRotation = localRotation;
            item.GetComponent<Renderer>().sharedMaterial = material;

            var itemCollider = item.GetComponent<Collider>();
            if (!solid && itemCollider != null)
            {
                itemCollider.enabled = false;
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(itemCollider);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(itemCollider);
                }
            }
            else if (solid && itemCollider != null)
            {
                context.solidColliderCount++;
            }

            context.primitiveRendererCount++;
            return item;
        }

        private static GameObject FindExistingRoot(Transform owner)
        {
            for (var index = 0; index < owner.childCount; index++)
            {
                var child = owner.GetChild(index);
                if (child.name == RootName)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static void RemoveInteriorLabels(Transform root)
        {
            foreach (var label in root.GetComponentsInChildren<TextMesh>(true))
            {
                if (label == null)
                {
                    continue;
                }

                label.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(label.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(label.gameObject);
                }
            }
        }

        private static void RegisterId(
            HashSet<string> ids,
            string id,
            List<string> failures)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                failures.Add("a landmark or reward has an empty ID");
                return;
            }

            if (!ids.Add(id))
            {
                failures.Add($"duplicate landmark ID '{id}'");
            }
        }

        private static void ValidatePosition(
            Vector3 position,
            string label,
            List<string> failures)
        {
            if (!IsFinite(position) ||
                Mathf.Abs(position.x) > 250f ||
                position.y < -2f ||
                position.y > 40f ||
                Mathf.Abs(position.z) > 132f)
            {
                failures.Add($"{label} has an invalid or out-of-world position {position}");
            }
        }

        private static void ValidateTransform(Transform item, List<string> failures)
        {
            ValidatePosition(item.position, item.name, failures);
            var scale = item.lossyScale;
            if (!IsFinite(scale) ||
                Mathf.Abs(scale.x) < 0.0001f ||
                Mathf.Abs(scale.y) < 0.0001f ||
                Mathf.Abs(scale.z) < 0.0001f ||
                Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z))) > 50f)
            {
                failures.Add($"{item.name} has invalid scale {scale}");
            }
        }

        private static bool IsInsideRegion(string regionId, float x)
        {
            switch (regionId)
            {
                case "Alderwake Basin":
                    return x >= -80f && x <= 80f;
                case "Morrowfen":
                    return x >= 80f && x <= 240f;
                case "Cinderwold Verge":
                    return x >= -240f && x <= -80f;
                default:
                    return false;
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private sealed class BuildContext
        {
            public readonly TetherfallGame game;
            public readonly LandmarkPalette palette;
            public int primitiveRendererCount;
            public int solidColliderCount;

            public BuildContext(TetherfallGame game, LandmarkPalette palette)
            {
                this.game = game;
                this.palette = palette;
            }
        }

        private sealed class RegionSpec
        {
            public string key;
            public string regionId;
            public string elevationId;
            public string elevationName;
            public Vector3 elevationOrigin;
            public float elevationYaw;
            public string caveId;
            public string caveName;
            public Vector3 caveEntrance;
            public float caveYaw;
            public string towerId;
            public string towerName;
            public Vector3 towerOrigin;
            public float towerYaw;
            public bool hasCombatCave;
            public RewardSpec secret;
            public RewardSpec caveReward;
            public RewardSpec towerReward;
        }

        private sealed class RewardSpec
        {
            public readonly string id;
            public readonly string displayName;
            public readonly string prompt;
            public readonly ResourceKind resourceKind;
            public readonly int resourceAmount;
            public readonly int experienceReward;
            public readonly string message;

            public RewardSpec(
                string id,
                string displayName,
                string prompt,
                ResourceKind resourceKind,
                int resourceAmount,
                int experienceReward,
                string message)
            {
                this.id = id;
                this.displayName = displayName;
                this.prompt = prompt;
                this.resourceKind = resourceKind;
                this.resourceAmount = resourceAmount;
                this.experienceReward = experienceReward;
                this.message = message;
            }
        }

        private sealed class LandmarkPalette
        {
            public readonly Material commonMetal;
            public readonly RegionMaterials[] regions;

            public LandmarkPalette(TetherfallGame game)
            {
                commonMetal = game.CreateMaterial(new Color(0.62f, 0.33f, 0.15f), 0.4f, 0.52f);
                regions = new[]
                {
                    new RegionMaterials(
                        game.CreateMaterial(new Color(0.25f, 0.38f, 0.22f), 0f, 0.22f),
                        game.CreateMaterial(new Color(0.38f, 0.4f, 0.31f), 0.04f, 0.3f),
                        game.CreateMaterial(new Color(0.2f, 0.12f, 0.075f), 0.02f, 0.24f),
                        game.CreateGlowMaterial(new Color(0.34f, 0.82f, 0.58f), 1.25f, 0.58f),
                        new Color(0.7f, 0.93f, 0.76f)),
                    new RegionMaterials(
                        game.CreateMaterial(new Color(0.12f, 0.24f, 0.25f), 0.02f, 0.28f),
                        game.CreateMaterial(new Color(0.25f, 0.38f, 0.4f), 0.08f, 0.38f),
                        game.CreateMaterial(new Color(0.24f, 0.34f, 0.28f), 0.04f, 0.25f),
                        game.CreateGlowMaterial(new Color(0.29f, 0.79f, 0.78f), 1.35f, 0.64f),
                        new Color(0.63f, 0.9f, 0.89f)),
                    new RegionMaterials(
                        game.CreateMaterial(new Color(0.15f, 0.075f, 0.085f), 0.06f, 0.25f),
                        game.CreateMaterial(new Color(0.29f, 0.16f, 0.15f), 0.1f, 0.33f),
                        game.CreateMaterial(new Color(0.42f, 0.19f, 0.11f), 0.28f, 0.46f),
                        game.CreateGlowMaterial(new Color(0.94f, 0.27f, 0.09f), 1.48f, 0.58f),
                        new Color(0.97f, 0.68f, 0.47f))
                };
            }
        }

        private sealed class RegionMaterials
        {
            public readonly Material terrain;
            public readonly Material stone;
            public readonly Material structure;
            public readonly Material accent;
            public readonly Color labelColor;

            public RegionMaterials(
                Material terrain,
                Material stone,
                Material structure,
                Material accent,
                Color labelColor)
            {
                this.terrain = terrain;
                this.stone = stone;
                this.structure = structure;
                this.accent = accent;
                this.labelColor = labelColor;
            }
        }
    }

    public sealed class ExplorationLandmarkBuildRoot : MonoBehaviour
    {
        public int AuthoredPrimitiveRenderers { get; private set; }
        public int AuthoredSolidColliders { get; private set; }

        public void Initialize(int primitiveRenderers, int solidColliders)
        {
            AuthoredPrimitiveRenderers = primitiveRenderers;
            AuthoredSolidColliders = solidColliders;
        }
    }

    public sealed class ExplorationLandmarkMarker : MonoBehaviour
    {
        public string LandmarkId { get; private set; }
        public string RegionId { get; private set; }
        public ExplorationLandmarkKind Kind { get; private set; }

        public void Initialize(
            string landmarkId,
            string regionId,
            ExplorationLandmarkKind kind)
        {
            LandmarkId = landmarkId;
            RegionId = regionId;
            Kind = kind;
        }
    }

    /// <summary>
    /// A bounded runtime-only reward. It has no Update loop and becomes inert after one use.
    /// Rewards intentionally avoid Favor so restarting a session cannot be used as a gold loop.
    /// </summary>
    public sealed class ExplorationReward : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private ResourceKind resourceKind;
        private int resourceAmount;
        private int experienceReward;
        private string prompt;
        private string message;
        private Renderer[] claimRenderers = Array.Empty<Renderer>();
        private Collider interactionCollider;

        public string RewardId { get; private set; }
        public string DisplayName { get; private set; }
        public bool IsClaimed { get; private set; }
        public string InteractionPrompt => IsClaimed ? string.Empty : prompt;

        public void Initialize(
            TetherfallGame owner,
            string rewardId,
            string displayName,
            string interactionPrompt,
            ResourceKind kind,
            int amount,
            int experience,
            string discoveryMessage,
            Renderer[] renderers,
            Collider rewardCollider)
        {
            game = owner;
            RewardId = rewardId;
            DisplayName = displayName;
            prompt = interactionPrompt;
            resourceKind = kind;
            resourceAmount = Mathf.Max(0, amount);
            experienceReward = Mathf.Max(0, experience);
            message = discoveryMessage;
            claimRenderers = renderers ?? Array.Empty<Renderer>();
            interactionCollider = rewardCollider;
            game.RegisterInteractable(this);
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return !IsClaimed && owner == game;
        }

        public void Interact(TetherfallGame owner)
        {
            if (!CanInteract(owner))
            {
                return;
            }

            IsClaimed = true;
            foreach (var itemRenderer in claimRenderers)
            {
                if (itemRenderer != null)
                {
                    itemRenderer.enabled = false;
                }
            }

            if (interactionCollider != null)
            {
                interactionCollider.enabled = false;
            }

            if (resourceAmount > 0)
            {
                owner.AddResource(resourceKind, resourceAmount);
            }

            if (experienceReward > 0)
            {
                owner.GainExperience(experienceReward);
            }

            owner.Notify(DisplayName + ": " + message, 6f);
        }
    }
}
