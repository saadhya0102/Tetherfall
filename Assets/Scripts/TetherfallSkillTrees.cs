using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public enum SkillBranch
    {
        WayfinderCombat,
        ExpeditionPact,
        TraversalSurvival
    }

    public enum SkillEffectKind
    {
        FlatDamage,
        Guard,
        CombatStaminaEfficiency,
        OvertonePotency,
        PactAssistPotency,
        ThreadlinkPotency,
        FavorReward,
        MaximumStamina,
        StaminaRegeneration,
        MoveSpeed,
        WindLift,
        ResourceYield
    }

    [Serializable]
    public sealed class SkillRankSaveData
    {
        public string id;
        public int rank;
    }

    [Serializable]
    public sealed class SkillPrerequisite
    {
        public string id;
        public int rank;

        public SkillPrerequisite(string prerequisiteId, int prerequisiteRank)
        {
            id = prerequisiteId;
            rank = prerequisiteRank;
        }
    }

    public sealed class SkillNodeDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public SkillBranch branch;
        public int rankCap;
        public int pointCost;
        public SkillEffectKind effect;
        public float effectPerRank;
        public string effectUnit;
        public List<SkillPrerequisite> prerequisites = new List<SkillPrerequisite>();

        public string DescribeEffect(int rank)
        {
            var total = effectPerRank * Mathf.Clamp(rank, 0, rankCap);
            var value = Mathf.Approximately(total, Mathf.Round(total))
                ? Mathf.RoundToInt(total).ToString()
                : total.ToString("0.#");
            return $"{EffectLabel(effect)} +{value}{effectUnit}";
        }

        private static string EffectLabel(SkillEffectKind kind)
        {
            return kind switch
            {
                SkillEffectKind.FlatDamage => "Weapon damage",
                SkillEffectKind.Guard => "Guard reduction",
                SkillEffectKind.CombatStaminaEfficiency => "Combat stamina efficiency",
                SkillEffectKind.OvertonePotency => "Overtone potency",
                SkillEffectKind.PactAssistPotency => "Pact assist potency",
                SkillEffectKind.ThreadlinkPotency => "Threadlink potency",
                SkillEffectKind.FavorReward => "Favor rewards",
                SkillEffectKind.MaximumStamina => "Maximum stamina",
                SkillEffectKind.StaminaRegeneration => "Stamina regeneration",
                SkillEffectKind.MoveSpeed => "Move speed",
                SkillEffectKind.WindLift => "Glide wind lift",
                SkillEffectKind.ResourceYield => "Resource yield",
                _ => kind.ToString()
            };
        }
    }

    public static class TetherfallSkillCatalog
    {
        private static readonly List<SkillNodeDefinition> Definitions = new List<SkillNodeDefinition>
        {
            Node("skill.combat.measured_strikes", "Measured Strikes", "Turn disciplined openings into reliable force.",
                SkillBranch.WayfinderCombat, 3, 1, SkillEffectKind.FlatDamage, 2f, ""),
            Node("skill.combat.guard_weave", "Guard Weave", "Set the Threadglass against incoming weight.",
                SkillBranch.WayfinderCombat, 3, 1, SkillEffectKind.Guard, 1f, "",
                Requirement("skill.combat.measured_strikes", 1)),
            Node("skill.combat.economy_motion", "Economy of Motion", "Spend less stamina on attacks, guards, and evasions.",
                SkillBranch.WayfinderCombat, 2, 1, SkillEffectKind.CombatStaminaEfficiency, 6f, "%",
                Requirement("skill.combat.measured_strikes", 2)),
            Node("skill.combat.resonant_opening", "Resonant Opening", "Deepen damage delivered through Overtone windows.",
                SkillBranch.WayfinderCombat, 2, 2, SkillEffectKind.OvertonePotency, 10f, "%",
                Requirement("skill.combat.guard_weave", 2)),
            Node("skill.combat.wayfinder_form", "Wayfinder Form", "Unify edge, stance, and route sense.",
                SkillBranch.WayfinderCombat, 1, 3, SkillEffectKind.FlatDamage, 5f, "",
                Requirement("skill.combat.economy_motion", 2),
                Requirement("skill.combat.resonant_opening", 1)),

            Node("skill.expedition.pact_cadence", "Pact Cadence", "Give an active Echosworn a clearer opening to assist.",
                SkillBranch.ExpeditionPact, 3, 1, SkillEffectKind.PactAssistPotency, 8f, "%"),
            Node("skill.expedition.linked_intent", "Linked Intent", "Carry more force through coordinated Threadlink beats.",
                SkillBranch.ExpeditionPact, 2, 1, SkillEffectKind.ThreadlinkPotency, 10f, "%",
                Requirement("skill.expedition.pact_cadence", 2)),
            Node("skill.expedition.route_dividends", "Route Dividends", "Earn greater Favor from completed field work.",
                SkillBranch.ExpeditionPact, 3, 1, SkillEffectKind.FavorReward, 10f, "%"),
            Node("skill.expedition.shared_reserves", "Shared Reserves", "Recover useful material through expedition teamwork.",
                SkillBranch.ExpeditionPact, 2, 2, SkillEffectKind.ResourceYield, 1f, "",
                Requirement("skill.expedition.route_dividends", 2)),
            Node("skill.expedition.concord", "Field Concord", "Perfect the cadence between Pact and Threadlink.",
                SkillBranch.ExpeditionPact, 1, 3, SkillEffectKind.ThreadlinkPotency, 20f, "%",
                Requirement("skill.expedition.linked_intent", 2),
                Requirement("skill.expedition.shared_reserves", 1)),

            Node("skill.traversal.trail_lungs", "Trail Lungs", "Build a deeper reserve for long crossings.",
                SkillBranch.TraversalSurvival, 3, 1, SkillEffectKind.MaximumStamina, 10f, ""),
            Node("skill.traversal.sure_footing", "Sure Footing", "Keep speed over broken and uncertain ground.",
                SkillBranch.TraversalSurvival, 2, 1, SkillEffectKind.MoveSpeed, 0.25f, "",
                Requirement("skill.traversal.trail_lungs", 1)),
            Node("skill.traversal.second_wind", "Second Wind", "Recover stamina faster after exertion.",
                SkillBranch.TraversalSurvival, 3, 1, SkillEffectKind.StaminaRegeneration, 2f, "/s",
                Requirement("skill.traversal.trail_lungs", 2)),
            Node("skill.traversal.updraft_reading", "Updraft Reading", "Find more lift while gliding through field winds.",
                SkillBranch.TraversalSurvival, 2, 2, SkillEffectKind.WindLift, 1.5f, "",
                Requirement("skill.traversal.sure_footing", 2)),
            Node("skill.traversal.wild_harvest", "Wild Harvest", "Bring back more from every resource recovery.",
                SkillBranch.TraversalSurvival, 2, 2, SkillEffectKind.ResourceYield, 1f, "",
                Requirement("skill.traversal.second_wind", 2))
        };

        private static readonly Dictionary<string, SkillNodeDefinition> ById = BuildLookup();

        public static IReadOnlyList<SkillNodeDefinition> All => Definitions;

        public static SkillNodeDefinition Find(string id)
        {
            return !string.IsNullOrEmpty(id) && ById.TryGetValue(id, out var definition)
                ? definition
                : null;
        }

        private static SkillNodeDefinition Node(
            string id,
            string name,
            string description,
            SkillBranch branch,
            int rankCap,
            int pointCost,
            SkillEffectKind effect,
            float effectPerRank,
            string effectUnit,
            params SkillPrerequisite[] prerequisites)
        {
            return new SkillNodeDefinition
            {
                id = id,
                displayName = name,
                description = description,
                branch = branch,
                rankCap = rankCap,
                pointCost = pointCost,
                effect = effect,
                effectPerRank = effectPerRank,
                effectUnit = effectUnit,
                prerequisites = new List<SkillPrerequisite>(prerequisites ?? Array.Empty<SkillPrerequisite>())
            };
        }

        private static SkillPrerequisite Requirement(string id, int rank)
        {
            return new SkillPrerequisite(id, rank);
        }

        private static Dictionary<string, SkillNodeDefinition> BuildLookup()
        {
            var result = new Dictionary<string, SkillNodeDefinition>(StringComparer.Ordinal);
            foreach (var definition in Definitions)
            {
                result.Add(definition.id, definition);
            }

            return result;
        }
    }

    public readonly struct SkillPointBudgetBreakdown
    {
        public SkillPointBudgetBreakdown(
            int levelPoints,
            int portableCampPoints,
            int alderwakeRestorationPoints,
            int skyvaultPoints)
        {
            LevelPoints = levelPoints;
            PortableCampPoints = portableCampPoints;
            AlderwakeRestorationPoints = alderwakeRestorationPoints;
            SkyvaultPoints = skyvaultPoints;
        }

        public int LevelPoints { get; }
        public int PortableCampPoints { get; }
        public int AlderwakeRestorationPoints { get; }
        public int SkyvaultPoints { get; }
        public int Total =>
            LevelPoints +
            PortableCampPoints +
            AlderwakeRestorationPoints +
            SkyvaultPoints;
    }

    public static class TetherfallWayfinderProgression
    {
        public const float ExperienceRequirementMultiplier = 1.28f;
        public const int MaxVitalityPerRank = 12;
        public const int DisciplinePointsPerRank = 1;

        private static readonly int[] ExpeditionCapacityMilestoneRanks = { 2, 4, 6 };

        public static int GetNextExperienceRequirement(int currentRequirement)
        {
            return Mathf.RoundToInt(
                Mathf.Max(1, currentRequirement) * ExperienceRequirementMultiplier);
        }

        public static bool TryCarryExperienceToNextRank(
            ref int experience,
            ref int experienceToNext)
        {
            var requirement = Mathf.Max(1, experienceToNext);
            if (experience < requirement)
            {
                return false;
            }

            experience -= requirement;
            experienceToNext = GetNextExperienceRequirement(requirement);
            return true;
        }

        public static bool IsExpeditionCapacityMilestone(int rank)
        {
            for (var index = 0; index < ExpeditionCapacityMilestoneRanks.Length; index++)
            {
                if (rank == ExpeditionCapacityMilestoneRanks[index])
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetExpeditionCapacityMilestonesEarned(int rank)
        {
            var earned = 0;
            for (var index = 0; index < ExpeditionCapacityMilestoneRanks.Length; index++)
            {
                if (rank >= ExpeditionCapacityMilestoneRanks[index])
                {
                    earned++;
                }
            }

            return earned;
        }

        public static string DescribeRankMilestone(int rank)
        {
            return IsExpeditionCapacityMilestone(rank)
                ? "Expedition capacity milestone: +1 field slot."
                : string.Empty;
        }
    }

    public static class TetherfallSkillRules
    {
        public static int GetPointBudget(
            int level,
            int campTier,
            int settlementTier,
            bool dungeonBossDefeated)
        {
            return GetPointBudgetBreakdown(
                level,
                campTier,
                settlementTier,
                dungeonBossDefeated).Total;
        }

        public static SkillPointBudgetBreakdown GetPointBudgetBreakdown(
            int level,
            int campTier,
            int settlementTier,
            bool dungeonBossDefeated)
        {
            return new SkillPointBudgetBreakdown(
                Mathf.Max(0, level - 1),
                Mathf.Clamp(campTier, 0, 2),
                Mathf.Clamp(settlementTier, 0, 3),
                dungeonBossDefeated ? 2 : 0);
        }

        public static string DescribePointBudget(SkillPointBudgetBreakdown breakdown)
        {
            return
                $"Budget: level {breakdown.LevelPoints} + portable camp {breakdown.PortableCampPoints} + " +
                $"Alderwake restoration {breakdown.AlderwakeRestorationPoints} + " +
                $"Skyvault {breakdown.SkyvaultPoints} = {breakdown.Total}";
        }

        public static int GetRank(IReadOnlyList<SkillRankSaveData> ranks, string id)
        {
            if (ranks == null)
            {
                return 0;
            }

            for (var index = 0; index < ranks.Count; index++)
            {
                var record = ranks[index];
                if (record != null && string.Equals(record.id, id, StringComparison.Ordinal))
                {
                    return Mathf.Max(0, record.rank);
                }
            }

            return 0;
        }

        public static int GetSpentPoints(IReadOnlyList<SkillRankSaveData> ranks)
        {
            var spent = 0;
            foreach (var definition in TetherfallSkillCatalog.All)
            {
                spent += GetRank(ranks, definition.id) * definition.pointCost;
            }

            return spent;
        }

        public static float GetEffectTotal(
            IReadOnlyList<SkillRankSaveData> ranks,
            SkillEffectKind effect)
        {
            var total = 0f;
            foreach (var definition in TetherfallSkillCatalog.All)
            {
                if (definition.effect == effect)
                {
                    total += GetRank(ranks, definition.id) * definition.effectPerRank;
                }
            }

            return total;
        }

        public static void NormalizeInPlace(List<SkillRankSaveData> ranks, int pointBudget)
        {
            if (ranks == null)
            {
                return;
            }

            var requested = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var record in ranks)
            {
                var definition = record != null ? TetherfallSkillCatalog.Find(record.id) : null;
                if (definition == null || record.rank <= 0)
                {
                    continue;
                }

                var clamped = Mathf.Clamp(record.rank, 1, definition.rankCap);
                if (!requested.TryGetValue(definition.id, out var existing) || clamped > existing)
                {
                    requested[definition.id] = clamped;
                }
            }

            ranks.Clear();
            var remaining = Mathf.Max(0, pointBudget);
            foreach (var definition in TetherfallSkillCatalog.All)
            {
                if (!requested.TryGetValue(definition.id, out var desired) ||
                    !PrerequisitesMet(ranks, definition))
                {
                    continue;
                }

                var affordable = Mathf.Min(desired, remaining / definition.pointCost);
                if (affordable <= 0)
                {
                    continue;
                }

                ranks.Add(new SkillRankSaveData { id = definition.id, rank = affordable });
                remaining -= affordable * definition.pointCost;
            }
        }

        public static bool CanPurchase(
            IReadOnlyList<SkillRankSaveData> ranks,
            int pointBudget,
            string id,
            out string reason)
        {
            var definition = TetherfallSkillCatalog.Find(id);
            if (definition == null)
            {
                reason = "Unknown skill.";
                return false;
            }

            var rank = GetRank(ranks, id);
            if (rank >= definition.rankCap)
            {
                reason = "Maximum rank reached.";
                return false;
            }

            if (!PrerequisitesMet(ranks, definition))
            {
                reason = DescribeMissingPrerequisites(ranks, definition);
                return false;
            }

            var available = Mathf.Max(0, pointBudget - GetSpentPoints(ranks));
            if (available < definition.pointCost)
            {
                reason = $"Needs {definition.pointCost} point{(definition.pointCost == 1 ? "" : "s")}.";
                return false;
            }

            reason = "Ready to learn.";
            return true;
        }

        public static bool TryPurchase(
            List<SkillRankSaveData> ranks,
            int pointBudget,
            string id,
            out string reason)
        {
            if (ranks == null)
            {
                reason = "Skill record is unavailable.";
                return false;
            }

            NormalizeInPlace(ranks, pointBudget);
            if (!CanPurchase(ranks, pointBudget, id, out reason))
            {
                return false;
            }

            var record = ranks.Find(item => item.id == id);
            if (record == null)
            {
                ranks.Add(new SkillRankSaveData { id = id, rank = 1 });
            }
            else
            {
                record.rank++;
            }

            reason = "Rank learned.";
            return true;
        }

        public static bool PrerequisitesMet(
            IReadOnlyList<SkillRankSaveData> ranks,
            SkillNodeDefinition definition)
        {
            foreach (var prerequisite in definition.prerequisites)
            {
                if (GetRank(ranks, prerequisite.id) < prerequisite.rank)
                {
                    return false;
                }
            }

            return true;
        }

        public static string DescribePrerequisites(SkillNodeDefinition definition)
        {
            if (definition.prerequisites.Count == 0)
            {
                return "Prerequisite: none";
            }

            var parts = new List<string>();
            foreach (var prerequisite in definition.prerequisites)
            {
                var required = TetherfallSkillCatalog.Find(prerequisite.id);
                parts.Add($"{required?.displayName ?? prerequisite.id} {prerequisite.rank}");
            }

            return "Requires: " + string.Join(", ", parts);
        }

        private static string DescribeMissingPrerequisites(
            IReadOnlyList<SkillRankSaveData> ranks,
            SkillNodeDefinition definition)
        {
            var missing = new List<string>();
            foreach (var prerequisite in definition.prerequisites)
            {
                if (GetRank(ranks, prerequisite.id) >= prerequisite.rank)
                {
                    continue;
                }

                var required = TetherfallSkillCatalog.Find(prerequisite.id);
                missing.Add($"{required?.displayName ?? prerequisite.id} {prerequisite.rank}");
            }

            return missing.Count == 0 ? "Prerequisites met." : "Requires " + string.Join(", ", missing) + ".";
        }
    }

    public sealed partial class TetherfallGame
    {
        private readonly List<SkillRankSaveData> skillRanks = new List<SkillRankSaveData>();
        private SkillBranch selectedSkillBranch;
        private string skillTreeFeedback = "Choose a discipline to inspect its route.";
        private Vector2 skillTreeScrollPosition;

        public IReadOnlyList<SkillRankSaveData> SkillRanks => skillRanks;
        public SkillPointBudgetBreakdown CurrentSkillPointBudget =>
            TetherfallSkillRules.GetPointBudgetBreakdown(
                Level,
                campTier,
                settlementTier,
                dungeonBossDefeated);
        public int SkillPointBudget => CurrentSkillPointBudget.Total;
        public int SpentSkillPoints => TetherfallSkillRules.GetSpentPoints(skillRanks);
        public int AvailableSkillPoints => Mathf.Max(0, SkillPointBudget - SpentSkillPoints);
        public int SkillFlatDamageBonus => Mathf.RoundToInt(GetSkillEffect(SkillEffectKind.FlatDamage));
        public int SkillGuardBonus => Mathf.RoundToInt(GetSkillEffect(SkillEffectKind.Guard));
        public float CombatStaminaCostMultiplier =>
            Mathf.Clamp(1f - GetSkillEffect(SkillEffectKind.CombatStaminaEfficiency) * 0.01f, 0.5f, 1f);
        public float OvertonePotencyMultiplier =>
            1f + GetSkillEffect(SkillEffectKind.OvertonePotency) * 0.01f;
        public float PactAssistPotencyMultiplier =>
            1f + GetSkillEffect(SkillEffectKind.PactAssistPotency) * 0.01f;
        public float ThreadlinkPotencyMultiplier =>
            1f + GetSkillEffect(SkillEffectKind.ThreadlinkPotency) * 0.01f;
        public float SkillFavorRewardMultiplier =>
            1f + GetSkillEffect(SkillEffectKind.FavorReward) * 0.01f;
        public float SkillMaxStaminaBonus => GetSkillEffect(SkillEffectKind.MaximumStamina);
        public float SkillStaminaRegenerationBonus => GetSkillEffect(SkillEffectKind.StaminaRegeneration);
        public float SkillMoveSpeedBonus => GetSkillEffect(SkillEffectKind.MoveSpeed);
        public float SkillWindLiftVelocityBonus => GetSkillEffect(SkillEffectKind.WindLift);
        public int SkillResourceYieldBonus => Mathf.RoundToInt(GetSkillEffect(SkillEffectKind.ResourceYield));

        public int GetSkillRank(string id)
        {
            return TetherfallSkillRules.GetRank(skillRanks, id);
        }

        public bool TryPurchaseSkill(string id)
        {
            if (!TetherfallSkillRules.TryPurchase(skillRanks, SkillPointBudget, id, out var reason))
            {
                skillTreeFeedback = reason;
                Notify(reason);
                return false;
            }

            var definition = TetherfallSkillCatalog.Find(id);
            skillTreeFeedback = $"{definition.displayName} is now rank {GetSkillRank(id)}/{definition.rankCap}.";
            Notify(skillTreeFeedback);
            RequestSave();
            return true;
        }

        private float GetSkillEffect(SkillEffectKind effect)
        {
            return TetherfallSkillRules.GetEffectTotal(skillRanks, effect);
        }

        private int ApplySkillResourceYield(int amount)
        {
            return amount > 0 ? amount + SkillResourceYieldBonus : amount;
        }

        private int ApplySkillFavorReward(int amount)
        {
            return amount > 0
                ? Mathf.Max(amount, Mathf.RoundToInt(amount * SkillFavorRewardMultiplier))
                : amount;
        }

        private void WriteSkillTreeSave(TetherfallSaveData data)
        {
            data.skillRanks.Clear();
            foreach (var record in skillRanks)
            {
                data.skillRanks.Add(new SkillRankSaveData { id = record.id, rank = record.rank });
            }
        }

        private void RestoreSkillTreeState(TetherfallSaveData data)
        {
            skillRanks.Clear();
            foreach (var record in data.skillRanks)
            {
                if (record != null)
                {
                    skillRanks.Add(new SkillRankSaveData { id = record.id, rank = record.rank });
                }
            }

            TetherfallSkillRules.NormalizeInPlace(skillRanks, SkillPointBudget);
        }

        private void DrawSkillTree(Rect rect)
        {
            GUI.BeginGroup(rect);
            var localRect = new Rect(0f, 0f, rect.width, rect.height);
            const float tabsY = 66f;
            var tabWidth = (localRect.width - 68f) / 3f;
            var compactTabs = localRect.width < 700f;
            DrawSkillBranchTab(
                26f,
                tabsY,
                tabWidth,
                SkillBranch.WayfinderCombat,
                compactTabs ? "COMBAT" : "WAYFINDER COMBAT");
            DrawSkillBranchTab(
                34f + tabWidth,
                tabsY,
                tabWidth,
                SkillBranch.ExpeditionPact,
                compactTabs ? "PACT" : "EXPEDITION / PACT");
            DrawSkillBranchTab(
                42f + tabWidth * 2f,
                tabsY,
                tabWidth,
                SkillBranch.TraversalSurvival,
                compactTabs ? "TRAVERSAL" : "TRAVERSAL / SURVIVAL");

            GUI.Label(
                new Rect(26f, 108f, localRect.width - 52f, 24f),
                $"Discipline points  {AvailableSkillPoints} available  ·  " +
                $"{SpentSkillPoints} invested  ·  {SkillPointBudget} earned",
                headingStyle);
            GUI.Label(
                new Rect(26f, 134f, localRect.width - 52f, 34f),
                TetherfallSkillRules.DescribePointBudget(CurrentSkillPointBudget),
                mutedStyle);
            GUI.Label(
                new Rect(26f, 170f, localRect.width - 52f, 20f),
                skillTreeFeedback,
                mutedStyle);

            var visibleNodeCount = 0;
            foreach (var definition in TetherfallSkillCatalog.All)
            {
                if (definition.branch == selectedSkillBranch)
                {
                    visibleNodeCount++;
                }
            }

            const float listTop = 194f;
            const float rowStride = 78f;
            var listRect = new Rect(
                0f,
                listTop,
                localRect.width,
                Mathf.Max(72f, localRect.height - listTop - 36f));
            var requiredHeight = visibleNodeCount * rowStride;
            var needsScroll = requiredHeight > listRect.height;
            var contentRect = new Rect(
                0f,
                0f,
                Mathf.Max(1f, listRect.width - (needsScroll ? 18f : 0f)),
                Mathf.Max(listRect.height, requiredHeight));
            skillTreeScrollPosition = GUI.BeginScrollView(
                listRect,
                skillTreeScrollPosition,
                contentRect,
                false,
                needsScroll);

            var y = 0f;
            foreach (var definition in TetherfallSkillCatalog.All)
            {
                if (definition.branch == selectedSkillBranch)
                {
                    DrawSkillNode(contentRect, definition, y);
                    y += rowStride;
                }
            }

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawSkillBranchTab(
            float x,
            float y,
            float width,
            SkillBranch branch,
            string label)
        {
            var priorColor = GUI.color;
            GUI.color = selectedSkillBranch == branch ? new Color(0.72f, 0.94f, 0.9f) : Color.white;
            if (GUI.Button(new Rect(x, y, width, 34f), label))
            {
                selectedSkillBranch = branch;
                skillTreeScrollPosition = Vector2.zero;
                skillTreeFeedback = "Review exact effects and requirements before investing.";
            }

            GUI.color = priorColor;
        }

        private void DrawSkillNode(Rect rect, SkillNodeDefinition definition, float y)
        {
            var rank = GetSkillRank(definition.id);
            var canPurchase = TetherfallSkillRules.CanPurchase(
                skillRanks,
                SkillPointBudget,
                definition.id,
                out var state);
            var status = rank >= definition.rankCap
                ? "MASTERED"
                : canPurchase ? $"LEARN · {definition.pointCost} PT" : "LOCKED";
            var buttonRect = new Rect(rect.xMax - 166f, y + 8f, 136f, 34f);

            GUI.DrawTexture(new Rect(rect.x + 22f, y, rect.width - 44f, 72f), inkTexture);
            GUI.Label(
                new Rect(rect.x + 34f, y + 7f, rect.width - 220f, 22f),
                $"{definition.displayName}   {rank}/{definition.rankCap}",
                headingStyle);
            GUI.Label(
                new Rect(rect.x + 34f, y + 30f, rect.width - 220f, 20f),
                $"{definition.description}  ·  {definition.DescribeEffect(Mathf.Max(1, rank))}",
                bodyStyle);
            GUI.Label(
                new Rect(rect.x + 34f, y + 51f, rect.width - 220f, 17f),
                $"{TetherfallSkillRules.DescribePrerequisites(definition)}  ·  {state}",
                mutedStyle);

            var wasEnabled = GUI.enabled;
            GUI.enabled = canPurchase;
            if (GUI.Button(buttonRect, status))
            {
                TryPurchaseSkill(definition.id);
            }

            GUI.enabled = wasEnabled;
        }
    }
}
