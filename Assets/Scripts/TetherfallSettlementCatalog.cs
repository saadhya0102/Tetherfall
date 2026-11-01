using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public enum SettlementPieceRole
    {
        ThreadheartCore = 100,
        TimberFoundation = 200,
        StoneFoundation = 201,
        StraightWall = 210,
        WindowWall = 211,
        Doorway = 212,
        Gate = 213,
        Floor = 220,
        Stair = 221,
        Ramp = 222,
        PitchedRoof = 223,
        PactBed = 300,
        WayfinderBed = 301,
        StorageChest = 310,
        ResourcePallet = 311,
        Workbench = 400,
        Forge = 401,
        Cookpot = 402,
        Loom = 403,
        MedicineTable = 404,
        Planter = 500,
        FarmPlot = 501,
        WaterCollector = 502,
        FeedingTrough = 503,
        WatchPost = 600,
        LanternPost = 601,
        Fence = 602,
        Banner = 603
    }

    public enum SettlementPieceCategory
    {
        Core,
        Structure,
        Bed,
        Storage,
        Station,
        Agriculture,
        Utility,
        Decoration
    }

    public enum SettlementRotationPolicy
    {
        Fine15Degrees,
        Structural90Degrees
    }

    public enum SettlementWorkRole
    {
        None = 0,
        Builder = 100,
        Smith = 110,
        Cook = 120,
        Weaver = 130,
        Healer = 140,
        Farmer = 200,
        WaterTender = 210,
        Rancher = 220,
        Watchkeeper = 300,
        Hauler = 310
    }

    public enum SettlementSimulationBand
    {
        Near,
        Far,
        Abstract
    }

    [Serializable]
    public sealed class SettlementPieceDefinition
    {
        public string id;
        public string displayName;
        public string artId;
        public SettlementPieceRole role;
        public SettlementPieceCategory category;
        public SettlementRotationPolicy rotationPolicy;
        public Vector3 footprint;
        public Vector3 colliderCenter;
        public int minimumCoreTier;
        public int minimumWayfinderLevel;
        public int maximumDurability;
        public int storageCapacity;
        public int bedSlots;
        public int workSlots;
        public SettlementWorkRole workRole;
        public float maximumSlopeDegrees;
        public float automaticCycleSeconds;
        public List<ResourceCost> buildCost = new List<ResourceCost>();
        public List<ResourceCost> automaticOutputs = new List<ResourceCost>();
        public int automaticFoodOutput;
        public int automaticWaterOutput;

        public float RotationStepDegrees =>
            rotationPolicy == SettlementRotationPolicy.Structural90Degrees ? 90f : 15f;

        public bool IsStructure =>
            category == SettlementPieceCategory.Core ||
            category == SettlementPieceCategory.Structure;

        public bool UsesStructuralSockets => IsStructure;

        public bool IsStation => category == SettlementPieceCategory.Station;

        public bool IsStorage => storageCapacity > 0;

        public bool IsBed => bedSlots > 0;
    }

    [Serializable]
    public sealed class SettlementRecipeDefinition
    {
        public string id;
        public string stationDefinitionId;
        public string displayName;
        public float cycleSeconds;
        public int minimumCoreTier;
        public int minimumWayfinderLevel;
        public List<ResourceCost> inputResources = new List<ResourceCost>();
        public List<ResourceCost> outputResources = new List<ResourceCost>();
        public int inputFoodUnits;
        public int inputWaterUnits;
        public int outputFoodUnits;
        public int outputWaterUnits;
        public int outputMaintenanceUnits;
        public float workerMoraleReward;

        public bool HasOutput =>
            outputResources.Count > 0 ||
            outputFoodUnits > 0 ||
            outputWaterUnits > 0 ||
            outputMaintenanceUnits > 0 ||
            workerMoraleReward > 0f;
    }

    [Serializable]
    public sealed class SettlementCoreUpgradeDefinition
    {
        public int tier;
        public string displayName;
        public int minimumWayfinderLevel;
        public float buildRadius;
        public int pieceCap;
        public int workerCap;
        public int baseStorageCapacity;
        public List<ResourceCost> cost = new List<ResourceCost>();
    }

    public static class TetherfallSettlementCatalog
    {
        public const string PlayerSettlementId = "settlement.player.threadheart";
        public const string ThreadheartCoreDefinitionId = "settlement.threadheart.core";
        public const string ThreadheartCoreInstanceId = "settlement.instance.threadheart.core";
        public const string WorkbenchDefinitionId = "settlement.station.workbench";
        public const string ForgeDefinitionId = "settlement.station.forge";
        public const string CookpotDefinitionId = "settlement.station.cookpot";
        public const string LoomDefinitionId = "settlement.station.loom";
        public const string MedicineTableDefinitionId = "settlement.station.medicine_table";

        private static readonly List<SettlementPieceDefinition> Pieces = BuildPieces();
        private static readonly List<SettlementRecipeDefinition> Recipes = BuildRecipes();
        private static readonly List<SettlementCoreUpgradeDefinition> CoreUpgrades = BuildCoreUpgrades();

        public static IReadOnlyList<SettlementPieceDefinition> AllPieces => Pieces;
        public static IReadOnlyList<SettlementRecipeDefinition> AllRecipes => Recipes;
        public static IReadOnlyList<SettlementCoreUpgradeDefinition> AllCoreUpgrades => CoreUpgrades;

        public static SettlementPieceDefinition FindPiece(string definitionId)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                return null;
            }

            return Pieces.Find(item =>
                item != null &&
                string.Equals(item.id, definitionId, StringComparison.Ordinal));
        }

        public static SettlementPieceDefinition FindPiece(SettlementPieceRole role)
        {
            return Pieces.Find(item => item != null && item.role == role);
        }

        public static SettlementRecipeDefinition FindRecipe(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                return null;
            }

            return Recipes.Find(item =>
                item != null &&
                string.Equals(item.id, recipeId, StringComparison.Ordinal));
        }

        public static List<SettlementRecipeDefinition> GetRecipesForStation(string stationDefinitionId)
        {
            var matches = new List<SettlementRecipeDefinition>();
            if (string.IsNullOrWhiteSpace(stationDefinitionId))
            {
                return matches;
            }

            foreach (var recipe in Recipes)
            {
                if (recipe != null &&
                    string.Equals(
                        recipe.stationDefinitionId,
                        stationDefinitionId,
                        StringComparison.Ordinal))
                {
                    matches.Add(recipe);
                }
            }

            return matches;
        }

        public static SettlementCoreUpgradeDefinition GetCoreTier(int tier)
        {
            return CoreUpgrades.Find(item => item != null && item.tier == tier);
        }

        private static List<SettlementPieceDefinition> BuildPieces()
        {
            var pieces = new List<SettlementPieceDefinition>
            {
                Piece(
                    ThreadheartCoreDefinitionId,
                    "Threadheart settlement core",
                    "alderwake.threadheart_core",
                    SettlementPieceRole.ThreadheartCore,
                    SettlementPieceCategory.Core,
                    new Vector3(6f, 4f, 6f),
                    new Vector3(0f, 2f, 0f),
                    0,
                    1,
                    600,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Timber, 8, ResourceKind.Stone, 6, ResourceKind.Chimesalt, 2),
                    storage: 200,
                    slope: 12f),
                Piece(
                    "settlement.foundation.timber",
                    "Timber foundation",
                    "alderwake.foundation.timber",
                    SettlementPieceRole.TimberFoundation,
                    SettlementPieceCategory.Structure,
                    new Vector3(4f, 0.5f, 4f),
                    new Vector3(0f, 0.25f, 0f),
                    1,
                    1,
                    180,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Timber, 4, ResourceKind.Resin, 1),
                    slope: 16f),
                Piece(
                    "settlement.foundation.stone",
                    "Stone foundation",
                    "alderwake.foundation.stone",
                    SettlementPieceRole.StoneFoundation,
                    SettlementPieceCategory.Structure,
                    new Vector3(4f, 0.6f, 4f),
                    new Vector3(0f, 0.3f, 0f),
                    1,
                    2,
                    280,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Stone, 5, ResourceKind.Resin, 1),
                    slope: 16f),
                Piece(
                    "settlement.wall.straight",
                    "Straight wall",
                    "alderwake.wall.straight",
                    SettlementPieceRole.StraightWall,
                    SettlementPieceCategory.Structure,
                    new Vector3(4f, 3f, 0.4f),
                    new Vector3(0f, 1.5f, 0f),
                    1,
                    1,
                    150,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Timber, 3, ResourceKind.Fiber, 1)),
                Piece(
                    "settlement.wall.window",
                    "Window wall",
                    "alderwake.wall.window",
                    SettlementPieceRole.WindowWall,
                    SettlementPieceCategory.Structure,
                    new Vector3(4f, 3f, 0.4f),
                    new Vector3(0f, 1.5f, 0f),
                    1,
                    2,
                    145,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Timber, 3, ResourceKind.Fiber, 1, ResourceKind.Resin, 1)),
                Piece(
                    "settlement.wall.doorway",
                    "Doorway",
                    "alderwake.wall.doorway",
                    SettlementPieceRole.Doorway,
                    SettlementPieceCategory.Structure,
                    new Vector3(4f, 3f, 0.4f),
                    new Vector3(0f, 1.5f, 0f),
                    1,
                    2,
                    150,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Timber, 4, ResourceKind.Fiber, 1)),
                Piece(
                    "settlement.wall.gate",
                    "Gate",
                    "alderwake.wall.gate",
                    SettlementPieceRole.Gate,
                    SettlementPieceCategory.Structure,
                    new Vector3(4f, 3.2f, 0.55f),
                    new Vector3(0f, 1.6f, 0f),
                    2,
                    4,
                    300,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Timber, 6, ResourceKind.Stone, 2, ResourceKind.Ore, 2)),
                Piece(
                    "settlement.floor.timber",
                    "Floor",
                    "alderwake.floor",
                    SettlementPieceRole.Floor,
                    SettlementPieceCategory.Structure,
                    new Vector3(4f, 0.25f, 4f),
                    new Vector3(0f, 0.125f, 0f),
                    1,
                    1,
                    120,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Timber, 3, ResourceKind.Resin, 1)),
                Piece(
                    "settlement.stair.timber",
                    "Stair",
                    "alderwake.stair",
                    SettlementPieceRole.Stair,
                    SettlementPieceCategory.Structure,
                    new Vector3(4f, 2.4f, 4f),
                    new Vector3(0f, 1.2f, 0f),
                    1,
                    2,
                    170,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Timber, 5, ResourceKind.Resin, 1),
                    slope: 24f),
                Piece(
                    "settlement.ramp.timber",
                    "Ramp",
                    "alderwake.ramp",
                    SettlementPieceRole.Ramp,
                    SettlementPieceCategory.Structure,
                    new Vector3(4f, 1.5f, 4f),
                    new Vector3(0f, 0.75f, 0f),
                    1,
                    2,
                    160,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Timber, 4, ResourceKind.Resin, 1),
                    slope: 24f),
                Piece(
                    "settlement.roof.pitched",
                    "Pitched roof",
                    "alderwake.roof.pitched",
                    SettlementPieceRole.PitchedRoof,
                    SettlementPieceCategory.Structure,
                    new Vector3(4f, 1.5f, 4f),
                    new Vector3(0f, 0.75f, 0f),
                    1,
                    2,
                    140,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Timber, 4, ResourceKind.Fiber, 2, ResourceKind.Resin, 1)),
                Piece(
                    "settlement.bed.pact",
                    "Pact bed",
                    "alderwake.bed.pact",
                    SettlementPieceRole.PactBed,
                    SettlementPieceCategory.Bed,
                    new Vector3(2f, 0.75f, 2.8f),
                    new Vector3(0f, 0.375f, 0f),
                    1,
                    2,
                    100,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Timber, 2, ResourceKind.Fiber, 3, ResourceKind.Herbs, 1),
                    beds: 1),
                Piece(
                    "settlement.bed.wayfinder",
                    "Wayfinder bed",
                    "alderwake.bed.wayfinder",
                    SettlementPieceRole.WayfinderBed,
                    SettlementPieceCategory.Bed,
                    new Vector3(2f, 0.7f, 2.8f),
                    new Vector3(0f, 0.35f, 0f),
                    1,
                    3,
                    110,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Timber, 2, ResourceKind.Fiber, 4, ResourceKind.Herbs, 1)),
                Piece(
                    "settlement.storage.chest",
                    "Storage chest",
                    "alderwake.storage.chest",
                    SettlementPieceRole.StorageChest,
                    SettlementPieceCategory.Storage,
                    new Vector3(1.6f, 1.2f, 1.1f),
                    new Vector3(0f, 0.6f, 0f),
                    1,
                    1,
                    140,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Timber, 4, ResourceKind.Ore, 1),
                    storage: 120),
                Piece(
                    "settlement.storage.pallet",
                    "Resource pallet",
                    "alderwake.storage.pallet",
                    SettlementPieceRole.ResourcePallet,
                    SettlementPieceCategory.Storage,
                    new Vector3(2.4f, 1.2f, 2.4f),
                    new Vector3(0f, 0.6f, 0f),
                    1,
                    2,
                    160,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Timber, 5, ResourceKind.Fiber, 2),
                    storage: 220),
                Piece(
                    WorkbenchDefinitionId,
                    "Workbench",
                    "alderwake.station.workbench",
                    SettlementPieceRole.Workbench,
                    SettlementPieceCategory.Station,
                    new Vector3(3f, 1.6f, 2f),
                    new Vector3(0f, 0.8f, 0f),
                    1,
                    2,
                    220,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Timber, 6, ResourceKind.Stone, 2, ResourceKind.Resin, 2),
                    workSlots: 1,
                    workRole: SettlementWorkRole.Builder),
                Piece(
                    ForgeDefinitionId,
                    "Forge",
                    "alderwake.station.forge",
                    SettlementPieceRole.Forge,
                    SettlementPieceCategory.Station,
                    new Vector3(3.4f, 2f, 2.8f),
                    new Vector3(0f, 1f, 0f),
                    2,
                    4,
                    360,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Stone, 8, ResourceKind.Ore, 5, ResourceKind.Timber, 3),
                    workSlots: 1,
                    workRole: SettlementWorkRole.Smith),
                Piece(
                    CookpotDefinitionId,
                    "Cookpot",
                    "alderwake.station.cookpot",
                    SettlementPieceRole.Cookpot,
                    SettlementPieceCategory.Station,
                    new Vector3(2.2f, 1.4f, 2.2f),
                    new Vector3(0f, 0.7f, 0f),
                    1,
                    2,
                    170,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Stone, 4, ResourceKind.Timber, 2, ResourceKind.Ore, 1),
                    workSlots: 1,
                    workRole: SettlementWorkRole.Cook),
                Piece(
                    LoomDefinitionId,
                    "Loom",
                    "alderwake.station.loom",
                    SettlementPieceRole.Loom,
                    SettlementPieceCategory.Station,
                    new Vector3(3f, 2.2f, 1.5f),
                    new Vector3(0f, 1.1f, 0f),
                    2,
                    4,
                    200,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Timber, 5, ResourceKind.Fiber, 4, ResourceKind.Resin, 1),
                    workSlots: 1,
                    workRole: SettlementWorkRole.Weaver),
                Piece(
                    MedicineTableDefinitionId,
                    "Medicine table",
                    "alderwake.station.medicine",
                    SettlementPieceRole.MedicineTable,
                    SettlementPieceCategory.Station,
                    new Vector3(2.8f, 1.5f, 1.8f),
                    new Vector3(0f, 0.75f, 0f),
                    2,
                    5,
                    180,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Timber, 4, ResourceKind.Herbs, 5, ResourceKind.Resin, 2),
                    workSlots: 1,
                    workRole: SettlementWorkRole.Healer),
                Piece(
                    "settlement.agriculture.planter",
                    "Planter",
                    "alderwake.growing.planter",
                    SettlementPieceRole.Planter,
                    SettlementPieceCategory.Agriculture,
                    new Vector3(2f, 0.8f, 2f),
                    new Vector3(0f, 0.4f, 0f),
                    1,
                    1,
                    100,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Timber, 2, ResourceKind.Stone, 1, ResourceKind.Herbs, 1),
                    workSlots: 1,
                    workRole: SettlementWorkRole.Farmer,
                    cycle: 120f,
                    outputs: Cost(ResourceKind.Herbs, 1),
                    slope: 10f),
                Piece(
                    "settlement.agriculture.farm_plot",
                    "Farm plot",
                    "alderwake.growing.farm_plot",
                    SettlementPieceRole.FarmPlot,
                    SettlementPieceCategory.Agriculture,
                    new Vector3(4f, 0.35f, 4f),
                    new Vector3(0f, 0.175f, 0f),
                    1,
                    2,
                    120,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Timber, 3, ResourceKind.Stone, 2, ResourceKind.Herbs, 2),
                    workSlots: 2,
                    workRole: SettlementWorkRole.Farmer,
                    cycle: 90f,
                    outputs: Cost(ResourceKind.Herbs, 2, ResourceKind.Fiber, 1),
                    slope: 8f),
                Piece(
                    "settlement.agriculture.water_collector",
                    "Water collector",
                    "alderwake.growing.water_collector",
                    SettlementPieceRole.WaterCollector,
                    SettlementPieceCategory.Agriculture,
                    new Vector3(2.6f, 2.4f, 2.6f),
                    new Vector3(0f, 1.2f, 0f),
                    1,
                    3,
                    150,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Timber, 4, ResourceKind.Fiber, 3, ResourceKind.Resin, 2),
                    workSlots: 1,
                    workRole: SettlementWorkRole.WaterTender,
                    cycle: 120f,
                    waterOutput: 2,
                    slope: 10f),
                Piece(
                    "settlement.agriculture.feeding_trough",
                    "Feeding trough",
                    "alderwake.growing.feeding_trough",
                    SettlementPieceRole.FeedingTrough,
                    SettlementPieceCategory.Agriculture,
                    new Vector3(3f, 0.9f, 1.2f),
                    new Vector3(0f, 0.45f, 0f),
                    1,
                    3,
                    140,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Timber, 4, ResourceKind.Ore, 1),
                    workSlots: 1,
                    workRole: SettlementWorkRole.Rancher),
                Piece(
                    "settlement.utility.watch_post",
                    "Watch post",
                    "alderwake.utility.watch_post",
                    SettlementPieceRole.WatchPost,
                    SettlementPieceCategory.Utility,
                    new Vector3(3.5f, 4.2f, 3.5f),
                    new Vector3(0f, 2.1f, 0f),
                    2,
                    4,
                    260,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Timber, 8, ResourceKind.Stone, 3, ResourceKind.Fiber, 2),
                    workSlots: 1,
                    workRole: SettlementWorkRole.Watchkeeper),
                Piece(
                    "settlement.utility.lantern_post",
                    "Lantern post",
                    "alderwake.utility.lantern_post",
                    SettlementPieceRole.LanternPost,
                    SettlementPieceCategory.Utility,
                    new Vector3(0.8f, 3.2f, 0.8f),
                    new Vector3(0f, 1.6f, 0f),
                    1,
                    2,
                    100,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Timber, 2, ResourceKind.Ore, 1, ResourceKind.Resin, 1)),
                Piece(
                    "settlement.utility.fence",
                    "Fence",
                    "alderwake.utility.fence",
                    SettlementPieceRole.Fence,
                    SettlementPieceCategory.Structure,
                    new Vector3(4f, 1.3f, 0.3f),
                    new Vector3(0f, 0.65f, 0f),
                    1,
                    1,
                    100,
                    SettlementRotationPolicy.Structural90Degrees,
                    Cost(ResourceKind.Timber, 2, ResourceKind.Fiber, 1)),
                Piece(
                    "settlement.decoration.banner",
                    "Banner",
                    "alderwake.utility.banner",
                    SettlementPieceRole.Banner,
                    SettlementPieceCategory.Decoration,
                    new Vector3(1f, 3f, 0.5f),
                    new Vector3(0f, 1.5f, 0f),
                    1,
                    2,
                    80,
                    SettlementRotationPolicy.Fine15Degrees,
                    Cost(ResourceKind.Timber, 1, ResourceKind.Fiber, 2, ResourceKind.Resin, 1))
            };

            return pieces;
        }

        private static List<SettlementRecipeDefinition> BuildRecipes()
        {
            return new List<SettlementRecipeDefinition>
            {
                Recipe(
                    "settlement.recipe.workbench.repair_kits",
                    WorkbenchDefinitionId,
                    "Joinery repair kits",
                    35f,
                    Cost(ResourceKind.Timber, 2, ResourceKind.Resin, 1),
                    null,
                    maintenanceOutput: 4),
                Recipe(
                    "settlement.recipe.workbench.reclaimed_beams",
                    WorkbenchDefinitionId,
                    "Reclaimed beams",
                    42f,
                    Cost(ResourceKind.Stone, 2, ResourceKind.Resin, 1),
                    Cost(ResourceKind.Timber, 3)),
                Recipe(
                    "settlement.recipe.forge.copper_fittings",
                    ForgeDefinitionId,
                    "Copper fittings",
                    48f,
                    Cost(ResourceKind.Ore, 3, ResourceKind.Timber, 1),
                    null,
                    maintenanceOutput: 7),
                Recipe(
                    "settlement.recipe.forge.threadglass_tools",
                    ForgeDefinitionId,
                    "Threadglass tools",
                    60f,
                    Cost(ResourceKind.Ore, 2, ResourceKind.Chimesalt, 1),
                    null,
                    maintenanceOutput: 6,
                    moraleReward: 1f),
                Recipe(
                    "settlement.recipe.cookpot.hearth_stew",
                    CookpotDefinitionId,
                    "Hearth stew",
                    32f,
                    Cost(ResourceKind.Herbs, 2),
                    null,
                    waterInput: 1,
                    foodOutput: 6,
                    moraleReward: 2f),
                Recipe(
                    "settlement.recipe.cookpot.trail_cakes",
                    CookpotDefinitionId,
                    "Trail cakes",
                    38f,
                    Cost(ResourceKind.Herbs, 1, ResourceKind.Fiber, 1),
                    null,
                    waterInput: 1,
                    foodOutput: 4),
                Recipe(
                    "settlement.recipe.loom.pact_bedding",
                    LoomDefinitionId,
                    "Pact bedding",
                    44f,
                    Cost(ResourceKind.Fiber, 3, ResourceKind.Resin, 1),
                    null,
                    maintenanceOutput: 2,
                    moraleReward: 5f),
                Recipe(
                    "settlement.recipe.loom.weather_canvas",
                    LoomDefinitionId,
                    "Weather canvas",
                    40f,
                    Cost(ResourceKind.Fiber, 2, ResourceKind.Resin, 1),
                    Cost(ResourceKind.Fiber, 1),
                    maintenanceOutput: 3),
                Recipe(
                    "settlement.recipe.medicine.restorative",
                    MedicineTableDefinitionId,
                    "Herbal restorative",
                    50f,
                    Cost(ResourceKind.Herbs, 3, ResourceKind.Resin, 1),
                    null,
                    waterInput: 1,
                    moraleReward: 8f),
                Recipe(
                    "settlement.recipe.medicine.salves",
                    MedicineTableDefinitionId,
                    "Field salves",
                    45f,
                    Cost(ResourceKind.Herbs, 2, ResourceKind.Fiber, 1),
                    null,
                    maintenanceOutput: 4,
                    moraleReward: 3f)
            };
        }

        private static List<SettlementCoreUpgradeDefinition> BuildCoreUpgrades()
        {
            return new List<SettlementCoreUpgradeDefinition>
            {
                new SettlementCoreUpgradeDefinition
                {
                    tier = 1,
                    displayName = "Claimed Threadheart",
                    minimumWayfinderLevel = 1,
                    buildRadius = 48f,
                    pieceCap = 96,
                    workerCap = 8,
                    baseStorageCapacity = 200
                },
                new SettlementCoreUpgradeDefinition
                {
                    tier = 2,
                    displayName = "Braided Threadheart",
                    minimumWayfinderLevel = 5,
                    buildRadius = 64f,
                    pieceCap = 160,
                    workerCap = 16,
                    baseStorageCapacity = 320,
                    cost = Cost(ResourceKind.Timber, 18, ResourceKind.Stone, 14, ResourceKind.Ore, 6, ResourceKind.Chimesalt, 3)
                },
                new SettlementCoreUpgradeDefinition
                {
                    tier = 3,
                    displayName = "Resonant Threadheart",
                    minimumWayfinderLevel = 9,
                    buildRadius = 80f,
                    pieceCap = TetherfallSettlementRules.HardPieceCap,
                    workerCap = TetherfallSettlementRules.HardWorkerCap,
                    baseStorageCapacity = 500,
                    cost = Cost(ResourceKind.Timber, 28, ResourceKind.Stone, 24, ResourceKind.Ore, 12, ResourceKind.Chimesalt, 7)
                }
            };
        }

        private static SettlementPieceDefinition Piece(
            string id,
            string displayName,
            string artId,
            SettlementPieceRole role,
            SettlementPieceCategory category,
            Vector3 footprint,
            Vector3 colliderCenter,
            int minimumCoreTier,
            int minimumWayfinderLevel,
            int maximumDurability,
            SettlementRotationPolicy rotation,
            List<ResourceCost> cost,
            int storage = 0,
            int beds = 0,
            int workSlots = 0,
            SettlementWorkRole workRole = SettlementWorkRole.None,
            float cycle = 0f,
            List<ResourceCost> outputs = null,
            int foodOutput = 0,
            int waterOutput = 0,
            float slope = 22f)
        {
            return new SettlementPieceDefinition
            {
                id = id,
                displayName = displayName,
                artId = artId,
                role = role,
                category = category,
                footprint = footprint,
                colliderCenter = colliderCenter,
                minimumCoreTier = minimumCoreTier,
                minimumWayfinderLevel = minimumWayfinderLevel,
                maximumDurability = maximumDurability,
                rotationPolicy = rotation,
                buildCost = cost ?? new List<ResourceCost>(),
                storageCapacity = storage,
                bedSlots = beds,
                workSlots = workSlots,
                workRole = workRole,
                automaticCycleSeconds = cycle,
                automaticOutputs = outputs ?? new List<ResourceCost>(),
                automaticFoodOutput = foodOutput,
                automaticWaterOutput = waterOutput,
                maximumSlopeDegrees = slope
            };
        }

        private static SettlementRecipeDefinition Recipe(
            string id,
            string stationId,
            string displayName,
            float seconds,
            List<ResourceCost> inputs,
            List<ResourceCost> outputs,
            int foodInput = 0,
            int waterInput = 0,
            int foodOutput = 0,
            int waterOutput = 0,
            int maintenanceOutput = 0,
            float moraleReward = 0f)
        {
            var station = Pieces.Find(candidate =>
                candidate != null &&
                string.Equals(candidate.id, stationId, StringComparison.Ordinal));
            return new SettlementRecipeDefinition
            {
                id = id,
                stationDefinitionId = stationId,
                displayName = displayName,
                cycleSeconds = seconds,
                minimumCoreTier = station != null
                    ? station.minimumCoreTier
                    : 1,
                minimumWayfinderLevel = station != null
                    ? station.minimumWayfinderLevel
                    : 1,
                inputResources = inputs ?? new List<ResourceCost>(),
                outputResources = outputs ?? new List<ResourceCost>(),
                inputFoodUnits = foodInput,
                inputWaterUnits = waterInput,
                outputFoodUnits = foodOutput,
                outputWaterUnits = waterOutput,
                outputMaintenanceUnits = maintenanceOutput,
                workerMoraleReward = moraleReward
            };
        }

        private static List<ResourceCost> Cost(params object[] values)
        {
            var costs = new List<ResourceCost>();
            for (var index = 0; index + 1 < values.Length; index += 2)
            {
                if (values[index] is ResourceKind kind && values[index + 1] is int amount)
                {
                    costs.Add(new ResourceCost(kind, amount));
                }
            }

            return costs;
        }
    }

    public static class TetherfallSettlementRules
    {
        public const float GridSize = 2f;
        public const float InitialBuildRadius = 48f;
        public const float MaximumBuildRadius = 80f;
        public const float NearSimulationDistance = 110f;
        public const float AbstractSimulationDistance = 180f;
        public const float NearTickSeconds = 1f;
        public const float FarTickSeconds = 10f;
        public const double MaximumOfflineSeconds = 8d * 60d * 60d;
        public const int MaximumCyclesPerAdvance = 2048;
        public const int MaximumCyclesPerWorkerAdvance = 256;
        public const int HardPieceCap = 256;
        public const int HardWorkerCap = 24;
        public const int HardStorageCap = 4000;
        public const int HardQueuedCyclesPerStation = 100;
        public const int HardSpecialStockCap = 1000;
        public const float DemolitionRefundFraction = 0.6f;
        public const float MaximumWorldCoordinate = 10000f;

        public static float GetBuildRadius(int coreTier)
        {
            var tier = TetherfallSettlementCatalog.GetCoreTier(Mathf.Clamp(coreTier, 1, 3));
            return Mathf.Clamp(
                tier != null ? tier.buildRadius : InitialBuildRadius,
                InitialBuildRadius,
                MaximumBuildRadius);
        }

        public static int GetPieceCap(int coreTier)
        {
            var tier = TetherfallSettlementCatalog.GetCoreTier(Mathf.Clamp(coreTier, 1, 3));
            return Mathf.Clamp(tier != null ? tier.pieceCap : 96, 1, HardPieceCap);
        }

        public static int GetWorkerCap(int coreTier)
        {
            var tier = TetherfallSettlementCatalog.GetCoreTier(Mathf.Clamp(coreTier, 1, 3));
            return Mathf.Clamp(tier != null ? tier.workerCap : 8, 0, HardWorkerCap);
        }

        public static int GetBaseStorageCapacity(int coreTier)
        {
            var tier = TetherfallSettlementCatalog.GetCoreTier(Mathf.Clamp(coreTier, 1, 3));
            return Mathf.Clamp(tier != null ? tier.baseStorageCapacity : 200, 0, HardStorageCap);
        }

        public static SettlementSimulationBand GetSimulationBand(float distanceToCore)
        {
            if (!IsFinite(distanceToCore) || distanceToCore > AbstractSimulationDistance)
            {
                return SettlementSimulationBand.Abstract;
            }

            return distanceToCore > NearSimulationDistance
                ? SettlementSimulationBand.Far
                : SettlementSimulationBand.Near;
        }

        public static float GetTickSeconds(SettlementSimulationBand band)
        {
            return band == SettlementSimulationBand.Near ? NearTickSeconds : FarTickSeconds;
        }

        public static double ClampOfflineSeconds(long previousUtcTicks, long nowUtcTicks)
        {
            if (previousUtcTicks <= 0L || nowUtcTicks <= previousUtcTicks)
            {
                return 0d;
            }

            var seconds = (nowUtcTicks - previousUtcTicks) / (double)TimeSpan.TicksPerSecond;
            return Math.Max(0d, Math.Min(MaximumOfflineSeconds, seconds));
        }

        public static int GetBoundedCycleCount(double accumulatedSeconds, float cycleSeconds)
        {
            if (double.IsNaN(accumulatedSeconds) ||
                double.IsInfinity(accumulatedSeconds) ||
                accumulatedSeconds <= 0d ||
                !IsFinite(cycleSeconds) ||
                cycleSeconds <= 0f)
            {
                return 0;
            }

            var rawCycles = Math.Floor(accumulatedSeconds / cycleSeconds);
            if (rawCycles >= MaximumCyclesPerAdvance)
            {
                return MaximumCyclesPerAdvance;
            }

            return rawCycles <= 0d ? 0 : (int)rawCycles;
        }

        public static Vector3 SnapToGrid(Vector3 worldPosition, Vector3 origin)
        {
            var local = worldPosition - origin;
            local.x = Mathf.Round(local.x / GridSize) * GridSize;
            local.z = Mathf.Round(local.z / GridSize) * GridSize;
            return origin + local;
        }

        public static float NormalizeRotation(float rotation, SettlementRotationPolicy policy)
        {
            if (!IsFinite(rotation))
            {
                return 0f;
            }

            var step = policy == SettlementRotationPolicy.Structural90Degrees ? 90f : 15f;
            var normalized = Mathf.Repeat(rotation, 360f);
            return Mathf.Repeat(Mathf.Round(normalized / step) * step, 360f);
        }

        public static List<ResourceCost> CalculateDemolitionRefund(IEnumerable<ResourceCost> costs)
        {
            var totals = AggregateCosts(costs);
            var refund = new List<ResourceCost>();
            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                if (!totals.TryGetValue(kind, out var amount))
                {
                    continue;
                }

                var returned = Mathf.FloorToInt(Mathf.Max(0, amount) * DemolitionRefundFraction);
                if (returned > 0)
                {
                    refund.Add(new ResourceCost(kind, returned));
                }
            }

            return refund;
        }

        public static List<ResourceCost> CalculateRepairCost(
            SettlementPieceDefinition definition,
            int currentDurability)
        {
            var result = new List<ResourceCost>();
            if (definition == null || definition.maximumDurability <= 0)
            {
                return result;
            }

            var missingFraction = 1f -
                                  Mathf.Clamp01(
                                      currentDurability /
                                      (float)definition.maximumDurability);
            if (missingFraction <= 0f)
            {
                return result;
            }

            var totals = AggregateCosts(definition.buildCost);
            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                if (!totals.TryGetValue(kind, out var amount) || amount <= 0)
                {
                    continue;
                }

                var repairAmount = Mathf.Max(
                    1,
                    Mathf.CeilToInt(amount * 0.25f * missingFraction));
                result.Add(new ResourceCost(kind, repairAmount));
            }

            return result;
        }

        public static Dictionary<ResourceKind, int> AggregateCosts(IEnumerable<ResourceCost> costs)
        {
            var totals = new Dictionary<ResourceKind, int>();
            if (costs == null)
            {
                return totals;
            }

            foreach (var cost in costs)
            {
                if (cost == null ||
                    !Enum.IsDefined(typeof(ResourceKind), cost.kind) ||
                    cost.amount <= 0)
                {
                    continue;
                }

                totals.TryGetValue(cost.kind, out var current);
                totals[cost.kind] = (int)Math.Min(
                    int.MaxValue,
                    (long)current + cost.amount);
            }

            return totals;
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        public static bool IsBoundedWorldPosition(Vector3 value)
        {
            return IsFinite(value) &&
                   Mathf.Abs(value.x) <= MaximumWorldCoordinate &&
                   Mathf.Abs(value.y) <= MaximumWorldCoordinate &&
                   Mathf.Abs(value.z) <= MaximumWorldCoordinate;
        }
    }

    public static class TetherfallSettlementForbiddenZones
    {
        public const float CityClearance = 44f;
        public const float ThreadwayArrivalClearance = 18f;
        public const float RestorationClearance = 30f;
        public const float EncounterClearance = 24f;
        public const float DungeonGateClearance = 30f;
        public const float DungeonInteriorPadding = 8f;

        private static readonly Vector3 AlderwakeRestorationCenter =
            new Vector3(-8f, 0f, 4f);
        private static readonly Vector3 SkyvaultGateCenter =
            new Vector3(121f, 0f, 31f);
        private static readonly Vector3 SkyvaultInteriorCenter =
            new Vector3(0f, 0f, 225f);
        private static readonly Vector2 SkyvaultInteriorSize =
            new Vector2(62f, 96f);

        private static readonly Vector3[] LegacyEncounterCenters =
        {
            new Vector3(-38f, 0f, 33f),
            new Vector3(169f, 0f, -28f),
            new Vector3(-171f, 0f, -31f),
            new Vector3(48f, 0f, 36f),
            new Vector3(118f, 0f, 43f),
            new Vector3(-126f, 0f, -41f)
        };

        private static readonly Vector3[] StoryEncounterCenters =
        {
            MainStoryCatalog.AlderwakeDefenseCenter,
            MainStoryCatalog.ObservatoryCenter,
            MainStoryCatalog.CinderwoldBreachCenter
        };

        public static bool CanClaimCoreAt(Vector3 position, out string failure)
        {
            failure = string.Empty;
            if (!TetherfallSettlementRules.IsBoundedWorldPosition(position))
            {
                failure = "The Threadheart core is outside the finite world bound.";
                return false;
            }

            if (IsWithinPlanarClearance(
                    position,
                    CityCatalog.AlderwakeHearthmarket.center,
                    CityClearance))
            {
                failure = "The Threadheart core must be outside Alderwake Hearthmarket.";
                return false;
            }

            if (IsWithinPlanarClearance(
                    position,
                    CityCatalog.VeyrwaterMooring.center,
                    CityClearance))
            {
                failure = "The Threadheart core must be outside Veyrwater Mooring.";
                return false;
            }

            foreach (var threadway in ThreadwayCatalog.All)
            {
                if (threadway != null &&
                    IsWithinPlanarClearance(
                        position,
                        threadway.arrivalPosition,
                        ThreadwayArrivalClearance))
                {
                    failure =
                        $"Keep the Threadheart clear of the {threadway.title} arrival route.";
                    return false;
                }
            }

            if (IsWithinPlanarClearance(
                    position,
                    AlderwakeRestorationCenter,
                    RestorationClearance))
            {
                failure =
                    "The Threadheart cannot overlap Alderwake's route-station restoration.";
                return false;
            }

            foreach (var restoration in RestorationCatalog.All)
            {
                if (restoration != null &&
                    IsWithinPlanarClearance(
                        position,
                        restoration.position,
                        RestorationClearance))
                {
                    failure =
                        $"The Threadheart cannot overlap {restoration.displayName}'s restoration footprint.";
                    return false;
                }
            }

            if (IsWithinPlanarClearance(
                    position,
                    CinderwoldArcCatalog.RelayPosition,
                    RestorationClearance))
            {
                failure =
                    "The Threadheart cannot overlap the Ember Relay story footprint.";
                return false;
            }

            foreach (var dungeon in DungeonCatalog.All)
            {
                if (dungeon == null)
                {
                    continue;
                }

                if (IsWithinPlanarClearance(
                        position,
                        dungeon.gatePosition,
                        DungeonGateClearance) ||
                    IsInsideHorizontalBounds(
                        position,
                        dungeon.interiorOrigin + dungeon.localBoundsCenter,
                        new Vector2(
                            dungeon.localBoundsSize.x,
                            dungeon.localBoundsSize.z),
                        DungeonInteriorPadding))
                {
                    failure =
                        $"The Threadheart cannot overlap {dungeon.displayName}.";
                    return false;
                }
            }

            if (IsWithinPlanarClearance(
                    position,
                    SkyvaultGateCenter,
                    DungeonGateClearance) ||
                IsInsideHorizontalBounds(
                    position,
                    SkyvaultInteriorCenter,
                    SkyvaultInteriorSize,
                    DungeonInteriorPadding))
            {
                failure = "The Threadheart cannot overlap the Skyvault.";
                return false;
            }

            foreach (var placement in EncounterCatalog.ExtendedPlacements)
            {
                if (placement != null &&
                    IsWithinPlanarClearance(
                        position,
                        placement.position,
                        EncounterClearance))
                {
                    failure = "Keep the Threadheart clear of the hostile encounter center.";
                    return false;
                }
            }

            foreach (var region in TetherfallRegionPacks.Playable)
            {
                if (region != null &&
                    IsWithinPlanarClearance(
                        position,
                        region.center + region.encounterLocalPosition,
                        EncounterClearance))
                {
                    failure =
                        $"Keep the Threadheart clear of {region.displayName}'s encounter footprint.";
                    return false;
                }
            }

            foreach (var routeEvent in RouteEventCatalog.All)
            {
                if (routeEvent != null &&
                    IsWithinPlanarClearance(
                        position,
                        routeEvent.position,
                        EncounterClearance))
                {
                    failure =
                        $"Keep the Threadheart clear of {routeEvent.displayName}'s encounter center.";
                    return false;
                }
            }

            foreach (var bounty in BountyCatalog.All)
            {
                if (bounty != null &&
                    IsWithinPlanarClearance(
                        position,
                        bounty.position,
                        EncounterClearance))
                {
                    failure = "Keep the Threadheart clear of the elite encounter center.";
                    return false;
                }
            }

            if (IsNearAny(
                    position,
                    LegacyEncounterCenters,
                    EncounterClearance) ||
                IsNearAny(
                    position,
                    StoryEncounterCenters,
                    EncounterClearance))
            {
                failure = "Keep the Threadheart clear of the encounter center.";
                return false;
            }

            return true;
        }

        private static bool IsNearAny(
            Vector3 position,
            Vector3[] centers,
            float clearance)
        {
            for (var index = 0; index < centers.Length; index++)
            {
                if (IsWithinPlanarClearance(
                        position,
                        centers[index],
                        clearance))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsWithinPlanarClearance(
            Vector3 position,
            Vector3 center,
            float clearance)
        {
            var x = position.x - center.x;
            var z = position.z - center.z;
            var radius = Mathf.Max(0f, clearance);
            return x * x + z * z <= radius * radius;
        }

        private static bool IsInsideHorizontalBounds(
            Vector3 position,
            Vector3 center,
            Vector2 size,
            float padding)
        {
            var safePadding = Mathf.Max(0f, padding);
            return Mathf.Abs(position.x - center.x) <=
                       Mathf.Max(0f, size.x) * 0.5f + safePadding &&
                   Mathf.Abs(position.z - center.z) <=
                       Mathf.Max(0f, size.y) * 0.5f + safePadding;
        }
    }
}
