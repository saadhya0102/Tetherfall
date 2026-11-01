using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tetherfall
{
    [Serializable]
    public sealed partial class TetherfallSaveData
    {
        public const int CurrentSchemaVersion = 24;

        public int schemaVersion = CurrentSchemaVersion;
        public int level = 1;
        public int experience;
        public int experienceToNext = 100;
        public int health = 100;
        public int maxHealth = 100;
        public int gatheredNodes;
        public int clearedCamps;
        public bool hearthgateRestored;
        public bool hasCamp;
        public bool wasMounted;
        public Vector3 playerPosition;
        public Vector3 campPosition;
        public string activePactName;
        public string mountPactName;
        public WeaponDiscipline activeWeapon = WeaponDiscipline.Pathblade;
        public int favors;
        public int cookedMeals;
        public string activeMealId;
        public MealEffect activeMealEffect;
        public long activeMealEffectEndsUtcTicks;
        public int campTier;
        public int settlementTier;
        public bool fenlightPatrolCleared;
        public int activatedDungeonPylons;
        public bool skyvaultWaitingForSwarm;
        public int skyvaultSwarmEnemies;
        public int partyRotation;
        public CinderwoldArcSaveData cinderwoldArc = new CinderwoldArcSaveData();
        public int tutorialStep;
        public int tutorialEventFlags;
        public bool dungeonBossDefeated;
        public bool tutorialComplete;
        public bool tutorialSkipped;
        public List<ResourceAmount> resources = new List<ResourceAmount>();
        public List<string> pactNames = new List<string>();
        public List<string> recruitNames = new List<string>();
        public List<string> discoveredRegions = new List<string>();
        public List<string> unlockedEquipment = new List<string>();
        public List<string> equippedEquipmentIds = new List<string>();
        public List<string> firstClearedEncounterIds = new List<string>();
        public List<string> completedRouteEventIds = new List<string>();
        public List<RouteEventSaveData> routeEvents = new List<RouteEventSaveData>();
        public List<string> recoveredFieldCacheIds = new List<string>();
        public List<RestorationSiteSaveData> restorationSites = new List<RestorationSiteSaveData>();
        public List<MealStackSaveData> packedMeals = new List<MealStackSaveData>();
        public List<string> knownRecipeIds = new List<string>();
        public List<string> completedCampContractIds = new List<string>();
        public List<ResourceNodeSaveData> resourceNodes = new List<ResourceNodeSaveData>();
        public List<FarmPlotSaveData> farmPlots = new List<FarmPlotSaveData>();
        public List<TrialSaveData> trials = new List<TrialSaveData>();
        public List<DungeonSaveData> dungeons = new List<DungeonSaveData>();
        public List<CityActivitySaveData> cityActivities = new List<CityActivitySaveData>();
        public List<BountySaveData> bounties = new List<BountySaveData>();
        public string activeBountyId;
        public List<string> unlockedThreadwayIds = new List<string>();
        public int expeditionDirective;
        public List<QuestProgressData> quests = new List<QuestProgressData>();
        public List<SkillRankSaveData> skillRanks = new List<SkillRankSaveData>();
        public List<PactProgressSaveData> pactProgress = new List<PactProgressSaveData>();
        public PactRosterSaveData pactRoster = new PactRosterSaveData();
    }

    [Serializable]
    public sealed class ResourceAmount
    {
        public ResourceKind kind;
        public int amount;
    }

    [Serializable]
    public sealed class QuestProgressData
    {
        public string id;
        public int current;
        public bool completed;
    }

    [Serializable]
    public sealed class FarmPlotSaveData
    {
        public string id;
        public long matureAtUtcTicks;
    }

    [Serializable]
    public sealed class RestorationSiteSaveData
    {
        public string id;
        public int tier;
    }

    [Serializable]
    public sealed class MealStackSaveData
    {
        public string mealId;
        public int amount;
    }

    [Serializable]
    public sealed class ResourceNodeSaveData
    {
        public string id;
        public long respawnAtUtcTicks;
    }

    [Serializable]
    public sealed class CinderwoldArcSaveData
    {
        public bool accepted;
        public int completedMilestoneMask;
        public string resolutionId;
        public bool resolutionRewardGranted;
    }

    [Serializable]
    public sealed class TrialSaveData
    {
        public string id;
        public TrialState state;
        public int[] dialStates = new int[0];
        public int activeWaveIndex;
        public List<string> remainingEnemyIds = new List<string>();
        public bool bossDefeated;
        public bool rewardGranted;
    }

    [Serializable]
    public sealed class CityActivitySaveData
    {
        public string id;
        public int completionCount;
        public long nextAvailableUtcTicks;
    }

    [Serializable]
    public sealed class BountySaveData
    {
        public string id;
        public int completionCount;
        public long nextAvailableUtcTicks;
    }

    [Serializable]
    public sealed class RouteEventSaveData
    {
        public string id;
        public int completionCount;
        public long nextAvailableUtcTicks;
    }

    public static class TetherfallSaveSystem
    {
        private const string SaveFileName = "tetherfall-expedition.json";
        private const string TemporaryFileName = "tetherfall-expedition.tmp";
        private const string BackupFileName = "tetherfall-expedition.bak";
        private static readonly object SaveLock = new object();

        private enum ReadResult
        {
            Missing,
            Valid,
            Invalid,
            UnsupportedFutureVersion
        }

        [Serializable]
        private sealed class SaveHeader
        {
            public int schemaVersion;
        }

        public static bool TryLoad(out TetherfallSaveData data)
        {
            lock (SaveLock)
            {
                data = null;
                try
                {
                    if (!TryGetSavePaths(
                            out _,
                            out var savePath,
                            out var temporaryPath,
                            out var backupPath))
                    {
                        Debug.LogWarning("Tetherfall could not load the expedition record because the persistent data path is unavailable.");
                        return false;
                    }

                    // A valid temporary record is the newest fully-written candidate after an
                    // interrupted fallback replacement. The backup remains the final safety net.
                    var candidatePaths = new[] { savePath, temporaryPath, backupPath };
                    var candidateNames = new[] { SaveFileName, TemporaryFileName, BackupFileName };
                    for (var index = 0; index < candidatePaths.Length; index++)
                    {
                        var result = TryRead(
                            candidatePaths[index],
                            out data,
                            out var sourceSchemaVersion,
                            out var failureReason);
                        if (result == ReadResult.Missing)
                        {
                            continue;
                        }

                        if (result == ReadResult.UnsupportedFutureVersion)
                        {
                            Debug.LogWarning(
                                $"Tetherfall did not load {candidateNames[index]} because schema {sourceSchemaVersion} " +
                                $"is newer than supported schema {TetherfallSaveData.CurrentSchemaVersion}.");
                            data = null;
                            return false;
                        }

                        if (result == ReadResult.Invalid)
                        {
                            Debug.LogWarning(
                                $"Tetherfall ignored an invalid {candidateNames[index]}: {failureReason}");
                            continue;
                        }

                        if (index > 0)
                        {
                            Debug.LogWarning(
                                $"Tetherfall recovered the expedition record from {candidateNames[index]}.");
                        }

                        return true;
                    }

                    data = null;
                    return false;
                }
                catch (Exception exception) when (IsRecoverableException(exception))
                {
                    Debug.LogWarning(
                        $"Tetherfall could not load the expedition record: {DescribeException(exception)}");
                    data = null;
                    return false;
                }
            }
        }

        public static bool Save(TetherfallSaveData data)
        {
            if (data == null)
            {
                Debug.LogWarning("Tetherfall could not save a null expedition record.");
                return false;
            }

            lock (SaveLock)
            {
                try
                {
                    if (data.schemaVersion > TetherfallSaveData.CurrentSchemaVersion)
                    {
                        Debug.LogWarning(
                            $"Tetherfall refused to overwrite expedition schema {data.schemaVersion} " +
                            $"with older schema {TetherfallSaveData.CurrentSchemaVersion}.");
                        return false;
                    }

                    if (!TryGetSavePaths(
                            out var directoryPath,
                            out var savePath,
                            out var temporaryPath,
                            out var backupPath))
                    {
                        Debug.LogWarning("Tetherfall could not save because the persistent data path is unavailable.");
                        return false;
                    }

                    Directory.CreateDirectory(directoryPath);
                    Normalize(data);
                    var json = JsonUtility.ToJson(data, true);
                    using (var stream = new FileStream(
                               temporaryPath,
                               FileMode.Create,
                               FileAccess.Write,
                               FileShare.None))
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(json);
                        writer.Flush();
                        stream.Flush(true);
                    }

                    var temporaryResult = TryRead(
                        temporaryPath,
                        out _,
                        out _,
                        out var validationFailure);
                    if (temporaryResult != ReadResult.Valid)
                    {
                        throw new IOException(
                            $"The temporary expedition record failed validation: {validationFailure}");
                    }

                    if (File.Exists(savePath))
                    {
                        try
                        {
                            File.Replace(temporaryPath, savePath, backupPath, true);
                        }
                        catch (PlatformNotSupportedException)
                        {
                            ReplaceWithFallback(temporaryPath, savePath, backupPath);
                        }
                        catch (IOException)
                        {
                            ReplaceWithFallback(temporaryPath, savePath, backupPath);
                        }
                    }
                    else
                    {
                        File.Move(temporaryPath, savePath);
                    }

                    return true;
                }
                catch (Exception exception) when (IsRecoverableException(exception))
                {
                    Debug.LogWarning(
                        $"Tetherfall could not save the expedition record: {DescribeException(exception)}");
                    return false;
                }
            }
        }

        private static ReadResult TryRead(
            string path,
            out TetherfallSaveData data,
            out int sourceSchemaVersion,
            out string failureReason)
        {
            data = null;
            sourceSchemaVersion = 0;
            failureReason = string.Empty;
            if (!File.Exists(path))
            {
                return ReadResult.Missing;
            }

            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    failureReason = "the file is empty";
                    return ReadResult.Invalid;
                }

                var objectJson = json.Trim();
                if (objectJson.Length > 0 && objectJson[0] == '\uFEFF')
                {
                    objectJson = objectJson.Substring(1).TrimStart();
                }

                if (objectJson.Length < 2 ||
                    objectJson[0] != '{' ||
                    objectJson[objectJson.Length - 1] != '}')
                {
                    failureReason = "the root value is not a complete JSON object";
                    return ReadResult.Invalid;
                }

                var header = new SaveHeader();
                JsonUtility.FromJsonOverwrite(objectJson, header);
                sourceSchemaVersion = header.schemaVersion;
                if (sourceSchemaVersion < 1)
                {
                    failureReason = "schemaVersion is missing or invalid";
                    return ReadResult.Invalid;
                }

                if (sourceSchemaVersion > TetherfallSaveData.CurrentSchemaVersion)
                {
                    return ReadResult.UnsupportedFutureVersion;
                }

                var loadedData = new TetherfallSaveData();
                JsonUtility.FromJsonOverwrite(objectJson, loadedData);
                if (loadedData.schemaVersion != sourceSchemaVersion)
                {
                    failureReason = "schemaVersion could not be read consistently";
                    return ReadResult.Invalid;
                }

                Normalize(loadedData);
                data = loadedData;
                return ReadResult.Valid;
            }
            catch (Exception exception) when (IsRecoverableException(exception))
            {
                failureReason = DescribeException(exception);
                return ReadResult.Invalid;
            }
        }

        private static bool TryGetSavePaths(
            out string directoryPath,
            out string savePath,
            out string temporaryPath,
            out string backupPath)
        {
            directoryPath = Application.persistentDataPath;
            savePath = null;
            temporaryPath = null;
            backupPath = null;
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return false;
            }

            savePath = Path.Combine(directoryPath, SaveFileName);
            temporaryPath = Path.Combine(directoryPath, TemporaryFileName);
            backupPath = Path.Combine(directoryPath, BackupFileName);
            return true;
        }

        private static void ReplaceWithFallback(
            string temporaryPath,
            string savePath,
            string backupPath)
        {
            if (!File.Exists(savePath))
            {
                File.Move(temporaryPath, savePath);
                return;
            }

            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            // Both moves stay in the save directory. If the process stops between them,
            // TryLoad can recover the validated temporary record or the prior backup.
            File.Move(savePath, backupPath);
            try
            {
                File.Move(temporaryPath, savePath);
            }
            catch (Exception exception) when (IsRecoverableException(exception))
            {
                try
                {
                    if (!File.Exists(savePath) && File.Exists(backupPath))
                    {
                        File.Copy(backupPath, savePath);
                    }
                }
                catch (Exception restoreException) when (IsRecoverableException(restoreException))
                {
                    Debug.LogWarning(
                        "Tetherfall could not restore the primary expedition record after a failed " +
                        $"replacement; the backup remains available: {DescribeException(restoreException)}");
                }

                throw;
            }
        }

        private static void Normalize(TetherfallSaveData data)
        {
            data.resources ??= new List<ResourceAmount>();
            data.pactNames ??= new List<string>();
            data.recruitNames ??= new List<string>();
            data.discoveredRegions ??= new List<string>();
            data.unlockedEquipment ??= new List<string>();
            data.equippedEquipmentIds ??= new List<string>();
            data.firstClearedEncounterIds ??= new List<string>();
            data.completedRouteEventIds ??= new List<string>();
            data.routeEvents ??= new List<RouteEventSaveData>();
            data.recoveredFieldCacheIds ??= new List<string>();
            data.restorationSites ??= new List<RestorationSiteSaveData>();
            data.packedMeals ??= new List<MealStackSaveData>();
            data.knownRecipeIds ??= new List<string>();
            data.completedCampContractIds ??= new List<string>();
            data.resourceNodes ??= new List<ResourceNodeSaveData>();
            data.farmPlots ??= new List<FarmPlotSaveData>();
            data.trials ??= new List<TrialSaveData>();
            data.dungeons ??= new List<DungeonSaveData>();
            data.cityActivities ??= new List<CityActivitySaveData>();
            data.bounties ??= new List<BountySaveData>();
            data.unlockedThreadwayIds ??= new List<string>();
            data.quests ??= new List<QuestProgressData>();
            data.skillRanks ??= new List<SkillRankSaveData>();
            data.pactProgress ??= new List<PactProgressSaveData>();
            data.pactRoster ??= new PactRosterSaveData();
            var safeUtcTicks = DateTime.UtcNow.Ticks;
            data.playerSettlement ??=
                TetherfallSettlementPersistence.CreateDefault(safeUtcTicks);
            data.cinderwoldArc ??= new CinderwoldArcSaveData();

            RemoveNullEntries(data.resources);
            RemoveNullEntries(data.pactNames);
            RemoveNullEntries(data.recruitNames);
            RemoveNullEntries(data.discoveredRegions);
            RemoveNullEntries(data.unlockedEquipment);
            RemoveNullEntries(data.equippedEquipmentIds);
            RemoveNullEntries(data.firstClearedEncounterIds);
            RemoveNullEntries(data.completedRouteEventIds);
            RemoveNullEntries(data.routeEvents);
            RemoveNullEntries(data.recoveredFieldCacheIds);
            RemoveNullEntries(data.restorationSites);
            RemoveNullEntries(data.packedMeals);
            RemoveNullEntries(data.knownRecipeIds);
            RemoveNullEntries(data.completedCampContractIds);
            RemoveNullEntries(data.resourceNodes);
            RemoveNullEntries(data.farmPlots);
            RemoveNullEntries(data.trials);
            RemoveNullEntries(data.dungeons);
            RemoveNullEntries(data.cityActivities);
            RemoveNullEntries(data.bounties);
            RemoveNullEntries(data.unlockedThreadwayIds);
            RemoveNullEntries(data.quests);
            RemoveNullEntries(data.skillRanks);
            RemoveNullEntries(data.pactProgress);

            foreach (var trial in data.trials)
            {
                trial.dialStates ??= new int[0];
                trial.remainingEnemyIds ??= new List<string>();
                RemoveNullEntries(trial.remainingEnemyIds);
            }

            DungeonSaveNormalization.NormalizeInPlace(data.dungeons);

            data.activePactName ??= string.Empty;
            data.mountPactName ??= string.Empty;
            data.activeMealId ??= string.Empty;
            data.activeBountyId ??= string.Empty;
            data.cinderwoldArc.resolutionId ??= string.Empty;

            TetherfallSkillRules.NormalizeInPlace(
                data.skillRanks,
                TetherfallSkillRules.GetPointBudget(
                    data.level,
                    data.campTier,
                    data.settlementTier,
                    data.dungeonBossDefeated));
            PactProgressionRules.NormalizeInPlace(data.pactProgress, data.pactNames);
            PactRosterNormalization.NormalizeInPlace(
                data.pactRoster,
                Mathf.Max(1, data.level));
            TetherfallSettlementPersistence.NormalizeInPlace(
                data.playerSettlement,
                safeUtcTicks);

            if (!IsFinite(data.playerPosition))
            {
                data.playerPosition = Vector3.zero;
            }

            if (!IsFinite(data.campPosition))
            {
                data.campPosition = Vector3.zero;
            }

            data.schemaVersion = TetherfallSaveData.CurrentSchemaVersion;
        }

        private static void RemoveNullEntries<T>(List<T> items) where T : class
        {
            items.RemoveAll(item => item == null);
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) &&
                   !float.IsInfinity(value.x) &&
                   !float.IsNaN(value.y) &&
                   !float.IsInfinity(value.y) &&
                   !float.IsNaN(value.z) &&
                   !float.IsInfinity(value.z);
        }

        private static bool IsRecoverableException(Exception exception)
        {
            return exception is IOException ||
                   exception is UnauthorizedAccessException ||
                   exception is ArgumentException ||
                   exception is NotSupportedException ||
                   exception is System.Security.SecurityException ||
                   exception is UnityException;
        }

        private static string DescribeException(Exception exception)
        {
            return $"{exception.GetType().Name}: {exception.Message}";
        }
    }
}
