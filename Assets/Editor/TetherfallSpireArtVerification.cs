using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tetherfall;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tetherfall.Editor
{
    /// <summary>
    /// Edit-mode contract verification for the 40-ID Infinite Spire visual kit.
    /// Both normal Resources selection and forced procedural fallback are exercised.
    /// </summary>
    public static class TetherfallSpireArtVerification
    {
        private const string MenuPath = "Tetherfall/Verify Infinite Spire Art Kit";
        private const float MaximumVisualExtent = 196f;
        private const int ConcurrentRendererBudget = 220;
        private static int assertionCount;

        private static readonly ExpectedIdentity[] ExpectedIdentities =
        {
            new ExpectedIdentity(
                TetherfallSpireArtId.BasePlinth,
                100,
                "spire.exterior.base_plinth"),
            new ExpectedIdentity(
                TetherfallSpireArtId.CityFlange,
                101,
                "spire.exterior.city_flange"),
            new ExpectedIdentity(
                TetherfallSpireArtId.TierDrumA,
                102,
                "spire.exterior.tier_drum_a"),
            new ExpectedIdentity(
                TetherfallSpireArtId.TierDrumB,
                103,
                "spire.exterior.tier_drum_b"),
            new ExpectedIdentity(
                TetherfallSpireArtId.CoilRibSet,
                104,
                "spire.exterior.coil_rib_set"),
            new ExpectedIdentity(
                TetherfallSpireArtId.CrownRing,
                105,
                "spire.exterior.crown_ring"),
            new ExpectedIdentity(
                TetherfallSpireArtId.ActiveSeam,
                106,
                "spire.exterior.active_seam"),

            new ExpectedIdentity(
                TetherfallSpireArtId.DistantNeedle,
                120,
                "spire.proxy.distant_needle"),
            new ExpectedIdentity(
                TetherfallSpireArtId.DistantCrown,
                121,
                "spire.proxy.distant_crown"),

            new ExpectedIdentity(
                TetherfallSpireArtId.FacadeStraight,
                200,
                "spire.facade.straight"),
            new ExpectedIdentity(
                TetherfallSpireArtId.FacadeWindow,
                201,
                "spire.facade.window"),
            new ExpectedIdentity(
                TetherfallSpireArtId.FacadeDoorBay,
                202,
                "spire.facade.door_bay"),
            new ExpectedIdentity(
                TetherfallSpireArtId.FacadeArch,
                203,
                "spire.facade.arch"),

            new ExpectedIdentity(
                TetherfallSpireArtId.ShellStandard,
                300,
                "spire.shell.standard"),
            new ExpectedIdentity(
                TetherfallSpireArtId.ShellCombat,
                301,
                "spire.shell.combat"),
            new ExpectedIdentity(
                TetherfallSpireArtId.ShellPuzzle,
                302,
                "spire.shell.puzzle"),
            new ExpectedIdentity(
                TetherfallSpireArtId.ShellRest,
                303,
                "spire.shell.rest"),

            new ExpectedIdentity(
                TetherfallSpireArtId.DoorRound,
                400,
                "spire.door.round"),
            new ExpectedIdentity(
                TetherfallSpireArtId.DoorSeal,
                401,
                "spire.door.seal"),
            new ExpectedIdentity(
                TetherfallSpireArtId.DoorRetreat,
                402,
                "spire.door.retreat"),

            new ExpectedIdentity(
                TetherfallSpireArtId.StairStraight,
                500,
                "spire.stair.straight"),
            new ExpectedIdentity(
                TetherfallSpireArtId.StairPlatform,
                501,
                "spire.stair.platform"),
            new ExpectedIdentity(
                TetherfallSpireArtId.LiftCage,
                502,
                "spire.lift.cage"),
            new ExpectedIdentity(
                TetherfallSpireArtId.LiftGuide,
                503,
                "spire.lift.guide"),

            new ExpectedIdentity(
                TetherfallSpireArtId.CoilDial,
                600,
                "spire.puzzle.coil_dial"),
            new ExpectedIdentity(
                TetherfallSpireArtId.SeamLatch,
                601,
                "spire.puzzle.seam_latch"),
            new ExpectedIdentity(
                TetherfallSpireArtId.ResonancePlinth,
                602,
                "spire.puzzle.resonance_plinth"),
            new ExpectedIdentity(
                TetherfallSpireArtId.PressureTile,
                603,
                "spire.puzzle.pressure_tile"),

            new ExpectedIdentity(
                TetherfallSpireArtId.MinibossRing,
                700,
                "spire.arena.miniboss_ring"),
            new ExpectedIdentity(
                TetherfallSpireArtId.BossThreshold,
                701,
                "spire.arena.boss_threshold"),
            new ExpectedIdentity(
                TetherfallSpireArtId.BossArenaFloor,
                702,
                "spire.arena.boss_floor"),

            new ExpectedIdentity(
                TetherfallSpireArtId.CacheChest,
                800,
                "spire.prop.cache_chest"),
            new ExpectedIdentity(
                TetherfallSpireArtId.RepairBench,
                801,
                "spire.prop.repair_bench"),
            new ExpectedIdentity(
                TetherfallSpireArtId.WaymarkBanner,
                802,
                "spire.prop.waymark_banner"),

            new ExpectedIdentity(
                TetherfallSpireArtId.CoilhavenCore,
                900,
                "spire.rest.coilhaven_core"),
            new ExpectedIdentity(
                TetherfallSpireArtId.CoilhavenBed,
                901,
                "spire.rest.coilhaven_bed"),
            new ExpectedIdentity(
                TetherfallSpireArtId.CoilhavenHearth,
                902,
                "spire.rest.coilhaven_hearth"),

            new ExpectedIdentity(
                TetherfallSpireArtId.SpirecoilStall,
                1000,
                "spire.city.spirecoil_stall"),
            new ExpectedIdentity(
                TetherfallSpireArtId.SpirecoilCounter,
                1001,
                "spire.city.spirecoil_counter"),
            new ExpectedIdentity(
                TetherfallSpireArtId.SpirecoilBeacon,
                1002,
                "spire.city.spirecoil_beacon")
        };

        [MenuItem(MenuPath, false, 2188)]
        public static void Verify()
        {
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
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Infinite Spire art verification is edit-mode only.");
            }

            assertionCount = 0;
            var failures = new List<string>();
            var fixture = new GameObject("Infinite Spire Art Verification");
            var normalResourceCount = 0;

            try
            {
                VerifyCatalog(failures);
                foreach (var definition in TetherfallSpireArtCatalog.All)
                {
                    if (VerifyPiece(definition, fixture.transform, failures))
                    {
                        normalResourceCount++;
                    }
                }

                Check(
                    normalResourceCount > 0,
                    "No normal build exercised an existing preferred Resources source.",
                    failures);
                Check(
                    !TetherfallSpireArt.TryBuild(
                        "spire.not_real",
                        fixture.transform,
                        out var unknownBuild) &&
                    unknownBuild == null &&
                    !TetherfallSpireArt.TryBuild(
                        (TetherfallSpireArtId)(-1),
                        fixture.transform,
                        out unknownBuild) &&
                    unknownBuild == null,
                    "TryBuild did not reject unknown stable and enum IDs safely.",
                    failures);
                VerifyHeroSilhouette(fixture.transform, failures);
                VerifyConcurrentFixtureAndController(fixture.transform, failures);
                VerifyDocumentationAndProvenance(failures);
            }
            finally
            {
                Object.DestroyImmediate(fixture);
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Infinite Spire art verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            if (logSuccess)
            {
                Debug.Log(
                    $"Infinite Spire art verification passed: {assertionCount} assertions across " +
                    $"exactly {ExpectedIdentities.Length} immutable IDs; {normalResourceCount} " +
                    "normal builds used provenance-linked Quaternius Resources geometry, every ID " +
                    "passed forced procedural fallback, the full concurrent fixture stayed within " +
                    "220 renderers and ten original shared materials, and exterior/proxy, " +
                    "rest/vendor, and interior activation contracts held.");
            }
        }

        private static void VerifyCatalog(List<string> failures)
        {
            var definitions = TetherfallSpireArtCatalog.All;
            var enumValues = (TetherfallSpireArtId[])Enum.GetValues(
                typeof(TetherfallSpireArtId));

            Check(
                ExpectedIdentities.Length == 40,
                $"Verifier identity manifest must contain exactly 40 entries; found " +
                $"{ExpectedIdentities.Length}.",
                failures);
            Check(
                definitions.Count == 40 && enumValues.Length == 40,
                $"Spire enum/catalog identity must be exactly 40/40; found " +
                $"{enumValues.Length}/{definitions.Count}.",
                failures);
            Check(
                definitions is ICollection<TetherfallSpireArtDefinition> definitionCollection &&
                definitionCollection.IsReadOnly,
                "Catalog does not expose a read-only definition collection.",
                failures);

            foreach (var property in typeof(TetherfallSpireArtDefinition).GetProperties(
                         BindingFlags.Instance | BindingFlags.Public))
            {
                Check(
                    property.SetMethod == null || !property.SetMethod.IsPublic,
                    $"Definition property '{property.Name}' exposes a public setter.",
                    failures);
            }

            var ids = new HashSet<TetherfallSpireArtId>();
            var numericIds = new HashSet<int>();
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var sourceNames = new HashSet<string>(StringComparer.Ordinal);
            var socketCoverage = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in TetherfallQuaterniusAssetCatalog.All)
            {
                sourceNames.Add(source.ResourceName);
            }

            for (var index = 0; index < ExpectedIdentities.Length; index++)
            {
                var expected = ExpectedIdentities[index];
                Check(
                    (int)expected.ArtId == expected.NumericValue,
                    $"Enum '{expected.ArtId}' changed from explicit value " +
                    $"{expected.NumericValue} to {(int)expected.ArtId}.",
                    failures);
                Check(
                    index < definitions.Count && definitions[index].ArtId == expected.ArtId,
                    $"Catalog order/identity at index {index} is not '{expected.ArtId}'.",
                    failures);

                TetherfallSpireArtDefinition definition;
                try
                {
                    definition = TetherfallSpireArtCatalog.Get(expected.ArtId);
                }
                catch (Exception exception)
                {
                    Check(
                        false,
                        $"Catalog lookup for '{expected.ArtId}' threw " +
                        $"{exception.GetType().Name}: {exception.Message}",
                        failures);
                    continue;
                }

                Check(
                    string.Equals(
                        definition.StableId,
                        expected.StableId,
                        StringComparison.Ordinal),
                    $"Enum '{expected.ArtId}' must map to immutable ID " +
                    $"'{expected.StableId}', found '{definition.StableId}'.",
                    failures);
                Check(
                    ids.Add(definition.ArtId),
                    $"Catalog repeats enum ID '{definition.ArtId}'.",
                    failures);
                Check(
                    numericIds.Add((int)definition.ArtId),
                    $"Catalog repeats numeric ID '{(int)definition.ArtId}'.",
                    failures);
                Check(
                    stableIds.Add(definition.StableId),
                    $"Catalog repeats stable ID '{definition.StableId}'.",
                    failures);
                Check(
                    definition.StableId.StartsWith("spire.", StringComparison.Ordinal) &&
                    definition.StableId.IndexOf(' ') < 0 &&
                    string.Equals(
                        definition.StableId,
                        definition.StableId.ToLowerInvariant(),
                        StringComparison.Ordinal),
                    $"Stable ID '{definition.StableId}' is not a normalized spire.* ID.",
                    failures);
                Check(
                    TetherfallSpireArtCatalog.TryGet(
                        definition.StableId,
                        out var byStableId) &&
                    ReferenceEquals(byStableId, definition),
                    $"Stable lookup did not return the immutable catalog instance for " +
                    $"'{definition.StableId}'.",
                    failures);
                Check(
                    TetherfallSpireArtCatalog.TryParse(
                        definition.StableId,
                        out var parsed) &&
                    parsed == definition.ArtId &&
                    string.Equals(
                        TetherfallSpireArtCatalog.GetStableId(parsed),
                        definition.StableId,
                        StringComparison.Ordinal),
                    $"Stable ID round trip failed for '{definition.StableId}'.",
                    failures);
                Check(
                    TetherfallSpireArt.IsFinite(definition.Dimensions) &&
                    definition.Dimensions.x > 0f &&
                    definition.Dimensions.y > 0f &&
                    definition.Dimensions.z > 0f &&
                    MaxAbs(definition.Dimensions) <= 96f,
                    $"'{definition.StableId}' has invalid dimensions " +
                    $"{definition.Dimensions}.",
                    failures);
                Check(
                    TetherfallSpireArt.IsFinite(
                        new Vector3(definition.Footprint.x, 0f, definition.Footprint.y)) &&
                    Mathf.Approximately(definition.Footprint.x, definition.Dimensions.x) &&
                    Mathf.Approximately(definition.Footprint.y, definition.Dimensions.z),
                    $"'{definition.StableId}' footprint does not match its X/Z dimensions.",
                    failures);
                Check(
                    Vector3.SqrMagnitude(definition.Pivot) < 0.000001f &&
                    Vector3.SqrMagnitude(definition.Forward - Vector3.forward) < 0.000001f,
                    $"'{definition.StableId}' does not declare a ground-contact, +Z-forward " +
                    "contract.",
                    failures);

                VerifyDeclaredBudget(definition, failures);
                VerifyCullingMetadata(definition, failures);
                VerifySocketDeclaration(definition, socketCoverage, failures);
                VerifyPreferredSource(definition, sourceNames, failures);
            }

            foreach (var enumValue in enumValues)
            {
                Check(
                    ids.Contains(enumValue),
                    $"Enum ID '{enumValue}' has no exact catalog identity.",
                    failures);
            }

            var requiredRoleSockets = new[]
            {
                TetherfallSpireArt.SocketDoor,
                TetherfallSpireArt.SocketStairUp,
                TetherfallSpireArt.SocketStairDown,
                TetherfallSpireArt.SocketLift,
                TetherfallSpireArt.SocketPuzzle,
                TetherfallSpireArt.SocketTreasure,
                TetherfallSpireArt.SocketVendor,
                TetherfallSpireArt.SocketRest,
                TetherfallSpireArt.SocketBossGate,
                TetherfallSpireArt.SocketWork,
                TetherfallSpireArt.SocketVfx
            };
            foreach (var socketName in requiredRoleSockets)
            {
                Check(
                    socketCoverage.Contains(socketName),
                    $"The 40-ID catalog never declares required role socket '{socketName}'.",
                    failures);
            }

            CheckPrefixCount(definitions, "spire.exterior.", 7, failures);
            CheckPrefixCount(definitions, "spire.proxy.", 2, failures);
            CheckPrefixCount(definitions, "spire.facade.", 4, failures);
            CheckPrefixCount(definitions, "spire.shell.", 4, failures);
            CheckPrefixCount(definitions, "spire.door.", 3, failures);
            CheckPrefixCount(definitions, "spire.stair.", 2, failures);
            CheckPrefixCount(definitions, "spire.lift.", 2, failures);
            CheckPrefixCount(definitions, "spire.puzzle.", 4, failures);
            CheckPrefixCount(definitions, "spire.arena.", 3, failures);
            CheckPrefixCount(definitions, "spire.prop.", 3, failures);
            CheckPrefixCount(definitions, "spire.rest.", 3, failures);
            CheckPrefixCount(definitions, "spire.city.", 3, failures);

            Check(
                !TetherfallSpireArtCatalog.TryGet("spire.not_real", out _) &&
                !TetherfallSpireArtCatalog.TryParse(null, out _),
                "Unknown/null Spire stable IDs unexpectedly resolved.",
                failures);
        }

        private static void VerifyDeclaredBudget(
            TetherfallSpireArtDefinition definition,
            List<string> failures)
        {
            TetherfallSpireArtCatalog.GetBudget(
                definition.BudgetClass,
                out var expectedRenderers,
                out var expectedMaterials);
            Check(
                definition.MaxRendererCount == expectedRenderers &&
                definition.MaxSharedMaterialCount == expectedMaterials,
                $"'{definition.StableId}' declares {definition.MaxRendererCount}/" +
                $"{definition.MaxSharedMaterialCount}; {definition.BudgetClass} requires " +
                $"{expectedRenderers}/{expectedMaterials}.",
                failures);
        }

        private static void VerifyCullingMetadata(
            TetherfallSpireArtDefinition definition,
            List<string> failures)
        {
            switch (definition.PresentationClass)
            {
                case TetherfallSpirePresentationClass.LocalExterior:
                case TetherfallSpirePresentationClass.DistantProxy:
                    Check(
                        definition.UsesDistanceCulling &&
                        Mathf.Approximately(
                            definition.NearDistance,
                            TetherfallSpirePresentationController.ExteriorNearDistance) &&
                        Mathf.Approximately(
                            definition.CullDistance,
                            TetherfallSpirePresentationController.ExteriorCullDistance),
                        $"'{definition.StableId}' does not expose the 70m/145m exterior band.",
                        failures);
                    break;
                case TetherfallSpirePresentationClass.CoilhavenOrVendor:
                    Check(
                        definition.UsesDistanceCulling &&
                        Mathf.Approximately(
                            definition.NearDistance,
                            TetherfallSpirePresentationController.RestVendorNearDistance) &&
                        Mathf.Approximately(
                            definition.CullDistance,
                            TetherfallSpirePresentationController.RestVendorCullDistance),
                        $"'{definition.StableId}' does not expose the 70m/110m rest/vendor band.",
                        failures);
                    break;
                default:
                    Check(
                        definition.IsInteriorActivationReady &&
                        !definition.UsesDistanceCulling &&
                        Mathf.Approximately(definition.NearDistance, 0f) &&
                        Mathf.Approximately(definition.CullDistance, 0f),
                        $"'{definition.StableId}' is not interior-activation ready.",
                        failures);
                    break;
            }
        }

        private static void VerifySocketDeclaration(
            TetherfallSpireArtDefinition definition,
            HashSet<string> socketCoverage,
            List<string> failures)
        {
            Check(
                definition.RequiredSockets is ICollection<string> socketCollection &&
                socketCollection.IsReadOnly,
                $"'{definition.StableId}' does not expose immutable socket requirements.",
                failures);

            var declared = new HashSet<string>(StringComparer.Ordinal);
            foreach (var socketName in definition.RequiredSockets)
            {
                Check(
                    IsKnownSocketName(socketName),
                    $"'{definition.StableId}' declares unknown socket '{socketName}'.",
                    failures);
                Check(
                    declared.Add(socketName),
                    $"'{definition.StableId}' repeats socket '{socketName}'.",
                    failures);
                Check(
                    definition.RequiresSocket(socketName),
                    $"'{definition.StableId}' cannot query declared socket '{socketName}'.",
                    failures);
                socketCoverage.Add(socketName);
            }

            if (definition.StableId.StartsWith("spire.exterior.", StringComparison.Ordinal) ||
                definition.StableId.StartsWith("spire.facade.", StringComparison.Ordinal) ||
                definition.StableId.StartsWith("spire.shell.", StringComparison.Ordinal) ||
                definition.StableId.StartsWith("spire.door.", StringComparison.Ordinal) ||
                definition.StableId.StartsWith("spire.stair.", StringComparison.Ordinal) ||
                definition.StableId.StartsWith("spire.lift.", StringComparison.Ordinal) ||
                definition.ArtId == TetherfallSpireArtId.MinibossRing ||
                definition.ArtId == TetherfallSpireArtId.BossArenaFloor)
            {
                foreach (var snap in SnapSocketNames())
                {
                    Check(
                        definition.RequiresSocket(snap),
                        $"Modular contract '{definition.StableId}' is missing '{snap}'.",
                        failures);
                }
            }
        }

        private static void VerifyPreferredSource(
            TetherfallSpireArtDefinition definition,
            HashSet<string> sourceNames,
            List<string> failures)
        {
            if (!definition.HasPreferredResource)
            {
                Check(
                    string.Equals(
                        definition.PreferredSourceCredit,
                        TetherfallSpireArt.OriginalSourceCredit,
                        StringComparison.Ordinal),
                    $"Procedural definition '{definition.StableId}' has unsafe source credit.",
                    failures);
                return;
            }

            Check(
                sourceNames.Contains(definition.PreferredResourceName),
                $"'{definition.StableId}' references uncurated Resources source " +
                $"'{definition.PreferredResourceName}'.",
                failures);
            Check(
                string.Equals(
                    definition.PreferredResourcePath,
                    TetherfallQuaterniusAssetCatalog.ResourcePrefix +
                    definition.PreferredResourceName,
                    StringComparison.Ordinal) &&
                definition.PreferredResourcePath.IndexOf(
                    "ThirdParty",
                    StringComparison.OrdinalIgnoreCase) < 0,
                $"'{definition.StableId}' has an unsafe or non-Resources source path " +
                $"'{definition.PreferredResourcePath}'.",
                failures);
            Check(
                string.Equals(
                    definition.PreferredSourceCredit,
                    TetherfallSpireArt.QuaterniusSourceCredit,
                    StringComparison.Ordinal) &&
                definition.PreferredSourceCredit.IndexOf(
                    "CC0",
                    StringComparison.Ordinal) >= 0,
                $"'{definition.StableId}' does not preserve Quaternius CC0 credit.",
                failures);

            var prefabPath =
                TetherfallQuaterniusAssetCatalog.OutputFolder + "/" +
                definition.PreferredResourceName + ".prefab";
            Check(
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null,
                $"Preferred source '{definition.PreferredResourceName}' is not an existing " +
                $"Resources prefab at '{prefabPath}'.",
                failures);
        }

        private static bool VerifyPiece(
            TetherfallSpireArtDefinition definition,
            Transform fixture,
            List<string> failures)
        {
            var parent = new GameObject("Piece fixture · " + definition.StableId).transform;
            parent.SetParent(fixture, false);
            var sourceStates = CapturePreferredSourceStates(definition);
            var usedPreferredResource = false;

            try
            {
                GameObject normal;
                try
                {
                    normal = TetherfallSpireArt.Build(definition.ArtId, parent);
                }
                catch (Exception exception)
                {
                    Check(
                        false,
                        $"Normal build for '{definition.StableId}' threw " +
                        $"{exception.GetType().Name}: {exception.Message}",
                        failures);
                    return false;
                }

                var normalPiece = normal == null
                    ? null
                    : normal.GetComponent<TetherfallSpireArtPiece>();
                VerifyBuiltPiece(normal, normalPiece, definition, false, failures);
                VerifySourceStatesUnchanged(sourceStates, definition.StableId, failures);
                usedPreferredResource =
                    normalPiece != null && normalPiece.UsedPreferredResource;

                var initialSnapshot = CaptureSnapshot(normal);
                var duplicate = TetherfallSpireArt.Build(definition.ArtId, parent);
                Check(
                    ReferenceEquals(normal, duplicate),
                    $"Duplicate Build replaced the root for '{definition.StableId}'.",
                    failures);
                Check(
                    initialSnapshot.Equals(CaptureSnapshot(duplicate)),
                    $"Duplicate Build changed the hierarchy for '{definition.StableId}'.",
                    failures);
                Check(
                    CountDirectPieces(parent, definition.ArtId) == 1,
                    $"Duplicate Build created multiple roots for '{definition.StableId}'.",
                    failures);

                var placedPosition = new Vector3(3.25f, 0f, -2.5f);
                var placedRotation = Quaternion.Euler(0f, 37f, 0f);
                normal.transform.localPosition = placedPosition;
                normal.transform.localRotation = placedRotation;
                var placedSnapshot = CaptureSnapshot(normal);
                var rebuilt = TetherfallSpireArt.Rebuild(definition.ArtId, parent);
                Check(
                    ReferenceEquals(normal, rebuilt),
                    $"Rebuild replaced the root for '{definition.StableId}'.",
                    failures);
                Check(
                    Vector3.SqrMagnitude(rebuilt.transform.localPosition - placedPosition) <
                    0.000001f &&
                    Quaternion.Angle(rebuilt.transform.localRotation, placedRotation) < 0.001f,
                    $"Rebuild did not preserve caller placement for '{definition.StableId}'.",
                    failures);
                Check(
                    placedSnapshot.Equals(CaptureSnapshot(rebuilt)),
                    $"Rebuild changed deterministic children for '{definition.StableId}'.",
                    failures);
                VerifySourceStatesUnchanged(sourceStates, definition.StableId, failures);

                rebuilt.transform.localPosition = Vector3.zero;
                rebuilt.transform.localRotation = Quaternion.identity;
                VerifyBuiltPiece(
                    rebuilt,
                    rebuilt.GetComponent<TetherfallSpireArtPiece>(),
                    definition,
                    false,
                    failures);

                Check(
                    TetherfallSpireArt.Clear(parent, definition.ArtId) &&
                    CountDirectPieces(parent, definition.ArtId) == 0 &&
                    !TetherfallSpireArt.Clear(parent, definition.ArtId),
                    $"Enum cleanup for '{definition.StableId}' was not idempotent.",
                    failures);

                Check(
                    TetherfallSpireArt.TryBuild(
                        definition.StableId,
                        parent,
                        out var tryBuilt) &&
                    tryBuilt != null &&
                    tryBuilt.GetComponent<TetherfallSpireArtPiece>() != null,
                    $"TryBuild failed for known ID '{definition.StableId}'.",
                    failures);
                Check(
                    TetherfallSpireArt.Clear(parent, definition.StableId),
                    $"Stable-string cleanup failed for '{definition.StableId}'.",
                    failures);

                GameObject fallback;
                try
                {
                    fallback = TetherfallSpireArt.BuildProceduralFallback(
                        definition.ArtId,
                        parent);
                }
                catch (Exception exception)
                {
                    Check(
                        false,
                        $"Forced fallback for '{definition.StableId}' threw " +
                        $"{exception.GetType().Name}: {exception.Message}",
                        failures);
                    return usedPreferredResource;
                }

                var fallbackPiece = fallback.GetComponent<TetherfallSpireArtPiece>();
                VerifyBuiltPiece(fallback, fallbackPiece, definition, true, failures);
                Check(
                    fallbackPiece != null &&
                    fallbackPiece.ForcedProceduralFallback &&
                    fallbackPiece.UsedProceduralFallback &&
                    !fallbackPiece.UsedPreferredResource,
                    $"Forced fallback metadata is incorrect for '{definition.StableId}'.",
                    failures);
                Check(
                    FindSourceChildren(fallback).Count == 0,
                    $"Forced fallback for '{definition.StableId}' retained Resources geometry.",
                    failures);

                var fallbackSnapshot = CaptureSnapshot(fallback);
                var repeatedFallback = TetherfallSpireArt.BuildProceduralFallback(
                    definition.ArtId,
                    parent);
                Check(
                    ReferenceEquals(fallback, repeatedFallback),
                    $"Repeated fallback replaced the root for '{definition.StableId}'.",
                    failures);
                Check(
                    fallbackSnapshot.Equals(CaptureSnapshot(repeatedFallback)),
                    $"Repeated fallback changed deterministic children for " +
                    $"'{definition.StableId}'.",
                    failures);
                VerifySourceStatesUnchanged(sourceStates, definition.StableId, failures);

                Check(
                    TetherfallSpireArt.Clear(parent, definition.StableId) &&
                    CountDirectPieces(parent, definition.ArtId) == 0 &&
                    !TetherfallSpireArt.Clear(parent, definition.StableId),
                    $"Fallback cleanup for '{definition.StableId}' was not idempotent.",
                    failures);
                return usedPreferredResource;
            }
            finally
            {
                Object.DestroyImmediate(parent.gameObject);
            }
        }

        private static void VerifyBuiltPiece(
            GameObject root,
            TetherfallSpireArtPiece piece,
            TetherfallSpireArtDefinition definition,
            bool requireForcedFallback,
            List<string> failures)
        {
            var label = requireForcedFallback
                ? definition.StableId + " forced fallback"
                : definition.StableId + " normal build";
            Check(root != null, $"{label} returned a null root.", failures);
            Check(piece != null, $"{label} has no Spire metadata.", failures);
            if (root == null || piece == null)
            {
                return;
            }

            Check(
                piece.ArtId == definition.ArtId &&
                string.Equals(piece.StableId, definition.StableId, StringComparison.Ordinal) &&
                ReferenceEquals(piece.Definition, definition),
                $"{label} metadata does not match its immutable definition.",
                failures);
            Check(
                root.transform.parent != null &&
                Vector3.SqrMagnitude(root.transform.localPosition) < 0.000001f &&
                Quaternion.Angle(root.transform.localRotation, Quaternion.identity) < 0.001f &&
                Vector3.SqrMagnitude(root.transform.localScale - Vector3.one) < 0.000001f &&
                Vector3.SqrMagnitude(root.transform.forward - root.transform.parent.forward) <
                0.000001f,
                $"{label} does not start at caller-local identity with +Z forward.",
                failures);
            Check(
                root.transform.Find(TetherfallSpireArt.VisualRootName) != null,
                $"{label} is missing its dedicated visual group.",
                failures);

            VerifyFiniteHierarchy(root.transform, label, failures);
            VerifyForbiddenComponents(root, label, failures);
            VerifyNoFrameLoopMethods(piece, label, failures);
            VerifySockets(root.transform, definition, label, failures);

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var materials = new HashSet<Material>();
            Check(
                renderers.Length > 0 &&
                renderers.Length <= definition.MaxRendererCount &&
                renderers.Length == piece.RendererCount,
                $"{label} has {renderers.Length} renderers (metadata {piece.RendererCount}); " +
                $"budget is {definition.MaxRendererCount}.",
                failures);

            foreach (var visualRenderer in renderers)
            {
                Check(
                    visualRenderer != null &&
                    visualRenderer.enabled &&
                    TetherfallSpireArt.IsFinite(visualRenderer.bounds) &&
                    visualRenderer.bounds.size.sqrMagnitude > 0.000001f &&
                    MaxAbs(visualRenderer.bounds.size) <= MaximumVisualExtent,
                    $"{label} has a disabled, empty, invalid, or unbounded renderer at " +
                    $"'{GetHierarchyPath(visualRenderer == null ? null : visualRenderer.transform)}'.",
                    failures);
                if (visualRenderer == null)
                {
                    continue;
                }

                Check(
                    visualRenderer.sharedMaterials.Length > 0,
                    $"{label} renderer '{GetHierarchyPath(visualRenderer.transform)}' has no " +
                    "shared material.",
                    failures);
                foreach (var material in visualRenderer.sharedMaterials)
                {
                    Check(
                        material != null,
                        $"{label} has a missing material at " +
                        $"'{GetHierarchyPath(visualRenderer.transform)}'.",
                        failures);
                    if (material != null)
                    {
                        materials.Add(material);
                    }
                }
            }

            Check(
                materials.Count > 0 &&
                materials.Count <= definition.MaxSharedMaterialCount &&
                materials.Count == piece.SharedMaterialCount,
                $"{label} uses {materials.Count} materials (metadata " +
                $"{piece.SharedMaterialCount}); budget is " +
                $"{definition.MaxSharedMaterialCount}.",
                failures);
            Check(
                TetherfallSpireArt.IsFinite(piece.LocalVisualBounds) &&
                piece.LocalVisualBounds.size.sqrMagnitude > 0.000001f &&
                MaxAbs(piece.LocalVisualBounds.size) <= MaximumVisualExtent &&
                piece.LocalVisualBounds.min.y >= -0.08f &&
                piece.LocalVisualBounds.min.y <= 0.2f,
                $"{label} has invalid or non-grounded local bounds " +
                $"{piece.LocalVisualBounds}.",
                failures);
            Check(
                piece.LocalVisualBounds.size.x <= definition.Dimensions.x * 1.5f + 1f &&
                piece.LocalVisualBounds.size.y <= definition.Dimensions.y * 1.5f + 1f &&
                piece.LocalVisualBounds.size.z <= definition.Dimensions.z * 1.5f + 1f,
                $"{label} visual bounds {piece.LocalVisualBounds.size} exceed declared " +
                $"dimensions {definition.Dimensions}.",
                failures);
            Check(
                piece.PresentationClass == definition.PresentationClass &&
                string.Equals(
                    piece.CullingGroupKey,
                    definition.CullingGroupKey,
                    StringComparison.Ordinal) &&
                Mathf.Approximately(piece.NearDistance, definition.NearDistance) &&
                Mathf.Approximately(piece.CullDistance, definition.CullDistance) &&
                piece.InteriorActivationReady == definition.IsInteriorActivationReady,
                $"{label} does not expose the catalog presentation metadata.",
                failures);

            if (requireForcedFallback)
            {
                Check(
                    piece.ForcedProceduralFallback &&
                    !piece.UsedPreferredResource,
                    $"{label} did not bypass Resources geometry.",
                    failures);
            }
            else
            {
                Check(
                    !piece.ForcedProceduralFallback,
                    $"{label} incorrectly reports a forced fallback.",
                    failures);
            }

            if (piece.UsedPreferredResource)
            {
                var sourceChildren = FindSourceChildren(root);
                Check(
                    definition.HasPreferredResource &&
                    sourceChildren.Count == 1 &&
                    string.Equals(
                        sourceChildren[0].name,
                        "Quaternius source · " + definition.PreferredResourceName,
                        StringComparison.Ordinal),
                    $"{label} does not expose exactly one provenance-safe source child.",
                    failures);
            }
        }

        private static void VerifySockets(
            Transform root,
            TetherfallSpireArtDefinition definition,
            string label,
            List<string> failures)
        {
            var sockets = root.Find(TetherfallSpireArt.SocketRootName);
            if (definition.RequiredSockets.Count == 0)
            {
                Check(sockets == null, $"{label} created undeclared sockets.", failures);
                return;
            }

            Check(sockets != null, $"{label} has no socket group.", failures);
            if (sockets == null)
            {
                return;
            }

            Check(
                sockets.childCount == definition.RequiredSockets.Count,
                $"{label} built {sockets.childCount} sockets for " +
                $"{definition.RequiredSockets.Count} declarations.",
                failures);
            var found = new HashSet<string>(StringComparer.Ordinal);
            foreach (Transform socket in sockets)
            {
                Check(
                    found.Add(socket.name),
                    $"{label} duplicates socket '{socket.name}'.",
                    failures);
                Check(
                    definition.RequiresSocket(socket.name),
                    $"{label} built undeclared socket '{socket.name}'.",
                    failures);
                Check(
                    TetherfallSpireArt.IsFinite(socket.localPosition) &&
                    TetherfallSpireArt.IsFinite(socket.localRotation) &&
                    Vector3.SqrMagnitude(socket.localScale - Vector3.one) < 0.000001f,
                    $"{label} socket '{socket.name}' has an invalid transform.",
                    failures);
            }

            foreach (var required in definition.RequiredSockets)
            {
                Check(
                    sockets.Find(required) != null,
                    $"{label} is missing required socket '{required}'.",
                    failures);
            }

            var halfX = definition.Footprint.x * 0.5f;
            var halfZ = definition.Footprint.y * 0.5f;
            VerifySocketPosition(sockets, TetherfallSpireArt.SnapNorth, new Vector3(0f, 0f, halfZ), label, failures);
            VerifySocketPosition(sockets, TetherfallSpireArt.SnapEast, new Vector3(halfX, 0f, 0f), label, failures);
            VerifySocketPosition(sockets, TetherfallSpireArt.SnapSouth, new Vector3(0f, 0f, -halfZ), label, failures);
            VerifySocketPosition(sockets, TetherfallSpireArt.SnapWest, new Vector3(-halfX, 0f, 0f), label, failures);
            VerifySocketPosition(sockets, TetherfallSpireArt.SnapUp, new Vector3(0f, definition.Dimensions.y, 0f), label, failures);
            VerifySocketPosition(sockets, TetherfallSpireArt.SnapDown, Vector3.zero, label, failures);

            var work = sockets.Find(TetherfallSpireArt.SocketWork);
            if (work != null)
            {
                var horizontal = work.localPosition;
                horizontal.y = 0f;
                Check(
                    horizontal.magnitude >= halfZ + 1.5f - 0.01f,
                    $"{label} does not preserve 1.5m work clearance at Socket_Work.",
                    failures);
            }

            if (definition.StableId.StartsWith("spire.door.", StringComparison.Ordinal) ||
                definition.ArtId == TetherfallSpireArtId.FacadeDoorBay ||
                definition.ArtId == TetherfallSpireArtId.FacadeArch)
            {
                Check(
                    definition.Dimensions.x >= 1.4f &&
                    definition.Dimensions.y >= 2.4f &&
                    sockets.Find(TetherfallSpireArt.SocketDoor) != null,
                    $"{label} does not preserve the 1.4m x 2.4m door contract.",
                    failures);
            }

            if (definition.StableId.StartsWith("spire.stair.", StringComparison.Ordinal))
            {
                Check(
                    definition.Dimensions.x >= 4f &&
                    definition.Dimensions.y >= 3f &&
                    definition.Dimensions.z >= 4f &&
                    sockets.Find(TetherfallSpireArt.SocketStairUp) != null &&
                    sockets.Find(TetherfallSpireArt.SocketStairDown) != null,
                    $"{label} does not preserve the 4m x 3m x 4m stair bay.",
                    failures);
            }

            if (definition.StableId.StartsWith("spire.lift.", StringComparison.Ordinal))
            {
                Check(
                    definition.Footprint.x >= 3f &&
                    definition.Footprint.y >= 0.5f &&
                    sockets.Find(TetherfallSpireArt.SocketLift) != null,
                    $"{label} does not expose its lift-deck contract.",
                    failures);
            }
        }

        private static void VerifyHeroSilhouette(
            Transform fixture,
            List<string> failures)
        {
            var root = new GameObject("Original Infinite Spire Hero Silhouette").transform;
            root.SetParent(fixture, false);
            var placements = new[]
            {
                new HeroPlacement(TetherfallSpireArtId.BasePlinth, 0f),
                new HeroPlacement(TetherfallSpireArtId.CityFlange, 6f),
                new HeroPlacement(TetherfallSpireArtId.TierDrumA, 10f),
                new HeroPlacement(TetherfallSpireArtId.TierDrumB, 22f),
                new HeroPlacement(TetherfallSpireArtId.CoilRibSet, 8f),
                new HeroPlacement(TetherfallSpireArtId.CrownRing, 34f),
                new HeroPlacement(TetherfallSpireArtId.ActiveSeam, 21.8f)
            };

            try
            {
                foreach (var placement in placements)
                {
                    var visual = TetherfallSpireArt.Build(placement.ArtId, root);
                    visual.transform.localPosition = Vector3.up * placement.Height;
                }

                var tealPieces = new HashSet<TetherfallSpireArtId>();
                var heroMaterials = new HashSet<Material>();
                var drumCount = 0;
                var liftRibCount = 0;
                var copperRendererCount = 0;
                var heroRenderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (var visualRenderer in heroRenderers)
                {
                    if (visualRenderer.name.IndexOf(
                            "Stepped civic drum",
                            StringComparison.Ordinal) >= 0)
                    {
                        drumCount++;
                    }
                    if (visualRenderer.name.IndexOf(
                            "External copper lift rib",
                            StringComparison.Ordinal) >= 0)
                    {
                        liftRibCount++;
                    }

                    foreach (var material in visualRenderer.sharedMaterials)
                    {
                        if (material == null)
                        {
                            continue;
                        }

                        heroMaterials.Add(material);
                        if (string.Equals(
                                material.name,
                                TetherfallSpireArt.OriginalMaterialPrefix + "Restrained Teal",
                                StringComparison.Ordinal))
                        {
                            var piece =
                                visualRenderer.GetComponentInParent<TetherfallSpireArtPiece>();
                            if (piece != null)
                            {
                                tealPieces.Add(piece.ArtId);
                            }
                        }
                        if (string.Equals(
                                material.name,
                                TetherfallSpireArt.OriginalMaterialPrefix + "Civic Copper",
                                StringComparison.Ordinal))
                        {
                            copperRendererCount++;
                        }
                    }
                }

                var bounds = CalculateRendererBounds(root);
                Check(
                    drumCount >= 4 &&
                    bounds.size.y > Mathf.Max(bounds.size.x, bounds.size.z) * 1.1f,
                    $"Hero is not a readable stepped civic drum stack: {drumCount} drum " +
                    $"renderers, bounds {bounds.size}.",
                    failures);
                Check(
                    liftRibCount == 4 && copperRendererCount >= 8,
                    $"Hero lacks four external copper lift ribs and civic copper hierarchy; " +
                    $"found {liftRibCount} ribs and {copperRendererCount} copper renderers.",
                    failures);
                Check(
                    tealPieces.Count == 1 &&
                    tealPieces.Contains(TetherfallSpireArtId.ActiveSeam),
                    "Hero exterior must contain one active teal seam piece and no teal wall fill.",
                    failures);
                Check(
                    heroRenderers.Length <=
                    TetherfallSpireArtCatalog.LocalExteriorRendererBudget &&
                    heroMaterials.Count <=
                    TetherfallSpireArtCatalog.LocalExteriorMaterialBudget,
                    $"Hero exterior uses {heroRenderers.Length} renderers and " +
                    $"{heroMaterials.Count} materials; local exterior budget is 48/8.",
                    failures);

                foreach (var source in FindSourceChildren(root.gameObject))
                {
                    Check(
                        source.name.IndexOf("Tower", StringComparison.OrdinalIgnoreCase) < 0,
                        $"Hero directly incorporates an unmodified tower source '{source.name}'.",
                        failures);
                }
                Check(
                    root.GetComponentsInChildren<Collider>(true).Length == 0 &&
                    root.GetComponentsInChildren<Light>(true).Length == 0,
                    "Hero silhouette contains visual colliders or lights.",
                    failures);
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        private static void VerifyConcurrentFixtureAndController(
            Transform fixture,
            List<string> failures)
        {
            var root = new GameObject("Concurrent Infinite Spire Fixture").transform;
            root.SetParent(fixture, false);
            var cameraObject = new GameObject("Spire Verification Camera");
            cameraObject.transform.SetParent(fixture, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 10f, -45f);
            var pieces = new Dictionary<TetherfallSpireArtId, TetherfallSpireArtPiece>();

            try
            {
                var interiorIndex = 0;
                var restIndex = 0;
                foreach (var definition in TetherfallSpireArtCatalog.All)
                {
                    var visual = TetherfallSpireArt.Build(definition.ArtId, root);
                    var piece = visual.GetComponent<TetherfallSpireArtPiece>();
                    pieces.Add(definition.ArtId, piece);

                    if (definition.ArtId == TetherfallSpireArtId.DistantCrown)
                    {
                        visual.transform.localPosition = new Vector3(0f, 82f, 0f);
                    }
                    else if (definition.StableId.StartsWith(
                                 "spire.exterior.",
                                 StringComparison.Ordinal))
                    {
                        visual.transform.localPosition =
                            GetHeroFixturePosition(definition.ArtId);
                    }
                    else if (definition.StableId.StartsWith(
                                 "spire.facade.",
                                 StringComparison.Ordinal))
                    {
                        visual.transform.localPosition =
                            new Vector3((int)definition.ArtId - 201.5f, 0f, 15f);
                    }
                    else if (
                        definition.PresentationClass ==
                        TetherfallSpirePresentationClass.CoilhavenOrVendor)
                    {
                        visual.transform.localPosition =
                            new Vector3(42f + restIndex * 4f, 0f, restIndex % 2 * 5f);
                        restIndex++;
                    }
                    else if (
                        definition.PresentationClass ==
                        TetherfallSpirePresentationClass.InteriorActivation)
                    {
                        visual.transform.localPosition =
                            new Vector3(
                                90f + interiorIndex % 6 * 36f,
                                0f,
                                interiorIndex / 6 * 36f);
                        interiorIndex++;
                    }
                }

                var renderers = root.GetComponentsInChildren<Renderer>(true);
                var proxyRenderers = new List<Renderer>();
                var proxyMaterials = new HashSet<Material>();
                foreach (var pair in pieces)
                {
                    if (pair.Value.PresentationClass !=
                        TetherfallSpirePresentationClass.DistantProxy)
                    {
                        continue;
                    }

                    foreach (var proxyRenderer in
                             pair.Value.GetComponentsInChildren<Renderer>(true))
                    {
                        proxyRenderers.Add(proxyRenderer);
                        foreach (var material in proxyRenderer.sharedMaterials)
                        {
                            if (material != null)
                            {
                                proxyMaterials.Add(material);
                            }
                        }
                    }
                }

                Check(
                    renderers.Length <= ConcurrentRendererBudget,
                    $"Concurrent fixture has {renderers.Length} renderers; budget is " +
                    $"{ConcurrentRendererBudget}.",
                    failures);
                Check(
                    proxyRenderers.Count <=
                    TetherfallSpireArtCatalog.ProxyRendererBudget &&
                    proxyMaterials.Count <=
                    TetherfallSpireArtCatalog.ProxyMaterialBudget,
                    $"Combined distant proxy uses {proxyRenderers.Count} renderers and " +
                    $"{proxyMaterials.Count} materials; total proxy budget is 6/3.",
                    failures);
                Check(
                    root.GetComponentsInChildren<Collider>(true).Length == 0 &&
                    root.GetComponentsInChildren<Collider2D>(true).Length == 0 &&
                    root.GetComponentsInChildren<Light>(true).Length == 0 &&
                    root.GetComponentsInChildren<Camera>(true).Length == 0 &&
                    root.GetComponentsInChildren<AudioListener>(true).Length == 0 &&
                    root.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                    "Concurrent fixture contains forbidden visual components.",
                    failures);
                Check(
                    TetherfallSpireArt.OriginalMaterialKeyCount == 10 &&
                    TetherfallSpireArt.CachedMaterialCount ==
                    TetherfallSpireArt.OriginalMaterialKeyCount &&
                    TetherfallSpireArt.CachedMaterialCount <= 12,
                    $"Original Spire material cache contains " +
                    $"{TetherfallSpireArt.CachedMaterialCount} keys; expected ten and at most 12.",
                    failures);
                VerifySharedOriginalMaterialInstances(root, failures);

                var controller =
                    TetherfallSpireArt.EnsurePresentationController(root, camera);
                Check(controller != null, "Concurrent fixture did not create a controller.", failures);
                if (controller == null)
                {
                    return;
                }

                Check(
                    controller.RegisteredPieceCount == 40 &&
                    controller.RegisteredRendererCount == renderers.Length &&
                    controller.LocalExteriorPieceCount == 11 &&
                    controller.ProxyPieceCount == 2 &&
                    controller.RestVendorPieceCount == 6 &&
                    controller.InteriorPieceCount == 21,
                    $"Controller registration mismatch: pieces {controller.RegisteredPieceCount}, " +
                    $"renderers {controller.RegisteredRendererCount}, local " +
                    $"{controller.LocalExteriorPieceCount}, proxy {controller.ProxyPieceCount}, " +
                    $"rest/vendor {controller.RestVendorPieceCount}, interior " +
                    $"{controller.InteriorPieceCount}.",
                    failures);
                Check(
                    controller.ActiveCullingGroupCount == 2 &&
                    Mathf.Approximately(
                        TetherfallSpirePresentationController.ExteriorNearDistance,
                        70f) &&
                    Mathf.Approximately(
                        TetherfallSpirePresentationController.ExteriorCullDistance,
                        145f) &&
                    Mathf.Approximately(
                        TetherfallSpirePresentationController.RestVendorNearDistance,
                        70f) &&
                    Mathf.Approximately(
                        TetherfallSpirePresentationController.RestVendorCullDistance,
                        110f),
                    "Controller does not own the required 70/145 and 70/110 CullingGroups.",
                    failures);
                VerifyNoFrameLoopMethods(controller, "Spire presentation controller", failures);

                var localPieces = PiecesForPresentationClass(
                    pieces,
                    TetherfallSpirePresentationClass.LocalExterior);
                var proxyPieces = PiecesForPresentationClass(
                    pieces,
                    TetherfallSpirePresentationClass.DistantProxy);
                var restPieces = PiecesForPresentationClass(
                    pieces,
                    TetherfallSpirePresentationClass.CoilhavenOrVendor);
                Check(
                    controller.LocalExteriorVisible &&
                    !controller.ProxyVisible &&
                    AllPieceRenderersEnabled(localPieces) &&
                    !AnyPieceRendererEnabled(proxyPieces),
                    "Near camera did not show local exterior exclusively over the proxy.",
                    failures);
                Check(
                    AllPieceRenderersEnabled(restPieces),
                    "Near camera unexpectedly culled Coilhaven/vendor pieces.",
                    failures);

                camera.transform.position = new Vector3(0f, 10f, -500f);
                controller.RebuildGroups(camera);
                Check(
                    !controller.LocalExteriorVisible &&
                    controller.ProxyVisible &&
                    !AnyPieceRendererEnabled(localPieces) &&
                    AllPieceRenderersEnabled(proxyPieces),
                    "Far camera did not switch exclusively from local exterior to proxy.",
                    failures);
                Check(
                    !AnyPieceRendererEnabled(restPieces),
                    "Far camera did not apply the 110m rest/vendor cull.",
                    failures);
                Check(
                    AllPieceRenderersEnabled(
                        new[]
                        {
                            pieces[TetherfallSpireArtId.ShellStandard],
                            pieces[TetherfallSpireArtId.ShellCombat]
                        }),
                    "Distance culling changed interior activation state.",
                    failures);

                controller.SetAllInteriorsActive(false);
                var standardShell = pieces[TetherfallSpireArtId.ShellStandard];
                var combatShell = pieces[TetherfallSpireArtId.ShellCombat];
                Check(
                    !AnyPieceRendererEnabled(
                        PiecesForPresentationClass(
                            pieces,
                            TetherfallSpirePresentationClass.InteriorActivation)),
                    "SetAllInteriorsActive(false) left an interior renderer enabled.",
                    failures);
                Check(
                    controller.SetInteriorActive(standardShell, true) &&
                    controller.IsInteriorActive(standardShell) &&
                    AllPieceRenderersEnabled(new[] { standardShell }) &&
                    !AnyPieceRendererEnabled(new[] { combatShell }),
                    "Individual interior activation is not director-ready.",
                    failures);

                camera.transform.position = new Vector3(0f, 10f, -45f);
                controller.RebuildGroups(camera);
                Check(
                    AllPieceRenderersEnabled(new[] { standardShell }) &&
                    !AnyPieceRendererEnabled(new[] { combatShell }),
                    "Controller rebuild did not preserve interior activation state.",
                    failures);
                Check(
                    controller.LocalExteriorVisible &&
                    !controller.ProxyVisible &&
                    !(
                        AnyPieceRendererEnabled(localPieces) &&
                        AnyPieceRendererEnabled(proxyPieces)),
                    "Local exterior and distant proxy are not mutually exclusive.",
                    failures);

                controller.SetAllInteriorsActive(true);
                var duplicateController =
                    TetherfallSpireArt.EnsurePresentationController(root, camera);
                Check(
                    ReferenceEquals(controller, duplicateController) &&
                    root.GetComponents<TetherfallSpirePresentationController>().Length == 1,
                    "Repeated presentation setup created another controller.",
                    failures);

                Check(
                    TetherfallSpireArt.ClearAll(root) == 40 &&
                    TetherfallSpireArt.ClearAll(root) == 0 &&
                    root.GetComponentsInChildren<TetherfallSpireArtPiece>(true).Length == 0,
                    "Concurrent fixture cleanup was not complete and idempotent.",
                    failures);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(root.gameObject);
            }
        }

        private static void VerifySharedOriginalMaterialInstances(
            Transform root,
            List<string> failures)
        {
            var byName = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (var visualRenderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in visualRenderer.sharedMaterials)
                {
                    if (material == null ||
                        !material.name.StartsWith(
                            TetherfallSpireArt.OriginalMaterialPrefix,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Check(
                        string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)),
                        $"Original runtime material '{material.name}' unexpectedly points to an " +
                        "asset file.",
                        failures);
                    if (byName.TryGetValue(material.name, out var first))
                    {
                        Check(
                            ReferenceEquals(first, material),
                            $"Original material '{material.name}' was instantiated per piece.",
                            failures);
                    }
                    else
                    {
                        byName.Add(material.name, material);
                    }
                }
            }

            Check(
                byName.Count == TetherfallSpireArt.CachedMaterialCount &&
                byName.Count <= 12,
                $"Fixture references {byName.Count} original materials while the cache reports " +
                $"{TetherfallSpireArt.CachedMaterialCount}.",
                failures);
        }

        private static void VerifyDocumentationAndProvenance(List<string> failures)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var skillRoot = Path.Combine(
                projectRoot,
                ".cursor",
                "skills",
                "stylized-3d-assets");
            var spireDocument = Path.Combine(projectRoot, "INFINITE_SPIRE_ART.md");

            VerifyDocument(
                spireDocument,
                new[]
                {
                    "exactly 40",
                    "stepped civic drum",
                    "external copper lift ribs",
                    "one active teal seam",
                    "70 m / 145 m",
                    "70 m / 110 m",
                    "220",
                    "Quaternius",
                    "CC0",
                    "parent integration",
                    "ART_PROVENANCE.md",
                    "forced procedural fallback",
                    "VerifyFromCommandLine",
                    "VerifyForFullGame"
                },
                failures);
            VerifyDocument(
                Path.Combine(Application.dataPath, "Art", "ART_DIRECTION.md"),
                new[]
                {
                    "Teal is a navigation or Accord signal",
                    "Copper means human-made repair",
                    "Quaternius dressing stays secondary"
                },
                failures);
            VerifyDocument(
                Path.Combine(Application.dataPath, "Art", "ART_PROVENANCE.md"),
                new[]
                {
                    "Quaternius environment MegaKits",
                    "CC0 1.0 Universal",
                    "Resources/Art/Quaternius",
                    "does not modify or claim authorship"
                },
                failures);
            VerifyDocument(
                Path.Combine(skillRoot, "SKILL.md"),
                new[]
                {
                    "Define the gameplay contract first",
                    "ground contact",
                    "colliders",
                    "centralized-culling",
                    "shared materials",
                    "provenance"
                },
                failures);

            if (File.Exists(spireDocument))
            {
                var contents = File.ReadAllText(spireDocument);
                foreach (var definition in TetherfallSpireArtCatalog.All)
                {
                    Check(
                        contents.IndexOf(
                            definition.StableId,
                            StringComparison.Ordinal) >= 0,
                        $"INFINITE_SPIRE_ART.md omits immutable ID " +
                        $"'{definition.StableId}'.",
                        failures);
                }

                Check(
                    contents.IndexOf(
                        "external download",
                        StringComparison.OrdinalIgnoreCase) >= 0 &&
                    contents.IndexOf(
                        "unmodified Quaternius tower",
                        StringComparison.OrdinalIgnoreCase) >= 0,
                    "Spire documentation does not state source/download and hero-tower safety.",
                    failures);
            }
        }

        private static List<SourceMaterialState> CapturePreferredSourceStates(
            TetherfallSpireArtDefinition definition)
        {
            var result = new List<SourceMaterialState>();
            if (!definition.HasPreferredResource)
            {
                return result;
            }

            var prefab = Resources.Load<GameObject>(definition.PreferredResourcePath);
            if (prefab == null)
            {
                return result;
            }

            var seen = new HashSet<Material>();
            foreach (var visualRenderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in visualRenderer.sharedMaterials)
                {
                    if (material != null && seen.Add(material))
                    {
                        result.Add(new SourceMaterialState(material));
                    }
                }
            }

            return result;
        }

        private static void VerifySourceStatesUnchanged(
            IReadOnlyList<SourceMaterialState> states,
            string stableId,
            List<string> failures)
        {
            for (var index = 0; index < states.Count; index++)
            {
                Check(
                    states[index].IsUnchanged(),
                    $"Building '{stableId}' recolored or mutated source material " +
                    $"'{states[index].Name}'.",
                    failures);
            }
        }

        private static void VerifyForbiddenComponents(
            GameObject root,
            string label,
            List<string> failures)
        {
            Check(
                root.GetComponentsInChildren<Collider>(true).Length == 0 &&
                root.GetComponentsInChildren<Collider2D>(true).Length == 0,
                $"{label} contains visual gameplay colliders.",
                failures);
            Check(
                root.GetComponentsInChildren<Light>(true).Length == 0 &&
                root.GetComponentsInChildren<Camera>(true).Length == 0 &&
                root.GetComponentsInChildren<AudioListener>(true).Length == 0 &&
                root.GetComponentsInChildren<AudioSource>(true).Length == 0,
                $"{label} contains a light, camera, or audio component.",
                failures);
            Check(
                root.GetComponentsInChildren<Rigidbody>(true).Length == 0 &&
                root.GetComponentsInChildren<Rigidbody2D>(true).Length == 0,
                $"{label} contains physics bodies.",
                failures);
            Check(
                root.GetComponentsInChildren<Animator>(true).Length == 0 &&
                root.GetComponentsInChildren<Animation>(true).Length == 0 &&
                root.GetComponentsInChildren<ParticleSystem>(true).Length == 0,
                $"{label} contains an animation or particle driver.",
                failures);

            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                Check(
                    behaviour != null && behaviour is TetherfallSpireArtPiece,
                    $"{label} contains unexpected script " +
                    $"'{(behaviour == null ? "MissingScript" : behaviour.GetType().FullName)}'.",
                    failures);
            }
        }

        private static void VerifyNoFrameLoopMethods(
            MonoBehaviour behaviour,
            string label,
            List<string> failures)
        {
            if (behaviour == null)
            {
                return;
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
                    $"{label} component '{type.FullName}' declares per-frame {methodName}.",
                    failures);
            }
        }

        private static void VerifyFiniteHierarchy(
            Transform root,
            string label,
            List<string> failures)
        {
            foreach (var value in root.GetComponentsInChildren<Transform>(true))
            {
                Check(
                    TetherfallSpireArt.IsFinite(value.localPosition) &&
                    TetherfallSpireArt.IsFinite(value.localRotation) &&
                    TetherfallSpireArt.IsFinite(value.localScale) &&
                    MaxAbs(value.localPosition) <= MaximumVisualExtent &&
                    MaxAbs(value.localScale) <= MaximumVisualExtent &&
                    Mathf.Min(
                        Mathf.Abs(value.localScale.x),
                        Mathf.Abs(value.localScale.y),
                        Mathf.Abs(value.localScale.z)) > 0.0001f,
                    $"{label} has an invalid transform at '{GetHierarchyPath(value)}'.",
                    failures);
            }
        }

        private static PieceSnapshot CaptureSnapshot(GameObject root)
        {
            if (root == null)
            {
                return default;
            }

            var signature = new StringBuilder();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < transforms.Length; index++)
            {
                var value = transforms[index];
                if (ReferenceEquals(value, root.transform))
                {
                    continue;
                }

                signature.Append(GetHierarchyPath(value));
                AppendVector(signature, value.localPosition);
                AppendQuaternion(signature, value.localRotation);
                AppendVector(signature, value.localScale);
            }

            var materials = new HashSet<Material>();
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var visualRenderer in renderers)
            {
                signature.Append('|').Append(GetHierarchyPath(visualRenderer.transform));
                foreach (var material in visualRenderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        materials.Add(material);
                        signature.Append('|').Append(material.name);
                    }
                }
            }

            var socketRoot = root.transform.Find(TetherfallSpireArt.SocketRootName);
            return new PieceSnapshot(
                transforms.Length,
                renderers.Length,
                materials.Count,
                socketRoot == null ? 0 : socketRoot.childCount,
                signature.ToString());
        }

        private static void AppendVector(StringBuilder builder, Vector3 value)
        {
            builder.Append('|')
                .Append(value.x.ToString("F4"))
                .Append(',')
                .Append(value.y.ToString("F4"))
                .Append(',')
                .Append(value.z.ToString("F4"));
        }

        private static void AppendQuaternion(StringBuilder builder, Quaternion value)
        {
            builder.Append('|')
                .Append(value.x.ToString("F4"))
                .Append(',')
                .Append(value.y.ToString("F4"))
                .Append(',')
                .Append(value.z.ToString("F4"))
                .Append(',')
                .Append(value.w.ToString("F4"));
        }

        private static List<GameObject> FindSourceChildren(GameObject root)
        {
            var result = new List<GameObject>();
            if (root == null)
            {
                return result;
            }

            foreach (var value in root.GetComponentsInChildren<Transform>(true))
            {
                if (value.name.StartsWith("Quaternius source · ", StringComparison.Ordinal))
                {
                    result.Add(value.gameObject);
                }
            }

            return result;
        }

        private static List<TetherfallSpireArtPiece> PiecesForPresentationClass(
            Dictionary<TetherfallSpireArtId, TetherfallSpireArtPiece> pieces,
            TetherfallSpirePresentationClass presentationClass)
        {
            var result = new List<TetherfallSpireArtPiece>();
            foreach (var pair in pieces)
            {
                if (pair.Value.PresentationClass == presentationClass)
                {
                    result.Add(pair.Value);
                }
            }

            return result;
        }

        private static bool AllPieceRenderersEnabled(
            IEnumerable<TetherfallSpireArtPiece> pieces)
        {
            var found = false;
            foreach (var piece in pieces)
            {
                foreach (var visualRenderer in piece.GetComponentsInChildren<Renderer>(true))
                {
                    found = true;
                    if (!visualRenderer.enabled)
                    {
                        return false;
                    }
                }
            }

            return found;
        }

        private static bool AnyPieceRendererEnabled(
            IEnumerable<TetherfallSpireArtPiece> pieces)
        {
            foreach (var piece in pieces)
            {
                foreach (var visualRenderer in piece.GetComponentsInChildren<Renderer>(true))
                {
                    if (visualRenderer.enabled)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Bounds CalculateRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            var result = new Bounds(root.position, Vector3.zero);
            foreach (var visualRenderer in renderers)
            {
                if (!hasBounds)
                {
                    result = visualRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    result.Encapsulate(visualRenderer.bounds);
                }
            }

            return result;
        }

        private static Vector3 GetHeroFixturePosition(TetherfallSpireArtId artId)
        {
            switch (artId)
            {
                case TetherfallSpireArtId.BasePlinth:
                    return Vector3.zero;
                case TetherfallSpireArtId.CityFlange:
                    return Vector3.up * 6f;
                case TetherfallSpireArtId.TierDrumA:
                    return Vector3.up * 10f;
                case TetherfallSpireArtId.TierDrumB:
                    return Vector3.up * 22f;
                case TetherfallSpireArtId.CoilRibSet:
                    return Vector3.up * 8f;
                case TetherfallSpireArtId.CrownRing:
                    return Vector3.up * 34f;
                case TetherfallSpireArtId.ActiveSeam:
                    return Vector3.up * 21.8f;
                default:
                    return Vector3.zero;
            }
        }

        private static void VerifySocketPosition(
            Transform socketRoot,
            string socketName,
            Vector3 expected,
            string label,
            List<string> failures)
        {
            var socket = socketRoot.Find(socketName);
            if (socket == null)
            {
                return;
            }

            Check(
                Vector3.SqrMagnitude(socket.localPosition - expected) < 0.0001f,
                $"{label} socket '{socketName}' is at {socket.localPosition}, expected " +
                $"{expected}.",
                failures);
        }

        private static int CountDirectPieces(
            Transform parent,
            TetherfallSpireArtId artId)
        {
            var count = 0;
            for (var index = 0; index < parent.childCount; index++)
            {
                var piece = parent.GetChild(index).GetComponent<TetherfallSpireArtPiece>();
                if (piece != null && piece.ArtId == artId)
                {
                    count++;
                }
            }

            return count;
        }

        private static void CheckPrefixCount(
            IReadOnlyList<TetherfallSpireArtDefinition> definitions,
            string prefix,
            int expected,
            List<string> failures)
        {
            var count = 0;
            foreach (var definition in definitions)
            {
                if (definition.StableId.StartsWith(prefix, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            Check(
                count == expected,
                $"Catalog prefix '{prefix}' must contain {expected} IDs, found {count}.",
                failures);
        }

        private static bool IsKnownSocketName(string name)
        {
            foreach (var socketName in AllSocketNames())
            {
                if (string.Equals(name, socketName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] SnapSocketNames()
        {
            return new[]
            {
                TetherfallSpireArt.SnapNorth,
                TetherfallSpireArt.SnapEast,
                TetherfallSpireArt.SnapSouth,
                TetherfallSpireArt.SnapWest,
                TetherfallSpireArt.SnapUp,
                TetherfallSpireArt.SnapDown
            };
        }

        private static string[] AllSocketNames()
        {
            return new[]
            {
                TetherfallSpireArt.SnapNorth,
                TetherfallSpireArt.SnapEast,
                TetherfallSpireArt.SnapSouth,
                TetherfallSpireArt.SnapWest,
                TetherfallSpireArt.SnapUp,
                TetherfallSpireArt.SnapDown,
                TetherfallSpireArt.SocketDoor,
                TetherfallSpireArt.SocketStairUp,
                TetherfallSpireArt.SocketStairDown,
                TetherfallSpireArt.SocketLift,
                TetherfallSpireArt.SocketPuzzle,
                TetherfallSpireArt.SocketTreasure,
                TetherfallSpireArt.SocketVendor,
                TetherfallSpireArt.SocketRest,
                TetherfallSpireArt.SocketBossGate,
                TetherfallSpireArt.SocketWork,
                TetherfallSpireArt.SocketVfx
            };
        }

        private static void VerifyDocument(
            string path,
            IReadOnlyList<string> requiredPhrases,
            List<string> failures)
        {
            Check(File.Exists(path), $"Required document is missing: '{path}'.", failures);
            if (!File.Exists(path))
            {
                return;
            }

            var contents = File.ReadAllText(path);
            foreach (var phrase in requiredPhrases)
            {
                Check(
                    contents.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0,
                    $"Document '{path}' is missing required phrase '{phrase}'.",
                    failures);
            }
        }

        private static string GetHierarchyPath(Transform value)
        {
            if (value == null)
            {
                return "<null>";
            }

            var names = new List<string>();
            while (value != null)
            {
                names.Add(value.name);
                value = value.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static float MaxAbs(Vector3 value)
        {
            return Mathf.Max(
                Mathf.Abs(value.x),
                Mathf.Abs(value.y),
                Mathf.Abs(value.z));
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

        private readonly struct ExpectedIdentity
        {
            public TetherfallSpireArtId ArtId { get; }
            public int NumericValue { get; }
            public string StableId { get; }

            public ExpectedIdentity(
                TetherfallSpireArtId artId,
                int numericValue,
                string stableId)
            {
                ArtId = artId;
                NumericValue = numericValue;
                StableId = stableId;
            }
        }

        private readonly struct HeroPlacement
        {
            public TetherfallSpireArtId ArtId { get; }
            public float Height { get; }

            public HeroPlacement(TetherfallSpireArtId artId, float height)
            {
                ArtId = artId;
                Height = height;
            }
        }

        private readonly struct PieceSnapshot : IEquatable<PieceSnapshot>
        {
            private readonly int transformCount;
            private readonly int rendererCount;
            private readonly int materialCount;
            private readonly int socketCount;
            private readonly string signature;

            public PieceSnapshot(
                int transformCount,
                int rendererCount,
                int materialCount,
                int socketCount,
                string signature)
            {
                this.transformCount = transformCount;
                this.rendererCount = rendererCount;
                this.materialCount = materialCount;
                this.socketCount = socketCount;
                this.signature = signature;
            }

            public bool Equals(PieceSnapshot other)
            {
                return transformCount == other.transformCount &&
                       rendererCount == other.rendererCount &&
                       materialCount == other.materialCount &&
                       socketCount == other.socketCount &&
                       string.Equals(signature, other.signature, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is PieceSnapshot other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = transformCount;
                    hashCode = hashCode * 397 ^ rendererCount;
                    hashCode = hashCode * 397 ^ materialCount;
                    hashCode = hashCode * 397 ^ socketCount;
                    hashCode =
                        hashCode * 397 ^
                        (signature == null ? 0 : signature.GetHashCode());
                    return hashCode;
                }
            }
        }

        private readonly struct SourceMaterialState
        {
            private readonly Material material;
            private readonly Shader shader;
            private readonly Texture mainTexture;
            private readonly Color color;
            private readonly bool hasColor;
            private readonly int renderQueue;

            public string Name { get; }

            public SourceMaterialState(Material material)
            {
                this.material = material;
                shader = material.shader;
                mainTexture = material.mainTexture;
                hasColor = material.HasProperty("_Color");
                color = hasColor ? material.GetColor("_Color") : Color.clear;
                renderQueue = material.renderQueue;
                Name = material.name;
            }

            public bool IsUnchanged()
            {
                if (material == null ||
                    material.shader != shader ||
                    material.mainTexture != mainTexture ||
                    material.renderQueue != renderQueue ||
                    !string.Equals(material.name, Name, StringComparison.Ordinal))
                {
                    return false;
                }

                return !hasColor || Approximately(material.GetColor("_Color"), color);
            }

            private static bool Approximately(Color left, Color right)
            {
                return Mathf.Abs(left.r - right.r) < 0.000001f &&
                       Mathf.Abs(left.g - right.g) < 0.000001f &&
                       Mathf.Abs(left.b - right.b) < 0.000001f &&
                       Mathf.Abs(left.a - right.a) < 0.000001f;
            }
        }
    }
}
