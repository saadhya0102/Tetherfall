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
    public static class TetherfallEmbercrownWorldVerification
    {
        private const string MenuPath = "Tetherfall/Verify Embercrown World";
        private const float RouteSampleSpacing = 0.45f;
        private static int assertionCount;

        [MenuItem(MenuPath, false, 2212)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Embercrown verification is edit-mode only. Stop Play Mode and run it again.");
            }

            assertionCount = 0;
            var failures = new List<string>();
            VerifyPublicContract(failures);
            VerifyCatalogAndGeometry(failures);
            VerifyRouteSafety(failures);
            VerifyLavaDamageCadence(failures);
            VerifyStaticSource(failures);
            VerifyRuntimeBuilds(failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Tetherfall Embercrown world verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            Debug.Log(
                $"Tetherfall Embercrown world verification passed: {assertionCount} assertions; " +
                $"{TetherfallEmbercrownWorld.LavaChannels.Count} contiguous branching lava channels, " +
                $"{TetherfallEmbercrownWorld.SafeRoutes.Count} safe routes, " +
                $"{TetherfallEmbercrownWorld.Sockets.Count} stable sockets, bounded lava cadence and " +
                "safe ejection, normal/fallback idempotence, collider separation, finite geometry, " +
                "and renderer/material/light/particle/update budgets all held.");
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        private static void VerifyPublicContract(List<string> failures)
        {
            var type = typeof(TetherfallEmbercrownWorld);
            var build = type.GetMethod(
                "Build",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(TetherfallGame) },
                null);
            Check(
                build != null && build.ReturnType == typeof(GameObject),
                "Public static Build(TetherfallGame) -> GameObject integration entrypoint is missing.",
                failures);
            Check(
                type.GetMethod(
                    "TryGetSocket",
                    BindingFlags.Public | BindingFlags.Static) != null &&
                type.GetMethod(
                    "InspectBuiltWorld",
                    BindingFlags.Public | BindingFlags.Static) != null,
                "Catalog lookup or runtime budget inspection API is missing.",
                failures);

            var budget = TetherfallEmbercrownWorld.BudgetLimits;
            Check(
                budget.Renderers <= 180 &&
                budget.Colliders <= 64 &&
                budget.SharedMaterials <= 12 &&
                budget.Lights <= 1 &&
                budget.ParticleSystems <= 2 &&
                budget.UpdateDrivers <= 2 &&
                budget.TotalParticleCapacity <= 60 &&
                budget.ProximityGatedRoots == 3,
                "Published Embercrown budgets exceed the strict vertical-slice ceilings.",
                failures);
            Check(
                TetherfallEmbercrownWorld.ActivationPollSeconds >= 0.2f &&
                TetherfallEmbercrownWorld.DetailPrewakeDistance <
                TetherfallEmbercrownWorld.TerrainPrewakeDistance &&
                TetherfallEmbercrownWorld.ActivationHysteresis > 0f,
                "Proximity activation cadence or hysteresis is not safely bounded.",
                failures);

            var bounds = TetherfallEmbercrownWorld.WorldBounds;
            Check(
                bounds.xMin >= TetherfallRegionPacks.WorldMinimumX &&
                bounds.xMax <= TetherfallRegionPacks.WorldMaximumX &&
                bounds.yMin >= TetherfallRegionPacks.WorldMinimumZ &&
                bounds.yMax <= TetherfallRegionPacks.WorldMaximumZ,
                $"Embercrown bounds {bounds} cross the established world boundary colliders.",
                failures);
            Check(
                Contains(bounds, TetherfallEmbercrownWorld.SafeArrival) &&
                Contains(bounds, TetherfallEmbercrownWorld.CalderaCenter) &&
                Contains(bounds, TetherfallEmbercrownWorld.ForgeCenter),
                "Arrival, caldera, or forge center lies outside the published region bounds.",
                failures);
        }

        private static void VerifyCatalogAndGeometry(List<string> failures)
        {
            var primaryIds = new HashSet<string>(StringComparer.Ordinal);
            Check(
                TetherfallEmbercrownWorld.LavaChannels.Count >= 4,
                "Embercrown needs at least four authored lava channels.",
                failures);
            Check(
                TetherfallEmbercrownWorld.SafeRoutes.Count >= 3,
                "Embercrown needs a primary route and at least two alternatives.",
                failures);
            Check(
                TetherfallEmbercrownWorld.Sockets.Count >= 8,
                "Embercrown needs at least eight stable resource/encounter sockets.",
                failures);

            for (var channelIndex = 0;
                 channelIndex < TetherfallEmbercrownWorld.LavaChannels.Count;
                 channelIndex++)
            {
                var channel = TetherfallEmbercrownWorld.LavaChannels[channelIndex];
                VerifyPrimaryId(
                    primaryIds,
                    channel.Id,
                    "lava.embercrown.",
                    "lava channel",
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(channel.DisplayName) &&
                    channel.Width >= 8f &&
                    channel.Width <= 20f &&
                    channel.Points.Count >= 4,
                    $"Lava channel '{channel.Id}' has invalid display, width, or point count.",
                    failures);
                Check(
                    IsFinite(channel.SafeEjectionPoint) &&
                    Contains(TetherfallEmbercrownWorld.WorldBounds, channel.SafeEjectionPoint) &&
                    !TetherfallEmbercrownWorld.IsHazardousPoint(channel.SafeEjectionPoint),
                    $"Lava channel '{channel.Id}' has an unsafe ejection point.",
                    failures);

                for (var pointIndex = 0;
                     pointIndex < channel.Points.Count;
                     pointIndex++)
                {
                    var point = channel.Points[pointIndex];
                    Check(
                        IsFinite(point) &&
                        Contains(TetherfallEmbercrownWorld.WorldBounds, point),
                        $"Lava channel '{channel.Id}' point {pointIndex} is nonfinite or out of bounds.",
                        failures);
                    if (pointIndex == 0)
                    {
                        continue;
                    }

                    var segmentLength = HorizontalDistance(
                        channel.Points[pointIndex - 1],
                        point);
                    Check(
                        segmentLength >= 8f && segmentLength <= 48f,
                        $"Lava channel '{channel.Id}' segment {pointIndex} length " +
                        $"{segmentLength:0.##} is not a contiguous authored span.",
                        failures);
                }
            }

            VerifyBranchingContinuity(failures);

            var alternateRouteCount = 0;
            for (var routeIndex = 0;
                 routeIndex < TetherfallEmbercrownWorld.SafeRoutes.Count;
                 routeIndex++)
            {
                var route = TetherfallEmbercrownWorld.SafeRoutes[routeIndex];
                VerifyPrimaryId(
                    primaryIds,
                    route.Id,
                    "route.embercrown.",
                    "safe route",
                    failures);
                if (route.IsAlternate)
                {
                    alternateRouteCount++;
                }

                Check(
                    route.MinimumWidth >= 4.5f &&
                    route.Points.Count >= 6 &&
                    HorizontalDistance(
                        route.Points[0],
                        TetherfallEmbercrownWorld.SafeArrival) <= 0.5f,
                    $"Safe route '{route.Id}' has insufficient width/points or no arrival connection.",
                    failures);
                for (var pointIndex = 0;
                     pointIndex < route.Points.Count;
                     pointIndex++)
                {
                    Check(
                        IsFinite(route.Points[pointIndex]) &&
                        Contains(TetherfallEmbercrownWorld.WorldBounds, route.Points[pointIndex]),
                        $"Safe route '{route.Id}' point {pointIndex} is nonfinite or out of bounds.",
                        failures);
                }
            }

            Check(
                alternateRouteCount >= 2,
                "Embercrown does not publish two independent alternate safe routes.",
                failures);

            var traversalIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0;
                 index < TetherfallEmbercrownWorld.Traversal.Count;
                 index++)
            {
                var traversal = TetherfallEmbercrownWorld.Traversal[index];
                VerifyPrimaryId(
                    primaryIds,
                    traversal.Id,
                    "traversal.embercrown.",
                    "traversal affordance",
                    failures);
                traversalIds.Add(traversal.Id);
                Check(
                    IsFinite(traversal.Position) &&
                    IsFinite(traversal.SafeLandingPoint) &&
                    Contains(TetherfallEmbercrownWorld.WorldBounds, traversal.Position) &&
                    Contains(TetherfallEmbercrownWorld.WorldBounds, traversal.SafeLandingPoint) &&
                    !TetherfallEmbercrownWorld.IsHazardousPoint(traversal.SafeLandingPoint),
                    $"Traversal '{traversal.Id}' has an invalid pose or unsafe landing.",
                    failures);
            }

            var bridgeCount = 0;
            var islandCount = 0;
            for (var index = 0;
                 index < TetherfallEmbercrownWorld.SafeSurfaces.Count;
                 index++)
            {
                var surface = TetherfallEmbercrownWorld.SafeSurfaces[index];
                Check(
                    traversalIds.Contains(surface.Id),
                    $"Safe surface '{surface.Id}' has no matching traversal catalog entry.",
                    failures);
                Check(
                    IsSemanticId(surface.Id, "traversal.embercrown.") &&
                    IsFinite(surface.Center) &&
                    IsFinite(surface.Size.x) &&
                    IsFinite(surface.Size.y) &&
                    IsFinite(surface.YawDegrees) &&
                    surface.Size.x >= 6f &&
                    surface.Size.y >= 8f &&
                    TetherfallEmbercrownWorld.IsInsideRawLava(surface.Center) &&
                    !TetherfallEmbercrownWorld.IsHazardousPoint(surface.Center),
                    $"Safe surface '{surface.Id}' is malformed or does not protect a lava crossing.",
                    failures);
                if (surface.Kind == TetherfallEmbercrownSafeSurfaceKind.Bridge)
                {
                    bridgeCount++;
                }
                else
                {
                    islandCount++;
                }
            }

            Check(
                bridgeCount >= 3 && islandCount >= 2,
                $"Expected at least three bridges and two islands; found {bridgeCount} and {islandCount}.",
                failures);
            VerifySockets(primaryIds, failures);
        }

        private static void VerifySockets(
            HashSet<string> primaryIds,
            List<string> failures)
        {
            var resourceCount = 0;
            var encounterCount = 0;
            for (var index = 0; index < TetherfallEmbercrownWorld.Sockets.Count; index++)
            {
                var socket = TetherfallEmbercrownWorld.Sockets[index];
                VerifyPrimaryId(
                    primaryIds,
                    socket.Id,
                    "socket.embercrown.",
                    "gameplay socket",
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(socket.DisplayName) &&
                    IsFinite(socket.Position) &&
                    IsFinite(socket.YawDegrees) &&
                    Contains(TetherfallEmbercrownWorld.WorldBounds, socket.Position) &&
                    !TetherfallEmbercrownWorld.IsHazardousPoint(socket.Position),
                    $"Socket '{socket.Id}' is unnamed, nonfinite, out of bounds, or in unsafe lava.",
                    failures);
                Check(
                    TetherfallEmbercrownWorld.TryGetSocket(socket.Id, out var lookup) &&
                    ReferenceEquals(socket, lookup),
                    $"Socket lookup did not return the catalog record for '{socket.Id}'.",
                    failures);

                if (socket.Kind == TetherfallEmbercrownSocketKind.Resource)
                {
                    resourceCount++;
                    Check(
                        string.IsNullOrEmpty(socket.EncounterId),
                        $"Resource socket '{socket.Id}' carries an encounter payload.",
                        failures);
                }
                else
                {
                    encounterCount++;
                    var encounter = EncounterCatalog.Get(socket.EncounterId);
                    Check(
                        !string.IsNullOrEmpty(socket.EncounterId) &&
                        encounter != null &&
                        string.Equals(encounter.id, socket.EncounterId, StringComparison.Ordinal),
                        $"Encounter socket '{socket.Id}' references unknown encounter " +
                        $"'{socket.EncounterId}'.",
                        failures);
                }
            }

            Check(
                resourceCount >= 5 && encounterCount >= 3,
                $"Socket mix is too small: {resourceCount} resource, {encounterCount} encounter.",
                failures);
        }

        private static void VerifyBranchingContinuity(List<string> failures)
        {
            var trunk = TetherfallEmbercrownWorld.LavaChannels[0];
            for (var channelIndex = 1;
                 channelIndex < TetherfallEmbercrownWorld.LavaChannels.Count;
                 channelIndex++)
            {
                var branch = TetherfallEmbercrownWorld.LavaChannels[channelIndex];
                var distance = DistanceToPolyline(branch.Points[0], trunk.Points);
                Check(
                    distance <= trunk.Width * 0.5f,
                    $"Branch '{branch.Id}' begins {distance:0.##}m from the Crownspill trunk.",
                    failures);
            }
        }

        private static void VerifyRouteSafety(List<string> failures)
        {
            Check(
                !TetherfallEmbercrownWorld.IsHazardousPoint(
                    TetherfallEmbercrownWorld.SafeArrival),
                "Embercrown safe arrival is inside an unprotected lava hazard.",
                failures);

            var forgeReachable = false;
            for (var routeIndex = 0;
                 routeIndex < TetherfallEmbercrownWorld.SafeRoutes.Count;
                 routeIndex++)
            {
                var route = TetherfallEmbercrownWorld.SafeRoutes[routeIndex];
                var unsafeSample = Vector3.zero;
                var isSafe = true;
                for (var pointIndex = 0;
                     pointIndex < route.Points.Count - 1 && isSafe;
                     pointIndex++)
                {
                    var start = route.Points[pointIndex];
                    var end = route.Points[pointIndex + 1];
                    var sampleCount = Mathf.Max(
                        1,
                        Mathf.CeilToInt(HorizontalDistance(start, end) / RouteSampleSpacing));
                    for (var sampleIndex = 0;
                         sampleIndex <= sampleCount;
                         sampleIndex++)
                    {
                        var sample = Vector3.Lerp(
                            start,
                            end,
                            sampleIndex / (float)sampleCount);
                        if (TetherfallEmbercrownWorld.IsHazardousPoint(sample))
                        {
                            isSafe = false;
                            unsafeSample = sample;
                            break;
                        }
                    }
                }

                Check(
                    isSafe,
                    $"Safe route '{route.Id}' enters unprotected lava near {unsafeSample}.",
                    failures);
                var finalPoint = route.Points[route.Points.Count - 1];
                if (HorizontalDistance(
                        finalPoint,
                        TetherfallEmbercrownWorld.ForgeCenter) <= 42f)
                {
                    forgeReachable = true;
                }
            }

            Check(
                forgeReachable,
                "No verified safe route reaches the Crownforge activation area.",
                failures);

            for (var channelIndex = 0;
                 channelIndex < TetherfallEmbercrownWorld.LavaChannels.Count;
                 channelIndex++)
            {
                var channel = TetherfallEmbercrownWorld.LavaChannels[channelIndex];
                Check(
                    DistanceToAnyRoute(channel.SafeEjectionPoint) <= 34f,
                    $"Safe ejection for '{channel.Id}' is not near a verified route.",
                    failures);
                Check(
                    TetherfallEmbercrownWorld.TryGetSafeEjectionPoint(
                        channel.Points[1],
                        out var resolved) &&
                    IsFinite(resolved) &&
                    !TetherfallEmbercrownWorld.IsHazardousPoint(resolved),
                    $"Safe-ejection resolver failed for '{channel.Id}'.",
                    failures);
            }
        }

        private static void VerifyLavaDamageCadence(List<string> failures)
        {
            Check(
                TetherfallEmbercrownWorld.LavaDamageTickSeconds >= 0.65f &&
                TetherfallEmbercrownWorld.LavaHazardPollSeconds >= 0.08f &&
                TetherfallEmbercrownWorld.LavaDamagePerTick > 0 &&
                TetherfallEmbercrownWorld.LavaDamagePerTick <= 10,
                "Lava damage tick/poll cadence is too aggressive.",
                failures);
            Check(
                TetherfallEmbercrownWorld.GetLavaTickCountForExposure(0f) == 0 &&
                TetherfallEmbercrownWorld.GetLavaTickCountForExposure(0.74f) == 0 &&
                TetherfallEmbercrownWorld.GetLavaTickCountForExposure(0.75f) == 1 &&
                TetherfallEmbercrownWorld.GetLavaTickCountForExposure(1.5f) == 2 &&
                TetherfallEmbercrownWorld.GetLavaTickCountForExposure(30f) ==
                TetherfallEmbercrownWorld.LavaTicksBeforeEjection,
                "Pure lava-cadence simulation does not match the authored 0.75-second ticks.",
                failures);
            Check(
                TetherfallEmbercrownWorld.GetBoundedLavaDamage(30f) ==
                TetherfallEmbercrownWorld.MaximumContinuousLavaDamage &&
                TetherfallEmbercrownWorld.MaximumContinuousLavaDamage <= 40,
                "Continuous lava exposure is not capped to the published damage bound.",
                failures);
            Check(
                !TetherfallEmbercrownWorld.ShouldEjectAfterLavaTicks(
                    TetherfallEmbercrownWorld.LavaTicksBeforeEjection - 1) &&
                TetherfallEmbercrownWorld.ShouldEjectAfterLavaTicks(
                    TetherfallEmbercrownWorld.LavaTicksBeforeEjection) &&
                TetherfallEmbercrownWorld.LavaReentryGraceSeconds >= 1f,
                "Lava ejection threshold or re-entry grace is invalid.",
                failures);
        }

        private static void VerifyStaticSource(List<string> failures)
        {
            var runtimePath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "TetherfallEmbercrownWorld.cs");
            var verifierPath = Path.Combine(
                Application.dataPath,
                "Editor",
                "TetherfallEmbercrownWorldVerification.cs");
            Check(File.Exists(runtimePath), $"Missing runtime source '{runtimePath}'.", failures);
            Check(File.Exists(verifierPath), $"Missing verifier source '{verifierPath}'.", failures);
            if (!File.Exists(runtimePath))
            {
                return;
            }

            var source = File.ReadAllText(runtimePath);
            var forbidden = new[]
            {
                "GameObject.Find(",
                "transform.Find(",
                "FindAnyObjectByType",
                "FindFirstObjectByType",
                "FindObjectsByType",
                "renderer.material =",
                "new Material(",
                "[RuntimeInitializeOnLoadMethod"
            };
            for (var index = 0; index < forbidden.Length; index++)
            {
                Check(
                    source.IndexOf(forbidden[index], StringComparison.Ordinal) < 0,
                    $"Runtime source contains forbidden static-lint token '{forbidden[index]}'.",
                    failures);
            }

            Check(
                source.Contains("public static GameObject Build(TetherfallGame game)") &&
                source.Contains("RuntimeEffectMaterialCache.GetOpaque") &&
                source.Contains("RuntimeEffectMaterialCache.GetGlow") &&
                source.Contains("private void Update()"),
                "Runtime source omits the explicit integration, shared material cache, or bounded driver.",
                failures);
        }

        private static void VerifyRuntimeBuilds(List<string> failures)
        {
            var fixture = new GameObject("Embercrown World Verification");
            var game = fixture.AddComponent<TetherfallGame>();
            try
            {
                var normalRoot = TetherfallEmbercrownWorld.Build(game);
                Check(normalRoot != null, "Normal Embercrown Build returned null.", failures);
                if (normalRoot != null)
                {
                    VerifyBuiltRoot(normalRoot, false, failures);
                    var firstSnapshot = CaptureSnapshot(normalRoot);
                    var repeated = TetherfallEmbercrownWorld.Build(game);
                    Check(
                        ReferenceEquals(normalRoot, repeated),
                        "Repeated Build did not return the existing Embercrown root.",
                        failures);
                    Check(
                        firstSnapshot.Equals(CaptureSnapshot(repeated)) &&
                        CountDirectRoots(game.transform) == 1,
                        "Repeated Build changed hierarchy/budgets or duplicated the root.",
                        failures);
                }

                Check(
                    TetherfallEmbercrownWorld.Cleanup(game),
                    "Normal Embercrown cleanup failed.",
                    failures);
                Check(
                    !TetherfallEmbercrownWorld.Cleanup(game) &&
                    CountDirectRoots(game.transform) == 0,
                    "Embercrown cleanup is not idempotent.",
                    failures);

                var fallbackRoot = TetherfallEmbercrownWorld.BuildProceduralFallback(game);
                Check(
                    fallbackRoot != null,
                    "Forced procedural Embercrown Build returned null.",
                    failures);
                if (fallbackRoot != null)
                {
                    VerifyBuiltRoot(fallbackRoot, true, failures);
                    var firstSnapshot = CaptureSnapshot(fallbackRoot);
                    var repeated =
                        TetherfallEmbercrownWorld.BuildProceduralFallback(game);
                    Check(
                        ReferenceEquals(fallbackRoot, repeated) &&
                        firstSnapshot.Equals(CaptureSnapshot(repeated)) &&
                        CountDirectRoots(game.transform) == 1,
                        "Repeated procedural fallback Build is not idempotent.",
                        failures);
                }
            }
            catch (Exception exception)
            {
                Check(
                    false,
                    $"Runtime build verification threw {exception.GetType().Name}: " +
                    exception.Message,
                    failures);
            }
            finally
            {
                TetherfallEmbercrownWorld.Cleanup(game);
                Object.DestroyImmediate(fixture);
                InvokeStaticReset(typeof(TetherfallGame), "ResetStatics");
                InvokeStaticReset(typeof(RuntimeEffectMaterialCache), "Reset");
            }
        }

        private static void VerifyBuiltRoot(
            GameObject root,
            bool forcedFallback,
            List<string> failures)
        {
            var marker = root.GetComponent<TetherfallEmbercrownWorldRoot>();
            Check(marker != null, "Built Embercrown root has no runtime metadata.", failures);
            if (marker == null)
            {
                return;
            }

            Check(
                root.name == TetherfallEmbercrownWorld.RootName &&
                root.transform.parent != null &&
                root.transform.localPosition == Vector3.zero &&
                Quaternion.Angle(root.transform.localRotation, Quaternion.identity) < 0.001f &&
                Vector3.SqrMagnitude(root.transform.localScale - Vector3.one) < 0.000001f &&
                marker.ForcedProceduralFallback == forcedFallback,
                "Built root identity transform or fallback metadata is invalid.",
                failures);
            Check(
                marker.SilhouetteRoot != null &&
                marker.ConnectorRoot != null &&
                marker.TerrainActivationRoot != null &&
                marker.DetailActivationRoot != null &&
                marker.ForgeActivationRoot != null &&
                marker.SilhouetteRoot.activeSelf &&
                marker.ConnectorRoot.activeSelf &&
                !marker.TerrainActivationRoot.activeSelf &&
                !marker.DetailActivationRoot.activeSelf &&
                !marker.ForgeActivationRoot.activeSelf,
                "Always-on or proximity-gated activation roots have the wrong initial state.",
                failures);

            VerifyFiniteHierarchy(root.transform, failures);
            VerifyColliderSeparation(root, failures);
            VerifyRuntimeCatalogMarkers(root, failures);
            VerifyRuntimeBudgets(root, marker, forcedFallback, failures);
            VerifyUpdateDrivers(root, failures);
        }

        private static void VerifyFiniteHierarchy(
            Transform root,
            List<string> failures)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < transforms.Length; index++)
            {
                var current = transforms[index];
                Check(
                    IsFinite(current.position) &&
                    IsFinite(current.localPosition) &&
                    IsFinite(current.localScale) &&
                    IsFinite(current.rotation) &&
                    current.localScale.x > 0f &&
                    current.localScale.y > 0f &&
                    current.localScale.z > 0f,
                    $"Nonfinite or non-positive transform at '{GetHierarchyPath(current)}'.",
                    failures);
            }

            var boxColliders = root.GetComponentsInChildren<BoxCollider>(true);
            for (var index = 0; index < boxColliders.Length; index++)
            {
                var box = boxColliders[index];
                Check(
                    IsFinite(box.center) &&
                    IsFinite(box.size) &&
                    box.size.x > 0f &&
                    box.size.y > 0f &&
                    box.size.z > 0f &&
                    MaxAbs(box.size) <= 260f,
                    $"Invalid BoxCollider at '{GetHierarchyPath(box.transform)}'.",
                    failures);
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                Check(
                    renderer.sharedMaterials.Length > 0,
                    $"Renderer '{GetHierarchyPath(renderer.transform)}' has no material slots.",
                    failures);
                var materials = renderer.sharedMaterials;
                for (var materialIndex = 0;
                     materialIndex < materials.Length;
                     materialIndex++)
                {
                    Check(
                        materials[materialIndex] != null,
                        $"Renderer '{GetHierarchyPath(renderer.transform)}' has a null shared material.",
                        failures);
                }
            }
        }

        private static void VerifyColliderSeparation(
            GameObject root,
            List<string> failures)
        {
            var artRoots = root.GetComponentsInChildren<TetherfallEmbercrownArtRoot>(true);
            var collisionRoots =
                root.GetComponentsInChildren<TetherfallEmbercrownCollisionRoot>(true);
            Check(
                artRoots.Length >= 6 && collisionRoots.Length >= 3,
                $"Expected separated art/collision roots; found {artRoots.Length}/{collisionRoots.Length}.",
                failures);
            for (var index = 0; index < artRoots.Length; index++)
            {
                Check(
                    artRoots[index].GetComponentsInChildren<Collider>(true).Length == 0,
                    $"Art root '{GetHierarchyPath(artRoots[index].transform)}' contains collision.",
                    failures);
            }

            for (var index = 0; index < collisionRoots.Length; index++)
            {
                Check(
                    collisionRoots[index].GetComponentsInChildren<Renderer>(true).Length == 0,
                    $"Collision root '{GetHierarchyPath(collisionRoots[index].transform)}' contains art.",
                    failures);
            }

            Check(
                root.GetComponentsInChildren<MeshCollider>(true).Length == 0 &&
                root.GetComponentsInChildren<Rigidbody>(true).Length == 0 &&
                root.GetComponentsInChildren<Rigidbody2D>(true).Length == 0 &&
                root.GetComponentsInChildren<Collider2D>(true).Length == 0 &&
                root.GetComponentsInChildren<Camera>(true).Length == 0 &&
                root.GetComponentsInChildren<Animator>(true).Length == 0 &&
                root.GetComponentsInChildren<Animation>(true).Length == 0,
                "Embercrown contains a forbidden mesh collider, body, 2D collider, camera, or animator.",
                failures);
        }

        private static void VerifyRuntimeCatalogMarkers(
            GameObject root,
            List<string> failures)
        {
            var sockets = root.GetComponentsInChildren<TetherfallEmbercrownWorldSocket>(true);
            var socketIds = new HashSet<string>(StringComparer.Ordinal);
            Check(
                sockets.Length == TetherfallEmbercrownWorld.Sockets.Count,
                $"Built {sockets.Length} socket markers; catalog has " +
                $"{TetherfallEmbercrownWorld.Sockets.Count}.",
                failures);
            for (var index = 0; index < sockets.Length; index++)
            {
                Check(
                    !string.IsNullOrWhiteSpace(sockets[index].StableId) &&
                    socketIds.Add(sockets[index].StableId) &&
                    TetherfallEmbercrownWorld.TryGetSocket(
                        sockets[index].StableId,
                        out var definition) &&
                    HorizontalDistance(sockets[index].transform.position, definition.Position) < 0.01f,
                    $"Runtime socket marker {index} has duplicate, unknown, or displaced metadata.",
                    failures);
            }

            var hazards =
                root.GetComponentsInChildren<TetherfallEmbercrownLavaHazardVolume>(true);
            var expectedHazards = 0;
            for (var index = 0;
                 index < TetherfallEmbercrownWorld.LavaChannels.Count;
                 index++)
            {
                expectedHazards +=
                    TetherfallEmbercrownWorld.LavaChannels[index].Points.Count - 1;
            }

            var hazardIds = new HashSet<string>(StringComparer.Ordinal);
            Check(
                hazards.Length == expectedHazards,
                $"Built {hazards.Length} lava volumes; expected {expectedHazards}.",
                failures);
            for (var index = 0; index < hazards.Length; index++)
            {
                Check(
                    !string.IsNullOrWhiteSpace(hazards[index].StableId) &&
                    !string.IsNullOrWhiteSpace(hazards[index].ChannelId) &&
                    hazardIds.Add(hazards[index].StableId) &&
                    hazards[index].GetComponent<BoxCollider>() != null &&
                    hazards[index].GetComponent<BoxCollider>().isTrigger &&
                    !TetherfallEmbercrownWorld.IsHazardousPoint(
                        hazards[index].SafeEjectionPoint),
                    $"Lava volume {index} has duplicate metadata, no trigger, or unsafe ejection.",
                    failures);
            }

            var traversal =
                root.GetComponentsInChildren<TetherfallEmbercrownTraversalAffordance>(true);
            var traversalIds = new HashSet<string>(StringComparer.Ordinal);
            Check(
                traversal.Length == TetherfallEmbercrownWorld.Traversal.Count,
                $"Built {traversal.Length} traversal markers; catalog has " +
                $"{TetherfallEmbercrownWorld.Traversal.Count}.",
                failures);
            for (var index = 0; index < traversal.Length; index++)
            {
                Check(
                    traversalIds.Add(traversal[index].StableId),
                    $"Runtime traversal ID '{traversal[index].StableId}' is duplicated.",
                    failures);
            }
        }

        private static void VerifyRuntimeBudgets(
            GameObject root,
            TetherfallEmbercrownWorldRoot marker,
            bool forcedFallback,
            List<string> failures)
        {
            var metrics = TetherfallEmbercrownWorld.InspectBuiltWorld(root);
            var budget = TetherfallEmbercrownWorld.BudgetLimits;
            Check(
                metrics.Renderers >= 110 &&
                metrics.Renderers <= budget.Renderers &&
                metrics.Colliders >= 35 &&
                metrics.Colliders <= budget.Colliders &&
                metrics.SharedMaterials > 0 &&
                metrics.SharedMaterials <= budget.SharedMaterials,
                $"Runtime geometry/material budget invalid: {metrics.Renderers} renderers, " +
                $"{metrics.Colliders} colliders, {metrics.SharedMaterials} materials.",
                failures);
            Check(
                metrics.Lights == 1 &&
                metrics.Lights <= budget.Lights &&
                metrics.ParticleSystems == 2 &&
                metrics.ParticleSystems <= budget.ParticleSystems &&
                metrics.TotalParticleCapacity ==
                TetherfallEmbercrownWorld.MaxTotalParticleCapacity,
                $"Runtime effect budget invalid: {metrics.Lights} lights, " +
                $"{metrics.ParticleSystems} systems, {metrics.TotalParticleCapacity} particles.",
                failures);
            Check(
                MetricsEqual(metrics, marker.Metrics),
                "Root budget metadata does not match a fresh runtime inspection.",
                failures);

            var particles = root.GetComponentsInChildren<ParticleSystem>(true);
            for (var index = 0; index < particles.Length; index++)
            {
                var particleRenderer = particles[index].GetComponent<ParticleSystemRenderer>();
                Check(
                    !particles[index].main.prewarm &&
                    !particles[index].collision.enabled &&
                    !particles[index].lights.enabled &&
                    !particles[index].trails.enabled &&
                    particleRenderer != null &&
                    particleRenderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off,
                    $"Particle system '{GetHierarchyPath(particles[index].transform)}' violates VFX budget policy.",
                    failures);
            }

            var light = root.GetComponentInChildren<Light>(true);
            Check(
                light != null &&
                light.shadows == LightShadows.None &&
                light.range <= 18f &&
                IsDescendantOf(light.transform, marker.ForgeActivationRoot.transform),
                "The sole light is unbounded, shadowed, or outside the gated forge root.",
                failures);

            if (forcedFallback)
            {
                Check(
                    metrics.ImportedPlacements == 0 &&
                    metrics.ProceduralFallbackPlacements >= 8,
                    "Forced fallback build retained imported art or omitted procedural replacements.",
                    failures);
            }
            else
            {
                var imports =
                    root.GetComponentsInChildren<TetherfallEmbercrownImportedArt>(true);
                var hasQuaternius = false;
                var hasKenney = false;
                for (var index = 0; index < imports.Length; index++)
                {
                    hasQuaternius |= imports[index].ResourcePath.StartsWith(
                        "Art/Quaternius/",
                        StringComparison.Ordinal);
                    hasKenney |= imports[index].ResourcePath.StartsWith(
                        "Art/KenneyNature/",
                        StringComparison.Ordinal);
                }

                Check(
                    hasQuaternius && hasKenney &&
                    metrics.ImportedPlacements + metrics.ProceduralFallbackPlacements >= 8,
                    "Normal build did not use both curated Quaternius and Kenney art with full fallback coverage.",
                    failures);
            }
        }

        private static void VerifyUpdateDrivers(
            GameObject root,
            List<string> failures)
        {
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            var updateDriverCount = 0;
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
                    var method = type.GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly);
                    if (method == null)
                    {
                        continue;
                    }

                    updateDriverCount++;
                    Check(
                        type == typeof(TetherfallEmbercrownProximityDirector) ||
                        type == typeof(TetherfallEmbercrownLavaHazardDirector),
                        $"Unexpected per-frame loop '{type.FullName}.{methodName}'.",
                        failures);
                }
            }

            Check(
                updateDriverCount == 2 &&
                updateDriverCount <= TetherfallEmbercrownWorld.MaxUpdateDriverCount,
                $"Expected exactly two centralized update drivers, found {updateDriverCount}.",
                failures);
        }

        private static Snapshot CaptureSnapshot(GameObject root)
        {
            var metrics = TetherfallEmbercrownWorld.InspectBuiltWorld(root);
            return new Snapshot(
                root.GetComponentsInChildren<Transform>(true).Length,
                metrics.Renderers,
                metrics.Colliders,
                metrics.SharedMaterials,
                metrics.Lights,
                metrics.ParticleSystems,
                metrics.UpdateDrivers,
                metrics.ImportedPlacements,
                metrics.ProceduralFallbackPlacements);
        }

        private static int CountDirectRoots(Transform parent)
        {
            var count = 0;
            for (var index = 0; index < parent.childCount; index++)
            {
                if (string.Equals(
                        parent.GetChild(index).name,
                        TetherfallEmbercrownWorld.RootName,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool MetricsEqual(
            TetherfallEmbercrownWorldMetrics left,
            TetherfallEmbercrownWorldMetrics right)
        {
            return left.Renderers == right.Renderers &&
                   left.Colliders == right.Colliders &&
                   left.SharedMaterials == right.SharedMaterials &&
                   left.Lights == right.Lights &&
                   left.ParticleSystems == right.ParticleSystems &&
                   left.UpdateDrivers == right.UpdateDrivers &&
                   left.TotalParticleCapacity == right.TotalParticleCapacity &&
                   left.ImportedPlacements == right.ImportedPlacements &&
                   left.ProceduralFallbackPlacements == right.ProceduralFallbackPlacements;
        }

        private static void VerifyPrimaryId(
            HashSet<string> ids,
            string id,
            string prefix,
            string owner,
            List<string> failures)
        {
            Check(
                IsSemanticId(id, prefix) && ids.Add(id),
                $"{owner} ID '{id}' is malformed or duplicated.",
                failures);
        }

        private static bool IsSemanticId(string id, string prefix)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                !id.StartsWith(prefix, StringComparison.Ordinal) ||
                id.EndsWith(".", StringComparison.Ordinal))
            {
                return false;
            }

            for (var index = 0; index < id.Length; index++)
            {
                var character = id[index];
                if ((character >= 'a' && character <= 'z') ||
                    (character >= '0' && character <= '9') ||
                    character == '.' ||
                    character == '-')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static float DistanceToAnyRoute(Vector3 point)
        {
            var closest = float.PositiveInfinity;
            for (var routeIndex = 0;
                 routeIndex < TetherfallEmbercrownWorld.SafeRoutes.Count;
                 routeIndex++)
            {
                closest = Mathf.Min(
                    closest,
                    DistanceToPolyline(
                        point,
                        TetherfallEmbercrownWorld.SafeRoutes[routeIndex].Points));
            }

            return closest;
        }

        private static float DistanceToPolyline(
            Vector3 point,
            IReadOnlyList<Vector3> polyline)
        {
            var closestSquared = float.PositiveInfinity;
            for (var index = 0; index < polyline.Count - 1; index++)
            {
                closestSquared = Mathf.Min(
                    closestSquared,
                    HorizontalDistanceSquaredToSegment(
                        point,
                        polyline[index],
                        polyline[index + 1]));
            }

            return Mathf.Sqrt(closestSquared);
        }

        private static float HorizontalDistanceSquaredToSegment(
            Vector3 point,
            Vector3 start,
            Vector3 end)
        {
            var segmentX = end.x - start.x;
            var segmentZ = end.z - start.z;
            var lengthSquared = segmentX * segmentX + segmentZ * segmentZ;
            if (lengthSquared <= 0.000001f)
            {
                var pointX = point.x - start.x;
                var pointZ = point.z - start.z;
                return pointX * pointX + pointZ * pointZ;
            }

            var projection = Mathf.Clamp01(
                ((point.x - start.x) * segmentX + (point.z - start.z) * segmentZ) /
                lengthSquared);
            var closestX = start.x + segmentX * projection;
            var closestZ = start.z + segmentZ * projection;
            var deltaX = point.x - closestX;
            var deltaZ = point.z - closestZ;
            return deltaX * deltaX + deltaZ * deltaZ;
        }

        private static bool Contains(Rect bounds, Vector3 point)
        {
            return point.x >= bounds.xMin &&
                   point.x <= bounds.xMax &&
                   point.z >= bounds.yMin &&
                   point.z <= bounds.yMax;
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right)
        {
            var deltaX = left.x - right.x;
            var deltaZ = left.z - right.z;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        private static float MaxAbs(Vector3 value)
        {
            return Mathf.Max(
                Mathf.Abs(value.x),
                Mathf.Max(Mathf.Abs(value.y), Mathf.Abs(value.z)));
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z) &&
                   IsFinite(value.w);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsDescendantOf(Transform child, Transform parent)
        {
            for (var current = child; current != null; current = current.parent)
            {
                if (current == parent)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "<missing>";
            }

            var path = transform.name;
            for (var parent = transform.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }

            return path;
        }

        private static void InvokeStaticReset(Type type, string methodName)
        {
            var method = type.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            method?.Invoke(null, null);
        }

        private static void Check(
            bool condition,
            string failure,
            List<string> failures)
        {
            assertionCount++;
            if (!condition)
            {
                failures.Add(failure);
            }
        }

        private readonly struct Snapshot : IEquatable<Snapshot>
        {
            private readonly int transforms;
            private readonly int renderers;
            private readonly int colliders;
            private readonly int materials;
            private readonly int lights;
            private readonly int particles;
            private readonly int updateDrivers;
            private readonly int imports;
            private readonly int fallbacks;

            public Snapshot(
                int transforms,
                int renderers,
                int colliders,
                int materials,
                int lights,
                int particles,
                int updateDrivers,
                int imports,
                int fallbacks)
            {
                this.transforms = transforms;
                this.renderers = renderers;
                this.colliders = colliders;
                this.materials = materials;
                this.lights = lights;
                this.particles = particles;
                this.updateDrivers = updateDrivers;
                this.imports = imports;
                this.fallbacks = fallbacks;
            }

            public bool Equals(Snapshot other)
            {
                return transforms == other.transforms &&
                       renderers == other.renderers &&
                       colliders == other.colliders &&
                       materials == other.materials &&
                       lights == other.lights &&
                       particles == other.particles &&
                       updateDrivers == other.updateDrivers &&
                       imports == other.imports &&
                       fallbacks == other.fallbacks;
            }

            public override bool Equals(object value)
            {
                return value is Snapshot other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = transforms;
                    hash = hash * 397 ^ renderers;
                    hash = hash * 397 ^ colliders;
                    hash = hash * 397 ^ materials;
                    hash = hash * 397 ^ lights;
                    hash = hash * 397 ^ particles;
                    hash = hash * 397 ^ updateDrivers;
                    hash = hash * 397 ^ imports;
                    hash = hash * 397 ^ fallbacks;
                    return hash;
                }
            }
        }
    }
}
