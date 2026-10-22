using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DroneTrainer.EditorTools
{
    /// <summary>Step 2: Android/ARCore player settings, landscape lock, URP mobile, AR background, TMP essentials.</summary>
    public static class ProjectConfigurator
    {
        public const string AppId = "com.tekaccess.droneinspection";

        public static void Run()
        {
            PlayerAndOrientation();
            RenderPipeline();
            ArBackgroundFeature();
            XrLoader();
            TmpEssentials();
            AssetDatabase.SaveAssets();
            Debug.Log("[DroneTrainer] Project configured for Android / ARCore (landscape).");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[DroneTrainer] Switching build target to Android…");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }
        }

        static void PlayerAndOrientation()
        {
            PlayerSettings.productName = "Pre-Flight Trainer";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, AppId);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if ((int)PlayerSettings.Android.minSdkVersion < 26)
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            try
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            }
            catch (Exception e) { Debug.LogWarning("[DroneTrainer] Graphics API: " + e.Message); }
        }

        static void RenderPipeline()
        {
            var mobile = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (!mobile) { Debug.LogWarning("[DroneTrainer] Mobile_RPAsset not found."); return; }
            GraphicsSettings.defaultRenderPipeline = mobile;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = mobile;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        /// <summary>URP needs ARBackgroundRendererFeature on the renderer or the camera feed is black.</summary>
        static void ArBackgroundFeature()
        {
            var featureType = FindType("UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature");
            if (featureType == null) { Debug.Log("[DroneTrainer] AR Foundation not installed yet; skipping background feature."); return; }
            foreach (var path in new[] { "Assets/Settings/Mobile_Renderer.asset", "Assets/Settings/PC_Renderer.asset" })
            {
                var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                if (!data || data.rendererFeatures.Any(f => f && f.GetType() == featureType)) continue;
                var feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(featureType);
                feature.name = "ARBackgroundRendererFeature";
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.SaveAssets();
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
                var so = new SerializedObject(data);
                var list = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(data);
                Debug.Log("[DroneTrainer] Added ARBackgroundRendererFeature to " + path);
            }
        }

        /// <summary>Enables the ARCore loader for Android in XR Plug-in Management (reflection keeps compile safe).</summary>
        static void XrLoader()
        {
            var generalT = FindType("UnityEngine.XR.Management.XRGeneralSettings");
            var perTargetT = FindType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget");
            var managerT = FindType("UnityEngine.XR.Management.XRManagerSettings");
            var storeT = FindType("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore");
            if (generalT == null || perTargetT == null || managerT == null || storeT == null)
            {
                Debug.Log("[DroneTrainer] XR Management not installed yet; skipping loader setup.");
                return;
            }
            try
            {
                string key = (string)generalT.GetField("k_SettingsKey", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                EditorBuildSettings.TryGetConfigObject(key, out UnityEngine.Object perTargetObj);
                if (!perTargetObj)
                {
                    Paths.Ensure("Assets/XR");
                    perTargetObj = ScriptableObject.CreateInstance(perTargetT);
                    AssetDatabase.CreateAsset(perTargetObj, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                    EditorBuildSettings.AddConfigObject(key, perTargetObj, true);
                }
                var group = BuildTargetGroup.Android;
                var settings = Call(perTargetObj, "SettingsForBuildTarget", group) as ScriptableObject;
                if (!settings)
                {
                    settings = ScriptableObject.CreateInstance(generalT);
                    settings.name = "Android Settings";
                    AssetDatabase.AddObjectToAsset(settings, perTargetObj);
                    Call(perTargetObj, "SetSettingsForBuildTarget", group, settings);
                }
                var manager = generalT.GetProperty("AssignedSettings").GetValue(settings) as ScriptableObject;
                if (!manager)
                {
                    manager = ScriptableObject.CreateInstance(managerT);
                    manager.name = "Android Providers";
                    AssetDatabase.AddObjectToAsset(manager, perTargetObj);
                    generalT.GetProperty("AssignedSettings").SetValue(settings, manager);
                }
                generalT.GetProperty("InitManagerOnStart")?.SetValue(settings, true);
                var assign = storeT.GetMethod("AssignLoader", BindingFlags.Public | BindingFlags.Static);
                bool ok = (bool)assign.Invoke(null, new object[] { manager, "UnityEngine.XR.ARCore.ARCoreLoader", group });
                EditorUtility.SetDirty(perTargetObj);
                EditorUtility.SetDirty(settings);
                EditorUtility.SetDirty(manager);
                Debug.Log(ok ? "[DroneTrainer] ARCore loader enabled for Android." :
                               "[DroneTrainer] ARCore loader already enabled (or could not be assigned). Check Project Settings ▸ XR Plug-in Management.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[DroneTrainer] Could not configure XR loader automatically: " + e.Message +
                                 "\nEnable ARCore manually: Project Settings ▸ XR Plug-in Management ▸ Android ▸ ARCore.");
            }
        }

        static void TmpEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return;
            var root = Path.GetFullPath("Packages/com.unity.ugui");
            if (!Directory.Exists(root)) return;
            var pkg = Directory.GetFiles(root, "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).FirstOrDefault();
            if (pkg == null) { Debug.LogWarning("[DroneTrainer] TMP Essential Resources package not found."); return; }
            AssetDatabase.ImportPackage(pkg, false);
            Debug.Log("[DroneTrainer] Imported TMP Essential Resources.");
        }

        public static Type FindType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(fullName, false)).FirstOrDefault(t => t != null);

        static object Call(object target, string method, params object[] args)
        {
            var m = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                          .FirstOrDefault(x => x.Name == method && x.GetParameters().Length == args.Length);
            return m?.Invoke(target, args);
        }
    }
}
