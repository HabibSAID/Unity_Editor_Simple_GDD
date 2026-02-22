#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace BeginnerGDD.Editor
{
    public class GddDocumentWindow : EditorWindow
    {
        // Stores locally on this machine (Editor-only). No files, no ScriptableObjects.
        private const string PrefKey = "GDDDocument";

        private GddData _data;

        private ScrollView _right;
        private Label _docTitleLabel;

        private enum Page { Introduction, Gameplay, ArtVisuals }
        private Page _page = Page.Introduction;

        // Nav (button + accent bar)
        private NavItem _navIntro;
        private NavItem _navGameplay;
        private NavItem _navArt;

        // ===== Theme =====
        private static readonly Color C_BG_CARD = new Color(0f, 0f, 0f, 0.10f);
        private static readonly Color C_BG_CARD_2 = new Color(0f, 0f, 0f, 0.14f);
        private static readonly Color C_FIELD_BG = new Color(0f, 0f, 0f, 0.20f);
        private static readonly Color C_BORDER = new Color(1f, 1f, 1f, 0.10f);

        private static readonly Color C_ACCENT = new Color(0.20f, 0.70f, 1.00f, 1f);
        private static readonly Color C_ACCENT_SOFT = new Color(0.20f, 0.70f, 1.00f, 0.16f);

        private static readonly Color C_INPUT_TEXT = new Color(1f, 1f, 1f, 0.96f);
        private static readonly Color C_LABEL_TEXT = new Color(1f, 1f, 1f, 0.86f);
        private static readonly Color C_HINT_TEXT = new Color(1f, 1f, 1f, 0.70f);

        // ================= DATA =================
        [Serializable]
        private class GddData
        {
            public string authorName = "YOUR_NAME";
            public string workingTitle = "";
            public string concept = "";
            public string genre = "";
            public string targetAudience = "";
            public string targetPlatform = "";

            public string controls = "";
            public string coreGameplayMechanics = "";
            public string uniqueGameplayElements = "";

            public List<ReferenceItem> references = new List<ReferenceItem>();
        }

        [Serializable]
        private class ReferenceItem
        {
            public string referenceUrl = "";
            public string caption = "";
            public string textureGuid = "";
        }

        private class NavItem
        {
            public VisualElement row;
            public VisualElement accent;
            public Button button;
            public Page page;
        }

        // ================= MENU =================
        [MenuItem("Tools/GDD Document")]
        public static void Open()
        {
            var w = GetWindow<GddDocumentWindow>();
            w.titleContent = new GUIContent("GDD Document");
            w.minSize = new Vector2(980, 620);
            w.Show();
        }

        private void OnEnable()
        {
            LoadPrefs();
            BuildUI();
            RenderPage();
        }

        private void OnDisable()
        {
            // Silent autosave on close
            SavePrefs();
        }

        // ================= SAVE / LOAD =================
        private void LoadPrefs()
        {
            try
            {
                var json = EditorPrefs.GetString(PrefKey, "");
                _data = !string.IsNullOrEmpty(json)
                    ? (JsonUtility.FromJson<GddData>(json) ?? new GddData())
                    : new GddData();
            }
            catch
            {
                _data = new GddData();
            }
        }

        private void SavePrefs()
        {
            try
            {
                var json = JsonUtility.ToJson(_data, true);
                EditorPrefs.SetString(PrefKey, json);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void Autosave()
        {
            SavePrefs();
            // no status, no dirty, silent
        }

        // ================= UI =================
        private void BuildUI()
        {
            var root = rootVisualElement;
            root.Clear();

            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;
            root.style.paddingLeft = 14;
            root.style.paddingRight = 14;
            root.style.paddingTop = 14;
            root.style.paddingBottom = 14;

            // ---------- TOP BAR ----------
            var top = CreateCard(18, C_BG_CARD_2);
            top.style.flexDirection = FlexDirection.Row;
            top.style.justifyContent = Justify.SpaceBetween;
            top.style.alignItems = Align.Center;
            top.style.marginBottom = 12;

            var titleCol = new VisualElement();
            titleCol.style.flexDirection = FlexDirection.Column;
            titleCol.style.flexGrow = 1;

            _docTitleLabel = new Label(string.IsNullOrWhiteSpace(_data.workingTitle) ? "Untitled GDD" : _data.workingTitle);
            _docTitleLabel.style.fontSize = 16;
            _docTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            var sub = new Label("Mini Game Design Document • Autosave");
            sub.style.opacity = 0.75f;
            sub.style.fontSize = 11;
            sub.style.marginTop = 3;

            titleCol.Add(_docTitleLabel);
            titleCol.Add(sub);

            top.Add(titleCol);

            // ---------- MAIN ----------
            var main = new VisualElement();
            main.style.flexDirection = FlexDirection.Row;
            main.style.flexGrow = 1;
            main.style.minHeight = 0;

            // NAV
            var nav = CreateCard(18, C_BG_CARD);
            nav.style.width = 280;
            nav.style.minWidth = 220;
            nav.style.maxWidth = 360;
            nav.style.marginRight = 12;
            nav.style.flexShrink = 0;

            var navTitle = new Label("Sections");
            navTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            navTitle.style.marginBottom = 10;
            navTitle.style.opacity = 0.9f;
            nav.Add(navTitle);

            _navIntro = MakeNavItem("Introduction", Page.Introduction);
            _navGameplay = MakeNavItem("Gameplay", Page.Gameplay);
            _navArt = MakeNavItem("Art & Visuals", Page.ArtVisuals);

            nav.Add(_navIntro.row);
            nav.Add(_navGameplay.row);
            nav.Add(_navArt.row);

            var tip = InfoCard("Quick tip", "Write short, clear sentences.\nThis is a tracker, not a novel.");
            tip.style.marginTop = 14;
            nav.Add(tip);

            // CONTENT FRAME
            var contentFrame = CreateCard(18, C_BG_CARD);
            contentFrame.style.flexGrow = 1;
            contentFrame.style.minWidth = 0;
            contentFrame.style.minHeight = 0;

            _right = new ScrollView(ScrollViewMode.Vertical);
            _right.style.flexGrow = 1;
            _right.style.minWidth = 0;
            _right.style.minHeight = 0;
            _right.style.paddingLeft = 12;
            _right.style.paddingRight = 12;
            _right.style.paddingTop = 12;
            _right.style.paddingBottom = 12;

            contentFrame.Add(_right);

            main.Add(nav);
            main.Add(contentFrame);

            root.Add(top);
            root.Add(main);

            HighlightNav();
        }

        private void RenderPage()
        {
            _right.Clear();
            HighlightNav();

            _right.Add(PageHeader(GetPageTitle(), GetPageSubtitle()));

            switch (_page)
            {
                case Page.Introduction: BuildIntro(); break;
                case Page.Gameplay: BuildGameplay(); break;
                case Page.ArtVisuals: BuildArt(); break;
            }
        }

        // ================= PAGES =================
        private void BuildIntro()
        {
            var card = CreateCard(18, C_BG_CARD_2);

            card.Add(BigTextField("By", _data.authorName, v => { _data.authorName = v; Autosave(); }));
            card.Add(BigTextField("Working Title", _data.workingTitle, v =>
            {
                _data.workingTitle = v;
                _docTitleLabel.text = string.IsNullOrWhiteSpace(v) ? "Untitled GDD" : v;
                Autosave();
            }));

            card.Add(BigTextArea("Concept", _data.concept, v => { _data.concept = v; Autosave(); }, minHeight: 300));

            card.Add(BigTextField("Genre", _data.genre, v => { _data.genre = v; Autosave(); }));
            card.Add(BigTextField("Target Audience", _data.targetAudience, v => { _data.targetAudience = v; Autosave(); }));
            card.Add(BigTextField("Target Platform", _data.targetPlatform, v => { _data.targetPlatform = v; Autosave(); }));

            _right.Add(card);
        }

        private void BuildGameplay()
        {
            var card = CreateCard(18, C_BG_CARD_2);

            card.Add(BigTextArea("Controls", _data.controls, v => { _data.controls = v; Autosave(); }, minHeight: 240));
            card.Add(BigTextArea("Core Gameplay Mechanics", _data.coreGameplayMechanics, v => { _data.coreGameplayMechanics = v; Autosave(); }, minHeight: 320));
            card.Add(BigTextArea("Unique Gameplay Elements", _data.uniqueGameplayElements, v => { _data.uniqueGameplayElements = v; Autosave(); }, minHeight: 300));

            _right.Add(card);
        }

        private void BuildArt()
        {
            var card = CreateCard(18, C_BG_CARD_2);

            // Header row
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 10;

            var title = new Label("References");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 13;

            var add = AccentButton("+ Add Reference", () =>
            {
                _data.references.Add(new ReferenceItem());
                Autosave();
                RenderPage();
            });

            headerRow.Add(title);
            headerRow.Add(add);
            card.Add(headerRow);

            var hint = new Label("Drop a Texture2D or add a Reference URL + caption.");
            hint.style.opacity = 0.7f;
            hint.style.marginBottom = 12;
            hint.style.whiteSpace = WhiteSpace.Normal;
            card.Add(hint);

            if (_data.references.Count == 0)
            {
                card.Add(InfoBox("No references yet. Add at least one."));
            }
            else
            {
                for (int i = 0; i < _data.references.Count; i++)
                {
                    int idx = i;
                    var r = _data.references[idx];

                    var sub = CreateCard(16, C_BG_CARD);

                    // Top row
                    var topRow = new VisualElement();
                    topRow.style.flexDirection = FlexDirection.Row;
                    topRow.style.justifyContent = Justify.SpaceBetween;
                    topRow.style.alignItems = Align.Center;
                    topRow.style.marginBottom = 10;

                    var t = new Label($"Reference {idx + 1}");
                    t.style.unityFontStyleAndWeight = FontStyle.Bold;

                    var rm = SoftButton("Remove", () =>
                    {
                        _data.references.RemoveAt(idx);
                        Autosave();
                        RenderPage();
                    });
                    rm.style.opacity = 0.9f;

                    topRow.Add(t);
                    topRow.Add(rm);
                    sub.Add(topRow);

                    // Texture row
                    var texRow = new VisualElement();
                    texRow.style.flexDirection = FlexDirection.Row;
                    texRow.style.alignItems = Align.FlexStart;
                    texRow.style.marginBottom = 10;

                    var left = new VisualElement();
                    left.style.flexGrow = 1;
                    left.style.minWidth = 0;
                    left.style.marginRight = 10;

                    var texField = new ObjectField("Texture (optional)") { objectType = typeof(Texture2D) };
                    Texture2D current = LoadTextureFromGuid(r.textureGuid);
                    texField.value = current;

                    texField.RegisterValueChangedCallback(e =>
                    {
                        var tex = e.newValue as Texture2D;
                        r.textureGuid = tex ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(tex)) : "";
                        Autosave();
                        RenderPage();
                    });

                    StyleObjectField(texField);
                    left.Add(texField);

                    var preview = MakeTexturePreview(current);
                    preview.style.flexShrink = 0;

                    texRow.Add(left);
                    texRow.Add(preview);
                    sub.Add(texRow);

                    sub.Add(BigTextField("Reference URL", r.referenceUrl, v => { r.referenceUrl = v; Autosave(); }));
                    sub.Add(BigTextArea("Caption", r.caption, v => { r.caption = v; Autosave(); }, minHeight: 260));

                    card.Add(sub);
                }
            }

            _right.Add(card);
        }

        // ================= NAV =================
        private NavItem MakeNavItem(string label, Page p)
        {
            var item = new NavItem();
            item.page = p;

            item.row = new VisualElement();
            item.row.style.flexDirection = FlexDirection.Row;
            item.row.style.alignItems = Align.Stretch;
            item.row.style.marginBottom = 8;

            item.accent = new VisualElement();
            item.accent.style.width = 6;
            item.accent.style.marginRight = 10;
            item.accent.style.borderTopLeftRadius = 6;
            item.accent.style.borderBottomLeftRadius = 6;
            item.accent.style.backgroundColor = new Color(0, 0, 0, 0);

            item.button = new Button(() =>
            {
                _page = p;
                RenderPage();
            })
            { text = label };

            item.button.style.flexGrow = 1;
            item.button.style.height = 42;
            item.button.style.unityTextAlign = TextAnchor.MiddleLeft;
            item.button.style.paddingLeft = 12;
            item.button.style.paddingRight = 12;
            item.button.style.borderTopLeftRadius = 14;
            item.button.style.borderTopRightRadius = 14;
            item.button.style.borderBottomLeftRadius = 14;
            item.button.style.borderBottomRightRadius = 14;
            item.button.style.backgroundColor = new Color(0, 0, 0, 0.08f);
            SetSoftBorder(item.button);

            item.row.Add(item.accent);
            item.row.Add(item.button);

            return item;
        }

        private void HighlightNav()
        {
            ApplyNav(_navIntro, _page == Page.Introduction);
            ApplyNav(_navGameplay, _page == Page.Gameplay);
            ApplyNav(_navArt, _page == Page.ArtVisuals);
        }

        private void ApplyNav(NavItem item, bool active)
        {
            if (item == null) return;

            item.button.style.backgroundColor = active ? C_ACCENT_SOFT : new Color(0, 0, 0, 0.08f);
            item.button.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
            item.accent.style.backgroundColor = active ? C_ACCENT : new Color(0, 0, 0, 0);
        }

        // ================= FIELDS =================
        private VisualElement BigTextField(string label, string value, Action<string> onChange)
        {
            var wrap = new VisualElement();
            wrap.style.marginBottom = 12;

            var lab = new Label(label);
            lab.style.opacity = 0.88f;
            lab.style.color = C_LABEL_TEXT;
            lab.style.marginBottom = 6;

            var tf = new TextField { value = value ?? "" };
            tf.style.flexGrow = 1;
            tf.style.minWidth = 0;

            StyleField(tf);
            SetInputTextColor(tf);

            tf.RegisterValueChangedCallback(e => onChange?.Invoke(e.newValue));

            wrap.Add(lab);
            wrap.Add(tf);
            return wrap;
        }

        private VisualElement BigTextArea(string label, string value, Action<string> onChange, int minHeight = 260)
        {
            var wrap = new VisualElement();
            wrap.style.marginBottom = 12;

            var lab = new Label(label);
            lab.style.opacity = 0.88f;
            lab.style.color = C_LABEL_TEXT;
            lab.style.marginBottom = 6;

            var tf = new TextField { multiline = true, value = value ?? "" };
            tf.style.flexGrow = 1;
            tf.style.minWidth = 0;

            StyleField(tf);
            SetInputTextColor(tf);
            ForceBigMultiline(tf, minHeight);

            tf.RegisterValueChangedCallback(e => onChange?.Invoke(e.newValue));

            wrap.Add(lab);
            wrap.Add(tf);
            return wrap;
        }

        // Fix: make multiline text input fill the whole area (not tiny strip)
        private void ForceBigMultiline(TextField tf, int minHeight)
        {
            tf.style.minHeight = minHeight;
            tf.style.height = minHeight;

            var input = tf.Q<VisualElement>("unity-text-input");
            if (input != null)
            {
                input.style.flexGrow = 1;
                input.style.minHeight = minHeight - 20;
                input.style.height = Length.Percent(100);
                input.style.whiteSpace = WhiteSpace.Normal;
            }

            var te = tf.Q<TextElement>();
            if (te != null)
                te.style.whiteSpace = WhiteSpace.Normal;
        }

        // Change typed text color inside the actual input
        private void SetInputTextColor(TextField tf)
        {
            var input = tf.Q<VisualElement>("unity-text-input");
            if (input != null)
            {
                input.style.color = C_INPUT_TEXT;
                input.style.unityFontStyleAndWeight = FontStyle.Normal;
            }
            tf.style.color = C_INPUT_TEXT;
        }

        // ================= WIDGETS =================
        private VisualElement CreateCard(int radius = 18, Color? bg = null)
        {
            var c = new VisualElement();
            c.style.paddingLeft = 16;
            c.style.paddingRight = 16;
            c.style.paddingTop = 16;
            c.style.paddingBottom = 16;
            c.style.marginBottom = 14;
            c.style.backgroundColor = bg ?? C_BG_CARD;
            c.style.borderTopLeftRadius = radius;
            c.style.borderTopRightRadius = radius;
            c.style.borderBottomLeftRadius = radius;
            c.style.borderBottomRightRadius = radius;
            SetSoftBorder(c);
            return c;
        }

        private VisualElement PageHeader(string title, string subtitle)
        {
            var wrap = new VisualElement();
            wrap.style.marginBottom = 12;

            var h1 = new Label(title);
            h1.style.fontSize = 16;
            h1.style.unityFontStyleAndWeight = FontStyle.Bold;

            var h2 = new Label(subtitle);
            h2.style.opacity = 0.7f;
            h2.style.marginTop = 4;
            h2.style.whiteSpace = WhiteSpace.Normal;

            wrap.Add(h1);
            wrap.Add(h2);
            return wrap;
        }

        private Button AccentButton(string text, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.style.height = 32;
            b.style.paddingLeft = 14;
            b.style.paddingRight = 14;
            b.style.borderTopLeftRadius = 12;
            b.style.borderTopRightRadius = 12;
            b.style.borderBottomLeftRadius = 12;
            b.style.borderBottomRightRadius = 12;
            b.style.backgroundColor = C_ACCENT_SOFT;
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            SetSoftBorder(b);
            return b;
        }

        private Button SoftButton(string text, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.style.height = 28;
            b.style.paddingLeft = 12;
            b.style.paddingRight = 12;
            b.style.borderTopLeftRadius = 12;
            b.style.borderTopRightRadius = 12;
            b.style.borderBottomLeftRadius = 12;
            b.style.borderBottomRightRadius = 12;
            b.style.backgroundColor = new Color(1, 1, 1, 0.06f);
            SetSoftBorder(b);
            return b;
        }

        private void StyleField(VisualElement ve)
        {
            ve.style.backgroundColor = C_FIELD_BG;
            ve.style.borderTopLeftRadius = 14;
            ve.style.borderTopRightRadius = 14;
            ve.style.borderBottomLeftRadius = 14;
            ve.style.borderBottomRightRadius = 14;
            ve.style.paddingLeft = 12;
            ve.style.paddingRight = 12;
            ve.style.paddingTop = 10;
            ve.style.paddingBottom = 10;
            SetSoftBorder(ve);
        }

        private void StyleObjectField(ObjectField of)
        {
            of.style.marginBottom = 6;
            of.style.backgroundColor = C_FIELD_BG;
            of.style.borderTopLeftRadius = 14;
            of.style.borderTopRightRadius = 14;
            of.style.borderBottomLeftRadius = 14;
            of.style.borderBottomRightRadius = 14;
            of.style.paddingLeft = 8;
            of.style.paddingRight = 8;
            of.style.paddingTop = 6;
            of.style.paddingBottom = 6;
            SetSoftBorder(of);
        }

        private static VisualElement MakeTexturePreview(Texture2D tex)
        {
            var frame = new VisualElement();
            frame.style.width = 170;
            frame.style.height = 120;
            frame.style.marginBottom = 6;
            frame.style.borderTopLeftRadius = 14;
            frame.style.borderTopRightRadius = 14;
            frame.style.borderBottomLeftRadius = 14;
            frame.style.borderBottomRightRadius = 14;
            frame.style.backgroundColor = new Color(0, 0, 0, 0.18f);
            SetSoftBorder(frame);

            if (tex == null)
            {
                var label = new Label("No Preview");
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.style.opacity = 0.7f;
                label.style.marginTop = 48;
                frame.Add(label);
                return frame;
            }

            var img = new Image();
            img.image = tex;
            img.scaleMode = ScaleMode.ScaleAndCrop;
            img.style.width = Length.Percent(100);
            img.style.height = Length.Percent(100);
            frame.Add(img);

            return frame;
        }

        private static void SetSoftBorder(VisualElement ve)
        {
            ve.style.borderLeftWidth = 1;
            ve.style.borderRightWidth = 1;
            ve.style.borderTopWidth = 1;
            ve.style.borderBottomWidth = 1;
            ve.style.borderLeftColor = C_BORDER;
            ve.style.borderRightColor = C_BORDER;
            ve.style.borderTopColor = C_BORDER;
            ve.style.borderBottomColor = C_BORDER;
        }

        private VisualElement InfoCard(string title, string body)
        {
            var box = CreateCard(16, new Color(1, 1, 1, 0.04f));
            box.style.marginBottom = 0;

            var t = new Label(title);
            t.style.unityFontStyleAndWeight = FontStyle.Bold;
            t.style.marginBottom = 6;

            var b = new Label(body);
            b.style.opacity = 0.75f;
            b.style.color = C_HINT_TEXT;
            b.style.whiteSpace = WhiteSpace.Normal;

            box.Clear();
            box.Add(t);
            box.Add(b);
            return box;
        }

        private VisualElement InfoBox(string msg)
        {
            var box = CreateCard(16, new Color(1, 1, 1, 0.04f));
            box.style.marginBottom = 0;

            var l = new Label(msg);
            l.style.opacity = 0.75f;
            l.style.color = C_HINT_TEXT;
            l.style.whiteSpace = WhiteSpace.Normal;

            box.Clear();
            box.Add(l);
            return box;
        }

        private static Texture2D LoadTextureFromGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private string GetPageTitle()
        {
            return _page switch
            {
                Page.Introduction => "Introduction",
                Page.Gameplay => "Gameplay",
                Page.ArtVisuals => "Art & Visuals",
                _ => "GDD"
            };
        }

        private string GetPageSubtitle()
        {
            return _page switch
            {
                Page.Introduction => "Define the game clearly and quickly.",
                Page.Gameplay => "Describe the core loop, mechanics and what makes it special.",
                Page.ArtVisuals => "Add references + captions to guide the art direction.",
                _ => ""
            };
        }
    }
}
#endif
