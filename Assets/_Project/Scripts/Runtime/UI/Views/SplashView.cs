using TMPro;
using UnityEngine;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S01 splash &amp; AR support check.</summary>
    public class SplashView : UIView
    {
        public UnityEngine.UI.Image progress;
        public TextMeshProUGUI status;

        protected override void Build()
        {
            var bg = Img(transform, "Background", T.bg1, T.gradient, true);
            bg.RT().Fill();
            var v = VStack(transform, "V", 10, TextAnchor.MiddleCenter);
            v.At(0.5f, 0.5f, 0.5f, 0.5f, 1200, 560);
            var iconRow = HStack(v, "IconRow", 0, TextAnchor.MiddleCenter);
            Icon(iconRow, "Logo", "drone", 150, T.accent);
            Text(v, "Title", $"PRE<color=#{ColorUtility.ToHtmlStringRGB(T.accent)}>·</color>FLIGHT", 130, T.text,
                 TextStyle.Display, TextAlignmentOptions.Center).Size(-1, 150);
            Text(v, "Sub", "AR DRONE INSPECTION TRAINER", 30, T.textDim, TextStyle.Label, TextAlignmentOptions.Center);
            var barRow = HStack(v, "BarRow", 0, TextAnchor.MiddleCenter, new RectOffset(260, 260, 30, 10));
            var bar = Bar(barRow, "Bar", 0.1f, T.accent);
            bar.transform.parent.Size(-1, 14, 1);
            progress = bar;
            status = Text(v, "Status", "Checking AR support…", 24, T.textDim, TextStyle.Mono, TextAlignmentOptions.Center);
            var foot = Text(transform, "Foot", "v1.0 · Summative Assignment 1 · Not affiliated with any drone manufacturer",
                            20, T.textMute, TextStyle.Body, TextAlignmentOptions.Center);
            foot.RT().At(0.5f, 0, 0.5f, 0, 1600, 50, 0, 40);
        }

        public void SetProgress(float v, string text)
        {
            SetBar(progress, v);
            status.text = text;
        }
    }
}
