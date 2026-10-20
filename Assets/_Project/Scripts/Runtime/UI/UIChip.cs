using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DroneTrainer.UI
{
    public class UIChip : MonoBehaviour
    {
        public Image background;
        public TextMeshProUGUI label;
        public Image icon;
        public bool lockColors;

        public void Set(string text, ChipStyle style)
        {
            if (label) label.text = text;
            if (lockColors) return;
            var c = UIFactory.Of(style);
            if (background) background.color = style == ChipStyle.Neutral ? UITheme.Current.panel2 : UITheme.A(c, 0.16f);
            if (label) label.color = c;
            if (icon && icon.sprite) icon.color = c;
        }

        public void SetText(string text) { if (label) label.text = text; }
    }
}
