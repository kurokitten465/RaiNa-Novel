#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VNFramework.Runtime.Player;
using VNFramework.Runtime.View;

namespace VNFramework.Editor
{
    /// <summary>
    /// One-click scene setup for the VN Framework MVP.
    ///
    /// Creates the full Canvas hierarchy so designers never have to hand-build
    /// the scene structure:
    ///
    ///   VNRoot (Canvas)
    ///     ├── BackgroundLayer  (Image)
    ///     ├── CharacterLayer   (RectTransform)
    ///     ├── DialogueBox      (panel + name + text + indicator)
    ///     ├── ChoicePanel      (panel + container)
    ///     └── BGMPlayer        (AudioSource + BGMPlayer)
    ///   VNRuntime (VNPlayer + VNView)
    ///
    /// A default CharacterSlot prefab is also created and wired.
    /// </summary>
    public static class VNSceneBuilder
    {
        [MenuItem("VN Framework/Setup Scene", priority = 0)]
        public static void SetupScene()
        {
            if (EditorUtility.DisplayDialog(
                "VN Framework — Setup Scene",
                "This will create the VN Framework scene hierarchy in the current scene. Continue?",
                "Create", "Cancel"))
            {
                Build();
            }
        }

        private static void Build()
        {
            // ── Canvas root ────────────────────────────────────────────────────
            var canvasGO = new GameObject("VNRoot");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Background layer ───────────────────────────────────────────────
            var bgGO   = CreateFullScreenRect("BackgroundLayer", canvasGO.transform);
            var bgImg  = bgGO.AddComponent<Image>();
            bgImg.color   = Color.black;
            bgImg.raycastTarget = false;

            // ── Character layer ────────────────────────────────────────────────
            var charLayer = CreateFullScreenRect("CharacterLayer", canvasGO.transform);
            // No Image — just a container.

            // ── Dialogue box ───────────────────────────────────────────────────
            var dbGO  = BuildDialogueBox(canvasGO.transform);
            var db    = dbGO.GetComponent<DialogueBox>();

            // ── Choice panel ───────────────────────────────────────────────────
            var cpGO  = BuildChoicePanel(canvasGO.transform);
            var cp    = cpGO.GetComponent<ChoicePanel>();

            // ── BGM Player ─────────────────────────────────────────────────────
            var bgmGO  = new GameObject("BGMPlayer");
            bgmGO.transform.SetParent(canvasGO.transform, false);
            bgmGO.AddComponent<AudioSource>();
            var bgmPlayer = bgmGO.AddComponent<BGMPlayer>();

            // ── Character slot prefab (simple placeholder) ─────────────────────
            var slotPrefab = BuildCharacterSlotPrefab();

            // ── Runtime root (VNPlayer + VNView) ───────────────────────────────
            var runtimeGO = new GameObject("VNRuntime");
            var player    = runtimeGO.AddComponent<VNPlayer>();
            var view      = runtimeGO.AddComponent<VNView>();

            // Wire VNView fields via SerializedObject (clean editor API)
            var so = new SerializedObject(view);
            so.FindProperty("_backgroundImage")     .objectReferenceValue = bgImg;
            so.FindProperty("_characterLayer")      .objectReferenceValue = charLayer.GetComponent<RectTransform>();
            so.FindProperty("_dialogueBox")         .objectReferenceValue = db;
            so.FindProperty("_choicePanel")         .objectReferenceValue = cp;
            so.FindProperty("_bgmPlayer")           .objectReferenceValue = bgmPlayer;
            so.FindProperty("_characterSlotPrefab") .objectReferenceValue = slotPrefab;
            so.FindProperty("_player")              .objectReferenceValue = player;
            so.ApplyModifiedProperties();

            // Select the runtime root so designer can assign a timeline immediately.
            Selection.activeGameObject = runtimeGO;
            Undo.RegisterCreatedObjectUndo(canvasGO,  "VN Framework Setup Scene");
            Undo.RegisterCreatedObjectUndo(runtimeGO, "VN Framework Setup Scene");

            Debug.Log("[VNSceneBuilder] Scene setup complete. Assign a VNTimeline to VNPlayer._startTimeline.");
        }

        // ── Hierarchy helpers ──────────────────────────────────────────────────

        private static GameObject CreateFullScreenRect(string name, Transform parent)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject BuildDialogueBox(Transform parent)
        {
            // Panel (bottom third of screen)
            var panel = new GameObject("DialogueBox");
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0.35f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.75f);

            // Nameplate
            var nameplate = new GameObject("Nameplate");
            nameplate.transform.SetParent(panel.transform, false);
            var npRect = nameplate.AddComponent<RectTransform>();
            npRect.anchorMin = new Vector2(0f, 1f);
            npRect.anchorMax = new Vector2(0f, 1f);
            npRect.pivot     = new Vector2(0f, 0f);
            npRect.anchoredPosition = new Vector2(20f, 4f);
            npRect.sizeDelta = new Vector2(300f, 50f);
            var npImg = nameplate.AddComponent<Image>();
            npImg.color = new Color(0.1f, 0.1f, 0.3f, 0.95f);

            // Speaker name TMP
            var nameTextGO = new GameObject("SpeakerName");
            nameTextGO.transform.SetParent(nameplate.transform, false);
            FillRect(nameTextGO);
            var nameTmp    = nameTextGO.AddComponent<TextMeshProUGUI>();
            nameTmp.text      = "Character Name";
            nameTmp.fontSize  = 28f;
            nameTmp.color     = Color.white;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            SetTMPMargin(nameTmp, 12f, 0f);

            // Dialogue TMP
            var dialogueTextGO = new GameObject("DialogueText");
            dialogueTextGO.transform.SetParent(panel.transform, false);
            var dtRect = dialogueTextGO.AddComponent<RectTransform>();
            dtRect.anchorMin = Vector2.zero;
            dtRect.anchorMax = Vector2.one;
            dtRect.offsetMin = new Vector2(30f, 20f);
            dtRect.offsetMax = new Vector2(-30f, -20f);
            var dialogueTmp   = dialogueTextGO.AddComponent<TextMeshProUGUI>();
            dialogueTmp.text      = "Dialogue text goes here...";
            dialogueTmp.fontSize  = 32f;
            dialogueTmp.color     = Color.white;
            dialogueTmp.alignment = TextAlignmentOptions.TopLeft;

            // Advance indicator (▼)
            var indicator = new GameObject("AdvanceIndicator");
            indicator.transform.SetParent(panel.transform, false);
            var indRect = indicator.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(1f, 0f);
            indRect.anchorMax = new Vector2(1f, 0f);
            indRect.pivot     = new Vector2(1f, 0f);
            indRect.anchoredPosition = new Vector2(-20f, 20f);
            indRect.sizeDelta = new Vector2(40f, 40f);
            var indTmp = indicator.AddComponent<TextMeshProUGUI>();
            indTmp.text      = "▼";
            indTmp.fontSize  = 28f;
            indTmp.color     = Color.white;
            indTmp.alignment = TextAlignmentOptions.Center;

            // Wire DialogueBox component
            var db = panel.AddComponent<DialogueBox>();
            var so = new SerializedObject(db);
            so.FindProperty("_panel")             .objectReferenceValue = panel;
            so.FindProperty("_speakerNameText")   .objectReferenceValue = nameTmp;
            so.FindProperty("_dialogueText")      .objectReferenceValue = dialogueTmp;
            so.FindProperty("_nameplate")         .objectReferenceValue = nameplate;
            so.FindProperty("_advanceIndicator")  .objectReferenceValue = indicator;
            so.ApplyModifiedProperties();

            panel.SetActive(false);
            return panel;
        }

        private static GameObject BuildChoicePanel(Transform parent)
        {
            // Fullscreen dim + centered container
            var panel = new GameObject("ChoicePanel");
            panel.transform.SetParent(parent, false);
            FillRect(panel);
            var dimImg = panel.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.55f);

            // Button container (vertical, centered)
            var container = new GameObject("ButtonContainer");
            container.transform.SetParent(panel.transform, false);
            var cRect = container.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.25f, 0.3f);
            cRect.anchorMax = new Vector2(0.75f, 0.7f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;
            var vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing            = 16f;
            vlg.childAlignment     = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            container.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            // Build button prefab inline (stored as local variable for wiring)
            var btnPrefab  = BuildChoiceButtonPrefab();

            // Wire ChoicePanel
            var cp = panel.AddComponent<ChoicePanel>();
            var so = new SerializedObject(cp);
            so.FindProperty("_panel")           .objectReferenceValue = panel;
            so.FindProperty("_buttonContainer") .objectReferenceValue = container.transform;
            so.FindProperty("_buttonPrefab")    .objectReferenceValue = btnPrefab;
            so.ApplyModifiedProperties();

            panel.SetActive(false);
            return panel;
        }

        private static GameObject BuildChoiceButtonPrefab()
        {
            var go   = new GameObject("ChoiceButton");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 70f);

            var img  = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.35f, 0.95f);

            go.AddComponent<Button>();

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            FillRect(labelGO);
            var tmp    = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = "Option";
            tmp.fontSize  = 30f;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            // Store as a prefab asset in the project.
            const string path = "Assets/VNFramework/Runtime/View/ChoiceButton.prefab";
            EnsureDirectory("Assets/VNFramework/Runtime/View");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildCharacterSlotPrefab()
        {
            var go   = new GameObject("CharacterSlot");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 800f);

            var img = go.AddComponent<Image>();
            img.color            = Color.white;
            img.preserveAspect   = true;
            img.raycastTarget    = false;

            go.AddComponent<CharacterSlot>();

            const string path = "Assets/VNFramework/Runtime/View/CharacterSlot.prefab";
            EnsureDirectory("Assets/VNFramework/Runtime/View");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ── Rect helpers ───────────────────────────────────────────────────────

        private static void FillRect(GameObject go)
        {
            var r = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        private static void SetTMPMargin(TextMeshProUGUI tmp, float left, float right)
        {
            tmp.margin = new Vector4(left, 0f, right, 0f);
        }

        private static void EnsureDirectory(string path)
        {
            var parts  = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
