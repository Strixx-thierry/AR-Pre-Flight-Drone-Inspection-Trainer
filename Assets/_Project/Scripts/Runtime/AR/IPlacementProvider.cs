using System;
using UnityEngine;

namespace DroneTrainer.AR
{
    /// <summary>
    /// Spatial mapping + placement (SRS: ARPlacementManager). Implemented by the real AR
    /// manager on device and by the desk simulator in the editor / on non-AR phones.
    /// </summary>
    public interface IPlacementProvider
    {
        bool IsAR { get; }
        bool HasSurface { get; }
        int SurfaceCount { get; }
        bool IsPlaced { get; }
        bool IsTracking { get; }
        Camera ActiveCamera { get; }

        event Action SurfaceFound;
        event Action<bool> TrackingChanged;

        void Activate();
        void BeginPlacement(Transform drone);
        bool TryPlaceAt(Vector2 screenPos);
        void Rotate(float degrees);
        void SetScale(float scale);
        void Reposition();
        void Confirm();
        void Orbit(Vector2 screenDelta);
        void Zoom(float ratio);
    }
}
