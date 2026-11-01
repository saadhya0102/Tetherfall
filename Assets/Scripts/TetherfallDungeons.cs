using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public static class ModularDungeonBuilder
    {
        public static void Build(TetherfallGame game)
        {
            if (game == null)
            {
                return;
            }

            foreach (var definition in DungeonCatalog.All)
            {
                if (definition == null)
                {
                    continue;
                }

                var root = new GameObject($"{definition.displayName} Dungeon Director");
                root.transform.SetParent(game.transform, false);
                var director = root.AddComponent<ModularDungeonDirector>();
                director.Initialize(game, definition);
                var gate = ModularDungeonGate.Create(game, director, definition);
                director.SetGate(gate);
            }
        }
    }

    public sealed partial class TetherfallGame
    {
        private readonly List<ModularDungeonDirector> modularDungeons =
            new List<ModularDungeonDirector>();

        internal void RegisterModularDungeon(ModularDungeonDirector dungeon)
        {
            if (dungeon != null && !modularDungeons.Contains(dungeon))
            {
                modularDungeons.Add(dungeon);
            }
        }

        internal bool CanEnterModularDungeon(ModularDungeonDefinition definition)
        {
            return definition != null &&
                   (string.IsNullOrEmpty(definition.requiredQuestId) ||
                    IsQuestComplete(definition.requiredQuestId));
        }

        internal void RequestModularDungeonSave()
        {
            RequestCriticalSave();
        }

        private void WriteDungeonSave(TetherfallSaveData data)
        {
            if (data == null)
            {
                return;
            }

            foreach (var dungeon in modularDungeons)
            {
                if (dungeon != null)
                {
                    data.dungeons.Add(dungeon.WriteSaveData());
                }
            }
        }

        private void RestoreDungeonState(TetherfallSaveData data)
        {
            if (data == null)
            {
                return;
            }

            foreach (var dungeon in modularDungeons)
            {
                if (dungeon == null)
                {
                    continue;
                }

                var saved = data.dungeons.Find(record => record != null && record.id == dungeon.DungeonId);
                dungeon.RestoreSaveData(saved);
                ReconcileModularDungeonQuest(dungeon.Definition, dungeon.Progress);
            }
        }

        private void ReconcileModularDungeonQuest(
            ModularDungeonDefinition definition,
            DungeonSaveData progress)
        {
            if (definition == null || progress == null)
            {
                return;
            }

            var quest = questLog.Find(record => record.id == definition.questId);
            if (quest == null)
            {
                return;
            }

            var reconciledProgress = Mathf.Clamp(
                DungeonProgressRules.CalculateQuestProgress(definition, progress),
                0,
                quest.required);
            quest.current = Mathf.Max(quest.current, reconciledProgress);
            if (progress.completed || quest.current >= quest.required)
            {
                quest.current = quest.required;
                quest.completed = true;
            }
        }

        internal void AdvanceModularDungeonQuest(
            ModularDungeonDefinition definition,
            int priorProgress,
            DungeonSaveData progress)
        {
            if (definition == null || progress == null)
            {
                return;
            }

            var current = DungeonProgressRules.CalculateQuestProgress(definition, progress);
            var amount = Mathf.Max(0, current - priorProgress);
            if (amount > 0)
            {
                AdvanceQuest(definition.questId, amount);
            }
        }

        internal bool EnsureModularDungeonRewardEquipment(string equipmentId)
        {
            if (string.IsNullOrEmpty(equipmentId))
            {
                return false;
            }

            var definition = equipmentCatalog.Find(item => item.id == equipmentId);
            if (definition == null || !unlockedEquipment.Add(equipmentId))
            {
                return false;
            }

            AutoEquipFieldGear(definition);
            Player?.RefreshEquipmentVisuals();
            return true;
        }

        internal void GrantModularDungeonRewards(ModularDungeonDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            EnsureModularDungeonRewardEquipment(definition.rewardEquipmentId);
            if (definition.favorReward > 0)
            {
                AddFavors(definition.favorReward, definition.displayName);
            }

            if (definition.resourceRewardAmount > 0)
            {
                AddResource(definition.resourceRewardKind, definition.resourceRewardAmount);
            }

            if (definition.experienceReward > 0)
            {
                GainExperience(definition.experienceReward);
            }
        }

        private string GetModularDungeonObjectiveText()
        {
            foreach (var dungeon in modularDungeons)
            {
                if (dungeon != null &&
                    dungeon.IsUnlocked &&
                    !dungeon.IsComplete &&
                    !IsQuestComplete(dungeon.Definition.questId))
                {
                    return dungeon.GetObjectiveText();
                }
            }

            return string.Empty;
        }

        private Transform GetModularDungeonObjectiveTarget()
        {
            foreach (var dungeon in modularDungeons)
            {
                if (dungeon != null &&
                    dungeon.IsUnlocked &&
                    !dungeon.IsComplete &&
                    !IsQuestComplete(dungeon.Definition.questId))
                {
                    return dungeon.GetGuidanceTarget();
                }
            }

            return null;
        }

        private bool TryGetModularDungeonDeathRecovery(out Vector3 destination)
        {
            foreach (var dungeon in modularDungeons)
            {
                if (dungeon != null && dungeon.TryHandlePlayerDefeat(out destination))
                {
                    return true;
                }
            }

            destination = Vector3.zero;
            return false;
        }
    }

    public sealed class ModularDungeonDirector : MonoBehaviour, IDungeonBossArena
    {
        private const float StateCheckInterval = 0.18f;
        private const float DoorGap = 6f;

        private sealed class DungeonPalette
        {
            public Material floor;
            public Material wall;
            public Material rib;
            public Material water;
            public Material dormant;
            public Material active;
            public Material warning;
            public Material glass;
        }

        private readonly List<DungeonDoor> doors = new List<DungeonDoor>();
        private readonly List<Enemy> activeEnemies = new List<Enemy>();
        private readonly Dictionary<string, DungeonMechanism> mechanisms =
            new Dictionary<string, DungeonMechanism>(StringComparer.Ordinal);
        private readonly Dictionary<string, Transform> roomAnchors =
            new Dictionary<string, Transform>(StringComparer.Ordinal);

        private TetherfallGame game;
        private ModularDungeonDefinition definition;
        private DungeonSaveData progress;
        private DungeonPalette palette;
        private ModularDungeonGate gate;
        private DungeonCheckpoint checkpoint;
        private DungeonTraversalLatch traversalLatch;
        private DungeonPortal exitPortal;
        private DungeonReplayMarker replayMarker;
        private DungeonBoss activeBoss;
        private Transform bossAnchor;
        private string lastRoomId = string.Empty;
        private bool attemptActive;
        private bool replayActive;
        private bool saveReconciliationPending;
        private float nextStateCheckAt;
        private int authoredRendererCount;
        private int authoredColliderCount;
        private int authoredLightCount;

        public string DungeonId => definition != null ? definition.id : string.Empty;
        public ModularDungeonDefinition Definition => definition;
        public DungeonSaveData Progress => progress;
        public bool IsComplete => progress != null && progress.completed;
        public bool IsUnlocked => game != null && game.CanEnterModularDungeon(definition);
        public string TraversalRoomId
        {
            get
            {
                var room = definition?.rooms?.Find(
                    candidate => candidate != null &&
                                 candidate.kind == ModularDungeonRoomKind.Traversal);
                return room != null ? room.id : string.Empty;
            }
        }
        public int AuthoredRendererCount => authoredRendererCount;
        public int AuthoredColliderCount => authoredColliderCount;
        public int AuthoredLightCount => authoredLightCount;

        public void Initialize(TetherfallGame owner, ModularDungeonDefinition dungeonDefinition)
        {
            game = owner;
            definition = dungeonDefinition;
            transform.position = definition.interiorOrigin;
            progress = DungeonProgressRules.CreateFresh(definition);
            palette = CreatePalette();
            BuildInterior();
            RefreshWorldState();
            game.RegisterModularDungeon(this);
        }

        public void SetGate(ModularDungeonGate dungeonGate)
        {
            gate = dungeonGate;
        }

        private void Update()
        {
            if (saveReconciliationPending)
            {
                saveReconciliationPending = false;
                game?.RequestModularDungeonSave();
            }

            if (!attemptActive ||
                game == null ||
                game.Player == null ||
                Time.time < nextStateCheckAt)
            {
                return;
            }

            nextStateCheckAt = Time.time + StateCheckInterval;
            if (!ContainsInterior(game.PlayerTransform.position))
            {
                if (ContainsInteriorHorizontally(game.PlayerTransform.position))
                {
                    RecoverToCheckpoint(
                        "The Threadglass catches the fall and returns you to the last stable sluice room.");
                }
                else
                {
                    HandleInterruptedAttempt();
                }

                return;
            }

            var room = FindRoomAt(game.PlayerTransform.position);
            if (room == null)
            {
                return;
            }

            if (!DungeonProgressRules.IsRoomAccessible(definition, progress, room.id))
            {
                RecoverToCheckpoint(
                    "The sluice folds back toward the last stable Threadglass mark.");
                return;
            }

            if (lastRoomId != room.id)
            {
                lastRoomId = room.id;
                progress.currentRoomId = room.id;
                HandleRoomEntered(room);
                game.RequestModularDungeonSave();
            }

            if (room.kind == ModularDungeonRoomKind.Combat &&
                progress.activeCombatRoomId == room.id &&
                activeEnemies.Count == 0 &&
                progress.remainingEnemyIds.Count > 0)
            {
                SpawnCurrentWave(room);
            }
        }

        public void Enter()
        {
            if (game == null || game.Player == null)
            {
                return;
            }

            if (!IsUnlocked)
            {
                game.Notify(definition.lockedMessage);
                return;
            }

            ResetTransientEncounter();
            attemptActive = true;
            replayActive = false;
            var recoveryRoomId = progress.completed
                ? definition.entranceRoomId
                : DungeonProgressRules.GetRecoveryRoomId(definition, progress);
            var room = definition.GetRoom(recoveryRoomId) ??
                       definition.GetRoom(definition.entranceRoomId);
            progress.currentRoomId = room.id;
            lastRoomId = string.Empty;
            game.Player.Teleport(transform.TransformPoint(room.safeLocalPosition));
            game.Notify(
                progress.completed
                    ? "Glasswater holds a stable current. The Regent's echo can be recalled in the reservoir."
                    : !string.IsNullOrEmpty(progress.checkpointId)
                        ? "Keeper's Threadglass draws the expedition back into Glasswater."
                        : "The drowned intake closes behind you. Read the water before turning its works.");
            RefreshWorldState();
            game.RequestModularDungeonSave();
        }

        public void Exit()
        {
            if (game == null)
            {
                return;
            }

            attemptActive = false;
            lastRoomId = string.Empty;
            ResetTransientEncounter();
            if (game.Player != null)
            {
                game.Player.Teleport(definition.returnPosition);
            }

            game.Notify(
                progress.completed
                    ? "You return to Morrowfen with Glasswater running clear."
                    : "You retreat to the Glasswater approach. Stable mechanisms keep their state.");
            game.RequestModularDungeonSave();
        }

        public bool TryHandlePlayerDefeat(out Vector3 destination)
        {
            if (!attemptActive ||
                game == null ||
                game.Player == null ||
                !ContainsInterior(game.PlayerTransform.position))
            {
                destination = Vector3.zero;
                return false;
            }

            ResetTransientEncounter();
            replayActive = false;
            var recoveryRoomId = DungeonProgressRules.GetRecoveryRoomId(definition, progress);
            var room = definition.GetRoom(recoveryRoomId) ??
                       definition.GetRoom(definition.entranceRoomId);
            progress.currentRoomId = room.id;
            lastRoomId = string.Empty;
            destination = transform.TransformPoint(room.safeLocalPosition);
            RefreshWorldState();
            game.RequestModularDungeonSave();
            return true;
        }

        public DungeonSaveData WriteSaveData()
        {
            DungeonProgressRules.Normalize(definition, progress);
            var json = JsonUtility.ToJson(progress);
            return JsonUtility.FromJson<DungeonSaveData>(json);
        }

        public void RestoreSaveData(DungeonSaveData data)
        {
            var wasInside = game != null &&
                            game.PlayerTransform != null &&
                            ContainsInterior(game.PlayerTransform.position);
            ResetTransientEncounter();
            progress = data != null
                ? JsonUtility.FromJson<DungeonSaveData>(JsonUtility.ToJson(data))
                : DungeonProgressRules.CreateFresh(definition);
            var reconciled = DungeonProgressRules.Normalize(definition, progress);

            if (progress.rewardGranted)
            {
                reconciled |= game.EnsureModularDungeonRewardEquipment(definition.rewardEquipmentId);
            }
            else if (progress.completed && DungeonProgressRules.TryClaimReward(progress))
            {
                game.GrantModularDungeonRewards(definition);
                reconciled = true;
            }

            attemptActive = wasInside;
            replayActive = false;
            lastRoomId = string.Empty;
            if (wasInside)
            {
                var recoveryRoom = definition.GetRoom(
                    DungeonProgressRules.GetRecoveryRoomId(definition, progress));
                recoveryRoom ??= definition.GetRoom(definition.entranceRoomId);
                progress.currentRoomId = recoveryRoom.id;
                game.Player.Teleport(transform.TransformPoint(recoveryRoom.safeLocalPosition));
            }

            RefreshWorldState();
            saveReconciliationPending |= reconciled;
        }

        public string GetGatePrompt()
        {
            if (!IsUnlocked)
            {
                return "Inspect the sealed Glasswater intake";
            }

            if (progress.completed)
            {
                return "Re-enter Glasswater Sluice";
            }

            return string.IsNullOrEmpty(progress.checkpointId)
                ? "Enter Glasswater Sluice"
                : "Return to Keeper's Threadglass";
        }

        public string GetObjectiveText()
        {
            if (!attemptActive || !ContainsInterior(game.PlayerTransform.position))
            {
                return "Enter Glasswater Sluice beyond the restored Fenlight route.";
            }

            var room = GetNextProgressionRoom();
            if (room == null)
            {
                return "Use the Outflow Loom to leave Glasswater Sluice.";
            }

            switch (room.kind)
            {
                case ModularDungeonRoomKind.Puzzle:
                {
                    var puzzle = definition.GetPuzzle(room.puzzleId);
                    var state = GetPuzzleState(room.puzzleId);
                    if (puzzle != null &&
                        state != null &&
                        puzzle.kind == ModularDungeonPuzzleKind.OrderedSequence)
                    {
                        return $"Set the Counterflow Weir in its carved order ({state.values[0]}/{puzzle.solution.Length}).";
                    }

                    return $"Balance the three Tideglass columns ({CountMatchingMechanisms(puzzle, state)}/3).";
                }
                case ModularDungeonRoomKind.Combat:
                    return progress.activeCombatRoomId == room.id
                        ? $"Clear the Brood Cistern: wave {progress.activeWaveIndex + 1}/{room.waves.Count}, " +
                          $"{progress.remainingEnemyIds.Count} echo(es) remain."
                        : "Enter the Brood Cistern and break the two-part fen swarm.";
                case ModularDungeonRoomKind.Checkpoint:
                    return "Stabilize Keeper's Threadglass checkpoint.";
                case ModularDungeonRoomKind.Traversal:
                    return "Ride the pressure lift or climb the marked spillway face, then release the upper latch.";
                case ModularDungeonRoomKind.Boss:
                    return "Defeat the Glasswater Regent in the Mirror Reservoir.";
                case ModularDungeonRoomKind.Exit:
                    return "Use the Outflow Loom to return to Morrowfen.";
                default:
                    return $"Proceed through {room.displayName}.";
            }
        }

        public Transform GetGuidanceTarget()
        {
            if (!attemptActive ||
                game == null ||
                game.PlayerTransform == null ||
                !ContainsInterior(game.PlayerTransform.position))
            {
                return gate != null ? gate.transform : transform;
            }

            var room = GetNextProgressionRoom();
            if (room == null)
            {
                return exitPortal != null ? exitPortal.transform : transform;
            }

            if (room.kind == ModularDungeonRoomKind.Puzzle)
            {
                var puzzle = definition.GetPuzzle(room.puzzleId);
                var state = GetPuzzleState(room.puzzleId);
                var mechanism = GetNextPuzzleMechanism(puzzle, state);
                if (mechanism != null && mechanisms.TryGetValue(mechanism.id, out var interactable))
                {
                    return interactable.transform;
                }
            }
            else if (room.kind == ModularDungeonRoomKind.Combat)
            {
                PruneActiveEnemies();
                if (activeEnemies.Count > 0 && activeEnemies[0] != null)
                {
                    return activeEnemies[0].transform;
                }
            }
            else if (room.kind == ModularDungeonRoomKind.Checkpoint && checkpoint != null)
            {
                return checkpoint.transform;
            }
            else if (room.kind == ModularDungeonRoomKind.Traversal && traversalLatch != null)
            {
                return traversalLatch.transform;
            }
            else if (room.kind == ModularDungeonRoomKind.Boss)
            {
                return activeBoss != null ? activeBoss.transform : bossAnchor;
            }

            return roomAnchors.TryGetValue(room.id, out var anchor) ? anchor : transform;
        }

        public bool CanUsePuzzle(string puzzleId)
        {
            var puzzle = definition.GetPuzzle(puzzleId);
            var state = GetPuzzleState(puzzleId);
            return attemptActive &&
                   puzzle != null &&
                   state != null &&
                   !state.solved &&
                   DungeonProgressRules.IsRoomAccessible(definition, progress, puzzle.roomId);
        }

        public string GetPuzzlePrompt(string puzzleId, int mechanismIndex)
        {
            var puzzle = definition.GetPuzzle(puzzleId);
            var state = GetPuzzleState(puzzleId);
            if (puzzle == null ||
                state == null ||
                mechanismIndex < 0 ||
                mechanismIndex >= puzzle.mechanisms.Count)
            {
                return "Inspect dormant sluicework";
            }

            if (state.solved)
            {
                return $"{puzzle.displayName} stabilized";
            }

            var mechanism = puzzle.mechanisms[mechanismIndex];
            if (puzzle.kind == ModularDungeonPuzzleKind.OrderedSequence)
            {
                return $"Turn {mechanism.displayName} · sequence {state.values[0]}/{puzzle.solution.Length}";
            }

            var current = state.values[mechanismIndex];
            var target = puzzle.solution[mechanismIndex];
            return $"Cycle {mechanism.displayName}: {GetStateLabel(mechanism, current)} → {GetStateLabel(mechanism, target)}";
        }

        public void ActivatePuzzleMechanism(string puzzleId, int mechanismIndex)
        {
            if (!CanUsePuzzle(puzzleId))
            {
                return;
            }

            var puzzle = definition.GetPuzzle(puzzleId);
            var state = GetPuzzleState(puzzleId);
            if (mechanismIndex < 0 || mechanismIndex >= puzzle.mechanisms.Count)
            {
                return;
            }

            var priorQuestProgress = DungeonProgressRules.CalculateQuestProgress(definition, progress);
            if (puzzle.kind == ModularDungeonPuzzleKind.OrderedSequence)
            {
                var step = Mathf.Clamp(state.values[0], 0, puzzle.solution.Length);
                if (step < puzzle.solution.Length && puzzle.solution[step] == mechanismIndex)
                {
                    state.values[0] = step + 1;
                    PlayPulse(
                        puzzle.mechanisms[mechanismIndex].localPosition,
                        definition.accentColor,
                        PrimitiveType.Cube);
                    game.Notify(
                        state.values[0] >= puzzle.solution.Length
                            ? "Basin, reed, and outflow answer as one current."
                            : $"The weir holds that tone. {state.values[0]}/{puzzle.solution.Length} set.");
                }
                else
                {
                    state.values[0] = 0;
                    PlayPulse(
                        puzzle.mechanisms[mechanismIndex].localPosition,
                        new Color(0.96f, 0.65f, 0.22f),
                        PrimitiveType.Cube);
                    game.Notify("The counterflow bucks against the wrong order. The three wheels release safely.");
                }

                state.solved = state.values[0] >= puzzle.solution.Length;
            }
            else
            {
                var mechanism = puzzle.mechanisms[mechanismIndex];
                state.values[mechanismIndex] =
                    (state.values[mechanismIndex] + 1) %
                    Mathf.Max(1, mechanism.stateCount);
                state.solved = IsStateMatchSolved(puzzle, state);
                PlayPulse(
                    mechanism.localPosition,
                    state.values[mechanismIndex] == puzzle.solution[mechanismIndex]
                        ? definition.accentColor
                        : new Color(0.38f, 0.67f, 0.85f),
                    PrimitiveType.Cylinder);
                game.Notify(
                    state.solved
                        ? "High, low, and mid waterlines balance. The reservoir gate drains open."
                        : $"{mechanism.displayName}: {GetStateLabel(mechanism, state.values[mechanismIndex])}. " +
                          $"{CountMatchingMechanisms(puzzle, state)}/{puzzle.mechanisms.Count} columns balanced.");
            }

            if (state.solved)
            {
                AddUnique(progress.solvedPuzzleIds, puzzle.id);
                AddUnique(progress.completedRoomIds, puzzle.roomId);
                game.AdvanceModularDungeonQuest(definition, priorQuestProgress, progress);
            }

            RefreshWorldState();
            game.RequestModularDungeonSave();
        }

        public bool CanActivateCheckpoint(string checkpointId)
        {
            var checkpointDefinition = definition.GetCheckpoint(checkpointId);
            return attemptActive &&
                   checkpointDefinition != null &&
                   DungeonProgressRules.IsRoomAccessible(
                       definition,
                       progress,
                       checkpointDefinition.roomId);
        }

        public string GetCheckpointPrompt(string checkpointId)
        {
            return progress.checkpointId == checkpointId
                ? "Rest at Keeper's Threadglass"
                : "Stabilize Keeper's Threadglass checkpoint";
        }

        public void ActivateCheckpoint(string checkpointId)
        {
            if (!CanActivateCheckpoint(checkpointId))
            {
                return;
            }

            var checkpointDefinition = definition.GetCheckpoint(checkpointId);
            var firstActivation = progress.checkpointId != checkpointId;
            progress.checkpointId = checkpointId;
            progress.currentRoomId = checkpointDefinition.roomId;
            AddUnique(progress.completedRoomIds, checkpointDefinition.roomId);
            game.HealPlayer(game.MaxHealth);
            PlayPulse(
                checkpointDefinition.localPosition,
                definition.accentColor,
                PrimitiveType.Sphere);
            game.Notify(
                firstActivation
                    ? "Keeper's Threadglass stabilized. Reloads, retreats, and defeats return here."
                    : "Keeper's Threadglass restores the expedition.");
            RefreshWorldState();
            game.RequestModularDungeonSave();
        }

        public bool CanReleaseTraversal()
        {
            return attemptActive &&
                   !string.IsNullOrEmpty(TraversalRoomId) &&
                   !progress.completedRoomIds.Contains(TraversalRoomId) &&
                   DungeonProgressRules.IsRoomAccessible(
                       definition,
                       progress,
                       TraversalRoomId);
        }

        public void ReleaseTraversal()
        {
            if (!CanReleaseTraversal())
            {
                return;
            }

            AddUnique(progress.completedRoomIds, TraversalRoomId);
            PlayPulse(
                traversalLatch.transform.localPosition,
                definition.accentColor,
                PrimitiveType.Cylinder);
            game.Notify("The upper spillway latch releases. A dry ledge opens toward the Tideglass gallery.");
            RefreshWorldState();
            game.RequestModularDungeonSave();
        }

        public bool CanReplayBoss()
        {
            return attemptActive &&
                   progress.completed &&
                   activeBoss == null &&
                   !replayActive;
        }

        public void StartBossReplay()
        {
            if (!CanReplayBoss())
            {
                return;
            }

            replayActive = true;
            StartBossEncounter(true);
            game.Notify("Keeper's Threadglass recalls the Regent's echo. Replay victories grant no first-clear reward.");
        }

        public bool CanUseExit(bool completionRequired)
        {
            return attemptActive && (!completionRequired || progress.completed);
        }

        public bool CanDamageBoss(DungeonBoss boss)
        {
            return attemptActive && boss != null && boss == activeBoss;
        }

        public void OnBossPhaseThreshold(DungeonBoss boss, int phase)
        {
            if (boss == null || boss != activeBoss)
            {
                return;
            }

            var color = phase >= 3
                ? new Color(0.76f, 0.91f, 1f)
                : new Color(0.35f, 0.86f, 0.8f);
            PlayWorldPulse(boss.transform.position, color, PrimitiveType.Cylinder, 8f);
            game.Notify(
                phase >= 3
                    ? "The Regent inverts both reservoirs. Watch the quake ring and reflected volley."
                    : "The Regent floods the mirror channels and calls vault wisps through the glass.");
            game.RequestModularDungeonSave();
        }

        public void OnBossDefeated(DungeonBoss boss)
        {
            if (boss == null || boss != activeBoss)
            {
                return;
            }

            game.ClearDungeonBoss(boss);
            activeBoss = null;
            if (replayActive)
            {
                replayActive = false;
                replayMarker?.Refresh();
                game.Notify("The recalled Regent disperses. First-clear rewards remain sealed against duplication.");
                return;
            }

            if (progress.bossDefeated)
            {
                return;
            }

            var priorQuestProgress = DungeonProgressRules.CalculateQuestProgress(definition, progress);
            progress.bossDefeated = true;
            progress.completed = true;
            progress.completionCount = Mathf.Max(1, progress.completionCount + 1);
            AddUnique(progress.completedRoomIds, definition.bossRoomId);
            AddUnique(progress.completedRoomIds, definition.exitRoomId);
            game.AdvanceModularDungeonQuest(definition, priorQuestProgress, progress);
            if (DungeonProgressRules.TryClaimReward(progress))
            {
                game.GrantModularDungeonRewards(definition);
            }

            PlayWorldPulse(
                bossAnchor.position,
                definition.accentColor,
                PrimitiveType.Sphere,
                10f);
            RefreshWorldState();
            game.Notify(
                "The Glasswater Regent falls quiet. The Sluiceglass Charm and outflow route are secured.");
            game.RequestModularDungeonSave();
        }

        private void HandleRoomEntered(DungeonRoomDefinition room)
        {
            if (room.kind == ModularDungeonRoomKind.Entrance &&
                !progress.completedRoomIds.Contains(room.id))
            {
                AddUnique(progress.completedRoomIds, room.id);
                RefreshWorldState();
                return;
            }

            if (room.kind == ModularDungeonRoomKind.Combat &&
                !progress.completedRoomIds.Contains(room.id))
            {
                BeginCombatRoom(room);
                return;
            }

            if (room.kind == ModularDungeonRoomKind.Boss &&
                !progress.bossDefeated &&
                activeBoss == null)
            {
                StartBossEncounter(false);
            }
        }

        private void BeginCombatRoom(DungeonRoomDefinition room)
        {
            if (room.waves == null || room.waves.Count == 0)
            {
                CompleteCombatRoom(room);
                return;
            }

            if (progress.activeCombatRoomId != room.id)
            {
                progress.activeCombatRoomId = room.id;
                progress.activeWaveIndex = 0;
                progress.remainingEnemyIds.Clear();
                progress.remainingEnemyIds.AddRange(room.waves[0].enemyIds);
            }

            DungeonProgressRules.Normalize(definition, progress);
            game.Notify(
                $"Fen echoes spill into the Brood Cistern. Wave {progress.activeWaveIndex + 1}/{room.waves.Count}.");
            SpawnCurrentWave(room);
        }

        private void SpawnCurrentWave(DungeonRoomDefinition room)
        {
            PruneActiveEnemies();
            if (activeEnemies.Count > 0 ||
                progress.remainingEnemyIds.Count == 0 ||
                room == null)
            {
                return;
            }

            var roster = new List<string>(progress.remainingEnemyIds);
            for (var index = 0; index < roster.Count; index++)
            {
                var angle = index * Mathf.PI * 2f / Mathf.Max(1, roster.Count);
                var local = room.localCenter +
                            new Vector3(Mathf.Cos(angle) * 7f, 1f, Mathf.Sin(angle) * 6f);
                var enemy = Enemy.Create(
                    game,
                    null,
                    roster[index],
                    transform.TransformPoint(local),
                    OnCombatEnemyDefeated);
                if (enemy != null)
                {
                    activeEnemies.Add(enemy);
                }
            }

            if (activeEnemies.Count == 0)
            {
                game.Notify("The cistern encounter could not form. Its safety gate releases.");
                CompleteCombatRoom(room);
            }
        }

        private void OnCombatEnemyDefeated(Enemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            activeEnemies.Remove(enemy);
            RemoveFirst(progress.remainingEnemyIds, enemy.EnemyId);
            if (progress.remainingEnemyIds.Count > 0)
            {
                game.RequestModularDungeonSave();
                return;
            }

            var room = definition.GetRoom(progress.activeCombatRoomId);
            if (room == null)
            {
                return;
            }

            if (progress.activeWaveIndex + 1 < room.waves.Count)
            {
                progress.activeWaveIndex++;
                progress.remainingEnemyIds.AddRange(
                    room.waves[progress.activeWaveIndex].enemyIds);
                game.Notify(
                    $"The cistern answers with a second pressure wave " +
                    $"({progress.activeWaveIndex + 1}/{room.waves.Count}).");
                SpawnCurrentWave(room);
                game.RequestModularDungeonSave();
                return;
            }

            CompleteCombatRoom(room);
        }

        private void CompleteCombatRoom(DungeonRoomDefinition room)
        {
            var priorQuestProgress = DungeonProgressRules.CalculateQuestProgress(definition, progress);
            AddUnique(progress.completedRoomIds, room.id);
            progress.activeCombatRoomId = string.Empty;
            progress.activeWaveIndex = -1;
            progress.remainingEnemyIds.Clear();
            activeEnemies.Clear();
            game.AdvanceModularDungeonQuest(definition, priorQuestProgress, progress);
            PlayPulse(room.localCenter + Vector3.up * 0.12f, definition.accentColor, PrimitiveType.Cylinder);
            game.Notify("The Brood Cistern clears. Keeper's Landing answers beyond the gate.");
            RefreshWorldState();
            game.RequestModularDungeonSave();
        }

        private void StartBossEncounter(bool replay)
        {
            var bossDefinition = BossCatalog.Get(definition.bossId);
            if (bossDefinition == null)
            {
                game.Notify("The reservoir's boss profile is unavailable. The outflow safety opens.");
                if (!replay)
                {
                    progress.bossDefeated = true;
                    progress.completed = true;
                    RefreshWorldState();
                }

                return;
            }

            activeBoss = DungeonBoss.Create(
                game,
                this,
                bossDefinition,
                bossAnchor.position,
                replay);
            replayActive = replay;
            game.RegisterDungeonBoss(activeBoss);
            replayMarker?.Refresh();
            game.Notify(
                replay
                    ? "The Glasswater Regent's echo condenses above the mirror basin."
                    : "Glasswater crowns a drowned keeper. The Regent reverses the reservoir current.");
        }

        private void HandleInterruptedAttempt()
        {
            attemptActive = false;
            lastRoomId = string.Empty;
            ResetTransientEncounter();
            game.RequestModularDungeonSave();
        }

        private void RecoverToCheckpoint(string message)
        {
            ResetTransientEncounter();
            var room = definition.GetRoom(
                DungeonProgressRules.GetRecoveryRoomId(definition, progress));
            room ??= definition.GetRoom(definition.entranceRoomId);
            progress.currentRoomId = room.id;
            lastRoomId = string.Empty;
            game.Player.Teleport(transform.TransformPoint(room.safeLocalPosition));
            game.Notify(message);
            game.RequestModularDungeonSave();
        }

        private void ResetTransientEncounter()
        {
            for (var index = activeEnemies.Count - 1; index >= 0; index--)
            {
                if (activeEnemies[index] != null)
                {
                    UnityEngine.Object.Destroy(activeEnemies[index].gameObject);
                }
            }

            activeEnemies.Clear();
            if (activeBoss != null)
            {
                game?.ClearDungeonBoss(activeBoss);
                UnityEngine.Object.Destroy(activeBoss.gameObject);
                activeBoss = null;
            }

            replayActive = false;
            replayMarker?.Refresh();
        }

        private void PruneActiveEnemies()
        {
            activeEnemies.RemoveAll(enemy => enemy == null || enemy.IsDefeated);
        }

        private DungeonRoomDefinition GetNextProgressionRoom()
        {
            foreach (var room in definition.rooms)
            {
                if (room == null)
                {
                    continue;
                }

                if (room.kind == ModularDungeonRoomKind.Exit)
                {
                    if (progress.completed)
                    {
                        return room;
                    }

                    continue;
                }

                if (!progress.completedRoomIds.Contains(room.id))
                {
                    return room;
                }
            }

            return progress.completed
                ? definition.GetRoom(definition.exitRoomId)
                : definition.GetRoom(definition.bossRoomId);
        }

        private DungeonRoomDefinition FindRoomAt(Vector3 worldPosition)
        {
            var local = transform.InverseTransformPoint(worldPosition);
            DungeonRoomDefinition closest = null;
            var closestDistance = float.MaxValue;
            foreach (var room in definition.rooms)
            {
                if (room == null ||
                    Mathf.Abs(local.x - room.localCenter.x) > room.size.x * 0.5f ||
                    Mathf.Abs(local.z - room.localCenter.z) > room.size.z * 0.5f ||
                    local.y < -2f ||
                    local.y > room.size.y + 3f)
                {
                    continue;
                }

                var distance = Mathf.Abs(local.z - room.localCenter.z);
                if (distance < closestDistance)
                {
                    closest = room;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private bool ContainsInterior(Vector3 worldPosition)
        {
            var local = transform.InverseTransformPoint(worldPosition);
            var half = definition.localBoundsSize * 0.5f;
            var delta = local - definition.localBoundsCenter;
            return Mathf.Abs(delta.x) <= half.x &&
                   Mathf.Abs(delta.y) <= half.y &&
                   Mathf.Abs(delta.z) <= half.z;
        }

        private bool ContainsInteriorHorizontally(Vector3 worldPosition)
        {
            var local = transform.InverseTransformPoint(worldPosition);
            var half = definition.localBoundsSize * 0.5f;
            var delta = local - definition.localBoundsCenter;
            return Mathf.Abs(delta.x) <= half.x &&
                   Mathf.Abs(delta.z) <= half.z;
        }

        private DungeonPuzzleSaveData GetPuzzleState(string puzzleId)
        {
            return progress?.puzzles?.Find(state => state != null && state.id == puzzleId);
        }

        private DungeonMechanismDefinition GetNextPuzzleMechanism(
            DungeonPuzzleDefinition puzzle,
            DungeonPuzzleSaveData state)
        {
            if (puzzle == null || state == null || puzzle.mechanisms.Count == 0)
            {
                return null;
            }

            if (puzzle.kind == ModularDungeonPuzzleKind.OrderedSequence)
            {
                var step = Mathf.Clamp(state.values[0], 0, puzzle.solution.Length - 1);
                var index = puzzle.solution.Length > 0 ? puzzle.solution[step] : 0;
                return index >= 0 && index < puzzle.mechanisms.Count
                    ? puzzle.mechanisms[index]
                    : puzzle.mechanisms[0];
            }

            for (var index = 0; index < puzzle.mechanisms.Count; index++)
            {
                if (index >= state.values.Length ||
                    index >= puzzle.solution.Length ||
                    state.values[index] != puzzle.solution[index])
                {
                    return puzzle.mechanisms[index];
                }
            }

            return puzzle.mechanisms[0];
        }

        private static bool IsStateMatchSolved(
            DungeonPuzzleDefinition puzzle,
            DungeonPuzzleSaveData state)
        {
            if (puzzle == null ||
                state == null ||
                state.values == null ||
                puzzle.solution == null ||
                state.values.Length != puzzle.solution.Length)
            {
                return false;
            }

            for (var index = 0; index < state.values.Length; index++)
            {
                if (state.values[index] != puzzle.solution[index])
                {
                    return false;
                }
            }

            return state.values.Length > 0;
        }

        private static int CountMatchingMechanisms(
            DungeonPuzzleDefinition puzzle,
            DungeonPuzzleSaveData state)
        {
            if (puzzle == null || state == null || state.values == null)
            {
                return 0;
            }

            var count = 0;
            var length = Mathf.Min(state.values.Length, puzzle.solution.Length);
            for (var index = 0; index < length; index++)
            {
                if (state.values[index] == puzzle.solution[index])
                {
                    count++;
                }
            }

            return count;
        }

        private static string GetStateLabel(DungeonMechanismDefinition mechanism, int state)
        {
            return mechanism != null &&
                   mechanism.stateLabels != null &&
                   state >= 0 &&
                   state < mechanism.stateLabels.Length
                ? mechanism.stateLabels[state]
                : $"STATE {state + 1}";
        }

        private void RefreshWorldState()
        {
            DungeonProgressRules.Normalize(definition, progress);
            foreach (var door in doors)
            {
                door?.Refresh();
            }

            foreach (var pair in mechanisms)
            {
                pair.Value?.Refresh();
            }

            checkpoint?.Refresh();
            traversalLatch?.Refresh();
            exitPortal?.Refresh();
            replayMarker?.Refresh();
        }

        private DungeonPalette CreatePalette()
        {
            return new DungeonPalette
            {
                floor = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    new Color(0.11f, 0.2f, 0.23f),
                    0.05f,
                    0.36f),
                wall = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    new Color(0.08f, 0.13f, 0.17f),
                    0.08f,
                    0.24f),
                rib = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    new Color(0.38f, 0.26f, 0.17f),
                    0.42f,
                    0.38f),
                water = RuntimeEffectMaterialCache.GetTransparent(
                    game,
                    new Color(0.18f, 0.62f, 0.68f, 0.34f),
                    0.72f),
                dormant = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    new Color(0.2f, 0.31f, 0.36f),
                    0.16f,
                    0.5f),
                active = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    definition.accentColor,
                    0.18f,
                    0.72f),
                warning = RuntimeEffectMaterialCache.GetOpaque(
                    game,
                    new Color(0.9f, 0.55f, 0.18f),
                    0.12f,
                    0.56f),
                glass = RuntimeEffectMaterialCache.GetTransparent(
                    game,
                    new Color(0.58f, 0.9f, 0.96f, 0.48f),
                    0.85f)
            };
        }

        private void BuildInterior()
        {
            foreach (var room in definition.rooms)
            {
                if (room != null)
                {
                    BuildRoom(room);
                }
            }

            foreach (var connection in definition.connections)
            {
                if (connection != null)
                {
                    BuildConnection(connection);
                }
            }

            BuildOuterWall(-10f, 24f, "Threshold retaining wall");
            BuildOuterWall(216f, 24f, "Outflow retaining wall");
            foreach (var puzzle in definition.puzzles)
            {
                if (puzzle != null)
                {
                    BuildPuzzle(puzzle);
                }
            }

            BuildCheckpoint();
            BuildTraversal();
            BuildBossReservoir();
            BuildPortals();
            ValidateRuntimeBudget();
        }

        private void BuildRoom(DungeonRoomDefinition room)
        {
            CreatePrimitive(
                PrimitiveType.Cube,
                $"{room.displayName} floor",
                room.localCenter + Vector3.down * 0.55f,
                new Vector3(room.size.x, 1.1f, room.size.z),
                palette.floor,
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                $"{room.displayName} west wall",
                room.localCenter + new Vector3(-room.size.x * 0.5f, room.size.y * 0.5f, 0f),
                new Vector3(1f, room.size.y, room.size.z),
                palette.wall,
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                $"{room.displayName} east wall",
                room.localCenter + new Vector3(room.size.x * 0.5f, room.size.y * 0.5f, 0f),
                new Vector3(1f, room.size.y, room.size.z),
                palette.wall,
                true);

            CreatePrimitive(
                PrimitiveType.Cube,
                $"{room.displayName} west watercourse",
                room.localCenter + new Vector3(-room.size.x * 0.34f, 0.03f, 0f),
                new Vector3(2.3f, 0.05f, room.size.z * 0.86f),
                palette.water,
                false);
            CreatePrimitive(
                PrimitiveType.Cube,
                $"{room.displayName} east watercourse",
                room.localCenter + new Vector3(room.size.x * 0.34f, 0.03f, 0f),
                new Vector3(2.3f, 0.05f, room.size.z * 0.86f),
                palette.water,
                false);

            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    $"{room.displayName} copper depth rib {side}",
                    room.localCenter +
                    new Vector3(side * (room.size.x * 0.5f - 0.62f), room.size.y * 0.58f, 0f),
                    new Vector3(0.28f, room.size.y * 0.76f, 0.5f),
                    palette.rib,
                    false);
            }

            var anchorObject = new GameObject($"{room.displayName} guidance anchor");
            anchorObject.transform.SetParent(transform, false);
            anchorObject.transform.localPosition = room.safeLocalPosition;
            roomAnchors[room.id] = anchorObject.transform;
            AddLabel(
                anchorObject.transform,
                room.displayName,
                new Vector3(0f, room.size.y - 1.2f, 0f),
                new Color(0.67f, 0.9f, 0.9f),
                0.09f);
        }

        private void BuildConnection(DungeonConnectionDefinition connection)
        {
            var segmentWidth = Mathf.Max(0.5f, (connection.wallWidth - DoorGap) * 0.5f);
            var offset = DoorGap * 0.5f + segmentWidth * 0.5f;
            CreatePrimitive(
                PrimitiveType.Cube,
                $"{connection.id} west frame",
                connection.doorLocalPosition + Vector3.left * offset,
                new Vector3(segmentWidth, 7f, 1f),
                palette.wall,
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                $"{connection.id} east frame",
                connection.doorLocalPosition + Vector3.right * offset,
                new Vector3(segmentWidth, 7f, 1f),
                palette.wall,
                true);

            var doorObject = CreatePrimitive(
                PrimitiveType.Cube,
                $"{connection.id} seal",
                connection.doorLocalPosition,
                connection.doorScale,
                palette.dormant,
                true);
            var door = doorObject.AddComponent<DungeonDoor>();
            door.Initialize(this, connection);
            doors.Add(door);
        }

        private void BuildOuterWall(float localZ, float width, string wallName)
        {
            CreatePrimitive(
                PrimitiveType.Cube,
                wallName,
                new Vector3(0f, 3.5f, localZ),
                new Vector3(width, 7f, 1f),
                palette.wall,
                true);
        }

        private void BuildPuzzle(DungeonPuzzleDefinition puzzle)
        {
            var room = definition.GetRoom(puzzle.roomId);
            if (room == null)
            {
                return;
            }

            var tablet = CreatePrimitive(
                PrimitiveType.Cube,
                $"{puzzle.displayName} carved instruction",
                room.localCenter + new Vector3(0f, 0.08f, -room.size.z * 0.32f),
                new Vector3(Mathf.Min(14f, room.size.x - 4f), 0.14f, 2.8f),
                palette.rib,
                false);
            AddLabel(
                tablet.transform,
                puzzle.clue,
                new Vector3(0f, 0.9f, 0f),
                new Color(0.73f, 0.94f, 0.91f),
                0.048f);

            for (var index = 0; index < puzzle.mechanisms.Count; index++)
            {
                var mechanismDefinition = puzzle.mechanisms[index];
                var root = CreatePrimitive(
                    PrimitiveType.Cylinder,
                    mechanismDefinition.displayName,
                    mechanismDefinition.localPosition,
                    new Vector3(0.92f, 1.5f, 0.92f),
                    palette.dormant,
                    true);

                var shapeType = index % 3 == 0
                    ? PrimitiveType.Cube
                    : index % 3 == 1
                        ? PrimitiveType.Sphere
                        : PrimitiveType.Cylinder;
                var shape = CreatePrimitive(
                    shapeType,
                    $"{mechanismDefinition.displayName} tactile glyph",
                    mechanismDefinition.localPosition + Vector3.up * 1.95f,
                    shapeType == PrimitiveType.Cube
                        ? new Vector3(0.55f, 0.55f, 0.2f)
                        : Vector3.one * 0.58f,
                    palette.glass,
                    false);
                shape.transform.localRotation = shapeType == PrimitiveType.Cylinder
                    ? Quaternion.Euler(90f, 0f, 0f)
                    : Quaternion.identity;

                Renderer levelRenderer = null;
                if (puzzle.kind == ModularDungeonPuzzleKind.StateMatch)
                {
                    var level = CreatePrimitive(
                        PrimitiveType.Cube,
                        $"{mechanismDefinition.displayName} water level",
                        mechanismDefinition.localPosition + new Vector3(0f, 0.2f, -0.63f),
                        new Vector3(0.48f, 0.2f, 0.12f),
                        palette.water,
                        false);
                    levelRenderer = level.GetComponent<Renderer>();
                    for (var mark = 0; mark < 3; mark++)
                    {
                        CreatePrimitive(
                            PrimitiveType.Cube,
                            $"{mechanismDefinition.displayName} etched level {mark + 1}",
                            mechanismDefinition.localPosition +
                            new Vector3(0.55f, -0.55f + mark * 0.55f, -0.64f),
                            new Vector3(0.2f + mark * 0.08f, 0.08f, 0.08f),
                            palette.rib,
                            false);
                    }
                }

                AddLabel(
                    root.transform,
                    mechanismDefinition.displayName,
                    new Vector3(0f, 2.25f, 0f),
                    new Color(0.72f, 0.9f, 0.92f),
                    0.055f);
                var mechanism = root.AddComponent<DungeonMechanism>();
                mechanism.Initialize(
                    this,
                    puzzle,
                    mechanismDefinition,
                    index,
                    root.GetComponent<Renderer>(),
                    levelRenderer);
                game.RegisterInteractable(mechanism);
                mechanisms[mechanismDefinition.id] = mechanism;
            }
        }

        private void BuildCheckpoint()
        {
            if (definition.checkpoints.Count == 0)
            {
                return;
            }

            var checkpointDefinition = definition.checkpoints[0];
            var root = CreatePrimitive(
                PrimitiveType.Cylinder,
                checkpointDefinition.displayName,
                checkpointDefinition.localPosition,
                new Vector3(1.15f, 1.3f, 1.15f),
                palette.dormant,
                true);
            var crown = CreatePrimitive(
                PrimitiveType.Sphere,
                "Keeper's Threadglass crown",
                checkpointDefinition.localPosition + Vector3.up * 1.7f,
                Vector3.one * 0.72f,
                palette.glass,
                false);
            crown.transform.localScale = new Vector3(0.72f, 0.42f, 0.72f);
            AddLabel(
                root.transform,
                checkpointDefinition.displayName,
                new Vector3(0f, 2.7f, 0f),
                new Color(0.7f, 0.95f, 0.9f),
                0.075f);
            checkpoint = root.AddComponent<DungeonCheckpoint>();
            checkpoint.Initialize(
                this,
                checkpointDefinition,
                root.GetComponent<Renderer>());
            game.RegisterInteractable(checkpoint);
        }

        private void BuildTraversal()
        {
            var room = definition.rooms.Find(
                candidate => candidate != null &&
                             candidate.kind == ModularDungeonRoomKind.Traversal);
            if (room == null)
            {
                return;
            }

            var wallZ = room.localCenter.z;
            var deckZ = wallZ + room.size.z * 0.16f;
            var liftZ = wallZ - room.size.z * 0.21f;
            var latchZ = wallZ + room.size.z * 0.26f;
            var climbFace = CreatePrimitive(
                PrimitiveType.Cube,
                "Glasswater marked spillway face",
                new Vector3(room.localCenter.x, 2.4f, wallZ),
                new Vector3(room.size.x - 4f, 4.8f, 1.8f),
                palette.wall,
                true);
            climbFace.AddComponent<ClimbableSurface>();
            CreatePrimitive(
                PrimitiveType.Cube,
                "Glasswater upper spillway deck",
                new Vector3(room.localCenter.x, 4.55f, deckZ),
                new Vector3(room.size.x - 4f, 1f, room.size.z * 0.29f),
                palette.floor,
                true);

            var updraft = CreatePrimitive(
                PrimitiveType.Cylinder,
                "Glasswater pressure lift",
                new Vector3(
                    room.localCenter.x - room.size.x * 0.25f,
                    2.4f,
                    liftZ),
                new Vector3(2.15f, 2.4f, 2.15f),
                palette.water,
                true);
            updraft.AddComponent<WindLift>();
            updraft.AddComponent<RouteGlow>().Initialize(0.06f, 0.42f);
            AddLabel(
                updraft.transform,
                "Pressure lift · climbable face alternate",
                new Vector3(0f, 2.8f, 0f),
                new Color(0.66f, 0.95f, 0.9f),
                0.058f);

            var latchObject = CreatePrimitive(
                PrimitiveType.Cylinder,
                "Upper spillway release",
                new Vector3(room.localCenter.x, 5.75f, latchZ),
                new Vector3(0.85f, 1.2f, 0.85f),
                palette.dormant,
                true);
            AddLabel(
                latchObject.transform,
                "Upper spillway release",
                new Vector3(0f, 1.8f, 0f),
                new Color(0.76f, 0.92f, 0.91f),
                0.065f);
            traversalLatch = latchObject.AddComponent<DungeonTraversalLatch>();
            traversalLatch.Initialize(this, latchObject.GetComponent<Renderer>());
            game.RegisterInteractable(traversalLatch);
        }

        private void BuildBossReservoir()
        {
            var room = definition.GetRoom(definition.bossRoomId);
            if (room == null)
            {
                return;
            }

            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Mirror Reservoir boss dais",
                room.localCenter + new Vector3(0f, 0.18f, 4f),
                new Vector3(8.5f, 0.2f, 8.5f),
                palette.glass,
                false);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    $"Mirror Reservoir reflected channel {side}",
                    room.localCenter + new Vector3(side * 8f, 0.08f, 3f),
                    new Vector3(3.5f, 0.08f, 30f),
                    palette.water,
                    false);
            }

            var anchor = new GameObject("Glasswater Regent spawn anchor");
            anchor.transform.SetParent(transform, false);
            anchor.transform.localPosition = room.localCenter + new Vector3(0f, 1.6f, 4f);
            bossAnchor = anchor.transform;
        }

        private void BuildPortals()
        {
            var entranceRoom = definition.GetRoom(definition.entranceRoomId);
            var exitRoom = definition.GetRoom(definition.exitRoomId);
            var bossRoom = definition.GetRoom(definition.bossRoomId);
            if (entranceRoom == null || exitRoom == null || bossRoom == null)
            {
                return;
            }

            var retreatObject = CreatePrimitive(
                PrimitiveType.Cylinder,
                "Glasswater emergency return",
                entranceRoom.localCenter +
                new Vector3(
                    entranceRoom.size.x * 0.33f,
                    1.2f,
                    -entranceRoom.size.z * 0.25f),
                new Vector3(1.35f, 1.2f, 1.35f),
                palette.warning,
                true);
            AddLabel(
                retreatObject.transform,
                "Emergency return",
                new Vector3(0f, 1.8f, 0f),
                new Color(0.98f, 0.76f, 0.38f),
                0.06f);
            var retreatPortal = retreatObject.AddComponent<DungeonPortal>();
            retreatPortal.Initialize(this, false);
            game.RegisterInteractable(retreatPortal);

            var exitObject = CreatePrimitive(
                PrimitiveType.Cylinder,
                "Glasswater Outflow Loom",
                exitRoom.localCenter + new Vector3(0f, 1.3f, exitRoom.size.z * 0.08f),
                new Vector3(1.8f, 1.3f, 1.8f),
                palette.active,
                true);
            AddLabel(
                exitObject.transform,
                "Outflow Loom · return to Morrowfen",
                new Vector3(0f, 2.2f, 0f),
                new Color(0.72f, 0.98f, 0.9f),
                0.07f);
            exitPortal = exitObject.AddComponent<DungeonPortal>();
            exitPortal.Initialize(this, true);
            game.RegisterInteractable(exitPortal);

            var replayObject = CreatePrimitive(
                PrimitiveType.Cylinder,
                "Glasswater Regent echo lens",
                bossRoom.localCenter +
                new Vector3(
                    bossRoom.size.x * 0.24f,
                    1.2f,
                    bossRoom.size.z * 0.3f),
                new Vector3(1.15f, 1.2f, 1.15f),
                palette.glass,
                true);
            AddLabel(
                replayObject.transform,
                "Recall Regent echo · no first-clear rewards",
                new Vector3(0f, 2f, 0f),
                new Color(0.72f, 0.91f, 0.98f),
                0.055f);
            replayMarker = replayObject.AddComponent<DungeonReplayMarker>();
            replayMarker.Initialize(this);
            game.RegisterInteractable(replayMarker);
        }

        private GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool solid)
        {
            var root = GameObject.CreatePrimitive(primitiveType);
            root.name = objectName;
            root.transform.SetParent(transform, false);
            root.transform.localPosition = localPosition;
            root.transform.localScale = localScale;
            root.GetComponent<Renderer>().sharedMaterial = material;
            authoredRendererCount++;
            var collider = root.GetComponent<Collider>();
            if (solid)
            {
                authoredColliderCount++;
            }
            else if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            return root;
        }

        private void AddLabel(
            Transform parent,
            string text,
            Vector3 localPosition,
            Color color,
            float characterSize)
        {
            WorldVisuals.AddLabel(parent, text, localPosition, color, characterSize);
            authoredRendererCount++;
        }

        private void ValidateRuntimeBudget()
        {
            if (authoredRendererCount > definition.rendererBudget ||
                authoredColliderCount > definition.colliderBudget ||
                authoredLightCount > definition.addedLightBudget)
            {
                Debug.LogError(
                    $"{definition.displayName} authored geometry exceeds its catalog budget: " +
                    $"{authoredRendererCount}/{definition.rendererBudget} renderers, " +
                    $"{authoredColliderCount}/{definition.colliderBudget} colliders, " +
                    $"{authoredLightCount}/{definition.addedLightBudget} lights.");
            }
        }

        private void RefreshPuzzleVisual(
            DungeonPuzzleDefinition puzzle,
            DungeonMechanismDefinition mechanism,
            int mechanismIndex,
            Renderer body,
            Renderer level)
        {
            var state = GetPuzzleState(puzzle.id);
            if (state == null)
            {
                return;
            }

            var current = puzzle.kind == ModularDungeonPuzzleKind.OrderedSequence
                ? IsOrderedMechanismLatched(puzzle, state, mechanismIndex) ? 1 : 0
                : mechanismIndex < state.values.Length ? state.values[mechanismIndex] : 0;
            var matches = puzzle.kind == ModularDungeonPuzzleKind.OrderedSequence
                ? current > 0
                : mechanismIndex < puzzle.solution.Length &&
                  current == puzzle.solution[mechanismIndex];
            var original = state.solved
                ? definition.accentColor
                : matches
                    ? new Color(0.34f, 0.78f, 0.7f)
                    : new Color(0.24f, 0.38f, 0.44f);
            var cue = state.solved || matches
                ? TetherfallAccessibilityColorCue.Positive
                : TetherfallAccessibilityColorCue.Interaction;
            SetRendererColor(body, TetherfallAccessibilityRuntime.ResolveColorCue(original, cue));
            if (level != null)
            {
                var normalized = (current + 1f) / Mathf.Max(1f, mechanism.stateCount);
                var levelTransform = level.transform;
                levelTransform.localScale = new Vector3(0.48f, 0.18f + normalized * 1.25f, 0.12f);
                var local = mechanism.localPosition + new Vector3(0f, -0.55f + levelTransform.localScale.y * 0.5f, -0.63f);
                levelTransform.localPosition = local;
                SetRendererColor(
                    level,
                    TetherfallAccessibilityRuntime.ResolveColorCue(
                        definition.accentColor,
                        matches
                            ? TetherfallAccessibilityColorCue.Positive
                            : TetherfallAccessibilityColorCue.Interaction));
            }
        }

        private static bool IsOrderedMechanismLatched(
            DungeonPuzzleDefinition puzzle,
            DungeonPuzzleSaveData state,
            int mechanismIndex)
        {
            var step = state.values != null && state.values.Length > 0 ? state.values[0] : 0;
            for (var index = 0; index < Mathf.Min(step, puzzle.solution.Length); index++)
            {
                if (puzzle.solution[index] == mechanismIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        private void PlayPulse(Vector3 localPosition, Color color, PrimitiveType shape)
        {
            PlayWorldPulse(transform.TransformPoint(localPosition), color, shape, 4.5f);
        }

        private void PlayWorldPulse(
            Vector3 worldPosition,
            Color color,
            PrimitiveType shape,
            float targetScale)
        {
            var resolved = TetherfallAccessibilityRuntime.ResolveTelegraphColor(
                new Color(color.r, color.g, color.b, 0.62f),
                TetherfallAccessibilityColorCue.Positive);
            TimedPulse.Spawn(
                shape,
                $"{definition.displayName} mechanism pulse",
                new Vector3(worldPosition.x, worldPosition.y + 0.1f, worldPosition.z),
                shape == PrimitiveType.Cube
                    ? new Vector3(0.6f, 0.08f, 0.6f)
                    : new Vector3(0.8f, 0.06f, 0.8f),
                RuntimeEffectMaterialCache.GetTransparent(game, resolved, 0.68f),
                0.72f,
                targetScale);
        }

        private static void AddUnique(List<string> ids, string id)
        {
            if (!string.IsNullOrEmpty(id) && !ids.Contains(id))
            {
                ids.Add(id);
            }
        }

        private static void RemoveFirst(List<string> ids, string id)
        {
            var index = ids.IndexOf(id);
            if (index >= 0)
            {
                ids.RemoveAt(index);
            }
        }

        public sealed class DungeonDoor : MonoBehaviour
        {
            private ModularDungeonDirector dungeon;
            private DungeonConnectionDefinition connection;

            public void Initialize(
                ModularDungeonDirector owner,
                DungeonConnectionDefinition definition)
            {
                dungeon = owner;
                connection = definition;
            }

            public void Refresh()
            {
                if (dungeon == null || connection == null || dungeon.progress == null)
                {
                    return;
                }

                gameObject.SetActive(
                    !dungeon.progress.completed &&
                    !dungeon.progress.completedRoomIds.Contains(connection.fromRoomId));
            }
        }

        public sealed class DungeonMechanism : MonoBehaviour, IWorldInteractable
        {
            private ModularDungeonDirector dungeon;
            private DungeonPuzzleDefinition puzzle;
            private DungeonMechanismDefinition definition;
            private int mechanismIndex;
            private Renderer body;
            private Renderer level;

            public string InteractionPrompt =>
                dungeon != null
                    ? dungeon.GetPuzzlePrompt(puzzle.id, mechanismIndex)
                    : "Inspect sluice mechanism";

            public void Initialize(
                ModularDungeonDirector owner,
                DungeonPuzzleDefinition puzzleDefinition,
                DungeonMechanismDefinition mechanismDefinition,
                int index,
                Renderer bodyRenderer,
                Renderer levelRenderer)
            {
                dungeon = owner;
                puzzle = puzzleDefinition;
                definition = mechanismDefinition;
                mechanismIndex = index;
                body = bodyRenderer;
                level = levelRenderer;
            }

            public bool CanInteract(TetherfallGame owner)
            {
                return dungeon != null && dungeon.CanUsePuzzle(puzzle.id);
            }

            public void Interact(TetherfallGame owner)
            {
                dungeon?.ActivatePuzzleMechanism(puzzle.id, mechanismIndex);
            }

            public void Refresh()
            {
                if (dungeon != null && puzzle != null && definition != null)
                {
                    dungeon.RefreshPuzzleVisual(
                        puzzle,
                        definition,
                        mechanismIndex,
                        body,
                        level);
                }
            }
        }
    }

    public sealed class ModularDungeonGate : MonoBehaviour, IWorldInteractable
    {
        private ModularDungeonDirector dungeon;

        public string InteractionPrompt =>
            dungeon != null ? dungeon.GetGatePrompt() : "Inspect dungeon entrance";

        public static ModularDungeonGate Create(
            TetherfallGame game,
            ModularDungeonDirector dungeon,
            ModularDungeonDefinition definition)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = $"{definition.displayName} authored entrance";
            root.transform.position = definition.gatePosition;
            root.transform.localScale = new Vector3(2.15f, 2.6f, 2.15f);
            root.GetComponent<Renderer>().sharedMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                new Color(0.18f, 0.48f, 0.52f),
                0.22f,
                0.66f);
            var gate = root.AddComponent<ModularDungeonGate>();
            gate.dungeon = dungeon;
            game.RegisterInteractable(gate);

            var stone = RuntimeEffectMaterialCache.GetOpaque(
                game,
                new Color(0.13f, 0.21f, 0.24f),
                0.04f,
                0.3f);
            for (var side = -1; side <= 1; side += 2)
            {
                var pylon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pylon.name = $"Glasswater intake pylon {side}";
                pylon.transform.SetParent(root.transform, false);
                pylon.transform.localPosition = new Vector3(side * 1.75f, 0.35f, 0f);
                pylon.transform.localScale = new Vector3(0.55f, 1.75f, 0.7f);
                pylon.GetComponent<Renderer>().sharedMaterial = stone;
            }

            var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintel.name = "Glasswater intake copper lintel";
            lintel.transform.SetParent(root.transform, false);
            lintel.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            lintel.transform.localScale = new Vector3(2.15f, 0.24f, 0.62f);
            lintel.GetComponent<Renderer>().sharedMaterial = RuntimeEffectMaterialCache.GetOpaque(
                game,
                new Color(0.52f, 0.32f, 0.17f),
                0.45f,
                0.48f);
            WorldVisuals.AddLabel(
                root.transform,
                "GLASSWATER SLUICE\nFenlight keeper intake",
                new Vector3(0f, 2.15f, 0f),
                new Color(0.67f, 0.94f, 0.91f),
                0.085f);
            return gate;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return dungeon != null;
        }

        public void Interact(TetherfallGame owner)
        {
            dungeon?.Enter();
        }
    }

    public sealed class DungeonCheckpoint : MonoBehaviour, IWorldInteractable
    {
        private ModularDungeonDirector dungeon;
        private DungeonCheckpointDefinition definition;
        private Renderer body;

        public string InteractionPrompt =>
            dungeon != null
                ? dungeon.GetCheckpointPrompt(definition.id)
                : "Inspect checkpoint";

        public void Initialize(
            ModularDungeonDirector owner,
            DungeonCheckpointDefinition checkpointDefinition,
            Renderer bodyRenderer)
        {
            dungeon = owner;
            definition = checkpointDefinition;
            body = bodyRenderer;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return dungeon != null && dungeon.CanActivateCheckpoint(definition.id);
        }

        public void Interact(TetherfallGame owner)
        {
            dungeon?.ActivateCheckpoint(definition.id);
        }

        public void Refresh()
        {
            if (dungeon == null || body == null)
            {
                return;
            }

            var active = dungeon.Progress != null &&
                         dungeon.Progress.checkpointId == definition.id;
            var color = TetherfallAccessibilityRuntime.ResolveColorCue(
                active ? dungeon.Definition.accentColor : new Color(0.22f, 0.34f, 0.38f),
                active
                    ? TetherfallAccessibilityColorCue.Positive
                    : TetherfallAccessibilityColorCue.Interaction);
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            body.SetPropertyBlock(block);
        }
    }

    public sealed class DungeonTraversalLatch : MonoBehaviour, IWorldInteractable
    {
        private ModularDungeonDirector dungeon;
        private Renderer body;

        public string InteractionPrompt =>
            dungeon != null && dungeon.CanReleaseTraversal()
                ? "Release the upper spillway latch"
                : "Upper spillway already released";

        public void Initialize(ModularDungeonDirector owner, Renderer bodyRenderer)
        {
            dungeon = owner;
            body = bodyRenderer;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return dungeon != null && dungeon.CanReleaseTraversal();
        }

        public void Interact(TetherfallGame owner)
        {
            dungeon?.ReleaseTraversal();
        }

        public void Refresh()
        {
            if (dungeon == null || body == null)
            {
                return;
            }

            var released = dungeon.Progress != null &&
                           !string.IsNullOrEmpty(dungeon.TraversalRoomId) &&
                           dungeon.Progress.completedRoomIds.Contains(
                               dungeon.TraversalRoomId);
            var color = TetherfallAccessibilityRuntime.ResolveColorCue(
                released ? dungeon.Definition.accentColor : new Color(0.9f, 0.55f, 0.18f),
                released
                    ? TetherfallAccessibilityColorCue.Positive
                    : TetherfallAccessibilityColorCue.Interaction);
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            body.SetPropertyBlock(block);
        }
    }

    public sealed class DungeonPortal : MonoBehaviour, IWorldInteractable
    {
        private ModularDungeonDirector dungeon;
        private bool completionRequired;

        public string InteractionPrompt => completionRequired
            ? "Leave through the Outflow Loom"
            : "Retreat to the Glasswater approach";

        public void Initialize(ModularDungeonDirector owner, bool requireCompletion)
        {
            dungeon = owner;
            completionRequired = requireCompletion;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return dungeon != null && dungeon.CanUseExit(completionRequired);
        }

        public void Interact(TetherfallGame owner)
        {
            dungeon?.Exit();
        }

        public void Refresh()
        {
            gameObject.SetActive(
                dungeon != null &&
                (!completionRequired || dungeon.IsComplete));
        }
    }

    public sealed class DungeonReplayMarker : MonoBehaviour, IWorldInteractable
    {
        private ModularDungeonDirector dungeon;

        public string InteractionPrompt => "Recall the Glasswater Regent echo";

        public void Initialize(ModularDungeonDirector owner)
        {
            dungeon = owner;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return dungeon != null && dungeon.CanReplayBoss();
        }

        public void Interact(TetherfallGame owner)
        {
            dungeon?.StartBossReplay();
        }

        public void Refresh()
        {
            gameObject.SetActive(dungeon != null && dungeon.IsComplete);
        }
    }
}
