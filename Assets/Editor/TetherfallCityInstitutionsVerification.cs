using System;
using System.Collections.Generic;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallCityInstitutionsVerification
    {
        private const string MenuPath = "Tetherfall/Verify City Institutions";
        private static int assertionCount;

        [MenuItem(MenuPath, false, 2190)]
        public static void VerifyCityInstitutions()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "City institution verification is edit-mode only. Stop Play Mode and run it again.");
            }

            assertionCount = 0;
            VerifyCatalog();
            VerifyDistrictCatalog();
            VerifyActivityPersistenceAndRewardGuard();
            VerifyWorldBuild();
            VerifyArenaEscalation();
            Debug.Log(
                $"City institution verification passed: {assertionCount} assertions, " +
                $"{CityInstitutionCatalog.All.Count} staffed institutions, " +
                $"{CityDistrictCatalog.Districts.Count} named districts, " +
                $"{CityDistrictCatalog.MarketStalls.Count} cooperative stalls, and " +
                $"{CityDistrictCatalog.TraversalRoutes.Count} traversal routes.");
        }

        [MenuItem(MenuPath, true, 2190)]
        private static bool CanVerifyCityInstitutions()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void VerifyCatalog()
        {
            var institutionIds = new HashSet<string>(StringComparer.Ordinal);
            var activityIds = new HashSet<string>(StringComparer.Ordinal);
            var stewardNames = new HashSet<string>(StringComparer.Ordinal);
            var alderwakeKinds = new HashSet<CityInstitutionKind>();
            var veyrwaterKinds = new HashSet<CityInstitutionKind>();
            var alderwakeHouses = 0;
            var veyrwaterHouses = 0;

            var definitions = CityInstitutionCatalog.All;
            Check(definitions.Count >= 15, "The institution catalog is missing planned services.");
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                Check(definition != null, $"Institution entry {index} is null.");
                Check(
                    !string.IsNullOrWhiteSpace(definition.id) && institutionIds.Add(definition.id),
                    $"Institution ID '{definition.id}' is empty or duplicated.");
                Check(
                    !string.IsNullOrWhiteSpace(definition.stewardName) &&
                    stewardNames.Add(definition.stewardName),
                    $"Institution steward '{definition.stewardName}' is empty or duplicated.");
                Check(
                    !string.IsNullOrWhiteSpace(definition.displayName) &&
                    !string.IsNullOrWhiteSpace(definition.stewardRole) &&
                    !string.IsNullOrWhiteSpace(definition.locationHint),
                    $"Institution '{definition.id}' is missing readable world copy.");
                Check(
                    IsFinite(definition.buildingPosition) &&
                    IsFinite(definition.interactionPosition),
                    $"Institution '{definition.id}' has a non-finite placement.");
                Check(
                    Vector3.Distance(
                        definition.buildingPosition,
                        definition.interactionPosition) <= 8.5f,
                    $"Institution '{definition.id}' places its steward too far from the entrance.");
                Check(
                    definition.activity != null &&
                    !string.IsNullOrWhiteSpace(definition.activity.id) &&
                    activityIds.Add(definition.activity.id),
                    $"Institution '{definition.id}' has no unique stable activity key.");
                Check(
                    CityInstitutionCatalog.Find(definition.id) == definition &&
                    CityInstitutionCatalog.FindActivity(definition.activity.id) ==
                    definition.activity,
                    $"Institution '{definition.id}' cannot be resolved from its stable keys.");
                Check(
                    definition.activity.cooldownSeconds > 0f &&
                    definition.activity.cooldownSeconds <= 3600f,
                    $"Institution '{definition.id}' has an invalid cooldown.");
                Check(
                    definition.activity.favorReward >= 0 &&
                    definition.activity.experienceReward >= 0,
                    $"Institution '{definition.id}' has a negative reward.");

                var city = GetCity(definition.cityId);
                Check(city != null, $"Institution '{definition.id}' references an unknown city.");
                Check(
                    Vector3.Distance(city.center, definition.interactionPosition) <= 66f,
                    $"Institution '{definition.id}' is outside the walkable city footprint.");
                Check(
                    CityInstitutionCatalog.FindActivityForCity(
                        definition.cityId,
                        definition.activity.id) == definition.activity,
                    $"Institution '{definition.id}' is not indexed under its city.");

                if (definition.cityId == CityCatalog.AlderwakeHearthmarket.id)
                {
                    alderwakeKinds.Add(definition.kind);
                    Check(
                        definition.activity.id.StartsWith("city.alderwake.", StringComparison.Ordinal),
                        $"Alderwake activity '{definition.activity.id}' has an unstable prefix.");
                    if (definition.kind == CityInstitutionKind.QuestHouse)
                    {
                        alderwakeHouses++;
                    }
                }
                else
                {
                    veyrwaterKinds.Add(definition.kind);
                    Check(
                        definition.activity.id.StartsWith("city.veyrwater.", StringComparison.Ordinal),
                        $"Veyrwater activity '{definition.activity.id}' has an unstable prefix.");
                    if (definition.kind == CityInstitutionKind.QuestHouse)
                    {
                        veyrwaterHouses++;
                    }
                }

                if (definition.action == CityInstitutionAction.Auction)
                {
                    VerifyAuction(definition);
                }
                else
                {
                    Check(
                        definition.auctionLots == null || definition.auctionLots.Length == 0,
                        $"Non-auction institution '{definition.id}' contains auction lots.");
                }
            }

            VerifyRequiredKinds("Alderwake", alderwakeKinds);
            VerifyRequiredKinds("Veyrwater", veyrwaterKinds);
            Check(alderwakeHouses >= 3, "Alderwake needs at least three questgiver houses.");
            Check(veyrwaterHouses >= 2, "Veyrwater needs at least two questgiver houses.");
            VerifyTrainingDefinition(CityCatalog.AlderwakeHearthmarket);
            VerifyTrainingDefinition(CityCatalog.VeyrwaterMooring);
        }

        private static void VerifyDistrictCatalog()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            Check(
                CityDistrictCatalog.Districts.Count == 6,
                "Each city should expose three named districts.");
            Check(
                CityDistrictCatalog.MarketStalls.Count == 4,
                "Each city should expose two staffed cooperative market stalls.");
            Check(
                CityDistrictCatalog.TraversalRoutes.Count == 2,
                "Each city should expose one service-alley traversal route.");

            var districts = CityDistrictCatalog.Districts;
            for (var index = 0; index < districts.Count; index++)
            {
                var definition = districts[index];
                var city = GetCity(definition?.cityId);
                Check(
                    definition != null &&
                    !string.IsNullOrWhiteSpace(definition.id) &&
                    ids.Add(definition.id),
                    $"District entry {index} has an empty or duplicate stable ID.");
                Check(
                    !string.IsNullOrWhiteSpace(definition.displayName) &&
                    !string.IsNullOrWhiteSpace(definition.character),
                    $"District '{definition.id}' is missing wayfinding copy.");
                Check(
                    city != null &&
                    IsFinite(definition.markerPosition) &&
                    Vector3.Distance(city.center, definition.markerPosition) <= 66f,
                    $"District '{definition.id}' has an invalid or unreachable marker.");
            }

            var stalls = CityDistrictCatalog.MarketStalls;
            var merchantNames = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < stalls.Count; index++)
            {
                var definition = stalls[index];
                var city = GetCity(definition?.cityId);
                Check(
                    definition != null &&
                    !string.IsNullOrWhiteSpace(definition.id) &&
                    ids.Add(definition.id),
                    $"Market stall entry {index} has an empty or duplicate stable ID.");
                Check(
                    !string.IsNullOrWhiteSpace(definition.displayName) &&
                    !string.IsNullOrWhiteSpace(definition.waresLabel) &&
                    !string.IsNullOrWhiteSpace(definition.merchantName) &&
                    merchantNames.Add(definition.merchantName),
                    $"Market stall '{definition.id}' has incomplete or duplicate merchant copy.");
                Check(
                    ProvisioningCatalog.GetVendor(definition.vendorId) != null,
                    $"Market stall '{definition.id}' references unknown cooperative stock.");
                Check(
                    city != null &&
                    IsFinite(definition.position) &&
                    Vector3.Distance(city.center, definition.position) <= 66f,
                    $"Market stall '{definition.id}' is outside the walkable market.");
            }

            var routes = CityDistrictCatalog.TraversalRoutes;
            for (var index = 0; index < routes.Count; index++)
            {
                var definition = routes[index];
                var city = GetCity(definition?.cityId);
                Check(
                    definition != null &&
                    !string.IsNullOrWhiteSpace(definition.id) &&
                    ids.Add(definition.id),
                    $"Traversal route entry {index} has an empty or duplicate stable ID.");
                Check(
                    city != null &&
                    CityDistrictCatalog.GetTraversalRouteForCity(definition.cityId) == definition,
                    $"Traversal route '{definition.id}' is not indexed under its city.");
                Check(
                    !string.IsNullOrWhiteSpace(definition.displayName) &&
                    !string.IsNullOrWhiteSpace(definition.routeCharacter) &&
                    IsFinite(definition.startPosition),
                    $"Traversal route '{definition.id}' is missing readable or finite setup.");
                Check(
                    definition.checkpoints != null && definition.checkpoints.Length >= 8,
                    $"Traversal route '{definition.id}' is too short to explore a district.");
                Check(
                    definition.timeLimitSeconds >= 45f && definition.timeLimitSeconds <= 120f,
                    $"Traversal route '{definition.id}' has an unreasonable time budget.");
                Check(
                    definition.activity != null &&
                    !string.IsNullOrWhiteSpace(definition.activity.id) &&
                    CityDistrictCatalog.FindActivity(definition.activity.id) == definition.activity,
                    $"Traversal route '{definition.id}' has no stable activity record.");
                Check(
                    definition.activity.favorReward > 0 &&
                    definition.activity.favorReward <= 10 &&
                    definition.activity.experienceReward > 0 &&
                    definition.activity.experienceReward <= 20 &&
                    definition.activity.cooldownSeconds >= 180f,
                    $"Traversal route '{definition.id}' has an exploitable reward cadence.");

                var previous = definition.startPosition;
                var routeLength = 0f;
                for (var pointIndex = 0; pointIndex < definition.checkpoints.Length; pointIndex++)
                {
                    var point = definition.checkpoints[pointIndex];
                    var stepDistance = Vector3.Distance(previous, point);
                    Check(
                        IsFinite(point) &&
                        Vector3.Distance(city.center, point) <= 72f,
                        $"Traversal route '{definition.id}' checkpoint {pointIndex} is invalid.");
                    Check(
                        stepDistance >= 2f &&
                        stepDistance <= 10f &&
                        Mathf.Abs(point.y - previous.y) <= 1.5f,
                        $"Traversal route '{definition.id}' checkpoint {pointIndex} is not traversable.");
                    routeLength += stepDistance;
                    previous = point;
                }

                Check(
                    routeLength >= 38f,
                    $"Traversal route '{definition.id}' does not cross a material district span.");
            }
        }

        private static void VerifyAuction(CityInstitutionDefinition definition)
        {
            Check(
                definition.auctionLots != null && definition.auctionLots.Length >= 3,
                $"Auction '{definition.id}' does not rotate at least three lots.");
            for (var index = 0; index < definition.auctionLots.Length; index++)
            {
                var lot = definition.auctionLots[index];
                Check(
                    lot != null &&
                    !string.IsNullOrWhiteSpace(lot.title) &&
                    !string.IsNullOrWhiteSpace(lot.description),
                    $"Auction '{definition.id}' lot {index} is incomplete.");
                Check(
                    lot.cost != null && lot.cost.Count > 0,
                    $"Auction '{definition.id}' lot '{lot.title}' has no consignment.");
                var unitCount = 0;
                for (var costIndex = 0; costIndex < lot.cost.Count; costIndex++)
                {
                    var cost = lot.cost[costIndex];
                    Check(
                        cost != null && cost.amount > 0,
                        $"Auction '{definition.id}' lot '{lot.title}' has an invalid cost.");
                    unitCount += cost.amount;
                }

                Check(
                    definition.activity.favorReward >= unitCount * 2 &&
                    definition.activity.favorReward <= unitCount * 4,
                    $"Auction '{definition.id}' lot '{lot.title}' falls outside the fair bid band.");
            }

            var first = CityInstitutionCatalog.GetAuctionLot(definition, 0);
            var second = CityInstitutionCatalog.GetAuctionLot(definition, 1);
            var wrapped = CityInstitutionCatalog.GetAuctionLot(
                definition,
                definition.auctionLots.Length);
            Check(first != null && second != null && first != second, $"Auction '{definition.id}' does not rotate.");
            Check(first == wrapped, $"Auction '{definition.id}' does not wrap its lot rotation.");
        }

        private static void VerifyRequiredKinds(
            string cityName,
            HashSet<CityInstitutionKind> kinds)
        {
            Check(kinds.Contains(CityInstitutionKind.Bazaar), $"{cityName} has no bazaar.");
            Check(kinds.Contains(CityInstitutionKind.Auction), $"{cityName} has no auction hall.");
            Check(
                kinds.Contains(CityInstitutionKind.WayfarersGuild),
                $"{cityName} has no Wayfarers guild.");
            Check(
                kinds.Contains(CityInstitutionKind.ThreadwrightGuild),
                $"{cityName} has no Threadwright equivalent.");
            Check(kinds.Contains(CityInstitutionKind.CivicManor), $"{cityName} has no civic manor.");
            Check(kinds.Contains(CityInstitutionKind.QuestHouse), $"{cityName} has no questgiver house.");
        }

        private static void VerifyTrainingDefinition(CityDefinition city)
        {
            CityActivityDefinition training = null;
            for (var index = 0; index < city.activities.Count; index++)
            {
                if (city.activities[index]?.kind == CityActivityKind.Training)
                {
                    training = city.activities[index];
                    break;
                }
            }

            Check(training != null, $"{city.displayName} has no arena activity.");
            Check(
                training.enemyIds != null && training.enemyIds.Length >= 3,
                $"{city.displayName} arena has too few controlled threats.");
            Check(IsFinite(training.arenaPosition), $"{city.displayName} arena position is invalid.");
        }

        private static void VerifyActivityPersistenceAndRewardGuard()
        {
            var root = new GameObject("City Institution Persistence Verification");
            try
            {
                var game = root.AddComponent<TetherfallGame>();
                var auction = CityInstitutionCatalog.Find("institution.alderwake.auction");
                var traversal = CityDistrictCatalog.GetTraversalRouteForCity(
                    CityCatalog.AlderwakeHearthmarket.id);
                var progressField = typeof(TetherfallGame).GetField(
                    "cityActivityProgress",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Check(progressField != null, "City activity progress storage is unavailable.");
                var progress = progressField.GetValue(game) as Dictionary<string, CityActivityProgress>;
                Check(progress != null, "City activity progress storage has the wrong type.");
                var cooldownTicks = DateTime.UtcNow.AddMinutes(2).Ticks;
                progress[auction.activity.id] = new CityActivityProgress
                {
                    completionCount = 2,
                    nextAvailableUtcTicks = cooldownTicks
                };
                progress[traversal.activity.id] = new CityActivityProgress
                {
                    completionCount = 1,
                    nextAvailableUtcTicks = cooldownTicks
                };

                var data = new TetherfallSaveData();
                Invoke(game, "WriteCitySave", data);
                var savedCount = 0;
                CityActivitySaveData savedAuction = null;
                CityActivitySaveData savedTraversal = null;
                for (var index = 0; index < data.cityActivities.Count; index++)
                {
                    if (data.cityActivities[index]?.id == auction.activity.id)
                    {
                        savedCount++;
                        savedAuction = data.cityActivities[index];
                    }

                    if (data.cityActivities[index]?.id == traversal.activity.id)
                    {
                        savedTraversal = data.cityActivities[index];
                    }
                }

                Check(savedCount == 1, "Institution progress was not written exactly once.");
                Check(
                    savedAuction != null &&
                    savedAuction.completionCount == 2 &&
                    savedAuction.nextAvailableUtcTicks == cooldownTicks,
                    "Institution completion history or cooldown changed during save.");
                Check(
                    savedTraversal != null &&
                    savedTraversal.completionCount == 1 &&
                    savedTraversal.nextAvailableUtcTicks == cooldownTicks,
                    "Traversal completion history or cooldown was not saved.");

                progress.Clear();
                Invoke(game, "RestoreCityState", data);
                Check(
                    progress.TryGetValue(auction.activity.id, out var restored) &&
                    restored.completionCount == 2 &&
                    restored.nextAvailableUtcTicks > DateTime.UtcNow.Ticks,
                    "Institution progress did not restore through city activity records.");
                Check(
                    progress.TryGetValue(traversal.activity.id, out var restoredTraversal) &&
                    restoredTraversal.completionCount == 1 &&
                    restoredTraversal.nextAvailableUtcTicks > DateTime.UtcNow.Ticks,
                    "Traversal progress did not restore through city activity records.");

                var favorsBefore = game.Favors;
                Invoke(game, "CompleteCityActivity", auction.activity);
                Check(game.Favors == favorsBefore, "A cooling institution granted a repeated reward.");
                Check(restored.completionCount == 2, "A cooling institution advanced twice.");
                Invoke(game, "CompleteCityActivity", traversal.activity);
                Check(game.Favors == favorsBefore, "A cooling traversal granted a repeated reward.");
                Check(restoredTraversal.completionCount == 1, "A cooling traversal advanced twice.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void VerifyWorldBuild()
        {
            var gameRoot = new GameObject("City Institution World Verification");
            var alderwakeRoot = new GameObject("Alderwake Institution Verification");
            var veyrwaterRoot = new GameObject("Veyrwater Institution Verification");
            try
            {
                var game = gameRoot.AddComponent<TetherfallGame>();
                var alderwake = CreateBareCity(
                    alderwakeRoot,
                    game,
                    CityCatalog.AlderwakeHearthmarket);
                var veyrwater = CreateBareCity(
                    veyrwaterRoot,
                    game,
                    CityCatalog.VeyrwaterMooring);

                BuildAndVerifyCity(game, alderwake, 430);
                BuildAndVerifyCity(game, veyrwater, 400);

                var arenaActivity = FindTraining(CityCatalog.AlderwakeHearthmarket);
                var arena = CityTrainingArena.Create(game, alderwake, arenaActivity);
                Check(arena != null, "Alderwake arena could not be built.");
                Check(
                    arena.GetComponentsInChildren<Renderer>(true).Length >= 30,
                    "Arena silhouettes are missing stands, braids, or scoring structures.");
                Check(
                    arena.GetComponentsInChildren<Light>(true).Length == 0,
                    "Arena geometry added a realtime light.");
                arena.RestoreRuntimeState();
                Check(!arena.HasActiveDrill, "Arena reset left an active drill.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(alderwakeRoot);
                UnityEngine.Object.DestroyImmediate(veyrwaterRoot);
                UnityEngine.Object.DestroyImmediate(gameRoot);
            }
        }

        private static void BuildAndVerifyCity(
            TetherfallGame game,
            CityDirector city,
            int rendererBudget)
        {
            var palette = CityVisualBuilder.GetPalette(game, city.Definition);
            CityInstitutionBuilder.Build(game, city, palette);
            var course = CityDistrictBuilder.Build(game, city, palette);
            var interactables = city.GetComponentsInChildren<CityInstitutionInteractable>(true);
            var expected = CityInstitutionCatalog.GetForCity(city.Definition.id).Count;
            Check(
                interactables.Length == expected,
                $"{city.Definition.displayName} built {interactables.Length} institution stewards, expected {expected}.");
            Check(
                city.GetComponentsInChildren<Light>(true).Length == 0,
                $"{city.Definition.displayName} institutions or districts added realtime lights.");
            var rendererCount = city.GetComponentsInChildren<Renderer>(true).Length;
            Check(
                rendererCount > expected * 8 && rendererCount <= rendererBudget,
                $"{city.Definition.displayName} city renderer count {rendererCount} is outside budget.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < interactables.Length; index++)
            {
                var interactable = interactables[index];
                Check(
                    interactable != null &&
                    !string.IsNullOrWhiteSpace(interactable.InstitutionId) &&
                    ids.Add(interactable.InstitutionId),
                    $"{city.Definition.displayName} built a duplicate institution interactable.");
                Check(
                    interactable.GetComponent<Collider>() == null ||
                    !interactable.GetComponent<Collider>().enabled,
                    $"{interactable.InstitutionId} steward is blocked by its own collider.");
                var animator = interactable.GetComponentInChildren<HumanProceduralAnimator>(true);
                Check(
                    animator != null &&
                    animator.IsBound &&
                    animator.BoundSocketCount == 15,
                    $"{interactable.InstitutionId} steward is missing its articulated resident rig.");
                Check(
                    animator == null ||
                    animator.BindingRoot == null ||
                    animator.BindingRoot.GetComponentsInChildren<Collider>(true).Length == 0,
                    $"{interactable.InstitutionId} steward art contains gameplay colliders.");
            }

            var expectedStalls = CityDistrictCatalog.GetMarketStallsForCity(
                city.Definition.id).Count;
            var stallIdentities = city.GetComponentsInChildren<CityMarketStallIdentity>(true);
            Check(
                stallIdentities.Length == expectedStalls,
                $"{city.Definition.displayName} built {stallIdentities.Length} market merchants, expected {expectedStalls}.");
            var stallIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < stallIdentities.Length; index++)
            {
                var stall = stallIdentities[index];
                Check(
                    stall != null &&
                    !string.IsNullOrWhiteSpace(stall.StallId) &&
                    stallIds.Add(stall.StallId) &&
                    stall.GetComponent<VendorNpc>() != null,
                    $"{city.Definition.displayName} built an invalid cooperative stall merchant.");
                Check(
                    stall.GetComponent<Collider>() == null ||
                    !stall.GetComponent<Collider>().enabled,
                    $"Market merchant '{stall.StallId}' has a non-gameplay collider.");
            }

            Check(course != null, $"{city.Definition.displayName} built no traversal course.");
            Check(
                course.Definition ==
                CityDistrictCatalog.GetTraversalRouteForCity(city.Definition.id),
                $"{city.Definition.displayName} traversal course lost its stable definition.");
            var routeColliders = course.GetComponentsInChildren<Collider>(true);
            Check(
                routeColliders.Length >= 15 && routeColliders.Length <= 24,
                $"{city.Definition.displayName} traversal collider count {routeColliders.Length} is outside budget.");
            for (var index = 0; index < routeColliders.Length; index++)
            {
                var colliderName = routeColliders[index].gameObject.name;
                Check(
                    colliderName.Contains("traversal landing") ||
                    colliderName.Contains("raised boardwalk") ||
                    colliderName.Contains("copper roof ramp"),
                    $"{city.Definition.displayName} traversal added a non-gameplay collider on '{colliderName}'.");
            }

            SetField(course, "active", true);
            course.RestoreRuntimeState();
            Check(!course.IsActive, $"{city.Definition.displayName} traversal did not reset safely.");

            CityInstitutionBuilder.Build(game, city, palette);
            var repeatedCourse = CityDistrictBuilder.Build(game, city, palette);
            Check(
                city.GetComponentsInChildren<CityInstitutionInteractable>(true).Length == expected,
                $"{city.Definition.displayName} duplicated institutions on a repeated build.");
            Check(
                repeatedCourse == course &&
                city.GetComponentsInChildren<CityTraversalCourse>(true).Length == 1 &&
                city.GetComponentsInChildren<CityMarketStallIdentity>(true).Length == expectedStalls,
                $"{city.Definition.displayName} duplicated district content on a repeated build.");
        }

        private static void VerifyArenaEscalation()
        {
            var calculate = typeof(CityTrainingArena).GetMethod(
                "CalculateWaveCount",
                BindingFlags.Static | BindingFlags.NonPublic);
            Check(calculate != null, "Arena escalation function is unavailable.");
            Check((int)calculate.Invoke(null, new object[] { 0 }) == 1, "First arena run is not one wave.");
            Check((int)calculate.Invoke(null, new object[] { 1 }) == 2, "Second arena run is not two waves.");
            Check((int)calculate.Invoke(null, new object[] { 2 }) == 3, "Third arena run is not three waves.");
            Check((int)calculate.Invoke(null, new object[] { 40 }) == 3, "Arena escalation does not cap safely.");
        }

        private static CityDirector CreateBareCity(
            GameObject root,
            TetherfallGame game,
            CityDefinition definition)
        {
            var city = root.AddComponent<CityDirector>();
            SetField(city, "game", game);
            SetField(city, "definition", definition);
            SetField(city, "palette", CityVisualBuilder.GetPalette(game, definition));
            return city;
        }

        private static CityDefinition GetCity(string cityId)
        {
            if (cityId == CityCatalog.AlderwakeHearthmarket.id)
            {
                return CityCatalog.AlderwakeHearthmarket;
            }

            return cityId == CityCatalog.VeyrwaterMooring.id
                ? CityCatalog.VeyrwaterMooring
                : null;
        }

        private static CityActivityDefinition FindTraining(CityDefinition city)
        {
            for (var index = 0; index < city.activities.Count; index++)
            {
                if (city.activities[index]?.kind == CityActivityKind.Training)
                {
                    return city.activities[index];
                }
            }

            return null;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Check(field != null, $"Field '{fieldName}' was unavailable.");
            field.SetValue(target, value);
        }

        private static void Invoke(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Check(method != null, $"Method '{methodName}' was unavailable.");
            method.Invoke(target, arguments);
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

        private static void Check(bool condition, string message)
        {
            assertionCount++;
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
