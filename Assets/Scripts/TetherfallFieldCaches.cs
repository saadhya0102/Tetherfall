using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private readonly List<FieldCache> fieldCaches = new List<FieldCache>();
        private readonly HashSet<string> recoveredFieldCacheIds = new HashSet<string>();

        public int RecoveredFieldCaches => CountFieldArchiveRecoveries();
        public float ClimbStaminaCostMultiplier =>
            (HasRegionalSurveyBoon("Alderwake Basin") ? 0.75f : 1f) *
            PactClimbStaminaCostMultiplier;
        public float GlideStaminaCostMultiplier =>
            (HasRegionalSurveyBoon("Cinderwold Verge") ? 0.75f : 1f) *
            PactGlideStaminaCostMultiplier;
        public float SwimSpeedBonus =>
            (HasRegionalSurveyBoon("Morrowfen") ? 1.25f : 0f) +
            PactSwimSpeedBonus;

        public void RegisterFieldCache(FieldCache fieldCache)
        {
            if (fieldCache != null && !fieldCaches.Contains(fieldCache))
            {
                fieldCaches.Add(fieldCache);
            }
        }

        public void OnFieldCacheRecovered(FieldCache fieldCache)
        {
            if (fieldCache == null ||
                fieldCache.Definition == null ||
                string.IsNullOrEmpty(fieldCache.CacheId) ||
                recoveredFieldCacheIds.Contains(fieldCache.CacheId))
            {
                return;
            }

            var definition = fieldCache.Definition;
            var hadRegionalBoon = HasRegionalSurveyBoon(definition.regionId);
            if (!recoveredFieldCacheIds.Add(fieldCache.CacheId))
            {
                return;
            }

            fieldCache.SetRecovered(true);
            TetherfallWorldInteractionFeedback.PlayFieldCacheOpened(
                this,
                fieldCache.transform,
                PlayerTransform,
                definition.color);
            AddResource(
                definition.resourceReward,
                definition.resourceAmount,
                ResourceAcquisitionSource.AuthoredReward);
            AddFavors(definition.favorReward, definition.displayName);
            AdvanceQuest("fieldarchives", 1);
            OnCinderwoldWorldStateChanged();
            Notify(definition.inscription, 6f);
            if (!hadRegionalBoon && HasRegionalSurveyBoon(definition.regionId))
            {
                Notify(GetRegionalBoonMessage(definition.regionId), 6f);
            }

            RequestSave();
        }

        public Transform GetNextFieldCacheTarget()
        {
            if (PlayerTransform == null)
            {
                return null;
            }

            FieldCache nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var fieldCache in fieldCaches)
            {
                if (fieldCache == null || fieldCache.IsRecovered)
                {
                    continue;
                }

                var distance = (fieldCache.transform.position - PlayerTransform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = fieldCache;
                    nearestDistance = distance;
                }
            }

            return nearest != null ? nearest.transform : null;
        }

        internal int CountFieldArchiveRecoveries()
        {
            var count = 0;
            foreach (var id in recoveredFieldCacheIds)
            {
                if (IsFieldArchiveRecoveryId(id))
                {
                    count++;
                }
            }

            return Mathf.Min(count, FieldCacheCatalog.All.Count);
        }

        internal static bool IsFieldArchiveRecoveryId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            foreach (var definition in FieldCacheCatalog.All)
            {
                if (definition != null && definition.id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasRegionalSurveyBoon(string regionId)
        {
            var recoveredInRegion = 0;
            foreach (var definition in FieldCacheCatalog.All)
            {
                if (definition.regionId == regionId && recoveredFieldCacheIds.Contains(definition.id))
                {
                    recoveredInRegion++;
                }
            }

            return recoveredInRegion >= 3;
        }

        private static string GetRegionalBoonMessage(string regionId)
        {
            return regionId switch
            {
                "Alderwake Basin" => "Rootline Knot attuned. Climbing costs 25% less stamina.",
                "Morrowfen" => "Reedwake Gauge attuned. Swim speed increased.",
                "Cinderwold Verge" => "Ashveil Loom attuned. Gliding costs 25% less stamina.",
                _ => "Regional field survey completed."
            };
        }

        private void WriteFieldCacheSave(TetherfallSaveData data)
        {
            data.recoveredFieldCacheIds.Clear();
            foreach (var id in recoveredFieldCacheIds)
            {
                data.recoveredFieldCacheIds.Add(id);
            }
        }

        private void RestoreFieldCacheState(TetherfallSaveData data)
        {
            recoveredFieldCacheIds.Clear();
            foreach (var id in data.recoveredFieldCacheIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    recoveredFieldCacheIds.Add(id);
                }
            }

            foreach (var fieldCache in fieldCaches)
            {
                if (fieldCache != null)
                {
                    fieldCache.SetRecovered(recoveredFieldCacheIds.Contains(fieldCache.CacheId));
                }
            }
        }
    }

    public static class FieldCacheBuilder
    {
        public static void Build(TetherfallGame game)
        {
            foreach (var definition in FieldCacheCatalog.All)
            {
                FieldCache.Create(game, definition);
            }
        }
    }

    public sealed class FieldCache : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private Renderer cacheRenderer;
        private Renderer signalRenderer;
        private Light signalLight;

        public string CacheId => Definition != null ? Definition.id : string.Empty;
        public FieldCacheDefinition Definition { get; private set; }
        public bool IsRecovered { get; private set; }
        public string InteractionPrompt => IsRecovered
            ? "Field cache recovered"
            : $"Recover {Definition.displayName}";

        public static FieldCache Create(TetherfallGame game, FieldCacheDefinition definition)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = definition.displayName;
            root.transform.position = definition.position + new Vector3(0f, 0.46f, 0f);
            root.transform.localScale = new Vector3(1.25f, 0.92f, 0.88f);

            var fieldCache = root.AddComponent<FieldCache>();
            fieldCache.game = game;
            fieldCache.Definition = definition;
            fieldCache.cacheRenderer = root.GetComponent<Renderer>();
            fieldCache.cacheRenderer.material = game.CreateMaterial(
                new Color(definition.color.r * 0.45f, definition.color.g * 0.45f, definition.color.b * 0.45f),
                0.12f,
                0.38f);

            var crest = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crest.name = "Threadglass cache crest";
            crest.transform.SetParent(root.transform, false);
            crest.transform.localPosition = new Vector3(0f, 0.64f, 0f);
            crest.transform.localScale = new Vector3(0.37f, 0.16f, 0.37f);
            fieldCache.signalRenderer = crest.GetComponent<Renderer>();
            fieldCache.signalRenderer.material = game.CreateMaterial(definition.color, 0.3f, 0.72f);
            Object.Destroy(crest.GetComponent<Collider>());

            var lightRoot = new GameObject("Field cache signal");
            lightRoot.transform.SetParent(root.transform, false);
            lightRoot.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            fieldCache.signalLight = lightRoot.AddComponent<Light>();
            fieldCache.signalLight.type = LightType.Point;
            fieldCache.signalLight.color = definition.color;
            fieldCache.signalLight.intensity = 1.4f;
            fieldCache.signalLight.range = 4.8f;

            WorldVisuals.AddLabel(root.transform, "field cache", new Vector3(0f, 1.45f, 0f), definition.color, 0.08f);
            game.RegisterInteractable(fieldCache);
            game.RegisterFieldCache(fieldCache);
            return fieldCache;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return !IsRecovered;
        }

        public void Interact(TetherfallGame owner)
        {
            owner.OnFieldCacheRecovered(this);
        }

        public void SetRecovered(bool recovered)
        {
            IsRecovered = recovered;
            if (cacheRenderer == null)
            {
                return;
            }

            cacheRenderer.material.color = recovered
                ? new Color(0.21f, 0.23f, 0.22f)
                : new Color(
                    Definition.color.r * 0.45f,
                    Definition.color.g * 0.45f,
                    Definition.color.b * 0.45f);
            signalRenderer.enabled = !recovered;
            signalLight.enabled = !recovered;
        }
    }
}
