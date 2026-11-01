using System.Collections.Generic;
using UnityEngine;
using DateTime = System.DateTime;
using TimeSpan = System.TimeSpan;

namespace Tetherfall
{
    public static class RouteEventBuilder
    {
        public static void Build(TetherfallGame game)
        {
            foreach (var definition in RouteEventCatalog.All)
            {
                RouteEvent.Create(game, definition);
            }
        }
    }

    public sealed class RouteEventProgress
    {
        public int completionCount;
        public long nextAvailableUtcTicks;
    }

    public sealed partial class TetherfallGame
    {
        private readonly List<RouteEvent> routeEvents = new List<RouteEvent>();
        private readonly HashSet<string> completedRouteEventIds = new HashSet<string>();
        private readonly Dictionary<string, RouteEventProgress> routeEventProgress =
            new Dictionary<string, RouteEventProgress>();

        public int CompletedRouteEvents => completedRouteEventIds.Count;

        public void RegisterRouteEvent(RouteEvent routeEvent)
        {
            if (routeEvent != null && !routeEvents.Contains(routeEvent))
            {
                routeEvents.Add(routeEvent);
            }
        }

        public void OnRouteEventCompleted(RouteEvent routeEvent)
        {
            if (routeEvent == null || routeEvent.Definition == null)
            {
                return;
            }

            var definition = routeEvent.Definition;
            var progress = GetRouteEventProgress(routeEvent.EventId);
            var firstCompletion = completedRouteEventIds.Add(routeEvent.EventId);
            progress.completionCount = Mathf.Max(0, progress.completionCount) + 1;
            progress.nextAvailableUtcTicks = definition.repeatable
                ? DateTime.UtcNow.AddSeconds(Mathf.Max(0f, definition.cooldownSeconds)).Ticks
                : 0L;

            var favorReward = firstCompletion ? definition.favorReward : definition.repeatFavorReward;
            var resourceAmount = firstCompletion ? definition.resourceAmount : definition.repeatResourceAmount;
            var experienceReward = firstCompletion ? 70 : definition.repeatExperienceReward;
            if (favorReward > 0)
            {
                AddFavors(favorReward, routeEvent.EventName);
            }

            if (resourceAmount > 0)
            {
                AddResource(definition.resourceReward, resourceAmount);
            }

            if (experienceReward > 0)
            {
                GainExperience(experienceReward);
            }

            if (firstCompletion)
            {
                OnRouteEventRestorationCompleted(routeEvent);
                if (definition.contributesToSignalbreak)
                {
                    AdvanceQuest("signalbreak", 1);
                }

                OnCinderwoldWorldStateChanged();
                Notify($"{routeEvent.EventName} stabilized. Its route light will remain visible.");
            }
            else
            {
                AdvanceQuest("routevigil", 1);
                Notify($"{routeEvent.EventName} holds. The signal will need another patrol later.");
            }

            RequestSave();
        }

        public Transform GetNextRouteEventTarget()
        {
            return GetNextRouteEvent()?.transform;
        }

        public string GetRoutePatrolObjectiveText()
        {
            if (!IsQuestComplete("signalbreak"))
            {
                return string.Empty;
            }

            var routeEvent = GetNextRouteEvent();
            if (routeEvent == null)
            {
                return string.Empty;
            }

            return routeEvent.IsRepeatPatrolAvailable
                ? $"Hold the destabilized {routeEvent.EventName} route signal."
                : $"Stabilize the new {routeEvent.EventName} route signal.";
        }

        internal bool IsRepeatRouteEventAvailable(RouteEvent routeEvent)
        {
            if (routeEvent == null ||
                routeEvent.Definition == null ||
                !routeEvent.IsCompleted ||
                !routeEvent.Definition.repeatable)
            {
                return false;
            }

            return GetRouteEventProgress(routeEvent.EventId).nextAvailableUtcTicks <= DateTime.UtcNow.Ticks;
        }

        internal string GetRouteEventCooldownText(RouteEvent routeEvent)
        {
            if (routeEvent == null || routeEvent.Definition == null || !routeEvent.Definition.repeatable)
            {
                return "stable";
            }

            var ticks = GetRouteEventProgress(routeEvent.EventId).nextAvailableUtcTicks;
            if (ticks <= DateTime.UtcNow.Ticks)
            {
                return "now";
            }

            var remaining = new TimeSpan(ticks - DateTime.UtcNow.Ticks);
            return remaining.TotalMinutes >= 1d
                ? $"in {Mathf.CeilToInt((float)remaining.TotalMinutes)} min"
                : $"in {Mathf.Max(1, Mathf.CeilToInt((float)remaining.TotalSeconds))} sec";
        }

        private void WriteRouteEventSave(TetherfallSaveData data)
        {
            foreach (var eventId in completedRouteEventIds)
            {
                data.completedRouteEventIds.Add(eventId);
            }

            foreach (var pair in routeEventProgress)
            {
                data.routeEvents.Add(new RouteEventSaveData
                {
                    id = pair.Key,
                    completionCount = pair.Value.completionCount,
                    nextAvailableUtcTicks = pair.Value.nextAvailableUtcTicks
                });
            }
        }

        private void RestoreRouteEventState(TetherfallSaveData data)
        {
            completedRouteEventIds.Clear();
            routeEventProgress.Clear();
            foreach (var eventId in data.completedRouteEventIds)
            {
                if (!string.IsNullOrEmpty(eventId))
                {
                    completedRouteEventIds.Add(eventId);
                }
            }

            foreach (var savedProgress in data.routeEvents)
            {
                if (savedProgress == null || string.IsNullOrEmpty(savedProgress.id))
                {
                    continue;
                }

                routeEventProgress[savedProgress.id] = new RouteEventProgress
                {
                    completionCount = Mathf.Max(0, savedProgress.completionCount),
                    nextAvailableUtcTicks = System.Math.Max(0L, savedProgress.nextAvailableUtcTicks)
                };
            }

            foreach (var eventId in completedRouteEventIds)
            {
                var hasSavedProgress = routeEventProgress.TryGetValue(eventId, out var progress);
                if (!hasSavedProgress)
                {
                    progress = new RouteEventProgress();
                    routeEventProgress[eventId] = progress;
                }

                progress.completionCount = Mathf.Max(1, progress.completionCount);
                if (!hasSavedProgress)
                {
                    // Legacy records predate per-signal cooldown state, so make their first patrol
                    // immediately available. Current records preserve their saved cooldown exactly.
                    progress.nextAvailableUtcTicks = 0L;
                }
            }

            foreach (var routeEvent in routeEvents)
            {
                if (routeEvent != null)
                {
                    routeEvent.RestoreCompleted(completedRouteEventIds.Contains(routeEvent.EventId));
                }
            }
        }

        private RouteEvent GetNextRouteEvent()
        {
            var prioritizeSignalbreak = !IsQuestComplete("signalbreak");
            RouteEvent closest = null;
            var closestDistance = float.PositiveInfinity;
            foreach (var routeEvent in routeEvents)
            {
                if (routeEvent == null ||
                    routeEvent.Definition == null ||
                    !routeEvent.IsAvailableForPatrol ||
                    !CanStartRouteEvent(routeEvent.Definition) ||
                    (prioritizeSignalbreak &&
                     (!routeEvent.Definition.contributesToSignalbreak || routeEvent.IsCompleted)))
                {
                    continue;
                }

                var distance = PlayerTransform == null
                    ? 0f
                    : Vector3.Distance(PlayerTransform.position, routeEvent.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = routeEvent;
                }
            }

            return closest;
        }

        private RouteEventProgress GetRouteEventProgress(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return new RouteEventProgress();
            }

            if (!routeEventProgress.TryGetValue(eventId, out var progress))
            {
                progress = new RouteEventProgress();
                routeEventProgress[eventId] = progress;
            }

            return progress;
        }

        private bool IsQuestComplete(string questId)
        {
            var quest = questLog.Find(record => record.id == questId);
            return quest != null && quest.completed;
        }
    }

    public sealed class RouteEvent : MonoBehaviour, IWorldInteractable
    {
        private readonly List<Enemy> activeEnemies = new List<Enemy>();

        private TetherfallGame game;
        private RouteEventDefinition definition;
        private readonly List<Renderer> signalRenderers = new List<Renderer>();
        private Renderer plinthRenderer;
        private Transform signal;
        private Light signalLight;
        private int waveIndex = -1;
        private float nextWaveAt;
        private bool active;
        private bool completed;
        private bool unstable;

        public string EventId => definition != null ? definition.id : string.Empty;
        public string EventName => definition != null ? definition.displayName : "Route event";
        public RouteEventDefinition Definition => definition;
        public bool IsCompleted => completed;
        public bool IsActive => active;
        public bool IsRepeatPatrolAvailable => completed &&
                                               game != null &&
                                               game.IsRepeatRouteEventAvailable(this);
        public bool IsAvailableForPatrol => !active && (!completed || IsRepeatPatrolAvailable);
        public string InteractionPrompt
        {
            get
            {
                if (completed)
                {
                    return IsRepeatPatrolAvailable
                        ? $"Answer unstable {EventName}"
                        : definition.repeatable
                            ? $"Stable route signal · returns {game.GetRouteEventCooldownText(this)}"
                            : "Stable route signal";
                }

                if (!game.CanStartRouteEvent(definition))
                {
                    return game.GetRouteEventBlockedText(definition);
                }

                return active
                    ? $"Defend the signal ({activeEnemies.Count} hostile{(activeEnemies.Count == 1 ? string.Empty : "s")} remaining)"
                    : $"Stabilize {EventName}";
            }
        }

        public static RouteEvent Create(TetherfallGame game, RouteEventDefinition definition)
        {
            var root = new GameObject(definition.displayName);
            root.transform.position = definition.position;
            var routeEvent = root.AddComponent<RouteEvent>();
            routeEvent.game = game;
            routeEvent.definition = definition;
            routeEvent.CreateVisuals();
            game.RegisterInteractable(routeEvent);
            game.RegisterRouteEvent(routeEvent);
            return routeEvent;
        }

        private void Update()
        {
            var wasUnstable = unstable;
            unstable = !active && IsRepeatPatrolAvailable;
            if (wasUnstable != unstable)
            {
                SetVisualState();
            }

            if (signal != null)
            {
                signal.Rotate(0f, (active ? 92f : unstable ? 58f : 34f) * Time.deltaTime, 0f, Space.Self);
                var pulse = active
                    ? 1f + Mathf.Sin(Time.time * 6.2f) * 0.14f
                    : unstable
                        ? 1f + Mathf.Sin(Time.time * 3.4f) * 0.09f
                        : 1f;
                signal.localScale = Vector3.one * pulse;
            }

            if (!active || game == null || game.GameplayPaused)
            {
                return;
            }

            PruneDefeatedEnemies();
            if (activeEnemies.Count > 0 || Time.time < nextWaveAt)
            {
                return;
            }

            if (waveIndex + 1 >= definition.waves.Length)
            {
                Complete();
                return;
            }

            SpawnNextWave();
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return !active && (!completed || definition.repeatable);
        }

        public void Interact(TetherfallGame owner)
        {
            if (!CanInteract(owner))
            {
                return;
            }

            if (completed && !IsRepeatPatrolAvailable)
            {
                owner.Notify($"{EventName} is stable. Its patrol returns {owner.GetRouteEventCooldownText(this)}.");
                return;
            }

            if (!owner.CanStartRouteEvent(definition))
            {
                owner.Notify(owner.GetRouteEventBlockedText(definition));
                return;
            }

            active = true;
            unstable = false;
            nextWaveAt = Time.time + 0.45f;
            SetVisualState();
            owner.Notify($"{EventName} destabilizes. Hold the signal.");
        }

        public void RestoreCompleted(bool isCompleted)
        {
            completed = isCompleted;
            active = false;
            activeEnemies.Clear();
            waveIndex = isCompleted ? definition.waves.Length : -1;
            unstable = false;
            SetVisualState();
        }

        private void SpawnNextWave()
        {
            waveIndex++;
            var wave = definition.waves[waveIndex];
            for (var index = 0; index < wave.Length; index++)
            {
                var angle = index * Mathf.PI * 2f / wave.Length + waveIndex * 0.47f;
                var radius = 5f + index % 2 * 1.6f;
                var position = transform.position + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
                var enemy = Enemy.Create(game, null, wave[index], position, ReportEnemyDefeated);
                if (enemy != null)
                {
                    activeEnemies.Add(enemy);
                }
            }

            nextWaveAt = Time.time + 1.1f;
            game.Notify($"{EventName}: wave {waveIndex + 1}/{definition.waves.Length}.");
        }

        private void ReportEnemyDefeated(Enemy enemy)
        {
            activeEnemies.Remove(enemy);
            if (activeEnemies.Count == 0 && waveIndex + 1 < definition.waves.Length)
            {
                nextWaveAt = Time.time + 1.5f;
                game.Notify("Signal holds. Another pressure wave is forming.");
            }
        }

        private void PruneDefeatedEnemies()
        {
            for (var index = activeEnemies.Count - 1; index >= 0; index--)
            {
                var enemy = activeEnemies[index];
                if (enemy == null || enemy.IsDefeated)
                {
                    activeEnemies.RemoveAt(index);
                }
            }
        }

        private void Complete()
        {
            active = false;
            completed = true;
            unstable = false;
            SetVisualState();
            game.OnRouteEventCompleted(this);
        }

        private void CreateVisuals()
        {
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plinth.name = "Route signal plinth";
            plinth.transform.SetParent(transform, false);
            plinth.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            plinth.transform.localScale = new Vector3(1.35f, 0.7f, 1.35f);
            plinthRenderer = plinth.GetComponent<Renderer>();
            plinthRenderer.material = game.CreateMaterial(
                Color.Lerp(definition.signalColor, new Color(0.12f, 0.15f, 0.19f), 0.7f),
                0.16f,
                0.42f);

            signal = new GameObject("Route signal crystal").transform;
            signal.SetParent(transform, false);
            signal.localPosition = new Vector3(0f, 2.25f, 0f);
            for (var index = 0; index < 3; index++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shard.name = "Route signal shard";
                shard.transform.SetParent(signal, false);
                var angle = index * Mathf.PI * 2f / 3f;
                shard.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 0.44f,
                    0f,
                    Mathf.Sin(angle) * 0.44f);
                shard.transform.localRotation = Quaternion.Euler(16f, index * 120f, 18f);
                shard.transform.localScale = new Vector3(0.14f, 0.78f, 0.14f);
                var renderer = shard.GetComponent<Renderer>();
                renderer.material = game.CreateGlowMaterial(definition.signalColor, 1.6f, 0.7f);
                signalRenderers.Add(renderer);
                Object.Destroy(shard.GetComponent<Collider>());
            }

            signalLight = gameObject.AddComponent<Light>();
            signalLight.type = LightType.Point;
            signalLight.range = 10f;
            signalLight.color = definition.signalColor;
            signalLight.shadows = LightShadows.None;
            WorldVisuals.AddLabel(
                transform,
                EventName,
                new Vector3(0f, 4.25f, 0f),
                Color.Lerp(definition.signalColor, Color.white, 0.4f),
                0.1f);
            SetVisualState();
        }

        private void SetVisualState()
        {
            foreach (var signalRenderer in signalRenderers)
            {
                if (signalRenderer != null)
                {
                    signalRenderer.material.color = completed
                        ? unstable
                            ? new Color(0.98f, 0.6f, 0.23f)
                            : Color.Lerp(definition.signalColor, Color.white, 0.5f)
                        : active
                            ? new Color(1f, 0.3f, 0.21f)
                            : definition.signalColor;
                }
            }

            if (plinthRenderer != null)
            {
                plinthRenderer.material.color = completed
                    ? unstable
                        ? new Color(0.42f, 0.22f, 0.1f)
                        : Color.Lerp(definition.signalColor, new Color(0.78f, 0.54f, 0.27f), 0.45f)
                    : active
                        ? new Color(0.39f, 0.12f, 0.13f)
                        : Color.Lerp(definition.signalColor, new Color(0.12f, 0.15f, 0.19f), 0.7f);
            }

            if (signalLight != null)
            {
                signalLight.intensity = completed ? unstable ? 1.45f : 1.7f : active ? 2.25f : 0.85f;
                signalLight.color = active
                    ? new Color(1f, 0.3f, 0.21f)
                    : unstable
                        ? new Color(0.98f, 0.6f, 0.23f)
                        : definition.signalColor;
            }
        }
    }
}
