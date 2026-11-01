using System;
using System.Collections.Generic;
using Tetherfall;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tetherfall.Editor
{
    public readonly struct TetherfallQuaterniusPrefabBuildSummary
    {
        public int PrefabCount { get; }
        public int RendererCount { get; }

        public TetherfallQuaterniusPrefabBuildSummary(int prefabCount, int rendererCount)
        {
            PrefabCount = prefabCount;
            RendererCount = rendererCount;
        }
    }

    /// <summary>
    /// Converts the curated raw FBX subset into ordinary Resources-loadable visual prefabs.
    /// Saving over the existing paths preserves prefab GUIDs, so repeated runs are idempotent.
    /// </summary>
    public static class TetherfallQuaterniusPrefabBuilder
    {
        private const string MenuPath = "Tetherfall/Build Quaternius Runtime Prefabs";

        [MenuItem(MenuPath, false, 2185)]
        public static void Build()
        {
            var summary = BuildPrefabs();
            Debug.Log(
                $"Quaternius runtime prefab build completed: {summary.PrefabCount} prefabs and " +
                $"{summary.RendererCount} preserved source renderer(s) at " +
                $"{TetherfallQuaterniusAssetCatalog.OutputFolder}.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static TetherfallQuaterniusPrefabBuildSummary BuildPrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Quaternius runtime prefabs must be built in Edit Mode.");
            }

            EnsureAssetFolder(TetherfallQuaterniusAssetCatalog.OutputFolder);
            var failures = new List<string>();
            var prefabCount = 0;
            var rendererCount = 0;

            foreach (var definition in TetherfallQuaterniusAssetCatalog.All)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(definition.SourceAssetPath);
                if (source == null)
                {
                    failures.Add($"Missing imported model '{definition.SourceAssetPath}'.");
                    continue;
                }

                GameObject instance = null;
                try
                {
                    instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
                    if (instance == null)
                    {
                        instance = Object.Instantiate(source);
                    }

                    if (PrefabUtility.IsPartOfPrefabInstance(instance))
                    {
                        PrefabUtility.UnpackPrefabInstance(
                            instance,
                            PrefabUnpackMode.Completely,
                            InteractionMode.AutomatedAction);
                    }

                    instance.name = definition.ResourceName;
                    instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    instance.transform.localScale = Vector3.one;
                    ClearHideFlags(instance);
                    StripForbiddenComponents(instance);

                    var renderers = instance.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0)
                    {
                        failures.Add(
                            $"Imported model '{definition.SourceAssetPath}' has no renderer to preserve.");
                        continue;
                    }

                    var saved = PrefabUtility.SaveAsPrefabAsset(
                        instance,
                        definition.OutputAssetPath,
                        out var success);
                    if (!success || saved == null)
                    {
                        failures.Add($"Could not save '{definition.OutputAssetPath}'.");
                        continue;
                    }

                    prefabCount++;
                    rendererCount += renderers.Length;
                }
                catch (Exception exception)
                {
                    failures.Add(
                        $"Failed to build '{definition.ResourceName}': " +
                        $"{exception.GetType().Name}: {exception.Message}");
                }
                finally
                {
                    if (instance != null)
                    {
                        Object.DestroyImmediate(instance);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (var definition in TetherfallQuaterniusAssetCatalog.All)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(definition.OutputAssetPath) == null)
                {
                    failures.Add($"Generated prefab is absent: '{definition.OutputAssetPath}'.");
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Quaternius runtime prefab build failed:\n- " +
                    string.Join("\n- ", failures));
            }

            return new TetherfallQuaterniusPrefabBuildSummary(prefabCount, rendererCount);
        }

        private static void EnsureAssetFolder(string path)
        {
            var parts = path.Split('/');
            if (parts.Length == 0 || !string.Equals(parts[0], "Assets", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Asset folder '{path}' must be rooted beneath Assets.",
                    nameof(path));
            }

            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    var guid = AssetDatabase.CreateFolder(current, parts[index]);
                    if (string.IsNullOrEmpty(guid))
                    {
                        throw new InvalidOperationException($"Could not create asset folder '{next}'.");
                    }
                }

                current = next;
            }
        }

        private static void ClearHideFlags(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.hideFlags = HideFlags.None;
                foreach (var component in transform.GetComponents<Component>())
                {
                    if (component != null)
                    {
                        component.hideFlags = HideFlags.None;
                    }
                }
            }
        }

        private static void StripForbiddenComponents(GameObject root)
        {
            DestroyComponents<Collider>(root);
            DestroyComponents<Collider2D>(root);
            DestroyComponents<Light>(root);
            DestroyComponents<Camera>(root);
            DestroyComponents<Animator>(root);
            DestroyComponents<Animation>(root);
        }

        private static void DestroyComponents<T>(GameObject root) where T : Component
        {
            foreach (var component in root.GetComponentsInChildren<T>(true))
            {
                if (component != null)
                {
                    Object.DestroyImmediate(component, true);
                }
            }
        }
    }
}
