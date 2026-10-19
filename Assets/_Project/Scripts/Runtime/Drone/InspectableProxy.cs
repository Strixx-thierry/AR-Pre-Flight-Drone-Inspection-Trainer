using UnityEngine;

namespace DroneTrainer.Drone
{
    /// <summary>Routes taps on a larger neighbour collider (e.g. the battery body) to a part.</summary>
    public class InspectableProxy : MonoBehaviour
    {
        public AInspectableComponent target;
    }
}
