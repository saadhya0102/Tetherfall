// THESIS: A compact third-person field expedition, not a menu-heavy RPG shell.
// OWN-WORLD: Slate, copper, rain-teal, and grounded field-instrument signals.
// STORY: The player becomes a Wayfinder by bonding, recruiting, restoring, and exploring.
// FIRST VIEWPORT: The landscape leads; HUD stays peripheral until the player needs it.
// FORM: Threadglass field-kit overlay for an original, readable operating surface.
// FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, and DESIGN.md.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Tetherfall
{
    public enum ResourceKind
    {
        Timber,
        Stone,
        Resin,
        Chimesalt,
        Herbs,
        Ore,
        Fiber
    }

    public enum ResourceAcquisitionSource
    {
        AuthoredReward,
        WorldGather,
        VendorTrade,
        SettlementTransfer
    }

    public interface IWorldInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract(TetherfallGame game);
        void Interact(TetherfallGame game);
    }

    public sealed partial class TetherfallGame : MonoBehaviour
    {
        private const float InteractionRange = 3.4f;
        private const int CampTimberCost = 3;
        private const int CampStoneCost = 2;
        private const float AutoSaveInterval = 20f;
        private const float SaveCoalesceDelay = 1.25f;
        private const float CombatSaveRetryDelay = 0.75f;
        private const float InteractionScanInterval = 0.12f;
        private const float InteractionVerticalLimit = 2.4f;
        private const float ExperienceFeedbackDuration = 2.8f;
        private const int CampPlacementQueryCapacity = 32;
        private const int MaximumWorldGatherBaseYieldForExperience = 5;
        public const int MaximumWorldGatherExperience = 30;
        public const float MinimumModalCanvasWidth = 1040f;
        public const float MinimumModalCanvasHeight = 690f;
        public const string HollowReedMilestoneId = "milestone.alderwake.hollow_reed";

        public static TetherfallGame Instance { get; private set; }
        public PlayerAvatar Player { get; private set; }
        public Transform PlayerTransform => Player != null ? Player.transform : transform;
        public IReadOnlyList<HumanCompanion> Party => party;
        public PactCreature ActiveEchosworn { get; private set; }
        public PactCreature MountEchosworn { get; private set; }
        public CampSite ActiveCamp { get; private set; }
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int ExperienceToNext { get; private set; } = 100;
        public int Health { get; private set; } = 100;
        public int MaxHealth { get; private set; } = 100;
        public bool CodexOpen { get; private set; }
        public string CurrentRegion { get; private set; } = "Alderwake Basin";

        private readonly Dictionary<ResourceKind, int> resources = new Dictionary<ResourceKind, int>
        {
            { ResourceKind.Timber, 0 },
            { ResourceKind.Stone, 0 },
            { ResourceKind.Resin, 0 },
            { ResourceKind.Chimesalt, 0 },
            { ResourceKind.Herbs, 0 },
            { ResourceKind.Ore, 0 },
            { ResourceKind.Fiber, 0 }
        };

        private readonly Collider[] campPlacementOverlapBuffer =
            new Collider[CampPlacementQueryCapacity];
        private readonly List<MonoBehaviour> interactables = new List<MonoBehaviour>();
        private readonly List<HumanCompanion> party = new List<HumanCompanion>();
        private readonly List<EnemyCamp> enemyCamps = new List<EnemyCamp>();
        private readonly List<NotificationEntry> notifications = new List<NotificationEntry>();
        private readonly HashSet<string> discoveredRegions = new HashSet<string>();
        private readonly List<PactCreature> pacts = new List<PactCreature>();
        private readonly List<PactCreature> knownEchosworn = new List<PactCreature>();
        private readonly List<HumanCompanion> knownCompanions = new List<HumanCompanion>();

        private ThirdPersonCamera cameraRig;
        private Hearthgate finalHearthgate;
        private CraftingStation alderwakeCraftingStation;
        private DungeonGate skyvaultGate;
        private int gatheredNodes;
        private int clearedCamps;
        private bool hearthgateRestored;
        private bool showControls = true;
        private bool isRestoringSave;
        private bool saveRequested;
        private bool criticalSaveRequested;
        private float nextCampActivationAt;
        private float nextAutoSaveAt;
        private float saveNotBefore;
        private float damageFlash;
        private int pendingExperienceFeedback;
        private float experienceFeedbackExpiresAt;
        private int cursorLockAcquiredFrame = -1;
        private IWorldInteractable cachedNearestInteractable;
        private MonoBehaviour cachedNearestBehaviour;
        private Vector3 lastInteractionScanPosition;
        private float cachedNearestDistance = InteractionRange;
        private float nextInteractionScanAt;
        private bool hasInteractionScanCache;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle mutedStyle;
        private GUIStyle hudRowStyle;
        private GUIStyle hudMutedRowStyle;
        private GUIStyle hudAccentRowStyle;
        private GUIStyle hudDangerRowStyle;
        private GUIStyle promptStyle;
        private GUIStyle tinyStyle;
        private Texture2D slateTexture;
        private Texture2D copperTexture;
        private Texture2D tealTexture;
        private Texture2D inkTexture;
        private Texture2D healthTexture;
        private Texture2D damageTexture;
        private Texture2D overlayTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<TetherfallGame>() != null)
            {
                return;
            }

            var root = new GameObject("Tetherfall Vertical Slice");
            root.AddComponent<TetherfallGame>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }

        private void Start()
        {
            ConfigureRendering();
            var playerConfig = TetherfallPlayerConfigStore.LoadAll();
            if (!playerConfig.Succeeded)
            {
                Debug.LogWarning("Tetherfall player preferences could not be fully restored; safe defaults remain active.");
            }

            CreatePlayerAndCamera();
            WorldBuilder.Build(this);
            InitializeExpeditionExpansion();
            InitializeTutorial();
            InitializeCombatSystems();
            InitializePactRosterSystem();
            InitializePlayerSettlementSystem();
            if (TetherfallSaveSystem.TryLoad(out var saveData))
            {
                ApplySavedExpedition(saveData);
                Notify("Expedition record restored.");
            }
            else
            {
                discoveredRegions.Add(CurrentRegion);
                Notify("Wayfinder field kit online. Follow the Threadglass markers.");
            }

            RefreshEnemyCampActivation(false);
            InitializePresentation();
            InitializeObjectiveGuidance();
            nextAutoSaveAt = Time.time + AutoSaveInterval;
            Notify("Press H to hide or show controls.");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log(
                $"Tetherfall runtime ready: {enemyCamps.Count} encounters, " +
                $"{knownEchosworn.Count} Pact creatures, {knownCompanions.Count} companions.");
        }

        private void Update()
        {
            if (Player == null)
            {
                return;
            }

            TetherfallInputBindings.Shared.SampleCurrentFrame();
            UpdatePresentation();
            UpdateTutorial();
            UpdateCombatSystems();
            UpdatePactRosterSystem();
            UpdatePlayerSettlementSystem();
            UpdateOvertoneLattice();
            UpdateCrossweave();
            UpdateExpeditionCombat();
            UpdateEnemyCampActivation();
            var autoSaveDue = Time.time >= nextAutoSaveAt;
            var dirtySaveDue = saveRequested && Time.time >= saveNotBefore;
            var criticalSaveDue =
                criticalSaveRequested && Time.time >= saveNotBefore;
            if (criticalSaveRequested
                    ? criticalSaveDue
                    : autoSaveDue || dirtySaveDue)
            {
                if (!criticalSaveRequested && IsCombatSaveUnsafe())
                {
                    saveNotBefore = Time.time + CombatSaveRetryDelay;
                    if (autoSaveDue)
                    {
                        nextAutoSaveAt = Time.time + CombatSaveRetryDelay;
                    }
                }
                else
                {
                    if (SaveGame())
                    {
                        nextAutoSaveAt = Time.time + AutoSaveInterval;
                    }
                }
            }

            if (HandlePlayerSettlementPlanningInput())
            {
                damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.deltaTime * 2f);
                PruneNotifications();
                return;
            }

            if (CombatHitstopActive)
            {
                damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.deltaTime * 2f);
                PruneNotifications();
                return;
            }

            if (HandleCodexInput())
            {
                damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.deltaTime * 2f);
                PruneNotifications();
                return;
            }

            HandleExpansionInput();
            if (activePanel != GamePanel.None)
            {
                damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.deltaTime * 2f);
                PruneNotifications();
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.deltaTime * 2f);
                PruneNotifications();
                return;
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(
                    TetherfallInputAction.CancelOrReleaseCursor))
            {
                CodexOpen = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.deltaTime * 2f);
                PruneNotifications();
                return;
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.Interact))
            {
                TryInteract();
            }

            if (!CursorLockAcquiredThisFrame &&
                TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.Attack))
            {
                Player.TryAttack();
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.BuildOrRestAtCamp))
            {
                TryPlaceCamp();
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.MountOrDismount))
            {
                TryToggleMount();
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.ToggleControls))
            {
                showControls = !showControls;
            }

            damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.deltaTime * 2f);
            UpdateRegion();
            UpdateObjectiveGuidance();
            PruneNotifications();
        }

        private bool HandleCodexInput()
        {
            if (activePanel != GamePanel.Threadways &&
                TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.OpenFieldLog))
            {
                if (!CodexOpen && activePanel != GamePanel.None)
                {
                    ClosePanel();
                }

                CodexOpen = !CodexOpen;
                Cursor.lockState = CodexOpen ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = CodexOpen;
                return true;
            }

            if (!CodexOpen)
            {
                return false;
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.CancelOrReleaseCursor))
            {
                CodexOpen = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            return true;
        }

        private bool HandlePlayerSettlementPlanningInput()
        {
            var togglePressed =
                TetherfallInputBindings.Shared.WasPressedThisFrame(
                    TetherfallInputAction.OpenSettlementPlanning);
            var cancelPressed =
                TetherfallInputBindings.Shared.WasPressedThisFrame(
                    TetherfallInputAction.CancelOrReleaseCursor);

            if (IsPlayerSettlementPlacementActive &&
                (togglePressed || cancelPressed))
            {
                CancelPlayerSettlementPlacement();
                if (togglePressed && !cancelPressed)
                {
                    RestoreGameplayCursorAfterSettlementToggle();
                }

                return true;
            }

            if (IsPlayerSettlementPanelOpen &&
                (togglePressed || cancelPressed))
            {
                ClosePlayerSettlementPanel();
                if (togglePressed && !cancelPressed)
                {
                    RestoreGameplayCursorAfterSettlementToggle();
                }

                return true;
            }

            if (togglePressed)
            {
                OpenPlayerSettlementPanel();
                return true;
            }

            return IsPlayerSettlementInputCaptured;
        }

        private void RestoreGameplayCursorAfterSettlementToggle()
        {
            if (CodexOpen ||
                activePanel != GamePanel.None ||
                IsPlayerSettlementPanelOpen ||
                IsPlayerSettlementPlacementActive)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cursorLockAcquiredFrame = Time.frameCount;
        }

        public void RegisterInteractable(MonoBehaviour source)
        {
            if (source != null && !interactables.Contains(source))
            {
                interactables.Add(source);
                nextInteractionScanAt = 0f;
                hasInteractionScanCache = false;
            }
        }

        public void RegisterEnemyCamp(EnemyCamp camp)
        {
            if (camp != null && !enemyCamps.Contains(camp))
            {
                enemyCamps.Add(camp);
            }
        }

        public void NotifyCameraTeleport()
        {
            cameraRig?.SnapToTarget();
        }

        public void RefreshEnemyCampActivation(bool forceSoftReset = false)
        {
            if (PlayerTransform == null || isRestoringSave)
            {
                return;
            }

            var playerPosition = PlayerTransform.position;
            for (var index = enemyCamps.Count - 1; index >= 0; index--)
            {
                var camp = enemyCamps[index];
                if (camp == null)
                {
                    enemyCamps.RemoveAt(index);
                    continue;
                }

                camp.EvaluatePopulation(playerPosition, forceSoftReset);
            }
        }

        private void UpdateEnemyCampActivation()
        {
            if (Time.time < nextCampActivationAt)
            {
                return;
            }

            nextCampActivationAt = Time.time + 0.25f;
            RefreshEnemyCampActivation(false);
        }

        public void RegisterPactCreature(PactCreature creature)
        {
            if (creature != null && !knownEchosworn.Contains(creature))
            {
                knownEchosworn.Add(creature);
            }
        }

        public void RegisterCompanion(HumanCompanion companion)
        {
            if (companion != null && !knownCompanions.Contains(companion))
            {
                knownCompanions.Add(companion);
            }
        }

        public void RegisterHearthgate(Hearthgate hearthgate)
        {
            finalHearthgate = hearthgate;
        }

        public void RegisterCraftingStation(CraftingStation station)
        {
            if (station != null && station.StationName == "Alderwake routework bench")
            {
                alderwakeCraftingStation = station;
            }
        }

        public void RegisterDungeonGate(DungeonGate gate)
        {
            skyvaultGate = gate;
        }

        public Transform GetAlderwakeCraftingStationTarget()
        {
            return alderwakeCraftingStation != null ? alderwakeCraftingStation.transform : null;
        }

        public Transform GetSkyvaultGateTarget()
        {
            return skyvaultGate != null ? skyvaultGate.transform : null;
        }

        public Material CreateMaterial(Color color, float metallic = 0f, float smoothness = 0.25f)
        {
            var runtimePrototype = Resources.Load<Material>("TetherfallRuntimeMaterial");
            Material material;
            if (runtimePrototype != null)
            {
                material = new Material(runtimePrototype);
            }
            else
            {
                var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
                if (shader == null)
                {
                    Debug.LogError("Tetherfall could not locate a runtime shader.");
                    return null;
                }

                material = new Material(shader);
            }

            material.color = color;
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            return material;
        }

        public void Notify(string message, float duration = 4.2f)
        {
            if (notifications.Count >= 3)
            {
                notifications.RemoveAt(0);
            }

            notifications.Add(new NotificationEntry
            {
                Message = message,
                ExpiresAt = Time.time + duration
            });
        }

        public int GetResource(ResourceKind kind)
        {
            return resources.TryGetValue(kind, out var value) ? value : 0;
        }

        public void AddResource(ResourceKind kind, int amount)
        {
            AddResource(kind, amount, ResourceAcquisitionSource.AuthoredReward);
        }

        public void AddResource(
            ResourceKind kind,
            int baseAmount,
            ResourceAcquisitionSource source)
        {
            if (baseAmount <= 0)
            {
                return;
            }

            var awardedAmount = source == ResourceAcquisitionSource.WorldGather
                ? ApplySkillResourceYield(baseAmount)
                : baseAmount;
            if (awardedAmount <= 0)
            {
                return;
            }

            resources.TryGetValue(kind, out var currentAmount);
            resources[kind] = (int)Math.Min(
                int.MaxValue,
                (long)Mathf.Max(0, currentAmount) + awardedAmount);
            var experience = CalculateResourceExperience(baseAmount, source);
            if (experience > 0)
            {
                GainExperience(experience);
            }

            Notify($"+{awardedAmount} {ResourceName(kind)}");
            RequestSave();
        }

        internal static int CalculateResourceExperience(
            int baseYield,
            ResourceAcquisitionSource source)
        {
            if (source != ResourceAcquisitionSource.WorldGather ||
                baseYield <= 0)
            {
                return 0;
            }

            var boundedBaseYield = Mathf.Min(
                baseYield,
                MaximumWorldGatherBaseYieldForExperience);
            return Mathf.Min(
                MaximumWorldGatherExperience,
                5 + boundedBaseYield * 5);
        }

        public void RecordResourceNodeGathered()
        {
            gatheredNodes++;
            RequestSave();
        }

        public bool SpendResources(int timber, int stone, int resin = 0, int chimesalt = 0)
        {
            if (GetResource(ResourceKind.Timber) < timber ||
                GetResource(ResourceKind.Stone) < stone ||
                GetResource(ResourceKind.Resin) < resin ||
                GetResource(ResourceKind.Chimesalt) < chimesalt)
            {
                return false;
            }

            resources[ResourceKind.Timber] -= timber;
            resources[ResourceKind.Stone] -= stone;
            resources[ResourceKind.Resin] -= resin;
            resources[ResourceKind.Chimesalt] -= chimesalt;
            RequestSave();
            return true;
        }

        public void GainExperience(int amount)
        {
            QueueExperienceFeedback(amount);
            Experience += amount;
            var carriedExperience = Experience;
            var nextRequirement = ExperienceToNext;
            while (TetherfallWayfinderProgression.TryCarryExperienceToNextRank(
                       ref carriedExperience,
                       ref nextRequirement))
            {
                Level++;
                MaxHealth += TetherfallWayfinderProgression.MaxVitalityPerRank;
                Health = MaxHealth;
                var milestone = TetherfallWayfinderProgression.DescribeRankMilestone(Level);
                var milestoneLine = string.IsNullOrEmpty(milestone) ? string.Empty : $"\n{milestone}";
                Notify(
                    $"Wayfinder rank {Level}\n" +
                    $"+{TetherfallWayfinderProgression.MaxVitalityPerRank} max vitality. " +
                    "Vitality fully restored. " +
                    $"+{TetherfallWayfinderProgression.DisciplinePointsPerRank} discipline point." +
                    milestoneLine,
                    string.IsNullOrEmpty(milestone) ? 5.8f : 7f);
            }

            Experience = carriedExperience;
            ExperienceToNext = nextRequirement;
            RequestSave();
        }

        private void QueueExperienceFeedback(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (Time.time >= experienceFeedbackExpiresAt)
            {
                pendingExperienceFeedback = 0;
            }

            pendingExperienceFeedback = pendingExperienceFeedback > int.MaxValue - amount
                ? int.MaxValue
                : pendingExperienceFeedback + amount;
            experienceFeedbackExpiresAt = Time.time + ExperienceFeedbackDuration;
        }

        public void DamagePlayer(int amount, string source, Enemy sourceEnemy = null)
        {
            if (Player == null || amount <= 0)
            {
                return;
            }

            amount = Player.ModifyIncomingDamage(amount);
            if (amount <= 0)
            {
                if (Player.IsDodging)
                {
                    Notify("The strike misses your dodge.");
                }
                else
                {
                    sourceEnemy?.TryBreakAshguardFromPerfectGuard();
                }

                return;
            }

            Health = Mathf.Max(0, Health - amount);
            damageFlash = 0.5f;
            PlayPlayerHitFeedback(amount);
            if (Health == 0)
            {
                RespawnPlayer(source);
            }

            RequestSave();
        }

        public void HealPlayer(int amount)
        {
            var prior = Health;
            Health = Mathf.Min(MaxHealth, Health + amount);
            if (Health > prior)
            {
                Notify($"+{Health - prior} vitality");
            }

            RequestSave();
        }

        public void OnEnemyDefeated(Enemy enemy)
        {
            GainExperience(enemy.ExperienceReward);
            var lootKind = enemy.EnemyId.Contains("cinder") ||
                           enemy.EnemyId.Contains("breaker") ||
                           enemy.EnemyId.Contains("ashjaw")
                ? ResourceKind.Ore
                : enemy.EnemyId.Contains("fen") || enemy.EnemyId.Contains("mossglass")
                    ? ResourceKind.Herbs
                    : enemy.EnemyId.Contains("vault")
                        ? ResourceKind.Chimesalt
                        : ResourceKind.Stone;
            var lootAmount = ApplySkillResourceYield(1);
            resources[lootKind] += lootAmount;
            Notify(
                enemy.OffersThreadbindOnDefeat
                    ? $"{enemy.EnemyName} is recovering. A 24-second Threadbind rescue is open. " +
                      $"+{lootAmount} {ResourceName(lootKind)}"
                    : $"{enemy.EnemyName} dispersed. +{lootAmount} {ResourceName(lootKind)}");
            RequestSave();
        }

        public void OnCampCleared(EnemyCamp camp)
        {
            if (camp != null && camp.IsWildAccordEncounter)
            {
                Notify(
                    $"{camp.CampName} is calm. Any recovering Echosworn can now be rescued.");
                RequestSave();
                return;
            }

            clearedCamps++;
            GainExperience(45);
            RecordPactFirstClear(camp);
            OnHostileCampCleared();
            OnFenlightPatrolCleared(camp);
            OnCinderwoldWorldStateChanged();
            Notify($"{camp.CampName} cleared. Scouts report it will repopulate after a while.");
            RequestSave();
        }

        public void FormPact(PactCreature creature)
        {
            if (creature == null || creature.IsPacted)
            {
                return;
            }

            creature.BeginPact(this);
            pacts.Add(creature);
            OnPactJoinedForUnfolding(creature);
            if (creature.IsMount)
            {
                MountEchosworn = creature;
                Notify($"{creature.CreatureName} accepted a Pact. Press R to ride.");
            }
            else
            {
                var joinedField = EnsureStoryPactInRoster(creature, true);
                Notify(
                    joinedField
                        ? $"{creature.CreatureName} joined the Pact field roster."
                        : $"{creature.CreatureName} is recorded in the Pact roster reserve.");
            }

            RefreshActivePactVisuals();
            GainExperience(60);
            if (!creature.IsMount)
            {
                OnPactFormed();
            }
            RecordTutorialEvent(TutorialEvent.FormPact);
            RequestSave();
        }

        public void Recruit(HumanCompanion companion)
        {
            if (companion == null || companion.IsRecruited)
            {
                return;
            }

            companion.JoinParty(this);
            party.Add(companion);
            RefreshPartyLineup();

            GainExperience(55);
            Notify($"{companion.CompanionName} joined the field party.");
            RequestSave();
        }

        public void TryToggleMount()
        {
            if (Player != null && Player.TryUsePactThermalLift())
            {
                return;
            }

            if (MountEchosworn == null || !MountEchosworn.IsPacted)
            {
                Notify(
                    HasBoundedPactThermalLift
                        ? "Begin gliding before asking Cinderwing for a thermal lift."
                        : "Find a ride-ready Echosworn and earn its Pact first.");
                return;
            }

            Player.SetMounted(!Player.IsMounted, MountEchosworn);
            RequestSave();
        }

        public void TryPlaceCamp()
        {
            if (ActiveCamp != null)
            {
                if (Vector3.Distance(PlayerTransform.position, ActiveCamp.transform.position) < 4f)
                {
                    ActiveCamp.Rest(this);
                }
                else
                {
                    Notify("Return to your portable camp or dismantle it before moving it.");
                }

                return;
            }

            if (GetResource(ResourceKind.Timber) < CampTimberCost ||
                GetResource(ResourceKind.Stone) < CampStoneCost)
            {
                Notify($"Camp requires {CampTimberCost} Timber and {CampStoneCost} Stone.");
                return;
            }

            var proposedPosition = PlayerTransform.position + PlayerTransform.forward * 3.2f + Vector3.up * 4f;
            if (!Physics.Raycast(
                    proposedPosition,
                    Vector3.down,
                    out var groundHit,
                    10f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                Notify("The field kit cannot find stable ground for a camp.");
                return;
            }

            var campPosition = groundHit.point;
            if (!CanPlaceCampAt(campPosition))
            {
                Notify("Clear more space before establishing the portable camp.");
                return;
            }

            SpendResources(CampTimberCost, CampStoneCost);
            ActiveCamp = CampSite.Create(this, campPosition);
            RecordPactCampLevel(1);
            Notify("Portable camp established. Press E nearby to rest.");
            GainExperience(35);
            RequestSave();
        }

        private bool CanPlaceCampAt(Vector3 position)
        {
            var overlapCount = Physics.OverlapBoxNonAlloc(
                position + Vector3.up * 0.78f,
                new Vector3(2.15f, 0.72f, 1.85f),
                campPlacementOverlapBuffer,
                Quaternion.identity,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            var boundedCount = Mathf.Min(
                overlapCount,
                campPlacementOverlapBuffer.Length);
            var canPlace = overlapCount < campPlacementOverlapBuffer.Length;
            for (var index = 0; index < boundedCount; index++)
            {
                var overlap = campPlacementOverlapBuffer[index];
                if (overlap == null ||
                    overlap.GetComponentInParent<PlayerAvatar>() != null ||
                    overlap.bounds.max.y <= position.y + 0.16f)
                {
                    continue;
                }

                canPlace = false;
            }

            for (var index = 0; index < boundedCount; index++)
            {
                campPlacementOverlapBuffer[index] = null;
            }

            return canPlace;
        }

        public void RemoveCamp(CampSite camp)
        {
            if (camp == ActiveCamp)
            {
                ActiveCamp = null;
                RequestSave();
            }
        }

        public void RestoreHearthgate()
        {
            if (hearthgateRestored)
            {
                Notify("The Hearthgate already hums with a stable route.");
                return;
            }

            if (pacts.Count == 0 || party.Count == 0 || clearedCamps == 0 || ActiveCamp == null)
            {
                Notify("The Hearthgate needs a Pact, a companion, a cleared camp, and a field refuge.");
                return;
            }

            hearthgateRestored = true;
            finalHearthgate.SetRestored(true);
            GainExperience(150);
            Notify("Hearthgate restored. Alderwake is connected again.");
            RequestSave();
        }

        public string GetObjective()
        {
            var bramblet = FindPactCreature("Bramblet");
            if (bramblet == null || !bramblet.IsPacted)
            {
                return "Find Bramblet in Alderwake and form a voluntary Pact.";
            }

            if (party.Count == 0)
            {
                return "Recruit Sera Venn at the river overlook.";
            }

            var bountyObjective = GetActiveBountyObjectiveText();
            if (!string.IsNullOrEmpty(bountyObjective))
            {
                return bountyObjective;
            }

            var cityActivityObjective = GetActiveCityActivityObjectiveText();
            if (!string.IsNullOrEmpty(cityActivityObjective))
            {
                return cityActivityObjective;
            }

            if (gatheredNodes < 8)
            {
                return $"Gather field materials ({gatheredNodes}/8).";
            }

            if (!HasClearedMilestoneEncounter(HollowReedMilestoneId))
            {
                return "Clear the Hollow Reed enemy camp.";
            }

            if (ActiveCamp == null)
            {
                return "Build a portable camp with 3 Timber and 2 Stone.";
            }

            if (!hearthgateRestored)
            {
                return "Return to the Hearthgate at Alderwake and restore the route.";
            }

            if (settlementTier < 3)
            {
                return $"Restore Alderwake's route station ({settlementTier}/3).";
            }

            if (!IsWeaponUnlocked(WeaponDiscipline.Riftbow) && !IsWeaponUnlocked(WeaponDiscipline.Threadstaff))
            {
                return "Craft a second weapon discipline at the routework bench.";
            }

            if (activatedDungeonPylons < 3)
            {
                return $"Enter the Morrowfen Skyvault and stabilize its pylons ({activatedDungeonPylons}/3).";
            }

            if (!dungeonBossDefeated)
            {
                return "Defeat the Skyvault Warden and reopen the buried route.";
            }

            var fenlightObjective = GetFenlightObjectiveText();
            if (!string.IsNullOrEmpty(fenlightObjective))
            {
                return fenlightObjective;
            }

            var modularDungeonObjective = GetModularDungeonObjectiveText();
            if (!string.IsNullOrEmpty(modularDungeonObjective))
            {
                return modularDungeonObjective;
            }

            var cinderwoldObjective = GetCinderwoldArcObjectiveText();
            if (!string.IsNullOrEmpty(cinderwoldObjective))
            {
                return cinderwoldObjective;
            }

            if (!IsQuestComplete("signalbreak"))
            {
                var signalbreak = questLog.Find(record => record.id == "signalbreak");
                return $"Stabilize broken route signals ({signalbreak?.current ?? 0}/{signalbreak?.required ?? 3}).";
            }

            if (RecoveredFieldCaches < FieldCacheCatalog.All.Count)
            {
                return $"Complete regional Wayfinder surveys ({RecoveredFieldCaches}/{FieldCacheCatalog.All.Count}) for traversal boons.";
            }

            var routePatrolObjective = GetRoutePatrolObjectiveText();
            if (!string.IsNullOrEmpty(routePatrolObjective))
            {
                return routePatrolObjective;
            }

            return "Complete field quests, improve camp services, and rotate your expedition team.";
        }

        public string GetInteractionPrompt()
        {
            var interactable = FindNearestInteractable(out _);
            if (interactable == null)
            {
                return string.Empty;
            }

            var prompt = interactable.InteractionPrompt;
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                return prompt;
            }

            return interactable.CanInteract(this) ? "Interact" : "Not ready yet";
        }

        public void TryInteract()
        {
            var interactable = FindNearestInteractable(out _, true);
            if (interactable == null)
            {
                Notify("Nothing nearby responds to the Threadglass.");
                return;
            }

            if (!interactable.CanInteract(this))
            {
                var unavailablePrompt = interactable.InteractionPrompt;
                Notify(string.IsNullOrWhiteSpace(unavailablePrompt)
                    ? "That action is not ready yet."
                    : unavailablePrompt);
                return;
            }

            interactable.Interact(this);
            RecordTutorialEvent(TutorialEvent.Interact);
        }

        private IWorldInteractable FindNearestInteractable(
            out float nearestDistance,
            bool forceRefresh = false)
        {
            var playerPosition = PlayerTransform.position;
            if (!forceRefresh &&
                hasInteractionScanCache &&
                Time.unscaledTime < nextInteractionScanAt &&
                (cachedNearestBehaviour == null || cachedNearestBehaviour.isActiveAndEnabled) &&
                (cachedNearestBehaviour != null || cachedNearestInteractable == null) &&
                (playerPosition - lastInteractionScanPosition).sqrMagnitude <= 0.12f)
            {
                nearestDistance = cachedNearestDistance;
                return cachedNearestInteractable;
            }

            IWorldInteractable nearest = null;
            MonoBehaviour nearestBehaviour = null;
            var nearestAvailable = false;
            nearestDistance = InteractionRange;
            for (var index = interactables.Count - 1; index >= 0; index--)
            {
                var behaviour = interactables[index];
                if (behaviour == null)
                {
                    interactables.RemoveAt(index);
                    continue;
                }

                if (!behaviour.isActiveAndEnabled)
                {
                    continue;
                }

                var interactable = behaviour as IWorldInteractable;
                if (interactable == null)
                {
                    continue;
                }

                var available = interactable.CanInteract(this);
                if (!available &&
                    (interactable is PactCreature pact && pact.IsPacted ||
                     interactable is HumanCompanion companion && companion.IsRecruited))
                {
                    continue;
                }

                var offset = behaviour.transform.position - playerPosition;
                var verticalDistance = Mathf.Abs(offset.y);
                if (verticalDistance > InteractionVerticalLimit)
                {
                    continue;
                }

                offset.y = 0f;
                var distance = offset.magnitude + verticalDistance * 0.35f;
                var score = distance + (available ? 0f : 0.2f);
                if (score < nearestDistance ||
                    (Mathf.Abs(score - nearestDistance) < 0.001f && available && !nearestAvailable))
                {
                    nearestDistance = score;
                    nearest = interactable;
                    nearestBehaviour = behaviour;
                    nearestAvailable = available;
                }
            }

            cachedNearestInteractable = nearest;
            cachedNearestBehaviour = nearestBehaviour;
            cachedNearestDistance = nearestDistance;
            lastInteractionScanPosition = playerPosition;
            nextInteractionScanAt = Time.unscaledTime + InteractionScanInterval;
            hasInteractionScanCache = true;
            return nearest;
        }

        private bool HasClearedMilestoneEncounter(string persistenceId)
        {
            foreach (var camp in enemyCamps)
            {
                if (camp != null &&
                    camp.PersistenceId == persistenceId &&
                    camp.AwardedFirstClear)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateRegion()
        {
            var region = GetRegionForPosition(PlayerTransform.position);
            if (region != CurrentRegion)
            {
                CurrentRegion = region;
                var firstDiscovery = discoveredRegions.Add(region);
                PresentRegion(region, false);
                Notify($"Entered {region}.");
                if (firstDiscovery)
                {
                    GainExperience(20);
                    RecordPactRegionDiscovery(region);
                }

                RequestSave();
            }
        }

        private static string GetRegionForPosition(Vector3 position)
        {
            return TetherfallRegionPacks.ResolveRegionName(position);
        }

        private void RespawnPlayer(string source)
        {
            var recoveredInDungeon = TryGetModularDungeonDeathRecovery(out var destination);
            if (!recoveredInDungeon)
            {
                destination = ActiveCamp != null
                    ? ActiveCamp.transform.position + Vector3.forward * 2f
                    : new Vector3(0f, 2f, -18f);
            }

            Player.Teleport(destination);
            Health = Mathf.Max(30, MaxHealth / 2);
            RestoreAllStamina();
            Notify(
                recoveredInDungeon
                    ? $"{source} overwhelmed you. Keeper's Threadglass restores the last safe sluice room."
                    : $"{source} overwhelmed you. You recover at a safe point.");
            RequestCriticalSave();
        }

        private void RequestSave()
        {
            if (!isRestoringSave)
            {
                if (!saveRequested)
                {
                    saveNotBefore = Time.time + SaveCoalesceDelay;
                }

                saveRequested = true;
            }
        }

        internal void RequestCriticalSave()
        {
            if (isRestoringSave)
            {
                return;
            }

            saveRequested = true;
            criticalSaveRequested = true;
            saveNotBefore = Time.time;
        }

        private bool IsCombatSaveUnsafe()
        {
            if (Player == null)
            {
                return false;
            }

            if (CombatTarget != null || Enemy.FindClosest(PlayerTransform.position, 30f) != null)
            {
                return true;
            }

            return ActiveDungeonBoss != null &&
                   !ActiveDungeonBoss.IsDefeated &&
                   (ActiveDungeonBoss.transform.position - PlayerTransform.position).sqrMagnitude < 45f * 45f;
        }

        internal bool CursorLockAcquiredThisFrame =>
            cursorLockAcquiredFrame == Time.frameCount;
        internal bool IsModalUiOpen =>
            CodexOpen ||
            activePanel != GamePanel.None ||
            IsPlayerSettlementInputCaptured;

        internal void AcquireGameplayCursor()
        {
            if (Cursor.lockState == CursorLockMode.Locked || IsModalUiOpen)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cursorLockAcquiredFrame = Time.frameCount;
        }

        private bool SaveGame()
        {
            if (Player == null || isRestoringSave)
            {
                return false;
            }

            var data = new TetherfallSaveData
            {
                level = Level,
                experience = Experience,
                experienceToNext = ExperienceToNext,
                health = Health,
                maxHealth = MaxHealth,
                gatheredNodes = gatheredNodes,
                clearedCamps = clearedCamps,
                hearthgateRestored = hearthgateRestored,
                hasCamp = ActiveCamp != null,
                wasMounted = Player.IsMounted,
                playerPosition = PlayerTransform.position,
                campPosition = ActiveCamp != null ? ActiveCamp.transform.position : Vector3.zero,
                activePactName = ActiveEchosworn != null ? ActiveEchosworn.CreatureName : string.Empty,
                mountPactName = MountEchosworn != null ? MountEchosworn.CreatureName : string.Empty
            };
            WriteExpansionSave(data);
            WriteTutorialSave(data);

            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                data.resources.Add(new ResourceAmount
                {
                    kind = kind,
                    amount = GetResource(kind)
                });
            }

            foreach (var pact in pacts)
            {
                if (pact != null && pact.IsPacted)
                {
                    data.pactNames.Add(pact.CreatureName);
                }
            }
            WritePactUnfoldingSave(data);
            WritePactRosterSave(data);
            WritePlayerSettlementSave(data);

            foreach (var companion in party)
            {
                if (companion != null && companion.IsRecruited)
                {
                    data.recruitNames.Add(companion.CompanionName);
                }
            }

            foreach (var region in discoveredRegions)
            {
                data.discoveredRegions.Add(region);
            }

            if (TetherfallSaveSystem.Save(data))
            {
                saveRequested = false;
                criticalSaveRequested = false;
                return true;
            }

            RetainSaveRequestAfterFailure();
            Notify("Expedition record could not save. Retrying shortly.");
            return false;
        }

        private void RetainSaveRequestAfterFailure()
        {
            saveRequested = true;
            saveNotBefore = Time.time + 3f;
            nextAutoSaveAt = saveNotBefore;
        }

        private void ApplySavedExpedition(TetherfallSaveData data)
        {
            isRestoringSave = true;
            Level = Mathf.Max(1, data.level);
            Experience = Mathf.Max(0, data.experience);
            ExperienceToNext = Mathf.Max(1, data.experienceToNext);
            MaxHealth = Mathf.Max(1, data.maxHealth);
            Health = Mathf.Clamp(data.health, 1, MaxHealth);
            gatheredNodes = Mathf.Max(0, data.gatheredNodes);
            clearedCamps = Mathf.Max(0, data.clearedCamps);
            hearthgateRestored = data.hearthgateRestored;

            foreach (var resource in data.resources)
            {
                resources[resource.kind] = Mathf.Max(0, resource.amount);
            }

            foreach (var pactName in data.pactNames)
            {
                var pact = FindPactCreature(pactName);
                if (pact != null)
                {
                    RestorePact(pact);
                }
            }

            foreach (var recruitName in data.recruitNames)
            {
                var companion = FindCompanion(recruitName);
                if (companion != null)
                {
                    RestoreRecruit(companion);
                }
            }

            var activePact = FindPactCreature(data.activePactName);
            if (activePact != null && activePact.IsPacted && !activePact.IsMount)
            {
                ActiveEchosworn = activePact;
            }

            var mountPact = FindPactCreature(data.mountPactName);
            if (mountPact != null && mountPact.IsPacted && mountPact.IsMount)
            {
                MountEchosworn = mountPact;
            }

            if (hearthgateRestored && finalHearthgate != null)
            {
                finalHearthgate.SetRestored(true);
            }

            if (data.hasCamp)
            {
                ActiveCamp = CampSite.Create(this, data.campPosition);
            }

            Player.Teleport(data.playerPosition);
            CurrentRegion = GetRegionForPosition(data.playerPosition);
            discoveredRegions.Clear();
            if (data.discoveredRegions.Count == 0)
            {
                discoveredRegions.Add(CurrentRegion);
            }
            else
            {
                foreach (var region in data.discoveredRegions)
                {
                    discoveredRegions.Add(region);
                }
            }

            if (data.wasMounted && MountEchosworn != null)
            {
                Player.SetMounted(true, MountEchosworn);
            }

            RestoreExpansionState(data);
            RestorePactUnfoldingState(data);
            RestorePactRosterState(data);
            RestorePlayerSettlementState(data);
            RestoreTutorialState(data);
            RestoreAllStamina();
            isRestoringSave = false;
        }

        private PactCreature FindPactCreature(string creatureName)
        {
            if (string.IsNullOrEmpty(creatureName))
            {
                return null;
            }

            foreach (var pact in knownEchosworn)
            {
                if (pact != null && pact.CreatureName == creatureName)
                {
                    return pact;
                }
            }

            return null;
        }

        private HumanCompanion FindCompanion(string companionName)
        {
            if (string.IsNullOrEmpty(companionName))
            {
                return null;
            }

            foreach (var companion in knownCompanions)
            {
                if (companion != null && companion.CompanionName == companionName)
                {
                    return companion;
                }
            }

            return null;
        }

        private void RestorePact(PactCreature creature)
        {
            if (!creature.IsPacted)
            {
                creature.BeginPact(this);
            }

            if (!pacts.Contains(creature))
            {
                pacts.Add(creature);
            }

            if (creature.IsMount)
            {
                MountEchosworn = creature;
            }
            else
            {
                EnsureStoryPactInRoster(creature, true);
            }
        }

        private void RestoreRecruit(HumanCompanion companion)
        {
            if (!companion.IsRecruited)
            {
                companion.JoinParty(this);
            }

            if (!party.Contains(companion))
            {
                party.Add(companion);
            }

            RefreshPartyLineup();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveGame();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveGame();
            }
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void ConfigureRendering()
        {
            QualitySettings.antiAliasing = Mathf.Max(QualitySettings.antiAliasing, 4);
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 4);
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 145f);
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowCascades = 4;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.56f, 0.63f);
            RenderSettings.ambientEquatorColor = new Color(0.31f, 0.4f, 0.35f);
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.15f, 0.12f);
            RenderSettings.ambientLight = new Color(0.38f, 0.47f, 0.4f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.25f, 0.37f, 0.32f);
            RenderSettings.fogDensity = 0.0035f;

            var skyboxPrototype = Resources.Load<Material>("TetherfallRuntimeSkybox");
            if (skyboxPrototype != null)
            {
                runtimeSkybox = new Material(skyboxPrototype);
                RenderSettings.skybox = runtimeSkybox;
            }

            sunLight = new GameObject("Veyra Sun").AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1f, 0.77f, 0.55f);
            sunLight.intensity = 1.14f;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.72f;
            sunLight.shadowBias = 0.045f;
            sunLight.shadowNormalBias = 0.36f;
            sunLight.transform.rotation = Quaternion.Euler(42f, -28f, 0f);

            fillLight = new GameObject("Threadglass Fill").AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.3f, 0.62f, 0.58f);
            fillLight.intensity = 0.24f;
            fillLight.shadows = LightShadows.None;
            fillLight.transform.rotation = Quaternion.Euler(24f, 134f, 0f);
        }

        private void CreatePlayerAndCamera()
        {
            var playerObject = new GameObject("Wayfinder");
            playerObject.transform.position = new Vector3(0f, 2f, -18f);
            Player = playerObject.AddComponent<PlayerAvatar>();
            Player.Initialize(this);

            var cameraObject = new GameObject("Wayfinder Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.15f, 0.25f, 0.32f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 420f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.useOcclusionCulling = true;
            cameraRig = cameraObject.AddComponent<ThirdPersonCamera>();
            cameraRig.Initialize(Player.transform);
        }

        private void PruneNotifications()
        {
            for (var index = notifications.Count - 1; index >= 0; index--)
            {
                if (notifications[index].ExpiresAt < Time.time)
                {
                    notifications.RemoveAt(index);
                }
            }

            if (pendingExperienceFeedback > 0 && Time.time >= experienceFeedbackExpiresAt)
            {
                pendingExperienceFeedback = 0;
            }
        }

        private void EnsureGuiStyles()
        {
            if (headingStyle != null)
            {
                return;
            }

            var font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 16);
            headingStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(0.94f, 0.88f, 0.73f) }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 14,
                wordWrap = true,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(0.84f, 0.91f, 0.9f) }
            };
            mutedStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.59f, 0.71f, 0.72f) }
            };
            hudRowStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 13,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            hudMutedRowStyle = new GUIStyle(mutedStyle)
            {
                fontSize = 12,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            hudAccentRowStyle = new GUIStyle(hudMutedRowStyle)
            {
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.29f, 0.79f, 0.74f) }
            };
            hudDangerRowStyle = new GUIStyle(hudMutedRowStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.78f, 0.27f, 0.22f) }
            };
            promptStyle = new GUIStyle(bodyStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.93f, 0.79f) }
            };
            tinyStyle = new GUIStyle(mutedStyle)
            {
                fontSize = 11
            };

            slateTexture = SolidTexture(new Color(0.05f, 0.11f, 0.14f, 0.88f));
            copperTexture = SolidTexture(new Color(0.77f, 0.47f, 0.22f, 0.96f));
            tealTexture = SolidTexture(new Color(0.29f, 0.79f, 0.74f, 0.96f));
            inkTexture = SolidTexture(new Color(0.01f, 0.03f, 0.04f, 0.78f));
            healthTexture = SolidTexture(new Color(0.78f, 0.27f, 0.22f, 0.96f));
            damageTexture = SolidTexture(new Color(0.5f, 0.04f, 0.03f, 0.09f));
            overlayTexture = SolidTexture(new Color(0f, 0.02f, 0.03f, 0.78f));
        }

        private static Texture2D SolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            EnsureGuiStyles();
            var previousMatrix = GUI.matrix;
            var hudScale = TetherfallAccessibilityRuntime.HudScale;
            var hudMatrix =
                previousMatrix *
                Matrix4x4.Scale(new Vector3(hudScale, hudScale, 1f));
            GUI.matrix = hudMatrix;
            var width = Screen.width / hudScale;
            var height = Screen.height / hudScale;
            var settlementUiActive =
                IsPlayerSettlementPanelOpen ||
                IsPlayerSettlementPlacementActive;
            var edge = Mathf.Clamp(width * 0.018f, 12f, 20f);
            const float gap = 10f;
            var leftWidth = Mathf.Clamp(width * 0.28f, 280f, 440f);
            var rightWidth = Mathf.Clamp(width * 0.24f, 250f, 360f);
            var availableSideWidth = Mathf.Max(1f, width - edge * 2f - gap);
            if (leftWidth + rightWidth > availableSideWidth)
            {
                var fitScale = availableSideWidth / (leftWidth + rightWidth);
                leftWidth *= fitScale;
                rightWidth *= fitScale;
            }

            var partyHeight = Mathf.Clamp(
                196f +
                Mathf.Min(
                    PactActorBudget,
                    ActiveCombatPactCount) *
                18f +
                (IsThreadlinkActive ? 44f : 0f),
                210f,
                340f);
            var hudRect = new Rect(edge, 18f, leftWidth, 260f);
            var questRect = new Rect(edge, hudRect.yMax + gap, leftWidth, 144f);
            var partyRect = new Rect(width - edge - rightWidth, 18f, rightWidth, partyHeight);
            var resourceRect = new Rect(
                partyRect.x,
                partyRect.yMax + gap,
                rightWidth,
                204f);

            DrawWorldFrame(width, height);
            DrawHudPanel(hudRect);
            DrawQuestPanel(questRect);
            DrawPartyPanel(partyRect);
            DrawResourcePanel(resourceRect);

            var prompt = GetInteractionPrompt();
            if (!string.IsNullOrEmpty(prompt) &&
                !CodexOpen &&
                !settlementUiActive)
            {
                var promptWidth = Mathf.Min(500f, width - 80f);
                var promptRect = new Rect(width * 0.5f - promptWidth * 0.5f, height - 116f, promptWidth, 42f);
                GUI.DrawTexture(new Rect(promptRect.x - 3f, promptRect.y - 3f, promptRect.width + 6f, promptRect.height + 6f), inkTexture);
                GUI.Label(
                    promptRect,
                    $"{TetherfallInputBindings.Shared.GetBindingSummary(TetherfallInputAction.Interact)}  {prompt}",
                    promptStyle);
            }

            DrawNotifications(width, height);
            DrawRegionDiscovery(width, height);
            DrawTutorial(width, height);
            if (showControls &&
                !IsTutorialPanelActive &&
                !CodexOpen &&
                !settlementUiActive)
            {
                GUI.Label(new Rect(20f, height - 58f, width - 40f, 46f),
                    BuildControlsReference(),
                    tinyStyle);
            }

            if (damageFlash > 0f)
            {
                var flashColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, damageFlash);
                GUI.DrawTexture(new Rect(0f, 0f, width, height), damageTexture);
                GUI.color = flashColor;
            }

            if (settlementUiActive)
            {
                DrawPlayerSettlementUi(
                    width,
                    height,
                    hudScale,
                    previousMatrix);
            }
            else
            {
                DrawExpansionAndCodexUi(
                    width,
                    height,
                    hudScale,
                    previousMatrix,
                    hudMatrix);
            }
            GUI.matrix = previousMatrix;
        }

        private void DrawPlayerSettlementUi(
            float width,
            float height,
            float hudScale,
            Matrix4x4 previousMatrix)
        {
            try
            {
                ApplyLowResolutionModalScale(
                    ref width,
                    ref height,
                    hudScale,
                    previousMatrix);

                if (IsPlayerSettlementPanelOpen)
                {
                    GUI.DrawTexture(
                        new Rect(0f, 0f, width, height),
                        overlayTexture);
                    var panelWidth = Mathf.Min(960f, Mathf.Max(1f, width - 80f));
                    var panelHeight = Mathf.Min(620f, Mathf.Max(1f, height - 70f));
                    DrawPlayerSettlementPanel(
                        new Rect(
                            width * 0.5f - panelWidth * 0.5f,
                            height * 0.5f - panelHeight * 0.5f,
                            panelWidth,
                            panelHeight));
                    return;
                }

                var statusWidth = Mathf.Min(720f, Mathf.Max(1f, width - 80f));
                DrawPlayerSettlementPanel(
                    new Rect(
                        width * 0.5f - statusWidth * 0.5f,
                        Mathf.Clamp(height * 0.07f, 18f, 44f),
                        statusWidth,
                        116f));
            }
            finally
            {
                GUI.matrix = previousMatrix;
            }
        }

        private void DrawExpansionAndCodexUi(
            float width,
            float height,
            float hudScale,
            Matrix4x4 previousMatrix,
            Matrix4x4 hudMatrix)
        {
            try
            {
                ApplyLowResolutionModalScale(
                    ref width,
                    ref height,
                    hudScale,
                    previousMatrix);
                if (CodexOpen)
                {
                    DrawCodex(width, height);
                }

                DrawExpansionUi(width, height);
            }
            finally
            {
                GUI.matrix = hudMatrix;
            }
        }

        private static void ApplyLowResolutionModalScale(
            ref float width,
            ref float height,
            float hudScale,
            Matrix4x4 previousMatrix)
        {
            var effectiveScale = GetEffectiveModalScale(
                Screen.width,
                Screen.height,
                hudScale);
            if (Mathf.Abs(effectiveScale - hudScale) < 0.001f)
            {
                return;
            }

            GUI.matrix =
                previousMatrix *
                Matrix4x4.Scale(
                    new Vector3(
                        effectiveScale,
                        effectiveScale,
                        1f));
            width = Screen.width / effectiveScale;
            height = Screen.height / effectiveScale;
        }

        public static float GetEffectiveModalScale(
            float screenWidth,
            float screenHeight,
            float hudScale)
        {
            var safeHudScale =
                float.IsNaN(hudScale) || float.IsInfinity(hudScale)
                    ? 1f
                    : Mathf.Clamp(hudScale, 0.01f, 4f);
            var safeWidth =
                float.IsNaN(screenWidth) || float.IsInfinity(screenWidth)
                    ? MinimumModalCanvasWidth
                    : Mathf.Max(1f, screenWidth);
            var safeHeight =
                float.IsNaN(screenHeight) || float.IsInfinity(screenHeight)
                    ? MinimumModalCanvasHeight
                    : Mathf.Max(1f, screenHeight);
            var logicalWidth = safeWidth / safeHudScale;
            var logicalHeight = safeHeight / safeHudScale;
            var fitScale = Mathf.Clamp(
                Mathf.Min(
                    1f,
                    logicalWidth / MinimumModalCanvasWidth,
                    logicalHeight / MinimumModalCanvasHeight),
                0.5f,
                1f);
            return Mathf.Max(0.01f, safeHudScale * fitScale);
        }

        private void DrawHudPanel(Rect rect)
        {
            DrawPanel(rect, "WAYFINDER");
            GUI.Label(new Rect(rect.x + 14f, rect.y + 34f, rect.width - 28f, 22f), $"Rank {Level}    {CurrentRegion}", headingStyle);
            DrawBar(new Rect(rect.x + 14f, rect.y + 62f, rect.width - 28f, 10f), Health, MaxHealth);
            var criticalVitality = Health <= MaxHealth * 0.25f;
            var vitalityStatus = criticalVitality
                ? $"CRITICAL VITALITY  {Health}/{MaxHealth}"
                : $"Vitality {Health}/{MaxHealth}";
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 75f, rect.width - 28f, 18f),
                vitalityStatus,
                criticalVitality ? hudDangerRowStyle : hudMutedRowStyle);

            DrawBar(
                new Rect(rect.x + 14f, rect.y + 98f, rect.width - 28f, 10f),
                Experience,
                ExperienceToNext,
                tealTexture);
            var xpTextWidth = (rect.width - 28f) * 0.64f;
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 111f, xpTextWidth, 18f),
                $"XP {Experience}/{ExperienceToNext}",
                hudMutedRowStyle);
            if (pendingExperienceFeedback > 0 && Time.time < experienceFeedbackExpiresAt)
            {
                GUI.Label(
                    new Rect(
                        rect.x + 14f + xpTextWidth,
                        rect.y + 111f,
                        rect.width - 28f - xpTextWidth,
                        18f),
                    $"+{pendingExperienceFeedback} XP",
                    hudAccentRowStyle);
            }

            var disciplineBinding =
                TetherfallInputBindings.Shared.GetBindingSummary(TetherfallInputAction.OpenSkillTree);
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 132f, rect.width - 28f, 18f),
                $"Discipline points {AvailableSkillPoints} unspent    {disciplineBinding} Open",
                hudRowStyle);
            GUI.DrawTexture(new Rect(rect.x + 14f, rect.y + 157f, rect.width - 28f, 8f), inkTexture);
            GUI.DrawTexture(
                new Rect(rect.x + 14f, rect.y + 157f, (rect.width - 28f) * Mathf.Clamp01(Stamina / MaxStamina), 8f),
                tealTexture);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 168f, rect.width - 28f, 18f),
                $"Stamina {Mathf.CeilToInt(Stamina)}/{Mathf.CeilToInt(MaxStamina)}",
                hudMutedRowStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 186f, rect.width - 28f, 18f),
                GetAccessibleCombatStatusText(),
                hudMutedRowStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 204f, rect.width - 28f, 19f),
                $"Weapon {activeWeapon}  ·  {GetExpeditionDirectiveText()}",
                hudRowStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 225f, rect.width - 28f, 18f),
                GetThreadlinkStatusText(),
                hudMutedRowStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 243f, rect.width - 28f, 15f),
                GetOvertoneStatusText(),
                hudMutedRowStyle);
        }

        private void DrawQuestPanel(Rect rect)
        {
            DrawPanel(rect, "THREADGLASS OBJECTIVE");
            GUI.Label(new Rect(rect.x + 14f, rect.y + 34f, rect.width - 28f, 54f), GetObjective(), bodyStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 92f, rect.width - 28f, 22f),
                GetObjectiveNavigationText(),
                hudMutedRowStyle);
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 116f, rect.width - 28f, 18f),
                GetPlayerSettlementHudHint(),
                hudMutedRowStyle);
        }

        private void DrawPartyPanel(Rect rect)
        {
            DrawPanel(rect, "EXPEDITION");
            var pactBinding =
                TetherfallInputBindings.Shared.GetBindingSummary(
                    TetherfallInputAction.RotateSupportPactOrRallyBeat1);
            var partyBinding =
                TetherfallInputBindings.Shared.GetBindingSummary(
                    TetherfallInputAction.RotateCompanionPairOrRallyBeat2);
            var tacticalBinding =
                TetherfallInputBindings.Shared.GetBindingSummary(
                    TetherfallInputAction.TacticalCommand);
            var fieldY = rect.y + 34f;
            GUI.Label(
                new Rect(rect.x + 14f, fieldY, rect.width - 28f, 18f),
                $"{pactBinding}  Pact focus  {ActiveCombatPactCount}/{MaximumActivePactSlots}  (cap {PactActorBudget})",
                hudRowStyle);
            fieldY += 20f;

            if (ActiveCombatPactCount == 0)
            {
                GUI.Label(
                    new Rect(rect.x + 14f, fieldY, rect.width - 28f, 17f),
                    "No combat Pact assigned.",
                    hudMutedRowStyle);
                fieldY += 18f;
            }
            else
            {
                var listedPacts = Mathf.Min(
                    PactActorBudget,
                    ActiveCombatPactCount);
                for (var slot = 0; slot < listedPacts; slot++)
                {
                    var record = GetActivePactRecord(slot);
                    if (record == null)
                    {
                        continue;
                    }

                    var definition =
                        TetherfallAccordSpeciesCatalog.Find(record.speciesId);
                    var focus =
                        string.Equals(
                            record.instanceId,
                            PactCommandFocusInstanceId,
                            StringComparison.Ordinal)
                            ? "FOCUS"
                            : $"SLOT {slot + 1}";
                    GUI.Label(
                        new Rect(
                            rect.x + 14f,
                            fieldY,
                            rect.width - 28f,
                            17f),
                        $"{focus}  {PactRosterRules.GetDisplayName(record)} · " +
                        $"{(definition != null ? definition.combatRole.ToString() : "Pact")}",
                        focus == "FOCUS" ? hudRowStyle : hudMutedRowStyle);
                    fieldY += 18f;
                }
            }

            GUI.Label(new Rect(rect.x + 14f, fieldY, rect.width - 28f, 18f),
                GetMountPactHudStatus(),
                hudMutedRowStyle);
            fieldY += 20f;
            GUI.Label(new Rect(rect.x + 14f, fieldY, rect.width - 28f, 18f),
                $"{tacticalBinding}  {GetTacticalCommandStatus()}",
                hudMutedRowStyle);
            fieldY += 22f;
            if (IsThreadlinkActive)
            {
                GUI.Label(new Rect(rect.x + 14f, fieldY, rect.width - 28f, 18f),
                    $"{pactBinding}  Beat 1  {GetThreadlinkWeaveBeatText(1)}",
                    hudRowStyle);
                GUI.Label(new Rect(rect.x + 14f, fieldY + 20f, rect.width - 28f, 18f),
                    $"{partyBinding}  Beat 2  {GetThreadlinkWeaveBeatText(2)}",
                    hudRowStyle);
                fieldY += 42f;
            }

            if (party.Count == 0)
            {
                GUI.Label(new Rect(rect.x + 14f, fieldY, rect.width - 28f, 18f), "No human companion recruited.", hudMutedRowStyle);
            }
            else
            {
                var y = fieldY;
                var reserveNames = string.Empty;
                foreach (var companion in party)
                {
                    if (companion == null)
                    {
                        continue;
                    }

                    if (companion.IsReserve)
                    {
                        reserveNames += string.IsNullOrEmpty(reserveNames)
                            ? companion.CompanionName
                            : $", {companion.CompanionName}";
                        continue;
                    }

                    if (y > rect.yMax - 20f)
                    {
                        break;
                    }

                    GUI.Label(new Rect(rect.x + 14f, y, rect.width - 28f, 18f),
                        $"{partyBinding}  Field  {companion.CompanionName} · {companion.TacticalAbilityShortLabel}",
                        hudRowStyle);
                    y += 19f;
                }

                if (!string.IsNullOrEmpty(reserveNames) && y <= rect.yMax - 22f)
                {
                    GUI.Label(new Rect(rect.x + 14f, y, rect.width - 28f, 20f),
                        $"Reserve  {reserveNames}",
                        hudMutedRowStyle);
                }
            }
        }

        private void DrawResourcePanel(Rect rect)
        {
            DrawPanel(rect, "FIELD STORES");
            var y = rect.y + 34f;
            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                GUI.Label(new Rect(rect.x + 14f, y, rect.width - 28f, 19f),
                    $"{ResourceName(kind)}  {GetResource(kind)}",
                    hudRowStyle);
                y += 21f;
            }

            var campStatus = ActiveCamp != null
                ? "Camp  Established"
                : $"Camp  Needs {Mathf.Max(0, CampTimberCost - GetResource(ResourceKind.Timber))} Timber, {Mathf.Max(0, CampStoneCost - GetResource(ResourceKind.Stone))} Stone";
            GUI.Label(new Rect(rect.x + 14f, y + 2f, rect.width - 28f, 20f), campStatus, hudMutedRowStyle);
        }

        private void DrawNotifications(float width, float height)
        {
            var y = height * 0.32f;
            foreach (var notification in notifications)
            {
                var notificationWidth = Mathf.Min(520f, width - 80f);
                var content = new GUIContent(notification.Message);
                var labelHeight = Mathf.Clamp(
                    promptStyle.CalcHeight(content, notificationWidth - 20f),
                    34f,
                    84f);
                var rect = new Rect(
                    width * 0.5f - notificationWidth * 0.5f,
                    y,
                    notificationWidth,
                    labelHeight);
                GUI.DrawTexture(rect, inkTexture);
                GUI.Label(new Rect(rect.x + 10f, rect.y, rect.width - 20f, rect.height), content, promptStyle);
                y += labelHeight + 7f;
            }
        }

        private string GetAccessibleCombatStatusText()
        {
            var status = GetCombatStatusText();
            if (!TetherfallAccessibilityRuntime.ShouldReinforceColorCues)
            {
                return status;
            }

            return CombatTarget != null
                ? $"TARGET LOCKED  {status}"
                : $"NO TARGET  {status}";
        }

        private void DrawCodex(float width, float height)
        {
            var outerMargin = Mathf.Clamp(Mathf.Min(width, height) * 0.06f, 18f, 40f);
            var overlayWidth = Mathf.Min(780f, Mathf.Max(1f, width - outerMargin * 2f));
            var overlayHeight = Mathf.Min(560f, Mathf.Max(1f, height - outerMargin * 2f));
            var overlay = new Rect(
                width * 0.5f - overlayWidth * 0.5f,
                height * 0.5f - overlayHeight * 0.5f,
                overlayWidth,
                overlayHeight);
            GUI.DrawTexture(new Rect(0f, 0f, width, height), overlayTexture);
            GUI.BeginGroup(overlay);
            GUI.DrawTexture(new Rect(0f, 0f, overlay.width, overlay.height), slateTexture);
            GUI.DrawTexture(new Rect(0f, 0f, overlay.width, 3f), copperTexture);

            var padding = Mathf.Min(
                Mathf.Clamp(overlay.width * 0.04f, 12f, 28f),
                overlay.width * 0.2f);
            GUI.Label(
                new Rect(padding, 24f, overlay.width - padding * 2f, 30f),
                "WAYFINDER FIELD LOG",
                headingStyle);

            var disciplineBinding =
                TetherfallInputBindings.Shared.GetBindingSummary(TetherfallInputAction.OpenSkillTree);
            var fieldLogBinding =
                TetherfallInputBindings.Shared.GetBindingSummary(TetherfallInputAction.OpenFieldLog);
            var settlementBinding =
                TetherfallInputBindings.Shared.GetBindingSummary(
                    TetherfallInputAction.OpenSettlementPlanning);
            var bodyTop = 70f;
            var footerHeight = 28f;
            var footerY = overlay.height - padding - footerHeight;
            GUI.Label(
                new Rect(
                    padding,
                    bodyTop,
                    overlay.width - padding * 2f,
                    Mathf.Max(0f, footerY - bodyTop - 12f)),
                "Earn XP through combat, gathering, exploration, recruitment, and Pacts. " +
                "The HUD bar shows rank progress, and nearby gains combine into one +XP readout.\n\n" +
                "Every rank grants +12 max vitality, fully restores vitality, and adds 1 discipline point. " +
                "Ranks 2, 4, and 6 mark +1 expedition field slot milestones.\n\n" +
                $"Press {disciplineBinding} to spend discipline points. The budget also gains up to 2 points " +
                "from portable camp upgrades, up to 3 from Alderwake restoration, and 2 from Skyvault.\n\n" +
                $"Press {settlementBinding} for settlement planning. Claim one Threadheart outside cities, " +
                "Threadway arrivals, dungeons, encounters, and story restoration footprints; return inside " +
                "its bounded claim to build, store resources, and assign consenting Pact workers.\n\n" +
                "Progress saves automatically. Resources regrow, cleared camps return, and repaired places remain changed.",
                bodyStyle);
            GUI.Label(
                new Rect(padding, footerY, overlay.width - padding * 2f, footerHeight),
                $"{fieldLogBinding} Close field log",
                mutedStyle);
            GUI.EndGroup();
        }

        private void DrawPanel(Rect rect, string title)
        {
            GUI.DrawTexture(rect, slateTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), copperTexture);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 9f, rect.width - 28f, 18f), title, tinyStyle);
        }

        private void DrawBar(Rect rect, int value, int maxValue)
        {
            DrawBar(rect, value, maxValue, healthTexture);
        }

        private void DrawBar(Rect rect, int value, int maxValue, Texture2D fillTexture)
        {
            GUI.DrawTexture(rect, inkTexture);
            var fillWidth = rect.width * Mathf.Clamp01(value / (float)Mathf.Max(1, maxValue));
            GUI.DrawTexture(new Rect(rect.x, rect.y, fillWidth, rect.height), fillTexture);
        }

        private static string ResourceName(ResourceKind kind)
        {
            return kind switch
            {
                ResourceKind.Timber => "Timber",
                ResourceKind.Stone => "Stone",
                ResourceKind.Resin => "Resin",
                ResourceKind.Chimesalt => "Chimesalt",
                ResourceKind.Herbs => "Herbs",
                ResourceKind.Ore => "Ore",
                ResourceKind.Fiber => "Fiber",
                _ => kind.ToString()
            };
        }

        private sealed class NotificationEntry
        {
            public string Message;
            public float ExpiresAt;
        }
    }
}
