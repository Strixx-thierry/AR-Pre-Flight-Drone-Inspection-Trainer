using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace DroneTrainer.Data
{
    /// <summary>
    /// Checklist sequence, part ids and instruction strings live in JSON under
    /// StreamingAssets/Scenarios (NFR Maintainability) so they change without recompiling.
    /// </summary>
    [Serializable]
    public class ScenarioConfig
    {
        public string id;
        public string droneId;
        public string module;
        public string title;
        public float targetSeconds = 240;
        public StepConfig[] steps = Array.Empty<StepConfig>();
        public FaultConfig faults = new FaultConfig();

        public static ScenarioConfig FromJson(string json) => JsonUtility.FromJson<ScenarioConfig>(json);

        public int IndexOfPart(string partId)
        {
            for (int i = 0; i < steps.Length; i++)
                if (Array.IndexOf(steps[i].targets, partId) >= 0) return i;
            return -1;
        }
    }

    [Serializable]
    public class StepConfig
    {
        public string id;
        public string title;
        public string instruction;
        public string helper;
        public string hint;
        public string icon;
        public string type;          // Latch | Verdict | Remove | Calibration
        public bool critical;
        public string[] targets = Array.Empty<string>();

        public bool IsCalibration => type == "Calibration";
    }

    [Serializable]
    public class FaultConfig
    {
        public string[] crackedProps = Array.Empty<string>();
        public bool batteryStartsUnlocked = true;
        public bool clampFitted = true;
        public int batteryCharge = 3;
    }

    public static class ScenarioLoader
    {
        /// <summary>Reads StreamingAssets (UnityWebRequest on Android), falls back to the bundled TextAsset.</summary>
        public static IEnumerator Load(DroneDefinition drone, Action<ScenarioConfig> done)
        {
            string json = null;
            if (drone != null && !string.IsNullOrEmpty(drone.scenarioFile))
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Scenarios", drone.scenarioFile);
                if (path.Contains("://") || path.Contains("jar:"))
                {
                    using var req = UnityWebRequest.Get(path);
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success) json = req.downloadHandler.text;
                    else Debug.LogWarning($"[Scenario] {path}: {req.error}");
                }
                else if (File.Exists(path))
                {
                    json = File.ReadAllText(path);
                }
            }

            if (string.IsNullOrEmpty(json) && drone != null && drone.fallbackScenario != null)
            {
                Debug.LogWarning("[Scenario] Using bundled fallback scenario.");
                json = drone.fallbackScenario.text;
            }

            ScenarioConfig cfg = null;
            try { cfg = string.IsNullOrEmpty(json) ? null : ScenarioConfig.FromJson(json); }
            catch (Exception e) { Debug.LogError($"[Scenario] Invalid JSON: {e.Message}"); }
            done?.Invoke(cfg);
        }
    }
}
