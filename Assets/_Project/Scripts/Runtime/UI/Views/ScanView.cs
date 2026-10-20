using System;
using TMPro;
using UnityEngine;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S05 scan for a surface (FR-01).</summary>
    public class ScanView : UIView
    {
        public UIButton closeButton;
        public RectTransform phone;
        public UIChip surfacePill;
        public TextMeshProUGUI tipText;
        public TextMeshProUGUI titleText, helperText;
        public event Action CloseClicked;
        float shownAt;

        protected override void Build()
        {
            var c = Content();
            closeButton = RoundButton(c, "Close", "x", 104);
            closeButton.RT().At(0, 1, 0, 1, 104, 104);

            var ph = Icon(c, "Phone", "phone", 150, T.text);
            phone = ph.RT().At(0.5f, 0.62f, 0.5f, 0.5f, 150, 150);

            tipText = Text(c, "Tip", "Try somewhere brighter, or a surface with more texture.", 26, T.warn,
                           TextStyle.Body, TextAlignmentOptions.Center);
            tipText.RT().At(0.5f, 0, 0.5f, 0, 1000, 60, 0, 190);
            tipText.gameObject.SetActive(false);

            var card = Panel(c, "Instruction", glass: true, large: true);
            card.RT().At(0.5f, 0, 0.5f, 0, 920, 150);
            var h = HStack(card.transform, "Row", 24, TextAnchor.MiddleLeft, new RectOffset(30, 30, 18, 18));
            h.Fill();
            Icon(h, "Icon", "phone", 72, T.accent);
            var v = VStack(h, "Text", 2, TextAnchor.MiddleLeft);
            v.Size(-1, -1, 1);
            titleText = Text(v, "Title", "SCAN THE SURFACE", 34, T.text, TextStyle.Display);
            helperText = Text(v, "Helper", "Move your phone slowly from side to side over a desk or the floor.", 25, T.textDim);

            surfacePill = Pill(c, "Surfaces", "0 surfaces found", "target", T.accent);
            surfacePill.RT().At(1, 0, 1, 0, 360, 80);
        }

        protected override void Bind() => closeButton.Clicked += () => CloseClicked?.Invoke();

        public override void Show()
        {
            base.Show();
            shownAt = Time.unscaledTime;
            tipText.gameObject.SetActive(false);
        }

        public void SetSimulator(bool sim)
        {
            if (sim) helperText.text = "Desk simulator: no AR device detected. Finding the virtual desk…";
        }

        public void SetSurfaceCount(int n) =>
            surfacePill.SetText(n == 1 ? "1 surface found" : $"{n} surfaces found");

        void Update()
        {
            if (phone) phone.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 2.2f) * 140f, 0);
            if (!tipText.gameObject.activeSelf && Time.unscaledTime - shownAt > 15f) tipText.gameObject.SetActive(true);
        }
    }
}
