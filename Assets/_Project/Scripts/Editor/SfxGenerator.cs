using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DroneTrainer.Audio;

namespace DroneTrainer.EditorTools
{
    /// <summary>
    /// Step 5: synthesises placeholder SFX into Assets/_Project/Audio/SFX and builds Resources/AudioLibrary.
    /// Drop your own file with the same name (wav/ogg/mp3) to replace any sound — it is never overwritten.
    /// </summary>
    public static class SfxGenerator
    {
        const int Rate = 44100;
        static readonly System.Random Rng = new System.Random(7);

        static readonly Dictionary<string, Func<float[]>> Sounds = new Dictionary<string, Func<float[]>>
        {
            ["tap"] = () => Tone(0.03f, t => Sin(t, 1800) * Decay(t, 0.008f) * 0.5f),
            ["tick"] = () => Tone(0.02f, t => Sin(t, 2600) * Decay(t, 0.004f) * 0.45f),
            ["select"] = () => Tone(0.08f, t => Sin(t, Lerp(900, 1400, t / 0.08f)) * Decay(t, 0.03f) * 0.5f),
            ["success"] = () => Concat(Note(880, 0.09f), Note(1320, 0.16f)),
            ["confirm"] = () => Concat(Tone(0.12f, t => Sin(t, Lerp(600, 1200, t / 0.12f)) * Env(t, 0.12f) * 0.45f), Note(1568, 0.18f)),
            ["found"] = () => Concat(Note(660, 0.08f, 0.35f), Note(990, 0.14f, 0.35f)),
            ["warning"] = () => Concat(Note(520, 0.08f, 0.45f, square: true), Silence(0.05f), Note(520, 0.08f, 0.45f, square: true)),
            ["error"] = () => Concat(Buzz(180, 0.12f), Silence(0.05f), Buzz(150, 0.16f)),
            ["blocked"] = () => Concat(Buzz(700, 0.12f), Silence(0.04f), Buzz(600, 0.12f), Silence(0.04f), Buzz(480, 0.2f)),
            ["latch"] = () => Concat(Tone(0.012f, t => Noise() * Decay(t, 0.003f) * 0.8f), Silence(0.035f),
                                     Tone(0.07f, t => (Sin(t, 120) * 0.8f + Noise() * 0.25f) * Decay(t, 0.02f) * 0.8f)),
            ["place"] = () => Tone(0.12f, t => (Sin(t, 85) + Noise() * 0.15f) * Decay(t, 0.035f) * 0.8f),
            ["swipe"] = () => Swoosh(0.28f),
            ["motor"] = () => Motor(1f),
        };

        public static void Run(bool force)
        {
            Paths.Ensure(Paths.Sfx);
            int made = 0;
            foreach (var kv in Sounds)
            {
                bool exists = Directory.GetFiles(Paths.Sfx, kv.Key + ".*").Any(f => !f.EndsWith(".meta"));
                if (exists && !force) continue;
                WriteWav($"{Paths.Sfx}/{kv.Key}.wav", kv.Value());
                made++;
            }
            AssetDatabase.Refresh();

            var lib = Paths.LoadOrCreate<AudioLibrary>(Paths.Resources + "/AudioLibrary.asset");
            lib.clips.Clear();
            foreach (var f in Directory.GetFiles(Paths.Sfx).Where(f => f.EndsWith(".wav") || f.EndsWith(".ogg") || f.EndsWith(".mp3")))
            {
                var path = f.Replace('\\', '/');
                var imp = (AudioImporter)AssetImporter.GetAtPath(path);
                if (imp != null)
                {
                    var s = imp.defaultSampleSettings;
                    s.loadType = AudioClipLoadType.DecompressOnLoad;
                    imp.defaultSampleSettings = s;
                    imp.forceToMono = true;
                    imp.SaveAndReimport();
                }
                var name = Path.GetFileNameWithoutExtension(path);
                lib.clips.Add(new NamedClip
                {
                    name = name,
                    clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path),
                    volume = name == "motor" ? 0.55f : name == "tap" || name == "tick" ? 0.6f : 0.85f
                });
            }
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DroneTrainer] SFX: {made} generated, {lib.clips.Count} in AudioLibrary.");
        }

        // ------------------------------------------------------------------ synthesis
        static float[] Tone(float seconds, Func<float, float> f)
        {
            var d = new float[Mathf.CeilToInt(seconds * Rate)];
            for (int i = 0; i < d.Length; i++) d[i] = f(i / (float)Rate);
            return d;
        }

        static float[] Note(float hz, float seconds, float vol = 0.45f, bool square = false) =>
            Tone(seconds, t => (square ? Mathf.Sign(Sin(t, hz)) * 0.5f : Sin(t, hz) + 0.25f * Sin(t, hz * 2)) * Env(t, seconds) * vol);

        static float[] Buzz(float hz, float seconds) =>
            Tone(seconds, t => (Saw(t, hz) * 0.6f + Sin(t, hz * 0.5f) * 0.4f) * Env(t, seconds) * 0.5f);

        static float[] Swoosh(float seconds)
        {
            var d = new float[Mathf.CeilToInt(seconds * Rate)];
            float lp = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate, k = Lerp(0.02f, 0.25f, t / seconds);
                lp += (Noise() - lp) * k;
                d[i] = lp * Mathf.Sin(Mathf.PI * t / seconds) * 1.6f;
            }
            return d;
        }

        /// <summary>Exactly 140 cycles per second over 1 s so the loop is seamless.</summary>
        static float[] Motor(float seconds) => Tone(seconds, t =>
            (Saw(t, 140) * 0.35f + Sin(t, 280) * 0.25f + Sin(t, 420) * 0.12f + Noise() * 0.08f) * 0.6f);

        static float[] Silence(float s) => new float[Mathf.CeilToInt(s * Rate)];
        static float[] Concat(params float[][] parts) => parts.SelectMany(p => p).ToArray();
        static float Sin(float t, float hz) => Mathf.Sin(2 * Mathf.PI * hz * t);
        static float Saw(float t, float hz) => 2f * (t * hz - Mathf.Floor(t * hz + 0.5f));
        static float Noise() => (float)(Rng.NextDouble() * 2 - 1);
        static float Decay(float t, float tau) => Mathf.Exp(-t / tau);
        static float Env(float t, float len) => Mathf.Clamp01(t / 0.005f) * Mathf.Clamp01((len - t) / Mathf.Min(0.04f, len * 0.5f));
        static float Lerp(float a, float b, float t) => a + (b - a) * Mathf.Clamp01(t);

        static void WriteWav(string path, float[] samples)
        {
            using var fs = new FileStream(path, FileMode.Create);
            using var w = new BinaryWriter(fs);
            int dataLen = samples.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + dataLen);
            w.Write(new[] { 'W', 'A', 'V', 'E' }); w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(dataLen);
            foreach (var s in samples) w.Write((short)(Mathf.Clamp(s, -1f, 1f) * 32000));
        }
    }
}
