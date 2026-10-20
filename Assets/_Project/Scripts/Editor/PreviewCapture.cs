using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using DroneTrainer.Core;
using DroneTrainer.Data;
using DroneTrainer.Drone;
using DroneTrainer.Tasks;
using DroneTrainer.UI;

namespace DroneTrainer.EditorTools
{
    /// <summary>
    /// Renders every generated screen with sample data to PNG (Design/renders/unity) without entering
    /// Play mode. Used to check layouts and for report screenshots. Scenes are not saved.
    /// </summary>
    public static class PreviewCapture
    {
        const int W = 2340, H = 1080;
        static string outDir;

        public static void CaptureMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CaptureAll();
            EditorUtility.RevealInFinder(outDir);
        }

        public static void CaptureAll()
        {
            outDir = Path.GetFullPath("Design/renders/unity");
            Directory.CreateDirectory(outDir);
            var catalog = AssetDatabase.LoadAssetAtPath<DroneCatalog>(Paths.Resources + "/DroneCatalog.asset");
            var def = catalog.FirstUnlocked();
            var scenario = ScenarioConfig.FromJson(File.ReadAllText($"{Paths.Scenarios}/{def.scenarioFile}"));

            // ------------------------------------------------------------ menu scene
            EditorSceneManager.OpenScene(Paths.MenuScene);
            catalog = AssetDatabase.LoadAssetAtPath<DroneCatalog>(Paths.Resources + "/DroneCatalog.asset");
            def = catalog.FirstUnlocked();
            var cam = GameObject.Find("Main Camera").GetComponent<Camera>();
            var stage = UnityEngine.Object.FindFirstObjectByType<HeroStage>();
            var drone = (GameObject)PrefabUtility.InstantiatePrefab(def.prefab, stage.pivot);
            stage.pivot.localRotation = Quaternion.Euler(0, 205, 0);
            var views = Views();
            var home = Get<HomeView>(views);
            Shot("S01_Splash", cam, views, () => Get<SplashView>(views).SetProgress(0.6f, "ARCore OK · Camera OK"), Get<SplashView>(views));
            Shot("S02_Home", cam, views, () => home.Set(def, 6, 204, 88, true), home);
            Shot("S03_Hangar", cam, views, () =>
            {
                var hv = Get<HangarView>(views);
                hv.Open(catalog, def.id);
                Debug.Log($"[Diag] hangar cards={hv.cards.childCount} specs={hv.specRows.childCount} cardsRect={hv.cards.rect} " +
                          $"cardsPos={hv.cards.position} active={hv.cards.gameObject.activeInHierarchy} catalog={catalog.drones.Count}");
            });
            Shot("S04_Briefing", cam, views, () => Get<BriefingView>(views).Open(def, scenario, TrainingMode.Guided));
            Shot("S16_Records", cam, views, () => Get<RecordsView>(views).Open(SampleSessions(def), 204));
            Shot("Modal_Settings", cam, views, () => { home.Set(def, 6, 204, 88, true); Get<SettingsView>(views).Show(); }, home);
            Shot("Modal_Help", cam, views, () => { home.Set(def, 6, 204, 88, true); Get<HelpView>(views).Show(); }, home);
            UnityEngine.Object.DestroyImmediate(drone);

            // ------------------------------------------------------------ AR scene (desk simulator)
            EditorSceneManager.OpenScene(Paths.ARScene);
            catalog = AssetDatabase.LoadAssetAtPath<DroneCatalog>(Paths.Resources + "/DroneCatalog.asset");
            def = catalog.FirstUnlocked();
            var sim = UnityEngine.Object.FindObjectsByType<AR.SimulatorPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
            sim.gameObject.SetActive(true);
            var simCam = sim.simCamera;
            var focus = sim.deskSurface.bounds.center + Vector3.up * sim.deskSurface.bounds.extents.y;
            var rot = Quaternion.Euler(30, 145, 0);
            simCam.transform.position = focus + Vector3.up * 0.03f - rot * Vector3.forward * 0.62f;
            simCam.transform.rotation = rot;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(def.prefab);
            go.transform.position = focus;
            var dc = go.GetComponent<DroneController>();
            views = Views();
            var hud = Get<HUDView>(views);
            hud.SetupChecklist(scenario);
            var rows = hud.checklistRows.GetComponentsInChildren<ChecklistRow>();
            rows[0].Set(StepStatus.Done, 1, 1);
            rows[1].Set(StepStatus.Active, 1, 4);
            rows[2].Set(StepStatus.Locked, 0, 1);
            rows[3].Set(StepStatus.Locked, 0, 1);
            hud.checklistCount.text = "1/4";
            hud.stepPill.SetText($"STEP <color=#{ColorUtility.ToHtmlStringRGB(UITheme.Current.accent)}>2</color>/4");
            hud.timerPill.SetText("01:24");
            hud.SetInstruction(scenario.steps[1].icon, scenario.steps[1].instruction, scenario.steps[1].helper);
            hud.SetHint(scenario.steps[1].hint, true);
            hud.SetCalibrateVisible(false);
            hud.launchButton.SetInteractable(false);
            void Highlights()
            {
                foreach (var p in dc.GetComponentsInChildren<AInspectableComponent>())
                    p.highlighter?.Set(p.PartId == "Prop_FL" ? HighlightState.Done :
                                       p.PartId == "Prop_FR" ? HighlightState.Active :
                                       p.PartId.StartsWith("Prop") ? HighlightState.Active : HighlightState.Idle);
            }

            Shot("S05_Scan", simCam, views, () => { go.SetActive(false); Get<ScanView>(views).SetSurfaceCount(1); }, Get<ScanView>(views));
            Shot("S06_Place", simCam, views, () => { go.SetActive(true); dc.SetGhost(true); Get<PlaceView>(views).SetPlaced(true); }, Get<PlaceView>(views));
            dc.SetGhost(false);
            Shot("S07_HUD", simCam, views, Highlights, hud);
            Shot("S08_RotorPanel", simCam, views, () => { Highlights(); hud.SetHint(null, false); Get<RotorPanelView>(views).Open("ROTOR · FRONT RIGHT", 2, 4, false); }, hud);
            Shot("S09_Battery", simCam, views, () =>
            {
                var latch = dc.GetComponentInChildren<BatteryLatchModule>();
                latch.highlighter?.Set(HighlightState.Warn);
                Get<BatteryPanelView>(views).Show();
                Get<BatteryPanelView>(views).Set(latch);
            }, hud);
            Shot("S10_Gimbal", simCam, views, () =>
            {
                dc.SetHighlights(HighlightState.Off);
                dc.GetComponentInChildren<GimbalCoverModule>().highlighter?.Set(HighlightState.Active);
                Get<GimbalPanelView>(views).Show();
            }, hud);
            Shot("S11_Calibration", simCam, views, () => { }, Get<CalibrationView>(views));
            Shot("S12_LaunchBlocked", simCam, views, () =>
            {
                dc.SetHighlights(HighlightState.Off);
                dc.GetComponentInChildren<BatteryLatchModule>().highlighter?.Set(HighlightState.Fault);
                Get<LaunchGateView>(views).Open(scenario, new List<StepConfig> { scenario.steps[0] });
            }, hud);
            Shot("S13_LaunchReady", simCam, views, () => { dc.SetHighlights(HighlightState.Off); Get<LaunchReadyView>(views).Open(4, "03:42"); });
            Shot("S14_TrackingLost", simCam, views, () => Get<TrackingLostView>(views).Show(), hud);
            Shot("S15_Report", simCam, views, () => Get<ReportView>(views).Open(SampleSessions(def)[0], 222));
            Shot("S17_Pause", simCam, views, () => Get<PauseView>(views).Open(false), hud);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            Debug.Log("[DroneTrainer] Previews written to " + outDir);
        }

        static List<UIView> Views() =>
            UnityEngine.Object.FindObjectsByType<UIView>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();

        static T Get<T>(List<UIView> v) where T : UIView => v.OfType<T>().First();

        static void Shot(string name, Camera cam, List<UIView> views, Action setup, params UIView[] visible)
        {
            foreach (var v in views) v.gameObject.SetActive(false);
            foreach (var v in visible) v.gameObject.SetActive(true);
            setup();
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 0.3f;
            var camData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (!camData) camData = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camData.renderShadows = false;
            camData.renderPostProcessing = false;
            foreach (var cg in canvas.GetComponentsInChildren<CanvasGroup>(true)) cg.alpha = 1;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            for (int i = 0; i < 3; i++)
            {
                foreach (var r in canvas.GetComponentsInChildren<RectTransform>()) LayoutRebuilder.MarkLayoutForRebuild(r);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                Canvas.ForceUpdateCanvases();
                foreach (var t in canvas.GetComponentsInChildren<TMPro.TMP_Text>())
                {
                    t.SetAllDirty();
                    t.ForceMeshUpdate(true, true);
                    // edit-mode captures skip the material hand-off that the player loop normally does
                    if (t.canvasRenderer.materialCount == 0 || !t.canvasRenderer.GetMaterial())
                    {
                        t.canvasRenderer.materialCount = 1;
                        t.canvasRenderer.SetMaterial(t.fontSharedMaterial, 0);
                        t.canvasRenderer.SetTexture(t.fontSharedMaterial.mainTexture);
                    }
                }
            }
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
        }

        static List<SessionResult> SampleSessions(DroneDefinition def)
        {
            var list = new List<SessionResult>();
            float[] times = { 222, 204, 240, 261, 302, 370 };
            int[] scores = { 88, 94, 58, 79, 72, 61 };
            for (int i = 0; i < times.Length; i++)
            {
                var r = new SessionResult
                {
                    droneId = def.id, droneName = def.displayName, mode = i % 2 == 0 ? "Guided" : "Assessment",
                    seconds = times[i], score = scores[i], passed = scores[i] >= 70,
                    safetyErrors = i == 2 ? 3 : i == 0 ? 1 : 0, minorErrors = i % 3 == 0 ? 1 : 0,
                    utcTicks = DateTime.UtcNow.AddHours(-i * 20).Ticks
                };
                r.steps = new List<StepResult>
                {
                    new StepResult { id = "battery", title = "Battery latch", seconds = 31, errors = i == 0 ? 1 : 0, completed = true },
                    new StepResult { id = "props", title = "Propellers", seconds = 108, completed = true, note = "1 replaced" },
                    new StepResult { id = "gimbal", title = "Gimbal clamp", seconds = 22, completed = true },
                    new StepResult { id = "calibration", title = "Controller calibration", seconds = 61, completed = true },
                };
                if (i == 0)
                {
                    r.errors.Add(new ErrorEntry { time = 29, message = "Attempted launch with battery latch not secured.", severity = ErrorSeverity.Safety });
                    r.errors.Add(new ErrorEntry { time = 95, message = "False alarm on Rotor · rear left: the blade was fine.", severity = ErrorSeverity.Minor });
                }
                list.Add(r);
            }
            return list;
        }
    }

    public static partial class BatchCapture
    {
        public static void DiagnoseText()
        {
            EditorSceneManager.OpenScene(Paths.MenuScene);
            var home = UnityEngine.Object.FindObjectsByType<HomeView>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
            home.gameObject.SetActive(true);
            var t = home.pilotName;
            t.ForceMeshUpdate(true, true);
            var f = t.font;
            Debug.Log($"[Diag] text='{t.text}' font={(f ? f.name : "NULL")} mode={(f ? f.atlasPopulationMode.ToString() : "-")} " +
                      $"atlas={(f && f.atlasTexture ? f.atlasTexture.width + "x" + f.atlasTexture.height : "none")} glyphs={(f ? f.glyphTable.Count : 0)} " +
                      $"chars={t.textInfo.characterCount} visible={t.textInfo.characterInfo.Take(t.textInfo.characterCount).Count(c => c.isVisible)} " +
                      $"verts={(t.mesh ? t.mesh.vertexCount : -1)} mat={(t.fontSharedMaterial ? t.fontSharedMaterial.shader.name : "NULL")} " +
                      $"color={t.color} alpha={t.alpha} size={t.rectTransform.rect.size} enabled={t.enabled} canvasRenderer={(t.canvasRenderer ? t.canvasRenderer.GetMaterial() : null)}");
            EditorApplication.Exit(0);
        }

        public static void Run()
        {
            try { PreviewCapture.CaptureAll(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
