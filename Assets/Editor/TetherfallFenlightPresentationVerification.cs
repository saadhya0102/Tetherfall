using System;
using System.Collections.Generic;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallFenlightPresentationVerification
    {
        [MenuItem("Tetherfall/Verify Fenlight Causeway Presentation")]
        public static void VerifyFenlightCausewayPresentation()
        {
            var gameRoot = new GameObject("Fenlight Presentation Verification");
            FenlightCausewayPresentation presentation = null;
            try
            {
                var game = gameRoot.AddComponent<TetherfallGame>();
                FenlightCausewayPresentationBuilder.Build(game);
                presentation = UnityEngine.Object.FindFirstObjectByType<FenlightCausewayPresentation>();
                Assert(presentation != null, "Fenlight presentation was not built.");
                Assert(presentation.CurrentTier == 0, "Fenlight presentation did not begin at tier zero.");
                Assert(
                    presentation.GetComponentsInChildren<Renderer>(true).Length <= 72,
                    "Fenlight presentation exceeded its renderer budget.");
                Assert(
                    presentation.GetComponentsInChildren<Light>(true).Length == 0,
                    "Fenlight presentation added a Light component.");

                SetFenlightTier(game, 3);
                InvokeUpdate(presentation);
                Assert(presentation.CurrentTier == 3, "Fenlight presentation did not react to restoration tier three.");

                var activeStateCount = 0;
                for (var index = 0; index < presentation.transform.childCount; index++)
                {
                    if (presentation.transform.GetChild(index).gameObject.activeSelf)
                    {
                        activeStateCount++;
                    }
                }

                Assert(activeStateCount == 1, "Fenlight presentation showed more than one tier state at once.");
                Debug.Log("Fenlight Causeway presentation verification passed.");
            }
            finally
            {
                if (presentation != null)
                {
                    UnityEngine.Object.DestroyImmediate(presentation.gameObject);
                }

                UnityEngine.Object.DestroyImmediate(gameRoot);
            }
        }

        private static void SetFenlightTier(TetherfallGame game, int tier)
        {
            var field = typeof(TetherfallGame).GetField(
                "restorationSiteTiers",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(game) is not Dictionary<string, int> tiers)
            {
                throw new InvalidOperationException("Restoration state was unavailable.");
            }

            tiers[RestorationCatalog.FenlightCausewayId] = tier;
        }

        private static void InvokeUpdate(FenlightCausewayPresentation presentation)
        {
            var update = typeof(FenlightCausewayPresentation).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (update == null)
            {
                throw new InvalidOperationException("Fenlight presentation update was unavailable.");
            }

            update.Invoke(presentation, null);
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
