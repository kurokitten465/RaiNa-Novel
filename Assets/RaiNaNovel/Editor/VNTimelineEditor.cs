#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VNFramework.Runtime.Core;
using VNFramework.Editor.TimelineEditor;

namespace VNFramework.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="VNTimeline"/> assets.
    /// Provides entry points to both the Script Editor and Timeline Editor.
    /// </summary>
    [CustomEditor(typeof(VNTimeline))]
    internal sealed class VNTimelineEditor : UnityEditor.Editor
    {
        private VNTimeline _timeline;
        private void OnEnable() => _timeline = (VNTimeline)target;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("📜 VN Timeline", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                GUILayout.Label(
                    $"{_timeline.tracks.Count} track(s)  •  {_timeline.TotalCommandCount} cmd(s)",
                    EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            ScriptEditorStyles.DrawHorizontalLine();
            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
                if (GUILayout.Button("📝 Script Editor", GUILayout.Height(28f)))
                    ScriptEditorWindow.OpenWith(_timeline);

                GUI.backgroundColor = new Color(0.3f, 0.85f, 0.55f);
                if (GUILayout.Button("⏱ Timeline Editor", GUILayout.Height(28f)))
                    TimelineEditorWindow.OpenWith(_timeline);

                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(8f);
            DrawDefaultInspector();

            if (_timeline.tracks.Count == 0)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox("No tracks yet.", MessageType.Info);
                if (GUILayout.Button("Add Default Tracks"))
                {
                    Undo.RecordObject(_timeline, "Init Default Tracks");
                    _timeline.InitDefaultTracks();
                    EditorUtility.SetDirty(_timeline);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
