using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private const int EmberfallMilestone = 1;
        private const int CacheMilestone = 2;
        private const int AshjawMilestone = 4;

        private CinderwoldLedger cinderwoldLedger;
        private EmberRelayDais emberRelayDais;
        private bool cinderwoldArcAccepted;
        private int cinderwoldMilestoneMask;
        private string cinderwoldResolutionId;
        private bool cinderwoldRewardGranted;

        public void RegisterCinderwoldLedger(CinderwoldLedger ledger)
        {
            cinderwoldLedger = ledger;
        }

        public void RegisterEmberRelayDais(EmberRelayDais dais)
        {
            emberRelayDais = dais;
        }

        public void TryAcceptCinderwoldArc()
        {
            if (FenlightTier < 3)
            {
                Notify("Daro needs the Fenlight Causeway restored before he can trust the witness core to the road.");
                return;
            }

            if (cinderwoldArcAccepted)
            {
                Notify("The Ember Compact is already recorded in the field ledger.");
                return;
            }

            cinderwoldArcAccepted = true;
            cinderwoldLedger?.SetAccepted(true);
            ReconcileCinderwoldArc();
            Notify("The Ember Compact accepted. Gather the relay's three forms of witness.");
            RequestSave();
        }

        public void OpenCinderwoldResolution()
        {
            if (!cinderwoldArcAccepted)
            {
                Notify("Read Daro's witness ledger before handling the Ember Relay core.");
                return;
            }

            if (!HasCompletedCinderwoldMilestones())
            {
                Notify(GetCinderwoldMissingProofText());
                return;
            }

            if (!string.IsNullOrEmpty(cinderwoldResolutionId))
            {
                Notify("The Ember Compact has already been resolved.");
                return;
            }

            OpenPanel(GamePanel.CinderResolution, "The Ember Compact");
        }

        public void OnCinderwoldWorldStateChanged()
        {
            ReconcileCinderwoldArc();
        }

        private void ReconcileCinderwoldArc()
        {
            cinderwoldMilestoneMask |= GetLiveCinderwoldMilestoneMask();
            if (!cinderwoldArcAccepted)
            {
                return;
            }

            var quest = questLog.Find(record => record.id == CinderwoldArcCatalog.QuestId);
            if (quest == null || quest.completed)
            {
                return;
            }

            var desiredProgress = CountMilestones(cinderwoldMilestoneMask);
            if (!string.IsNullOrEmpty(cinderwoldResolutionId))
            {
                desiredProgress++;
            }

            if (desiredProgress > quest.current)
            {
                AdvanceQuest(CinderwoldArcCatalog.QuestId, desiredProgress - quest.current);
            }
        }

        private int GetLiveCinderwoldMilestoneMask()
        {
            var mask = 0;
            if (completedRouteEventIds.Contains(CinderwoldArcCatalog.EmberfallEventId))
            {
                mask |= EmberfallMilestone;
            }

            var allCachesRecovered = true;
            foreach (var cacheId in CinderwoldArcCatalog.RequiredCacheIds)
            {
                if (!recoveredFieldCacheIds.Contains(cacheId))
                {
                    allCachesRecovered = false;
                    break;
                }
            }

            if (allCachesRecovered)
            {
                mask |= CacheMilestone;
            }

            if (HasClearedMilestoneEncounter(CinderwoldArcCatalog.AshjawMilestoneId))
            {
                mask |= AshjawMilestone;
            }

            return mask;
        }

        private bool HasCompletedCinderwoldMilestones()
        {
            return (cinderwoldMilestoneMask & (EmberfallMilestone | CacheMilestone | AshjawMilestone)) ==
                   (EmberfallMilestone | CacheMilestone | AshjawMilestone);
        }

        private static int CountMilestones(int mask)
        {
            var count = 0;
            if ((mask & EmberfallMilestone) != 0)
            {
                count++;
            }

            if ((mask & CacheMilestone) != 0)
            {
                count++;
            }

            if ((mask & AshjawMilestone) != 0)
            {
                count++;
            }

            return count;
        }

        private string GetCinderwoldArcObjectiveText()
        {
            if (FenlightTier < 3 || !string.IsNullOrEmpty(cinderwoldResolutionId))
            {
                return null;
            }

            if (!cinderwoldArcAccepted)
            {
                return "Speak with Daro Quell about the Ember Relay witness core.";
            }

            if ((cinderwoldMilestoneMask & EmberfallMilestone) == 0)
            {
                return "Stabilize the Ember Relay Breach in Cinderwold.";
            }

            if ((cinderwoldMilestoneMask & CacheMilestone) == 0)
            {
                return "Recover all three Cinderwold witness caches.";
            }

            if ((cinderwoldMilestoneMask & AshjawMilestone) == 0)
            {
                return "Clear the Ashjaw Encampment at Blackravine.";
            }

            return "Return to the Ember Relay and choose its future.";
        }

        private Transform GetCinderwoldArcObjectiveTarget()
        {
            if (FenlightTier < 3 || !string.IsNullOrEmpty(cinderwoldResolutionId))
            {
                return null;
            }

            if (!cinderwoldArcAccepted)
            {
                return cinderwoldLedger != null ? cinderwoldLedger.transform : null;
            }

            if ((cinderwoldMilestoneMask & EmberfallMilestone) == 0)
            {
                foreach (var routeEvent in routeEvents)
                {
                    if (routeEvent != null && routeEvent.EventId == CinderwoldArcCatalog.EmberfallEventId)
                    {
                        return routeEvent.transform;
                    }
                }
            }

            if ((cinderwoldMilestoneMask & CacheMilestone) == 0)
            {
                foreach (var cacheId in CinderwoldArcCatalog.RequiredCacheIds)
                {
                    if (recoveredFieldCacheIds.Contains(cacheId))
                    {
                        continue;
                    }

                    foreach (var fieldCache in fieldCaches)
                    {
                        if (fieldCache != null && fieldCache.CacheId == cacheId)
                        {
                            return fieldCache.transform;
                        }
                    }
                }
            }

            if ((cinderwoldMilestoneMask & AshjawMilestone) == 0)
            {
                foreach (var camp in enemyCamps)
                {
                    if (camp != null && camp.PersistenceId == CinderwoldArcCatalog.AshjawMilestoneId)
                    {
                        return camp.transform;
                    }
                }
            }

            return emberRelayDais != null ? emberRelayDais.transform : null;
        }

        private string GetCinderwoldMissingProofText()
        {
            if ((cinderwoldMilestoneMask & EmberfallMilestone) == 0)
            {
                return "Stabilize the Ember Relay Breach first.";
            }

            if ((cinderwoldMilestoneMask & CacheMilestone) == 0)
            {
                return "Recover the three Cinderwold witness caches first.";
            }

            if ((cinderwoldMilestoneMask & AshjawMilestone) == 0)
            {
                return "Clear the Ashjaw Encampment first.";
            }

            return "The witness core is not ready.";
        }

        private void TryResolveCinderwoldArc(string resolutionId)
        {
            if (!string.IsNullOrEmpty(cinderwoldResolutionId) || !HasCompletedCinderwoldMilestones())
            {
                return;
            }

            var rewardEquipmentId = string.Empty;
            if (resolutionId == CinderwoldArcCatalog.RouteboundResolutionId)
            {
                if (!CanAfford(CinderwoldArcCatalog.RouteboundCost))
                {
                    Notify($"Routebound needs {DescribeMissingCost(CinderwoldArcCatalog.RouteboundCost)}.");
                    return;
                }

                SpendCost(CinderwoldArcCatalog.RouteboundCost);
                rewardEquipmentId = "charm.emberwatch";
            }
            else if (resolutionId == CinderwoldArcCatalog.AshboundResolutionId)
            {
                var cinderwing = FindPactCreature("Cinderwing");
                if (cinderwing == null || !cinderwing.IsPacted)
                {
                    Notify("Cinderwing must willingly join your Pact before the core can be released to the ash currents.");
                    return;
                }

                rewardEquipmentId = "tool.ashkite";
            }
            else
            {
                return;
            }

            cinderwoldResolutionId = resolutionId;
            GrantCinderwoldResolutionReward(rewardEquipmentId);
            emberRelayDais?.SetResolution(resolutionId);
            ReconcileCinderwoldArc();
            GainExperience(120);
            AddFavors(30, "the Ember Compact");
            Notify(
                resolutionId == CinderwoldArcCatalog.RouteboundResolutionId
                    ? "The relay is routebound. Its lanterns hold a safe line through the ash."
                    : "The core is ashbound. Cinderwing's route opens on the thermal currents.");
            ClosePanel();
            RequestSave();
        }

        private void GrantCinderwoldResolutionReward(string equipmentId)
        {
            if (cinderwoldRewardGranted)
            {
                return;
            }

            cinderwoldRewardGranted = true;
            var equipment = equipmentCatalog.Find(item => item.id == equipmentId);
            if (equipment == null)
            {
                return;
            }

            unlockedEquipment.Add(equipment.id);
            AutoEquipFieldGear(equipment);
            Notify($"{equipment.displayName} added to the field pack.");
        }

        private void WriteCinderwoldArcSave(TetherfallSaveData data)
        {
            data.cinderwoldArc.accepted = cinderwoldArcAccepted;
            data.cinderwoldArc.completedMilestoneMask = cinderwoldMilestoneMask;
            data.cinderwoldArc.resolutionId = cinderwoldResolutionId;
            data.cinderwoldArc.resolutionRewardGranted = cinderwoldRewardGranted;
        }

        private void RestoreCinderwoldArcState(TetherfallSaveData data)
        {
            cinderwoldArcAccepted = data.cinderwoldArc.accepted;
            cinderwoldMilestoneMask = data.cinderwoldArc.completedMilestoneMask | GetLiveCinderwoldMilestoneMask();
            cinderwoldResolutionId = data.cinderwoldArc.resolutionId == CinderwoldArcCatalog.RouteboundResolutionId ||
                                    data.cinderwoldArc.resolutionId == CinderwoldArcCatalog.AshboundResolutionId
                ? data.cinderwoldArc.resolutionId
                : string.Empty;
            cinderwoldRewardGranted = data.cinderwoldArc.resolutionRewardGranted;
            cinderwoldLedger?.SetAccepted(cinderwoldArcAccepted);
            emberRelayDais?.SetResolution(cinderwoldResolutionId);

            if (!string.IsNullOrEmpty(cinderwoldResolutionId))
            {
                var rewardEquipmentId = cinderwoldResolutionId == CinderwoldArcCatalog.RouteboundResolutionId
                    ? "charm.emberwatch"
                    : "tool.ashkite";
                var equipment = equipmentCatalog.Find(item => item.id == rewardEquipmentId);
                if (equipment != null)
                {
                    unlockedEquipment.Add(equipment.id);
                    AutoEquipFieldGear(equipment);
                }
            }

            if (cinderwoldArcAccepted)
            {
                var quest = questLog.Find(record => record.id == CinderwoldArcCatalog.QuestId);
                if (quest != null)
                {
                    var restoredProgress = CountMilestones(cinderwoldMilestoneMask) +
                                           (string.IsNullOrEmpty(cinderwoldResolutionId) ? 0 : 1);
                    quest.current = Mathf.Max(quest.current, restoredProgress);
                    quest.completed = quest.completed || quest.IsComplete;
                }
            }
        }

        private void DrawCinderwoldResolution(Rect rect)
        {
            GUI.Label(
                new Rect(rect.x + 26f, rect.y + 70f, 680f, 52f),
                "The witness core can bind the Ember Relay to travelers, or return its ember-memory to Cinderwing's migration route. This choice is permanent.",
                bodyStyle);

            if (GUI.Button(new Rect(rect.x + 26f, rect.y + 158f, 220f, 42f), "Routebound relay"))
            {
                TryResolveCinderwoldArc(CinderwoldArcCatalog.RouteboundResolutionId);
            }

            GUI.Label(
                new Rect(rect.x + 266f, rect.y + 160f, 470f, 48f),
                $"{DescribeCost(CinderwoldArcCatalog.RouteboundCost)}\nReward: Emberwatch Charm (+6 guard).",
                bodyStyle);

            if (GUI.Button(new Rect(rect.x + 26f, rect.y + 252f, 220f, 42f), "Ashbound release"))
            {
                TryResolveCinderwoldArc(CinderwoldArcCatalog.AshboundResolutionId);
            }

            GUI.Label(
                new Rect(rect.x + 266f, rect.y + 254f, 470f, 50f),
                "Requires a Pact with Cinderwing.\nReward: Ashkite (+18 stamina) and persistent thermal shortcuts.",
                bodyStyle);
        }
    }

    public static class CinderwoldArcBuilder
    {
        public static void Build(TetherfallGame game)
        {
            CinderwoldLedger.Create(game, CinderwoldArcCatalog.LedgerPosition);
            EmberRelayDais.Create(game, CinderwoldArcCatalog.DaisPosition);
        }
    }

    public sealed class CinderwoldLedger : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private Renderer pageRenderer;
        private bool accepted;

        public string InteractionPrompt => accepted
            ? "Review the Ember Compact"
            : "Read Daro's witness ledger";

        public static CinderwoldLedger Create(TetherfallGame game, Vector3 position)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "Daro's witness ledger";
            root.transform.position = position + new Vector3(0f, 0.5f, 0f);
            root.transform.localScale = new Vector3(0.92f, 1f, 0.72f);
            var ledger = root.AddComponent<CinderwoldLedger>();
            ledger.game = game;
            ledger.pageRenderer = root.GetComponent<Renderer>();
            ledger.pageRenderer.material = game.CreateMaterial(new Color(0.39f, 0.23f, 0.13f), 0.08f, 0.38f);

            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Witness core";
            core.transform.SetParent(root.transform, false);
            core.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            core.transform.localScale = Vector3.one * 0.34f;
            core.GetComponent<Renderer>().material = game.CreateGlowMaterial(new Color(0.94f, 0.38f, 0.19f), 1.2f, 0.6f);
            Object.Destroy(core.GetComponent<Collider>());
            WorldVisuals.AddLabel(root.transform, "Witness ledger", new Vector3(0f, 1.35f, 0f), new Color(0.96f, 0.61f, 0.36f), 0.09f);
            game.RegisterInteractable(ledger);
            game.RegisterCinderwoldLedger(ledger);
            return ledger;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            owner.TryAcceptCinderwoldArc();
        }

        public void SetAccepted(bool isAccepted)
        {
            accepted = isAccepted;
            if (pageRenderer != null)
            {
                pageRenderer.material.color = isAccepted
                    ? new Color(0.5f, 0.31f, 0.16f)
                    : new Color(0.39f, 0.23f, 0.13f);
            }
        }
    }

    public sealed class EmberRelayDais : MonoBehaviour, IWorldInteractable
    {
        private readonly List<GameObject> routeboundVisuals = new List<GameObject>();
        private readonly List<GameObject> ashboundVisuals = new List<GameObject>();
        private TetherfallGame game;
        private Renderer coreRenderer;
        private Light coreLight;

        public string InteractionPrompt => "Handle the Ember Relay witness core";

        public static EmberRelayDais Create(TetherfallGame game, Vector3 position)
        {
            var root = new GameObject("Ember Relay witness dais");
            root.transform.position = position;
            var dais = root.AddComponent<EmberRelayDais>();
            dais.game = game;
            dais.CreateVisuals();
            game.RegisterInteractable(dais);
            game.RegisterEmberRelayDais(dais);
            WorldVisuals.AddLabel(root.transform, "Ember Relay witness dais", new Vector3(0f, 4f, 0f), new Color(0.98f, 0.52f, 0.27f), 0.09f);
            return dais;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return true;
        }

        public void Interact(TetherfallGame owner)
        {
            owner.OpenCinderwoldResolution();
        }

        public void SetResolution(string resolutionId)
        {
            var routebound = resolutionId == CinderwoldArcCatalog.RouteboundResolutionId;
            var ashbound = resolutionId == CinderwoldArcCatalog.AshboundResolutionId;
            game.SetCinderwoldRelayResolution(resolutionId);
            foreach (var visual in routeboundVisuals)
            {
                visual.SetActive(routebound);
            }

            foreach (var visual in ashboundVisuals)
            {
                visual.SetActive(ashbound);
            }

            if (coreRenderer != null)
            {
                coreRenderer.material.color = routebound
                    ? new Color(0.98f, 0.66f, 0.28f)
                    : ashbound
                        ? new Color(0.96f, 0.3f, 0.15f)
                        : new Color(0.47f, 0.14f, 0.12f);
            }

            if (coreLight != null)
            {
                coreLight.intensity = routebound ? 2.4f : ashbound ? 1.8f : 0.7f;
            }
        }

        private void CreateVisuals()
        {
            var baseStone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseStone.name = "Ember relay dais base";
            baseStone.transform.SetParent(transform, false);
            baseStone.transform.localPosition = Vector3.up * 0.75f;
            baseStone.transform.localScale = new Vector3(2.1f, 0.75f, 2.1f);
            baseStone.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.23f, 0.14f, 0.12f), 0.12f, 0.36f);

            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Ember witness core";
            core.transform.SetParent(transform, false);
            core.transform.localPosition = Vector3.up * 2.1f;
            core.transform.localScale = Vector3.one * 0.82f;
            coreRenderer = core.GetComponent<Renderer>();
            coreRenderer.material = game.CreateGlowMaterial(new Color(0.47f, 0.14f, 0.12f), 1.4f, 0.64f);
            Object.Destroy(core.GetComponent<Collider>());

            coreLight = gameObject.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.range = 10f;
            coreLight.color = new Color(0.98f, 0.4f, 0.18f);
            coreLight.shadows = LightShadows.None;

            CreateRouteboundVisuals();
            CreateAshboundVisuals();
            SetResolution(string.Empty);
        }

        private void CreateRouteboundVisuals()
        {
            for (var index = 0; index < 3; index++)
            {
                var lantern = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lantern.name = "Routebound Ember Lantern";
                lantern.transform.SetParent(transform, false);
                lantern.transform.localPosition = new Vector3(-3f + index * 3f, 1.7f, 1.65f);
                lantern.transform.localScale = new Vector3(0.22f, 0.72f, 0.22f);
                lantern.GetComponent<Renderer>().material = game.CreateGlowMaterial(new Color(0.98f, 0.57f, 0.22f), 1.5f, 0.62f);
                Object.Destroy(lantern.GetComponent<Collider>());
                routeboundVisuals.Add(lantern);
            }
        }

        private void CreateAshboundVisuals()
        {
            var positions = new[]
            {
                new Vector3(-146f, 2.5f, 40f),
                new Vector3(-171f, 2.5f, 68f)
            };
            foreach (var position in positions)
            {
                var lift = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lift.name = "Ashbound thermal current";
                lift.transform.position = position;
                lift.transform.localScale = new Vector3(2f, 2.5f, 2f);
                lift.GetComponent<Renderer>().material = game.CreateTransparentMaterial(
                    new Color(0.96f, 0.38f, 0.17f, 0.28f),
                    0.58f);
                lift.AddComponent<WindLift>();
                lift.AddComponent<RouteGlow>().Initialize(0.08f, position.x + position.z);
                WorldVisuals.AddLabel(lift.transform, "Ashbound current", new Vector3(0f, 3.2f, 0f), new Color(0.98f, 0.6f, 0.32f), 0.08f);
                ashboundVisuals.Add(lift);
            }
        }
    }
}
