using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tetherfall.Editor
{
    public static class TetherfallBuildTools
    {
        private const string BootScenePath = "Assets/Scenes/TetherfallBoot.unity";
        private const string RuntimeMaterialPath = "Assets/Resources/TetherfallRuntimeMaterial.mat";
        private const string RuntimeSkyboxPath = "Assets/Resources/TetherfallRuntimeSkybox.mat";

        [MenuItem("Tetherfall/Create or Refresh Boot Scene")]
        public static void ConfigureProject()
        {
            EnsureRuntimeMaterial();
            EnsureRuntimeSkybox();
            var sceneDirectory = Path.GetDirectoryName(BootScenePath);
            if (!Directory.Exists(sceneDirectory))
            {
                Directory.CreateDirectory(sceneDirectory);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, BootScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScenePath, true)
            };

            PlayerSettings.productName = "Project Tetherfall";
            PlayerSettings.companyName = "Tetherfall Prototype";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.runInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tetherfall boot scene and build settings are ready.");
        }

        private static void EnsureRuntimeMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(RuntimeMaterialPath) != null)
            {
                return;
            }

            Directory.CreateDirectory("Assets/Resources");
            var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            if (shader == null)
            {
                throw new System.Exception("No built-in runtime shader was available for the Tetherfall material.");
            }

            var material = new Material(shader)
            {
                color = Color.white
            };
            AssetDatabase.CreateAsset(material, RuntimeMaterialPath);
        }

        private static void EnsureRuntimeSkybox()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(RuntimeSkyboxPath) != null)
            {
                return;
            }

            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                Debug.LogWarning("The procedural skybox shader was unavailable. Tetherfall will use the camera background.");
                return;
            }

            var skybox = new Material(shader);
            if (skybox.HasProperty("_SkyTint"))
            {
                skybox.SetColor("_SkyTint", new Color(0.31f, 0.54f, 0.62f));
            }

            if (skybox.HasProperty("_GroundColor"))
            {
                skybox.SetColor("_GroundColor", new Color(0.13f, 0.19f, 0.21f));
            }

            if (skybox.HasProperty("_Exposure"))
            {
                skybox.SetFloat("_Exposure", 1.15f);
            }

            AssetDatabase.CreateAsset(skybox, RuntimeSkyboxPath);
        }

        [MenuItem("Tetherfall/Build Windows Vertical Slice")]
        public static void BuildWindows()
        {
            ConfigureProject();
            const string buildDirectory = "Builds";
            if (!Directory.Exists(buildDirectory))
            {
                Directory.CreateDirectory(buildDirectory);
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { BootScenePath },
                locationPathName = Path.Combine(buildDirectory, "ProjectTetherfall.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new System.Exception($"Project Tetherfall Windows build failed: {report.summary.result}");
            }

            Debug.Log($"Project Tetherfall built successfully at {report.summary.outputPath}");
        }
    }
}
