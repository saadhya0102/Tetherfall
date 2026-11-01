using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallMainStoryVerification
    {
        private const string MenuPath = "Tetherfall/Verify Main Story";
        private static readonly List<string> Failures = new List<string>();
        private static int assertionCount;

        [MenuItem(MenuPath, false, 2190)]
        public static void VerifyMainStory()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Tetherfall main-story verification is edit-mode only. Stop Play Mode and run it again.");
            }

            Failures.Clear();
            assertionCount = 0;
            VerifyStageCatalog();
            VerifyQuestRecords();
            VerifyFightRosters();
            VerifyBossDefinition();
            VerifyPlacementAndReentry();
            VerifyRuntimeWiring();
            Complete();
        }

        [MenuItem(MenuPath, true, 2190)]
        private static bool CanVerifyMainStory()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void VerifyStageCatalog()
        {
            Check(
                MainStoryCatalog.Stages != null,
                "MainStoryCatalog.Stages is null.");
            if (MainStoryCatalog.Stages == null)
            {
                return;
            }

            Check(
                MainStoryCatalog.Stages.Count == 6,
                $"Main story has {MainStoryCatalog.Stages.Count} stages; expected exactly 6.");
            Check(
                MainStoryCatalog.CompletedProgress == MainStoryCatalog.Stages.Count,
                "CompletedProgress must equal the authored stage count.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < MainStoryCatalog.Stages.Count; index++)
            {
                var stage = MainStoryCatalog.Stages[index];
                Check(stage != null, $"Main story stage {index} is null.");
                if (stage == null)
                {
                    continue;
                }

                Check(
                    !string.IsNullOrWhiteSpace(stage.id) &&
                    stage.id.StartsWith("story.hollow_choir.stage.", StringComparison.Ordinal),
                    $"Main story stage {index} has an invalid stable ID '{stage.id}'.");
                Check(
                    ids.Add(stage.id),
                    $"Main story stage ID '{stage.id}' is duplicated.");
                Check(
                    stage.progress == index,
                    $"Main story stage '{stage.id}' has progress {stage.progress}; expected {index}.");
                Check(
                    !string.IsNullOrWhiteSpace(stage.title),
                    $"Main story stage '{stage.id}' has no title.");
                Check(
                    !string.IsNullOrWhiteSpace(stage.objective),
                    $"Main story stage '{stage.id}' has no objective guidance.");
            }
        }

        private static void VerifyQuestRecords()
        {
            var quest = MainStoryCatalog.CreateQuestRecord();
            Check(quest != null, "MainStoryCatalog.CreateQuestRecord returned null.");
            if (quest != null)
            {
                Check(
                    quest.id == MainStoryCatalog.QuestId,
                    "Main story quest ID does not match MainStoryCatalog.QuestId.");
                Check(
                    quest.required == MainStoryCatalog.CompletedProgress,
                    "Main story quest requirement does not match its stage count.");
                Check(
                    quest.current == 0 && !quest.completed,
                    "A fresh main story quest must begin incomplete at progress zero.");
                Check(
                    quest.favorReward > 0,
                    "The chapter completion quest needs a positive one-time Favor reward.");
            }

            var hearthline = MainStoryCatalog.CreateRouteMarker(MainStoryCatalog.HearthlineRouteId);
            var openCurrent = MainStoryCatalog.CreateRouteMarker(MainStoryCatalog.OpenCurrentRouteId);
            Check(
                hearthline != null && hearthline.id == MainStoryCatalog.HearthlineRouteId,
                "Hearthline route marker is missing or has the wrong ID.");
            Check(
                openCurrent != null && openCurrent.id == MainStoryCatalog.OpenCurrentRouteId,
                "Open Current route marker is missing or has the wrong ID.");
            Check(
                hearthline != null &&
                openCurrent != null &&
                hearthline.id != openCurrent.id &&
                hearthline.id != MainStoryCatalog.QuestId &&
                openCurrent.id != MainStoryCatalog.QuestId,
                "Main story quest and route marker IDs must be distinct.");
            Check(
                hearthline != null &&
                openCurrent != null &&
                hearthline.required == 1 &&
                openCurrent.required == 1 &&
                hearthline.favorReward == 0 &&
                openCurrent.favorReward == 0,
                "Route markers must be one-step, zero-reward persistence records.");

            var baseQuestIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var baseQuest in QuestCatalog.CreateAll())
            {
                if (baseQuest != null)
                {
                    baseQuestIds.Add(baseQuest.id);
                }
            }

            Check(
                !baseQuestIds.Contains(MainStoryCatalog.QuestId) &&
                !baseQuestIds.Contains(MainStoryCatalog.HearthlineRouteId) &&
                !baseQuestIds.Contains(MainStoryCatalog.OpenCurrentRouteId),
                "Main story runtime quest IDs collide with an existing QuestCatalog record.");
        }

        private static void VerifyFightRosters()
        {
            var definitionsField = typeof(EnemyCatalog).GetField(
                "Definitions",
                BindingFlags.Static | BindingFlags.NonPublic);
            Check(definitionsField != null, "EnemyCatalog.Definitions was not found for story verification.");
            if (definitionsField == null)
            {
                return;
            }

            var enemyDefinitions = definitionsField.GetValue(null) as Dictionary<string, EnemyDefinition>;
            Check(enemyDefinitions != null, "EnemyCatalog.Definitions has an unexpected type.");
            if (enemyDefinitions == null)
            {
                return;
            }

            VerifyWaveSet(
                "Alderwake defense",
                MainStoryCatalog.AlderwakeDefenseWaves,
                enemyDefinitions);
            VerifyWaveSet(
                "Morrowfen observatory",
                MainStoryCatalog.ObservatoryWaves,
                enemyDefinitions);

            Check(
                MainStoryCatalog.AlderwakeDefenseWaves.Length == 3,
                "Alderwake defense must have three authored pressure waves.");
            Check(
                MainStoryCatalog.ObservatoryWaves.Length == 3,
                "Morrowfen observatory must have three escalating room fights.");
        }

        private static void VerifyWaveSet(
            string label,
            string[][] waves,
            Dictionary<string, EnemyDefinition> enemyDefinitions)
        {
            Check(waves != null && waves.Length > 0, $"{label} has no waves.");
            if (waves == null)
            {
                return;
            }

            for (var waveIndex = 0; waveIndex < waves.Length; waveIndex++)
            {
                var wave = waves[waveIndex];
                Check(
                    wave != null && wave.Length >= 3 && wave.Length <= 5,
                    $"{label} wave {waveIndex + 1} must contain 3-5 enemies.");
                if (wave == null)
                {
                    continue;
                }

                for (var enemyIndex = 0; enemyIndex < wave.Length; enemyIndex++)
                {
                    Check(
                        !string.IsNullOrWhiteSpace(wave[enemyIndex]) &&
                        enemyDefinitions.ContainsKey(wave[enemyIndex]),
                        $"{label} wave {waveIndex + 1} references missing enemy '{wave[enemyIndex]}'.");
                }
            }
        }

        private static void VerifyBossDefinition()
        {
            var boss = BossCatalog.Get(MainStoryCatalog.BossId);
            Check(boss != null, $"BossCatalog is missing '{MainStoryCatalog.BossId}'.");
            if (boss == null)
            {
                return;
            }

            Check(boss.maxHealth > 0, "Hollow Cantor must have positive health.");
            Check(
                boss.phaseThresholdOne > boss.phaseThresholdTwo &&
                boss.phaseThresholdOne < 1f &&
                boss.phaseThresholdTwo > 0f,
                "Hollow Cantor needs two descending thresholds for three readable phases.");
            Check(
                boss.phaseOneActions != null && boss.phaseOneActions.Count >= 2,
                "Hollow Cantor phase one needs at least two actions.");
            Check(
                boss.phaseTwoActions != null && boss.phaseTwoActions.Count >= 3,
                "Hollow Cantor later phases need at least three actions.");
            Check(
                !string.IsNullOrWhiteSpace(boss.summonedEnemyId),
                "Hollow Cantor needs a catalogued reinforcement enemy.");
            Check(
                HollowCantorVisualBuilder.RendererBudget <= 18,
                "Hollow Cantor local art exceeds its 18-renderer budget.");
            Check(
                HollowCantorVisualBuilder.MaterialBudget <= 4,
                "Hollow Cantor local art exceeds its four-material budget.");
            Check(
                typeof(IDungeonBossArena).IsAssignableFrom(typeof(MainStoryBreach)),
                "MainStoryBreach must use the existing IDungeonBossArena architecture.");
        }

        private static void VerifyPlacementAndReentry()
        {
            ValidatePosition(MainStoryCatalog.RoutekeeperPosition, "Routekeeper position");
            ValidatePosition(MainStoryCatalog.AlderwakeDefenseCenter, "Alderwake defense center");
            ValidatePosition(MainStoryCatalog.ObservatoryGatePosition, "Observatory gate position");
            ValidatePosition(MainStoryCatalog.ObservatoryReturnPosition, "Observatory return position");
            ValidatePosition(MainStoryCatalog.ObservatoryCenter, "Observatory center");
            ValidatePosition(MainStoryCatalog.CinderwoldBreachCenter, "Cinderwold breach center");

            Check(
                Vector3.Distance(
                    MainStoryCatalog.RoutekeeperPosition,
                    MainStoryCatalog.AlderwakeDefenseCenter) >= 8f,
                "Routekeeper and Rainline defense signal are too close for unambiguous interaction.");
            Check(
                Vector3.Distance(
                    MainStoryCatalog.ObservatoryGatePosition,
                    MainStoryCatalog.ObservatoryReturnPosition) >= 3f,
                "Observatory return point must not overlap its entry interaction.");
            Check(
                Vector3.Distance(
                    MainStoryCatalog.ObservatoryGatePosition,
                    MainStoryCatalog.ObservatoryCenter) >= 80f,
                "The hidden observatory arena should be isolated from its overworld gate.");

            Check(
                typeof(IWorldInteractable).IsAssignableFrom(typeof(MainStoryObservatoryExit)),
                "The observatory needs an interactable retreat route.");
            Check(
                typeof(IWorldInteractable).IsAssignableFrom(typeof(MainStoryBreachRetreat)),
                "The Cinderwold breach needs an interactable retreat route.");
        }

        private static void VerifyRuntimeWiring()
        {
            var scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            var adventurePath = Path.Combine(scriptsRoot, "TetherfallAdventure.cs");
            var storyPath = Path.Combine(scriptsRoot, "TetherfallMainStory.cs");
            Check(File.Exists(adventurePath), $"Missing adventure source at '{adventurePath}'.");
            Check(File.Exists(storyPath), $"Missing main-story source at '{storyPath}'.");
            if (File.Exists(adventurePath))
            {
                var adventureSource = File.ReadAllText(adventurePath);
                Check(
                    adventureSource.Contains("TetherfallMainStoryBuilder.Build(game)"),
                    "WorldExpansionBuilder does not construct the main-story chapter.");
            }

            if (File.Exists(storyPath))
            {
                var storySource = File.ReadAllText(storyPath);
                Check(
                    !storySource.Contains("TetherfallSaveData") &&
                    !storySource.Contains("TetherfallSaveSystem"),
                    "Main story must derive persistence from quest records, not custom save fields.");
                Check(
                    storySource.Contains("game.GameplayPaused"),
                    "Main-story combat directors do not appear to honor gameplay pause.");
                Check(
                    storySource.Contains("MaximumEncounterDistance"),
                    "Main-story fights need a bounded far-distance interruption guard.");
            }
        }

        private static void ValidatePosition(Vector3 position, string label)
        {
            Check(
                IsFinite(position.x) && IsFinite(position.y) && IsFinite(position.z),
                $"{label} contains NaN or Infinity: {position}.");
            Check(
                Mathf.Abs(position.x) <= 1000f &&
                Mathf.Abs(position.y) <= 1000f &&
                Mathf.Abs(position.z) <= 1000f,
                $"{label} is outside the authored world envelope: {position}.");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void Check(bool condition, string failure)
        {
            assertionCount++;
            if (!condition && !Failures.Contains(failure))
            {
                Failures.Add(failure);
            }
        }

        private static void Complete()
        {
            if (Failures.Count == 0)
            {
                Debug.Log(
                    $"Tetherfall main-story verification passed: {assertionCount} assertions. " +
                    "Six persisted stages, three bounded set pieces, route-choice recovery, and boss budgets are valid.");
                return;
            }

            var message = new StringBuilder();
            message.Append("Tetherfall main-story verification failed with ");
            message.Append(Failures.Count);
            message.AppendLine(" actionable issue(s):");
            for (var index = 0; index < Failures.Count; index++)
            {
                message.Append(index + 1);
                message.Append(". ");
                message.AppendLine(Failures[index]);
            }

            message.Append($"Checked {assertionCount} assertions.");
            var failureMessage = message.ToString();
            Debug.LogError(failureMessage);
            throw new InvalidOperationException(failureMessage);
        }
    }
}
