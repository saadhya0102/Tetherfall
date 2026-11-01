using System;
using System.Collections.Generic;

namespace Tetherfall
{
    [Serializable]
    public sealed class InfiniteSpirePuzzleStateSaveData
    {
        public string puzzleId = string.Empty;
        public List<int> values = new List<int>();
        public bool solved;
    }

    [Serializable]
    public sealed class InfiniteSpireEnemyStateSaveData
    {
        public string instanceId = string.Empty;
        public string enemyId = string.Empty;
        public int currentHealth;
        public bool defeated;
    }

    [Serializable]
    public sealed class InfiniteSpireMerchantStockSaveData
    {
        public string stockId = string.Empty;
        public string itemId = string.Empty;
        public int spirecoilPrice;
        public bool purchased;
    }

    [Serializable]
    public sealed class InfiniteSpireRoomHistorySaveData
    {
        public int floor;
        public string roomId = string.Empty;
        public List<string> openedChestIds = new List<string>();
        public List<string> purchasedStockIds = new List<string>();
    }

    [Serializable]
    public sealed class InfiniteSpireMeridianVisitSaveData
    {
        public string visitId = string.Empty;
        public int floor;
        public int room;
        public bool partialBankUsed;
    }

    [Serializable]
    public sealed class InfiniteSpireSaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public bool unlocked;
        public bool runActive;
        public bool recoveryPending;
        public int runSeed;
        public int floor;
        public int room;
        public string currentFloorId = string.Empty;
        public string currentRoomId = InfiniteSpireStableIds.CityGateId;
        public string recoveryRoomId = InfiniteSpireStableIds.CityGateId;
        public string currentTemplateId = string.Empty;
        public string currentBossId = string.Empty;
        public string currentMeridianVisitId = string.Empty;

        public int spirecoil;
        public int bankedSpiremark;
        public int personalBestFloor;

        // UTC day number (UTC ticks / TimeSpan.TicksPerDay), not a local date.
        public long favorLedgerUtcDay;
        public int favorEarnedOnUtcDay;

        public List<string> recentTemplateIds = new List<string>();
        public List<string> recentBossIds = new List<string>();
        public List<InfiniteSpireMeridianVisitSaveData> meridianVisits =
            new List<InfiniteSpireMeridianVisitSaveData>();

        public int activeRoomWaveIndex = -1;
        public InfiniteSpirePuzzleStateSaveData activeRoomPuzzle =
            new InfiniteSpirePuzzleStateSaveData();
        public List<InfiniteSpireEnemyStateSaveData> activeRoomEnemies =
            new List<InfiniteSpireEnemyStateSaveData>();
        public List<string> openedChestIds = new List<string>();
        public string activeMerchantId = string.Empty;
        public List<InfiniteSpireMerchantStockSaveData> activeMerchantStock =
            new List<InfiniteSpireMerchantStockSaveData>();
        public List<string> purchasedMerchantStockIds = new List<string>();
        public List<InfiniteSpireRoomHistorySaveData> roomLocalHistory =
            new List<InfiniteSpireRoomHistorySaveData>();

        public float permanentCoilFindBonus;
        public float permanentDeathRetainBonus;
        public bool meridianChartUnlocked;
        public int fieldRations;
    }

    public sealed partial class TetherfallSaveData
    {
        public InfiniteSpireSaveData infiniteSpire = new InfiniteSpireSaveData();
    }

    public delegate bool InfiniteSpireTryParseFloor(string stableId, out int floor);

    /// <summary>
    /// Optional catalog-facing hooks for schema-25 integration. A supplied validator is
    /// authoritative for its ID kind. Missing hooks use tolerant semantic-prefix checks.
    /// </summary>
    public sealed class InfiniteSpireIdValidationHooks
    {
        public Func<string, bool> floorIdValidator;
        public Func<string, bool> roomIdValidator;
        public Func<string, bool> recoveryIdValidator;
        public Func<string, bool> templateIdValidator;
        public Func<string, bool> bossIdValidator;
        public Func<string, bool> puzzleIdValidator;
        public Func<string, bool> enemyIdValidator;
        public Func<string, bool> enemyInstanceIdValidator;
        public Func<string, bool> chestIdValidator;
        public Func<string, bool> merchantIdValidator;
        public Func<string, bool> stockIdValidator;
        public Func<string, bool> itemIdValidator;
        public Func<string, bool> meridianVisitIdValidator;
        public InfiniteSpireTryParseFloor tryParseFloor;
    }

    public sealed class InfiniteSpireNormalizationReport
    {
        public bool inputWasNull;
        public bool recoveredCorruptRun;
        public bool clampedPermanentBonus;
        public int removedInvalidIds;
        public int removedInvalidRecords;
        public int trimmedRecords;

        public bool Changed =>
            recoveredCorruptRun ||
            clampedPermanentBonus ||
            removedInvalidIds > 0 ||
            removedInvalidRecords > 0 ||
            trimmedRecords > 0;
    }

    public static class InfiniteSpireStableIds
    {
        public const string CityGateId = "infinite_spire.city_gate";
        public const string FloorPrefix = "spire.floor.";
        public const string RoomPrefix = "spire.room.";
        public const string LoomrestMeridianPrefix = "infinite_spire.meridian.loomrest.";
        public const string TemplatePrefix = "spire.template.";
        public const string BossPrefix = "spire.template.boss.";
        public const string PuzzlePrefix = "spire.puzzle.";
        public const string EnemyPrefix = "spire.enemy.";
        public const string EnemyInstancePrefix = "spire.enemy_instance.";
        public const string ChestPrefix = "spire.chest.";
        public const string MerchantPrefix = "spire.template.merchant.";
        public const string StockPrefix = "spire.stock.";
        public const string ItemPrefix = "spire.template.merchant.offer.";
        public const string MeridianVisitPrefix = "spire.visit.meridian.";

        public static string CreateFloorId(int floor)
        {
            return FloorPrefix + Math.Max(1, floor);
        }

        public static string CreateRoomId(int floor, int room)
        {
            return CreateRoomId(0, floor, room);
        }

        public static string CreateRoomId(int runSeed, int floor, int room)
        {
            return InfiniteSpireInstanceIds.FormatRoomId(
                new InfiniteSpireRoomInstanceKey(
                    runSeed,
                    Math.Max(1, floor),
                    Math.Max(0, room)));
        }

        public static string CreateLoomrestMeridianRoomId(int floor, int room)
        {
            return CreateLoomrestMeridianRoomId(0, floor, room);
        }

        public static string CreateLoomrestMeridianRoomId(
            int runSeed,
            int floor,
            int room)
        {
            var seedToken = runSeed < 0
                ? "n" + (-(long)runSeed)
                : "p" + runSeed;
            return LoomrestMeridianPrefix +
                   seedToken +
                   "." +
                   Math.Max(1, floor) +
                   "." +
                   Math.Max(0, room);
        }

        public static string CreateMeridianVisitId(
            int runSeed,
            int floor,
            int room)
        {
            var seedToken = runSeed < 0
                ? "n" + (-(long)runSeed)
                : "p" + runSeed;
            return MeridianVisitPrefix +
                   seedToken +
                   "." +
                   Math.Max(1, floor) +
                   "." +
                   Math.Max(0, room);
        }

        public static bool IsLoomrestMeridianRoomId(string stableId)
        {
            return HasPrefix(stableId, LoomrestMeridianPrefix) ||
                   HasPrefix(stableId, "spire.meridian.loomrest.");
        }

        public static bool IsValidFloorId(string stableId)
        {
            return IsStableId(stableId) &&
                   (HasPrefix(stableId, FloorPrefix) ||
                    HasPrefix(stableId, "infinite_spire.floor."));
        }

        public static bool IsValidRoomId(string stableId)
        {
            if (InfiniteSpireInstanceIds.TryParseRoomId(
                    stableId,
                    out _))
            {
                return true;
            }

            return IsStableId(stableId) &&
                   (HasPrefix(stableId, LoomrestMeridianPrefix) ||
                    HasPrefix(stableId, "infinite_spire.room.") ||
                    HasPrefix(stableId, "spire.meridian."));
        }

        public static bool IsValidRecoveryId(string stableId)
        {
            return string.Equals(stableId, CityGateId, StringComparison.Ordinal) ||
                   IsValidRoomId(stableId);
        }

        public static bool IsValidTemplateId(string stableId)
        {
            return IsStableId(stableId) &&
                   (HasPrefix(stableId, TemplatePrefix) ||
                    HasPrefix(stableId, "infinite_spire.template."));
        }

        public static bool IsValidBossId(string stableId)
        {
            if (TryParseInstanceKind(
                    stableId,
                    InfiniteSpireInstanceEntityKind.Boss))
            {
                return true;
            }

            return IsStableId(stableId) &&
                   (HasPrefix(stableId, BossPrefix) ||
                    HasPrefix(stableId, "infinite_spire.boss.") ||
                    HasPrefix(stableId, "boss."));
        }

        public static bool IsValidPuzzleId(string stableId)
        {
            if (TryParseInstanceKind(
                    stableId,
                    InfiniteSpireInstanceEntityKind.Puzzle))
            {
                return true;
            }

            return IsStableId(stableId) &&
                   (HasPrefix(stableId, PuzzlePrefix) ||
                    HasPrefix(stableId, "infinite_spire.puzzle."));
        }

        public static bool IsValidEnemyId(string stableId)
        {
            return IsStableId(stableId) &&
                   (HasPrefix(stableId, EnemyPrefix) ||
                    HasPrefix(stableId, "spire.enemy.") ||
                    HasPrefix(stableId, "enemy."));
        }

        public static bool IsValidEnemyInstanceId(string stableId)
        {
            return IsStableId(stableId) &&
                   (HasPrefix(stableId, EnemyInstancePrefix) ||
                    HasPrefix(stableId, EnemyPrefix) ||
                    HasPrefix(stableId, RoomPrefix) ||
                    HasPrefix(stableId, "infinite_spire.enemy_instance.") ||
                    HasPrefix(stableId, "enemy."));
        }

        public static bool IsValidChestId(string stableId)
        {
            if (TryParseInstanceKind(
                    stableId,
                    InfiniteSpireInstanceEntityKind.Chest))
            {
                return true;
            }

            return IsStableId(stableId) &&
                   (HasPrefix(stableId, ChestPrefix) ||
                    HasPrefix(stableId, "infinite_spire.chest."));
        }

        public static bool IsValidMerchantId(string stableId)
        {
            return IsStableId(stableId) &&
                   (HasPrefix(stableId, MerchantPrefix) ||
                    HasPrefix(stableId, "infinite_spire.merchant.") ||
                    HasPrefix(stableId, "merchant."));
        }

        public static bool IsValidStockId(string stableId)
        {
            if (TryParseInstanceKind(
                    stableId,
                    InfiniteSpireInstanceEntityKind.Stock))
            {
                return true;
            }

            return IsStableId(stableId) &&
                   (HasPrefix(stableId, StockPrefix) ||
                    HasPrefix(stableId, "infinite_spire.stock."));
        }

        public static bool IsValidItemId(string stableId)
        {
            return IsStableId(stableId) &&
                   (HasPrefix(stableId, ItemPrefix) ||
                    HasPrefix(stableId, "infinite_spire.item.") ||
                    HasPrefix(stableId, "item.") ||
                    HasPrefix(stableId, "equipment."));
        }

        public static bool IsValidMeridianVisitId(string stableId)
        {
            return IsStableId(stableId) &&
                   (HasPrefix(stableId, MeridianVisitPrefix) ||
                    HasPrefix(stableId, "infinite_spire.visit.meridian."));
        }

        public static bool TryParseFloor(string stableId, out int floor)
        {
            floor = 0;
            if (!IsStableId(stableId))
            {
                return false;
            }

            if (InfiniteSpireInstanceIds.TryParse(
                    stableId,
                    out var instanceId))
            {
                floor = instanceId.Room.FloorIndex;
                return true;
            }

            if (InfiniteSpireInstanceIds.TryParseRoomId(
                    stableId,
                    out var room))
            {
                floor = room.FloorIndex;
                return true;
            }

            var tokens = stableId.Split('.');
            for (var index = 0; index < tokens.Length; index++)
            {
                if (int.TryParse(tokens[index], out var parsed) && parsed > 0)
                {
                    floor = parsed;
                    return true;
                }
            }

            return false;
        }

        public static bool IsStableId(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId) ||
                stableId.Length > InfiniteSpireSaveNormalization.MaximumStableIdLength ||
                !string.Equals(stableId, stableId.Trim(), StringComparison.Ordinal))
            {
                return false;
            }

            for (var index = 0; index < stableId.Length; index++)
            {
                var character = stableId[index];
                if (!char.IsLetterOrDigit(character) &&
                    character != '.' &&
                    character != '_' &&
                    character != '-' &&
                    character != ':' &&
                    character != '@')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseInstanceKind(
            string stableId,
            InfiniteSpireInstanceEntityKind expectedKind)
        {
            return InfiniteSpireInstanceIds.TryParse(
                       stableId,
                       out var instanceId) &&
                   instanceId.Kind == expectedKind;
        }

        private static bool HasPrefix(string value, string prefix)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class InfiniteSpireSaveNormalization
    {
        public const int MaximumFloor = 1000000;
        public const int MaximumRoom = 64;
        public const int MaximumResource = 1000000000;
        public const int MaximumStableIdLength = 128;
        public const int MaximumRecentTemplates =
            InfiniteSpireGenerationState.TemplateHistoryCapacity;
        public const int MaximumRecentBosses =
            InfiniteSpireGenerationState.BossHistoryCapacity;
        public const int MaximumMeridianVisits = 64;
        public const int MaximumRoomHistory = 8;
        public const int MaximumActiveEnemies =
            InfiniteSpireLoadedWindowBudgets.MaximumActiveEnemies;
        public const int MaximumChestsPerFloor = 8;
        public const int MaximumMerchantStock = 16;
        public const int MaximumPuzzleValues = 8;
        public const int MaximumFieldRations = 9;
        public const float MaximumPermanentBonus = 0.15f;

        public static InfiniteSpireNormalizationReport NormalizeInPlace(
            InfiniteSpireSaveData data,
            InfiniteSpireIdValidationHooks hooks = null)
        {
            var report = new InfiniteSpireNormalizationReport();
            if (data == null)
            {
                report.inputWasNull = true;
                return report;
            }

            data.version = InfiniteSpireSaveData.CurrentVersion;
            data.bankedSpiremark = Clamp(data.bankedSpiremark, 0, MaximumResource);
            data.personalBestFloor = Clamp(data.personalBestFloor, 0, MaximumFloor);
            data.favorLedgerUtcDay = Clamp(
                data.favorLedgerUtcDay,
                0L,
                DateTime.MaxValue.Ticks / TimeSpan.TicksPerDay);
            data.favorEarnedOnUtcDay = Clamp(
                data.favorEarnedOnUtcDay,
                0,
                MaximumResource);
            data.fieldRations = Clamp(data.fieldRations, 0, MaximumFieldRations);

            data.permanentCoilFindBonus = NormalizeBonus(
                data.permanentCoilFindBonus,
                report);
            data.permanentDeathRetainBonus = NormalizeBonus(
                data.permanentDeathRetainBonus,
                report);

            data.recentTemplateIds ??= new List<string>();
            data.recentBossIds ??= new List<string>();
            data.meridianVisits ??= new List<InfiniteSpireMeridianVisitSaveData>();
            data.activeRoomPuzzle ??= new InfiniteSpirePuzzleStateSaveData();
            data.activeRoomEnemies ??= new List<InfiniteSpireEnemyStateSaveData>();
            data.openedChestIds ??= new List<string>();
            data.activeMerchantStock ??= new List<InfiniteSpireMerchantStockSaveData>();
            data.purchasedMerchantStockIds ??= new List<string>();
            data.roomLocalHistory ??= new List<InfiniteSpireRoomHistorySaveData>();

            NormalizeIdList(
                data.recentTemplateIds,
                MaximumRecentTemplates,
                value => Validate(value, hooks?.templateIdValidator,
                    InfiniteSpireStableIds.IsValidTemplateId),
                report,
                true);
            NormalizeIdList(
                data.recentBossIds,
                MaximumRecentBosses,
                value => Validate(value, hooks?.bossIdValidator,
                    InfiniteSpireStableIds.IsValidBossId),
                report,
                true);

            var activeRunWasCorrupt = data.runActive &&
                                      !IsActiveRunStructurallyValid(
                                          data,
                                          hooks,
                                          out _);

            data.floor = Clamp(data.floor, 0, MaximumFloor);
            data.room = Clamp(data.room, 0, MaximumRoom);
            data.spirecoil = Clamp(data.spirecoil, 0, MaximumResource);
            data.activeRoomWaveIndex = Clamp(data.activeRoomWaveIndex, -1, 1024);

            data.currentFloorId = NormalizeText(data.currentFloorId);
            data.currentRoomId = NormalizeText(data.currentRoomId);
            data.recoveryRoomId = NormalizeText(data.recoveryRoomId);
            data.currentTemplateId = NormalizeOptionalId(
                data.currentTemplateId,
                value => Validate(value, hooks?.templateIdValidator,
                    InfiniteSpireStableIds.IsValidTemplateId),
                report);
            data.currentBossId = NormalizeOptionalId(
                data.currentBossId,
                value => Validate(value, hooks?.bossIdValidator,
                    InfiniteSpireStableIds.IsValidBossId),
                report);
            data.currentMeridianVisitId = NormalizeOptionalId(
                data.currentMeridianVisitId,
                value => Validate(value, hooks?.meridianVisitIdValidator,
                    InfiniteSpireStableIds.IsValidMeridianVisitId),
                report);

            NormalizePuzzle(data.activeRoomPuzzle, hooks, report);
            NormalizeEnemies(data.activeRoomEnemies, hooks, report);
            NormalizeIdList(
                data.openedChestIds,
                MaximumChestsPerFloor,
                value => Validate(value, hooks?.chestIdValidator,
                    InfiniteSpireStableIds.IsValidChestId),
                report);
            data.activeMerchantId = NormalizeOptionalId(
                data.activeMerchantId,
                value => Validate(value, hooks?.merchantIdValidator,
                    InfiniteSpireStableIds.IsValidMerchantId),
                report);
            NormalizeMerchantStock(data.activeMerchantStock, hooks, report);
            NormalizeIdList(
                data.purchasedMerchantStockIds,
                MaximumMerchantStock,
                value => Validate(value, hooks?.stockIdValidator,
                    InfiniteSpireStableIds.IsValidStockId),
                report);

            for (var index = 0; index < data.activeMerchantStock.Count; index++)
            {
                var stock = data.activeMerchantStock[index];
                if (stock.purchased &&
                    !data.purchasedMerchantStockIds.Contains(stock.stockId))
                {
                    data.purchasedMerchantStockIds.Add(stock.stockId);
                }
            }
            TrimOldest(
                data.purchasedMerchantStockIds,
                MaximumMerchantStock,
                report);

            NormalizeMeridianVisits(data, hooks, report);
            NormalizeRoomHistory(data, hooks, report);

            if (activeRunWasCorrupt)
            {
                report.recoveredCorruptRun = true;
                ClearRunStateToCityGate(data);
            }
            else if (!data.runActive)
            {
                ClearRunStateToCityGate(data);
            }
            else
            {
                data.unlocked = true;
                if (!string.Equals(
                        data.currentTemplateId,
                        InfiniteSpireIds.RestLoomrestMeridian,
                        StringComparison.Ordinal) &&
                    !InfiniteSpireStableIds.IsLoomrestMeridianRoomId(
                        data.currentRoomId))
                {
                    data.currentMeridianVisitId = string.Empty;
                }
            }

            return report;
        }

        public static bool IsActiveRunStructurallyValid(
            InfiniteSpireSaveData data,
            InfiniteSpireIdValidationHooks hooks,
            out string reason)
        {
            reason = string.Empty;
            if (data == null)
            {
                reason = "save data is missing";
                return false;
            }

            if (!data.runActive)
            {
                reason = "the run is inactive";
                return false;
            }

            if (!data.unlocked)
            {
                reason = "the Spire is locked";
                return false;
            }

            if (data.floor < 1 || data.floor > MaximumFloor)
            {
                reason = "the floor is outside the supported range";
                return false;
            }

            if (data.room < 0 || data.room > MaximumRoom)
            {
                reason = "the room index is outside the supported range";
                return false;
            }

            var floorId = NormalizeText(data.currentFloorId);
            if (!Validate(
                    floorId,
                    hooks?.floorIdValidator,
                    InfiniteSpireStableIds.IsValidFloorId))
            {
                reason = "the current floor ID is invalid";
                return false;
            }

            var roomId = NormalizeText(data.currentRoomId);
            if (!Validate(
                    roomId,
                    hooks?.roomIdValidator,
                    InfiniteSpireStableIds.IsValidRoomId))
            {
                reason = "the current room ID is invalid";
                return false;
            }

            var recoveryId = NormalizeText(data.recoveryRoomId);
            if (string.Equals(
                    recoveryId,
                    InfiniteSpireStableIds.CityGateId,
                    StringComparison.Ordinal) ||
                !Validate(
                    recoveryId,
                    hooks?.recoveryIdValidator,
                    InfiniteSpireStableIds.IsValidRecoveryId))
            {
                reason = "the active run recovery ID is invalid";
                return false;
            }

            if (TryParseFloor(floorId, hooks, out var parsedFloor) &&
                parsedFloor != data.floor)
            {
                reason = "the floor ID does not match the current floor";
                return false;
            }

            if (TryParseFloor(roomId, hooks, out parsedFloor) &&
                parsedFloor != data.floor)
            {
                reason = "the room ID does not match the current floor";
                return false;
            }

            if (hooks?.roomIdValidator == null &&
                InfiniteSpireInstanceIds.TryParseRoomId(
                    roomId,
                    out var currentRoomKey) &&
                (currentRoomKey.RunSeed != data.runSeed ||
                 currentRoomKey.RoomIndex != data.room))
            {
                reason = "the room ID does not match the current run instance";
                return false;
            }

            if (TryParseFloor(recoveryId, hooks, out parsedFloor) &&
                (parsedFloor < 1 || parsedFloor > data.floor))
            {
                reason = "the recovery ID points outside the completed floor window";
                return false;
            }

            if (hooks?.recoveryIdValidator == null &&
                InfiniteSpireInstanceIds.TryParseRoomId(
                    recoveryId,
                    out var recoveryRoomKey) &&
                recoveryRoomKey.RunSeed != data.runSeed)
            {
                reason = "the recovery ID does not match the current run seed";
                return false;
            }

            return true;
        }

        internal static void ClearRunStateToCityGate(InfiniteSpireSaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.runActive = false;
            data.recoveryPending = false;
            data.runSeed = 0;
            data.floor = 0;
            data.room = 0;
            data.currentFloorId = string.Empty;
            data.currentRoomId = InfiniteSpireStableIds.CityGateId;
            data.recoveryRoomId = InfiniteSpireStableIds.CityGateId;
            data.currentTemplateId = string.Empty;
            data.currentBossId = string.Empty;
            data.currentMeridianVisitId = string.Empty;
            data.spirecoil = 0;
            data.recentTemplateIds ??= new List<string>();
            data.recentTemplateIds.Clear();
            data.recentBossIds ??= new List<string>();
            data.recentBossIds.Clear();
            data.meridianVisits ??= new List<InfiniteSpireMeridianVisitSaveData>();
            data.meridianVisits.Clear();
            data.roomLocalHistory ??= new List<InfiniteSpireRoomHistorySaveData>();
            data.roomLocalHistory.Clear();
            ClearActiveRoomState(data);
        }

        internal static void ClearActiveRoomState(InfiniteSpireSaveData data)
        {
            data.activeRoomWaveIndex = -1;
            data.activeRoomPuzzle ??= new InfiniteSpirePuzzleStateSaveData();
            data.activeRoomPuzzle.puzzleId = string.Empty;
            data.activeRoomPuzzle.values ??= new List<int>();
            data.activeRoomPuzzle.values.Clear();
            data.activeRoomPuzzle.solved = false;
            data.activeRoomEnemies ??= new List<InfiniteSpireEnemyStateSaveData>();
            data.activeRoomEnemies.Clear();
            data.openedChestIds ??= new List<string>();
            data.openedChestIds.Clear();
            data.activeMerchantId = string.Empty;
            data.activeMerchantStock ??= new List<InfiniteSpireMerchantStockSaveData>();
            data.activeMerchantStock.Clear();
            data.purchasedMerchantStockIds ??= new List<string>();
            data.purchasedMerchantStockIds.Clear();
        }

        private static void NormalizePuzzle(
            InfiniteSpirePuzzleStateSaveData puzzle,
            InfiniteSpireIdValidationHooks hooks,
            InfiniteSpireNormalizationReport report)
        {
            puzzle.values ??= new List<int>();
            puzzle.puzzleId = NormalizeOptionalId(
                puzzle.puzzleId,
                value => Validate(value, hooks?.puzzleIdValidator,
                    InfiniteSpireStableIds.IsValidPuzzleId),
                report);
            if (string.IsNullOrEmpty(puzzle.puzzleId))
            {
                puzzle.values.Clear();
                puzzle.solved = false;
                return;
            }

            for (var index = 0; index < puzzle.values.Count; index++)
            {
                puzzle.values[index] = Clamp(
                    puzzle.values[index],
                    -MaximumResource,
                    MaximumResource);
            }

            TrimOldest(puzzle.values, MaximumPuzzleValues, report);
        }

        private static void NormalizeEnemies(
            List<InfiniteSpireEnemyStateSaveData> enemies,
            InfiniteSpireIdValidationHooks hooks,
            InfiniteSpireNormalizationReport report)
        {
            var instanceIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = enemies.Count - 1; index >= 0; index--)
            {
                var enemy = enemies[index];
                if (enemy == null)
                {
                    enemies.RemoveAt(index);
                    report.removedInvalidRecords++;
                    continue;
                }

                enemy.instanceId = NormalizeText(enemy.instanceId);
                enemy.enemyId = NormalizeText(enemy.enemyId);
                if (!Validate(
                        enemy.instanceId,
                        hooks?.enemyInstanceIdValidator,
                        InfiniteSpireStableIds.IsValidEnemyInstanceId) ||
                    !Validate(
                        enemy.enemyId,
                        hooks?.enemyIdValidator,
                        InfiniteSpireStableIds.IsValidEnemyId) ||
                    !instanceIds.Add(enemy.instanceId))
                {
                    enemies.RemoveAt(index);
                    report.removedInvalidRecords++;
                    continue;
                }

                enemy.currentHealth = Clamp(
                    enemy.currentHealth,
                    0,
                    MaximumResource);
                if (enemy.defeated)
                {
                    enemy.currentHealth = 0;
                }
            }

            TrimOldest(enemies, MaximumActiveEnemies, report);
        }

        private static void NormalizeMerchantStock(
            List<InfiniteSpireMerchantStockSaveData> stock,
            InfiniteSpireIdValidationHooks hooks,
            InfiniteSpireNormalizationReport report)
        {
            var byId =
                new Dictionary<string, InfiniteSpireMerchantStockSaveData>(
                    StringComparer.Ordinal);
            for (var index = 0; index < stock.Count; index++)
            {
                var entry = stock[index];
                if (entry == null)
                {
                    stock.RemoveAt(index);
                    index--;
                    report.removedInvalidRecords++;
                    continue;
                }

                entry.stockId = NormalizeText(entry.stockId);
                entry.itemId = NormalizeText(entry.itemId);
                if (!Validate(
                        entry.stockId,
                        hooks?.stockIdValidator,
                        InfiniteSpireStableIds.IsValidStockId) ||
                    !Validate(
                        entry.itemId,
                        hooks?.itemIdValidator,
                        InfiniteSpireStableIds.IsValidItemId))
                {
                    stock.RemoveAt(index);
                    index--;
                    report.removedInvalidRecords++;
                    continue;
                }

                entry.spirecoilPrice = Clamp(
                    entry.spirecoilPrice,
                    1,
                    MaximumResource);
                if (byId.TryGetValue(entry.stockId, out var existing))
                {
                    existing.purchased |= entry.purchased;
                    stock.RemoveAt(index);
                    index--;
                    report.removedInvalidRecords++;
                    continue;
                }

                byId.Add(entry.stockId, entry);
            }

            TrimOldest(stock, MaximumMerchantStock, report);
        }

        private static void NormalizeMeridianVisits(
            InfiniteSpireSaveData data,
            InfiniteSpireIdValidationHooks hooks,
            InfiniteSpireNormalizationReport report)
        {
            var byId = new Dictionary<string, InfiniteSpireMeridianVisitSaveData>(
                StringComparer.Ordinal);
            for (var index = 0; index < data.meridianVisits.Count; index++)
            {
                var visit = data.meridianVisits[index];
                if (visit == null)
                {
                    data.meridianVisits.RemoveAt(index--);
                    report.removedInvalidRecords++;
                    continue;
                }

                visit.visitId = NormalizeText(visit.visitId);
                var floorIsValid = visit.floor >= 1 &&
                                   visit.floor <= MaximumFloor &&
                                   (!data.runActive || visit.floor <= data.floor);
                var roomIsValid = visit.room >= 0 && visit.room <= MaximumRoom;
                if (!floorIsValid ||
                    !roomIsValid ||
                    !Validate(
                        visit.visitId,
                        hooks?.meridianVisitIdValidator,
                        InfiniteSpireStableIds.IsValidMeridianVisitId))
                {
                    data.meridianVisits.RemoveAt(index--);
                    report.removedInvalidRecords++;
                    continue;
                }

                if (byId.TryGetValue(visit.visitId, out var existing))
                {
                    existing.partialBankUsed |= visit.partialBankUsed;
                    data.meridianVisits.RemoveAt(index--);
                    report.removedInvalidRecords++;
                    continue;
                }

                byId.Add(visit.visitId, visit);
            }

            TrimOldest(data.meridianVisits, MaximumMeridianVisits, report);
        }

        private static void NormalizeRoomHistory(
            InfiniteSpireSaveData data,
            InfiniteSpireIdValidationHooks hooks,
            InfiniteSpireNormalizationReport report)
        {
            var byRoom =
                new Dictionary<string, InfiniteSpireRoomHistorySaveData>(
                    StringComparer.Ordinal);
            for (var index = 0; index < data.roomLocalHistory.Count; index++)
            {
                var history = data.roomLocalHistory[index];
                if (history == null)
                {
                    data.roomLocalHistory.RemoveAt(index);
                    index--;
                    report.removedInvalidRecords++;
                    continue;
                }

                history.openedChestIds ??= new List<string>();
                history.purchasedStockIds ??= new List<string>();
                history.roomId = NormalizeText(history.roomId);
                var validCurrentFloor = data.runActive &&
                                        history.floor == data.floor;
                if (!validCurrentFloor ||
                    !Validate(
                        history.roomId,
                        hooks?.roomIdValidator,
                        InfiniteSpireStableIds.IsValidRoomId))
                {
                    data.roomLocalHistory.RemoveAt(index);
                    index--;
                    report.removedInvalidRecords++;
                    continue;
                }

                if (TryParseFloor(history.roomId, hooks, out var parsedFloor) &&
                    parsedFloor != history.floor)
                {
                    data.roomLocalHistory.RemoveAt(index);
                    index--;
                    report.removedInvalidRecords++;
                    continue;
                }

                if (hooks?.roomIdValidator == null &&
                    InfiniteSpireInstanceIds.TryParseRoomId(
                        history.roomId,
                        out var historyRoomKey) &&
                    (historyRoomKey.RunSeed != data.runSeed ||
                     historyRoomKey.RoomIndex > MaximumRoom))
                {
                    data.roomLocalHistory.RemoveAt(index);
                    index--;
                    report.removedInvalidRecords++;
                    continue;
                }

                NormalizeIdList(
                    history.openedChestIds,
                    MaximumChestsPerFloor,
                    value => Validate(value, hooks?.chestIdValidator,
                        InfiniteSpireStableIds.IsValidChestId),
                    report);
                NormalizeIdList(
                    history.purchasedStockIds,
                    MaximumMerchantStock,
                    value => Validate(value, hooks?.stockIdValidator,
                        InfiniteSpireStableIds.IsValidStockId),
                    report);

                if (byRoom.TryGetValue(history.roomId, out var existing))
                {
                    UnionIds(
                        existing.openedChestIds,
                        history.openedChestIds,
                        MaximumChestsPerFloor,
                        report);
                    UnionIds(
                        existing.purchasedStockIds,
                        history.purchasedStockIds,
                        MaximumMerchantStock,
                        report);
                    data.roomLocalHistory.RemoveAt(index);
                    index--;
                    report.removedInvalidRecords++;
                    continue;
                }

                byRoom.Add(history.roomId, history);
            }

            TrimOldest(data.roomLocalHistory, MaximumRoomHistory, report);
        }

        private static void UnionIds(
            List<string> target,
            List<string> source,
            int maximumCount,
            InfiniteSpireNormalizationReport report)
        {
            var distinct = new HashSet<string>(target, StringComparer.Ordinal);
            for (var index = 0; index < source.Count; index++)
            {
                if (distinct.Add(source[index]))
                {
                    target.Add(source[index]);
                }
            }

            TrimOldest(target, maximumCount, report);
        }

        private static void NormalizeIdList(
            List<string> ids,
            int maximumCount,
            Func<string, bool> validator,
            InfiniteSpireNormalizationReport report,
            bool newestFirst = false)
        {
            var distinct = new HashSet<string>(StringComparer.Ordinal);
            if (newestFirst)
            {
                for (var index = 0; index < ids.Count; index++)
                {
                    var normalized = NormalizeText(ids[index]);
                    if (!validator(normalized) || !distinct.Add(normalized))
                    {
                        ids.RemoveAt(index);
                        index--;
                        report.removedInvalidIds++;
                        continue;
                    }

                    ids[index] = normalized;
                }

                TrimTrailing(ids, maximumCount, report);
                return;
            }

            for (var index = ids.Count - 1; index >= 0; index--)
            {
                var normalized = NormalizeText(ids[index]);
                if (!validator(normalized) || !distinct.Add(normalized))
                {
                    ids.RemoveAt(index);
                    report.removedInvalidIds++;
                    continue;
                }

                ids[index] = normalized;
            }

            TrimOldest(ids, maximumCount, report);
        }

        private static string NormalizeOptionalId(
            string value,
            Func<string, bool> validator,
            InfiniteSpireNormalizationReport report)
        {
            var normalized = NormalizeText(value);
            if (string.IsNullOrEmpty(normalized))
            {
                return string.Empty;
            }

            if (validator(normalized))
            {
                return normalized;
            }

            report.removedInvalidIds++;
            return string.Empty;
        }

        private static string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        private static bool Validate(
            string value,
            Func<string, bool> hook,
            Func<string, bool> fallback)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Length > MaximumStableIdLength ||
                !InfiniteSpireStableIds.IsStableId(value))
            {
                return false;
            }

            if (hook == null)
            {
                return fallback(value);
            }

            try
            {
                return hook(value);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryParseFloor(
            string stableId,
            InfiniteSpireIdValidationHooks hooks,
            out int floor)
        {
            floor = 0;
            if (hooks?.tryParseFloor != null)
            {
                try
                {
                    return hooks.tryParseFloor(stableId, out floor) &&
                           floor >= 1 &&
                           floor <= MaximumFloor;
                }
                catch (Exception)
                {
                    floor = 0;
                    return false;
                }
            }

            return InfiniteSpireStableIds.TryParseFloor(stableId, out floor);
        }

        private static float NormalizeBonus(
            float value,
            InfiniteSpireNormalizationReport report)
        {
            var normalized = IsFinite(value)
                ? Clamp(value, 0f, MaximumPermanentBonus)
                : 0f;
            if (!IsFinite(value) || Math.Abs(normalized - value) > 0.00001f)
            {
                report.clampedPermanentBonus = true;
            }

            return normalized;
        }

        private static void TrimOldest<T>(
            List<T> values,
            int maximumCount,
            InfiniteSpireNormalizationReport report)
        {
            if (values.Count <= maximumCount)
            {
                return;
            }

            var removeCount = values.Count - maximumCount;
            values.RemoveRange(0, removeCount);
            report.trimmedRecords += removeCount;
        }

        private static void TrimTrailing<T>(
            List<T> values,
            int maximumCount,
            InfiniteSpireNormalizationReport report)
        {
            if (values.Count <= maximumCount)
            {
                return;
            }

            var removeCount = values.Count - maximumCount;
            values.RemoveRange(maximumCount, removeCount);
            report.trimmedRecords += removeCount;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum
                ? minimum
                : value > maximum
                    ? maximum
                    : value;
        }

        private static long Clamp(long value, long minimum, long maximum)
        {
            return value < minimum
                ? minimum
                : value > maximum
                    ? maximum
                    : value;
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            return value < minimum
                ? minimum
                : value > maximum
                    ? maximum
                    : value;
        }
    }
}
