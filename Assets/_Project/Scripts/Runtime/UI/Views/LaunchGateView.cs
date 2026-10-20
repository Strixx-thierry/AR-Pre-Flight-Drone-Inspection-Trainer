using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DroneTrainer.Data;
using DroneTrainer.Tasks;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S12 launch blocked (FR-08).</summary>
    public class LaunchGateView : UIView
    {
        public RectTransform rows;
        public UIButton goButton;
        public TextMeshProUGUI subtitle;
        public event Action<int> GoToStep;
        int target;

        protected override void Build()
        {
            Scrim(new Color(0.16f, 0.015f, 0.03f, 0.72f));
            var m = Panel(transform, "Modal", large: true);
            m.RT().At(0.5f, 0.5f, 0.5f, 0.5f, 1180, 780);
            m.GetComponent<UnityEngine.UI.Outline>().effectColor = UITheme.A(T.danger, 0.8f);
            var v = VStack(m.transform, "V", 12, TextAnchor.UpperLeft, new RectOffset(48, 48, 40, 40));
            v.Fill();
            var head = HStack(v, "Head", 18);
            Icon(head, "Icon", "x", 80, T.danger);
            Text(head, "Title", "LAUNCH BLOCKED", 64, T.danger, TextStyle.Display);
            subtitle = Text(v, "Sub", "This aircraft isn't safe to fly. Fix the critical item below and try again.", 28, T.textDim);
            rows = VStack(v, "Rows", 4);
            Spacer(v);
            var foot = HStack(v, "Foot", 18);
            Chip(foot, "Penalty", "+1 SAFETY ERROR", ChipStyle.Danger, "warn");
            Spacer(foot);
            goButton = Button(foot, "Go", "GO TO STEP", ButtonStyle.Danger, "right");
            goButton.Size(620, TouchSize);
        }

        protected override void Bind() => goButton.Clicked += () => GoToStep?.Invoke(target);

        public void Open(ScenarioConfig scenario, List<StepConfig> blocking)
        {
            Show();
            Clear(rows);
            target = Array.IndexOf(scenario.steps, blocking[0]);
            for (int i = 0; i < scenario.steps.Length; i++)
            {
                var s = scenario.steps[i];
                var row = ChecklistRow.Create(rows, i, s, showCriticalTag: blocking.Contains(s));
                bool failed = blocking.Contains(s);
                row.Set(failed ? StepStatus.Locked : StepStatus.Done, 0, 0, failed);
                if (failed) row.title.text = $"{s.title}: <color=#{ColorUtility.ToHtmlStringRGB(T.danger)}>NOT SECURED</color>";
            }
            goButton.SetLabel("GO TO " + blocking[0].title.ToUpperInvariant());
        }
    }
}
