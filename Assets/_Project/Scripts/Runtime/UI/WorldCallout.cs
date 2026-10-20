using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DroneTrainer.UI
{
    /// <summary>Billboard label that tracks a world-space part on screen (WorldCallout in the sheet).</summary>
    public class WorldCallout : MonoBehaviour
    {
        public TextMeshProUGUI label;
        public Image background;
        public Transform target;
        public Camera cam;
        public Vector2 screenOffset = new Vector2(0, 90);
        RectTransform rt, canvasRt;

        public static WorldCallout Create(Transform parent)
        {
            var t = UITheme.Current;
            var bg = UIFactory.Img(parent, "Callout", t.accent, t.rounded);
            var c = bg.gameObject.AddComponent<WorldCallout>();
            c.background = bg;
            var h = bg.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(22, 22, 10, 10);
            h.childControlWidth = h.childControlHeight = true;
            var fit = bg.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            c.label = UIFactory.Text(bg.transform, "Text", "TAP TO INSPECT", 24, t.bg0, TextStyle.Label, TextAlignmentOptions.Center);
            c.label.characterSpacing = 4;
            bg.RT().At(0, 0, 0.5f, 0, 300, 60);
            bg.gameObject.SetActive(false);
            return c;
        }

        public void Point(Transform worldTarget, Camera camera, string text, Color color)
        {
            target = worldTarget;
            cam = camera;
            label.text = text;
            background.color = color;
            gameObject.SetActive(worldTarget && camera);
        }

        public void Clear() { target = null; gameObject.SetActive(false); }

        void LateUpdate()
        {
            if (!target || !cam) { gameObject.SetActive(false); return; }
            if (!rt) { rt = (RectTransform)transform; canvasRt = (RectTransform)rt.parent; }
            Vector3 sp = cam.WorldToScreenPoint(target.position);
            if (sp.z < 0) { background.enabled = label.enabled = false; return; }
            background.enabled = label.enabled = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out var local);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = local + screenOffset;
        }
    }
}
