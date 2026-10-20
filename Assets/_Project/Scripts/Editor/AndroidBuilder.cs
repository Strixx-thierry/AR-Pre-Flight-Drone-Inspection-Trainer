using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DroneTrainer.EditorTools
{
    /// <summary>One-click APK into Builds/ (gitignored).</summary>
    public static class AndroidBuilder
    {
        public const string Output = "Builds/ARPreFlightTrainer.apk";

        public static bool Build()
        {
            if (!File.Exists(Paths.MenuScene) || !File.Exists(Paths.ARScene)) DTMenu.BuildAll();
            SceneBuilders.RegisterScenes();
            Directory.CreateDirectory("Builds");
            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = Output,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            bool ok = s.result == BuildResult.Succeeded;
            var msg = $"[DroneTrainer] APK build {s.result}: {Output} ({s.totalSize / (1024 * 1024)} MB, {s.totalErrors} errors, {s.totalTime:mm\\:ss})";
            if (ok) Debug.Log(msg); else Debug.LogError(msg);
            if (ok && !Application.isBatchMode) EditorUtility.RevealInFinder(Output);
            return ok;
        }
    }
}
