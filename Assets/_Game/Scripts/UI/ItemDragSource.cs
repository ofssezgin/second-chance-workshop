using System;
using CopAtolyesi.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CopAtolyesi.UI
{
    // Esyayi surukler. Birakma hedefi kabul ederse Accept cagirir; kabul edilmezse DropMissed calisir.
    [RequireComponent(typeof(ItemView))]
    public sealed class ItemDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        const float DraggedAlpha = 0.35f;

        ItemView view;
        CanvasGroup group;
        UiContext context;
        RectTransform ghost;
        bool accepted;

        public static ItemDragSource Current { get; private set; }
        public ItemInstance DraggedItem { get; private set; }
        public Action<ItemInstance> DropMissed;

        public void Init(UiContext context)
        {
            this.context = context;
            view = GetComponent<ItemView>();
            group = gameObject.AddComponent<CanvasGroup>();
        }

        public void Accept() => accepted = true;

        public void OnBeginDrag(PointerEventData eventData)
        {
            DraggedItem = view.Item;
            Current = this;
            accepted = false;
            view.EndHover();
            group.alpha = DraggedAlpha;
            group.blocksRaycasts = false;

            var ghostView = ItemView.Create(context.Overlay);
            ghostView.Show(DraggedItem);
            ghostView.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            ghost = (RectTransform)ghostView.transform;
            ghost.anchorMin = ghost.anchorMax = new Vector2(0.5f, 0.5f);
            ghost.sizeDelta = Vector2.one * GameData.Config.pileItemSize;
            ghost.position = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ghost != null) ghost.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var item = DraggedItem;
            EndDrag();
            if (!accepted && item != null) DropMissed?.Invoke(item);
        }

        // Birakma listeyi yenileyip bu nesneyi kapatabilir; o durumda OnEndDrag hic gelmez.
        void OnDisable() => EndDrag();

        void EndDrag()
        {
            DraggedItem = null;
            if (Current == this) Current = null;
            if (group != null)
            {
                group.alpha = 1;
                group.blocksRaycasts = true;
            }
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = null;
        }
    }
}
