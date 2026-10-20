using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DroneTrainer.Audio;

namespace DroneTrainer.UI
{
    public class UIToggle : MonoBehaviour, IPointerClickHandler
    {
        public Image track;
        public RectTransform knob;
        public Image knobImage;
        public bool isOn;
        public bool locked;
        public event Action<bool> Changed;

        public static UIToggle Create(Transform parent, string name)
        {
            var t = UITheme.Current;
            var bg = UIFactory.Img(parent, name, t.panel2, t.pill, true);
            bg.Size(110, 62);
            var tg = bg.gameObject.AddComponent<UIToggle>();
            tg.track = bg;
            tg.knobImage = UIFactory.Img(bg.transform, "Knob", t.textMute, t.circle);
            tg.knob = tg.knobImage.RT().At(0, 0.5f, 0, 0.5f, 46, 46, 8, 0);
            return tg;
        }

        public void Set(bool on, bool notify = false)
        {
            isOn = on;
            var t = UITheme.Current;
            track.color = locked ? UITheme.A(t.panel2, 0.5f) : on ? UITheme.A(t.accent, 0.25f) : t.panel2;
            knobImage.color = on ? t.accent : t.textMute;
            knob.anchorMin = knob.anchorMax = new Vector2(on ? 1 : 0, 0.5f);
            knob.pivot = new Vector2(on ? 1 : 0, 0.5f);
            knob.anchoredPosition = new Vector2(on ? -8 : 8, 0);
            if (notify) Changed?.Invoke(on);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (locked) return;
            AudioManager.Play("tap");
            Set(!isOn, true);
        }
    }
}
