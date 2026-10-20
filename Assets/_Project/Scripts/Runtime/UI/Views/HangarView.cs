using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DroneTrainer.Data;
using static DroneTrainer.UI.UIFactory;

namespace DroneTrainer.UI
{
    /// <summary>S03 aircraft select. Cards and specs come from the DroneCatalog asset (data-driven).</summary>
    public class HangarView : UIView
    {
        public UIButton backButton, selectButton;
        public TextMeshProUGUI nameText, subText;
        public RectTransform specRows, difficultyRow, cards;
        public UIChip difficultyChip;
        public event Action Back;
        public event Action<DroneDefinition> Focused, Selected;

        DroneDefinition focused;
        readonly List<(DroneDefinition def, UIButton btn)> cardButtons = new List<(DroneDefinition, UIButton)>();

        protected override void Build()
        {
            var c = Content();
            var top = HStack(c, "Top", 18);
            top.Span(0, 1, 1, 1, 0, -112, 0, 0);
            backButton = RoundButton(top, "Back", "left", 104);
            Text(top, "Title", "SELECT AIRCRAFT", 48, T.text, TextStyle.Display).Size(-1, -1, 1);
            difficultyChip = Chip(top, "Difficulty", "BEGINNER", ChipStyle.Ok);

            var spec = Panel(c, "Spec", large: true);
            spec.RT().Span(0.66f, 0.0f, 1, 1, 0, 0, 0, 136);
            var v = VStack(spec.transform, "V", 4, TextAnchor.UpperLeft, new RectOffset(36, 36, 30, 30));
            v.Fill();
            nameText = Text(v, "Name", "SKYFOLD F4", 54, T.text, TextStyle.Display);
            nameText.Size(-1, 70);
            subText = Text(v, "Sub", "Foldable camera quadcopter", 26, T.textDim);
            subText.Size(-1, 50);
            specRows = VStack(v, "Specs", 0);
            difficultyRow = HStack(v, "DifficultyBars", 8);
            difficultyRow.Size(-1, 60);
            Spacer(v);
            selectButton = Button(v, "Select", "SELECT", ButtonStyle.Primary, "check");

            cards = HStack(c, "Cards", 18, TextAnchor.LowerLeft);
            cards.At(0, 0, 0, 0, 1200, 250);
        }

        protected override void Bind()
        {
            backButton.Clicked += () => Back?.Invoke();
            selectButton.Clicked += () => { if (focused && !focused.locked) Selected?.Invoke(focused); };
        }

        public void Open(DroneCatalog catalog, string selectedId)
        {
            Show();
            Clear(cards);
            cardButtons.Clear();
            DroneDefinition first = null;
            foreach (var d in catalog.drones)
            {
                if (!d) continue;
                var def = d;
                var b = Button(cards, "Card_" + d.id, null, ButtonStyle.Ghost, d.locked ? "lock" : "drone", 240);
                b.Size(270, 240);
                b.background.sprite = T.roundedLarge;
                var row = b.transform.Find("Row");
                DestroyImmediate(row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>());
                var vl = row.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                vl.childAlignment = TextAnchor.MiddleCenter;
                vl.spacing = 6;
                vl.childControlWidth = vl.childControlHeight = true;
                vl.childForceExpandWidth = vl.childForceExpandHeight = false;
                b.icon.Size(90, 90);
                b.icon.color = d.locked ? T.textMute : T.text;
                Text(row, "Name", d.displayName, 26, d.locked ? T.textMute : T.text, TextStyle.Display, TextAlignmentOptions.Center);
                Text(row, "Note", d.locked ? d.lockedNote : d.subtitle, 20, d.locked ? T.textMute : T.textDim,
                     TextStyle.Body, TextAlignmentOptions.Center).textWrappingMode = TextWrappingModes.NoWrap;
                b.Clicked += () => Focus(def);
                cardButtons.Add((d, b));
                if (d.id == selectedId) first = d;
            }
            Focus(first ? first : catalog.FirstUnlocked());
        }

        void Focus(DroneDefinition d)
        {
            if (!d) return;
            focused = d;
            foreach (var (def, btn) in cardButtons)
            {
                var o = btn.GetComponent<UnityEngine.UI.Outline>();
                if (!o) o = btn.gameObject.AddComponent<UnityEngine.UI.Outline>();
                o.effectDistance = new Vector2(4, -4);
                o.effectColor = def == d ? T.accent : Color.clear;
                btn.background.color = def == d ? Color.Lerp(T.panel, T.accent, 0.16f) : T.panel;
            }
            nameText.text = d.displayName;
            subText.text = d.locked ? d.lockedNote : d.subtitle;
            difficultyChip.Set(d.difficultyLabel, d.difficulty <= 1 ? ChipStyle.Ok : d.difficulty == 2 ? ChipStyle.Warn : ChipStyle.Danger);
            Clear(specRows);
            foreach (var s in d.specs) KeyValue(specRows, s.label, s.value);
            Clear(difficultyRow);
            Text(difficultyRow, "Label", "Difficulty", 24, T.textDim).Size(-1, -1, 1);
            for (int i = 0; i < 3; i++)
                Img(difficultyRow, "Bar" + i, i < d.difficulty ? T.ok : T.line, T.pill).Size(56, 14);
            selectButton.SetInteractable(!d.locked && d.prefab);
            selectButton.SetLabel(d.locked ? "COMING SOON" : "SELECT");
            Focused?.Invoke(d);
        }
    }
}
