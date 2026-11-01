using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Kesme aleti (makas, testere): masanin saginda durur, basilinca ele gecer ve masadaki esyada kullanilir.
    // Eldeyken yerinde soluk izi kalir ve renklenir; kilitliyse gri siluet kalir.
    public sealed class WorkshopStation : MonoBehaviour
    {
        const float Size = 128f;
        const float Gap = 8f;
        const float HeldAlpha = 0.55f;
        const float HeldIconAlpha = 0.25f;
        static readonly Color IdleColor = new Color(0, 0, 0, 0.22f);

        UiContext context;
        StationDef def;
        Image panel;
        Image icon;
        Sprite art;
        TMP_Text label;

        public bool Highlighted { get; private set; }

        public static RectTransform Create(RectTransform parent, UiContext context, StationDef def, int index)
        {
            var corner = new Vector2(1, 1);
            var panel = UiKit.Panel(parent, "Station_" + def.id, corner, corner, IdleColor, true);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = Vector2.one * Size;
            panel.rectTransform.anchoredPosition = new Vector2(-Size / 2, -Size / 2 - index * (Size + Gap));

            var view = panel.gameObject.AddComponent<WorkshopStation>();
            view.context = context;
            view.def = def;
            view.panel = panel;
            panel.gameObject.AddComponent<ToolPickup>().Pressed = () => context.Wash.OnStationPressed(def);

            var center = new Vector2(0.5f, 0.6f);
            view.icon = UiKit.Panel(panel.transform, "Icon", center, center, Color.white);
            view.icon.raycastTarget = false;
            view.icon.rectTransform.sizeDelta = Vector2.one * 84;
            view.art = Resources.Load<Sprite>("Sprites/Tools/" + def.id);

            var labelArea = UiKit.Area(panel.transform, "Label", Vector2.zero, new Vector2(1, 0.26f));
            view.label = UiKit.Label(labelArea, "Text", string.Empty, 18, GameData.ParseColor(GameData.Config.palette.cream));

            HoverHint.Attach(panel.gameObject, context, () => context.Session.IsStationUnlocked(def)
                ? (GameData.Text("tool." + def.id), GameData.Text($"tool.{def.id}.desc"))
                : (GameData.Text("ui.tool.unknown"), GameData.Text("tool.locked.desc")));

            context.Session.Changed += view.Refresh;
            view.Refresh();
            return panel.rectTransform;
        }

        void OnDestroy()
        {
            if (context != null) context.Session.Changed -= Refresh;
        }

        void Refresh()
        {
            var open = context.Session.IsStationUnlocked(def);
            icon.sprite = open ? art : Silhouette.Of(art);
            icon.color = open ? Color.white : Color.Lerp(GameData.ParseColor(GameData.Config.palette.locked), Color.black, 0.35f);
            label.text = GameData.Text(open ? "tool." + def.id : "ui.tool.unknown");
        }

        void Update()
        {
            Highlighted = context.Wash.SelectedStation == def.id;
            var held = GameData.ParseColor(GameData.Config.palette.mustard);
            held.a = HeldAlpha;
            panel.color = Highlighted ? held : IdleColor;
            if (!context.Session.IsStationUnlocked(def)) return;
            icon.color = new Color(1, 1, 1, Highlighted ? HeldIconAlpha : 1);
        }
    }

    // Musterinin konusmasinin yanindaki teslim alani: urun buraya birakilir.
    public sealed class DeliveryZone : MonoBehaviour, IDropHandler
    {
        UiContext context;
        TMP_Text label;

        public static DeliveryZone Create(RectTransform parent, UiContext context)
        {
            var panel = UiKit.Panel(parent, "Delivery", Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.1f), true);
            var view = panel.gameObject.AddComponent<DeliveryZone>();
            view.context = context;
            view.label = UiKit.Label(panel.transform, "Text", GameData.Text("ui.delivery"), 18, GameData.ParseColor(GameData.Config.palette.anthracite));
            view.label.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Pad(view.label.rectTransform, 12, 12, 12, 12);
            return view;
        }

        public void SetLabelVisible(bool visible) => label.enabled = visible;

        public void OnDrop(PointerEventData eventData)
        {
            var source = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<ItemDragSource>() : null;
            if (source == null || source.DraggedItem == null) return;
            source.Accept();
            var reason = context.Session.Deliver(source.DraggedItem);
            if (reason == null) return;
            AudioKit.Play("error");
            context.Toast.Show(GameData.Text(reason));
        }
    }
}
