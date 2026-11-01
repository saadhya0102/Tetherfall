using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public enum CityInstitutionKind
    {
        Bazaar,
        Auction,
        WayfarersGuild,
        ThreadwrightGuild,
        CivicManor,
        QuestHouse
    }

    public enum CityInstitutionAction
    {
        Activity,
        Auction,
        ThreadwrightService
    }

    public sealed class CityAuctionLot
    {
        public string title;
        public string description;
        public List<ResourceCost> cost = new List<ResourceCost>();
    }

    public sealed class CityInstitutionDefinition
    {
        public string id;
        public string cityId;
        public string displayName;
        public string stewardName;
        public string stewardRole;
        public string dialogue;
        public string readyPrompt;
        public string locationHint;
        public CityInstitutionKind kind;
        public CityInstitutionAction action;
        public Vector3 buildingPosition;
        public Vector3 interactionPosition;
        public float yaw;
        public CityActivityDefinition activity;
        public CityAuctionLot[] auctionLots = Array.Empty<CityAuctionLot>();
    }

    public static class CityInstitutionCatalog
    {
        private static readonly CityInstitutionDefinition[] Alderwake =
        {
            Institution(
                "institution.alderwake.bazaar",
                CityCatalog.AlderwakeHearthmarket.id,
                "Hearthmarket Bazaar",
                "Nima Reed",
                "bazaar factor",
                "The awnings keep rain off the ledgers. The growers do the difficult part.",
                "Trade at the bazaar factor's counter",
                "commons arcade",
                CityInstitutionKind.Bazaar,
                CityInstitutionAction.Activity,
                new Vector3(0f, 0f, 72f),
                new Vector3(-6f, 0.9f, 67f),
                0f,
                Activity(
                    "city.alderwake.bazaar_exchange",
                    "Bazaar growers' exchange",
                    "Bundle common field goods for the public kitchens and awning crews.",
                    CityActivityKind.Delivery,
                    Costs(
                        new ResourceCost(ResourceKind.Herbs, 1),
                        new ResourceCost(ResourceKind.Fiber, 2)),
                    8,
                    12,
                    180f)),
            Institution(
                "institution.alderwake.auction",
                CityCatalog.AlderwakeHearthmarket.id,
                "Emberleaf Auction Hall",
                "Sera Quill",
                "auction crier",
                "Consignment keeps useful goods moving without pretending every crate is a treasure.",
                "Review the current consignment lot",
                "southwest lane",
                CityInstitutionKind.Auction,
                CityInstitutionAction.Auction,
                new Vector3(-35f, 0f, 60f),
                new Vector3(-30.2f, 0.9f, 60f),
                -90f,
                Activity(
                    "city.alderwake.auction_consignment",
                    "Emberleaf consignment",
                    "Sell one posted local lot for a fixed public bid.",
                    CityActivityKind.Archive,
                    Costs(),
                    12,
                    16,
                    240f),
                Lot(
                    "Roofwright parcel",
                    "Timber and fiber for the next public awning run.",
                    new ResourceCost(ResourceKind.Timber, 2),
                    new ResourceCost(ResourceKind.Fiber, 3)),
                Lot(
                    "Copper salvage tally",
                    "Usable metal and resin for repaired route fittings.",
                    new ResourceCost(ResourceKind.Ore, 2),
                    new ResourceCost(ResourceKind.Resin, 1)),
                Lot(
                    "Hearthgarden crate",
                    "Fresh herbs and sealing resin for the city kitchens.",
                    new ResourceCost(ResourceKind.Herbs, 3),
                    new ResourceCost(ResourceKind.Resin, 1))),
            Institution(
                "institution.alderwake.wayfarers",
                CityCatalog.AlderwakeHearthmarket.id,
                "Wayfarers Hall",
                "Tovan Marr",
                "contract keeper",
                "A contract is a promise the road can measure. Bring back what the crews cannot gather safely.",
                "Turn in a Wayfarers contract",
                "west terrace",
                CityInstitutionKind.WayfarersGuild,
                CityInstitutionAction.Activity,
                new Vector3(-42f, 0f, 89f),
                new Vector3(-36.8f, 0.9f, 89f),
                -90f,
                Activity(
                    "city.alderwake.wayfarers_contract",
                    "Wayfarers roofline contract",
                    "Deliver route timber and resin for expedition shelters beyond the north gate.",
                    CityActivityKind.Delivery,
                    Costs(
                        new ResourceCost(ResourceKind.Timber, 3),
                        new ResourceCost(ResourceKind.Resin, 2)),
                    18,
                    24,
                    240f)),
            Institution(
                "institution.alderwake.threadwright",
                CityCatalog.AlderwakeHearthmarket.id,
                "Threadwright Loomhouse",
                "Iria Loom",
                "senior threadwright",
                "A field kit drifts out of tune one hard landing at a time. Hold still and let the loom listen.",
                "Tune the field kit",
                "east loom court",
                CityInstitutionKind.ThreadwrightGuild,
                CityInstitutionAction.ThreadwrightService,
                new Vector3(43f, 0f, 91f),
                new Vector3(37.8f, 0.9f, 91f),
                90f,
                Activity(
                    "city.alderwake.threadwright_tuning",
                    "Threadwright resonance tuning",
                    "Spend Chimesalt and ore to mend field resonance, vitality, and stamina.",
                    CityActivityKind.Delivery,
                    Costs(
                        new ResourceCost(ResourceKind.Chimesalt, 1),
                        new ResourceCost(ResourceKind.Ore, 1)),
                    6,
                    22,
                    300f)),
            Institution(
                "institution.alderwake.manor",
                CityCatalog.AlderwakeHearthmarket.id,
                "Alderwake Civic Manor",
                "Warden Elow Fenn",
                "civic warden",
                "Renown here is work remembered by people who had reason to need it.",
                "Report to the civic warden",
                "north manor court",
                CityInstitutionKind.CivicManor,
                CityInstitutionAction.Activity,
                new Vector3(-10f, 0f, 105f),
                new Vector3(-10f, 0.9f, 98.8f),
                0f,
                Activity(
                    "city.alderwake.manor_petition",
                    "Manor route testimony",
                    "Report recent field work so the council can redirect crews and public stores.",
                    CityActivityKind.Archive,
                    Costs(),
                    10,
                    18,
                    600f)),
            Institution(
                "institution.alderwake.house.rootstitch",
                CityCatalog.AlderwakeHearthmarket.id,
                "Rootstitch House",
                "Pella Nook",
                "roofpatch resident",
                "The west eave still sings in a crosswind. Two sound stones would quiet it.",
                "Help Pella patch the west eave",
                "west garden row",
                CityInstitutionKind.QuestHouse,
                CityInstitutionAction.Activity,
                new Vector3(-49f, 0f, 70f),
                new Vector3(-44.6f, 0.9f, 70f),
                -90f,
                Activity(
                    "city.alderwake.house_roofpatch",
                    "Rootstitch roofpatch",
                    "Bring two stones for a resident's storm-braced eave.",
                    CityActivityKind.Delivery,
                    Costs(new ResourceCost(ResourceKind.Stone, 2)),
                    7,
                    10,
                    300f)),
            Institution(
                "institution.alderwake.house.glassgarden",
                CityCatalog.AlderwakeHearthmarket.id,
                "Glassgarden House",
                "Ola Fern",
                "courtyard grower",
                "The seedlings survived the cold. They only need enough herbs to teach the soil what comes next.",
                "Help Ola tend the courtyard",
                "east garden row",
                CityInstitutionKind.QuestHouse,
                CityInstitutionAction.Activity,
                new Vector3(50f, 0f, 73f),
                new Vector3(45.5f, 0.9f, 73f),
                90f,
                Activity(
                    "city.alderwake.house_garden",
                    "Glassgarden seedlings",
                    "Share two herbs with a resident rebuilding a sheltered garden.",
                    CityActivityKind.Delivery,
                    Costs(new ResourceCost(ResourceKind.Herbs, 2)),
                    6,
                    10,
                    240f)),
            Institution(
                "institution.alderwake.house.courier",
                CityCatalog.AlderwakeHearthmarket.id,
                "Bluecord House",
                "Dessa Fallow",
                "dock courier",
                "A dry binding is the difference between a letter and wet pulp at the far bank.",
                "Bind Dessa's courier parcel",
                "northwest homes",
                CityInstitutionKind.QuestHouse,
                CityInstitutionAction.Activity,
                new Vector3(-48f, 0f, 101f),
                new Vector3(-43.5f, 0.9f, 101f),
                -90f,
                Activity(
                    "city.alderwake.house_courier",
                    "Bluecord parcel",
                    "Provide fiber for a resident's marsh-bound courier packet.",
                    CityActivityKind.Delivery,
                    Costs(new ResourceCost(ResourceKind.Fiber, 2)),
                    6,
                    10,
                    240f))
        };

        private static readonly CityInstitutionDefinition[] Veyrwater =
        {
            Institution(
                "institution.veyrwater.bazaar",
                CityCatalog.VeyrwaterMooring.id,
                "Tidemarket Arcade",
                "Kesh Brindle",
                "tide factor",
                "Every crate has a high-water mark. Honest sellers leave it visible.",
                "Trade at the tide factor's counter",
                "lantern commons",
                CityInstitutionKind.Bazaar,
                CityInstitutionAction.Activity,
                new Vector3(203f, 0f, 72f),
                new Vector3(198.5f, 1.15f, 68.5f),
                0f,
                Activity(
                    "city.veyrwater.tidemarket_exchange",
                    "Tidemarket dry-goods exchange",
                    "Bundle fiber and resin for ferry crews working beyond the lantern chain.",
                    CityActivityKind.Delivery,
                    Costs(
                        new ResourceCost(ResourceKind.Fiber, 2),
                        new ResourceCost(ResourceKind.Resin, 1)),
                    8,
                    13,
                    180f)),
            Institution(
                "institution.veyrwater.auction",
                CityCatalog.VeyrwaterMooring.id,
                "Lowtide Auction Loft",
                "Venn Silt",
                "lot recorder",
                "The tide sets the hour. The ledger sets the price. Neither one argues.",
                "Review the current lowtide lot",
                "west landing",
                CityInstitutionKind.Auction,
                CityInstitutionAction.Auction,
                new Vector3(187f, 0f, 87f),
                new Vector3(181.8f, 1.15f, 87f),
                90f,
                Activity(
                    "city.veyrwater.auction_consignment",
                    "Lowtide consignment",
                    "Sell one posted mooring lot for a fixed public bid.",
                    CityActivityKind.Archive,
                    Costs(),
                    11,
                    16,
                    240f),
                Lot(
                    "Dry reed bale",
                    "Fiber and resin packed above the floodline.",
                    new ResourceCost(ResourceKind.Fiber, 3),
                    new ResourceCost(ResourceKind.Resin, 1)),
                Lot(
                    "Fen remedy box",
                    "Field herbs sorted for the floodwatch medics.",
                    new ResourceCost(ResourceKind.Herbs, 3)),
                Lot(
                    "Mooring fitting crate",
                    "Ore and timber for storm-loosened cleats.",
                    new ResourceCost(ResourceKind.Ore, 1),
                    new ResourceCost(ResourceKind.Timber, 2))),
            Institution(
                "institution.veyrwater.wayfarers",
                CityCatalog.VeyrwaterMooring.id,
                "Reedbound Wayfarers Hall",
                "Tali Wren",
                "channel guide",
                "A route through water is still a route. It only needs remembering every morning.",
                "Turn in a channel contract",
                "west boardwalk",
                CityInstitutionKind.WayfarersGuild,
                CityInstitutionAction.Activity,
                new Vector3(220f, 0f, 66f),
                new Vector3(214.8f, 1.15f, 66f),
                90f,
                Activity(
                    "city.veyrwater.wayfarers_contract",
                    "Reedbound channel contract",
                    "Deliver herbs and resin for guides marking a safe dawn channel.",
                    CityActivityKind.Delivery,
                    Costs(
                        new ResourceCost(ResourceKind.Herbs, 3),
                        new ResourceCost(ResourceKind.Resin, 2)),
                    18,
                    25,
                    240f)),
            Institution(
                "institution.veyrwater.threadwright",
                CityCatalog.VeyrwaterMooring.id,
                "Tidewright Spindlehouse",
                "Nell Yarrow",
                "tidewright",
                "Wet Threadglass carries yesterday's current. The spindle teaches it where today begins.",
                "Tune the field kit",
                "east spindle court",
                CityInstitutionKind.ThreadwrightGuild,
                CityInstitutionAction.ThreadwrightService,
                new Vector3(222f, 0f, 89f),
                new Vector3(216.8f, 1.15f, 89f),
                90f,
                Activity(
                    "city.veyrwater.tidewright_tuning",
                    "Tidewright current tuning",
                    "Spend Chimesalt and resin to clear field resonance, vitality, and stamina.",
                    CityActivityKind.Delivery,
                    Costs(
                        new ResourceCost(ResourceKind.Chimesalt, 1),
                        new ResourceCost(ResourceKind.Resin, 2)),
                    6,
                    22,
                    300f)),
            Institution(
                "institution.veyrwater.manor",
                CityCatalog.VeyrwaterMooring.id,
                "Flood Council Lodge",
                "Councilor Mara Wick",
                "flood councilor",
                "The council remembers who kept a lamp burning when the channel went black.",
                "Report to the flood council",
                "north bridge court",
                CityInstitutionKind.CivicManor,
                CityInstitutionAction.Activity,
                new Vector3(203f, 0f, 106f),
                new Vector3(203f, 1.5f, 99.6f),
                0f,
                Activity(
                    "city.veyrwater.council_report",
                    "Flood council report",
                    "File a route account so the council can reposition ferries and lantern crews.",
                    CityActivityKind.Archive,
                    Costs(),
                    11,
                    19,
                    600f)),
            Institution(
                "institution.veyrwater.house.oilcloth",
                CityCatalog.VeyrwaterMooring.id,
                "Oilcloth House",
                "Rusk Arlo",
                "marsh courier",
                "A parcel can cross wet country if the binding is patient enough.",
                "Bind Rusk's ferry parcel",
                "east landing homes",
                CityInstitutionKind.QuestHouse,
                CityInstitutionAction.Activity,
                new Vector3(187f, 0f, 66f),
                new Vector3(192.2f, 1.15f, 66f),
                -90f,
                Activity(
                    "city.veyrwater.house_oilcloth",
                    "Oilcloth ferry parcel",
                    "Provide fiber for a courier packet crossing the flooded road.",
                    CityActivityKind.Delivery,
                    Costs(new ResourceCost(ResourceKind.Fiber, 2)),
                    6,
                    10,
                    240f)),
            Institution(
                "institution.veyrwater.house.reedroof",
                CityCatalog.VeyrwaterMooring.id,
                "Bent Reed House",
                "Lio Mire",
                "reedroof resident",
                "The roof bends well. The split ridge does not. Resin will keep both lessons separate.",
                "Help Lio seal the ridge",
                "northwest homes",
                CityInstitutionKind.QuestHouse,
                CityInstitutionAction.Activity,
                new Vector3(211f, 0f, 84f),
                new Vector3(211f, 1.15f, 80.2f),
                0f,
                Activity(
                    "city.veyrwater.house_reedroof",
                    "Bent Reed ridge seal",
                    "Bring resin for a resident's storm-split reed roof.",
                    CityActivityKind.Delivery,
                    Costs(new ResourceCost(ResourceKind.Resin, 2)),
                    7,
                    11,
                    270f))
        };

        private static readonly CityInstitutionDefinition[] AllDefinitions = Combine(Alderwake, Veyrwater);
        private static readonly CityInstitutionDefinition[] NoInstitutions = Array.Empty<CityInstitutionDefinition>();

        public static IReadOnlyList<CityInstitutionDefinition> All => AllDefinitions;

        public static IReadOnlyList<CityInstitutionDefinition> GetForCity(string cityId)
        {
            if (string.Equals(cityId, CityCatalog.AlderwakeHearthmarket.id, StringComparison.Ordinal))
            {
                return Alderwake;
            }

            if (string.Equals(cityId, CityCatalog.VeyrwaterMooring.id, StringComparison.Ordinal))
            {
                return Veyrwater;
            }

            return NoInstitutions;
        }

        public static CityInstitutionDefinition Find(string institutionId)
        {
            if (string.IsNullOrEmpty(institutionId))
            {
                return null;
            }

            for (var index = 0; index < AllDefinitions.Length; index++)
            {
                var definition = AllDefinitions[index];
                if (definition != null &&
                    string.Equals(definition.id, institutionId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public static CityActivityDefinition FindActivity(string activityId)
        {
            if (string.IsNullOrEmpty(activityId))
            {
                return null;
            }

            for (var index = 0; index < AllDefinitions.Length; index++)
            {
                var activity = AllDefinitions[index]?.activity;
                if (activity != null &&
                    string.Equals(activity.id, activityId, StringComparison.Ordinal))
                {
                    return activity;
                }
            }

            return null;
        }

        public static CityActivityDefinition FindActivityForCity(string cityId, string activityId)
        {
            var institutions = GetForCity(cityId);
            for (var index = 0; index < institutions.Count; index++)
            {
                var activity = institutions[index]?.activity;
                if (activity != null &&
                    string.Equals(activity.id, activityId, StringComparison.Ordinal))
                {
                    return activity;
                }
            }

            return null;
        }

        public static CityAuctionLot GetAuctionLot(CityInstitutionDefinition definition, int completionCount)
        {
            if (definition?.auctionLots == null || definition.auctionLots.Length == 0)
            {
                return null;
            }

            var safeCount = Mathf.Max(0, completionCount);
            return definition.auctionLots[safeCount % definition.auctionLots.Length];
        }

        private static CityInstitutionDefinition[] Combine(
            CityInstitutionDefinition[] first,
            CityInstitutionDefinition[] second)
        {
            var combined = new CityInstitutionDefinition[first.Length + second.Length];
            Array.Copy(first, 0, combined, 0, first.Length);
            Array.Copy(second, 0, combined, first.Length, second.Length);
            return combined;
        }

        private static CityInstitutionDefinition Institution(
            string id,
            string cityId,
            string displayName,
            string stewardName,
            string stewardRole,
            string dialogue,
            string readyPrompt,
            string locationHint,
            CityInstitutionKind kind,
            CityInstitutionAction action,
            Vector3 buildingPosition,
            Vector3 interactionPosition,
            float yaw,
            CityActivityDefinition activity,
            params CityAuctionLot[] auctionLots)
        {
            return new CityInstitutionDefinition
            {
                id = id,
                cityId = cityId,
                displayName = displayName,
                stewardName = stewardName,
                stewardRole = stewardRole,
                dialogue = dialogue,
                readyPrompt = readyPrompt,
                locationHint = locationHint,
                kind = kind,
                action = action,
                buildingPosition = buildingPosition,
                interactionPosition = interactionPosition,
                yaw = yaw,
                activity = activity,
                auctionLots = auctionLots ?? Array.Empty<CityAuctionLot>()
            };
        }

        private static CityActivityDefinition Activity(
            string id,
            string title,
            string description,
            CityActivityKind kind,
            List<ResourceCost> cost,
            int favorReward,
            int experienceReward,
            float cooldownSeconds)
        {
            return new CityActivityDefinition
            {
                id = id,
                title = title,
                description = description,
                kind = kind,
                cost = cost ?? new List<ResourceCost>(),
                favorReward = favorReward,
                experienceReward = experienceReward,
                cooldownSeconds = cooldownSeconds
            };
        }

        private static CityAuctionLot Lot(
            string title,
            string description,
            params ResourceCost[] costs)
        {
            return new CityAuctionLot
            {
                title = title,
                description = description,
                cost = Costs(costs)
            };
        }

        private static List<ResourceCost> Costs(params ResourceCost[] costs)
        {
            return costs != null
                ? new List<ResourceCost>(costs)
                : new List<ResourceCost>();
        }
    }

    public sealed partial class TetherfallGame
    {
        internal void TryUseCityInstitution(
            CityDirector city,
            CityInstitutionDefinition definition)
        {
            if (city == null ||
                definition?.activity == null ||
                city.Definition == null ||
                !string.Equals(city.Definition.id, definition.cityId, StringComparison.Ordinal) ||
                CityInstitutionCatalog.Find(definition.id) == null)
            {
                Notify("That city service is not available.");
                return;
            }

            var activity = CityInstitutionCatalog.FindActivity(definition.activity.id);
            if (activity == null)
            {
                Notify("That institution's ledger could not be read.");
                return;
            }

            if (!IsCityActivityAvailable(activity))
            {
                Notify(
                    $"{definition.stewardName}: {definition.dialogue} " +
                    $"{activity.title} returns {GetCityActivityCooldownText(activity)}.",
                    6f);
                return;
            }

            switch (definition.action)
            {
                case CityInstitutionAction.Auction:
                    TryUseCityAuction(definition, activity);
                    break;
                case CityInstitutionAction.ThreadwrightService:
                    TryUseThreadwrightService(definition, activity);
                    break;
                default:
                    Notify($"{definition.stewardName}: {definition.dialogue}", 5.4f);
                    TryStartCityActivity(city, activity);
                    break;
            }
        }

        internal string GetCityInstitutionPrompt(CityInstitutionDefinition definition)
        {
            var activity = definition?.activity;
            if (activity == null)
            {
                return "Inspect city institution";
            }

            if (!IsCityActivityAvailable(activity))
            {
                return $"{definition.displayName} returns {GetCityActivityCooldownText(activity)}";
            }

            if (definition.action == CityInstitutionAction.Auction)
            {
                var lot = CityInstitutionCatalog.GetAuctionLot(
                    definition,
                    GetCityActivityCompletionCount(activity));
                return lot != null
                    ? $"Consign {DescribeCityCost(lot.cost)} at {definition.displayName}"
                    : definition.readyPrompt;
            }

            return definition.readyPrompt;
        }

        internal string GetCityInstitutionBoardStatus(CityInstitutionDefinition definition)
        {
            if (definition?.activity == null)
            {
                return "Service unavailable";
            }

            var available = IsCityActivityAvailable(definition.activity);
            if (definition.action == CityInstitutionAction.Auction && available)
            {
                var lot = CityInstitutionCatalog.GetAuctionLot(
                    definition,
                    GetCityActivityCompletionCount(definition.activity));
                return lot != null
                    ? $"READY · {lot.title}"
                    : "READY";
            }

            return available
                ? "READY"
                : $"Returns {GetCityActivityCooldownText(definition.activity)}";
        }

        private void TryUseCityAuction(
            CityInstitutionDefinition definition,
            CityActivityDefinition activity)
        {
            var lot = CityInstitutionCatalog.GetAuctionLot(
                definition,
                GetCityActivityCompletionCount(activity));
            if (lot == null)
            {
                Notify($"{definition.displayName} has no lot posted.");
                return;
            }

            var validCost = GetValidCityCost(lot.cost);
            if (validCost.Count == 0)
            {
                Notify($"{definition.displayName}'s current lot was withdrawn.");
                return;
            }

            if (!CanAfford(validCost))
            {
                Notify(
                    $"{definition.stewardName}: {lot.title} needs " +
                    $"{DescribeMissingCost(validCost)}.");
                return;
            }

            SpendCost(validCost);
            CompleteCityActivity(activity);
            Notify(
                $"{definition.stewardName} closes the {lot.title} lot. " +
                $"{lot.description}",
                5.6f);
        }

        private void TryUseThreadwrightService(
            CityInstitutionDefinition definition,
            CityActivityDefinition activity)
        {
            var validCost = GetValidCityCost(activity.cost);
            if (validCost.Count == 0 || !CanAfford(validCost))
            {
                Notify(
                    validCost.Count == 0
                        ? $"{definition.displayName}'s tuning materials are not listed."
                        : $"{definition.stewardName}: Tuning needs {DescribeMissingCost(validCost)}.");
                return;
            }

            SpendCost(validCost);
            RestoreAllStamina();
            HealPlayer(Mathf.Max(12, Mathf.CeilToInt(MaxHealth * 0.2f)));
            CompleteCityActivity(activity);
            Notify(
                $"{definition.stewardName} settles the field kit into a clean resonance. " +
                "Stamina restored and vitality mended.",
                5.8f);
        }
    }

    public static class CityInstitutionBuilder
    {
        private const string InstitutionRootName = "City institutions";

        public static void Build(
            TetherfallGame game,
            CityDirector city,
            CityVisualBuilder.CityArtPalette palette)
        {
            if (game == null || city?.Definition == null || palette == null)
            {
                return;
            }

            if (city.transform.Find(InstitutionRootName) != null)
            {
                return;
            }

            var districtRoot = new GameObject(InstitutionRootName).transform;
            districtRoot.SetParent(city.transform, false);
            var institutions = CityInstitutionCatalog.GetForCity(city.Definition.id);
            var harbor = string.Equals(
                city.Definition.id,
                CityCatalog.VeyrwaterMooring.id,
                StringComparison.Ordinal);
            for (var index = 0; index < institutions.Count; index++)
            {
                var definition = institutions[index];
                if (definition == null)
                {
                    continue;
                }

                if (definition.kind != CityInstitutionKind.Bazaar)
                {
                    BuildApproach(city.Definition, definition, districtRoot, palette);
                }

                var building = new GameObject(definition.displayName).transform;
                building.SetParent(districtRoot, false);
                building.position = definition.buildingPosition;
                building.rotation = Quaternion.Euler(0f, definition.yaw, 0f);

                switch (definition.kind)
                {
                    case CityInstitutionKind.Bazaar:
                        BuildBazaar(building, palette, city.Definition.id);
                        break;
                    case CityInstitutionKind.Auction:
                        if (harbor)
                        {
                            BuildHarborAuctionLoft(building, palette);
                        }
                        else
                        {
                            BuildAuctionHall(building, palette);
                        }
                        break;
                    case CityInstitutionKind.WayfarersGuild:
                        if (harbor)
                        {
                            BuildHarborWayfarersLanding(building, palette);
                        }
                        else
                        {
                            BuildWayfarersHall(building, palette);
                        }
                        break;
                    case CityInstitutionKind.ThreadwrightGuild:
                        if (harbor)
                        {
                            BuildHarborThreadwrightCourt(game, building, palette);
                        }
                        else
                        {
                            BuildThreadwrightHall(game, building, palette);
                        }
                        break;
                    case CityInstitutionKind.CivicManor:
                        BuildCivicManor(building, palette);
                        break;
                    default:
                        if (harbor)
                        {
                            BuildHarborQuestPorch(
                                building,
                                palette,
                                definition.id.EndsWith("reedroof", StringComparison.Ordinal));
                        }
                        else
                        {
                            BuildQuestHouse(building, palette, index);
                        }
                        break;
                }

                WorldVisuals.AddLabel(
                    building,
                    definition.displayName,
                    GetLabelPosition(definition.kind),
                    city.Definition.accentColor,
                    definition.kind == CityInstitutionKind.QuestHouse ? 0.068f : 0.082f);
                var steward = CityInstitutionInteractable.Create(
                    game,
                    city,
                    definition,
                    palette,
                    districtRoot);
                game.RegisterInteractable(steward);
            }

            BuildAmbientRoutes(game, city.Definition, palette, districtRoot);
        }

        private static Vector3 GetLabelPosition(CityInstitutionKind kind)
        {
            return kind switch
            {
                CityInstitutionKind.WayfarersGuild => new Vector3(0f, 8.7f, 0f),
                CityInstitutionKind.ThreadwrightGuild => new Vector3(0f, 7.8f, 0f),
                CityInstitutionKind.CivicManor => new Vector3(0f, 7.3f, 0f),
                CityInstitutionKind.Auction => new Vector3(0f, 6.7f, 0f),
                CityInstitutionKind.QuestHouse => new Vector3(0f, 5.7f, 0f),
                _ => new Vector3(0f, 5.6f, 0f)
            };
        }

        private static void BuildApproach(
            CityDefinition city,
            CityInstitutionDefinition institution,
            Transform parent,
            CityVisualBuilder.CityArtPalette palette)
        {
            var from = institution.interactionPosition;
            from.y = 0f;
            var toCenter = city.center - from;
            toCenter.y = 0f;
            var distance = toCenter.magnitude;
            if (distance <= 16f)
            {
                return;
            }

            var length = Mathf.Max(5f, distance - 14f);
            var direction = toCenter / distance;
            var center = from + direction * (length * 0.5f);
            var harbor = string.Equals(city.id, CityCatalog.VeyrwaterMooring.id, StringComparison.Ordinal);
            var raisedBoardwalk = harbor && institution.kind == CityInstitutionKind.CivicManor;
            var path = CreatePrimitive(
                PrimitiveType.Cube,
                $"{institution.displayName} approach",
                parent,
                center + Vector3.up * (raisedBoardwalk ? 0.38f : 0.06f),
                new Vector3(
                    harbor ? 3.8f : 4.6f,
                    raisedBoardwalk ? 0.3f : 0.12f,
                    length),
                harbor ? palette.timber : palette.road,
                true);
            path.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            var markerCount = Mathf.Clamp(Mathf.FloorToInt(length / 9f), 1, 4);
            for (var index = 0; index < markerCount; index++)
            {
                var t = (index + 0.5f) / markerCount;
                var markerPosition = from + direction * (length * t);
                var side = Vector3.Cross(Vector3.up, direction) * 1.75f;
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    $"{institution.displayName} route post",
                    parent,
                    markerPosition + side + Vector3.up * 0.65f,
                    new Vector3(0.08f, 0.65f, 0.08f),
                    palette.copper,
                    false);
            }
        }

        private static void BuildBazaar(
            Transform root,
            CityVisualBuilder.CityArtPalette palette,
            string cityId)
        {
            var harbor = string.Equals(cityId, CityCatalog.VeyrwaterMooring.id, StringComparison.Ordinal);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Bazaar walking deck",
                root,
                new Vector3(0f, harbor ? 0.12f : 0.04f, 0f),
                new Vector3(harbor ? 27f : 30f, harbor ? 0.22f : 0.12f, 14f),
                harbor ? palette.timber : palette.plaza,
                true);

            if (harbor)
            {
                BuildHarborBazaarCanopies(root, palette);
            }
            else
            {
                var modulePositions = new[]
                {
                    new Vector3(-9f, 0f, -9.5f),
                    new Vector3(-14f, 0f, 4.5f),
                    new Vector3(9f, 0f, -9.5f),
                    new Vector3(14f, 0f, 4.5f)
                };
                for (var index = 0; index < modulePositions.Length; index++)
                {
                    var side = Mathf.Sign(modulePositions[index].x);
                    var module = new GameObject($"Bazaar awning {index + 1}").transform;
                    module.SetParent(root, false);
                    module.localPosition = modulePositions[index];
                    module.localRotation = Quaternion.Euler(0f, side < 0f ? -5f : 5f, 0f);

                    CreatePrimitive(
                        PrimitiveType.Cube,
                        "Bazaar counter",
                        module,
                        new Vector3(0f, 0.72f, 0f),
                        new Vector3(5.4f, 1.35f, 1.55f),
                        palette.timber,
                        false);
                    var canopy = CreatePrimitive(
                        PrimitiveType.Cube,
                        "Layered bazaar canopy",
                        module,
                        new Vector3(0f, 3.15f, 0f),
                        new Vector3(6.2f, 0.22f, 3.1f),
                        index % 2 == 0 ? palette.canopy : palette.roof,
                        false);
                    canopy.transform.localRotation = Quaternion.Euler(0f, 0f, side * 4f);

                    for (var post = -1; post <= 1; post += 2)
                    {
                        CreatePrimitive(
                            PrimitiveType.Cylinder,
                            "Bazaar awning post",
                            module,
                            new Vector3(post * 2.55f, 1.55f, -0.9f),
                            new Vector3(0.09f, 1.55f, 0.09f),
                            palette.copper,
                            false);
                    }

                    for (var clutter = 0; clutter < 3; clutter++)
                    {
                        CreatePrimitive(
                            clutter == 2 ? PrimitiveType.Sphere : PrimitiveType.Cylinder,
                            "Bazaar sorted wares",
                            module,
                            new Vector3(-1.5f + clutter * 1.45f, 1.55f, -0.08f),
                            clutter == 2
                                ? Vector3.one * 0.42f
                                : new Vector3(0.38f, 0.22f + clutter * 0.08f, 0.38f),
                            clutter % 2 == 0 ? palette.marker : palette.stone,
                            false);
                    }
                }
            }

            for (var side = -1; side <= 1; side += 2)
            {
                var arch = new GameObject("Bazaar wayfinding arch").transform;
                arch.SetParent(root, false);
                arch.localPosition = new Vector3(0f, 0f, side * 6.1f);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Bazaar arch post",
                    arch,
                    new Vector3(-3.2f, 2f, 0f),
                    new Vector3(0.13f, 2f, 0.13f),
                    palette.copper,
                    false);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Bazaar arch post",
                    arch,
                    new Vector3(3.2f, 2f, 0f),
                    new Vector3(0.13f, 2f, 0.13f),
                    palette.copper,
                    false);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Bazaar arch lintel",
                    arch,
                    new Vector3(0f, 3.95f, 0f),
                    new Vector3(6.7f, 0.16f, 0.22f),
                    palette.copper,
                    false);
                CreateRingCrest(arch, new Vector3(0f, 4.45f, 0f), 0.72f, palette.marker);
            }
        }

        private static void BuildHarborBazaarCanopies(
            Transform root,
            CityVisualBuilder.CityArtPalette palette)
        {
            for (var index = 0; index < 3; index++)
            {
                var sail = CreatePrimitive(
                    PrimitiveType.Cube,
                    "Tidemarket overlapping sail",
                    root,
                    new Vector3(-7.5f + index * 7.5f, 3.75f + (index % 2) * 0.24f, -0.4f),
                    new Vector3(7.9f, 0.18f, 5.6f),
                    index == 1 ? palette.roof : palette.canopy,
                    false);
                sail.transform.localRotation = Quaternion.Euler(
                    0f,
                    index == 1 ? 0f : (index == 0 ? -8f : 8f),
                    index == 1 ? -3f : 3f);
            }

            for (var side = -1; side <= 1; side += 2)
            {
                for (var depth = -1; depth <= 1; depth += 2)
                {
                    CreatePrimitive(
                        PrimitiveType.Cylinder,
                        "Tidemarket sail post",
                        root,
                        new Vector3(side * 11.8f, 1.9f, depth * 4.9f),
                        new Vector3(0.11f, 1.9f, 0.11f),
                        palette.copper,
                        false);
                }
            }

            var freightPositions = new[]
            {
                new Vector3(-11f, 0.55f, -5.5f),
                new Vector3(-11f, 0.55f, 0f),
                new Vector3(11f, 0.55f, -5.5f),
                new Vector3(11f, 0.55f, 0f),
                new Vector3(0f, 0.55f, 7f)
            };
            for (var index = 0; index < freightPositions.Length; index++)
            {
                CreatePrimitive(
                    index % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Cylinder,
                    "Tidemarket sorted freight",
                    root,
                    freightPositions[index],
                    index % 2 == 0
                        ? new Vector3(0.72f, 0.68f, 0.72f)
                        : new Vector3(0.34f, 0.5f, 0.34f),
                    index == 2 ? palette.marker : palette.stone,
                    false);
            }
        }

        private static void BuildHarborAuctionLoft(
            Transform root,
            CityVisualBuilder.CityArtPalette palette)
        {
            CreatePrimitive(
                PrimitiveType.Cube,
                "Lowtide auction deck",
                root,
                new Vector3(0f, 0.12f, -4.1f),
                new Vector3(8.6f, 0.24f, 4.2f),
                palette.timber,
                true);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Lowtide loft stilt",
                    root,
                    new Vector3(side * 3.7f, 1.8f, -5.25f),
                    new Vector3(0.14f, 1.65f, 0.14f),
                    palette.copper,
                    false);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Lowtide loft stilt",
                    root,
                    new Vector3(side * 3.7f, 1.8f, -2.9f),
                    new Vector3(0.14f, 1.65f, 0.14f),
                    palette.copper,
                    false);
            }

            var canopy = CreatePrimitive(
                PrimitiveType.Cube,
                "Lowtide layered canopy",
                root,
                new Vector3(0f, 3.7f, -4.1f),
                new Vector3(9.2f, 0.22f, 4.8f),
                palette.canopy,
                false);
            canopy.transform.localRotation = Quaternion.Euler(0f, 0f, 3f);

            for (var lot = -1; lot <= 1; lot++)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Lowtide lot plinth",
                    root,
                    new Vector3(lot * 2.3f, 0.85f, -4.15f),
                    new Vector3(0.72f, 0.48f, 0.72f),
                    palette.stone,
                    false);
                CreatePrimitive(
                    lot == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube,
                    "Lowtide display lot",
                    root,
                    new Vector3(lot * 2.3f, 1.55f, -4.15f),
                    lot == 0 ? Vector3.one * 0.48f : new Vector3(0.78f, 0.52f, 0.62f),
                    lot == 0 ? palette.marker : palette.copper,
                    false);
            }

            CreatePrimitive(
                PrimitiveType.Cube,
                "Lowtide lot beam",
                root,
                new Vector3(0f, 3.25f, -5.35f),
                new Vector3(7.8f, 0.16f, 0.18f),
                palette.copper,
                false);
            var gavel = CreatePrimitive(
                PrimitiveType.Cube,
                "Lowtide gavel crest",
                root,
                new Vector3(0f, 4.15f, -5.45f),
                new Vector3(1.5f, 0.34f, 0.3f),
                palette.marker,
                false);
            gavel.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
        }

        private static void BuildHarborWayfarersLanding(
            Transform root,
            CityVisualBuilder.CityArtPalette palette)
        {
            CreatePrimitive(
                PrimitiveType.Cube,
                "Reedbound landing deck",
                root,
                new Vector3(0f, 0.12f, -4f),
                new Vector3(9.2f, 0.24f, 4.4f),
                palette.timber,
                true);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Reedbound route mast",
                    root,
                    new Vector3(side * 3.5f, 2.8f, -4.8f),
                    new Vector3(0.18f, 2.8f, 0.18f),
                    palette.copper,
                    false);
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Reedbound mast marker",
                    root,
                    new Vector3(side * 3.5f, 5.75f, -4.8f),
                    Vector3.one * 0.38f,
                    palette.marker,
                    false);
            }

            CreatePrimitive(
                PrimitiveType.Cube,
                "Reedbound mast crossbeam",
                root,
                new Vector3(0f, 5.2f, -4.8f),
                new Vector3(7.4f, 0.16f, 0.2f),
                palette.copper,
                false);
            CreateRingCrest(root, new Vector3(0f, 4.1f, -4.93f), 0.92f, palette.marker);

            for (var rack = -1; rack <= 1; rack += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Reedbound gear rack",
                    root,
                    new Vector3(rack * 2.8f, 1.2f, -3.45f),
                    new Vector3(1.8f, 0.14f, 0.24f),
                    palette.stone,
                    false);
                for (var tool = -1; tool <= 1; tool += 2)
                {
                    var pole = CreatePrimitive(
                        PrimitiveType.Cylinder,
                        "Reedbound channel pole",
                        root,
                        new Vector3(rack * 2.8f + tool * 0.5f, 0.85f, -3.45f),
                        new Vector3(0.06f, 0.78f, 0.06f),
                        palette.copper,
                        false);
                    pole.transform.localRotation = Quaternion.Euler(0f, 0f, tool * 9f);
                }
            }
        }

        private static void BuildHarborThreadwrightCourt(
            TetherfallGame game,
            Transform root,
            CityVisualBuilder.CityArtPalette palette)
        {
            CreatePrimitive(
                PrimitiveType.Cube,
                "Tidewright spindle deck",
                root,
                new Vector3(0f, 0.12f, -4f),
                new Vector3(9f, 0.24f, 4.5f),
                palette.timber,
                true);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Tidewright spindle",
                    root,
                    new Vector3(side * 3.15f, 2.45f, -4.2f),
                    new Vector3(0.34f, 2.35f, 0.34f),
                    palette.copper,
                    false);
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Tidewright current marker",
                    root,
                    new Vector3(side * 3.15f, 4.95f, -4.2f),
                    Vector3.one * 0.44f,
                    palette.marker,
                    false);
            }

            var spindle = new GameObject("Tidewright current wheel").transform;
            spindle.SetParent(root, false);
            spindle.localPosition = new Vector3(0f, 0f, -4.45f);
            CreateRingCrest(spindle, new Vector3(0f, 3.4f, 0f), 1.55f, palette.marker);
            for (var strand = -1; strand <= 1; strand++)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Tidewright wet strand",
                    spindle,
                    new Vector3(strand * 0.72f, 3.4f, 0.05f),
                    new Vector3(0.055f, 2.5f - Mathf.Abs(strand) * 0.4f, 0.055f),
                    strand == 0 ? palette.marker : palette.copper,
                    false);
            }

            CreatePrimitive(
                PrimitiveType.Cube,
                "Tidewright tuning counter",
                root,
                new Vector3(0f, 0.8f, -2.75f),
                new Vector3(3.6f, 1.35f, 1.1f),
                palette.stone,
                false);
            spindle.gameObject.AddComponent<CityInstitutionAccentMotion>().Initialize(
                game,
                root.position,
                spindle,
                6f);
        }

        private static void BuildHarborQuestPorch(
            Transform root,
            CityVisualBuilder.CityArtPalette palette,
            bool standalone)
        {
            if (standalone)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Bent reed cabin wall",
                    root,
                    new Vector3(0f, 2f, 1.3f),
                    new Vector3(6.4f, 3.8f, 2.5f),
                    palette.wall,
                    true);
                var roof = CreatePrimitive(
                    PrimitiveType.Cube,
                    "Bent reed cabin roof",
                    root,
                    new Vector3(0f, 4.1f, 0.9f),
                    new Vector3(7.2f, 0.36f, 3.8f),
                    palette.canopy,
                    false);
                roof.transform.localRotation = Quaternion.Euler(0f, 0f, 5f);
            }

            CreatePrimitive(
                PrimitiveType.Cube,
                "Mooring house porch deck",
                root,
                new Vector3(0f, 0.12f, -3.45f),
                new Vector3(6.4f, 0.24f, 3.1f),
                palette.timber,
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Mooring house porch canopy",
                root,
                new Vector3(0f, 3f, -3.45f),
                new Vector3(6.8f, 0.22f, 3.5f),
                palette.canopy,
                false);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Mooring house porch post",
                    root,
                    new Vector3(side * 2.7f, 1.55f, -4.35f),
                    new Vector3(0.11f, 1.45f, 0.11f),
                    palette.copper,
                    false);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Mooring house parcel crate",
                    root,
                    new Vector3(side * 1.8f, 0.75f, -3.25f),
                    new Vector3(0.72f, 0.68f, 0.68f),
                    side < 0 ? palette.stone : palette.copper,
                    false);
            }

            CreatePrimitive(
                PrimitiveType.Cube,
                "Mooring house access step",
                root,
                new Vector3(0f, 0.06f, -5.05f),
                new Vector3(2.6f, 0.12f, 0.72f),
                palette.road,
                true);
        }

        private static void BuildAuctionHall(
            Transform root,
            CityVisualBuilder.CityArtPalette palette)
        {
            CreateOpenHall(root, palette, 10.5f, 8.5f, 4.4f);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Auction balcony",
                root,
                new Vector3(0f, 3.25f, 2.65f),
                new Vector3(8.7f, 0.24f, 1.35f),
                palette.timber,
                false);
            for (var rail = -3; rail <= 3; rail++)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Auction balcony rail",
                    root,
                    new Vector3(rail * 1.15f, 3.75f, 1.95f),
                    new Vector3(0.06f, 0.48f, 0.06f),
                    palette.copper,
                    false);
            }

            for (var lot = -1; lot <= 1; lot++)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Auction lot plinth",
                    root,
                    new Vector3(lot * 2.5f, 0.58f, -1.6f),
                    new Vector3(0.85f, 0.52f, 0.85f),
                    palette.stone,
                    false);
                CreatePrimitive(
                    lot == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube,
                    "Auction display lot",
                    root,
                    new Vector3(lot * 2.5f, 1.35f, -1.6f),
                    lot == 0 ? Vector3.one * 0.55f : new Vector3(0.92f, 0.62f, 0.72f),
                    lot == 0 ? palette.marker : palette.copper,
                    false);
            }

            var gavel = new GameObject("Auction gavel crest").transform;
            gavel.SetParent(root, false);
            gavel.localPosition = new Vector3(0f, 5.15f, 3.92f);
            gavel.localRotation = Quaternion.Euler(0f, 0f, -28f);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Auction gavel handle",
                gavel,
                Vector3.zero,
                new Vector3(0.09f, 1.15f, 0.09f),
                palette.copper,
                false);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Auction gavel head",
                gavel,
                new Vector3(0f, 1.16f, 0f),
                new Vector3(1.4f, 0.38f, 0.42f),
                palette.marker,
                false);
        }

        private static void BuildWayfarersHall(
            Transform root,
            CityVisualBuilder.CityArtPalette palette)
        {
            CreateOpenHall(root, palette, 11.5f, 9f, 4.7f);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Wayfarers route tower",
                root,
                new Vector3(0f, 4.15f, 3.25f),
                new Vector3(3.2f, 8.3f, 2.8f),
                palette.stone,
                true);
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "Wayfarers tower cap",
                root,
                new Vector3(0f, 8.3f, 3.25f),
                new Vector3(2.2f, 0.22f, 2.2f),
                palette.copper,
                false);
            CreateRingCrest(root, new Vector3(0f, 6.35f, 1.72f), 1.25f, palette.marker);
            for (var spoke = 0; spoke < 4; spoke++)
            {
                var angle = spoke * 45f;
                var arm = CreatePrimitive(
                    PrimitiveType.Cube,
                    "Wayfarers compass spoke",
                    root,
                    new Vector3(0f, 6.35f, 1.68f),
                    new Vector3(0.11f, 2.1f, 0.1f),
                    palette.copper,
                    false);
                arm.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            for (var side = -1; side <= 1; side += 2)
            {
                var rack = new GameObject("Wayfarers equipment rack").transform;
                rack.SetParent(root, false);
                rack.localPosition = new Vector3(side * 3.8f, 0f, -1.5f);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Wayfarers rack beam",
                    rack,
                    new Vector3(0f, 1.2f, 0f),
                    new Vector3(1.8f, 0.13f, 0.22f),
                    palette.timber,
                    false);
                for (var tool = -1; tool <= 1; tool += 2)
                {
                    var pole = CreatePrimitive(
                        PrimitiveType.Cylinder,
                        "Wayfarers route tool",
                        rack,
                        new Vector3(tool * 0.55f, 0.75f, 0f),
                        new Vector3(0.07f, 0.78f, 0.07f),
                        palette.copper,
                        false);
                    pole.transform.localRotation = Quaternion.Euler(0f, 0f, tool * 11f);
                }
            }
        }

        private static void BuildThreadwrightHall(
            TetherfallGame game,
            Transform root,
            CityVisualBuilder.CityArtPalette palette)
        {
            CreatePrimitive(
                PrimitiveType.Cube,
                "Threadwright courtyard",
                root,
                new Vector3(0f, 0.12f, 0f),
                new Vector3(11f, 0.24f, 9f),
                palette.plaza,
                true);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Threadwright spindle tower",
                    root,
                    new Vector3(side * 3.8f, 3f, 2.8f),
                    new Vector3(0.48f, 3f, 0.48f),
                    palette.copper,
                    true);
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "Threadwright spindle marker",
                    root,
                    new Vector3(side * 3.8f, 6.15f, 2.8f),
                    Vector3.one * 0.52f,
                    palette.marker,
                    false);
            }

            var loom = new GameObject("Threadwright resonance loom").transform;
            loom.SetParent(root, false);
            loom.localPosition = new Vector3(0f, 0f, 2.9f);
            CreateRingCrest(loom, new Vector3(0f, 3.8f, 0f), 2.25f, palette.marker);
            for (var strand = -2; strand <= 2; strand++)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Threadwright loom strand",
                    loom,
                    new Vector3(strand * 0.72f, 3.8f, 0.04f),
                    new Vector3(0.055f, 4.2f - Mathf.Abs(strand) * 0.55f, 0.055f),
                    strand == 0 ? palette.marker : palette.copper,
                    false);
            }

            CreatePrimitive(
                PrimitiveType.Cube,
                "Threadwright service table",
                root,
                new Vector3(0f, 0.75f, -2.15f),
                new Vector3(4f, 1.35f, 1.4f),
                palette.timber,
                false);
            for (var tool = -1; tool <= 1; tool++)
            {
                CreatePrimitive(
                    tool == 0 ? PrimitiveType.Sphere : PrimitiveType.Cylinder,
                    "Threadwright tuning tool",
                    root,
                    new Vector3(tool * 1.1f, 1.55f, -2.15f),
                    tool == 0 ? Vector3.one * 0.42f : new Vector3(0.22f, 0.32f, 0.22f),
                    tool == 0 ? palette.marker : palette.stone,
                    false);
            }

            loom.gameObject.AddComponent<CityInstitutionAccentMotion>().Initialize(
                game,
                root.position,
                loom,
                7f);
        }

        private static void BuildCivicManor(
            Transform root,
            CityVisualBuilder.CityArtPalette palette)
        {
            CreatePrimitive(
                PrimitiveType.Cube,
                "Civic manor foundation",
                root,
                new Vector3(0f, 0.45f, 0f),
                new Vector3(15f, 0.9f, 10f),
                palette.stone,
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Civic manor rear hall",
                root,
                new Vector3(0f, 3f, 2.7f),
                new Vector3(13.5f, 5.1f, 3.6f),
                palette.wall,
                true);
            for (var side = -2; side <= 2; side++)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Civic manor colonnade",
                    root,
                    new Vector3(side * 2.45f, 2.4f, -2.4f),
                    new Vector3(0.28f, 2.05f, 0.28f),
                    palette.copper,
                    false);
            }

            var roofLeft = CreatePrimitive(
                PrimitiveType.Cube,
                "Civic manor roof",
                root,
                new Vector3(-3.25f, 5.8f, 1.5f),
                new Vector3(7.6f, 0.42f, 7.2f),
                palette.roof,
                false);
            roofLeft.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            var roofRight = CreatePrimitive(
                PrimitiveType.Cube,
                "Civic manor roof",
                root,
                new Vector3(3.25f, 5.8f, 1.5f),
                new Vector3(7.6f, 0.42f, 7.2f),
                palette.roof,
                false);
            roofRight.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);

            CreatePrimitive(
                PrimitiveType.Cube,
                "Civic manor balcony",
                root,
                new Vector3(0f, 3.75f, -0.25f),
                new Vector3(8.5f, 0.24f, 1.65f),
                palette.timber,
                false);
            for (var rail = -3; rail <= 3; rail++)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Civic manor balcony rail",
                    root,
                    new Vector3(rail * 1.12f, 4.28f, -1f),
                    new Vector3(0.06f, 0.5f, 0.06f),
                    palette.copper,
                    false);
            }

            var ramp = CreatePrimitive(
                PrimitiveType.Cube,
                "Civic manor access ramp",
                root,
                new Vector3(0f, 0.16f, -5.3f),
                new Vector3(3.4f, 0.28f, 4.1f),
                palette.road,
                true);
            ramp.transform.localRotation = Quaternion.Euler(-7f, 0f, 0f);
            CreateRingCrest(root, new Vector3(0f, 4.9f, -1.58f), 0.9f, palette.marker);
        }

        private static void BuildQuestHouse(
            Transform root,
            CityVisualBuilder.CityArtPalette palette,
            int variant)
        {
            CreatePrimitive(
                PrimitiveType.Cube,
                "Quest house floor",
                root,
                new Vector3(0f, 0.18f, 0f),
                new Vector3(7.5f, 0.36f, 6f),
                palette.stone,
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Quest house rear wall",
                root,
                new Vector3(0f, 2.15f, 2.62f),
                new Vector3(7.2f, 4f, 0.42f),
                palette.wall,
                true);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Quest house side wall",
                    root,
                    new Vector3(side * 3.4f, 2.15f, 0.8f),
                    new Vector3(0.4f, 4f, 3.8f),
                    palette.wall,
                    true);
            }

            var leftRoof = CreatePrimitive(
                PrimitiveType.Cube,
                "Quest house roof",
                root,
                new Vector3(-1.8f, 4.6f, 0.3f),
                new Vector3(4.3f, 0.38f, 6.8f),
                variant % 2 == 0 ? palette.roof : palette.canopy,
                false);
            leftRoof.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            var rightRoof = CreatePrimitive(
                PrimitiveType.Cube,
                "Quest house roof",
                root,
                new Vector3(1.8f, 4.6f, 0.3f),
                new Vector3(4.3f, 0.38f, 6.8f),
                variant % 2 == 0 ? palette.roof : palette.canopy,
                false);
            rightRoof.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);

            CreatePrimitive(
                PrimitiveType.Cube,
                "Quest house porch",
                root,
                new Vector3(0f, 0.38f, -3.25f),
                new Vector3(5.6f, 0.32f, 1.8f),
                palette.timber,
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Quest house porch awning",
                root,
                new Vector3(0f, 3.1f, -3.15f),
                new Vector3(5.9f, 0.22f, 2.1f),
                palette.canopy,
                false);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Quest house porch post",
                    root,
                    new Vector3(side * 2.35f, 1.65f, -3.55f),
                    new Vector3(0.1f, 1.45f, 0.1f),
                    palette.copper,
                    false);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Quest house window",
                    root,
                    new Vector3(side * 1.9f, 2.2f, -2.83f),
                    new Vector3(1.1f, 1.25f, 0.12f),
                    palette.window,
                    false);
            }

            for (var step = 0; step < 2; step++)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Quest house access step",
                    root,
                    new Vector3(0f, 0.12f + step * 0.13f, -4.35f + step * 0.42f),
                    new Vector3(2.8f - step * 0.35f, 0.22f, 0.72f),
                    palette.road,
                    true);
            }
        }

        private static void CreateOpenHall(
            Transform root,
            CityVisualBuilder.CityArtPalette palette,
            float width,
            float depth,
            float wallHeight)
        {
            CreatePrimitive(
                PrimitiveType.Cube,
                "Institution hall floor",
                root,
                new Vector3(0f, 0.18f, 0f),
                new Vector3(width, 0.36f, depth),
                palette.stone,
                true);
            CreatePrimitive(
                PrimitiveType.Cube,
                "Institution hall rear wall",
                root,
                new Vector3(0f, wallHeight * 0.5f, depth * 0.47f),
                new Vector3(width, wallHeight, 0.42f),
                palette.wall,
                true);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "Institution hall side wall",
                    root,
                    new Vector3(side * width * 0.48f, wallHeight * 0.5f, depth * 0.12f),
                    new Vector3(0.42f, wallHeight, depth * 0.72f),
                    palette.wall,
                    true);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Institution entry column",
                    root,
                    new Vector3(side * width * 0.42f, wallHeight * 0.48f, -depth * 0.43f),
                    new Vector3(0.22f, wallHeight * 0.48f, 0.22f),
                    palette.copper,
                    false);
            }

            var leftRoof = CreatePrimitive(
                PrimitiveType.Cube,
                "Institution layered roof",
                root,
                new Vector3(-width * 0.24f, wallHeight + 0.55f, 0f),
                new Vector3(width * 0.57f, 0.4f, depth * 1.12f),
                palette.roof,
                false);
            leftRoof.transform.localRotation = Quaternion.Euler(0f, 0f, -16f);
            var rightRoof = CreatePrimitive(
                PrimitiveType.Cube,
                "Institution layered roof",
                root,
                new Vector3(width * 0.24f, wallHeight + 0.55f, 0f),
                new Vector3(width * 0.57f, 0.4f, depth * 1.12f),
                palette.roof,
                false);
            rightRoof.transform.localRotation = Quaternion.Euler(0f, 0f, 16f);
        }

        private static void CreateRingCrest(
            Transform parent,
            Vector3 localCenter,
            float radius,
            Material material)
        {
            const int segmentCount = 10;
            for (var index = 0; index < segmentCount; index++)
            {
                var angle = index * Mathf.PI * 2f / segmentCount;
                var segment = CreatePrimitive(
                    PrimitiveType.Cube,
                    "Institution crest segment",
                    parent,
                    localCenter + new Vector3(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius,
                        0f),
                    new Vector3(0.12f, radius * 0.72f, 0.12f),
                    material,
                    false);
                segment.transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    -index * 360f / segmentCount);
            }
        }

        private static void BuildAmbientRoutes(
            TetherfallGame game,
            CityDefinition city,
            CityVisualBuilder.CityArtPalette palette,
            Transform parent)
        {
            var harbor = string.Equals(city.id, CityCatalog.VeyrwaterMooring.id, StringComparison.Ordinal);
            if (harbor)
            {
                CityScheduledCitizen.Create(
                    game,
                    "ferry porter",
                    new[]
                    {
                        new Vector3(191f, 1.15f, 68f),
                        new Vector3(202f, 1.15f, 68f),
                        new Vector3(213f, 1.15f, 72f),
                        new Vector3(203f, 1.15f, 82f)
                    },
                    palette.wall,
                    palette.copper,
                    parent,
                    0.55f);
                CityScheduledCitizen.Create(
                    game,
                    "lantern runner",
                    new[]
                    {
                        new Vector3(181f, 1.15f, 86f),
                        new Vector3(190f, 1.15f, 82f),
                        new Vector3(203f, 1.15f, 84f),
                        new Vector3(215f, 1.15f, 86f)
                    },
                    palette.canopy,
                    palette.marker,
                    parent,
                    0.62f);
                return;
            }

            CityScheduledCitizen.Create(
                game,
                "market porter",
                new[]
                {
                    new Vector3(-12f, 0.9f, 66f),
                    new Vector3(8f, 0.9f, 66f),
                    new Vector3(12f, 0.9f, 78f),
                    new Vector3(-9f, 0.9f, 81f)
                },
                palette.wall,
                palette.copper,
                parent,
                0.58f);
            CityScheduledCitizen.Create(
                game,
                "Wayfarers runner",
                new[]
                {
                    new Vector3(-37f, 0.9f, 89f),
                    new Vector3(-27f, 0.9f, 86f),
                    new Vector3(-15f, 0.9f, 78f),
                    new Vector3(-8f, 0.9f, 72f)
                },
                palette.canopy,
                palette.marker,
                parent,
                0.64f);
        }

        internal static GameObject CreatePrimitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool solid)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;
            var renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            if (!solid)
            {
                var collider = primitive.GetComponent<Collider>();
                if (collider != null)
                {
                    RemoveCollider(collider);
                }
            }

            return primitive;
        }

        internal static void RemoveCollider(Collider collider)
        {
            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(collider);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }
    }

    public sealed class CityInstitutionInteractable : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private CityDirector city;
        private CityInstitutionDefinition definition;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private Vector3 baseScale;
        private Transform visualRoot;
        private HumanProceduralAnimator proceduralAnimator;
        private string cachedPrompt = "Inspect city institution";
        private float nextPromptRefreshAt;

        public string InstitutionId => definition?.id ?? string.Empty;
        public CityInstitutionDefinition Definition => definition;

        public string InteractionPrompt
        {
            get
            {
                if (game != null && Time.time >= nextPromptRefreshAt)
                {
                    RefreshPrompt();
                }

                return cachedPrompt;
            }
        }

        public static CityInstitutionInteractable Create(
            TetherfallGame game,
            CityDirector city,
            CityInstitutionDefinition definition,
            CityVisualBuilder.CityArtPalette palette,
            Transform parent)
        {
            if (game == null ||
                city == null ||
                definition == null ||
                palette == null ||
                parent == null)
            {
                return null;
            }

            var root = new GameObject($"{definition.stewardName}, {definition.stewardRole}");
            root.name = $"{definition.stewardName}, {definition.stewardRole}";
            root.transform.SetParent(parent, false);
            root.transform.position = definition.interactionPosition;
            root.transform.localScale = new Vector3(0.72f, 1f, 0.72f);
            var stewardMaterial = GetStewardMaterial(definition.kind, palette);
            var visual = TetherfallCityResidentArt.BuildCrowd(
                game,
                root.transform,
                stewardMaterial != null ? stewardMaterial.color : new Color(0.32f, 0.58f, 0.55f),
                definition.id);

            WorldVisuals.AddLabel(
                root.transform,
                definition.stewardName,
                new Vector3(0f, 1.55f, 0f),
                new Color(0.96f, 0.9f, 0.76f),
                0.071f);
            WorldVisuals.AddLabel(
                root.transform,
                definition.stewardRole,
                new Vector3(0f, 1.29f, 0f),
                new Color(0.61f, 0.82f, 0.77f),
                0.052f);

            var interactable = root.AddComponent<CityInstitutionInteractable>();
            interactable.game = game;
            interactable.city = city;
            interactable.definition = definition;
            interactable.homePosition = root.transform.position;
            interactable.homeRotation = root.transform.rotation;
            interactable.baseScale = root.transform.localScale;
            interactable.visualRoot = visual;
            interactable.proceduralAnimator = TetherfallHumanAnimation.Attach(visual, game);
            interactable.proceduralAnimator?.SetIdleVariant(HumanIdleVariant.Working);
            interactable.RefreshPrompt();
            return interactable;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null &&
                   owner == game &&
                   city?.Definition != null &&
                   definition?.activity != null;
        }

        public void Interact(TetherfallGame owner)
        {
            if (!CanInteract(owner))
            {
                return;
            }

            owner.TryUseCityInstitution(city, definition);
            if (proceduralAnimator != null)
            {
                if (definition.kind == CityInstitutionKind.Auction)
                {
                    proceduralAnimator.TriggerVendorGesture();
                }
                else if (definition.kind == CityInstitutionKind.QuestHouse ||
                         definition.kind == CityInstitutionKind.CivicManor)
                {
                    proceduralAnimator.TriggerQuestHandoff();
                }
                else
                {
                    proceduralAnimator.TriggerTalk();
                }
            }

            RefreshPrompt();
        }

        private void Update()
        {
            if (game == null || definition == null)
            {
                return;
            }

            if (!IsFinite(transform.position) ||
                (transform.position - homePosition).sqrMagnitude > 1f)
            {
                transform.position = homePosition;
                transform.rotation = homeRotation;
            }

            var playerDistanceSquared = game.PlayerTransform != null
                ? (game.PlayerTransform.position - transform.position).sqrMagnitude
                : float.PositiveInfinity;
            var playerInAnimationRange = playerDistanceSquared <= 55f * 55f;
            var playerInPresentationRange = playerDistanceSquared <= 95f * 95f;
            if (visualRoot != null &&
                visualRoot.gameObject.activeSelf != playerInPresentationRange)
            {
                visualRoot.gameObject.SetActive(playerInPresentationRange);
            }

            if (proceduralAnimator != null &&
                proceduralAnimator.enabled != playerInAnimationRange)
            {
                proceduralAnimator.enabled = playerInAnimationRange;
            }

            if (game.GameplayPaused || !playerInAnimationRange)
            {
                transform.localScale = baseScale;
                return;
            }

            var toPlayer = game.PlayerTransform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.1f && toPlayer.sqrMagnitude <= 12.25f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(toPlayer.normalized),
                    Time.deltaTime * 4.5f);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    homeRotation,
                    Time.deltaTime * 1.5f);
            }
        }

        private void RefreshPrompt()
        {
            cachedPrompt = game != null
                ? game.GetCityInstitutionPrompt(definition)
                : "Inspect city institution";
            nextPromptRefreshAt = Time.time + 0.75f;
        }

        private static Material GetStewardMaterial(
            CityInstitutionKind kind,
            CityVisualBuilder.CityArtPalette palette)
        {
            return kind switch
            {
                CityInstitutionKind.Auction => palette.roof,
                CityInstitutionKind.WayfarersGuild => palette.wall,
                CityInstitutionKind.ThreadwrightGuild => palette.canopy,
                CityInstitutionKind.CivicManor => palette.stone,
                CityInstitutionKind.QuestHouse => palette.wall,
                _ => palette.canopy
            };
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) &&
                   !float.IsNaN(value.y) &&
                   !float.IsNaN(value.z) &&
                   !float.IsInfinity(value.x) &&
                   !float.IsInfinity(value.y) &&
                   !float.IsInfinity(value.z);
        }
    }

    public sealed class CityScheduledCitizen : MonoBehaviour
    {
        private TetherfallGame game;
        private Vector3[] route = Array.Empty<Vector3>();
        private int targetIndex;
        private float speed;
        private float phase;
        private Vector3 baseScale;

        public static CityScheduledCitizen Create(
            TetherfallGame game,
            string role,
            Vector3[] route,
            Material bodyMaterial,
            Material accentMaterial,
            Transform parent,
            float speed)
        {
            if (game == null || route == null || route.Length < 2 || parent == null)
            {
                return null;
            }

            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = role;
            root.transform.SetParent(parent, false);
            root.transform.position = route[0];
            root.transform.localScale = new Vector3(0.62f, 0.92f, 0.62f);
            var collider = root.GetComponent<Collider>();
            if (collider != null)
            {
                CityInstitutionBuilder.RemoveCollider(collider);
            }

            var renderer = root.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = bodyMaterial;
            }

            CityInstitutionBuilder.CreatePrimitive(
                PrimitiveType.Cube,
                "Scheduled citizen sash",
                root.transform,
                new Vector3(0f, 0.3f, -0.37f),
                new Vector3(0.5f, 0.14f, 0.07f),
                accentMaterial,
                false);
            WorldVisuals.AddLabel(
                root.transform,
                role,
                new Vector3(0f, 1.42f, 0f),
                new Color(0.62f, 0.8f, 0.77f),
                0.051f);

            var citizen = root.AddComponent<CityScheduledCitizen>();
            citizen.game = game;
            citizen.route = route;
            citizen.targetIndex = 1;
            citizen.speed = Mathf.Clamp(speed, 0.3f, 1.1f);
            citizen.phase = route[0].x * 0.12f + route[0].z * 0.08f;
            citizen.baseScale = root.transform.localScale;
            return citizen;
        }

        private void Update()
        {
            if (game == null ||
                route.Length < 2 ||
                game.GameplayPaused ||
                game.PlayerTransform == null ||
                (game.PlayerTransform.position - transform.position).sqrMagnitude > 3025f)
            {
                return;
            }

            var target = route[targetIndex];
            var direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.08f)
            {
                targetIndex = (targetIndex + 1) % route.Length;
                return;
            }

            var normalized = direction.normalized;
            transform.position += normalized * Mathf.Min(direction.magnitude, speed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(normalized),
                Time.deltaTime * 4f);
            var step = Mathf.Sin(Time.time * 6f + phase) * 0.018f;
            transform.localScale = new Vector3(
                baseScale.x * (1f - step * 0.25f),
                baseScale.y * (1f + step),
                baseScale.z * (1f - step * 0.25f));
        }
    }

    public sealed class CityInstitutionAccentMotion : MonoBehaviour
    {
        private TetherfallGame game;
        private Vector3 center;
        private Transform target;
        private float degreesPerSecond;

        public void Initialize(
            TetherfallGame owner,
            Vector3 cityCenter,
            Transform animatedTarget,
            float rotationSpeed)
        {
            game = owner;
            center = cityCenter;
            target = animatedTarget;
            degreesPerSecond = rotationSpeed;
        }

        private void Update()
        {
            if (game == null ||
                target == null ||
                game.GameplayPaused ||
                game.PlayerTransform == null ||
                (game.PlayerTransform.position - center).sqrMagnitude > 3600f)
            {
                return;
            }

            target.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
