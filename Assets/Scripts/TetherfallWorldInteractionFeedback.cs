using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tetherfall
{
    public static class TetherfallWorldInteractionFeedback
    {
        public const int MaximumActiveEffects = 18;
        public const int ShapesPerEffect = 8;

        private static readonly List<TetherfallWorldInteractionTransient> ActiveEffects =
            new List<TetherfallWorldInteractionTransient>(MaximumActiveEffects);
        private static readonly Stack<TetherfallWorldInteractionTransient> EffectPool =
            new Stack<TetherfallWorldInteractionTransient>(MaximumActiveEffects);

        private static Mesh sharedCubeMesh;
        private static int sequence;

        internal static int ActiveEffectCount => ActiveEffects.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            for (var index = ActiveEffects.Count - 1; index >= 0; index--)
            {
                var effect = ActiveEffects[index];
                if (effect != null)
                {
                    UnityEngine.Object.Destroy(effect.gameObject);
                }
            }

            while (EffectPool.Count > 0)
            {
                var effect = EffectPool.Pop();
                if (effect != null)
                {
                    UnityEngine.Object.Destroy(effect.gameObject);
                }
            }

            ActiveEffects.Clear();
            if (sharedCubeMesh != null)
            {
                UnityEngine.Object.Destroy(sharedCubeMesh);
                sharedCubeMesh = null;
            }

            sequence = 0;
        }

        public static void PlayGather(
            TetherfallGame game,
            Transform node,
            Transform collector)
        {
            if (node == null)
            {
                return;
            }

            var primary = GetSourceColor(node, new Color(0.78f, 0.57f, 0.24f));
            Spawn(
                game,
                WorldInteractionFeedbackKind.Gather,
                node.position + Vector3.up * 0.62f,
                collector != null ? collector.position + Vector3.up * 0.9f : node.position + Vector3.up * 1.8f,
                collector != null ? collector.position - node.position : Vector3.forward,
                primary,
                Color.Lerp(primary, new Color(0.45f, 0.92f, 0.73f), 0.52f),
                0.78f,
                1f);
        }

        public static void PlayResourceRespawn(TetherfallGame game, Transform node)
        {
            if (node == null)
            {
                return;
            }

            var primary = GetSourceColor(node, new Color(0.42f, 0.82f, 0.46f));
            Spawn(
                game,
                WorldInteractionFeedbackKind.ResourceRespawn,
                node.position + Vector3.up * 0.08f,
                node.position + Vector3.up * 1.4f,
                Vector3.forward,
                primary,
                new Color(0.77f, 0.91f, 0.58f),
                0.82f,
                1f);
        }

        public static void PlayFarmPlant(TetherfallGame game, Transform plot)
        {
            if (plot == null)
            {
                return;
            }

            Spawn(
                game,
                WorldInteractionFeedbackKind.FarmPlant,
                plot.position + Vector3.up * 0.18f,
                plot.position,
                Vector3.forward,
                new Color(0.57f, 0.78f, 0.28f),
                new Color(0.55f, 0.34f, 0.16f),
                0.68f,
                1.08f);
        }

        public static void PlayFarmReady(TetherfallGame game, Transform plot)
        {
            if (plot == null)
            {
                return;
            }

            Spawn(
                game,
                WorldInteractionFeedbackKind.FarmReady,
                plot.position + Vector3.up * 0.18f,
                plot.position + Vector3.up * 1.5f,
                Vector3.forward,
                new Color(0.59f, 0.92f, 0.3f),
                new Color(0.88f, 0.78f, 0.31f),
                0.9f,
                1.12f,
                plot.Find("Growing Glowreed"),
                WorldInteractionTargetMotion.Reveal);
        }

        public static void PlayFarmHarvest(
            TetherfallGame game,
            Transform plot,
            Transform collector)
        {
            if (plot == null)
            {
                return;
            }

            Spawn(
                game,
                WorldInteractionFeedbackKind.FarmHarvest,
                plot.position + Vector3.up * 0.45f,
                collector != null ? collector.position + Vector3.up * 0.9f : plot.position + Vector3.up * 1.8f,
                collector != null ? collector.position - plot.position : Vector3.forward,
                new Color(0.62f, 0.91f, 0.32f),
                new Color(0.83f, 0.59f, 0.22f),
                0.74f,
                1.08f);
        }

        public static void PlayFieldCacheOpened(
            TetherfallGame game,
            Transform cache,
            Transform collector,
            Color accent)
        {
            if (cache == null)
            {
                return;
            }

            Spawn(
                game,
                WorldInteractionFeedbackKind.FieldCacheOpen,
                cache.position + Vector3.up * 0.45f,
                collector != null ? collector.position + Vector3.up : cache.position + Vector3.up * 1.8f,
                collector != null ? collector.position - cache.position : Vector3.forward,
                accent,
                new Color(0.76f, 0.88f, 0.82f),
                0.86f,
                1.08f);
        }

        public static void PlayRestorationStage(
            TetherfallGame game,
            Transform site,
            Vector3 fallbackPosition,
            Color accent,
            int stage)
        {
            var position = site != null ? site.position : fallbackPosition;
            Spawn(
                game,
                WorldInteractionFeedbackKind.RestorationStage,
                position + Vector3.up * 0.08f,
                position + Vector3.up * 2f,
                Vector3.forward,
                accent,
                new Color(0.76f, 0.49f, 0.22f),
                1.05f,
                1f + Mathf.Clamp(stage, 1, 4) * 0.1f);
        }

        public static void PlayThreadwayActivation(
            TetherfallGame game,
            Transform anchor,
            Color accent)
        {
            if (anchor == null)
            {
                return;
            }

            Spawn(
                game,
                WorldInteractionFeedbackKind.ThreadwayActivate,
                anchor.position + Vector3.up * 0.12f,
                anchor.position + Vector3.up * 2f,
                Vector3.forward,
                accent,
                Color.Lerp(accent, Color.white, 0.48f),
                1f,
                1.14f);
        }

        public static void PlayThreadwayTravel(
            TetherfallGame game,
            Transform sourceAnchor,
            Vector3 destination,
            Color accent)
        {
            if (sourceAnchor == null)
            {
                return;
            }

            Spawn(
                game,
                WorldInteractionFeedbackKind.ThreadwayDepart,
                sourceAnchor.position + Vector3.up * 0.08f,
                destination,
                destination - sourceAnchor.position,
                accent,
                Color.Lerp(accent, Color.white, 0.52f),
                0.72f,
                1.15f);
            Spawn(
                game,
                WorldInteractionFeedbackKind.ThreadwayArrive,
                destination + Vector3.up * 0.08f,
                destination,
                sourceAnchor.position - destination,
                accent,
                Color.Lerp(accent, Color.white, 0.52f),
                0.82f,
                1.15f);
        }

        public static void PlayCooking(
            TetherfallGame game,
            Transform camp,
            MealEffect mealEffect)
        {
            if (camp == null)
            {
                return;
            }

            var primary = mealEffect switch
            {
                MealEffect.Swift => new Color(0.36f, 0.9f, 0.78f),
                MealEffect.Ward => new Color(0.42f, 0.67f, 0.92f),
                _ => new Color(0.94f, 0.58f, 0.23f)
            };
            Spawn(
                game,
                WorldInteractionFeedbackKind.Cooking,
                camp.position + Vector3.up * 0.28f,
                camp.position + Vector3.up * 1.7f,
                game != null && game.PlayerTransform != null ? game.PlayerTransform.forward : Vector3.forward,
                primary,
                new Color(0.84f, 0.72f, 0.46f),
                0.92f,
                1f,
                camp.Find("Hearthfire"),
                WorldInteractionTargetMotion.Pulse);
        }

        public static void PlayCampContract(
            TetherfallGame game,
            Transform camp,
            Transform dispatcher)
        {
            if (camp == null)
            {
                return;
            }

            var direction = dispatcher != null ? dispatcher.forward : Vector3.forward;
            Spawn(
                game,
                WorldInteractionFeedbackKind.CampContract,
                camp.position + Vector3.up * 0.5f,
                camp.position + direction * 3f + Vector3.up,
                direction,
                new Color(0.78f, 0.48f, 0.2f),
                new Color(0.38f, 0.86f, 0.76f),
                0.94f,
                1.06f);
        }

        internal static void Recycle(TetherfallWorldInteractionTransient effect)
        {
            if (effect == null)
            {
                return;
            }

            ActiveEffects.Remove(effect);
            effect.RestoreMotionTarget();
            effect.gameObject.SetActive(false);
            if (EffectPool.Count < MaximumActiveEffects)
            {
                EffectPool.Push(effect);
            }
            else
            {
                UnityEngine.Object.Destroy(effect.gameObject);
            }
        }

        private static void Spawn(
            TetherfallGame game,
            WorldInteractionFeedbackKind kind,
            Vector3 position,
            Vector3 destination,
            Vector3 direction,
            Color primary,
            Color secondary,
            float duration,
            float size,
            Transform motionTarget = null,
            WorldInteractionTargetMotion targetMotion = WorldInteractionTargetMotion.None)
        {
            if (game == null || !IsFinite(position) || !IsFinite(destination))
            {
                return;
            }

            PruneDestroyedEffects();
            if (ActiveEffects.Count >= MaximumActiveEffects)
            {
                if (kind == WorldInteractionFeedbackKind.ResourceRespawn)
                {
                    return;
                }

                Recycle(ActiveEffects[0]);
            }

            TetherfallWorldInteractionTransient effect = null;
            while (EffectPool.Count > 0 && effect == null)
            {
                effect = EffectPool.Pop();
            }

            if (effect == null)
            {
                var root = new GameObject("World interaction feedback");
                effect = root.AddComponent<TetherfallWorldInteractionTransient>();
                effect.BuildShapes(GetSharedCubeMesh());
            }

            effect.gameObject.name = $"World interaction feedback {kind} {sequence++ % 1000}";
            effect.gameObject.SetActive(true);
            ActiveEffects.Add(effect);
            primary.a = 1f;
            secondary.a = 1f;
            effect.Play(
                game,
                kind,
                position,
                destination,
                direction,
                RuntimeEffectMaterialCache.GetGlow(game, primary, 1.35f, 0.56f),
                RuntimeEffectMaterialCache.GetOpaque(game, secondary, 0.12f, 0.48f),
                Mathf.Clamp(duration, 0.1f, 2f),
                Mathf.Clamp(size, 0.5f, 2.5f),
                motionTarget,
                targetMotion);
        }

        private static void PruneDestroyedEffects()
        {
            for (var index = ActiveEffects.Count - 1; index >= 0; index--)
            {
                if (ActiveEffects[index] == null)
                {
                    ActiveEffects.RemoveAt(index);
                }
            }
        }

        private static Color GetSourceColor(Transform source, Color fallback)
        {
            var renderer = source != null ? source.GetComponentInChildren<Renderer>(true) : null;
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return fallback;
            }

            var color = renderer.sharedMaterial.color;
            color.a = 1f;
            return color.maxColorComponent > 0.12f ? color : fallback;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) &&
                   !float.IsNaN(value.y) &&
                   !float.IsNaN(value.z) &&
                   !float.IsInfinity(value.x) &&
                   !float.IsInfinity(value.y) &&
                   !float.IsInfinity(value.z);
        }

        private static Mesh GetSharedCubeMesh()
        {
            if (sharedCubeMesh != null)
            {
                return sharedCubeMesh;
            }

            sharedCubeMesh = new Mesh
            {
                name = "Tetherfall collider-free feedback cube"
            };
            sharedCubeMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f)
            };
            sharedCubeMesh.triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5,
                0, 1, 5, 0, 5, 4,
                3, 7, 6, 3, 6, 2
            };
            sharedCubeMesh.RecalculateNormals();
            sharedCubeMesh.RecalculateBounds();
            sharedCubeMesh.UploadMeshData(true);
            return sharedCubeMesh;
        }
    }

    internal enum WorldInteractionFeedbackKind
    {
        Gather,
        ResourceRespawn,
        FarmPlant,
        FarmReady,
        FarmHarvest,
        FieldCacheOpen,
        RestorationStage,
        ThreadwayActivate,
        ThreadwayDepart,
        ThreadwayArrive,
        Cooking,
        CampContract
    }

    internal enum WorldInteractionTargetMotion
    {
        None,
        Reveal,
        Pulse
    }

    public sealed class TetherfallWorldInteractionTransient : MonoBehaviour
    {
        private readonly Transform[] shapes = new Transform[TetherfallWorldInteractionFeedback.ShapesPerEffect];
        private readonly Renderer[] shapeRenderers = new Renderer[TetherfallWorldInteractionFeedback.ShapesPerEffect];

        private TetherfallGame game;
        private WorldInteractionFeedbackKind kind;
        private WorldInteractionTargetMotion targetMotion;
        private Transform motionTarget;
        private Vector3 motionTargetPosition;
        private Quaternion motionTargetRotation;
        private Vector3 motionTargetScale;
        private Vector3 destination;
        private float duration;
        private float elapsed;
        private float size;
        private bool shapesBuilt;

        internal void BuildShapes(Mesh sharedMesh)
        {
            if (shapesBuilt)
            {
                return;
            }

            for (var index = 0; index < shapes.Length; index++)
            {
                var shape = new GameObject($"Feedback shape {index + 1}");
                shape.transform.SetParent(transform, false);
                var meshFilter = shape.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = sharedMesh;
                var meshRenderer = shape.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                shapes[index] = shape.transform;
                shapeRenderers[index] = meshRenderer;
            }

            shapesBuilt = true;
        }

        internal void Play(
            TetherfallGame owner,
            WorldInteractionFeedbackKind feedbackKind,
            Vector3 position,
            Vector3 targetPosition,
            Vector3 direction,
            Material primaryMaterial,
            Material secondaryMaterial,
            float lifetime,
            float effectSize,
            Transform persistentMotionTarget,
            WorldInteractionTargetMotion persistentMotion)
        {
            RestoreMotionTarget();
            game = owner;
            kind = feedbackKind;
            destination = targetPosition;
            duration = lifetime;
            size = effectSize;
            elapsed = 0f;

            transform.position = position;
            direction.y = 0f;
            transform.rotation = direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction.normalized)
                : Quaternion.identity;
            transform.localScale = Vector3.one;

            for (var index = 0; index < shapes.Length; index++)
            {
                shapeRenderers[index].sharedMaterial = index < shapes.Length / 2
                    ? primaryMaterial
                    : secondaryMaterial;
                shapeRenderers[index].enabled = false;
                shapes[index].localPosition = Vector3.zero;
                shapes[index].localRotation = Quaternion.identity;
                shapes[index].localScale = Vector3.zero;
            }

            motionTarget = persistentMotionTarget;
            targetMotion = motionTarget != null ? persistentMotion : WorldInteractionTargetMotion.None;
            if (motionTarget != null)
            {
                motionTargetPosition = motionTarget.localPosition;
                motionTargetRotation = motionTarget.localRotation;
                motionTargetScale = motionTarget.localScale;
                ApplyMotionTarget(0f);
            }

            Animate(0f);
        }

        internal void RestoreMotionTarget()
        {
            if (motionTarget != null)
            {
                motionTarget.localPosition = motionTargetPosition;
                motionTarget.localRotation = motionTargetRotation;
                motionTarget.localScale = motionTargetScale;
            }

            motionTarget = null;
            targetMotion = WorldInteractionTargetMotion.None;
        }

        private void Update()
        {
            if (game != null && game.GameplayPaused)
            {
                return;
            }

            elapsed += Mathf.Max(0f, Time.deltaTime);
            var normalizedTime = Mathf.Clamp01(elapsed / duration);
            Animate(normalizedTime);
            ApplyMotionTarget(normalizedTime);
            if (normalizedTime >= 1f)
            {
                TetherfallWorldInteractionFeedback.Recycle(this);
            }
        }

        private void OnDestroy()
        {
            RestoreMotionTarget();
        }

        private void Animate(float time)
        {
            HideShapes();
            switch (kind)
            {
                case WorldInteractionFeedbackKind.Gather:
                    AnimateGather(time);
                    break;
                case WorldInteractionFeedbackKind.ResourceRespawn:
                    AnimateResourceRespawn(time);
                    break;
                case WorldInteractionFeedbackKind.FarmPlant:
                    AnimateFarmPlant(time);
                    break;
                case WorldInteractionFeedbackKind.FarmReady:
                    AnimateFarmReady(time);
                    break;
                case WorldInteractionFeedbackKind.FarmHarvest:
                    AnimateFarmHarvest(time);
                    break;
                case WorldInteractionFeedbackKind.FieldCacheOpen:
                    AnimateFieldCacheOpen(time);
                    break;
                case WorldInteractionFeedbackKind.RestorationStage:
                    AnimateRestorationStage(time);
                    break;
                case WorldInteractionFeedbackKind.ThreadwayActivate:
                    AnimateThreadwayActivation(time);
                    break;
                case WorldInteractionFeedbackKind.ThreadwayDepart:
                    AnimateThreadwayDeparture(time);
                    break;
                case WorldInteractionFeedbackKind.ThreadwayArrive:
                    AnimateThreadwayArrival(time);
                    break;
                case WorldInteractionFeedbackKind.Cooking:
                    AnimateCooking(time);
                    break;
                case WorldInteractionFeedbackKind.CampContract:
                    AnimateCampContract(time);
                    break;
            }
        }

        private void AnimateGather(float time)
        {
            if (time < 0.22f)
            {
                var phase = EaseOut(time / 0.22f);
                for (var index = 0; index < 4; index++)
                {
                    var radial = Radial(index, 4, 45f);
                    SetShape(
                        index,
                        radial * Mathf.Lerp(0.98f, 0.26f, phase) + Vector3.up * (0.08f + (index & 1) * 0.14f),
                        new Vector3(0.075f, 0.075f, Mathf.Lerp(0.5f, 0.22f, phase)),
                        new Vector3(0f, index * 90f + 45f, 0f));
                }

                return;
            }

            if (time < 0.48f)
            {
                var phase = EaseOut((time - 0.22f) / 0.26f);
                for (var index = 0; index < 6; index++)
                {
                    var radial = Radial(index, 6, 15f);
                    SetShape(
                        index,
                        radial * Mathf.Lerp(0.12f, 1.14f, phase) +
                        Vector3.up * (0.18f + phase * (0.32f + (index & 1) * 0.18f)),
                        new Vector3(0.085f, 0.085f, Mathf.Lerp(0.4f, 0.05f, phase)),
                        new Vector3(index * 21f, index * 60f + 15f, 18f + index * 7f));
                }

                SetShape(6, Vector3.up * 0.03f, new Vector3(0.72f, 0.035f, 0.08f), Vector3.zero);
                SetShape(7, Vector3.up * 0.03f, new Vector3(0.08f, 0.035f, 0.72f), Vector3.zero);
                return;
            }

            var harvest = Mathf.Clamp01((time - 0.48f) / 0.52f);
            var target = transform.InverseTransformPoint(destination) / size;
            var tail = 1f - SmoothRange(time, 0.82f, 1f);
            for (var index = 0; index < 4; index++)
            {
                var radial = Radial(index, 4, 22.5f);
                var start = radial * 0.76f + Vector3.up * (0.32f + index * 0.08f);
                var control = Vector3.Lerp(start, target, 0.48f) + Vector3.up * (1.05f + index * 0.08f);
                SetShape(
                    index,
                    QuadraticBezier(start, control, target, EaseOut(harvest)),
                    Vector3.one * (0.13f * tail),
                    new Vector3(harvest * 260f + index * 25f, index * 90f, harvest * 180f));
            }

            for (var index = 4; index < 8; index++)
            {
                var radial = Radial(index - 4, 4, 45f);
                SetShape(
                    index,
                    radial * Mathf.Lerp(0.62f, 0.18f, harvest) + Vector3.down * 0.48f,
                    new Vector3(0.46f * tail, 0.035f * tail, 0.075f),
                    new Vector3(0f, (index - 4) * 90f + 45f, 0f));
            }
        }

        private void AnimateResourceRespawn(float time)
        {
            var tail = 1f - SmoothRange(time, 0.78f, 1f);
            for (var index = 0; index < 4; index++)
            {
                var delayed = Mathf.Clamp01((time - index * 0.045f) / 0.66f);
                var growth = EaseOut(delayed);
                var radial = Radial(index, 4, 45f);
                SetShape(
                    index,
                    radial * 0.28f + Vector3.up * (0.04f + growth * 0.54f),
                    new Vector3(0.085f, 0.76f * growth * tail, 0.085f),
                    new Vector3(radial.z * 22f, index * 90f + 45f, -radial.x * 22f));
            }

            var spread = EaseOut(Mathf.Clamp01(time / 0.72f));
            for (var index = 4; index < 8; index++)
            {
                var radial = Radial(index - 4, 4, 0f);
                SetShape(
                    index,
                    radial * Mathf.Lerp(0.18f, 1.16f, spread) + Vector3.up * (0.04f + spread * 0.14f),
                    new Vector3(0.42f * tail, 0.035f * tail, 0.075f),
                    new Vector3(0f, (index - 4) * 90f + 90f, 0f));
            }
        }

        private void AnimateFarmPlant(float time)
        {
            var settle = EaseOut(Mathf.Clamp01(time / 0.72f));
            var tail = 1f - SmoothRange(time, 0.82f, 1f);
            for (var index = 0; index < 4; index++)
            {
                var radial = Radial(index, 4, 45f);
                SetShape(
                    index,
                    radial * 0.42f + Vector3.up * Mathf.Lerp(1.18f, 0.05f, settle),
                    Vector3.one * (0.14f * Mathf.Sin(Mathf.Clamp01(settle) * Mathf.PI) * tail),
                    new Vector3(settle * 240f, index * 90f + settle * 160f, 45f));
            }

            for (var index = 4; index < 8; index++)
            {
                var radial = Radial(index - 4, 4, 45f);
                SetShape(
                    index,
                    radial * Mathf.Lerp(1.02f, 0.5f, settle) + Vector3.down * 0.12f,
                    new Vector3(0.56f * tail, 0.03f * tail, 0.08f),
                    new Vector3(0f, (index - 4) * 90f + 135f, 0f));
            }
        }

        private void AnimateFarmReady(float time)
        {
            var tail = 1f - SmoothRange(time, 0.82f, 1f);
            for (var index = 0; index < 6; index++)
            {
                var delayed = Mathf.Clamp01((time - index * 0.035f) / 0.64f);
                var growth = EaseOut(delayed);
                var radial = Radial(index, 6, 30f);
                SetShape(
                    index,
                    radial * 0.34f + Vector3.up * (0.02f + growth * 0.72f),
                    new Vector3(0.065f, 1.15f * growth * tail, 0.065f),
                    new Vector3(radial.z * 10f, index * 60f, -radial.x * 10f));
            }

            var crown = EaseOut(Mathf.Clamp01((time - 0.38f) / 0.42f));
            SetShape(
                6,
                Vector3.up * 1.42f,
                new Vector3(0.62f * crown * tail, 0.055f * tail, 0.1f),
                new Vector3(0f, 25f, 18f));
            SetShape(
                7,
                Vector3.up * 1.42f,
                new Vector3(0.62f * crown * tail, 0.055f * tail, 0.1f),
                new Vector3(0f, -25f, -18f));
        }

        private void AnimateFarmHarvest(float time)
        {
            var target = transform.InverseTransformPoint(destination) / size;
            var travel = EaseOut(Mathf.Clamp01((time - 0.12f) / 0.76f));
            var tail = 1f - SmoothRange(time, 0.82f, 1f);
            for (var index = 0; index < 6; index++)
            {
                var radial = Radial(index, 6, 30f);
                var start = radial * 0.38f + Vector3.up * (0.38f + (index & 1) * 0.22f);
                var control = Vector3.Lerp(start, target, 0.45f) + Vector3.up * (1.1f + index * 0.04f);
                SetShape(
                    index,
                    QuadraticBezier(start, control, target, travel),
                    new Vector3(0.065f * tail, 0.42f * tail, 0.065f * tail),
                    new Vector3(18f + travel * 120f, index * 60f + travel * 180f, 22f));
            }

            var sweep = EaseOut(Mathf.Clamp01(time / 0.54f));
            SetShape(6, new Vector3(Mathf.Lerp(0.1f, 1.16f, sweep), -0.32f, 0f), new Vector3(0.5f * tail, 0.03f, 0.07f), Vector3.zero);
            SetShape(7, new Vector3(Mathf.Lerp(-0.1f, -1.16f, sweep), -0.32f, 0f), new Vector3(0.5f * tail, 0.03f, 0.07f), Vector3.zero);
        }

        private void AnimateFieldCacheOpen(float time)
        {
            var open = EaseOut(Mathf.Clamp01(time / 0.62f));
            var tail = 1f - SmoothRange(time, 0.82f, 1f);
            for (var index = 0; index < 4; index++)
            {
                var radial = Radial(index, 4, 45f);
                SetShape(
                    index,
                    radial * Mathf.Lerp(0.22f, 0.86f, open) + Vector3.up * Mathf.Lerp(0.2f, 0.92f, open),
                    new Vector3(0.42f * tail, 0.065f * tail, 0.58f * tail),
                    new Vector3(radial.z * open * 72f, index * 90f + 45f, -radial.x * open * 72f));
            }

            var target = transform.InverseTransformPoint(destination) / size;
            var route = EaseOut(Mathf.Clamp01((time - 0.24f) / 0.68f));
            for (var index = 4; index < 8; index++)
            {
                var radial = Radial(index - 4, 4, 22.5f);
                var start = radial * 0.38f + Vector3.up * 0.68f;
                var control = Vector3.Lerp(start, target, 0.48f) + Vector3.up * 1.15f;
                SetShape(
                    index,
                    QuadraticBezier(start, control, target, route),
                    Vector3.one * (0.12f * tail),
                    new Vector3(route * 220f, index * 37f, route * 170f));
            }
        }

        private void AnimateRestorationStage(float time)
        {
            var tail = 1f - SmoothRange(time, 0.86f, 1f);
            for (var index = 0; index < 6; index++)
            {
                var delayed = Mathf.Clamp01((time - index * 0.045f) / 0.62f);
                var rise = EaseOut(delayed);
                var x = index < 3 ? -1f : 1f;
                var z = (index % 3 - 1) * 0.72f;
                SetShape(
                    index,
                    new Vector3(x * 0.92f, rise * 0.92f, z),
                    new Vector3(0.09f, 1.72f * rise * tail, 0.09f),
                    new Vector3(0f, 0f, x * 4f));
            }

            var lockIn = EaseOut(Mathf.Clamp01((time - 0.38f) / 0.42f));
            SetShape(
                6,
                Vector3.up * 1.08f,
                new Vector3(2.15f * lockIn * tail, 0.075f * tail, 0.1f),
                new Vector3(0f, 0f, 26f));
            SetShape(
                7,
                Vector3.up * 1.08f,
                new Vector3(2.15f * lockIn * tail, 0.075f * tail, 0.1f),
                new Vector3(0f, 0f, -26f));
        }

        private void AnimateThreadwayActivation(float time)
        {
            var spiral = EaseOut(Mathf.Clamp01(time / 0.78f));
            var tail = 1f - SmoothRange(time, 0.84f, 1f);
            for (var index = 0; index < 6; index++)
            {
                var angle = index * 60f + spiral * 155f;
                var radial = RadialDegrees(angle);
                SetShape(
                    index,
                    radial * Mathf.Lerp(1.48f, 0.68f, spiral) + Vector3.up * (0.04f + spiral * (0.45f + index * 0.12f)),
                    new Vector3(0.48f * tail, 0.045f * tail, 0.08f),
                    new Vector3(0f, angle + 90f, spiral * 35f));
            }

            var core = EaseOut(Mathf.Clamp01((time - 0.34f) / 0.48f));
            SetShape(6, Vector3.up * (0.3f + core * 1.05f), new Vector3(0.08f, 1.5f * core * tail, 0.08f), new Vector3(0f, 0f, 18f));
            SetShape(7, Vector3.up * (0.3f + core * 1.05f), new Vector3(0.08f, 1.5f * core * tail, 0.08f), new Vector3(0f, 0f, -18f));
        }

        private void AnimateThreadwayDeparture(float time)
        {
            var close = EaseOut(time);
            var tail = 1f - SmoothRange(time, 0.82f, 1f);
            for (var index = 0; index < 6; index++)
            {
                var radial = Radial(index, 6, close * 80f);
                SetShape(
                    index,
                    radial * Mathf.Lerp(1.48f, 0.18f, close) + Vector3.up * (0.06f + close * 1.32f),
                    new Vector3(0.46f * tail, 0.045f * tail, 0.08f),
                    new Vector3(0f, index * 60f + 90f + close * 80f, close * 28f));
            }

            var column = Mathf.Sin(Mathf.Clamp01(time) * Mathf.PI);
            SetShape(6, Vector3.up * 0.8f, new Vector3(0.07f, 1.6f * column, 0.07f), new Vector3(0f, 0f, 20f));
            SetShape(7, Vector3.up * 0.8f, new Vector3(0.07f, 1.6f * column, 0.07f), new Vector3(0f, 0f, -20f));
        }

        private void AnimateThreadwayArrival(float time)
        {
            var open = EaseOut(time);
            var tail = 1f - SmoothRange(time, 0.84f, 1f);
            for (var index = 0; index < 6; index++)
            {
                var radial = Radial(index, 6, (1f - open) * 80f);
                SetShape(
                    index,
                    radial * Mathf.Lerp(0.18f, 1.52f, open) + Vector3.up * Mathf.Lerp(1.35f, 0.05f, open),
                    new Vector3(0.46f * tail, 0.045f * tail, 0.08f),
                    new Vector3(0f, index * 60f + 90f + (1f - open) * 80f, (1f - open) * 28f));
            }

            var column = Mathf.Sin(Mathf.Clamp01(time) * Mathf.PI);
            SetShape(6, Vector3.up * 0.8f, new Vector3(0.07f, 1.6f * column, 0.07f), new Vector3(0f, 0f, 20f));
            SetShape(7, Vector3.up * 0.8f, new Vector3(0.07f, 1.6f * column, 0.07f), new Vector3(0f, 0f, -20f));
        }

        private void AnimateCooking(float time)
        {
            var simmer = EaseOut(Mathf.Clamp01(time / 0.62f));
            var tail = 1f - SmoothRange(time, 0.86f, 1f);
            for (var index = 0; index < 4; index++)
            {
                var angle = index * 90f + simmer * 170f;
                var radial = RadialDegrees(angle);
                SetShape(
                    index,
                    radial * Mathf.Lerp(0.88f, 0.22f, simmer) + Vector3.up * Mathf.Lerp(0.72f, 0.28f, simmer),
                    Vector3.one * (0.16f * tail),
                    new Vector3(simmer * 230f, angle, 45f));
            }

            for (var index = 4; index < 7; index++)
            {
                var steam = Mathf.Clamp01((time - 0.2f - (index - 4) * 0.08f) / 0.62f);
                SetShape(
                    index,
                    new Vector3(Mathf.Sin(steam * Mathf.PI * 2f + index) * 0.16f, 0.42f + steam * 1.05f, 0f),
                    new Vector3(0.055f * tail, 0.38f * (1f - steam) * tail, 0.055f * tail),
                    new Vector3(0f, steam * 120f, 12f));
            }

            var pack = EaseOut(Mathf.Clamp01((time - 0.42f) / 0.42f));
            SetShape(
                7,
                Vector3.up * Mathf.Lerp(1.2f, 0.62f, pack),
                new Vector3(0.42f, 0.28f, 0.34f) * (pack * tail),
                new Vector3(0f, 0f, 8f * (1f - pack)));
        }

        private void AnimateCampContract(float time)
        {
            var packed = EaseOut(Mathf.Clamp01(time / 0.38f));
            var tail = 1f - SmoothRange(time, 0.86f, 1f);
            for (var index = 0; index < 3; index++)
            {
                var start = new Vector3((index - 1) * 0.62f, 0.35f + index * 0.22f, (index & 1) * 0.28f);
                SetShape(
                    index,
                    Vector3.Lerp(start, Vector3.up * 0.42f, packed),
                    new Vector3(0.48f, 0.22f, 0.36f) * tail,
                    new Vector3(0f, (index - 1) * 24f * (1f - packed), (index - 1) * 18f * (1f - packed)));
            }

            for (var index = 3; index < 8; index++)
            {
                var delayed = Mathf.Clamp01((time - 0.28f - (index - 3) * 0.055f) / 0.56f);
                var march = EaseOut(delayed);
                SetShape(
                    index,
                    new Vector3((index % 2 == 0 ? -0.13f : 0.13f), 0.45f + (index & 1) * 0.12f, march * 2.75f),
                    new Vector3(0.22f * tail, 0.055f * tail, 0.38f * tail),
                    new Vector3(0f, 0f, index % 2 == 0 ? 28f : -28f));
            }
        }

        private void ApplyMotionTarget(float time)
        {
            if (motionTarget == null)
            {
                return;
            }

            switch (targetMotion)
            {
                case WorldInteractionTargetMotion.Reveal:
                {
                    var reveal = EaseOut(Mathf.Clamp01(time / 0.72f));
                    motionTarget.localScale = Vector3.Scale(
                        motionTargetScale,
                        new Vector3(
                            Mathf.Lerp(0.62f, 1f, reveal),
                            Mathf.Lerp(0.12f, 1f, reveal),
                            Mathf.Lerp(0.62f, 1f, reveal)));
                    break;
                }
                case WorldInteractionTargetMotion.Pulse:
                    motionTarget.localScale = motionTargetScale * (1f + Mathf.Sin(Mathf.Clamp01(time) * Mathf.PI) * 0.24f);
                    break;
            }

            if (time >= 1f)
            {
                motionTarget.localPosition = motionTargetPosition;
                motionTarget.localRotation = motionTargetRotation;
                motionTarget.localScale = motionTargetScale;
            }
        }

        private void HideShapes()
        {
            for (var index = 0; index < shapeRenderers.Length; index++)
            {
                shapeRenderers[index].enabled = false;
            }
        }

        private void SetShape(int index, Vector3 localPosition, Vector3 localScale, Vector3 localEuler)
        {
            var shape = shapes[index];
            shape.localPosition = localPosition * size;
            shape.localScale = localScale * size;
            shape.localRotation = Quaternion.Euler(localEuler);
            shapeRenderers[index].enabled = shape.localScale.sqrMagnitude > 0.000001f;
        }

        private static Vector3 Radial(int index, int count, float angleOffset)
        {
            return RadialDegrees(index * (360f / count) + angleOffset);
        }

        private static Vector3 RadialDegrees(float angle)
        {
            var radians = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        }

        private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float time)
        {
            var inverse = 1f - time;
            return inverse * inverse * start + 2f * inverse * time * control + time * time * end;
        }

        private static float EaseOut(float time)
        {
            time = Mathf.Clamp01(time);
            var inverse = 1f - time;
            return 1f - inverse * inverse * inverse;
        }

        private static float SmoothRange(float value, float start, float end)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));
        }
    }

    public sealed partial class TetherfallGame
    {
        private readonly Dictionary<ResourceNode, bool> resourceFeedbackStates =
            new Dictionary<ResourceNode, bool>();
        private readonly Dictionary<FarmPlot, FarmFeedbackPhase> farmFeedbackStates =
            new Dictionary<FarmPlot, FarmFeedbackPhase>();

        private enum FarmFeedbackPhase
        {
            Empty,
            Growing,
            Ready
        }

        private void RegisterResourceNodeFeedback(ResourceNode node)
        {
            if (node != null)
            {
                resourceFeedbackStates[node] = node.IsAvailable;
            }
        }

        private void PlayResourceNodeStateFeedback()
        {
            for (var index = 0; index < resourceNodes.Count; index++)
            {
                var node = resourceNodes[index];
                if (node == null)
                {
                    continue;
                }

                if (!resourceFeedbackStates.TryGetValue(node, out var wasAvailable))
                {
                    resourceFeedbackStates[node] = node.IsAvailable;
                    continue;
                }

                if (wasAvailable == node.IsAvailable)
                {
                    continue;
                }

                resourceFeedbackStates[node] = node.IsAvailable;
                if (node.IsAvailable)
                {
                    TetherfallWorldInteractionFeedback.PlayResourceRespawn(this, node.transform);
                }
                else
                {
                    TetherfallWorldInteractionFeedback.PlayGather(this, node.transform, PlayerTransform);
                }
            }
        }

        private void SynchronizeResourceNodeFeedbackState()
        {
            for (var index = 0; index < resourceNodes.Count; index++)
            {
                var node = resourceNodes[index];
                if (node != null)
                {
                    resourceFeedbackStates[node] = node.IsAvailable;
                }
            }
        }

        private void RegisterFarmPlotFeedback(FarmPlot plot)
        {
            if (plot != null)
            {
                farmFeedbackStates[plot] = GetFarmFeedbackPhase(plot, DateTime.UtcNow.Ticks);
            }
        }

        private void PlayFarmPlotStateFeedback()
        {
            var nowUtcTicks = DateTime.UtcNow.Ticks;
            for (var index = 0; index < farmPlots.Count; index++)
            {
                var plot = farmPlots[index];
                if (plot == null)
                {
                    continue;
                }

                var current = GetFarmFeedbackPhase(plot, nowUtcTicks);
                if (!farmFeedbackStates.TryGetValue(plot, out var previous))
                {
                    farmFeedbackStates[plot] = current;
                    continue;
                }

                if (current == previous)
                {
                    continue;
                }

                farmFeedbackStates[plot] = current;
                switch (current)
                {
                    case FarmFeedbackPhase.Growing:
                        TetherfallWorldInteractionFeedback.PlayFarmPlant(this, plot.transform);
                        break;
                    case FarmFeedbackPhase.Ready:
                        TetherfallWorldInteractionFeedback.PlayFarmReady(this, plot.transform);
                        break;
                    default:
                        if (previous != FarmFeedbackPhase.Empty)
                        {
                            TetherfallWorldInteractionFeedback.PlayFarmHarvest(this, plot.transform, PlayerTransform);
                        }

                        break;
                }
            }
        }

        private void SynchronizeFarmPlotFeedbackState()
        {
            var nowUtcTicks = DateTime.UtcNow.Ticks;
            for (var index = 0; index < farmPlots.Count; index++)
            {
                var plot = farmPlots[index];
                if (plot != null)
                {
                    farmFeedbackStates[plot] = GetFarmFeedbackPhase(plot, nowUtcTicks);
                }
            }
        }

        private static FarmFeedbackPhase GetFarmFeedbackPhase(FarmPlot plot, long nowUtcTicks)
        {
            var saveData = plot.WriteSaveData();
            if (saveData == null || saveData.matureAtUtcTicks <= 0L)
            {
                return FarmFeedbackPhase.Empty;
            }

            return saveData.matureAtUtcTicks > nowUtcTicks
                ? FarmFeedbackPhase.Growing
                : FarmFeedbackPhase.Ready;
        }
    }
}
