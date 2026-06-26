#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VNFramework.Runtime.Player;

namespace VNFramework.Validation.Editor
{
    /// <summary>
    /// Builds the complete end-to-end validation scene programmatically.
    ///
    /// What it creates
    /// ───────────────
    /// • A new Unity scene "VN_Validation"
    /// • A Camera (orthographic, black background)
    /// • A GameObject with VNPlayer + ValidationView
    ///   → ValidationView implements IVNView with IMGUI rendering
    ///   → VNPlayer._startTimeline = ACT_1_TheCrossroads
    ///   → VNPlayer._autoPlay = true
    ///
    /// Prerequisites
    /// ─────────────
    /// Run "VN Framework → Validation → Create Validation Assets" first
    /// so the VNTimeline assets exist at Assets/VNFramework/Validation/Timelines/.
    ///
    /// Success criteria (from the project spec)
    /// ──────────────────────────────────────────
    /// A designer presses Play and experiences a short branching VN scene
    /// without writing any code.
    /// </summary>
    public static class ValidationSceneBuilder
    {
        private const string TIMELINE_ROOT = "Assets/VNFramework/Validation/Timelines";
        private const string ACT1_PATH     = TIMELINE_ROOT + "/ACT_1_TheCrossroads.asset";
        private const string SCENE_PATH    = "Assets/VNFramework/Validation/VN_Validation.unity";

        [MenuItem("VN Framework/Validation/Build Validation Scene", priority = 51)]
        public static void BuildScene()
        {
            // Verify assets exist
            var act1 = AssetDatabase.LoadAssetAtPath<Runtime.Core.VNTimeline>(ACT1_PATH);
            if (act1 == null)
            {
                EditorUtility.DisplayDialog(
                    "Missing Validation Assets",
                    "Please run:\nVN Framework → Validation → Create Validation Assets\nbefore building the scene.",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Build Validation Scene",
                $"This will create a new scene at:\n{SCENE_PATH}\n\nSave any unsaved work first.",
                "Build", "Cancel"))
                return;

            // ── New scene ──────────────────────────────────────────────────────
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                                                    NewSceneMode.Single);

            // ── Camera ────────────────────────────────────────────────────────
            var camGO     = new GameObject("Main Camera");
            var cam       = camGO.AddComponent<Camera>();
            cam.clearFlags        = CameraClearFlags.SolidColor;
            cam.backgroundColor   = Color.black;
            cam.orthographic      = true;
            cam.orthographicSize  = 5f;
            cam.depth             = -1;
            camGO.tag             = "MainCamera";
            camGO.AddComponent<AudioListener>();

            // ── VN Runtime ────────────────────────────────────────────────────
            var runtimeGO  = new GameObject("VNRuntime");
            var player     = runtimeGO.AddComponent<VNPlayer>();
            var view       = runtimeGO.AddComponent<ValidationView>();

            // Wire: set _startTimeline and _autoPlay via SerializedObject
            var so = new SerializedObject(player);
            so.FindProperty("_startTimeline").objectReferenceValue = act1;
            so.FindProperty("_autoPlay")     .boolValue            = true;
            so.ApplyModifiedProperties();

            // ── Save scene ────────────────────────────────────────────────────
            EnsureFolder("Assets/VNFramework/Validation");
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.Refresh();

            Debug.Log($"[ValidationSceneBuilder] Scene saved to {SCENE_PATH}\n" +
                       "Press Play to run the end-to-end validation.");

            EditorUtility.DisplayDialog(
                "Validation Scene Ready",
                $"Scene saved to:\n{SCENE_PATH}\n\n" +
                "Press Play to run the validation.\n\n" +
                "Expected flow:\n" +
                "1. Background + character appear\n" +
                "2. Dialogue: \"The path splits ahead...\"\n" +
                "3. Choice panel: Go left / Go right\n" +
                "4. Branch plays to END",
                "Open & Play");

            // Open it
            EditorSceneManager.OpenScene(SCENE_PATH);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            var parent = string.Join("/", parts, 0, parts.Length - 1);
            AssetDatabase.CreateFolder(parent, parts[parts.Length - 1]);
        }
    }
}
#endif
