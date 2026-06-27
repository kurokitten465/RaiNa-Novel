#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VNFramework.Runtime.Commands;

namespace VNFramework.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="ChoiceCommand"/>.
    ///
    /// Renders the options list with per-option target timeline pickers and
    /// a visual preview of the branch structure.
    /// </summary>
    [CustomEditor(typeof(ChoiceCommand))]
    internal sealed class ChoiceCommandEditor : BaseCommandEditor
    {
        private ChoiceCommand Cmd => (ChoiceCommand)target;

        protected override void DrawCommandFields()
        {
            // Timeline Metadata section (from base)
            DrawTimelineMetadataFields();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Choice Options", EditorStyles.boldLabel);
            ScriptEditorStyles.DrawHorizontalLine(0.15f);
            EditorGUILayout.Space(4f);

            var optionsProp = serializedObject.FindProperty("options");
            if (optionsProp == null)
            {
                EditorGUILayout.HelpBox("Could not find 'options' property.", MessageType.Error);
                return;
            }

            // Draw each option with inline label + target
            for (int i = 0; i < optionsProp.arraySize; i++)
            {
                var element = optionsProp.GetArrayElementAtIndex(i);
                DrawOptionRow(element, i, optionsProp);
            }

            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Add Option", EditorStyles.miniButton))
                {
                    optionsProp.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                }

                GUI.enabled = optionsProp.arraySize > 0;
                if (GUILayout.Button("- Remove Last", EditorStyles.miniButton))
                {
                    optionsProp.arraySize--;
                    serializedObject.ApplyModifiedProperties();
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space(10f);

            // ── Branch preview ─────────────────────────────────────────────────
            DrawBranchPreview();
        }

        private void DrawOptionRow(SerializedProperty element, int index, SerializedProperty listProp)
        {
            var labelProp  = element.FindPropertyRelative("label");
            var targetProp = element.FindPropertyRelative("targetTimeline");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.color = new Color(0.8f, 0.9f, 1f);
                    GUILayout.Label($"Option {index + 1}", EditorStyles.boldLabel, GUILayout.Width(64f));
                    GUI.color = Color.white;

                    GUILayout.FlexibleSpace();

                    // Remove this option
                    if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20f)))
                    {
                        listProp.DeleteArrayElementAtIndex(index);
                        serializedObject.ApplyModifiedProperties();
                        return;
                    }
                }

                EditorGUILayout.PropertyField(labelProp,  new GUIContent("Label"));
                EditorGUILayout.PropertyField(targetProp, new GUIContent("Target Timeline"));

                if (targetProp.objectReferenceValue == null)
                {
                    GUI.color = new Color(1f, 0.8f, 0.4f);
                    EditorGUILayout.LabelField("↳ null target = end of story", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }
            }

            EditorGUILayout.Space(2f);
        }

        private void DrawBranchPreview()
        {
            if (Cmd.options == null || Cmd.options.Length == 0) return;

            EditorGUILayout.LabelField("Branch Map", EditorStyles.boldLabel);
            ScriptEditorStyles.DrawHorizontalLine(0.15f);

            var boxStyle = EditorStyles.helpBox;

            using (new EditorGUILayout.VerticalScope(boxStyle))
            {
                GUILayout.Label("[ Choice ]", EditorStyles.centeredGreyMiniLabel);

                foreach (var opt in Cmd.options)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUI.color = new Color(0.5f, 0.9f, 0.5f);
                        GUILayout.Label("  ├─ ", EditorStyles.label, GUILayout.Width(28f));
                        GUI.color = Color.white;

                        var label = string.IsNullOrEmpty(opt.label) ? "(no label)" : opt.label;
                        var targetName = opt.targetTimeline != null
                            ? opt.targetTimeline.sceneName
                            : "END";

                        GUILayout.Label($"\"{label}\"  →  {targetName}",
                                        EditorStyles.miniLabel);
                    }
                }
            }
        }

        private void DrawTimelineMetadataFields()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("commandLabel"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("trackIndex"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
        }
    }
}
#endif
