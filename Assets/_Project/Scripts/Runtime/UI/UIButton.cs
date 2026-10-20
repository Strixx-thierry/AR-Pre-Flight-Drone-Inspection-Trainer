using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DroneTrainer.Audio;

namespace DroneTrainer.UI
{
    /// <summary>Styled button (Btn_Primary / Ghost / Verdict / Round in the components sheet).</summary>
    public class UIButton : MonoBehaviour
    {
        public Button button;
        public Image background;
        public TextMeshProUGUI label;
        public Image icon;
        public ButtonStyle style;
        public string clickSound = "tap";

        public event Action Clicked;

        // Start (not Awake) so buttons created at runtime by the factory are wired after their fields are set.
        void Start()
        {
            if (button) button.onClick.AddListener(() =>
            {
                AudioManager.Play(clickSound);
                Clicked?.Invoke();
            });
        }

        public void SetLabel(string text) { if (label) label.text = text; }

        public void SetInteractable(bool on)
        {
            if (button) button.interactable = on;
            SetStyle(on ? (style == ButtonStyle.Disabled ? ButtonStyle.Primary : style) : ButtonStyle.Disabled, keep: true);
        }

        public void SetStyle(ButtonStyle s, bool keep = false)
        {
            if (!keep) style = s;
            var t = UITheme.Current;
            Color bg, fg;
            switch (s)
            {
                case ButtonStyle.Ghost: bg = UITheme.A(t.panel2, 0.9f); fg = t.text; break;
                case ButtonStyle.Ok: bg = t.ok; fg = UITheme.Hex("062B14"); break;
                case ButtonStyle.Danger: bg = t.danger; fg = Color.white; break;
                case ButtonStyle.Disabled: bg = t.panel2; fg = t.textMute; break;
                case ButtonStyle.Round: bg = UITheme.A(t.panel2, 0.92f); fg = t.text; break;
                default: bg = t.accent; fg = t.bg0; break;
            }
            if (background) background.color = bg;
            if (label) label.color = fg;
            if (icon && icon.sprite) icon.color = fg;
        }
    }
}
