using System.Collections;
using UnityEngine;

namespace DroneTrainer.UI
{
    /// <summary>
    /// Base for every screen/overlay. <see cref="Build"/> creates the hierarchy (run by the editor
    /// scene builders so scenes are visible and editable); <see cref="Bind"/> wires listeners at runtime.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIView : MonoBehaviour
    {
        [SerializeField, HideInInspector] bool built;
        protected UITheme T => UITheme.Current;
        protected RectTransform Root => (RectTransform)transform;
        bool bound;

        public bool IsVisible => gameObject.activeSelf;

        public void EnsureBuilt()
        {
            if (built) return;
            UIFactory.Clear(transform);
            Build();
            built = true;
        }

        public void Rebuild()
        {
            built = false;
            EnsureBuilt();
        }

        protected abstract void Build();
        protected virtual void Bind() { }

        protected virtual void Awake()
        {
            EnsureBuilt();
            if (!bound) { bound = true; Bind(); }
        }

        public virtual void Show()
        {
            EnsureBuilt();
            gameObject.SetActive(true);
            if (!bound) { bound = true; Bind(); }
            var cg = GetComponent<CanvasGroup>();
            if (cg && isActiveAndEnabled) StartCoroutine(Fade(cg));
        }

        public virtual void Hide() => gameObject.SetActive(false);

        public void SetVisible(bool v) { if (v && !IsVisible) Show(); else if (!v && IsVisible) Hide(); }

        IEnumerator Fade(CanvasGroup cg)
        {
            cg.alpha = 0;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.15f)
            {
                cg.alpha = t;
                yield return null;
            }
            cg.alpha = 1;
        }

        /// <summary>Content rect inset by the design gutters.</summary>
        protected RectTransform Content(string name = "Content")
        {
            var c = UIFactory.Rect(name, transform);
            c.Fill(UIFactory.GutterX, UIFactory.GutterY, UIFactory.GutterX, UIFactory.GutterY);
            return c;
        }

        /// <summary>Full-screen dim layer that blocks input behind modals.</summary>
        protected UnityEngine.UI.Image Scrim(Color c)
        {
            var s = UIFactory.Img(transform, "Scrim", c, null, true);
            s.RT().Fill();
            return s;
        }
    }
}
