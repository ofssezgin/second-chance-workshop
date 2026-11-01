using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Atolye Defteri: oyun durmaz, panel olarak acilir. Her sayfa gorselli kartlardan olusur;
    // etiket haplari esya ipucuyla aynidir. Icerik gorulen seylerle dolar.
    public sealed class NotebookView : MonoBehaviour
    {
        enum Tab { Appointments, Tags, Recipes, Items, Transforms }

        const float PanelWidth = 1180f;
        const float PanelHeight = 664f;
        const float Pad = 16f;
        const float CardWidth = 566f;
        const float CardGap = 8f;
        const float FilterHeight = 40f;
        const float ItemCardHeight = 60f;
        const float RecipeCardHeight = 130f;
        const float TagCardHeight = 60f;
        const float TransformCardHeight = 84f;
        const float PillSize = 15f;
        const float RowHeight = 24f;
        const float SlotNameWidth = 84f;
        const float TurnSeconds = 0.09f;

        GameSession session;
        RectTransform page;
        RectTransform cards;
        RectTransform filters;
        GridLayoutGroup grid;
        readonly Dictionary<Tab, Image> tabImages = new Dictionary<Tab, Image>();
        Tab tab;
        string tagFilter;
        Coroutine turning;

        public static NotebookView Create(Transform parent, GameSession session)
        {
            var palette = GameData.Config.palette;
            var center = new Vector2(0.5f, 0.5f);
            var panel = UiKit.Panel(parent, "Notebook", center, center, GameData.ParseColor(palette.cream), true);
            panel.rectTransform.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var view = panel.gameObject.AddComponent<NotebookView>();
            view.session = session;

            var ink = GameData.ParseColor(palette.anthracite);
            var titleArea = UiKit.Area(panel.transform, "Title", new Vector2(0, 0.91f), new Vector2(0.2f, 1));
            UiKit.Label(titleArea, "Text", GameData.Text("ui.notebook.title"), 22, ink);

            view.AddTab(panel.transform, "ui.notebook.appointments", Tab.Appointments, 0.2f);
            view.AddTab(panel.transform, "ui.notebook.tags", Tab.Tags, 0.335f);
            view.AddTab(panel.transform, "ui.notebook.recipes", Tab.Recipes, 0.47f);
            view.AddTab(panel.transform, "ui.notebook.items", Tab.Items, 0.605f);
            view.AddTab(panel.transform, "ui.notebook.transforms", Tab.Transforms, 0.74f);

            var close = UiKit.Button(panel.transform, "Close", GameData.Text("ui.notebook.close"), new Vector2(0.885f, 0.92f), new Vector2(0.985f, 0.985f), out _);
            close.onClick.AddListener(view.Toggle);

            view.page = UiKit.Area(panel.transform, "Page", Vector2.zero, new Vector2(1, 0.91f));
            view.page.pivot = new Vector2(0, 0.5f);
            UiKit.Pad(view.page, Pad, Pad, Pad, 4);

            view.filters = UiKit.Area(view.page, "Filters", new Vector2(0, 1), Vector2.one);
            view.filters.offsetMin = new Vector2(0, -FilterHeight + 6);
            view.filters.offsetMax = Vector2.zero;

            view.cards = UiKit.Area(view.page, "Cards", Vector2.zero, Vector2.one);
            view.grid = view.cards.gameObject.AddComponent<GridLayoutGroup>();
            view.grid.spacing = Vector2.one * CardGap;
            view.grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            view.grid.constraintCount = 2;

            panel.gameObject.SetActive(false);
            return view;
        }

        void AddTab(Transform parent, string key, Tab target, float left)
        {
            var button = UiKit.Button(parent, "Tab", GameData.Text(key), new Vector2(left, 0.92f), new Vector2(left + 0.13f, 0.985f), out _);
            button.onClick.AddListener(() => TurnTo(target));
            tabImages[target] = button.GetComponent<Image>();
        }

        public bool IsOpen => gameObject.activeSelf;

        public void Toggle()
        {
            var open = !gameObject.activeSelf;
            gameObject.SetActive(open);
            if (!open) return;
            transform.SetAsLastSibling();
            page.localScale = Vector3.one;
            turning = null;
            Build();
        }

#if DEVELOPMENT_BUILD
        public void TurnForTest(int index) => TurnTo((Tab)index);
#endif

        void TurnTo(Tab target)
        {
            if (target == tab) return;
            if (turning != null) StopCoroutine(turning);
            turning = StartCoroutine(Turn(target));
        }

        // Sayfa sol kenari etrafinda kapanir, icerik degisir, yeni sayfa acilir.
        IEnumerator Turn(Tab target)
        {
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / TurnSeconds)
            {
                page.localScale = new Vector3(1f - t, 1, 1);
                yield return null;
            }
            tab = target;
            Build();
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / TurnSeconds)
            {
                page.localScale = new Vector3(t, 1, 1);
                yield return null;
            }
            page.localScale = Vector3.one;
            turning = null;
        }

        void Build()
        {
            var palette = GameData.Config.palette;
            foreach (var pair in tabImages) pair.Value.color = GameData.ParseColor(pair.Key == tab ? palette.mustard : palette.teal);

            Clear(cards);
            Clear(filters);
            var showFilters = tab == Tab.Items;
            filters.gameObject.SetActive(showFilters);
            cards.offsetMax = new Vector2(0, showFilters ? -FilterHeight : 0);

            if (tab == Tab.Appointments) BuildAppointments();
            else if (tab == Tab.Tags) BuildTags();
            else if (tab == Tab.Recipes) BuildRecipes();
            else if (tab == Tab.Items) BuildItems();
            else BuildTransforms();
        }

        static void Clear(RectTransform parent)
        {
            foreach (Transform child in parent)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        void SetCardHeight(float height) => grid.cellSize = new Vector2(CardWidth, height);

        RectTransform Card() =>
            UiKit.Panel(cards, "Card", Vector2.zero, Vector2.one, GameData.ParseColor(GameData.Config.palette.anthracite), true).rectTransform;

        static Image Icon(RectTransform card, string path, float x, float size)
        {
            var left = new Vector2(0, 0.5f);
            var image = UiKit.Panel(card, "Icon", left, left, Color.white);
            image.raycastTarget = false;
            image.rectTransform.pivot = left;
            image.rectTransform.sizeDelta = Vector2.one * size;
            image.rectTransform.anchoredPosition = new Vector2(x, 0);
            image.sprite = path != null ? Resources.Load<Sprite>(path) : null;
            image.preserveAspect = true;
            return image;
        }

        // Esya cizimleri acik zemin uzerinde durur: lastik gibi koyu esyalar kartta kaybolmaz.
        static void ItemIcon(RectTransform card, string path, float x, float size)
        {
            const float margin = 2f;
            var left = new Vector2(0, 0.5f);
            var backdrop = UiKit.Panel(card, "Backdrop", left, left, GameData.ParseColor(GameData.Config.palette.wall), true);
            backdrop.raycastTarget = false;
            backdrop.rectTransform.pivot = left;
            backdrop.rectTransform.sizeDelta = Vector2.one * (size + margin * 2);
            backdrop.rectTransform.anchoredPosition = new Vector2(x - margin, 0);
            Icon(card, path, x, size);
        }

        // Cizimi olmayan veya henuz bilinmeyen sey icin gri soru isareti.
        static void UnknownIcon(RectTransform card, float x, float size)
        {
            var image = Icon(card, null, x, size);
            image.sprite = UiKit.Rounded;
            image.type = Image.Type.Sliced;
            image.color = GameData.ParseColor(GameData.Config.palette.locked);
            UiKit.Label(image.transform, "Text", GameData.Text("ui.tool.unknown"), 24, GameData.ParseColor(GameData.Config.palette.cream));
        }

        static RectTransform Row(RectTransform card, float left, float top, float height)
        {
            var row = UiKit.Area(card, "Row", new Vector2(0, 1), Vector2.one);
            row.offsetMin = new Vector2(left, -top - height);
            row.offsetMax = new Vector2(-8, -top);
            return row;
        }

        static TMP_Text Title(RectTransform card, string text, float left, float top)
        {
            return UiKit.Label(Row(card, left, top, RowHeight), "Text", text, 18,
                GameData.ParseColor(GameData.Config.palette.cream), TextAlignmentOptions.MidlineLeft);
        }

        static RectTransform PillRow(RectTransform card, float left, float top)
        {
            var row = Row(card, left, top, RowHeight);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        // Tarif ve randevu kartlarinin ortak ozu: urun cizimi, baslik, her bolumun ihtiyaci.
        void ProductCard(string title, string product, string portrait)
        {
            var card = Card();
            var left = 8f;
            if (portrait != null)
            {
                Icon(card, portrait, left, 72);
                left += 76;
            }

            if (product != null) Icon(card, "Sprites/Products/" + product, left, 72);
            else UnknownIcon(card, left, 72);
            left += 80;

            Title(card, title, left, 6);
            if (product == null) return;
            var cream = GameData.ParseColor(GameData.Config.palette.cream);
            var top = 6 + RowHeight;
            foreach (var slot in GameData.Products[product].slots)
            {
                var row = PillRow(card, left, top);
                var name = UiKit.Label(row, "Slot", GameData.Text("slot." + slot.id), PillSize, cream, TextAlignmentOptions.MidlineLeft);
                name.gameObject.AddComponent<LayoutElement>().preferredWidth = SlotNameWidth;
                foreach (var tag in slot.requires ?? new string[0]) TooltipView.AddPill(row, TagName(tag), cream, PillSize);
                foreach (var material in slot.materials ?? new string[0])
                    TooltipView.AddPill(row, GameData.Text("material." + material), GameData.ParseColor(GameData.Materials[material].color), PillSize);
                top += RowHeight;
            }
        }

        // Randevulu musteriler adiyla ve urunun ihtiyaciyla yazar;
        // randevusuz gelecekler ve gizli siparisler soru isareti kalir.
        void BuildAppointments()
        {
            SetCardHeight(RecipeCardHeight);
            var day = session.Day;
            var orders = (day != null ? day.orders : new OrderDef[0]).ToList();
            var walkIns = day != null ? day.walkIns ?? new OrderDef[0] : new OrderDef[0];
            foreach (var walkIn in walkIns) orders.Insert(System.Math.Min(walkIn.queueIndex, orders.Count), walkIn);

            foreach (var order in orders)
            {
                if (walkIns.Contains(order))
                {
                    ProductCard(GameData.Text("ui.notebook.appointment_unknown"), null, null);
                    continue;
                }
                var title = GameData.Text("ui.intro.order", ("customer", GameData.Text("customer." + order.customer)),
                    ("product", order.hidden ? "?" : GameData.Text("product." + order.product)));
                ProductCard(title, order.hidden ? null : order.product, GameData.Customers[order.customer].portrait);
            }
            if (orders.Count == 0) Message("ui.notebook.none");
        }

        void Message(string key)
        {
            var card = Card();
            Title(card, GameData.Text(key), 12, 8);
        }

        void BuildTags()
        {
            SetCardHeight(TagCardHeight);
            var cream = GameData.ParseColor(GameData.Config.palette.cream);
            foreach (var id in GameData.Tags.Keys)
            {
                var card = Card();
                TooltipView.AddPill(PillRow(card, 10, 18), TagName(id), cream, PillSize + 1);
                var desc = UiKit.Label(Row(card, 120, 6, 48), "Text", GameData.Text($"tag.{id}.desc"), 16, cream, TextAlignmentOptions.MidlineLeft);
                desc.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        void BuildRecipes()
        {
            SetCardHeight(RecipeCardHeight);
            foreach (var id in session.SeenProducts)
                ProductCard($"<b>{GameData.Text("product." + id)}</b> - {GameData.Text($"product.{id}.purpose")}", id, null);
            if (session.SeenProducts.Count == 0) Message("ui.notebook.none");
        }

        // Gorulen esyalar ve acik aletlerle elde edilebilenler; etiket secilirse yalniz onu tasiyanlar.
        void BuildItems()
        {
            AddFilters();
            SetCardHeight(ItemCardHeight);
            var known = session.SeenItems.ToList();
            foreach (var id in session.SeenItems)
                foreach (var transform in GameData.Items[id].transforms ?? new TransformDef[0])
                    if (session.IsStationUnlocked(GameData.Stations[transform.station]) && !known.Contains(transform.into))
                        known.Add(transform.into);

            foreach (var item in known.Select(id => GameData.Items[id]).Where(i => tagFilter == null || i.tags.Contains(tagFilter)))
            {
                var card = Card();
                ItemIcon(card, item.sprite, 8, 48);
                Title(card, GameData.Text("item." + item.id), 64, 4);
                TooltipView.AddItemPills(PillRow(card, 64, 30), item, PillSize);
            }
        }

        void AddFilters()
        {
            var ids = new List<string> { null };
            ids.AddRange(GameData.Tags.Keys);
            var width = 1f / ids.Count;
            var palette = GameData.Config.palette;
            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                var text = id == null ? GameData.Text("ui.notebook.all") : TagName(id);
                var button = UiKit.Button(filters, "Filter", text, new Vector2(i * width, 0), new Vector2((i + 1) * width - 0.003f, 1), out var label);
                label.fontSize = 14;
                button.GetComponent<Image>().color = GameData.ParseColor(id == tagFilter ? palette.mustard : palette.teal);
                button.onClick.AddListener(() =>
                {
                    tagFilter = id;
                    Build();
                });
            }
        }

        // Donusumler: giren esya, alet, cikan esya ve ozellikleri. Kilitli aletinkiler gri kalir,
        // alet acilinca gorunur olur.
        void BuildTransforms()
        {
            SetCardHeight(TransformCardHeight);
            foreach (var station in GameData.Stations.Values)
            {
                var open = session.IsStationUnlocked(station);
                var tool = "Sprites/Tools/" + station.id;
                foreach (var item in GameData.Items.Values)
                    foreach (var transform in (item.transforms ?? new TransformDef[0]).Where(t => t.station == station.id))
                    {
                        if (open) TransformCard(item, tool, transform);
                        else LockedTransformCard();
                    }
                if (!open || !station.bench) continue;

                var card = Card();
                Icon(card, tool, 8, 56);
                Title(card, GameData.Text("tool." + station.id), 76, 6);
                var note = UiKit.Label(Row(card, 76, 30, 48), "Text", GameData.Text("ui.notebook.bench_line"), 15,
                    GameData.ParseColor(GameData.Config.palette.cream), TextAlignmentOptions.TopLeft);
                note.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        void LockedTransformCard()
        {
            var card = Card();
            UnknownIcon(card, 8, 56);
            UnknownIcon(card, 68, 44);
            UnknownIcon(card, 114, 56);
            Title(card, GameData.Text("ui.tool.unknown"), 174, 28);
        }

        void TransformCard(ItemDef from, string tool, TransformDef transform)
        {
            var result = GameData.Items[transform.into];
            var card = Card();
            ItemIcon(card, from.sprite, 8, 56);
            Icon(card, tool, 68, 44);
            ItemIcon(card, result.sprite, 114, 56);
            Title(card, $"{GameData.Text("item." + from.id)} > {transform.count} x {GameData.Text("item." + result.id)}", 174, 10);
            TooltipView.AddItemPills(PillRow(card, 174, 42), result, PillSize);
        }

        static string TagName(string id) => GameData.Text("tag." + id);
    }
}
