using System.Collections.Generic;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Rehber acikken ekrani kapatir; yalnizca adimin izin verdigi alanlardaki tiklamalar gecer.
    public sealed class TutorialGate : MonoBehaviour, ICanvasRaycastFilter
    {
        public readonly List<RectTransform> Open = new List<RectTransform>();

        public bool IsRaycastLocationValid(Vector2 point, Camera eventCamera) =>
            !Open.Exists(rect => RectTransformUtility.RectangleContainsScreenPoint(rect, point, eventCamera));
    }

    // Veriden gelen adim adim rehber. Yalnizca siradaki adim yapilinca ilerler ve o adimda
    // soylenen yerler disinda hicbir seye dokunulamaz; gun bitince kendini kapatir.
    public sealed class TutorialView : MonoBehaviour
    {
        const float PulseSpeed = 4f;
        const float PulseMin = 0.15f;
        const float PulseMax = 0.3f;

        UiContext context;
        RectTransform banner;
        TMP_Text text;
        Image highlight;
        TutorialGate gate;
        int step = -1;

        public bool Active => step >= 0;
        public string CurrentKey => Active ? GameData.Tutorial.steps[step].textKey : null;

        public static TutorialView Create(RectTransform overlay, UiContext context)
        {
            var palette = GameData.Config.palette;
            var view = overlay.gameObject.AddComponent<TutorialView>();
            view.context = context;

            view.gate = UiKit.Panel(overlay, "TutorialGate", Vector2.zero, Vector2.one, Color.clear).gameObject.AddComponent<TutorialGate>();
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
            gate.gameObject.SetActive(visible);
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

            if (GameData.Tutorial.steps[step].wait != signal) return;
            step++;
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

            gate.Open.Clear();
            gate.Open.Add(context.Targets[def.target]);
            gate.Open.Add(context.Targets["settings"]);
            foreach (var id in def.allow ?? new string[0]) gate.Open.Add(context.Targets[id]);

            gate.transform.SetAsLastSibling();
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
