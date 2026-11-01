using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tetherfall;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tetherfall.Editor
{
    public static class TetherfallPelagicCourtVerification
    {
        private const string MenuPath = "Tetherfall/Verify Pelagic Court";
        private const string RuntimeRelativePath =
            "Scripts/TetherfallPelagicCourt.cs";
        private const string VerificationRelativePath =
            "Editor/TetherfallPelagicCourtVerification.cs";
        private static readonly List<string> Failures = new List<string>();
        private static int assertionCount;
        private static int sectionCount;

        [MenuItem(MenuPath, false, 2212)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Pelagic Court verification is edit-mode only. Stop Play Mode and run it again.");
            }

            VerifyInternal(true);
        }

        [MenuItem(MenuPath, true, 2212)]
        private static bool CanVerify()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        public static void VerifyFromCommandLine()
        {
            Verify();
        }

        public static void VerifyForFullGame()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Pelagic Court verification is edit-mode only.");
            }

            VerifyInternal(false);
        }

        private static void VerifyInternal(bool logSuccess)
        {
            Failures.Clear();
            assertionCount = 0;
            sectionCount = 0;
            Run("public contract and explicit startup", VerifyPublicContract);
            Run("progression gates", VerifyProgressionGates);
            Run("one-shot relic reward", VerifyOneShotRelicReward);
            Run("finite oxygen math", VerifyFiniteOxygenMath);
            Run("safe evacuation", VerifySafeEvacuation);
            Run("semantic IDs", VerifySemanticIds);
            Run("room connectivity", VerifyRoomConnectivity);
            Run("idempotent builds and budgets", VerifyBuildsAndBudgets);
            Run("static source lint", VerifyStaticSourceLint);
            Complete(logSuccess);
        }

        private static void VerifyPublicContract()
        {
            var build = typeof(TetherfallPelagicCourt).GetMethod(
                "Build",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(TetherfallGame) },
                null);
            Check(
                build != null && build.ReturnType == typeof(GameObject),
                "Public static Build(TetherfallGame) -> GameObject is missing.");
            Check(
                typeof(TetherfallPelagicCourt).GetMethod(
                    "BuildForcedFallback",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(TetherfallGame) },
                    null) != null,
                "Forced procedural-fallback build API is missing.");
            Check(
                typeof(TetherfallPelagicCourt).GetMethod(
                    "Cleanup",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(TetherfallGame) },
                    null) != null,
                "Pelagic Court cleanup API is missing.");
            Check(
                typeof(IWorldInteractable).IsAssignableFrom(
                    typeof(TetherfallPelagicEntrance)),
                "The sealed entrance does not use IWorldInteractable.");
            Check(
                typeof(IWorldInteractable).IsAssignableFrom(
                    typeof(TetherfallPelagicSocket)),
                "Pelagic content sockets do not use IWorldInteractable.");
            Check(
                typeof(TetherfallSaveData).GetField("pelagicCourt") != null,
                "TetherfallSaveData has no additive Pelagic Court state field.");
            Check(
                HasInstanceMethod(
                    typeof(TetherfallGame),
                    "OnPelagicCourtBossRewardResolved",
                    typeof(string)) &&
                HasInstanceMethod(
                    typeof(TetherfallGame),
                    "WritePelagicCourtState",
                    typeof(TetherfallSaveData)) &&
                HasInstanceMethod(
                    typeof(TetherfallGame),
                    "RestorePelagicCourtState",
                    typeof(TetherfallSaveData)),
                "Explicit boss-reward or save-state integration hooks are missing.");
            Check(
                HasInstanceMethod(
                    typeof(TetherfallPelagicPressure),
                    "BindPlayerAvatar",
                    typeof(PlayerAvatar)) &&
                HasInstanceMethod(
                    typeof(TetherfallPelagicPressure),
                    "SimulateBoundedStep",
                    typeof(float),
                    typeof(bool),
                    typeof(bool)),
                "The oxygen-pressure component does not expose PlayerAvatar and simulation hooks.");
            Check(
                HasInstanceMethod(
                    typeof(TetherfallPelagicCourtRuntime),
                    "TryAdvanceProgression",
                    typeof(string)) &&
                HasInstanceMethod(
                    typeof(TetherfallPelagicCourtRuntime),
                    "ReportQuestCompleted",
                    typeof(string)) &&
                HasInstanceMethod(
                    typeof(TetherfallPelagicCourtRuntime),
                    "ReportActivityCompleted",
                    typeof(string)) &&
                HasInstanceMethod(
                    typeof(TetherfallPelagicCourtRuntime),
                    "ReportBossDefeated",
                    typeof(string)),
                "Runtime progression reporting APIs are incomplete.");

            var boss = BossCatalog.Get(TetherfallPelagicCourt.KeyBossId);
            Check(
                string.Equals(
                    TetherfallPelagicCourt.KeyBossId,
                    DungeonCatalog.GlasswaterBossId,
                    StringComparison.Ordinal) &&
                boss != null &&
                string.Equals(
                    boss.displayName,
                    "Glasswater Regent",
                    StringComparison.Ordinal),
                "The gate is not anchored to the named Glasswater Regent river boss.");

            var relic = TetherfallPelagicCourt.CreateRelicEquipmentDefinition();
            Check(
                relic != null &&
                string.Equals(
                    relic.id,
                    TetherfallPelagicCourt.InfiniteBreathingRelicId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    relic.displayName,
                    "Tideglass Crown",
                    StringComparison.Ordinal) &&
                relic.slot == EquipmentSlot.Charm &&
                relic.restorationRewardOnly &&
                relic.description.IndexOf(
                    "infinite breath",
                    StringComparison.OrdinalIgnoreCase) >= 0,
                "The one-shot Tideglass Crown equipment definition is incomplete.");

            CheckNoRuntimeInitializer(typeof(TetherfallPelagicCourt));
            CheckNoRuntimeInitializer(typeof(TetherfallPelagicCourtRuntime));
            CheckNoRuntimeInitializer(typeof(TetherfallPelagicPressure));
        }

        private static void VerifyProgressionGates()
        {
            var unavailable = TetherfallPelagicCourtRules.ResolveGate(
                new TetherfallPelagicGateFacts(false, false, false));
            var bossLocked = TetherfallPelagicCourtRules.ResolveGate(
                new TetherfallPelagicGateFacts(true, false, false));
            var crownLocked = TetherfallPelagicCourtRules.ResolveGate(
                new TetherfallPelagicGateFacts(true, true, false));
            var open = TetherfallPelagicCourtRules.ResolveGate(
                new TetherfallPelagicGateFacts(true, true, true));

            Check(
                unavailable.Status ==
                TetherfallPelagicGateStatus.ModuleUnavailable &&
                !unavailable.CanEnter,
                "An unavailable module resolved as enterable.");
            Check(
                bossLocked.Status ==
                TetherfallPelagicGateStatus.SealedByGlasswaterRegent &&
                !bossLocked.CanEnter &&
                bossLocked.RequirementText.IndexOf(
                    "Glasswater Regent",
                    StringComparison.Ordinal) >= 0,
                "The initial gate does not clearly require the Glasswater Regent.");
            Check(
                crownLocked.Status ==
                TetherfallPelagicGateStatus.AwaitingTideglassCrown &&
                !crownLocked.CanEnter &&
                crownLocked.RequirementText.IndexOf(
                    "Tideglass Crown",
                    StringComparison.Ordinal) >= 0,
                "Boss completion without the relic incorrectly opens the gate.");
            Check(
                open.Status == TetherfallPelagicGateStatus.Open &&
                open.CanEnter,
                "The Regent reward plus Tideglass Crown does not open the court.");

            var nodes = TetherfallPelagicCourt.Progression;
            Check(nodes.Count >= 8, "The semantic progression chain is too short.");
            var completed = 0;
            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                Check(
                    TetherfallPelagicCourtRules.CanAdvanceProgression(
                        nodes,
                        completed,
                        node.id),
                    $"Progression node '{node.id}' cannot advance in authored order.");
                if (index + 1 < nodes.Count)
                {
                    Check(
                        !TetherfallPelagicCourtRules.CanAdvanceProgression(
                            nodes,
                            completed,
                            nodes[index + 1].id),
                        $"Progression can skip directly to '{nodes[index + 1].id}'.");
                }

                completed++;
                Check(
                    !TetherfallPelagicCourtRules.CanAdvanceProgression(
                        nodes,
                        completed,
                        node.id),
                    $"Completed progression node '{node.id}' can be applied twice.");
            }

            var normalized = TetherfallPelagicCourtRules.NormalizeState(
                new TetherfallPelagicCourtStateData
                {
                    completedProgressionNodeCount = -20,
                    bossRewardObserved = false,
                    relicGranted = true,
                    relicGrantCount = 99
                },
                nodes.Count);
            Check(
                normalized.bossRewardObserved &&
                normalized.relicGranted &&
                normalized.relicGrantCount == 1 &&
                normalized.completedProgressionNodeCount >= 2 &&
                normalized.completedProgressionNodeCount <= nodes.Count,
                "State normalization does not preserve prerequisite implications safely.");
        }

        private static void VerifyOneShotRelicReward()
        {
            var ledger = new TetherfallPelagicRewardLedger();
            Check(
                !TetherfallPelagicCourtRules.TryResolveRelicReward(
                    ref ledger,
                    "boss.not_glasswater",
                    true,
                    false) &&
                !ledger.relicGranted &&
                ledger.relicGrantCount == 0,
                "An unrelated boss can grant the Tideglass Crown.");
            var unrelatedOwnedLedger = new TetherfallPelagicRewardLedger();
            Check(
                !TetherfallPelagicCourtRules.TryResolveRelicReward(
                    ref unrelatedOwnedLedger,
                    "boss.not_glasswater",
                    true,
                    true) &&
                !unrelatedOwnedLedger.bossRewardObserved &&
                !unrelatedOwnedLedger.relicGranted,
                "Existing ownership lets an unrelated boss satisfy the Regent ledger.");
            Check(
                TetherfallPelagicCourtRules.TryResolveRelicReward(
                    ref ledger,
                    TetherfallPelagicCourt.KeyBossId,
                    true,
                    false) &&
                ledger.bossRewardObserved &&
                ledger.relicGranted &&
                ledger.relicGrantCount == 1,
                "The first valid Regent reward did not grant exactly one relic.");
            Check(
                !TetherfallPelagicCourtRules.TryResolveRelicReward(
                    ref ledger,
                    TetherfallPelagicCourt.KeyBossId,
                    true,
                    false) &&
                ledger.relicGrantCount == 1,
                "Repeating the Regent reward can duplicate the relic.");
            Check(
                !TetherfallPelagicCourtRules.TryResolveRelicReward(
                    ref ledger,
                    TetherfallPelagicCourt.KeyBossId,
                    true,
                    true) &&
                ledger.relicGrantCount == 1,
                "An already-owned relic was granted again.");

            var corrupt = new TetherfallPelagicRewardLedger
            {
                bossRewardObserved = false,
                relicGranted = true,
                relicGrantCount = int.MaxValue
            };
            TetherfallPelagicCourtRules.NormalizeRewardLedger(ref corrupt);
            Check(
                corrupt.bossRewardObserved &&
                corrupt.relicGranted &&
                corrupt.relicGrantCount == 1,
                "Relic-ledger normalization does not enforce a one-item ceiling.");
        }

        private static void VerifyFiniteOxygenMath()
        {
            var state = TetherfallPelagicCourtRules.CreateFullOxygenState();
            var first = TetherfallPelagicCourtRules.StepOxygen(
                state,
                0.5f,
                true,
                false);
            var expected =
                TetherfallPelagicCourtRules.MaximumOxygenSeconds -
                TetherfallPelagicCourtRules.PressureDrainPerSecond * 0.5f;
            Check(
                Approximately(first.State.remainingSeconds, expected) &&
                !first.ShouldEvacuate,
                "A finite pressure step drains oxygen incorrectly.");

            var bounded = TetherfallPelagicCourtRules.StepOxygen(
                first.State,
                5000f,
                true,
                false);
            Check(
                Approximately(
                    first.State.remainingSeconds - bounded.State.remainingSeconds,
                    TetherfallPelagicCourtRules.PressureDrainPerSecond *
                    TetherfallPelagicCourtRules.MaximumSimulationStepSeconds),
                "A large frame delta bypasses the bounded oxygen simulation step.");

            var invalidDelta = TetherfallPelagicCourtRules.StepOxygen(
                bounded.State,
                float.NaN,
                true,
                false);
            Check(
                TetherfallPelagicCourtRules.IsFinite(
                    invalidDelta.State.remainingSeconds) &&
                Approximately(
                    invalidDelta.State.remainingSeconds,
                    bounded.State.remainingSeconds),
                "NaN delta time contaminated oxygen state.");

            var invalidState = TetherfallPelagicCourtRules.NormalizeOxygen(
                new TetherfallPelagicOxygenState
                {
                    remainingSeconds = float.PositiveInfinity,
                    evacuationCount = -8
                });
            Check(
                Approximately(
                    invalidState.remainingSeconds,
                    TetherfallPelagicCourtRules.MaximumOxygenSeconds) &&
                invalidState.evacuationCount == 0,
                "Nonfinite or negative oxygen save data was not normalized.");

            var recovering = TetherfallPelagicCourtRules.StepOxygen(
                new TetherfallPelagicOxygenState
                {
                    remainingSeconds = 2f,
                    evacuationCount = 3
                },
                0.5f,
                false,
                false);
            Check(
                recovering.State.remainingSeconds > 2f &&
                recovering.State.remainingSeconds <=
                TetherfallPelagicCourtRules.MaximumOxygenSeconds &&
                recovering.State.evacuationCount == 3,
                "Safe-water recovery is not finite and bounded.");

            var infinite = TetherfallPelagicCourtRules.StepOxygen(
                new TetherfallPelagicOxygenState
                {
                    remainingSeconds = 0.01f
                },
                0.5f,
                true,
                true);
            Check(
                Approximately(
                    infinite.State.remainingSeconds,
                    TetherfallPelagicCourtRules.MaximumOxygenSeconds) &&
                !infinite.ShouldEvacuate,
                "The Tideglass Crown does not provide infinite breathing.");
        }

        private static void VerifySafeEvacuation()
        {
            var depleted = TetherfallPelagicCourtRules.StepOxygen(
                new TetherfallPelagicOxygenState
                {
                    remainingSeconds = 0f
                },
                0.1f,
                true,
                false);
            Check(
                depleted.ShouldEvacuate &&
                depleted.EvacuationReason ==
                TetherfallPelagicEvacuationReason.OxygenDepleted,
                "Zero oxygen under pressure does not request evacuation.");

            var safe = TetherfallPelagicCourtRules.StepOxygen(
                new TetherfallPelagicOxygenState
                {
                    remainingSeconds = 0f
                },
                0.1f,
                false,
                false);
            Check(
                !safe.ShouldEvacuate && safe.State.remainingSeconds > 0f,
                "A player outside abyssal pressure is evacuated instead of recovering.");

            Check(
                TetherfallPelagicPressure.ContainsPressure(
                    TetherfallPelagicCourt.CityCenter +
                    TetherfallPelagicCourt.PressureLocalBounds.center),
                "The authored city center is outside its pressure envelope.");
            Check(
                !TetherfallPelagicPressure.ContainsPressure(
                    TetherfallPelagicCourt.SurfaceEvacuationPosition),
                "The evacuation destination is still inside abyssal pressure.");
            Check(
                IsFinite(TetherfallPelagicCourt.SurfaceEvacuationPosition) &&
                TetherfallPelagicCourt.SurfaceEvacuationPosition.y >= 0.5f &&
                Vector3.Distance(
                    TetherfallPelagicCourt.SurfaceEvacuationPosition,
                    TetherfallPelagicCourt.SurfaceEntrancePosition) <= 25f,
                "The evacuation destination is nonfinite, submerged, or detached from the entrance.");
            Check(
                TetherfallPelagicCourt.GetSocket(
                    TetherfallPelagicCourt.EmergencyAscentSocketId)?.kind ==
                TetherfallPelagicSocketKind.EmergencyAscent,
                "The city has no explicit emergency-ascent interaction socket.");
        }

        private static void VerifySemanticIds()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            RegisterSemanticId(
                ids,
                TetherfallPelagicCourt.CourtId,
                "city.",
                "Pelagic Court");
            RegisterSemanticId(
                ids,
                TetherfallPelagicCourt.InfiniteBreathingRelicId,
                "relic.",
                "Tideglass Crown");
            RegisterSemanticId(
                ids,
                TetherfallPelagicCourt.PelagicBossId,
                "boss.",
                "Pelagic boss");

            var roomIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var room in TetherfallPelagicCourt.Rooms)
            {
                Check(room != null, "Room catalog contains a null entry.");
                if (room == null)
                {
                    continue;
                }

                RegisterSemanticId(ids, room.id, "room.pelagic_court.", "Room");
                Check(
                    roomIds.Add(room.id),
                    $"Room ID '{room.id}' is duplicated.");
                Check(
                    !string.IsNullOrWhiteSpace(room.displayName) &&
                    IsFinite(room.localCenter) &&
                    IsFinite(room.size) &&
                    room.size.x > 0f &&
                    room.size.y > 0f &&
                    room.size.z > 0f,
                    $"Room '{room.id}' has incomplete or nonfinite metadata.");
            }

            var edgePairs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var connection in TetherfallPelagicCourt.Connections)
            {
                Check(connection != null, "Connection catalog contains a null entry.");
                if (connection == null)
                {
                    continue;
                }

                RegisterSemanticId(
                    ids,
                    connection.id,
                    "connection.pelagic_court.",
                    "Connection");
                Check(
                    roomIds.Contains(connection.fromRoomId) &&
                    roomIds.Contains(connection.toRoomId) &&
                    !string.Equals(
                        connection.fromRoomId,
                        connection.toRoomId,
                        StringComparison.Ordinal),
                    $"Connection '{connection.id}' has invalid room references.");
                Check(
                    edgePairs.Add(
                        connection.fromRoomId + "->" + connection.toRoomId),
                    $"Connection '{connection.id}' duplicates a room edge.");
            }

            var socketKinds = new HashSet<TetherfallPelagicSocketKind>();
            foreach (var socket in TetherfallPelagicCourt.Sockets)
            {
                Check(socket != null, "Socket catalog contains a null entry.");
                if (socket == null)
                {
                    continue;
                }

                RegisterSemanticId(
                    ids,
                    socket.id,
                    "socket.pelagic_court.",
                    "Socket");
                socketKinds.Add(socket.kind);
                Check(
                    roomIds.Contains(socket.roomId) &&
                    IsFinite(socket.localPosition) &&
                    !string.IsNullOrWhiteSpace(socket.displayName) &&
                    !string.IsNullOrWhiteSpace(socket.integrationHint),
                    $"Socket '{socket.id}' has incomplete or invalid metadata.");
            }

            foreach (TetherfallPelagicSocketKind kind in
                     Enum.GetValues(typeof(TetherfallPelagicSocketKind)))
            {
                Check(
                    socketKinds.Contains(kind),
                    $"Pelagic Court has no {kind} socket.");
            }

            string priorNodeId = null;
            foreach (var node in TetherfallPelagicCourt.Progression)
            {
                Check(node != null, "Progression catalog contains a null entry.");
                if (node == null)
                {
                    continue;
                }

                RegisterSemanticId(
                    ids,
                    node.id,
                    "progress.pelagic_court.",
                    "Progression node");
                Check(
                    string.Equals(
                        node.prerequisiteId,
                        priorNodeId ?? string.Empty,
                        StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(node.displayName) &&
                    !string.IsNullOrWhiteSpace(node.triggerId),
                    $"Progression node '{node.id}' does not form a strict semantic chain.");
                priorNodeId = node.id;
            }
        }

        private static void VerifyRoomConnectivity()
        {
            var rooms = TetherfallPelagicCourt.Rooms;
            var connections = TetherfallPelagicCourt.Connections;
            Check(rooms.Count >= 8, "Pelagic Court needs at least eight distinct rooms.");
            Check(
                connections.Count >= rooms.Count - 1,
                "Pelagic Court has too few room connections.");
            if (rooms.Count == 0)
            {
                return;
            }

            var adjacency =
                new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var room in rooms)
            {
                if (room != null)
                {
                    adjacency[room.id] = new List<string>();
                }
            }

            foreach (var connection in connections)
            {
                if (connection == null ||
                    !adjacency.ContainsKey(connection.fromRoomId) ||
                    !adjacency.ContainsKey(connection.toRoomId))
                {
                    continue;
                }

                adjacency[connection.fromRoomId].Add(connection.toRoomId);
                adjacency[connection.toRoomId].Add(connection.fromRoomId);
            }

            var visited = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>();
            queue.Enqueue(rooms[0].id);
            visited.Add(rooms[0].id);
            while (queue.Count > 0)
            {
                var roomId = queue.Dequeue();
                var neighbors = adjacency[roomId];
                for (var index = 0; index < neighbors.Count; index++)
                {
                    if (visited.Add(neighbors[index]))
                    {
                        queue.Enqueue(neighbors[index]);
                    }
                }
            }

            Check(
                visited.Count == rooms.Count,
                $"Only {visited.Count}/{rooms.Count} Pelagic Court rooms are reachable.");

            var entranceCount = 0;
            var dockCount = 0;
            var throneCount = 0;
            var arenaCount = 0;
            var bossCount = 0;
            foreach (var room in rooms)
            {
                if (room == null)
                {
                    continue;
                }

                entranceCount +=
                    room.kind == TetherfallPelagicRoomKind.Entrance ? 1 : 0;
                dockCount += room.kind == TetherfallPelagicRoomKind.Dock ? 1 : 0;
                throneCount += room.kind == TetherfallPelagicRoomKind.Throne ? 1 : 0;
                arenaCount += room.kind == TetherfallPelagicRoomKind.Arena ? 1 : 0;
                bossCount += room.kind == TetherfallPelagicRoomKind.Boss ? 1 : 0;
            }

            Check(
                entranceCount == 1 &&
                dockCount == 1 &&
                throneCount == 1 &&
                arenaCount == 1 &&
                bossCount == 1,
                "Entrance, dock, throne, arena, or boss room cardinality is invalid.");
        }

        private static void VerifyBuildsAndBudgets()
        {
            var fixture = new GameObject("Pelagic Court Verification Fixture");
            var game = fixture.AddComponent<TetherfallGame>();
            try
            {
                Check(
                    TetherfallPelagicCourt.GetBuiltRoot(game) == null,
                    "Pelagic Court auto-built before explicit Build was called.");
                var normal = TetherfallPelagicCourt.Build(game);
                Check(normal != null, "Normal Pelagic Court Build returned null.");
                if (normal == null)
                {
                    return;
                }

                var normalMarker = normal.GetComponent<TetherfallPelagicCourtRoot>();
                VerifyBuiltRoot(normal, normalMarker, false);
                var snapshot = CaptureSnapshot(normal, normalMarker);
                var repeated = TetherfallPelagicCourt.Build(game);
                Check(
                    ReferenceEquals(normal, repeated),
                    "Repeated Build replaced the stable Pelagic Court root.");
                Check(
                    snapshot.Equals(
                        CaptureSnapshot(
                            repeated,
                            repeated.GetComponent<TetherfallPelagicCourtRoot>())),
                    "Repeated Build changed the hierarchy or budget snapshot.");
                Check(
                    CountDirectRoots(game.transform) == 1,
                    "Repeated Build created duplicate Pelagic Court roots.");
                Check(
                    CountRelicDefinitions(game) == 1,
                    "Repeated Build duplicated the Tideglass Crown equipment definition.");

                Check(
                    TetherfallPelagicCourt.Cleanup(game) &&
                    !TetherfallPelagicCourt.Cleanup(game) &&
                    CountDirectRoots(game.transform) == 0,
                    "Pelagic Court cleanup is not idempotent.");

                var fallback = TetherfallPelagicCourt.BuildForcedFallback(game);
                var fallbackMarker =
                    fallback != null
                        ? fallback.GetComponent<TetherfallPelagicCourtRoot>()
                        : null;
                VerifyBuiltRoot(fallback, fallbackMarker, true);
                Check(
                    fallbackMarker != null &&
                    fallbackMarker.PreferredResourceCount == 0 &&
                    fallbackMarker.FallbackCount ==
                    TetherfallPelagicCourt.PreferredResourcePlacementCount,
                    "Forced fallback did not replace every preferred resource deterministically.");
                Check(
                    CountRelicDefinitions(game) == 1,
                    "Fallback build duplicated the Tideglass Crown definition.");
            }
            catch (Exception exception)
            {
                Fail(
                    "Pelagic Court edit-mode build threw " +
                    Describe(exception));
            }
            finally
            {
                TetherfallPelagicCourt.Cleanup(game);
                Object.DestroyImmediate(fixture);
            }
        }

        private static void VerifyBuiltRoot(
            GameObject root,
            TetherfallPelagicCourtRoot marker,
            bool forcedFallback)
        {
            Check(root != null, "Built root is null.");
            Check(marker != null, "Built root has no Pelagic Court metadata.");
            if (root == null || marker == null)
            {
                return;
            }

            Check(
                root.name == TetherfallPelagicCourt.RootName &&
                root.transform.parent != null &&
                root.transform.localPosition == Vector3.zero &&
                Quaternion.Angle(
                    root.transform.localRotation,
                    Quaternion.identity) < 0.001f &&
                Vector3.SqrMagnitude(
                    root.transform.localScale - Vector3.one) < 0.000001f,
                "The Pelagic Court root is not a stable identity child of TetherfallGame.");
            Check(
                marker.ForcedFallback == forcedFallback &&
                marker.Runtime != null &&
                marker.Pressure != null &&
                marker.VisualRoot != null &&
                marker.GameplayRoot != null &&
                marker.InteractionRoot != null,
                "Root metadata is incomplete or reports the wrong fallback mode.");

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var colliders = root.GetComponentsInChildren<Collider>(true);
            var lights = root.GetComponentsInChildren<Light>(true);
            Check(
                renderers.Length >= 120 &&
                renderers.Length <= TetherfallPelagicCourt.MaxRendererCount &&
                renderers.Length == marker.RendererCount,
                $"Renderer count is {renderers.Length} (metadata {marker.RendererCount}); " +
                $"expected a substantive build within {TetherfallPelagicCourt.MaxRendererCount}.");
            Check(
                colliders.Length >= 20 &&
                colliders.Length <= TetherfallPelagicCourt.MaxColliderCount &&
                colliders.Length == marker.ColliderCount,
                $"Collider count is {colliders.Length} (metadata {marker.ColliderCount}); " +
                $"budget is {TetherfallPelagicCourt.MaxColliderCount}.");
            Check(
                lights.Length ==
                TetherfallPelagicCourt.AddedRealtimeLightCount &&
                lights.Length == marker.LightCount,
                "Bubble lighting added a realtime Light instead of cached emissive materials.");
            Check(
                marker.SharedMaterialCount > 0 &&
                marker.SharedMaterialCount <=
                TetherfallPelagicCourt.MaxSharedMaterialCount,
                $"Shared material count {marker.SharedMaterialCount} exceeds its cache budget.");
            Check(
                marker.ResourcePlacementCount ==
                TetherfallPelagicCourt.PreferredResourcePlacementCount &&
                marker.PreferredResourceCount + marker.FallbackCount ==
                marker.ResourcePlacementCount,
                "Existing-asset and procedural-fallback accounting is inconsistent.");

            Check(
                marker.VisualRoot.GetComponentsInChildren<Collider>(true).Length == 0,
                "Collider-free visual art contains gameplay collision.");
            Check(
                marker.GameplayRoot.GetComponentsInChildren<Renderer>(true).Length == 0,
                "Renderer-free gameplay geometry contains presentation renderers.");
            Check(
                marker.InteractionRoot.GetComponentsInChildren<Renderer>(true).Length == 0 &&
                marker.InteractionRoot.GetComponentsInChildren<Collider>(true).Length == 0,
                "Interaction sockets contain hidden art or collision.");
            Check(
                root.GetComponentsInChildren<WaterVolume>(true).Length == 1 &&
                root.GetComponentsInChildren<TetherfallPelagicCurrentVolume>(true).Length == 3,
                "The court lacks one bounded water volume or exactly three authored current lanes.");
            Check(
                root.GetComponentsInChildren<TetherfallPelagicEntrance>(true).Length == 1 &&
                root.GetComponentsInChildren<TetherfallPelagicSocket>(true).Length ==
                TetherfallPelagicCourt.Sockets.Count,
                "Entrance or stable interaction socket runtime counts are incorrect.");
            Check(
                root.GetComponentsInChildren<Camera>(true).Length == 0 &&
                root.GetComponentsInChildren<ParticleSystem>(true).Length == 0 &&
                root.GetComponentsInChildren<Rigidbody>(true).Length == 0 &&
                root.GetComponentsInChildren<Animator>(true).Length == 0 &&
                root.GetComponentsInChildren<LODGroup>(true).Length == 0,
                "The module added an unbudgeted camera, particles, rigidbody, animator, or per-piece LOD.");

            var updateDrivers = CountDeclaredUpdateDrivers(root);
            Check(
                updateDrivers == TetherfallPelagicCourt.UpdateDriverCount &&
                marker.UpdateDriverCount == updateDrivers,
                $"Found {updateDrivers} Update drivers; expected exactly " +
                $"{TetherfallPelagicCourt.UpdateDriverCount} bounded central drivers.");

            var deepStructural = FindDescendant(root.transform, "Deep Structural Art");
            var deepDetail = FindDescendant(root.transform, "Deep Detail Art");
            var deepGameplay = FindDescendant(root.transform, "Deep Gameplay Volumes");
            var deepInteractions = FindDescendant(root.transform, "Deep Interaction Sockets");
            Check(
                deepStructural != null &&
                deepDetail != null &&
                deepGameplay != null &&
                deepInteractions != null &&
                !deepStructural.gameObject.activeSelf &&
                !deepDetail.gameObject.activeSelf &&
                !deepGameplay.gameObject.activeSelf &&
                !deepInteractions.gameObject.activeSelf,
                "Deep city roots do not begin under explicit proximity activation.");
        }

        private static void VerifyStaticSourceLint()
        {
            var runtimePath = Path.Combine(Application.dataPath, RuntimeRelativePath);
            var verificationPath = Path.Combine(
                Application.dataPath,
                VerificationRelativePath);
            Check(File.Exists(runtimePath), "Pelagic Court runtime source file is missing.");
            Check(
                File.Exists(verificationPath),
                "Pelagic Court verification source file is missing.");
            if (!File.Exists(runtimePath))
            {
                return;
            }

            var source = File.ReadAllText(runtimePath);
            Check(
                source.IndexOf(
                    "RuntimeInitializeOnLoadMethod",
                    StringComparison.Ordinal) < 0,
                "Pelagic Court contains an auto-run runtime initializer.");
            Check(
                CountOccurrences(source, "private void Update()") ==
                TetherfallPelagicCourt.UpdateDriverCount,
                "Runtime source does not contain exactly two bounded Update drivers.");
            Check(
                source.IndexOf("new Material(", StringComparison.Ordinal) < 0 &&
                source.IndexOf(".material =", StringComparison.Ordinal) < 0,
                "Runtime source bypasses the shared material cache.");
            Check(
                source.IndexOf("using System.Linq", StringComparison.Ordinal) < 0 &&
                source.IndexOf(".ToList(", StringComparison.Ordinal) < 0 &&
                source.IndexOf(".Where(", StringComparison.Ordinal) < 0,
                "Runtime source uses allocation-prone LINQ.");
            Check(
                source.IndexOf("Physics.OverlapSphere(", StringComparison.Ordinal) < 0 &&
                source.IndexOf("Physics.OverlapBox(", StringComparison.Ordinal) < 0 &&
                source.IndexOf("Physics.RaycastAll(", StringComparison.Ordinal) < 0 &&
                source.IndexOf("FindObjectsOfType", StringComparison.Ordinal) < 0,
                "Runtime source uses an allocating world query.");
            Check(
                source.IndexOf(
                    "public static GameObject Build(TetherfallGame game)",
                    StringComparison.Ordinal) >= 0 &&
                source.IndexOf(
                    "public TetherfallPelagicGateDecision EvaluatePelagicCourtGate()",
                    StringComparison.Ordinal) >= 0 &&
                source.IndexOf(
                    "public void BindPlayerAvatar(PlayerAvatar avatar)",
                    StringComparison.Ordinal) >= 0,
                "Static lint could not find the explicit build, gate, or PlayerAvatar hooks.");
            Check(
                source.IndexOf(
                    "RuntimeEffectMaterialCache.GetOpaque",
                    StringComparison.Ordinal) >= 0 &&
                source.IndexOf(
                    "RuntimeEffectMaterialCache.GetGlow",
                    StringComparison.Ordinal) >= 0 &&
                source.IndexOf(
                    "RuntimeEffectMaterialCache.GetTransparent",
                    StringComparison.Ordinal) >= 0,
                "Static lint could not find cached opaque, glow, and transparent materials.");
        }

        private static int CountRelicDefinitions(TetherfallGame game)
        {
            var field = typeof(TetherfallGame).GetField(
                "equipmentCatalog",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var catalog = field?.GetValue(game) as List<EquipmentDefinition>;
            if (catalog == null)
            {
                return 0;
            }

            var count = 0;
            for (var index = 0; index < catalog.Count; index++)
            {
                if (catalog[index] != null &&
                    string.Equals(
                        catalog[index].id,
                        TetherfallPelagicCourt.InfiniteBreathingRelicId,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountDeclaredUpdateDrivers(GameObject root)
        {
            if (root == null)
            {
                return 0;
            }

            var count = 0;
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                var behaviour = behaviours[index];
                if (behaviour == null)
                {
                    continue;
                }

                var update = behaviour.GetType().GetMethod(
                    "Update",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);
                if (update != null &&
                    update.ReturnType == typeof(void) &&
                    update.GetParameters().Length == 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountDirectRoots(Transform parent)
        {
            var count = 0;
            if (parent == null)
            {
                return count;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child != null &&
                    child.GetComponent<TetherfallPelagicCourtRoot>() != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (string.Equals(root.name, name, StringComparison.Ordinal))
            {
                return root;
            }

            for (var index = 0; index < root.childCount; index++)
            {
                var found = FindDescendant(root.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static BuildSnapshot CaptureSnapshot(
            GameObject root,
            TetherfallPelagicCourtRoot marker)
        {
            return new BuildSnapshot(
                root != null
                    ? root.GetComponentsInChildren<Transform>(true).Length
                    : 0,
                marker?.RendererCount ?? 0,
                marker?.ColliderCount ?? 0,
                marker?.LightCount ?? 0,
                marker?.SharedMaterialCount ?? 0,
                marker?.ResourcePlacementCount ?? 0,
                marker?.PreferredResourceCount ?? 0,
                marker?.FallbackCount ?? 0);
        }

        private static void RegisterSemanticId(
            HashSet<string> ids,
            string id,
            string requiredPrefix,
            string owner)
        {
            Check(
                IsSemanticId(id, requiredPrefix),
                $"{owner} ID '{id}' is not a semantic '{requiredPrefix}' stable ID.");
            if (!string.IsNullOrWhiteSpace(id))
            {
                Check(
                    ids.Add(id),
                    $"Stable ID '{id}' is duplicated within Pelagic Court.");
            }
        }

        private static bool IsSemanticId(string id, string requiredPrefix)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                !id.StartsWith(requiredPrefix, StringComparison.Ordinal) ||
                !string.Equals(id, id.Trim(), StringComparison.Ordinal) ||
                !string.Equals(id, id.ToLowerInvariant(), StringComparison.Ordinal) ||
                id.IndexOf("..", StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            for (var index = 0; index < id.Length; index++)
            {
                var character = id[index];
                if (!(character >= 'a' && character <= 'z') &&
                    !(character >= '0' && character <= '9') &&
                    character != '.' &&
                    character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasInstanceMethod(
            Type type,
            string name,
            params Type[] parameterTypes)
        {
            return type.GetMethod(
                       name,
                       BindingFlags.Instance |
                       BindingFlags.Public |
                       BindingFlags.NonPublic,
                       null,
                       parameterTypes,
                       null) != null;
        }

        private static void CheckNoRuntimeInitializer(Type type)
        {
            var methods = type.GetMethods(
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            for (var index = 0; index < methods.Length; index++)
            {
                Check(
                    methods[index].GetCustomAttributes(
                        typeof(RuntimeInitializeOnLoadMethodAttribute),
                        false).Length == 0,
                    $"{type.Name}.{methods[index].Name} auto-runs at runtime.");
            }
        }

        private static int CountOccurrences(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            {
                return 0;
            }

            var count = 0;
            var offset = 0;
            while (offset <= source.Length - value.Length)
            {
                var index = source.IndexOf(value, offset, StringComparison.Ordinal);
                if (index < 0)
                {
                    break;
                }

                count++;
                offset = index + value.Length;
            }

            return count;
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.0001f;
        }

        private static bool IsFinite(Vector3 value)
        {
            return TetherfallPelagicCourtRules.IsFinite(value.x) &&
                   TetherfallPelagicCourtRules.IsFinite(value.y) &&
                   TetherfallPelagicCourtRules.IsFinite(value.z);
        }

        private static void Run(string section, Action action)
        {
            sectionCount++;
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Fail(
                    $"{section} check could not complete: {Describe(exception)}");
            }
        }

        private static void Check(bool condition, string failure)
        {
            assertionCount++;
            if (!condition)
            {
                Fail(failure);
            }
        }

        private static void Fail(string failure)
        {
            if (!string.IsNullOrWhiteSpace(failure) &&
                !Failures.Contains(failure))
            {
                Failures.Add(failure);
            }
        }

        private static void Complete(bool logSuccess)
        {
            if (Failures.Count == 0)
            {
                if (logSuccess)
                {
                    Debug.Log(
                        $"Pelagic Court verification passed: {assertionCount} assertions across " +
                        $"{sectionCount} sections. Progression, one-shot reward, bounded breath, " +
                        "evacuation, graph, idempotence, source lint, and performance budgets are stable.");
                }

                return;
            }

            var message = new StringBuilder();
            message.Append("Pelagic Court verification failed with ");
            message.Append(Failures.Count);
            message.AppendLine(" actionable issue(s):");
            for (var index = 0; index < Failures.Count; index++)
            {
                message.Append(index + 1);
                message.Append(". ");
                message.AppendLine(Failures[index]);
            }

            message.Append(
                $"Checked {assertionCount} assertions across {sectionCount} sections.");
            var failureMessage = message.ToString();
            Debug.LogError(failureMessage);
            throw new InvalidOperationException(failureMessage);
        }

        private static string Describe(Exception exception)
        {
            var current = exception;
            while (current.InnerException != null &&
                   (current is TargetInvocationException ||
                    current is TypeInitializationException))
            {
                current = current.InnerException;
            }

            return $"{current.GetType().Name}: {current.Message}";
        }

        private readonly struct BuildSnapshot : IEquatable<BuildSnapshot>
        {
            private readonly int transforms;
            private readonly int renderers;
            private readonly int colliders;
            private readonly int lights;
            private readonly int materials;
            private readonly int placements;
            private readonly int preferred;
            private readonly int fallback;

            public BuildSnapshot(
                int transformCount,
                int rendererCount,
                int colliderCount,
                int lightCount,
                int materialCount,
                int placementCount,
                int preferredCount,
                int fallbackCount)
            {
                transforms = transformCount;
                renderers = rendererCount;
                colliders = colliderCount;
                lights = lightCount;
                materials = materialCount;
                placements = placementCount;
                preferred = preferredCount;
                fallback = fallbackCount;
            }

            public bool Equals(BuildSnapshot other)
            {
                return transforms == other.transforms &&
                       renderers == other.renderers &&
                       colliders == other.colliders &&
                       lights == other.lights &&
                       materials == other.materials &&
                       placements == other.placements &&
                       preferred == other.preferred &&
                       fallback == other.fallback;
            }

            public override bool Equals(object value)
            {
                return value is BuildSnapshot other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = transforms;
                    hash = hash * 397 ^ renderers;
                    hash = hash * 397 ^ colliders;
                    hash = hash * 397 ^ lights;
                    hash = hash * 397 ^ materials;
                    hash = hash * 397 ^ placements;
                    hash = hash * 397 ^ preferred;
                    hash = hash * 397 ^ fallback;
                    return hash;
                }
            }
        }
    }
}
