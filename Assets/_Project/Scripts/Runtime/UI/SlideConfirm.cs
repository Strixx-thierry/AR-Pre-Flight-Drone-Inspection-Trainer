using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DroneTrainer.Audio;

namespace DroneTrainer.UI
{
    /// <summary>Slide-to-confirm control (lock the latch / arm the motors). Can't fire from a stray tap.</summary>
    public class SlideConfirm : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform track;
        public RectTransform thumb;
        public Image thumbImage;
        public Image thumbIcon;
        public TextMeshProUGUI label;
        public event Action Confirmed;

        float progress;
        bool dragging;

        public static SlideConfirm Create(Transform parent, string name, string text, string icon, Color thumbColor)
        {
            var t = UITheme.Current;
            var bg = UIFactory.Img(parent, name, UITheme.A(t.bg1, 0.9f), t.pill, true);
            bg.Size(-1, UIFactory.TouchSize);
            var o = bg.gameObject.AddComponent<Outline>();
            o.effectColor = UITheme.A(thumbColor, 0.6f);
            o.effectDistance = new Vector2(2, -2);
            var s = bg.gameObject.AddComponent<SlideConfirm>();
            s.track = bg.RT();
            s.label = UIFactory.Text(bg.transform, "Label", text, 28, t.textDim, TextStyle.Label, TextAlignmentOptions.Center);
            s.label.RT().Fill(140, 0, 30, 0);
            s.thumbImage = UIFactory.Img(bg.transform, "Thumb", thumbColor, t.circle);
            s.thumb = s.thumbImage.RT().At(0, 0.5f, 0, 0.5f, 112, 112, 10, 0);
            s.thumbIcon = UIFactory.Icon(s.thumb, "Icon", icon, 52, UITheme.Hex("1A1100"));
            s.thumbIcon.RT().At(0.5f, 0.5f, 0.5f, 0.5f, 52, 52);
            return s;
        }

        public void SetText(string text) { if (label) label.text = text; }

        public void ResetSlider()
        {
            progress = 0;
            Layout();
        }

        void OnEnable() => ResetSlider();

        float Travel => Mathf.Max(1, track.rect.width - thumb.rect.width - 20);

        public void OnBeginDrag(PointerEventData e) => dragging = true;

        public void OnDrag(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(track, e.position, e.pressEventCamera, out var local);
            float x = local.x - track.rect.xMin - thumb.rect.width * 0.5f - 10;
            progress = Mathf.Clamp01(x / Travel);
            Layout();
        }

        public void OnEndDrag(PointerEventData e)
        {
            dragging = false;
            if (progress > 0.88f)
            {
                progress = 1;
                Layout();
                AudioManager.Play("confirm");
                Confirmed?.Invoke();
            }
        }

        void Update()
        {
            if (!dragging && progress > 0 && progress < 1)
            {
                progress = Mathf.MoveTowards(progress, 0, Time.unscaledDeltaTime * 3f);
                Layout();
            }
        }

        void Layout()
        {
            if (!thumb || !track) return;
            thumb.anchoredPosition = new Vector2(10 + progress * Travel, 0);
            if (label) label.alpha = 1 - progress;
        }
    }
}
