using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using DroneTrainer.Core;
using DroneTrainer.Data;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S16 progress history from PlayerPrefs — evidence of improving protocol compliance.</summary>
    public class RecordsView : UIView
    {
        public UIButton backButton;
        public RectTransform chart, list;
        public TextMeshProUGUI sessionsText, passText, avgText, bestText, emptyText;
        public event Action Back;

        protected override void Build()
        {
            var c = Content();
            var top = HStack(c, "Top", 18);
            top.Span(0, 1, 1, 1, 0, -112, 0, 0);
            backButton = RoundButton(top, "Back", "left", 104);
            Text(top, "Title", "RECORDS", 48, T.text, TextStyle.Display).Size(-1, -1, 1);
            bestText = Text(top, "Best", "BEST --:--", 26, T.ok, TextStyle.Label, TextAlignmentOptions.MidlineRight);

            var left = Panel(c, "ChartPanel", large: true);
            left.RT().Span(0, 0, 0.47f, 1, 0, 0, 0, 136);
            var lv = VStack(left.transform, "V", 16, TextAnchor.UpperLeft, new RectOffset(36, 36, 30, 30));
            lv.Fill();
            Text(lv, "Label", "COMPLETION TIME · LAST 6", 22, T.textMute, TextStyle.Label);
            chart = HStack(lv, "Chart", 16, TextAnchor.LowerCenter);
            chart.Size(-1, 380);
            chart.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = true;
            var stats = HStack(lv, "Stats", 60);
            sessionsText = Stat(stats, "SESSIONS", T.text);
            passText = Stat(stats, "PASS RATE", T.ok);
            avgText = Stat(stats, "AVG ERRORS", T.danger);

            var right = Panel(c, "ListPanel", large: true);
            right.RT().Span(0.5f, 0, 1, 1, 0, 0, 0, 136);
            var rv = VStack(right.transform, "V", 0, TextAnchor.UpperLeft, new RectOffset(36, 36, 30, 30));
            rv.Fill();
            Text(rv, "Label", "SESSIONS", 22, T.textMute, TextStyle.Label).Size(-1, 50);
            list = VStack(rv, "List", 0);
            emptyText = Text(rv, "Empty", "No sessions yet. Run a pre-flight to start your log.", 28, T.textDim);
        }

        TextMeshProUGUI Stat(Transform parent, string label, Color color)
        {
            var v = VStack(parent, label, 0);
            var value = Text(v, "Value", "0", 54, color, TextStyle.Display);
            Text(v, "Label", label, 20, T.textMute, TextStyle.Label);
            return value;
        }

        protected override void Bind() => backButton.Clicked += () => Back?.Invoke();

        public void Open(List<SessionResult> sessions, float best)
        {
            Show();
            bestText.text = best > 0 ? $"BEST {TimeFormat.Clock(best)}" : "BEST --:--";
            sessionsText.text = sessions.Count.ToString();
            passText.text = sessions.Count == 0 ? "-" : $"{Mathf.RoundToInt(100f * sessions.Count(s => s.passed) / sessions.Count)}%";
            avgText.text = sessions.Count == 0 ? "-" : sessions.Average(s => s.TotalErrors).ToString("0.0");
            emptyText.gameObject.SetActive(sessions.Count == 0);

            Clear(chart);
            var last = sessions.Take(6).Reverse().ToList();
            float max = last.Count == 0 ? 1 : last.Max(s => s.seconds);
            foreach (var s in last)
            {
                var col = VStack(chart, "Col", 6, TextAnchor.LowerCenter);
                col.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childForceExpandWidth = true;
                Text(col, "Time", TimeFormat.Clock(s.seconds), 20, T.textDim, TextStyle.Mono, TextAlignmentOptions.Center);
                bool isBest = Mathf.Approximately(s.seconds, best);
                var bar = Img(col, "Bar", UITheme.A(isBest ? T.ok : T.accent, 0.35f), T.rounded);
                bar.Size(-1, Mathf.Max(20, 300 * s.seconds / max));
            }

            Clear(list);
            foreach (var s in sessions.Take(8))
            {
                var row = HStack(list, "Row", 16);
                row.Size(-1, 72);
                Text(row, "When", $"{s.LocalTime:MMM dd HH:mm} · {s.mode}", 26, T.text).Size(-1, -1, 1);
                Text(row, "Time", TimeFormat.Clock(s.seconds), 26, T.textDim, TextStyle.Mono, TextAlignmentOptions.MidlineRight).Size(120);
                Text(row, "Score", s.passed ? s.score.ToString() : "FAIL", 26, s.passed ? T.ok : T.danger,
                     TextStyle.Mono, TextAlignmentOptions.MidlineRight).Size(110);
                Img(list, "Rule", UITheme.A(T.line, 0.8f)).Size(-1, 2);
            }
        }
    }
}
