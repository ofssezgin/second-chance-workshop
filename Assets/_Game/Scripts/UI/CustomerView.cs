using System.Collections;
using System.Collections.Generic;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Ust yaridaki musteri: portre, konusma balonu, teslim alani, sira bilgisi ve servis sonrasi tepki.
    public sealed class CustomerView : MonoBehaviour
    {
        const float PortraitWidth = 256f;
        const float ReactionSeconds = 2.4f;
        const float DeliveryWidth = 220f;
        const float StarSize = 48f;
        const float StarPopSeconds = 0.22f;

        UiContext context;
        GameSession session;
        Image portrait;
        TMP_Text portraitName;
        TMP_Text speech;
        TMP_Text queueLabel;
        RectTransform bubble;
        RectTransform deliveryArea;
        RectTransform reactionRoot;
        OrderDef reacting;
        OrderDef lastOrder;
        readonly HashSet<OrderDef> hiddenNoticed = new HashSet<OrderDef>();

        public string SpeechText => speech.text;
        public Vector2 DeliveryPoint => deliveryArea.position;

        public static CustomerView Create(RectTransform content, UiContext context)
        {
            var palette = GameData.Config.palette;
            var view = content.gameObject.AddComponent<CustomerView>();
            view.context = context;
            view.session = context.Session;

            view.portrait = UiKit.Panel(content, "Portrait", Vector2.zero, new Vector2(0, 1), GameData.ParseColor(palette.teal), true);
            view.portrait.rectTransform.offsetMax = new Vector2(PortraitWidth, 0);
            view.portrait.raycastTarget = false;
            view.portraitName = UiKit.Label(view.portrait.transform, "Name", string.Empty, 22, GameData.ParseColor(palette.cream));

            var bubble = UiKit.Panel(content, "Bubble", Vector2.zero, Vector2.one, GameData.ParseColor(palette.cream), true);
            bubble.rectTransform.offsetMin = new Vector2(PortraitWidth + 16, 40);
            bubble.rectTransform.offsetMax = new Vector2(-(DeliveryWidth + 12), 0);
            bubble.raycastTarget = false;
            view.bubble = bubble.rectTransform;
            view.speech = UiKit.Label(bubble.transform, "Text", string.Empty, 24, GameData.ParseColor(palette.anthracite), TextAlignmentOptions.MidlineLeft);
            view.speech.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Pad(view.speech.rectTransform, 20, 8, 20, 8);

            view.deliveryArea = UiKit.Area(content, "DeliveryArea", new Vector2(1, 0), Vector2.one);
            view.deliveryArea.offsetMin = new Vector2(-DeliveryWidth, 0);
            view.deliveryArea.offsetMax = Vector2.zero;
            context.Delivery = DeliveryZone.Create(view.deliveryArea, context);
            context.Targets["delivery"] = view.deliveryArea;

            var queueArea = UiKit.Area(content, "Queue", Vector2.zero, new Vector2(1, 0));
            queueArea.offsetMin = new Vector2(PortraitWidth + 16, 0);
            queueArea.offsetMax = new Vector2(-(DeliveryWidth + 12), 34);
            view.queueLabel = UiKit.Label(queueArea, "Text", string.Empty, 20, GameData.ParseColor(palette.anthracite), TextAlignmentOptions.MidlineLeft);

            view.session.Changed += view.Refresh;
            view.session.Served += view.OnServed;
            view.session.ItemBrought += view.OnItemBrought;
            view.Refresh();
            return view;
        }

        void OnDestroy()
        {
            if (session == null) return;
            session.Changed -= Refresh;
            session.Served -= OnServed;
            session.ItemBrought -= OnItemBrought;
        }

        void OnItemBrought(BroughtItemDef brought) => context.Toast.Show(GameData.Text(brought.textKey));

        // Tercih karsilanmadiysa musteri urunu alir ama tam memnun degildir ve nedenini soyler.
        void OnServed(OrderDef order, CraftResult result)
        {
            reacting = order;
            ShowCustomer(order.customer, result.PrefMet);
            var reaction = result.PrefMet ? "success" : "partial";
            speech.text = GameData.Text($"customer.{order.customer}.{reaction}");
            if (!result.PrefMet)
                speech.text += "\n" + GameData.Text("ui.customer.pref_missed", ("pref", OrderReminderView.PrefValue(order)));

            AudioKit.Play("success");
            if (reactionRoot != null) Destroy(reactionRoot.gameObject);
            reactionRoot = UiKit.Area(bubble, "Reaction", Vector2.zero, Vector2.one);
            context.Delivery.SetLabelVisible(false);
            var delivered = ItemView.Create(deliveryArea);
            delivered.Show(result.Delivered);
            var rect = (RectTransform)delivered.transform;
            rect.SetParent(reactionRoot, true);
            StartCoroutine(PopStars(reactionRoot, result.Stars));
            StartCoroutine(EndReaction(order));
        }

        IEnumerator PopStars(RectTransform parent, int count)
        {
            var art = Resources.Load<Sprite>("Sprites/Ui/star");
            for (var i = 0; i < count && parent != null; i++)
            {
                var anchor = new Vector2(0, 1);
                var star = UiKit.Panel(parent, "Star", anchor, anchor, Color.white);
                star.sprite = art;
                star.raycastTarget = false;
                star.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                star.rectTransform.sizeDelta = Vector2.one * StarSize;
                star.rectTransform.anchoredPosition = new Vector2(44 + i * (StarSize + 8), -38);
                AudioKit.Play("star");
                for (var t = 0f; t < 1f && star != null; t += Time.unscaledDeltaTime / StarPopSeconds)
                {
                    star.rectTransform.localScale = Vector3.one * Mathf.Sin(t * Mathf.PI * 0.75f) * 1.35f;
                    yield return null;
                }
                if (star != null) star.rectTransform.localScale = Vector3.one;
            }
        }

        IEnumerator EndReaction(OrderDef order)
        {
            yield return new WaitForSecondsRealtime(ReactionSeconds);
            if (reacting != order) yield break;
            reacting = null;
            if (reactionRoot != null) Destroy(reactionRoot.gameObject);
            context.Delivery.SetLabelVisible(true);
            Refresh();
        }

        void Refresh()
        {
            queueLabel.text = GameData.Text("ui.customer.queue", ("count", Mathf.Max(0, session.Queue.Count - 1)));
            if (reacting != null) return;

            var order = session.CurrentOrder;
            portrait.gameObject.SetActive(order != null);
            if (order == null)
            {
                speech.text = GameData.Text("ui.customer.none_end");
                return;
            }
            ShowCustomer(order.customer, false);
            speech.text = GameData.Text(order.textKey);
            if (order != lastOrder) AudioKit.Play("bell");
            lastOrder = order;
            if (order.hidden && session.IsCurrentOrderSeen && hiddenNoticed.Add(order))
                context.Notice.Show(GameData.Text("feedback.notebook_hint"));
        }

        void ShowCustomer(string id, bool happy)
        {
            portrait.gameObject.SetActive(true);
            ApplyPortrait(portrait, portraitName, id, happy);
        }

        public static void ApplyPortrait(Image image, TMP_Text name, string id, bool happy)
        {
            var art = Resources.Load<Sprite>(GameData.Customers[id].portrait + (happy ? "_happy" : string.Empty));
            image.sprite = art != null ? art : UiKit.Rounded;
            image.type = art != null ? Image.Type.Simple : Image.Type.Sliced;
            image.preserveAspect = art != null;
            image.color = art != null ? Color.white : GameData.ParseColor(GameData.Config.palette.teal);
            name.enabled = art == null;
            name.text = GameData.Text("customer." + id);
        }
    }
}
