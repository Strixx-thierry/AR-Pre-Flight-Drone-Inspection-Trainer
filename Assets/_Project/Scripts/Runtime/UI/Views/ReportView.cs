using System;
using System.Linq;
using TMPro;
using UnityEngine;
using DroneTrainer.Core;
using DroneTrainer.Data;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S15 evaluation report (FR-10): time, safety errors, pass/fail, step breakdown.</summary>
    public class ReportView : UIView
    {
        public UnityEngine.UI.Image gaugeFill;
        public TextMeshProUGUI scoreText, resultText, timeText, errorsText, deltaText, headerText;
        public RectTransform stepRows, errorRows;
        public UIButton homeButton, recordsButton, retryButton;
        public event Action Home, Records, Retry;

        protected override void Build()
        {
            var bg = Img(transform, "Background", T.bg1, T.gradient, true);
            bg.RT().Fill();
            var c = Content();
            headerText = Text(c, "Header", "MISSION REPORT", 24, T.textMute, TextStyle.Label);
            headerText.RT().Span(0, 1, 1, 1, 0, -60, 0, 0);

            var left = Panel(c, "Result", large: true);
            left.RT().Span(0, 0, 0.3f, 1, 0, 0, 0, 80);
            var lv = VStack(left.transform, "V", 10, TextAnchor.UpperCenter, new RectOffset(30, 30, 40, 30));
            lv.Fill();
            lv.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childForceExpandWidth = false;   // keep the gauge square
            var gauge = Img(lv, "Gauge", T.panel2, T.ring);
            gauge.Size(320, 320);
            gauge.preserveAspect = true;
            gaugeFill = Img(gauge.transform, "Fill", T.ok, T.ring);
            gaugeFill.RT().Fill();
            gaugeFill.type = UnityEngine.UI.Image.Type.Filled;
            gaugeFill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            gaugeFill.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            gaugeFill.fillClockwise = true;
            var gv = VStack(gauge.transform, "Center", 0, TextAnchor.MiddleCenter);
            gv.Fill();
            scoreText = Text(gv, "Score", "88", 110, T.text, TextStyle.Display, TextAlignmentOptions.Center);
            Text(gv, "Label", "SCORE", 22, T.textMute, TextStyle.Label, TextAlignmentOptions.Center);
            resultText = Text(lv, "Result", "PASSED", 64, T.ok, TextStyle.Display, TextAlignmentOptions.Center);
            var stats = HStack(lv, "Stats", 40, TextAnchor.MiddleCenter);
            timeText = Stat(stats, "TIME");
            errorsText = Stat(stats, "ERRORS");
            deltaText = Stat(stats, "VS BEST");

            var right = Panel(c, "Breakdown", large: true);
            right.RT().Span(0.32f, 0.2f, 1, 1, 0, 0, 0, 80);
            var rv = VStack(right.transform, "V", 4, TextAnchor.UpperLeft, new RectOffset(36, 36, 28, 28));
            rv.Fill();
            Text(rv, "Label", "STEP BREAKDOWN", 22, T.textMute, TextStyle.Label).Size(-1, 50);
            stepRows = VStack(rv, "Steps", 0);
            Spacer(rv);
            errorRows = VStack(rv, "Errors", 6);

            var buttons = HStack(c, "Buttons", 18, TextAnchor.MiddleRight);
            buttons.Span(0.32f, 0, 1, 0, 0, 0, 0, -TouchSize);
            Spacer(buttons);
            homeButton = Button(buttons, "Home", "HOME", ButtonStyle.Ghost, "home");
            homeButton.Size(280, TouchSize);
            recordsButton = Button(buttons, "Records", "RECORDS", ButtonStyle.Ghost, "records");
            recordsButton.Size(320, TouchSize);
            retryButton = Button(buttons, "Retry", "RETRY", ButtonStyle.Primary, "reset");
            retryButton.Size(300, TouchSize);
        }

        TextMeshProUGUI Stat(Transform parent, string label)
        {
            var v = VStack(parent, label, 0, TextAnchor.MiddleCenter);
            var value = Text(v, "Value", "-", 40, T.text, TextStyle.Mono, TextAlignmentOptions.Center);
            Text(v, "Label", label, 20, T.textMute, TextStyle.Label, TextAlignmentOptions.Center);
            return value;
        }

        protected override void Bind()
        {
            homeButton.Clicked += () => Home?.Invoke();
            recordsButton.Clicked += () => Records?.Invoke();
            retryButton.Clicked += () => Retry?.Invoke();
        }

        public void Open(SessionResult r, float previousBest)
        {
            Show();
            headerText.text = $"MISSION REPORT · {r.droneName} · {r.mode}";
            scoreText.text = r.score.ToString();
            gaugeFill.fillAmount = r.score / 100f;
            gaugeFill.color = r.passed ? T.ok : T.danger;
            resultText.text = r.passed ? "PASSED" : "NOT PASSED";
            resultText.color = r.passed ? T.ok : T.danger;
            timeText.text = TimeFormat.Clock(r.seconds);
            errorsText.text = r.TotalErrors.ToString();
            errorsText.color = r.TotalErrors > 0 ? T.danger : T.ok;
            if (previousBest > 0)
            {
                float d = r.seconds - previousBest;
                deltaText.text = (d <= 0 ? "−" : "+") + TimeFormat.Clock(Mathf.Abs(d));
                deltaText.color = d <= 0 ? T.accent : T.textDim;
            }
            else { deltaText.text = "NEW"; deltaText.color = T.accent; }

            Clear(stepRows);
            foreach (var s in r.steps)
            {
                var row = HStack(stepRows, "Step_" + s.id, 16);
                row.Size(-1, 70);
                Icon(row, "Mark", s.completed ? "check" : "x", 34, s.completed ? T.ok : T.danger);
                var title = Text(row, "Title", s.title + (string.IsNullOrEmpty(s.note) ? "" : $"  <color=#{ColorUtility.ToHtmlStringRGB(T.warn)}>({s.note})</color>"), 28, T.text);
                title.Size(-1, -1, 1);
                Text(row, "Time", TimeFormat.Clock(s.seconds), 26, T.textDim, TextStyle.Mono, TextAlignmentOptions.MidlineRight).Size(130);
                Text(row, "Err", s.errors == 0 ? "0" : $"{s.errors} err", 26, s.errors == 0 ? T.ok : T.danger,
                     TextStyle.Mono, TextAlignmentOptions.MidlineRight).Size(130);
                Img(stepRows, "Rule", UITheme.A(T.line, 0.8f)).Size(-1, 2);
            }

            Clear(errorRows);
            foreach (var e in r.errors.Where(e => e.severity == ErrorSeverity.Safety).Take(2)
                                      .Concat(r.errors.Where(e => e.severity == ErrorSeverity.Minor).Take(1)))
            {
                var box = Img(errorRows, "Error", UITheme.A(e.severity == ErrorSeverity.Safety ? T.danger : T.warn, 0.12f), T.rounded);
                box.Size(-1, 66);
                var h = HStack(box.transform, "Row", 14, TextAnchor.MiddleLeft, new RectOffset(20, 20, 6, 6));
                h.Fill();
                Icon(h, "Icon", "warn", 30, e.severity == ErrorSeverity.Safety ? T.danger : T.warn);
                var label = e.severity == ErrorSeverity.Safety ? "Safety error" : "Minor";
                Text(h, "Text", $"<b>{label} at {TimeFormat.Clock(e.time)}:</b> {e.message}", 24, T.text).Size(-1, -1, 1);
            }
        }
    }
}
