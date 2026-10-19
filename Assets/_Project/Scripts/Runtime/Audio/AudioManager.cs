using UnityEngine;
using DroneTrainer.Core;

namespace DroneTrainer.Audio
{
    /// <summary>One-shot SFX + motor loop + haptics. Created on first use and kept across scenes.</summary>
    public class AudioManager : MonoBehaviour
    {
        static AudioManager instance;
        AudioLibrary library;
        AudioSource sfx, loop;

        static AudioManager Instance
        {
            get
            {
                if (instance) return instance;
                var go = new GameObject("[AudioManager]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<AudioManager>();
                instance.library = Resources.Load<AudioLibrary>("AudioLibrary");
                instance.sfx = go.AddComponent<AudioSource>();
                instance.sfx.playOnAwake = false;
                instance.loop = go.AddComponent<AudioSource>();
                instance.loop.playOnAwake = false;
                instance.loop.loop = true;
                return instance;
            }
        }

        public static void Play(string name)
        {
            if (string.IsNullOrEmpty(name) || !AppSettings.Sound || !Application.isPlaying) return;
            var i = Instance;
            var c = i.library ? i.library.Get(name) : null;
            if (c != null) i.sfx.PlayOneShot(c.clip, c.volume);
        }

        public static void PlayLoop(string name, float pitch = 1f)
        {
            if (!Application.isPlaying) return;
            var i = Instance;
            var c = i.library ? i.library.Get(name) : null;
            if (c == null || !AppSettings.Sound) { i.loop.Stop(); return; }
            i.loop.clip = c.clip;
            i.loop.volume = c.volume;
            i.loop.pitch = pitch;
            if (!i.loop.isPlaying) i.loop.Play();
        }

        public static void SetLoopPitch(float pitch) { if (instance) instance.loop.pitch = pitch; }
        public static void StopLoop() { if (instance) instance.loop.Stop(); }

        public static void Haptic()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (AppSettings.Haptics && Application.isPlaying) Handheld.Vibrate();
#endif
        }
    }
}
