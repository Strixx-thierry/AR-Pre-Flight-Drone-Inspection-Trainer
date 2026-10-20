using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using DroneTrainer.Audio;
using DroneTrainer.Data;
using DroneTrainer.UI;

namespace DroneTrainer.EditorTools
{
    /// <summary>Prints a pass/fail checklist of everything the build needs.</summary>
    public static class ProjectValidator
    {
        public static bool Run()
        {
            var checks = new List<(string name, bool ok, string fix)>();
            void Check(string n, bool ok, string fix) => checks.Add((n, ok, fix));

            Check("AR Foundation installed", ProjectConfigurator.FindType("UnityEngine.XR.ARFoundation.ARSession") != null, "1 · Install Packages");
            Check("ARCore plugin installed", ProjectConfigurator.FindType("UnityEngine.XR.ARCore.ARCoreLoader") != null, "1 · Install Packages");
            var featureT = ProjectConfigurator.FindType("UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature");
            var mobile = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/Mobile_Renderer.asset");
            Check("URP AR background feature", featureT != null && mobile && mobile.rendererFeatures.Any(f => f && f.GetType() == featureT), "2 · Configure Project");
            Check("XR loader settings asset", AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget").Length > 0, "2 · Configure Project");
            Check("Landscape only", !PlayerSettings.allowedAutorotateToPortrait && PlayerSettings.allowedAutorotateToLandscapeLeft, "2 · Configure Project");
            Check("Android ARM64 / IL2CPP", PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64, "2 · Configure Project");
            Check("TextMeshPro essentials", AssetDatabase.FindAssets("t:TMP_Settings").Length > 0, "2 · Configure Project");

            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemeBuilder.ThemePath);
            Check("UI theme + icons", theme && theme.icons.Count > 20 && theme.rounded, "3 · Generate Theme & UI Kit");
            Check("Highlight shader", Shader.Find("DroneTrainer/RimHighlight") != null, "check Shaders/RimHighlight.shader compiles");

            var catalog = AssetDatabase.LoadAssetAtPath<DroneCatalog>(Paths.Resources + "/DroneCatalog.asset");
            Check("Drone catalog with a playable drone", catalog && catalog.FirstUnlocked() != null, "4 · Import Drones");
            Check("Scenario JSON in StreamingAssets", File.Exists(Paths.Scenarios + "/skyfold_f4_standard.json"), "restore StreamingAssets/Scenarios");

            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(Paths.Resources + "/AudioLibrary.asset");
            Check("SFX library", lib && lib.clips.Count >= 10, "5 · Generate SFX");

            Check("MainMenu scene", File.Exists(Paths.MenuScene), "6 · Build Scene ▸ Main Menu");
            Check("ARInspection scene", File.Exists(Paths.ARScene), "6 · Build Scene ▸ AR Inspection");
            var scenes = EditorBuildSettings.scenes.Select(s => s.path).ToList();
            Check("Build settings scene order", scenes.Count >= 2 && scenes[0] == Paths.MenuScene && scenes.Contains(Paths.ARScene), "7 · Build Everything");

            var sb = new StringBuilder("[DroneTrainer] Validation\n");
            foreach (var c in checks) sb.AppendLine($"{(c.ok ? "  ✔" : "  ✘")} {c.name}{(c.ok ? "" : "   → " + c.fix)}");
            bool all = checks.All(c => c.ok);
            sb.AppendLine(all ? "All checks passed." : $"{checks.Count(c => !c.ok)} issue(s).");
            if (all) Debug.Log(sb.ToString()); else Debug.LogWarning(sb.ToString());
            return all;
        }
    }
}
