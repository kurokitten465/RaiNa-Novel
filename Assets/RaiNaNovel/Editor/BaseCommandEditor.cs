#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VNFramework.Runtime.Commands;

namespace VNFramework.Editor
{
    /// <summary>
    /// Custom Inspector base for all <see cref="BaseCommand"/> sub-assets.
    ///
    /// Draws a small header showing the command type and execution mode,
    /// then falls through to the default field rendering.
    ///
    /// Concrete command inspectors can override <see cref="DrawCommandFields"/>
    /// to add custom controls without repeating the header boilerplate.
    ///
    /// Applied to BaseCommand so all commands get the header automatically.
    /// Sub-classes may provide their own [CustomEditor] to override further.
    /// </summary>
    [CustomEditor(typeof(BaseCommand), true)]   // true = apply to subclasses
    internal class BaseCommandEditor : UnityEditor.Editor
    {
        protected BaseCommand Command => (BaseCommand)target;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawCommandHeader();
            ScriptEditorStyles.DrawHorizontalLine();
            EditorGUILayout.Space(4f);

            DrawCommandFields();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawCommandFields()
        {
            // Default: draw every serialized field except m_Script.
            var iter = serializedObject.GetIterator();
            iter.NextVisible(true); // enter

            while (iter.NextVisible(false))
                EditorGUILayout.PropertyField(iter, true);
        }

        // ── Header ─────────────────────────────────────────────────────────────

        private void DrawCommandHeader()
        {
            var typeName = Command.GetType().Name.Replace("Command", "");
            bool isInstant = Command.ExecutionMode == CommandExecutionMode.Instant;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(typeName, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                var modeColor = isInstant
                    ? new Color(0.3f, 0.9f, 0.4f)
                    : new Color(0.9f, 0.6f, 0.2f);
                GUI.color = modeColor;
                GUILayout.Label(
                    isInstant ? "● INSTANT" : "◎ AWAIT",
                    EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            // Summary preview
            var summary = Command.EditorSummary;
            if (!string.IsNullOrEmpty(summary) && summary != typeName)
            {
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                GUILayout.Label(summary, EditorStyles.wordWrappedMiniLabel);
                GUI.color = Color.white;
            }
        }
    }
}
#endif
