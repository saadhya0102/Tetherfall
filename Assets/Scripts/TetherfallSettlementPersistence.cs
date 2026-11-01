using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    [Serializable]
    public sealed class SettlementResourceStackSaveData
    {
        public ResourceKind kind;
        public int amount;
    }

    [Serializable]
    public sealed class SettlementPlacedPieceSaveData
    {
        public string instanceId;
        public string definitionId;
        public Vector3 position;
        public float rotationY;
        public int durability;
        public int visualSeed;
    }

    [Serializable]
    public sealed class SettlementWorkerAssignmentSaveData
    {
        public string pactInstanceId;
        public SettlementWorkRole workRole;
        public string workPieceId;
        public string bedPieceId;
        public float morale = 75f;
        public float stamina = 100f;
        public double productionProgressSeconds;
        public double needProgressSeconds;
        public bool lastMealSatisfied = true;
    }

    [Serializable]
    public sealed class SettlementStationJobSaveData
    {
        public string stationPieceId;
        public string recipeId;
        public int queuedCycles;
        public double progressSeconds;
    }

    [Serializable]
    public sealed class SettlementSaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public bool coreClaimed;
        public int coreTier = 1;
        public long lastSimulatedUtcTicks;
        public int nextPieceSequence = 1;
        public int foodUnits;
        public int waterUnits;
        public int maintenanceUnits;
        public List<SettlementResourceStackSaveData> storage =
            new List<SettlementResourceStackSaveData>();
        public List<SettlementPlacedPieceSaveData> pieces =
            new List<SettlementPlacedPieceSaveData>();
        public List<SettlementWorkerAssignmentSaveData> workers =
            new List<SettlementWorkerAssignmentSaveData>();
        public List<SettlementStationJobSaveData> stationJobs =
            new List<SettlementStationJobSaveData>();
    }

    [Serializable]
    public sealed class SettlementNormalizationReport
    {
        public int droppedPieces;
        public int droppedWorkers;
        public int droppedJobs;
        public int clampedStorageUnits;
        public bool removedDuplicateCore;
        public bool clearedInvalidClaim;

        public bool Changed =>
            droppedPieces > 0 ||
            droppedWorkers > 0 ||
            droppedJobs > 0 ||
            clampedStorageUnits > 0 ||
            removedDuplicateCore ||
            clearedInvalidClaim;
    }

    // Integration dependency: the existing declaration in TetherfallSaveSystem.cs must
    // also receive the partial modifier before this isolated module is compiled.
    public sealed partial class TetherfallSaveData
    {
        public SettlementSaveData playerSettlement = new SettlementSaveData();
    }

    public static class TetherfallSettlementPersistence
    {
        private const int MaximumRecordsToInspect = 4096;
        private const int MaximumStableIdLength = 128;

        public static SettlementSaveData CreateDefault(long nowUtcTicks = 0L)
        {
            return new SettlementSaveData
            {
                version = SettlementSaveData.CurrentVersion,
                coreTier = 1,
                nextPieceSequence = 1,
                lastSimulatedUtcTicks = nowUtcTicks > 0L
                    ? nowUtcTicks
                    : DateTime.UtcNow.Ticks
            };
        }

        public static SettlementSaveData Clone(SettlementSaveData source)
        {
            if (source == null)
            {
                return CreateDefault();
            }

            var clone = new SettlementSaveData
            {
                version = source.version,
                coreClaimed = source.coreClaimed,
                coreTier = source.coreTier,
                lastSimulatedUtcTicks = source.lastSimulatedUtcTicks,
                nextPieceSequence = source.nextPieceSequence,
                foodUnits = source.foodUnits,
                waterUnits = source.waterUnits,
                maintenanceUnits = source.maintenanceUnits
            };

            if (source.storage != null)
            {
                foreach (var stack in source.storage)
                {
                    if (stack != null)
                    {
                        clone.storage.Add(new SettlementResourceStackSaveData
                        {
                            kind = stack.kind,
                            amount = stack.amount
                        });
                    }
                }
            }

            if (source.pieces != null)
            {
                foreach (var piece in source.pieces)
                {
                    if (piece != null)
                    {
                        clone.pieces.Add(ClonePiece(piece));
                    }
                }
            }

            if (source.workers != null)
            {
                foreach (var worker in source.workers)
                {
                    if (worker != null)
                    {
                        clone.workers.Add(new SettlementWorkerAssignmentSaveData
                        {
                            pactInstanceId = worker.pactInstanceId,
                            workRole = worker.workRole,
                            workPieceId = worker.workPieceId,
                            bedPieceId = worker.bedPieceId,
                            morale = worker.morale,
                            stamina = worker.stamina,
                            productionProgressSeconds = worker.productionProgressSeconds,
                            needProgressSeconds = worker.needProgressSeconds,
                            lastMealSatisfied = worker.lastMealSatisfied
                        });
                    }
                }
            }

            if (source.stationJobs != null)
            {
                foreach (var job in source.stationJobs)
                {
                    if (job != null)
                    {
                        clone.stationJobs.Add(new SettlementStationJobSaveData
                        {
                            stationPieceId = job.stationPieceId,
                            recipeId = job.recipeId,
                            queuedCycles = job.queuedCycles,
                            progressSeconds = job.progressSeconds
                        });
                    }
                }
            }

            return clone;
        }

        public static SettlementNormalizationReport NormalizeInPlace(
            SettlementSaveData data,
            long nowUtcTicks = 0L)
        {
            var report = new SettlementNormalizationReport();
            if (data == null)
            {
                return report;
            }

            var now = nowUtcTicks > 0L ? nowUtcTicks : DateTime.UtcNow.Ticks;
            data.version = SettlementSaveData.CurrentVersion;
            data.coreTier = Mathf.Clamp(data.coreTier, 1, 3);
            data.nextPieceSequence = Mathf.Clamp(data.nextPieceSequence, 1, int.MaxValue);
            data.lastSimulatedUtcTicks = Math.Max(
                0L,
                Math.Min(data.lastSimulatedUtcTicks, now));
            data.storage ??= new List<SettlementResourceStackSaveData>();
            data.pieces ??= new List<SettlementPlacedPieceSaveData>();
            data.workers ??= new List<SettlementWorkerAssignmentSaveData>();
            data.stationJobs ??= new List<SettlementStationJobSaveData>();

            NormalizePieces(data, report);
            NormalizeStorage(data, report);
            NormalizeWorkers(data, report);
            NormalizeJobs(data, report);

            var specialCap = data.coreClaimed
                ? Mathf.Min(
                    TetherfallSettlementRules.HardSpecialStockCap,
                    Mathf.Max(100, GetStorageCapacity(data)))
                : 0;
            data.foodUnits = Mathf.Clamp(data.foodUnits, 0, specialCap);
            data.waterUnits = Mathf.Clamp(data.waterUnits, 0, specialCap);
            data.maintenanceUnits = Mathf.Clamp(data.maintenanceUnits, 0, specialCap);
            return report;
        }

        public static int GetStorageCapacity(SettlementSaveData data)
        {
            if (data == null || !data.coreClaimed)
            {
                return 0;
            }

            long capacity = TetherfallSettlementRules.GetBaseStorageCapacity(data.coreTier);
            if (data.pieces != null)
            {
                var inspectCount = Math.Min(data.pieces.Count, MaximumRecordsToInspect);
                for (var index = 0; index < inspectCount; index++)
                {
                    var piece = data.pieces[index];
                    var definition = piece != null
                        ? TetherfallSettlementCatalog.FindPiece(piece.definitionId)
                        : null;
                    if (definition != null &&
                        definition.id != TetherfallSettlementCatalog.ThreadheartCoreDefinitionId)
                    {
                        capacity += Mathf.Max(0, definition.storageCapacity);
                    }
                }
            }

            return (int)Math.Min(TetherfallSettlementRules.HardStorageCap, capacity);
        }

        public static int GetStoredResourceTotal(SettlementSaveData data)
        {
            if (data?.storage == null)
            {
                return 0;
            }

            long total = 0L;
            foreach (var stack in data.storage)
            {
                if (stack != null && stack.amount > 0)
                {
                    total += stack.amount;
                    if (total >= int.MaxValue)
                    {
                        return int.MaxValue;
                    }
                }
            }

            return (int)total;
        }

        public static SettlementPlacedPieceSaveData FindCore(SettlementSaveData data)
        {
            if (data?.pieces == null)
            {
                return null;
            }

            return data.pieces.Find(piece =>
                piece != null &&
                string.Equals(
                    piece.definitionId,
                    TetherfallSettlementCatalog.ThreadheartCoreDefinitionId,
                    StringComparison.Ordinal));
        }

        public static SettlementPlacedPieceSaveData ClonePiece(
            SettlementPlacedPieceSaveData source)
        {
            return new SettlementPlacedPieceSaveData
            {
                instanceId = source.instanceId,
                definitionId = source.definitionId,
                position = source.position,
                rotationY = source.rotationY,
                durability = source.durability,
                visualSeed = source.visualSeed
            };
        }

        private static void NormalizePieces(
            SettlementSaveData data,
            SettlementNormalizationReport report)
        {
            var source = data.pieces;
            var normalized = new List<SettlementPlacedPieceSaveData>();
            SettlementPlacedPieceSaveData core = null;
            var inspectCount = Math.Min(source.Count, MaximumRecordsToInspect);
            for (var index = 0; index < inspectCount; index++)
            {
                var candidate = source[index];
                if (candidate == null ||
                    !string.Equals(
                        candidate.definitionId,
                        TetherfallSettlementCatalog.ThreadheartCoreDefinitionId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (core == null)
                {
                    core = candidate;
                }
                else
                {
                    report.removedDuplicateCore = true;
                    report.droppedPieces++;
                }
            }

            if (source.Count > MaximumRecordsToInspect)
            {
                report.droppedPieces += source.Count - MaximumRecordsToInspect;
            }

            if (core != null &&
                TetherfallSettlementRules.IsBoundedWorldPosition(core.position))
            {
                var definition = TetherfallSettlementCatalog.FindPiece(
                    TetherfallSettlementCatalog.ThreadheartCoreDefinitionId);
                core = ClonePiece(core);
                core.instanceId = TetherfallSettlementCatalog.ThreadheartCoreInstanceId;
                core.definitionId = TetherfallSettlementCatalog.ThreadheartCoreDefinitionId;
                core.rotationY = TetherfallSettlementRules.NormalizeRotation(
                    core.rotationY,
                    definition.rotationPolicy);
                core.durability = Mathf.Clamp(
                    core.durability,
                    0,
                    definition.maximumDurability);
                normalized.Add(core);
                data.coreClaimed = true;
            }
            else
            {
                if (data.coreClaimed)
                {
                    report.clearedInvalidClaim = true;
                }

                data.coreClaimed = false;
                data.coreTier = 1;
            }

            if (data.coreClaimed)
            {
                var ids = new HashSet<string>(StringComparer.Ordinal)
                {
                    TetherfallSettlementCatalog.ThreadheartCoreInstanceId
                };
                var pieceCap = TetherfallSettlementRules.GetPieceCap(data.coreTier);
                var buildRadius = TetherfallSettlementRules.GetBuildRadius(data.coreTier);
                var recoveredSequence = 1;
                for (var index = 0; index < inspectCount && normalized.Count < pieceCap; index++)
                {
                    var candidate = source[index];
                    if (candidate == null ||
                        string.Equals(
                            candidate.definitionId,
                            TetherfallSettlementCatalog.ThreadheartCoreDefinitionId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var definition = TetherfallSettlementCatalog.FindPiece(candidate.definitionId);
                    if (definition == null ||
                        !TetherfallSettlementRules.IsBoundedWorldPosition(candidate.position) ||
                        definition.minimumCoreTier > data.coreTier)
                    {
                        report.droppedPieces++;
                        continue;
                    }

                    var offset = candidate.position - core.position;
                    var horizontal = new Vector2(offset.x, offset.z);
                    if (horizontal.sqrMagnitude > buildRadius * buildRadius ||
                        Mathf.Abs(offset.y) > 24f)
                    {
                        report.droppedPieces++;
                        continue;
                    }

                    var id = TrimStableId(candidate.instanceId);
                    if (string.IsNullOrEmpty(id))
                    {
                        do
                        {
                            id = $"settlement.piece.recovered.{recoveredSequence:0000}";
                            recoveredSequence++;
                        }
                        while (ids.Contains(id));
                    }

                    if (!ids.Add(id))
                    {
                        report.droppedPieces++;
                        continue;
                    }

                    var clean = ClonePiece(candidate);
                    clean.instanceId = id;
                    clean.definitionId = definition.id;
                    clean.rotationY = TetherfallSettlementRules.NormalizeRotation(
                        clean.rotationY,
                        definition.rotationPolicy);
                    clean.durability = Mathf.Clamp(
                        clean.durability,
                        0,
                        definition.maximumDurability);
                    normalized.Add(clean);
                }

                var validCandidates = 0;
                for (var index = 0; index < inspectCount; index++)
                {
                    var candidate = source[index];
                    if (candidate != null &&
                        !string.Equals(
                            candidate.definitionId,
                            TetherfallSettlementCatalog.ThreadheartCoreDefinitionId,
                            StringComparison.Ordinal))
                    {
                        validCandidates++;
                    }
                }

                report.droppedPieces += Mathf.Max(
                    0,
                    validCandidates - (normalized.Count - 1) - report.droppedPieces);
            }
            else
            {
                for (var index = 0; index < inspectCount; index++)
                {
                    var candidate = source[index];
                    if (candidate != null && candidate != core)
                    {
                        report.droppedPieces++;
                    }
                }
            }

            data.pieces = normalized;
            data.nextPieceSequence = Mathf.Max(data.nextPieceSequence, normalized.Count + 1);
        }

        private static void NormalizeStorage(
            SettlementSaveData data,
            SettlementNormalizationReport report)
        {
            var totals = new Dictionary<ResourceKind, long>();
            var inspectCount = Math.Min(data.storage.Count, MaximumRecordsToInspect);
            for (var index = 0; index < inspectCount; index++)
            {
                var stack = data.storage[index];
                if (stack == null ||
                    !Enum.IsDefined(typeof(ResourceKind), stack.kind) ||
                    stack.amount <= 0)
                {
                    continue;
                }

                totals.TryGetValue(stack.kind, out var current);
                totals[stack.kind] = Math.Min(int.MaxValue, current + stack.amount);
            }

            var capacity = GetStorageCapacity(data);
            var remaining = capacity;
            var normalized = new List<SettlementResourceStackSaveData>();
            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                if (!totals.TryGetValue(kind, out var rawAmount) || rawAmount <= 0L)
                {
                    continue;
                }

                var accepted = Mathf.Clamp((int)Math.Min(int.MaxValue, rawAmount), 0, remaining);
                var discarded = rawAmount - accepted;
                var discardedForReport = discarded >= int.MaxValue
                    ? int.MaxValue
                    : (int)Math.Max(0L, discarded);
                report.clampedStorageUnits = (int)Math.Min(
                    int.MaxValue,
                    (long)report.clampedStorageUnits + discardedForReport);
                if (accepted > 0)
                {
                    normalized.Add(new SettlementResourceStackSaveData
                    {
                        kind = kind,
                        amount = accepted
                    });
                    remaining -= accepted;
                }
            }

            data.storage = normalized;
        }

        private static void NormalizeWorkers(
            SettlementSaveData data,
            SettlementNormalizationReport report)
        {
            var normalized = new List<SettlementWorkerAssignmentSaveData>();
            if (!data.coreClaimed)
            {
                report.droppedWorkers += data.workers.Count;
                data.workers = normalized;
                return;
            }

            var pieceById = BuildPieceMap(data.pieces);
            var workOccupancy = new Dictionary<string, int>(StringComparer.Ordinal);
            var bedOccupancy = new Dictionary<string, int>(StringComparer.Ordinal);
            var workerIds = new HashSet<string>(StringComparer.Ordinal);
            var workerCap = TetherfallSettlementRules.GetWorkerCap(data.coreTier);
            var inspectCount = Math.Min(data.workers.Count, MaximumRecordsToInspect);
            for (var index = 0; index < inspectCount && normalized.Count < workerCap; index++)
            {
                var source = data.workers[index];
                var pactId = TrimStableId(source?.pactInstanceId);
                if (source == null || string.IsNullOrEmpty(pactId) || !workerIds.Add(pactId))
                {
                    report.droppedWorkers++;
                    continue;
                }

                var worker = new SettlementWorkerAssignmentSaveData
                {
                    pactInstanceId = pactId,
                    workRole = Enum.IsDefined(typeof(SettlementWorkRole), source.workRole)
                        ? source.workRole
                        : SettlementWorkRole.None,
                    workPieceId = TrimStableId(source.workPieceId),
                    bedPieceId = TrimStableId(source.bedPieceId),
                    morale = SanitizeUnitStat(source.morale, 50f),
                    stamina = SanitizeUnitStat(source.stamina, 50f),
                    productionProgressSeconds = SanitizeProgress(
                        source.productionProgressSeconds,
                        86400d),
                    needProgressSeconds = SanitizeProgress(source.needProgressSeconds, 3600d),
                    lastMealSatisfied = source.lastMealSatisfied
                };

                if (!TryValidateWorkAssignment(
                        worker,
                        pieceById,
                        workOccupancy))
                {
                    report.droppedWorkers++;
                    continue;
                }

                if (!TryValidateBedAssignment(worker, pieceById, bedOccupancy))
                {
                    report.droppedWorkers++;
                    workOccupancy[worker.workPieceId] =
                        Mathf.Max(0, workOccupancy[worker.workPieceId] - 1);
                    continue;
                }

                normalized.Add(worker);
            }

            report.droppedWorkers += Math.Max(
                0,
                data.workers.Count - inspectCount);
            report.droppedWorkers += Math.Max(
                0,
                Math.Min(data.workers.Count, inspectCount) - normalized.Count - report.droppedWorkers);
            data.workers = normalized;
        }

        private static void NormalizeJobs(
            SettlementSaveData data,
            SettlementNormalizationReport report)
        {
            var normalized = new List<SettlementStationJobSaveData>();
            if (!data.coreClaimed)
            {
                report.droppedJobs += data.stationJobs.Count;
                data.stationJobs = normalized;
                return;
            }

            var pieceById = BuildPieceMap(data.pieces);
            var stationIds = new HashSet<string>(StringComparer.Ordinal);
            var inspectCount = Math.Min(data.stationJobs.Count, MaximumRecordsToInspect);
            for (var index = 0; index < inspectCount; index++)
            {
                var source = data.stationJobs[index];
                var stationId = TrimStableId(source?.stationPieceId);
                var recipe = source != null
                    ? TetherfallSettlementCatalog.FindRecipe(source.recipeId)
                    : null;
                if (source == null ||
                    string.IsNullOrEmpty(stationId) ||
                    recipe == null ||
                    recipe.minimumCoreTier > data.coreTier ||
                    source.queuedCycles <= 0 ||
                    !stationIds.Add(stationId) ||
                    !pieceById.TryGetValue(stationId, out var piece) ||
                    !string.Equals(
                        piece.definitionId,
                        recipe.stationDefinitionId,
                        StringComparison.Ordinal))
                {
                    report.droppedJobs++;
                    continue;
                }

                normalized.Add(new SettlementStationJobSaveData
                {
                    stationPieceId = stationId,
                    recipeId = recipe.id,
                    queuedCycles = Mathf.Clamp(
                        source.queuedCycles,
                        1,
                        TetherfallSettlementRules.HardQueuedCyclesPerStation),
                    progressSeconds = SanitizeProgress(
                        source.progressSeconds,
                        Math.Max(0.01d, recipe.cycleSeconds))
                });
            }

            report.droppedJobs += Math.Max(0, data.stationJobs.Count - inspectCount);
            data.stationJobs = normalized;
        }

        private static bool TryValidateWorkAssignment(
            SettlementWorkerAssignmentSaveData worker,
            Dictionary<string, SettlementPlacedPieceSaveData> pieceById,
            Dictionary<string, int> occupancy)
        {
            if (string.IsNullOrEmpty(worker.workPieceId) ||
                !pieceById.TryGetValue(worker.workPieceId, out var piece))
            {
                return false;
            }

            var definition = TetherfallSettlementCatalog.FindPiece(piece.definitionId);
            if (piece.durability <= 0 ||
                definition == null ||
                definition.workSlots <= 0 ||
                definition.workRole == SettlementWorkRole.None ||
                TetherfallSettlementPactBridge.GetCompatibleWorkKinds(
                    definition.workRole).Count == 0)
            {
                return false;
            }

            occupancy.TryGetValue(worker.workPieceId, out var assigned);
            if (assigned >= definition.workSlots)
            {
                return false;
            }

            occupancy[worker.workPieceId] = assigned + 1;
            worker.workRole = definition.workRole;
            return true;
        }

        private static bool TryValidateBedAssignment(
            SettlementWorkerAssignmentSaveData worker,
            Dictionary<string, SettlementPlacedPieceSaveData> pieceById,
            Dictionary<string, int> occupancy)
        {
            if (string.IsNullOrEmpty(worker.bedPieceId) ||
                !pieceById.TryGetValue(worker.bedPieceId, out var piece))
            {
                return false;
            }

            var definition = TetherfallSettlementCatalog.FindPiece(piece.definitionId);
            if (piece.durability <= 0 ||
                definition == null ||
                definition.role != SettlementPieceRole.PactBed ||
                definition.bedSlots <= 0)
            {
                return false;
            }

            occupancy.TryGetValue(worker.bedPieceId, out var assigned);
            if (assigned >= definition.bedSlots)
            {
                return false;
            }

            occupancy[worker.bedPieceId] = assigned + 1;
            return true;
        }

        private static Dictionary<string, SettlementPlacedPieceSaveData> BuildPieceMap(
            List<SettlementPlacedPieceSaveData> pieces)
        {
            var result = new Dictionary<string, SettlementPlacedPieceSaveData>(
                StringComparer.Ordinal);
            if (pieces == null)
            {
                return result;
            }

            foreach (var piece in pieces)
            {
                if (piece != null && !string.IsNullOrEmpty(piece.instanceId))
                {
                    result[piece.instanceId] = piece;
                }
            }

            return result;
        }

        private static string TrimStableId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= MaximumStableIdLength
                ? trimmed
                : trimmed.Substring(0, MaximumStableIdLength);
        }

        private static float SanitizeUnitStat(float value, float fallback)
        {
            return TetherfallSettlementRules.IsFinite(value)
                ? Mathf.Clamp(value, 0f, 100f)
                : fallback;
        }

        private static double SanitizeProgress(double value, double maximum)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0d;
            }

            return Math.Max(0d, Math.Min(maximum, value));
        }
    }
}
