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
        public GameObject prefab;
        public string scenarioFile;
        public TextAsset fallbackScenario;
        public List<SpecEntry> specs = new List<SpecEntry>();
    }
}
