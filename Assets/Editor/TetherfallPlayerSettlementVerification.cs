using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallPlayerSettlementVerification
    {
        private const string MenuPath = "Tetherfall/Verify Player Settlement";

        [MenuItem(MenuPath, false, 2215)]
        public static void Verify()
        {
            VerifyInternal(true);
        }

        public static void VerifyFromCommandLine()
        {
            try
            {
                VerifyInternal(true);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                    return;
                }

                throw;
            }
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
                    "Player-settlement verification is edit-mode only.");
            }

            var failures = new List<string>();
            VerifyCatalog(failures);
            VerifyRecipes(failures);
            VerifyWorkerRoleMapping(failures);
            VerifyBoundedRules(failures);
            VerifyCorruptionNormalization(failures);
            VerifyRuntimeShape(failures);
            VerifyRuntimeIntegration(failures);
            VerifySettlementInputPersistence(failures);
            VerifySourceContract(failures);
            VerifyDocumentation(failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Player settlement verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            if (logSuccess)
            {
                Debug.Log(
                    "Player settlement verification passed: " +
                    $"{TetherfallSettlementCatalog.AllPieces.Count} stable roles, " +
                    $"{TetherfallSettlementCatalog.AllRecipes.Count} station recipes, " +
                    "48-80 m bounded claims, 60% refunds, corruption normalization, " +
                    "8-hour offline caps, NonAlloc placement, centralized simulation, " +
                    "Pact assignment rollback, near-only worker proxies, roster UI, " +
                    "public integration hooks, and documentation are valid.");
            }
        }

        private static void VerifyCatalog(List<string> failures)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var roles = new HashSet<SettlementPieceRole>();
            Check(
                TetherfallSettlementCatalog.AllPieces.Count >= 24,
                "Settlement kit must expose at least 24 stable piece roles.",
                failures);

            foreach (var definition in TetherfallSettlementCatalog.AllPieces)
            {
                Check(definition != null, "Catalog contains a null piece.", failures);
                if (definition == null)
                {
                    continue;
                }

                Check(
                    !string.IsNullOrWhiteSpace(definition.id) &&
                    definition.id.StartsWith("settlement.", StringComparison.Ordinal),
                    $"Piece role {definition.role} has an invalid stable ID.",
                    failures);
                Check(
                    ids.Add(definition.id),
                    $"Duplicate settlement piece ID '{definition.id}'.",
                    failures);
                Check(
                    roles.Add(definition.role),
                    $"Duplicate settlement role '{definition.role}'.",
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(definition.displayName),
                    $"Piece '{definition.id}' has no display name.",
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(definition.artId),
                    $"Piece '{definition.id}' has no art adapter ID.",
                    failures);
                Check(
                    TetherfallSettlementArtCatalog.TryGet(
                        definition.artId,
                        out var artDefinition) &&
                    artDefinition.ArtId.ToString() == definition.role.ToString() &&
                    (int)artDefinition.ArtId == (int)definition.role,
                    $"Piece '{definition.id}' does not map one-to-one to the confirmed art catalog.",
                    failures);
                Check(
                    TetherfallSettlementRules.IsFinite(definition.footprint) &&
                    definition.footprint.x > 0f &&
                    definition.footprint.y > 0f &&
                    definition.footprint.z > 0f,
                    $"Piece '{definition.id}' has an invalid footprint.",
                    failures);
                Check(
                    definition.maximumDurability > 0,
                    $"Piece '{definition.id}' has no durability budget.",
                    failures);
                Check(
                    definition.minimumCoreTier >= 0 &&
                    definition.minimumCoreTier <= 3 &&
                    definition.minimumWayfinderLevel >= 1,
                    $"Piece '{definition.id}' has an invalid progression gate.",
                    failures);
                Check(
                    Mathf.Approximately(definition.RotationStepDegrees, 15f) ||
                    Mathf.Approximately(definition.RotationStepDegrees, 90f),
                    $"Piece '{definition.id}' violates the 15/90-degree rotation policy.",
                    failures);
                Check(
                    definition.maximumSlopeDegrees >= 0f &&
                    definition.maximumSlopeDegrees <= 30f,
                    $"Piece '{definition.id}' has an unsafe slope limit.",
                    failures);
                VerifyCosts(
                    definition.buildCost,
                    $"piece '{definition.id}' build recipe",
                    true,
                    failures);

                if (definition.UsesStructuralSockets)
                {
                    Check(
                        definition.rotationPolicy ==
                        SettlementRotationPolicy.Structural90Degrees,
                        $"Socket structure '{definition.id}' must rotate in 90-degree steps.",
                        failures);
                }

                if (definition.workSlots > 0)
                {
                    Check(
                        definition.workRole != SettlementWorkRole.None,
                        $"Work piece '{definition.id}' has no persistent work role.",
                        failures);
                }
            }

            foreach (SettlementPieceRole expected in Enum.GetValues(
                         typeof(SettlementPieceRole)))
            {
                Check(
                    roles.Contains(expected),
                    $"Settlement kit role '{expected}' is missing.",
                    failures);
            }

            var core = TetherfallSettlementCatalog.FindPiece(
                TetherfallSettlementCatalog.ThreadheartCoreDefinitionId);
            Check(core != null, "Stable Threadheart core definition is missing.", failures);
            Check(
                core != null &&
                core.role == SettlementPieceRole.ThreadheartCore &&
                core.category == SettlementPieceCategory.Core,
                "Stable Threadheart ID does not resolve to the core role.",
                failures);
            Check(
                string.Equals(
                    TetherfallSettlementCatalog.ThreadheartCoreInstanceId,
                    "settlement.instance.threadheart.core",
                    StringComparison.Ordinal),
                "Threadheart runtime instance ID changed.",
                failures);
        }

        private static void VerifyRecipes(List<string> failures)
        {
            var coveredStations = new HashSet<string>(StringComparer.Ordinal);
            var recipeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var recipe in TetherfallSettlementCatalog.AllRecipes)
            {
                Check(recipe != null, "Recipe catalog contains null.", failures);
                if (recipe == null)
                {
                    continue;
                }

                Check(
                    !string.IsNullOrWhiteSpace(recipe.id) &&
                    recipeIds.Add(recipe.id),
                    $"Recipe ID '{recipe.id}' is missing or duplicated.",
                    failures);
                var station = TetherfallSettlementCatalog.FindPiece(
                    recipe.stationDefinitionId);
                Check(
                    station != null && station.IsStation,
                    $"Recipe '{recipe.id}' points to a missing station.",
                    failures);
                coveredStations.Add(recipe.stationDefinitionId);
                Check(
                    recipe.cycleSeconds >= 5f && recipe.cycleSeconds <= 3600f,
                    $"Recipe '{recipe.id}' has an unsafe cycle duration.",
                    failures);
                Check(
                    recipe.minimumCoreTier >= 1 &&
                    recipe.minimumCoreTier <= 3 &&
                    recipe.minimumWayfinderLevel >= 1,
                    $"Recipe '{recipe.id}' has an invalid progression gate.",
                    failures);
                Check(
                    recipe.HasOutput,
                    $"Recipe '{recipe.id}' has no output.",
                    failures);
                VerifyCosts(
                    recipe.inputResources,
                    $"recipe '{recipe.id}' inputs",
                    true,
                    failures);
                VerifyCosts(
                    recipe.outputResources,
                    $"recipe '{recipe.id}' outputs",
                    false,
                    failures);
            }

            var requiredStations = new[]
            {
                TetherfallSettlementCatalog.WorkbenchDefinitionId,
                TetherfallSettlementCatalog.ForgeDefinitionId,
                TetherfallSettlementCatalog.CookpotDefinitionId,
                TetherfallSettlementCatalog.LoomDefinitionId,
                TetherfallSettlementCatalog.MedicineTableDefinitionId
            };
            foreach (var stationId in requiredStations)
            {
                Check(
                    coveredStations.Contains(stationId),
                    $"Production station '{stationId}' has no recipe.",
                    failures);
            }
        }

        private static void VerifyWorkerRoleMapping(List<string> failures)
        {
            Check(
                string.Equals(
                    TetherfallSettlementCatalog.PlayerSettlementId,
                    "settlement.player.threadheart",
                    StringComparison.Ordinal),
                "Player settlement stable ID changed.",
                failures);

            foreach (SettlementWorkRole role in Enum.GetValues(
                         typeof(SettlementWorkRole)))
            {
                if (role == SettlementWorkRole.None)
                {
                    continue;
                }

                var kinds =
                    TetherfallSettlementPactBridge.GetCompatibleWorkKinds(role);
                Check(
                    kinds != null && kinds.Count > 0,
                    $"Settlement work role '{role}' has no PactWorkKind mapping.",
                    failures);
                var unique = new HashSet<PactWorkKind>();
                if (kinds == null)
                {
                    continue;
                }

                for (var index = 0; index < kinds.Count; index++)
                {
                    Check(
                        Enum.IsDefined(typeof(PactWorkKind), kinds[index]) &&
                        unique.Add(kinds[index]),
                        $"Settlement work role '{role}' has an invalid or duplicate mapping.",
                        failures);
                }
            }

            foreach (var piece in TetherfallSettlementCatalog.AllPieces)
            {
                if (piece == null || piece.workSlots <= 0)
                {
                    continue;
                }

                Check(
                    TetherfallSettlementPactBridge.GetCompatibleWorkKinds(
                        piece.workRole).Count > 0,
                    $"Work piece '{piece.id}' has no playable Pact affinity mapping.",
                    failures);
            }

            Check(
                TetherfallSettlementPactBridge.IsWorkKindCompatible(
                    SettlementWorkRole.Farmer,
                    PactWorkKind.Cultivation) &&
                TetherfallSettlementPactBridge.IsWorkKindCompatible(
                    SettlementWorkRole.WaterTender,
                    PactWorkKind.Waterkeeping) &&
                TetherfallSettlementPactBridge.IsWorkKindCompatible(
                    SettlementWorkRole.Watchkeeper,
                    PactWorkKind.Watch) &&
                !TetherfallSettlementPactBridge.IsWorkKindCompatible(
                    SettlementWorkRole.Weaver,
                    PactWorkKind.Survey),
                "Coherent Pact role mapping or mismatch rejection changed.",
                failures);

            var mossbell = TetherfallAccordSpeciesCatalog.Find(
                TetherfallAccordSpeciesCatalog.MossbellStriderId);
            Check(
                TetherfallSettlementPactBridge.TryMatchWorkPreference(
                    mossbell,
                    SettlementWorkRole.Farmer,
                    out var mossbellWork,
                    out var mossbellAffinity) &&
                mossbellWork == PactWorkKind.Cultivation &&
                mossbellAffinity != null &&
                mossbellAffinity.rating == 5,
                "Highest authored matching work affinity is not selected.",
                failures);
            Check(
                !TetherfallSettlementPactBridge.TryMatchWorkPreference(
                    mossbell,
                    SettlementWorkRole.Weaver,
                    out _,
                    out _),
                "A Pact without a matching authored affinity was accepted.",
                failures);

            var pactRecord = new CapturedPactSaveData
            {
                instanceId = "accord.verify.worker",
                stamina = 140f,
                morale = -20f,
                settlementId = "settlement.before",
                stationId = "station.before",
                bedId = "bed.before",
                fieldSlotState = PactFieldSlotState.SettlementPreference,
                fieldSlot = -1
            };
            var worker = new SettlementWorkerAssignmentSaveData();
            TetherfallSettlementPactBridge.InitializeWorkerVitalsFromPact(
                worker,
                pactRecord);
            Check(
                Mathf.Approximately(worker.stamina, 100f) &&
                Mathf.Approximately(worker.morale, 0f),
                "Pact-to-settlement vital initialization is not bounded.",
                failures);
            worker.stamina = float.NaN;
            worker.morale = 77f;
            Check(
                TetherfallSettlementPactBridge.WriteWorkerVitalsToPact(
                    new[] { pactRecord },
                    new SettlementWorkerAssignmentSaveData
                    {
                        pactInstanceId = pactRecord.instanceId,
                        stamina = worker.stamina,
                        morale = worker.morale
                    }) &&
                Mathf.Approximately(pactRecord.stamina, 0f) &&
                Mathf.Approximately(pactRecord.morale, 77f),
                "Settlement-to-Pact vital write-back is not bounded.",
                failures);

            var snapshot = new SettlementPactPreferenceSnapshot(
                pactRecord.settlementId,
                pactRecord.stationId,
                pactRecord.bedId,
                pactRecord.fieldSlotState,
                pactRecord.fieldSlot,
                false,
                string.Empty);
            pactRecord.settlementId = "settlement.changed";
            pactRecord.stationId = "station.changed";
            pactRecord.bedId = "bed.changed";
            TetherfallSettlementPactBridge.RestoreRecordFields(
                pactRecord,
                snapshot);
            Check(
                pactRecord.settlementId == "settlement.before" &&
                pactRecord.stationId == "station.before" &&
                pactRecord.bedId == "bed.before" &&
                pactRecord.fieldSlotState ==
                PactFieldSlotState.SettlementPreference,
                "Pact preference rollback did not restore both assignment sides.",
                failures);

            var saveOwner = new TetherfallSaveData();
            var rosterSnapshot = new PactRosterSaveData();
            rosterSnapshot.pacts.Add(pactRecord.Clone());
            PactRosterSaveBridge.Write(saveOwner, rosterSnapshot);
            TetherfallSettlementPactBridge.WriteWorkerVitalsToPactSave(
                saveOwner,
                new[]
                {
                    new SettlementWorkerAssignmentSaveData
                    {
                        pactInstanceId = pactRecord.instanceId,
                        stamina = 44f,
                        morale = 55f
                    }
                });
            Check(
                PactRosterSaveBridge.TryRead(
                    saveOwner,
                    out var writtenRoster) &&
                writtenRoster.pacts.Count == 1 &&
                Mathf.Approximately(writtenRoster.pacts[0].stamina, 44f) &&
                Mathf.Approximately(writtenRoster.pacts[0].morale, 55f),
                "Settlement vital write-back did not reach the Pact save snapshot.",
                failures);
        }

        private static void VerifyBoundedRules(List<string> failures)
        {
            var previousRadius = 0f;
            var previousPieceCap = 0;
            var previousWorkerCap = 0;
            foreach (var tier in TetherfallSettlementCatalog.AllCoreUpgrades)
            {
                Check(tier != null, "Core upgrade catalog contains null.", failures);
                if (tier == null)
                {
                    continue;
                }

                Check(
                    tier.buildRadius > previousRadius &&
                    tier.buildRadius <= TetherfallSettlementRules.MaximumBuildRadius,
                    $"Core tier {tier.tier} radius is not increasing and bounded.",
                    failures);
                Check(
                    tier.pieceCap > previousPieceCap &&
                    tier.pieceCap <= TetherfallSettlementRules.HardPieceCap,
                    $"Core tier {tier.tier} piece cap is invalid.",
                    failures);
                Check(
                    tier.workerCap > previousWorkerCap &&
                    tier.workerCap <= TetherfallSettlementRules.HardWorkerCap,
                    $"Core tier {tier.tier} worker cap is invalid.",
                    failures);
                Check(
                    tier.baseStorageCapacity <=
                    TetherfallSettlementRules.HardStorageCap,
                    $"Core tier {tier.tier} storage cap is invalid.",
                    failures);
                if (tier.tier > 1)
                {
                    VerifyCosts(
                        tier.cost,
                        $"core tier {tier.tier} upgrade",
                        true,
                        failures);
                }

                previousRadius = tier.buildRadius;
                previousPieceCap = tier.pieceCap;
                previousWorkerCap = tier.workerCap;
            }

            Check(
                Mathf.Approximately(
                    TetherfallSettlementRules.GetBuildRadius(1),
                    48f),
                "Initial Threadheart build radius must remain exactly 48 m.",
                failures);
            Check(
                Mathf.Approximately(
                    TetherfallSettlementRules.GetBuildRadius(3),
                    80f),
                "Maximum Threadheart build radius must remain exactly 80 m.",
                failures);
            Check(
                Mathf.Approximately(TetherfallSettlementRules.GridSize, 2f),
                "Settlement structural grid must remain 2 m.",
                failures);
            Check(
                TetherfallSettlementRules.GetSimulationBand(110f) ==
                SettlementSimulationBand.Near &&
                TetherfallSettlementRules.GetSimulationBand(111f) ==
                SettlementSimulationBand.Far &&
                TetherfallSettlementRules.GetSimulationBand(181f) ==
                SettlementSimulationBand.Abstract,
                "Near/far/abstract simulation thresholds changed.",
                failures);
            Check(
                Mathf.Approximately(
                    TetherfallSettlementRules.GetTickSeconds(
                        SettlementSimulationBand.Near),
                    1f) &&
                Mathf.Approximately(
                    TetherfallSettlementRules.GetTickSeconds(
                        SettlementSimulationBand.Far),
                    10f),
                "Settlement tick cadence must remain 1 s near and 10 s far.",
                failures);
            Check(
                Math.Abs(
                    TetherfallSettlementRules.ClampOfflineSeconds(
                        1L,
                        1L + TimeSpan.FromHours(20d).Ticks) -
                    TimeSpan.FromHours(8d).TotalSeconds) < 0.01d,
                "Offline production does not cap at 8 hours.",
                failures);
            Check(
                TetherfallSettlementRules.GetBoundedCycleCount(
                    100000000d,
                    0.1f) ==
                TetherfallSettlementRules.MaximumCyclesPerAdvance,
                "Production cycle bounding is ineffective.",
                failures);

            var refund = TetherfallSettlementRules.CalculateDemolitionRefund(
                new[]
                {
                    new ResourceCost(ResourceKind.Timber, 10),
                    new ResourceCost(ResourceKind.Stone, 3)
                });
            Check(
                FindAmount(refund, ResourceKind.Timber) == 6 &&
                FindAmount(refund, ResourceKind.Stone) == 1,
                "Demolition refund is not rounded down from exactly 60%.",
                failures);

            var grid = TetherfallSettlementRules.SnapToGrid(
                new Vector3(5.1f, 2f, -2.9f),
                new Vector3(1f, 2f, 1f));
            Check(
                Mathf.Approximately(grid.x, 5f) &&
                Mathf.Approximately(grid.z, -3f),
                "Two-metre grid snapping is not origin-relative.",
                failures);
            Check(
                !TetherfallSettlementForbiddenZones.CanClaimCoreAt(
                    CityCatalog.AlderwakeHearthmarket.center,
                    out _),
                "A Threadheart core can still be claimed inside Alderwake.",
                failures);
            Check(
                !TetherfallSettlementForbiddenZones.CanClaimCoreAt(
                    CityCatalog.VeyrwaterMooring.center,
                    out _),
                "A Threadheart core can still be claimed inside Veyrwater.",
                failures);
            Check(
                ThreadwayCatalog.All.Count > 0 &&
                !TetherfallSettlementForbiddenZones.CanClaimCoreAt(
                    ThreadwayCatalog.All[0].arrivalPosition,
                    out _),
                "A Threadheart core can still block a Threadway arrival.",
                failures);
            Check(
                DungeonCatalog.All.Count > 0 &&
                !TetherfallSettlementForbiddenZones.CanClaimCoreAt(
                    DungeonCatalog.All[0].gatePosition,
                    out _),
                "A Threadheart core can still overlap a dungeon gate.",
                failures);
            Check(
                RestorationCatalog.All.Count > 0 &&
                !TetherfallSettlementForbiddenZones.CanClaimCoreAt(
                    RestorationCatalog.All[0].position,
                    out _),
                "A Threadheart core can still overlap a restoration footprint.",
                failures);
            Check(
                EncounterCatalog.ExtendedPlacements.Count > 0 &&
                !TetherfallSettlementForbiddenZones.CanClaimCoreAt(
                    EncounterCatalog.ExtendedPlacements[0].position,
                    out _),
                "A Threadheart core can still overlap an encounter center.",
                failures);
        }

        private static void VerifyCorruptionNormalization(List<string> failures)
        {
            var coreDefinition = TetherfallSettlementCatalog.FindPiece(
                TetherfallSettlementCatalog.ThreadheartCoreDefinitionId);
            var workbench = TetherfallSettlementCatalog.FindPiece(
                TetherfallSettlementCatalog.WorkbenchDefinitionId);
            var bed = TetherfallSettlementCatalog.FindPiece(
                SettlementPieceRole.PactBed);
            var state = new SettlementSaveData
            {
                coreClaimed = true,
                coreTier = 99,
                lastSimulatedUtcTicks = long.MaxValue,
                nextPieceSequence = -5,
                foodUnits = int.MaxValue,
                waterUnits = -20,
                maintenanceUnits = int.MaxValue
            };
            state.pieces.Add(new SettlementPlacedPieceSaveData
            {
                instanceId = "wrong-core-id",
                definitionId = coreDefinition.id,
                position = Vector3.zero,
                rotationY = float.NaN,
                durability = -100
            });
            state.pieces.Add(new SettlementPlacedPieceSaveData
            {
                instanceId = "duplicate-core",
                definitionId = coreDefinition.id,
                position = Vector3.one,
                durability = 1
            });
            state.pieces.Add(new SettlementPlacedPieceSaveData
            {
                instanceId = "workbench-1",
                definitionId = workbench.id,
                position = new Vector3(2f, 0f, 0f),
                rotationY = 17f,
                durability = int.MaxValue
            });
            state.pieces.Add(new SettlementPlacedPieceSaveData
            {
                instanceId = "bed-1",
                definitionId = bed.id,
                position = new Vector3(4f, 0f, 0f),
                durability = bed.maximumDurability
            });
            state.pieces.Add(new SettlementPlacedPieceSaveData
            {
                instanceId = "unknown",
                definitionId = "settlement.invalid",
                position = Vector3.zero
            });
            state.pieces.Add(new SettlementPlacedPieceSaveData
            {
                instanceId = "non-finite",
                definitionId = workbench.id,
                position = new Vector3(float.PositiveInfinity, 0f, 0f)
            });
            state.storage.Add(new SettlementResourceStackSaveData
            {
                kind = ResourceKind.Timber,
                amount = int.MaxValue
            });
            state.storage.Add(new SettlementResourceStackSaveData
            {
                kind = ResourceKind.Timber,
                amount = int.MaxValue
            });
            for (var index = 0; index < 80; index++)
            {
                state.workers.Add(new SettlementWorkerAssignmentSaveData
                {
                    pactInstanceId = index < 2 ? "duplicate-pact" : $"pact-{index}",
                    workRole = (SettlementWorkRole)999,
                    workPieceId = "missing-piece",
                    bedPieceId = "missing-bed",
                    morale = float.NaN,
                    stamina = float.PositiveInfinity,
                    productionProgressSeconds = double.PositiveInfinity,
                    needProgressSeconds = -1d
                });
            }

            state.stationJobs.Add(new SettlementStationJobSaveData
            {
                stationPieceId = "missing",
                recipeId = "missing",
                queuedCycles = int.MaxValue,
                progressSeconds = double.NaN
            });

            var now = DateTime.UtcNow.Ticks;
            var report = TetherfallSettlementPersistence.NormalizeInPlace(
                state,
                now);
            Check(report.Changed, "Corrupt fixture reported no normalization.", failures);
            Check(
                state.coreClaimed &&
                state.coreTier == 3 &&
                state.pieces.Count <= TetherfallSettlementRules.HardPieceCap,
                "Core/piece normalization failed.",
                failures);
            Check(
                state.pieces.FindAll(piece =>
                    piece.definitionId ==
                    TetherfallSettlementCatalog.ThreadheartCoreDefinitionId).Count == 1 &&
                state.pieces[0].instanceId ==
                TetherfallSettlementCatalog.ThreadheartCoreInstanceId &&
                state.pieces[0].durability == 0,
                "Duplicate core normalization failed.",
                failures);
            Check(
                state.pieces.TrueForAll(piece =>
                    piece != null &&
                    TetherfallSettlementCatalog.FindPiece(piece.definitionId) != null &&
                    TetherfallSettlementRules.IsBoundedWorldPosition(piece.position) &&
                    TetherfallSettlementRules.IsFinite(piece.rotationY)),
                "Invalid or non-finite piece survived normalization.",
                failures);
            Check(
                TetherfallSettlementPersistence.GetStoredResourceTotal(state) <=
                TetherfallSettlementPersistence.GetStorageCapacity(state),
                "Storage normalization exceeded computed capacity.",
                failures);
            Check(
                state.workers.Count <=
                TetherfallSettlementRules.GetWorkerCap(state.coreTier),
                "Worker normalization exceeded the tier cap.",
                failures);
            Check(
                UniqueWorkerIds(state.workers),
                "Duplicate Pact instance IDs survived normalization.",
                failures);
            Check(
                state.workers.TrueForAll(worker =>
                    worker != null &&
                    !string.IsNullOrEmpty(worker.workPieceId) &&
                    !string.IsNullOrEmpty(worker.bedPieceId) &&
                    worker.workRole != SettlementWorkRole.None),
                "Incomplete worker assignment survived normalization.",
                failures);
            Check(
                state.stationJobs.Count == 0,
                "Invalid station job survived normalization.",
                failures);
            Check(
                state.lastSimulatedUtcTicks <= now &&
                state.nextPieceSequence >= 1 &&
                state.waterUnits >= 0,
                "Scalar save normalization failed.",
                failures);
        }

        private static void VerifyRuntimeShape(List<string> failures)
        {
            Check(
                typeof(IWorldInteractable).IsAssignableFrom(
                    typeof(TetherfallSettlementPiece)),
                "Placed settlement pieces do not implement IWorldInteractable.",
                failures);
            Check(
                typeof(MonoBehaviour).IsAssignableFrom(
                    typeof(TetherfallSettlementPiece)),
                "Placed settlement piece is not a runtime component.",
                failures);
            Check(
                !typeof(Component).IsAssignableFrom(
                    typeof(TetherfallSettlementPlacementController)),
                "Placement controller must remain centralized, not a per-piece component.",
                failures);
            var placement = new TetherfallSettlementPlacementController(null, null);
            Check(
                ReadPrivateArrayLength(placement, "cameraHits") >= 32 &&
                ReadPrivateArrayLength(placement, "groundHits") >= 16 &&
                ReadPrivateArrayLength(placement, "overlapHits") >= 64,
                "Placement controller NonAlloc query buffers are missing or undersized.",
                failures);
            Check(
                typeof(TetherfallSettlementPiece).GetMethod(
                    "Update",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly) == null,
                "Placed settlement pieces must not have Update loops.",
                failures);
            Check(
                typeof(TetherfallSettlementPiece).GetMethod(
                    "CollectSnapPoints",
                    BindingFlags.Instance | BindingFlags.Public) != null,
                "Placed structures expose no stable snap sockets.",
                failures);
            Check(
                typeof(TetherfallSettlementPiece).GetProperty(
                    "GameplayColliderCount",
                    BindingFlags.Instance | BindingFlags.Public) != null,
                "Placed pieces expose no deliberate gameplay-collider contract.",
                failures);
            Check(
                typeof(MonoBehaviour).IsAssignableFrom(
                    typeof(TetherfallSettlementPactWorkerProxy)) &&
                typeof(ITetherfallSettlementWorkerProxy).IsAssignableFrom(
                    typeof(TetherfallSettlementPactWorkerProxy)),
                "Built-in Pact worker proxy does not implement the registered proxy seam.",
                failures);
            Check(
                typeof(TetherfallSettlementPactWorkerProxy).GetMethod(
                    "Update",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly) == null,
                "Settlement Pact worker proxies must not have per-proxy Update loops.",
                failures);
            Check(
                typeof(TetherfallSettlementPactWorkerProxy).GetMethod(
                    "TickCentralized",
                    BindingFlags.Instance | BindingFlags.Public) != null &&
                typeof(TetherfallSettlementPiece).GetMethod(
                    "TryGetWorkSocketPose",
                    BindingFlags.Instance | BindingFlags.Public) != null,
                "Central proxy tick or deterministic station work socket is missing.",
                failures);
            Check(
                TetherfallSettlementRules.HardWorkerCap == 24 &&
                TetherfallCapturedPactArt.MaximumCollidersPerActor == 0 &&
                TetherfallCapturedPactArt.MaximumLightsPerActor == 0,
                "Worker proxy, collider, or light budget changed.",
                failures);

            var gameType = typeof(TetherfallGame);
            var requiredMethods = new[]
            {
                "InitializePlayerSettlementSystem",
                "UpdatePlayerSettlementSystem",
                "WritePlayerSettlementSave",
                "RestorePlayerSettlementState",
                "OpenPlayerSettlementPanel",
                "DrawPlayerSettlementPanel",
                "TryAssignPlayerSettlementWorker",
                "TrySetPlayerSettlementWorkerBed",
                "UnassignPlayerSettlementWorker",
                "RegisterPlayerSettlementWorkerProxy"
            };
            foreach (var methodName in requiredMethods)
            {
                Check(
                    gameType.GetMethod(
                        methodName,
                        BindingFlags.Instance | BindingFlags.Public) != null,
                    $"Public integration method '{methodName}' is missing.",
                    failures);
            }

            Check(
                gameType.GetProperty(
                    "CanOpenPlayerSettlement",
                    BindingFlags.Instance | BindingFlags.Public) != null,
                "Public CanOpenPlayerSettlement property is missing.",
                failures);
            Check(
                gameType.GetProperty(
                    "PlayerSettlementVisibleWorkerProxyCount",
                    BindingFlags.Instance | BindingFlags.Public) != null,
                "Public bounded worker-proxy count is missing.",
                failures);
            Check(
                typeof(TetherfallSaveData).GetField(
                    "playerSettlement",
                    BindingFlags.Instance | BindingFlags.Public) != null,
                "TetherfallSaveData partial field 'playerSettlement' is missing.",
                failures);
        }

        private static void VerifyRuntimeIntegration(List<string> failures)
        {
            var saveData = new TetherfallSaveData();
            var pactField = typeof(TetherfallSaveData).GetField(
                "pactRoster",
                BindingFlags.Instance | BindingFlags.Public);
            var settlementField = typeof(TetherfallSaveData).GetField(
                "playerSettlement",
                BindingFlags.Instance | BindingFlags.Public);
            var json = JsonUtility.ToJson(saveData);
            Check(
                TetherfallSaveData.CurrentSchemaVersion == 24 &&
                saveData.schemaVersion == 24,
                "The integrated parent save schema must be 24.",
                failures);
            Check(
                pactField != null &&
                pactField.GetValue(saveData) is PactRosterSaveData &&
                settlementField != null &&
                settlementField.GetValue(saveData) is SettlementSaveData,
                "Pact roster or player settlement save data is not initialized.",
                failures);
            Check(
                json.Contains("\"pactRoster\"") &&
                json.Contains("\"playerSettlement\""),
                "JsonUtility does not see both pactRoster and playerSettlement.",
                failures);

            var scripts = Path.Combine(Application.dataPath, "Scripts");
            var gamePath = Path.Combine(scripts, "TetherfallGame.cs");
            var savePath = Path.Combine(scripts, "TetherfallSaveSystem.cs");
            var inputPath = Path.Combine(scripts, "TetherfallInputBindings.cs");
            var tutorialPath = Path.Combine(scripts, "TetherfallTutorial.cs");
            var pactPath = Path.Combine(scripts, "TetherfallPactRoster.cs");
            Check(File.Exists(gamePath), "Central game source is missing.", failures);
            Check(File.Exists(savePath), "Main save source is missing.", failures);
            Check(File.Exists(inputPath), "Input binding source is missing.", failures);
            Check(File.Exists(tutorialPath), "Tutorial/pause source is missing.", failures);
            Check(File.Exists(pactPath), "Pact roster source is missing.", failures);
            if (!File.Exists(gamePath) ||
                !File.Exists(savePath) ||
                !File.Exists(inputPath) ||
                !File.Exists(tutorialPath) ||
                !File.Exists(pactPath))
            {
                return;
            }

            var game = File.ReadAllText(gamePath);
            var save = File.ReadAllText(savePath);
            var input = File.ReadAllText(inputPath);
            var tutorial = File.ReadAllText(tutorialPath);
            var pact = File.ReadAllText(pactPath);

            Check(
                save.Contains("data.playerSettlement ??=") &&
                save.Contains(
                    "TetherfallSettlementPersistence.CreateDefault(safeUtcTicks);") &&
                save.Contains(
                    "TetherfallSettlementPersistence.NormalizeInPlace(") &&
                save.Contains("safeUtcTicks = DateTime.UtcNow.Ticks"),
                "Main save normalization does not initialize and safely normalize playerSettlement.",
                failures);
            Check(
                CountOccurrences(
                    game,
                    "InitializePlayerSettlementSystem();") == 1 &&
                CountOccurrences(
                    game,
                    "UpdatePlayerSettlementSystem();") == 1 &&
                CountOccurrences(
                    game,
                    "WritePlayerSettlementSave(data);") == 1 &&
                CountOccurrences(
                    game,
                    "RestorePlayerSettlementState(data);") == 1,
                "The central runtime must call each player-settlement lifecycle hook exactly once.",
                failures);
            Check(
                AppearsInOrder(
                    game,
                    "InitializePactRosterSystem();",
                    "InitializePlayerSettlementSystem();",
                    "TetherfallSaveSystem.TryLoad") &&
                AppearsInOrder(
                    game,
                    "UpdatePactRosterSystem();",
                    "UpdatePlayerSettlementSystem();",
                    "HandlePlayerSettlementPlanningInput()",
                    "TryInteract();") &&
                AppearsInOrder(
                    game,
                    "WritePactRosterSave(data);",
                    "WritePlayerSettlementSave(data);",
                    "TetherfallSaveSystem.Save(data)") &&
                AppearsInOrder(
                    game,
                    "RestorePactRosterState(data);",
                    "RestorePlayerSettlementState(data);",
                    "isRestoringSave = false;"),
                "Pact/settlement initialization, update, save, or restore ordering is invalid.",
                failures);
            Check(
                game.Contains(
                    "TetherfallInputAction.OpenSettlementPlanning") &&
                game.Contains("Input.GetKeyDown(KeyCode.Escape)") &&
                game.Contains("return IsPlayerSettlementInputCaptured;") &&
                tutorial.Contains("IsPlayerSettlementInputCaptured") &&
                tutorial.Contains(
                    "TetherfallInputAction.OpenSettlementPlanning"),
                "Settlement planning does not own its toggle/Esc frame or pause gameplay.",
                failures);
            Check(
                game.Contains("internal bool IsModalUiOpen =>") &&
                game.Contains("DrawPlayerSettlementUi(") &&
                game.Contains("IsPlayerSettlementPanelOpen ||") &&
                game.Contains("IsPlayerSettlementPlacementActive") &&
                game.Contains("GUI.matrix = previousMatrix;"),
                "Central modal/UI wiring or GUI-matrix restoration is missing.",
                failures);
            Check(
                input.Contains("OpenSettlementPlanning") &&
                input.Contains("\"Settlement Planning\"") &&
                input.Contains("TetherfallInputBinding.Key(KeyCode.N)"),
                "The remappable Settlement Planning action or default N binding is missing.",
                failures);
            Check(
                pact.Contains("HasClaimedPlayerSettlement") &&
                pact.Contains("GetPlayerSettlementCorePosition()") &&
                pact.Contains("safeRadius * safeRadius"),
                "Pact no-warp safety does not include the player settlement.",
                failures);
        }

        private static void VerifySettlementInputPersistence(
            List<string> failures)
        {
            var createDefaults = typeof(TetherfallInputBindings).GetMethod(
                "CreateDefaultData",
                BindingFlags.Static | BindingFlags.NonPublic);
            var prepareData = typeof(TetherfallInputBindings).GetMethod(
                "TryPrepareData",
                BindingFlags.Static | BindingFlags.NonPublic);
            Check(
                createDefaults != null && prepareData != null,
                "Input persistence normalization helpers are missing.",
                failures);
            if (createDefaults == null || prepareData == null)
            {
                return;
            }

            var defaults =
                createDefaults.Invoke(null, null) as TetherfallInputBindingData;
            var settlementDefault = defaults?.actions?.Find(
                entry =>
                    entry != null &&
                    entry.action ==
                    TetherfallInputAction.OpenSettlementPlanning);
            Check(
                settlementDefault?.bindings != null &&
                settlementDefault.bindings.Count == 1 &&
                settlementDefault.bindings[0].source ==
                    TetherfallInputBindingSource.KeyOrJoystickButton &&
                settlementDefault.bindings[0].keyCode == KeyCode.N,
                "Settlement Planning does not persist with default key N.",
                failures);
            if (defaults == null)
            {
                return;
            }

            defaults.version = 1;
            defaults.actions.RemoveAll(
                entry =>
                    entry != null &&
                    entry.action ==
                    TetherfallInputAction.OpenSettlementPlanning);
            var arguments = new object[] { defaults, null, string.Empty };
            var prepared = (bool)prepareData.Invoke(null, arguments);
            var upgraded = arguments[1] as TetherfallInputBindingData;
            var restored = upgraded?.actions?.Find(
                entry =>
                    entry != null &&
                    entry.action ==
                    TetherfallInputAction.OpenSettlementPlanning);
            Check(
                prepared &&
                restored?.bindings != null &&
                restored.bindings.Count == 1 &&
                restored.bindings[0].keyCode == KeyCode.N,
                "Legacy input data does not receive the Settlement Planning default.",
                failures);
        }

        private static void VerifySourceContract(List<string> failures)
        {
            var scripts = Path.Combine(Application.dataPath, "Scripts");
            var placementPath = Path.Combine(
                scripts,
                "TetherfallSettlementPlacement.cs");
            var playerPath = Path.Combine(
                scripts,
                "TetherfallPlayerSettlement.cs");
            var catalogPath = Path.Combine(
                scripts,
                "TetherfallSettlementCatalog.cs");
            var persistencePath = Path.Combine(
                scripts,
                "TetherfallSettlementPersistence.cs");
            var pactWorkersPath = Path.Combine(
                scripts,
                "TetherfallSettlementPactWorkers.cs");
            Check(File.Exists(placementPath), "Placement source file is missing.", failures);
            Check(File.Exists(playerPath), "Player settlement source file is missing.", failures);
            Check(File.Exists(catalogPath), "Catalog source file is missing.", failures);
            Check(File.Exists(persistencePath), "Persistence source file is missing.", failures);
            Check(File.Exists(pactWorkersPath), "Pact worker bridge source is missing.", failures);
            if (!File.Exists(placementPath) ||
                !File.Exists(playerPath) ||
                !File.Exists(catalogPath) ||
                !File.Exists(persistencePath) ||
                !File.Exists(pactWorkersPath))
            {
                return;
            }

            var placement = File.ReadAllText(placementPath);
            var player = File.ReadAllText(playerPath);
            var catalog = File.ReadAllText(catalogPath);
            var persistence = File.ReadAllText(persistencePath);
            var pactWorkers = File.ReadAllText(pactWorkersPath);
            Check(
                placement.Contains("ScreenPointToRay") &&
                placement.Contains("Physics.RaycastNonAlloc") &&
                placement.Contains("Physics.OverlapBoxNonAlloc"),
                "Placement source lacks camera-ray, ground, or NonAlloc overlap validation.",
                failures);
            Check(
                placement.Contains("Input.GetMouseButtonDown(0)") &&
                placement.Contains("Input.GetMouseButtonDown(1)") &&
                placement.Contains("TetherfallSettlementLegacyPlacementInput"),
                "LMB/RMB legacy placement input is missing or not isolated.",
                failures);
            Check(
                !player.Contains("Input.") &&
                !catalog.Contains("Input.") &&
                !persistence.Contains("Input.") &&
                !pactWorkers.Contains("Input."),
                "Legacy Input escaped the isolated placement adapter.",
                failures);
            Check(
                placement.Contains("Snap_N") &&
                placement.Contains("Snap_E") &&
                placement.Contains("Snap_S") &&
                placement.Contains("Snap_W") &&
                placement.Contains("Snap_Up") &&
                placement.Contains("Snap_Down"),
                "Required structural socket names are missing.",
                failures);
            Check(
                placement.Contains("openingWidth") &&
                placement.Contains("Gameplay Collider · Traversable slope") &&
                placement.Contains("gameplayColliders"),
                "Door, gate, stair, or ramp gameplay collider policy is missing.",
                failures);
            Check(
                !Regex.IsMatch(
                    placement,
                    @"\bvoid\s+Update\s*\(") &&
                !Regex.IsMatch(
                    catalog,
                    @"\bvoid\s+Update\s*\(") &&
                !Regex.IsMatch(
                    persistence,
                    @"\bvoid\s+Update\s*\(") &&
                !Regex.IsMatch(
                    pactWorkers,
                    @"\bvoid\s+Update\s*\("),
                "A settlement support type introduced a per-piece Update loop.",
                failures);
            Check(
                placement.Contains("TetherfallSettlementArtAdapter.Build") &&
                placement.Contains("TetherfallSettlementArt.Build(artId, parent)") &&
                placement.Contains("BuildFallback("),
                "Confirmed settlement art and fallback are not routed through the single adapter seam.",
                failures);
            Check(
                player.Contains("RequestSave()") &&
                player.Contains("RequestCriticalSave()") &&
                player.Contains("CanAfford(") &&
                player.Contains("SpendCost("),
                "Partial-class integration does not use established save/cost patterns.",
                failures);

            var pactAssignmentCall = player.IndexOf(
                "TryAssignPactSettlementPreference(",
                StringComparison.Ordinal);
            var settlementAssignmentAdd = player.IndexOf(
                "playerSettlementState.workers.Add(worker)",
                StringComparison.Ordinal);
            Check(
                pactAssignmentCall >= 0 &&
                settlementAssignmentAdd > pactAssignmentCall &&
                player.Contains("RollbackPactPreference("),
                "Pact preference is not committed first with explicit settlement rollback.",
                failures);
            Check(
                Regex.Matches(
                    player,
                    "ClearPactSettlementPreference\\(").Count >= 2 &&
                Regex.Matches(
                    player,
                    "RemovePlayerSettlementAssignmentsForPiece\\(").Count >= 3 &&
                player.Contains("TrySetPlayerSettlementWorkerBed(") &&
                player.Contains("return TryAssignPlayerSettlementWorker("),
                "Unassignment, broken-piece cleanup, demolition, or bed synchronization is missing.",
                failures);
            var simulationStart = player.IndexOf(
                "private SettlementSimulationSummary SimulatePlayerSettlement(",
                StringComparison.Ordinal);
            var simulationEnd = simulationStart >= 0
                ? player.IndexOf(
                    "private void UpdatePlayerSettlementWorkerNeeds(",
                    simulationStart,
                    StringComparison.Ordinal)
                : -1;
            var simulationSource =
                simulationStart >= 0 && simulationEnd > simulationStart
                    ? player.Substring(
                        simulationStart,
                        simulationEnd - simulationStart)
                    : string.Empty;
            var writeStart = player.IndexOf(
                "public void WritePlayerSettlementSave(",
                StringComparison.Ordinal);
            var writeEnd = writeStart >= 0
                ? player.IndexOf(
                    "public void RestorePlayerSettlementState(",
                    writeStart,
                    StringComparison.Ordinal)
                : -1;
            var writeSource =
                writeStart >= 0 && writeEnd > writeStart
                    ? player.Substring(writeStart, writeEnd - writeStart)
                    : string.Empty;
            var vitalWrite = simulationSource.IndexOf(
                "SynchronizeAllPlayerSettlementWorkerVitalsToPact();",
                StringComparison.Ordinal);
            var saveVitalWrite = writeSource.IndexOf(
                "SynchronizeAllPlayerSettlementWorkerVitalsToPact();",
                StringComparison.Ordinal);
            var saveSnapshotWrite = writeSource.IndexOf(
                "WriteWorkerVitalsToPactSave(",
                StringComparison.Ordinal);
            Check(
                vitalWrite >= 0 &&
                saveVitalWrite >= 0 &&
                saveSnapshotWrite > saveVitalWrite,
                "Live, offline, or save-time Pact vital synchronization is missing.",
                failures);
            Check(
                catalog.Contains(
                    "PlayerSettlementId = \"settlement.player.threadheart\"") &&
                player.Contains(
                    "TetherfallSettlementCatalog.PlayerSettlementId") &&
                pactWorkers.Contains(
                    "TetherfallSettlementCatalog.PlayerSettlementId"),
                "Settlement assignment uses display text instead of its stable ID.",
                failures);
            Check(
                catalog.Contains("class TetherfallSettlementForbiddenZones") &&
                catalog.Contains("CityCatalog.AlderwakeHearthmarket") &&
                catalog.Contains("CityCatalog.VeyrwaterMooring") &&
                catalog.Contains("ThreadwayCatalog.All") &&
                catalog.Contains("DungeonCatalog.All") &&
                catalog.Contains("EncounterCatalog.ExtendedPlacements") &&
                catalog.Contains("RestorationCatalog.All") &&
                !catalog.Contains("FindObjects") &&
                player.Contains(
                    "TetherfallSettlementForbiddenZones.CanClaimCoreAt("),
                "Core claims are not routed through the bounded catalog-based forbidden-zone helper.",
                failures);
            Check(
                pactWorkers.Contains("PactRoster") &&
                pactWorkers.Contains("workAffinities") &&
                pactWorkers.Contains("GUI.BeginScrollView") &&
                pactWorkers.Contains("Species:") &&
                pactWorkers.Contains("Stamina") &&
                pactWorkers.Contains("Morale") &&
                pactWorkers.Contains("FIELD SLOT") &&
                pactWorkers.Contains("RESERVE") &&
                pactWorkers.Contains("SETTLEMENT") &&
                pactWorkers.Contains("Rank 2/4/6 total field capacity") &&
                pactWorkers.Contains("PactConcordMultiplier") &&
                pactWorkers.Contains("TryAssignPlayerSettlementWorker(") &&
                !pactWorkers.Contains("DrawPactRosterSection("),
                "Workers page does not expose the bounded playable Accord assignment flow.",
                failures);
            Check(
                pactWorkers.Contains("TetherfallCapturedPactArt.Build(") &&
                pactWorkers.Contains("RegisterPlayerSettlementWorkerProxy(") &&
                pactWorkers.Contains("TickCentralized(") &&
                pactWorkers.Contains("artRig.Tick(") &&
                pactWorkers.Contains("TryGetWorkSocketPose(") &&
                pactWorkers.Contains("HardWorkerCap") &&
                Regex.IsMatch(
                    player,
                    @"settlementWorkerProxies\.Count\s*>=\s*" +
                    @"TetherfallSettlementRules\.HardWorkerCap") &&
                pactWorkers.Contains("RemoveForbiddenProxyComponents(") &&
                pactWorkers.Contains("DestroyAllPlayerSettlementPactWorkerProxies(") &&
                !pactWorkers.Contains("Physics.") &&
                !pactWorkers.Contains("AddComponent<Collider>") &&
                !pactWorkers.Contains("AddComponent<Light>") &&
                !pactWorkers.Contains("AddComponent<Rigidbody>"),
                "Near-only worker proxies violate art, registration, physics, or budget policy.",
                failures);
            var proxyTickStart = pactWorkers.IndexOf(
                "public void TickCentralized(",
                StringComparison.Ordinal);
            var proxyTickEnd = proxyTickStart >= 0
                ? pactWorkers.IndexOf(
                    "public void Retire(",
                    proxyTickStart,
                    StringComparison.Ordinal)
                : -1;
            var proxyTickSource =
                proxyTickStart >= 0 && proxyTickEnd > proxyTickStart
                    ? pactWorkers.Substring(
                        proxyTickStart,
                        proxyTickEnd - proxyTickStart)
                    : string.Empty;
            Check(
                !string.IsNullOrEmpty(proxyTickSource) &&
                !proxyTickSource.Contains("new ") &&
                !proxyTickSource.Contains("GetComponents") &&
                !proxyTickSource.Contains("Find"),
                "Central worker-proxy tick performs allocating or scene-search work.",
                failures);
        }

        private static void VerifyDocumentation(List<string> failures)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            var path = projectRoot != null
                ? Path.Combine(projectRoot, "PLAYER_SETTLEMENT.md")
                : string.Empty;
            Check(
                !string.IsNullOrEmpty(path) && File.Exists(path),
                "PLAYER_SETTLEMENT.md is missing.",
                failures);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            var text = File.ReadAllText(path);
            var requiredPhrases = new[]
            {
                "InitializePlayerSettlementSystem()",
                "UpdatePlayerSettlementSystem()",
                "WritePlayerSettlementSave",
                "RestorePlayerSettlementState",
                "DrawPlayerSettlementPanel",
                "CanOpenPlayerSettlement",
                "OpenSettlementPlanning",
                "schema 24",
                "LMB",
                "RMB",
                "8 hours",
                "TetherfallSettlementArt.Build(artId, parent)",
                "partial",
                "settlement.player.threadheart",
                "TryAssignPactSettlementPreference",
                "ClearPactSettlementPreference",
                "TickCentralized",
                "TetherfallSettlementForbiddenZones",
                "Rank 2/4/6"
            };
            foreach (var phrase in requiredPhrases)
            {
                Check(
                    text.Contains(phrase),
                    $"PLAYER_SETTLEMENT.md is missing '{phrase}'.",
                    failures);
            }
        }

        private static void VerifyCosts(
            List<ResourceCost> costs,
            string owner,
            bool requireAtLeastOne,
            List<string> failures)
        {
            Check(
                costs != null,
                $"{owner} has a null ResourceCost list.",
                failures);
            if (costs == null)
            {
                return;
            }

            if (requireAtLeastOne)
            {
                Check(costs.Count > 0, $"{owner} has no ResourceCost entries.", failures);
            }

            var seen = new HashSet<ResourceKind>();
            foreach (var cost in costs)
            {
                Check(cost != null, $"{owner} contains a null ResourceCost.", failures);
                if (cost == null)
                {
                    continue;
                }

                Check(
                    Enum.IsDefined(typeof(ResourceKind), cost.kind) &&
                    cost.amount > 0,
                    $"{owner} contains an invalid ResourceCost.",
                    failures);
                Check(
                    seen.Add(cost.kind),
                    $"{owner} repeats ResourceKind {cost.kind}.",
                    failures);
            }
        }

        private static int FindAmount(
            List<ResourceCost> costs,
            ResourceKind kind)
        {
            var cost = costs?.Find(candidate =>
                candidate != null && candidate.kind == kind);
            return cost != null ? cost.amount : 0;
        }

        private static bool UniqueWorkerIds(
            List<SettlementWorkerAssignmentSaveData> workers)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var worker in workers)
            {
                if (worker == null ||
                    string.IsNullOrWhiteSpace(worker.pactInstanceId) ||
                    !ids.Add(worker.pactInstanceId))
                {
                    return false;
                }
            }

            return true;
        }

        private static int ReadPrivateArrayLength(object owner, string fieldName)
        {
            var field = owner?.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(owner) is Array values ? values.Length : 0;
        }

        private static bool AppearsInOrder(
            string source,
            params string[] markers)
        {
            var searchStart = 0;
            for (var index = 0; index < markers.Length; index++)
            {
                var markerIndex = source.IndexOf(
                    markers[index],
                    searchStart,
                    StringComparison.Ordinal);
                if (markerIndex < 0)
                {
                    return false;
                }

                searchStart = markerIndex + markers[index].Length;
            }

            return true;
        }

        private static int CountOccurrences(string source, string marker)
        {
            var count = 0;
            var searchStart = 0;
            while (searchStart < source.Length)
            {
                var markerIndex = source.IndexOf(
                    marker,
                    searchStart,
                    StringComparison.Ordinal);
                if (markerIndex < 0)
                {
                    break;
                }

                count++;
                searchStart = markerIndex + marker.Length;
            }

            return count;
        }

        private static void Check(
            bool condition,
            string failure,
            List<string> failures)
        {
            if (!condition)
            {
                failures.Add(failure);
            }
        }
    }
}
