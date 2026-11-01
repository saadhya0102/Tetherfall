using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tetherfall
{
    public readonly struct SettlementPactPreferenceSnapshot
    {
        public SettlementPactPreferenceSnapshot(
            string settlementId,
            string stationId,
            string bedId,
            PactFieldSlotState fieldSlotState,
            int fieldSlot,
            bool wasActiveField,
            string commandFocusInstanceId)
        {
            SettlementId = settlementId ?? string.Empty;
            StationId = stationId ?? string.Empty;
            BedId = bedId ?? string.Empty;
            FieldSlotState = fieldSlotState;
            FieldSlot = fieldSlot;
            WasActiveField = wasActiveField;
            CommandFocusInstanceId = commandFocusInstanceId ?? string.Empty;
        }

        public string SettlementId { get; }
        public string StationId { get; }
        public string BedId { get; }
        public PactFieldSlotState FieldSlotState { get; }
        public int FieldSlot { get; }
        public bool WasActiveField { get; }
        public string CommandFocusInstanceId { get; }
        public bool HadSettlementPreference =>
            !string.IsNullOrEmpty(SettlementId) ||
            !string.IsNullOrEmpty(StationId) ||
            !string.IsNullOrEmpty(BedId);
    }

    public static class TetherfallSettlementPactBridge
    {
        private static readonly PactWorkKind[] BuilderKinds =
        {
            PactWorkKind.Crafting,
            PactWorkKind.Salvage
        };

        private static readonly PactWorkKind[] SmithKinds =
        {
            PactWorkKind.Heatkeeping,
            PactWorkKind.Crafting,
            PactWorkKind.Salvage
        };

        private static readonly PactWorkKind[] CookKinds =
        {
            PactWorkKind.Heatkeeping,
            PactWorkKind.Foraging,
            PactWorkKind.Care
        };

        private static readonly PactWorkKind[] WeaverKinds =
        {
            PactWorkKind.Crafting
        };

        private static readonly PactWorkKind[] HealerKinds =
        {
            PactWorkKind.Care,
            PactWorkKind.Foraging
        };

        private static readonly PactWorkKind[] FarmerKinds =
        {
            PactWorkKind.Cultivation,
            PactWorkKind.Foraging
        };

        private static readonly PactWorkKind[] WaterTenderKinds =
        {
            PactWorkKind.Waterkeeping,
            PactWorkKind.Care
        };

        private static readonly PactWorkKind[] RancherKinds =
        {
            PactWorkKind.Care,
            PactWorkKind.Foraging,
            PactWorkKind.Cultivation
        };

        private static readonly PactWorkKind[] WatchkeeperKinds =
        {
            PactWorkKind.Watch,
            PactWorkKind.Survey
        };

        private static readonly PactWorkKind[] HaulerKinds =
        {
            PactWorkKind.Courier,
            PactWorkKind.Salvage
        };

        public static IReadOnlyList<PactWorkKind> GetCompatibleWorkKinds(
            SettlementWorkRole role)
        {
            return role switch
            {
                SettlementWorkRole.Builder => BuilderKinds,
                SettlementWorkRole.Smith => SmithKinds,
                SettlementWorkRole.Cook => CookKinds,
                SettlementWorkRole.Weaver => WeaverKinds,
                SettlementWorkRole.Healer => HealerKinds,
                SettlementWorkRole.Farmer => FarmerKinds,
                SettlementWorkRole.WaterTender => WaterTenderKinds,
                SettlementWorkRole.Rancher => RancherKinds,
                SettlementWorkRole.Watchkeeper => WatchkeeperKinds,
                SettlementWorkRole.Hauler => HaulerKinds,
                _ => Array.Empty<PactWorkKind>()
            };
        }

        public static bool IsWorkKindCompatible(
            SettlementWorkRole role,
            PactWorkKind workKind)
        {
            var compatible = GetCompatibleWorkKinds(role);
            for (var index = 0; index < compatible.Count; index++)
            {
                if (compatible[index] == workKind)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryMatchWorkPreference(
            PactSpeciesDefinition species,
            SettlementWorkRole role,
            out PactWorkKind matchedKind,
            out PactWorkAffinity matchedAffinity)
        {
            matchedKind = default;
            matchedAffinity = null;
            if (species?.workAffinities == null ||
                role == SettlementWorkRole.None)
            {
                return false;
            }

            var compatible = GetCompatibleWorkKinds(role);
            for (var kindIndex = 0;
                 kindIndex < compatible.Count;
                 kindIndex++)
            {
                var candidate = TetherfallAccordSpeciesCatalog.FindWorkAffinity(
                    species,
                    compatible[kindIndex]);
                if (candidate == null ||
                    candidate.rating <= 0 ||
                    matchedAffinity != null &&
                    candidate.rating <= matchedAffinity.rating)
                {
                    continue;
                }

                matchedKind = candidate.kind;
                matchedAffinity = candidate;
            }

            return matchedAffinity != null;
        }

        public static CapturedPactSaveData FindPactRecord(
            IReadOnlyList<CapturedPactSaveData> roster,
            string instanceId)
        {
            return PactRosterRules.FindRecord(roster, instanceId);
        }

        public static SettlementPactPreferenceSnapshot CapturePreference(
            TetherfallGame game,
            CapturedPactSaveData record)
        {
            if (record == null)
            {
                return default;
            }

            var active = false;
            var activeSlot = record.fieldSlot;
            var activeIds = game?.ActivePactFieldIds;
            if (activeIds != null)
            {
                for (var index = 0; index < activeIds.Count; index++)
                {
                    if (string.Equals(
                            activeIds[index],
                            record.instanceId,
                            StringComparison.Ordinal))
                    {
                        active = true;
                        activeSlot = index;
                        break;
                    }
                }
            }

            return new SettlementPactPreferenceSnapshot(
                record.settlementId,
                record.stationId,
                record.bedId,
                record.fieldSlotState,
                activeSlot,
                active,
                game?.PactCommandFocusInstanceId);
        }

        public static void RestoreRecordFields(
            CapturedPactSaveData record,
            SettlementPactPreferenceSnapshot snapshot)
        {
            if (record == null)
            {
                return;
            }

            record.settlementId = snapshot.SettlementId;
            record.stationId = snapshot.StationId;
            record.bedId = snapshot.BedId;
            record.fieldSlotState = snapshot.FieldSlotState;
            record.fieldSlot = snapshot.FieldSlot;
        }

        public static void RollbackPactPreference(
            TetherfallGame game,
            CapturedPactSaveData record,
            SettlementPactPreferenceSnapshot snapshot)
        {
            if (record == null)
            {
                return;
            }

            game?.ClearPactSettlementPreference(record.instanceId);
            record.settlementId = string.Empty;
            record.stationId = string.Empty;
            record.bedId = string.Empty;
            record.fieldSlotState = PactFieldSlotState.Reserve;
            record.fieldSlot = -1;

            if (snapshot.HadSettlementPreference)
            {
                RestoreRecordFields(record, snapshot);
                return;
            }

            if (snapshot.WasActiveField && game != null)
            {
                var activeIds = game.ActivePactFieldIds;
                var restoreSlot = Mathf.Clamp(
                    snapshot.FieldSlot,
                    0,
                    activeIds.Count);
                var displaced = new string[activeIds.Count - restoreSlot];
                for (var index = 0; index < displaced.Length; index++)
                {
                    displaced[index] = activeIds[restoreSlot + index];
                }

                for (var index = displaced.Length - 1; index >= 0; index--)
                {
                    game.RemovePactFromField(displaced[index]);
                }

                var restored = game.TryAddPactToField(
                    record.instanceId,
                    out _);
                for (var index = 0; index < displaced.Length; index++)
                {
                    game.TryAddPactToField(displaced[index], out _);
                }

                if (!string.IsNullOrEmpty(snapshot.CommandFocusInstanceId))
                {
                    game.SetPactCommandFocus(snapshot.CommandFocusInstanceId);
                }

                if (restored)
                {
                    return;
                }
            }

            RestoreRecordFields(record, snapshot);
        }

        public static void InitializeWorkerVitalsFromPact(
            SettlementWorkerAssignmentSaveData worker,
            CapturedPactSaveData record)
        {
            if (worker == null || record == null)
            {
                return;
            }

            worker.stamina = SanitizeVital(record.stamina);
            worker.morale = SanitizeVital(record.morale);
        }

        public static bool WriteWorkerVitalsToPact(
            IReadOnlyList<CapturedPactSaveData> roster,
            SettlementWorkerAssignmentSaveData worker)
        {
            var record = worker != null
                ? FindPactRecord(roster, worker.pactInstanceId)
                : null;
            if (record == null)
            {
                return false;
            }

            worker.stamina = SanitizeVital(worker.stamina);
            worker.morale = SanitizeVital(worker.morale);
            record.stamina = worker.stamina;
            record.morale = worker.morale;
            return true;
        }

        public static void WriteWorkerVitalsToPactSave(
            TetherfallSaveData saveData,
            IReadOnlyList<SettlementWorkerAssignmentSaveData> workers)
        {
            if (saveData == null ||
                workers == null ||
                !PactRosterSaveBridge.TryRead(saveData, out var roster) ||
                roster == null)
            {
                return;
            }

            var changed = false;
            for (var index = 0; index < workers.Count; index++)
            {
                changed |= WriteWorkerVitalsToPact(roster.pacts, workers[index]);
            }

            if (changed)
            {
                PactRosterSaveBridge.Write(saveData, roster);
            }
        }

        private static float SanitizeVital(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? 0f
                : Mathf.Clamp(value, 0f, 100f);
        }
    }

    public sealed class TetherfallSettlementPactWorkerProxy :
        MonoBehaviour,
        ITetherfallSettlementWorkerProxy
    {
        private const float MinimumMoveSpeed = 1.35f;
        private const float MaximumMoveSpeed = 2.8f;
        private const float TurnSharpness = 7.5f;

        private CapturedPactArtRig artRig;
        private Vector3 targetPosition;
        private Vector3 targetForward = Vector3.forward;
        private bool targetVisible;
        private float efficiency;
        private bool retired;

        public string PactInstanceId { get; private set; } = string.Empty;

        public static TetherfallSettlementPactWorkerProxy Create(
            TetherfallGame game,
            Transform parent,
            CapturedPactSaveData record,
            PactSpeciesDefinition species,
            Vector3 initialPosition)
        {
            if (game == null ||
                record == null ||
                species == null ||
                !species.isWild ||
                !string.IsNullOrEmpty(record.linkedLegendaryPactId))
            {
                return null;
            }

            var root = new GameObject(
                $"Settlement Pact Worker · {species.displayName}");
            if (parent != null)
            {
                root.transform.SetParent(parent, true);
            }

            root.transform.position = initialPosition;
            root.SetActive(false);
            var proxy = root.AddComponent<TetherfallSettlementPactWorkerProxy>();
            proxy.PactInstanceId = record.instanceId;
            proxy.targetPosition = initialPosition;
            proxy.artRig = TetherfallCapturedPactArt.Build(
                game,
                root.transform,
                species);
            RemoveForbiddenProxyComponents(root);
            if (proxy.artRig == null)
            {
                DestroyProxyObject(root);
                return null;
            }

            return proxy;
        }

        private static void RemoveForbiddenProxyComponents(GameObject root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var index = 0; index < colliders.Length; index++)
            {
                if (colliders[index] == null)
                {
                    continue;
                }

                colliders[index].enabled = false;
                DestroyProxyObject(colliders[index]);
            }

            var lights = root.GetComponentsInChildren<Light>(true);
            for (var index = 0; index < lights.Length; index++)
            {
                if (lights[index] == null)
                {
                    continue;
                }

                lights[index].enabled = false;
                DestroyProxyObject(lights[index]);
            }

            var bodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (var index = 0; index < bodies.Length; index++)
            {
                if (bodies[index] == null)
                {
                    continue;
                }

                bodies[index].detectCollisions = false;
                bodies[index].isKinematic = true;
                DestroyProxyObject(bodies[index]);
            }
        }

        private static void DestroyProxyObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }

        public void SetSettlementProxyState(
            bool visibleNearSettlement,
            Vector3 workPosition,
            Vector3 workForward,
            SettlementWorkRole role,
            float workerEfficiency)
        {
            if (retired)
            {
                return;
            }

            targetVisible =
                visibleNearSettlement &&
                role != SettlementWorkRole.None &&
                TetherfallSettlementRules.IsFinite(workPosition);
            targetPosition = workPosition;
            workForward.y = 0f;
            if (workForward.sqrMagnitude > 0.001f)
            {
                targetForward = workForward.normalized;
            }

            efficiency = Mathf.Clamp(workerEfficiency, 0f, 1.2f);
            if (!targetVisible && gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        public void TickCentralized(float deltaTime, float unscaledTime)
        {
            if (retired || !targetVisible || artRig == null)
            {
                if (!retired && gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            var boundedDelta = Mathf.Clamp(deltaTime, 0f, 0.1f);
            var previousPosition = transform.position;
            var moveSpeed = Mathf.Lerp(
                MinimumMoveSpeed,
                MaximumMoveSpeed,
                Mathf.Clamp01(efficiency));
            transform.position = Vector3.MoveTowards(
                previousPosition,
                targetPosition,
                moveSpeed * boundedDelta);
            var movement = transform.position - previousPosition;
            var remaining = targetPosition - transform.position;
            remaining.y = 0f;
            var desiredForward = remaining.sqrMagnitude > 0.01f
                ? remaining.normalized
                : targetForward;
            if (desiredForward.sqrMagnitude > 0.001f && boundedDelta > 0f)
            {
                var desiredRotation = Quaternion.LookRotation(
                    desiredForward,
                    Vector3.up);
                var turnT = 1f - Mathf.Exp(-TurnSharpness * boundedDelta);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    turnT);
            }

            var velocity = boundedDelta > 0.0001f
                ? movement / boundedDelta
                : Vector3.zero;
            artRig.Tick(unscaledTime, velocity, false);
        }

        public void Retire()
        {
            if (retired)
            {
                return;
            }

            retired = true;
            targetVisible = false;
            artRig?.Dispose();
            artRig = null;
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void OnDestroy()
        {
            artRig?.Dispose();
            artRig = null;
        }
    }

    internal readonly struct SettlementWorkerStationOption
    {
        public SettlementWorkerStationOption(
            SettlementPlacedPieceSaveData piece,
            SettlementPieceDefinition definition,
            PactWorkKind workKind,
            PactWorkAffinity affinity)
        {
            Piece = piece;
            Definition = definition;
            WorkKind = workKind;
            Affinity = affinity;
        }

        public SettlementPlacedPieceSaveData Piece { get; }
        public SettlementPieceDefinition Definition { get; }
        public PactWorkKind WorkKind { get; }
        public PactWorkAffinity Affinity { get; }
    }

    public sealed partial class TetherfallGame
    {
        private readonly Dictionary<string, TetherfallSettlementPactWorkerProxy>
            playerSettlementPactWorkerProxies =
                new Dictionary<string, TetherfallSettlementPactWorkerProxy>(
                    TetherfallSettlementRules.HardWorkerCap,
                    StringComparer.Ordinal);
        private readonly List<string> playerSettlementPactWorkerRemovalBuffer =
            new List<string>(TetherfallSettlementRules.HardWorkerCap);
        private readonly HashSet<string> playerSettlementPactVitalsInitialized =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly List<SettlementWorkerStationOption>
            playerSettlementWorkerStationOptions =
                new List<SettlementWorkerStationOption>(
                    TetherfallSettlementRules.HardPieceCap);
        private readonly List<SettlementPlacedPieceSaveData>
            playerSettlementWorkerBedOptions =
                new List<SettlementPlacedPieceSaveData>(
                    TetherfallSettlementRules.HardWorkerCap);
        private readonly StringBuilder playerSettlementWorkerText =
            new StringBuilder(192);

        private Vector2 playerSettlementWorkerRosterScroll;
        private string selectedPlayerSettlementPactId = string.Empty;
        private string selectedPlayerSettlementWorkPieceId = string.Empty;
        private string selectedPlayerSettlementBedPieceId = string.Empty;

        public int PlayerSettlementVisibleWorkerProxyCount =>
            playerSettlementPactWorkerProxies.Count;

        private void ResetPlayerSettlementPactWorkerBridgeState()
        {
            playerSettlementPactVitalsInitialized.Clear();
            selectedPlayerSettlementPactId = string.Empty;
            selectedPlayerSettlementWorkPieceId = string.Empty;
            selectedPlayerSettlementBedPieceId = string.Empty;
            playerSettlementWorkerRosterScroll = Vector2.zero;
            DestroyAllPlayerSettlementPactWorkerProxies();
        }

        private void ReconcilePlayerSettlementPactAssignments()
        {
            if (playerSettlementState?.workers == null)
            {
                return;
            }

            for (var index = playerSettlementState.workers.Count - 1;
                 index >= 0;
                 index--)
            {
                var worker = playerSettlementState.workers[index];
                var record = worker != null
                    ? TetherfallSettlementPactBridge.FindPactRecord(
                        PactRoster,
                        worker.pactInstanceId)
                    : null;
                var species = record != null
                    ? TetherfallAccordSpeciesCatalog.Find(record.speciesId)
                    : null;
                var validPieces =
                    worker != null &&
                    TryGetPlayerSettlementPiece(
                        worker.workPieceId,
                        out var workRuntime,
                        out _,
                        out var workDefinition) &&
                    workRuntime != null &&
                    workRuntime.Durability > 0 &&
                    workDefinition.workSlots > 0 &&
                    workDefinition.workRole == worker.workRole &&
                    TryGetPlayerSettlementPiece(
                        worker.bedPieceId,
                        out var bedRuntime,
                        out _,
                        out var bedDefinition) &&
                    bedRuntime != null &&
                    bedRuntime.Durability > 0 &&
                    bedDefinition.role == SettlementPieceRole.PactBed;
                var matchedWork = default(PactWorkKind);
                var hasMatchedPreference =
                    validPieces &&
                    TetherfallSettlementPactBridge.TryMatchWorkPreference(
                        species,
                        worker.workRole,
                        out matchedWork,
                        out _);
                var assignedElsewhere =
                    record != null &&
                    record.IsSettlementAssigned &&
                    !string.Equals(
                        record.settlementId,
                        TetherfallSettlementCatalog.PlayerSettlementId,
                        StringComparison.Ordinal);
                if (record == null ||
                    species == null ||
                    !validPieces ||
                    !hasMatchedPreference ||
                    assignedElsewhere)
                {
                    if (record != null &&
                        string.Equals(
                            record.settlementId,
                            TetherfallSettlementCatalog.PlayerSettlementId,
                            StringComparison.Ordinal))
                    {
                        ClearPactSettlementPreference(record.instanceId);
                    }

                    ForgetPlayerSettlementWorkerAt(index);
                    continue;
                }

                var preferenceMatches =
                    string.Equals(
                        record.settlementId,
                        TetherfallSettlementCatalog.PlayerSettlementId,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        record.stationId,
                        worker.workPieceId,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        record.bedId,
                        worker.bedPieceId,
                        StringComparison.Ordinal);
                if (!preferenceMatches &&
                    !TryAssignPactSettlementPreference(
                        record.instanceId,
                        TetherfallSettlementCatalog.PlayerSettlementId,
                        worker.workPieceId,
                        worker.bedPieceId,
                        matchedWork,
                        out _))
                {
                    ForgetPlayerSettlementWorkerAt(index);
                    continue;
                }

                if (playerSettlementPactVitalsInitialized.Add(
                        worker.pactInstanceId))
                {
                    TetherfallSettlementPactBridge.InitializeWorkerVitalsFromPact(
                        worker,
                        record);
                }
            }

            var roster = PactRoster;
            for (var index = 0; index < roster.Count; index++)
            {
                var record = roster[index];
                if (record != null &&
                    string.Equals(
                        record.settlementId,
                        TetherfallSettlementCatalog.PlayerSettlementId,
                        StringComparison.Ordinal) &&
                    FindPlayerSettlementWorker(record.instanceId) == null)
                {
                    ClearPactSettlementPreference(record.instanceId);
                }
            }

            SynchronizePlayerSettlementPactWorkerProxies();
        }

        private void ForgetPlayerSettlementWorkerAt(int index)
        {
            if (playerSettlementState?.workers == null ||
                index < 0 ||
                index >= playerSettlementState.workers.Count)
            {
                return;
            }

            var worker = playerSettlementState.workers[index];
            playerSettlementState.workers.RemoveAt(index);
            if (worker == null)
            {
                return;
            }

            playerSettlementPactVitalsInitialized.Remove(worker.pactInstanceId);
            DestroyPlayerSettlementPactWorkerProxy(worker.pactInstanceId);
        }

        private void SynchronizeAllPlayerSettlementWorkerVitalsToPact()
        {
            if (playerSettlementState?.workers == null)
            {
                return;
            }

            var roster = PactRoster;
            for (var index = 0;
                 index < playerSettlementState.workers.Count;
                 index++)
            {
                var worker = playerSettlementState.workers[index];
                if (worker == null ||
                    !playerSettlementPactVitalsInitialized.Contains(
                        worker.pactInstanceId))
                {
                    continue;
                }

                TetherfallSettlementPactBridge.WriteWorkerVitalsToPact(
                    roster,
                    worker);
            }
        }

        private void UpdatePlayerSettlementPactWorkerProxies(
            float deltaTime)
        {
            SynchronizePlayerSettlementPactWorkerProxies();
            if (playerSettlementSimulationBand != SettlementSimulationBand.Near)
            {
                return;
            }

            foreach (var proxy in playerSettlementPactWorkerProxies.Values)
            {
                proxy?.TickCentralized(deltaTime, Time.unscaledTime);
            }
        }

        private void SynchronizePlayerSettlementPactWorkerProxies()
        {
            if (!HasClaimedPlayerSettlement ||
                playerSettlementSimulationBand != SettlementSimulationBand.Near)
            {
                DestroyAllPlayerSettlementPactWorkerProxies();
                return;
            }

            playerSettlementPactWorkerRemovalBuffer.Clear();
            foreach (var pair in playerSettlementPactWorkerProxies)
            {
                var worker = FindPlayerSettlementWorker(pair.Key);
                if (pair.Value == null ||
                    !TryResolvePlayerSettlementProxyInputs(
                        worker,
                        out _,
                        out _,
                        out _,
                        out _))
                {
                    playerSettlementPactWorkerRemovalBuffer.Add(pair.Key);
                }
            }

            for (var index = 0;
                 index < playerSettlementPactWorkerRemovalBuffer.Count;
                 index++)
            {
                DestroyPlayerSettlementPactWorkerProxy(
                    playerSettlementPactWorkerRemovalBuffer[index]);
            }

            var proxyCap = Mathf.Min(
                TetherfallSettlementRules.HardWorkerCap,
                TetherfallSettlementRules.GetWorkerCap(
                    playerSettlementState.coreTier));
            for (var index = 0;
                 index < playerSettlementState.workers.Count &&
                 playerSettlementPactWorkerProxies.Count < proxyCap;
                 index++)
            {
                var worker = playerSettlementState.workers[index];
                if (worker == null ||
                    playerSettlementPactWorkerProxies.ContainsKey(
                        worker.pactInstanceId) ||
                    settlementWorkerProxies.ContainsKey(
                        worker.pactInstanceId) ||
                    !TryResolvePlayerSettlementProxyInputs(
                        worker,
                        out var record,
                        out var species,
                        out var position,
                        out _))
                {
                    continue;
                }

                var proxy = TetherfallSettlementPactWorkerProxy.Create(
                    this,
                    playerSettlementRuntimeRoot,
                    record,
                    species,
                    position);
                if (proxy == null ||
                    !RegisterPlayerSettlementWorkerProxy(proxy))
                {
                    proxy?.Retire();
                    continue;
                }

                playerSettlementPactWorkerProxies[record.instanceId] = proxy;
            }

            RefreshPlayerSettlementWorkerProxies();
        }

        private bool TryResolvePlayerSettlementProxyInputs(
            SettlementWorkerAssignmentSaveData worker,
            out CapturedPactSaveData record,
            out PactSpeciesDefinition species,
            out Vector3 position,
            out Vector3 forward)
        {
            record = worker != null
                ? TetherfallSettlementPactBridge.FindPactRecord(
                    PactRoster,
                    worker.pactInstanceId)
                : null;
            species = record != null
                ? TetherfallAccordSpeciesCatalog.Find(record.speciesId)
                : null;
            position = Vector3.zero;
            forward = Vector3.forward;
            if (worker == null ||
                record == null ||
                species == null ||
                !species.isWild ||
                !string.IsNullOrEmpty(record.linkedLegendaryPactId) ||
                !string.Equals(
                    record.settlementId,
                    TetherfallSettlementCatalog.PlayerSettlementId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    record.stationId,
                    worker.workPieceId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    record.bedId,
                    worker.bedPieceId,
                    StringComparison.Ordinal) ||
                !playerSettlementPieces.TryGetValue(
                    worker.workPieceId,
                    out var workPiece) ||
                workPiece == null)
            {
                return false;
            }

            return workPiece.TryGetWorkSocketPose(
                GetPlayerSettlementWorkSlotIndex(worker),
                out position,
                out forward);
        }

        private int GetPlayerSettlementWorkSlotIndex(
            SettlementWorkerAssignmentSaveData worker)
        {
            var slot = 0;
            if (worker == null || playerSettlementState?.workers == null)
            {
                return slot;
            }

            for (var index = 0;
                 index < playerSettlementState.workers.Count;
                 index++)
            {
                var candidate = playerSettlementState.workers[index];
                if (candidate == worker)
                {
                    break;
                }

                if (candidate != null &&
                    string.Equals(
                        candidate.workPieceId,
                        worker.workPieceId,
                        StringComparison.Ordinal))
                {
                    slot++;
                }
            }

            return slot;
        }

        private void DestroyPlayerSettlementPactWorkerProxy(
            string pactInstanceId)
        {
            if (string.IsNullOrEmpty(pactInstanceId) ||
                !playerSettlementPactWorkerProxies.TryGetValue(
                    pactInstanceId,
                    out var proxy))
            {
                return;
            }

            UnregisterPlayerSettlementWorkerProxy(pactInstanceId);
            playerSettlementPactWorkerProxies.Remove(pactInstanceId);
            if (proxy != null)
            {
                proxy.Retire();
            }
        }

        private void DestroyAllPlayerSettlementPactWorkerProxies()
        {
            foreach (var pair in playerSettlementPactWorkerProxies)
            {
                UnregisterPlayerSettlementWorkerProxy(pair.Key);
                if (pair.Value != null)
                {
                    pair.Value.Retire();
                }
            }

            playerSettlementPactWorkerProxies.Clear();
            playerSettlementPactWorkerRemovalBuffer.Clear();
        }

        private void DrawPlayerSettlementPactWorkersPage(Rect rect)
        {
            var cap = HasClaimedPlayerSettlement
                ? TetherfallSettlementRules.GetWorkerCap(
                    playerSettlementState.coreTier)
                : 0;
            GUI.Label(
                new Rect(rect.x, rect.y, rect.width, 24f),
                $"Accord roster · settlement {playerSettlementState.workers.Count}/{cap}",
                headingStyle);
            GUI.Label(
                new Rect(rect.x, rect.y + 25f, rect.width, 38f),
                $"Field fights with you; Reserve stays abstract; Settlement works here. " +
                $"Concord x{PactConcordMultiplier:0.00} · Field " +
                $"{ActiveCombatPactCount}/{MaximumActivePactSlots}. " +
                "Rank 2/4/6 total field capacity: 3/4/5.",
                mutedStyle);

            var roster = PactRoster;
            SelectFirstReachablePlayerSettlementPact(roster);
            var listRect = new Rect(
                rect.x,
                rect.y + 68f,
                rect.width,
                Mathf.Clamp(rect.height - 220f, 96f, 220f));
            var contentWidth = Mathf.Max(240f, listRect.width - 18f);
            var contentHeight = Mathf.Max(
                listRect.height,
                roster.Count * 66f + 4f);
            playerSettlementWorkerRosterScroll = GUI.BeginScrollView(
                listRect,
                playerSettlementWorkerRosterScroll,
                new Rect(0f, 0f, contentWidth, contentHeight));
            var y = 2f;
            for (var index = 0; index < roster.Count; index++)
            {
                var record = roster[index];
                var species = record != null
                    ? TetherfallAccordSpeciesCatalog.Find(record.speciesId)
                    : null;
                if (record == null || species == null)
                {
                    continue;
                }

                var selected = string.Equals(
                    selectedPlayerSettlementPactId,
                    record.instanceId,
                    StringComparison.Ordinal);
                GUI.Box(
                    new Rect(0f, y, contentWidth - 2f, 60f),
                    GUIContent.none);
                GUI.Label(
                    new Rect(10f, y + 4f, contentWidth - 106f, 18f),
                    $"{TrimWorkerUiText(PactRosterRules.GetDisplayName(record), 42)} · " +
                    $"Species: {species.displayName}",
                    bodyStyle);
                GUI.Label(
                    new Rect(10f, y + 22f, contentWidth - 106f, 17f),
                    $"Work: {DescribePlayerSettlementPactAffinities(species)}",
                    mutedStyle);
                GUI.Label(
                    new Rect(10f, y + 39f, contentWidth - 106f, 17f),
                    $"{GetPlayerSettlementPactRosterState(record)} · " +
                    $"Stamina {record.stamina:0} · Morale {record.morale:0}",
                    mutedStyle);
                if (GUI.Button(
                        new Rect(contentWidth - 88f, y + 14f, 78f, 30f),
                        selected ? "Selected" : "Choose"))
                {
                    SelectPlayerSettlementPact(record.instanceId);
                }

                y += 66f;
            }

            GUI.EndScrollView();
            var editorRect = new Rect(
                rect.x,
                listRect.yMax + 8f,
                rect.width,
                Mathf.Max(0f, rect.yMax - listRect.yMax - 8f));
            DrawPlayerSettlementPactAssignmentEditor(editorRect);
        }

        private void DrawPlayerSettlementPactAssignmentEditor(Rect rect)
        {
            var record = TetherfallSettlementPactBridge.FindPactRecord(
                PactRoster,
                selectedPlayerSettlementPactId);
            var species = record != null
                ? TetherfallAccordSpeciesCatalog.Find(record.speciesId)
                : null;
            if (record == null || species == null)
            {
                GUI.Label(
                    new Rect(rect.x, rect.y, rect.width, 40f),
                    "No Accord is available for settlement work.",
                    bodyStyle);
                return;
            }

            var existing = FindPlayerSettlementWorker(record.instanceId);
            PopulatePlayerSettlementWorkerOptions(record, species);
            EnsurePlayerSettlementWorkerOptionSelection(existing);
            var stationIndex = FindPlayerSettlementStationOptionIndex(
                selectedPlayerSettlementWorkPieceId);
            var bedIndex = FindPlayerSettlementBedOptionIndex(
                selectedPlayerSettlementBedPieceId);
            var station = stationIndex >= 0
                ? playerSettlementWorkerStationOptions[stationIndex]
                : default;
            var bed = bedIndex >= 0
                ? playerSettlementWorkerBedOptions[bedIndex]
                : null;

            GUI.Label(
                new Rect(rect.x, rect.y, rect.width, 20f),
                $"Assignment for {TrimWorkerUiText(PactRosterRules.GetDisplayName(record), 48)}",
                bodyStyle);
            DrawPlayerSettlementWorkerOptionRow(
                new Rect(rect.x, rect.y + 24f, rect.width, 28f),
                stationIndex >= 0
                    ? $"Work: {station.Definition.displayName} " +
                      $"{GetPlayerSettlementPieceChoiceTag(station.Piece.instanceId)} · " +
                      $"{station.Definition.workRole} via {station.WorkKind} " +
                      $"{station.Affinity.rating}"
                    : "Work: no free placed station matches this Accord",
                true,
                stationIndex);
            DrawPlayerSettlementWorkerOptionRow(
                new Rect(rect.x, rect.y + 56f, rect.width, 28f),
                bed != null
                    ? $"Bed: Pact Bed " +
                      $"{GetPlayerSettlementPieceChoiceTag(bed.instanceId)}"
                    : "Bed: build or free a Pact bed",
                false,
                bedIndex);

            var assignedElsewhere =
                record.IsSettlementAssigned &&
                !string.Equals(
                    record.settlementId,
                    TetherfallSettlementCatalog.PlayerSettlementId,
                    StringComparison.Ordinal);
            var previousEnabled = GUI.enabled;
            GUI.enabled =
                previousEnabled &&
                !assignedElsewhere &&
                record.morale >= 25f &&
                stationIndex >= 0 &&
                bedIndex >= 0;
            if (GUI.Button(
                    new Rect(rect.x, rect.y + 90f, 160f, 30f),
                    existing == null ? "Assign to settlement" : "Apply assignment"))
            {
                if (TryAssignPlayerSettlementWorker(
                        record.instanceId,
                        station.Definition.workRole,
                        station.Piece.instanceId,
                        bed.instanceId,
                        out var failure))
                {
                    Notify(
                        $"{PactRosterRules.GetDisplayName(record)} chose " +
                        $"{station.Definition.displayName}.");
                }
                else
                {
                    Notify(failure);
                }
            }

            GUI.enabled = previousEnabled && existing != null;
            if (GUI.Button(
                    new Rect(rect.x + 168f, rect.y + 90f, 106f, 30f),
                    "Unassign"))
            {
                UnassignPlayerSettlementWorker(record.instanceId);
            }

            GUI.enabled = previousEnabled;
            var guidance = assignedElsewhere
                ? "This Accord is assigned to another settlement."
                : record.morale < 25f
                    ? "Morale must reach 25 before settlement work."
                    : stationIndex < 0
                        ? "Place a compatible station with a free work socket."
                        : bedIndex < 0
                            ? "Place a free Pact bed before assigning this Accord."
                            : "Prev and Next cycle only through compatible free choices.";
            GUI.Label(
                new Rect(rect.x + 286f, rect.y + 91f, rect.width - 286f, 34f),
                guidance,
                mutedStyle);
        }

        private void DrawPlayerSettlementWorkerOptionRow(
            Rect rect,
            string label,
            bool station,
            int selectedIndex)
        {
            var count = station
                ? playerSettlementWorkerStationOptions.Count
                : playerSettlementWorkerBedOptions.Count;
            GUI.Label(
                new Rect(rect.x + 58f, rect.y + 3f, rect.width - 116f, 22f),
                label,
                mutedStyle);
            var previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && count > 1;
            if (GUI.Button(new Rect(rect.x, rect.y, 50f, 26f), "Prev"))
            {
                CyclePlayerSettlementWorkerOption(station, selectedIndex, -1);
            }

            if (GUI.Button(
                    new Rect(rect.xMax - 50f, rect.y, 50f, 26f),
                    "Next"))
            {
                CyclePlayerSettlementWorkerOption(station, selectedIndex, 1);
            }

            GUI.enabled = previousEnabled;
        }

        private void PopulatePlayerSettlementWorkerOptions(
            CapturedPactSaveData record,
            PactSpeciesDefinition species)
        {
            playerSettlementWorkerStationOptions.Clear();
            playerSettlementWorkerBedOptions.Clear();
            if (playerSettlementState?.pieces == null)
            {
                return;
            }

            for (var index = 0;
                 index < playerSettlementState.pieces.Count;
                 index++)
            {
                var piece = playerSettlementState.pieces[index];
                var definition = piece != null
                    ? TetherfallSettlementCatalog.FindPiece(piece.definitionId)
                    : null;
                if (piece == null ||
                    definition == null ||
                    !playerSettlementPieces.TryGetValue(
                        piece.instanceId,
                        out var runtime) ||
                    runtime == null ||
                    runtime.Durability <= 0)
                {
                    continue;
                }

                if (definition.workSlots > 0 &&
                    CountPlayerSettlementAssignments(
                        piece.instanceId,
                        false,
                        record.instanceId) < definition.workSlots &&
                    TetherfallSettlementPactBridge.TryMatchWorkPreference(
                        species,
                        definition.workRole,
                        out var workKind,
                        out var affinity))
                {
                    playerSettlementWorkerStationOptions.Add(
                        new SettlementWorkerStationOption(
                            piece,
                            definition,
                            workKind,
                            affinity));
                }

                if (definition.role == SettlementPieceRole.PactBed &&
                    CountPlayerSettlementAssignments(
                        piece.instanceId,
                        true,
                        record.instanceId) < definition.bedSlots)
                {
                    playerSettlementWorkerBedOptions.Add(piece);
                }
            }
        }

        private void EnsurePlayerSettlementWorkerOptionSelection(
            SettlementWorkerAssignmentSaveData existing)
        {
            if (FindPlayerSettlementStationOptionIndex(
                    selectedPlayerSettlementWorkPieceId) < 0)
            {
                var existingIndex = FindPlayerSettlementStationOptionIndex(
                    existing?.workPieceId);
                selectedPlayerSettlementWorkPieceId =
                    existingIndex >= 0
                        ? playerSettlementWorkerStationOptions[existingIndex]
                            .Piece.instanceId
                        : playerSettlementWorkerStationOptions.Count > 0
                            ? playerSettlementWorkerStationOptions[0]
                                .Piece.instanceId
                            : string.Empty;
            }

            if (FindPlayerSettlementBedOptionIndex(
                    selectedPlayerSettlementBedPieceId) < 0)
            {
                var existingIndex = FindPlayerSettlementBedOptionIndex(
                    existing?.bedPieceId);
                selectedPlayerSettlementBedPieceId =
                    existingIndex >= 0
                        ? playerSettlementWorkerBedOptions[existingIndex].instanceId
                        : playerSettlementWorkerBedOptions.Count > 0
                            ? playerSettlementWorkerBedOptions[0].instanceId
                            : string.Empty;
            }
        }

        private int FindPlayerSettlementStationOptionIndex(string pieceId)
        {
            for (var index = 0;
                 index < playerSettlementWorkerStationOptions.Count;
                 index++)
            {
                if (string.Equals(
                        playerSettlementWorkerStationOptions[index]
                            .Piece.instanceId,
                        pieceId,
                        StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindPlayerSettlementBedOptionIndex(string pieceId)
        {
            for (var index = 0;
                 index < playerSettlementWorkerBedOptions.Count;
                 index++)
            {
                if (string.Equals(
                        playerSettlementWorkerBedOptions[index].instanceId,
                        pieceId,
                        StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private void CyclePlayerSettlementWorkerOption(
            bool station,
            int selectedIndex,
            int direction)
        {
            var count = station
                ? playerSettlementWorkerStationOptions.Count
                : playerSettlementWorkerBedOptions.Count;
            if (count <= 0)
            {
                return;
            }

            var next = (Mathf.Max(0, selectedIndex) + direction + count) % count;
            if (station)
            {
                selectedPlayerSettlementWorkPieceId =
                    playerSettlementWorkerStationOptions[next].Piece.instanceId;
            }
            else
            {
                selectedPlayerSettlementBedPieceId =
                    playerSettlementWorkerBedOptions[next].instanceId;
            }
        }

        private void SelectFirstReachablePlayerSettlementPact(
            IReadOnlyList<CapturedPactSaveData> roster)
        {
            if (TetherfallSettlementPactBridge.FindPactRecord(
                    roster,
                    selectedPlayerSettlementPactId) != null)
            {
                return;
            }

            selectedPlayerSettlementPactId = string.Empty;
            for (var index = 0; index < roster.Count; index++)
            {
                if (roster[index] != null)
                {
                    SelectPlayerSettlementPact(roster[index].instanceId);
                    return;
                }
            }
        }

        private void SelectPlayerSettlementPact(string instanceId)
        {
            selectedPlayerSettlementPactId = instanceId ?? string.Empty;
            var existing = FindPlayerSettlementWorker(
                selectedPlayerSettlementPactId);
            selectedPlayerSettlementWorkPieceId =
                existing?.workPieceId ?? string.Empty;
            selectedPlayerSettlementBedPieceId =
                existing?.bedPieceId ?? string.Empty;
        }

        private string GetPlayerSettlementPactRosterState(
            CapturedPactSaveData record)
        {
            if (record == null)
            {
                return "UNAVAILABLE";
            }

            if (record.IsSettlementAssigned)
            {
                return string.Equals(
                    record.settlementId,
                    TetherfallSettlementCatalog.PlayerSettlementId,
                    StringComparison.Ordinal)
                    ? "SETTLEMENT"
                    : "SETTLEMENT ELSEWHERE";
            }

            for (var index = 0; index < ActivePactFieldIds.Count; index++)
            {
                if (string.Equals(
                        ActivePactFieldIds[index],
                        record.instanceId,
                        StringComparison.Ordinal))
                {
                    return $"FIELD SLOT {index + 1}";
                }
            }

            return "RESERVE";
        }

        private string DescribePlayerSettlementPactAffinities(
            PactSpeciesDefinition species)
        {
            playerSettlementWorkerText.Clear();
            var affinities = species?.workAffinities;
            if (affinities == null || affinities.Length == 0)
            {
                return "none authored";
            }

            for (var index = 0; index < affinities.Length; index++)
            {
                var affinity = affinities[index];
                if (affinity == null || affinity.rating <= 0)
                {
                    continue;
                }

                if (playerSettlementWorkerText.Length > 0)
                {
                    playerSettlementWorkerText.Append(" · ");
                }

                playerSettlementWorkerText.Append(affinity.kind);
                playerSettlementWorkerText.Append(' ');
                playerSettlementWorkerText.Append(affinity.rating);
            }

            return TrimWorkerUiText(
                playerSettlementWorkerText.Length > 0
                    ? playerSettlementWorkerText.ToString()
                    : "none authored",
                84);
        }

        private static string TrimWorkerUiText(string value, int maximumLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maximumLength)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, Mathf.Max(0, maximumLength - 3)) + "...";
        }

        private static string GetPlayerSettlementPieceChoiceTag(string pieceId)
        {
            if (string.IsNullOrEmpty(pieceId))
            {
                return "#?";
            }

            var separator = pieceId.LastIndexOf('.');
            return separator >= 0 && separator < pieceId.Length - 1
                ? "#" + pieceId.Substring(separator + 1)
                : TrimWorkerUiText(pieceId, 12);
        }
    }
}
