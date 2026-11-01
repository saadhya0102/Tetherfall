using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallRegionalAnnexesVerification
    {
        private const string MenuPath = "Tetherfall/Verify Regional Frontier Annexes";
        private static int assertionCount;

        [MenuItem(MenuPath, false, 2208)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Regional-annex verification is edit-mode only. Stop Play Mode and run it again.");
            }

            assertionCount = 0;
            var failures = new List<string>();
            var parents = CreateParentRecords();
            VerifyCatalogShape(parents, failures);
            VerifyDefinitions(parents, failures);
            VerifyNonOverlap(parents, failures);
            VerifyStreamingAndBudgets(failures, out var maximumBudget);
            VerifyProjectionAndResolution(failures);
            VerifyDocumentation(failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Tetherfall regional frontier annex verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            Debug.Log(
                $"Tetherfall regional frontier annex verification passed: {assertionCount} assertions; " +
                $"{TetherfallRegionalAnnexes.All.Count} parents each have one equal-area annex; " +
                $"per-annex conservative maxima {maximumBudget.renderers} renderers, " +
                $"{maximumBudget.colliders} colliders, {maximumBudget.lights} lights, " +
                $"{maximumBudget.activeUpdateDrivers} active update drivers; " +
                $"{TetherfallRegionalAnnexes.MaxActiveStaticRoots} static and " +
                $"{TetherfallRegionalAnnexes.MaxActiveDetailRoots} detail roots maximum.");
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        private static void VerifyCatalogShape(
            List<ParentRecord> parents,
            List<string> failures)
        {
            Check(
                parents.Count == TetherfallRegionalAnnexes.ParentRegionCount,
                $"Expected {TetherfallRegionalAnnexes.ParentRegionCount} current parents, " +
                $"found {parents.Count}.",
                failures);
            Check(
                TetherfallRegionalAnnexes.All.Count == TetherfallRegionalAnnexes.AnnexCount,
                $"Expected {TetherfallRegionalAnnexes.AnnexCount} annexes, " +
                $"found {TetherfallRegionalAnnexes.All.Count}.",
                failures);

            var parentIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parent in parents)
            {
                Check(parentIds.Add(parent.Id),
                    $"Parent region ID '{parent.Id}' is duplicated in verifier authority.",
                    failures);
            }

            var annexCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var definition in TetherfallRegionalAnnexes.All)
            {
                if (definition == null)
                {
                    Check(false, "Regional-annex registry contains a null definition.", failures);
                    continue;
                }

                annexCounts.TryGetValue(definition.parentRegionId, out var count);
                annexCounts[definition.parentRegionId] = count + 1;
            }

            foreach (var parent in parents)
            {
                annexCounts.TryGetValue(parent.Id, out var count);
                Check(count == 1,
                    $"Parent '{parent.DisplayName}' has {count} annex definitions; expected exactly one.",
                    failures);
            }

            foreach (var pair in annexCounts)
            {
                Check(parentIds.Contains(pair.Key),
                    $"Annex registry references unknown parent region '{pair.Key}'.",
                    failures);
            }

            var parentArea = 0f;
            foreach (var parent in parents)
            {
                parentArea += parent.Bounds.width * parent.Bounds.height;
            }

            var annexArea = 0f;
            foreach (var definition in TetherfallRegionalAnnexes.All)
            {
                if (definition != null)
                {
                    annexArea += definition.Area;
                }
            }

            Check(Approximately(parentArea, 320800f) &&
                  Approximately(annexArea, 320800f) &&
                  Approximately(parentArea + annexArea, 641600f),
                $"Regional area totals drifted to parent {parentArea:0.##}, annex " +
                $"{annexArea:0.##}, combined {parentArea + annexArea:0.##}.",
                failures);
        }

        private static void VerifyDefinitions(
            List<ParentRecord> parents,
            List<string> failures)
        {
            var stableIds = CreateReleasedStableIds();
            var landmarkStyles = new HashSet<RegionalAnnexLandmarkStyle>();
            foreach (var definition in TetherfallRegionalAnnexes.All)
            {
                if (definition == null)
                {
                    continue;
                }

                var parent = parents.Find(item =>
                    string.Equals(item.Id, definition.parentRegionId, StringComparison.Ordinal));
                Check(parent != null,
                    $"Annex '{definition.id}' has no current parent authority.",
                    failures);
                if (parent == null)
                {
                    continue;
                }

                Check(IsSemanticId(definition.id, "annex."),
                    $"Annex ID '{definition.id}' is not a semantic stable ID.",
                    failures);
                Check(stableIds.Add(definition.id),
                    $"Stable ID '{definition.id}' is duplicated.",
                    failures);
                Check(string.Equals(
                          definition.parentDisplayName,
                          parent.DisplayName,
                          StringComparison.Ordinal),
                    $"Annex '{definition.id}' resolves to '{definition.parentDisplayName}', " +
                    $"not parent display name '{parent.DisplayName}'.",
                    failures);
                Check(RectApproximately(definition.parentBounds, parent.Bounds),
                    $"Annex '{definition.id}' records stale parent bounds {definition.parentBounds}; " +
                    $"current bounds are {parent.Bounds}.",
                    failures);
                Check(Approximately(definition.Area, parent.Bounds.width * parent.Bounds.height),
                    $"Annex '{definition.id}' area {definition.Area:0.##} does not equal parent area " +
                    $"{parent.Bounds.width * parent.Bounds.height:0.##}.",
                    failures);
                Check(Approximately(definition.size.x, parent.Bounds.width) &&
                      Approximately(definition.size.y, parent.Bounds.height),
                    $"Annex '{definition.id}' dimensions {definition.size.x:0.##}x" +
                    $"{definition.size.y:0.##} do not preserve parent dimensions " +
                    $"{parent.Bounds.width:0.##}x{parent.Bounds.height:0.##}.",
                    failures);
                Check(IsFinite(definition.center) &&
                      IsFinite(definition.size.x) &&
                      IsFinite(definition.size.y) &&
                      definition.size.x > 0f &&
                      definition.size.y > 0f,
                    $"Annex '{definition.id}' has nonfinite or non-positive bounds.",
                    failures);
                Check(BoundsInsideWorld(definition.WorldBounds),
                    $"Annex '{definition.id}' lies outside the declared world AABB.",
                    failures);
                Check(!string.IsNullOrWhiteSpace(definition.frontierName) &&
                      !string.IsNullOrWhiteSpace(definition.biomeBrief) &&
                      !string.IsNullOrWhiteSpace(definition.routeBrief),
                    $"Annex '{definition.id}' is missing authored frontier copy.",
                    failures);
                Check(landmarkStyles.Add(definition.landmarkStyle),
                    $"Annex '{definition.id}' repeats landmark silhouette " +
                    $"'{definition.landmarkStyle}'.",
                    failures);
                Check(!string.IsNullOrWhiteSpace(definition.landmarkName) &&
                      !string.IsNullOrWhiteSpace(definition.landmarkSubtitle) &&
                      LocalPointInside(definition, definition.landmarkLocalPosition),
                    $"Annex '{definition.id}' has incomplete or misplaced landmark metadata.",
                    failures);
                Check(definition.hasProceduralGroundVariation,
                    $"Annex '{definition.id}' has no procedural shared-material ground variation.",
                    failures);
                Check(definition.hasBoundedAtmosphereField,
                    $"Annex '{definition.id}' has no bounded atmosphere field.",
                    failures);
                Check(definition.routeDressingPieceCount >= 8,
                    $"Annex '{definition.id}' has insufficient environmental route dressing.",
                    failures);

                VerifyTerrain(definition, stableIds, failures);
                VerifyResources(definition, stableIds, failures);
                VerifySecretAndCamp(definition, stableIds, failures);
                VerifyConnector(definition, parents, failures);
                VerifyThreadway(definition, stableIds, failures);
                VerifyFiniteTransforms(definition, failures);
            }

            Check(
                landmarkStyles.Count == TetherfallRegionalAnnexes.AnnexCount,
                "The annex set does not provide thirteen distinct landmark silhouettes.",
                failures);
        }

        private static HashSet<string> CreateReleasedStableIds()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var threadway in ThreadwayCatalog.All)
            {
                if (threadway != null &&
                    !string.IsNullOrWhiteSpace(threadway.id) &&
                    !IsRegisteredAnnexThreadway(threadway))
                {
                    ids.Add(threadway.id);
                }
            }

            foreach (var pack in TetherfallRegionPacks.Playable)
            {
                if (pack == null)
                {
                    continue;
                }

                if (pack.threadway != null && !string.IsNullOrWhiteSpace(pack.threadway.id))
                {
                    ids.Add(pack.threadway.id);
                }

                if (!string.IsNullOrWhiteSpace(pack.secretId))
                {
                    ids.Add(pack.secretId);
                }

                if (!string.IsNullOrWhiteSpace(pack.encounterPersistenceId))
                {
                    ids.Add(pack.encounterPersistenceId);
                }
            }

            foreach (var cache in FieldCacheCatalog.All)
            {
                if (cache != null && !string.IsNullOrWhiteSpace(cache.id))
                {
                    ids.Add(cache.id);
                }
            }

            foreach (var placement in EncounterCatalog.ExtendedPlacements)
            {
                if (placement != null && !string.IsNullOrWhiteSpace(placement.persistenceId))
                {
                    ids.Add(placement.persistenceId);
                }
            }

            return ids;
        }

        private static bool IsRegisteredAnnexThreadway(ThreadwayDefinition threadway)
        {
            foreach (var definition in TetherfallRegionalAnnexes.All)
            {
                if (definition != null && ReferenceEquals(definition.threadway, threadway))
                {
                    return true;
                }
            }

            return false;
        }

        private static void VerifyTerrain(
            RegionalAnnexDefinition definition,
            HashSet<string> stableIds,
            List<string> failures)
        {
            Check(
                definition.terrainPieces != null &&
                definition.terrainPieces.Length >= TetherfallRegionalAnnexes.MinimumTerrainPieces,
                $"Annex '{definition.id}' needs at least " +
                $"{TetherfallRegionalAnnexes.MinimumTerrainPieces} terrain pieces.",
                failures);
            if (definition.terrainPieces == null)
            {
                return;
            }

            var hasRamp = false;
            var hasClimb = false;
            var maximumSolidTop = float.NegativeInfinity;
            var minimumRaisedTop = float.PositiveInfinity;
            foreach (var piece in definition.terrainPieces)
            {
                Check(piece != null,
                    $"Annex '{definition.id}' contains a null terrain piece.",
                    failures);
                if (piece == null)
                {
                    continue;
                }

                Check(IsSemanticId(piece.id, "terrain.annex."),
                    $"Terrain ID '{piece.id}' is not semantic.",
                    failures);
                Check(stableIds.Add(piece.id),
                    $"Stable ID '{piece.id}' is duplicated.",
                    failures);
                Check(!string.IsNullOrWhiteSpace(piece.displayName),
                    $"Terrain '{piece.id}' has no authored display name.",
                    failures);
                Check(IsFinite(piece.localPosition) &&
                      IsFinite(piece.scale) &&
                      IsFinite(piece.eulerAngles) &&
                      piece.scale.x > 0f &&
                      piece.scale.y > 0f &&
                      piece.scale.z > 0f,
                    $"Terrain '{piece.id}' has invalid transform data.",
                    failures);
                Check(LocalPointInside(definition, piece.localPosition),
                    $"Terrain '{piece.id}' lies outside annex '{definition.id}'.",
                    failures);
                hasRamp |= piece.solid &&
                           piece.isRamp &&
                           (Mathf.Abs(NormalizeAngle(piece.eulerAngles.x)) >= 5f ||
                            Mathf.Abs(NormalizeAngle(piece.eulerAngles.z)) >= 5f);
                hasClimb |= piece.solid && piece.climbable;
                if (piece.solid)
                {
                    var top = piece.localPosition.y + piece.scale.y * 0.5f;
                    maximumSolidTop = Mathf.Max(maximumSolidTop, top);
                    if (top > 0.25f)
                    {
                        minimumRaisedTop = Mathf.Min(minimumRaisedTop, top);
                    }
                }
            }

            Check(hasRamp,
                $"Annex '{definition.id}' has no authored solid ramp.",
                failures);
            Check(hasClimb,
                $"Annex '{definition.id}' has no authored climb route.",
                failures);
            Check(maximumSolidTop >= 6f,
                $"Annex '{definition.id}' has insufficient vertical relief.",
                failures);
            Check(minimumRaisedTop < float.PositiveInfinity &&
                  maximumSolidTop - minimumRaisedTop >= 3f,
                $"Annex '{definition.id}' does not span at least three units of raised relief.",
                failures);
        }

        private static void VerifyResources(
            RegionalAnnexDefinition definition,
            HashSet<string> stableIds,
            List<string> failures)
        {
            Check(
                definition.resources != null &&
                definition.resources.Length >= TetherfallRegionalAnnexes.MinimumResourceNodes,
                $"Annex '{definition.id}' needs at least " +
                $"{TetherfallRegionalAnnexes.MinimumResourceNodes} explicit-ID resource nodes.",
                failures);
            if (definition.resources == null)
            {
                return;
            }

            var kinds = new HashSet<ResourceKind>();
            var positions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var placement in definition.resources)
            {
                Check(placement != null,
                    $"Annex '{definition.id}' contains a null resource placement.",
                    failures);
                if (placement == null)
                {
                    continue;
                }

                Check(IsSemanticId(placement.persistenceId, "resource.annex.") &&
                      placement.persistenceId.IndexOf('@') < 0 &&
                      placement.persistenceId.IndexOf(':') < 0,
                    $"Resource ID '{placement.persistenceId}' is missing the explicit semantic " +
                    "PersistenceId path or is coordinate-derived.",
                    failures);
                Check(stableIds.Add(placement.persistenceId),
                    $"Stable ID '{placement.persistenceId}' is duplicated.",
                    failures);
                Check(Enum.IsDefined(typeof(ResourceKind), placement.kind),
                    $"Resource '{placement.persistenceId}' has invalid kind '{placement.kind}'.",
                    failures);
                Check(IsFinite(placement.localPosition) &&
                      LocalPointInside(definition, placement.localPosition),
                    $"Resource '{placement.persistenceId}' lies outside '{definition.id}'.",
                    failures);
                kinds.Add(placement.kind);
                var positionKey =
                    $"{Mathf.RoundToInt(placement.localPosition.x * 10f)}:" +
                    $"{Mathf.RoundToInt(placement.localPosition.z * 10f)}";
                Check(positions.Add(positionKey),
                    $"Annex '{definition.id}' overlaps resource placements at {positionKey}.",
                    failures);
            }

            Check(kinds.Count >= 3,
                $"Annex '{definition.id}' uses only {kinds.Count} resource kinds.",
                failures);
        }

        private static void VerifySecretAndCamp(
            RegionalAnnexDefinition definition,
            HashSet<string> stableIds,
            List<string> failures)
        {
            Check(IsSemanticId(definition.secretId, "secret.annex."),
                $"Annex '{definition.id}' has no semantic secret persistence ID.",
                failures);
            Check(stableIds.Add(definition.secretId),
                $"Stable ID '{definition.secretId}' is duplicated.",
                failures);
            Check(!string.IsNullOrWhiteSpace(definition.secretName) &&
                  !string.IsNullOrWhiteSpace(definition.secretInscription) &&
                  LocalPointInside(definition, definition.secretLocalPosition) &&
                  definition.secretResourceAmount > 0,
                $"Annex '{definition.id}' has incomplete or misplaced stable cache content.",
                failures);
            var encounter = EncounterCatalog.Get(definition.encounterId);
            Check(encounter != null &&
                  string.Equals(encounter.id, definition.encounterId, StringComparison.Ordinal),
                $"Annex '{definition.id}' references missing encounter '{definition.encounterId}'.",
                failures);
            Check(IsSemanticId(definition.encounterPersistenceId, "encounter.annex.") &&
                  definition.encounterPersistenceId.IndexOf('@') < 0 &&
                  definition.encounterPersistenceId.IndexOf(':') < 0,
                $"Annex '{definition.id}' has a coordinate-derived or nonsemantic camp ID " +
                $"'{definition.encounterPersistenceId}'.",
                failures);
            Check(stableIds.Add(definition.encounterPersistenceId),
                $"Stable ID '{definition.encounterPersistenceId}' is duplicated.",
                failures);
            Check(LocalPointInside(definition, definition.encounterLocalPosition),
                $"Camp '{definition.encounterPersistenceId}' lies outside '{definition.id}'.",
                failures);
        }

        private static void VerifyConnector(
            RegionalAnnexDefinition definition,
            List<ParentRecord> parents,
            List<string> failures)
        {
            Check(definition.connectorPoints != null && definition.connectorPoints.Length >= 2,
                $"Annex '{definition.id}' has no deliberate connector route.",
                failures);
            if (definition.connectorPoints == null || definition.connectorPoints.Length < 2)
            {
                return;
            }

            Check(definition.connectorWidth >= 10f && definition.connectorWidth <= 16f,
                $"Annex '{definition.id}' connector width {definition.connectorWidth:0.##} " +
                "is outside 10..16.",
                failures);
            var start = definition.connectorPoints[0];
            var end = definition.connectorPoints[definition.connectorPoints.Length - 1];
            Check(RectContains(definition.parentBounds, start, 0.01f),
                $"Annex '{definition.id}' connector does not begin inside its parent bounds.",
                failures);
            Check(definition.Contains(end),
                $"Annex '{definition.id}' connector does not end inside the annex.",
                failures);

            for (var index = 0; index < definition.connectorPoints.Length - 1; index++)
            {
                var left = definition.connectorPoints[index];
                var right = definition.connectorPoints[index + 1];
                var length = HorizontalDistance(left, right);
                Check(IsFinite(left) &&
                      IsFinite(right) &&
                      Mathf.Abs(left.y) <= 0.01f &&
                      Mathf.Abs(right.y) <= 0.01f,
                    $"Annex '{definition.id}' connector segment {index + 1} is nonfinite " +
                    "or not aligned to its support floor.",
                    failures);
                Check(length >= 4f && length <= 112f,
                    $"Annex '{definition.id}' connector segment {index + 1} has unsupported " +
                    $"span {length:0.##}; expected 4..112.",
                    failures);
                var midpoint = Vector3.Lerp(left, right, 0.5f);
                Check(string.Equals(
                          TetherfallRegionPacks.ResolveRegionName(midpoint),
                          definition.parentDisplayName,
                          StringComparison.Ordinal),
                    $"Connector for '{definition.id}' does not retain parent region identity " +
                    $"on segment {index + 1}.",
                    failures);
                VerifyConnectorClearance(
                    definition,
                    parents,
                    left,
                    right,
                    index,
                    failures);
            }
        }

        private static void VerifyConnectorClearance(
            RegionalAnnexDefinition owner,
            List<ParentRecord> parents,
            Vector3 start,
            Vector3 end,
            int segmentIndex,
            List<string> failures)
        {
            var length = HorizontalDistance(start, end);
            var samples = Mathf.Max(1, Mathf.CeilToInt(length / 2f));
            for (var sampleIndex = 0; sampleIndex <= samples; sampleIndex++)
            {
                var point = Vector3.Lerp(start, end, sampleIndex / (float)samples);
                foreach (var parent in parents)
                {
                    if (!string.Equals(parent.Id, owner.parentRegionId, StringComparison.Ordinal))
                    {
                        Check(!RectContains(
                                  parent.Bounds,
                                  point,
                                  owner.connectorWidth * 0.5f),
                            $"Connector for '{owner.id}' crosses unrelated parent " +
                            $"'{parent.DisplayName}' on segment {segmentIndex + 1}.",
                            failures);
                    }
                }

                foreach (var annex in TetherfallRegionalAnnexes.All)
                {
                    if (annex != null && !ReferenceEquals(annex, owner))
                    {
                        Check(!annex.Contains(point, owner.connectorWidth * 0.5f),
                            $"Connector for '{owner.id}' crosses unrelated annex " +
                            $"'{annex.id}' on segment {segmentIndex + 1}.",
                            failures);
                    }
                }
            }
        }

        private static void VerifyThreadway(
            RegionalAnnexDefinition definition,
            HashSet<string> stableIds,
            List<string> failures)
        {
            var threadway = definition.threadway;
            Check(threadway != null,
                $"Annex '{definition.id}' has no Threadway.",
                failures);
            if (threadway == null)
            {
                return;
            }

            Check(IsSemanticId(threadway.id, "threadway.annex."),
                $"Threadway ID '{threadway.id}' is not semantic.",
                failures);
            Check(stableIds.Add(threadway.id),
                $"Stable ID '{threadway.id}' is duplicated.",
                failures);
            Check(string.Equals(threadway.region, definition.parentDisplayName, StringComparison.Ordinal),
                $"Threadway '{threadway.id}' region '{threadway.region}' does not resolve to " +
                $"parent '{definition.parentDisplayName}'.",
                failures);
            Check(!string.IsNullOrWhiteSpace(threadway.title) &&
                  !string.IsNullOrWhiteSpace(threadway.description),
                $"Threadway '{threadway.id}' has incomplete authored copy.",
                failures);
            Check(IsFinite(threadway.position) &&
                  IsFinite(threadway.arrivalPosition) &&
                  definition.Contains(threadway.position) &&
                  definition.Contains(threadway.arrivalPosition),
                $"Threadway '{threadway.id}' has nonfinite or out-of-annex transforms.",
                failures);
            var arrivalDistance = Vector3.Distance(
                threadway.position,
                threadway.arrivalPosition);
            Check(arrivalDistance >= 0.25f && arrivalDistance <= 25f,
                $"Threadway '{threadway.id}' arrival distance is {arrivalDistance:0.##}.",
                failures);
            Check(Mathf.Abs(threadway.arrivalPosition.y - 0.05f) <= 0.01f,
                $"Threadway '{threadway.id}' arrival is not aligned to its always-on pad.",
                failures);
            var connectorEnd = definition.connectorPoints[definition.connectorPoints.Length - 1];
            Check(HorizontalDistance(connectorEnd, threadway.arrivalPosition) <= 24f,
                $"Threadway '{threadway.id}' arrival is too far from connector support.",
                failures);
            Check(HorizontalDistance(
                      threadway.arrivalPosition,
                      definition.center + definition.encounterLocalPosition) >= 22f,
                $"Threadway '{threadway.id}' arrival is too close to its enemy camp.",
                failures);
            Check(HorizontalDistance(
                      threadway.arrivalPosition,
                      definition.center + definition.secretLocalPosition) >= 8f,
                $"Threadway '{threadway.id}' arrival is too close to its secret.",
                failures);
            VerifyArrivalTerrainClearance(definition, failures);
        }

        private static void VerifyArrivalTerrainClearance(
            RegionalAnnexDefinition definition,
            List<string> failures)
        {
            if (definition.terrainPieces == null || definition.threadway == null)
            {
                return;
            }

            var localArrival = definition.threadway.arrivalPosition - definition.center;
            foreach (var piece in definition.terrainPieces)
            {
                if (piece == null || !piece.solid)
                {
                    continue;
                }

                var bottom = piece.localPosition.y - piece.scale.y * 0.5f;
                var top = piece.localPosition.y + piece.scale.y * 0.5f;
                var lowObstacle = bottom < 2.1f && top > 0.05f;
                var horizontalRadius =
                    Mathf.Sqrt(piece.scale.x * piece.scale.x + piece.scale.z * piece.scale.z) * 0.5f;
                Check(!lowObstacle ||
                      HorizontalDistance(localArrival, piece.localPosition) > horizontalRadius + 1f,
                    $"Threadway '{definition.threadway.id}' arrival is obstructed by " +
                    $"terrain '{piece.id}'.",
                    failures);
            }
        }

        private static void VerifyFiniteTransforms(
            RegionalAnnexDefinition definition,
            List<string> failures)
        {
            Check(IsFinite(definition.landmarkLocalPosition) &&
                  IsFinite(definition.secretLocalPosition) &&
                  IsFinite(definition.encounterLocalPosition),
                $"Annex '{definition.id}' has a nonfinite authored content transform.",
                failures);
            if (definition.connectorPoints != null)
            {
                foreach (var point in definition.connectorPoints)
                {
                    Check(IsFinite(point),
                        $"Annex '{definition.id}' has a nonfinite connector point.",
                        failures);
                }
            }
        }

        private static void VerifyNonOverlap(
            List<ParentRecord> parents,
            List<string> failures)
        {
            var minimumX = float.PositiveInfinity;
            var maximumX = float.NegativeInfinity;
            var minimumZ = float.PositiveInfinity;
            var maximumZ = float.NegativeInfinity;
            for (var index = 0; index < TetherfallRegionalAnnexes.All.Count; index++)
            {
                var annex = TetherfallRegionalAnnexes.All[index];
                if (annex == null)
                {
                    continue;
                }

                minimumX = Mathf.Min(minimumX, annex.WorldBounds.xMin);
                maximumX = Mathf.Max(maximumX, annex.WorldBounds.xMax);
                minimumZ = Mathf.Min(minimumZ, annex.WorldBounds.yMin);
                maximumZ = Mathf.Max(maximumZ, annex.WorldBounds.yMax);
                foreach (var parent in parents)
                {
                    Check(!annex.WorldBounds.Overlaps(parent.Bounds),
                        $"Annex '{annex.id}' overlaps existing parent bounds " +
                        $"'{parent.DisplayName}'.",
                        failures);
                }

                for (var otherIndex = index + 1;
                     otherIndex < TetherfallRegionalAnnexes.All.Count;
                     otherIndex++)
                {
                    var other = TetherfallRegionalAnnexes.All[otherIndex];
                    if (other != null)
                    {
                        Check(!annex.WorldBounds.Overlaps(other.WorldBounds),
                            $"Annex bounds overlap: '{annex.id}' and '{other.id}'.",
                            failures);
                    }
                }
            }

            Check(Approximately(TetherfallRegionPacks.WorldMinimumX, minimumX - 11f) &&
                  Approximately(TetherfallRegionPacks.WorldMaximumX, maximumX + 11f) &&
                  Approximately(TetherfallRegionPacks.WorldMinimumZ, minimumZ - 11f) &&
                  Approximately(TetherfallRegionPacks.WorldMaximumZ, maximumZ + 11f),
                "World AABB must keep an exact eleven-unit buffer around the outer annex shells.",
                failures);
        }

        private static void VerifyStreamingAndBudgets(
            List<string> failures,
            out RegionalAnnexBudgetEstimate maximumBudget)
        {
            Check(TetherfallRegionalAnnexes.MaxActiveStaticRoots <= 4,
                "Annex streaming allows more than four static roots.",
                failures);
            Check(TetherfallRegionalAnnexes.MaxActiveDetailRoots <= 2,
                "Annex streaming allows more than two detail roots.",
                failures);
            Check(TetherfallRegionalAnnexes.StaticPrewakeDistance >= 120f,
                "Annex static prewake distance is not generous enough for connector approach.",
                failures);
            Check(TetherfallRegionalAnnexes.StaticHysteresisDistance >= 40f,
                "Annex static streaming lacks a generous hysteresis band.",
                failures);
            Check(
                TetherfallRegionalAnnexes.DetailPrewakeDistance >=
                TetherfallRegionalAnnexes.CampPopulationWakeDistance + 2f,
                "Annex detail roots can wake behind their enemy-camp population radius.",
                failures);
            Check(TetherfallRegionalAnnexes.DetailHysteresisDistance >= 40f,
                "Annex detail streaming does not retain enough distance for camp soft reset.",
                failures);
            Check(
                TetherfallRegionalAnnexes.DetailPrewakeDistance +
                TetherfallRegionalAnnexes.DetailHysteresisDistance >=
                TetherfallRegionalAnnexes.CampPopulationSoftResetDistance,
                "Annex detail streaming can deactivate before a distant camp soft reset.",
                failures);
            VerifyEnemyCampDistanceAuthorities(failures);
            Check(TetherfallRegionalAnnexes.ActivationRefreshSeconds >= 0.2f,
                "Annex activation polling is too frequent.",
                failures);
            Check(
                typeof(RegionalAnnexAtmosphereField).GetMethod(
                    "Update",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly) == null,
                "Bounded atmosphere fields must not run Update.",
                failures);
            Check(
                typeof(RegionalAnnexSecret).GetMethod(
                    "Update",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly) == null,
                "Annex secrets must not run Update.",
                failures);

            var maxRenderers = 0;
            var maxColliders = 0;
            var maxLights = 0;
            var maxUpdates = 0;
            for (var index = 0; index < TetherfallRegionalAnnexes.All.Count; index++)
            {
                var definition = TetherfallRegionalAnnexes.All[index];
                if (definition == null)
                {
                    continue;
                }

                var budget = TetherfallRegionalAnnexes.EstimateBudget(definition);
                Check(budget.renderers <= TetherfallRegionalAnnexes.MaxRenderersPerAnnex,
                    $"Annex '{definition.id}' estimates {budget.renderers} renderers; limit is " +
                    $"{TetherfallRegionalAnnexes.MaxRenderersPerAnnex}.",
                    failures);
                Check(budget.colliders <= TetherfallRegionalAnnexes.MaxCollidersPerAnnex,
                    $"Annex '{definition.id}' estimates {budget.colliders} colliders; limit is " +
                    $"{TetherfallRegionalAnnexes.MaxCollidersPerAnnex}.",
                    failures);
                Check(budget.lights <= TetherfallRegionalAnnexes.MaxLightsPerAnnex,
                    $"Annex '{definition.id}' estimates {budget.lights} lights; limit is " +
                    $"{TetherfallRegionalAnnexes.MaxLightsPerAnnex}.",
                    failures);
                Check(budget.activeUpdateDrivers <=
                      TetherfallRegionalAnnexes.MaxUpdateDriversPerAnnex,
                    $"Annex '{definition.id}' estimates {budget.activeUpdateDrivers} update drivers; " +
                    $"limit is {TetherfallRegionalAnnexes.MaxUpdateDriversPerAnnex}.",
                    failures);
                maxRenderers = Mathf.Max(maxRenderers, budget.renderers);
                maxColliders = Mathf.Max(maxColliders, budget.colliders);
                maxLights = Mathf.Max(maxLights, budget.lights);
                maxUpdates = Mathf.Max(maxUpdates, budget.activeUpdateDrivers);

                for (var otherIndex = index + 1;
                     otherIndex < TetherfallRegionalAnnexes.All.Count;
                     otherIndex++)
                {
                    var other = TetherfallRegionalAnnexes.All[otherIndex];
                    if (other == null)
                    {
                        continue;
                    }

                    var campDistance = HorizontalDistance(
                        definition.center + definition.encounterLocalPosition,
                        other.center + other.encounterLocalPosition);
                    Check(
                        campDistance >
                        TetherfallRegionalAnnexes.CampPopulationWakeDistance * 2f,
                        $"Annex camps '{definition.encounterPersistenceId}' and " +
                        $"'{other.encounterPersistenceId}' have overlapping wake discs.",
                        failures);
                }
            }

            maximumBudget = new RegionalAnnexBudgetEstimate(
                maxRenderers,
                maxColliders,
                maxLights,
                maxUpdates);
            Check(maximumBudget.renderers == 177 &&
                  maximumBudget.colliders == 53 &&
                  maximumBudget.lights == 2 &&
                  maximumBudget.activeUpdateDrivers == 15,
                $"Documented annex maximum drifted to {maximumBudget.renderers} renderers, " +
                $"{maximumBudget.colliders} colliders, {maximumBudget.lights} lights, " +
                $"{maximumBudget.activeUpdateDrivers} update drivers.",
                failures);
        }

        private static void VerifyEnemyCampDistanceAuthorities(List<string> failures)
        {
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var wakeField = typeof(EnemyCamp).GetField("WakeDistance", flags);
            var resetField = typeof(EnemyCamp).GetField("SoftResetDistance", flags);
            var populationField = typeof(EnemyCamp).GetField(
                "aliveEnemies",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Check(
                wakeField != null &&
                wakeField.GetRawConstantValue() is float wakeDistance &&
                Approximately(
                    wakeDistance,
                    TetherfallRegionalAnnexes.CampPopulationWakeDistance),
                "Annex streaming wake authority drifted from EnemyCamp.WakeDistance.",
                failures);
            Check(
                resetField != null &&
                resetField.GetRawConstantValue() is float resetDistance &&
                Approximately(
                    resetDistance,
                    TetherfallRegionalAnnexes.CampPopulationSoftResetDistance),
                "Annex streaming reset authority drifted from EnemyCamp.SoftResetDistance.",
                failures);
            Check(
                populationField != null &&
                populationField.FieldType == typeof(List<Enemy>),
                "Annex enemy ownership can no longer resolve EnemyCamp.aliveEnemies.",
                failures);
        }

        private static void VerifyProjectionAndResolution(List<string> failures)
        {
            var mapRect = new Rect(10f, 20f, 520f, 320f);
            foreach (var definition in TetherfallRegionalAnnexes.All)
            {
                if (definition == null)
                {
                    continue;
                }

                Check(string.Equals(
                          TetherfallRegionPacks.ResolveRegionName(definition.center),
                          definition.parentDisplayName,
                          StringComparison.Ordinal),
                    $"Annex '{definition.id}' does not resolve to parent display name " +
                    $"'{definition.parentDisplayName}'.",
                    failures);
                Check(TetherfallWorldMapRules.TryProjectWorldToMap(
                          new Vector3(
                              definition.WorldBounds.xMin,
                              0f,
                              definition.WorldBounds.yMin),
                          mapRect,
                          out var minimum) &&
                      mapRect.Contains(minimum),
                    $"Annex '{definition.id}' minimum map corner does not project.",
                    failures);
                Check(TetherfallWorldMapRules.TryProjectWorldToMap(
                          new Vector3(
                              definition.WorldBounds.xMax,
                              0f,
                              definition.WorldBounds.yMax),
                          mapRect,
                          out var maximum) &&
                      mapRect.Contains(maximum),
                    $"Annex '{definition.id}' maximum map corner does not project.",
                    failures);
            }
        }

        private static void VerifyDocumentation(List<string> failures)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var packsPath = Path.Combine(projectRoot, "REGION_PACKS.md");
            var authoringPath = Path.Combine(projectRoot, "CONTENT_AUTHORING.md");
            Check(File.Exists(packsPath), "REGION_PACKS.md is missing.", failures);
            Check(File.Exists(authoringPath), "CONTENT_AUTHORING.md is missing.", failures);
            if (File.Exists(packsPath))
            {
                var packs = File.ReadAllText(packsPath);
                Check(packs.Contains("TetherfallRegionalAnnexes.cs"),
                    "REGION_PACKS.md does not document the annex registry.",
                    failures);
                Check(packs.Contains("13 frontier annexes"),
                    "REGION_PACKS.md does not state the exact annex count.",
                    failures);
                Check(packs.Contains("X -601..581") && packs.Contains("Z -631..731"),
                    "REGION_PACKS.md does not state the expanded world bounds.",
                    failures);
                Check(packs.Contains("320,800 m²") && packs.Contains("641,600 m²"),
                    "REGION_PACKS.md does not state the exact doubled area totals.",
                    failures);
                Check(packs.Contains("177 renderers") &&
                      packs.Contains("53 colliders") &&
                      packs.Contains("15 active update drivers"),
                    "REGION_PACKS.md does not state the verified per-annex maxima.",
                    failures);
                foreach (var definition in TetherfallRegionalAnnexes.All)
                {
                    Check(packs.Contains(definition.frontierName),
                        $"REGION_PACKS.md does not account for '{definition.frontierName}'.",
                        failures);
                }
            }

            if (File.Exists(authoringPath))
            {
                var authoring = File.ReadAllText(authoringPath);
                Check(authoring.Contains("TetherfallRegionalAnnexes.cs"),
                    "CONTENT_AUTHORING.md does not document the annex seam.",
                    failures);
                Check(authoring.Contains("explicit `PersistenceId`"),
                    "CONTENT_AUTHORING.md does not require explicit resource persistence IDs.",
                    failures);
                Check(authoring.Contains("Verify Regional Frontier Annexes"),
                    "CONTENT_AUTHORING.md does not document the annex verifier.",
                    failures);
            }
        }

        private static List<ParentRecord> CreateParentRecords()
        {
            var parents = new List<ParentRecord>
            {
                new ParentRecord(
                    "region.alderwake",
                    "Alderwake Basin",
                    new Rect(-80f, -130f, 160f, 260f)),
                new ParentRecord(
                    "region.morrowfen",
                    "Morrowfen",
                    new Rect(80f, -130f, 160f, 260f)),
                new ParentRecord(
                    "region.cinderwold",
                    "Cinderwold Verge",
                    new Rect(-240f, -130f, 160f, 260f))
            };
            foreach (var definition in TetherfallRegionPacks.Playable)
            {
                if (definition != null)
                {
                    parents.Add(new ParentRecord(
                        definition.id,
                        definition.displayName,
                        definition.WorldBounds));
                }
            }

            return parents;
        }

        private static bool BoundsInsideWorld(Rect bounds)
        {
            return bounds.xMin >= TetherfallRegionPacks.WorldMinimumX &&
                   bounds.xMax <= TetherfallRegionPacks.WorldMaximumX &&
                   bounds.yMin >= TetherfallRegionPacks.WorldMinimumZ &&
                   bounds.yMax <= TetherfallRegionPacks.WorldMaximumZ;
        }

        private static bool LocalPointInside(
            RegionalAnnexDefinition definition,
            Vector3 point)
        {
            return Mathf.Abs(point.x) <= definition.size.x * 0.5f &&
                   Mathf.Abs(point.z) <= definition.size.y * 0.5f;
        }

        private static bool RectContains(Rect bounds, Vector3 point, float margin)
        {
            return point.x >= bounds.xMin - margin &&
                   point.x <= bounds.xMax + margin &&
                   point.z >= bounds.yMin - margin &&
                   point.z <= bounds.yMax + margin;
        }

        private static bool RectApproximately(Rect left, Rect right)
        {
            return Approximately(left.xMin, right.xMin) &&
                   Approximately(left.xMax, right.xMax) &&
                   Approximately(left.yMin, right.yMin) &&
                   Approximately(left.yMax, right.yMax);
        }

        private static bool IsSemanticId(string id, string prefix)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                !id.StartsWith(prefix, StringComparison.Ordinal) ||
                id != id.ToLowerInvariant())
            {
                return false;
            }

            foreach (var character in id)
            {
                if ((character < 'a' || character > 'z') &&
                    (character < '0' || character > '9') &&
                    character != '.' &&
                    character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right)
        {
            var deltaX = left.x - right.x;
            var deltaZ = left.z - right.z;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            return angle > 180f ? angle - 360f : angle < -180f ? angle + 360f : angle;
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.01f;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
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

        private sealed class ParentRecord
        {
            public string Id { get; }
            public string DisplayName { get; }
            public Rect Bounds { get; }

            public ParentRecord(string id, string displayName, Rect bounds)
            {
                Id = id;
                DisplayName = displayName;
                Bounds = bounds;
            }
        }
    }
}
