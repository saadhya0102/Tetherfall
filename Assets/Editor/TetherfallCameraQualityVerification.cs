using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallCameraQualityVerification
    {
        [MenuItem("Tetherfall/Verify Camera Quality", false, 2211)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Camera quality verification is edit-mode only.");
            }

            var originalAccessibility = TetherfallAccessibilityRuntime.GetSnapshot();
            var target = new GameObject("Camera Verification Target");
            var cameraObject = new GameObject("Camera Verification Rig");
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                target.transform.position = Vector3.zero;
                cameraObject.AddComponent<Camera>();
                var rig = cameraObject.AddComponent<ThirdPersonCamera>();

                var pivot = target.transform.position + Vector3.up * 1.15f;
                var fullOffset = Quaternion.Euler(19f, 0f, 0f) *
                                 new Vector3(0f, 2.4f, -7.4f);
                obstacle.name = "Camera Verification Obstacle";
                obstacle.transform.position = pivot + fullOffset.normalized * 3f;
                obstacle.transform.localScale = Vector3.one * 2.2f;
                Physics.SyncTransforms();

                rig.Initialize(target.transform);
                var obstructedDistance = Vector3.Distance(cameraObject.transform.position, pivot);
                Assert(
                    obstructedDistance < fullOffset.magnitude - 1f,
                    "Sphere-cast obstruction did not pull the camera in.");

                UnityEngine.Object.DestroyImmediate(obstacle);
                obstacle = null;
                Physics.SyncTransforms();
                rig.SnapToTarget();
                var restoredDistance = Vector3.Distance(cameraObject.transform.position, pivot);
                Assert(
                    Mathf.Abs(restoredDistance - fullOffset.magnitude) < 0.05f,
                    $"Camera did not restore its configured distance after obstruction cleared " +
                    $"(actual {restoredDistance:0.000}, expected {fullOffset.magnitude:0.000}).");
                Assert(IsFinite(cameraObject.transform.position), "Camera snap produced a non-finite position.");
                Assert(IsFinite(cameraObject.transform.rotation), "Camera snap produced a non-finite rotation.");

                var settings = originalAccessibility.Copy();
                settings.cameraShakeIntensity = 0f;
                TetherfallAccessibilityRuntime.Apply(settings);
                rig.AddImpulse(1f, 0.2f);
                Assert(
                    Mathf.Approximately(ReadFloat(rig, "impulseMagnitude"), 0f),
                    "Zero camera-shake accessibility setting still stored an impulse.");

                settings.cameraShakeIntensity = 0.5f;
                TetherfallAccessibilityRuntime.Apply(settings);
                rig.AddImpulse(1f, 0.2f);
                Assert(
                    Mathf.Abs(ReadFloat(rig, "impulseMagnitude") - 0.5f) < 0.001f,
                    "Camera impulse was not scaled exactly once by accessibility.");

                var hitBuffer = ReadField<RaycastHit[]>(rig, "obstructionHits");
                Assert(
                    hitBuffer != null && hitBuffer.Length >= 16,
                    "Camera obstruction query is missing its reusable non-alloc hit buffer.");

                Debug.Log(
                    "Camera quality verification passed: obstruction pull-in, snap recovery, finite transforms, " +
                    "single-pass shake scaling, and a reusable non-alloc query buffer are valid.");
            }
            finally
            {
                TetherfallAccessibilityRuntime.Apply(originalAccessibility);
                if (obstacle != null)
                {
                    UnityEngine.Object.DestroyImmediate(obstacle);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static float ReadFloat(object instance, string fieldName)
        {
            return ReadField<float>(instance, fieldName);
        }

        private static T ReadField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException($"Camera field '{fieldName}' is missing.");
            }

            return (T)field.GetValue(instance);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z) &&
                   IsFinite(value.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
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
