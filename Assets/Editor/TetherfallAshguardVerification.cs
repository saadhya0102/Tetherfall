using System;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallAshguardVerification
    {
        [MenuItem("Tetherfall/Verify Cindermaw Ashguard")]
        public static void VerifyCindermawAshguard()
        {
            var gameRoot = new GameObject("Ashguard Verification");
            Enemy cindermaw = null;
            try
            {
                var game = gameRoot.AddComponent<TetherfallGame>();
                cindermaw = Enemy.Create(game, null, "enemy.elite.cindermaw", Vector3.zero);
                Assert(cindermaw != null, "Cindermaw could not be created.");
                SetPrivate(cindermaw, "telegraphing", true);
                SetPrivate(cindermaw, "ashguardActive", true);
                SetPrivate(cindermaw, "strikeAt", Time.time + 3f);
                SetPrivate(cindermaw, "ashguardEndsAt", Time.time + 3.7f);

                var healthBefore = cindermaw.Health;
                cindermaw.TakeDamage(50, "verification");
                Assert(
                    cindermaw.Health == healthBefore - Mathf.CeilToInt(50f * 0.68f),
                    "Ashguard did not reduce routine damage to its configured chip value.");
                Assert(!cindermaw.ApplyStagger(1f), "Routine stagger interrupted an active Ashguard.");

                Assert(
                    cindermaw.TryBreakAshguardFromCrossweave("Verifier"),
                    "A valid Crossweave did not fracture Ashguard.");
                Assert(!cindermaw.IsAshguarded, "Ashguard remained active after a valid Crossweave.");
                Assert(cindermaw.ExposeRemaining >= 5f, "Ashguard break did not grant its exposed window.");
                Assert(cindermaw.StaggerRemaining >= 0.5f, "Ashguard break did not grant its stagger opening.");
                Assert(
                    !cindermaw.TryBreakAshguardFromCrossweave("Verifier"),
                    "Ashguard fractured more than once from the same state.");

                Debug.Log("Cindermaw Ashguard verification passed.");
            }
            finally
            {
                if (cindermaw != null)
                {
                    UnityEngine.Object.DestroyImmediate(cindermaw.gameObject);
                }

                UnityEngine.Object.DestroyImmediate(gameRoot);
            }
        }

        private static void SetPrivate(Enemy enemy, string fieldName, object value)
        {
            var field = typeof(Enemy).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException($"Enemy field '{fieldName}' was unavailable.");
            }

            field.SetValue(enemy, value);
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
