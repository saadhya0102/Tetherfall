using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallPlayerCombatQueryVerification
    {
        [MenuItem("Tetherfall/Verify Player Combat Queries", false, 2213)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Player-combat query verification is edit-mode only.");
            }

            var fixture = new GameObject("Player Combat Query Verification");
            GameObject target = null;
            GameObject obstacle = null;
            try
            {
                var playerObject = new GameObject("Player Query Fixture");
                playerObject.transform.SetParent(fixture.transform, false);
                var player = playerObject.AddComponent<PlayerAvatar>();

                var overlapBuffer = ReadField<Collider[]>(player, "combatHitBuffer");
                var lineBuffer = ReadField<RaycastHit[]>(player, "combatLineHitBuffer");
                var enemySet = ReadField<IEnumerable>(player, "struckEnemies");
                var bossSet = ReadField<IEnumerable>(player, "struckBosses");
                Assert(overlapBuffer != null && overlapBuffer.Length >= 64,
                    "Reusable combat overlap buffer is too small.");
                Assert(lineBuffer != null && lineBuffer.Length >= 16,
                    "Reusable combat line-of-sight buffer is too small.");
                Assert(enemySet != null && bossSet != null,
                    "Reusable target de-duplication sets are missing.");

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.name = "Combat Query Target";
                target.transform.position = new Vector3(0f, 0.9f, 2f);
                Physics.SyncTransforms();
                Assert(
                    CanStrike(player, target.transform, Vector3.up * 0.9f, Vector3.forward, false),
                    "A clear forward target failed the melee line-of-sight query.");

                obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = "Combat Query Obstacle";
                obstacle.transform.position = new Vector3(0f, 0.9f, 1f);
                obstacle.transform.localScale = new Vector3(1.2f, 2f, 0.25f);
                Physics.SyncTransforms();
                Assert(
                    !CanStrike(player, target.transform, Vector3.up * 0.9f, Vector3.forward, false),
                    "A solid intervening obstacle did not block the melee query.");

                UnityEngine.Object.DestroyImmediate(obstacle);
                obstacle = null;
                target.transform.position = new Vector3(0f, 0.9f, -2f);
                Physics.SyncTransforms();
                Assert(
                    !CanStrike(player, target.transform, Vector3.up * 0.9f, Vector3.forward, false),
                    "A narrow forward attack struck a target behind the player.");
                Assert(
                    CanStrike(player, target.transform, Vector3.up * 0.9f, Vector3.forward, true),
                    "A clear wide-area attack incorrectly rejected a target behind the player.");

                Debug.Log(
                    "Player-combat query verification passed: reusable overlap/line buffers, " +
                    "target de-duplication, obstacle rejection, and narrow/wide arc semantics are valid.");
            }
            finally
            {
                if (obstacle != null)
                {
                    UnityEngine.Object.DestroyImmediate(obstacle);
                }

                if (target != null)
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }

                UnityEngine.Object.DestroyImmediate(fixture);
            }
        }

        private static bool CanStrike(
            PlayerAvatar player,
            Transform target,
            Vector3 origin,
            Vector3 direction,
            bool wideArc)
        {
            var method = typeof(PlayerAvatar).GetMethod(
                "CanStrikeTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert(method != null, "Player melee line-of-sight method is missing.");
            return (bool)method.Invoke(player, new object[]
            {
                target,
                origin,
                direction,
                wideArc
            });
        }

        private static T ReadField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException($"Player field '{fieldName}' is missing.");
            }

            return (T)field.GetValue(instance);
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
