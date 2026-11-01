using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private readonly List<EquipmentDefinition> equipmentCatalog = new List<EquipmentDefinition>();
        private readonly HashSet<string> unlockedEquipment = new HashSet<string>();
        private readonly Dictionary<EquipmentSlot, string> equippedFieldGear = new Dictionary<EquipmentSlot, string>();
        private readonly List<QuestRecord> questLog = new List<QuestRecord>();

        private GamePanel activePanel;
        private VendorNpc activeVendor;
        private SettlementRestorationNode settlement;
        private SkyvaultDungeon skyvault;
        private DungeonBoss activeDungeonBoss;
        private Texture2D threadglassIcons;
        private WeaponDiscipline activeWeapon = WeaponDiscipline.Pathblade;
        private int favors;
        private int cookedMeals;
        private int campTier;
        private int settlementTier;
        private int activatedDungeonPylons;
        private int partyRotation;
        private bool dungeonBossDefeated;
        private float nextMealUseAt;
        private string activeStationName;
        private int questLogPage;

        public WeaponDiscipline ActiveWeapon => activeWeapon;
        public int Favors => favors;
        public int CampTier => campTier;
        public int SettlementTier => settlementTier;
        public DungeonBoss ActiveDungeonBoss => activeDungeonBoss;
        public bool SkyvaultRouteStabilized => dungeonBossDefeated;
        internal bool CanProcessExpansionGameplayInput =>
            !GameplayPaused && Cursor.lockState == CursorLockMode.Locked;
        public int CombatDamageBonus
        {
            get
            {
                var weapon = equipmentCatalog.Find(item =>
                    item.slot == EquipmentSlot.Weapon &&
                    item.discipline == activeWeapon &&
                    unlockedEquipment.Contains(item.id));
                return (weapon != null ? weapon.damageBonus : 0) + SkillFlatDamageBonus;
            }
        }
        public int GuardBonus =>
            GetEquippedBonus(EquipmentSlot.Charm, gear => gear.guardBonus) +
            MealGuardBonus +
            SkillGuardBonus;
        public float MoveSpeedBonus =>
            GetEquippedBonus(EquipmentSlot.Tool, gear => gear.moveSpeedBonus) +
            MealMoveSpeedBonus +
            SkillMoveSpeedBonus;
        public float StaminaBonus =>
            GetEquippedBonus(EquipmentSlot.Tool, gear => gear.staminaBonus) +
            SkillMaxStaminaBonus;
        public float StaminaRegenerationBonus =>
            GetEquippedBonus(EquipmentSlot.Charm, gear => gear.staminaRegenerationBonus) +
            SkillStaminaRegenerationBonus +
            PactStaminaRegenerationBonus;
        public float WindLiftVelocityBonus =>
            GetEquippedBonus(EquipmentSlot.Tool, gear => gear.windLiftVelocityBonus) +
            SkillWindLiftVelocityBonus;

        private void InitializeExpeditionExpansion()
        {
            BuildEquipmentCatalog();
            BuildQuestLog();
            InitializeProvisioning();
            threadglassIcons = Resources.Load<Texture2D>("Art/Original/tetherfall_threadglass_icons");
            WorldExpansionBuilder.Build(this);
        }

        private void HandleExpansionInput()
        {
            if (CodexOpen)
            {
                return;
            }

            if (activePanel != GamePanel.None)
            {
                if (activePanel == GamePanel.Settings && HandleSettingsRebindInput())
                {
                    return;
                }

                if (activePanel == GamePanel.Threadways && UpdateWorldMapUiInput())
                {
                    return;
                }

                var closeRequested =
                    TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.CancelOrReleaseCursor) ||
                    (activePanel == GamePanel.FieldPack &&
                     TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.OpenEquipmentAndStores)) ||
                    (activePanel == GamePanel.QuestLog &&
                     TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.OpenQuestLedger)) ||
                    (activePanel == GamePanel.SkillTree &&
                     TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.OpenSkillTree)) ||
                    (activePanel == GamePanel.Settings &&
                     TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.OpenSettings)) ||
                    (activePanel == GamePanel.Threadways &&
                     TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.OpenWorldMap)) ||
                    ((activePanel == GamePanel.Crafting || activePanel == GamePanel.Camp) &&
                     TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.OpenCraftingOrCampServices));
                if (closeRequested)
                {
                    ClosePanel();
                }

                return;
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.OpenWorldMap))
            {
                OpenWorldMapFromField();
                return;
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.OpenSettings))
            {
                OpenPanel(GamePanel.Settings, "Controls & accessibility");
                return;
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.OpenEquipmentAndStores))
            {
                OpenPanel(GamePanel.FieldPack, "Field pack");
                RecordTutorialEvent(TutorialEvent.OpenFieldKit);
                return;
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.OpenQuestLedger))
            {
                OpenPanel(GamePanel.QuestLog, "Quest ledger");
                return;
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.OpenSkillTree))
            {
                OpenPanel(GamePanel.SkillTree, "Wayfinder disciplines");
                return;
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.OpenCraftingOrCampServices))
            {
                if (ActiveCamp != null && Vector3.Distance(PlayerTransform.position, ActiveCamp.transform.position) < 4.2f)
                {
                    OpenPanel(GamePanel.Camp, "Portable camp");
                }
                else
                {
                    OpenPanel(GamePanel.Crafting, "Field crafting");
                }

                return;
            }

            if (!CanProcessExpansionGameplayInput)
            {
                return;
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.EquipPathblade))
            {
                TrySetWeapon(WeaponDiscipline.Pathblade);
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.EquipRiftbow))
            {
                TrySetWeapon(WeaponDiscipline.Riftbow);
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.EquipThreadstaff))
            {
                TrySetWeapon(WeaponDiscipline.Threadstaff);
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.RotateCompanionPairOrRallyBeat2))
            {
                if (!TryCycleThreadlinkWeaveBeat(false))
                {
                    CycleParty();
                }
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.RotateSupportPactOrRallyBeat1))
            {
                if (!TryCycleThreadlinkWeaveBeat(true))
                {
                    CycleActivePact();
                }
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.ResonanceAbility))
            {
                Player.TryResonanceAbility();
            }

            if (TetherfallAccessibilityRuntime.GetInputPreference(
                    TetherfallAccessibilityInputAction.CombatFocus) ==
                TetherfallAccessibilityInputPreference.Toggle &&
                TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.CombatFocus))
            {
                ToggleCombatTarget();
            }
            else if (TetherfallAccessibilityRuntime.GetInputPreference(
                         TetherfallAccessibilityInputAction.CombatFocus) ==
                     TetherfallAccessibilityInputPreference.Hold)
            {
                if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.CombatFocus) &&
                    CombatTarget == null)
                {
                    ToggleCombatTarget();
                }
                else if (TetherfallInputBindings.Shared.WasReleasedThisFrame(TetherfallInputAction.CombatFocus) &&
                         CombatTarget != null)
                {
                    ClearCombatTarget();
                }
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.TacticalCommand))
            {
                TryTacticalCommand();
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.CycleExpeditionOrder))
            {
                CycleExpeditionDirective();
            }

            if (TetherfallInputBindings.Shared.WasPressedThisFrame(TetherfallInputAction.UseProvision))
            {
                TryUseCookedMeal();
            }
        }

        public int GetBaseDamage()
        {
            return activeWeapon switch
            {
                WeaponDiscipline.Pathblade => 26 + CombatDamageBonus,
                WeaponDiscipline.Riftbow => 20 + CombatDamageBonus,
                WeaponDiscipline.Threadstaff => 18 + CombatDamageBonus,
                _ => 20
            };
        }

        public bool IsWeaponUnlocked(WeaponDiscipline discipline)
        {
            return discipline == WeaponDiscipline.Pathblade ||
                   unlockedEquipment.Contains(discipline == WeaponDiscipline.Riftbow ? "weapon.riftbow" : "weapon.threadstaff");
        }

        public void TrySetWeapon(WeaponDiscipline discipline, bool armCombatRelay = true)
        {
            if (!IsWeaponUnlocked(discipline))
            {
                Notify($"{WeaponName(discipline)} is not crafted yet.");
                return;
            }

            if (activeWeapon == discipline)
            {
                Notify($"{WeaponName(discipline)} is already equipped.");
                return;
            }

            var previousWeapon = activeWeapon;
            activeWeapon = discipline;
            Player?.RefreshWeaponVisual();
            if (armCombatRelay)
            {
                ArmCrossweaveFromWeaponSwap(previousWeapon, discipline);
                RequestCrossweaveWeaponSave();
            }
            else
            {
                ClearCrossweave();
                RequestSave();
            }

            Notify($"{WeaponName(discipline)} equipped.");
        }

        public void OpenVendor(VendorNpc vendor)
        {
            activeVendor = vendor;
            OpenPanel(GamePanel.Vendor, vendor.VendorName);
        }

        public void OpenCrafting(string stationName)
        {
            activeStationName = stationName;
            OpenPanel(GamePanel.Crafting, stationName);
        }

        public void OpenCampPanel()
        {
            activeStationName = "Portable camp";
            OpenPanel(GamePanel.Camp, activeStationName);
        }

        public void RegisterSettlement(SettlementRestorationNode restorationNode)
        {
            settlement = restorationNode;
        }

        public void RegisterSkyvault(SkyvaultDungeon dungeon)
        {
            skyvault = dungeon;
        }

        public void RegisterDungeonBoss(DungeonBoss boss)
        {
            activeDungeonBoss = boss;
        }

        public void ClearDungeonBoss(DungeonBoss boss)
        {
            if (activeDungeonBoss == boss)
            {
                activeDungeonBoss = null;
            }
        }

        public void RefreshPartyLineup()
        {
            if (party.Count == 0)
            {
                return;
            }

            partyRotation = Mathf.Abs(partyRotation) % party.Count;
            var activeSlot = 0;
            for (var index = 0; index < party.Count; index++)
            {
                var active = party.Count <= 2 ||
                             index == partyRotation ||
                             index == (partyRotation + 1) % party.Count;
                party[index].SetReserve(!active, active ? activeSlot++ : -1);
            }
        }

        public void RefreshActivePactVisuals()
        {
            foreach (var pact in pacts)
            {
                if (pact != null)
                {
                    pact.SetActiveSupport(pact == ActiveEchosworn);
                }
            }
        }

        public void CycleParty()
        {
            if (party.Count <= 2)
            {
                Notify("Recruit a third companion before changing the field pair.");
                return;
            }

            partyRotation = (partyRotation + 1) % party.Count;
            RefreshPartyLineup();
            var names = new List<string>();
            foreach (var companion in party)
            {
                if (companion != null && !companion.IsReserve)
                {
                    names.Add(companion.CompanionName);
                }
            }

            Notify($"Field pair: {string.Join(" and ", names)}.");
            RequestSave();
        }

        public void CycleActivePact()
        {
            CyclePactCommandFocus();
        }

        public void AddFavors(int amount, string source)
        {
            amount = ApplySkillFavorReward(amount);
            favors += Mathf.Max(0, amount);
            Notify($"+{amount} Favors from {source}");
            RequestSave();
        }

        public void OnDungeonPylonActivated()
        {
            activatedDungeonPylons = Mathf.Min(3, activatedDungeonPylons + 1);
            AdvanceQuest("skyvault", 1);
            RequestSave();
        }

        public void OnDungeonBossDefeated()
        {
            if (dungeonBossDefeated)
            {
                return;
            }

            dungeonBossDefeated = true;
            RecordPactSkyvaultRestored();
            ClearDungeonBoss(activeDungeonBoss);
            AddFavors(40, "the Skyvault Warden");
            AddResource(ResourceKind.Chimesalt, 4);
            AddResource(ResourceKind.Ore, 3);
            AdvanceQuest("skyvault", 2);
            ReconcileFenlightQuest();
            Notify("The Skyvault route is stable. New trade stock reaches Alderwake.");
            RequestSave();
        }

        public void OnSettlementRestored()
        {
            AdvanceQuest("alderwake", 1);
            RequestSave();
        }

        public void OnFarmHarvested()
        {
            AdvanceQuest("hearthgarden", 1);
            AddFavors(2, "Hearthgarden harvest");
        }

        public void OnPactFormed()
        {
            AdvanceQuest("accords", 1);
        }

        public void OnHostileCampCleared()
        {
            AdvanceQuest("campward", 1);
            AddFavors(6, "safe-route patrol");
        }

        public void OpenSettlement()
        {
            if (settlement == null)
            {
                Notify("No restoration site is active.");
                return;
            }

            OpenPanel(GamePanel.Crafting, "Alderwake restoration");
        }

        private void BuildEquipmentCatalog()
        {
            if (equipmentCatalog.Count > 0)
            {
                return;
            }

            equipmentCatalog.AddRange(EquipmentCatalog.CreateAll());
        }

        private void BuildQuestLog()
        {
            if (questLog.Count > 0)
            {
                return;
            }

            questLog.AddRange(QuestCatalog.CreateAll());
        }

        private void AdvanceQuest(string questId, int amount)
        {
            var quest = questLog.Find(record => record.id == questId);
            if (quest == null || quest.completed)
            {
                return;
            }

            quest.current = Mathf.Min(quest.required, quest.current + amount);
            if (!quest.IsComplete)
            {
                return;
            }

            quest.completed = true;
            GainExperience(80);
            AddFavors(quest.favorReward, quest.title);
            Notify($"{quest.title} completed.");
        }

        private bool TryCraft(EquipmentDefinition definition)
        {
            if (unlockedEquipment.Contains(definition.id))
            {
                Notify($"{definition.displayName} is already prepared.");
                return false;
            }

            if (!CanAfford(definition.craftCost))
            {
                Notify($"You need {DescribeMissingCost(definition.craftCost)}.");
                return false;
            }

            SpendCost(definition.craftCost);
            unlockedEquipment.Add(definition.id);
            AutoEquipFieldGear(definition);
            if (definition.maxHealthBonus > 0)
            {
                MaxHealth += definition.maxHealthBonus;
                HealPlayer(MaxHealth);
            }

            GainExperience(45);
            AdvanceQuest("fieldcraft", 1);
            Notify($"{definition.displayName} crafted.");
            RequestSave();
            return true;
        }

        private bool TryBuy(EquipmentDefinition definition)
        {
            if (unlockedEquipment.Contains(definition.id))
            {
                Notify($"{definition.displayName} is already in your field pack.");
                return false;
            }

            if (favors < definition.favorPrice)
            {
                Notify($"Need {definition.favorPrice - favors} more Favors.");
                return false;
            }

            favors -= definition.favorPrice;
            unlockedEquipment.Add(definition.id);
            AutoEquipFieldGear(definition);
            if (definition.maxHealthBonus > 0)
            {
                MaxHealth += definition.maxHealthBonus;
                HealPlayer(MaxHealth);
            }

            GainExperience(30);
            AdvanceQuest("fieldcraft", 1);
            Notify($"{definition.displayName} purchased.");
            RequestSave();
            return true;
        }

        private void TryCook()
        {
            TryCookRecipe(ProvisioningCatalog.BrightbrothId);
        }

        private void TryUseCookedMeal()
        {
            TryUseActiveMeal();
        }

        private void TryUpgradeCamp()
        {
            if (ActiveCamp == null)
            {
                Notify("Build a portable camp before improving it.");
                return;
            }

            if (campTier >= 2)
            {
                Notify("The portable camp is fully upgraded.");
                return;
            }

            var cost = campTier == 0
                ? new List<ResourceCost> { new ResourceCost(ResourceKind.Timber, 4), new ResourceCost(ResourceKind.Stone, 3), new ResourceCost(ResourceKind.Fiber, 2) }
                : new List<ResourceCost> { new ResourceCost(ResourceKind.Timber, 6), new ResourceCost(ResourceKind.Ore, 3), new ResourceCost(ResourceKind.Chimesalt, 2) };

            if (!CanAfford(cost))
            {
                Notify($"Camp upgrade needs {DescribeMissingCost(cost)}.");
                return;
            }

            SpendCost(cost);
            campTier++;
            ActiveCamp.SetTier(campTier);
            RecordPactCampLevel(campTier + 1);
            MaxHealth += 6;
            HealPlayer(MaxHealth);
            var serviceUnlock = campTier == 1
                ? "The camp kitchen can now prepare provisions."
                : "Supply contracts and advanced provisions are now available.";
            Notify($"Portable camp upgraded to level {campTier + 1}. {serviceUnlock}");
            RequestSave();
        }

        private void TryBuyResource(ResourceKind kind, int amount, int price)
        {
            if (favors < price)
            {
                Notify($"Need {price - favors} more Favors.");
                return;
            }

            favors -= price;
            resources[kind] += amount;
            Notify($"Purchased {amount} {ResourceLabel(kind)}.");
            RequestSave();
        }

        private void WriteExpansionSave(TetherfallSaveData data)
        {
            data.activeWeapon = activeWeapon;
            data.favors = favors;
            data.cookedMeals = cookedMeals;
            data.campTier = campTier;
            data.settlementTier = settlementTier;
            data.activatedDungeonPylons = activatedDungeonPylons;
            data.skyvaultWaitingForSwarm = skyvault != null && skyvault.WaitingForSwarm;
            data.skyvaultSwarmEnemies = skyvault != null ? skyvault.RemainingSwarmEnemies : 0;
            data.partyRotation = partyRotation;
            WriteFarmSave(data);
            WriteResourceNodeSave(data);
            WriteFieldCacheSave(data);
            WriteRestorationSave(data);
            WriteProvisioningSave(data);
            WriteCinderwoldArcSave(data);
            WriteTrialSave(data);
            WriteDungeonSave(data);
            WriteCitySave(data);
            WriteBountySave(data);
            WriteThreadwaySave(data);
            WriteExpeditionCombatSave(data);
            WriteSkillTreeSave(data);
            data.dungeonBossDefeated = dungeonBossDefeated;

            foreach (var equipmentId in unlockedEquipment)
            {
                data.unlockedEquipment.Add(equipmentId);
            }

            foreach (var equipmentId in equippedFieldGear.Values)
            {
                data.equippedEquipmentIds.Add(equipmentId);
            }

            foreach (var quest in questLog)
            {
                data.quests.Add(new QuestProgressData
                {
                    id = quest.id,
                    current = quest.current,
                    completed = quest.completed
                });
            }

            foreach (var camp in enemyCamps)
            {
                if (camp != null && camp.AwardedFirstClear && !string.IsNullOrEmpty(camp.PersistenceId))
                {
                    data.firstClearedEncounterIds.Add(camp.PersistenceId);
                }
            }

            WriteRouteEventSave(data);
        }

        private void RestoreExpansionState(TetherfallSaveData data)
        {
            favors = Mathf.Max(0, data.favors);
            cookedMeals = Mathf.Max(0, data.cookedMeals);
            campTier = Mathf.Clamp(data.campTier, 0, 2);
            settlementTier = Mathf.Clamp(data.settlementTier, 0, 3);
            activatedDungeonPylons = Mathf.Clamp(data.activatedDungeonPylons, 0, 3);
            partyRotation = Mathf.Max(0, data.partyRotation);
            dungeonBossDefeated = data.dungeonBossDefeated;
            RestoreSkillTreeState(data);

            unlockedEquipment.Clear();
            foreach (var equipmentId in data.unlockedEquipment)
            {
                if (equipmentCatalog.Exists(item => item.id == equipmentId))
                {
                    unlockedEquipment.Add(equipmentId);
                }
            }

            equippedFieldGear.Clear();
            foreach (var equipmentId in data.equippedEquipmentIds)
            {
                var definition = equipmentCatalog.Find(item => item.id == equipmentId);
                if (definition != null &&
                    definition.slot != EquipmentSlot.Weapon &&
                    unlockedEquipment.Contains(definition.id))
                {
                    equippedFieldGear[definition.slot] = definition.id;
                }
            }

            foreach (var definition in equipmentCatalog)
            {
                AutoEquipFieldGear(definition);
            }

            Player?.RefreshEquipmentVisuals();

            foreach (var progress in data.quests)
            {
                var quest = questLog.Find(record => record.id == progress.id);
                if (quest == null)
                {
                    continue;
                }

                quest.current = Mathf.Clamp(progress.current, 0, quest.required);
                quest.completed = progress.completed || quest.IsComplete;
            }

            RestoreFieldCacheState(data);
            var fieldArchiveQuest = questLog.Find(record => record.id == "fieldarchives");
            if (fieldArchiveQuest != null)
            {
                fieldArchiveQuest.current = Mathf.Clamp(
                    CountFieldArchiveRecoveries(),
                    0,
                    fieldArchiveQuest.required);
                fieldArchiveQuest.completed = fieldArchiveQuest.IsComplete;
            }

            var clearedEncounterIds = new HashSet<string>(data.firstClearedEncounterIds);
            foreach (var camp in enemyCamps)
            {
                if (camp != null)
                {
                    var wasFirstClear = clearedEncounterIds.Contains(camp.PersistenceId) ||
                                        (camp.PersistenceId == HollowReedMilestoneId &&
                                         clearedEncounterIds.Contains("encounter.alderwake.hollow_reed@-38:33"));
                    camp.RestoreFirstClear(wasFirstClear);
                }
            }

            RestoreRouteEventState(data);
            RestoreRestorationState(data);
            RestoreProvisioningState(data);
            RestoreResourceNodeState(data);

            if (data.quests.Count == 0)
            {
                RestoreLegacyQuestProgress("alderwake", settlementTier);
                RestoreLegacyQuestProgress("skyvault", activatedDungeonPylons + (dungeonBossDefeated ? 2 : 0));
                RestoreLegacyQuestProgress("fieldcraft", unlockedEquipment.Count);
                RestoreLegacyQuestProgress("campward", clearedCamps);
                var supportPactCount = 0;
                foreach (var pact in pacts)
                {
                    if (pact != null && pact.IsPacted && !pact.IsMount)
                    {
                        supportPactCount++;
                    }
                }

                RestoreLegacyQuestProgress("accords", supportPactCount);
            }

            activeWeapon = IsWeaponUnlocked(data.activeWeapon)
                ? data.activeWeapon
                : WeaponDiscipline.Pathblade;
            settlement?.RestoreToTier(settlementTier);
            if (ActiveCamp != null)
            {
                ActiveCamp.SetTier(campTier);
            }

            RefreshPartyLineup();
            RefreshActivePactVisuals();
            RestoreFarmState(data);
            RestoreCinderwoldArcState(data);
            RestoreCityState(data);
            RestoreBountyState(data);
            RestoreThreadwayState(data);
            RestoreExpeditionCombatState(data);
            skyvault?.RestoreState(
                activatedDungeonPylons,
                dungeonBossDefeated,
                data.skyvaultWaitingForSwarm,
                data.skyvaultSwarmEnemies);
            RestoreDungeonState(data);
            RestoreTrialState(data);
        }

        private void RestoreLegacyQuestProgress(string questId, int current)
        {
            var quest = questLog.Find(record => record.id == questId);
            if (quest == null)
            {
                return;
            }

            quest.current = Mathf.Clamp(current, 0, quest.required);
            quest.completed = quest.IsComplete;
        }

        private void TryRestoreSettlement()
        {
            if (settlement == null)
            {
                Notify("The Alderwake restoration point is unavailable.");
                return;
            }

            if (settlementTier >= 3)
            {
                Notify("Alderwake's station is fully restored.");
                return;
            }

            var cost = settlementTier switch
            {
                0 => new List<ResourceCost> { new ResourceCost(ResourceKind.Timber, 6), new ResourceCost(ResourceKind.Stone, 5) },
                1 => new List<ResourceCost> { new ResourceCost(ResourceKind.Resin, 4), new ResourceCost(ResourceKind.Fiber, 4), new ResourceCost(ResourceKind.Ore, 2) },
                _ => new List<ResourceCost> { new ResourceCost(ResourceKind.Chimesalt, 4), new ResourceCost(ResourceKind.Ore, 4), new ResourceCost(ResourceKind.Herbs, 3) }
            };

            if (!CanAfford(cost))
            {
                Notify($"Restoration needs {DescribeMissingCost(cost)}.");
                return;
            }

            SpendCost(cost);
            settlementTier++;
            settlement.RestoreToTier(settlementTier);
            GainExperience(85);
            AddFavors(15, "Alderwake restoration");
            OnSettlementRestored();
            Notify($"Alderwake station restored to tier {settlementTier}.");
            RequestSave();
        }

        private bool CanAfford(IEnumerable<ResourceCost> costs)
        {
            foreach (var cost in costs)
            {
                if (GetResource(cost.kind) < cost.amount)
                {
                    return false;
                }
            }

            return true;
        }

        private void SpendCost(IEnumerable<ResourceCost> costs)
        {
            foreach (var cost in costs)
            {
                resources[cost.kind] -= cost.amount;
            }

            RequestSave();
        }

        private string DescribeMissingCost(IEnumerable<ResourceCost> costs)
        {
            var parts = new List<string>();
            foreach (var cost in costs)
            {
                var missing = Mathf.Max(0, cost.amount - GetResource(cost.kind));
                if (missing > 0)
                {
                    parts.Add($"{missing} {ResourceLabel(cost.kind)}");
                }
            }

            return string.Join(", ", parts);
        }

        private void AutoEquipFieldGear(EquipmentDefinition definition)
        {
            if (definition == null ||
                definition.slot == EquipmentSlot.Weapon ||
                !unlockedEquipment.Contains(definition.id) ||
                equippedFieldGear.ContainsKey(definition.slot))
            {
                return;
            }

            equippedFieldGear[definition.slot] = definition.id;
            Player?.RefreshEquipmentVisuals();
        }

        internal string GetEquippedFieldGearId(EquipmentSlot slot)
        {
            return equippedFieldGear.TryGetValue(slot, out var equipmentId) ? equipmentId : string.Empty;
        }

        private bool IsEquipmentEquipped(EquipmentDefinition definition)
        {
            return definition != null &&
                   definition.slot != EquipmentSlot.Weapon &&
                   equippedFieldGear.TryGetValue(definition.slot, out var equipmentId) &&
                   equipmentId == definition.id;
        }

        private void EquipFieldGear(EquipmentDefinition definition)
        {
            if (definition == null || definition.slot == EquipmentSlot.Weapon)
            {
                return;
            }

            if (!unlockedEquipment.Contains(definition.id))
            {
                Notify($"{definition.displayName} is not in your field pack.");
                return;
            }

            equippedFieldGear[definition.slot] = definition.id;
            Player?.RefreshEquipmentVisuals();
            Notify($"{definition.displayName} equipped.");
            RequestSave();
        }

        private int GetEquippedBonus(EquipmentSlot slot, Func<EquipmentDefinition, int> selector)
        {
            if (!equippedFieldGear.TryGetValue(slot, out var equipmentId))
            {
                return 0;
            }

            var item = equipmentCatalog.Find(candidate => candidate.id == equipmentId);
            return item != null ? selector(item) : 0;
        }

        private float GetEquippedBonus(EquipmentSlot slot, Func<EquipmentDefinition, float> selector)
        {
            if (!equippedFieldGear.TryGetValue(slot, out var equipmentId))
            {
                return 0f;
            }

            var item = equipmentCatalog.Find(candidate => candidate.id == equipmentId);
            return item != null ? selector(item) : 0f;
        }

        private void OpenPanel(GamePanel panel, string stationName)
        {
            CodexOpen = false;
            if (panel == GamePanel.QuestLog)
            {
                questLogPage = 0;
            }

            activePanel = panel;
            activeStationName = stationName;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ClosePanel()
        {
            CancelSettingsRebind();
            if (activePanel == GamePanel.Threadways)
            {
                ResetWorldMapUi();
            }

            activePanel = GamePanel.None;
            activeVendor = null;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void DrawExpansionUi(float width, float height)
        {
            if (activePanel == GamePanel.None || CodexOpen)
            {
                return;
            }

            var panelWidth = Mathf.Min(960f, width - 80f);
            var panelHeight = Mathf.Min(620f, height - 70f);
            var rect = new Rect(
                width * 0.5f - panelWidth * 0.5f,
                height * 0.5f - panelHeight * 0.5f,
                panelWidth,
                panelHeight);
            GUI.DrawTexture(new Rect(0f, 0f, width, height), overlayTexture);
            GUI.DrawTexture(rect, slateTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), copperTexture);
            DrawPanelHeading(rect, activeStationName);

            if (threadglassIcons != null)
            {
                GUI.DrawTextureWithTexCoords(
                    new Rect(rect.xMax - 70f, rect.y + 18f, 44f, 44f),
                    threadglassIcons,
                    new Rect(0.5f, 0.5f, 0.25f, 0.25f));
            }

            switch (activePanel)
            {
                case GamePanel.FieldPack:
                    DrawFieldPack(rect);
                    break;
                case GamePanel.Crafting:
                    DrawCrafting(rect);
                    break;
                case GamePanel.Vendor:
                    DrawVendor(rect);
                    break;
                case GamePanel.QuestLog:
                    DrawQuestLog(rect);
                    break;
                case GamePanel.Camp:
                    DrawCampPanel(rect);
                    break;
                case GamePanel.Restoration:
                    DrawRestorationPanel(rect);
                    break;
                case GamePanel.CinderResolution:
                    DrawCinderwoldResolution(rect);
                    break;
                case GamePanel.CityBoard:
                    DrawCityBoard(rect);
                    break;
                case GamePanel.Threadways:
                    DrawThreadwayPanel(rect);
                    break;
                case GamePanel.SkillTree:
                    DrawSkillTree(rect);
                    break;
                case GamePanel.Settings:
                    DrawSettingsPanel(rect);
                    break;
            }

            var cancelBinding =
                TetherfallInputBindings.Shared.GetBindingSummary(
                    TetherfallInputAction.CancelOrReleaseCursor);
            GUI.Label(
                new Rect(
                    rect.x + 24f,
                    rect.y + rect.height - 30f,
                    rect.width - 48f,
                    20f),
                $"{cancelBinding} closes this panel.",
                mutedStyle);
        }

        private void DrawPanelHeading(Rect rect, string heading)
        {
            GUI.Label(new Rect(rect.x + 26f, rect.y + 20f, 520f, 28f), heading.ToUpperInvariant(), headingStyle);
        }

        private void DrawFieldPack(Rect rect)
        {
            var rightColumnX = rect.x + rect.width * 0.51f;
            var rightColumnWidth = rect.xMax - 26f - rightColumnX;
            GUI.Label(new Rect(rect.x + 26f, rect.y + 70f, rect.width * 0.43f, 24f), "Weapon disciplines", headingStyle);
            DrawWeaponRow(rect, 0, WeaponDiscipline.Pathblade, "Reliable close-range strikes.");
            DrawWeaponRow(rect, 1, WeaponDiscipline.Riftbow, "Longer reach with a Threadglass bolt.");
            DrawWeaponRow(rect, 2, WeaponDiscipline.Threadstaff, "Short-range resonance burst.");

            GUI.Label(new Rect(rightColumnX, rect.y + 70f, rightColumnWidth, 24f), "Field equipment", headingStyle);
            var y = rect.y + 104f;
            foreach (var item in equipmentCatalog)
            {
                if (item.slot == EquipmentSlot.Weapon || !unlockedEquipment.Contains(item.id))
                {
                    continue;
                }

                var equipped = IsEquipmentEquipped(item);
                if (GUI.Button(new Rect(rightColumnX, y, 92f, 34f), equipped ? "Equipped" : "Equip"))
                {
                    EquipFieldGear(item);
                }

                GUI.Label(
                    new Rect(rightColumnX + 106f, y, rightColumnWidth - 106f, 44f),
                    $"{item.displayName}\n{item.description}",
                    equipped ? bodyStyle : mutedStyle);
                y += 50f;
            }

            var storesY = rect.y + rect.height - 212f;
            GUI.Label(new Rect(rect.x + 26f, storesY, rect.width * 0.58f, 24f), "Field stores", headingStyle);
            var resourcesY = storesY + 28f;
            var column = 0;
            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                var x = rect.x + 26f + (column % 2) * 190f;
                GUI.Label(new Rect(x, resourcesY + (column / 2) * 24f, 175f, 20f), $"{ResourceLabel(kind)}  {GetResource(kind)}", bodyStyle);
                column++;
            }

            GUI.Label(new Rect(rect.x + 300f, storesY, 160f, 24f), $"Favors  {favors}", headingStyle);
            DrawPackedMealSelection(rect);
        }

        private void DrawWeaponRow(Rect rect, int index, WeaponDiscipline discipline, string description)
        {
            var unlocked = IsWeaponUnlocked(discipline);
            var selected = activeWeapon == discipline;
            var label = $"{index + 1}. {WeaponName(discipline)}";
            if (!unlocked)
            {
                label += "  Locked";
            }

            if (GUI.Button(new Rect(rect.x + 26f, rect.y + 104f + index * 60f, 208f, 38f), label))
            {
                TrySetWeapon(discipline, false);
            }

            GUI.Label(new Rect(rect.x + 248f, rect.y + 108f + index * 60f, 145f, 42f), selected ? $"Equipped\n{description}" : description, mutedStyle);
        }

        private void DrawCrafting(Rect rect)
        {
            if (activeStationName == "Alderwake restoration")
            {
                DrawAlderwakeRestorationPanel(rect);
                return;
            }

            GUI.Label(new Rect(rect.x + 26f, rect.y + 70f, 470f, 24f), "Crafted tools are permanent expedition upgrades.", bodyStyle);
            var y = rect.y + 110f;
            foreach (var item in equipmentCatalog)
            {
                if (item.favorPrice > 0 || item.restorationRewardOnly)
                {
                    continue;
                }

                var craftLabel = unlockedEquipment.Contains(item.id) ? "Prepared" : "Craft";
                if (GUI.Button(new Rect(rect.x + 26f, y, 130f, 34f), craftLabel))
                {
                    TryCraft(item);
                }

                GUI.Label(new Rect(rect.x + 174f, y, 540f, 40f), $"{item.displayName}: {item.description}\n{DescribeCost(item.craftCost)}", bodyStyle);
                y += 62f;
            }
        }

        private void DrawVendor(Rect rect)
        {
            DrawProvisioningVendor(rect);
        }

        private void DrawQuestLog(Rect rect)
        {
            GUI.Label(new Rect(rect.x + 26f, rect.y + 70f, 680f, 24f), "Routes change only when work is completed.", bodyStyle);
            const int rowsPerPage = 5;
            var pageCount = Mathf.Max(1, Mathf.CeilToInt(questLog.Count / (float)rowsPerPage));
            questLogPage = Mathf.Clamp(questLogPage, 0, pageCount - 1);
            var y = rect.y + 112f;
            var startIndex = questLogPage * rowsPerPage;
            var endIndex = Mathf.Min(startIndex + rowsPerPage, questLog.Count);
            for (var index = startIndex; index < endIndex; index++)
            {
                var quest = questLog[index];
                var state = quest.completed ? "Complete" : $"{quest.current}/{quest.required}";
                GUI.Label(new Rect(rect.x + 26f, y, 650f, 20f), $"{quest.title}   {state}", headingStyle);
                GUI.Label(new Rect(rect.x + 26f, y + 23f, 660f, 30f), quest.objective, bodyStyle);
                y += 62f;
            }

            var navigationY = rect.yMax - 70f;
            if (GUI.Button(new Rect(rect.x + 26f, navigationY, 110f, 32f), "Previous") && questLogPage > 0)
            {
                questLogPage--;
            }

            if (GUI.Button(new Rect(rect.x + 150f, navigationY, 110f, 32f), "Next") && questLogPage + 1 < pageCount)
            {
                questLogPage++;
            }

            GUI.Label(
                new Rect(rect.x + 280f, navigationY + 6f, 240f, 20f),
                $"Page {questLogPage + 1}/{pageCount}",
                mutedStyle);
        }

        private void DrawCampPanel(Rect rect)
        {
            if (GUI.Button(
                    new Rect(rect.xMax - 472f, rect.y + 18f, 118f, 34f),
                    !showingPactUnfoldingCamp && !showingPactRosterCamp
                        ? "Services ·"
                        : "Services"))
            {
                showingPactUnfoldingCamp = false;
                showingPactRosterCamp = false;
                pendingPactFormId = string.Empty;
            }

            if (GUI.Button(
                    new Rect(rect.xMax - 344f, rect.y + 18f, 118f, 34f),
                    showingPactUnfoldingCamp
                        ? "Unfolding ·"
                        : "Unfolding"))
            {
                showingPactUnfoldingCamp = true;
                showingPactRosterCamp = false;
                pendingPactFormId = string.Empty;
            }

            if (GUI.Button(
                    new Rect(rect.xMax - 216f, rect.y + 18f, 128f, 34f),
                    showingPactRosterCamp
                        ? "Pact roster ·"
                        : "Pact roster"))
            {
                showingPactUnfoldingCamp = false;
                showingPactRosterCamp = true;
                pendingPactFormId = string.Empty;
            }

            if (showingPactRosterCamp)
            {
                DrawPactRosterSection(
                    new Rect(
                        rect.x + 20f,
                        rect.y + 64f,
                        rect.width - 40f,
                        rect.height - 104f));
            }
            else if (showingPactUnfoldingCamp)
            {
                DrawPactUnfoldingCamp(rect);
            }
            else
            {
                DrawProvisioningCamp(rect);
            }
        }

        private void DrawAlderwakeRestorationPanel(Rect rect)
        {
            GUI.Label(new Rect(rect.x + 26f, rect.y + 70f, 670f, 50f), "Restore Alderwake's route station. Each tier changes the visible site and expands the local service loop.", bodyStyle);
            if (GUI.Button(new Rect(rect.x + 26f, rect.y + 142f, 220f, 40f), settlementTier >= 3 ? "Station restored" : $"Restore tier {settlementTier + 1}"))
            {
                TryRestoreSettlement();
            }
            GUI.Label(new Rect(rect.x + 266f, rect.y + 147f, 420f, 38f), $"Current tier: {settlementTier}/3", headingStyle);
        }

        private static string WeaponName(WeaponDiscipline discipline)
        {
            return discipline switch
            {
                WeaponDiscipline.Pathblade => "Pathblade",
                WeaponDiscipline.Riftbow => "Riftbow",
                WeaponDiscipline.Threadstaff => "Threadstaff",
                _ => discipline.ToString()
            };
        }

        private static string ResourceLabel(ResourceKind kind)
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

        private static string DescribeCost(IEnumerable<ResourceCost> costs)
        {
            var parts = new List<string>();
            foreach (var cost in costs)
            {
                parts.Add($"{cost.amount} {ResourceLabel(cost.kind)}");
            }

            return string.Join(", ", parts);
        }
    }
}
