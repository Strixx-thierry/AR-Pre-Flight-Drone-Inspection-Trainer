using System;
using TMPro;
using UnityEngine;
using DroneTrainer.Audio;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S11 virtual RC calibration: centre → full circles (8/8 each stick) → centre.</summary>
    public class CalibrationView : UIView
    {
        public VirtualStick left, right;
        public TextMeshProUGUI phaseLabel, phaseTitle, leftCount, rightCount, timerText;
        public UIChip chip1, chip2, chip3;
        public UIButton backButton, startButton, finishButton;
        public event Action<bool> Completed;
        public event Action Cancelled;

        int phase;     // 0 ready, 1 rotate, 2 centre, 3 done
        float releasedFor;

        protected override void Build()
        {
            var bg = Img(transform, "Background", T.bg1, null, true);
            bg.RT().Fill();
            var c = Content();

            var top = HStack(c, "Top", 18);
            top.Span(0, 1, 1, 1, 0, -112, 0, 0);
            backButton = RoundButton(top, "Back", "left", 104);
            Text(top, "Title", "CONTROLLER CALIBRATION", 40, T.text, TextStyle.Display).Size(-1, -1, 1);
            timerText = Text(top, "Timer", "", 30, T.textDim, TextStyle.Mono, TextAlignmentOptions.MidlineRight);

            var body = Img(c, "Controller", UITheme.Hex("1B2027"), T.roundedLarge);
            body.RT().Span(0.08f, 0.1f, 0.92f, 0.84f);
            var screen = Img(body.transform, "Screen", UITheme.Hex("0B0E12"), T.rounded);
            screen.RT().At(0.5f, 1, 0.5f, 1, 560, 190, 0, -40);
            var sv = VStack(screen.transform, "V", 4, TextAnchor.MiddleCenter);
            sv.Fill(10, 10, 10, 10);
            phaseLabel = Text(sv, "Phase", "PHASE 1 / 3", 22, T.textMute, TextStyle.Label, TextAlignmentOptions.Center);
            phaseTitle = Text(sv, "PhaseTitle", "CENTRE STICKS", 36, T.text, TextStyle.Display, TextAlignmentOptions.Center);

            left = VirtualStick.Create(body.transform, "LeftStick", 380);
            left.RT().At(0, 0.5f, 0, 0.5f, 380, 380, 90, -20);
            right = VirtualStick.Create(body.transform, "RightStick", 380);
            right.RT().At(1, 0.5f, 1, 0.5f, 380, 380, -90, -20);
            leftCount = Text(body.transform, "LeftCount", "L · 0/8", 24, T.textDim, TextStyle.Mono, TextAlignmentOptions.Center);
            leftCount.RT().At(0, 0, 0, 0, 380, 50, 90, 24);
            rightCount = Text(body.transform, "RightCount", "R · 0/8", 24, T.textDim, TextStyle.Mono, TextAlignmentOptions.Center);
            rightCount.RT().At(1, 0, 1, 0, 380, 50, -90, 24);

            var chips = HStack(body.transform, "Chips", 14, TextAnchor.MiddleCenter);
            chips.At(0.5f, 0.5f, 0.5f, 0.5f, 700, 70, 0, -40);
            chip1 = Chip(chips, "C1", "1 · CENTRE", ChipStyle.Accent);
            chip2 = Chip(chips, "C2", "2 · CIRCLES", ChipStyle.Neutral);
            chip3 = Chip(chips, "C3", "3 · CENTRE", ChipStyle.Neutral);

            startButton = Button(body.transform, "Start", "START", ButtonStyle.Primary, "play");
            startButton.RT().At(0.5f, 0, 0.5f, 0, 360, TouchSize, 0, 70);
            finishButton = Button(c, "Finish", "FINISH", ButtonStyle.Ghost, "check");
            finishButton.RT().At(1, 0, 1, 0, 300, 110, 0, -20);
        }

        protected override void Bind()
        {
            backButton.Clicked += () => Cancelled?.Invoke();
            startButton.Clicked += () => SetPhase(1);
            finishButton.Clicked += () => Finish(phase >= 3);
            left.TickLit += () => AudioManager.Play("tick");
            right.TickLit += () => AudioManager.Play("tick");
        }

        public override void Show()
        {
            base.Show();
            left.ResetTicks();
            right.ResetTicks();
            SetPhase(0);
        }

        void SetPhase(int p)
        {
            phase = p;
            releasedFor = 0;
            phaseLabel.text = $"PHASE {Mathf.Min(p + 1, 3)} / 3";
            phaseTitle.text = p switch { 0 => "CENTRE STICKS", 1 => "ROTATE BOTH STICKS", 2 => "RELEASE TO CENTRE", _ => "CALIBRATED" };
            chip1.Set("1 · CENTRE", p == 0 ? ChipStyle.Accent : ChipStyle.Ok);
            chip2.Set("2 · CIRCLES", p == 1 ? ChipStyle.Accent : p > 1 ? ChipStyle.Ok : ChipStyle.Neutral);
            chip3.Set("3 · CENTRE", p == 2 ? ChipStyle.Accent : p > 2 ? ChipStyle.Ok : ChipStyle.Neutral);
            startButton.gameObject.SetActive(p == 0);
            if (p == 3)
            {
                AudioManager.Play("success");
                Finish(true);
            }
        }

        void Finish(bool complete)
        {
            if (!complete) AudioManager.Play("warning");
            Completed?.Invoke(complete);
        }

        void Update()
        {
            leftCount.text = $"L · {left.TicksLit}/8";
            rightCount.text = $"R · {right.TicksLit}/8";
            if (phase == 1 && left.TicksLit >= 8 && right.TicksLit >= 8) SetPhase(2);
            else if (phase == 2)
            {
                releasedFor = (!left.Held && !right.Held) ? releasedFor + Time.unscaledDeltaTime : 0;
                if (releasedFor > 0.6f) SetPhase(3);
            }
        }
    }
}
