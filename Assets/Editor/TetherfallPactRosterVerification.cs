using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallPactRosterVerification
    {
        private const string MenuPath = "Tetherfall/Verify Pact Roster Foundation";

        [MenuItem(MenuPath, false, 2218)]
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
                    "Pact roster verification is edit-mode only.");
            }

            var failures = new List<string>();
            VerifyCatalog(failures);
            VerifyForbiddenEligibility(failures);
            VerifyRosterClampAndDedupe(failures);
            VerifySlotMilestones(failures);
            VerifyAffinityDiversity(failures);
            VerifyStableSaveNormalization(failures);
            VerifySerializedRuntimeIntegration(failures);
            VerifyStoryFieldAdapter(failures);
            VerifyWildEncounterIntegration(failures);
            VerifyActorBudgets(failures);
            VerifyCopy(failures);
            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Pact roster foundation verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            if (logSuccess)
            {
                Debug.Log(
                    "Pact roster foundation verification passed: stable species and gameplay enemy IDs, " +
                    "forbidden eligibility, five-slot rank clamps, affinity Concord, deterministic save " +
                    "normalization, schema-24 lifecycle hooks, unified story adapters, six wild Threadbind " +
                    "encounters, bounded nonalloc actors, and consent-led copy are valid.");
            }
        }

        private static void VerifyCatalog(List<string> failures)
        {
            Check(
                TetherfallAccordSpeciesCatalog.Wild.Count >= 6,
                $"Wild species catalog has {TetherfallAccordSpeciesCatalog.Wild.Count}; expected at least 6.",
                failures);

            var speciesIds = new HashSet<string>(StringComparer.Ordinal);
            var enemyIds = new HashSet<string>(StringComparer.Ordinal);
            var artIds = new HashSet<string>(StringComparer.Ordinal);
            var regionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < TetherfallAccordSpeciesCatalog.All.Count; index++)
            {
                var definition = TetherfallAccordSpeciesCatalog.All[index];
                Check(definition != null, "Species catalog contains a null definition.", failures);
                if (definition == null)
                {
                    continue;
                }

                Check(
                    !string.IsNullOrWhiteSpace(definition.speciesId) &&
                    definition.speciesId.StartsWith("species.", StringComparison.Ordinal),
                    "A species definition is missing its stable species ID.",
                    failures);
                Check(
                    speciesIds.Add(definition.speciesId),
                    $"Duplicate species ID '{definition.speciesId}'.",
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(definition.displayName),
                    $"Species '{definition.speciesId}' has no display name.",
                    failures);
                Check(
                    definition.baseStats != null &&
                    definition.baseStats.vitality > 0 &&
                    definition.baseStats.power > 0 &&
                    definition.baseStats.guard >= 0 &&
                    IsFinite(definition.baseStats.speed) &&
                    definition.baseStats.speed > 0f &&
                    IsFinite(definition.baseStats.assistRange) &&
                    definition.baseStats.assistRange >= 2f &&
                    definition.baseStats.assistRange <= 14f &&
                    IsFinite(definition.baseStats.assistCooldown) &&
                    definition.baseStats.assistCooldown >= 1.5f,
                    $"Species '{definition.speciesId}' has invalid base combat stats.",
                    failures);
                Check(
                    definition.movement != null &&
                    !string.IsNullOrWhiteSpace(definition.movement.traversalCharacter) &&
                    IsFinite(definition.movement.formationSpacing) &&
                    definition.movement.formationSpacing > 0f,
                    $"Species '{definition.speciesId}' has an incomplete movement profile.",
                    failures);
                Check(
                    definition.workAffinities != null &&
                    definition.workAffinities.Length >= 2 &&
                    definition.workAffinities.Length <= 4,
                    $"Species '{definition.speciesId}' needs 2-4 work affinities.",
                    failures);
                VerifyWorkAffinities(definition, failures);
                VerifyArtProfile(definition, artIds, failures);
                Check(
                    IsFinite(definition.baseStamina) &&
                    definition.baseStamina >= 0f &&
                    definition.baseStamina <= 100f &&
                    IsFinite(definition.baseMorale) &&
                    definition.baseMorale >= 0f &&
                    definition.baseMorale <= 100f,
                    $"Species '{definition.speciesId}' has invalid stamina or morale.",
                    failures);

                if (!definition.isWild)
                {
                    Check(
                        string.IsNullOrEmpty(definition.eligibleEnemyId) &&
                        !string.IsNullOrEmpty(definition.linkedLegendaryPactId),
                        $"Story-link species '{definition.speciesId}' must not be defeat-eligible.",
                        failures);
                    continue;
                }

                Check(
                    definition.speciesId.StartsWith("species.wild.", StringComparison.Ordinal),
                    $"Wild species '{definition.speciesId}' does not use the wild namespace.",
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(definition.eligibleEnemyId) &&
                    definition.eligibleEnemyId.StartsWith("enemy.wild.", StringComparison.Ordinal),
                    $"Wild species '{definition.speciesId}' lacks a dedicated gameplay enemy ID.",
                    failures);
                Check(
                    enemyIds.Add(definition.eligibleEnemyId),
                    $"Duplicate eligible enemy ID '{definition.eligibleEnemyId}'.",
                    failures);
                Check(
                    ReferenceEquals(
                        TetherfallAccordSpeciesCatalog.FindByEligibleEnemyId(
                            definition.eligibleEnemyId),
                        definition),
                    $"Eligible enemy ID '{definition.eligibleEnemyId}' does not reverse-resolve.",
                    failures);
                if (!regionCounts.TryGetValue(definition.regionId, out var regionCount))
                {
                    regionCount = 0;
                }

                regionCounts[definition.regionId] = regionCount + 1;
            }

            Check(
                regionCounts.Count >= 3,
                "Wild species do not span the three established regional themes.",
                failures);
            Check(
                regionCounts.TryGetValue("region.alderwake", out var alderwakeCount) &&
                alderwakeCount >= 2,
                "Alderwake needs at least two new wild species.",
                failures);
            Check(
                regionCounts.TryGetValue("region.morrowfen", out var morrowfenCount) &&
                morrowfenCount >= 2,
                "Morrowfen needs at least two new wild species.",
                failures);
            Check(
                regionCounts.TryGetValue("region.cinderwold", out var cinderwoldCount) &&
                cinderwoldCount >= 2,
                "Cinderwold needs at least two new wild species.",
                failures);
        }

        private static void VerifyWorkAffinities(
            PactSpeciesDefinition definition,
            List<string> failures)
        {
            if (definition.workAffinities == null)
            {
                return;
            }

            var workKinds = new HashSet<PactWorkKind>();
            for (var index = 0; index < definition.workAffinities.Length; index++)
            {
                var work = definition.workAffinities[index];
                Check(
                    work != null,
                    $"Species '{definition.speciesId}' has a null work affinity.",
                    failures);
                if (work == null)
                {
                    continue;
                }

                Check(
                    workKinds.Add(work.kind),
                    $"Species '{definition.speciesId}' repeats work affinity '{work.kind}'.",
                    failures);
                Check(
                    work.rating >= 1 && work.rating <= 5,
                    $"Species '{definition.speciesId}' has work rating {work.rating}; expected 1-5.",
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(work.voluntaryPreference),
                    $"Species '{definition.speciesId}' lacks voluntary preference copy.",
                    failures);
            }
        }

        private static void VerifyArtProfile(
            PactSpeciesDefinition definition,
            HashSet<string> artIds,
            List<string> failures)
        {
            var art = definition.art;
            Check(art != null, $"Species '{definition.speciesId}' has no art profile.", failures);
            if (art == null)
            {
                return;
            }

            Check(
                !string.IsNullOrWhiteSpace(art.artProfileId) &&
                artIds.Add(art.artProfileId),
                $"Species '{definition.speciesId}' has a missing or duplicate art profile ID.",
                failures);
            Check(
                IsFinite(art.bodyScale) &&
                art.bodyScale.x > 0f &&
                art.bodyScale.y > 0f &&
                art.bodyScale.z > 0f &&
                IsFinite(art.headScale) &&
                art.headScale.x > 0f &&
                art.headScale.y > 0f &&
                art.headScale.z > 0f,
                $"Species '{definition.speciesId}' has invalid art proportions.",
                failures);
            Check(
                art.silhouetteDirection?.Length >= 40 &&
                art.materialDirection?.Length >= 40 &&
                art.articulationDirection?.Length >= 40,
                $"Species '{definition.speciesId}' art direction is not sufficiently authored.",
                failures);
            Check(
                IsFinite(art.surfaceColor) &&
                IsFinite(art.accentColor) &&
                IsFinite(art.resonanceColor) &&
                IsFinite(art.eyeColor),
                $"Species '{definition.speciesId}' art palette is non-finite.",
                failures);
        }

        private static void VerifyForbiddenEligibility(List<string> failures)
        {
            var forbiddenIds = new[]
            {
                "enemy.hollow_raider",
                "enemy.hollow_scout",
                "enemy.relay_breaker",
                "enemy.elite.cindermaw",
                "boss.skyvault",
                "runtime.story_actor",
                TetherfallPactCatalog.BrambletId,
                "enemy.wild.unmapped"
            };
            for (var index = 0; index < forbiddenIds.Length; index++)
            {
                Check(
                    !TetherfallAccordSpeciesCatalog.IsEligibleWildEnemyId(
                        forbiddenIds[index]),
                    $"Forbidden ID '{forbiddenIds[index]}' became roster-eligible.",
                    failures);
            }

            for (var index = 0; index < TetherfallAccordSpeciesCatalog.Wild.Count; index++)
            {
                var definition = TetherfallAccordSpeciesCatalog.Wild[index];
                Check(
                    TetherfallAccordSpeciesCatalog.IsEligibleWildEnemyId(
                        definition.eligibleEnemyId),
                    $"Dedicated future ID '{definition.eligibleEnemyId}' was rejected.",
                    failures);
            }
        }

        private static void VerifyRosterClampAndDedupe(List<string> failures)
        {
            var roster = new PactRosterSaveData();
            for (var index = 0; index < TetherfallAccordSpeciesCatalog.Wild.Count; index++)
            {
                var record = CreateRecord(
                    TetherfallAccordSpeciesCatalog.Wild[index],
                    $"accord.verify.{index}");
                roster.pacts.Add(record);
                roster.activeFieldInstanceIds.Add(record.instanceId);
            }

            roster.pacts.Add(roster.pacts[0].Clone());
            roster.activeFieldInstanceIds.Insert(1, roster.pacts[0].instanceId);
            var assigned = roster.pacts[roster.pacts.Count - 2];
            assigned.settlementId = "city.verify";
            assigned.stationId = "station.verify";
            roster.commandFocusInstanceId = assigned.instanceId;
            PactRosterNormalization.NormalizeInPlace(roster, 6);

            Check(
                roster.pacts.Count == TetherfallAccordSpeciesCatalog.Wild.Count,
                "Duplicate roster records were not removed.",
                failures);
            Check(
                roster.activeFieldInstanceIds.Count <=
                PactRosterRules.AbsoluteMaximumFieldUnits,
                "Active roster exceeded the absolute five-unit budget.",
                failures);
            Check(
                !roster.activeFieldInstanceIds.Contains(assigned.instanceId),
                "A settlement-assigned worker remained active in the field.",
                failures);
            Check(
                assigned.fieldSlotState == PactFieldSlotState.SettlementPreference &&
                assigned.fieldSlot == -1,
                "Settlement assignment did not suppress the field slot.",
                failures);

            var activeIds = new HashSet<string>(
                roster.activeFieldInstanceIds,
                StringComparer.Ordinal);
            Check(
                activeIds.Count == roster.activeFieldInstanceIds.Count,
                "Active field IDs were not deduplicated.",
                failures);
            for (var index = 0; index < roster.activeFieldInstanceIds.Count; index++)
            {
                var record = PactRosterRules.FindRecord(
                    roster.pacts,
                    roster.activeFieldInstanceIds[index]);
                Check(
                    record != null &&
                    record.fieldSlotState == PactFieldSlotState.ActiveField &&
                    record.fieldSlot == index,
                    $"Active field slot {index} did not normalize consistently.",
                    failures);
            }

            Check(
                string.IsNullOrEmpty(roster.commandFocusInstanceId) ||
                activeIds.Contains(roster.commandFocusInstanceId),
                "Command focus was not repaired to an active field ID.",
                failures);
        }

        private static void VerifySlotMilestones(List<string> failures)
        {
            var expected = new[]
            {
                2,
                2,
                3,
                3,
                4,
                4,
                5,
                5
            };
            for (var rank = 0; rank < expected.Length; rank++)
            {
                Check(
                    PactRosterRules.GetMaximumActiveSlots(rank) == expected[rank],
                    $"Clear rank {rank} returned " +
                    $"{PactRosterRules.GetMaximumActiveSlots(rank)} slots; " +
                    $"expected {expected[rank]}.",
                    failures);
            }

            Check(
                PactRosterRules.GetMaximumActiveSlots(100) == 5,
                "High clear rank exceeded the five-unit field cap.",
                failures);
        }

        private static void VerifyAffinityDiversity(List<string> failures)
        {
            var homogeneous = new PactRosterSaveData();
            var varied = new PactRosterSaveData();
            for (var index = 0; index < 5; index++)
            {
                var repeated = CreateRecord(
                    TetherfallAccordSpeciesCatalog.Wild[0],
                    $"accord.same.{index}");
                homogeneous.pacts.Add(repeated);
                homogeneous.activeFieldInstanceIds.Add(repeated.instanceId);

                var diverse = CreateRecord(
                    TetherfallAccordSpeciesCatalog.Wild[index],
                    $"accord.varied.{index}");
                varied.pacts.Add(diverse);
                varied.activeFieldInstanceIds.Add(diverse.instanceId);
            }

            PactRosterNormalization.NormalizeInPlace(homogeneous, 6);
            PactRosterNormalization.NormalizeInPlace(varied, 6);
            var homogeneousMultiplier =
                PactRosterRules.GetConcordMultiplier(homogeneous);
            var variedMultiplier = PactRosterRules.GetConcordMultiplier(varied);
            Check(
                Mathf.Approximately(homogeneousMultiplier, 1f),
                "Repeated affinity incorrectly increased Concord.",
                failures);
            Check(
                variedMultiplier > homogeneousMultiplier,
                "Varied active affinities did not improve Concord.",
                failures);
            Check(
                variedMultiplier <= PactRosterRules.MaximumConcordMultiplier,
                "Concord exceeded its bounded multiplier.",
                failures);
        }

        private static void VerifyStableSaveNormalization(List<string> failures)
        {
            var definition = TetherfallAccordSpeciesCatalog.Wild[0];
            var roster = new PactRosterSaveData
            {
                schemaVersion = -5,
                commandFocusInstanceId = "missing",
                lastAbstractUpdateUtcTicks = -1
            };
            roster.pacts.Add(new CapturedPactSaveData
            {
                speciesId = definition.speciesId,
                nickname = "  Rainstep  ",
                level = -2,
                experience = -5,
                bond = 999,
                stamina = float.NaN,
                morale = float.PositiveInfinity,
                originId = "  origin.verify  ",
                accordFormedUtcTicks = 1234
            });
            PactRosterNormalization.NormalizeInPlace(roster, 1);
            var stableId = roster.pacts[0].instanceId;
            var firstJson = JsonUtility.ToJson(roster);
            PactRosterNormalization.NormalizeInPlace(roster, 1);
            var secondJson = JsonUtility.ToJson(roster);
            Check(
                !string.IsNullOrEmpty(stableId) &&
                stableId == roster.pacts[0].instanceId,
                "Missing instance ID did not receive a stable migration ID.",
                failures);
            Check(
                string.Equals(firstJson, secondJson, StringComparison.Ordinal),
                "Roster normalization was not idempotent.",
                failures);
            Check(
                roster.pacts[0].nickname == "Rainstep" &&
                roster.pacts[0].level == 1 &&
                roster.pacts[0].experience == 0 &&
                roster.pacts[0].bond == 100 &&
                roster.pacts[0].stamina == 0f &&
                roster.pacts[0].morale == 0f,
                "Malformed record fields were not safely clamped.",
                failures);

            var legendary = new PactProgressSaveData
            {
                pactId = TetherfallPactCatalog.BrambletId,
                bondXp = 87,
                bondRank = 3,
                unfoldedFormId = TetherfallPactCatalog.GrovewardenFormId,
                awardedSourceIds = new List<string> { PactBondSourceIds.PactFormed }
            };
            var migration = new PactRosterSaveData();
            var progress = new List<PactProgressSaveData> { legendary };
            var seeded = PactRosterMigration.SeedCurrentStoryPacts(
                migration,
                progress,
                new[] { "Bramblet" },
                9876);
            var seededAgain = PactRosterMigration.SeedCurrentStoryPacts(
                migration,
                progress,
                new[] { "Bramblet" },
                9876);
            Check(
                seeded == 1 && seededAgain == 0 && migration.pacts.Count == 1,
                "Story Pact migration did not seed idempotently.",
                failures);
            Check(
                legendary.bondXp == 87 &&
                legendary.bondRank == 3 &&
                legendary.unfoldedFormId == TetherfallPactCatalog.GrovewardenFormId &&
                legendary.awardedSourceIds.Count == 1,
                "Story-link migration mutated legendary Pact progression.",
                failures);
            Check(
                migration.pacts[0].linkedLegendaryPactId ==
                TetherfallPactCatalog.BrambletId,
                "Story roster record did not retain its authoritative progression link.",
                failures);
            migration.activeFieldInstanceIds.Add(migration.pacts[0].instanceId);
            PactRosterNormalization.NormalizeInPlace(migration, 6);
            Check(
                migration.activeFieldInstanceIds.Count == 1 &&
                migration.pacts[0].fieldSlotState ==
                PactFieldSlotState.ActiveField &&
                PactRosterRules.IsFieldEligible(migration.pacts[0]),
                "Non-mount story-link record was not accepted by the unified field roster.",
                failures);

            var saveOwner = new TetherfallSaveData();
            PactRosterSaveBridge.Write(saveOwner, roster);
            Check(
                PactRosterSaveBridge.TryRead(saveOwner, out var roundTrip) &&
                roundTrip.pacts.Count == 1 &&
                roundTrip.pacts[0].instanceId == stableId,
                "Pact roster save bridge did not preserve a stable snapshot.",
                failures);
            if (roundTrip != null && roundTrip.pacts.Count > 0)
            {
                roundTrip.pacts[0].nickname = "Changed copy";
                PactRosterSaveBridge.TryRead(saveOwner, out var secondRead);
                Check(
                    secondRead != null &&
                    secondRead.pacts.Count == 1 &&
                    secondRead.pacts[0].nickname == "Rainstep",
                    "Pact roster save bridge leaked a mutable snapshot.",
                    failures);
            }
        }

        private static void VerifySerializedRuntimeIntegration(
            List<string> failures)
        {
            var saveField = typeof(TetherfallSaveData).GetField(
                "pactRoster",
                BindingFlags.Instance | BindingFlags.Public);
            var saveData = new TetherfallSaveData();
            Check(
                TetherfallSaveData.CurrentSchemaVersion == 24 &&
                saveField != null &&
                saveField.FieldType == typeof(PactRosterSaveData) &&
                saveField.GetValue(saveData) is PactRosterSaveData,
                "Schema 24 does not expose a real initialized PactRosterSaveData field.",
                failures);

            var saveSource = ReadProjectSource(
                "Scripts/TetherfallSaveSystem.cs",
                failures);
            Check(
                saveSource.Contains(
                    "public sealed partial class TetherfallSaveData") &&
                saveSource.Contains(
                    "data.pactRoster ??= new PactRosterSaveData();") &&
                saveSource.Contains(
                    "PactRosterNormalization.NormalizeInPlace(") &&
                saveSource.Contains(
                    "Mathf.Max(1, data.level)"),
                "Main save normalization does not initialize and normalize the serialized roster using saved level.",
                failures);

            var gameSource = ReadProjectSource(
                "Scripts/TetherfallGame.cs",
                failures);
            Check(
                CountOccurrences(
                    gameSource,
                    "InitializePactRosterSystem();") == 1 &&
                CountOccurrences(
                    gameSource,
                    "UpdatePactRosterSystem();") == 1 &&
                CountOccurrences(
                    gameSource,
                    "WritePactRosterSave(data);") == 1 &&
                CountOccurrences(
                    gameSource,
                    "RestorePactRosterState(data);") == 1,
                "Main game lifecycle does not call each Pact roster hook exactly once.",
                failures);
            Check(
                AppearsInOrder(
                    gameSource,
                    "InitializeCombatSystems();",
                    "InitializePactRosterSystem();",
                    "TetherfallSaveSystem.TryLoad") &&
                AppearsInOrder(
                    gameSource,
                    "WritePactUnfoldingSave(data);",
                    "WritePactRosterSave(data);",
                    "WritePlayerSettlementSave(data);",
                    "TetherfallSaveSystem.Save") &&
                AppearsInOrder(
                    gameSource,
                    "RestorePactUnfoldingState(data);",
                    "RestorePactRosterState(data);",
                    "RestorePlayerSettlementState(data);",
                    "RestoreTutorialState(data);"),
                "Pact roster lifecycle hooks are not ordered around load/save dependencies.",
                failures);
        }

        private static void VerifyStoryFieldAdapter(List<string> failures)
        {
            var roster = new PactRosterSaveData();
            var storyRecord = PactRosterMigration.EnsureStoryPactRecord(
                roster,
                TetherfallPactCatalog.BrambletId,
                1234,
                out var storyAdded);
            Check(
                storyAdded &&
                storyRecord != null &&
                PactRosterRules.IsLinkedStoryRecord(storyRecord) &&
                PactRosterRules.IsFieldEligible(storyRecord),
                "Bramblet did not seed as a field-eligible linked story record.",
                failures);
            var mountRecord = PactRosterMigration.EnsureStoryPactRecord(
                roster,
                TetherfallPactCatalog.SlatebackId,
                1234,
                out var mountAdded);
            Check(
                mountRecord == null && !mountAdded,
                "Slateback mount incorrectly entered the combat Pact roster.",
                failures);

            if (storyRecord != null)
            {
                roster.activeFieldInstanceIds.Add(storyRecord.instanceId);
                PactRosterNormalization.NormalizeInPlace(roster, 6);
                Check(
                    roster.activeFieldInstanceIds.Count == 1 &&
                    roster.activeFieldInstanceIds[0] ==
                    storyRecord.instanceId,
                    "Story adapter could not occupy one of the shared five field slots.",
                    failures);
            }

            var pactType = typeof(PactCreature);
            Check(
                pactType.GetProperty("IsFieldActive") != null &&
                pactType.GetProperty("FieldSlot") != null &&
                pactType.GetMethod(
                    "ConfigureFieldRosterState",
                    BindingFlags.Instance | BindingFlags.Public) != null &&
                pactType.GetMethod(
                    "SetFieldSimulationActive",
                    BindingFlags.Instance | BindingFlags.Public) != null &&
                pactType.GetMethod(
                    "TryRecallToFormation",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(Vector3), typeof(bool) },
                    null) != null,
                "PactCreature is missing its field-active, slot, simulation, or recall adapter API.",
                failures);

            var rosterSource = ReadProjectSource(
                "Scripts/TetherfallPactRoster.cs",
                failures);
            var actorSource = ReadProjectSource(
                "Scripts/TetherfallActors.cs",
                failures);
            Check(
                rosterSource.Contains(
                    "PactRosterRules.IsLinkedStoryRecord(record)") &&
                rosterSource.Contains(
                    "PactRosterRules.IsLinkedStoryRecord(actorRecord)") &&
                rosterSource.Contains(
                    "SynchronizePactCommandFocusAlias();") &&
                actorSource.Contains(
                    "game.GetPactFormationPosition(fieldSlot)") &&
                actorSource.Contains(
                    "game.RegisterPactAllyOvertone("),
                "Runtime source does not adapt story Pacts into shared formation, focus, and combat paths.",
                failures);
        }

        private static void VerifyWildEncounterIntegration(
            List<string> failures)
        {
            var expectedEnemyIds = new[]
            {
                "enemy.wild.alderwake.mossbell_strider",
                "enemy.wild.alderwake.copperquill_nester",
                "enemy.wild.morrowfen.tideveil_skimmer",
                "enemy.wild.morrowfen.gloamreed_lanternback",
                "enemy.wild.cinderwold.emberhorn_burrower",
                "enemy.wild.cinderwold.prismtail_kitelizard"
            };
            var expected = new HashSet<string>(
                expectedEnemyIds,
                StringComparer.Ordinal);
            var placedSpecies = new Dictionary<string, int>(
                StringComparer.Ordinal);
            var wildPositions = new List<Vector3>(6);
            for (var index = 0; index < expectedEnemyIds.Length; index++)
            {
                placedSpecies[expectedEnemyIds[index]] = 0;
                var enemy = EnemyCatalog.Get(expectedEnemyIds[index]);
                Check(
                    enemy != null &&
                    enemy.id == expectedEnemyIds[index] &&
                    enemy.maxHealth >= 40 &&
                    enemy.maxHealth <= 160 &&
                    enemy.attackDamage >= 5 &&
                    enemy.attackDamage <= 16 &&
                    enemy.experienceReward >= 15 &&
                    enemy.experienceReward <= 45,
                    $"Wild enemy definition '{expectedEnemyIds[index]}' is missing or outside low/midgame bounds.",
                    failures);
            }

            Check(
                expected.Count == TetherfallAccordSpeciesCatalog.Wild.Count,
                "Wild accord catalog no longer matches the six exact gameplay enemy IDs.",
                failures);
            for (var index = 0;
                 index < TetherfallAccordSpeciesCatalog.Wild.Count;
                 index++)
            {
                Check(
                    expected.Contains(
                        TetherfallAccordSpeciesCatalog.Wild[index]
                            .eligibleEnemyId),
                    $"Unexpected wild gameplay enemy ID '{TetherfallAccordSpeciesCatalog.Wild[index].eligibleEnemyId}'.",
                    failures);
            }

            var wildPlacementCount = 0;
            for (var placementIndex = 0;
                 placementIndex < EncounterCatalog.ExtendedPlacements.Count;
                 placementIndex++)
            {
                var placement =
                    EncounterCatalog.ExtendedPlacements[placementIndex];
                if (placement == null ||
                    !placement.encounterId.StartsWith(
                        "encounter.wild.",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                wildPlacementCount++;
                var separatedFromWildPlacements = true;
                for (var priorIndex = 0;
                     priorIndex < wildPositions.Count;
                     priorIndex++)
                {
                    if (Vector3.Distance(
                            placement.position,
                            wildPositions[priorIndex]) < 30f)
                    {
                        separatedFromWildPlacements = false;
                        break;
                    }
                }

                wildPositions.Add(placement.position);
                var clearOfThreadways = true;
                for (var threadwayIndex = 0;
                     threadwayIndex < ThreadwayCatalog.All.Count;
                     threadwayIndex++)
                {
                    if (Vector3.Distance(
                            placement.position,
                            ThreadwayCatalog.All[threadwayIndex].position) < 48f)
                    {
                        clearOfThreadways = false;
                        break;
                    }
                }

                Check(
                    IsFinite(placement.position) &&
                    IsWildPlacementInsideRegion(
                        placement.encounterId,
                        placement.position) &&
                    separatedFromWildPlacements &&
                    clearOfThreadways &&
                    Vector3.Distance(
                        placement.position,
                        CityCatalog.AlderwakeHearthmarket.center) >= 48f &&
                    Vector3.Distance(
                        placement.position,
                        CityCatalog.VeyrwaterMooring.center) >= 48f &&
                    !string.IsNullOrWhiteSpace(placement.persistenceId) &&
                    placement.persistenceId.StartsWith(
                        placement.encounterId + "@",
                        StringComparison.Ordinal),
                    $"Wild placement '{placement.encounterId}' is not stable or clear of cities, Threadways, and other habitats.",
                    failures);
                var encounter = EncounterCatalog.Get(
                    placement.encounterId);
                Check(
                    encounter != null &&
                    encounter.id == placement.encounterId &&
                    encounter.respawnSeconds > 24f &&
                    encounter.respawnSeconds <= 120f &&
                    encounter.WaveCount == 1,
                    $"Wild placement '{placement.encounterId}' lacks a small respawning encounter.",
                    failures);
                var wave = encounter.GetWave(0);
                Check(
                    wave.Length >= 1 && wave.Length <= 2,
                    $"Wild encounter '{encounter.id}' is not a small one-or-two creature group.",
                    failures);
                for (var enemyIndex = 0;
                     enemyIndex < wave.Length;
                     enemyIndex++)
                {
                    Check(
                        expected.Contains(wave[enemyIndex]),
                        $"Wild encounter '{encounter.id}' contains ineligible enemy '{wave[enemyIndex]}'.",
                        failures);
                    if (placedSpecies.ContainsKey(wave[enemyIndex]))
                    {
                        placedSpecies[wave[enemyIndex]]++;
                    }
                }
            }

            Check(
                wildPlacementCount == 6,
                $"Found {wildPlacementCount} wild Accord placements; expected exactly six.",
                failures);
            foreach (var pair in placedSpecies)
            {
                Check(
                    pair.Value >= 1,
                    $"Wild enemy '{pair.Key}' has no dedicated world placement.",
                    failures);
            }

            var actorSource = ReadProjectSource(
                "Scripts/TetherfallActors.cs",
                failures);
            var accordSource = ReadProjectSource(
                "Scripts/TetherfallThreadbindAccords.cs",
                failures);
            var threadwaySource = ReadProjectSource(
                "Scripts/TetherfallThreadways.cs",
                failures);
            Check(
                AppearsInOrder(
                    actorSource,
                    "defeated = true;",
                    "game.OnEligibleWildEchoswornDefeated(",
                    "Destroy(gameObject, 0.32f);") &&
                actorSource.Contains(
                    "camp.IsWildAccordEncounter") &&
                actorSource.Contains(
                    "if (threadbindEligibleSpawn)") &&
                actorSource.Contains(
                    "BuildThreadbindOriginId()") &&
                actorSource.Contains(
                    "Mathf.RoundToInt(spawnPosition.x)") &&
                actorSource.Contains(
                    "TetherfallCapturedPactArt.Build(") &&
                actorSource.Contains(
                    "enemy.bodyRenderer.enabled = false;") &&
                actorSource.Contains(
                    "wildPactArtRig?.Tick(") &&
                actorSource.Contains(
                    "wildPactArtRig?.Dispose();") &&
                actorSource.Contains(
                    "Destroy(bodyMaterial);"),
                "Eligible wild defeat does not create Threadbind before destroy with owned art cleanup.",
                failures);
            Check(
                ThreadbindOpportunity.LifetimeSeconds == 24f &&
                accordSource.Contains(
                    "return $\"{enemyId}@{Mathf.RoundToInt(position.x)}:\"") &&
                accordSource.Contains(
                    "TetherfallAccordSpeciesCatalog.IsEligibleWildEnemyId"),
                "Threadbind opportunity is not exact-ID gated for 24 seconds with rounded-position origin.",
                failures);
            Check(
                accordSource.Contains("Physics.OverlapSphereNonAlloc(") &&
                !accordSource.Contains("Physics.OverlapSphere(") &&
                actorSource.Contains("Physics.RaycastNonAlloc(") &&
                actorSource.Contains(
                    "game?.RecallPactRosterToFormation();") &&
                accordSource.Contains(
                    "(!immediate && game.GameplayPaused)") &&
                threadwaySource.Contains(
                    "RecallPactRosterToFormation();"),
                "Pact target/recall integration regressed to allocating physics or omitted pause-safe player/Threadway recall.",
                failures);
            Check(
                actorSource.Contains("IsWildAccordEncounter") &&
                actorSource.Contains("camp.ReportDeath(this);") &&
                actorSource.Contains("FinishCurrentWave();"),
                "Wild encounter defeat no longer preserves camp/wave clear bookkeeping.",
                failures);
        }

        private static void VerifyActorBudgets(List<string> failures)
        {
            var expeditionSource = ReadProjectSource(
                "Scripts/TetherfallExpeditionCombat.cs",
                failures);
            Check(
                TetherfallGame.PactActorBudget == 5 &&
                PactRosterRules.AbsoluteMaximumFieldUnits == 5,
                "Runtime actor budget is not fixed at five.",
                failures);
            Check(
                CapturedPactActor.TargetQueryCapacity >= 16 &&
                CapturedPactActor.TargetQueryCapacity <= 32 &&
                CapturedPactActor.MaximumAssistQueriesPerTick == 1,
                "CapturedPactActor target lookup is not bounded.",
                failures);
            Check(
                expeditionSource.Contains(
                    "private readonly struct ThreadlinkRallyResponder") &&
                expeditionSource.Contains(
                    "threadlinkAvailableResponderBuffer.Clear();") &&
                expeditionSource.Contains(
                    "new List<ThreadlinkRallyResponder>(PactActorBudget + 2)") &&
                !expeditionSource.Contains(
                    "private sealed class ThreadlinkRallyResponder"),
                "Threadlink responder selection no longer uses reusable bounded struct buffers.",
                failures);
            Check(
                IsFinite(TetherfallGame.PactActorActivationDistance) &&
                TetherfallGame.PactActorActivationDistance > 20f &&
                TetherfallGame.PactActorActivationDistance <= 60f,
                "Pact actor activation distance is invalid.",
                failures);
            Check(
                ThreadbindOpportunity.LifetimeSeconds > 0f &&
                ThreadbindOpportunity.LifetimeSeconds <= 30f,
                "Threadbind opportunity lifetime is not tightly bounded.",
                failures);

            var actorObject = new GameObject("Pact roster actor verification");
            try
            {
                var actor = actorObject.AddComponent<CapturedPactActor>();
                var targetBuffer = ReadField<Collider[]>(actor, "assistTargetBuffer");
                var motor = ReadField<ExpeditionFollowerMotor>(actor, "followerMotor");
                Check(
                    targetBuffer != null &&
                    targetBuffer.Length == CapturedPactActor.TargetQueryCapacity,
                    "CapturedPactActor is missing its reusable NonAlloc target buffer.",
                    failures);
                Check(
                    motor != null,
                    "CapturedPactActor is not backed by ExpeditionFollowerMotor.",
                    failures);
                Check(
                    IsReadonlyField(typeof(CapturedPactActor), "assistTargetBuffer") &&
                    IsReadonlyField(typeof(CapturedPactActor), "followerMotor"),
                    "Actor query/motor storage can be replaced during simulation.",
                    failures);
                Check(
                    actorObject.GetComponentsInChildren<Collider>(true).Length == 0 &&
                    actorObject.GetComponentsInChildren<Rigidbody>(true).Length == 0 &&
                    actorObject.GetComponentsInChildren<Light>(true).Length == 0,
                    "Bare roster actor added physics or lights.",
                    failures);

                var formationField = typeof(TetherfallGame).GetField(
                    "PactFormationOffsets",
                    BindingFlags.Static | BindingFlags.NonPublic);
                var formation = formationField?.GetValue(null) as Vector3[];
                Check(
                    formation != null &&
                    formation.Length == PactRosterRules.AbsoluteMaximumFieldUnits,
                    "Field formation does not define exactly five slots.",
                    failures);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actorObject);
            }

            for (var index = 0; index < TetherfallAccordSpeciesCatalog.Wild.Count; index++)
            {
                var artObject = new GameObject($"Pact art verification {index}");
                CapturedPactArtRig rig = null;
                try
                {
                    rig = TetherfallCapturedPactArt.Build(
                        null,
                        artObject.transform,
                        TetherfallAccordSpeciesCatalog.Wild[index]);
                    var renderers = artObject.GetComponentsInChildren<Renderer>(true);
                    Check(
                        rig != null &&
                        rig.RendererCount == renderers.Length &&
                        renderers.Length <=
                        TetherfallCapturedPactArt.MaximumRenderersPerActor,
                        $"Fallback art '{TetherfallAccordSpeciesCatalog.Wild[index].speciesId}' " +
                        "exceeded its renderer budget.",
                        failures);
                    Check(
                        rig != null &&
                        rig.MaterialCount <=
                        TetherfallCapturedPactArt.MaximumMaterialsPerActor,
                        $"Fallback art '{TetherfallAccordSpeciesCatalog.Wild[index].speciesId}' " +
                        "exceeded its material budget.",
                        failures);
                    Check(
                        artObject.GetComponentsInChildren<Collider>(true).Length ==
                        TetherfallCapturedPactArt.MaximumCollidersPerActor &&
                        artObject.GetComponentsInChildren<Rigidbody>(true).Length == 0 &&
                        artObject.GetComponentsInChildren<Light>(true).Length ==
                        TetherfallCapturedPactArt.MaximumLightsPerActor,
                        $"Fallback art '{TetherfallAccordSpeciesCatalog.Wild[index].speciesId}' " +
                        "added forbidden components.",
                        failures);
                }
                finally
                {
                    rig?.Dispose();
                    UnityEngine.Object.DestroyImmediate(artObject);
                }
            }
        }

        private static void VerifyCopy(List<string> failures)
        {
            var banned = new[]
            {
                "capture",
                "tame",
                "enslave",
                "forced",
                "ownership",
                "labor",
                "human",
                "generic hollow"
            };
            var allCopy = new List<string>();
            for (var index = 0; index < TetherfallAccordSpeciesCatalog.All.Count; index++)
            {
                var species = TetherfallAccordSpeciesCatalog.All[index];
                allCopy.Add(species.displayName);
                allCopy.Add(species.defaultEpithet);
                allCopy.Add(species.fieldTemperament);
                allCopy.Add(species.movement?.traversalCharacter);
                allCopy.Add(species.art?.silhouetteDirection);
                allCopy.Add(species.art?.materialDirection);
                allCopy.Add(species.art?.articulationDirection);
                if (species.workAffinities == null)
                {
                    continue;
                }

                for (var workIndex = 0;
                     workIndex < species.workAffinities.Length;
                     workIndex++)
                {
                    allCopy.Add(species.workAffinities[workIndex]?.voluntaryPreference);
                }
            }

            for (var index = 0; index < ThreadbindOpportunity.PlayerFacingCopySamples.Count; index++)
            {
                allCopy.Add(ThreadbindOpportunity.PlayerFacingCopySamples[index]);
            }

            var combined = string.Join("\n", allCopy).ToLowerInvariant();
            for (var index = 0; index < banned.Length; index++)
            {
                Check(
                    !combined.Contains(banned[index]),
                    $"Player-facing Pact copy contains forbidden term '{banned[index]}'.",
                    failures);
            }

            Check(
                combined.Contains("threadbind accord") &&
                combined.Contains("rescue") &&
                combined.Contains("voluntary"),
                "Player-facing copy does not establish rescue and voluntary Threadbind Accord language.",
                failures);
        }

        private static CapturedPactSaveData CreateRecord(
            PactSpeciesDefinition definition,
            string instanceId)
        {
            return new CapturedPactSaveData
            {
                instanceId = instanceId,
                speciesId = definition.speciesId,
                nickname = string.Empty,
                epithet = definition.defaultEpithet,
                level = 1,
                experience = 0,
                bond = 10,
                stamina = definition.baseStamina,
                morale = definition.baseMorale,
                fieldSlotState = PactFieldSlotState.Reserve,
                fieldSlot = -1,
                settlementId = string.Empty,
                stationId = string.Empty,
                bedId = string.Empty,
                originId = "origin.verify",
                accordFormedUtcTicks = 1000,
                linkedLegendaryPactId = definition.linkedLegendaryPactId
            };
        }

        private static string ReadProjectSource(
            string assetRelativePath,
            List<string> failures)
        {
            var path = Path.Combine(
                Application.dataPath,
                assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                failures.Add(
                    $"Verification source '{assetRelativePath}' was not found.");
                return string.Empty;
            }

            return File.ReadAllText(path);
        }

        private static int CountOccurrences(string source, string value)
        {
            if (string.IsNullOrEmpty(source) ||
                string.IsNullOrEmpty(value))
            {
                return 0;
            }

            var count = 0;
            var offset = 0;
            while ((offset = source.IndexOf(
                       value,
                       offset,
                       StringComparison.Ordinal)) >= 0)
            {
                count++;
                offset += value.Length;
            }

            return count;
        }

        private static bool AppearsInOrder(
            string source,
            params string[] values)
        {
            if (string.IsNullOrEmpty(source) ||
                values == null ||
                values.Length == 0)
            {
                return false;
            }

            var searchOffset = 0;
            foreach (var value in values)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return false;
                }

                var index = source.IndexOf(
                    value,
                    searchOffset,
                    StringComparison.Ordinal);
                if (index < 0)
                {
                    return false;
                }

                searchOffset = index + value.Length;
            }

            return true;
        }

        private static T ReadField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? (T)field.GetValue(instance) : default;
        }

        private static bool IsReadonlyField(Type type, string fieldName)
        {
            var field = type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null && field.IsInitOnly;
        }

        private static bool IsWildPlacementInsideRegion(
            string encounterId,
            Vector3 position)
        {
            if (string.IsNullOrEmpty(encounterId) ||
                position.z < -124f ||
                position.z > 124f)
            {
                return false;
            }

            if (encounterId.StartsWith(
                    "encounter.wild.alderwake.",
                    StringComparison.Ordinal))
            {
                return position.x >= -78f && position.x <= 78f;
            }

            if (encounterId.StartsWith(
                    "encounter.wild.morrowfen.",
                    StringComparison.Ordinal))
            {
                return position.x >= 82f && position.x <= 238f;
            }

            if (encounterId.StartsWith(
                    "encounter.wild.cinderwold.",
                    StringComparison.Ordinal))
            {
                return position.x >= -238f && position.x <= -82f;
            }

            return false;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Color value)
        {
            return IsFinite(value.r) &&
                   IsFinite(value.g) &&
                   IsFinite(value.b) &&
                   IsFinite(value.a);
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
            if (!condition)
            {
                failures.Add(message);
            }
        }
    }
}
