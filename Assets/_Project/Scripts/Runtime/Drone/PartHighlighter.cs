using UnityEngine;

namespace DroneTrainer.Drone
{
    public enum HighlightState { Off, Idle, Active, Done, Warn, Fault }

    /// <summary>
    /// FR-03 visual outline: a shell copy of the part's mesh drawn with the
    /// DroneTrainer/RimHighlight shader, tinted per state with a property block.
    /// </summary>
    public class PartHighlighter : MonoBehaviour
    {
        public Material highlightMaterial;
        public HighlightState Current { get; private set; } = HighlightState.Off;

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int PulseId = Shader.PropertyToID("_Pulse");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        MeshRenderer[] shells = new MeshRenderer[0];
        MaterialPropertyBlock block;

        void Awake() => EnsureShells();

        void EnsureShells()
        {
            if (shells.Length > 0 || !highlightMaterial) return;
            block = new MaterialPropertyBlock();
            var filters = GetComponentsInChildren<MeshFilter>(true);
            var list = new System.Collections.Generic.List<MeshRenderer>();
            foreach (var f in filters)
            {
                if (!f.sharedMesh || f.name.EndsWith("_Highlight")) continue;
                // only this part's own meshes, not child parts that have their own highlighter
                var owner = f.GetComponentInParent<PartHighlighter>();
                if (owner != this) continue;
                var go = new GameObject(f.name + "_Highlight");
                go.transform.SetParent(f.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = f.sharedMesh;
                var mr = go.AddComponent<MeshRenderer>();
                var mats = new Material[Mathf.Max(1, f.sharedMesh.subMeshCount)];
                for (int i = 0; i < mats.Length; i++) mats[i] = highlightMaterial;
                mr.sharedMaterials = mats;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                go.SetActive(false);
                list.Add(mr);
            }
            shells = list.ToArray();
        }

        /// <summary>Shells follow mesh swaps (e.g. the damaged propeller).</summary>
        public void SyncMeshes()
        {
            foreach (var s in shells)
            {
                var src = s.transform.parent.GetComponent<MeshFilter>();
                if (src) s.GetComponent<MeshFilter>().sharedMesh = src.sharedMesh;
            }
        }

        public void Set(HighlightState state)
        {
            EnsureShells();
            Current = state;
            bool on = state != HighlightState.Off;
            if (on)
            {
                var (c, pulse, intensity) = Style(state);
                block.SetColor(ColorId, c);
                block.SetFloat(PulseId, pulse);
                block.SetFloat(IntensityId, intensity);
            }
            foreach (var s in shells)
            {
                if (!s) continue;
                s.gameObject.SetActive(on);
                if (on) s.SetPropertyBlock(block);
            }
        }

        static (Color, float, float) Style(HighlightState s) => s switch
        {
            HighlightState.Active => (new Color(0.27f, 0.88f, 0.82f), 3.2f, 2.2f),
            HighlightState.Done => (new Color(0.29f, 0.87f, 0.50f), 0f, 1.4f),
            HighlightState.Warn => (new Color(1f, 0.69f, 0.13f), 4f, 2.2f),
            HighlightState.Fault => (new Color(1f, 0.30f, 0.37f), 5f, 2.4f),
            _ => (new Color(0.9f, 0.93f, 0.95f), 0f, 0.6f),
        };
    }
}
