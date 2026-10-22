using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DroneTrainer.UI
{
    public enum TextStyle { Body, Display, Label, Mono }
    public enum ButtonStyle { Primary, Ghost, Ok, Danger, Disabled, Round }
    public enum ChipStyle { Neutral, Accent, Ok, Warn, Danger }

    /// <summary>
    /// Factory for every UI element. Used by the editor scene builders (static layout)
    /// and at runtime for data-driven lists like checklist rows (Factory pattern, SRS §5).
    /// Units: canvas reference 2340×1080 (match height). 1 design em ≈ 32 units.
    /// </summary>
    public static class UIFactory
    {
        public const float TouchSize = 132f;     // ≈48dp
        public const float GutterX = 72f;
        public const float GutterY = 40f;
        public static UITheme T => UITheme.Current;

        // ---------------------------------------------------------------- rect helpers
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchor to a point (ax, ay), pivot (px, py), size, offset.</summary>
        public static RectTransform At(this RectTransform rt, float ax, float ay, float px, float py,
                                       float w, float h, float x = 0, float y = 0)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(px, py);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            return rt;
        }

        /// <summary>Stretch inside the parent with edge insets.</summary>
        public static RectTransform Fill(this RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static RectTransform Span(this RectTransform rt, float minX, float minY, float maxX, float maxY,
                                         float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static T Size<T>(this T c, float w = -1, float h = -1, float flex = -1) where T : Component
        {
            var le = c.GetComponent<LayoutElement>();
            if (!le) le = c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0) { le.preferredWidth = w; le.minWidth = w; }
            if (h >= 0) { le.preferredHeight = h; le.minHeight = h; }
            if (flex >= 0) le.flexibleWidth = flex;
            return c;
        }

        public static RectTransform RT(this Component c) => (RectTransform)c.transform;

        // ---------------------------------------------------------------- primitives
        public static Image Img(Transform parent, string name, Color color, Sprite sprite = null, bool raycast = false)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.type = sprite && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Panel(Transform parent, string name, bool glass = false, bool large = false, bool outline = true)
        {
            bool art = !glass && T.panelArt;
            var img = Img(parent, name, art ? Color.white : glass ? T.glass : UITheme.A(T.panel, 0.94f),
                          art ? T.panelArt : large ? T.roundedLarge : T.rounded, true);
            if (art) outline = false;     // the art has its own border
            // Outline duplicates the sprite; on translucent glass those copies tint the fill, so glass gets none.
            if (outline && !glass)
            {
                var o = img.gameObject.AddComponent<Outline>();
                o.effectColor = UITheme.A(T.line, 0.9f);
                o.effectDistance = new Vector2(2, -2);
            }
            return img;
        }

        /// <summary>Thin coloured bar on a panel's left edge (status accent for glass cards).</summary>
        public static Image AccentBar(Transform panel, Color color)
        {
            var bar = Img(panel, "Accent", color, T.pill);
            var rt = bar.RT();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.offsetMin = new Vector2(12, 20);
            rt.offsetMax = new Vector2(20, -20);
            bar.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return bar;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
                                           TextStyle style = TextStyle.Body,
                                           TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var tmp = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (T.font) tmp.font = T.font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            switch (style)
            {
                case TextStyle.Display:
                    tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                    tmp.characterSpacing = 3;
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    break;
                case TextStyle.Label:
                    tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                    tmp.characterSpacing = 10;
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    break;
                case TextStyle.Mono:
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.characterSpacing = 4;
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    break;
            }
            return tmp;
        }

        public static Image Icon(Transform parent, string name, string icon, float size, Color color)
        {
            var img = Img(parent, name, color, T.Icon(icon));
            img.preserveAspect = true;
            img.RT().sizeDelta = new Vector2(size, size);
            img.Size(size, size);
            if (!img.sprite) img.color = Color.clear;
            return img;
        }

        // ---------------------------------------------------------------- composites
        public static UIButton Button(Transform parent, string name, string label, ButtonStyle style,
                                      string icon = null, float height = TouchSize)
        {
            var bg = Img(parent, name, T.accent, style == ButtonStyle.Round ? T.circle : T.rounded, true);
            var btn = bg.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(1, 1, 1, 0.55f);
            btn.colors = colors;
            var ub = bg.gameObject.AddComponent<UIButton>();
            ub.button = btn;
            ub.background = bg;

            var row = Rect("Row", bg.transform).Fill(28, 0, 28, 0);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 14;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;

            if (!string.IsNullOrEmpty(icon)) ub.icon = Icon(row, "Icon", icon, style == ButtonStyle.Round ? 52 : 40, Color.white);
            if (!string.IsNullOrEmpty(label))
            {
                ub.label = Text(row, "Label", label, 30, Color.white, TextStyle.Display, TextAlignmentOptions.Center);
                ub.label.textWrappingMode = TextWrappingModes.NoWrap;
            }
            ub.SetStyle(style);
            bg.Size(-1, height);
            return ub;
        }

        public static UIButton RoundButton(Transform parent, string name, string icon, float size = 120)
        {
            var b = Button(parent, name, null, ButtonStyle.Round, icon, size);
            b.RT().sizeDelta = new Vector2(size, size);
            b.Size(size, size);
            var row = b.transform.Find("Row") as RectTransform;
            if (row) row.Fill();
            return b;
        }

        public static UIChip Chip(Transform parent, string name, string text, ChipStyle style, string icon = null)
        {
            var bg = Img(parent, name, T.panel2, T.pill);
            var chip = bg.gameObject.AddComponent<UIChip>();
            chip.background = bg;
            var h = bg.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(22, 22, 8, 8);
            h.spacing = 8;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            var fit = bg.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (!string.IsNullOrEmpty(icon)) chip.icon = Icon(bg.transform, "Icon", icon, 28, Color.white);
            chip.label = Text(bg.transform, "Label", text, 22, Color.white, TextStyle.Label, TextAlignmentOptions.Center);
            chip.label.characterSpacing = 4;
            chip.Set(text, style);
            return chip;
        }

        /// <summary>Glass status pill used over the camera (timer, errors, tracking).</summary>
        public static UIChip Pill(Transform parent, string name, string text, string icon, Color iconColor)
        {
            var c = Chip(parent, name, text, ChipStyle.Neutral, icon);
            c.background.color = T.glass;
            c.label.color = T.text;
            c.label.fontSize = 28;
            c.label.fontStyle = FontStyles.Bold;
            c.label.characterSpacing = 2;
            var h = c.GetComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(26, 26, 18, 18);
            if (c.icon) c.icon.color = iconColor;
            c.lockColors = true;
            return c;
        }

        public static RectTransform HStack(Transform parent, string name, float spacing,
                                           TextAnchor align = TextAnchor.MiddleLeft, RectOffset padding = null)
        {
            var rt = Rect(name, parent);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.padding = padding ?? new RectOffset();
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            return rt;
        }

        public static RectTransform VStack(Transform parent, string name, float spacing,
                                           TextAnchor align = TextAnchor.UpperLeft, RectOffset padding = null)
        {
            var rt = Rect(name, parent);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = align;
            v.padding = padding ?? new RectOffset();
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return rt;
        }

        public static RectTransform Spacer(Transform parent, float flex = 1)
        {
            var rt = Rect("Spacer", parent);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = flex;
            le.flexibleHeight = flex;
            return rt;
        }

        public static Image Bar(Transform parent, string name, float fill, Color color, float height = 14)
        {
            var bg = Img(parent, name, T.panel2, T.pill);
            bg.Size(-1, height);
            var f = Img(bg.transform, "Fill", color, T.pill);
            f.RT().Span(0, 0, Mathf.Clamp01(fill), 1);
            return f;
        }

        public static void SetBar(Image fill, float value)
        {
            var rt = fill.RT();
            rt.anchorMax = new Vector2(Mathf.Clamp01(value), 1);
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Key/value row used in spec and status panels.</summary>
        public static TextMeshProUGUI KeyValue(Transform parent, string key, string value, Color? valueColor = null)
        {
            var row = HStack(parent, "KV_" + key, 12);
            row.Size(-1, 56);
            var k = Text(row, "Key", key, 26, T.textDim);
            k.Size(-1, -1, 1);
            var v = Text(row, "Value", value, 26, valueColor ?? T.text, TextStyle.Body, TextAlignmentOptions.MidlineRight);
            v.fontStyle = FontStyles.Bold;
            v.textWrappingMode = TextWrappingModes.NoWrap;
            var line = Img(parent, "Rule", UITheme.A(T.line, 0.8f));
            line.Size(-1, 2);
            return v;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                if (Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c);
            }
        }

        public static Color Of(ChipStyle s) => s switch
        {
            ChipStyle.Accent => T.accent,
            ChipStyle.Ok => T.ok,
            ChipStyle.Warn => T.warn,
            ChipStyle.Danger => T.danger,
            _ => T.textDim
        };
    }
}
