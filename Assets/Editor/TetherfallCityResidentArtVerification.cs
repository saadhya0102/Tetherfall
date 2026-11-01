using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallCityResidentArtVerification
    {
        [MenuItem("Tetherfall/Verify City Resident Art", false, 2212)]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("City-resident art verification is edit-mode only.");
            }

            var fixture = new GameObject("City Resident Art Verification");
            try
            {
                var gameObject = new GameObject("Material Owner");
                gameObject.transform.SetParent(fixture.transform, false);
                var game = gameObject.AddComponent<TetherfallGame>();

                var actor = new GameObject("Resident Gameplay Root");
                actor.transform.SetParent(fixture.transform, false);
                var visual = TetherfallCityResidentArt.Build(
                    game,
                    actor.transform,
                    new Color(0.24f, 0.63f, 0.58f),
                    "archive threadwright",
                    "npc.verification");

                Assert(visual != null, "Resident art builder returned no visual root.");
                Assert(
                    visual.GetComponentsInChildren<Collider>(true).Length == 0,
                    "Resident visual hierarchy contains gameplay colliders.");
                Assert(
                    visual.GetComponentsInChildren<Light>(true).Length == 0,
                    "Resident visual hierarchy added per-character lights.");
                Assert(
                    visual.GetComponentsInChildren<Renderer>(true).Length >= 22,
                    "Resident silhouette is missing its layered body, face, or role dressing.");

                var animator = TetherfallHumanAnimation.Attach(visual, game);
                Assert(animator != null && animator.IsBound, "Resident rig did not bind to human animation.");
                Assert(
                    animator.BoundSocketCount == 15,
                    $"Resident rig bound {animator.BoundSocketCount} sockets instead of 15.");
                Assert(!animator.UsesFallbackBody, "Resident rig fell back to single-body animation.");

                var visualPosition = visual.localPosition;
                var visualRotation = visual.localRotation;
                animator.SetLocomotion(1f, 0f, 1f);
                animator.TriggerTalk();
                InvokeTick(animator, 0.16f);
                Assert(
                    Vector3.SqrMagnitude(visual.localPosition - visualPosition) < 0.0000001f &&
                    Quaternion.Angle(visual.localRotation, visualRotation) < 0.001f,
                    "Resident animation moved the gameplay-facing visual root instead of its rig sockets.");

                var duplicate = TetherfallHumanAnimation.Attach(visual, game);
                Assert(
                    ReferenceEquals(animator, duplicate) &&
                    visual.GetComponents<HumanProceduralAnimator>().Length == 1,
                    "Repeated resident animation attachment created another driver.");

                var vendor = VendorNpc.Create(
                    game,
                    "vendor.verification",
                    "Verification Merchant",
                    "market broker",
                    new Vector3(4f, 0.9f, 0f),
                    "Verification greeting");
                Assert(vendor != null, "Vendor construction returned null.");
                vendor.transform.SetParent(fixture.transform, true);
                var vendorAnimator = vendor.GetComponentInChildren<HumanProceduralAnimator>(true);
                Assert(
                    vendorAnimator != null &&
                    vendorAnimator.IsBound &&
                    vendorAnimator.BoundSocketCount == 15,
                    "Vendor did not use the articulated resident rig.");
                Assert(
                    vendor.GetComponentsInChildren<Collider>(true).Length == 0,
                    "Vendor hierarchy contains a blocking collider.");

                var crowdRoot = new GameObject("Crowd Resident Root");
                crowdRoot.transform.SetParent(fixture.transform, false);
                var crowdVisual = TetherfallCityResidentArt.BuildCrowd(
                    game,
                    crowdRoot.transform,
                    new Color(0.38f, 0.52f, 0.7f),
                    "npc.crowd.verification");
                var crowdAnimator = TetherfallHumanAnimation.Attach(crowdVisual, game);
                Assert(
                    crowdAnimator != null &&
                    crowdAnimator.IsBound &&
                    crowdAnimator.BoundSocketCount == 15,
                    "Budget resident rig lost articulated socket coverage.");
                Assert(
                    crowdVisual.GetComponentsInChildren<Renderer>(true).Length <= 8,
                    "Budget resident silhouette exceeded its eight-renderer cap.");

                Debug.Log(
                    "City-resident art verification passed: layered 15-socket silhouettes, " +
                    "collider/light safety, articulated resident/vendor animation, an eight-renderer " +
                    "crowd tier, root protection, and idempotent attachment are valid.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fixture);
            }
        }

        private static void InvokeTick(HumanProceduralAnimator animator, float deltaTime)
        {
            var tick = typeof(HumanProceduralAnimator).GetMethod(
                "Tick",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert(tick != null, "Human animator's internal tick path is missing.");
            tick.Invoke(animator, new object[] { deltaTime });
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
