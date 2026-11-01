using System;

namespace Tetherfall
{
    public readonly struct InfiniteSpireScaleSnapshot
    {
        public InfiniteSpireScaleSnapshot(
            int floorIndex,
            float concordDamp,
            float enemyHealthMultiplier,
            float enemyDamageMultiplier,
            float enemyExperienceMultiplier,
            float bossHealthMultiplier,
            float bossMoveMultiplier,
            float telegraphDurationMultiplier,
            float rewardMultiplier,
            float treasureChanceBonus)
        {
            FloorIndex = floorIndex;
            ConcordDamp = concordDamp;
            EnemyHealthMultiplier = enemyHealthMultiplier;
            EnemyDamageMultiplier = enemyDamageMultiplier;
            EnemyExperienceMultiplier = enemyExperienceMultiplier;
            BossHealthMultiplier = bossHealthMultiplier;
            BossMoveMultiplier = bossMoveMultiplier;
            TelegraphDurationMultiplier = telegraphDurationMultiplier;
            RewardMultiplier = rewardMultiplier;
            TreasureChanceBonus = treasureChanceBonus;
        }

        public int FloorIndex { get; }
        public float ConcordDamp { get; }
        public float EnemyHealthMultiplier { get; }
        public float EnemyDamageMultiplier { get; }
        public float EnemyExperienceMultiplier { get; }
        public float BossHealthMultiplier { get; }
        public float BossMoveMultiplier { get; }
        public float TelegraphDurationMultiplier { get; }
        public float RewardMultiplier { get; }
        public float TreasureChanceBonus { get; }
    }

    /// <summary>
    /// Pure bounded Infinite Spire scaling. It never mutates catalog definitions.
    /// </summary>
    public static class InfiniteSpireScaling
    {
        public const int SoftTierFloorCap = 120;
        public const int ExperienceTierFloorCap = 80;
        public const int RewardPrimaryFloorCap = 50;
        public const int RewardSecondaryFloorCap = 100;

        public const float EnemyHealthMultiplierCap = 8f;
        public const float EnemyDamageMultiplierCap = 5f;
        public const float EnemyExperienceMultiplierCap = 3.4f;
        public const float BossHealthMultiplierCap = 7.5f;
        public const float BossMoveMultiplierCap = 1.35f;
        public const float MinimumTelegraphDurationMultiplier = 0.85f;
        public const float MaximumTreasureChanceBonus = 0.08f;

        public const float MinimumConcordDamp = 0.92f;
        public const float MaximumConcordDamp = 1f;

        public const int MaximumSafeBaseInteger = 1000000;
        public const float MaximumSafeBaseMoveSpeed = 100f;
        public const float MinimumSafeTelegraphSeconds = 0.05f;
        public const float MaximumSafeTelegraphSeconds = 60f;

        public static InfiniteSpireScaleSnapshot Evaluate(
            int floorIndex,
            float concordMultiplier = 1f)
        {
            floorIndex = NormalizeFloor(floorIndex);
            return new InfiniteSpireScaleSnapshot(
                floorIndex,
                ConcordDamp(concordMultiplier),
                EnemyHealthMultiplier(floorIndex, concordMultiplier),
                EnemyDamageMultiplier(floorIndex),
                EnemyExperienceMultiplier(floorIndex),
                BossHealthMultiplier(floorIndex),
                BossMoveMultiplier(floorIndex),
                TelegraphDurationMultiplier(floorIndex),
                RewardMultiplier(floorIndex),
                TreasureChanceBonus(floorIndex));
        }

        public static float EnemyHealthMultiplier(
            int floorIndex,
            float concordMultiplier = 1f)
        {
            GetTierAndDepth(floorIndex, out var tier, out var depth);
            var raw = (1d + 0.065d * tier + 0.02d * depth) *
                      ConcordDamp(concordMultiplier);
            return BoundedMultiplier(raw, 0f, EnemyHealthMultiplierCap);
        }

        public static float EnemyDamageMultiplier(int floorIndex)
        {
            GetTierAndDepth(floorIndex, out var tier, out var depth);
            var raw = 1d + 0.045d * tier + 0.015d * depth;
            return BoundedMultiplier(raw, 1f, EnemyDamageMultiplierCap);
        }

        public static float EnemyExperienceMultiplier(int floorIndex)
        {
            floorIndex = NormalizeFloor(floorIndex);
            var tier = Math.Min(floorIndex, ExperienceTierFloorCap);
            var raw = 1d + 0.03d * tier;
            return BoundedMultiplier(raw, 1f, EnemyExperienceMultiplierCap);
        }

        public static float BossHealthMultiplier(int floorIndex)
        {
            GetTierAndDepth(floorIndex, out var tier, out var depth);
            var raw = 1d + 0.055d * tier + 0.018d * depth;
            return BoundedMultiplier(raw, 1f, BossHealthMultiplierCap);
        }

        public static float BossMoveMultiplier(int floorIndex)
        {
            floorIndex = NormalizeFloor(floorIndex);
            var tier = Math.Min(floorIndex, SoftTierFloorCap);
            var raw = 1d + 0.004d * tier;
            return BoundedMultiplier(raw, 1f, BossMoveMultiplierCap);
        }

        public static float TelegraphDurationMultiplier(int floorIndex)
        {
            floorIndex = NormalizeFloor(floorIndex);
            var tier = Math.Min(floorIndex, SoftTierFloorCap);
            var raw = 1d - 0.00125d * tier;
            return BoundedMultiplier(
                raw,
                MinimumTelegraphDurationMultiplier,
                1f);
        }

        public static float ConcordDamp(float concordMultiplier)
        {
            if (!IsFinite(concordMultiplier))
            {
                concordMultiplier = 1f;
            }

            var normalized = (concordMultiplier - 1f) / 0.16f;
            normalized = Clamp(normalized, 0f, 1f);
            return Clamp(
                1f + (MinimumConcordDamp - 1f) * normalized,
                MinimumConcordDamp,
                MaximumConcordDamp);
        }

        public static float RewardMultiplier(int floorIndex)
        {
            floorIndex = NormalizeFloor(floorIndex);
            var primary = Math.Min(floorIndex, RewardPrimaryFloorCap);
            var secondary = Math.Max(
                0,
                Math.Min(floorIndex, RewardSecondaryFloorCap) -
                RewardPrimaryFloorCap);
            var raw = 1d + 0.04d * primary + 0.015d * secondary;
            return BoundedMultiplier(raw, 1f, 3.75f);
        }

        public static float TreasureChanceBonus(int floorIndex)
        {
            floorIndex = NormalizeFloor(floorIndex);
            var steps = floorIndex / 10;
            return Clamp(steps * 0.01f, 0f, MaximumTreasureChanceBonus);
        }

        public static int SwarmWeightMultiplier(int floorIndex)
        {
            return NormalizeFloor(floorIndex) >= 25 ? 2 : 1;
        }

        public static int ScaleEnemyHealth(
            int baseHealth,
            int floorIndex,
            float concordMultiplier = 1f)
        {
            return ScalePositiveInteger(
                baseHealth,
                EnemyHealthMultiplier(floorIndex, concordMultiplier));
        }

        public static int ScaleEnemyDamage(int baseDamage, int floorIndex)
        {
            return ScalePositiveInteger(
                baseDamage,
                EnemyDamageMultiplier(floorIndex));
        }

        public static int ScaleEnemyExperience(int baseExperience, int floorIndex)
        {
            return ScaleNonNegativeInteger(
                baseExperience,
                EnemyExperienceMultiplier(floorIndex));
        }

        public static int ScaleBossHealth(int baseHealth, int floorIndex)
        {
            return ScalePositiveInteger(
                baseHealth,
                BossHealthMultiplier(floorIndex));
        }

        public static float ScaleBossMoveSpeed(float baseMoveSpeed, int floorIndex)
        {
            var safeBase = SanitizeFinite(
                baseMoveSpeed,
                0f,
                MaximumSafeBaseMoveSpeed,
                0f);
            return SafeProduct(
                safeBase,
                BossMoveMultiplier(floorIndex),
                MaximumSafeBaseMoveSpeed * BossMoveMultiplierCap);
        }

        public static float ScaleTelegraphSeconds(
            float baselineSeconds,
            int floorIndex)
        {
            var safeBaseline = SanitizeFinite(
                baselineSeconds,
                MinimumSafeTelegraphSeconds,
                MaximumSafeTelegraphSeconds,
                MinimumSafeTelegraphSeconds);
            var result = SafeProduct(
                safeBaseline,
                TelegraphDurationMultiplier(floorIndex),
                MaximumSafeTelegraphSeconds);
            var minimumForBaseline =
                safeBaseline * MinimumTelegraphDurationMultiplier;
            return Clamp(
                result,
                minimumForBaseline,
                MaximumSafeTelegraphSeconds);
        }

        public static float ScaleReward(float baseReward, int floorIndex)
        {
            var safeBase = SanitizeFinite(
                baseReward,
                0f,
                MaximumSafeBaseInteger,
                0f);
            return SafeProduct(
                safeBase,
                RewardMultiplier(floorIndex),
                MaximumSafeBaseInteger * 3.75f);
        }

        private static void GetTierAndDepth(
            int floorIndex,
            out int tier,
            out int depth)
        {
            floorIndex = NormalizeFloor(floorIndex);
            tier = Math.Min(floorIndex, SoftTierFloorCap);
            depth = Math.Max(0, floorIndex - SoftTierFloorCap);
        }

        private static int NormalizeFloor(int floorIndex)
        {
            return floorIndex < 1 ? 1 : floorIndex;
        }

        private static int ScalePositiveInteger(int baseValue, float multiplier)
        {
            if (baseValue <= 0)
            {
                return 1;
            }

            baseValue = Math.Min(baseValue, MaximumSafeBaseInteger);
            var value = Math.Ceiling((double)baseValue * multiplier);
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return MaximumSafeBaseInteger;
            }

            return (int)Math.Min(
                MaximumSafeBaseInteger,
                Math.Max(1d, value));
        }

        private static int ScaleNonNegativeInteger(int baseValue, float multiplier)
        {
            if (baseValue <= 0)
            {
                return 0;
            }

            baseValue = Math.Min(baseValue, MaximumSafeBaseInteger);
            var value = Math.Ceiling((double)baseValue * multiplier);
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return MaximumSafeBaseInteger;
            }

            return (int)Math.Min(
                MaximumSafeBaseInteger,
                Math.Max(0d, value));
        }

        private static float SafeProduct(float first, float second, float maximum)
        {
            var value = (double)first * second;
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return maximum;
            }

            return (float)Math.Min(
                maximum,
                Math.Max(0d, value));
        }

        private static float BoundedMultiplier(
            double value,
            float minimum,
            float maximum)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return maximum;
            }

            return (float)Math.Min(
                maximum,
                Math.Max(minimum, value));
        }

        private static float SanitizeFinite(
            float value,
            float minimum,
            float maximum,
            float fallback)
        {
            if (!IsFinite(value))
            {
                return fallback;
            }

            return Clamp(value, minimum, maximum);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (!IsFinite(value))
            {
                return minimum;
            }

            return Math.Min(maximum, Math.Max(minimum, value));
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
