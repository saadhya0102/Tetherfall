using System;
using System.Collections.Generic;
using System.IO;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallRegionPacksVerification
    {
        private const string MenuPath = "Tetherfall/Verify Regional Expansion Packs";
        private static int assertionCount;

        [MenuItem(MenuPath, false, 2207)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Region-pack verification is edit-mode only. Stop Play Mode and run it again.");
            }

            assertionCount = 0;
            var failures = new List<string>();
            VerifyCatalogShape(failures);
            VerifyPlayablePacks(failures);
            VerifyNoAuthoringSlots(failures);
            var maximumConcurrentActive = VerifyBoundedActivation(failures);
            VerifyProjection(failures);
            VerifyDocumentation(failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Tetherfall regional expansion verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            var maximumBudget = GetMaximumBudget();
            Debug.Log(
                $"Tetherfall regional expansion verification passed: {assertionCount} assertions; " +
                $"{TetherfallRegionPacks.Playable.Count}/{TetherfallRegionPacks.AdditionalRegionTarget} " +
                "additional-region packs playable, zero authoring slots; " +
                $"per-pack conservative maxima {maximumBudget.renderers} renderers, " +
                $"{maximumBudget.colliders} colliders, {maximumBudget.lights} lights, " +
                $"{maximumBudget.activeUpdateDrivers} active update drivers; " +
                $"{maximumConcurrentActive} packs maximum concurrently active.");
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        private static void VerifyCatalogShape(List<string> failures)
        {
            Check(
                TetherfallRegionPacks.All.Count == TetherfallRegionPacks.AdditionalRegionTarget,
                $"Expected {TetherfallRegionPacks.AdditionalRegionTarget} additional-region catalog slots, " +
                $"found {TetherfallRegionPacks.All.Count}.",
                failures);
            Check(
                TetherfallRegionPacks.Playable.Count == TetherfallRegionPacks.PlayableRegionPackCount,
                $"Expected {TetherfallRegionPacks.PlayableRegionPackCount} playable packs, " +
                $"found {TetherfallRegionPacks.Playable.Count}.",
                failures);
            Check(
                TetherfallRegionPacks.Playable.Count == TetherfallRegionPacks.AdditionalRegionTarget,
                "Every additional-region catalog entry must be genuinely playable.",
                failures);
            Check(
                TetherfallRegionPacks.AuthoringSlotCount == 0 &&
                TetherfallRegionPacks.All.Count == TetherfallRegionPacks.Playable.Count,
                "The completed ten-pack catalog must not retain catalog-only authoring slots.",
                failures);

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in TetherfallRegionPacks.All)
            {
                Check(definition != null, "Region-pack catalog contains a null definition.", failures);
                if (definition == null)
                {
                    continue;
                }

                Check(!string.IsNullOrWhiteSpace(definition.id),
                    "A region-pack definition has no stable ID.", failures);
                Check(!string.IsNullOrWhiteSpace(definition.displayName),
                    $"Region-pack '{definition.id}' has no display name.", failures);
                Check(ids.Add(definition.id),
                    $"Region-pack ID '{definition.id}' is duplicated.", failures);
                Check(names.Add(definition.displayName),
                    $"Region-pack display name '{definition.displayName}' is duplicated.", failures);
                Check(!string.IsNullOrWhiteSpace(definition.biome),
                    $"Region-pack '{definition.id}' has no biome brief.", failures);
                Check(!string.IsNullOrWhiteSpace(definition.silhouette),
                    $"Region-pack '{definition.id}' has no silhouette brief.", failures);
                Check(!string.IsNullOrWhiteSpace(definition.authoringBrief),
                    $"Region-pack '{definition.id}' has no authoring brief.", failures);
            }
        }

        private static void VerifyPlayablePacks(List<string> failures)
        {
            var biomes = new HashSet<string>(StringComparer.Ordinal);
            var silhouettes = new HashSet<string>(StringComparer.Ordinal);
            var landmarkStyles = new HashSet<RegionPackLandmarkStyle>();
            var footprintStyles = new HashSet<RegionPackEncounterFootprint>();
            var threadwayIds = new HashSet<string>(StringComparer.Ordinal);
            var secretIds = new HashSet<string>(StringComparer.Ordinal);
            var persistenceIds = new HashSet<string>(StringComparer.Ordinal);
            var mapColors = new List<Color>();

            for (var index = 0; index < TetherfallRegionPacks.Playable.Count; index++)
            {
                var definition = TetherfallRegionPacks.Playable[index];
                Check(definition.status == RegionPackStatus.Playable,
                    $"Playable catalog entry '{definition.id}' is not marked Playable.", failures);
                Check(biomes.Add(definition.biome),
                    $"Playable pack '{definition.id}' repeats biome brief '{definition.biome}'.", failures);
                Check(silhouettes.Add(definition.silhouette),
                    $"Playable pack '{definition.id}' repeats silhouette brief '{definition.silhouette}'.", failures);
                Check(landmarkStyles.Add(definition.landmarkStyle),
                    $"Playable pack '{definition.id}' repeats landmark style '{definition.landmarkStyle}'.", failures);
                Check(footprintStyles.Add(definition.encounterFootprint),
                    $"Playable pack '{definition.id}' repeats encounter footprint " +
                    $"'{definition.encounterFootprint}'.",
                    failures);
                Check(!string.IsNullOrWhiteSpace(definition.discoveryText),
                    $"Playable pack '{definition.id}' has no discovery copy.", failures);
                Check(!string.IsNullOrWhiteSpace(definition.mapSummary),
                    $"Playable pack '{definition.id}' has no map summary.", failures);
                Check(!string.IsNullOrWhiteSpace(definition.traversalNote),
                    $"Playable pack '{definition.id}' has no traversal brief.", failures);
                Check(IsFinite(definition.center) &&
                      IsFinite(definition.size.x) &&
                      IsFinite(definition.size.y),
                    $"Playable pack '{definition.id}' has nonfinite bounds.", failures);
                Check(definition.size.x >= 100f && definition.size.y >= 100f,
                    $"Playable pack '{definition.id}' is too small to support a regional route.", failures);
                Check(definition.activationRadius >= 90f && definition.activationRadius <= 125f,
                    $"Playable pack '{definition.id}' activation radius " +
                    $"{definition.activationRadius:0.##} is outside the bounded 90..125 range.",
                    failures);
                Check(BoundsInsideWorld(definition.WorldBounds),
                    $"Playable pack '{definition.id}' lies outside the expanded world bounds.", failures);

                for (var colorIndex = 0; colorIndex < mapColors.Count; colorIndex++)
                {
                    Check(ColorDistance(definition.mapColor, mapColors[colorIndex]) >= 0.18f,
                        $"Playable pack '{definition.id}' does not have a sufficiently distinct map palette.",
                        failures);
                }

                mapColors.Add(definition.mapColor);
                VerifyTerrain(definition, failures);
                VerifyLandmarkAndSecret(definition, secretIds, failures);
                VerifyEcology(definition, failures);
                VerifyEncounter(definition, persistenceIds, failures);
                VerifyThreadway(definition, threadwayIds, failures);
                VerifyConnector(definition, failures);
                VerifyBudget(definition, failures);

                for (var otherIndex = index + 1;
                     otherIndex < TetherfallRegionPacks.Playable.Count;
                     otherIndex++)
                {
                    var other = TetherfallRegionPacks.Playable[otherIndex];
                    Check(!definition.WorldBounds.Overlaps(other.WorldBounds),
                        $"Playable region bounds overlap: '{definition.id}' and '{other.id}'.", failures);
                }
            }

            Check(landmarkStyles.Count == TetherfallRegionPacks.Playable.Count,
                "The expansion catalog does not provide a unique landmark silhouette per playable pack.",
                failures);
            Check(footprintStyles.Count == TetherfallRegionPacks.Playable.Count,
                "The expansion catalog does not provide a unique encounter footprint per playable pack.",
                failures);
        }

        private static void VerifyTerrain(RegionPackDefinition definition, List<string> failures)
        {
            Check(definition.terrainPieces != null && definition.terrainPieces.Length >= 8,
                $"Playable pack '{definition.id}' needs at least eight authored terrain pieces.", failures);
            if (definition.terrainPieces == null)
            {
                return;
            }

            var maximumTop = 0f;
            var minimumRaisedTop = float.MaxValue;
            var hasRamp = false;
            var hasSolidTraversal = false;
            var pieceIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var piece in definition.terrainPieces)
            {
                Check(piece != null, $"Playable pack '{definition.id}' contains a null terrain piece.", failures);
                if (piece == null)
                {
                    continue;
                }

                Check(!string.IsNullOrWhiteSpace(piece.id),
                    $"Playable pack '{definition.id}' has an unnamed terrain piece.", failures);
                Check(pieceIds.Add(piece.id),
                    $"Playable pack '{definition.id}' repeats terrain piece ID '{piece.id}'.", failures);
                Check(IsFinite(piece.localPosition) && IsFinite(piece.scale) && IsFinite(piece.eulerAngles),
                    $"Terrain piece '{piece.id}' in '{definition.id}' contains a nonfinite value.", failures);
                Check(piece.scale.x > 0f && piece.scale.y > 0f && piece.scale.z > 0f,
                    $"Terrain piece '{piece.id}' in '{definition.id}' has a non-positive scale.", failures);
                Check(LocalPointInside(definition, piece.localPosition),
                    $"Terrain piece '{piece.id}' in '{definition.id}' is outside its region bounds.", failures);

                if (!piece.solid)
                {
                    continue;
                }

                hasSolidTraversal = true;
                var top = piece.localPosition.y + piece.scale.y * 0.5f;
                maximumTop = Mathf.Max(maximumTop, top);
                if (top > 0.25f)
                {
                    minimumRaisedTop = Mathf.Min(minimumRaisedTop, top);
                }

                var pitch = Mathf.Abs(NormalizeAngle(piece.eulerAngles.x));
                var roll = Mathf.Abs(NormalizeAngle(piece.eulerAngles.z));
                if ((pitch >= 5f && pitch <= 35f) || (roll >= 5f && roll <= 35f))
                {
                    hasRamp = true;
                }
            }

            Check(hasSolidTraversal,
                $"Playable pack '{definition.id}' has no solid traversal terrain.", failures);
            Check(maximumTop >= 4f,
                $"Playable pack '{definition.id}' has insufficient vertical relief ({maximumTop:0.##}).", failures);
            Check(minimumRaisedTop < float.MaxValue && maximumTop - minimumRaisedTop >= 2.5f,
                $"Playable pack '{definition.id}' does not span at least 2.5 units of raised relief.", failures);
            Check(hasRamp,
                $"Playable pack '{definition.id}' has raised terrain but no authored ramp.", failures);
        }

        private static void VerifyLandmarkAndSecret(
            RegionPackDefinition definition,
            HashSet<string> secretIds,
            List<string> failures)
        {
            Check(!string.IsNullOrWhiteSpace(definition.landmarkName),
                $"Playable pack '{definition.id}' has no landmark name.", failures);
            Check(!string.IsNullOrWhiteSpace(definition.landmarkSubtitle),
                $"Playable pack '{definition.id}' has no landmark field note.", failures);
            Check(LocalPointInside(definition, definition.landmarkLocalPosition),
                $"Landmark for '{definition.id}' lies outside its region.", failures);
            Check(!string.IsNullOrWhiteSpace(definition.secretId),
                $"Playable pack '{definition.id}' has no secret ID.", failures);
            Check(secretIds.Add(definition.secretId),
                $"Secret ID '{definition.secretId}' is duplicated.", failures);
            Check(!string.IsNullOrWhiteSpace(definition.secretName),
                $"Playable pack '{definition.id}' has no secret name.", failures);
            Check(!string.IsNullOrWhiteSpace(definition.secretInscription),
                $"Playable pack '{definition.id}' has no secret inscription.", failures);
            Check(LocalPointInside(definition, definition.secretLocalPosition),
                $"Secret '{definition.secretId}' lies outside '{definition.id}'.", failures);
            Check(definition.secretResourceAmount > 0,
                $"Secret '{definition.secretId}' has no resource payoff.", failures);
        }

        private static void VerifyEcology(RegionPackDefinition definition, List<string> failures)
        {
            Check(definition.resources != null && definition.resources.Length >= 6,
                $"Playable pack '{definition.id}' needs at least six resource nodes.", failures);
            if (definition.resources == null)
            {
                return;
            }

            var kinds = new HashSet<ResourceKind>();
            var positions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var placement in definition.resources)
            {
                Check(placement != null,
                    $"Playable pack '{definition.id}' contains a null resource placement.", failures);
                if (placement == null)
                {
                    continue;
                }

                Check(Enum.IsDefined(typeof(ResourceKind), placement.kind),
                    $"Playable pack '{definition.id}' has invalid resource kind '{placement.kind}'.", failures);
                Check(IsFinite(placement.localPosition) &&
                      LocalPointInside(definition, placement.localPosition),
                    $"A '{placement.kind}' node lies outside playable pack '{definition.id}'.", failures);
                kinds.Add(placement.kind);
                var key =
                    $"{Mathf.RoundToInt(placement.localPosition.x * 10f)}:" +
                    $"{Mathf.RoundToInt(placement.localPosition.z * 10f)}";
                Check(positions.Add(key),
                    $"Playable pack '{definition.id}' has overlapping resource placements at {key}.", failures);
            }

            Check(kinds.Count >= 3,
                $"Playable pack '{definition.id}' needs at least three resource types; found {kinds.Count}.",
                failures);
        }

        private static void VerifyEncounter(
            RegionPackDefinition definition,
            HashSet<string> persistenceIds,
            List<string> failures)
        {
            Check(!string.IsNullOrWhiteSpace(definition.encounterId),
                $"Playable pack '{definition.id}' has no encounter definition.", failures);
            var encounter = EncounterCatalog.Get(definition.encounterId);
            Check(encounter != null &&
                  string.Equals(encounter.id, definition.encounterId, StringComparison.Ordinal),
                $"Playable pack '{definition.id}' references missing encounter " +
                $"'{definition.encounterId}'.",
                failures);
            Check(!string.IsNullOrWhiteSpace(definition.encounterPersistenceId),
                $"Playable pack '{definition.id}' has no stable encounter persistence ID.", failures);
            Check(persistenceIds.Add(definition.encounterPersistenceId),
                $"Encounter persistence ID '{definition.encounterPersistenceId}' is duplicated.", failures);
            Check(LocalPointInside(definition, definition.encounterLocalPosition),
                $"Encounter for '{definition.id}' lies outside its region.", failures);
            Check(Vector2.Distance(
                      new Vector2(definition.encounterLocalPosition.x, definition.encounterLocalPosition.z),
                      new Vector2(definition.threadway.position.x - definition.center.x,
                          definition.threadway.position.z - definition.center.z)) >= 22f,
                $"Encounter in '{definition.id}' occupies the Threadway arrival lane.", failures);
        }

        private static void VerifyThreadway(
            RegionPackDefinition definition,
            HashSet<string> ids,
            List<string> failures)
        {
            var threadway = definition.threadway;
            Check(threadway != null,
                $"Playable pack '{definition.id}' has no Threadway metadata.", failures);
            if (threadway == null)
            {
                return;
            }

            Check(!string.IsNullOrWhiteSpace(threadway.id) && ids.Add(threadway.id),
                $"Threadway ID '{threadway.id}' is empty or duplicated.", failures);
            Check(string.Equals(threadway.region, definition.displayName, StringComparison.Ordinal),
                $"Threadway '{threadway.id}' region '{threadway.region}' does not match " +
                $"'{definition.displayName}'.",
                failures);
            Check(!string.IsNullOrWhiteSpace(threadway.title) &&
                  !string.IsNullOrWhiteSpace(threadway.description),
                $"Threadway '{threadway.id}' is missing map copy.", failures);
            Check(definition.Contains(threadway.position) &&
                  definition.Contains(threadway.arrivalPosition),
                $"Threadway '{threadway.id}' lies outside '{definition.displayName}'.", failures);
            Check(IsFinite(threadway.position) && IsFinite(threadway.arrivalPosition),
                $"Threadway '{threadway.id}' contains a nonfinite position.", failures);
            var arrivalDistance = Vector3.Distance(threadway.position, threadway.arrivalPosition);
            Check(arrivalDistance >= 0.25f && arrivalDistance <= 25f,
                $"Threadway '{threadway.id}' arrival distance is {arrivalDistance:0.##}.", failures);
            Check(threadway.activationChimesaltCost >= 1 && threadway.activationChimesaltCost <= 3,
                $"Threadway '{threadway.id}' activation cost is outside 1..3.", failures);
            VerifyArrivalLane(definition, failures);
        }

        private static void VerifyConnector(RegionPackDefinition definition, List<string> failures)
        {
            Check(IsFinite(definition.connectorStart) && IsFinite(definition.connectorEnd),
                $"Playable pack '{definition.id}' has a nonfinite connector.", failures);
            Check(IsConnectorSourceSupported(definition),
                $"Connector for '{definition.id}' does not begin on the established route network.",
                failures);
            Check(definition.Contains(definition.connectorEnd, 3f),
                $"Connector for '{definition.id}' does not reach its region shell.", failures);
            var connectorSpan = Vector3.Distance(definition.connectorStart, definition.connectorEnd);
            Check(connectorSpan >= 12f && connectorSpan <= 80f,
                $"Connector for '{definition.id}' has an implausible span.", failures);
            Check(definition.connectorWidth >= 10f && definition.connectorWidth <= 18f,
                $"Connector for '{definition.id}' is outside the safe 10..18 width range.", failures);
            Check(Mathf.Abs(definition.connectorStart.y) <= 0.1f &&
                  Mathf.Abs(definition.connectorEnd.y) <= 0.1f,
                $"Connector for '{definition.id}' is not aligned to the always-on ground shell.",
                failures);
        }

        private static void VerifyBudget(RegionPackDefinition definition, List<string> failures)
        {
            var budget = TetherfallRegionPacks.EstimateBudget(definition);
            Check(budget.renderers <= TetherfallRegionPacks.MaxRenderersPerActivePack,
                $"Playable pack '{definition.id}' estimates {budget.renderers} renderers; budget is " +
                $"{TetherfallRegionPacks.MaxRenderersPerActivePack}.",
                failures);
            Check(budget.colliders <= TetherfallRegionPacks.MaxCollidersPerActivePack,
                $"Playable pack '{definition.id}' estimates {budget.colliders} colliders; budget is " +
                $"{TetherfallRegionPacks.MaxCollidersPerActivePack}.",
                failures);
            Check(budget.lights <= TetherfallRegionPacks.MaxLightsPerActivePack,
                $"Playable pack '{definition.id}' estimates {budget.lights} lights; budget is " +
                $"{TetherfallRegionPacks.MaxLightsPerActivePack}.",
                failures);
            Check(budget.activeUpdateDrivers <= TetherfallRegionPacks.MaxUpdateDriversPerActivePack,
                $"Playable pack '{definition.id}' estimates {budget.activeUpdateDrivers} update drivers; " +
                $"budget is {TetherfallRegionPacks.MaxUpdateDriversPerActivePack}.",
                failures);
        }

        private static void VerifyNoAuthoringSlots(List<string> failures)
        {
            var nonPlayableCount = 0;
            foreach (var definition in TetherfallRegionPacks.All)
            {
                if (definition != null && definition.status == RegionPackStatus.Playable)
                {
                    continue;
                }

                nonPlayableCount++;
            }

            Check(nonPlayableCount == 0,
                $"Found {nonPlayableCount} non-playable expansion entries; expected zero.",
                failures);
        }

        private static int VerifyBoundedActivation(List<string> failures)
        {
            var maximumConcurrent = 0;
            for (var z = TetherfallRegionPacks.WorldMinimumZ;
                 z <= TetherfallRegionPacks.WorldMaximumZ;
                 z += 5f)
            {
                for (var x = TetherfallRegionPacks.WorldMinimumX;
                     x <= TetherfallRegionPacks.WorldMaximumX;
                     x += 5f)
                {
                    var position = new Vector3(x, 0f, z);
                    var active = 0;
                    foreach (var definition in TetherfallRegionPacks.Playable)
                    {
                        var delta = position - definition.center;
                        delta.y = 0f;
                        if (delta.sqrMagnitude <=
                            definition.activationRadius * definition.activationRadius ||
                            definition.Contains(position, TetherfallRegionPacks.ActivationMargin))
                        {
                            active++;
                        }
                    }

                    maximumConcurrent = Mathf.Max(maximumConcurrent, active);
                }
            }

            Check(maximumConcurrent <= TetherfallRegionPacks.MaxSimultaneouslyActivePacks,
                $"Activation layout can enable {maximumConcurrent} packs at once; budget allows " +
                $"{TetherfallRegionPacks.MaxSimultaneouslyActivePacks}.",
                failures);
            Check(TetherfallRegionPacks.ActivationRefreshSeconds >= 0.2f,
                "Region-pack detail activation is being polled too frequently.", failures);
            return maximumConcurrent;
        }

        private static void VerifyProjection(List<string> failures)
        {
            var mapRect = new Rect(12f, 18f, 520f, 300f);
            foreach (var definition in TetherfallRegionPacks.Playable)
            {
                var bounds = definition.WorldBounds;
                Check(TetherfallWorldMapRules.TryProjectWorldToMap(
                          new Vector3(bounds.xMin, 0f, bounds.yMin),
                          mapRect,
                          out var minimum) &&
                      mapRect.Contains(minimum),
                    $"Minimum map corner for '{definition.id}' does not project into the map.", failures);
                Check(TetherfallWorldMapRules.TryProjectWorldToMap(
                          new Vector3(bounds.xMax, 0f, bounds.yMax),
                          mapRect,
                          out var maximum) &&
                      mapRect.Contains(maximum),
                    $"Maximum map corner for '{definition.id}' does not project into the map.", failures);
                Check(TetherfallWorldMapRules.TryProjectWorldToMap(
                          definition.threadway.position,
                          mapRect,
                          out var threadway) &&
                      mapRect.Contains(threadway),
                    $"Threadway for '{definition.id}' does not project into the map.", failures);
            }
        }

        private static void VerifyDocumentation(List<string> failures)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var readmePath = Path.Combine(projectRoot, "README.md");
            var authoringPath = Path.Combine(projectRoot, "CONTENT_AUTHORING.md");
            var regionPacksPath = Path.Combine(projectRoot, "REGION_PACKS.md");
            Check(File.Exists(readmePath), "README.md is missing.", failures);
            Check(File.Exists(authoringPath), "CONTENT_AUTHORING.md is missing.", failures);
            Check(File.Exists(regionPacksPath), "REGION_PACKS.md is missing.", failures);
            if (File.Exists(readmePath))
            {
                var readme = File.ReadAllText(readmePath);
                Check(readme.Contains("Thirteen distinct playable regions"),
                    "README.md does not state the exact thirteen-region playable count.", failures);
                Check(readme.Contains("all ten expansion packs are playable"),
                    "README.md does not state that all ten expansion packs are playable.", failures);
                Check(readme.Contains("Verify Regional Expansion Packs"),
                    "README.md does not document the focused region-pack verifier.", failures);
            }

            if (File.Exists(authoringPath))
            {
                var authoring = File.ReadAllText(authoringPath);
                Check(authoring.Contains("TetherfallRegionPacks.cs"),
                    "CONTENT_AUTHORING.md does not document the registry-first region-pack seam.", failures);
            }

            if (File.Exists(regionPacksPath))
            {
                var packs = File.ReadAllText(regionPacksPath);
                foreach (var definition in TetherfallRegionPacks.All)
                {
                    Check(packs.Contains(definition.displayName),
                        $"REGION_PACKS.md does not account for '{definition.displayName}'.", failures);
                }
            }
        }

        private static RegionPackBudgetEstimate GetMaximumBudget()
        {
            var maximumRenderers = 0;
            var maximumColliders = 0;
            var maximumLights = 0;
            var maximumUpdates = 0;
            foreach (var definition in TetherfallRegionPacks.Playable)
            {
                var budget = TetherfallRegionPacks.EstimateBudget(definition);
                maximumRenderers = Mathf.Max(maximumRenderers, budget.renderers);
                maximumColliders = Mathf.Max(maximumColliders, budget.colliders);
                maximumLights = Mathf.Max(maximumLights, budget.lights);
                maximumUpdates = Mathf.Max(maximumUpdates, budget.activeUpdateDrivers);
            }

            return new RegionPackBudgetEstimate(
                maximumRenderers,
                maximumColliders,
                maximumLights,
                maximumUpdates);
        }

        private static void VerifyArrivalLane(
            RegionPackDefinition definition,
            List<string> failures)
        {
            var threadway = definition.threadway;
            if (threadway == null)
            {
                return;
            }

            var arrival = threadway.arrivalPosition;
            var localArrival = arrival - definition.center;
            Check(Mathf.Abs(arrival.y - 0.05f) <= 0.1f,
                $"Threadway '{threadway.id}' arrival is not aligned to the flat ground shell.",
                failures);
            var connectorDistance = HorizontalDistance(arrival, definition.connectorEnd);
            Check(connectorDistance >= 12f && connectorDistance <= 32f,
                $"Threadway '{threadway.id}' arrival is {connectorDistance:0.##} units from its connector.",
                failures);
            Check(HorizontalDistance(
                      localArrival,
                      definition.encounterLocalPosition) >= 22f,
                $"Threadway '{threadway.id}' arrival is too close to its encounter.",
                failures);
            Check(HorizontalDistance(
                      localArrival,
                      definition.secretLocalPosition) >= 8f,
                $"Threadway '{threadway.id}' arrival is obstructed by its secret.",
                failures);

            if (definition.hasWater)
            {
                var waterHalfWidth = definition.waterSize.x * 0.5f + 1f;
                var waterHalfDepth = definition.waterSize.z * 0.5f + 1f;
                Check(Mathf.Abs(localArrival.x - definition.waterLocalPosition.x) > waterHalfWidth ||
                      Mathf.Abs(localArrival.z - definition.waterLocalPosition.z) > waterHalfDepth,
                    $"Threadway '{threadway.id}' arrival is inside its water volume.",
                    failures);
            }

            if (definition.hasWindLift)
            {
                var windHalfWidth = definition.windLiftSize.x * 0.5f + 1f;
                var windHalfDepth = definition.windLiftSize.z * 0.5f + 1f;
                Check(Mathf.Abs(localArrival.x - definition.windLiftLocalPosition.x) > windHalfWidth ||
                      Mathf.Abs(localArrival.z - definition.windLiftLocalPosition.z) > windHalfDepth,
                    $"Threadway '{threadway.id}' arrival is inside its wind-lift trigger.",
                    failures);
            }

            if (definition.terrainPieces == null)
            {
                return;
            }

            foreach (var piece in definition.terrainPieces)
            {
                if (piece == null || !piece.solid)
                {
                    continue;
                }

                var bottom = piece.localPosition.y - piece.scale.y * 0.5f;
                var top = piece.localPosition.y + piece.scale.y * 0.5f;
                var overlapsArrivalHeight = bottom < 2.1f && top > -0.1f;
                var overlapsArrivalFootprint =
                    Mathf.Abs(localArrival.x - piece.localPosition.x) <= piece.scale.x * 0.5f + 0.75f &&
                    Mathf.Abs(localArrival.z - piece.localPosition.z) <= piece.scale.z * 0.5f + 0.75f;
                Check(!overlapsArrivalHeight || !overlapsArrivalFootprint,
                    $"Threadway '{threadway.id}' arrival intersects low terrain '{piece.id}'.",
                    failures);
            }
        }

        private static bool IsConnectorSourceSupported(RegionPackDefinition definition)
        {
            var source = definition.connectorStart;
            if (Mathf.Abs(source.x) <= 240f && Mathf.Abs(source.z) <= 130f)
            {
                return true;
            }

            foreach (var other in TetherfallRegionPacks.Playable)
            {
                if (other != null &&
                    !ReferenceEquals(other, definition) &&
                    other.Contains(source, 3f))
                {
                    return true;
                }
            }

            return false;
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right)
        {
            var deltaX = left.x - right.x;
            var deltaZ = left.z - right.z;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        private static bool BoundsInsideWorld(Rect bounds)
        {
            return bounds.xMin >= TetherfallRegionPacks.WorldMinimumX &&
                   bounds.xMax <= TetherfallRegionPacks.WorldMaximumX &&
                   bounds.yMin >= TetherfallRegionPacks.WorldMinimumZ &&
                   bounds.yMax <= TetherfallRegionPacks.WorldMaximumZ;
        }

        private static bool LocalPointInside(RegionPackDefinition definition, Vector3 point)
        {
            return Mathf.Abs(point.x) <= definition.size.x * 0.5f &&
                   Mathf.Abs(point.z) <= definition.size.y * 0.5f;
        }

        private static float ColorDistance(Color left, Color right)
        {
            var red = left.r - right.r;
            var green = left.g - right.g;
            var blue = left.b - right.b;
            return Mathf.Sqrt(red * red + green * green + blue * blue);
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            return angle > 180f ? angle - 360f : angle < -180f ? angle + 360f : angle;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void Check(bool condition, string message, List<string> failures)
        {
            assertionCount++;
            if (!condition)
            {
                failures.Add(message);
            }
        }
    }
}
