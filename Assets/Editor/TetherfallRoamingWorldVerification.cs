using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallRoamingWorldVerification
    {
        private const string MenuPath = "Tetherfall/Verify Roaming World";

        [MenuItem(MenuPath, false, 2190)]
        public static void VerifyRoamingWorld()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Roaming-world verification must run in Edit Mode.");
            }

            if (!TetherfallRoamingWorldDirector.ValidateAuthoredConfiguration(out var summary))
            {
                throw new InvalidOperationException(summary);
            }

            var existingGames = UnityEngine.Object.FindObjectsByType<TetherfallGame>(
                FindObjectsInactive.Include);
            if (existingGames.Length > 0)
            {
                throw new InvalidOperationException(
                    "Roaming-world fixture verification needs an empty Edit Mode scene.");
            }

            var fixture = new GameObject("Roaming World Verification Fixture");
            try
            {
                var game = fixture.AddComponent<TetherfallGame>();
                TetherfallRoamingWorldDirector.EnsureInitialized(game);
                TetherfallRoamingWorldDirector.EnsureInitialized(game);

                var directors = fixture.GetComponentsInChildren<TetherfallRoamingWorldDirector>(true);
                Assert(directors.Length == 1, "Repeated initialization created duplicate directors.");
                var director = directors[0];
                Assert(
                    director.name == TetherfallRoamingWorldDirector.RuntimeRootName,
                    "The roaming director root has an unstable name.");
                Assert(
                    director.RuntimeMerchantCount ==
                    TetherfallRoamingWorldDirector.AuthoredMerchantPopulation,
                    "The runtime merchant population differs from authored data.");
                Assert(
                    director.RuntimePackCount ==
                    TetherfallRoamingWorldDirector.AuthoredPackPopulation,
                    "The runtime pack population differs from authored data.");
                Assert(
                    director.RuntimeMinibossCount ==
                    TetherfallRoamingWorldDirector.AuthoredMinibossPopulation,
                    "The runtime miniboss population differs from authored data.");

                var merchants = director.GetComponentsInChildren<TetherfallRoamingMerchant>(true);
                Assert(
                    merchants.Length == TetherfallRoamingWorldDirector.AuthoredMerchantPopulation,
                    "Not every merchant has exactly one interaction component.");

                var lights = director.GetComponentsInChildren<Light>(true);
                Assert(lights.Length == 0, "Roaming-world content added a Light.");

                var renderers = director.GetComponentsInChildren<Renderer>(true);
                Assert(
                    renderers.Length <= TetherfallRoamingWorldDirector.MaxMerchantRenderers,
                    $"Merchant fixture exceeded its renderer budget ({renderers.Length}/" +
                    $"{TetherfallRoamingWorldDirector.MaxMerchantRenderers}).");

                var authoredMaterials = new HashSet<Material>();
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    var renderer = renderers[rendererIndex];
                    if (renderer == null || renderer.GetComponent<TextMesh>() != null)
                    {
                        continue;
                    }

                    var sharedMaterials = renderer.sharedMaterials;
                    for (var materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                    {
                        var material = sharedMaterials[materialIndex];
                        if (material != null)
                        {
                            authoredMaterials.Add(material);
                        }
                    }
                }

                Assert(
                    authoredMaterials.Count <=
                    TetherfallRoamingWorldDirector.MaxMerchantSharedMaterials,
                    $"Merchants use {authoredMaterials.Count} authored materials; expected no more than " +
                    $"{TetherfallRoamingWorldDirector.MaxMerchantSharedMaterials} shared materials.");

                VerifyAtomicRotatingTrade(game, director, merchants);
                Debug.Log(
                    $"Roaming-world verification passed: {summary} " +
                    $"{renderers.Length} merchant renderers, {authoredMaterials.Count} shared materials, " +
                    "zero added lights, duplicate initialization bounded, same-frame trade guarded.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fixture);
            }
        }

        [MenuItem(MenuPath, true, 2190)]
        private static bool CanVerifyRoamingWorld()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void VerifyAtomicRotatingTrade(
            TetherfallGame game,
            TetherfallRoamingWorldDirector director,
            TetherfallRoamingMerchant[] interactables)
        {
            var statesField = typeof(TetherfallRoamingWorldDirector).GetField(
                "merchants",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var states = statesField?.GetValue(director) as Array;
            Assert(states != null && states.Length > 0, "Merchant runtime state was unavailable.");
            var firstState = states.GetValue(0);
            var stateType = firstState.GetType();
            var visibleField = stateType.GetField(
                "visible",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var rootField = stateType.GetField(
                "root",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var tradeIndexField = stateType.GetField(
                "tradeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert(
                visibleField != null && rootField != null && tradeIndexField != null,
                "Merchant verification fields were unavailable.");

            var root = rootField.GetValue(firstState) as GameObject;
            Assert(root != null, "The first route merchant has no runtime root.");
            visibleField.SetValue(firstState, true);
            root.SetActive(true);

            TetherfallRoamingMerchant lark = null;
            for (var index = 0; index < interactables.Length; index++)
            {
                if (interactables[index] != null &&
                    interactables[index].name.StartsWith("Lark Fen", StringComparison.Ordinal))
                {
                    lark = interactables[index];
                    break;
                }
            }

            Assert(lark != null, "Lark Fen's trade interaction was not found.");
            Assert(
                lark.InteractionPrompt.Contains("2 Timber"),
                "Lark Fen did not begin on the authored rotating trade.");
            game.AddResource(ResourceKind.Timber, 4);
            lark.Interact(game);
            lark.Interact(game);
            Assert(
                game.GetResource(ResourceKind.Timber) == 2 &&
                game.GetResource(ResourceKind.Herbs) == 2,
                "A same-frame repeat bypassed the atomic route-trade guard.");
            Assert(
                (int)tradeIndexField.GetValue(firstState) == 1,
                "A successful route trade did not rotate exactly once.");
            root.SetActive(false);
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
