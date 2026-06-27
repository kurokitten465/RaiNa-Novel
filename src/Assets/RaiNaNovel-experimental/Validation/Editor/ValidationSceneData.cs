#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VNFramework.Runtime.Commands;
using VNFramework.Runtime.Core;

namespace VNFramework.Validation.Editor
{
    /// <summary>
    /// Creates all VNTimeline assets needed for the end-to-end validation scene.
    ///
    /// Validation story (2-branch, short):
    /// ────────────────────────────────────
    /// [ACT_1]  "The Crossroads"
    ///   ShowBackground  → forest_bg
    ///   ShowCharacter   → "aria" at 0.25 (left)
    ///   PlayBGM         → calm theme
    ///   ShowDialogue    → "Aria: The path splits ahead. Which way do we go?"
    ///   Choice
    ///     ├── "Go left"   → [BRANCH_LEFT]
    ///     └── "Go right"  → [BRANCH_RIGHT]
    ///
    /// [BRANCH_LEFT]  "The Forest Path"
    ///   ShowBackground  → forest_deep_bg
    ///   ShowDialogue    → "Aria: The trees grow dense here..."
    ///   ShowDialogue    → "Aria: But it feels safe. Good choice."
    ///   HideCharacter   → "aria"
    ///   ShowDialogue    → (narrator) "END: You chose the forest."
    ///
    /// [BRANCH_RIGHT]  "The Cliff Path"
    ///   ShowBackground  → cliff_bg
    ///   ShowCharacter   → "aria" at 0.75 (right) — same character, new position
    ///   PlayBGM         → tense theme
    ///   ShowDialogue    → "Aria: The wind is fierce up here!"
    ///   ShowDialogue    → "Aria: I hope we don't regret this."
    ///   ShowDialogue    → (narrator) "END: You chose the cliff."
    ///
    /// All assets are created as sub-assets of their parent VNTimeline for clean packaging.
    /// </summary>
    public static class ValidationSceneData
    {
        private const string ROOT_PATH = "Assets/VNFramework/Validation";

        [MenuItem("VN Framework/Validation/Create Validation Assets", priority = 50)]
        public static void CreateAll()
        {
            EnsureDirectories();

            var branchLeft  = CreateBranchLeft();
            var branchRight = CreateBranchRight();
            var act1        = CreateAct1(branchLeft, branchRight);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Validation] Assets created. Now run:\n" +
                      "VN Framework → Validation → Build Validation Scene");

            Selection.activeObject = act1;
        }

        // ── Act 1 — The Crossroads ─────────────────────────────────────────────

        private static VNTimeline CreateAct1(VNTimeline left, VNTimeline right)
        {
            var tl = CreateTimeline("ACT_1_TheCrossroads", "The Crossroads");

            // Track 0: Background
            // Track 1: Characters
            // Track 2: Dialogue / Choice
            // Track 3: Audio

            float t = 0f;

            AddCmd<ShowBackgroundCommand>(tl, 2, t, 0.5f, cmd =>
            {
                cmd.commandLabel = "Forest BG";
                // backgroundSprite left null — validation uses placeholder colour
            });
            t += 0.5f;

            AddCmd<ShowCharacterCommand>(tl, 1, t, 0.5f, cmd =>
            {
                cmd.commandLabel      = "Show Aria (left)";
                cmd.characterId       = "aria";
                cmd.horizontalPosition = 0.25f;
                cmd.layerOrder        = 0;
                // characterSprite left null — ValidationNullView handles it
            });
            t += 0.5f;

            AddCmd<PlayBGMCommand>(tl, 3, t, 0.1f, cmd =>
            {
                cmd.commandLabel = "Calm Theme";
                cmd.volume       = 0.8f;
                cmd.loop         = true;
                // bgmClip left null — safe, NullVNView / ValidationView handles it
            });

            AddCmd<ShowDialogueCommand>(tl, 0, t, 2f, cmd =>
            {
                cmd.commandLabel  = "Aria — Crossroads";
                cmd.speakerName   = "Aria";
                cmd.dialogueText  = "The path splits ahead. Which way do we go?";
            });
            t += 2f;

            var choice = AddCmd<ChoiceCommand>(tl, 0, t, 1f, cmd =>
            {
                cmd.commandLabel = "Branch Choice";
                cmd.options = new[]
                {
                    new ChoiceCommand.ChoiceOption { label = "Go left",  targetTimeline = left  },
                    new ChoiceCommand.ChoiceOption { label = "Go right", targetTimeline = right }
                };
            });

            return tl;
        }

        // ── Branch Left — The Forest Path ─────────────────────────────────────

        private static VNTimeline CreateBranchLeft()
        {
            var tl = CreateTimeline("BRANCH_LEFT_ForestPath", "The Forest Path");

            float t = 0f;

            AddCmd<ShowBackgroundCommand>(tl, 2, t, 0.5f, cmd =>
                cmd.commandLabel = "Deep Forest BG");
            t += 0.5f;

            AddCmd<ShowDialogueCommand>(tl, 0, t, 2f, cmd =>
            {
                cmd.speakerName  = "Aria";
                cmd.dialogueText = "The trees grow dense here...";
            });
            t += 2f;

            AddCmd<ShowDialogueCommand>(tl, 0, t, 2f, cmd =>
            {
                cmd.speakerName  = "Aria";
                cmd.dialogueText = "But it feels safe. Good choice.";
            });
            t += 2f;

            AddCmd<HideCharacterCommand>(tl, 1, t, 0.3f, cmd =>
            {
                cmd.commandLabel = "Hide Aria";
                cmd.characterId  = "aria";
            });
            t += 0.3f;

            AddCmd<ShowDialogueCommand>(tl, 0, t, 2f, cmd =>
            {
                cmd.speakerName  = string.Empty;
                cmd.dialogueText = "END — You chose the forest path. The journey continues in the shade.";
            });

            return tl;
        }

        // ── Branch Right — The Cliff Path ─────────────────────────────────────

        private static VNTimeline CreateBranchRight()
        {
            var tl = CreateTimeline("BRANCH_RIGHT_CliffPath", "The Cliff Path");

            float t = 0f;

            AddCmd<ShowBackgroundCommand>(tl, 2, t, 0.5f, cmd =>
                cmd.commandLabel = "Cliff BG");
            t += 0.5f;

            AddCmd<ShowCharacterCommand>(tl, 1, t, 0.5f, cmd =>
            {
                cmd.commandLabel       = "Aria (right)";
                cmd.characterId        = "aria";
                cmd.horizontalPosition = 0.75f;
                cmd.layerOrder         = 0;
            });
            t += 0.5f;

            AddCmd<PlayBGMCommand>(tl, 3, t, 0.1f, cmd =>
            {
                cmd.commandLabel = "Tense Theme";
                cmd.volume       = 1f;
                cmd.loop         = true;
            });

            AddCmd<ShowDialogueCommand>(tl, 0, t, 2f, cmd =>
            {
                cmd.speakerName  = "Aria";
                cmd.dialogueText = "The wind is fierce up here!";
            });
            t += 2f;

            AddCmd<ShowDialogueCommand>(tl, 0, t, 2f, cmd =>
            {
                cmd.speakerName  = "Aria";
                cmd.dialogueText = "I hope we don't regret this.";
            });
            t += 2f;

            AddCmd<ShowDialogueCommand>(tl, 0, t, 2f, cmd =>
            {
                cmd.speakerName  = string.Empty;
                cmd.dialogueText = "END — You chose the cliff path. The horizon stretches before you.";
            });

            return tl;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static VNTimeline CreateTimeline(string assetName, string sceneName)
        {
            var tl      = ScriptableObject.CreateInstance<VNTimeline>();
            tl.sceneName = sceneName;
            tl.InitDefaultTracks();

            string path = $"{ROOT_PATH}/Timelines/{assetName}.asset";
            AssetDatabase.CreateAsset(tl, path);
            return tl;
        }

        private static T AddCmd<T>(VNTimeline tl, int trackIdx, float startTime, float duration,
                                    System.Action<T> configure)
            where T : BaseCommand
        {
            var cmd = ScriptableObject.CreateInstance<T>();
            cmd.name       = typeof(T).Name;
            cmd.startTime  = startTime;
            cmd.duration   = duration;
            cmd.trackIndex = trackIdx;

            configure?.Invoke(cmd);

            AssetDatabase.AddObjectToAsset(cmd, tl);

            while (tl.tracks.Count <= trackIdx)
                tl.tracks.Add(new Runtime.Core.VNTrack { trackName = $"Track {tl.tracks.Count}" });

            tl.tracks[trackIdx].commands.Add(cmd);
            EditorUtility.SetDirty(tl);
            return cmd;
        }

        private static void EnsureDirectories()
        {
            EnsureFolder("Assets/VNFramework");
            EnsureFolder("Assets/VNFramework/Validation");
            EnsureFolder("Assets/VNFramework/Validation/Timelines");
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts  = path.Split('/');
                var parent = string.Join("/", parts, 0, parts.Length - 1);
                AssetDatabase.CreateFolder(parent, parts[parts.Length - 1]);
            }
        }
    }
}
#endif
