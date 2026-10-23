using System.Collections.Generic;
using UnityEngine;

namespace DroneTrainer.Data
{
    [CreateAssetMenu(menuName = "Drone Trainer/Drone Catalog")]
    public class DroneCatalog : ScriptableObject
    {
        public List<DroneDefinition> drones = new List<DroneDefinition>();

        static DroneCatalog cached;
        public static DroneCatalog Load() => cached ? cached : (cached = Resources.Load<DroneCatalog>("DroneCatalog"));

        public DroneDefinition Find(string id)
        {
            foreach (var d in drones) if (d && d.id == id && !d.locked && d.HasModel) return d;
            return FirstUnlocked();
        }

        public DroneDefinition FirstUnlocked()
        {
            foreach (var d in drones) if (d && !d.locked && d.HasModel) return d;
            return null;
        }
    }
}
