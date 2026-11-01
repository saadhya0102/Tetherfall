using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public enum ModularDungeonRoomKind
    {
        Entrance,
        Puzzle,
        Combat,
        Checkpoint,
        Traversal,
        Boss,
        Exit
    }

    public enum ModularDungeonPuzzleKind
    {
        OrderedSequence,
        StateMatch
    }

    [Serializable]
    public sealed class DungeonMechanismDefinition
    {
        public string id;
        public string displayName;
        public Vector3 localPosition;
        public int stateCount = 1;
        public string[] stateLabels = Array.Empty<string>();
    }

    [Serializable]
    public sealed class DungeonPuzzleDefinition
    {
        public string id;
        public string roomId;
        public string displayName;
        public ModularDungeonPuzzleKind kind;
        public string clue;
        public int[] solution = Array.Empty<int>();
        public List<DungeonMechanismDefinition> mechanisms = new List<DungeonMechanismDefinition>();
    }

    [Serializable]
    public sealed class DungeonWaveDefinition
    {
        public string id;
        public string[] enemyIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class DungeonRoomDefinition
    {
        public string id;
        public string displayName;
        public ModularDungeonRoomKind kind;
        public Vector3 localCenter;
        public Vector3 size;
        public Vector3 safeLocalPosition;
        public string puzzleId;
        public string checkpointId;
        public List<DungeonWaveDefinition> waves = new List<DungeonWaveDefinition>();
    }

    [Serializable]
    public sealed class DungeonConnectionDefinition
    {
        public string id;
        public string fromRoomId;
        public string toRoomId;
        public Vector3 doorLocalPosition;
        public Vector3 doorScale;
        public float wallWidth;
    }

    [Serializable]
    public sealed class DungeonCheckpointDefinition
    {
        public string id;
        public string roomId;
        public string displayName;
        public Vector3 localPosition;
    }

    [Serializable]
    public sealed class ModularDungeonDefinition
    {
        public string id;
        public string displayName;
        public string region;
        public string questId;
        public string requiredQuestId;
        public string lockedMessage;
        public Vector3 gatePosition;
        public Vector3 returnPosition;
        public Vector3 interiorOrigin;
        public Vector3 localBoundsCenter;
        public Vector3 localBoundsSize;
        public string entranceRoomId;
        public string bossRoomId;
        public string exitRoomId;
        public string bossId;
        public string rewardEquipmentId;
        public int favorReward;
        public int experienceReward;
        public ResourceKind resourceRewardKind;
        public int resourceRewardAmount;
        public Color accentColor;
        public int rendererBudget;
        public int colliderBudget;
        public int addedLightBudget;
        public List<DungeonRoomDefinition> rooms = new List<DungeonRoomDefinition>();
        public List<DungeonConnectionDefinition> connections = new List<DungeonConnectionDefinition>();
        public List<DungeonPuzzleDefinition> puzzles = new List<DungeonPuzzleDefinition>();
        public List<DungeonCheckpointDefinition> checkpoints = new List<DungeonCheckpointDefinition>();

        public DungeonRoomDefinition GetRoom(string roomId)
        {
            return rooms.Find(room => room != null && room.id == roomId);
        }

        public DungeonPuzzleDefinition GetPuzzle(string puzzleId)
        {
            return puzzles.Find(puzzle => puzzle != null && puzzle.id == puzzleId);
        }

        public DungeonCheckpointDefinition GetCheckpoint(string checkpointId)
        {
            return checkpoints.Find(checkpoint => checkpoint != null && checkpoint.id == checkpointId);
        }
    }

    public static class DungeonCatalog
    {
        public const string GlasswaterId = "dungeon.morrowfen.glasswater_sluice";
        public const string GlasswaterQuestId = "glasswater";
        public const string GlasswaterBossId = "boss.glasswater_regent";
        public const string GlasswaterRewardId = "charm.sluiceglass";

        public const string GlasswaterEntranceRoomId = "dungeon.glasswater.room.threshold";
        public const string GlasswaterWeirRoomId = "dungeon.glasswater.room.counterflow_weir";
        public const string GlasswaterCombatRoomId = "dungeon.glasswater.room.brood_cistern";
        public const string GlasswaterCheckpointRoomId = "dungeon.glasswater.room.keepers_landing";
        public const string GlasswaterTraversalRoomId = "dungeon.glasswater.room.spillway_spine";
        public const string GlasswaterBalanceRoomId = "dungeon.glasswater.room.tideglass_balance";
        public const string GlasswaterBossRoomId = "dungeon.glasswater.room.mirror_reservoir";
        public const string GlasswaterExitRoomId = "dungeon.glasswater.room.outflow_loom";

        public const string CounterflowPuzzleId = "dungeon.glasswater.puzzle.counterflow_order";
        public const string TideglassPuzzleId = "dungeon.glasswater.puzzle.tideglass_balance";
        public const string KeepersCheckpointId = "dungeon.glasswater.checkpoint.keepers_landing";

        public static readonly IReadOnlyList<ModularDungeonDefinition> All =
            new List<ModularDungeonDefinition>
            {
                CreateGlasswaterSluice()
            };

        public static ModularDungeonDefinition Get(string id)
        {
            foreach (var definition in All)
            {
                if (definition != null && definition.id == id)
                {
                    return definition;
                }
            }

            return null;
        }

        private static ModularDungeonDefinition CreateGlasswaterSluice()
        {
            var definition = new ModularDungeonDefinition
            {
                id = GlasswaterId,
                displayName = "Glasswater Sluice",
                region = "Morrowfen",
                questId = GlasswaterQuestId,
                requiredQuestId = "fenlight",
                lockedMessage = "The drowned intake will not hold until Fenlight's route is fully restored.",
                gatePosition = new Vector3(232f, 0.9f, -58f),
                returnPosition = new Vector3(227f, 0.1f, -58f),
                interiorOrigin = new Vector3(520f, 0f, 320f),
                localBoundsCenter = new Vector3(0f, 4f, 103f),
                localBoundsSize = new Vector3(54f, 20f, 232f),
                entranceRoomId = GlasswaterEntranceRoomId,
                bossRoomId = GlasswaterBossRoomId,
                exitRoomId = GlasswaterExitRoomId,
                bossId = GlasswaterBossId,
                rewardEquipmentId = GlasswaterRewardId,
                favorReward = 54,
                experienceReward = 190,
                resourceRewardKind = ResourceKind.Chimesalt,
                resourceRewardAmount = 6,
                accentColor = new Color(0.31f, 0.86f, 0.8f),
                rendererBudget = 190,
                colliderBudget = 96,
                addedLightBudget = 0
            };

            definition.rooms.AddRange(new[]
            {
                Room(
                    GlasswaterEntranceRoomId,
                    "Drowned Threshold",
                    ModularDungeonRoomKind.Entrance,
                    new Vector3(0f, 0f, 0f),
                    new Vector3(24f, 8f, 20f),
                    new Vector3(0f, 1.1f, -6f)),
                Room(
                    GlasswaterWeirRoomId,
                    "Counterflow Weir",
                    ModularDungeonRoomKind.Puzzle,
                    new Vector3(0f, 0f, 23f),
                    new Vector3(28f, 8f, 26f),
                    new Vector3(0f, 1.1f, 14f),
                    CounterflowPuzzleId),
                CombatRoom(),
                Room(
                    GlasswaterCheckpointRoomId,
                    "Keeper's Landing",
                    ModularDungeonRoomKind.Checkpoint,
                    new Vector3(0f, 0f, 71f),
                    new Vector3(22f, 8f, 18f),
                    new Vector3(0f, 1.1f, 67f),
                    checkpointId: KeepersCheckpointId),
                Room(
                    GlasswaterTraversalRoomId,
                    "Spillway Spine",
                    ModularDungeonRoomKind.Traversal,
                    new Vector3(0f, 0f, 99f),
                    new Vector3(24f, 12f, 38f),
                    new Vector3(0f, 1.1f, 84f)),
                Room(
                    GlasswaterBalanceRoomId,
                    "Tideglass Balance",
                    ModularDungeonRoomKind.Puzzle,
                    new Vector3(0f, 0f, 133f),
                    new Vector3(30f, 9f, 30f),
                    new Vector3(0f, 1.1f, 122f),
                    TideglassPuzzleId),
                Room(
                    GlasswaterBossRoomId,
                    "Mirror Reservoir",
                    ModularDungeonRoomKind.Boss,
                    new Vector3(0f, 0f, 170f),
                    new Vector3(42f, 10f, 44f),
                    new Vector3(0f, 1.1f, 153f)),
                Room(
                    GlasswaterExitRoomId,
                    "Outflow Loom",
                    ModularDungeonRoomKind.Exit,
                    new Vector3(0f, 0f, 204f),
                    new Vector3(24f, 8f, 24f),
                    new Vector3(0f, 1.1f, 197f))
            });

            definition.connections.AddRange(new[]
            {
                Connection(
                    "dungeon.glasswater.connection.threshold_weir",
                    GlasswaterEntranceRoomId,
                    GlasswaterWeirRoomId,
                    10f,
                    28f),
                Connection(
                    "dungeon.glasswater.connection.weir_cistern",
                    GlasswaterWeirRoomId,
                    GlasswaterCombatRoomId,
                    36f,
                    34f),
                Connection(
                    "dungeon.glasswater.connection.cistern_landing",
                    GlasswaterCombatRoomId,
                    GlasswaterCheckpointRoomId,
                    62f,
                    34f),
                Connection(
                    "dungeon.glasswater.connection.landing_spine",
                    GlasswaterCheckpointRoomId,
                    GlasswaterTraversalRoomId,
                    80f,
                    24f),
                Connection(
                    "dungeon.glasswater.connection.spine_balance",
                    GlasswaterTraversalRoomId,
                    GlasswaterBalanceRoomId,
                    118f,
                    30f),
                Connection(
                    "dungeon.glasswater.connection.balance_reservoir",
                    GlasswaterBalanceRoomId,
                    GlasswaterBossRoomId,
                    148f,
                    42f),
                Connection(
                    "dungeon.glasswater.connection.reservoir_outflow",
                    GlasswaterBossRoomId,
                    GlasswaterExitRoomId,
                    192f,
                    42f)
            });

            definition.puzzles.Add(CreateCounterflowPuzzle());
            definition.puzzles.Add(CreateTideglassPuzzle());
            definition.checkpoints.Add(new DungeonCheckpointDefinition
            {
                id = KeepersCheckpointId,
                roomId = GlasswaterCheckpointRoomId,
                displayName = "Keeper's Threadglass",
                localPosition = new Vector3(0f, 1.3f, 71f)
            });
            return definition;
        }

        private static DungeonRoomDefinition CombatRoom()
        {
            var room = Room(
                GlasswaterCombatRoomId,
                "Brood Cistern",
                ModularDungeonRoomKind.Combat,
                new Vector3(0f, 0f, 49f),
                new Vector3(34f, 9f, 26f),
                new Vector3(0f, 1.1f, 40f));
            room.waves.Add(new DungeonWaveDefinition
            {
                id = "dungeon.glasswater.wave.cistern_rakers",
                enemyIds = new[]
                {
                    "enemy.fen_spitter",
                    "enemy.mossglass_stalker",
                    "enemy.mossglass_stalker",
                    "enemy.vault_wisp"
                }
            });
            room.waves.Add(new DungeonWaveDefinition
            {
                id = "dungeon.glasswater.wave.cistern_siltcallers",
                enemyIds = new[]
                {
                    "enemy.fen_siltcaller",
                    "enemy.fen_spitter",
                    "enemy.vault_wisp",
                    "enemy.mossglass_stalker",
                    "enemy.fen_spitter"
                }
            });
            return room;
        }

        private static DungeonPuzzleDefinition CreateCounterflowPuzzle()
        {
            return new DungeonPuzzleDefinition
            {
                id = CounterflowPuzzleId,
                roomId = GlasswaterWeirRoomId,
                displayName = "Counterflow Weir",
                kind = ModularDungeonPuzzleKind.OrderedSequence,
                clue = "The basin drinks first. Reedwater follows. The outflow bell answers last.",
                solution = new[] { 1, 0, 2 },
                mechanisms = new List<DungeonMechanismDefinition>
                {
                    Mechanism(
                        "dungeon.glasswater.mechanism.reed_intake",
                        "Reed Intake · chevron",
                        new Vector3(-7f, 1.5f, 24f)),
                    Mechanism(
                        "dungeon.glasswater.mechanism.glass_basin",
                        "Glass Basin · ring",
                        new Vector3(0f, 1.5f, 24f)),
                    Mechanism(
                        "dungeon.glasswater.mechanism.outflow_bell",
                        "Outflow Bell · bars",
                        new Vector3(7f, 1.5f, 24f))
                }
            };
        }

        private static DungeonPuzzleDefinition CreateTideglassPuzzle()
        {
            var stateLabels = new[] { "LOW", "MID", "HIGH" };
            return new DungeonPuzzleDefinition
            {
                id = TideglassPuzzleId,
                roomId = GlasswaterBalanceRoomId,
                displayName = "Tideglass Balance",
                kind = ModularDungeonPuzzleKind.StateMatch,
                clue = "Raise the left tideglass HIGH, drain the center LOW, and hold the right at MID.",
                solution = new[] { 2, 0, 1 },
                mechanisms = new List<DungeonMechanismDefinition>
                {
                    Mechanism(
                        "dungeon.glasswater.mechanism.left_tideglass",
                        "Left Tideglass · triangle",
                        new Vector3(-8f, 1.5f, 134f),
                        3,
                        stateLabels),
                    Mechanism(
                        "dungeon.glasswater.mechanism.center_tideglass",
                        "Center Tideglass · circle",
                        new Vector3(0f, 1.5f, 134f),
                        3,
                        stateLabels),
                    Mechanism(
                        "dungeon.glasswater.mechanism.right_tideglass",
                        "Right Tideglass · square",
                        new Vector3(8f, 1.5f, 134f),
                        3,
                        stateLabels)
                }
            };
        }

        private static DungeonRoomDefinition Room(
            string id,
            string displayName,
            ModularDungeonRoomKind kind,
            Vector3 center,
            Vector3 size,
            Vector3 safePosition,
            string puzzleId = null,
            string checkpointId = null)
        {
            return new DungeonRoomDefinition
            {
                id = id,
                displayName = displayName,
                kind = kind,
                localCenter = center,
                size = size,
                safeLocalPosition = safePosition,
                puzzleId = puzzleId ?? string.Empty,
                checkpointId = checkpointId ?? string.Empty
            };
        }

        private static DungeonConnectionDefinition Connection(
            string id,
            string fromRoomId,
            string toRoomId,
            float localZ,
            float wallWidth)
        {
            return new DungeonConnectionDefinition
            {
                id = id,
                fromRoomId = fromRoomId,
                toRoomId = toRoomId,
                doorLocalPosition = new Vector3(0f, 2.75f, localZ),
                doorScale = new Vector3(6f, 5.5f, 0.8f),
                wallWidth = wallWidth
            };
        }

        private static DungeonMechanismDefinition Mechanism(
            string id,
            string displayName,
            Vector3 localPosition,
            int stateCount = 1,
            string[] stateLabels = null)
        {
            return new DungeonMechanismDefinition
            {
                id = id,
                displayName = displayName,
                localPosition = localPosition,
                stateCount = Mathf.Max(1, stateCount),
                stateLabels = stateLabels ?? Array.Empty<string>()
            };
        }
    }

    [Serializable]
    public sealed class DungeonPuzzleSaveData
    {
        public string id;
        public int[] values = Array.Empty<int>();
        public bool solved;
    }

    [Serializable]
    public sealed class DungeonSaveData
    {
        public string id;
        public string currentRoomId;
        public string checkpointId;
        public List<string> completedRoomIds = new List<string>();
        public List<string> solvedPuzzleIds = new List<string>();
        public List<DungeonPuzzleSaveData> puzzles = new List<DungeonPuzzleSaveData>();
        public string activeCombatRoomId;
        public int activeWaveIndex = -1;
        public List<string> remainingEnemyIds = new List<string>();
        public bool bossDefeated;
        public bool completed;
        public bool rewardGranted;
        public int completionCount;
    }

    public static class DungeonSaveNormalization
    {
        public static void NormalizeInPlace(List<DungeonSaveData> dungeons)
        {
            if (dungeons == null)
            {
                return;
            }

            dungeons.RemoveAll(dungeon => dungeon == null);
            var byId = new Dictionary<string, DungeonSaveData>(StringComparer.Ordinal);
            for (var index = 0; index < dungeons.Count; index++)
            {
                var dungeon = dungeons[index];
                NormalizeStructural(dungeon);
                if (string.IsNullOrWhiteSpace(dungeon.id))
                {
                    dungeons.RemoveAt(index--);
                    continue;
                }

                if (!byId.TryGetValue(dungeon.id, out var existing))
                {
                    byId.Add(dungeon.id, dungeon);
                    continue;
                }

                Merge(existing, dungeon);
                dungeons.RemoveAt(index--);
            }
        }

        public static void NormalizeStructural(DungeonSaveData dungeon)
        {
            if (dungeon == null)
            {
                return;
            }

            dungeon.id ??= string.Empty;
            dungeon.currentRoomId ??= string.Empty;
            dungeon.checkpointId ??= string.Empty;
            dungeon.activeCombatRoomId ??= string.Empty;
            dungeon.completedRoomIds ??= new List<string>();
            dungeon.solvedPuzzleIds ??= new List<string>();
            dungeon.puzzles ??= new List<DungeonPuzzleSaveData>();
            dungeon.remainingEnemyIds ??= new List<string>();

            NormalizeIdList(dungeon.completedRoomIds);
            NormalizeIdList(dungeon.solvedPuzzleIds);
            dungeon.remainingEnemyIds.RemoveAll(string.IsNullOrWhiteSpace);
            dungeon.puzzles.RemoveAll(puzzle => puzzle == null);
            var puzzleIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < dungeon.puzzles.Count; index++)
            {
                var puzzle = dungeon.puzzles[index];
                puzzle.id ??= string.Empty;
                puzzle.values ??= Array.Empty<int>();
                if (string.IsNullOrWhiteSpace(puzzle.id) || !puzzleIds.Add(puzzle.id))
                {
                    dungeon.puzzles.RemoveAt(index--);
                }
            }

            dungeon.activeWaveIndex = Mathf.Max(-1, dungeon.activeWaveIndex);
            dungeon.completionCount = Mathf.Max(0, dungeon.completionCount);
            if (dungeon.rewardGranted)
            {
                dungeon.completed = true;
                dungeon.bossDefeated = true;
            }

            if (dungeon.completed)
            {
                dungeon.bossDefeated = true;
                dungeon.completionCount = Mathf.Max(1, dungeon.completionCount);
            }
        }

        private static void Merge(DungeonSaveData target, DungeonSaveData source)
        {
            UnionInto(target.completedRoomIds, source.completedRoomIds);
            UnionInto(target.solvedPuzzleIds, source.solvedPuzzleIds);
            target.bossDefeated |= source.bossDefeated;
            target.completed |= source.completed;
            target.rewardGranted |= source.rewardGranted;
            target.completionCount = Mathf.Max(target.completionCount, source.completionCount);

            if (string.IsNullOrEmpty(target.currentRoomId))
            {
                target.currentRoomId = source.currentRoomId;
            }

            if (string.IsNullOrEmpty(target.checkpointId))
            {
                target.checkpointId = source.checkpointId;
            }

            foreach (var sourcePuzzle in source.puzzles)
            {
                var targetPuzzle = target.puzzles.Find(puzzle => puzzle.id == sourcePuzzle.id);
                if (targetPuzzle == null)
                {
                    target.puzzles.Add(sourcePuzzle);
                    continue;
                }

                targetPuzzle.solved |= sourcePuzzle.solved;
                if (targetPuzzle.values.Length == 0 && sourcePuzzle.values.Length > 0)
                {
                    targetPuzzle.values = sourcePuzzle.values;
                }
            }

            if (string.IsNullOrEmpty(target.activeCombatRoomId) &&
                !string.IsNullOrEmpty(source.activeCombatRoomId))
            {
                target.activeCombatRoomId = source.activeCombatRoomId;
                target.activeWaveIndex = source.activeWaveIndex;
                target.remainingEnemyIds.Clear();
                target.remainingEnemyIds.AddRange(source.remainingEnemyIds);
            }

            NormalizeStructural(target);
        }

        private static void NormalizeIdList(List<string> ids)
        {
            ids.RemoveAll(string.IsNullOrWhiteSpace);
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < ids.Count; index++)
            {
                if (!unique.Add(ids[index]))
                {
                    ids.RemoveAt(index--);
                }
            }
        }

        private static void UnionInto(List<string> target, IEnumerable<string> source)
        {
            var existing = new HashSet<string>(target, StringComparer.Ordinal);
            foreach (var id in source)
            {
                if (!string.IsNullOrWhiteSpace(id) && existing.Add(id))
                {
                    target.Add(id);
                }
            }
        }
    }

    public static class DungeonProgressRules
    {
        public static DungeonSaveData CreateFresh(ModularDungeonDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            var data = new DungeonSaveData
            {
                id = definition.id,
                currentRoomId = definition.entranceRoomId
            };
            foreach (var puzzle in definition.puzzles)
            {
                data.puzzles.Add(CreatePuzzleState(puzzle));
            }

            Normalize(definition, data);
            return data;
        }

        public static bool Normalize(ModularDungeonDefinition definition, DungeonSaveData data)
        {
            if (definition == null || data == null)
            {
                return false;
            }

            var before = JsonUtility.ToJson(data);
            DungeonSaveNormalization.NormalizeStructural(data);
            data.id = definition.id;

            var roomIds = GetIds(definition.rooms, room => room.id);
            var puzzleIds = GetIds(definition.puzzles, puzzle => puzzle.id);
            var checkpointIds = GetIds(definition.checkpoints, checkpoint => checkpoint.id);
            RemoveUnknownIds(data.completedRoomIds, roomIds);
            RemoveUnknownIds(data.solvedPuzzleIds, puzzleIds);

            data.puzzles.RemoveAll(puzzle => !puzzleIds.Contains(puzzle.id));
            foreach (var puzzle in definition.puzzles)
            {
                if (puzzle == null)
                {
                    continue;
                }

                var puzzleState = data.puzzles.Find(state => state.id == puzzle.id);
                if (puzzleState == null)
                {
                    puzzleState = CreatePuzzleState(puzzle);
                    data.puzzles.Add(puzzleState);
                }

                NormalizePuzzleState(puzzle, puzzleState);
                if (data.solvedPuzzleIds.Contains(puzzle.id))
                {
                    puzzleState.solved = true;
                }

                if (puzzleState.solved)
                {
                    AddUnique(data.solvedPuzzleIds, puzzle.id);
                    AddUnique(data.completedRoomIds, puzzle.roomId);
                }
            }

            if (!checkpointIds.Contains(data.checkpointId))
            {
                data.checkpointId = string.Empty;
            }
            else
            {
                var checkpoint = definition.GetCheckpoint(data.checkpointId);
                if (checkpoint != null)
                {
                    AddUnique(data.completedRoomIds, checkpoint.roomId);
                }
            }

            if (data.rewardGranted)
            {
                data.completed = true;
                data.bossDefeated = true;
            }

            if (data.bossDefeated)
            {
                data.completed = true;
            }

            if (data.completed)
            {
                data.bossDefeated = true;
                data.completionCount = Mathf.Max(1, data.completionCount);
                foreach (var room in definition.rooms)
                {
                    if (room != null)
                    {
                        AddUnique(data.completedRoomIds, room.id);
                    }
                }

                foreach (var puzzle in definition.puzzles)
                {
                    if (puzzle == null)
                    {
                        continue;
                    }

                    AddUnique(data.solvedPuzzleIds, puzzle.id);
                    var puzzleState = data.puzzles.Find(state => state.id == puzzle.id);
                    if (puzzleState != null)
                    {
                        puzzleState.solved = true;
                    }
                }
            }

            NormalizeCombatProgress(definition, data);
            if (!roomIds.Contains(data.currentRoomId) ||
                !IsRoomAccessible(definition, data, data.currentRoomId))
            {
                data.currentRoomId = GetRecoveryRoomId(definition, data);
            }

            return before != JsonUtility.ToJson(data);
        }

        public static bool IsRoomAccessible(
            ModularDungeonDefinition definition,
            DungeonSaveData data,
            string roomId)
        {
            if (definition == null || data == null || string.IsNullOrEmpty(roomId))
            {
                return false;
            }

            if (roomId == definition.entranceRoomId || data.completed)
            {
                return definition.GetRoom(roomId) != null;
            }

            foreach (var connection in definition.connections)
            {
                if (connection != null &&
                    connection.toRoomId == roomId &&
                    data.completedRoomIds.Contains(connection.fromRoomId))
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetRecoveryRoomId(
            ModularDungeonDefinition definition,
            DungeonSaveData data)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            if (data != null && !string.IsNullOrEmpty(data.checkpointId))
            {
                var checkpoint = definition.GetCheckpoint(data.checkpointId);
                if (checkpoint != null)
                {
                    return checkpoint.roomId;
                }
            }

            return definition.entranceRoomId;
        }

        public static int CalculateQuestProgress(
            ModularDungeonDefinition definition,
            DungeonSaveData data)
        {
            if (definition == null || data == null)
            {
                return 0;
            }

            var progress = 0;
            foreach (var puzzle in definition.puzzles)
            {
                if (puzzle != null && data.solvedPuzzleIds.Contains(puzzle.id))
                {
                    progress++;
                }
            }

            foreach (var room in definition.rooms)
            {
                if (room != null &&
                    room.kind == ModularDungeonRoomKind.Combat &&
                    data.completedRoomIds.Contains(room.id))
                {
                    progress++;
                }
            }

            if (data.bossDefeated)
            {
                progress++;
            }

            return progress;
        }

        public static bool TryClaimReward(DungeonSaveData data)
        {
            if (data == null ||
                !data.completed ||
                !data.bossDefeated ||
                data.rewardGranted)
            {
                return false;
            }

            data.rewardGranted = true;
            data.completionCount = Mathf.Max(1, data.completionCount);
            return true;
        }

        private static DungeonPuzzleSaveData CreatePuzzleState(DungeonPuzzleDefinition puzzle)
        {
            var valueCount = puzzle != null &&
                             puzzle.kind == ModularDungeonPuzzleKind.StateMatch
                ? puzzle.mechanisms.Count
                : 1;
            return new DungeonPuzzleSaveData
            {
                id = puzzle != null ? puzzle.id : string.Empty,
                values = new int[Mathf.Max(1, valueCount)]
            };
        }

        private static void NormalizePuzzleState(
            DungeonPuzzleDefinition definition,
            DungeonPuzzleSaveData state)
        {
            if (definition.kind == ModularDungeonPuzzleKind.OrderedSequence)
            {
                var step = state.values != null && state.values.Length > 0
                    ? state.values[0]
                    : 0;
                step = Mathf.Clamp(step, 0, definition.solution.Length);
                state.values = new[] { step };
                state.solved |= definition.solution.Length > 0 && step >= definition.solution.Length;
                return;
            }

            var values = new int[definition.mechanisms.Count];
            var matches = definition.solution.Length == values.Length && values.Length > 0;
            for (var index = 0; index < values.Length; index++)
            {
                var mechanism = definition.mechanisms[index];
                var stateCount = Mathf.Max(1, mechanism != null ? mechanism.stateCount : 1);
                var savedValue = state.values != null && index < state.values.Length
                    ? state.values[index]
                    : 0;
                values[index] = PositiveModulo(savedValue, stateCount);
                matches &= values[index] == definition.solution[index];
            }

            state.values = values;
            state.solved |= matches;
        }

        private static void NormalizeCombatProgress(
            ModularDungeonDefinition definition,
            DungeonSaveData data)
        {
            if (data.completed ||
                string.IsNullOrEmpty(data.activeCombatRoomId) ||
                data.completedRoomIds.Contains(data.activeCombatRoomId))
            {
                ClearCombatProgress(data);
                return;
            }

            var room = definition.GetRoom(data.activeCombatRoomId);
            if (room == null ||
                room.kind != ModularDungeonRoomKind.Combat ||
                room.waves == null ||
                room.waves.Count == 0)
            {
                ClearCombatProgress(data);
                return;
            }

            data.activeWaveIndex = Mathf.Clamp(data.activeWaveIndex, 0, room.waves.Count - 1);
            var roster = room.waves[data.activeWaveIndex].enemyIds ?? Array.Empty<string>();
            var remainingCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var enemyId in roster)
            {
                if (string.IsNullOrWhiteSpace(enemyId))
                {
                    continue;
                }

                remainingCounts.TryGetValue(enemyId, out var count);
                remainingCounts[enemyId] = count + 1;
            }

            var normalizedRemaining = new List<string>();
            foreach (var enemyId in data.remainingEnemyIds)
            {
                if (string.IsNullOrWhiteSpace(enemyId) ||
                    !remainingCounts.TryGetValue(enemyId, out var count) ||
                    count <= 0)
                {
                    continue;
                }

                normalizedRemaining.Add(enemyId);
                remainingCounts[enemyId] = count - 1;
            }

            if (normalizedRemaining.Count == 0)
            {
                normalizedRemaining.AddRange(roster);
            }

            data.remainingEnemyIds.Clear();
            data.remainingEnemyIds.AddRange(normalizedRemaining);
        }

        private static void ClearCombatProgress(DungeonSaveData data)
        {
            data.activeCombatRoomId = string.Empty;
            data.activeWaveIndex = -1;
            data.remainingEnemyIds.Clear();
        }

        private static HashSet<string> GetIds<T>(
            IEnumerable<T> definitions,
            Func<T, string> selector)
            where T : class
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (definitions == null)
            {
                return ids;
            }

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                var id = selector(definition);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        private static void RemoveUnknownIds(List<string> ids, HashSet<string> validIds)
        {
            ids.RemoveAll(id => !validIds.Contains(id));
        }

        private static void AddUnique(List<string> ids, string id)
        {
            if (!string.IsNullOrWhiteSpace(id) && !ids.Contains(id))
            {
                ids.Add(id);
            }
        }

        private static int PositiveModulo(int value, int modulus)
        {
            if (modulus <= 1)
            {
                return 0;
            }

            var result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
