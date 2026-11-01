using System;
using System.IO;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallPlayerConfigVerification
    {
        private const string MenuPath = "Tetherfall/Verify Player Configuration Persistence";
        private const string BindingsPrimaryFileName = "tetherfall-input-bindings.json";
        private const string BindingsBackupFileName = "tetherfall-input-bindings.bak";
        private const string AccessibilityPrimaryFileName = "tetherfall-accessibility.json";
        private const string AccessibilityBackupFileName = "tetherfall-accessibility.bak";

        private static readonly string[] ExpeditionFileNames =
        {
            "tetherfall-expedition.json",
            "tetherfall-expedition.tmp",
            "tetherfall-expedition.bak"
        };

        [MenuItem(MenuPath, false, 2215)]
        public static void VerifyPlayerConfigPersistence()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Player-configuration verification is edit-mode only. Stop Play Mode and run it again.");
            }

            var originalBindings = TetherfallInputBindings.Shared.ExportData();
            var originalAccessibility = TetherfallAccessibilityRuntime.GetSnapshot();
            var verificationRoot = Path.Combine(
                Path.GetTempPath(),
                "tetherfall-player-config-verification-" + Guid.NewGuid().ToString("N"));
            var assertionCount = 0;

            try
            {
                Directory.CreateDirectory(verificationRoot);
                var expeditionSentinels = SeedExpeditionFiles(verificationRoot);

                VerifyValidRoundTrip(verificationRoot, ref assertionCount);
                VerifyResetSaves(verificationRoot, ref assertionCount);
                VerifyExpeditionIsolation(
                    verificationRoot,
                    expeditionSentinels,
                    ref assertionCount);

                VerifyCorruptedPrimaryBackupRecovery(
                    Path.Combine(verificationRoot, "backup-recovery"),
                    ref assertionCount);
                VerifyInvalidJsonFallback(
                    Path.Combine(verificationRoot, "invalid-json"),
                    ref assertionCount);
                VerifyFutureVersionSafety(
                    Path.Combine(verificationRoot, "future-version"),
                    ref assertionCount);

                Debug.Log(
                    "Tetherfall player-configuration persistence verification passed: " +
                    assertionCount +
                    " assertions covered round-trip saves, backup recovery, invalid-data " +
                    "defaults, future-version refusal, reset saves, and expedition isolation.");
            }
            finally
            {
                RestoreRuntimeState(originalBindings, originalAccessibility);
                TryDeleteDirectory(verificationRoot);
            }
        }

        [MenuItem(MenuPath, true, 2215)]
        private static bool CanVerifyPlayerConfigPersistence()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void VerifyValidRoundTrip(string root, ref int assertionCount)
        {
            TetherfallInputBindings.Shared.ResetToDefaults();
            SetToggleControlsBinding(KeyCode.F12);

            var customAccessibility =
                TetherfallAccessibilityRuntime.CreateDefaultSettings();
            customAccessibility.cameraShakeIntensity = 0.35f;
            customAccessibility.hudScale = 1.4f;
            customAccessibility.highContrastTelegraphs = true;
            customAccessibility.colorCuePalette =
                TetherfallAccessibilityColorCuePalette.Grayscale;
            Assert(
                TetherfallAccessibilityRuntime.Apply(customAccessibility),
                "Round-trip accessibility settings could not be applied.",
                ref assertionCount);

            var save = TetherfallPlayerConfigStore.SaveAll(root);
            Assert(save.Succeeded, "The valid player configuration did not save.", ref assertionCount);
            Assert(
                save.Bindings.Status == TetherfallPlayerConfigStatus.Saved,
                "Input bindings did not report a successful save.",
                ref assertionCount);
            Assert(
                save.Accessibility.Status == TetherfallPlayerConfigStatus.Saved,
                "Accessibility settings did not report a successful save.",
                ref assertionCount);
            Assert(
                File.Exists(Path.Combine(root, BindingsPrimaryFileName)),
                "The input-bindings primary file was not created.",
                ref assertionCount);
            Assert(
                File.Exists(Path.Combine(root, AccessibilityPrimaryFileName)),
                "The accessibility primary file was not created.",
                ref assertionCount);

            TetherfallInputBindings.Shared.ResetToDefaults();
            Assert(
                TetherfallAccessibilityRuntime.Apply(
                    TetherfallAccessibilityRuntime.CreateDefaultSettings()),
                "Round-trip defaults could not be staged before loading.",
                ref assertionCount);

            var load = TetherfallPlayerConfigStore.LoadAll(root);
            Assert(load.Succeeded, "The valid player configuration did not load.", ref assertionCount);
            Assert(
                load.Bindings.Status == TetherfallPlayerConfigStatus.LoadedPrimary,
                "Input bindings were not loaded from the primary file.",
                ref assertionCount);
            Assert(
                load.Accessibility.Status == TetherfallPlayerConfigStatus.LoadedPrimary,
                "Accessibility settings were not loaded from the primary file.",
                ref assertionCount);
            AssertToggleControlsBinding(KeyCode.F12, ref assertionCount);
            Assert(
                Mathf.Approximately(TetherfallAccessibilityRuntime.Current.cameraShakeIntensity, 0.35f),
                "Camera-shake intensity did not survive a valid round trip.",
                ref assertionCount);
            Assert(
                Mathf.Approximately(TetherfallAccessibilityRuntime.Current.hudScale, 1.4f),
                "HUD scale did not survive a valid round trip.",
                ref assertionCount);
            Assert(
                TetherfallAccessibilityRuntime.Current.highContrastTelegraphs,
                "High-contrast telegraphs did not survive a valid round trip.",
                ref assertionCount);
            Assert(
                TetherfallAccessibilityRuntime.Current.colorCuePalette ==
                TetherfallAccessibilityColorCuePalette.Grayscale,
                "The color-cue palette did not survive a valid round trip.",
                ref assertionCount);
        }

        private static void VerifyResetSaves(string root, ref int assertionCount)
        {
            SetToggleControlsBinding(KeyCode.F11);
            var bindingsReset = TetherfallPlayerConfigStore.ResetBindingsAndSave(root);
            Assert(
                bindingsReset.Succeeded &&
                bindingsReset.Bindings.ResetApplied &&
                bindingsReset.Bindings.Status == TetherfallPlayerConfigStatus.ResetAndSaved,
                "ResetBindingsAndSave did not report both reset and persistence success.",
                ref assertionCount);
            AssertToggleControlsBinding(KeyCode.H, ref assertionCount);

            var customAccessibility =
                TetherfallAccessibilityRuntime.CreateDefaultSettings();
            customAccessibility.hudScale = 1.8f;
            Assert(
                TetherfallAccessibilityRuntime.Apply(customAccessibility),
                "Reset verification accessibility settings could not be staged.",
                ref assertionCount);

            var accessibilityReset =
                TetherfallPlayerConfigStore.ResetAccessibilityAndSave(root);
            Assert(
                accessibilityReset.Succeeded &&
                accessibilityReset.Accessibility.ResetApplied &&
                accessibilityReset.Accessibility.Status ==
                TetherfallPlayerConfigStatus.ResetAndSaved,
                "ResetAccessibilityAndSave did not report both reset and persistence success.",
                ref assertionCount);
            Assert(
                Mathf.Approximately(TetherfallAccessibilityRuntime.Current.hudScale, 1f),
                "ResetAccessibilityAndSave did not apply the default HUD scale.",
                ref assertionCount);
        }

        private static void VerifyCorruptedPrimaryBackupRecovery(
            string root,
            ref int assertionCount)
        {
            Directory.CreateDirectory(root);

            TetherfallInputBindings.Shared.ResetToDefaults();
            SetToggleControlsBinding(KeyCode.F10);
            ApplyHudScale(1.25f);
            var firstSave = TetherfallPlayerConfigStore.SaveAll(root);
            Assert(firstSave.Succeeded, "The backup fixture's first save failed.", ref assertionCount);

            SetToggleControlsBinding(KeyCode.F11);
            ApplyHudScale(1.75f);
            var secondSave = TetherfallPlayerConfigStore.SaveAll(root);
            Assert(secondSave.Succeeded, "The backup fixture's second save failed.", ref assertionCount);
            Assert(
                File.Exists(Path.Combine(root, BindingsBackupFileName)),
                "The second input-bindings save did not create a backup.",
                ref assertionCount);
            Assert(
                File.Exists(Path.Combine(root, AccessibilityBackupFileName)),
                "The second accessibility save did not create a backup.",
                ref assertionCount);

            File.WriteAllText(Path.Combine(root, BindingsPrimaryFileName), "{corrupted");
            File.WriteAllText(Path.Combine(root, AccessibilityPrimaryFileName), "{corrupted");
            TetherfallInputBindings.Shared.ResetToDefaults();
            Assert(
                TetherfallAccessibilityRuntime.Apply(
                    TetherfallAccessibilityRuntime.CreateDefaultSettings()),
                "Backup recovery defaults could not be staged.",
                ref assertionCount);

            var recovery = TetherfallPlayerConfigStore.LoadAll(root);
            Assert(recovery.Succeeded, "Backup recovery did not complete successfully.", ref assertionCount);
            Assert(
                recovery.Bindings.Status == TetherfallPlayerConfigStatus.LoadedBackup &&
                recovery.Bindings.RecoveredFromBackup,
                "Input bindings were not recovered from their backup.",
                ref assertionCount);
            Assert(
                recovery.Accessibility.Status == TetherfallPlayerConfigStatus.LoadedBackup &&
                recovery.Accessibility.RecoveredFromBackup,
                "Accessibility settings were not recovered from their backup.",
                ref assertionCount);
            AssertToggleControlsBinding(KeyCode.F10, ref assertionCount);
            Assert(
                Mathf.Approximately(TetherfallAccessibilityRuntime.Current.hudScale, 1.25f),
                "Accessibility backup recovery restored the wrong HUD scale.",
                ref assertionCount);
        }

        private static void VerifyInvalidJsonFallback(
            string root,
            ref int assertionCount)
        {
            Directory.CreateDirectory(root);
            const string invalidBindings = "not-json-bindings";
            const string invalidAccessibility = "not-json-accessibility";
            var bindingsPath = Path.Combine(root, BindingsPrimaryFileName);
            var accessibilityPath = Path.Combine(root, AccessibilityPrimaryFileName);
            File.WriteAllText(bindingsPath, invalidBindings);
            File.WriteAllText(accessibilityPath, invalidAccessibility);

            TetherfallInputBindings.Shared.ResetToDefaults();
            SetToggleControlsBinding(KeyCode.F9);
            ApplyHudScale(1.9f);

            var load = TetherfallPlayerConfigStore.LoadAll(root);
            Assert(load.Succeeded, "Invalid JSON did not fall back safely.", ref assertionCount);
            Assert(
                load.Bindings.Status == TetherfallPlayerConfigStatus.AppliedDefaultsInvalid,
                "Invalid input JSON did not report a default fallback.",
                ref assertionCount);
            Assert(
                load.Accessibility.Status ==
                TetherfallPlayerConfigStatus.AppliedDefaultsInvalid,
                "Invalid accessibility JSON did not report a default fallback.",
                ref assertionCount);
            AssertToggleControlsBinding(KeyCode.H, ref assertionCount);
            Assert(
                Mathf.Approximately(TetherfallAccessibilityRuntime.Current.hudScale, 1f),
                "Invalid accessibility JSON did not apply defaults.",
                ref assertionCount);
            Assert(
                File.ReadAllText(bindingsPath) == invalidBindings,
                "Loading invalid input JSON unexpectedly rewrote its source file.",
                ref assertionCount);
            Assert(
                File.ReadAllText(accessibilityPath) == invalidAccessibility,
                "Loading invalid accessibility JSON unexpectedly rewrote its source file.",
                ref assertionCount);
        }

        private static void VerifyFutureVersionSafety(
            string root,
            ref int assertionCount)
        {
            Directory.CreateDirectory(root);
            const string futureBindings = "{\"version\":999,\"actions\":[]}";
            const string futureAccessibility = "{\"schemaVersion\":999}";
            var bindingsPath = Path.Combine(root, BindingsPrimaryFileName);
            var accessibilityPath = Path.Combine(root, AccessibilityPrimaryFileName);
            File.WriteAllText(bindingsPath, futureBindings);
            File.WriteAllText(accessibilityPath, futureAccessibility);

            SetToggleControlsBinding(KeyCode.F8);
            ApplyHudScale(1.65f);

            var load = TetherfallPlayerConfigStore.LoadAll(root);
            Assert(!load.Succeeded, "Future-version data was not reported to the caller.", ref assertionCount);
            Assert(
                load.Bindings.Status ==
                TetherfallPlayerConfigStatus.AppliedDefaultsUnsupportedVersion,
                "Future input-binding data did not fail safe to defaults.",
                ref assertionCount);
            Assert(
                load.Accessibility.Status ==
                TetherfallPlayerConfigStatus.AppliedDefaultsUnsupportedVersion,
                "Future accessibility data did not fail safe to defaults.",
                ref assertionCount);
            AssertToggleControlsBinding(KeyCode.H, ref assertionCount);
            Assert(
                Mathf.Approximately(TetherfallAccessibilityRuntime.Current.hudScale, 1f),
                "Future accessibility data did not apply safe runtime defaults.",
                ref assertionCount);

            var save = TetherfallPlayerConfigStore.SaveAll(root);
            Assert(!save.Succeeded, "A save overwrote future-version player data.", ref assertionCount);
            Assert(
                save.Bindings.Status ==
                TetherfallPlayerConfigStatus.RefusedUnsupportedFutureVersion,
                "The input-binding save did not refuse a future-version primary.",
                ref assertionCount);
            Assert(
                save.Accessibility.Status ==
                TetherfallPlayerConfigStatus.RefusedUnsupportedFutureVersion,
                "The accessibility save did not refuse a future-version primary.",
                ref assertionCount);
            Assert(
                File.ReadAllText(bindingsPath) == futureBindings,
                "Future input-binding data changed during load or save.",
                ref assertionCount);
            Assert(
                File.ReadAllText(accessibilityPath) == futureAccessibility,
                "Future accessibility data changed during load or save.",
                ref assertionCount);
        }

        private static string[] SeedExpeditionFiles(string root)
        {
            var sentinels = new string[ExpeditionFileNames.Length];
            for (var index = 0; index < ExpeditionFileNames.Length; index++)
            {
                sentinels[index] =
                    "expedition-sentinel-" + index + "-" + Guid.NewGuid().ToString("N");
                File.WriteAllText(
                    Path.Combine(root, ExpeditionFileNames[index]),
                    sentinels[index]);
            }

            return sentinels;
        }

        private static void VerifyExpeditionIsolation(
            string root,
            string[] sentinels,
            ref int assertionCount)
        {
            for (var index = 0; index < ExpeditionFileNames.Length; index++)
            {
                Assert(
                    File.ReadAllText(Path.Combine(root, ExpeditionFileNames[index])) ==
                    sentinels[index],
                    ExpeditionFileNames[index] +
                    " changed while player configuration APIs were exercised.",
                    ref assertionCount);
            }
        }

        private static void SetToggleControlsBinding(KeyCode key)
        {
            if (!TetherfallInputBindings.Shared.TrySetBinding(
                    TetherfallInputAction.ToggleControls,
                    0,
                    TetherfallInputBinding.Key(key),
                    TetherfallInputConflictResolution.Allow,
                    out _,
                    out var error))
            {
                throw new InvalidOperationException(
                    "Could not stage the Toggle Controls binding: " + error);
            }
        }

        private static void AssertToggleControlsBinding(
            KeyCode expected,
            ref int assertionCount)
        {
            var bindings =
                TetherfallInputBindings.Shared.GetBindings(
                    TetherfallInputAction.ToggleControls);
            Assert(
                bindings.Count > 0 &&
                bindings[0].source ==
                TetherfallInputBindingSource.KeyOrJoystickButton &&
                bindings[0].keyCode == expected,
                "Toggle Controls did not use the expected key after persistence.",
                ref assertionCount);
        }

        private static void ApplyHudScale(float scale)
        {
            var settings = TetherfallAccessibilityRuntime.CreateDefaultSettings();
            settings.hudScale = scale;
            if (!TetherfallAccessibilityRuntime.Apply(settings))
            {
                throw new InvalidOperationException(
                    "Could not stage accessibility settings for verification.");
            }
        }

        private static void RestoreRuntimeState(
            TetherfallInputBindingData bindings,
            TetherfallAccessibilitySettings accessibility)
        {
            try
            {
                if (!TetherfallInputBindings.Shared.TryApplyData(
                        bindings,
                        false,
                        out _))
                {
                    TetherfallInputBindings.Shared.ResetToDefaults();
                }
            }
            catch (Exception)
            {
                TetherfallInputBindings.Shared.ResetToDefaults();
            }

            try
            {
                TetherfallAccessibilityRuntime.Apply(accessibility);
            }
            catch (Exception)
            {
                TetherfallAccessibilityRuntime.Apply(
                    TetherfallAccessibilityRuntime.CreateDefaultSettings());
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Tetherfall player-configuration verification could not clean its " +
                    "temporary directory (" + exception.GetType().Name + ").");
            }
        }

        private static void Assert(
            bool condition,
            string message,
            ref int assertionCount)
        {
            assertionCount++;
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
