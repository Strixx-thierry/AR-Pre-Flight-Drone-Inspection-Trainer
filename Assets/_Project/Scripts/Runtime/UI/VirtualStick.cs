using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DroneTrainer.UI
{
    /// <summary>On-screen RC stick for the calibration step; lights 8 ticks at full deflection.</summary>
    public class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform knob;
        public Image knobImage;
        public Image[] ticks = new Image[8];
        public float radius = 150f;

        public Vector2 Value { get; private set; }
        public bool Held { get; private set; }
        public int TicksLit { get; private set; }
        public event Action TickLit;

        bool[] lit = new bool[8];

        public static VirtualStick Create(Transform parent, string name, float size)
        {
            var t = UITheme.Current;
            var bg = UIFactory.Img(parent, name, UITheme.Hex("12161B"), t.circle, true);
            bg.RT().sizeDelta = new Vector2(size, size);
            var ring = UIFactory.Img(bg.transform, "Ring", UITheme.Hex("1B2027"), t.circle);
            ring.RT().Fill(size * 0.2f, size * 0.2f, size * 0.2f, size * 0.2f);
            var s = bg.gameObject.AddComponent<VirtualStick>();
            s.radius = size * 0.36f;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var tick = UIFactory.Img(bg.transform, "Tick" + i, t.line, t.circle);
                tick.RT().At(0.5f, 0.5f, 0.5f, 0.5f, 24, 24, Mathf.Sin(a) * size * 0.46f, Mathf.Cos(a) * size * 0.46f);
                s.ticks[i] = tick;
            }
            s.knobImage = UIFactory.Img(bg.transform, "Knob", UITheme.Hex("2A3039"), t.circle);
            s.knob = s.knobImage.RT().At(0.5f, 0.5f, 0.5f, 0.5f, size * 0.34f, size * 0.34f);
            var o = s.knobImage.gameObject.AddComponent<Outline>();
            o.effectColor = t.accent;
            o.effectDistance = new Vector2(4, -4);
            return s;
        }

        public void ResetTicks()
        {
            lit = new bool[8];
            TicksLit = 0;
            foreach (var tk in ticks) if (tk) tk.color = UITheme.Current.line;
        }

        public void OnPointerDown(PointerEventData e) { Held = true; Move(e); }
        public void OnDrag(PointerEventData e) => Move(e);

        public void OnPointerUp(PointerEventData e)
        {
            Held = false;
            Value = Vector2.zero;
            knob.anchoredPosition = Vector2.zero;
        }

        void Move(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var p);
            p = Vector2.ClampMagnitude(p, radius);
            knob.anchoredPosition = p;
            Value = p / radius;
            if (Value.magnitude > 0.85f)
            {
                float ang = Mathf.Atan2(Value.x, Value.y) * Mathf.Rad2Deg;
                int idx = Mathf.RoundToInt(((ang + 360f) % 360f) / 45f) % 8;
                if (!lit[idx])
                {
                    lit[idx] = true;
                    TicksLit++;
                    ticks[idx].color = UITheme.Current.ok;
                    TickLit?.Invoke();
                }
            }
        }
    }
}
