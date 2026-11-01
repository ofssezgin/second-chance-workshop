using System;
using System.Collections.Generic;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Ana menu: baslik, oyna, emegi gecenler, cikis, dil secimi ve kosede ayarlar.
    public sealed class MenuView : MonoBehaviour
    {
        const float RowLeft = 0.32f;
        const float RowRight = 0.68f;
        const float LabelWidth = 0.1f;
        const float GearSize = 56f;
        const float GearMargin = 16f;

        TMP_Text title;
        TMP_Text playLabel;
        TMP_Text creditsLabel;
        TMP_Text quitLabel;
        TMP_Text languageLabel;
        TMP_Text creditsTitle;
        TMP_Text creditsRole;
        TMP_Text creditsName;
        TMP_Text creditsMail;
        TMP_Text creditsRole2;
        TMP_Text creditsName2;
        TMP_Text creditsMail2;
        TMP_Text creditsMusic;
        TMP_Text backLabel;
        GameObject creditsPanel;
        RectTransform playRect;
        RectTransform creditsRect;
        RectTransform backRect;
        RectTransform quitRect;
        RectTransform gearRect;
        SettingsView settings;
        readonly Dictionary<string, Image> languageButtons = new Dictionary<string, Image>();

        public static MenuView Create(Transform parent, Action onPlay)
        {
            var palette = GameData.Config.palette;
            var background = UiKit.Panel(parent, "Menu", Vector2.zero, Vector2.one, GameData.ParseColor(palette.wall));
            UiKit.ApplyArt(background, "Sprites/Menu/background");

            var view = background.gameObject.AddComponent<MenuView>();
            var ink = GameData.ParseColor(palette.anthracite);

            var titleArea = UiKit.Area(background.transform, "Title", new Vector2(0, 0.67f), new Vector2(1, 0.98f));
            view.title = UiKit.Label(titleArea, "Text", string.Empty, 76, ink);

            var play = UiKit.Button(background.transform, "Play", string.Empty, new Vector2(RowLeft, 0.54f), new Vector2(RowRight, 0.65f), out view.playLabel);
            play.onClick.AddListener(() => onPlay());
            view.playLabel.fontSize = 30;
            view.playRect = play.GetComponent<RectTransform>();

            var credits = UiKit.Button(background.transform, "Credits", string.Empty, new Vector2(RowLeft, 0.45f), new Vector2(RowRight, 0.53f), out view.creditsLabel);
            credits.onClick.AddListener(() =>
            {
                view.creditsPanel.SetActive(true);
                view.creditsPanel.transform.SetAsLastSibling();
            });
            view.creditsRect = credits.GetComponent<RectTransform>();

            var quit = UiKit.Button(background.transform, "Quit", string.Empty, new Vector2(RowLeft, 0.35f), new Vector2(RowRight, 0.43f), out view.quitLabel);
            quit.onClick.AddListener(Application.Quit);
            view.quitRect = quit.GetComponent<RectTransform>();
#if UNITY_WEBGL && !UNITY_EDITOR
            // Tarayicida oyundan cikilamaz; sekme kapatilir.
            quit.gameObject.SetActive(false);
#endif

            view.BuildLanguageRow(background.transform, ink);
            view.BuildCredits(background.transform, ink);

            var gear = UiKit.GearButton(background.transform, GearSize, GearMargin);
            gear.onClick.AddListener(() => view.settings.Toggle());
            view.gearRect = gear.GetComponent<RectTransform>();
            view.settings = SettingsView.Create(background.transform);
            view.Refresh();
            return view;
        }

        void BuildCredits(Transform parent, Color ink)
        {
            var dim = UiKit.Panel(parent, "CreditsPanel", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.6f));
            creditsPanel = dim.gameObject;

            var card = UiKit.Panel(dim.transform, "Card", new Vector2(0.18f, 0.2f), new Vector2(0.82f, 0.8f), GameData.ParseColor(GameData.Config.palette.cream), true);
            creditsTitle = UiKit.Label(UiKit.Area(card.transform, "Title", new Vector2(0, 0.79f), new Vector2(1, 0.97f)), "Text", string.Empty, 40, ink);
            (creditsRole, creditsName, creditsMail) = PersonColumn(card.transform, 0, 0.5f, ink);
            (creditsRole2, creditsName2, creditsMail2) = PersonColumn(card.transform, 0.5f, 1, ink);
            creditsMusic = UiKit.Label(UiKit.Area(card.transform, "Music", new Vector2(0, 0.3f), new Vector2(1, 0.4f)), "Text", string.Empty, 20, ink);

            var back = UiKit.Button(card.transform, "Back", string.Empty, new Vector2(0.3f, 0.07f), new Vector2(0.7f, 0.24f), out backLabel);
            back.onClick.AddListener(() => creditsPanel.SetActive(false));
            backRect = back.GetComponent<RectTransform>();
            creditsPanel.SetActive(false);
        }

        static (TMP_Text role, TMP_Text name, TMP_Text mail) PersonColumn(Transform card, float left, float right, Color ink)
        {
            var role = UiKit.Label(UiKit.Area(card, "Role", new Vector2(left, 0.67f), new Vector2(right, 0.77f)), "Text", string.Empty, 22, ink);
            var name = UiKit.Label(UiKit.Area(card, "Name", new Vector2(left, 0.54f), new Vector2(right, 0.67f)), "Text", string.Empty, 30, ink);
            var mail = UiKit.Label(UiKit.Area(card, "Mail", new Vector2(left, 0.44f), new Vector2(right, 0.54f)), "Text", string.Empty, 22, ink);
            return (role, name, mail);
        }

        void BuildLanguageRow(Transform parent, Color ink)
        {
            // Arka plan hareketli oldugu icin yazi acik bir zeminde durur.
            var labelBack = UiKit.Panel(parent, "LanguageLabel", new Vector2(RowLeft, 0.25f), new Vector2(RowLeft + LabelWidth - 0.005f, 0.33f), GameData.ParseColor(GameData.Config.palette.cream), true);
            languageLabel = UiKit.Label(labelBack.transform, "Text", string.Empty, 22, ink);

            var left = RowLeft + LabelWidth;
            var width = (RowRight - left) / GameSettings.Languages.Length;
            for (var i = 0; i < GameSettings.Languages.Length; i++)
            {
                var language = GameSettings.Languages[i];
                var x = left + i * width;
                var button = UiKit.Button(parent, "Lang_" + language, GameData.Text("lang." + language),
                    new Vector2(x + 0.005f, 0.25f), new Vector2(x + width - 0.005f, 0.33f), out _);
                button.onClick.AddListener(() => SelectLanguage(language));
                languageButtons[language] = button.GetComponent<Image>();
            }
        }

        void SelectLanguage(string language)
        {
            GameSettings.Language = language;
            Refresh();
        }

        void Refresh()
        {
            title.text = GameData.Text("game.title");
            playLabel.text = GameData.Text("menu.play");
            creditsLabel.text = GameData.Text("menu.credits");
            creditsTitle.text = GameData.Text("menu.credits");
            creditsRole.text = GameData.Text("menu.credits.role");
            creditsName.text = GameData.Text("menu.credits.name");
            creditsMail.text = GameData.Text("menu.credits.mail");
            creditsRole2.text = GameData.Text("menu.credits.role2");
            creditsName2.text = GameData.Text("menu.credits.name2");
            creditsMail2.text = GameData.Text("menu.credits.mail2");
            creditsMusic.text = GameData.Text("menu.credits.music");
            backLabel.text = GameData.Text("menu.back");
            quitLabel.text = GameData.Text("menu.quit");
            languageLabel.text = GameData.Text("menu.language");

            var palette = GameData.Config.palette;
            foreach (var pair in languageButtons)
                pair.Value.color = GameData.ParseColor(pair.Key == GameSettings.Language ? palette.teal : palette.locked);
        }

        // Otomatik test icin ekran konumlari.
        public Vector2 PlayPoint => playRect.position;
        public Vector2 CreditsPoint => creditsRect.position;
        public Vector2 BackPoint => backRect.position;
        public bool CreditsOpen => creditsPanel.activeSelf;
        public Vector2 LanguagePoint(string language) => languageButtons[language].rectTransform.position;
        public Vector2 GearPoint => gearRect.position;
        public SettingsView Settings => settings;
    }
}
