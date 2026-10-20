using System;
using UnityEngine;
using DroneTrainer.Data;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>Menu settings modal (same toggles as the pause menu) + reset progress.</summary>
    public class SettingsView : UIView
    {
        public SettingsList settings;
        public UIButton closeButton, resetButton;
        public event Action Close, ProgressReset;

        protected override void Build()
        {
            Scrim(new Color(0.02f, 0.03f, 0.035f, 0.75f));
            var m = Panel(transform, "Modal", large: true);
            m.RT().At(0.5f, 0.5f, 0.5f, 0.5f, 1000, 820);
            var v = VStack(m.transform, "V", 10, TextAnchor.UpperLeft, new RectOffset(48, 48, 36, 36));
            v.Fill();
            var head = HStack(v, "Head", 10);
            Text(head, "Title", "SETTINGS", 52, T.text, TextStyle.Display).Size(-1, -1, 1);
            closeButton = RoundButton(head, "Close", "x", 96);
            settings = SettingsList.Create(v, inAR: false);
            Spacer(v);
            resetButton = Button(v, "Reset", "RESET PROGRESS", ButtonStyle.Ghost, "reset");
        }

        protected override void Bind()
        {
            closeButton.Clicked += () => Close?.Invoke();
            resetButton.Clicked += () =>
            {
                ProgressStore.Clear();
                resetButton.SetLabel("PROGRESS CLEARED");
                ProgressReset?.Invoke();
            };
        }

        public override void Show()
        {
            base.Show();
            settings.Refresh(false);
            resetButton.SetLabel("RESET PROGRESS");
        }
    }
}
