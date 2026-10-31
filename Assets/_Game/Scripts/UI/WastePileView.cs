using System;
using System.Collections.Generic;
using System.Linq;
using CopAtolyesi.Core;
using UnityEngine;

namespace CopAtolyesi.UI
{
    // Atiklar sandiga rastgele atilmis gibi durur. Esyanin yeri ve acisi bir kez secilir, sonra korunur.
    public sealed class WastePileView : MonoBehaviour
    {
        const int PlacementTries = 12;

        struct Placement
        {
            public Vector2 Anchor;
            public float Tilt;
        }

        UiContext context;
        RectTransform area;
        Func<IReadOnlyList<ItemInstance>> items;
        readonly Dictionary<ItemInstance, Placement> placements = new Dictionary<ItemInstance, Placement>();
        readonly Dictionary<ItemInstance, ItemView> views = new Dictionary<ItemInstance, ItemView>();

        public static WastePileView Create(RectTransform area, UiContext context, Func<IReadOnlyList<ItemInstance>> items)
        {
            var view = area.gameObject.AddComponent<WastePileView>();
            view.context = context;
            view.area = area;
            view.items = items;
            context.Session.Changed += view.Refresh;
            view.Refresh();
            return view;
        }

        void OnDestroy()
        {
            if (context != null) context.Session.Changed -= Refresh;
        }

        void Refresh()
        {
            var list = items();
            foreach (var gone in views.Keys.Except(list).ToList())
            {
                Destroy(views[gone].gameObject);
                views.Remove(gone);
            }
            foreach (var item in list)
            {
                if (views.TryGetValue(item, out var view)) view.Show(item);
                else views[item] = CreateView(item);
            }
        }

        ItemView CreateView(ItemInstance item)
        {
            if (!placements.TryGetValue(item, out var placement))
            {
                placement = PickPlacement();
                placements[item] = placement;
            }

            var view = ItemView.Create(area, context);
            view.Show(item);
            view.gameObject.AddComponent<ItemDragSource>().Init(context);

            var rect = (RectTransform)view.transform;
            rect.anchorMin = rect.anchorMax = placement.Anchor;
            rect.sizeDelta = Vector2.one * GameData.Config.pileItemSize;
            rect.anchoredPosition = Vector2.zero;
            rect.localEulerAngles = new Vector3(0, 0, placement.Tilt);
            return view;
        }

        // Birkac rastgele aday arasindan digerlerine en uzak olani secer: daginik ama gorunur kalir.
        Placement PickPlacement()
        {
            var size = GameData.Config.pileItemSize * 0.6f;
            var margin = new Vector2(Mathf.Clamp01(size / area.rect.width), Mathf.Clamp01(size / area.rect.height));
            var taken = views.Keys.Where(placements.ContainsKey).Select(i => placements[i].Anchor).ToList();

            var best = new Vector2(0.5f, 0.5f);
            var bestDistance = -1f;
            for (var i = 0; i < PlacementTries; i++)
            {
                var candidate = new Vector2(UnityEngine.Random.Range(margin.x, 1 - margin.x), UnityEngine.Random.Range(margin.y, 1 - margin.y));
                var distance = taken.Count == 0 ? 1f : taken.Min(t => (t - candidate).sqrMagnitude);
                if (distance <= bestDistance) continue;
                bestDistance = distance;
                best = candidate;
            }
            var maxTilt = GameData.Config.pileMaxTilt;
            return new Placement { Anchor = best, Tilt = UnityEngine.Random.Range(-maxTilt, maxTilt) };
        }
    }
}
