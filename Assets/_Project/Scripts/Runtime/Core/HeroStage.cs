using UnityEngine;
using DroneTrainer.Data;
using DroneTrainer.Drone;

namespace DroneTrainer.Core
{
    /// <summary>Menu turntable that shows the selected drone (hangar hero shot).</summary>
    public class HeroStage : MonoBehaviour
    {
        public Transform pivot;
        public float spinSpeed = 14f;
        public float bobAmount = 0.008f;
        GameObject current;
        string currentId;

        public void Show(DroneDefinition def)
        {
            if (!def || !def.prefab || def.id == currentId) return;
            if (current) Destroy(current);
            currentId = def.id;
            current = Instantiate(def.prefab, pivot ? pivot : transform);
            current.transform.localPosition = Vector3.zero;
            current.transform.localRotation = Quaternion.identity;
            foreach (var c in current.GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var m in current.GetComponentsInChildren<AInspectableComponent>()) m.enabled = false;
            var dc = current.GetComponent<DroneController>();
            if (dc) dc.SpinProps(0);
        }

        void Update()
        {
            var p = pivot ? pivot : transform;
            p.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            if (current) current.transform.localPosition = Vector3.up * Mathf.Sin(Time.time * 1.4f) * bobAmount;
        }
    }
}
