using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DroneTrainer.Data;

namespace DroneTrainer.Drone
{
    /// <summary>
    /// Composition root of a drone (SRS: DroneController handles component composition).
    /// Owns the inspectable parts, picking, ghost preview and launch animation.
    /// </summary>
    public class DroneController : MonoBehaviour
    {
        public string droneId;
        public Material ghostMaterial;

        public IReadOnlyList<AInspectableComponent> Parts { get { Init(); return parts; } }
        List<AInspectableComponent> parts;
        readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        Collider[] colliders;
        bool ghost;

        void Awake() => Init();

        void Init()
        {
            if (parts != null) return;
            parts = GetComponentsInChildren<AInspectableComponent>(true).ToList();
            colliders = GetComponentsInChildren<Collider>(true);
        }

        public AInspectableComponent Find(string partId) => Parts.FirstOrDefault(p => p.PartId == partId);
        public IEnumerable<T> PartsOf<T>() where T : AInspectableComponent => Parts.OfType<T>();

        public void ApplyScenario(FaultConfig faults)
        {
            foreach (var p in Parts) p.ApplyScenario(faults);
            foreach (var p in Parts) if (p.highlighter) p.highlighter.SyncMeshes();
        }

        /// <summary>Raycast tap selection (FR-04). Colliders are oversized for touch (NFR usability).</summary>
        public AInspectableComponent Pick(Ray ray, float maxDistance = 20f)
        {
            if (ghost) return null;
            var hits = Physics.RaycastAll(ray, maxDistance);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (!h.collider.transform.IsChildOf(transform)) continue;
                var proxy = h.collider.GetComponent<InspectableProxy>();
                if (proxy && proxy.target && proxy.target.isActiveAndEnabled) return proxy.target;
                var part = h.collider.GetComponentInParent<AInspectableComponent>();
                if (part) return part;
            }
            return null;
        }

        public void SetHighlights(HighlightState state)
        {
            foreach (var p in Parts) if (p.highlighter) p.highlighter.Set(state);
        }

        public void SpinProps(float revsPerSecond)
        {
            foreach (var r in PartsOf<RotorModule>()) r.SetSpin(revsPerSecond);
        }

        /// <summary>Translucent preview while placing (S06).</summary>
        public void SetGhost(bool on)
        {
            Init();
            if (ghost == on) return;
            ghost = on;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r.name.EndsWith("_Highlight")) continue;
                if (on)
                {
                    if (!ghostMaterial) continue;
                    originalMaterials[r] = r.sharedMaterials;
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
                    r.sharedMaterials = mats;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                else if (originalMaterials.TryGetValue(r, out var m))
                {
                    r.sharedMaterials = m;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
            }
            if (!on) originalMaterials.Clear();
            foreach (var c in colliders) if (c) c.enabled = !on;
            if (on) SetHighlights(HighlightState.Off);
        }

        public Bounds WorldBounds()
        {
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(transform.position, Vector3.one * 0.3f);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
