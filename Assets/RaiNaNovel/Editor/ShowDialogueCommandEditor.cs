#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VNFramework.Runtime.Commands;

namespace VNFramework.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="ShowDialogueCommand"/>.
    ///
    /// Adds a live preview box below the text fields so designers can see
    /// roughly how the dialogue will look without running the game.
    /// </summary>
    [CustomEditor(typeof(ShowDialogueCommand))]
    internal sealed class ShowDialogueCommandEditor : BaseCommandEditor
    {
        private ShowDialogueCommand Cmd => (ShowDialogueCommand)target;

        protected override void DrawCommandFields()
        {
            // Standard fields
            base.DrawCommandFields();

            EditorGUILayout.Space(8f);
            ScriptEditorStyles.DrawHorizontalLine(0.15f);

            // ── Dialogue preview ───────────────────────────────────────────────
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            var previewStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize  = 12,
                wordWrap  = true,
                padding   = new RectOffset(10, 10, 8, 8)
            };

            using (new EditorGUILayout.VerticalScope(previewStyle))
            {
                // Name plate
                if (!string.IsNullOrEmpty(Cmd.speakerName))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUI.color = new Color(0.4f, 0.75f, 1f);
                        GUILayout.Label($"[ {Cmd.speakerName} ]", EditorStyles.boldLabel);
                        GUI.color = Color.white;
                    }
                }

                // Body text
                var text = string.IsNullOrEmpty(Cmd.dialogueText)
                    ? "(no text)"
                    : Cmd.dialogueText;

                GUILayout.Label(text, previewStyle);

                // Tap indicator
                GUILayout.Space(2f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUI.color = new Color(0.6f, 0.6f, 0.6f);
                    GUILayout.Label("▼ tap to continue", EditorStyles.centeredGreyMiniLabel);
                    GUI.color = Color.white;
                }
            }
        }
    }
}
#endif
