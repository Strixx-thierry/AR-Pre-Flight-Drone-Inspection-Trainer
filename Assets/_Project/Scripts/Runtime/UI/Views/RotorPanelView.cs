using System;
using TMPro;
using UnityEngine;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S08 component panel: the trainee must decide, not just look (FR-04, FR-06).</summary>
    public class RotorPanelView : UIView
    {
        public TextMeshProUGUI counterText, titleText, guidanceText, questionText;
        public UIButton closeButton, airworthyButton, damagedButton, replaceButton;
        public RectTransform verdictRow;
        public event Action<bool> Verdict;
        public event Action Replace, Close;

        protected override void Build()
        {
            var c = Content();
            var p = Panel(c, "Panel", large: true);
            p.RT().Span(0.54f, 0, 1, 1, 0, 0, 0, 136);
            var v = VStack(p.transform, "V", 14, TextAnchor.UpperLeft, new RectOffset(36, 36, 30, 30));
            v.Fill();

            var head = HStack(v, "Head", 12);
            var hv = VStack(head, "Titles", 2);
            hv.Size(-1, -1, 1);
            counterText = Text(hv, "Counter", "COMPONENT 1 / 4", 22, T.textMute, TextStyle.Label);
            titleText = Text(hv, "Title", "ROTOR · FRONT RIGHT", 40, T.text, TextStyle.Display);
            closeButton = RoundButton(head, "Close", "x", 96);

            var preview = Img(v, "Preview", T.panel2, T.rounded);
            preview.Size(-1, 230);
            var pv = HStack(preview.transform, "Row", 26, TextAnchor.MiddleLeft, new RectOffset(28, 28, 20, 20));
            pv.Fill();
            Icon(pv, "Icon", "eye", 96, T.accent);
            guidanceText = Text(pv, "Guidance",
                "Move your phone close to the blade. Check the leading edge and tip for chips, cracks or bends.",
                27, T.textDim);
            guidanceText.Size(-1, -1, 1);

            Spacer(v);
            questionText = Text(v, "Question", "Is this propeller fit to fly?", 28, T.textDim);
            verdictRow = HStack(v, "Verdict", 18);
            airworthyButton = Button(verdictRow, "Airworthy", "AIRWORTHY", ButtonStyle.Ok, "check");
            airworthyButton.Size(-1, TouchSize, 1);
            damagedButton = Button(verdictRow, "Damaged", "DAMAGED", ButtonStyle.Danger, "flag");
            damagedButton.Size(-1, TouchSize, 1);
            replaceButton = Button(v, "Replace", "REPLACE PROPELLER", ButtonStyle.Primary, "wrench");
            replaceButton.gameObject.SetActive(false);
        }

        protected override void Bind()
        {
            closeButton.Clicked += () => Close?.Invoke();
            airworthyButton.Clicked += () => Verdict?.Invoke(true);
            damagedButton.Clicked += () => Verdict?.Invoke(false);
            replaceButton.Clicked += () => Replace?.Invoke();
        }

        public void Open(string partName, int index, int total, bool needsReplace)
        {
            Show();
            counterText.text = $"COMPONENT {index} / {total}";
            titleText.text = partName;
            SetReplaceMode(needsReplace);
        }

        public void SetReplaceMode(bool needsReplace)
        {
            verdictRow.gameObject.SetActive(!needsReplace);
            replaceButton.gameObject.SetActive(needsReplace);
            questionText.text = needsReplace
                ? "Damaged blades must be replaced before flight."
                : "Is this propeller fit to fly?";
        }
    }
}
