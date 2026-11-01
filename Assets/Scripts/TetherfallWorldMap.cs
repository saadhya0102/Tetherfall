using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public struct WorldMapTravelQuote
    {
        public string SourceId { get; internal set; }
        public string DestinationId { get; internal set; }
        public float DistanceMeters { get; internal set; }
        public int ChimesaltCost { get; internal set; }
        public bool WayfinderReady { get; internal set; }
        public bool SourceCurrent { get; internal set; }
        public bool SourceRestored { get; internal set; }
        public bool DestinationRestored { get; internal set; }
        public bool SourceHostiles { get; internal set; }
        public bool DestinationHostiles { get; internal set; }
        public bool ArrivalFinite { get; internal set; }
        public bool HasEnoughChimesalt { get; internal set; }
        public bool CanTravel { get; internal set; }
        public string DisabledReason { get; internal set; }
    }

    public static class TetherfallWorldMapRules
    {
        public const float MinimumWorldX = TetherfallRegionPacks.WorldMinimumX;
        public const float MaximumWorldX = TetherfallRegionPacks.WorldMaximumX;
        public const float MinimumWorldZ = TetherfallRegionPacks.WorldMinimumZ;
        public const float MaximumWorldZ = TetherfallRegionPacks.WorldMaximumZ;

        public const string SelectDestinationDisabledReason = "Select a Threadway destination.";
        public const string WayfinderUnavailableDisabledReason = "The Wayfinder is not ready to travel.";
        public const string MissingSourceDisabledReason = "Travel must begin at a restored Threadway.";
        public const string SourceNotCurrentDisabledReason = "Travel must begin at the current Threadway.";
        public const string CurrentDestinationDisabledReason = "You are already at this Threadway.";
        public const string SourceNotRestoredDisabledReason = "Restore the current Threadway before travelling.";
        public const string DestinationNotRestoredDisabledReason = "Restore this Threadway before travelling.";
        public const string SourceHostilesDisabledReason = "Hostiles are too close to begin travel.";
        public const string DestinationHostilesDisabledReason = "Hostiles are occupying the arrival route.";
        public const string NonFiniteSourceDisabledReason = "The current Threadway has no finite route origin.";
        public const string NonFiniteArrivalDisabledReason = "The destination has no finite arrival point.";

        public static WorldMapTravelQuote CreateTravelQuote(
            ThreadwayDefinition source,
            ThreadwayDefinition destination,
            bool wayfinderReady,
            bool sourceCurrent,
            bool sourceRestored,
            bool destinationRestored,
            bool sourceHostiles,
            bool destinationHostiles,
            int availableChimesalt)
        {
            var sourceFinite = source != null && IsFinite(source.arrivalPosition);
            var arrivalFinite = destination != null && IsFinite(destination.arrivalPosition);
            var distance = sourceFinite && arrivalFinite
                ? HorizontalDistance(source.arrivalPosition, destination.arrivalPosition)
                : 0f;
            var cost = sourceFinite && arrivalFinite ? GetTravelChimesaltCost(distance) : 0;
            var hasEnoughChimesalt = Mathf.Max(0, availableChimesalt) >= cost;
            var quote = new WorldMapTravelQuote
            {
                SourceId = source != null ? source.id : string.Empty,
                DestinationId = destination != null ? destination.id : string.Empty,
                DistanceMeters = distance,
                ChimesaltCost = cost,
                WayfinderReady = wayfinderReady,
                SourceCurrent = sourceCurrent,
                SourceRestored = sourceRestored,
                DestinationRestored = destinationRestored,
                SourceHostiles = sourceHostiles,
                DestinationHostiles = destinationHostiles,
                ArrivalFinite = arrivalFinite,
                HasEnoughChimesalt = hasEnoughChimesalt,
                CanTravel = false,
                DisabledReason = string.Empty
            };

            if (destination == null)
            {
                quote.DisabledReason = SelectDestinationDisabledReason;
            }
            else if (!wayfinderReady)
            {
                quote.DisabledReason = WayfinderUnavailableDisabledReason;
            }
            else if (source == null)
            {
                quote.DisabledReason = MissingSourceDisabledReason;
            }
            else if (!sourceCurrent)
            {
                quote.DisabledReason = SourceNotCurrentDisabledReason;
            }
            else if (!sourceFinite)
            {
                quote.DisabledReason = NonFiniteSourceDisabledReason;
            }
            else if (!arrivalFinite)
            {
                quote.DisabledReason = NonFiniteArrivalDisabledReason;
            }
            else if (string.Equals(source.id, destination.id, StringComparison.Ordinal))
            {
                quote.DisabledReason = CurrentDestinationDisabledReason;
            }
            else if (!sourceRestored)
            {
                quote.DisabledReason = SourceNotRestoredDisabledReason;
            }
            else if (!destinationRestored)
            {
                quote.DisabledReason = DestinationNotRestoredDisabledReason;
            }
            else if (sourceHostiles)
            {
                quote.DisabledReason = SourceHostilesDisabledReason;
            }
            else if (destinationHostiles)
            {
                quote.DisabledReason = DestinationHostilesDisabledReason;
            }
            else if (!hasEnoughChimesalt)
            {
                quote.DisabledReason = GetMissingChimesaltReason(cost - Mathf.Max(0, availableChimesalt));
            }
            else
            {
                quote.CanTravel = true;
            }

            return quote;
        }

        public static int GetTravelChimesaltCost(float distanceMeters)
        {
            if (!IsFinite(distanceMeters) || distanceMeters < 0f)
            {
                return 2;
            }

            if (distanceMeters < 80f)
            {
                return 0;
            }

            return distanceMeters < 160f ? 1 : 2;
        }

        public static int GetTravelChimesaltCost(Vector3 source, Vector3 destination)
        {
            if (!IsFinite(source) || !IsFinite(destination))
            {
                return 2;
            }

            return GetTravelChimesaltCost(HorizontalDistance(source, destination));
        }

        public static float HorizontalDistance(Vector3 source, Vector3 destination)
        {
            var deltaX = destination.x - source.x;
            var deltaZ = destination.z - source.z;
            return Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        public static bool TryProjectWorldToMap(Vector3 worldPosition, Rect mapRect, out Vector2 mapPosition)
        {
            mapPosition = new Vector2(
                IsFinite(mapRect.x) && IsFinite(mapRect.width) ? mapRect.center.x : 0f,
                IsFinite(mapRect.y) && IsFinite(mapRect.height) ? mapRect.center.y : 0f);
            if (!IsFinite(worldPosition) ||
                !IsFinite(mapRect.x) ||
                !IsFinite(mapRect.y) ||
                !IsFinite(mapRect.width) ||
                !IsFinite(mapRect.height) ||
                mapRect.width <= 0f ||
                mapRect.height <= 0f)
            {
                return false;
            }

            var normalizedX = Mathf.InverseLerp(MinimumWorldX, MaximumWorldX, worldPosition.x);
            var normalizedZ = Mathf.InverseLerp(MinimumWorldZ, MaximumWorldZ, worldPosition.z);
            mapPosition = new Vector2(
                Mathf.Lerp(mapRect.xMin, mapRect.xMax, normalizedX),
                Mathf.Lerp(mapRect.yMax, mapRect.yMin, normalizedZ));
            return IsFinite(mapPosition.x) && IsFinite(mapPosition.y);
        }

        public static Vector2 ProjectWorldToMap(Vector3 worldPosition, Rect mapRect)
        {
            TryProjectWorldToMap(worldPosition, mapRect, out var mapPosition);
            return mapPosition;
        }

        public static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string GetMissingChimesaltReason(int missing)
        {
            return $"Need {Mathf.Max(1, missing)} more Chimesalt.";
        }
    }

    public sealed partial class TetherfallGame
    {
        private enum WorldMapMarkerKind
        {
            Player,
            Objective,
            City,
            EnemyCamp,
            PortableCamp,
            Dungeon,
            Threadway
        }

        private enum WorldMapMarkerTone
        {
            Muted,
            Parchment,
            Copper,
            Teal,
            Danger
        }

        private struct WorldMapMarker
        {
            public string Id;
            public string Name;
            public string Meta;
            public string State;
            public string Description;
            public string Coordinates;
            public Vector3 WorldPosition;
            public WorldMapMarkerKind Kind;
            public WorldMapMarkerTone Tone;
            public bool Discovered;
            public bool Restored;
        }

        private const float WorldMapMarkerRefreshSeconds = 0.35f;
        private const float ThreadwayDiscoveryRefreshSeconds = 0.4f;
        private const float ThreadwayDiscoveryRadius = 20f;
        private const float WorldMapCurrentSourceRange = 9f;
        private const float WorldMapHostileRange = 18f;
        private const float ArrivalCastHeight = 8f;
        private const float ArrivalCastDistance = 16f;
        private const float ArrivalMaximumGroundDelta = 0.9f;
        private const float ArrivalMinimumGroundNormalY = 0.56f;
        private const float ArrivalGroundClearance = 0.08f;
        private const int ArrivalQueryCapacity = 48;
        private const int ArrivalFallbackRings = 4;
        private const int ArrivalFallbackSamples = 8;
        private const float ArrivalFallbackStep = 1.5f;

        private static readonly Color WorldMapInk = new Color(0.012f, 0.031f, 0.039f, 0.98f);
        private static readonly Color WorldMapSlate = new Color(0.051f, 0.11f, 0.141f, 0.98f);
        private static readonly Color WorldMapCopper = new Color(0.769f, 0.471f, 0.22f, 1f);
        private static readonly Color WorldMapTeal = new Color(0.29f, 0.788f, 0.741f, 1f);
        private static readonly Color WorldMapParchment = new Color(0.941f, 0.878f, 0.729f, 1f);
        private static readonly Color WorldMapMist = new Color(0.839f, 0.91f, 0.902f, 1f);
        private static readonly Color WorldMapMuted = new Color(0.588f, 0.71f, 0.722f, 1f);
        private static readonly Color WorldMapDanger = new Color(0.78f, 0.271f, 0.22f, 1f);
        private static readonly Vector3 AuthoredSkyvaultGatePosition = new Vector3(121f, 0.9f, 31f);

        private readonly HashSet<string> discoveredThreadwayIds =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly List<WorldMapMarker> worldMapMarkers = new List<WorldMapMarker>(80);
        private readonly RaycastHit[] worldMapArrivalCastBuffer = new RaycastHit[ArrivalQueryCapacity];
        private readonly Collider[] worldMapArrivalOverlapBuffer = new Collider[ArrivalQueryCapacity];
        private readonly GUIContent worldMapMarkerLabelContent = new GUIContent();
        private readonly GUIContent worldMapButtonContent = new GUIContent();

        private GUIStyle worldMapTitleStyle;
        private GUIStyle worldMapBodyStyle;
        private GUIStyle worldMapMutedStyle;
        private GUIStyle worldMapLabelStyle;
        private GUIStyle worldMapButtonTextStyle;
        private GUIStyle worldMapInvisibleButtonStyle;
        private GUIStyle worldMapTealStatusStyle;
        private GUIStyle worldMapCopperStatusStyle;
        private GUIStyle worldMapDangerStatusStyle;
        private GUIStyle worldMapParchmentStatusStyle;
        private string selectedWorldMapMarkerId;
        private int selectedWorldMapMarkerIndex = -1;
        private int worldMapLastDrawFrame = -100;
        private int worldMapLastInputFrame = -100;
        private int worldMapLastConfirmFrame = -100;
        private float nextWorldMapMarkerRefreshAt;
        private float nextThreadwayDiscoveryAt;
        private bool worldMapUiPrepared;
        private WorldMapTravelQuote cachedWorldMapTravelQuote;
        private string cachedWorldMapRouteText = string.Empty;
        private string cachedWorldMapCostText = string.Empty;
        private string cachedWorldMapTravelStatus = TetherfallWorldMapRules.SelectDestinationDisabledReason;

        public string SelectedWorldMapMarkerId =>
            string.IsNullOrEmpty(selectedWorldMapMarkerId) ? string.Empty : selectedWorldMapMarkerId;
        public bool CanConfirmWorldMapTravel => cachedWorldMapTravelQuote.CanTravel;
        public string WorldMapTravelDisabledReason =>
            cachedWorldMapTravelQuote.CanTravel
                ? string.Empty
                : string.IsNullOrEmpty(cachedWorldMapTravelQuote.DisabledReason)
                    ? TetherfallWorldMapRules.SelectDestinationDisabledReason
                    : cachedWorldMapTravelQuote.DisabledReason;

        public bool IsThreadwayDiscovered(string id)
        {
            return !string.IsNullOrEmpty(id) && discoveredThreadwayIds.Contains(id);
        }

        public bool DiscoverThreadway(string id)
        {
            var definition = string.IsNullOrEmpty(id) ? null : ThreadwayCatalog.Get(id);
            if (definition == null || !discoveredThreadwayIds.Add(definition.id))
            {
                return false;
            }

            nextWorldMapMarkerRefreshAt = 0f;
            Notify($"Threadway charted: {definition.title}.");
            RequestSave();
            return true;
        }

        public void CaptureDiscoveredThreadwayIds(List<string> destination)
        {
            if (destination == null)
            {
                return;
            }

            SeedWorldMapDiscoveryFromRestoredThreadways();
            destination.Clear();
            foreach (var id in discoveredThreadwayIds)
            {
                destination.Add(id);
            }

            destination.Sort(StringComparer.Ordinal);
        }

        public void RestoreDiscoveredThreadwayIds(IEnumerable<string> savedIds)
        {
            discoveredThreadwayIds.Clear();
            if (savedIds != null)
            {
                foreach (var id in savedIds)
                {
                    if (string.IsNullOrEmpty(id) || ThreadwayCatalog.Get(id) == null)
                    {
                        continue;
                    }

                    discoveredThreadwayIds.Add(id);
                }
            }

            SeedWorldMapDiscoveryFromRestoredThreadways();
            nextWorldMapMarkerRefreshAt = 0f;
        }

        public void SeedWorldMapDiscoveryFromRestoredThreadways()
        {
            foreach (var id in unlockedThreadwayIds)
            {
                if (!string.IsNullOrEmpty(id) && ThreadwayCatalog.Get(id) != null)
                {
                    discoveredThreadwayIds.Add(id);
                }
            }
        }

        public void UpdateThreadwayDiscoveryProximity()
        {
            if (Player == null || Time.unscaledTime < nextThreadwayDiscoveryAt)
            {
                return;
            }

            nextThreadwayDiscoveryAt = Time.unscaledTime + ThreadwayDiscoveryRefreshSeconds;
            var playerPosition = PlayerTransform.position;
            var discoveryDistanceSquared = ThreadwayDiscoveryRadius * ThreadwayDiscoveryRadius;
            foreach (var definition in ThreadwayCatalog.All)
            {
                if (definition == null ||
                    discoveredThreadwayIds.Contains(definition.id) ||
                    !TetherfallWorldMapRules.IsFinite(definition.position))
                {
                    continue;
                }

                var deltaX = definition.position.x - playerPosition.x;
                var deltaZ = definition.position.z - playerPosition.z;
                if (deltaX * deltaX + deltaZ * deltaZ <= discoveryDistanceSquared)
                {
                    DiscoverThreadway(definition.id);
                }
            }
        }

        public void OpenWorldMapUi()
        {
            worldMapUiPrepared = true;
            selectedWorldMapMarkerId =
                activeThreadway != null && activeThreadway.Definition != null
                    ? activeThreadway.Definition.id
                    : null;
            selectedWorldMapMarkerIndex = -1;
            nextWorldMapMarkerRefreshAt = 0f;
            RefreshWorldMapMarkers(true);
        }

        public void ResetWorldMapUi()
        {
            selectedWorldMapMarkerId = null;
            selectedWorldMapMarkerIndex = -1;
            worldMapUiPrepared = false;
            nextWorldMapMarkerRefreshAt = 0f;
            SetWorldMapNoSelectionQuote();
        }

        public bool CancelWorldMapSelection()
        {
            if (string.IsNullOrEmpty(selectedWorldMapMarkerId))
            {
                return false;
            }

            selectedWorldMapMarkerId = null;
            selectedWorldMapMarkerIndex = -1;
            SetWorldMapNoSelectionQuote();
            return true;
        }

        public bool SelectWorldMapMarker(string markerId)
        {
            if (string.IsNullOrEmpty(markerId))
            {
                return false;
            }

            RefreshWorldMapMarkers(false);
            var markerIndex = FindWorldMapMarkerIndex(markerId);
            if (markerIndex < 0)
            {
                return false;
            }

            selectedWorldMapMarkerId = markerId;
            selectedWorldMapMarkerIndex = markerIndex;
            RefreshCachedWorldMapQuote();
            return true;
        }

        public bool UpdateWorldMapUiInput()
        {
            if (activePanel != GamePanel.Threadways || worldMapLastInputFrame == Time.frameCount)
            {
                return false;
            }

            var key = KeyCode.None;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                key = KeyCode.Escape;
            }
            else if (Input.GetKeyDown(KeyCode.Delete))
            {
                key = KeyCode.Delete;
            }
            else if (Input.GetKeyDown(KeyCode.Backspace))
            {
                key = KeyCode.Backspace;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                key = KeyCode.LeftArrow;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                key = KeyCode.RightArrow;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                key = KeyCode.UpArrow;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                key = KeyCode.DownArrow;
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                key = KeyCode.Tab;
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                key = KeyCode.Return;
            }
            else if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                key = KeyCode.KeypadEnter;
            }

            if (key == KeyCode.None)
            {
                return false;
            }

            var handled = ProcessWorldMapKey(
                key,
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            if (handled)
            {
                worldMapLastInputFrame = Time.frameCount;
            }

            return handled;
        }

        public WorldMapTravelQuote GetWorldMapTravelQuote(string destinationId)
        {
            var destination = string.IsNullOrEmpty(destinationId)
                ? null
                : ThreadwayCatalog.Get(destinationId);
            var source = activeThreadway != null ? activeThreadway.Definition : null;
            var wayfinderReady = Player != null;
            var sourcePosition = source != null ? source.position : Vector3.zero;
            if (activeThreadway != null)
            {
                sourcePosition = activeThreadway.transform.position;
            }

            var sourceCurrent =
                wayfinderReady &&
                source != null &&
                TetherfallWorldMapRules.HorizontalDistance(PlayerTransform.position, sourcePosition) <=
                WorldMapCurrentSourceRange;
            var sourceHostiles =
                wayfinderReady && HasWorldMapHostilesAt(PlayerTransform.position, WorldMapHostileRange);
            var destinationHostiles =
                destination != null &&
                TetherfallWorldMapRules.IsFinite(destination.arrivalPosition) &&
                HasWorldMapHostilesAt(destination.arrivalPosition, WorldMapHostileRange);

            return TetherfallWorldMapRules.CreateTravelQuote(
                source,
                destination,
                wayfinderReady,
                sourceCurrent,
                source != null && IsThreadwayUnlocked(source.id),
                destination != null && IsThreadwayUnlocked(destination.id),
                sourceHostiles,
                destinationHostiles,
                GetResource(ResourceKind.Chimesalt));
        }

        public bool ConfirmWorldMapTravel()
        {
            if (worldMapLastConfirmFrame == Time.frameCount)
            {
                return false;
            }

            worldMapLastConfirmFrame = Time.frameCount;
            var markerIndex = FindWorldMapMarkerIndex(selectedWorldMapMarkerId);
            if (markerIndex < 0 || worldMapMarkers[markerIndex].Kind != WorldMapMarkerKind.Threadway)
            {
                Notify(TetherfallWorldMapRules.SelectDestinationDisabledReason);
                return false;
            }

            var destination = ThreadwayCatalog.Get(worldMapMarkers[markerIndex].Id);
            var quote = GetWorldMapTravelQuote(destination != null ? destination.id : null);
            cachedWorldMapTravelQuote = quote;
            RefreshCachedWorldMapQuoteText(destination);
            if (!quote.CanTravel)
            {
                Notify(quote.DisabledReason);
                return false;
            }

            if (!TryFindSafeWorldMapArrival(
                    destination.arrivalPosition,
                    out var safeArrival,
                    out var arrivalDisabledReason))
            {
                Notify(arrivalDisabledReason);
                return false;
            }

            if (quote.ChimesaltCost > 0 &&
                !SpendResources(0, 0, 0, quote.ChimesaltCost))
            {
                var refreshedQuote = GetWorldMapTravelQuote(destination.id);
                cachedWorldMapTravelQuote = refreshedQuote;
                RefreshCachedWorldMapQuoteText(destination);
                Notify(refreshedQuote.DisabledReason);
                return false;
            }

            var sourceAnchor = activeThreadway;
            if (sourceAnchor != null)
            {
                TetherfallWorldInteractionFeedback.PlayThreadwayTravel(
                    this,
                    sourceAnchor.transform,
                    safeArrival,
                    destination.accentColor);
            }

            TetherfallRegionalAnnexes.PrepareArrival(safeArrival);
            Player.Teleport(safeArrival);
            if (threadways.TryGetValue(destination.id, out var destinationAnchor) &&
                destinationAnchor != null)
            {
                activeThreadway = destinationAnchor;
            }

            RecallExpeditionToThreadway();
            ClearCombatTarget();
            ClosePanel();
            ResetWorldMapUi();
            Notify($"Threadway carried you to {destination.title}.");
            RequestCriticalSave();
            return true;
        }

        public bool TryFindSafeWorldMapArrival(
            Vector3 desiredArrival,
            out Vector3 safeArrival,
            out string disabledReason)
        {
            safeArrival = Vector3.zero;
            if (!TetherfallWorldMapRules.IsFinite(desiredArrival))
            {
                disabledReason = TetherfallWorldMapRules.NonFiniteArrivalDisabledReason;
                return false;
            }

            var foundGround = false;
            if (TryValidateWorldMapArrivalCandidate(
                    desiredArrival,
                    out safeArrival,
                    out var candidateFoundGround))
            {
                disabledReason = string.Empty;
                return true;
            }

            foundGround |= candidateFoundGround;
            for (var ring = 1; ring <= ArrivalFallbackRings; ring++)
            {
                var radius = ring * ArrivalFallbackStep;
                var phase = (ring & 1) == 0 ? Mathf.PI / ArrivalFallbackSamples : 0f;
                for (var sample = 0; sample < ArrivalFallbackSamples; sample++)
                {
                    var angle = sample * Mathf.PI * 2f / ArrivalFallbackSamples + phase;
                    var candidate = desiredArrival + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius);
                    if (TryValidateWorldMapArrivalCandidate(
                            candidate,
                            out safeArrival,
                            out candidateFoundGround))
                    {
                        disabledReason = string.Empty;
                        return true;
                    }

                    foundGround |= candidateFoundGround;
                }
            }

            disabledReason = foundGround
                ? "No clear arrival space was found near this Threadway."
                : "The destination opens over unstable ground.";
            return false;
        }

        public void DrawWorldMapPanel(Rect rect)
        {
            EnsureGuiStyles();
            EnsureWorldMapStyles();
            var reopened = !worldMapUiPrepared || Time.frameCount > worldMapLastDrawFrame + 1;
            worldMapLastDrawFrame = Time.frameCount;
            if (reopened)
            {
                OpenWorldMapUi();
            }
            else
            {
                RefreshWorldMapMarkers(false);
            }

            HandleWorldMapGuiKeyboard(Event.current);

            const float outerInset = 26f;
            const float contentTop = 66f;
            const float contentBottom = 44f;
            const float columnGap = 14f;
            var availableWidth = Mathf.Max(560f, rect.width - outerInset * 2f);
            var railWidth = Mathf.Clamp(availableWidth * 0.325f, 270f, 320f);
            var mapWidth = Mathf.Max(276f, availableWidth - railWidth - columnGap);
            var contentHeight = Mathf.Max(350f, rect.height - contentTop - contentBottom);
            var mapRect = new Rect(
                rect.x + outerInset,
                rect.y + contentTop,
                mapWidth,
                contentHeight);
            var railRect = new Rect(
                mapRect.xMax + columnGap,
                mapRect.y,
                railWidth,
                contentHeight);

            DrawWorldMapWell(mapRect);
            DrawWorldMapDetailRail(railRect);
        }

        private void RefreshWorldMapMarkers(bool force)
        {
            if (!force &&
                worldMapMarkers.Count > 0 &&
                Time.unscaledTime < nextWorldMapMarkerRefreshAt)
            {
                return;
            }

            nextWorldMapMarkerRefreshAt = Time.unscaledTime + WorldMapMarkerRefreshSeconds;
            SeedWorldMapDiscoveryFromRestoredThreadways();
            worldMapMarkers.Clear();

            if (Player != null && TetherfallWorldMapRules.IsFinite(PlayerTransform.position))
            {
                AddWorldMapMarker(
                    "map.player",
                    "Wayfinder",
                    "WAYFINDER",
                    CurrentRegion,
                    "CURRENT POSITION",
                    "Current expedition position.",
                    PlayerTransform.position,
                    WorldMapMarkerKind.Player,
                    WorldMapMarkerTone.Parchment,
                    true,
                    false);
            }

            if (objectiveTarget != null &&
                objectiveTarget.gameObject.activeInHierarchy &&
                TetherfallWorldMapRules.IsFinite(objectiveTarget.position))
            {
                AddWorldMapMarker(
                    "map.objective",
                    "Active objective",
                    "OBJECTIVE",
                    TetherfallRegionPacks.ResolveRegionName(objectiveTarget.position),
                    "ACTIVE OBJECTIVE",
                    GetObjective(),
                    objectiveTarget.position,
                    WorldMapMarkerKind.Objective,
                    WorldMapMarkerTone.Copper,
                    true,
                    false);
            }

            AddCityWorldMapMarker(CityCatalog.AlderwakeHearthmarket);
            AddCityWorldMapMarker(CityCatalog.VeyrwaterMooring);

            for (var index = 0; index < enemyCamps.Count; index++)
            {
                var camp = enemyCamps[index];
                if (camp == null || !TetherfallWorldMapRules.IsFinite(camp.transform.position))
                {
                    continue;
                }

                var hostilesPresent = HasWorldMapHostilesAt(camp.transform.position, 12f);
                AddWorldMapMarker(
                    string.IsNullOrEmpty(camp.PersistenceId)
                        ? $"map.enemycamp.{index}"
                        : $"map.enemycamp.{camp.PersistenceId}",
                    string.IsNullOrEmpty(camp.CampName) ? "Hostile camp" : camp.CampName,
                    "ENEMY CAMP",
                    TetherfallRegionPacks.ResolveRegionName(camp.transform.position),
                    hostilesPresent ? "HOSTILES ACTIVE" : camp.AwardedFirstClear ? "ROUTE CLEARED" : "CAMP QUIET",
                    hostilesPresent
                        ? "Scouts report active hostiles at this camp."
                        : "No active hostile signal is visible at this camp.",
                    camp.transform.position,
                    WorldMapMarkerKind.EnemyCamp,
                    hostilesPresent ? WorldMapMarkerTone.Danger : WorldMapMarkerTone.Muted,
                    true,
                    false);
            }

            if (ActiveCamp != null && TetherfallWorldMapRules.IsFinite(ActiveCamp.transform.position))
            {
                AddWorldMapMarker(
                    "map.portablecamp",
                    "Portable camp",
                    "FIELD REFUGE",
                    TetherfallRegionPacks.ResolveRegionName(ActiveCamp.transform.position),
                    "CAMP ESTABLISHED",
                    "The expedition's current field refuge.",
                    ActiveCamp.transform.position,
                    WorldMapMarkerKind.PortableCamp,
                    WorldMapMarkerTone.Copper,
                    true,
                    false);
            }

            if (skyvaultGate != null || skyvault != null)
            {
                var skyvaultPosition = skyvaultGate != null
                    ? skyvaultGate.transform.position
                    : AuthoredSkyvaultGatePosition;
                AddWorldMapMarker(
                    "map.skyvault",
                    "The Skyvault",
                    "DUNGEON",
                    "Morrowfen",
                    dungeonBossDefeated ? "ROUTE STABILIZED" : "VAULT ACCESS",
                    dungeonBossDefeated
                        ? "The buried route is stable."
                        : "A buried Threadglass vault and its sealed route.",
                    skyvaultPosition,
                    WorldMapMarkerKind.Dungeon,
                    dungeonBossDefeated ? WorldMapMarkerTone.Teal : WorldMapMarkerTone.Parchment,
                    true,
                    dungeonBossDefeated);
            }

            foreach (var definition in ThreadwayCatalog.All)
            {
                if (definition == null || !TetherfallWorldMapRules.IsFinite(definition.position))
                {
                    continue;
                }

                var restored = IsThreadwayUnlocked(definition.id);
                var discovered = restored || discoveredThreadwayIds.Contains(definition.id);
                var current =
                    activeThreadway != null &&
                    activeThreadway.Definition != null &&
                    string.Equals(activeThreadway.Definition.id, definition.id, StringComparison.Ordinal);
                AddWorldMapMarker(
                    definition.id,
                    discovered ? definition.title : "Uncharted Threadway",
                    "THREADWAY",
                    discovered ? definition.region : "Unsurveyed",
                    restored ? current ? "CURRENT THREADWAY" : "RESTORED" : discovered ? "DISCOVERED" : "UNCHARTED",
                    discovered ? definition.description : "A faint route signal with no recorded bearing.",
                    definition.position,
                    WorldMapMarkerKind.Threadway,
                    restored
                        ? WorldMapMarkerTone.Teal
                        : discovered
                            ? WorldMapMarkerTone.Copper
                            : WorldMapMarkerTone.Muted,
                    discovered,
                    restored);
            }

            selectedWorldMapMarkerIndex = FindWorldMapMarkerIndex(selectedWorldMapMarkerId);
            if (selectedWorldMapMarkerIndex < 0)
            {
                selectedWorldMapMarkerId = null;
            }

            RefreshCachedWorldMapQuote();
        }

        private void AddCityWorldMapMarker(CityDefinition city)
        {
            if (city == null || !TetherfallWorldMapRules.IsFinite(city.center))
            {
                return;
            }

            AddWorldMapMarker(
                city.id,
                city.displayName,
                "CITY",
                TetherfallRegionPacks.ResolveRegionName(city.center),
                "KNOWN CITY",
                city.subtitle,
                city.center,
                WorldMapMarkerKind.City,
                WorldMapMarkerTone.Parchment,
                true,
                false);
        }

        private void AddWorldMapMarker(
            string id,
            string displayName,
            string category,
            string region,
            string state,
            string description,
            Vector3 worldPosition,
            WorldMapMarkerKind kind,
            WorldMapMarkerTone tone,
            bool discovered,
            bool restored)
        {
            if (string.IsNullOrEmpty(id) || !TetherfallWorldMapRules.IsFinite(worldPosition))
            {
                return;
            }

            worldMapMarkers.Add(new WorldMapMarker
            {
                Id = id,
                Name = string.IsNullOrEmpty(displayName) ? "Unknown marker" : displayName,
                Meta = string.IsNullOrEmpty(region) ? category : $"{category}  ·  {region}",
                State = string.IsNullOrEmpty(state) ? "RECORDED" : state,
                Description = string.IsNullOrEmpty(description) ? "No field note recorded." : description,
                Coordinates = $"X {Mathf.RoundToInt(worldPosition.x):+0;-0;0}   Z {Mathf.RoundToInt(worldPosition.z):+0;-0;0}",
                WorldPosition = worldPosition,
                Kind = kind,
                Tone = tone,
                Discovered = discovered,
                Restored = restored
            });
        }

        private int FindWorldMapMarkerIndex(string markerId)
        {
            if (string.IsNullOrEmpty(markerId))
            {
                return -1;
            }

            for (var index = 0; index < worldMapMarkers.Count; index++)
            {
                if (string.Equals(worldMapMarkers[index].Id, markerId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private bool HasWorldMapHostilesAt(Vector3 position, float range)
        {
            var enemy = Enemy.FindClosest(position, range);
            if (enemy != null && !enemy.IsDefeated && enemy.gameObject.activeInHierarchy)
            {
                return true;
            }

            return activeDungeonBoss != null &&
                   !activeDungeonBoss.IsDefeated &&
                   activeDungeonBoss.gameObject.activeInHierarchy &&
                   TetherfallWorldMapRules.HorizontalDistance(
                       position,
                       activeDungeonBoss.transform.position) <= range;
        }

        private void RefreshCachedWorldMapQuote()
        {
            var destination =
                selectedWorldMapMarkerIndex >= 0 &&
                selectedWorldMapMarkerIndex < worldMapMarkers.Count &&
                worldMapMarkers[selectedWorldMapMarkerIndex].Kind == WorldMapMarkerKind.Threadway
                    ? ThreadwayCatalog.Get(worldMapMarkers[selectedWorldMapMarkerIndex].Id)
                    : null;
            cachedWorldMapTravelQuote = GetWorldMapTravelQuote(destination != null ? destination.id : null);
            RefreshCachedWorldMapQuoteText(destination);
        }

        private void RefreshCachedWorldMapQuoteText(ThreadwayDefinition destination)
        {
            var source = activeThreadway != null ? activeThreadway.Definition : null;
            cachedWorldMapRouteText =
                destination == null
                    ? string.Empty
                    : source == null
                        ? $"No current Threadway to {destination.title}"
                        : $"{source.title} to {destination.title}";
            cachedWorldMapCostText =
                destination == null
                    ? string.Empty
                    : cachedWorldMapTravelQuote.ChimesaltCost == 0
                        ? $"{Mathf.CeilToInt(cachedWorldMapTravelQuote.DistanceMeters)}m route  ·  No Chimesalt required"
                        : $"{Mathf.CeilToInt(cachedWorldMapTravelQuote.DistanceMeters)}m route  ·  " +
                          $"{cachedWorldMapTravelQuote.ChimesaltCost} Chimesalt";
            cachedWorldMapTravelStatus = cachedWorldMapTravelQuote.CanTravel
                ? "Route ready."
                : string.IsNullOrEmpty(cachedWorldMapTravelQuote.DisabledReason)
                    ? TetherfallWorldMapRules.SelectDestinationDisabledReason
                    : cachedWorldMapTravelQuote.DisabledReason;
        }

        private void SetWorldMapNoSelectionQuote()
        {
            cachedWorldMapTravelQuote = TetherfallWorldMapRules.CreateTravelQuote(
                activeThreadway != null ? activeThreadway.Definition : null,
                null,
                Player != null,
                false,
                false,
                false,
                false,
                false,
                GetResource(ResourceKind.Chimesalt));
            cachedWorldMapRouteText = string.Empty;
            cachedWorldMapCostText = string.Empty;
            cachedWorldMapTravelStatus = TetherfallWorldMapRules.SelectDestinationDisabledReason;
        }

        private bool ProcessWorldMapKey(KeyCode key, bool reverse)
        {
            switch (key)
            {
                case KeyCode.Escape:
                    if (!CancelWorldMapSelection())
                    {
                        ResetWorldMapUi();
                        ClosePanel();
                    }

                    return true;

                case KeyCode.Delete:
                case KeyCode.Backspace:
                    CancelWorldMapSelection();
                    return true;

                case KeyCode.LeftArrow:
                    return MoveWorldMapSelection(new Vector2(-1f, 0f));

                case KeyCode.RightArrow:
                    return MoveWorldMapSelection(new Vector2(1f, 0f));

                case KeyCode.UpArrow:
                    return MoveWorldMapSelection(new Vector2(0f, -1f));

                case KeyCode.DownArrow:
                    return MoveWorldMapSelection(new Vector2(0f, 1f));

                case KeyCode.Tab:
                    return CycleWorldMapSelection(reverse ? -1 : 1);

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    ConfirmWorldMapTravel();
                    return true;

                default:
                    return false;
            }
        }

        private void HandleWorldMapGuiKeyboard(Event currentEvent)
        {
            if (currentEvent == null ||
                currentEvent.type != EventType.KeyDown ||
                worldMapLastInputFrame == Time.frameCount)
            {
                return;
            }

            var handled = ProcessWorldMapKey(
                currentEvent.keyCode,
                (currentEvent.modifiers & EventModifiers.Shift) != 0);
            if (!handled)
            {
                return;
            }

            worldMapLastInputFrame = Time.frameCount;
            currentEvent.Use();
        }

        private bool CycleWorldMapSelection(int direction)
        {
            if (worldMapMarkers.Count == 0)
            {
                return false;
            }

            var index = selectedWorldMapMarkerIndex;
            if (index < 0 || index >= worldMapMarkers.Count)
            {
                index = direction >= 0 ? 0 : worldMapMarkers.Count - 1;
            }
            else
            {
                index = (index + direction + worldMapMarkers.Count) % worldMapMarkers.Count;
            }

            return SelectWorldMapMarker(worldMapMarkers[index].Id);
        }

        private bool MoveWorldMapSelection(Vector2 direction)
        {
            if (worldMapMarkers.Count == 0)
            {
                return false;
            }

            if (selectedWorldMapMarkerIndex < 0 ||
                selectedWorldMapMarkerIndex >= worldMapMarkers.Count)
            {
                return CycleWorldMapSelection(1);
            }

            var current = worldMapMarkers[selectedWorldMapMarkerIndex].WorldPosition;
            var currentX = Mathf.InverseLerp(
                TetherfallWorldMapRules.MinimumWorldX,
                TetherfallWorldMapRules.MaximumWorldX,
                current.x);
            var currentY = 1f - Mathf.InverseLerp(
                TetherfallWorldMapRules.MinimumWorldZ,
                TetherfallWorldMapRules.MaximumWorldZ,
                current.z);
            var bestIndex = -1;
            var bestScore = float.MaxValue;
            for (var index = 0; index < worldMapMarkers.Count; index++)
            {
                if (index == selectedWorldMapMarkerIndex)
                {
                    continue;
                }

                var candidate = worldMapMarkers[index].WorldPosition;
                var delta = new Vector2(
                    Mathf.InverseLerp(
                        TetherfallWorldMapRules.MinimumWorldX,
                        TetherfallWorldMapRules.MaximumWorldX,
                        candidate.x) - currentX,
                    1f - Mathf.InverseLerp(
                        TetherfallWorldMapRules.MinimumWorldZ,
                        TetherfallWorldMapRules.MaximumWorldZ,
                        candidate.z) - currentY);
                var forward = Vector2.Dot(delta, direction);
                if (forward <= 0.002f)
                {
                    continue;
                }

                var lateral = Mathf.Abs(delta.x * direction.y - delta.y * direction.x);
                var score = delta.sqrMagnitude + lateral * lateral * 2.5f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = index;
                }
            }

            return bestIndex >= 0
                ? SelectWorldMapMarker(worldMapMarkers[bestIndex].Id)
                : CycleWorldMapSelection(direction.x + direction.y >= 0f ? 1 : -1);
        }

        private bool TryValidateWorldMapArrivalCandidate(
            Vector3 desiredArrival,
            out Vector3 safeArrival,
            out bool foundGround)
        {
            safeArrival = Vector3.zero;
            foundGround = false;
            if (!TetherfallWorldMapRules.IsFinite(desiredArrival) ||
                desiredArrival.x < TetherfallWorldMapRules.MinimumWorldX ||
                desiredArrival.x > TetherfallWorldMapRules.MaximumWorldX ||
                desiredArrival.z < TetherfallWorldMapRules.MinimumWorldZ ||
                desiredArrival.z > TetherfallWorldMapRules.MaximumWorldZ)
            {
                return false;
            }

            var castOrigin = desiredArrival + Vector3.up * ArrivalCastHeight;
            var hitCount = Physics.RaycastNonAlloc(
                castOrigin,
                Vector3.down,
                worldMapArrivalCastBuffer,
                ArrivalCastDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            var bestDifference = float.MaxValue;
            var bestPoint = Vector3.zero;
            Collider supportCollider = null;
            for (var index = 0; index < hitCount; index++)
            {
                var hit = worldMapArrivalCastBuffer[index];
                if (hit.collider == null ||
                    hit.normal.y < ArrivalMinimumGroundNormalY ||
                    IsWorldMapActorCollider(hit.collider) ||
                    !TetherfallWorldMapRules.IsFinite(hit.point))
                {
                    continue;
                }

                var difference = Mathf.Abs(hit.point.y - desiredArrival.y);
                if (difference > ArrivalMaximumGroundDelta || difference >= bestDifference)
                {
                    continue;
                }

                bestDifference = difference;
                bestPoint = hit.point;
                supportCollider = hit.collider;
            }

            if (supportCollider == null)
            {
                return false;
            }

            foundGround = true;
            var candidate = bestPoint + Vector3.up * ArrivalGroundClearance;
            if (!TetherfallWorldMapRules.IsFinite(candidate) ||
                IsWorldMapArrivalBlocked(candidate, supportCollider))
            {
                return false;
            }

            safeArrival = candidate;
            return true;
        }

        private bool IsWorldMapArrivalBlocked(Vector3 candidate, Collider supportCollider)
        {
            var controller = Player != null ? Player.GetComponent<CharacterController>() : null;
            var radius = controller != null ? Mathf.Max(0.28f, controller.radius) : 0.36f;
            var height = controller != null ? Mathf.Max(radius * 2f, controller.height) : 1.8f;
            var bottom = candidate + Vector3.up * (radius + 0.04f);
            var top = candidate + Vector3.up * (height - radius + 0.04f);
            var overlapCount = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                radius,
                worldMapArrivalOverlapBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            if (overlapCount >= worldMapArrivalOverlapBuffer.Length)
            {
                return true;
            }

            for (var index = 0; index < overlapCount; index++)
            {
                var collider = worldMapArrivalOverlapBuffer[index];
                if (collider == null ||
                    collider == supportCollider ||
                    IsOwnedExpeditionCollider(collider) ||
                    collider.bounds.max.y <= candidate.y + 0.1f)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool IsWorldMapActorCollider(Collider collider)
        {
            return collider != null &&
                   (collider.GetComponentInParent<PlayerAvatar>() != null ||
                    collider.GetComponentInParent<HumanCompanion>() != null ||
                    collider.GetComponentInParent<PactCreature>() != null ||
                    collider.GetComponentInParent<Enemy>() != null ||
                    collider.GetComponentInParent<DungeonBoss>() != null);
        }

        private static bool IsOwnedExpeditionCollider(Collider collider)
        {
            return collider != null &&
                   (collider.GetComponentInParent<PlayerAvatar>() != null ||
                    collider.GetComponentInParent<HumanCompanion>() != null ||
                    collider.GetComponentInParent<PactCreature>() != null);
        }

        private void EnsureWorldMapStyles()
        {
            if (worldMapTitleStyle != null)
            {
                return;
            }

            worldMapTitleStyle = new GUIStyle(headingStyle)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                clipping = TextClipping.Clip,
                normal = { textColor = WorldMapParchment }
            };
            worldMapBodyStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = WorldMapMist }
            };
            worldMapMutedStyle = new GUIStyle(mutedStyle)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = WorldMapMuted }
            };
            worldMapLabelStyle = new GUIStyle(tinyStyle)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = WorldMapParchment }
            };
            worldMapButtonTextStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = WorldMapParchment }
            };
            worldMapInvisibleButtonStyle = new GUIStyle(GUIStyle.none);
            worldMapTealStatusStyle = new GUIStyle(worldMapMutedStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = WorldMapTeal }
            };
            worldMapCopperStatusStyle = new GUIStyle(worldMapMutedStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = WorldMapCopper }
            };
            worldMapDangerStatusStyle = new GUIStyle(worldMapMutedStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = WorldMapDanger }
            };
            worldMapParchmentStatusStyle = new GUIStyle(worldMapMutedStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = WorldMapParchment }
            };
        }

        private void DrawWorldMapWell(Rect rect)
        {
            DrawWorldMapColorRect(rect, WorldMapInk);
            DrawWorldMapColorRect(new Rect(rect.x, rect.y, rect.width, 2f), WorldMapCopper);

            var plotRect = new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, rect.height - 24f);
            var third = plotRect.width / 3f;
            DrawWorldMapColorRect(
                new Rect(plotRect.x, plotRect.y, third, plotRect.height),
                new Color(WorldMapDanger.r, WorldMapDanger.g, WorldMapDanger.b, 0.035f));
            DrawWorldMapColorRect(
                new Rect(plotRect.x + third, plotRect.y, third, plotRect.height),
                new Color(WorldMapParchment.r, WorldMapParchment.g, WorldMapParchment.b, 0.025f));
            DrawWorldMapColorRect(
                new Rect(plotRect.x + third * 2f, plotRect.y, third, plotRect.height),
                new Color(WorldMapTeal.r, WorldMapTeal.g, WorldMapTeal.b, 0.035f));

            for (var index = 1; index < 6; index++)
            {
                var x = Mathf.Round(plotRect.x + plotRect.width * index / 6f);
                var y = Mathf.Round(plotRect.y + plotRect.height * index / 6f);
                DrawWorldMapColorRect(
                    new Rect(x, plotRect.y, 1f, plotRect.height),
                    new Color(WorldMapMuted.r, WorldMapMuted.g, WorldMapMuted.b, 0.09f));
                DrawWorldMapColorRect(
                    new Rect(plotRect.x, y, plotRect.width, 1f),
                    new Color(WorldMapMuted.r, WorldMapMuted.g, WorldMapMuted.b, 0.09f));
            }

            DrawWorldMapColorRect(
                new Rect(Mathf.Round(plotRect.x + third), plotRect.y, 1f, plotRect.height),
                new Color(WorldMapCopper.r, WorldMapCopper.g, WorldMapCopper.b, 0.32f));
            DrawWorldMapColorRect(
                new Rect(Mathf.Round(plotRect.x + third * 2f), plotRect.y, 1f, plotRect.height),
                new Color(WorldMapCopper.r, WorldMapCopper.g, WorldMapCopper.b, 0.32f));
            DrawRegionPackWorldMapTiles(plotRect);

            for (var index = 0; index < worldMapMarkers.Count; index++)
            {
                var marker = worldMapMarkers[index];
                if (!TetherfallWorldMapRules.TryProjectWorldToMap(
                        marker.WorldPosition,
                        plotRect,
                        out var markerPosition))
                {
                    continue;
                }

                DrawWorldMapMarker(marker, markerPosition, index == selectedWorldMapMarkerIndex);
                var hitRect = new Rect(markerPosition.x - 11f, markerPosition.y - 11f, 22f, 22f);
                worldMapButtonContent.text = string.Empty;
                if (GUI.Button(hitRect, worldMapButtonContent, worldMapInvisibleButtonStyle))
                {
                    SelectWorldMapMarker(marker.Id);
                }
            }

            if (selectedWorldMapMarkerIndex >= 0 &&
                selectedWorldMapMarkerIndex < worldMapMarkers.Count)
            {
                var marker = worldMapMarkers[selectedWorldMapMarkerIndex];
                if (TetherfallWorldMapRules.TryProjectWorldToMap(
                        marker.WorldPosition,
                        plotRect,
                        out var markerPosition))
                {
                    DrawSelectedWorldMapLabel(plotRect, marker, markerPosition);
                }
            }
        }

        private static void DrawRegionPackWorldMapTiles(Rect plotRect)
        {
            foreach (var definition in TetherfallRegionPacks.Playable)
            {
                if (definition == null)
                {
                    continue;
                }

                DrawRegionalWorldMapTile(plotRect, definition.WorldBounds, definition.mapColor);
            }

            foreach (var annex in TetherfallRegionalAnnexes.All)
            {
                if (annex != null)
                {
                    DrawRegionalWorldMapTile(plotRect, annex.WorldBounds, annex.mapColor);
                }
            }
        }

        private static void DrawRegionalWorldMapTile(Rect plotRect, Rect bounds, Color tint)
        {
            if (!TetherfallWorldMapRules.TryProjectWorldToMap(
                    new Vector3(bounds.xMin, 0f, bounds.yMin),
                    plotRect,
                    out var minimum) ||
                !TetherfallWorldMapRules.TryProjectWorldToMap(
                    new Vector3(bounds.xMax, 0f, bounds.yMax),
                    plotRect,
                    out var maximum))
            {
                return;
            }

            var regionRect = Rect.MinMaxRect(
                Mathf.Min(minimum.x, maximum.x),
                Mathf.Min(minimum.y, maximum.y),
                Mathf.Max(minimum.x, maximum.x),
                Mathf.Max(minimum.y, maximum.y));
            DrawWorldMapColorRect(
                regionRect,
                new Color(tint.r, tint.g, tint.b, 0.11f));
            DrawWorldMapOutline(
                regionRect,
                new Color(tint.r, tint.g, tint.b, 0.48f),
                1f);
        }

        private void DrawWorldMapMarker(WorldMapMarker marker, Vector2 position, bool selected)
        {
            var tone = GetWorldMapTone(marker.Tone);
            switch (marker.Kind)
            {
                case WorldMapMarkerKind.Player:
                    DrawWorldMapColorRect(
                        new Rect(position.x - 5f, position.y - 1f, 10f, 2f),
                        WorldMapParchment);
                    DrawWorldMapColorRect(
                        new Rect(position.x - 1f, position.y - 5f, 2f, 10f),
                        WorldMapParchment);
                    DrawWorldMapColorRect(
                        new Rect(position.x - 2f, position.y - 2f, 4f, 4f),
                        WorldMapTeal);
                    break;

                case WorldMapMarkerKind.Objective:
                    DrawWorldMapColorRect(
                        new Rect(position.x - 6f, position.y - 1f, 12f, 2f),
                        WorldMapCopper);
                    DrawWorldMapColorRect(
                        new Rect(position.x - 1f, position.y - 6f, 2f, 12f),
                        WorldMapCopper);
                    break;

                case WorldMapMarkerKind.City:
                    DrawWorldMapOutline(new Rect(position.x - 5f, position.y - 5f, 10f, 10f), tone, 1f);
                    DrawWorldMapColorRect(
                        new Rect(position.x - 2f, position.y - 2f, 4f, 4f),
                        tone);
                    break;

                case WorldMapMarkerKind.EnemyCamp:
                    DrawWorldMapColorRect(
                        new Rect(position.x - 4f, position.y - 4f, 8f, 8f),
                        tone);
                    DrawWorldMapColorRect(
                        new Rect(position.x - 2f, position.y - 2f, 4f, 4f),
                        WorldMapInk);
                    break;

                case WorldMapMarkerKind.PortableCamp:
                    DrawWorldMapOutline(new Rect(position.x - 6f, position.y - 5f, 12f, 10f), tone, 1f);
                    DrawWorldMapColorRect(
                        new Rect(position.x - 2f, position.y - 2f, 4f, 4f),
                        WorldMapTeal);
                    break;

                case WorldMapMarkerKind.Dungeon:
                    DrawWorldMapOutline(new Rect(position.x - 6f, position.y - 6f, 12f, 12f), tone, 2f);
                    DrawWorldMapColorRect(
                        new Rect(position.x - 2f, position.y - 2f, 4f, 4f),
                        WorldMapTeal);
                    break;

                case WorldMapMarkerKind.Threadway:
                    if (marker.Restored)
                    {
                        DrawWorldMapOutline(new Rect(position.x - 6f, position.y - 6f, 12f, 12f), WorldMapTeal, 2f);
                        DrawWorldMapColorRect(
                            new Rect(position.x - 2f, position.y - 2f, 4f, 4f),
                            WorldMapTeal);
                    }
                    else if (marker.Discovered)
                    {
                        DrawWorldMapOutline(new Rect(position.x - 5f, position.y - 5f, 10f, 10f), WorldMapCopper, 1f);
                    }
                    else
                    {
                        DrawWorldMapColorRect(
                            new Rect(position.x - 2f, position.y - 2f, 4f, 4f),
                            WorldMapMuted);
                    }

                    break;
            }

            if (TetherfallAccessibilityRuntime.ShouldReinforceColorCues &&
                marker.Tone == WorldMapMarkerTone.Danger)
            {
                DrawWorldMapOutline(
                    new Rect(position.x - 7f, position.y - 7f, 14f, 14f),
                    WorldMapParchment,
                    1f);
            }

            if (selected)
            {
                DrawWorldMapFocusCorners(new Rect(position.x - 10f, position.y - 10f, 20f, 20f));
            }
        }

        private void DrawSelectedWorldMapLabel(
            Rect plotRect,
            WorldMapMarker marker,
            Vector2 markerPosition)
        {
            worldMapMarkerLabelContent.text = marker.Name;
            var measured = worldMapLabelStyle.CalcSize(worldMapMarkerLabelContent);
            var labelWidth = Mathf.Clamp(measured.x + 18f, 92f, Mathf.Min(230f, plotRect.width - 12f));
            const float labelHeight = 24f;
            var labelX = Mathf.Clamp(
                markerPosition.x + 12f,
                plotRect.x + 4f,
                plotRect.xMax - labelWidth - 4f);
            var labelY = markerPosition.y - labelHeight - 10f;
            if (labelY < plotRect.y + 4f)
            {
                labelY = markerPosition.y + 10f;
            }

            labelY = Mathf.Clamp(labelY, plotRect.y + 4f, plotRect.yMax - labelHeight - 4f);
            var labelRect = new Rect(labelX, labelY, labelWidth, labelHeight);
            DrawWorldMapColorRect(labelRect, WorldMapInk);
            DrawWorldMapColorRect(new Rect(labelRect.x, labelRect.y, labelRect.width, 1f), WorldMapCopper);
            GUI.Label(
                new Rect(labelRect.x + 8f, labelRect.y, labelRect.width - 16f, labelRect.height),
                worldMapMarkerLabelContent,
                worldMapLabelStyle);
        }

        private void DrawWorldMapDetailRail(Rect rect)
        {
            DrawWorldMapColorRect(rect, WorldMapSlate);
            DrawWorldMapColorRect(new Rect(rect.x, rect.y, rect.width, 2f), WorldMapCopper);
            var content = new Rect(rect.x + 16f, rect.y + 14f, rect.width - 32f, rect.height - 28f);

            if (selectedWorldMapMarkerIndex < 0 ||
                selectedWorldMapMarkerIndex >= worldMapMarkers.Count)
            {
                GUI.Label(
                    new Rect(content.x, content.y + 4f, content.width, 26f),
                    "Select a marker",
                    worldMapTitleStyle);
                GUI.Label(
                    new Rect(content.x, content.y + 42f, content.width, 56f),
                    "Click a marker or use the arrow keys.",
                    worldMapBodyStyle);
                GUI.Label(
                    new Rect(content.x, content.yMax - 30f, content.width, 22f),
                    "Arrow keys select  ·  Esc closes",
                    worldMapMutedStyle);
                return;
            }

            var marker = worldMapMarkers[selectedWorldMapMarkerIndex];
            GUI.Label(
                new Rect(content.x, content.y, content.width, 42f),
                marker.Name,
                worldMapTitleStyle);
            GUI.Label(
                new Rect(content.x, content.y + 44f, content.width, 20f),
                marker.Meta,
                worldMapMutedStyle);
            DrawWorldMapColorRect(
                new Rect(content.x, content.y + 72f, content.width, 1f),
                new Color(WorldMapCopper.r, WorldMapCopper.g, WorldMapCopper.b, 0.62f));
            GUI.Label(
                new Rect(content.x, content.y + 86f, content.width, 20f),
                GetAccessibleWorldMapState(marker),
                GetWorldMapStatusStyle(marker.Tone));
            GUI.Label(
                new Rect(content.x, content.y + 116f, content.width, 82f),
                marker.Description,
                worldMapBodyStyle);
            GUI.Label(
                new Rect(content.x, content.y + 202f, content.width, 20f),
                marker.Coordinates,
                worldMapMutedStyle);

            var routeY = content.y + 242f;
            if (marker.Kind == WorldMapMarkerKind.Threadway)
            {
                DrawWorldMapColorRect(
                    new Rect(content.x, routeY, content.width, 1f),
                    new Color(WorldMapCopper.r, WorldMapCopper.g, WorldMapCopper.b, 0.62f));
                GUI.Label(
                    new Rect(content.x, routeY + 14f, content.width, 38f),
                    cachedWorldMapRouteText,
                    worldMapBodyStyle);
                GUI.Label(
                    new Rect(content.x, routeY + 56f, content.width, 34f),
                    cachedWorldMapCostText,
                    worldMapMutedStyle);
                GUI.Label(
                    new Rect(content.x, routeY + 96f, content.width, 48f),
                    cachedWorldMapTravelStatus,
                    cachedWorldMapTravelQuote.CanTravel
                        ? worldMapTealStatusStyle
                        : worldMapDangerStatusStyle);
            }
            else
            {
                GUI.Label(
                    new Rect(content.x, routeY + 14f, content.width, 52f),
                    "Travel is available only between restored Threadways.",
                    worldMapMutedStyle);
            }

            var buttonRect = new Rect(content.x, content.yMax - 70f, content.width, 34f);
            DrawWorldMapConfirmButton(buttonRect, marker.Kind == WorldMapMarkerKind.Threadway);
            GUI.Label(
                new Rect(content.x, content.yMax - 26f, content.width, 20f),
                "Arrow keys select  ·  Enter confirms  ·  Esc clears",
                worldMapMutedStyle);
        }

        private static string GetAccessibleWorldMapState(
            WorldMapMarker marker)
        {
            if (!TetherfallAccessibilityRuntime.ShouldReinforceColorCues)
            {
                return marker.State;
            }

            var shape = marker.Kind switch
            {
                WorldMapMarkerKind.Player => "CROSS",
                WorldMapMarkerKind.Objective => "CROSS",
                WorldMapMarkerKind.City => "SQUARE",
                WorldMapMarkerKind.EnemyCamp => "DOUBLE SQUARE",
                WorldMapMarkerKind.PortableCamp => "FRAME",
                WorldMapMarkerKind.Dungeon => "DOUBLE FRAME",
                WorldMapMarkerKind.Threadway =>
                    marker.Restored ? "DOUBLE FRAME" : "ROUTE FRAME",
                _ => "MARKER"
            };
            return $"{shape}  {marker.State}";
        }

        private void DrawWorldMapConfirmButton(Rect rect, bool threadwaySelected)
        {
            var canTravel = threadwaySelected && cachedWorldMapTravelQuote.CanTravel;
            var hovered = rect.Contains(Event.current != null ? Event.current.mousePosition : Vector2.zero);
            DrawWorldMapColorRect(
                rect,
                canTravel && hovered
                    ? new Color(0.075f, 0.18f, 0.2f, 1f)
                    : WorldMapInk);
            DrawWorldMapColorRect(
                new Rect(rect.x, rect.y, rect.width, 2f),
                canTravel ? WorldMapTeal : new Color(WorldMapMuted.r, WorldMapMuted.g, WorldMapMuted.b, 0.42f));

            var previousColor = worldMapButtonTextStyle.normal.textColor;
            worldMapButtonTextStyle.normal.textColor = canTravel ? WorldMapParchment : WorldMapMuted;
            GUI.Label(rect, "Confirm travel", worldMapButtonTextStyle);
            worldMapButtonTextStyle.normal.textColor = previousColor;

            var previousEnabled = GUI.enabled;
            GUI.enabled = canTravel;
            worldMapButtonContent.text = string.Empty;
            if (GUI.Button(rect, worldMapButtonContent, worldMapInvisibleButtonStyle))
            {
                ConfirmWorldMapTravel();
            }

            GUI.enabled = previousEnabled;
        }

        private GUIStyle GetWorldMapStatusStyle(WorldMapMarkerTone tone)
        {
            switch (tone)
            {
                case WorldMapMarkerTone.Teal:
                    return worldMapTealStatusStyle;
                case WorldMapMarkerTone.Copper:
                    return worldMapCopperStatusStyle;
                case WorldMapMarkerTone.Danger:
                    return worldMapDangerStatusStyle;
                case WorldMapMarkerTone.Parchment:
                    return worldMapParchmentStatusStyle;
                default:
                    return worldMapMutedStyle;
            }
        }

        private static Color GetWorldMapTone(WorldMapMarkerTone tone)
        {
            switch (tone)
            {
                case WorldMapMarkerTone.Teal:
                    return WorldMapTeal;
                case WorldMapMarkerTone.Copper:
                    return WorldMapCopper;
                case WorldMapMarkerTone.Danger:
                    return WorldMapDanger;
                case WorldMapMarkerTone.Parchment:
                    return WorldMapParchment;
                default:
                    return WorldMapMuted;
            }
        }

        private static void DrawWorldMapFocusCorners(Rect rect)
        {
            const float length = 5f;
            const float thickness = 1f;
            DrawWorldMapColorRect(new Rect(rect.x, rect.y, length, thickness), WorldMapCopper);
            DrawWorldMapColorRect(new Rect(rect.x, rect.y, thickness, length), WorldMapCopper);
            DrawWorldMapColorRect(new Rect(rect.xMax - length, rect.y, length, thickness), WorldMapCopper);
            DrawWorldMapColorRect(new Rect(rect.xMax - thickness, rect.y, thickness, length), WorldMapCopper);
            DrawWorldMapColorRect(new Rect(rect.x, rect.yMax - thickness, length, thickness), WorldMapCopper);
            DrawWorldMapColorRect(new Rect(rect.x, rect.yMax - length, thickness, length), WorldMapCopper);
            DrawWorldMapColorRect(
                new Rect(rect.xMax - length, rect.yMax - thickness, length, thickness),
                WorldMapCopper);
            DrawWorldMapColorRect(
                new Rect(rect.xMax - thickness, rect.yMax - length, thickness, length),
                WorldMapCopper);
        }

        private static void DrawWorldMapOutline(Rect rect, Color color, float thickness)
        {
            DrawWorldMapColorRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawWorldMapColorRect(
                new Rect(rect.x, rect.yMax - thickness, rect.width, thickness),
                color);
            DrawWorldMapColorRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawWorldMapColorRect(
                new Rect(rect.xMax - thickness, rect.y, thickness, rect.height),
                color);
        }

        private static void DrawWorldMapColorRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
    }
}
