using System;
using TMPro;
using UnityEngine;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>How-to + credits modal.</summary>
    public class HelpView : UIView
    {
        public UIButton closeButton;
        public event Action Close;

        static readonly string[] Steps =
        {
            "Pick an aircraft in the Hangar and choose Guided or Assessment.",
            "Scan a flat, well-lit surface, then tap to place the drone at real size.",
            "Walk around it. Tap glowing parts to inspect and fix them in order.",
            "Try Launch Check. Unsafe items block the launch and count as errors.",
            "Read your report and beat your best time in Records."
        };

        protected override void Build()
        {
            Scrim(new Color(0.02f, 0.03f, 0.035f, 0.75f));
            var m = Panel(transform, "Modal", large: true);
            m.RT().At(0.5f, 0.5f, 0.5f, 0.5f, 1500, 880);
            var v = VStack(m.transform, "V", 10, TextAnchor.UpperLeft, new RectOffset(52, 52, 40, 40));
            v.Fill();
            var head = HStack(v, "Head", 10);
            Text(head, "Title", "HOW IT WORKS", 52, T.text, TextStyle.Display).Size(-1, -1, 1);
            closeButton = RoundButton(head, "Close", "x", 96);
            for (int i = 0; i < Steps.Length; i++)
            {
                var row = HStack(v, "Step" + i, 20);
                row.Size(-1, 84);
                var dot = Img(row, "Dot", UITheme.A(T.accent, 0.2f), T.circle);
                dot.Size(56, 56);
                var n = Text(dot.transform, "N", (i + 1).ToString(), 26, T.accent, TextStyle.Mono, TextAlignmentOptions.Center);
                n.RT().Fill();
                Text(row, "Text", Steps[i], 28, T.text).Size(-1, -1, 1);
            }
            Spacer(v);
            Text(v, "Credits",
                "Credits: SKYFOLD F4 model built procedurally in Blender for this project. Sound effects synthesised. " +
                "Not affiliated with any drone manufacturer. Built with Unity AR Foundation.",
                22, T.textMute);
        }

        protected override void Bind() => closeButton.Clicked += () => Close?.Invoke();
    }
}
