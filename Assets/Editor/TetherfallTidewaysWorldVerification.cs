using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Tetherfall;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tetherfall.Editor
{
    public static class TetherfallTidewaysWorldVerification
    {
        private const string MenuPath = "Tetherfall/Verify Tideways Maritime World";
        private const float PositionTolerance = 0.025f;
        private const float RouteSampleSpacing = 3f;
        private const float MaximumDryEdgeSpan = 70f;
        private static int assertionCount;

        [MenuItem(MenuPath, false, 2212)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Tideways maritime verification is edit-mode only. Stop Play Mode first.");
            }

            assertionCount = 0;
            var failures = new List<string>();
            VerifyPublicContract(failures);
            VerifySemanticIdsAndFiniteData(failures);
            VerifyWaterAndDryTopology(failures);
            VerifyParkourAndContent(failures);
            VerifyRouteContinuity(failures);
            VerifyLoopAndDockingMath(failures);
            VerifySafePlatformMath(failures);
            VerifySourceContracts(failures);
            VerifyIdempotentBuildAndBudgets(failures);

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Tetherfall Tideways maritime verification failed:\n- " +
                    string.Join("\n- ", failures));
            }

            Debug.Log(
                $"Tideways maritime verification passed: {assertionCount} assertions; " +
                $"{TetherfallTidewaysWorld.Waterways.Count} connected waterways, " +
                $"{TetherfallTidewaysWorld.Districts.Count} districts, " +
                $"{TetherfallTidewaysWorld.ShipRoutes.Count} deterministic boardable ships, " +
                "continuous dry topology, swimming triggers, docking loops, route recovery, " +
                "semantic content sockets, idempotence, and all published budgets held.");
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        private static void VerifyPublicContract(List<string> failures)
        {
            var type = typeof(TetherfallTidewaysWorld);
            var build = type.GetMethod(
                "Build",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(TetherfallGame) },
                null);
            Check(
                build != null && build.ReturnType == typeof(void),
                "Required public static void Build(TetherfallGame game) contract is missing.",
                failures);
            Check(
                type.GetMethod(
                    "IsBuilt",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(TetherfallGame) },
                    null) != null,
                "The idempotence query IsBuilt(TetherfallGame) is missing.",
                failures);
            Check(
                type.GetMethod(
                    "GetBuiltRoot",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(TetherfallGame) },
                    null) != null,
                "The stable root query GetBuiltRoot(TetherfallGame) is missing.",
                failures);
            Check(
                type.GetMethod(
                    "IsNavigableWater",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(Vector3), typeof(float) },
                    null) != null,
                "The navigable-water topology query is missing.",
                failures);
            Check(
                TetherfallTidewaysWorld.AddedRealtimeLightCount == 0 &&
                TetherfallTidewaysWorld.PerPieceUpdateLoopCount == 0 &&
                TetherfallTidewaysWorld.MaxActiveUpdateDrivers ==
                TetherfallTidewaysWorld.ExpectedShipCount + 1,
                "Published light or update-driver budgets do not match the central-streaming design.",
                failures);
            Check(
                TetherfallTidewaysWorld.MaxRendererCount <= 560 &&
                TetherfallTidewaysWorld.MaxDeliberateColliderCount <= 152 &&
                TetherfallTidewaysWorld.MaxSharedMaterialCount <= 40 &&
                TetherfallTidewaysWorld.MaxWaterVolumeCount <= 10,
                "Published renderer, collider, material, or water-volume ceilings drifted.",
                failures);
        }

        private static void VerifySemanticIdsAndFiniteData(List<string> failures)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            RegisterId(
                TetherfallTidewaysWorld.WorldId,
                "world.tideways.",
                "world",
                ids,
                failures);

            Check(
                TetherfallTidewaysWorld.WaterZones.Count ==
                TetherfallTidewaysWorld.ExpectedWaterZoneCount,
                "Water-zone catalog count does not match its published contract.",
                failures);
            for (var index = 0;
                 index < TetherfallTidewaysWorld.WaterZones.Count;
                 index++)
            {
                var zone = TetherfallTidewaysWorld.WaterZones[index];
                Check(zone != null, $"Water zone {index} is null.", failures);
                if (zone == null)
                {
                    continue;
                }

                RegisterId(
                    zone.Id,
                    "waterzone.tideways.",
                    "water zone",
                    ids,
                    failures);
                Check(
                    IsFinite(zone.Center) &&
                    IsFinite(zone.Size) &&
                    zone.Size.x > 0f &&
                    zone.Size.y > 0f &&
                    zone.Size.z > 0f,
                    $"Water zone '{zone.Id}' has invalid bounds.",
                    failures);
            }

            Check(
                TetherfallTidewaysWorld.Waterways.Count ==
                TetherfallTidewaysWorld.ExpectedWaterwayCount,
                "Waterway catalog count does not match its published contract.",
                failures);
            for (var index = 0;
                 index < TetherfallTidewaysWorld.Waterways.Count;
                 index++)
            {
                var waterway = TetherfallTidewaysWorld.Waterways[index];
                Check(waterway != null, $"Waterway {index} is null.", failures);
                if (waterway == null)
                {
                    continue;
                }

                RegisterId(
                    waterway.Id,
                    "waterway.tideways.",
                    "waterway",
                    ids,
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(waterway.DisplayName) &&
                    IsFinite(waterway.HalfWidth) &&
                    waterway.HalfWidth >= 4f &&
                    waterway.Points != null &&
                    waterway.Points.Count >= 2,
                    $"Waterway '{waterway.Id}' has incomplete navigable metadata.",
                    failures);
                for (var pointIndex = 0;
                     pointIndex < waterway.Points.Count;
                     pointIndex++)
                {
                    Check(
                        IsFinite(waterway.Points[pointIndex]) &&
                        Approximately(
                            waterway.Points[pointIndex].y,
                            TetherfallTidewaysWorld.WaterSurfaceY,
                            0.01f),
                        $"Waterway '{waterway.Id}' point {pointIndex + 1} is nonfinite or off the water plane.",
                        failures);
                }
            }

            Check(
                TetherfallTidewaysWorld.DryIslands.Count ==
                TetherfallTidewaysWorld.ExpectedDryIslandCount,
                "Dry-island catalog count does not match its published contract.",
                failures);
            for (var index = 0;
                 index < TetherfallTidewaysWorld.DryIslands.Count;
                 index++)
            {
                var island = TetherfallTidewaysWorld.DryIslands[index];
                Check(island != null, $"Dry island {index} is null.", failures);
                if (island == null)
                {
                    continue;
                }

                RegisterId(
                    island.Id,
                    "island.tideways.",
                    "dry island",
                    ids,
                    failures);
                Check(
                    IsFinite(island.Center) &&
                    IsFinite(island.Size) &&
                    island.Size.x >= 20f &&
                    island.Size.y >= 1f &&
                    island.Size.z >= 20f,
                    $"Dry island '{island.Id}' has invalid support geometry.",
                    failures);
            }

            Check(
                TetherfallTidewaysWorld.Districts.Count ==
                TetherfallTidewaysWorld.ExpectedDistrictCount,
                "District catalog count does not match its published contract.",
                failures);
            for (var index = 0;
                 index < TetherfallTidewaysWorld.Districts.Count;
                 index++)
            {
                var district = TetherfallTidewaysWorld.Districts[index];
                Check(district != null, $"District {index} is null.", failures);
                if (district == null)
                {
                    continue;
                }

                RegisterId(
                    district.Id,
                    "district.tideways.",
                    "district",
                    ids,
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(district.DisplayName) &&
                    !string.IsNullOrWhiteSpace(district.Character) &&
                    IsFinite(district.Center) &&
                    IsFinite(district.LandmarkPosition),
                    $"District '{district.Id}' has incomplete landmark authorship.",
                    failures);
            }

            for (var index = 0;
                 index < TetherfallTidewaysWorld.DryNodes.Count;
                 index++)
            {
                var node = TetherfallTidewaysWorld.DryNodes[index];
                Check(node != null, $"Dry node {index} is null.", failures);
                if (node == null)
                {
                    continue;
                }

                RegisterId(
                    node.Id,
                    "drynode.tideways.",
                    "dry node",
                    ids,
                    failures);
                Check(
                    IsFinite(node.Position),
                    $"Dry node '{node.Id}' has a nonfinite position.",
                    failures);
            }

            for (var index = 0;
                 index < TetherfallTidewaysWorld.DryEdges.Count;
                 index++)
            {
                var edge = TetherfallTidewaysWorld.DryEdges[index];
                Check(edge != null, $"Dry edge {index} is null.", failures);
                if (edge == null)
                {
                    continue;
                }

                RegisterId(
                    edge.Id,
                    "dryedge.tideways.",
                    "dry edge",
                    ids,
                    failures);
                Check(
                    IsFinite(edge.Width) && edge.Width >= 2.8f,
                    $"Dry edge '{edge.Id}' is too narrow for the PlayerAvatar controller.",
                    failures);
            }

            Check(
                TetherfallTidewaysWorld.Docks.Count ==
                TetherfallTidewaysWorld.ExpectedDockCount,
                "Dock catalog count does not match its published contract.",
                failures);
            for (var index = 0;
                 index < TetherfallTidewaysWorld.Docks.Count;
                 index++)
            {
                var dock = TetherfallTidewaysWorld.Docks[index];
                Check(dock != null, $"Dock {index} is null.", failures);
                if (dock == null)
                {
                    continue;
                }

                RegisterId(
                    dock.Id,
                    "dock.tideways.",
                    "dock",
                    ids,
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(dock.DisplayName) &&
                    IsFinite(dock.RoutePoint) &&
                    IsFinite(dock.DeckCenter) &&
                    IsFinite(dock.DeckSize) &&
                    IsFinite(dock.Yaw) &&
                    dock.DeckSize.x >= 6f &&
                    dock.DeckSize.y > 0f &&
                    dock.DeckSize.z >= 6f,
                    $"Dock '{dock.Id}' has invalid boarding geometry.",
                    failures);
            }

            Check(
                TetherfallTidewaysWorld.ShipRoutes.Count ==
                TetherfallTidewaysWorld.ExpectedShipCount,
                "Ship route count does not match the published ship count.",
                failures);
            for (var routeIndex = 0;
                 routeIndex < TetherfallTidewaysWorld.ShipRoutes.Count;
                 routeIndex++)
            {
                var route = TetherfallTidewaysWorld.ShipRoutes[routeIndex];
                Check(route != null, $"Ship route {routeIndex} is null.", failures);
                if (route == null)
                {
                    continue;
                }

                RegisterId(
                    route.Id,
                    "route.tideways.",
                    "ship route",
                    ids,
                    failures);
                RegisterId(
                    route.ShipId,
                    "ship.tideways.",
                    "ship",
                    ids,
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(route.DisplayName) &&
                    IsFinite(route.Speed) &&
                    route.Speed > 0f &&
                    IsFinite(route.DeckHalfExtents) &&
                    route.DeckHalfExtents.x >= 1.5f &&
                    route.DeckHalfExtents.y >= 3.4f &&
                    IsFinite(route.DeckSurfaceY) &&
                    route.DeckSurfaceY > 0.5f &&
                    IsFinite(route.VisualDistance) &&
                    IsFinite(route.SimulationDistance) &&
                    route.VisualDistance > 0f &&
                    route.SimulationDistance >= route.VisualDistance,
                    $"Ship route '{route.Id}' has invalid simulation or platform metadata.",
                    failures);
                Check(
                    route.Waypoints != null && route.Waypoints.Count >= 4,
                    $"Ship route '{route.Id}' has too few deterministic waypoints.",
                    failures);
                if (route.Waypoints == null)
                {
                    continue;
                }

                for (var waypointIndex = 0;
                     waypointIndex < route.Waypoints.Count;
                     waypointIndex++)
                {
                    var waypoint = route.Waypoints[waypointIndex];
                    Check(
                        waypoint != null,
                        $"Route '{route.Id}' waypoint {waypointIndex} is null.",
                        failures);
                    if (waypoint == null)
                    {
                        continue;
                    }

                    RegisterId(
                        waypoint.Id,
                        "waypoint.tideways.",
                        "ship waypoint",
                        ids,
                        failures);
                    Check(
                        IsFinite(waypoint.Position) &&
                        IsFinite(waypoint.DockSeconds) &&
                        waypoint.DockSeconds >= 0f,
                        $"Waypoint '{waypoint.Id}' has invalid position or docking time.",
                        failures);
                }
            }

            Check(
                TetherfallTidewaysWorld.ContentSockets.Count ==
                TetherfallTidewaysWorld.ExpectedContentSocketCount,
                "Content socket count does not match its published contract.",
                failures);
            for (var index = 0;
                 index < TetherfallTidewaysWorld.ContentSockets.Count;
                 index++)
            {
                var socket = TetherfallTidewaysWorld.ContentSockets[index];
                Check(socket != null, $"Content socket {index} is null.", failures);
                if (socket == null)
                {
                    continue;
                }

                RegisterId(
                    socket.Id,
                    SocketPrefix(socket.Kind),
                    "content socket",
                    ids,
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(socket.DisplayName) &&
                    !string.IsNullOrWhiteSpace(socket.InteractionPrompt) &&
                    !string.IsNullOrWhiteSpace(socket.InteractionText) &&
                    IsFinite(socket.Position),
                    $"Content socket '{socket.Id}' has incomplete authored metadata.",
                    failures);
            }
        }

        private static void VerifyWaterAndDryTopology(List<string> failures)
        {
            var waterTop =
                TetherfallTidewaysWorld.WaterVolumeCenterY +
                TetherfallTidewaysWorld.WaterVolumeHeight * 0.5f;
            Check(
                waterTop < 0.85f,
                "Swimming trigger top is high enough to wet raised dry walkways.",
                failures);
            Check(
                TetherfallTidewaysWorld.OceanSurfaceSize.x >= 340f &&
                TetherfallTidewaysWorld.OceanSurfaceSize.z >= 170f &&
                TetherfallTidewaysWorld.OceanSurfaceCenter.z < -500f,
                "Ocean extension no longer provides a large Tideglass-connected shipping field.",
                failures);

            var tideglassGate = FindWaterway("waterway.tideways.tideglass_gate");
            Check(tideglassGate != null, "Tideglass Gate waterway is missing.", failures);
            if (tideglassGate != null && tideglassGate.Points.Count >= 2)
            {
                var join = tideglassGate.Points[0];
                Check(
                    Mathf.Abs(join.x - 2f) <= 0.01f &&
                    Mathf.Abs(join.z + 438f) <= 0.01f,
                    "Tideglass Gate no longer meets the shipped Tideglass water edge.",
                    failures);
                Check(
                    TetherfallTidewaysWorld.IsNavigableWater(join) &&
                    TetherfallTidewaysWorld.IsNavigableWater(
                        tideglassGate.Points[tideglassGate.Points.Count - 1]),
                    "Tideglass connector endpoints are outside navigable-water topology.",
                    failures);
            }

            var nodeById =
                new Dictionary<string, TidewaysDryNodeDefinition>(StringComparer.Ordinal);
            var adjacency =
                new Dictionary<string, List<string>>(StringComparer.Ordinal);
            for (var index = 0;
                 index < TetherfallTidewaysWorld.DryNodes.Count;
                 index++)
            {
                var node = TetherfallTidewaysWorld.DryNodes[index];
                if (node == null)
                {
                    continue;
                }

                nodeById[node.Id] = node;
                adjacency[node.Id] = new List<string>();
                Check(
                    node.Position.y - 0.14f > waterTop + 0.08f,
                    $"Dry node '{node.Id}' does not clear swimming triggers.",
                    failures);
            }

            var archedBridgeCount = 0;
            for (var index = 0;
                 index < TetherfallTidewaysWorld.DryEdges.Count;
                 index++)
            {
                var edge = TetherfallTidewaysWorld.DryEdges[index];
                if (edge == null)
                {
                    continue;
                }

                var hasFrom = nodeById.TryGetValue(edge.FromNodeId, out var from);
                var hasTo = nodeById.TryGetValue(edge.ToNodeId, out var to);
                Check(
                    hasFrom && hasTo,
                    $"Dry edge '{edge.Id}' references an unknown endpoint.",
                    failures);
                if (!hasFrom || !hasTo)
                {
                    continue;
                }

                adjacency[from.Id].Add(to.Id);
                adjacency[to.Id].Add(from.Id);
                var span = HorizontalDistance(from.Position, to.Position);
                Check(
                    span > 1f && span <= MaximumDryEdgeSpan,
                    $"Dry edge '{edge.Id}' span {span:0.##} is unsupported.",
                    failures);
                if (edge.Style == TidewaysDryRouteStyle.ArchedBridge)
                {
                    archedBridgeCount++;
                }
            }

            Check(
                archedBridgeCount ==
                TetherfallTidewaysWorld.ExpectedArchedBridgeCount,
                $"Expected {TetherfallTidewaysWorld.ExpectedArchedBridgeCount} arched bridges, " +
                $"found {archedBridgeCount}.",
                failures);
            Check(
                nodeById.ContainsKey("drynode.tideways.tideglass_causeway"),
                "Dry network has no Tideglass causeway entry.",
                failures);

            var visited = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>();
            const string entryId = "drynode.tideways.tideglass_causeway";
            if (adjacency.ContainsKey(entryId))
            {
                visited.Add(entryId);
                queue.Enqueue(entryId);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var neighbors = adjacency[current];
                for (var index = 0; index < neighbors.Count; index++)
                {
                    if (visited.Add(neighbors[index]))
                    {
                        queue.Enqueue(neighbors[index]);
                    }
                }
            }

            Check(
                visited.Count == nodeById.Count && nodeById.Count >= 13,
                $"Dry walkway graph reaches {visited.Count}/{nodeById.Count} nodes from Tideglass.",
                failures);

            for (var islandIndex = 0;
                 islandIndex < TetherfallTidewaysWorld.DryIslands.Count;
                 islandIndex++)
            {
                var island = TetherfallTidewaysWorld.DryIslands[islandIndex];
                if (island == null)
                {
                    continue;
                }

                for (var waterwayIndex = 1;
                     waterwayIndex < TetherfallTidewaysWorld.Waterways.Count;
                     waterwayIndex++)
                {
                    var waterway = TetherfallTidewaysWorld.Waterways[waterwayIndex];
                    for (var pointIndex = 0;
                         pointIndex + 1 < waterway.Points.Count;
                         pointIndex++)
                    {
                        var midpoint = Vector3.Lerp(
                            waterway.Points[pointIndex],
                            waterway.Points[pointIndex + 1],
                            0.5f);
                        Check(
                            !ContainsHorizontal(island, midpoint, -0.2f),
                            $"Dry island '{island.Id}' blocks waterway '{waterway.Id}'.",
                            failures);
                    }
                }
            }

            for (var dockIndex = 0;
                 dockIndex < TetherfallTidewaysWorld.Docks.Count;
                 dockIndex++)
            {
                var dock = TetherfallTidewaysWorld.Docks[dockIndex];
                if (dock == null)
                {
                    continue;
                }

                Check(
                    TetherfallTidewaysWorld.IsNavigableWater(dock.RoutePoint, 0.25f),
                    $"Dock '{dock.Id}' route point is outside swimmable/navigable water.",
                    failures);
                Check(
                    dock.DeckCenter.y - dock.DeckSize.y * 0.5f > waterTop,
                    $"Dock '{dock.Id}' deck intersects the swimming-trigger ceiling.",
                    failures);
            }
        }

        private static void VerifyParkourAndContent(List<string> failures)
        {
            Check(
                TetherfallTidewaysWorld.ParkourRoutes.Count ==
                TetherfallTidewaysWorld.ExpectedParkourRouteCount,
                "Parkour route count does not match its published contract.",
                failures);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var routeIndex = 0;
                 routeIndex < TetherfallTidewaysWorld.ParkourRoutes.Count;
                 routeIndex++)
            {
                var route = TetherfallTidewaysWorld.ParkourRoutes[routeIndex];
                Check(route != null, $"Parkour route {routeIndex} is null.", failures);
                if (route == null)
                {
                    continue;
                }

                RegisterId(
                    route.Id,
                    "parkour.tideways.",
                    "parkour route",
                    ids,
                    failures);
                Check(
                    !string.IsNullOrWhiteSpace(route.DisplayName) &&
                    route.Points != null &&
                    route.Points.Count >= 6,
                    $"Parkour route '{route.Id}' lacks an authored route.",
                    failures);
                for (var pointIndex = 0;
                     pointIndex < route.Points.Count;
                     pointIndex++)
                {
                    Check(
                        IsFinite(route.Points[pointIndex]) &&
                        route.Points[pointIndex].y >= 1.2f,
                        $"Parkour route '{route.Id}' point {pointIndex + 1} is invalid.",
                        failures);
                    if (pointIndex + 1 < route.Points.Count)
                    {
                        var span = Vector3.Distance(
                            route.Points[pointIndex],
                            route.Points[pointIndex + 1]);
                        Check(
                            span > 0.5f &&
                            span <= TetherfallTidewaysWorld.MaximumParkourSpan,
                            $"Parkour route '{route.Id}' span {pointIndex + 1} is {span:0.##}; " +
                            $"budget is {TetherfallTidewaysWorld.MaximumParkourSpan:0.##}.",
                            failures);
                    }
                }
            }

            var kindCoverage = new HashSet<TidewaysContentSocketKind>();
            for (var index = 0;
                 index < TetherfallTidewaysWorld.ContentSockets.Count;
                 index++)
            {
                var socket = TetherfallTidewaysWorld.ContentSockets[index];
                if (socket != null)
                {
                    kindCoverage.Add(socket.Kind);
                }
            }

            Check(
                kindCoverage.Contains(TidewaysContentSocketKind.Quest) &&
                kindCoverage.Contains(TidewaysContentSocketKind.Enemy) &&
                kindCoverage.Contains(TidewaysContentSocketKind.Cargo) &&
                kindCoverage.Contains(TidewaysContentSocketKind.Vendor) &&
                kindCoverage.Contains(TidewaysContentSocketKind.Activity) &&
                kindCoverage.Contains(TidewaysContentSocketKind.SeaBoss),
                "Stable sockets do not cover quests, enemies, cargo, vendors, activities, and sea boss.",
                failures);
            Check(
                CountSocketKind(TidewaysContentSocketKind.SeaBoss) == 1,
                "The vertical slice must expose exactly one unambiguous sea-boss socket.",
                failures);
        }

        private static void VerifyRouteContinuity(List<string> failures)
        {
            for (var routeIndex = 0;
                 routeIndex < TetherfallTidewaysWorld.ShipRoutes.Count;
                 routeIndex++)
            {
                var route = TetherfallTidewaysWorld.ShipRoutes[routeIndex];
                if (route?.Waypoints == null || route.Waypoints.Count < 2)
                {
                    continue;
                }

                var dockingCount = 0;
                var routeLength = 0f;
                for (var waypointIndex = 0;
                     waypointIndex < route.Waypoints.Count;
                     waypointIndex++)
                {
                    var current = route.Waypoints[waypointIndex];
                    var next = route.Waypoints[
                        (waypointIndex + 1) % route.Waypoints.Count];
                    if (current == null || next == null)
                    {
                        continue;
                    }

                    if (current.DockSeconds > 0f)
                    {
                        dockingCount++;
                        var dock = FindDock(current.DockId);
                        Check(
                            dock != null,
                            $"Waypoint '{current.Id}' references missing dock '{current.DockId}'.",
                            failures);
                        if (dock != null)
                        {
                            Check(
                                HorizontalDistance(current.Position, dock.RoutePoint) <= 0.05f,
                                $"Waypoint '{current.Id}' is not aligned with dock '{dock.Id}'.",
                                failures);
                        }
                    }
                    else
                    {
                        Check(
                            string.IsNullOrEmpty(current.DockId),
                            $"Waypoint '{current.Id}' names a dock without a docking pause.",
                            failures);
                    }

                    var length = HorizontalDistance(
                        current.Position,
                        next.Position);
                    routeLength += length;
                    Check(
                        length >= 8f && length <= 175f,
                        $"Route '{route.Id}' segment {waypointIndex + 1} has invalid span {length:0.##}.",
                        failures);
                    var samples = Mathf.Max(
                        1,
                        Mathf.CeilToInt(length / RouteSampleSpacing));
                    for (var sampleIndex = 0;
                         sampleIndex <= samples;
                         sampleIndex++)
                    {
                        var sample = Vector3.Lerp(
                            current.Position,
                            next.Position,
                            sampleIndex / (float)samples);
                        Check(
                            TetherfallTidewaysWorld.IsNavigableWater(sample, 0.35f),
                            $"Route '{route.Id}' leaves navigable water on segment " +
                            $"{waypointIndex + 1} sample {sampleIndex + 1}/{samples + 1}.",
                            failures);
                    }
                }

                Check(
                    dockingCount >= 2,
                    $"Route '{route.Id}' has only {dockingCount} docking pauses.",
                    failures);
                Check(
                    routeLength >= 90f,
                    $"Route '{route.Id}' loop is too short to prove maritime movement.",
                    failures);
            }
        }

        private static void VerifyLoopAndDockingMath(List<string> failures)
        {
            for (var routeIndex = 0;
                 routeIndex < TetherfallTidewaysWorld.ShipRoutes.Count;
                 routeIndex++)
            {
                var route = TetherfallTidewaysWorld.ShipRoutes[routeIndex];
                if (!TetherfallTidewaysRouteMath.HasUsableRoute(route))
                {
                    Check(false, $"Route {routeIndex} is unusable by route math.", failures);
                    continue;
                }

                var initial = TetherfallTidewaysRouteMath.CreateInitial(route);
                TetherfallTidewaysRouteMath.Evaluate(
                    route,
                    initial,
                    out var initialPosition,
                    out var initialForward);
                Check(
                    IsFinite(initialPosition) &&
                    IsFinite(initialForward) &&
                    initialForward.sqrMagnitude > 0.99f,
                    $"Route '{route.Id}' initial pose is invalid.",
                    failures);

                var loopDuration =
                    TetherfallTidewaysRouteMath.CalculateLoopDuration(route);
                Check(
                    IsFinite(loopDuration) && loopDuration > 10f && loopDuration < 600f,
                    $"Route '{route.Id}' has invalid loop duration {loopDuration:0.###}.",
                    failures);
                var looped = TetherfallTidewaysRouteMath.Advance(
                    route,
                    initial,
                    loopDuration,
                    out var loopedPosition,
                    out var loopedForward);
                Check(
                    Vector3.Distance(initialPosition, loopedPosition) <= PositionTolerance &&
                    Vector3.Angle(initialForward, loopedForward) <= 0.1f &&
                    looped.SegmentStartIndex == initial.SegmentStartIndex &&
                    Approximately(
                        looped.DistanceAlongSegment,
                        initial.DistanceAlongSegment,
                        PositionTolerance) &&
                    Approximately(
                        looped.DockRemaining,
                        initial.DockRemaining,
                        PositionTolerance),
                    $"Route '{route.Id}' does not return to its initial docking state after one loop.",
                    failures);

                var firstPause = route.Waypoints[0].DockSeconds;
                Check(
                    firstPause > 0f,
                    $"Route '{route.Id}' does not begin at a deterministic dock.",
                    failures);
                if (firstPause > 0f)
                {
                    var paused = TetherfallTidewaysRouteMath.Advance(
                        route,
                        initial,
                        firstPause * 0.5f,
                        out var pausedPosition,
                        out _);
                    Check(
                        Vector3.Distance(pausedPosition, initialPosition) <= PositionTolerance &&
                        paused.DockRemaining > 0f &&
                        paused.DockRemaining < firstPause,
                        $"Route '{route.Id}' moved during its opening docking pause.",
                        failures);
                }

                const float deterministicWindow = 37.25f;
                var single = TetherfallTidewaysRouteMath.Advance(
                    route,
                    initial,
                    deterministicWindow,
                    out var singlePosition,
                    out var singleForward);
                var stepped = initial;
                var steppedPosition = initialPosition;
                var steppedForward = initialForward;
                const float deterministicStep = 0.25f;
                var remaining = deterministicWindow;
                while (remaining > 0.0001f)
                {
                    var step = Mathf.Min(deterministicStep, remaining);
                    stepped = TetherfallTidewaysRouteMath.Advance(
                        route,
                        stepped,
                        step,
                        out steppedPosition,
                        out steppedForward);
                    remaining -= step;
                }

                Check(
                    Vector3.Distance(singlePosition, steppedPosition) <= 0.04f &&
                    Vector3.Angle(singleForward, steppedForward) <= 0.1f &&
                    single.SegmentStartIndex == stepped.SegmentStartIndex &&
                    Approximately(
                        single.DistanceAlongSegment,
                        stepped.DistanceAlongSegment,
                        0.04f) &&
                    Approximately(single.DockRemaining, stepped.DockRemaining, 0.04f),
                    $"Route '{route.Id}' changes result when the same time is subdivided.",
                    failures);

                var recovered =
                    TetherfallTidewaysRouteMath.RecoverToNearestWaypoint(
                        route,
                        new Vector3(float.NaN, float.PositiveInfinity, 0f));
                TetherfallTidewaysRouteMath.Evaluate(
                    route,
                    recovered,
                    out var recoveredPosition,
                    out var recoveredForward);
                Check(
                    IsFinite(recoveredPosition) &&
                    IsFinite(recoveredForward) &&
                    recovered.SegmentStartIndex >= 0 &&
                    recovered.SegmentStartIndex < route.Waypoints.Count,
                    $"Route '{route.Id}' cannot recover from a nonfinite pose.",
                    failures);
            }
        }

        private static void VerifySafePlatformMath(List<string> failures)
        {
            for (var routeIndex = 0;
                 routeIndex < TetherfallTidewaysWorld.ShipRoutes.Count;
                 routeIndex++)
            {
                var route = TetherfallTidewaysWorld.ShipRoutes[routeIndex];
                var worstCase =
                    TetherfallTidewaysRouteMath.CalculateWorstCasePlatformDelta(route);
                Check(
                    IsFinite(worstCase) &&
                    worstCase <= TetherfallTidewaysWorld.MaximumSafePlatformDelta,
                    $"Ship '{route?.ShipId}' worst near-frame platform delta is " +
                    $"{worstCase:0.###}, above " +
                    $"{TetherfallTidewaysWorld.MaximumSafePlatformDelta:0.###}.",
                    failures);
            }

            var limited = TetherfallTidewaysRouteMath.SanitizePlatformDelta(
                new Vector3(100f, -25f, 10f),
                TetherfallTidewaysWorld.MaximumSafePlatformDelta);
            Check(
                IsFinite(limited) &&
                limited.magnitude <=
                TetherfallTidewaysWorld.MaximumSafePlatformDelta + 0.0001f,
                "Safe platform-delta sanitizer does not clamp large movement.",
                failures);
            var rejected = TetherfallTidewaysRouteMath.SanitizePlatformDelta(
                new Vector3(float.NaN, 0f, 0f),
                TetherfallTidewaysWorld.MaximumSafePlatformDelta);
            Check(
                rejected == Vector3.zero,
                "Safe platform-delta sanitizer does not reject nonfinite movement.",
                failures);
            Check(
                TetherfallTidewaysWorld.MaximumNearSimulationStep <= 0.05f &&
                TetherfallTidewaysWorld.MaximumShipTurnDegreesPerSecond <= 18f &&
                TetherfallTidewaysWorld.RouteRecoveryDistance <= 18f,
                "Near simulation, turning, or route-recovery limits exceed the safe platform contract.",
                failures);
        }

        private static void VerifySourceContracts(List<string> failures)
        {
            var runtimePath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "TetherfallTidewaysWorld.cs");
            Check(
                File.Exists(runtimePath),
                "Tideways runtime source file is missing.",
                failures);
            if (!File.Exists(runtimePath))
            {
                return;
            }

            var source = File.ReadAllText(runtimePath);
            Check(
                source.IndexOf(
                    "RuntimeInitializeOnLoadMethod",
                    StringComparison.Ordinal) < 0,
                "Tideways must not auto-run through RuntimeInitializeOnLoadMethod.",
                failures);
            Check(
                source.IndexOf(
                    "[DefaultExecutionOrder(-120)]",
                    StringComparison.Ordinal) >= 0,
                "Ship execution order no longer precedes PlayerAvatar movement.",
                failures);
            Check(
                source.IndexOf(
                    "Rigidbody",
                    StringComparison.Ordinal) >= 0,
                "Source contract no longer documents/guards against Rigidbody ship motion.",
                failures);
            Check(
                source.IndexOf(
                    "root.AddComponent<Rigidbody>",
                    StringComparison.Ordinal) < 0 &&
                source.IndexOf(
                    "AddComponent<Rigidbody>",
                    StringComparison.Ordinal) < 0,
                "Tideways source adds a Rigidbody to moving ships.",
                failures);

            var shipClass = ExtractBlock(
                source,
                "public sealed class TetherfallTidewaysShip");
            Check(
                !string.IsNullOrEmpty(shipClass),
                "Could not statically inspect the ship class.",
                failures);
            if (string.IsNullOrEmpty(shipClass))
            {
                return;
            }

            VerifyAllocationFreeMethod(shipClass, "private void Update()", failures);
            VerifyAllocationFreeMethod(
                shipClass,
                "private void MoveShip(float deltaTime, PlayerAvatar player, bool supported)",
                failures);
            VerifyAllocationFreeMethod(
                shipClass,
                "private bool IsPlayerSupported(PlayerAvatar player)",
                failures);
            VerifyAllocationFreeMethod(
                shipClass,
                "private void SetPresentation(bool active)",
                failures);
        }

        private static void VerifyAllocationFreeMethod(
            string classSource,
            string signature,
            List<string> failures)
        {
            var body = ExtractBlock(classSource, signature);
            Check(
                !string.IsNullOrEmpty(body),
                $"Could not inspect allocation-free method '{signature}'.",
                failures);
            if (string.IsNullOrEmpty(body))
            {
                return;
            }

            var forbiddenTokens = new[]
            {
                "new ",
                "foreach",
                ".ToArray(",
                ".ToList(",
                "string.Format",
                "$\"",
                "GetComponents<",
                "FindObjects"
            };
            for (var index = 0; index < forbiddenTokens.Length; index++)
            {
                Check(
                    body.IndexOf(forbiddenTokens[index], StringComparison.Ordinal) < 0,
                    $"Ship method '{signature}' contains allocation-risk token " +
                    $"'{forbiddenTokens[index]}'.",
                    failures);
            }
        }

        private static void VerifyIdempotentBuildAndBudgets(List<string> failures)
        {
            var fixture = new GameObject("Tideways Verification Fixture");
            var game = fixture.AddComponent<TetherfallGame>();
            try
            {
                TetherfallTidewaysWorld.Build(game);
                var firstRoot = TetherfallTidewaysWorld.GetBuiltRoot(game);
                Check(firstRoot != null, "Build did not create a Tideways root.", failures);
                if (firstRoot == null)
                {
                    return;
                }

                var firstSnapshot = Capture(firstRoot);
                TetherfallTidewaysWorld.Build(game);
                var secondRoot = TetherfallTidewaysWorld.GetBuiltRoot(game);
                var secondSnapshot = Capture(secondRoot);
                Check(
                    ReferenceEquals(firstRoot, secondRoot),
                    "Repeated Build replaced the stable Tideways root.",
                    failures);
                Check(
                    firstSnapshot.Equals(secondSnapshot),
                    "Repeated Build changed Tideways hierarchy or budget metrics.",
                    failures);
                Check(
                    CountDirectRoots(game.transform) == 1 &&
                    TetherfallTidewaysWorld.IsBuilt(game),
                    "Repeated Build created duplicate roots or broke IsBuilt.",
                    failures);

                VerifyBuiltRoot(firstRoot, failures);
            }
            catch (Exception exception)
            {
                Check(
                    false,
                    $"Edit-mode Build threw {exception.GetType().Name}: {exception.Message}",
                    failures);
            }
            finally
            {
                Object.DestroyImmediate(fixture);
            }
        }

        private static void VerifyBuiltRoot(
            TetherfallTidewaysWorldRoot marker,
            List<string> failures)
        {
            var root = marker.gameObject;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var colliders = root.GetComponentsInChildren<Collider>(true);
            var waterVolumes = root.GetComponentsInChildren<WaterVolume>(true);
            var waterIdentities =
                root.GetComponentsInChildren<TetherfallTidewaysWaterVolumeIdentity>(true);
            var ships =
                root.GetComponentsInChildren<TetherfallTidewaysShip>(true);
            var sockets =
                root.GetComponentsInChildren<TetherfallTidewaysContentSocket>(true);
            var bridges =
                root.GetComponentsInChildren<TetherfallTidewaysBridgeIdentity>(true);
            var clusters =
                root.GetComponentsInChildren<TetherfallTidewaysVisualCluster>(true);
            var streaming =
                root.GetComponentsInChildren<TetherfallTidewaysStreaming>(true);
            var imported =
                root.GetComponentsInChildren<TetherfallTidewaysImportedIdentity>(true);

            Check(
                string.Equals(root.name, TetherfallTidewaysWorld.RootName, StringComparison.Ordinal) &&
                string.Equals(marker.WorldId, TetherfallTidewaysWorld.WorldId, StringComparison.Ordinal) &&
                root.transform.parent != null &&
                root.transform.localPosition == Vector3.zero &&
                Quaternion.Angle(root.transform.localRotation, Quaternion.identity) < 0.001f &&
                Vector3.SqrMagnitude(root.transform.localScale - Vector3.one) < 0.000001f,
                "Built root does not preserve its stable identity transform.",
                failures);
            Check(
                renderers.Length >= 400 &&
                renderers.Length <= TetherfallTidewaysWorld.MaxRendererCount &&
                marker.RendererCount == renderers.Length,
                $"Renderer count {renderers.Length} is outside the substantive/budgeted range.",
                failures);
            Check(
                colliders.Length >= 120 &&
                colliders.Length <=
                TetherfallTidewaysWorld.MaxDeliberateColliderCount &&
                marker.ColliderCount == colliders.Length,
                $"Collider count {colliders.Length} is outside the deliberate budget.",
                failures);
            Check(
                marker.SharedMaterialCount > 0 &&
                marker.SharedMaterialCount <=
                TetherfallTidewaysWorld.MaxSharedMaterialCount,
                $"Shared material count {marker.SharedMaterialCount} exceeds its budget.",
                failures);
            Check(
                root.GetComponentsInChildren<Light>(true).Length ==
                TetherfallTidewaysWorld.AddedRealtimeLightCount &&
                marker.LightCount == TetherfallTidewaysWorld.AddedRealtimeLightCount,
                "Tideways added a realtime Light.",
                failures);
            Check(
                waterVolumes.Length == TetherfallTidewaysWorld.MaxWaterVolumeCount &&
                waterIdentities.Length == waterVolumes.Length &&
                marker.WaterVolumeCount == waterVolumes.Length,
                $"Expected {TetherfallTidewaysWorld.MaxWaterVolumeCount} identified water volumes; " +
                $"found {waterVolumes.Length}/{waterIdentities.Length}.",
                failures);
            Check(
                ships.Length == TetherfallTidewaysWorld.ExpectedShipCount &&
                marker.ShipCount == ships.Length,
                $"Expected {TetherfallTidewaysWorld.ExpectedShipCount} ships, found {ships.Length}.",
                failures);
            Check(
                sockets.Length == TetherfallTidewaysWorld.ExpectedContentSocketCount &&
                marker.ContentSocketCount == sockets.Length,
                $"Expected {TetherfallTidewaysWorld.ExpectedContentSocketCount} content sockets, " +
                $"found {sockets.Length}.",
                failures);
            Check(
                bridges.Length == TetherfallTidewaysWorld.ExpectedArchedBridgeCount,
                $"Expected {TetherfallTidewaysWorld.ExpectedArchedBridgeCount} built arch identities, " +
                $"found {bridges.Length}.",
                failures);
            Check(
                clusters.Length == 5 &&
                streaming.Length == 1 &&
                streaming[0].ClusterCount == clusters.Length &&
                marker.UpdateDriverCount ==
                ships.Length + streaming.Length &&
                marker.UpdateDriverCount <=
                TetherfallTidewaysWorld.MaxActiveUpdateDrivers,
                "Central static streaming or update-driver accounting is inconsistent.",
                failures);
            Check(
                imported.Length ==
                TetherfallTidewaysWorld.ExpectedImportedPlacementCount &&
                marker.ImportedPlacementCount == imported.Length &&
                marker.ImportedFallbackCount >= 0 &&
                marker.ImportedFallbackCount <= imported.Length,
                "Quaternius placement/fallback accounting is inconsistent.",
                failures);
            Check(
                root.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                "A moving ship or world piece contains a Rigidbody.",
                failures);
            Check(
                root.GetComponentsInChildren<Camera>(true).Length == 0 &&
                root.GetComponentsInChildren<AudioListener>(true).Length == 0 &&
                root.GetComponentsInChildren<Animator>(true).Length == 0 &&
                root.GetComponentsInChildren<Animation>(true).Length == 0,
                "Imported or procedural art contains a forbidden camera/listener/animation component.",
                failures);

            var shipIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < ships.Length; index++)
            {
                var ship = ships[index];
                var deck = ship.GetComponent<BoxCollider>();
                var identity =
                    ship.GetComponent<TetherfallTidewaysDeliberateCollider>();
                Check(
                    shipIds.Add(ship.ShipId) &&
                    deck != null &&
                    !deck.isTrigger &&
                    identity != null &&
                    identity.IsMovingPlatform &&
                    ship.GetComponent<Rigidbody>() == null,
                    $"Ship '{ship.ShipId}' is duplicated or lacks a Rigidbody-free boardable deck.",
                    failures);
            }

            for (var index = 0; index < waterVolumes.Length; index++)
            {
                var box = waterVolumes[index].GetComponent<BoxCollider>();
                Check(
                    box != null &&
                    box.isTrigger &&
                    box.size.y >= TetherfallTidewaysWorld.WaterVolumeHeight - 0.01f,
                    $"Water volume '{waterVolumes[index].name}' does not preserve swimming depth.",
                    failures);
            }

            var nonTriggerColliders = 0;
            for (var index = 0; index < colliders.Length; index++)
            {
                var collider = colliders[index];
                if (collider == null || collider.isTrigger)
                {
                    continue;
                }

                nonTriggerColliders++;
                Check(
                    collider.GetComponent<TetherfallTidewaysDeliberateCollider>() != null,
                    $"Solid collider '{HierarchyPath(collider.transform)}' lacks deliberate metadata.",
                    failures);
            }

            Check(
                nonTriggerColliders >= 110,
                "Built world does not contain the expected continuous deliberate support shell.",
                failures);
            VerifyFiniteHierarchy(root.transform, failures);
        }

        private static BuildSnapshot Capture(TetherfallTidewaysWorldRoot marker)
        {
            if (marker == null)
            {
                return default;
            }

            return new BuildSnapshot(
                marker.transform.childCount,
                marker.GetComponentsInChildren<Transform>(true).Length,
                marker.RendererCount,
                marker.ColliderCount,
                marker.SharedMaterialCount,
                marker.WaterVolumeCount,
                marker.ShipCount,
                marker.ContentSocketCount,
                marker.ImportedPlacementCount,
                marker.ImportedFallbackCount,
                marker.UpdateDriverCount);
        }

        private static void VerifyFiniteHierarchy(
            Transform root,
            List<string> failures)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < transforms.Length; index++)
            {
                var current = transforms[index];
                Check(
                    IsFinite(current.position) &&
                    IsFinite(current.lossyScale) &&
                    current.lossyScale.x > 0f &&
                    current.lossyScale.y > 0f &&
                    current.lossyScale.z > 0f &&
                    IsFinite(current.rotation),
                    $"Built transform '{HierarchyPath(current)}' is nonfinite or non-positive.",
                    failures);
            }
        }

        private static string ExtractBlock(string source, string signature)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(signature))
            {
                return string.Empty;
            }

            var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
            if (signatureIndex < 0)
            {
                return string.Empty;
            }

            var openIndex = source.IndexOf('{', signatureIndex + signature.Length);
            if (openIndex < 0)
            {
                return string.Empty;
            }

            var depth = 0;
            for (var index = openIndex; index < source.Length; index++)
            {
                var value = source[index];
                if (value == '{')
                {
                    depth++;
                }
                else if (value == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source.Substring(
                            signatureIndex,
                            index - signatureIndex + 1);
                    }
                }
            }

            return string.Empty;
        }

        private static int CountDirectRoots(Transform parent)
        {
            var count = 0;
            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child != null &&
                    child.GetComponent<TetherfallTidewaysWorldRoot>() != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountSocketKind(TidewaysContentSocketKind kind)
        {
            var count = 0;
            for (var index = 0;
                 index < TetherfallTidewaysWorld.ContentSockets.Count;
                 index++)
            {
                if (TetherfallTidewaysWorld.ContentSockets[index]?.Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static TidewaysWaterwayDefinition FindWaterway(string id)
        {
            for (var index = 0;
                 index < TetherfallTidewaysWorld.Waterways.Count;
                 index++)
            {
                var waterway = TetherfallTidewaysWorld.Waterways[index];
                if (waterway != null &&
                    string.Equals(waterway.Id, id, StringComparison.Ordinal))
                {
                    return waterway;
                }
            }

            return null;
        }

        private static TidewaysDockDefinition FindDock(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (var index = 0;
                 index < TetherfallTidewaysWorld.Docks.Count;
                 index++)
            {
                var dock = TetherfallTidewaysWorld.Docks[index];
                if (dock != null &&
                    string.Equals(dock.Id, id, StringComparison.Ordinal))
                {
                    return dock;
                }
            }

            return null;
        }

        private static bool ContainsHorizontal(
            TidewaysDryIslandDefinition island,
            Vector3 point,
            float margin)
        {
            return island != null &&
                   Mathf.Abs(point.x - island.Center.x) <=
                   island.Size.x * 0.5f + margin &&
                   Mathf.Abs(point.z - island.Center.z) <=
                   island.Size.z * 0.5f + margin;
        }

        private static string SocketPrefix(TidewaysContentSocketKind kind)
        {
            switch (kind)
            {
                case TidewaysContentSocketKind.Quest:
                    return "quest.tideways.";
                case TidewaysContentSocketKind.Enemy:
                    return "enemy.tideways.";
                case TidewaysContentSocketKind.Cargo:
                    return "cargo.tideways.";
                case TidewaysContentSocketKind.Vendor:
                    return "vendor.tideways.";
                case TidewaysContentSocketKind.Activity:
                    return "activity.tideways.";
                case TidewaysContentSocketKind.SeaBoss:
                    return "boss.tideways.";
                default:
                    return string.Empty;
            }
        }

        private static void RegisterId(
            string id,
            string requiredPrefix,
            string label,
            HashSet<string> ids,
            List<string> failures)
        {
            Check(
                IsSemanticId(id, requiredPrefix),
                $"{label} ID '{id}' is not a stable semantic ID.",
                failures);
            Check(
                ids.Add(id ?? string.Empty),
                $"{label} ID '{id}' is duplicated.",
                failures);
        }

        private static bool IsSemanticId(string id, string requiredPrefix)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrEmpty(requiredPrefix) ||
                !id.StartsWith(requiredPrefix, StringComparison.Ordinal) ||
                id.IndexOf('@') >= 0 ||
                id.IndexOf(':') >= 0 ||
                id.IndexOf(' ') >= 0)
            {
                return false;
            }

            return !Regex.IsMatch(
                id,
                @"(?:^|[._-])(?:x|y|z|pos|coord)-?\d",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right)
        {
            var x = left.x - right.x;
            var z = left.z - right.z;
            return Mathf.Sqrt(x * x + z * z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
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

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z) &&
                   IsFinite(value.w);
        }

        private static bool Approximately(
            float left,
            float right,
            float tolerance)
        {
            return Mathf.Abs(left - right) <= tolerance;
        }

        private static string HierarchyPath(Transform value)
        {
            if (value == null)
            {
                return "<null>";
            }

            var path = value.name;
            var current = value.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private static void Check(
            bool condition,
            string failure,
            List<string> failures)
        {
            assertionCount++;
            if (!condition)
            {
                failures.Add(failure);
            }
        }

        private readonly struct BuildSnapshot : IEquatable<BuildSnapshot>
        {
            private readonly int directChildren;
            private readonly int transforms;
            private readonly int renderers;
            private readonly int colliders;
            private readonly int materials;
            private readonly int waterVolumes;
            private readonly int ships;
            private readonly int sockets;
            private readonly int imported;
            private readonly int fallbacks;
            private readonly int updateDrivers;

            public BuildSnapshot(
                int directChildren,
                int transforms,
                int renderers,
                int colliders,
                int materials,
                int waterVolumes,
                int ships,
                int sockets,
                int imported,
                int fallbacks,
                int updateDrivers)
            {
                this.directChildren = directChildren;
                this.transforms = transforms;
                this.renderers = renderers;
                this.colliders = colliders;
                this.materials = materials;
                this.waterVolumes = waterVolumes;
                this.ships = ships;
                this.sockets = sockets;
                this.imported = imported;
                this.fallbacks = fallbacks;
                this.updateDrivers = updateDrivers;
            }

            public bool Equals(BuildSnapshot other)
            {
                return directChildren == other.directChildren &&
                       transforms == other.transforms &&
                       renderers == other.renderers &&
                       colliders == other.colliders &&
                       materials == other.materials &&
                       waterVolumes == other.waterVolumes &&
                       ships == other.ships &&
                       sockets == other.sockets &&
                       imported == other.imported &&
                       fallbacks == other.fallbacks &&
                       updateDrivers == other.updateDrivers;
            }

            public override bool Equals(object obj)
            {
                return obj is BuildSnapshot other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = directChildren;
                    hash = hash * 397 ^ transforms;
                    hash = hash * 397 ^ renderers;
                    hash = hash * 397 ^ colliders;
                    hash = hash * 397 ^ materials;
                    hash = hash * 397 ^ waterVolumes;
                    hash = hash * 397 ^ ships;
                    hash = hash * 397 ^ sockets;
                    hash = hash * 397 ^ imported;
                    hash = hash * 397 ^ fallbacks;
                    hash = hash * 397 ^ updateDrivers;
                    return hash;
                }
            }
        }
    }
}
