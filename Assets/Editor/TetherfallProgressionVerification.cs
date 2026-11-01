#if UNITY_EDITOR
using System;
using System.IO;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallProgressionVerification
    {
        private const string MenuPath = "Tetherfall/Verify Wayfinder Progression";
        private static int assertions;

        [MenuItem(MenuPath, false, 2206)]
        public static void Verify()
        {
            VerifyInternal(true);
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        public static void VerifyForFullGame()
        {
            VerifyInternal(false);
        }

        private static void VerifyInternal(bool logSuccess)
        {
            assertions = 0;
            VerifyExperienceCurveCarryover();
            VerifyPointBudgetBreakdown();
            VerifyRankMilestones();
            VerifyHudAndFieldLogSourceHooks();

            if (logSuccess)
            {
                Debug.Log(
                    $"Tetherfall Wayfinder progression verification passed: {assertions} assertions cover " +
                    "XP curve carryover, discipline-point sources, rank milestones, and HUD/Field Log hooks.");
            }
        }

        private static void VerifyExperienceCurveCarryover()
        {
            Assert(
                TetherfallWayfinderProgression.MaxVitalityPerRank == 12,
                "Every Wayfinder rank must add 12 max vitality.");
            Assert(
                TetherfallWayfinderProgression.DisciplinePointsPerRank == 1,
                "Every Wayfinder rank must add one discipline point.");
            Assert(
                TetherfallWayfinderProgression.GetNextExperienceRequirement(100) == 128,
                "The first XP requirement must retain the 1.28 curve.");
            Assert(
                TetherfallWayfinderProgression.GetNextExperienceRequirement(128) == 164,
                "XP requirements must retain rounded 1.28 growth.");

            var level = 1;
            var experience = 250;
            var experienceToNext = 100;
            var safety = 0;
            while (TetherfallWayfinderProgression.TryCarryExperienceToNextRank(
                       ref experience,
                       ref experienceToNext))
            {
                level++;
                safety++;
                Assert(safety < 20, "XP carryover did not terminate.");
            }

            Assert(level == 3, $"250 XP from rank 1 reached rank {level}; expected rank 3.");
            Assert(experience == 22, $"XP carryover retained {experience}; expected 22.");
            Assert(
                experienceToNext == 164,
                $"The post-carryover requirement was {experienceToNext}; expected 164.");
        }

        private static void VerifyPointBudgetBreakdown()
        {
            var baseline = TetherfallSkillRules.GetPointBudgetBreakdown(1, 0, 0, false);
            Assert(baseline.Total == 0, "A new rank-1 expedition must start with a zero-point budget.");

            var complete = TetherfallSkillRules.GetPointBudgetBreakdown(6, 2, 3, true);
            Assert(complete.LevelPoints == 5, "Rank 6 must contribute 5 discipline points.");
            Assert(complete.PortableCampPoints == 2, "Portable camp progression must contribute 2 points.");
            Assert(
                complete.AlderwakeRestorationPoints == 3,
                "Alderwake restoration must contribute 3 points.");
            Assert(complete.SkyvaultPoints == 2, "Skyvault restoration must contribute 2 points.");
            Assert(complete.Total == 12, $"The complete test budget was {complete.Total}; expected 12.");
            Assert(
                TetherfallSkillRules.GetPointBudget(6, 2, 3, true) == complete.Total,
                "The total budget and displayed breakdown must use the same source calculation.");

            Assert(
                TetherfallSkillRules.GetPointBudgetBreakdown(2, 0, 0, false).Total == 1,
                "One Wayfinder rank must add exactly one discipline point.");
            Assert(
                TetherfallSkillRules.GetPointBudgetBreakdown(1, 1, 0, false).Total == 1,
                "One portable camp upgrade must add exactly one discipline point.");
            Assert(
                TetherfallSkillRules.GetPointBudgetBreakdown(1, 0, 1, false).Total == 1,
                "One Alderwake restoration tier must add exactly one discipline point.");
            Assert(
                TetherfallSkillRules.GetPointBudgetBreakdown(1, 0, 0, true).Total == 2,
                "Skyvault restoration must add exactly two discipline points.");

            var clamped = TetherfallSkillRules.GetPointBudgetBreakdown(-4, 99, 99, false);
            Assert(clamped.LevelPoints == 0, "Invalid low ranks must not create negative points.");
            Assert(clamped.PortableCampPoints == 2, "Portable camp points must remain capped at 2.");
            Assert(
                clamped.AlderwakeRestorationPoints == 3,
                "Alderwake restoration points must remain capped at 3.");

            var description = TetherfallSkillRules.DescribePointBudget(complete);
            Assert(
                description ==
                "Budget: level 5 + portable camp 2 + Alderwake restoration 3 + Skyvault 2 = 12",
                "The skill header must name every point source and its exact contribution.");
        }

        private static void VerifyRankMilestones()
        {
            Assert(
                !TetherfallWayfinderProgression.IsExpeditionCapacityMilestone(1),
                "Rank 1 must not report an expedition-capacity milestone.");
            Assert(
                TetherfallWayfinderProgression.IsExpeditionCapacityMilestone(2) &&
                TetherfallWayfinderProgression.IsExpeditionCapacityMilestone(4) &&
                TetherfallWayfinderProgression.IsExpeditionCapacityMilestone(6),
                "Ranks 2, 4, and 6 must report expedition-capacity milestones.");
            Assert(
                TetherfallWayfinderProgression.GetExpeditionCapacityMilestonesEarned(6) == 3,
                "Rank 6 must expose three roster-consumable expedition-capacity milestones.");
            Assert(
                TetherfallWayfinderProgression.DescribeRankMilestone(2) ==
                "Expedition capacity milestone: +1 field slot.",
                "The rank-up milestone description must state the field-slot benefit.");
            Assert(
                string.IsNullOrEmpty(TetherfallWayfinderProgression.DescribeRankMilestone(3)),
                "Non-milestone ranks must not report a field-slot benefit.");
        }

        private static void VerifyHudAndFieldLogSourceHooks()
        {
            var scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            var gameSource = ReadNormalizedSource(Path.Combine(scriptsRoot, "TetherfallGame.cs"));
            var skillSource = ReadNormalizedSource(Path.Combine(scriptsRoot, "TetherfallSkillTrees.cs"));

            Assert(
                gameSource.Contains("QueueExperienceFeedback(amount);"),
                "GainExperience must feed the aggregated HUD XP readout.");
            Assert(
                gameSource.Contains("pendingExperienceFeedback") &&
                gameSource.Contains("$\"+{pendingExperienceFeedback} XP\""),
                "The HUD must display aggregated +XP feedback.");
            Assert(
                !gameSource.Contains("Notify($\"+{amount} XP"),
                "Routine XP gains must not create notification spam.");
            Assert(
                gameSource.Contains("ExperienceToNext,\n                tealTexture"),
                "The HUD must draw XP progress with the Threadglass bar treatment.");
            Assert(
                gameSource.Contains("TetherfallInputAction.OpenSkillTree") &&
                gameSource.Contains("Discipline points {AvailableSkillPoints} unspent"),
                "The HUD must show the discipline-point count and current skill-panel binding.");
            Assert(
                gameSource.Contains("TetherfallWayfinderProgression.DescribeRankMilestone(Level)"),
                "Rank-up feedback must call the roster-safe milestone helper.");
            Assert(
                gameSource.Contains("TetherfallWayfinderProgression.MaxVitalityPerRank") &&
                gameSource.Contains("\"Vitality fully restored. \"") &&
                gameSource.Contains("TetherfallWayfinderProgression.DisciplinePointsPerRank"),
                "Rank-up feedback must state max vitality, full restoration, and the discipline point.");
            Assert(
                gameSource.Contains("WAYFINDER FIELD LOG") &&
                gameSource.Contains("Every rank grants +12 max vitality") &&
                gameSource.Contains("nearby gains combine into one +XP readout"),
                "The Field Log must teach XP feedback and rank benefits.");
            Assert(
                skillSource.Contains("DescribePointBudget(CurrentSkillPointBudget)") &&
                skillSource.Contains("GUI.BeginScrollView("),
                "The skill panel must render the shared budget breakdown in a clipped scrollable layout.");
        }

        private static string ReadNormalizedSource(string path)
        {
            Assert(File.Exists(path), $"Required source file was not found: {path}");
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }

        private static void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
#endif
