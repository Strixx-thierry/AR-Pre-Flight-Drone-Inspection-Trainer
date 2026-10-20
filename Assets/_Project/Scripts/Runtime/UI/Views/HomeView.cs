using System;
using TMPro;
using UnityEngine;
using DroneTrainer.Data;
using static DroneTrainer.UI.UIFactory;
using UImage = UnityEngine.UI.Image;

namespace DroneTrainer.UI
{
    /// <summary>
    /// S02 home hub — 2D only, matching the reference: round rail on the left, drone picture with
    /// square arrows, textured bottom bar with nav tiles, green status tag and a simple START button.
    /// All art comes from Assets/_Project/Art/UI.
    /// </summary>
    public class HomeView : UIView
    {
        public UImage heroImage, recordsBadge;
        public TextMeshProUGUI droneName, statusTop, statusBottom;
        public UIButton settingsButton, helpButton, recordsRailButton, prevButton, nextButton;
        public UIButton homeTile, hangarTile, trainingTile, recordsTile, quickStart;
        public event Action Settings, Help, Prev, Next, Hangar, Training, Records, QuickStart;

        protected override void Build()
        {
            // full-width bottom strip (outside the gutters, like the reference)
            var bar = Img(transform, "BottomBar", Color.white, T.bottomBar);
            bar.RT().Span(0, 0, 1, 0, 0, 0, 0, -240);
            if (T.bottomBar) bar.type = UImage.Type.Sliced;
            else bar.color = T.panel;

            var c = Content();

            heroImage = Img(c, "Hero", Color.white, T.heroDrone);
            heroImage.preserveAspect = true;
            heroImage.RT().At(0.5f, 0.58f, 0.5f, 0.5f, 1250, 760);
            if (!T.heroDrone) heroImage.color = Color.clear;

            droneName = Text(c, "DroneName", "SKYFOLD F4", 34, T.textDim, TextStyle.Display, TextAlignmentOptions.Center);
            droneName.characterSpacing = 14;
            droneName.RT().At(0.5f, 0, 0.5f, 0.5f, 700, 50, 0, 290);

            // left rail
            var rail = VStack(c, "Rail", 26, TextAnchor.MiddleCenter);
            rail.At(0, 0.6f, 0, 0.5f, 140, 470);
            settingsButton = Rail(rail, "Settings", "gear");
            helpButton = Rail(rail, "Help", "help");
            recordsRailButton = Rail(rail, "Records", "clip");
            recordsBadge = Img(recordsRailButton.transform, "Badge", Color.white, T.badgeAlert);
            recordsBadge.RT().At(1, 1, 0.5f, 0.5f, 48, 48, -16, -16);
            recordsBadge.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;

            // square arrows either side of the drone
            prevButton = Arrow(c, "Prev", "left", 0.2f);
            nextButton = Arrow(c, "Next", "right", 0.8f);

            // nav tiles on the bar
            var nav = HStack(c, "Nav", 14, TextAnchor.LowerLeft);
            nav.At(0, 0, 0, 0, 880, 250, 0, -GutterY + 10);
            homeTile = Tile(nav, "HOME", "home", active: true);
            hangarTile = Tile(nav, "HANGAR", "hangar", active: false);
            trainingTile = Tile(nav, "TRAINING", "train", active: false);
            recordsTile = Tile(nav, "RECORDS", "records", active: false);

            // green status tag
            var tag = Img(c, "StatusTag", Color.white, T.statusTag);
            tag.preserveAspect = true;
            tag.RT().At(1, 0, 1, 0, 400, 120, -470, 30);
            if (!T.statusTag) tag.color = Color.clear;
            var tv = VStack(tag.transform, "Text", -6, TextAnchor.MiddleCenter);
            tv.Fill(40, 10, 40, 10);
            statusTop = Text(tv, "Top", "MODULE 1", 30, T.text, TextStyle.Display, TextAlignmentOptions.Center);
            statusBottom = Text(tv, "Bottom", "PRE-FLIGHT", 30, T.ok, TextStyle.Display, TextAlignmentOptions.Center);

            // simple START button
            quickStart = Button(c, "Start", "START", ButtonStyle.Ghost, null, 190);
            quickStart.RT().At(1, 0, 1, 0, 430, 190, 0, 10);
            quickStart.background.sprite = T.startButton ? T.startButton : T.roundedLarge;
            quickStart.background.type = UImage.Type.Sliced;
            quickStart.background.color = Color.white;
            quickStart.style = ButtonStyle.Round;
            quickStart.label.fontSize = 64;
            quickStart.label.color = T.text;
            quickStart.label.characterSpacing = 10;
        }

        UIButton Rail(Transform parent, string name, string icon)
        {
            var b = RoundButton(parent, name, icon, 128);
            b.background.sprite = T.railButton ? T.railButton : T.circle;
            b.background.type = UImage.Type.Simple;
            b.background.color = Color.white;
            b.style = ButtonStyle.Round;
            if (b.icon) { b.icon.color = UITheme.Hex("C9CFD5"); b.icon.Size(56, 56); }
            return b;
        }

        UIButton Arrow(Transform parent, string name, string icon, float x)
        {
            var b = RoundButton(parent, name, icon, 96);
            b.RT().At(x, 0.58f, 0.5f, 0.5f, 96, 110);
            b.background.sprite = T.arrowButton ? T.arrowButton : T.rounded;
            b.background.type = UImage.Type.Sliced;
            b.background.color = Color.white;
            b.style = ButtonStyle.Round;
            if (b.icon) b.icon.color = UITheme.Hex("C9CFD5");
            return b;
        }

        UIButton Tile(Transform parent, string label, string icon, bool active)
        {
            var b = Button(parent, "Tile_" + label, null, ButtonStyle.Ghost, icon, 228);
            b.Size(200, 228);
            var art = active ? T.navTileActive : T.navTile;
            b.background.sprite = art ? art : T.roundedLarge;
            b.background.type = UImage.Type.Sliced;
            b.background.color = art ? Color.white : (active ? UITheme.Hex("EEF1F4") : T.panel);
            b.style = ButtonStyle.Round;
            var row = b.transform.Find("Row");
            DestroyImmediate(row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>());
            var v = row.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.MiddleCenter;
            v.spacing = 8;
            v.padding = new RectOffset(0, 0, 0, 22);
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = v.childForceExpandHeight = false;
            var fg = active ? UITheme.Hex("1A1F26") : UITheme.Hex("8A929C");
            b.icon.Size(88, 88);
            b.icon.color = fg;
            b.label = Text(row, "Label", label, 30, fg, TextStyle.Display, TextAlignmentOptions.Center);
            b.label.characterSpacing = 2;
            return b;
        }

        protected override void Bind()
        {
            settingsButton.Clicked += () => Settings?.Invoke();
            helpButton.Clicked += () => Help?.Invoke();
            recordsRailButton.Clicked += () => Records?.Invoke();
            prevButton.Clicked += () => Prev?.Invoke();
            nextButton.Clicked += () => Next?.Invoke();
            hangarTile.Clicked += () => Hangar?.Invoke();
            trainingTile.Clicked += () => Training?.Invoke();
            recordsTile.Clicked += () => Records?.Invoke();
            quickStart.Clicked += () => QuickStart?.Invoke();
        }

        public void Set(DroneDefinition d, int sessions, float best, int lastScore, bool arReady)
        {
            if (d) droneName.text = d.displayName;
            statusTop.text = "MODULE 1";
            statusBottom.text = lastScore >= 0 ? $"SCORE {lastScore}" : "PRE-FLIGHT";
            statusBottom.color = lastScore >= 70 || lastScore < 0 ? T.ok : T.warn;
            recordsBadge.gameObject.SetActive(sessions == 0);   // nudge until the first run is logged
        }
    }
}
