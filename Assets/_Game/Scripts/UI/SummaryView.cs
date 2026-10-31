using System;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Gun sonu ve final icin ortak ekran: baslik, satirlar ve tek buton.
    public sealed class SummaryView : MonoBehaviour
    {
        TMP_Text title;
        TMP_Text lines;
        TMP_Text buttonLabel;
        Action onContinue;
        RectTransform button;

        public Vector2 ContinuePoint => button.position;

        public static SummaryView Create(Transform parent)
        {
            var palette = GameData.Config.palette;
            var dim = UiKit.Panel(parent, "Summary", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.65f));
            var view = dim.gameObject.AddComponent<SummaryView>();

            var center = new Vector2(0.5f, 0.5f);
            var card = UiKit.Panel(dim.transform, "Card", center, center, GameData.ParseColor(palette.cream), true);
            card.rectTransform.sizeDelta = new Vector2(720, 480);

            var ink = GameData.ParseColor(palette.anthracite);
            var titleArea = UiKit.Area(card.transform, "Title", new Vector2(0, 0.8f), Vector2.one);
            view.title = UiKit.Label(titleArea, "Text", string.Empty, 32, ink);
            var linesArea = UiKit.Area(card.transform, "Lines", new Vector2(0, 0.24f), new Vector2(1, 0.78f));
            view.lines = UiKit.Label(linesArea, "Text", string.Empty, 22, ink);
            view.lines.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Pad(view.lines.rectTransform, 36, 0, 36, 0);

            var button = UiKit.Button(card.transform, "Continue", string.Empty, new Vector2(0.28f, 0.05f), new Vector2(0.72f, 0.19f), out view.buttonLabel);
            button.onClick.AddListener(view.Continue);
            view.button = button.GetComponent<RectTransform>();

            dim.gameObject.SetActive(false);
            return view;
        }

        public void Show(string titleText, string linesText, string buttonText, Action action)
        {
            title.text = titleText;
            lines.text = linesText;
            buttonLabel.text = buttonText;
            onContinue = action;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        void Continue()
        {
            gameObject.SetActive(false);
            onContinue?.Invoke();
        }
    }
}
