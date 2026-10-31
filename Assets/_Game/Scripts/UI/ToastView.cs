using System.Collections;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;

namespace CopAtolyesi.UI
{
    // Kisa uyari mesaji: "Raf dolu", "Su bitti" gibi engeller burada gorunur.
    public sealed class ToastView : MonoBehaviour
    {
        const float FadeSeconds = 0.25f;

        CanvasGroup group;
        TMP_Text label;
        Coroutine running;

        public string Text => label.text;

        public static ToastView Create(Transform parent)
        {
            var center = new Vector2(0.5f, 0.5f);
            var panel = UiKit.Panel(parent, "Toast", center, center, GameData.ParseColor(GameData.Config.palette.tile), true);
            panel.raycastTarget = false;
            panel.rectTransform.sizeDelta = new Vector2(680, 76);

            var view = panel.gameObject.AddComponent<ToastView>();
            view.group = panel.gameObject.AddComponent<CanvasGroup>();
            view.group.alpha = 0;
            view.group.blocksRaycasts = false;
            view.label = UiKit.Label(panel.transform, "Text", string.Empty, 22, GameData.ParseColor(GameData.Config.palette.cream));
            return view;
        }

        public void Show(string text)
        {
            label.text = text;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            group.alpha = 1;
            yield return new WaitForSecondsRealtime(GameData.Config.toastSeconds);
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / FadeSeconds)
            {
                group.alpha = 1f - t;
                yield return null;
            }
            group.alpha = 0;
        }
    }
}
