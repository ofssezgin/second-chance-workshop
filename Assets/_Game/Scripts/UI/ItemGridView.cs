using System;
using System.Collections.Generic;
using CopAtolyesi.Core;
using UnityEngine;

namespace CopAtolyesi.UI
{
    // Bir esya listesini izgarada gosterir. Raf iki odada ayni listeye bagli iki ayri izgaradir.
    public sealed class ItemGridView : MonoBehaviour
    {
        const float Gap = 6f;

        GameSession session;
        Func<IReadOnlyList<ItemInstance>> items;
        ItemView[] cells;

        public static ItemGridView Create(RectTransform parent, UiContext context,
            Func<IReadOnlyList<ItemInstance>> items, int capacity, int columns, bool draggable = false)
        {
            var view = parent.gameObject.AddComponent<ItemGridView>();
            view.session = context.Session;
            view.items = items;
            view.cells = new ItemView[capacity];

            var rows = Mathf.CeilToInt(capacity / (float)columns);
            var slotColor = GameData.ParseColor(GameData.Config.palette.slot);
            for (var i = 0; i < capacity; i++)
            {
                var column = i % columns;
                var row = i / columns;
                var min = new Vector2(column / (float)columns, 1f - (row + 1f) / rows);
                var max = new Vector2((column + 1f) / columns, 1f - row / (float)rows);
                var slot = UiKit.Panel(parent, "Slot" + i, min, max, slotColor, true);
                UiKit.Pad(slot.rectTransform, Gap / 2, Gap / 2, Gap / 2, Gap / 2);
                view.cells[i] = ItemView.Create(slot.transform, context);
                if (draggable) view.cells[i].gameObject.AddComponent<ItemDragSource>().Init(context);
            }

            view.session.Changed += view.Refresh;
            view.Refresh();
            return view;
        }

        public RectTransform CellRect(int index) => (RectTransform)cells[index].transform.parent;

        void OnDestroy()
        {
            if (session != null) session.Changed -= Refresh;
        }

        void Refresh()
        {
            var list = items();
            for (var i = 0; i < cells.Length; i++)
            {
                var filled = i < list.Count;
                cells[i].gameObject.SetActive(filled);
                if (filled) cells[i].Show(list[i]);
            }
        }
    }
}
