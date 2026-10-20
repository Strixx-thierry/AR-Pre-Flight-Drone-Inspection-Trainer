using UnityEngine;

namespace DroneTrainer.UI
{
    /// <summary>Keeps touch UI clear of notches / punch-holes (landscape camera cut-out).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        Rect last;

        void OnEnable() => Apply();
        void Update() { if (Screen.safeArea != last) Apply(); }

        void Apply()
        {
            last = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            var min = new Vector2(last.xMin / Screen.width, last.yMin / Screen.height);
            var max = new Vector2(last.xMax / Screen.width, last.yMax / Screen.height);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
