using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public enum CombatImpactKind
    {
        Light,
        Heavy,
        PerfectGuard,
        Threadlink,
        Overtone,
        Controller,
        PassiveAlly
    }

    public enum EncounterScalingContext
    {
        Ambient,
        CampFirstClear,
        CampRepeat,
        WildAccordFirstClear,
        WildAccordRepeat,
        Elite,
        BossFirstClear,
        BossRepeat
    }

    public struct CombatPoiseProfile
    {
        public CombatPoiseProfile(
            float threshold,
            float recoveryPerSecond,
            float recoveryDelaySeconds)
        {
            Threshold = threshold;
            RecoveryPerSecond = recoveryPerSecond;
            RecoveryDelaySeconds = recoveryDelaySeconds;
        }

        public float Threshold { get; }
        public float RecoveryPerSecond { get; }
        public float RecoveryDelaySeconds { get; }
    }

    public struct EncounterScalingBand
    {
        public EncounterScalingBand(float healthMultiplier, float damageMultiplier)
        {
            HealthMultiplier = healthMultiplier;
            DamageMultiplier = damageMultiplier;
        }

        public float HealthMultiplier { get; }
        public float DamageMultiplier { get; }
    }

    public struct CombatTargetCycleKey
    {
        public CombatTargetCycleKey(
            float distanceSquared,
            float azimuth,
            string stableId,
            Vector3 position,
            int kindOrder,
            int runtimeId)
        {
            DistanceSquared = distanceSquared;
            Azimuth = azimuth;
            StableId = stableId ?? string.Empty;
            Position = position;
            KindOrder = kindOrder;
            RuntimeId = runtimeId;
        }

        public float DistanceSquared { get; }
        public float Azimuth { get; }
        public string StableId { get; }
        public Vector3 Position { get; }
        public int KindOrder { get; }
        public int RuntimeId { get; }
    }

    public static class TetherfallCombatQualityRules
    {
        public const float EnemyExposeDamageMultiplier = 1.28f;
        public const float BossExposeDamageMultiplier = 1.15f;
        public const float BossMaximumExposeSeconds = 8f;
        public const float EnemyMaximumStaggerSeconds = 1.35f;
        public const float BossMaximumStaggerSeconds = 0.85f;
        public const float MaximumEnemyHealthScale = 1.42f;
        public const float MaximumEnemyDamageScale = 1.22f;
        public const float MaximumBossHealthScale = 1.2f;
        public const float MaximumBossDamageScale = 1.12f;

        public static CombatPoiseProfile GetEnemyPoiseProfile(
            EnemyAttackStyle attackStyle,
            bool elite,
            bool hasSpecialMove)
        {
            var threshold = attackStyle switch
            {
                EnemyAttackStyle.Ranged => 48f,
                EnemyAttackStyle.Heavy => 92f,
                _ => 62f
            };
            if (hasSpecialMove)
            {
                threshold *= 1.08f;
            }

            if (elite)
            {
                threshold *= 1.32f;
            }

            var recoveryRate = threshold *
                               (attackStyle == EnemyAttackStyle.Heavy ? 0.4f : 0.52f);
            return new CombatPoiseProfile(
                Mathf.Clamp(threshold, 40f, 164f),
                Mathf.Clamp(recoveryRate, 20f, 72f),
                attackStyle == EnemyAttackStyle.Heavy || elite ? 1f : 0.82f);
        }

        public static CombatPoiseProfile GetBossPoiseProfile()
        {
            return new CombatPoiseProfile(148f, 48f, 1.05f);
        }

        public static float CalculatePoiseContribution(
            int damage,
            int maximumHealth,
            float threshold,
            CombatImpactKind impactKind,
            bool protectedWindup,
            bool boss)
        {
            if (damage <= 0 || maximumHealth <= 0 || threshold <= 0f)
            {
                return 0f;
            }

            var healthFraction = Mathf.Clamp(damage / (float)maximumHealth, 0f, 0.5f);
            var contribution = 7f + damage * 0.5f + threshold * healthFraction * 0.28f;
            contribution = impactKind switch
            {
                CombatImpactKind.Heavy => contribution * 1.8f + threshold * 0.32f,
                CombatImpactKind.PerfectGuard => threshold,
                CombatImpactKind.Threadlink => contribution * 1.35f + threshold * 0.28f,
                CombatImpactKind.Overtone => contribution * 1.25f + threshold * 0.2f,
                CombatImpactKind.Controller => contribution * 1.2f + threshold * 0.22f,
                CombatImpactKind.PassiveAlly => contribution * 0.42f,
                _ => contribution
            };
            if (boss && impactKind != CombatImpactKind.PerfectGuard)
            {
                contribution *= 0.72f;
            }

            if (protectedWindup && !CanBreakProtectedWindup(impactKind))
            {
                contribution = Mathf.Min(contribution, threshold * 0.16f);
            }

            return Mathf.Clamp(contribution, 0f, threshold);
        }

        public static bool CanBreakProtectedWindup(CombatImpactKind impactKind)
        {
            return impactKind != CombatImpactKind.PassiveAlly;
        }

        public static float GetThresholdStaggerSeconds(
            CombatImpactKind impactKind,
            bool boss)
        {
            var duration = impactKind switch
            {
                CombatImpactKind.Heavy => 0.56f,
                CombatImpactKind.PerfectGuard => 1.05f,
                CombatImpactKind.Threadlink => 0.62f,
                CombatImpactKind.Overtone => 0.46f,
                CombatImpactKind.Controller => 0.48f,
                CombatImpactKind.PassiveAlly => 0.18f,
                _ => 0.28f
            };
            return boss
                ? Mathf.Min(BossMaximumStaggerSeconds, duration * 0.62f)
                : Mathf.Min(EnemyMaximumStaggerSeconds, duration);
        }

        public static EncounterScalingBand GetEnemyScalingBand(
            int wayfinderRank,
            EncounterScalingContext context,
            bool elite)
        {
            var rankBand = Mathf.Clamp((Mathf.Max(1, wayfinderRank) - 1) / 2, 0, 5);
            if (rankBand == 0)
            {
                return new EncounterScalingBand(1f, 1f);
            }

            var healthPerBand = 0.035f;
            var damagePerBand = 0.02f;
            switch (context)
            {
                case EncounterScalingContext.CampFirstClear:
                    healthPerBand = 0.05f;
                    damagePerBand = 0.0275f;
                    break;
                case EncounterScalingContext.CampRepeat:
                    healthPerBand = 0.07f;
                    damagePerBand = 0.04f;
                    break;
                case EncounterScalingContext.WildAccordFirstClear:
                    healthPerBand = 0.025f;
                    damagePerBand = 0.015f;
                    break;
                case EncounterScalingContext.WildAccordRepeat:
                    healthPerBand = 0.035f;
                    damagePerBand = 0.022f;
                    break;
                case EncounterScalingContext.Elite:
                    healthPerBand = 0.065f;
                    damagePerBand = 0.035f;
                    break;
            }

            var healthMultiplier = 1f + healthPerBand * rankBand;
            var damageMultiplier = 1f + damagePerBand * rankBand;
            if (elite)
            {
                healthMultiplier += 0.03f * rankBand;
                damageMultiplier += 0.015f * rankBand;
            }

            return new EncounterScalingBand(
                Mathf.Clamp(healthMultiplier, 1f, MaximumEnemyHealthScale),
                Mathf.Clamp(damageMultiplier, 1f, MaximumEnemyDamageScale));
        }

        public static EncounterScalingBand GetBossScalingBand(
            int wayfinderRank,
            bool repeatEncounter)
        {
            var rankBand = Mathf.Clamp((Mathf.Max(1, wayfinderRank) - 1) / 2, 0, 5);
            if (rankBand == 0)
            {
                return new EncounterScalingBand(1f, 1f);
            }

            var healthMultiplier = 1f + rankBand * (repeatEncounter ? 0.04f : 0.03f);
            var damageMultiplier = 1f + rankBand * (repeatEncounter ? 0.024f : 0.018f);
            return new EncounterScalingBand(
                Mathf.Clamp(healthMultiplier, 1f, MaximumBossHealthScale),
                Mathf.Clamp(damageMultiplier, 1f, MaximumBossDamageScale));
        }

        public static int GetThreatBand(
            EncounterScalingBand scaling,
            bool boss)
        {
            var maximumHealthScale = boss
                ? MaximumBossHealthScale
                : MaximumEnemyHealthScale;
            var maximumDamageScale = boss
                ? MaximumBossDamageScale
                : MaximumEnemyDamageScale;
            var healthProgress = NormalizeThreatScale(
                scaling.HealthMultiplier,
                maximumHealthScale);
            var damageProgress = NormalizeThreatScale(
                scaling.DamageMultiplier,
                maximumDamageScale);
            return Mathf.Clamp(
                1 + Mathf.FloorToInt(
                    Mathf.Max(healthProgress, damageProgress) * 4f + 0.001f),
                1,
                5);
        }

        public static float ClampBossExposeDuration(float duration)
        {
            return Mathf.Clamp(duration, 0f, BossMaximumExposeSeconds);
        }

        public static bool ShouldRetainAttackIntent(
            bool gameplayPaused,
            bool combatHitstopActive,
            bool gameplayInputContextAvailable)
        {
            return gameplayInputContextAvailable &&
                   (!gameplayPaused || combatHitstopActive);
        }

        public static float GetTargetCycleAzimuth(Vector3 origin, Vector3 target)
        {
            var delta = target - origin;
            var azimuth = Mathf.Atan2(delta.z, delta.x);
            return azimuth < 0f ? azimuth + Mathf.PI * 2f : azimuth;
        }

        private static float NormalizeThreatScale(float value, float maximum)
        {
            if (float.IsNaN(value) ||
                float.IsInfinity(value) ||
                maximum <= 1f)
            {
                return 0f;
            }

            return Mathf.Clamp01((Mathf.Clamp(value, 1f, maximum) - 1f) / (maximum - 1f));
        }

        public static int CompareTargetCycleKeys(
            CombatTargetCycleKey left,
            CombatTargetCycleKey right)
        {
            var comparison = left.DistanceSquared.CompareTo(right.DistanceSquared);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.Azimuth.CompareTo(right.Azimuth);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.KindOrder.CompareTo(right.KindOrder);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = string.CompareOrdinal(left.StableId, right.StableId);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.Position.x.CompareTo(right.Position.x);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.Position.y.CompareTo(right.Position.y);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.Position.z.CompareTo(right.Position.z);
            return comparison != 0
                ? comparison
                : left.RuntimeId.CompareTo(right.RuntimeId);
        }
    }

    public sealed partial class TetherfallGame
    {
        private const float MaximumStamina = 100f;
        private const float StaminaRegenerationPerSecond = 30f;
        private const float StaminaRegenerationDelay = 0.28f;
        private const float StaminaWarningCooldown = 0.8f;
        private const float AttackInputBufferSeconds = 0.16f;
        private const float PerfectGuardRepeatSuppression = 0.06f;
        private const float PerfectGuardResolutionLockSeconds = 0.3f;
        private const float EnemyCombatFocusAcquireRange = 23f;
        private const float BossCombatFocusAcquireRange = 36f;
        private const float CombatFocusReleaseRange = 48f;

        private readonly Collider[] perfectGuardHitBuffer = new Collider[64];
        private readonly Collider[] combatFocusHitBuffer = new Collider[96];
        private readonly CombatFocusCandidate[] combatFocusCandidates =
            new CombatFocusCandidate[96];
        private readonly HashSet<Enemy> perfectGuardAffectedEnemies = new HashSet<Enemy>();
        private float stamina;
        private float lastStaminaUseAt;
        private float nextStaminaWarningAt;
        private float attackInputBufferedUntil;
        private float lastPerfectGuardAt = float.NegativeInfinity;
        private float perfectGuardResolutionLockUntil;
        private int attackInputBufferedFrame = -1;
        private int attackInputSwingSequence;
        private int combatSwingSequence;
        private int observedGuardPressFrame = -1;
        private bool perfectGuardResolvedForCurrentPress;
        private Enemy lockedEnemy;
        private DungeonBoss lockedBoss;
        private CombatTargetIndicator targetIndicator;

        public float Stamina => stamina;
        public float MaxStamina => MaximumStamina + StaminaBonus;
        public Transform CombatTarget
        {
            get
            {
                if (IsCombatTargetAvailable(lockedEnemy))
                {
                    return lockedEnemy.transform;
                }

                if (IsCombatTargetAvailable(lockedBoss))
                {
                    return lockedBoss.transform;
                }

                return null;
            }
        }

        private void InitializeCombatSystems()
        {
            stamina = Mathf.Max(0f, MaxStamina);
            lastStaminaUseAt = Time.time - StaminaRegenerationDelay;
            nextStaminaWarningAt = 0f;
            attackInputBufferedUntil = 0f;
            attackInputBufferedFrame = -1;
            attackInputSwingSequence = combatSwingSequence;
            observedGuardPressFrame = -1;
            perfectGuardResolvedForCurrentPress = false;
            lastPerfectGuardAt = float.NegativeInfinity;
            perfectGuardResolutionLockUntil = 0f;
        }

        private void UpdateCombatSystems()
        {
            if (GameplayPaused)
            {
                UpdateAttackInputBuffer();
                lastStaminaUseAt += Time.deltaTime;
                if (!float.IsNegativeInfinity(lastPerfectGuardAt))
                {
                    lastPerfectGuardAt += Time.deltaTime;
                }

                if (perfectGuardResolutionLockUntil > Time.time)
                {
                    perfectGuardResolutionLockUntil += Time.deltaTime;
                }

                return;
            }

            ObserveGuardInput();
            UpdateAttackInputBuffer();
            ValidateCombatTarget();
            var maximum = Mathf.Max(0f, MaxStamina);
            if (float.IsNaN(stamina) || float.IsInfinity(stamina))
            {
                stamina = maximum;
            }
            else
            {
                stamina = Mathf.Clamp(stamina, 0f, maximum);
            }

            if (GameplayPaused)
            {
                lastStaminaUseAt += Time.deltaTime;
            }
            else if (Time.time >= lastStaminaUseAt + StaminaRegenerationDelay &&
                     stamina < maximum)
            {
                var regeneration = Mathf.Max(0f, StaminaRegenerationPerSecond + StaminaRegenerationBonus);
                stamina = Mathf.Min(maximum, stamina + regeneration * Time.deltaTime);
            }

            if (targetIndicator != null)
            {
                targetIndicator.SetTarget(CombatTarget);
            }
        }

        public bool TrySpendStamina(float amount, bool warnWhenEmpty = false)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount))
            {
                return false;
            }

            if (amount <= 0f)
            {
                return true;
            }

            if (stamina + 0.001f < amount)
            {
                // A held traversal input must be released before regeneration can
                // resume; otherwise tiny regenerated amounts produce one-frame
                // sprint/glide/climb flicker.
                lastStaminaUseAt = Time.time;
                if (warnWhenEmpty && Time.time >= nextStaminaWarningAt)
                {
                    nextStaminaWarningAt = Time.time + StaminaWarningCooldown;
                    Notify("Catch your breath before that maneuver.");
                }

                return false;
            }

            stamina = Mathf.Max(0f, stamina - amount);
            lastStaminaUseAt = Time.time;
            return true;
        }

        public void RestoreStamina(float amount)
        {
            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
            {
                return;
            }

            stamina = Mathf.Clamp(stamina + amount, 0f, Mathf.Max(0f, MaxStamina));
        }

        public void RestoreAllStamina()
        {
            stamina = Mathf.Max(0f, MaxStamina);
            lastStaminaUseAt = Time.time - StaminaRegenerationDelay;
        }

        public void ToggleCombatTarget()
        {
            if (PlayerTransform == null)
            {
                return;
            }

            var currentTarget = CombatTarget;
            var candidateCount = CollectCombatFocusCandidates(currentTarget);
            if (candidateCount == 0)
            {
                if (currentTarget != null)
                {
                    ClearCombatTarget();
                    Notify("Threadglass focus released.");
                }
                else
                {
                    Notify("No hostile is close enough to focus.");
                }

                return;
            }

            var nextIndex = 0;
            if (currentTarget != null)
            {
                var currentIndex = FindCombatFocusCandidate(currentTarget, candidateCount);
                if (currentIndex >= 0)
                {
                    nextIndex = currentIndex + 1;
                    if (nextIndex >= candidateCount)
                    {
                        ClearCombatFocusCandidateBuffer(candidateCount);
                        ClearCombatTarget();
                        Notify("Threadglass focus released.");
                        return;
                    }
                }
            }

            var candidate = combatFocusCandidates[nextIndex];
            ClearCombatFocusCandidateBuffer(candidateCount);
            SetCombatTarget(candidate.Enemy, candidate.Boss);
        }

        public Vector3 GetCombatAimDirection(Vector3 origin, Vector3 fallback)
        {
            var target = CombatTarget;
            if (target == null)
            {
                return fallback.sqrMagnitude > 0.01f ? fallback.normalized : PlayerTransform.forward;
            }

            var direction = target.position - origin;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                return fallback.sqrMagnitude > 0.01f ? fallback.normalized : PlayerTransform.forward;
            }

            return direction.normalized;
        }

        public void TriggerPerfectGuard(Vector3 position)
        {
            ObserveGuardInput();
            if ((perfectGuardResolvedForCurrentPress &&
                 Time.time < perfectGuardResolutionLockUntil) ||
                Time.time < lastPerfectGuardAt + PerfectGuardRepeatSuppression)
            {
                return;
            }

            perfectGuardResolvedForCurrentPress = true;
            perfectGuardResolutionLockUntil = Time.time + PerfectGuardResolutionLockSeconds;
            lastPerfectGuardAt = Time.time;
            var staggered = 0;
            perfectGuardAffectedEnemies.Clear();
            var colliderCount = Physics.OverlapSphereNonAlloc(
                position,
                4.4f,
                perfectGuardHitBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (var colliderIndex = 0; colliderIndex < colliderCount; colliderIndex++)
            {
                var collider = perfectGuardHitBuffer[colliderIndex];
                if (collider == null)
                {
                    continue;
                }

                var enemy = collider.GetComponentInParent<Enemy>();
                if (IsCombatTargetAvailable(enemy) && perfectGuardAffectedEnemies.Add(enemy))
                {
                    if (enemy.TryBreakAshguardFromPerfectGuard() || enemy.ApplyStagger(1.05f))
                    {
                        staggered++;
                    }

                    RegisterOvertone(enemy, OvertonePitch.Bind, "Perfect guard");
                }
            }

            for (var colliderIndex = 0; colliderIndex < colliderCount; colliderIndex++)
            {
                perfectGuardHitBuffer[colliderIndex] = null;
            }

            perfectGuardAffectedEnemies.Clear();
            var boss = ActiveDungeonBoss;
            if (IsCombatTargetAvailable(boss) &&
                Vector3.Distance(position, boss.transform.position) <= 5.5f)
            {
                boss.ApplyStagger(0.7f);
                RegisterOvertone(boss, OvertonePitch.Bind, "Perfect guard");
                staggered++;
            }

            TimedPulse.Spawn(
                PrimitiveType.Cylinder,
                "Perfect guard pulse",
                position + Vector3.up * 0.05f,
                new Vector3(0.35f, 0.035f, 0.35f),
                RuntimeEffectMaterialCache.GetTransparent(this, new Color(0.54f, 0.94f, 0.83f, 0.64f), 0.72f),
                0.3f,
                5.8f);
            cameraRig?.AddImpulse(0.13f, 0.12f);
            Notify(staggered > 0
                ? $"Perfect guard. {staggered} hostile{(staggered == 1 ? string.Empty : "s")} staggered."
                : "Perfect guard. The impact is turned aside.");
        }

        public string GetCombatStatusText()
        {
            var focusText = "Focus unbound";
            if (lockedEnemy != null && !lockedEnemy.IsDefeated)
            {
                focusText = $"Focus {lockedEnemy.EnemyName}";
            }
            else if (lockedBoss != null && !lockedBoss.IsDefeated)
            {
                focusText = $"Focus {lockedBoss.BossName}";
            }

            var mealEffect = GetMealEffectStatus();
            var wardStatus = GetExpeditionWardStatus();
            if (string.IsNullOrEmpty(mealEffect))
            {
                return string.IsNullOrEmpty(wardStatus) ? focusText : $"{focusText} · {wardStatus}";
            }

            return string.IsNullOrEmpty(wardStatus)
                ? $"{focusText} · {mealEffect}"
                : $"{focusText} · {mealEffect} · {wardStatus}";
        }

        private void ValidateCombatTarget()
        {
            if (!HasCombatTargetReference())
            {
                return;
            }

            var target = CombatTarget;
            if (target == null ||
                PlayerTransform == null ||
                Vector3.Distance(PlayerTransform.position, target.position) > CombatFocusReleaseRange)
            {
                ClearCombatTarget();
            }
        }

        private void UpdateAttackInputBuffer()
        {
            var inputContextAvailable =
                Player != null &&
                !CursorLockAcquiredThisFrame &&
                Cursor.lockState == CursorLockMode.Locked &&
                Application.isFocused &&
                !CodexOpen &&
                activePanel == GamePanel.None &&
                !IsPlayerSettlementInputCaptured &&
                !TetherfallInputBindings.Shared.WasPressedThisFrame(
                    TetherfallInputAction.OpenSettlementPlanning);
            if (!TetherfallCombatQualityRules.ShouldRetainAttackIntent(
                    GameplayPaused,
                    CombatHitstopActive,
                    inputContextAvailable))
            {
                attackInputBufferedUntil = 0f;
                attackInputBufferedFrame = -1;
                return;
            }

            if (TetherfallInputBindings.Shared.GetButtonDown(TetherfallInputAction.Attack))
            {
                attackInputBufferedUntil = Time.time + AttackInputBufferSeconds;
                attackInputBufferedFrame = Time.frameCount;
                attackInputSwingSequence = combatSwingSequence;
            }

            if (CombatHitstopActive)
            {
                return;
            }

            if (attackInputBufferedUntil <= 0f)
            {
                return;
            }

            if (attackInputSwingSequence != combatSwingSequence ||
                Time.time > attackInputBufferedUntil)
            {
                attackInputBufferedUntil = 0f;
                attackInputBufferedFrame = -1;
                return;
            }

            // The existing game-loop input owns the press frame. Retrying only
            // on later frames preserves weapon-swap ordering while accepting a
            // press made just before an attack/guard/dodge lock ends.
            if (Time.frameCount > attackInputBufferedFrame)
            {
                Player.TryAttack();
                if (attackInputSwingSequence != combatSwingSequence)
                {
                    attackInputBufferedUntil = 0f;
                    attackInputBufferedFrame = -1;
                }
            }
        }

        private void ObserveGuardInput()
        {
            if (TetherfallInputBindings.Shared.GetButtonDown(TetherfallInputAction.Guard) &&
                observedGuardPressFrame != Time.frameCount)
            {
                observedGuardPressFrame = Time.frameCount;
                perfectGuardResolvedForCurrentPress = false;
                perfectGuardResolutionLockUntil = 0f;
            }
        }

        private bool HasCombatTargetReference()
        {
            // Unity's destroyed-object null overload hides the managed reference.
            // ReferenceEquals lets validation clear that stale lock exactly once.
            return !ReferenceEquals(lockedEnemy, null) || !ReferenceEquals(lockedBoss, null);
        }

        private static bool IsCombatTargetAvailable(Enemy enemy)
        {
            return enemy != null && !enemy.IsDefeated && enemy.gameObject.activeInHierarchy;
        }

        private static bool IsCombatTargetAvailable(DungeonBoss boss)
        {
            return boss != null && !boss.IsDefeated && boss.gameObject.activeInHierarchy;
        }

        private int CollectCombatFocusCandidates(Transform currentTarget)
        {
            var playerPosition = PlayerTransform.position;
            var candidateCount = 0;
            var hitCount = Physics.OverlapSphereNonAlloc(
                playerPosition,
                BossCombatFocusAcquireRange,
                combatFocusHitBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);
            var boundedHitCount = Mathf.Min(hitCount, combatFocusHitBuffer.Length);
            for (var hitIndex = 0; hitIndex < boundedHitCount; hitIndex++)
            {
                var collider = combatFocusHitBuffer[hitIndex];
                if (collider == null)
                {
                    continue;
                }

                var enemy = collider.GetComponentInParent<Enemy>();
                if (IsCombatTargetAvailable(enemy) &&
                    HorizontalDistanceSquared(playerPosition, enemy.transform.position) <=
                    EnemyCombatFocusAcquireRange * EnemyCombatFocusAcquireRange)
                {
                    candidateCount = AddCombatFocusCandidate(
                        candidateCount,
                        enemy,
                        null,
                        playerPosition);
                }

                var boss = collider.GetComponentInParent<DungeonBoss>();
                if (IsCombatTargetAvailable(boss) &&
                    HorizontalDistanceSquared(playerPosition, boss.transform.position) <=
                    BossCombatFocusAcquireRange * BossCombatFocusAcquireRange)
                {
                    candidateCount = AddCombatFocusCandidate(
                        candidateCount,
                        null,
                        boss,
                        playerPosition);
                }
            }

            for (var hitIndex = 0; hitIndex < boundedHitCount; hitIndex++)
            {
                combatFocusHitBuffer[hitIndex] = null;
            }

            if (IsCombatTargetAvailable(ActiveDungeonBoss) &&
                HorizontalDistanceSquared(
                    playerPosition,
                    ActiveDungeonBoss.transform.position) <=
                BossCombatFocusAcquireRange * BossCombatFocusAcquireRange)
            {
                candidateCount = AddCombatFocusCandidate(
                    candidateCount,
                    null,
                    ActiveDungeonBoss,
                    playerPosition);
            }

            if (currentTarget != null &&
                HorizontalDistanceSquared(playerPosition, currentTarget.position) <=
                CombatFocusReleaseRange * CombatFocusReleaseRange)
            {
                candidateCount = AddCombatFocusCandidate(
                    candidateCount,
                    currentTarget.GetComponent<Enemy>(),
                    currentTarget.GetComponent<DungeonBoss>(),
                    playerPosition);
            }

            for (var index = 1; index < candidateCount; index++)
            {
                var candidate = combatFocusCandidates[index];
                var insertionIndex = index - 1;
                while (insertionIndex >= 0 &&
                       TetherfallCombatQualityRules.CompareTargetCycleKeys(
                           candidate.Key,
                           combatFocusCandidates[insertionIndex].Key) < 0)
                {
                    combatFocusCandidates[insertionIndex + 1] =
                        combatFocusCandidates[insertionIndex];
                    insertionIndex--;
                }

                combatFocusCandidates[insertionIndex + 1] = candidate;
            }

            return candidateCount;
        }

        private int AddCombatFocusCandidate(
            int candidateCount,
            Enemy enemy,
            DungeonBoss boss,
            Vector3 playerPosition)
        {
            var target = enemy != null
                ? enemy.transform
                : boss != null
                    ? boss.transform
                    : null;
            if (target == null || candidateCount >= combatFocusCandidates.Length)
            {
                return candidateCount;
            }

            for (var index = 0; index < candidateCount; index++)
            {
                if (combatFocusCandidates[index].Target == target)
                {
                    return candidateCount;
                }
            }

            var distanceSquared =
                HorizontalDistanceSquared(playerPosition, target.position);
            combatFocusCandidates[candidateCount] = new CombatFocusCandidate
            {
                Enemy = enemy,
                Boss = boss,
                Target = target,
                Key = new CombatTargetCycleKey(
                    distanceSquared,
                    TetherfallCombatQualityRules.GetTargetCycleAzimuth(
                        playerPosition,
                        target.position),
                    enemy != null ? enemy.EnemyId : boss.BossId,
                    target.position,
                    enemy != null ? 0 : 1,
                    target.GetInstanceID())
            };
            return candidateCount + 1;
        }

        private int FindCombatFocusCandidate(Transform target, int candidateCount)
        {
            for (var index = 0; index < candidateCount; index++)
            {
                if (combatFocusCandidates[index].Target == target)
                {
                    return index;
                }
            }

            return -1;
        }

        private void ClearCombatFocusCandidateBuffer(int candidateCount)
        {
            for (var index = 0; index < candidateCount; index++)
            {
                combatFocusCandidates[index] = default;
            }
        }

        private void SetCombatTarget(Enemy enemy, DungeonBoss boss)
        {
            ClearOvertoneLattice();
            ClearCrossweave();
            lockedEnemy = enemy;
            lockedBoss = boss;
            if (targetIndicator == null)
            {
                targetIndicator = CombatTargetIndicator.Create(this);
            }

            targetIndicator.SetTarget(CombatTarget);
            cameraRig?.SetCombatFocus(CombatTarget);
            Notify($"Threadglass focus: {(lockedEnemy != null ? lockedEnemy.EnemyName : lockedBoss.BossName)}.");
        }

        private static float HorizontalDistanceSquared(Vector3 left, Vector3 right)
        {
            var delta = right - left;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }

        private void ClearCombatTarget()
        {
            lockedEnemy = null;
            lockedBoss = null;
            ClearOvertoneLattice();
            ClearCrossweave();
            if (targetIndicator != null)
            {
                targetIndicator.SetTarget(null);
            }

            cameraRig?.SetCombatFocus(null);
        }

        private struct CombatFocusCandidate
        {
            public Enemy Enemy;
            public DungeonBoss Boss;
            public Transform Target;
            public CombatTargetCycleKey Key;
        }
    }

    public sealed class CombatTargetIndicator : MonoBehaviour
    {
        private TetherfallGame game;
        private Transform target;
        private Transform ring;
        private Transform shardRoot;
        private Material ringMaterial;
        private Material shardMaterial;

        public static CombatTargetIndicator Create(TetherfallGame game)
        {
            var root = new GameObject("Threadglass combat focus");
            root.transform.SetParent(game.transform, false);
            var indicator = root.AddComponent<CombatTargetIndicator>();
            indicator.game = game;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Combat focus ring";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localScale = new Vector3(0.74f, 0.022f, 0.74f);
            indicator.ringMaterial = game.CreateTransparentMaterial(
                new Color(0.45f, 0.96f, 0.82f, 0.58f),
                0.74f);
            ring.GetComponent<Renderer>().sharedMaterial = indicator.ringMaterial;
            Object.Destroy(ring.GetComponent<Collider>());
            indicator.ring = ring.transform;

            indicator.shardRoot = new GameObject("Combat focus shards").transform;
            indicator.shardRoot.SetParent(root.transform, false);
            indicator.shardMaterial = game.CreateGlowMaterial(
                new Color(0.46f, 0.97f, 0.84f),
                1.55f,
                0.65f);
            for (var index = 0; index < 4; index++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Combat focus shard";
                shard.transform.SetParent(indicator.shardRoot, false);
                var angle = index * 90f * Mathf.Deg2Rad;
                shard.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.55f, 0f, Mathf.Sin(angle) * 0.55f);
                shard.transform.localScale = new Vector3(0.12f, 0.12f, 0.3f);
                shard.GetComponent<Renderer>().sharedMaterial = indicator.shardMaterial;
                Object.Destroy(shard.GetComponent<Collider>());
            }

            root.SetActive(false);
            return indicator;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            var shouldBeVisible = target != null;
            if (gameObject.activeSelf != shouldBeVisible)
            {
                gameObject.SetActive(shouldBeVisible);
            }
        }

        private void Update()
        {
            if (target == null || game == null || !target.gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }

            var boss = target.GetComponent<DungeonBoss>();
            transform.position = target.position + Vector3.up * (boss != null ? 4.8f : 2.35f);
            var camera = Camera.main;
            if (camera != null)
            {
                transform.rotation = Quaternion.LookRotation(camera.transform.forward, Vector3.up);
            }

            var pulse = 1f + Mathf.Sin(Time.time * 5.4f) * 0.12f;
            ring.localScale = new Vector3(0.74f * pulse, 0.022f, 0.74f * pulse);
            shardRoot.Rotate(0f, 90f * Time.deltaTime, 0f, Space.Self);
        }

        private void OnDestroy()
        {
            if (ringMaterial != null)
            {
                Destroy(ringMaterial);
            }

            if (shardMaterial != null)
            {
                Destroy(shardMaterial);
            }
        }
    }
}
