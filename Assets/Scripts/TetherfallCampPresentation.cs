using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tetherfall
{
    public enum TetherfallCampArchetype
    {
        RaiderRoadblock,
        ForestSalvageCamp,
        FenStiltCamp,
        RuinedWatchPost,
        CinderwoldQuarryForgeCamp,
        EliteWarCamp
    }

    /// <summary>
    /// Builds static, gameplay-neutral dressing beneath an existing enemy-camp root.
    /// Every collider is visible architecture outside the encounter's central spawn lane.
    /// </summary>
    public static class TetherfallCampPresentation
    {
        public const string PresentationRootName = "Tetherfall Camp Presentation";
        public const int MaxRenderersPerCamp = 96;
        public const int MaxCollidersPerCamp = 28;
        public const int MaxSharedMaterialsPerCamp = 7;
        public const float ClearCombatLaneRadius = 6.25f;

        private static readonly Dictionary<MaterialKey, Material> SharedMaterials =
            new Dictionary<MaterialKey, Material>();

        public static Transform Build(
            TetherfallGame game,
            EnemyCamp camp,
            EncounterDefinition encounter)
        {
            return Build(game, camp, encounter, camp != null ? camp.PersistenceId : null);
        }

        public static Transform Build(
            TetherfallGame game,
            EnemyCamp camp,
            EncounterDefinition encounter,
            string stableCampIdentity)
        {
            return camp == null
                ? null
                : BuildInternal(
                    game,
                    camp.transform,
                    encounter,
                    stableCampIdentity,
                    camp.transform.position);
        }

        public static Transform Build(
            TetherfallGame game,
            Transform campRoot,
            EncounterDefinition encounter,
            string stableCampIdentity = null)
        {
            return campRoot == null
                ? null
                : BuildInternal(
                    game,
                    campRoot,
                    encounter,
                    stableCampIdentity,
                    campRoot.position);
        }

        public static Transform Build(
            TetherfallGame game,
            Transform campRoot,
            string stableCampIdentity,
            Vector3 stableWorldPosition)
        {
            return BuildInternal(
                game,
                campRoot,
                null,
                stableCampIdentity,
                stableWorldPosition);
        }

        public static Transform Build(
            TetherfallGame game,
            Transform campRoot,
            string stableCampIdentity,
            Vector3 stableWorldPosition,
            EncounterDefinition encounter)
        {
            return BuildInternal(
                game,
                campRoot,
                encounter,
                stableCampIdentity,
                stableWorldPosition);
        }

        public static TetherfallCampArchetype SelectArchetype(
            EncounterDefinition encounter,
            string stableCampIdentity,
            Vector3 stableWorldPosition)
        {
            var region = ResolveRegion(encounter, stableCampIdentity, stableWorldPosition);
            var identity = ((encounter != null ? encounter.id : string.Empty) + "|" +
                            (stableCampIdentity ?? string.Empty)).ToLowerInvariant();
            var largestRoster = GetLargestRoster(encounter);
            var hasBrute = RosterContains(encounter, "brute");
            var hasBreaker = RosterContains(encounter, "breaker");
            var stalkerCount = CountRosterMatches(encounter, "stalker");

            if (identity.Contains("elite") ||
                identity.Contains("war_camp") ||
                identity.Contains("warcamp") ||
                largestRoster >= 7 ||
                hasBrute ||
                (region == CampRegion.Cinderwold && identity.Contains("relay_breaker")) ||
                (region == CampRegion.Cinderwold && hasBreaker && encounter != null && encounter.WaveCount > 1))
            {
                return TetherfallCampArchetype.EliteWarCamp;
            }

            if (region == CampRegion.Cinderwold)
            {
                return TetherfallCampArchetype.CinderwoldQuarryForgeCamp;
            }

            if (region == CampRegion.Morrowfen)
            {
                return identity.Contains("watch") || identity.Contains("ruin")
                    ? TetherfallCampArchetype.RuinedWatchPost
                    : TetherfallCampArchetype.FenStiltCamp;
            }

            if (region == CampRegion.Alderwake)
            {
                var forestIdentity =
                    identity.Contains("mossglass") ||
                    identity.Contains("forest") ||
                    identity.Contains("salvage");
                var rosterSuggestsForest =
                    stalkerCount >= Mathf.Max(2, (largestRoster + 1) / 2) &&
                    !identity.Contains("hollow_reed") &&
                    !identity.Contains("ridge");
                return forestIdentity || rosterSuggestsForest
                    ? TetherfallCampArchetype.ForestSalvageCamp
                    : TetherfallCampArchetype.RaiderRoadblock;
            }

            var hash = ComputeStableHash(encounter, stableCampIdentity, stableWorldPosition);
            return (TetherfallCampArchetype)(hash % 6u);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedMaterialReferences()
        {
            SharedMaterials.Clear();
        }

        private static Transform BuildInternal(
            TetherfallGame game,
            Transform campRoot,
            EncounterDefinition encounter,
            string stableCampIdentity,
            Vector3 stableWorldPosition)
        {
            if (campRoot == null)
            {
                return null;
            }

            for (var childIndex = 0; childIndex < campRoot.childCount; childIndex++)
            {
                var child = campRoot.GetChild(childIndex);
                if (child != null && child.name == PresentationRootName)
                {
                    return child;
                }
            }

            var safeWorldPosition = IsFinite(stableWorldPosition)
                ? stableWorldPosition
                : (IsFinite(campRoot.position) ? campRoot.position : Vector3.zero);
            var region = ResolveRegion(encounter, stableCampIdentity, safeWorldPosition);
            var archetype = SelectArchetype(encounter, stableCampIdentity, safeWorldPosition);
            var stableHash = ComputeStableHash(encounter, stableCampIdentity, safeWorldPosition);

            var presentationObject = new GameObject(PresentationRootName);
            var presentationRoot = presentationObject.transform;
            presentationRoot.SetParent(campRoot, false);
            var localPosition = campRoot.InverseTransformPoint(safeWorldPosition);
            presentationRoot.localPosition = IsFinite(localPosition) ? localPosition : Vector3.zero;
            presentationRoot.localRotation = Quaternion.Euler(0f, (stableHash % 4u) * 90f, 0f);
            presentationRoot.localScale = Vector3.one;

            var context = new BuildContext(
                presentationRoot,
                new CampPalette(region));
            var layoutRoot = CreateGroup(
                context,
                "Archetype - " + GetArchetypeName(archetype),
                presentationRoot,
                Vector3.zero,
                0f);

            switch (archetype)
            {
                case TetherfallCampArchetype.RaiderRoadblock:
                    BuildRaiderRoadblock(context, layoutRoot);
                    break;
                case TetherfallCampArchetype.ForestSalvageCamp:
                    BuildForestSalvageCamp(context, layoutRoot);
                    break;
                case TetherfallCampArchetype.FenStiltCamp:
                    BuildFenStiltCamp(context, layoutRoot);
                    break;
                case TetherfallCampArchetype.RuinedWatchPost:
                    BuildRuinedWatchPost(context, layoutRoot);
                    break;
                case TetherfallCampArchetype.CinderwoldQuarryForgeCamp:
                    BuildCinderwoldQuarryForgeCamp(context, layoutRoot);
                    break;
                case TetherfallCampArchetype.EliteWarCamp:
                    BuildEliteWarCamp(context, layoutRoot);
                    break;
                default:
                    BuildRaiderRoadblock(context, layoutRoot);
                    break;
            }

            if (context.BudgetLimited)
            {
                Debug.LogWarning(
                    "Camp presentation reached a hard presentation budget while building '" +
                    (stableCampIdentity ?? encounter?.id ?? campRoot.name) + "'.",
                    presentationObject);
            }

            return presentationRoot;
        }

        private static void BuildRaiderRoadblock(BuildContext context, Transform root)
        {
            CreatePalisade(context, root, "North west roadblock palisade", new Vector3(-8.1f, 0f, 9.5f), -4f, 5.2f);
            CreatePalisade(context, root, "North east roadblock palisade", new Vector3(8.2f, 0f, 9.8f), 5f, 5f);
            CreatePalisade(context, root, "South west roadblock palisade", new Vector3(-8.7f, 0f, -9.7f), 7f, 4.5f);
            CreatePalisade(context, root, "South east roadblock palisade", new Vector3(8.5f, 0f, -10.1f), -8f, 4.4f);

            CreateShelter(context, root, "West raider shelter", new Vector3(-11.4f, 0f, -2.5f), -90f, 4.7f, 3.8f, 2.35f);
            CreateShelter(context, root, "East raider shelter", new Vector3(11.5f, 0f, 3.3f), 90f, 4.5f, 3.6f, 2.25f);
            CreateWatchPlatform(
                context,
                root,
                "Roadblock watch platform",
                new Vector3(11.8f, 0f, -8.4f),
                18f,
                context.Palette.Timber,
                context.Palette.Metal);

            CreateSupplyStack(context, root, "Roadblock supply stack", new Vector3(-9.4f, 0f, 5.8f), 12f);
            CreateWeaponRack(context, root, "Roadblock weapon rack", new Vector3(-10.2f, 0f, -6.8f), 82f);
            CreateCart(context, root, "Roadblock toll cart", new Vector3(9.2f, 0f, 7.1f), -18f);
            CreateCookfire(context, root, "Roadblock cookfire", new Vector3(7.8f, 0f, -4.5f), 0f);
            CreateBanner(context, root, "North roadblock banner", new Vector3(-4.4f, 0f, 9.6f), 0f, 3.8f);
            CreateBanner(context, root, "South roadblock banner", new Vector3(4.5f, 0f, -10f), 180f, 3.5f);
        }

        private static void BuildForestSalvageCamp(BuildContext context, Transform root)
        {
            CreateShelter(context, root, "Broad salvage shelter", new Vector3(-11.3f, 0f, 1.5f), -90f, 5.4f, 4.2f, 2.45f);
            CreateShelter(context, root, "Stalker hide shelter", new Vector3(9.8f, 0f, -8.1f), 28f, 4.2f, 3.5f, 2.15f);
            CreatePalisade(context, root, "Salvage north screen", new Vector3(-6.8f, 0f, 10.1f), -18f, 4.8f);
            CreatePalisade(context, root, "Salvage east screen", new Vector3(11.2f, 0f, 4.4f), 78f, 4.4f);
            CreatePalisade(context, root, "Salvage south screen", new Vector3(-4.8f, 0f, -10.4f), 11f, 4.2f);
            CreateWatchPlatform(
                context,
                root,
                "Salvage lookout platform",
                new Vector3(11.7f, 0f, 8.8f),
                155f,
                context.Palette.DarkTimber,
                context.Palette.Timber);

            CreateCart(context, root, "Whole salvage cart", new Vector3(8.5f, 0f, 7.1f), 22f);
            CreateCart(context, root, "Broken salvage cart", new Vector3(-9.3f, 0f, -7.2f), -31f);
            CreateLogPile(context, root, "North log pile", new Vector3(-9f, 0f, 7.2f), 17f);
            CreateSupplyStack(context, root, "Forest salvage supply stack", new Vector3(8f, 0f, -4.8f), -20f);
            CreateWeaponRack(context, root, "Forest weapon rack", new Vector3(-11f, 0f, -4.5f), 88f);
            CreateCookfire(context, root, "Forest cookfire", new Vector3(7.5f, 0f, 2.6f), 24f);
            CreateBanner(context, root, "Salvage trail banner", new Vector3(4.3f, 0f, 10.5f), 12f, 3.4f);
        }

        private static void BuildFenStiltCamp(BuildContext context, Transform root)
        {
            CreateStiltDeck(context, root, "West fen stilt deck", new Vector3(-10.6f, 0f, 1.8f), 0f);
            CreateStiltDeck(context, root, "East fen stilt deck", new Vector3(10.5f, 0f, -1.8f), 180f);
            CreateLowBridge(context, root, "Fen rear bridge", new Vector3(0f, 0f, 10.5f), 0f);

            CreateShelter(context, root, "West fen shelter", new Vector3(-10.6f, 1.34f, 2.2f), -5f, 4.4f, 3.6f, 2.1f);
            CreateShelter(context, root, "East fen shelter", new Vector3(10.5f, 1.34f, -1.4f), 175f, 4.1f, 3.3f, 2.05f);
            CreatePalisade(context, root, "Fen north reed screen", new Vector3(-8.1f, 0f, -9.5f), 8f, 4.1f);
            CreatePalisade(context, root, "Fen south reed screen", new Vector3(8.4f, 0f, 9.3f), -10f, 4f);

            CreateSupplyStack(context, root, "Fen dry supply stack", new Vector3(-10.5f, 1.45f, 2.3f), 5f);
            CreateWeaponRack(context, root, "Fen deck weapon rack", new Vector3(10.3f, 1.4f, -1.2f), 175f);
            CreateCookfire(context, root, "Fen raised cookfire", new Vector3(10.5f, 1.42f, -3.1f), 180f);
            CreateBanner(context, root, "Fen bridge banner", new Vector3(-4.9f, 0.9f, 10.4f), 0f, 3.2f);
            CreateReedBundles(context, root, "Fen reed bundles", new Vector3(7.7f, 0f, -8.5f), 11f);
        }

        private static void BuildRuinedWatchPost(BuildContext context, Transform root)
        {
            CreateWatchPlatform(
                context,
                root,
                "Ruined watch platform",
                new Vector3(-11.5f, 0f, 1f),
                -72f,
                context.Palette.Stone,
                context.Palette.Metal);
            CreateStoneWall(context, root, "Ruined north wall west", new Vector3(-7.7f, 0f, 10.1f), -5f, 5f);
            CreateStoneWall(context, root, "Ruined north wall east", new Vector3(8.5f, 0f, 10.2f), 7f, 4.2f);
            CreateStoneWall(context, root, "Ruined south wall", new Vector3(-5.8f, 0f, -10.4f), 13f, 4.6f);
            CreateRuinedArch(context, root, "Ruined watch arch", new Vector3(5.3f, 0f, -10.1f), -7f);
            CreateShelter(context, root, "Ruin scavenger shelter", new Vector3(11.2f, 0f, -3.4f), 92f, 4.2f, 3.4f, 2.15f);

            CreateRubble(context, root, "Collapsed watch rubble", new Vector3(-8.1f, 0f, 7.5f), 18f);
            CreateSupplyStack(context, root, "Ruin supply stack", new Vector3(8.8f, 0f, 6.3f), -12f);
            CreateWeaponRack(context, root, "Ruin weapon rack", new Vector3(10.2f, 0f, 2.2f), 94f);
            CreateCart(context, root, "Ruin scavenger cart", new Vector3(-8.7f, 0f, -7.1f), 28f);
            CreateCookfire(context, root, "Ruin cookfire", new Vector3(7.6f, 0f, -6.1f), 0f);
            CreateBanner(context, root, "Ruined watch banner", new Vector3(5.3f, 0f, -9.9f), -7f, 4.3f);
        }

        private static void BuildCinderwoldQuarryForgeCamp(BuildContext context, Transform root)
        {
            CreateWatchPlatform(
                context,
                root,
                "Quarry scaffold platform",
                new Vector3(-11.8f, 0f, -1.2f),
                -82f,
                context.Palette.DarkTimber,
                context.Palette.Metal);
            CreateForge(context, root, "Cinderwold forge", new Vector3(10.4f, 0f, 3.4f), 90f);
            CreateShelter(context, root, "Quarry crew shelter", new Vector3(8.8f, 0f, -8.9f), 42f, 4.4f, 3.6f, 2.25f);
            CreateStoneWall(context, root, "Quarry north retaining wall", new Vector3(-5.7f, 0f, 10.5f), -9f, 5f);
            CreateStoneWall(context, root, "Quarry east retaining wall", new Vector3(11.6f, 0f, 8.2f), 78f, 4.2f);
            CreateStoneWall(context, root, "Quarry south retaining wall", new Vector3(-5.1f, 0f, -10.5f), 12f, 4.6f);

            CreateOreHoist(context, root, "Quarry ore hoist", new Vector3(-8.7f, 0f, 8f), -12f);
            CreateCart(context, root, "Quarry ore cart", new Vector3(8.4f, 0f, 7.8f), 21f);
            CreateOrePile(context, root, "Quarry ore pile", new Vector3(-8.4f, 0f, -7.7f), 19f);
            CreateSupplyStack(context, root, "Forge supply stack", new Vector3(9.3f, 0f, -4.6f), 88f);
            CreateWeaponRack(context, root, "Forge weapon rack", new Vector3(11.3f, 0f, -1.7f), 92f);
            CreateBanner(context, root, "Quarry claim banner", new Vector3(4.4f, 0f, 10.5f), 0f, 3.7f);
        }

        private static void BuildEliteWarCamp(BuildContext context, Transform root)
        {
            CreateShelter(context, root, "Elite command shelter", new Vector3(0f, 0f, 11.2f), 0f, 6.2f, 4.3f, 2.75f);
            CreatePalisade(context, root, "Elite north west palisade", new Vector3(-8.8f, 0f, 9.4f), -28f, 4.8f);
            CreatePalisade(context, root, "Elite north east palisade", new Vector3(8.8f, 0f, 9.4f), 28f, 4.8f);
            CreatePalisade(context, root, "Elite south west palisade", new Vector3(-9.3f, 0f, -9.2f), 31f, 4.6f);
            CreatePalisade(context, root, "Elite south east palisade", new Vector3(9.3f, 0f, -9.2f), -31f, 4.6f);
            CreateWatchPlatform(
                context,
                root,
                "Elite west watch platform",
                new Vector3(-12.6f, 0f, -7.4f),
                -28f,
                context.Palette.DarkTimber,
                context.Palette.Metal);
            CreateWatchPlatform(
                context,
                root,
                "Elite east watch platform",
                new Vector3(12.6f, 0f, -7.4f),
                28f,
                context.Palette.DarkTimber,
                context.Palette.Metal);

            CreateSupplyStack(context, root, "Elite west supply stack", new Vector3(-9.1f, 0f, 4.8f), 14f);
            CreateSupplyStack(context, root, "Elite east supply stack", new Vector3(9.1f, 0f, 4.8f), -14f);
            CreateWeaponRack(context, root, "Elite west weapon rack", new Vector3(-10.5f, 0f, -2.8f), 86f);
            CreateWeaponRack(context, root, "Elite east weapon rack", new Vector3(10.5f, 0f, -2.8f), -86f);
            CreateCart(context, root, "Elite baggage cart", new Vector3(-8.1f, 0f, 7.4f), 20f);
            CreateCookfire(context, root, "Elite cookfire", new Vector3(0f, 0f, -8.1f), 0f);
            CreateBanner(context, root, "Elite west banner", new Vector3(-4.2f, 0f, 10.2f), 0f, 4.5f);
            CreateBanner(context, root, "Elite east banner", new Vector3(4.2f, 0f, 10.2f), 0f, 4.5f);
            CreateCommandTable(context, root, "Elite command table", new Vector3(0f, 0f, 8.4f), 0f);
        }

        private static void CreateShelter(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw,
            float width,
            float depth,
            float height)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " left roof",
                group,
                new Vector3(-width * 0.22f, height, 0f),
                new Vector3(width * 0.56f, 0.16f, depth),
                Quaternion.Euler(0f, 0f, -25f),
                context.Palette.Cloth,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " right roof",
                group,
                new Vector3(width * 0.22f, height, 0f),
                new Vector3(width * 0.56f, 0.16f, depth),
                Quaternion.Euler(0f, 0f, 25f),
                context.Palette.Cloth,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " visible back wall",
                group,
                new Vector3(0f, height * 0.55f, depth * 0.48f),
                new Vector3(width * 0.92f, height * 1.05f, 0.14f),
                Quaternion.identity,
                context.Palette.DarkTimber,
                true);

            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " open front post",
                    group,
                    new Vector3(side * width * 0.43f, height * 0.45f, -depth * 0.43f),
                    new Vector3(0.16f, height * 0.9f, 0.16f),
                    Quaternion.identity,
                    context.Palette.Timber,
                    false);
            }

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " bedroll",
                group,
                new Vector3(-width * 0.2f, 0.14f, depth * 0.2f),
                new Vector3(width * 0.36f, 0.18f, depth * 0.34f),
                Quaternion.Euler(0f, -8f, 0f),
                context.Palette.Accent,
                false);
        }

        private static void CreatePalisade(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw,
            float length)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " visible wall",
                group,
                new Vector3(0f, 1.05f, 0f),
                new Vector3(length, 1.55f, 0.24f),
                Quaternion.Euler(0f, 0f, 1.5f),
                context.Palette.DarkTimber,
                true);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " end post",
                    group,
                    new Vector3(side * length * 0.46f, 1.32f, 0f),
                    new Vector3(0.24f, 2.65f, 0.24f),
                    Quaternion.Euler(0f, 0f, side * 3f),
                    context.Palette.Timber,
                    false);
            }

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " cross brace",
                group,
                new Vector3(0f, 1.45f, -0.16f),
                new Vector3(length * 0.92f, 0.16f, 0.16f),
                Quaternion.Euler(0f, 0f, -5f),
                context.Palette.Timber,
                false);
        }

        private static void CreateStoneWall(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw,
            float length)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " visible masonry",
                group,
                new Vector3(0f, 0.85f, 0f),
                new Vector3(length, 1.7f, 0.48f),
                Quaternion.Euler(0f, 0f, 1.5f),
                context.Palette.Stone,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " broken cap left",
                group,
                new Vector3(-length * 0.3f, 1.82f, 0f),
                new Vector3(length * 0.27f, 0.34f, 0.55f),
                Quaternion.Euler(0f, 4f, -4f),
                context.Palette.Stone,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " broken cap right",
                group,
                new Vector3(length * 0.33f, 1.65f, 0.02f),
                new Vector3(length * 0.22f, 0.3f, 0.54f),
                Quaternion.Euler(0f, -5f, 7f),
                context.Palette.Stone,
                false);
        }

        private static void CreateWatchPlatform(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw,
            Material deckMaterial,
            Material railMaterial)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " raised deck",
                group,
                new Vector3(0f, 2.15f, 0f),
                new Vector3(4.4f, 0.3f, 3.4f),
                Quaternion.identity,
                deckMaterial,
                true);

            for (var x = -1; x <= 1; x += 2)
            {
                for (var z = -1; z <= 1; z += 2)
                {
                    CreatePrimitive(
                        context,
                        PrimitiveType.Cube,
                        name + " visible support",
                        group,
                        new Vector3(x * 1.82f, 1f, z * 1.3f),
                        new Vector3(0.22f, 2f, 0.22f),
                        Quaternion.Euler(0f, x * z * 3f, 0f),
                        context.Palette.DarkTimber,
                        false);
                }
            }

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " safe rear rail",
                group,
                new Vector3(0f, 2.95f, 1.58f),
                new Vector3(4.3f, 0.18f, 0.16f),
                Quaternion.identity,
                railMaterial,
                true);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " safe side rail",
                    group,
                    new Vector3(side * 2.08f, 2.95f, 0.25f),
                    new Vector3(0.16f, 0.18f, 2.7f),
                    Quaternion.identity,
                    railMaterial,
                    true);
            }

            var rampRotation = Quaternion.Euler(-31f, 0f, 0f);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " access ramp",
                group,
                new Vector3(0f, 1.08f, -3.18f),
                new Vector3(1.8f, 0.18f, 3.55f),
                rampRotation,
                deckMaterial,
                true);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " safe ramp rail",
                    group,
                    new Vector3(side * 0.86f, 1.43f, -3.18f),
                    new Vector3(0.12f, 0.24f, 3.55f),
                    rampRotation,
                    railMaterial,
                    true);
            }
        }

        private static void CreateStiltDeck(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " raised deck",
                group,
                new Vector3(0f, 1.18f, 0f),
                new Vector3(5.8f, 0.28f, 5.4f),
                Quaternion.identity,
                context.Palette.Timber,
                true);

            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " visible stilt",
                    group,
                    new Vector3(side * 2.4f, 0.54f, 1.95f),
                    new Vector3(0.22f, 1.1f, 0.22f),
                    Quaternion.Euler(side * 2f, 0f, side * 3f),
                    context.Palette.DarkTimber,
                    false);
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " safe outer rail",
                    group,
                    new Vector3(side * 2.7f, 1.85f, 0.25f),
                    new Vector3(0.14f, 0.18f, 4.7f),
                    Quaternion.identity,
                    context.Palette.Metal,
                    true);
            }

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " safe rear rail",
                group,
                new Vector3(0f, 1.85f, 2.5f),
                new Vector3(5.35f, 0.18f, 0.14f),
                Quaternion.identity,
                context.Palette.Metal,
                true);
            var rampRotation = Quaternion.Euler(-20f, 0f, 0f);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " access ramp",
                group,
                new Vector3(0f, 0.55f, -3.5f),
                new Vector3(1.9f, 0.16f, 2.8f),
                rampRotation,
                context.Palette.Timber,
                true);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " safe ramp rail",
                    group,
                    new Vector3(side * 0.9f, 0.87f, -3.5f),
                    new Vector3(0.12f, 0.2f, 2.8f),
                    rampRotation,
                    context.Palette.Metal,
                    true);
            }
        }

        private static void CreateLowBridge(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " stilt deck",
                group,
                new Vector3(0f, 0.82f, 0f),
                new Vector3(12.2f, 0.24f, 1.65f),
                Quaternion.identity,
                context.Palette.Timber,
                true);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " safe rail",
                    group,
                    new Vector3(0f, 1.42f, side * 0.74f),
                    new Vector3(11.8f, 0.14f, 0.12f),
                    Quaternion.identity,
                    context.Palette.Metal,
                    true);
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " end ramp",
                    group,
                    new Vector3(side * 6.8f, 0.42f, 0f),
                    new Vector3(2f, 0.16f, 1.55f),
                    Quaternion.Euler(0f, 0f, side * -20f),
                    context.Palette.Timber,
                    true);
            }

            for (var index = 0; index < 4; index++)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " visible stilt",
                    group,
                    new Vector3(-4.5f + index * 3f, 0.38f, index % 2 == 0 ? -0.58f : 0.58f),
                    new Vector3(0.18f, 0.76f, 0.18f),
                    Quaternion.identity,
                    context.Palette.DarkTimber,
                    false);
            }
        }

        private static void CreateSupplyStack(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " crate large",
                group,
                new Vector3(-0.45f, 0.42f, 0f),
                new Vector3(0.9f, 0.84f, 0.9f),
                Quaternion.identity,
                context.Palette.Timber,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " crate small",
                group,
                new Vector3(0.48f, 0.3f, 0.12f),
                new Vector3(0.72f, 0.6f, 0.72f),
                Quaternion.Euler(0f, 11f, 0f),
                context.Palette.DarkTimber,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Sphere,
                name + " tied sack",
                group,
                new Vector3(0.02f, 1.02f, 0.04f),
                new Vector3(0.62f, 0.72f, 0.62f),
                Quaternion.identity,
                context.Palette.Cloth,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " binding strap",
                group,
                new Vector3(-0.45f, 0.43f, -0.47f),
                new Vector3(0.12f, 0.72f, 0.08f),
                Quaternion.identity,
                context.Palette.Metal,
                false);
        }

        private static void CreateWeaponRack(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " upright",
                    group,
                    new Vector3(side * 0.9f, 0.85f, 0f),
                    new Vector3(0.14f, 1.7f, 0.14f),
                    Quaternion.Euler(0f, 0f, side * 4f),
                    context.Palette.Timber,
                    false);
            }

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " crossbar",
                group,
                new Vector3(0f, 1.15f, 0f),
                new Vector3(1.9f, 0.14f, 0.14f),
                Quaternion.identity,
                context.Palette.DarkTimber,
                false);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " visible weapon",
                    group,
                    new Vector3(side * 0.38f, 1.1f, -0.12f),
                    new Vector3(0.1f, 1.75f, 0.08f),
                    Quaternion.Euler(0f, 0f, side * 12f),
                    context.Palette.Metal,
                    false);
            }
        }

        private static void CreateCart(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " open bed",
                group,
                new Vector3(0f, 0.72f, 0f),
                new Vector3(2.25f, 0.45f, 1.55f),
                Quaternion.Euler(-4f, 0f, 0f),
                context.Palette.DarkTimber,
                false);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cylinder,
                    name + " visible wheel",
                    group,
                    new Vector3(side * 1.18f, 0.56f, 0f),
                    new Vector3(0.62f, 0.12f, 0.62f),
                    Quaternion.Euler(0f, 0f, 90f),
                    context.Palette.Timber,
                    false);
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " pull handle",
                    group,
                    new Vector3(side * 0.55f, 0.45f, -1.55f),
                    new Vector3(0.12f, 0.12f, 2.1f),
                    Quaternion.Euler(7f, 0f, 0f),
                    context.Palette.Timber,
                    false);
            }

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " bundled load",
                group,
                new Vector3(0.18f, 1.05f, 0.1f),
                new Vector3(1.4f, 0.34f, 1f),
                Quaternion.Euler(0f, 7f, 0f),
                context.Palette.Cloth,
                false);
        }

        private static void CreateCookfire(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            for (var index = 0; index < 4; index++)
            {
                var angle = index * Mathf.PI * 0.5f;
                CreatePrimitive(
                    context,
                    PrimitiveType.Sphere,
                    name + " ring stone",
                    group,
                    new Vector3(Mathf.Cos(angle) * 0.62f, 0.16f, Mathf.Sin(angle) * 0.62f),
                    new Vector3(0.55f, 0.3f, 0.42f),
                    Quaternion.Euler(0f, index * 90f, 0f),
                    context.Palette.Stone,
                    false);
            }

            CreatePrimitive(
                context,
                PrimitiveType.Sphere,
                name + " ember bed",
                group,
                new Vector3(0f, 0.22f, 0f),
                new Vector3(0.72f, 0.28f, 0.72f),
                Quaternion.identity,
                context.Palette.Ember,
                false);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " spit post",
                    group,
                    new Vector3(side * 0.82f, 0.78f, 0f),
                    new Vector3(0.1f, 1.45f, 0.1f),
                    Quaternion.Euler(0f, 0f, side * 7f),
                    context.Palette.Metal,
                    false);
            }

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " cooking spit",
                group,
                new Vector3(0f, 1.24f, 0f),
                new Vector3(1.85f, 0.08f, 0.08f),
                Quaternion.identity,
                context.Palette.Metal,
                false);
        }

        private static void CreateBanner(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw,
            float height)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " pole",
                group,
                new Vector3(0f, height * 0.5f, 0f),
                new Vector3(0.12f, height, 0.12f),
                Quaternion.identity,
                context.Palette.Metal,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " crossbar",
                group,
                new Vector3(0.48f, height - 0.38f, 0f),
                new Vector3(1.1f, 0.1f, 0.1f),
                Quaternion.identity,
                context.Palette.Metal,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " cloth",
                group,
                new Vector3(0.48f, height - 1.05f, 0f),
                new Vector3(0.95f, 1.3f, 0.08f),
                Quaternion.Euler(0f, 0f, -4f),
                context.Palette.Accent,
                false);
        }

        private static void CreateLogPile(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            for (var index = 0; index < 6; index++)
            {
                var row = index / 3;
                var column = index % 3;
                CreatePrimitive(
                    context,
                    PrimitiveType.Cylinder,
                    name + " cut timber",
                    group,
                    new Vector3(-0.72f + column * 0.72f, 0.28f + row * 0.46f, 0f),
                    new Vector3(0.24f, 1.15f, 0.24f),
                    Quaternion.Euler(0f, 0f, 90f),
                    context.Palette.Timber,
                    false);
            }
        }

        private static void CreateReedBundles(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            for (var bundle = 0; bundle < 3; bundle++)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cylinder,
                    name + " tied bundle",
                    group,
                    new Vector3((bundle - 1) * 0.52f, 0.72f, bundle % 2 * 0.25f),
                    new Vector3(0.28f, 0.72f + bundle * 0.08f, 0.28f),
                    Quaternion.Euler(bundle * 4f, 0f, (bundle - 1) * 5f),
                    context.Palette.Cloth,
                    false);
            }
        }

        private static void CreateRuinedArch(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            for (var side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Cube,
                    name + " visible column",
                    group,
                    new Vector3(side * 1.8f, 1.55f, 0f),
                    new Vector3(0.72f, 3.1f, 0.82f),
                    Quaternion.Euler(0f, side * 3f, side * 2f),
                    context.Palette.Stone,
                    true);
            }

            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " visible lintel",
                group,
                new Vector3(0f, 3.05f, 0f),
                new Vector3(4.2f, 0.65f, 0.84f),
                Quaternion.Euler(0f, 0f, -3f),
                context.Palette.Stone,
                true);
        }

        private static void CreateRubble(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            for (var index = 0; index < 6; index++)
            {
                CreatePrimitive(
                    context,
                    index % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere,
                    name + " loose stone",
                    group,
                    new Vector3((index % 3 - 1) * 0.62f, 0.2f + index / 3 * 0.3f, (index % 2) * 0.5f),
                    new Vector3(0.62f, 0.42f, 0.55f),
                    Quaternion.Euler(index * 9f, index * 23f, index * 7f),
                    context.Palette.Stone,
                    false);
            }
        }

        private static void CreateForge(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " visible forge bed",
                group,
                new Vector3(0f, 0.62f, 0f),
                new Vector3(3.4f, 1.24f, 2.4f),
                Quaternion.identity,
                context.Palette.Stone,
                true);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " coal trough",
                group,
                new Vector3(0f, 1.3f, 0f),
                new Vector3(2.6f, 0.28f, 1.5f),
                Quaternion.identity,
                context.Palette.DarkTimber,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " hot coals",
                group,
                new Vector3(0f, 1.48f, 0f),
                new Vector3(2.15f, 0.12f, 1.1f),
                Quaternion.identity,
                context.Palette.Ember,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " chimney",
                group,
                new Vector3(1.15f, 2.25f, 0.55f),
                new Vector3(0.62f, 2.7f, 0.62f),
                Quaternion.Euler(0f, 0f, -3f),
                context.Palette.Stone,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " anvil",
                group,
                new Vector3(-2f, 0.9f, 0.15f),
                new Vector3(1.15f, 0.3f, 0.62f),
                Quaternion.identity,
                context.Palette.Metal,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " anvil stand",
                group,
                new Vector3(-2f, 0.43f, 0.15f),
                new Vector3(0.58f, 0.86f, 0.58f),
                Quaternion.identity,
                context.Palette.Timber,
                false);
        }

        private static void CreateOreHoist(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " mast",
                group,
                new Vector3(0f, 2.1f, 0f),
                new Vector3(0.28f, 4.2f, 0.28f),
                Quaternion.Euler(0f, 0f, -4f),
                context.Palette.DarkTimber,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " boom",
                group,
                new Vector3(1.35f, 3.75f, 0f),
                new Vector3(2.9f, 0.24f, 0.24f),
                Quaternion.Euler(0f, 0f, 6f),
                context.Palette.Timber,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cylinder,
                name + " winding drum",
                group,
                new Vector3(0.3f, 1.4f, 0f),
                new Vector3(0.48f, 0.55f, 0.48f),
                Quaternion.Euler(90f, 0f, 0f),
                context.Palette.Metal,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " hanging cable",
                group,
                new Vector3(2.65f, 2.45f, 0f),
                new Vector3(0.07f, 2.4f, 0.07f),
                Quaternion.identity,
                context.Palette.Metal,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " ore bucket",
                group,
                new Vector3(2.65f, 1.12f, 0f),
                new Vector3(0.9f, 0.72f, 0.82f),
                Quaternion.Euler(0f, 0f, 3f),
                context.Palette.DarkTimber,
                false);
        }

        private static void CreateOrePile(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            for (var index = 0; index < 6; index++)
            {
                CreatePrimitive(
                    context,
                    PrimitiveType.Sphere,
                    name + " loose ore",
                    group,
                    new Vector3((index % 3 - 1) * 0.72f, 0.28f + index / 3 * 0.42f, (index % 2) * 0.55f),
                    new Vector3(0.72f, 0.55f, 0.64f),
                    Quaternion.Euler(index * 11f, index * 19f, index * 5f),
                    index % 3 == 0 ? context.Palette.Metal : context.Palette.Stone,
                    false);
            }
        }

        private static void CreateCommandTable(
            BuildContext context,
            Transform parent,
            string name,
            Vector3 position,
            float yaw)
        {
            var group = CreateGroup(context, name, parent, position, yaw);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " top",
                group,
                new Vector3(0f, 0.92f, 0f),
                new Vector3(2.8f, 0.18f, 1.55f),
                Quaternion.identity,
                context.Palette.Timber,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " map",
                group,
                new Vector3(0f, 1.03f, 0f),
                new Vector3(1.9f, 0.04f, 1.05f),
                Quaternion.Euler(0f, 7f, 0f),
                context.Palette.Cloth,
                false);
            CreatePrimitive(
                context,
                PrimitiveType.Cube,
                name + " trestle",
                group,
                new Vector3(0f, 0.43f, 0f),
                new Vector3(1.8f, 0.76f, 0.25f),
                Quaternion.identity,
                context.Palette.DarkTimber,
                false);
        }

        private static Transform CreateGroup(
            BuildContext context,
            string name,
            Transform parent,
            Vector3 localPosition,
            float localYaw)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent != null ? parent : context.Root, false);
            group.localPosition = IsFinite(localPosition) ? localPosition : Vector3.zero;
            group.localRotation = Quaternion.Euler(0f, IsFinite(localYaw) ? localYaw : 0f, 0f);
            group.localScale = Vector3.one;
            return group;
        }

        private static GameObject CreatePrimitive(
            BuildContext context,
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material,
            bool collidable)
        {
            if (!context.CanCreate(material, collidable))
            {
                return null;
            }

            var visual = GameObject.CreatePrimitive(primitiveType);
            visual.name = name;
            visual.transform.SetParent(parent != null ? parent : context.Root, false);
            visual.transform.localPosition = IsFinite(localPosition) ? localPosition : Vector3.zero;
            visual.transform.localScale = SanitizeScale(localScale);
            visual.transform.localRotation = IsFinite(localRotation) ? localRotation : Quaternion.identity;

            var visualRenderer = visual.GetComponent<Renderer>();
            if (visualRenderer != null)
            {
                visualRenderer.sharedMaterial = material;
            }

            var visualCollider = visual.GetComponent<Collider>();
            if (collidable)
            {
                visual.isStatic = true;
            }
            else if (visualCollider != null)
            {
                visualCollider.enabled = false;
                if (Application.isPlaying)
                {
                    Object.Destroy(visualCollider);
                }
                else
                {
                    Object.DestroyImmediate(visualCollider);
                }
            }

            context.RecordCreated(material, collidable && visualCollider != null);
            return visual;
        }

        private static Material GetSharedMaterial(CampRegion region, MaterialRole role)
        {
            var key = new MaterialKey(region, role);
            if (SharedMaterials.TryGetValue(key, out var existing) && existing != null)
            {
                return existing;
            }

            var prototype = Resources.Load<Material>("TetherfallRuntimeMaterial");
            Material material;
            if (prototype != null)
            {
                material = new Material(prototype);
            }
            else
            {
                var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ??
                             Shader.Find("Unlit/Color");
                if (shader == null)
                {
                    Debug.LogError("Tetherfall camp presentation could not locate a runtime shader.");
                    return null;
                }

                material = new Material(shader);
            }

            var color = GetMaterialColor(region, role);
            material.name = "Tetherfall Camp " + region + " " + role;
            material.hideFlags = HideFlags.HideAndDontSave;
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            var metallic = role == MaterialRole.Metal ? 0.58f : 0f;
            var smoothness = role == MaterialRole.Metal ? 0.42f : 0.18f;
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            SharedMaterials[key] = material;
            return material;
        }

        private static Color GetMaterialColor(CampRegion region, MaterialRole role)
        {
            switch (region)
            {
                case CampRegion.Morrowfen:
                    switch (role)
                    {
                        case MaterialRole.Timber: return new Color(0.30f, 0.38f, 0.36f);
                        case MaterialRole.DarkTimber: return new Color(0.16f, 0.21f, 0.22f);
                        case MaterialRole.Cloth: return new Color(0.32f, 0.39f, 0.45f);
                        case MaterialRole.Stone: return new Color(0.34f, 0.39f, 0.42f);
                        case MaterialRole.Metal: return new Color(0.45f, 0.53f, 0.50f);
                        case MaterialRole.Accent: return new Color(0.30f, 0.55f, 0.58f);
                        case MaterialRole.Ember: return new Color(0.92f, 0.38f, 0.18f);
                    }
                    break;
                case CampRegion.Cinderwold:
                    switch (role)
                    {
                        case MaterialRole.Timber: return new Color(0.35f, 0.27f, 0.23f);
                        case MaterialRole.DarkTimber: return new Color(0.16f, 0.14f, 0.15f);
                        case MaterialRole.Cloth: return new Color(0.43f, 0.23f, 0.18f);
                        case MaterialRole.Stone: return new Color(0.28f, 0.25f, 0.27f);
                        case MaterialRole.Metal: return new Color(0.46f, 0.40f, 0.37f);
                        case MaterialRole.Accent: return new Color(0.66f, 0.22f, 0.13f);
                        case MaterialRole.Ember: return new Color(1f, 0.34f, 0.08f);
                    }
                    break;
                default:
                    switch (role)
                    {
                        case MaterialRole.Timber: return new Color(0.42f, 0.32f, 0.20f);
                        case MaterialRole.DarkTimber: return new Color(0.22f, 0.19f, 0.16f);
                        case MaterialRole.Cloth: return new Color(0.31f, 0.40f, 0.30f);
                        case MaterialRole.Stone: return new Color(0.38f, 0.43f, 0.39f);
                        case MaterialRole.Metal: return new Color(0.49f, 0.43f, 0.34f);
                        case MaterialRole.Accent: return new Color(0.52f, 0.22f, 0.22f);
                        case MaterialRole.Ember: return new Color(0.96f, 0.40f, 0.13f);
                    }
                    break;
            }

            return Color.magenta;
        }

        private static CampRegion ResolveRegion(
            EncounterDefinition encounter,
            string stableCampIdentity,
            Vector3 stableWorldPosition)
        {
            var identity = ((encounter != null ? encounter.id : string.Empty) + "|" +
                            (stableCampIdentity ?? string.Empty)).ToLowerInvariant();
            if (identity.Contains("morrowfen"))
            {
                return CampRegion.Morrowfen;
            }

            if (identity.Contains("cinderwold"))
            {
                return CampRegion.Cinderwold;
            }

            if (identity.Contains("alderwake"))
            {
                return CampRegion.Alderwake;
            }

            if (IsFinite(stableWorldPosition))
            {
                if (stableWorldPosition.x >= 80f)
                {
                    return CampRegion.Morrowfen;
                }

                if (stableWorldPosition.x <= -70f)
                {
                    return CampRegion.Cinderwold;
                }
            }

            return CampRegion.Alderwake;
        }

        private static uint ComputeStableHash(
            EncounterDefinition encounter,
            string stableCampIdentity,
            Vector3 stableWorldPosition)
        {
            unchecked
            {
                var hash = 2166136261u;
                AppendHash(ref hash, encounter != null ? encounter.id : string.Empty);
                AppendHash(ref hash, stableCampIdentity ?? string.Empty);
                AppendHash(ref hash, Mathf.RoundToInt(IsFinite(stableWorldPosition.x) ? stableWorldPosition.x : 0f).ToString());
                AppendHash(ref hash, Mathf.RoundToInt(IsFinite(stableWorldPosition.z) ? stableWorldPosition.z : 0f).ToString());
                if (encounter != null)
                {
                    if (encounter.enemyIds != null)
                    {
                        foreach (var enemyId in encounter.enemyIds)
                        {
                            AppendHash(ref hash, enemyId ?? string.Empty);
                        }
                    }

                    if (encounter.waves != null)
                    {
                        foreach (var wave in encounter.waves)
                        {
                            if (wave == null)
                            {
                                continue;
                            }

                            foreach (var enemyId in wave)
                            {
                                AppendHash(ref hash, enemyId ?? string.Empty);
                            }
                        }
                    }
                }

                return hash;
            }
        }

        private static void AppendHash(ref uint hash, string value)
        {
            unchecked
            {
                for (var index = 0; index < value.Length; index++)
                {
                    hash ^= value[index];
                    hash *= 16777619u;
                }
            }
        }

        private static int GetLargestRoster(EncounterDefinition encounter)
        {
            if (encounter == null)
            {
                return 0;
            }

            var largest = encounter.enemyIds != null ? encounter.enemyIds.Length : 0;
            if (encounter.waves == null)
            {
                return largest;
            }

            foreach (var wave in encounter.waves)
            {
                if (wave != null)
                {
                    largest = Mathf.Max(largest, wave.Length);
                }
            }

            return largest;
        }

        private static bool RosterContains(EncounterDefinition encounter, string token)
        {
            return CountRosterMatches(encounter, token) > 0;
        }

        private static int CountRosterMatches(EncounterDefinition encounter, string token)
        {
            if (encounter == null || string.IsNullOrEmpty(token))
            {
                return 0;
            }

            var matches = 0;
            if (encounter.enemyIds != null)
            {
                foreach (var enemyId in encounter.enemyIds)
                {
                    if (!string.IsNullOrEmpty(enemyId) &&
                        enemyId.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matches++;
                    }
                }
            }

            if (matches > 0 || encounter.waves == null)
            {
                return matches;
            }

            foreach (var wave in encounter.waves)
            {
                if (wave == null)
                {
                    continue;
                }

                foreach (var enemyId in wave)
                {
                    if (!string.IsNullOrEmpty(enemyId) &&
                        enemyId.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matches++;
                    }
                }
            }

            return matches;
        }

        private static string GetArchetypeName(TetherfallCampArchetype archetype)
        {
            switch (archetype)
            {
                case TetherfallCampArchetype.RaiderRoadblock: return "Raider Roadblock";
                case TetherfallCampArchetype.ForestSalvageCamp: return "Forest Salvage Camp";
                case TetherfallCampArchetype.FenStiltCamp: return "Fen Stilt Camp";
                case TetherfallCampArchetype.RuinedWatchPost: return "Ruined Watch Post";
                case TetherfallCampArchetype.CinderwoldQuarryForgeCamp: return "Cinderwold Quarry Forge Camp";
                case TetherfallCampArchetype.EliteWarCamp: return "Elite War Camp";
                default: return archetype.ToString();
            }
        }

        private static Vector3 SanitizeScale(Vector3 value)
        {
            if (!IsFinite(value))
            {
                return Vector3.one;
            }

            return new Vector3(
                Mathf.Clamp(Mathf.Abs(value.x), 0.02f, 50f),
                Mathf.Clamp(Mathf.Abs(value.y), 0.02f, 50f),
                Mathf.Clamp(Mathf.Abs(value.z), 0.02f, 50f));
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private enum CampRegion
        {
            Alderwake,
            Morrowfen,
            Cinderwold
        }

        private enum MaterialRole
        {
            Timber,
            DarkTimber,
            Cloth,
            Stone,
            Metal,
            Accent,
            Ember
        }

        private readonly struct MaterialKey : IEquatable<MaterialKey>
        {
            private readonly CampRegion region;
            private readonly MaterialRole role;

            public MaterialKey(CampRegion materialRegion, MaterialRole materialRole)
            {
                region = materialRegion;
                role = materialRole;
            }

            public bool Equals(MaterialKey other)
            {
                return region == other.region && role == other.role;
            }

            public override bool Equals(object other)
            {
                return other is MaterialKey key && Equals(key);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)region * 397) ^ (int)role;
                }
            }
        }

        private sealed class CampPalette
        {
            public readonly Material Timber;
            public readonly Material DarkTimber;
            public readonly Material Cloth;
            public readonly Material Stone;
            public readonly Material Metal;
            public readonly Material Accent;
            public readonly Material Ember;

            public CampPalette(CampRegion region)
            {
                Timber = GetSharedMaterial(region, MaterialRole.Timber);
                DarkTimber = GetSharedMaterial(region, MaterialRole.DarkTimber);
                Cloth = GetSharedMaterial(region, MaterialRole.Cloth);
                Stone = GetSharedMaterial(region, MaterialRole.Stone);
                Metal = GetSharedMaterial(region, MaterialRole.Metal);
                Accent = GetSharedMaterial(region, MaterialRole.Accent);
                Ember = GetSharedMaterial(region, MaterialRole.Ember);
            }
        }

        private sealed class BuildContext
        {
            private readonly HashSet<Material> materials = new HashSet<Material>();

            public readonly Transform Root;
            public readonly CampPalette Palette;
            public int RendererCount { get; private set; }
            public int ColliderCount { get; private set; }
            public bool BudgetLimited { get; private set; }

            public BuildContext(Transform root, CampPalette palette)
            {
                Root = root;
                Palette = palette;
            }

            public bool CanCreate(Material material, bool collidable)
            {
                var addsMaterial = material != null && !materials.Contains(material);
                if (RendererCount >= MaxRenderersPerCamp ||
                    (collidable && ColliderCount >= MaxCollidersPerCamp) ||
                    (addsMaterial && materials.Count >= MaxSharedMaterialsPerCamp))
                {
                    BudgetLimited = true;
                    return false;
                }

                return true;
            }

            public void RecordCreated(Material material, bool colliderCreated)
            {
                RendererCount++;
                if (material != null)
                {
                    materials.Add(material);
                }

                if (colliderCreated)
                {
                    ColliderCount++;
                }
            }
        }
    }
}
