using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Rafin yanindaki kucuk hatirlatma: bakilan musterinin resmi ve yaninda kisa istek ozeti.
    public sealed class OrderReminderView : MonoBehaviour
    {
        const float PortraitWidth = 64f;
        const float PortraitHeight = 72f;
        const float Inset = 8f;

        GameSession session;
        GameObject root;
        Image portrait;
        TMP_Text portraitName;
        TMP_Text summary;

        public static OrderReminderView Create(RectTransform area, GameSession session)
        {
            var palette = GameData.Config.palette;
            var panel = UiKit.Panel(area, "OrderReminder", Vector2.zero, Vector2.one, GameData.ParseColor(palette.cream), true);
            panel.raycastTarget = false;

            var view = area.gameObject.AddComponent<OrderReminderView>();
            view.session = session;
            view.root = panel.gameObject;

            var left = new Vector2(0, 0.5f);
            view.portrait = UiKit.Panel(panel.transform, "Portrait", left, left, Color.white, true);
            view.portrait.raycastTarget = false;
            view.portrait.rectTransform.pivot = left;
            view.portrait.rectTransform.sizeDelta = new Vector2(PortraitWidth, PortraitHeight);
            view.portrait.rectTransform.anchoredPosition = new Vector2(Inset, 0);
            view.portraitName = UiKit.Label(view.portrait.transform, "Name", string.Empty, 18, GameData.ParseColor(palette.cream));
            view.portraitName.textWrappingMode = TextWrappingModes.Normal;
            view.portraitName.overflowMode = TextOverflowModes.Truncate;

            view.summary = UiKit.Label(panel.transform, "Summary", string.Empty, 18, GameData.ParseColor(palette.anthracite), TextAlignmentOptions.MidlineLeft);
            view.summary.textWrappingMode = TextWrappingModes.Normal;
            view.summary.rectTransform.offsetMin = new Vector2(PortraitWidth + Inset * 2, 4);
            view.summary.rectTransform.offsetMax = new Vector2(-Inset, -4);

            session.Changed += view.Refresh;
            view.Refresh();
            return view;
        }

        void OnDestroy()
        {
            if (session != null) session.Changed -= Refresh;
        }

        void Refresh()
        {
            var order = session.CurrentOrder;
            root.SetActive(order != null);
            if (order == null) return;

            if (!session.IsCurrentOrderSeen)
            {
                portrait.sprite = UiKit.Rounded;
                portrait.type = Image.Type.Sliced;
                portrait.color = GameData.ParseColor(GameData.Config.palette.locked);
                portraitName.enabled = true;
                portraitName.text = GameData.Text("ui.tool.unknown");
                summary.text = GameData.Text("ui.reminder.unseen");
                return;
            }

            CustomerView.ApplyPortrait(portrait, portraitName, order.customer, false);
            summary.text = order.hidden ? GameData.Text(order.textKey) : GameData.Text("ui.reminder.order",
                ("product", GameData.Text("product." + order.product)), ("pref", PrefSummary(order)));
        }

        public static string PrefValue(OrderDef order)
        {
            var pref = order.pref;
            return GameData.Text((pref.type.StartsWith("tag") ? "tag." : "material.") + pref.value);
        }

        static string PrefSummary(OrderDef order)
        {
            if (!order.HasPref) return string.Empty;
            var key = order.pref.type.EndsWith("dislike") ? "ui.reminder.pref_dislike" : "ui.reminder.pref_like";
            return GameData.Text(key, ("value", PrefValue(order)));
        }
    }
}
