using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CopAtolyesi.EditorTools
{
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/_Game/Scenes/Main.unity";
        const string TmpPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        [MenuItem("Cop Atolyesi/Projeyi Kur")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
                AssetDatabase.ImportPackage(TmpPackage, false);

            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            settings.FindProperty("activeInputHandler").intValue = 1;
            settings.ApplyModifiedProperties();

            PlayerSettings.productName = "Second Chance Workshop";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            camera.GetComponent<Camera>().orthographic = true;
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = new Color(0.17f, 0.17f, 0.17f);

            new GameObject("Game", typeof(GameBootstrap));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Cop Atolyesi/Windows Build")]
        public static void BuildWindows()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/SecondChanceWorkshop.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            Debug.Log($"Build sonucu: {report.summary.result}");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        [MenuItem("Cop Atolyesi/Windows Dev Build")]
        public static void BuildWindowsDev()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/WindowsDev/CopAtolyesi.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            var ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log($"Build sonucu: {report.summary.result}");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
