using System;
using TMPro;
using UnityEngine;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S10 gimbal clamp removal — swipe forward to slide it off.</summary>
    public class GimbalPanelView : UIView
    {
        public SwipeArea swipe;
        public RectTransform arrows;
        public TextMeshProUGUI helperText;
        public UIButton closeButton;
        public event Action Swiped, Close;

        protected override void Build()
        {
            var c = Content();
            var p = Panel(c, "Panel", glass: true, large: true);
            p.RT().At(1, 0, 1, 0, 620, 560);
            swipe = p.gameObject.AddComponent<SwipeArea>();
            var v = VStack(p.transform, "V", 12, TextAnchor.UpperCenter, new RectOffset(32, 32, 26, 30));
            v.Fill();
            var head = HStack(v, "Head", 12);
            Text(head, "Label", "GIMBAL CLAMP", 22, T.textMute, TextStyle.Label).Size(-1, -1, 1);
            closeButton = RoundButton(head, "Close", "x", 88);

            arrows = VStack(v, "Arrows", -40, TextAnchor.MiddleCenter);
            arrows.Size(-1, 220);
            for (int i = 0; i < 3; i++)
            {
                var a = Icon(arrows, "Arrow" + i, "right", 110, UITheme.A(T.accent, 1f - i * 0.3f));
                a.RT().localRotation = Quaternion.Euler(0, 0, 90);
            }
            Text(v, "Title", "SWIPE UP TO REMOVE", 36, T.text, TextStyle.Display, TextAlignmentOptions.Center);
            helperText = Text(v, "Helper", "Swipe here (or on the clamp) to slide the clear cover off the camera.",
                              25, T.textDim, TextStyle.Body, TextAlignmentOptions.Center);
        }

        protected override void Bind()
        {
            swipe.Swiped += () => Swiped?.Invoke();
            swipe.Tapped += () => helperText.text = "Don't tap: swipe upward to slide it off.";
            closeButton.Clicked += () => Close?.Invoke();
        }

        void Update()
        {
            if (arrows) arrows.anchoredPosition = new Vector2(arrows.anchoredPosition.x, Mathf.PingPong(Time.unscaledTime * 60f, 30f));
        }
    }
}
