using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Tetherfall
{
    /// <summary>
    /// Stable serialized identities for the reusable Infinite Spire visual kit.
    /// Values and matching spire.* strings are save-data contracts and must never be renumbered.
    /// </summary>
    public enum TetherfallSpireArtId
    {
        BasePlinth = 100,
        CityFlange = 101,
        TierDrumA = 102,
        TierDrumB = 103,
        CoilRibSet = 104,
        CrownRing = 105,
        ActiveSeam = 106,

        DistantNeedle = 120,
        DistantCrown = 121,

        FacadeStraight = 200,
        FacadeWindow = 201,
        FacadeDoorBay = 202,
        FacadeArch = 203,

        ShellStandard = 300,
        ShellCombat = 301,
        ShellPuzzle = 302,
        ShellRest = 303,

        DoorRound = 400,
        DoorSeal = 401,
        DoorRetreat = 402,

        StairStraight = 500,
        StairPlatform = 501,
        LiftCage = 502,
        LiftGuide = 503,

        CoilDial = 600,
        SeamLatch = 601,
        ResonancePlinth = 602,
        PressureTile = 603,

        MinibossRing = 700,
        BossThreshold = 701,
        BossArenaFloor = 702,

        CacheChest = 800,
        RepairBench = 801,
        WaymarkBanner = 802,

        CoilhavenCore = 900,
        CoilhavenBed = 901,
        CoilhavenHearth = 902,

        SpirecoilStall = 1000,
        SpirecoilCounter = 1001,
        SpirecoilBeacon = 1002
    }

    public enum TetherfallSpireArtBudgetClass
    {
        Structural,
        Station,
        RoomShell,
        Arena,
        HeroRestOrVendor,
        LocalExterior,
        DistantProxy
    }

    public enum TetherfallSpirePresentationClass
    {
        LocalExterior,
        DistantProxy,
        InteriorActivation,
        CoilhavenOrVendor
    }

    /// <summary>
    /// Immutable gameplay-facing contract for one visual role. Dimensions are metres.
    /// Every definition uses a ground-contact pivot, +Y up, and +Z forward.
    /// </summary>
    public sealed class TetherfallSpireArtDefinition
    {
        private readonly ReadOnlyCollection<string> requiredSockets;

        public TetherfallSpireArtId ArtId { get; }
        public string StableId { get; }
        public string DisplayName { get; }
        public TetherfallSpireArtBudgetClass BudgetClass { get; }
        public TetherfallSpirePresentationClass PresentationClass { get; }
        public Vector3 Dimensions { get; }
        public Vector2 Footprint { get; }
        public Vector3 Pivot => Vector3.zero;
        public Vector3 Forward => Vector3.forward;
        public int MaxRendererCount { get; }
        public int MaxSharedMaterialCount { get; }
        public string PreferredResourceName { get; }
        public bool HasPreferredResource => !string.IsNullOrEmpty(PreferredResourceName);
        public string PreferredResourcePath => HasPreferredResource
            ? TetherfallSpireArt.ResourcePrefix + PreferredResourceName
            : string.Empty;
        public string PreferredSourceCredit => HasPreferredResource
            ? TetherfallSpireArt.QuaterniusSourceCredit
            : TetherfallSpireArt.OriginalSourceCredit;
        public IReadOnlyList<string> RequiredSockets => requiredSockets;
        public bool UsesDistanceCulling =>
            PresentationClass != TetherfallSpirePresentationClass.InteriorActivation;
        public bool IsInteriorActivationReady =>
            PresentationClass == TetherfallSpirePresentationClass.InteriorActivation;
        public bool IsDistantProxy =>
            PresentationClass == TetherfallSpirePresentationClass.DistantProxy;
        public bool IsLocalExterior =>
            PresentationClass == TetherfallSpirePresentationClass.LocalExterior;
        public float NearDistance
        {
            get
            {
                return PresentationClass == TetherfallSpirePresentationClass.CoilhavenOrVendor
                    ? TetherfallSpirePresentationController.RestVendorNearDistance
                    : PresentationClass == TetherfallSpirePresentationClass.InteriorActivation
                        ? 0f
                        : TetherfallSpirePresentationController.ExteriorNearDistance;
            }
        }
        public float CullDistance
        {
            get
            {
                return PresentationClass == TetherfallSpirePresentationClass.CoilhavenOrVendor
                    ? TetherfallSpirePresentationController.RestVendorCullDistance
                    : PresentationClass == TetherfallSpirePresentationClass.InteriorActivation
                        ? 0f
                        : TetherfallSpirePresentationController.ExteriorCullDistance;
            }
        }
        public string CullingGroupKey
        {
            get
            {
                switch (PresentationClass)
                {
                    case TetherfallSpirePresentationClass.LocalExterior:
                        return "TetherfallSpire/ExteriorLocal";
                    case TetherfallSpirePresentationClass.DistantProxy:
                        return "TetherfallSpire/ExteriorProxy";
                    case TetherfallSpirePresentationClass.CoilhavenOrVendor:
                        return "TetherfallSpire/RestVendor";
                    default:
                        return "TetherfallSpire/InteriorActivation";
                }
            }
        }

        internal TetherfallSpireArtDefinition(
            TetherfallSpireArtId artId,
            string stableId,
            string displayName,
            TetherfallSpireArtBudgetClass budgetClass,
            TetherfallSpirePresentationClass presentationClass,
            Vector3 dimensions,
            int maxRendererCount,
            int maxSharedMaterialCount,
            string preferredResourceName,
            string[] sockets)
        {
            ArtId = artId;
            StableId = stableId;
            DisplayName = displayName;
            BudgetClass = budgetClass;
            PresentationClass = presentationClass;
            Dimensions = dimensions;
            Footprint = new Vector2(dimensions.x, dimensions.z);
            MaxRendererCount = maxRendererCount;
            MaxSharedMaterialCount = maxSharedMaterialCount;
            PreferredResourceName = preferredResourceName;
            requiredSockets = Array.AsReadOnly(
                sockets == null ? Array.Empty<string>() : (string[])sockets.Clone());
        }

        public bool RequiresSocket(string socketName)
        {
            if (string.IsNullOrEmpty(socketName))
            {
                return false;
            }

            for (var index = 0; index < requiredSockets.Count; index++)
            {
                if (string.Equals(requiredSockets[index], socketName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Single source of truth for all 40 immutable Infinite Spire visual contracts.
    /// </summary>
    public static class TetherfallSpireArtCatalog
    {
        public const int StructuralRendererBudget = 3;
        public const int StructuralMaterialBudget = 3;
        public const int StationRendererBudget = 8;
        public const int StationMaterialBudget = 6;
        public const int ShellRendererBudget = 24;
        public const int ShellMaterialBudget = 6;
        public const int ArenaRendererBudget = 32;
        public const int ArenaMaterialBudget = 6;
        public const int HeroRendererBudget = 16;
        public const int HeroMaterialBudget = 8;
        public const int LocalExteriorRendererBudget = 48;
        public const int LocalExteriorMaterialBudget = 8;
        public const int ProxyRendererBudget = 6;
        public const int ProxyMaterialBudget = 3;

        private static readonly ReadOnlyCollection<TetherfallSpireArtDefinition> Definitions =
            Array.AsReadOnly(new[]
            {
                Define(
                    TetherfallSpireArtId.BasePlinth,
                    "spire.exterior.base_plinth",
                    "Infinite Spire base plinth",
                    TetherfallSpireArtBudgetClass.LocalExterior,
                    TetherfallSpirePresentationClass.LocalExterior,
                    new Vector3(28f, 6f, 28f),
                    "Rock_Medium_2",
                    WithSnaps(TetherfallSpireArt.SocketLift)),
                Define(
                    TetherfallSpireArtId.CityFlange,
                    "spire.exterior.city_flange",
                    "Hearthmarket city flange",
                    TetherfallSpireArtBudgetClass.LocalExterior,
                    TetherfallSpirePresentationClass.LocalExterior,
                    new Vector3(32f, 4f, 32f),
                    null,
                    WithSnaps(TetherfallSpireArt.SocketLift)),
                Define(
                    TetherfallSpireArtId.TierDrumA,
                    "spire.exterior.tier_drum_a",
                    "Lower stepped civic drum",
                    TetherfallSpireArtBudgetClass.LocalExterior,
                    TetherfallSpirePresentationClass.LocalExterior,
                    new Vector3(22f, 12f, 22f),
                    null,
                    WithSnaps(TetherfallSpireArt.SocketLift, TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.TierDrumB,
                    "spire.exterior.tier_drum_b",
                    "Upper stepped civic drum",
                    TetherfallSpireArtBudgetClass.LocalExterior,
                    TetherfallSpirePresentationClass.LocalExterior,
                    new Vector3(18f, 12f, 18f),
                    null,
                    WithSnaps(TetherfallSpireArt.SocketLift, TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.CoilRibSet,
                    "spire.exterior.coil_rib_set",
                    "External copper lift ribs",
                    TetherfallSpireArtBudgetClass.LocalExterior,
                    TetherfallSpirePresentationClass.LocalExterior,
                    new Vector3(24f, 24f, 24f),
                    null,
                    WithSnaps(TetherfallSpireArt.SocketLift)),
                Define(
                    TetherfallSpireArtId.CrownRing,
                    "spire.exterior.crown_ring",
                    "Open civic crown ring",
                    TetherfallSpireArtBudgetClass.LocalExterior,
                    TetherfallSpirePresentationClass.LocalExterior,
                    new Vector3(12f, 4f, 12f),
                    null,
                    WithSnaps(TetherfallSpireArt.SocketLift, TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.ActiveSeam,
                    "spire.exterior.active_seam",
                    "Single active Threadglass seam",
                    TetherfallSpireArtBudgetClass.LocalExterior,
                    TetherfallSpirePresentationClass.LocalExterior,
                    new Vector3(20f, 0.3f, 20f),
                    null,
                    WithSnaps(TetherfallSpireArt.SocketVfx)),

                Define(
                    TetherfallSpireArtId.DistantNeedle,
                    "spire.proxy.distant_needle",
                    "Distant Spire mass proxy",
                    TetherfallSpireArtBudgetClass.DistantProxy,
                    TetherfallSpirePresentationClass.DistantProxy,
                    new Vector3(18f, 88f, 18f),
                    null,
                    NoSockets()),
                Define(
                    TetherfallSpireArtId.DistantCrown,
                    "spire.proxy.distant_crown",
                    "Distant crown proxy",
                    TetherfallSpireArtBudgetClass.DistantProxy,
                    TetherfallSpirePresentationClass.DistantProxy,
                    new Vector3(12f, 5f, 12f),
                    null,
                    NoSockets()),

                Define(
                    TetherfallSpireArtId.FacadeStraight,
                    "spire.facade.straight",
                    "Straight Spire facade",
                    TetherfallSpireArtBudgetClass.Structural,
                    TetherfallSpirePresentationClass.LocalExterior,
                    new Vector3(4f, 3f, 0.5f),
                    "Wall_Plaster_Straight",
                    WithSnaps()),
                Define(
                    TetherfallSpireArtId.FacadeWindow,
                    "spire.facade.window",
                    "Window Spire facade",
                    TetherfallSpireArtBudgetClass.Structural,
                    TetherfallSpirePresentationClass.LocalExterior,
                    new Vector3(4f, 3f, 0.5f),
                    "Wall_Plaster_Window_Wide_Round",
                    WithSnaps()),
                Define(
                    TetherfallSpireArtId.FacadeDoorBay,
                    "spire.facade.door_bay",
                    "Door-bay Spire facade",
                    TetherfallSpireArtBudgetClass.Structural,
                    TetherfallSpirePresentationClass.LocalExterior,
                    new Vector3(4f, 3f, 0.6f),
                    "Wall_UnevenBrick_Door_Round",
                    WithSnaps(TetherfallSpireArt.SocketDoor)),
                Define(
                    TetherfallSpireArtId.FacadeArch,
                    "spire.facade.arch",
                    "Civic arch facade",
                    TetherfallSpireArtBudgetClass.Structural,
                    TetherfallSpirePresentationClass.LocalExterior,
                    new Vector3(4f, 3.2f, 0.7f),
                    "Wall_Arch",
                    WithSnaps(TetherfallSpireArt.SocketDoor)),

                Define(
                    TetherfallSpireArtId.ShellStandard,
                    "spire.shell.standard",
                    "Standard Spire room shell",
                    TetherfallSpireArtBudgetClass.RoomShell,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(16f, 6f, 16f),
                    null,
                    WithSnaps(
                        TetherfallSpireArt.SocketDoor,
                        TetherfallSpireArt.SocketStairUp,
                        TetherfallSpireArt.SocketStairDown,
                        TetherfallSpireArt.SocketLift)),
                Define(
                    TetherfallSpireArtId.ShellCombat,
                    "spire.shell.combat",
                    "Combat Spire room shell",
                    TetherfallSpireArtBudgetClass.RoomShell,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(20f, 6f, 20f),
                    null,
                    WithSnaps(TetherfallSpireArt.SocketDoor, TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.ShellPuzzle,
                    "spire.shell.puzzle",
                    "Puzzle Spire room shell",
                    TetherfallSpireArtBudgetClass.RoomShell,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(16f, 6f, 16f),
                    null,
                    WithSnaps(
                        TetherfallSpireArt.SocketDoor,
                        TetherfallSpireArt.SocketPuzzle,
                        TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.ShellRest,
                    "spire.shell.rest",
                    "Coilhaven room shell",
                    TetherfallSpireArtBudgetClass.RoomShell,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(18f, 6f, 18f),
                    null,
                    WithSnaps(
                        TetherfallSpireArt.SocketDoor,
                        TetherfallSpireArt.SocketRest,
                        TetherfallSpireArt.SocketWork,
                        TetherfallSpireArt.SocketVfx)),

                Define(
                    TetherfallSpireArtId.DoorRound,
                    "spire.door.round",
                    "Round Spire door",
                    TetherfallSpireArtBudgetClass.Structural,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(4f, 3.2f, 0.6f),
                    "Wall_Plaster_Door_Round",
                    WithSnaps(TetherfallSpireArt.SocketDoor)),
                Define(
                    TetherfallSpireArtId.DoorSeal,
                    "spire.door.seal",
                    "Threadglass locking seal",
                    TetherfallSpireArtBudgetClass.Structural,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(4f, 3.2f, 0.6f),
                    null,
                    WithSnaps(TetherfallSpireArt.SocketDoor, TetherfallSpireArt.SocketBossGate)),
                Define(
                    TetherfallSpireArtId.DoorRetreat,
                    "spire.door.retreat",
                    "Amber retreat door",
                    TetherfallSpireArtBudgetClass.Structural,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(4f, 3.2f, 0.6f),
                    null,
                    WithSnaps(TetherfallSpireArt.SocketDoor, TetherfallSpireArt.SocketRest)),

                Define(
                    TetherfallSpireArtId.StairStraight,
                    "spire.stair.straight",
                    "Straight Spire stair",
                    TetherfallSpireArtBudgetClass.Structural,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(4f, 3f, 4f),
                    null,
                    WithSnaps(
                        TetherfallSpireArt.SocketStairUp,
                        TetherfallSpireArt.SocketStairDown)),
                Define(
                    TetherfallSpireArtId.StairPlatform,
                    "spire.stair.platform",
                    "Spire stair landing",
                    TetherfallSpireArtBudgetClass.Structural,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(4f, 3f, 4f),
                    null,
                    WithSnaps(
                        TetherfallSpireArt.SocketStairUp,
                        TetherfallSpireArt.SocketStairDown)),
                Define(
                    TetherfallSpireArtId.LiftCage,
                    "spire.lift.cage",
                    "Open copper lift cage",
                    TetherfallSpireArtBudgetClass.Station,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(3f, 3.6f, 3f),
                    null,
                    WithSnaps(TetherfallSpireArt.SocketLift, TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.LiftGuide,
                    "spire.lift.guide",
                    "Threadglass lift guide",
                    TetherfallSpireArtBudgetClass.Structural,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(3f, 3f, 0.5f),
                    null,
                    WithSnaps(TetherfallSpireArt.SocketLift, TetherfallSpireArt.SocketVfx)),

                Define(
                    TetherfallSpireArtId.CoilDial,
                    "spire.puzzle.coil_dial",
                    "Shape-coded coil dial",
                    TetherfallSpireArtBudgetClass.Station,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(2.5f, 2.5f, 1.5f),
                    null,
                    Roles(
                        TetherfallSpireArt.SocketPuzzle,
                        TetherfallSpireArt.SocketWork,
                        TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.SeamLatch,
                    "spire.puzzle.seam_latch",
                    "Height-coded seam latch",
                    TetherfallSpireArtBudgetClass.Station,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(3f, 2.6f, 1.5f),
                    null,
                    Roles(
                        TetherfallSpireArt.SocketPuzzle,
                        TetherfallSpireArt.SocketWork,
                        TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.ResonancePlinth,
                    "spire.puzzle.resonance_plinth",
                    "Resonance plinth",
                    TetherfallSpireArtBudgetClass.Station,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(2.5f, 2.8f, 2.5f),
                    null,
                    Roles(
                        TetherfallSpireArt.SocketPuzzle,
                        TetherfallSpireArt.SocketWork,
                        TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.PressureTile,
                    "spire.puzzle.pressure_tile",
                    "Shape-coded pressure tile",
                    TetherfallSpireArtBudgetClass.Station,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(2.5f, 0.35f, 2.5f),
                    null,
                    Roles(TetherfallSpireArt.SocketPuzzle, TetherfallSpireArt.SocketVfx)),

                Define(
                    TetherfallSpireArtId.MinibossRing,
                    "spire.arena.miniboss_ring",
                    "Miniboss civic ring",
                    TetherfallSpireArtBudgetClass.Arena,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(24f, 7f, 24f),
                    null,
                    WithSnaps(TetherfallSpireArt.SocketBossGate, TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.BossThreshold,
                    "spire.arena.boss_threshold",
                    "Boss threshold veil",
                    TetherfallSpireArtBudgetClass.Arena,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(10f, 8f, 2f),
                    null,
                    Roles(TetherfallSpireArt.SocketBossGate, TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.BossArenaFloor,
                    "spire.arena.boss_floor",
                    "Open-center boss arena floor",
                    TetherfallSpireArtBudgetClass.Arena,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(32f, 8f, 32f),
                    null,
                    WithSnaps(TetherfallSpireArt.SocketBossGate, TetherfallSpireArt.SocketVfx)),

                Define(
                    TetherfallSpireArtId.CacheChest,
                    "spire.prop.cache_chest",
                    "Spire cache chest",
                    TetherfallSpireArtBudgetClass.Station,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(1.8f, 1.2f, 1.3f),
                    "Crate_Wooden",
                    Roles(
                        TetherfallSpireArt.SocketTreasure,
                        TetherfallSpireArt.SocketWork,
                        TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.RepairBench,
                    "spire.prop.repair_bench",
                    "Spire repair bench",
                    TetherfallSpireArtBudgetClass.Station,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(3f, 1.8f, 1.8f),
                    "Workbench",
                    Roles(TetherfallSpireArt.SocketWork, TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.WaymarkBanner,
                    "spire.prop.waymark_banner",
                    "Spire waymark banner",
                    TetherfallSpireArtBudgetClass.Station,
                    TetherfallSpirePresentationClass.InteriorActivation,
                    new Vector3(1.8f, 3.5f, 0.6f),
                    null,
                    Roles(TetherfallSpireArt.SocketWork, TetherfallSpireArt.SocketVfx)),

                Define(
                    TetherfallSpireArtId.CoilhavenCore,
                    "spire.rest.coilhaven_core",
                    "Coilhaven rest landmark",
                    TetherfallSpireArtBudgetClass.HeroRestOrVendor,
                    TetherfallSpirePresentationClass.CoilhavenOrVendor,
                    new Vector3(8f, 5f, 8f),
                    null,
                    Roles(
                        TetherfallSpireArt.SocketRest,
                        TetherfallSpireArt.SocketWork,
                        TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.CoilhavenBed,
                    "spire.rest.coilhaven_bed",
                    "Coilhaven rest bed",
                    TetherfallSpireArtBudgetClass.HeroRestOrVendor,
                    TetherfallSpirePresentationClass.CoilhavenOrVendor,
                    new Vector3(3f, 1.4f, 3.5f),
                    "Bench",
                    Roles(
                        TetherfallSpireArt.SocketRest,
                        TetherfallSpireArt.SocketWork,
                        TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.CoilhavenHearth,
                    "spire.rest.coilhaven_hearth",
                    "Coilhaven coil hearth",
                    TetherfallSpireArtBudgetClass.HeroRestOrVendor,
                    TetherfallSpirePresentationClass.CoilhavenOrVendor,
                    new Vector3(3.5f, 2.2f, 3.5f),
                    "Cauldron",
                    Roles(
                        TetherfallSpireArt.SocketRest,
                        TetherfallSpireArt.SocketWork,
                        TetherfallSpireArt.SocketVfx)),

                Define(
                    TetherfallSpireArtId.SpirecoilStall,
                    "spire.city.spirecoil_stall",
                    "Spirecoil market stall",
                    TetherfallSpireArtBudgetClass.HeroRestOrVendor,
                    TetherfallSpirePresentationClass.CoilhavenOrVendor,
                    new Vector3(5f, 4f, 4f),
                    "Stall_Empty",
                    Roles(
                        TetherfallSpireArt.SocketVendor,
                        TetherfallSpireArt.SocketWork,
                        TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.SpirecoilCounter,
                    "spire.city.spirecoil_counter",
                    "Spirecoil vendor counter",
                    TetherfallSpireArtBudgetClass.HeroRestOrVendor,
                    TetherfallSpirePresentationClass.CoilhavenOrVendor,
                    new Vector3(3.5f, 1.7f, 1.8f),
                    "Table_Large",
                    Roles(
                        TetherfallSpireArt.SocketVendor,
                        TetherfallSpireArt.SocketWork,
                        TetherfallSpireArt.SocketVfx)),
                Define(
                    TetherfallSpireArtId.SpirecoilBeacon,
                    "spire.city.spirecoil_beacon",
                    "Spirecoil no-light beacon",
                    TetherfallSpireArtBudgetClass.HeroRestOrVendor,
                    TetherfallSpirePresentationClass.CoilhavenOrVendor,
                    new Vector3(1.2f, 4f, 1.2f),
                    null,
                    Roles(TetherfallSpireArt.SocketVendor, TetherfallSpireArt.SocketVfx))
            });

        private static readonly Dictionary<TetherfallSpireArtId, TetherfallSpireArtDefinition>
            DefinitionsById;
        private static readonly Dictionary<string, TetherfallSpireArtDefinition>
            DefinitionsByStableId;

        static TetherfallSpireArtCatalog()
        {
            DefinitionsById =
                new Dictionary<TetherfallSpireArtId, TetherfallSpireArtDefinition>();
            DefinitionsByStableId =
                new Dictionary<string, TetherfallSpireArtDefinition>(StringComparer.Ordinal);

            foreach (var definition in Definitions)
            {
                DefinitionsById.Add(definition.ArtId, definition);
                DefinitionsByStableId.Add(definition.StableId, definition);
            }
        }

        public static IReadOnlyList<TetherfallSpireArtDefinition> All => Definitions;

        public static TetherfallSpireArtDefinition Get(TetherfallSpireArtId artId)
        {
            if (!DefinitionsById.TryGetValue(artId, out var definition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(artId),
                    artId,
                    "Unknown Infinite Spire art ID.");
            }

            return definition;
        }

        public static bool TryGet(
            string stableId,
            out TetherfallSpireArtDefinition definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(stableId) &&
                   DefinitionsByStableId.TryGetValue(stableId, out definition);
        }

        public static bool TryParse(string stableId, out TetherfallSpireArtId artId)
        {
            if (TryGet(stableId, out var definition))
            {
                artId = definition.ArtId;
                return true;
            }

            artId = default;
            return false;
        }

        public static string GetStableId(TetherfallSpireArtId artId)
        {
            return Get(artId).StableId;
        }

        public static void GetBudget(
            TetherfallSpireArtBudgetClass budgetClass,
            out int rendererBudget,
            out int materialBudget)
        {
            switch (budgetClass)
            {
                case TetherfallSpireArtBudgetClass.Structural:
                    rendererBudget = StructuralRendererBudget;
                    materialBudget = StructuralMaterialBudget;
                    return;
                case TetherfallSpireArtBudgetClass.Station:
                    rendererBudget = StationRendererBudget;
                    materialBudget = StationMaterialBudget;
                    return;
                case TetherfallSpireArtBudgetClass.RoomShell:
                    rendererBudget = ShellRendererBudget;
                    materialBudget = ShellMaterialBudget;
                    return;
                case TetherfallSpireArtBudgetClass.Arena:
                    rendererBudget = ArenaRendererBudget;
                    materialBudget = ArenaMaterialBudget;
                    return;
                case TetherfallSpireArtBudgetClass.HeroRestOrVendor:
                    rendererBudget = HeroRendererBudget;
                    materialBudget = HeroMaterialBudget;
                    return;
                case TetherfallSpireArtBudgetClass.LocalExterior:
                    rendererBudget = LocalExteriorRendererBudget;
                    materialBudget = LocalExteriorMaterialBudget;
                    return;
                case TetherfallSpireArtBudgetClass.DistantProxy:
                    rendererBudget = ProxyRendererBudget;
                    materialBudget = ProxyMaterialBudget;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(budgetClass),
                        budgetClass,
                        "Unknown Infinite Spire budget class.");
            }
        }

        private static TetherfallSpireArtDefinition Define(
            TetherfallSpireArtId artId,
            string stableId,
            string displayName,
            TetherfallSpireArtBudgetClass budgetClass,
            TetherfallSpirePresentationClass presentationClass,
            Vector3 dimensions,
            string preferredResourceName,
            string[] sockets)
        {
            GetBudget(budgetClass, out var renderers, out var materials);
            return new TetherfallSpireArtDefinition(
                artId,
                stableId,
                displayName,
                budgetClass,
                presentationClass,
                dimensions,
                renderers,
                materials,
                preferredResourceName,
                sockets);
        }

        private static string[] NoSockets()
        {
            return Array.Empty<string>();
        }

        private static string[] Roles(params string[] roles)
        {
            return roles == null ? Array.Empty<string>() : (string[])roles.Clone();
        }

        private static string[] WithSnaps(params string[] roles)
        {
            var roleCount = roles == null ? 0 : roles.Length;
            var result = new string[6 + roleCount];
            result[0] = TetherfallSpireArt.SnapNorth;
            result[1] = TetherfallSpireArt.SnapEast;
            result[2] = TetherfallSpireArt.SnapSouth;
            result[3] = TetherfallSpireArt.SnapWest;
            result[4] = TetherfallSpireArt.SnapUp;
            result[5] = TetherfallSpireArt.SnapDown;
            for (var index = 0; index < roleCount; index++)
            {
                result[6 + index] = roles[index];
            }

            return result;
        }
    }

    public readonly struct TetherfallSpireArtBuildOptions
    {
        public bool ForceProceduralFallback { get; }
        public bool RebuildExisting { get; }

        public static TetherfallSpireArtBuildOptions Default =>
            new TetherfallSpireArtBuildOptions(false, false);
        public static TetherfallSpireArtBuildOptions Rebuild =>
            new TetherfallSpireArtBuildOptions(false, true);
        public static TetherfallSpireArtBuildOptions ProceduralFallback =>
            new TetherfallSpireArtBuildOptions(true, true);

        public TetherfallSpireArtBuildOptions(
            bool forceProceduralFallback,
            bool rebuildExisting)
        {
            ForceProceduralFallback = forceProceduralFallback;
            RebuildExisting = rebuildExisting;
        }
    }

    /// <summary>
    /// Collider-free visual metadata. This component deliberately has no frame callbacks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TetherfallSpireArtPiece : MonoBehaviour
    {
        [SerializeField] private TetherfallSpireArtId artId;
        [SerializeField] private string stableId;
        [SerializeField] private int rendererCount;
        [SerializeField] private int sharedMaterialCount;
        [SerializeField] private bool usedPreferredResource;
        [SerializeField] private bool forcedProceduralFallback;
        [SerializeField] private Bounds localVisualBounds;

        public TetherfallSpireArtId ArtId => artId;
        public string StableId => stableId;
        public int RendererCount => rendererCount;
        public int SharedMaterialCount => sharedMaterialCount;
        public bool UsedPreferredResource => usedPreferredResource;
        public bool UsedProceduralFallback => !usedPreferredResource;
        public bool ForcedProceduralFallback => forcedProceduralFallback;
        public Bounds LocalVisualBounds => localVisualBounds;
        public TetherfallSpireArtDefinition Definition => TetherfallSpireArtCatalog.Get(artId);
        public TetherfallSpirePresentationClass PresentationClass =>
            Definition.PresentationClass;
        public string CullingGroupKey => Definition.CullingGroupKey;
        public float NearDistance => Definition.NearDistance;
        public float CullDistance => Definition.CullDistance;
        public bool InteriorActivationReady => Definition.IsInteriorActivationReady;
        public bool ProxyMutuallyExclusiveWithLocalExterior =>
            Definition.IsDistantProxy;

        internal void Configure(
            TetherfallSpireArtDefinition definition,
            bool usedResource,
            bool forcedFallback,
            int renderers,
            int materials,
            Bounds bounds)
        {
            artId = definition.ArtId;
            stableId = definition.StableId;
            usedPreferredResource = usedResource;
            forcedProceduralFallback = forcedFallback;
            rendererCount = renderers;
            sharedMaterialCount = materials;
            localVisualBounds = bounds;
        }
    }

    /// <summary>
    /// Central presentation owner. Two CullingGroups apply the 70/145 metre exterior and
    /// 70/110 metre rest/vendor bands. Interior visibility is director-controlled, and the
    /// proxy is always the logical inverse of local-exterior visibility.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TetherfallSpirePresentationController : MonoBehaviour
    {
        public const float ExteriorNearDistance = 70f;
        public const float ExteriorCullDistance = 145f;
        public const float RestVendorNearDistance = 70f;
        public const float RestVendorCullDistance = 110f;

        private CullingGroup exteriorCullingGroup;
        private CullingGroup restVendorCullingGroup;
        private Renderer[] localExteriorRenderers = Array.Empty<Renderer>();
        private Renderer[] proxyRenderers = Array.Empty<Renderer>();
        private Renderer[][] restVendorRendererGroups = Array.Empty<Renderer[]>();
        private readonly Dictionary<int, Renderer[]> interiorRendererGroups =
            new Dictionary<int, Renderer[]>();
        private readonly Dictionary<int, bool> interiorActivationStates =
            new Dictionary<int, bool>();
        private Camera targetCamera;

        public int RegisteredPieceCount { get; private set; }
        public int RegisteredRendererCount { get; private set; }
        public int LocalExteriorPieceCount { get; private set; }
        public int ProxyPieceCount { get; private set; }
        public int InteriorPieceCount { get; private set; }
        public int RestVendorPieceCount { get; private set; }
        public int ActiveCullingGroupCount =>
            (exteriorCullingGroup == null ? 0 : 1) +
            (restVendorCullingGroup == null ? 0 : 1);
        public bool LocalExteriorVisible { get; private set; }
        public bool ProxyVisible { get; private set; }
        public Camera TargetCamera => targetCamera;

        public static TetherfallSpirePresentationController Ensure(
            Transform presentationParent,
            Camera camera = null)
        {
            if (presentationParent == null)
            {
                throw new ArgumentNullException(nameof(presentationParent));
            }

            var controller =
                presentationParent.GetComponent<TetherfallSpirePresentationController>();
            if (controller == null)
            {
                controller =
                    presentationParent.gameObject.AddComponent<TetherfallSpirePresentationController>();
            }

            controller.RebuildGroups(camera);
            return controller;
        }

        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
            RebuildGroups(camera);
        }

        public void RebuildGroups(Camera camera = null)
        {
            DisposeCullingGroups();
            targetCamera = camera != null ? camera : targetCamera;

            var pieces = GetComponentsInChildren<TetherfallSpireArtPiece>(true);
            var localRenderers = new List<Renderer>();
            var proxyList = new List<Renderer>();
            var restGroups = new List<Renderer[]>();
            var restSpheres = new List<BoundingSphere>();
            var liveInteriorIds = new HashSet<int>();

            RegisteredPieceCount = pieces.Length;
            RegisteredRendererCount = 0;
            LocalExteriorPieceCount = 0;
            ProxyPieceCount = 0;
            InteriorPieceCount = 0;
            RestVendorPieceCount = 0;
            interiorRendererGroups.Clear();

            foreach (var piece in pieces)
            {
                var renderers = piece.GetComponentsInChildren<Renderer>(true);
                RegisteredRendererCount += renderers.Length;
                switch (piece.PresentationClass)
                {
                    case TetherfallSpirePresentationClass.LocalExterior:
                        LocalExteriorPieceCount++;
                        localRenderers.AddRange(renderers);
                        break;
                    case TetherfallSpirePresentationClass.DistantProxy:
                        ProxyPieceCount++;
                        proxyList.AddRange(renderers);
                        break;
                    case TetherfallSpirePresentationClass.CoilhavenOrVendor:
                        RestVendorPieceCount++;
                        restGroups.Add(renderers);
                        restSpheres.Add(CalculateBoundingSphere(renderers, piece.transform.position));
                        break;
                    default:
                    {
                        InteriorPieceCount++;
                        var instanceId = piece.GetInstanceID();
                        liveInteriorIds.Add(instanceId);
                        interiorRendererGroups[instanceId] = renderers;
                        if (!interiorActivationStates.TryGetValue(instanceId, out var active))
                        {
                            active = true;
                            interiorActivationStates.Add(instanceId, true);
                        }

                        SetRendererGroup(renderers, active);
                        break;
                    }
                }
            }

            RemoveStaleInteriorStates(liveInteriorIds);
            localExteriorRenderers = localRenderers.ToArray();
            proxyRenderers = proxyList.ToArray();
            restVendorRendererGroups = restGroups.ToArray();

            if (targetCamera == null)
            {
                SetLocalExteriorState(localExteriorRenderers.Length > 0);
                SetAllRendererGroups(restVendorRendererGroups, true);
                return;
            }

            BuildExteriorCullingGroup();
            BuildRestVendorCullingGroup(restSpheres.ToArray());
        }

        public bool SetInteriorActive(TetherfallSpireArtPiece piece, bool active)
        {
            if (piece == null || !piece.InteriorActivationReady)
            {
                return false;
            }

            var instanceId = piece.GetInstanceID();
            interiorActivationStates[instanceId] = active;
            if (!interiorRendererGroups.TryGetValue(instanceId, out var renderers))
            {
                renderers = piece.GetComponentsInChildren<Renderer>(true);
                interiorRendererGroups[instanceId] = renderers;
            }

            SetRendererGroup(renderers, active);
            return true;
        }

        public void SetAllInteriorsActive(bool active)
        {
            var pieces = GetComponentsInChildren<TetherfallSpireArtPiece>(true);
            foreach (var piece in pieces)
            {
                if (piece.InteriorActivationReady)
                {
                    SetInteriorActive(piece, active);
                }
            }
        }

        public bool IsInteriorActive(TetherfallSpireArtPiece piece)
        {
            if (piece == null || !piece.InteriorActivationReady)
            {
                return false;
            }

            return !interiorActivationStates.TryGetValue(piece.GetInstanceID(), out var active) ||
                   active;
        }

        private void BuildExteriorCullingGroup()
        {
            if (localExteriorRenderers.Length == 0)
            {
                SetLocalExteriorState(false);
                return;
            }

            var sphere = CalculateBoundingSphere(localExteriorRenderers, transform.position);
            exteriorCullingGroup = new CullingGroup
            {
                targetCamera = targetCamera,
                onStateChanged = HandleExteriorStateChanged
            };
            exteriorCullingGroup.SetDistanceReferencePoint(targetCamera.transform);
            exteriorCullingGroup.SetBoundingDistances(
                new[] { ExteriorNearDistance, ExteriorCullDistance });
            exteriorCullingGroup.SetBoundingSpheres(new[] { sphere });
            exteriorCullingGroup.SetBoundingSphereCount(1);

            var distance =
                Vector3.Distance(targetCamera.transform.position, sphere.position) - sphere.radius;
            SetLocalExteriorState(distance <= ExteriorCullDistance);
        }

        private void BuildRestVendorCullingGroup(BoundingSphere[] spheres)
        {
            if (spheres == null || spheres.Length == 0)
            {
                return;
            }

            restVendorCullingGroup = new CullingGroup
            {
                targetCamera = targetCamera,
                onStateChanged = HandleRestVendorStateChanged
            };
            restVendorCullingGroup.SetDistanceReferencePoint(targetCamera.transform);
            restVendorCullingGroup.SetBoundingDistances(
                new[] { RestVendorNearDistance, RestVendorCullDistance });
            restVendorCullingGroup.SetBoundingSpheres(spheres);
            restVendorCullingGroup.SetBoundingSphereCount(spheres.Length);

            for (var index = 0; index < spheres.Length; index++)
            {
                var distance =
                    Vector3.Distance(targetCamera.transform.position, spheres[index].position) -
                    spheres[index].radius;
                SetRendererGroup(
                    restVendorRendererGroups[index],
                    distance <= RestVendorCullDistance);
            }
        }

        private void HandleExteriorStateChanged(CullingGroupEvent state)
        {
            SetLocalExteriorState(state.isVisible && state.currentDistance < 2);
        }

        private void HandleRestVendorStateChanged(CullingGroupEvent state)
        {
            if (state.index < 0 || state.index >= restVendorRendererGroups.Length)
            {
                return;
            }

            SetRendererGroup(
                restVendorRendererGroups[state.index],
                state.isVisible && state.currentDistance < 2);
        }

        private void SetLocalExteriorState(bool localVisible)
        {
            LocalExteriorVisible = localExteriorRenderers.Length > 0 && localVisible;
            SetRendererGroup(localExteriorRenderers, LocalExteriorVisible);

            ProxyVisible =
                proxyRenderers.Length > 0 &&
                (localExteriorRenderers.Length == 0 || !LocalExteriorVisible);
            SetRendererGroup(proxyRenderers, ProxyVisible);
        }

        private void RemoveStaleInteriorStates(HashSet<int> liveIds)
        {
            var staleIds = new List<int>();
            foreach (var pair in interiorActivationStates)
            {
                if (!liveIds.Contains(pair.Key))
                {
                    staleIds.Add(pair.Key);
                }
            }

            foreach (var staleId in staleIds)
            {
                interiorActivationStates.Remove(staleId);
            }
        }

        private static void SetAllRendererGroups(Renderer[][] groups, bool visible)
        {
            foreach (var group in groups)
            {
                SetRendererGroup(group, visible);
            }
        }

        private static void SetRendererGroup(Renderer[] renderers, bool visible)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (var visualRenderer in renderers)
            {
                if (visualRenderer != null)
                {
                    visualRenderer.enabled = visible;
                }
            }
        }

        private void DisposeCullingGroups()
        {
            if (exteriorCullingGroup != null)
            {
                exteriorCullingGroup.onStateChanged = null;
                exteriorCullingGroup.Dispose();
                exteriorCullingGroup = null;
            }

            if (restVendorCullingGroup != null)
            {
                restVendorCullingGroup.onStateChanged = null;
                restVendorCullingGroup.Dispose();
                restVendorCullingGroup = null;
            }
        }

        private void OnDestroy()
        {
            DisposeCullingGroups();
        }

        private static BoundingSphere CalculateBoundingSphere(
            IReadOnlyList<Renderer> renderers,
            Vector3 fallbackCenter)
        {
            var hasBounds = false;
            var combined = new Bounds(fallbackCenter, Vector3.one);
            for (var index = 0; index < renderers.Count; index++)
            {
                var visualRenderer = renderers[index];
                if (visualRenderer == null || !TetherfallSpireArt.IsFinite(visualRenderer.bounds))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combined = visualRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(visualRenderer.bounds);
                }
            }

            return hasBounds
                ? new BoundingSphere(combined.center, Mathf.Max(0.5f, combined.extents.magnitude))
                : new BoundingSphere(fallbackCenter, 0.5f);
        }
    }

    /// <summary>
    /// Runtime construction API for the reusable Infinite Spire art family.
    /// Visual roots are deterministic, collider-free, source-safe, and material-cached.
    /// </summary>
    public static class TetherfallSpireArt
    {
        public const string RootPrefix = "Infinite Spire Art · ";
        public const string VisualRootName = "Visuals";
        public const string SocketRootName = "Sockets";
        public const string OriginalMaterialPrefix = "Tetherfall Spire · ";
        public const string ResourcePrefix = "Art/Quaternius/";
        public const string QuaterniusSourceCredit = "Existing Quaternius Resources prefab (CC0)";
        public const string OriginalSourceCredit = "Original Tetherfall procedural fallback";
        public const int OriginalMaterialKeyCount = 10;

        public const string SnapNorth = "Snap_N";
        public const string SnapEast = "Snap_E";
        public const string SnapSouth = "Snap_S";
        public const string SnapWest = "Snap_W";
        public const string SnapUp = "Snap_Up";
        public const string SnapDown = "Snap_Down";
        public const string SocketDoor = "Socket_Door";
        public const string SocketStairUp = "Socket_Stair_Up";
        public const string SocketStairDown = "Socket_Stair_Down";
        public const string SocketLift = "Socket_Lift";
        public const string SocketPuzzle = "Socket_Puzzle";
        public const string SocketTreasure = "Socket_Treasure";
        public const string SocketVendor = "Socket_Vendor";
        public const string SocketRest = "Socket_Rest";
        public const string SocketBossGate = "Socket_BossGate";
        public const string SocketWork = "Socket_Work";
        public const string SocketVfx = "Socket_VFX";

        private enum SpireMaterialKey
        {
            PaleAshStone,
            DeepStone,
            Timber,
            Copper,
            RestrainedTeal,
            Canvas,
            Moss,
            AmberRetreat,
            GlassMist,
            ShadowProxy
        }

        private static readonly Dictionary<SpireMaterialKey, Material> SharedMaterials =
            new Dictionary<SpireMaterialKey, Material>();

        public static int CachedMaterialCount => SharedMaterials.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetMaterialCache()
        {
            foreach (var material in SharedMaterials.Values)
            {
                DestroyObject(material);
            }

            SharedMaterials.Clear();
        }

        public static GameObject Build(TetherfallSpireArtId artId, Transform parent)
        {
            return Build(artId, parent, TetherfallSpireArtBuildOptions.Default);
        }

        public static GameObject Build(string stableId, Transform parent)
        {
            if (!TetherfallSpireArtCatalog.TryParse(stableId, out var artId))
            {
                throw new ArgumentException(
                    $"Unknown Infinite Spire art stable ID '{stableId}'.",
                    nameof(stableId));
            }

            return Build(artId, parent, TetherfallSpireArtBuildOptions.Default);
        }

        public static bool TryBuild(
            string stableId,
            Transform parent,
            out GameObject visualRoot)
        {
            visualRoot = null;
            if (!TetherfallSpireArtCatalog.TryParse(stableId, out var artId))
            {
                return false;
            }

            visualRoot = Build(artId, parent, TetherfallSpireArtBuildOptions.Default);
            return visualRoot != null;
        }

        public static bool TryBuild(
            TetherfallSpireArtId artId,
            Transform parent,
            out GameObject visualRoot)
        {
            visualRoot = null;
            try
            {
                visualRoot = Build(artId, parent, TetherfallSpireArtBuildOptions.Default);
                return visualRoot != null;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        public static GameObject Rebuild(TetherfallSpireArtId artId, Transform parent)
        {
            return Build(artId, parent, TetherfallSpireArtBuildOptions.Rebuild);
        }

        public static GameObject Rebuild(string stableId, Transform parent)
        {
            if (!TetherfallSpireArtCatalog.TryParse(stableId, out var artId))
            {
                throw new ArgumentException(
                    $"Unknown Infinite Spire art stable ID '{stableId}'.",
                    nameof(stableId));
            }

            return Rebuild(artId, parent);
        }

        public static GameObject BuildProceduralFallback(
            TetherfallSpireArtId artId,
            Transform parent)
        {
            return Build(artId, parent, TetherfallSpireArtBuildOptions.ProceduralFallback);
        }

        public static GameObject BuildProceduralFallback(
            string stableId,
            Transform parent)
        {
            if (!TetherfallSpireArtCatalog.TryParse(stableId, out var artId))
            {
                throw new ArgumentException(
                    $"Unknown Infinite Spire art stable ID '{stableId}'.",
                    nameof(stableId));
            }

            return BuildProceduralFallback(artId, parent);
        }

        public static GameObject Build(
            TetherfallSpireArtId artId,
            Transform parent,
            TetherfallSpireArtBuildOptions options)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var definition = TetherfallSpireArtCatalog.Get(artId);
            var piece = FindDirectPiece(parent, artId);
            if (piece != null && !options.RebuildExisting)
            {
                return piece.gameObject;
            }

            var createdRoot = piece == null;
            if (createdRoot)
            {
                var rootObject = new GameObject(RootPrefix + definition.StableId);
                rootObject.transform.SetParent(parent, false);
                piece = rootObject.AddComponent<TetherfallSpireArtPiece>();
            }
            else
            {
                ClearChildren(piece.transform);
                piece.gameObject.name = RootPrefix + definition.StableId;
            }

            try
            {
                var visualRoot = new GameObject(VisualRootName).transform;
                visualRoot.SetParent(piece.transform, false);
                var builder = new PieceBuilder(
                    definition,
                    visualRoot,
                    options.ForceProceduralFallback);
                BuildVisual(builder);
                builder.ValidateFinalBudget();
                BuildSockets(definition, piece.transform);

                piece.Configure(
                    definition,
                    builder.UsedPreferredResource,
                    options.ForceProceduralFallback,
                    builder.RendererCount,
                    builder.SharedMaterialCount,
                    CalculateLocalBounds(piece.transform, builder.Renderers));
                return piece.gameObject;
            }
            catch
            {
                if (createdRoot)
                {
                    DestroyObject(piece.gameObject);
                }
                else
                {
                    ClearChildren(piece.transform);
                }

                throw;
            }
        }

        public static bool Clear(Transform parent, TetherfallSpireArtId artId)
        {
            if (parent == null)
            {
                return false;
            }

            var removed = false;
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);
                var piece = child.GetComponent<TetherfallSpireArtPiece>();
                if (piece == null || piece.ArtId != artId)
                {
                    continue;
                }

                removed = true;
                DestroyObject(child.gameObject);
            }

            return removed;
        }

        public static bool Clear(Transform parent, string stableId)
        {
            return TetherfallSpireArtCatalog.TryParse(stableId, out var artId) &&
                   Clear(parent, artId);
        }

        public static int ClearAll(Transform parent)
        {
            if (parent == null)
            {
                return 0;
            }

            var removed = 0;
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);
                if (child.GetComponent<TetherfallSpireArtPiece>() == null)
                {
                    continue;
                }

                removed++;
                DestroyObject(child.gameObject);
            }

            return removed;
        }

        public static TetherfallSpirePresentationController EnsurePresentationController(
            Transform presentationParent,
            Camera camera = null)
        {
            return TetherfallSpirePresentationController.Ensure(presentationParent, camera);
        }

        private static void BuildVisual(PieceBuilder builder)
        {
            switch (builder.Definition.ArtId)
            {
                case TetherfallSpireArtId.BasePlinth:
                    BuildBasePlinth(builder);
                    return;
                case TetherfallSpireArtId.CityFlange:
                    BuildCityFlange(builder);
                    return;
                case TetherfallSpireArtId.TierDrumA:
                    BuildTierDrum(builder, false);
                    return;
                case TetherfallSpireArtId.TierDrumB:
                    BuildTierDrum(builder, true);
                    return;
                case TetherfallSpireArtId.CoilRibSet:
                    BuildCoilRibSet(builder);
                    return;
                case TetherfallSpireArtId.CrownRing:
                    BuildCrownRing(builder);
                    return;
                case TetherfallSpireArtId.ActiveSeam:
                    BuildActiveSeam(builder);
                    return;
                case TetherfallSpireArtId.DistantNeedle:
                    BuildDistantNeedle(builder);
                    return;
                case TetherfallSpireArtId.DistantCrown:
                    BuildDistantCrown(builder);
                    return;
                case TetherfallSpireArtId.FacadeStraight:
                    BuildFacadeStraight(builder);
                    return;
                case TetherfallSpireArtId.FacadeWindow:
                    BuildFacadeWindow(builder);
                    return;
                case TetherfallSpireArtId.FacadeDoorBay:
                    BuildFacadeDoorBay(builder);
                    return;
                case TetherfallSpireArtId.FacadeArch:
                    BuildFacadeArch(builder);
                    return;
                case TetherfallSpireArtId.ShellStandard:
                    BuildShellStandard(builder);
                    return;
                case TetherfallSpireArtId.ShellCombat:
                    BuildShellCombat(builder);
                    return;
                case TetherfallSpireArtId.ShellPuzzle:
                    BuildShellPuzzle(builder);
                    return;
                case TetherfallSpireArtId.ShellRest:
                    BuildShellRest(builder);
                    return;
                case TetherfallSpireArtId.DoorRound:
                    BuildDoorRound(builder);
                    return;
                case TetherfallSpireArtId.DoorSeal:
                    BuildDoorSeal(builder);
                    return;
                case TetherfallSpireArtId.DoorRetreat:
                    BuildDoorRetreat(builder);
                    return;
                case TetherfallSpireArtId.StairStraight:
                    BuildStairStraight(builder);
                    return;
                case TetherfallSpireArtId.StairPlatform:
                    BuildStairPlatform(builder);
                    return;
                case TetherfallSpireArtId.LiftCage:
                    BuildLiftCage(builder);
                    return;
                case TetherfallSpireArtId.LiftGuide:
                    BuildLiftGuide(builder);
                    return;
                case TetherfallSpireArtId.CoilDial:
                    BuildCoilDial(builder);
                    return;
                case TetherfallSpireArtId.SeamLatch:
                    BuildSeamLatch(builder);
                    return;
                case TetherfallSpireArtId.ResonancePlinth:
                    BuildResonancePlinth(builder);
                    return;
                case TetherfallSpireArtId.PressureTile:
                    BuildPressureTile(builder);
                    return;
                case TetherfallSpireArtId.MinibossRing:
                    BuildMinibossRing(builder);
                    return;
                case TetherfallSpireArtId.BossThreshold:
                    BuildBossThreshold(builder);
                    return;
                case TetherfallSpireArtId.BossArenaFloor:
                    BuildBossArenaFloor(builder);
                    return;
                case TetherfallSpireArtId.CacheChest:
                    BuildCacheChest(builder);
                    return;
                case TetherfallSpireArtId.RepairBench:
                    BuildRepairBench(builder);
                    return;
                case TetherfallSpireArtId.WaymarkBanner:
                    BuildWaymarkBanner(builder);
                    return;
                case TetherfallSpireArtId.CoilhavenCore:
                    BuildCoilhavenCore(builder);
                    return;
                case TetherfallSpireArtId.CoilhavenBed:
                    BuildCoilhavenBed(builder);
                    return;
                case TetherfallSpireArtId.CoilhavenHearth:
                    BuildCoilhavenHearth(builder);
                    return;
                case TetherfallSpireArtId.SpirecoilStall:
                    BuildSpirecoilStall(builder);
                    return;
                case TetherfallSpireArtId.SpirecoilCounter:
                    BuildSpirecoilCounter(builder);
                    return;
                case TetherfallSpireArtId.SpirecoilBeacon:
                    BuildSpirecoilBeacon(builder);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(builder.Definition.ArtId),
                        builder.Definition.ArtId,
                        "Infinite Spire art ID has no visual recipe.");
            }
        }

        private static void BuildBasePlinth(PieceBuilder builder)
        {
            builder.AddCylinder(
                "Stepped civic drum · broad pale-ash foot",
                new Vector3(0f, 0.75f, 0f),
                new Vector3(28f, 1.5f, 28f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddCylinder(
                "Stepped civic drum · deep inset",
                new Vector3(0f, 1.75f, 0f),
                new Vector3(24f, 0.5f, 24f),
                Quaternion.identity,
                SpireMaterialKey.DeepStone);
            builder.AddCylinder(
                "Copper civic toe ring",
                new Vector3(0f, 0.12f, 0f),
                new Vector3(29f, 0.24f, 29f),
                Quaternion.identity,
                SpireMaterialKey.Copper);

            if (!builder.TryPreferredResource(
                    new Vector3(8.5f, 0f, 7f),
                    Quaternion.Euler(0f, 28f, 0f),
                    Vector3.one * 1.4f,
                    2,
                    2,
                    0,
                    0))
            {
                builder.AddSphere(
                    "Original ashstone buttress",
                    new Vector3(8.5f, 0.65f, 7f),
                    new Vector3(2.5f, 1.3f, 2.2f),
                    Quaternion.Euler(0f, 28f, 0f),
                    SpireMaterialKey.PaleAshStone);
            }
        }

        private static void BuildCityFlange(PieceBuilder builder)
        {
            builder.AddCylinder(
                "Weathered timber skybridge flange",
                new Vector3(0f, 0.45f, 0f),
                new Vector3(32f, 0.9f, 32f),
                Quaternion.identity,
                SpireMaterialKey.Timber);
            builder.AddCylinder(
                "Moss contact apron",
                new Vector3(0f, 0.08f, 0f),
                new Vector3(30.5f, 0.16f, 30.5f),
                Quaternion.identity,
                SpireMaterialKey.Moss);
            builder.AddCylinder(
                "Copper city-route collar",
                new Vector3(0f, 0.94f, 0f),
                new Vector3(29f, 0.16f, 29f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
        }

        private static void BuildTierDrum(PieceBuilder builder, bool upper)
        {
            var width = upper ? 18f : 22f;
            var inset = upper ? 15.5f : 19.5f;
            var anchor = upper ? 7.8f : 9.6f;
            var suffix = upper ? "B" : "A";

            builder.AddCylinder(
                "Stepped civic drum " + suffix,
                new Vector3(0f, 6f, 0f),
                new Vector3(width, 12f, width),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddCylinder(
                "Deep-stone tier shadow " + suffix,
                new Vector3(0f, 11.65f, 0f),
                new Vector3(inset, 0.5f, inset),
                Quaternion.identity,
                SpireMaterialKey.DeepStone);
            builder.AddBox(
                "West copper lift anchor " + suffix,
                new Vector3(-anchor, 5.8f, 0f),
                new Vector3(0.5f, 10.5f, 0.8f),
                Quaternion.Euler(0f, 0f, upper ? -3f : -5f),
                SpireMaterialKey.Copper);
            builder.AddBox(
                "East copper lift anchor " + suffix,
                new Vector3(anchor, 5.8f, 0f),
                new Vector3(0.5f, 10.5f, 0.8f),
                Quaternion.Euler(0f, 0f, upper ? 3f : 5f),
                SpireMaterialKey.Copper);
        }

        private static void BuildCoilRibSet(PieceBuilder builder)
        {
            builder.AddCylinder(
                "Copper lift-rib collector collar",
                new Vector3(0f, 0.18f, 0f),
                new Vector3(23f, 0.36f, 23f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
            builder.AddBox(
                "External copper lift rib west",
                new Vector3(-10.2f, 12f, 0f),
                new Vector3(0.55f, 24f, 0.65f),
                Quaternion.Euler(0f, 0f, -5f),
                SpireMaterialKey.Copper);
            builder.AddBox(
                "External copper lift rib east",
                new Vector3(10.2f, 12f, 0f),
                new Vector3(0.55f, 24f, 0.65f),
                Quaternion.Euler(0f, 0f, 5f),
                SpireMaterialKey.Copper);
            builder.AddBox(
                "External copper lift rib south",
                new Vector3(0f, 12f, -10.2f),
                new Vector3(0.65f, 24f, 0.55f),
                Quaternion.Euler(-5f, 0f, 0f),
                SpireMaterialKey.Copper);
            builder.AddBox(
                "External copper lift rib north",
                new Vector3(0f, 12f, 10.2f),
                new Vector3(0.65f, 24f, 0.55f),
                Quaternion.Euler(5f, 0f, 0f),
                SpireMaterialKey.Copper);
        }

        private static void BuildCrownRing(PieceBuilder builder)
        {
            builder.AddCylinder(
                "Pale-ash civic crown cap",
                new Vector3(0f, 0.2f, 0f),
                new Vector3(7f, 0.4f, 7f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddSquareRing(
                "Open copper crown annulus",
                4.7f,
                0.35f,
                0.35f,
                2.5f,
                SpireMaterialKey.Copper);
        }

        private static void BuildActiveSeam(PieceBuilder builder)
        {
            builder.AddSquareRing(
                "One active teal seam",
                9.2f,
                0.24f,
                0.12f,
                0.06f,
                SpireMaterialKey.RestrainedTeal);
        }

        private static void BuildDistantNeedle(PieceBuilder builder)
        {
            builder.AddCylinder(
                "Shadow proxy lower mass",
                new Vector3(0f, 24f, 0f),
                new Vector3(18f, 48f, 18f),
                Quaternion.identity,
                SpireMaterialKey.ShadowProxy);
            builder.AddCylinder(
                "Shadow proxy upper mass",
                new Vector3(0f, 64f, 0f),
                new Vector3(10f, 32f, 10f),
                Quaternion.identity,
                SpireMaterialKey.ShadowProxy);
            builder.AddCylinder(
                "Copper proxy lift-tip",
                new Vector3(0f, 81f, 0f),
                new Vector3(4f, 2f, 4f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
        }

        private static void BuildDistantCrown(PieceBuilder builder)
        {
            builder.AddCylinder(
                "Copper distant crown",
                new Vector3(0f, 0.3f, 0f),
                new Vector3(12f, 0.6f, 12f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
            builder.AddSphere(
                "Restrained distant Accord point",
                new Vector3(0f, 2.1f, 0f),
                Vector3.one * 1.3f,
                Quaternion.identity,
                SpireMaterialKey.RestrainedTeal);
        }

        private static void BuildFacadeStraight(PieceBuilder builder)
        {
            if (builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    new Vector3(2f, 0.96f, 1f),
                    1,
                    2,
                    1,
                    1))
            {
                builder.AddBox(
                    "Copper facade repair seam",
                    new Vector3(-1.62f, 1.45f, -0.2f),
                    new Vector3(0.08f, 2.5f, 0.08f),
                    Quaternion.identity,
                    SpireMaterialKey.Copper);
                return;
            }

            builder.AddBox(
                "Pale-ash straight panel",
                new Vector3(0f, 1.5f, 0f),
                new Vector3(4f, 3f, 0.28f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddBox(
                "Deep-stone facade foot",
                new Vector3(0f, 0.18f, 0f),
                new Vector3(4f, 0.36f, 0.42f),
                Quaternion.identity,
                SpireMaterialKey.DeepStone);
            builder.AddBox(
                "Copper facade repair seam",
                new Vector3(-1.62f, 1.45f, -0.2f),
                new Vector3(0.08f, 2.5f, 0.08f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
        }

        private static void BuildFacadeWindow(PieceBuilder builder)
        {
            if (builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    new Vector3(2f, 0.96f, 1f),
                    1,
                    2,
                    1,
                    1))
            {
                builder.AddBox(
                    "Copper window sill",
                    new Vector3(0f, 0.95f, -0.2f),
                    new Vector3(2.1f, 0.14f, 0.12f),
                    Quaternion.identity,
                    SpireMaterialKey.Copper);
                return;
            }

            builder.AddBox(
                "Left pale-ash window pier",
                new Vector3(-1.5f, 1.5f, 0f),
                new Vector3(1f, 3f, 0.3f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddBox(
                "Right pale-ash window pier",
                new Vector3(1.5f, 1.5f, 0f),
                new Vector3(1f, 3f, 0.3f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddBox(
                "Copper window lintel",
                new Vector3(0f, 2.84f, 0f),
                new Vector3(4f, 0.3f, 0.4f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
        }

        private static void BuildFacadeDoorBay(PieceBuilder builder)
        {
            if (builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    new Vector3(2f, 0.96f, 1f),
                    1,
                    2,
                    1,
                    1))
            {
                builder.AddBox(
                    "Copper civic door lintel",
                    new Vector3(0f, 2.7f, -0.2f),
                    new Vector3(2.2f, 0.14f, 0.12f),
                    Quaternion.identity,
                    SpireMaterialKey.Copper);
                return;
            }

            BuildOpenDoorFrame(builder, SpireMaterialKey.PaleAshStone, SpireMaterialKey.Copper);
        }

        private static void BuildFacadeArch(PieceBuilder builder)
        {
            if (builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    new Vector3(2f, 1f, 1f),
                    1,
                    2,
                    1,
                    1))
            {
                builder.AddBox(
                    "Copper arch crown",
                    new Vector3(0f, 3f, -0.24f),
                    new Vector3(2.8f, 0.15f, 0.12f),
                    Quaternion.identity,
                    SpireMaterialKey.Copper);
                return;
            }

            BuildOpenDoorFrame(builder, SpireMaterialKey.DeepStone, SpireMaterialKey.Copper);
        }

        private static void BuildShellStandard(PieceBuilder builder)
        {
            BuildRetainingFloorAndBands(builder, 16f, SpireMaterialKey.DeepStone);
            builder.AddBox(
                "North copper ceiling rib",
                new Vector3(0f, 5.75f, 5.2f),
                new Vector3(15.5f, 0.18f, 0.18f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
            builder.AddBox(
                "South copper ceiling rib",
                new Vector3(0f, 5.75f, -5.2f),
                new Vector3(15.5f, 0.18f, 0.18f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
        }

        private static void BuildShellCombat(PieceBuilder builder)
        {
            builder.AddCylinder(
                "Open combat floor",
                new Vector3(0f, 0.1f, 0f),
                new Vector3(19.5f, 0.2f, 19.5f),
                Quaternion.identity,
                SpireMaterialKey.DeepStone);
            var corners = new[]
            {
                new Vector3(-8.4f, 2.4f, -8.4f),
                new Vector3(8.4f, 2.4f, -8.4f),
                new Vector3(-8.4f, 2.4f, 8.4f),
                new Vector3(8.4f, 2.4f, 8.4f)
            };
            for (var index = 0; index < corners.Length; index++)
            {
                builder.AddCylinder(
                    "Camera-safe combat corner pillar " + index,
                    corners[index],
                    new Vector3(1.3f, 4.8f, 1.3f),
                    Quaternion.identity,
                    SpireMaterialKey.PaleAshStone);
                builder.AddBox(
                    "Copper combat cardinal marker " + index,
                    new Vector3(corners[index].x * 0.72f, 0.18f, corners[index].z * 0.72f),
                    new Vector3(0.25f, 0.16f, 1.8f),
                    Quaternion.Euler(0f, index * 90f + 45f, 0f),
                    SpireMaterialKey.Copper);
            }
        }

        private static void BuildShellPuzzle(PieceBuilder builder)
        {
            builder.AddBox(
                "Puzzle room retaining floor",
                new Vector3(0f, 0.1f, 0f),
                new Vector3(15.5f, 0.2f, 15.5f),
                Quaternion.identity,
                SpireMaterialKey.DeepStone);
            builder.AddCylinder(
                "Shape-coded puzzle dais",
                new Vector3(0f, 0.3f, 0f),
                new Vector3(4f, 0.6f, 4f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddCylinder(
                "Copper puzzle index ring",
                new Vector3(0f, 0.63f, 0f),
                new Vector3(4.5f, 0.1f, 4.5f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
            for (var index = 0; index < 3; index++)
            {
                builder.AddBox(
                    "Puzzle height marker " + (index + 1),
                    new Vector3(-1.2f + index * 1.2f, 0.8f + index * 0.25f, 0f),
                    new Vector3(0.35f, 0.6f + index * 0.5f, 0.35f),
                    Quaternion.Euler(0f, index * 45f, 0f),
                    index == 1 ? SpireMaterialKey.RestrainedTeal : SpireMaterialKey.Copper);
            }
        }

        private static void BuildShellRest(PieceBuilder builder)
        {
            builder.AddBox(
                "Warm Coilhaven timber floor",
                new Vector3(0f, 0.12f, 0f),
                new Vector3(17.5f, 0.24f, 17.5f),
                Quaternion.identity,
                SpireMaterialKey.Timber);
            builder.AddBox(
                "Deep-stone rest back",
                new Vector3(0f, 2.4f, 8.65f),
                new Vector3(17.5f, 4.8f, 0.3f),
                Quaternion.identity,
                SpireMaterialKey.DeepStone);
            builder.AddBox(
                "West canvas rest vault",
                new Vector3(-4.3f, 5.1f, 0f),
                new Vector3(8.2f, 0.2f, 17f),
                Quaternion.Euler(0f, 0f, 7f),
                SpireMaterialKey.Canvas);
            builder.AddBox(
                "East canvas rest vault",
                new Vector3(4.3f, 5.1f, 0f),
                new Vector3(8.2f, 0.2f, 17f),
                Quaternion.Euler(0f, 0f, -7f),
                SpireMaterialKey.Canvas);
            builder.AddCylinder(
                "Copper Coilhaven room focus",
                new Vector3(0f, 0.3f, 5.8f),
                new Vector3(2.8f, 0.6f, 2.8f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
            builder.AddBox(
                "Single calm Threadglass rest seam",
                new Vector3(0f, 0.28f, 8.35f),
                new Vector3(5f, 0.08f, 0.08f),
                Quaternion.identity,
                SpireMaterialKey.RestrainedTeal);
        }

        private static void BuildDoorRound(PieceBuilder builder)
        {
            if (builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    new Vector3(2f, 0.96f, 1f),
                    1,
                    2,
                    1,
                    1))
            {
                builder.AddBox(
                    "Copper round-door threshold",
                    new Vector3(0f, 0.05f, -0.2f),
                    new Vector3(1.8f, 0.1f, 0.45f),
                    Quaternion.identity,
                    SpireMaterialKey.Copper);
                return;
            }

            BuildOpenDoorFrame(builder, SpireMaterialKey.PaleAshStone, SpireMaterialKey.Copper);
        }

        private static void BuildDoorSeal(PieceBuilder builder)
        {
            builder.AddBox(
                "Deep-stone locking seal",
                new Vector3(0f, 1.5f, 0f),
                new Vector3(3.2f, 3f, 0.3f),
                Quaternion.identity,
                SpireMaterialKey.DeepStone);
            builder.AddBox(
                "Copper locking crossbar",
                new Vector3(0f, 1.5f, -0.18f),
                new Vector3(3.5f, 0.22f, 0.12f),
                Quaternion.Euler(0f, 0f, 8f),
                SpireMaterialKey.Copper);
            builder.AddSphere(
                "Shape-readable Threadglass latch",
                new Vector3(0f, 1.5f, -0.3f),
                new Vector3(0.42f, 0.62f, 0.24f),
                Quaternion.Euler(0f, 0f, 45f),
                SpireMaterialKey.RestrainedTeal);
        }

        private static void BuildDoorRetreat(PieceBuilder builder)
        {
            BuildOpenDoorFrame(builder, SpireMaterialKey.Timber, SpireMaterialKey.AmberRetreat);
        }

        private static void BuildStairStraight(PieceBuilder builder)
        {
            for (var index = 0; index < 3; index++)
            {
                var height = index + 1f;
                builder.AddBox(
                    "Broad shape-readable stair tread " + (index + 1),
                    new Vector3(0f, height * 0.5f, -1.3f + index * 1.3f),
                    new Vector3(4f, height, 1.2f),
                    Quaternion.identity,
                    index == 2 ? SpireMaterialKey.Copper : SpireMaterialKey.PaleAshStone);
            }
        }

        private static void BuildStairPlatform(PieceBuilder builder)
        {
            builder.AddBox(
                "Timber stair landing",
                new Vector3(0f, 2.85f, 0f),
                new Vector3(4f, 0.3f, 4f),
                Quaternion.identity,
                SpireMaterialKey.Timber);
            builder.AddBox(
                "West copper landing rail",
                new Vector3(-1.85f, 1.55f, 0f),
                new Vector3(0.12f, 2.8f, 4f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
            builder.AddBox(
                "East copper landing rail",
                new Vector3(1.85f, 1.55f, 0f),
                new Vector3(0.12f, 2.8f, 4f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
        }

        private static void BuildLiftCage(PieceBuilder builder)
        {
            builder.AddBox(
                "Timber lift deck",
                new Vector3(0f, 0.12f, 0f),
                new Vector3(3f, 0.24f, 3f),
                Quaternion.identity,
                SpireMaterialKey.Timber);
            var posts = new[]
            {
                new Vector3(-1.3f, 1.8f, -1.3f),
                new Vector3(1.3f, 1.8f, -1.3f),
                new Vector3(-1.3f, 1.8f, 1.3f),
                new Vector3(1.3f, 1.8f, 1.3f)
            };
            for (var index = 0; index < posts.Length; index++)
            {
                builder.AddBox(
                    "Open copper lift-cage upright " + index,
                    posts[index],
                    new Vector3(0.16f, 3.6f, 0.16f),
                    Quaternion.identity,
                    SpireMaterialKey.Copper);
            }

            builder.AddCylinder(
                "Copper lift-cage crown",
                new Vector3(0f, 3.45f, 0f),
                new Vector3(2.9f, 0.18f, 2.9f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
        }

        private static void BuildLiftGuide(PieceBuilder builder)
        {
            builder.AddBox(
                "West restrained teal lift guide",
                new Vector3(-1.15f, 1.5f, 0f),
                new Vector3(0.16f, 3f, 0.16f),
                Quaternion.identity,
                SpireMaterialKey.RestrainedTeal);
            builder.AddBox(
                "East restrained teal lift guide",
                new Vector3(1.15f, 1.5f, 0f),
                new Vector3(0.16f, 3f, 0.16f),
                Quaternion.identity,
                SpireMaterialKey.RestrainedTeal);
            builder.AddBox(
                "Copper lift-guide header",
                new Vector3(0f, 2.85f, 0f),
                new Vector3(2.6f, 0.2f, 0.3f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
        }

        private static void BuildCoilDial(PieceBuilder builder)
        {
            builder.AddBox(
                "Deep-stone dial pedestal",
                new Vector3(0f, 0.75f, 0f),
                new Vector3(1.4f, 1.5f, 1f),
                Quaternion.identity,
                SpireMaterialKey.DeepStone);
            builder.AddCylinder(
                "Copper circle-coded dial",
                new Vector3(0f, 1.65f, -0.52f),
                new Vector3(1.8f, 0.24f, 1.8f),
                Quaternion.Euler(90f, 0f, 0f),
                SpireMaterialKey.Copper);
            builder.AddBox(
                "Teal dial pointer",
                new Vector3(0.35f, 1.85f, -0.68f),
                new Vector3(0.15f, 0.8f, 0.12f),
                Quaternion.Euler(0f, 0f, -35f),
                SpireMaterialKey.RestrainedTeal);
            builder.AddSphere(
                "Dial center boss",
                new Vector3(0f, 1.65f, -0.7f),
                Vector3.one * 0.28f,
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
        }

        private static void BuildSeamLatch(PieceBuilder builder)
        {
            builder.AddBox(
                "Deep-stone seam-latch base",
                new Vector3(0f, 0.45f, 0f),
                new Vector3(2.8f, 0.9f, 1.3f),
                Quaternion.identity,
                SpireMaterialKey.DeepStone);
            for (var index = 0; index < 3; index++)
            {
                builder.AddBox(
                    "LOW MID HIGH geometry marker " + index,
                    new Vector3(-0.85f + index * 0.85f, 0.9f + index * 0.28f, -0.4f),
                    new Vector3(0.28f, 0.5f + index * 0.55f, 0.28f),
                    Quaternion.identity,
                    index == 1 ? SpireMaterialKey.RestrainedTeal : SpireMaterialKey.Copper);
            }

            builder.AddBox(
                "Amber reset lever",
                new Vector3(1.1f, 1.25f, -0.55f),
                new Vector3(0.16f, 1.1f, 0.16f),
                Quaternion.Euler(0f, 0f, -22f),
                SpireMaterialKey.AmberRetreat);
        }

        private static void BuildResonancePlinth(PieceBuilder builder)
        {
            builder.AddCylinder(
                "Pale-ash resonance foot",
                new Vector3(0f, 0.25f, 0f),
                new Vector3(2.5f, 0.5f, 2.5f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddBox(
                "Deep-stone resonance body",
                new Vector3(0f, 0.9f, 0f),
                new Vector3(1.3f, 1.3f, 1.3f),
                Quaternion.Euler(0f, 45f, 0f),
                SpireMaterialKey.DeepStone);
            builder.AddBox(
                "Threadglass resonance shard",
                new Vector3(0f, 1.9f, 0f),
                new Vector3(0.38f, 1.5f, 0.38f),
                Quaternion.Euler(0f, 45f, 45f),
                SpireMaterialKey.RestrainedTeal);
            builder.AddCylinder(
                "Copper resonance index",
                new Vector3(0f, 0.53f, 0f),
                new Vector3(2f, 0.12f, 2f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
        }

        private static void BuildPressureTile(PieceBuilder builder)
        {
            builder.AddBox(
                "Pale-ash pressure-tile frame",
                new Vector3(0f, 0.08f, 0f),
                new Vector3(2.5f, 0.16f, 2.5f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddBox(
                "Deep inset pressure surface",
                new Vector3(0f, 0.17f, 0f),
                new Vector3(1.9f, 0.12f, 1.9f),
                Quaternion.Euler(0f, 45f, 0f),
                SpireMaterialKey.DeepStone);
            builder.AddCylinder(
                "Copper shape-code boss",
                new Vector3(0f, 0.27f, 0f),
                new Vector3(0.65f, 0.12f, 0.65f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
        }

        private static void BuildMinibossRing(PieceBuilder builder)
        {
            builder.AddCylinder(
                "Open-center miniboss floor",
                new Vector3(0f, 0.1f, 0f),
                new Vector3(24f, 0.2f, 24f),
                Quaternion.identity,
                SpireMaterialKey.DeepStone);
            var positions = new[]
            {
                new Vector3(-10f, 2.5f, 0f),
                new Vector3(10f, 2.5f, 0f),
                new Vector3(0f, 2.5f, -10f),
                new Vector3(0f, 2.5f, 10f)
            };
            for (var index = 0; index < positions.Length; index++)
            {
                builder.AddCylinder(
                    "Side-only miniboss pillar " + index,
                    positions[index],
                    new Vector3(1.4f, 5f, 1.4f),
                    Quaternion.identity,
                    SpireMaterialKey.PaleAshStone);
            }

            builder.AddCylinder(
                "Copper miniboss boundary",
                new Vector3(0f, 0.23f, 0f),
                new Vector3(22f, 0.1f, 22f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
        }

        private static void BuildBossThreshold(PieceBuilder builder)
        {
            builder.AddBox(
                "West boss-threshold pier",
                new Vector3(-4.2f, 4f, 0f),
                new Vector3(1.1f, 8f, 1.2f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddBox(
                "East boss-threshold pier",
                new Vector3(4.2f, 4f, 0f),
                new Vector3(1.1f, 8f, 1.2f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddBox(
                "Copper boss-gate header",
                new Vector3(0f, 7.5f, 0f),
                new Vector3(9.5f, 0.5f, 1.4f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
            builder.AddBox(
                "Glass-mist threshold veil",
                new Vector3(0f, 3.6f, 0.15f),
                new Vector3(7.2f, 6.8f, 0.08f),
                Quaternion.identity,
                SpireMaterialKey.GlassMist);
        }

        private static void BuildBossArenaFloor(PieceBuilder builder)
        {
            builder.AddCylinder(
                "Open boss arena floor",
                new Vector3(0f, 0.1f, 0f),
                new Vector3(32f, 0.2f, 32f),
                Quaternion.identity,
                SpireMaterialKey.DeepStone);
            var positions = new[]
            {
                new Vector3(-13.8f, 2.6f, -8f),
                new Vector3(13.8f, 2.6f, -8f),
                new Vector3(-13.8f, 2.6f, 8f),
                new Vector3(13.8f, 2.6f, 8f)
            };
            for (var index = 0; index < positions.Length; index++)
            {
                builder.AddCylinder(
                    "Boss arena side mass " + index,
                    positions[index],
                    new Vector3(2f, 5.2f, 2f),
                    Quaternion.identity,
                    SpireMaterialKey.PaleAshStone);
                builder.AddBox(
                    "Copper telegraph-safe edge marker " + index,
                    new Vector3(positions[index].x * 0.75f, 0.18f, positions[index].z * 0.75f),
                    new Vector3(0.3f, 0.16f, 2.4f),
                    Quaternion.Euler(0f, index * 90f + 45f, 0f),
                    SpireMaterialKey.Copper);
            }
        }

        private static void BuildCacheChest(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    2,
                    4,
                    2,
                    2))
            {
                builder.AddBox(
                    "Original timber cache body",
                    new Vector3(0f, 0.5f, 0f),
                    new Vector3(1.7f, 1f, 1.2f),
                    Quaternion.identity,
                    SpireMaterialKey.Timber);
            }

            builder.AddBox(
                "Copper cache band",
                new Vector3(0f, 0.55f, -0.62f),
                new Vector3(0.18f, 0.9f, 0.08f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
            builder.AddSphere(
                "Shape-readable cache latch",
                new Vector3(0f, 0.58f, -0.7f),
                new Vector3(0.28f, 0.38f, 0.18f),
                Quaternion.Euler(0f, 0f, 45f),
                SpireMaterialKey.RestrainedTeal);
        }

        private static void BuildRepairBench(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    2,
                    4,
                    3,
                    2))
            {
                builder.AddBox(
                    "Original timber repair top",
                    new Vector3(0f, 1f, 0f),
                    new Vector3(2.8f, 0.25f, 1.5f),
                    Quaternion.identity,
                    SpireMaterialKey.Timber);
                builder.AddBox(
                    "Original repair trestle",
                    new Vector3(0f, 0.48f, 0f),
                    new Vector3(2.3f, 0.85f, 0.35f),
                    Quaternion.identity,
                    SpireMaterialKey.Timber);
            }

            builder.AddBox(
                "Copper repair straightedge",
                new Vector3(-0.5f, 1.18f, 0f),
                new Vector3(1.6f, 0.08f, 0.12f),
                Quaternion.Euler(0f, 18f, 0f),
                SpireMaterialKey.Copper);
            builder.AddBox(
                "Copper repair hammer silhouette",
                new Vector3(0.65f, 1.24f, 0f),
                new Vector3(0.7f, 0.16f, 0.16f),
                Quaternion.Euler(0f, -25f, 0f),
                SpireMaterialKey.Copper);
            builder.AddBox(
                "Restrained work-ready seam",
                new Vector3(0f, 1.14f, -0.72f),
                new Vector3(1.4f, 0.06f, 0.06f),
                Quaternion.identity,
                SpireMaterialKey.RestrainedTeal);
        }

        private static void BuildWaymarkBanner(PieceBuilder builder)
        {
            builder.AddBox(
                "Timber waymark mast",
                new Vector3(-0.65f, 1.75f, 0f),
                new Vector3(0.18f, 3.5f, 0.18f),
                Quaternion.identity,
                SpireMaterialKey.Timber);
            builder.AddBox(
                "Canvas Spire waymark",
                new Vector3(0.15f, 2.45f, 0f),
                new Vector3(1.45f, 1.45f, 0.08f),
                Quaternion.Euler(0f, 0f, -4f),
                SpireMaterialKey.Canvas);
            builder.AddCylinder(
                "Copper coil crest",
                new Vector3(0.15f, 2.45f, -0.08f),
                new Vector3(0.55f, 0.08f, 0.55f),
                Quaternion.Euler(90f, 0f, 0f),
                SpireMaterialKey.Copper);
        }

        private static void BuildCoilhavenCore(PieceBuilder builder)
        {
            builder.AddCylinder(
                "Warm timber Coilhaven deck",
                new Vector3(0f, 0.2f, 0f),
                new Vector3(8f, 0.4f, 8f),
                Quaternion.identity,
                SpireMaterialKey.Timber);
            builder.AddCylinder(
                "Moss-softened rest apron",
                new Vector3(0f, 0.05f, 0f),
                new Vector3(7.5f, 0.1f, 7.5f),
                Quaternion.identity,
                SpireMaterialKey.Moss);
            builder.AddBox(
                "West canvas Coilhaven vault",
                new Vector3(-2f, 3.65f, 0f),
                new Vector3(4.1f, 0.18f, 7.2f),
                Quaternion.Euler(0f, 0f, 18f),
                SpireMaterialKey.Canvas);
            builder.AddBox(
                "East canvas Coilhaven vault",
                new Vector3(2f, 3.65f, 0f),
                new Vector3(4.1f, 0.18f, 7.2f),
                Quaternion.Euler(0f, 0f, -18f),
                SpireMaterialKey.Canvas);
            builder.AddBox(
                "North canvas Coilhaven rib",
                new Vector3(0f, 3.7f, 2.8f),
                new Vector3(7f, 0.14f, 0.2f),
                Quaternion.identity,
                SpireMaterialKey.Canvas);
            builder.AddCylinder(
                "Copper coil hearth focus",
                new Vector3(0f, 0.6f, 0f),
                new Vector3(2.4f, 1.2f, 2.4f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
            builder.AddCylinder(
                "Copper rest coil",
                new Vector3(0f, 1.25f, 0f),
                new Vector3(2.9f, 0.12f, 2.9f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
            builder.AddBox(
                "One calm Coilhaven teal seam",
                new Vector3(0f, 0.5f, -3.65f),
                new Vector3(3.4f, 0.08f, 0.08f),
                Quaternion.identity,
                SpireMaterialKey.RestrainedTeal);
        }

        private static void BuildCoilhavenBed(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    new Vector3(1.15f, 0.8f, 1.35f),
                    2,
                    4,
                    3,
                    3))
            {
                builder.AddBox(
                    "Original timber Coilhaven bed frame",
                    new Vector3(0f, 0.35f, 0f),
                    new Vector3(2.8f, 0.7f, 3.2f),
                    Quaternion.identity,
                    SpireMaterialKey.Timber);
            }

            builder.AddBox(
                "Canvas Coilhaven mattress",
                new Vector3(0f, 0.75f, 0f),
                new Vector3(2.5f, 0.32f, 2.9f),
                Quaternion.identity,
                SpireMaterialKey.Canvas);
            builder.AddBox(
                "Timber rest headboard",
                new Vector3(0f, 1f, 1.48f),
                new Vector3(2.7f, 1.1f, 0.2f),
                Quaternion.identity,
                SpireMaterialKey.Timber);
            builder.AddBox(
                "Restrained teal rest notch",
                new Vector3(0f, 1.05f, 1.36f),
                new Vector3(0.8f, 0.08f, 0.08f),
                Quaternion.identity,
                SpireMaterialKey.RestrainedTeal);
        }

        private static void BuildCoilhavenHearth(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    2,
                    4,
                    3,
                    3))
            {
                builder.AddCylinder(
                    "Original deep-stone hearth bowl",
                    new Vector3(0f, 0.65f, 0f),
                    new Vector3(1.8f, 1.3f, 1.8f),
                    Quaternion.identity,
                    SpireMaterialKey.DeepStone);
            }

            builder.AddCylinder(
                "Copper Coilhaven hearth coil",
                new Vector3(0f, 0.35f, 0f),
                new Vector3(3f, 0.18f, 3f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
            builder.AddSphere(
                "Amber retreat-safe embers",
                new Vector3(0f, 0.9f, 0f),
                new Vector3(0.9f, 0.45f, 0.9f),
                Quaternion.identity,
                SpireMaterialKey.AmberRetreat);
            builder.AddCylinder(
                "Pale-ash hearth footing",
                new Vector3(0f, 0.12f, 0f),
                new Vector3(3.4f, 0.24f, 3.4f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
        }

        private static void BuildSpirecoilStall(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    3,
                    5,
                    3,
                    3))
            {
                builder.AddBox(
                    "Original timber Spirecoil stall deck",
                    new Vector3(0f, 0.2f, 0f),
                    new Vector3(4.8f, 0.4f, 3.8f),
                    Quaternion.identity,
                    SpireMaterialKey.Timber);
                builder.AddBox(
                    "Original west stall post",
                    new Vector3(-2f, 2f, 1.4f),
                    new Vector3(0.22f, 4f, 0.22f),
                    Quaternion.identity,
                    SpireMaterialKey.Timber);
                builder.AddBox(
                    "Original east stall post",
                    new Vector3(2f, 2f, 1.4f),
                    new Vector3(0.22f, 4f, 0.22f),
                    Quaternion.identity,
                    SpireMaterialKey.Timber);
            }

            builder.AddBox(
                "Canvas Spirecoil canopy",
                new Vector3(0f, 3.65f, 0f),
                new Vector3(4.8f, 0.18f, 3.8f),
                Quaternion.Euler(0f, 0f, 3f),
                SpireMaterialKey.Canvas);
            builder.AddBox(
                "Copper Spirecoil upright west",
                new Vector3(-1.6f, 2f, -1.3f),
                new Vector3(0.14f, 3.5f, 0.14f),
                Quaternion.Euler(0f, 0f, -6f),
                SpireMaterialKey.Copper);
            builder.AddBox(
                "Copper Spirecoil upright east",
                new Vector3(1.6f, 2f, -1.3f),
                new Vector3(0.14f, 3.5f, 0.14f),
                Quaternion.Euler(0f, 0f, 6f),
                SpireMaterialKey.Copper);
        }

        private static void BuildSpirecoilCounter(PieceBuilder builder)
        {
            if (!builder.TryPreferredResource(
                    Vector3.zero,
                    Quaternion.identity,
                    new Vector3(1.15f, 0.9f, 1f),
                    2,
                    4,
                    2,
                    2))
            {
                builder.AddBox(
                    "Original timber vendor counter",
                    new Vector3(0f, 0.95f, 0f),
                    new Vector3(3.4f, 0.25f, 1.6f),
                    Quaternion.identity,
                    SpireMaterialKey.Timber);
                builder.AddBox(
                    "Original counter trestle",
                    new Vector3(0f, 0.45f, 0f),
                    new Vector3(2.8f, 0.85f, 0.4f),
                    Quaternion.identity,
                    SpireMaterialKey.Timber);
            }

            builder.AddBox(
                "Copper vendor route rail",
                new Vector3(0f, 1.12f, -0.72f),
                new Vector3(2.7f, 0.08f, 0.08f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
            builder.AddSphere(
                "Shape-readable teal vendor marker",
                new Vector3(1.25f, 1.3f, -0.72f),
                new Vector3(0.3f, 0.45f, 0.18f),
                Quaternion.Euler(0f, 0f, 45f),
                SpireMaterialKey.RestrainedTeal);
        }

        private static void BuildSpirecoilBeacon(PieceBuilder builder)
        {
            builder.AddCylinder(
                "Pale-ash no-light beacon foot",
                new Vector3(0f, 0.18f, 0f),
                new Vector3(1.2f, 0.36f, 1.2f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddBox(
                "Copper Spirecoil beacon mast",
                new Vector3(0f, 2f, 0f),
                new Vector3(0.18f, 3.7f, 0.18f),
                Quaternion.identity,
                SpireMaterialKey.Copper);
            builder.AddSphere(
                "Restrained teal beacon tip without Light",
                new Vector3(0f, 3.85f, 0f),
                new Vector3(0.55f, 0.75f, 0.55f),
                Quaternion.identity,
                SpireMaterialKey.RestrainedTeal);
        }

        private static void BuildOpenDoorFrame(
            PieceBuilder builder,
            SpireMaterialKey frameMaterial,
            SpireMaterialKey headerMaterial)
        {
            builder.AddBox(
                "Left clear-opening door post",
                new Vector3(-1.55f, 1.5f, 0f),
                new Vector3(0.55f, 3f, 0.45f),
                Quaternion.identity,
                frameMaterial);
            builder.AddBox(
                "Right clear-opening door post",
                new Vector3(1.55f, 1.5f, 0f),
                new Vector3(0.55f, 3f, 0.45f),
                Quaternion.identity,
                frameMaterial);
            builder.AddBox(
                "Clear-opening door header",
                new Vector3(0f, 2.85f, 0f),
                new Vector3(3.65f, 0.3f, 0.5f),
                Quaternion.identity,
                headerMaterial);
        }

        private static void BuildRetainingFloorAndBands(
            PieceBuilder builder,
            float width,
            SpireMaterialKey floorMaterial)
        {
            var half = width * 0.5f;
            builder.AddBox(
                "Collider-free retaining floor",
                new Vector3(0f, 0.1f, 0f),
                new Vector3(width - 0.5f, 0.2f, width - 0.5f),
                Quaternion.identity,
                floorMaterial);
            builder.AddBox(
                "North pale-ash retaining band",
                new Vector3(0f, 0.45f, half - 0.2f),
                new Vector3(width, 0.9f, 0.4f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddBox(
                "South pale-ash retaining band",
                new Vector3(0f, 0.45f, -half + 0.2f),
                new Vector3(width, 0.9f, 0.4f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddBox(
                "East pale-ash retaining band",
                new Vector3(half - 0.2f, 0.45f, 0f),
                new Vector3(0.4f, 0.9f, width - 0.8f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
            builder.AddBox(
                "West pale-ash retaining band",
                new Vector3(-half + 0.2f, 0.45f, 0f),
                new Vector3(0.4f, 0.9f, width - 0.8f),
                Quaternion.identity,
                SpireMaterialKey.PaleAshStone);
        }

        private static void BuildSockets(
            TetherfallSpireArtDefinition definition,
            Transform pieceRoot)
        {
            if (definition.RequiredSockets.Count == 0)
            {
                return;
            }

            var sockets = new GameObject(SocketRootName).transform;
            sockets.SetParent(pieceRoot, false);
            foreach (var socketName in definition.RequiredSockets)
            {
                GetSocketPose(definition, socketName, out var position, out var rotation);
                var socket = new GameObject(socketName).transform;
                socket.SetParent(sockets, false);
                socket.localPosition = position;
                socket.localRotation = rotation;
                socket.localScale = Vector3.one;
            }
        }

        private static void GetSocketPose(
            TetherfallSpireArtDefinition definition,
            string socketName,
            out Vector3 position,
            out Quaternion rotation)
        {
            var halfX = definition.Footprint.x * 0.5f;
            var halfZ = definition.Footprint.y * 0.5f;
            position = Vector3.zero;
            rotation = Quaternion.identity;

            switch (socketName)
            {
                case SnapNorth:
                    position = new Vector3(0f, 0f, halfZ);
                    return;
                case SnapEast:
                    position = new Vector3(halfX, 0f, 0f);
                    rotation = Quaternion.Euler(0f, 90f, 0f);
                    return;
                case SnapSouth:
                    position = new Vector3(0f, 0f, -halfZ);
                    rotation = Quaternion.Euler(0f, 180f, 0f);
                    return;
                case SnapWest:
                    position = new Vector3(-halfX, 0f, 0f);
                    rotation = Quaternion.Euler(0f, -90f, 0f);
                    return;
                case SnapUp:
                    position = new Vector3(0f, definition.Dimensions.y, 0f);
                    return;
                case SnapDown:
                    return;
                case SocketDoor:
                case SocketBossGate:
                    position = new Vector3(0f, 0f, halfZ);
                    return;
                case SocketStairUp:
                    position = new Vector3(0f, definition.Dimensions.y, halfZ);
                    return;
                case SocketStairDown:
                    position = new Vector3(0f, 0f, -halfZ);
                    rotation = Quaternion.Euler(0f, 180f, 0f);
                    return;
                case SocketLift:
                case SocketPuzzle:
                    return;
                case SocketTreasure:
                    position = new Vector3(0f, 0f, halfZ + 0.75f);
                    return;
                case SocketVendor:
                case SocketRest:
                case SocketWork:
                    position = new Vector3(0f, 0f, halfZ + 1.5f);
                    return;
                case SocketVfx:
                    position = new Vector3(0f, definition.Dimensions.y * 0.65f, 0f);
                    return;
                default:
                    throw new InvalidOperationException(
                        $"No socket placement rule exists for '{socketName}'.");
            }
        }

        private static TetherfallSpireArtPiece FindDirectPiece(
            Transform parent,
            TetherfallSpireArtId artId)
        {
            for (var index = 0; index < parent.childCount; index++)
            {
                var piece = parent.GetChild(index).GetComponent<TetherfallSpireArtPiece>();
                if (piece != null && piece.ArtId == artId)
                {
                    return piece;
                }
            }

            return null;
        }

        private static void ClearChildren(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                DestroyObject(root.GetChild(index).gameObject);
            }
        }

        private static Bounds CalculateLocalBounds(
            Transform root,
            IReadOnlyList<Renderer> renderers)
        {
            var hasPoint = false;
            var minimum = Vector3.zero;
            var maximum = Vector3.zero;
            for (var index = 0; index < renderers.Count; index++)
            {
                var visualRenderer = renderers[index];
                if (visualRenderer == null || !IsFinite(visualRenderer.bounds))
                {
                    continue;
                }

                var bounds = visualRenderer.localBounds;
                if (!IsFinite(bounds))
                {
                    continue;
                }

                for (var corner = 0; corner < 8; corner++)
                {
                    var rendererLocal = bounds.center + Vector3.Scale(
                        bounds.extents,
                        new Vector3(
                            (corner & 1) == 0 ? -1f : 1f,
                            (corner & 2) == 0 ? -1f : 1f,
                            (corner & 4) == 0 ? -1f : 1f));
                    var world = visualRenderer.transform.TransformPoint(rendererLocal);
                    var local = root.InverseTransformPoint(world);
                    if (!hasPoint)
                    {
                        minimum = local;
                        maximum = local;
                        hasPoint = true;
                    }
                    else
                    {
                        minimum = Vector3.Min(minimum, local);
                        maximum = Vector3.Max(maximum, local);
                    }
                }
            }

            if (!hasPoint)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            var result = new Bounds();
            result.SetMinMax(minimum, maximum);
            return result;
        }

        private static Material GetSharedMaterial(SpireMaterialKey key)
        {
            if (SharedMaterials.TryGetValue(key, out var material) && material != null)
            {
                return material;
            }

            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Infinite Spire art could not find a compatible lit shader.");
            }

            material = new Material(shader)
            {
                name = OriginalMaterialPrefix + GetMaterialDisplayName(key),
                hideFlags = HideFlags.DontSave
            };

            var color = GetMaterialColor(key);
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (key == SpireMaterialKey.RestrainedTeal ||
                key == SpireMaterialKey.AmberRetreat)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", color * 0.45f);
                }
            }

            if (key == SpireMaterialKey.GlassMist)
            {
                ConfigureTransparentMaterial(material);
            }

            SharedMaterials[key] = material;
            return material;
        }

        private static string GetMaterialDisplayName(SpireMaterialKey key)
        {
            switch (key)
            {
                case SpireMaterialKey.PaleAshStone:
                    return "Pale Ash Stone";
                case SpireMaterialKey.DeepStone:
                    return "Deep Stone";
                case SpireMaterialKey.Timber:
                    return "Weathered Timber";
                case SpireMaterialKey.Copper:
                    return "Civic Copper";
                case SpireMaterialKey.RestrainedTeal:
                    return "Restrained Teal";
                case SpireMaterialKey.Canvas:
                    return "Coilhaven Canvas";
                case SpireMaterialKey.Moss:
                    return "Contact Moss";
                case SpireMaterialKey.AmberRetreat:
                    return "Amber Retreat";
                case SpireMaterialKey.GlassMist:
                    return "Glass Mist";
                case SpireMaterialKey.ShadowProxy:
                    return "Shadow Proxy";
                default:
                    throw new ArgumentOutOfRangeException(nameof(key), key, null);
            }
        }

        private static Color GetMaterialColor(SpireMaterialKey key)
        {
            switch (key)
            {
                case SpireMaterialKey.PaleAshStone:
                    return new Color(0.58f, 0.56f, 0.49f, 1f);
                case SpireMaterialKey.DeepStone:
                    return new Color(0.18f, 0.17f, 0.16f, 1f);
                case SpireMaterialKey.Timber:
                    return new Color(0.24f, 0.14f, 0.08f, 1f);
                case SpireMaterialKey.Copper:
                    return new Color(0.68f, 0.35f, 0.15f, 1f);
                case SpireMaterialKey.RestrainedTeal:
                    return new Color(0.19f, 0.72f, 0.65f, 1f);
                case SpireMaterialKey.Canvas:
                    return new Color(0.62f, 0.53f, 0.36f, 1f);
                case SpireMaterialKey.Moss:
                    return new Color(0.31f, 0.49f, 0.25f, 1f);
                case SpireMaterialKey.AmberRetreat:
                    return new Color(0.9f, 0.55f, 0.18f, 1f);
                case SpireMaterialKey.GlassMist:
                    return new Color(0.34f, 0.72f, 0.73f, 0.26f);
                case SpireMaterialKey.ShadowProxy:
                    return new Color(0.12f, 0.14f, 0.16f, 1f);
                default:
                    return Color.magenta;
            }
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
            }
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }
            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }
            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void ConfigureRenderer(
            Renderer renderer,
            bool proxyOrMist)
        {
            renderer.enabled = true;
            renderer.shadowCastingMode = proxyOrMist
                ? ShadowCastingMode.Off
                : ShadowCastingMode.On;
            renderer.receiveShadows = !proxyOrMist;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
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

        public static bool IsFinite(Bounds value)
        {
            return IsFinite(value.center) && IsFinite(value.size);
        }

        public static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        public static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z) &&
                   IsFinite(value.w);
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private sealed class PieceBuilder
        {
            private readonly Transform parent;
            private readonly bool forceProceduralFallback;
            private readonly List<Renderer> renderers = new List<Renderer>();
            private readonly HashSet<Material> materials = new HashSet<Material>();

            public TetherfallSpireArtDefinition Definition { get; }
            public bool UsedPreferredResource { get; private set; }
            public int RendererCount => renderers.Count;
            public int SharedMaterialCount => materials.Count;
            public IReadOnlyList<Renderer> Renderers => renderers;

            public PieceBuilder(
                TetherfallSpireArtDefinition definition,
                Transform parent,
                bool forceProceduralFallback)
            {
                Definition = definition;
                this.parent = parent;
                this.forceProceduralFallback = forceProceduralFallback;
            }

            public bool TryPreferredResource(
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale,
                int maxRendererContribution,
                int maxMaterialContribution,
                int reservedRenderers,
                int reservedMaterials)
            {
                if (forceProceduralFallback || !Definition.HasPreferredResource)
                {
                    return false;
                }

                var prefab = Resources.Load<GameObject>(Definition.PreferredResourcePath);
                if (!IsSafeVisualPrefab(prefab))
                {
                    return false;
                }

                var sourceRenderers = prefab.GetComponentsInChildren<Renderer>(true);
                var sourceMaterials = new HashSet<Material>();
                foreach (var sourceRenderer in sourceRenderers)
                {
                    foreach (var sourceMaterial in sourceRenderer.sharedMaterials)
                    {
                        if (sourceMaterial == null)
                        {
                            return false;
                        }

                        sourceMaterials.Add(sourceMaterial);
                    }
                }

                if (sourceRenderers.Length == 0 ||
                    sourceRenderers.Length > maxRendererContribution ||
                    sourceMaterials.Count == 0 ||
                    sourceMaterials.Count > maxMaterialContribution ||
                    RendererCount + sourceRenderers.Length + reservedRenderers >
                    Definition.MaxRendererCount ||
                    SharedMaterialCount + CountNewMaterials(sourceMaterials) + reservedMaterials >
                    Definition.MaxSharedMaterialCount)
                {
                    return false;
                }

                var instance = Object.Instantiate(prefab, parent, false);
                instance.name = "Quaternius source · " + Definition.PreferredResourceName;
                instance.transform.localPosition = IsFinite(localPosition)
                    ? localPosition
                    : Vector3.zero;
                instance.transform.localRotation = IsFinite(localRotation)
                    ? localRotation
                    : Quaternion.identity;
                instance.transform.localScale = SanitizeScale(localScale);
                GroundInstance(instance, localPosition.y);

                if (!IsSafeVisualPrefab(instance))
                {
                    DestroyObject(instance);
                    return false;
                }

                var instanceRenderers = instance.GetComponentsInChildren<Renderer>(true);
                if (instanceRenderers.Length != sourceRenderers.Length)
                {
                    DestroyObject(instance);
                    return false;
                }

                foreach (var visualRenderer in instanceRenderers)
                {
                    ConfigureRenderer(visualRenderer, Definition.IsDistantProxy);
                    renderers.Add(visualRenderer);
                    foreach (var material in visualRenderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            materials.Add(material);
                        }
                    }
                }

                UsedPreferredResource = true;
                return true;
            }

            public GameObject AddBox(
                string name,
                Vector3 localPosition,
                Vector3 size,
                Quaternion localRotation,
                SpireMaterialKey material)
            {
                return AddPrimitive(
                    PrimitiveType.Cube,
                    name,
                    localPosition,
                    size,
                    localRotation,
                    material);
            }

            public GameObject AddCylinder(
                string name,
                Vector3 localPosition,
                Vector3 size,
                Quaternion localRotation,
                SpireMaterialKey material)
            {
                return AddPrimitive(
                    PrimitiveType.Cylinder,
                    name,
                    localPosition,
                    new Vector3(size.x, size.y * 0.5f, size.z),
                    localRotation,
                    material);
            }

            public GameObject AddSphere(
                string name,
                Vector3 localPosition,
                Vector3 size,
                Quaternion localRotation,
                SpireMaterialKey material)
            {
                return AddPrimitive(
                    PrimitiveType.Sphere,
                    name,
                    localPosition,
                    size,
                    localRotation,
                    material);
            }

            public void AddSquareRing(
                string name,
                float radius,
                float thickness,
                float height,
                float centerY,
                SpireMaterialKey material)
            {
                var diameter = radius * 2f;
                AddBox(
                    name + " north",
                    new Vector3(0f, centerY, radius),
                    new Vector3(diameter, height, thickness),
                    Quaternion.identity,
                    material);
                AddBox(
                    name + " south",
                    new Vector3(0f, centerY, -radius),
                    new Vector3(diameter, height, thickness),
                    Quaternion.identity,
                    material);
                AddBox(
                    name + " east",
                    new Vector3(radius, centerY, 0f),
                    new Vector3(thickness, height, diameter - thickness * 2f),
                    Quaternion.identity,
                    material);
                AddBox(
                    name + " west",
                    new Vector3(-radius, centerY, 0f),
                    new Vector3(thickness, height, diameter - thickness * 2f),
                    Quaternion.identity,
                    material);
            }

            public void ValidateFinalBudget()
            {
                if (RendererCount == 0)
                {
                    throw new InvalidOperationException(
                        $"Infinite Spire art '{Definition.StableId}' produced no renderers.");
                }

                if (RendererCount > Definition.MaxRendererCount)
                {
                    throw new InvalidOperationException(
                        $"Infinite Spire art '{Definition.StableId}' produced {RendererCount} " +
                        $"renderers; budget is {Definition.MaxRendererCount}.");
                }

                if (SharedMaterialCount == 0 ||
                    SharedMaterialCount > Definition.MaxSharedMaterialCount)
                {
                    throw new InvalidOperationException(
                        $"Infinite Spire art '{Definition.StableId}' uses {SharedMaterialCount} " +
                        $"shared materials; budget is {Definition.MaxSharedMaterialCount}.");
                }
            }

            private GameObject AddPrimitive(
                PrimitiveType primitiveType,
                string name,
                Vector3 localPosition,
                Vector3 localScale,
                Quaternion localRotation,
                SpireMaterialKey materialKey)
            {
                var material = GetSharedMaterial(materialKey);
                var newMaterialCount = materials.Contains(material) ? 0 : 1;
                if (RendererCount + 1 > Definition.MaxRendererCount ||
                    SharedMaterialCount + newMaterialCount >
                    Definition.MaxSharedMaterialCount)
                {
                    throw new InvalidOperationException(
                        $"Recipe for '{Definition.StableId}' exceeded its declared budget while " +
                        $"adding '{name}'.");
                }

                var visual = GameObject.CreatePrimitive(primitiveType);
                visual.name = name;
                visual.transform.SetParent(parent, false);
                visual.transform.localPosition = IsFinite(localPosition)
                    ? localPosition
                    : Vector3.zero;
                visual.transform.localScale = SanitizeScale(localScale);
                visual.transform.localRotation = IsFinite(localRotation)
                    ? localRotation
                    : Quaternion.identity;

                var visualCollider = visual.GetComponent<Collider>();
                if (visualCollider != null)
                {
                    visualCollider.enabled = false;
                    DestroyObject(visualCollider);
                }

                var visualRenderer = visual.GetComponent<Renderer>();
                visualRenderer.sharedMaterial = material;
                ConfigureRenderer(
                    visualRenderer,
                    materialKey == SpireMaterialKey.GlassMist ||
                    materialKey == SpireMaterialKey.ShadowProxy);
                renderers.Add(visualRenderer);
                materials.Add(material);
                return visual;
            }

            private int CountNewMaterials(IEnumerable<Material> candidates)
            {
                var count = 0;
                foreach (var candidate in candidates)
                {
                    if (candidate != null && !materials.Contains(candidate))
                    {
                        count++;
                    }
                }

                return count;
            }

            private void GroundInstance(GameObject instance, float requestedGroundY)
            {
                var instanceRenderers = instance.GetComponentsInChildren<Renderer>(true);
                var hasBounds = false;
                var minimumY = 0f;
                foreach (var visualRenderer in instanceRenderers)
                {
                    if (visualRenderer == null || !IsFinite(visualRenderer.bounds))
                    {
                        continue;
                    }

                    var bounds = visualRenderer.bounds;
                    for (var corner = 0; corner < 8; corner++)
                    {
                        var world = bounds.center + Vector3.Scale(
                            bounds.extents,
                            new Vector3(
                                (corner & 1) == 0 ? -1f : 1f,
                                (corner & 2) == 0 ? -1f : 1f,
                                (corner & 4) == 0 ? -1f : 1f));
                        var local = parent.InverseTransformPoint(world);
                        if (!hasBounds || local.y < minimumY)
                        {
                            minimumY = local.y;
                            hasBounds = true;
                        }
                    }
                }

                if (hasBounds)
                {
                    instance.transform.localPosition +=
                        Vector3.up * (requestedGroundY - minimumY);
                }
            }

            private static bool IsSafeVisualPrefab(GameObject prefab)
            {
                if (prefab == null ||
                    prefab.GetComponentsInChildren<Collider>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<Collider2D>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<Light>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<Camera>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<AudioListener>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<AudioSource>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<Rigidbody>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<Rigidbody2D>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<Animator>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<Animation>(true).Length > 0 ||
                    prefab.GetComponentsInChildren<ParticleSystem>(true).Length > 0)
                {
                    return false;
                }

                var scripts = prefab.GetComponentsInChildren<MonoBehaviour>(true);
                if (scripts.Length > 0)
                {
                    return false;
                }

                foreach (var value in prefab.GetComponentsInChildren<Transform>(true))
                {
                    if (!IsFinite(value.localPosition) ||
                        !IsFinite(value.localRotation) ||
                        !IsFinite(value.localScale))
                    {
                        return false;
                    }
                }

                return true;
            }

            private static Vector3 SanitizeScale(Vector3 value)
            {
                if (!IsFinite(value))
                {
                    return Vector3.one;
                }

                return new Vector3(
                    Mathf.Clamp(Mathf.Abs(value.x), 0.01f, 128f),
                    Mathf.Clamp(Mathf.Abs(value.y), 0.01f, 128f),
                    Mathf.Clamp(Mathf.Abs(value.z), 0.01f, 128f));
            }
        }
    }
}
