using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private readonly Dictionary<string, ThreadwayAnchor> threadways =
            new Dictionary<string, ThreadwayAnchor>();
        private readonly HashSet<string> unlockedThreadwayIds = new HashSet<string>();
        private ThreadwayAnchor activeThreadway;

        internal void RegisterThreadway(ThreadwayAnchor anchor)
        {
            if (anchor == null || anchor.Definition == null)
            {
                return;
            }

            threadways[anchor.Definition.id] = anchor;
        }

        internal bool IsThreadwayUnlocked(string id)
        {
            return !string.IsNullOrEmpty(id) && unlockedThreadwayIds.Contains(id);
        }

        internal void OpenThreadway(ThreadwayAnchor anchor)
        {
            if (anchor == null || anchor.Definition == null)
            {
                return;
            }

            if (!IsThreadwayUnlocked(anchor.Definition.id))
            {
                TryRestoreThreadway(anchor);
                return;
            }

            activeThreadway = anchor;
            OpenPanel(GamePanel.Threadways, $"{anchor.Definition.title} routes");
            OpenWorldMapUi();
        }

        public void OpenWorldMapFromField()
        {
            activeThreadway = null;
            if (PlayerTransform != null)
            {
                var closestDistance = float.MaxValue;
                foreach (var pair in threadways)
                {
                    var anchor = pair.Value;
                    if (anchor == null || !IsThreadwayUnlocked(pair.Key))
                    {
                        continue;
                    }

                    var offset = anchor.transform.position - PlayerTransform.position;
                    offset.y = 0f;
                    var distance = offset.magnitude;
                    if (distance <= WorldMapCurrentSourceRange && distance < closestDistance)
                    {
                        closestDistance = distance;
                        activeThreadway = anchor;
                    }
                }
            }

            OpenPanel(
                GamePanel.Threadways,
                activeThreadway != null
                    ? $"{activeThreadway.Definition.title} routes"
                    : "Threadglass world map");
            OpenWorldMapUi();
        }

        private void TryRestoreThreadway(ThreadwayAnchor anchor)
        {
            var definition = anchor != null ? anchor.Definition : null;
            if (definition == null || IsThreadwayUnlocked(definition.id))
            {
                return;
            }

            if (Enemy.FindClosest(PlayerTransform.position, 18f) != null)
            {
                Notify("Hostiles are too close to stabilize a Threadway.");
                return;
            }

            if (definition.activationChimesaltCost > 0 &&
                !SpendResources(0, 0, 0, definition.activationChimesaltCost))
            {
                Notify($"{definition.title} needs {definition.activationChimesaltCost} Chimesalt to awaken.");
                return;
            }

            if (!unlockedThreadwayIds.Add(definition.id))
            {
                return;
            }

            anchor.SetUnlocked(true);
            activeThreadway = anchor;
            TetherfallWorldInteractionFeedback.PlayThreadwayActivation(
                this,
                anchor.transform,
                definition.accentColor);
            AddFavors(4, definition.title);
            GainExperience(26);
            AdvanceQuest("threadways", 1);
            Notify($"{definition.title} is restored. Its route now answers across Tetherfall.");
            RequestSave();
        }

        private void TryTravelThreadway(ThreadwayDefinition destination)
        {
            if (destination == null || !IsThreadwayUnlocked(destination.id))
            {
                return;
            }

            if (Player == null)
            {
                return;
            }

            if (Enemy.FindClosest(PlayerTransform.position, 18f) != null)
            {
                Notify("Hostiles are too close to thread a safe route.");
                return;
            }

            if (activeThreadway != null && activeThreadway.Definition.id == destination.id)
            {
                Notify("You are already standing at this Threadway.");
                return;
            }

            if (activeThreadway != null)
            {
                TetherfallWorldInteractionFeedback.PlayThreadwayTravel(
                    this,
                    activeThreadway.transform,
                    destination.arrivalPosition,
                    destination.accentColor);
            }

            TetherfallRegionalAnnexes.PrepareArrival(destination.arrivalPosition);
            Player.Teleport(destination.arrivalPosition);
            RecallExpeditionToThreadway();
            ClearCombatTarget();
            ClosePanel();
            Notify($"Threadway carried you to {destination.title}.");
            RequestSave();
        }

        private void RecallExpeditionToThreadway()
        {
            if (PlayerTransform == null)
            {
                return;
            }

            RecallPactRosterToFormation();
            foreach (var pact in pacts)
            {
                if (pact == null || !pact.IsPacted || !pact.IsMount)
                {
                    continue;
                }

                pact.transform.position = PlayerTransform.position +
                                          PlayerTransform.right * -2.35f -
                                          PlayerTransform.forward * 2.75f;
            }

            var companionIndex = 0;
            foreach (var companion in party)
            {
                if (companion == null || !companion.IsRecruited || companion.IsReserve)
                {
                    continue;
                }

                var lateral = companionIndex++ % 2 == 0 ? 1f : -1f;
                companion.transform.position = PlayerTransform.position +
                                               PlayerTransform.right * lateral * 1.45f -
                                               PlayerTransform.forward * 1.55f;
            }
        }

        private void DrawThreadwayPanel(Rect rect)
        {
            DrawWorldMapPanel(rect);
        }

        private void WriteThreadwaySave(TetherfallSaveData data)
        {
            data.unlockedThreadwayIds.Clear();
            foreach (var id in unlockedThreadwayIds)
            {
                data.unlockedThreadwayIds.Add(id);
            }
        }

        private void RestoreThreadwayState(TetherfallSaveData data)
        {
            unlockedThreadwayIds.Clear();
            if (data.unlockedThreadwayIds == null)
            {
                SeedWorldMapDiscoveryFromRestoredThreadways();
                return;
            }

            foreach (var id in data.unlockedThreadwayIds)
            {
                if (ThreadwayCatalog.Get(id) == null)
                {
                    continue;
                }

                unlockedThreadwayIds.Add(id);
                if (threadways.TryGetValue(id, out var anchor))
                {
                    anchor.SetUnlocked(true);
                }
            }

            SeedWorldMapDiscoveryFromRestoredThreadways();
        }
    }

    public static class ThreadwayBuilder
    {
        public static void Build(TetherfallGame game)
        {
            foreach (var definition in ThreadwayCatalog.All)
            {
                var anchor = ThreadwayAnchor.Create(game, definition);
                TetherfallRegionalAnnexes.AttachThreadwayShell(anchor);
            }
        }
    }

    public sealed class ThreadwayAnchor : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private Renderer coreRenderer;
        private Renderer ringRenderer;
        private Light beaconLight;
        private bool unlocked;
        private bool visualStateInitialized;

        public ThreadwayDefinition Definition { get; private set; }
        public string InteractionPrompt => unlocked
            ? $"Use {Definition.title} Threadway"
            : Definition.activationChimesaltCost > 0
                ? $"Restore {Definition.title} ({Definition.activationChimesaltCost} Chimesalt)"
                : $"Restore {Definition.title}";

        public static ThreadwayAnchor Create(TetherfallGame game, ThreadwayDefinition definition)
        {
            var root = new GameObject(definition.title);
            root.transform.position = definition.position;
            var anchor = root.AddComponent<ThreadwayAnchor>();
            anchor.Initialize(game, definition);
            return anchor;
        }

        public void Initialize(TetherfallGame owner, ThreadwayDefinition definition)
        {
            game = owner;
            Definition = definition;

            var foundation = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            foundation.name = "Threadway stone foundation";
            foundation.transform.SetParent(transform, false);
            foundation.transform.localPosition = Vector3.up * 0.2f;
            foundation.transform.localScale = new Vector3(1.65f, 0.2f, 1.65f);
            foundation.GetComponent<Renderer>().material = game.CreateMaterial(
                Color.Lerp(definition.accentColor, Color.black, 0.72f),
                0.06f,
                0.28f);

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Threadway resonant ring";
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = Vector3.up * 0.42f;
            ring.transform.localScale = new Vector3(1.32f, 0.055f, 1.32f);
            ringRenderer = ring.GetComponent<Renderer>();
            UnityEngine.Object.Destroy(ring.GetComponent<Collider>());
            ring.AddComponent<RouteGlow>().Initialize(0.06f, definition.position.x - definition.position.z);

            var core = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            core.name = "Threadway core";
            core.transform.SetParent(transform, false);
            core.transform.localPosition = Vector3.up * 1.45f;
            core.transform.localScale = new Vector3(0.56f, 1.45f, 0.56f);
            coreRenderer = core.GetComponent<Renderer>();
            UnityEngine.Object.Destroy(core.GetComponent<Collider>());
            core.AddComponent<RouteGlow>().Initialize(0.08f, definition.position.x + definition.position.z);

            for (var index = 0; index < 3; index++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Threadway orbit shard";
                shard.transform.SetParent(transform, false);
                var angle = index * 120f * Mathf.Deg2Rad;
                shard.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 1.12f,
                    1.2f + index * 0.12f,
                    Mathf.Sin(angle) * 1.12f);
                shard.transform.localScale = new Vector3(0.18f, 0.6f, 0.18f);
                shard.transform.localRotation = Quaternion.Euler(18f, index * 120f, 24f);
                shard.GetComponent<Renderer>().material = game.CreateMaterial(
                    Color.Lerp(definition.accentColor, Color.white, 0.24f),
                    0.18f,
                    0.65f);
                UnityEngine.Object.Destroy(shard.GetComponent<Collider>());
                FoliageSway.Attach(shard, 8f + index * 1.4f);
            }

            beaconLight = gameObject.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.range = 7f;
            beaconLight.color = definition.accentColor;
            WorldVisuals.AddLabel(
                transform,
                definition.title,
                new Vector3(0f, 3.55f, 0f),
                Color.Lerp(definition.accentColor, Color.white, 0.3f),
                0.09f);
            SetUnlocked(false);
            game.RegisterInteractable(this);
            game.RegisterThreadway(this);
        }

        public void SetUnlocked(bool value)
        {
            if (visualStateInitialized && unlocked == value)
            {
                return;
            }

            unlocked = value;
            var color = value
                ? Definition.accentColor
                : Color.Lerp(Definition.accentColor, Color.black, 0.74f);
            if (coreRenderer != null)
            {
                coreRenderer.sharedMaterial = value
                    ? RuntimeEffectMaterialCache.GetGlow(game, color, 1.35f, 0.76f)
                    : RuntimeEffectMaterialCache.GetOpaque(game, color, 0.02f, 0.18f);
            }

            if (ringRenderer != null)
            {
                var ringColor = Color.Lerp(color, Color.white, value ? 0.22f : 0f);
                ringRenderer.sharedMaterial = value
                    ? RuntimeEffectMaterialCache.GetGlow(game, ringColor, 1.18f, 0.68f)
                    : RuntimeEffectMaterialCache.GetOpaque(game, ringColor, 0.02f, 0.12f);
            }

            if (beaconLight != null)
            {
                beaconLight.intensity = value ? 1.55f : 0.15f;
            }

            visualStateInitialized = true;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null && owner.PlayerTransform != null;
        }

        public void Interact(TetherfallGame owner)
        {
            owner.OpenThreadway(this);
        }
    }
}
