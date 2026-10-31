using System;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;

namespace CopAtolyesi.UI
{
    // Esc ile acilan duraklatma menusu: devam et veya ana menuye don.
    public sealed class PauseView : MonoBehaviour
    {
        RectTransform menuButton;

        public static PauseView Create(Transform parent, Action onMenu)
        {
            var palette = GameData.Config.palette;
            var dim = UiKit.Panel(parent, "Pause", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.65f));
            var view = dim.gameObject.AddComponent<PauseView>();

            var center = new Vector2(0.5f, 0.5f);
            var card = UiKit.Panel(dim.transform, "Card", center, center, GameData.ParseColor(palette.cream), true);
            card.rectTransform.sizeDelta = new Vector2(420, 280);

            var titleArea = UiKit.Area(card.transform, "Title", new Vector2(0, 0.72f), Vector2.one);
            UiKit.Label(titleArea, "Text", GameData.Text("ui.pause.title"), 30, GameData.ParseColor(palette.anthracite));

            var resume = UiKit.Button(card.transform, "Resume", GameData.Text("ui.pause.resume"), new Vector2(0.16f, 0.4f), new Vector2(0.84f, 0.64f), out _);
            resume.onClick.AddListener(view.Toggle);
            var menu = UiKit.Button(card.transform, "Menu", GameData.Text("ui.ending.again"), new Vector2(0.16f, 0.1f), new Vector2(0.84f, 0.34f), out _);
            menu.onClick.AddListener(() => onMenu());
            view.menuButton = menu.GetComponent<RectTransform>();

            dim.gameObject.SetActive(false);
            return view;
        }

        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            transform.SetAsLastSibling();
        }
    }
}
