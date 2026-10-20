using System;
using UnityEngine;
using DroneTrainer.Core;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S17 pause &amp; settings.</summary>
    public class PauseView : UIView
    {
        public UIButton resumeButton, restartButton, quitButton;
        public SettingsList settings;
        public event Action Resume, Restart, Quit;

        protected override void Build()
        {
            Scrim(new Color(0.02f, 0.03f, 0.035f, 0.7f));
            var m = Panel(transform, "Modal", large: true);
            m.RT().At(0.5f, 0.5f, 0.5f, 0.5f, 1360, 720);
            var h = HStack(m.transform, "H", 50, TextAnchor.UpperLeft, new RectOffset(52, 52, 44, 44));
            h.Fill();
            h.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandHeight = true;
            var left = VStack(h, "Left", 18);
            left.Size(500);
            Text(left, "Title", "PAUSED", 64, T.text, TextStyle.Display).Size(-1, 90);
            resumeButton = Button(left, "Resume", "RESUME", ButtonStyle.Primary, "play");
            restartButton = Button(left, "Restart", "RESTART", ButtonStyle.Ghost, "reset");
            quitButton = Button(left, "Quit", "QUIT TO HANGAR", ButtonStyle.Ghost, "exit");
            var right = VStack(h, "Right", 0);
            right.Size(-1, -1, 1);
            Text(right, "Label", "SETTINGS", 22, T.textMute, TextStyle.Label).Size(-1, 70);
            settings = SettingsList.Create(right, inAR: true);
        }

        protected override void Bind()
        {
            resumeButton.Clicked += () => Resume?.Invoke();
            restartButton.Clicked += () => Restart?.Invoke();
            quitButton.Clicked += () => Quit?.Invoke();
        }

        public void Open(bool assessment)
        {
            Show();
            settings.Refresh(hintsLocked: assessment);
        }
    }
}
