using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DroneTrainer.EditorTools
{
    public static class Paths
    {
        public const string Root = "Assets/_Project";
        public const string Data = Root + "/Data";
        public const string Resources = Root + "/Resources";
        public const string Prefabs = Root + "/Prefabs";
        public const string Materials = Root + "/Materials";
        public const string Sprites = Root + "/Sprites";
        public const string Icons = Root + "/Sprites/Icons";
        public const string Scenes = Root + "/Scenes";
        public const string Sfx = Root + "/Audio/SFX";
        public const string Models = "Assets/Models/Drones";
        public const string Scenarios = "Assets/StreamingAssets/Scenarios";
        public const string MenuScene = Scenes + "/MainMenu.unity";
        public const string ARScene = Scenes + "/ARInspection.unity";

        public static void Ensure(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            var parent = Path.GetDirectoryName(assetFolder).Replace('\\', '/');
            Ensure(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetFolder));
        }

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a) return a;
            Ensure(Path.GetDirectoryName(path).Replace('\\', '/'));
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }
    }

    /// <summary>The importer: Tools ▸ Drone Trainer. Each step is safe to re-run.</summary>
    public static class DTMenu
    {
        const string M = "Tools/Drone Trainer/";
        public static void InstallPackages() => PackageInstaller.Install(null);
        public static void Configure() => ProjectConfigurator.Run();
        public static void Theme() => ThemeBuilder.Run();
        public static void Drones() => DroneImporter.Run();
        public static void Sfx() => SfxGenerator.Run(force: false);
        public static void MenuScene() { if (SaveGuard()) SceneBuilders.BuildMenu(); }
        public static void ARScene() { if (SaveGuard()) SceneBuilders.BuildAR(); }

        [MenuItem(M + "Rebuild Scenes", priority = 1)]
        public static void BuildEverything()
        {
            if (!SaveGuard()) return;
            BuildAll();
            EditorSceneManager.OpenScene(Paths.MenuScene);
            EditorUtility.DisplayDialog("Drone Trainer", "Everything rebuilt.\nPress Play in MainMenu to test (desk simulator in the editor).", "OK");
        }

        public static void BuildAll()
        {
            ThemeBuilder.Run();
            SfxGenerator.Run(force: false);
            DroneImporter.Run();
            SceneBuilders.BuildMenu();
            SceneBuilders.BuildAR();
            SceneBuilders.RegisterScenes();
            ProjectValidator.Run();
        }
        public static void Validate() => ProjectValidator.Run();
        public static void SfxForce() => SfxGenerator.Run(force: true);

        [MenuItem(M + "Build Android APK", priority = 2)]
        public static void Apk() => AndroidBuilder.Build();
        public static void OpenMenu() { if (SaveGuard()) EditorSceneManager.OpenScene(Paths.MenuScene); }

        static bool SaveGuard() => Application.isBatchMode || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }

    /// <summary>Command-line entry points (unity -batchmode -executeMethod ...).</summary>
    public static class Batch
    {
        public static void InstallPackages() => PackageInstaller.Install(ok => EditorApplication.Exit(ok ? 0 : 1));

        public static void Configure() => Run(ProjectConfigurator.Run);

        public static void BuildAll() => Run(DTMenu.BuildAll);

        /// <summary>TMP essentials import is asynchronous — wait for it before quitting.</summary>
        public static void ImportTmp()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) { EditorApplication.Exit(0); return; }
            AssetDatabase.importPackageCompleted += _ => { AssetDatabase.SaveAssets(); EditorApplication.Exit(0); };
            AssetDatabase.importPackageFailed += (_, err) => { Debug.LogError("[DroneTrainer] TMP import failed: " + err); EditorApplication.Exit(1); };
            var root = Path.GetFullPath("Packages/com.unity.ugui");
            var pkg = Directory.GetFiles(root, "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
            if (pkg.Length == 0) { Debug.LogError("[DroneTrainer] TMP package not found"); EditorApplication.Exit(1); return; }
            AssetDatabase.ImportPackage(pkg[0], false);
        }

        public static void BuildApk() => Run(() => { if (!AndroidBuilder.Build()) throw new Exception("APK build failed"); });

        static void Run(Action a)
        {
            try { a(); AssetDatabase.SaveAssets(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
