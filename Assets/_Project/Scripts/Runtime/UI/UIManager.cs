using UnityEngine;

namespace DroneTrainer.UI
{
    /// <summary>Per-scene view registry (SRS: UIManager coordinates overlays).</summary>
    public class UIManager : MonoBehaviour
    {
        public UIView[] views = new UIView[0];

        void Awake()
        {
            foreach (var v in views) if (v) v.EnsureBuilt();
        }

        public T Get<T>() where T : UIView
        {
            foreach (var v in views) if (v is T t) return t;
            return null;
        }

        public void HideAll()
        {
            foreach (var v in views) if (v) v.gameObject.SetActive(false);
        }

        public void ShowOnly(UIView view)
        {
            foreach (var v in views) if (v && v != view) v.gameObject.SetActive(false);
            if (view) view.Show();
        }
    }
}
