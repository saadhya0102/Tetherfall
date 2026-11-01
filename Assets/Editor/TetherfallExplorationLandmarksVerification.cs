using System;
using System.Collections.Generic;
using System.Reflection;
using Tetherfall;
using UnityEditor;
using UnityEngine;

namespace Tetherfall.Editor
{
    public static class TetherfallExplorationLandmarksVerification
    {
        [MenuItem("Tetherfall/Verify Exploration Landmarks")]
        public static void VerifyExplorationLandmarks()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exploration landmark verification must run in Edit Mode.");
            }

            Assert(
                TetherfallExplorationLandmarks.ValidateAuthoring(out var authoringReport),
                "Authored landmark validation failed: " + authoringReport);

            var fixture = new GameObject("Exploration Landmark Verification Fixture");
            var transientMaterials = new HashSet<Material>();
            try
            {
                var game = fixture.AddComponent<TetherfallGame>();
                var firstRoot = TetherfallExplorationLandmarks.Build(game);
                Assert(firstRoot != null, "The landmark builder did not create its root.");
                var first = Snapshot.Capture(firstRoot, transientMaterials);

                var secondRoot = TetherfallExplorationLandmarks.Build(game);
                Assert(
                    secondRoot == firstRoot,
                    "A duplicate Build call returned a different landmark root.");
                var second = Snapshot.Capture(secondRoot, transientMaterials);

                Assert(
                    first.Renderers == second.Renderers &&
                    first.Colliders == second.Colliders &&
                    first.Materials == second.Materials &&
                    first.Lights == second.Lights &&
                    first.Markers == second.Markers &&
                    first.Rewards == second.Rewards,
                    "A duplicate Build call changed the landmark hierarchy.");
                Assert(
                    CountDirectRoots(game.transform) == 1,
                    "The game owner has more than one exploration-landmark root.");
                Assert(
                    TetherfallExplorationLandmarks.ValidateBuiltRoot(firstRoot, out var builtReport),
                    "Built landmark validation failed: " + builtReport);

                var buildStats = firstRoot.GetComponent<ExplorationLandmarkBuildRoot>();
                Assert(buildStats != null, "The landmark root is missing its build-budget marker.");
                Assert(
                    buildStats.AuthoredPrimitiveRenderers <=
                    TetherfallExplorationLandmarks.MaxRendererBudget,
                    "Authored primitive count exceeds the renderer budget.");
                Assert(
                    buildStats.AuthoredSolidColliders <=
                    TetherfallExplorationLandmarks.MaxColliderBudget,
                    "Authored solid count exceeds the collider budget.");

                Debug.Log(
                    "Exploration landmark verification passed: " + authoringReport + "; " +
                    builtReport + $"; {buildStats.AuthoredPrimitiveRenderers} authored primitives " +
                    $"and {buildStats.AuthoredSolidColliders} authored solid colliders. " +
                    "Duplicate Build remained inert.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fixture);
                foreach (var material in transientMaterials)
                {
                    if (material != null && !EditorUtility.IsPersistent(material))
                    {
                        UnityEngine.Object.DestroyImmediate(material);
                    }
                }

                ResetStatics(typeof(Enemy));
                ResetStatics(typeof(TetherfallGame));
            }
        }

        private static int CountDirectRoots(Transform owner)
        {
            var count = 0;
            for (var index = 0; index < owner.childCount; index++)
            {
                if (owner.GetChild(index).name == TetherfallExplorationLandmarks.RootName)
                {
                    count++;
                }
            }

            return count;
        }

        private static void ResetStatics(Type type)
        {
            var reset = type.GetMethod(
                "ResetStatics",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (reset != null && reset.GetParameters().Length == 0)
            {
                reset.Invoke(null, null);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private readonly struct Snapshot
        {
            public readonly int Renderers;
            public readonly int Colliders;
            public readonly int Materials;
            public readonly int Lights;
            public readonly int Markers;
            public readonly int Rewards;

            private Snapshot(
                int renderers,
                int colliders,
                int materials,
                int lights,
                int markers,
                int rewards)
            {
                Renderers = renderers;
                Colliders = colliders;
                Materials = materials;
                Lights = lights;
                Markers = markers;
                Rewards = rewards;
            }

            public static Snapshot Capture(
                GameObject root,
                HashSet<Material> transientMaterials)
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                var materials = new HashSet<Material>();
                foreach (var renderer in renderers)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            materials.Add(material);
                            transientMaterials.Add(material);
                        }
                    }
                }

                var enabledColliders = 0;
                foreach (var itemCollider in root.GetComponentsInChildren<Collider>(true))
                {
                    if (itemCollider.enabled)
                    {
                        enabledColliders++;
                    }
                }

                return new Snapshot(
                    renderers.Length,
                    enabledColliders,
                    materials.Count,
                    root.GetComponentsInChildren<Light>(true).Length,
                    root.GetComponentsInChildren<ExplorationLandmarkMarker>(true).Length,
                    root.GetComponentsInChildren<ExplorationReward>(true).Length);
            }
        }
    }
}
