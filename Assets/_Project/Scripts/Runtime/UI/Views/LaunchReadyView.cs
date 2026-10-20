using System;
using TMPro;
using UnityEngine;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S13 launch cleared — slide to arm motors.</summary>
    public class LaunchReadyView : UIView
    {
        public TextMeshProUGUI subtitle;
        public SlideConfirm slide;
        public UIButton backButton;
        public event Action Armed, Back;

        protected override void Build()
        {
            var c = Content();
            backButton = RoundButton(c, "Back", "left", 104);
            backButton.RT().At(0, 1, 0, 1, 104, 104);
            var title = Text(c, "Title", "READY FOR LAUNCH", 72, T.ok, TextStyle.Display, TextAlignmentOptions.Center);
            title.RT().At(0.5f, 1, 0.5f, 1, 1400, 100, 0, -10);
            subtitle = Text(c, "Sub", "All checks passed", 30, T.textDim, TextStyle.Body, TextAlignmentOptions.Center);
            subtitle.RT().At(0.5f, 1, 0.5f, 1, 1400, 50, 0, -110);
            slide = SlideConfirm.Create(c, "Arm", "SLIDE TO ARM MOTORS", "play", T.ok);
            slide.RT().At(0.5f, 0, 0.5f, 0, 960, TouchSize, 0, 10);
        }

        protected override void Bind()
        {
            slide.Confirmed += () => Armed?.Invoke();
            backButton.Clicked += () => Back?.Invoke();
        }

        public void Open(int steps, string time)
        {
            Show();
            subtitle.text = $"All {steps} checks passed · {time}";
            slide.ResetSlider();
        }
    }
}
