using System;
using TMPro;
using UnityEngine;

namespace DroneTrainer.UI
{
    /// <summary>Short floating label ("+1 SAFETY ERROR", "LOCKED"…) that rises, fades, then returns to its pool.</summary>
    public class FeedbackPopup : MonoBehaviour
    {
        public TextMeshProUGUI label;
        public float lifetime = 1.3f;
        public float rise = 110f;

        Action<FeedbackPopup> release;
        Vector2 start;
        float age;

        public void Play(string text, Color color, Vector2 anchoredPos, Action<FeedbackPopup> onDone)
        {
            label.text = text;
            label.color = color;
            start = anchoredPos;
            age = 0;
            release = onDone;
            ((RectTransform)transform).anchoredPosition = start;
            transform.localScale = Vector3.one * 0.8f;
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(age / lifetime);
            ((RectTransform)transform).anchoredPosition = start + Vector2.up * rise * Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, Mathf.Clamp01(t * 4));
            label.alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
            if (t >= 1f) release?.Invoke(this);
        }
    }
}
