using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Kosedeki disliyle acilir: muzik ve ses efektleri ayri ayarlanir.
    public sealed class SettingsView : MonoBehaviour
    {
        const float RowLabelRight = 0.42f;

        Slider music;
        Slider sfx;
        RectTransform closeRect;

        public static SettingsView Create(Transform parent)
        {
            var palette = GameData.Config.palette;
            var ink = GameData.ParseColor(palette.anthracite);
            var dim = UiKit.Panel(parent, "Settings", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.65f));
            var view = dim.gameObject.AddComponent<SettingsView>();

            var center = new Vector2(0.5f, 0.5f);
            var card = UiKit.Panel(dim.transform, "Card", center, center, GameData.ParseColor(palette.cream), true);
            card.rectTransform.sizeDelta = new Vector2(520, 300);

            var titleArea = UiKit.Area(card.transform, "Title", new Vector2(0, 0.76f), Vector2.one);
            UiKit.Label(titleArea, "Text", GameData.Text("ui.settings.title"), 30, ink);

            view.music = view.Row(card.transform, "ui.settings.music", 0.52f, ink);
            view.music.onValueChanged.AddListener(value =>
            {
                GameSettings.MusicVolume = value;
                AudioKit.ApplyVolume();
            });

            view.sfx = view.Row(card.transform, "ui.settings.sfx", 0.3f, ink);
            view.sfx.onValueChanged.AddListener(value =>
            {
                GameSettings.SfxVolume = value;
                AudioKit.PlayWork("click");
            });

            var close = UiKit.Button(card.transform, "Close", GameData.Text("ui.settings.close"), new Vector2(0.32f, 0.06f), new Vector2(0.68f, 0.24f), out _);
            close.onClick.AddListener(view.Toggle);
            view.closeRect = close.GetComponent<RectTransform>();

            dim.gameObject.SetActive(false);
            return view;
        }

        Slider Row(Transform card, string textKey, float bottom, Color ink)
        {
            var labelArea = UiKit.Area(card, "Label", new Vector2(0.07f, bottom), new Vector2(RowLabelRight, bottom + 0.2f));
            UiKit.Label(labelArea, "Text", GameData.Text(textKey), 22, ink, TextAlignmentOptions.MidlineLeft);
            return UiKit.VolumeSlider(card, new Vector2(RowLabelRight, bottom), new Vector2(0.93f, bottom + 0.2f));
        }

        public void Toggle()
        {
            music.SetValueWithoutNotify(GameSettings.MusicVolume);
            sfx.SetValueWithoutNotify(GameSettings.SfxVolume);
            gameObject.SetActive(!gameObject.activeSelf);
            transform.SetAsLastSibling();
        }

        // Otomatik test icin ekran konumlari.
        public bool IsOpen => gameObject.activeSelf;
        public Vector2 ClosePoint => closeRect.position;
        public Vector2 MusicPoint(float ratio) => SliderPoint(music, ratio);
        public Vector2 SfxPoint(float ratio) => SliderPoint(sfx, ratio);

        static Vector2 SliderPoint(Slider slider, float ratio)
        {
            var corners = new Vector3[4];
            ((RectTransform)slider.transform).GetWorldCorners(corners);
            return Vector3.Lerp(corners[0], corners[3], ratio) + (corners[1] - corners[0]) / 2;
        }
    }
}
