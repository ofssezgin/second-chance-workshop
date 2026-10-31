using System.Collections;
using System.Linq;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Tarif karti: slotlara raftan esya birakilir, Birlestir urunu yapar. Teslim dukkandaki alana yapilir.
    public sealed class CraftTableView : MonoBehaviour
    {
        const float ShakeSeconds = 0.3f;

        UiContext context;
        GameSession session;
        TMP_Text header;
        RectTransform slotsRoot;
        GameObject buttons;
        GameObject makeButtonObject;
        GameObject clearButtonObject;
        Button postponeButton;
        RectTransform[] slotBoxes = new RectTransform[0];
        RectTransform makeButton;

        public static CraftTableView Create(RectTransform content, UiContext context)
        {
            var view = content.gameObject.AddComponent<CraftTableView>();
            view.context = context;
            view.session = context.Session;

            var cream = GameData.ParseColor(GameData.Config.palette.cream);
            var headerArea = UiKit.Area(content, "Header", new Vector2(0, 0.88f), Vector2.one);
            view.header = UiKit.Label(headerArea, "Text", string.Empty, 20, cream);
            view.header.textWrappingMode = TextWrappingModes.Normal;
            view.slotsRoot = UiKit.Area(content, "Slots", new Vector2(0, 0.17f), new Vector2(1, 0.87f));

            var row = UiKit.Area(content, "Buttons", Vector2.zero, new Vector2(1, 0.15f));
            view.buttons = row.gameObject;
            var make = UiKit.Button(row, "Make", GameData.Text("ui.craft.make"), Vector2.zero, new Vector2(0.33f, 1), out _);
            make.onClick.AddListener(view.Make);
            view.makeButton = make.GetComponent<RectTransform>();
            view.makeButtonObject = make.gameObject;
            var clear = UiKit.Button(row, "Clear", GameData.Text("ui.craft.clear"), new Vector2(0.34f, 0), new Vector2(0.66f, 1), out _);
            clear.onClick.AddListener(view.session.ClearPlacements);
            view.clearButtonObject = clear.gameObject;
            view.postponeButton = UiKit.Button(row, "Later", GameData.Text("ui.craft.later"), new Vector2(0.67f, 0), Vector2.one, out _);
            view.postponeButton.onClick.AddListener(view.session.Postpone);

            view.session.Changed += view.Refresh;
            view.Refresh();
            return view;
        }

        void OnDestroy()
        {
            if (session != null) session.Changed -= Refresh;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame && session.Placements.Count > 0) session.ClearPlacements();
        }

        void Make()
        {
            if (session.CurrentOrder == null) return;
            var order = session.CurrentOrder;
            var result = session.Craft();
            if (result.Success)
            {
                AudioKit.Play("success");
                return;
            }

            // Gizli sipariste hangi ozelligin gerektigi soylenmez: tarif Defter'den bulunur.
            AudioKit.Play("error");
            var revealsNeed = result.Failure != CraftFailure.SlotEmpty;
            context.Toast.Show(order.hidden && revealsNeed ? GameData.Text("feedback.hidden_wrong") : result.Feedback());
            var index = System.Array.IndexOf(GameData.Products[order.product].slots, result.Slot);
            if (index >= 0 && index < slotBoxes.Length) StartCoroutine(ShakeSlot(slotBoxes[index]));
        }

        // Hatali slot sallanir ve kisa bir an kirmiziya doner.
        IEnumerator ShakeSlot(RectTransform box)
        {
            var image = box.GetComponent<Image>();
            var color = image.color;
            image.color = GameData.ParseColor(GameData.Config.palette.tile);
            var origin = box.anchoredPosition;
            for (var t = 0f; t < ShakeSeconds && box != null; t += Time.unscaledDeltaTime)
            {
                box.anchoredPosition = origin + Vector2.right * Mathf.Sin(t * 50f) * 6f;
                yield return null;
            }
            if (box == null) yield break;
            box.anchoredPosition = origin;
            image.color = color;
        }

        void Refresh()
        {
            foreach (Transform child in slotsRoot) Destroy(child.gameObject);

            var order = session.CurrentOrder;
            buttons.SetActive(order != null);
            if (order == null)
            {
                header.text = GameData.Text("ui.craft.empty");
                slotBoxes = new RectTransform[0];
                return;
            }

            postponeButton.interactable = session.CanPostpone;
            var product = GameData.Products[order.product];
            makeButtonObject.SetActive(!product.workbench);
            clearButtonObject.SetActive(!product.workbench);
            if (product.workbench)
            {
                header.text = GameData.Text("ui.craft.workbench");
                slotBoxes = new RectTransform[0];
                return;
            }

            header.text = GameData.Text(order.hidden ? "ui.craft.hidden" : "product." + order.product);
            slotBoxes = new RectTransform[product.slots.Length];
            for (var i = 0; i < product.slots.Length; i++) slotBoxes[i] = BuildSlot(product.slots[i], i, product.slots.Length, order.hidden);
        }

        // Gizli sipariste bolge adlari gorunur ama ne istedigi yazmaz: tarif Defter'den bulunur.
        RectTransform BuildSlot(SlotDef slot, int index, int count, bool hidden)
        {
            const float gap = 0.015f;
            var cream = GameData.ParseColor(GameData.Config.palette.cream);
            var column = UiKit.Area(slotsRoot, "Slot_" + slot.id,
                new Vector2(index / (float)count + gap, 0), new Vector2((index + 1f) / count - gap, 1));

            var nameArea = UiKit.Area(column, "Name", new Vector2(0, 0.86f), Vector2.one);
            UiKit.Label(nameArea, "Text", GameData.Text("slot." + slot.id), 18, cream);

            var box = UiKit.Panel(column, "Box", new Vector2(0, 0.36f), new Vector2(1, 0.85f), GameData.ParseColor(GameData.Config.palette.slot), true);
            box.gameObject.AddComponent<CraftSlotDrop>().Init(session, slot.id);

            session.Placements.TryGetValue(slot.id, out var placed);
            if (placed != null)
            {
                var view = ItemView.Create(box.transform, context);
                view.Show(placed);
                var drag = view.gameObject.AddComponent<ItemDragSource>();
                drag.Init(context);
                drag.DropMissed = session.Unplace;
            }

            if (hidden)
            {
                BuildPill(column, null, false, 0);
                return box.rectTransform;
            }

            var needs = (slot.requires ?? new string[0]).Select(t => ("tag." + t, placed != null && placed.HasTag(t)))
                .Concat((slot.materials ?? new string[0]).Select(m => ("material." + m, placed != null && placed.Def.material == m)))
                .ToList();
            for (var i = 0; i < needs.Count; i++) BuildPill(column, needs[i].Item1, needs[i].Item2, i);
            return box.rectTransform;
        }

        // Saglanan gereksinim yesil, saglanmayan krem gorunur; yazi her zaman durur.
        static void BuildPill(RectTransform column, string key, bool met, int row)
        {
            const float height = 0.16f;
            var top = 0.34f - row * (height + 0.01f);
            var palette = GameData.Config.palette;
            var pill = UiKit.Panel(column, "Pill", new Vector2(0, top - height), new Vector2(1, top),
                GameData.ParseColor(met ? palette.leaf : palette.cream), true);
            UiKit.Label(pill.transform, "Text", key != null ? GameData.Text(key) : "?", 18, Color.black);
        }

        // Otomatik test icin ekran konumlari.
        public Vector2 SlotPoint(int index) => slotBoxes[index].position;
        public Vector2 MakeButtonPoint => makeButton.position + new Vector3(40, 16, 0);
    }

    public sealed class CraftSlotDrop : MonoBehaviour, IDropHandler
    {
        GameSession session;
        string slotId;

        public void Init(GameSession session, string slotId)
        {
            this.session = session;
            this.slotId = slotId;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var source = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<ItemDragSource>() : null;
            if (source == null || source.DraggedItem == null || source.DraggedItem.Product != null) return;
            source.Accept();
            session.Place(slotId, source.DraggedItem);
        }
    }
}
