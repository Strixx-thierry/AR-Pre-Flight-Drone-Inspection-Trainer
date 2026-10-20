using System;
using TMPro;
using UnityEngine;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S06 place &amp; adjust (FR-02).</summary>
    public class PlaceView : UIView
    {
        public UIButton backButton, rotateLeft, rotateRight, scaleButton, resetButton, confirmButton;
        public TextMeshProUGUI titleText, helperText, scaleLabel;
        public event Action Back, RotateLeft, RotateRight, ScaleToggled, Reposition, Confirm;

        protected override void Build()
        {
            var c = Content();
            backButton = RoundButton(c, "Back", "left", 104);
            backButton.RT().At(0, 1, 0, 1, 104, 104);

            var tools = Panel(c, "Tools", glass: true);
            tools.RT().At(0, 0.5f, 0, 0.5f, 150, 560);
            var v = VStack(tools.transform, "V", 16, TextAnchor.MiddleCenter, new RectOffset(16, 16, 20, 20));
            v.Fill();
            v.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childForceExpandWidth = false;
            rotateLeft = RoundButton(v, "RotateLeft", "rotate", 112);
            rotateLeft.icon.RT().localScale = new Vector3(-1, 1, 1);
            rotateRight = RoundButton(v, "RotateRight", "rotate", 112);
            scaleButton = RoundButton(v, "Scale", "scale", 112);
            resetButton = RoundButton(v, "Reset", "reset", 112);
            scaleLabel = Text(v, "ScaleLabel", "1:1", 22, T.textDim, TextStyle.Label, TextAlignmentOptions.Center);

            var card = Panel(c, "Instruction", glass: true, large: true);
            card.RT().At(0.5f, 0, 0.5f, 0, 880, 150);
            var h = HStack(card.transform, "Row", 24, TextAnchor.MiddleLeft, new RectOffset(30, 30, 18, 18));
            h.Fill();
            Icon(h, "Icon", "hand", 72, T.accent);
            var tv = VStack(h, "Text", 2, TextAnchor.MiddleLeft);
            tv.Size(-1, -1, 1);
            titleText = Text(tv, "Title", "TAP TO PLACE", 34, T.text, TextStyle.Display);
            helperText = Text(tv, "Helper", "Shown at real size (32 cm). Twist with two fingers to rotate.", 25, T.textDim);

            confirmButton = Button(c, "Confirm", "CONFIRM", ButtonStyle.Primary, "check");
            confirmButton.RT().At(1, 0, 1, 0, 360, TouchSize);
        }

        protected override void Bind()
        {
            backButton.Clicked += () => Back?.Invoke();
            rotateLeft.Clicked += () => RotateLeft?.Invoke();
            rotateRight.Clicked += () => RotateRight?.Invoke();
            scaleButton.Clicked += () => ScaleToggled?.Invoke();
            resetButton.Clicked += () => Reposition?.Invoke();
            confirmButton.Clicked += () => Confirm?.Invoke();
        }

        public void SetPlaced(bool placed)
        {
            confirmButton.SetInteractable(placed);
            titleText.text = placed ? "ADJUST, THEN CONFIRM" : "TAP TO PLACE";
            helperText.text = placed
                ? "Rotate it so you can walk around it. Tap again to move it."
                : "Shown at real size (32 cm). Twist with two fingers to rotate.";
        }

        public void SetScale(float s) => scaleLabel.text = s > 1.01f ? "×2" : "1:1";
    }
}
