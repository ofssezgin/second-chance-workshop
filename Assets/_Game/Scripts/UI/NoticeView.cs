using System.Collections.Generic;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;

namespace CopAtolyesi.UI
{
    // Oyuncu Gec dugmesine basana kadar ekranda kalan rehber notu; arkadaki hicbir seye dokunulamaz.
    // Sirasi gelen notlar arka arkaya gosterilir.
    public sealed class NoticeView : MonoBehaviour
    {
        GameSession session;
        TMP_Text message;
        RectTransform nextRect;
        readonly Queue<string> waiting = new Queue<string>();
        readonly HashSet<NoticeDef> shown = new HashSet<NoticeDef>();

        public bool IsOpen => gameObject.activeSelf;
        public string Message => message.text;
        public Vector2 NextPoint => nextRect.position;

        public static NoticeView Create(Transform parent, GameSession session)
        {
            var palette = GameData.Config.palette;
            var dim = UiKit.Panel(parent, "Notice", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.65f));
            var view = dim.gameObject.AddComponent<NoticeView>();
            view.session = session;

            var center = new Vector2(0.5f, 0.5f);
            var card = UiKit.Panel(dim.transform, "Card", center, center, GameData.ParseColor(palette.cream), true);
            card.rectTransform.sizeDelta = new Vector2(640, 300);

            var ink = GameData.ParseColor(palette.anthracite);
            var textArea = UiKit.Area(card.transform, "Text", new Vector2(0, 0.24f), Vector2.one);
            view.message = UiKit.Label(textArea, "Text", string.Empty, 24, ink);
            view.message.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Pad(view.message.rectTransform, 28, 8, 28, 8);

            var next = UiKit.Button(card.transform, "Next", GameData.Text("ui.notice.next"), new Vector2(0.38f, 0.06f), new Vector2(0.62f, 0.22f), out _);
            next.onClick.AddListener(view.Next);
            view.nextRect = next.GetComponent<RectTransform>();

            dim.gameObject.SetActive(false);
            session.Signaled += view.OnSignal;
            return view;
        }

        void OnDestroy() => session.Signaled -= OnSignal;

        void OnSignal(string signal)
        {
            foreach (var notice in GameData.Tutorial.notices)
                if (notice.signal == signal && session.DayNumber >= notice.day && shown.Add(notice))
                    Show(GameData.Text(notice.textKey));
        }

        public void Show(string text)
        {
            waiting.Enqueue(text);
            if (!IsOpen) ShowNext();
        }

        void ShowNext()
        {
            message.text = waiting.Dequeue();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        void Next()
        {
            if (waiting.Count > 0) ShowNext();
            else gameObject.SetActive(false);
        }
    }
}
