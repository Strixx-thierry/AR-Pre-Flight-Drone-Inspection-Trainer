using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneTrainer.AR
{
    /// <summary>
    /// Desk simulator: the full flow without an AR device (editor testing and a demo fallback).
    /// Press T in the editor to simulate a tracking loss.
    /// </summary>
    public class SimulatorPlacement : MonoBehaviour, IPlacementProvider
    {
        public Camera simCamera;
        public Collider deskSurface;
        public float scanDelay = 1.4f;
        [Header("Orbit")]
        public float distance = 0.75f;
        public float yaw = -35f, pitch = 32f;
        public float minDistance = 0.25f, maxDistance = 1.6f;

        public bool IsAR => false;
        public bool HasSurface { get; private set; }
        public int SurfaceCount => HasSurface ? 1 : 0;
        public bool IsPlaced { get; private set; }
        public bool IsTracking { get; private set; } = true;
        public Camera ActiveCamera => simCamera;

        public event Action SurfaceFound;
        public event Action<bool> TrackingChanged;

        Transform drone;
        Vector3 focus;
        float scale = 1f;

        public void Activate()
        {
            gameObject.SetActive(true);
            if (simCamera) simCamera.gameObject.SetActive(true);
            focus = deskSurface ? deskSurface.bounds.center + Vector3.up * deskSurface.bounds.extents.y : Vector3.zero;
            ApplyOrbit();
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
            drone.position = focus;
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

        public void SetScale(float s)
        {
            scale = s;
            if (drone) drone.localScale = Vector3.one * s;
        }

        public void Reposition() { IsPlaced = false; if (drone) drone.position = focus; }

        public void Confirm()
        {
            IsPlaced = true;
            if (drone) focus = drone.position + Vector3.up * 0.03f * scale;
            ApplyOrbit();
        }

        public void Orbit(Vector2 delta)
        {
            yaw += delta.x * 0.25f;
            pitch = Mathf.Clamp(pitch - delta.y * 0.2f, 5f, 85f);
            ApplyOrbit();
        }

        public void Zoom(float ratio)
        {
            distance = Mathf.Clamp(distance / Mathf.Max(0.01f, ratio), minDistance, maxDistance);
            ApplyOrbit();
        }

        void ApplyOrbit()
        {
            if (!simCamera) return;
            var rot = Quaternion.Euler(pitch, yaw, 0);
            simCamera.transform.position = focus - rot * Vector3.forward * distance;
            simCamera.transform.rotation = rot;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.tKey.wasPressedThisFrame)
            {
                IsTracking = !IsTracking;
                TrackingChanged?.Invoke(IsTracking);
            }
        }
    }
}
