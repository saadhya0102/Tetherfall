using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Tetherfall
{
    public enum TetherfallPelagicGateStatus
    {
        ModuleUnavailable,
        SealedByGlasswaterRegent,
        AwaitingTideglassCrown,
        Open
    }

    public enum TetherfallPelagicSocketKind
    {
        ThreadwayDock,
        EmergencyAscent,
        Vendor,
        Quest,
        Activity,
        Arena,
        Boss
    }

    public enum TetherfallPelagicRoomKind
    {
        Entrance,
        Dock,
        Terrace,
        Market,
        Current,
        Throne,
        Arena,
        Boss
    }

    public enum TetherfallPelagicEvacuationReason
    {
        None,
        OxygenDepleted,
        ManualAscent
    }

    public readonly struct TetherfallPelagicGateFacts
    {
        public bool ModuleAvailable { get; }
        public bool GlasswaterRewardResolved { get; }
        public bool TideglassCrownOwned { get; }

        public TetherfallPelagicGateFacts(
            bool moduleAvailable,
            bool glasswaterRewardResolved,
            bool tideglassCrownOwned)
        {
            ModuleAvailable = moduleAvailable;
            GlasswaterRewardResolved = glasswaterRewardResolved;
            TideglassCrownOwned = tideglassCrownOwned;
        }
    }

    public readonly struct TetherfallPelagicGateDecision
    {
        public TetherfallPelagicGateStatus Status { get; }
        public bool CanEnter => Status == TetherfallPelagicGateStatus.Open;
        public string RequirementText { get; }
        public string InteractionPrompt { get; }

        public TetherfallPelagicGateDecision(
            TetherfallPelagicGateStatus status,
            string requirementText,
            string interactionPrompt)
        {
            Status = status;
            RequirementText = requirementText;
            InteractionPrompt = interactionPrompt;
        }
    }

    [Serializable]
    public struct TetherfallPelagicOxygenState
    {
        public float remainingSeconds;
        public int evacuationCount;
    }

    public readonly struct TetherfallPelagicBreathStep
    {
        public TetherfallPelagicOxygenState State { get; }
        public bool ShouldEvacuate { get; }
        public TetherfallPelagicEvacuationReason EvacuationReason { get; }

        public TetherfallPelagicBreathStep(
            TetherfallPelagicOxygenState state,
            bool shouldEvacuate,
            TetherfallPelagicEvacuationReason evacuationReason)
        {
            State = state;
            ShouldEvacuate = shouldEvacuate;
            EvacuationReason = evacuationReason;
        }
    }

    [Serializable]
    public struct TetherfallPelagicRewardLedger
    {
        public bool bossRewardObserved;
        public bool relicGranted;
        public int relicGrantCount;
    }

    [Serializable]
    public sealed class TetherfallPelagicCourtStateData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int completedProgressionNodeCount;
        public bool bossRewardObserved;
        public bool relicGranted;
        public int relicGrantCount;
        public float oxygenRemainingSeconds = TetherfallPelagicCourtRules.MaximumOxygenSeconds;
        public int evacuationCount;
    }

    public sealed partial class TetherfallSaveData
    {
        public TetherfallPelagicCourtStateData pelagicCourt =
            new TetherfallPelagicCourtStateData();
    }

    public sealed class TetherfallPelagicRoomDefinition
    {
        public string id;
        public string displayName;
        public TetherfallPelagicRoomKind kind;
        public Vector3 localCenter;
        public Vector3 size;
    }

    public sealed class TetherfallPelagicConnectionDefinition
    {
        public string id;
        public string fromRoomId;
        public string toRoomId;
    }

    public sealed class TetherfallPelagicSocketDefinition
    {
        public string id;
        public string displayName;
        public string roomId;
        public TetherfallPelagicSocketKind kind;
        public Vector3 localPosition;
        public string integrationHint;
    }

    public sealed class TetherfallPelagicProgressionNode
    {
        public string id;
        public string displayName;
        public string prerequisiteId;
        public string triggerId;
    }

    public static class TetherfallPelagicCourtRules
    {
        public const float MaximumOxygenSeconds = 36f;
        public const float PressureDrainPerSecond = 1.25f;
        public const float SurfaceRecoveryPerSecond = 9f;
        public const float MaximumSimulationStepSeconds = 0.5f;

        public const string RegentRequirement =
            "The abyssal kelp seal is unsafe. Defeat the Glasswater Regent in Glasswater Sluice.";
        public const string CrownRequirement =
            "Glasswater is clear, but abyssal pressure remains lethal. Claim and attune the Tideglass Crown.";
        public const string OpenRequirement =
            "Tideglass Crown attuned. Nacre Court can be entered safely.";
        public const string UnavailableRequirement =
            "Nacre Court is not built in this runtime.";

        public static TetherfallPelagicGateDecision ResolveGate(
            TetherfallPelagicGateFacts facts)
        {
            if (!facts.ModuleAvailable)
            {
                return new TetherfallPelagicGateDecision(
                    TetherfallPelagicGateStatus.ModuleUnavailable,
                    UnavailableRequirement,
                    "Nacre Court unavailable");
            }

            if (!facts.GlasswaterRewardResolved)
            {
                return new TetherfallPelagicGateDecision(
                    TetherfallPelagicGateStatus.SealedByGlasswaterRegent,
                    RegentRequirement,
                    "Sealed: defeat the Glasswater Regent");
            }

            if (!facts.TideglassCrownOwned)
            {
                return new TetherfallPelagicGateDecision(
                    TetherfallPelagicGateStatus.AwaitingTideglassCrown,
                    CrownRequirement,
                    "Sealed: attune the Tideglass Crown");
            }

            return new TetherfallPelagicGateDecision(
                TetherfallPelagicGateStatus.Open,
                OpenRequirement,
                "Descend through the Nacre Court kelp gate");
        }

        public static TetherfallPelagicOxygenState CreateFullOxygenState(
            int evacuationCount = 0)
        {
            return new TetherfallPelagicOxygenState
            {
                remainingSeconds = MaximumOxygenSeconds,
                evacuationCount = Math.Max(0, evacuationCount)
            };
        }

        public static TetherfallPelagicOxygenState NormalizeOxygen(
            TetherfallPelagicOxygenState state)
        {
            state.remainingSeconds = ClampFinite(
                state.remainingSeconds,
                0f,
                MaximumOxygenSeconds,
                MaximumOxygenSeconds);
            state.evacuationCount = Math.Max(0, state.evacuationCount);
            return state;
        }

        public static TetherfallPelagicBreathStep StepOxygen(
            TetherfallPelagicOxygenState state,
            float deltaSeconds,
            bool underAbyssalPressure,
            bool hasInfiniteBreathing)
        {
            state = NormalizeOxygen(state);
            var boundedDelta = ClampFinite(
                deltaSeconds,
                0f,
                MaximumSimulationStepSeconds,
                0f);

            if (hasInfiniteBreathing)
            {
                state.remainingSeconds = MaximumOxygenSeconds;
                return new TetherfallPelagicBreathStep(
                    state,
                    false,
                    TetherfallPelagicEvacuationReason.None);
            }

            if (underAbyssalPressure)
            {
                state.remainingSeconds = Math.Max(
                    0f,
                    state.remainingSeconds - PressureDrainPerSecond * boundedDelta);
            }
            else
            {
                state.remainingSeconds = Math.Min(
                    MaximumOxygenSeconds,
                    state.remainingSeconds + SurfaceRecoveryPerSecond * boundedDelta);
            }

            state = NormalizeOxygen(state);
            var evacuate = underAbyssalPressure && state.remainingSeconds <= 0f;
            return new TetherfallPelagicBreathStep(
                state,
                evacuate,
                evacuate
                    ? TetherfallPelagicEvacuationReason.OxygenDepleted
                    : TetherfallPelagicEvacuationReason.None);
        }

        public static bool TryResolveRelicReward(
            ref TetherfallPelagicRewardLedger ledger,
            string bossId,
            bool bossRewardResolved,
            bool relicAlreadyOwned)
        {
            NormalizeRewardLedger(ref ledger);
            if (!string.Equals(
                    bossId,
                    TetherfallPelagicCourt.KeyBossId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            if (!bossRewardResolved && !relicAlreadyOwned)
            {
                return false;
            }

            ledger.bossRewardObserved = true;
            var shouldGrant = !relicAlreadyOwned && !ledger.relicGranted;
            ledger.relicGranted = true;
            ledger.relicGrantCount = 1;
            return shouldGrant;
        }

        public static void NormalizeRewardLedger(
            ref TetherfallPelagicRewardLedger ledger)
        {
            ledger.relicGrantCount = Math.Max(0, Math.Min(1, ledger.relicGrantCount));
            if (ledger.relicGranted || ledger.relicGrantCount > 0)
            {
                ledger.bossRewardObserved = true;
                ledger.relicGranted = true;
                ledger.relicGrantCount = 1;
            }
        }

        public static TetherfallPelagicCourtStateData NormalizeState(
            TetherfallPelagicCourtStateData source,
            int progressionNodeCount)
        {
            var state = source ?? new TetherfallPelagicCourtStateData();
            state.version = TetherfallPelagicCourtStateData.CurrentVersion;
            state.completedProgressionNodeCount = Math.Max(
                0,
                Math.Min(progressionNodeCount, state.completedProgressionNodeCount));
            state.relicGrantCount = Math.Max(0, Math.Min(1, state.relicGrantCount));
            if (state.relicGranted || state.relicGrantCount > 0)
            {
                state.bossRewardObserved = true;
                state.relicGranted = true;
                state.relicGrantCount = 1;
                state.completedProgressionNodeCount = Math.Max(
                    2,
                    state.completedProgressionNodeCount);
            }
            else if (state.bossRewardObserved)
            {
                state.completedProgressionNodeCount = Math.Max(
                    1,
                    state.completedProgressionNodeCount);
            }

            state.oxygenRemainingSeconds = ClampFinite(
                state.oxygenRemainingSeconds,
                0f,
                MaximumOxygenSeconds,
                MaximumOxygenSeconds);
            state.evacuationCount = Math.Max(0, state.evacuationCount);
            return state;
        }

        public static bool CanAdvanceProgression(
            IReadOnlyList<TetherfallPelagicProgressionNode> nodes,
            int completedNodeCount,
            string requestedNodeId)
        {
            if (nodes == null ||
                completedNodeCount < 0 ||
                completedNodeCount >= nodes.Count ||
                string.IsNullOrEmpty(requestedNodeId))
            {
                return false;
            }

            var candidate = nodes[completedNodeCount];
            if (candidate == null ||
                !string.Equals(candidate.id, requestedNodeId, StringComparison.Ordinal))
            {
                return false;
            }

            if (completedNodeCount == 0)
            {
                return string.IsNullOrEmpty(candidate.prerequisiteId);
            }

            var prior = nodes[completedNodeCount - 1];
            return prior != null &&
                   string.Equals(
                       candidate.prerequisiteId,
                       prior.id,
                       StringComparison.Ordinal);
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float ClampFinite(
            float value,
            float minimum,
            float maximum,
            float fallback)
        {
            if (!IsFinite(value))
            {
                return fallback;
            }

            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }

    public sealed partial class TetherfallGame
    {
        private TetherfallPelagicCourtRuntime pelagicCourtRuntime;

        public TetherfallPelagicCourtRuntime PelagicCourtRuntime =>
            pelagicCourtRuntime;

        public bool HasTideglassCrown =>
            unlockedEquipment.Contains(TetherfallPelagicCourt.InfiniteBreathingRelicId);

        internal void RegisterPelagicCourtRuntime(
            TetherfallPelagicCourtRuntime runtime)
        {
            pelagicCourtRuntime = runtime;
        }

        internal void EnsurePelagicCourtEquipmentDefinition()
        {
            if (equipmentCatalog.Exists(
                    item => item != null &&
                            string.Equals(
                                item.id,
                                TetherfallPelagicCourt.InfiniteBreathingRelicId,
                                StringComparison.Ordinal)))
            {
                return;
            }

            equipmentCatalog.Add(TetherfallPelagicCourt.CreateRelicEquipmentDefinition());
        }

        internal bool IsGlasswaterRegentRewardResolvedForPelagicCourt()
        {
            for (var index = 0; index < modularDungeons.Count; index++)
            {
                var dungeon = modularDungeons[index];
                if (dungeon == null ||
                    dungeon.Definition == null ||
                    dungeon.Progress == null)
                {
                    continue;
                }

                if (string.Equals(
                        dungeon.Definition.bossId,
                        TetherfallPelagicCourt.KeyBossId,
                        StringComparison.Ordinal) &&
                    dungeon.Progress.bossDefeated &&
                    dungeon.Progress.rewardGranted)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool TryGrantTideglassCrown()
        {
            EnsurePelagicCourtEquipmentDefinition();
            var definition = equipmentCatalog.Find(
                item => item != null &&
                        string.Equals(
                            item.id,
                            TetherfallPelagicCourt.InfiniteBreathingRelicId,
                            StringComparison.Ordinal));
            if (definition == null ||
                !unlockedEquipment.Add(TetherfallPelagicCourt.InfiniteBreathingRelicId))
            {
                return false;
            }

            AutoEquipFieldGear(definition);
            Player?.RefreshEquipmentVisuals();
            Notify(
                "Glasswater yields the Tideglass Crown. Its sealed current grants infinite breath below Nacre Court.");
            RequestSave();
            return true;
        }

        public TetherfallPelagicGateDecision EvaluatePelagicCourtGate()
        {
            return TetherfallPelagicCourtRules.ResolveGate(
                new TetherfallPelagicGateFacts(
                    pelagicCourtRuntime != null,
                    pelagicCourtRuntime != null
                        ? pelagicCourtRuntime.State.bossRewardObserved
                        : IsGlasswaterRegentRewardResolvedForPelagicCourt(),
                    HasTideglassCrown));
        }

        public bool OnPelagicCourtBossRewardResolved(string bossId)
        {
            return pelagicCourtRuntime != null &&
                   pelagicCourtRuntime.ObserveBossReward(bossId);
        }

        public void WritePelagicCourtState(TetherfallSaveData data)
        {
            if (data == null || pelagicCourtRuntime == null)
            {
                return;
            }

            data.pelagicCourt = pelagicCourtRuntime.CaptureState();
        }

        public void RestorePelagicCourtState(TetherfallSaveData data)
        {
            if (pelagicCourtRuntime == null)
            {
                return;
            }

            pelagicCourtRuntime.RestoreState(data?.pelagicCourt);
        }
    }

    public static class TetherfallPelagicCourt
    {
        public const string RootName = "Pelagic Court · Nacre Court";
        public const string CourtId = "city.pelagic_court.nacre_court";
        public const string KeyBossId = DungeonCatalog.GlasswaterBossId;
        public const string InfiniteBreathingRelicId = "relic.pelagic_court.tideglass_crown";
        public const string PelagicBossId = "boss.pelagic_court.leviathan_heir";
        public const string ThreadwayDockSocketId =
            "socket.pelagic_court.threadway_dock";
        public const string EmergencyAscentSocketId =
            "socket.pelagic_court.emergency_ascent";
        public const string ThroneQuestSocketId =
            "socket.pelagic_court.quest.pearl_audience";
        public const string CurrentTrialSocketId =
            "socket.pelagic_court.activity.current_trial";
        public const string ArenaSocketId =
            "socket.pelagic_court.activity.reef_arena";
        public const string BossSocketId =
            "socket.pelagic_court.boss.leviathan_heir";

        public const int MaxRendererCount = 180;
        public const int MaxColliderCount = 48;
        public const int MaxSharedMaterialCount = 14;
        public const int AddedRealtimeLightCount = 0;
        public const int UpdateDriverCount = 2;
        public const int PreferredResourcePlacementCount = 7;
        public const float ActivationRefreshSeconds = 0.25f;
        public const float StructuralActivationDistance = 190f;
        public const float DetailActivationDistance = 118f;
        public const float DeepActivationCeiling = -12f;

        public static readonly Vector3 SurfaceEntrancePosition =
            new Vector3(165f, 0.9f, -34f);
        public static readonly Vector3 SurfaceEvacuationPosition =
            new Vector3(165f, 1.1f, -18f);
        public static readonly Vector3 CityCenter =
            new Vector3(165f, -46f, -34f);
        public static readonly Vector3 CityArrivalLocalPosition =
            new Vector3(0f, 2.4f, 51f);
        public static readonly Bounds PressureLocalBounds =
            new Bounds(new Vector3(0f, 5f, 0f), new Vector3(132f, 48f, 142f));

        private static readonly Dictionary<PrimitiveType, Mesh> PrimitiveMeshCache =
            new Dictionary<PrimitiveType, Mesh>();
        private static readonly TetherfallPelagicRoomDefinition[] RoomDefinitions =
        {
            Room(
                "room.pelagic_court.kelp_threshold",
                "Kelp-Seal Threshold",
                TetherfallPelagicRoomKind.Entrance,
                new Vector3(0f, 2f, 51f),
                new Vector3(26f, 12f, 20f)),
            Room(
                "room.pelagic_court.threadway_dock",
                "Undertow Threadway Dock",
                TetherfallPelagicRoomKind.Dock,
                new Vector3(0f, 2f, 37f),
                new Vector3(28f, 14f, 20f)),
            Room(
                "room.pelagic_court.coral_terraces",
                "Coral Terraces",
                TetherfallPelagicRoomKind.Terrace,
                new Vector3(-29f, 1f, 18f),
                new Vector3(32f, 16f, 30f)),
            Room(
                "room.pelagic_court.current_gallery",
                "Current Gallery",
                TetherfallPelagicRoomKind.Current,
                new Vector3(29f, 3f, 18f),
                new Vector3(30f, 18f, 30f)),
            Room(
                "room.pelagic_court.nacre_market",
                "Nacre Market",
                TetherfallPelagicRoomKind.Market,
                new Vector3(-25f, 2f, -9f),
                new Vector3(32f, 16f, 28f)),
            Room(
                "room.pelagic_court.bubble_gardens",
                "Bubble Gardens",
                TetherfallPelagicRoomKind.Terrace,
                new Vector3(25f, 3f, -10f),
                new Vector3(30f, 18f, 28f)),
            Room(
                "room.pelagic_court.throne_district",
                "Pearl Throne District",
                TetherfallPelagicRoomKind.Throne,
                new Vector3(0f, 4f, -30f),
                new Vector3(36f, 20f, 30f)),
            Room(
                "room.pelagic_court.reef_arena",
                "Reefguard Arena",
                TetherfallPelagicRoomKind.Arena,
                new Vector3(-33f, 1f, -49f),
                new Vector3(34f, 16f, 30f)),
            Room(
                "room.pelagic_court.abyssal_sanctum",
                "Abyssal Crown Sanctum",
                TetherfallPelagicRoomKind.Boss,
                new Vector3(30f, 3f, -50f),
                new Vector3(38f, 20f, 34f))
        };

        private static readonly TetherfallPelagicConnectionDefinition[] ConnectionDefinitions =
        {
            Connection(
                "connection.pelagic_court.threshold_dock",
                "room.pelagic_court.kelp_threshold",
                "room.pelagic_court.threadway_dock"),
            Connection(
                "connection.pelagic_court.dock_terraces",
                "room.pelagic_court.threadway_dock",
                "room.pelagic_court.coral_terraces"),
            Connection(
                "connection.pelagic_court.dock_currents",
                "room.pelagic_court.threadway_dock",
                "room.pelagic_court.current_gallery"),
            Connection(
                "connection.pelagic_court.terraces_market",
                "room.pelagic_court.coral_terraces",
                "room.pelagic_court.nacre_market"),
            Connection(
                "connection.pelagic_court.currents_gardens",
                "room.pelagic_court.current_gallery",
                "room.pelagic_court.bubble_gardens"),
            Connection(
                "connection.pelagic_court.market_throne",
                "room.pelagic_court.nacre_market",
                "room.pelagic_court.throne_district"),
            Connection(
                "connection.pelagic_court.gardens_throne",
                "room.pelagic_court.bubble_gardens",
                "room.pelagic_court.throne_district"),
            Connection(
                "connection.pelagic_court.throne_arena",
                "room.pelagic_court.throne_district",
                "room.pelagic_court.reef_arena"),
            Connection(
                "connection.pelagic_court.arena_sanctum",
                "room.pelagic_court.reef_arena",
                "room.pelagic_court.abyssal_sanctum")
        };

        private static readonly TetherfallPelagicSocketDefinition[] SocketDefinitions =
        {
            Socket(
                ThreadwayDockSocketId,
                "Undertow Threadway docking socket",
                "room.pelagic_court.threadway_dock",
                TetherfallPelagicSocketKind.ThreadwayDock,
                new Vector3(0f, 3f, 37f),
                "Bind to the global Threadway catalog only after relic-gated destination filtering is available."),
            Socket(
                EmergencyAscentSocketId,
                "Emergency ascent bell",
                "room.pelagic_court.threadway_dock",
                TetherfallPelagicSocketKind.EmergencyAscent,
                new Vector3(7f, 3f, 39f),
                "Always returns the expedition to finite safe ground."),
            Socket(
                "socket.pelagic_court.vendor.nacre_exchange",
                "Serrin's Nacre Exchange",
                "room.pelagic_court.nacre_market",
                TetherfallPelagicSocketKind.Vendor,
                new Vector3(-29f, 3f, -7f),
                "Bind to OpenVendor after adding a pelagic vendor profile."),
            Socket(
                "socket.pelagic_court.vendor.currentwright",
                "Ilyra's Currentwright Bench",
                "room.pelagic_court.nacre_market",
                TetherfallPelagicSocketKind.Vendor,
                new Vector3(-20f, 3f, -8f),
                "Bind to a pressure-safe equipment service."),
            Socket(
                ThroneQuestSocketId,
                "Pearl Throne audience socket",
                "room.pelagic_court.throne_district",
                TetherfallPelagicSocketKind.Quest,
                new Vector3(0f, 6f, -28f),
                "Report completion through TryAdvanceProgression after the authored audience quest resolves."),
            Socket(
                CurrentTrialSocketId,
                "Three Currents trial socket",
                "room.pelagic_court.current_gallery",
                TetherfallPelagicSocketKind.Activity,
                new Vector3(29f, 4f, 18f),
                "Bind to a bounded current-navigation activity."),
            Socket(
                ArenaSocketId,
                "Reefguard combat arena socket",
                "room.pelagic_court.reef_arena",
                TetherfallPelagicSocketKind.Arena,
                new Vector3(-33f, 3f, -49f),
                "Bind to an Enemy.Create wave director and report arena completion."),
            Socket(
                BossSocketId,
                "Leviathan Heir boss socket",
                "room.pelagic_court.abyssal_sanctum",
                TetherfallPelagicSocketKind.Boss,
                new Vector3(30f, 5f, -50f),
                "Bind to DungeonBoss after adding the stable boss profile and authored silhouette.")
        };

        private static readonly TetherfallPelagicProgressionNode[] ProgressionDefinitions =
        {
            Progression(
                "progress.pelagic_court.glasswater_regent_defeated",
                "Glasswater Regent defeated",
                string.Empty,
                KeyBossId),
            Progression(
                "progress.pelagic_court.tideglass_crown_attuned",
                "Tideglass Crown attuned",
                "progress.pelagic_court.glasswater_regent_defeated",
                InfiniteBreathingRelicId),
            Progression(
                "progress.pelagic_court.kelp_gate_opened",
                "Abyssal kelp gate opened",
                "progress.pelagic_court.tideglass_crown_attuned",
                CourtId),
            Progression(
                "progress.pelagic_court.threadway_dock_charted",
                "Undertow dock charted",
                "progress.pelagic_court.kelp_gate_opened",
                ThreadwayDockSocketId),
            Progression(
                "progress.pelagic_court.pearl_audience_completed",
                "Pearl Throne audience completed",
                "progress.pelagic_court.threadway_dock_charted",
                ThroneQuestSocketId),
            Progression(
                "progress.pelagic_court.current_trial_completed",
                "Three Currents trial completed",
                "progress.pelagic_court.pearl_audience_completed",
                CurrentTrialSocketId),
            Progression(
                "progress.pelagic_court.reef_arena_cleared",
                "Reefguard arena cleared",
                "progress.pelagic_court.current_trial_completed",
                ArenaSocketId),
            Progression(
                "progress.pelagic_court.leviathan_heir_defeated",
                "Leviathan Heir defeated",
                "progress.pelagic_court.reef_arena_cleared",
                PelagicBossId)
        };

        public static IReadOnlyList<TetherfallPelagicRoomDefinition> Rooms =>
            RoomDefinitions;
        public static IReadOnlyList<TetherfallPelagicConnectionDefinition> Connections =>
            ConnectionDefinitions;
        public static IReadOnlyList<TetherfallPelagicSocketDefinition> Sockets =>
            SocketDefinitions;
        public static IReadOnlyList<TetherfallPelagicProgressionNode> Progression =>
            ProgressionDefinitions;

        public static GameObject Build(TetherfallGame game)
        {
            return BuildInternal(game, false);
        }

        public static GameObject BuildForcedFallback(TetherfallGame game)
        {
            return BuildInternal(game, true);
        }

        public static bool Cleanup(TetherfallGame game)
        {
            if (game == null)
            {
                return false;
            }

            var marker = FindRoot(game.transform);
            if (marker == null)
            {
                return false;
            }

            marker.gameObject.SetActive(false);
            if (game.PelagicCourtRuntime == marker.Runtime)
            {
                game.RegisterPelagicCourtRuntime(null);
            }

            marker.transform.SetParent(null, true);
            DestroyObject(marker.gameObject);
            return true;
        }

        public static TetherfallPelagicCourtRoot GetBuiltRoot(TetherfallGame game)
        {
            return game != null ? FindRoot(game.transform) : null;
        }

        public static TetherfallPelagicRoomDefinition GetRoom(string roomId)
        {
            for (var index = 0; index < RoomDefinitions.Length; index++)
            {
                if (string.Equals(
                        RoomDefinitions[index].id,
                        roomId,
                        StringComparison.Ordinal))
                {
                    return RoomDefinitions[index];
                }
            }

            return null;
        }

        public static TetherfallPelagicSocketDefinition GetSocket(string socketId)
        {
            for (var index = 0; index < SocketDefinitions.Length; index++)
            {
                if (string.Equals(
                        SocketDefinitions[index].id,
                        socketId,
                        StringComparison.Ordinal))
                {
                    return SocketDefinitions[index];
                }
            }

            return null;
        }

        public static EquipmentDefinition CreateRelicEquipmentDefinition()
        {
            return new EquipmentDefinition
            {
                id = InfiniteBreathingRelicId,
                displayName = "Tideglass Crown",
                slot = EquipmentSlot.Charm,
                restorationRewardOnly = true,
                description =
                    "The Glasswater Regent's pressure-sealed crown. Once attuned, it grants infinite breath in abyssal water."
            };
        }

        private static GameObject BuildInternal(
            TetherfallGame game,
            bool forceFallback)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            game.EnsurePelagicCourtEquipmentDefinition();
            var existing = FindRoot(game.transform);
            if (existing != null)
            {
                existing.Runtime?.ReconcileExternalProgress();
                return existing.gameObject;
            }

            var root = new GameObject(RootName);
            root.transform.SetParent(game.transform, false);
            try
            {
                var visuals = CreateRoot(root.transform, "Visuals · Collider Free", Vector3.zero);
                var gameplay = CreateRoot(root.transform, "Gameplay · Renderer Free", Vector3.zero);
                var interactions = CreateRoot(root.transform, "Interaction Sockets", Vector3.zero);
                var surfaceVisuals = CreateRoot(
                    visuals,
                    "Surface Kelp Seal",
                    Vector3.zero);
                var deepStructural = CreateRoot(
                    visuals,
                    "Deep Structural Art",
                    CityCenter);
                var deepDetail = CreateRoot(
                    visuals,
                    "Deep Detail Art",
                    CityCenter);
                var deepGameplay = CreateRoot(
                    gameplay,
                    "Deep Gameplay Volumes",
                    CityCenter);
                var deepInteractions = CreateRoot(
                    interactions,
                    "Deep Interaction Sockets",
                    CityCenter);

                var marker = root.AddComponent<TetherfallPelagicCourtRoot>();
                var context = new BuildContext(
                    game,
                    root.transform,
                    surfaceVisuals,
                    deepStructural,
                    deepDetail,
                    deepGameplay,
                    forceFallback);
                context.BuildSurface(out var gateSealRenderer, out var gateLabel);
                context.BuildCity();

                var runtime = root.AddComponent<TetherfallPelagicCourtRuntime>();
                var pressure = root.AddComponent<TetherfallPelagicPressure>();
                runtime.Initialize(
                    game,
                    marker,
                    deepStructural.gameObject,
                    deepDetail.gameObject,
                    deepGameplay.gameObject,
                    deepInteractions.gameObject,
                    gateSealRenderer,
                    gateLabel,
                    pressure);
                pressure.Initialize(game, runtime);
                BuildInteractions(
                    game,
                    runtime,
                    interactions,
                    deepInteractions);
                context.ValidateAndConfigure(
                    marker,
                    visuals,
                    gameplay,
                    interactions,
                    runtime,
                    pressure);
                game.RegisterPelagicCourtRuntime(runtime);
                runtime.ReconcileExternalProgress();
                return root;
            }
            catch
            {
                root.transform.SetParent(null, true);
                DestroyObject(root);
                throw;
            }
        }

        private static void BuildInteractions(
            TetherfallGame game,
            TetherfallPelagicCourtRuntime runtime,
            Transform interactions,
            Transform deepInteractions)
        {
            var entranceObject = new GameObject("interaction.pelagic_court.kelp_gate");
            entranceObject.transform.SetParent(interactions, false);
            entranceObject.transform.localPosition = SurfaceEntrancePosition;
            var entrance = entranceObject.AddComponent<TetherfallPelagicEntrance>();
            entrance.Initialize(runtime);
            game.RegisterInteractable(entrance);

            for (var index = 0; index < SocketDefinitions.Length; index++)
            {
                var definition = SocketDefinitions[index];
                var socketObject = new GameObject(definition.id);
                socketObject.transform.SetParent(deepInteractions, false);
                socketObject.transform.localPosition = definition.localPosition;
                var socket = socketObject.AddComponent<TetherfallPelagicSocket>();
                socket.Initialize(runtime, definition);
                game.RegisterInteractable(socket);
            }
        }

        private static Transform CreateRoot(
            Transform parent,
            string name,
            Vector3 localPosition)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = localPosition;
            return root;
        }

        private static TetherfallPelagicCourtRoot FindRoot(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                var candidate = parent.GetChild(index);
                if (candidate == null)
                {
                    continue;
                }

                var marker = candidate.GetComponent<TetherfallPelagicCourtRoot>();
                if (marker != null)
                {
                    return marker;
                }
            }

            return null;
        }

        private static TetherfallPelagicRoomDefinition Room(
            string id,
            string displayName,
            TetherfallPelagicRoomKind kind,
            Vector3 localCenter,
            Vector3 size)
        {
            return new TetherfallPelagicRoomDefinition
            {
                id = id,
                displayName = displayName,
                kind = kind,
                localCenter = localCenter,
                size = size
            };
        }

        private static TetherfallPelagicConnectionDefinition Connection(
            string id,
            string fromRoomId,
            string toRoomId)
        {
            return new TetherfallPelagicConnectionDefinition
            {
                id = id,
                fromRoomId = fromRoomId,
                toRoomId = toRoomId
            };
        }

        private static TetherfallPelagicSocketDefinition Socket(
            string id,
            string displayName,
            string roomId,
            TetherfallPelagicSocketKind kind,
            Vector3 localPosition,
            string integrationHint)
        {
            return new TetherfallPelagicSocketDefinition
            {
                id = id,
                displayName = displayName,
                roomId = roomId,
                kind = kind,
                localPosition = localPosition,
                integrationHint = integrationHint
            };
        }

        private static TetherfallPelagicProgressionNode Progression(
            string id,
            string displayName,
            string prerequisiteId,
            string triggerId)
        {
            return new TetherfallPelagicProgressionNode
            {
                id = id,
                displayName = displayName,
                prerequisiteId = prerequisiteId,
                triggerId = triggerId
            };
        }

        private static void DestroyObject(Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(value);
            }
            else
            {
                Object.DestroyImmediate(value);
            }
        }

        private static Mesh GetPrimitiveMesh(PrimitiveType primitiveType)
        {
            if (PrimitiveMeshCache.TryGetValue(primitiveType, out var cached) &&
                cached != null)
            {
                return cached;
            }

            var source = GameObject.CreatePrimitive(primitiveType);
            source.name = "Pelagic Court primitive mesh source";
            source.hideFlags = HideFlags.HideAndDontSave;
            source.SetActive(false);
            var filter = source.GetComponent<MeshFilter>();
            var mesh = filter != null ? filter.sharedMesh : null;
            DestroyObject(source);
            if (mesh == null)
            {
                throw new InvalidOperationException(
                    $"Unity did not provide a mesh for primitive {primitiveType}.");
            }

            PrimitiveMeshCache[primitiveType] = mesh;
            return mesh;
        }

        private sealed class BuildPalette
        {
            public Material abyssStone;
            public Material shell;
            public Material coral;
            public Material coralGold;
            public Material nacre;
            public Material bubble;
            public Material current;
            public Material glow;
            public Material pearlGlow;
            public Material warning;
        }

        private sealed class BuildContext
        {
            private const string ResourcePrefix = "Art/Quaternius/";

            private readonly TetherfallGame game;
            private readonly Transform root;
            private readonly Transform surfaceVisuals;
            private readonly Transform deepStructural;
            private readonly Transform deepDetail;
            private readonly Transform deepGameplay;
            private readonly bool forceFallback;
            private readonly BuildPalette palette;
            private int resourcePlacementCount;
            private int preferredResourceCount;
            private int fallbackCount;

            public BuildContext(
                TetherfallGame game,
                Transform root,
                Transform surfaceVisuals,
                Transform deepStructural,
                Transform deepDetail,
                Transform deepGameplay,
                bool forceFallback)
            {
                this.game = game;
                this.root = root;
                this.surfaceVisuals = surfaceVisuals;
                this.deepStructural = deepStructural;
                this.deepDetail = deepDetail;
                this.deepGameplay = deepGameplay;
                this.forceFallback = forceFallback;
                palette = CreatePalette(game);
            }

            public void BuildSurface(
                out Renderer gateSealRenderer,
                out TextMesh gateLabel)
            {
                for (var side = -1; side <= 1; side += 2)
                {
                    AddVisual(
                        surfaceVisuals,
                        PrimitiveType.Cylinder,
                        $"Surface kelp-seal pylon {side}",
                        SurfaceEntrancePosition + new Vector3(side * 2.2f, 1.7f, 0f),
                        new Vector3(0.36f, 1.7f, 0.36f),
                        Quaternion.Euler(0f, 0f, side * 8f),
                        palette.abyssStone);
                    AddVisual(
                        surfaceVisuals,
                        PrimitiveType.Sphere,
                        $"Surface bubble lamp {side}",
                        SurfaceEntrancePosition + new Vector3(side * 2.2f, 3.55f, 0f),
                        Vector3.one * 0.58f,
                        Quaternion.identity,
                        palette.glow);
                }

                var seal = AddVisual(
                    surfaceVisuals,
                    PrimitiveType.Sphere,
                    "Abyssal kelp pressure seal",
                    SurfaceEntrancePosition + new Vector3(0f, 1.4f, 0f),
                    new Vector3(3.3f, 2.4f, 0.5f),
                    Quaternion.identity,
                    palette.warning);
                gateSealRenderer = seal.GetComponent<Renderer>();
                AddVisual(
                    surfaceVisuals,
                    PrimitiveType.Cylinder,
                    "Kelp-seal threshold ring",
                    SurfaceEntrancePosition + new Vector3(0f, 0.1f, 0f),
                    new Vector3(3.4f, 0.12f, 3.4f),
                    Quaternion.identity,
                    palette.shell);
                AddPreferredResource(
                    surfaceVisuals,
                    "Wall_Arch",
                    SurfaceEntrancePosition + new Vector3(0f, 0.2f, 0.15f),
                    new Vector3(1.8f, 2.2f, 1.2f),
                    Quaternion.identity,
                    PrimitiveType.Cube,
                    SurfaceEntrancePosition + new Vector3(0f, 2.5f, 0.4f),
                    new Vector3(6.5f, 0.7f, 0.7f),
                    palette.shell);
                gateLabel = AddLabel(
                    surfaceVisuals,
                    "NACRE COURT\nSEALED · GLASSWATER REGENT + TIDEGLASS CROWN",
                    SurfaceEntrancePosition + new Vector3(0f, 4.65f, -0.4f),
                    new Color(0.68f, 0.95f, 0.94f),
                    0.075f);
            }

            public void BuildCity()
            {
                AddVisual(
                    deepStructural,
                    PrimitiveType.Cylinder,
                    "Nacre Court abyssal shelf",
                    new Vector3(0f, -2.1f, 0f),
                    new Vector3(61f, 1.2f, 66f),
                    Quaternion.identity,
                    palette.abyssStone);
                AddCollider(
                    "Nacre Court abyssal shelf collider",
                    new Vector3(0f, -1.75f, 0f),
                    new Vector3(118f, 1.1f, 128f),
                    Quaternion.identity,
                    false);

                BuildRooms();
                BuildConnections();
                BuildDomes();
                BuildKelpGates();
                BuildCoralTerraces();
                BuildCurrentGallery();
                BuildMarket();
                BuildThroneDistrict();
                BuildArenaAndBossSanctum();
                BuildSocketMarkers();
                BuildWaterVolume();
            }

            public void ValidateAndConfigure(
                TetherfallPelagicCourtRoot marker,
                Transform visuals,
                Transform gameplay,
                Transform interactions,
                TetherfallPelagicCourtRuntime runtime,
                TetherfallPelagicPressure pressure)
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                var colliders = root.GetComponentsInChildren<Collider>(true);
                var lights = root.GetComponentsInChildren<Light>(true);
                var visualColliders = visuals.GetComponentsInChildren<Collider>(true);
                var gameplayRenderers = gameplay.GetComponentsInChildren<Renderer>(true);
                var materials = new HashSet<Material>();
                for (var index = 0; index < renderers.Length; index++)
                {
                    var renderer = renderers[index];
                    if (renderer == null)
                    {
                        continue;
                    }

                    var shared = renderer.sharedMaterials;
                    for (var materialIndex = 0; materialIndex < shared.Length; materialIndex++)
                    {
                        if (shared[materialIndex] != null)
                        {
                            materials.Add(shared[materialIndex]);
                        }
                    }
                }

                if (renderers.Length > MaxRendererCount ||
                    colliders.Length > MaxColliderCount ||
                    lights.Length > AddedRealtimeLightCount ||
                    materials.Count > MaxSharedMaterialCount ||
                    visualColliders.Length != 0 ||
                    gameplayRenderers.Length != 0 ||
                    resourcePlacementCount != PreferredResourcePlacementCount)
                {
                    throw new InvalidOperationException(
                        "Pelagic Court build exceeded its authored budget or art/collider separation contract: " +
                        $"{renderers.Length}/{MaxRendererCount} renderers, " +
                        $"{colliders.Length}/{MaxColliderCount} colliders, " +
                        $"{lights.Length}/{AddedRealtimeLightCount} lights, " +
                        $"{materials.Count}/{MaxSharedMaterialCount} shared materials, " +
                        $"{visualColliders.Length} visual colliders, " +
                        $"{gameplayRenderers.Length} gameplay renderers, " +
                        $"{resourcePlacementCount}/{PreferredResourcePlacementCount} resource placements.");
                }

                marker.Configure(
                    visuals,
                    gameplay,
                    interactions,
                    runtime,
                    pressure,
                    forceFallback,
                    renderers.Length,
                    colliders.Length,
                    lights.Length,
                    materials.Count,
                    resourcePlacementCount,
                    preferredResourceCount,
                    fallbackCount);
            }

            private void BuildRooms()
            {
                for (var index = 0; index < RoomDefinitions.Length; index++)
                {
                    var room = RoomDefinitions[index];
                    var radius = Math.Max(5f, Math.Min(room.size.x, room.size.z) * 0.38f);
                    AddVisual(
                        deepStructural,
                        PrimitiveType.Cylinder,
                        room.displayName + " shell terrace",
                        room.localCenter + Vector3.down * 0.75f,
                        new Vector3(radius, 0.42f, radius),
                        Quaternion.identity,
                        room.kind == TetherfallPelagicRoomKind.Boss
                            ? palette.nacre
                            : palette.shell);
                    AddCollider(
                        room.id + ".floor",
                        room.localCenter + Vector3.down * 0.52f,
                        new Vector3(radius * 1.7f, 0.8f, radius * 1.7f),
                        Quaternion.identity,
                        false);
                    AddLabel(
                        deepStructural,
                        room.displayName,
                        room.localCenter + new Vector3(0f, 6.5f, 0f),
                        new Color(0.68f, 0.92f, 0.91f),
                        0.065f);
                }
            }

            private void BuildConnections()
            {
                for (var index = 0; index < ConnectionDefinitions.Length; index++)
                {
                    var connection = ConnectionDefinitions[index];
                    var from = GetRoom(connection.fromRoomId);
                    var to = GetRoom(connection.toRoomId);
                    if (from == null || to == null)
                    {
                        continue;
                    }

                    var start = from.localCenter;
                    var end = to.localCenter;
                    var delta = end - start;
                    var horizontalLength = new Vector2(delta.x, delta.z).magnitude;
                    var midpoint = (start + end) * 0.5f;
                    midpoint.y = Math.Min(start.y, end.y) - 0.1f;
                    var rotation = Quaternion.Euler(
                        0f,
                        Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg,
                        0f);
                    AddVisual(
                        deepStructural,
                        PrimitiveType.Cube,
                        connection.id + ".nacre_causeway",
                        midpoint,
                        new Vector3(5.2f, 0.5f, horizontalLength + 1.5f),
                        rotation,
                        palette.nacre);
                    AddCollider(
                        connection.id + ".causeway_collider",
                        midpoint,
                        new Vector3(5.2f, 0.75f, horizontalLength + 1.5f),
                        rotation,
                        false);
                }
            }

            private void BuildDomes()
            {
                AddDome(
                    "Undertow docking dome",
                    new Vector3(0f, 5f, 37f),
                    new Vector3(15f, 10f, 15f));
                AddDome(
                    "Nacre market dome",
                    new Vector3(-25f, 6f, -9f),
                    new Vector3(18f, 12f, 17f));
                AddDome(
                    "Bubble garden dome",
                    new Vector3(25f, 7f, -10f),
                    new Vector3(17f, 13f, 16f));
                AddDome(
                    "Pearl throne dome",
                    new Vector3(0f, 9f, -30f),
                    new Vector3(21f, 17f, 19f));
                AddDomeLightPearls(new Vector3(0f, 8f, 37f), 10f, 3);
                AddDomeLightPearls(new Vector3(-25f, 9f, -9f), 12f, 3);
                AddDomeLightPearls(new Vector3(25f, 10f, -10f), 11f, 3);
                AddDomeLightPearls(new Vector3(0f, 13f, -30f), 14f, 3);
            }

            private void AddDome(
                string name,
                Vector3 center,
                Vector3 scale)
            {
                AddVisual(
                    deepStructural,
                    PrimitiveType.Sphere,
                    name + " bubble shell",
                    center,
                    scale,
                    Quaternion.identity,
                    palette.bubble);
                AddVisual(
                    deepStructural,
                    PrimitiveType.Cylinder,
                    name + " nacre footing",
                    new Vector3(center.x, center.y - scale.y * 0.47f, center.z),
                    new Vector3(scale.x * 0.54f, 0.24f, scale.z * 0.54f),
                    Quaternion.identity,
                    palette.nacre);
                AddVisual(
                    deepStructural,
                    PrimitiveType.Sphere,
                    name + " bubble-light crown",
                    new Vector3(center.x, center.y + scale.y * 0.45f, center.z),
                    Vector3.one * 0.72f,
                    Quaternion.identity,
                    palette.pearlGlow);
            }

            private void BuildKelpGates()
            {
                var centers = new[]
                {
                    new Vector3(0f, 3.5f, 46f),
                    new Vector3(0f, 5f, -20f),
                    new Vector3(0f, 4f, -43f)
                };
                for (var gateIndex = 0; gateIndex < centers.Length; gateIndex++)
                {
                    var center = centers[gateIndex];
                    for (var side = -1; side <= 1; side += 2)
                    {
                        AddVisual(
                            deepStructural,
                            PrimitiveType.Capsule,
                            $"Kelp gate {gateIndex + 1} frond {side}",
                            center + new Vector3(side * 3f, 0f, 0f),
                            new Vector3(0.55f, 3.4f, 0.55f),
                            Quaternion.Euler(0f, 0f, side * 12f),
                            gateIndex == 0 ? palette.glow : palette.coral);
                    }

                    AddVisual(
                        deepStructural,
                        PrimitiveType.Cube,
                        $"Kelp gate {gateIndex + 1} header",
                        center + Vector3.up * 3.5f,
                        new Vector3(6.8f, 0.5f, 0.5f),
                        Quaternion.identity,
                        palette.nacre);
                }
            }

            private void BuildCoralTerraces()
            {
                for (var index = 0; index < 12; index++)
                {
                    var angle = index * Mathf.PI * 2f / 12f;
                    var radius = 8f + index % 3 * 2.2f;
                    var position = new Vector3(
                        -29f + Mathf.Cos(angle) * radius,
                        1.2f + index % 2 * 0.45f,
                        18f + Mathf.Sin(angle) * radius);
                    var height = 1.7f + index % 4 * 0.45f;
                    AddVisual(
                        deepDetail,
                        PrimitiveType.Capsule,
                        $"Coral terrace fan {index + 1}",
                        position,
                        new Vector3(0.55f + index % 2 * 0.18f, height, 0.55f),
                        Quaternion.Euler(index * 11f % 18f, index * 37f, index % 2 == 0 ? 14f : -14f),
                        index % 2 == 0 ? palette.coral : palette.coralGold);
                    AddVisual(
                        deepDetail,
                        PrimitiveType.Sphere,
                        $"Coral terrace pearl {index + 1}",
                        position + Vector3.up * (height + 0.8f),
                        Vector3.one * (0.26f + index % 3 * 0.08f),
                        Quaternion.identity,
                        palette.pearlGlow);
                }

                AddPreferredResource(
                    deepDetail,
                    "Rock_Medium_1",
                    new Vector3(-39f, 0.5f, 12f),
                    Vector3.one * 2.2f,
                    Quaternion.Euler(0f, 18f, 0f),
                    PrimitiveType.Sphere,
                    new Vector3(-39f, 0.8f, 12f),
                    new Vector3(2.8f, 1.4f, 2.4f),
                    palette.abyssStone);
                AddPreferredResource(
                    deepDetail,
                    "Rock_Medium_2",
                    new Vector3(-20f, 0.4f, 11f),
                    Vector3.one * 1.8f,
                    Quaternion.Euler(0f, 93f, 0f),
                    PrimitiveType.Sphere,
                    new Vector3(-20f, 0.7f, 11f),
                    new Vector3(2.2f, 1.2f, 2.5f),
                    palette.abyssStone);
                AddPreferredResource(
                    deepDetail,
                    "Rock_Medium_3",
                    new Vector3(-35f, 0.5f, 27f),
                    Vector3.one * 1.9f,
                    Quaternion.Euler(0f, 152f, 0f),
                    PrimitiveType.Sphere,
                    new Vector3(-35f, 0.7f, 27f),
                    new Vector3(2.4f, 1.3f, 2.1f),
                    palette.abyssStone);
            }

            private void BuildCurrentGallery()
            {
                var directions = new[]
                {
                    new Vector3(0.7f, 0.18f, -0.3f),
                    new Vector3(-0.55f, 0.12f, -0.52f),
                    new Vector3(0.2f, 0.32f, 0.68f)
                };
                var centers = new[]
                {
                    new Vector3(23f, 4f, 23f),
                    new Vector3(31f, 5.5f, 18f),
                    new Vector3(35f, 7f, 12f)
                };
                for (var index = 0; index < centers.Length; index++)
                {
                    AddVisual(
                        deepDetail,
                        PrimitiveType.Cube,
                        $"Visible current lane {index + 1}",
                        centers[index],
                        new Vector3(5.2f, 1.1f, 12f),
                        Quaternion.Euler(0f, index * 31f - 20f, 0f),
                        palette.current);
                    AddVisual(
                        deepDetail,
                        PrimitiveType.Sphere,
                        $"Current lane heading pearl {index + 1}",
                        centers[index] + directions[index].normalized * 5f,
                        new Vector3(0.38f, 0.38f, 0.75f),
                        Quaternion.LookRotation(directions[index]),
                        palette.glow);
                    AddCurrentTrigger(
                        $"current.pelagic_court.gallery_{index + 1}",
                        centers[index],
                        new Vector3(5.2f, 2.2f, 12f),
                        Quaternion.Euler(0f, index * 31f - 20f, 0f),
                        directions[index],
                        0.9f + index * 0.15f);
                }
            }

            private void BuildMarket()
            {
                AddPreferredResource(
                    deepDetail,
                    "Stall_Empty",
                    new Vector3(-30f, 2.2f, -7f),
                    Vector3.one * 1.5f,
                    Quaternion.Euler(0f, 35f, 0f),
                    PrimitiveType.Cube,
                    new Vector3(-30f, 2.1f, -7f),
                    new Vector3(4.5f, 2.8f, 2.4f),
                    palette.shell);
                AddPreferredResource(
                    deepDetail,
                    "Stall_Empty",
                    new Vector3(-20f, 2.2f, -8f),
                    Vector3.one * 1.45f,
                    Quaternion.Euler(0f, -28f, 0f),
                    PrimitiveType.Cube,
                    new Vector3(-20f, 2.1f, -8f),
                    new Vector3(4.4f, 2.8f, 2.4f),
                    palette.shell);
                AddPreferredResource(
                    deepDetail,
                    "Workbench",
                    new Vector3(-20f, 2.1f, -3f),
                    Vector3.one * 1.4f,
                    Quaternion.Euler(0f, 180f, 0f),
                    PrimitiveType.Cube,
                    new Vector3(-20f, 2.1f, -3f),
                    new Vector3(3.2f, 1.2f, 1.4f),
                    palette.nacre);
                for (var index = 0; index < 5; index++)
                {
                    AddVisual(
                        deepDetail,
                        PrimitiveType.Sphere,
                        $"Nacre market trade pearl {index + 1}",
                        new Vector3(-31f + index * 3.2f, 3.4f + index % 2 * 0.35f, -12f),
                        Vector3.one * (0.42f + index % 2 * 0.12f),
                        Quaternion.identity,
                        index % 2 == 0 ? palette.pearlGlow : palette.coralGold);
                }
            }

            private void BuildThroneDistrict()
            {
                AddVisual(
                    deepStructural,
                    PrimitiveType.Cylinder,
                    "Pearl Throne stepped dais",
                    new Vector3(0f, 3.4f, -29f),
                    new Vector3(7.5f, 0.9f, 7.5f),
                    Quaternion.identity,
                    palette.nacre);
                AddVisual(
                    deepDetail,
                    PrimitiveType.Cube,
                    "Pearl Throne seat",
                    new Vector3(0f, 5.2f, -31f),
                    new Vector3(3.2f, 2.8f, 2.4f),
                    Quaternion.identity,
                    palette.shell);
                AddVisual(
                    deepDetail,
                    PrimitiveType.Sphere,
                    "Pearl Throne crown",
                    new Vector3(0f, 7.4f, -31f),
                    new Vector3(2.8f, 2.1f, 1.2f),
                    Quaternion.identity,
                    palette.pearlGlow);
                for (var side = -1; side <= 1; side += 2)
                {
                    AddVisual(
                        deepDetail,
                        PrimitiveType.Capsule,
                        $"Throne district coral standard {side}",
                        new Vector3(side * 6f, 5.2f, -28f),
                        new Vector3(0.65f, 3.4f, 0.65f),
                        Quaternion.Euler(0f, 0f, side * 9f),
                        palette.coral);
                    AddVisual(
                        deepDetail,
                        PrimitiveType.Sphere,
                        $"Throne district oath pearl {side}",
                        new Vector3(side * 6f, 8.7f, -28f),
                        Vector3.one * 0.62f,
                        Quaternion.identity,
                        palette.glow);
                }
            }

            private void BuildArenaAndBossSanctum()
            {
                AddVisual(
                    deepStructural,
                    PrimitiveType.Cylinder,
                    "Reefguard combat arena floor",
                    new Vector3(-33f, 0.7f, -49f),
                    new Vector3(14f, 0.32f, 14f),
                    Quaternion.identity,
                    palette.abyssStone);
                AddCollider(
                    "collider.pelagic_court.reef_arena_floor",
                    new Vector3(-33f, 0.65f, -49f),
                    new Vector3(25f, 0.75f, 25f),
                    Quaternion.identity,
                    false);
                for (var index = 0; index < 8; index++)
                {
                    var angle = index * Mathf.PI * 2f / 8f;
                    AddVisual(
                        deepDetail,
                        PrimitiveType.Capsule,
                        $"Reefguard arena coral post {index + 1}",
                        new Vector3(
                            -33f + Mathf.Cos(angle) * 13f,
                            3.2f,
                            -49f + Mathf.Sin(angle) * 13f),
                        new Vector3(0.55f, 2.7f, 0.55f),
                        Quaternion.Euler(0f, index * 45f, index % 2 == 0 ? 8f : -8f),
                        index % 2 == 0 ? palette.coral : palette.coralGold);
                }

                AddVisual(
                    deepStructural,
                    PrimitiveType.Cylinder,
                    "Abyssal Crown boss dais",
                    new Vector3(30f, 2.3f, -50f),
                    new Vector3(10f, 0.45f, 10f),
                    Quaternion.identity,
                    palette.nacre);
                AddCollider(
                    "collider.pelagic_court.abyssal_sanctum_floor",
                    new Vector3(30f, 2.15f, -50f),
                    new Vector3(18f, 0.8f, 18f),
                    Quaternion.identity,
                    false);
                for (var index = 0; index < 6; index++)
                {
                    var angle = index * Mathf.PI * 2f / 6f;
                    AddVisual(
                        deepDetail,
                        PrimitiveType.Cube,
                        $"Abyssal sanctum crown rib {index + 1}",
                        new Vector3(
                            30f + Mathf.Cos(angle) * 10f,
                            7f,
                            -50f + Mathf.Sin(angle) * 10f),
                        new Vector3(0.55f, 7f, 0.55f),
                        Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 18f),
                        palette.shell);
                }

                AddVisual(
                    deepDetail,
                    PrimitiveType.Sphere,
                    "Leviathan Heir pressure heart",
                    new Vector3(30f, 8f, -50f),
                    Vector3.one * 2.2f,
                    Quaternion.identity,
                    palette.warning);
            }

            private void BuildSocketMarkers()
            {
                for (var index = 0; index < SocketDefinitions.Length; index++)
                {
                    var socket = SocketDefinitions[index];
                    var material = socket.kind == TetherfallPelagicSocketKind.EmergencyAscent
                        ? palette.warning
                        : socket.kind == TetherfallPelagicSocketKind.Boss
                            ? palette.coral
                            : palette.glow;
                    AddVisual(
                        deepDetail,
                        PrimitiveType.Cylinder,
                        socket.id + ".plinth",
                        socket.localPosition + Vector3.down * 0.8f,
                        new Vector3(0.9f, 0.5f, 0.9f),
                        Quaternion.identity,
                        palette.shell);
                    AddVisual(
                        deepDetail,
                        PrimitiveType.Sphere,
                        socket.id + ".signal",
                        socket.localPosition + Vector3.up * 0.45f,
                        Vector3.one * 0.48f,
                        Quaternion.identity,
                        material);
                    AddLabel(
                        deepDetail,
                        socket.displayName,
                        socket.localPosition + new Vector3(0f, 1.7f, 0f),
                        new Color(0.7f, 0.94f, 0.91f),
                        0.046f);
                }
            }

            private void BuildWaterVolume()
            {
                var volume = WaterVolume.Create(
                    CityCenter + PressureLocalBounds.center,
                    PressureLocalBounds.size);
                volume.name = "volume.pelagic_court.swimmable_city";
                volume.transform.SetParent(deepGameplay, true);
            }

            private void AddDomeLightPearls(
                Vector3 center,
                float radius,
                int count)
            {
                for (var index = 0; index < count; index++)
                {
                    var angle = index * Mathf.PI * 2f / Math.Max(1, count);
                    AddVisual(
                        deepDetail,
                        PrimitiveType.Sphere,
                        $"Bubble-light pearl {center.x:0}_{center.z:0}_{index + 1}",
                        center + new Vector3(
                            Mathf.Cos(angle) * radius,
                            0f,
                            Mathf.Sin(angle) * radius),
                        Vector3.one * 0.35f,
                        Quaternion.identity,
                        palette.glow);
                }
            }

            private GameObject AddVisual(
                Transform parent,
                PrimitiveType primitiveType,
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                Material material)
            {
                var visual = new GameObject(name);
                visual.transform.SetParent(parent, false);
                visual.transform.localPosition = SanitizePosition(localPosition);
                visual.transform.localScale = SanitizeScale(localScale);
                visual.transform.localRotation = IsFinite(localRotation)
                    ? localRotation
                    : Quaternion.identity;
                var filter = visual.AddComponent<MeshFilter>();
                filter.sharedMesh = GetPrimitiveMesh(primitiveType);
                var renderer = visual.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                ConfigureRenderer(renderer);
                return visual;
            }

            private TextMesh AddLabel(
                Transform parent,
                string text,
                Vector3 localPosition,
                Color color,
                float characterSize)
            {
                var labelObject = new GameObject("Static label · " + text);
                labelObject.transform.SetParent(parent, false);
                labelObject.transform.localPosition = SanitizePosition(localPosition);
                labelObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var label = labelObject.AddComponent<TextMesh>();
                label.text = text ?? string.Empty;
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.fontSize = 48;
                label.characterSize = Math.Max(0.01f, characterSize);
                label.color = color;
                ConfigureRenderer(label.GetComponent<Renderer>());
                return label;
            }

            private void AddCollider(
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                bool isTrigger)
            {
                var colliderObject = new GameObject(name);
                colliderObject.transform.SetParent(deepGameplay, false);
                colliderObject.transform.localPosition = SanitizePosition(localPosition);
                colliderObject.transform.localScale = SanitizeScale(localScale);
                colliderObject.transform.localRotation = IsFinite(localRotation)
                    ? localRotation
                    : Quaternion.identity;
                var collider = colliderObject.AddComponent<BoxCollider>();
                collider.size = Vector3.one;
                collider.isTrigger = isTrigger;
                colliderObject.AddComponent<TetherfallPelagicGameplayCollider>().Configure(name);
            }

            private void AddCurrentTrigger(
                string stableId,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                Vector3 direction,
                float strength)
            {
                var currentObject = new GameObject(stableId);
                currentObject.transform.SetParent(deepGameplay, false);
                currentObject.transform.localPosition = SanitizePosition(localPosition);
                currentObject.transform.localScale = SanitizeScale(localScale);
                currentObject.transform.localRotation = IsFinite(localRotation)
                    ? localRotation
                    : Quaternion.identity;
                var collider = currentObject.AddComponent<BoxCollider>();
                collider.size = Vector3.one;
                collider.isTrigger = true;
                currentObject.AddComponent<TetherfallPelagicGameplayCollider>().Configure(stableId);
                currentObject.AddComponent<TetherfallPelagicCurrentVolume>().Initialize(
                    direction,
                    strength);
            }

            private void AddPreferredResource(
                Transform parent,
                string resourceName,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                PrimitiveType fallbackType,
                Vector3 fallbackPosition,
                Vector3 fallbackScale,
                Material material)
            {
                resourcePlacementCount++;
                if (!forceFallback)
                {
                    var prefab = Resources.Load<GameObject>(ResourcePrefix + resourceName);
                    if (IsSafeResource(prefab))
                    {
                        var instance = Object.Instantiate(prefab, parent, false);
                        instance.name = "Existing asset · " + resourceName;
                        instance.transform.localPosition = SanitizePosition(localPosition);
                        instance.transform.localScale = SanitizeScale(localScale);
                        instance.transform.localRotation = IsFinite(localRotation)
                            ? localRotation
                            : Quaternion.identity;
                        var renderers = instance.GetComponentsInChildren<Renderer>(true);
                        for (var rendererIndex = 0;
                             rendererIndex < renderers.Length;
                             rendererIndex++)
                        {
                            var renderer = renderers[rendererIndex];
                            var shared = renderer.sharedMaterials;
                            for (var materialIndex = 0;
                                 materialIndex < shared.Length;
                                 materialIndex++)
                            {
                                shared[materialIndex] = material;
                            }

                            renderer.sharedMaterials = shared;
                            ConfigureRenderer(renderer);
                        }

                        preferredResourceCount++;
                        return;
                    }
                }

                fallbackCount++;
                AddVisual(
                    parent,
                    fallbackType,
                    "Procedural fallback · " + resourceName,
                    fallbackPosition,
                    fallbackScale,
                    localRotation,
                    material);
            }

            private static bool IsSafeResource(GameObject prefab)
            {
                if (prefab == null ||
                    prefab.GetComponentsInChildren<Renderer>(true).Length == 0 ||
                    prefab.GetComponentsInChildren<Renderer>(true).Length > 3 ||
                    prefab.GetComponentsInChildren<Collider>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<Light>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<Camera>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<ParticleSystem>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<Animator>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<LODGroup>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<MonoBehaviour>(true).Length > 0)
                {
                    return false;
                }

                return true;
            }

            private static BuildPalette CreatePalette(TetherfallGame game)
            {
                return new BuildPalette
                {
                    abyssStone = RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.035f, 0.09f, 0.13f),
                        0.08f,
                        0.25f),
                    shell = RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.42f, 0.58f, 0.58f),
                        0.14f,
                        0.58f),
                    coral = RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.78f, 0.28f, 0.42f),
                        0.02f,
                        0.42f),
                    coralGold = RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.86f, 0.52f, 0.24f),
                        0.03f,
                        0.4f),
                    nacre = RuntimeEffectMaterialCache.GetOpaque(
                        game,
                        new Color(0.7f, 0.82f, 0.78f),
                        0.24f,
                        0.82f),
                    bubble = RuntimeEffectMaterialCache.GetTransparent(
                        game,
                        new Color(0.3f, 0.83f, 0.88f, 0.18f),
                        0.9f),
                    current = RuntimeEffectMaterialCache.GetTransparent(
                        game,
                        new Color(0.18f, 0.7f, 0.82f, 0.27f),
                        0.72f),
                    glow = RuntimeEffectMaterialCache.GetGlow(
                        game,
                        new Color(0.24f, 0.92f, 0.84f),
                        1.45f,
                        0.68f),
                    pearlGlow = RuntimeEffectMaterialCache.GetGlow(
                        game,
                        new Color(0.74f, 0.88f, 1f),
                        1.32f,
                        0.78f),
                    warning = RuntimeEffectMaterialCache.GetGlow(
                        game,
                        new Color(0.86f, 0.28f, 0.32f),
                        1.18f,
                        0.58f)
                };
            }

            private static void ConfigureRenderer(Renderer renderer)
            {
                if (renderer == null)
                {
                    return;
                }

                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.motionVectorGenerationMode =
                    MotionVectorGenerationMode.ForceNoMotion;
                renderer.allowOcclusionWhenDynamic = true;
            }

            private static Vector3 SanitizePosition(Vector3 value)
            {
                return IsFinite(value) ? value : Vector3.zero;
            }

            private static Vector3 SanitizeScale(Vector3 value)
            {
                if (!IsFinite(value))
                {
                    return Vector3.one;
                }

                return new Vector3(
                    Math.Max(0.001f, Math.Abs(value.x)),
                    Math.Max(0.001f, Math.Abs(value.y)),
                    Math.Max(0.001f, Math.Abs(value.z)));
            }

            private static bool IsFinite(Vector3 value)
            {
                return TetherfallPelagicCourtRules.IsFinite(value.x) &&
                       TetherfallPelagicCourtRules.IsFinite(value.y) &&
                       TetherfallPelagicCourtRules.IsFinite(value.z);
            }

            private static bool IsFinite(Quaternion value)
            {
                return TetherfallPelagicCourtRules.IsFinite(value.x) &&
                       TetherfallPelagicCourtRules.IsFinite(value.y) &&
                       TetherfallPelagicCourtRules.IsFinite(value.z) &&
                       TetherfallPelagicCourtRules.IsFinite(value.w);
            }
        }
    }

    public sealed class TetherfallPelagicCourtRoot : MonoBehaviour
    {
        public Transform VisualRoot { get; private set; }
        public Transform GameplayRoot { get; private set; }
        public Transform InteractionRoot { get; private set; }
        public TetherfallPelagicCourtRuntime Runtime { get; private set; }
        public TetherfallPelagicPressure Pressure { get; private set; }
        public bool ForcedFallback { get; private set; }
        public int RendererCount { get; private set; }
        public int ColliderCount { get; private set; }
        public int LightCount { get; private set; }
        public int SharedMaterialCount { get; private set; }
        public int ResourcePlacementCount { get; private set; }
        public int PreferredResourceCount { get; private set; }
        public int FallbackCount { get; private set; }
        public int UpdateDriverCount => TetherfallPelagicCourt.UpdateDriverCount;

        internal void Configure(
            Transform visualRoot,
            Transform gameplayRoot,
            Transform interactionRoot,
            TetherfallPelagicCourtRuntime runtime,
            TetherfallPelagicPressure pressure,
            bool forcedFallback,
            int rendererCount,
            int colliderCount,
            int lightCount,
            int sharedMaterialCount,
            int resourcePlacementCount,
            int preferredResourceCount,
            int fallbackCount)
        {
            VisualRoot = visualRoot;
            GameplayRoot = gameplayRoot;
            InteractionRoot = interactionRoot;
            Runtime = runtime;
            Pressure = pressure;
            ForcedFallback = forcedFallback;
            RendererCount = Math.Max(0, rendererCount);
            ColliderCount = Math.Max(0, colliderCount);
            LightCount = Math.Max(0, lightCount);
            SharedMaterialCount = Math.Max(0, sharedMaterialCount);
            ResourcePlacementCount = Math.Max(0, resourcePlacementCount);
            PreferredResourceCount = Math.Max(0, preferredResourceCount);
            FallbackCount = Math.Max(0, fallbackCount);
        }
    }

    public sealed class TetherfallPelagicCourtRuntime : MonoBehaviour
    {
        private const float ArrivalPrimeSeconds = 2f;

        private TetherfallGame game;
        private TetherfallPelagicCourtRoot marker;
        private TetherfallPelagicPressure pressure;
        private GameObject structuralRoot;
        private GameObject detailRoot;
        private GameObject gameplayRoot;
        private GameObject interactionRoot;
        private Renderer gateSealRenderer;
        private TextMesh gateLabel;
        private TetherfallPelagicCourtStateData state =
            new TetherfallPelagicCourtStateData();
        private TetherfallPelagicGateStatus lastGateStatus =
            (TetherfallPelagicGateStatus)(-1);
        private float nextActivationRefreshAt;
        private float arrivalPrimeUntil;

        public event Action<TetherfallPelagicSocketDefinition> SocketActivated;

        public TetherfallPelagicCourtStateData State => state;
        public TetherfallPelagicCourtRoot Marker => marker;
        public TetherfallPelagicPressure Pressure => pressure;
        public TetherfallPelagicGateDecision GateDecision =>
            TetherfallPelagicCourtRules.ResolveGate(
                new TetherfallPelagicGateFacts(
                    true,
                    state.bossRewardObserved,
                    game != null && game.HasTideglassCrown));
        public int CompletedProgressionNodeCount =>
            state.completedProgressionNodeCount;

        internal void Initialize(
            TetherfallGame owner,
            TetherfallPelagicCourtRoot rootMarker,
            GameObject structural,
            GameObject detail,
            GameObject gameplay,
            GameObject interactions,
            Renderer sealRenderer,
            TextMesh sealLabel,
            TetherfallPelagicPressure pressureComponent)
        {
            game = owner;
            marker = rootMarker;
            structuralRoot = structural;
            detailRoot = detail;
            gameplayRoot = gameplay;
            interactionRoot = interactions;
            gateSealRenderer = sealRenderer;
            gateLabel = sealLabel;
            pressure = pressureComponent;
            state = TetherfallPelagicCourtRules.NormalizeState(
                state,
                TetherfallPelagicCourt.Progression.Count);
            SetDeepActivation(false, false);
            RefreshGatePresentation(true);
        }

        private void Update()
        {
            if (game == null ||
                Time.unscaledTime < nextActivationRefreshAt)
            {
                return;
            }

            nextActivationRefreshAt =
                Time.unscaledTime + TetherfallPelagicCourt.ActivationRefreshSeconds;
            ReconcileExternalProgress();
            var player = game.PlayerTransform;
            if (player == null)
            {
                SetDeepActivation(false, false);
                return;
            }

            RefreshActivation(player.position);
        }

        public void ReconcileExternalProgress()
        {
            if (game == null)
            {
                return;
            }

            var rewardResolved =
                state.bossRewardObserved ||
                game.IsGlasswaterRegentRewardResolvedForPelagicCourt() ||
                game.HasTideglassCrown;
            var ledger = new TetherfallPelagicRewardLedger
            {
                bossRewardObserved = state.bossRewardObserved,
                relicGranted = state.relicGranted,
                relicGrantCount = state.relicGrantCount
            };
            if (rewardResolved &&
                !game.HasTideglassCrown &&
                ledger.relicGranted)
            {
                // Recover a missing equipment-ledger entry without ever incrementing
                // the authored one-shot reward count beyond one.
                ledger.relicGranted = false;
                ledger.relicGrantCount = 0;
            }

            var shouldGrant = TetherfallPelagicCourtRules.TryResolveRelicReward(
                ref ledger,
                TetherfallPelagicCourt.KeyBossId,
                rewardResolved,
                game.HasTideglassCrown);
            if (shouldGrant)
            {
                game.TryGrantTideglassCrown();
            }

            state.bossRewardObserved = ledger.bossRewardObserved;
            state.relicGranted = game.HasTideglassCrown && ledger.relicGranted;
            state.relicGrantCount = state.relicGranted ? 1 : 0;
            if (state.bossRewardObserved)
            {
                TryAdvanceProgression(
                    "progress.pelagic_court.glasswater_regent_defeated");
            }

            if (state.relicGranted)
            {
                TryAdvanceProgression(
                    "progress.pelagic_court.tideglass_crown_attuned");
            }

            RefreshGatePresentation(false);
        }

        public bool ObserveBossReward(string bossId)
        {
            if (!string.Equals(
                    bossId,
                    TetherfallPelagicCourt.KeyBossId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            var wasObserved = state.bossRewardObserved;
            var relicWasOwned = game != null && game.HasTideglassCrown;
            state.bossRewardObserved = true;
            ReconcileExternalProgress();
            return !wasObserved || !relicWasOwned && state.relicGranted;
        }

        public bool TryEnter()
        {
            var decision = GateDecision;
            if (!decision.CanEnter ||
                game == null ||
                game.Player == null)
            {
                game?.Notify(decision.RequirementText);
                return false;
            }

            PrimeForArrival();
            game.Player.Teleport(
                TetherfallPelagicCourt.CityCenter +
                TetherfallPelagicCourt.CityArrivalLocalPosition);
            TryAdvanceProgression(
                "progress.pelagic_court.kelp_gate_opened");
            game.Notify(
                "The Tideglass Crown seals a breathing current. Nacre Court opens below the Morrowfen channel.");
            return true;
        }

        public bool TryActivateSocket(string socketId)
        {
            var socket = TetherfallPelagicCourt.GetSocket(socketId);
            if (socket == null || game == null)
            {
                return false;
            }

            if (!GateDecision.CanEnter &&
                socket.kind != TetherfallPelagicSocketKind.EmergencyAscent)
            {
                game.Notify(GateDecision.RequirementText);
                return false;
            }

            switch (socket.kind)
            {
                case TetherfallPelagicSocketKind.ThreadwayDock:
                    TryAdvanceProgression(
                        "progress.pelagic_court.threadway_dock_charted");
                    if (SocketActivated != null)
                    {
                        SocketActivated(socket);
                    }
                    else
                    {
                        game.Notify(
                            "Undertow Dock charted. Its Threadway socket remains local until relic-gated travel filtering is connected.");
                    }

                    break;
                case TetherfallPelagicSocketKind.EmergencyAscent:
                    ReturnToSurface(
                        "The ascent bell returns you to the Morrowfen bank.");
                    break;
                default:
                    if (SocketActivated != null)
                    {
                        SocketActivated(socket);
                    }
                    else
                    {
                        game.Notify(
                            socket.displayName + " is staged as an explicit integration socket. " +
                            socket.integrationHint);
                    }

                    break;
            }

            return true;
        }

        public bool TryAdvanceProgression(string progressionNodeId)
        {
            if (!TetherfallPelagicCourtRules.CanAdvanceProgression(
                    TetherfallPelagicCourt.Progression,
                    state.completedProgressionNodeCount,
                    progressionNodeId))
            {
                return false;
            }

            state.completedProgressionNodeCount++;
            return true;
        }

        public bool ReportQuestCompleted(string socketId)
        {
            return string.Equals(
                       socketId,
                       TetherfallPelagicCourt.ThroneQuestSocketId,
                       StringComparison.Ordinal) &&
                   TryAdvanceProgression(
                       "progress.pelagic_court.pearl_audience_completed");
        }

        public bool ReportActivityCompleted(string socketId)
        {
            if (string.Equals(
                    socketId,
                    TetherfallPelagicCourt.CurrentTrialSocketId,
                    StringComparison.Ordinal))
            {
                return TryAdvanceProgression(
                    "progress.pelagic_court.current_trial_completed");
            }

            if (string.Equals(
                    socketId,
                    TetherfallPelagicCourt.ArenaSocketId,
                    StringComparison.Ordinal))
            {
                return TryAdvanceProgression(
                    "progress.pelagic_court.reef_arena_cleared");
            }

            return false;
        }

        public bool ReportBossDefeated(string bossId)
        {
            return string.Equals(
                       bossId,
                       TetherfallPelagicCourt.PelagicBossId,
                       StringComparison.Ordinal) &&
                   TryAdvanceProgression(
                       "progress.pelagic_court.leviathan_heir_defeated");
        }

        public TetherfallPelagicCourtStateData CaptureState()
        {
            if (pressure != null)
            {
                state.oxygenRemainingSeconds = pressure.OxygenRemainingSeconds;
                state.evacuationCount = pressure.EvacuationCount;
            }

            state = TetherfallPelagicCourtRules.NormalizeState(
                state,
                TetherfallPelagicCourt.Progression.Count);
            return new TetherfallPelagicCourtStateData
            {
                version = state.version,
                completedProgressionNodeCount = state.completedProgressionNodeCount,
                bossRewardObserved = state.bossRewardObserved,
                relicGranted = state.relicGranted,
                relicGrantCount = state.relicGrantCount,
                oxygenRemainingSeconds = state.oxygenRemainingSeconds,
                evacuationCount = state.evacuationCount
            };
        }

        public void RestoreState(TetherfallPelagicCourtStateData saved)
        {
            state = TetherfallPelagicCourtRules.NormalizeState(
                saved == null
                    ? new TetherfallPelagicCourtStateData()
                    : new TetherfallPelagicCourtStateData
                    {
                        version = saved.version,
                        completedProgressionNodeCount =
                            saved.completedProgressionNodeCount,
                        bossRewardObserved = saved.bossRewardObserved,
                        relicGranted = saved.relicGranted,
                        relicGrantCount = saved.relicGrantCount,
                        oxygenRemainingSeconds = saved.oxygenRemainingSeconds,
                        evacuationCount = saved.evacuationCount
                    },
                TetherfallPelagicCourt.Progression.Count);
            pressure?.RestoreState(
                new TetherfallPelagicOxygenState
                {
                    remainingSeconds = state.oxygenRemainingSeconds,
                    evacuationCount = state.evacuationCount
                });
            if (game?.Player != null &&
                TetherfallPelagicPressure.ContainsPressure(
                    game.Player.transform.position))
            {
                PrimeForArrival();
            }

            ReconcileExternalProgress();
        }

        public void PrimeForArrival()
        {
            arrivalPrimeUntil = Time.unscaledTime + ArrivalPrimeSeconds;
            SetDeepActivation(true, true);
        }

        public void ReturnToSurface(string message)
        {
            if (pressure != null)
            {
                pressure.RequestSafeEvacuation(
                    TetherfallPelagicEvacuationReason.ManualAscent,
                    message);
                return;
            }

            if (game?.Player != null)
            {
                game.Player.Teleport(TetherfallPelagicCourt.SurfaceEvacuationPosition);
                game.Notify(message);
            }
        }

        internal void OnPressureStateChanged(
            TetherfallPelagicOxygenState oxygenState)
        {
            state.oxygenRemainingSeconds = oxygenState.remainingSeconds;
            state.evacuationCount = oxygenState.evacuationCount;
        }

        private void RefreshActivation(Vector3 playerPosition)
        {
            var offset = playerPosition - TetherfallPelagicCourt.CityCenter;
            var horizontalDistanceSquared =
                offset.x * offset.x + offset.z * offset.z;
            var depthEligible =
                playerPosition.y <= TetherfallPelagicCourt.DeepActivationCeiling;
            var primed = Time.unscaledTime <= arrivalPrimeUntil;
            var structuralActive =
                primed ||
                depthEligible &&
                horizontalDistanceSquared <=
                TetherfallPelagicCourt.StructuralActivationDistance *
                TetherfallPelagicCourt.StructuralActivationDistance;
            var detailActive =
                primed ||
                depthEligible &&
                horizontalDistanceSquared <=
                TetherfallPelagicCourt.DetailActivationDistance *
                TetherfallPelagicCourt.DetailActivationDistance;
            SetDeepActivation(structuralActive, detailActive);
        }

        private void SetDeepActivation(
            bool structuralActive,
            bool detailActive)
        {
            SetActive(structuralRoot, structuralActive);
            SetActive(gameplayRoot, structuralActive);
            SetActive(detailRoot, detailActive);
            SetActive(interactionRoot, detailActive);
        }

        private void RefreshGatePresentation(bool force)
        {
            var decision = GateDecision;
            if (!force && decision.Status == lastGateStatus)
            {
                return;
            }

            lastGateStatus = decision.Status;
            if (gateSealRenderer != null && game != null)
            {
                var color = decision.CanEnter
                    ? new Color(0.24f, 0.9f, 0.78f)
                    : new Color(0.82f, 0.25f, 0.3f);
                gateSealRenderer.sharedMaterial = RuntimeEffectMaterialCache.GetGlow(
                    game,
                    color,
                    decision.CanEnter ? 1.4f : 1.12f,
                    0.62f);
            }

            if (gateLabel != null)
            {
                gateLabel.text = decision.CanEnter
                    ? "NACRE COURT\nOPEN · TIDEGLASS CROWN ATTUNED"
                    : decision.Status ==
                      TetherfallPelagicGateStatus.SealedByGlasswaterRegent
                        ? "NACRE COURT\nSEALED · DEFEAT GLASSWATER REGENT"
                        : "NACRE COURT\nSEALED · ATTUNE TIDEGLASS CROWN";
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }

    public sealed class TetherfallPelagicPressure : MonoBehaviour
    {
        private TetherfallGame game;
        private TetherfallPelagicCourtRuntime runtime;
        private PlayerAvatar player;
        private TetherfallPelagicOxygenState oxygenState =
            TetherfallPelagicCourtRules.CreateFullOxygenState();
        private bool wasUnderPressure;
        private bool evacuationLatched;

        public float OxygenRemainingSeconds => oxygenState.remainingSeconds;
        public float NormalizedOxygen =>
            TetherfallPelagicCourtRules.MaximumOxygenSeconds > 0f
                ? oxygenState.remainingSeconds /
                  TetherfallPelagicCourtRules.MaximumOxygenSeconds
                : 1f;
        public int EvacuationCount => oxygenState.evacuationCount;
        public TetherfallPelagicEvacuationReason LastEvacuationReason
        {
            get;
            private set;
        }
        public bool IsUnderPressure { get; private set; }
        public bool HasInfiniteBreathing =>
            game != null && game.HasTideglassCrown;

        internal void Initialize(
            TetherfallGame owner,
            TetherfallPelagicCourtRuntime courtRuntime)
        {
            game = owner;
            runtime = courtRuntime;
            player = game != null ? game.Player : null;
            oxygenState = TetherfallPelagicCourtRules.NormalizeOxygen(oxygenState);
        }

        private void Update()
        {
            if (game == null || game.GameplayPaused)
            {
                return;
            }

            if (player == null)
            {
                player = game.Player;
                if (player == null)
                {
                    return;
                }
            }

            IsUnderPressure = ContainsPressure(player.transform.position);
            var step = TetherfallPelagicCourtRules.StepOxygen(
                oxygenState,
                Time.deltaTime,
                IsUnderPressure,
                HasInfiniteBreathing);
            oxygenState = step.State;
            runtime?.OnPressureStateChanged(oxygenState);

            if (IsUnderPressure && !wasUnderPressure && !HasInfiniteBreathing)
            {
                game.Notify(
                    "Abyssal pressure is consuming a bounded air reserve. Reach the ascent bell before it empties.");
            }

            wasUnderPressure = IsUnderPressure;
            if (!IsUnderPressure || oxygenState.remainingSeconds > 1f)
            {
                evacuationLatched = false;
            }

            if (step.ShouldEvacuate && !evacuationLatched)
            {
                RequestSafeEvacuation(
                    step.EvacuationReason,
                    "The air reserve collapses. Tideglass evacuates you to the Morrowfen bank.");
            }
        }

        public void BindPlayerAvatar(PlayerAvatar avatar)
        {
            player = avatar;
        }

        public TetherfallPelagicBreathStep SimulateBoundedStep(
            float deltaSeconds,
            bool underPressure,
            bool infiniteBreathing)
        {
            return TetherfallPelagicCourtRules.StepOxygen(
                oxygenState,
                deltaSeconds,
                underPressure,
                infiniteBreathing);
        }

        public void RestoreState(TetherfallPelagicOxygenState restored)
        {
            oxygenState = TetherfallPelagicCourtRules.NormalizeOxygen(restored);
            evacuationLatched = false;
            runtime?.OnPressureStateChanged(oxygenState);
        }

        public void RequestSafeEvacuation(
            TetherfallPelagicEvacuationReason reason,
            string message)
        {
            if (player == null && game != null)
            {
                player = game.Player;
            }

            if (player == null)
            {
                return;
            }

            evacuationLatched = true;
            LastEvacuationReason = reason;
            if (oxygenState.evacuationCount < int.MaxValue)
            {
                oxygenState.evacuationCount++;
            }

            oxygenState.remainingSeconds =
                TetherfallPelagicCourtRules.MaximumOxygenSeconds;
            player.Teleport(TetherfallPelagicCourt.SurfaceEvacuationPosition);
            runtime?.OnPressureStateChanged(oxygenState);
            if (game != null && !string.IsNullOrWhiteSpace(message))
            {
                game.Notify(message);
            }
        }

        public static bool ContainsPressure(Vector3 worldPosition)
        {
            var local = worldPosition - TetherfallPelagicCourt.CityCenter;
            return TetherfallPelagicCourt.PressureLocalBounds.Contains(local);
        }
    }

    public sealed class TetherfallPelagicEntrance : MonoBehaviour, IWorldInteractable
    {
        private TetherfallPelagicCourtRuntime runtime;

        public string InteractionPrompt =>
            runtime != null
                ? runtime.GateDecision.InteractionPrompt
                : TetherfallPelagicCourtRules.UnavailableRequirement;

        internal void Initialize(TetherfallPelagicCourtRuntime courtRuntime)
        {
            runtime = courtRuntime;
        }

        public bool CanInteract(TetherfallGame game)
        {
            return game != null && game.Player != null;
        }

        public void Interact(TetherfallGame game)
        {
            runtime?.TryEnter();
        }
    }

    public sealed class TetherfallPelagicSocket : MonoBehaviour, IWorldInteractable
    {
        private TetherfallPelagicCourtRuntime runtime;
        private TetherfallPelagicSocketDefinition definition;

        public TetherfallPelagicSocketDefinition Definition => definition;
        public string InteractionPrompt =>
            definition != null
                ? definition.kind == TetherfallPelagicSocketKind.EmergencyAscent
                    ? "Ring the emergency ascent bell"
                    : "Use " + definition.displayName
                : "Pelagic socket unavailable";

        internal void Initialize(
            TetherfallPelagicCourtRuntime courtRuntime,
            TetherfallPelagicSocketDefinition socketDefinition)
        {
            runtime = courtRuntime;
            definition = socketDefinition;
        }

        public bool CanInteract(TetherfallGame game)
        {
            return game != null &&
                   game.Player != null &&
                   definition != null;
        }

        public void Interact(TetherfallGame game)
        {
            if (definition != null)
            {
                runtime?.TryActivateSocket(definition.id);
            }
        }
    }

    public sealed class TetherfallPelagicCurrentVolume : MonoBehaviour
    {
        private Vector3 direction;
        private float strength;

        internal void Initialize(Vector3 currentDirection, float currentStrength)
        {
            direction = currentDirection.sqrMagnitude > 0.0001f
                ? currentDirection.normalized
                : Vector3.forward;
            strength = TetherfallPelagicCourtRules.IsFinite(currentStrength)
                ? Mathf.Clamp(currentStrength, 0f, 2.5f)
                : 0f;
        }

        private void OnTriggerStay(Collider other)
        {
            var avatar = other != null ? other.GetComponent<PlayerAvatar>() : null;
            if (avatar != null && avatar.IsSwimming)
            {
                avatar.ApplyImpact(direction * (strength * Time.fixedDeltaTime));
            }
        }
    }

    public sealed class TetherfallPelagicGameplayCollider : MonoBehaviour
    {
        public string StableId { get; private set; }

        internal void Configure(string stableId)
        {
            StableId = stableId ?? string.Empty;
        }
    }
}
