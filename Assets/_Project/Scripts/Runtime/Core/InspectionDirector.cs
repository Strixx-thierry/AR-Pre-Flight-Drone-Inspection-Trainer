using System.Collections;
using System.Linq;
using UnityEngine;
using DroneTrainer.AR;
using DroneTrainer.Audio;
using DroneTrainer.Data;
using DroneTrainer.Drone;
using DroneTrainer.Input;
using DroneTrainer.Tasks;
using DroneTrainer.UI;

namespace DroneTrainer.Core
{
    /// <summary>
    /// AR scene controller. Runs the high-level State pattern (Placing → Inspection → Evaluation)
    /// and routes input: AR raycaster → TaskManager logic → UI presentation (SRS §4 event flow).
    /// </summary>
    public class InspectionDirector : MonoBehaviour
    {
        [Header("Systems")]
        public TaskManager tasks;
        public InputRouter input;
        public UIManager ui;
        public SimulatorPlacement simulator;
        public MonoBehaviour arPlacement;     // ARPlacementManager when AR Foundation is installed
        public GameObject arRig;
        public GameObject simulatorRig;

        [Header("Views")]
        public HUDView hud;
        public ScanView scan;
        public PlaceView place;
        public RotorPanelView rotorPanel;
        public BatteryPanelView batteryPanel;
        public GimbalPanelView gimbalPanel;
        public CalibrationView calibration;
        public LaunchGateView gate;
        public LaunchReadyView ready;
        public TrackingLostView trackingLost;
        public PauseView pause;
        public ReportView report;

        public IPlacementProvider Placement { get; private set; }
        public DroneController Drone { get; private set; }

        DroneDefinition def;
        ScenarioConfig scenario;
        readonly StateMachine fsm = new StateMachine();
        AInspectableComponent openPart, dragPart;
        float scale = 1f, lostSince = -1f, bestBefore;
        bool trackingOk = true, started, inspectionBegun;

        // ================================================================== boot
        IEnumerator Start()
        {
            ui.HideAll();
            def = DroneCatalog.Load()?.Find(GameSession.DroneId);
            if (!def)
            {
                Debug.LogError("[AR] No drone available. Run Tools ▸ Drone Trainer ▸ Build Everything.");
                yield break;
            }
            yield return ScenarioLoader.Load(def, s => scenario = s);
            if (scenario == null) { SceneNames.Load(SceneNames.MainMenu); yield break; }
            bestBefore = ProgressStore.BestTime(def.id);

            var go = Instantiate(def.prefab);
            go.name = def.prefab.name;
            Drone = go.GetComponent<DroneController>();
            go.SetActive(false);

            bool arOk = false;
#if DT_ARFOUNDATION
            if (GameSession.UseAR && arPlacement) yield return ARPlacementManager.CheckSupport(ok => arOk = ok);
#endif
            if (arRig) arRig.SetActive(arOk);
            if (simulatorRig) simulatorRig.SetActive(!arOk);
            Placement = arOk ? arPlacement as IPlacementProvider : simulator;
            Placement.Activate();
            Placement.TrackingChanged += OnTracking;

            BindViews();
            BindInput();
            BindTasks();
            hud.SetTracking(true, Placement.IsAR);
            started = true;
            if (Placement.IsAR) fsm.Change(new PlacingState(this, resume: false));
            else
            {
                // 3D bay (design change): drone floats in view, no scan/place step
                Placement.BeginPlacement(Drone.transform);
                Placement.Confirm();
                fsm.Change(new InspectionState(this, resume: false));
            }
        }

        void Update()
        {
            if (!started) return;
            fsm.Tick();
            if (!trackingOk && lostSince > 0 && Time.unscaledTime - lostSince > 0.5f && !trackingLost.IsVisible
                && fsm.Current is InspectionState)
            {
                trackingLost.Show();
                tasks.SetPaused(true);
                input.Enabled = false;
            }
        }

        // ================================================================== states
        class PlacingState : IState
        {
            readonly InspectionDirector d;
            readonly bool resume;
            public PlacingState(InspectionDirector d, bool resume) { this.d = d; this.resume = resume; }

            public void Enter()
            {
                d.CloseAllPanels();
                d.hud.Hide();
                d.Drone.SetGhost(true);
                d.Placement.BeginPlacement(d.Drone.transform);
                d.scan.SetSimulator(!d.Placement.IsAR);
                d.ui.ShowOnly(d.Placement.HasSurface ? (UIView)d.place : d.scan);
                d.place.SetPlaced(d.Placement.IsPlaced);
                d.place.SetScale(d.scale);
                if (resume) d.tasks.SetPaused(true);
            }

            public void Tick()
            {
                d.scan.SetSurfaceCount(d.Placement.SurfaceCount);
                if (d.scan.IsVisible && d.Placement.HasSurface)
                {
                    AudioManager.Play("found");
                    d.ui.ShowOnly(d.place);
                }
                if (d.place.IsVisible) d.place.SetPlaced(d.Placement.IsPlaced);
            }

            public void Exit() { }
            public bool Resume => resume;
        }

        class InspectionState : IState
        {
            readonly InspectionDirector d;
            readonly bool resume;
            public InspectionState(InspectionDirector d, bool resume) { this.d = d; this.resume = resume; }

            public void Enter()
            {
                d.Drone.SetGhost(false);
                d.ui.ShowOnly(d.hud);
                if (!resume)
                {
                    d.hud.SetupChecklist(d.scenario);
                    d.tasks.Begin(d.scenario, d.Drone, GameSession.Mode, d.def.displayName);
                    d.inspectionBegun = true;
                }
                d.tasks.SetPaused(false);
                d.RefreshHud(newStep: true);
            }

            public void Tick() => d.hud.Tick(d.tasks);
            public void Exit() { }
        }

        class EvaluationState : IState
        {
            readonly InspectionDirector d;
            public EvaluationState(InspectionDirector d) { this.d = d; }
            public void Enter() => d.StartCoroutine(d.LaunchSequence());
            public void Tick() { }
            public void Exit() { }
        }

        // ================================================================== bindings
        void BindViews()
        {
            scan.CloseClicked += () => SceneNames.Load(SceneNames.MainMenu);
            place.Back += () => SceneNames.Load(SceneNames.MainMenu);
            place.RotateLeft += () => Placement.Rotate(-30);
            place.RotateRight += () => Placement.Rotate(30);
            place.ScaleToggled += () =>
            {
                scale = scale > 1.01f ? 1f : 2f;
                Placement.SetScale(scale);
                place.SetScale(scale);
            };
            place.Reposition += () => Placement.Reposition();
            place.Confirm += () =>
            {
                if (!Placement.IsPlaced) return;
                Placement.Confirm();
                AudioManager.Play("confirm");
                bool resume = fsm.Current is PlacingState p && p.Resume;
                fsm.Change(new InspectionState(this, resume || inspectionBegun));
            };

            hud.PauseClicked += OpenPause;
            hud.HelpClicked += () =>
            {
                var step = tasks.CurrentStep;
                hud.SetHint(step?.hint, !hud.HintVisible);
            };
            hud.LaunchClicked += () => { CloseAllPanels(); tasks.RequestLaunch(); };
            hud.CalibrateClicked += () =>
            {
                if (!CanStartCalibration(out var reason)) { hud.ShowFeedback(reason, FeedbackKind.Info); return; }
                CloseAllPanels();
                hud.Hide();
                calibration.Show();
            };

            rotorPanel.Close += CloseAllPanels;
            rotorPanel.Verdict += airworthy =>
            {
                if (!(openPart is RotorModule r)) return;
                var res = tasks.SubmitVerdict(r, airworthy);
                if (res == VerdictResult.CorrectFault) rotorPanel.SetReplaceMode(true);
                else if (res != VerdictResult.MissedFault) CloseAllPanels();
            };
            rotorPanel.Replace += () =>
            {
                if (openPart is RotorModule r) { tasks.ReplaceProp(r); AudioManager.Haptic(); }
                CloseAllPanels();
            };

            batteryPanel.Close += CloseAllPanels;
            batteryPanel.Lock += () =>
            {
                if (openPart is BatteryLatchModule l && !l.IsLocked)
                {
                    tasks.ToggleLatch(l);
                    AudioManager.Play("latch");
                    AudioManager.Haptic();
                    batteryPanel.Set(l);
                    StartCoroutine(CloseAfter(0.7f));
                }
            };
            batteryPanel.Swap += () =>
            {
                if (openPart is BatteryLatchModule l)
                {
                    tasks.SwapBattery(l);
                    AudioManager.Play("latch");
                    batteryPanel.Set(l);
                    StartCoroutine(CloseAfter(0.7f));
                }
            };
            batteryPanel.Release += () =>
            {
                if (openPart is BatteryLatchModule l && l.IsLocked)
                {
                    tasks.ToggleLatch(l);
                    AudioManager.Play("latch");
                    batteryPanel.Set(l);
                }
            };

            gimbalPanel.Close += CloseAllPanels;
            gimbalPanel.Swiped += () =>
            {
                if (openPart is GimbalCoverModule g) RemoveClamp(g);
                CloseAllPanels();
            };

            calibration.Completed += complete =>
            {
                tasks.CompleteCalibration(complete);
                calibration.Hide();
                hud.Show();
            };
            calibration.Cancelled += () => { calibration.Hide(); hud.Show(); };

            gate.GoToStep += i =>
            {
                gate.Hide();
                var step = scenario.steps[i];
                if (step.IsCalibration) { hud.Hide(); calibration.Show(); return; }
                var target = step.targets.Select(Drone.Find).FirstOrDefault(p => p && !p.IsSafe);
                if (target) hud.callout.Point(target.transform, Placement.ActiveCamera,
                                             target.DisplayName.ToUpperInvariant() + " · FIX THIS", UITheme.Current.danger);
            };

            ready.Back += () => { ready.Hide(); hud.Show(); };
            ready.Armed += () => fsm.Change(new EvaluationState(this));

            trackingLost.Replace += () =>
            {
                trackingLost.Hide();
                input.Enabled = true;
                Placement.Reposition();
                fsm.Change(new PlacingState(this, resume: true));
            };

            pause.Resume += ClosePause;
            pause.Restart += () => SceneNames.Load(SceneNames.ARInspection);
            pause.Quit += () => SceneNames.Load(SceneNames.MainMenu);

            report.Home += () => SceneNames.Load(SceneNames.MainMenu);
            report.Records += () => { GameSession.OpenRecordsOnMenu = true; SceneNames.Load(SceneNames.MainMenu); };
            report.Retry += () => SceneNames.Load(SceneNames.ARInspection);
        }

        void BindInput()
        {
            input.Tapped += OnTap;
            input.DragBegan += pos => dragPart = PickAt(pos);
            input.Dragging += (pos, delta) => { if (!dragPart) Placement.Orbit(delta); };
            input.DragEnded += (start, end) =>
            {
                var dir = end - start;
                if (dragPart is GimbalCoverModule g && dir.y > 90 && Mathf.Abs(dir.y) > Mathf.Abs(dir.x) * 0.8f
                    && fsm.Current is InspectionState && tasks.CanInteract(g, out _))
                {
                    RemoveClamp(g);
                    CloseAllPanels();
                }
                dragPart = null;
            };
            input.SecondaryDrag += delta => Placement.Pan(delta);
            input.Pinched += ratio => Placement.Zoom(ratio);
            input.Twisted += deg => { if (fsm.Current is PlacingState) Placement.Rotate(-deg); };
        }

        void BindTasks()
        {
            tasks.Changed += () => { if (fsm.Current is InspectionState) RefreshHud(newStep: false); };
            tasks.StepActivated += _ => { if (fsm.Current is InspectionState) RefreshHud(newStep: true); };
            tasks.Feedback += (msg, kind) =>
            {
                hud.ShowFeedback(msg, kind);
                AudioManager.Play(kind switch
                {
                    FeedbackKind.Success => "success",
                    FeedbackKind.Error => "error",
                    FeedbackKind.Warning => "warning",
                    _ => "tap"
                });
                if (kind == FeedbackKind.Error) AudioManager.Haptic();
            };
            tasks.LaunchBlocked += blocking =>
            {
                AudioManager.Play("blocked");
                AudioManager.Haptic();
                gate.Open(tasks.Scenario, blocking);
            };
            tasks.LaunchCleared += () =>
            {
                AudioManager.Play("confirm");
                hud.Hide();
                ready.Open(tasks.StepCount, TimeFormat.Clock(tasks.Elapsed));
            };
        }

        // ================================================================== interaction
        AInspectableComponent PickAt(Vector2 screen)
        {
            if (!Drone || Placement?.ActiveCamera == null) return null;
            return Drone.Pick(Placement.ActiveCamera.ScreenPointToRay(screen));
        }

        void OnTap(Vector2 pos)
        {
            if (fsm.Current is PlacingState)
            {
                if (Placement.HasSurface && Placement.TryPlaceAt(pos))
                {
                    AudioManager.Play("place");
                    if (!place.IsVisible) ui.ShowOnly(place);
                }
                return;
            }
            if (!(fsm.Current is InspectionState) || tasks.IsPaused) return;

            var part = PickAt(pos);
            if (!part)
            {
                if (tasks.Mode == TrainingMode.Assessment) tasks.Select(null);
                return;
            }
            if (!tasks.CanInteract(part, out var reason))
            {
                hud.ShowFeedback(reason ?? "Not now.", FeedbackKind.Info, 1.8f);
                AudioManager.Play("tap");
                return;
            }
            OpenPanel(part);
        }

        void OpenPanel(AInspectableComponent part)
        {
            CloseAllPanels();
            tasks.Select(part);
            openPart = part;
            AudioManager.Play("select");
            switch (part)
            {
                case RotorModule r:
                    if (r.IsSafe) { hud.ShowFeedback($"{r.DisplayName}: {r.StatusLabel.ToLowerInvariant()}.", FeedbackKind.Info, 1.6f); openPart = null; return; }
                    var step = tasks.StepOf(r);
                    int idx = System.Array.IndexOf(step.targets, r.PartId) + 1;
                    rotorPanel.Open(r.DisplayName.ToUpperInvariant(), idx, step.targets.Length, r.State == PartState.Faulty);
                    break;
                case BatteryLatchModule l:
                    batteryPanel.Show();
                    batteryPanel.Set(l);
                    break;
                case GimbalCoverModule g:
                    if (!g.IsFitted) { hud.ShowFeedback("Clamp already removed.", FeedbackKind.Info, 1.6f); openPart = null; return; }
                    gimbalPanel.Show();
                    break;
            }
            hud.callout.Point(part.transform, Placement.ActiveCamera, part.DisplayName.ToUpperInvariant(), UITheme.Current.accent);
            Placement.FocusOn(PartCenter(part));
        }

        static Vector3 PartCenter(AInspectableComponent p)
        {
            var r = p.GetComponent<Renderer>();
            return r ? r.bounds.center : p.transform.position;
        }

        void RemoveClamp(GimbalCoverModule g)
        {
            tasks.RemoveClamp(g);
            AudioManager.Play("swipe");
        }

        void CloseAllPanels()
        {
            rotorPanel.Hide();
            batteryPanel.Hide();
            gimbalPanel.Hide();
            if (openPart && Placement != null) Placement.FocusOn(null);
            openPart = null;
            if (tasks.Mode == TrainingMode.Assessment && tasks.IsRunning) tasks.Select(null);
            if (tasks.IsRunning) RefreshCallout();
        }

        IEnumerator CloseAfter(float s)
        {
            yield return new WaitForSecondsRealtime(s);
            CloseAllPanels();
        }

        bool CanStartCalibration(out string reason)
        {
            reason = null;
            int idx = System.Array.FindIndex(scenario.steps, s => s.IsCalibration);
            if (idx < 0 || tasks.CalibrationDone) { reason = "Controller already calibrated."; return false; }
            if (tasks.Mode == TrainingMode.Guided && tasks.CurrentStepIndex != idx)
            {
                reason = $"Finish \"{tasks.CurrentStep?.title}\" first.";
                return false;
            }
            return true;
        }

        void RefreshHud(bool newStep)
        {
            hud.Refresh(tasks);
            var step = tasks.CurrentStep;
            bool guided = tasks.Mode == TrainingMode.Guided;
            if (step != null)
            {
                if (guided) hud.SetInstruction(step.icon, step.instruction, step.helper);
                else hud.SetInstruction("target", "Inspect the aircraft", "Find and check every critical item, then run the launch check.");
                if (newStep) hud.SetHint(step.hint, guided && AppSettings.Hints);
            }
            else
            {
                hud.SetInstruction("check", "All checks complete", "Run the launch check when you're ready.");
                hud.SetHint(null, false);
            }
            bool calibAvailable = !tasks.CalibrationDone && (!guided || (step != null && step.IsCalibration));
            hud.SetCalibrateVisible(calibAvailable);
            RefreshCallout();
        }

        void RefreshCallout()
        {
            if (openPart || tasks.Mode == TrainingMode.Assessment) return;
            var step = tasks.CurrentStep;
            var target = step == null || step.IsCalibration ? null
                : step.targets.Select(Drone.Find).FirstOrDefault(p => p && !p.IsSafe);
            if (target)
            {
                string verb = target is GimbalCoverModule ? "SWIPE UP" : "TAP TO INSPECT";
                hud.callout.Point(target.transform, Placement.ActiveCamera,
                                  $"{target.DisplayName.ToUpperInvariant()} · {verb}", UITheme.Current.accent);
            }
            else hud.callout.Clear();
        }

        void OpenPause()
        {
            tasks.SetPaused(true);
            input.Enabled = false;
            pause.Open(tasks.Mode == TrainingMode.Assessment);
        }

        void ClosePause()
        {
            pause.Hide();
            input.Enabled = trackingOk;
            tasks.SetPaused(!trackingOk);
            if (AppSettings.Hints && tasks.Mode == TrainingMode.Guided) hud.SetHint(tasks.CurrentStep?.hint, true);
        }

        void OnTracking(bool ok)
        {
            hud.SetTracking(ok, Placement.IsAR);
            trackingOk = ok;
            if (!ok) { lostSince = Time.unscaledTime; return; }
            lostSince = -1;
            if (trackingLost.IsVisible)
            {
                trackingLost.Hide();
                input.Enabled = !pause.IsVisible;
                tasks.SetPaused(pause.IsVisible);
            }
        }

        // ================================================================== evaluation
        IEnumerator LaunchSequence()
        {
            ready.Hide();
            hud.Hide();
            hud.callout.Clear();
            Drone.SetHighlights(HighlightState.Off);
            var result = tasks.Finish();
            input.Enabled = false;

            AudioManager.PlayLoop("motor", 0.6f);
            Vector3 start = Drone.transform.position;
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.2f)
            {
                Drone.SpinProps(Mathf.Lerp(0, 45, t));
                AudioManager.SetLoopPitch(Mathf.Lerp(0.6f, 1.2f, t));
                yield return null;
            }
            float lift = 0.3f * scale;
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.5f)
            {
                Drone.transform.position = start + Vector3.up * lift * Mathf.SmoothStep(0, 1, t);
                yield return null;
            }
            for (float t = 0; t < 1.6f; t += Time.deltaTime)
            {
                Drone.transform.position = start + Vector3.up * (lift + Mathf.Sin(t * 4f) * 0.008f);
                yield return null;
            }
            AudioManager.StopLoop();
            AudioManager.Play(result.passed ? "success" : "warning");
            report.Open(result, bestBefore);
        }
    }
}
