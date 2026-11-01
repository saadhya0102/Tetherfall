using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        public const string FenlightDrownedChoirPersistenceId = "encounter.morrowfen.drowned_choir@151:88";

        private readonly Dictionary<string, RestorationSite> restorationSites =
            new Dictionary<string, RestorationSite>();
        private readonly Dictionary<string, int> restorationSiteTiers =
            new Dictionary<string, int>();
        private bool fenlightPatrolCleared;
        private string activeRestorationSiteId;

        public int FenlightTier => GetRestorationTier(RestorationCatalog.FenlightCausewayId);

        public void RegisterRestorationSite(RestorationSite site)
        {
            if (site == null || site.Definition == null || string.IsNullOrEmpty(site.SiteId))
            {
                return;
            }

            restorationSites[site.SiteId] = site;
            if (!restorationSiteTiers.ContainsKey(site.SiteId))
            {
                restorationSiteTiers[site.SiteId] = 0;
            }
        }

        public void OpenRestorationSite(RestorationSite site)
        {
            if (site == null || site.Definition == null)
            {
                return;
            }

            activeRestorationSiteId = site.SiteId;
            OpenPanel(GamePanel.Restoration, site.Definition.displayName);
        }

        public int GetRestorationTier(string siteId)
        {
            return restorationSiteTiers.TryGetValue(siteId, out var tier) ? tier : 0;
        }

        public bool CanStartRouteEvent(RouteEventDefinition definition)
        {
            if (definition == null)
            {
                return true;
            }

            if (definition.requiresSignalbreakCompletion && !IsQuestComplete("signalbreak"))
            {
                return false;
            }

            return string.IsNullOrEmpty(definition.requiredRestorationSiteId) ||
                   GetRestorationTier(definition.requiredRestorationSiteId) >= definition.requiredRestorationTier;
        }

        public string GetRouteEventBlockedText(RouteEventDefinition definition)
        {
            if (definition == null || CanStartRouteEvent(definition))
            {
                return string.Empty;
            }

            if (definition.requiresSignalbreakCompletion && !IsQuestComplete("signalbreak"))
            {
                return "Stabilize the original route signals before opening this patrol.";
            }

            var site = RestorationCatalog.Get(definition.requiredRestorationSiteId);
            return site != null
                ? $"Restore {site.displayName} to tier {definition.requiredRestorationTier} first."
                : "This route signal is not ready to stabilize.";
        }

        public void OnFenlightPatrolCleared(EnemyCamp camp)
        {
            if (camp == null || camp.PersistenceId != FenlightDrownedChoirPersistenceId)
            {
                return;
            }

            fenlightPatrolCleared = true;
            ReconcileFenlightQuest();
            RequestSave();
        }

        public void OnRouteEventRestorationCompleted(RouteEvent routeEvent)
        {
            var definition = routeEvent != null ? routeEvent.Definition : null;
            if (definition == null || string.IsNullOrEmpty(definition.completionRestorationSiteId))
            {
                return;
            }

            SetRestorationTier(
                definition.completionRestorationSiteId,
                definition.completionRestorationTier,
                true);
        }

        private void TryRestoreActiveSite()
        {
            if (string.IsNullOrEmpty(activeRestorationSiteId) ||
                !restorationSites.TryGetValue(activeRestorationSiteId, out var site))
            {
                Notify("This restoration plan is unavailable.");
                return;
            }

            var nextTier = GetRestorationTier(site.SiteId) + 1;
            var tierDefinition = GetRestorationTierDefinition(site.Definition, nextTier);
            if (tierDefinition == null)
            {
                Notify($"{site.Definition.displayName} is fully restored.");
                return;
            }

            if (!tierDefinition.isManual)
            {
                Notify(tierDefinition.description);
                return;
            }

            if (!CanRestoreSite(site, tierDefinition, out var blockedReason))
            {
                Notify(blockedReason);
                return;
            }

            if (!CanAfford(tierDefinition.cost))
            {
                Notify($"Restoration needs {DescribeMissingCost(tierDefinition.cost)}.");
                return;
            }

            SpendCost(tierDefinition.cost);
            SetRestorationTier(site.SiteId, nextTier, true);
        }

        private bool CanRestoreSite(
            RestorationSite site,
            RestorationTierDefinition tierDefinition,
            out string blockedReason)
        {
            if (site.SiteId == RestorationCatalog.FenlightCausewayId)
            {
                if (!dungeonBossDefeated)
                {
                    blockedReason = "Defeat the Skyvault Warden before rebuilding the Fenlight Causeway.";
                    return false;
                }

                if (tierDefinition.tier == 1 && !fenlightPatrolCleared)
                {
                    blockedReason = "Clear the Drowned Choir Patrol before exposing the lantern chain.";
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(tierDefinition.requiredRouteEventId) &&
                !IsRouteEventCompleted(tierDefinition.requiredRouteEventId))
            {
                blockedReason = "The route signal must be stabilized before this work can continue.";
                return false;
            }

            blockedReason = string.Empty;
            return true;
        }

        private void SetRestorationTier(string siteId, int tier, bool awardRewards)
        {
            var definition = RestorationCatalog.Get(siteId);
            if (definition == null)
            {
                return;
            }

            var currentTier = GetRestorationTier(siteId);
            var targetTier = Mathf.Clamp(tier, 0, definition.maxTier);
            if (targetTier <= currentTier)
            {
                return;
            }

            restorationSiteTiers[siteId] = targetTier;
            restorationSites.TryGetValue(siteId, out var site);
            if (site != null)
            {
                site.RestoreToTier(targetTier);
            }

            if (awardRewards)
            {
                TetherfallWorldInteractionFeedback.PlayRestorationStage(
                    this,
                    site != null ? site.transform : null,
                    definition.position,
                    definition.accentColor,
                    targetTier);
                var tierDefinition = GetRestorationTierDefinition(definition, targetTier);
                if (tierDefinition != null)
                {
                    if (tierDefinition.experienceReward > 0)
                    {
                        GainExperience(tierDefinition.experienceReward);
                    }

                    if (tierDefinition.favorReward > 0)
                    {
                        AddFavors(tierDefinition.favorReward, $"{definition.displayName} restoration");
                    }

                    UnlockRestorationReward(tierDefinition.rewardEquipmentId);
                    Notify($"{definition.displayName}: {tierDefinition.title}.");
                }
            }

            ReconcileFenlightQuest();
            RequestSave();
        }

        private void UnlockRestorationReward(string equipmentId)
        {
            UnlockEquipmentReward(equipmentId);
        }

        internal bool UnlockEquipmentReward(string equipmentId)
        {
            if (string.IsNullOrEmpty(equipmentId))
            {
                return false;
            }

            var equipment = equipmentCatalog.Find(item => item.id == equipmentId);
            if (equipment == null || !unlockedEquipment.Add(equipmentId))
            {
                return false;
            }

            AutoEquipFieldGear(equipment);
            Notify($"{equipment.displayName} added to the field pack.");
            return true;
        }

        private bool IsRouteEventCompleted(string eventId)
        {
            return completedRouteEventIds.Contains(eventId);
        }

        private static RestorationTierDefinition GetRestorationTierDefinition(
            RestorationSiteDefinition definition,
            int tier)
        {
            foreach (var tierDefinition in definition.tiers)
            {
                if (tierDefinition.tier == tier)
                {
                    return tierDefinition;
                }
            }

            return null;
        }

        private string GetFenlightObjectiveText()
        {
            if (!dungeonBossDefeated)
            {
                return null;
            }

            if (!fenlightPatrolCleared)
            {
                return "Clear the Drowned Choir Patrol near Fenlight Causeway.";
            }

            if (FenlightTier == 0)
            {
                return "Rehang Fenlight's lantern chain at the Causeway.";
            }

            if (FenlightTier == 1 && !IsRouteEventCompleted("event.morrowfen.lantern_breach"))
            {
                return "Defend the Fenlight Lantern Breach.";
            }

            if (FenlightTier == 2)
            {
                return "Raise the Fenlight Causeway above the flooded route.";
            }

            return null;
        }

        private Transform GetFenlightObjectiveTarget()
        {
            if (!dungeonBossDefeated)
            {
                return null;
            }

            if (!fenlightPatrolCleared)
            {
                foreach (var camp in enemyCamps)
                {
                    if (camp != null && camp.PersistenceId == FenlightDrownedChoirPersistenceId)
                    {
                        return camp.transform;
                    }
                }

                return null;
            }

            if (FenlightTier == 1 && !IsRouteEventCompleted("event.morrowfen.lantern_breach"))
            {
                foreach (var routeEvent in routeEvents)
                {
                    if (routeEvent != null && routeEvent.EventId == "event.morrowfen.lantern_breach")
                    {
                        return routeEvent.transform;
                    }
                }
            }

            if (FenlightTier < 3 &&
                restorationSites.TryGetValue(RestorationCatalog.FenlightCausewayId, out var site))
            {
                return site.transform;
            }

            return null;
        }

        private void ReconcileFenlightQuest()
        {
            if (!dungeonBossDefeated)
            {
                return;
            }

            var quest = questLog.Find(record => record.id == "fenlight");
            if (quest == null || quest.completed)
            {
                return;
            }

            var desiredProgress = 0;
            if (fenlightPatrolCleared)
            {
                desiredProgress = 1;
            }

            if (FenlightTier >= 1)
            {
                desiredProgress = 2;
            }

            if (IsRouteEventCompleted("event.morrowfen.lantern_breach"))
            {
                desiredProgress = 3;
            }

            if (FenlightTier >= 3)
            {
                desiredProgress = 4;
            }

            if (desiredProgress > quest.current)
            {
                AdvanceQuest("fenlight", desiredProgress - quest.current);
            }
        }

        private void WriteRestorationSave(TetherfallSaveData data)
        {
            data.restorationSites.Clear();
            foreach (var pair in restorationSiteTiers)
            {
                data.restorationSites.Add(new RestorationSiteSaveData
                {
                    id = pair.Key,
                    tier = pair.Value
                });
            }

            data.fenlightPatrolCleared = fenlightPatrolCleared;
        }

        private void RestoreRestorationState(TetherfallSaveData data)
        {
            restorationSiteTiers.Clear();
            foreach (var savedSite in data.restorationSites)
            {
                var definition = savedSite != null ? RestorationCatalog.Get(savedSite.id) : null;
                if (definition == null)
                {
                    continue;
                }

                restorationSiteTiers[savedSite.id] = Mathf.Clamp(savedSite.tier, 0, definition.maxTier);
            }

            fenlightPatrolCleared = data.fenlightPatrolCleared ||
                                    data.firstClearedEncounterIds.Contains(FenlightDrownedChoirPersistenceId);
            if (IsRouteEventCompleted("event.morrowfen.lantern_breach"))
            {
                restorationSiteTiers[RestorationCatalog.FenlightCausewayId] =
                    Mathf.Max(GetRestorationTier(RestorationCatalog.FenlightCausewayId), 2);
            }

            foreach (var pair in restorationSites)
            {
                pair.Value.RestoreToTier(GetRestorationTier(pair.Key));
            }

            var quest = questLog.Find(record => record.id == "fenlight");
            if (quest != null && dungeonBossDefeated)
            {
                var restoredProgress = 0;
                if (fenlightPatrolCleared)
                {
                    restoredProgress = 1;
                }

                if (FenlightTier >= 1)
                {
                    restoredProgress = 2;
                }

                if (IsRouteEventCompleted("event.morrowfen.lantern_breach"))
                {
                    restoredProgress = 3;
                }

                if (FenlightTier >= 3)
                {
                    restoredProgress = 4;
                }

                quest.current = Mathf.Max(quest.current, restoredProgress);
                quest.completed = quest.completed || quest.IsComplete;
            }
        }

        private void DrawRestorationPanel(Rect rect)
        {
            if (string.IsNullOrEmpty(activeRestorationSiteId) ||
                !restorationSites.TryGetValue(activeRestorationSiteId, out var site))
            {
                GUI.Label(new Rect(rect.x + 26f, rect.y + 70f, 650f, 30f), "No restoration plan is available.", bodyStyle);
                return;
            }

            var definition = site.Definition;
            var currentTier = GetRestorationTier(site.SiteId);
            GUI.Label(new Rect(rect.x + 26f, rect.y + 70f, 670f, 50f), definition.description, bodyStyle);
            GUI.Label(
                new Rect(rect.x + 26f, rect.y + 130f, 260f, 26f),
                $"Current restoration: {currentTier}/{definition.maxTier}",
                headingStyle);

            var nextTier = GetRestorationTierDefinition(definition, currentTier + 1);
            if (nextTier == null)
            {
                GUI.Label(new Rect(rect.x + 26f, rect.y + 180f, 650f, 36f), $"{definition.displayName} is fully restored.", headingStyle);
                return;
            }

            GUI.Label(new Rect(rect.x + 26f, rect.y + 180f, 650f, 26f), nextTier.title, headingStyle);
            GUI.Label(new Rect(rect.x + 26f, rect.y + 211f, 650f, 44f), nextTier.description, bodyStyle);

            if (!nextTier.isManual)
            {
                GUI.Label(new Rect(rect.x + 26f, rect.y + 274f, 650f, 34f), "This tier is earned through the linked route event.", mutedStyle);
                return;
            }

            if (!CanRestoreSite(site, nextTier, out var blockedReason))
            {
                GUI.Label(new Rect(rect.x + 26f, rect.y + 274f, 650f, 34f), blockedReason, mutedStyle);
                return;
            }

            if (GUI.Button(new Rect(rect.x + 26f, rect.y + 274f, 220f, 40f), $"Restore tier {nextTier.tier}"))
            {
                TryRestoreActiveSite();
            }

            GUI.Label(
                new Rect(rect.x + 266f, rect.y + 279f, 450f, 34f),
                nextTier.cost.Count > 0 ? DescribeCost(nextTier.cost) : "No materials required",
                bodyStyle);
        }
    }

    public static class RestorationSiteBuilder
    {
        public static void Build(TetherfallGame game)
        {
            foreach (var definition in RestorationCatalog.All)
            {
                RestorationSite.Create(game, definition);
            }
        }
    }

    public sealed class RestorationSite : MonoBehaviour, IWorldInteractable
    {
        private readonly List<GameObject> tierPieces = new List<GameObject>();
        private TetherfallGame game;
        private Light routeLight;
        private int tier;

        public RestorationSiteDefinition Definition { get; private set; }
        public string SiteId => Definition != null ? Definition.id : string.Empty;
        public string InteractionPrompt => tier >= Definition.maxTier
            ? $"Visit restored {Definition.displayName}"
            : $"Plan {Definition.displayName} restoration";

        public static RestorationSite Create(TetherfallGame game, RestorationSiteDefinition definition)
        {
            var root = new GameObject(definition.displayName);
            root.transform.position = definition.position;
            var site = root.AddComponent<RestorationSite>();
            site.game = game;
            site.Definition = definition;
            site.CreateBaseVisuals();
            game.RegisterInteractable(site);
            game.RegisterRestorationSite(site);
            WorldVisuals.AddLabel(root.transform, definition.displayName, new Vector3(0f, 4.4f, 0f), definition.accentColor, 0.1f);
            return site;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            owner.OpenRestorationSite(this);
        }

        public void RestoreToTier(int restoredTier)
        {
            tier = Mathf.Clamp(restoredTier, 0, Definition.maxTier);
            while (tierPieces.Count < tier)
            {
                tierPieces.Add(CreateTierVisual(tierPieces.Count + 1));
            }

            for (var index = 0; index < tierPieces.Count; index++)
            {
                tierPieces[index].SetActive(index < tier);
            }

            if (routeLight != null)
            {
                routeLight.intensity = tier switch
                {
                    0 => 0.22f,
                    1 => 1.1f,
                    2 => 1.7f,
                    _ => 2.2f
                };
            }
        }

        private void CreateBaseVisuals()
        {
            var wreckage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wreckage.name = "Drowned causeway wreckage";
            wreckage.transform.SetParent(transform, false);
            wreckage.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            wreckage.transform.localScale = new Vector3(6.4f, 0.7f, 2.2f);
            wreckage.transform.localRotation = Quaternion.Euler(0f, -7f, 7f);
            wreckage.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.16f, 0.24f, 0.25f), 0.08f, 0.28f);

            var mooring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mooring.name = "Fenlight mooring";
            mooring.transform.SetParent(transform, false);
            mooring.transform.localPosition = new Vector3(-2.5f, 1.15f, 0.5f);
            mooring.transform.localScale = new Vector3(0.42f, 1.15f, 0.42f);
            mooring.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.25f, 0.18f, 0.12f), 0.05f, 0.3f);

            routeLight = gameObject.AddComponent<Light>();
            routeLight.type = LightType.Point;
            routeLight.range = 10f;
            routeLight.color = Definition.accentColor;
            routeLight.shadows = LightShadows.None;
            RestoreToTier(0);
        }

        private GameObject CreateTierVisual(int visualTier)
        {
            var root = new GameObject($"Fenlight restoration tier {visualTier}");
            root.transform.SetParent(transform, false);
            switch (visualTier)
            {
                case 1:
                    CreateLanternChain(root.transform);
                    break;
                case 2:
                    CreateStabilizedSignal(root.transform);
                    break;
                default:
                    CreateRaisedCauseway(root.transform);
                    break;
            }

            return root;
        }

        private void CreateLanternChain(Transform parent)
        {
            for (var index = 0; index < 3; index++)
            {
                var lantern = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lantern.name = "Fenlight lantern";
                lantern.transform.SetParent(parent, false);
                lantern.transform.localPosition = new Vector3(-2.1f + index * 2.1f, 1.45f, 0.65f);
                lantern.transform.localScale = new Vector3(0.26f, 0.65f, 0.26f);
                lantern.GetComponent<Renderer>().material = game.CreateGlowMaterial(Definition.accentColor, 1.4f, 0.55f);
                Object.Destroy(lantern.GetComponent<Collider>());

                var lightRoot = new GameObject("Fenlight lantern glow");
                lightRoot.transform.SetParent(lantern.transform, false);
                var lanternLight = lightRoot.AddComponent<Light>();
                lanternLight.type = LightType.Point;
                lanternLight.range = 4.5f;
                lanternLight.intensity = 0.8f;
                lanternLight.color = Definition.accentColor;
            }
        }

        private void CreateStabilizedSignal(Transform parent)
        {
            var signal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            signal.name = "Stable Fenlight relay";
            signal.transform.SetParent(parent, false);
            signal.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            signal.transform.localScale = Vector3.one * 0.92f;
            signal.GetComponent<Renderer>().material = game.CreateGlowMaterial(Definition.accentColor, 1.9f, 0.7f);
            Object.Destroy(signal.GetComponent<Collider>());
            signal.AddComponent<RouteGlow>().Initialize(0.08f, transform.position.x + transform.position.z);
        }

        private void CreateRaisedCauseway(Transform parent)
        {
            var walkway = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walkway.name = "Raised Fenlight walkway";
            walkway.transform.SetParent(parent, false);
            walkway.transform.localPosition = new Vector3(0f, 1.1f, -0.1f);
            walkway.transform.localScale = new Vector3(8.2f, 0.32f, 2.45f);
            walkway.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.39f, 0.25f, 0.14f), 0.18f, 0.42f);

            for (var index = 0; index < 2; index++)
            {
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = "Fenlight copper rail";
                rail.transform.SetParent(parent, false);
                rail.transform.localPosition = new Vector3(0f, 1.82f, index == 0 ? -1.08f : 1.08f);
                rail.transform.localScale = new Vector3(8.1f, 0.12f, 0.12f);
                rail.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.69f, 0.42f, 0.18f), 0.32f, 0.6f);
                Object.Destroy(rail.GetComponent<Collider>());
            }
        }
    }
}
