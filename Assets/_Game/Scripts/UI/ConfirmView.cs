using System;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;

namespace CopAtolyesi.UI
{
    // Evet/hayir onayi: musteri varken gunu bitirmeden once sorulur.
    public sealed class ConfirmView : MonoBehaviour
    {
        TMP_Text message;
        Action onYes;
        RectTransform yesRect;

        public Vector2 YesPoint => yesRect.position;

        public static ConfirmView Create(Transform parent)
        {
            var palette = GameData.Config.palette;
            var dim = UiKit.Panel(parent, "Confirm", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.65f));
            var view = dim.gameObject.AddComponent<ConfirmView>();

            var center = new Vector2(0.5f, 0.5f);
            var card = UiKit.Panel(dim.transform, "Card", center, center, GameData.ParseColor(palette.cream), true);
            card.rectTransform.sizeDelta = new Vector2(560, 240);

            var textArea = UiKit.Area(card.transform, "Text", new Vector2(0, 0.38f), Vector2.one);
            view.message = UiKit.Label(textArea, "Text", string.Empty, 24, GameData.ParseColor(palette.anthracite));
            view.message.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Pad(view.message.rectTransform, 28, 8, 28, 8);

            var yes = UiKit.Button(card.transform, "Yes", GameData.Text("ui.confirm.yes"), new Vector2(0.08f, 0.1f), new Vector2(0.48f, 0.34f), out _);
            yes.onClick.AddListener(view.Yes);
            view.yesRect = yes.GetComponent<RectTransform>();
            var no = UiKit.Button(card.transform, "No", GameData.Text("ui.confirm.no"), new Vector2(0.52f, 0.1f), new Vector2(0.92f, 0.34f), out _);
            no.onClick.AddListener(() => view.gameObject.SetActive(false));

            dim.gameObject.SetActive(false);
            return view;
        }

        public void Show(string text, Action action)
        {
            message.text = text;
            onYes = action;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        void Yes()
        {
            gameObject.SetActive(false);
            onYes?.Invoke();
        }
    }
}
