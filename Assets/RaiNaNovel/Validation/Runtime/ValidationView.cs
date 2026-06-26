using System;
using System.Collections.Generic;
using UnityEngine;
using VNFramework.Runtime.Core;
using VNFramework.Runtime.Player;

namespace VNFramework.Validation
{
    /// <summary>
    /// Self-contained IVNView for end-to-end validation.
    /// Renders entirely in OnGUI — no Canvas, no prefabs required.
    /// Exposes ChoicesVisible + SimulateChoice/SimulateTap for the checklist.
    /// </summary>
    [AddComponentMenu("VN Framework/Validation View (Dev Only)")]
    public sealed class ValidationView : MonoBehaviour, IVNView
    {
        // ── Checklist surface ──────────────────────────────────────────────────
        public bool ChoicesVisible => _choicesVisible;

        public void SimulateChoice(int index)
        {
            if (!_choicesVisible || _choiceSelected == null) return;
            if (_choiceLabels == null || index >= _choiceLabels.Length) return;
            var cb = _choiceSelected;
            HideChoices();
            cb.Invoke(index);
        }

        public void SimulateTap()
        {
            if (!_dialogueVisible || _dialogueTap == null) return;
            var cb = _dialogueTap;
            HideDialogue();
            cb.Invoke();
        }

        // ── State ──────────────────────────────────────────────────────────────
        private Color  _bgColor  = new Color(0.05f, 0.08f, 0.15f);
        private Sprite _bgSprite;

        private readonly Dictionary<string, CharacterEntry> _characters = new();

        private bool   _dialogueVisible;
        private string _speakerName;
        private string _dialogueText;
        private Action _dialogueTap;

        private bool        _choicesVisible;
        private string[]    _choiceLabels;
        private Action<int> _choiceSelected;

        private readonly Queue<string> _log = new();
        private const int LOG_MAX = 8;

        // ── Wiring ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            var player = GetComponent<VNPlayer>() ?? FindObjectOfType<VNPlayer>();
            if (player != null)
            {
                player.SetView(this);
                player.OnStateChanged   += s  => AddLog($"[Player] {s}");
                player.OnTimelineLoaded += tl => AddLog($"[Scene] {tl.sceneName}");
                player.OnFinished       += () => AddLog("[Player] Finished OK");
                AddLog("ValidationView ready.");
            }
            else AddLog("[ERROR] No VNPlayer found.");
        }

        // ── IVNView ────────────────────────────────────────────────────────────
        public void SetBackground(Sprite sprite)
        {
            _bgSprite = sprite;
            _bgColor  = new Color(
                UnityEngine.Random.Range(0.04f, 0.20f),
                UnityEngine.Random.Range(0.04f, 0.20f),
                UnityEngine.Random.Range(0.08f, 0.30f));
            AddLog($"BG: {sprite?.name ?? "colour placeholder"}");
        }

        public void ShowCharacter(string id, Sprite sprite, float pos, int layer)
        {
            _characters[id] = new CharacterEntry
                { Id=id, Sprite=sprite, Position=pos, Layer=layer, Visible=true };
            AddLog($"Show [{id}] @ {pos:F2}");
        }

        public void HideCharacter(string id)
        {
            if (_characters.TryGetValue(id, out var e)) { e.Visible=false; _characters[id]=e; }
            AddLog($"Hide [{id}]");
        }

        public void ShowDialogue(string speaker, string text, Action onTap)
        {
            _dialogueVisible=true; _speakerName=speaker;
            _dialogueText=text; _dialogueTap=onTap;
            var p = text?.Length > 48 ? text[..48]+"…" : text;
            AddLog($"[{speaker}]: {p}");
        }

        public void HideDialogue()  { _dialogueVisible=false; _dialogueTap=null; }

        public void ShowChoices(string[] opts, Action<int> cb)
        {
            _choicesVisible=true; _choiceLabels=opts; _choiceSelected=cb;
            AddLog($"Choice: {string.Join(" / ", opts)}");
        }

        public void HideChoices() { _choicesVisible=false; _choiceSelected=null; }

        public void PlayBGM(AudioClip clip, float vol, bool loop) =>
            AddLog($"BGM: {clip?.name ?? "null"} vol={vol}");

        public void StopBGM() => AddLog("StopBGM");

        // ── Rendering ──────────────────────────────────────────────────────────
        private void OnGUI()
        {
            float sw = Screen.width, sh = Screen.height;
            DrawBg(sw, sh);
            DrawCharacters(sw, sh);
            if (_dialogueVisible) DrawDialogue(sw, sh);
            if (_choicesVisible)  DrawChoices(sw, sh);
            DrawLog();
        }

        private void DrawBg(float sw, float sh)
        {
            if (_bgSprite != null)
                GUI.DrawTexture(new Rect(0,0,sw,sh), _bgSprite.texture, ScaleMode.ScaleAndCrop);
            else
                Fill(new Rect(0,0,sw,sh), _bgColor);
        }

        private void DrawCharacters(float sw, float sh)
        {
            float cw = sw*0.18f, ch = sh*0.55f, cy = sh - ch - sh*0.30f;
            foreach (var kv in _characters)
            {
                var e = kv.Value; if (!e.Visible) continue;
                var r = new Rect(e.Position*sw - cw*0.5f, cy, cw, ch);
                if (e.Sprite != null) GUI.DrawTexture(r, e.Sprite.texture, ScaleMode.ScaleToFit);
                else { Fill(r, new Color(0.3f,0.3f,0.5f,0.7f)); GUI.Label(r, $"[{e.Id}]", Ctr(16,Color.white)); }
            }
        }

        private void DrawDialogue(float sw, float sh)
        {
            float bh=sh*0.28f, by=sh-bh-12f;
            var box = new Rect(16, by, sw-32f, bh);
            Fill(box, new Color(0,0,0,0.82f));
            Bord(box, new Color(0.4f,0.6f,0.9f,0.8f), 2f);

            if (!string.IsNullOrEmpty(_speakerName))
            {
                var np = new Rect(box.x+12, box.y-28, 200, 26);
                Fill(np, new Color(0.15f,0.25f,0.5f,0.95f));
                GUI.Label(np, $"  {_speakerName}", Lft(15,Color.white));
            }

            GUI.Label(new Rect(box.x+20,box.y+12,box.width-40,box.height-40),
                      _dialogueText, Wrp(16,Color.white));
            GUI.Label(new Rect(box.xMax-72,box.yMax-26,60,18),
                      "▼ tap", Lft(12,new Color(0.6f,0.6f,0.6f)));

            if (Event.current.type==EventType.MouseDown && box.Contains(Event.current.mousePosition))
            { SimulateTap(); Event.current.Use(); }
        }

        private void DrawChoices(float sw, float sh)
        {
            Fill(new Rect(0,0,sw,sh), new Color(0,0,0,0.55f));
            float bw=Mathf.Min(sw*0.5f,480f), bh=52f, gap=12f;
            int n = _choiceLabels?.Length ?? 0;
            float sy=(sh - (n*bh+(n-1)*gap))*0.5f, sx=(sw-bw)*0.5f;
            for (int i=0; i<n; i++)
            {
                var r = new Rect(sx, sy+i*(bh+gap), bw, bh);
                bool hov = r.Contains(Event.current.mousePosition);
                Fill(r, hov ? new Color(0.25f,0.45f,0.85f,0.97f) : new Color(0.12f,0.18f,0.38f,0.95f));
                Bord(r, new Color(0.4f,0.6f,1f,hov?1f:0.5f), 1.5f);
                GUI.Label(r, _choiceLabels[i], Ctr(17,Color.white));
                if (Event.current.type==EventType.MouseDown && r.Contains(Event.current.mousePosition))
                { int idx=i; SimulateChoice(idx); Event.current.Use(); break; }
            }
        }

        private void DrawLog()
        {
            var lines = new List<string>(_log);
            float lh=17; var lb=new Rect(8,8,462,lines.Count*lh+8);
            Fill(lb, new Color(0,0,0,0.52f));
            var s=Lft(11,new Color(0.7f,1f,0.7f));
            for (int i=0; i<lines.Count; i++)
                GUI.Label(new Rect(12,8+i*lh,450,lh), lines[i], s);
        }

        // ── Draw helpers ───────────────────────────────────────────────────────
        private static void Fill(Rect r, Color c)
        { var o=GUI.color; GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=o; }

        private static void Bord(Rect r, Color c, float t)
        {
            Fill(new Rect(r.x,r.y,r.width,t),c);
            Fill(new Rect(r.x,r.yMax-t,r.width,t),c);
            Fill(new Rect(r.x,r.y,t,r.height),c);
            Fill(new Rect(r.xMax-t,r.y,t,r.height),c);
        }

        private static GUIStyle Ctr(int sz, Color c) => new GUIStyle(GUI.skin.label)
            { fontSize=sz, alignment=TextAnchor.MiddleCenter, normal={textColor=c} };
        private static GUIStyle Lft(int sz, Color c) => new GUIStyle(GUI.skin.label)
            { fontSize=sz, alignment=TextAnchor.MiddleLeft, normal={textColor=c} };
        private static GUIStyle Wrp(int sz, Color c) => new GUIStyle(GUI.skin.label)
            { fontSize=sz, wordWrap=true, alignment=TextAnchor.UpperLeft, normal={textColor=c} };

        private void AddLog(string m) { _log.Enqueue(m); while (_log.Count>LOG_MAX) _log.Dequeue(); }

        private struct CharacterEntry
        { public string Id; public Sprite Sprite; public float Position; public int Layer; public bool Visible; }
    }
}
