using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace DroneTrainer.UI
{
    /// <summary>
    /// Object Pool pattern: every action in the inspection spawns a short feedback label at the part.
    /// Instead of Instantiate/Destroy per event (GC spikes and canvas rebuilds on mobile), a fixed
    /// set of popups is created once and recycled: Get → Play → (animation ends) → Release.
    /// </summary>
    public class FeedbackPopupPool : MonoBehaviour
    {
        public int defaultCapacity = 6;
        public int maxSize = 12;

        ObjectPool<FeedbackPopup> pool;
        RectTransform area;
        public int CountActive => pool?.CountActive ?? 0;
        public int CountInactive => pool?.CountInactive ?? 0;

        void Awake()
        {
            area = (RectTransform)transform;
            pool = new ObjectPool<FeedbackPopup>(
                createFunc: Create,
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => Destroy(p.gameObject),
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
            // warm up so the first popups don't allocate mid-inspection
            var warm = new FeedbackPopup[defaultCapacity];
            for (int i = 0; i < warm.Length; i++) warm[i] = pool.Get();
            foreach (var p in warm) pool.Release(p);
        }

        FeedbackPopup Create()
        {
            var t = UITheme.Current;
            var label = UIFactory.Text(transform, "Popup", "", 34, Color.white, TextStyle.Display, TextAlignmentOptions.Center);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color32(13, 16, 20, 255);
            var rt = label.RT();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600, 60);
            var popup = label.gameObject.AddComponent<FeedbackPopup>();
            popup.label = label;
            label.gameObject.SetActive(false);
            return popup;
        }

        /// <summary>Show a popup at a world position (projected to the HUD) or the screen centre.</summary>
        public void Spawn(string text, Color color, Camera cam = null, Vector3? world = null)
        {
            Vector2 pos = new Vector2(0, 120);
            if (cam && world.HasValue)
            {
                var sp = cam.WorldToScreenPoint(world.Value);
                if (sp.z > 0) RectTransformUtility.ScreenPointToLocalPointInRectangle(area, sp, null, out pos);
            }
            var p = pool.Get();
            p.transform.SetAsLastSibling();
            p.Play(text, color, pos, pool.Release);
        }
    }
}
