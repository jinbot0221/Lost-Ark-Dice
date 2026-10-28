using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Arkesia.Editor
{
    public static class ArkesiaEditor
    {
        const string Main = "Assets/Arkesia/Scenes/Main.unity";

        [InitializeOnLoadMethod]
        static void OnLoad() { EditorApplication.delayCall += Prepare; }

        [MenuItem("Arkesia/1. Prepare Project")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            // Preserve a referenced material so player builds retain the Standard shader.
            const string materialPath = "Assets/Arkesia/Resources/PrototypeStandard.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null) { Debug.LogError("Arkesia requires the Built-in Render Pipeline Standard shader."); return; }
                AssetDatabase.CreateAsset(new Material(shader), materialPath);
            }
            PlayerSettings.companyName = "ArkesiaPrototype";
            PlayerSettings.productName = "Arkesia Blue Marble";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.defaultScreenWidth = 390;
            PlayerSettings.defaultScreenHeight = 844;
            // Native legacy input matches the supplied uGUI EventSystem and mouse/touch controls.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset"));
            var input = settings.FindProperty("activeInputHandler");
            if (input != null && input.intValue != 0) { input.intValue = 0; settings.ApplyModifiedProperties(); }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Main, true) };
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Arkesia/2. Open Main Scene")]
        public static void OpenMain()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode before opening the main scene."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare(); EditorSceneManager.OpenScene(Main);
        }

        [MenuItem("Arkesia/3. Run Prototype")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) return;
            OpenMain(); EditorApplication.isPlaying = true;
        }

        [MenuItem("Arkesia/4. Capture All 11 Screens (Play Mode)")]
        public static void Capture()
        {
            if (!EditorApplication.isPlaying || ArkesiaApp.Instance == null)
            { Debug.LogWarning("Run Arkesia > Run Prototype first, then keep Game View visible and capture again."); return; }
            ArkesiaApp.Instance.CaptureAll();
        }

        [MenuItem("Arkesia/5. Open Captures Folder")]
        public static void OpenCaptures()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Captures"));
            Directory.CreateDirectory(path); EditorUtility.RevealInFinder(path);
        }

        /// <summary>Optional batch-mode entrypoint after activating Unity locally.</summary>
        public static void ValidateProject()
        {
            Prepare();
            if (GameData.Tiles.Length != 32 || GameData.Prices.Length != 32 || GameData.Screens.Length != 11)
                throw new InvalidOperationException("Game data dimensions are invalid.");
            Debug.Log("Arkesia scripts compiled and basic project data validated. This is not a runtime smoke test.");
        }
    }
}
