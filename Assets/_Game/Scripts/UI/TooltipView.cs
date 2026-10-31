using System.Linq;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Esyanin adini, malzemesini, etiket haplarini ve kirliyse gereken bakim islerini gosterir.
    public sealed class TooltipView : MonoBehaviour
    {
        const float Margin = 8f;
        const float BodyWidth = 320f;

        RectTransform rect;
        RectTransform pills;
        TMP_Text title;
        TMP_Text craftLabel;
        RectTransform craftPills;

        public bool ShowsCraft => craftLabel.gameObject.activeSelf;

        public static TooltipView Create(Transform parent)
        {
            var palette = GameData.Config.palette;
            var center = new Vector2(0.5f, 0.5f);
            var panel = UiKit.Panel(parent, "Tooltip", center, center, GameData.ParseColor(palette.anthracite), true);
            panel.raycastTarget = false;

            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 10);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var view = panel.gameObject.AddComponent<TooltipView>();
            view.rect = panel.rectTransform;
            view.title = UiKit.Label(panel.transform, "Title", string.Empty, 20, GameData.ParseColor(palette.cream), TextAlignmentOptions.Left);
            view.pills = UiKit.Area(panel.transform, "Pills", Vector2.zero, Vector2.one);
            var row = view.pills.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 6;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            view.craftLabel = UiKit.Label(panel.transform, "CraftTitle", string.Empty, 18, GameData.ParseColor(palette.mustard), TextAlignmentOptions.Left);
            view.craftPills = UiKit.Area(panel.transform, "CraftPills", Vector2.zero, Vector2.one);
            var craftRow = view.craftPills.gameObject.AddComponent<HorizontalLayoutGroup>();
            craftRow.spacing = 6;
            craftRow.childControlWidth = true;
            craftRow.childControlHeight = true;
            craftRow.childForceExpandWidth = false;
            craftRow.childForceExpandHeight = false;

            panel.gameObject.SetActive(false);
            return view;
        }

        static void Clear(RectTransform parent)
        {
            foreach (Transform child in parent)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        void SetCraftVisible(bool visible)
        {
            craftLabel.gameObject.SetActive(visible);
            craftPills.gameObject.SetActive(visible);
        }

        // Ikinci satir: esya acik bir aletle donusebiliyorsa sonucun ozellikleri.
        void ShowCraft(ItemInstance item, GameSession session)
        {
            var transform = (item.Def.transforms ?? new TransformDef[0])
                .FirstOrDefault(t => item.Product == null && session.IsStationUnlocked(GameData.Stations[t.station]));
            SetCraftVisible(transform != null);
            if (transform == null) return;

            var result = GameData.Items[transform.into];
            craftLabel.text = GameData.Text("ui.tooltip.craftable", ("tool", GameData.Text("tool." + transform.station)),
                ("count", transform.count), ("item", GameData.Text("item." + result.id)));
            Clear(craftPills);
            AddItemPills(craftPills, result, 18);
        }

        public void Show(ItemInstance item, RectTransform target, GameSession session)
        {
            gameObject.SetActive(true);
            title.text = item.DisplayName;
            Clear(pills);

            AddItemPills(pills, item.Def, 18);

            if (!item.IsClean)
            {
                var tile = GameData.ParseColor(GameData.Config.palette.tile);
                foreach (var treatment in item.Treatments) AddPill(pills, GameData.Text("care." + treatment.id), tile, 18);
            }

            ShowCraft(item, session);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Place(target);
        }

        // Esya disindaki seyler icin: baslik ve aciklama (alet, su gostergesi).
        public void ShowText(string heading, string body, RectTransform target)
        {
            gameObject.SetActive(true);
            title.text = heading;
            Clear(pills);
            SetCraftVisible(false);
            var label = UiKit.Label(pills, "Body", body, 18, GameData.ParseColor(GameData.Config.palette.cream), TextAlignmentOptions.Left);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = BodyWidth;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Place(target);
        }

        public void Hide() => gameObject.SetActive(false);

        // Malzeme ve etiket haplari: ipucu ile defter ayni gorunumu paylasir.
        public static void AddItemPills(RectTransform parent, ItemDef item, float size)
        {
            var material = GameData.Materials[item.material];
            AddPill(parent, GameData.Text("material." + material.id), GameData.ParseColor(material.color), size);
            var cream = GameData.ParseColor(GameData.Config.palette.cream);
            foreach (var tag in item.tags) AddPill(parent, GameData.Text("tag." + tag), cream, size);
        }

        public static void AddPill(RectTransform parent, string text, Color color, float size)
        {
            var pill = UiKit.Panel(parent, "Pill", Vector2.zero, Vector2.one, color, true);
            var layout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 3, 3);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var label = UiKit.Label(pill.transform, "Text", text, size, Color.black);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        // Esyanin ustune, yer yoksa altina acar ve ekrandan tasmasini engeller.
        void Place(RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var above = corners[1].y < Screen.height * 0.5f;
            var scale = rect.lossyScale.x;
            var width = rect.rect.width * scale;

            rect.pivot = new Vector2(0.5f, above ? 0 : 1);
            var x = Mathf.Clamp((corners[0].x + corners[2].x) / 2, width / 2 + Margin, Screen.width - width / 2 - Margin);
            var y = above ? corners[1].y + Margin : corners[0].y - Margin;
            rect.position = new Vector3(x, y, 0);
        }
    }
}
