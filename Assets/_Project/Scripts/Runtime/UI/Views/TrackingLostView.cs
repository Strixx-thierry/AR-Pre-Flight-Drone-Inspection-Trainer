using TMPro;
using System;
using UnityEngine;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S14 tracking-loss recovery overlay (NFR reliability): timer paused, anchor frozen.</summary>
    public class TrackingLostView : UIView
    {
        public UIButton replaceButton;
        public event Action Replace;
        float shownAt;

        protected override void Build()
        {
            Scrim(new Color(0.02f, 0.03f, 0.035f, 0.78f));
            var v = VStack(transform, "V", 16, TextAnchor.MiddleCenter);
            v.At(0.5f, 0.5f, 0.5f, 0.5f, 1300, 620);
            var iconRow = HStack(v, "IconRow", 0, TextAnchor.MiddleCenter);
            Icon(iconRow, "Icon", "eye", 130, T.warn);
            Text(v, "Title", "TRACKING LOST", 72, T.text, TextStyle.Display, TextAlignmentOptions.Center);
            Text(v, "Sub", "Point your camera back at the desk. Your progress is saved.", 30, T.textDim,
                 TextStyle.Body, TextAlignmentOptions.Center);
            var tips = HStack(v, "Tips", 14, TextAnchor.MiddleCenter);
            Chip(tips, "T1", "MORE LIGHT", ChipStyle.Neutral, "info");
            Chip(tips, "T2", "MOVE SLOWLY", ChipStyle.Neutral, "phone");
            Chip(tips, "T3", "AIM AT THE SURFACE", ChipStyle.Neutral, "target");
            var st = HStack(v, "State", 14, TextAnchor.MiddleCenter);
            Chip(st, "Timer", "TIMER PAUSED", ChipStyle.Warn, "pause");
            Chip(st, "Anchor", "ANCHOR FROZEN", ChipStyle.Neutral, "lock");

            replaceButton = Button(transform, "Replace", "RE-PLACE DRONE", ButtonStyle.Ghost, "reset");
            replaceButton.RT().At(1, 0, 1, 0, 460, TouchSize, -GutterX, GutterY);
        }

        protected override void Bind() => replaceButton.Clicked += () => Replace?.Invoke();

        public override void Show()
        {
            base.Show();
            shownAt = Time.unscaledTime;
            replaceButton.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!replaceButton.gameObject.activeSelf && Time.unscaledTime - shownAt > 10f)
                replaceButton.gameObject.SetActive(true);
        }
    }
}
