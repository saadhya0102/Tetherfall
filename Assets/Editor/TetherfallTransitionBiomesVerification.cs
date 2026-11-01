using System;
using System.Collections.Generic;
using System.IO;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallTransitionBiomesVerification
    {
        private const string MenuPath = "Tetherfall/Verify Transition Biomes";
        private const float PositionTolerance = 0.01f;
        private static int assertionCount;

        [MenuItem(MenuPath, false, 2208)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Transition-biome verification is edit-mode only. Stop Play Mode and run it again.");
            }

            VerifyInternal(true);
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
            assertionCount = 0;
            var failures = new List<string>();
            VerifyCatalogShape(failures);
            VerifySemanticIds(failures);
            VerifyConnectorAlignment(failures);
            VerifyProjectionAndBlend(failures);
            VerifyLocaleIdentityAndHysteresis(failures);
            VerifyBudgetsAndStreaming(failures);
            VerifyMapBands(failures);
            VerifyStaticRuntimeContract(failures);
            VerifyDocumentation(failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Tetherfall transition-biome verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            if (logSuccess)
            {
                Debug.Log(
                    $"Tetherfall transition-biome verification passed: {assertionCount} assertions; " +
                    $"{TetherfallTransitionBiomes.V1Definitions.Count} immutable v1 seams, " +
                    "four semantic resources plus one camp and secret per seam, monotonic blends, " +
                    "parent-only progression identity, zero added lights, and bounded 3/2 streaming.");
            }
        }

        private static void VerifyCatalogShape(List<string> failures)
        {
            Check(
                TetherfallTransitionBiomes.V1Definitions.Count ==
                TetherfallTransitionBiomes.V1SeamCount,
                $"Expected exactly {TetherfallTransitionBiomes.V1SeamCount} v1 seams; found " +
                $"{TetherfallTransitionBiomes.V1Definitions.Count}.",
                failures);
            Check(
                TetherfallTransitionBiomes.V1SeamCount == 12,
                "The v1 seam contract must remain exactly 12.",
                failures);

            var expectedIds = new HashSet<string>(StringComparer.Ordinal)
            {
                "transition.alderwake.morrowfen",
                "transition.alderwake.cinderwold",
                "transition.alderwake.glassmere",
                "transition.morrowfen.rimecoil",
                "transition.cinderwold.sunken_weald",
                "transition.cinderwold.prismscar",
                "transition.morrowfen.galecradle",
                "transition.cinderwold.lumenwaste",
                "transition.morrowfen.brassroot",
                "transition.sunken_weald.cloudbarrow",
                "transition.rimecoil.nightloom",
                "transition.prismscar.tideglass"
            };
            var actualIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0;
                 index < TetherfallTransitionBiomes.V1Definitions.Count;
                 index++)
            {
                var definition = TetherfallTransitionBiomes.V1Definitions[index];
                Check(definition != null, $"V1 seam {index} is null.", failures);
                if (definition == null)
                {
                    continue;
                }

                Check(
                    actualIds.Add(definition.Id),
                    $"Transition ID '{definition.Id}' is duplicated.",
                    failures);
                Check(
                    expectedIds.Contains(definition.Id),
                    $"Unexpected v1 transition ID '{definition.Id}'.",
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(definition.AmbientLocaleName),
                    $"Transition '{definition.Id}' has no ambient locale name.",
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(definition.DiscoveryText),
                    $"Transition '{definition.Id}' has no ambient discovery copy.",
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(definition.ParentAProgressionRegion) &&
                    !string.IsNullOrWhiteSpace(definition.ParentBProgressionRegion),
                    $"Transition '{definition.Id}' has an empty parent progression identity.",
                    failures);
                Check(
                    !string.Equals(
                        definition.AmbientLocaleName,
                        definition.ParentAProgressionRegion,
                        StringComparison.Ordinal) &&
                    !string.Equals(
                        definition.AmbientLocaleName,
                        definition.ParentBProgressionRegion,
                        StringComparison.Ordinal),
                    $"Transition '{definition.Id}' reuses a progression-region name as its locale.",
                    failures);
                Check(
                    TetherfallTransitionBiomes.IsTransitionLocaleName(
                        definition.AmbientLocaleName),
                    $"Transition locale '{definition.AmbientLocaleName}' is not recognized.",
                    failures);
                VerifyExpectedParents(definition, failures);
            }

            Check(
                actualIds.SetEquals(expectedIds),
                "The v1 transition ID set does not match the completed 12-seam design.",
                failures);
        }

        private static void VerifySemanticIds(List<string> failures)
        {
            var allIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0;
                 index < TetherfallTransitionBiomes.V1Definitions.Count;
                 index++)
            {
                var definition = TetherfallTransitionBiomes.V1Definitions[index];
                if (definition == null)
                {
                    continue;
                }

                Check(
                    definition.Id.StartsWith("transition.", StringComparison.Ordinal) &&
                    allIds.Add(definition.Id),
                    $"Transition ID '{definition.Id}' is not stable and unique.",
                    failures);
                Check(
                    definition.ResourceCount == TetherfallTransitionBiomes.ResourcesPerSeam,
                    $"Transition '{definition.Id}' must have exactly " +
                    $"{TetherfallTransitionBiomes.ResourcesPerSeam} resources; found " +
                    $"{definition.ResourceCount}.",
                    failures);
                for (var resourceIndex = 0;
                     resourceIndex < definition.ResourceCount;
                     resourceIndex++)
                {
                    var resource = definition.GetResource(resourceIndex);
                    Check(
                        resource.Id.StartsWith(
                            "resource.transition.",
                            StringComparison.Ordinal) &&
                        resource.Id.StartsWith(
                            "resource." + definition.Id + ".",
                            StringComparison.Ordinal),
                        $"Resource '{resource.Id}' does not use resource.transition.*.",
                        failures);
                    Check(
                        allIds.Add(resource.Id),
                        $"Semantic resource ID '{resource.Id}' is duplicated.",
                        failures);
                    Check(
                        resource.Id.IndexOf('@') < 0 &&
                        resource.Id.IndexOf(':') < 0,
                        $"Resource '{resource.Id}' depends on coordinate identity.",
                        failures);
                    Check(
                        Enum.IsDefined(typeof(ResourceKind), resource.Kind) &&
                        resource.Amount > 0,
                        $"Resource '{resource.Id}' has invalid authored yield metadata.",
                        failures);
                }

                Check(
                    definition.EncounterPersistenceId.StartsWith(
                        "encounter.transition.",
                        StringComparison.Ordinal) &&
                    definition.EncounterPersistenceId.StartsWith(
                        "encounter." + definition.Id + ".",
                        StringComparison.Ordinal),
                    $"Camp '{definition.EncounterPersistenceId}' does not use encounter.transition.*.",
                    failures);
                Check(
                    allIds.Add(definition.EncounterPersistenceId),
                    $"Camp persistence ID '{definition.EncounterPersistenceId}' is duplicated.",
                    failures);
                Check(
                    definition.EncounterPersistenceId.IndexOf('@') < 0 &&
                    definition.EncounterPersistenceId.IndexOf(':') < 0,
                    $"Camp '{definition.EncounterPersistenceId}' depends on coordinate identity.",
                    failures);
                Check(
                    EncounterCatalog.Get(definition.EncounterDefinitionId) != null,
                    $"Transition '{definition.Id}' references missing encounter definition " +
                    $"'{definition.EncounterDefinitionId}'.",
                    failures);

                Check(
                    definition.SecretId.StartsWith(
                        "secret.transition.",
                        StringComparison.Ordinal) &&
                    definition.SecretId.StartsWith(
                        "secret." + definition.Id + ".",
                        StringComparison.Ordinal),
                    $"Secret '{definition.SecretId}' does not use secret.transition.*.",
                    failures);
                Check(
                    allIds.Add(definition.SecretId),
                    $"Transition secret ID '{definition.SecretId}' is duplicated.",
                    failures);
                Check(
                    definition.SecretId.IndexOf('@') < 0 &&
                    definition.SecretId.IndexOf(':') < 0,
                    $"Secret '{definition.SecretId}' depends on coordinate identity.",
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(definition.SecretName) &&
                    !string.IsNullOrWhiteSpace(definition.SecretInscription) &&
                    definition.SecretRewardAmount > 0,
                    $"Secret '{definition.SecretId}' lacks semantic content or reward.",
                    failures);
            }
        }

        private static void VerifyConnectorAlignment(List<string> failures)
        {
            var legacyCount = 0;
            var connectorCount = 0;
            for (var index = 0;
                 index < TetherfallTransitionBiomes.V1Definitions.Count;
                 index++)
            {
                var definition = TetherfallTransitionBiomes.V1Definitions[index];
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(definition.ConnectorRegionId))
                {
                    legacyCount++;
                    var expectedX = string.Equals(
                        definition.Id,
                        "transition.alderwake.morrowfen",
                        StringComparison.Ordinal)
                        ? 80f
                        : string.Equals(
                            definition.Id,
                            "transition.alderwake.cinderwold",
                            StringComparison.Ordinal)
                            ? -80f
                            : float.NaN;
                    Check(
                        IsFinite(expectedX),
                        $"Only the two legacy x=±80 seams may omit connector metadata; " +
                        $"'{definition.Id}' does.",
                        failures);
                    Check(
                        definition.BlendMode == TransitionBiomeBlendMode.AcrossSegment &&
                        Approximately(definition.SegmentStart.x, expectedX) &&
                        Approximately(definition.SegmentEnd.x, expectedX),
                        $"Legacy transition '{definition.Id}' is not aligned to x={expectedX}.",
                        failures);
                    Check(
                        Approximately(definition.SegmentStart.z, -100f) &&
                        Approximately(definition.SegmentEnd.z, 100f),
                        $"Legacy transition '{definition.Id}' does not span the authored border lane.",
                        failures);
                    continue;
                }

                connectorCount++;
                var pack = TetherfallRegionPacks.Get(definition.ConnectorRegionId);
                Check(
                    pack != null,
                    $"Transition '{definition.Id}' references unknown connector region " +
                    $"'{definition.ConnectorRegionId}'.",
                    failures);
                if (pack == null)
                {
                    continue;
                }

                Check(
                    definition.BlendMode == TransitionBiomeBlendMode.AlongSegment,
                    $"Connector transition '{definition.Id}' must blend along its segment.",
                    failures);
                Check(
                    HorizontalDistance(
                        definition.SegmentStart,
                        pack.connectorStart) <= PositionTolerance &&
                    HorizontalDistance(
                        definition.SegmentEnd,
                        pack.connectorEnd) <= PositionTolerance,
                    $"Transition '{definition.Id}' no longer matches the current " +
                    $"'{pack.id}' connector alignment.",
                    failures);
                Check(
                    Mathf.Abs(
                        definition.CorridorHalfWidth * 2f -
                        pack.connectorWidth) <= PositionTolerance,
                    $"Transition '{definition.Id}' width no longer matches connector " +
                    $"'{pack.id}'.",
                    failures);
            }

            Check(
                legacyCount == 2 && connectorCount == 10,
                $"Expected two legacy borders and ten connector-backed seams; found " +
                $"{legacyCount} legacy and {connectorCount} connector-backed.",
                failures);
        }

        private static void VerifyProjectionAndBlend(List<string> failures)
        {
            for (var definitionIndex = 0;
                 definitionIndex < TetherfallTransitionBiomes.V1Definitions.Count;
                 definitionIndex++)
            {
                var definition =
                    TetherfallTransitionBiomes.V1Definitions[definitionIndex];
                if (definition == null)
                {
                    continue;
                }

                var previousBlend = -1f;
                for (var sampleIndex = 0; sampleIndex <= 32; sampleIndex++)
                {
                    var sampleT = sampleIndex / 32f;
                    var position = GetBlendSample(definition, sampleT);
                    Check(
                        TetherfallTransitionBiomes.TryProject(
                            definition,
                            position,
                            out var projection),
                        $"Projection failed for '{definition.Id}' at t={sampleT:0.###}.",
                        failures);
                    if (!TetherfallTransitionBiomes.TryProject(
                            definition,
                            position,
                            out projection))
                    {
                        continue;
                    }

                    Check(
                        IsFinite(projection.BlendT) &&
                        projection.BlendT >= 0f &&
                        projection.BlendT <= 1f,
                        $"BlendT for '{definition.Id}' is nonfinite or out of range.",
                        failures);
                    Check(
                        projection.BlendT + 0.00001f >= previousBlend,
                        $"BlendT for '{definition.Id}' is not monotonic at " +
                        $"sample {sampleIndex}.",
                        failures);
                    previousBlend = projection.BlendT;
                }

                Check(
                    previousBlend >= 0.999f,
                    $"BlendT for '{definition.Id}' never reaches parent B.",
                    failures);
            }
        }

        private static void VerifyLocaleIdentityAndHysteresis(List<string> failures)
        {
            for (var index = 0;
                 index < TetherfallTransitionBiomes.V1Definitions.Count;
                 index++)
            {
                var definition = TetherfallTransitionBiomes.V1Definitions[index];
                if (definition == null)
                {
                    continue;
                }

                var state = default(TransitionLocaleHysteresisState);
                var core = GetBlendSample(definition, 0.5f);
                Check(
                    TetherfallTransitionBiomes.TryGetLocale(
                        core,
                        definition.AmbientLocaleName,
                        ref state,
                        out var locale),
                    $"Core locale query failed for '{definition.Id}'.",
                    failures);
                Check(
                    locale.TransitionId == definition.Id &&
                    locale.InSeamCore &&
                    locale.AmbientLocaleName == definition.AmbientLocaleName,
                    $"Core locale data is incomplete for '{definition.Id}'.",
                    failures);
                Check(
                    IsParentProgression(definition, locale.ProgressionRegion),
                    $"Transition '{definition.Id}' leaked locale " +
                    $"'{locale.ProgressionRegion}' into progression identity.",
                    failures);
                Check(
                    !TetherfallTransitionBiomes.IsTransitionLocaleName(
                        locale.ProgressionRegion),
                    $"Transition locale '{locale.ProgressionRegion}' was used as a progression ID.",
                    failures);

                state.Reset();
                Check(
                    TetherfallTransitionBiomes.TryGetLocale(
                        core,
                        definition.ParentAProgressionRegion,
                        ref state,
                        out locale) &&
                    locale.ProgressionRegion == definition.ParentAProgressionRegion,
                    $"Transition '{definition.Id}' does not preserve parent A progression identity.",
                    failures);
                state.Reset();
                Check(
                    TetherfallTransitionBiomes.TryGetLocale(
                        core,
                        definition.ParentBProgressionRegion,
                        ref state,
                        out locale) &&
                    locale.ProgressionRegion == definition.ParentBProgressionRegion,
                    $"Transition '{definition.Id}' does not preserve parent B progression identity.",
                    failures);
            }

            var hysteresisDefinition =
                TetherfallTransitionBiomes.Get("transition.alderwake.glassmere");
            Check(
                hysteresisDefinition != null,
                "Hysteresis fixture transition is missing.",
                failures);
            if (hysteresisDefinition == null)
            {
                return;
            }

            var hysteresisState = default(TransitionLocaleHysteresisState);
            var midpoint = GetBlendSample(hysteresisDefinition, 0.5f);
            Check(
                TetherfallTransitionBiomes.TryGetLocale(
                    midpoint,
                    hysteresisDefinition.ParentAProgressionRegion,
                    ref hysteresisState,
                    out var entered) &&
                hysteresisState.IsActive &&
                entered.AmbientLocaleName ==
                hysteresisDefinition.AmbientLocaleName,
                "Locale hysteresis does not enter in the seam core.",
                failures);

            var lingerPoint = GetBlendSample(hysteresisDefinition, 0.05f);
            Check(
                TetherfallTransitionBiomes.TryGetLocale(
                    lingerPoint,
                    hysteresisDefinition.ParentAProgressionRegion,
                    ref hysteresisState,
                    out var lingered) &&
                hysteresisState.IsActive &&
                !lingered.InSeamCore &&
                lingered.AmbientLocaleName ==
                hysteresisDefinition.AmbientLocaleName,
                "Locale hysteresis does not retain the ambient verge name after leaving the core.",
                failures);

            GetHorizontalFrame(hysteresisDefinition, out _, out var right);
            var justOutsideExit =
                midpoint +
                right *
                (hysteresisDefinition.CoreHalfWidth +
                 TetherfallTransitionBiomes.LocaleExitPadding +
                 0.5f);
            Check(
                TetherfallTransitionBiomes.TryGetLocale(
                    justOutsideExit,
                    hysteresisDefinition.ParentAProgressionRegion,
                    ref hysteresisState,
                    out var outsideExit) &&
                !hysteresisState.IsActive &&
                outsideExit.AmbientLocaleName ==
                hysteresisDefinition.ParentAProgressionRegion,
                "Locale hysteresis does not release immediately outside core + exit padding.",
                failures);

            hysteresisState.Reset();
            TetherfallTransitionBiomes.TryGetLocale(
                midpoint,
                hysteresisDefinition.ParentAProgressionRegion,
                ref hysteresisState,
                out _);
            var leavePoint =
                midpoint +
                right *
                (hysteresisDefinition.CorridorHalfWidth +
                 hysteresisDefinition.SoftFalloff +
                 TetherfallTransitionBiomes.LocaleExitPadding +
                 4f);
            var stillHasTransition = TetherfallTransitionBiomes.TryGetLocale(
                leavePoint,
                hysteresisDefinition.ParentAProgressionRegion,
                ref hysteresisState,
                out var left);
            Check(
                !stillHasTransition &&
                !hysteresisState.IsActive &&
                left.AmbientLocaleName ==
                hysteresisDefinition.ParentAProgressionRegion,
                "Locale hysteresis does not leave beyond the padded exit envelope.",
                failures);
            Check(
                TetherfallTransitionBiomes.ActivationRefreshSeconds >= 0.25f,
                "Transition polling is more frequent than the required 0.25-second cadence.",
                failures);
        }

        private static void VerifyBudgetsAndStreaming(List<string> failures)
        {
            Check(
                TetherfallTransitionBiomes.MaxActiveStaticShells <= 3,
                "Transition director may activate more than three static shells.",
                failures);
            Check(
                TetherfallTransitionBiomes.MaxActiveDetailRoots <= 2,
                "Transition director may activate more than two detail roots.",
                failures);
            Check(
                TetherfallTransitionBiomes.StaticPrewakeDistance <
                TetherfallTransitionBiomes.StaticSleepDistance,
                "Static-shell streaming has no enter/leave hysteresis.",
                failures);
            Check(
                TetherfallTransitionBiomes.DetailWakeDistance <
                TetherfallTransitionBiomes.DetailSleepDistance,
                "Detail-root streaming has no enter/leave hysteresis.",
                failures);
            Check(
                TetherfallTransitionBiomes.StaticPrewakeDistance >
                TetherfallTransitionBiomes.DetailWakeDistance,
                "Static corridor shells do not prewake ahead of detail ecology.",
                failures);

            for (var index = 0;
                 index < TetherfallTransitionBiomes.V1Definitions.Count;
                 index++)
            {
                var definition = TetherfallTransitionBiomes.V1Definitions[index];
                var budget = TetherfallTransitionBiomes.EstimateBudget(definition);
                Check(
                    budget.LandmarkDressingRenderers <=
                    TetherfallTransitionBiomes.MaxLandmarkDressingRenderers,
                    $"Transition '{definition.Id}' estimates " +
                    $"{budget.LandmarkDressingRenderers} landmark/dressing renderers; cap is " +
                    $"{TetherfallTransitionBiomes.MaxLandmarkDressingRenderers}.",
                    failures);
                Check(
                    budget.SolidRouteColliders <=
                    TetherfallTransitionBiomes.MaxSolidRouteColliders,
                    $"Transition '{definition.Id}' estimates " +
                    $"{budget.SolidRouteColliders} solid route colliders; cap is " +
                    $"{TetherfallTransitionBiomes.MaxSolidRouteColliders}.",
                    failures);
                Check(
                    budget.ResourceInteractables ==
                    TetherfallTransitionBiomes.ResourcesPerSeam,
                    $"Transition '{definition.Id}' budget does not contain exactly four resources.",
                    failures);
                Check(
                    budget.Camps == 1 && budget.Secrets == 1,
                    $"Transition '{definition.Id}' must contain one camp and one secret.",
                    failures);
                Check(
                    budget.AddedLights == 0,
                    $"Transition '{definition.Id}' adds lights.",
                    failures);
                Check(
                    definition.EncounterDefinitionId.StartsWith(
                        "encounter.wild.",
                        StringComparison.Ordinal),
                    $"V1 transition '{definition.Id}' must reuse a zero-light wild encounter.",
                    failures);
            }
        }

        private static void VerifyMapBands(List<string> failures)
        {
            var mapRect = new Rect(12f, 18f, 720f, 420f);
            Check(
                TetherfallTransitionBiomes.MapSeamBandCount ==
                TetherfallTransitionBiomes.DefinitionCount *
                TetherfallTransitionBiomes.MapBandsPerSeam,
                "Soft map-band count does not match the complete transition catalog.",
                failures);
            Check(
                TetherfallTransitionBiomes.MapSeamBandCount >=
                TetherfallTransitionBiomes.V1Definitions.Count *
                TetherfallTransitionBiomes.MapBandsPerSeam,
                "Soft map bands do not cover every immutable v1 seam.",
                failures);

            for (var definitionIndex = 0;
                 definitionIndex < TetherfallTransitionBiomes.V1Definitions.Count;
                 definitionIndex++)
            {
                var definition =
                    TetherfallTransitionBiomes.V1Definitions[definitionIndex];
                Check(
                    TetherfallWorldMapRules.TryProjectWorldToMap(
                        definition.SegmentStart,
                        mapRect,
                        out var start) &&
                    IsFinite(start.x) &&
                    IsFinite(start.y) &&
                    mapRect.Contains(start),
                    $"Map start for '{definition.Id}' is not projectable.",
                    failures);
                Check(
                    TetherfallWorldMapRules.TryProjectWorldToMap(
                        definition.SegmentEnd,
                        mapRect,
                        out var end) &&
                    IsFinite(end.x) &&
                    IsFinite(end.y) &&
                    mapRect.Contains(end),
                    $"Map end for '{definition.Id}' is not projectable.",
                    failures);

                var previousThickness = float.PositiveInfinity;
                for (var layer = 0;
                     layer < TetherfallTransitionBiomes.MapBandsPerSeam;
                     layer++)
                {
                    var bandIndex =
                        definitionIndex *
                        TetherfallTransitionBiomes.MapBandsPerSeam +
                        layer;
                    Check(
                        TetherfallTransitionBiomes.TryGetMapSeamBand(
                            bandIndex,
                            mapRect,
                            out var band),
                        $"Soft map band {layer} for '{definition.Id}' is unavailable.",
                        failures);
                    Check(
                        band.TransitionId == definition.Id &&
                        band.Thickness > 0f &&
                        band.Thickness <= previousThickness &&
                        band.Tint.a > 0f &&
                        band.Tint.a < 0.5f,
                        $"Soft map band {layer} for '{definition.Id}' is invalid.",
                        failures);
                    previousThickness = band.Thickness;
                }
            }
        }

        private static void VerifyStaticRuntimeContract(List<string> failures)
        {
            var sourcePath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "TetherfallTransitionBiomes.cs");
            Check(File.Exists(sourcePath), "Transition runtime source is missing.", failures);
            if (!File.Exists(sourcePath))
            {
                return;
            }

            var source = File.ReadAllText(sourcePath);
            Check(
                source.Contains("public static TransitionBiomeDirector Build("),
                "Transition runtime does not expose Build(TetherfallGame, Transform).",
                failures);
            Check(
                source.Contains("staticRoot.SetActive(false)") &&
                source.Contains("detailRoot.SetActive(false)"),
                "Transition roots are not born inactive for bounded streaming.",
                failures);
            Check(
                CountOccurrences(source, "private void Update()") == 1,
                "Only the shared transition director may own a per-frame Update loop.",
                failures);
            Check(
                source.Contains("GetComponentsInChildren<Light>(true)") &&
                !source.Contains("AddComponent<Light>"),
                "Transition runtime does not enforce its zero-added-light contract.",
                failures);
            Check(
                !source.Contains("using System.Linq") &&
                !source.Contains(".OrderBy(") &&
                !source.Contains(".Select(") &&
                !source.Contains(".Where("),
                "Transition runtime uses LINQ in a polling-sensitive path.",
                failures);
            Check(
                source.Contains("TransitionResourceInteractable") &&
                !source.Contains("ResourceNode.Create("),
                "Transition resources are not self-contained semantic interactables.",
                failures);
            Check(
                source.Contains("RegisteredAnnexDefinitions") &&
                source.Contains("RegisterAnnexConnectorSeam"),
                "Transition runtime does not expose the local annex seam registry.",
                failures);
            Check(
                source.Contains("Array.AsReadOnly(V1DefinitionsInternal)") &&
                source.Contains(
                    "private static readonly TransitionBiomeDefinition[] V1DefinitionsInternal"),
                "The immutable v1 catalog is not protected from runtime mutation.",
                failures);
            Check(
                !source.Contains("discoveredRegions"),
                "Transition runtime writes directly to region discovery state.",
                failures);
            Check(
                source.Contains("TransitionMapSeamBand") &&
                !source.Contains("GUI.DrawTexture"),
                "Transition map integration is not exposed as soft seam-band data.",
                failures);
        }

        private static void VerifyDocumentation(List<string> failures)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var documentationPath = Path.Combine(projectRoot, "TRANSITION_BIOMES.md");
            Check(
                File.Exists(documentationPath),
                "TRANSITION_BIOMES.md is missing.",
                failures);
            if (!File.Exists(documentationPath))
            {
                return;
            }

            var documentation = File.ReadAllText(documentationPath);
            Check(
                documentation.Contains("not progression regions"),
                "Transition documentation does not state the progression-identity rule.",
                failures);
            Check(
                documentation.Contains("exactly 12") ||
                documentation.Contains("12 v1"),
                "Transition documentation does not state the exact v1 seam count.",
                failures);
            Check(
                documentation.Contains("Build(game, worldRoot)") &&
                documentation.Contains("TryGetBlendedPresentation") &&
                documentation.Contains("TryGetMapSeamBand"),
                "Transition documentation omits required integration hooks.",
                failures);
            Check(
                documentation.Contains("resource.transition.*") &&
                documentation.Contains("encounter.transition.*") &&
                documentation.Contains("secret.transition.*"),
                "Transition documentation omits semantic persistence prefixes.",
                failures);
            Check(
                documentation.Contains("RegisterAnnexConnectorSeam") &&
                documentation.Contains("same progression region"),
                "Transition documentation omits annex compatibility.",
                failures);
            for (var index = 0;
                 index < TetherfallTransitionBiomes.V1Definitions.Count;
                 index++)
            {
                var definition = TetherfallTransitionBiomes.V1Definitions[index];
                Check(
                    documentation.Contains(definition.Id) &&
                    documentation.Contains(definition.AmbientLocaleName),
                    $"Transition documentation does not account for '{definition.Id}'.",
                    failures);
            }
        }

        private static void VerifyExpectedParents(
            TransitionBiomeDefinition definition,
            List<string> failures)
        {
            string expectedA;
            string expectedB;
            switch (definition.Id)
            {
                case "transition.alderwake.morrowfen":
                    expectedA = "Alderwake Basin";
                    expectedB = "Morrowfen";
                    break;
                case "transition.alderwake.cinderwold":
                    expectedA = "Alderwake Basin";
                    expectedB = "Cinderwold Verge";
                    break;
                case "transition.alderwake.glassmere":
                    expectedA = "Alderwake Basin";
                    expectedB = "Glassmere Steppe";
                    break;
                case "transition.morrowfen.rimecoil":
                    expectedA = "Morrowfen";
                    expectedB = "Rimecoil Range";
                    break;
                case "transition.cinderwold.sunken_weald":
                    expectedA = "Cinderwold Verge";
                    expectedB = "Sunken Weald";
                    break;
                case "transition.cinderwold.prismscar":
                    expectedA = "Cinderwold Verge";
                    expectedB = "Prismscar Downs";
                    break;
                case "transition.morrowfen.galecradle":
                    expectedA = "Morrowfen";
                    expectedB = "Galecradle Mesa";
                    break;
                case "transition.cinderwold.lumenwaste":
                    expectedA = "Cinderwold Verge";
                    expectedB = "Lumenwaste Reach";
                    break;
                case "transition.morrowfen.brassroot":
                    expectedA = "Morrowfen";
                    expectedB = "Brassroot Deeps";
                    break;
                case "transition.sunken_weald.cloudbarrow":
                    expectedA = "Sunken Weald";
                    expectedB = "Cloudbarrow Expanse";
                    break;
                case "transition.rimecoil.nightloom":
                    expectedA = "Rimecoil Range";
                    expectedB = "Nightloom Hollow";
                    break;
                case "transition.prismscar.tideglass":
                    expectedA = "Prismscar Downs";
                    expectedB = "Tideglass Strand";
                    break;
                default:
                    expectedA = string.Empty;
                    expectedB = string.Empty;
                    break;
            }

            Check(
                definition.ParentAProgressionRegion == expectedA &&
                definition.ParentBProgressionRegion == expectedB,
                $"Transition '{definition.Id}' does not preserve its completed parent pair.",
                failures);
        }

        private static Vector3 GetBlendSample(
            TransitionBiomeDefinition definition,
            float sampleT)
        {
            sampleT = Mathf.Clamp01(sampleT);
            if (definition.BlendMode == TransitionBiomeBlendMode.AlongSegment)
            {
                return Vector3.Lerp(
                    definition.SegmentStart,
                    definition.SegmentEnd,
                    sampleT);
            }

            var midpoint =
                (definition.SegmentStart + definition.SegmentEnd) * 0.5f;
            var parentBSide = definition.ParentBSide;
            parentBSide.y = 0f;
            parentBSide = parentBSide.sqrMagnitude > 0.0001f
                ? parentBSide.normalized
                : Vector3.right;
            return midpoint +
                   parentBSide *
                   Mathf.Lerp(
                       -definition.CorridorHalfWidth,
                       definition.CorridorHalfWidth,
                       sampleT);
        }

        private static bool IsParentProgression(
            TransitionBiomeDefinition definition,
            string progressionRegion)
        {
            return string.Equals(
                       progressionRegion,
                       definition.ParentAProgressionRegion,
                       StringComparison.Ordinal) ||
                   string.Equals(
                       progressionRegion,
                       definition.ParentBProgressionRegion,
                       StringComparison.Ordinal);
        }

        private static void GetHorizontalFrame(
            TransitionBiomeDefinition definition,
            out Vector3 forward,
            out Vector3 right)
        {
            forward = definition.SegmentEnd - definition.SegmentStart;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f
                ? forward.normalized
                : Vector3.forward;
            right = new Vector3(forward.z, 0f, -forward.x);
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right)
        {
            var deltaX = left.x - right.x;
            var deltaZ = left.z - right.z;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= PositionTolerance;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static int CountOccurrences(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            {
                return 0;
            }

            var count = 0;
            var searchIndex = 0;
            while (searchIndex < source.Length)
            {
                var matchIndex = source.IndexOf(
                    value,
                    searchIndex,
                    StringComparison.Ordinal);
                if (matchIndex < 0)
                {
                    break;
                }

                count++;
                searchIndex = matchIndex + value.Length;
            }

            return count;
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
    }
}
