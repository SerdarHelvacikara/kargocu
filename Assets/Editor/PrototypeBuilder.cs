using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KurmaliKargo.Editor
{
    public static class PrototypeBuilder
    {
        [MenuItem("Kurmalı Kargo/Prepare prototype scene")]
        public static void Prepare()
        {
            PlayerSettings.companyName = "KurmaliKargoPrototype";
            PlayerSettings.productName = "Kurmalı Kargo";
            PlayerSettings.bundleVersion = "0.10.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            var serialized = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = serialized.FindProperty("activeInputHandler");
            if(input != null) input.intValue=0;
            var analytics = serialized.FindProperty("submitAnalytics");
            if (analytics != null) analytics.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // Materials are procedural; explicitly retain their shader in standalone builds.
            Directory.CreateDirectory("Assets/Resources");
            const string materialPath = "Assets/Resources/NeighborhoodShader.mat";
            if (!AssetDatabase.LoadAssetAtPath<Material>(materialPath))
                AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")), materialPath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Kurmalı Kargo — press Play").AddComponent<CourierGame>();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Neighborhood.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Neighborhood.unity",true) };
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Kurmalı Kargo/Build Windows prototype")]
        public static void Build()
        {
            Prepare();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath,"../../Windows-v010/KurmaliKargo.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[]{"Assets/Scenes/Neighborhood.unity"},
                locationPathName = output, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if(report.summary.result != BuildResult.Succeeded)
                throw new Exception("Prototype build failed: " + report.summary.result + " errors: " + report.summary.totalErrors);
            Debug.Log("KARGO_BUILD_OK: " + output + " bytes=" + report.summary.totalSize);
        }
    }
}
