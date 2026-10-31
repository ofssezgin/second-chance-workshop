using System.Collections;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Sprite varsa onu, yoksa malzeme renginde yedek kutuyu gosterir (GDD 7, yedek plan).
    // Izler cizimin icinde kalsin diye maskeli cizimin altina konur.
    public sealed class ItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        const float ProductTintStrength = 0.55f;

        Image background;
        Image sprite;
        RectTransform marksRoot;
        ItemInstance marksFor;
        TMP_Text label;
        UiContext context;
        Coroutine hover;

        public ItemInstance Item { get; private set; }
        public RectTransform MarksRoot => marksRoot;

        // context verilmezse esya uzerine gelmeye tepki vermez (surukleme ve lavabo gorunumu).
        public static ItemView Create(Transform parent, UiContext context = null)
        {
            var image = UiKit.Panel(parent, "Item", Vector2.zero, Vector2.one, Color.white, true);
            var view = image.gameObject.AddComponent<ItemView>();
            view.background = image;
            view.context = context;

            view.sprite = UiKit.Panel(image.transform, "Sprite", Vector2.zero, Vector2.one, Color.white);
            view.sprite.raycastTarget = false;
            view.sprite.gameObject.AddComponent<Mask>();

            view.label = UiKit.Label(image.transform, "Name", string.Empty, 18, Color.black);
            view.label.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Pad(view.label.rectTransform, 4, 4, 4, 4);

            view.marksRoot = UiKit.Area(view.sprite.transform, "Marks", Vector2.zero, Vector2.one);
            return view;
        }

        public void Show(ItemInstance item, bool showMarks = true)
        {
            if (Item != item) EndHover();
            Item = item;

            var art = Resources.Load<Sprite>(item.Def.sprite);
            var color = GameData.Materials.TryGetValue(item.Def.material ?? string.Empty, out var material)
                ? GameData.ParseColor(material.color) : Color.white;
            var tint = item.IsClean ? Color.white : Color.Lerp(Color.white, Color.black, GameData.Config.dirtyTint);
            if (item.Product != null) tint = Color.Lerp(Color.white, color, ProductTintStrength);

            background.color = art == null ? color * tint : Color.clear;
            sprite.enabled = art != null;
            sprite.sprite = art;
            sprite.color = tint;
            label.enabled = art == null;
            label.text = item.DisplayName;
            FitArt();

            var wanted = showMarks && !item.IsClean ? item : null;
            if (marksFor == wanted) return;
            marksFor = wanted;
            foreach (Transform child in marksRoot) Destroy(child.gameObject);
            if (wanted != null) CareMarks.Build(marksRoot, wanted);
        }

        void OnRectTransformDimensionsChange() => FitArt();

        // Pixel art bozulmasin diye cizim alana sigan en buyuk tam sayi katiyla buyutulur.
        void FitArt()
        {
            if (sprite == null || sprite.sprite == null) return;
            var box = ((RectTransform)transform).rect.size;
            var pixels = sprite.sprite.rect.size;
            var scale = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(box.x / pixels.x, box.y / pixels.y)));
            var rect = sprite.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = pixels * scale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (context == null || eventData.dragging) return;
            transform.localScale = Vector3.one * GameData.Config.hoverScale;
            hover = StartCoroutine(ShowTooltipAfterDelay());
        }

        public void OnPointerExit(PointerEventData eventData) => EndHover();

        void OnDisable() => EndHover();

        public void EndHover()
        {
            if (context == null) return;
            transform.localScale = Vector3.one;
            if (hover != null) StopCoroutine(hover);
            hover = null;
            if (context.Tooltip != null) context.Tooltip.Hide();
        }

        IEnumerator ShowTooltipAfterDelay()
        {
            yield return new WaitForSecondsRealtime(GameData.Config.hoverSeconds);
            if (Item == null) yield break;
            context.Tooltip.Show(Item, (RectTransform)transform, context.Session);
            context.Session.Signal("item_hovered");
        }
    }
}
