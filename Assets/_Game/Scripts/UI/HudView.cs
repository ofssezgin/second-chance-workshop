using System;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;

namespace CopAtolyesi.UI
{
    public sealed class HudView : MonoBehaviour
    {
        const float GearSize = 40f;
        const float GearMargin = 8f;

        GameSession session;
        TMP_Text dayLabel;
        TMP_Text waterLabel;
        TMP_Text starsLabel;
        TMP_Text doorLabel;
        UnityEngine.UI.Image endDayImage;
        RectTransform notebookRect;
        RectTransform doorRect;
        RectTransform settingsRect;

        public static HudView Create(Transform parent, UiContext context, Action onDoor, Action onEndDay, Action onNotebook, Action onSettings)
        {
            var session = context.Session;
            var config = GameData.Config;
            var palette = config.palette;
            var bar = UiKit.Panel(parent, "Hud", new Vector2(0, 1), Vector2.one, GameData.ParseColor(palette.anthracite));
            bar.rectTransform.offsetMin = new Vector2(0, -config.hudHeight);
            bar.rectTransform.offsetMax = Vector2.zero;

            var view = bar.gameObject.AddComponent<HudView>();
            view.session = session;

            var cream = GameData.ParseColor(palette.cream);
            view.dayLabel = UiKit.Label(bar.transform, "Day", string.Empty, 22, cream, TextAlignmentOptions.MidlineLeft);
            UiKit.Pad(view.dayLabel.rectTransform, 16, 0, 0, 0);
            view.waterLabel = UiKit.Label(bar.transform, "Water", string.Empty, 22, cream);
            UiKit.Pad(view.waterLabel.rectTransform, 0, 0, 320, 0);

            var waterHover = UiKit.Panel(bar.transform, "WaterHover", new Vector2(0.5f, 0), new Vector2(0.5f, 1), Color.clear);
            waterHover.rectTransform.offsetMin = new Vector2(-220, 0);
            waterHover.rectTransform.offsetMax = new Vector2(-100, 0);
            HoverHint.Attach(waterHover.gameObject, context, () => (GameData.Text("ui.hud.water_title"), GameData.Text("ui.hud.water_desc")));
            view.starsLabel = UiKit.Label(bar.transform, "Stars", string.Empty, 22, GameData.ParseColor(palette.mustard), TextAlignmentOptions.MidlineRight);
            UiKit.Pad(view.starsLabel.rectTransform, 0, 0, 610, 0);

            var notebook = UiKit.Button(bar.transform, "Notebook", GameData.Text("ui.hud.notebook"), new Vector2(1, 0), Vector2.one, out _);
            notebook.GetComponent<RectTransform>().offsetMin = new Vector2(-598, 8);
            notebook.GetComponent<RectTransform>().offsetMax = new Vector2(-452, -8);
            notebook.onClick.AddListener(() => onNotebook());
            view.notebookRect = notebook.GetComponent<RectTransform>();

            var endDay = UiKit.Button(bar.transform, "EndDay", GameData.Text("ui.hud.end_day"), new Vector2(1, 0), Vector2.one, out _);
            endDay.GetComponent<RectTransform>().offsetMin = new Vector2(-444, 8);
            endDay.GetComponent<RectTransform>().offsetMax = new Vector2(-292, -8);
            endDay.onClick.AddListener(() => onEndDay());
            view.endDayImage = endDay.GetComponent<UnityEngine.UI.Image>();

            var door = UiKit.Button(bar.transform, "Door", string.Empty, new Vector2(1, 0), Vector2.one, out view.doorLabel);
            door.GetComponent<RectTransform>().offsetMin = new Vector2(-284, 8);
            door.GetComponent<RectTransform>().offsetMax = new Vector2(-56, -8);
            door.onClick.AddListener(() => onDoor());
            view.doorRect = door.GetComponent<RectTransform>();

            var gear = UiKit.GearButton(bar.transform, GearSize, GearMargin);
            gear.onClick.AddListener(() => onSettings());
            view.settingsRect = gear.GetComponent<RectTransform>();

            session.Changed += view.Refresh;
            view.Refresh();
            return view;
        }

        void OnDestroy()
        {
            if (session != null) session.Changed -= Refresh;
        }

        public Vector2 EndDayPoint => endDayImage.rectTransform.position;
        public Vector2 NotebookPoint => notebookRect.position;
        public Vector2 SettingsPoint => settingsRect.position;
        public RectTransform SettingsRect => settingsRect;
        public RectTransform DoorRect => doorRect;
        public RectTransform EndDayRect => endDayImage.rectTransform;

        public void SetRoom(bool inFrontRoom)
        {
            doorLabel.text = GameData.Text(inFrontRoom ? "ui.door.to_back" : "ui.door.to_front");
        }

        void Refresh()
        {
            if (session.Day == null) return;
            dayLabel.text = GameData.Text($"day.{session.DayNumber}.title");
            waterLabel.text = GameData.Text("ui.hud.water", ("count", session.CleaningPoints));
            starsLabel.text = GameData.Text("ui.hud.stars", ("count", session.TotalStars));
            var palette = GameData.Config.palette;
            waterLabel.color = GameData.ParseColor(session.WaterWasted ? palette.tile : palette.cream);
            endDayImage.color = GameData.ParseColor(session.Queue.Count == 0 ? palette.leaf : palette.teal);
        }
    }
}
