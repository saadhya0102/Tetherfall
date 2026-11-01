using System;

namespace Tetherfall
{
    public static class InfiniteSpireHash
    {
        public const uint OffsetBasis = 2166136261u;
        public const uint Prime = 16777619u;

        /// <summary>
        /// FNV-1a over four explicit little-endian 32-bit inputs.
        /// </summary>
        public static uint Compute(
            int runSeed,
            int floorIndex,
            int roomIndex,
            uint templateSalt)
        {
            var hash = OffsetBasis;
            AppendUInt32(ref hash, unchecked((uint)runSeed));
            AppendUInt32(ref hash, unchecked((uint)floorIndex));
            AppendUInt32(ref hash, unchecked((uint)roomIndex));
            AppendUInt32(ref hash, templateSalt);
            return hash;
        }

        public static uint Rehash(uint value, int attempt)
        {
            var hash = value;
            AppendUInt32(ref hash, unchecked((uint)attempt));
            return hash;
        }

        public static uint Mix(uint first, uint second)
        {
            var hash = OffsetBasis;
            AppendUInt32(ref hash, first);
            AppendUInt32(ref hash, second);
            return hash;
        }

        public static uint StableString(string value)
        {
            var hash = OffsetBasis;
            if (value == null)
            {
                return hash;
            }

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                AppendByte(ref hash, (byte)character);
                AppendByte(ref hash, (byte)(character >> 8));
            }

            return hash;
        }

        private static void AppendUInt32(ref uint hash, uint value)
        {
            AppendByte(ref hash, (byte)value);
            AppendByte(ref hash, (byte)(value >> 8));
            AppendByte(ref hash, (byte)(value >> 16));
            AppendByte(ref hash, (byte)(value >> 24));
        }

        private static void AppendByte(ref uint hash, byte value)
        {
            hash ^= value;
            hash *= Prime;
        }
    }

    [Flags]
    public enum InfiniteSpireCadence
    {
        None = 0,
        Miniboss = 1 << 0,
        Boss = 1 << 1,
        Merchant = 1 << 2,
        LoomrestMeridian = 1 << 3
    }

    public static class InfiniteSpireCadenceRules
    {
        public const int MinibossInterval = 5;
        public const int BossInterval = 10;
        public const int MerchantInterval = 7;
        public const int MeridianInterval = 12;
        public const int FirstMeridianFloor = 6;

        public static bool IsBossFloor(int floorIndex)
        {
            return floorIndex > 1 && floorIndex % BossInterval == 0;
        }

        public static bool IsMinibossFloor(int floorIndex)
        {
            return floorIndex > 1 &&
                   floorIndex % MinibossInterval == 0 &&
                   !IsBossFloor(floorIndex);
        }

        public static bool IsMerchantEligibleFloor(int floorIndex)
        {
            return floorIndex >= MerchantInterval &&
                   floorIndex % MerchantInterval == 0;
        }

        public static bool IsLoomrestMeridianFloor(int floorIndex)
        {
            return floorIndex >= FirstMeridianFloor &&
                   (floorIndex - FirstMeridianFloor) % MeridianInterval == 0;
        }
    }

    public readonly struct InfiniteSpireRoomInstanceKey :
        IEquatable<InfiniteSpireRoomInstanceKey>
    {
        public InfiniteSpireRoomInstanceKey(int runSeed, int floorIndex, int roomIndex)
        {
            RunSeed = runSeed;
            FloorIndex = floorIndex;
            RoomIndex = roomIndex;
        }

        public int RunSeed { get; }
        public int FloorIndex { get; }
        public int RoomIndex { get; }
        public bool IsValid => FloorIndex >= 1 && RoomIndex >= 0;

        public bool Equals(InfiniteSpireRoomInstanceKey other)
        {
            return RunSeed == other.RunSeed &&
                   FloorIndex == other.FloorIndex &&
                   RoomIndex == other.RoomIndex;
        }

        public override bool Equals(object value)
        {
            return value is InfiniteSpireRoomInstanceKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = RunSeed;
                hash = (hash * 397) ^ FloorIndex;
                hash = (hash * 397) ^ RoomIndex;
                return hash;
            }
        }

        public static bool operator ==(
            InfiniteSpireRoomInstanceKey left,
            InfiniteSpireRoomInstanceKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            InfiniteSpireRoomInstanceKey left,
            InfiniteSpireRoomInstanceKey right)
        {
            return !left.Equals(right);
        }
    }

    public enum InfiniteSpireInstanceEntityKind
    {
        Room = 0,
        Wave = 1,
        Mechanism = 2,
        Puzzle = 3,
        Chest = 4,
        Boss = 5,
        Stock = 6
    }

    public readonly struct InfiniteSpireInstanceId : IEquatable<InfiniteSpireInstanceId>
    {
        public InfiniteSpireInstanceId(
            InfiniteSpireRoomInstanceKey room,
            InfiniteSpireInstanceEntityKind kind,
            int slot = -1)
        {
            Room = room;
            Kind = kind;
            Slot = slot;
        }

        public InfiniteSpireRoomInstanceKey Room { get; }
        public InfiniteSpireInstanceEntityKind Kind { get; }
        public int Slot { get; }

        public bool IsValid
        {
            get
            {
                if (!Room.IsValid)
                {
                    return false;
                }

                return Kind == InfiniteSpireInstanceEntityKind.Wave ||
                       Kind == InfiniteSpireInstanceEntityKind.Mechanism ||
                       Kind == InfiniteSpireInstanceEntityKind.Stock
                    ? Slot >= 0
                    : Slot == -1;
            }
        }

        public bool Equals(InfiniteSpireInstanceId other)
        {
            return Room.Equals(other.Room) && Kind == other.Kind && Slot == other.Slot;
        }

        public override bool Equals(object value)
        {
            return value is InfiniteSpireInstanceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Room.GetHashCode();
                hash = (hash * 397) ^ (int)Kind;
                hash = (hash * 397) ^ Slot;
                return hash;
            }
        }

        public static bool operator ==(
            InfiniteSpireInstanceId left,
            InfiniteSpireInstanceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            InfiniteSpireInstanceId left,
            InfiniteSpireInstanceId right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Stable run-local ID formatting. Caller-provided char buffers avoid hot-path allocations.
    /// </summary>
    public static class InfiniteSpireInstanceIds
    {
        public const string RoomPrefix = "spire.room.";
        public const int MaximumInstanceIdLength = 72;

        private const string WaveSuffix = ".wave.";
        private const string MechanismSuffix = ".mechanism.";
        private const string PuzzleSuffix = ".puzzle";
        private const string ChestSuffix = ".chest";
        private const string BossSuffix = ".boss";
        private const string StockSuffix = ".stock.";

        private const string HexDigits = "0123456789abcdef";

        public static string FormatRoomId(InfiniteSpireRoomInstanceKey room)
        {
            return Format(new InfiniteSpireInstanceId(
                room,
                InfiniteSpireInstanceEntityKind.Room));
        }

        public static string FormatWaveId(InfiniteSpireRoomInstanceKey room, int waveIndex)
        {
            return Format(new InfiniteSpireInstanceId(
                room,
                InfiniteSpireInstanceEntityKind.Wave,
                waveIndex));
        }

        public static string FormatMechanismId(
            InfiniteSpireRoomInstanceKey room,
            int mechanismIndex)
        {
            return Format(new InfiniteSpireInstanceId(
                room,
                InfiniteSpireInstanceEntityKind.Mechanism,
                mechanismIndex));
        }

        public static string FormatPuzzleId(InfiniteSpireRoomInstanceKey room)
        {
            return Format(new InfiniteSpireInstanceId(
                room,
                InfiniteSpireInstanceEntityKind.Puzzle));
        }

        public static string FormatChestId(InfiniteSpireRoomInstanceKey room)
        {
            return Format(new InfiniteSpireInstanceId(
                room,
                InfiniteSpireInstanceEntityKind.Chest));
        }

        public static string FormatBossId(InfiniteSpireRoomInstanceKey room)
        {
            return Format(new InfiniteSpireInstanceId(
                room,
                InfiniteSpireInstanceEntityKind.Boss));
        }

        public static string FormatStockId(InfiniteSpireRoomInstanceKey room, int stockIndex)
        {
            return Format(new InfiniteSpireInstanceId(
                room,
                InfiniteSpireInstanceEntityKind.Stock,
                stockIndex));
        }

        public static string Format(InfiniteSpireInstanceId id)
        {
            var buffer = new char[MaximumInstanceIdLength];
            if (!TryFormat(id, buffer, 0, out var written))
            {
                return string.Empty;
            }

            return new string(buffer, 0, written);
        }

        public static bool TryFormatRoomId(
            InfiniteSpireRoomInstanceKey room,
            char[] destination,
            int offset,
            out int charsWritten)
        {
            return TryFormat(
                new InfiniteSpireInstanceId(room, InfiniteSpireInstanceEntityKind.Room),
                destination,
                offset,
                out charsWritten);
        }

        public static bool TryFormat(
            InfiniteSpireInstanceId id,
            char[] destination,
            int offset,
            out int charsWritten)
        {
            charsWritten = 0;
            if (!id.IsValid ||
                destination == null ||
                offset < 0 ||
                offset > destination.Length)
            {
                return false;
            }

            var cursor = offset;
            if (!TryWriteLiteral(destination, ref cursor, RoomPrefix) ||
                !TryWriteHex8(destination, ref cursor, unchecked((uint)id.Room.RunSeed)) ||
                !TryWriteCharacter(destination, ref cursor, '.') ||
                !TryWriteDecimal(destination, ref cursor, id.Room.FloorIndex) ||
                !TryWriteCharacter(destination, ref cursor, '.') ||
                !TryWriteDecimal(destination, ref cursor, id.Room.RoomIndex))
            {
                return false;
            }

            var suffix = string.Empty;
            var indexed = false;
            switch (id.Kind)
            {
                case InfiniteSpireInstanceEntityKind.Room:
                    break;
                case InfiniteSpireInstanceEntityKind.Wave:
                    suffix = WaveSuffix;
                    indexed = true;
                    break;
                case InfiniteSpireInstanceEntityKind.Mechanism:
                    suffix = MechanismSuffix;
                    indexed = true;
                    break;
                case InfiniteSpireInstanceEntityKind.Puzzle:
                    suffix = PuzzleSuffix;
                    break;
                case InfiniteSpireInstanceEntityKind.Chest:
                    suffix = ChestSuffix;
                    break;
                case InfiniteSpireInstanceEntityKind.Boss:
                    suffix = BossSuffix;
                    break;
                case InfiniteSpireInstanceEntityKind.Stock:
                    suffix = StockSuffix;
                    indexed = true;
                    break;
                default:
                    return false;
            }

            if (!TryWriteLiteral(destination, ref cursor, suffix) ||
                (indexed && !TryWriteDecimal(destination, ref cursor, id.Slot)))
            {
                return false;
            }

            charsWritten = cursor - offset;
            return true;
        }

        public static bool TryParseRoomId(
            string value,
            out InfiniteSpireRoomInstanceKey room)
        {
            room = default;
            return TryParseRoomCore(value, out room, out var cursor) &&
                   cursor == value.Length;
        }

        public static bool TryParse(
            string value,
            out InfiniteSpireInstanceId instanceId)
        {
            instanceId = default;
            if (!TryParseRoomCore(value, out var room, out var cursor))
            {
                return false;
            }

            if (cursor == value.Length)
            {
                instanceId = new InfiniteSpireInstanceId(
                    room,
                    InfiniteSpireInstanceEntityKind.Room);
                return true;
            }

            if (TryReadIndexedSuffix(
                    value,
                    cursor,
                    WaveSuffix,
                    out var waveIndex))
            {
                instanceId = new InfiniteSpireInstanceId(
                    room,
                    InfiniteSpireInstanceEntityKind.Wave,
                    waveIndex);
                return true;
            }

            if (TryReadIndexedSuffix(
                    value,
                    cursor,
                    MechanismSuffix,
                    out var mechanismIndex))
            {
                instanceId = new InfiniteSpireInstanceId(
                    room,
                    InfiniteSpireInstanceEntityKind.Mechanism,
                    mechanismIndex);
                return true;
            }

            if (MatchesToEnd(value, cursor, PuzzleSuffix))
            {
                instanceId = new InfiniteSpireInstanceId(
                    room,
                    InfiniteSpireInstanceEntityKind.Puzzle);
                return true;
            }

            if (MatchesToEnd(value, cursor, ChestSuffix))
            {
                instanceId = new InfiniteSpireInstanceId(
                    room,
                    InfiniteSpireInstanceEntityKind.Chest);
                return true;
            }

            if (MatchesToEnd(value, cursor, BossSuffix))
            {
                instanceId = new InfiniteSpireInstanceId(
                    room,
                    InfiniteSpireInstanceEntityKind.Boss);
                return true;
            }

            if (TryReadIndexedSuffix(
                    value,
                    cursor,
                    StockSuffix,
                    out var stockIndex))
            {
                instanceId = new InfiniteSpireInstanceId(
                    room,
                    InfiniteSpireInstanceEntityKind.Stock,
                    stockIndex);
                return true;
            }

            return false;
        }

        private static bool TryParseRoomCore(
            string value,
            out InfiniteSpireRoomInstanceKey room,
            out int cursor)
        {
            room = default;
            cursor = 0;
            if (string.IsNullOrEmpty(value) ||
                !Matches(value, 0, RoomPrefix))
            {
                return false;
            }

            cursor = RoomPrefix.Length;
            if (!TryReadHex8(value, ref cursor, out var seedBits) ||
                cursor >= value.Length ||
                value[cursor++] != '.' ||
                !TryReadDecimalSegment(value, ref cursor, out var floorIndex) ||
                cursor >= value.Length ||
                value[cursor++] != '.' ||
                !TryReadDecimalSegment(value, ref cursor, out var roomIndex))
            {
                room = default;
                cursor = 0;
                return false;
            }

            room = new InfiniteSpireRoomInstanceKey(
                unchecked((int)seedBits),
                floorIndex,
                roomIndex);
            return room.IsValid;
        }

        private static bool TryReadHex8(string value, ref int cursor, out uint result)
        {
            result = 0u;
            if (cursor < 0 || cursor + 8 > value.Length)
            {
                return false;
            }

            for (var index = 0; index < 8; index++)
            {
                var character = value[cursor++];
                uint digit;
                if (character >= '0' && character <= '9')
                {
                    digit = (uint)(character - '0');
                }
                else if (character >= 'a' && character <= 'f')
                {
                    digit = (uint)(character - 'a' + 10);
                }
                else if (character >= 'A' && character <= 'F')
                {
                    digit = (uint)(character - 'A' + 10);
                }
                else
                {
                    return false;
                }

                result = (result << 4) | digit;
            }

            return true;
        }

        private static bool TryReadDecimalSegment(
            string value,
            ref int cursor,
            out int result)
        {
            result = 0;
            var start = cursor;
            while (cursor < value.Length)
            {
                var character = value[cursor];
                if (character == '.')
                {
                    break;
                }

                if (character < '0' || character > '9')
                {
                    return false;
                }

                var digit = character - '0';
                if (result > (int.MaxValue - digit) / 10)
                {
                    return false;
                }

                result = result * 10 + digit;
                cursor++;
            }

            return cursor > start;
        }

        private static bool TryReadIndexedSuffix(
            string value,
            int cursor,
            string suffix,
            out int index)
        {
            index = -1;
            if (!Matches(value, cursor, suffix))
            {
                return false;
            }

            cursor += suffix.Length;
            if (!TryReadDecimalSegment(value, ref cursor, out index))
            {
                index = -1;
                return false;
            }

            return cursor == value.Length;
        }

        private static bool MatchesToEnd(string value, int cursor, string expected)
        {
            return cursor + expected.Length == value.Length &&
                   Matches(value, cursor, expected);
        }

        private static bool Matches(string value, int cursor, string expected)
        {
            if (value == null ||
                expected == null ||
                cursor < 0 ||
                cursor + expected.Length > value.Length)
            {
                return false;
            }

            for (var index = 0; index < expected.Length; index++)
            {
                if (value[cursor + index] != expected[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryWriteLiteral(
            char[] destination,
            ref int cursor,
            string value)
        {
            if (cursor + value.Length > destination.Length)
            {
                return false;
            }

            for (var index = 0; index < value.Length; index++)
            {
                destination[cursor++] = value[index];
            }

            return true;
        }

        private static bool TryWriteCharacter(
            char[] destination,
            ref int cursor,
            char value)
        {
            if (cursor >= destination.Length)
            {
                return false;
            }

            destination[cursor++] = value;
            return true;
        }

        private static bool TryWriteHex8(
            char[] destination,
            ref int cursor,
            uint value)
        {
            if (cursor + 8 > destination.Length)
            {
                return false;
            }

            for (var shift = 28; shift >= 0; shift -= 4)
            {
                destination[cursor++] = HexDigits[(int)((value >> shift) & 0x0fu)];
            }

            return true;
        }

        private static bool TryWriteDecimal(
            char[] destination,
            ref int cursor,
            int value)
        {
            if (value < 0)
            {
                return false;
            }

            var digits = DecimalDigitCount(value);
            if (cursor + digits > destination.Length)
            {
                return false;
            }

            var write = cursor + digits - 1;
            do
            {
                destination[write--] = (char)('0' + value % 10);
                value /= 10;
            }
            while (value > 0);

            cursor += digits;
            return true;
        }

        private static int DecimalDigitCount(int value)
        {
            if (value < 10)
            {
                return 1;
            }

            var digits = 0;
            do
            {
                digits++;
                value /= 10;
            }
            while (value > 0);

            return digits;
        }
    }

    public struct InfiniteSpirePuzzleSelection
    {
        private int value0;
        private int value1;
        private int value2;
        private int value3;

        public InfiniteSpirePuzzleKind Kind { get; internal set; }
        public int Count { get; internal set; }
        public int StateCount { get; internal set; }

        public int GetValue(int index)
        {
            return index switch
            {
                0 when Count > 0 => value0,
                1 when Count > 1 => value1,
                2 when Count > 2 => value2,
                3 when Count > 3 => value3,
                _ => -1
            };
        }

        internal void SetValue(int index, int value)
        {
            switch (index)
            {
                case 0:
                    value0 = value;
                    break;
                case 1:
                    value1 = value;
                    break;
                case 2:
                    value2 = value;
                    break;
                case 3:
                    value3 = value;
                    break;
            }
        }
    }

    public struct InfiniteSpireMerchantStockSelection
    {
        private InfiniteSpireMerchantOfferDefinition offer0;
        private InfiniteSpireMerchantOfferDefinition offer1;
        private InfiniteSpireMerchantOfferDefinition offer2;

        public InfiniteSpireMerchantKitDefinition Kit { get; internal set; }
        public int Count { get; internal set; }

        public InfiniteSpireMerchantOfferDefinition GetOffer(int index)
        {
            return index switch
            {
                0 when Count > 0 => offer0,
                1 when Count > 1 => offer1,
                2 when Count > 2 => offer2,
                _ => null
            };
        }

        internal void SetOffer(int index, InfiniteSpireMerchantOfferDefinition offer)
        {
            switch (index)
            {
                case 0:
                    offer0 = offer;
                    break;
                case 1:
                    offer1 = offer;
                    break;
                case 2:
                    offer2 = offer;
                    break;
            }
        }

        internal bool Contains(InfiniteSpireMerchantOfferDefinition offer)
        {
            return (Count > 0 && ReferenceEquals(offer0, offer)) ||
                   (Count > 1 && ReferenceEquals(offer1, offer)) ||
                   (Count > 2 && ReferenceEquals(offer2, offer));
        }
    }

    public struct InfiniteSpireRoomPlan
    {
        public InfiniteSpireRoomInstanceKey InstanceKey { get; internal set; }
        public InfiniteSpireRoomBeat Beat { get; internal set; }
        public InfiniteSpireTemplateDefinition Template { get; internal set; }
        public uint RoomSeed { get; internal set; }
        public int LayoutVariant { get; internal set; }
        public int RotationQuarterTurns { get; internal set; }
        public int WaveSetIndex { get; internal set; }
        public InfiniteSpireWaveSetDefinition WaveSet { get; internal set; }
        public InfiniteSpirePuzzleSelection Puzzle { get; internal set; }
        public InfiniteSpireMerchantStockSelection MerchantStock { get; internal set; }
        public InfiniteSpireMinibossProfile MinibossProfile { get; internal set; }
        public InfiniteSpireBossProfile BossProfile { get; internal set; }
        public int TreasureSpirecoil { get; internal set; }
        public int RehashAttempts { get; internal set; }
    }

    /// <summary>
    /// Reusable five-room output buffer. Construct once and pass it to every floor generation call.
    /// </summary>
    public sealed class InfiniteSpireFloorPlan
    {
        public const int MinimumRoomCount = 3;
        public const int MaximumRoomCount = 5;

        private readonly InfiniteSpireRoomPlan[] rooms =
            new InfiniteSpireRoomPlan[MaximumRoomCount];

        public int FloorIndex { get; private set; }
        public InfiniteSpireCadence Cadence { get; private set; }
        public int Count { get; private set; }

        public InfiniteSpireRoomPlan GetRoom(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return rooms[index];
        }

        public bool TryGetRoom(int index, out InfiniteSpireRoomPlan room)
        {
            if (index < 0 || index >= Count)
            {
                room = default;
                return false;
            }

            room = rooms[index];
            return true;
        }

        public int CopyTo(InfiniteSpireRoomPlan[] destination, int destinationIndex)
        {
            if (destination == null ||
                destinationIndex < 0 ||
                destinationIndex + Count > destination.Length)
            {
                return 0;
            }

            Array.Copy(rooms, 0, destination, destinationIndex, Count);
            return Count;
        }

        internal void Begin(int floorIndex, InfiniteSpireCadence cadence)
        {
            FloorIndex = floorIndex;
            Cadence = cadence;
            Count = 0;
        }

        internal bool Add(InfiniteSpireRoomPlan room)
        {
            if (Count >= rooms.Length)
            {
                return false;
            }

            rooms[Count++] = room;
            return true;
        }
    }

    /// <summary>
    /// Bounded caller-owned history used by anti-repetition. It allocates only in its constructor.
    /// </summary>
    public sealed class InfiniteSpireGenerationState
    {
        public const int TemplateHistoryCapacity = 8;
        public const int BossHistoryCapacity = 3;

        private readonly string[] recentTemplateIds =
            new string[TemplateHistoryCapacity];
        private readonly InfiniteSpireRoomCategory[] recentCategories =
            new InfiniteSpireRoomCategory[TemplateHistoryCapacity];
        private readonly int[] recentTemplateFloors =
            new int[TemplateHistoryCapacity];
        private readonly string[] recentBossIds =
            new string[BossHistoryCapacity];

        public int RecentTemplateCount { get; private set; }
        public int RecentBossCount { get; private set; }
        public int LastMerchantFloor { get; private set; } = int.MinValue;
        public int LastMeridianFloor { get; private set; } = int.MinValue;

        public string GetRecentTemplateId(int index)
        {
            return index >= 0 && index < RecentTemplateCount
                ? recentTemplateIds[index]
                : string.Empty;
        }

        public InfiniteSpireRoomCategory GetRecentCategory(int index)
        {
            return index >= 0 && index < RecentTemplateCount
                ? recentCategories[index]
                : InfiniteSpireRoomCategory.Easy;
        }

        public int GetRecentTemplateFloor(int index)
        {
            return index >= 0 && index < RecentTemplateCount
                ? recentTemplateFloors[index]
                : 0;
        }

        public string GetRecentBossId(int index)
        {
            return index >= 0 && index < RecentBossCount
                ? recentBossIds[index]
                : string.Empty;
        }

        public void Reset()
        {
            Array.Clear(recentTemplateIds, 0, recentTemplateIds.Length);
            Array.Clear(recentCategories, 0, recentCategories.Length);
            Array.Clear(recentTemplateFloors, 0, recentTemplateFloors.Length);
            Array.Clear(recentBossIds, 0, recentBossIds.Length);
            RecentTemplateCount = 0;
            RecentBossCount = 0;
            LastMerchantFloor = int.MinValue;
            LastMeridianFloor = int.MinValue;
        }

        public bool RestoreRecentTemplate(string templateId, int floorIndex)
        {
            var template = InfiniteSpireCatalog.GetTemplate(templateId);
            if (template == null || floorIndex < 1)
            {
                return false;
            }

            RecordTemplate(template, floorIndex);
            if (template.Category == InfiniteSpireRoomCategory.Merchant)
            {
                LastMerchantFloor = floorIndex;
            }
            else if (template.Category == InfiniteSpireRoomCategory.Rest)
            {
                LastMeridianFloor = floorIndex;
            }

            return true;
        }

        public bool RestoreRecentBoss(string bossId)
        {
            if (string.IsNullOrEmpty(bossId) || !IsCatalogBossId(bossId))
            {
                return false;
            }

            RecordBoss(bossId);
            return true;
        }

        internal bool ContainsTemplate(string templateId)
        {
            for (var index = 0; index < RecentTemplateCount; index++)
            {
                if (string.Equals(
                        recentTemplateIds[index],
                        templateId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool ContainsBoss(string bossId)
        {
            for (var index = 0; index < RecentBossCount; index++)
            {
                if (string.Equals(recentBossIds[index], bossId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        internal int GetTemplateAge(string templateId)
        {
            for (var index = 0; index < RecentTemplateCount; index++)
            {
                if (string.Equals(
                        recentTemplateIds[index],
                        templateId,
                        StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return int.MaxValue;
        }

        internal int GetCategoryAge(InfiniteSpireRoomCategory category)
        {
            for (var index = 0; index < RecentTemplateCount; index++)
            {
                if (recentCategories[index] == category)
                {
                    return index;
                }
            }

            return int.MaxValue;
        }

        internal void RecordRoom(InfiniteSpireTemplateDefinition template, int floorIndex)
        {
            RecordTemplate(template, floorIndex);
            if (template.Category == InfiniteSpireRoomCategory.Merchant)
            {
                LastMerchantFloor = floorIndex;
            }
            else if (template.Category == InfiniteSpireRoomCategory.Rest)
            {
                LastMeridianFloor = floorIndex;
            }
        }

        internal void RecordBoss(string bossId)
        {
            var upper = Math.Min(RecentBossCount, recentBossIds.Length - 1);
            for (var index = upper; index > 0; index--)
            {
                recentBossIds[index] = recentBossIds[index - 1];
            }

            recentBossIds[0] = bossId;
            RecentBossCount = Math.Min(RecentBossCount + 1, recentBossIds.Length);
        }

        private void RecordTemplate(
            InfiniteSpireTemplateDefinition template,
            int floorIndex)
        {
            var upper = Math.Min(RecentTemplateCount, recentTemplateIds.Length - 1);
            for (var index = upper; index > 0; index--)
            {
                recentTemplateIds[index] = recentTemplateIds[index - 1];
                recentCategories[index] = recentCategories[index - 1];
                recentTemplateFloors[index] = recentTemplateFloors[index - 1];
            }

            recentTemplateIds[0] = template.Id;
            recentCategories[0] = template.Category;
            recentTemplateFloors[0] = floorIndex;
            RecentTemplateCount = Math.Min(
                RecentTemplateCount + 1,
                recentTemplateIds.Length);
        }

        private static bool IsCatalogBossId(string bossId)
        {
            for (var index = 0; index < InfiniteSpireCatalog.BossProfiles.Count; index++)
            {
                if (string.Equals(
                        InfiniteSpireCatalog.BossProfiles[index].BossId,
                        bossId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class InfiniteSpireGenerator
    {
        public const int MaximumRehashAttempts = 12;

        private const int RoomCategoryCount = 10;
        private const uint ApproachCategorySalt = 0x1bf27499u;
        private const uint PressureCategorySalt = 0x72ac94e1u;
        private const uint SealCategorySalt = 0x93861743u;
        private const uint LayoutSalt = 0x27c3a5d9u;
        private const uint RotationSalt = 0xf06a129bu;
        private const uint WaveSalt = 0x9b5248d1u;
        private const uint PuzzleSalt = 0x3e71c65fu;
        private const uint MerchantSalt = 0xc6540a73u;
        private const uint TreasureSalt = 0x17b93f2du;
        private const uint MinibossSalt = 0x4d2f813bu;
        private const uint BossSalt = 0xe18249a7u;

        public static bool TryGenerateFloor(
            int runSeed,
            int floorIndex,
            InfiniteSpireGenerationState state,
            InfiniteSpireFloorPlan destination)
        {
            if (state == null || destination == null)
            {
                return false;
            }

            floorIndex = floorIndex < 1 ? 1 : floorIndex;
            var boss = InfiniteSpireCadenceRules.IsBossFloor(floorIndex);
            var miniboss = InfiniteSpireCadenceRules.IsMinibossFloor(floorIndex);
            var merchant =
                InfiniteSpireCadenceRules.IsMerchantEligibleFloor(floorIndex) &&
                state.LastMerchantFloor != floorIndex - 1;
            var meridian =
                InfiniteSpireCadenceRules.IsLoomrestMeridianFloor(floorIndex);

            var cadence = InfiniteSpireCadence.None;
            if (boss)
            {
                cadence |= InfiniteSpireCadence.Boss;
            }
            else if (miniboss)
            {
                cadence |= InfiniteSpireCadence.Miniboss;
            }

            if (merchant)
            {
                cadence |= InfiniteSpireCadence.Merchant;
            }

            if (meridian)
            {
                cadence |= InfiniteSpireCadence.LoomrestMeridian;
            }

            destination.Begin(floorIndex, cadence);

            var specialCount = (boss || miniboss ? 1 : 0) +
                               (merchant ? 1 : 0) +
                               (meridian ? 1 : 0);
            if (specialCount == 0)
            {
                specialCount = 1;
            }

            var baseRoomCount = 3 + floorIndex % 3;
            var requiredRoomCount = 2 + specialCount;
            var targetRoomCount = Math.Min(
                InfiniteSpireFloorPlan.MaximumRoomCount,
                Math.Max(baseRoomCount, requiredRoomCount));
            var pressureCount = Math.Min(
                2,
                Math.Max(0, targetRoomCount - requiredRoomCount));

            var approachCategory = floorIndex == 1
                ? InfiniteSpireRoomCategory.Easy
                : SelectApproachCategory(runSeed, floorIndex);
            if (!AppendRoom(
                    runSeed,
                    floorIndex,
                    InfiniteSpireRoomBeat.Approach,
                    approachCategory,
                    floorIndex == 1,
                    state,
                    destination))
            {
                return false;
            }

            for (var pressureIndex = 0; pressureIndex < pressureCount; pressureIndex++)
            {
                var category = floorIndex == 1
                    ? InfiniteSpireRoomCategory.Combat
                    : SelectPressureCategory(
                        runSeed,
                        floorIndex,
                        destination.Count);
                if (!AppendRoom(
                        runSeed,
                        floorIndex,
                        InfiniteSpireRoomBeat.Pressure,
                        category,
                        false,
                        state,
                        destination))
                {
                    return false;
                }
            }

            if (boss)
            {
                if (!AppendRoom(
                        runSeed,
                        floorIndex,
                        InfiniteSpireRoomBeat.Cadence,
                        InfiniteSpireRoomCategory.Boss,
                        true,
                        state,
                        destination))
                {
                    return false;
                }
            }
            else if (miniboss)
            {
                if (!AppendRoom(
                        runSeed,
                        floorIndex,
                        InfiniteSpireRoomBeat.Cadence,
                        InfiniteSpireRoomCategory.Miniboss,
                        true,
                        state,
                        destination))
                {
                    return false;
                }
            }

            if (merchant &&
                !AppendRoom(
                    runSeed,
                    floorIndex,
                    InfiniteSpireRoomBeat.Cadence,
                    InfiniteSpireRoomCategory.Merchant,
                    true,
                    state,
                    destination))
            {
                return false;
            }

            if (meridian &&
                !AppendRoom(
                    runSeed,
                    floorIndex,
                    InfiniteSpireRoomBeat.Cadence,
                    InfiniteSpireRoomCategory.Rest,
                    true,
                    state,
                    destination))
            {
                return false;
            }

            if (!boss && !miniboss && !merchant && !meridian)
            {
                var fallbackCadence = floorIndex == 1
                    ? InfiniteSpireRoomCategory.Combat
                    : InfiniteSpireRoomCategory.Treasure;
                if (!AppendRoom(
                        runSeed,
                        floorIndex,
                        InfiniteSpireRoomBeat.Cadence,
                        fallbackCadence,
                        true,
                        state,
                        destination))
                {
                    return false;
                }
            }

            var sealCategory = floorIndex == 1
                ? InfiniteSpireRoomCategory.Easy
                : SelectSealCategory(runSeed, floorIndex, destination.Count);
            if (!AppendRoom(
                    runSeed,
                    floorIndex,
                    InfiniteSpireRoomBeat.Seal,
                    sealCategory,
                    floorIndex == 1,
                    state,
                    destination))
            {
                return false;
            }

            return destination.Count >= InfiniteSpireFloorPlan.MinimumRoomCount &&
                   destination.Count <= InfiniteSpireFloorPlan.MaximumRoomCount;
        }

        private static bool AppendRoom(
            int runSeed,
            int floorIndex,
            InfiniteSpireRoomBeat beat,
            InfiniteSpireRoomCategory desiredCategory,
            bool strictCategory,
            InfiniteSpireGenerationState state,
            InfiniteSpireFloorPlan destination)
        {
            var roomIndex = destination.Count;
            var template = SelectTemplate(
                runSeed,
                floorIndex,
                roomIndex,
                beat,
                desiredCategory,
                strictCategory,
                state,
                out var attempts);
            if (template == null)
            {
                return false;
            }

            var roomSeed = InfiniteSpireHash.Compute(
                runSeed,
                floorIndex,
                roomIndex,
                template.Salt);
            var plan = new InfiniteSpireRoomPlan
            {
                InstanceKey = new InfiniteSpireRoomInstanceKey(
                    runSeed,
                    floorIndex,
                    roomIndex),
                Beat = beat,
                Template = template,
                RoomSeed = roomSeed,
                LayoutVariant = SelectBounded(
                    InfiniteSpireHash.Mix(roomSeed, LayoutSalt),
                    template.LayoutVariantCount),
                RotationQuarterTurns = SelectBounded(
                    InfiniteSpireHash.Mix(roomSeed, RotationSalt),
                    4),
                RehashAttempts = attempts,
                WaveSetIndex = -1
            };

            SelectWaveSet(template, roomSeed, ref plan);
            plan.Puzzle = SelectPuzzle(
                template,
                runSeed,
                floorIndex,
                roomIndex);
            plan.MerchantStock = SelectMerchantStock(
                template,
                runSeed,
                floorIndex,
                roomIndex);
            if (template.Category == InfiniteSpireRoomCategory.Miniboss)
            {
                plan.MinibossProfile = SelectMinibossProfile(
                    runSeed,
                    floorIndex,
                    roomIndex,
                    template.Salt);
            }
            else if (template.Category == InfiniteSpireRoomCategory.Boss)
            {
                plan.BossProfile = SelectBossProfile(
                    runSeed,
                    floorIndex,
                    roomIndex,
                    template.Salt,
                    state);
                if (plan.BossProfile == null)
                {
                    return false;
                }

                state.RecordBoss(plan.BossProfile.BossId);
            }
            else if (template.Category == InfiniteSpireRoomCategory.Treasure)
            {
                plan.TreasureSpirecoil = 12 + SelectBounded(
                    InfiniteSpireHash.Compute(
                        runSeed,
                        floorIndex,
                        roomIndex,
                        InfiniteSpireHash.Mix(template.Salt, TreasureSalt)),
                    17);
            }

            if (!destination.Add(plan))
            {
                return false;
            }

            state.RecordRoom(template, floorIndex);
            return true;
        }

        private static InfiniteSpireTemplateDefinition SelectTemplate(
            int runSeed,
            int floorIndex,
            int roomIndex,
            InfiniteSpireRoomBeat beat,
            InfiniteSpireRoomCategory desiredCategory,
            bool strictCategory,
            InfiniteSpireGenerationState state,
            out int attempts)
        {
            var salt = InfiniteSpireHash.Mix(
                0x6c93a251u + (uint)desiredCategory,
                (uint)beat);
            var eligibleCount = CountTemplates(desiredCategory, beat);
            for (var attempt = 0; attempt < MaximumRehashAttempts; attempt++)
            {
                var pick = InfiniteSpireHash.Compute(
                    runSeed,
                    floorIndex,
                    roomIndex,
                    InfiniteSpireHash.Mix(salt, (uint)attempt));
                var candidate = GetTemplateByOrdinal(
                    desiredCategory,
                    beat,
                    SelectBounded(pick, eligibleCount));
                if (IsTemplateLegal(candidate, floorIndex, state))
                {
                    attempts = attempt;
                    return candidate;
                }
            }

            attempts = MaximumRehashAttempts;
            if (strictCategory)
            {
                return SelectLeastRecentTemplate(
                    desiredCategory,
                    beat,
                    floorIndex,
                    runSeed,
                    roomIndex,
                    state,
                    false);
            }

            var fallbackCategory = SelectLeastRecentLegalCategory(
                beat,
                floorIndex,
                runSeed,
                roomIndex,
                state);
            if (fallbackCategory.HasValue)
            {
                return SelectLeastRecentTemplate(
                    fallbackCategory.Value,
                    beat,
                    floorIndex,
                    runSeed,
                    roomIndex,
                    state,
                    false);
            }

            // This final path is deterministic recovery for corrupt caller history.
            return SelectLeastRecentTemplate(
                desiredCategory,
                beat,
                floorIndex,
                runSeed,
                roomIndex,
                state,
                true);
        }

        private static InfiniteSpireRoomCategory? SelectLeastRecentLegalCategory(
            InfiniteSpireRoomBeat beat,
            int floorIndex,
            int runSeed,
            int roomIndex,
            InfiniteSpireGenerationState state)
        {
            var rotation = SelectBounded(
                InfiniteSpireHash.Compute(
                    runSeed,
                    floorIndex,
                    roomIndex,
                    0xc73a91e5u),
                RoomCategoryCount);
            InfiniteSpireRoomCategory? selected = null;
            var selectedAge = -1;
            for (var offset = 0; offset < RoomCategoryCount; offset++)
            {
                var category = (InfiniteSpireRoomCategory)(
                    (rotation + offset) % RoomCategoryCount);
                if (!IsFlexibleCategoryAllowed(beat, category) ||
                    !HasLegalTemplate(category, beat, floorIndex, state))
                {
                    continue;
                }

                var age = state.GetCategoryAge(category);
                if (!selected.HasValue || age > selectedAge)
                {
                    selected = category;
                    selectedAge = age;
                }
            }

            return selected;
        }

        private static InfiniteSpireTemplateDefinition SelectLeastRecentTemplate(
            InfiniteSpireRoomCategory category,
            InfiniteSpireRoomBeat beat,
            int floorIndex,
            int runSeed,
            int roomIndex,
            InfiniteSpireGenerationState state,
            bool relaxHistory)
        {
            var count = CountTemplates(category, beat);
            if (count <= 0)
            {
                return null;
            }

            var rotation = SelectBounded(
                InfiniteSpireHash.Compute(
                    runSeed,
                    floorIndex,
                    roomIndex,
                    0x35b7c921u + (uint)category),
                count);
            InfiniteSpireTemplateDefinition selected = null;
            var selectedAge = -1;
            for (var offset = 0; offset < count; offset++)
            {
                var candidate = GetTemplateByOrdinal(
                    category,
                    beat,
                    (rotation + offset) % count);
                if (candidate == null ||
                    (!relaxHistory && !IsTemplateLegal(candidate, floorIndex, state)))
                {
                    continue;
                }

                var age = state.GetTemplateAge(candidate.Id);
                if (selected == null || age > selectedAge)
                {
                    selected = candidate;
                    selectedAge = age;
                }
            }

            return selected;
        }

        private static bool HasLegalTemplate(
            InfiniteSpireRoomCategory category,
            InfiniteSpireRoomBeat beat,
            int floorIndex,
            InfiniteSpireGenerationState state)
        {
            for (var index = 0; index < InfiniteSpireCatalog.Templates.Count; index++)
            {
                var template = InfiniteSpireCatalog.Templates[index];
                if (template.Category == category &&
                    template.SupportsBeat(beat) &&
                    IsTemplateLegal(template, floorIndex, state))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTemplateLegal(
            InfiniteSpireTemplateDefinition template,
            int floorIndex,
            InfiniteSpireGenerationState state)
        {
            if (template == null || state.ContainsTemplate(template.Id))
            {
                return false;
            }

            if (state.RecentTemplateCount >= 2 &&
                state.GetRecentCategory(0) == template.Category &&
                state.GetRecentCategory(1) == template.Category)
            {
                return false;
            }

            if (state.RecentTemplateCount >= 1)
            {
                var previous = state.GetRecentCategory(0);
                if ((previous == InfiniteSpireRoomCategory.Puzzle &&
                     template.Category == InfiniteSpireRoomCategory.Puzzle) ||
                    (previous == InfiniteSpireRoomCategory.Swarm &&
                     template.Category == InfiniteSpireRoomCategory.Swarm))
                {
                    return false;
                }
            }

            if (template.Category == InfiniteSpireRoomCategory.Merchant)
            {
                return InfiniteSpireCadenceRules.IsMerchantEligibleFloor(floorIndex) &&
                       state.LastMerchantFloor != floorIndex &&
                       state.LastMerchantFloor != floorIndex - 1;
            }

            if (template.Category == InfiniteSpireRoomCategory.Rest)
            {
                return InfiniteSpireCadenceRules.IsLoomrestMeridianFloor(floorIndex) &&
                       (state.LastMeridianFloor == int.MinValue ||
                        floorIndex - state.LastMeridianFloor >= 4);
            }

            return true;
        }

        private static bool IsFlexibleCategoryAllowed(
            InfiniteSpireRoomBeat beat,
            InfiniteSpireRoomCategory category)
        {
            return beat switch
            {
                InfiniteSpireRoomBeat.Approach =>
                    category == InfiniteSpireRoomCategory.Easy ||
                    category == InfiniteSpireRoomCategory.Traversal,
                InfiniteSpireRoomBeat.Pressure =>
                    category == InfiniteSpireRoomCategory.Combat ||
                    category == InfiniteSpireRoomCategory.Swarm ||
                    category == InfiniteSpireRoomCategory.Traversal ||
                    category == InfiniteSpireRoomCategory.Puzzle,
                InfiniteSpireRoomBeat.Seal =>
                    category == InfiniteSpireRoomCategory.Easy ||
                    category == InfiniteSpireRoomCategory.Traversal,
                _ => false
            };
        }

        private static InfiniteSpireRoomCategory SelectApproachCategory(
            int runSeed,
            int floorIndex)
        {
            var pick = InfiniteSpireHash.Compute(
                runSeed,
                floorIndex,
                0,
                ApproachCategorySalt);
            return pick % 4u == 0u
                ? InfiniteSpireRoomCategory.Traversal
                : InfiniteSpireRoomCategory.Easy;
        }

        private static InfiniteSpireRoomCategory SelectSealCategory(
            int runSeed,
            int floorIndex,
            int roomIndex)
        {
            var pick = InfiniteSpireHash.Compute(
                runSeed,
                floorIndex,
                roomIndex,
                SealCategorySalt);
            return pick % 3u == 0u
                ? InfiniteSpireRoomCategory.Traversal
                : InfiniteSpireRoomCategory.Easy;
        }

        private static InfiniteSpireRoomCategory SelectPressureCategory(
            int runSeed,
            int floorIndex,
            int roomIndex)
        {
            var swarmWeight = floorIndex >= 25 ? 2 : 1;
            var totalWeight = 5 + swarmWeight + 2 + 1;
            var pick = SelectBounded(
                InfiniteSpireHash.Compute(
                    runSeed,
                    floorIndex,
                    roomIndex,
                    PressureCategorySalt),
                totalWeight);
            if (pick < 5)
            {
                return InfiniteSpireRoomCategory.Combat;
            }

            pick -= 5;
            if (pick < swarmWeight)
            {
                return InfiniteSpireRoomCategory.Swarm;
            }

            pick -= swarmWeight;
            if (pick < 2)
            {
                return InfiniteSpireRoomCategory.Puzzle;
            }

            return InfiniteSpireRoomCategory.Traversal;
        }

        private static void SelectWaveSet(
            InfiniteSpireTemplateDefinition template,
            uint roomSeed,
            ref InfiniteSpireRoomPlan plan)
        {
            if (template.WaveSets == null || template.WaveSets.Count == 0)
            {
                plan.WaveSetIndex = -1;
                plan.WaveSet = null;
                return;
            }

            var index = SelectBounded(
                InfiniteSpireHash.Mix(roomSeed, WaveSalt),
                template.WaveSets.Count);
            plan.WaveSetIndex = index;
            plan.WaveSet = template.WaveSets[index];
        }

        private static InfiniteSpirePuzzleSelection SelectPuzzle(
            InfiniteSpireTemplateDefinition template,
            int runSeed,
            int floorIndex,
            int roomIndex)
        {
            var selection = new InfiniteSpirePuzzleSelection
            {
                Kind = template.PuzzleKind,
                Count = Math.Min(4, Math.Max(0, template.MechanismCount)),
                StateCount = Math.Max(0, template.MechanismStateCount)
            };
            if (selection.Kind == InfiniteSpirePuzzleKind.None || selection.Count == 0)
            {
                return selection;
            }

            var hash = InfiniteSpireHash.Compute(
                runSeed,
                floorIndex,
                roomIndex,
                InfiniteSpireHash.Mix(template.Salt, PuzzleSalt));
            if (selection.Kind == InfiniteSpirePuzzleKind.OrderedSequence)
            {
                for (var index = 0; index < selection.Count; index++)
                {
                    selection.SetValue(index, index);
                }

                for (var index = selection.Count - 1; index > 0; index--)
                {
                    hash = InfiniteSpireHash.Rehash(hash, selection.Count - index);
                    var swapIndex = SelectBounded(hash, index + 1);
                    var current = selection.GetValue(index);
                    selection.SetValue(index, selection.GetValue(swapIndex));
                    selection.SetValue(swapIndex, current);
                }

                return selection;
            }

            var stateCount = Math.Max(2, selection.StateCount);
            selection.StateCount = stateCount;
            for (var index = 0; index < selection.Count; index++)
            {
                hash = InfiniteSpireHash.Rehash(hash, index + 1);
                selection.SetValue(index, SelectBounded(hash, stateCount));
            }

            return selection;
        }

        private static InfiniteSpireMerchantStockSelection SelectMerchantStock(
            InfiniteSpireTemplateDefinition template,
            int runSeed,
            int floorIndex,
            int roomIndex)
        {
            var selection = new InfiniteSpireMerchantStockSelection
            {
                Kit = template.MerchantKit,
                Count = 0
            };
            var kit = template.MerchantKit;
            if (kit == null || kit.Offers == null || kit.Offers.Count == 0)
            {
                return selection;
            }

            var targetCount = Math.Min(3, kit.Offers.Count);
            var hash = InfiniteSpireHash.Compute(
                runSeed,
                floorIndex,
                roomIndex,
                InfiniteSpireHash.Mix(template.Salt, MerchantSalt));
            for (var slot = 0; slot < targetCount; slot++)
            {
                InfiniteSpireMerchantOfferDefinition selected = null;
                for (var attempt = 0; attempt < kit.Offers.Count; attempt++)
                {
                    hash = InfiniteSpireHash.Rehash(hash, slot * 17 + attempt + 1);
                    var candidate = kit.Offers[SelectBounded(hash, kit.Offers.Count)];
                    if (!selection.Contains(candidate))
                    {
                        selected = candidate;
                        break;
                    }
                }

                if (selected == null)
                {
                    for (var index = 0; index < kit.Offers.Count; index++)
                    {
                        if (!selection.Contains(kit.Offers[index]))
                        {
                            selected = kit.Offers[index];
                            break;
                        }
                    }
                }

                if (selected == null)
                {
                    break;
                }

                selection.SetOffer(slot, selected);
                selection.Count++;
            }

            return selection;
        }

        private static InfiniteSpireMinibossProfile SelectMinibossProfile(
            int runSeed,
            int floorIndex,
            int roomIndex,
            uint templateSalt)
        {
            var count = InfiniteSpireCatalog.MinibossProfiles.Count;
            if (count == 0)
            {
                return null;
            }

            var hash = InfiniteSpireHash.Compute(
                runSeed,
                floorIndex,
                roomIndex,
                InfiniteSpireHash.Mix(templateSalt, MinibossSalt));
            return InfiniteSpireCatalog.MinibossProfiles[SelectBounded(hash, count)];
        }

        private static InfiniteSpireBossProfile SelectBossProfile(
            int runSeed,
            int floorIndex,
            int roomIndex,
            uint templateSalt,
            InfiniteSpireGenerationState state)
        {
            var count = InfiniteSpireCatalog.BossProfiles.Count;
            if (count == 0)
            {
                return null;
            }

            var baseHash = InfiniteSpireHash.Compute(
                runSeed,
                floorIndex,
                roomIndex,
                InfiniteSpireHash.Mix(templateSalt, BossSalt));
            for (var attempt = 0; attempt < MaximumRehashAttempts; attempt++)
            {
                var hash = InfiniteSpireHash.Rehash(baseHash, attempt);
                var profile = InfiniteSpireCatalog.BossProfiles[SelectBounded(hash, count)];
                if (!state.ContainsBoss(profile.BossId))
                {
                    return profile;
                }
            }

            var start = SelectBounded(baseHash, count);
            for (var offset = 0; offset < count; offset++)
            {
                var profile = InfiniteSpireCatalog.BossProfiles[(start + offset) % count];
                if (!state.ContainsBoss(profile.BossId))
                {
                    return profile;
                }
            }

            return InfiniteSpireCatalog.BossProfiles[start];
        }

        private static int CountTemplates(
            InfiniteSpireRoomCategory category,
            InfiniteSpireRoomBeat beat)
        {
            return InfiniteSpireCatalog.CountTemplates(category, beat);
        }

        private static InfiniteSpireTemplateDefinition GetTemplateByOrdinal(
            InfiniteSpireRoomCategory category,
            InfiniteSpireRoomBeat beat,
            int ordinal)
        {
            if (ordinal < 0)
            {
                return null;
            }

            for (var index = 0; index < InfiniteSpireCatalog.Templates.Count; index++)
            {
                var template = InfiniteSpireCatalog.Templates[index];
                if (template.Category != category || !template.SupportsBeat(beat))
                {
                    continue;
                }

                if (ordinal == 0)
                {
                    return template;
                }

                ordinal--;
            }

            return null;
        }

        private static int SelectBounded(uint value, int count)
        {
            return count <= 1 ? 0 : (int)(value % (uint)count);
        }
    }
}
