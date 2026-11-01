using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    /// <summary>
    /// Edit-mode, catalog-first verification for the additive Skylance vertical slice.
    /// It does not enter Play Mode or invoke the world builder.
    /// </summary>
    public static class TetherfallSkylanceWorldVerification
    {
        private const string MenuPath = "Tetherfall/Verify Skylance World";
        private static int assertionCount;

        [MenuItem(MenuPath, false, 2218)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Skylance verification is edit-mode only. Stop Play Mode first.");
            }

            assertionCount = 0;
            var failures = new List<string>();
            VerifyCatalogShape(failures);
            VerifyFiniteTerrainAndLandmarks(failures);
            VerifyFlightRoutes(failures);
            VerifyAccessProgression(failures);
            VerifyLandingSafetyMath(failures);
            VerifyIslandConnectivity(failures);
            VerifyIdempotenceContract(failures);
            VerifyBudgetsAndStreaming(failures);
            VerifyStaticSourceLint(failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Tetherfall Skylance world verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            var budget = TetherfallSkylanceWorld.EstimateBudget();
            Debug.Log(
                $"Tetherfall Skylance world verification passed: {assertionCount} assertions; " +
                $"{TetherfallSkylanceWorld.Terrain.Count} alpine terrain pieces, " +
                $"{TetherfallSkylanceWorld.FloatingIslands.Count} connected floating islands, " +
                $"{TetherfallSkylanceWorld.FlightRoutes.Count} deterministic routes, " +
                $"{TetherfallSkylanceWorld.FlyingPactEncounterSockets.Count} flying-Pact sockets; " +
                $"conservative budget {budget.Renderers} renderers, {budget.Colliders} colliders, " +
                $"{budget.Lights} lights, {budget.UpdateDrivers} update drivers, " +
                $"{budget.SharedMaterials} shared materials.");
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        public static void VerifyForFullGame()
        {
            Verify();
        }

        private static void VerifyCatalogShape(List<string> failures)
        {
            Check(
                TetherfallSkylanceWorld.Terrain.Count >= 24,
                "The alpine extension needs at least 24 authored terrain pieces.",
                failures);
            Check(
                TetherfallSkylanceWorld.FloatingIslands.Count >= 7,
                "The Aerie Kingdom needs at least seven floating islands.",
                failures);
            Check(
                TetherfallSkylanceWorld.IslandBridges.Count >= 6,
                "The Aerie Kingdom needs a connected multi-bridge network.",
                failures);
            Check(
                TetherfallSkylanceWorld.CityDistricts.Count >= 6,
                "The Aerie Kingdom needs at least six vertical districts.",
                failures);
            Check(
                TetherfallSkylanceWorld.ServiceSockets.Count >= 7,
                "Vendor, quest, boss, landing, and recovery socket coverage is incomplete.",
                failures);
            Check(
                TetherfallSkylanceWorld.FlyingPactEncounterSockets.Count >= 4,
                "The module needs several flying-Pact encounter sockets.",
                failures);
            Check(
                TetherfallSkylanceWorld.FlightRoutes.Count == 4,
                "The route foundation must expose grounded, ascent, circuit, and descent routes.",
                failures);

            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            AddStableId(
                TetherfallSkylanceWorld.MountainRegionId,
                "region.skylance.",
                stableIds,
                failures);
            AddStableId(
                TetherfallSkylanceWorld.KingdomRegionId,
                "region.skylance.",
                stableIds,
                failures);

            var vendors = 0;
            var quests = 0;
            var bosses = 0;
            var landings = 0;
            var respawns = 0;
            for (var index = 0;
                 index < TetherfallSkylanceWorld.ServiceSockets.Count;
                 index++)
            {
                var socket = TetherfallSkylanceWorld.ServiceSockets[index];
                AddStableId(socket.StableId, "socket.skylance.", stableIds, failures);
                Check(
                    !string.IsNullOrWhiteSpace(socket.DisplayName),
                    $"Content socket '{socket.StableId}' has no authored name.",
                    failures);
                Check(
                    IsFinite(socket.Position) &&
                    IsFinite(socket.Forward) &&
                    socket.Forward.sqrMagnitude > 0.9f,
                    $"Content socket '{socket.StableId}' has an invalid pose.",
                    failures);
                switch (socket.Kind)
                {
                    case SkylanceSocketKind.Vendor:
                        vendors++;
                        break;
                    case SkylanceSocketKind.Quest:
                        quests++;
                        break;
                    case SkylanceSocketKind.Boss:
                        bosses++;
                        break;
                    case SkylanceSocketKind.Landing:
                        landings++;
                        break;
                    case SkylanceSocketKind.Respawn:
                        respawns++;
                        break;
                }
            }

            Check(vendors >= 2, "The kingdom needs at least two vendor sockets.", failures);
            Check(quests >= 3, "The kingdom needs at least three quest sockets.", failures);
            Check(bosses >= 1, "The kingdom needs a boss socket.", failures);
            Check(landings >= 1, "The kingdom needs an authored landing socket.", failures);
            Check(respawns >= 1, "The module needs an authored respawn socket.", failures);
        }

        private static void VerifyFiniteTerrainAndLandmarks(
            List<string> failures)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var switchbacks = 0;
            var avalanchePieces = 0;
            var climbablePieces = 0;
            var solidPieces = 0;
            var minimumX = float.PositiveInfinity;
            var maximumX = float.NegativeInfinity;
            var minimumZ = float.PositiveInfinity;
            var maximumZ = float.NegativeInfinity;

            for (var index = 0;
                 index < TetherfallSkylanceWorld.Terrain.Count;
                 index++)
            {
                var piece = TetherfallSkylanceWorld.Terrain[index];
                AddStableId(piece.StableId, "terrain.skylance.", ids, failures);
                Check(
                    !string.IsNullOrWhiteSpace(piece.DisplayName),
                    $"Terrain '{piece.StableId}' has no authored display name.",
                    failures);
                Check(
                    IsFinite(piece.Position) &&
                    IsFinite(piece.Scale) &&
                    IsFinite(piece.EulerAngles),
                    $"Terrain '{piece.StableId}' contains a non-finite transform.",
                    failures);
                Check(
                    piece.Scale.x > 0f &&
                    piece.Scale.y > 0f &&
                    piece.Scale.z > 0f,
                    $"Terrain '{piece.StableId}' has a non-positive scale.",
                    failures);
                Check(
                    piece.Zone == SkylanceZone.AlpineApproach,
                    $"Terrain '{piece.StableId}' is assigned to the wrong zone.",
                    failures);

                if (piece.StableId.IndexOf(
                        "switchback",
                        StringComparison.Ordinal) >= 0)
                {
                    switchbacks++;
                }

                if (piece.StableId.IndexOf(
                        "avalanche",
                        StringComparison.Ordinal) >= 0 ||
                    piece.StableId.IndexOf(
                        "ravine",
                        StringComparison.Ordinal) >= 0)
                {
                    avalanchePieces++;
                }

                if (piece.Climbable)
                {
                    climbablePieces++;
                    Check(
                        piece.Solid,
                        $"Climbable terrain '{piece.StableId}' is not solid.",
                        failures);
                }

                if (piece.Solid)
                {
                    solidPieces++;
                }

                minimumX = Mathf.Min(minimumX, piece.Position.x);
                maximumX = Mathf.Max(maximumX, piece.Position.x);
                minimumZ = Mathf.Min(minimumZ, piece.Position.z);
                maximumZ = Mathf.Max(maximumZ, piece.Position.z);
            }

            Check(switchbacks >= 7, "The mountain route lacks a full switchback sequence.", failures);
            Check(
                avalanchePieces >= 7,
                "The alpine composition lacks an authored avalanche-ravine identity.",
                failures);
            Check(
                climbablePieces >= 5,
                "The alpine extension needs several marked climb faces.",
                failures);
            Check(solidPieces >= 20, "The alpine traversal shell is underspecified.", failures);
            Check(
                maximumX - minimumX >= 110f &&
                maximumZ - minimumZ >= 150f,
                "The alpine extension is too small to read as a region.",
                failures);

            var islandIds = new HashSet<string>(StringComparer.Ordinal);
            var districtIds = new HashSet<string>(StringComparer.Ordinal);
            var minimumIslandY = float.PositiveInfinity;
            var maximumIslandY = float.NegativeInfinity;
            for (var index = 0;
                 index < TetherfallSkylanceWorld.FloatingIslands.Count;
                 index++)
            {
                var island = TetherfallSkylanceWorld.FloatingIslands[index];
                AddStableId(island.StableId, "island.skylance.", islandIds, failures);
                Check(
                    IsFinite(island.SurfaceCenter) &&
                    IsFinite(island.Footprint) &&
                    IsFinite(island.Depth),
                    $"Island '{island.StableId}' has invalid geometry.",
                    failures);
                Check(
                    island.SurfaceCenter.y >= TetherfallSkylanceWorld.HighAltitudeFloor,
                    $"Island '{island.StableId}' is not genuinely high-altitude.",
                    failures);
                Check(
                    island.Footprint.x >= 30f &&
                    island.Footprint.y >= 30f &&
                    island.Depth >= 10f,
                    $"Island '{island.StableId}' has an unsafe or insubstantial footprint.",
                    failures);
                Check(
                    TetherfallSkylanceWorld.IsSemanticStableId(island.DistrictId),
                    $"Island '{island.StableId}' has an invalid district ID.",
                    failures);
                minimumIslandY = Mathf.Min(minimumIslandY, island.SurfaceCenter.y);
                maximumIslandY = Mathf.Max(maximumIslandY, island.SurfaceCenter.y);
            }

            for (var index = 0;
                 index < TetherfallSkylanceWorld.CityDistricts.Count;
                 index++)
            {
                var district = TetherfallSkylanceWorld.CityDistricts[index];
                AddStableId(
                    district.StableId,
                    "district.skylance.aerie.",
                    districtIds,
                    failures);
                var island = TetherfallSkylanceWorld.GetIsland(district.IslandId);
                Check(
                    !string.IsNullOrEmpty(island.StableId),
                    $"District '{district.StableId}' references missing island " +
                    $"'{district.IslandId}'.",
                    failures);
                Check(
                    string.Equals(
                        island.DistrictId,
                        district.StableId,
                        StringComparison.Ordinal),
                    $"District '{district.StableId}' and island '{island.StableId}' disagree.",
                    failures);
                Check(
                    IsFinite(district.PlazaPosition) &&
                    Mathf.Abs(
                        district.PlazaPosition.y -
                        (island.SurfaceCenter.y + 1f)) <= 0.25f,
                    $"District '{district.StableId}' plaza is not on its island.",
                    failures);
            }

            Check(
                districtIds.Count == islandIds.Count,
                "Every floating island must own exactly one vertical-city district.",
                failures);
            Check(
                maximumIslandY - minimumIslandY >= 40f,
                "The Aerie districts do not span a meaningful vertical hierarchy.",
                failures);

            Check(
                TetherfallSkylanceWorld.TryResolveRegion(
                    TetherfallSkylanceWorld.MountainEntryPosition,
                    out var mountainId,
                    out var mountainName) &&
                string.Equals(
                    mountainId,
                    TetherfallSkylanceWorld.MountainRegionId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    mountainName,
                    TetherfallSkylanceWorld.MountainRegionName,
                    StringComparison.Ordinal),
                "Mountain region resolution is not stable.",
                failures);
            Check(
                TetherfallSkylanceWorld.TryResolveRegion(
                    TetherfallSkylanceWorld.KingdomArrivalPosition,
                    out var kingdomId,
                    out var kingdomName) &&
                string.Equals(
                    kingdomId,
                    TetherfallSkylanceWorld.KingdomRegionId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    kingdomName,
                    TetherfallSkylanceWorld.KingdomRegionName,
                    StringComparison.Ordinal),
                "High-altitude kingdom region resolution is not stable.",
                failures);
            Check(
                !TetherfallSkylanceWorld.TryResolveRegion(
                    Vector3.zero,
                    out _,
                    out _),
                "Skylance region resolution claims unrelated world coordinates.",
                failures);
        }

        private static void VerifyFlightRoutes(List<string> failures)
        {
            var routeIds = new HashSet<string>(StringComparer.Ordinal);
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            SkylanceFlightRouteDefinition ascent = null;
            SkylanceFlightRouteDefinition circuit = null;
            SkylanceFlightRouteDefinition descent = null;
            SkylanceFlightRouteDefinition grounded = null;

            for (var routeIndex = 0;
                 routeIndex < TetherfallSkylanceWorld.FlightRoutes.Count;
                 routeIndex++)
            {
                var route = TetherfallSkylanceWorld.FlightRoutes[routeIndex];
                Check(route != null, "Flight-route catalog contains a null route.", failures);
                if (route == null)
                {
                    continue;
                }

                AddStableId(route.StableId, "route.skylance.", routeIds, failures);
                Check(
                    !string.IsNullOrWhiteSpace(route.DisplayName),
                    $"Route '{route.StableId}' has no authored display name.",
                    failures);
                Check(
                    route.NodeCount >= 4,
                    $"Route '{route.StableId}' has fewer than four authored nodes.",
                    failures);
                Check(
                    TetherfallSkylanceWorld.GetRouteLength(route) >= 40f,
                    $"Route '{route.StableId}' is too short to be meaningful.",
                    failures);

                for (var nodeIndex = 0; nodeIndex < route.NodeCount; nodeIndex++)
                {
                    Check(
                        route.TryGetNode(nodeIndex, out var node),
                        $"Route '{route.StableId}' cannot retrieve node {nodeIndex}.",
                        failures);
                    AddStableId(
                        node.StableId,
                        "route_node.skylance.",
                        nodeIds,
                        failures);
                    Check(
                        IsFinite(node.Position) &&
                        IsFinite(node.CaptureRadius) &&
                        IsFinite(node.LiftVelocity) &&
                        node.CaptureRadius > 0f,
                        $"Route node '{node.StableId}' has invalid route data.",
                        failures);
                    if (node.Kind == SkylanceRouteNodeKind.LiftRing)
                    {
                        Check(
                            node.LiftVelocity >= 10f,
                            $"Lift ring '{node.StableId}' has insufficient authored lift.",
                            failures);
                    }
                }

                Check(
                    TetherfallSkylanceWorld.TrySampleRoute(
                        route,
                        0.5f,
                        out var midpointA) &&
                    TetherfallSkylanceWorld.TrySampleRoute(
                        route,
                        0.5f,
                        out var midpointB) &&
                    Approximately(midpointA.Position, midpointB.Position) &&
                    Approximately(midpointA.Tangent, midpointB.Tangent) &&
                    midpointA.SegmentIndex == midpointB.SegmentIndex &&
                    Approximately(
                        midpointA.SegmentProgress,
                        midpointB.SegmentProgress),
                    $"Route '{route.StableId}' sampling is not deterministic.",
                    failures);

                switch (route.Kind)
                {
                    case SkylanceRouteKind.GroundedSwitchback:
                        grounded = route;
                        break;
                    case SkylanceRouteKind.GatedAscent:
                        ascent = route;
                        break;
                    case SkylanceRouteKind.AerialCircuit:
                        circuit = route;
                        break;
                    case SkylanceRouteKind.SafeDescent:
                        descent = route;
                        break;
                }
            }

            Check(
                grounded != null &&
                grounded.Access == SkylanceRouteAccess.Open,
                "The mountain switchback route must remain openly walkable.",
                failures);
            Check(
                ascent != null &&
                ascent.Access == SkylanceRouteAccess.FlightPactOrAuthoredAscent,
                "The Sunhook route is missing its flight progression gate.",
                failures);
            Check(
                circuit != null &&
                circuit.Access == SkylanceRouteAccess.FlightPactOrAuthoredAscent &&
                circuit.ClosedCircuit,
                "The crown circuit must be a closed, flight-gated route.",
                failures);
            Check(
                descent != null &&
                descent.Access == SkylanceRouteAccess.AlwaysReturn,
                "The lee descent must always remain open.",
                failures);

            if (ascent != null)
            {
                ascent.TryGetNode(0, out var start);
                ascent.TryGetNode(ascent.NodeCount - 1, out var end);
                Check(
                    start.Position.y < 70f &&
                    end.Position.y >= TetherfallSkylanceWorld.HighAltitudeFloor &&
                    end.Position.y - start.Position.y >= 115f,
                    "The authored ascent does not bridge mountain and true high altitude.",
                    failures);
                var priorY = float.NegativeInfinity;
                for (var index = 0; index < ascent.NodeCount; index++)
                {
                    ascent.TryGetNode(index, out var node);
                    Check(
                        node.Position.y > priorY,
                        $"Ascent node '{node.StableId}' does not climb monotonically.",
                        failures);
                    if (index > 0)
                    {
                        ascent.TryGetNode(index - 1, out var previous);
                        var delta = node.Position - previous.Position;
                        var planarDistance =
                            new Vector2(delta.x, delta.z).magnitude;
                        var ballisticRise =
                            previous.LiftVelocity * previous.LiftVelocity /
                            (2f * 9.81f);
                        Check(
                            previous.LiftVelocity > 0f &&
                            delta.y <= ballisticRise * 0.95f &&
                            planarDistance <= 55f,
                            $"Ascent segment into '{node.StableId}' exceeds the base " +
                            "lift/glide traversal envelope.",
                            failures);
                    }

                    priorY = node.Position.y;
                }

                Check(
                    Vector3.Distance(
                        end.Position,
                        TetherfallSkylanceWorld.KingdomArrivalPosition) <= 0.25f,
                    "The ascent does not end at the authored kingdom landing.",
                    failures);
            }

            if (circuit != null)
            {
                circuit.TryGetNode(0, out var first);
                circuit.TryGetNode(circuit.NodeCount - 1, out var last);
                Check(
                    Vector3.Distance(first.Position, last.Position) <= 0.1f,
                    "The closed aerial circuit does not return to its first ring.",
                    failures);
            }

            if (descent != null)
            {
                descent.TryGetNode(0, out var first);
                descent.TryGetNode(descent.NodeCount - 1, out var last);
                Check(
                    first.Position.y >= TetherfallSkylanceWorld.HighAltitudeFloor &&
                    last.Position.y < 70f &&
                    first.Position.y - last.Position.y >= 115f,
                    "The lee descent does not return safely to mountain altitude.",
                    failures);
            }

            Check(
                !TetherfallSkylanceWorld.TrySampleRoute(
                    null,
                    0.5f,
                    out _),
                "Route sampling accepts a null route.",
                failures);
            Check(
                !TetherfallSkylanceWorld.TrySampleRoute(
                    ascent,
                    float.NaN,
                    out _),
                "Route sampling accepts non-finite progress.",
                failures);
        }

        private static void VerifyAccessProgression(List<string> failures)
        {
            var locked = new SkylanceAccessState(
                false,
                false,
                true,
                false);
            var flightPact = new SkylanceAccessState(
                true,
                false,
                true,
                false);
            var authored = new SkylanceAccessState(
                false,
                true,
                true,
                false);
            var unavailable = new SkylanceAccessState(
                true,
                true,
                false,
                false);
            var returnTravel = new SkylanceAccessState(
                false,
                false,
                false,
                true);

            var lockedDecision =
                SkylanceAccessRules.EvaluateKingdomAccess(in locked);
            var flightDecision =
                SkylanceAccessRules.EvaluateKingdomAccess(in flightPact);
            var authoredDecision =
                SkylanceAccessRules.EvaluateKingdomAccess(in authored);
            var unavailableDecision =
                SkylanceAccessRules.EvaluateKingdomAccess(in unavailable);
            var returnDecision =
                SkylanceAccessRules.EvaluateKingdomAccess(in returnTravel);
            Check(
                !lockedDecision.Allowed &&
                lockedDecision.Reason ==
                SkylanceAccessReason.FlightCapabilityRequired,
                "A fresh expedition can bypass the high-altitude gate.",
                failures);
            Check(
                flightDecision.Allowed &&
                flightDecision.Reason == SkylanceAccessReason.FlightCapablePact,
                "A flight-capable Pact does not open the authored ascent.",
                failures);
            Check(
                authoredDecision.Allowed &&
                authoredDecision.Reason == SkylanceAccessReason.AuthoredAscent,
                "The equivalent authored-ascent path does not open the gate.",
                failures);
            Check(
                !unavailableDecision.Allowed &&
                unavailableDecision.Reason ==
                SkylanceAccessReason.AscentRouteUnavailable,
                "A disabled ascent route can still be entered.",
                failures);
            Check(
                returnDecision.Allowed &&
                returnDecision.Reason ==
                SkylanceAccessReason.ReturnRouteAlwaysOpen,
                "Return travel can be progression-locked.",
                failures);

            var groundDecision = SkylanceAccessRules.EvaluateRouteAccess(
                SkylanceRouteAccess.Open,
                in locked);
            var descentDecision = SkylanceAccessRules.EvaluateRouteAccess(
                SkylanceRouteAccess.AlwaysReturn,
                in locked);
            Check(
                groundDecision.Allowed &&
                groundDecision.Reason ==
                SkylanceAccessReason.MountainRouteOpen,
                "The grounded alpine route is incorrectly gated.",
                failures);
            Check(
                descentDecision.Allowed,
                "The descent route is not fail-open.",
                failures);

            var groundedRoster = new CapturedPactSaveData[]
            {
                new CapturedPactSaveData
                {
                    instanceId = "verification.grounded",
                    speciesId = TetherfallAccordSpeciesCatalog.MossbellStriderId
                }
            };
            var flyingRoster = new CapturedPactSaveData[]
            {
                new CapturedPactSaveData
                {
                    instanceId = "verification.flying",
                    speciesId = TetherfallAccordSpeciesCatalog.CopperquillNesterId
                }
            };
            Check(
                !SkylanceAccessRules.HasFlightCapablePact(groundedRoster),
                "A grounded Pact incorrectly grants Aerie access.",
                failures);
            Check(
                SkylanceAccessRules.HasFlightCapablePact(flyingRoster),
                "A gliding roster Pact does not grant Aerie access.",
                failures);
            Check(
                !SkylanceAccessRules.HasFlightCapablePact(null),
                "A null Pact roster grants Aerie access.",
                failures);

            var noForm = new PactProgressSaveData[]
            {
                new PactProgressSaveData
                {
                    pactId = TetherfallPactCatalog.CinderwingId,
                    unfoldedFormId = string.Empty
                }
            };
            var suncrest = new PactProgressSaveData[]
            {
                new PactProgressSaveData
                {
                    pactId = TetherfallPactCatalog.CinderwingId,
                    unfoldedFormId = TetherfallPactCatalog.SuncrestFormId
                }
            };
            Check(
                !SkylanceAccessRules.HasUnfoldedFlightSupport(noForm) &&
                SkylanceAccessRules.HasUnfoldedFlightSupport(suncrest),
                "Cinderwing unfolding is not reflected in the access adapter.",
                failures);

            var persistenceIds = new HashSet<string>(StringComparer.Ordinal);
            var accessibleFlyingSockets = 0;
            var highSockets = 0;
            for (var index = 0;
                 index < TetherfallSkylanceWorld.FlyingPactEncounterSockets.Count;
                 index++)
            {
                var socket =
                    TetherfallSkylanceWorld.FlyingPactEncounterSockets[index];
                Check(
                    TetherfallSkylanceWorld.IsSemanticStableId(socket.StableId) &&
                    socket.StableId.StartsWith(
                        "socket.skylance.encounter.",
                        StringComparison.Ordinal),
                    $"Encounter socket ID '{socket.StableId}' is not semantic.",
                    failures);
                Check(
                    TetherfallSkylanceWorld.IsSemanticStableId(socket.PersistenceId) &&
                    socket.PersistenceId.StartsWith(
                        "encounter.skylance.",
                        StringComparison.Ordinal) &&
                    persistenceIds.Add(socket.PersistenceId),
                    $"Encounter persistence ID '{socket.PersistenceId}' is invalid or duplicated.",
                    failures);
                Check(
                    IsFinite(socket.Position),
                    $"Encounter socket '{socket.StableId}' has a non-finite position.",
                    failures);

                var encounter = EncounterCatalog.Get(socket.EncounterCatalogId);
                Check(
                    encounter != null &&
                    string.Equals(
                        encounter.id,
                        socket.EncounterCatalogId,
                        StringComparison.Ordinal) &&
                    socket.EncounterCatalogId.StartsWith(
                        "encounter.wild.",
                        StringComparison.Ordinal),
                    $"Encounter socket '{socket.StableId}' does not reference a wild Accord encounter.",
                    failures);
                if (encounter != null)
                {
                    for (var waveIndex = 0;
                         waveIndex < encounter.WaveCount;
                         waveIndex++)
                    {
                        var enemies = encounter.GetWave(waveIndex);
                        for (var enemyIndex = 0;
                             enemyIndex < enemies.Length;
                             enemyIndex++)
                        {
                            var species =
                                TetherfallAccordSpeciesCatalog.FindByEligibleEnemyId(
                                    enemies[enemyIndex]);
                            Check(
                                species?.movement?.usesFlyingMotor == true,
                                $"Encounter socket '{socket.StableId}' contains non-flying " +
                                $"or non-capturable enemy '{enemies[enemyIndex]}'.",
                                failures);
                        }
                    }
                }

                if (socket.SpawnAtBuild &&
                    socket.Position.y <
                    TetherfallSkylanceWorld.KingdomBoundaryAltitude)
                {
                    accessibleFlyingSockets++;
                }

                if (!socket.SpawnAtBuild &&
                    socket.Position.y >= TetherfallSkylanceWorld.HighAltitudeFloor)
                {
                    highSockets++;
                }
            }

            Check(
                accessibleFlyingSockets >= 3,
                "The player cannot earn a flying Pact before the kingdom gate.",
                failures);
            Check(
                highSockets >= 1,
                "The kingdom lacks a gated flying-Pact encounter socket.",
                failures);
        }

        private static void VerifyLandingSafetyMath(List<string> failures)
        {
            var policy = SkylanceLandingPolicy.SafeMount;
            var requiredEdge =
                policy.FootprintRadius + policy.RequiredEdgeClearance;
            var valid = new SkylanceLandingProbe(
                new Vector3(2f, 4f, 3f),
                new Vector3(2f, 1f, 3f),
                Vector3.up,
                true,
                false,
                requiredEdge + 0.2f,
                3f);
            Check(
                SkylanceLandingRules.Evaluate(in valid, in policy) ==
                SkylanceLandingFailure.None,
                "A valid broad, level, unoccupied landing is rejected.",
                failures);

            var noSurface = new SkylanceLandingProbe(
                Vector3.zero,
                Vector3.zero,
                Vector3.up,
                false,
                false,
                requiredEdge + 1f,
                1f);
            Check(
                SkylanceLandingRules.Evaluate(in noSurface, in policy) ==
                SkylanceLandingFailure.NoSurface,
                "Landing math accepts a missing surface.",
                failures);

            var steep = new SkylanceLandingProbe(
                Vector3.up * 3f,
                Vector3.zero,
                new Vector3(0.9f, 0.2f, 0f).normalized,
                true,
                false,
                requiredEdge + 1f,
                3f);
            Check(
                SkylanceLandingRules.Evaluate(in steep, in policy) ==
                SkylanceLandingFailure.SurfaceTooSteep,
                "Landing math accepts an unsafe slope.",
                failures);

            var edge = new SkylanceLandingProbe(
                Vector3.up * 3f,
                Vector3.zero,
                Vector3.up,
                true,
                false,
                requiredEdge - 0.01f,
                3f);
            Check(
                SkylanceLandingRules.Evaluate(in edge, in policy) ==
                SkylanceLandingFailure.EdgeClearanceTooSmall,
                "Landing math accepts an unsafe perch edge.",
                failures);

            var occupied = new SkylanceLandingProbe(
                Vector3.up * 3f,
                Vector3.zero,
                Vector3.up,
                true,
                true,
                requiredEdge + 1f,
                3f);
            Check(
                SkylanceLandingRules.Evaluate(in occupied, in policy) ==
                SkylanceLandingFailure.Occupied,
                "Landing math accepts an occupied mount capsule.",
                failures);

            var excessiveDrop = new SkylanceLandingProbe(
                Vector3.up * 30f,
                Vector3.zero,
                Vector3.up,
                true,
                false,
                requiredEdge + 1f,
                policy.MaximumDrop + 0.1f);
            Check(
                SkylanceLandingRules.Evaluate(in excessiveDrop, in policy) ==
                SkylanceLandingFailure.ExcessiveDrop,
                "Landing math accepts an excessive vertical drop.",
                failures);

            var nonFinite = new SkylanceLandingProbe(
                new Vector3(float.NaN, 0f, 0f),
                Vector3.zero,
                Vector3.up,
                true,
                false,
                requiredEdge + 1f,
                1f);
            Check(
                SkylanceLandingRules.Evaluate(in nonFinite, in policy) ==
                SkylanceLandingFailure.InvalidInput,
                "Landing math accepts a non-finite coordinate.",
                failures);

            var repeatedA = SkylanceLandingRules.Evaluate(in valid, in policy);
            var repeatedB = SkylanceLandingRules.Evaluate(in valid, in policy);
            Check(
                repeatedA == repeatedB,
                "Pure landing evaluation is not deterministic.",
                failures);

            var method = typeof(SkylanceLandingRules).GetMethod(
                "TryResolveSafeLandingNonAlloc",
                BindingFlags.Public | BindingFlags.Static);
            Check(
                method != null &&
                method.ReturnType == typeof(bool),
                "The reusable NonAlloc physics landing adapter is missing.",
                failures);
            if (method != null)
            {
                var parameters = method.GetParameters();
                var hasRaycastBuffer = false;
                var hasOverlapBuffer = false;
                for (var index = 0; index < parameters.Length; index++)
                {
                    hasRaycastBuffer |=
                        parameters[index].ParameterType == typeof(RaycastHit[]);
                    hasOverlapBuffer |=
                        parameters[index].ParameterType == typeof(Collider[]);
                }

                Check(
                    hasRaycastBuffer && hasOverlapBuffer,
                    "The landing adapter does not require caller-owned reusable buffers.",
                    failures);
            }
        }

        private static void VerifyIslandConnectivity(List<string> failures)
        {
            var adjacency =
                new Dictionary<string, List<string>>(StringComparer.Ordinal);
            for (var index = 0;
                 index < TetherfallSkylanceWorld.FloatingIslands.Count;
                 index++)
            {
                var island = TetherfallSkylanceWorld.FloatingIslands[index];
                adjacency[island.StableId] = new List<string>();
            }

            var bridgeIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0;
                 index < TetherfallSkylanceWorld.IslandBridges.Count;
                 index++)
            {
                var bridge = TetherfallSkylanceWorld.IslandBridges[index];
                AddStableId(
                    bridge.StableId,
                    "bridge.skylance.aerie.",
                    bridgeIds,
                    failures);
                Check(
                    adjacency.ContainsKey(bridge.FromIslandId) &&
                    adjacency.ContainsKey(bridge.ToIslandId) &&
                    !string.Equals(
                        bridge.FromIslandId,
                        bridge.ToIslandId,
                        StringComparison.Ordinal),
                    $"Bridge '{bridge.StableId}' has invalid endpoints.",
                    failures);
                Check(
                    bridge.Width >= 5f,
                    $"Bridge '{bridge.StableId}' is too narrow for safe mount landing flow.",
                    failures);
                if (!adjacency.ContainsKey(bridge.FromIslandId) ||
                    !adjacency.ContainsKey(bridge.ToIslandId))
                {
                    continue;
                }

                adjacency[bridge.FromIslandId].Add(bridge.ToIslandId);
                adjacency[bridge.ToIslandId].Add(bridge.FromIslandId);
                var from = TetherfallSkylanceWorld.GetIsland(
                    bridge.FromIslandId);
                var to = TetherfallSkylanceWorld.GetIsland(
                    bridge.ToIslandId);
                var delta = to.SurfaceCenter - from.SurfaceCenter;
                var planar = new Vector2(delta.x, delta.z).magnitude;
                var slope = Mathf.Atan2(Mathf.Abs(delta.y), planar) *
                            Mathf.Rad2Deg;
                Check(
                    slope <= 18f,
                    $"Bridge '{bridge.StableId}' exceeds the safe authored slope budget.",
                    failures);
            }

            var firstId =
                TetherfallSkylanceWorld.FloatingIslands[0].StableId;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>();
            visited.Add(firstId);
            queue.Enqueue(firstId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var neighbours = adjacency[current];
                for (var index = 0; index < neighbours.Count; index++)
                {
                    if (visited.Add(neighbours[index]))
                    {
                        queue.Enqueue(neighbours[index]);
                    }
                }
            }

            Check(
                visited.Count == TetherfallSkylanceWorld.FloatingIslands.Count,
                $"Only {visited.Count}/{TetherfallSkylanceWorld.FloatingIslands.Count} " +
                "floating islands are connected.",
                failures);
            Check(
                TetherfallSkylanceWorld.TryGetRoute(
                    TetherfallSkylanceWorld.AscentRouteId,
                    out var ascent) &&
                ascent.TryGetNode(ascent.NodeCount - 1, out var arrivalNode) &&
                Vector3.Distance(
                    arrivalNode.Position,
                    TetherfallSkylanceWorld.FloatingIslands[0].SurfaceCenter) <= 2f,
                "The flight-gated route does not join the connected island graph.",
                failures);
        }

        private static void VerifyIdempotenceContract(List<string> failures)
        {
            var build = typeof(TetherfallSkylanceWorld).GetMethod(
                "Build",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(TetherfallGame) },
                null);
            Check(
                build != null &&
                build.ReturnType == typeof(void),
                "The module does not expose public static void Build(TetherfallGame game).",
                failures);
            Check(
                !string.IsNullOrWhiteSpace(TetherfallSkylanceWorld.RootName),
                "The idempotence root name is empty.",
                failures);

            var runtimePath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "TetherfallSkylanceWorld.cs");
            Check(File.Exists(runtimePath), $"Runtime source is missing: {runtimePath}", failures);
            if (!File.Exists(runtimePath))
            {
                return;
            }

            var source = File.ReadAllText(runtimePath);
            var findIndex = source.IndexOf(
                "var existing = FindDirectChild(game.transform, RootName);",
                StringComparison.Ordinal);
            var existingGuardIndex = source.IndexOf(
                "if (existing != null)",
                findIndex >= 0 ? findIndex : 0,
                StringComparison.Ordinal);
            var creationIndex = source.IndexOf(
                "var root = new GameObject(RootName);",
                StringComparison.Ordinal);
            Check(
                findIndex >= 0 &&
                existingGuardIndex > findIndex &&
                creationIndex > existingGuardIndex,
                "Build does not check the direct stable root before creating content.",
                failures);
            Check(
                source.IndexOf(
                    "lastRuntime = existing.GetComponent<SkylanceWorldRuntime>();",
                    StringComparison.Ordinal) > existingGuardIndex,
                "Build does not restore its runtime reference from an existing root.",
                failures);
            Check(
                CountOccurrences(
                    source,
                    "public static void Build(TetherfallGame game)") == 1,
                "The runtime exposes more than one ambiguous Build(TetherfallGame) entry point.",
                failures);
        }

        private static void VerifyBudgetsAndStreaming(List<string> failures)
        {
            var budget = TetherfallSkylanceWorld.EstimateBudget();
            Check(
                budget.Renderers > 0 &&
                budget.Renderers <= TetherfallSkylanceWorld.MaximumRenderers,
                $"Renderer estimate {budget.Renderers} exceeds budget " +
                $"{TetherfallSkylanceWorld.MaximumRenderers}.",
                failures);
            Check(
                budget.Colliders > 0 &&
                budget.Colliders <= TetherfallSkylanceWorld.MaximumColliders,
                $"Collider estimate {budget.Colliders} exceeds budget " +
                $"{TetherfallSkylanceWorld.MaximumColliders}.",
                failures);
            Check(
                budget.Lights == 0 &&
                TetherfallSkylanceWorld.MaximumLights == 0,
                "The module must add zero lights.",
                failures);
            Check(
                budget.UpdateDrivers > 0 &&
                budget.UpdateDrivers <=
                TetherfallSkylanceWorld.MaximumUpdateDrivers,
                $"Update-driver estimate {budget.UpdateDrivers} exceeds budget " +
                $"{TetherfallSkylanceWorld.MaximumUpdateDrivers}.",
                failures);
            Check(
                budget.SharedMaterials > 0 &&
                budget.SharedMaterials <=
                TetherfallSkylanceWorld.MaximumSharedMaterials,
                $"Shared-material estimate {budget.SharedMaterials} exceeds budget " +
                $"{TetherfallSkylanceWorld.MaximumSharedMaterials}.",
                failures);
            Check(
                TetherfallSkylanceWorld.MaximumActiveKingdomDetailRenderers <
                TetherfallSkylanceWorld.MaximumRenderers,
                "The altitude-gated detail ceiling does not reduce the full renderer budget.",
                failures);
            Check(
                TetherfallSkylanceWorld.KingdomDetailMinimumAltitude >
                TetherfallSkylanceWorld.MountainSummitPosition.y &&
                TetherfallSkylanceWorld.KingdomDetailMinimumAltitude <
                TetherfallSkylanceWorld.HighAltitudeFloor,
                "Kingdom detail altitude gating is not between mountain and island floors.",
                failures);
            Check(
                TetherfallSkylanceWorld.MountainDetailDistance > 0f &&
                TetherfallSkylanceWorld.KingdomDetailDistance > 0f &&
                TetherfallSkylanceWorld.DirectorRefreshSeconds >= 0.1f,
                "Distance streaming or refresh cadence is unbounded.",
                failures);
        }

        private static void VerifyStaticSourceLint(List<string> failures)
        {
            var runtimePath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "TetherfallSkylanceWorld.cs");
            var editorPath = Path.Combine(
                Application.dataPath,
                "Editor",
                "TetherfallSkylanceWorldVerification.cs");
            Check(File.Exists(runtimePath), $"Runtime source is missing: {runtimePath}", failures);
            Check(File.Exists(editorPath), $"Verifier source is missing: {editorPath}", failures);
            if (!File.Exists(runtimePath) || !File.Exists(editorPath))
            {
                return;
            }

            var runtime = File.ReadAllText(runtimePath);
            var editor = File.ReadAllText(editorPath);
            var autoRunToken = "RuntimeInitialize" + "OnLoadMethod";
            Check(
                runtime.IndexOf(autoRunToken, StringComparison.Ordinal) < 0,
                "The module contains an automatic runtime initializer.",
                failures);
            Check(
                runtime.IndexOf("using System.Linq", StringComparison.Ordinal) < 0,
                "The runtime imports LINQ into allocation-sensitive route code.",
                failures);
            Check(
                runtime.IndexOf("UnityEngine.Random", StringComparison.Ordinal) < 0 &&
                runtime.IndexOf("Random.Range", StringComparison.Ordinal) < 0 &&
                runtime.IndexOf("Guid.NewGuid", StringComparison.Ordinal) < 0 &&
                runtime.IndexOf("DateTime.Now", StringComparison.Ordinal) < 0 &&
                runtime.IndexOf("DateTime.UtcNow", StringComparison.Ordinal) < 0,
                "The module contains a non-deterministic authoring dependency.",
                failures);
            Check(
                runtime.IndexOf("Physics.RaycastAll(", StringComparison.Ordinal) < 0 &&
                runtime.IndexOf("Physics.OverlapSphere(", StringComparison.Ordinal) < 0 &&
                runtime.IndexOf("Physics.OverlapCapsule(", StringComparison.Ordinal) < 0,
                "The runtime contains an allocating physics query.",
                failures);
            Check(
                runtime.IndexOf("Physics.RaycastNonAlloc(", StringComparison.Ordinal) >= 0 &&
                runtime.IndexOf("Physics.OverlapCapsuleNonAlloc(", StringComparison.Ordinal) >= 0,
                "The landing adapter is not using bounded NonAlloc physics queries.",
                failures);
            Check(
                runtime.IndexOf(".material =", StringComparison.Ordinal) < 0 &&
                runtime.IndexOf(".materials =", StringComparison.Ordinal) < 0,
                "The module instantiates renderer materials instead of using shared caches.",
                failures);
            Check(
                runtime.IndexOf(
                    "RuntimeEffectMaterialCache.GetOpaque",
                    StringComparison.Ordinal) >= 0 &&
                runtime.IndexOf(
                    "RuntimeEffectMaterialCache.GetTransparent",
                    StringComparison.Ordinal) >= 0 &&
                runtime.IndexOf(
                    "RuntimeEffectMaterialCache.GetGlow",
                    StringComparison.Ordinal) >= 0,
                "The runtime does not use the shared material cache for all material classes.",
                failures);
            Check(
                runtime.IndexOf(
                    "Resources.Load<GameObject>",
                    StringComparison.Ordinal) >= 0 &&
                runtime.IndexOf(
                    "procedural fallback",
                    StringComparison.Ordinal) >= 0,
                "Existing Resources assets or procedural fallback support is missing.",
                failures);
            Check(
                runtime.IndexOf("AddComponent<Light>", StringComparison.Ordinal) < 0 &&
                runtime.IndexOf("new Light", StringComparison.Ordinal) < 0,
                "The module adds a light despite its zero-light budget.",
                failures);
            Check(
                runtime.IndexOf("FindObjectsByType", StringComparison.Ordinal) < 0 &&
                runtime.IndexOf("FindObjectsOfType", StringComparison.Ordinal) < 0,
                "The module performs a scene-wide object search.",
                failures);
            Check(
                runtime.IndexOf("partial class PlayerAvatar", StringComparison.Ordinal) < 0 &&
                runtime.IndexOf("partial class PactCreature", StringComparison.Ordinal) < 0,
                "The module edits PlayerAvatar or PactCreature through a partial declaration.",
                failures);
            Check(
                CountOccurrences(runtime, "private void Update()") == 1,
                "Only the single coarse Skylance streaming director may own an Update loop.",
                failures);
            Check(
                runtime.IndexOf("Alpine shell art", StringComparison.Ordinal) >= 0 &&
                runtime.IndexOf("Aerie silhouette shell", StringComparison.Ordinal) >= 0 &&
                runtime.IndexOf("Aerie altitude detail", StringComparison.Ordinal) >= 0 &&
                runtime.IndexOf("Collision · always loaded", StringComparison.Ordinal) >= 0,
                "Art/collision separation or detail-tier roots are missing.",
                failures);
            Check(
                runtime.IndexOf(
                    "position.y >= TetherfallSkylanceWorld.KingdomDetailMinimumAltitude",
                    StringComparison.Ordinal) >= 0 &&
                runtime.IndexOf(
                    "KingdomDetailDistance *",
                    StringComparison.Ordinal) >= 0,
                "Expensive kingdom details are not gated by both altitude and distance.",
                failures);

            var initializeOnLoadToken = "Initialize" + "OnLoad";
            Check(
                editor.IndexOf(initializeOnLoadToken, StringComparison.Ordinal) < 0,
                "The verifier auto-runs on editor load.",
                failures);
            Check(
                editor.IndexOf("[MenuItem(", StringComparison.Ordinal) >= 0 &&
                editor.IndexOf(
                    "public static void VerifyFromCommandLine()",
                    StringComparison.Ordinal) >= 0,
                "The verifier lacks explicit menu and command-line entry points.",
                failures);
        }

        private static void AddStableId(
            string id,
            string prefix,
            HashSet<string> ids,
            List<string> failures)
        {
            Check(
                TetherfallSkylanceWorld.IsSemanticStableId(id) &&
                id.StartsWith(prefix, StringComparison.Ordinal) &&
                id.IndexOf('@') < 0 &&
                id.IndexOf(':') < 0,
                $"Stable ID '{id}' is not semantic or has a coordinate-derived suffix.",
                failures);
            Check(
                ids.Add(id),
                $"Stable ID '{id}' is duplicated.",
                failures);
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.0001f;
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return (left - right).sqrMagnitude <= 0.000001f;
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static int CountOccurrences(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            {
                return 0;
            }

            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(
                       value,
                       index,
                       StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private static void Check(
            bool condition,
            string message,
            List<string> failures)
        {
            assertionCount++;
            if (!condition)
            {
                failures.Add(message);
            }
        }
    }
}
