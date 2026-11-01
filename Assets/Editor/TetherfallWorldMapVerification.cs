using System;
using System.Collections.Generic;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallWorldMapVerification
    {
        private const string MenuPath = "Tetherfall/Verify Threadway World Map";

        [MenuItem(MenuPath, false, 2206)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Threadway world-map verification is edit-mode only. Stop Play Mode and run it again.");
            }

            var failures = new List<string>();
            VerifyCatalogAndProjection(failures);
            VerifyTravelQuotes(failures);
            VerifyRuntimeSelectionDiscoveryAndArrival(failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Threadway world-map verification failed:\n- " + string.Join("\n- ", failures));
            }

            Debug.Log(
                "Threadway world-map verification passed: nine projected anchors, explicit confirmation gating, " +
                "distance-band quotes, discovery seeding, finite validation, and bounded safe-arrival fallback verified.");
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        private static void VerifyCatalogAndProjection(List<string> failures)
        {
            Check(ThreadwayCatalog.All.Count == 9,
                $"Expected nine Threadways, found {ThreadwayCatalog.All.Count}.",
                failures);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var mapRect = new Rect(10f, 20f, 480f, 260f);
            foreach (var definition in ThreadwayCatalog.All)
            {
                Check(definition != null, "Threadway catalog contains a null definition.", failures);
                if (definition == null)
                {
                    continue;
                }

                Check(!string.IsNullOrEmpty(definition.id),
                    "A Threadway has no stable ID.",
                    failures);
                Check(ids.Add(definition.id),
                    $"Duplicate Threadway ID '{definition.id}'.",
                    failures);
                Check(TetherfallWorldMapRules.IsFinite(definition.position),
                    $"Threadway '{definition.id}' has a nonfinite marker position.",
                    failures);
                Check(TetherfallWorldMapRules.IsFinite(definition.arrivalPosition),
                    $"Threadway '{definition.id}' has a nonfinite arrival.",
                    failures);
                Check(
                    TetherfallWorldMapRules.TryProjectWorldToMap(
                        definition.position,
                        mapRect,
                        out var projected),
                    $"Threadway '{definition.id}' did not project.",
                    failures);
                Check(
                    TetherfallWorldMapRules.IsFinite(projected.x) &&
                    TetherfallWorldMapRules.IsFinite(projected.y) &&
                    mapRect.Contains(projected),
                    $"Threadway '{definition.id}' projected outside the map well.",
                    failures);
            }

            var minimumCorner = TetherfallWorldMapRules.ProjectWorldToMap(
                new Vector3(
                    TetherfallWorldMapRules.MinimumWorldX,
                    0f,
                    TetherfallWorldMapRules.MinimumWorldZ),
                mapRect);
            var maximumCorner = TetherfallWorldMapRules.ProjectWorldToMap(
                new Vector3(
                    TetherfallWorldMapRules.MaximumWorldX,
                    0f,
                    TetherfallWorldMapRules.MaximumWorldZ),
                mapRect);
            Check(Approximately(minimumCorner.x, mapRect.xMin) &&
                  Approximately(minimumCorner.y, mapRect.yMax),
                "Minimum X/Z did not map to the lower-left map corner.",
                failures);
            Check(Approximately(maximumCorner.x, mapRect.xMax) &&
                  Approximately(maximumCorner.y, mapRect.yMin),
                "Maximum X/Z did not map to the upper-right map corner.",
                failures);

            var nonfiniteProjected = TetherfallWorldMapRules.TryProjectWorldToMap(
                new Vector3(float.NaN, 0f, 0f),
                mapRect,
                out var finiteFallback);
            Check(!nonfiniteProjected,
                "A nonfinite world position projected successfully.",
                failures);
            Check(TetherfallWorldMapRules.IsFinite(finiteFallback.x) &&
                  TetherfallWorldMapRules.IsFinite(finiteFallback.y),
                "Projection failure did not return a finite fallback.",
                failures);
        }

        private static void VerifyTravelQuotes(List<string> failures)
        {
            Check(TetherfallWorldMapRules.GetTravelChimesaltCost(79.999f) == 0,
                "A route under 80m did not cost zero Chimesalt.",
                failures);
            Check(TetherfallWorldMapRules.GetTravelChimesaltCost(80f) == 1,
                "An 80m route did not cost one Chimesalt.",
                failures);
            Check(TetherfallWorldMapRules.GetTravelChimesaltCost(159.999f) == 1,
                "A route under 160m did not cost one Chimesalt.",
                failures);
            Check(TetherfallWorldMapRules.GetTravelChimesaltCost(160f) == 2,
                "A 160m route did not cost two Chimesalt.",
                failures);

            var source = ThreadwayCatalog.Get("threadway.hearthmarket");
            var destination = ThreadwayCatalog.Get("threadway.ashshelf");
            var ready = TetherfallWorldMapRules.CreateTravelQuote(
                source,
                destination,
                true,
                true,
                true,
                true,
                false,
                false,
                2);
            Check(ready.CanTravel,
                $"A valid restored route was disabled: '{ready.DisabledReason}'.",
                failures);
            Check(ready.ChimesaltCost == 2,
                $"Long route quoted {ready.ChimesaltCost} Chimesalt instead of two.",
                failures);

            var insufficient = TetherfallWorldMapRules.CreateTravelQuote(
                source,
                destination,
                true,
                true,
                true,
                true,
                false,
                false,
                1);
            Check(!insufficient.CanTravel &&
                  insufficient.DisabledReason == "Need 1 more Chimesalt.",
                "Insufficient Chimesalt did not return the exact disabled reason.",
                failures);

            var current = TetherfallWorldMapRules.CreateTravelQuote(
                source,
                source,
                true,
                true,
                true,
                true,
                false,
                false,
                9);
            Check(!current.CanTravel &&
                  current.DisabledReason == TetherfallWorldMapRules.CurrentDestinationDisabledReason,
                "The current Threadway was confirmable.",
                failures);

            var unrestored = TetherfallWorldMapRules.CreateTravelQuote(
                source,
                destination,
                true,
                true,
                true,
                false,
                false,
                false,
                9);
            Check(!unrestored.CanTravel &&
                  unrestored.DisabledReason == TetherfallWorldMapRules.DestinationNotRestoredDisabledReason,
                "An unrestored destination did not return the exact disabled reason.",
                failures);

            var sourceHostiles = TetherfallWorldMapRules.CreateTravelQuote(
                source,
                destination,
                true,
                true,
                true,
                true,
                true,
                false,
                9);
            Check(!sourceHostiles.CanTravel &&
                  sourceHostiles.DisabledReason == TetherfallWorldMapRules.SourceHostilesDisabledReason,
                "Source hostiles did not block confirmation with the exact reason.",
                failures);

            var destinationHostiles = TetherfallWorldMapRules.CreateTravelQuote(
                source,
                destination,
                true,
                true,
                true,
                true,
                false,
                true,
                9);
            Check(!destinationHostiles.CanTravel &&
                  destinationHostiles.DisabledReason == TetherfallWorldMapRules.DestinationHostilesDisabledReason,
                "Destination hostiles did not block confirmation with the exact reason.",
                failures);

            var nonfiniteDestination = new ThreadwayDefinition
            {
                id = "threadway.verifier.nonfinite",
                title = "Nonfinite verifier",
                position = Vector3.zero,
                arrivalPosition = new Vector3(float.PositiveInfinity, 0f, 0f)
            };
            var nonfinite = TetherfallWorldMapRules.CreateTravelQuote(
                source,
                nonfiniteDestination,
                true,
                true,
                true,
                true,
                false,
                false,
                9);
            Check(!nonfinite.CanTravel &&
                  nonfinite.DisabledReason == TetherfallWorldMapRules.NonFiniteArrivalDisabledReason,
                "A nonfinite arrival did not return the exact disabled reason.",
                failures);
        }

        private static void VerifyRuntimeSelectionDiscoveryAndArrival(List<string> failures)
        {
            var existingGames = UnityEngine.Object.FindObjectsByType<TetherfallGame>(
                FindObjectsInactive.Include);
            if (existingGames.Length > 0)
            {
                failures.Add(
                    "Runtime fixture checks need an empty Edit Mode scene with no TetherfallGame.");
                return;
            }

            GameObject fixture = null;
            GameObject floor = null;
            GameObject blocker = null;
            try
            {
                fixture = new GameObject("Threadway World Map Verification Fixture");
                var game = fixture.AddComponent<TetherfallGame>();
                game.OpenWorldMapUi();

                const string destinationId = "threadway.ashshelf";
                Check(game.SelectWorldMapMarker(destinationId),
                    "A catalogued marker could not be selected through the public API.",
                    failures);
                Check(game.SelectedWorldMapMarkerId == destinationId,
                    "Marker selection did not preserve the selected stable ID.",
                    failures);
                Check(!game.CanConfirmWorldMapTravel,
                    "Marker selection alone enabled confirmation without a current Wayfinder source.",
                    failures);
                Check(!game.ConfirmWorldMapTravel(),
                    "Selection alone executed travel without a valid explicit confirmation quote.",
                    failures);
                Check(game.CancelWorldMapSelection(),
                    "The first selection cancel was not consumed.",
                    failures);
                Check(!game.CancelWorldMapSelection(),
                    "Selection cancel was not idempotent.",
                    failures);

                var unlockedField = typeof(TetherfallGame).GetField(
                    "unlockedThreadwayIds",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var unlockedIds = unlockedField?.GetValue(game) as HashSet<string>;
                Check(unlockedIds != null,
                    "Verifier could not inspect restored Threadway IDs.",
                    failures);
                unlockedIds?.Add("threadway.cinderstation");
                var seededIds = new List<string>();
                game.CaptureDiscoveredThreadwayIds(seededIds);
                Check(seededIds.Count == 1 &&
                      seededIds[0] == "threadway.cinderstation",
                    "Discovery capture did not include a newly restored Threadway.",
                    failures);

                var restoredIds = new[]
                {
                    "threadway.veyrwater",
                    "threadway.missing",
                    null,
                    "threadway.veyrwater"
                };
                game.RestoreDiscoveredThreadwayIds(restoredIds);
                var capturedIds = new List<string>();
                game.CaptureDiscoveredThreadwayIds(capturedIds);
                Check(capturedIds.Count == 2,
                    $"Discovery restore captured {capturedIds.Count} IDs instead of two valid IDs.",
                    failures);
                Check(capturedIds.Count == 2 &&
                      capturedIds[0] == "threadway.cinderstation" &&
                      capturedIds[1] == "threadway.veyrwater",
                    "Discovery capture was not sorted or did not seed restored Threadways.",
                    failures);
                game.RestoreDiscoveredThreadwayIds(capturedIds);
                capturedIds.Clear();
                game.CaptureDiscoveredThreadwayIds(capturedIds);
                Check(capturedIds.Count == 2,
                    "Discovery restore was not idempotent.",
                    failures);

                Check(!game.TryFindSafeWorldMapArrival(
                        new Vector3(float.NaN, 0f, 0f),
                        out _,
                        out var nonfiniteReason) &&
                      nonfiniteReason == TetherfallWorldMapRules.NonFiniteArrivalDisabledReason,
                    "Safe-arrival validation accepted a nonfinite destination.",
                    failures);

                floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "World Map Verification Floor";
                floor.transform.position = new Vector3(0f, -0.5f, 0f);
                floor.transform.localScale = new Vector3(30f, 1f, 30f);
                Physics.SyncTransforms();

                Check(game.TryFindSafeWorldMapArrival(
                        new Vector3(5f, 0.05f, 5f),
                        out var clearArrival,
                        out var clearReason),
                    $"A clear finite arrival was rejected: '{clearReason}'.",
                    failures);
                Check(TetherfallWorldMapRules.IsFinite(clearArrival),
                    "A clear arrival returned a nonfinite fallback.",
                    failures);

                blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blocker.name = "World Map Verification Local Blocker";
                blocker.transform.position = new Vector3(0f, 0.75f, 0f);
                blocker.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                Physics.SyncTransforms();
                Check(game.TryFindSafeWorldMapArrival(
                        new Vector3(0f, 0.05f, 0f),
                        out var fallbackArrival,
                        out var fallbackReason),
                    $"Bounded radial fallback did not find clear ground: '{fallbackReason}'.",
                    failures);
                Check(TetherfallWorldMapRules.HorizontalDistance(
                          Vector3.zero,
                          fallbackArrival) > 0.5f,
                    "A blocked direct arrival did not move to a radial fallback.",
                    failures);

                UnityEngine.Object.DestroyImmediate(blocker);
                blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blocker.name = "World Map Verification Bounded Blocker";
                blocker.transform.position = new Vector3(0f, 1f, 0f);
                blocker.transform.localScale = new Vector3(16f, 2f, 16f);
                Physics.SyncTransforms();
                Check(!game.TryFindSafeWorldMapArrival(
                        new Vector3(0f, 0.05f, 0f),
                        out _,
                        out var blockedReason) &&
                      blockedReason == "No clear arrival space was found near this Threadway.",
                    "A fully blocked bounded search did not reject the arrival.",
                    failures);

                Check(!game.TryFindSafeWorldMapArrival(
                        new Vector3(230f, 0.05f, 120f),
                        out _,
                        out var voidReason) &&
                      voidReason == "The destination opens over unstable ground.",
                    "A void arrival did not return the unstable-ground reason.",
                    failures);
            }
            finally
            {
                if (blocker != null)
                {
                    UnityEngine.Object.DestroyImmediate(blocker);
                }

                if (floor != null)
                {
                    UnityEngine.Object.DestroyImmediate(floor);
                }

                if (fixture != null)
                {
                    UnityEngine.Object.DestroyImmediate(fixture);
                }

                var resetStatics = typeof(TetherfallGame).GetMethod(
                    "ResetStatics",
                    BindingFlags.Static | BindingFlags.NonPublic);
                resetStatics?.Invoke(null, null);
            }
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.01f;
        }

        private static void Check(bool condition, string message, List<string> failures)
        {
            if (!condition)
            {
                failures.Add(message);
            }
        }
    }
}
