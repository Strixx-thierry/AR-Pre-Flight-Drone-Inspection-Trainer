using System;
using UnityEngine;
using DroneTrainer.Core;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>Settings rows bound to <see cref="AppSettings"/> (PlayerPrefs).</summary>
    public class SettingsList : MonoBehaviour
    {
        public UIToggle sound, haptics, hints, planes;
        public event Action Changed;

        public static SettingsList Create(Transform parent, bool inAR)
        {
            var v = VStack(parent, "Settings", 0);
            var s = v.gameObject.AddComponent<SettingsList>();
            s.sound = Row(v, "sound", "Sound effects");
            s.haptics = Row(v, "vibrate", "Haptics");
            s.hints = Row(v, "help", "Show hints (Guided)");
            s.planes = Row(v, "target", "Show detected surfaces");
            s.planes.transform.parent.gameObject.SetActive(GameSession.UseAR);   // AR-only setting
            return s;
        }

        static UIToggle Row(Transform parent, string icon, string label)
        {
            var t = UITheme.Current;
            var row = HStack(parent, "Row_" + label, 18);
            row.Size(-1, 96);
            Icon(row, "Icon", icon, 40, t.textDim);
            Text(row, "Label", label, 28, t.text).Size(-1, -1, 1);
            var tg = UIToggle.Create(row, "Toggle");
            Img(parent, "Rule", UITheme.A(t.line, 0.8f)).Size(-1, 2);
            return tg;
        }

        void Start()
        {
            if (!sound) return;
            sound.Changed += v => { AppSettings.Sound = v; Changed?.Invoke(); };
            haptics.Changed += v => { AppSettings.Haptics = v; Changed?.Invoke(); };
            hints.Changed += v => { AppSettings.Hints = v; Changed?.Invoke(); };
            planes.Changed += v => { AppSettings.ShowPlanes = v; Changed?.Invoke(); };
        }

        void OnEnable() => Refresh(hints && hints.locked);

        public void Refresh(bool hintsLocked)
        {
            if (!sound || !haptics || !hints || !planes) return;
            sound.Set(AppSettings.Sound);
            haptics.Set(AppSettings.Haptics);
            hints.locked = hintsLocked;
            hints.Set(AppSettings.Hints && !hintsLocked);
            planes.Set(AppSettings.ShowPlanes);
        }
    }
}
