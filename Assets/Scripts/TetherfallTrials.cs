using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public interface IOptionalTrial
    {
        string TrialId { get; }
        TrialSaveData WriteSaveData();
        void RestoreSaveData(TrialSaveData data);
    }

    public sealed partial class TetherfallGame
    {
        private readonly Dictionary<string, IOptionalTrial> optionalTrials =
            new Dictionary<string, IOptionalTrial>();

        internal void RegisterTrial(IOptionalTrial trial)
        {
            if (trial != null && !string.IsNullOrEmpty(trial.TrialId))
            {
                optionalTrials[trial.TrialId] = trial;
            }
        }

        internal void RequestTrialSave()
        {
            RequestSave();
        }

        private void WriteTrialSave(TetherfallSaveData data)
        {
            if (data == null)
            {
                return;
            }

            if (data.trials == null)
            {
                data.trials = new List<TrialSaveData>();
            }
            else
            {
                data.trials.Clear();
            }

            foreach (var trial in optionalTrials.Values)
            {
                var trialData = trial.WriteSaveData();
                if (trialData != null)
                {
                    data.trials.Add(trialData);
                }
            }
        }

        private void RestoreTrialState(TetherfallSaveData data)
        {
            if (data == null || data.trials == null)
            {
                return;
            }

            var restoredTrialIds = new HashSet<string>();
            foreach (var trialData in data.trials)
            {
                if (trialData != null &&
                    !string.IsNullOrEmpty(trialData.id) &&
                    restoredTrialIds.Add(trialData.id) &&
                    optionalTrials.TryGetValue(trialData.id, out var trial))
                {
                    trial.RestoreSaveData(trialData);
                }
            }
        }
    }

    public static class TrialBuilder
    {
        public static void Build(TetherfallGame game)
        {
            var root = new GameObject("Ashwake Crucible Director");
            var trial = root.AddComponent<AshwakeTrial>();
            trial.Initialize(game, TrialCatalog.AshwakeCrucible);
            TrialGate.Create(game, trial, TrialCatalog.AshwakeCrucible.gatePosition);
        }
    }

    public sealed class TrialGate : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private AshwakeTrial trial;
        private Renderer body;

        public string InteractionPrompt => trial != null && trial.CanEnter
            ? trial.IsComplete
                ? "Enter the quiet Ashwake Crucible"
                : "Enter the Ashwake Crucible"
            : "Inspect the dormant Ashwake bell";

        public static TrialGate Create(TetherfallGame game, AshwakeTrial trial, Vector3 position)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "Ashwake Crucible bell";
            root.transform.position = position;
            root.transform.localScale = new Vector3(2.8f, 3.2f, 2.8f);
            var gate = root.AddComponent<TrialGate>();
            gate.game = game;
            gate.trial = trial;
            gate.body = root.GetComponent<Renderer>();
            gate.body.material = game.CreateMaterial(new Color(0.42f, 0.18f, 0.1f), 0.28f, 0.58f);
            WorldVisuals.AddLabel(
                root.transform,
                "Ashwake Crucible",
                new Vector3(0f, 3.8f, 0f),
                new Color(1f, 0.65f, 0.31f),
                0.1f);
            game.RegisterInteractable(gate);
            return gate;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            if (trial == null || !trial.CanEnter)
            {
                if (owner != null)
                {
                    owner.Notify("The Ashwake bell needs a stabilized Skyvault route before it will answer.");
                }

                return;
            }

            trial.Enter();
        }
    }

    public sealed class AshwakeTrial : MonoBehaviour, IOptionalTrial, IDungeonBossArena
    {
        private const float WaveTransitionDelay = 2.2f;
        private const float BossTransitionDelay = 3f;
        private const float RecoveryCheckInterval = 0.6f;
        private const float ArenaHalfWidth = 31f;
        private const float ArenaHalfDepth = 40f;
        private static readonly Vector3 ArenaCenter = new Vector3(-160f, 0f, -222f);
        private static readonly string[][] SurgeWaves =
        {
            new[] { "enemy.cinder_echo", "enemy.cinder_echo", "enemy.vault_wisp", "enemy.vault_wisp" },
            new[] { "enemy.relay_breaker", "enemy.vault_wisp", "enemy.cinder_echo", "enemy.relay_breaker", "enemy.vault_wisp" },
            new[] { "enemy.ashjaw_brute", "enemy.cinder_echo", "enemy.relay_breaker", "enemy.vault_wisp", "enemy.cinder_echo" }
        };

        private readonly List<AshflowDial> dials = new List<AshflowDial>();
        private readonly List<GameObject> windLifts = new List<GameObject>();
        private readonly List<ReliefLatch> reliefLatches = new List<ReliefLatch>();
        private readonly List<Enemy> activeEnemies = new List<Enemy>();
        private readonly List<string> remainingEnemyIds = new List<string>();

        private TetherfallGame game;
        private TrialDefinition definition;
        private TrialState state = TrialState.Locked;
        private TrialCore core;
        private Renderer coreRenderer;
        private DungeonBoss boss;
        private int activeWaveIndex;
        private bool bossDefeated;
        private bool rewardGranted;
        private bool coreSealed;
        private bool attemptActive;
        private bool transitionPending;
        private bool transitionToBoss;
        private bool saveReconciliationPending;
        private float transitionAt;
        private float nextRecoveryCheckAt;
        private int activeReliefLatchIndex = -1;

        public string TrialId => definition != null ? definition.id : string.Empty;
        public bool CanEnter => game != null && game.SkyvaultRouteStabilized;
        public bool IsComplete => state == TrialState.Complete;
        public bool CanAdjustDials => state == TrialState.AligningFlows && !AreFlowsAligned();

        public void Initialize(TetherfallGame owner, TrialDefinition trialDefinition)
        {
            game = owner;
            definition = trialDefinition;
            BuildArena();
            game.RegisterTrial(this);
        }

        private void Update()
        {
            if (game == null || game.Player == null)
            {
                return;
            }

            if (saveReconciliationPending)
            {
                saveReconciliationPending = false;
                game.RequestTrialSave();
            }

            if (!attemptActive)
            {
                return;
            }

            if (!IsPlayerInsideArena())
            {
                HandleInterruptedAttempt();
                return;
            }

            if (game.GameplayPaused)
            {
                if (transitionPending)
                {
                    transitionAt += Time.deltaTime;
                }

                return;
            }

            if (transitionPending && Time.time >= transitionAt)
            {
                var startBoss = transitionToBoss;
                CancelTransition();
                if (startBoss && state == TrialState.Boss)
                {
                    StartBoss();
                }
                else if (!startBoss && state == TrialState.Surge)
                {
                    SpawnCurrentWave();
                }

                game.RequestTrialSave();
                return;
            }

            if (Time.time < nextRecoveryCheckAt)
            {
                return;
            }

            nextRecoveryCheckAt = Time.time + RecoveryCheckInterval;
            RecoverActiveEncounter();
        }

        public void Enter()
        {
            if (!CanEnter || game.Player == null)
            {
                return;
            }

            attemptActive = true;
            CancelTransition();
            game.Player.Teleport(definition.entrancePosition);
            if (state == TrialState.Locked)
            {
                state = TrialState.AligningFlows;
            }

            var encounterAnnounced = false;
            PruneActiveEnemies();
            if (state == TrialState.Surge && activeEnemies.Count == 0)
            {
                SpawnCurrentWave();
                encounterAnnounced = true;
            }
            else if (state == TrialState.Boss && !bossDefeated)
            {
                StartBoss();
                encounterAnnounced = true;
            }

            if (!encounterAnnounced)
            {
                game.Notify(GetEntryMessage());
            }

            game.RequestTrialSave();
        }

        public void Exit()
        {
            if (game == null)
            {
                return;
            }

            attemptActive = false;
            CancelTransition();
            ResetActiveCombat();
            if (game.Player != null)
            {
                game.Player.Teleport(definition.returnPosition);
            }

            game.Notify(GetRetreatMessage());
            game.RequestTrialSave();
        }

        public void OnDialAdjusted(AshflowDial dial)
        {
            if (state != TrialState.AligningFlows || dial == null)
            {
                return;
            }

            var alignedFlows = CountAlignedFlows();
            if (alignedFlows < dials.Count)
            {
                var status = dial.IsAligned ? "matches its vent" : "does not match its vent";
                game.Notify(
                    $"{dial.name}: glyph {dial.State + 1} {status}. " +
                    $"{alignedFlows}/{dials.Count} ashflows aligned.");
                game.RequestTrialSave();
                return;
            }

            SetWindLiftsActive(true);
            RefreshCoreVisual();
            game.Notify("The ashflows align. Threadwind lifts open. Ignite the core.");
            game.RequestTrialSave();
        }

        public bool CanIgnite => state == TrialState.AligningFlows && AreFlowsAligned();

        public string GetCorePrompt()
        {
            if (state == TrialState.AligningFlows)
            {
                return AreFlowsAligned()
                    ? "Ignite the Ashwake surge"
                    : $"Align ashflows: {CountAlignedFlows()}/{dials.Count}";
            }

            if (transitionPending)
            {
                return transitionToBoss
                    ? "Scoria Harrower incoming"
                    : $"Next surge forming: {activeWaveIndex + 1}/{SurgeWaves.Length}";
            }

            if (state == TrialState.Surge)
            {
                return
                    $"Surge {activeWaveIndex + 1}/{SurgeWaves.Length}: " +
                    DescribeEchoes(remainingEnemyIds.Count);
            }

            if (state == TrialState.Boss)
            {
                return coreSealed
                    ? "Core sealed: vent the lit relief latch"
                    : "Scoria Harrower core exposed";
            }

            return state == TrialState.Complete
                ? "The Ashwake Crucible is quiet"
                : "The Ashwake core is dormant";
        }

        public void InteractWithCore()
        {
            if (game == null)
            {
                return;
            }

            if (!CanIgnite)
            {
                if (transitionPending)
                {
                    game.Notify(
                        transitionToBoss
                            ? "The pressure rings are clear. Scoria Harrower is entering the forge."
                            : $"Surge {activeWaveIndex + 1} is forming.");
                    return;
                }

                switch (state)
                {
                    case TrialState.AligningFlows:
                        game.Notify(
                            $"{CountAlignedFlows()}/{dials.Count} ashflows aligned. " +
                            "Match each dial to its carved vent glyph.");
                        break;
                    case TrialState.Surge:
                        game.Notify($"{DescribeRemainingEchoes(remainingEnemyIds.Count)} in this surge.");
                        break;
                    case TrialState.Boss:
                        game.Notify(
                            coreSealed
                                ? "The Harrower's core is sealed. Vent the lit relief latch."
                                : "The Harrower's core is exposed. Keep pressure on it.");
                        break;
                    case TrialState.Complete:
                        game.Notify("The Crucible is stable. The Threadwind lifts remain open.");
                        break;
                    default:
                        game.Notify("The Ashwake core is dormant.");
                        break;
                }

                return;
            }

            state = TrialState.Surge;
            activeWaveIndex = 0;
            remainingEnemyIds.Clear();
            remainingEnemyIds.AddRange(SurgeWaves[activeWaveIndex]);
            attemptActive = true;
            SetWindLiftsActive(true);
            RefreshCoreVisual();
            SpawnCurrentWave();
            game.RequestTrialSave();
        }

        public bool CanDamageBoss(DungeonBoss activeBoss)
        {
            return attemptActive &&
                   state == TrialState.Boss &&
                   !bossDefeated &&
                   !coreSealed &&
                   activeBoss != null &&
                   activeBoss == boss;
        }

        public void OnBossPhaseThreshold(DungeonBoss activeBoss, int phase)
        {
            if (!attemptActive ||
                state != TrialState.Boss ||
                bossDefeated ||
                coreSealed ||
                activeBoss == null ||
                activeBoss != boss)
            {
                return;
            }

            DeactivateReliefLatches();
            if (reliefLatches.Count == 0)
            {
                coreSealed = false;
                activeReliefLatchIndex = -1;
                RefreshCoreVisual();
                game.Notify("No relief latch answered. The safety vent keeps the core exposed.");
                return;
            }

            var latchIndex = Mathf.Clamp(phase - 2, 0, reliefLatches.Count - 1);
            SetWindLiftsActive(true);
            if (!HasUsableWindLift())
            {
                coreSealed = false;
                activeReliefLatchIndex = -1;
                RefreshCoreVisual();
                game.Notify("The Threadwind route failed. The safety vent keeps the core exposed.");
                return;
            }

            if (!TryActivateReliefLatch(latchIndex))
            {
                coreSealed = false;
                activeReliefLatchIndex = -1;
                RefreshCoreVisual();
                game.Notify("No relief latch answered. The safety vent keeps the core exposed.");
                return;
            }

            coreSealed = true;
            RefreshCoreVisual();
            activeBoss.ApplyStagger(1.1f);
            game.Notify(
                $"Core sealed. Ride a Threadwind lift and vent {reliefLatches[activeReliefLatchIndex].name}.");
            game.RequestTrialSave();
        }

        public void OnBossDefeated(DungeonBoss activeBoss)
        {
            if (activeBoss == null ||
                activeBoss != boss ||
                bossDefeated ||
                state != TrialState.Boss)
            {
                return;
            }

            CancelTransition();
            bossDefeated = true;
            state = TrialState.Complete;
            coreSealed = false;
            activeReliefLatchIndex = -1;
            remainingEnemyIds.Clear();
            DeactivateReliefLatches();
            SetWindLiftsActive(true);
            RefreshCoreVisual();

            game.ClearDungeonBoss(activeBoss);
            GrantCompletionRewards();
            game.Notify("The forge falls quiet. The Thermal Reel now tunes every Threadwind lift.");
            game.RequestTrialSave();
        }

        public void ReleaseCore(ReliefLatch latch)
        {
            if (!attemptActive ||
                state != TrialState.Boss ||
                bossDefeated ||
                !coreSealed ||
                latch == null ||
                !latch.IsAvailable)
            {
                return;
            }

            coreSealed = false;
            activeReliefLatchIndex = -1;
            DeactivateReliefLatches();
            RefreshCoreVisual();
            if (boss != null && boss.gameObject.activeInHierarchy)
            {
                boss.ApplyStagger(5.2f);
            }

            game.Notify("The relief channel opens. The Harrower's core is exposed.");
            game.RequestTrialSave();
        }

        public TrialSaveData WriteSaveData()
        {
            var dialStates = new int[dials.Count];
            for (var index = 0; index < dials.Count; index++)
            {
                dialStates[index] = dials[index].State;
            }

            return new TrialSaveData
            {
                id = TrialId,
                state = state,
                dialStates = dialStates,
                activeWaveIndex = activeWaveIndex,
                remainingEnemyIds = new List<string>(remainingEnemyIds),
                bossDefeated = bossDefeated,
                rewardGranted = rewardGranted
            };
        }

        public void RestoreSaveData(TrialSaveData data)
        {
            if (data == null)
            {
                return;
            }

            attemptActive = false;
            CancelTransition();
            ResetActiveCombat();

            var reconciled = false;
            var restoredStateValue = (int)data.state;
            if (restoredStateValue < (int)TrialState.Locked ||
                restoredStateValue > (int)TrialState.Complete)
            {
                state = TrialState.Locked;
                reconciled = true;
            }
            else
            {
                state = data.state;
            }

            activeWaveIndex = Mathf.Clamp(data.activeWaveIndex, 0, SurgeWaves.Length - 1);
            if (activeWaveIndex != data.activeWaveIndex)
            {
                reconciled = true;
            }

            if (data.remainingEnemyIds == null)
            {
                reconciled = true;
            }

            bossDefeated = data.bossDefeated;
            rewardGranted = data.rewardGranted;
            for (var index = 0; index < dials.Count; index++)
            {
                var hasSavedState = data.dialStates != null && index < data.dialStates.Length;
                var savedState = hasSavedState ? data.dialStates[index] : 0;
                var normalizedState = NormalizeDialState(savedState);
                if (!hasSavedState || savedState != normalizedState)
                {
                    reconciled = true;
                }

                dials[index].SetState(normalizedState);
            }

            if (data.dialStates != null && data.dialStates.Length != dials.Count)
            {
                reconciled = true;
            }

            if (state == TrialState.Complete || bossDefeated || rewardGranted)
            {
                reconciled |= state != TrialState.Complete || !bossDefeated;
                state = TrialState.Complete;
                bossDefeated = true;
            }

            if (state == TrialState.Surge ||
                state == TrialState.Boss ||
                state == TrialState.Complete)
            {
                for (var index = 0; index < dials.Count; index++)
                {
                    if (!dials[index].IsAligned)
                    {
                        dials[index].SetAligned();
                        reconciled = true;
                    }
                }
            }

            remainingEnemyIds.Clear();
            switch (state)
            {
                case TrialState.Locked:
                case TrialState.AligningFlows:
                    if (activeWaveIndex != 0 ||
                        (data.remainingEnemyIds != null && data.remainingEnemyIds.Count > 0))
                    {
                        reconciled = true;
                    }

                    activeWaveIndex = 0;
                    break;
                case TrialState.Surge:
                    reconciled |= ReconcileRemainingEnemies(data.remainingEnemyIds);
                    break;
                case TrialState.Boss:
                case TrialState.Complete:
                    if (activeWaveIndex != SurgeWaves.Length - 1 ||
                        (data.remainingEnemyIds != null && data.remainingEnemyIds.Count > 0))
                    {
                        reconciled = true;
                    }

                    activeWaveIndex = SurgeWaves.Length - 1;
                    break;
            }

            coreSealed = false;
            activeReliefLatchIndex = -1;
            DeactivateReliefLatches();
            SetWindLiftsActive(state != TrialState.Locked && AreFlowsAligned());
            if (state == TrialState.Complete)
            {
                if (boss != null)
                {
                    boss.SetRestoredDefeated();
                }

                if (!rewardGranted)
                {
                    GrantCompletionRewards();
                    reconciled = true;
                }
            }
            else if (EnsureBossExists())
            {
                boss.ResetForEncounter();
                boss.gameObject.SetActive(false);
            }

            RefreshCoreVisual();
            attemptActive = state != TrialState.Locked && IsPlayerInsideArena();
            if (attemptActive && state == TrialState.Surge)
            {
                SpawnCurrentWave();
            }
            else if (attemptActive && state == TrialState.Boss)
            {
                StartBoss();
            }

            if (reconciled)
            {
                saveReconciliationPending = true;
            }
        }

        private void BuildArena()
        {
            CreateBlock("Ashwake Floor", ArenaCenter + Vector3.down, new Vector3(58f, 2f, 78f), new Color(0.19f, 0.07f, 0.05f));
            CreateBlock("Ashwake north wall", ArenaCenter + new Vector3(0f, 5f, 38f), new Vector3(58f, 10f, 2f), new Color(0.25f, 0.09f, 0.07f));
            CreateBlock("Ashwake south wall", ArenaCenter + new Vector3(0f, 5f, -38f), new Vector3(58f, 10f, 2f), new Color(0.25f, 0.09f, 0.07f));
            CreateBlock("Ashwake west wall", ArenaCenter + new Vector3(-29f, 5f, 0f), new Vector3(2f, 10f, 78f), new Color(0.25f, 0.09f, 0.07f));
            CreateBlock("Ashwake east wall", ArenaCenter + new Vector3(29f, 5f, 0f), new Vector3(2f, 10f, 78f), new Color(0.25f, 0.09f, 0.07f));

            CreateDialPlatform(new Vector3(-176f, 0f, -238f), 2.4f, "Ember flow", 2);
            CreateDialPlatform(new Vector3(-144f, 0f, -238f), 3.4f, "Draft flow", 0);
            CreateDialPlatform(new Vector3(-160f, 0f, -208f), 2.9f, "Cinder flow", 1);
            CreateReliefLedge(new Vector3(-177f, 0f, -211f), 6.2f, "West relief latch");
            CreateReliefLedge(new Vector3(-143f, 0f, -211f), 6.2f, "East relief latch");
            CreateWindLift(new Vector3(-176f, 2.5f, -218f), "West ashflow");
            CreateWindLift(new Vector3(-144f, 2.5f, -218f), "East ashflow");

            var coreRoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coreRoot.name = "Ashwake ignition core";
            coreRoot.transform.position = ArenaCenter + new Vector3(0f, 1.3f, -2f);
            coreRoot.transform.localScale = new Vector3(2.8f, 1.3f, 2.8f);
            coreRenderer = coreRoot.GetComponent<Renderer>();
            coreRenderer.material = game.CreateMaterial(new Color(0.34f, 0.13f, 0.08f), 0.35f, 0.62f);
            core = coreRoot.AddComponent<TrialCore>();
            core.Initialize(game, this);
            game.RegisterInteractable(core);
            WorldVisuals.AddLabel(coreRoot.transform, "Ashwake ignition core", new Vector3(0f, 2f, 0f), new Color(1f, 0.63f, 0.27f), 0.08f);

            var exitRoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            exitRoot.name = "Ashwake retreat route";
            exitRoot.transform.position = ArenaCenter + new Vector3(0f, 1.1f, 31f);
            exitRoot.transform.localScale = new Vector3(2.3f, 1.1f, 2.3f);
            exitRoot.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.43f, 0.82f, 0.72f), 0.18f, 0.62f);
            var exit = exitRoot.AddComponent<TrialExit>();
            exit.Initialize(game, this);
            game.RegisterInteractable(exit);
            WorldVisuals.AddLabel(exitRoot.transform, "Retreat route", new Vector3(0f, 1.9f, 0f), new Color(0.67f, 0.96f, 0.82f), 0.08f);

            EnsureBossExists();
            WorldVisuals.AddLabel(transform, "The Ashwake Crucible", new Vector3(0f, 8f, -232f), definition.accentColor, 0.16f);
        }

        private void CreateDialPlatform(Vector3 position, float height, string label, int requiredState)
        {
            var platform = CreateBlock(
                $"{label} climbing brace",
                position + Vector3.up * (height * 0.5f),
                new Vector3(5f, height, 5f),
                new Color(0.29f, 0.12f, 0.08f));
            platform.AddComponent<ClimbableSurface>();
            var dial = AshflowDial.Create(
                game,
                this,
                position + Vector3.up * (height + 0.6f),
                label,
                requiredState);
            dials.Add(dial);
            game.RegisterInteractable(dial);
            WorldVisuals.AddLabel(platform.transform, $"Vent glyph: {requiredState + 1}", new Vector3(0f, height + 1.5f, 0f), new Color(0.96f, 0.61f, 0.29f), 0.07f);
        }

        private void CreateReliefLedge(Vector3 position, float height, string label)
        {
            var platform = CreateBlock(
                $"{label} ledge",
                position + Vector3.up * (height * 0.5f),
                new Vector3(5.5f, height, 5.5f),
                new Color(0.31f, 0.1f, 0.06f));
            platform.AddComponent<ClimbableSurface>();
            var latch = ReliefLatch.Create(game, this, position + Vector3.up * (height + 0.6f), label);
            reliefLatches.Add(latch);
            game.RegisterInteractable(latch);
        }

        private void CreateWindLift(Vector3 position, string label)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = label;
            root.transform.position = position;
            root.transform.localScale = new Vector3(2.6f, 2.5f, 2.6f);
            root.GetComponent<Renderer>().material = game.CreateTransparentMaterial(new Color(1f, 0.38f, 0.12f, 0.34f), 0.64f);
            root.AddComponent<WindLift>().Initialize(11.7f);
            root.AddComponent<RouteGlow>().Initialize(0.08f, position.x + position.z);
            WorldVisuals.AddLabel(root.transform, "Ashflow lift", new Vector3(0f, 3.2f, 0f), new Color(1f, 0.68f, 0.38f), 0.07f);
            root.SetActive(false);
            windLifts.Add(root);
        }

        private GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Color color)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().material = game.CreateMaterial(color, 0.14f, 0.4f);
            return block;
        }

        private bool AreFlowsAligned()
        {
            return dials.Count > 0 && CountAlignedFlows() == dials.Count;
        }

        private int CountAlignedFlows()
        {
            var aligned = 0;
            foreach (var dial in dials)
            {
                if (dial != null && dial.IsAligned)
                {
                    aligned++;
                }
            }

            return aligned;
        }

        private static int NormalizeDialState(int dialState)
        {
            dialState %= 3;
            return dialState < 0 ? dialState + 3 : dialState;
        }

        private void SetWindLiftsActive(bool active)
        {
            foreach (var windLift in windLifts)
            {
                if (windLift != null)
                {
                    windLift.SetActive(active);
                }
            }
        }

        private void RefreshCoreVisual()
        {
            if (coreRenderer == null)
            {
                return;
            }

            var color = new Color(0.34f, 0.13f, 0.08f);
            if (state == TrialState.Complete)
            {
                color = new Color(0.42f, 0.94f, 0.72f);
            }
            else if (state == TrialState.Boss)
            {
                color = coreSealed
                    ? new Color(0.65f, 0.18f, 0.08f)
                    : new Color(1f, 0.58f, 0.16f);
            }
            else if (state == TrialState.Surge)
            {
                color = new Color(0.98f, 0.37f, 0.1f);
            }
            else if (state == TrialState.AligningFlows && AreFlowsAligned())
            {
                color = new Color(0.96f, 0.5f, 0.16f);
            }

            coreRenderer.material.color = color;
        }

        private void SpawnCurrentWave()
        {
            if (!attemptActive ||
                state != TrialState.Surge ||
                activeWaveIndex < 0 ||
                activeWaveIndex >= SurgeWaves.Length)
            {
                return;
            }

            PruneActiveEnemies();
            if (activeEnemies.Count > 0)
            {
                return;
            }

            if (remainingEnemyIds.Count == 0)
            {
                remainingEnemyIds.AddRange(SurgeWaves[activeWaveIndex]);
            }

            var spawnCount = remainingEnemyIds.Count;
            for (var index = 0; index < spawnCount; index++)
            {
                var angle = index * Mathf.PI * 2f / spawnCount + activeWaveIndex * 0.51f;
                var position = ArenaCenter + new Vector3(Mathf.Cos(angle) * 18f, 0f, Mathf.Sin(angle) * 17f);
                var enemy = Enemy.Create(game, null, remainingEnemyIds[index], position, OnSurgeEnemyDefeated);
                if (enemy != null)
                {
                    activeEnemies.Add(enemy);
                }
            }

            game.Notify(
                $"Surge {activeWaveIndex + 1}/{SurgeWaves.Length}. " +
                $"{DescribeRemainingEchoes(spawnCount)}.");
        }

        private void OnSurgeEnemyDefeated(Enemy enemy)
        {
            if (state != TrialState.Surge ||
                enemy == null ||
                !activeEnemies.Remove(enemy))
            {
                return;
            }

            remainingEnemyIds.Remove(enemy.EnemyId);
            if (activeEnemies.Count > 0)
            {
                game.RequestTrialSave();
                return;
            }

            if (remainingEnemyIds.Count > 0)
            {
                ScheduleTransition(false, WaveTransitionDelay);
                game.Notify("The pressure ring flickers. Unresolved echoes are reforming.");
                game.RequestTrialSave();
                return;
            }

            if (activeWaveIndex + 1 < SurgeWaves.Length)
            {
                var clearedWave = activeWaveIndex + 1;
                activeWaveIndex++;
                remainingEnemyIds.AddRange(SurgeWaves[activeWaveIndex]);
                ScheduleTransition(false, WaveTransitionDelay);
                game.Notify(
                    $"Surge {clearedWave}/{SurgeWaves.Length} cleared. " +
                    $"Surge {activeWaveIndex + 1} is forming.");
                game.RequestTrialSave();
                return;
            }

            state = TrialState.Boss;
            activeWaveIndex = SurgeWaves.Length - 1;
            ScheduleTransition(true, BossTransitionDelay);
            RefreshCoreVisual();
            game.Notify("All pressure rings are clear. Scoria Harrower is entering the forge.");
            game.RequestTrialSave();
        }

        private void ScheduleTransition(bool startBoss, float delay)
        {
            transitionPending = true;
            transitionToBoss = startBoss;
            transitionAt = Time.time + Mathf.Max(0f, delay);
        }

        private void CancelTransition()
        {
            transitionPending = false;
            transitionToBoss = false;
            transitionAt = 0f;
        }

        private void StartBoss()
        {
            if (!attemptActive || bossDefeated || state == TrialState.Complete)
            {
                return;
            }

            CancelTransition();
            ResetEnemies();
            state = TrialState.Boss;
            activeWaveIndex = SurgeWaves.Length - 1;
            remainingEnemyIds.Clear();
            coreSealed = false;
            activeReliefLatchIndex = -1;
            DeactivateReliefLatches();
            SetWindLiftsActive(true);
            if (!EnsureBossExists())
            {
                RefreshCoreVisual();
                game.Notify("The Harrower failed to form. The Crucible will retry.");
                saveReconciliationPending = true;
                return;
            }

            boss.ResetForEncounter();
            boss.gameObject.SetActive(true);
            game.RegisterDungeonBoss(boss);
            RefreshCoreVisual();
            game.Notify("The forge opens. Scoria Harrower enters at full pressure.");
        }

        private bool EnsureBossExists()
        {
            if (boss != null)
            {
                return true;
            }

            if (game == null || definition == null)
            {
                return false;
            }

            boss = DungeonBoss.Create(
                game,
                this,
                BossCatalog.Get(definition.bossId),
                ArenaCenter + new Vector3(0f, 1.6f, 14f));
            if (boss == null)
            {
                return false;
            }

            boss.gameObject.SetActive(false);
            return true;
        }

        private void RecoverActiveEncounter()
        {
            PruneActiveEnemies();
            if (state == TrialState.Surge && !transitionPending && activeEnemies.Count == 0)
            {
                if (remainingEnemyIds.Count == 0)
                {
                    remainingEnemyIds.AddRange(SurgeWaves[activeWaveIndex]);
                }

                game.Notify("The current pressure ring lost its echoes. This surge is reforming.");
                SpawnCurrentWave();
                game.RequestTrialSave();
                return;
            }

            if (state != TrialState.Boss || bossDefeated || transitionPending)
            {
                return;
            }

            if (!EnsureBossExists())
            {
                return;
            }

            if (boss.IsDefeated || !boss.gameObject.activeInHierarchy)
            {
                StartBoss();
                game.RequestTrialSave();
                return;
            }

            if (!boss.enabled)
            {
                boss.enabled = true;
            }

            if (game.ActiveDungeonBoss != boss)
            {
                game.RegisterDungeonBoss(boss);
            }

            if (!coreSealed)
            {
                return;
            }

            if (!HasUsableWindLift())
            {
                coreSealed = false;
                activeReliefLatchIndex = -1;
                DeactivateReliefLatches();
                RefreshCoreVisual();
                game.Notify("The Threadwind route failed. The safety vent exposes the Harrower's core.");
                return;
            }

            if (HasAvailableReliefLatch())
            {
                return;
            }

            if (TryActivateReliefLatch(activeReliefLatchIndex))
            {
                game.Notify("The relief latch relights. Vent it to expose the Harrower's core.");
                return;
            }

            coreSealed = false;
            activeReliefLatchIndex = -1;
            RefreshCoreVisual();
            game.Notify("The relief route failed. The safety vent exposes the Harrower's core.");
        }

        private bool TryActivateReliefLatch(int preferredIndex)
        {
            if (IsReliefRouteUsable(preferredIndex))
            {
                activeReliefLatchIndex = preferredIndex;
                reliefLatches[preferredIndex].SetAvailable(true);
                return true;
            }

            for (var index = 0; index < reliefLatches.Count; index++)
            {
                if (!IsReliefRouteUsable(index))
                {
                    continue;
                }

                activeReliefLatchIndex = index;
                reliefLatches[index].SetAvailable(true);
                return true;
            }

            return false;
        }

        private bool HasAvailableReliefLatch()
        {
            for (var index = 0; index < reliefLatches.Count; index++)
            {
                if (IsReliefRouteUsable(index) && reliefLatches[index].IsAvailable)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsReliefRouteUsable(int index)
        {
            return index >= 0 &&
                   index < reliefLatches.Count &&
                   index < windLifts.Count &&
                   reliefLatches[index] != null &&
                   reliefLatches[index].gameObject.activeInHierarchy &&
                   windLifts[index] != null &&
                   windLifts[index].activeInHierarchy;
        }

        private bool HasUsableWindLift()
        {
            foreach (var windLift in windLifts)
            {
                if (windLift != null && windLift.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private void DeactivateReliefLatches()
        {
            foreach (var latch in reliefLatches)
            {
                if (latch != null)
                {
                    latch.SetAvailable(false);
                }
            }
        }

        private void ResetActiveCombat()
        {
            ResetEnemies();
            if (boss != null)
            {
                game.ClearDungeonBoss(boss);
                if (!bossDefeated)
                {
                    boss.ResetForEncounter();
                    boss.gameObject.SetActive(false);
                }
            }

            coreSealed = false;
            activeReliefLatchIndex = -1;
            DeactivateReliefLatches();
            RefreshCoreVisual();
        }

        private void ResetEnemies()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    enemy.gameObject.SetActive(false);
                    Object.Destroy(enemy.gameObject);
                }
            }

            activeEnemies.Clear();
        }

        private void PruneActiveEnemies()
        {
            for (var index = activeEnemies.Count - 1; index >= 0; index--)
            {
                if (activeEnemies[index] == null)
                {
                    activeEnemies.RemoveAt(index);
                }
            }
        }

        private bool ReconcileRemainingEnemies(List<string> restoredEnemyIds)
        {
            var reconciled = restoredEnemyIds == null;
            var availableEnemyIds = new List<string>(SurgeWaves[activeWaveIndex]);
            if (restoredEnemyIds != null)
            {
                foreach (var enemyId in restoredEnemyIds)
                {
                    var availableIndex = availableEnemyIds.IndexOf(enemyId);
                    if (availableIndex < 0)
                    {
                        reconciled = true;
                        continue;
                    }

                    remainingEnemyIds.Add(enemyId);
                    availableEnemyIds.RemoveAt(availableIndex);
                }
            }

            if (remainingEnemyIds.Count == 0)
            {
                remainingEnemyIds.AddRange(SurgeWaves[activeWaveIndex]);
                reconciled = true;
            }

            return reconciled;
        }

        private bool IsPlayerInsideArena()
        {
            if (game == null || game.Player == null)
            {
                return false;
            }

            var offset = game.PlayerTransform.position - ArenaCenter;
            return Mathf.Abs(offset.x) <= ArenaHalfWidth &&
                   Mathf.Abs(offset.z) <= ArenaHalfDepth &&
                   offset.y >= -10f &&
                   offset.y <= 48f;
        }

        private void HandleInterruptedAttempt()
        {
            var interruptedCombat = state == TrialState.Surge || state == TrialState.Boss;
            var fellBelowArena = game.Player != null &&
                                 game.PlayerTransform.position.y < ArenaCenter.y - 10f;
            attemptActive = false;
            CancelTransition();
            ResetActiveCombat();
            if (fellBelowArena && game.Player != null)
            {
                game.Player.Teleport(definition.returnPosition);
                game.Notify("The Crucible safety line returns you to the Ashwake bell.");
            }
            else if (interruptedCombat)
            {
                game.Notify("The Crucible attempt cooled. Return to the Ashwake bell to retry.");
            }

            game.RequestTrialSave();
        }

        private void GrantCompletionRewards()
        {
            if (rewardGranted || game == null || definition == null)
            {
                return;
            }

            rewardGranted = true;
            game.UnlockEquipmentReward(definition.rewardEquipmentId);
            game.AddFavors(58, "the Ashwake Crucible");
            game.AddResource(ResourceKind.Ore, 4);
            game.AddResource(ResourceKind.Chimesalt, 3);
        }

        private static string DescribeEchoes(int count)
        {
            return count == 1 ? "1 forge echo" : $"{Mathf.Max(0, count)} forge echoes";
        }

        private static string DescribeRemainingEchoes(int count)
        {
            return count == 1
                ? "1 forge echo remains"
                : $"{Mathf.Max(0, count)} forge echoes remain";
        }

        private string GetEntryMessage()
        {
            return state switch
            {
                TrialState.AligningFlows => AreFlowsAligned()
                    ? "All ashflows match their vents. Ignite the core."
                    : $"{CountAlignedFlows()}/{dials.Count} ashflows aligned. Match each dial to its vent glyph.",
                TrialState.Surge =>
                    $"Surge {activeWaveIndex + 1}/{SurgeWaves.Length} resumes with " +
                    $"{DescribeEchoes(remainingEnemyIds.Count)}.",
                TrialState.Boss => "Scoria Harrower returns at full pressure. Use Threadwind lifts when it seals its core.",
                TrialState.Complete => "The quiet crucible keeps its Threadwind routes open.",
                _ => "The Ashwake Crucible waits for a route through its furnace rings."
            };
        }

        private string GetRetreatMessage()
        {
            return state switch
            {
                TrialState.AligningFlows => "You leave the Crucible. Its dial settings remain recorded.",
                TrialState.Surge =>
                    $"You leave the Crucible. Surge {activeWaveIndex + 1} will resume with " +
                    $"{DescribeEchoes(remainingEnemyIds.Count)}.",
                TrialState.Boss => "You leave the Crucible. Scoria Harrower will restart at full pressure.",
                TrialState.Complete => "You leave the quiet Crucible. Its Threadwind routes remain open.",
                _ => "You leave the Ashwake Crucible."
            };
        }
    }

    public sealed class TrialCore : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private AshwakeTrial trial;

        public string InteractionPrompt => trial != null ? trial.GetCorePrompt() : string.Empty;

        public void Initialize(TetherfallGame owner, AshwakeTrial director)
        {
            game = owner;
            trial = director;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return trial != null;
        }

        public void Interact(TetherfallGame owner)
        {
            if (trial != null)
            {
                trial.InteractWithCore();
            }
        }
    }

    public sealed class TrialExit : MonoBehaviour, IWorldInteractable
    {
        private AshwakeTrial trial;

        public string InteractionPrompt => "Leave the Ashwake Crucible";

        public void Initialize(TetherfallGame owner, AshwakeTrial director)
        {
            trial = director;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return trial != null;
        }

        public void Interact(TetherfallGame owner)
        {
            if (trial != null)
            {
                trial.Exit();
            }
        }
    }

    public sealed class AshflowDial : MonoBehaviour, IWorldInteractable
    {
        private readonly Color[] stateColors =
        {
            new Color(0.27f, 0.34f, 0.43f),
            new Color(0.96f, 0.58f, 0.2f),
            new Color(0.74f, 0.25f, 0.12f)
        };

        private AshwakeTrial trial;
        private Renderer body;
        private int requiredState;

        public int State { get; private set; }
        public bool IsAligned => State == requiredState;
        public string InteractionPrompt =>
            $"Turn {name}: glyph {State + 1}" + (IsAligned ? " (matches vent)" : string.Empty);

        public static AshflowDial Create(
            TetherfallGame game,
            AshwakeTrial trial,
            Vector3 position,
            string label,
            int requiredState)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = label;
            root.transform.position = position;
            root.transform.localScale = new Vector3(1.1f, 0.7f, 1.1f);
            var dial = root.AddComponent<AshflowDial>();
            dial.trial = trial;
            dial.requiredState = Mathf.Clamp(requiredState, 0, 2);
            dial.body = root.GetComponent<Renderer>();
            dial.body.material = game.CreateMaterial(dial.stateColors[0], 0.24f, 0.55f);
            WorldVisuals.AddLabel(root.transform, label, new Vector3(0f, 1.1f, 0f), new Color(0.95f, 0.72f, 0.46f), 0.07f);
            return dial;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return trial != null && trial.CanAdjustDials;
        }

        public void Interact(TetherfallGame owner)
        {
            if (trial == null)
            {
                return;
            }

            SetState((State + 1) % 3);
            trial.OnDialAdjusted(this);
        }

        public void SetState(int state)
        {
            state %= 3;
            State = state < 0 ? state + 3 : state;
            if (body != null)
            {
                body.material.color = stateColors[State];
            }
        }

        public void SetAligned()
        {
            SetState(requiredState);
        }
    }

    public sealed class ReliefLatch : MonoBehaviour, IWorldInteractable
    {
        private AshwakeTrial trial;
        private Renderer body;

        public bool IsAvailable { get; private set; }
        public string InteractionPrompt => IsAvailable ? $"Vent {name}" : "Relief latch dormant";

        public static ReliefLatch Create(TetherfallGame game, AshwakeTrial trial, Vector3 position, string label)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = label;
            root.transform.position = position;
            root.transform.localScale = new Vector3(1.2f, 0.45f, 1.2f);
            var latch = root.AddComponent<ReliefLatch>();
            latch.trial = trial;
            latch.body = root.GetComponent<Renderer>();
            latch.body.material = game.CreateMaterial(new Color(0.19f, 0.2f, 0.24f), 0.22f, 0.44f);
            WorldVisuals.AddLabel(root.transform, label, new Vector3(0f, 0.8f, 0f), new Color(0.94f, 0.66f, 0.36f), 0.065f);
            return latch;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return trial != null && IsAvailable;
        }

        public void Interact(TetherfallGame owner)
        {
            if (trial != null)
            {
                trial.ReleaseCore(this);
            }
        }

        public void SetAvailable(bool available)
        {
            IsAvailable = available;
            if (body != null)
            {
                body.material.color = available
                    ? new Color(0.98f, 0.49f, 0.12f)
                    : new Color(0.19f, 0.2f, 0.24f);
            }
        }
    }
}
