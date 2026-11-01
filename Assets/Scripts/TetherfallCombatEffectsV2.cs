using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tetherfall
{
    public readonly struct CombatEffectsBudgetSnapshot
    {
        public readonly int ActiveEffects;
        public readonly int CreatedEffects;
        public readonly int PooledEffects;
        public readonly int DroppedEffects;

        public CombatEffectsBudgetSnapshot(
            int activeEffects,
            int createdEffects,
            int pooledEffects,
            int droppedEffects)
        {
            ActiveEffects = activeEffects;
            CreatedEffects = createdEffects;
            PooledEffects = pooledEffects;
            DroppedEffects = droppedEffects;
        }
    }

    internal enum CombatEffectPoolId
    {
        PathRibbon,
        PathCore,
        BowRelease,
        BowTracer,
        StaffRing,
        StaffThread,
        ContactFlash,
        ImpactShard,
        GroundRing,
        ImpactLine,
        AllyBolt,
        AllyShard,
        AllyOrb,
        BossRing,
        BossShard,
        Count
    }

    internal enum CombatEffectMaterialKind
    {
        Glow,
        Transparent,
        Opaque
    }

    internal enum CombatImpactProfile
    {
        Generic,
        Pathblade,
        Riftbow,
        Threadstaff,
        Slateback,
        Player
    }

    internal enum AllyEffectIdentity
    {
        Fallback,
        Bramblet,
        Slateback,
        Mistling,
        Cinderwing,
        Scout,
        Medic,
        Breaker
    }

    internal sealed class CombatEffectPool
    {
        public readonly PrimitiveType Primitive;
        public readonly int MaximumActive;
        public readonly Queue<TetherfallCombatEffect> Inactive;
        public int Active;
        public int Created;

        public CombatEffectPool(PrimitiveType primitive, int maximumActive)
        {
            Primitive = primitive;
            MaximumActive = maximumActive;
            Inactive = new Queue<TetherfallCombatEffect>(maximumActive);
        }
    }

    /// <summary>
    /// Bounded, collider-free runtime effect primitive. Instances are owned by
    /// TetherfallCombatEffectsV2 and returned to a per-shape pool after use.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TetherfallCombatEffect : MonoBehaviour
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");

        private Renderer visual;
        private MaterialPropertyBlock propertyBlock;
        private CombatEffectPoolId poolId;
        private Vector3 startScale;
        private Vector3 targetScale;
        private Vector3 velocity;
        private Vector3 angularVelocity;
        private Color baseColor;
        private float emissionStrength;
        private float startedAt;
        private float expiresAt;
        private float duration;
        private bool leased;
        private bool suppressDestroyNotification;

        public bool IsLeased => leased;

        internal void Activate(
            CombatEffectPoolId newPoolId,
            string effectName,
            Vector3 position,
            Quaternion rotation,
            Vector3 initialScale,
            Vector3 finalScale,
            Vector3 movement,
            Vector3 spin,
            Color color,
            Material material,
            float lifetime,
            float emission)
        {
            if (visual == null)
            {
                visual = GetComponent<Renderer>();
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            poolId = newPoolId;
            leased = true;
            suppressDestroyNotification = false;
            gameObject.name = effectName;
            gameObject.layer = TetherfallCombatEffectsV2.IgnoreRaycastLayer;
            transform.SetParent(null, false);
            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = initialScale;
            startScale = initialScale;
            targetScale = finalScale;
            velocity = movement;
            angularVelocity = spin;
            baseColor = color;
            emissionStrength = Mathf.Max(0f, emission);
            duration = Mathf.Max(0.02f, lifetime);
            startedAt = Time.time;
            expiresAt = startedAt + duration;

            if (visual != null)
            {
                visual.enabled = true;
                visual.sharedMaterial = material;
                visual.shadowCastingMode = ShadowCastingMode.Off;
                visual.receiveShadows = false;
                ApplyColor(1f);
            }
        }

        internal void RecycleNow()
        {
            if (leased)
            {
                TetherfallCombatEffectsV2.Recycle(this, poolId);
            }
        }

        internal void PrepareForPool()
        {
            leased = false;
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
            startScale = Vector3.one;
            targetScale = Vector3.one;
            if (visual != null)
            {
                propertyBlock?.Clear();
                if (propertyBlock != null)
                {
                    visual.SetPropertyBlock(propertyBlock);
                }

                visual.sharedMaterial = null;
                visual.enabled = false;
            }

            transform.SetParent(null, false);
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        internal void SuppressDestroyNotification()
        {
            suppressDestroyNotification = true;
            leased = false;
        }

        private void Update()
        {
            if (!leased)
            {
                return;
            }

            var game = TetherfallGame.Instance;
            if (game != null && game.GameplayPaused)
            {
                startedAt += Time.deltaTime;
                expiresAt += Time.deltaTime;
                return;
            }

            var deltaTime = Time.deltaTime;
            transform.position += velocity * deltaTime;
            if (angularVelocity.sqrMagnitude > 0.0001f)
            {
                transform.Rotate(angularVelocity * deltaTime, Space.Self);
            }

            var progress = Mathf.Clamp01((Time.time - startedAt) / duration);
            var eased = progress * progress * (3f - 2f * progress);
            transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);

            var fade = 1f - Mathf.Clamp01((progress - 0.28f) / 0.72f);
            fade *= fade;
            ApplyColor(fade);
            if (Time.time >= expiresAt)
            {
                RecycleNow();
            }
        }

        private void ApplyColor(float visibility)
        {
            if (visual == null || propertyBlock == null)
            {
                return;
            }

            var visibleColor = new Color(
                baseColor.r,
                baseColor.g,
                baseColor.b,
                baseColor.a * visibility);
            propertyBlock.Clear();
            propertyBlock.SetColor(ColorProperty, visibleColor);
            propertyBlock.SetColor(BaseColorProperty, visibleColor);
            propertyBlock.SetColor(
                EmissionColorProperty,
                new Color(
                    baseColor.r * emissionStrength * visibility,
                    baseColor.g * emissionStrength * visibility,
                    baseColor.b * emissionStrength * visibility,
                    visibleColor.a));
            visual.SetPropertyBlock(propertyBlock);
        }

        private void OnDestroy()
        {
            if (!suppressDestroyNotification)
            {
                TetherfallCombatEffectsV2.NotifyDestroyed(this, poolId, leased);
            }
        }
    }

    /// <summary>
    /// Optimized layered combat-effects pass. All hot-path geometry is pooled,
    /// collider-free, material-shared, distance-gated, and globally bounded.
    /// </summary>
    public static class TetherfallCombatEffectsV2
    {
        public const int MaximumActiveEffects = 112;
        public const int MaximumCreatedEffects = 144;
        public const int IgnoreRaycastLayer = 2;

        private const int WeaponTraceCapacity = 6;
        private const float FullDetailDistance = 34f;
        private const float MaximumEffectDistance = 58f;

        private static readonly CombatEffectPool[] Pools =
            new CombatEffectPool[(int)CombatEffectPoolId.Count];
        private static readonly HashSet<TetherfallCombatEffect> TrackedEffects =
            new HashSet<TetherfallCombatEffect>();
        private static readonly WeaponTrace[] WeaponTraces = new WeaponTrace[WeaponTraceCapacity];

        private static int activeEffects;
        private static int createdEffects;
        private static int droppedEffects;
        private static int weaponTraceSequence;
        private static int effectSequence;
        private static float nextGroundRingAt;
        private static bool resetting;

        private struct WeaponTrace
        {
            public WeaponDiscipline Weapon;
            public Vector3 Origin;
            public Vector3 Direction;
            public float StartedAt;
            public int Sequence;
            public bool Slateback;
            public bool Valid;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ClearAllEffects();
            for (var index = 0; index < WeaponTraces.Length; index++)
            {
                WeaponTraces[index] = default;
            }

            weaponTraceSequence = 0;
            effectSequence = 0;
            nextGroundRingAt = 0f;
            droppedEffects = 0;
        }

        public static CombatEffectsBudgetSnapshot GetBudgetSnapshot()
        {
            var pooled = 0;
            for (var index = 0; index < Pools.Length; index++)
            {
                var pool = Pools[index];
                if (pool != null)
                {
                    pooled += pool.Inactive.Count;
                }
            }

            return new CombatEffectsBudgetSnapshot(
                activeEffects,
                createdEffects,
                pooled,
                droppedEffects);
        }

        public static void RecycleAllActive()
        {
            foreach (var effect in TrackedEffects)
            {
                if (effect != null && effect.IsLeased)
                {
                    effect.RecycleNow();
                }
            }
        }

        public static void ResetForVerification()
        {
            ClearAllEffects();
            for (var index = 0; index < WeaponTraces.Length; index++)
            {
                WeaponTraces[index] = default;
            }

            weaponTraceSequence = 0;
            effectSequence = 0;
            nextGroundRingAt = 0f;
            droppedEffects = 0;
        }

        public static void PlayWeaponSwing(
            TetherfallGame game,
            WeaponDiscipline weapon,
            Vector3 position,
            Vector3 direction,
            int comboStage)
        {
            if (game == null || !IsFinite(position))
            {
                return;
            }

            direction = NormalizeHorizontal(direction, Vector3.forward);
            if (game.Player != null &&
                game.Player.IsMounted &&
                game.MountEchosworn != null &&
                ContainsToken(game.MountEchosworn.CreatureName, "Slateback"))
            {
                RecordSlatebackTrace(position, direction);
                var mountedDetail = GetDetailLevel(game, position);
                if (mountedDetail > 0)
                {
                    SpawnSlatebackCharge(game, position, direction, mountedDetail);
                }

                return;
            }

            RecordWeaponTrace(weapon, position, direction);
            var detail = GetDetailLevel(game, position);
            if (detail == 0)
            {
                return;
            }

            var color = ResolveWeaponColor(weapon);
            switch (weapon)
            {
                case WeaponDiscipline.Riftbow:
                    SpawnRiftbowRelease(game, position, direction, color, detail);
                    break;

                case WeaponDiscipline.Threadstaff:
                    SpawnThreadstaffCast(game, position, direction, color, detail);
                    break;

                default:
                    SpawnPathbladeRibbon(
                        game,
                        position,
                        direction,
                        color,
                        Mathf.Max(0, comboStage),
                        detail);
                    break;
            }
        }

        public static void CreateLegacySwingArc(
            TetherfallGame game,
            Vector3 position,
            Vector3 direction,
            Color color,
            float scale)
        {
            if (game == null || !IsFinite(scale))
            {
                return;
            }

            direction = NormalizeHorizontal(direction, Vector3.forward);
            var detail = GetDetailLevel(game, position);
            if (detail == 0)
            {
                return;
            }

            color = ResolveFriendlyColor(color);
            SpawnPathbladeRibbon(
                game,
                position,
                direction,
                color,
                Mathf.RoundToInt(Mathf.Clamp(Mathf.Abs(scale), 0f, 3f)),
                detail);
        }

        public static void PlayEnemyHit(
            TetherfallGame game,
            Enemy enemy,
            int damage,
            Color fallbackColor)
        {
            if (game == null || enemy == null || damage <= 0)
            {
                return;
            }

            var contact = enemy.transform.position + Vector3.up * 1.35f;
            var ground = enemy.transform.position + Vector3.up * 0.055f;
            var detail = GetDetailLevel(game, contact);
            if (detail == 0)
            {
                return;
            }

            var profile = ResolveRecentImpactProfile(contact, out var direction, out var weapon);
            var color = ResolveImpactColor(profile, weapon, fallbackColor);
            var intensity = Mathf.Clamp01(damage / 80f);
            CreateLayeredImpact(
                game,
                contact,
                ground,
                direction,
                color,
                profile,
                intensity,
                detail);

            if (enemy.IsStaggered)
            {
                SpawnStaggerAccent(game, ground, contact, color, detail, false);
            }

            CombatFeedback.CreateDamageNumber(
                contact + Vector3.up * 0.24f,
                damage,
                color);
        }

        public static void PlayBossHit(
            TetherfallGame game,
            Vector3 bossPosition,
            int damage)
        {
            if (game == null || damage <= 0 || !IsFinite(bossPosition))
            {
                return;
            }

            var contact = bossPosition + Vector3.up * 2.4f;
            var ground = bossPosition + Vector3.up * 0.065f;
            var detail = GetDetailLevel(game, contact);
            var profile = ResolveRecentImpactProfile(contact, out var direction, out var weapon);
            var bossColor = ResolveFriendlyColor(new Color(0.94f, 0.58f, 0.8f));
            var color = profile == CombatImpactProfile.Generic
                ? bossColor
                : Color.Lerp(
                    bossColor,
                    ResolveImpactColor(profile, weapon, bossColor),
                    0.68f);
            color.a = 1f;
            var intensity = Mathf.Clamp01(damage / 100f);

            if (detail > 0)
            {
                CreateLayeredImpact(
                    game,
                    contact,
                    ground,
                    direction,
                    color,
                    profile,
                    Mathf.Max(0.45f, intensity),
                    detail);
                SpawnBossHitLayer(game, contact, direction, color, intensity, detail);
                CombatFeedback.CreateDamageNumber(
                    bossPosition + Vector3.up * 3.15f,
                    damage,
                    color);
            }

            var boss = game.ActiveDungeonBoss;
            if (boss == null)
            {
                return;
            }

            ObserveBossState(game, boss);
            var maximumHealth = Mathf.Max(1, boss.MaxHealth);
            var currentRatio = Mathf.Clamp01(boss.Health / (float)maximumHealth);
            var previousRatio = Mathf.Clamp01((boss.Health + damage) / (float)maximumHealth);
            var phaseIndex = GetCrossedBossPhase(boss.BossName, previousRatio, currentRatio);
            if (phaseIndex > 0 && detail > 0)
            {
                SpawnBossPhaseTransition(game, bossPosition, boss.BossName, phaseIndex, detail);
            }
        }

        public static void PlayPlayerHit(TetherfallGame game, Vector3 position, int amount)
        {
            if (game == null || amount <= 0 || !IsFinite(position))
            {
                return;
            }

            var detail = GetDetailLevel(game, position);
            if (detail == 0)
            {
                return;
            }

            var color = TetherfallAccessibilityRuntime.ResolveColorCue(
                new Color(0.95f, 0.26f, 0.18f),
                TetherfallAccessibilityColorCue.Hostile);
            color.a = 1f;
            var direction = game.PlayerTransform != null
                ? -game.PlayerTransform.forward
                : Vector3.back;
            var ground = position - Vector3.up * 0.9f;
            var intensity = Mathf.Clamp01(amount / 40f);
            CreateLayeredImpact(
                game,
                position,
                ground,
                direction,
                color,
                CombatImpactProfile.Player,
                intensity,
                detail);
            CombatFeedback.CreateDamageNumber(position + Vector3.up * 0.38f, amount, color);
        }

        public static void CreateGenericImpact(
            TetherfallGame game,
            Vector3 position,
            Color color,
            float scale)
        {
            if (game == null || !IsFinite(position) || !IsFinite(scale))
            {
                return;
            }

            var detail = GetDetailLevel(game, position);
            if (detail == 0)
            {
                return;
            }

            var observer = GetObserverPosition(game);
            var direction = NormalizeHorizontal(position - observer, Vector3.forward);
            var clampedScale = Mathf.Clamp(Mathf.Abs(scale), 0.1f, 4f);
            var intensity = Mathf.InverseLerp(0.35f, 2.25f, clampedScale);
            CreateLayeredImpact(
                game,
                position,
                ResolveGroundPosition(position),
                direction,
                ResolveHostileColor(color),
                CombatImpactProfile.Generic,
                intensity,
                detail);
        }

        public static void CreateAllyStrike(
            TetherfallGame game,
            Vector3 source,
            Vector3 target,
            Color fallbackColor,
            string strikeSource)
        {
            if (game == null || !IsFinite(source) || !IsFinite(target))
            {
                return;
            }

            var detail = Mathf.Max(GetDetailLevel(game, source), GetDetailLevel(game, target));
            if (detail == 0)
            {
                return;
            }

            var identity = ResolveAllyIdentity(strikeSource);
            var color = ResolveAllyColor(identity, fallbackColor);
            var direction = NormalizeDirection(target - source, game.transform.forward);
            var distance = Mathf.Max(0.3f, Vector3.Distance(source, target));
            var emphatic = ContainsToken(strikeSource, "Crossweave") ||
                           ContainsToken(strikeSource, "Threadlink") ||
                           ContainsToken(strikeSource, "tactical");
            var intensity = emphatic ? 1f : 0.72f;

            SpawnAllySourceCue(game, source, direction, color, identity, detail);
            SpawnAllyTravel(game, source, direction, distance, color, identity, detail);
            SpawnAllyImpact(
                game,
                target,
                target - Vector3.up * 0.84f,
                direction,
                color,
                identity,
                intensity,
                detail);
        }

        internal static void ObserveBossState(TetherfallGame game, DungeonBoss boss)
        {
            if (game == null || boss == null)
            {
                return;
            }

            var driver = game.GetComponent<TetherfallCombatEffectsDriver>();
            if (driver == null)
            {
                driver = game.gameObject.AddComponent<TetherfallCombatEffectsDriver>();
            }

            driver.Configure(game);
            driver.ObserveBoss(boss);
        }

        internal static void SpawnBossStagger(
            TetherfallGame game,
            DungeonBoss boss)
        {
            if (game == null || boss == null)
            {
                return;
            }

            var contact = boss.transform.position + Vector3.up * 2.25f;
            var ground = boss.transform.position + Vector3.up * 0.07f;
            var detail = GetDetailLevel(game, contact);
            if (detail == 0)
            {
                return;
            }

            var color = TetherfallAccessibilityRuntime.ResolveColorCue(
                new Color(0.47f, 0.91f, 0.82f),
                TetherfallAccessibilityColorCue.Positive);
            color.a = 1f;
            SpawnStaggerAccent(game, ground, contact, color, detail, true);
        }

        internal static void Recycle(
            TetherfallCombatEffect effect,
            CombatEffectPoolId poolId)
        {
            if (effect == null || resetting)
            {
                return;
            }

            var pool = Pools[(int)poolId];
            if (pool == null)
            {
                effect.SuppressDestroyNotification();
                TrackedEffects.Remove(effect);
                createdEffects = Mathf.Max(0, createdEffects - 1);
                activeEffects = Mathf.Max(0, activeEffects - 1);
                DestroyObject(effect.gameObject);
                return;
            }

            pool.Active = Mathf.Max(0, pool.Active - 1);
            activeEffects = Mathf.Max(0, activeEffects - 1);
            effect.PrepareForPool();
            effect.gameObject.SetActive(false);
            pool.Inactive.Enqueue(effect);
        }

        internal static void NotifyDestroyed(
            TetherfallCombatEffect effect,
            CombatEffectPoolId poolId,
            bool wasLeased)
        {
            if (resetting || ReferenceEquals(effect, null))
            {
                return;
            }

            TrackedEffects.Remove(effect);
            createdEffects = Mathf.Max(0, createdEffects - 1);
            var pool = Pools[(int)poolId];
            if (pool != null)
            {
                pool.Created = Mathf.Max(0, pool.Created - 1);
                if (wasLeased)
                {
                    pool.Active = Mathf.Max(0, pool.Active - 1);
                    activeEffects = Mathf.Max(0, activeEffects - 1);
                }
            }
        }

        private static void SpawnPathbladeRibbon(
            TetherfallGame game,
            Vector3 position,
            Vector3 direction,
            Color color,
            int comboStage,
            int detail)
        {
            var side = (comboStage & 1) == 0 ? 1f : -1f;
            var right = Vector3.Cross(Vector3.up, direction);
            var comboScale = 1f + Mathf.Min(2, comboStage) * 0.1f;
            var center = position +
                         Vector3.up * (1.02f + comboStage * 0.025f) +
                         direction * (1.08f + comboStage * 0.08f) +
                         right * side * 0.14f;
            var rotation = Quaternion.LookRotation(direction, Vector3.up) *
                           Quaternion.Euler(0f, 0f, side * (21f + comboStage * 4f));

            SpawnEffect(
                game,
                CombatEffectPoolId.PathRibbon,
                "Pathblade directional ribbon",
                center,
                rotation,
                new Vector3(0.075f, 0.72f, 1.3f) * comboScale,
                new Vector3(0.025f, 1.02f, 1.68f) * comboScale,
                direction * 0.55f,
                new Vector3(0f, 0f, -side * 115f),
                WithAlpha(color, 0.42f),
                CombatEffectMaterialKind.Transparent,
                0.18f,
                0f);

            SpawnEffect(
                game,
                CombatEffectPoolId.PathCore,
                "Pathblade ribbon edge",
                center + direction * 0.12f,
                rotation,
                new Vector3(0.018f, 0.48f, 1.1f) * comboScale,
                new Vector3(0.008f, 0.66f, 1.42f) * comboScale,
                direction * 0.7f,
                new Vector3(0f, 0f, -side * 120f),
                WithAlpha(Color.Lerp(color, Color.white, 0.44f), 0.9f),
                CombatEffectMaterialKind.Glow,
                0.145f,
                1.75f);

            if (detail > 1)
            {
                SpawnEffect(
                    game,
                    CombatEffectPoolId.PathRibbon,
                    "Pathblade trailing ribbon",
                    center - direction * 0.18f - right * side * 0.08f,
                    rotation * Quaternion.Euler(0f, 0f, -side * 11f),
                    new Vector3(0.055f, 0.58f, 1.08f) * comboScale,
                    new Vector3(0.018f, 0.88f, 1.5f) * comboScale,
                    direction * 0.36f,
                    new Vector3(0f, 0f, -side * 82f),
                    WithAlpha(color, 0.24f),
                    CombatEffectMaterialKind.Transparent,
                    0.21f,
                    0f);
            }
        }

        private static void SpawnRiftbowRelease(
            TetherfallGame game,
            Vector3 position,
            Vector3 direction,
            Color color,
            int detail)
        {
            var nock = position + Vector3.up * 1.08f + direction * 0.66f;
            var facing = Quaternion.LookRotation(direction, Vector3.up) *
                         Quaternion.Euler(90f, 0f, 0f);
            SpawnEffect(
                game,
                CombatEffectPoolId.BowRelease,
                "Riftbow collapsing draw ring",
                nock,
                facing,
                new Vector3(0.34f, 0.018f, 0.34f),
                new Vector3(0.055f, 0.008f, 0.055f),
                -direction * 0.18f,
                Vector3.zero,
                WithAlpha(color, 0.52f),
                CombatEffectMaterialKind.Transparent,
                0.16f,
                0f);

            SpawnEffect(
                game,
                CombatEffectPoolId.AllyOrb,
                "Riftbow nock flash",
                nock,
                Quaternion.identity,
                Vector3.one * 0.2f,
                Vector3.one * 0.035f,
                direction * 0.35f,
                Vector3.zero,
                WithAlpha(Color.Lerp(color, Color.white, 0.48f), 0.92f),
                CombatEffectMaterialKind.Glow,
                0.105f,
                1.8f);

            SpawnEffect(
                game,
                CombatEffectPoolId.BowTracer,
                "Riftbow release tracer",
                nock + direction * 0.74f,
                Quaternion.LookRotation(direction, Vector3.up),
                new Vector3(0.038f, 0.038f, 1.42f),
                new Vector3(0.012f, 0.012f, 0.72f),
                direction * 25f,
                Vector3.zero,
                WithAlpha(color, 0.82f),
                CombatEffectMaterialKind.Glow,
                0.34f,
                1.55f);

            if (detail > 1)
            {
                var right = Vector3.Cross(Vector3.up, direction) * 0.075f;
                SpawnEffect(
                    game,
                    CombatEffectPoolId.BowTracer,
                    "Riftbow split tracer",
                    nock + direction * 0.56f + right,
                    Quaternion.LookRotation(direction, Vector3.up),
                    new Vector3(0.018f, 0.018f, 0.92f),
                    new Vector3(0.006f, 0.006f, 0.44f),
                    direction * 23.5f - right * 0.8f,
                    Vector3.zero,
                    WithAlpha(Color.Lerp(color, Color.white, 0.35f), 0.52f),
                    CombatEffectMaterialKind.Glow,
                    0.3f,
                    1.35f);
            }
        }

        private static void SpawnThreadstaffCast(
            TetherfallGame game,
            Vector3 position,
            Vector3 direction,
            Color color,
            int detail)
        {
            var focus = position + Vector3.up * 1.28f + direction * 0.48f;
            var areaCenter = position + direction * 1.28f + Vector3.up * 0.06f;
            SpawnEffect(
                game,
                CombatEffectPoolId.StaffRing,
                "Threadstaff focus ring",
                focus,
                Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.12f, 0.014f, 0.12f),
                new Vector3(0.62f, 0.01f, 0.62f),
                direction * 0.22f,
                new Vector3(0f, 70f, 0f),
                WithAlpha(Color.Lerp(color, Color.white, 0.3f), 0.62f),
                CombatEffectMaterialKind.Transparent,
                0.24f,
                0f);
            SpawnEffect(
                game,
                CombatEffectPoolId.StaffRing,
                "Threadstaff area ring",
                areaCenter,
                Quaternion.identity,
                new Vector3(0.48f, 0.014f, 0.48f),
                new Vector3(detail > 1 ? 3.7f : 2.8f, 0.01f, detail > 1 ? 3.7f : 2.8f),
                Vector3.zero,
                new Vector3(0f, 95f, 0f),
                WithAlpha(color, TetherfallAccessibilityRuntime.HighContrastTelegraphs ? 0.16f : 0.24f),
                CombatEffectMaterialKind.Transparent,
                0.3f,
                0f);

            var right = Vector3.Cross(Vector3.up, direction);
            var threadCount = detail > 1 ? 3 : 2;
            for (var index = 0; index < threadCount; index++)
            {
                var offset = (index - (threadCount - 1) * 0.5f) * 0.18f;
                var end = areaCenter + Vector3.up * (0.24f + index * 0.08f) + right * offset;
                SpawnLine(
                    game,
                    CombatEffectPoolId.StaffThread,
                    "Threadstaff cast thread",
                    focus + right * offset * 0.35f,
                    end,
                    0.022f,
                    0.008f,
                    WithAlpha(Color.Lerp(color, Color.white, index * 0.12f), 0.66f),
                    CombatEffectMaterialKind.Glow,
                    0.24f,
                    direction * (0.08f + index * 0.04f),
                    new Vector3(0f, 0f, index % 2 == 0 ? 34f : -34f),
                    1.35f);
            }
        }

        private static void SpawnSlatebackCharge(
            TetherfallGame game,
            Vector3 position,
            Vector3 direction,
            int detail)
        {
            var color = ResolveAllyColor(
                AllyEffectIdentity.Slateback,
                new Color(0.38f, 0.72f, 0.76f));
            var ground = position + direction * 1.15f + Vector3.up * 0.055f;
            TrySpawnGroundRing(game, ground, color, 0.5f, detail > 1 ? 2.65f : 2.1f, 0.26f);
            SpawnEffect(
                game,
                CombatEffectPoolId.AllyShard,
                "Slateback charge wake",
                position + direction * 0.82f + Vector3.up * 0.12f,
                Quaternion.LookRotation(direction, Vector3.up),
                new Vector3(0.24f, 0.07f, 0.72f),
                new Vector3(0.1f, 0.025f, 1.42f),
                direction * 3.1f,
                Vector3.zero,
                WithAlpha(color, 0.46f),
                CombatEffectMaterialKind.Transparent,
                0.25f,
                0f);
            SpawnAllyDebris(
                game,
                position + direction * 1.35f + Vector3.up * 0.18f,
                direction,
                color,
                detail > 1 ? 4 : 2,
                true);
        }

        private static void CreateLayeredImpact(
            TetherfallGame game,
            Vector3 contact,
            Vector3 ground,
            Vector3 direction,
            Color color,
            CombatImpactProfile profile,
            float intensity,
            int detail)
        {
            intensity = Mathf.Clamp01(intensity);
            direction = NormalizeHorizontal(direction, Vector3.forward);
            var scale = Mathf.Lerp(0.72f, 1.26f, intensity);
            var bright = Color.Lerp(color, Color.white, 0.45f);
            SpawnEffect(
                game,
                CombatEffectPoolId.ContactFlash,
                "Combat contact flash",
                contact,
                Quaternion.identity,
                Vector3.one * (0.12f * scale),
                Vector3.one * (0.72f * scale),
                direction * 0.18f,
                Vector3.zero,
                WithAlpha(bright, 0.92f),
                CombatEffectMaterialKind.Transparent,
                Mathf.Lerp(0.09f, 0.14f, intensity),
                0f);

            switch (profile)
            {
                case CombatImpactProfile.Pathblade:
                    SpawnLine(
                        game,
                        CombatEffectPoolId.ImpactLine,
                        "Pathblade impact cut",
                        contact - direction * (0.64f * scale),
                        contact + direction * (0.82f * scale),
                        0.065f,
                        0.016f,
                        WithAlpha(bright, 0.86f),
                        CombatEffectMaterialKind.Glow,
                        0.17f,
                        direction * 0.38f,
                        Vector3.zero,
                        1.65f);
                    SpawnDirectionalShards(
                        game,
                        contact,
                        direction,
                        color,
                        detail > 1 ? 3 + Mathf.RoundToInt(intensity * 2f) : 2,
                        scale,
                        CombatImpactProfile.Pathblade);
                    TrySpawnGroundRing(game, ground, color, 0.34f, 1.35f * scale, 0.2f);
                    break;

                case CombatImpactProfile.Riftbow:
                    SpawnLine(
                        game,
                        CombatEffectPoolId.ImpactLine,
                        "Riftbow piercing impact",
                        contact - direction * (0.78f * scale),
                        contact + direction * (0.64f * scale),
                        0.038f,
                        0.009f,
                        WithAlpha(bright, 0.9f),
                        CombatEffectMaterialKind.Glow,
                        0.2f,
                        direction * 0.72f,
                        Vector3.zero,
                        1.8f);
                    SpawnDirectionalShards(
                        game,
                        contact,
                        direction,
                        color,
                        detail > 1 ? 4 : 2,
                        scale,
                        CombatImpactProfile.Riftbow);
                    if (intensity > 0.68f)
                    {
                        TrySpawnGroundRing(game, ground, color, 0.26f, 1.2f * scale, 0.18f);
                    }

                    break;

                case CombatImpactProfile.Threadstaff:
                    TrySpawnGroundRing(game, ground, color, 0.42f, 2.15f * scale, 0.27f);
                    var right = Vector3.Cross(Vector3.up, direction);
                    var threads = detail > 1 ? 3 : 2;
                    for (var index = 0; index < threads; index++)
                    {
                        var lateral = (index - (threads - 1) * 0.5f) * 0.18f;
                        SpawnLine(
                            game,
                            CombatEffectPoolId.StaffThread,
                            "Threadstaff impact thread",
                            ground + right * lateral,
                            contact + Vector3.up * (0.22f + index * 0.08f) + right * lateral,
                            0.025f,
                            0.007f,
                            WithAlpha(color, 0.64f),
                            CombatEffectMaterialKind.Glow,
                            0.25f,
                            Vector3.up * 0.3f,
                            new Vector3(0f, 0f, index % 2 == 0 ? 28f : -28f),
                            1.3f);
                    }

                    SpawnDirectionalShards(
                        game,
                        contact,
                        direction,
                        color,
                        detail > 1 ? 3 : 2,
                        scale * 0.78f,
                        CombatImpactProfile.Threadstaff);
                    break;

                case CombatImpactProfile.Slateback:
                    TrySpawnGroundRing(game, ground, color, 0.52f, 2.35f * scale, 0.26f);
                    SpawnAllyDebris(
                        game,
                        ground + Vector3.up * 0.25f,
                        direction,
                        color,
                        detail > 1 ? 5 : 3,
                        true);
                    break;

                case CombatImpactProfile.Player:
                    SpawnDirectionalShards(
                        game,
                        contact,
                        direction,
                        color,
                        detail > 1 ? 3 + Mathf.RoundToInt(intensity) : 2,
                        scale,
                        CombatImpactProfile.Player);
                    if (intensity > 0.72f && !TetherfallAccessibilityRuntime.HighContrastTelegraphs)
                    {
                        TrySpawnGroundRing(game, ground, color, 0.3f, 1.25f, 0.16f);
                    }

                    break;

                default:
                    SpawnDirectionalShards(
                        game,
                        contact,
                        direction,
                        color,
                        detail > 1 ? 3 + Mathf.RoundToInt(intensity * 2f) : 2,
                        scale,
                        CombatImpactProfile.Generic);
                    if (intensity > 0.46f)
                    {
                        TrySpawnGroundRing(game, ground, color, 0.34f, 1.4f * scale, 0.2f);
                    }

                    break;
            }
        }

        private static void SpawnDirectionalShards(
            TetherfallGame game,
            Vector3 position,
            Vector3 direction,
            Color color,
            int count,
            float scale,
            CombatImpactProfile profile)
        {
            direction = NormalizeHorizontal(direction, DeterministicHorizontal(effectSequence++));
            var right = Vector3.Cross(Vector3.up, direction);
            count = Mathf.Clamp(count, 1, 6);
            for (var index = 0; index < count; index++)
            {
                var spread = count == 1
                    ? 0f
                    : (index / (float)(count - 1) - 0.5f) * 1.55f;
                var lift = 0.42f + (index % 3) * 0.22f;
                var velocity = direction * (1.45f + index * 0.18f) +
                               right * spread +
                               Vector3.up * lift;
                var shardScale = GetShardScale(profile, scale, index);
                SpawnEffect(
                    game,
                    profile == CombatImpactProfile.Generic
                        ? CombatEffectPoolId.ImpactShard
                        : CombatEffectPoolId.ImpactShard,
                    "Directional impact shard",
                    position,
                    Quaternion.LookRotation(velocity.normalized, Vector3.up),
                    shardScale,
                    shardScale * 0.42f,
                    velocity * scale,
                    new Vector3(90f + index * 17f, 125f - index * 11f, 68f + index * 9f),
                    WithAlpha(color, profile == CombatImpactProfile.Player ? 0.66f : 0.82f),
                    profile == CombatImpactProfile.Player
                        ? CombatEffectMaterialKind.Transparent
                        : CombatEffectMaterialKind.Glow,
                    0.22f + (index % 3) * 0.035f,
                    profile == CombatImpactProfile.Player ? 0f : 1.35f);
            }
        }

        private static Vector3 GetShardScale(
            CombatImpactProfile profile,
            float scale,
            int index)
        {
            switch (profile)
            {
                case CombatImpactProfile.Pathblade:
                    return new Vector3(0.055f, 0.035f, 0.29f + index % 2 * 0.05f) * scale;
                case CombatImpactProfile.Riftbow:
                    return new Vector3(0.032f, 0.032f, 0.36f + index % 2 * 0.07f) * scale;
                case CombatImpactProfile.Threadstaff:
                    return new Vector3(0.042f, 0.075f, 0.2f) * scale;
                case CombatImpactProfile.Slateback:
                    return new Vector3(0.13f, 0.1f, 0.17f) * scale;
                case CombatImpactProfile.Player:
                    return new Vector3(0.065f, 0.05f, 0.24f) * scale;
                default:
                    return new Vector3(0.065f, 0.06f, 0.25f + index % 2 * 0.04f) * scale;
            }
        }

        private static void TrySpawnGroundRing(
            TetherfallGame game,
            Vector3 position,
            Color color,
            float startDiameter,
            float endDiameter,
            float lifetime)
        {
            if (Time.time < nextGroundRingAt)
            {
                return;
            }

            nextGroundRingAt = Time.time + 0.035f;
            var alpha = TetherfallAccessibilityRuntime.HighContrastTelegraphs ? 0.14f : 0.24f;
            SpawnEffect(
                game,
                CombatEffectPoolId.GroundRing,
                "Brief combat ground ring",
                position,
                Quaternion.identity,
                new Vector3(startDiameter, 0.012f, startDiameter),
                new Vector3(endDiameter, 0.008f, endDiameter),
                Vector3.zero,
                new Vector3(0f, 72f, 0f),
                WithAlpha(color, alpha),
                CombatEffectMaterialKind.Transparent,
                Mathf.Min(0.3f, lifetime),
                0f);
        }

        private static void SpawnStaggerAccent(
            TetherfallGame game,
            Vector3 ground,
            Vector3 contact,
            Color color,
            int detail,
            bool boss)
        {
            var size = boss ? 1.3f : 0.72f;
            var bright = Color.Lerp(color, Color.white, 0.34f);
            SpawnLine(
                game,
                boss ? CombatEffectPoolId.BossShard : CombatEffectPoolId.ImpactLine,
                boss ? "Boss stagger fracture" : "Stagger fracture",
                contact + new Vector3(-size, -size * 0.3f, 0f),
                contact + new Vector3(size, size * 0.3f, 0f),
                boss ? 0.08f : 0.045f,
                0.012f,
                WithAlpha(bright, 0.86f),
                CombatEffectMaterialKind.Glow,
                boss ? 0.3f : 0.22f,
                Vector3.up * 0.14f,
                Vector3.zero,
                1.6f);
            SpawnLine(
                game,
                boss ? CombatEffectPoolId.BossShard : CombatEffectPoolId.ImpactLine,
                boss ? "Boss stagger counter-fracture" : "Stagger counter-fracture",
                contact + new Vector3(0f, -size * 0.28f, -size),
                contact + new Vector3(0f, size * 0.28f, size),
                boss ? 0.08f : 0.045f,
                0.012f,
                WithAlpha(bright, 0.76f),
                CombatEffectMaterialKind.Glow,
                boss ? 0.32f : 0.23f,
                Vector3.up * 0.12f,
                Vector3.zero,
                1.45f);
            if (boss || detail > 1)
            {
                SpawnEffect(
                    game,
                    boss ? CombatEffectPoolId.BossRing : CombatEffectPoolId.GroundRing,
                    boss ? "Boss stagger ground break" : "Stagger ground break",
                    ground,
                    Quaternion.identity,
                    new Vector3(0.45f, 0.012f, 0.45f),
                    new Vector3(boss ? 3.2f : 1.55f, 0.008f, boss ? 3.2f : 1.55f),
                    Vector3.zero,
                    new Vector3(0f, 110f, 0f),
                    WithAlpha(color, TetherfallAccessibilityRuntime.HighContrastTelegraphs ? 0.14f : 0.24f),
                    CombatEffectMaterialKind.Transparent,
                    boss ? 0.34f : 0.24f,
                    0f);
            }
        }

        private static void SpawnBossHitLayer(
            TetherfallGame game,
            Vector3 contact,
            Vector3 direction,
            Color color,
            float intensity,
            int detail)
        {
            var shardCount = detail > 1 ? 2 + Mathf.RoundToInt(intensity * 2f) : 2;
            var right = Vector3.Cross(Vector3.up, NormalizeHorizontal(direction, Vector3.forward));
            for (var index = 0; index < shardCount; index++)
            {
                var side = (index & 1) == 0 ? -1f : 1f;
                var velocity = right * side * (1.1f + index * 0.15f) +
                               Vector3.up * (0.65f + index * 0.18f) +
                               direction * 0.45f;
                SpawnEffect(
                    game,
                    CombatEffectPoolId.BossShard,
                    "Boss core fracture shard",
                    contact,
                    Quaternion.LookRotation(velocity.normalized, Vector3.up),
                    new Vector3(0.085f, 0.07f, 0.34f) * Mathf.Lerp(0.9f, 1.25f, intensity),
                    new Vector3(0.03f, 0.025f, 0.13f),
                    velocity,
                    new Vector3(85f, side * 145f, 110f),
                    WithAlpha(color, 0.84f),
                    CombatEffectMaterialKind.Glow,
                    0.29f + index * 0.025f,
                    1.45f);
            }
        }

        private static void SpawnBossPhaseTransition(
            TetherfallGame game,
            Vector3 position,
            string bossName,
            int phaseIndex,
            int detail)
        {
            var original = ContainsToken(bossName, "Scoria")
                ? new Color(1f, 0.43f, 0.13f)
                : new Color(0.9f, 0.36f, 0.76f);
            var color = TetherfallAccessibilityRuntime.ResolveTelegraphColor(
                original,
                TetherfallAccessibilityColorCue.Warning);
            color.a = 1f;
            var ringCount = detail > 1 ? 3 : 2;
            var phaseScale = phaseIndex >= 3 ? 1.18f : 1f;
            for (var index = 0; index < ringCount; index++)
            {
                var start = 0.65f + index * 0.38f;
                var end = (2.7f + index * 0.8f) * phaseScale;
                SpawnEffect(
                    game,
                    CombatEffectPoolId.BossRing,
                    "Boss phase resonance ring",
                    position + Vector3.up * (0.075f + index * 0.018f),
                    Quaternion.identity,
                    new Vector3(start, 0.014f, start),
                    new Vector3(end, 0.008f, end),
                    Vector3.zero,
                    new Vector3(0f, index % 2 == 0 ? 115f : -115f, 0f),
                    WithAlpha(
                        color,
                        TetherfallAccessibilityRuntime.HighContrastTelegraphs ? 0.14f : 0.24f),
                    CombatEffectMaterialKind.Transparent,
                    0.42f + index * 0.035f,
                    0f);
            }

            var shardCount = detail > 1 ? 6 : 4;
            for (var index = 0; index < shardCount; index++)
            {
                var angle = index * Mathf.PI * 2f / shardCount + phaseIndex * 0.31f;
                var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var origin = position + Vector3.up * (1.3f + index % 2 * 0.55f);
                SpawnEffect(
                    game,
                    CombatEffectPoolId.BossShard,
                    "Boss phase prism",
                    origin,
                    Quaternion.LookRotation(radial + Vector3.up * 0.34f, Vector3.up),
                    new Vector3(0.09f, 0.08f, 0.42f) * phaseScale,
                    new Vector3(0.035f, 0.03f, 0.16f),
                    radial * (1.8f + phaseIndex * 0.2f) + Vector3.up * 0.62f,
                    new Vector3(70f, 130f, 92f),
                    WithAlpha(color, 0.82f),
                    CombatEffectMaterialKind.Glow,
                    0.38f + index % 2 * 0.04f,
                    1.5f);
            }
        }

        private static void SpawnAllySourceCue(
            TetherfallGame game,
            Vector3 source,
            Vector3 direction,
            Color color,
            AllyEffectIdentity identity,
            int detail)
        {
            var size = identity == AllyEffectIdentity.Slateback ? 0.24f : 0.15f;
            SpawnEffect(
                game,
                CombatEffectPoolId.AllyOrb,
                "Expedition ally source cue",
                source,
                Quaternion.identity,
                Vector3.one * size,
                Vector3.one * (size * 1.9f),
                Vector3.up * 0.3f,
                Vector3.zero,
                WithAlpha(color, 0.78f),
                CombatEffectMaterialKind.Glow,
                0.2f,
                1.4f);

            if (identity == AllyEffectIdentity.Cinderwing && detail > 1)
            {
                var right = Vector3.Cross(Vector3.up, NormalizeHorizontal(direction, Vector3.forward));
                SpawnLine(
                    game,
                    CombatEffectPoolId.AllyShard,
                    "Cinderwing release wing",
                    source,
                    source + direction * 0.72f + right * 0.46f,
                    0.055f,
                    0.012f,
                    WithAlpha(color, 0.72f),
                    CombatEffectMaterialKind.Glow,
                    0.2f,
                    Vector3.up * 0.18f,
                    Vector3.zero,
                    1.45f);
                SpawnLine(
                    game,
                    CombatEffectPoolId.AllyShard,
                    "Cinderwing release wing",
                    source,
                    source + direction * 0.72f - right * 0.46f,
                    0.055f,
                    0.012f,
                    WithAlpha(color, 0.72f),
                    CombatEffectMaterialKind.Glow,
                    0.2f,
                    Vector3.up * 0.18f,
                    Vector3.zero,
                    1.45f);
            }
        }

        private static void SpawnAllyTravel(
            TetherfallGame game,
            Vector3 source,
            Vector3 direction,
            float distance,
            Color color,
            AllyEffectIdentity identity,
            int detail)
        {
            var ranged = identity == AllyEffectIdentity.Mistling ||
                         identity == AllyEffectIdentity.Cinderwing ||
                         identity == AllyEffectIdentity.Scout ||
                         identity == AllyEffectIdentity.Medic;
            if (!ranged)
            {
                var side = (effectSequence++ & 1) == 0 ? 1f : -1f;
                SpawnEffect(
                    game,
                    CombatEffectPoolId.AllyShard,
                    identity == AllyEffectIdentity.Slateback
                        ? "Slateback ground wake"
                        : "Expedition ally strike arc",
                    source + direction * 0.48f,
                    Quaternion.LookRotation(NormalizeHorizontal(direction, Vector3.forward), Vector3.up) *
                    Quaternion.Euler(0f, 0f, side * 24f),
                    identity == AllyEffectIdentity.Slateback
                        ? new Vector3(0.16f, 0.08f, 0.72f)
                        : new Vector3(0.05f, 0.42f, 0.86f),
                    identity == AllyEffectIdentity.Slateback
                        ? new Vector3(0.08f, 0.035f, 1.18f)
                        : new Vector3(0.018f, 0.66f, 1.18f),
                    direction * (identity == AllyEffectIdentity.Slateback ? 2.4f : 0.9f),
                    new Vector3(0f, 0f, side * -92f),
                    WithAlpha(color, identity == AllyEffectIdentity.Slateback ? 0.5f : 0.66f),
                    CombatEffectMaterialKind.Transparent,
                    0.22f,
                    0f);
                return;
            }

            var speed = identity == AllyEffectIdentity.Cinderwing ? 29f :
                        identity == AllyEffectIdentity.Mistling ? 20f : 24f;
            var lifetime = Mathf.Clamp(distance / speed, 0.13f, 0.38f);
            var width = identity == AllyEffectIdentity.Mistling ? 0.06f : 0.035f;
            SpawnEffect(
                game,
                CombatEffectPoolId.AllyBolt,
                GetAllyBoltName(identity),
                source + direction * 0.52f,
                Quaternion.LookRotation(direction, Vector3.up),
                new Vector3(width, width, identity == AllyEffectIdentity.Mistling ? 0.84f : 1.08f),
                new Vector3(width * 0.35f, width * 0.35f, 0.42f),
                direction * speed,
                identity == AllyEffectIdentity.Mistling
                    ? new Vector3(0f, 0f, 120f)
                    : Vector3.zero,
                WithAlpha(color, 0.82f),
                CombatEffectMaterialKind.Glow,
                lifetime,
                identity == AllyEffectIdentity.Cinderwing ? 1.75f : 1.4f);

            if (identity == AllyEffectIdentity.Mistling && detail > 1)
            {
                var right = Vector3.Cross(Vector3.up, NormalizeHorizontal(direction, Vector3.forward));
                SpawnEffect(
                    game,
                    CombatEffectPoolId.AllyOrb,
                    "Mistling veil mote",
                    source + right * 0.12f,
                    Quaternion.identity,
                    Vector3.one * 0.11f,
                    Vector3.one * 0.04f,
                    direction * (speed * 0.9f) - right * 0.4f + Vector3.up * 0.22f,
                    Vector3.zero,
                    WithAlpha(color, 0.52f),
                    CombatEffectMaterialKind.Transparent,
                    lifetime,
                    0f);
            }
        }

        private static void SpawnAllyImpact(
            TetherfallGame game,
            Vector3 contact,
            Vector3 ground,
            Vector3 direction,
            Color color,
            AllyEffectIdentity identity,
            float intensity,
            int detail)
        {
            var bright = Color.Lerp(color, Color.white, 0.38f);
            SpawnEffect(
                game,
                CombatEffectPoolId.ContactFlash,
                GetAllyImpactName(identity),
                contact,
                Quaternion.identity,
                Vector3.one * 0.1f,
                Vector3.one * Mathf.Lerp(0.48f, 0.72f, intensity),
                direction * 0.12f,
                Vector3.zero,
                WithAlpha(bright, 0.84f),
                CombatEffectMaterialKind.Transparent,
                0.13f,
                0f);

            switch (identity)
            {
                case AllyEffectIdentity.Bramblet:
                    TrySpawnGroundRing(game, ground, color, 0.38f, 1.8f, 0.28f);
                    SpawnRootThreads(game, ground, contact, direction, color, detail);
                    SpawnAllyDebris(game, contact, direction, color, detail > 1 ? 3 : 2, false);
                    break;

                case AllyEffectIdentity.Slateback:
                    TrySpawnGroundRing(game, ground, color, 0.52f, 2.2f, 0.25f);
                    SpawnAllyDebris(game, ground + Vector3.up * 0.25f, direction, color, detail > 1 ? 5 : 3, true);
                    break;

                case AllyEffectIdentity.Mistling:
                    TrySpawnGroundRing(game, ground, color, 0.34f, 1.7f, 0.28f);
                    SpawnMistlingWisps(game, contact, direction, color, detail);
                    break;

                case AllyEffectIdentity.Cinderwing:
                    SpawnAllyDebris(game, contact, direction, color, detail > 1 ? 5 : 3, false);
                    SpawnLine(
                        game,
                        CombatEffectPoolId.AllyShard,
                        "Cinderwing ember brand",
                        contact - Vector3.up * 0.56f - Vector3.Cross(Vector3.up, direction) * 0.4f,
                        contact + Vector3.up * 0.56f + Vector3.Cross(Vector3.up, direction) * 0.4f,
                        0.05f,
                        0.012f,
                        WithAlpha(color, 0.76f),
                        CombatEffectMaterialKind.Glow,
                        0.22f,
                        Vector3.up * 0.2f,
                        Vector3.zero,
                        1.65f);
                    break;

                case AllyEffectIdentity.Medic:
                    TrySpawnGroundRing(game, ground, color, 0.3f, 1.5f, 0.25f);
                    SpawnMistlingWisps(game, contact, direction, color, 1);
                    break;

                case AllyEffectIdentity.Breaker:
                    SpawnAllyDebris(game, contact, direction, color, detail > 1 ? 4 : 2, true);
                    break;

                case AllyEffectIdentity.Scout:
                    SpawnLine(
                        game,
                        CombatEffectPoolId.AllyShard,
                        "Scout sight mark",
                        contact - Vector3.right * 0.48f,
                        contact + Vector3.right * 0.48f,
                        0.035f,
                        0.008f,
                        WithAlpha(color, 0.78f),
                        CombatEffectMaterialKind.Glow,
                        0.19f,
                        Vector3.up * 0.12f,
                        Vector3.zero,
                        1.45f);
                    break;

                default:
                    SpawnAllyDebris(game, contact, direction, color, detail > 1 ? 3 : 2, false);
                    break;
            }
        }

        private static void SpawnRootThreads(
            TetherfallGame game,
            Vector3 ground,
            Vector3 contact,
            Vector3 direction,
            Color color,
            int detail)
        {
            var right = Vector3.Cross(Vector3.up, NormalizeHorizontal(direction, Vector3.forward));
            var count = detail > 1 ? 3 : 2;
            for (var index = 0; index < count; index++)
            {
                var lateral = (index - (count - 1) * 0.5f) * 0.28f;
                SpawnLine(
                    game,
                    CombatEffectPoolId.AllyShard,
                    "Bramblet root thread",
                    ground + right * lateral,
                    contact + right * lateral + Vector3.up * (0.18f + index * 0.1f),
                    0.045f,
                    0.012f,
                    WithAlpha(color, 0.68f),
                    CombatEffectMaterialKind.Glow,
                    0.3f,
                    Vector3.up * 0.16f,
                    new Vector3(0f, 0f, index % 2 == 0 ? 32f : -32f),
                    1.25f);
            }
        }

        private static void SpawnMistlingWisps(
            TetherfallGame game,
            Vector3 contact,
            Vector3 direction,
            Color color,
            int detail)
        {
            var right = Vector3.Cross(Vector3.up, NormalizeHorizontal(direction, Vector3.forward));
            var count = detail > 1 ? 3 : 2;
            for (var index = 0; index < count; index++)
            {
                var side = index % 2 == 0 ? -1f : 1f;
                SpawnEffect(
                    game,
                    CombatEffectPoolId.AllyOrb,
                    "Mistling impact wisp",
                    contact + right * side * 0.12f,
                    Quaternion.identity,
                    Vector3.one * (0.13f + index * 0.015f),
                    Vector3.one * 0.035f,
                    right * side * (0.7f + index * 0.1f) +
                    Vector3.up * (0.7f + index * 0.14f),
                    Vector3.zero,
                    WithAlpha(color, 0.48f),
                    CombatEffectMaterialKind.Transparent,
                    0.28f + index * 0.035f,
                    0f);
            }
        }

        private static void SpawnAllyDebris(
            TetherfallGame game,
            Vector3 position,
            Vector3 direction,
            Color color,
            int count,
            bool chunky)
        {
            direction = NormalizeHorizontal(direction, Vector3.forward);
            var right = Vector3.Cross(Vector3.up, direction);
            count = Mathf.Clamp(count, 1, 5);
            for (var index = 0; index < count; index++)
            {
                var spread = (index - (count - 1) * 0.5f) * 0.46f;
                var velocity = direction * (1.1f + index * 0.13f) +
                               right * spread +
                               Vector3.up * (chunky ? 0.42f : 0.72f + index * 0.08f);
                var initialScale = chunky
                    ? new Vector3(0.13f, 0.1f, 0.17f)
                    : new Vector3(0.045f, 0.04f, 0.24f);
                SpawnEffect(
                    game,
                    CombatEffectPoolId.AllyShard,
                    chunky ? "Slateback impact debris" : "Pact impact mote",
                    position,
                    Quaternion.LookRotation(velocity.normalized, Vector3.up),
                    initialScale,
                    initialScale * 0.38f,
                    velocity,
                    new Vector3(92f, 135f, 76f),
                    WithAlpha(color, chunky ? 0.72f : 0.82f),
                    chunky
                        ? CombatEffectMaterialKind.Opaque
                        : CombatEffectMaterialKind.Glow,
                    chunky ? 0.32f : 0.25f,
                    chunky ? 0f : 1.45f);
            }
        }

        private static TetherfallCombatEffect SpawnLine(
            TetherfallGame game,
            CombatEffectPoolId poolId,
            string effectName,
            Vector3 start,
            Vector3 end,
            float startWidth,
            float endWidth,
            Color color,
            CombatEffectMaterialKind materialKind,
            float lifetime,
            Vector3 velocity,
            Vector3 angularVelocity,
            float emissionStrength)
        {
            var delta = end - start;
            var distance = delta.magnitude;
            if (distance < 0.001f)
            {
                return null;
            }

            return SpawnEffect(
                game,
                poolId,
                effectName,
                (start + end) * 0.5f,
                Quaternion.LookRotation(delta / distance, Vector3.up),
                new Vector3(startWidth, startWidth, distance),
                new Vector3(endWidth, endWidth, distance * 0.76f),
                velocity,
                angularVelocity,
                color,
                materialKind,
                lifetime,
                emissionStrength);
        }

        private static TetherfallCombatEffect SpawnEffect(
            TetherfallGame game,
            CombatEffectPoolId poolId,
            string effectName,
            Vector3 position,
            Quaternion rotation,
            Vector3 startScale,
            Vector3 endScale,
            Vector3 velocity,
            Vector3 angularVelocity,
            Color color,
            CombatEffectMaterialKind materialKind,
            float lifetime,
            float emissionStrength)
        {
            if (game == null ||
                !IsFinite(position) ||
                !IsFinite(startScale) ||
                !IsFinite(endScale) ||
                !IsFinite(velocity) ||
                !IsFinite(lifetime))
            {
                return null;
            }

            var effect = Acquire(poolId);
            if (effect == null)
            {
                return null;
            }

            color = SanitizeColor(color);
            var materialColor = new Color(color.r, color.g, color.b, 1f);
            Material material;
            switch (materialKind)
            {
                case CombatEffectMaterialKind.Transparent:
                    material = RuntimeEffectMaterialCache.GetTransparent(game, materialColor, 0.58f);
                    break;
                case CombatEffectMaterialKind.Opaque:
                    material = RuntimeEffectMaterialCache.GetOpaque(game, materialColor, 0.02f, 0.34f);
                    break;
                default:
                    material = RuntimeEffectMaterialCache.GetGlow(game, materialColor, 1.55f, 0.58f);
                    break;
            }

            if (material == null)
            {
                effect.RecycleNow();
                return null;
            }

            effect.Activate(
                poolId,
                effectName,
                position,
                rotation,
                ClampScale(startScale),
                ClampScale(endScale),
                velocity,
                angularVelocity,
                color,
                material,
                Mathf.Clamp(lifetime, 0.02f, 0.8f),
                emissionStrength);
            return effect;
        }

        private static TetherfallCombatEffect Acquire(CombatEffectPoolId poolId)
        {
            var pool = GetOrCreatePool(poolId);
            if (activeEffects >= MaximumActiveEffects ||
                pool.Active >= pool.MaximumActive)
            {
                droppedEffects++;
                return null;
            }

            TetherfallCombatEffect effect = null;
            while (pool.Inactive.Count > 0 && effect == null)
            {
                effect = pool.Inactive.Dequeue();
            }

            if (effect == null)
            {
                if (createdEffects >= MaximumCreatedEffects)
                {
                    droppedEffects++;
                    return null;
                }

                var root = GameObject.CreatePrimitive(pool.Primitive);
                root.name = "Pooled combat effect";
                root.layer = IgnoreRaycastLayer;
                RemoveCollider(root);
                var renderer = root.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                effect = root.AddComponent<TetherfallCombatEffect>();
                TrackedEffects.Add(effect);
                pool.Created++;
                createdEffects++;
            }

            pool.Active++;
            activeEffects++;
            effect.gameObject.SetActive(true);
            return effect;
        }

        private static CombatEffectPool GetOrCreatePool(CombatEffectPoolId poolId)
        {
            var index = (int)poolId;
            var pool = Pools[index];
            if (pool != null)
            {
                return pool;
            }

            GetPoolSpecification(poolId, out var primitive, out var maximumActive);
            pool = new CombatEffectPool(primitive, maximumActive);
            Pools[index] = pool;
            return pool;
        }

        private static void GetPoolSpecification(
            CombatEffectPoolId poolId,
            out PrimitiveType primitive,
            out int maximumActive)
        {
            switch (poolId)
            {
                case CombatEffectPoolId.PathRibbon:
                    primitive = PrimitiveType.Cube;
                    maximumActive = 9;
                    break;
                case CombatEffectPoolId.PathCore:
                    primitive = PrimitiveType.Cube;
                    maximumActive = 8;
                    break;
                case CombatEffectPoolId.BowRelease:
                    primitive = PrimitiveType.Cylinder;
                    maximumActive = 8;
                    break;
                case CombatEffectPoolId.BowTracer:
                    primitive = PrimitiveType.Cube;
                    maximumActive = 12;
                    break;
                case CombatEffectPoolId.StaffRing:
                    primitive = PrimitiveType.Cylinder;
                    maximumActive = 12;
                    break;
                case CombatEffectPoolId.StaffThread:
                    primitive = PrimitiveType.Cube;
                    maximumActive = 16;
                    break;
                case CombatEffectPoolId.ContactFlash:
                    primitive = PrimitiveType.Sphere;
                    maximumActive = 20;
                    break;
                case CombatEffectPoolId.ImpactShard:
                    primitive = PrimitiveType.Cube;
                    maximumActive = 34;
                    break;
                case CombatEffectPoolId.GroundRing:
                    primitive = PrimitiveType.Cylinder;
                    maximumActive = 12;
                    break;
                case CombatEffectPoolId.ImpactLine:
                    primitive = PrimitiveType.Cube;
                    maximumActive = 16;
                    break;
                case CombatEffectPoolId.AllyBolt:
                    primitive = PrimitiveType.Cube;
                    maximumActive = 10;
                    break;
                case CombatEffectPoolId.AllyShard:
                    primitive = PrimitiveType.Cube;
                    maximumActive = 24;
                    break;
                case CombatEffectPoolId.AllyOrb:
                    primitive = PrimitiveType.Sphere;
                    maximumActive = 16;
                    break;
                case CombatEffectPoolId.BossRing:
                    primitive = PrimitiveType.Cylinder;
                    maximumActive = 10;
                    break;
                default:
                    primitive = PrimitiveType.Cube;
                    maximumActive = 20;
                    break;
            }
        }

        private static void RecordWeaponTrace(
            WeaponDiscipline weapon,
            Vector3 origin,
            Vector3 direction)
        {
            var sequence = ++weaponTraceSequence;
            var slot = sequence % WeaponTraceCapacity;
            WeaponTraces[slot] = new WeaponTrace
            {
                Weapon = weapon,
                Origin = origin,
                Direction = direction,
                StartedAt = Time.time,
                Sequence = sequence,
                Slateback = false,
                Valid = true
            };
        }

        private static void RecordSlatebackTrace(Vector3 origin, Vector3 direction)
        {
            var sequence = ++weaponTraceSequence;
            var slot = sequence % WeaponTraceCapacity;
            WeaponTraces[slot] = new WeaponTrace
            {
                Weapon = WeaponDiscipline.Pathblade,
                Origin = origin,
                Direction = direction,
                StartedAt = Time.time,
                Sequence = sequence,
                Slateback = true,
                Valid = true
            };
        }

        private static CombatImpactProfile ResolveRecentImpactProfile(
            Vector3 target,
            out Vector3 direction,
            out WeaponDiscipline weapon)
        {
            var now = Time.time;
            var bestScore = float.PositiveInfinity;
            var bestIndex = -1;
            for (var index = 0; index < WeaponTraces.Length; index++)
            {
                var trace = WeaponTraces[index];
                if (!trace.Valid)
                {
                    continue;
                }

                var age = now - trace.StartedAt;
                if (age < -0.01f || age > 2.25f)
                {
                    continue;
                }

                var offset = target - trace.Origin;
                offset.y = 0f;
                var forward = Vector3.Dot(offset, trace.Direction);
                if (forward < -0.4f)
                {
                    continue;
                }

                var lateral = offset - trace.Direction * forward;
                var score = float.PositiveInfinity;
                if (trace.Slateback)
                {
                    if (age <= 0.42f &&
                        forward <= 6.2f &&
                        lateral.sqrMagnitude <= 12.25f)
                    {
                        score = age * 3f + lateral.sqrMagnitude * 0.035f;
                    }
                }
                else
                {
                    switch (trace.Weapon)
                    {
                        case WeaponDiscipline.Pathblade:
                            if (age <= 0.38f &&
                                forward <= 5.6f &&
                                lateral.sqrMagnitude <= 10.24f)
                            {
                                score = age * 4f + lateral.sqrMagnitude * 0.04f;
                            }

                            break;

                        case WeaponDiscipline.Threadstaff:
                            if (age <= 0.42f && offset.sqrMagnitude <= 81f)
                            {
                                score = age * 3f + offset.sqrMagnitude * 0.005f;
                            }

                            break;

                        case WeaponDiscipline.Riftbow:
                            var expectedAge = forward / 28f;
                            var timingError = Mathf.Abs(age - expectedAge);
                            var lateralAllowance = 1.65f + forward * 0.025f;
                            if (forward <= 61f &&
                                timingError <= 0.24f &&
                                lateral.sqrMagnitude <= lateralAllowance * lateralAllowance)
                            {
                                score = timingError * 5f + lateral.sqrMagnitude * 0.02f;
                            }

                            break;
                    }
                }

                score -= trace.Sequence * 0.00001f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = index;
                }
            }

            if (bestIndex < 0)
            {
                direction = Vector3.forward;
                weapon = WeaponDiscipline.Pathblade;
                return CombatImpactProfile.Generic;
            }

            var best = WeaponTraces[bestIndex];
            direction = best.Direction;
            weapon = best.Weapon;
            if (best.Slateback)
            {
                return CombatImpactProfile.Slateback;
            }

            return best.Weapon switch
            {
                WeaponDiscipline.Riftbow => CombatImpactProfile.Riftbow,
                WeaponDiscipline.Threadstaff => CombatImpactProfile.Threadstaff,
                _ => CombatImpactProfile.Pathblade
            };
        }

        private static int GetCrossedBossPhase(
            string bossName,
            float previousRatio,
            float currentRatio)
        {
            var firstThreshold = ContainsToken(bossName, "Scoria") ? 0.7f : 0.5f;
            var secondThreshold = ContainsToken(bossName, "Scoria") ? 0.35f : 0f;
            var phase = 0;
            if (previousRatio > firstThreshold && currentRatio <= firstThreshold)
            {
                phase = 2;
            }

            if (secondThreshold > 0f &&
                previousRatio > secondThreshold &&
                currentRatio <= secondThreshold)
            {
                phase = 3;
            }

            return phase;
        }

        private static AllyEffectIdentity ResolveAllyIdentity(string source)
        {
            if (ContainsToken(source, "Bramblet"))
            {
                return AllyEffectIdentity.Bramblet;
            }

            if (ContainsToken(source, "Slateback"))
            {
                return AllyEffectIdentity.Slateback;
            }

            if (ContainsToken(source, "Mistling"))
            {
                return AllyEffectIdentity.Mistling;
            }

            if (ContainsToken(source, "Cinderwing"))
            {
                return AllyEffectIdentity.Cinderwing;
            }

            if (ContainsToken(source, "scout") || ContainsToken(source, "focus mark"))
            {
                return AllyEffectIdentity.Scout;
            }

            if (ContainsToken(source, "medic") || ContainsToken(source, "field aid"))
            {
                return AllyEffectIdentity.Medic;
            }

            if (ContainsToken(source, "breaker") || ContainsToken(source, "breach"))
            {
                return AllyEffectIdentity.Breaker;
            }

            return AllyEffectIdentity.Fallback;
        }

        private static Color ResolveAllyColor(AllyEffectIdentity identity, Color fallback)
        {
            var original = identity switch
            {
                AllyEffectIdentity.Bramblet => new Color(0.42f, 0.88f, 0.48f),
                AllyEffectIdentity.Slateback => new Color(0.38f, 0.72f, 0.76f),
                AllyEffectIdentity.Mistling => new Color(0.43f, 0.85f, 0.96f),
                AllyEffectIdentity.Cinderwing => new Color(1f, 0.4f, 0.18f),
                _ => SanitizeColor(fallback)
            };
            return ResolveFriendlyColor(original);
        }

        private static string GetAllyBoltName(AllyEffectIdentity identity)
        {
            return identity switch
            {
                AllyEffectIdentity.Cinderwing => "Cinderwing ember dart",
                AllyEffectIdentity.Mistling => "Mistling veil thread",
                AllyEffectIdentity.Medic => "Medic restorative thread",
                AllyEffectIdentity.Scout => "Scout sightline",
                _ => "Expedition ally bolt"
            };
        }

        private static string GetAllyImpactName(AllyEffectIdentity identity)
        {
            return identity switch
            {
                AllyEffectIdentity.Bramblet => "Bramblet root contact",
                AllyEffectIdentity.Slateback => "Slateback stone contact",
                AllyEffectIdentity.Mistling => "Mistling veil contact",
                AllyEffectIdentity.Cinderwing => "Cinderwing ember contact",
                AllyEffectIdentity.Medic => "Medic thread contact",
                AllyEffectIdentity.Scout => "Scout mark contact",
                AllyEffectIdentity.Breaker => "Breaker impact contact",
                _ => "Expedition ally contact"
            };
        }

        private static Color ResolveWeaponColor(WeaponDiscipline weapon)
        {
            var original = weapon switch
            {
                WeaponDiscipline.Riftbow => new Color(0.43f, 0.94f, 0.86f),
                WeaponDiscipline.Threadstaff => new Color(0.55f, 0.72f, 1f),
                _ => new Color(0.94f, 0.68f, 0.33f)
            };
            return ResolveFriendlyColor(original);
        }

        private static Color ResolveImpactColor(
            CombatImpactProfile profile,
            WeaponDiscipline weapon,
            Color fallback)
        {
            if (profile == CombatImpactProfile.Slateback)
            {
                return ResolveAllyColor(AllyEffectIdentity.Slateback, fallback);
            }

            return profile == CombatImpactProfile.Generic
                ? ResolveFriendlyColor(fallback)
                : ResolveWeaponColor(weapon);
        }

        private static Color ResolveFriendlyColor(Color color)
        {
            var resolved = TetherfallAccessibilityRuntime.ResolveColorCue(
                SanitizeColor(color),
                TetherfallAccessibilityColorCue.Friendly);
            resolved.a = 1f;
            return resolved;
        }

        private static Color ResolveHostileColor(Color color)
        {
            var resolved = TetherfallAccessibilityRuntime.ResolveColorCue(
                SanitizeColor(color),
                TetherfallAccessibilityColorCue.Hostile);
            resolved.a = 1f;
            return resolved;
        }

        private static int GetDetailLevel(TetherfallGame game, Vector3 position)
        {
            if (game == null)
            {
                return 0;
            }

            var offset = position - GetObserverPosition(game);
            var distanceSquared = offset.sqrMagnitude;
            if (distanceSquared > MaximumEffectDistance * MaximumEffectDistance)
            {
                return 0;
            }

            return distanceSquared > FullDetailDistance * FullDetailDistance ? 1 : 2;
        }

        private static Vector3 GetObserverPosition(TetherfallGame game)
        {
            var camera = Camera.main;
            if (camera != null)
            {
                return camera.transform.position;
            }

            return game != null && game.PlayerTransform != null
                ? game.PlayerTransform.position
                : Vector3.zero;
        }

        private static Vector3 ResolveGroundPosition(Vector3 position)
        {
            if (Physics.Raycast(
                    position + Vector3.up * 0.28f,
                    Vector3.down,
                    out var hit,
                    3.6f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * 0.045f;
            }

            return position;
        }

        private static Vector3 NormalizeHorizontal(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (!IsFinite(direction) || direction.sqrMagnitude < 0.0001f)
            {
                fallback.y = 0f;
                return fallback.sqrMagnitude < 0.0001f ? Vector3.forward : fallback.normalized;
            }

            return direction.normalized;
        }

        private static Vector3 NormalizeDirection(Vector3 direction, Vector3 fallback)
        {
            if (!IsFinite(direction) || direction.sqrMagnitude < 0.0001f)
            {
                return fallback.sqrMagnitude < 0.0001f ? Vector3.forward : fallback.normalized;
            }

            return direction.normalized;
        }

        private static Vector3 DeterministicHorizontal(int sequence)
        {
            var angle = (sequence * 137.50776f + 23f) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static Color SanitizeColor(Color color)
        {
            if (!IsFinite(color.r) ||
                !IsFinite(color.g) ||
                !IsFinite(color.b) ||
                !IsFinite(color.a))
            {
                return Color.white;
            }

            return new Color(
                Mathf.Clamp01(color.r),
                Mathf.Clamp01(color.g),
                Mathf.Clamp01(color.b),
                Mathf.Clamp01(color.a));
        }

        private static Vector3 ClampScale(Vector3 scale)
        {
            return new Vector3(
                Mathf.Clamp(Mathf.Abs(scale.x), 0.001f, 12f),
                Mathf.Clamp(Mathf.Abs(scale.y), 0.001f, 12f),
                Mathf.Clamp(Mathf.Abs(scale.z), 0.001f, 12f));
        }

        private static bool ContainsToken(string value, string token)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void RemoveCollider(GameObject root)
        {
            var collider = root.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            collider.enabled = false;
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(collider);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void ClearAllEffects()
        {
            resetting = true;
            foreach (var effect in TrackedEffects)
            {
                if (effect == null)
                {
                    continue;
                }

                effect.SuppressDestroyNotification();
                DestroyObject(effect.gameObject);
            }

            TrackedEffects.Clear();
            for (var index = 0; index < Pools.Length; index++)
            {
                Pools[index] = null;
            }

            activeEffects = 0;
            createdEffects = 0;
            resetting = false;
        }

        private static void DestroyObject(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(item);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(item);
            }
        }
    }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(80)]
    public sealed class TetherfallCombatEffectsDriver : MonoBehaviour
    {
        private TetherfallGame game;
        private DungeonBoss observedBoss;
        private bool wasStaggered;

        internal void Configure(TetherfallGame owner)
        {
            game = owner;
        }

        internal void ObserveBoss(DungeonBoss boss)
        {
            if (boss == null)
            {
                observedBoss = null;
                wasStaggered = false;
                return;
            }

            if (observedBoss != boss)
            {
                observedBoss = boss;
                wasStaggered = false;
            }

            EvaluateStagger();
        }

        private void Update()
        {
            if (game == null || game.GameplayPaused || observedBoss == null)
            {
                return;
            }

            EvaluateStagger();
        }

        private void EvaluateStagger()
        {
            if (observedBoss == null)
            {
                wasStaggered = false;
                return;
            }

            var staggered = observedBoss.IsStaggered;
            if (staggered && !wasStaggered)
            {
                TetherfallCombatEffectsV2.SpawnBossStagger(game, observedBoss);
            }

            wasStaggered = staggered;
        }
    }
}
