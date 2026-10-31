using System.Collections.Generic;
using System.Linq;

namespace CopAtolyesi.Core
{
    public enum CraftFailure { None, SlotEmpty, WrongMaterial, MissingTag }

    public sealed class CraftResult
    {
        public CraftFailure Failure;
        public SlotDef Slot;
        public ItemInstance Item;
        public string Tag;
        public int Stars;
        public bool PrefMet;
        public Dictionary<string, ItemInstance> Placed;
        public ItemInstance Delivered;

        public bool Success => Failure == CraftFailure.None;

        public string Feedback()
        {
            if (Success) return string.Empty;
            var slot = GameData.Text("slot." + Slot.id);
            switch (Failure)
            {
                case CraftFailure.SlotEmpty:
                    return GameData.Text("feedback.slot_empty", ("slot", slot));
                case CraftFailure.WrongMaterial:
                    return GameData.Text("feedback.wrong_material", ("slot", slot),
                        ("material", GameData.Text("material." + Slot.materials[0])));
                default:
                    return GameData.Text("feedback.missing_tag", ("slot", slot),
                        ("tag", GameData.Text("tag." + Tag)), ("item", Item.DisplayName));
            }
        }
    }

    public static class CraftEvaluator
    {
        public static CraftResult Evaluate(OrderDef order, IReadOnlyDictionary<string, ItemInstance> placements)
        {
            var product = GameData.Products[order.product];
            foreach (var slot in product.slots)
            {
                placements.TryGetValue(slot.id, out var item);
                var failure = CheckSlot(slot, item);
                if (failure != null) return failure;
            }
            var used = placements.Values.ToList();
            return new CraftResult
            {
                Stars = StarsFor(order, used),
                PrefMet = !order.HasPref || PrefSatisfied(order.pref, used),
                Placed = placements.ToDictionary(p => p.Key, p => p.Value)
            };
        }

        public static bool Accepts(SlotDef slot, ItemInstance item) => CheckSlot(slot, item) == null;

        static CraftResult CheckSlot(SlotDef slot, ItemInstance item)
        {
            if (item == null) return new CraftResult { Failure = CraftFailure.SlotEmpty, Slot = slot };

            if (slot.materials != null && slot.materials.Length > 0 && !slot.materials.Contains(item.Def.material))
                return new CraftResult { Failure = CraftFailure.WrongMaterial, Slot = slot, Item = item };

            foreach (var tag in slot.requires ?? new string[0])
            {
                if (!item.HasTag(tag))
                    return new CraftResult { Failure = CraftFailure.MissingTag, Slot = slot, Item = item, Tag = tag };
            }
            return null;
        }

        public static int StarsFor(OrderDef order, List<ItemInstance> used)
        {
            return order.HasPref && PrefSatisfied(order.pref, used) ? 2 : 1;
        }

        public static int MaxStars(OrderDef order) => order.HasPref ? 2 : 1;

        static bool PrefSatisfied(PrefDef pref, List<ItemInstance> used)
        {
            switch (pref.type)
            {
                case "tag_like": return used.Any(i => i.HasTag(pref.value));
                case "tag_dislike": return !used.Any(i => i.HasTag(pref.value));
                case "material_like": return used.Any(i => i.Def.material == pref.value);
                case "material_dislike": return used.All(i => i.Def.material != pref.value);
                default: return false;
            }
        }
    }
}
