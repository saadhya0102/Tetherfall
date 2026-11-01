using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed class BountyProgress
    {
        public int completionCount;
        public long nextAvailableUtcTicks;
    }

    public sealed partial class TetherfallGame
    {
        private readonly Dictionary<string, BountyProgress> bountyProgress =
            new Dictionary<string, BountyProgress>();
        private BountyDirector bountyDirector;
        private string activeBountyId;
        private bool cityBoardShowingBounties;

        internal void RegisterBountyDirector(BountyDirector director)
        {
            bountyDirector = director;
        }

        internal void SetCityBoardBountyView(bool showingBounties)
        {
            cityBoardShowingBounties = showingBounties;
        }

        internal bool IsCityBoardShowingBounties => cityBoardShowingBounties;

        internal void TryAcceptBounty(BountyDefinition bounty)
        {
            if (bounty == null)
            {
                return;
            }

            if (!HasBountyExpedition())
            {
                Notify("Form a Pact and recruit a field companion before accepting an elite hunt.");
                return;
            }

            if (RequiresAshguardCounterweave(bounty) && !HasCounterweaveLoadout())
            {
                Notify("Cindermaw's Ashguard requires two prepared weapon disciplines for a Crossweave response.");
                return;
            }

            if (!string.IsNullOrEmpty(activeBountyId))
            {
                if (activeBountyId == bounty.id)
                {
                    Notify($"{bounty.title} is already marked in your Threadglass.");
                }
                else
                {
                    Notify("Resolve your active bounty before accepting another elite hunt.");
                }

                return;
            }

            if (!IsBountyAvailable(bounty))
            {
                Notify($"{bounty.title} returns {GetBountyCooldownText(bounty)}.");
                return;
            }

            if (bountyDirector == null || !bountyDirector.Activate(bounty.id))
            {
                Notify("The bounty route could not be prepared.");
                return;
            }

            activeBountyId = bounty.id;
            Notify($"{bounty.title} accepted. Threadglass now marks the hunt.");
            RequestSave();
        }

        internal void OnBountyTargetDefeated(BountyDefinition bounty)
        {
            if (bounty == null || activeBountyId != bounty.id)
            {
                return;
            }

            var progress = GetBountyProgress(bounty.id);
            var firstCompletion = progress.completionCount == 0;
            progress.completionCount++;
            progress.nextAvailableUtcTicks = DateTime.UtcNow.AddSeconds(
                Mathf.Max(0f, bounty.cooldownSeconds)).Ticks;
            activeBountyId = string.Empty;
            bountyDirector?.ClearActive(bounty.id, false);

            if (bounty.favorReward > 0)
            {
                AddFavors(bounty.favorReward, bounty.title);
            }

            if (bounty.experienceReward > 0)
            {
                GainExperience(bounty.experienceReward);
            }

            foreach (var reward in bounty.materialReward)
            {
                AddResource(reward.kind, reward.amount);
            }

            if (firstCompletion)
            {
                AdvanceQuest("bounties", 1);
            }

            Notify($"{bounty.title} is resolved. Its route can breathe again.");
            RequestSave();
        }

        internal bool IsBountyAvailable(BountyDefinition bounty)
        {
            return bounty != null &&
                   GetBountyProgress(bounty.id).nextAvailableUtcTicks <= DateTime.UtcNow.Ticks;
        }

        internal bool IsBountyActive(BountyDefinition bounty)
        {
            return bounty != null && activeBountyId == bounty.id;
        }

        internal string GetBountyCooldownText(BountyDefinition bounty)
        {
            if (bounty == null)
            {
                return "later";
            }

            var ticks = GetBountyProgress(bounty.id).nextAvailableUtcTicks;
            if (ticks <= DateTime.UtcNow.Ticks)
            {
                return "now";
            }

            var remaining = new TimeSpan(ticks - DateTime.UtcNow.Ticks);
            return remaining.TotalMinutes >= 1d
                ? $"in {Mathf.CeilToInt((float)remaining.TotalMinutes)} min"
                : $"in {Mathf.Max(1, Mathf.CeilToInt((float)remaining.TotalSeconds))} sec";
        }

        internal string GetActiveBountyObjectiveText()
        {
            var bounty = BountyCatalog.Get(activeBountyId);
            return bounty == null ? string.Empty : $"Resolve the {bounty.title} elite bounty.";
        }

        internal Transform GetActiveBountyTarget()
        {
            return string.IsNullOrEmpty(activeBountyId) ? null : bountyDirector?.GetTarget(activeBountyId);
        }

        private BountyProgress GetBountyProgress(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new BountyProgress();
            }

            if (!bountyProgress.TryGetValue(id, out var progress))
            {
                progress = new BountyProgress();
                bountyProgress[id] = progress;
            }

            return progress;
        }

        private void DrawCityBounties(Rect rect, CityDirector city)
        {
            GUI.Label(
                new Rect(rect.x + 26f, rect.y + 142f, 680f, 24f),
                "Only one hunt can be active. Elite routes reset after a real-time cooldown.",
                mutedStyle);
            var y = rect.y + 180f;
            var expeditionReady = HasBountyExpedition();
            var anotherHuntActive = !string.IsNullOrEmpty(activeBountyId);
            foreach (var bounty in BountyCatalog.All)
            {
                if (bounty.sponsorCityId != city.Definition.id)
                {
                    continue;
                }

                var active = IsBountyActive(bounty);
                var available = IsBountyAvailable(bounty);
                var counterweaveReady = !RequiresAshguardCounterweave(bounty) || HasCounterweaveLoadout();
                var previousEnabled = GUI.enabled;
                GUI.enabled = active || (available && !anotherHuntActive && expeditionReady && counterweaveReady);
                var buttonText = active
                    ? "Tracked"
                    : !expeditionReady
                        ? "Need party"
                        : !counterweaveReady
                            ? "Need 2 weapons"
                        : anotherHuntActive
                            ? "Busy"
                            : available
                                ? "Accept"
                                : "Cooldown";
                if (GUI.Button(new Rect(rect.x + 26f, y, 128f, 34f), buttonText))
                {
                    TryAcceptBounty(bounty);
                }

                GUI.enabled = previousEnabled;
                var status = active
                    ? "ACTIVE"
                    : !expeditionReady
                        ? "FORM A PACT + RECRUIT A COMPANION"
                        : !counterweaveReady
                            ? "PREPARE TWO WEAPON DISCIPLINES"
                        : anotherHuntActive
                            ? "RESOLVE YOUR ACTIVE HUNT"
                    : available
                        ? $"{bounty.difficulty}  +{bounty.favorReward} Favors"
                        : $"Returns {GetBountyCooldownText(bounty)}";
                GUI.Label(
                    new Rect(rect.x + 170f, y - 2f, 536f, 46f),
                    $"{bounty.title}  {status}\n{bounty.description}",
                    active || available ? bodyStyle : mutedStyle);
                y += 96f;
            }
        }

        private bool HasBountyExpedition()
        {
            return ActiveCombatPactCount > 0 && party.Count > 0;
        }

        private static bool RequiresAshguardCounterweave(BountyDefinition bounty)
        {
            return bounty != null &&
                   EnemyCatalog.Get(bounty.targetEnemyId).ashguardDuringShockwave;
        }

        private bool HasCounterweaveLoadout()
        {
            var unlockedWeapons = 0;
            foreach (WeaponDiscipline discipline in System.Enum.GetValues(typeof(WeaponDiscipline)))
            {
                if (IsWeaponUnlocked(discipline))
                {
                    unlockedWeapons++;
                }
            }

            return unlockedWeapons >= 2;
        }

        private void WriteBountySave(TetherfallSaveData data)
        {
            data.bounties.Clear();
            data.activeBountyId = activeBountyId;
            foreach (var pair in bountyProgress)
            {
                data.bounties.Add(new BountySaveData
                {
                    id = pair.Key,
                    completionCount = pair.Value.completionCount,
                    nextAvailableUtcTicks = pair.Value.nextAvailableUtcTicks
                });
            }
        }

        private void RestoreBountyState(TetherfallSaveData data)
        {
            bountyProgress.Clear();
            if (data.bounties != null)
            {
                foreach (var savedBounty in data.bounties)
                {
                    if (savedBounty == null || string.IsNullOrEmpty(savedBounty.id))
                    {
                        continue;
                    }

                    bountyProgress[savedBounty.id] = new BountyProgress
                    {
                        completionCount = Mathf.Max(0, savedBounty.completionCount),
                        nextAvailableUtcTicks = Math.Max(0L, savedBounty.nextAvailableUtcTicks)
                    };
                }
            }

            activeBountyId = BountyCatalog.Get(data.activeBountyId) != null ? data.activeBountyId : string.Empty;
            if (!string.IsNullOrEmpty(activeBountyId))
            {
                bountyDirector?.Activate(activeBountyId);
            }
        }
    }

    public static class BountyBuilder
    {
        public static void Build(TetherfallGame game)
        {
            var root = new GameObject("City Bounty Director");
            var director = root.AddComponent<BountyDirector>();
            director.Initialize(game);
        }
    }

    public sealed class BountyDirector : MonoBehaviour
    {
        private readonly Dictionary<string, BountyHunt> hunts = new Dictionary<string, BountyHunt>();
        private TetherfallGame game;

        public void Initialize(TetherfallGame owner)
        {
            game = owner;
            foreach (var definition in BountyCatalog.All)
            {
                var huntRoot = new GameObject(definition.title);
                var hunt = huntRoot.AddComponent<BountyHunt>();
                hunt.Initialize(game, definition);
                hunts[definition.id] = hunt;
            }

            game.RegisterBountyDirector(this);
        }

        public bool Activate(string id)
        {
            if (!hunts.TryGetValue(id, out var hunt))
            {
                return false;
            }

            return hunt.Activate();
        }

        public void ClearActive(string id, bool destroyTarget = true)
        {
            if (hunts.TryGetValue(id, out var hunt))
            {
                hunt.Deactivate(destroyTarget);
            }
        }

        public Transform GetTarget(string id)
        {
            return hunts.TryGetValue(id, out var hunt) ? hunt.TargetTransform : null;
        }
    }

    public sealed class BountyHunt : MonoBehaviour
    {
        private TetherfallGame game;
        private BountyDefinition definition;
        private Enemy target;
        private GameObject marker;
        private readonly List<Enemy> reinforcements = new List<Enemy>();
        private bool phaseTriggered;

        public Transform TargetTransform => target != null ? target.transform : marker != null && marker.activeSelf ? marker.transform : null;

        public void Initialize(TetherfallGame owner, BountyDefinition bountyDefinition)
        {
            game = owner;
            definition = bountyDefinition;
            marker = CreateMarker();
            marker.SetActive(false);
        }

        public bool Activate()
        {
            if (target != null)
            {
                return true;
            }

            marker.SetActive(true);
            phaseTriggered = false;
            ClearReinforcements();
            target = Enemy.Create(game, null, definition.targetEnemyId, definition.position, OnTargetDefeated);
            if (target != null)
            {
                BountyVisualBuilder.DressTarget(game, target.transform, definition);
            }

            return target != null;
        }

        private void Update()
        {
            if (target == null || phaseTriggered || target.IsDefeated || target.MaxHealth <= 0)
            {
                return;
            }

            if (target.Health > target.MaxHealth * 0.5f)
            {
                return;
            }

            phaseTriggered = true;
            BountyVisualBuilder.PlayPhaseShift(game, target.transform, definition);
            SpawnReinforcements();
            game.Notify($"{definition.title}: {definition.phaseTitle}.");
        }

        public void Deactivate(bool destroyTarget)
        {
            marker.SetActive(false);
            if (destroyTarget && target != null)
            {
                UnityEngine.Object.Destroy(target.gameObject);
            }

            target = null;
            phaseTriggered = false;
            ClearReinforcements();
        }

        private void OnTargetDefeated(Enemy defeatedTarget)
        {
            target = null;
            marker.SetActive(false);
            game.OnBountyTargetDefeated(definition);
        }

        private void SpawnReinforcements()
        {
            if (definition.reinforcementEnemyIds == null || definition.reinforcementEnemyIds.Length == 0)
            {
                return;
            }

            for (var index = 0; index < definition.reinforcementEnemyIds.Length; index++)
            {
                var angle = index * Mathf.PI * 2f / definition.reinforcementEnemyIds.Length + 0.5f;
                var position = target.transform.position + new Vector3(
                    Mathf.Cos(angle) * 4.1f,
                    0.15f,
                    Mathf.Sin(angle) * 4.1f);
                var reinforcement = Enemy.Create(
                    game,
                    null,
                    definition.reinforcementEnemyIds[index],
                    position);
                if (reinforcement != null)
                {
                    reinforcements.Add(reinforcement);
                }
            }
        }

        private void ClearReinforcements()
        {
            foreach (var reinforcement in reinforcements)
            {
                if (reinforcement != null)
                {
                    UnityEngine.Object.Destroy(reinforcement.gameObject);
                }
            }

            reinforcements.Clear();
        }

        private GameObject CreateMarker()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = $"{definition.title} bounty signal";
            root.transform.position = definition.position + Vector3.up * 0.9f;
            root.transform.localScale = new Vector3(1.55f, 0.9f, 1.55f);
            root.GetComponent<Renderer>().material = game.CreateMaterial(definition.accentColor, 0.2f, 0.72f);
            UnityEngine.Object.Destroy(root.GetComponent<Collider>());
            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = definition.accentColor;
            light.range = 7f;
            light.intensity = 1.4f;
            WorldVisuals.AddLabel(root.transform, "Bounty route", new Vector3(0f, 1.5f, 0f), definition.accentColor, 0.07f);
            return root;
        }
    }

    public static class BountyVisualBuilder
    {
        public static void PlayPhaseShift(TetherfallGame game, Transform target, BountyDefinition definition)
        {
            if (game == null || target == null || definition == null)
            {
                return;
            }

            var accent = definition.accentColor;
            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                "Elite bounty phase signal",
                target.position + Vector3.up * 0.08f,
                new Vector3(0.42f, 0.04f, 0.42f),
                RuntimeEffectMaterialCache.GetTransparent(
                    game,
                    new Color(accent.r, accent.g, accent.b, 0.68f),
                    0.72f),
                0.36f,
                8.4f);
            CombatFeedback.CreateImpact(game, target.position + Vector3.up * 1.15f, accent, 1.1f);
        }

        public static void DressTarget(TetherfallGame game, Transform root, BountyDefinition definition)
        {
            var crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "Elite bounty crown";
            crown.transform.SetParent(root, false);
            crown.transform.localPosition = new Vector3(0f, 1.18f, 0f);
            crown.transform.localScale = new Vector3(0.72f, 0.08f, 0.72f);
            crown.GetComponent<Renderer>().material = game.CreateMaterial(definition.accentColor, 0.25f, 0.8f);
            UnityEngine.Object.Destroy(crown.GetComponent<Collider>());

            for (var index = 0; index < 3; index++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Elite bounty shard";
                shard.transform.SetParent(root, false);
                var angle = index * 120f * Mathf.Deg2Rad;
                shard.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.86f, 0.58f, Mathf.Sin(angle) * 0.86f);
                shard.transform.localScale = new Vector3(0.18f, 0.52f, 0.18f);
                shard.transform.localRotation = Quaternion.Euler(24f, index * 120f, 26f);
                shard.GetComponent<Renderer>().material = game.CreateMaterial(
                    Color.Lerp(definition.accentColor, Color.white, 0.32f),
                    0.25f,
                    0.75f);
                UnityEngine.Object.Destroy(shard.GetComponent<Collider>());
            }
        }
    }
}
