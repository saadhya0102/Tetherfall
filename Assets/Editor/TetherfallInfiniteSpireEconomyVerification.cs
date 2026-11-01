#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallInfiniteSpireEconomyVerification
    {
        private const string MenuPath =
            "Tetherfall/Verify Infinite Spire Save and Economy";
        private static int assertions;

        [MenuItem(MenuPath, false, 2220)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Infinite Spire save and economy verification is edit-mode only.");
            }

            VerifyInternal(true);
        }

        public static void VerifyFromCommandLine()
        {
            VerifyInternal(true);
        }

        public static void VerifyForFullGame()
        {
            VerifyInternal(false);
        }

        private static void VerifyInternal(bool logSuccess)
        {
            assertions = 0;
            VerifySchemaDefaultsAndNormalization();
            VerifyRunStartAndResume();
            VerifyDeathRetention();
            VerifyMeridianRetirementRules();
            VerifyBankRateTiers();
            VerifyFavorLedger();
            VerifyLowFloorDiminishingReturns();
            VerifyCityServices();
            VerifyChestAndMerchantGuards();
            VerifyJsonUtilityFieldTypes();
            VerifyNoWorldMutationOrPrematureIntegration();

            if (logSuccess)
            {
                Debug.Log(
                    $"Infinite Spire save and economy verification passed: {assertions} assertions " +
                    "cover recovery, banking, Favor, services, deterministic stock, and save-only transitions.");
            }
        }

        private static void VerifySchemaDefaultsAndNormalization()
        {
            var root = new TetherfallSaveData();
            Assert(
                TetherfallSaveData.CurrentSchemaVersion == 24,
                "The foundation must not bump the main save schema before schema-25 integration.");
            Assert(
                root.infiniteSpire != null &&
                root.infiniteSpire.version == InfiniteSpireSaveData.CurrentVersion,
                "New saves must initialize the Infinite Spire versioned payload.");
            Assert(
                !root.infiniteSpire.runActive &&
                root.infiniteSpire.currentRoomId == InfiniteSpireStableIds.CityGateId &&
                root.infiniteSpire.recoveryRoomId == InfiniteSpireStableIds.CityGateId,
                "The default Infinite Spire payload must be safely inactive at the city gate.");

            var nullReport = InfiniteSpireSaveNormalization.NormalizeInPlace(null);
            Assert(
                nullReport.inputWasNull,
                "Null Infinite Spire normalization must return a report without throwing.");

            var corrupt = new InfiniteSpireSaveData
            {
                unlocked = true,
                runActive = true,
                recoveryPending = true,
                runSeed = int.MinValue,
                floor = -12,
                room = int.MaxValue,
                currentFloorId = "bad floor",
                currentRoomId = "bad room",
                recoveryRoomId = "bad recovery",
                spirecoil = -900,
                bankedSpiremark = 321,
                personalBestFloor = 44,
                favorLedgerUtcDay = 2345,
                favorEarnedOnUtcDay = 177,
                permanentCoilFindBonus = 0.12f,
                permanentDeathRetainBonus = 0.14f,
                recentTemplateIds = null,
                recentBossIds = null,
                meridianVisits = null,
                activeRoomPuzzle = null,
                activeRoomEnemies = null,
                openedChestIds = null,
                activeMerchantStock = null,
                purchasedMerchantStockIds = null,
                roomLocalHistory = null
            };
            var corruptReport =
                InfiniteSpireSaveNormalization.NormalizeInPlace(corrupt);
            Assert(
                corruptReport.recoveredCorruptRun &&
                !corrupt.runActive &&
                !corrupt.recoveryPending &&
                corrupt.currentRoomId == InfiniteSpireStableIds.CityGateId &&
                corrupt.recoveryRoomId == InfiniteSpireStableIds.CityGateId,
                "A corrupt active run must recover inactive at the city gate.");
            Assert(
                corrupt.bankedSpiremark == 321 &&
                corrupt.personalBestFloor == 44 &&
                corrupt.favorLedgerUtcDay == 2345 &&
                corrupt.favorEarnedOnUtcDay == 177 &&
                Approximately(corrupt.permanentCoilFindBonus, 0.12f) &&
                Approximately(corrupt.permanentDeathRetainBonus, 0.14f),
                "Corrupt-run recovery must preserve the permanent currency, best, Favor, and bonus ledger.");
            Assert(
                corrupt.recentTemplateIds != null &&
                corrupt.recentBossIds != null &&
                corrupt.meridianVisits != null &&
                corrupt.activeRoomPuzzle != null &&
                corrupt.activeRoomEnemies != null &&
                corrupt.openedChestIds != null &&
                corrupt.activeMerchantStock != null &&
                corrupt.purchasedMerchantStockIds != null &&
                corrupt.roomLocalHistory != null,
                "Normalization must initialize every nullable Infinite Spire collection and room record.");

            var valid = CreateActiveRun(7, 3, false);
            valid.runSeed = int.MinValue;
            valid.currentRoomId = InfiniteSpireStableIds.CreateRoomId(
                valid.runSeed,
                valid.floor,
                valid.room);
            valid.recoveryRoomId = InfiniteSpireStableIds.CreateRoomId(
                valid.runSeed,
                valid.floor,
                0);
            valid.permanentCoilFindBonus = float.NaN;
            valid.permanentDeathRetainBonus = float.PositiveInfinity;
            valid.recentTemplateIds = new List<string>();
            for (var index = 0;
                 index < InfiniteSpireSaveNormalization.MaximumRecentTemplates + 5;
                 index++)
            {
                valid.recentTemplateIds.Add(
                    InfiniteSpireStableIds.TemplatePrefix + index);
            }

            valid.roomLocalHistory.Add(new InfiniteSpireRoomHistorySaveData
            {
                floor = 6,
                roomId = InfiniteSpireStableIds.CreateRoomId(
                    valid.runSeed,
                    6,
                    1)
            });
            valid.roomLocalHistory.Add(new InfiniteSpireRoomHistorySaveData
            {
                floor = 7,
                roomId = InfiniteSpireStableIds.CreateRoomId(
                    valid.runSeed,
                    7,
                    2)
            });
            var validReport =
                InfiniteSpireSaveNormalization.NormalizeInPlace(valid);
            Assert(
                !validReport.recoveredCorruptRun &&
                valid.runActive &&
                valid.runSeed == int.MinValue,
                "A valid active run and its signed seed must survive normalization.");
            Assert(
                valid.recentTemplateIds.Count ==
                InfiniteSpireSaveNormalization.MaximumRecentTemplates &&
                valid.recentTemplateIds[0] ==
                InfiniteSpireStableIds.TemplatePrefix + "0",
                "The recent template window must be bounded without dropping its newest entry.");
            Assert(
                valid.roomLocalHistory.Count == 1 &&
                valid.roomLocalHistory[0].floor == 7,
                "Room-local history must be trimmed to the current floor window.");
            Assert(
                Approximately(valid.permanentCoilFindBonus, 0f) &&
                Approximately(valid.permanentDeathRetainBonus, 0f),
                "Non-finite permanent bonuses must normalize to zero.");

            var hooked = new InfiniteSpireSaveData
            {
                unlocked = true,
                runActive = true,
                runSeed = -19,
                floor = 9,
                room = 2,
                currentFloorId = "future_floor_alpha",
                currentRoomId = "future_room_alpha",
                recoveryRoomId = "future_recovery_alpha"
            };
            var hooks = new InfiniteSpireIdValidationHooks
            {
                floorIdValidator = value => value.StartsWith(
                    "future_floor_",
                    StringComparison.Ordinal),
                roomIdValidator = value => value.StartsWith(
                    "future_room_",
                    StringComparison.Ordinal),
                recoveryIdValidator = value => value.StartsWith(
                    "future_recovery_",
                    StringComparison.Ordinal),
                tryParseFloor = delegate(string _, out int parsedFloor)
                {
                    parsedFloor = 9;
                    return true;
                }
            };
            var hookReport =
                InfiniteSpireSaveNormalization.NormalizeInPlace(hooked, hooks);
            Assert(
                !hookReport.recoveredCorruptRun && hooked.runActive,
                "Catalog-facing validation and floor-parse hooks must allow future semantic IDs.");

            var json = JsonUtility.ToJson(valid);
            var roundTrip = JsonUtility.FromJson<InfiniteSpireSaveData>(json);
            Assert(
                roundTrip != null &&
                roundTrip.runSeed == int.MinValue &&
                roundTrip.recentTemplateIds != null &&
                roundTrip.recentTemplateIds.Count ==
                InfiniteSpireSaveNormalization.MaximumRecentTemplates,
                "JsonUtility must round-trip the signed seed and bounded history.");
        }

        private static void VerifyDeathRetention()
        {
            Assert(
                Approximately(
                    InfiniteSpireEconomy.GetDeathRetentionRate(0f),
                    0.45f) &&
                InfiniteSpireEconomy.GetRetainedSpirecoilOnDeath(100, 0f) == 45,
                "Death retention must begin at 45%.");
            Assert(
                Approximately(
                    InfiniteSpireEconomy.GetDeathRetentionRate(0.15f),
                    0.60f) &&
                Approximately(
                    InfiniteSpireEconomy.GetDeathRetentionRate(99f),
                    0.60f) &&
                InfiniteSpireEconomy.GetRetainedSpirecoilOnDeath(
                    100,
                    0.15f) == 60,
                "Death-retention upgrades must cap the final rate at 60%.");

            var run = CreateActiveRun(12, 4, false);
            run.spirecoil = 100;
            var death = InfiniteSpireRunTransitions.ApplyDeath(run);
            Assert(
                death.Applied &&
                death.RequestCriticalSave &&
                death.SpirecoilLost == 55 &&
                run.spirecoil == 45,
                "ApplyDeath must expose its critical-save request and exact death tax.");
            Assert(
                run.runActive &&
                run.recoveryPending &&
                InfiniteSpireRunTransitions.DecideResume(run).Kind ==
                InfiniteSpireResumeKind.RecoverAtSavedRoom,
                "Death must keep the run active and select saved-room recovery.");

            var recovery =
                InfiniteSpireRunTransitions.AcknowledgeRecovery(run);
            Assert(
                recovery.Applied &&
                recovery.RequestCriticalSave &&
                run.runActive &&
                !run.recoveryPending &&
                run.currentRoomId == run.recoveryRoomId,
                "Recovery acknowledgement must remain a save-only active-run transition.");
        }

        private static void VerifyRunStartAndResume()
        {
            var locked = new InfiniteSpireSaveData();
            Assert(
                !InfiniteSpireRunTransitions.CanStart(locked) &&
                InfiniteSpireRunTransitions.Start(locked, int.MinValue).Status ==
                InfiniteSpireTransitionStatus.Locked,
                "A locked Infinite Spire record must not start a run.");

            locked.unlocked = true;
            Assert(
                InfiniteSpireRunTransitions.CanStart(locked),
                "An unlocked inactive Infinite Spire record must permit a run.");
            var started =
                InfiniteSpireRunTransitions.Start(locked, int.MinValue);
            Assert(
                started.Applied &&
                started.RequestCriticalSave &&
                locked.runActive &&
                locked.runSeed == int.MinValue &&
                locked.floor == 1 &&
                locked.recoveryRoomId ==
                InfiniteSpireStableIds.CreateRoomId(int.MinValue, 1, 0),
                "Start must preserve a signed seed and return critical-save intent.");
            Assert(
                !InfiniteSpireRunTransitions.CanStart(locked) &&
                InfiniteSpireRunTransitions.Start(locked, 7).Status ==
                InfiniteSpireTransitionStatus.RunAlreadyActive,
                "A second start must not replace an active run.");

            var resume = InfiniteSpireRunTransitions.DecideResume(locked);
            Assert(
                resume.Kind == InfiniteSpireResumeKind.ResumeCurrentRoom &&
                resume.DestinationId == locked.currentRoomId &&
                !resume.ShouldDeactivateRun,
                "A valid active run must resume at its current room.");

            locked.currentFloorId = "corrupt";
            var corruptResume =
                InfiniteSpireRunTransitions.DecideResume(locked);
            Assert(
                corruptResume.Kind ==
                InfiniteSpireResumeKind.RecoverCorruptRunAtCityGate &&
                corruptResume.DestinationId ==
                InfiniteSpireStableIds.CityGateId &&
                corruptResume.ShouldDeactivateRun &&
                corruptResume.RequestCriticalSave,
                "A corrupt active-run resume must request safe city-gate deactivation.");
        }

        private static void VerifyMeridianRetirementRules()
        {
            var ordinary = CreateActiveRun(18, 3, false);
            ordinary.spirecoil = 500;
            Assert(
                !InfiniteSpireRunTransitions.CanRetire(ordinary),
                "Retirement must be unavailable outside Loomrest Meridian.");
            var denied = InfiniteSpireRunTransitions.ApplyFullRetirement(
                ordinary,
                80,
                TimeSpan.TicksPerDay * 10L);
            Assert(
                !denied.Applied &&
                denied.Status ==
                InfiniteSpireTransitionStatus.NotAtLoomrestMeridian &&
                ordinary.runActive &&
                ordinary.spirecoil == 500,
                "A non-Meridian retirement attempt must leave the run untouched.");

            var partialRun = CreateActiveRun(30, 6, true);
            partialRun.spirecoil = 1000;
            Assert(
                InfiniteSpireRunTransitions.CanRetire(partialRun),
                "Loomrest Meridian must permit retirement and banking.");
            var partial =
                InfiniteSpireRunTransitions.ApplyPartialBank(partialRun);
            Assert(
                partial.Applied &&
                partial.RequestCriticalSave &&
                partial.BankedSpirecoil == 280 &&
                partial.SpiremarkAwarded == 11 &&
                partialRun.spirecoil == 600 &&
                partialRun.runActive,
                "Bank and Continue must extract 40%, bank it, and keep the run active.");
            var duplicate =
                InfiniteSpireRunTransitions.ApplyPartialBank(partialRun);
            Assert(
                !duplicate.Applied &&
                duplicate.Status ==
                InfiniteSpireTransitionStatus.PartialBankAlreadyUsed &&
                partialRun.spirecoil == 600,
                "Bank and Continue must be available only once per Meridian visit.");

            var retirement = InfiniteSpireRunTransitions.ApplyFullRetirement(
                partialRun,
                120,
                TimeSpan.TicksPerDay * 10L);
            Assert(
                retirement.Applied &&
                retirement.RequestCriticalSave &&
                !partialRun.runActive &&
                partialRun.currentRoomId ==
                InfiniteSpireStableIds.CityGateId &&
                retirement.DestinationId ==
                InfiniteSpireStableIds.CityGateId,
                "Full retirement must bank and safely end the run at the city gate.");
        }

        private static void VerifyBankRateTiers()
        {
            Assert(
                Approximately(InfiniteSpireEconomy.GetBankRate(6), 1f) &&
                Approximately(InfiniteSpireEconomy.GetBankRate(18), 0.85f) &&
                Approximately(InfiniteSpireEconomy.GetBankRate(30), 0.70f) &&
                Approximately(InfiniteSpireEconomy.GetBankRate(42), 0.55f),
                "Loomrest bank-rate tiers must remain 1.0, 0.85, 0.70, and 0.55.");
            Assert(
                InfiniteSpireEconomy.CalculateFullRetirementBank(101, 6)
                    .BankedSpirecoil == 101 &&
                InfiniteSpireEconomy.CalculateFullRetirementBank(101, 18)
                    .BankedSpirecoil == 85 &&
                InfiniteSpireEconomy.CalculateFullRetirementBank(101, 30)
                    .BankedSpirecoil == 70 &&
                InfiniteSpireEconomy.CalculateFullRetirementBank(101, 42)
                    .BankedSpirecoil == 55,
                "Bank conversion must floor each tier deterministically.");
            Assert(
                InfiniteSpireEconomy.ConvertBankedSpirecoilToSpiremark(24) == 0 &&
                InfiniteSpireEconomy.ConvertBankedSpirecoilToSpiremark(25) == 1 &&
                InfiniteSpireEconomy.ConvertBankedSpirecoilToSpiremark(74) == 2,
                "Spiremark conversion must use integer bankedCoil / 25.");
        }

        private static void VerifyFavorLedger()
        {
            var day = 42L;
            var ticks = day * TimeSpan.TicksPerDay;
            var first = InfiniteSpireEconomy.CalculateFavorAward(
                500,
                day,
                0,
                ticks);
            Assert(
                first.EligibleFavor == 120 &&
                first.FavorAwarded == 120 &&
                first.FavorEarnedOnUtcDay == 120,
                "A retirement must offer at most 120 Favor.");

            var crossing = InfiniteSpireEconomy.CalculateFavorAward(
                120,
                day,
                first.FavorEarnedOnUtcDay,
                ticks);
            Assert(
                crossing.FullRateFavor == 60 &&
                crossing.ExcessFavor == 60 &&
                crossing.FavorAwarded == 75 &&
                crossing.FavorEarnedOnUtcDay == 195,
                "Favor above the 180 daily soft cap must convert at 25%.");

            var capped = InfiniteSpireEconomy.CalculateFavorAward(
                120,
                day,
                180,
                ticks);
            Assert(
                capped.FullRateFavor == 0 &&
                capped.ExcessFavor == 120 &&
                capped.FavorAwarded == 30,
                "A day already at the soft cap must convert all eligible Favor at 25%.");

            var rollover = InfiniteSpireEconomy.CalculateFavorAward(
                120,
                day,
                999,
                ticks + TimeSpan.TicksPerDay);
            Assert(
                rollover.RolledToNewUtcDay &&
                rollover.UtcDay == day + 1 &&
                rollover.FavorAwarded == 120 &&
                rollover.FavorEarnedOnUtcDay == 120,
                "The Favor ledger must reset on a UTC-day rollover.");
        }

        private static void VerifyLowFloorDiminishingReturns()
        {
            Assert(
                Approximately(
                    InfiniteSpireEconomy.GetLowFloorRewardMultiplier(80, 100),
                    1f) &&
                Approximately(
                    InfiniteSpireEconomy.GetLowFloorRewardMultiplier(60, 100),
                    0.75f) &&
                Approximately(
                    InfiniteSpireEconomy.GetLowFloorRewardMultiplier(30, 100),
                    0.50f) &&
                Approximately(
                    InfiniteSpireEconomy.GetLowFloorRewardMultiplier(10, 100),
                    0.35f),
                "Low-floor rewards must diminish in bounded tiers relative to personal best.");
            var fresh = InfiniteSpireEconomy.GetRoomSpirecoilReward(10, 10, 0f);
            var repeated =
                InfiniteSpireEconomy.GetRoomSpirecoilReward(10, 100, 0f);
            Assert(
                repeated < fresh && repeated > 0,
                "Replaying far below personal best must reduce, not erase, room rewards.");
            Assert(
                InfiniteSpireEconomy.GetBossSpirecoilReward(20, 20, 0f) >
                InfiniteSpireEconomy.GetMinibossSpirecoilReward(20, 20, 0f) &&
                InfiniteSpireEconomy.GetMinibossSpirecoilReward(20, 20, 0f) >
                InfiniteSpireEconomy.GetTreasureSpirecoilReward(20, 20, 0f) &&
                InfiniteSpireEconomy.GetTreasureSpirecoilReward(20, 20, 0f) >
                InfiniteSpireEconomy.GetRoomSpirecoilReward(20, 20, 0f),
                "Boss, miniboss, treasure, and room Spirecoil rewards must retain their order.");
        }

        private static void VerifyCityServices()
        {
            Assert(
                InfiniteSpireCityServices.All.Count == 4 &&
                InfiniteSpireCityServices.Find("coil_attunement") != null &&
                InfiniteSpireCityServices.Find("seal_temper") != null &&
                InfiniteSpireCityServices.Find("meridian_chart") != null &&
                InfiniteSpireCityServices.Find("field_ration") != null,
                "The four semantic city service IDs must be defined exactly.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0;
                 index < InfiniteSpireCityServices.All.Count;
                 index++)
            {
                var service = InfiniteSpireCityServices.All[index];
                Assert(
                    ids.Add(service.Id) &&
                    service.BaseSpiremarkCost > 0 &&
                    !service.UsesRealMoney &&
                    !service.UsesRandomPaidPull &&
                    !service.GrantsPaidCombatPower,
                    $"City service '{service.Id}' must have a positive earned cost and no paid or random pull.");
            }

            Assert(
                InfiniteSpireCityServices.CoilAttunement.GetSpiremarkCost(0) == 6 &&
                InfiniteSpireCityServices.CoilAttunement.GetSpiremarkCost(4) == 22 &&
                InfiniteSpireCityServices.SealTemper.GetSpiremarkCost(0) == 8 &&
                InfiniteSpireCityServices.SealTemper.GetSpiremarkCost(4) == 28,
                "Permanent service costs must scale deterministically by completed rank.");

            var balance = 1000;
            var coilBonus = 0f;
            for (var rank = 0; rank < 5; rank++)
            {
                var quote = InfiniteSpireEconomy.QuoteCityService(
                    InfiniteSpireCityServices.CoilAttunementId,
                    balance,
                    coilBonus,
                    0f,
                    false,
                    0);
                Assert(
                    quote.CanPurchase && quote.RequestCriticalSave,
                    "Each Coil Attunement rank below the cap must be purchasable with enough Spiremark.");
                balance = quote.RemainingSpiremark;
                coilBonus = quote.CoilFindBonus;
            }

            var atCap = InfiniteSpireEconomy.QuoteCityService(
                InfiniteSpireCityServices.CoilAttunementId,
                balance,
                coilBonus,
                0f,
                false,
                0);
            Assert(
                Approximately(coilBonus, 0.15f) &&
                !atCap.CanPurchase &&
                atCap.Status == InfiniteSpireTransitionStatus.ServiceAtCap,
                "Permanent coil-find service bonuses must cap at 15%.");

            var deathAtCap = InfiniteSpireEconomy.QuoteCityService(
                InfiniteSpireCityServices.SealTemperId,
                100,
                0f,
                0.15f,
                false,
                0);
            Assert(
                !deathAtCap.CanPurchase &&
                deathAtCap.Status ==
                InfiniteSpireTransitionStatus.ServiceAtCap,
                "Permanent death-retain service bonuses must cap at 15%.");

            var chart = InfiniteSpireEconomy.QuoteCityService(
                InfiniteSpireCityServices.MeridianChartId,
                20,
                0f,
                0f,
                false,
                0);
            Assert(
                chart.CanPurchase &&
                chart.MeridianChartUnlocked &&
                chart.RemainingSpiremark == 8,
                "Meridian Chart must be a deterministic one-time service.");
        }

        private static void VerifyChestAndMerchantGuards()
        {
            var run = CreateActiveRun(24, 5, false);
            run.spirecoil = 1000;
            var roomKey = new InfiniteSpireRoomInstanceKey(
                run.runSeed,
                run.floor,
                run.room);
            var chestId = InfiniteSpireInstanceIds.FormatChestId(roomKey);
            Assert(
                InfiniteSpireRunTransitions.CanOpenChest(run, chestId),
                "An unopened current-floor chest must be claimable.");
            var chest = InfiniteSpireRunTransitions.RecordChestOpened(
                run,
                chestId);
            var duplicateChest =
                InfiniteSpireRunTransitions.RecordChestOpened(run, chestId);
            Assert(
                chest.Applied &&
                chest.RequestCriticalSave &&
                !duplicateChest.Applied &&
                duplicateChest.Status ==
                InfiniteSpireTransitionStatus.AlreadyClaimed,
                "Chest state must reject a second claim and request a save on the first.");

            run.activeMerchantId = InfiniteSpireIds.MerchantFieldCounter;
            var stockA = InfiniteSpireEconomy.BuildMerchantStock(
                run.runSeed,
                run.floor,
                run.activeMerchantId,
                4);
            var stockB = InfiniteSpireEconomy.BuildMerchantStock(
                run.runSeed,
                run.floor,
                run.activeMerchantId,
                4);
            Assert(
                stockA.Count == 4 &&
                stockB.Count == 4 &&
                stockA[0].stockId == stockB[0].stockId &&
                stockA[0].itemId == stockB[0].itemId &&
                stockA[0].spirecoilPrice == stockB[0].spirecoilPrice,
                "Merchant stock IDs, items, and prices must be deterministic.");

            var generatedStockId = InfiniteSpireEconomy.CreateMerchantStockId(
                run.runSeed,
                run.floor,
                run.room,
                0);
            Assert(
                InfiniteSpireInstanceIds.TryParse(
                    generatedStockId,
                    out var parsedStockId) &&
                parsedStockId.Kind == InfiniteSpireInstanceEntityKind.Stock &&
                parsedStockId.Room == roomKey,
                "Merchant stock helpers must use the shared deterministic room-instance ID format.");

            run.activeMerchantStock = stockA;
            var stockId = stockA[0].stockId;
            Assert(
                InfiniteSpireRunTransitions.CanPurchaseMerchantStock(
                    run,
                    stockId),
                "Affordable unpurchased deterministic stock must be purchasable.");
            var purchase = InfiniteSpireRunTransitions.PurchaseMerchantStock(
                run,
                stockId);
            var duplicatePurchase =
                InfiniteSpireRunTransitions.PurchaseMerchantStock(run, stockId);
            Assert(
                purchase.Applied &&
                purchase.RequestCriticalSave &&
                !duplicatePurchase.Applied &&
                duplicatePurchase.Status ==
                InfiniteSpireTransitionStatus.AlreadyClaimed,
                "Merchant stock state must reject a double purchase.");
        }

        private static void VerifyJsonUtilityFieldTypes()
        {
            var saveTypes = new[]
            {
                typeof(InfiniteSpireSaveData),
                typeof(InfiniteSpirePuzzleStateSaveData),
                typeof(InfiniteSpireEnemyStateSaveData),
                typeof(InfiniteSpireMerchantStockSaveData),
                typeof(InfiniteSpireRoomHistorySaveData),
                typeof(InfiniteSpireMeridianVisitSaveData)
            };
            for (var index = 0; index < saveTypes.Length; index++)
            {
                var type = saveTypes[index];
                Assert(
                    type.IsSerializable,
                    $"{type.Name} must carry SerializableAttribute for JsonUtility.");
                var fields = type.GetFields(
                    BindingFlags.Instance | BindingFlags.Public);
                Assert(
                    fields.Length > 0,
                    $"{type.Name} must expose serialized public fields.");
                for (var fieldIndex = 0;
                     fieldIndex < fields.Length;
                     fieldIndex++)
                {
                    Assert(
                        IsJsonUtilityCompatible(fields[fieldIndex].FieldType),
                        $"{type.Name}.{fields[fieldIndex].Name} uses unsupported JsonUtility type " +
                        $"{fields[fieldIndex].FieldType.FullName}.");
                }
            }

            var rootField = typeof(TetherfallSaveData).GetField(
                "infiniteSpire",
                BindingFlags.Instance | BindingFlags.Public);
            Assert(
                rootField != null &&
                rootField.FieldType == typeof(InfiniteSpireSaveData) &&
                rootField.GetValue(new TetherfallSaveData()) != null,
                "TetherfallSaveData must expose only the initialized InfiniteSpireSaveData extension field.");
        }

        private static void VerifyNoWorldMutationOrPrematureIntegration()
        {
            var saveSource = ReadSource(
                "Scripts",
                "TetherfallInfiniteSpireSave.cs");
            var economySource = ReadSource(
                "Scripts",
                "TetherfallInfiniteSpireEconomy.cs");
            var runtimeSource = saveSource + "\n" + economySource;
            var forbiddenWorldTokens = new[]
            {
                "using UnityEngine",
                "GameObject",
                "MonoBehaviour",
                "Transform",
                "SceneManager",
                "Physics.",
                "TetherfallGame",
                "RequestCriticalSave();",
                "new Random("
            };
            for (var index = 0; index < forbiddenWorldTokens.Length; index++)
            {
                Assert(
                    !runtimeSource.Contains(forbiddenWorldTokens[index]),
                    $"Infinite Spire foundation source must not contain world mutation token " +
                    $"'{forbiddenWorldTokens[index]}'.");
            }

            Assert(
                !runtimeSource.Contains("\u2014"),
                "Infinite Spire foundation copy must not contain em dashes.");
            Assert(
                economySource.Contains("public bool RequestCriticalSave") &&
                economySource.Contains("public static class InfiniteSpireRunTransitions"),
                "Critical-save intent must be returned by transition results for later runtime integration.");

            var mainSaveSource = ReadSource(
                "Scripts",
                "TetherfallSaveSystem.cs");
            Assert(
                mainSaveSource.Contains(
                    "public const int CurrentSchemaVersion = 24;") &&
                !mainSaveSource.Contains(
                    "InfiniteSpireSaveNormalization.NormalizeInPlace"),
                "The foundation must leave schema 24 and main normalization unchanged until schema-25 integration.");
        }

        private static InfiniteSpireSaveData CreateActiveRun(
            int floor,
            int room,
            bool atMeridian)
        {
            const int runSeed = -731;
            var currentRoomId = InfiniteSpireStableIds.CreateRoomId(
                runSeed,
                floor,
                room);
            return new InfiniteSpireSaveData
            {
                unlocked = true,
                runActive = true,
                runSeed = runSeed,
                floor = floor,
                room = room,
                currentFloorId = InfiniteSpireStableIds.CreateFloorId(floor),
                currentRoomId = currentRoomId,
                currentTemplateId = atMeridian
                    ? InfiniteSpireIds.RestLoomrestMeridian
                    : InfiniteSpireIds.CombatRibArena,
                recoveryRoomId = InfiniteSpireStableIds.CreateRoomId(
                    runSeed,
                    floor,
                    0)
            };
        }

        private static bool IsJsonUtilityCompatible(Type type)
        {
            if (type == typeof(string) ||
                type == typeof(bool) ||
                type == typeof(byte) ||
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong) ||
                type == typeof(float) ||
                type == typeof(double) ||
                type.IsEnum)
            {
                return true;
            }

            if (type.IsArray)
            {
                return IsJsonUtilityCompatible(type.GetElementType());
            }

            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return IsJsonUtilityCompatible(type.GetGenericArguments()[0]);
            }

            return type.IsClass && type.IsSerializable;
        }

        private static string ReadSource(string folder, string fileName)
        {
            var path = Path.Combine(Application.dataPath, folder, fileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Infinite Spire verification source is missing: {path}",
                    path);
            }

            return File.ReadAllText(path);
        }

        private static bool Approximately(float left, float right)
        {
            return Math.Abs(left - right) <= 0.0001f;
        }

        private static void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
#endif
