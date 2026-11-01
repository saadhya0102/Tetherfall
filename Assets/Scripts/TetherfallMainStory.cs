using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public enum MainStoryRouteChoice
    {
        None,
        Hearthline,
        OpenCurrent
    }

    public static class TetherfallMainStoryBuilder
    {
        public static void Build(TetherfallGame game)
        {
            if (game == null || game.GetComponentInChildren<TetherfallMainStoryDirector>() != null)
            {
                return;
            }

            game.InitializeMainStoryQuestRecords();
            var root = new GameObject("Hollow Choir Main Story Director");
            root.transform.SetParent(game.transform, false);
            var director = root.AddComponent<TetherfallMainStoryDirector>();
            director.Initialize(game);
        }
    }

    public sealed partial class TetherfallGame
    {
        private TetherfallMainStoryDirector mainStoryDirector;

        internal int MainStoryProgress
        {
            get
            {
                var quest = GetMainStoryQuest();
                return quest != null ? Mathf.Clamp(quest.current, 0, quest.required) : 0;
            }
        }

        internal bool MainStoryComplete
        {
            get
            {
                var quest = GetMainStoryQuest();
                return quest != null && quest.completed;
            }
        }

        internal void InitializeMainStoryQuestRecords()
        {
            if (GetMainStoryQuest() == null)
            {
                questLog.Add(MainStoryCatalog.CreateQuestRecord());
            }

            EnsureMainStoryRouteMarker(MainStoryCatalog.HearthlineRouteId);
            EnsureMainStoryRouteMarker(MainStoryCatalog.OpenCurrentRouteId);
        }

        internal void RegisterMainStoryDirector(TetherfallMainStoryDirector director)
        {
            mainStoryDirector = director;
        }

        internal bool TryAdvanceMainStory(int expectedProgress, int nextProgress)
        {
            var quest = GetMainStoryQuest();
            if (quest == null ||
                quest.completed ||
                quest.current != expectedProgress ||
                nextProgress <= expectedProgress ||
                nextProgress > quest.required)
            {
                return false;
            }

            AdvanceQuest(MainStoryCatalog.QuestId, nextProgress - expectedProgress);
            RefreshMainStoryQuestObjective();
            RequestSave();
            return quest.current == nextProgress;
        }

        internal bool RecordMainStoryRouteChoice(MainStoryRouteChoice choice)
        {
            if (choice == MainStoryRouteChoice.None ||
                MainStoryProgress != 4 ||
                GetMainStoryRouteChoice() != MainStoryRouteChoice.None)
            {
                return false;
            }

            RemoveMainStoryRouteMarkers();
            var routeId = choice == MainStoryRouteChoice.Hearthline
                ? MainStoryCatalog.HearthlineRouteId
                : MainStoryCatalog.OpenCurrentRouteId;
            var marker = MainStoryCatalog.CreateRouteMarker(routeId);
            marker.current = marker.required;
            marker.completed = true;
            questLog.Add(marker);

            if (!TryAdvanceMainStory(4, 5))
            {
                questLog.Remove(marker);
                EnsureMainStoryRouteMarker(MainStoryCatalog.HearthlineRouteId);
                EnsureMainStoryRouteMarker(MainStoryCatalog.OpenCurrentRouteId);
                return false;
            }

            RequestSave();
            return true;
        }

        internal MainStoryRouteChoice GetMainStoryRouteChoice()
        {
            var hearthline = IsMainStoryRouteMarkerComplete(MainStoryCatalog.HearthlineRouteId);
            var openCurrent = IsMainStoryRouteMarkerComplete(MainStoryCatalog.OpenCurrentRouteId);
            if (hearthline == openCurrent)
            {
                return MainStoryRouteChoice.None;
            }

            return hearthline ? MainStoryRouteChoice.Hearthline : MainStoryRouteChoice.OpenCurrent;
        }

        internal MainStoryRouteChoice GetSuggestedMainStoryRouteChoice()
        {
            if (cinderwoldResolutionId == CinderwoldArcCatalog.AshboundResolutionId)
            {
                return MainStoryRouteChoice.OpenCurrent;
            }

            if (cinderwoldResolutionId == CinderwoldArcCatalog.RouteboundResolutionId)
            {
                return MainStoryRouteChoice.Hearthline;
            }

            var cinderwing = FindPactCreature("Cinderwing");
            return cinderwing != null && cinderwing.IsPacted
                ? MainStoryRouteChoice.OpenCurrent
                : MainStoryRouteChoice.Hearthline;
        }

        internal bool IsMainStoryEligibleForGuidance()
        {
            return MainStoryProgress > 0 ||
                   hearthgateRestored ||
                   settlementTier > 0 ||
                   gatheredNodes >= 8 ||
                   clearedCamps > 0 ||
                   dungeonBossDefeated ||
                   Level > 1;
        }

        internal string GetMainStoryAlderwakeContext()
        {
            if (hearthgateRestored && settlementTier >= 3)
            {
                return "Alderwake's restored Hearthgate carried a false chime in from the Rainline Watch.";
            }

            if (hearthgateRestored || settlementTier > 0)
            {
                return "Alderwake's rebuilding crews opened one honest road, and the Hollow Choir found its note.";
            }

            return "The station is still frayed, but its west watchfire is already sheltering travelers. That is enough road to defend.";
        }

        internal string GetMainStoryMorrowfenContext()
        {
            if (FenlightTier >= 3)
            {
                return "Fenlight's restored lantern chain caught the stolen resonance and marked the observatory cave.";
            }

            if (dungeonBossDefeated)
            {
                return "The stabilized Skyvault route gives the stolen resonance nowhere to hide.";
            }

            return "Veyrwater's ferriers found a dry approach. The observatory can be entered without waiting on another restoration.";
        }

        internal string GetMainStoryCinderwoldContext(MainStoryRouteChoice choice)
        {
            var routeText = choice == MainStoryRouteChoice.Hearthline
                ? "The Hearthline will carry the breach load toward repaired shelters."
                : "The Open Current will keep a willing Echosworn passage beside the road.";
            if (IsThreadwayUnlocked("threadway.emberrelay"))
            {
                routeText += " The restored Ember Relay can share the strain.";
            }
            else
            {
                routeText += " Field anchors will hold it until the Ember Relay is restored.";
            }

            return routeText;
        }

        internal void OverrideObjectiveWithMainStory(Transform target)
        {
            if (target == null)
            {
                return;
            }

            objectiveTarget = target;
            if (objectiveBeacon == null)
            {
                objectiveBeacon = ObjectiveBeacon.Create(this);
            }

            objectiveBeacon.SetTarget(target);
        }

        internal void ReconcileMainStoryFromQuestMilestones()
        {
            InitializeMainStoryQuestRecords();
            var quest = GetMainStoryQuest();
            if (quest == null)
            {
                return;
            }

            var reconciled = false;
            var normalizedProgress = Mathf.Clamp(quest.current, 0, quest.required);
            if (normalizedProgress != quest.current)
            {
                quest.current = normalizedProgress;
                reconciled = true;
            }

            if (quest.completed && quest.current < quest.required)
            {
                quest.current = quest.required;
                reconciled = true;
            }

            if (!quest.completed && quest.current >= quest.required)
            {
                quest.completed = true;
                reconciled = true;
            }

            var hearthMarker = questLog.Find(record => record.id == MainStoryCatalog.HearthlineRouteId);
            var currentMarker = questLog.Find(record => record.id == MainStoryCatalog.OpenCurrentRouteId);
            var hearthComplete = hearthMarker != null && hearthMarker.completed;
            var currentComplete = currentMarker != null && currentMarker.completed;

            if (hearthComplete && currentComplete)
            {
                var keepOpenCurrent = GetSuggestedMainStoryRouteChoice() == MainStoryRouteChoice.OpenCurrent;
                questLog.Remove(keepOpenCurrent ? hearthMarker : currentMarker);
                hearthComplete = !keepOpenCurrent;
                currentComplete = keepOpenCurrent;
                reconciled = true;
            }

            if ((quest.current >= 5 || quest.completed) && !hearthComplete && !currentComplete)
            {
                RemoveMainStoryRouteMarkers();
                var inferred = GetSuggestedMainStoryRouteChoice();
                var inferredId = inferred == MainStoryRouteChoice.OpenCurrent
                    ? MainStoryCatalog.OpenCurrentRouteId
                    : MainStoryCatalog.HearthlineRouteId;
                var inferredMarker = MainStoryCatalog.CreateRouteMarker(inferredId);
                inferredMarker.current = inferredMarker.required;
                inferredMarker.completed = true;
                questLog.Add(inferredMarker);
                hearthComplete = inferred == MainStoryRouteChoice.Hearthline;
                currentComplete = inferred == MainStoryRouteChoice.OpenCurrent;
                reconciled = true;
            }

            if ((hearthComplete || currentComplete) && quest.current < 5)
            {
                quest.current = 5;
                reconciled = true;
            }

            if (!hearthComplete && !currentComplete)
            {
                RemoveMainStoryRouteMarkers();
            }
            else
            {
                var unchosenId = hearthComplete
                    ? MainStoryCatalog.OpenCurrentRouteId
                    : MainStoryCatalog.HearthlineRouteId;
                var unchosen = questLog.Find(record => record.id == unchosenId);
                if (unchosen != null)
                {
                    questLog.Remove(unchosen);
                    reconciled = true;
                }
            }

            RefreshMainStoryQuestObjective();
            if (reconciled)
            {
                RequestSave();
            }
        }

        internal void RefreshMainStoryQuestObjective()
        {
            var quest = GetMainStoryQuest();
            if (quest == null)
            {
                return;
            }

            if (quest.completed)
            {
                var choice = GetMainStoryRouteChoice();
                quest.objective = choice == MainStoryRouteChoice.OpenCurrent
                    ? "The Hollow Cantor is quiet. The Open Current remains a willing passage beside the road."
                    : "The Hollow Cantor is quiet. The Hearthline carries travelers safely toward restored shelters.";
                return;
            }

            var stage = MainStoryCatalog.GetStage(quest.current);
            if (stage == null)
            {
                return;
            }

            quest.objective = $"{stage.title}: {stage.objective}";
        }

        private QuestRecord GetMainStoryQuest()
        {
            return questLog.Find(record => record.id == MainStoryCatalog.QuestId);
        }

        private void EnsureMainStoryRouteMarker(string routeId)
        {
            if (questLog.Exists(record => record.id == routeId))
            {
                return;
            }

            questLog.Add(MainStoryCatalog.CreateRouteMarker(routeId));
        }

        private bool IsMainStoryRouteMarkerComplete(string routeId)
        {
            var marker = questLog.Find(record => record.id == routeId);
            return marker != null && marker.completed && marker.current >= marker.required;
        }

        private void RemoveMainStoryRouteMarkers()
        {
            questLog.RemoveAll(record => MainStoryCatalog.IsRouteMarker(record.id));
        }
    }

    public sealed class TetherfallMainStoryDirector : MonoBehaviour
    {
        private TetherfallGame game;
        private MainStoryRoutekeeper routekeeper;
        private MainStoryRouteDefense defense;
        private MainStoryObservatory observatory;
        private MainStoryBreach breach;
        private float nextRefreshAt;

        public int Progress => game != null ? game.MainStoryProgress : 0;
        public bool IsComplete => game != null && game.MainStoryComplete;
        public MainStoryRouteChoice RouteChoice =>
            game != null ? game.GetMainStoryRouteChoice() : MainStoryRouteChoice.None;

        public void Initialize(TetherfallGame owner)
        {
            game = owner;
            game.RegisterMainStoryDirector(this);
            routekeeper = MainStoryRoutekeeper.Create(game, this, MainStoryCatalog.RoutekeeperPosition);
            defense = MainStoryRouteDefense.Create(game, this, MainStoryCatalog.AlderwakeDefenseCenter);
            observatory = MainStoryObservatory.Create(game, this);
            breach = MainStoryBreach.Create(game, this, MainStoryCatalog.CinderwoldBreachCenter);
        }

        private void Start()
        {
            game?.ReconcileMainStoryFromQuestMilestones();
            RefreshWorldState();
        }

        private void Update()
        {
            if (game == null || Time.time < nextRefreshAt)
            {
                return;
            }

            nextRefreshAt = Time.time + 0.25f;
            RefreshWorldState();
        }

        private void LateUpdate()
        {
            if (game == null || IsComplete)
            {
                return;
            }

            game.RefreshMainStoryQuestObjective();
            if (Progress == 0 && !game.IsMainStoryEligibleForGuidance())
            {
                return;
            }

            var target = GetObjectiveTarget();
            if (target != null)
            {
                game.OverrideObjectiveWithMainStory(target);
            }
        }

        public void InteractWithRoutekeeper()
        {
            if (game == null)
            {
                return;
            }

            if (Progress == 0)
            {
                var context = game.GetMainStoryAlderwakeContext();
                if (game.TryAdvanceMainStory(0, 1))
                {
                    game.Notify($"{context} Maelin: \"Sound the watch. I will hold the inner brace.\"");
                    RefreshWorldState();
                }

                return;
            }

            if (Progress == 1)
            {
                game.Notify("Maelin: \"Three approaches, one signal. Stay near the light when their chorus pulls.\"");
                return;
            }

            if (Progress == 2 || Progress == 3)
            {
                game.Notify("Maelin: \"Follow the stolen note into Morrowfen. Bring back its meaning, not its command.\"");
                return;
            }

            if (Progress == 4 || Progress == 5)
            {
                game.Notify("Maelin: \"At the breach, protect the route people choose to walk. I will answer from this end.\"");
                return;
            }

            game.Notify("Maelin: \"The road is singing in travelers' voices again. That is enough.\"");
        }

        public void OnAlderwakeDefenseCompleted()
        {
            if (game != null && game.TryAdvanceMainStory(1, 2))
            {
                game.Notify(game.GetMainStoryMorrowfenContext());
                RefreshWorldState();
            }
        }

        public void OnObservatoryAmbushCompleted()
        {
            if (game != null && game.TryAdvanceMainStory(2, 3))
            {
                game.Notify("The stolen chord resolves into a map of a Cinderwold breach. Read the lens before you leave.");
                RefreshWorldState();
            }
        }

        public void OnObservatoryLensRead()
        {
            if (game != null && game.TryAdvanceMainStory(3, 4))
            {
                game.Notify(
                    "The Choir cannot create a bond. It can only drown choices beneath one voice. " +
                    "Two living routes still reach the Cinderwold breach.");
                RefreshWorldState();
            }
        }

        public bool ChooseRoute(MainStoryRouteChoice choice)
        {
            if (game == null || !game.RecordMainStoryRouteChoice(choice))
            {
                return false;
            }

            game.Notify(game.GetMainStoryCinderwoldContext(choice));
            RefreshWorldState();
            return true;
        }

        public void OnHollowCantorDefeated()
        {
            if (game == null || !game.TryAdvanceMainStory(5, MainStoryCatalog.CompletedProgress))
            {
                return;
            }

            var choice = RouteChoice;
            game.Notify(
                choice == MainStoryRouteChoice.OpenCurrent
                    ? "The breach quiets around the Open Current. Road bells and willing Echosworn answer in different voices."
                    : "The breach settles into the Hearthline. Warm route lights carry the answer back toward Alderwake.");
            RefreshWorldState();
        }

        public Transform GetObjectiveTarget()
        {
            switch (Progress)
            {
                case 0:
                    return routekeeper != null ? routekeeper.transform : null;
                case 1:
                    return defense != null ? defense.ObjectiveTarget : null;
                case 2:
                    return observatory != null ? observatory.GetObjectiveTarget(false) : null;
                case 3:
                    return observatory != null ? observatory.GetObjectiveTarget(true) : null;
                case 4:
                    return breach != null
                        ? breach.GetChoiceTarget(game.GetSuggestedMainStoryRouteChoice())
                        : null;
                case 5:
                    return breach != null ? breach.GetCombatObjectiveTarget() : null;
                default:
                    return null;
            }
        }

        private void RefreshWorldState()
        {
            routekeeper?.RefreshState();
            defense?.RefreshState();
            observatory?.RefreshState();
            breach?.RefreshState(RouteChoice, Progress, IsComplete);
        }
    }

    public sealed class MainStoryRoutekeeper : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private TetherfallMainStoryDirector director;

        public string InteractionPrompt
        {
            get
            {
                if (director == null)
                {
                    return "Listen at the Rainline Watch";
                }

                return director.Progress == 0
                    ? "Hear Maelin's route report"
                    : director.Progress == 1
                        ? "Review the Rainline defense"
                        : director.IsComplete
                            ? "Speak with Maelin about the quiet road"
                            : "Ask Maelin about the Hollow Choir";
            }
        }

        public static MainStoryRoutekeeper Create(
            TetherfallGame game,
            TetherfallMainStoryDirector director,
            Vector3 position)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "Maelin Rook";
            root.transform.position = position;
            root.transform.localScale = new Vector3(0.86f, 1.04f, 0.86f);
            var routekeeper = root.AddComponent<MainStoryRoutekeeper>();
            routekeeper.game = game;
            routekeeper.director = director;
            root.GetComponent<Renderer>().material = game.CreateMaterial(
                new Color(0.24f, 0.42f, 0.37f),
                0.08f,
                0.36f);

            var mantle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mantle.name = "Maelin rain mantle";
            mantle.transform.SetParent(root.transform, false);
            mantle.transform.localPosition = new Vector3(0f, 0.3f, -0.08f);
            mantle.transform.localScale = new Vector3(0.78f, 0.54f, 0.32f);
            mantle.GetComponent<Renderer>().material = game.CreateMaterial(
                new Color(0.16f, 0.27f, 0.25f),
                0.04f,
                0.28f);
            Object.Destroy(mantle.GetComponent<Collider>());

            var routeLamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            routeLamp.name = "Maelin route lamp";
            routeLamp.transform.SetParent(root.transform, false);
            routeLamp.transform.localPosition = new Vector3(0.62f, 0.38f, 0.12f);
            routeLamp.transform.localScale = Vector3.one * 0.2f;
            routeLamp.GetComponent<Renderer>().material = game.CreateGlowMaterial(
                new Color(0.38f, 0.94f, 0.72f),
                1.35f,
                0.65f);
            Object.Destroy(routeLamp.GetComponent<Collider>());

            WorldVisuals.AddLabel(
                root.transform,
                "Maelin Rook · routekeeper",
                new Vector3(0f, 1.72f, 0f),
                new Color(0.77f, 0.94f, 0.82f),
                0.085f);
            game.RegisterInteractable(routekeeper);
            return routekeeper;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            director?.InteractWithRoutekeeper();
        }

        public void RefreshState()
        {
        }
    }

    public sealed class MainStoryRouteDefense : MonoBehaviour, IWorldInteractable
    {
        private const float MaximumEncounterDistance = 44f;
        private const float HoldRadius = 10.5f;

        private readonly List<Enemy> activeEnemies = new List<Enemy>();
        private readonly List<Transform> approachLanes = new List<Transform>();
        private TetherfallGame game;
        private TetherfallMainStoryDirector director;
        private Renderer signalRenderer;
        private bool attemptActive;
        private bool waitingForTransition;
        private int waveIndex = -1;
        private int integrityNoticeMask;
        private float routeIntegrity = 100f;
        private float transitionAt;

        public Transform ObjectiveTarget => transform;
        public bool IsActive => attemptActive;

        public string InteractionPrompt
        {
            get
            {
                if (director == null || director.Progress < 1)
                {
                    return "Rainline watchfire is listening";
                }

                if (director.Progress > 1)
                {
                    return "Review the held Rainline signal";
                }

                return attemptActive
                    ? $"Hold Rainline signal · {Mathf.CeilToInt(routeIntegrity)}% integrity"
                    : "Sound the Rainline defense";
            }
        }

        public static MainStoryRouteDefense Create(
            TetherfallGame game,
            TetherfallMainStoryDirector director,
            Vector3 position)
        {
            var root = new GameObject("Rainline Route Defense");
            root.transform.position = position;
            var defense = root.AddComponent<MainStoryRouteDefense>();
            defense.game = game;
            defense.director = director;
            defense.BuildSite();
            game.RegisterInteractable(defense);
            return defense;
        }

        private void Update()
        {
            if (!attemptActive || game == null || game.PlayerTransform == null)
            {
                return;
            }

            if (game.GameplayPaused)
            {
                if (waitingForTransition)
                {
                    transitionAt += Time.deltaTime;
                }

                return;
            }

            var distance = Vector3.Distance(game.PlayerTransform.position, transform.position);
            if (distance > MaximumEncounterDistance)
            {
                AbortAttempt("The Rainline pressure fades as you withdraw. Sound the signal when you are ready to try again.");
                return;
            }

            routeIntegrity += (distance <= HoldRadius ? 12f : -8f) * Time.deltaTime;
            routeIntegrity = Mathf.Clamp(routeIntegrity, 0f, 100f);
            RefreshSignalColor();
            NotifyIntegrityPressure();
            if (routeIntegrity <= 0f)
            {
                AbortAttempt("The false chord pulls the Rainline signal dark. Maelin resets its braces for another attempt.");
                return;
            }

            PruneEnemies();
            if (!waitingForTransition && activeEnemies.Count == 0)
            {
                BeginTransition();
            }

            if (!waitingForTransition || Time.time < transitionAt)
            {
                return;
            }

            waitingForTransition = false;
            if (waveIndex + 1 >= MainStoryCatalog.AlderwakeDefenseWaves.Length)
            {
                CompleteDefense();
            }
            else
            {
                SpawnNextWave();
            }
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return director != null && director.Progress >= 1;
        }

        public void Interact(TetherfallGame owner)
        {
            if (director == null)
            {
                return;
            }

            if (director.Progress > 1)
            {
                owner.Notify("The Rainline signal holds a clean Alderwake note.");
                return;
            }

            if (attemptActive)
            {
                owner.Notify(
                    $"Rainline integrity {Mathf.CeilToInt(routeIntegrity)}%. " +
                    "Fight close to the signal when the Hollow Choir pulls at it.");
                return;
            }

            StartAttempt();
        }

        public void RefreshState()
        {
            if (director != null && director.Progress != 1 && attemptActive)
            {
                ResetCombat();
            }

            RefreshSignalColor();
        }

        private void StartAttempt()
        {
            ResetCombat();
            attemptActive = true;
            waitingForTransition = true;
            transitionAt = Time.time + 0.7f;
            routeIntegrity = 100f;
            integrityNoticeMask = 0;
            game.Notify(
                "Rainline defense begun. Stay near the signal to resist the Choir's pull; the stone braces provide retreat cover.");
            RefreshSignalColor();
        }

        private void SpawnNextWave()
        {
            waveIndex++;
            var wave = MainStoryCatalog.AlderwakeDefenseWaves[waveIndex];
            for (var index = 0; index < wave.Length; index++)
            {
                var lane = approachLanes[(index + waveIndex) % approachLanes.Count];
                var side = index % 2 == 0 ? -1f : 1f;
                var position = lane.position + lane.right * side * (1.3f + index * 0.25f);
                var enemy = Enemy.Create(game, null, wave[index], position, OnEnemyDefeated);
                if (enemy != null)
                {
                    activeEnemies.Add(enemy);
                }
            }

            game.Notify(
                waveIndex == 0
                    ? "Hollow scouts split across the west and north approaches."
                    : waveIndex == 1
                        ? "Maelin: \"I have the inner brace. Break their crossfire.\""
                        : "The Choir sends a relay breaker down the center. The signal must stay lit.");
        }

        private void OnEnemyDefeated(Enemy enemy)
        {
            activeEnemies.Remove(enemy);
        }

        private void BeginTransition()
        {
            waitingForTransition = true;
            transitionAt = Time.time + (waveIndex + 1 >= MainStoryCatalog.AlderwakeDefenseWaves.Length ? 0.8f : 1.65f);
            if (waveIndex < 0)
            {
                return;
            }

            var heal = waveIndex == 0 ? 10 : 14;
            game.HealPlayer(heal);
            MainStoryEffects.EmitAllyPulse(
                game,
                transform.position,
                new Color(0.32f, 0.94f, 0.72f));
            game.Notify(
                waveIndex + 1 >= MainStoryCatalog.AlderwakeDefenseWaves.Length
                    ? "Maelin catches the last false note and turns it back toward Morrowfen."
                    : "Maelin's routeward restores your footing. Another approach is sounding.");
        }

        private void CompleteDefense()
        {
            attemptActive = false;
            waitingForTransition = false;
            routeIntegrity = 100f;
            activeEnemies.Clear();
            RefreshSignalColor();
            director?.OnAlderwakeDefenseCompleted();
        }

        private void AbortAttempt(string message)
        {
            ResetCombat();
            game.HealPlayer(16);
            game.Notify(message);
        }

        private void ResetCombat()
        {
            attemptActive = false;
            waitingForTransition = false;
            waveIndex = -1;
            routeIntegrity = 100f;
            integrityNoticeMask = 0;
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    enemy.gameObject.SetActive(false);
                    Destroy(enemy.gameObject);
                }
            }

            activeEnemies.Clear();
            RefreshSignalColor();
        }

        private void PruneEnemies()
        {
            for (var index = activeEnemies.Count - 1; index >= 0; index--)
            {
                if (activeEnemies[index] == null || activeEnemies[index].IsDefeated)
                {
                    activeEnemies.RemoveAt(index);
                }
            }
        }

        private void NotifyIntegrityPressure()
        {
            if (routeIntegrity <= 25f && (integrityNoticeMask & 4) == 0)
            {
                integrityNoticeMask |= 4;
                game.Notify("Rainline integrity critical. Return to the lit inner ring.");
            }
            else if (routeIntegrity <= 50f && (integrityNoticeMask & 2) == 0)
            {
                integrityNoticeMask |= 2;
                game.Notify("The Choir is pulling the signal off-key. Fight closer to its light.");
            }
            else if (routeIntegrity <= 75f && (integrityNoticeMask & 1) == 0)
            {
                integrityNoticeMask |= 1;
                game.Notify("Rainline pressure rising. The inner ring restores signal integrity.");
            }

            if (routeIntegrity >= 86f)
            {
                integrityNoticeMask = 0;
            }
        }

        private void BuildSite()
        {
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plinth.name = "Rainline signal plinth";
            plinth.transform.SetParent(transform, false);
            plinth.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            plinth.transform.localScale = new Vector3(1.45f, 0.65f, 1.45f);
            plinth.GetComponent<Renderer>().material = game.CreateMaterial(
                new Color(0.14f, 0.24f, 0.2f),
                0.12f,
                0.4f);

            var signal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            signal.name = "Rainline signal light";
            signal.transform.SetParent(transform, false);
            signal.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            signal.transform.localScale = new Vector3(0.62f, 0.88f, 0.62f);
            signalRenderer = signal.GetComponent<Renderer>();
            signalRenderer.material = game.CreateGlowMaterial(
                new Color(0.32f, 0.9f, 0.68f),
                1.55f,
                0.68f);
            Object.Destroy(signal.GetComponent<Collider>());

            for (var index = 0; index < 3; index++)
            {
                var angle = index * 120f + 18f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var lane = new GameObject($"Rainline approach {index + 1}").transform;
                lane.SetParent(transform, false);
                lane.localPosition = direction * 13.5f;
                lane.localRotation = Quaternion.LookRotation(-direction);
                approachLanes.Add(lane);

                var brace = GameObject.CreatePrimitive(PrimitiveType.Cube);
                brace.name = $"Rainline cover brace {index + 1}";
                brace.transform.SetParent(transform, false);
                brace.transform.localPosition = direction * 6.2f + Vector3.up * 1.05f;
                brace.transform.localRotation = Quaternion.Euler(0f, angle + 90f, 0f);
                brace.transform.localScale = new Vector3(3.4f, 2.1f, 0.75f);
                brace.GetComponent<Renderer>().material = game.CreateMaterial(
                    new Color(0.22f, 0.31f, 0.24f),
                    0.08f,
                    0.3f);

                var laneMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                laneMarker.name = $"Rainline approach marker {index + 1}";
                laneMarker.transform.SetParent(transform, false);
                laneMarker.transform.localPosition = direction * 9.2f + Vector3.up * 0.35f;
                laneMarker.transform.localScale = new Vector3(0.16f, 0.35f, 0.16f);
                laneMarker.GetComponent<Renderer>().material = game.CreateGlowMaterial(
                    new Color(0.27f, 0.68f, 0.52f),
                    0.9f,
                    0.55f);
                Object.Destroy(laneMarker.GetComponent<Collider>());
            }

            WorldVisuals.AddLabel(
                transform,
                "Rainline Watch",
                new Vector3(0f, 3.65f, 0f),
                new Color(0.68f, 0.94f, 0.77f),
                0.105f);
            RefreshSignalColor();
        }

        private void RefreshSignalColor()
        {
            if (signalRenderer == null)
            {
                return;
            }

            signalRenderer.material.color = director != null && director.Progress > 1
                ? new Color(0.65f, 0.96f, 0.79f)
                : attemptActive
                    ? Color.Lerp(
                        new Color(0.95f, 0.22f, 0.3f),
                        new Color(0.32f, 0.9f, 0.68f),
                        routeIntegrity / 100f)
                    : new Color(0.32f, 0.72f, 0.58f);
        }
    }

    public sealed class MainStoryObservatory : MonoBehaviour
    {
        private const float HalfWidth = 27f;
        private const float HalfDepth = 48f;

        private readonly List<MainStoryResonanceTrace> traces = new List<MainStoryResonanceTrace>();
        private readonly List<GameObject> sealDoors = new List<GameObject>();
        private readonly List<Enemy> activeEnemies = new List<Enemy>();
        private TetherfallGame game;
        private TetherfallMainStoryDirector director;
        private MainStoryObservatoryGate gate;
        private MainStoryResonanceLens lens;
        private bool attemptActive;
        private bool roomCombatActive;
        private int roomIndex;

        public bool IsPlayerInside =>
            game != null &&
            game.PlayerTransform != null &&
            Mathf.Abs(game.PlayerTransform.position.x - MainStoryCatalog.ObservatoryCenter.x) <= HalfWidth &&
            Mathf.Abs(game.PlayerTransform.position.z - MainStoryCatalog.ObservatoryCenter.z) <= HalfDepth;

        public static MainStoryObservatory Create(
            TetherfallGame game,
            TetherfallMainStoryDirector director)
        {
            var root = new GameObject("Morrowfen Resonance Observatory");
            root.transform.position = MainStoryCatalog.ObservatoryCenter;
            var observatory = root.AddComponent<MainStoryObservatory>();
            observatory.game = game;
            observatory.director = director;
            observatory.BuildObservatory();
            observatory.gate = MainStoryObservatoryGate.Create(
                game,
                observatory,
                MainStoryCatalog.ObservatoryGatePosition);
            return observatory;
        }

        private void Update()
        {
            if (!attemptActive || game == null)
            {
                return;
            }

            if (game.GameplayPaused)
            {
                return;
            }

            if (!IsPlayerInside)
            {
                AbortToSurface("The observatory's stolen chord loses you. Its chambers reset to a safe entry pattern.");
                return;
            }

            if (!roomCombatActive)
            {
                return;
            }

            PruneEnemies();
            if (activeEnemies.Count == 0)
            {
                CompleteRoom();
            }
        }

        public void Enter()
        {
            if (game == null || game.Player == null || director == null)
            {
                return;
            }

            if (director.Progress < 2)
            {
                game.Notify("The observatory cave answers only with ordinary fen echoes.");
                return;
            }

            ResetCombat();
            attemptActive = true;
            roomIndex = director.Progress == 2 ? 0 : traces.Count;
            ConfigureRooms();
            game.Player.Teleport(MainStoryCatalog.ObservatoryCenter + new Vector3(0f, 1.3f, -39f));
            game.Notify(
                director.Progress == 2
                    ? "The stolen chord retreats through three chambers. Touch each trace only when you are ready for its ambush."
                    : director.Progress == 3
                        ? "The cleared observatory waits. The recovered lens holds the Choir's destination."
                        : "The observatory remains quiet. Both return lifts are open.");
        }

        public void Exit()
        {
            if (game == null)
            {
                return;
            }

            ResetCombat();
            attemptActive = false;
            if (game.Player != null)
            {
                game.Player.Teleport(MainStoryCatalog.ObservatoryReturnPosition);
            }

            game.Notify(
                director != null && director.Progress == 2
                    ? "You surface safely. The observatory ambush will reset when you return."
                    : "You return to the Morrowfen route.");
        }

        public void StartTrace(MainStoryResonanceTrace trace)
        {
            if (trace == null ||
                director == null ||
                director.Progress != 2 ||
                !attemptActive ||
                roomCombatActive ||
                trace.RoomIndex != roomIndex ||
                roomIndex < 0 ||
                roomIndex >= MainStoryCatalog.ObservatoryWaves.Length)
            {
                return;
            }

            roomCombatActive = true;
            trace.SetState(false, true);
            var wave = MainStoryCatalog.ObservatoryWaves[roomIndex];
            for (var index = 0; index < wave.Length; index++)
            {
                var angle = index * Mathf.PI * 2f / wave.Length + roomIndex * 0.36f;
                var radius = 4.5f + index % 2 * 1.5f;
                var position = trace.transform.position + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
                var enemy = Enemy.Create(game, null, wave[index], position, OnEnemyDefeated);
                if (enemy != null)
                {
                    activeEnemies.Add(enemy);
                }
            }

            game.Notify(
                roomIndex == 0
                    ? "First chamber: scouts answer from the blind corners. The entrance lift remains open."
                    : roomIndex == 1
                        ? "Second chamber: a siltcaller pins the center while scouts cross the side lanes."
                        : "Final chamber: the Choir springs its relay-breaker ambush. The south lift is still your retreat.");
        }

        public void ReadLens()
        {
            if (director == null)
            {
                return;
            }

            if (director.Progress == 3)
            {
                director.OnObservatoryLensRead();
                ConfigureRooms();
                return;
            }

            game.Notify(
                director.Progress >= 4
                    ? "The lens remembers two answers: a road held for travelers, and a current left open by consent."
                    : "The lens is still wrapped in the stolen chord. Clear all three chambers.");
        }

        public Transform GetObjectiveTarget(bool lensStage)
        {
            if (!IsPlayerInside)
            {
                return gate != null ? gate.transform : null;
            }

            if (lensStage || director != null && director.Progress >= 3)
            {
                return lens != null ? lens.transform : null;
            }

            PruneEnemies();
            if (activeEnemies.Count > 0)
            {
                return activeEnemies[0] != null ? activeEnemies[0].transform : null;
            }

            return roomIndex >= 0 && roomIndex < traces.Count ? traces[roomIndex].transform : null;
        }

        public void RefreshState()
        {
            if (director == null)
            {
                return;
            }

            gate?.RefreshState();
            if (director.Progress != 2 && roomCombatActive)
            {
                ResetCombat();
            }

            ConfigureRooms();
        }

        private void CompleteRoom()
        {
            roomCombatActive = false;
            activeEnemies.Clear();
            var completedRoom = roomIndex;
            roomIndex++;
            game.HealPlayer(12 + completedRoom * 2);
            MainStoryEffects.EmitAllyPulse(
                game,
                traces[completedRoom].transform.position,
                new Color(0.32f, 0.82f, 0.9f));

            if (roomIndex >= traces.Count)
            {
                ConfigureRooms();
                director?.OnObservatoryAmbushCompleted();
                return;
            }

            ConfigureRooms();
            game.Notify(
                completedRoom == 0
                    ? "The first seal opens. A dry alcove restores your footing before the next trace."
                    : "The second seal opens. The recovered chord grows clearer; the entrance remains behind you.");
        }

        private void OnEnemyDefeated(Enemy enemy)
        {
            activeEnemies.Remove(enemy);
        }

        private void AbortToSurface(string message)
        {
            ResetCombat();
            attemptActive = false;
            if (game.Player != null)
            {
                game.Player.Teleport(MainStoryCatalog.ObservatoryReturnPosition);
            }

            game.HealPlayer(14);
            game.Notify(message);
        }

        private void ResetCombat()
        {
            roomCombatActive = false;
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    enemy.gameObject.SetActive(false);
                    Destroy(enemy.gameObject);
                }
            }

            activeEnemies.Clear();
            roomIndex = director != null && director.Progress == 2 ? 0 : traces.Count;
            ConfigureRooms();
        }

        private void PruneEnemies()
        {
            for (var index = activeEnemies.Count - 1; index >= 0; index--)
            {
                if (activeEnemies[index] == null || activeEnemies[index].IsDefeated)
                {
                    activeEnemies.RemoveAt(index);
                }
            }
        }

        private void ConfigureRooms()
        {
            if (director == null)
            {
                return;
            }

            var combatStage = director.Progress == 2;
            for (var index = 0; index < traces.Count; index++)
            {
                var available = combatStage && attemptActive && !roomCombatActive && index == roomIndex;
                var resolved = !combatStage || index < roomIndex;
                traces[index].SetState(available, !resolved && roomCombatActive && index == roomIndex);
            }

            for (var index = 0; index < sealDoors.Count; index++)
            {
                sealDoors[index].SetActive(combatStage && roomIndex <= index);
            }

            lens?.SetAvailable(director.Progress >= 3);
        }

        private void BuildObservatory()
        {
            var center = MainStoryCatalog.ObservatoryCenter;
            CreateBlock(
                "Observatory cave floor",
                center + Vector3.down,
                new Vector3(52f, 2f, 92f),
                new Color(0.09f, 0.15f, 0.18f));
            CreateBlock(
                "Observatory west wall",
                center + new Vector3(-26f, 4.5f, 0f),
                new Vector3(2f, 9f, 92f),
                new Color(0.11f, 0.2f, 0.22f));
            CreateBlock(
                "Observatory east wall",
                center + new Vector3(26f, 4.5f, 0f),
                new Vector3(2f, 9f, 92f),
                new Color(0.11f, 0.2f, 0.22f));
            CreateBlock(
                "Observatory south wall",
                center + new Vector3(0f, 4.5f, -46f),
                new Vector3(52f, 9f, 2f),
                new Color(0.11f, 0.2f, 0.22f));
            CreateBlock(
                "Observatory north wall",
                center + new Vector3(0f, 4.5f, 46f),
                new Vector3(52f, 9f, 2f),
                new Color(0.11f, 0.2f, 0.22f));

            var dividerZ = new[] { -11f, 11f };
            for (var index = 0; index < dividerZ.Length; index++)
            {
                CreateBlock(
                    $"Observatory chamber divider {index + 1} west",
                    center + new Vector3(-15f, 3.2f, dividerZ[index]),
                    new Vector3(22f, 6.4f, 1.2f),
                    new Color(0.12f, 0.23f, 0.25f));
                CreateBlock(
                    $"Observatory chamber divider {index + 1} east",
                    center + new Vector3(15f, 3.2f, dividerZ[index]),
                    new Vector3(22f, 6.4f, 1.2f),
                    new Color(0.12f, 0.23f, 0.25f));
                var door = CreateBlock(
                    $"Observatory resonance seal {index + 1}",
                    center + new Vector3(0f, 2.7f, dividerZ[index]),
                    new Vector3(8f, 5.4f, 1f),
                    new Color(0.28f, 0.13f, 0.35f));
                sealDoors.Add(door);
            }

            var roomOffsets = new[] { -30f, -3f, 25f };
            for (var index = 0; index < roomOffsets.Length; index++)
            {
                traces.Add(MainStoryResonanceTrace.Create(
                    game,
                    this,
                    center + new Vector3(index % 2 == 0 ? -5f : 5f, 0.55f, roomOffsets[index]),
                    index));
            }

            lens = MainStoryResonanceLens.Create(
                game,
                this,
                center + new Vector3(0f, 1.2f, 38f));
            MainStoryObservatoryExit.Create(
                game,
                this,
                center + new Vector3(0f, 1f, -40f),
                "South surface lift");
            MainStoryObservatoryExit.Create(
                game,
                this,
                center + new Vector3(0f, 1f, 41f),
                "North surface lift");

            WorldVisuals.AddLabel(
                transform,
                "Morrowfen Resonance Observatory",
                new Vector3(0f, 7.2f, -33f),
                new Color(0.52f, 0.88f, 0.9f),
                0.14f);
            ConfigureRooms();
        }

        private GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Color color)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().material = game.CreateMaterial(color, 0.08f, 0.3f);
            return block;
        }
    }

    public sealed class MainStoryObservatoryGate : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private MainStoryObservatory observatory;
        private Renderer body;

        public string InteractionPrompt => "Enter the resonance observatory";

        public static MainStoryObservatoryGate Create(
            TetherfallGame game,
            MainStoryObservatory observatory,
            Vector3 position)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "Morrowfen observatory cave mouth";
            root.transform.position = position;
            root.transform.localScale = new Vector3(2.3f, 1.15f, 2.3f);
            var gate = root.AddComponent<MainStoryObservatoryGate>();
            gate.game = game;
            gate.observatory = observatory;
            gate.body = root.GetComponent<Renderer>();
            gate.body.material = game.CreateMaterial(
                new Color(0.15f, 0.33f, 0.36f),
                0.18f,
                0.48f);

            var chord = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chord.name = "Stolen resonance chord";
            chord.transform.SetParent(root.transform, false);
            chord.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            chord.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            chord.transform.localScale = new Vector3(0.22f, 0.8f, 0.22f);
            chord.GetComponent<Renderer>().material = game.CreateGlowMaterial(
                new Color(0.68f, 0.27f, 0.72f),
                1.45f,
                0.68f);
            Object.Destroy(chord.GetComponent<Collider>());

            WorldVisuals.AddLabel(
                root.transform,
                "Resonance observatory",
                new Vector3(0f, 2.55f, 0f),
                new Color(0.61f, 0.9f, 0.91f),
                0.09f);
            game.RegisterInteractable(gate);
            return gate;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            observatory?.Enter();
        }

        public void RefreshState()
        {
            if (body != null)
            {
                body.material.color = new Color(0.15f, 0.33f, 0.36f);
            }
        }
    }

    public sealed class MainStoryObservatoryExit : MonoBehaviour, IWorldInteractable
    {
        private MainStoryObservatory observatory;

        public string InteractionPrompt => "Return to the Morrowfen surface";

        public static MainStoryObservatoryExit Create(
            TetherfallGame game,
            MainStoryObservatory observatory,
            Vector3 position,
            string label)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = label;
            root.transform.position = position;
            root.transform.localScale = new Vector3(1.8f, 1f, 1.8f);
            root.GetComponent<Renderer>().material = game.CreateMaterial(
                new Color(0.31f, 0.72f, 0.67f),
                0.16f,
                0.55f);
            var exit = root.AddComponent<MainStoryObservatoryExit>();
            exit.observatory = observatory;
            WorldVisuals.AddLabel(
                root.transform,
                "Safe surface route",
                new Vector3(0f, 1.65f, 0f),
                new Color(0.68f, 0.95f, 0.83f),
                0.075f);
            game.RegisterInteractable(exit);
            return exit;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            observatory?.Exit();
        }
    }

    public sealed class MainStoryResonanceTrace : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private MainStoryObservatory observatory;
        private Renderer body;
        private bool available;
        private bool sounding;

        public int RoomIndex { get; private set; }
        public string InteractionPrompt => available
            ? $"Follow resonance trace {RoomIndex + 1}"
            : sounding
                ? "Ambush in progress"
                : "Resonance trace quiet";

        public static MainStoryResonanceTrace Create(
            TetherfallGame game,
            MainStoryObservatory observatory,
            Vector3 position,
            int roomIndex)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = $"Stolen resonance trace {roomIndex + 1}";
            root.transform.position = position;
            root.transform.localScale = new Vector3(0.8f, 1.15f, 0.8f);
            var trace = root.AddComponent<MainStoryResonanceTrace>();
            trace.game = game;
            trace.observatory = observatory;
            trace.RoomIndex = roomIndex;
            trace.body = root.GetComponent<Renderer>();
            trace.body.material = game.CreateGlowMaterial(
                new Color(0.45f, 0.2f, 0.56f),
                1.2f,
                0.62f);
            WorldVisuals.AddLabel(
                root.transform,
                $"Stolen chord · {roomIndex + 1}",
                new Vector3(0f, 1.6f, 0f),
                new Color(0.78f, 0.64f, 0.9f),
                0.07f);
            game.RegisterInteractable(trace);
            return trace;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return available;
        }

        public void Interact(TetherfallGame owner)
        {
            observatory?.StartTrace(this);
        }

        public void SetState(bool canStart, bool isSounding)
        {
            available = canStart;
            sounding = isSounding;
            if (body != null)
            {
                body.material.color = sounding
                    ? new Color(0.96f, 0.22f, 0.36f)
                    : available
                        ? new Color(0.64f, 0.34f, 0.78f)
                        : new Color(0.18f, 0.35f, 0.38f);
            }
        }
    }

    public sealed class MainStoryResonanceLens : MonoBehaviour, IWorldInteractable
    {
        private MainStoryObservatory observatory;
        private Renderer body;
        private bool available;

        public string InteractionPrompt => available
            ? "Read the recovered resonance lens"
            : "Lens wrapped in a stolen chord";

        public static MainStoryResonanceLens Create(
            TetherfallGame game,
            MainStoryObservatory observatory,
            Vector3 position)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "Recovered resonance lens";
            root.transform.position = position;
            root.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            root.transform.localScale = new Vector3(1.65f, 0.3f, 1.65f);
            var lens = root.AddComponent<MainStoryResonanceLens>();
            lens.observatory = observatory;
            lens.body = root.GetComponent<Renderer>();
            lens.body.material = game.CreateGlowMaterial(
                new Color(0.28f, 0.66f, 0.78f),
                1.5f,
                0.72f);
            WorldVisuals.AddLabel(
                root.transform,
                "Recovered witness lens",
                new Vector3(0f, 2.2f, 0f),
                new Color(0.66f, 0.93f, 0.92f),
                0.085f);
            game.RegisterInteractable(lens);
            return lens;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            observatory?.ReadLens();
        }

        public void SetAvailable(bool isAvailable)
        {
            available = isAvailable;
            if (body != null)
            {
                body.material.color = available
                    ? new Color(0.42f, 0.9f, 0.86f)
                    : new Color(0.28f, 0.26f, 0.42f);
            }
        }
    }

    public sealed class MainStoryBreach : MonoBehaviour, IDungeonBossArena
    {
        private const float MaximumEncounterDistance = 45f;

        private TetherfallGame game;
        private TetherfallMainStoryDirector director;
        private MainStoryRouteSignal hearthlineSignal;
        private MainStoryRouteSignal openCurrentSignal;
        private DungeonBoss boss;
        private MainStoryRouteChoice routeChoice;
        private bool encounterActive;
        private bool reliefLatchActive;
        private bool storyComplete;

        public static MainStoryBreach Create(
            TetherfallGame game,
            TetherfallMainStoryDirector director,
            Vector3 center)
        {
            var root = new GameObject("Cinderwold Hollow Choir Breach");
            root.transform.position = center;
            var breach = root.AddComponent<MainStoryBreach>();
            breach.game = game;
            breach.director = director;
            breach.BuildArena();
            return breach;
        }

        private void Update()
        {
            if (!encounterActive || game == null || game.PlayerTransform == null)
            {
                return;
            }

            if (game.GameplayPaused)
            {
                return;
            }

            var playerOffset = game.PlayerTransform.position - transform.position;
            if (new Vector2(playerOffset.x, playerOffset.z).magnitude > MaximumEncounterDistance ||
                Mathf.Abs(playerOffset.y) > 10f)
            {
                AbortEncounter("The breach falls out of hearing. The protected route remains ready for another challenge.");
                return;
            }

            if (boss == null && director != null && director.Progress == 5)
            {
                AbortEncounter("The Hollow Cantor loses its shape. Sound the protected route to call it back cleanly.");
            }
        }

        public void RefreshState(MainStoryRouteChoice choice, int progress, bool complete)
        {
            routeChoice = choice;
            storyComplete = complete;
            if (progress != 5 && encounterActive)
            {
                AbortEncounter(string.Empty);
            }

            hearthlineSignal?.SetState(
                choice == MainStoryRouteChoice.Hearthline,
                reliefLatchActive && choice == MainStoryRouteChoice.Hearthline,
                complete);
            openCurrentSignal?.SetState(
                choice == MainStoryRouteChoice.OpenCurrent,
                reliefLatchActive && choice == MainStoryRouteChoice.OpenCurrent,
                complete);
        }

        public Transform GetChoiceTarget(MainStoryRouteChoice suggested)
        {
            return suggested == MainStoryRouteChoice.OpenCurrent
                ? openCurrentSignal != null ? openCurrentSignal.transform : transform
                : hearthlineSignal != null ? hearthlineSignal.transform : transform;
        }

        public Transform GetCombatObjectiveTarget()
        {
            if (reliefLatchActive)
            {
                return routeChoice == MainStoryRouteChoice.OpenCurrent
                    ? openCurrentSignal != null ? openCurrentSignal.transform : transform
                    : hearthlineSignal != null ? hearthlineSignal.transform : transform;
            }

            if (boss != null && boss.gameObject.activeInHierarchy)
            {
                return boss.transform;
            }

            return routeChoice == MainStoryRouteChoice.OpenCurrent
                ? openCurrentSignal != null ? openCurrentSignal.transform : transform
                : hearthlineSignal != null ? hearthlineSignal.transform : transform;
        }

        public bool CanInteractRoute(MainStoryRouteChoice choice)
        {
            if (director == null)
            {
                return false;
            }

            if (director.Progress == 4)
            {
                return true;
            }

            return choice == routeChoice && (director.Progress == 5 || storyComplete);
        }

        public string GetRoutePrompt(MainStoryRouteChoice choice)
        {
            var routeName = choice == MainStoryRouteChoice.Hearthline ? "Hearthline" : "Open Current";
            if (director == null)
            {
                return $"Inspect {routeName}";
            }

            if (director.Progress == 4)
            {
                var suggested = game.GetSuggestedMainStoryRouteChoice() == choice
                    ? " · aligned with current routework"
                    : string.Empty;
                return $"Protect {routeName}{suggested}";
            }

            if (choice != routeChoice)
            {
                return $"{routeName} was not chosen";
            }

            if (storyComplete)
            {
                return $"Review the protected {routeName}";
            }

            if (encounterActive && reliefLatchActive)
            {
                return $"Vent {routeName} relief latch";
            }

            return encounterActive
                ? $"{routeName} is carrying the fight"
                : $"Sound the protected {routeName}";
        }

        public void InteractWithRoute(MainStoryRouteChoice choice)
        {
            if (director == null || !CanInteractRoute(choice))
            {
                return;
            }

            if (director.Progress == 4)
            {
                if (director.ChooseRoute(choice))
                {
                    routeChoice = choice;
                    RefreshState(routeChoice, director.Progress, false);
                    StartEncounter();
                }

                return;
            }

            if (storyComplete)
            {
                game.Notify(game.GetMainStoryCinderwoldContext(choice));
                return;
            }

            if (reliefLatchActive && encounterActive)
            {
                ReleaseReliefLatch(choice);
                return;
            }

            if (!encounterActive)
            {
                StartEncounter();
                return;
            }

            game.Notify("The protected route is carrying a clean signal. Keep pressure on the Hollow Cantor.");
        }

        public void Retreat()
        {
            if (!encounterActive)
            {
                game.Notify("The retreat shelf is clear.");
                return;
            }

            AbortEncounter("You fall back to the marked shelf. The route choice holds; sound it again when ready.");
            game.HealPlayer(18);
        }

        public bool CanDamageBoss(DungeonBoss activeBoss)
        {
            return encounterActive &&
                   !reliefLatchActive &&
                   activeBoss != null &&
                   activeBoss == boss;
        }

        public void OnBossPhaseThreshold(DungeonBoss activeBoss, int phase)
        {
            if (!encounterActive || activeBoss == null || activeBoss != boss)
            {
                return;
            }

            if (phase == 2)
            {
                reliefLatchActive = true;
                activeBoss.ApplyStagger(4.5f);
                RefreshState(routeChoice, director.Progress, false);
                game.Notify(
                    routeChoice == MainStoryRouteChoice.OpenCurrent
                        ? "Phase two: Vesryn folds the Open Current inward. Vent its lit relief latch; Maelin holds the far brace."
                        : "Phase two: Vesryn folds the Hearthline inward. Vent its lit relief latch; Maelin holds the far brace.");
                MainStoryEffects.EmitWarningPulse(game, transform.position + new Vector3(0f, 0.08f, 9f));
                return;
            }

            if (phase == 3)
            {
                reliefLatchActive = false;
                activeBoss.ApplyStagger(1.8f);
                game.HealPlayer(18);
                RefreshState(routeChoice, director.Progress, false);
                MainStoryEffects.EmitAllyPulse(
                    game,
                    transform.position,
                    routeChoice == MainStoryRouteChoice.OpenCurrent
                        ? new Color(0.42f, 0.83f, 0.92f)
                        : new Color(0.96f, 0.63f, 0.31f));
                game.Notify(
                    "Phase three: the false chorus breaks into readable volleys, charges, and ground rings. " +
                    "Maelin's answer restores your footing.");
            }
        }

        public void OnBossDefeated(DungeonBoss activeBoss)
        {
            if (!encounterActive || activeBoss == null || activeBoss != boss)
            {
                return;
            }

            encounterActive = false;
            reliefLatchActive = false;
            game.ClearDungeonBoss(activeBoss);
            boss = null;
            director?.OnHollowCantorDefeated();
            RefreshState(routeChoice, MainStoryCatalog.CompletedProgress, true);
        }

        private void StartEncounter()
        {
            if (encounterActive ||
                director == null ||
                director.Progress != 5 ||
                routeChoice == MainStoryRouteChoice.None)
            {
                return;
            }

            ClearBoss();
            var definition = BossCatalog.Get(MainStoryCatalog.BossId);
            if (definition == null)
            {
                game.Notify("The Hollow Cantor definition is unavailable. The route remains safe to retry.");
                return;
            }

            encounterActive = true;
            reliefLatchActive = false;
            boss = DungeonBoss.Create(
                game,
                this,
                definition,
                transform.position + new Vector3(0f, 1.6f, 10f));
            HollowCantorVisualBuilder.Build(game, boss);
            game.RegisterDungeonBoss(boss);
            RefreshState(routeChoice, director.Progress, false);
            game.Notify(
                "Vesryn enters in three phrases. Every strike is telegraphed; the marked retreat shelf stays open behind you.");
        }

        private void ReleaseReliefLatch(MainStoryRouteChoice choice)
        {
            if (!reliefLatchActive || choice != routeChoice)
            {
                return;
            }

            reliefLatchActive = false;
            if (boss != null)
            {
                boss.ApplyStagger(5.2f);
            }

            game.HealPlayer(14);
            RefreshState(routeChoice, director.Progress, false);
            MainStoryEffects.EmitAllyPulse(
                game,
                choice == MainStoryRouteChoice.OpenCurrent
                    ? openCurrentSignal.transform.position
                    : hearthlineSignal.transform.position,
                choice == MainStoryRouteChoice.OpenCurrent
                    ? new Color(0.42f, 0.87f, 0.94f)
                    : new Color(0.98f, 0.65f, 0.32f));
            game.Notify("The relief channel opens. Vesryn's core is exposed for a long counterattack.");
        }

        private void AbortEncounter(string message)
        {
            encounterActive = false;
            reliefLatchActive = false;
            ClearBoss();
            RefreshState(routeChoice, director != null ? director.Progress : 5, false);
            if (!string.IsNullOrEmpty(message))
            {
                game.Notify(message);
            }
        }

        private void ClearBoss()
        {
            if (boss == null)
            {
                return;
            }

            var oldBoss = boss;
            boss = null;
            game.ClearDungeonBoss(oldBoss);
            oldBoss.gameObject.SetActive(false);
            Destroy(oldBoss.gameObject);
        }

        private void BuildArena()
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Hollow Choir breach ring";
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.08f, 5f);
            ring.transform.localScale = new Vector3(12f, 0.08f, 12f);
            ring.GetComponent<Renderer>().material = game.CreateMaterial(
                new Color(0.23f, 0.09f, 0.12f),
                0.14f,
                0.38f);
            Object.Destroy(ring.GetComponent<Collider>());

            for (var index = 0; index < 4; index++)
            {
                var angle = 45f + index * 90f;
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cover.name = $"Breach recovery stone {index + 1}";
                cover.transform.SetParent(transform, false);
                cover.transform.localPosition = direction * 11f + new Vector3(0f, 1.2f, 5f);
                cover.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                cover.transform.localScale = new Vector3(3f, 2.4f, 1.1f);
                cover.GetComponent<Renderer>().material = game.CreateMaterial(
                    new Color(0.3f, 0.12f, 0.1f),
                    0.1f,
                    0.32f);
            }

            hearthlineSignal = MainStoryRouteSignal.Create(
                game,
                this,
                transform.position + new Vector3(-5f, 0.9f, -10f),
                MainStoryRouteChoice.Hearthline);
            openCurrentSignal = MainStoryRouteSignal.Create(
                game,
                this,
                transform.position + new Vector3(5f, 0.9f, -10f),
                MainStoryRouteChoice.OpenCurrent);
            MainStoryBreachRetreat.Create(
                game,
                this,
                transform.position + new Vector3(0f, 0.9f, -17f));

            var safePath = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safePath.name = "Breach marked retreat path";
            safePath.transform.SetParent(transform, false);
            safePath.transform.localPosition = new Vector3(0f, 0.06f, -9f);
            safePath.transform.localScale = new Vector3(3f, 0.08f, 16f);
            safePath.GetComponent<Renderer>().material = game.CreateTransparentMaterial(
                new Color(0.31f, 0.8f, 0.68f, 0.3f),
                0.52f);
            Object.Destroy(safePath.GetComponent<Collider>());

            WorldVisuals.AddLabel(
                transform,
                "Cinderwold Listening Breach",
                new Vector3(0f, 5.8f, 6f),
                new Color(0.96f, 0.55f, 0.35f),
                0.13f);
        }
    }

    public sealed class MainStoryRouteSignal : MonoBehaviour, IWorldInteractable
    {
        private MainStoryBreach breach;
        private MainStoryRouteChoice choice;
        private Renderer body;
        private Renderer crown;
        private bool selected;
        private bool relief;
        private bool complete;

        public string InteractionPrompt => breach != null
            ? breach.GetRoutePrompt(choice)
            : "Inspect route signal";

        public static MainStoryRouteSignal Create(
            TetherfallGame game,
            MainStoryBreach breach,
            Vector3 position,
            MainStoryRouteChoice choice)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var routeName = choice == MainStoryRouteChoice.Hearthline ? "Hearthline" : "Open Current";
            root.name = $"{routeName} relief latch";
            root.transform.position = position;
            root.transform.localScale = new Vector3(1.35f, 0.9f, 1.35f);
            var signal = root.AddComponent<MainStoryRouteSignal>();
            signal.breach = breach;
            signal.choice = choice;
            signal.body = root.GetComponent<Renderer>();
            signal.body.material = game.CreateMaterial(
                choice == MainStoryRouteChoice.Hearthline
                    ? new Color(0.42f, 0.2f, 0.1f)
                    : new Color(0.11f, 0.3f, 0.35f),
                0.18f,
                0.48f);

            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = $"{routeName} route crown";
            crown.transform.SetParent(root.transform, false);
            crown.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            crown.transform.localScale = new Vector3(0.48f, 0.72f, 0.48f);
            signal.crown = crown.GetComponent<Renderer>();
            signal.crown.material = game.CreateGlowMaterial(
                choice == MainStoryRouteChoice.Hearthline
                    ? new Color(0.96f, 0.58f, 0.26f)
                    : new Color(0.32f, 0.82f, 0.92f),
                1.45f,
                0.68f);
            Object.Destroy(crown.GetComponent<Collider>());

            WorldVisuals.AddLabel(
                root.transform,
                routeName,
                new Vector3(0f, 2.5f, 0f),
                choice == MainStoryRouteChoice.Hearthline
                    ? new Color(1f, 0.72f, 0.42f)
                    : new Color(0.58f, 0.92f, 0.96f),
                0.085f);
            game.RegisterInteractable(signal);
            signal.SetState(false, false, false);
            return signal;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return breach != null && breach.CanInteractRoute(choice);
        }

        public void Interact(TetherfallGame owner)
        {
            breach?.InteractWithRoute(choice);
        }

        public void SetState(bool isSelected, bool isReliefLatch, bool isComplete)
        {
            selected = isSelected;
            relief = isReliefLatch;
            complete = isComplete;
            if (body != null)
            {
                body.material.color = relief
                    ? new Color(0.92f, 0.23f, 0.18f)
                    : selected
                        ? choice == MainStoryRouteChoice.Hearthline
                            ? new Color(0.68f, 0.31f, 0.12f)
                            : new Color(0.12f, 0.48f, 0.56f)
                        : new Color(0.16f, 0.14f, 0.17f);
            }

            if (crown != null)
            {
                crown.material.color = relief
                    ? new Color(1f, 0.28f, 0.18f)
                    : complete
                        ? Color.white
                        : selected
                            ? choice == MainStoryRouteChoice.Hearthline
                                ? new Color(1f, 0.66f, 0.29f)
                                : new Color(0.38f, 0.9f, 0.96f)
                            : new Color(0.33f, 0.25f, 0.38f);
            }
        }
    }

    public sealed class MainStoryBreachRetreat : MonoBehaviour, IWorldInteractable
    {
        private MainStoryBreach breach;

        public string InteractionPrompt => "Fall back along the marked route";

        public static MainStoryBreachRetreat Create(
            TetherfallGame game,
            MainStoryBreach breach,
            Vector3 position)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "Cinderwold breach retreat shelf";
            root.transform.position = position;
            root.transform.localScale = new Vector3(1.8f, 0.9f, 1.8f);
            root.GetComponent<Renderer>().material = game.CreateMaterial(
                new Color(0.26f, 0.64f, 0.57f),
                0.16f,
                0.52f);
            var retreat = root.AddComponent<MainStoryBreachRetreat>();
            retreat.breach = breach;
            WorldVisuals.AddLabel(
                root.transform,
                "Marked retreat shelf",
                new Vector3(0f, 1.7f, 0f),
                new Color(0.66f, 0.94f, 0.79f),
                0.075f);
            game.RegisterInteractable(retreat);
            return retreat;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            breach?.Retreat();
        }
    }

    public static class HollowCantorVisualBuilder
    {
        public const int RendererBudget = 18;
        public const int MaterialBudget = 4;
        private const string RootName = "Hollow Cantor Collider-Free Art";

        public static Transform Build(TetherfallGame game, DungeonBoss boss)
        {
            if (game == null || boss == null)
            {
                return null;
            }

            var existing = boss.transform.Find(RootName);
            if (existing != null)
            {
                return existing;
            }

            var primitiveRenderer = boss.GetComponent<Renderer>();
            if (primitiveRenderer != null)
            {
                primitiveRenderer.enabled = false;
            }

            var dark = RuntimeEffectMaterialCache.GetOpaque(
                game,
                new Color(0.055f, 0.045f, 0.09f),
                0.12f,
                0.28f);
            var shell = RuntimeEffectMaterialCache.GetOpaque(
                game,
                new Color(0.21f, 0.12f, 0.29f),
                0.2f,
                0.46f);
            var pale = RuntimeEffectMaterialCache.GetGlow(
                game,
                new Color(0.48f, 0.94f, 0.82f),
                1.7f,
                0.72f);
            var warning = RuntimeEffectMaterialCache.GetGlow(
                game,
                new Color(0.92f, 0.2f, 0.37f),
                1.85f,
                0.7f);

            var root = new GameObject(RootName).transform;
            root.SetParent(boss.transform, false);
            var resonantRenderers = new List<Renderer>();

            AddPart(root, PrimitiveType.Capsule, "Cantor hollow spindle",
                new Vector3(0f, 0.72f, 0f), new Vector3(0.32f, 0.72f, 0.32f),
                Quaternion.identity, dark, null);
            AddPart(root, PrimitiveType.Cube, "Cantor bell mantle",
                new Vector3(0f, 0.78f, 0.12f), new Vector3(0.62f, 0.58f, 0.34f),
                Quaternion.Euler(-8f, 0f, 0f), shell, null);
            AddPart(root, PrimitiveType.Sphere, "Cantor listening mask",
                new Vector3(0f, 1.25f, 0.12f), new Vector3(0.42f, 0.3f, 0.28f),
                Quaternion.identity, shell, null);
            AddPart(root, PrimitiveType.Cube, "Cantor silent mouth",
                new Vector3(0f, 1.2f, 0.38f), new Vector3(0.28f, 0.065f, 0.08f),
                Quaternion.identity, warning, resonantRenderers);
            AddPart(root, PrimitiveType.Cylinder, "Cantor left answer bell",
                new Vector3(-0.58f, 0.86f, 0f), new Vector3(0.22f, 0.38f, 0.22f),
                Quaternion.Euler(0f, 0f, 24f), shell, null);
            AddPart(root, PrimitiveType.Cylinder, "Cantor right answer bell",
                new Vector3(0.58f, 0.86f, 0f), new Vector3(0.22f, 0.38f, 0.22f),
                Quaternion.Euler(0f, 0f, -24f), shell, null);
            AddPart(root, PrimitiveType.Cube, "Cantor left tuning fork",
                new Vector3(-0.48f, 0.2f, 0.08f), new Vector3(0.09f, 0.58f, 0.09f),
                Quaternion.Euler(0f, 0f, 16f), pale, resonantRenderers);
            AddPart(root, PrimitiveType.Cube, "Cantor right tuning fork",
                new Vector3(0.48f, 0.2f, 0.08f), new Vector3(0.09f, 0.58f, 0.09f),
                Quaternion.Euler(0f, 0f, -16f), pale, resonantRenderers);
            AddPart(root, PrimitiveType.Cube, "Cantor left split vane",
                new Vector3(-0.28f, -0.05f, -0.04f), new Vector3(0.16f, 0.52f, 0.13f),
                Quaternion.Euler(0f, 0f, 19f), dark, null);
            AddPart(root, PrimitiveType.Cube, "Cantor right split vane",
                new Vector3(0.28f, -0.05f, -0.04f), new Vector3(0.16f, 0.52f, 0.13f),
                Quaternion.Euler(0f, 0f, -19f), dark, null);
            AddPart(root, PrimitiveType.Sphere, "Cantor stolen chord core",
                new Vector3(0f, 0.75f, 0.48f), Vector3.one * 0.18f,
                Quaternion.identity, warning, resonantRenderers);

            var halo = new GameObject("Cantor broken listening halo").transform;
            halo.SetParent(root, false);
            halo.localPosition = new Vector3(0f, 1.02f, -0.28f);
            for (var index = 0; index < 6; index++)
            {
                var angle = index * Mathf.PI * 2f / 6f;
                AddPart(
                    halo,
                    PrimitiveType.Cube,
                    $"Cantor halo chime {index + 1}",
                    new Vector3(Mathf.Cos(angle) * 0.74f, Mathf.Sin(angle) * 0.74f, 0f),
                    new Vector3(0.09f, 0.24f, 0.08f),
                    Quaternion.Euler(0f, 0f, -index * 60f),
                    index % 2 == 0 ? pale : warning,
                    resonantRenderers);
            }

            var motion = root.gameObject.AddComponent<HollowCantorVisual>();
            motion.Initialize(boss, halo, resonantRenderers.ToArray());
            return root;
        }

        private static Renderer AddPart(
            Transform parent,
            PrimitiveType primitive,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material,
            List<Renderer> trackedRenderers)
        {
            var part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            trackedRenderers?.Add(renderer);
            Object.Destroy(part.GetComponent<Collider>());
            return renderer;
        }
    }

    public sealed class HollowCantorVisual : MonoBehaviour
    {
        private DungeonBoss boss;
        private Transform halo;
        private Renderer[] resonantRenderers;
        private MaterialPropertyBlock propertyBlock;
        private Vector3 baseScale;

        public void Initialize(
            DungeonBoss owner,
            Transform rotatingHalo,
            Renderer[] phaseRenderers)
        {
            boss = owner;
            halo = rotatingHalo;
            resonantRenderers = phaseRenderers ?? System.Array.Empty<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            baseScale = transform.localScale;
        }

        private void Update()
        {
            if (boss == null)
            {
                return;
            }

            if (halo != null)
            {
                halo.Rotate(0f, 0f, Time.deltaTime * (boss.Health > boss.MaxHealth * 0.35f ? 42f : 68f));
            }

            var healthRatio = boss.Health / (float)Mathf.Max(1, boss.MaxHealth);
            var phaseColor = healthRatio > 0.7f
                ? new Color(0.48f, 0.94f, 0.82f)
                : healthRatio > 0.35f
                    ? new Color(0.94f, 0.38f, 0.52f)
                    : new Color(1f, 0.68f, 0.3f);
            var pulse = 0.86f + Mathf.Sin(Time.time * (healthRatio > 0.35f ? 4.2f : 6.4f)) * 0.14f;
            transform.localScale = baseScale * (0.96f + pulse * 0.04f);
            foreach (var renderer in resonantRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", phaseColor * pulse);
                propertyBlock.SetColor("_BaseColor", phaseColor * pulse);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }

    public static class MainStoryEffects
    {
        public static void EmitAllyPulse(TetherfallGame game, Vector3 position, Color color)
        {
            if (game == null)
            {
                return;
            }

            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                "Main story ally routeward",
                new Vector3(position.x, 0.08f, position.z),
                new Vector3(0.3f, 0.04f, 0.3f),
                RuntimeEffectMaterialCache.GetTransparent(game, new Color(color.r, color.g, color.b, 0.62f), 0.62f),
                0.75f,
                12f);
            CombatFeedback.CreateImpact(game, position + Vector3.up * 0.35f, color, 0.9f);
        }

        public static void EmitWarningPulse(TetherfallGame game, Vector3 position)
        {
            if (game == null)
            {
                return;
            }

            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                "Hollow Choir phase warning",
                position,
                new Vector3(0.28f, 0.04f, 0.28f),
                RuntimeEffectMaterialCache.GetTransparent(
                    game,
                    new Color(0.96f, 0.2f, 0.34f, 0.68f),
                    0.62f),
                1.1f,
                20f);
        }
    }
}
