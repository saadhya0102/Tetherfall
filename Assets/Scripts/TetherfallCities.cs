using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed class CityActivityProgress
    {
        public int completionCount;
        public long nextAvailableUtcTicks;
    }

    public sealed partial class TetherfallGame
    {
        private readonly Dictionary<string, CityDirector> cities = new Dictionary<string, CityDirector>();
        private readonly Dictionary<string, CityActivityProgress> cityActivityProgress =
            new Dictionary<string, CityActivityProgress>();
        private CityNoticeBoard activeCityBoard;
        private bool cityBoardShowingInstitutions;

        internal void RegisterCity(CityDirector city)
        {
            if (city != null && city.Definition != null && !string.IsNullOrEmpty(city.Definition.id))
            {
                cities[city.Definition.id] = city;
            }
        }

        internal bool HasRegisteredCity(string cityId)
        {
            return !string.IsNullOrEmpty(cityId) &&
                   cities.TryGetValue(cityId, out var city) &&
                   city != null;
        }

        public void OpenCityBoard(CityNoticeBoard board)
        {
            if (board == null || board.City == null)
            {
                return;
            }

            activeCityBoard = board;
            cityBoardShowingInstitutions = false;
            cityBoardShowingBounties = false;
            OpenPanel(GamePanel.CityBoard, $"{board.City.Definition.displayName} civic notices");
        }

        internal void RestAtCityInn(CityResident resident)
        {
            if (resident == null)
            {
                return;
            }

            var restoredVitality = Mathf.Min(
                Mathf.CeilToInt(MaxHealth * 0.45f),
                Mathf.Max(0, MaxHealth - Health));
            Health = Mathf.Min(MaxHealth, Health + restoredVitality);
            RestoreAllStamina();
            Notify(restoredVitality > 0
                ? $"{resident.DisplayName} prepares a warm room. +{restoredVitality} vitality; stamina restored."
                : $"{resident.DisplayName} prepares a warm room. Stamina restored; vitality was already full.");
            RequestSave();
        }

        internal void OpenCityArchive(CityDirector city)
        {
            if (city == null)
            {
                return;
            }

            var activity = city.FindActivity(CityActivityKind.Archive);
            if (activity != null)
            {
                TryStartCityActivity(city, activity);
                return;
            }

            Notify($"{city.Definition?.displayName ?? "This city"} has no archive exchange posted.");
        }

        internal void TryStartCityActivity(CityDirector city, CityActivityDefinition activity)
        {
            if (city == null || activity == null)
            {
                return;
            }

            var catalogActivity = city.FindActivity(activity.id);
            if (catalogActivity == null)
            {
                Notify("That civic activity is no longer listed here.");
                return;
            }

            activity = catalogActivity;
            if (activity.kind == CityActivityKind.Training &&
                TryGetActiveCityTraining(out var activeCity, out var activeTraining))
            {
                Notify(activeCity == city && activeTraining.id == activity.id
                    ? $"{activity.title} is already underway. {city.GetTrainingStatusText()}"
                    : $"Finish {activeTraining.title} at {activeCity.Definition.displayName} before starting another drill.");
                return;
            }

            if (!IsCityActivityAvailable(activity))
            {
                Notify($"{activity.title} returns {GetCityActivityCooldownText(activity)}.");
                return;
            }

            switch (activity.kind)
            {
                case CityActivityKind.Delivery:
                    var deliveryCost = GetValidCityCost(activity.cost);
                    if (!CanAfford(deliveryCost))
                    {
                        Notify($"{activity.title} still needs {DescribeMissingCost(deliveryCost)}.");
                        return;
                    }

                    SpendCost(deliveryCost);
                    CompleteCityActivity(activity);
                    break;

                case CityActivityKind.Training:
                    if (PlayerTransform == null ||
                        Vector3.Distance(PlayerTransform.position, activity.arenaPosition) > 6.5f)
                    {
                        Notify($"Go to the {activity.title} to begin that drill.");
                        return;
                    }

                    if (HasTacticalAreaHostile(activity.arenaPosition, 12f, 12f))
                    {
                        Notify($"Clear the area around {activity.title} before beginning the controlled drill.");
                        return;
                    }

                    city.BeginTraining(activity);
                    break;

                case CityActivityKind.Archive:
                    CompleteCityActivity(activity);
                    break;

                default:
                    Notify($"{activity.title} is not taking participants right now.");
                    break;
            }
        }

        internal void CompleteCityActivity(CityActivityDefinition activity)
        {
            if (activity == null || string.IsNullOrEmpty(activity.id))
            {
                return;
            }

            var catalogActivity = FindCatalogCityActivity(activity.id);
            if (catalogActivity == null)
            {
                return;
            }

            activity = catalogActivity;
            var progress = GetCityActivityProgress(activity.id);
            if (progress.nextAvailableUtcTicks > DateTime.UtcNow.Ticks)
            {
                Notify($"{activity.title} was already settled. It returns {GetCityActivityCooldownText(activity)}.");
                return;
            }

            var firstCompletion = progress.completionCount == 0;
            if (progress.completionCount < int.MaxValue)
            {
                progress.completionCount++;
            }

            progress.nextAvailableUtcTicks = GetNextCityActivityUtcTicks(activity);

            if (activity.favorReward > 0)
            {
                AddFavors(activity.favorReward, activity.title);
            }

            if (activity.experienceReward > 0)
            {
                GainExperience(activity.experienceReward);
            }

            if (firstCompletion)
            {
                if (activity.id.StartsWith("city.alderwake", StringComparison.Ordinal))
                {
                    AdvanceQuest("hearthmarket", 1);
                }
                else if (activity.id.StartsWith("city.veyrwater", StringComparison.Ordinal))
                {
                    AdvanceQuest("veyrwater", 1);
                }
            }

            var cycleText = firstCompletion ? "first completion" : $"completion {progress.completionCount}";
            var deliveredCost = DescribeCityCost(activity.cost);
            var contributionText = activity.kind == CityActivityKind.Delivery &&
                                   !string.IsNullOrEmpty(deliveredCost)
                ? $" Contributed {deliveredCost}."
                : string.Empty;
            Notify(
                $"{activity.title}: {cycleText}.{contributionText} +{Mathf.Max(0, activity.favorReward)} Favors, " +
                $"+{Mathf.Max(0, activity.experienceReward)} XP. Returns {GetCityActivityCooldownText(activity)}.");
            RequestSave();
        }

        internal bool IsCityActivityActive(CityDirector city, CityActivityDefinition activity)
        {
            return city != null && activity != null && city.IsTrainingActive(activity);
        }

        internal string GetActiveCityActivityObjectiveText()
        {
            if (TryGetActiveCityTraining(out var city, out var activity))
            {
                return $"Complete {activity.title} at {city.Definition.displayName}. {city.GetTrainingStatusText()}";
            }

            return string.Empty;
        }

        internal Transform GetActiveCityActivityTarget()
        {
            return TryGetActiveCityTraining(out var city, out _)
                ? city.TrainingArenaTransform
                : null;
        }

        private bool TryGetActiveCityTraining(
            out CityDirector activeCity,
            out CityActivityDefinition activeActivity)
        {
            foreach (var city in cities.Values)
            {
                if (city != null && city.TryGetActiveTraining(out activeActivity))
                {
                    activeCity = city;
                    return true;
                }
            }

            activeCity = null;
            activeActivity = null;
            return false;
        }

        internal bool IsCityActivityAvailable(CityActivityDefinition activity)
        {
            var catalogActivity = activity != null
                ? FindCatalogCityActivity(activity.id)
                : null;
            return catalogActivity != null &&
                   GetCityActivityProgress(catalogActivity.id).nextAvailableUtcTicks <= DateTime.UtcNow.Ticks;
        }

        internal string GetCityActivityCooldownText(CityActivityDefinition activity)
        {
            var catalogActivity = activity != null
                ? FindCatalogCityActivity(activity.id)
                : null;
            if (catalogActivity == null)
            {
                return "later";
            }

            var ticks = GetCityActivityProgress(catalogActivity.id).nextAvailableUtcTicks;
            if (ticks <= DateTime.UtcNow.Ticks)
            {
                return "now";
            }

            var remaining = new TimeSpan(ticks - DateTime.UtcNow.Ticks);
            return remaining.TotalMinutes >= 1d
                ? $"in {Mathf.CeilToInt((float)remaining.TotalMinutes)} min"
                : $"in {Mathf.Max(1, Mathf.CeilToInt((float)remaining.TotalSeconds))} sec";
        }

        internal int GetCityActivityCompletionCount(CityActivityDefinition activity)
        {
            var catalogActivity = activity != null
                ? FindCatalogCityActivity(activity.id)
                : null;
            return catalogActivity == null
                ? 0
                : Mathf.Max(0, GetCityActivityProgress(catalogActivity.id).completionCount);
        }

        private CityActivityProgress GetCityActivityProgress(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new CityActivityProgress();
            }

            if (!cityActivityProgress.TryGetValue(id, out var progress))
            {
                progress = new CityActivityProgress();
                cityActivityProgress[id] = progress;
            }

            return progress;
        }

        private static CityActivityDefinition FindCatalogCityActivity(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            var activity = FindCityActivity(CityCatalog.AlderwakeHearthmarket, id);
            activity = activity ?? FindCityActivity(CityCatalog.VeyrwaterMooring, id);
            activity = activity ?? CityInstitutionCatalog.FindActivity(id);
            return activity ?? CityDistrictCatalog.FindActivity(id);
        }

        private static CityActivityDefinition FindCityActivity(CityDefinition city, string id)
        {
            if (city?.activities == null)
            {
                return null;
            }

            foreach (var activity in city.activities)
            {
                if (activity != null && string.Equals(activity.id, id, StringComparison.Ordinal))
                {
                    return activity;
                }
            }

            return null;
        }

        private static long GetNextCityActivityUtcTicks(CityActivityDefinition activity)
        {
            var cooldownSeconds = activity != null
                ? Mathf.Clamp(activity.cooldownSeconds, 0f, 86400f)
                : 0f;
            return DateTime.UtcNow.AddSeconds(cooldownSeconds).Ticks;
        }

        private static string DescribeCityActivityRequirement(CityActivityDefinition activity)
        {
            if (activity == null)
            {
                return string.Empty;
            }

            switch (activity.kind)
            {
                case CityActivityKind.Delivery:
                    var costText = DescribeCityCost(activity.cost);
                    return !string.IsNullOrEmpty(costText)
                        ? $"Needs {costText}"
                        : "No materials needed";

                case CityActivityKind.Training:
                    return "1-3 controlled waves · bonus per added wave";

                case CityActivityKind.Archive:
                    return "Share one field observation";

                default:
                    return "Speak with the city steward";
            }
        }

        private static string DescribeCityCost(IEnumerable<ResourceCost> costs)
        {
            var parts = new List<string>();
            if (costs != null)
            {
                foreach (var cost in costs)
                {
                    if (cost != null && cost.amount > 0)
                    {
                        parts.Add($"{cost.amount} {cost.kind}");
                    }
                }
            }

            return string.Join(", ", parts);
        }

        private static List<ResourceCost> GetValidCityCost(IEnumerable<ResourceCost> costs)
        {
            var validCosts = new List<ResourceCost>();
            if (costs != null)
            {
                foreach (var cost in costs)
                {
                    if (cost != null && cost.amount > 0)
                    {
                        validCosts.Add(cost);
                    }
                }
            }

            return validCosts;
        }

        private void DrawCityBoard(Rect rect)
        {
            if (activeCityBoard == null || activeCityBoard.City == null)
            {
                GUI.Label(
                    new Rect(rect.x + 26f, rect.y + 76f, rect.width - 52f, 28f),
                    "No civic notices are available.",
                    mutedStyle);
                return;
            }

            var city = activeCityBoard.City;
            var contentWidth = rect.width - 52f;
            var tabGap = 10f;
            var tabWidth = (contentWidth - tabGap * 2f) / 3f;
            GUI.Label(
                new Rect(rect.x + 26f, rect.y + 70f, contentWidth, 24f),
                city.Definition.subtitle,
                bodyStyle);
            if (GUI.Button(new Rect(rect.x + 26f, rect.y + 101f, tabWidth, 30f), "Civic work"))
            {
                cityBoardShowingInstitutions = false;
                SetCityBoardBountyView(false);
            }

            if (GUI.Button(
                    new Rect(rect.x + 26f + tabWidth + tabGap, rect.y + 101f, tabWidth, 30f),
                    "Institutions"))
            {
                cityBoardShowingInstitutions = true;
                SetCityBoardBountyView(false);
            }

            if (GUI.Button(
                    new Rect(rect.x + 26f + (tabWidth + tabGap) * 2f, rect.y + 101f, tabWidth, 30f),
                    "Elite bounties"))
            {
                cityBoardShowingInstitutions = false;
                SetCityBoardBountyView(true);
            }

            if (IsCityBoardShowingBounties)
            {
                DrawCityBounties(rect, city);
                return;
            }

            if (cityBoardShowingInstitutions)
            {
                DrawCityInstitutionStatus(rect, city);
                return;
            }

            GUI.Label(
                new Rect(rect.x + 26f, rect.y + 142f, contentWidth, 22f),
                "CIVIC ACTIVITIES",
                headingStyle);

            var y = rect.y + 174f;
            if (city.Definition.activities == null || city.Definition.activities.Count == 0)
            {
                GUI.Label(
                    new Rect(rect.x + 26f, y, contentWidth, 28f),
                    "No civic activities are posted today.",
                    mutedStyle);
                return;
            }

            foreach (var activity in city.Definition.activities)
            {
                if (activity == null)
                {
                    continue;
                }

                var active = IsCityActivityActive(city, activity);
                var available = !active && IsCityActivityAvailable(activity);
                var completionCount = GetCityActivityCompletionCount(activity);
                var buttonText = active
                    ? "In progress"
                    : activity.kind == CityActivityKind.Delivery
                        ? "Contribute"
                        : activity.kind == CityActivityKind.Training
                            ? "Locate ring"
                            : "File record";
                var previousEnabled = GUI.enabled;
                GUI.enabled = available;
                if (GUI.Button(new Rect(rect.x + 26f, y, 128f, 34f), buttonText))
                {
                    TryStartCityActivity(city, activity);
                }

                GUI.enabled = previousEnabled;
                var availability = active
                    ? city.GetTrainingStatusText()
                    : available
                        ? "Ready"
                        : $"Returns {GetCityActivityCooldownText(activity)}";
                var history = completionCount > 0
                    ? $"{completionCount} completed"
                    : "First completion";
                GUI.Label(
                    new Rect(rect.x + 170f, y - 2f, Mathf.Max(220f, contentWidth - 144f), 64f),
                    $"{activity.title}  ·  {availability}  ·  {history}\n" +
                    $"{activity.description}\n" +
                    $"{DescribeCityActivityRequirement(activity)}  ·  " +
                    $"+{Mathf.Max(0, activity.favorReward)} Favors  ·  +{Mathf.Max(0, activity.experienceReward)} XP",
                    active || available ? bodyStyle : mutedStyle);
                y += 80f;
            }

            GUI.Label(
                new Rect(rect.x + 26f, rect.y + rect.height - 106f, contentWidth, 44f),
                "A city becomes a route in its own right: work its notices, visit its people, and return as its needs change.",
                mutedStyle);
        }

        private void DrawCityInstitutionStatus(Rect rect, CityDirector city)
        {
            var institutions = CityInstitutionCatalog.GetForCity(city?.Definition?.id);
            var contentWidth = rect.width - 52f;
            GUI.Label(
                new Rect(rect.x + 26f, rect.y + 142f, contentWidth, 24f),
                "INSTITUTION LEDGER",
                headingStyle);
            if (institutions == null || institutions.Count == 0)
            {
                GUI.Label(
                    new Rect(rect.x + 26f, rect.y + 178f, contentWidth, 28f),
                    "No staffed institutions are listed.",
                    mutedStyle);
                return;
            }

            const float columnGap = 22f;
            var columnWidth = (contentWidth - columnGap) * 0.5f;
            var rowsPerColumn = Mathf.Max(1, Mathf.CeilToInt(institutions.Count * 0.5f));
            var availableHeight = Mathf.Max(220f, rect.height - 248f);
            var rowHeight = Mathf.Clamp(availableHeight / rowsPerColumn, 62f, 82f);
            for (var index = 0; index < institutions.Count; index++)
            {
                var institution = institutions[index];
                if (institution == null)
                {
                    continue;
                }

                var column = index / rowsPerColumn;
                var row = index % rowsPerColumn;
                var x = rect.x + 26f + column * (columnWidth + columnGap);
                var y = rect.y + 176f + row * rowHeight;
                var ready = institution.activity != null &&
                            IsCityActivityAvailable(institution.activity);
                GUI.Label(
                    new Rect(x, y, columnWidth, rowHeight - 4f),
                    $"{institution.displayName}  ·  {GetCityInstitutionBoardStatus(institution)}\n" +
                    $"{institution.activity?.title ?? "No work posted."}  ·  {institution.locationHint}",
                    ready ? bodyStyle : mutedStyle);
            }

            GUI.Label(
                new Rect(rect.x + 26f, rect.y + rect.height - 55f, contentWidth, 24f),
                "Walk to a staffed counter to trade, take work, tune gear, or report.",
                mutedStyle);
        }

        private void WriteCitySave(TetherfallSaveData data)
        {
            if (data == null)
            {
                return;
            }

            if (data.cityActivities == null)
            {
                data.cityActivities = new List<CityActivitySaveData>();
            }
            else
            {
                data.cityActivities.Clear();
            }

            WriteCityActivitySaveData(data, CityCatalog.AlderwakeHearthmarket);
            WriteCityActivitySaveData(data, CityCatalog.VeyrwaterMooring);
            var institutions = CityInstitutionCatalog.All;
            for (var index = 0; index < institutions.Count; index++)
            {
                WriteCityActivitySaveData(data, institutions[index]?.activity);
            }

            var traversalRoutes = CityDistrictCatalog.TraversalRoutes;
            for (var index = 0; index < traversalRoutes.Count; index++)
            {
                WriteCityActivitySaveData(data, traversalRoutes[index]?.activity);
            }
        }

        private void WriteCityActivitySaveData(TetherfallSaveData data, CityDefinition city)
        {
            if (city?.activities == null)
            {
                return;
            }

            foreach (var activity in city.activities)
            {
                WriteCityActivitySaveData(data, activity);
            }
        }

        private void WriteCityActivitySaveData(
            TetherfallSaveData data,
            CityActivityDefinition activity)
        {
            if (data?.cityActivities == null ||
                activity == null ||
                string.IsNullOrEmpty(activity.id) ||
                !cityActivityProgress.TryGetValue(activity.id, out var progress) ||
                progress == null ||
                (progress.completionCount <= 0 &&
                 progress.nextAvailableUtcTicks <= DateTime.UtcNow.Ticks))
            {
                return;
            }

            foreach (var savedActivity in data.cityActivities)
            {
                if (savedActivity != null &&
                    string.Equals(savedActivity.id, activity.id, StringComparison.Ordinal))
                {
                    return;
                }
            }

            data.cityActivities.Add(new CityActivitySaveData
            {
                id = activity.id,
                completionCount = Mathf.Max(0, progress.completionCount),
                nextAvailableUtcTicks = Math.Max(0L, progress.nextAvailableUtcTicks)
            });
        }

        private void RestoreCityState(TetherfallSaveData data)
        {
            cityActivityProgress.Clear();
            activeCityBoard = null;
            cityBoardShowingInstitutions = false;
            cityBoardShowingBounties = false;
            if (data?.cityActivities != null)
            {
                var nowTicks = DateTime.UtcNow.Ticks;
                foreach (var savedActivity in data.cityActivities)
                {
                    if (savedActivity == null || string.IsNullOrEmpty(savedActivity.id))
                    {
                        continue;
                    }

                    var activity = FindCatalogCityActivity(savedActivity.id);
                    if (activity == null)
                    {
                        continue;
                    }

                    var restoredTicks = Math.Max(0L, savedActivity.nextAvailableUtcTicks);
                    restoredTicks = restoredTicks <= nowTicks
                        ? 0L
                        : Math.Min(restoredTicks, GetNextCityActivityUtcTicks(activity));
                    if (!cityActivityProgress.TryGetValue(activity.id, out var progress))
                    {
                        progress = new CityActivityProgress();
                        cityActivityProgress[activity.id] = progress;
                    }

                    progress.completionCount = Mathf.Max(
                        progress.completionCount,
                        Mathf.Max(0, savedActivity.completionCount));
                    progress.nextAvailableUtcTicks = Math.Max(
                        progress.nextAvailableUtcTicks,
                        restoredTicks);
                }
            }

            foreach (var city in cities.Values)
            {
                city?.RestoreRuntimeState();
            }
        }
    }

    public static class CityBuilder
    {
        public static void Build(TetherfallGame game)
        {
            if (game == null)
            {
                return;
            }

            BuildCity(game, CityCatalog.AlderwakeHearthmarket);
            BuildCity(game, CityCatalog.VeyrwaterMooring);
        }

        private static void BuildCity(TetherfallGame game, CityDefinition definition)
        {
            if (game == null ||
                definition == null ||
                string.IsNullOrEmpty(definition.id) ||
                game.HasRegisteredCity(definition.id))
            {
                return;
            }

            var root = new GameObject($"{definition.displayName} Director");
            root.transform.SetParent(game.transform, false);
            var city = root.AddComponent<CityDirector>();
            city.Initialize(game, definition);
        }
    }

    public sealed class CityDirector : MonoBehaviour
    {
        private readonly List<CityResident> residents = new List<CityResident>();
        private readonly List<Light> cityLights = new List<Light>();
        private TetherfallGame game;
        private CityDefinition definition;
        private CityTrainingArena trainingArena;
        private CityTraversalCourse traversalCourse;
        private CityNoticeBoard noticeBoard;
        private CityLifeLandmark lifeLandmark;
        private CityVisualBuilder.CityArtPalette palette;

        public CityDefinition Definition => definition;
        public CityNoticeBoard NoticeBoard => noticeBoard;

        public void Initialize(TetherfallGame owner, CityDefinition cityDefinition)
        {
            if (owner == null || cityDefinition == null)
            {
                enabled = false;
                return;
            }

            game = owner;
            definition = cityDefinition;
            BuildCity();
            game.RegisterCity(this);
        }

        public CityActivityDefinition FindActivity(CityActivityKind kind)
        {
            if (definition?.activities == null)
            {
                return null;
            }

            foreach (var activity in definition.activities)
            {
                if (activity != null && activity.kind == kind)
                {
                    return activity;
                }
            }

            return null;
        }

        public CityActivityDefinition FindActivity(string activityId)
        {
            if (definition?.activities == null || string.IsNullOrEmpty(activityId))
            {
                return null;
            }

            foreach (var activity in definition.activities)
            {
                if (activity != null &&
                    string.Equals(activity.id, activityId, StringComparison.Ordinal))
                {
                    return activity;
                }
            }

            var institutionActivity = CityInstitutionCatalog.FindActivityForCity(
                definition.id,
                activityId);
            if (institutionActivity != null)
            {
                return institutionActivity;
            }

            var traversalActivity = CityDistrictCatalog.FindActivity(activityId);
            var traversal = CityDistrictCatalog.GetTraversalRouteForCity(definition.id);
            return traversal != null && traversal.activity == traversalActivity
                ? traversalActivity
                : null;
        }

        public bool BeginTraining(CityActivityDefinition activity)
        {
            if (trainingArena != null && trainingArena.Begin(activity))
            {
                return true;
            }

            game.Notify(
                trainingArena == null
                    ? $"{definition.displayName} has no staffed training ring."
                    : $"The {definition.displayName} training ring cannot begin that drill. {GetTrainingStatusText()}");
            return false;
        }

        public bool IsTrainingActive(CityActivityDefinition activity)
        {
            return trainingArena != null && trainingArena.IsActive(activity);
        }

        public bool TryGetActiveTraining(out CityActivityDefinition activity)
        {
            if (trainingArena != null && trainingArena.TryGetActiveActivity(out activity))
            {
                return true;
            }

            activity = null;
            return false;
        }

        public Transform TrainingArenaTransform => trainingArena != null ? trainingArena.transform : null;

        public string GetTrainingStatusText()
        {
            return trainingArena != null && trainingArena.HasActiveDrill
                ? trainingArena.StatusText
                : "Drill ready";
        }

        public void OnTrainingCompleted(
            CityActivityDefinition activity,
            int completedWaveCount)
        {
            if (game != null &&
                activity != null &&
                game.IsCityActivityAvailable(activity))
            {
                var bonusWaves = Mathf.Clamp(completedWaveCount - 1, 0, 2);
                if (bonusWaves > 0)
                {
                    game.AddFavors(bonusWaves * 6, $"{activity.title} escalation");
                    game.GainExperience(bonusWaves * 8);
                }

                game.CompleteCityActivity(activity);
            }
        }

        public void RestoreRuntimeState()
        {
            trainingArena?.RestoreRuntimeState();
            traversalCourse?.RestoreRuntimeState();
            lifeLandmark?.RestoreRuntimeState();
            foreach (var resident in residents)
            {
                resident?.RestoreRuntimeState();
            }
        }

        private void BuildCity()
        {
            palette = CityVisualBuilder.GetPalette(game, definition);
            if (definition.id == CityCatalog.VeyrwaterMooring.id)
            {
                BuildVeyrwaterMooring();
            }
            else
            {
                BuildHearthmarket();
            }

            CityInstitutionBuilder.Build(game, this, palette);
            traversalCourse = CityDistrictBuilder.Build(game, this, palette);
            if (cityLights.Count > 0)
            {
                gameObject.AddComponent<CityLightCluster>().Initialize(
                    game,
                    definition.center,
                    cityLights);
            }

            noticeBoard = CityNoticeBoard.Create(game, this, definition.center + new Vector3(0f, 0.95f, -8f));
            game.RegisterInteractable(noticeBoard);

            if (definition.residents != null)
            {
                foreach (var residentDefinition in definition.residents)
                {
                    if (residentDefinition == null)
                    {
                        continue;
                    }

                    var resident = CityResident.Create(game, this, residentDefinition);
                    if (resident != null)
                    {
                        residents.Add(resident);
                        game.RegisterInteractable(resident);
                    }
                }
            }

            var trainingActivity = FindActivity(CityActivityKind.Training);
            if (trainingActivity != null)
            {
                trainingArena = CityTrainingArena.Create(game, this, trainingActivity);
                game.RegisterInteractable(trainingArena);
            }

            WorldVisuals.AddLabel(
                transform,
                definition.displayName,
                new Vector3(definition.center.x, 7.5f, definition.center.z),
                definition.accentColor,
                0.15f);
        }

        private void BuildHearthmarket()
        {
            CreateRoad(definition.center + new Vector3(0f, 0.02f, -20f), new Vector3(10f, 0.16f, 62f));
            CreateRoad(definition.center + new Vector3(0f, 0.03f, 1f), new Vector3(58f, 0.17f, 10f));
            CreatePlaza(definition.center);
            CreateMarketBuildings();
            CreateMarketStalls();
            CreateGardenAndRiverwork();
            DressHearthmarketTerraces();
            CreateHearthmarketGateway();
            CreateMarketLighting();
            CreateHearthmarketForgePlaza();
            VendorNpc.Create(
                game,
                "vendor.alderwake.hearthmarket",
                "Kite Rowe",
                "commons merchant",
                new Vector3(8f, 0.9f, 68f),
                "A good route begins with dry cord, a full stomach, and one thing you did not know you needed.");
            CraftingStation.Create(game, "Hearthmarket copperwork bench", new Vector3(14f, 0f, 88f));
        }

        private void BuildVeyrwaterMooring()
        {
            CreateRoad(definition.center + new Vector3(0f, 0.02f, -18f), new Vector3(9f, 0.16f, 57f));
            CreateRoad(definition.center + new Vector3(0f, 0.03f, 1f), new Vector3(54f, 0.17f, 9f));
            CreatePlaza(definition.center);
            CreateHouse(new Vector3(187f, 0f, 66f), new Color(0.22f, 0.38f, 0.4f), new Color(0.24f, 0.51f, 0.56f), "Mire's Ferryhouse");
            CreateHouse(new Vector3(187f, 0f, 87f), new Color(0.19f, 0.32f, 0.37f), new Color(0.26f, 0.42f, 0.52f), "Flood archive");
            CreateHouse(new Vector3(220f, 0f, 66f), new Color(0.3f, 0.29f, 0.19f), new Color(0.29f, 0.38f, 0.57f), "Floodwatch house");
            CreateHouse(new Vector3(222f, 0f, 89f), new Color(0.25f, 0.44f, 0.35f), new Color(0.32f, 0.58f, 0.52f), "Reedwright lodge");

            var stalls = new[]
            {
                new Vector3(194f, 0f, 74f),
                new Vector3(201f, 0f, 78f),
                new Vector3(210f, 0f, 77f),
                new Vector3(215f, 0f, 70f)
            };
            for (var index = 0; index < stalls.Length; index++)
            {
                CreateStall(
                    stalls[index],
                    Color.Lerp(definition.accentColor, new Color(0.5f, 0.78f, 0.46f), index / 4f),
                    index % 2 == 0 ? "fen supplies" : "ferry wares");
            }

            CreateVeyrwaterCanal();
            DressVeyrwaterMooringEdges();
            WorldArt.SpawnKenney(game, "bridge_wood", new Vector3(203f, 0.4f, 98f), new Vector3(1.45f, 1.1f, 1.45f), new Color(0.29f, 0.2f, 0.15f), true);
            WorldArt.SpawnKenney(game, "tent_detailedOpen", new Vector3(198f, 0f, 90f), Vector3.one * 1.15f, new Color(0.21f, 0.46f, 0.48f), true);
            CreateVeyrwaterChannelFrame();
            CreateBanner(new Vector3(197f, 0f, 58f), definition.accentColor);
            CreateBanner(new Vector3(209f, 0f, 58f), new Color(0.48f, 0.83f, 0.68f));
            CreateBanner(new Vector3(183f, 0f, 73f), new Color(0.36f, 0.65f, 0.8f));
            CreateBanner(new Vector3(225f, 0f, 73f), new Color(0.72f, 0.49f, 0.24f));
            CreateMooringLighting();
            CreateVeyrwaterPortLife();
            VendorNpc.Create(
                game,
                "vendor.veyrwater.ferrier",
                "Luma Sedge",
                "ferry stores",
                new Vector3(198f, 0.9f, 68f),
                "If it cannot survive a wet crossing, it is not a thing you own. It is a thing you are carrying for the fen.");
            CraftingStation.Create(game, "Veyrwater reedwork bench", new Vector3(213f, 0f, 89f));
        }

        private void CreateHearthmarketForgePlaza()
        {
            var root = new GameObject("Hearthmarket expedition forge plaza");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(30f, 0f, 96f);

            for (var side = -1; side <= 1; side += 2)
            {
                for (var depth = -1; depth <= 1; depth += 2)
                {
                    CreateCityVisualPrimitive(
                        PrimitiveType.Cylinder,
                        "Hearthmarket forge pavilion post",
                        root.transform,
                        new Vector3(side * 3f, 2f, depth * 2.2f),
                        new Vector3(0.13f, 2f, 0.13f),
                        palette.timber,
                        false);
                }

                var roof = CreateCityVisualPrimitive(
                    PrimitiveType.Cube,
                    "Hearthmarket layered forge awning",
                    root.transform,
                    new Vector3(side * 1.65f, 4.25f, 0f),
                    new Vector3(3.45f, 0.2f, 4.9f),
                    palette.canopy,
                    false);
                roof.transform.localRotation = Quaternion.Euler(0f, 0f, side * 7f);
            }

            CreateCityVisualPrimitive(
                PrimitiveType.Cube,
                "Hearthmarket forge body",
                root.transform,
                new Vector3(0f, 0.55f, 0.3f),
                new Vector3(2.6f, 1.1f, 1.8f),
                palette.stone,
                true);
            CreateCityVisualPrimitive(
                PrimitiveType.Cylinder,
                "Hearthmarket forge basin",
                root.transform,
                new Vector3(0f, 1.25f, 0.3f),
                new Vector3(1.05f, 0.16f, 0.75f),
                palette.copper,
                false);
            var forgeGlow = CreateCityVisualPrimitive(
                PrimitiveType.Sphere,
                "Hearthmarket banked forge glow",
                root.transform,
                new Vector3(0f, 1.5f, 0.3f),
                Vector3.one * 0.48f,
                palette.marker,
                false);
            CreateCityVisualPrimitive(
                PrimitiveType.Cylinder,
                "Hearthmarket forge flue",
                root.transform,
                new Vector3(2.15f, 1.25f, 1.3f),
                new Vector3(0.25f, 1.25f, 0.25f),
                palette.copper,
                false);

            var wheelRoot = new GameObject("Hearthmarket forge flywheel").transform;
            wheelRoot.SetParent(root.transform, false);
            wheelRoot.localPosition = new Vector3(-2.7f, 1.85f, -0.45f);
            var wheelDisk = CreateCityVisualPrimitive(
                PrimitiveType.Cylinder,
                "Hearthmarket flywheel hub",
                wheelRoot,
                Vector3.zero,
                new Vector3(0.86f, 0.12f, 0.86f),
                palette.copper,
                false);
            wheelDisk.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            for (var spoke = 0; spoke < 4; spoke++)
            {
                var spokeVisual = CreateCityVisualPrimitive(
                    PrimitiveType.Cube,
                    "Hearthmarket flywheel spoke",
                    wheelRoot,
                    Vector3.zero,
                    new Vector3(0.08f, 1.65f, 0.08f),
                    palette.timber,
                    false);
                spokeVisual.transform.localRotation = Quaternion.Euler(spoke * 45f, 0f, 0f);
            }

            var hammerRoot = new GameObject("Hearthmarket trip hammer").transform;
            hammerRoot.SetParent(root.transform, false);
            hammerRoot.localPosition = new Vector3(1.55f, 1.75f, -0.5f);
            CreateCityVisualPrimitive(
                PrimitiveType.Cube,
                "Hearthmarket trip hammer handle",
                hammerRoot,
                Vector3.zero,
                new Vector3(0.18f, 1.35f, 0.18f),
                palette.timber,
                false);
            CreateCityVisualPrimitive(
                PrimitiveType.Cube,
                "Hearthmarket trip hammer head",
                hammerRoot,
                new Vector3(0f, 0.78f, 0f),
                new Vector3(1f, 0.36f, 0.48f),
                palette.stone,
                false);

            var stateRenderers = new Renderer[3];
            var cratePositions = new[]
            {
                new Vector3(-4f, 0.42f, 1.8f),
                new Vector3(-4.9f, 0.42f, 0.55f),
                new Vector3(-3.95f, 1.2f, 1.55f),
                new Vector3(-4.85f, 1.2f, 0.3f)
            };
            for (var index = 0; index < cratePositions.Length; index++)
            {
                var crate = CreateCityVisualPrimitive(
                    PrimitiveType.Cube,
                    "Hearthmarket expedition supply crate",
                    root.transform,
                    cratePositions[index],
                    new Vector3(0.84f, 0.78f, 0.78f),
                    index % 2 == 0 ? palette.timber : palette.copper,
                    false);
                if (index > 0)
                {
                    stateRenderers[index - 1] = crate.GetComponent<Renderer>();
                }
            }

            var mastRoot = new GameObject("Hearthmarket route mast").transform;
            mastRoot.SetParent(root.transform, false);
            mastRoot.localPosition = new Vector3(3.8f, 0f, 0.2f);
            CreateCityVisualPrimitive(
                PrimitiveType.Cylinder,
                "Hearthmarket route mast pole",
                mastRoot,
                new Vector3(0f, 2f, 0f),
                new Vector3(0.1f, 2f, 0.1f),
                palette.copper,
                false);
            CreateCityVisualPrimitive(
                PrimitiveType.Cube,
                "Hearthmarket route mast arm",
                mastRoot,
                new Vector3(0.62f, 3.25f, 0f),
                new Vector3(1.25f, 0.1f, 0.1f),
                palette.timber,
                false);
            CreateCityVisualPrimitive(
                PrimitiveType.Cube,
                "Hearthmarket route mast crossarm",
                mastRoot,
                new Vector3(0f, 2.75f, 0.52f),
                new Vector3(0.1f, 0.1f, 1.05f),
                palette.timber,
                false);
            var mastMarker = CreateCityVisualPrimitive(
                PrimitiveType.Sphere,
                "Hearthmarket route mast beacon",
                mastRoot,
                new Vector3(0f, 4.15f, 0f),
                Vector3.one * 0.32f,
                palette.marker,
                false);

            lifeLandmark = root.AddComponent<CityLifeLandmark>();
            lifeLandmark.Initialize(
                game,
                this,
                FindActivity(CityActivityKind.Delivery),
                "Work the expedition forge bellows",
                "The forge cadence rolls through the market.",
                68f,
                Vector3.right,
                new[] { wheelRoot },
                new[] { hammerRoot },
                new[] { forgeGlow.transform, mastMarker.transform },
                stateRenderers);
            game.RegisterInteractable(lifeLandmark);
        }

        private void CreateVeyrwaterPortLife()
        {
            var root = new GameObject("Veyrwater living canal works");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(227f, 0f, 97f);

            var firstSkiff = CreateMooredSkiff(
                root.transform,
                new Vector3(-36f, 0.48f, -1f),
                -8f,
                "west ferry skiff");
            var secondSkiff = CreateMooredSkiff(
                root.transform,
                new Vector3(-12f, 0.5f, 4f),
                174f,
                "reedwright cargo skiff");

            for (var bank = -1; bank <= 1; bank += 2)
            {
                var leaf = CreateCityVisualPrimitive(
                    PrimitiveType.Cube,
                    "Veyrwater raised canal bridge leaf",
                    root.transform,
                    new Vector3(-39f, 0.82f, bank < 0 ? -3.8f : 5.8f),
                    new Vector3(3.2f, 0.22f, 4.6f),
                    palette.timber,
                    false);
                leaf.transform.localRotation = Quaternion.Euler(bank * 26f, 0f, 0f);
                var hinge = CreateCityVisualPrimitive(
                    PrimitiveType.Cylinder,
                    "Veyrwater lift bridge hinge",
                    root.transform,
                    new Vector3(-39f, 0.68f, bank < 0 ? -6.05f : 8.05f),
                    new Vector3(0.22f, 1.8f, 0.22f),
                    palette.copper,
                    false);
                hinge.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            CreateCityVisualPrimitive(
                PrimitiveType.Cube,
                "Veyrwater channel winch base",
                root.transform,
                new Vector3(0f, 0.5f, 0f),
                new Vector3(2.8f, 1f, 2.4f),
                palette.stone,
                true);
            for (var side = -1; side <= 1; side += 2)
            {
                CreateCityVisualPrimitive(
                    PrimitiveType.Cylinder,
                    "Veyrwater channel winch support",
                    root.transform,
                    new Vector3(side * 0.82f, 1.55f, 0f),
                    new Vector3(0.13f, 1.15f, 0.13f),
                    palette.timber,
                    false);
            }

            var wheelRoot = new GameObject("Veyrwater channel signal wheel").transform;
            wheelRoot.SetParent(root.transform, false);
            wheelRoot.localPosition = new Vector3(0f, 2.45f, 0f);
            var wheelDisk = CreateCityVisualPrimitive(
                PrimitiveType.Cylinder,
                "Veyrwater signal wheel hub",
                wheelRoot,
                Vector3.zero,
                new Vector3(1.08f, 0.14f, 1.08f),
                palette.copper,
                false);
            wheelDisk.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (var spoke = 0; spoke < 3; spoke++)
            {
                var spokeVisual = CreateCityVisualPrimitive(
                    PrimitiveType.Cube,
                    "Veyrwater signal wheel spoke",
                    wheelRoot,
                    Vector3.zero,
                    new Vector3(1.85f, 0.08f, 0.08f),
                    palette.timber,
                    false);
                spokeVisual.transform.localRotation = Quaternion.Euler(0f, 0f, spoke * 60f);
            }

            var crankMarker = CreateCityVisualPrimitive(
                PrimitiveType.Sphere,
                "Veyrwater signal wheel marker",
                wheelRoot,
                new Vector3(0f, 1.25f, 0f),
                Vector3.one * 0.28f,
                palette.marker,
                false);

            var markerTransforms = new Transform[4];
            var stateRenderers = new Renderer[3];
            var lanternPositions = new[]
            {
                new Vector3(-42f, 0f, -7f),
                new Vector3(-6f, 0f, -7f),
                new Vector3(-42f, 0f, 9f),
                new Vector3(-6f, 0f, 9f)
            };
            for (var index = 0; index < lanternPositions.Length; index++)
            {
                var lanternPosition = lanternPositions[index];
                CreateCityVisualPrimitive(
                    PrimitiveType.Cylinder,
                    "Veyrwater route lantern pole",
                    root.transform,
                    lanternPosition + Vector3.up * 2f,
                    new Vector3(0.09f, 2f, 0.09f),
                    palette.timber,
                    false);
                var marker = CreateCityVisualPrimitive(
                    PrimitiveType.Sphere,
                    "Veyrwater route lantern marker",
                    root.transform,
                    lanternPosition + Vector3.up * 4.15f,
                    Vector3.one * 0.31f,
                    palette.marker,
                    false);
                markerTransforms[index] = marker.transform;
                if (index > 0)
                {
                    stateRenderers[index - 1] = marker.GetComponent<Renderer>();
                }
            }

            lifeLandmark = root.AddComponent<CityLifeLandmark>();
            lifeLandmark.Initialize(
                game,
                this,
                FindActivity(CityActivityKind.Delivery),
                "Align the channel signal winch",
                "The lantern code travels from mooring to mooring.",
                52f,
                Vector3.forward,
                new[] { wheelRoot },
                new[] { firstSkiff, secondSkiff },
                new[]
                {
                    crankMarker.transform,
                    markerTransforms[0],
                    markerTransforms[1],
                    markerTransforms[2],
                    markerTransforms[3]
                },
                stateRenderers);
            game.RegisterInteractable(lifeLandmark);
        }

        private Transform CreateMooredSkiff(
            Transform parent,
            Vector3 localPosition,
            float yaw,
            string label)
        {
            var skiff = new GameObject(label).transform;
            skiff.SetParent(parent, false);
            skiff.localPosition = localPosition;
            skiff.localRotation = Quaternion.Euler(0f, yaw, 0f);
            CreateCityVisualPrimitive(
                PrimitiveType.Cube,
                $"{label} hull",
                skiff,
                Vector3.zero,
                new Vector3(4.1f, 0.42f, 1.45f),
                palette.timber,
                false);
            var prow = CreateCityVisualPrimitive(
                PrimitiveType.Cube,
                $"{label} raised prow",
                skiff,
                new Vector3(2.15f, 0.18f, 0f),
                new Vector3(1.2f, 0.32f, 1.2f),
                palette.copper,
                false);
            prow.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            CreateCityVisualPrimitive(
                PrimitiveType.Cylinder,
                $"{label} mast",
                skiff,
                new Vector3(0f, 1.35f, 0f),
                new Vector3(0.08f, 1.3f, 0.08f),
                palette.timber,
                false);
            var sail = CreateCityVisualPrimitive(
                PrimitiveType.Quad,
                $"{label} route sail",
                skiff,
                new Vector3(0.15f, 2.25f, 0f),
                new Vector3(1.35f, 1.1f, 1f),
                palette.canopy,
                false);
            sail.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            return skiff;
        }

        private void DressHearthmarketTerraces()
        {
            SpawnCityDressing(
                "plant_bushLarge",
                new Vector3(-30f, 0f, 92f),
                Vector3.one * 0.82f,
                new Color(0.32f, 0.62f, 0.31f),
                18f);
            SpawnCityDressing(
                "plant_bushDetailed",
                new Vector3(-25f, 0f, 96f),
                Vector3.one * 0.72f,
                new Color(0.4f, 0.7f, 0.34f),
                -12f);
            SpawnCityDressing(
                "plant_bushLarge",
                new Vector3(27f, 0f, 94f),
                Vector3.one * 0.78f,
                new Color(0.35f, 0.64f, 0.3f),
                -22f);
            SpawnCityDressing(
                "plant_bushDetailed",
                new Vector3(31f, 0f, 88f),
                Vector3.one * 0.66f,
                new Color(0.42f, 0.68f, 0.35f),
                16f);
            SpawnCityDressing(
                "fence_simple",
                new Vector3(-28f, 0f, 98f),
                new Vector3(0.92f, 1.05f, 0.92f),
                new Color(0.29f, 0.17f, 0.08f),
                90f);
            SpawnCityDressing(
                "fence_simple",
                new Vector3(29f, 0f, 98f),
                new Vector3(0.92f, 1.05f, 0.92f),
                new Color(0.29f, 0.17f, 0.08f),
                90f);
        }

        private void DressVeyrwaterMooringEdges()
        {
            SpawnCityDressing(
                "plant_bushDetailed",
                new Vector3(185f, 0f, 108f),
                Vector3.one * 0.74f,
                new Color(0.2f, 0.53f, 0.45f),
                12f);
            SpawnCityDressing(
                "plant_bushLarge",
                new Vector3(192f, 0f, 108f),
                Vector3.one * 0.84f,
                new Color(0.22f, 0.5f, 0.42f),
                -20f);
            SpawnCityDressing(
                "plant_bushDetailed",
                new Vector3(216f, 0f, 108f),
                Vector3.one * 0.72f,
                new Color(0.19f, 0.47f, 0.4f),
                24f);
            SpawnCityDressing(
                "plant_bushLarge",
                new Vector3(223f, 0f, 108f),
                Vector3.one * 0.8f,
                new Color(0.23f, 0.5f, 0.4f),
                -14f);
            SpawnCityDressing(
                "rock_smallA",
                new Vector3(181f, 0f, 107f),
                Vector3.one * 0.8f,
                new Color(0.25f, 0.37f, 0.39f),
                28f);
            SpawnCityDressing(
                "rock_smallA",
                new Vector3(226f, 0f, 106f),
                Vector3.one * 0.76f,
                new Color(0.24f, 0.36f, 0.38f),
                -18f);
            SpawnCityDressing(
                "fence_simple",
                new Vector3(188f, 0f, 109.5f),
                Vector3.one * 0.9f,
                new Color(0.17f, 0.12f, 0.08f),
                90f);
            SpawnCityDressing(
                "fence_simple",
                new Vector3(218f, 0f, 109.5f),
                Vector3.one * 0.9f,
                new Color(0.17f, 0.12f, 0.08f),
                90f);
        }

        private void CreateHearthmarketGateway()
        {
            var root = new GameObject("Hearthmarket copper gateway");
            root.transform.SetParent(transform, false);
            root.transform.position = definition.center + new Vector3(0f, 0f, -24f);
            for (var side = -1; side <= 1; side += 2)
            {
                CreateCityVisualPrimitive(
                    PrimitiveType.Cube,
                    "Hearthmarket gateway stone support",
                    root.transform,
                    new Vector3(side * 6f, 3f, 0f),
                    new Vector3(0.7f, 3f, 0.82f),
                    palette.stone,
                    true);
                CreateCityVisualPrimitive(
                    PrimitiveType.Cylinder,
                    "Hearthmarket gateway copper cap",
                    root.transform,
                    new Vector3(side * 6f, 6.08f, 0f),
                    new Vector3(0.44f, 0.12f, 0.44f),
                    palette.copper,
                    false);
            }

            CreateCityVisualPrimitive(
                PrimitiveType.Cube,
                "Hearthmarket gateway copper lintel",
                root.transform,
                new Vector3(0f, 6f, 0f),
                new Vector3(12.8f, 0.26f, 0.5f),
                palette.copper,
                false);
            CreateWayfindingSign(root.transform, new Vector3(0f, 4.58f, -0.3f), new Vector3(2.4f, 1.08f, 1f));
        }

        private void CreateVeyrwaterChannelFrame()
        {
            var root = new GameObject("Veyrwater channel frame");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(203f, 0f, 98f);
            for (var side = -1; side <= 1; side += 2)
            {
                CreateCityVisualPrimitive(
                    PrimitiveType.Cylinder,
                    "Veyrwater channel frame support",
                    root.transform,
                    new Vector3(0f, 2.8f, side * 7.65f),
                    new Vector3(0.34f, 2.8f, 0.34f),
                    palette.timber,
                    false);
                CreateCityVisualPrimitive(
                    PrimitiveType.Sphere,
                    "Veyrwater channel frame marker",
                    root.transform,
                    new Vector3(0f, 5.76f, side * 7.65f),
                    Vector3.one * 0.28f,
                    palette.marker,
                    false);
            }

            CreateCityVisualPrimitive(
                PrimitiveType.Cube,
                "Veyrwater channel frame beam",
                root.transform,
                new Vector3(0f, 5.55f, 0f),
                new Vector3(0.36f, 0.22f, 16.1f),
                palette.copper,
                false);
            CreateWayfindingSign(root.transform, new Vector3(-0.22f, 4.3f, -0.26f), new Vector3(1.92f, 0.9f, 1f));
        }

        private void CreateVeyrwaterCanal()
        {
            var position = new Vector3(203f, 0.02f, 98f);
            var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name = "Veyrwater swimmable canal";
            surface.transform.position = position;
            surface.transform.localScale = new Vector3(42f, 0.12f, 13f);
            surface.GetComponent<Renderer>().sharedMaterial = palette.water;
            UnityEngine.Object.Destroy(surface.GetComponent<Collider>());
            WaterSurface.Attach(surface, palette.water, palette.waterColor);
            WaterVolume.Create(new Vector3(position.x, 1f, position.z), new Vector3(42f, 3f, 13f));

            for (var index = 0; index < 6; index++)
            {
                var x = 186f + index * 6.8f;
                CreateMooringPost(new Vector3(x, 0f, 91.9f));
                CreateMooringPost(new Vector3(x, 0f, 104.1f));
            }
        }

        private void CreateMooringPost(Vector3 position)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Veyrwater canal mooring post";
            post.transform.position = position + Vector3.up * 1.05f;
            post.transform.localScale = new Vector3(0.2f, 1.05f, 0.2f);
            post.GetComponent<Renderer>().sharedMaterial = palette.timber;
            UnityEngine.Object.Destroy(post.GetComponent<Collider>());

            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "Veyrwater mooring cap";
            cap.transform.SetParent(post.transform, false);
            cap.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            cap.transform.localScale = Vector3.one * 0.34f;
            cap.GetComponent<Renderer>().sharedMaterial = palette.marker;
            UnityEngine.Object.Destroy(cap.GetComponent<Collider>());
        }

        private void CreateMarketBuildings()
        {
            CreateHouse(new Vector3(-21f, 0f, 65f), new Color(0.26f, 0.4f, 0.3f), new Color(0.64f, 0.32f, 0.19f), "Pell's Hearth");
            CreateHouse(new Vector3(-21f, 0f, 85f), new Color(0.23f, 0.36f, 0.44f), new Color(0.46f, 0.3f, 0.64f), "Route archive");
            CreateHouse(new Vector3(17f, 0f, 64f), new Color(0.39f, 0.28f, 0.2f), new Color(0.51f, 0.22f, 0.18f), "Rivergate ringhouse");
            CreateHouse(new Vector3(20f, 0f, 88f), new Color(0.29f, 0.47f, 0.3f), new Color(0.72f, 0.4f, 0.18f), "Copperweaver");
            CreateHouse(new Vector3(-38f, 0f, 77f), new Color(0.32f, 0.38f, 0.26f), new Color(0.43f, 0.22f, 0.15f), "Courier lodge");
            CreateHouse(new Vector3(39f, 0f, 75f), new Color(0.31f, 0.43f, 0.34f), new Color(0.68f, 0.31f, 0.21f), "Terrace store");
        }

        private void CreateMarketStalls()
        {
            var stalls = new[]
            {
                new Vector3(-10f, 0f, 72f),
                new Vector3(-3f, 0f, 76f),
                new Vector3(6f, 0f, 75f),
                new Vector3(12f, 0f, 71f),
                new Vector3(-9f, 0f, 82f)
            };
            for (var index = 0; index < stalls.Length; index++)
            {
                CreateStall(
                    stalls[index],
                    Color.Lerp(definition.accentColor, new Color(0.92f, 0.52f, 0.22f), index / 5f),
                    index % 2 == 0 ? "route goods" : "garden wares");
            }

            WorldArt.SpawnKenney(game, "tent_detailedOpen", new Vector3(-4f, 0f, 91f), Vector3.one * 1.25f, new Color(0.2f, 0.47f, 0.4f), true);
            WorldArt.SpawnKenney(game, "campfire_stones", new Vector3(-14f, 0f, 61f), Vector3.one * 1.15f, new Color(0.94f, 0.48f, 0.2f));
            WorldArt.SpawnKenney(game, "fence_simple", new Vector3(5f, 0f, 96f), new Vector3(1.35f, 1.35f, 1.35f), new Color(0.37f, 0.23f, 0.14f), true);
        }

        private void CreateGardenAndRiverwork()
        {
            var beds = new[]
            {
                new Vector3(3f, 0.16f, 92f),
                new Vector3(9f, 0.16f, 94f),
                new Vector3(15f, 0.16f, 92f)
            };
            foreach (var bed in beds)
            {
                var planter = GameObject.CreatePrimitive(PrimitiveType.Cube);
                planter.name = "Hearthmarket planter";
                planter.transform.position = bed;
                planter.transform.localScale = new Vector3(4.2f, 0.32f, 1.7f);
                planter.GetComponent<Renderer>().sharedMaterial = palette.timber;
                WorldArt.SpawnKenney(game, "plant_bushDetailed", bed + Vector3.up * 0.35f, Vector3.one * 0.65f, new Color(0.33f, 0.76f, 0.42f));
            }

            CreateBanner(new Vector3(-2.7f, 0f, 57f), definition.accentColor);
            CreateBanner(new Vector3(2.7f, 0f, 57f), new Color(0.92f, 0.52f, 0.24f));
            CreateBanner(new Vector3(-30f, 0f, 72f), new Color(0.35f, 0.7f, 0.58f));
            CreateBanner(new Vector3(30f, 0f, 72f), new Color(0.88f, 0.38f, 0.2f));
        }

        private void CreateRoad(Vector3 position, Vector3 scale)
        {
            var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "Hearthmarket stone road";
            road.transform.position = position;
            road.transform.localScale = scale;
            road.GetComponent<Renderer>().sharedMaterial = palette.road;
        }

        private void CreatePlaza(Vector3 center)
        {
            var plaza = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plaza.name = "Hearthmarket commons";
            plaza.transform.position = center + new Vector3(0f, 0.02f, 1f);
            plaza.transform.localScale = new Vector3(18f, 0.1f, 18f);
            plaza.GetComponent<Renderer>().sharedMaterial = palette.plaza;

            var hearth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hearth.name = "Hearthmarket route lantern";
            hearth.transform.position = center + new Vector3(0f, 1.4f, 1f);
            hearth.transform.localScale = new Vector3(0.78f, 1.35f, 0.78f);
            hearth.GetComponent<Renderer>().sharedMaterial = palette.copper;
            WorldVisuals.AddLabel(hearth.transform, "Hearthmarket commons", new Vector3(0f, 2.05f, 0f), new Color(0.84f, 0.95f, 0.71f), 0.08f);
        }

        private void CreateHouse(Vector3 position, Color wallColor, Color roofColor, string label)
        {
            var root = new GameObject(label);
            root.transform.position = position;

            var walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walls.name = $"{label} walls";
            walls.transform.SetParent(root.transform, false);
            walls.transform.localPosition = new Vector3(0f, 2f, 0f);
            walls.transform.localScale = new Vector3(8f, 4f, 6f);
            walls.GetComponent<Renderer>().sharedMaterial = palette.wall;

            CreateRoofHalf(root.transform, label, -1f, roofColor);
            CreateRoofHalf(root.transform, label, 1f, roofColor);

            var chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimney.name = $"{label} chimney";
            chimney.transform.SetParent(root.transform, false);
            chimney.transform.localPosition = new Vector3(2.2f, 5.75f, 1.45f);
            chimney.transform.localScale = new Vector3(0.7f, 2.1f, 0.8f);
            chimney.GetComponent<Renderer>().sharedMaterial = palette.stone;

            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = $"{label} door";
            door.transform.SetParent(root.transform, false);
            door.transform.localPosition = new Vector3(0f, 1.05f, -3.06f);
            door.transform.localScale = new Vector3(1.35f, 2.1f, 0.16f);
            door.GetComponent<Renderer>().sharedMaterial = palette.timber;

            for (var index = 0; index < 2; index++)
            {
                var window = GameObject.CreatePrimitive(PrimitiveType.Cube);
                window.name = $"{label} window";
                window.transform.SetParent(root.transform, false);
                window.transform.localPosition = new Vector3(index == 0 ? -2.35f : 2.35f, 2.2f, -3.08f);
                window.transform.localScale = new Vector3(1.1f, 1.25f, 0.14f);
                window.GetComponent<Renderer>().sharedMaterial = palette.window;
            }

            CreateBanner(position + new Vector3(-3.7f, 0f, -3.35f), Color.Lerp(wallColor, Color.white, 0.25f));
        }

        private void CreateRoofHalf(Transform parent, string label, float direction, Color roofColor)
        {
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = $"{label} roof";
            roof.transform.SetParent(parent, false);
            roof.transform.localPosition = new Vector3(direction * 2.2f, 4.82f, 0f);
            roof.transform.localScale = new Vector3(4.9f, 0.5f, 6.85f);
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, direction * 25f);
            roof.GetComponent<Renderer>().sharedMaterial = palette.roof;
        }

        private void CreateStall(Vector3 position, Color canopyColor, string label)
        {
            var root = new GameObject($"Hearthmarket {label}");
            root.transform.position = position;
            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.transform.SetParent(root.transform, false);
            counter.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            counter.transform.localScale = new Vector3(3.8f, 1.45f, 1.45f);
            counter.GetComponent<Renderer>().sharedMaterial = palette.timber;

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.transform.SetParent(root.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 3.1f, 0f);
            canopy.transform.localScale = new Vector3(4.6f, 0.35f, 2.25f);
            canopy.GetComponent<Renderer>().sharedMaterial = palette.canopy;

            for (var index = 0; index < 4; index++)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.transform.SetParent(root.transform, false);
                post.transform.localPosition = new Vector3(index < 2 ? -1.8f : 1.8f, 1.55f, index % 2 == 0 ? -0.8f : 0.8f);
                post.transform.localScale = new Vector3(0.12f, 1.55f, 0.12f);
                post.GetComponent<Renderer>().sharedMaterial = palette.timber;
            }
        }

        private void CreateBanner(Vector3 position, Color color)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Hearthmarket banner pole";
            pole.transform.position = position + Vector3.up * 2f;
            pole.transform.localScale = new Vector3(0.09f, 2f, 0.09f);
            pole.GetComponent<Renderer>().sharedMaterial = palette.copper;

            var fabric = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fabric.name = "Hearthmarket banner";
            fabric.transform.position = position + new Vector3(0.48f, 3f, 0f);
            fabric.transform.localScale = new Vector3(1.2f, 1.65f, 1f);
            fabric.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            fabric.GetComponent<Renderer>().material = CityVisualBuilder.CreateCrestMaterial(game, color, definition.id);
            UnityEngine.Object.Destroy(fabric.GetComponent<Collider>());
            fabric.AddComponent<CityBannerMotion>().Initialize(position.x + position.z);
        }

        private void CreateMarketLighting()
        {
            var lanternPositions = new[]
            {
                new Vector3(-10f, 2.5f, 63f),
                new Vector3(10f, 2.5f, 63f),
                new Vector3(-12f, 2.5f, 82f),
                new Vector3(12f, 2.5f, 82f),
                new Vector3(-28f, 2.5f, 72f),
                new Vector3(28f, 2.5f, 72f)
            };
            foreach (var position in lanternPositions)
            {
                CreateCityLantern(position, new Color(1f, 0.62f, 0.32f), "Hearthmarket lantern");
            }
        }

        private void CreateMooringLighting()
        {
            var lanternPositions = new[]
            {
                new Vector3(192f, 2.5f, 64f),
                new Vector3(214f, 2.5f, 64f),
                new Vector3(192f, 2.5f, 83f),
                new Vector3(214f, 2.5f, 83f),
                new Vector3(188f, 2.5f, 96f),
                new Vector3(218f, 2.5f, 96f)
            };
            foreach (var position in lanternPositions)
            {
                CreateCityLantern(position, new Color(0.38f, 0.86f, 0.79f), "Veyrwater lantern");
            }
        }

        private void CreateCityLantern(Vector3 position, Color color, string name)
        {
            var lantern = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lantern.name = name;
            lantern.transform.position = position;
            lantern.transform.localScale = Vector3.one * 0.38f;
            lantern.GetComponent<Renderer>().sharedMaterial = palette.lantern;
            UnityEngine.Object.Destroy(lantern.GetComponent<Collider>());
            var light = lantern.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 1.05f;
            light.range = 8f;
            light.shadows = LightShadows.None;
            cityLights.Add(light);
        }

        private GameObject CreateCityVisualPrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool solid)
        {
            var visual = GameObject.CreatePrimitive(primitiveType);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
            visual.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid)
            {
                UnityEngine.Object.Destroy(visual.GetComponent<Collider>());
            }

            return visual;
        }

        private void SpawnCityDressing(
            string assetName,
            Vector3 position,
            Vector3 scale,
            Color tint,
            float yaw)
        {
            var prop = WorldArt.SpawnKenney(game, assetName, position, scale, tint);
            if (prop == null)
            {
                return;
            }

            prop.transform.SetParent(transform, true);
            prop.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void CreateWayfindingSign(Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            var sign = CreateCityVisualPrimitive(
                PrimitiveType.Quad,
                $"{definition.displayName} wayfinding sign",
                parent,
                localPosition,
                localScale,
                CityVisualBuilder.CreateWayfindingMaterial(game, definition),
                false);
            sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }

    public sealed class CityNoticeBoard : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private CityDirector city;

        public CityDirector City => city;
        public string InteractionPrompt => city?.Definition != null
            ? $"Read {city.Definition.displayName} civic notices"
            : "Read civic notices";

        public static CityNoticeBoard Create(TetherfallGame game, CityDirector city, Vector3 position)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = $"{city.Definition.displayName} civic noticeboard";
            root.transform.position = position;
            root.transform.localScale = new Vector3(2f, 2.7f, 0.28f);
            var board = root.AddComponent<CityNoticeBoard>();
            board.game = game;
            board.city = city;
            root.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.31f, 0.19f, 0.1f), 0.04f, 0.24f);
            WorldVisuals.AddLabel(root.transform, "Civic notices", new Vector3(0f, 1.75f, 0f), new Color(0.95f, 0.79f, 0.51f), 0.075f);
            return board;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null && city?.Definition != null;
        }

        public void Interact(TetherfallGame owner)
        {
            if (CanInteract(owner))
            {
                owner.OpenCityBoard(this);
            }
        }
    }

    public sealed class CityResident : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private CityDirector city;
        private CityNpcDefinition definition;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private Vector3 baseScale;
        private Vector3 wanderTarget;
        private Vector3 lastAnimationPosition;
        private Transform visualRoot;
        private float nextWanderAt;
        private float idlePhase;
        private HumanProceduralAnimator proceduralAnimator;

        public string DisplayName => definition != null ? definition.displayName : "Resident";
        public string InteractionPrompt
        {
            get
            {
                if (definition == null)
                {
                    return "Speak";
                }

                switch (definition.service)
                {
                    case CityResidentService.NoticeBoard:
                        return $"Review civic work with {definition.displayName}";
                    case CityResidentService.Inn:
                        return $"Rest at {definition.displayName}'s hearth";
                    case CityResidentService.Archive:
                        return $"Record field notes with {definition.displayName}";
                    default:
                        return $"Speak with {definition.displayName}, {definition.role}";
                }
            }
        }

        public static CityResident Create(TetherfallGame game, CityDirector city, CityNpcDefinition definition)
        {
            if (game == null || city == null || definition == null)
            {
                return null;
            }

            var root = new GameObject(definition.displayName);
            root.name = definition.displayName;
            root.transform.position = definition.position;
            root.transform.localScale = new Vector3(0.74f, 1.02f, 0.74f);
            var resident = root.AddComponent<CityResident>();
            resident.game = game;
            resident.city = city;
            resident.definition = definition;
            resident.homePosition = definition.position;
            resident.homeRotation = root.transform.rotation;
            resident.baseScale = root.transform.localScale;
            resident.wanderTarget = definition.position;
            resident.lastAnimationPosition = definition.position;
            resident.nextWanderAt = Time.time + 1.5f;
            resident.idlePhase = definition.position.x * 0.13f + definition.position.z * 0.07f;
            var visual = TetherfallCityResidentArt.Build(
                game,
                root.transform,
                definition.color,
                definition.role,
                definition.id);
            resident.visualRoot = visual;
            resident.proceduralAnimator = TetherfallHumanAnimation.Attach(visual, game);
            if (resident.proceduralAnimator != null)
            {
                resident.proceduralAnimator.SetIdleVariant(ResolveIdleVariant(definition));
            }
            WorldVisuals.AddLabel(root.transform, definition.displayName, new Vector3(0f, 1.55f, 0f), new Color(0.96f, 0.9f, 0.76f), 0.075f);
            WorldVisuals.AddLabel(
                root.transform,
                GetRoleLabel(definition),
                new Vector3(0f, 1.28f, 0f),
                new Color(0.61f, 0.82f, 0.77f),
                0.055f);
            return resident;
        }

        private void Update()
        {
            EnsureProceduralAnimation();
            if (game == null || definition == null)
            {
                return;
            }

            var playerDistanceSquared = game.PlayerTransform != null
                ? (game.PlayerTransform.position - homePosition).sqrMagnitude
                : float.PositiveInfinity;
            var playerInAnimationRange = playerDistanceSquared <= 48f * 48f;
            var playerInPresentationRange = playerDistanceSquared <= 90f * 90f;
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
                if (baseScale != Vector3.zero)
                {
                    transform.localScale = baseScale;
                }

                lastAnimationPosition = transform.position;
                return;
            }

            UpdateIdleAmbience();
            UpdateProceduralAnimation();
            var homeOffset = transform.position - homePosition;
            homeOffset.y = 0f;
            var permittedRadius = Mathf.Max(0f, definition.wanderRadius) + 0.8f;
            if (!IsFinite(transform.position) ||
                Mathf.Abs(transform.position.y - homePosition.y) > 1.25f ||
                homeOffset.sqrMagnitude > permittedRadius * permittedRadius)
            {
                RestoreRuntimeState();
            }

            if (definition.wanderRadius <= 0f)
            {
                return;
            }

            var toPlayer = game.PlayerTransform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude <= 10.24f)
            {
                wanderTarget = transform.position;
                nextWanderAt = Mathf.Max(nextWanderAt, Time.time + 1.2f);
                if (toPlayer.sqrMagnitude > 0.05f)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(toPlayer.normalized),
                        Time.deltaTime * 5f);
                }

                return;
            }

            if (Time.time >= nextWanderAt || Vector3.Distance(transform.position, wanderTarget) < 0.14f)
            {
                ChooseSafeWanderTarget();
            }

            var direction = wanderTarget - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.position += direction.normalized *
                                      Mathf.Min(direction.magnitude, Time.deltaTime * 0.55f);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction.normalized),
                    Time.deltaTime * 4f);
            }
        }

        private void EnsureProceduralAnimation()
        {
            if (proceduralAnimator != null && proceduralAnimator.IsBound)
            {
                return;
            }

            proceduralAnimator = TetherfallHumanAnimation.Attach(transform, game);
            if (proceduralAnimator != null)
            {
                proceduralAnimator.SetIdleVariant(ResolveIdleVariant(definition));
            }
        }

        private void UpdateProceduralAnimation()
        {
            var displacement = transform.position - lastAnimationPosition;
            lastAnimationPosition = transform.position;
            if (proceduralAnimator == null || Time.deltaTime <= 0.0001f)
            {
                return;
            }

            displacement.y = 0f;
            var velocity = displacement / Time.deltaTime;
            var localVelocity = transform.InverseTransformDirection(velocity);
            proceduralAnimator.SetLocomotion(
                Mathf.Clamp(localVelocity.z / 0.55f, -1f, 1f),
                Mathf.Clamp(localVelocity.x / 0.55f, -1f, 1f),
                Mathf.Clamp01(velocity.magnitude / 0.55f));
        }

        private void ChooseSafeWanderTarget()
        {
            var phase = Time.time * 0.37f + idlePhase;
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var candidatePhase = phase + attempt * 1.71f;
                var candidate = homePosition + new Vector3(
                    Mathf.Sin(candidatePhase) * definition.wanderRadius,
                    0f,
                    Mathf.Cos(candidatePhase * 1.31f) * definition.wanderRadius);
                var path = candidate - transform.position;
                path.y = 0f;
                if (path.sqrMagnitude <= 0.02f ||
                    !Physics.SphereCast(
                        transform.position + Vector3.up * 0.35f,
                        0.28f,
                        path.normalized,
                        out _,
                        path.magnitude,
                        Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore))
                {
                    wanderTarget = candidate;
                    nextWanderAt = Time.time + 2.5f + Mathf.Abs(Mathf.Sin(candidatePhase)) * 2f;
                    return;
                }
            }

            wanderTarget = homePosition;
            nextWanderAt = Time.time + 2f;
        }

        private void UpdateIdleAmbience()
        {
            if (baseScale == Vector3.zero)
            {
                baseScale = transform.localScale;
            }

            var breath = Mathf.Sin(Time.time * 1.35f + idlePhase) * 0.012f;
            transform.localScale = new Vector3(
                baseScale.x * (1f - breath * 0.35f),
                baseScale.y * (1f + breath),
                baseScale.z * (1f - breath * 0.35f));
        }

        private static bool IsFinite(Vector3 position)
        {
            return !float.IsNaN(position.x) &&
                   !float.IsNaN(position.y) &&
                   !float.IsNaN(position.z) &&
                   !float.IsInfinity(position.x) &&
                   !float.IsInfinity(position.y) &&
                   !float.IsInfinity(position.z);
        }

        private static string GetRoleLabel(CityNpcDefinition resident)
        {
            if (resident == null)
            {
                return "resident";
            }

            switch (resident.service)
            {
                case CityResidentService.NoticeBoard:
                    return $"{resident.role} · civic work";
                case CityResidentService.Inn:
                    return $"{resident.role} · rest";
                case CityResidentService.Archive:
                    return $"{resident.role} · archive";
                default:
                    return resident.role;
            }
        }

        private static HumanIdleVariant ResolveIdleVariant(CityNpcDefinition resident)
        {
            if (resident == null)
            {
                return HumanIdleVariant.Neutral;
            }

            switch (resident.service)
            {
                case CityResidentService.NoticeBoard:
                case CityResidentService.Archive:
                    return HumanIdleVariant.Working;
                case CityResidentService.Inn:
                    return HumanIdleVariant.Relaxed;
                default:
                    return HumanIdleVariant.Neutral;
            }
        }

        public void RestoreRuntimeState()
        {
            transform.position = homePosition;
            transform.rotation = homeRotation;
            if (baseScale != Vector3.zero)
            {
                transform.localScale = baseScale;
            }

            wanderTarget = homePosition;
            lastAnimationPosition = homePosition;
            nextWanderAt = Time.time + 1.5f;
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null && definition != null && city != null;
        }

        public void Interact(TetherfallGame owner)
        {
            if (!CanInteract(owner))
            {
                return;
            }

            wanderTarget = transform.position;
            nextWanderAt = Time.time + 4f;
            if (proceduralAnimator != null)
            {
                switch (definition.service)
                {
                    case CityResidentService.NoticeBoard:
                    case CityResidentService.Archive:
                        proceduralAnimator.TriggerQuestHandoff();
                        break;
                    case CityResidentService.Inn:
                        proceduralAnimator.TriggerVendorGesture();
                        break;
                    default:
                        proceduralAnimator.TriggerTalk();
                        break;
                }
            }

            switch (definition.service)
            {
                case CityResidentService.NoticeBoard:
                    if (city.NoticeBoard != null)
                    {
                        owner.OpenCityBoard(city.NoticeBoard);
                    }
                    else
                    {
                        owner.Notify($"{definition.displayName}: The civic board is being repaired.");
                    }
                    break;

                case CityResidentService.Inn:
                    owner.RestAtCityInn(this);
                    break;

                case CityResidentService.Archive:
                    owner.OpenCityArchive(city);
                    break;

                default:
                    owner.Notify($"{definition.displayName}: {definition.dialogue}");
                    break;
            }
        }
    }

    public sealed class CityTrainingArena : MonoBehaviour, IWorldInteractable
    {
        private readonly List<Enemy> activeEnemies = new List<Enemy>();
        private TetherfallGame game;
        private CityDirector city;
        private CityActivityDefinition configuredActivity;
        private CityActivityDefinition activeActivity;
        private Vector3 center;
        private float drillStartedAt;
        private int expectedEnemyCount;
        private int currentWave;
        private int totalWaves;
        private int runNumber;
        private bool completionAwarded;
        private bool resetting;
        private float noLiveEnemiesSince;

        public bool HasActiveDrill => activeActivity != null;
        public int CurrentWave => currentWave;
        public int TotalWaves => totalWaves;
        public string StatusText
        {
            get
            {
                if (activeActivity == null)
                {
                    return "Drill ready";
                }

                var remaining = RemainingEnemyCount;
                return $"Wave {Mathf.Max(1, currentWave)}/{Mathf.Max(1, totalWaves)} · " +
                       $"{remaining} {(remaining == 1 ? "threat" : "threats")} remaining";
            }
        }

        public int RemainingEnemyCount
        {
            get
            {
                var count = 0;
                foreach (var enemy in activeEnemies)
                {
                    if (enemy != null && !enemy.IsDefeated)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public string InteractionPrompt
        {
            get
            {
                if (activeActivity != null)
                {
                    return $"{activeActivity.title}: {StatusText}";
                }

                if (configuredActivity == null)
                {
                    return "Inspect training ring";
                }

                return game != null && !game.IsCityActivityAvailable(configuredActivity)
                    ? $"{configuredActivity.title} returns {game.GetCityActivityCooldownText(configuredActivity)}"
                    : $"Begin {configuredActivity.title}";
            }
        }

        public static CityTrainingArena Create(TetherfallGame game, CityDirector city, CityActivityDefinition activity)
        {
            if (game == null || city?.Definition == null || activity == null)
            {
                return null;
            }

            var root = new GameObject($"{city.Definition.displayName} Training Arena");
            root.transform.SetParent(city.transform, false);
            root.transform.position = activity.arenaPosition;
            var arena = root.AddComponent<CityTrainingArena>();
            arena.game = game;
            arena.city = city;
            arena.configuredActivity = activity;
            arena.center = activity.arenaPosition;
            arena.BuildGeometry();
            WorldVisuals.AddLabel(
                root.transform,
                activity.title,
                new Vector3(0f, 4.7f, 0f),
                city.Definition.accentColor,
                0.085f);
            return arena;
        }

        public bool IsActive(CityActivityDefinition activity)
        {
            return activeActivity != null &&
                   activity != null &&
                   string.Equals(activeActivity.id, activity.id, StringComparison.Ordinal);
        }

        public bool TryGetActiveActivity(out CityActivityDefinition activity)
        {
            activity = activeActivity;
            return activity != null;
        }

        public bool Begin(CityActivityDefinition activity)
        {
            if (activeActivity != null ||
                game == null ||
                city == null ||
                configuredActivity == null ||
                activity == null ||
                !string.Equals(activity.id, configuredActivity.id, StringComparison.Ordinal) ||
                activity.enemyIds == null ||
                activity.enemyIds.Length == 0 ||
                !game.IsCityActivityAvailable(activity))
            {
                return false;
            }

            if (activeEnemies.Count > 0)
            {
                ResetDrill();
            }

            activeActivity = activity;
            completionAwarded = false;
            var completedRuns = game.GetCityActivityCompletionCount(activity);
            runNumber = completedRuns < int.MaxValue ? completedRuns + 1 : int.MaxValue;
            currentWave = 1;
            totalWaves = CalculateWaveCount(completedRuns);
            drillStartedAt = Time.time;
            if (!SpawnWave())
            {
                ResetDrill();
                return false;
            }

            game.Notify(
                $"{activity.title} run {runNumber} started: {totalWaves} " +
                $"{(totalWaves == 1 ? "wave" : "waves")}. Remain near the ring.");
            return true;
        }

        internal static int CalculateWaveCount(int completedRuns)
        {
            return Mathf.Clamp(Mathf.Max(0, completedRuns) + 1, 1, 3);
        }

        private bool SpawnWave()
        {
            if (activeActivity?.enemyIds == null ||
                activeActivity.enemyIds.Length == 0 ||
                activeEnemies.Count > 0)
            {
                return false;
            }

            var enemyCount = Mathf.Min(
                activeActivity.enemyIds.Length + Mathf.Max(0, currentWave - 1),
                5);
            for (var index = 0; index < enemyCount; index++)
            {
                var enemyId = activeActivity.enemyIds[
                    (index + Mathf.Max(0, currentWave - 1)) % activeActivity.enemyIds.Length];
                var position = GetSafeSpawnPosition(index, enemyCount, currentWave);
                var enemy = Enemy.Create(game, null, enemyId, position, OnEnemyDefeated);
                if (enemy != null)
                {
                    activeEnemies.Add(enemy);
                }
            }

            expectedEnemyCount = activeEnemies.Count;
            return expectedEnemyCount > 0;
        }

        private void Update()
        {
            if (activeActivity == null || game == null || game.PlayerTransform == null || game.GameplayPaused)
            {
                return;
            }

            for (var index = activeEnemies.Count - 1; index >= 0; index--)
            {
                if (activeEnemies[index] == null)
                {
                    activeEnemies.RemoveAt(index);
                }
            }

            if (activeEnemies.Count != expectedEnemyCount)
            {
                var interruptedTitle = activeActivity.title;
                ResetDrill();
                game.Notify($"{interruptedTitle} ended because its remaining threats could not be accounted for.");
                return;
            }

            var liveEnemies = 0;
            for (var index = 0; index < activeEnemies.Count; index++)
            {
                if (activeEnemies[index] != null && !activeEnemies[index].IsDefeated)
                {
                    liveEnemies++;
                }
            }

            if (activeEnemies.Count > 0 && liveEnemies == 0)
            {
                if (noLiveEnemiesSince <= 0f)
                {
                    noLiveEnemiesSince = Time.time;
                }
                else if (Time.time - noLiveEnemiesSince > 2f)
                {
                    var interruptedTitle = activeActivity.title;
                    ResetDrill();
                    game.Notify(
                        $"{interruptedTitle} reset after its defeated formation failed to clear. " +
                        "It can be restarted.");
                }

                return;
            }

            noLiveEnemiesSince = 0f;
            if ((game.PlayerTransform.position - center).sqrMagnitude > 784f)
            {
                var activityTitle = activeActivity.title;
                ResetDrill();
                game.Notify($"{activityTitle} dispersed when you left the training perimeter. It can be restarted.");
                return;
            }

            if (Time.time - drillStartedAt > 300f || HasEnemyLeftPerimeter())
            {
                var activityTitle = activeActivity.title;
                ResetDrill();
                game.Notify($"{activityTitle} was reset after the controlled formation broke. It can be restarted.");
            }
        }

        private void OnEnemyDefeated(Enemy enemy)
        {
            if (activeActivity == null)
            {
                return;
            }

            if (!activeEnemies.Remove(enemy))
            {
                return;
            }

            expectedEnemyCount = Mathf.Max(0, expectedEnemyCount - 1);
            for (var index = activeEnemies.Count - 1; index >= 0; index--)
            {
                if (activeEnemies[index] == null)
                {
                    activeEnemies.RemoveAt(index);
                }
            }

            if (activeEnemies.Count != expectedEnemyCount)
            {
                var interruptedTitle = activeActivity.title;
                ResetDrill();
                game.Notify($"{interruptedTitle} ended because its remaining threats could not be accounted for.");
                return;
            }

            if (activeEnemies.Count > 0)
            {
                game.Notify(
                    $"{activeActivity.title}, wave {currentWave}/{totalWaves}: {activeEnemies.Count} " +
                    $"{(activeEnemies.Count == 1 ? "threat" : "threats")} remaining.");
                return;
            }

            if (currentWave < totalWaves)
            {
                currentWave++;
                if (!SpawnWave())
                {
                    var interruptedTitle = activeActivity.title;
                    ResetDrill();
                    game.Notify(
                        $"{interruptedTitle} reset because the next controlled wave could not assemble. " +
                        "It can be restarted.");
                    return;
                }

                game.Notify(
                    $"{activeActivity.title}: wave {currentWave}/{totalWaves} entering, " +
                    $"{activeEnemies.Count} controlled threats.");
                return;
            }

            if (completionAwarded)
            {
                return;
            }

            completionAwarded = true;
            var completedActivity = activeActivity;
            var completedWaveCount = totalWaves;
            activeActivity = null;
            drillStartedAt = 0f;
            expectedEnemyCount = 0;
            currentWave = 0;
            totalWaves = 0;
            noLiveEnemiesSince = 0f;
            city?.OnTrainingCompleted(completedActivity, completedWaveCount);
        }

        private void ResetDrill()
        {
            if (resetting)
            {
                return;
            }

            resetting = true;
            activeActivity = null;
            drillStartedAt = 0f;
            expectedEnemyCount = 0;
            currentWave = 0;
            totalWaves = 0;
            runNumber = 0;
            completionAwarded = false;
            noLiveEnemiesSince = 0f;
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    UnityEngine.Object.Destroy(enemy.gameObject);
                }
            }

            activeEnemies.Clear();
            resetting = false;
        }

        public void RestoreRuntimeState()
        {
            ResetDrill();
        }

        private void OnDestroy()
        {
            ResetDrill();
        }

        private bool HasEnemyLeftPerimeter()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null &&
                    (enemy.transform.position - center).sqrMagnitude > 1296f)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3 GetSafeSpawnPosition(int index, int total, int wave)
        {
            var angle = index * Mathf.PI * 2f / Mathf.Max(1, total) +
                        Mathf.Max(0, wave - 1) * 0.41f;
            var radius = 4.5f + Mathf.Min(1f, Mathf.Max(0, wave - 1) * 0.35f);
            var position = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);
            if (Physics.Raycast(
                    position + Vector3.up * 6f,
                    Vector3.down,
                    out var groundHit,
                    12f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                position.y = groundHit.point.y + 0.04f;
            }

            return position;
        }

        private void BuildGeometry()
        {
            var palette = CityVisualBuilder.GetPalette(game, city.Definition);
            CityInstitutionBuilder.CreatePrimitive(
                PrimitiveType.Cylinder,
                $"{city.Definition.displayName} arena floor",
                transform,
                new Vector3(0f, 0.04f, 0f),
                new Vector3(13f, 0.12f, 13f),
                palette.plaza,
                true);

            for (var index = 0; index < 8; index++)
            {
                var angle = index * Mathf.PI * 2f / 8f;
                var position = new Vector3(
                    Mathf.Cos(angle) * 7.2f,
                    1.25f,
                    Mathf.Sin(angle) * 7.2f);
                CityInstitutionBuilder.CreatePrimitive(
                    PrimitiveType.Cylinder,
                    $"{city.Definition.displayName} arena post",
                    transform,
                    position,
                    new Vector3(0.12f, 1.25f, 0.12f),
                    palette.copper,
                    false);

                var nextAngle = (index + 1) * Mathf.PI * 2f / 8f;
                var nextPosition = new Vector3(
                    Mathf.Cos(nextAngle) * 7.2f,
                    1.25f,
                    Mathf.Sin(nextAngle) * 7.2f);
                for (var ropeLevel = 0; ropeLevel < 2; ropeLevel++)
                {
                    var rope = CityInstitutionBuilder.CreatePrimitive(
                        PrimitiveType.Cube,
                        $"{city.Definition.displayName} arena boundary braid",
                        transform,
                        (position + nextPosition) * 0.5f +
                        Vector3.up * (-0.45f + ropeLevel * 0.7f),
                        new Vector3(0.075f, 0.075f, Vector3.Distance(position, nextPosition)),
                        ropeLevel == 0 ? palette.timber : palette.canopy,
                        false);
                    rope.transform.localRotation = Quaternion.LookRotation(
                        nextPosition - position,
                        Vector3.up);
                }
            }

            for (var side = 0; side < 4; side += 2)
            {
                var angle = side * Mathf.PI * 0.5f;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                for (var tier = 0; tier < 3; tier++)
                {
                    var stand = CityInstitutionBuilder.CreatePrimitive(
                        PrimitiveType.Cube,
                        $"{city.Definition.displayName} arena stand",
                        transform,
                        outward * (9.2f + tier * 0.72f) +
                        Vector3.up * (0.32f + tier * 0.32f),
                        new Vector3(5.1f, 0.42f, 1.25f),
                        tier % 2 == 0 ? palette.timber : palette.stone,
                        false);
                    stand.transform.localRotation = Quaternion.Euler(
                        0f,
                        -side * 90f + 90f,
                        0f);
                }
            }

            for (var gate = -1; gate <= 1; gate += 2)
            {
                for (var side = -1; side <= 1; side += 2)
                {
                    CityInstitutionBuilder.CreatePrimitive(
                        PrimitiveType.Cylinder,
                        $"{city.Definition.displayName} arena gate post",
                        transform,
                        new Vector3(side * 1.7f, 1.7f, gate * 7.75f),
                        new Vector3(0.15f, 1.7f, 0.15f),
                        palette.copper,
                        false);
                }

                CityInstitutionBuilder.CreatePrimitive(
                    PrimitiveType.Cube,
                    $"{city.Definition.displayName} arena gate lintel",
                    transform,
                    new Vector3(0f, 3.35f, gate * 7.75f),
                    new Vector3(3.7f, 0.18f, 0.28f),
                    palette.marker,
                    false);
            }

            for (var side = -1; side <= 1; side += 2)
            {
                CityInstitutionBuilder.CreatePrimitive(
                    PrimitiveType.Cylinder,
                    $"{city.Definition.displayName} arena score totem",
                    transform,
                    new Vector3(side * 8.25f, 1.55f, -6.2f),
                    new Vector3(0.34f, 1.55f, 0.34f),
                    palette.stone,
                    false);
                CityInstitutionBuilder.CreatePrimitive(
                    PrimitiveType.Sphere,
                    $"{city.Definition.displayName} arena score marker",
                    transform,
                    new Vector3(side * 8.25f, 3.18f, -6.2f),
                    Vector3.one * 0.42f,
                    palette.marker,
                    false);
            }
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null && city != null && configuredActivity != null;
        }

        public void Interact(TetherfallGame owner)
        {
            if (!CanInteract(owner))
            {
                return;
            }

            if (activeActivity != null)
            {
                owner.Notify($"{activeActivity.title} is underway. {city.GetTrainingStatusText()}.");
                return;
            }

            owner.TryStartCityActivity(city, configuredActivity);
        }
    }

    public sealed class CityBannerMotion : MonoBehaviour
    {
        private float phase;
        private Vector3 baseScale;

        public void Initialize(float seed)
        {
            phase = seed;
            baseScale = transform.localScale;
        }

        private void Update()
        {
            if (baseScale == Vector3.zero)
            {
                baseScale = transform.localScale;
            }

            transform.localScale = new Vector3(
                baseScale.x * (1f + Mathf.Sin(Time.time * 1.8f + phase) * 0.06f),
                baseScale.y,
                baseScale.z);
        }
    }

    public sealed class CityLifeLandmark : MonoBehaviour, IWorldInteractable
    {
        private TetherfallGame game;
        private CityDirector city;
        private CityActivityDefinition linkedActivity;
        private string prompt;
        private string settlingPrompt;
        private string actionText;
        private float rotationSpeed;
        private Vector3 rotationAxis;
        private Transform[] rotors = Array.Empty<Transform>();
        private Transform[] bobbers = Array.Empty<Transform>();
        private Transform[] pulses = Array.Empty<Transform>();
        private Renderer[] stateRenderers = Array.Empty<Renderer>();
        private Quaternion[] rotorBaseRotations = Array.Empty<Quaternion>();
        private Vector3[] bobberBasePositions = Array.Empty<Vector3>();
        private Vector3[] pulseBaseScales = Array.Empty<Vector3>();
        private float nextStateRefreshAt;
        private float nextInteractionAt;
        private float flourishUntil;
        private bool linkedActivityReady;
        private int completionCount;

        public string InteractionPrompt
        {
            get
            {
                if (string.IsNullOrEmpty(prompt))
                {
                    return "Inspect city landmark";
                }

                return nextInteractionAt > Time.time ? settlingPrompt : prompt;
            }
        }

        public void Initialize(
            TetherfallGame owner,
            CityDirector director,
            CityActivityDefinition activity,
            string interactionPrompt,
            string interactionAction,
            float spinSpeed,
            Vector3 spinAxis,
            Transform[] rotatingParts,
            Transform[] bobbingParts,
            Transform[] pulsingParts,
            Renderer[] reactiveRenderers)
        {
            game = owner;
            city = director;
            linkedActivity = activity;
            prompt = interactionPrompt;
            settlingPrompt = $"{interactionPrompt} · mechanism settling";
            actionText = interactionAction;
            rotationSpeed = spinSpeed;
            rotationAxis = spinAxis.sqrMagnitude > 0.01f ? spinAxis.normalized : Vector3.up;
            rotors = rotatingParts ?? Array.Empty<Transform>();
            bobbers = bobbingParts ?? Array.Empty<Transform>();
            pulses = pulsingParts ?? Array.Empty<Transform>();
            stateRenderers = reactiveRenderers ?? Array.Empty<Renderer>();
            rotorBaseRotations = new Quaternion[rotors.Length];
            for (var index = 0; index < rotors.Length; index++)
            {
                rotorBaseRotations[index] = rotors[index] != null
                    ? rotors[index].localRotation
                    : Quaternion.identity;
            }

            bobberBasePositions = new Vector3[bobbers.Length];
            for (var index = 0; index < bobbers.Length; index++)
            {
                bobberBasePositions[index] = bobbers[index] != null
                    ? bobbers[index].localPosition
                    : Vector3.zero;
            }

            pulseBaseScales = new Vector3[pulses.Length];
            for (var index = 0; index < pulses.Length; index++)
            {
                pulseBaseScales[index] = pulses[index] != null
                    ? pulses[index].localScale
                    : Vector3.one;
            }

            RefreshReactiveState();
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            if (Time.time >= nextStateRefreshAt)
            {
                RefreshReactiveState();
            }

            if (game.GameplayPaused ||
                game.PlayerTransform == null ||
                (game.PlayerTransform.position - transform.position).sqrMagnitude > 7225f)
            {
                return;
            }

            var flourish = Time.time < flourishUntil ? 2.15f : 1f;
            var readiness = linkedActivityReady ? 1.15f : 0.82f;
            for (var index = 0; index < rotors.Length; index++)
            {
                if (rotors[index] != null)
                {
                    rotors[index].Rotate(
                        rotationAxis,
                        rotationSpeed * readiness * flourish * Time.deltaTime,
                        Space.Self);
                }
            }

            for (var index = 0; index < bobbers.Length; index++)
            {
                if (bobbers[index] != null)
                {
                    var offset = Mathf.Sin(Time.time * 1.45f + index * 1.73f) *
                                 0.07f *
                                 flourish;
                    bobbers[index].localPosition = bobberBasePositions[index] + Vector3.up * offset;
                }
            }

            for (var index = 0; index < pulses.Length; index++)
            {
                if (pulses[index] != null)
                {
                    var pulse = 1f + Mathf.Sin(Time.time * 2.25f + index * 0.81f) *
                                0.055f *
                                flourish;
                    pulses[index].localScale = pulseBaseScales[index] * pulse;
                }
            }
        }

        private void RefreshReactiveState()
        {
            nextStateRefreshAt = Time.time + 0.75f;
            completionCount = game != null
                ? game.GetCityActivityCompletionCount(linkedActivity)
                : 0;
            linkedActivityReady = game != null &&
                                  linkedActivity != null &&
                                  game.IsCityActivityAvailable(linkedActivity);
            var visibleStates = linkedActivity != null
                ? Mathf.Clamp(completionCount, 0, stateRenderers.Length)
                : stateRenderers.Length;
            for (var index = 0; index < stateRenderers.Length; index++)
            {
                if (stateRenderers[index] != null)
                {
                    stateRenderers[index].enabled = index < visibleStates;
                }
            }
        }

        public bool CanInteract(TetherfallGame owner)
        {
            return owner != null && owner == game && city != null;
        }

        public void Interact(TetherfallGame owner)
        {
            if (!CanInteract(owner))
            {
                return;
            }

            var remaining = nextInteractionAt - Time.time;
            if (remaining > 0f)
            {
                owner.Notify(
                    $"{city.Definition.displayName}'s mechanism is still settling " +
                    $"({Mathf.CeilToInt(remaining)} sec).");
                return;
            }

            nextInteractionAt = Time.time + 30f;
            flourishUntil = Time.time + 2.8f;
            RefreshReactiveState();
            var restoredStamina = Mathf.Min(8f, Mathf.Max(0f, owner.MaxStamina - owner.Stamina));
            if (restoredStamina > 0f)
            {
                owner.RestoreStamina(restoredStamina);
            }

            var activityStatus = linkedActivity == null
                ? "The city rhythm holds steady."
                : linkedActivityReady
                    ? $"{linkedActivity.title} is ready at the civic board."
                    : $"{linkedActivity.title} returns {owner.GetCityActivityCooldownText(linkedActivity)}.";
            var history = completionCount > 0
                ? $" Civic response: {completionCount} completed."
                : " The first civic response is still waiting.";
            var staminaText = restoredStamina > 0f
                ? $" +{Mathf.CeilToInt(restoredStamina)} stamina."
                : string.Empty;
            owner.Notify($"{actionText} {activityStatus}{history}{staminaText}", 6.2f);
        }

        public void RestoreRuntimeState()
        {
            nextInteractionAt = 0f;
            flourishUntil = 0f;
            for (var index = 0; index < rotors.Length; index++)
            {
                if (rotors[index] != null)
                {
                    rotors[index].localRotation = rotorBaseRotations[index];
                }
            }

            for (var index = 0; index < bobbers.Length; index++)
            {
                if (bobbers[index] != null)
                {
                    bobbers[index].localPosition = bobberBasePositions[index];
                }
            }

            for (var index = 0; index < pulses.Length; index++)
            {
                if (pulses[index] != null)
                {
                    pulses[index].localScale = pulseBaseScales[index];
                }
            }

            RefreshReactiveState();
        }
    }

    public sealed class CityLightCluster : MonoBehaviour
    {
        private TetherfallGame game;
        private Vector3 cityCenter;
        private Light[] lights = Array.Empty<Light>();
        private float[] baseIntensities = Array.Empty<float>();
        private Vector3[] baseScales = Array.Empty<Vector3>();
        private float[] phases = Array.Empty<float>();

        public void Initialize(TetherfallGame owner, Vector3 center, List<Light> sources)
        {
            game = owner;
            cityCenter = center;
            var count = sources != null ? sources.Count : 0;
            lights = new Light[count];
            baseIntensities = new float[count];
            baseScales = new Vector3[count];
            phases = new float[count];
            for (var index = 0; index < count; index++)
            {
                var source = sources[index];
                lights[index] = source;
                baseIntensities[index] = source != null ? source.intensity : 0f;
                baseScales[index] = source != null ? source.transform.localScale : Vector3.one;
                phases[index] = source != null
                    ? source.transform.position.x * 0.17f + source.transform.position.z * 0.11f
                    : index * 0.73f;
            }
        }

        private void Update()
        {
            if (game == null ||
                game.PlayerTransform == null ||
                (game.PlayerTransform.position - cityCenter).sqrMagnitude > 8100f)
            {
                return;
            }

            for (var index = 0; index < lights.Length; index++)
            {
                var source = lights[index];
                if (source == null)
                {
                    continue;
                }

                var slowPulse = Mathf.Sin(Time.time * 1.7f + phases[index]) * 0.065f;
                var quickPulse = Mathf.Sin(Time.time * 4.3f + phases[index] * 1.9f) * 0.018f;
                source.intensity = Mathf.Max(
                    0f,
                    baseIntensities[index] * (1f + slowPulse + quickPulse));
                source.transform.localScale = baseScales[index] * (1f + slowPulse * 0.32f);
            }
        }
    }

    public static class CityVisualBuilder
    {
        private static readonly Dictionary<string, Material> CrestMaterials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Material> WayfindingMaterials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, CityArtPalette> Palettes = new Dictionary<string, CityArtPalette>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCachedArt()
        {
            CrestMaterials.Clear();
            WayfindingMaterials.Clear();
            Palettes.Clear();
        }

        public static CityArtPalette GetPalette(TetherfallGame game, CityDefinition definition)
        {
            if (definition != null &&
                Palettes.TryGetValue(definition.id, out var cached) &&
                cached != null)
            {
                return cached;
            }

            var palette = new CityArtPalette(game, definition);
            if (definition != null)
            {
                Palettes[definition.id] = palette;
            }

            return palette;
        }

        public static Material CreateWayfindingMaterial(TetherfallGame game, CityDefinition definition)
        {
            var cityId = definition != null ? definition.id : "city";
            if (WayfindingMaterials.TryGetValue(cityId, out var cached))
            {
                return cached;
            }

            const int size = 128;
            var harbor = definition != null && definition.id == CityCatalog.VeyrwaterMooring.id;
            var accent = definition != null ? definition.accentColor : new Color(0.45f, 0.78f, 0.7f);
            var ink = harbor
                ? new Color(0.035f, 0.09f, 0.12f, 1f)
                : new Color(0.07f, 0.1f, 0.07f, 1f);
            var copper = harbor
                ? new Color(0.36f, 0.57f, 0.54f, 1f)
                : new Color(0.78f, 0.36f, 0.15f, 1f);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"{definition?.displayName ?? "City"} wayfinding texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var centerX = x - (size - 1) * 0.5f;
                    var centerY = y - (size - 1) * 0.5f;
                    var border = Mathf.Abs(centerX) > 55f || Mathf.Abs(centerY) > 48f;
                    var pixel = border ? copper : ink;
                    if (harbor)
                    {
                        var channel = Mathf.Abs(centerY + Mathf.Sin(centerX * 0.16f) * 8f) < 3.2f;
                        var reed = (Mathf.Abs(centerX - 31f) < 2f && centerY > -30f && centerY < 31f) ||
                                   (Mathf.Abs(centerX + 29f) < 2f && centerY > -25f && centerY < 27f);
                        if (channel)
                        {
                            pixel = Color.Lerp(accent, new Color(0.55f, 0.96f, 0.86f, 1f), 0.48f);
                        }
                        else if (reed)
                        {
                            pixel = new Color(0.51f, 0.68f, 0.38f, 1f);
                        }
                    }
                    else
                    {
                        var arch = Mathf.Abs(Mathf.Abs(centerX) - 28f) < 3f && centerY > -30f && centerY < 21f ||
                                   Mathf.Abs(centerY - 21f) < 3f && Mathf.Abs(centerX) < 30f;
                        var lantern = Mathf.Abs(centerX) < 9f && centerY > -14f && centerY < 13f;
                        if (arch)
                        {
                            pixel = copper;
                        }
                        else if (lantern)
                        {
                            pixel = Color.Lerp(accent, new Color(0.94f, 0.82f, 0.42f, 1f), 0.52f);
                        }
                    }

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply(false, true);
            var material = game.CreateMaterial(Color.white, 0.1f, 0.48f);
            material.mainTexture = texture;
            WayfindingMaterials[cityId] = material;
            return material;
        }

        public sealed class CityArtPalette
        {
            public readonly Material road;
            public readonly Material plaza;
            public readonly Material wall;
            public readonly Material roof;
            public readonly Material timber;
            public readonly Material stone;
            public readonly Material copper;
            public readonly Material canopy;
            public readonly Material window;
            public readonly Material lantern;
            public readonly Material marker;
            public readonly Material water;
            public readonly Color waterColor;

            public CityArtPalette(TetherfallGame game, CityDefinition definition)
            {
                var harbor = definition != null && definition.id == CityCatalog.VeyrwaterMooring.id;
                var roadColor = harbor ? new Color(0.13f, 0.25f, 0.27f) : new Color(0.27f, 0.3f, 0.24f);
                var plazaColor = harbor ? new Color(0.18f, 0.31f, 0.3f) : new Color(0.34f, 0.35f, 0.28f);
                var wallColor = harbor ? new Color(0.22f, 0.35f, 0.33f) : new Color(0.35f, 0.39f, 0.29f);
                var roofColor = harbor ? new Color(0.15f, 0.26f, 0.29f) : new Color(0.37f, 0.2f, 0.12f);
                var timberColor = harbor ? new Color(0.16f, 0.11f, 0.075f) : new Color(0.24f, 0.14f, 0.08f);
                var stoneColor = harbor ? new Color(0.22f, 0.29f, 0.3f) : new Color(0.28f, 0.25f, 0.19f);
                var copperColor = harbor ? new Color(0.22f, 0.52f, 0.49f) : new Color(0.72f, 0.33f, 0.14f);
                var canopyColor = harbor ? new Color(0.2f, 0.47f, 0.42f) : new Color(0.49f, 0.43f, 0.23f);
                var glowColor = harbor ? new Color(0.38f, 0.86f, 0.79f) : new Color(1f, 0.62f, 0.32f);
                waterColor = new Color(0.08f, 0.32f, 0.39f, 0.76f);

                road = game.CreateMaterial(roadColor, 0.08f, 0.32f);
                plaza = game.CreateMaterial(plazaColor, 0.1f, 0.36f);
                wall = game.CreateMaterial(wallColor, 0.04f, 0.28f);
                roof = game.CreateMaterial(roofColor, 0.16f, 0.46f);
                timber = game.CreateMaterial(timberColor, 0.04f, 0.22f);
                stone = game.CreateMaterial(stoneColor, 0.08f, 0.28f);
                copper = game.CreateMaterial(copperColor, 0.32f, 0.56f);
                canopy = game.CreateMaterial(canopyColor, 0.12f, 0.45f);
                lantern = game.CreateGlowMaterial(glowColor, 1.72f, 0.7f);
                window = lantern;
                marker = lantern;
                water = game.CreateTransparentMaterial(waterColor, 0.52f);
            }
        }

        public static Material CreateCrestMaterial(TetherfallGame game, Color accent, string crestId)
        {
            if (CrestMaterials.TryGetValue(crestId, out var cached))
            {
                return cached;
            }

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Hearthmarket procedural crest",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var harborCrest = crestId == CityCatalog.VeyrwaterMooring.id;
            var deepInk = harborCrest
                ? new Color(0.035f, 0.07f, 0.11f, 1f)
                : new Color(0.055f, 0.09f, 0.08f, 1f);
            var copper = new Color(0.78f, 0.38f, 0.16f, 1f);
            var lantern = Color.Lerp(accent, new Color(0.86f, 0.96f, 0.63f), 0.54f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var p = new Vector2(x - 63.5f, y - 63.5f);
                    var radial = Mathf.Clamp01(p.magnitude / 90f);
                    var pixel = Color.Lerp(deepInk, new Color(0.12f, 0.17f, 0.13f, 1f), 1f - radial);
                    var ringDistance = Mathf.Abs(p.magnitude - 43f);
                    if (ringDistance < 3.2f)
                    {
                        pixel = copper;
                    }

                    var lanternBody = Mathf.Abs(p.x) < 16f && p.y > -25f && p.y < 22f;
                    var lanternRoof = p.y >= 21f && p.y < 35f && Mathf.Abs(p.x) < (35f - p.y) * 1.15f;
                    var lanternLight = Mathf.Abs(p.x) < 9f && p.y > -15f && p.y < 14f;
                    var reed = (Mathf.Abs(p.x - 24f) < 2.1f && p.y > -25f && p.y < 32f) ||
                               (Mathf.Abs(p.x + 25f) < 2.1f && p.y > -25f && p.y < 23f);
                    var current = harborCrest &&
                                  p.y < -17f &&
                                  Mathf.Abs(p.y + 28f - Mathf.Sin(p.x * 0.17f) * 4f) < 1.8f;
                    if (lanternBody || lanternRoof)
                    {
                        pixel = copper;
                    }

                    if (lanternLight)
                    {
                        pixel = lantern;
                    }

                    if (reed)
                    {
                        pixel = Color.Lerp(accent, new Color(0.63f, 0.85f, 0.46f), 0.38f);
                    }

                    if (current)
                    {
                        pixel = Color.Lerp(accent, new Color(0.46f, 0.92f, 0.9f), 0.38f);
                    }

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply(false, true);
            var material = game.CreateMaterial(Color.white, 0.12f, 0.52f);
            material.mainTexture = texture;
            CrestMaterials[crestId] = material;
            return material;
        }

        public static void DressResident(TetherfallGame game, Transform root, Color accent, string role)
        {
            var sash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sash.name = "Resident sash";
            sash.transform.SetParent(root, false);
            sash.transform.localPosition = new Vector3(0f, 0.38f, -0.36f);
            sash.transform.localScale = new Vector3(0.56f, 0.18f, 0.08f);
            sash.GetComponent<Renderer>().material = game.CreateMaterial(Color.Lerp(accent, Color.white, 0.25f), 0.1f, 0.5f);
            UnityEngine.Object.Destroy(sash.GetComponent<Collider>());

            var tool = GameObject.CreatePrimitive(role.Contains("marshal") ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            tool.name = "Resident role tool";
            tool.transform.SetParent(root, false);
            tool.transform.localPosition = new Vector3(0.36f, 0.12f, 0.12f);
            tool.transform.localRotation = Quaternion.Euler(20f, 0f, 28f);
            tool.transform.localScale = role.Contains("marshal")
                ? new Vector3(0.07f, 0.58f, 0.07f)
                : new Vector3(0.14f, 0.42f, 0.18f);
            tool.GetComponent<Renderer>().material = game.CreateMaterial(new Color(0.44f, 0.25f, 0.12f), 0.12f, 0.4f);
            UnityEngine.Object.Destroy(tool.GetComponent<Collider>());
        }
    }
}
