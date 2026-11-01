using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallInfiniteSpireGenerationVerification
    {
        private const string MenuPath = "Tetherfall/Verify Infinite Spire Generation";
        private const int DeterministicFloorCount = 60;

        private static int assertionCount;

        [MenuItem(MenuPath, false, 2192)]
        public static void Verify()
        {
            VerifyInternal(true);
        }

        [MenuItem(MenuPath, true, 2192)]
        private static bool CanVerify()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        public static void VerifyFromCommandLine()
        {
            try
            {
                VerifyInternal(true);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                    return;
                }

                throw;
            }
        }

        public static void VerifyForFullGame()
        {
            VerifyInternal(false);
        }

        private static void VerifyInternal(bool logSuccess)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Infinite Spire generation verification is edit-mode only.");
            }

            assertionCount = 0;
            var failures = new List<string>();
            Run("catalog completeness", failures, () => VerifyCatalog(failures));
            Run("catalog references", failures, () => VerifyReferences(failures));
            Run(
                "deterministic generation",
                failures,
                () => VerifyGeneration(failures));
            Run("instance IDs", failures, () => VerifyInstanceIds(failures));
            Run("scaling", failures, () => VerifyScaling(failures));
            Run("budgets", failures, () => VerifyBudgets(failures));
            Run("pure runtime files", failures, () => VerifyRuntimePurity(failures));

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Infinite Spire generation verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            if (logSuccess)
            {
                Debug.Log(
                    $"Infinite Spire generation verification passed: {assertionCount} assertions; " +
                    $"{InfiniteSpireCatalog.Templates.Count} immutable room templates, " +
                    $"{DeterministicFloorCount} deterministic floors, bounded scaling, stable IDs, " +
                    "valid current hostile references, and conservative streaming budgets.");
            }
        }

        private static void VerifyCatalog(List<string> failures)
        {
            Check(
                InfiniteSpireCatalog.DungeonId == "dungeon.infinite_spire",
                "Dungeon ID must remain 'dungeon.infinite_spire'.",
                failures);
            Check(
                InfiniteSpireCatalog.Templates != null &&
                InfiniteSpireCatalog.Templates.Count > 0,
                "Infinite Spire template catalog is empty.",
                failures);

            var allIds = new HashSet<string>(StringComparer.Ordinal);
            var categoryCounts = new int[10];
            var restCount = 0;
            for (var index = 0; index < InfiniteSpireCatalog.Templates.Count; index++)
            {
                var template = InfiniteSpireCatalog.Templates[index];
                Check(template != null, $"Template index {index} is null.", failures);
                if (template == null)
                {
                    continue;
                }

                RegisterTemplateId(allIds, template.Id, "room template", failures);
                Check(
                    !string.IsNullOrWhiteSpace(template.DisplayName),
                    $"Template '{template.Id}' has no display name.",
                    failures);
                Check(
                    template.DisplayName.IndexOf('\u2014') < 0,
                    $"Template '{template.Id}' display name contains an em dash.",
                    failures);
                Check(
                    Enum.IsDefined(typeof(InfiniteSpireRoomCategory), template.Category),
                    $"Template '{template.Id}' has invalid category {template.Category}.",
                    failures);
                Check(
                    template.AllowedBeats != InfiniteSpireRoomBeatMask.None,
                    $"Template '{template.Id}' has no allowed floor beat.",
                    failures);
                Check(
                    template.LayoutVariantCount >= 1,
                    $"Template '{template.Id}' has no layout variants.",
                    failures);

                var categoryIndex = (int)template.Category;
                if (categoryIndex >= 0 && categoryIndex < categoryCounts.Length)
                {
                    categoryCounts[categoryIndex]++;
                }

                if (template.Category == InfiniteSpireRoomCategory.Rest)
                {
                    restCount++;
                    Check(
                        template.Id == InfiniteSpireIds.RestLoomrestMeridian &&
                        template.DisplayName == "Loomrest Meridian",
                        "The one Rest template must be Loomrest Meridian with its locked stable ID.",
                        failures);
                }

                if (template.Category == InfiniteSpireRoomCategory.Puzzle)
                {
                    Check(
                        template.PuzzleKind != InfiniteSpirePuzzleKind.None,
                        $"Puzzle template '{template.Id}' has no puzzle kind.",
                        failures);
                    Check(
                        template.MechanismCount >= 3 &&
                        template.MechanismCount <= 4,
                        $"Puzzle template '{template.Id}' must use three or four mechanisms.",
                        failures);
                }
                else
                {
                    Check(
                        template.PuzzleKind == InfiniteSpirePuzzleKind.None &&
                        template.MechanismCount == 0,
                        $"Non-puzzle template '{template.Id}' contains puzzle metadata.",
                        failures);
                }

                if (template.Category == InfiniteSpireRoomCategory.Merchant)
                {
                    Check(
                        template.MerchantKit != null,
                        $"Merchant template '{template.Id}' has no stock kit.",
                        failures);
                }
                else
                {
                    Check(
                        template.MerchantKit == null,
                        $"Non-merchant template '{template.Id}' has a merchant kit.",
                        failures);
                }
            }

            CheckCategoryCount(
                categoryCounts,
                InfiniteSpireRoomCategory.Easy,
                4,
                failures);
            CheckCategoryCount(
                categoryCounts,
                InfiniteSpireRoomCategory.Combat,
                4,
                failures);
            CheckCategoryCount(
                categoryCounts,
                InfiniteSpireRoomCategory.Swarm,
                4,
                failures);
            CheckCategoryCount(
                categoryCounts,
                InfiniteSpireRoomCategory.Traversal,
                2,
                failures);
            CheckCategoryCount(
                categoryCounts,
                InfiniteSpireRoomCategory.Puzzle,
                3,
                failures);
            CheckCategoryCount(
                categoryCounts,
                InfiniteSpireRoomCategory.Miniboss,
                4,
                failures);
            CheckCategoryCount(
                categoryCounts,
                InfiniteSpireRoomCategory.Boss,
                4,
                failures);
            CheckCategoryCount(
                categoryCounts,
                InfiniteSpireRoomCategory.Treasure,
                2,
                failures);
            CheckCategoryCount(
                categoryCounts,
                InfiniteSpireRoomCategory.Merchant,
                3,
                failures);
            Check(restCount == 1, $"Expected exactly one Rest template, found {restCount}.", failures);

            Check(
                InfiniteSpireCatalog.MerchantKits.Count >= 3,
                $"Expected at least three merchant kits, found {InfiniteSpireCatalog.MerchantKits.Count}.",
                failures);
            for (var index = 0; index < InfiniteSpireCatalog.MerchantKits.Count; index++)
            {
                var kit = InfiniteSpireCatalog.MerchantKits[index];
                Check(kit != null, $"Merchant kit index {index} is null.", failures);
                if (kit == null)
                {
                    continue;
                }

                RegisterTemplateId(allIds, kit.Id, "merchant kit", failures);
                Check(
                    kit.Offers != null && kit.Offers.Count >= 3,
                    $"Merchant kit '{kit.Id}' needs at least three offers.",
                    failures);
            }

            Check(
                InfiniteSpireCatalog.MerchantOffers.Count >= 3,
                "Merchant offer catalog is too shallow.",
                failures);
            for (var index = 0; index < InfiniteSpireCatalog.MerchantOffers.Count; index++)
            {
                var offer = InfiniteSpireCatalog.MerchantOffers[index];
                Check(offer != null, $"Merchant offer index {index} is null.", failures);
                if (offer == null)
                {
                    continue;
                }

                RegisterTemplateId(allIds, offer.Id, "merchant offer", failures);
                Check(
                    offer.BaseSpirecoilCost > 0,
                    $"Merchant offer '{offer.Id}' must have a positive Spirecoil cost.",
                    failures);
            }

            Check(
                InfiniteSpireCatalog.MinibossProfiles.Count >= 4,
                $"Expected at least four miniboss profiles, found " +
                $"{InfiniteSpireCatalog.MinibossProfiles.Count}.",
                failures);
            for (var index = 0; index < InfiniteSpireCatalog.MinibossProfiles.Count; index++)
            {
                var profile = InfiniteSpireCatalog.MinibossProfiles[index];
                Check(profile != null, $"Miniboss profile index {index} is null.", failures);
                if (profile != null)
                {
                    RegisterTemplateId(allIds, profile.Id, "miniboss profile", failures);
                }
            }

            Check(
                InfiniteSpireCatalog.BossProfiles.Count >= 6,
                $"Expected at least six boss profiles, found {InfiniteSpireCatalog.BossProfiles.Count}.",
                failures);
            for (var index = 0; index < InfiniteSpireCatalog.BossProfiles.Count; index++)
            {
                var profile = InfiniteSpireCatalog.BossProfiles[index];
                Check(profile != null, $"Boss profile index {index} is null.", failures);
                if (profile != null)
                {
                    RegisterTemplateId(allIds, profile.Id, "boss profile", failures);
                }
            }

            VerifyImmutableDefinitionType(
                typeof(InfiniteSpireTemplateDefinition),
                failures);
            VerifyImmutableDefinitionType(
                typeof(InfiniteSpireMinibossProfile),
                failures);
            VerifyImmutableDefinitionType(
                typeof(InfiniteSpireBossProfile),
                failures);
            VerifyImmutableDefinitionType(
                typeof(InfiniteSpireMerchantKitDefinition),
                failures);
        }

        private static void VerifyReferences(List<string> failures)
        {
            var enemies = GetPrivateStaticField<Dictionary<string, EnemyDefinition>>(
                typeof(EnemyCatalog),
                "Definitions");
            Check(enemies != null && enemies.Count > 0, "Enemy catalog could not be loaded.", failures);
            if (enemies == null)
            {
                return;
            }

            for (var templateIndex = 0;
                 templateIndex < InfiniteSpireCatalog.Templates.Count;
                 templateIndex++)
            {
                var template = InfiniteSpireCatalog.Templates[templateIndex];
                if (template?.WaveSets == null)
                {
                    continue;
                }

                for (var setIndex = 0; setIndex < template.WaveSets.Count; setIndex++)
                {
                    var set = template.WaveSets[setIndex];
                    Check(
                        set != null && set.Waves != null && set.Waves.Count > 0,
                        $"Template '{template.Id}' wave set {setIndex} is empty.",
                        failures);
                    if (set?.Waves == null)
                    {
                        continue;
                    }

                    for (var waveIndex = 0; waveIndex < set.Waves.Count; waveIndex++)
                    {
                        var wave = set.Waves[waveIndex];
                        Check(
                            wave != null &&
                            wave.EnemyIds != null &&
                            wave.EnemyIds.Count > 0,
                            $"Template '{template.Id}' wave {setIndex}:{waveIndex} is empty.",
                            failures);
                        if (wave?.EnemyIds == null)
                        {
                            continue;
                        }

                        for (var enemyIndex = 0;
                             enemyIndex < wave.EnemyIds.Count;
                             enemyIndex++)
                        {
                            ValidateEnemyReference(
                                enemies,
                                wave.EnemyIds[enemyIndex],
                                $"Template '{template.Id}' wave {setIndex}:{waveIndex}",
                                failures);
                        }
                    }
                }
            }

            for (var index = 0; index < InfiniteSpireCatalog.MinibossProfiles.Count; index++)
            {
                var profile = InfiniteSpireCatalog.MinibossProfiles[index];
                if (profile != null)
                {
                    ValidateEnemyReference(
                        enemies,
                        profile.EnemyId,
                        $"Miniboss profile '{profile.Id}'",
                        failures);
                }
            }

            for (var index = 0; index < InfiniteSpireCatalog.BossProfiles.Count; index++)
            {
                var profile = InfiniteSpireCatalog.BossProfiles[index];
                if (profile == null)
                {
                    continue;
                }

                Check(
                    BossCatalog.Get(profile.BossId) != null,
                    $"Boss profile '{profile.Id}' references missing boss '{profile.BossId}'.",
                    failures);
                if (!string.IsNullOrEmpty(profile.SupportEnemyId))
                {
                    ValidateEnemyReference(
                        enemies,
                        profile.SupportEnemyId,
                        $"Boss profile '{profile.Id}'",
                        failures);
                }
            }

            for (var index = 0; index < InfiniteSpireCatalog.MerchantKits.Count; index++)
            {
                var kit = InfiniteSpireCatalog.MerchantKits[index];
                if (kit?.Offers == null)
                {
                    continue;
                }

                var offerIds = new HashSet<string>(StringComparer.Ordinal);
                for (var offerIndex = 0; offerIndex < kit.Offers.Count; offerIndex++)
                {
                    var offer = kit.Offers[offerIndex];
                    Check(
                        offer != null &&
                        ReferenceEquals(
                            offer,
                            InfiniteSpireCatalog.GetMerchantOffer(offer.Id)),
                        $"Merchant kit '{kit.Id}' offer {offerIndex} is not canonical.",
                        failures);
                    Check(
                        offer != null && offerIds.Add(offer.Id),
                        $"Merchant kit '{kit.Id}' repeats offer '{offer?.Id}'.",
                        failures);
                }
            }
        }

        private static void VerifyGeneration(List<string> failures)
        {
            VerifyOneSequence(unchecked((int)0x9e3779b9u), failures);
            VerifyOneSequence(-1, failures);
        }

        private static void VerifyOneSequence(int runSeed, List<string> failures)
        {
            var firstState = new InfiniteSpireGenerationState();
            var secondState = new InfiniteSpireGenerationState();
            var first = new InfiniteSpireFloorPlan();
            var second = new InfiniteSpireFloorPlan();

            var recentTemplates = new string[InfiniteSpireGenerationState.TemplateHistoryCapacity];
            var recentTemplateCount = 0;
            var previousCategory = (InfiniteSpireRoomCategory)(-1);
            var categoryBeforePrevious = (InfiniteSpireRoomCategory)(-1);
            var previousFloorHadMerchant = false;
            var recentBosses = new string[InfiniteSpireGenerationState.BossHistoryCapacity];
            var recentBossCount = 0;

            for (var floor = 1; floor <= DeterministicFloorCount; floor++)
            {
                Check(
                    InfiniteSpireGenerator.TryGenerateFloor(runSeed, floor, firstState, first),
                    $"Seed {runSeed} floor {floor} failed first generation.",
                    failures);
                Check(
                    InfiniteSpireGenerator.TryGenerateFloor(runSeed, floor, secondState, second),
                    $"Seed {runSeed} floor {floor} failed repeat generation.",
                    failures);
                CompareFloorPlans(first, second, runSeed, floor, failures);

                Check(
                    first.Count >= InfiniteSpireFloorPlan.MinimumRoomCount &&
                    first.Count <= InfiniteSpireFloorPlan.MaximumRoomCount,
                    $"Seed {runSeed} floor {floor} produced {first.Count} rooms.",
                    failures);
                var scheduledSpecialCount =
                    (InfiniteSpireCadenceRules.IsBossFloor(floor) ||
                     InfiniteSpireCadenceRules.IsMinibossFloor(floor)
                        ? 1
                        : 0) +
                    (InfiniteSpireCadenceRules.IsMerchantEligibleFloor(floor)
                        ? 1
                        : 0) +
                    (InfiniteSpireCadenceRules.IsLoomrestMeridianFloor(floor)
                        ? 1
                        : 0);
                scheduledSpecialCount = Math.Max(1, scheduledSpecialCount);
                var expectedRoomCount = Math.Min(
                    InfiniteSpireFloorPlan.MaximumRoomCount,
                    Math.Max(3 + floor % 3, 2 + scheduledSpecialCount));
                Check(
                    first.Count == expectedRoomCount,
                    $"Seed {runSeed} floor {floor} produced {first.Count} rooms; " +
                    $"expected {expectedRoomCount} from floor cadence.",
                    failures);
                if (first.Count == 0)
                {
                    continue;
                }

                Check(
                    first.GetRoom(0).Beat == InfiniteSpireRoomBeat.Approach,
                    $"Seed {runSeed} floor {floor} does not begin with Approach.",
                    failures);
                Check(
                    first.GetRoom(first.Count - 1).Beat == InfiniteSpireRoomBeat.Seal,
                    $"Seed {runSeed} floor {floor} does not end with Seal.",
                    failures);

                var pressureCount = 0;
                var bossCount = 0;
                var minibossCount = 0;
                var merchantCount = 0;
                var restCount = 0;
                var treasureCount = 0;
                var cadenceStarted = false;

                for (var roomIndex = 0; roomIndex < first.Count; roomIndex++)
                {
                    var room = first.GetRoom(roomIndex);
                    var template = room.Template;
                    Check(
                        template != null,
                        $"Seed {runSeed} floor {floor} room {roomIndex} has no template.",
                        failures);
                    if (template == null)
                    {
                        continue;
                    }

                    Check(
                        room.InstanceKey ==
                        new InfiniteSpireRoomInstanceKey(runSeed, floor, roomIndex),
                        $"Seed {runSeed} floor {floor} room {roomIndex} has the wrong instance key.",
                        failures);
                    Check(
                        template.SupportsBeat(room.Beat),
                        $"Template '{template.Id}' does not support generated beat {room.Beat}.",
                        failures);
                    Check(
                        room.LayoutVariant >= 0 &&
                        room.LayoutVariant < template.LayoutVariantCount,
                        $"Template '{template.Id}' generated invalid layout {room.LayoutVariant}.",
                        failures);
                    Check(
                        room.RotationQuarterTurns >= 0 &&
                        room.RotationQuarterTurns <= 3,
                        $"Template '{template.Id}' generated invalid quarter rotation.",
                        failures);
                    Check(
                        room.RehashAttempts >= 0 &&
                        room.RehashAttempts <= InfiniteSpireGenerator.MaximumRehashAttempts,
                        $"Template '{template.Id}' exceeded the rehash bound.",
                        failures);

                    for (var historyIndex = 0;
                         historyIndex < recentTemplateCount;
                         historyIndex++)
                    {
                        Check(
                            !string.Equals(
                                recentTemplates[historyIndex],
                                template.Id,
                                StringComparison.Ordinal),
                            $"Template '{template.Id}' repeated inside the last-eight window " +
                            $"at floor {floor}, room {roomIndex}.",
                            failures);
                    }

                    Check(
                        !(previousCategory == template.Category &&
                          categoryBeforePrevious == template.Category),
                        $"Category {template.Category} appeared three times at floor {floor}, " +
                        $"room {roomIndex}.",
                        failures);
                    Check(
                        !(previousCategory == InfiniteSpireRoomCategory.Puzzle &&
                          template.Category == InfiniteSpireRoomCategory.Puzzle),
                        $"Puzzle rooms are adjacent at floor {floor}, room {roomIndex}.",
                        failures);
                    Check(
                        !(previousCategory == InfiniteSpireRoomCategory.Swarm &&
                          template.Category == InfiniteSpireRoomCategory.Swarm),
                        $"Swarm rooms are adjacent at floor {floor}, room {roomIndex}.",
                        failures);

                    PushRecentTemplate(
                        recentTemplates,
                        ref recentTemplateCount,
                        template.Id);
                    categoryBeforePrevious = previousCategory;
                    previousCategory = template.Category;

                    if (room.Beat == InfiniteSpireRoomBeat.Pressure)
                    {
                        pressureCount++;
                        Check(
                            !cadenceStarted,
                            $"Pressure room appears after cadence at floor {floor}.",
                            failures);
                    }
                    else if (room.Beat == InfiniteSpireRoomBeat.Cadence)
                    {
                        cadenceStarted = true;
                    }

                    switch (template.Category)
                    {
                        case InfiniteSpireRoomCategory.Boss:
                            bossCount++;
                            Check(
                                room.BossProfile != null,
                                $"Boss room at floor {floor} has no boss profile.",
                                failures);
                            if (room.BossProfile != null)
                            {
                                for (var bossIndex = 0;
                                     bossIndex < recentBossCount;
                                     bossIndex++)
                                {
                                    Check(
                                        !string.Equals(
                                            recentBosses[bossIndex],
                                            room.BossProfile.BossId,
                                            StringComparison.Ordinal),
                                        $"Boss '{room.BossProfile.BossId}' repeated inside the " +
                                        $"last-three boss window at floor {floor}.",
                                        failures);
                                }

                                PushRecentBoss(
                                    recentBosses,
                                    ref recentBossCount,
                                    room.BossProfile.BossId);
                            }

                            break;
                        case InfiniteSpireRoomCategory.Miniboss:
                            minibossCount++;
                            Check(
                                room.MinibossProfile != null,
                                $"Miniboss room at floor {floor} has no profile.",
                                failures);
                            break;
                        case InfiniteSpireRoomCategory.Merchant:
                            merchantCount++;
                            Check(
                                InfiniteSpireCadenceRules.IsMerchantEligibleFloor(floor),
                                $"Merchant generated on ineligible floor {floor}.",
                                failures);
                            Check(
                                room.MerchantStock.Kit != null &&
                                room.MerchantStock.Count == 3,
                                $"Merchant room on floor {floor} has incomplete deterministic stock.",
                                failures);
                            break;
                        case InfiniteSpireRoomCategory.Rest:
                            restCount++;
                            Check(
                                template.Id == InfiniteSpireIds.RestLoomrestMeridian,
                                $"Floor {floor} generated an unknown Rest template.",
                                failures);
                            break;
                        case InfiniteSpireRoomCategory.Treasure:
                            treasureCount++;
                            Check(
                                room.TreasureSpirecoil >= 12 &&
                                room.TreasureSpirecoil <= 28,
                                $"Treasure room on floor {floor} has out-of-range Spirecoil.",
                                failures);
                            break;
                    }

                    if (template.Category == InfiniteSpireRoomCategory.Puzzle)
                    {
                        VerifyPuzzleSelection(room, floor, failures);
                    }
                }

                Check(
                    pressureCount >= 0 && pressureCount <= 2,
                    $"Floor {floor} has {pressureCount} pressure rooms.",
                    failures);
                Check(
                    bossCount == (InfiniteSpireCadenceRules.IsBossFloor(floor) ? 1 : 0),
                    $"Floor {floor} boss cadence is incorrect.",
                    failures);
                Check(
                    minibossCount ==
                    (InfiniteSpireCadenceRules.IsMinibossFloor(floor) ? 1 : 0),
                    $"Floor {floor} miniboss cadence is incorrect.",
                    failures);
                Check(
                    restCount ==
                    (InfiniteSpireCadenceRules.IsLoomrestMeridianFloor(floor) ? 1 : 0),
                    $"Floor {floor} Loomrest Meridian cadence is incorrect.",
                    failures);
                Check(
                    merchantCount ==
                    (InfiniteSpireCadenceRules.IsMerchantEligibleFloor(floor) ? 1 : 0),
                    $"Floor {floor} merchant cadence is incorrect.",
                    failures);
                Check(
                    !(previousFloorHadMerchant && merchantCount > 0),
                    $"Merchants appeared on consecutive floors ending at {floor}.",
                    failures);
                Check(
                    bossCount == 0 || treasureCount == 0,
                    $"Boss floor {floor} contains treasure before its boss.",
                    failures);
                if (floor == 1)
                {
                    Check(bossCount == 0, "Floor 1 generated a boss.", failures);
                    for (var index = 0; index < first.Count; index++)
                    {
                        var category = first.GetRoom(index).Template.Category;
                        Check(
                            category == InfiniteSpireRoomCategory.Easy ||
                            category == InfiniteSpireRoomCategory.Combat,
                            $"Floor 1 generated disallowed category {category}.",
                            failures);
                    }
                }

                previousFloorHadMerchant = merchantCount > 0;
            }
        }

        private static void CompareFloorPlans(
            InfiniteSpireFloorPlan first,
            InfiniteSpireFloorPlan second,
            int runSeed,
            int floor,
            List<string> failures)
        {
            Check(
                first.Count == second.Count && first.Cadence == second.Cadence,
                $"Seed {runSeed} floor {floor} changed room count or cadence on repeat.",
                failures);
            var count = Math.Min(first.Count, second.Count);
            for (var index = 0; index < count; index++)
            {
                var left = first.GetRoom(index);
                var right = second.GetRoom(index);
                Check(
                    left.InstanceKey == right.InstanceKey &&
                    left.Beat == right.Beat &&
                    left.Template?.Id == right.Template?.Id &&
                    left.RoomSeed == right.RoomSeed &&
                    left.LayoutVariant == right.LayoutVariant &&
                    left.RotationQuarterTurns == right.RotationQuarterTurns &&
                    left.WaveSetIndex == right.WaveSetIndex &&
                    left.TreasureSpirecoil == right.TreasureSpirecoil &&
                    left.MinibossProfile?.Id == right.MinibossProfile?.Id &&
                    left.BossProfile?.Id == right.BossProfile?.Id,
                    $"Seed {runSeed} floor {floor} room {index} is not deterministic.",
                    failures);
                Check(
                    left.Puzzle.Kind == right.Puzzle.Kind &&
                    left.Puzzle.Count == right.Puzzle.Count &&
                    left.Puzzle.StateCount == right.Puzzle.StateCount,
                    $"Seed {runSeed} floor {floor} room {index} changed puzzle metadata.",
                    failures);
                for (var puzzleIndex = 0;
                     puzzleIndex < Math.Min(left.Puzzle.Count, right.Puzzle.Count);
                     puzzleIndex++)
                {
                    Check(
                        left.Puzzle.GetValue(puzzleIndex) ==
                        right.Puzzle.GetValue(puzzleIndex),
                        $"Seed {runSeed} floor {floor} room {index} changed puzzle solution.",
                        failures);
                }

                Check(
                    left.MerchantStock.Kit?.Id == right.MerchantStock.Kit?.Id &&
                    left.MerchantStock.Count == right.MerchantStock.Count,
                    $"Seed {runSeed} floor {floor} room {index} changed merchant kit.",
                    failures);
                for (var stockIndex = 0;
                     stockIndex < Math.Min(
                         left.MerchantStock.Count,
                         right.MerchantStock.Count);
                     stockIndex++)
                {
                    Check(
                        left.MerchantStock.GetOffer(stockIndex)?.Id ==
                        right.MerchantStock.GetOffer(stockIndex)?.Id,
                        $"Seed {runSeed} floor {floor} room {index} changed stock selection.",
                        failures);
                }
            }
        }

        private static void VerifyPuzzleSelection(
            InfiniteSpireRoomPlan room,
            int floor,
            List<string> failures)
        {
            var puzzle = room.Puzzle;
            Check(
                puzzle.Kind == room.Template.PuzzleKind &&
                puzzle.Count == room.Template.MechanismCount,
                $"Puzzle room '{room.Template.Id}' on floor {floor} has mismatched metadata.",
                failures);
            var seen = new bool[4];
            for (var index = 0; index < puzzle.Count; index++)
            {
                var value = puzzle.GetValue(index);
                if (puzzle.Kind == InfiniteSpirePuzzleKind.OrderedSequence)
                {
                    Check(
                        value >= 0 && value < puzzle.Count,
                        $"Ordered puzzle '{room.Template.Id}' has invalid value {value}.",
                        failures);
                    if (value >= 0 && value < seen.Length)
                    {
                        Check(
                            !seen[value],
                            $"Ordered puzzle '{room.Template.Id}' repeats value {value}.",
                            failures);
                        seen[value] = true;
                    }
                }
                else
                {
                    Check(
                        value >= 0 && value < puzzle.StateCount,
                        $"State puzzle '{room.Template.Id}' has invalid value {value}.",
                        failures);
                }
            }
        }

        private static void VerifyInstanceIds(List<string> failures)
        {
            var seeds = new[] { int.MinValue, -1, 0, 1, int.MaxValue };
            var buffer = new char[InfiniteSpireInstanceIds.MaximumInstanceIdLength];
            for (var seedIndex = 0; seedIndex < seeds.Length; seedIndex++)
            {
                var room = new InfiniteSpireRoomInstanceKey(
                    seeds[seedIndex],
                    seedIndex == seeds.Length - 1 ? int.MaxValue : 10000 + seedIndex,
                    seedIndex * 3);
                var roomId = InfiniteSpireInstanceIds.FormatRoomId(room);
                Check(
                    InfiniteSpireInstanceIds.TryParseRoomId(roomId, out var parsedRoom) &&
                    parsedRoom == room,
                    $"Room ID '{roomId}' did not round trip.",
                    failures);
                Check(
                    InfiniteSpireInstanceIds.TryFormatRoomId(
                        room,
                        buffer,
                        0,
                        out var written) &&
                    written == roomId.Length &&
                    new string(buffer, 0, written) == roomId,
                    $"Room ID '{roomId}' caller-buffer format differs.",
                    failures);

                VerifyEntityId(
                    new InfiniteSpireInstanceId(
                        room,
                        InfiniteSpireInstanceEntityKind.Wave,
                        2),
                    failures);
                VerifyEntityId(
                    new InfiniteSpireInstanceId(
                        room,
                        InfiniteSpireInstanceEntityKind.Mechanism,
                        3),
                    failures);
                VerifyEntityId(
                    new InfiniteSpireInstanceId(
                        room,
                        InfiniteSpireInstanceEntityKind.Puzzle),
                    failures);
                VerifyEntityId(
                    new InfiniteSpireInstanceId(
                        room,
                        InfiniteSpireInstanceEntityKind.Chest),
                    failures);
                VerifyEntityId(
                    new InfiniteSpireInstanceId(
                        room,
                        InfiniteSpireInstanceEntityKind.Boss),
                    failures);
                VerifyEntityId(
                    new InfiniteSpireInstanceId(
                        room,
                        InfiniteSpireInstanceEntityKind.Stock,
                        1),
                    failures);
            }

            var malformed = new[]
            {
                string.Empty,
                "spire.room",
                "spire.room.zzzzzzzz.1.0",
                "spire.room.00000000.0.0",
                "spire.room.00000000.1.-1",
                "spire.room.00000000.1.0.wave.-1",
                "spire.room.00000000.1.0.unknown"
            };
            for (var index = 0; index < malformed.Length; index++)
            {
                Check(
                    !InfiniteSpireInstanceIds.TryParse(malformed[index], out _),
                    $"Malformed instance ID '{malformed[index]}' was accepted.",
                    failures);
            }
        }

        private static void VerifyEntityId(
            InfiniteSpireInstanceId instanceId,
            List<string> failures)
        {
            var formatted = InfiniteSpireInstanceIds.Format(instanceId);
            Check(
                !string.IsNullOrEmpty(formatted) &&
                InfiniteSpireInstanceIds.TryParse(formatted, out var parsed) &&
                parsed == instanceId,
                $"Instance ID '{formatted}' did not round trip.",
                failures);
        }

        private static void VerifyScaling(List<string> failures)
        {
            var floors = new[] { 1, 10, 50, 120, 200, 10000 };
            var previousReward = 0f;
            for (var index = 0; index < floors.Length; index++)
            {
                var floor = floors[index];
                var scale = InfiniteSpireScaling.Evaluate(floor, 1.16f);
                CheckFiniteAndRange(
                    scale.ConcordDamp,
                    InfiniteSpireScaling.MinimumConcordDamp,
                    InfiniteSpireScaling.MaximumConcordDamp,
                    $"Floor {floor} Concord damp",
                    failures);
                CheckFiniteAndRange(
                    scale.EnemyHealthMultiplier,
                    0f,
                    InfiniteSpireScaling.EnemyHealthMultiplierCap,
                    $"Floor {floor} enemy HP multiplier",
                    failures);
                CheckFiniteAndRange(
                    scale.EnemyDamageMultiplier,
                    1f,
                    InfiniteSpireScaling.EnemyDamageMultiplierCap,
                    $"Floor {floor} enemy damage multiplier",
                    failures);
                CheckFiniteAndRange(
                    scale.BossHealthMultiplier,
                    1f,
                    InfiniteSpireScaling.BossHealthMultiplierCap,
                    $"Floor {floor} boss HP multiplier",
                    failures);
                CheckFiniteAndRange(
                    scale.BossMoveMultiplier,
                    1f,
                    InfiniteSpireScaling.BossMoveMultiplierCap,
                    $"Floor {floor} boss move multiplier",
                    failures);
                CheckFiniteAndRange(
                    scale.TelegraphDurationMultiplier,
                    InfiniteSpireScaling.MinimumTelegraphDurationMultiplier,
                    1f,
                    $"Floor {floor} telegraph multiplier",
                    failures);
                CheckFiniteAndRange(
                    scale.RewardMultiplier,
                    1f,
                    3.75f,
                    $"Floor {floor} reward multiplier",
                    failures);
                CheckFiniteAndRange(
                    scale.TreasureChanceBonus,
                    0f,
                    InfiniteSpireScaling.MaximumTreasureChanceBonus,
                    $"Floor {floor} treasure chance",
                    failures);
                Check(
                    scale.RewardMultiplier + 0.0001f >= previousReward,
                    $"Reward curve decreased at floor {floor}.",
                    failures);
                previousReward = scale.RewardMultiplier;
            }

            CheckApproximately(
                InfiniteSpireScaling.EnemyHealthMultiplier(1, 1f),
                1.065f,
                "Floor 1 enemy HP formula changed.",
                failures);
            CheckApproximately(
                InfiniteSpireScaling.EnemyDamageMultiplier(1),
                1.045f,
                "Floor 1 enemy damage formula changed.",
                failures);
            CheckApproximately(
                InfiniteSpireScaling.BossHealthMultiplier(1),
                1.055f,
                "Floor 1 boss HP formula changed.",
                failures);
            CheckApproximately(
                InfiniteSpireScaling.RewardMultiplier(1),
                1.04f,
                "Floor 1 reward formula changed.",
                failures);
            CheckApproximately(
                InfiniteSpireScaling.ConcordDamp(1.16f),
                0.92f,
                "Full Concord damp must resolve to 0.92.",
                failures);

            var bad = InfiniteSpireScaling.Evaluate(int.MinValue, float.NaN);
            Check(
                bad.FloorIndex == 1 &&
                IsFinite(bad.EnemyHealthMultiplier) &&
                IsFinite(bad.BossHealthMultiplier),
                "Bad floor or Concord input did not normalize safely.",
                failures);
            Check(
                InfiniteSpireScaling.ScaleEnemyHealth(int.MaxValue, int.MaxValue, float.PositiveInfinity) <=
                InfiniteSpireScaling.MaximumSafeBaseInteger,
                "Extreme enemy health did not clamp safely.",
                failures);
            Check(
                InfiniteSpireScaling.ScaleEnemyDamage(int.MinValue, 10000) == 1,
                "Invalid enemy damage did not recover to a positive safe value.",
                failures);
            var telegraph = InfiniteSpireScaling.ScaleTelegraphSeconds(
                float.PositiveInfinity,
                10000);
            Check(
                IsFinite(telegraph) &&
                telegraph >= InfiniteSpireScaling.MinimumSafeTelegraphSeconds *
                InfiniteSpireScaling.MinimumTelegraphDurationMultiplier,
                "Nonfinite telegraph input did not recover safely.",
                failures);
            var baseline = 0.72f;
            Check(
                InfiniteSpireScaling.ScaleTelegraphSeconds(baseline, 10000) + 0.0001f >=
                baseline * InfiniteSpireScaling.MinimumTelegraphDurationMultiplier,
                "Telegraph duration fell below 0.85 of its baseline.",
                failures);
        }

        private static void VerifyBudgets(List<string> failures)
        {
            Check(
                InfiniteSpireLoadedWindowBudgets.MaximumLoadedRooms == 3,
                "Loaded room window must remain previous, current, and next stub.",
                failures);
            Check(
                InfiniteSpireLoadedWindowBudgets.MaximumFullyActiveRooms == 1,
                "Only one loaded room may be fully active.",
                failures);
            Check(
                InfiniteSpireLoadedWindowBudgets.MaximumLoadedRenderers <= 110 &&
                InfiniteSpireLoadedWindowBudgets.MaximumLoadedColliders <= 72,
                "Loaded-window geometry budgets exceed their conservative caps.",
                failures);
            Check(
                InfiniteSpireLoadedWindowBudgets.MaximumAddedLights == 0,
                "Infinite Spire runtime budget must add zero lights.",
                failures);
            Check(
                InfiniteSpireLoadedWindowBudgets.MaximumActiveEnemies <= 8 &&
                InfiniteSpireLoadedWindowBudgets.MaximumActiveBosses == 1,
                "Loaded-window actor budgets are unsafe.",
                failures);
            Check(
                InfiniteSpireLoadedWindowBudgets.MaximumBossRoomRenderers +
                2 * InfiniteSpireLoadedWindowBudgets.MaximumStubRenderers <=
                InfiniteSpireLoadedWindowBudgets.MaximumLoadedRenderers,
                "Boss room plus two stubs exceeds the loaded renderer budget.",
                failures);
            Check(
                InfiniteSpireLoadedWindowBudgets.MaximumBossRoomColliders +
                2 * InfiniteSpireLoadedWindowBudgets.MaximumStubColliders <=
                InfiniteSpireLoadedWindowBudgets.MaximumLoadedColliders,
                "Boss room plus two stubs exceeds the loaded collider budget.",
                failures);

            for (var index = 0; index < InfiniteSpireCatalog.Templates.Count; index++)
            {
                var template = InfiniteSpireCatalog.Templates[index];
                var budget = template.GeometryBudget;
                var boss = template.Category == InfiniteSpireRoomCategory.Boss;
                Check(
                    budget.Renderers > 0 &&
                    budget.Renderers <=
                    (boss
                        ? InfiniteSpireLoadedWindowBudgets.MaximumBossRoomRenderers
                        : InfiniteSpireLoadedWindowBudgets.MaximumStandardRoomRenderers),
                    $"Template '{template.Id}' renderer budget is invalid.",
                    failures);
                Check(
                    budget.Colliders > 0 &&
                    budget.Colliders <=
                    (boss
                        ? InfiniteSpireLoadedWindowBudgets.MaximumBossRoomColliders
                        : InfiniteSpireLoadedWindowBudgets.MaximumStandardRoomColliders),
                    $"Template '{template.Id}' collider budget is invalid.",
                    failures);
                Check(
                    budget.AddedLights == 0,
                    $"Template '{template.Id}' must add zero lights.",
                    failures);
                Check(
                    budget.ParticleSystems >= 0 &&
                    budget.ParticleSystems <=
                    (boss ? 2 : 1),
                    $"Template '{template.Id}' particle budget is invalid.",
                    failures);
                Check(
                    budget.MaximumSimultaneousEnemies >= 0 &&
                    budget.MaximumSimultaneousEnemies <=
                    InfiniteSpireLoadedWindowBudgets.MaximumActiveEnemies,
                    $"Template '{template.Id}' enemy budget is invalid.",
                    failures);
                Check(
                    budget.SerializedWaves >= 0 && budget.SerializedWaves <= 2,
                    $"Template '{template.Id}' wave budget is invalid.",
                    failures);
                Check(
                    budget.Mechanisms == template.MechanismCount,
                    $"Template '{template.Id}' mechanism budget does not match metadata.",
                    failures);

                if (template.WaveSets != null)
                {
                    for (var setIndex = 0; setIndex < template.WaveSets.Count; setIndex++)
                    {
                        var set = template.WaveSets[setIndex];
                        Check(
                            set.Waves.Count <= budget.SerializedWaves,
                            $"Template '{template.Id}' wave set exceeds its serialized-wave budget.",
                            failures);
                        for (var waveIndex = 0; waveIndex < set.Waves.Count; waveIndex++)
                        {
                            Check(
                                set.Waves[waveIndex].EnemyIds.Count <=
                                budget.MaximumSimultaneousEnemies,
                                $"Template '{template.Id}' wave exceeds its active-enemy budget.",
                                failures);
                        }
                    }
                }
            }
        }

        private static void VerifyRuntimePurity(List<string> failures)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var paths = new[]
            {
                Path.Combine(
                    projectRoot,
                    "Assets",
                    "Scripts",
                    "TetherfallInfiniteSpireCatalog.cs"),
                Path.Combine(
                    projectRoot,
                    "Assets",
                    "Scripts",
                    "TetherfallInfiniteSpireGeneration.cs"),
                Path.Combine(
                    projectRoot,
                    "Assets",
                    "Scripts",
                    "TetherfallInfiniteSpireScaling.cs")
            };
            for (var index = 0; index < paths.Length; index++)
            {
                Check(File.Exists(paths[index]), $"Missing runtime file '{paths[index]}'.", failures);
                if (!File.Exists(paths[index]))
                {
                    continue;
                }

                var source = File.ReadAllText(paths[index]);
                Check(
                    source.IndexOf("GameObject", StringComparison.Ordinal) < 0,
                    $"Pure runtime file '{Path.GetFileName(paths[index])}' references GameObject.",
                    failures);
                Check(
                    source.IndexOf("using System.Linq", StringComparison.Ordinal) < 0,
                    $"Pure runtime file '{Path.GetFileName(paths[index])}' imports LINQ.",
                    failures);
                Check(
                    source.IndexOf('\u2014') < 0,
                    $"Runtime file '{Path.GetFileName(paths[index])}' contains an em dash.",
                    failures);
            }
        }

        private static void VerifyImmutableDefinitionType(
            Type type,
            List<string> failures)
        {
            Check(type.IsSealed, $"{type.Name} must remain sealed.", failures);
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            for (var index = 0; index < properties.Length; index++)
            {
                Check(
                    !properties[index].CanWrite,
                    $"{type.Name}.{properties[index].Name} must remain read-only.",
                    failures);
            }

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            Check(
                fields.Length == 0,
                $"{type.Name} exposes mutable public fields.",
                failures);
        }

        private static void CheckCategoryCount(
            int[] counts,
            InfiniteSpireRoomCategory category,
            int minimum,
            List<string> failures)
        {
            var count = counts[(int)category];
            Check(
                count >= minimum,
                $"Category {category} needs at least {minimum} templates, found {count}.",
                failures);
        }

        private static void RegisterTemplateId(
            HashSet<string> ids,
            string id,
            string owner,
            List<string> failures)
        {
            Check(
                !string.IsNullOrWhiteSpace(id),
                $"{owner} has a null, empty, or whitespace ID.",
                failures);
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            Check(
                id.StartsWith(
                    InfiniteSpireCatalog.TemplateIdPrefix,
                    StringComparison.Ordinal),
                $"{owner} ID '{id}' must use the 'spire.template.' namespace.",
                failures);
            Check(
                id == id.Trim(),
                $"{owner} ID '{id}' contains surrounding whitespace.",
                failures);
            Check(ids.Add(id), $"Stable ID '{id}' is duplicated.", failures);
        }

        private static void ValidateEnemyReference(
            Dictionary<string, EnemyDefinition> enemies,
            string enemyId,
            string owner,
            List<string> failures)
        {
            Check(
                !string.IsNullOrWhiteSpace(enemyId) && enemies.ContainsKey(enemyId),
                $"{owner} references missing enemy '{enemyId}'.",
                failures);
            Check(
                string.IsNullOrEmpty(enemyId) ||
                !enemyId.StartsWith("enemy.wild.", StringComparison.Ordinal),
                $"{owner} references forbidden wild Threadbind enemy '{enemyId}'.",
                failures);
        }

        private static void PushRecentTemplate(
            string[] recent,
            ref int count,
            string templateId)
        {
            var upper = Math.Min(count, recent.Length - 1);
            for (var index = upper; index > 0; index--)
            {
                recent[index] = recent[index - 1];
            }

            recent[0] = templateId;
            count = Math.Min(count + 1, recent.Length);
        }

        private static void PushRecentBoss(
            string[] recent,
            ref int count,
            string bossId)
        {
            var upper = Math.Min(count, recent.Length - 1);
            for (var index = upper; index > 0; index--)
            {
                recent[index] = recent[index - 1];
            }

            recent[0] = bossId;
            count = Math.Min(count + 1, recent.Length);
        }

        private static void CheckFiniteAndRange(
            float value,
            float minimum,
            float maximum,
            string label,
            List<string> failures)
        {
            Check(
                IsFinite(value) &&
                value >= minimum - 0.0001f &&
                value <= maximum + 0.0001f,
                $"{label} is outside [{minimum}, {maximum}]: {value}.",
                failures);
        }

        private static void CheckApproximately(
            float actual,
            float expected,
            string failure,
            List<string> failures)
        {
            Check(Math.Abs(actual - expected) <= 0.0001f, failure, failures);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static T GetPrivateStaticField<T>(Type owner, string fieldName)
        {
            var field = owner.GetField(
                fieldName,
                BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(owner.FullName, fieldName);
            }

            return (T)field.GetValue(null);
        }

        private static void Run(
            string section,
            List<string> failures,
            Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failures.Add(
                    $"{section} could not complete: {exception.GetType().Name}: " +
                    exception.Message);
            }
        }

        private static void Check(
            bool condition,
            string failure,
            List<string> failures)
        {
            assertionCount++;
            if (!condition && !failures.Contains(failure))
            {
                failures.Add(failure);
            }
        }
    }
}
