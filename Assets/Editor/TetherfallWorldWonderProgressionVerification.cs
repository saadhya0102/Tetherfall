#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallWorldWonderProgressionVerification
    {
        private const string MenuPath =
            "Tetherfall/Verify World Wonder Progression";
        private const string FlightSpeciesId =
            "species.wild.cinderwold.prismtail_kitelizard";

        private static readonly List<string> Failures = new List<string>();
        private static int assertionCount;

        [MenuItem(MenuPath, false, 2224)]
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

        [MenuItem(MenuPath, true, 2224)]
        private static bool CanVerify()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void VerifyInternal(bool logSuccess)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "World-wonder progression verification is edit-mode only.");
            }

            Failures.Clear();
            assertionCount = 0;
            VerifyCatalogAndSemanticIds();
            VerifyPrimaryOrderingAndAccess();
            VerifyPelagicBranches();
            VerifySequenceBreaks();
            VerifyDuplicateRewards();
            VerifyNormalization();
            VerifyOldAndDefaultSaves();
            VerifyCrossPathIndependence();
            VerifyDeterministicQuestObjectives();
            VerifyStaticSourceContract();
            Complete(logSuccess);
        }

        private static void VerifyCatalogAndSemanticIds()
        {
            Check(
                TetherfallWorldWonderCatalog.Paths.Count == 2,
                "World-wonder catalog must expose the tide-to-sky and Embercrown paths.");
            Check(
                TetherfallWorldWonderCatalog.Milestones.Count == 17,
                $"World-wonder catalog has {TetherfallWorldWonderCatalog.Milestones.Count} milestones; expected 17.");
            Check(
                TetherfallWorldWonderCatalog.Rewards.Count == 3,
                "World-wonder catalog must expose exactly three one-time rewards.");
            Check(
                TetherfallWorldWonderCatalog.Resolutions.Count == 2,
                "Pelagic Court must expose exactly one boss and one quest resolution.");

            var pathIds = new HashSet<string>(StringComparer.Ordinal);
            var questIds = new HashSet<string>(StringComparer.Ordinal);
            var expectedOrder = new Dictionary<string, int>(StringComparer.Ordinal);
            var pathCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < TetherfallWorldWonderCatalog.Paths.Count; index++)
            {
                var path = TetherfallWorldWonderCatalog.Paths[index];
                Check(path != null, $"World-wonder path {index} is null.");
                if (path == null)
                {
                    continue;
                }

                CheckStableId(path.Id, $"path {index}");
                CheckStableId(path.QuestId, $"path quest {index}");
                Check(pathIds.Add(path.Id), $"Duplicate path ID '{path.Id}'.");
                Check(questIds.Add(path.QuestId), $"Duplicate quest ID '{path.QuestId}'.");
                Check(
                    !string.IsNullOrWhiteSpace(path.Title),
                    $"Path '{path.Id}' has no title.");
                Check(
                    path.RequiredMilestones > 0,
                    $"Path '{path.Id}' has no required milestones.");
                expectedOrder[path.Id] = 0;
                pathCounts[path.Id] = 0;
            }

            var milestoneIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0;
                 index < TetherfallWorldWonderCatalog.Milestones.Count;
                 index++)
            {
                var milestone = TetherfallWorldWonderCatalog.Milestones[index];
                Check(milestone != null, $"World-wonder milestone {index} is null.");
                if (milestone == null)
                {
                    continue;
                }

                CheckStableId(milestone.Id, $"milestone {index}");
                Check(
                    milestoneIds.Add(milestone.Id),
                    $"Duplicate milestone ID '{milestone.Id}'.");
                Check(
                    pathIds.Contains(milestone.PathId),
                    $"Milestone '{milestone.Id}' references unknown path '{milestone.PathId}'.");
                Check(
                    questIds.Contains(milestone.QuestId),
                    $"Milestone '{milestone.Id}' references unknown quest '{milestone.QuestId}'.");
                Check(
                    expectedOrder.TryGetValue(milestone.PathId, out var nextOrder) &&
                    milestone.Order == nextOrder,
                    $"Milestone '{milestone.Id}' has order {milestone.Order}; expected {nextOrder}.");
                expectedOrder[milestone.PathId] = nextOrder + 1;
                pathCounts[milestone.PathId] = pathCounts[milestone.PathId] + 1;
                Check(
                    !string.IsNullOrWhiteSpace(milestone.Title) &&
                    !string.IsNullOrWhiteSpace(milestone.Objective),
                    $"Milestone '{milestone.Id}' lacks authored title or objective copy.");
                Check(
                    !string.IsNullOrWhiteSpace(milestone.CompletionNotification),
                    $"Milestone '{milestone.Id}' lacks completion notification copy.");
                if (!string.IsNullOrEmpty(milestone.SubjectId))
                {
                    CheckStableId(
                        milestone.SubjectId,
                        $"subject for '{milestone.Id}'");
                }

                for (var prerequisiteIndex = 0;
                     prerequisiteIndex < milestone.PrerequisiteMilestoneIds.Count;
                     prerequisiteIndex++)
                {
                    var prerequisiteId =
                        milestone.PrerequisiteMilestoneIds[prerequisiteIndex];
                    var prerequisite =
                        TetherfallWorldWonderCatalog.GetMilestone(prerequisiteId);
                    Check(
                        prerequisite != null &&
                        prerequisite.PathId == milestone.PathId &&
                        prerequisite.Order < milestone.Order,
                        $"Milestone '{milestone.Id}' has invalid prerequisite '{prerequisiteId}'.");
                }
            }

            for (var index = 0; index < TetherfallWorldWonderCatalog.Paths.Count; index++)
            {
                var path = TetherfallWorldWonderCatalog.Paths[index];
                Check(
                    pathCounts[path.Id] == path.RequiredMilestones,
                    $"Path '{path.Id}' requires {path.RequiredMilestones} milestones but catalogues {pathCounts[path.Id]}.");
            }

            var rewardIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < TetherfallWorldWonderCatalog.Rewards.Count; index++)
            {
                var reward = TetherfallWorldWonderCatalog.Rewards[index];
                CheckStableId(reward.Id, $"reward {index}");
                Check(
                    rewardIds.Add(reward.Id),
                    $"Duplicate reward ID '{reward.Id}'.");
                Check(
                    TetherfallWorldWonderCatalog.GetMilestone(
                        reward.PrerequisiteMilestoneId) != null &&
                    TetherfallWorldWonderCatalog.GetMilestone(
                        reward.GrantMilestoneId) != null,
                    $"Reward '{reward.Id}' has an invalid prerequisite or grant milestone.");
                if (!string.IsNullOrEmpty(reward.EquipMilestoneId))
                {
                    Check(
                        TetherfallWorldWonderCatalog.GetMilestone(
                            reward.EquipMilestoneId) != null,
                        $"Reward '{reward.Id}' has an invalid equip milestone.");
                }
            }

            var locationIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0;
                 index < TetherfallWorldWonderCatalog.Locations.Count;
                 index++)
            {
                var location = TetherfallWorldWonderCatalog.Locations[index];
                CheckStableId(location.Id, $"location {index}");
                Check(
                    locationIds.Add(location.Id),
                    $"Duplicate location ID '{location.Id}'.");
                Check(
                    TetherfallWorldWonderCatalog.GetMilestone(
                        location.AccessMilestoneId) != null,
                    $"Location '{location.Id}' has no valid access milestone.");
                if (!string.IsNullOrEmpty(location.EntryMilestoneId))
                {
                    Check(
                        TetherfallWorldWonderCatalog.GetMilestone(
                            location.EntryMilestoneId) != null,
                        $"Location '{location.Id}' has no valid entry milestone.");
                }
            }

            var resolutionIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0;
                 index < TetherfallWorldWonderCatalog.Resolutions.Count;
                 index++)
            {
                var resolution = TetherfallWorldWonderCatalog.Resolutions[index];
                CheckStableId(resolution.Id, $"resolution {index}");
                CheckStableId(
                    resolution.CompletionSubjectId,
                    $"resolution subject {index}");
                Check(
                    resolutionIds.Add(resolution.Id),
                    $"Duplicate resolution ID '{resolution.Id}'.");
                Check(
                    resolution.CompletionTrigger == WorldWonderTriggerKind.BossDefeated ||
                    resolution.CompletionTrigger == WorldWonderTriggerKind.QuestCompleted,
                    $"Resolution '{resolution.Id}' is not a boss or quest branch.");
            }

            Check(
                !TetherfallWorldWonderProgressionRules.IsStableSemanticId(
                    "milestone.test.120_45") &&
                !TetherfallWorldWonderProgressionRules.IsStableSemanticId(
                    "site@120:45"),
                "Coordinate-like or punctuation-encoded IDs were accepted as semantic IDs.");

            var existingQuestIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var quest in QuestCatalog.CreateAll())
            {
                if (quest != null)
                {
                    existingQuestIds.Add(quest.id);
                }
            }

            Check(
                !existingQuestIds.Contains(
                    TetherfallWorldWonderCatalog.TideToSkyQuestId) &&
                !existingQuestIds.Contains(
                    TetherfallWorldWonderCatalog.EmbercrownQuestId),
                "World-wonder quest IDs collide with the current QuestCatalog.");
        }

        private static void VerifyPrimaryOrderingAndAccess()
        {
            var progress = TetherfallWorldWonderProgressionRules.CreateDefault();
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordBossDefeated(
                    progress,
                    TetherfallWorldWonderCatalog.RiverGuardianBossId),
                WorldWonderProgressionStatus.Blocked,
                "River guardian before Tideways discovery");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryComplete(
                    progress,
                    TetherfallWorldWonderCatalog.EnterAeralithMilestoneId),
                WorldWonderProgressionStatus.Blocked,
                "Aeralith sequence break");
            Check(
                TetherfallWorldWonderProgressionRules.CountCompletedMilestones(
                    progress,
                    TetherfallWorldWonderCatalog.TideToSkyPathId) == 0,
                "Blocked primary-path events changed progress.");

            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordThreadwayDiscovered(
                    progress,
                    TetherfallWorldWonderCatalog.TidewaysThreadwayId),
                WorldWonderProgressionStatus.Applied,
                "Tideways discovery");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordThreadwayDiscovered(
                    progress,
                    TetherfallWorldWonderCatalog.TidewaysThreadwayId),
                WorldWonderProgressionStatus.AlreadyApplied,
                "Repeated Tideways discovery");
            Check(
                TetherfallWorldWonderProgressionRules.CanAccessLocation(
                    progress,
                    TetherfallWorldWonderCatalog.TidewaysLocationId) &&
                !TetherfallWorldWonderProgressionRules.CanAccessLocation(
                    progress,
                    TetherfallWorldWonderCatalog.PelagicCourtLocationId),
                "Tideways discovery produced incorrect derived location access.");

            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordBossDefeated(
                    progress,
                    TetherfallWorldWonderCatalog.RiverGuardianBossId),
                WorldWonderProgressionStatus.Applied,
                "River guardian defeat");
            Check(
                TetherfallWorldWonderProgressionRules.CanClaimReward(
                    progress,
                    TetherfallWorldWonderCatalog.InfiniteBreathingHelmetId),
                "River guardian defeat did not make the Breathhelm claimable.");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryGrantReward(
                    progress,
                    TetherfallWorldWonderCatalog.InfiniteBreathingHelmetId),
                WorldWonderProgressionStatus.Applied,
                "Breathhelm grant");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordEquipmentEquipped(
                    progress,
                    TetherfallWorldWonderCatalog.InfiniteBreathingHelmetId),
                WorldWonderProgressionStatus.Applied,
                "Breathhelm first equip");
            Check(
                TetherfallWorldWonderProgressionRules.HasInfiniteBreathing(progress),
                "Breathhelm first equip did not derive infinite breathing.");

            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryUnlockLocation(
                    progress,
                    TetherfallWorldWonderCatalog.PelagicCourtLocationId),
                WorldWonderProgressionStatus.Applied,
                "Pelagic Court unlock");
            Check(
                TetherfallWorldWonderProgressionRules.CanAccessLocation(
                    progress,
                    TetherfallWorldWonderCatalog.PelagicCourtLocationId),
                "Pelagic Court did not become accessible after its unlock.");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordLocationEntered(
                    progress,
                    TetherfallWorldWonderCatalog.PelagicCourtLocationId),
                WorldWonderProgressionStatus.Applied,
                "Pelagic Court entry");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TrySelectPelagicResolution(
                    progress,
                    TetherfallWorldWonderCatalog.RegentTrialResolutionId),
                WorldWonderProgressionStatus.Applied,
                "Pelagic boss-branch selection");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordQuestCompleted(
                    progress,
                    TetherfallWorldWonderCatalog.PelagicCompactQuestId),
                WorldWonderProgressionStatus.Conflict,
                "Unselected Pelagic quest branch");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordBossDefeated(
                    progress,
                    TetherfallWorldWonderCatalog.PelagicCourtBossId),
                WorldWonderProgressionStatus.Applied,
                "Nacre Regent branch resolution");

            Check(
                TetherfallWorldWonderProgressionRules.CanAccessLocation(
                    progress,
                    TetherfallWorldWonderCatalog.SkylanceSummitLocationId),
                "Pelagic resolution did not unlock the Skylance route.");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordSkylanceSummitReached(
                    progress,
                    TetherfallWorldWonderCatalog.SkylanceSummitLocationId),
                WorldWonderProgressionStatus.Applied,
                "Skylance summit");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordFlightPactBonded(
                    progress,
                    FlightSpeciesId,
                    false),
                WorldWonderProgressionStatus.Blocked,
                "Non-flying Pact bond");

            var flightSpecies = TetherfallAccordSpeciesCatalog.Find(FlightSpeciesId);
            Check(
                flightSpecies != null &&
                flightSpecies.movement != null &&
                flightSpecies.movement.usesFlyingMotor,
                $"Verifier flight species '{FlightSpeciesId}' is not catalogued with a flying motor.");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordFlightPactBonded(
                    progress,
                    FlightSpeciesId,
                    flightSpecies != null &&
                    flightSpecies.movement != null &&
                    flightSpecies.movement.usesFlyingMotor),
                WorldWonderProgressionStatus.Applied,
                "Flight Pact bond");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryUnlockMountedFlight(progress),
                WorldWonderProgressionStatus.Applied,
                "Mounted-flight clearance");
            Check(
                TetherfallWorldWonderProgressionRules.HasMountedFlightClearance(
                    progress) &&
                TetherfallWorldWonderProgressionRules.CanAccessLocation(
                    progress,
                    TetherfallWorldWonderCatalog.AeralithKingdomLocationId),
                "Mounted-flight clearance did not derive Aeralith access.");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordLocationEntered(
                    progress,
                    TetherfallWorldWonderCatalog.AeralithKingdomLocationId),
                WorldWonderProgressionStatus.Applied,
                "Aeralith entry");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryGrantReward(
                    progress,
                    TetherfallWorldWonderCatalog.AeralithRegionalWeaponId),
                WorldWonderProgressionStatus.Applied,
                "Aeralith regional weapon");
            Check(
                TetherfallWorldWonderProgressionRules.IsPathComplete(
                    progress,
                    TetherfallWorldWonderCatalog.TideToSkyPathId) &&
                TetherfallWorldWonderProgressionRules.CountCompletedMilestones(
                    progress,
                    TetherfallWorldWonderCatalog.TideToSkyPathId) == 12,
                "The ordered tide-to-sky chain did not complete at exactly 12 milestones.");
        }

        private static void VerifyPelagicBranches()
        {
            var questBranch = AdvancePrimaryToPelagicCourtEntry();
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordBossDefeated(
                    questBranch,
                    TetherfallWorldWonderCatalog.PelagicCourtBossId),
                WorldWonderProgressionStatus.Blocked,
                "Pelagic boss before branch selection");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TrySelectPelagicResolution(
                    questBranch,
                    TetherfallWorldWonderCatalog.ShoalCompactResolutionId),
                WorldWonderProgressionStatus.Applied,
                "Shoal Compact selection");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TrySelectPelagicResolution(
                    questBranch,
                    TetherfallWorldWonderCatalog.RegentTrialResolutionId),
                WorldWonderProgressionStatus.Conflict,
                "Pelagic branch reselection");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordBossDefeated(
                    questBranch,
                    TetherfallWorldWonderCatalog.PelagicCourtBossId),
                WorldWonderProgressionStatus.Conflict,
                "Boss completion on selected quest branch");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordQuestCompleted(
                    questBranch,
                    TetherfallWorldWonderCatalog.PelagicCompactQuestId),
                WorldWonderProgressionStatus.Applied,
                "Shoal Compact completion");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordQuestCompleted(
                    questBranch,
                    TetherfallWorldWonderCatalog.PelagicCompactQuestId),
                WorldWonderProgressionStatus.AlreadyApplied,
                "Repeated Shoal Compact completion");
            Check(
                TetherfallWorldWonderProgressionRules.IsMilestoneComplete(
                    questBranch,
                    TetherfallWorldWonderCatalog.ResolvePelagicCourtMilestoneId),
                "Quest resolution did not complete the shared Pelagic Court milestone.");
        }

        private static void VerifySequenceBreaks()
        {
            var malformed = new TetherfallWorldWonderProgressData
            {
                schemaVersion = -4,
                pelagicResolutionId =
                    TetherfallWorldWonderCatalog.RegentTrialResolutionId,
                flightPactSpeciesId = FlightSpeciesId,
                completedMilestoneIds = new List<string>
                {
                    TetherfallWorldWonderCatalog.EnterAeralithMilestoneId,
                    TetherfallWorldWonderCatalog.UnlockMountedFlightMilestoneId,
                    TetherfallWorldWonderCatalog.ResolvePelagicCourtMilestoneId,
                    TetherfallWorldWonderCatalog.EnterPelagicCourtMilestoneId,
                    TetherfallWorldWonderCatalog.EnterAeralithMilestoneId,
                    "milestone.test.120_45"
                },
                grantedRewardIds = new List<string>
                {
                    TetherfallWorldWonderCatalog.AeralithRegionalWeaponId,
                    TetherfallWorldWonderCatalog.AeralithRegionalWeaponId
                },
                firstEquippedEquipmentIds = new List<string>
                {
                    TetherfallWorldWonderCatalog.InfiniteBreathingHelmetId
                }
            };
            TetherfallWorldWonderProgressionRules.NormalizeInPlace(malformed);
            Check(
                malformed.completedMilestoneIds.Count == 0 &&
                malformed.grantedRewardIds.Count == 0 &&
                malformed.firstEquippedEquipmentIds.Count == 0,
                "Normalization retained out-of-sequence milestones or unsupported reward evidence.");
            Check(
                string.IsNullOrEmpty(malformed.pelagicResolutionId) &&
                string.IsNullOrEmpty(malformed.flightPactSpeciesId),
                "Normalization retained branch or Pact evidence after pruning its sequence.");

            var unsupportedEvidence = new TetherfallWorldWonderProgressData
            {
                completedMilestoneIds = new List<string>
                {
                    TetherfallWorldWonderCatalog.DiscoverTidewaysMilestoneId,
                    TetherfallWorldWonderCatalog.DefeatRiverGuardianMilestoneId,
                    TetherfallWorldWonderCatalog.ClaimBreathhelmMilestoneId,
                    TetherfallWorldWonderCatalog.EquipBreathhelmMilestoneId,
                    TetherfallWorldWonderCatalog.UnlockPelagicCourtMilestoneId,
                    TetherfallWorldWonderCatalog.EnterPelagicCourtMilestoneId
                },
                grantedRewardIds = null,
                firstEquippedEquipmentIds = null
            };
            TetherfallWorldWonderProgressionRules.NormalizeInPlace(unsupportedEvidence);
            Check(
                unsupportedEvidence.completedMilestoneIds.Count == 2 &&
                TetherfallWorldWonderProgressionRules.IsMilestoneComplete(
                    unsupportedEvidence,
                    TetherfallWorldWonderCatalog.DiscoverTidewaysMilestoneId) &&
                TetherfallWorldWonderProgressionRules.IsMilestoneComplete(
                    unsupportedEvidence,
                    TetherfallWorldWonderCatalog.DefeatRiverGuardianMilestoneId),
                "Missing reward/equip evidence did not prune the dependent sequence conservatively.");
        }

        private static void VerifyDuplicateRewards()
        {
            var progress = TetherfallWorldWonderProgressionRules.CreateDefault();
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordThreadwayDiscovered(
                    progress,
                    TetherfallWorldWonderCatalog.TidewaysThreadwayId),
                WorldWonderProgressionStatus.Applied,
                "Duplicate test Tideways discovery");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordBossDefeated(
                    progress,
                    TetherfallWorldWonderCatalog.RiverGuardianBossId),
                WorldWonderProgressionStatus.Applied,
                "Duplicate test guardian defeat");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryGrantReward(
                    progress,
                    TetherfallWorldWonderCatalog.InfiniteBreathingHelmetId),
                WorldWonderProgressionStatus.Applied,
                "First Breathhelm grant");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryGrantReward(
                    progress,
                    TetherfallWorldWonderCatalog.InfiniteBreathingHelmetId),
                WorldWonderProgressionStatus.AlreadyApplied,
                "Second Breathhelm grant");
            Check(
                CountId(
                    progress.grantedRewardIds,
                    TetherfallWorldWonderCatalog.InfiniteBreathingHelmetId) == 1,
                "Repeated Breathhelm grant created duplicate reward records.");

            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordEquipmentEquipped(
                    progress,
                    TetherfallWorldWonderCatalog.InfiniteBreathingHelmetId),
                WorldWonderProgressionStatus.Applied,
                "First Breathhelm equip");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordEquipmentEquipped(
                    progress,
                    TetherfallWorldWonderCatalog.InfiniteBreathingHelmetId),
                WorldWonderProgressionStatus.AlreadyApplied,
                "Second Breathhelm equip");
            Check(
                CountId(
                    progress.firstEquippedEquipmentIds,
                    TetherfallWorldWonderCatalog.InfiniteBreathingHelmetId) == 1 &&
                CountId(
                    progress.completedMilestoneIds,
                    TetherfallWorldWonderCatalog.EquipBreathhelmMilestoneId) == 1,
                "Repeated Breathhelm equip created duplicate state or progression.");
        }

        private static void VerifyNormalization()
        {
            var progress = new TetherfallWorldWonderProgressData
            {
                schemaVersion = -10,
                pelagicResolutionId = "  invalid.resolution  ",
                flightPactSpeciesId = " bad@species ",
                completedMilestoneIds = new List<string>
                {
                    " milestone.world_wonders.extension.future_gate ",
                    "milestone.world_wonders.extension.future_gate",
                    string.Empty,
                    null,
                    "bad@id"
                },
                grantedRewardIds = new List<string>
                {
                    " weapon.future.sky_relay ",
                    "weapon.future.sky_relay",
                    null
                },
                firstEquippedEquipmentIds = new List<string>
                {
                    " charm.future.deepward ",
                    "charm.future.deepward"
                }
            };
            TetherfallWorldWonderProgressionRules.NormalizeInPlace(progress);
            var firstFingerprint = Fingerprint(progress);
            TetherfallWorldWonderProgressionRules.NormalizeInPlace(progress);
            var secondFingerprint = Fingerprint(progress);

            Check(
                progress.schemaVersion ==
                TetherfallWorldWonderProgressData.CurrentSchemaVersion,
                "Normalization did not upgrade the DTO schema.");
            Check(
                progress.completedMilestoneIds.Count == 1 &&
                progress.completedMilestoneIds[0] ==
                "milestone.world_wonders.extension.future_gate",
                "Normalization did not dedupe valid forward-compatible milestone IDs.");
            Check(
                progress.grantedRewardIds.Count == 1 &&
                progress.firstEquippedEquipmentIds.Count == 1,
                "Normalization did not preserve and dedupe valid extension evidence.");
            Check(
                TetherfallWorldWonderProgressionRules
                    .GetGrantedRewardIdsForReconciliation(progress).Count == 0,
                "Forward-compatible unknown rewards leaked into current inventory reconciliation.");
            Check(
                string.IsNullOrEmpty(progress.pelagicResolutionId) &&
                string.IsNullOrEmpty(progress.flightPactSpeciesId),
                "Normalization retained invalid scalar IDs.");
            Check(
                string.Equals(
                    firstFingerprint,
                    secondFingerprint,
                    StringComparison.Ordinal),
                "World-wonder normalization is not idempotent.");
        }

        private static void VerifyOldAndDefaultSaves()
        {
            var fromNull = TetherfallWorldWonderProgressionRules.Normalize(null);
            Check(
                fromNull != null &&
                fromNull.schemaVersion ==
                TetherfallWorldWonderProgressData.CurrentSchemaVersion &&
                fromNull.completedMilestoneIds != null &&
                fromNull.grantedRewardIds != null &&
                fromNull.firstEquippedEquipmentIds != null,
                "A missing sidecar did not normalize to a safe default.");

            var old = new TetherfallWorldWonderProgressData
            {
                schemaVersion = 0,
                completedMilestoneIds = null,
                grantedRewardIds = null,
                firstEquippedEquipmentIds = null,
                pelagicResolutionId = null,
                flightPactSpeciesId = null
            };
            var restored = TetherfallWorldWonderProgressionRules.RestoreFromSave(old);
            Check(
                restored.schemaVersion ==
                TetherfallWorldWonderProgressData.CurrentSchemaVersion &&
                restored.completedMilestoneIds.Count == 0 &&
                restored.grantedRewardIds.Count == 0 &&
                restored.firstEquippedEquipmentIds.Count == 0 &&
                string.IsNullOrEmpty(restored.pelagicResolutionId) &&
                string.IsNullOrEmpty(restored.flightPactSpeciesId),
                "Schema-zero/default fields did not restore to a safe fresh state.");
            Check(
                old.schemaVersion == 0 &&
                old.completedMilestoneIds == null,
                "RestoreFromSave mutated its source DTO.");

            var runtime = AdvancePrimaryToPelagicCourtEntry();
            var snapshot =
                TetherfallWorldWonderProgressionRules.CaptureForSave(runtime);
            snapshot.completedMilestoneIds.Clear();
            Check(
                runtime.completedMilestoneIds.Count == 6,
                "CaptureForSave leaked a mutable list back into runtime progress.");
        }

        private static void VerifyCrossPathIndependence()
        {
            var emberOnly = CompleteEmbercrownPath();
            Check(
                TetherfallWorldWonderProgressionRules.IsPathComplete(
                    emberOnly,
                    TetherfallWorldWonderCatalog.EmbercrownPathId) &&
                TetherfallWorldWonderProgressionRules.CountCompletedMilestones(
                    emberOnly,
                    TetherfallWorldWonderCatalog.TideToSkyPathId) == 0 &&
                !TetherfallWorldWonderProgressionRules.HasInfiniteBreathing(
                    emberOnly) &&
                !TetherfallWorldWonderProgressionRules.CanAccessLocation(
                    emberOnly,
                    TetherfallWorldWonderCatalog.TidewaysLocationId),
                "Independent Embercrown completion changed tide-to-sky state.");

            var tideOnly = AdvancePrimaryToPelagicCourtEntry();
            Check(
                TetherfallWorldWonderProgressionRules.CountCompletedMilestones(
                    tideOnly,
                    TetherfallWorldWonderCatalog.EmbercrownPathId) == 0 &&
                !TetherfallWorldWonderProgressionRules.CanAccessLocation(
                    tideOnly,
                    TetherfallWorldWonderCatalog.EmbercrownCalderaLocationId) &&
                !TetherfallWorldWonderProgressionRules.IsRewardGranted(
                    tideOnly,
                    TetherfallWorldWonderCatalog.EmbercrownRegionalWeaponId),
                "Tide-to-sky progress changed independent Embercrown state.");
        }

        private static void VerifyDeterministicQuestObjectives()
        {
            var fresh = TetherfallWorldWonderProgressionRules.CreateQuestSnapshots(null);
            Check(
                fresh.Count == 2 &&
                fresh[0].PathId ==
                TetherfallWorldWonderCatalog.TideToSkyPathId &&
                fresh[0].ObjectiveId ==
                TetherfallWorldWonderCatalog.DiscoverTidewaysMilestoneId &&
                fresh[1].PathId ==
                TetherfallWorldWonderCatalog.EmbercrownPathId &&
                fresh[1].ObjectiveId ==
                TetherfallWorldWonderCatalog.DiscoverEmbercrownMilestoneId,
                "Fresh quest objectives are not in deterministic authored path order.");

            var applied = TetherfallWorldWonderProgressionRules.CreateDefault();
            TetherfallWorldWonderProgressionRules.TryRecordThreadwayDiscovered(
                applied,
                TetherfallWorldWonderCatalog.TidewaysThreadwayId);
            TetherfallWorldWonderProgressionRules.TryRecordBossDefeated(
                applied,
                TetherfallWorldWonderCatalog.RiverGuardianBossId);

            var unordered = new TetherfallWorldWonderProgressData
            {
                completedMilestoneIds = new List<string>
                {
                    TetherfallWorldWonderCatalog.DefeatRiverGuardianMilestoneId,
                    TetherfallWorldWonderCatalog.DiscoverTidewaysMilestoneId,
                    TetherfallWorldWonderCatalog.DiscoverTidewaysMilestoneId
                }
            };
            var appliedObjectives =
                TetherfallWorldWonderProgressionRules.CreateQuestSnapshots(applied);
            var unorderedObjectives =
                TetherfallWorldWonderProgressionRules.CreateQuestSnapshots(unordered);
            Check(
                string.Equals(
                    ObjectiveFingerprint(appliedObjectives),
                    ObjectiveFingerprint(unorderedObjectives),
                    StringComparison.Ordinal),
                "Equivalent progress produced order-dependent quest objectives.");

            var branch = AdvancePrimaryToPelagicCourtEntry();
            var beforeChoice =
                TetherfallWorldWonderProgressionRules.GetQuestSnapshot(
                    branch,
                    TetherfallWorldWonderCatalog.TideToSkyPathId);
            Check(
                beforeChoice != null &&
                beforeChoice.ObjectiveId ==
                TetherfallWorldWonderCatalog.ChoosePelagicResolutionObjectiveId,
                "Pelagic Court did not expose a deterministic branch-choice objective.");
            TetherfallWorldWonderProgressionRules.TrySelectPelagicResolution(
                branch,
                TetherfallWorldWonderCatalog.ShoalCompactResolutionId);
            var selected =
                TetherfallWorldWonderProgressionRules.GetQuestSnapshot(
                    branch,
                    TetherfallWorldWonderCatalog.TideToSkyPathId);
            var selectedAgain =
                TetherfallWorldWonderProgressionRules.GetQuestSnapshot(
                    branch,
                    TetherfallWorldWonderCatalog.TideToSkyPathId);
            Check(
                selected != null &&
                selected.ObjectiveId ==
                TetherfallWorldWonderCatalog.ShoalCompactResolutionId &&
                string.Equals(
                    selected.Objective,
                    selectedAgain?.Objective,
                    StringComparison.Ordinal),
                "Selected Pelagic quest branch did not produce a stable objective.");
            TetherfallWorldWonderProgressionRules.TryRecordQuestCompleted(
                branch,
                TetherfallWorldWonderCatalog.PelagicCompactQuestId);
            var afterResolution =
                TetherfallWorldWonderProgressionRules.GetQuestSnapshot(
                    branch,
                    TetherfallWorldWonderCatalog.TideToSkyPathId);
            Check(
                afterResolution != null &&
                afterResolution.ObjectiveId ==
                TetherfallWorldWonderCatalog.ClimbSkylanceMilestoneId,
                "Pelagic resolution did not deterministically advance guidance to Skylance.");
        }

        private static void VerifyStaticSourceContract()
        {
            var path = Path.Combine(
                Application.dataPath,
                "Scripts",
                "TetherfallWorldWonderProgression.cs");
            Check(File.Exists(path), $"Missing progression source at '{path}'.");
            if (!File.Exists(path))
            {
                return;
            }

            var source = File.ReadAllText(path).Replace("\r\n", "\n");
            Check(
                !source.Contains("using UnityEngine") &&
                !source.Contains("TetherfallGame") &&
                !source.Contains("Vector3") &&
                !source.Contains("Mathf."),
                "Progression foundation is not a Unity-independent pure-rules module.");
            Check(
                !source.Contains("public sealed partial class TetherfallSaveData") &&
                !source.Contains("worldWonderProgression = new"),
                "Progression foundation was added to the main save schema prematurely.");
            Check(
                source.Contains("CaptureForSave(") &&
                source.Contains("RestoreFromSave(") &&
                source.Contains("TryRecordThreadwayDiscovered(") &&
                source.Contains("TryRecordBossDefeated(") &&
                source.Contains("TryGrantReward(") &&
                source.Contains("TryRecordEquipmentEquipped(") &&
                source.Contains("TryRecordFlightPactBonded(") &&
                source.Contains("TryUnlockMountedFlight(") &&
                source.Contains("CreateQuestSnapshots("),
                "Progression source is missing one or more required integration APIs.");
            Check(
                source.Contains("StringComparer.Ordinal") &&
                source.Contains("IsStableSemanticId("),
                "Progression source does not consistently expose ordinal semantic-ID handling.");
        }

        private static TetherfallWorldWonderProgressData
            AdvancePrimaryToPelagicCourtEntry()
        {
            var progress = TetherfallWorldWonderProgressionRules.CreateDefault();
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordThreadwayDiscovered(
                    progress,
                    TetherfallWorldWonderCatalog.TidewaysThreadwayId),
                WorldWonderProgressionStatus.Applied,
                "Helper Tideways discovery");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordBossDefeated(
                    progress,
                    TetherfallWorldWonderCatalog.RiverGuardianBossId),
                WorldWonderProgressionStatus.Applied,
                "Helper river guardian defeat");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryGrantReward(
                    progress,
                    TetherfallWorldWonderCatalog.InfiniteBreathingHelmetId),
                WorldWonderProgressionStatus.Applied,
                "Helper Breathhelm grant");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordEquipmentEquipped(
                    progress,
                    TetherfallWorldWonderCatalog.InfiniteBreathingHelmetId),
                WorldWonderProgressionStatus.Applied,
                "Helper Breathhelm equip");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryUnlockLocation(
                    progress,
                    TetherfallWorldWonderCatalog.PelagicCourtLocationId),
                WorldWonderProgressionStatus.Applied,
                "Helper Pelagic unlock");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordLocationEntered(
                    progress,
                    TetherfallWorldWonderCatalog.PelagicCourtLocationId),
                WorldWonderProgressionStatus.Applied,
                "Helper Pelagic entry");
            return progress;
        }

        private static TetherfallWorldWonderProgressData CompleteEmbercrownPath()
        {
            var progress = TetherfallWorldWonderProgressionRules.CreateDefault();
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordThreadwayDiscovered(
                    progress,
                    TetherfallWorldWonderCatalog.EmbercrownThreadwayId),
                WorldWonderProgressionStatus.Applied,
                "Embercrown discovery");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordLocationEntered(
                    progress,
                    TetherfallWorldWonderCatalog.EmbercrownCalderaLocationId),
                WorldWonderProgressionStatus.Applied,
                "Embercrown entry");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordQuestCompleted(
                    progress,
                    TetherfallWorldWonderCatalog.EmberwardQuestId),
                WorldWonderProgressionStatus.Applied,
                "Emberward forge quest");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryRecordBossDefeated(
                    progress,
                    TetherfallWorldWonderCatalog.EmbercrownBossId),
                WorldWonderProgressionStatus.Applied,
                "Embercrown boss");
            ExpectStatus(
                TetherfallWorldWonderProgressionRules.TryGrantReward(
                    progress,
                    TetherfallWorldWonderCatalog.EmbercrownRegionalWeaponId),
                WorldWonderProgressionStatus.Applied,
                "Embercrown reward");
            return progress;
        }

        private static void CheckStableId(string id, string label)
        {
            Check(
                TetherfallWorldWonderProgressionRules.IsStableSemanticId(id),
                $"World-wonder {label} has invalid or coordinate-like ID '{id}'.");
        }

        private static void ExpectStatus(
            WorldWonderProgressionResult result,
            WorldWonderProgressionStatus expected,
            string label)
        {
            Check(
                result != null && result.Status == expected,
                $"{label} returned {result?.Status.ToString() ?? "null"}; expected {expected}.");
            if (expected == WorldWonderProgressionStatus.Applied)
            {
                Check(
                    result != null &&
                    result.Changed &&
                    result.ShouldRequestSave &&
                    result.ShouldNotify &&
                    !string.IsNullOrWhiteSpace(result.NotificationText),
                    $"{label} did not expose save/notification hooks for its applied transition.");
            }
            else
            {
                Check(
                    result != null &&
                    !result.Changed &&
                    !result.ShouldRequestSave &&
                    !result.ShouldNotify,
                    $"{label} exposed side effects for a non-applied transition.");
            }
        }

        private static int CountId(IReadOnlyList<string> ids, string expected)
        {
            var count = 0;
            if (ids == null)
            {
                return count;
            }

            for (var index = 0; index < ids.Count; index++)
            {
                if (string.Equals(ids[index], expected, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static string Fingerprint(TetherfallWorldWonderProgressData progress)
        {
            return string.Join(
                "|",
                progress.schemaVersion.ToString(),
                string.Join(",", progress.completedMilestoneIds),
                string.Join(",", progress.grantedRewardIds),
                string.Join(",", progress.firstEquippedEquipmentIds),
                progress.pelagicResolutionId,
                progress.flightPactSpeciesId);
        }

        private static string ObjectiveFingerprint(
            IReadOnlyList<WorldWonderQuestSnapshot> snapshots)
        {
            var builder = new StringBuilder();
            for (var index = 0; index < snapshots.Count; index++)
            {
                var snapshot = snapshots[index];
                builder.Append(snapshot.PathId);
                builder.Append('|');
                builder.Append(snapshot.QuestId);
                builder.Append('|');
                builder.Append(snapshot.ObjectiveId);
                builder.Append('|');
                builder.Append(snapshot.Objective);
                builder.Append('|');
                builder.Append(snapshot.Current);
                builder.Append('/');
                builder.Append(snapshot.Required);
                builder.Append(';');
            }

            return builder.ToString();
        }

        private static void Check(bool condition, string message)
        {
            assertionCount++;
            if (!condition && !Failures.Contains(message))
            {
                Failures.Add(message);
            }
        }

        private static void Complete(bool logSuccess)
        {
            if (Failures.Count == 0)
            {
                if (logSuccess)
                {
                    Debug.Log(
                        $"Tetherfall world-wonder progression verification passed: {assertionCount} assertions cover " +
                        "semantic IDs, ordered tide-to-sky and independent Embercrown paths, Pelagic branches, " +
                        "one-time rewards, normalization, old saves, access checks, and deterministic objectives.");
                }

                return;
            }

            var message = new StringBuilder();
            message.Append("World-wonder progression verification failed with ");
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
#endif
