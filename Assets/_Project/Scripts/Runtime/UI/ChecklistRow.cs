using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DroneTrainer.Data;
using DroneTrainer.Tasks;

namespace DroneTrainer.UI
{
    /// <summary>Checklist row with done / active / failed / locked states (components sheet).</summary>
    public class ChecklistRow : MonoBehaviour
    {
        public Image background;
        public Image dot;
        public TextMeshProUGUI number;
        public Image dotIcon;
        public TextMeshProUGUI title;
        public Transform subDots;
        public UIChip criticalTag;
        int index;

        /// <summary>Factory method: one row per JSON step.</summary>
        public static ChecklistRow Create(Transform parent, int index, StepConfig step, bool showCriticalTag = false)
        {
            var t = UITheme.Current;
            var bg = UIFactory.Img(parent, "Row_" + step.id, Color.clear, t.rounded);
            bg.Size(-1, 76);
            var row = bg.gameObject.AddComponent<ChecklistRow>();
            row.index = index;
            row.background = bg;
            var h = bg.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(18, 18, 8, 8);
            h.spacing = 16;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;

            row.dot = UIFactory.Img(bg.transform, "Dot", t.panel2, t.circle);
            row.dot.Size(48, 48);
            row.number = UIFactory.Text(row.dot.transform, "N", (index + 1).ToString(), 24, t.textMute,
                                        TextStyle.Mono, TextAlignmentOptions.Center);
            row.number.RT().Fill();
            row.dotIcon = UIFactory.Icon(row.dot.transform, "Mark", "check", 30, Color.white);
            row.dotIcon.RT().At(0.5f, 0.5f, 0.5f, 0.5f, 30, 30);
            row.dotIcon.enabled = false;
            row.title = UIFactory.Text(bg.transform, "Title", step.title, 28, t.text);
            row.title.textWrappingMode = TextWrappingModes.NoWrap;
            row.title.Size(-1, -1, 1);
            var dots = UIFactory.HStack(bg.transform, "SubDots", 8, TextAnchor.MiddleRight);
            row.subDots = dots;
            if (showCriticalTag && step.critical)
                row.criticalTag = UIFactory.Chip(bg.transform, "Tag", "CRITICAL", ChipStyle.Danger);
            return row;
        }

        public void Set(StepStatus status, int done, int total, bool failed = false)
        {
            var t = UITheme.Current;
            bool isDone = status == StepStatus.Done;
            number.text = (index + 1).ToString();
            number.enabled = !(failed || isDone);
            if (dotIcon)
            {
                dotIcon.sprite = t.Icon(failed ? "x" : "check");
                dotIcon.enabled = (failed || isDone) && dotIcon.sprite;
                dotIcon.color = failed ? Color.white : UITheme.Hex("062B14");
            }
            if (failed) { dot.color = t.danger; number.color = Color.white; }
            else if (isDone) { dot.color = t.ok; number.color = UITheme.Hex("062B14"); }
            else if (status == StepStatus.Active) { dot.color = UITheme.A(t.accent, 0.25f); number.color = t.accent; }
            else { dot.color = t.panel2; number.color = t.textMute; }

            background.color = status == StepStatus.Active ? UITheme.A(t.accent, 0.14f) : Color.clear;
            title.color = status == StepStatus.Locked ? t.textMute : t.text;

            if (subDots == null) return;
            if (total <= 1 || isDone) { UIFactory.Clear(subDots); return; }
            if (subDots.childCount != total)
            {
                UIFactory.Clear(subDots);
                for (int i = 0; i < total; i++) UIFactory.Img(subDots, "d", t.line, t.circle).Size(16, 16);
            }
            for (int i = 0; i < total; i++)
                subDots.GetChild(i).GetComponent<Image>().color = i < done ? t.ok : t.line;
        }
    }
}
