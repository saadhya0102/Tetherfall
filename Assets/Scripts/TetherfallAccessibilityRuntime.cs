using System;
using UnityEngine;

namespace Tetherfall
{
    public enum TetherfallAccessibilityInputPreference
    {
        Hold,
        Toggle
    }

    public enum TetherfallAccessibilityInputAction
    {
        Sprint,
        Guard,
        Glide,
        CombatFocus
    }

    public enum TetherfallAccessibilityColorCuePalette
    {
        Original,
        ColorVisionDeficiencyFriendly,
        Grayscale
    }

    public enum TetherfallAccessibilityColorCue
    {
        Neutral,
        Friendly,
        Hostile,
        Warning,
        Positive,
        Objective,
        Interaction
    }

    [Serializable]
    public sealed class TetherfallAccessibilitySettings
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;

        [Range(0f, 1f)]
        public float cameraShakeIntensity = 1f;

        [Range(0f, 1f)]
        public float hitstopIntensity = 1f;

        [Range(0.75f, 2f)]
        public float hudScale = 1f;

        [Range(0.75f, 2f)]
        public float subtitleTutorialTextScale = 1f;

        public bool highContrastTelegraphs;
        public TetherfallAccessibilityInputPreference sprintPreference =
            TetherfallAccessibilityInputPreference.Hold;
        public TetherfallAccessibilityInputPreference guardPreference =
            TetherfallAccessibilityInputPreference.Hold;
        public TetherfallAccessibilityInputPreference glidePreference =
            TetherfallAccessibilityInputPreference.Hold;
        public TetherfallAccessibilityInputPreference combatFocusPreference =
            TetherfallAccessibilityInputPreference.Toggle;
        public bool colorCuesEnabled = true;
        public TetherfallAccessibilityColorCuePalette colorCuePalette =
            TetherfallAccessibilityColorCuePalette.Original;
        public bool reinforceColorCuesWithShapes = true;

        public TetherfallAccessibilitySettings Copy()
        {
            return new TetherfallAccessibilitySettings
            {
                schemaVersion = schemaVersion,
                cameraShakeIntensity = cameraShakeIntensity,
                hitstopIntensity = hitstopIntensity,
                hudScale = hudScale,
                subtitleTutorialTextScale = subtitleTutorialTextScale,
                highContrastTelegraphs = highContrastTelegraphs,
                sprintPreference = sprintPreference,
                guardPreference = guardPreference,
                glidePreference = glidePreference,
                combatFocusPreference = combatFocusPreference,
                colorCuesEnabled = colorCuesEnabled,
                colorCuePalette = colorCuePalette,
                reinforceColorCuesWithShapes = reinforceColorCuesWithShapes
            };
        }

        public void Sanitize()
        {
            schemaVersion = CurrentSchemaVersion;
            cameraShakeIntensity = SanitizeRange(cameraShakeIntensity, 0f, 1f, 1f);
            hitstopIntensity = SanitizeRange(hitstopIntensity, 0f, 1f, 1f);
            hudScale = SanitizeRange(hudScale, 0.75f, 2f, 1f);
            subtitleTutorialTextScale =
                SanitizeRange(subtitleTutorialTextScale, 0.75f, 2f, 1f);

            sprintPreference = SanitizePreference(
                sprintPreference,
                TetherfallAccessibilityInputPreference.Hold);
            guardPreference = SanitizePreference(
                guardPreference,
                TetherfallAccessibilityInputPreference.Hold);
            glidePreference = SanitizePreference(
                glidePreference,
                TetherfallAccessibilityInputPreference.Hold);
            combatFocusPreference = SanitizePreference(
                combatFocusPreference,
                TetherfallAccessibilityInputPreference.Toggle);

            if (colorCuePalette != TetherfallAccessibilityColorCuePalette.Original &&
                colorCuePalette != TetherfallAccessibilityColorCuePalette.ColorVisionDeficiencyFriendly &&
                colorCuePalette != TetherfallAccessibilityColorCuePalette.Grayscale)
            {
                colorCuePalette = TetherfallAccessibilityColorCuePalette.Original;
            }
        }

        private static float SanitizeRange(float value, float minimum, float maximum, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : Mathf.Clamp(value, minimum, maximum);
        }

        private static TetherfallAccessibilityInputPreference SanitizePreference(
            TetherfallAccessibilityInputPreference preference,
            TetherfallAccessibilityInputPreference fallback)
        {
            return preference == TetherfallAccessibilityInputPreference.Hold ||
                   preference == TetherfallAccessibilityInputPreference.Toggle
                ? preference
                : fallback;
        }
    }

    /// <summary>
    /// Owns runtime accessibility settings and opt-in helpers. Existing game systems are
    /// unchanged until they explicitly read this class.
    /// </summary>
    public static class TetherfallAccessibilityRuntime
    {
        public const float MinimumHudScale = 0.75f;
        public const float MaximumHudScale = 2f;
        public const float MinimumTextScale = 0.75f;
        public const float MaximumTextScale = 2f;

        private static TetherfallAccessibilitySettings current = CreateDefaultSettings();

        public static event Action SettingsChanged;

        public static TetherfallAccessibilitySettings Current
        {
            get
            {
                if (current == null)
                {
                    current = CreateDefaultSettings();
                }

                return current;
            }
        }

        public static float CameraShakeIntensity =>
            ReadScale(Current.cameraShakeIntensity, 0f, 1f, 1f);

        public static float HitstopIntensity =>
            ReadScale(Current.hitstopIntensity, 0f, 1f, 1f);

        public static float HudScale =>
            ReadScale(Current.hudScale, MinimumHudScale, MaximumHudScale, 1f);

        public static float SubtitleTutorialTextScale =>
            ReadScale(
                Current.subtitleTutorialTextScale,
                MinimumTextScale,
                MaximumTextScale,
                1f);

        public static bool HighContrastTelegraphs => Current.highContrastTelegraphs;
        public static bool ColorCuesEnabled => Current.colorCuesEnabled;

        public static bool ShouldReinforceColorCues =>
            Current.reinforceColorCuesWithShapes ||
            GetColorCuePalette() == TetherfallAccessibilityColorCuePalette.Grayscale;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            current = CreateDefaultSettings();
            SettingsChanged = null;
        }

        public static TetherfallAccessibilitySettings CreateDefaultSettings()
        {
            var settings = new TetherfallAccessibilitySettings();
            settings.Sanitize();
            return settings;
        }

        public static TetherfallAccessibilitySettings GetSnapshot()
        {
            var snapshot = Current.Copy();
            snapshot.Sanitize();
            return snapshot;
        }

        public static bool Apply(TetherfallAccessibilitySettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            var replacement = settings.Copy();
            replacement.Sanitize();
            current = replacement;
            SettingsChanged?.Invoke();
            return true;
        }

        public static void ResetToDefaults()
        {
            current = CreateDefaultSettings();
            SettingsChanged?.Invoke();
        }

        public static string SerializeCurrent(bool prettyPrint = false)
        {
            return JsonUtility.ToJson(GetSnapshot(), prettyPrint);
        }

        public static bool TryApplyJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                var settings = CreateDefaultSettings();
                JsonUtility.FromJsonOverwrite(json, settings);
                if (settings.schemaVersion < 1 ||
                    settings.schemaVersion > TetherfallAccessibilitySettings.CurrentSchemaVersion)
                {
                    return false;
                }

                return Apply(settings);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static float ScaleCameraShakeMagnitude(float magnitude)
        {
            return ScaleNonNegativeFinite(magnitude, CameraShakeIntensity);
        }

        public static float ScaleHitstopDuration(float duration)
        {
            return ScaleNonNegativeFinite(duration, HitstopIntensity);
        }

        public static float ScaleHudValue(float value)
        {
            return ScaleFinite(value, HudScale);
        }

        public static float ScaleSubtitleTutorialTextSize(float size)
        {
            return ScaleNonNegativeFinite(size, SubtitleTutorialTextScale);
        }

        public static int ScaleSubtitleTutorialFontSize(int fontSize)
        {
            if (fontSize <= 0)
            {
                return 0;
            }

            var scaled = ScaleSubtitleTutorialTextSize(fontSize);
            return scaled >= int.MaxValue
                ? int.MaxValue
                : Mathf.Max(1, Mathf.RoundToInt(scaled));
        }

        public static TetherfallAccessibilityInputPreference GetInputPreference(
            TetherfallAccessibilityInputAction action)
        {
            var preference = action switch
            {
                TetherfallAccessibilityInputAction.Sprint => Current.sprintPreference,
                TetherfallAccessibilityInputAction.Guard => Current.guardPreference,
                TetherfallAccessibilityInputAction.Glide => Current.glidePreference,
                TetherfallAccessibilityInputAction.CombatFocus => Current.combatFocusPreference,
                _ => TetherfallAccessibilityInputPreference.Hold
            };

            if (preference == TetherfallAccessibilityInputPreference.Hold ||
                preference == TetherfallAccessibilityInputPreference.Toggle)
            {
                return preference;
            }

            return action == TetherfallAccessibilityInputAction.CombatFocus
                ? TetherfallAccessibilityInputPreference.Toggle
                : TetherfallAccessibilityInputPreference.Hold;
        }

        public static bool ResolveInput(
            TetherfallAccessibilityInputAction action,
            bool isHeld,
            bool pressedThisFrame,
            ref bool toggledState)
        {
            if (GetInputPreference(action) == TetherfallAccessibilityInputPreference.Hold)
            {
                toggledState = false;
                return isHeld;
            }

            if (pressedThisFrame)
            {
                toggledState = !toggledState;
            }

            return toggledState;
        }

        public static void ResetToggleState(ref bool toggledState)
        {
            toggledState = false;
        }

        public static Color ResolveColorCue(
            Color originalColor,
            TetherfallAccessibilityColorCue cue)
        {
            var safeOriginal = SanitizeColor(originalColor, Color.white);
            safeOriginal.a = Mathf.Clamp01(safeOriginal.a);
            if (!ColorCuesEnabled)
            {
                return safeOriginal;
            }

            var palette = GetColorCuePalette();
            if (palette == TetherfallAccessibilityColorCuePalette.Original)
            {
                return safeOriginal;
            }

            var resolved = palette == TetherfallAccessibilityColorCuePalette.Grayscale
                ? GetGrayscaleCueColor(cue)
                : GetColorVisionDeficiencyFriendlyCueColor(cue);
            resolved.a = safeOriginal.a;
            return resolved;
        }

        public static Color ResolveTelegraphColor(
            Color originalColor,
            TetherfallAccessibilityColorCue cue = TetherfallAccessibilityColorCue.Warning)
        {
            var resolved = ResolveColorCue(originalColor, cue);
            if (!HighContrastTelegraphs)
            {
                return resolved;
            }

            var highContrast = GetHighContrastCueColor(cue);
            highContrast.a = Mathf.Max(resolved.a, 0.82f);
            return highContrast;
        }

        private static TetherfallAccessibilityColorCuePalette GetColorCuePalette()
        {
            var palette = Current.colorCuePalette;
            return palette == TetherfallAccessibilityColorCuePalette.Original ||
                   palette == TetherfallAccessibilityColorCuePalette.ColorVisionDeficiencyFriendly ||
                   palette == TetherfallAccessibilityColorCuePalette.Grayscale
                ? palette
                : TetherfallAccessibilityColorCuePalette.Original;
        }

        private static Color GetColorVisionDeficiencyFriendlyCueColor(
            TetherfallAccessibilityColorCue cue)
        {
            return cue switch
            {
                TetherfallAccessibilityColorCue.Friendly => Rgb(86, 180, 233),
                TetherfallAccessibilityColorCue.Hostile => Rgb(213, 94, 0),
                TetherfallAccessibilityColorCue.Warning => Rgb(240, 228, 66),
                TetherfallAccessibilityColorCue.Positive => Rgb(0, 158, 115),
                TetherfallAccessibilityColorCue.Objective => Rgb(204, 121, 167),
                TetherfallAccessibilityColorCue.Interaction => Rgb(230, 159, 0),
                _ => Rgb(238, 238, 238)
            };
        }

        private static Color GetGrayscaleCueColor(TetherfallAccessibilityColorCue cue)
        {
            var value = cue switch
            {
                TetherfallAccessibilityColorCue.Hostile => 0.22f,
                TetherfallAccessibilityColorCue.Warning => 1f,
                TetherfallAccessibilityColorCue.Positive => 0.72f,
                TetherfallAccessibilityColorCue.Objective => 0.48f,
                TetherfallAccessibilityColorCue.Interaction => 0.86f,
                TetherfallAccessibilityColorCue.Friendly => 0.62f,
                _ => 0.92f
            };
            return new Color(value, value, value, 1f);
        }

        private static Color GetHighContrastCueColor(TetherfallAccessibilityColorCue cue)
        {
            return cue switch
            {
                TetherfallAccessibilityColorCue.Friendly => new Color(0f, 0.9f, 1f),
                TetherfallAccessibilityColorCue.Hostile => new Color(1f, 0.12f, 0.04f),
                TetherfallAccessibilityColorCue.Warning => new Color(1f, 0.92f, 0f),
                TetherfallAccessibilityColorCue.Positive => new Color(0.2f, 1f, 0.58f),
                TetherfallAccessibilityColorCue.Objective => new Color(1f, 0.32f, 0.9f),
                TetherfallAccessibilityColorCue.Interaction => new Color(1f, 0.62f, 0f),
                _ => Color.white
            };
        }

        private static Color Rgb(byte red, byte green, byte blue)
        {
            return new Color(red / 255f, green / 255f, blue / 255f, 1f);
        }

        private static Color SanitizeColor(Color color, Color fallback)
        {
            return IsFinite(color.r) &&
                   IsFinite(color.g) &&
                   IsFinite(color.b) &&
                   IsFinite(color.a)
                ? color
                : fallback;
        }

        private static float ReadScale(
            float value,
            float minimum,
            float maximum,
            float fallback)
        {
            return IsFinite(value) ? Mathf.Clamp(value, minimum, maximum) : fallback;
        }

        private static float ScaleNonNegativeFinite(float value, float scale)
        {
            return !IsFinite(value) || value <= 0f
                ? 0f
                : ScaleFinite(value, scale);
        }

        private static float ScaleFinite(float value, float scale)
        {
            if (!IsFinite(value))
            {
                return 0f;
            }

            var scaled = value * scale;
            return IsFinite(scaled) ? scaled : 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
