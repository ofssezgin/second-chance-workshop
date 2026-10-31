using System;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Veriden gelen adim adim rehber. Oyuncu bir adimi atlarsa rehber onunla birlikte ileri gider,
    // gun bitince de kendini kapatir: eski bir adim sonraki gunlere tasinmaz.
    public sealed class TutorialView : MonoBehaviour
    {
        const float PulseSpeed = 4f;
        const float PulseMin = 0.15f;
        const float PulseMax = 0.3f;

        UiContext context;
        RectTransform banner;
        TMP_Text text;
        Image highlight;
        int step = -1;

        public bool Active => step >= 0;

        public static TutorialView Create(RectTransform overlay, UiContext context)
        {
            var palette = GameData.Config.palette;
            var view = overlay.gameObject.AddComponent<TutorialView>();
            view.context = context;

            view.highlight = UiKit.Panel(overlay, "TutorialHighlight", Vector2.zero, Vector2.zero, GameData.ParseColor(palette.mustard), true);
            view.highlight.raycastTarget = false;

            var center = new Vector2(0.5f, 0.5f);
            var panel = UiKit.Panel(overlay, "TutorialBanner", center, center, GameData.ParseColor(palette.teal), true);
            panel.raycastTarget = false;
            view.banner = panel.rectTransform;
            view.banner.sizeDelta = new Vector2(500, 58);
            view.text = UiKit.Label(panel.transform, "Text", string.Empty, 18, GameData.ParseColor(palette.cream));
            view.text.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Pad(view.text.rectTransform, 12, 2, 12, 2);

            view.SetVisible(false);
            context.Session.Signaled += view.OnSignal;
            return view;
        }

        void OnDestroy() => context.Session.Signaled -= OnSignal;

        void SetVisible(bool visible)
        {
            highlight.gameObject.SetActive(visible);
            banner.gameObject.SetActive(visible);
        }

        public void Begin()
        {
            step = 0;
            ShowStep();
        }

        void Finish()
        {
            step = -1;
            SetVisible(false);
        }

        void OnSignal(string signal)
        {
            if (!Active) return;
            if (signal == "day_ended")
            {
                Finish();
                return;
            }

            var reached = Array.FindIndex(GameData.Tutorial.steps, step, s => s.wait == signal);
            if (reached < 0) return;
            step = reached + 1;
            if (step < GameData.Tutorial.steps.Length) ShowStep();
            else Finish();
        }

        void ShowStep()
        {
            var def = GameData.Tutorial.steps[step];
            text.text = GameData.Text(def.textKey);
            banner.anchorMin = banner.anchorMax = new Vector2(def.anchor[0], def.anchor[1]);
            banner.anchoredPosition = Vector2.zero;
            SetVisible(true);
            highlight.transform.SetAsLastSibling();
            banner.SetAsLastSibling();
        }

        void Update()
        {
            if (!Active) return;
            var target = context.Targets[GameData.Tutorial.steps[step].target];
            var scale = highlight.rectTransform.lossyScale.x;
            highlight.rectTransform.position = target.TransformPoint(target.rect.center);
            highlight.rectTransform.sizeDelta = target.rect.size * target.lossyScale.x / scale;

            var color = highlight.color;
            color.a = Mathf.Lerp(PulseMin, PulseMax, (Mathf.Sin(Time.unscaledTime * PulseSpeed) + 1f) / 2f);
            highlight.color = color;
        }
    }
}
