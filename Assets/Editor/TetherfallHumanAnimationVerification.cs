using System;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallHumanAnimationVerification
    {
        private const string MenuPath = "Tetherfall/Verify Human Animation";

        [MenuItem(MenuPath, false, 2192)]
        public static void VerifyHumanAnimation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Human-animation verification must run in Edit Mode.");
            }

            var fixture = new GameObject("Human Animation Verification Fixture");
            try
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.name = "Fallback Capsule Visual";
                fallback.transform.SetParent(fixture.transform, false);
                fallback.transform.localPosition = new Vector3(0.23f, 0.48f, -0.17f);
                fallback.transform.localRotation = Quaternion.Euler(4f, 17f, -3f);
                fallback.transform.localScale = new Vector3(0.72f, 1.04f, 0.72f);
                UnityEngine.Object.DestroyImmediate(fallback.GetComponent<Collider>());

                var refusedRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                refusedRoot.name = "Collider Gameplay Root";
                refusedRoot.transform.SetParent(fixture.transform, false);

                var choosingRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                choosingRoot.name = "Collider Root With Visual";
                choosingRoot.transform.SetParent(fixture.transform, false);
                var chosenVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                chosenVisual.name = "Human Visual";
                chosenVisual.transform.SetParent(choosingRoot.transform, false);
                UnityEngine.Object.DestroyImmediate(
                    chosenVisual.GetComponent<Collider>());

                var colliderCountBefore =
                    fixture.GetComponentsInChildren<Collider>(true).Length;
                var lightCountBefore =
                    fixture.GetComponentsInChildren<Light>(true).Length;

                var initialPosition = fallback.transform.localPosition;
                var initialRotation = fallback.transform.localRotation;
                var initialScale = fallback.transform.localScale;

                var first = TetherfallHumanAnimation.Attach(fallback.transform);
                var second = TetherfallHumanAnimation.Attach(fallback.transform);
                Assert(first != null, "Attach rejected a collider-free primitive visual.");
                Assert(
                    ReferenceEquals(first, second),
                    "Repeated Attach returned a different animator.");
                Assert(
                    fallback.GetComponents<HumanProceduralAnimator>().Length == 1,
                    "Repeated Attach added more than one Update component.");
                Assert(first.IsBound, "Fallback primitive did not bind.");
                Assert(first.UsesFallbackBody, "Fallback primitive was not classified as a body.");
                Assert(
                    first.BoundSocketCount == 1,
                    $"Fallback primitive bound {first.BoundSocketCount} sockets instead of one.");

                first.TriggerTalk();
                InvokeTick(first, 0.14f);
                var poseMoved =
                    Vector3.SqrMagnitude(
                        fallback.transform.localPosition - initialPosition) > 0.0000001f ||
                    Quaternion.Angle(
                        fallback.transform.localRotation,
                        initialRotation) > 0.001f;
                Assert(poseMoved, "A triggered talk layer did not alter the fallback pose.");

                first.ResetPose();
                Assert(
                    Approximately(fallback.transform.localPosition, initialPosition),
                    "ResetPose did not restore local position.");
                Assert(
                    Quaternion.Angle(fallback.transform.localRotation, initialRotation) < 0.001f,
                    "ResetPose did not restore local rotation.");
                Assert(
                    Approximately(fallback.transform.localScale, initialScale),
                    "ResetPose did not restore local scale.");

                var refused = TetherfallHumanAnimation.Attach(refusedRoot.transform);
                Assert(
                    refused == null,
                    "Attach accepted a collider-bearing root with no safe visual descendant.");
                Assert(
                    refusedRoot.GetComponent<HumanProceduralAnimator>() == null,
                    "Attach placed an animator on a collider-bearing root.");

                var chosen = TetherfallHumanAnimation.Attach(choosingRoot.transform);
                Assert(
                    chosen != null,
                    "Attach did not choose a collider-free visual descendant.");
                Assert(
                    chosen.transform == chosenVisual.transform &&
                    chosen.BindingRoot == chosenVisual.transform,
                    "Attach chose the wrong visual descendant.");
                Assert(
                    choosingRoot.GetComponent<HumanProceduralAnimator>() == null,
                    "Attach placed a driver on the collider-bearing parent.");

                var colliderCountAfter =
                    fixture.GetComponentsInChildren<Collider>(true).Length;
                var lightCountAfter =
                    fixture.GetComponentsInChildren<Light>(true).Length;
                Assert(
                    colliderCountAfter == colliderCountBefore,
                    "Human animation added or removed a gameplay collider.");
                Assert(
                    lightCountAfter == lightCountBefore,
                    "Human animation added a Light.");

                Debug.Log(
                    "Human-animation verification passed: idempotent Attach, collider-root " +
                    "refusal/safe descendant selection, primitive fallback binding, exact pose " +
                    "restoration, one driver per visual, and zero added lights/colliders.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fixture);
            }
        }

        [MenuItem(MenuPath, true, 2192)]
        private static bool CanVerifyHumanAnimation()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void InvokeTick(HumanProceduralAnimator animator, float deltaTime)
        {
            var tick = typeof(HumanProceduralAnimator).GetMethod(
                "Tick",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert(tick != null, "Human animation no longer exposes its internal tick path.");
            tick.Invoke(animator, new object[] { deltaTime });
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return Vector3.SqrMagnitude(left - right) < 0.0000001f;
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
