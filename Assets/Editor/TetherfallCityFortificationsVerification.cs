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
    public static class TetherfallCityFortificationsVerification
    {
        private const string MenuPath = "Tetherfall/Verify City Fortifications";
        private const float ThreadwayClearance = 8f;
        private const float CombatClearance = 10f;
        private const float InstitutionClearance = 5f;
        private const float MarketClearance = 4.5f;
        private const float ExistingTraversalClearance = 3.25f;
        private const float ProtectedRoadHalfWidth = 5.4f;
        private const float MaximumFiniteExtent = 260f;
        private static int assertionCount;

        [MenuItem(MenuPath, false, 2191)]
        public static void Verify()
        {
            VerifyInternal(true);
        }

        [MenuItem(MenuPath, true, 2191)]
        private static bool CanVerify()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        public static void VerifyForFullGame()
        {
            VerifyInternal(false);
        }

        private static void VerifyInternal(bool logSuccess)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "City fortification verification is edit-mode only.");
            }

            assertionCount = 0;
            var failures = new List<string>();
            VerifyPublicContract(failures);
            VerifyDocumentation(failures);

            var fixture = new GameObject("City Fortification Verification");
            var parent = new GameObject("Fortification Build Parent").transform;
            parent.SetParent(fixture.transform, false);
            var game = fixture.AddComponent<TetherfallGame>();

            try
            {
                VerifyNormalBuild(game, parent, failures);
                VerifyForcedFallbackBuild(game, parent, failures);
            }
            finally
            {
                TetherfallCityFortifications.Cleanup(parent);
                Object.DestroyImmediate(fixture);
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "City fortification verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            if (logSuccess)
            {
                Debug.Log(
                    $"City fortification verification passed: {assertionCount} assertions; " +
                    "normal and forced missing-Resources builds were deterministic and idempotent, " +
                    "both city silhouettes retained clear protected routes, four return-safe wall " +
                    "ascents reached tower overlooks, collider separation held, and the shared " +
                    "renderer/collider/material/light/culling budgets remained within contract.");
            }
        }

        private static void VerifyPublicContract(List<string> failures)
        {
            var type = typeof(TetherfallCityFortifications);
            Check(
                type.GetMethod(
                    "Build",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(TetherfallGame), typeof(Transform) },
                    null) != null,
                "Build(TetherfallGame, Transform) is missing.",
                failures);
            Check(
                type.GetMethod(
                    "Rebuild",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(TetherfallGame), typeof(Transform) },
                    null) != null,
                "Rebuild(TetherfallGame, Transform) is missing.",
                failures);
            Check(
                type.GetMethod(
                    "Cleanup",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(Transform) },
                    null) != null,
                "Cleanup(Transform) is missing.",
                failures);
            Check(
                type.GetMethod(
                    "BuildForcedMissingResources",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(TetherfallGame), typeof(Transform) },
                    null) != null,
                "Forced missing-Resources build API is missing.",
                failures);
            Check(
                type.GetMethod(
                    "RebuildForcedMissingResources",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(TetherfallGame), typeof(Transform) },
                    null) != null,
                "Forced missing-Resources rebuild API is missing.",
                failures);
            Check(
                TetherfallCityFortifications.MaxRendererCount <= 220 &&
                TetherfallCityFortifications.MaxDeliberateColliderCount <= 70 &&
                TetherfallCityFortifications.MaxProceduralMaterialCount <= 16 &&
                TetherfallCityFortifications.AddedRealtimeLightCount == 0 &&
                TetherfallCityFortifications.CullingDriverCount == 1 &&
                TetherfallCityFortifications.PerPieceUpdateLoopCount == 0,
                "Published performance ceilings exceed the city-fortification brief.",
                failures);
        }

        private static void VerifyNormalBuild(
            TetherfallGame game,
            Transform parent,
            List<string> failures)
        {
            GameObject root;
            try
            {
                root = TetherfallCityFortifications.Build(game, parent);
            }
            catch (Exception exception)
            {
                Check(
                    false,
                    $"Normal fortification build threw {exception.GetType().Name}: " +
                    exception.Message,
                    failures);
                return;
            }

            Check(root != null, "Normal Build returned null.", failures);
            if (root == null)
            {
                return;
            }

            VerifyBuiltRoot(root, false, failures);
            var firstSnapshot = CaptureSnapshot(root);
            var duplicate = TetherfallCityFortifications.Build(game, parent);
            Check(
                ReferenceEquals(root, duplicate),
                "Repeated normal Build replaced the root.",
                failures);
            Check(
                firstSnapshot.Equals(CaptureSnapshot(duplicate)),
                "Repeated normal Build changed hierarchy or budget counts.",
                failures);
            Check(
                CountDirectRoots(parent) == 1,
                "Repeated normal Build created more than one direct fortification root.",
                failures);

            var rebuilt = TetherfallCityFortifications.Rebuild(game, parent);
            Check(
                ReferenceEquals(root, rebuilt),
                "Explicit normal Rebuild replaced the stable root.",
                failures);
            Check(
                firstSnapshot.Equals(CaptureSnapshot(rebuilt)),
                "Explicit normal Rebuild changed deterministic counts.",
                failures);
            VerifyBuiltRoot(rebuilt, false, failures);

            Check(
                TetherfallCityFortifications.Cleanup(parent),
                "Normal cleanup did not remove the fortification root.",
                failures);
            Check(
                !TetherfallCityFortifications.Cleanup(parent) &&
                !TetherfallCityFortifications.IsBuilt(parent) &&
                CountDirectRoots(parent) == 0,
                "Normal cleanup was not idempotent.",
                failures);
        }

        private static void VerifyForcedFallbackBuild(
            TetherfallGame game,
            Transform parent,
            List<string> failures)
        {
            GameObject root;
            try
            {
                root = TetherfallCityFortifications.BuildForcedMissingResources(game, parent);
            }
            catch (Exception exception)
            {
                Check(
                    false,
                    $"Forced missing-Resources build threw {exception.GetType().Name}: " +
                    exception.Message,
                    failures);
                return;
            }

            Check(root != null, "Forced fallback Build returned null.", failures);
            if (root == null)
            {
                return;
            }

            VerifyBuiltRoot(root, true, failures);
            var firstSnapshot = CaptureSnapshot(root);
            var duplicate =
                TetherfallCityFortifications.BuildForcedMissingResources(game, parent);
            Check(
                ReferenceEquals(root, duplicate),
                "Repeated forced fallback Build replaced the root.",
                failures);
            Check(
                firstSnapshot.Equals(CaptureSnapshot(duplicate)),
                "Repeated forced fallback Build changed hierarchy or budget counts.",
                failures);

            var rebuilt =
                TetherfallCityFortifications.RebuildForcedMissingResources(game, parent);
            Check(
                ReferenceEquals(root, rebuilt),
                "Forced fallback Rebuild replaced the stable root.",
                failures);
            Check(
                firstSnapshot.Equals(CaptureSnapshot(rebuilt)),
                "Forced fallback Rebuild changed deterministic counts.",
                failures);
            VerifyBuiltRoot(rebuilt, true, failures);

            Check(
                TetherfallCityFortifications.Cleanup(parent),
                "Forced fallback cleanup did not remove the root.",
                failures);
            Check(
                !TetherfallCityFortifications.Cleanup(parent) &&
                CountDirectRoots(parent) == 0,
                "Forced fallback cleanup was not idempotent.",
                failures);
        }

        private static void VerifyBuiltRoot(
            GameObject root,
            bool requireForcedFallback,
            List<string> failures)
        {
            var marker = root.GetComponent<TetherfallCityFortificationRoot>();
            Check(marker != null, "Built root has no fortification metadata.", failures);
            if (marker == null)
            {
                return;
            }

            Check(
                root.name == TetherfallCityFortifications.RootName &&
                root.transform.parent != null &&
                root.transform.localPosition == Vector3.zero &&
                Quaternion.Angle(root.transform.localRotation, Quaternion.identity) < 0.001f &&
                Vector3.SqrMagnitude(root.transform.localScale - Vector3.one) < 0.000001f,
                "Fortification root is not a stable identity child of the supplied parent.",
                failures);
            Check(
                marker.ForcedMissingResources == requireForcedFallback,
                "Root fallback-mode metadata does not match the requested build.",
                failures);

            VerifyFiniteHierarchy(root.transform, failures);
            VerifyBudgetsAndCulling(root, marker, failures);
            VerifyColliderSeparation(root, marker, failures);
            VerifyNoForbiddenComponents(root, failures);
            VerifyNoFrameLoops(root, failures);
            VerifyCityProfiles(root, failures);
            VerifyTraversalLoops(root, failures);
            VerifyProtectedClearances(root, failures);
            VerifyMaterials(root, marker, failures);
            VerifySourceFallbackAccounting(root, marker, requireForcedFallback, failures);
        }

        private static void VerifyBudgetsAndCulling(
            GameObject root,
            TetherfallCityFortificationRoot marker,
            List<string> failures)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var colliders = root.GetComponentsInChildren<Collider>(true);
            var drivers =
                root.GetComponentsInChildren<TetherfallCityFortificationDistanceCulling>(true);
            var clusters =
                root.GetComponentsInChildren<TetherfallCityFortificationCullingCluster>(true);

            Check(
                renderers.Length >= 170 &&
                renderers.Length <= TetherfallCityFortifications.MaxRendererCount &&
                renderers.Length == marker.RendererCount &&
                renderers.Length == TetherfallCityFortifications.BuiltRendererCount,
                $"Renderer count is {renderers.Length} (metadata {marker.RendererCount}); " +
                $"expected a substantive build within " +
                $"{TetherfallCityFortifications.MaxRendererCount}.",
                failures);
            Check(
                colliders.Length >= 40 &&
                colliders.Length <= TetherfallCityFortifications.MaxDeliberateColliderCount &&
                colliders.Length == marker.DeliberateColliderCount &&
                colliders.Length == TetherfallCityFortifications.BuiltColliderCount,
                $"Collider count is {colliders.Length} (metadata " +
                $"{marker.DeliberateColliderCount}); budget is " +
                $"{TetherfallCityFortifications.MaxDeliberateColliderCount}.",
                failures);
            Check(
                marker.ProceduralMaterialCount > 0 &&
                marker.ProceduralMaterialCount <=
                TetherfallCityFortifications.MaxProceduralMaterialCount &&
                marker.ProceduralMaterialCount ==
                TetherfallCityFortifications.CachedProceduralMaterialCount,
                $"Procedural material count {marker.ProceduralMaterialCount} is invalid.",
                failures);
            Check(
                root.GetComponentsInChildren<Light>(true).Length == 0,
                "Fortifications added a realtime Light.",
                failures);
            Check(
                drivers.Length == TetherfallCityFortifications.CullingDriverCount &&
                clusters.Length == TetherfallCityFortifications.ExpectedCityCount &&
                marker.CullingClusterCount == clusters.Length,
                $"Expected one central culling driver and two city clusters; found " +
                $"{drivers.Length} and {clusters.Length}.",
                failures);
            Check(
                root.GetComponentsInChildren<LODGroup>(true).Length == 0,
                "A source or piece added a per-piece LODGroup instead of using central culling.",
                failures);

            if (drivers.Length == 1)
            {
                var driver = drivers[0];
                Check(
                    driver.ClusterCount == clusters.Length &&
                    driver.RegisteredRendererCount == renderers.Length &&
                    Mathf.Approximately(
                        marker.NearDistance,
                        TetherfallCityFortificationDistanceCulling.NearDistance) &&
                    Mathf.Approximately(
                        marker.CullDistance,
                        TetherfallCityFortificationDistanceCulling.CullDistance) &&
                    marker.NearDistance < marker.CullDistance,
                    "Central culling registration or distance metadata is inconsistent.",
                    failures);
            }

            var clusterRendererCount = 0;
            var clusterCities = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < clusters.Length; index++)
            {
                var cluster = clusters[index];
                clusterRendererCount += cluster.RendererCount;
                Check(
                    !string.IsNullOrWhiteSpace(cluster.CityId) &&
                    clusterCities.Add(cluster.CityId) &&
                    cluster.RendererCount > 0 &&
                    IsFinite(cluster.WorldBounds) &&
                    cluster.WorldBounds.size.sqrMagnitude > 0.001f,
                    $"Culling cluster {index} has duplicate city metadata or invalid bounds.",
                    failures);
            }

            Check(
                clusterRendererCount == renderers.Length,
                $"Culling clusters register {clusterRendererCount} renderers, but the root has " +
                $"{renderers.Length}.",
                failures);
        }

        private static void VerifyColliderSeparation(
            GameObject root,
            TetherfallCityFortificationRoot marker,
            List<string> failures)
        {
            var cities = root.GetComponentsInChildren<TetherfallCityFortificationCity>(true);
            var colliders = root.GetComponentsInChildren<Collider>(true);
            var markers =
                root.GetComponentsInChildren<TetherfallCityFortificationCollider>(true);
            Check(
                cities.Length == TetherfallCityFortifications.ExpectedCityCount &&
                marker.CityCount == cities.Length,
                $"Expected two city roots, found {cities.Length}.",
                failures);
            Check(
                colliders.Length == markers.Length,
                $"Found {colliders.Length} colliders but {markers.Length} collider metadata records.",
                failures);

            for (var cityIndex = 0; cityIndex < cities.Length; cityIndex++)
            {
                var city = cities[cityIndex];
                Check(
                    city.VisualRoot != null &&
                    city.GameplayRoot != null &&
                    city.VisualRoot.name == TetherfallCityFortifications.VisualRootName &&
                    city.GameplayRoot.name == TetherfallCityFortifications.GameplayRootName,
                    $"City '{city.CityId}' is missing separated visual/gameplay roots.",
                    failures);
                if (city.VisualRoot == null || city.GameplayRoot == null)
                {
                    continue;
                }

                Check(
                    city.VisualRoot.GetComponentsInChildren<Collider>(true).Length == 0,
                    $"City '{city.CityId}' visual root contains gameplay collision.",
                    failures);
                Check(
                    city.GameplayRoot.GetComponentsInChildren<Renderer>(true).Length == 0,
                    $"City '{city.CityId}' gameplay root contains presentation renderers.",
                    failures);

                var cityColliders = city.GameplayRoot.GetComponentsInChildren<Collider>(true);
                for (var index = 0; index < cityColliders.Length; index++)
                {
                    var collider = cityColliders[index];
                    var colliderMarker =
                        collider.GetComponent<TetherfallCityFortificationCollider>();
                    Check(
                        collider is BoxCollider &&
                        collider.enabled &&
                        !collider.isTrigger &&
                        colliderMarker != null &&
                        string.Equals(colliderMarker.CityId, city.CityId, StringComparison.Ordinal),
                        $"City '{city.CityId}' has a non-simple, disabled, trigger, or unmarked " +
                        $"collider at '{GetHierarchyPath(collider.transform)}'.",
                        failures);
                }
            }

            var climbables = root.GetComponentsInChildren<ClimbableSurface>(true);
            Check(
                climbables.Length == TetherfallCityFortifications.ExpectedAscentRouteCount,
                $"Expected four authored climb faces, found {climbables.Length}.",
                failures);
            for (var index = 0; index < markers.Length; index++)
            {
                var colliderMarker = markers[index];
                var climbable = colliderMarker.GetComponent<ClimbableSurface>();
                Check(
                    (colliderMarker.Role ==
                     TetherfallCityFortificationColliderRole.ClimbableFace) ==
                    (climbable != null),
                    $"Climbable tagging does not match collider role at " +
                    $"'{GetHierarchyPath(colliderMarker.transform)}'.",
                    failures);
                if (colliderMarker.Role ==
                    TetherfallCityFortificationColliderRole.ClimbableFace)
                {
                    Check(
                        !string.IsNullOrWhiteSpace(colliderMarker.RouteId),
                        "A climbable wall face is not assigned to an authored ascent route.",
                        failures);
                }
            }
        }

        private static void VerifyNoForbiddenComponents(
            GameObject root,
            List<string> failures)
        {
            Check(
                root.GetComponentsInChildren<Collider2D>(true).Length == 0,
                "Fortification hierarchy contains Collider2D.",
                failures);
            Check(
                root.GetComponentsInChildren<Camera>(true).Length == 0,
                "Fortification hierarchy contains a Camera.",
                failures);
            Check(
                root.GetComponentsInChildren<AudioListener>(true).Length == 0,
                "Fortification hierarchy contains an AudioListener.",
                failures);
            Check(
                root.GetComponentsInChildren<Animator>(true).Length == 0 &&
                root.GetComponentsInChildren<Animation>(true).Length == 0,
                "Fortification hierarchy contains an animation driver.",
                failures);
            Check(
                root.GetComponentsInChildren<Rigidbody>(true).Length == 0 &&
                root.GetComponentsInChildren<Rigidbody2D>(true).Length == 0,
                "Fortification hierarchy contains a physics body.",
                failures);
            Check(
                root.GetComponentsInChildren<ParticleSystem>(true).Length == 0,
                "Fortification hierarchy contains an unbudgeted particle system.",
                failures);
        }

        private static void VerifyNoFrameLoops(GameObject root, List<string> failures)
        {
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                var behaviour = behaviours[index];
                if (behaviour == null)
                {
                    continue;
                }

                var type = behaviour.GetType();
                foreach (var methodName in new[] { "Update", "LateUpdate", "FixedUpdate" })
                {
                    Check(
                        type.GetMethod(
                            methodName,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly) == null,
                        $"Component '{type.FullName}' declares per-piece {methodName}.",
                        failures);
                }
            }
        }

        private static void VerifyCityProfiles(GameObject root, List<string> failures)
        {
            var cities = root.GetComponentsInChildren<TetherfallCityFortificationCity>(true);
            TetherfallCityFortificationCity alderwake = null;
            TetherfallCityFortificationCity veyrwater = null;
            for (var index = 0; index < cities.Length; index++)
            {
                if (string.Equals(
                        cities[index].CityId,
                        CityCatalog.AlderwakeHearthmarket.id,
                        StringComparison.Ordinal))
                {
                    alderwake = cities[index];
                }
                else if (string.Equals(
                             cities[index].CityId,
                             CityCatalog.VeyrwaterMooring.id,
                             StringComparison.Ordinal))
                {
                    veyrwater = cities[index];
                }
            }

            Check(alderwake != null, "Alderwake fortification city root is missing.", failures);
            Check(veyrwater != null, "Veyrwater fortification city root is missing.", failures);
            if (alderwake == null || veyrwater == null)
            {
                return;
            }

            Check(
                HorizontalDistance(
                    alderwake.transform.position,
                    CityCatalog.AlderwakeHearthmarket.center) <= 0.01f &&
                HorizontalDistance(
                    veyrwater.transform.position,
                    CityCatalog.VeyrwaterMooring.center) <= 0.01f,
                "Fortification build changed or failed to resolve a CityCatalog center.",
                failures);
            Check(
                alderwake.Style ==
                TetherfallCityFortificationStyle.AlderwakeTerracedCitadel &&
                veyrwater.Style ==
                TetherfallCityFortificationStyle.VeyrwaterFloodwallMooring &&
                alderwake.Style != veyrwater.Style,
                "The two cities do not expose distinct fortification style metadata.",
                failures);
            Check(
                alderwake.TowerCount >= 5 &&
                alderwake.TowerCount <= 7 &&
                veyrwater.TowerCount >= 4 &&
                veyrwater.TowerCount <= 6 &&
                alderwake.TowerCount != veyrwater.TowerCount,
                $"Tower counts are Alderwake {alderwake.TowerCount}, Veyrwater " +
                $"{veyrwater.TowerCount}; expected distinct 5-7 and 4-6 silhouettes.",
                failures);
            Check(
                alderwake.WalkableSegmentCount >= 4 &&
                veyrwater.WalkableSegmentCount >= 4 &&
                alderwake.GuardAlcoveCount >= 3 &&
                veyrwater.GuardAlcoveCount >= 3,
                "A city lacks walkable wall spans or guard alcoves.",
                failures);
            Check(
                !string.Equals(
                    alderwake.GatehouseSignature,
                    veyrwater.GatehouseSignature,
                    StringComparison.Ordinal) &&
                Mathf.Abs(alderwake.SkylineHeight - veyrwater.SkylineHeight) >= 0.75f,
                "Gatehouse signatures or skyline heights do not distinguish the cities.",
                failures);

            VerifyNamedFeatures(
                alderwake.VisualRoot,
                "Alderwake",
                new[]
                {
                    "curtain wall",
                    "gate",
                    "watch tower",
                    "wall-walk",
                    "guard alcove",
                    "banner",
                    "skyline focal frame",
                    "defensive terrace"
                },
                failures);
            VerifyNamedFeatures(
                veyrwater.VisualRoot,
                "Veyrwater",
                new[]
                {
                    "floodwall",
                    "canal-edge battlement",
                    "lantern watch tower",
                    "sluice",
                    "chain",
                    "reed",
                    "safe-crossing"
                },
                failures);

            var alderwakeBounds = CalculateRendererBounds(alderwake.VisualRoot);
            var veyrwaterBounds = CalculateRendererBounds(veyrwater.VisualRoot);
            Check(
                IsFinite(alderwakeBounds) &&
                IsFinite(veyrwaterBounds) &&
                alderwakeBounds.size.y >= 14f &&
                veyrwaterBounds.size.y >= 15f &&
                Mathf.Abs(alderwakeBounds.size.x - veyrwaterBounds.size.x) >= 25f,
                $"City silhouettes are insufficiently vertical or distinct: Alderwake " +
                $"{alderwakeBounds.size}, Veyrwater {veyrwaterBounds.size}.",
                failures);
        }

        private static void VerifyTraversalLoops(GameObject root, List<string> failures)
        {
            var routes =
                root.GetComponentsInChildren<TetherfallCityFortificationTraversal>(true);
            Check(
                routes.Length == TetherfallCityFortifications.ExpectedAscentRouteCount,
                $"Expected four wall ascent routes, found {routes.Length}.",
                failures);
            var routeIds = new HashSet<string>(StringComparer.Ordinal);
            var routesPerCity = new Dictionary<string, int>(StringComparer.Ordinal);
            var overlooksPerCity = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var routeIndex = 0; routeIndex < routes.Length; routeIndex++)
            {
                var route = routes[routeIndex];
                Check(
                    route != null &&
                    route.IsWallAscent &&
                    route.HasAlternateExit &&
                    route.StartsAtGround &&
                    route.ReturnsToGround &&
                    !string.IsNullOrWhiteSpace(route.RouteId) &&
                    routeIds.Add(route.RouteId) &&
                    !string.IsNullOrWhiteSpace(route.OverlookId),
                    $"Route {routeIndex} is duplicated, incomplete, dead-ended, or not a wall ascent.",
                    failures);
                if (route == null)
                {
                    continue;
                }

                routesPerCity.TryGetValue(route.CityId, out var cityRouteCount);
                routesPerCity[route.CityId] = cityRouteCount + 1;
                if (!string.IsNullOrWhiteSpace(route.OverlookId))
                {
                    overlooksPerCity.TryGetValue(route.CityId, out var cityOverlookCount);
                    overlooksPerCity[route.CityId] = cityOverlookCount + 1;
                }

                Check(
                    route.WorldNodes.Count >= 8 && route.PeakWorldHeight >= 8f,
                    $"Route '{route.RouteId}' does not reach a tower-height wall loop.",
                    failures);
                for (var nodeIndex = 0; nodeIndex < route.WorldNodes.Count; nodeIndex++)
                {
                    var node = route.WorldNodes[nodeIndex];
                    Check(
                        IsFinite(node),
                        $"Route '{route.RouteId}' node {nodeIndex} is non-finite.",
                        failures);
                    if (nodeIndex == 0)
                    {
                        continue;
                    }

                    var previous = route.WorldNodes[nodeIndex - 1];
                    var stepDistance = Vector3.Distance(previous, node);
                    var verticalStep = Mathf.Abs(node.y - previous.y);
                    Check(
                        stepDistance >= 0.25f &&
                        stepDistance <= 24f &&
                        verticalStep <= 6.5f,
                        $"Route '{route.RouteId}' step {nodeIndex} is unreachable " +
                        $"({stepDistance:0.##}m, vertical {verticalStep:0.##}m).",
                        failures);
                }

                var routeColliders =
                    root.GetComponentsInChildren<TetherfallCityFortificationCollider>(true);
                var ascentCount = 0;
                var climbCount = 0;
                var overlookCount = 0;
                var unreachableSupportCount = 0;
                for (var markerIndex = 0; markerIndex < routeColliders.Length; markerIndex++)
                {
                    var colliderMarker = routeColliders[markerIndex];
                    if (!string.Equals(
                            colliderMarker.RouteId,
                            route.RouteId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    ascentCount += colliderMarker.Role ==
                                   TetherfallCityFortificationColliderRole.Ascent
                        ? 1
                        : 0;
                    climbCount += colliderMarker.Role ==
                                  TetherfallCityFortificationColliderRole.ClimbableFace
                        ? 1
                        : 0;
                    overlookCount += colliderMarker.Role ==
                                     TetherfallCityFortificationColliderRole.TowerOverlook
                        ? 1
                        : 0;
                    var routeCollider = colliderMarker.GetComponent<Collider>();
                    if (routeCollider == null)
                    {
                        unreachableSupportCount++;
                        continue;
                    }

                    var closestNodeDistance = float.PositiveInfinity;
                    for (var nodeIndex = 0; nodeIndex < route.WorldNodes.Count; nodeIndex++)
                    {
                        closestNodeDistance = Mathf.Min(
                            closestNodeDistance,
                            Mathf.Sqrt(routeCollider.bounds.SqrDistance(
                                route.WorldNodes[nodeIndex])));
                    }

                    if (closestNodeDistance > 3.5f)
                    {
                        unreachableSupportCount++;
                    }
                }

                Check(
                    ascentCount >= 4 &&
                    climbCount == 1 &&
                    overlookCount == 1 &&
                    unreachableSupportCount == 0,
                    $"Route '{route.RouteId}' has {ascentCount} ascent supports, " +
                    $"{climbCount} climb faces, {overlookCount} overlooks, and " +
                    $"{unreachableSupportCount} supports disconnected from its route nodes.",
                    failures);
            }

            VerifyCityRouteCount(
                CityCatalog.AlderwakeHearthmarket.id,
                routesPerCity,
                overlooksPerCity,
                failures);
            VerifyCityRouteCount(
                CityCatalog.VeyrwaterMooring.id,
                routesPerCity,
                overlooksPerCity,
                failures);
        }

        private static void VerifyCityRouteCount(
            string cityId,
            Dictionary<string, int> routesPerCity,
            Dictionary<string, int> overlooksPerCity,
            List<string> failures)
        {
            routesPerCity.TryGetValue(cityId, out var routeCount);
            overlooksPerCity.TryGetValue(cityId, out var overlookCount);
            Check(
                routeCount >= 2 && overlookCount >= 1,
                $"City '{cityId}' has {routeCount} ascent routes and {overlookCount} overlooks.",
                failures);
        }

        private static void VerifyProtectedClearances(
            GameObject root,
            List<string> failures)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                var collider = colliders[colliderIndex];
                var label = GetHierarchyPath(collider.transform);
                Check(
                    IsFinite(collider.bounds) &&
                    collider.bounds.size.sqrMagnitude > 0.000001f,
                    $"Collider '{label}' has invalid bounds.",
                    failures);

                foreach (var threadway in ThreadwayCatalog.All)
                {
                    if (threadway == null)
                    {
                        continue;
                    }

                    VerifyPointClearance(
                        collider.bounds,
                        threadway.position,
                        ThreadwayClearance,
                        label,
                        "Threadway anchor '" + threadway.id + "'",
                        failures);
                    VerifyPointClearance(
                        collider.bounds,
                        threadway.arrivalPosition,
                        ThreadwayClearance,
                        label,
                        "Threadway arrival '" + threadway.id + "'",
                        failures);
                }

                foreach (var routeEvent in RouteEventCatalog.All)
                {
                    if (routeEvent != null)
                    {
                        VerifyPointClearance(
                            collider.bounds,
                            routeEvent.position,
                            CombatClearance,
                            label,
                            "route-event combat center '" + routeEvent.id + "'",
                            failures);
                    }
                }

                foreach (var bounty in BountyCatalog.All)
                {
                    if (bounty != null)
                    {
                        VerifyPointClearance(
                            collider.bounds,
                            bounty.position,
                            CombatClearance,
                            label,
                            "bounty combat center '" + bounty.id + "'",
                            failures);
                    }
                }

                VerifyCityClearances(
                    collider.bounds,
                    label,
                    CityCatalog.AlderwakeHearthmarket,
                    failures);
                VerifyCityClearances(
                    collider.bounds,
                    label,
                    CityCatalog.VeyrwaterMooring,
                    failures);
            }
        }

        private static void VerifyCityClearances(
            Bounds bounds,
            string colliderLabel,
            CityDefinition city,
            List<string> failures)
        {
            if (city == null)
            {
                return;
            }

            if (city.activities != null)
            {
                for (var index = 0; index < city.activities.Count; index++)
                {
                    var activity = city.activities[index];
                    if (activity != null && activity.kind == CityActivityKind.Training)
                    {
                        VerifyPointClearance(
                            bounds,
                            activity.arenaPosition,
                            CombatClearance,
                            colliderLabel,
                            "city training center '" + activity.id + "'",
                            failures);
                    }
                }
            }

            var institutions = CityInstitutionCatalog.GetForCity(city.id);
            for (var index = 0; index < institutions.Count; index++)
            {
                var institution = institutions[index];
                if (institution == null)
                {
                    continue;
                }

                VerifyPointClearance(
                    bounds,
                    institution.buildingPosition,
                    InstitutionClearance,
                    colliderLabel,
                    "institution '" + institution.id + "'",
                    failures);
                VerifyPointClearance(
                    bounds,
                    institution.interactionPosition,
                    InstitutionClearance,
                    colliderLabel,
                    "door approach '" + institution.id + "'",
                    failures);
            }

            var stalls = CityDistrictCatalog.GetMarketStallsForCity(city.id);
            for (var index = 0; index < stalls.Count; index++)
            {
                var stall = stalls[index];
                if (stall != null)
                {
                    VerifyPointClearance(
                        bounds,
                        stall.position,
                        MarketClearance,
                        colliderLabel,
                        "market stall '" + stall.id + "'",
                        failures);
                }
            }

            var traversal = CityDistrictCatalog.GetTraversalRouteForCity(city.id);
            if (traversal != null)
            {
                VerifyPointClearance(
                    bounds,
                    traversal.startPosition,
                    ExistingTraversalClearance,
                    colliderLabel,
                    "existing city traversal start '" + traversal.id + "'",
                    failures);
                if (traversal.checkpoints != null)
                {
                    for (var index = 0; index < traversal.checkpoints.Length; index++)
                    {
                        VerifyPointClearance(
                            bounds,
                            traversal.checkpoints[index],
                            ExistingTraversalClearance,
                            colliderLabel,
                            $"existing city traversal '{traversal.id}' checkpoint {index}",
                            failures);
                    }
                }
            }

            var isVeyrwater = string.Equals(
                city.id,
                CityCatalog.VeyrwaterMooring.id,
                StringComparison.Ordinal);
            var northSouthStartOffset = isVeyrwater ? -46.5f : -51f;
            var northSouthEndOffset = isVeyrwater ? 10.5f : 11f;
            var eastWestHalfLength = isVeyrwater ? 27f : 29f;
            VerifyRoadLane(
                bounds,
                city.center + new Vector3(0f, 0f, northSouthStartOffset),
                city.center + new Vector3(0f, 0f, northSouthEndOffset),
                colliderLabel,
                city.displayName + " north-south road",
                failures);
            VerifyRoadLane(
                bounds,
                city.center + new Vector3(-eastWestHalfLength, 0f, 1f),
                city.center + new Vector3(eastWestHalfLength, 0f, 1f),
                colliderLabel,
                city.displayName + " east-west road",
                failures);
        }

        private static void VerifyRoadLane(
            Bounds bounds,
            Vector3 start,
            Vector3 end,
            string colliderLabel,
            string roadLabel,
            List<string> failures)
        {
            var length = HorizontalDistance(start, end);
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(length / 2f));
            var closest = float.PositiveInfinity;
            for (var index = 0; index <= sampleCount; index++)
            {
                var point = Vector3.Lerp(start, end, index / (float)sampleCount);
                closest = Mathf.Min(closest, DistanceToBoundsXZ(bounds, point));
            }

            Check(
                closest >= ProtectedRoadHalfWidth,
                $"Collider '{colliderLabel}' enters the protected {roadLabel} lane " +
                $"({closest:0.##}m clearance).",
                failures);
        }

        private static void VerifyPointClearance(
            Bounds bounds,
            Vector3 point,
            float clearance,
            string colliderLabel,
            string protectedLabel,
            List<string> failures)
        {
            if (!IsFinite(point))
            {
                Check(false, $"{protectedLabel} has a non-finite position.", failures);
                return;
            }

            var distance = DistanceToBoundsXZ(bounds, point);
            Check(
                distance + 0.001f >= clearance,
                $"Collider '{colliderLabel}' is {distance:0.##}m from {protectedLabel}; " +
                $"required clearance is {clearance:0.##}m.",
                failures);
        }

        private static void VerifyMaterials(
            GameObject root,
            TetherfallCityFortificationRoot marker,
            List<string> failures)
        {
            const string prefix = "Tetherfall Fortifications · ";
            var authoredByName = new Dictionary<string, Material>(StringComparer.Ordinal);
            var tealRendererCount = 0;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var visualRenderer = renderers[rendererIndex];
                Check(
                    visualRenderer != null &&
                    visualRenderer.sharedMaterials.Length > 0 &&
                    IsFinite(visualRenderer.bounds) &&
                    visualRenderer.bounds.size.sqrMagnitude > 0.000001f &&
                    MaxAbs(visualRenderer.bounds.size) <= MaximumFiniteExtent,
                    $"Renderer '{GetHierarchyPath(visualRenderer.transform)}' is empty, " +
                    "unbounded, or missing materials.",
                    failures);
                if (visualRenderer == null)
                {
                    continue;
                }

                var materials = visualRenderer.sharedMaterials;
                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    var material = materials[materialIndex];
                    Check(
                        material != null,
                        $"Renderer '{GetHierarchyPath(visualRenderer.transform)}' has a null material.",
                        failures);
                    if (material == null || !material.name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (authoredByName.TryGetValue(material.name, out var first))
                    {
                        Check(
                            ReferenceEquals(first, material),
                            $"Procedural material '{material.name}' was instantiated per piece.",
                            failures);
                    }
                    else
                    {
                        authoredByName.Add(material.name, material);
                    }

                    if (material.name.EndsWith("SafeTeal", StringComparison.Ordinal))
                    {
                        tealRendererCount++;
                        var lowerName = visualRenderer.gameObject.name.ToLowerInvariant();
                        Check(
                            lowerName.Contains("teal") ||
                            lowerName.Contains("safe") ||
                            lowerName.Contains("bearing"),
                            $"Teal material appears on non-navigation renderer " +
                            $"'{GetHierarchyPath(visualRenderer.transform)}'.",
                            failures);
                    }
                }
            }

            Check(
                authoredByName.Count == marker.ProceduralMaterialCount &&
                authoredByName.Count <= TetherfallCityFortifications.MaxProceduralMaterialCount,
                $"Root references {authoredByName.Count} authored materials while metadata reports " +
                $"{marker.ProceduralMaterialCount}.",
                failures);
            Check(
                tealRendererCount > 0 && tealRendererCount <= 6,
                $"Teal navigation use is missing or excessive ({tealRendererCount} renderers).",
                failures);
        }

        private static void VerifySourceFallbackAccounting(
            GameObject root,
            TetherfallCityFortificationRoot marker,
            bool requireForcedFallback,
            List<string> failures)
        {
            var sourceNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in TetherfallQuaterniusAssetCatalog.All)
            {
                sourceNames.Add(definition.ResourceName);
            }

            var usedSources = 0;
            var fallbackCount = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < transforms.Length; index++)
            {
                var currentName = transforms[index].name;
                const string sourcePrefix = "Quaternius source · ";
                const string fallbackPrefix = "Procedural fallback · ";
                if (currentName.StartsWith(sourcePrefix, StringComparison.Ordinal))
                {
                    usedSources++;
                    var resourceName = currentName.Substring(sourcePrefix.Length);
                    Check(
                        sourceNames.Contains(resourceName),
                        $"Runtime hierarchy uses unapproved Quaternius source '{resourceName}'.",
                        failures);
                }
                else if (currentName.StartsWith(fallbackPrefix, StringComparison.Ordinal))
                {
                    fallbackCount++;
                    var resourceName = currentName.Substring(fallbackPrefix.Length);
                    Check(
                        sourceNames.Contains(resourceName),
                        $"Fallback hierarchy names unapproved source role '{resourceName}'.",
                        failures);
                }
            }

            Check(
                marker.PreferredResourcePlacementCount ==
                TetherfallCityFortifications.ExpectedPreferredResourcePlacementCount &&
                usedSources == marker.UsedPreferredResourceCount &&
                fallbackCount == marker.ProceduralFallbackCount &&
                usedSources + fallbackCount == marker.PreferredResourcePlacementCount,
                $"Source/fallback accounting is {usedSources}+{fallbackCount}/" +
                $"{marker.PreferredResourcePlacementCount}.",
                failures);
            Check(
                marker.UsedPreferredResourceCount ==
                TetherfallCityFortifications.BuiltPreferredResourceCount &&
                marker.ProceduralFallbackCount ==
                TetherfallCityFortifications.BuiltFallbackCount,
                "Static source/fallback metrics do not match root metadata.",
                failures);

            if (requireForcedFallback)
            {
                Check(
                    usedSources == 0 &&
                    fallbackCount ==
                    TetherfallCityFortifications.ExpectedPreferredResourcePlacementCount,
                    "Forced missing-Resources build retained a source prefab or missed a fallback.",
                    failures);
            }
            else
            {
                Check(
                    usedSources > 0 || fallbackCount ==
                    TetherfallCityFortifications.ExpectedPreferredResourcePlacementCount,
                    "Normal build produced neither approved Resources use nor complete fallback.",
                    failures);
            }
        }

        private static void VerifyFiniteHierarchy(
            Transform root,
            List<string> failures)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < transforms.Length; index++)
            {
                var value = transforms[index];
                Check(
                    IsFinite(value.localPosition) &&
                    IsFinite(value.localRotation) &&
                    IsFinite(value.localScale) &&
                    MaxAbs(value.localPosition) <= 500f &&
                    MaxAbs(value.localScale) <= MaximumFiniteExtent &&
                    Mathf.Min(
                        Mathf.Abs(value.localScale.x),
                        Mathf.Abs(value.localScale.y),
                        Mathf.Abs(value.localScale.z)) > 0.0001f,
                    $"Hierarchy contains an invalid transform at '{GetHierarchyPath(value)}'.",
                    failures);
            }
        }

        private static void VerifyNamedFeatures(
            Transform visualRoot,
            string cityName,
            IEnumerable<string> requiredTokens,
            List<string> failures)
        {
            Check(visualRoot != null, $"{cityName} visual root is null.", failures);
            if (visualRoot == null)
            {
                return;
            }

            var names = new List<string>();
            var transforms = visualRoot.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < transforms.Length; index++)
            {
                names.Add(transforms[index].name.ToLowerInvariant());
            }

            foreach (var token in requiredTokens)
            {
                var lowerToken = token.ToLowerInvariant();
                var found = false;
                for (var index = 0; index < names.Count; index++)
                {
                    if (names[index].Contains(lowerToken))
                    {
                        found = true;
                        break;
                    }
                }

                Check(
                    found,
                    $"{cityName} visual hierarchy is missing required '{token}' architecture.",
                    failures);
            }
        }

        private static void VerifyDocumentation(List<string> failures)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var path = Path.Combine(projectRoot, "CITY_FORTIFICATIONS.md");
            Check(File.Exists(path), "CITY_FORTIFICATIONS.md is missing.", failures);
            if (!File.Exists(path))
            {
                return;
            }

            var content = File.ReadAllText(path);
            foreach (var required in new[]
                     {
                         "TetherfallCityFortifications.cs",
                         "TetherfallCityFortificationsVerification.cs",
                         "CityDirector",
                         "Quaternius",
                         "CC0",
                         "forced missing-Resources",
                         "220 renderers",
                         "70 deliberate colliders",
                         "16 shared procedural materials",
                         "zero added realtime lights",
                         "one central culling driver",
                         "Alderwake Hearthmarket",
                         "Veyrwater Mooring"
                     })
            {
                Check(
                    content.Contains(required),
                    $"CITY_FORTIFICATIONS.md is missing required documentation phrase " +
                    $"'{required}'.",
                    failures);
            }
        }

        private static int CountDirectRoots(Transform parent)
        {
            var count = 0;
            for (var index = 0; index < parent.childCount; index++)
            {
                if (parent.GetChild(index)
                        .GetComponent<TetherfallCityFortificationRoot>() != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static BuildSnapshot CaptureSnapshot(GameObject root)
        {
            return new BuildSnapshot(
                root.GetComponentsInChildren<Transform>(true).Length,
                root.GetComponentsInChildren<Renderer>(true).Length,
                root.GetComponentsInChildren<Collider>(true).Length,
                root.GetComponentsInChildren<TetherfallCityFortificationTraversal>(true).Length,
                root.GetComponentsInChildren<TetherfallCityFortificationCullingCluster>(true).Length,
                CountNamedRoots(root.transform, "Quaternius source · "),
                CountNamedRoots(root.transform, "Procedural fallback · "));
        }

        private static int CountNamedRoots(Transform root, string prefix)
        {
            var count = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < transforms.Length; index++)
            {
                if (transforms[index].name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static Bounds CalculateRendererBounds(Transform root)
        {
            var renderers = root != null
                ? root.GetComponentsInChildren<Renderer>(true)
                : Array.Empty<Renderer>();
            var found = false;
            var bounds = new Bounds(root != null ? root.position : Vector3.zero, Vector3.zero);
            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] == null || !IsFinite(renderers[index].bounds))
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderers[index].bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[index].bounds);
                }
            }

            return found ? bounds : new Bounds(Vector3.zero, Vector3.zero);
        }

        private static float DistanceToBoundsXZ(Bounds bounds, Vector3 point)
        {
            var dx = point.x < bounds.min.x
                ? bounds.min.x - point.x
                : point.x > bounds.max.x
                    ? point.x - bounds.max.x
                    : 0f;
            var dz = point.z < bounds.min.z
                ? bounds.min.z - point.z
                : point.z > bounds.max.z
                    ? point.z - bounds.max.z
                    : 0f;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right)
        {
            var dx = left.x - right.x;
            var dz = left.z - right.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static float MaxAbs(Vector3 value)
        {
            return Mathf.Max(
                Mathf.Abs(value.x),
                Mathf.Abs(value.y),
                Mathf.Abs(value.z));
        }

        private static bool IsFinite(Bounds value)
        {
            return IsFinite(value.center) && IsFinite(value.size);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z) &&
                   IsFinite(value.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string GetHierarchyPath(Transform value)
        {
            if (value == null)
            {
                return "<null>";
            }

            var parts = new List<string>();
            var current = value;
            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static void Check(
            bool condition,
            string message,
            List<string> failures)
        {
            assertionCount++;
            if (!condition)
            {
                failures.Add(message);
            }
        }

        private readonly struct BuildSnapshot : IEquatable<BuildSnapshot>
        {
            private readonly int transforms;
            private readonly int renderers;
            private readonly int colliders;
            private readonly int routes;
            private readonly int clusters;
            private readonly int sources;
            private readonly int fallbacks;

            public BuildSnapshot(
                int transforms,
                int renderers,
                int colliders,
                int routes,
                int clusters,
                int sources,
                int fallbacks)
            {
                this.transforms = transforms;
                this.renderers = renderers;
                this.colliders = colliders;
                this.routes = routes;
                this.clusters = clusters;
                this.sources = sources;
                this.fallbacks = fallbacks;
            }

            public bool Equals(BuildSnapshot other)
            {
                return transforms == other.transforms &&
                       renderers == other.renderers &&
                       colliders == other.colliders &&
                       routes == other.routes &&
                       clusters == other.clusters &&
                       sources == other.sources &&
                       fallbacks == other.fallbacks;
            }

            public override bool Equals(object value)
            {
                return value is BuildSnapshot other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = transforms;
                    hash = hash * 397 ^ renderers;
                    hash = hash * 397 ^ colliders;
                    hash = hash * 397 ^ routes;
                    hash = hash * 397 ^ clusters;
                    hash = hash * 397 ^ sources;
                    hash = hash * 397 ^ fallbacks;
                    return hash;
                }
            }
        }
    }
}
