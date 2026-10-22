using System;
using TMPro;
using UnityEngine;
using DroneTrainer.Drone;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S09 battery latch toggle (FR-05) with slide-to-lock for an easy touch target.</summary>
    public class BatteryPanelView : UIView
    {
        public UIChip stateChip;
        public TextMeshProUGUI chargeText, seatedText;
        public SlideConfirm slide;
        public UIButton releaseButton, closeButton, swapButton;
        public event Action Lock, Release, Close, Swap;

        protected override void Build()
        {
            var c = Content();
            var p = Panel(c, "Panel", glass: true, large: true);
            p.RT().At(1, 0, 1, 0, 680, 560);
            var v = VStack(p.transform, "V", 10, TextAnchor.UpperLeft, new RectOffset(32, 32, 26, 26));
            v.Fill();
            var head = HStack(v, "Head", 12);
            Text(head, "Label", "BATTERY STATUS", 22, T.textMute, TextStyle.Label).Size(-1, -1, 1);
            stateChip = Chip(head, "State", "UNLOCKED", ChipStyle.Warn, "unlock");
            closeButton = RoundButton(head, "Close", "x", 88);
            chargeText = KeyValue(v, "Charge", "75% · 3 of 4 LEDs", T.ok);
            seatedText = KeyValue(v, "Seated", "Flush", T.text);
            Spacer(v);
            slide = SlideConfirm.Create(v, "SlideLock", "SLIDE TO LOCK", "lock", T.warn);
            releaseButton = Button(v, "Release", "RELEASE LATCH", ButtonStyle.Ghost, "unlock");
            swapButton = Button(v, "Swap", "SWAP BATTERY", ButtonStyle.Danger, "battery");
        }

        protected override void Bind()
        {
            slide.Confirmed += () => Lock?.Invoke();
            releaseButton.Clicked += () => Release?.Invoke();
            closeButton.Clicked += () => Close?.Invoke();
            swapButton.Clicked += () => Swap?.Invoke();
        }

        public void Set(BatteryLatchModule latch)
        {
            bool locked = latch.IsLocked;
            stateChip.Set(locked ? "LOCKED" : "UNLOCKED", locked ? ChipStyle.Ok : ChipStyle.Warn);
            if (stateChip.icon) stateChip.icon.sprite = T.Icon(locked ? "lock" : "unlock");
            chargeText.text = $"{latch.Charge * 25}% · {latch.Charge} of 4 LEDs";
            chargeText.color = latch.Charge >= 2 ? T.ok : T.danger;
            slide.gameObject.SetActive(!locked);
            slide.ResetSlider();
            releaseButton.gameObject.SetActive(locked && !latch.LowCharge);
            swapButton.gameObject.SetActive(latch.LowCharge);
            if (latch.LowCharge) stateChip.Set("LOW CHARGE", ChipStyle.Danger);
        }
    }
}
