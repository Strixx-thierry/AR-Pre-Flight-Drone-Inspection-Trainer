using System;
using System.Collections.Generic;
using UnityEngine;

namespace DroneTrainer.Data
{
    [Serializable]
    public class SpecEntry
    {
        public string label;
        public string value;
        public SpecEntry(string l, string v) { label = l; value = v; }
    }

    [CreateAssetMenu(menuName = "Drone Trainer/Drone Definition")]
    public class DroneDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string subtitle;
        public string difficultyLabel = "BEGINNER";
        [Range(1, 3)] public int difficulty = 1;
        public bool locked;
        public string lockedNote = "Coming soon";
        [Tooltip("Only used when Addressables is not installed")] public GameObject prefab;
        [Tooltip("Addressables key of the 3D model (group 'Drones')")] public string address;
        public bool HasModel => prefab || !string.IsNullOrEmpty(address);
        public Sprite heroImage;
        public string scenarioFile;
        public TextAsset fallbackScenario;
        public List<SpecEntry> specs = new List<SpecEntry>();
    }
}
