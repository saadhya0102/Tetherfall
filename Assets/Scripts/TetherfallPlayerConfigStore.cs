using System;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assembly-CSharp-Editor")]
#endif

namespace Tetherfall
{
    public enum TetherfallPlayerConfigStatus
    {
        NotAttempted,
        LoadedPrimary,
        LoadedBackup,
        AppliedDefaultsMissing,
        AppliedDefaultsInvalid,
        AppliedDefaultsAfterReadFailure,
        AppliedDefaultsUnsupportedVersion,
        Saved,
        ResetAndSaved,
        FailedPersistentDataPath,
        FailedRuntimeApply,
        FailedValidation,
        FailedRead,
        FailedWrite,
        RefusedUnsupportedFutureVersion
    }

    public readonly struct TetherfallPlayerConfigFileResult
    {
        internal TetherfallPlayerConfigFileResult(
            bool succeeded,
            TetherfallPlayerConfigStatus status,
            bool resetApplied = false)
        {
            Succeeded = succeeded;
            Status = status;
            ResetApplied = resetApplied;
        }

        public bool Attempted => Status != TetherfallPlayerConfigStatus.NotAttempted;
        public bool Succeeded { get; }
        public TetherfallPlayerConfigStatus Status { get; }
        public bool ResetApplied { get; }

        public bool RecoveredFromBackup =>
            Status == TetherfallPlayerConfigStatus.LoadedBackup;

        public bool UsedDefaults =>
            Status == TetherfallPlayerConfigStatus.AppliedDefaultsMissing ||
            Status == TetherfallPlayerConfigStatus.AppliedDefaultsInvalid ||
            Status == TetherfallPlayerConfigStatus.AppliedDefaultsAfterReadFailure ||
            Status == TetherfallPlayerConfigStatus.AppliedDefaultsUnsupportedVersion;

        internal TetherfallPlayerConfigFileResult WithResetApplied()
        {
            return new TetherfallPlayerConfigFileResult(
                Succeeded,
                Succeeded && Status == TetherfallPlayerConfigStatus.Saved
                    ? TetherfallPlayerConfigStatus.ResetAndSaved
                    : Status,
                true);
        }
    }

    public readonly struct TetherfallPlayerConfigResult
    {
        internal TetherfallPlayerConfigResult(
            TetherfallPlayerConfigFileResult bindings,
            TetherfallPlayerConfigFileResult accessibility)
        {
            Bindings = bindings;
            Accessibility = accessibility;
        }

        public TetherfallPlayerConfigFileResult Bindings { get; }
        public TetherfallPlayerConfigFileResult Accessibility { get; }

        public bool Succeeded =>
            (Bindings.Attempted || Accessibility.Attempted) &&
            (!Bindings.Attempted || Bindings.Succeeded) &&
            (!Accessibility.Attempted || Accessibility.Succeeded);
    }

    /// <summary>
    /// Persists player-owned controls and accessibility preferences separately
    /// from expedition progression. Call LoadAll once during startup, after the
    /// runtime statics have been registered and before gameplay reads them.
    /// </summary>
    public static class TetherfallPlayerConfigStore
    {
        private const string BindingsPrimaryFileName = "tetherfall-input-bindings.json";
        private const string BindingsTemporaryFileName = "tetherfall-input-bindings.tmp";
        private const string BindingsBackupFileName = "tetherfall-input-bindings.bak";
        private const string AccessibilityPrimaryFileName = "tetherfall-accessibility.json";
        private const string AccessibilityTemporaryFileName = "tetherfall-accessibility.tmp";
        private const string AccessibilityBackupFileName = "tetherfall-accessibility.bak";

        private static readonly object ConfigLock = new object();

        private enum ConfigKind
        {
            Bindings,
            Accessibility
        }

        private enum CandidateReadStatus
        {
            Missing,
            Valid,
            Invalid,
            Unreadable,
            UnsupportedFutureVersion
        }

        [Serializable]
        private sealed class BindingsHeader
        {
            public int version;
        }

        [Serializable]
        private sealed class AccessibilityHeader
        {
            public int schemaVersion;
        }

        private readonly struct ConfigPaths
        {
            public ConfigPaths(
                string directory,
                string primaryFileName,
                string temporaryFileName,
                string backupFileName)
            {
                Directory = directory;
                Primary = Path.Combine(directory, primaryFileName);
                Temporary = Path.Combine(directory, temporaryFileName);
                Backup = Path.Combine(directory, backupFileName);
            }

            public string Directory { get; }
            public string Primary { get; }
            public string Temporary { get; }
            public string Backup { get; }
        }

        private readonly struct Candidate<T> where T : class
        {
            public Candidate(
                CandidateReadStatus status,
                T value = null,
                string exceptionType = null)
            {
                Status = status;
                Value = value;
                ExceptionType = exceptionType ?? string.Empty;
            }

            public CandidateReadStatus Status { get; }
            public T Value { get; }
            public string ExceptionType { get; }
        }

        public static TetherfallPlayerConfigResult LoadAll()
        {
            try
            {
                return LoadAll(GetPersistentRoot());
            }
            catch (Exception exception)
            {
                WarnUnexpectedFailure("load player configuration", exception);
                return CreateUnavailableLoadResult(false);
            }
        }

        public static TetherfallPlayerConfigResult SaveBindings()
        {
            try
            {
                return SaveBindings(GetPersistentRoot());
            }
            catch (Exception exception)
            {
                WarnUnexpectedFailure("save input bindings", exception);
                return SingleBindingsResult(Failed(TetherfallPlayerConfigStatus.FailedWrite));
            }
        }

        public static TetherfallPlayerConfigResult SaveAccessibility()
        {
            try
            {
                return SaveAccessibility(GetPersistentRoot());
            }
            catch (Exception exception)
            {
                WarnUnexpectedFailure("save accessibility settings", exception);
                return SingleAccessibilityResult(Failed(TetherfallPlayerConfigStatus.FailedWrite));
            }
        }

        public static TetherfallPlayerConfigResult SaveAll()
        {
            try
            {
                return SaveAll(GetPersistentRoot());
            }
            catch (Exception exception)
            {
                WarnUnexpectedFailure("save player configuration", exception);
                var failure = Failed(TetherfallPlayerConfigStatus.FailedWrite);
                return new TetherfallPlayerConfigResult(failure, failure);
            }
        }

        public static TetherfallPlayerConfigResult ResetBindingsAndSave()
        {
            try
            {
                return ResetBindingsAndSave(GetPersistentRoot());
            }
            catch (Exception exception)
            {
                WarnUnexpectedFailure("reset input bindings", exception);
                return SingleBindingsResult(Failed(TetherfallPlayerConfigStatus.FailedWrite));
            }
        }

        public static TetherfallPlayerConfigResult ResetAccessibilityAndSave()
        {
            try
            {
                return ResetAccessibilityAndSave(GetPersistentRoot());
            }
            catch (Exception exception)
            {
                WarnUnexpectedFailure("reset accessibility settings", exception);
                return SingleAccessibilityResult(Failed(TetherfallPlayerConfigStatus.FailedWrite));
            }
        }

        // Editor verification uses these overloads to stay out of the user's
        // real persistent-data directory. They remain internal in player code.
        internal static TetherfallPlayerConfigResult LoadAll(string rootPath)
        {
            lock (ConfigLock)
            {
                if (!IsUsableRoot(rootPath))
                {
                    return CreateUnavailableLoadResult(true);
                }

                return new TetherfallPlayerConfigResult(
                    LoadBindingsCore(rootPath),
                    LoadAccessibilityCore(rootPath));
            }
        }

        internal static TetherfallPlayerConfigResult SaveBindings(string rootPath)
        {
            lock (ConfigLock)
            {
                if (!IsUsableRoot(rootPath))
                {
                    WarnUnavailableRoot("save input bindings");
                    return SingleBindingsResult(
                        Failed(TetherfallPlayerConfigStatus.FailedPersistentDataPath));
                }

                return SingleBindingsResult(SaveBindingsCore(rootPath));
            }
        }

        internal static TetherfallPlayerConfigResult SaveAccessibility(string rootPath)
        {
            lock (ConfigLock)
            {
                if (!IsUsableRoot(rootPath))
                {
                    WarnUnavailableRoot("save accessibility settings");
                    return SingleAccessibilityResult(
                        Failed(TetherfallPlayerConfigStatus.FailedPersistentDataPath));
                }

                return SingleAccessibilityResult(SaveAccessibilityCore(rootPath));
            }
        }

        internal static TetherfallPlayerConfigResult SaveAll(string rootPath)
        {
            lock (ConfigLock)
            {
                if (!IsUsableRoot(rootPath))
                {
                    WarnUnavailableRoot("save player configuration");
                    var failure = Failed(TetherfallPlayerConfigStatus.FailedPersistentDataPath);
                    return new TetherfallPlayerConfigResult(failure, failure);
                }

                return new TetherfallPlayerConfigResult(
                    SaveBindingsCore(rootPath),
                    SaveAccessibilityCore(rootPath));
            }
        }

        internal static TetherfallPlayerConfigResult ResetBindingsAndSave(string rootPath)
        {
            lock (ConfigLock)
            {
                if (!TryResetBindings())
                {
                    Debug.LogWarning("Tetherfall could not apply default input bindings.");
                    return SingleBindingsResult(
                        Failed(TetherfallPlayerConfigStatus.FailedRuntimeApply));
                }

                if (!IsUsableRoot(rootPath))
                {
                    WarnUnavailableRoot("save reset input bindings");
                    return SingleBindingsResult(
                        new TetherfallPlayerConfigFileResult(
                            false,
                            TetherfallPlayerConfigStatus.FailedPersistentDataPath,
                            true));
                }

                return SingleBindingsResult(
                    SaveBindingsCore(rootPath).WithResetApplied());
            }
        }

        internal static TetherfallPlayerConfigResult ResetAccessibilityAndSave(string rootPath)
        {
            lock (ConfigLock)
            {
                if (!TryApplyAccessibilityDefaults())
                {
                    Debug.LogWarning("Tetherfall could not apply default accessibility settings.");
                    return SingleAccessibilityResult(
                        Failed(TetherfallPlayerConfigStatus.FailedRuntimeApply));
                }

                if (!IsUsableRoot(rootPath))
                {
                    WarnUnavailableRoot("save reset accessibility settings");
                    return SingleAccessibilityResult(
                        new TetherfallPlayerConfigFileResult(
                            false,
                            TetherfallPlayerConfigStatus.FailedPersistentDataPath,
                            true));
                }

                return SingleAccessibilityResult(
                    SaveAccessibilityCore(rootPath).WithResetApplied());
            }
        }

        private static TetherfallPlayerConfigFileResult LoadBindingsCore(string rootPath)
        {
            try
            {
                var paths = GetPaths(rootPath, ConfigKind.Bindings);
                var candidates = new[] { paths.Primary, paths.Backup };
                var sourceNames = new[] { "primary", "backup" };
                var sawInvalid = false;
                var sawReadFailure = false;

                for (var index = 0; index < candidates.Length; index++)
                {
                    var candidate = ReadBindings(candidates[index]);
                    switch (candidate.Status)
                    {
                        case CandidateReadStatus.Missing:
                            continue;

                        case CandidateReadStatus.Valid:
                            if (TryApplyBindings(candidate.Value))
                            {
                                return Succeeded(
                                    index == 0
                                        ? TetherfallPlayerConfigStatus.LoadedPrimary
                                        : TetherfallPlayerConfigStatus.LoadedBackup);
                            }

                            sawInvalid = true;
                            Debug.LogWarning(
                                "Tetherfall could not apply the input bindings " +
                                sourceNames[index] + "; the next fallback will be tried.");
                            continue;

                        case CandidateReadStatus.UnsupportedFutureVersion:
                            TryResetBindings();
                            WarnUnsupportedLoad("input bindings", sourceNames[index]);
                            return Failed(
                                TetherfallPlayerConfigStatus.AppliedDefaultsUnsupportedVersion);

                        case CandidateReadStatus.Unreadable:
                            sawReadFailure = true;
                            WarnUnreadableLoad(
                                "input bindings",
                                sourceNames[index],
                                candidate.ExceptionType);
                            continue;

                        default:
                            sawInvalid = true;
                            WarnInvalidLoad("input bindings", sourceNames[index]);
                            continue;
                    }
                }

                if (!TryResetBindings())
                {
                    Debug.LogWarning("Tetherfall could not apply default input bindings.");
                    return Failed(TetherfallPlayerConfigStatus.FailedRuntimeApply);
                }

                if (sawReadFailure)
                {
                    return Failed(
                        TetherfallPlayerConfigStatus.AppliedDefaultsAfterReadFailure);
                }

                return Succeeded(
                    sawInvalid
                        ? TetherfallPlayerConfigStatus.AppliedDefaultsInvalid
                        : TetherfallPlayerConfigStatus.AppliedDefaultsMissing);
            }
            catch (Exception exception)
            {
                TryResetBindings();
                WarnUnexpectedFailure("load input bindings", exception);
                return Failed(TetherfallPlayerConfigStatus.AppliedDefaultsAfterReadFailure);
            }
        }

        private static TetherfallPlayerConfigFileResult LoadAccessibilityCore(string rootPath)
        {
            try
            {
                var paths = GetPaths(rootPath, ConfigKind.Accessibility);
                var candidates = new[] { paths.Primary, paths.Backup };
                var sourceNames = new[] { "primary", "backup" };
                var sawInvalid = false;
                var sawReadFailure = false;

                for (var index = 0; index < candidates.Length; index++)
                {
                    var candidate = ReadAccessibility(candidates[index]);
                    switch (candidate.Status)
                    {
                        case CandidateReadStatus.Missing:
                            continue;

                        case CandidateReadStatus.Valid:
                            if (TryApplyAccessibility(candidate.Value))
                            {
                                return Succeeded(
                                    index == 0
                                        ? TetherfallPlayerConfigStatus.LoadedPrimary
                                        : TetherfallPlayerConfigStatus.LoadedBackup);
                            }

                            sawInvalid = true;
                            Debug.LogWarning(
                                "Tetherfall could not apply the accessibility settings " +
                                sourceNames[index] + "; the next fallback will be tried.");
                            continue;

                        case CandidateReadStatus.UnsupportedFutureVersion:
                            TryApplyAccessibilityDefaults();
                            WarnUnsupportedLoad("accessibility settings", sourceNames[index]);
                            return Failed(
                                TetherfallPlayerConfigStatus.AppliedDefaultsUnsupportedVersion);

                        case CandidateReadStatus.Unreadable:
                            sawReadFailure = true;
                            WarnUnreadableLoad(
                                "accessibility settings",
                                sourceNames[index],
                                candidate.ExceptionType);
                            continue;

                        default:
                            sawInvalid = true;
                            WarnInvalidLoad("accessibility settings", sourceNames[index]);
                            continue;
                    }
                }

                if (!TryApplyAccessibilityDefaults())
                {
                    Debug.LogWarning("Tetherfall could not apply default accessibility settings.");
                    return Failed(TetherfallPlayerConfigStatus.FailedRuntimeApply);
                }

                if (sawReadFailure)
                {
                    return Failed(
                        TetherfallPlayerConfigStatus.AppliedDefaultsAfterReadFailure);
                }

                return Succeeded(
                    sawInvalid
                        ? TetherfallPlayerConfigStatus.AppliedDefaultsInvalid
                        : TetherfallPlayerConfigStatus.AppliedDefaultsMissing);
            }
            catch (Exception exception)
            {
                TryApplyAccessibilityDefaults();
                WarnUnexpectedFailure("load accessibility settings", exception);
                return Failed(TetherfallPlayerConfigStatus.AppliedDefaultsAfterReadFailure);
            }
        }

        private static TetherfallPlayerConfigFileResult SaveBindingsCore(string rootPath)
        {
            try
            {
                var paths = GetPaths(rootPath, ConfigKind.Bindings);
                if (!TryInspectForWrite(
                        ConfigKind.Bindings,
                        paths,
                        true,
                        out _,
                        out var inspectionFailure))
                {
                    return inspectionFailure;
                }

                var json = TetherfallInputBindings.Shared.ToJson(true);
                if (ParseBindingsJson(json).Status != CandidateReadStatus.Valid)
                {
                    Debug.LogWarning(
                        "Tetherfall did not save input bindings because generated data failed validation.");
                    return Failed(TetherfallPlayerConfigStatus.FailedValidation);
                }

                Directory.CreateDirectory(paths.Directory);
                WriteTemporary(paths.Temporary, json);
                var temporary = ReadBindings(paths.Temporary);
                if (temporary.Status != CandidateReadStatus.Valid)
                {
                    TryDelete(paths.Temporary);
                    WarnTemporaryValidationFailure("input bindings", temporary);
                    return Failed(TetherfallPlayerConfigStatus.FailedValidation);
                }

                if (!TryInspectForWrite(
                        ConfigKind.Bindings,
                        paths,
                        false,
                        out var primaryStatus,
                        out inspectionFailure))
                {
                    return inspectionFailure;
                }

                PromoteTemporary(paths, primaryStatus, json);
                return Succeeded(TetherfallPlayerConfigStatus.Saved);
            }
            catch (Exception exception)
            {
                WarnSaveFailure("input bindings", exception);
                return Failed(TetherfallPlayerConfigStatus.FailedWrite);
            }
        }

        private static TetherfallPlayerConfigFileResult SaveAccessibilityCore(string rootPath)
        {
            try
            {
                var paths = GetPaths(rootPath, ConfigKind.Accessibility);
                if (!TryInspectForWrite(
                        ConfigKind.Accessibility,
                        paths,
                        true,
                        out _,
                        out var inspectionFailure))
                {
                    return inspectionFailure;
                }

                var json = TetherfallAccessibilityRuntime.SerializeCurrent(true);
                if (ParseAccessibilityJson(json).Status != CandidateReadStatus.Valid)
                {
                    Debug.LogWarning(
                        "Tetherfall did not save accessibility settings because generated data failed validation.");
                    return Failed(TetherfallPlayerConfigStatus.FailedValidation);
                }

                Directory.CreateDirectory(paths.Directory);
                WriteTemporary(paths.Temporary, json);
                var temporary = ReadAccessibility(paths.Temporary);
                if (temporary.Status != CandidateReadStatus.Valid)
                {
                    TryDelete(paths.Temporary);
                    WarnTemporaryValidationFailure("accessibility settings", temporary);
                    return Failed(TetherfallPlayerConfigStatus.FailedValidation);
                }

                if (!TryInspectForWrite(
                        ConfigKind.Accessibility,
                        paths,
                        false,
                        out var primaryStatus,
                        out inspectionFailure))
                {
                    return inspectionFailure;
                }

                PromoteTemporary(paths, primaryStatus, json);
                return Succeeded(TetherfallPlayerConfigStatus.Saved);
            }
            catch (Exception exception)
            {
                WarnSaveFailure("accessibility settings", exception);
                return Failed(TetherfallPlayerConfigStatus.FailedWrite);
            }
        }

        private static Candidate<TetherfallInputBindingData> ReadBindings(string path)
        {
            if (!File.Exists(path))
            {
                return new Candidate<TetherfallInputBindingData>(
                    CandidateReadStatus.Missing);
            }

            try
            {
                return ParseBindingsJson(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                return new Candidate<TetherfallInputBindingData>(
                    CandidateReadStatus.Unreadable,
                    exceptionType: ExceptionType(exception));
            }
        }

        private static Candidate<TetherfallAccessibilitySettings> ReadAccessibility(string path)
        {
            if (!File.Exists(path))
            {
                return new Candidate<TetherfallAccessibilitySettings>(
                    CandidateReadStatus.Missing);
            }

            try
            {
                return ParseAccessibilityJson(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                return new Candidate<TetherfallAccessibilitySettings>(
                    CandidateReadStatus.Unreadable,
                    exceptionType: ExceptionType(exception));
            }
        }

        private static Candidate<TetherfallInputBindingData> ParseBindingsJson(string json)
        {
            if (!TryGetCompleteJsonObject(json, out var objectJson))
            {
                return new Candidate<TetherfallInputBindingData>(
                    CandidateReadStatus.Invalid);
            }

            try
            {
                var header = new BindingsHeader();
                JsonUtility.FromJsonOverwrite(objectJson, header);
                if (header.version < 1)
                {
                    return new Candidate<TetherfallInputBindingData>(
                        CandidateReadStatus.Invalid);
                }

                if (header.version > TetherfallInputBindings.CurrentSchemaVersion)
                {
                    return new Candidate<TetherfallInputBindingData>(
                        CandidateReadStatus.UnsupportedFutureVersion);
                }

                var temporaryBindings = new TetherfallInputBindings();
                if (!temporaryBindings.TryLoadJson(objectJson, out _))
                {
                    return new Candidate<TetherfallInputBindingData>(
                        CandidateReadStatus.Invalid);
                }

                return new Candidate<TetherfallInputBindingData>(
                    CandidateReadStatus.Valid,
                    temporaryBindings.ExportData());
            }
            catch (Exception)
            {
                return new Candidate<TetherfallInputBindingData>(
                    CandidateReadStatus.Invalid);
            }
        }

        private static Candidate<TetherfallAccessibilitySettings> ParseAccessibilityJson(
            string json)
        {
            if (!TryGetCompleteJsonObject(json, out var objectJson))
            {
                return new Candidate<TetherfallAccessibilitySettings>(
                    CandidateReadStatus.Invalid);
            }

            try
            {
                var header = new AccessibilityHeader();
                JsonUtility.FromJsonOverwrite(objectJson, header);
                if (header.schemaVersion < 1)
                {
                    return new Candidate<TetherfallAccessibilitySettings>(
                        CandidateReadStatus.Invalid);
                }

                if (header.schemaVersion >
                    TetherfallAccessibilitySettings.CurrentSchemaVersion)
                {
                    return new Candidate<TetherfallAccessibilitySettings>(
                        CandidateReadStatus.UnsupportedFutureVersion);
                }

                var temporarySettings =
                    TetherfallAccessibilityRuntime.CreateDefaultSettings();
                JsonUtility.FromJsonOverwrite(objectJson, temporarySettings);
                if (temporarySettings.schemaVersion != header.schemaVersion)
                {
                    return new Candidate<TetherfallAccessibilitySettings>(
                        CandidateReadStatus.Invalid);
                }

                temporarySettings.Sanitize();
                return new Candidate<TetherfallAccessibilitySettings>(
                    CandidateReadStatus.Valid,
                    temporarySettings);
            }
            catch (Exception)
            {
                return new Candidate<TetherfallAccessibilitySettings>(
                    CandidateReadStatus.Invalid);
            }
        }

        private static bool TryInspectForWrite(
            ConfigKind kind,
            ConfigPaths paths,
            bool includeTemporary,
            out CandidateReadStatus primaryStatus,
            out TetherfallPlayerConfigFileResult failure)
        {
            primaryStatus = CandidateReadStatus.Missing;
            failure = default;

            var sourcePaths = includeTemporary
                ? new[] { paths.Primary, paths.Backup, paths.Temporary }
                : new[] { paths.Primary, paths.Backup };
            var sourceNames = includeTemporary
                ? new[] { "primary", "backup", "temporary file" }
                : new[] { "primary", "backup" };

            for (var index = 0; index < sourcePaths.Length; index++)
            {
                var status = Inspect(kind, sourcePaths[index], out var exceptionType);
                if (index == 0)
                {
                    primaryStatus = status;
                }

                if (status == CandidateReadStatus.UnsupportedFutureVersion)
                {
                    Debug.LogWarning(
                        "Tetherfall refused to overwrite the " + ConfigLabel(kind) + " " +
                        sourceNames[index] +
                        " because it belongs to a newer version of the game.");
                    failure = Failed(
                        TetherfallPlayerConfigStatus.RefusedUnsupportedFutureVersion);
                    return false;
                }

                if (status == CandidateReadStatus.Unreadable)
                {
                    Debug.LogWarning(
                        "Tetherfall could not inspect the " + ConfigLabel(kind) + " " +
                        sourceNames[index] + " before saving (" +
                        SafeExceptionType(exceptionType) + ").");
                    failure = Failed(TetherfallPlayerConfigStatus.FailedRead);
                    return false;
                }
            }

            return true;
        }

        private static CandidateReadStatus Inspect(
            ConfigKind kind,
            string path,
            out string exceptionType)
        {
            if (kind == ConfigKind.Bindings)
            {
                var candidate = ReadBindings(path);
                exceptionType = candidate.ExceptionType;
                return candidate.Status;
            }

            var accessibility = ReadAccessibility(path);
            exceptionType = accessibility.ExceptionType;
            return accessibility.Status;
        }

        private static void WriteTemporary(string path, string json)
        {
            using (var stream = new FileStream(
                       path,
                       FileMode.Create,
                       FileAccess.Write,
                       FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(true);
            }
        }

        private static void PromoteTemporary(
            ConfigPaths paths,
            CandidateReadStatus primaryStatus,
            string expectedJson)
        {
            if (primaryStatus == CandidateReadStatus.Missing)
            {
                File.Move(paths.Temporary, paths.Primary);
                return;
            }

            if (primaryStatus != CandidateReadStatus.Valid)
            {
                ReplaceInvalidPrimary(paths);
                return;
            }

            try
            {
                File.Replace(paths.Temporary, paths.Primary, paths.Backup, true);
            }
            catch (PlatformNotSupportedException)
            {
                if (ReplacementAlreadyCompleted(paths, expectedJson))
                {
                    return;
                }

                ReplaceValidPrimaryWithFallback(paths);
            }
            catch (IOException)
            {
                if (ReplacementAlreadyCompleted(paths, expectedJson))
                {
                    return;
                }

                ReplaceValidPrimaryWithFallback(paths);
            }
        }

        private static void ReplaceInvalidPrimary(ConfigPaths paths)
        {
            File.Delete(paths.Primary);
            try
            {
                File.Move(paths.Temporary, paths.Primary);
            }
            catch (Exception)
            {
                RestoreBackupToPrimary(paths);
                throw;
            }
        }

        private static void ReplaceValidPrimaryWithFallback(ConfigPaths paths)
        {
            if (File.Exists(paths.Backup))
            {
                File.Delete(paths.Backup);
            }

            File.Move(paths.Primary, paths.Backup);
            try
            {
                File.Move(paths.Temporary, paths.Primary);
            }
            catch (Exception)
            {
                RestoreBackupToPrimary(paths);
                throw;
            }
        }

        private static void RestoreBackupToPrimary(ConfigPaths paths)
        {
            if (File.Exists(paths.Primary) || !File.Exists(paths.Backup))
            {
                return;
            }

            try
            {
                File.Copy(paths.Backup, paths.Primary);
                return;
            }
            catch (Exception)
            {
                // A same-volume move can still restore the primary when a copy
                // is blocked by free-space or platform behavior.
            }

            try
            {
                File.Move(paths.Backup, paths.Primary);
            }
            catch (Exception)
            {
                // The valid prior record remains at the backup path and will be
                // available to the next load.
            }
        }

        private static bool ReplacementAlreadyCompleted(
            ConfigPaths paths,
            string expectedJson)
        {
            if (File.Exists(paths.Temporary) || !File.Exists(paths.Primary))
            {
                return false;
            }

            try
            {
                return string.Equals(
                    File.ReadAllText(paths.Primary),
                    expectedJson,
                    StringComparison.Ordinal);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryGetCompleteJsonObject(
            string json,
            out string objectJson)
        {
            objectJson = string.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            objectJson = json.Trim();
            if (objectJson.Length > 0 && objectJson[0] == '\uFEFF')
            {
                objectJson = objectJson.Substring(1).TrimStart();
            }

            return objectJson.Length >= 2 &&
                   objectJson[0] == '{' &&
                   objectJson[objectJson.Length - 1] == '}';
        }

        private static bool TryApplyBindings(TetherfallInputBindingData data)
        {
            try
            {
                return TetherfallInputBindings.Shared.TryApplyData(
                    data,
                    false,
                    out _);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryResetBindings()
        {
            try
            {
                TetherfallInputBindings.Shared.ResetToDefaults();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryApplyAccessibility(
            TetherfallAccessibilitySettings settings)
        {
            try
            {
                return TetherfallAccessibilityRuntime.Apply(settings);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryApplyAccessibilityDefaults()
        {
            try
            {
                return TetherfallAccessibilityRuntime.Apply(
                    TetherfallAccessibilityRuntime.CreateDefaultSettings());
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static ConfigPaths GetPaths(string rootPath, ConfigKind kind)
        {
            return kind == ConfigKind.Bindings
                ? new ConfigPaths(
                    rootPath,
                    BindingsPrimaryFileName,
                    BindingsTemporaryFileName,
                    BindingsBackupFileName)
                : new ConfigPaths(
                    rootPath,
                    AccessibilityPrimaryFileName,
                    AccessibilityTemporaryFileName,
                    AccessibilityBackupFileName);
        }

        private static string GetPersistentRoot()
        {
            try
            {
                return Application.persistentDataPath;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool IsUsableRoot(string rootPath)
        {
            return !string.IsNullOrWhiteSpace(rootPath);
        }

        private static TetherfallPlayerConfigResult CreateUnavailableLoadResult(
            bool logWarning)
        {
            var bindingsApplied = TryResetBindings();
            var accessibilityApplied = TryApplyAccessibilityDefaults();
            if (logWarning)
            {
                WarnUnavailableRoot("load player configuration");
            }

            return new TetherfallPlayerConfigResult(
                new TetherfallPlayerConfigFileResult(
                    false,
                    bindingsApplied
                        ? TetherfallPlayerConfigStatus.FailedPersistentDataPath
                        : TetherfallPlayerConfigStatus.FailedRuntimeApply),
                new TetherfallPlayerConfigFileResult(
                    false,
                    accessibilityApplied
                        ? TetherfallPlayerConfigStatus.FailedPersistentDataPath
                        : TetherfallPlayerConfigStatus.FailedRuntimeApply));
        }

        private static TetherfallPlayerConfigResult SingleBindingsResult(
            TetherfallPlayerConfigFileResult bindings)
        {
            return new TetherfallPlayerConfigResult(bindings, default);
        }

        private static TetherfallPlayerConfigResult SingleAccessibilityResult(
            TetherfallPlayerConfigFileResult accessibility)
        {
            return new TetherfallPlayerConfigResult(default, accessibility);
        }

        private static TetherfallPlayerConfigFileResult Succeeded(
            TetherfallPlayerConfigStatus status)
        {
            return new TetherfallPlayerConfigFileResult(true, status);
        }

        private static TetherfallPlayerConfigFileResult Failed(
            TetherfallPlayerConfigStatus status)
        {
            return new TetherfallPlayerConfigFileResult(false, status);
        }

        private static string ConfigLabel(ConfigKind kind)
        {
            return kind == ConfigKind.Bindings
                ? "input bindings"
                : "accessibility settings";
        }

        private static void WarnInvalidLoad(string label, string source)
        {
            Debug.LogWarning(
                "Tetherfall ignored the invalid " + label + " " + source + ".");
        }

        private static void WarnUnreadableLoad(
            string label,
            string source,
            string exceptionType)
        {
            Debug.LogWarning(
                "Tetherfall could not read the " + label + " " + source + " (" +
                SafeExceptionType(exceptionType) + "); the next fallback will be tried.");
        }

        private static void WarnUnsupportedLoad(string label, string source)
        {
            Debug.LogWarning(
                "Tetherfall did not load the " + label + " " + source +
                " because it belongs to a newer version of the game; defaults were " +
                "applied without changing the file.");
        }

        private static void WarnTemporaryValidationFailure<T>(
            string label,
            Candidate<T> candidate) where T : class
        {
            var suffix = candidate.Status == CandidateReadStatus.Unreadable
                ? " (" + SafeExceptionType(candidate.ExceptionType) + ")"
                : string.Empty;
            Debug.LogWarning(
                "Tetherfall did not save " + label +
                " because the temporary file failed validation" + suffix + ".");
        }

        private static void WarnSaveFailure(string label, Exception exception)
        {
            Debug.LogWarning(
                "Tetherfall could not save " + label + " (" +
                ExceptionType(exception) + ").");
        }

        private static void WarnUnexpectedFailure(string operation, Exception exception)
        {
            Debug.LogWarning(
                "Tetherfall could not " + operation + " (" +
                ExceptionType(exception) + ").");
        }

        private static void WarnUnavailableRoot(string operation)
        {
            Debug.LogWarning(
                "Tetherfall could not " + operation +
                " because the persistent data path is unavailable.");
        }

        private static string ExceptionType(Exception exception)
        {
            return exception == null
                ? "UnknownError"
                : exception.GetType().Name;
        }

        private static string SafeExceptionType(string exceptionType)
        {
            return string.IsNullOrWhiteSpace(exceptionType)
                ? "UnknownError"
                : exceptionType;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
                // The primary has not been touched; a stale temporary file is
                // harmless and will be validated before a future overwrite.
            }
        }
    }
}
