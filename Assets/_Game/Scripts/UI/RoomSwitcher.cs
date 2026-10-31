using System;
using System.Collections;
using UnityEngine;

namespace CopAtolyesi.UI
{
    // Iki oda yan yana durur; kaydirma kok alani bir ekran genisligi kadar tasir.
    public sealed class RoomSwitcher : MonoBehaviour
    {
        RectTransform track;
        float duration;
        Coroutine slide;

        public bool InFrontRoom { get; private set; }
        public event Action<bool> RoomChanged;

        public void Init(RectTransform track, float duration, bool startInFront)
        {
            this.track = track;
            this.duration = duration;
            InFrontRoom = startInFront;
            track.anchoredPosition = new Vector2(startInFront ? -track.rect.width : 0, 0);
        }

        public void Toggle()
        {
            InFrontRoom = !InFrontRoom;
            RoomChanged?.Invoke(InFrontRoom);
            if (slide != null) StopCoroutine(slide);
            slide = StartCoroutine(Slide(InFrontRoom ? -track.rect.width : 0));
        }

        IEnumerator Slide(float targetX)
        {
            var startX = track.anchoredPosition.x;
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                track.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, Mathf.SmoothStep(0, 1, t)), 0);
                yield return null;
            }
            track.anchoredPosition = new Vector2(targetX, 0);
        }
    }
}
