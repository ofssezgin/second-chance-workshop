using System.Collections.Generic;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CopAtolyesi.UI
{
    // Oyuncu bir tusa basana kadar ekranda kalan rehber notu. Sirasi gelen notlar arka arkaya gosterilir.
    public sealed class NoticeView : MonoBehaviour
    {
        GameSession session;
        TMP_Text message;
        readonly Queue<string> waiting = new Queue<string>();
        readonly HashSet<NoticeDef> shown = new HashSet<NoticeDef>();
        int shownFrame;

        public bool IsOpen => gameObject.activeSelf;
        public string Message => message.text;

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
            var textArea = UiKit.Area(card.transform, "Text", new Vector2(0, 0.18f), Vector2.one);
            view.message = UiKit.Label(textArea, "Text", string.Empty, 24, ink);
            view.message.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Pad(view.message.rectTransform, 28, 8, 28, 8);

            var promptArea = UiKit.Area(card.transform, "Prompt", Vector2.zero, new Vector2(1, 0.18f));
            UiKit.Label(promptArea, "Text", GameData.Text("ui.cutscene.continue"), 18, GameData.ParseColor(palette.teal));

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
            shownFrame = Time.frameCount;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        void Update()
        {
            if (Time.frameCount == shownFrame) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if ((keyboard == null || !keyboard.anyKey.wasPressedThisFrame) && (mouse == null || !mouse.leftButton.wasPressedThisFrame)) return;

            if (waiting.Count > 0) ShowNext();
            else gameObject.SetActive(false);
        }
    }
}
