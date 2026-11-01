using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallFullGameVerification
    {
        private const string MenuPath = "Tetherfall/Verify Full Game";
        private const float MaximumWorldCoordinate = 10000f;
        private const float MaximumEnemyScale = 10f;

        [MenuItem(MenuPath, false, 2200)]
        public static void VerifyFullGame()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Tetherfall full-game verification is edit-mode only. Stop Play Mode and run it again.");
            }

            var report = new VerificationReport();
            CatalogSnapshot catalogs = null;

            report.Run("catalog loading", () => catalogs = CatalogSnapshot.Load());
            if (catalogs != null)
            {
                report.Run("catalog IDs", () => VerifyCatalogIds(report, catalogs));
                report.Run("quest references", () => VerifyQuestReferences(report, catalogs));
                report.Run("equipment references", () => VerifyEquipmentReferences(report, catalogs));
                report.Run(
                    "enemy and encounter references",
                    () => VerifyEnemyAndEncounterReferences(report, catalogs));
                report.Run(
                    "city, trial, bounty, and Threadway data",
                    () => VerifyWorldData(report, catalogs));
            }

            report.Run("save normalization", () => VerifySaveNormalization(report));
            report.Run(
                "Pact Unfolding",
                TetherfallPactUnfoldingVerification.VerifyForFullGame);
            report.Run(
                "Wayfinder progression",
                TetherfallProgressionVerification.VerifyForFullGame);
            report.Run(
                "Pact roster",
                TetherfallPactRosterVerification.VerifyForFullGame);
            report.Run(
                "settlement art",
                TetherfallSettlementArtVerification.VerifyForFullGame);
            report.Run(
                "player settlement",
                TetherfallPlayerSettlementVerification.VerifyForFullGame);
            report.Run(
                "modular dungeons",
                TetherfallDungeonVerification.VerifyForFullGame);
            report.Run(
                "regional expansion packs",
                TetherfallRegionPacksVerification.Verify);
            report.Run(
                "regional frontier annexes",
                TetherfallRegionalAnnexesVerification.Verify);
            report.Run(
                "Quaternius runtime dressing",
                TetherfallQuaterniusVerification.VerifyForFullGame);
            report.Run(
                "expedition follower motor",
                TetherfallFollowerMotorVerification.Verify);
            report.Run(
                "Commit & Counter combat",
                TetherfallCommitCounterVerification.VerifyForFullGame);
            report.Run(
                "Field Instrument threat presentation",
                TetherfallThreatPresentationVerification.VerifyForFullGame);
            report.Run("control documentation", () => VerifyControlDocumentation(report));
            report.Run("runtime audit regressions", TetherfallAuditRegressionVerification.Verify);
            report.Complete();
        }

        [MenuItem(MenuPath, true, 2200)]
        private static bool CanVerifyFullGame()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void VerifyCatalogIds(VerificationReport report, CatalogSnapshot catalogs)
        {
            var globalIds = new Dictionary<string, string>(StringComparer.Ordinal);

            ValidateDictionaryIds(
                report,
                "EnemyCatalog.Definitions",
                catalogs.Enemies,
                definition => definition.id,
                globalIds);
            ValidateDictionaryIds(
                report,
                "EncounterCatalog.Definitions",
                catalogs.Encounters,
                definition => definition.id,
                globalIds);
            ValidateIds(
                report,
                "RouteEventCatalog.All",
                catalogs.RouteEvents,
                definition => definition.id,
                globalIds);
            ValidateIds(
                report,
                "FieldCacheCatalog.All",
                catalogs.FieldCaches,
                definition => definition.id,
                globalIds);
            ValidateIds(
                report,
                "RestorationCatalog.All",
                catalogs.RestorationSites,
                definition => definition.id,
                globalIds);
            ValidateIds(
                report,
                "ProvisioningCatalog.Meals",
                catalogs.Meals,
                definition => definition.id,
                globalIds);
            ValidateIds(
                report,
                "ProvisioningCatalog.Vendors",
                catalogs.Vendors,
                definition => definition.id,
                globalIds);
            ValidateIds(
                report,
                "ProvisioningCatalog.CampContracts",
                catalogs.CampContracts,
                definition => definition.id,
                globalIds);
            ValidateIds(
                report,
                "EquipmentCatalog.CreateAll()",
                catalogs.Equipment,
                definition => definition.id,
                globalIds);
            ValidateIds(
                report,
                "BossCatalog.Definitions",
                catalogs.Bosses,
                definition => definition.id,
                globalIds);
            ValidateIds(
                report,
                "TrialCatalog",
                catalogs.Trials,
                definition => definition.id,
                globalIds);
            ValidateIds(
                report,
                "CityCatalog",
                catalogs.Cities,
                definition => definition.id,
                globalIds);
            ValidateIds(
                report,
                "BountyCatalog.All",
                catalogs.Bounties,
                definition => definition.id,
                globalIds);
            ValidateIds(
                report,
                "ThreadwayCatalog.All",
                catalogs.Threadways,
                definition => definition.id,
                globalIds);
            ValidateIds(
                report,
                "QuestCatalog.CreateAll()",
                catalogs.Quests,
                definition => definition.id,
                globalIds);

            foreach (var city in catalogs.Cities)
            {
                if (city == null)
                {
                    continue;
                }

                ValidateIds(
                    report,
                    $"CityCatalog city '{city.id}' residents",
                    city.residents,
                    definition => definition.id,
                    globalIds);
                ValidateIds(
                    report,
                    $"CityCatalog city '{city.id}' activities",
                    city.activities,
                    definition => definition.id,
                    globalIds);
            }

            var crossweaveWeapons = new HashSet<WeaponDiscipline>();
            report.Check(
                catalogs.Crossweaves != null,
                "CrossweaveCatalog.Definitions is null. Initialize the catalog before verification.");
            if (catalogs.Crossweaves != null)
            {
                for (var index = 0; index < catalogs.Crossweaves.Count; index++)
                {
                    var definition = catalogs.Crossweaves[index];
                    report.Check(
                        definition != null,
                        $"CrossweaveCatalog.Definitions[{index}] is null. Remove it or provide a definition.");
                    if (definition == null)
                    {
                        continue;
                    }

                    report.Check(
                        crossweaveWeapons.Add(definition.weapon),
                        $"CrossweaveCatalog has more than one definition for weapon '{definition.weapon}'.");
                    RequireText(
                        report,
                        definition.displayName,
                        $"CrossweaveCatalog weapon '{definition.weapon}' displayName");
                    RequireText(
                        report,
                        definition.cue,
                        $"CrossweaveCatalog weapon '{definition.weapon}' cue");
                }
            }

            foreach (WeaponDiscipline weapon in Enum.GetValues(typeof(WeaponDiscipline)))
            {
                report.Check(
                    crossweaveWeapons.Contains(weapon),
                    $"CrossweaveCatalog is missing a definition for weapon '{weapon}'.");
            }
        }

        private static void VerifyQuestReferences(VerificationReport report, CatalogSnapshot catalogs)
        {
            var questIds = GetIds(catalogs.Quests, definition => definition.id);
            var referencedIds = new HashSet<string>(StringComparer.Ordinal);

            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "alderwake",
                "TetherfallGame.OnSettlementRestored");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "skyvault",
                "TetherfallGame dungeon progression");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "fieldcraft",
                "TetherfallGame equipment progression");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "campward",
                "TetherfallGame.OnHostileCampCleared");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "hearthgarden",
                "TetherfallGame.OnFarmHarvested");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "accords",
                "TetherfallGame.OnPactFormed");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "signalbreak",
                "Tetherfall route-event progression");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "fieldarchives",
                "Tetherfall field-cache progression");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "fenlight",
                "Tetherfall restoration progression");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                DungeonCatalog.GlasswaterQuestId,
                "Glasswater Sluice dungeon progression");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                CinderwoldArcCatalog.QuestId,
                "CinderwoldArcCatalog.QuestId");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "hearthmarket",
                "Alderwake city activity progression");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "veyrwater",
                "Veyrwater city activity progression");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "bounties",
                "Bounty progression");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "threadways",
                "Threadway progression");
            RequireQuestReference(
                report,
                questIds,
                referencedIds,
                "routevigil",
                "repeat route-patrol progression");

            foreach (var quest in catalogs.Quests)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.id))
                {
                    continue;
                }

                RequireText(report, quest.title, $"Quest '{quest.id}' title");
                RequireText(report, quest.objective, $"Quest '{quest.id}' objective");
                report.Check(
                    quest.required > 0,
                    $"Quest '{quest.id}' has required={quest.required}. Set required to a positive objective count.");
                report.Check(
                    quest.favorReward >= 0,
                    $"Quest '{quest.id}' has a negative Favor reward ({quest.favorReward}).");
                report.Check(
                    referencedIds.Contains(quest.id),
                    $"Quest '{quest.id}' has no verified gameplay progression reference. " +
                    "Add its AdvanceQuest/restore integration to the verification suite.");
            }

            CheckQuestRequirement(
                report,
                catalogs.Quests,
                "fieldarchives",
                catalogs.FieldCaches.Count,
                "FieldCacheCatalog.All.Count");
            CheckQuestRequirement(
                report,
                catalogs.Quests,
                "bounties",
                catalogs.Bounties.Count,
                "BountyCatalog.All.Count");
            CheckQuestRequirement(
                report,
                catalogs.Quests,
                "threadways",
                catalogs.Threadways.Count,
                "ThreadwayCatalog.All.Count");

            var storySignalCount = 0;
            foreach (var routeEvent in catalogs.RouteEvents)
            {
                if (routeEvent != null && routeEvent.contributesToSignalbreak)
                {
                    storySignalCount++;
                }
            }

            CheckQuestRequirement(
                report,
                catalogs.Quests,
                "signalbreak",
                storySignalCount,
                "the number of route events contributing to Signalbreak");

            var alderwakeActivities = CityCatalog.AlderwakeHearthmarket != null &&
                                      CityCatalog.AlderwakeHearthmarket.activities != null
                ? CityCatalog.AlderwakeHearthmarket.activities.Count
                : 0;
            var veyrwaterActivities = CityCatalog.VeyrwaterMooring != null &&
                                     CityCatalog.VeyrwaterMooring.activities != null
                ? CityCatalog.VeyrwaterMooring.activities.Count
                : 0;
            CheckQuestRequirement(
                report,
                catalogs.Quests,
                "hearthmarket",
                alderwakeActivities,
                "Alderwake Hearthmarket activity count");
            CheckQuestRequirement(
                report,
                catalogs.Quests,
                "veyrwater",
                veyrwaterActivities,
                "Veyrwater Mooring activity count");

            var fenlight = FindById(
                catalogs.RestorationSites,
                RestorationCatalog.FenlightCausewayId,
                definition => definition.id);
            if (fenlight != null)
            {
                CheckQuestRequirement(
                    report,
                    catalogs.Quests,
                    "fenlight",
                    fenlight.maxTier + 1,
                    "Fenlight's route-opening beat plus restoration tiers");
            }
        }

        private static void VerifyEquipmentReferences(
            VerificationReport report,
            CatalogSnapshot catalogs)
        {
            var equipmentIds = GetIds(catalogs.Equipment, definition => definition.id);
            var mealIds = GetIds(catalogs.Meals, definition => definition.id);

            foreach (var definition in catalogs.Equipment)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                {
                    continue;
                }

                RequireText(report, definition.displayName, $"Equipment '{definition.id}' displayName");
                RequireText(report, definition.description, $"Equipment '{definition.id}' description");
                report.Check(
                    Enum.IsDefined(typeof(EquipmentSlot), definition.slot),
                    $"Equipment '{definition.id}' has invalid slot value '{definition.slot}'.");
                report.Check(
                    definition.favorPrice >= 0,
                    $"Equipment '{definition.id}' has a negative Favor price ({definition.favorPrice}).");
                CheckFinite(
                    report,
                    definition.moveSpeedBonus,
                    $"Equipment '{definition.id}' moveSpeedBonus");
                CheckFinite(
                    report,
                    definition.staminaBonus,
                    $"Equipment '{definition.id}' staminaBonus");
                CheckFinite(
                    report,
                    definition.staminaRegenerationBonus,
                    $"Equipment '{definition.id}' staminaRegenerationBonus");
                CheckFinite(
                    report,
                    definition.windLiftVelocityBonus,
                    $"Equipment '{definition.id}' windLiftVelocityBonus");
                ValidateCosts(
                    report,
                    definition.craftCost,
                    $"Equipment '{definition.id}' craftCost",
                    false);

                var hasAcquisitionPath = definition.restorationRewardOnly ||
                                         definition.favorPrice > 0 ||
                                         (definition.craftCost != null && definition.craftCost.Count > 0);
                report.Check(
                    hasAcquisitionPath,
                    $"Equipment '{definition.id}' has no craft cost, Favor price, or reward-only acquisition path.");
            }

            RequireEquipment(
                report,
                catalogs.Equipment,
                "weapon.riftbow",
                EquipmentSlot.Weapon,
                WeaponDiscipline.Riftbow,
                "TetherfallGame.IsWeaponUnlocked");
            RequireEquipment(
                report,
                catalogs.Equipment,
                "weapon.threadstaff",
                EquipmentSlot.Weapon,
                WeaponDiscipline.Threadstaff,
                "TetherfallGame.IsWeaponUnlocked");

            RequireReference(
                report,
                equipmentIds,
                "coat.mosscoat",
                "Wayfinder authored coat visual",
                "equipment");
            RequireReference(
                report,
                equipmentIds,
                "charm.anchor",
                "Wayfinder authored charm visual",
                "equipment");
            RequireReference(
                report,
                equipmentIds,
                "charm.galespring",
                "Wayfinder authored charm visual",
                "equipment");
            RequireReference(
                report,
                equipmentIds,
                "charm.mirelight",
                "Wayfinder authored charm visual",
                "equipment");
            RequireReference(
                report,
                equipmentIds,
                "charm.emberwatch",
                "Cinderwold routebound reward",
                "equipment");
            RequireReference(
                report,
                equipmentIds,
                "tool.ashkite",
                "Cinderwold ashbound reward",
                "equipment");
            RequireReference(
                report,
                equipmentIds,
                "tool.hookline",
                "Wayfinder authored tool visual",
                "equipment");
            RequireReference(
                report,
                equipmentIds,
                "tool.ridgepack",
                "Wayfinder authored tool visual",
                "equipment");
            RequireReference(
                report,
                equipmentIds,
                "tool.thermal_reel",
                "Ashwake Crucible reward",
                "equipment");

            foreach (var vendor in catalogs.Vendors)
            {
                if (vendor == null)
                {
                    continue;
                }

                report.Check(
                    vendor.equipmentIds != null,
                    $"Vendor '{vendor.id}' equipmentIds is null.");
                if (vendor.equipmentIds != null)
                {
                    foreach (var equipmentId in vendor.equipmentIds)
                    {
                        RequireReference(
                            report,
                            equipmentIds,
                            equipmentId,
                            $"Vendor '{vendor.id}'",
                            "equipment");
                    }
                }

                report.Check(
                    vendor.recipeIds != null,
                    $"Vendor '{vendor.id}' recipeIds is null.");
                if (vendor.recipeIds != null)
                {
                    foreach (var recipeId in vendor.recipeIds)
                    {
                        RequireReference(
                            report,
                            mealIds,
                            recipeId,
                            $"Vendor '{vendor.id}'",
                            "meal recipe");
                    }
                }
            }

            foreach (var site in catalogs.RestorationSites)
            {
                if (site == null || site.tiers == null)
                {
                    continue;
                }

                foreach (var tier in site.tiers)
                {
                    if (tier != null && !string.IsNullOrWhiteSpace(tier.rewardEquipmentId))
                    {
                        RequireReference(
                            report,
                            equipmentIds,
                            tier.rewardEquipmentId,
                            $"Restoration '{site.id}' tier {tier.tier}",
                            "equipment reward");
                    }
                }
            }

            foreach (var trial in catalogs.Trials)
            {
                if (trial != null)
                {
                    RequireReference(
                        report,
                        equipmentIds,
                        trial.rewardEquipmentId,
                        $"Trial '{trial.id}'",
                        "equipment reward");
                }
            }
        }

        private static void VerifyEnemyAndEncounterReferences(
            VerificationReport report,
            CatalogSnapshot catalogs)
        {
            var enemyIds = GetIds(catalogs.Enemies.Values, definition => definition.id);
            var encounterIds = GetIds(catalogs.Encounters.Values, definition => definition.id);

            foreach (var definition in catalogs.Enemies.Values)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                {
                    continue;
                }

                RequireText(report, definition.displayName, $"Enemy '{definition.id}' displayName");
                report.Check(
                    definition.maxHealth > 0,
                    $"Enemy '{definition.id}' has maxHealth={definition.maxHealth}. Use a positive value.");
                report.Check(
                    definition.attackDamage > 0,
                    $"Enemy '{definition.id}' has attackDamage={definition.attackDamage}. Use a positive value.");
                report.Check(
                    definition.experienceReward >= 0,
                    $"Enemy '{definition.id}' has a negative XP reward ({definition.experienceReward}).");
                CheckPositiveFinite(report, definition.moveSpeed, $"Enemy '{definition.id}' moveSpeed");
                CheckPositiveFinite(report, definition.attackRange, $"Enemy '{definition.id}' attackRange");
                CheckPositiveFinite(report, definition.detectionRange, $"Enemy '{definition.id}' detectionRange");
                CheckPositiveFinite(report, definition.attackCooldown, $"Enemy '{definition.id}' attackCooldown");
                CheckPositiveFinite(report, definition.telegraphSeconds, $"Enemy '{definition.id}' telegraphSeconds");
                CheckPositiveFinite(report, definition.scale, $"Enemy '{definition.id}' scale");
                report.Check(
                    IsFinite(definition.scale) && definition.scale <= MaximumEnemyScale,
                    $"Enemy '{definition.id}' scale={definition.scale} is implausibly large. " +
                    $"Keep authored enemy scale at or below {MaximumEnemyScale}.");
                report.Check(
                    definition.detectionRange >= definition.attackRange,
                    $"Enemy '{definition.id}' detectionRange ({definition.detectionRange}) is smaller than " +
                    $"attackRange ({definition.attackRange}).");

                if (definition.attackStyle == EnemyAttackStyle.Ranged &&
                    definition.specialMove != EnemySpecialMove.GroundBurst)
                {
                    CheckPositiveFinite(
                        report,
                        definition.projectileSpeed,
                        $"Ranged enemy '{definition.id}' projectileSpeed");
                }

                if (definition.specialMove == EnemySpecialMove.GroundBurst)
                {
                    CheckPositiveFinite(
                        report,
                        definition.groundBurstRadius,
                        $"Ground-burst enemy '{definition.id}' groundBurstRadius");
                }

                if (definition.ashguardDuringShockwave)
                {
                    report.Check(
                        definition.specialMove == EnemySpecialMove.Shockwave,
                        $"Enemy '{definition.id}' enables Ashguard but does not use the Shockwave special move.");
                    report.Check(
                        IsFinite(definition.ashguardDamageMultiplier) &&
                        definition.ashguardDamageMultiplier > 0f &&
                        definition.ashguardDamageMultiplier <= 1f,
                        $"Enemy '{definition.id}' has invalid ashguardDamageMultiplier=" +
                        $"{definition.ashguardDamageMultiplier}. Use a value in (0, 1].");
                    CheckPositiveFinite(
                        report,
                        definition.ashguardBreakExposeSeconds,
                        $"Enemy '{definition.id}' ashguardBreakExposeSeconds");
                    CheckPositiveFinite(
                        report,
                        definition.ashguardBreakStaggerSeconds,
                        $"Enemy '{definition.id}' ashguardBreakStaggerSeconds");
                }
            }

            foreach (var definition in catalogs.Encounters.Values)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                {
                    continue;
                }

                RequireText(report, definition.displayName, $"Encounter '{definition.id}' displayName");
                CheckPositiveFinite(
                    report,
                    definition.respawnSeconds,
                    $"Encounter '{definition.id}' respawnSeconds");
                ValidateEnemyRoster(
                    report,
                    enemyIds,
                    definition.enemyIds,
                    $"Encounter '{definition.id}' enemyIds",
                    true);

                var largestWave = definition.enemyIds != null ? definition.enemyIds.Length : 0;
                report.Check(
                    definition.waves != null && definition.waves.Length > 0,
                    $"Encounter '{definition.id}' has no waves. Add at least one non-empty enemy wave.");
                if (definition.waves != null)
                {
                    for (var waveIndex = 0; waveIndex < definition.waves.Length; waveIndex++)
                    {
                        var wave = definition.waves[waveIndex];
                        ValidateEnemyRoster(
                            report,
                            enemyIds,
                            wave,
                            $"Encounter '{definition.id}' wave {waveIndex + 1}",
                            true);
                        if (wave != null)
                        {
                            largestWave = Mathf.Max(largestWave, wave.Length);
                        }
                    }
                }

                report.Check(
                    definition.spawnOffsets != null,
                    $"Encounter '{definition.id}' spawnOffsets is null.");
                if (definition.spawnOffsets != null)
                {
                    report.Check(
                        definition.spawnOffsets.Length >= largestWave,
                        $"Encounter '{definition.id}' has {definition.spawnOffsets.Length} spawn offsets " +
                        $"for a largest wave of {largestWave} enemies.");
                    for (var index = 0; index < definition.spawnOffsets.Length; index++)
                    {
                        ValidateVector(
                            report,
                            definition.spawnOffsets[index],
                            $"Encounter '{definition.id}' spawnOffsets[{index}]",
                            100f);
                    }
                }
            }

            var placementPersistenceIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < catalogs.EncounterPlacements.Count; index++)
            {
                var placement = catalogs.EncounterPlacements[index];
                report.Check(
                    placement != null,
                    $"EncounterCatalog.ExtendedPlacements[{index}] is null.");
                if (placement == null)
                {
                    continue;
                }

                RequireReference(
                    report,
                    encounterIds,
                    placement.encounterId,
                    $"EncounterCatalog.ExtendedPlacements[{index}]",
                    "encounter");
                ValidatePosition(
                    report,
                    placement.position,
                    $"Encounter placement '{placement.encounterId}' position");
                var persistenceId = string.IsNullOrWhiteSpace(placement.persistenceId)
                    ? BuildEncounterPersistenceId(placement.encounterId, placement.position)
                    : placement.persistenceId;
                report.Check(
                    !string.IsNullOrWhiteSpace(persistenceId),
                    $"Encounter placement '{placement.encounterId}' has no usable persistence ID.");
                if (!string.IsNullOrWhiteSpace(persistenceId))
                {
                    report.Check(
                        placementPersistenceIds.Add(persistenceId),
                        $"Encounter placement persistence ID '{persistenceId}' is duplicated. " +
                        "Move one camp or assign a unique stable persistenceId.");
                }
            }

            RequireReference(
                report,
                encounterIds,
                "encounter.alderwake.hollow_reed",
                "WorldBuilder Alderwake camp",
                "encounter");
            RequireReference(
                report,
                encounterIds,
                "encounter.morrowfen.sunken_watch",
                "WorldBuilder Morrowfen camp",
                "encounter");
            RequireReference(
                report,
                encounterIds,
                "encounter.cinderwold.ashjaw",
                "WorldBuilder Cinderwold camp",
                "encounter");
            RequireReference(
                report,
                encounterIds,
                "encounter.alderwake.mossglass",
                "WorldExpansionBuilder Alderwake camp",
                "encounter");
            RequireReference(
                report,
                encounterIds,
                "encounter.morrowfen.fen_tithe",
                "WorldExpansionBuilder Morrowfen camp",
                "encounter");
            RequireReference(
                report,
                encounterIds,
                "encounter.cinderwold.relay_breakers",
                "WorldExpansionBuilder Cinderwold camp",
                "encounter");

            foreach (var routeEvent in catalogs.RouteEvents)
            {
                if (routeEvent == null)
                {
                    continue;
                }

                report.Check(
                    routeEvent.waves != null && routeEvent.waves.Length > 0,
                    $"Route event '{routeEvent.id}' has no enemy waves.");
                if (routeEvent.waves == null)
                {
                    continue;
                }

                for (var waveIndex = 0; waveIndex < routeEvent.waves.Length; waveIndex++)
                {
                    ValidateEnemyRoster(
                        report,
                        enemyIds,
                        routeEvent.waves[waveIndex],
                        $"Route event '{routeEvent.id}' wave {waveIndex + 1}",
                        true);
                }
            }

            foreach (var city in catalogs.Cities)
            {
                if (city == null || city.activities == null)
                {
                    continue;
                }

                foreach (var activity in city.activities)
                {
                    if (activity == null)
                    {
                        continue;
                    }

                    ValidateEnemyRoster(
                        report,
                        enemyIds,
                        activity.enemyIds,
                        $"City activity '{activity.id}' enemyIds",
                        activity.kind == CityActivityKind.Training);
                }
            }

            foreach (var bounty in catalogs.Bounties)
            {
                if (bounty == null)
                {
                    continue;
                }

                RequireReference(
                    report,
                    enemyIds,
                    bounty.targetEnemyId,
                    $"Bounty '{bounty.id}'",
                    "target enemy");
                ValidateEnemyRoster(
                    report,
                    enemyIds,
                    bounty.reinforcementEnemyIds,
                    $"Bounty '{bounty.id}' reinforcementEnemyIds",
                    false);
            }

            foreach (var boss in catalogs.Bosses)
            {
                if (boss == null)
                {
                    continue;
                }

                RequireText(report, boss.displayName, $"Boss '{boss.id}' displayName");
                report.Check(
                    boss.maxHealth > 0,
                    $"Boss '{boss.id}' has maxHealth={boss.maxHealth}. Use a positive value.");
                CheckPositiveFinite(
                    report,
                    boss.phaseOneMoveSpeed,
                    $"Boss '{boss.id}' phaseOneMoveSpeed");
                CheckPositiveFinite(
                    report,
                    boss.phaseTwoMoveSpeed,
                    $"Boss '{boss.id}' phaseTwoMoveSpeed");
                report.Check(
                    boss.phaseThresholdOne > 0f && boss.phaseThresholdOne < 1f,
                    $"Boss '{boss.id}' phaseThresholdOne={boss.phaseThresholdOne}. Use a value in (0, 1).");
                report.Check(
                    boss.phaseThresholdTwo == 0f ||
                    (boss.phaseThresholdTwo > 0f &&
                     boss.phaseThresholdTwo < boss.phaseThresholdOne),
                    $"Boss '{boss.id}' phaseThresholdTwo={boss.phaseThresholdTwo} must be zero or below " +
                    $"phaseThresholdOne={boss.phaseThresholdOne}.");
                report.Check(
                    boss.phaseOneActions != null && boss.phaseOneActions.Count > 0,
                    $"Boss '{boss.id}' has no phase-one actions.");
                report.Check(
                    boss.phaseTwoActions != null && boss.phaseTwoActions.Count > 0,
                    $"Boss '{boss.id}' has no phase-two actions.");
                if (!string.IsNullOrWhiteSpace(boss.summonedEnemyId))
                {
                    RequireReference(
                        report,
                        enemyIds,
                        boss.summonedEnemyId,
                        $"Boss '{boss.id}'",
                        "summoned enemy");
                }
            }

            var surgeWaves = GetPrivateStaticField<string[][]>(typeof(AshwakeTrial), "SurgeWaves");
            report.Check(
                surgeWaves != null && surgeWaves.Length > 0,
                "AshwakeTrial.SurgeWaves is null or empty.");
            if (surgeWaves != null)
            {
                for (var waveIndex = 0; waveIndex < surgeWaves.Length; waveIndex++)
                {
                    ValidateEnemyRoster(
                        report,
                        enemyIds,
                        surgeWaves[waveIndex],
                        $"AshwakeTrial.SurgeWaves[{waveIndex}]",
                        true);
                }
            }

            var routeEventIds = GetIds(catalogs.RouteEvents, definition => definition.id);
            var fieldCacheIds = GetIds(catalogs.FieldCaches, definition => definition.id);
            RequireReference(
                report,
                routeEventIds,
                CinderwoldArcCatalog.EmberfallEventId,
                "CinderwoldArcCatalog.EmberfallEventId",
                "route event");
            report.Check(
                CinderwoldArcCatalog.RequiredCacheIds != null,
                "CinderwoldArcCatalog.RequiredCacheIds is null.");
            if (CinderwoldArcCatalog.RequiredCacheIds != null)
            {
                foreach (var cacheId in CinderwoldArcCatalog.RequiredCacheIds)
                {
                    RequireReference(
                        report,
                        fieldCacheIds,
                        cacheId,
                        "CinderwoldArcCatalog.RequiredCacheIds",
                        "field cache");
                }
            }

            RequireReference(
                report,
                placementPersistenceIds,
                CinderwoldArcCatalog.AshjawMilestoneId,
                "CinderwoldArcCatalog.AshjawMilestoneId",
                "encounter placement persistence");
        }

        private static void VerifyWorldData(VerificationReport report, CatalogSnapshot catalogs)
        {
            VerifyRouteAndRestorationData(report, catalogs);
            VerifyProvisioningData(report, catalogs);
            VerifyCityData(report, catalogs);
            VerifyTrialData(report, catalogs);
            VerifyBountyData(report, catalogs);
            VerifyThreadwayData(report, catalogs);

            ValidatePosition(
                report,
                CinderwoldArcCatalog.LedgerPosition,
                "CinderwoldArcCatalog.LedgerPosition");
            ValidatePosition(
                report,
                CinderwoldArcCatalog.RelayPosition,
                "CinderwoldArcCatalog.RelayPosition");
            ValidatePosition(
                report,
                CinderwoldArcCatalog.DaisPosition,
                "CinderwoldArcCatalog.DaisPosition");
            ValidateCosts(
                report,
                CinderwoldArcCatalog.RouteboundCost,
                "CinderwoldArcCatalog.RouteboundCost",
                true);
        }

        private static void VerifyRouteAndRestorationData(
            VerificationReport report,
            CatalogSnapshot catalogs)
        {
            var routeEventIds = GetIds(catalogs.RouteEvents, definition => definition.id);
            var restorationIds = GetIds(catalogs.RestorationSites, definition => definition.id);

            foreach (var routeEvent in catalogs.RouteEvents)
            {
                if (routeEvent == null)
                {
                    continue;
                }

                RequireText(report, routeEvent.displayName, $"Route event '{routeEvent.id}' displayName");
                RequireText(report, routeEvent.description, $"Route event '{routeEvent.id}' description");
                ValidatePosition(report, routeEvent.position, $"Route event '{routeEvent.id}' position");
                report.Check(
                    routeEvent.favorReward >= 0,
                    $"Route event '{routeEvent.id}' has a negative Favor reward.");
                report.Check(
                    routeEvent.resourceAmount >= 0,
                    $"Route event '{routeEvent.id}' has resourceAmount={routeEvent.resourceAmount}.");
                if (routeEvent.repeatable)
                {
                    CheckPositiveFinite(
                        report,
                        routeEvent.cooldownSeconds,
                        $"Repeatable route event '{routeEvent.id}' cooldownSeconds");
                    report.Check(
                        routeEvent.repeatFavorReward >= 0 &&
                        routeEvent.repeatResourceAmount >= 0 &&
                        routeEvent.repeatExperienceReward >= 0,
                        $"Repeatable route event '{routeEvent.id}' has a negative repeat reward.");
                }

                ValidateRestorationReference(
                    report,
                    catalogs.RestorationSites,
                    routeEvent.requiredRestorationSiteId,
                    routeEvent.requiredRestorationTier,
                    $"Route event '{routeEvent.id}' required restoration");
                ValidateRestorationReference(
                    report,
                    catalogs.RestorationSites,
                    routeEvent.completionRestorationSiteId,
                    routeEvent.completionRestorationTier,
                    $"Route event '{routeEvent.id}' completion restoration");
            }

            foreach (var cache in catalogs.FieldCaches)
            {
                if (cache == null)
                {
                    continue;
                }

                RequireText(report, cache.displayName, $"Field cache '{cache.id}' displayName");
                RequireText(report, cache.inscription, $"Field cache '{cache.id}' inscription");
                RequireText(report, cache.regionId, $"Field cache '{cache.id}' regionId");
                ValidatePosition(report, cache.position, $"Field cache '{cache.id}' position");
                report.Check(
                    cache.resourceAmount > 0,
                    $"Field cache '{cache.id}' has resourceAmount={cache.resourceAmount}. Use a positive reward.");
                report.Check(
                    cache.favorReward >= 0,
                    $"Field cache '{cache.id}' has a negative Favor reward.");
            }

            foreach (var site in catalogs.RestorationSites)
            {
                if (site == null)
                {
                    continue;
                }

                RequireText(report, site.displayName, $"Restoration site '{site.id}' displayName");
                RequireText(report, site.description, $"Restoration site '{site.id}' description");
                ValidatePosition(report, site.position, $"Restoration site '{site.id}' position");
                report.Check(
                    site.maxTier > 0,
                    $"Restoration site '{site.id}' has maxTier={site.maxTier}. Use a positive tier count.");
                report.Check(
                    site.tiers != null,
                    $"Restoration site '{site.id}' tiers is null.");
                if (site.tiers == null)
                {
                    continue;
                }

                report.Check(
                    site.tiers.Count == site.maxTier,
                    $"Restoration site '{site.id}' declares maxTier={site.maxTier} but has " +
                    $"{site.tiers.Count} tier definitions.");
                var tierNumbers = new HashSet<int>();
                foreach (var tier in site.tiers)
                {
                    report.Check(
                        tier != null,
                        $"Restoration site '{site.id}' contains a null tier.");
                    if (tier == null)
                    {
                        continue;
                    }

                    report.Check(
                        tier.tier > 0 && tier.tier <= site.maxTier,
                        $"Restoration site '{site.id}' tier number {tier.tier} is outside 1..{site.maxTier}.");
                    report.Check(
                        tierNumbers.Add(tier.tier),
                        $"Restoration site '{site.id}' defines tier {tier.tier} more than once.");
                    RequireText(
                        report,
                        tier.title,
                        $"Restoration site '{site.id}' tier {tier.tier} title");
                    RequireText(
                        report,
                        tier.description,
                        $"Restoration site '{site.id}' tier {tier.tier} description");
                    ValidateCosts(
                        report,
                        tier.cost,
                        $"Restoration site '{site.id}' tier {tier.tier} cost",
                        tier.isManual);
                    report.Check(
                        tier.experienceReward >= 0 && tier.favorReward >= 0,
                        $"Restoration site '{site.id}' tier {tier.tier} has a negative reward.");

                    if (!tier.isManual)
                    {
                        RequireReference(
                            report,
                            routeEventIds,
                            tier.requiredRouteEventId,
                            $"Restoration site '{site.id}' tier {tier.tier}",
                            "route event");
                    }
                    else
                    {
                        report.Check(
                            string.IsNullOrWhiteSpace(tier.requiredRouteEventId),
                            $"Restoration site '{site.id}' tier {tier.tier} is manual but also requires " +
                            $"route event '{tier.requiredRouteEventId}'.");
                    }
                }

                for (var expectedTier = 1; expectedTier <= site.maxTier; expectedTier++)
                {
                    report.Check(
                        tierNumbers.Contains(expectedTier),
                        $"Restoration site '{site.id}' is missing tier {expectedTier}.");
                }
            }

            report.Check(
                restorationIds.Contains(RestorationCatalog.FenlightCausewayId),
                $"RestorationCatalog.FenlightCausewayId references missing restoration site " +
                $"'{RestorationCatalog.FenlightCausewayId}'.");
        }

        private static void VerifyProvisioningData(
            VerificationReport report,
            CatalogSnapshot catalogs)
        {
            foreach (var meal in catalogs.Meals)
            {
                if (meal == null)
                {
                    continue;
                }

                RequireText(report, meal.displayName, $"Meal '{meal.id}' displayName");
                RequireText(report, meal.description, $"Meal '{meal.id}' description");
                report.Check(
                    meal.minimumCampTier >= 0,
                    $"Meal '{meal.id}' has minimumCampTier={meal.minimumCampTier}.");
                report.Check(
                    meal.recipeUnlockFavorPrice >= 0,
                    $"Meal '{meal.id}' has a negative recipe unlock price.");
                ValidateCosts(report, meal.ingredients, $"Meal '{meal.id}' ingredients", true);
                report.Check(
                    meal.vitalityRestore >= 0,
                    $"Meal '{meal.id}' has vitalityRestore={meal.vitalityRestore}.");
                report.Check(
                    IsFinite(meal.staminaRestore) && meal.staminaRestore >= 0f,
                    $"Meal '{meal.id}' has invalid staminaRestore={meal.staminaRestore}.");
                if (meal.effect != MealEffect.Restore)
                {
                    CheckPositiveFinite(
                        report,
                        meal.effectDurationSeconds,
                        $"Meal '{meal.id}' effectDurationSeconds");
                }
            }

            foreach (var vendor in catalogs.Vendors)
            {
                if (vendor == null)
                {
                    continue;
                }

                RequireText(report, vendor.displayName, $"Vendor '{vendor.id}' displayName");
                RequireText(
                    report,
                    vendor.serviceDescription,
                    $"Vendor '{vendor.id}' serviceDescription");
                report.Check(vendor.supplies != null, $"Vendor '{vendor.id}' supplies is null.");
                if (vendor.supplies != null)
                {
                    for (var index = 0; index < vendor.supplies.Count; index++)
                    {
                        var supply = vendor.supplies[index];
                        report.Check(
                            supply != null,
                            $"Vendor '{vendor.id}' supplies[{index}] is null.");
                        if (supply == null)
                        {
                            continue;
                        }

                        report.Check(
                            supply.amount > 0,
                            $"Vendor '{vendor.id}' supply '{supply.kind}' has amount={supply.amount}.");
                        report.Check(
                            supply.favorPrice > 0,
                            $"Vendor '{vendor.id}' supply '{supply.kind}' has favorPrice={supply.favorPrice}.");
                    }
                }
            }

            foreach (var contract in catalogs.CampContracts)
            {
                if (contract == null)
                {
                    continue;
                }

                RequireText(report, contract.title, $"Camp contract '{contract.id}' title");
                RequireText(report, contract.description, $"Camp contract '{contract.id}' description");
                report.Check(
                    contract.minimumCampTier >= 0,
                    $"Camp contract '{contract.id}' has minimumCampTier={contract.minimumCampTier}.");
                ValidateCosts(
                    report,
                    contract.delivery,
                    $"Camp contract '{contract.id}' delivery",
                    true);
                report.Check(
                    contract.favorReward >= 0,
                    $"Camp contract '{contract.id}' has a negative Favor reward.");
            }
        }

        private static void VerifyCityData(VerificationReport report, CatalogSnapshot catalogs)
        {
            report.Check(catalogs.Cities.Count > 0, "CityCatalog contains no city definitions.");
            foreach (var city in catalogs.Cities)
            {
                if (city == null)
                {
                    continue;
                }

                RequireText(report, city.displayName, $"City '{city.id}' displayName");
                RequireText(report, city.subtitle, $"City '{city.id}' subtitle");
                ValidatePosition(report, city.center, $"City '{city.id}' center");
                report.Check(
                    city.residents != null && city.residents.Count > 0,
                    $"City '{city.id}' has no residents.");
                report.Check(
                    city.activities != null && city.activities.Count > 0,
                    $"City '{city.id}' has no civic activities.");

                if (city.residents != null)
                {
                    foreach (var resident in city.residents)
                    {
                        if (resident == null)
                        {
                            continue;
                        }

                        RequireText(
                            report,
                            resident.displayName,
                            $"City resident '{resident.id}' displayName");
                        RequireText(report, resident.role, $"City resident '{resident.id}' role");
                        RequireText(report, resident.dialogue, $"City resident '{resident.id}' dialogue");
                        ValidatePosition(
                            report,
                            resident.position,
                            $"City resident '{resident.id}' position");
                        report.Check(
                            IsFinite(resident.wanderRadius) && resident.wanderRadius >= 0f,
                            $"City resident '{resident.id}' has invalid wanderRadius={resident.wanderRadius}.");
                        report.Check(
                            IsFiniteVector(city.center) &&
                            IsFiniteVector(resident.position) &&
                            Vector3.Distance(city.center, resident.position) <= 250f,
                            $"City resident '{resident.id}' is more than 250 units from city '{city.id}'. " +
                            "Check its authored position.");
                    }
                }

                if (city.activities == null)
                {
                    continue;
                }

                foreach (var activity in city.activities)
                {
                    if (activity == null)
                    {
                        continue;
                    }

                    RequireText(report, activity.title, $"City activity '{activity.id}' title");
                    RequireText(report, activity.description, $"City activity '{activity.id}' description");
                    CheckPositiveFinite(
                        report,
                        activity.cooldownSeconds,
                        $"City activity '{activity.id}' cooldownSeconds");
                    report.Check(
                        activity.favorReward >= 0 && activity.experienceReward >= 0,
                        $"City activity '{activity.id}' has a negative reward.");
                    ValidateCosts(
                        report,
                        activity.cost,
                        $"City activity '{activity.id}' cost",
                        activity.kind == CityActivityKind.Delivery);

                    if (activity.kind == CityActivityKind.Training)
                    {
                        ValidatePosition(
                            report,
                            activity.arenaPosition,
                            $"City activity '{activity.id}' arenaPosition");
                        report.Check(
                            IsFiniteVector(city.center) &&
                            IsFiniteVector(activity.arenaPosition) &&
                            Vector3.Distance(city.center, activity.arenaPosition) <= 250f,
                            $"Training activity '{activity.id}' arena is more than 250 units from " +
                            $"city '{city.id}'. Check arenaPosition.");
                    }
                }
            }
        }

        private static void VerifyTrialData(VerificationReport report, CatalogSnapshot catalogs)
        {
            var bossIds = GetIds(catalogs.Bosses, definition => definition.id);
            report.Check(catalogs.Trials.Count > 0, "TrialCatalog contains no trial definitions.");
            foreach (var trial in catalogs.Trials)
            {
                if (trial == null)
                {
                    continue;
                }

                RequireText(report, trial.displayName, $"Trial '{trial.id}' displayName");
                ValidatePosition(report, trial.gatePosition, $"Trial '{trial.id}' gatePosition");
                ValidatePosition(report, trial.entrancePosition, $"Trial '{trial.id}' entrancePosition");
                ValidatePosition(report, trial.returnPosition, $"Trial '{trial.id}' returnPosition");
                RequireReference(
                    report,
                    bossIds,
                    trial.bossId,
                    $"Trial '{trial.id}'",
                    "boss");
                if (IsFiniteVector(trial.gatePosition) &&
                    IsFiniteVector(trial.entrancePosition) &&
                    IsFiniteVector(trial.returnPosition))
                {
                    report.Check(
                        Vector3.Distance(trial.gatePosition, trial.entrancePosition) > 1f,
                        $"Trial '{trial.id}' gatePosition and entrancePosition overlap.");
                    report.Check(
                        Vector3.Distance(trial.entrancePosition, trial.returnPosition) > 1f,
                        $"Trial '{trial.id}' entrancePosition and returnPosition overlap.");
                }
            }
        }

        private static void VerifyBountyData(VerificationReport report, CatalogSnapshot catalogs)
        {
            var cityIds = GetIds(catalogs.Cities, definition => definition.id);
            foreach (var bounty in catalogs.Bounties)
            {
                if (bounty == null)
                {
                    continue;
                }

                RequireText(report, bounty.title, $"Bounty '{bounty.id}' title");
                RequireText(report, bounty.description, $"Bounty '{bounty.id}' description");
                RequireText(report, bounty.phaseTitle, $"Bounty '{bounty.id}' phaseTitle");
                RequireReference(
                    report,
                    cityIds,
                    bounty.sponsorCityId,
                    $"Bounty '{bounty.id}'",
                    "sponsor city");
                ValidatePosition(report, bounty.position, $"Bounty '{bounty.id}' position");
                report.Check(
                    bounty.favorReward >= 0 && bounty.experienceReward >= 0,
                    $"Bounty '{bounty.id}' has a negative reward.");
                CheckPositiveFinite(
                    report,
                    bounty.cooldownSeconds,
                    $"Bounty '{bounty.id}' cooldownSeconds");
                ValidateCosts(
                    report,
                    bounty.materialReward,
                    $"Bounty '{bounty.id}' materialReward",
                    true);
            }
        }

        private static void VerifyThreadwayData(VerificationReport report, CatalogSnapshot catalogs)
        {
            report.Check(
                catalogs.Threadways.Count >= 2,
                "ThreadwayCatalog needs at least two anchors for travel between destinations.");
            for (var index = 0; index < catalogs.Threadways.Count; index++)
            {
                var threadway = catalogs.Threadways[index];
                if (threadway == null)
                {
                    continue;
                }

                RequireText(report, threadway.title, $"Threadway '{threadway.id}' title");
                RequireText(report, threadway.region, $"Threadway '{threadway.id}' region");
                RequireText(report, threadway.description, $"Threadway '{threadway.id}' description");
                ValidatePosition(report, threadway.position, $"Threadway '{threadway.id}' position");
                ValidatePosition(
                    report,
                    threadway.arrivalPosition,
                    $"Threadway '{threadway.id}' arrivalPosition");
                report.Check(
                    threadway.activationChimesaltCost >= 0,
                    $"Threadway '{threadway.id}' has activationChimesaltCost=" +
                    $"{threadway.activationChimesaltCost}.");
                if (IsFiniteVector(threadway.position) && IsFiniteVector(threadway.arrivalPosition))
                {
                    var arrivalDistance = Vector3.Distance(
                        threadway.position,
                        threadway.arrivalPosition);
                    report.Check(
                        arrivalDistance >= 0.25f && arrivalDistance <= 25f,
                        $"Threadway '{threadway.id}' arrival point is {arrivalDistance:0.##} units from " +
                        "its anchor. Keep it distinct and within 25 units.");
                }

                for (var otherIndex = index + 1; otherIndex < catalogs.Threadways.Count; otherIndex++)
                {
                    var other = catalogs.Threadways[otherIndex];
                    if (other == null ||
                        !IsFiniteVector(threadway.position) ||
                        !IsFiniteVector(other.position))
                    {
                        continue;
                    }

                    report.Check(
                        Vector3.Distance(threadway.position, other.position) >= 0.5f,
                        $"Threadways '{threadway.id}' and '{other.id}' have overlapping anchor positions.");
                }
            }
        }

        private static void VerifySaveNormalization(VerificationReport report)
        {
            var normalize = typeof(TetherfallSaveSystem).GetMethod(
                "Normalize",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(TetherfallSaveData) },
                null);
            report.Check(
                normalize != null,
                "TetherfallSaveSystem.Normalize(TetherfallSaveData) was not found by reflection.");
            if (normalize == null)
            {
                return;
            }

            var expectedSchema = new TetherfallSaveData().schemaVersion;
            var listFields = GetSaveListFields();
            report.Check(
                listFields.Count > 0,
                "No save-list fields were discovered on TetherfallSaveData.");

            var legacy = new TetherfallSaveData
            {
                schemaVersion = 1,
                cinderwoldArc = null,
                pactRoster = null,
                playerSettlement = null
            };
            foreach (var field in listFields)
            {
                field.SetValue(legacy, null);
            }

            InvokeNormalize(normalize, legacy);
            report.Check(
                legacy.schemaVersion == expectedSchema,
                $"Save normalization produced schemaVersion={legacy.schemaVersion}; expected {expectedSchema}.");
            report.Check(
                legacy.cinderwoldArc != null,
                "Save normalization left cinderwoldArc null.");
            report.Check(
                legacy.pactRoster != null,
                "Save normalization left pactRoster null.");
            report.Check(
                legacy.playerSettlement != null,
                "Save normalization left playerSettlement null.");
            var normalizedJson = JsonUtility.ToJson(legacy);
            report.Check(
                normalizedJson.Contains("\"pactRoster\"") &&
                normalizedJson.Contains("\"playerSettlement\""),
                "Normalized JSON does not persist both pactRoster and playerSettlement.");
            foreach (var field in listFields)
            {
                report.Check(
                    field.GetValue(legacy) != null,
                    $"Save normalization left TetherfallSaveData.{field.Name} null.");
            }

            var preserved = new TetherfallSaveData
            {
                schemaVersion = 1,
                health = 73,
                playerPosition = new Vector3(12f, 3f, -9f),
                activePactName = "verification-pact"
            };
            preserved.resources.Add(new ResourceAmount
            {
                kind = ResourceKind.Ore,
                amount = 7
            });

            var originalLists = new Dictionary<FieldInfo, object>();
            foreach (var field in listFields)
            {
                var value = field.GetValue(preserved);
                report.Check(
                    value != null,
                    $"New TetherfallSaveData initializes {field.Name} to null.");
                if (value != null)
                {
                    originalLists[field] = value;
                }
            }

            var originalArc = preserved.cinderwoldArc;
            InvokeNormalize(normalize, preserved);
            foreach (var pair in originalLists)
            {
                report.Check(
                    ReferenceEquals(pair.Value, pair.Key.GetValue(preserved)),
                    $"Save normalization replaced existing list '{pair.Key.Name}' instead of preserving it.");
            }

            report.Check(
                ReferenceEquals(originalArc, preserved.cinderwoldArc),
                "Save normalization replaced an existing cinderwoldArc record.");
            report.Check(
                preserved.health == 73 &&
                preserved.playerPosition == new Vector3(12f, 3f, -9f) &&
                preserved.activePactName == "verification-pact",
                "Save normalization changed unrelated scalar or position data.");
            report.Check(
                preserved.resources.Count == 1 &&
                preserved.resources[0] != null &&
                preserved.resources[0].amount == 7,
                "Save normalization changed existing resource data.");

            var normalizedLists = new Dictionary<FieldInfo, object>();
            foreach (var field in listFields)
            {
                normalizedLists[field] = field.GetValue(preserved);
            }

            InvokeNormalize(normalize, preserved);
            foreach (var pair in normalizedLists)
            {
                report.Check(
                    ReferenceEquals(pair.Value, pair.Key.GetValue(preserved)),
                    $"Save normalization is not idempotent for list '{pair.Key.Name}'.");
            }
        }

        private static void VerifyControlDocumentation(VerificationReport report)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var readmePath = Path.Combine(projectRoot, "README.md");
            report.Check(
                File.Exists(readmePath),
                $"Required control documentation is missing: '{readmePath}'.");
            if (!File.Exists(readmePath))
            {
                return;
            }

            var readme = File.ReadAllText(readmePath);
            var controls = ExtractMarkdownSection(readme, "Controls");
            report.Check(
                !string.IsNullOrWhiteSpace(controls),
                "README.md is missing a '## Controls' section.");
            if (string.IsNullOrWhiteSpace(controls))
            {
                return;
            }

            RequireDocumentedControl(report, controls, "`WASD`", "movement");
            RequireDocumentedControl(report, controls, "Mouse:", "camera control");
            RequireDocumentedControl(report, controls, "`Shift`", "sprint");
            RequireDocumentedControl(report, controls, "`Space`", "jump, glide, and climb");
            RequireDocumentedControl(report, controls, "`Left Ctrl`", "dodge and swim descent");
            RequireDocumentedControl(report, controls, "Right mouse", "guard");
            RequireDocumentedControl(report, controls, "`F` or left click", "attack");
            RequireDocumentedControl(report, controls, "`G`", "weapon resonance ability");
            RequireDocumentedControl(report, controls, "`L` or middle mouse", "Threadglass focus");
            RequireDocumentedControl(report, controls, "`V`", "tactical command or Rally");
            RequireDocumentedControl(report, controls, "`Z`", "expedition order");
            RequireDocumentedControl(report, controls, "`X`", "prepared provision");
            RequireDocumentedControl(report, controls, "`1`, `2`, `3`", "weapon switching");
            RequireDocumentedControl(report, controls, "`E`", "interaction");
            RequireDocumentedControl(report, controls, "`I`", "equipment");
            RequireDocumentedControl(report, controls, "`J`", "quest ledger");
            RequireDocumentedControl(report, controls, "`N`", "settlement planning");
            RequireDocumentedControl(report, controls, "`B`", "crafting and camp services");
            RequireDocumentedControl(report, controls, "`Q`", "party or Rally Beat 2");
            RequireDocumentedControl(report, controls, "`T`", "Pact or Rally Beat 1");
            RequireDocumentedControl(report, controls, "`C`", "portable camp");
            RequireDocumentedControl(report, controls, "`R`", "mount");
            RequireDocumentedControl(report, controls, "`Tab`", "field log");
            RequireDocumentedControl(report, controls, "`F1`", "guidance visibility");
            RequireDocumentedControl(report, controls, "`K`", "skip guidance");
            RequireDocumentedControl(report, controls, "`H`", "control visibility");
            RequireDocumentedControl(report, controls, "`Escape`", "panel/cursor release");
        }

        private static void ValidateDictionaryIds<T>(
            VerificationReport report,
            string catalogName,
            Dictionary<string, T> definitions,
            Func<T, string> idSelector,
            Dictionary<string, string> globalIds)
            where T : class
        {
            report.Check(definitions != null, $"{catalogName} is null.");
            if (definitions == null)
            {
                return;
            }

            foreach (var pair in definitions)
            {
                report.Check(
                    !string.IsNullOrWhiteSpace(pair.Key),
                    $"{catalogName} contains a null, empty, or whitespace dictionary key.");
                report.Check(
                    pair.Value != null,
                    $"{catalogName}['{pair.Key}'] is null.");
                if (pair.Value == null)
                {
                    continue;
                }

                var definitionId = idSelector(pair.Value);
                report.Check(
                    string.Equals(pair.Key, definitionId, StringComparison.Ordinal),
                    $"{catalogName} key '{pair.Key}' does not match definition ID '{definitionId}'.");
            }

            ValidateIds(report, catalogName, definitions.Values, idSelector, globalIds);
        }

        private static void ValidateIds<T>(
            VerificationReport report,
            string catalogName,
            IEnumerable<T> definitions,
            Func<T, string> idSelector,
            Dictionary<string, string> globalIds)
            where T : class
        {
            report.Check(definitions != null, $"{catalogName} is null.");
            if (definitions == null)
            {
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var index = 0;
            foreach (var definition in definitions)
            {
                report.Check(
                    definition != null,
                    $"{catalogName}[{index}] is null. Remove it or provide a definition.");
                if (definition != null)
                {
                    var id = idSelector(definition);
                    report.Check(
                        !string.IsNullOrWhiteSpace(id),
                        $"{catalogName}[{index}] has a null, empty, or whitespace ID.");
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        report.Check(
                            id == id.Trim(),
                            $"{catalogName}[{index}] ID '{id}' has leading or trailing whitespace.");
                        report.Check(
                            ids.Add(id),
                            $"{catalogName} contains duplicate ID '{id}'.");
                        RegisterGlobalId(report, globalIds, id, catalogName);
                    }
                }

                index++;
            }

            report.Check(index > 0, $"{catalogName} contains no definitions.");
        }

        private static void RegisterGlobalId(
            VerificationReport report,
            Dictionary<string, string> globalIds,
            string id,
            string owner)
        {
            if (globalIds.TryGetValue(id, out var existingOwner))
            {
                report.Check(
                    false,
                    $"Stable ID '{id}' is defined by both {existingOwner} and {owner}. " +
                    "Use a globally unique content ID.");
                return;
            }

            globalIds.Add(id, owner);
        }

        private static HashSet<string> GetIds<T>(
            IEnumerable<T> definitions,
            Func<T, string> idSelector)
            where T : class
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (definitions == null)
            {
                return result;
            }

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                var id = idSelector(definition);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    result.Add(id);
                }
            }

            return result;
        }

        private static void RequireQuestReference(
            VerificationReport report,
            HashSet<string> questIds,
            HashSet<string> referencedIds,
            string questId,
            string source)
        {
            if (!string.IsNullOrWhiteSpace(questId))
            {
                referencedIds.Add(questId);
            }

            RequireReference(report, questIds, questId, source, "quest");
        }

        private static void CheckQuestRequirement(
            VerificationReport report,
            List<QuestRecord> quests,
            string questId,
            int expected,
            string expectedSource)
        {
            var quest = FindById(quests, questId, definition => definition.id);
            report.Check(
                quest != null,
                $"Cannot verify quest '{questId}' objective count because the quest is missing.");
            if (quest != null)
            {
                report.Check(
                    quest.required == expected,
                    $"Quest '{questId}' requires {quest.required}, but {expectedSource} is {expected}. " +
                    "Update the quest count or its referenced content.");
            }
        }

        private static void RequireEquipment(
            VerificationReport report,
            List<EquipmentDefinition> equipment,
            string id,
            EquipmentSlot expectedSlot,
            WeaponDiscipline expectedDiscipline,
            string source)
        {
            var definition = FindById(equipment, id, item => item.id);
            report.Check(
                definition != null,
                $"{source} references missing equipment '{id}'.");
            if (definition == null)
            {
                return;
            }

            report.Check(
                definition.slot == expectedSlot,
                $"{source} expects equipment '{id}' in slot {expectedSlot}, but it uses {definition.slot}.");
            report.Check(
                definition.discipline == expectedDiscipline,
                $"{source} expects equipment '{id}' to use {expectedDiscipline}, " +
                $"but it uses {definition.discipline}.");
        }

        private static void RequireReference(
            VerificationReport report,
            HashSet<string> targetIds,
            string referencedId,
            string source,
            string targetKind)
        {
            report.Check(
                !string.IsNullOrWhiteSpace(referencedId),
                $"{source} has a null, empty, or whitespace {targetKind} ID.");
            if (string.IsNullOrWhiteSpace(referencedId))
            {
                return;
            }

            report.Check(
                targetIds.Contains(referencedId),
                $"{source} references missing {targetKind} ID '{referencedId}'.");
        }

        private static void ValidateEnemyRoster(
            VerificationReport report,
            HashSet<string> enemyIds,
            string[] roster,
            string source,
            bool requireEnemies)
        {
            report.Check(
                roster != null,
                $"{source} is null.");
            if (roster == null)
            {
                return;
            }

            if (requireEnemies)
            {
                report.Check(roster.Length > 0, $"{source} is empty.");
            }

            for (var index = 0; index < roster.Length; index++)
            {
                RequireReference(
                    report,
                    enemyIds,
                    roster[index],
                    $"{source}[{index}]",
                    "enemy");
            }
        }

        private static void ValidateRestorationReference(
            VerificationReport report,
            IReadOnlyList<RestorationSiteDefinition> sites,
            string siteId,
            int tier,
            string source)
        {
            if (string.IsNullOrWhiteSpace(siteId))
            {
                report.Check(
                    tier == 0,
                    $"{source} has tier {tier} but no restoration site ID.");
                return;
            }

            var site = FindById(sites, siteId, definition => definition.id);
            report.Check(
                site != null,
                $"{source} references missing restoration site '{siteId}'.");
            if (site != null)
            {
                report.Check(
                    tier > 0 && tier <= site.maxTier,
                    $"{source} references tier {tier}, outside restoration site '{siteId}' range " +
                    $"1..{site.maxTier}.");
            }
        }

        private static void ValidateCosts(
            VerificationReport report,
            IList<ResourceCost> costs,
            string source,
            bool requireCost)
        {
            report.Check(costs != null, $"{source} is null.");
            if (costs == null)
            {
                return;
            }

            if (requireCost)
            {
                report.Check(costs.Count > 0, $"{source} is empty.");
            }

            for (var index = 0; index < costs.Count; index++)
            {
                var cost = costs[index];
                report.Check(cost != null, $"{source}[{index}] is null.");
                if (cost == null)
                {
                    continue;
                }

                report.Check(
                    Enum.IsDefined(typeof(ResourceKind), cost.kind),
                    $"{source}[{index}] has invalid resource kind '{cost.kind}'.");
                report.Check(
                    cost.amount > 0,
                    $"{source}[{index}] for '{cost.kind}' has amount={cost.amount}. Use a positive amount.");
            }
        }

        private static void RequireText(
            VerificationReport report,
            string value,
            string fieldDescription)
        {
            report.Check(
                !string.IsNullOrWhiteSpace(value),
                $"{fieldDescription} is null, empty, or whitespace.");
        }

        private static void ValidatePosition(
            VerificationReport report,
            Vector3 position,
            string fieldDescription)
        {
            ValidateVector(report, position, fieldDescription, MaximumWorldCoordinate);
        }

        private static void ValidateVector(
            VerificationReport report,
            Vector3 value,
            string fieldDescription,
            float maximumAbsoluteComponent)
        {
            report.Check(
                IsFiniteVector(value),
                $"{fieldDescription} contains NaN or Infinity: {value}.");
            if (!IsFiniteVector(value))
            {
                return;
            }

            report.Check(
                Mathf.Abs(value.x) <= maximumAbsoluteComponent &&
                Mathf.Abs(value.y) <= maximumAbsoluteComponent &&
                Mathf.Abs(value.z) <= maximumAbsoluteComponent,
                $"{fieldDescription}={value} exceeds the allowed coordinate magnitude " +
                $"{maximumAbsoluteComponent}. Check for a misplaced decimal or unit.");
        }

        private static void CheckFinite(
            VerificationReport report,
            float value,
            string fieldDescription)
        {
            report.Check(IsFinite(value), $"{fieldDescription} is NaN or Infinity.");
        }

        private static void CheckPositiveFinite(
            VerificationReport report,
            float value,
            string fieldDescription)
        {
            report.Check(
                IsFinite(value) && value > 0f,
                $"{fieldDescription}={value}. Use a finite value greater than zero.");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFiniteVector(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static string BuildEncounterPersistenceId(string encounterId, Vector3 position)
        {
            return string.IsNullOrWhiteSpace(encounterId)
                ? string.Empty
                : $"{encounterId}@{Mathf.RoundToInt(position.x)}:{Mathf.RoundToInt(position.z)}";
        }

        private static T FindById<T>(
            IEnumerable<T> definitions,
            string id,
            Func<T, string> idSelector)
            where T : class
        {
            if (definitions == null || string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            foreach (var definition in definitions)
            {
                if (definition != null &&
                    string.Equals(idSelector(definition), id, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        private static T GetPrivateStaticField<T>(Type ownerType, string fieldName)
        {
            var field = ownerType.GetField(
                fieldName,
                BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(ownerType.FullName, fieldName);
            }

            var value = field.GetValue(null);
            if (value == null)
            {
                return default;
            }

            if (value is T typedValue)
            {
                return typedValue;
            }

            throw new InvalidCastException(
                $"{ownerType.FullName}.{fieldName} is {value.GetType().FullName}, not {typeof(T).FullName}.");
        }

        private static List<T> GetPublicStaticDefinitions<T>(Type ownerType)
            where T : class
        {
            var definitions = new List<T>();
            var fields = ownerType.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (typeof(T).IsAssignableFrom(field.FieldType))
                {
                    definitions.Add(field.GetValue(null) as T);
                }
            }

            var properties = ownerType.GetProperties(BindingFlags.Static | BindingFlags.Public);
            foreach (var property in properties)
            {
                if (property.GetIndexParameters().Length == 0 &&
                    typeof(T).IsAssignableFrom(property.PropertyType))
                {
                    var value = property.GetValue(null, null) as T;
                    if (!definitions.Contains(value))
                    {
                        definitions.Add(value);
                    }
                }
            }

            if (definitions.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{ownerType.FullName} exposes no public static {typeof(T).Name} definitions.");
            }

            return definitions;
        }

        private static List<FieldInfo> GetSaveListFields()
        {
            var result = new List<FieldInfo>();
            var fields = typeof(TetherfallSaveData).GetFields(
                BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (typeof(IList).IsAssignableFrom(field.FieldType))
                {
                    result.Add(field);
                }
            }

            return result;
        }

        private static void InvokeNormalize(MethodInfo normalize, TetherfallSaveData data)
        {
            try
            {
                normalize.Invoke(null, new object[] { data });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static string ExtractMarkdownSection(string markdown, string heading)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return string.Empty;
            }

            var marker = "## " + heading;
            var start = markdown.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                return string.Empty;
            }

            var contentStart = start + marker.Length;
            var nextHeading = markdown.IndexOf(
                "\n## ",
                contentStart,
                StringComparison.OrdinalIgnoreCase);
            return nextHeading < 0
                ? markdown.Substring(contentStart)
                : markdown.Substring(contentStart, nextHeading - contentStart);
        }

        private static void RequireDocumentedControl(
            VerificationReport report,
            string controlsSection,
            string requiredText,
            string action)
        {
            report.Check(
                controlsSection.IndexOf(requiredText, StringComparison.OrdinalIgnoreCase) >= 0,
                $"README.md Controls must document {action} using '{requiredText}'.");
        }

        private sealed class CatalogSnapshot
        {
            public Dictionary<string, EnemyDefinition> Enemies;
            public Dictionary<string, EncounterDefinition> Encounters;
            public IReadOnlyList<EncounterPlacement> EncounterPlacements;
            public IReadOnlyList<RouteEventDefinition> RouteEvents;
            public IReadOnlyList<FieldCacheDefinition> FieldCaches;
            public IReadOnlyList<RestorationSiteDefinition> RestorationSites;
            public IReadOnlyList<MealDefinition> Meals;
            public IReadOnlyList<VendorProfileDefinition> Vendors;
            public IReadOnlyList<CampContractDefinition> CampContracts;
            public List<EquipmentDefinition> Equipment;
            public List<CrossweaveDefinition> Crossweaves;
            public List<BossDefinition> Bosses;
            public List<TrialDefinition> Trials;
            public List<CityDefinition> Cities;
            public IReadOnlyList<BountyDefinition> Bounties;
            public IReadOnlyList<ThreadwayDefinition> Threadways;
            public List<QuestRecord> Quests;

            public static CatalogSnapshot Load()
            {
                return new CatalogSnapshot
                {
                    Enemies = GetPrivateStaticField<Dictionary<string, EnemyDefinition>>(
                        typeof(EnemyCatalog),
                        "Definitions"),
                    Encounters = GetPrivateStaticField<Dictionary<string, EncounterDefinition>>(
                        typeof(EncounterCatalog),
                        "Definitions"),
                    EncounterPlacements = EncounterCatalog.ExtendedPlacements,
                    RouteEvents = RouteEventCatalog.All,
                    FieldCaches = FieldCacheCatalog.All,
                    RestorationSites = RestorationCatalog.All,
                    Meals = ProvisioningCatalog.Meals,
                    Vendors = ProvisioningCatalog.Vendors,
                    CampContracts = ProvisioningCatalog.CampContracts,
                    Equipment = EquipmentCatalog.CreateAll(),
                    Crossweaves = GetPrivateStaticField<List<CrossweaveDefinition>>(
                        typeof(CrossweaveCatalog),
                        "Definitions"),
                    Bosses = GetPrivateStaticField<List<BossDefinition>>(
                        typeof(BossCatalog),
                        "Definitions"),
                    Trials = GetPublicStaticDefinitions<TrialDefinition>(typeof(TrialCatalog)),
                    Cities = GetPublicStaticDefinitions<CityDefinition>(typeof(CityCatalog)),
                    Bounties = BountyCatalog.All,
                    Threadways = ThreadwayCatalog.All,
                    Quests = QuestCatalog.CreateAll()
                };
            }
        }

        private sealed class VerificationReport
        {
            private readonly List<string> failures = new List<string>();
            private int assertionCount;
            private int sectionCount;

            public void Run(string section, Action action)
            {
                sectionCount++;
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    Fail($"{section} check could not complete: {DescribeException(exception)}");
                }
            }

            public void Check(bool condition, string failure)
            {
                assertionCount++;
                if (!condition)
                {
                    Fail(failure);
                }
            }

            public void Complete()
            {
                if (failures.Count == 0)
                {
                    Debug.Log(
                        $"Tetherfall full-game verification passed: {assertionCount} assertions " +
                        $"across {sectionCount} sections. Play Mode was not entered.");
                    return;
                }

                var message = new StringBuilder();
                message.Append("Tetherfall full-game verification failed with ");
                message.Append(failures.Count);
                message.AppendLine(" actionable issue(s):");
                for (var index = 0; index < failures.Count; index++)
                {
                    message.Append(index + 1);
                    message.Append(". ");
                    message.AppendLine(failures[index]);
                }

                message.Append(
                    $"Checked {assertionCount} assertions across {sectionCount} sections; Play Mode was not entered.");
                var failureMessage = message.ToString();
                Debug.LogError(failureMessage);
                throw new InvalidOperationException(failureMessage);
            }

            private void Fail(string failure)
            {
                if (!string.IsNullOrWhiteSpace(failure) && !failures.Contains(failure))
                {
                    failures.Add(failure);
                }
            }

            private static string DescribeException(Exception exception)
            {
                var current = exception;
                while (current.InnerException != null &&
                       (current is TargetInvocationException ||
                        current is TypeInitializationException))
                {
                    current = current.InnerException;
                }

                return $"{current.GetType().Name}: {current.Message}";
            }
        }
    }
}
