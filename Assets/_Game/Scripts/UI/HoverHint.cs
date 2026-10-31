using System;
using System.Collections;
using CopAtolyesi.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CopAtolyesi.UI
{
    // Bir arayuz ogesinin uzerine gelince baslik ve aciklama gosterir.
    public sealed class HoverHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        Func<(string title, string body)> text;
        UiContext context;
        Coroutine hover;

        public static void Attach(GameObject target, UiContext context, Func<(string title, string body)> text)
        {
            var hint = target.AddComponent<HoverHint>();
            hint.context = context;
            hint.text = text;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            hover = StartCoroutine(Show());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (hover != null) StopCoroutine(hover);
            hover = null;
            context.Tooltip.Hide();
        }

        IEnumerator Show()
        {
            yield return new WaitForSecondsRealtime(GameData.Config.hoverSeconds);
            var (title, body) = text();
            context.Tooltip.ShowText(title, body, (RectTransform)transform);
        }
    }
}
