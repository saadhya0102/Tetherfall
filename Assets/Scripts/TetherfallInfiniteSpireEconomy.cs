using System;
using System.Collections.Generic;
using System.Globalization;

namespace Tetherfall
{
    public enum InfiniteSpireRewardKind
    {
        Room,
        Miniboss,
        Boss,
        Treasure
    }

    public enum InfiniteSpireTransitionStatus
    {
        Applied,
        MissingSaveData,
        Locked,
        RunAlreadyActive,
        RunInactive,
        InvalidId,
        InvalidLocation,
        NotAtLoomrestMeridian,
        PartialBankAlreadyUsed,
        InsufficientSpirecoil,
        InsufficientSpiremark,
        AlreadyClaimed,
        StockNotFound,
        ServiceUnavailable,
        ServiceAtCap
    }

    public enum InfiniteSpireResumeKind
    {
        LockedAtCityGate,
        InactiveAtCityGate,
        ResumeCurrentRoom,
        RecoverAtSavedRoom,
        RecoverCorruptRunAtCityGate
    }

    public enum InfiniteSpireCityServiceKind
    {
        CoilAttunement,
        SealTemper,
        MeridianChart,
        FieldRation
    }

    public readonly struct InfiniteSpireBankResult
    {
        public InfiniteSpireBankResult(
            int carriedSpirecoil,
            int extractedSpirecoil,
            int bankedSpirecoil,
            int remainingSpirecoil,
            int spiremarkAwarded,
            float bankRate)
        {
            CarriedSpirecoil = carriedSpirecoil;
            ExtractedSpirecoil = extractedSpirecoil;
            BankedSpirecoil = bankedSpirecoil;
            RemainingSpirecoil = remainingSpirecoil;
            SpiremarkAwarded = spiremarkAwarded;
            BankRate = bankRate;
        }

        public int CarriedSpirecoil { get; }
        public int ExtractedSpirecoil { get; }
        public int BankedSpirecoil { get; }
        public int RemainingSpirecoil { get; }
        public int SpiremarkAwarded { get; }
        public float BankRate { get; }
    }

    public readonly struct InfiniteSpireFavorResult
    {
        public InfiniteSpireFavorResult(
            long utcDay,
            int requestedFavor,
            int eligibleFavor,
            int fullRateFavor,
            int excessFavor,
            int favorAwarded,
            int favorEarnedOnUtcDay,
            bool rolledToNewUtcDay)
        {
            UtcDay = utcDay;
            RequestedFavor = requestedFavor;
            EligibleFavor = eligibleFavor;
            FullRateFavor = fullRateFavor;
            ExcessFavor = excessFavor;
            FavorAwarded = favorAwarded;
            FavorEarnedOnUtcDay = favorEarnedOnUtcDay;
            RolledToNewUtcDay = rolledToNewUtcDay;
        }

        public long UtcDay { get; }
        public int RequestedFavor { get; }
        public int EligibleFavor { get; }
        public int FullRateFavor { get; }
        public int ExcessFavor { get; }
        public int FavorAwarded { get; }
        public int FavorEarnedOnUtcDay { get; }
        public bool RolledToNewUtcDay { get; }
    }

    public readonly struct InfiniteSpireTransitionResult
    {
        internal InfiniteSpireTransitionResult(
            InfiniteSpireTransitionStatus status,
            bool requestCriticalSave = false,
            int spirecoilBefore = 0,
            int spirecoilAfter = 0,
            int spirecoilLost = 0,
            int bankedSpirecoil = 0,
            int spiremarkAwarded = 0,
            int favorAwarded = 0,
            string destinationId = null)
        {
            Status = status;
            RequestCriticalSave = requestCriticalSave;
            SpirecoilBefore = spirecoilBefore;
            SpirecoilAfter = spirecoilAfter;
            SpirecoilLost = spirecoilLost;
            BankedSpirecoil = bankedSpirecoil;
            SpiremarkAwarded = spiremarkAwarded;
            FavorAwarded = favorAwarded;
            DestinationId = destinationId ?? string.Empty;
        }

        public InfiniteSpireTransitionStatus Status { get; }
        public bool Applied => Status == InfiniteSpireTransitionStatus.Applied;
        public bool RequestCriticalSave { get; }
        public int SpirecoilBefore { get; }
        public int SpirecoilAfter { get; }
        public int SpirecoilLost { get; }
        public int BankedSpirecoil { get; }
        public int SpiremarkAwarded { get; }
        public int FavorAwarded { get; }
        public string DestinationId { get; }
    }

    public readonly struct InfiniteSpireResumeDecision
    {
        public InfiniteSpireResumeDecision(
            InfiniteSpireResumeKind kind,
            string destinationId,
            bool shouldDeactivateRun,
            bool requestCriticalSave)
        {
            Kind = kind;
            DestinationId = destinationId ?? InfiniteSpireStableIds.CityGateId;
            ShouldDeactivateRun = shouldDeactivateRun;
            RequestCriticalSave = requestCriticalSave;
        }

        public InfiniteSpireResumeKind Kind { get; }
        public string DestinationId { get; }
        public bool ShouldDeactivateRun { get; }
        public bool RequestCriticalSave { get; }
    }

    public sealed class InfiniteSpireCityServiceDefinition
    {
        public InfiniteSpireCityServiceDefinition(
            string id,
            string displayName,
            string description,
            InfiniteSpireCityServiceKind kind,
            int baseSpiremarkCost,
            int spiremarkCostPerRank,
            float bonusPerPurchase,
            float maximumPermanentBonus,
            int maximumPurchases)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Kind = kind;
            BaseSpiremarkCost = baseSpiremarkCost;
            SpiremarkCostPerRank = spiremarkCostPerRank;
            BonusPerPurchase = bonusPerPurchase;
            MaximumPermanentBonus = maximumPermanentBonus;
            MaximumPurchases = maximumPurchases;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public InfiniteSpireCityServiceKind Kind { get; }
        public int BaseSpiremarkCost { get; }
        public int SpiremarkCostPerRank { get; }
        public float BonusPerPurchase { get; }
        public float MaximumPermanentBonus { get; }
        public int MaximumPurchases { get; }

        public bool UsesRealMoney => false;
        public bool UsesRandomPaidPull => false;
        public bool GrantsPaidCombatPower => false;

        public int GetSpiremarkCost(int completedPurchases)
        {
            var rank = Math.Max(0, completedPurchases);
            var cost = (long)BaseSpiremarkCost +
                       (long)SpiremarkCostPerRank * rank;
            return cost > InfiniteSpireSaveNormalization.MaximumResource
                ? InfiniteSpireSaveNormalization.MaximumResource
                : (int)Math.Max(0L, cost);
        }
    }

    public readonly struct InfiniteSpireCityServicePurchaseResult
    {
        internal InfiniteSpireCityServicePurchaseResult(
            InfiniteSpireTransitionStatus status,
            string serviceId,
            int spiremarkCost,
            int remainingSpiremark,
            float coilFindBonus,
            float deathRetainBonus,
            bool meridianChartUnlocked,
            int fieldRations)
        {
            Status = status;
            ServiceId = serviceId ?? string.Empty;
            SpiremarkCost = spiremarkCost;
            RemainingSpiremark = remainingSpiremark;
            CoilFindBonus = coilFindBonus;
            DeathRetainBonus = deathRetainBonus;
            MeridianChartUnlocked = meridianChartUnlocked;
            FieldRations = fieldRations;
        }

        public InfiniteSpireTransitionStatus Status { get; }
        public bool CanPurchase => Status == InfiniteSpireTransitionStatus.Applied;
        public bool RequestCriticalSave => CanPurchase;
        public string ServiceId { get; }
        public int SpiremarkCost { get; }
        public int RemainingSpiremark { get; }
        public float CoilFindBonus { get; }
        public float DeathRetainBonus { get; }
        public bool MeridianChartUnlocked { get; }
        public int FieldRations { get; }
    }

    public static class InfiniteSpireCityServices
    {
        public const string CoilAttunementId = "coil_attunement";
        public const string SealTemperId = "seal_temper";
        public const string MeridianChartId = "meridian_chart";
        public const string FieldRationId = "field_ration";

        public static readonly InfiniteSpireCityServiceDefinition CoilAttunement =
            new InfiniteSpireCityServiceDefinition(
                CoilAttunementId,
                "Coil Attunement",
                "Tune the earned-coil finder. Each rank adds 3%, up to 15%.",
                InfiniteSpireCityServiceKind.CoilAttunement,
                6,
                4,
                0.03f,
                InfiniteSpireSaveNormalization.MaximumPermanentBonus,
                5);

        public static readonly InfiniteSpireCityServiceDefinition SealTemper =
            new InfiniteSpireCityServiceDefinition(
                SealTemperId,
                "Seal Temper",
                "Temper death retention by 3% per rank, up to 15%.",
                InfiniteSpireCityServiceKind.SealTemper,
                8,
                5,
                0.03f,
                InfiniteSpireSaveNormalization.MaximumPermanentBonus,
                5);

        public static readonly InfiniteSpireCityServiceDefinition MeridianChart =
            new InfiniteSpireCityServiceDefinition(
                MeridianChartId,
                "Meridian Chart",
                "Unlock a fixed route reference for Loomrest Meridian planning.",
                InfiniteSpireCityServiceKind.MeridianChart,
                12,
                0,
                0f,
                0f,
                1);

        public static readonly InfiniteSpireCityServiceDefinition FieldRation =
            new InfiniteSpireCityServiceDefinition(
                FieldRationId,
                "Field Ration",
                "Exchange earned Spiremark for one known ration. No random pull.",
                InfiniteSpireCityServiceKind.FieldRation,
                3,
                0,
                0f,
                0f,
                InfiniteSpireSaveNormalization.MaximumFieldRations);

        private static readonly InfiniteSpireCityServiceDefinition[] Definitions =
        {
            CoilAttunement,
            SealTemper,
            MeridianChart,
            FieldRation
        };

        public static IReadOnlyList<InfiniteSpireCityServiceDefinition> All =>
            Definitions;

        public static InfiniteSpireCityServiceDefinition Find(string serviceId)
        {
            if (string.IsNullOrEmpty(serviceId))
            {
                return null;
            }

            for (var index = 0; index < Definitions.Length; index++)
            {
                if (string.Equals(
                        Definitions[index].Id,
                        serviceId,
                        StringComparison.Ordinal))
                {
                    return Definitions[index];
                }
            }

            return null;
        }
    }

    public static class InfiniteSpireEconomy
    {
        public const float BaseDeathRetentionRate = 0.45f;
        public const float MaximumDeathRetentionRate = 0.60f;
        public const float PartialBankShare = 0.40f;
        public const int SpirecoilPerSpiremark = 25;
        public const int MaximumFavorPerRetirement = 120;
        public const int DailyFavorSoftCap = 180;
        public const float ExcessFavorConversionRate = 0.25f;

        public static int GetRoomSpirecoilReward(
            int floor,
            int personalBestFloor,
            float permanentCoilFindBonus)
        {
            return GetSpirecoilReward(
                InfiniteSpireRewardKind.Room,
                floor,
                personalBestFloor,
                permanentCoilFindBonus);
        }

        public static int GetMinibossSpirecoilReward(
            int floor,
            int personalBestFloor,
            float permanentCoilFindBonus)
        {
            return GetSpirecoilReward(
                InfiniteSpireRewardKind.Miniboss,
                floor,
                personalBestFloor,
                permanentCoilFindBonus);
        }

        public static int GetBossSpirecoilReward(
            int floor,
            int personalBestFloor,
            float permanentCoilFindBonus)
        {
            return GetSpirecoilReward(
                InfiniteSpireRewardKind.Boss,
                floor,
                personalBestFloor,
                permanentCoilFindBonus);
        }

        public static int GetTreasureSpirecoilReward(
            int floor,
            int personalBestFloor,
            float permanentCoilFindBonus)
        {
            return GetSpirecoilReward(
                InfiniteSpireRewardKind.Treasure,
                floor,
                personalBestFloor,
                permanentCoilFindBonus);
        }

        public static int GetTreasureSpirecoilReward(
            int generatedBaseSpirecoil,
            int floor,
            int personalBestFloor,
            float permanentCoilFindBonus)
        {
            return ApplySpirecoilRewardModifiers(
                generatedBaseSpirecoil,
                floor,
                personalBestFloor,
                permanentCoilFindBonus);
        }

        public static int GetSpirecoilReward(
            InfiniteSpireRewardKind kind,
            int floor,
            int personalBestFloor,
            float permanentCoilFindBonus)
        {
            var safeFloor = Clamp(
                floor,
                1,
                InfiniteSpireSaveNormalization.MaximumFloor);
            long baseReward;
            switch (kind)
            {
                case InfiniteSpireRewardKind.Miniboss:
                    baseReward = 24L + safeFloor * 5L;
                    break;
                case InfiniteSpireRewardKind.Boss:
                    baseReward = 60L + safeFloor * 9L;
                    break;
                case InfiniteSpireRewardKind.Treasure:
                    baseReward = 15L + safeFloor * 3L;
                    break;
                default:
                    baseReward = 6L + safeFloor * 2L;
                    break;
            }

            return ApplySpirecoilRewardModifiers(
                baseReward > InfiniteSpireSaveNormalization.MaximumResource
                    ? InfiniteSpireSaveNormalization.MaximumResource
                    : (int)baseReward,
                safeFloor,
                personalBestFloor,
                permanentCoilFindBonus);
        }

        public static int ApplySpirecoilRewardModifiers(
            int baseSpirecoil,
            int floor,
            int personalBestFloor,
            float permanentCoilFindBonus)
        {
            var safeBase = Clamp(
                baseSpirecoil,
                0,
                InfiniteSpireSaveNormalization.MaximumResource);
            var diminishing = GetLowFloorRewardMultiplier(
                floor,
                personalBestFloor);
            var findBonus = ClampFinite(
                permanentCoilFindBonus,
                0f,
                InfiniteSpireSaveNormalization.MaximumPermanentBonus);
            var reward = Math.Floor(safeBase * diminishing * (1f + findBonus));
            return reward > InfiniteSpireSaveNormalization.MaximumResource
                ? InfiniteSpireSaveNormalization.MaximumResource
                : Math.Max(0, (int)reward);
        }

        public static float GetLowFloorRewardMultiplier(
            int floor,
            int personalBestFloor)
        {
            var safeFloor = Math.Max(1, floor);
            var safeBest = Math.Max(0, personalBestFloor);
            if (safeBest < 10 || safeFloor >= safeBest)
            {
                return 1f;
            }

            var progress = safeFloor / (float)safeBest;
            if (progress >= 0.75f)
            {
                return 1f;
            }

            if (progress >= 0.50f)
            {
                return 0.75f;
            }

            if (progress >= 0.25f)
            {
                return 0.50f;
            }

            return 0.35f;
        }

        public static float GetDeathRetentionRate(float permanentBonus)
        {
            var bonus = ClampFinite(
                permanentBonus,
                0f,
                InfiniteSpireSaveNormalization.MaximumPermanentBonus);
            return Math.Min(
                MaximumDeathRetentionRate,
                BaseDeathRetentionRate + bonus);
        }

        public static int GetRetainedSpirecoilOnDeath(
            int carriedSpirecoil,
            float permanentBonus)
        {
            var carried = Clamp(
                carriedSpirecoil,
                0,
                InfiniteSpireSaveNormalization.MaximumResource);
            return (int)Math.Floor(
                carried * (double)GetDeathRetentionRate(permanentBonus) +
                0.000001d);
        }

        public static float GetBankRate(int floor)
        {
            return GetBankRatePercent(floor) / 100f;
        }

        public static InfiniteSpireBankResult CalculateFullRetirementBank(
            int carriedSpirecoil,
            int floor)
        {
            var carried = Clamp(
                carriedSpirecoil,
                0,
                InfiniteSpireSaveNormalization.MaximumResource);
            return CalculateBank(carried, carried, floor);
        }

        public static InfiniteSpireBankResult CalculatePartialBank(
            int carriedSpirecoil,
            int floor)
        {
            var carried = Clamp(
                carriedSpirecoil,
                0,
                InfiniteSpireSaveNormalization.MaximumResource);
            var extracted = (int)((long)carried * 40L / 100L);
            return CalculateBank(carried, extracted, floor);
        }

        public static int ConvertBankedSpirecoilToSpiremark(int bankedSpirecoil)
        {
            return Math.Max(0, bankedSpirecoil) / SpirecoilPerSpiremark;
        }

        public static int GetRetirementFavorProposal(
            int floor,
            int bankedSpirecoil)
        {
            var safeFloor = Math.Max(0, floor);
            var safeBanked = Math.Max(0, bankedSpirecoil);
            var proposed = (long)safeFloor * 2L + safeBanked / 25L;
            return (int)Math.Min(MaximumFavorPerRetirement, proposed);
        }

        public static long GetUtcDay(long utcTicks)
        {
            var safeTicks = Math.Max(0L, utcTicks);
            return safeTicks / TimeSpan.TicksPerDay;
        }

        public static InfiniteSpireFavorResult CalculateFavorAward(
            int requestedFavor,
            long favorLedgerUtcDay,
            int favorEarnedOnUtcDay,
            long nowUtcTicks)
        {
            var currentUtcDay = GetUtcDay(nowUtcTicks);
            var savedUtcDay = Math.Max(0L, favorLedgerUtcDay);
            var rolled = currentUtcDay > savedUtcDay;
            var ledgerUtcDay = rolled ? currentUtcDay : savedUtcDay;
            var priorEarned = rolled
                ? 0
                : Clamp(
                    favorEarnedOnUtcDay,
                    0,
                    InfiniteSpireSaveNormalization.MaximumResource);
            var eligible = Clamp(requestedFavor, 0, MaximumFavorPerRetirement);
            var fullRateAllowance = Math.Max(0, DailyFavorSoftCap - priorEarned);
            var fullRate = Math.Min(eligible, fullRateAllowance);
            var excess = eligible - fullRate;
            var convertedExcess = excess / 4;
            var awarded = fullRate + convertedExcess;
            var newLedgerAmount = SaturatingAdd(priorEarned, awarded);

            return new InfiniteSpireFavorResult(
                ledgerUtcDay,
                requestedFavor,
                eligible,
                fullRate,
                excess,
                awarded,
                newLedgerAmount,
                rolled);
        }

        public static string CreateMerchantStockId(
            int runSeed,
            int floor,
            string merchantId,
            int slotIndex)
        {
            var safeFloor = Math.Max(1, floor);
            var safeSlot = Math.Max(0, slotIndex);
            if (InfiniteSpireInstanceIds.TryParseRoomId(
                    merchantId,
                    out var room) &&
                room.RunSeed == runSeed &&
                room.FloorIndex == safeFloor)
            {
                return InfiniteSpireInstanceIds.FormatStockId(room, safeSlot);
            }

            var hash = InfiniteSpireHash.Compute(
                runSeed,
                safeFloor,
                safeSlot,
                InfiniteSpireHash.StableString(merchantId ?? string.Empty));
            return InfiniteSpireStableIds.StockPrefix +
                   safeFloor.ToString(CultureInfo.InvariantCulture) +
                   "." +
                   safeSlot.ToString(CultureInfo.InvariantCulture) +
                   "." +
                   hash.ToString("x8", CultureInfo.InvariantCulture);
        }

        public static string CreateMerchantStockId(
            int runSeed,
            int floor,
            int room,
            int slotIndex)
        {
            return InfiniteSpireInstanceIds.FormatStockId(
                new InfiniteSpireRoomInstanceKey(
                    runSeed,
                    Math.Max(1, floor),
                    Math.Max(0, room)),
                Math.Max(0, slotIndex));
        }

        public static string GetMerchantStockItemId(
            int runSeed,
            int floor,
            string merchantId,
            int slotIndex)
        {
            var hash = InfiniteSpireHash.Compute(
                runSeed,
                Math.Max(1, floor),
                Math.Max(0, slotIndex),
                InfiniteSpireHash.StableString(merchantId ?? string.Empty));
            var offers = ResolveMerchantOffers(merchantId);
            return offers.Count == 0
                ? string.Empty
                : offers[(int)(hash % (uint)offers.Count)].Id;
        }

        public static int GetMerchantPrice(
            int runSeed,
            int floor,
            string merchantId,
            string itemId,
            int slotIndex)
        {
            var offer = InfiniteSpireCatalog.GetMerchantOffer(itemId);
            var basePrice = offer?.BaseSpirecoilCost ?? 12;
            var safeFloor = Clamp(
                floor,
                1,
                InfiniteSpireSaveNormalization.MaximumFloor);
            var hash = InfiniteSpireHash.Compute(
                runSeed,
                safeFloor,
                Math.Max(0, slotIndex),
                InfiniteSpireHash.Mix(
                    InfiniteSpireHash.StableString(merchantId ?? string.Empty),
                    InfiniteSpireHash.StableString(itemId ?? string.Empty)));
            var floorPremium = Math.Min(60, (safeFloor - 1) / 3);
            var deterministicVariation = (int)(hash % 7u) - 3;
            return Clamp(
                basePrice + floorPremium + deterministicVariation,
                1,
                InfiniteSpireSaveNormalization.MaximumResource);
        }

        public static int GetMerchantPrice(
            int runSeed,
            int floor,
            int room,
            InfiniteSpireMerchantOfferDefinition offer,
            int slotIndex)
        {
            if (offer == null)
            {
                return 0;
            }

            var safeFloor = Clamp(
                floor,
                1,
                InfiniteSpireSaveNormalization.MaximumFloor);
            var safeRoom = Math.Max(0, room);
            var safeSlot = Math.Max(0, slotIndex);
            var priceSalt = InfiniteSpireHash.Mix(
                offer.Salt,
                unchecked((uint)safeSlot));
            var hash = InfiniteSpireHash.Compute(
                runSeed,
                safeFloor,
                safeRoom,
                priceSalt);
            var floorPremium = Math.Min(60, (safeFloor - 1) / 3);
            var deterministicVariation = (int)(hash % 7u) - 3;
            return Clamp(
                offer.BaseSpirecoilCost +
                floorPremium +
                deterministicVariation,
                1,
                InfiniteSpireSaveNormalization.MaximumResource);
        }

        public static List<InfiniteSpireMerchantStockSaveData> BuildMerchantStock(
            int runSeed,
            int floor,
            string merchantId,
            int stockCount)
        {
            var count = Clamp(
                stockCount,
                0,
                InfiniteSpireSaveNormalization.MaximumMerchantStock);
            var stock = new List<InfiniteSpireMerchantStockSaveData>(count);
            for (var slot = 0; slot < count; slot++)
            {
                var itemId = GetMerchantStockItemId(
                    runSeed,
                    floor,
                    merchantId,
                    slot);
                stock.Add(new InfiniteSpireMerchantStockSaveData
                {
                    stockId = CreateMerchantStockId(
                        runSeed,
                        floor,
                        merchantId,
                        slot),
                    itemId = itemId,
                    spirecoilPrice = GetMerchantPrice(
                        runSeed,
                        floor,
                        merchantId,
                        itemId,
                        slot)
                });
            }

            return stock;
        }

        public static List<InfiniteSpireMerchantStockSaveData> BuildMerchantStock(
            int runSeed,
            int floor,
            int room,
            InfiniteSpireMerchantStockSelection selection)
        {
            var count = Clamp(
                selection.Count,
                0,
                InfiniteSpireSaveNormalization.MaximumMerchantStock);
            var stock = new List<InfiniteSpireMerchantStockSaveData>(count);
            for (var slot = 0; slot < count; slot++)
            {
                var offer = selection.GetOffer(slot);
                if (offer == null)
                {
                    continue;
                }

                stock.Add(new InfiniteSpireMerchantStockSaveData
                {
                    stockId = CreateMerchantStockId(
                        runSeed,
                        floor,
                        room,
                        slot),
                    itemId = offer.Id,
                    spirecoilPrice = GetMerchantPrice(
                        runSeed,
                        floor,
                        room,
                        offer,
                        slot)
                });
            }

            return stock;
        }

        public static InfiniteSpireCityServicePurchaseResult QuoteCityService(
            string serviceId,
            int bankedSpiremark,
            float permanentCoilFindBonus,
            float permanentDeathRetainBonus,
            bool meridianChartUnlocked,
            int fieldRations)
        {
            var definition = InfiniteSpireCityServices.Find(serviceId);
            var balance = Math.Max(0, bankedSpiremark);
            var coilBonus = ClampFinite(
                permanentCoilFindBonus,
                0f,
                InfiniteSpireSaveNormalization.MaximumPermanentBonus);
            var deathBonus = ClampFinite(
                permanentDeathRetainBonus,
                0f,
                InfiniteSpireSaveNormalization.MaximumPermanentBonus);
            var rationCount = Clamp(
                fieldRations,
                0,
                InfiniteSpireSaveNormalization.MaximumFieldRations);
            if (definition == null)
            {
                return new InfiniteSpireCityServicePurchaseResult(
                    InfiniteSpireTransitionStatus.ServiceUnavailable,
                    serviceId,
                    0,
                    balance,
                    coilBonus,
                    deathBonus,
                    meridianChartUnlocked,
                    rationCount);
            }

            var completedPurchases = GetCompletedServicePurchases(
                definition.Kind,
                coilBonus,
                deathBonus,
                meridianChartUnlocked,
                rationCount);
            if (completedPurchases >= definition.MaximumPurchases)
            {
                return new InfiniteSpireCityServicePurchaseResult(
                    InfiniteSpireTransitionStatus.ServiceAtCap,
                    serviceId,
                    0,
                    balance,
                    coilBonus,
                    deathBonus,
                    meridianChartUnlocked,
                    rationCount);
            }

            var cost = definition.GetSpiremarkCost(completedPurchases);
            if (balance < cost)
            {
                return new InfiniteSpireCityServicePurchaseResult(
                    InfiniteSpireTransitionStatus.InsufficientSpiremark,
                    serviceId,
                    cost,
                    balance,
                    coilBonus,
                    deathBonus,
                    meridianChartUnlocked,
                    rationCount);
            }

            switch (definition.Kind)
            {
                case InfiniteSpireCityServiceKind.CoilAttunement:
                    coilBonus = Math.Min(
                        definition.MaximumPermanentBonus,
                        coilBonus + definition.BonusPerPurchase);
                    break;
                case InfiniteSpireCityServiceKind.SealTemper:
                    deathBonus = Math.Min(
                        definition.MaximumPermanentBonus,
                        deathBonus + definition.BonusPerPurchase);
                    break;
                case InfiniteSpireCityServiceKind.MeridianChart:
                    meridianChartUnlocked = true;
                    break;
                case InfiniteSpireCityServiceKind.FieldRation:
                    rationCount = Math.Min(
                        InfiniteSpireSaveNormalization.MaximumFieldRations,
                        rationCount + 1);
                    break;
            }

            return new InfiniteSpireCityServicePurchaseResult(
                InfiniteSpireTransitionStatus.Applied,
                serviceId,
                cost,
                balance - cost,
                coilBonus,
                deathBonus,
                meridianChartUnlocked,
                rationCount);
        }

        private static InfiniteSpireBankResult CalculateBank(
            int carried,
            int extracted,
            int floor)
        {
            var safeExtracted = Clamp(extracted, 0, carried);
            var ratePercent = GetBankRatePercent(floor);
            var rate = ratePercent / 100f;
            var banked = (int)((long)safeExtracted * ratePercent / 100L);
            return new InfiniteSpireBankResult(
                carried,
                safeExtracted,
                banked,
                carried - safeExtracted,
                ConvertBankedSpirecoilToSpiremark(banked),
                rate);
        }

        private static int GetBankRatePercent(int floor)
        {
            if (floor <= 12)
            {
                return 100;
            }

            if (floor <= 24)
            {
                return 85;
            }

            if (floor <= 36)
            {
                return 70;
            }

            return 55;
        }

        private static int GetCompletedServicePurchases(
            InfiniteSpireCityServiceKind kind,
            float coilBonus,
            float deathBonus,
            bool meridianChartUnlocked,
            int fieldRations)
        {
            switch (kind)
            {
                case InfiniteSpireCityServiceKind.CoilAttunement:
                    return BonusRank(coilBonus);
                case InfiniteSpireCityServiceKind.SealTemper:
                    return BonusRank(deathBonus);
                case InfiniteSpireCityServiceKind.MeridianChart:
                    return meridianChartUnlocked ? 1 : 0;
                case InfiniteSpireCityServiceKind.FieldRation:
                    return Math.Max(0, fieldRations);
                default:
                    return 0;
            }
        }

        private static int BonusRank(float bonus)
        {
            var safe = ClampFinite(
                bonus,
                0f,
                InfiniteSpireSaveNormalization.MaximumPermanentBonus);
            return Clamp(
                (int)Math.Floor((safe + 0.0001f) / 0.03f),
                0,
                5);
        }

        private static IReadOnlyList<InfiniteSpireMerchantOfferDefinition>
            ResolveMerchantOffers(string merchantId)
        {
            var template = InfiniteSpireCatalog.GetTemplate(merchantId);
            if (template?.MerchantKit?.Offers != null)
            {
                return template.MerchantKit.Offers;
            }

            var kit = InfiniteSpireCatalog.GetMerchantKit(merchantId);
            return kit?.Offers ?? InfiniteSpireCatalog.MerchantOffers;
        }

        private static int SaturatingAdd(int left, int right)
        {
            var total = (long)Math.Max(0, left) + Math.Max(0, right);
            return total > InfiniteSpireSaveNormalization.MaximumResource
                ? InfiniteSpireSaveNormalization.MaximumResource
                : (int)total;
        }

        private static float ClampFinite(float value, float minimum, float maximum)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return minimum;
            }

            return value < minimum
                ? minimum
                : value > maximum
                    ? maximum
                    : value;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum
                ? minimum
                : value > maximum
                    ? maximum
                    : value;
        }
    }

    public static class InfiniteSpireRunTransitions
    {
        public static bool CanStart(InfiniteSpireSaveData data)
        {
            return data != null && data.unlocked && !data.runActive;
        }

        public static InfiniteSpireTransitionResult Start(
            InfiniteSpireSaveData data,
            int runSeed)
        {
            return Start(
                data,
                runSeed,
                1,
                0,
                InfiniteSpireStableIds.CreateRoomId(runSeed, 1, 0));
        }

        public static InfiniteSpireTransitionResult Start(
            InfiniteSpireSaveData data,
            int runSeed,
            int floor,
            int room,
            string startingRoomId)
        {
            if (data == null)
            {
                return Failure(InfiniteSpireTransitionStatus.MissingSaveData);
            }

            if (!data.unlocked)
            {
                return Failure(InfiniteSpireTransitionStatus.Locked);
            }

            if (data.runActive)
            {
                return Failure(InfiniteSpireTransitionStatus.RunAlreadyActive);
            }

            if (floor < 1 ||
                floor > InfiniteSpireSaveNormalization.MaximumFloor ||
                room < 0 ||
                room > InfiniteSpireSaveNormalization.MaximumRoom ||
                !InfiniteSpireStableIds.IsValidRoomId(startingRoomId))
            {
                return Failure(InfiniteSpireTransitionStatus.InvalidLocation);
            }

            if (InfiniteSpireStableIds.TryParseFloor(
                    startingRoomId,
                    out var parsedFloor) &&
                parsedFloor != floor)
            {
                return Failure(InfiniteSpireTransitionStatus.InvalidLocation);
            }

            if (InfiniteSpireInstanceIds.TryParseRoomId(
                    startingRoomId,
                    out var startingRoomKey) &&
                (startingRoomKey.RunSeed != runSeed ||
                 startingRoomKey.RoomIndex != room))
            {
                return Failure(InfiniteSpireTransitionStatus.InvalidLocation);
            }

            EnsureCollections(data);
            data.runActive = true;
            data.recoveryPending = false;
            data.runSeed = runSeed;
            data.floor = floor;
            data.room = room;
            data.currentFloorId = InfiniteSpireStableIds.CreateFloorId(floor);
            data.currentRoomId = startingRoomId;
            data.recoveryRoomId = startingRoomId;
            data.currentTemplateId = string.Empty;
            data.currentBossId = string.Empty;
            data.currentMeridianVisitId = string.Empty;
            data.spirecoil = 0;
            data.recentTemplateIds.Clear();
            data.recentBossIds.Clear();
            data.meridianVisits.Clear();
            data.roomLocalHistory.Clear();
            InfiniteSpireSaveNormalization.ClearActiveRoomState(data);

            return new InfiniteSpireTransitionResult(
                InfiniteSpireTransitionStatus.Applied,
                true,
                destinationId: startingRoomId);
        }

        public static bool CanRetire(InfiniteSpireSaveData data)
        {
            return data != null &&
                   data.runActive &&
                   InfiniteSpireCadenceRules.IsLoomrestMeridianFloor(
                       data.floor) &&
                   (string.Equals(
                        data.currentTemplateId,
                        InfiniteSpireIds.RestLoomrestMeridian,
                        StringComparison.Ordinal) ||
                    InfiniteSpireStableIds.IsLoomrestMeridianRoomId(
                        data.currentRoomId));
        }

        public static InfiniteSpireTransitionResult RegisterLoomrestMeridianVisit(
            InfiniteSpireSaveData data,
            string visitId = null)
        {
            if (data == null)
            {
                return Failure(InfiniteSpireTransitionStatus.MissingSaveData);
            }

            if (!data.runActive)
            {
                return Failure(InfiniteSpireTransitionStatus.RunInactive);
            }

            if (!CanRetire(data))
            {
                return Failure(
                    InfiniteSpireTransitionStatus.NotAtLoomrestMeridian);
            }

            EnsureCollections(data);
            var resolvedVisitId = string.IsNullOrWhiteSpace(visitId)
                ? InfiniteSpireStableIds.CreateMeridianVisitId(
                    data.runSeed,
                    data.floor,
                    data.room)
                : visitId.Trim();
            if (!InfiniteSpireStableIds.IsValidMeridianVisitId(resolvedVisitId))
            {
                return Failure(InfiniteSpireTransitionStatus.InvalidId);
            }

            var existing = FindMeridianVisit(data, resolvedVisitId);
            if (existing != null &&
                (existing.floor != data.floor || existing.room != data.room))
            {
                return Failure(InfiniteSpireTransitionStatus.InvalidId);
            }

            data.currentMeridianVisitId = resolvedVisitId;
            data.recoveryRoomId = data.currentRoomId;
            data.recoveryPending = false;
            if (existing == null)
            {
                data.meridianVisits.Add(new InfiniteSpireMeridianVisitSaveData
                {
                    visitId = resolvedVisitId,
                    floor = data.floor,
                    room = data.room
                });
                TrimOldest(
                    data.meridianVisits,
                    InfiniteSpireSaveNormalization.MaximumMeridianVisits);
            }

            return new InfiniteSpireTransitionResult(
                InfiniteSpireTransitionStatus.Applied,
                true,
                data.spirecoil,
                data.spirecoil,
                destinationId: data.currentRoomId);
        }

        public static InfiniteSpireTransitionResult ApplyDeath(
            InfiniteSpireSaveData data)
        {
            if (data == null)
            {
                return Failure(InfiniteSpireTransitionStatus.MissingSaveData);
            }

            if (!data.runActive)
            {
                return Failure(InfiniteSpireTransitionStatus.RunInactive);
            }

            var before = Math.Max(0, data.spirecoil);
            var retained = InfiniteSpireEconomy.GetRetainedSpirecoilOnDeath(
                before,
                data.permanentDeathRetainBonus);
            data.spirecoil = retained;
            data.personalBestFloor = Math.Max(
                data.personalBestFloor,
                Math.Max(0, data.floor));
            data.recoveryPending = true;
            InfiniteSpireSaveNormalization.ClearActiveRoomState(data);

            return new InfiniteSpireTransitionResult(
                InfiniteSpireTransitionStatus.Applied,
                true,
                before,
                retained,
                before - retained,
                destinationId: data.recoveryRoomId);
        }

        public static InfiniteSpireTransitionResult ApplyFullRetirement(
            InfiniteSpireSaveData data,
            long nowUtcTicks)
        {
            if (data == null)
            {
                return Failure(InfiniteSpireTransitionStatus.MissingSaveData);
            }

            var preview = InfiniteSpireEconomy.CalculateFullRetirementBank(
                data.spirecoil,
                data.floor);
            var proposedFavor = InfiniteSpireEconomy.GetRetirementFavorProposal(
                data.floor,
                preview.BankedSpirecoil);
            return ApplyFullRetirement(data, proposedFavor, nowUtcTicks);
        }

        public static InfiniteSpireTransitionResult ApplyFullRetirement(
            InfiniteSpireSaveData data,
            int requestedFavor,
            long nowUtcTicks)
        {
            if (data == null)
            {
                return Failure(InfiniteSpireTransitionStatus.MissingSaveData);
            }

            if (!data.runActive)
            {
                return Failure(InfiniteSpireTransitionStatus.RunInactive);
            }

            if (!CanRetire(data))
            {
                return Failure(
                    InfiniteSpireTransitionStatus.NotAtLoomrestMeridian);
            }

            var floor = data.floor;
            var before = Math.Max(0, data.spirecoil);
            var bank = InfiniteSpireEconomy.CalculateFullRetirementBank(
                before,
                floor);
            var favor = InfiniteSpireEconomy.CalculateFavorAward(
                requestedFavor,
                data.favorLedgerUtcDay,
                data.favorEarnedOnUtcDay,
                nowUtcTicks);
            data.bankedSpiremark = SaturatingAdd(
                data.bankedSpiremark,
                bank.SpiremarkAwarded);
            data.personalBestFloor = Math.Max(data.personalBestFloor, floor);
            data.favorLedgerUtcDay = favor.UtcDay;
            data.favorEarnedOnUtcDay = favor.FavorEarnedOnUtcDay;
            InfiniteSpireSaveNormalization.ClearRunStateToCityGate(data);

            return new InfiniteSpireTransitionResult(
                InfiniteSpireTransitionStatus.Applied,
                true,
                before,
                0,
                before - bank.BankedSpirecoil,
                bank.BankedSpirecoil,
                bank.SpiremarkAwarded,
                favor.FavorAwarded,
                InfiniteSpireStableIds.CityGateId);
        }

        public static InfiniteSpireTransitionResult ApplyPartialBank(
            InfiniteSpireSaveData data)
        {
            if (data == null)
            {
                return Failure(InfiniteSpireTransitionStatus.MissingSaveData);
            }

            if (!data.runActive)
            {
                return Failure(InfiniteSpireTransitionStatus.RunInactive);
            }

            if (!CanRetire(data))
            {
                return Failure(
                    InfiniteSpireTransitionStatus.NotAtLoomrestMeridian);
            }

            EnsureCollections(data);
            var visitId = ResolveCurrentMeridianVisitId(data);
            var visit = FindMeridianVisit(data, visitId);
            if (visit != null && visit.partialBankUsed)
            {
                return Failure(
                    InfiniteSpireTransitionStatus.PartialBankAlreadyUsed);
            }

            var before = Math.Max(0, data.spirecoil);
            var bank = InfiniteSpireEconomy.CalculatePartialBank(
                before,
                data.floor);
            if (bank.ExtractedSpirecoil <= 0)
            {
                return Failure(
                    InfiniteSpireTransitionStatus.InsufficientSpirecoil);
            }

            if (visit == null)
            {
                visit = new InfiniteSpireMeridianVisitSaveData
                {
                    visitId = visitId,
                    floor = data.floor,
                    room = data.room
                };
                data.meridianVisits.Add(visit);
                TrimOldest(
                    data.meridianVisits,
                    InfiniteSpireSaveNormalization.MaximumMeridianVisits);
            }

            visit.partialBankUsed = true;
            data.currentMeridianVisitId = visitId;
            data.spirecoil = bank.RemainingSpirecoil;
            data.bankedSpiremark = SaturatingAdd(
                data.bankedSpiremark,
                bank.SpiremarkAwarded);

            return new InfiniteSpireTransitionResult(
                InfiniteSpireTransitionStatus.Applied,
                true,
                before,
                bank.RemainingSpirecoil,
                bank.ExtractedSpirecoil - bank.BankedSpirecoil,
                bank.BankedSpirecoil,
                bank.SpiremarkAwarded,
                destinationId: data.currentRoomId);
        }

        public static InfiniteSpireResumeDecision DecideResume(
            InfiniteSpireSaveData data,
            InfiniteSpireIdValidationHooks hooks = null)
        {
            if (data == null || !data.unlocked)
            {
                return new InfiniteSpireResumeDecision(
                    InfiniteSpireResumeKind.LockedAtCityGate,
                    InfiniteSpireStableIds.CityGateId,
                    false,
                    false);
            }

            if (!data.runActive)
            {
                return new InfiniteSpireResumeDecision(
                    InfiniteSpireResumeKind.InactiveAtCityGate,
                    InfiniteSpireStableIds.CityGateId,
                    false,
                    false);
            }

            if (!InfiniteSpireSaveNormalization.IsActiveRunStructurallyValid(
                    data,
                    hooks,
                    out _))
            {
                return new InfiniteSpireResumeDecision(
                    InfiniteSpireResumeKind.RecoverCorruptRunAtCityGate,
                    InfiniteSpireStableIds.CityGateId,
                    true,
                    true);
            }

            if (data.recoveryPending)
            {
                return new InfiniteSpireResumeDecision(
                    InfiniteSpireResumeKind.RecoverAtSavedRoom,
                    data.recoveryRoomId,
                    false,
                    false);
            }

            return new InfiniteSpireResumeDecision(
                InfiniteSpireResumeKind.ResumeCurrentRoom,
                data.currentRoomId,
                false,
                false);
        }

        public static InfiniteSpireTransitionResult AcknowledgeRecovery(
            InfiniteSpireSaveData data)
        {
            if (data == null)
            {
                return Failure(InfiniteSpireTransitionStatus.MissingSaveData);
            }

            if (!data.runActive)
            {
                return Failure(InfiniteSpireTransitionStatus.RunInactive);
            }

            if (!data.recoveryPending ||
                !InfiniteSpireStableIds.IsValidRoomId(data.recoveryRoomId))
            {
                return Failure(InfiniteSpireTransitionStatus.InvalidLocation);
            }

            data.currentRoomId = data.recoveryRoomId;
            if (InfiniteSpireInstanceIds.TryParseRoomId(
                    data.recoveryRoomId,
                    out var recoveryRoom))
            {
                data.floor = recoveryRoom.FloorIndex;
                data.room = recoveryRoom.RoomIndex;
                data.currentFloorId =
                    InfiniteSpireStableIds.CreateFloorId(data.floor);
            }

            data.currentTemplateId = string.Empty;
            data.currentBossId = string.Empty;
            data.currentMeridianVisitId = string.Empty;
            data.recoveryPending = false;
            return new InfiniteSpireTransitionResult(
                InfiniteSpireTransitionStatus.Applied,
                true,
                data.spirecoil,
                data.spirecoil,
                destinationId: data.recoveryRoomId);
        }

        public static bool CanOpenChest(
            InfiniteSpireSaveData data,
            string chestId)
        {
            if (data == null ||
                !data.runActive ||
                !InfiniteSpireStableIds.IsValidChestId(chestId) ||
                Contains(data.openedChestIds, chestId))
            {
                return false;
            }

            return !HistoryContainsChest(data, chestId);
        }

        public static InfiniteSpireTransitionResult RecordChestOpened(
            InfiniteSpireSaveData data,
            string chestId)
        {
            if (data == null)
            {
                return Failure(InfiniteSpireTransitionStatus.MissingSaveData);
            }

            if (!data.runActive)
            {
                return Failure(InfiniteSpireTransitionStatus.RunInactive);
            }

            if (!InfiniteSpireStableIds.IsValidChestId(chestId))
            {
                return Failure(InfiniteSpireTransitionStatus.InvalidId);
            }

            if (!CanOpenChest(data, chestId))
            {
                return Failure(InfiniteSpireTransitionStatus.AlreadyClaimed);
            }

            EnsureCollections(data);
            data.openedChestIds.Add(chestId);
            TrimOldest(
                data.openedChestIds,
                InfiniteSpireSaveNormalization.MaximumChestsPerFloor);
            var history = GetOrCreateCurrentRoomHistory(data);
            if (!history.openedChestIds.Contains(chestId))
            {
                history.openedChestIds.Add(chestId);
                TrimOldest(
                    history.openedChestIds,
                    InfiniteSpireSaveNormalization.MaximumChestsPerFloor);
            }

            return new InfiniteSpireTransitionResult(
                InfiniteSpireTransitionStatus.Applied,
                true,
                data.spirecoil,
                data.spirecoil,
                destinationId: data.currentRoomId);
        }

        public static bool CanPurchaseMerchantStock(
            InfiniteSpireSaveData data,
            string stockId)
        {
            if (data == null ||
                !data.runActive ||
                !InfiniteSpireStableIds.IsValidStockId(stockId) ||
                Contains(data.purchasedMerchantStockIds, stockId) ||
                HistoryContainsStock(data, stockId))
            {
                return false;
            }

            var stock = FindStock(data.activeMerchantStock, stockId);
            return stock != null &&
                   !stock.purchased &&
                   Math.Max(0, data.spirecoil) >= Math.Max(1, stock.spirecoilPrice);
        }

        public static InfiniteSpireTransitionResult PurchaseMerchantStock(
            InfiniteSpireSaveData data,
            string stockId)
        {
            if (data == null)
            {
                return Failure(InfiniteSpireTransitionStatus.MissingSaveData);
            }

            if (!data.runActive)
            {
                return Failure(InfiniteSpireTransitionStatus.RunInactive);
            }

            if (!InfiniteSpireStableIds.IsValidStockId(stockId))
            {
                return Failure(InfiniteSpireTransitionStatus.InvalidId);
            }

            EnsureCollections(data);
            var stock = FindStock(data.activeMerchantStock, stockId);
            if (stock == null)
            {
                return Failure(InfiniteSpireTransitionStatus.StockNotFound);
            }

            if (stock.purchased ||
                data.purchasedMerchantStockIds.Contains(stockId) ||
                HistoryContainsStock(data, stockId))
            {
                return Failure(InfiniteSpireTransitionStatus.AlreadyClaimed);
            }

            var price = Math.Max(1, stock.spirecoilPrice);
            var before = Math.Max(0, data.spirecoil);
            if (before < price)
            {
                return Failure(
                    InfiniteSpireTransitionStatus.InsufficientSpirecoil);
            }

            stock.purchased = true;
            data.spirecoil = before - price;
            data.purchasedMerchantStockIds.Add(stockId);
            TrimOldest(
                data.purchasedMerchantStockIds,
                InfiniteSpireSaveNormalization.MaximumMerchantStock);
            var history = GetOrCreateCurrentRoomHistory(data);
            if (!history.purchasedStockIds.Contains(stockId))
            {
                history.purchasedStockIds.Add(stockId);
                TrimOldest(
                    history.purchasedStockIds,
                    InfiniteSpireSaveNormalization.MaximumMerchantStock);
            }

            return new InfiniteSpireTransitionResult(
                InfiniteSpireTransitionStatus.Applied,
                true,
                before,
                data.spirecoil,
                price,
                destinationId: data.currentRoomId);
        }

        public static InfiniteSpireTransitionResult ApplyCityServicePurchase(
            InfiniteSpireSaveData data,
            string serviceId)
        {
            if (data == null)
            {
                return Failure(InfiniteSpireTransitionStatus.MissingSaveData);
            }

            if (data.runActive)
            {
                return Failure(InfiniteSpireTransitionStatus.InvalidLocation);
            }

            if (!data.unlocked)
            {
                return Failure(InfiniteSpireTransitionStatus.Locked);
            }

            var quote = InfiniteSpireEconomy.QuoteCityService(
                serviceId,
                data.bankedSpiremark,
                data.permanentCoilFindBonus,
                data.permanentDeathRetainBonus,
                data.meridianChartUnlocked,
                data.fieldRations);
            if (!quote.CanPurchase)
            {
                return Failure(quote.Status);
            }

            data.bankedSpiremark = quote.RemainingSpiremark;
            data.permanentCoilFindBonus = quote.CoilFindBonus;
            data.permanentDeathRetainBonus = quote.DeathRetainBonus;
            data.meridianChartUnlocked = quote.MeridianChartUnlocked;
            data.fieldRations = quote.FieldRations;
            return new InfiniteSpireTransitionResult(
                InfiniteSpireTransitionStatus.Applied,
                quote.RequestCriticalSave,
                destinationId: InfiniteSpireStableIds.CityGateId);
        }

        private static InfiniteSpireTransitionResult Failure(
            InfiniteSpireTransitionStatus status)
        {
            return new InfiniteSpireTransitionResult(status);
        }

        private static string ResolveCurrentMeridianVisitId(
            InfiniteSpireSaveData data)
        {
            if (InfiniteSpireStableIds.IsValidMeridianVisitId(
                    data.currentMeridianVisitId))
            {
                var current = FindMeridianVisit(
                    data,
                    data.currentMeridianVisitId);
                if (current != null &&
                    current.floor == data.floor &&
                    current.room == data.room)
                {
                    return data.currentMeridianVisitId;
                }
            }

            return InfiniteSpireStableIds.CreateMeridianVisitId(
                data.runSeed,
                data.floor,
                data.room);
        }

        private static InfiniteSpireMeridianVisitSaveData FindMeridianVisit(
            InfiniteSpireSaveData data,
            string visitId)
        {
            if (data?.meridianVisits == null)
            {
                return null;
            }

            for (var index = 0; index < data.meridianVisits.Count; index++)
            {
                var visit = data.meridianVisits[index];
                if (visit != null &&
                    string.Equals(
                        visit.visitId,
                        visitId,
                        StringComparison.Ordinal))
                {
                    return visit;
                }
            }

            return null;
        }

        private static InfiniteSpireMerchantStockSaveData FindStock(
            List<InfiniteSpireMerchantStockSaveData> stock,
            string stockId)
        {
            if (stock == null)
            {
                return null;
            }

            for (var index = 0; index < stock.Count; index++)
            {
                var entry = stock[index];
                if (entry != null &&
                    string.Equals(
                        entry.stockId,
                        stockId,
                        StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static InfiniteSpireRoomHistorySaveData
            GetOrCreateCurrentRoomHistory(InfiniteSpireSaveData data)
        {
            EnsureCollections(data);
            for (var index = 0; index < data.roomLocalHistory.Count; index++)
            {
                var history = data.roomLocalHistory[index];
                if (history != null &&
                    history.floor == data.floor &&
                    string.Equals(
                        history.roomId,
                        data.currentRoomId,
                        StringComparison.Ordinal))
                {
                    history.openedChestIds ??= new List<string>();
                    history.purchasedStockIds ??= new List<string>();
                    return history;
                }
            }

            var created = new InfiniteSpireRoomHistorySaveData
            {
                floor = data.floor,
                roomId = data.currentRoomId
            };
            data.roomLocalHistory.Add(created);
            TrimOldest(
                data.roomLocalHistory,
                InfiniteSpireSaveNormalization.MaximumRoomHistory);
            return created;
        }

        private static bool HistoryContainsChest(
            InfiniteSpireSaveData data,
            string chestId)
        {
            if (data?.roomLocalHistory == null)
            {
                return false;
            }

            for (var index = 0; index < data.roomLocalHistory.Count; index++)
            {
                var history = data.roomLocalHistory[index];
                if (history != null &&
                    history.floor == data.floor &&
                    Contains(history.openedChestIds, chestId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HistoryContainsStock(
            InfiniteSpireSaveData data,
            string stockId)
        {
            if (data?.roomLocalHistory == null)
            {
                return false;
            }

            for (var index = 0; index < data.roomLocalHistory.Count; index++)
            {
                var history = data.roomLocalHistory[index];
                if (history != null &&
                    history.floor == data.floor &&
                    Contains(history.purchasedStockIds, stockId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(List<string> ids, string id)
        {
            return ids != null && ids.Contains(id);
        }

        private static void EnsureCollections(InfiniteSpireSaveData data)
        {
            data.recentTemplateIds ??= new List<string>();
            data.recentBossIds ??= new List<string>();
            data.meridianVisits ??= new List<InfiniteSpireMeridianVisitSaveData>();
            data.activeRoomPuzzle ??= new InfiniteSpirePuzzleStateSaveData();
            data.activeRoomEnemies ??= new List<InfiniteSpireEnemyStateSaveData>();
            data.openedChestIds ??= new List<string>();
            data.activeMerchantStock ??= new List<InfiniteSpireMerchantStockSaveData>();
            data.purchasedMerchantStockIds ??= new List<string>();
            data.roomLocalHistory ??= new List<InfiniteSpireRoomHistorySaveData>();
        }

        private static int SaturatingAdd(int left, int right)
        {
            var total = (long)Math.Max(0, left) + Math.Max(0, right);
            return total > InfiniteSpireSaveNormalization.MaximumResource
                ? InfiniteSpireSaveNormalization.MaximumResource
                : (int)total;
        }

        private static void TrimOldest<T>(List<T> values, int maximumCount)
        {
            if (values.Count > maximumCount)
            {
                values.RemoveRange(0, values.Count - maximumCount);
            }
        }
    }
}
