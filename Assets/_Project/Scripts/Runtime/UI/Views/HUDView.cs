using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DroneTrainer.Core;
using DroneTrainer.Data;
using DroneTrainer.Tasks;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S07 master inspection HUD (FR-03, FR-07, FR-09).</summary>
    public class HUDView : UIView
    {
        public UIButton pauseButton, helpButton, launchButton, calibrateButton;
        public UIChip stepPill, timerPill, errorPill, trackingPill;
        public RectTransform checklistPanel, checklistRows;
        public TextMeshProUGUI checklistCount;
        public UnityEngine.UI.Image instructionCard, instructionIcon, instructionAccent;
        public TextMeshProUGUI instructionTitle, instructionHelper;
        public RectTransform hintCard;
        public TextMeshProUGUI hintText;
        public WorldCallout callout;

        public event Action PauseClicked, HelpClicked, LaunchClicked, CalibrateClicked;

        readonly List<ChecklistRow> rows = new List<ChecklistRow>();
        string baseIcon, baseTitle, baseHelper;
        float feedbackUntil;
        int lastErrors;

        protected override void Build()
        {
            var c = Content();

            var top = HStack(c, "TopBar", 18);
            top.Span(0, 1, 1, 1, 0, -112, 0, 0);
            pauseButton = RoundButton(top, "Pause", "pause", 104);
            stepPill = Pill(top, "Step", "STEP 1/4", null, T.accent);
            Spacer(top);
            timerPill = Pill(top, "Timer", "00:00", "timer", T.text);
            errorPill = Pill(top, "Errors", "0 errors", "warn", T.danger);
            trackingPill = Pill(top, "Tracking", "AR", "signal", T.ok);

            var panel = Panel(c, "Checklist", glass: true);
            checklistPanel = panel.RT().At(0, 1, 0, 1, 600, 420, 0, -136);
            var v = panel.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            v.padding = new RectOffset(16, 16, 16, 16);
            v.spacing = 4;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;
            var fit = panel.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fit.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            var head = HStack(panel.transform, "Header", 8);
            head.Size(-1, 44);
            Text(head, "Label", "CHECKLIST", 22, T.textDim, TextStyle.Label).Size(-1, -1, 1);
            checklistCount = Text(head, "Count", "0/4", 22, T.textDim, TextStyle.Label, TextAlignmentOptions.MidlineRight);
            checklistRows = VStack(panel.transform, "Rows", 4);

            instructionCard = Panel(c, "Instruction", glass: true, large: true);
            instructionCard.RT().At(0.5f, 0, 0.5f, 0, 920, 150);
            instructionAccent = AccentBar(instructionCard.transform, T.accent);
            var ih = HStack(instructionCard.transform, "Row", 24, TextAnchor.MiddleLeft, new RectOffset(40, 30, 18, 18));
            ih.Fill();
            instructionIcon = Icon(ih, "Icon", "prop", 72, T.accent);
            var iv = VStack(ih, "Text", 2, TextAnchor.MiddleLeft);
            iv.Size(-1, -1, 1);
            instructionTitle = Text(iv, "Title", "Inspect each propeller", 34, T.text, TextStyle.Display);
            instructionHelper = Text(iv, "Helper", "Tap the glowing rotor.", 25, T.textDim);

            var hint = Panel(c, "Hint", glass: true);
            hintCard = hint.RT().At(0, 0, 0, 0, 560, 200);
            AccentBar(hint.transform, T.warn);
            var hv = VStack(hint.transform, "V", 6, TextAnchor.UpperLeft, new RectOffset(40, 26, 20, 20));
            hv.Fill();
            var hh = HStack(hv, "Head", 10);
            Icon(hh, "Icon", "warn", 30, T.warn);
            Text(hh, "Label", "WHY IT MATTERS", 22, T.warn, TextStyle.Label);
            hintText = Text(hv, "Body", "", 24, T.textDim);
            hintText.textWrappingMode = TextWrappingModes.Normal;
            hintText.Size(-1, 110);

            var actions = VStack(c, "Actions", 18, TextAnchor.LowerRight);
            actions.At(1, 0, 1, 0, 460, 430);
            var helpRow = HStack(actions, "HelpRow", 0, TextAnchor.MiddleRight);
            helpButton = RoundButton(helpRow, "Help", "help", 104);
            calibrateButton = Button(actions, "Calibrate", "CALIBRATE RC", ButtonStyle.Primary, "rc");
            launchButton = Button(actions, "Launch", "LAUNCH CHECK", ButtonStyle.Primary, "play");

            callout = WorldCallout.Create(transform);
        }

        protected override void Bind()
        {
            pauseButton.Clicked += () => PauseClicked?.Invoke();
            helpButton.Clicked += () => HelpClicked?.Invoke();
            launchButton.Clicked += () => LaunchClicked?.Invoke();
            calibrateButton.Clicked += () => CalibrateClicked?.Invoke();
        }

        public void SetCalibrateVisible(bool v) => calibrateButton.gameObject.SetActive(v);

        public void SetupChecklist(ScenarioConfig s)
        {
            Clear(checklistRows);
            rows.Clear();
            for (int i = 0; i < s.steps.Length; i++) rows.Add(ChecklistRow.Create(checklistRows, i, s.steps[i]));
        }

        public void Refresh(TaskManager tm)
        {
            if (tm.Scenario == null) return;
            int n = tm.StepCount, done = 0;
            for (int i = 0; i < n && i < rows.Count; i++)
            {
                var (d, total) = tm.Progress(i);
                var st = tm.Status(i);
                if (st == StepStatus.Done) done++;
                rows[i].Set(st, d, total);
            }
            checklistCount.text = $"{done}/{n}";
            stepPill.SetText(tm.Mode == TrainingMode.Assessment
                ? "ASSESSMENT"
                : $"STEP <color=#{ColorUtility.ToHtmlStringRGB(T.accent)}>{Mathf.Min(tm.CurrentStepIndex + 1, n)}</color>/{n}");
            int errs = tm.Errors.Count;
            errorPill.SetText(errs == 1 ? "1 error" : $"{errs} errors");
            if (errs > lastErrors) StartCoroutine(Flash(errorPill));
            lastErrors = errs;
            bool canLaunch = tm.Mode == TrainingMode.Assessment || tm.AllDone;
            launchButton.SetInteractable(canLaunch);
        }

        public void Tick(TaskManager tm) => timerPill.SetText(TimeFormat.Clock(tm.Elapsed));

        public void SetTracking(bool ok, bool isAR)
        {
            trackingPill.SetText(isAR ? (ok ? "AR" : "LOST") : "3D");
            if (trackingPill.icon) trackingPill.icon.color = ok ? T.ok : T.warn;
        }

        public void SetInstruction(string icon, string title, string helper)
        {
            baseIcon = icon; baseTitle = title; baseHelper = helper;
            if (Time.unscaledTime >= feedbackUntil) ApplyInstruction(icon, title, helper, T.accent, T.line);
        }

        public void ShowFeedback(string message, FeedbackKind kind, float seconds = 2.6f)
        {
            Color c = kind switch
            {
                FeedbackKind.Success => T.ok,
                FeedbackKind.Warning => T.warn,
                FeedbackKind.Error => T.danger,
                _ => T.info
            };
            string icon = kind switch
            {
                FeedbackKind.Success => "check",
                FeedbackKind.Error => "warn",
                FeedbackKind.Warning => "warn",
                _ => "info"
            };
            string title = kind switch
            {
                FeedbackKind.Success => "Good",
                FeedbackKind.Error => "Safety error",
                FeedbackKind.Warning => "Action needed",
                _ => "Note"
            };
            feedbackUntil = Time.unscaledTime + seconds;
            ApplyInstruction(icon, title, message, c, c);
        }

        public void SetHint(string text, bool visible)
        {
            hintText.text = text;
            hintCard.gameObject.SetActive(visible && !string.IsNullOrEmpty(text));
        }

        public bool HintVisible => hintCard.gameObject.activeSelf;

        void ApplyInstruction(string icon, string title, string helper, Color iconColor, Color border)
        {
            instructionIcon.sprite = T.Icon(icon);
            instructionIcon.color = instructionIcon.sprite ? iconColor : Color.clear;
            instructionTitle.text = title;
            instructionTitle.color = iconColor == T.accent ? T.text : iconColor;
            instructionHelper.text = helper;
            if (instructionAccent) instructionAccent.color = border == T.line ? T.accent : border;
        }

        void Update()
        {
            if (feedbackUntil > 0 && Time.unscaledTime >= feedbackUntil)
            {
                feedbackUntil = 0;
                ApplyInstruction(baseIcon, baseTitle, baseHelper, T.accent, T.line);
            }
        }

        System.Collections.IEnumerator Flash(UIChip chip)
        {
            for (int i = 0; i < 3; i++)
            {
                chip.background.color = UITheme.A(T.danger, 0.6f);
                yield return new WaitForSecondsRealtime(0.12f);
                chip.background.color = T.glass;
                yield return new WaitForSecondsRealtime(0.12f);
            }
        }
    }
}
