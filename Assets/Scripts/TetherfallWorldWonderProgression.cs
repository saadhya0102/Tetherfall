using System;
using System.Collections.Generic;

namespace Tetherfall
{
    public enum WorldWonderTriggerKind
    {
        ThreadwayDiscovered,
        BossDefeated,
        RewardGranted,
        EquipmentEquipped,
        LocationUnlocked,
        LocationEntered,
        PelagicResolutionCompleted,
        MountainSummitReached,
        FlightPactBonded,
        MountedFlightUnlocked,
        QuestCompleted
    }

    public enum WorldWonderProgressionStatus
    {
        Applied,
        AlreadyApplied,
        Blocked,
        Conflict,
        Invalid
    }

    /// <summary>
    /// Serializable sidecar for authored world-wonder progress.
    /// This type is intentionally not referenced by TetherfallSaveData yet.
    /// Use CaptureForSave and RestoreFromSave when the main save schema adopts it.
    /// </summary>
    [Serializable]
    public sealed class TetherfallWorldWonderProgressData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public List<string> completedMilestoneIds = new List<string>();
        public List<string> grantedRewardIds = new List<string>();
        public List<string> firstEquippedEquipmentIds = new List<string>();
        public string pelagicResolutionId = string.Empty;
        public string flightPactSpeciesId = string.Empty;

        public TetherfallWorldWonderProgressData Clone()
        {
            var clone = new TetherfallWorldWonderProgressData
            {
                schemaVersion = schemaVersion,
                pelagicResolutionId = pelagicResolutionId,
                flightPactSpeciesId = flightPactSpeciesId
            };
            if (completedMilestoneIds != null)
            {
                clone.completedMilestoneIds.AddRange(completedMilestoneIds);
            }

            if (grantedRewardIds != null)
            {
                clone.grantedRewardIds.AddRange(grantedRewardIds);
            }

            if (firstEquippedEquipmentIds != null)
            {
                clone.firstEquippedEquipmentIds.AddRange(firstEquippedEquipmentIds);
            }

            return clone;
        }
    }

    public sealed class WorldWonderPathDefinition
    {
        public WorldWonderPathDefinition(
            string id,
            string questId,
            string title,
            int requiredMilestones)
        {
            Id = id;
            QuestId = questId;
            Title = title;
            RequiredMilestones = requiredMilestones;
        }

        public string Id { get; }
        public string QuestId { get; }
        public string Title { get; }
        public int RequiredMilestones { get; }
    }

    public sealed class WorldWonderMilestoneDefinition
    {
        public WorldWonderMilestoneDefinition(
            string id,
            string pathId,
            string questId,
            string title,
            string objective,
            int order,
            WorldWonderTriggerKind triggerKind,
            string subjectId,
            string completionNotification,
            params string[] prerequisiteMilestoneIds)
        {
            Id = id;
            PathId = pathId;
            QuestId = questId;
            Title = title;
            Objective = objective;
            Order = order;
            TriggerKind = triggerKind;
            SubjectId = subjectId ?? string.Empty;
            CompletionNotification = completionNotification ?? string.Empty;
            PrerequisiteMilestoneIds =
                prerequisiteMilestoneIds ?? Array.Empty<string>();
        }

        public string Id { get; }
        public string PathId { get; }
        public string QuestId { get; }
        public string Title { get; }
        public string Objective { get; }
        public int Order { get; }
        public WorldWonderTriggerKind TriggerKind { get; }
        public string SubjectId { get; }
        public string CompletionNotification { get; }
        public IReadOnlyList<string> PrerequisiteMilestoneIds { get; }
    }

    public sealed class WorldWonderRewardDefinition
    {
        public WorldWonderRewardDefinition(
            string id,
            string displayName,
            string pathId,
            string prerequisiteMilestoneId,
            string grantMilestoneId,
            string equipMilestoneId)
        {
            Id = id;
            DisplayName = displayName;
            PathId = pathId;
            PrerequisiteMilestoneId = prerequisiteMilestoneId;
            GrantMilestoneId = grantMilestoneId;
            EquipMilestoneId = equipMilestoneId ?? string.Empty;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string PathId { get; }
        public string PrerequisiteMilestoneId { get; }
        public string GrantMilestoneId { get; }
        public string EquipMilestoneId { get; }
    }

    public sealed class WorldWonderLocationDefinition
    {
        public WorldWonderLocationDefinition(
            string id,
            string displayName,
            string accessMilestoneId,
            string entryMilestoneId)
        {
            Id = id;
            DisplayName = displayName;
            AccessMilestoneId = accessMilestoneId;
            EntryMilestoneId = entryMilestoneId ?? string.Empty;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string AccessMilestoneId { get; }
        public string EntryMilestoneId { get; }
    }

    public sealed class WorldWonderResolutionDefinition
    {
        public WorldWonderResolutionDefinition(
            string id,
            string title,
            string objective,
            WorldWonderTriggerKind completionTrigger,
            string completionSubjectId)
        {
            Id = id;
            Title = title;
            Objective = objective;
            CompletionTrigger = completionTrigger;
            CompletionSubjectId = completionSubjectId;
        }

        public string Id { get; }
        public string Title { get; }
        public string Objective { get; }
        public WorldWonderTriggerKind CompletionTrigger { get; }
        public string CompletionSubjectId { get; }
    }

    public sealed class WorldWonderQuestSnapshot
    {
        public WorldWonderQuestSnapshot(
            string pathId,
            string questId,
            string title,
            string objectiveId,
            string objective,
            int current,
            int required,
            bool completed)
        {
            PathId = pathId;
            QuestId = questId;
            Title = title;
            ObjectiveId = objectiveId;
            Objective = objective;
            Current = current;
            Required = required;
            Completed = completed;
        }

        public string PathId { get; }
        public string QuestId { get; }
        public string Title { get; }
        public string ObjectiveId { get; }
        public string Objective { get; }
        public int Current { get; }
        public int Required { get; }
        public bool Completed { get; }
    }

    public sealed class WorldWonderProgressionResult
    {
        internal WorldWonderProgressionResult(
            WorldWonderProgressionStatus status,
            string milestoneId,
            string subjectId,
            string rewardId,
            string notificationText,
            string detail)
        {
            Status = status;
            MilestoneId = milestoneId ?? string.Empty;
            SubjectId = subjectId ?? string.Empty;
            RewardId = rewardId ?? string.Empty;
            NotificationText = notificationText ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public WorldWonderProgressionStatus Status { get; }
        public string MilestoneId { get; }
        public string SubjectId { get; }
        public string RewardId { get; }
        public string NotificationText { get; }
        public string Detail { get; }
        public bool Changed => Status == WorldWonderProgressionStatus.Applied;
        public bool ShouldRequestSave => Changed;
        public bool ShouldNotify => Changed && !string.IsNullOrEmpty(NotificationText);
    }

    public static class TetherfallWorldWonderCatalog
    {
        public const string TideToSkyPathId = "path.world_wonders.tide_to_sky";
        public const string EmbercrownPathId = "path.world_wonders.embercrown";

        public const string TideToSkyQuestId = "quest.world_wonders.tide_to_sky";
        public const string EmbercrownQuestId = "quest.world_wonders.embercrown";
        public const string PelagicCompactQuestId = "quest.pelagic_court.shoal_compact";
        public const string EmberwardQuestId = "quest.embercrown.three_forges";

        public const string TidewaysThreadwayId = "threadway.tideways.confluence";
        public const string EmbercrownThreadwayId = "threadway.embercrown.foothold";

        public const string TidewaysLocationId = "location.tideways.confluence";
        public const string PelagicCourtLocationId = "location.pelagic_court.threshold";
        public const string SkylanceSummitLocationId = "location.skylance.summit";
        public const string AeralithKingdomLocationId = "location.aeralith.sky_gate";
        public const string EmbercrownCalderaLocationId = "location.embercrown.caldera";

        public const string RiverGuardianBossId = "boss.tideways.orrun_river_guardian";
        public const string PelagicCourtBossId = "boss.pelagic_court.nacre_regent";
        public const string EmbercrownBossId = "boss.embercrown.pyreheart_matriarch";

        public const string InfiniteBreathingHelmetId = "charm.tideways.breathhelm";
        public const string AeralithRegionalWeaponId = "weapon.aeralith.stormlance";
        public const string EmbercrownRegionalWeaponId = "weapon.embercrown.cinderwake";
        public const string MountedFlightClearanceId = "clearance.mounted_flight.aeralith";

        public const string RegentTrialResolutionId =
            "resolution.pelagic_court.regent_trial";
        public const string ShoalCompactResolutionId =
            "resolution.pelagic_court.shoal_compact";
        public const string ChoosePelagicResolutionObjectiveId =
            "objective.pelagic_court.choose_resolution";

        public const string DiscoverTidewaysMilestoneId =
            "milestone.world_wonders.tide_to_sky.discover_tideways";
        public const string DefeatRiverGuardianMilestoneId =
            "milestone.world_wonders.tide_to_sky.defeat_river_guardian";
        public const string ClaimBreathhelmMilestoneId =
            "milestone.world_wonders.tide_to_sky.claim_breathhelm";
        public const string EquipBreathhelmMilestoneId =
            "milestone.world_wonders.tide_to_sky.equip_breathhelm";
        public const string UnlockPelagicCourtMilestoneId =
            "milestone.world_wonders.tide_to_sky.unlock_pelagic_court";
        public const string EnterPelagicCourtMilestoneId =
            "milestone.world_wonders.tide_to_sky.enter_pelagic_court";
        public const string ResolvePelagicCourtMilestoneId =
            "milestone.world_wonders.tide_to_sky.resolve_pelagic_court";
        public const string ClimbSkylanceMilestoneId =
            "milestone.world_wonders.tide_to_sky.climb_skylance";
        public const string BondFlightPactMilestoneId =
            "milestone.world_wonders.tide_to_sky.bond_flight_pact";
        public const string UnlockMountedFlightMilestoneId =
            "milestone.world_wonders.tide_to_sky.unlock_mounted_flight";
        public const string EnterAeralithMilestoneId =
            "milestone.world_wonders.tide_to_sky.enter_aeralith";
        public const string ClaimAeralithRewardMilestoneId =
            "milestone.world_wonders.tide_to_sky.claim_aeralith_reward";

        public const string DiscoverEmbercrownMilestoneId =
            "milestone.world_wonders.embercrown.discover_foothold";
        public const string EnterEmbercrownMilestoneId =
            "milestone.world_wonders.embercrown.enter_caldera";
        public const string RestoreEmberwardForgesMilestoneId =
            "milestone.world_wonders.embercrown.restore_three_forges";
        public const string DefeatEmbercrownBossMilestoneId =
            "milestone.world_wonders.embercrown.defeat_pyreheart";
        public const string ClaimEmbercrownRewardMilestoneId =
            "milestone.world_wonders.embercrown.claim_cinderwake";

        private static readonly WorldWonderPathDefinition[] PathDefinitions =
        {
            new WorldWonderPathDefinition(
                TideToSkyPathId,
                TideToSkyQuestId,
                "From the Tideways to Aeralith",
                12),
            new WorldWonderPathDefinition(
                EmbercrownPathId,
                EmbercrownQuestId,
                "The Crown Beneath the Ember",
                5)
        };

        private static readonly WorldWonderMilestoneDefinition[] MilestoneDefinitions =
        {
            Milestone(
                DiscoverTidewaysMilestoneId,
                TideToSkyPathId,
                TideToSkyQuestId,
                "Where Rivers Remember",
                "Chart the Tideways Confluence Threadway.",
                0,
                WorldWonderTriggerKind.ThreadwayDiscovered,
                TidewaysThreadwayId,
                "Tideways charted. Orrun's river bells answer upstream."),
            Milestone(
                DefeatRiverGuardianMilestoneId,
                TideToSkyPathId,
                TideToSkyQuestId,
                "The River's Named Guardian",
                "Defeat Orrun, River Guardian, at the Nine-Reach weir.",
                1,
                WorldWonderTriggerKind.BossDefeated,
                RiverGuardianBossId,
                "Orrun yields the Breathhelm seal without breaking the river's course.",
                DiscoverTidewaysMilestoneId),
            Milestone(
                ClaimBreathhelmMilestoneId,
                TideToSkyPathId,
                TideToSkyQuestId,
                "Breath Without Measure",
                "Claim the Tideglass Breathhelm from Orrun's opened reliquary.",
                2,
                WorldWonderTriggerKind.RewardGranted,
                InfiniteBreathingHelmetId,
                "Tideglass Breathhelm added to the field pack.",
                DefeatRiverGuardianMilestoneId),
            Milestone(
                EquipBreathhelmMilestoneId,
                TideToSkyPathId,
                TideToSkyQuestId,
                "Wear the Deep Current",
                "Equip the Tideglass Breathhelm to establish infinite-breathing clearance.",
                3,
                WorldWonderTriggerKind.EquipmentEquipped,
                InfiniteBreathingHelmetId,
                "The Breathhelm settles into a closed current. Underwater breathing is unlimited.",
                ClaimBreathhelmMilestoneId),
            Milestone(
                UnlockPelagicCourtMilestoneId,
                TideToSkyPathId,
                TideToSkyQuestId,
                "The Drowned Gate Answers",
                "Open the Pelagic Court threshold while the Breathhelm is equipped.",
                4,
                WorldWonderTriggerKind.LocationUnlocked,
                PelagicCourtLocationId,
                "The Pelagic Court recognizes the Breathhelm seal.",
                EquipBreathhelmMilestoneId),
            Milestone(
                EnterPelagicCourtMilestoneId,
                TideToSkyPathId,
                TideToSkyQuestId,
                "Below the Last Sunbeam",
                "Enter the underwater Pelagic Court.",
                5,
                WorldWonderTriggerKind.LocationEntered,
                PelagicCourtLocationId,
                "The Pelagic Court opens around an unbroken column of water.",
                UnlockPelagicCourtMilestoneId),
            Milestone(
                ResolvePelagicCourtMilestoneId,
                TideToSkyPathId,
                TideToSkyQuestId,
                "Two Judgments of the Court",
                "Choose and resolve the Nacre Regent trial or the Shoal Compact.",
                6,
                WorldWonderTriggerKind.PelagicResolutionCompleted,
                string.Empty,
                "The Pelagic Court records its judgment and releases the Skylance route.",
                EnterPelagicCourtMilestoneId),
            Milestone(
                ClimbSkylanceMilestoneId,
                TideToSkyPathId,
                TideToSkyQuestId,
                "Above the Rain's Beginning",
                "Climb the Skylance Mountains and reach the wind-carved summit.",
                7,
                WorldWonderTriggerKind.MountainSummitReached,
                SkylanceSummitLocationId,
                "Skylance summit reached. Aeralith's lower currents are visible.",
                ResolvePelagicCourtMilestoneId),
            Milestone(
                BondFlightPactMilestoneId,
                TideToSkyPathId,
                TideToSkyQuestId,
                "A Willing Pair of Wings",
                "Form a voluntary Threadbind Accord with a flight-capable Pact.",
                8,
                WorldWonderTriggerKind.FlightPactBonded,
                string.Empty,
                "A flight-capable Pact answers the Skylance route.",
                ClimbSkylanceMilestoneId),
            Milestone(
                UnlockMountedFlightMilestoneId,
                TideToSkyPathId,
                TideToSkyQuestId,
                "The Sky-Rider's Clearance",
                "Complete the Skylance flight check and unlock mounted-flight clearance.",
                9,
                WorldWonderTriggerKind.MountedFlightUnlocked,
                MountedFlightClearanceId,
                "Mounted-flight clearance granted for the Aeralith approach.",
                BondFlightPactMilestoneId),
            Milestone(
                EnterAeralithMilestoneId,
                TideToSkyPathId,
                TideToSkyQuestId,
                "A Kingdom Held Aloft",
                "Use mounted flight to enter Aeralith, the airborne kingdom.",
                10,
                WorldWonderTriggerKind.LocationEntered,
                AeralithKingdomLocationId,
                "Aeralith admits the expedition through its lower sky gate.",
                UnlockMountedFlightMilestoneId),
            Milestone(
                ClaimAeralithRewardMilestoneId,
                TideToSkyPathId,
                TideToSkyQuestId,
                "Stormlance of Aeralith",
                "Claim Aeralith's regional Stormlance.",
                11,
                WorldWonderTriggerKind.RewardGranted,
                AeralithRegionalWeaponId,
                "Aeralith Stormlance added to the field pack.",
                EnterAeralithMilestoneId),

            Milestone(
                DiscoverEmbercrownMilestoneId,
                EmbercrownPathId,
                EmbercrownQuestId,
                "Smoke Above the Old Crown",
                "Chart the Embercrown Foothold Threadway.",
                0,
                WorldWonderTriggerKind.ThreadwayDiscovered,
                EmbercrownThreadwayId,
                "Embercrown Foothold charted. Three forge-signals remain dark."),
            Milestone(
                EnterEmbercrownMilestoneId,
                EmbercrownPathId,
                EmbercrownQuestId,
                "Into the Caldera",
                "Enter the Embercrown caldera through the cooled basalt throat.",
                1,
                WorldWonderTriggerKind.LocationEntered,
                EmbercrownCalderaLocationId,
                "The Embercrown caldera opens beyond the basalt throat.",
                DiscoverEmbercrownMilestoneId),
            Milestone(
                RestoreEmberwardForgesMilestoneId,
                EmbercrownPathId,
                EmbercrownQuestId,
                "Three Forges, One Warning",
                "Restore the three Emberward forges around the caldera.",
                2,
                WorldWonderTriggerKind.QuestCompleted,
                EmberwardQuestId,
                "The Emberward forges ring together and expose the crown chamber.",
                EnterEmbercrownMilestoneId),
            Milestone(
                DefeatEmbercrownBossMilestoneId,
                EmbercrownPathId,
                EmbercrownQuestId,
                "The Pyreheart Matriarch",
                "Defeat Vauldris, the Pyreheart Matriarch, without quenching the heartforge.",
                3,
                WorldWonderTriggerKind.BossDefeated,
                EmbercrownBossId,
                "Vauldris yields. The heartforge steadies instead of collapsing.",
                RestoreEmberwardForgesMilestoneId),
            Milestone(
                ClaimEmbercrownRewardMilestoneId,
                EmbercrownPathId,
                EmbercrownQuestId,
                "Cinderwake",
                "Claim Cinderwake from the stabilized Embercrown heartforge.",
                4,
                WorldWonderTriggerKind.RewardGranted,
                EmbercrownRegionalWeaponId,
                "Cinderwake added to the field pack.",
                DefeatEmbercrownBossMilestoneId)
        };

        private static readonly WorldWonderRewardDefinition[] RewardDefinitions =
        {
            new WorldWonderRewardDefinition(
                InfiniteBreathingHelmetId,
                "Tideglass Breathhelm",
                TideToSkyPathId,
                DefeatRiverGuardianMilestoneId,
                ClaimBreathhelmMilestoneId,
                EquipBreathhelmMilestoneId),
            new WorldWonderRewardDefinition(
                AeralithRegionalWeaponId,
                "Aeralith Stormlance",
                TideToSkyPathId,
                EnterAeralithMilestoneId,
                ClaimAeralithRewardMilestoneId,
                string.Empty),
            new WorldWonderRewardDefinition(
                EmbercrownRegionalWeaponId,
                "Cinderwake",
                EmbercrownPathId,
                DefeatEmbercrownBossMilestoneId,
                ClaimEmbercrownRewardMilestoneId,
                string.Empty)
        };

        private static readonly WorldWonderLocationDefinition[] LocationDefinitions =
        {
            new WorldWonderLocationDefinition(
                TidewaysLocationId,
                "Tideways Confluence",
                DiscoverTidewaysMilestoneId,
                string.Empty),
            new WorldWonderLocationDefinition(
                PelagicCourtLocationId,
                "Pelagic Court",
                UnlockPelagicCourtMilestoneId,
                EnterPelagicCourtMilestoneId),
            new WorldWonderLocationDefinition(
                SkylanceSummitLocationId,
                "Skylance Summit",
                ResolvePelagicCourtMilestoneId,
                ClimbSkylanceMilestoneId),
            new WorldWonderLocationDefinition(
                AeralithKingdomLocationId,
                "Aeralith",
                UnlockMountedFlightMilestoneId,
                EnterAeralithMilestoneId),
            new WorldWonderLocationDefinition(
                EmbercrownCalderaLocationId,
                "Embercrown Caldera",
                DiscoverEmbercrownMilestoneId,
                EnterEmbercrownMilestoneId)
        };

        private static readonly WorldWonderResolutionDefinition[] ResolutionDefinitions =
        {
            new WorldWonderResolutionDefinition(
                RegentTrialResolutionId,
                "Trial of the Nacre Regent",
                "Defeat the Nacre Regent in the Court's current-bound trial.",
                WorldWonderTriggerKind.BossDefeated,
                PelagicCourtBossId),
            new WorldWonderResolutionDefinition(
                ShoalCompactResolutionId,
                "The Shoal Compact",
                "Complete the Shoal Compact and return its living testimony to the Court.",
                WorldWonderTriggerKind.QuestCompleted,
                PelagicCompactQuestId)
        };

        public static IReadOnlyList<WorldWonderPathDefinition> Paths => PathDefinitions;
        public static IReadOnlyList<WorldWonderMilestoneDefinition> Milestones =>
            MilestoneDefinitions;
        public static IReadOnlyList<WorldWonderRewardDefinition> Rewards => RewardDefinitions;
        public static IReadOnlyList<WorldWonderLocationDefinition> Locations =>
            LocationDefinitions;
        public static IReadOnlyList<WorldWonderResolutionDefinition> Resolutions =>
            ResolutionDefinitions;

        public static WorldWonderPathDefinition GetPath(string id)
        {
            for (var index = 0; index < PathDefinitions.Length; index++)
            {
                if (string.Equals(PathDefinitions[index].Id, id, StringComparison.Ordinal))
                {
                    return PathDefinitions[index];
                }
            }

            return null;
        }

        public static WorldWonderMilestoneDefinition GetMilestone(string id)
        {
            for (var index = 0; index < MilestoneDefinitions.Length; index++)
            {
                if (string.Equals(MilestoneDefinitions[index].Id, id, StringComparison.Ordinal))
                {
                    return MilestoneDefinitions[index];
                }
            }

            return null;
        }

        public static WorldWonderRewardDefinition GetReward(string id)
        {
            for (var index = 0; index < RewardDefinitions.Length; index++)
            {
                if (string.Equals(RewardDefinitions[index].Id, id, StringComparison.Ordinal))
                {
                    return RewardDefinitions[index];
                }
            }

            return null;
        }

        public static WorldWonderLocationDefinition GetLocation(string id)
        {
            for (var index = 0; index < LocationDefinitions.Length; index++)
            {
                if (string.Equals(LocationDefinitions[index].Id, id, StringComparison.Ordinal))
                {
                    return LocationDefinitions[index];
                }
            }

            return null;
        }

        public static WorldWonderResolutionDefinition GetResolution(string id)
        {
            for (var index = 0; index < ResolutionDefinitions.Length; index++)
            {
                if (string.Equals(ResolutionDefinitions[index].Id, id, StringComparison.Ordinal))
                {
                    return ResolutionDefinitions[index];
                }
            }

            return null;
        }

        internal static WorldWonderMilestoneDefinition GetMilestoneForSignal(
            WorldWonderTriggerKind triggerKind,
            string subjectId)
        {
            for (var index = 0; index < MilestoneDefinitions.Length; index++)
            {
                var definition = MilestoneDefinitions[index];
                if (definition.TriggerKind == triggerKind &&
                    string.Equals(definition.SubjectId, subjectId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        internal static WorldWonderResolutionDefinition GetResolutionForSignal(
            WorldWonderTriggerKind triggerKind,
            string subjectId)
        {
            for (var index = 0; index < ResolutionDefinitions.Length; index++)
            {
                var definition = ResolutionDefinitions[index];
                if (definition.CompletionTrigger == triggerKind &&
                    string.Equals(
                        definition.CompletionSubjectId,
                        subjectId,
                        StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        private static WorldWonderMilestoneDefinition Milestone(
            string id,
            string pathId,
            string questId,
            string title,
            string objective,
            int order,
            WorldWonderTriggerKind triggerKind,
            string subjectId,
            string completionNotification,
            params string[] prerequisiteMilestoneIds)
        {
            return new WorldWonderMilestoneDefinition(
                id,
                pathId,
                questId,
                title,
                objective,
                order,
                triggerKind,
                subjectId,
                completionNotification,
                prerequisiteMilestoneIds);
        }
    }

    /// <summary>
    /// Pure, deterministic rules for world-wonder state. Methods mutate only the supplied DTO;
    /// callers own inventory grants, world changes, notifications, and save requests.
    /// </summary>
    public static class TetherfallWorldWonderProgressionRules
    {
        private const int MaximumStableIdLength = 160;

        public static TetherfallWorldWonderProgressData CreateDefault()
        {
            return new TetherfallWorldWonderProgressData();
        }

        public static TetherfallWorldWonderProgressData Normalize(
            TetherfallWorldWonderProgressData progress)
        {
            progress ??= CreateDefault();
            NormalizeInPlace(progress);
            return progress;
        }

        public static TetherfallWorldWonderProgressData CaptureForSave(
            TetherfallWorldWonderProgressData runtimeProgress)
        {
            var snapshot = runtimeProgress != null
                ? runtimeProgress.Clone()
                : CreateDefault();
            NormalizeInPlace(snapshot);
            return snapshot;
        }

        public static TetherfallWorldWonderProgressData RestoreFromSave(
            TetherfallWorldWonderProgressData savedProgress)
        {
            var restored = savedProgress != null
                ? savedProgress.Clone()
                : CreateDefault();
            NormalizeInPlace(restored);
            return restored;
        }

        public static void NormalizeInPlace(TetherfallWorldWonderProgressData progress)
        {
            if (progress == null)
            {
                return;
            }

            progress.completedMilestoneIds = NormalizeIdList(progress.completedMilestoneIds);
            progress.grantedRewardIds = NormalizeIdList(progress.grantedRewardIds);
            progress.firstEquippedEquipmentIds =
                NormalizeIdList(progress.firstEquippedEquipmentIds);
            progress.pelagicResolutionId = NormalizeScalarId(progress.pelagicResolutionId);
            progress.flightPactSpeciesId = NormalizeScalarId(progress.flightPactSpeciesId);

            if (TetherfallWorldWonderCatalog.GetResolution(progress.pelagicResolutionId) == null)
            {
                progress.pelagicResolutionId = string.Empty;
            }

            if (!progress.flightPactSpeciesId.StartsWith(
                    "species.",
                    StringComparison.Ordinal))
            {
                progress.flightPactSpeciesId = string.Empty;
            }

            var requestedMilestones = new HashSet<string>(
                progress.completedMilestoneIds,
                StringComparer.Ordinal);
            var requestedRewards = new HashSet<string>(
                progress.grantedRewardIds,
                StringComparer.Ordinal);
            var requestedEquips = new HashSet<string>(
                progress.firstEquippedEquipmentIds,
                StringComparer.Ordinal);
            var retainedMilestones = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0;
                 index < TetherfallWorldWonderCatalog.Milestones.Count;
                 index++)
            {
                var definition = TetherfallWorldWonderCatalog.Milestones[index];
                if (!requestedMilestones.Contains(definition.Id) ||
                    !PrerequisitesComplete(definition, retainedMilestones) ||
                    !HasRequiredEvidence(
                        definition,
                        progress,
                        requestedRewards,
                        requestedEquips))
                {
                    continue;
                }

                retainedMilestones.Add(definition.Id);
            }

            var normalizedMilestones = new List<string>();
            for (var index = 0; index < progress.completedMilestoneIds.Count; index++)
            {
                var id = progress.completedMilestoneIds[index];
                if (TetherfallWorldWonderCatalog.GetMilestone(id) == null)
                {
                    normalizedMilestones.Add(id);
                }
            }

            normalizedMilestones.AddRange(retainedMilestones);
            normalizedMilestones.Sort(StringComparer.Ordinal);
            progress.completedMilestoneIds = normalizedMilestones;

            progress.grantedRewardIds = RetainSupportedEvidence(
                progress.grantedRewardIds,
                retainedMilestones,
                false);
            progress.firstEquippedEquipmentIds = RetainSupportedEvidence(
                progress.firstEquippedEquipmentIds,
                retainedMilestones,
                true);

            if (!retainedMilestones.Contains(
                    TetherfallWorldWonderCatalog.EnterPelagicCourtMilestoneId))
            {
                progress.pelagicResolutionId = string.Empty;
            }

            if (!retainedMilestones.Contains(
                    TetherfallWorldWonderCatalog.BondFlightPactMilestoneId))
            {
                progress.flightPactSpeciesId = string.Empty;
            }

            progress.schemaVersion =
                TetherfallWorldWonderProgressData.CurrentSchemaVersion;
        }

        public static WorldWonderProgressionResult TryComplete(
            TetherfallWorldWonderProgressData progress,
            string milestoneId)
        {
            if (progress == null)
            {
                return Invalid(milestoneId, "Progress is null.");
            }

            NormalizeInPlace(progress);
            var definition = TetherfallWorldWonderCatalog.GetMilestone(milestoneId);
            if (definition == null)
            {
                return Invalid(milestoneId, "Milestone ID is not catalogued.");
            }

            switch (definition.TriggerKind)
            {
                case WorldWonderTriggerKind.RewardGranted:
                    return TryGrantReward(progress, definition.SubjectId);
                case WorldWonderTriggerKind.EquipmentEquipped:
                    return TryRecordEquipmentEquipped(progress, definition.SubjectId);
                case WorldWonderTriggerKind.PelagicResolutionCompleted:
                    return Blocked(
                        definition,
                        "Resolve the selected Pelagic Court boss or quest branch.");
                case WorldWonderTriggerKind.FlightPactBonded:
                    return Blocked(
                        definition,
                        "Use TryRecordFlightPactBonded with verified movement capability.");
                default:
                    return CompleteCore(progress, definition, definition.SubjectId);
            }
        }

        public static WorldWonderProgressionResult TryRecordThreadwayDiscovered(
            TetherfallWorldWonderProgressData progress,
            string threadwayId)
        {
            return TryApplyCataloguedSignal(
                progress,
                WorldWonderTriggerKind.ThreadwayDiscovered,
                threadwayId);
        }

        public static WorldWonderProgressionResult TryRecordBossDefeated(
            TetherfallWorldWonderProgressData progress,
            string bossId)
        {
            return TryRecordBranchOrCataloguedSignal(
                progress,
                WorldWonderTriggerKind.BossDefeated,
                bossId);
        }

        public static WorldWonderProgressionResult TryRecordQuestCompleted(
            TetherfallWorldWonderProgressData progress,
            string questId)
        {
            return TryRecordBranchOrCataloguedSignal(
                progress,
                WorldWonderTriggerKind.QuestCompleted,
                questId);
        }

        public static WorldWonderProgressionResult TryGrantReward(
            TetherfallWorldWonderProgressData progress,
            string rewardId)
        {
            if (progress == null)
            {
                return Invalid(rewardId, "Progress is null.");
            }

            NormalizeInPlace(progress);
            var reward = TetherfallWorldWonderCatalog.GetReward(rewardId);
            if (reward == null)
            {
                return Invalid(rewardId, "Reward ID is not catalogued.");
            }

            if (ContainsOrdinal(progress.grantedRewardIds, reward.Id) &&
                IsMilestoneComplete(progress, reward.GrantMilestoneId))
            {
                return AlreadyApplied(reward.GrantMilestoneId, reward.Id, reward.Id);
            }

            if (!IsMilestoneComplete(progress, reward.PrerequisiteMilestoneId))
            {
                return Blocked(
                    TetherfallWorldWonderCatalog.GetMilestone(reward.GrantMilestoneId),
                    $"Required milestone '{reward.PrerequisiteMilestoneId}' is incomplete.");
            }

            AddUnique(progress.grantedRewardIds, reward.Id);
            var definition =
                TetherfallWorldWonderCatalog.GetMilestone(reward.GrantMilestoneId);
            var result = CompleteCore(progress, definition, reward.Id, reward.Id);
            if (!result.Changed)
            {
                progress.grantedRewardIds.Remove(reward.Id);
            }

            return result;
        }

        public static WorldWonderProgressionResult TryRecordEquipmentEquipped(
            TetherfallWorldWonderProgressData progress,
            string equipmentId)
        {
            if (progress == null)
            {
                return Invalid(equipmentId, "Progress is null.");
            }

            NormalizeInPlace(progress);
            var reward = TetherfallWorldWonderCatalog.GetReward(equipmentId);
            if (reward == null || string.IsNullOrEmpty(reward.EquipMilestoneId))
            {
                return Invalid(
                    equipmentId,
                    "Equipment is not a progression-tracked first equip.");
            }

            if (ContainsOrdinal(progress.firstEquippedEquipmentIds, equipmentId) &&
                IsMilestoneComplete(progress, reward.EquipMilestoneId))
            {
                return AlreadyApplied(
                    reward.EquipMilestoneId,
                    equipmentId,
                    equipmentId);
            }

            if (!IsRewardGranted(progress, equipmentId))
            {
                return Blocked(
                    TetherfallWorldWonderCatalog.GetMilestone(reward.EquipMilestoneId),
                    "The equipment reward has not been granted.");
            }

            AddUnique(progress.firstEquippedEquipmentIds, equipmentId);
            var definition =
                TetherfallWorldWonderCatalog.GetMilestone(reward.EquipMilestoneId);
            var result = CompleteCore(progress, definition, equipmentId, equipmentId);
            if (!result.Changed)
            {
                progress.firstEquippedEquipmentIds.Remove(equipmentId);
            }

            return result;
        }

        public static WorldWonderProgressionResult TryUnlockLocation(
            TetherfallWorldWonderProgressData progress,
            string locationId)
        {
            return TryApplyCataloguedSignal(
                progress,
                WorldWonderTriggerKind.LocationUnlocked,
                locationId);
        }

        public static WorldWonderProgressionResult TryRecordLocationEntered(
            TetherfallWorldWonderProgressData progress,
            string locationId)
        {
            if (progress == null)
            {
                return Invalid(locationId, "Progress is null.");
            }

            NormalizeInPlace(progress);
            var location = TetherfallWorldWonderCatalog.GetLocation(locationId);
            if (location == null || string.IsNullOrEmpty(location.EntryMilestoneId))
            {
                return Invalid(locationId, "Location has no tracked entry milestone.");
            }

            if (!CanAccessLocation(progress, locationId))
            {
                return Blocked(
                    TetherfallWorldWonderCatalog.GetMilestone(location.EntryMilestoneId),
                    $"Access milestone '{location.AccessMilestoneId}' is incomplete.");
            }

            var definition =
                TetherfallWorldWonderCatalog.GetMilestone(location.EntryMilestoneId);
            return CompleteCore(progress, definition, locationId);
        }

        public static WorldWonderProgressionResult TrySelectPelagicResolution(
            TetherfallWorldWonderProgressData progress,
            string resolutionId)
        {
            if (progress == null)
            {
                return Invalid(resolutionId, "Progress is null.");
            }

            NormalizeInPlace(progress);
            var resolution = TetherfallWorldWonderCatalog.GetResolution(resolutionId);
            if (resolution == null)
            {
                return Invalid(resolutionId, "Pelagic Court resolution is not catalogued.");
            }

            var milestone = TetherfallWorldWonderCatalog.GetMilestone(
                TetherfallWorldWonderCatalog.ResolvePelagicCourtMilestoneId);
            if (!IsMilestoneComplete(
                    progress,
                    TetherfallWorldWonderCatalog.EnterPelagicCourtMilestoneId))
            {
                return Blocked(milestone, "The Pelagic Court has not been entered.");
            }

            if (string.Equals(
                    progress.pelagicResolutionId,
                    resolution.Id,
                    StringComparison.Ordinal))
            {
                return AlreadyApplied(milestone.Id, resolution.Id, string.Empty);
            }

            if (!string.IsNullOrEmpty(progress.pelagicResolutionId))
            {
                return new WorldWonderProgressionResult(
                    WorldWonderProgressionStatus.Conflict,
                    milestone.Id,
                    resolution.Id,
                    string.Empty,
                    string.Empty,
                    $"Resolution '{progress.pelagicResolutionId}' is already selected.");
            }

            progress.pelagicResolutionId = resolution.Id;
            return new WorldWonderProgressionResult(
                WorldWonderProgressionStatus.Applied,
                milestone.Id,
                resolution.Id,
                string.Empty,
                $"Pelagic Court path chosen: {resolution.Title}.",
                string.Empty);
        }

        public static WorldWonderProgressionResult TryRecordSkylanceSummitReached(
            TetherfallWorldWonderProgressData progress,
            string locationId)
        {
            return TryApplyCataloguedSignal(
                progress,
                WorldWonderTriggerKind.MountainSummitReached,
                locationId);
        }

        public static WorldWonderProgressionResult TryRecordFlightPactBonded(
            TetherfallWorldWonderProgressData progress,
            string speciesId,
            bool usesFlyingMotor)
        {
            if (progress == null)
            {
                return Invalid(speciesId, "Progress is null.");
            }

            NormalizeInPlace(progress);
            var definition = TetherfallWorldWonderCatalog.GetMilestone(
                TetherfallWorldWonderCatalog.BondFlightPactMilestoneId);
            var normalizedSpeciesId = NormalizeScalarId(speciesId);
            if (!usesFlyingMotor ||
                !normalizedSpeciesId.StartsWith("species.", StringComparison.Ordinal))
            {
                return Blocked(
                    definition,
                    "The bonded Pact does not expose a catalogued flying movement profile.");
            }

            if (IsMilestoneComplete(progress, definition.Id))
            {
                return AlreadyApplied(definition.Id, progress.flightPactSpeciesId, string.Empty);
            }

            if (!PrerequisitesComplete(definition, progress.completedMilestoneIds))
            {
                return Blocked(definition, "Skylance summit has not been reached.");
            }

            progress.flightPactSpeciesId = normalizedSpeciesId;
            var result = CompleteCore(progress, definition, normalizedSpeciesId);
            if (!result.Changed)
            {
                progress.flightPactSpeciesId = string.Empty;
            }

            return result;
        }

        public static WorldWonderProgressionResult TryUnlockMountedFlight(
            TetherfallWorldWonderProgressData progress)
        {
            if (progress == null)
            {
                return Invalid(
                    TetherfallWorldWonderCatalog.MountedFlightClearanceId,
                    "Progress is null.");
            }

            NormalizeInPlace(progress);
            var definition = TetherfallWorldWonderCatalog.GetMilestone(
                TetherfallWorldWonderCatalog.UnlockMountedFlightMilestoneId);
            return CompleteCore(
                progress,
                definition,
                TetherfallWorldWonderCatalog.MountedFlightClearanceId);
        }

        public static bool IsMilestoneComplete(
            TetherfallWorldWonderProgressData progress,
            string milestoneId)
        {
            return progress != null &&
                   ContainsOrdinal(progress.completedMilestoneIds, milestoneId);
        }

        public static bool IsRewardGranted(
            TetherfallWorldWonderProgressData progress,
            string rewardId)
        {
            return progress != null &&
                   ContainsOrdinal(progress.grantedRewardIds, rewardId);
        }

        public static bool WasEquipmentFirstEquipped(
            TetherfallWorldWonderProgressData progress,
            string equipmentId)
        {
            return progress != null &&
                   ContainsOrdinal(progress.firstEquippedEquipmentIds, equipmentId);
        }

        public static bool HasInfiniteBreathing(
            TetherfallWorldWonderProgressData progress)
        {
            return IsMilestoneComplete(
                       progress,
                       TetherfallWorldWonderCatalog.EquipBreathhelmMilestoneId) &&
                   WasEquipmentFirstEquipped(
                       progress,
                       TetherfallWorldWonderCatalog.InfiniteBreathingHelmetId);
        }

        public static bool HasMountedFlightClearance(
            TetherfallWorldWonderProgressData progress)
        {
            return IsMilestoneComplete(
                progress,
                TetherfallWorldWonderCatalog.UnlockMountedFlightMilestoneId);
        }

        public static bool CanAccessLocation(
            TetherfallWorldWonderProgressData progress,
            string locationId)
        {
            var location = TetherfallWorldWonderCatalog.GetLocation(locationId);
            return location != null &&
                   IsMilestoneComplete(progress, location.AccessMilestoneId);
        }

        public static bool CanClaimReward(
            TetherfallWorldWonderProgressData progress,
            string rewardId)
        {
            var reward = TetherfallWorldWonderCatalog.GetReward(rewardId);
            return reward != null &&
                   !IsRewardGranted(progress, rewardId) &&
                   IsMilestoneComplete(progress, reward.PrerequisiteMilestoneId);
        }

        public static bool IsPathComplete(
            TetherfallWorldWonderProgressData progress,
            string pathId)
        {
            var path = TetherfallWorldWonderCatalog.GetPath(pathId);
            return path != null &&
                   CountCompletedMilestones(progress, pathId) >=
                   path.RequiredMilestones;
        }

        public static int CountCompletedMilestones(
            TetherfallWorldWonderProgressData progress,
            string pathId)
        {
            if (progress == null)
            {
                return 0;
            }

            var count = 0;
            for (var index = 0;
                 index < TetherfallWorldWonderCatalog.Milestones.Count;
                 index++)
            {
                var definition = TetherfallWorldWonderCatalog.Milestones[index];
                if (string.Equals(definition.PathId, pathId, StringComparison.Ordinal) &&
                    IsMilestoneComplete(progress, definition.Id))
                {
                    count++;
                }
            }

            return count;
        }

        public static IReadOnlyList<WorldWonderQuestSnapshot> CreateQuestSnapshots(
            TetherfallWorldWonderProgressData progress)
        {
            var normalized = RestoreFromSave(progress);
            var snapshots = new List<WorldWonderQuestSnapshot>(
                TetherfallWorldWonderCatalog.Paths.Count);
            for (var pathIndex = 0;
                 pathIndex < TetherfallWorldWonderCatalog.Paths.Count;
                 pathIndex++)
            {
                var path = TetherfallWorldWonderCatalog.Paths[pathIndex];
                var current = CountCompletedMilestones(normalized, path.Id);
                var completed = current >= path.RequiredMilestones;
                var objectiveId = string.Empty;
                var objective = completed
                    ? "All authored wonders on this route are resolved."
                    : string.Empty;

                if (!completed)
                {
                    var next = GetNextIncompleteMilestone(normalized, path.Id);
                    if (next != null &&
                        string.Equals(
                            next.Id,
                            TetherfallWorldWonderCatalog.ResolvePelagicCourtMilestoneId,
                            StringComparison.Ordinal))
                    {
                        var resolution = TetherfallWorldWonderCatalog.GetResolution(
                            normalized.pelagicResolutionId);
                        if (resolution == null)
                        {
                            objectiveId =
                                TetherfallWorldWonderCatalog
                                    .ChoosePelagicResolutionObjectiveId;
                            objective =
                                "Choose the Nacre Regent trial or the Shoal Compact in the Pelagic Court.";
                        }
                        else
                        {
                            objectiveId = resolution.Id;
                            objective = resolution.Objective;
                        }
                    }
                    else if (next != null)
                    {
                        objectiveId = next.Id;
                        objective = next.Objective;
                    }
                }

                snapshots.Add(new WorldWonderQuestSnapshot(
                    path.Id,
                    path.QuestId,
                    path.Title,
                    objectiveId,
                    objective,
                    current,
                    path.RequiredMilestones,
                    completed));
            }

            return snapshots;
        }

        public static WorldWonderQuestSnapshot GetQuestSnapshot(
            TetherfallWorldWonderProgressData progress,
            string pathId)
        {
            var snapshots = CreateQuestSnapshots(progress);
            for (var index = 0; index < snapshots.Count; index++)
            {
                if (string.Equals(
                        snapshots[index].PathId,
                        pathId,
                        StringComparison.Ordinal))
                {
                    return snapshots[index];
                }
            }

            return null;
        }

        public static IReadOnlyList<string> GetGrantedRewardIdsForReconciliation(
            TetherfallWorldWonderProgressData progress)
        {
            var normalized = RestoreFromSave(progress);
            var catalogued = new List<string>();
            for (var index = 0; index < normalized.grantedRewardIds.Count; index++)
            {
                var rewardId = normalized.grantedRewardIds[index];
                if (TetherfallWorldWonderCatalog.GetReward(rewardId) != null)
                {
                    catalogued.Add(rewardId);
                }
            }

            return catalogued.ToArray();
        }

        public static bool IsStableSemanticId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            value = value.Trim();
            if (value.Length > MaximumStableIdLength ||
                value[0] < 'a' ||
                value[0] > 'z' ||
                value[value.Length - 1] == '.' ||
                value.Contains(".."))
            {
                return false;
            }

            var segmentStart = 0;
            for (var index = 0; index <= value.Length; index++)
            {
                if (index < value.Length)
                {
                    var character = value[index];
                    var valid =
                        character >= 'a' && character <= 'z' ||
                        character >= '0' && character <= '9' ||
                        character == '_' ||
                        character == '.';
                    if (!valid)
                    {
                        return false;
                    }

                    if (character != '.')
                    {
                        continue;
                    }
                }

                if (index == segmentStart ||
                    IsCoordinateLikeSegment(
                        value.Substring(segmentStart, index - segmentStart)))
                {
                    return false;
                }

                segmentStart = index + 1;
            }

            return true;
        }

        private static WorldWonderProgressionResult TryApplyCataloguedSignal(
            TetherfallWorldWonderProgressData progress,
            WorldWonderTriggerKind triggerKind,
            string subjectId)
        {
            if (progress == null)
            {
                return Invalid(subjectId, "Progress is null.");
            }

            NormalizeInPlace(progress);
            var normalizedSubjectId = NormalizeScalarId(subjectId);
            var definition = TetherfallWorldWonderCatalog.GetMilestoneForSignal(
                triggerKind,
                normalizedSubjectId);
            if (definition == null)
            {
                return Invalid(subjectId, "Signal subject is not catalogued for this trigger.");
            }

            return CompleteCore(progress, definition, normalizedSubjectId);
        }

        private static WorldWonderProgressionResult TryRecordBranchOrCataloguedSignal(
            TetherfallWorldWonderProgressData progress,
            WorldWonderTriggerKind triggerKind,
            string subjectId)
        {
            if (progress == null)
            {
                return Invalid(subjectId, "Progress is null.");
            }

            NormalizeInPlace(progress);
            var normalizedSubjectId = NormalizeScalarId(subjectId);
            var branchSignal = TetherfallWorldWonderCatalog.GetResolutionForSignal(
                triggerKind,
                normalizedSubjectId);
            if (branchSignal != null)
            {
                var milestone = TetherfallWorldWonderCatalog.GetMilestone(
                    TetherfallWorldWonderCatalog.ResolvePelagicCourtMilestoneId);
                if (string.IsNullOrEmpty(progress.pelagicResolutionId))
                {
                    return Blocked(
                        milestone,
                        "Choose a Pelagic Court resolution before completing its branch.");
                }

                if (!string.Equals(
                        progress.pelagicResolutionId,
                        branchSignal.Id,
                        StringComparison.Ordinal))
                {
                    return new WorldWonderProgressionResult(
                        WorldWonderProgressionStatus.Conflict,
                        milestone.Id,
                        normalizedSubjectId,
                        string.Empty,
                        string.Empty,
                        $"Signal belongs to unselected resolution '{branchSignal.Id}'.");
                }

                return CompleteCore(
                    progress,
                    milestone,
                    normalizedSubjectId);
            }

            var definition = TetherfallWorldWonderCatalog.GetMilestoneForSignal(
                triggerKind,
                normalizedSubjectId);
            if (definition == null)
            {
                return Invalid(subjectId, "Signal subject is not catalogued for this trigger.");
            }

            return CompleteCore(progress, definition, normalizedSubjectId);
        }

        private static WorldWonderProgressionResult CompleteCore(
            TetherfallWorldWonderProgressData progress,
            WorldWonderMilestoneDefinition definition,
            string subjectId,
            string rewardId = "")
        {
            if (definition == null)
            {
                return Invalid(subjectId, "Milestone definition is missing.");
            }

            if (IsMilestoneComplete(progress, definition.Id))
            {
                return AlreadyApplied(definition.Id, subjectId, rewardId);
            }

            if (!PrerequisitesComplete(definition, progress.completedMilestoneIds))
            {
                return Blocked(
                    definition,
                    "One or more prerequisite milestones are incomplete.");
            }

            AddUnique(progress.completedMilestoneIds, definition.Id);
            return new WorldWonderProgressionResult(
                WorldWonderProgressionStatus.Applied,
                definition.Id,
                subjectId,
                rewardId,
                definition.CompletionNotification,
                string.Empty);
        }

        private static WorldWonderProgressionResult Invalid(
            string subjectId,
            string detail)
        {
            return new WorldWonderProgressionResult(
                WorldWonderProgressionStatus.Invalid,
                string.Empty,
                subjectId,
                string.Empty,
                string.Empty,
                detail);
        }

        private static WorldWonderProgressionResult Blocked(
            WorldWonderMilestoneDefinition definition,
            string detail)
        {
            return new WorldWonderProgressionResult(
                WorldWonderProgressionStatus.Blocked,
                definition != null ? definition.Id : string.Empty,
                definition != null ? definition.SubjectId : string.Empty,
                string.Empty,
                string.Empty,
                detail);
        }

        private static WorldWonderProgressionResult AlreadyApplied(
            string milestoneId,
            string subjectId,
            string rewardId)
        {
            return new WorldWonderProgressionResult(
                WorldWonderProgressionStatus.AlreadyApplied,
                milestoneId,
                subjectId,
                rewardId,
                string.Empty,
                string.Empty);
        }

        private static bool HasRequiredEvidence(
            WorldWonderMilestoneDefinition definition,
            TetherfallWorldWonderProgressData progress,
            HashSet<string> requestedRewards,
            HashSet<string> requestedEquips)
        {
            switch (definition.TriggerKind)
            {
                case WorldWonderTriggerKind.RewardGranted:
                    return requestedRewards.Contains(definition.SubjectId);
                case WorldWonderTriggerKind.EquipmentEquipped:
                    return requestedEquips.Contains(definition.SubjectId);
                case WorldWonderTriggerKind.PelagicResolutionCompleted:
                    return TetherfallWorldWonderCatalog.GetResolution(
                        progress.pelagicResolutionId) != null;
                case WorldWonderTriggerKind.FlightPactBonded:
                    return progress.flightPactSpeciesId.StartsWith(
                        "species.",
                        StringComparison.Ordinal);
                default:
                    return true;
            }
        }

        private static List<string> RetainSupportedEvidence(
            List<string> source,
            HashSet<string> retainedMilestones,
            bool equipmentEvidence)
        {
            var retained = new List<string>();
            for (var index = 0; index < source.Count; index++)
            {
                var id = source[index];
                var reward = TetherfallWorldWonderCatalog.GetReward(id);
                if (reward == null)
                {
                    retained.Add(id);
                    continue;
                }

                var milestoneId = equipmentEvidence
                    ? reward.EquipMilestoneId
                    : reward.GrantMilestoneId;
                if (!string.IsNullOrEmpty(milestoneId) &&
                    retainedMilestones.Contains(milestoneId))
                {
                    retained.Add(id);
                }
            }

            retained.Sort(StringComparer.Ordinal);
            return retained;
        }

        private static bool PrerequisitesComplete(
            WorldWonderMilestoneDefinition definition,
            ICollection<string> completedIds)
        {
            if (definition == null || completedIds == null)
            {
                return false;
            }

            for (var index = 0; index < definition.PrerequisiteMilestoneIds.Count; index++)
            {
                if (!completedIds.Contains(definition.PrerequisiteMilestoneIds[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static WorldWonderMilestoneDefinition GetNextIncompleteMilestone(
            TetherfallWorldWonderProgressData progress,
            string pathId)
        {
            WorldWonderMilestoneDefinition next = null;
            for (var index = 0;
                 index < TetherfallWorldWonderCatalog.Milestones.Count;
                 index++)
            {
                var definition = TetherfallWorldWonderCatalog.Milestones[index];
                if (!string.Equals(definition.PathId, pathId, StringComparison.Ordinal) ||
                    IsMilestoneComplete(progress, definition.Id))
                {
                    continue;
                }

                if (next == null || definition.Order < next.Order)
                {
                    next = definition;
                }
            }

            return next;
        }

        private static List<string> NormalizeIdList(List<string> source)
        {
            var normalized = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            if (source != null)
            {
                for (var index = 0; index < source.Count; index++)
                {
                    var id = NormalizeScalarId(source[index]);
                    if (!string.IsNullOrEmpty(id) && unique.Add(id))
                    {
                        normalized.Add(id);
                    }
                }
            }

            normalized.Sort(StringComparer.Ordinal);
            return normalized;
        }

        private static string NormalizeScalarId(string value)
        {
            value = value?.Trim() ?? string.Empty;
            return IsStableSemanticId(value) ? value : string.Empty;
        }

        private static bool ContainsOrdinal(List<string> values, string value)
        {
            if (values == null || string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (var index = 0; index < values.Count; index++)
            {
                if (string.Equals(values[index], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (values != null &&
                !string.IsNullOrEmpty(value) &&
                !ContainsOrdinal(values, value))
            {
                values.Add(value);
            }
        }

        private static bool IsCoordinateLikeSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment))
            {
                return true;
            }

            var allDigits = true;
            var underscoreCount = 0;
            for (var index = 0; index < segment.Length; index++)
            {
                var character = segment[index];
                if (character == '_')
                {
                    underscoreCount++;
                    continue;
                }

                if (character < '0' || character > '9')
                {
                    allDigits = false;
                }
            }

            if (allDigits)
            {
                return true;
            }

            if (underscoreCount != 1)
            {
                return false;
            }

            var parts = segment.Split('_');
            return parts.Length == 2 &&
                   IsUnsignedInteger(parts[0]) &&
                   IsUnsignedInteger(parts[1]);
        }

        private static bool IsUnsignedInteger(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (var index = 0; index < value.Length; index++)
            {
                if (value[index] < '0' || value[index] > '9')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
