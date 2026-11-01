using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tetherfall.Editor
{
    public static class TetherfallQuaterniusVerification
    {
        private const string MenuPath = "Tetherfall/Verify Quaternius Runtime Dressing";
        private const int MaxCuratedPrefabSharedMaterials = 96;
        private const float MinimumThreadwayClearance = 8f;
        private const float MinimumCombatCenterClearance = 10f;
        private static int assertionCount;

        [MenuItem(MenuPath, false, 2186)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Quaternius runtime-dressing verification is edit-mode only.");
            }

            assertionCount = 0;
            var failures = new List<string>();
            var firstBuild = TetherfallQuaterniusPrefabBuilder.BuildPrefabs();
            var firstGuids = CapturePrefabGuids(failures);
            var secondBuild = TetherfallQuaterniusPrefabBuilder.BuildPrefabs();
            var secondGuids = CapturePrefabGuids(failures);

            Check(
                firstBuild.PrefabCount == TetherfallQuaterniusAssetCatalog.All.Count &&
                secondBuild.PrefabCount == firstBuild.PrefabCount,
                "Repeated prefab builds did not produce the complete curated asset count.",
                failures);
            Check(
                firstBuild.RendererCount == secondBuild.RendererCount,
                $"Repeated prefab builds changed renderer count from {firstBuild.RendererCount} " +
                $"to {secondBuild.RendererCount}.",
                failures);
            CheckGuidSnapshots(firstGuids, secondGuids, failures);

            var prefabMaterials = VerifyResourcePrefabs(failures);
            var placementSummary = VerifyPlacementPlan(failures);
            var runtimeSummary = VerifyRuntimeBuild(failures);
            VerifyDocumentation(failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Quaternius runtime-dressing verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            Debug.Log(
                $"Quaternius runtime-dressing verification passed: {assertionCount} assertions; " +
                $"{TetherfallQuaterniusAssetCatalog.All.Count} Resources prefabs, " +
                $"{prefabMaterials} curated source materials, " +
                $"{placementSummary.Total} deterministic placements " +
                $"({placementSummary.CityFacades} facades, {placementSummary.CityServices} services, " +
                $"{placementSummary.Camps} camp props, {placementSummary.Regions} regional props), " +
                $"{runtimeSummary.Renderers} runtime renderers, " +
                $"{runtimeSummary.Materials} shared runtime materials, zero colliders/lights, " +
                $"and duplicate builds remained idempotent.");
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        public static void VerifyForFullGame()
        {
            assertionCount = 0;
            var failures = new List<string>();
            VerifyResourcePrefabs(failures);
            VerifyPlacementPlan(failures);
            VerifyRuntimeBuild(failures);
            VerifyDocumentation(failures);
            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Quaternius runtime-dressing full-game checks failed:\n- " +
                    string.Join("\n- ", failures));
            }
        }

        private static Dictionary<string, string> CapturePrefabGuids(List<string> failures)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var definition in TetherfallQuaterniusAssetCatalog.All)
            {
                var guid = AssetDatabase.AssetPathToGUID(definition.OutputAssetPath);
                Check(
                    !string.IsNullOrEmpty(guid),
                    $"Generated prefab '{definition.OutputAssetPath}' has no stable asset GUID.",
                    failures);
                result[definition.OutputAssetPath] = guid;
            }

            return result;
        }

        private static void CheckGuidSnapshots(
            Dictionary<string, string> first,
            Dictionary<string, string> second,
            List<string> failures)
        {
            Check(
                first.Count == second.Count,
                "Repeated prefab builds changed the number of generated prefab GUIDs.",
                failures);
            foreach (var pair in first)
            {
                Check(
                    second.TryGetValue(pair.Key, out var secondGuid) &&
                    string.Equals(pair.Value, secondGuid, StringComparison.Ordinal),
                    $"Repeated prefab build replaced the GUID for '{pair.Key}'.",
                    failures);
            }
        }

        private static int VerifyResourcePrefabs(List<string> failures)
        {
            var outputGuids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { TetherfallQuaterniusAssetCatalog.OutputFolder });
            Check(
                outputGuids.Length == TetherfallQuaterniusAssetCatalog.All.Count,
                $"Expected exactly {TetherfallQuaterniusAssetCatalog.All.Count} curated prefab assets " +
                $"beneath {TetherfallQuaterniusAssetCatalog.OutputFolder}, found {outputGuids.Length}.",
                failures);

            var names = new HashSet<string>(StringComparer.Ordinal);
            var sourcePaths = new HashSet<string>(StringComparer.Ordinal);
            var materials = new HashSet<Material>();
            var preservedSourceMaterialCount = 0;
            var preservedTextureCount = 0;

            foreach (var definition in TetherfallQuaterniusAssetCatalog.All)
            {
                Check(
                    names.Add(definition.ResourceName),
                    $"Curated resource name '{definition.ResourceName}' is duplicated.",
                    failures);
                Check(
                    sourcePaths.Add(definition.SourceAssetPath),
                    $"Curated source path '{definition.SourceAssetPath}' is duplicated.",
                    failures);

                var source = AssetDatabase.LoadAssetAtPath<GameObject>(definition.SourceAssetPath);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(definition.OutputAssetPath);
                var resource = Resources.Load<GameObject>(definition.ResourcePath);
                Check(source != null, $"Source FBX is missing: '{definition.SourceAssetPath}'.", failures);
                Check(prefab != null, $"Generated prefab is missing: '{definition.OutputAssetPath}'.", failures);
                Check(
                    resource == prefab,
                    $"Resources.Load<GameObject>(\"{definition.ResourcePath}\") did not resolve " +
                    "the generated prefab.",
                    failures);
                if (prefab == null)
                {
                    continue;
                }

                var dependencies = new HashSet<string>(
                    AssetDatabase.GetDependencies(definition.OutputAssetPath, true),
                    StringComparer.Ordinal);
                Check(
                    dependencies.Contains(definition.SourceAssetPath),
                    $"Prefab '{definition.OutputAssetPath}' no longer references its source FBX.",
                    failures);
                VerifyForbiddenComponents(prefab, "prefab " + definition.ResourceName, failures);

                var instance = Object.Instantiate(prefab);
                instance.name = "Quaternius verification " + definition.ResourceName;
                try
                {
                    VerifyFiniteHierarchy(instance.transform, definition.ResourceName, failures);
                    var renderers = instance.GetComponentsInChildren<Renderer>(true);
                    Check(
                        renderers.Length > 0 && renderers.Length <= 4,
                        $"Prefab '{definition.ResourceName}' has {renderers.Length} renderers; " +
                        "expected a compact one-to-four renderer visual.",
                        failures);

                    foreach (var renderer in renderers)
                    {
                        Check(
                            IsFinite(renderer.bounds.center) &&
                            IsFinite(renderer.bounds.size) &&
                            renderer.bounds.size.sqrMagnitude > 0.000001f &&
                            MaxAbs(renderer.bounds.size) <= 50f,
                            $"Prefab '{definition.ResourceName}' has invalid or unbounded renderer bounds " +
                            $"{renderer.bounds}.",
                            failures);
                        var sharedMaterials = renderer.sharedMaterials;
                        Check(
                            sharedMaterials.Length > 0,
                            $"Prefab '{definition.ResourceName}' renderer has no source material slots.",
                            failures);
                        foreach (var material in sharedMaterials)
                        {
                            Check(
                                material != null,
                                $"Prefab '{definition.ResourceName}' has a missing shared material.",
                                failures);
                            if (material == null)
                            {
                                continue;
                            }

                            materials.Add(material);
                            var materialPath = AssetDatabase.GetAssetPath(material);
                            if (materialPath.StartsWith(
                                    "Assets/ThirdParty/Quaternius/",
                                    StringComparison.Ordinal))
                            {
                                preservedSourceMaterialCount++;
                            }

                            if (material.mainTexture != null)
                            {
                                var texturePath = AssetDatabase.GetAssetPath(material.mainTexture);
                                if (texturePath.StartsWith(
                                        "Assets/ThirdParty/Quaternius/",
                                        StringComparison.Ordinal))
                                {
                                    preservedTextureCount++;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }

            Check(
                materials.Count > 0 && materials.Count <= MaxCuratedPrefabSharedMaterials,
                $"Curated prefab set uses {materials.Count} shared materials; budget is " +
                $"{MaxCuratedPrefabSharedMaterials}.",
                failures);
            Check(
                preservedSourceMaterialCount > 0,
                "Generated prefabs do not preserve any material references from the Quaternius FBXs.",
                failures);
            Check(
                preservedTextureCount > 0,
                "Generated prefabs do not preserve any Quaternius texture references.",
                failures);
            return materials.Count;
        }

        private static PlacementSummary VerifyPlacementPlan(List<string> failures)
        {
            var placements = TetherfallQuaterniusDressing.GetPlannedPlacements();
            var summary = new PlacementSummary();
            var assetNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in TetherfallQuaterniusAssetCatalog.All)
            {
                assetNames.Add(definition.ResourceName);
            }

            var campCenters = TetherfallQuaterniusDressing.GetPlannedCampCenters();
            var logicalPlacements = new HashSet<string>(StringComparer.Ordinal);
            foreach (var placement in placements)
            {
                summary.Total++;
                switch (placement.Zone)
                {
                    case TetherfallQuaterniusDressingZone.CityFacade:
                        summary.CityFacades++;
                        break;
                    case TetherfallQuaterniusDressingZone.CityService:
                        summary.CityServices++;
                        break;
                    case TetherfallQuaterniusDressingZone.EnemyCamp:
                        summary.Camps++;
                        break;
                    case TetherfallQuaterniusDressingZone.OriginalRegion:
                        summary.Regions++;
                        break;
                }

                Check(
                    !string.IsNullOrWhiteSpace(placement.ClusterName),
                    "A planned Quaternius placement has no cluster name.",
                    failures);
                Check(
                    assetNames.Contains(placement.ResourceName),
                    $"Placement '{placement.ClusterName}' references uncurated resource " +
                    $"'{placement.ResourceName}'.",
                    failures);
                Check(
                    IsFinite(placement.WorldPosition) && MaxAbs(placement.WorldPosition) <= 1000f,
                    $"Placement '{placement.ClusterName}/{placement.ResourceName}' has invalid position " +
                    $"{placement.WorldPosition}.",
                    failures);
                Check(
                    logicalPlacements.Add(
                        placement.ClusterName + "|" + placement.ResourceName + "|" +
                        placement.WorldPosition.ToString("F3")),
                    $"Placement '{placement.ClusterName}/{placement.ResourceName}' is duplicated at " +
                    $"{placement.WorldPosition}.",
                    failures);

                VerifyRouteClearance(placement, failures);
                foreach (var campCenter in campCenters)
                {
                    CheckHorizontalClearance(
                        placement,
                        campCenter,
                        TetherfallQuaterniusDressing.ClearCombatRadius,
                        "enemy-camp combat center",
                        failures);
                }
            }

            Check(
                summary.Total == TetherfallQuaterniusDressing.ExpectedPlacementCount &&
                summary.Total <= TetherfallQuaterniusDressing.MaxPlacementCount,
                $"Placement plan contains {summary.Total} entries; expected " +
                $"{TetherfallQuaterniusDressing.ExpectedPlacementCount} with a hard maximum of " +
                $"{TetherfallQuaterniusDressing.MaxPlacementCount}.",
                failures);
            Check(summary.CityFacades == 8, $"Expected 8 city facade overlays, found {summary.CityFacades}.", failures);
            Check(summary.CityServices == 16, $"Expected 16 city service props, found {summary.CityServices}.", failures);
            Check(
                summary.Camps == TetherfallQuaterniusDressing.ExpectedCampPlacementCount,
                $"Expected {TetherfallQuaterniusDressing.ExpectedCampPlacementCount} camp props, " +
                $"found {summary.Camps}.",
                failures);
            Check(summary.Regions == 24, $"Expected 24 original-region props, found {summary.Regions}.", failures);
            return summary;
        }

        private static void VerifyRouteClearance(
            TetherfallQuaterniusPlannedPlacement placement,
            List<string> failures)
        {
            foreach (var threadway in ThreadwayCatalog.All)
            {
                if (threadway == null)
                {
                    continue;
                }

                CheckHorizontalClearance(
                    placement,
                    threadway.position,
                    MinimumThreadwayClearance,
                    "Threadway anchor '" + threadway.id + "'",
                    failures);
                CheckHorizontalClearance(
                    placement,
                    threadway.arrivalPosition,
                    MinimumThreadwayClearance,
                    "Threadway arrival '" + threadway.id + "'",
                    failures);
            }

            foreach (var routeEvent in RouteEventCatalog.All)
            {
                if (routeEvent != null)
                {
                    CheckHorizontalClearance(
                        placement,
                        routeEvent.position,
                        MinimumCombatCenterClearance,
                        "route-event combat center '" + routeEvent.id + "'",
                        failures);
                }
            }

            foreach (var bounty in BountyCatalog.All)
            {
                if (bounty != null)
                {
                    CheckHorizontalClearance(
                        placement,
                        bounty.position,
                        MinimumCombatCenterClearance,
                        "bounty combat center '" + bounty.id + "'",
                        failures);
                }
            }

            VerifyCityActivityClearance(placement, CityCatalog.AlderwakeHearthmarket, failures);
            VerifyCityActivityClearance(placement, CityCatalog.VeyrwaterMooring, failures);
            VerifyCityRouteLane(placement, CityCatalog.AlderwakeHearthmarket, failures);
            VerifyCityRouteLane(placement, CityCatalog.VeyrwaterMooring, failures);
        }

        private static void VerifyCityActivityClearance(
            TetherfallQuaterniusPlannedPlacement placement,
            CityDefinition city,
            List<string> failures)
        {
            if (city?.activities == null)
            {
                return;
            }

            foreach (var activity in city.activities)
            {
                if (activity != null && activity.kind == CityActivityKind.Training)
                {
                    CheckHorizontalClearance(
                        placement,
                        activity.arenaPosition,
                        MinimumCombatCenterClearance,
                        "city training center '" + activity.id + "'",
                        failures);
                }
            }
        }

        private static void VerifyCityRouteLane(
            TetherfallQuaterniusPlannedPlacement placement,
            CityDefinition city,
            List<string> failures)
        {
            if (city == null ||
                (placement.Zone != TetherfallQuaterniusDressingZone.CityFacade &&
                 placement.Zone != TetherfallQuaterniusDressingZone.CityService) ||
                HorizontalDistance(placement.WorldPosition, city.center) > 55f)
            {
                return;
            }

            var local = placement.WorldPosition - city.center;
            var clearsNorthSouthLane = Mathf.Abs(local.x) >= 6f || Mathf.Abs(local.z) > 34f;
            var clearsEastWestLane = Mathf.Abs(local.z) >= 6f || Mathf.Abs(local.x) > 34f;
            Check(
                clearsNorthSouthLane && clearsEastWestLane,
                $"City placement '{placement.ClusterName}/{placement.ResourceName}' enters a protected " +
                $"route lane at {placement.WorldPosition}.",
                failures);
        }

        private static void CheckHorizontalClearance(
            TetherfallQuaterniusPlannedPlacement placement,
            Vector3 protectedPosition,
            float clearance,
            string protectedName,
            List<string> failures)
        {
            Check(
                HorizontalDistance(placement.WorldPosition, protectedPosition) >= clearance,
                $"Placement '{placement.ClusterName}/{placement.ResourceName}' is within {clearance:0.#}m " +
                $"of {protectedName}.",
                failures);
        }

        private static RuntimeSummary VerifyRuntimeBuild(List<string> failures)
        {
            var fixture = new GameObject("Quaternius Dressing Verification Game");
            var game = fixture.AddComponent<TetherfallGame>();
            try
            {
                var first = TetherfallQuaterniusDressing.Build(game);
                Check(first != null, "Runtime dressing Build returned null.", failures);
                if (first == null)
                {
                    return default;
                }

                var firstRendererCount = first.GetComponentsInChildren<Renderer>(true).Length;
                var firstTransformCount = first.GetComponentsInChildren<Transform>(true).Length;
                var second = TetherfallQuaterniusDressing.Build(game);
                Check(
                    second == first,
                    "A duplicate runtime Build did not return the existing dressing root.",
                    failures);
                Check(
                    first.GetComponentsInChildren<Renderer>(true).Length == firstRendererCount &&
                    first.GetComponentsInChildren<Transform>(true).Length == firstTransformCount,
                    "A duplicate runtime Build changed renderer or hierarchy counts.",
                    failures);

                var matchingRoots = 0;
                for (var index = 0; index < game.transform.childCount; index++)
                {
                    if (game.transform.GetChild(index).name == TetherfallQuaterniusDressing.RootName)
                    {
                        matchingRoots++;
                    }
                }

                Check(matchingRoots == 1, $"Runtime Build created {matchingRoots} dressing roots.", failures);
                VerifyForbiddenComponents(first, "runtime dressing root", failures);
                VerifyFiniteHierarchy(first.transform, "runtime dressing", failures);

                var renderers = first.GetComponentsInChildren<Renderer>(true);
                var materials = new HashSet<Material>();
                foreach (var renderer in renderers)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            materials.Add(material);
                        }
                    }
                }

                Check(
                    TetherfallQuaterniusDressing.BuiltPlacementCount ==
                    TetherfallQuaterniusDressing.ExpectedPlacementCount,
                    $"Runtime built {TetherfallQuaterniusDressing.BuiltPlacementCount} placements; " +
                    $"expected {TetherfallQuaterniusDressing.ExpectedPlacementCount}.",
                    failures);
                Check(
                    TetherfallQuaterniusDressing.BuiltClusterCount ==
                    TetherfallQuaterniusDressing.ExpectedClusterCount,
                    $"Runtime built {TetherfallQuaterniusDressing.BuiltClusterCount} clusters; " +
                    $"expected {TetherfallQuaterniusDressing.ExpectedClusterCount}.",
                    failures);
                Check(
                    renderers.Length == TetherfallQuaterniusDressing.BuiltRendererCount &&
                    renderers.Length <= TetherfallQuaterniusDressing.MaxRendererCount,
                    $"Runtime renderer count is {renderers.Length}; tracked count is " +
                    $"{TetherfallQuaterniusDressing.BuiltRendererCount} and budget is " +
                    $"{TetherfallQuaterniusDressing.MaxRendererCount}.",
                    failures);
                Check(
                    materials.Count == TetherfallQuaterniusDressing.BuiltSharedMaterialCount &&
                    materials.Count <= TetherfallQuaterniusDressing.MaxSharedMaterialCount,
                    $"Runtime shared-material count is {materials.Count}; tracked count is " +
                    $"{TetherfallQuaterniusDressing.BuiltSharedMaterialCount} and budget is " +
                    $"{TetherfallQuaterniusDressing.MaxSharedMaterialCount}.",
                    failures);
                Check(
                    TetherfallQuaterniusDressing.MissingPrefabCount == 0,
                    $"Runtime build reported {TetherfallQuaterniusDressing.MissingPrefabCount} " +
                    "missing Resources prefabs.",
                    failures);
                VerifyNoUpdateLoops(first, failures);
                return new RuntimeSummary(renderers.Length, materials.Count);
            }
            finally
            {
                Object.DestroyImmediate(fixture);
                InvokeStaticReset(typeof(TetherfallQuaterniusDressing), "ResetStatics");
                InvokeStaticReset(typeof(TetherfallGame), "ResetStatics");
            }
        }

        private static void VerifyNoUpdateLoops(GameObject root, List<string> failures)
        {
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                {
                    continue;
                }

                var type = behaviour.GetType();
                foreach (var methodName in new[] { "Update", "LateUpdate", "FixedUpdate" })
                {
                    var method = type.GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly);
                    Check(
                        method == null,
                        $"Runtime dressing component '{type.FullName}' declares a per-frame " +
                        $"{methodName} loop.",
                        failures);
                }
            }
        }

        private static void VerifyDocumentation(List<string> failures)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var quaterniusReadme = Path.Combine(
                Application.dataPath,
                "ThirdParty",
                "Quaternius",
                "README.md");
            var provenance = Path.Combine(Application.dataPath, "Art", "ART_PROVENANCE.md");
            var direction = Path.Combine(Application.dataPath, "Art", "ART_DIRECTION.md");
            var projectReadme = Path.Combine(projectRoot, "README.md");
            var licenses = new[]
            {
                Path.Combine(Application.dataPath, "ThirdParty", "Quaternius",
                    "MedievalVillageMegaKit_Standard", "License_Standard.txt"),
                Path.Combine(Application.dataPath, "ThirdParty", "Quaternius",
                    "FantasyPropsMegaKit_Standard", "License_Standard.txt"),
                Path.Combine(Application.dataPath, "ThirdParty", "Quaternius",
                    "StylizedNatureMegaKit_Standard", "License_Standard.txt")
            };

            foreach (var path in licenses)
            {
                Check(File.Exists(path), $"Preserved Quaternius license is missing: '{path}'.", failures);
                if (File.Exists(path))
                {
                    var text = File.ReadAllText(path);
                    Check(
                        text.IndexOf("CC0", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        text.IndexOf("public domain", StringComparison.OrdinalIgnoreCase) >= 0,
                        $"Preserved license '{path}' does not identify CC0/public-domain terms.",
                        failures);
                }
            }

            VerifyDocument(
                quaterniusReadme,
                new[] { "46 FBX", "35 referenced PNG", "CC0 1.0", "Quaternius" },
                failures);
            VerifyDocument(
                provenance,
                new[]
                {
                    "Quaternius environment MegaKits",
                    "46 raw FBX",
                    "31 Resources-loadable",
                    "TetherfallQuaterniusPrefabBuilder",
                    "TetherfallQuaterniusDressing"
                },
                failures);
            VerifyDocument(
                direction,
                new[]
                {
                    "Quaternius",
                    "66 deterministic",
                    "145",
                    "zero added lights",
                    "collider-free"
                },
                failures);
            VerifyDocument(
                projectReadme,
                new[]
                {
                    "Quaternius",
                    "Build Quaternius Runtime Prefabs",
                    "Verify Quaternius Runtime Dressing",
                    "66"
                },
                failures);
        }

        private static void VerifyDocument(
            string path,
            string[] requiredText,
            List<string> failures)
        {
            Check(File.Exists(path), $"Required documentation is missing: '{path}'.", failures);
            if (!File.Exists(path))
            {
                return;
            }

            var text = File.ReadAllText(path);
            foreach (var required in requiredText)
            {
                Check(
                    text.IndexOf(required, StringComparison.OrdinalIgnoreCase) >= 0,
                    $"Documentation '{path}' must mention '{required}'.",
                    failures);
            }
        }

        private static void VerifyForbiddenComponents(
            GameObject root,
            string label,
            List<string> failures)
        {
            Check(root.GetComponentsInChildren<Collider>(true).Length == 0,
                $"{label} contains a Collider.", failures);
            Check(root.GetComponentsInChildren<Collider2D>(true).Length == 0,
                $"{label} contains a Collider2D.", failures);
            Check(root.GetComponentsInChildren<Light>(true).Length == 0,
                $"{label} contains a Light.", failures);
            Check(root.GetComponentsInChildren<Camera>(true).Length == 0,
                $"{label} contains a Camera.", failures);
            Check(root.GetComponentsInChildren<Animator>(true).Length == 0,
                $"{label} contains an Animator.", failures);
            Check(root.GetComponentsInChildren<Animation>(true).Length == 0,
                $"{label} contains a legacy Animation component.", failures);
        }

        private static void VerifyFiniteHierarchy(
            Transform root,
            string label,
            List<string> failures)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                Check(
                    IsFinite(transform.localPosition) &&
                    IsFinite(transform.localScale) &&
                    IsFinite(transform.localRotation) &&
                    MaxAbs(transform.localPosition) <= 1000f &&
                    MaxAbs(transform.localScale) <= 50f &&
                    Mathf.Min(
                        Mathf.Abs(transform.localScale.x),
                        Mathf.Abs(transform.localScale.y),
                        Mathf.Abs(transform.localScale.z)) > 0.0001f,
                    $"{label} has an invalid transform at '{GetHierarchyPath(transform)}'.",
                    failures);
            }
        }

        private static void InvokeStaticReset(Type type, string methodName)
        {
            var method = type.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null && method.GetParameters().Length == 0)
            {
                method.Invoke(null, null);
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right)
        {
            var delta = left - right;
            delta.y = 0f;
            return delta.magnitude;
        }

        private static float MaxAbs(Vector3 value)
        {
            return Mathf.Max(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void Check(bool condition, string failure, List<string> failures)
        {
            assertionCount++;
            if (!condition && !failures.Contains(failure))
            {
                failures.Add(failure);
            }
        }

        private sealed class PlacementSummary
        {
            public int Total;
            public int CityFacades;
            public int CityServices;
            public int Camps;
            public int Regions;
        }

        private readonly struct RuntimeSummary
        {
            public int Renderers { get; }
            public int Materials { get; }

            public RuntimeSummary(int renderers, int materials)
            {
                Renderers = renderers;
                Materials = materials;
            }
        }
    }
}
