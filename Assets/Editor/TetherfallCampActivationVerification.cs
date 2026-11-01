using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallCampActivationVerification
    {
        [MenuItem("Tetherfall/Verify Enemy Camp Activation", false, 2210)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Enemy-camp activation verification is edit-mode only.");
            }

            var gameObject = new GameObject("Camp Activation Verification Game");
            EnemyCamp camp = null;
            try
            {
                var game = gameObject.AddComponent<TetherfallGame>();
                camp = EnemyCamp.Create(
                    game,
                    "encounter.alderwake.roadblock",
                    Vector3.zero,
                    "verification.lazy-camp");

                Assert(!camp.IsPopulated, "Camp spawned its population during construction.");
                Assert(!camp.enabled, "Dormant camp retained an Update callback.");
                Assert(CountTrackedEnemies(camp) == 0, "Dormant camp tracks enemies.");

                camp.EvaluatePopulation(new Vector3(200f, 0f, 0f));
                Assert(!camp.IsPopulated, "Camp woke outside its activation radius.");
                Assert(CountTrackedEnemies(camp) == 0, "Far camp spawned enemies.");

                camp.EvaluatePopulation(Vector3.zero);
                Assert(camp.IsPopulated, "Camp did not wake near the player.");
                Assert(camp.enabled, "Awake camp did not enable its encounter scheduler.");
                Assert(CountTrackedEnemies(camp) > 0, "Awake camp did not spawn its first wave.");

                camp.EvaluatePopulation(new Vector3(200f, 0f, 0f));
                Assert(camp.IsPopulated, "Ordinary distance despawn interrupted active combat.");

                camp.EvaluatePopulation(new Vector3(200f, 0f, 0f), true);
                Assert(!camp.IsPopulated, "Teleport soft reset did not sleep a distant camp.");
                Assert(!camp.enabled, "Soft-reset camp retained an Update callback.");
                Assert(CountTrackedEnemies(camp) == 0, "Soft-reset camp retained tracked enemies.");

                Debug.Log(
                    "Enemy-camp activation verification passed: construction is population-free, " +
                    "nearby wake-up works, ordinary distance preserves combat, teleport soft reset is silent, " +
                    "and dormant camps have no Update callback.");
            }
            finally
            {
                if (camp != null)
                {
                    UnityEngine.Object.DestroyImmediate(camp.gameObject);
                }

                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static int CountTrackedEnemies(EnemyCamp camp)
        {
            var field = typeof(EnemyCamp).GetField(
                "aliveEnemies",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var collection = field?.GetValue(camp) as System.Collections.ICollection;
            return collection?.Count ?? -1;
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
