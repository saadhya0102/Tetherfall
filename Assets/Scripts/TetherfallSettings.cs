using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private static readonly float[] CameraShakeSteps = { 0f, 0.35f, 0.7f, 1f };
        private static readonly float[] HitstopSteps = { 0f, 0.5f, 1f };
        private static readonly float[] InterfaceScaleSteps = { 0.85f, 1f, 1.15f, 1.3f, 1.5f };
        private static readonly TetherfallInputAction[] SettingsActions =
            TetherfallInputBindings.GetAllActions();

        private Vector2 settingsControlScroll;
        private bool settingsWaitingForBinding;
        private TetherfallInputAction settingsPendingAction;
        private string settingsStatus = string.Empty;
        private float settingsStatusUntil;

        private void DrawSettingsPanel(Rect rect)
        {
            var contentTop = rect.y + 72f;
            var contentBottom = rect.yMax - 48f;
            var left = new Rect(
                rect.x + 26f,
                contentTop,
                rect.width * 0.43f,
                contentBottom - contentTop);
            var right = new Rect(
                rect.x + rect.width * 0.47f,
                contentTop,
                rect.width * 0.5f - 26f,
                contentBottom - contentTop);

            GUI.Label(new Rect(left.x, left.y, left.width, 24f), "Accessibility", headingStyle);
            var settings = TetherfallAccessibilityRuntime.Current;
            var rowY = left.y + 34f;
            DrawSettingsChoice(
                left,
                ref rowY,
                "Camera shake",
                $"{Mathf.RoundToInt(settings.cameraShakeIntensity * 100f)}%",
                () => ChangeAccessibility(value =>
                    value.cameraShakeIntensity = NextStep(value.cameraShakeIntensity, CameraShakeSteps)));
            DrawSettingsChoice(
                left,
                ref rowY,
                "Hit pause",
                $"{Mathf.RoundToInt(settings.hitstopIntensity * 100f)}%",
                () => ChangeAccessibility(value =>
                    value.hitstopIntensity = NextStep(value.hitstopIntensity, HitstopSteps)));
            DrawSettingsChoice(
                left,
                ref rowY,
                "HUD scale",
                $"{settings.hudScale:0.00}x",
                () => ChangeAccessibility(value =>
                    value.hudScale = NextStep(value.hudScale, InterfaceScaleSteps)));
            DrawSettingsChoice(
                left,
                ref rowY,
                "Guidance text",
                $"{settings.subtitleTutorialTextScale:0.00}x",
                () => ChangeAccessibility(value =>
                    value.subtitleTutorialTextScale =
                        NextStep(value.subtitleTutorialTextScale, InterfaceScaleSteps)));
            DrawSettingsChoice(
                left,
                ref rowY,
                "Telegraph contrast",
                settings.highContrastTelegraphs ? "High" : "Standard",
                () => ChangeAccessibility(value =>
                    value.highContrastTelegraphs = !value.highContrastTelegraphs));
            DrawSettingsChoice(
                left,
                ref rowY,
                "Color palette",
                PaletteLabel(settings.colorCuePalette),
                () => ChangeAccessibility(value =>
                    value.colorCuePalette = NextPalette(value.colorCuePalette)));
            DrawSettingsChoice(
                left,
                ref rowY,
                "Color cues",
                settings.colorCuesEnabled ? "On" : "Off",
                () => ChangeAccessibility(value =>
                    value.colorCuesEnabled = !value.colorCuesEnabled));
            DrawSettingsChoice(
                left,
                ref rowY,
                "Shape and text cues",
                settings.reinforceColorCuesWithShapes ? "On" : "Off",
                () => ChangeAccessibility(value =>
                    value.reinforceColorCuesWithShapes =
                        !value.reinforceColorCuesWithShapes));
            DrawSettingsChoice(
                left,
                ref rowY,
                "Sprint input",
                PreferenceLabel(settings.sprintPreference),
                () => ChangeAccessibility(value =>
                    value.sprintPreference = TogglePreference(value.sprintPreference)));
            DrawSettingsChoice(
                left,
                ref rowY,
                "Guard input",
                PreferenceLabel(settings.guardPreference),
                () => ChangeAccessibility(value =>
                    value.guardPreference = TogglePreference(value.guardPreference)));
            DrawSettingsChoice(
                left,
                ref rowY,
                "Glide input",
                PreferenceLabel(settings.glidePreference),
                () => ChangeAccessibility(value =>
                    value.glidePreference = TogglePreference(value.glidePreference)));
            DrawSettingsChoice(
                left,
                ref rowY,
                "Combat focus",
                PreferenceLabel(settings.combatFocusPreference),
                () => ChangeAccessibility(value =>
                    value.combatFocusPreference = TogglePreference(value.combatFocusPreference)));

            if (GUI.Button(
                    new Rect(left.x, Mathf.Min(rowY + 6f, left.yMax - 34f), left.width, 28f),
                    "Restore accessibility defaults"))
            {
                var result = TetherfallPlayerConfigStore.ResetAccessibilityAndSave();
                SetSettingsStatus(
                    result.Accessibility.Succeeded
                        ? "Accessibility defaults restored."
                        : "Defaults applied, but preferences could not be saved.");
            }

            DrawControlBindings(right);
            if (!string.IsNullOrEmpty(settingsStatus) && Time.time <= settingsStatusUntil)
            {
                GUI.Label(
                    new Rect(rect.x + 26f, rect.yMax - 57f, rect.width - 52f, 22f),
                    settingsStatus,
                    mutedStyle);
            }
        }

        private void DrawSettingsChoice(
            Rect column,
            ref float rowY,
            string label,
            string value,
            Action onPressed)
        {
            GUI.Label(new Rect(column.x, rowY + 3f, column.width - 144f, 22f), label, bodyStyle);
            if (GUI.Button(new Rect(column.xMax - 138f, rowY, 138f, 27f), value))
            {
                onPressed?.Invoke();
            }

            rowY += 34f;
        }

        private void DrawControlBindings(Rect rect)
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 24f), "Remappable controls", headingStyle);
            GUI.Label(
                new Rect(rect.x, rect.y + 26f, rect.width, 34f),
                settingsWaitingForBinding
                    ? $"Press a key or mouse button for {TetherfallInputBindings.GetActionDisplayLabel(settingsPendingAction)}. Esc cancels."
                    : "Choose Rebind, then press the replacement input. Conflicts move to the new action.",
                mutedStyle);

            var viewport = new Rect(rect.x, rect.y + 64f, rect.width, rect.height - 104f);
            var contentHeight = SettingsActions.Length * 56f + 8f;
            settingsControlScroll = GUI.BeginScrollView(
                viewport,
                settingsControlScroll,
                new Rect(0f, 0f, viewport.width - 18f, contentHeight));

            for (var index = 0; index < SettingsActions.Length; index++)
            {
                var action = SettingsActions[index];
                var y = index * 56f;
                GUI.Label(
                    new Rect(4f, y + 2f, viewport.width - 156f, 21f),
                    TetherfallInputBindings.GetActionDisplayLabel(action),
                    bodyStyle);
                GUI.Label(
                    new Rect(4f, y + 24f, viewport.width - 156f, 22f),
                    TetherfallInputBindings.Shared.GetBindingSummary(action),
                    mutedStyle);

                var previousEnabled = GUI.enabled;
                GUI.enabled = !settingsWaitingForBinding || settingsPendingAction == action;
                if (GUI.Button(
                        new Rect(viewport.width - 144f, y + 8f, 116f, 30f),
                        settingsWaitingForBinding && settingsPendingAction == action
                            ? "Listening..."
                            : "Rebind"))
                {
                    settingsPendingAction = action;
                    settingsWaitingForBinding = true;
                    SetSettingsStatus("Waiting for a replacement input.");
                }

                GUI.enabled = previousEnabled;
            }

            GUI.EndScrollView();

            if (GUI.Button(
                    new Rect(rect.x, rect.yMax - 34f, rect.width, 28f),
                    "Restore default controls"))
            {
                settingsWaitingForBinding = false;
                var result = TetherfallPlayerConfigStore.ResetBindingsAndSave();
                SetSettingsStatus(
                    result.Bindings.Succeeded
                        ? "Default controls restored."
                        : "Defaults applied, but controls could not be saved.");
            }
        }

        private bool HandleSettingsRebindInput()
        {
            if (!settingsWaitingForBinding)
            {
                return false;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                settingsWaitingForBinding = false;
                SetSettingsStatus("Rebinding cancelled.");
                return true;
            }

            for (var mouseButton = 0;
                 mouseButton <= TetherfallInputBindings.MaximumLegacyMouseButton;
                 mouseButton++)
            {
                if (Input.GetMouseButtonDown(mouseButton))
                {
                    ApplyCapturedBinding(TetherfallInputBinding.Mouse(mouseButton));
                    return true;
                }
            }

            if (!Input.anyKeyDown)
            {
                return true;
            }

            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (keyCode == KeyCode.None ||
                    ((int)keyCode >= (int)KeyCode.Mouse0 && (int)keyCode <= (int)KeyCode.Mouse6) ||
                    !Input.GetKeyDown(keyCode))
                {
                    continue;
                }

                ApplyCapturedBinding(TetherfallInputBinding.Key(keyCode));
                return true;
            }

            return true;
        }

        private void ApplyCapturedBinding(TetherfallInputBinding binding)
        {
            var existing = TetherfallInputBindings.Shared.GetBindings(settingsPendingAction);
            var bindingIndex = existing.Count > 0 ? 0 : existing.Count;
            List<TetherfallInputConflict> conflicts;
            string error;
            var applied = TetherfallInputBindings.Shared.TrySetBinding(
                settingsPendingAction,
                bindingIndex,
                binding,
                TetherfallInputConflictResolution.ReplaceExisting,
                out conflicts,
                out error);
            settingsWaitingForBinding = false;
            if (!applied)
            {
                SetSettingsStatus($"Could not rebind: {error}");
                return;
            }

            var save = TetherfallPlayerConfigStore.SaveBindings();
            SetSettingsStatus(
                save.Bindings.Succeeded
                    ? $"{TetherfallInputBindings.GetActionDisplayLabel(settingsPendingAction)} rebound."
                    : "Binding changed for this session, but could not be saved.");
        }

        private void CancelSettingsRebind()
        {
            settingsWaitingForBinding = false;
        }

        private void ChangeAccessibility(Action<TetherfallAccessibilitySettings> change)
        {
            var settings = TetherfallAccessibilityRuntime.GetSnapshot();
            change(settings);
            TetherfallAccessibilityRuntime.Apply(settings);
            var save = TetherfallPlayerConfigStore.SaveAccessibility();
            SetSettingsStatus(
                save.Accessibility.Succeeded
                    ? "Accessibility preference saved."
                    : "Preference changed for this session, but could not be saved.");
        }

        private void SetSettingsStatus(string message)
        {
            settingsStatus = message ?? string.Empty;
            settingsStatusUntil = Time.time + 4f;
        }

        private static float NextStep(float current, float[] steps)
        {
            for (var index = 0; index < steps.Length; index++)
            {
                if (steps[index] > current + 0.001f)
                {
                    return steps[index];
                }
            }

            return steps[0];
        }

        private static TetherfallAccessibilityInputPreference TogglePreference(
            TetherfallAccessibilityInputPreference preference)
        {
            return preference == TetherfallAccessibilityInputPreference.Toggle
                ? TetherfallAccessibilityInputPreference.Hold
                : TetherfallAccessibilityInputPreference.Toggle;
        }

        private static string PreferenceLabel(TetherfallAccessibilityInputPreference preference)
        {
            return preference == TetherfallAccessibilityInputPreference.Toggle ? "Toggle" : "Hold";
        }

        private static TetherfallAccessibilityColorCuePalette NextPalette(
            TetherfallAccessibilityColorCuePalette palette)
        {
            return palette switch
            {
                TetherfallAccessibilityColorCuePalette.Original =>
                    TetherfallAccessibilityColorCuePalette.ColorVisionDeficiencyFriendly,
                TetherfallAccessibilityColorCuePalette.ColorVisionDeficiencyFriendly =>
                    TetherfallAccessibilityColorCuePalette.Grayscale,
                _ => TetherfallAccessibilityColorCuePalette.Original
            };
        }

        private static string PaletteLabel(TetherfallAccessibilityColorCuePalette palette)
        {
            return palette switch
            {
                TetherfallAccessibilityColorCuePalette.ColorVisionDeficiencyFriendly => "Color-safe",
                TetherfallAccessibilityColorCuePalette.Grayscale => "Grayscale",
                _ => "Original"
            };
        }

        private static string BuildControlsReference()
        {
            var input = TetherfallInputBindings.Shared;
            return
                $"{input.GetBindingSummary(TetherfallInputAction.MoveVertical)}/" +
                $"{input.GetBindingSummary(TetherfallInputAction.MoveHorizontal)} Move  " +
                $"{input.GetBindingSummary(TetherfallInputAction.Sprint)} Sprint  " +
                $"{input.GetBindingSummary(TetherfallInputAction.JumpGlideClimb)} Jump/Glide/Climb  " +
                $"{input.GetBindingSummary(TetherfallInputAction.DodgeDescendDrop)} Dodge/Descend  " +
                $"{input.GetBindingSummary(TetherfallInputAction.Guard)} Guard  " +
                $"{input.GetBindingSummary(TetherfallInputAction.Attack)} Attack  " +
                $"{input.GetBindingSummary(TetherfallInputAction.Interact)} Interact\n" +
                $"{input.GetBindingSummary(TetherfallInputAction.OpenEquipmentAndStores)} Gear  " +
                $"{input.GetBindingSummary(TetherfallInputAction.OpenQuestLedger)} Quests  " +
                $"{input.GetBindingSummary(TetherfallInputAction.OpenSkillTree)} Skills  " +
                $"{input.GetBindingSummary(TetherfallInputAction.OpenWorldMap)} Map  " +
                $"{input.GetBindingSummary(TetherfallInputAction.OpenSettlementPlanning)} Settlement\n" +
                $"{input.GetBindingSummary(TetherfallInputAction.OpenSettings)} Settings  " +
                $"{input.GetBindingSummary(TetherfallInputAction.OpenFieldLog)} Log  " +
                $"{input.GetBindingSummary(TetherfallInputAction.ToggleControls)} Hide controls";
        }
    }
}
