#if DT_ARFOUNDATION
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DroneTrainer.AR
{
    /// <summary>
    /// AR Foundation placement: horizontal plane detection (FR-01), ghost preview that follows
    /// the screen centre, tap to place (FR-02), twist/scale, anchor on confirm, tracking monitor.
    /// </summary>
    public class ARPlacementManager : MonoBehaviour, IPlacementProvider
    {
        public ARSession session;
        public ARPlaneManager planeManager;
        public ARRaycastManager raycastManager;
        public Camera arCamera;
        public GameObject originRoot;

        public bool IsAR => true;
        public bool HasSurface => SurfaceCount > 0;
        public int SurfaceCount => planeManager ? planeManager.trackables.count : 0;
        public bool IsPlaced { get; private set; }
        public bool IsTracking { get; private set; } = true;
        public Camera ActiveCamera => arCamera;

        public event Action SurfaceFound;
        public event Action<bool> TrackingChanged;

        static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        Transform drone;
        bool following, surfaceAnnounced, planesVisible = true;
        float scale = 1f;

        /// <summary>Runs ARSession.CheckAvailability and reports whether AR can be used.</summary>
        public static IEnumerator CheckSupport(Action<bool> done)
        {
            if (Application.isEditor) { done(false); yield break; }
            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
                yield return ARSession.CheckAvailability();
            done(ARSession.state != ARSessionState.Unsupported);
        }

        public void Activate()
        {
            if (originRoot) originRoot.SetActive(true);
            if (session) session.gameObject.SetActive(true);
            if (planeManager)
            {
                planeManager.enabled = true;
                planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            }
            ARSession.stateChanged += OnSessionState;
        }

        void OnDestroy() => ARSession.stateChanged -= OnSessionState;

        void OnSessionState(ARSessionStateChangedEventArgs e)
        {
            bool tracking = e.state == ARSessionState.SessionTracking;
            if (tracking == IsTracking) return;
            IsTracking = tracking;
            TrackingChanged?.Invoke(tracking);
        }

        public void BeginPlacement(Transform d)
        {
            drone = d;
            IsPlaced = false;
            following = true;
            SetPlanesVisible(true);
        }

        void Update()
        {
            if (!surfaceAnnounced && HasSurface)
            {
                surfaceAnnounced = true;
                SurfaceFound?.Invoke();
            }
            if (!planesVisible && planeManager)
                foreach (var p in planeManager.trackables) if (p.gameObject.activeSelf) p.gameObject.SetActive(false);

            if (following && drone && arCamera)
            {
                var centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                if (Raycast(centre, out var pose))
                {
                    if (!drone.gameObject.activeSelf) drone.gameObject.SetActive(true);
                    drone.position = pose.position;
                    FaceCamera();
                }
            }
        }

        bool Raycast(Vector2 screen, out Pose pose)
        {
            pose = default;
            if (!raycastManager || !raycastManager.Raycast(screen, hits, TrackableType.PlaneWithinPolygon)) return false;
            pose = hits[0].pose;
            return true;
        }

        void FaceCamera()
        {
            Vector3 toCam = arCamera.transform.position - drone.position;
            toCam.y = 0;
            if (toCam.sqrMagnitude > 1e-4f) drone.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        }

        public bool TryPlaceAt(Vector2 screenPos)
        {
            if (!drone || !Raycast(screenPos, out var pose)) return false;
            drone.gameObject.SetActive(true);
            drone.position = pose.position;
            if (following) FaceCamera();
            following = false;
            IsPlaced = true;
            return true;
        }

        public void Rotate(float degrees) { if (drone) drone.Rotate(Vector3.up, degrees, Space.World); }

        public void SetScale(float s)
        {
            scale = s;
            if (drone) drone.localScale = Vector3.one * s;
        }

        public void Reposition()
        {
            var anchor = drone ? drone.GetComponent<ARAnchor>() : null;
            if (anchor) Destroy(anchor);
            IsPlaced = false;
            following = true;
            SetPlanesVisible(true);
        }

        public void Confirm()
        {
            following = false;
            IsPlaced = true;
            if (drone && !drone.GetComponent<ARAnchor>()) drone.gameObject.AddComponent<ARAnchor>();
            SetPlanesVisible(false);
        }

        void SetPlanesVisible(bool v)
        {
            planesVisible = v;
            if (!planeManager) return;
            foreach (var p in planeManager.trackables) p.gameObject.SetActive(v);
        }

        public void Orbit(Vector2 screenDelta) { }
        public void Zoom(float ratio) { }
        public void Pan(Vector2 screenDelta) { }
        public void FocusOn(Vector3? point, float distance = -1) { }
    }
}
#endif
