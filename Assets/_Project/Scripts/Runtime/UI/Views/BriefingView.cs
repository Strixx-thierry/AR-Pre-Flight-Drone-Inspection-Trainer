using System;
using TMPro;
using UnityEngine;
using DroneTrainer.Core;
using DroneTrainer.Data;
using DroneTrainer.Tasks;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S04 mission briefing &amp; mode select. Checklist rows are built from the JSON (Factory).</summary>
    public class BriefingView : UIView
    {
        public UIButton backButton, startButton, startARButton, guidedCard, assessmentCard;
        public TextMeshProUGUI moduleText, titleText, countText, estimateText;
        public RectTransform rows;
        public event Action Back;
        public event Action<TrainingMode> StartRequested, StartARRequested;

        /// <summary>AR add-on button only on ARCore-capable devices.</summary>
        public void SetARAvailable(bool ok) => startARButton.gameObject.SetActive(ok);
        TrainingMode mode = TrainingMode.Guided;

        protected override void Build()
        {
            var c = Content();
            var top = HStack(c, "Top", 22);
            top.Span(0, 1, 1, 1, 0, -112, 0, 0);
            backButton = RoundButton(top, "Back", "left", 104);
            var tv = VStack(top, "Titles", 0, TextAnchor.MiddleLeft);
            moduleText = Text(tv, "Module", "SKYFOLD F4 · MODULE 1", 22, T.textMute, TextStyle.Label);
            titleText = Text(tv, "Title", "STANDARD PRE-FLIGHT", 46, T.text, TextStyle.Display);

            var left = Panel(c, "Checklist", large: true);
            left.RT().Span(0, 0.17f, 0.47f, 1, 0, 0, 0, 136);
            var lv = VStack(left.transform, "V", 6, TextAnchor.UpperLeft, new RectOffset(32, 32, 26, 26));
            lv.Fill();
            var lh = HStack(lv, "Head", 10);
            countText = Text(lh, "Count", "CHECKLIST · 4 ITEMS", 22, T.textMute, TextStyle.Label);
            countText.Size(-1, -1, 1);
            estimateText = Text(lh, "Estimate", "~4 min", 24, T.textDim, TextStyle.Body, TextAlignmentOptions.MidlineRight);
            rows = VStack(lv, "Rows", 4);
            Spacer(lv);

            var right = VStack(c, "Modes", 18);
            right.Span(0.5f, 0.17f, 1, 1, 0, 0, 0, 136);
            guidedCard = ModeCard(right, "GUIDED", "RECOMMENDED",
                "Targets glow in order, with hints and step-by-step text. Best for a first run.");
            assessmentCard = ModeCard(right, "ASSESSMENT", "SCORED",
                "No glow and no hints. You choose what to inspect, and every miss counts as an error.");

            startButton = Button(c, "Start", "START", ButtonStyle.Primary, "play");
            startButton.RT().At(1, 0, 1, 0, 420, TouchSize);
            startARButton = Button(c, "StartAR", "START IN AR", ButtonStyle.Ghost, "target");
            startARButton.RT().At(1, 0, 1, 0, 440, TouchSize, -450, 0);
            startARButton.gameObject.SetActive(false);
        }

        UIButton ModeCard(Transform parent, string title, string tag, string body)
        {
            var b = Button(parent, "Mode_" + title, null, ButtonStyle.Ghost, null, 230);
            b.background.sprite = T.navTile ? T.navTile : T.roundedLarge;
            b.background.type = UnityEngine.UI.Image.Type.Sliced;
            b.style = ButtonStyle.Round;
            var row = b.transform.Find("Row");
            DestroyImmediate(row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>());
            var v = row.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            v.padding = new RectOffset(10, 10, 24, 24);
            v.spacing = 8;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;
            var h = HStack(row, "Head", 10);
            b.label = Text(h, "Title", title, 36, T.text, TextStyle.Display);
            b.label.Size(-1, -1, 1);
            Chip(h, "Tag", tag, title == "GUIDED" ? ChipStyle.Accent : ChipStyle.Neutral);
            Text(row, "Body", body, 25, T.textDim);
            return b;
        }

        protected override void Bind()
        {
            backButton.Clicked += () => Back?.Invoke();
            guidedCard.Clicked += () => SetMode(TrainingMode.Guided);
            assessmentCard.Clicked += () => SetMode(TrainingMode.Assessment);
            startButton.Clicked += () => StartRequested?.Invoke(mode);
            startARButton.Clicked += () => StartARRequested?.Invoke(mode);
        }

        public void Open(DroneDefinition d, ScenarioConfig s, TrainingMode current)
        {
            Show();
            moduleText.text = $"{d.displayName} · {s.module}".ToUpperInvariant();
            titleText.text = s.title;
            countText.text = $"CHECKLIST · {s.steps.Length} ITEMS";
            estimateText.text = $"~{Mathf.CeilToInt(s.targetSeconds / 60f)} min";
            Clear(rows);
            for (int i = 0; i < s.steps.Length; i++)
                ChecklistRow.Create(rows, i, s.steps[i], showCriticalTag: true).Set(StepStatus.Open, 0, 0);
            SetMode(current);
        }

        void SetMode(TrainingMode m)
        {
            mode = m;
            Style(guidedCard, m == TrainingMode.Guided);
            Style(assessmentCard, m == TrainingMode.Assessment);
        }

        void Style(UIButton b, bool on)
        {
            b.background.color = T.navTile ? (on ? Color.Lerp(Color.white, T.accent, 0.55f) : Color.white) : (on ? Color.Lerp(T.panel, T.accent, 0.16f) : T.panel);
            var o = b.GetComponent<UnityEngine.UI.Outline>();
            if (!o) o = b.gameObject.AddComponent<UnityEngine.UI.Outline>();
            o.effectDistance = new Vector2(3, -3);
            o.effectColor = on ? T.accent : T.line;
            b.label.color = T.text;
        }
    }
}
