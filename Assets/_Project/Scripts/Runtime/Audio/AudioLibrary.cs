using System;
using System.Collections.Generic;
using UnityEngine;

namespace DroneTrainer.Audio
{
    [Serializable]
    public class NamedClip
    {
        public string name;
        public AudioClip clip;
        [Range(0, 1)] public float volume = 0.8f;
    }

    /// <summary>SFX lookup by name. Filled by Tools ▸ Drone Trainer ▸ 5 · Generate SFX (Resources/AudioLibrary).</summary>
    [CreateAssetMenu(menuName = "Drone Trainer/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        public List<NamedClip> clips = new List<NamedClip>();

        public NamedClip Get(string name)
        {
            foreach (var c in clips) if (c != null && c.name == name && c.clip) return c;
            return null;
        }
    }
}
