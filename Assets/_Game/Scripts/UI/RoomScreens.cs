using System.Linq;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;

namespace CopAtolyesi.UI
{
    // Iki odanin yerlesimi. Oranlar config.json'dadir: shopCustomerRatio, cleaningCounterRatio.
    public static class RoomScreens
    {
        const float AreaPadding = 12f;

        public static void BuildBackRoom(RectTransform room, UiContext context)
        {
            var config = GameData.Config;
            var session = context.Session;
            var counterTop = config.cleaningCounterRatio;

            var top = UiKit.Panel(room, "ShelfArea", new Vector2(0, counterTop), Vector2.one, Color(p => p.wall));
            UiKit.ApplyArt(top, "Sprites/Rooms/back_wall");
            var shelf = UiKit.Area(top.transform, "ShelfGrid", Vector2.zero, new Vector2(config.cleaningShelfWidthRatio, 1));
            UiKit.Pad(shelf, AreaPadding, AreaPadding, AreaPadding, config.hudHeight + 4);
            var reminder = UiKit.Area(top.transform, "Reminder", new Vector2(config.cleaningShelfWidthRatio, 0), Vector2.one);
            UiKit.Pad(reminder, 0, AreaPadding + 3, AreaPadding + 3, config.hudHeight + 7);
            context.Reminder = OrderReminderView.Create(reminder, session);
            context.BackShelf = ItemGridView.Create(shelf, context, () => session.AvailableShelf, config.shelfCapacity, config.cleaningShelfColumns, true);
            context.Targets["shelf_back"] = shelf;

            var counter = UiKit.Panel(room, "Counter", Vector2.zero, new Vector2(1, counterTop), Color(p => p.counter));
            UiKit.ApplyArt(counter, "Sprites/Rooms/back_counter");

            var crate = Station(counter.transform, "Crate", "ui.area.crate", Vector2.zero, new Vector2(config.crateWidthRatio, 1));
            WastePileView.Create(crate, context, () => session.Waste.Where(i => i != session.Washing).ToList());
            context.Targets["crate"] = crate;

            var sink = Station(counter.transform, "Sink", "ui.area.sink", new Vector2(config.crateWidthRatio, 0), Vector2.one);
            context.Wash = WashStation.Create(sink, context);
            context.Targets["sink"] = sink;
            var index = 0;
            foreach (var station in GameData.Stations.Values)
                context.Targets[station.id] = WorkshopStation.Create(sink, context, station, index++);
        }

        public static void BuildFrontRoom(RectTransform room, UiContext context)
        {
            var config = GameData.Config;
            var session = context.Session;
            var counterTop = 1f - config.shopCustomerRatio;

            var customer = UiKit.Panel(room, "CustomerArea", new Vector2(0, counterTop), Vector2.one, Color(p => p.wall));
            UiKit.ApplyArt(customer, "Sprites/Rooms/front_wall");
            var customerContent = UiKit.Area(customer.transform, "Content", Vector2.zero, Vector2.one);
            UiKit.Pad(customerContent, AreaPadding, AreaPadding, AreaPadding, config.hudHeight + AreaPadding);
            CustomerView.Create(customerContent, context);
            context.Targets["customer"] = customerContent;

            var counter = UiKit.Panel(room, "Counter", Vector2.zero, new Vector2(1, counterTop), Color(p => p.counter));
            UiKit.ApplyArt(counter, "Sprites/Rooms/front_counter");

            var shelf = Station(counter.transform, "Shelf", "ui.area.shelf", Vector2.zero, new Vector2(config.shopShelfWidthRatio, 1));
            context.FrontShelf = ItemGridView.Create(shelf, context, () => session.AvailableShelf, config.shelfCapacity, config.shopShelfColumns, true);
            context.Targets["shelf_front"] = shelf;

            var craft = Station(counter.transform, "CraftTable", "ui.area.craft", new Vector2(config.shopShelfWidthRatio, 0), Vector2.one);
            context.Craft = CraftTableView.Create(craft, context);
            context.Targets["craft"] = craft;
        }

        // Baslikli bir tezgah bolumu; icerik icin baslik altindaki alani dondurur.
        static RectTransform Station(Transform parent, string name, string titleKey, Vector2 min, Vector2 max)
        {
            const float titleHeight = 32f;
            var frame = UiKit.Panel(parent, name, min, max, new Color(0, 0, 0, 0.12f), true);
            UiKit.Pad(frame.rectTransform, AreaPadding / 2, AreaPadding / 2, AreaPadding / 2, AreaPadding / 2);

            var title = UiKit.Label(frame.transform, "Title", GameData.Text(titleKey), 20, Color(p => p.cream), TextAlignmentOptions.Top);
            UiKit.Pad(title.rectTransform, 0, 0, 0, 4);

            var content = UiKit.Area(frame.transform, "Content", Vector2.zero, Vector2.one);
            UiKit.Pad(content, AreaPadding, AreaPadding, AreaPadding, titleHeight);
            return content;
        }

        static Color Color(System.Func<PaletteDef, string> pick) => GameData.ParseColor(pick(GameData.Config.palette));
    }
}
