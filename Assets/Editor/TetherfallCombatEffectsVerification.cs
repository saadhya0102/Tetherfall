using System;
using System.Collections.Generic;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tetherfall.Editor
{
    public static class TetherfallCombatEffectsVerification
    {
        private const string MenuPath = "Tetherfall/Verify Combat Effects V2";
        private const int MaximumSharedMaterials = 36;

        [MenuItem(MenuPath, false, 2210)]
        public static void VerifyCombatEffects()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Combat-effects verification is edit-mode only. Stop Play Mode and run it again.");
            }

            var originalAccessibility = TetherfallAccessibilityRuntime.GetSnapshot();
            var originalTargetFrameRate = Application.targetFrameRate;
            var originalVSyncCount = QualitySettings.vSyncCount;
            var game = Object.FindAnyObjectByType<TetherfallGame>();
            GameObject fixtureRoot = null;
            var ownsGame = game == null;
            try
            {
                TetherfallCombatEffectsV2.ResetForVerification();
                if (game == null)
                {
                    fixtureRoot = new GameObject("Combat Effects Verification");
                    game = fixtureRoot.AddComponent<TetherfallGame>();
                }

                var verificationSettings = originalAccessibility.Copy();
                verificationSettings.cameraShakeIntensity = 0f;
                verificationSettings.hitstopIntensity = 0f;
                verificationSettings.highContrastTelegraphs = true;
                verificationSettings.colorCuesEnabled = true;
                verificationSettings.reinforceColorCuesWithShapes = true;
                Assert(
                    TetherfallAccessibilityRuntime.Apply(verificationSettings),
                    "Accessibility verification settings could not be applied.");
                Assert(
                    TetherfallAccessibilityRuntime.ScaleCameraShakeMagnitude(0.2f) == 0f,
                    "Camera-shake scaling did not honor a zero-intensity accessibility setting.");
                Assert(
                    TetherfallAccessibilityRuntime.ScaleHitstopDuration(0.1f) == 0f,
                    "Hitstop scaling did not honor a zero-intensity accessibility setting.");

                var origin = game.transform.position;
                SpawnWeaponFixtures(game, origin);
                SpawnAllyFixtures(game, origin);
                SpawnImpactStressFixture(game, origin);

                var saturated = TetherfallCombatEffectsV2.GetBudgetSnapshot();
                Assert(
                    saturated.ActiveEffects <= TetherfallCombatEffectsV2.MaximumActiveEffects,
                    "Combat effects exceeded their global active budget (" +
                    saturated.ActiveEffects + "/" +
                    TetherfallCombatEffectsV2.MaximumActiveEffects + ").");
                Assert(
                    saturated.CreatedEffects <= TetherfallCombatEffectsV2.MaximumCreatedEffects,
                    "Combat effects exceeded their global created-object budget (" +
                    saturated.CreatedEffects + "/" +
                    TetherfallCombatEffectsV2.MaximumCreatedEffects + ").");
                Assert(
                    saturated.DroppedEffects > 0,
                    "The stress fixture did not exercise an effect cap; hard-cap behavior was not verified.");

                VerifyEffectObjects(saturated);

                var createdBeforeReuse = saturated.CreatedEffects;
                TetherfallCombatEffectsV2.RecycleAllActive();
                var recycled = TetherfallCombatEffectsV2.GetBudgetSnapshot();
                Assert(recycled.ActiveEffects == 0, "Recycling left active combat effects behind.");
                Assert(
                    recycled.PooledEffects == recycled.CreatedEffects,
                    "Not every created combat effect returned to a pool.");

                SpawnWeaponFixtures(game, origin);
                var reused = TetherfallCombatEffectsV2.GetBudgetSnapshot();
                Assert(
                    reused.CreatedEffects <= createdBeforeReuse,
                    "A repeated weapon pass created objects instead of reusing its warmed pools.");

                Debug.Log(
                    "Combat Effects V2 verification passed: " +
                    saturated.ActiveEffects + " active / " +
                    saturated.CreatedEffects + " created at saturation, " +
                    saturated.DroppedEffects + " bounded spawns dropped, collider-free, " +
                    "light-free, accessibility-scaled, and pool reuse confirmed.");
            }
            finally
            {
                TetherfallCombatEffectsV2.ResetForVerification();
                TetherfallAccessibilityRuntime.Apply(originalAccessibility);
                Application.targetFrameRate = originalTargetFrameRate;
                QualitySettings.vSyncCount = originalVSyncCount;
                if (fixtureRoot != null)
                {
                    Object.DestroyImmediate(fixtureRoot);
                }

                if (ownsGame)
                {
                    ResetGameStatics();
                }
            }
        }

        [MenuItem(MenuPath, true, 2210)]
        private static bool CanVerifyCombatEffects()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void SpawnWeaponFixtures(TetherfallGame game, Vector3 origin)
        {
            for (var index = 0; index < 3; index++)
            {
                var direction = Quaternion.Euler(0f, index * 24f - 24f, 0f) * Vector3.forward;
                game.PlayWeaponSwing(
                    (WeaponDiscipline)index,
                    origin + Vector3.right * (index - 1) * 1.4f,
                    direction,
                    index);
            }
        }

        private static void SpawnAllyFixtures(TetherfallGame game, Vector3 origin)
        {
            var sources = new[]
            {
                "Bramblet Crossweave response",
                "Slateback tactical response",
                "Mistling resonance",
                "Cinderwing Threadlink weave",
                "scout focus mark",
                "medic field aid",
                "ruin-breaker breach",
                "unknown expedition ally"
            };
            for (var index = 0; index < sources.Length; index++)
            {
                var source = origin + new Vector3(-3f, 0.9f, index * 0.32f - 1.1f);
                var target = origin + new Vector3(3f, 1f, index * 0.24f - 0.8f);
                CombatFeedback.CreateAllyStrike(
                    game,
                    source,
                    target,
                    new Color(0.55f, 0.82f, 0.72f),
                    sources[index]);
            }
        }

        private static void SpawnImpactStressFixture(TetherfallGame game, Vector3 origin)
        {
            for (var index = 0; index < 96; index++)
            {
                var angle = index * 19f * Mathf.Deg2Rad;
                var position = origin + new Vector3(
                    Mathf.Cos(angle) * 2.4f,
                    0.12f + index % 3 * 0.18f,
                    Mathf.Sin(angle) * 2.4f);
                CombatFeedback.CreateImpact(
                    game,
                    position,
                    index % 2 == 0
                        ? new Color(0.93f, 0.63f, 0.3f)
                        : new Color(0.42f, 0.88f, 0.8f),
                    0.8f + index % 4 * 0.24f);
            }
        }

        private static void VerifyEffectObjects(CombatEffectsBudgetSnapshot budget)
        {
            var found = 0;
            var materials = new HashSet<int>();
            foreach (var effect in Resources.FindObjectsOfTypeAll<TetherfallCombatEffect>())
            {
                if (effect == null ||
                    !effect.gameObject.scene.IsValid() ||
                    !effect.gameObject.scene.isLoaded ||
                    EditorUtility.IsPersistent(effect))
                {
                    continue;
                }

                found++;
                Assert(
                    effect.gameObject.layer == TetherfallCombatEffectsV2.IgnoreRaycastLayer,
                    effect.name + " was not placed on the Ignore Raycast layer.");
                Assert(
                    effect.GetComponent<Collider>() == null,
                    effect.name + " retained a gameplay collider.");
                Assert(
                    effect.GetComponent<Collider2D>() == null,
                    effect.name + " retained a 2D gameplay collider.");
                Assert(
                    effect.GetComponent<Light>() == null,
                    effect.name + " introduced a realtime light.");

                var renderer = effect.GetComponent<Renderer>();
                Assert(renderer != null, effect.name + " did not retain a renderer.");
                Assert(
                    renderer.sharedMaterial != null,
                    effect.name + " did not use a shared runtime material.");
                materials.Add(renderer.sharedMaterial.GetInstanceID());
            }

            Assert(
                found == budget.CreatedEffects,
                "Diagnostics reported " + budget.CreatedEffects +
                " created effects, but the scene contained " + found + ".");
            Assert(
                materials.Count <= MaximumSharedMaterials,
                "Combat effects exceeded the shared-material budget (" +
                materials.Count + "/" + MaximumSharedMaterials + ").");
        }

        private static void ResetGameStatics()
        {
            var reset = typeof(TetherfallGame).GetMethod(
                "ResetStatics",
                BindingFlags.Static | BindingFlags.NonPublic);
            reset?.Invoke(null, null);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
