using System;
using System.Collections;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Oyun sonu: mahalle parki, yapilan urunler, atiklardan anit ve parka gelen musteriler.
    public sealed class FinalView : MonoBehaviour
    {
        const float FadeSeconds = 0.9f;
        const int PropScale = 3;
        const int PortraitScale = 2;
        const float ProductSize = 96f;
        const float MonumentBaseY = 240f;
        static readonly Color DryPark = new Color(0.85f, 0.72f, 0.5f);

        static readonly (string product, string art, float x, float y)[] Props =
        {
            ("swing", "swing", 0.2f, 0.38f),
            ("bird_feeder", "bird_feeder", 0.86f, 0.42f),
            ("planter", "planter", 0.34f, 0.3f),
            ("wind_chime", "wind_chime", 0.68f, 0.42f)
        };

        static readonly float[] CustomerSlots = { 0.09f, 0.25f, 0.41f, 0.59f, 0.75f, 0.91f };

        RectTransform root;
        CanvasGroup group;
        TMP_Text title;
        TMP_Text lines;
        TMP_Text buttonLabel;
        RectTransform button;
        Action onContinue;

        public Vector2 ContinuePoint => button.position;
        public string TitleText => title.text;

        public static FinalView Create(Transform parent)
        {
            var palette = GameData.Config.palette;
            var background = UiKit.Panel(parent, "Final", Vector2.zero, Vector2.one, GameData.ParseColor(palette.leaf));
            UiKit.ApplyArt(background, "Sprites/Park/park");

            var view = background.gameObject.AddComponent<FinalView>();
            view.root = background.rectTransform;
            view.group = background.gameObject.AddComponent<CanvasGroup>();
            background.gameObject.SetActive(false);
            return view;
        }

        public void Show(GameSession session, string titleText, string linesText, string buttonText, Action action)
        {
            onContinue = action;
            if (session.WaterWasted) root.GetComponent<Image>().color = DryPark;
            AudioKit.PlayMusic(session.WaterWasted ? "music_dry" : "music_good");
            BuildProps(session);
            BuildMonument(session);
            BuildCustomers(session);
            BuildCard(titleText, linesText, buttonText);
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            StartCoroutine(FadeIn());
        }

        IEnumerator FadeIn()
        {
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / FadeSeconds)
            {
                group.alpha = t;
                yield return null;
            }
            group.alpha = 1;
        }

        Image Sprite(string path, Vector2 anchor, Vector2 pivot, int scale)
        {
            var art = Resources.Load<Sprite>("Sprites/" + path);
            var image = UiKit.Panel(root, path, anchor, anchor, Color.white);
            image.raycastTarget = false;
            image.rectTransform.pivot = pivot;
            image.rectTransform.anchoredPosition = Vector2.zero;
            image.sprite = art;
            image.enabled = art != null;
            if (art != null) image.rectTransform.sizeDelta = art.rect.size * scale;
            return image;
        }

        // Son gun servis edilen urunler parkta durur; yapilamayan urunun yeri bos kalir.
        // Su israf edildiyse park kurur, hicbiri kurulamaz.
        void BuildProps(GameSession session)
        {
            if (session.WaterWasted) return;
            foreach (var (product, art, x, y) in Props)
                if (session.FinalProducts.Contains(product))
                    Sprite("Park/" + art, new Vector2(x, y), new Vector2(0.5f, 0), PropScale);
        }

        // Mahalle Aniti sabit bir heykeldir: atiktan agac ve meyve koparan robot.
        // Su israf edildiyse agac kurumus, robot uzgundur ve heykel de parkla birlikte solar.
        void BuildMonument(GameSession session)
        {
            var bottom = new Vector2(0.5f, 0);
            var pedestal = Sprite("Park/pedestal", bottom, bottom, PropScale);
            pedestal.rectTransform.anchoredPosition = new Vector2(0, MonumentBaseY - 60);

            var monument = Sprite(session.WaterWasted ? "Park/monument_dry" : "Park/monument", bottom, bottom, PropScale);
            monument.rectTransform.anchoredPosition = new Vector2(0, MonumentBaseY);
            if (session.WaterWasted) monument.color = DryPark;
        }

        // Mahalleli parkin onunde yan yana durur; yaptigin urun elindedir ve sana tesekkur eder.
        // Beklerken kacirilan musteri gulumsemez.
        void BuildCustomers(GameSession session)
        {
            var palette = GameData.Config.palette;
            var index = 0;
            foreach (var customer in GameData.Customers.Keys)
            {
                var x = CustomerSlots[index++];
                var suffix = session.WaterWasted || session.MissedBy.Contains(customer) ? string.Empty : "_happy";
                Sprite("Customers/" + customer + suffix, new Vector2(x, 0), new Vector2(0.5f, 0), PortraitScale);

                if (session.Delivered.TryGetValue(customer, out var product))
                {
                    var view = ItemView.Create(root);
                    view.Show(product.CleanCopy());
                    var rect = (RectTransform)view.transform;
                    rect.anchorMin = rect.anchorMax = new Vector2(x, 0);
                    rect.pivot = new Vector2(0.5f, 0);
                    rect.sizeDelta = Vector2.one * ProductSize;
                    rect.anchoredPosition = new Vector2(62, 8);
                }

                if (session.WaterWasted) continue;
                var bubble = UiKit.Panel(root, "Thanks", new Vector2(x, 0), new Vector2(x, 0), GameData.ParseColor(palette.cream), true);
                bubble.raycastTarget = false;
                bubble.rectTransform.pivot = new Vector2(0.5f, 0);
                bubble.rectTransform.sizeDelta = new Vector2(150, 40);
                bubble.rectTransform.anchoredPosition = new Vector2(0, 150);
                UiKit.Label(bubble.transform, "Text", GameData.Text("ui.final.thanks"), 18, GameData.ParseColor(palette.anthracite));
            }
        }

        void BuildCard(string titleText, string linesText, string buttonText)
        {
            var palette = GameData.Config.palette;
            var ink = GameData.ParseColor(palette.anthracite);
            var top = new Vector2(0.5f, 1);
            var card = UiKit.Panel(root, "Card", top, top, GameData.ParseColor(palette.cream), true);
            card.rectTransform.pivot = top;
            card.rectTransform.sizeDelta = new Vector2(900, 246);
            card.rectTransform.anchoredPosition = new Vector2(0, -8);

            var titleArea = UiKit.Area(card.transform, "Title", new Vector2(0, 0.82f), Vector2.one);
            title = UiKit.Label(titleArea, "Text", titleText, 28, ink);
            UiKit.UseDisplayFont(title);
            var linesArea = UiKit.Area(card.transform, "Lines", new Vector2(0, 0.2f), new Vector2(1, 0.82f));
            lines = UiKit.Label(linesArea, "Text", linesText, 17, ink);
            lines.textWrappingMode = TextWrappingModes.Normal;
            UiKit.UseDisplayFont(lines);
            UiKit.Pad(lines.rectTransform, 16, 0, 16, 0);

            var continueButton = UiKit.Button(card.transform, "Continue", buttonText, new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.19f), out buttonLabel);
            continueButton.onClick.AddListener(() => onContinue?.Invoke());
            button = continueButton.GetComponent<RectTransform>();
        }
    }
}
