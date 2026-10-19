using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DroneTrainer.Core;
using DroneTrainer.Data;
using DroneTrainer.Drone;

namespace DroneTrainer.Tasks
{
    public enum StepStatus { Locked, Open, Active, Done }
    public enum FeedbackKind { Info, Success, Warning, Error }

    /// <summary>
    /// Coordinates the inspection (SRS: TaskManager): step sequence from JSON, validation rules,
    /// error tracking, timer (FR-06, FR-09) and the launch gate (FR-08).
    /// Observes every part's StateChanged event instead of being called by the parts.
    /// </summary>
    public class TaskManager : MonoBehaviour
    {
        public ScenarioConfig Scenario { get; private set; }
        public DroneController Drone { get; private set; }
        public TrainingMode Mode { get; private set; }
        public string DroneName { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public float Elapsed { get; private set; }
        public int CurrentStepIndex { get; private set; }
        public bool CalibrationDone { get; private set; }
        public bool CalibrationComplete { get; private set; }
        public AInspectableComponent Selected { get; private set; }
        public readonly List<ErrorEntry> Errors = new List<ErrorEntry>();

        public int StepCount => Scenario?.steps.Length ?? 0;
        public StepConfig CurrentStep => Scenario != null && CurrentStepIndex < StepCount ? Scenario.steps[CurrentStepIndex] : null;
        public bool AllDone => Scenario != null && Enumerable.Range(0, StepCount).All(IsStepDone);
        public int SafetyErrors => Errors.Count(e => e.severity == ErrorSeverity.Safety);
        public int MinorErrors => Errors.Count(e => e.severity == ErrorSeverity.Minor);

        public event Action Changed;
        public event Action<string, FeedbackKind> Feedback;
        public event Action<ErrorEntry> ErrorLogged;
        public event Action<List<StepConfig>> LaunchBlocked;
        public event Action LaunchCleared;
        public event Action<int> StepActivated;

        float[] stepStart, stepEnd;
        int[] stepErrors;

        public void Begin(ScenarioConfig scenario, DroneController drone, TrainingMode mode, string droneName)
        {
            if (Drone) foreach (var p in Drone.Parts) p.StateChanged -= OnPartChanged;
            Scenario = scenario;
            Drone = drone;
            Mode = mode;
            DroneName = droneName;
            Errors.Clear();
            Elapsed = 0;
            CalibrationDone = CalibrationComplete = false;
            Selected = null;
            stepStart = Enumerable.Repeat(-1f, StepCount).ToArray();
            stepEnd = Enumerable.Repeat(-1f, StepCount).ToArray();
            stepErrors = new int[StepCount];

            drone.ApplyScenario(scenario.faults);
            foreach (var p in drone.Parts) p.StateChanged += OnPartChanged;
            CurrentStepIndex = -1;
            IsRunning = true;
            IsPaused = false;
            Evaluate();
        }

        void OnDestroy()
        {
            if (Drone) foreach (var p in Drone.Parts) p.StateChanged -= OnPartChanged;
        }

        void Update()
        {
            if (IsRunning && !IsPaused) Elapsed += Time.unscaledDeltaTime;
        }

        public void SetPaused(bool paused) => IsPaused = paused;

        // ------------------------------------------------------------------ queries
        public bool IsStepDone(int i)
        {
            var s = Scenario.steps[i];
            if (s.IsCalibration) return CalibrationDone;
            foreach (var t in s.targets)
            {
                var p = Drone.Find(t);
                if (p && !p.IsSafe) return false;
            }
            return true;
        }

        public (int done, int total) Progress(int i)
        {
            var s = Scenario.steps[i];
            if (s.IsCalibration) return (CalibrationDone ? 1 : 0, 1);
            int total = 0, done = 0;
            foreach (var t in s.targets)
            {
                var p = Drone.Find(t);
                if (!p) continue;
                total++;
                if (p.IsSafe) done++;
            }
            return (done, total);
        }

        public StepStatus Status(int i)
        {
            if (IsStepDone(i)) return StepStatus.Done;
            if (Mode == TrainingMode.Assessment) return StepStatus.Open;
            return i == CurrentStepIndex ? StepStatus.Active : StepStatus.Locked;
        }

        public int StepIndexOf(AInspectableComponent p) => p ? Scenario.IndexOfPart(p.PartId) : -1;
        public StepConfig StepOf(AInspectableComponent p)
        {
            int i = StepIndexOf(p);
            return i >= 0 ? Scenario.steps[i] : null;
        }

        public bool CanInteract(AInspectableComponent p, out string reason)
        {
            reason = null;
            if (!IsRunning || IsPaused || p == null) return false;
            int idx = StepIndexOf(p);
            if (idx < 0) { reason = "This part isn't on today's checklist."; return false; }
            if (Mode == TrainingMode.Guided && idx != CurrentStepIndex && !IsStepDone(idx))
            {
                reason = $"Finish \"{CurrentStep?.title}\" first.";
                return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ actions
        public void Select(AInspectableComponent p)
        {
            Selected = p;
            UpdateHighlights();
        }

        public VerdictResult SubmitVerdict(RotorModule rotor, bool airworthy)
        {
            var result = rotor.SubmitVerdict(airworthy);
            switch (result)
            {
                case VerdictResult.CorrectPass:
                    Say($"{rotor.DisplayName}: airworthy.", FeedbackKind.Success); break;
                case VerdictResult.CorrectFault:
                    Say("Damage confirmed. Replace the propeller before flight.", FeedbackKind.Warning); break;
                case VerdictResult.MissedFault:
                    LogError(rotor, $"Passed a damaged propeller ({rotor.DisplayName}).", ErrorSeverity.Safety);
                    Say("That blade is damaged. Look again at the leading edge.", FeedbackKind.Error); break;
                case VerdictResult.FalseAlarm:
                    LogError(rotor, $"False alarm on {rotor.DisplayName}: the blade was fine.", ErrorSeverity.Minor);
                    Say("No damage on this blade. It's airworthy.", FeedbackKind.Info); break;
            }
            return result;
        }

        public void ReplaceProp(RotorModule rotor)
        {
            rotor.Replace();
            if (rotor.highlighter) rotor.highlighter.SyncMeshes();
            Say("New propeller fitted and secured.", FeedbackKind.Success);
        }

        public void ToggleLatch(BatteryLatchModule latch)
        {
            latch.Toggle();
            Say(latch.IsLocked ? "Battery latch locked." : "Battery latch released.",
                latch.IsLocked ? FeedbackKind.Success : FeedbackKind.Warning);
        }

        public void RemoveClamp(GimbalCoverModule clamp)
        {
            if (!clamp.IsFitted) return;
            clamp.Remove();
            Say("Gimbal clamp removed.", FeedbackKind.Success);
        }

        public void CompleteCalibration(bool allPositionsReached)
        {
            CalibrationDone = true;
            CalibrationComplete = allPositionsReached;
            int idx = Array.FindIndex(Scenario.steps, s => s.IsCalibration);
            if (!allPositionsReached)
                LogError(idx >= 0 ? Scenario.steps[idx].id : "calibration", "",
                         "Calibration finished before every stick position was reached.", ErrorSeverity.Minor);
            Say(allPositionsReached ? "Controller calibrated." : "Calibration incomplete. Logged.",
                allPositionsReached ? FeedbackKind.Success : FeedbackKind.Warning);
            Evaluate();
        }

        /// <summary>FR-08: launch is refused while any critical item is unsafe.</summary>
        public bool RequestLaunch()
        {
            var blocking = Enumerable.Range(0, StepCount)
                .Where(i => Scenario.steps[i].critical && !IsStepDone(i))
                .Select(i => Scenario.steps[i]).ToList();
            if (blocking.Count > 0)
            {
                LogError(blocking[0].id, "", "Attempted launch with " +
                         string.Join(", ", blocking.Select(b => b.title.ToLowerInvariant())) + " not secured.",
                         ErrorSeverity.Safety);
                LaunchBlocked?.Invoke(blocking);
                Changed?.Invoke();
                return false;
            }
            LaunchCleared?.Invoke();
            return true;
        }

        public SessionResult Finish()
        {
            IsRunning = false;
            var r = new SessionResult
            {
                droneId = Drone ? Drone.droneId : GameSession.DroneId,
                droneName = DroneName,
                mode = Mode.ToString(),
                seconds = Elapsed,
                safetyErrors = SafetyErrors,
                minorErrors = MinorErrors,
                utcTicks = DateTime.UtcNow.Ticks,
                errors = new List<ErrorEntry>(Errors)
            };
            for (int i = 0; i < StepCount; i++)
            {
                var s = Scenario.steps[i];
                float start = stepStart[i] < 0 ? 0 : stepStart[i];
                float end = stepEnd[i] < 0 ? Elapsed : stepEnd[i];
                string note = null;
                if (!s.IsCalibration)
                {
                    int replaced = s.targets.Select(Drone.Find).OfType<AInspectableComponent>().Count(p => p.State == PartState.Fixed);
                    if (replaced > 0) note = $"{replaced} replaced";
                }
                else if (!CalibrationComplete) note = "incomplete";
                r.steps.Add(new StepResult { id = s.id, title = s.title, seconds = Mathf.Max(0, end - start),
                                             errors = stepErrors[i], completed = IsStepDone(i), note = note });
            }
            r.score = ScoreCalculator.Compute(r, Scenario.targetSeconds);
            r.passed = r.steps.All(s => s.completed) && r.score >= ScoreCalculator.PassMark;
            ProgressStore.Add(r);
            GameSession.LastResult = r;
            return r;
        }

        // ------------------------------------------------------------------ internals
        void OnPartChanged(AInspectableComponent p) => Evaluate();

        void Evaluate()
        {
            if (Scenario == null) return;
            int next = 0;
            while (next < StepCount && IsStepDone(next)) next++;

            if (next != CurrentStepIndex)
            {
                for (int i = 0; i < StepCount; i++)
                    if (IsStepDone(i) && stepEnd[i] < 0) stepEnd[i] = Elapsed;
                CurrentStepIndex = next;
                if (next < StepCount)
                {
                    if (stepStart[next] < 0) stepStart[next] = Elapsed;
                    StepActivated?.Invoke(next);
                }
            }
            UpdateHighlights();
            Changed?.Invoke();
        }

        void UpdateHighlights()
        {
            if (!Drone) return;
            foreach (var p in Drone.Parts)
            {
                if (!p.highlighter) continue;
                int idx = StepIndexOf(p);
                HighlightState h;
                if (Mode == TrainingMode.Assessment)
                    h = p == Selected ? HighlightState.Active : HighlightState.Off;
                else if (idx == CurrentStepIndex)
                    h = p.IsSafe ? HighlightState.Done
                      : p.State == PartState.Faulty ? HighlightState.Fault
                      : p.Kind == PartKind.Toggle ? HighlightState.Warn
                      : HighlightState.Active;
                else if (idx > CurrentStepIndex) h = HighlightState.Idle;
                else h = HighlightState.Off;
                if (p is GimbalCoverModule g && !g.IsFitted) h = HighlightState.Off;
                p.highlighter.Set(h);
            }
        }

        void LogError(AInspectableComponent p, string message, ErrorSeverity sev) =>
            LogError(StepOf(p)?.id ?? "", p ? p.PartId : "", message, sev);

        void LogError(string stepId, string partId, string message, ErrorSeverity sev)
        {
            var e = new ErrorEntry { time = Elapsed, stepId = stepId, partId = partId, message = message, severity = sev };
            Errors.Add(e);
            int idx = Array.FindIndex(Scenario.steps, s => s.id == stepId);
            if (idx >= 0) stepErrors[idx]++;
            ErrorLogged?.Invoke(e);
        }

        void Say(string msg, FeedbackKind kind) => Feedback?.Invoke(msg, kind);
    }

    public static class ScoreCalculator
    {
        public const int PassMark = 70;

        /// <summary>100 − 10 per safety error − 2 per minor error − 1 per 30 s over target.</summary>
        public static int Compute(SessionResult r, float targetSeconds)
        {
            float over = Mathf.Max(0, r.seconds - targetSeconds);
            int score = 100 - 10 * r.safetyErrors - 2 * r.minorErrors - Mathf.FloorToInt(over / 30f);
            if (r.steps.Any(s => !s.completed)) score -= 25;
            return Mathf.Clamp(score, 0, 100);
        }
    }
}
