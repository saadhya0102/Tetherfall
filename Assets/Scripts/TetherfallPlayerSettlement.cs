using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    [Serializable]
    public sealed class SettlementSimulationSummary
    {
        public double simulatedSeconds;
        public int completedProductionCycles;
        public int mealsConsumed;
        public int resourcesProduced;
        public bool cycleLimitReached;

        public bool DidWork =>
            completedProductionCycles > 0 ||
            mealsConsumed > 0 ||
            resourcesProduced > 0;
    }

    public sealed partial class TetherfallGame
    {
        private const float MaximumCoreClaimDistanceFromPlayer = 12f;
        private const float MaximumSettlementVerticalExtent = 24f;
        private const float WorkerNeedIntervalSeconds = 900f;
        private const float FedTroughNeedIntervalSeconds = 1200f;
        private const float SettlementSaveRequestInterval = 20f;
        private const int CatalogRowsPerPage = 7;
        private static readonly string[] SettlementCompassDirections =
        {
            "N", "NE", "E", "SE", "S", "SW", "W", "NW"
        };

        private readonly Dictionary<string, TetherfallSettlementPiece> playerSettlementPieces =
            new Dictionary<string, TetherfallSettlementPiece>(StringComparer.Ordinal);
        private readonly Dictionary<ResourceKind, int> playerSettlementStorage =
            new Dictionary<ResourceKind, int>();
        private readonly Dictionary<string, ITetherfallSettlementWorkerProxy> settlementWorkerProxies =
            new Dictionary<string, ITetherfallSettlementWorkerProxy>(StringComparer.Ordinal);
        private readonly List<SettlementSnapPoint> settlementSnapBuffer =
            new List<SettlementSnapPoint>(64);

        private SettlementSaveData playerSettlementState;
        private Transform playerSettlementRuntimeRoot;
        private Transform playerSettlementPlacementRoot;
        private TetherfallSettlementPlacementController playerSettlementPlacement;
        private bool playerSettlementInitialized;
        private bool playerSettlementPanelOpen;
        private int playerSettlementPanelPage;
        private int playerSettlementCatalogPage;
        private int playerSettlementInputCapturedFrame = -1;
        private string selectedPlayerSettlementPieceId = string.Empty;
        private float nextPlayerSettlementTickAt;
        private float nextPlayerSettlementSaveRequestAt;
        private SettlementSimulationBand playerSettlementSimulationBand =
            SettlementSimulationBand.Near;

        public bool HasClaimedPlayerSettlement =>
            playerSettlementState != null &&
            playerSettlementState.coreClaimed &&
            GetPlayerSettlementCoreRecord() != null;

        public bool IsPlayerSettlementPanelOpen => playerSettlementPanelOpen;

        public bool IsPlayerSettlementPlacementActive =>
            playerSettlementPlacement != null && playerSettlementPlacement.IsActive;

        public bool IsPlayerSettlementInputCaptured =>
            playerSettlementPanelOpen ||
            IsPlayerSettlementPlacementActive ||
            playerSettlementInputCapturedFrame == Time.frameCount;

        public bool CanOpenPlayerSettlement =>
            string.IsNullOrEmpty(GetPlayerSettlementPlanningBlockReason());

        public string GetPlayerSettlementPlanningBlockReason()
        {
            if (!playerSettlementInitialized)
            {
                return "Settlement planning is still initializing.";
            }

            if (Player == null)
            {
                return "Settlement planning needs an active Wayfinder.";
            }

            if (IsPlayerSettlementPlacementActive)
            {
                return "Finish or cancel the current settlement placement first.";
            }

            if (CodexOpen || activePanel != GamePanel.None)
            {
                return "Close the current field panel before opening settlement planning.";
            }

            if (!HasClaimedPlayerSettlement)
            {
                return string.Empty;
            }

            var radius = TetherfallSettlementRules.GetBuildRadius(
                playerSettlementState.coreTier) + 8f;
            var offset = GetPlayerSettlementCorePosition() - PlayerTransform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= radius * radius)
            {
                return string.Empty;
            }

            return
                $"Threadheart is {offset.magnitude:0} m " +
                $"{GetPlayerSettlementCompassDirection(offset)}; move within " +
                $"{radius:0} m to manage it.";
        }

        public string GetPlayerSettlementHudHint()
        {
            if (!playerSettlementInitialized ||
                Player == null ||
                CodexOpen ||
                activePanel != GamePanel.None ||
                IsPlayerSettlementInputCaptured)
            {
                return string.Empty;
            }

            var planningBinding =
                TetherfallInputBindings.Shared.GetBindingSummary(
                    TetherfallInputAction.OpenSettlementPlanning);
            if (!HasClaimedPlayerSettlement)
            {
                return CanOpenPlayerSettlement
                    ? $"{planningBinding}  Settlement planning available"
                    : string.Empty;
            }

            var offset = GetPlayerSettlementCorePosition() - PlayerTransform.position;
            offset.y = 0f;
            var distance = offset.magnitude;
            var managementRadius =
                TetherfallSettlementRules.GetBuildRadius(
                    playerSettlementState.coreTier) + 8f;
            return distance <= managementRadius
                ? $"{planningBinding}  Settlement planning · Threadheart {distance:0} m"
                : $"Threadheart {GetPlayerSettlementCompassDirection(offset)} · {distance:0} m";
        }

        public int PlayerSettlementCoreTier =>
            HasClaimedPlayerSettlement ? playerSettlementState.coreTier : 0;

        public int PlayerSettlementPieceCount =>
            playerSettlementState?.pieces?.Count ?? 0;

        public int PlayerSettlementWorkerCount =>
            playerSettlementState?.workers?.Count ?? 0;

        public SettlementSimulationBand PlayerSettlementSimulationBand =>
            playerSettlementSimulationBand;

        public string PlayerSettlementPlacementFailureText =>
            playerSettlementPlacement?.FailureText ?? string.Empty;

        private bool IsPlayerSettlementCoreOperational =>
            playerSettlementPieces.TryGetValue(
                TetherfallSettlementCatalog.ThreadheartCoreInstanceId,
                out var coreRuntime) &&
            coreRuntime != null &&
            coreRuntime.Durability > 0;

        public void InitializePlayerSettlementSystem()
        {
            if (playerSettlementInitialized)
            {
                return;
            }

            playerSettlementInitialized = true;
            playerSettlementState = TetherfallSettlementPersistence.CreateDefault(
                DateTime.UtcNow.Ticks);
            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                playerSettlementStorage[kind] = 0;
            }

            var runtimeRoot = new GameObject("Player Settlement Runtime");
            runtimeRoot.transform.SetParent(transform, false);
            playerSettlementRuntimeRoot = runtimeRoot.transform;

            var placementRoot = new GameObject("Placement Preview");
            placementRoot.transform.SetParent(playerSettlementRuntimeRoot, false);
            playerSettlementPlacementRoot = placementRoot.transform;
            playerSettlementPlacement = new TetherfallSettlementPlacementController(
                this,
                playerSettlementPlacementRoot);
            nextPlayerSettlementTickAt = Time.unscaledTime;
            nextPlayerSettlementSaveRequestAt =
                Time.unscaledTime + SettlementSaveRequestInterval;
        }

        public void UpdatePlayerSettlementSystem()
        {
            if (!playerSettlementInitialized)
            {
                return;
            }

            if (playerSettlementPlacement != null && playerSettlementPlacement.IsActive)
            {
                playerSettlementInputCapturedFrame = Time.frameCount;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                playerSettlementPlacement.Tick();
            }
            else if (playerSettlementPanelOpen &&
                     TetherfallInputBindings.Shared.WasPressedThisFrame(
                         TetherfallInputAction.CancelOrReleaseCursor))
            {
                playerSettlementInputCapturedFrame = Time.frameCount;
                ClosePlayerSettlementPanel();
            }
            else if (playerSettlementPanelOpen)
            {
                playerSettlementInputCapturedFrame = Time.frameCount;
            }

            if (!HasClaimedPlayerSettlement)
            {
                DestroyAllPlayerSettlementPactWorkerProxies();
                return;
            }

            var corePosition = GetPlayerSettlementCorePosition();
            var playerPosition = PlayerTransform != null
                ? PlayerTransform.position
                : corePosition + Vector3.one * 1000f;
            var distance = Vector3.Distance(playerPosition, corePosition);
            var band = TetherfallSettlementRules.GetSimulationBand(distance);
            if (band != playerSettlementSimulationBand)
            {
                SetPlayerSettlementSimulationBand(band);
            }

            var simulationTickDue = Time.unscaledTime >= nextPlayerSettlementTickAt;
            if (simulationTickDue)
            {
                ReconcilePlayerSettlementPactAssignments();
            }

            UpdatePlayerSettlementPactWorkerProxies(Time.unscaledDeltaTime);
            if (!simulationTickDue)
            {
                return;
            }

            nextPlayerSettlementTickAt =
                Time.unscaledTime + TetherfallSettlementRules.GetTickSeconds(band);
            var summary = AdvancePlayerSettlementSimulationToUtc(
                DateTime.UtcNow.Ticks,
                false);
            RefreshPlayerSettlementWorkerProxies();
            if (summary.DidWork &&
                Time.unscaledTime >= nextPlayerSettlementSaveRequestAt)
            {
                nextPlayerSettlementSaveRequestAt =
                    Time.unscaledTime + SettlementSaveRequestInterval;
                RequestSave();
            }
        }

        public void WritePlayerSettlementSave(TetherfallSaveData data)
        {
            if (data == null)
            {
                return;
            }

            if (!playerSettlementInitialized)
            {
                InitializePlayerSettlementSystem();
            }

            ReconcilePlayerSettlementPactAssignments();
            AdvancePlayerSettlementSimulationToUtc(DateTime.UtcNow.Ticks, false);
            SynchronizeAllPlayerSettlementWorkerVitalsToPact();
            foreach (var piece in playerSettlementPieces.Values)
            {
                piece?.CopyTransformToRecord();
            }

            SyncPlayerSettlementStorageToState();
            var copy = TetherfallSettlementPersistence.Clone(playerSettlementState);
            TetherfallSettlementPersistence.NormalizeInPlace(
                copy,
                DateTime.UtcNow.Ticks);
            data.playerSettlement = copy;
            TetherfallSettlementPactBridge.WriteWorkerVitalsToPactSave(
                data,
                copy.workers);
        }

        public void RestorePlayerSettlementState(TetherfallSaveData data)
        {
            if (!playerSettlementInitialized)
            {
                InitializePlayerSettlementSystem();
            }

            playerSettlementPlacement?.Cancel(false);
            playerSettlementPanelOpen = false;
            selectedPlayerSettlementPieceId = string.Empty;
            ResetPlayerSettlementPactWorkerBridgeState();
            ClearPlayerSettlementRuntimePieces();

            var restored = data?.playerSettlement != null
                ? TetherfallSettlementPersistence.Clone(data.playerSettlement)
                : TetherfallSettlementPersistence.CreateDefault();
            var nowTicks = DateTime.UtcNow.Ticks;
            var previousSimulationTicks = restored.lastSimulatedUtcTicks;
            var normalization = TetherfallSettlementPersistence.NormalizeInPlace(
                restored,
                nowTicks);
            playerSettlementState = restored;
            LoadPlayerSettlementStorageFromState();

            if (playerSettlementState.pieces != null)
            {
                foreach (var record in playerSettlementState.pieces)
                {
                    SpawnPlayerSettlementRuntimePiece(record);
                }
            }

            playerSettlementSimulationBand = SettlementSimulationBand.Near;
            if (HasClaimedPlayerSettlement)
            {
                var distance = Vector3.Distance(
                    PlayerTransform.position,
                    GetPlayerSettlementCorePosition());
                SetPlayerSettlementSimulationBand(
                    TetherfallSettlementRules.GetSimulationBand(distance));
            }

            ReconcilePlayerSettlementPactAssignments();
            var offlineSeconds = TetherfallSettlementRules.ClampOfflineSeconds(
                previousSimulationTicks,
                nowTicks);
            playerSettlementState.lastSimulatedUtcTicks = nowTicks;
            var offlineSummary = SimulatePlayerSettlement(
                offlineSeconds,
                true);

            if (normalization.Changed)
            {
                Debug.LogWarning(
                    "Player settlement save data required bounded corruption normalization: " +
                    $"{normalization.droppedPieces} pieces, " +
                    $"{normalization.droppedWorkers} workers, and " +
                    $"{normalization.droppedJobs} jobs were discarded.");
            }

            if (offlineSummary.completedProductionCycles > 0)
            {
                Notify(
                    $"Settlement report: {offlineSummary.completedProductionCycles} " +
                    $"offline cycles completed across " +
                    $"{offlineSummary.simulatedSeconds / 3600d:0.#} h.");
            }

            nextPlayerSettlementTickAt =
                Time.unscaledTime +
                TetherfallSettlementRules.GetTickSeconds(
                    playerSettlementSimulationBand);
            RefreshPlayerSettlementWorkerProxies();
        }

        public void OpenPlayerSettlementPanel()
        {
            if (!CanOpenPlayerSettlement)
            {
                Notify(GetPlayerSettlementPlanningBlockReason());
                return;
            }

            playerSettlementPanelOpen = true;
            selectedPlayerSettlementPieceId = string.Empty;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ClosePlayerSettlementPanel()
        {
            playerSettlementInputCapturedFrame = Time.frameCount;
            playerSettlementPanelOpen = false;
            selectedPlayerSettlementPieceId = string.Empty;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void OpenPlayerSettlementPanelForPiece(string pieceInstanceId)
        {
            if (!CanOpenPlayerSettlement ||
                string.IsNullOrEmpty(pieceInstanceId) ||
                !playerSettlementPieces.ContainsKey(pieceInstanceId))
            {
                return;
            }

            playerSettlementPanelOpen = true;
            playerSettlementPanelPage = 0;
            selectedPlayerSettlementPieceId = pieceInstanceId;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public bool BeginPlayerSettlementPlacement(
            string definitionId,
            out string failure)
        {
            failure = string.Empty;
            if (!playerSettlementInitialized)
            {
                InitializePlayerSettlementSystem();
            }

            var definition = TetherfallSettlementCatalog.FindPiece(definitionId);
            if (definition == null)
            {
                failure = "That settlement piece is not in the stable catalog.";
                return false;
            }

            var validationPosition = definition.role == SettlementPieceRole.ThreadheartCore
                ? PlayerTransform.position
                : GetPlayerSettlementCorePosition();
            if (!ValidatePlayerSettlementPlacement(
                    definition,
                    validationPosition,
                    out failure,
                    false))
            {
                return false;
            }

            if (!playerSettlementPlacement.Begin(definition, out failure))
            {
                return false;
            }

            playerSettlementPanelOpen = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return true;
        }

        public void CancelPlayerSettlementPlacement()
        {
            playerSettlementInputCapturedFrame = Time.frameCount;
            playerSettlementPlacement?.Cancel(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        internal bool ValidatePlayerSettlementPlacement(
            SettlementPieceDefinition definition,
            Vector3 position,
            out string failure)
        {
            return ValidatePlayerSettlementPlacement(
                definition,
                position,
                out failure,
                true);
        }

        internal bool TryCommitPlayerSettlementPlacement(
            SettlementPieceDefinition definition,
            Vector3 position,
            float rotationY,
            out string failure)
        {
            failure = string.Empty;
            if (!ValidatePlayerSettlementPlacement(
                    definition,
                    position,
                    out failure,
                    true))
            {
                return false;
            }

            SpendCost(definition.buildCost);
            var isCore = definition.role == SettlementPieceRole.ThreadheartCore;
            var instanceId = isCore
                ? TetherfallSettlementCatalog.ThreadheartCoreInstanceId
                : CreateNextPlayerSettlementPieceId();
            var record = new SettlementPlacedPieceSaveData
            {
                instanceId = instanceId,
                definitionId = definition.id,
                position = position,
                rotationY = TetherfallSettlementRules.NormalizeRotation(
                    rotationY,
                    definition.rotationPolicy),
                durability = definition.maximumDurability,
                visualSeed = StableSettlementHash(instanceId)
            };
            playerSettlementState.pieces.Add(record);
            if (isCore)
            {
                playerSettlementState.coreClaimed = true;
                playerSettlementState.coreTier = 1;
                playerSettlementState.lastSimulatedUtcTicks = DateTime.UtcNow.Ticks;
            }

            SpawnPlayerSettlementRuntimePiece(record);
            if (isCore)
            {
                SetPlayerSettlementSimulationBand(SettlementSimulationBand.Near);
                RequestCriticalSave();
            }
            else
            {
                RequestSave();
            }

            return true;
        }

        internal bool TryFindPlayerSettlementSnap(
            SettlementPieceDefinition definition,
            Vector3 targetPoint,
            float rotationY,
            out SettlementSnapResult result)
        {
            result = default;
            if (definition == null || !definition.UsesStructuralSockets)
            {
                return false;
            }

            var candidateRotation = Quaternion.Euler(0f, rotationY, 0f);
            var right = candidateRotation * Vector3.right;
            var forward = candidateRotation * Vector3.forward;
            var bestDistanceSquared = 2.8f * 2.8f;
            foreach (var piece in playerSettlementPieces.Values)
            {
                if (piece == null ||
                    piece.Definition == null ||
                    !piece.Definition.UsesStructuralSockets)
                {
                    continue;
                }

                settlementSnapBuffer.Clear();
                piece.CollectSnapPoints(settlementSnapBuffer);
                foreach (var socket in settlementSnapBuffer)
                {
                    if (socket.Direction.y < -0.5f)
                    {
                        continue;
                    }

                    Vector3 candidate;
                    if (socket.Direction.y > 0.5f)
                    {
                        candidate = socket.Position;
                    }
                    else
                    {
                        var horizontalDirection = socket.Direction;
                        horizontalDirection.y = 0f;
                        horizontalDirection.Normalize();
                        var projectedHalf =
                            Mathf.Abs(Vector3.Dot(horizontalDirection, right)) *
                            definition.footprint.x * 0.5f +
                            Mathf.Abs(Vector3.Dot(horizontalDirection, forward)) *
                            definition.footprint.z * 0.5f;
                        candidate =
                            socket.Position +
                            horizontalDirection * (projectedHalf + 0.025f);
                    }

                    var distanceSquared = (targetPoint - candidate).sqrMagnitude;
                    if (distanceSquared < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        result = new SettlementSnapResult(
                            candidate,
                            piece,
                            socket.SupportsWithoutGround);
                    }
                }
            }

            return result.IsValid;
        }

        public Vector3 GetPlayerSettlementCorePosition()
        {
            var core = GetPlayerSettlementCoreRecord();
            return core != null ? core.position : Vector3.zero;
        }

        public bool TryUpgradePlayerSettlementCore(out string failure)
        {
            failure = string.Empty;
            if (!HasClaimedPlayerSettlement)
            {
                failure = "Claim a Threadheart core first.";
                return false;
            }

            if (!IsPlayerSettlementCoreOperational)
            {
                failure = "Repair the Threadheart core before upgrading it.";
                return false;
            }

            var nextTier = playerSettlementState.coreTier + 1;
            var upgrade = TetherfallSettlementCatalog.GetCoreTier(nextTier);
            if (upgrade == null)
            {
                failure = "The Threadheart is already at its bounded maximum tier.";
                return false;
            }

            if (Level < upgrade.minimumWayfinderLevel)
            {
                failure = $"Requires Wayfinder rank {upgrade.minimumWayfinderLevel}.";
                return false;
            }

            if (!CanAfford(upgrade.cost))
            {
                failure = $"Upgrade needs {DescribeMissingCost(upgrade.cost)}.";
                return false;
            }

            SpendCost(upgrade.cost);
            playerSettlementState.coreTier = upgrade.tier;
            RequestCriticalSave();
            Notify(
                $"Threadheart upgraded to {upgrade.displayName}: " +
                $"{upgrade.buildRadius:0} m claim, {upgrade.pieceCap} pieces, " +
                $"{upgrade.workerCap} workers.");
            return true;
        }

        public bool TryRepairPlayerSettlementPiece(
            string pieceInstanceId,
            out string failure)
        {
            failure = string.Empty;
            if (!TryGetPlayerSettlementPiece(
                    pieceInstanceId,
                    out var runtime,
                    out _,
                    out var definition))
            {
                failure = "That placed piece no longer exists.";
                return false;
            }

            if (runtime.Durability >= definition.maximumDurability)
            {
                failure = $"{definition.displayName} does not need repair.";
                return false;
            }

            var repairCost = TetherfallSettlementRules.CalculateRepairCost(
                definition,
                runtime.Durability);
            var maintenanceNeeded = 0;
            foreach (var cost in repairCost)
            {
                maintenanceNeeded += Mathf.Max(0, cost.amount);
            }

            if (maintenanceNeeded > 0 &&
                playerSettlementState.maintenanceUnits >= maintenanceNeeded)
            {
                playerSettlementState.maintenanceUnits -= maintenanceNeeded;
            }
            else
            {
                if (!CanAfford(repairCost))
                {
                    failure = $"Repair needs {DescribeMissingCost(repairCost)}.";
                    return false;
                }

                SpendCost(repairCost);
            }

            runtime.SetDurability(definition.maximumDurability);
            RequestSave();
            Notify($"{definition.displayName} fully repaired.");
            return true;
        }

        public bool DamagePlayerSettlementPiece(string pieceInstanceId, int amount)
        {
            if (amount <= 0 ||
                !TryGetPlayerSettlementPiece(
                    pieceInstanceId,
                    out var runtime,
                    out _,
                    out _))
            {
                return false;
            }

            runtime.SetDurability(runtime.Durability - amount);
            if (runtime.Durability <= 0)
            {
                RemovePlayerSettlementAssignmentsForPiece(pieceInstanceId);
            }

            RequestSave();
            return true;
        }

        public bool TryRestAtPlayerSettlementBed(
            string pieceInstanceId,
            out string failure)
        {
            failure = string.Empty;
            if (!TryGetPlayerSettlementPiece(
                    pieceInstanceId,
                    out var runtime,
                    out _,
                    out var definition) ||
                definition.role != SettlementPieceRole.WayfinderBed ||
                runtime.Durability <= 0)
            {
                failure = "A sound Wayfinder bed is required.";
                return false;
            }

            if ((PlayerTransform.position - runtime.transform.position).sqrMagnitude >
                4.2f * 4.2f)
            {
                failure = "Move closer to the Wayfinder bed.";
                return false;
            }

            var ate = playerSettlementState.foodUnits > 0;
            if (ate)
            {
                playerSettlementState.foodUnits--;
            }

            RestoreAllStamina();
            HealPlayer(Mathf.Max(10, Mathf.CeilToInt(MaxHealth * (ate ? 0.2f : 0.1f))));
            RequestSave();
            Notify(
                ate
                    ? "You rest in shelter, share one settlement meal, and recover."
                    : "You rest in shelter. No settlement meal was stocked.");
            return true;
        }

        public bool TryDemolishPlayerSettlementPiece(
            string pieceInstanceId,
            out string failure)
        {
            failure = string.Empty;
            if (!TryGetPlayerSettlementPiece(
                    pieceInstanceId,
                    out var runtime,
                    out var record,
                    out var definition))
            {
                failure = "That placed piece no longer exists.";
                return false;
            }

            var isCore = definition.role == SettlementPieceRole.ThreadheartCore;
            if (isCore)
            {
                if (playerSettlementState.pieces.Count > 1 ||
                    playerSettlementState.workers.Count > 0 ||
                    GetPlayerSettlementStoredTotal() > 0 ||
                    playerSettlementState.foodUnits > 0 ||
                    playerSettlementState.waterUnits > 0 ||
                    playerSettlementState.maintenanceUnits > 0)
                {
                    failure =
                        "Remove all other pieces, workers, and stored stock before releasing the Threadheart.";
                    return false;
                }
            }
            else if (definition.storageCapacity > 0)
            {
                var capacityAfterRemoval = Mathf.Max(
                    0,
                    GetPlayerSettlementStorageCapacity() - definition.storageCapacity);
                if (GetPlayerSettlementStoredTotal() > capacityAfterRemoval)
                {
                    failure =
                        $"Move resources first; demolition would reduce storage below " +
                        $"{GetPlayerSettlementStoredTotal()} units.";
                    return false;
                }
            }

            var refund = TetherfallSettlementRules.CalculateDemolitionRefund(
                definition.buildCost);
            RemovePlayerSettlementAssignmentsForPiece(pieceInstanceId);
            playerSettlementState.stationJobs.RemoveAll(job =>
                job != null &&
                string.Equals(
                    job.stationPieceId,
                    pieceInstanceId,
                    StringComparison.Ordinal));
            playerSettlementState.pieces.Remove(record);
            playerSettlementPieces.Remove(pieceInstanceId);
            if (runtime != null)
            {
                Destroy(runtime.gameObject);
            }

            ApplyExactSettlementRefundToPlayerBag(refund);
            if (isCore)
            {
                playerSettlementState.coreClaimed = false;
                playerSettlementState.coreTier = 1;
                playerSettlementState.lastSimulatedUtcTicks = DateTime.UtcNow.Ticks;
                DeactivateAllPlayerSettlementWorkerProxies();
            }

            if (selectedPlayerSettlementPieceId == pieceInstanceId)
            {
                selectedPlayerSettlementPieceId = string.Empty;
            }

            RequestCriticalSave();
            Notify(
                $"{definition.displayName} demolished. " +
                $"The Wayfinder bag received the rounded-down 60% refund.");
            return true;
        }

        public int GetPlayerSettlementStorage(ResourceKind kind)
        {
            return playerSettlementStorage.TryGetValue(kind, out var amount)
                ? Mathf.Max(0, amount)
                : 0;
        }

        public int GetPlayerSettlementStorageCapacity()
        {
            return TetherfallSettlementPersistence.GetStorageCapacity(
                playerSettlementState);
        }

        public int GetPlayerSettlementStoredTotal()
        {
            long total = 0L;
            foreach (var amount in playerSettlementStorage.Values)
            {
                total += Mathf.Max(0, amount);
            }

            return (int)Math.Min(int.MaxValue, total);
        }

        public bool TryDepositPlayerSettlementResource(
            ResourceKind kind,
            int amount,
            out string failure)
        {
            failure = string.Empty;
            if (!HasClaimedPlayerSettlement)
            {
                failure = "Claim a Threadheart before storing resources.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ResourceKind), kind) || amount <= 0)
            {
                failure = "Deposit amount must be positive.";
                return false;
            }

            if ((long)GetPlayerSettlementStoredTotal() + amount >
                GetPlayerSettlementStorageCapacity())
            {
                failure = "Settlement storage is full.";
                return false;
            }

            var cost = new List<ResourceCost> { new ResourceCost(kind, amount) };
            if (!CanAfford(cost))
            {
                failure = $"The Wayfinder bag lacks {amount} {ResourceLabel(kind)}.";
                return false;
            }

            SpendCost(cost);
            playerSettlementStorage[kind] =
                GetPlayerSettlementStorage(kind) + amount;
            RequestSave();
            return true;
        }

        public bool TryWithdrawPlayerSettlementResource(
            ResourceKind kind,
            int amount,
            out string failure)
        {
            failure = string.Empty;
            if (!Enum.IsDefined(typeof(ResourceKind), kind) || amount <= 0)
            {
                failure = "Withdrawal amount must be positive.";
                return false;
            }

            var stored = GetPlayerSettlementStorage(kind);
            if (stored < amount)
            {
                failure = $"Settlement storage lacks {amount} {ResourceLabel(kind)}.";
                return false;
            }

            playerSettlementStorage[kind] = stored - amount;
            resources.TryGetValue(kind, out var bagAmount);
            resources[kind] = (int)Math.Min(int.MaxValue, (long)bagAmount + amount);
            RequestSave();
            return true;
        }

        public bool TryAssignPlayerSettlementWorker(
            string pactInstanceId,
            SettlementWorkRole workRole,
            string workPieceId,
            string bedPieceId,
            out string failure)
        {
            if (!TryValidatePlayerSettlementWorkerAssignment(
                    pactInstanceId,
                    workRole,
                    workPieceId,
                    bedPieceId,
                    out var stablePactId,
                    out var stableWorkPieceId,
                    out var stableBedPieceId,
                    out var pactRecord,
                    out var matchedWork,
                    out failure))
            {
                return false;
            }

            var preferenceSnapshot =
                TetherfallSettlementPactBridge.CapturePreference(
                    this,
                    pactRecord);
            if (!TryAssignPactSettlementPreference(
                    stablePactId,
                    TetherfallSettlementCatalog.PlayerSettlementId,
                    stableWorkPieceId,
                    stableBedPieceId,
                    matchedWork,
                    out failure))
            {
                return false;
            }

            if (!TryCommitPlayerSettlementWorkerAssignment(
                    pactRecord,
                    workRole,
                    stableWorkPieceId,
                    stableBedPieceId,
                    out failure))
            {
                TetherfallSettlementPactBridge.RollbackPactPreference(
                    this,
                    pactRecord,
                    preferenceSnapshot);
                return false;
            }

            RequestSave();
            SynchronizePlayerSettlementPactWorkerProxies();
            return true;
        }

        public bool TrySetPlayerSettlementWorkerBed(
            string pactInstanceId,
            string bedPieceId,
            out string failure)
        {
            failure = string.Empty;
            var worker = FindPlayerSettlementWorker(pactInstanceId);
            if (worker == null)
            {
                failure = "That Pact instance has no settlement assignment.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(bedPieceId))
            {
                failure = "Settlement workers must choose a placed Pact bed.";
                return false;
            }

            return TryAssignPlayerSettlementWorker(
                worker.pactInstanceId,
                worker.workRole,
                worker.workPieceId,
                bedPieceId,
                out failure);
        }

        public bool UnassignPlayerSettlementWorker(string pactInstanceId)
        {
            var worker = FindPlayerSettlementWorker(pactInstanceId);
            if (worker == null)
            {
                return false;
            }

            ClearPactSettlementPreference(worker.pactInstanceId);
            playerSettlementState.workers.Remove(worker);
            playerSettlementPactVitalsInitialized.Remove(worker.pactInstanceId);
            DestroyPlayerSettlementPactWorkerProxy(worker.pactInstanceId);
            if (settlementWorkerProxies.TryGetValue(
                    worker.pactInstanceId,
                    out var proxy))
            {
                SetSettlementWorkerProxySafe(
                    proxy,
                    false,
                    Vector3.zero,
                    Vector3.forward,
                    SettlementWorkRole.None,
                    0f);
            }

            RequestSave();
            SynchronizePlayerSettlementPactWorkerProxies();
            return true;
        }

        private bool TryValidatePlayerSettlementWorkerAssignment(
            string pactInstanceId,
            SettlementWorkRole workRole,
            string workPieceId,
            string bedPieceId,
            out string stablePactId,
            out string stableWorkPieceId,
            out string stableBedPieceId,
            out CapturedPactSaveData pactRecord,
            out PactWorkKind matchedWork,
            out string failure)
        {
            stablePactId = NormalizeSettlementExternalId(pactInstanceId);
            stableWorkPieceId = NormalizeSettlementExternalId(workPieceId);
            stableBedPieceId = NormalizeSettlementExternalId(bedPieceId);
            pactRecord = null;
            matchedWork = default;
            failure = string.Empty;
            if (!HasClaimedPlayerSettlement)
            {
                failure = "Claim a Threadheart before assigning Pact workers.";
                return false;
            }

            if (string.IsNullOrEmpty(stablePactId))
            {
                failure = "Choose an Accord from the Pact roster.";
                return false;
            }

            pactRecord = TetherfallSettlementPactBridge.FindPactRecord(
                PactRoster,
                stablePactId);
            var species = pactRecord != null
                ? TetherfallAccordSpeciesCatalog.Find(pactRecord.speciesId)
                : null;
            if (pactRecord == null || species == null)
            {
                failure = "That Accord is not available in the Pact roster.";
                return false;
            }

            if (pactRecord.IsSettlementAssigned &&
                !string.Equals(
                    pactRecord.settlementId,
                    TetherfallSettlementCatalog.PlayerSettlementId,
                    StringComparison.Ordinal))
            {
                failure = "That Accord already chose another settlement.";
                return false;
            }

            if (!TryGetPlayerSettlementPiece(
                    stableWorkPieceId,
                    out var workRuntime,
                    out _,
                    out var workDefinition) ||
                workRuntime.Durability <= 0 ||
                workDefinition.workSlots <= 0 ||
                workDefinition.workRole == SettlementWorkRole.None)
            {
                failure = "Choose an operational piece with a free work socket.";
                return false;
            }

            if (workRole != workDefinition.workRole)
            {
                failure =
                    $"{workDefinition.displayName} requires the " +
                    $"{workDefinition.workRole} role.";
                return false;
            }

            if (!TetherfallSettlementPactBridge.TryMatchWorkPreference(
                    species,
                    workRole,
                    out matchedWork,
                    out _))
            {
                failure =
                    $"{species.displayName} has no authored affinity for " +
                    $"{workRole} work.";
                return false;
            }

            var existing = FindPlayerSettlementWorker(stablePactId);
            if (existing == null &&
                playerSettlementState.workers.Count >=
                TetherfallSettlementRules.GetWorkerCap(
                    playerSettlementState.coreTier))
            {
                failure = "The Threadheart worker cap has been reached.";
                return false;
            }

            if (CountPlayerSettlementAssignments(
                    stableWorkPieceId,
                    false,
                    stablePactId) >= workDefinition.workSlots)
            {
                failure = $"{workDefinition.displayName} has no free work socket.";
                return false;
            }

            if (string.IsNullOrEmpty(stableBedPieceId) ||
                !TryGetPlayerSettlementPiece(
                    stableBedPieceId,
                    out var bedRuntime,
                    out _,
                    out var bedDefinition) ||
                bedRuntime.Durability <= 0 ||
                bedDefinition.role != SettlementPieceRole.PactBed)
            {
                failure = "Choose an operational, free Pact bed.";
                return false;
            }

            if (CountPlayerSettlementAssignments(
                    stableBedPieceId,
                    true,
                    stablePactId) >= bedDefinition.bedSlots)
            {
                failure = "That Pact bed is already assigned.";
                return false;
            }

            return true;
        }

        private bool TryCommitPlayerSettlementWorkerAssignment(
            CapturedPactSaveData pactRecord,
            SettlementWorkRole workRole,
            string workPieceId,
            string bedPieceId,
            out string failure)
        {
            failure = string.Empty;
            if (pactRecord == null ||
                !TryGetPlayerSettlementPiece(
                    workPieceId,
                    out var workRuntime,
                    out _,
                    out var workDefinition) ||
                workRuntime.Durability <= 0 ||
                workDefinition.workRole != workRole ||
                CountPlayerSettlementAssignments(
                    workPieceId,
                    false,
                    pactRecord.instanceId) >= workDefinition.workSlots ||
                !TryGetPlayerSettlementPiece(
                    bedPieceId,
                    out var bedRuntime,
                    out _,
                    out var bedDefinition) ||
                bedRuntime.Durability <= 0 ||
                bedDefinition.role != SettlementPieceRole.PactBed ||
                CountPlayerSettlementAssignments(
                    bedPieceId,
                    true,
                    pactRecord.instanceId) >= bedDefinition.bedSlots)
            {
                failure =
                    "The work station or Pact bed changed before the assignment completed.";
                return false;
            }

            var worker = FindPlayerSettlementWorker(pactRecord.instanceId);
            var isNewAssignment = worker == null;
            if (isNewAssignment)
            {
                if (playerSettlementState.workers.Count >=
                    TetherfallSettlementRules.GetWorkerCap(
                        playerSettlementState.coreTier))
                {
                    failure = "The Threadheart worker cap changed before assignment.";
                    return false;
                }

                worker = new SettlementWorkerAssignmentSaveData
                {
                    pactInstanceId = pactRecord.instanceId,
                    lastMealSatisfied = true
                };
            }

            var workChanged =
                isNewAssignment ||
                worker.workRole != workRole ||
                !string.Equals(
                    worker.workPieceId,
                    workPieceId,
                    StringComparison.Ordinal);
            worker.workRole = workRole;
            worker.workPieceId = workPieceId;
            worker.bedPieceId = bedPieceId;
            if (workChanged)
            {
                worker.productionProgressSeconds = 0d;
            }

            TetherfallSettlementPactBridge.InitializeWorkerVitalsFromPact(
                worker,
                pactRecord);
            if (isNewAssignment)
            {
                playerSettlementState.workers.Add(worker);
            }

            playerSettlementPactVitalsInitialized.Add(worker.pactInstanceId);
            return true;
        }

        public IReadOnlyList<SettlementWorkerAssignmentSaveData>
            GetPlayerSettlementWorkerAssignments()
        {
            if (playerSettlementState?.workers != null)
            {
                return playerSettlementState.workers;
            }

            return Array.Empty<SettlementWorkerAssignmentSaveData>();
        }

        public bool RegisterPlayerSettlementWorkerProxy(
            ITetherfallSettlementWorkerProxy proxy)
        {
            var id = NormalizeSettlementExternalId(proxy?.PactInstanceId);
            if (proxy == null ||
                string.IsNullOrEmpty(id) ||
                FindPlayerSettlementWorker(id) == null)
            {
                return false;
            }

            if (settlementWorkerProxies.TryGetValue(id, out var existing) &&
                !ReferenceEquals(existing, proxy))
            {
                return false;
            }

            if (!settlementWorkerProxies.ContainsKey(id) &&
                settlementWorkerProxies.Count >=
                TetherfallSettlementRules.HardWorkerCap)
            {
                return false;
            }

            settlementWorkerProxies[id] = proxy;
            RefreshPlayerSettlementWorkerProxies();
            return true;
        }

        public void UnregisterPlayerSettlementWorkerProxy(string pactInstanceId)
        {
            var id = NormalizeSettlementExternalId(pactInstanceId);
            if (!settlementWorkerProxies.TryGetValue(id, out var proxy))
            {
                return;
            }

            SetSettlementWorkerProxySafe(
                proxy,
                false,
                Vector3.zero,
                Vector3.forward,
                SettlementWorkRole.None,
                0f);
            settlementWorkerProxies.Remove(id);
        }

        public bool TryQueuePlayerSettlementRecipe(
            string stationPieceId,
            string recipeId,
            int cycles,
            out string failure)
        {
            failure = string.Empty;
            if (cycles <= 0)
            {
                failure = "Queue at least one production cycle.";
                return false;
            }

            if (!TryGetPlayerSettlementPiece(
                    stationPieceId,
                    out _,
                    out _,
                    out var stationDefinition) ||
                !stationDefinition.IsStation)
            {
                failure = "That placed piece is not a production station.";
                return false;
            }

            var recipe = TetherfallSettlementCatalog.FindRecipe(recipeId);
            if (recipe == null ||
                !string.Equals(
                    recipe.stationDefinitionId,
                    stationDefinition.id,
                    StringComparison.Ordinal))
            {
                failure = "That recipe does not belong to this station.";
                return false;
            }

            if (playerSettlementState.coreTier < recipe.minimumCoreTier)
            {
                failure =
                    $"{recipe.displayName} requires Threadheart tier " +
                    $"{recipe.minimumCoreTier}.";
                return false;
            }

            if (Level < recipe.minimumWayfinderLevel)
            {
                failure =
                    $"{recipe.displayName} requires Wayfinder rank " +
                    $"{recipe.minimumWayfinderLevel}.";
                return false;
            }

            var existing = playerSettlementState.stationJobs.Find(job =>
                job != null &&
                string.Equals(
                    job.stationPieceId,
                    stationPieceId,
                    StringComparison.Ordinal));
            if (existing != null &&
                !string.Equals(existing.recipeId, recipe.id, StringComparison.Ordinal))
            {
                failure = "Finish or cancel the station's current recipe first.";
                return false;
            }

            if (existing == null)
            {
                existing = new SettlementStationJobSaveData
                {
                    stationPieceId = stationPieceId,
                    recipeId = recipe.id
                };
                playerSettlementState.stationJobs.Add(existing);
            }

            existing.queuedCycles = (int)Math.Min(
                TetherfallSettlementRules.HardQueuedCyclesPerStation,
                Math.Max(1L, (long)existing.queuedCycles + cycles));
            RequestSave();
            return true;
        }

        public bool CancelPlayerSettlementRecipe(string stationPieceId)
        {
            var removed = playerSettlementState.stationJobs.RemoveAll(job =>
                job != null &&
                string.Equals(
                    job.stationPieceId,
                    stationPieceId,
                    StringComparison.Ordinal));
            if (removed > 0)
            {
                RequestSave();
                return true;
            }

            return false;
        }

        public float GetPlayerSettlementWorkerEfficiency(string pactInstanceId)
        {
            var worker = FindPlayerSettlementWorker(pactInstanceId);
            if (worker == null)
            {
                return 0f;
            }

            return CalculatePlayerSettlementWorkerEfficiency(
                worker,
                HasValidPlayerSettlementBed(worker),
                HasOperationalPlayerSettlementTrough());
        }

        public void DrawPlayerSettlementPanel(Rect rect)
        {
            EnsureGuiStyles();
            if (IsPlayerSettlementPlacementActive)
            {
                DrawPlayerSettlementPlacementStatus(rect);
                return;
            }

            if (!playerSettlementPanelOpen)
            {
                return;
            }

            GUI.DrawTexture(rect, slateTexture);
            GUI.DrawTexture(
                new Rect(rect.x, rect.y, rect.width, 3f),
                copperTexture);
            GUI.Label(
                new Rect(rect.x + 24f, rect.y + 18f, rect.width - 48f, 30f),
                "PLAYER SETTLEMENT",
                headingStyle);

            var tabs = new[] { "Build", "Storage", "Workers", "Production" };
            for (var index = 0; index < tabs.Length; index++)
            {
                if (GUI.Button(
                        new Rect(rect.x + 24f + index * 126f, rect.y + 52f, 116f, 30f),
                        playerSettlementPanelPage == index
                            ? $"• {tabs[index]}"
                            : tabs[index]))
                {
                    playerSettlementPanelPage = index;
                }
            }

            var content = new Rect(
                rect.x + 24f,
                rect.y + 94f,
                rect.width - 48f,
                rect.height - 132f);
            switch (playerSettlementPanelPage)
            {
                case 0:
                    DrawPlayerSettlementBuildPage(content);
                    break;
                case 1:
                    DrawPlayerSettlementStoragePage(content);
                    break;
                case 2:
                    DrawPlayerSettlementWorkersPage(content);
                    break;
                default:
                    DrawPlayerSettlementProductionPage(content);
                    break;
            }

            GUI.Label(
                new Rect(rect.x + 24f, rect.yMax - 28f, rect.width - 48f, 20f),
                $"{TetherfallInputBindings.Shared.GetBindingSummary(TetherfallInputAction.OpenSettlementPlanning)} or Esc closes · placed pieces open this panel with E",
                mutedStyle);
        }

        private bool ValidatePlayerSettlementPlacement(
            SettlementPieceDefinition definition,
            Vector3 position,
            out string failure,
            bool validatePosition)
        {
            failure = string.Empty;
            if (definition == null ||
                TetherfallSettlementCatalog.FindPiece(definition.id) == null)
            {
                failure = "Settlement definition is not in the stable catalog.";
                return false;
            }

            if (validatePosition &&
                !TetherfallSettlementRules.IsBoundedWorldPosition(position))
            {
                failure = "Placement position is outside the finite world bound.";
                return false;
            }

            if (Level < definition.minimumWayfinderLevel)
            {
                failure =
                    $"{definition.displayName} requires Wayfinder rank " +
                    $"{definition.minimumWayfinderLevel}.";
                return false;
            }

            var isCore = definition.role == SettlementPieceRole.ThreadheartCore;
            if (isCore)
            {
                if (HasClaimedPlayerSettlement ||
                    playerSettlementState.pieces.Exists(piece =>
                        piece != null &&
                        piece.definitionId ==
                        TetherfallSettlementCatalog.ThreadheartCoreDefinitionId))
                {
                    failure = "Only one Threadheart core may be claimed.";
                    return false;
                }

                if (validatePosition)
                {
                    var playerOffset = position - PlayerTransform.position;
                    playerOffset.y = 0f;
                    if (playerOffset.sqrMagnitude >
                        MaximumCoreClaimDistanceFromPlayer *
                        MaximumCoreClaimDistanceFromPlayer)
                    {
                        failure =
                            $"The first Threadheart must be within " +
                            $"{MaximumCoreClaimDistanceFromPlayer:0} m of the Wayfinder.";
                        return false;
                    }

                    if (!TetherfallSettlementForbiddenZones.CanClaimCoreAt(
                            position,
                            out failure))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!HasClaimedPlayerSettlement)
                {
                    failure = "Claim the Threadheart core before placing other pieces.";
                    return false;
                }

                if (!IsPlayerSettlementCoreOperational)
                {
                    failure = "Repair the Threadheart core before extending the settlement.";
                    return false;
                }

                if (playerSettlementState.coreTier < definition.minimumCoreTier)
                {
                    failure =
                        $"{definition.displayName} requires Threadheart tier " +
                        $"{definition.minimumCoreTier}.";
                    return false;
                }

                if (playerSettlementState.pieces.Count >=
                    TetherfallSettlementRules.GetPieceCap(
                        playerSettlementState.coreTier))
                {
                    failure = "The current Threadheart piece cap has been reached.";
                    return false;
                }

                if (validatePosition)
                {
                    var offset = position - GetPlayerSettlementCorePosition();
                    var horizontal = new Vector2(offset.x, offset.z);
                    var radius = TetherfallSettlementRules.GetBuildRadius(
                        playerSettlementState.coreTier);
                    var footprintRadius =
                        Mathf.Max(definition.footprint.x, definition.footprint.z) *
                        0.5f;
                    if (horizontal.magnitude + footprintRadius > radius)
                    {
                        failure =
                            $"Outside the {radius:0} m Threadheart claim radius.";
                        return false;
                    }

                    if (Mathf.Abs(offset.y) > MaximumSettlementVerticalExtent)
                    {
                        failure =
                            $"Placement exceeds the ±{MaximumSettlementVerticalExtent:0} m " +
                            "vertical settlement bound.";
                        return false;
                    }
                }
            }

            if (!CanAfford(definition.buildCost))
            {
                failure =
                    $"{definition.displayName} needs " +
                    $"{DescribeMissingCost(definition.buildCost)}.";
                return false;
            }

            return true;
        }

        private SettlementPlacedPieceSaveData GetPlayerSettlementCoreRecord()
        {
            return TetherfallSettlementPersistence.FindCore(
                playerSettlementState);
        }

        private string CreateNextPlayerSettlementPieceId()
        {
            string id;
            do
            {
                id =
                    $"settlement.piece.{Mathf.Max(1, playerSettlementState.nextPieceSequence):000000}";
                playerSettlementState.nextPieceSequence =
                    playerSettlementState.nextPieceSequence == int.MaxValue
                        ? 1
                        : playerSettlementState.nextPieceSequence + 1;
            }
            while (playerSettlementPieces.ContainsKey(id));

            return id;
        }

        private static int StableSettlementHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                if (value != null)
                {
                    for (var index = 0; index < value.Length; index++)
                    {
                        hash ^= value[index];
                        hash *= 16777619;
                    }
                }

                return (int)(hash & 0x7fffffff);
            }
        }

        private void SpawnPlayerSettlementRuntimePiece(
            SettlementPlacedPieceSaveData record)
        {
            if (record == null ||
                string.IsNullOrEmpty(record.instanceId) ||
                playerSettlementPieces.ContainsKey(record.instanceId))
            {
                return;
            }

            var definition = TetherfallSettlementCatalog.FindPiece(
                record.definitionId);
            if (definition == null)
            {
                return;
            }

            var runtime = TetherfallSettlementPiece.Create(
                this,
                playerSettlementRuntimeRoot,
                record,
                definition);
            if (runtime == null)
            {
                return;
            }

            runtime.SetSimulationBand(playerSettlementSimulationBand);
            playerSettlementPieces[record.instanceId] = runtime;
        }

        private void ClearPlayerSettlementRuntimePieces()
        {
            foreach (var piece in playerSettlementPieces.Values)
            {
                if (piece != null)
                {
                    Destroy(piece.gameObject);
                }
            }

            playerSettlementPieces.Clear();
            DestroyAllPlayerSettlementPactWorkerProxies();
            DeactivateAllPlayerSettlementWorkerProxies();
        }

        private bool TryGetPlayerSettlementPiece(
            string instanceId,
            out TetherfallSettlementPiece runtime,
            out SettlementPlacedPieceSaveData record,
            out SettlementPieceDefinition definition)
        {
            runtime = null;
            record = null;
            definition = null;
            if (string.IsNullOrEmpty(instanceId) ||
                !playerSettlementPieces.TryGetValue(instanceId, out runtime) ||
                runtime == null)
            {
                return false;
            }

            for (var index = 0;
                 index < playerSettlementState.pieces.Count;
                 index++)
            {
                var candidate = playerSettlementState.pieces[index];
                if (candidate != null &&
                    string.Equals(
                        candidate.instanceId,
                        instanceId,
                        StringComparison.Ordinal))
                {
                    record = candidate;
                    break;
                }
            }

            definition = runtime.Definition;
            return record != null && definition != null;
        }

        private void SetPlayerSettlementSimulationBand(
            SettlementSimulationBand band)
        {
            playerSettlementSimulationBand = band;
            foreach (var piece in playerSettlementPieces.Values)
            {
                piece?.SetSimulationBand(band);
            }

            if (band != SettlementSimulationBand.Near)
            {
                DestroyAllPlayerSettlementPactWorkerProxies();
                DeactivateAllPlayerSettlementWorkerProxies();
            }
        }

        private SettlementSimulationSummary AdvancePlayerSettlementSimulationToUtc(
            long nowUtcTicks,
            bool offline)
        {
            var summary = new SettlementSimulationSummary();
            if (playerSettlementState == null)
            {
                return summary;
            }

            if (playerSettlementState.lastSimulatedUtcTicks <= 0L ||
                nowUtcTicks <= playerSettlementState.lastSimulatedUtcTicks)
            {
                playerSettlementState.lastSimulatedUtcTicks = Math.Max(
                    0L,
                    nowUtcTicks);
                return summary;
            }

            var elapsed = TetherfallSettlementRules.ClampOfflineSeconds(
                playerSettlementState.lastSimulatedUtcTicks,
                nowUtcTicks);
            playerSettlementState.lastSimulatedUtcTicks = nowUtcTicks;
            return SimulatePlayerSettlement(elapsed, offline);
        }

        private SettlementSimulationSummary SimulatePlayerSettlement(
            double elapsedSeconds,
            bool offline)
        {
            var summary = new SettlementSimulationSummary
            {
                simulatedSeconds = Math.Max(
                    0d,
                    Math.Min(
                        TetherfallSettlementRules.MaximumOfflineSeconds,
                        elapsedSeconds))
            };
            if (!HasClaimedPlayerSettlement ||
                !IsPlayerSettlementCoreOperational ||
                summary.simulatedSeconds <= 0d ||
                playerSettlementState.workers.Count == 0)
            {
                return summary;
            }

            var cycleBudget = TetherfallSettlementRules.MaximumCyclesPerAdvance;
            var hasTrough = HasOperationalPlayerSettlementTrough();
            var hasWatch = HasOperationalPlayerSettlementWatchPost();
            var completedJobs = new List<SettlementStationJobSaveData>();
            foreach (var worker in playerSettlementState.workers)
            {
                if (worker == null)
                {
                    continue;
                }

                if (cycleBudget <= 0)
                {
                    summary.cycleLimitReached = true;
                }

                var hasBed = HasValidPlayerSettlementBed(worker);
                UpdatePlayerSettlementWorkerNeeds(
                    worker,
                    summary.simulatedSeconds,
                    hasTrough,
                    hasBed,
                    hasWatch,
                    summary);
                if (!TryGetPlayerSettlementPiece(
                        worker.workPieceId,
                        out var runtime,
                        out _,
                        out var definition) ||
                    runtime.Durability <= 0)
                {
                    continue;
                }

                var efficiency = CalculatePlayerSettlementWorkerEfficiency(
                    worker,
                    hasBed,
                    hasTrough);
                var durabilityEfficiency = Mathf.Clamp(
                    runtime.Durability / (float)Mathf.Max(1, runtime.MaximumDurability),
                    0.25f,
                    1f);
                efficiency *= durabilityEfficiency;

                var completedForWorker = 0;
                if (definition.IsStation)
                {
                    var job = playerSettlementState.stationJobs.Find(candidate =>
                        candidate != null &&
                        string.Equals(
                            candidate.stationPieceId,
                            worker.workPieceId,
                            StringComparison.Ordinal));
                    var recipe = job != null
                        ? TetherfallSettlementCatalog.FindRecipe(job.recipeId)
                        : null;
                    if (job != null && recipe != null && job.queuedCycles > 0)
                    {
                        job.progressSeconds += summary.simulatedSeconds * efficiency;
                        var possibleCycles = Math.Min(
                            TetherfallSettlementRules.MaximumCyclesPerWorkerAdvance,
                            TetherfallSettlementRules.GetBoundedCycleCount(
                                job.progressSeconds,
                                recipe.cycleSeconds));
                        possibleCycles = Math.Min(
                            possibleCycles,
                            Math.Min(job.queuedCycles, cycleBudget));
                        for (var cycle = 0; cycle < possibleCycles; cycle++)
                        {
                            if (!TryCompletePlayerSettlementRecipe(
                                    recipe,
                                    worker,
                                    summary))
                            {
                                break;
                            }

                            job.progressSeconds -= recipe.cycleSeconds;
                            job.queuedCycles--;
                            cycleBudget--;
                            completedForWorker++;
                        }

                        job.progressSeconds = Math.Min(
                            job.progressSeconds,
                            recipe.cycleSeconds);
                        if (job.queuedCycles <= 0)
                        {
                            completedJobs.Add(job);
                        }
                    }
                }
                else
                {
                    var cycleSeconds = definition.automaticCycleSeconds;
                    if (definition.role == SettlementPieceRole.FeedingTrough)
                    {
                        cycleSeconds = 180f;
                    }
                    else if (definition.role == SettlementPieceRole.WatchPost)
                    {
                        cycleSeconds = 300f;
                    }

                    if (cycleSeconds > 0f)
                    {
                        worker.productionProgressSeconds +=
                            summary.simulatedSeconds * efficiency;
                        var possibleCycles = Math.Min(
                            TetherfallSettlementRules.MaximumCyclesPerWorkerAdvance,
                            TetherfallSettlementRules.GetBoundedCycleCount(
                                worker.productionProgressSeconds,
                                cycleSeconds));
                        possibleCycles = Math.Min(possibleCycles, cycleBudget);
                        for (var cycle = 0; cycle < possibleCycles; cycle++)
                        {
                            if (!TryCompletePlayerSettlementAutomaticCycle(
                                    definition,
                                    worker,
                                    summary))
                            {
                                break;
                            }

                            worker.productionProgressSeconds -= cycleSeconds;
                            cycleBudget--;
                            completedForWorker++;
                        }

                        worker.productionProgressSeconds = Math.Min(
                            worker.productionProgressSeconds,
                            cycleSeconds);
                    }
                }

                worker.stamina = Mathf.Clamp(
                    worker.stamina - completedForWorker * 0.65f,
                    0f,
                    100f);
                summary.completedProductionCycles += completedForWorker;
            }

            foreach (var completed in completedJobs)
            {
                playerSettlementState.stationJobs.Remove(completed);
            }

            summary.cycleLimitReached =
                summary.cycleLimitReached || cycleBudget <= 0;
            SynchronizeAllPlayerSettlementWorkerVitalsToPact();
            return summary;
        }

        private void UpdatePlayerSettlementWorkerNeeds(
            SettlementWorkerAssignmentSaveData worker,
            double elapsedSeconds,
            bool hasTrough,
            bool hasBed,
            bool hasWatch,
            SettlementSimulationSummary summary)
        {
            var needInterval = hasTrough
                ? FedTroughNeedIntervalSeconds
                : WorkerNeedIntervalSeconds;
            worker.needProgressSeconds += elapsedSeconds;
            var needCycles = Mathf.Clamp(
                (int)Math.Floor(worker.needProgressSeconds / needInterval),
                0,
                64);
            for (var cycle = 0; cycle < needCycles; cycle++)
            {
                if (playerSettlementState.foodUnits > 0)
                {
                    playerSettlementState.foodUnits--;
                    if (playerSettlementState.waterUnits > 0)
                    {
                        playerSettlementState.waterUnits--;
                    }

                    worker.lastMealSatisfied = true;
                    summary.mealsConsumed++;
                }
                else
                {
                    worker.lastMealSatisfied = false;
                }

                worker.needProgressSeconds -= needInterval;
            }

            worker.needProgressSeconds = Math.Max(
                0d,
                Math.Min(needInterval, worker.needProgressSeconds));
            var moraleTarget =
                42f +
                (hasBed ? 22f : 0f) +
                (worker.lastMealSatisfied ? 20f : -12f) +
                (hasTrough ? 8f : 0f) +
                (hasWatch ? 5f : 0f);
            var moraleChange = (float)(elapsedSeconds / 3600d) * 10f;
            worker.morale = Mathf.MoveTowards(
                worker.morale,
                Mathf.Clamp(moraleTarget, 10f, 100f),
                moraleChange);
            var staminaRecoveryPerHour = hasBed ? 18f : 5f;
            if (!worker.lastMealSatisfied)
            {
                staminaRecoveryPerHour *= 0.35f;
            }

            worker.stamina = Mathf.Clamp(
                worker.stamina +
                (float)(elapsedSeconds / 3600d) * staminaRecoveryPerHour,
                0f,
                100f);
        }

        private float CalculatePlayerSettlementWorkerEfficiency(
            SettlementWorkerAssignmentSaveData worker,
            bool hasBed,
            bool hasTrough)
        {
            if (worker == null)
            {
                return 0f;
            }

            var efficiency =
                0.28f +
                Mathf.Clamp01(worker.morale / 100f) * 0.34f +
                Mathf.Clamp01(worker.stamina / 100f) * 0.3f;
            if (hasBed)
            {
                efficiency *= 1.08f;
            }

            if (worker.lastMealSatisfied)
            {
                efficiency *= 1.1f;
            }
            else
            {
                efficiency *= 0.68f;
            }

            if (hasTrough)
            {
                efficiency *= 1.04f;
            }

            return Mathf.Clamp(efficiency, 0.2f, 1.2f);
        }

        private bool TryCompletePlayerSettlementRecipe(
            SettlementRecipeDefinition recipe,
            SettlementWorkerAssignmentSaveData worker,
            SettlementSimulationSummary summary)
        {
            if (recipe == null ||
                !CanConsumePlayerSettlementRecipe(recipe) ||
                !CanAcceptPlayerSettlementOutputs(
                    recipe.outputResources,
                    recipe.outputFoodUnits,
                    recipe.outputWaterUnits,
                    recipe.outputMaintenanceUnits,
                    recipe.inputResources))
            {
                return false;
            }

            ConsumePlayerSettlementStorage(recipe.inputResources);
            playerSettlementState.foodUnits -= recipe.inputFoodUnits;
            playerSettlementState.waterUnits -= recipe.inputWaterUnits;
            AddPlayerSettlementOutputs(
                recipe.outputResources,
                recipe.outputFoodUnits,
                recipe.outputWaterUnits,
                recipe.outputMaintenanceUnits,
                summary);
            worker.morale = Mathf.Clamp(
                worker.morale + recipe.workerMoraleReward,
                0f,
                100f);
            return true;
        }

        private bool TryCompletePlayerSettlementAutomaticCycle(
            SettlementPieceDefinition definition,
            SettlementWorkerAssignmentSaveData worker,
            SettlementSimulationSummary summary)
        {
            if (definition.role == SettlementPieceRole.FeedingTrough)
            {
                if (playerSettlementState.foodUnits <= 0 ||
                    playerSettlementState.waterUnits <= 0)
                {
                    return false;
                }

                playerSettlementState.foodUnits--;
                playerSettlementState.waterUnits--;
                worker.lastMealSatisfied = true;
                worker.morale = Mathf.Clamp(worker.morale + 3f, 0f, 100f);
                worker.stamina = Mathf.Clamp(worker.stamina + 2f, 0f, 100f);
                return true;
            }

            if (definition.role == SettlementPieceRole.WatchPost)
            {
                worker.morale = Mathf.Clamp(worker.morale + 1f, 0f, 100f);
                return true;
            }

            if (!CanAcceptPlayerSettlementOutputs(
                    definition.automaticOutputs,
                    definition.automaticFoodOutput,
                    definition.automaticWaterOutput,
                    0,
                    null))
            {
                return false;
            }

            AddPlayerSettlementOutputs(
                definition.automaticOutputs,
                definition.automaticFoodOutput,
                definition.automaticWaterOutput,
                0,
                summary);
            return true;
        }

        private bool CanConsumePlayerSettlementRecipe(
            SettlementRecipeDefinition recipe)
        {
            if (playerSettlementState.foodUnits < recipe.inputFoodUnits ||
                playerSettlementState.waterUnits < recipe.inputWaterUnits)
            {
                return false;
            }

            var totals = TetherfallSettlementRules.AggregateCosts(
                recipe.inputResources);
            foreach (var pair in totals)
            {
                if (GetPlayerSettlementStorage(pair.Key) < pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanAcceptPlayerSettlementOutputs(
            IEnumerable<ResourceCost> resourceOutputs,
            int foodOutput,
            int waterOutput,
            int maintenanceOutput,
            IEnumerable<ResourceCost> resourceInputs)
        {
            var outputUnits = 0;
            var outputTotals = TetherfallSettlementRules.AggregateCosts(
                resourceOutputs);
            foreach (var pair in outputTotals)
            {
                outputUnits += pair.Value;
            }

            var inputUnits = 0;
            var inputTotals = TetherfallSettlementRules.AggregateCosts(
                resourceInputs);
            foreach (var pair in inputTotals)
            {
                inputUnits += pair.Value;
            }

            if (GetPlayerSettlementStoredTotal() - inputUnits + outputUnits >
                GetPlayerSettlementStorageCapacity())
            {
                return false;
            }

            var specialCap = Mathf.Min(
                TetherfallSettlementRules.HardSpecialStockCap,
                Mathf.Max(100, GetPlayerSettlementStorageCapacity()));
            return playerSettlementState.foodUnits + foodOutput <= specialCap &&
                   playerSettlementState.waterUnits + waterOutput <= specialCap &&
                   playerSettlementState.maintenanceUnits + maintenanceOutput <=
                   specialCap;
        }

        private void ConsumePlayerSettlementStorage(
            IEnumerable<ResourceCost> costs)
        {
            var totals = TetherfallSettlementRules.AggregateCosts(costs);
            foreach (var pair in totals)
            {
                playerSettlementStorage[pair.Key] =
                    Mathf.Max(0, GetPlayerSettlementStorage(pair.Key) - pair.Value);
            }
        }

        private void AddPlayerSettlementOutputs(
            IEnumerable<ResourceCost> resourcesToAdd,
            int food,
            int water,
            int maintenance,
            SettlementSimulationSummary summary)
        {
            var totals = TetherfallSettlementRules.AggregateCosts(resourcesToAdd);
            foreach (var pair in totals)
            {
                playerSettlementStorage[pair.Key] =
                    GetPlayerSettlementStorage(pair.Key) + pair.Value;
                summary.resourcesProduced += pair.Value;
            }

            var specialCap = Mathf.Min(
                TetherfallSettlementRules.HardSpecialStockCap,
                Mathf.Max(100, GetPlayerSettlementStorageCapacity()));
            playerSettlementState.foodUnits = Mathf.Clamp(
                playerSettlementState.foodUnits + Mathf.Max(0, food),
                0,
                specialCap);
            playerSettlementState.waterUnits = Mathf.Clamp(
                playerSettlementState.waterUnits + Mathf.Max(0, water),
                0,
                specialCap);
            playerSettlementState.maintenanceUnits = Mathf.Clamp(
                playerSettlementState.maintenanceUnits +
                Mathf.Max(0, maintenance),
                0,
                specialCap);
        }

        private bool HasValidPlayerSettlementBed(
            SettlementWorkerAssignmentSaveData worker)
        {
            return worker != null &&
                   TryGetPlayerSettlementPiece(
                       worker.bedPieceId,
                       out var runtime,
                       out _,
                       out var definition) &&
                   definition.role == SettlementPieceRole.PactBed &&
                   runtime.Durability > 0;
        }

        private bool HasOperationalPlayerSettlementTrough()
        {
            if (playerSettlementState.foodUnits <= 0 ||
                playerSettlementState.waterUnits <= 0)
            {
                return false;
            }

            foreach (var piece in playerSettlementPieces.Values)
            {
                if (piece != null &&
                    piece.Definition?.role == SettlementPieceRole.FeedingTrough &&
                    piece.Durability > 0 &&
                    HasAssignedPlayerSettlementWorkerAt(
                        piece.InstanceId,
                        SettlementWorkRole.Rancher))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasOperationalPlayerSettlementWatchPost()
        {
            foreach (var piece in playerSettlementPieces.Values)
            {
                if (piece != null &&
                    piece.Definition?.role == SettlementPieceRole.WatchPost &&
                    piece.Durability > 0 &&
                    HasAssignedPlayerSettlementWorkerAt(
                        piece.InstanceId,
                        SettlementWorkRole.Watchkeeper))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAssignedPlayerSettlementWorkerAt(
            string pieceInstanceId,
            SettlementWorkRole role)
        {
            return playerSettlementState?.workers?.Exists(worker =>
                worker != null &&
                worker.workRole == role &&
                string.Equals(
                    worker.workPieceId,
                    pieceInstanceId,
                    StringComparison.Ordinal)) == true;
        }

        private SettlementWorkerAssignmentSaveData FindPlayerSettlementWorker(
            string pactInstanceId)
        {
            var id = NormalizeSettlementExternalId(pactInstanceId);
            if (playerSettlementState?.workers == null ||
                string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (var index = 0;
                 index < playerSettlementState.workers.Count;
                 index++)
            {
                var worker = playerSettlementState.workers[index];
                if (worker != null &&
                    string.Equals(
                        worker.pactInstanceId,
                        id,
                        StringComparison.Ordinal))
                {
                    return worker;
                }
            }

            return null;
        }

        private int CountPlayerSettlementAssignments(
            string pieceInstanceId,
            bool bed,
            string excludedPactInstanceId)
        {
            var count = 0;
            foreach (var worker in playerSettlementState.workers)
            {
                if (worker == null ||
                    string.Equals(
                        worker.pactInstanceId,
                        excludedPactInstanceId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var assignedId = bed ? worker.bedPieceId : worker.workPieceId;
                if (string.Equals(
                        assignedId,
                        pieceInstanceId,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private void RemovePlayerSettlementAssignmentsForPiece(
            string pieceInstanceId)
        {
            for (var index = playerSettlementState.workers.Count - 1;
                 index >= 0;
                 index--)
            {
                var worker = playerSettlementState.workers[index];
                if (worker == null)
                {
                    continue;
                }

                var usedWorkPiece = string.Equals(
                        worker.workPieceId,
                        pieceInstanceId,
                        StringComparison.Ordinal);
                var usedBedPiece = string.Equals(
                        worker.bedPieceId,
                        pieceInstanceId,
                        StringComparison.Ordinal);
                if (!usedWorkPiece && !usedBedPiece)
                {
                    continue;
                }

                ClearPactSettlementPreference(worker.pactInstanceId);
                playerSettlementState.workers.RemoveAt(index);
                playerSettlementPactVitalsInitialized.Remove(
                    worker.pactInstanceId);
                DestroyPlayerSettlementPactWorkerProxy(worker.pactInstanceId);
            }

            SynchronizePlayerSettlementPactWorkerProxies();
        }

        private void RefreshPlayerSettlementWorkerProxies()
        {
            if (playerSettlementSimulationBand != SettlementSimulationBand.Near)
            {
                DeactivateAllPlayerSettlementWorkerProxies();
                return;
            }

            foreach (var pair in settlementWorkerProxies)
            {
                var worker = FindPlayerSettlementWorker(pair.Key);
                if (worker == null ||
                    !playerSettlementPieces.TryGetValue(
                        worker.workPieceId,
                        out var piece) ||
                    piece == null)
                {
                    SetSettlementWorkerProxySafe(
                        pair.Value,
                        false,
                        Vector3.zero,
                        Vector3.forward,
                        SettlementWorkRole.None,
                        0f);
                    continue;
                }

                if (!piece.TryGetWorkSocketPose(
                        GetPlayerSettlementWorkSlotIndex(worker),
                        out var workPosition,
                        out var workForward))
                {
                    SetSettlementWorkerProxySafe(
                        pair.Value,
                        false,
                        Vector3.zero,
                        Vector3.forward,
                        SettlementWorkRole.None,
                        0f);
                    continue;
                }

                SetSettlementWorkerProxySafe(
                    pair.Value,
                    true,
                    workPosition,
                    workForward,
                    worker.workRole,
                    GetPlayerSettlementWorkerEfficiency(worker.pactInstanceId));
            }
        }

        private void DeactivateAllPlayerSettlementWorkerProxies()
        {
            foreach (var proxy in settlementWorkerProxies.Values)
            {
                SetSettlementWorkerProxySafe(
                    proxy,
                    false,
                    Vector3.zero,
                    Vector3.forward,
                    SettlementWorkRole.None,
                    0f);
            }
        }

        private static void SetSettlementWorkerProxySafe(
            ITetherfallSettlementWorkerProxy proxy,
            bool visible,
            Vector3 workPosition,
            Vector3 workForward,
            SettlementWorkRole role,
            float efficiency)
        {
            if (proxy == null)
            {
                return;
            }

            try
            {
                proxy.SetSettlementProxyState(
                    visible,
                    workPosition,
                    workForward,
                    role,
                    efficiency);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Settlement worker proxy '{proxy.PactInstanceId}' failed safely: " +
                    $"{exception.GetType().Name}: {exception.Message}");
            }
        }

        private void LoadPlayerSettlementStorageFromState()
        {
            playerSettlementStorage.Clear();
            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                playerSettlementStorage[kind] = 0;
            }

            if (playerSettlementState?.storage == null)
            {
                return;
            }

            foreach (var stack in playerSettlementState.storage)
            {
                if (stack != null &&
                    Enum.IsDefined(typeof(ResourceKind), stack.kind) &&
                    stack.amount > 0)
                {
                    playerSettlementStorage[stack.kind] = stack.amount;
                }
            }
        }

        private void SyncPlayerSettlementStorageToState()
        {
            playerSettlementState.storage.Clear();
            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                var amount = GetPlayerSettlementStorage(kind);
                if (amount > 0)
                {
                    playerSettlementState.storage.Add(
                        new SettlementResourceStackSaveData
                        {
                            kind = kind,
                            amount = amount
                        });
                }
            }
        }

        private void ApplyExactSettlementRefundToPlayerBag(
            IEnumerable<ResourceCost> refund)
        {
            if (refund == null)
            {
                return;
            }

            foreach (var cost in refund)
            {
                if (cost == null || cost.amount <= 0)
                {
                    continue;
                }

                resources.TryGetValue(cost.kind, out var current);
                resources[cost.kind] = (int)Math.Min(
                    int.MaxValue,
                    (long)Mathf.Max(0, current) + cost.amount);
            }
        }

        private static string NormalizeSettlementExternalId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= 128 ? trimmed : trimmed.Substring(0, 128);
        }

        private void DrawPlayerSettlementPlacementStatus(Rect rect)
        {
            var statusRect = new Rect(
                rect.x,
                rect.y,
                rect.width,
                Mathf.Min(116f, rect.height));
            GUI.DrawTexture(statusRect, slateTexture);
            GUI.DrawTexture(
                new Rect(statusRect.x, statusRect.y, statusRect.width, 3f),
                playerSettlementPlacement.CandidateValid
                    ? tealTexture
                    : copperTexture);
            var definition = playerSettlementPlacement.ActiveDefinition;
            GUI.Label(
                new Rect(
                    statusRect.x + 18f,
                    statusRect.y + 14f,
                    statusRect.width - 36f,
                    24f),
                definition != null
                    ? $"PLACING {definition.displayName.ToUpperInvariant()}"
                    : "SETTLEMENT PLACEMENT",
                headingStyle);
            GUI.Label(
                new Rect(
                    statusRect.x + 18f,
                    statusRect.y + 42f,
                    statusRect.width - 36f,
                    42f),
                playerSettlementPlacement.FailureText,
                playerSettlementPlacement.CandidateValid
                    ? bodyStyle
                    : mutedStyle);
            GUI.Label(
                new Rect(
                    statusRect.x + 18f,
                    statusRect.yMax - 24f,
                    statusRect.width - 36f,
                    18f),
                $"LMB commit · {TetherfallInputBindings.Shared.GetBindingSummary(TetherfallInputAction.OpenSettlementPlanning)}/RMB/Esc cancel · mouse wheel or R rotate",
                mutedStyle);
        }

        private static string GetPlayerSettlementCompassDirection(Vector3 offset)
        {
            if (offset.sqrMagnitude <= 0.001f)
            {
                return "nearby";
            }

            var angle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            if (angle < 0f)
            {
                angle += 360f;
            }

            var index = Mathf.RoundToInt(angle / 45f) %
                        SettlementCompassDirections.Length;
            return SettlementCompassDirections[index];
        }

        private void DrawPlayerSettlementBuildPage(Rect rect)
        {
            if (!HasClaimedPlayerSettlement)
            {
                GUI.Label(
                    new Rect(rect.x, rect.y, rect.width * 0.64f, 48f),
                    "Claim one stable Threadheart core. Its first build radius is " +
                    "bounded to 48 m.",
                    bodyStyle);
                var core = TetherfallSettlementCatalog.FindPiece(
                    TetherfallSettlementCatalog.ThreadheartCoreDefinitionId);
                if (GUI.Button(
                        new Rect(rect.x, rect.y + 58f, 190f, 36f),
                        "Place Threadheart core"))
                {
                    if (!BeginPlayerSettlementPlacement(core.id, out var failure))
                    {
                        Notify(failure);
                    }
                }

                GUI.Label(
                    new Rect(rect.x + 206f, rect.y + 62f, rect.width - 206f, 40f),
                    DescribeCost(core.buildCost),
                    mutedStyle);
                return;
            }

            var tier = TetherfallSettlementCatalog.GetCoreTier(
                playerSettlementState.coreTier);
            GUI.Label(
                new Rect(rect.x, rect.y, rect.width, 24f),
                $"{tier.displayName} · {tier.buildRadius:0} m · " +
                $"{playerSettlementState.pieces.Count}/{tier.pieceCap} pieces · " +
                $"{playerSettlementState.workers.Count}/{tier.workerCap} workers",
                headingStyle);
            var nextTier = TetherfallSettlementCatalog.GetCoreTier(
                playerSettlementState.coreTier + 1);
            if (nextTier != null)
            {
                if (GUI.Button(
                        new Rect(rect.x, rect.y + 30f, 150f, 30f),
                        $"Upgrade to tier {nextTier.tier}"))
                {
                    if (!TryUpgradePlayerSettlementCore(out var failure))
                    {
                        Notify(failure);
                    }
                }

                GUI.Label(
                    new Rect(rect.x + 164f, rect.y + 34f, rect.width - 164f, 24f),
                    $"Rank {nextTier.minimumWayfinderLevel} · {DescribeCost(nextTier.cost)}",
                    mutedStyle);
            }
            else
            {
                GUI.Label(
                    new Rect(rect.x, rect.y + 34f, rect.width, 22f),
                    "Maximum bounded Threadheart tier reached.",
                    mutedStyle);
            }

            var buildable = new List<SettlementPieceDefinition>();
            foreach (var candidate in TetherfallSettlementCatalog.AllPieces)
            {
                if (candidate != null &&
                    candidate.role != SettlementPieceRole.ThreadheartCore)
                {
                    buildable.Add(candidate);
                }
            }

            var pageCount = Mathf.Max(
                1,
                Mathf.CeilToInt(buildable.Count / (float)CatalogRowsPerPage));
            playerSettlementCatalogPage = Mathf.Clamp(
                playerSettlementCatalogPage,
                0,
                pageCount - 1);
            var start = playerSettlementCatalogPage * CatalogRowsPerPage;
            var end = Mathf.Min(start + CatalogRowsPerPage, buildable.Count);
            var y = rect.y + 72f;
            for (var index = start; index < end; index++)
            {
                var definition = buildable[index];
                if (GUI.Button(
                        new Rect(rect.x, y, 152f, 30f),
                        $"Build {definition.displayName}"))
                {
                    if (!BeginPlayerSettlementPlacement(
                            definition.id,
                            out var failure))
                    {
                        Notify(failure);
                    }
                }

                var gate =
                    $"T{definition.minimumCoreTier} · Rank {definition.minimumWayfinderLevel}";
                GUI.Label(
                    new Rect(
                        rect.x + 164f,
                        y + 2f,
                        rect.width - 164f,
                        30f),
                    $"{gate} · {DescribeCost(definition.buildCost)}",
                    mutedStyle);
                y += 38f;
            }

            var navigationY = rect.yMax - 36f;
            if (GUI.Button(
                    new Rect(rect.x, navigationY, 90f, 28f),
                    "Previous") &&
                playerSettlementCatalogPage > 0)
            {
                playerSettlementCatalogPage--;
            }

            if (GUI.Button(
                    new Rect(rect.x + 98f, navigationY, 90f, 28f),
                    "Next") &&
                playerSettlementCatalogPage + 1 < pageCount)
            {
                playerSettlementCatalogPage++;
            }

            GUI.Label(
                new Rect(rect.x + 202f, navigationY + 4f, 120f, 20f),
                $"{playerSettlementCatalogPage + 1}/{pageCount}",
                mutedStyle);
            DrawSelectedPlayerSettlementPieceActions(rect);
        }

        private void DrawPlayerSettlementStoragePage(Rect rect)
        {
            GUI.Label(
                new Rect(rect.x, rect.y, rect.width, 24f),
                $"Settlement stores {GetPlayerSettlementStoredTotal()}/" +
                $"{GetPlayerSettlementStorageCapacity()} resource units",
                headingStyle);
            GUI.Label(
                new Rect(rect.x, rect.y + 27f, rect.width, 22f),
                $"Food {playerSettlementState.foodUnits} · Water " +
                $"{playerSettlementState.waterUnits} · Maintenance " +
                $"{playerSettlementState.maintenanceUnits}",
                bodyStyle);
            var y = rect.y + 62f;
            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                GUI.Label(
                    new Rect(rect.x, y + 6f, 230f, 22f),
                    $"{ResourceLabel(kind)} · settlement " +
                    $"{GetPlayerSettlementStorage(kind)} · bag {GetResource(kind)}",
                    bodyStyle);
                if (GUI.Button(
                        new Rect(rect.x + 244f, y, 88f, 30f),
                        "Deposit 1"))
                {
                    if (!TryDepositPlayerSettlementResource(
                            kind,
                            1,
                            out var failure))
                    {
                        Notify(failure);
                    }
                }

                if (GUI.Button(
                        new Rect(rect.x + 340f, y, 92f, 30f),
                        "Withdraw 1"))
                {
                    if (!TryWithdrawPlayerSettlementResource(
                            kind,
                            1,
                            out var failure))
                    {
                        Notify(failure);
                    }
                }

                y += 38f;
            }

            GUI.Label(
                new Rect(rect.x, rect.yMax - 50f, rect.width, 42f),
                "Production reads only settlement storage. It never consumes the " +
                "Wayfinder bag unless you explicitly deposit.",
                mutedStyle);
        }

        private void DrawPlayerSettlementWorkersPage(Rect rect)
        {
            DrawPlayerSettlementPactWorkersPage(rect);
        }

        private void DrawPlayerSettlementProductionPage(Rect rect)
        {
            GUI.Label(
                new Rect(rect.x, rect.y, rect.width, 24f),
                "Station queues",
                headingStyle);
            var y = rect.y + 34f;
            var stationCount = 0;
            foreach (var record in playerSettlementState.pieces)
            {
                var definition = record != null
                    ? TetherfallSettlementCatalog.FindPiece(record.definitionId)
                    : null;
                if (record == null || definition == null || !definition.IsStation)
                {
                    continue;
                }

                stationCount++;
                var job = playerSettlementState.stationJobs.Find(candidate =>
                    candidate != null &&
                    candidate.stationPieceId == record.instanceId);
                var recipes = TetherfallSettlementCatalog.GetRecipesForStation(
                    definition.id);
                GUI.Label(
                    new Rect(rect.x, y, 190f, 24f),
                    job != null
                        ? $"{definition.displayName} · {job.queuedCycles} queued"
                        : $"{definition.displayName} · idle",
                    bodyStyle);
                var buttonX = rect.x + 196f;
                for (var recipeIndex = 0;
                     recipeIndex < recipes.Count && recipeIndex < 2;
                     recipeIndex++)
                {
                    var recipe = recipes[recipeIndex];
                    if (GUI.Button(
                            new Rect(buttonX, y, 154f, 28f),
                            $"+1 {recipe.displayName}"))
                    {
                        if (!TryQueuePlayerSettlementRecipe(
                                record.instanceId,
                                recipe.id,
                                1,
                                out var failure))
                        {
                            Notify(failure);
                        }
                    }

                    buttonX += 162f;
                }

                if (job != null &&
                    GUI.Button(
                        new Rect(rect.xMax - 64f, y, 64f, 28f),
                        "Clear"))
                {
                    CancelPlayerSettlementRecipe(record.instanceId);
                }

                y += 38f;
                if (y > rect.yMax - 72f)
                {
                    break;
                }
            }

            if (stationCount == 0)
            {
                GUI.Label(
                    new Rect(rect.x, y, rect.width, 40f),
                    "Build a workbench, forge, cookpot, loom, or medicine table.",
                    bodyStyle);
            }

            GUI.Label(
                new Rect(rect.x, rect.yMax - 58f, rect.width, 50f),
                "Farms and water collectors run from assigned workers. Beds, food, " +
                "water, troughs, morale, stamina, damage, and watch coverage affect output.",
                mutedStyle);
        }

        private void DrawSelectedPlayerSettlementPieceActions(Rect rect)
        {
            if (string.IsNullOrEmpty(selectedPlayerSettlementPieceId) ||
                !TryGetPlayerSettlementPiece(
                    selectedPlayerSettlementPieceId,
                    out var runtime,
                    out _,
                    out var definition))
            {
                return;
            }

            var x = rect.xMax - 292f;
            var y = rect.yMax - 76f;
            GUI.Label(
                new Rect(x, y - 24f, 292f, 22f),
                $"{definition.displayName} · {runtime.Durability}/" +
                $"{runtime.MaximumDurability}",
                bodyStyle);
            var isWayfinderBed =
                definition.role == SettlementPieceRole.WayfinderBed;
            var repairX = x;
            if (isWayfinderBed)
            {
                if (GUI.Button(new Rect(x, y, 90f, 30f), "Rest"))
                {
                    if (!TryRestAtPlayerSettlementBed(
                            selectedPlayerSettlementPieceId,
                            out var failure))
                    {
                        Notify(failure);
                    }
                }

                repairX += 98f;
            }

            var actionWidth = isWayfinderBed ? 90f : 138f;
            if (GUI.Button(new Rect(repairX, y, actionWidth, 30f), "Repair"))
            {
                if (!TryRepairPlayerSettlementPiece(
                        selectedPlayerSettlementPieceId,
                        out var failure))
                {
                    Notify(failure);
                }
            }

            if (GUI.Button(
                    new Rect(
                        repairX + actionWidth + 8f,
                        y,
                        isWayfinderBed ? 96f : 146f,
                        30f),
                    "Demolish · 60%"))
            {
                if (!TryDemolishPlayerSettlementPiece(
                        selectedPlayerSettlementPieceId,
                        out var failure))
                {
                    Notify(failure);
                }
            }
        }
    }
}
