using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneTrainer.AR
{
    /// <summary>
    /// 3D inspection viewer (design change: replaces AR placement, see Docs/DesignChanges.md).
    /// Sketchfab-style controls: drag = orbit (incl. underneath), right-drag / two fingers = pan,
    /// scroll / pinch = zoom, smooth damping, and the camera glides to a part when it is opened.
    /// F = frame the whole drone. T = demo the tracking-loss overlay.
    /// </summary>
    public class SimulatorPlacement : MonoBehaviour, IPlacementProvider
    {
        public Camera simCamera;
        public Collider deskSurface;
        public float scanDelay = 0.2f;
        [Header("Viewer")]
        public float distance = 0.55f;
        public float yaw = -35f, pitch = 25f;
        public float minDistance = 0.08f, maxDistance = 1.6f;
        public float damping = 10f;
        public float orbitSpeed = 0.3f;

        public bool IsAR => false;
        public bool HasSurface { get; private set; }
        public int SurfaceCount => HasSurface ? 1 : 0;
        public bool IsPlaced { get; private set; }
        public bool IsTracking { get; private set; } = true;
        public Camera ActiveCamera => simCamera;

        public event Action SurfaceFound;
        public event Action<bool> TrackingChanged;

        Transform drone;
        Vector3 focus, focusTarget, home;
        float yawTarget, pitchTarget, distTarget, homeDist;

        void Awake()
        {
            yawTarget = yaw; pitchTarget = pitch; distTarget = homeDist = distance;
        }

        public void Activate()
        {
            gameObject.SetActive(true);
            if (simCamera) simCamera.gameObject.SetActive(true);
            focus = focusTarget = deskSurface ? deskSurface.bounds.center + Vector3.up * deskSurface.bounds.extents.y : Vector3.zero;
            Apply();
            StartCoroutine(FakeScan());
        }

        IEnumerator FakeScan()
        {
            yield return new WaitForSeconds(scanDelay);
            HasSurface = true;
            SurfaceFound?.Invoke();
        }

        public void BeginPlacement(Transform d)
        {
            drone = d;
            IsPlaced = false;
            drone.gameObject.SetActive(true);
            drone.position = focusTarget;
        }

        public bool TryPlaceAt(Vector2 screenPos)
        {
            if (!HasSurface || !drone) return false;
            var ray = simCamera.ScreenPointToRay(screenPos);
            if (deskSurface && deskSurface.Raycast(ray, out var hit, 10f))
            {
                drone.position = hit.point;
                IsPlaced = true;
                return true;
            }
            return false;
        }

        public void Rotate(float degrees) { if (drone) drone.Rotate(Vector3.up, degrees, Space.World); }
        public void SetScale(float s) { if (drone) drone.localScale = Vector3.one * s; }
        public void Reposition() { IsPlaced = false; if (drone) drone.position = focusTarget; }

        /// <summary>Centres the view on the drone and remembers it as the "home" view.</summary>
        public void Confirm()
        {
            IsPlaced = true;
            var b = DroneBounds();
            home = b.center;
            homeDist = Mathf.Clamp(b.size.magnitude * 1.6f, minDistance, maxDistance);
            focus = focusTarget = home;
            distance = distTarget = homeDist;
            Apply();
        }

        public void Orbit(Vector2 delta)
        {
            yawTarget += delta.x * orbitSpeed;
            pitchTarget = Mathf.Clamp(pitchTarget - delta.y * orbitSpeed * 0.8f, -85f, 85f);
        }

        public void Pan(Vector2 delta)
        {
            if (!simCamera) return;
            float k = distance * 0.0016f;
            var t = simCamera.transform;
            focusTarget -= (t.right * delta.x + t.up * delta.y) * k;
        }

        public void Zoom(float ratio) =>
            distTarget = Mathf.Clamp(distTarget / Mathf.Max(0.01f, ratio), minDistance, maxDistance);

        /// <summary>Glide to a point (e.g. the part being inspected). Null = back to the whole drone.</summary>
        public void FocusOn(Vector3? point, float dist = -1)
        {
            focusTarget = point ?? home;
            distTarget = point.HasValue ? (dist > 0 ? dist : homeDist * 0.45f) : homeDist;
        }

        Bounds DroneBounds()
        {
            if (!drone) return new Bounds(focusTarget, Vector3.one * 0.3f);
            Bounds? b = null;
            foreach (var r in drone.GetComponentsInChildren<Renderer>())
            {
                if (r.name.EndsWith("_Highlight")) continue;
                if (b == null) b = r.bounds; else { var x = b.Value; x.Encapsulate(r.bounds); b = x; }
            }
            return b ?? new Bounds(drone.position, Vector3.one * 0.3f);
        }

        void Apply()
        {
            if (!simCamera) return;
            var rot = Quaternion.Euler(pitch, yaw, 0);
            simCamera.transform.position = focus - rot * Vector3.forward * distance;
            simCamera.transform.rotation = rot;
        }

        void LateUpdate()
        {
            float k = 1f - Mathf.Exp(-damping * Time.unscaledDeltaTime);
            yaw = Mathf.LerpAngle(yaw, yawTarget, k);
            pitch = Mathf.Lerp(pitch, pitchTarget, k);
            distance = Mathf.Lerp(distance, distTarget, k);
            focus = Vector3.Lerp(focus, focusTarget, k);
            Apply();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.tKey.wasPressedThisFrame)
            {
                IsTracking = !IsTracking;
                TrackingChanged?.Invoke(IsTracking);
            }
            if (kb.fKey.wasPressedThisFrame) FocusOn(null);
        }
    }
}
