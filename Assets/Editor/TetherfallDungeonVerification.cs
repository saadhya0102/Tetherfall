using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallDungeonVerification
    {
        private const string MenuPath = "Tetherfall/Verify Modular Dungeons";
        private const float MaximumCoordinate = 10000f;
        private static readonly List<string> Failures = new List<string>();
        private static int assertionCount;
        private static int sectionCount;

        [MenuItem(MenuPath, false, 2188)]
        public static void VerifyModularDungeons()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Tetherfall dungeon verification is edit-mode only. Stop Play Mode and run it again.");
            }

            VerifyForFullGame();
        }

        [MenuItem(MenuPath, true, 2188)]
        private static bool CanVerifyModularDungeons()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        public static void VerifyForFullGame()
        {
            Failures.Clear();
            assertionCount = 0;
            sectionCount = 0;
            Run("catalog and stable IDs", VerifyCatalogAndStableIds);
            Run("graph integrity", VerifyGraphIntegrity);
            Run("referential integrity", VerifyReferentialIntegrity);
            Run("progression and reload normalization", VerifyProgressionNormalization);
            Run("one-shot rewards", VerifyOneShotRewards);
            Run("geometry and effects bounds", VerifyBoundsAndBudgets);
            Run("runtime wiring", VerifyRuntimeWiring);
            Complete();
        }

        private static void VerifyCatalogAndStableIds()
        {
            Check(DungeonCatalog.All != null, "DungeonCatalog.All is null.");
            if (DungeonCatalog.All == null)
            {
                return;
            }

            Check(DungeonCatalog.All.Count > 0, "DungeonCatalog contains no dungeon definitions.");
            var globalIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var dungeon in DungeonCatalog.All)
            {
                Check(dungeon != null, "DungeonCatalog contains a null definition.");
                if (dungeon == null)
                {
                    continue;
                }

                RegisterId(globalIds, dungeon.id, "dungeon");
                Check(
                    dungeon.id.StartsWith("dungeon.", StringComparison.Ordinal),
                    $"Dungeon ID '{dungeon.id}' must use the 'dungeon.' namespace.");
                RequireText(dungeon.displayName, $"Dungeon '{dungeon.id}' displayName");
                RequireText(dungeon.region, $"Dungeon '{dungeon.id}' region");
                RequireText(dungeon.questId, $"Dungeon '{dungeon.id}' questId");

                foreach (var room in dungeon.rooms)
                {
                    Check(room != null, $"Dungeon '{dungeon.id}' has a null room.");
                    if (room != null)
                    {
                        RegisterId(globalIds, room.id, $"dungeon '{dungeon.id}' room");
                    }
                }

                foreach (var connection in dungeon.connections)
                {
                    Check(connection != null, $"Dungeon '{dungeon.id}' has a null connection.");
                    if (connection != null)
                    {
                        RegisterId(globalIds, connection.id, $"dungeon '{dungeon.id}' connection");
                    }
                }

                foreach (var puzzle in dungeon.puzzles)
                {
                    Check(puzzle != null, $"Dungeon '{dungeon.id}' has a null puzzle.");
                    if (puzzle == null)
                    {
                        continue;
                    }

                    RegisterId(globalIds, puzzle.id, $"dungeon '{dungeon.id}' puzzle");
                    foreach (var mechanism in puzzle.mechanisms)
                    {
                        Check(
                            mechanism != null,
                            $"Puzzle '{puzzle.id}' has a null mechanism.");
                        if (mechanism != null)
                        {
                            RegisterId(
                                globalIds,
                                mechanism.id,
                                $"puzzle '{puzzle.id}' mechanism");
                        }
                    }
                }

                foreach (var checkpoint in dungeon.checkpoints)
                {
                    Check(
                        checkpoint != null,
                        $"Dungeon '{dungeon.id}' has a null checkpoint.");
                    if (checkpoint != null)
                    {
                        RegisterId(
                            globalIds,
                            checkpoint.id,
                            $"dungeon '{dungeon.id}' checkpoint");
                    }
                }

                foreach (var room in dungeon.rooms)
                {
                    if (room?.waves == null)
                    {
                        continue;
                    }

                    foreach (var wave in room.waves)
                    {
                        Check(wave != null, $"Combat room '{room.id}' has a null wave.");
                        if (wave != null)
                        {
                            RegisterId(globalIds, wave.id, $"combat room '{room.id}' wave");
                        }
                    }
                }
            }

            var glasswater = DungeonCatalog.Get(DungeonCatalog.GlasswaterId);
            Check(glasswater != null, "Glasswater Sluice is missing from DungeonCatalog.");
            Check(
                glasswater != null &&
                ReferenceEquals(glasswater, DungeonCatalog.All[0]),
                "DungeonCatalog.Get does not return the canonical Glasswater definition.");
        }

        private static void VerifyGraphIntegrity()
        {
            foreach (var dungeon in DungeonCatalog.All)
            {
                if (dungeon == null)
                {
                    continue;
                }

                Check(
                    dungeon.rooms != null && dungeon.rooms.Count >= 7,
                    $"Dungeon '{dungeon.id}' needs at least seven authored room nodes.");
                Check(
                    dungeon.connections != null && dungeon.connections.Count > 0,
                    $"Dungeon '{dungeon.id}' has no graph connections.");
                if (dungeon.rooms == null || dungeon.connections == null)
                {
                    continue;
                }

                var rooms = new Dictionary<string, DungeonRoomDefinition>(StringComparer.Ordinal);
                foreach (var room in dungeon.rooms)
                {
                    if (room != null && !string.IsNullOrWhiteSpace(room.id))
                    {
                        rooms[room.id] = room;
                    }
                }

                Check(
                    rooms.ContainsKey(dungeon.entranceRoomId),
                    $"Dungeon '{dungeon.id}' entranceRoomId '{dungeon.entranceRoomId}' is missing.");
                Check(
                    rooms.ContainsKey(dungeon.bossRoomId),
                    $"Dungeon '{dungeon.id}' bossRoomId '{dungeon.bossRoomId}' is missing.");
                Check(
                    rooms.ContainsKey(dungeon.exitRoomId),
                    $"Dungeon '{dungeon.id}' exitRoomId '{dungeon.exitRoomId}' is missing.");

                var outgoing = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                foreach (var roomId in rooms.Keys)
                {
                    outgoing[roomId] = new List<string>();
                }

                var edgePairs = new HashSet<string>(StringComparer.Ordinal);
                foreach (var connection in dungeon.connections)
                {
                    if (connection == null)
                    {
                        continue;
                    }

                    Check(
                        rooms.ContainsKey(connection.fromRoomId),
                        $"Connection '{connection.id}' references missing fromRoomId '{connection.fromRoomId}'.");
                    Check(
                        rooms.ContainsKey(connection.toRoomId),
                        $"Connection '{connection.id}' references missing toRoomId '{connection.toRoomId}'.");
                    Check(
                        connection.fromRoomId != connection.toRoomId,
                        $"Connection '{connection.id}' is a self-loop.");
                    Check(
                        edgePairs.Add(connection.fromRoomId + "->" + connection.toRoomId),
                        $"Dungeon '{dungeon.id}' duplicates edge " +
                        $"'{connection.fromRoomId}->{connection.toRoomId}'.");
                    if (outgoing.TryGetValue(connection.fromRoomId, out var targets) &&
                        rooms.ContainsKey(connection.toRoomId))
                    {
                        targets.Add(connection.toRoomId);
                    }
                }

                var reachable = new HashSet<string>(StringComparer.Ordinal);
                var queue = new Queue<string>();
                if (rooms.ContainsKey(dungeon.entranceRoomId))
                {
                    queue.Enqueue(dungeon.entranceRoomId);
                    reachable.Add(dungeon.entranceRoomId);
                }

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    foreach (var target in outgoing[current])
                    {
                        if (reachable.Add(target))
                        {
                            queue.Enqueue(target);
                        }
                    }
                }

                foreach (var roomId in rooms.Keys)
                {
                    Check(
                        reachable.Contains(roomId),
                        $"Dungeon '{dungeon.id}' room '{roomId}' is unreachable from its entrance.");
                }

                Check(
                    reachable.Contains(dungeon.bossRoomId),
                    $"Dungeon '{dungeon.id}' boss room is not reachable.");
                Check(
                    reachable.Contains(dungeon.exitRoomId),
                    $"Dungeon '{dungeon.id}' exit room is not reachable.");
                Check(
                    CountRooms(dungeon, ModularDungeonRoomKind.Entrance) == 1,
                    $"Dungeon '{dungeon.id}' must have exactly one entrance room.");
                Check(
                    CountRooms(dungeon, ModularDungeonRoomKind.Puzzle) >= 2,
                    $"Dungeon '{dungeon.id}' must have at least two puzzle rooms.");
                Check(
                    CountRooms(dungeon, ModularDungeonRoomKind.Combat) >= 1,
                    $"Dungeon '{dungeon.id}' needs a combat/swarm room.");
                Check(
                    CountRooms(dungeon, ModularDungeonRoomKind.Checkpoint) >= 1,
                    $"Dungeon '{dungeon.id}' needs a checkpoint room.");
                Check(
                    CountRooms(dungeon, ModularDungeonRoomKind.Traversal) >= 1,
                    $"Dungeon '{dungeon.id}' needs a traversal room.");
                Check(
                    CountRooms(dungeon, ModularDungeonRoomKind.Boss) == 1,
                    $"Dungeon '{dungeon.id}' must have exactly one boss room.");
                Check(
                    CountRooms(dungeon, ModularDungeonRoomKind.Exit) == 1,
                    $"Dungeon '{dungeon.id}' must have exactly one exit room.");
            }
        }

        private static void VerifyReferentialIntegrity()
        {
            var enemyDefinitions = GetPrivateStaticField<Dictionary<string, EnemyDefinition>>(
                typeof(EnemyCatalog),
                "Definitions");
            var equipment = EquipmentCatalog.CreateAll();
            var quests = QuestCatalog.CreateAll();

            foreach (var dungeon in DungeonCatalog.All)
            {
                if (dungeon == null)
                {
                    continue;
                }

                var boss = BossCatalog.Get(dungeon.bossId);
                Check(
                    boss != null,
                    $"Dungeon '{dungeon.id}' references missing boss '{dungeon.bossId}'.");
                Check(
                    boss != null && TetherfallBossArtOverhaul.Supports(boss.id),
                    $"Dungeon boss '{dungeon.bossId}' lacks an authored reusable boss silhouette.");
                Check(
                    equipment.Exists(item => item.id == dungeon.rewardEquipmentId),
                    $"Dungeon '{dungeon.id}' references missing reward equipment " +
                    $"'{dungeon.rewardEquipmentId}'.");
                var quest = quests.Find(item => item.id == dungeon.questId);
                Check(
                    quest != null,
                    $"Dungeon '{dungeon.id}' references missing quest '{dungeon.questId}'.");
                Check(
                    string.IsNullOrEmpty(dungeon.requiredQuestId) ||
                    quests.Exists(item => item.id == dungeon.requiredQuestId),
                    $"Dungeon '{dungeon.id}' references missing unlock quest " +
                    $"'{dungeon.requiredQuestId}'.");

                var expectedQuestSteps = dungeon.puzzles.Count +
                                         CountRooms(dungeon, ModularDungeonRoomKind.Combat) +
                                         1;
                Check(
                    quest != null && quest.required == expectedQuestSteps,
                    $"Dungeon quest '{dungeon.questId}' requires {quest?.required ?? 0}; " +
                    $"expected {expectedQuestSteps} puzzle/combat/boss beats.");

                foreach (var room in dungeon.rooms)
                {
                    if (room == null)
                    {
                        continue;
                    }

                    if (room.kind == ModularDungeonRoomKind.Puzzle)
                    {
                        var puzzle = dungeon.GetPuzzle(room.puzzleId);
                        Check(
                            puzzle != null && puzzle.roomId == room.id,
                            $"Puzzle room '{room.id}' has invalid puzzle reference '{room.puzzleId}'.");
                    }
                    else
                    {
                        Check(
                            string.IsNullOrEmpty(room.puzzleId),
                            $"Non-puzzle room '{room.id}' unexpectedly references puzzle '{room.puzzleId}'.");
                    }

                    if (room.kind == ModularDungeonRoomKind.Combat)
                    {
                        Check(
                            room.waves != null && room.waves.Count >= 2,
                            $"Combat room '{room.id}' needs at least two readable waves.");
                        if (room.waves != null)
                        {
                            foreach (var wave in room.waves)
                            {
                                Check(
                                    wave != null &&
                                    wave.enemyIds != null &&
                                    wave.enemyIds.Length >= 3 &&
                                    wave.enemyIds.Length <= 6,
                                    $"Combat wave '{wave?.id}' must contain 3-6 enemies.");
                                if (wave?.enemyIds == null)
                                {
                                    continue;
                                }

                                foreach (var enemyId in wave.enemyIds)
                                {
                                    Check(
                                        enemyDefinitions.ContainsKey(enemyId),
                                        $"Combat wave '{wave.id}' references missing enemy '{enemyId}'.");
                                }
                            }
                        }
                    }
                }

                foreach (var puzzle in dungeon.puzzles)
                {
                    if (puzzle == null)
                    {
                        continue;
                    }

                    Check(
                        dungeon.GetRoom(puzzle.roomId)?.kind == ModularDungeonRoomKind.Puzzle,
                        $"Puzzle '{puzzle.id}' does not reference a puzzle room.");
                    RequireText(puzzle.displayName, $"Puzzle '{puzzle.id}' displayName");
                    RequireText(puzzle.clue, $"Puzzle '{puzzle.id}' clue");
                    Check(
                        puzzle.mechanisms != null && puzzle.mechanisms.Count >= 3,
                        $"Puzzle '{puzzle.id}' needs at least three readable mechanisms.");
                    Check(
                        puzzle.solution != null && puzzle.mechanisms != null &&
                        puzzle.solution.Length == puzzle.mechanisms.Count,
                        $"Puzzle '{puzzle.id}' solution length must match its mechanism count.");
                    if (puzzle.solution == null || puzzle.mechanisms == null)
                    {
                        continue;
                    }

                    if (puzzle.kind == ModularDungeonPuzzleKind.OrderedSequence)
                    {
                        var indexes = new HashSet<int>();
                        foreach (var solutionIndex in puzzle.solution)
                        {
                            Check(
                                solutionIndex >= 0 && solutionIndex < puzzle.mechanisms.Count,
                                $"Ordered puzzle '{puzzle.id}' has out-of-range solution index {solutionIndex}.");
                            Check(
                                indexes.Add(solutionIndex),
                                $"Ordered puzzle '{puzzle.id}' repeats mechanism index {solutionIndex}.");
                        }
                    }
                    else
                    {
                        for (var index = 0; index < puzzle.mechanisms.Count; index++)
                        {
                            var mechanism = puzzle.mechanisms[index];
                            Check(
                                mechanism != null &&
                                mechanism.stateCount >= 2 &&
                                puzzle.solution[index] >= 0 &&
                                puzzle.solution[index] < mechanism.stateCount,
                                $"State puzzle '{puzzle.id}' mechanism {index} has an invalid target.");
                            Check(
                                mechanism != null &&
                                mechanism.stateLabels != null &&
                                mechanism.stateLabels.Length == mechanism.stateCount,
                                $"State puzzle '{puzzle.id}' mechanism {index} needs a label per state.");
                        }
                    }
                }

                foreach (var checkpoint in dungeon.checkpoints)
                {
                    if (checkpoint == null)
                    {
                        continue;
                    }

                    var room = dungeon.GetRoom(checkpoint.roomId);
                    Check(
                        room != null &&
                        room.kind == ModularDungeonRoomKind.Checkpoint &&
                        room.checkpointId == checkpoint.id,
                        $"Checkpoint '{checkpoint.id}' has inconsistent room linkage.");
                }
            }
        }

        private static void VerifyProgressionNormalization()
        {
            var definition = DungeonCatalog.Get(DungeonCatalog.GlasswaterId);
            Check(definition != null, "Cannot verify progression without Glasswater.");
            if (definition == null)
            {
                return;
            }

            var fresh = DungeonProgressRules.CreateFresh(definition);
            Check(fresh != null, "DungeonProgressRules.CreateFresh returned null.");
            Check(
                fresh != null &&
                fresh.id == definition.id &&
                fresh.currentRoomId == definition.entranceRoomId &&
                fresh.puzzles.Count == definition.puzzles.Count &&
                !fresh.completed &&
                !fresh.rewardGranted,
                "Fresh dungeon progress is not a safe entrance-state record.");

            var corrupt = new DungeonSaveData
            {
                id = definition.id,
                currentRoomId = definition.bossRoomId,
                checkpointId = DungeonCatalog.KeepersCheckpointId,
                activeCombatRoomId = DungeonCatalog.GlasswaterCombatRoomId,
                activeWaveIndex = 99,
                remainingEnemyIds = new List<string> { "enemy.invalid" },
                completedRoomIds = new List<string>
                {
                    "dungeon.invalid.room",
                    DungeonCatalog.GlasswaterCheckpointRoomId
                },
                solvedPuzzleIds = new List<string>
                {
                    DungeonCatalog.CounterflowPuzzleId,
                    "dungeon.invalid.puzzle"
                },
                puzzles = new List<DungeonPuzzleSaveData>
                {
                    new DungeonPuzzleSaveData
                    {
                        id = DungeonCatalog.CounterflowPuzzleId,
                        values = new[] { 999 }
                    }
                }
            };
            var changed = DungeonProgressRules.Normalize(definition, corrupt);
            Check(changed, "Corrupt dungeon progress was not reported as reconciled.");
            Check(
                corrupt.currentRoomId == DungeonCatalog.GlasswaterCheckpointRoomId,
                "Inaccessible current room did not recover to the saved checkpoint.");
            Check(
                !corrupt.completedRoomIds.Contains("dungeon.invalid.room") &&
                !corrupt.solvedPuzzleIds.Contains("dungeon.invalid.puzzle"),
                "Definition-aware normalization retained invalid stable IDs.");
            Check(
                corrupt.puzzles.Count == definition.puzzles.Count,
                "Definition-aware normalization did not rebuild missing puzzle records.");
            Check(
                corrupt.activeWaveIndex == 1 &&
                corrupt.remainingEnemyIds.Count ==
                definition.GetRoom(DungeonCatalog.GlasswaterCombatRoomId).waves[1].enemyIds.Length,
                "Combat reload normalization did not restore the valid current wave roster.");

            var counterflowState = corrupt.puzzles.Find(
                state => state.id == DungeonCatalog.CounterflowPuzzleId);
            Check(
                counterflowState != null &&
                counterflowState.solved &&
                corrupt.completedRoomIds.Contains(DungeonCatalog.GlasswaterWeirRoomId),
                "Solved puzzle state did not imply its room completion.");

            var reloadJson = JsonUtility.ToJson(corrupt);
            var reload = JsonUtility.FromJson<DungeonSaveData>(reloadJson);
            DungeonProgressRules.Normalize(definition, reload);
            var once = JsonUtility.ToJson(reload);
            DungeonProgressRules.Normalize(definition, reload);
            var twice = JsonUtility.ToJson(reload);
            Check(once == twice, "Dungeon progress normalization is not idempotent after reload.");

            var completed = DungeonProgressRules.CreateFresh(definition);
            completed.bossDefeated = true;
            DungeonProgressRules.Normalize(definition, completed);
            Check(
                completed.completed &&
                completed.completionCount == 1 &&
                completed.completedRoomIds.Count == definition.rooms.Count &&
                completed.solvedPuzzleIds.Count == definition.puzzles.Count &&
                string.IsNullOrEmpty(completed.activeCombatRoomId),
                "Boss-defeated normalization did not produce a traversable completed graph.");

            var duplicates = new List<DungeonSaveData>
            {
                new DungeonSaveData
                {
                    id = definition.id,
                    currentRoomId = definition.entranceRoomId,
                    completedRoomIds = new List<string> { definition.entranceRoomId }
                },
                new DungeonSaveData
                {
                    id = definition.id,
                    checkpointId = DungeonCatalog.KeepersCheckpointId,
                    completedRoomIds = new List<string> { DungeonCatalog.GlasswaterCombatRoomId }
                }
            };
            DungeonSaveNormalization.NormalizeInPlace(duplicates);
            Check(
                duplicates.Count == 1 &&
                duplicates[0].checkpointId == DungeonCatalog.KeepersCheckpointId &&
                duplicates[0].completedRoomIds.Contains(definition.entranceRoomId) &&
                duplicates[0].completedRoomIds.Contains(DungeonCatalog.GlasswaterCombatRoomId),
                "Duplicate dungeon records were not merged without losing progression.");

            var normalize = typeof(TetherfallSaveSystem).GetMethod(
                "Normalize",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(TetherfallSaveData) },
                null);
            Check(normalize != null, "TetherfallSaveSystem.Normalize was not found.");
            if (normalize != null)
            {
                var legacy = new TetherfallSaveData
                {
                    schemaVersion = 1,
                    dungeons = null
                };
                InvokeNormalize(normalize, legacy);
                Check(
                    legacy.dungeons != null &&
                    legacy.schemaVersion == TetherfallSaveData.CurrentSchemaVersion,
                    "Legacy save normalization did not initialize the dungeon collection.");
            }
        }

        private static void VerifyOneShotRewards()
        {
            var definition = DungeonCatalog.Get(DungeonCatalog.GlasswaterId);
            var progress = DungeonProgressRules.CreateFresh(definition);
            progress.bossDefeated = true;
            DungeonProgressRules.Normalize(definition, progress);

            Check(
                DungeonProgressRules.TryClaimReward(progress),
                "A completed first clear could not claim its reward.");
            Check(
                progress.rewardGranted,
                "Reward claim did not persist the one-shot guard.");
            Check(
                !DungeonProgressRules.TryClaimReward(progress),
                "The same completion reward could be claimed twice.");

            var reload = JsonUtility.FromJson<DungeonSaveData>(JsonUtility.ToJson(progress));
            DungeonProgressRules.Normalize(definition, reload);
            Check(
                !DungeonProgressRules.TryClaimReward(reload),
                "A completed reward became claimable again after serialization/reload.");

            var incomplete = DungeonProgressRules.CreateFresh(definition);
            Check(
                !DungeonProgressRules.TryClaimReward(incomplete),
                "An incomplete dungeon could claim completion rewards.");
        }

        private static void VerifyBoundsAndBudgets()
        {
            foreach (var dungeon in DungeonCatalog.All)
            {
                if (dungeon == null)
                {
                    continue;
                }

                ValidateVector(dungeon.gatePosition, $"Dungeon '{dungeon.id}' gatePosition");
                ValidateVector(dungeon.returnPosition, $"Dungeon '{dungeon.id}' returnPosition");
                ValidateVector(dungeon.interiorOrigin, $"Dungeon '{dungeon.id}' interiorOrigin");
                ValidatePositiveVector(
                    dungeon.localBoundsSize,
                    $"Dungeon '{dungeon.id}' localBoundsSize");
                Check(
                    Vector3.Distance(dungeon.gatePosition, dungeon.returnPosition) >= 2f &&
                    Vector3.Distance(dungeon.gatePosition, dungeon.returnPosition) <= 25f,
                    $"Dungeon '{dungeon.id}' return point must be distinct and within 25m of its gate.");
                Check(
                    Vector3.Distance(dungeon.gatePosition, dungeon.interiorOrigin) >= 100f,
                    $"Dungeon '{dungeon.id}' interior is too close to its regional entrance.");

                var halfBounds = dungeon.localBoundsSize * 0.5f;
                foreach (var room in dungeon.rooms)
                {
                    if (room == null)
                    {
                        continue;
                    }

                    ValidatePositiveVector(room.size, $"Room '{room.id}' size");
                    Check(
                        FitsBounds(room.localCenter, room.size * 0.5f, dungeon.localBoundsCenter, halfBounds),
                        $"Room '{room.id}' exceeds dungeon '{dungeon.id}' local bounds.");
                    Check(
                        FitsBounds(
                            room.safeLocalPosition,
                            Vector3.zero,
                            room.localCenter,
                            room.size * 0.5f + new Vector3(0f, 3f, 0f)),
                        $"Room '{room.id}' safe re-entry point is outside the room.");
                    ValidateVector(
                        dungeon.interiorOrigin + room.localCenter,
                        $"Room '{room.id}' world center");
                }

                foreach (var connection in dungeon.connections)
                {
                    if (connection == null)
                    {
                        continue;
                    }

                    ValidatePositiveVector(
                        connection.doorScale,
                        $"Connection '{connection.id}' doorScale");
                    Check(
                        connection.wallWidth > connection.doorScale.x &&
                        connection.wallWidth <= dungeon.localBoundsSize.x,
                        $"Connection '{connection.id}' wall width is invalid.");
                    Check(
                        FitsBounds(
                            connection.doorLocalPosition,
                            connection.doorScale * 0.5f,
                            dungeon.localBoundsCenter,
                            halfBounds),
                        $"Connection '{connection.id}' seal exceeds dungeon local bounds.");
                }

                var estimatedRenderers = EstimateRenderers(dungeon);
                var estimatedColliders = EstimateColliders(dungeon);
                Check(
                    dungeon.rendererBudget >= estimatedRenderers &&
                    dungeon.rendererBudget <= 240,
                    $"Dungeon '{dungeon.id}' renderer budget {dungeon.rendererBudget} does not cover " +
                    $"the {estimatedRenderers}-renderer authored estimate or exceeds 240.");
                Check(
                    dungeon.colliderBudget >= estimatedColliders &&
                    dungeon.colliderBudget <= 120,
                    $"Dungeon '{dungeon.id}' collider budget {dungeon.colliderBudget} does not cover " +
                    $"the {estimatedColliders}-collider authored estimate or exceeds 120.");
                Check(
                    dungeon.addedLightBudget == 0,
                    $"Dungeon '{dungeon.id}' must retain a zero-added-light budget.");
                Check(
                    TetherfallWorldInteractionFeedback.MaximumActiveEffects <= 18,
                    "Shared world-interaction effect cap exceeds the documented 18-effect budget.");
                Check(
                    TetherfallWorldInteractionFeedback.ShapesPerEffect <= 8,
                    "Shared world-interaction shape budget exceeds eight shapes per effect.");
            }
        }

        private static void VerifyRuntimeWiring()
        {
            Check(
                typeof(IDungeonBossArena).IsAssignableFrom(typeof(ModularDungeonDirector)),
                "ModularDungeonDirector must reuse IDungeonBossArena.");
            Check(
                typeof(IWorldInteractable).IsAssignableFrom(typeof(ModularDungeonGate)),
                "ModularDungeonGate must reuse the world interaction API.");
            Check(
                typeof(IWorldInteractable).IsAssignableFrom(typeof(DungeonCheckpoint)),
                "DungeonCheckpoint must reuse the world interaction API.");
            Check(
                typeof(TetherfallSaveData).GetField("dungeons") != null,
                "TetherfallSaveData is missing its dungeon collection.");
            Check(
                typeof(TetherfallGame).GetMethod(
                    "WriteDungeonSave",
                    BindingFlags.Instance | BindingFlags.NonPublic) != null,
                "TetherfallGame is missing WriteDungeonSave integration.");
            Check(
                typeof(TetherfallGame).GetMethod(
                    "RestoreDungeonState",
                    BindingFlags.Instance | BindingFlags.NonPublic) != null,
                "TetherfallGame is missing RestoreDungeonState integration.");
            Check(
                typeof(TetherfallGame).GetMethod(
                    "TryGetModularDungeonDeathRecovery",
                    BindingFlags.Instance | BindingFlags.NonPublic) != null,
                "Player defeat does not expose modular-dungeon checkpoint recovery.");
            Check(
                typeof(TetherfallGame).GetMethod(
                    "GetModularDungeonObjectiveTarget",
                    BindingFlags.Instance | BindingFlags.NonPublic) != null,
                "Objective guidance is not wired to modular dungeons.");
            Check(
                typeof(TetherfallAccessibilityRuntime).GetMethod(
                    "ResolveColorCue",
                    BindingFlags.Static | BindingFlags.Public) != null &&
                typeof(TetherfallAccessibilityRuntime).GetMethod(
                    "ResolveTelegraphColor",
                    BindingFlags.Static | BindingFlags.Public) != null,
                "Dungeon telegraphs cannot reuse accessibility color APIs.");
        }

        private static int EstimateRenderers(ModularDungeonDefinition dungeon)
        {
            var count = dungeon.rooms.Count * 8;
            count += dungeon.connections.Count * 3;
            count += 2;
            foreach (var puzzle in dungeon.puzzles)
            {
                count += 2;
                foreach (var mechanism in puzzle.mechanisms)
                {
                    count += 3;
                    if (puzzle.kind == ModularDungeonPuzzleKind.StateMatch)
                    {
                        count += 4;
                    }
                }
            }

            count += dungeon.checkpoints.Count * 3;
            count += 6;
            count += 3;
            count += 6;
            return count;
        }

        private static int EstimateColliders(ModularDungeonDefinition dungeon)
        {
            var count = dungeon.rooms.Count * 3;
            count += dungeon.connections.Count * 3;
            count += 2;
            foreach (var puzzle in dungeon.puzzles)
            {
                count += puzzle.mechanisms.Count;
            }

            count += dungeon.checkpoints.Count;
            count += 4;
            count += 3;
            return count;
        }

        private static int CountRooms(
            ModularDungeonDefinition dungeon,
            ModularDungeonRoomKind kind)
        {
            var count = 0;
            if (dungeon?.rooms == null)
            {
                return count;
            }

            foreach (var room in dungeon.rooms)
            {
                if (room != null && room.kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool FitsBounds(
            Vector3 center,
            Vector3 halfSize,
            Vector3 boundsCenter,
            Vector3 boundsHalfSize)
        {
            var delta = center - boundsCenter;
            return Mathf.Abs(delta.x) + halfSize.x <= boundsHalfSize.x + 0.01f &&
                   Mathf.Abs(delta.y) + halfSize.y <= boundsHalfSize.y + 0.01f &&
                   Mathf.Abs(delta.z) + halfSize.z <= boundsHalfSize.z + 0.01f;
        }

        private static void ValidateVector(Vector3 value, string label)
        {
            Check(
                IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z),
                $"{label} contains NaN or Infinity.");
            Check(
                Mathf.Abs(value.x) <= MaximumCoordinate &&
                Mathf.Abs(value.y) <= MaximumCoordinate &&
                Mathf.Abs(value.z) <= MaximumCoordinate,
                $"{label} exceeds the {MaximumCoordinate} coordinate bound.");
        }

        private static void ValidatePositiveVector(Vector3 value, string label)
        {
            ValidateVector(value, label);
            Check(
                value.x > 0f && value.y > 0f && value.z > 0f,
                $"{label} must have positive components.");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static T GetPrivateStaticField<T>(Type owner, string fieldName)
        {
            var field = owner.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(owner.FullName, fieldName);
            }

            return (T)field.GetValue(null);
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

        private static void RegisterId(HashSet<string> ids, string id, string owner)
        {
            Check(
                !string.IsNullOrWhiteSpace(id),
                $"{owner} has a null, empty, or whitespace stable ID.");
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            Check(id == id.Trim(), $"{owner} ID '{id}' has surrounding whitespace.");
            Check(ids.Add(id), $"Stable ID '{id}' is duplicated within DungeonCatalog.");
        }

        private static void RequireText(string text, string label)
        {
            Check(!string.IsNullOrWhiteSpace(text), $"{label} is null, empty, or whitespace.");
        }

        private static void Run(string section, Action action)
        {
            sectionCount++;
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Fail($"{section} check could not complete: {Describe(exception)}");
            }
        }

        private static void Check(bool condition, string failure)
        {
            assertionCount++;
            if (!condition)
            {
                Fail(failure);
            }
        }

        private static void Fail(string failure)
        {
            if (!string.IsNullOrWhiteSpace(failure) && !Failures.Contains(failure))
            {
                Failures.Add(failure);
            }
        }

        private static void Complete()
        {
            if (Failures.Count == 0)
            {
                Debug.Log(
                    $"Tetherfall modular-dungeon verification passed: {assertionCount} assertions " +
                    $"across {sectionCount} sections. Graph, references, reloads, rewards, and bounds are stable.");
                return;
            }

            var message = new StringBuilder();
            message.Append("Tetherfall modular-dungeon verification failed with ");
            message.Append(Failures.Count);
            message.AppendLine(" actionable issue(s):");
            for (var index = 0; index < Failures.Count; index++)
            {
                message.Append(index + 1);
                message.Append(". ");
                message.AppendLine(Failures[index]);
            }

            message.Append(
                $"Checked {assertionCount} assertions across {sectionCount} sections.");
            var failureMessage = message.ToString();
            Debug.LogError(failureMessage);
            throw new InvalidOperationException(failureMessage);
        }

        private static string Describe(Exception exception)
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
