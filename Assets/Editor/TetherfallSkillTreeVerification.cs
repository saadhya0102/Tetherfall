using System;
using System.Collections.Generic;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallSkillTreeVerification
    {
        private const string MenuPath = "Tetherfall/Verify Skill Trees";

        [MenuItem(MenuPath, false, 2205)]
        public static void Verify()
        {
            var failures = new List<string>();
            VerifyCatalog(failures);
            VerifyCycles(failures);
            VerifyNormalization(failures);
            VerifyPurchases(failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Skill-tree verification failed:\n- " + string.Join("\n- ", failures));
            }

            Debug.Log(
                $"Tetherfall skill-tree verification passed: {TetherfallSkillCatalog.All.Count} nodes, " +
                "catalog topology, persistence normalization, budgets, and purchase rules verified.");
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        private static void VerifyCatalog(List<string> failures)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var branchCounts = new Dictionary<SkillBranch, int>();
            foreach (SkillBranch branch in Enum.GetValues(typeof(SkillBranch)))
            {
                branchCounts[branch] = 0;
            }

            foreach (var definition in TetherfallSkillCatalog.All)
            {
                Check(definition != null, "Catalog contains a null node.", failures);
                if (definition == null)
                {
                    continue;
                }

                Check(!string.IsNullOrWhiteSpace(definition.id), "A node has no stable ID.", failures);
                Check(ids.Add(definition.id), $"Duplicate node ID '{definition.id}'.", failures);
                Check(!string.IsNullOrWhiteSpace(definition.displayName),
                    $"Node '{definition.id}' has no display name.", failures);
                Check(!string.IsNullOrWhiteSpace(definition.description),
                    $"Node '{definition.id}' has no description.", failures);
                Check(definition.rankCap > 0,
                    $"Node '{definition.id}' rank cap must be positive.", failures);
                Check(definition.pointCost > 0,
                    $"Node '{definition.id}' point cost must be positive.", failures);
                Check(IsFinite(definition.effectPerRank) && definition.effectPerRank > 0f,
                    $"Node '{definition.id}' effect value must be finite and positive.", failures);
                Check(Enum.IsDefined(typeof(SkillBranch), definition.branch),
                    $"Node '{definition.id}' has an invalid branch.", failures);
                Check(Enum.IsDefined(typeof(SkillEffectKind), definition.effect),
                    $"Node '{definition.id}' has an invalid effect.", failures);
                branchCounts[definition.branch]++;

                var prerequisiteIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var prerequisite in definition.prerequisites)
                {
                    Check(prerequisite != null,
                        $"Node '{definition.id}' has a null prerequisite.", failures);
                    if (prerequisite == null)
                    {
                        continue;
                    }

                    var required = TetherfallSkillCatalog.Find(prerequisite.id);
                    Check(required != null,
                        $"Node '{definition.id}' references unknown prerequisite '{prerequisite.id}'.", failures);
                    Check(prerequisiteIds.Add(prerequisite.id),
                        $"Node '{definition.id}' repeats prerequisite '{prerequisite.id}'.", failures);
                    Check(required == null || prerequisite.rank > 0 && prerequisite.rank <= required.rankCap,
                        $"Node '{definition.id}' has invalid required rank {prerequisite.rank} for " +
                        $"'{prerequisite.id}'.", failures);
                    Check(!string.Equals(definition.id, prerequisite.id, StringComparison.Ordinal),
                        $"Node '{definition.id}' requires itself.", failures);
                }
            }

            foreach (var pair in branchCounts)
            {
                Check(pair.Value >= 4 && pair.Value <= 6,
                    $"Branch '{pair.Key}' has {pair.Value} nodes; expected 4-6.", failures);
            }
        }

        private static void VerifyCycles(List<string> failures)
        {
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in TetherfallSkillCatalog.All)
            {
                Visit(definition, visiting, visited, failures);
            }
        }

        private static void Visit(
            SkillNodeDefinition definition,
            HashSet<string> visiting,
            HashSet<string> visited,
            List<string> failures)
        {
            if (definition == null || visited.Contains(definition.id))
            {
                return;
            }

            if (!visiting.Add(definition.id))
            {
                failures.Add($"Prerequisite cycle reaches '{definition.id}'.");
                return;
            }

            foreach (var prerequisite in definition.prerequisites)
            {
                Visit(TetherfallSkillCatalog.Find(prerequisite.id), visiting, visited, failures);
            }

            visiting.Remove(definition.id);
            visited.Add(definition.id);
        }

        private static void VerifyNormalization(List<string> failures)
        {
            var ranks = new List<SkillRankSaveData>
            {
                new SkillRankSaveData { id = "skill.combat.measured_strikes", rank = 2 },
                new SkillRankSaveData { id = "skill.combat.measured_strikes", rank = 99 },
                new SkillRankSaveData { id = "skill.combat.guard_weave", rank = 99 },
                new SkillRankSaveData { id = "skill.expedition.linked_intent", rank = 2 },
                new SkillRankSaveData { id = "skill.unknown", rank = 50 },
                new SkillRankSaveData { id = "skill.traversal.trail_lungs", rank = -2 },
                null
            };

            TetherfallSkillRules.NormalizeInPlace(ranks, 4);
            Check(ranks.Count == 2, "Normalization did not remove invalid or unaffordable records.", failures);
            Check(TetherfallSkillRules.GetRank(ranks, "skill.combat.measured_strikes") == 3,
                "Normalization did not clamp/merge duplicate rank records safely.", failures);
            Check(TetherfallSkillRules.GetRank(ranks, "skill.combat.guard_weave") == 1,
                "Normalization did not cap ranks to the remaining deterministic budget.", failures);
            Check(TetherfallSkillRules.GetSpentPoints(ranks) == 4,
                "Normalized records exceed or fail to use the expected test budget.", failures);
            Check(TetherfallSkillRules.GetRank(ranks, "skill.expedition.linked_intent") == 0,
                "Normalization retained a prerequisite-bypassing record.", failures);

            TetherfallSkillRules.NormalizeInPlace(ranks, 4);
            Check(ranks.Count == 2 &&
                  TetherfallSkillRules.GetSpentPoints(ranks) == 4,
                "Normalization is not idempotent.", failures);

            var expectedBudget = TetherfallSkillRules.GetPointBudget(6, 2, 3, true);
            Check(expectedBudget == 12,
                $"Deterministic progression budget returned {expectedBudget}; expected 12.", failures);
        }

        private static void VerifyPurchases(List<string> failures)
        {
            var ranks = new List<SkillRankSaveData>();
            Check(!TetherfallSkillRules.TryPurchase(
                    ranks,
                    0,
                    "skill.combat.measured_strikes",
                    out _),
                "A purchase succeeded without points.", failures);
            Check(!TetherfallSkillRules.TryPurchase(
                    ranks,
                    10,
                    "skill.expedition.linked_intent",
                    out _),
                "A purchase bypassed its prerequisite.", failures);
            Check(!TetherfallSkillRules.TryPurchase(ranks, 3, "skill.combat.guard_weave", out _),
                "Guard Weave purchased before Measured Strikes.", failures);
            Check(TetherfallSkillRules.TryPurchase(ranks, 3, "skill.combat.measured_strikes", out _),
                "A valid root-node purchase failed.", failures);
            Check(TetherfallSkillRules.TryPurchase(ranks, 3, "skill.combat.guard_weave", out _),
                "A valid prerequisite-gated purchase failed.", failures);
            Check(TetherfallSkillRules.GetSpentPoints(ranks) == 2,
                "Valid purchases spent an unexpected number of points.", failures);

            Check(!TetherfallSkillRules.TryPurchase(ranks, 2, "skill.combat.measured_strikes", out _),
                "A purchase overspent its budget.", failures);
            Check(!TetherfallSkillRules.TryPurchase(ranks, 20, "skill.missing", out _),
                "An unknown skill ID was purchasable.", failures);

            var capped = new List<SkillRankSaveData>
            {
                new SkillRankSaveData { id = "skill.combat.measured_strikes", rank = 3 }
            };
            Check(!TetherfallSkillRules.TryPurchase(
                    capped,
                    20,
                    "skill.combat.measured_strikes",
                    out _),
                "A rank-cap purchase succeeded.", failures);
        }

        private static void Check(bool condition, string message, List<string> failures)
        {
            if (!condition)
            {
                failures.Add(message);
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
