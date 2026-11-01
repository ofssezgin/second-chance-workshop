using System.Collections.Generic;
using System.Linq;
using System.Text;
using CopAtolyesi.Core;
using UnityEditor;
using UnityEngine;

namespace CopAtolyesi.EditorTools
{
    // GDD 12: her gun yalnizca o gunun atigi ve su hakkiyla cozulebilir mi, tum veri referanslari gecerli mi?
    public static class DayValidator
    {
        [MenuItem("Cop Atolyesi/Gunleri Dogrula")]
        public static void Run()
        {
            GameData.Load();
            var errors = new List<string>();
            CheckContent(errors);
            foreach (var day in GameData.Days) CheckDay(day, errors);

            foreach (var error in errors) Debug.LogError("[Dogrulayici] " + error);
            Debug.Log($"[Dogrulayici] {GameData.Days.Length} gun kontrol edildi, {errors.Count} hata.");
            if (Application.isBatchMode) EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
        }

        static void CheckContent(List<string> errors)
        {
            void Need(string key)
            {
                if (!GameData.HasText(key)) errors.Add($"Metin eksik: {key}");
            }

            var baseKeys = GameData.ReadStrings(GameSettings.Languages[0]).Keys;
            foreach (var language in GameSettings.Languages)
            {
                var keys = GameData.ReadStrings(language).Keys;
                foreach (var key in baseKeys.Except(keys).Concat(keys.Except(baseKeys)))
                    errors.Add($"Dil dosyalari uyusmuyor ({language}): {key}");
            }

            foreach (var item in GameData.Items.Values)
            {
                Need("item." + item.id);
                if (!GameData.Materials.ContainsKey(item.material)) errors.Add($"{item.id}: bilinmeyen malzeme {item.material}");
                foreach (var tag in item.tags)
                {
                    if (!GameData.Tags.TryGetValue(tag, out var def)) errors.Add($"{item.id}: bilinmeyen etiket {tag}");
                    else if (!string.IsNullOrEmpty(def.opposite) && item.tags.Contains(def.opposite)) errors.Add($"{item.id}: zit etiketler {tag}/{def.opposite}");
                }
            }
            foreach (var product in GameData.Products.Values)
            {
                Need("product." + product.id);
                Need($"product.{product.id}.purpose");
                foreach (var slot in product.slots)
                {
                    Need("slot." + slot.id);
                    foreach (var tag in slot.requires ?? new string[0])
                        if (!GameData.Tags.ContainsKey(tag)) errors.Add($"{product.id}/{slot.id}: bilinmeyen etiket {tag}");
                }
            }
            foreach (var customer in GameData.Customers.Keys)
            {
                Need("customer." + customer);
                Need($"customer.{customer}.success");
                Need($"customer.{customer}.partial");
                Need($"customer.{customer}.fail");
            }
            foreach (var tag in GameData.Tags.Keys)
            {
                Need("tag." + tag);
                Need($"tag.{tag}.desc");
            }
            foreach (var step in GameData.Tutorial.steps) Need(step.textKey);
            foreach (var material in GameData.Materials.Keys) Need("material." + material);
            foreach (var tool in GameData.Tools.Keys)
            {
                Need("tool." + tool);
                Need($"tool.{tool}.desc");
                Need("ui.sink.use." + tool);
            }
            foreach (var station in GameData.Stations.Keys)
            {
                Need("tool." + station);
                Need($"tool.{station}.desc");
                Need("ui.bench.use." + station);
            }
            foreach (var item in GameData.Items.Values)
                foreach (var transform in item.transforms ?? new TransformDef[0])
                {
                    if (!GameData.Stations.ContainsKey(transform.station)) errors.Add($"{item.id}: bilinmeyen alet {transform.station}");
                    if (!GameData.Items.ContainsKey(transform.into)) errors.Add($"{item.id}: bilinmeyen sonuc {transform.into}");
                }
            foreach (var notice in GameData.Tutorial.notices) Need(notice.textKey);
            foreach (var treatment in GameData.Treatments.Values)
            {
                Need("care." + treatment.id);
                if (!GameData.Tools.ContainsKey(treatment.tool)) errors.Add($"{treatment.id}: bilinmeyen alet {treatment.tool}");
                if (!new[] { "rub", "trace", "shake", "stitch" }.Contains(treatment.mode)) errors.Add($"{treatment.id}: bilinmeyen kullanim {treatment.mode}");
                if (treatment.mode != "rub" && treatment.mode != "shake" && treatment.shape != "scratch") errors.Add($"{treatment.id}: {treatment.mode} cizgi izi ister");
                if (treatment.mode == "shake" && treatment.strokes <= 0) errors.Add($"{treatment.id}: strokes eksik");
            }
            foreach (var day in GameData.Days)
            {
                Need($"day.{day.day}.title");
                Need($"day.{day.day}.note");
                Need($"day.{day.day}.fact");
                Need($"day.{day.day}.source");
                foreach (var entry in day.manifest)
                    foreach (var care in entry.care ?? new string[0])
                    {
                        if (!GameData.Treatments.TryGetValue(care, out var treatment)) errors.Add($"Gun {day.day}: bilinmeyen bakim {care}");
                        else if (GameData.Tools[treatment.tool].unlockDay > day.day) errors.Add($"Gun {day.day}: {care} icin {treatment.tool} henuz kilitli");
                    }
                foreach (var order in day.orders.Concat(day.walkIns ?? new OrderDef[0])) Need(order.textKey);
                foreach (var brought in day.broughtItems ?? new BroughtItemDef[0]) Need("gift." + brought.customer);
            }
        }

        static void CheckDay(DayDef day, List<string> errors)
        {
            var orders = day.orders.Concat(day.walkIns ?? new OrderDef[0]).ToList();
            foreach (var order in orders)
            {
                if (!GameData.Customers.ContainsKey(order.customer)) errors.Add($"Gun {day.day}: bilinmeyen musteri {order.customer}");
                if (!GameData.Products.ContainsKey(order.product)) { errors.Add($"Gun {day.day}: bilinmeyen urun {order.product}"); return; }
            }

            var pool = day.manifest.Select(m => new Source(ItemInstance.FromManifest(m), day.day)).ToList();
            foreach (var entry in day.manifest)
                foreach (var care in entry.care ?? new string[0])
                    if (GameData.Treatments.TryGetValue(care, out var def) && def.materials.Length > 0 && !def.materials.Contains(GameData.Items[entry.item].material))
                        errors.Add($"Gun {day.day}: {entry.item} malzemesinde {care} olamaz");
            var brought = day.broughtItems ?? new BroughtItemDef[0];
            pool.AddRange(brought.Select(b => new Source(new ItemInstance(GameData.Items[b.item], null, true), day.day)));

            var slots = orders.SelectMany(o => GameData.Products[o.product].slots.Select(s => (order: o, slot: s))).ToList();
            var assignment = new string[slots.Count];
            if (Solve(0, slots, pool, assignment, day.cleaningPoints))
            {
                var log = new StringBuilder($"[Dogrulayici] Gun {day.day} cozulebilir: ");
                for (var i = 0; i < slots.Count; i++) log.Append($"{slots[i].order.product}.{slots[i].slot.id}={assignment[i]} ");
                log.Append($"| artan esya: {pool.Count(p => p.Chosen == null)}");
                Debug.Log(log);
            }
            else errors.Add($"Gun {day.day}: su hakki ({day.cleaningPoints}) ve atiklarla cozum yok. Siparisler: {string.Join(", ", orders.Select(o => o.product))}");

            var least = Enumerable.Range(0, day.cleaningPoints + 1).First(water =>
            {
                foreach (var source in pool) source.Reset();
                return Solve(0, slots, pool, assignment, water) || water == day.cleaningPoints;
            });
            Debug.Log($"[Dogrulayici] Gun {day.day} en az temizlik: {least}, su: {day.cleaningPoints}");

            // Hata payi: tek bir esya yanlis yere gitse de gun kurtuluyor mu?
            var fragile = new List<string>();
            for (var i = 0; i < pool.Count; i++)
            {
                var rest = pool.Where((_, index) => index != i).ToList();
                foreach (var source in pool) source.Reset();
                if (!Solve(0, slots, rest, assignment, day.cleaningPoints)) fragile.Add(pool[i].Variants[0].item.Def.id);
            }
            Debug.Log($"[Dogrulayici] Gun {day.day} yedegi olmayan esyalar: {(fragile.Count > 0 ? string.Join(", ", fragile.Distinct()) : "yok")}");

            foreach (var order in orders)
                if (GameData.Products[order.product].slots.Length > GameData.Config.shelfCapacity)
                    errors.Add($"Gun {day.day}: {order.product} rafa sigmiyor.");
        }

        // Bir atik ya oldugu gibi ya da o gun acik bir aletle donusmus haliyle kullanilir; donusum birden cok parca verebilir.
        sealed class Source
        {
            public readonly List<(ItemInstance item, int count)> Variants = new List<(ItemInstance, int)>();
            public readonly bool Clean;
            public (ItemInstance item, int count)? Chosen;
            public int Left;

            public Source(ItemInstance item, int day)
            {
                Clean = item.IsClean;
                Variants.Add((item, 1));
                foreach (var transform in item.Def.transforms ?? new TransformDef[0])
                    if (GameData.Stations[transform.station].unlockDay <= day)
                        Variants.Add((new ItemInstance(GameData.Items[transform.into], new TreatmentDef[0], true), transform.count));
            }

            public void Reset()
            {
                Chosen = null;
                Left = 0;
            }
        }

        // Her slota bir parca atar; temizlenecek atik sayisi su hakkini asamaz.
        static bool Solve(int index, List<(OrderDef order, SlotDef slot)> slots, List<Source> pool, string[] assignment, int water)
        {
            if (index == slots.Count) return true;
            foreach (var source in pool)
            {
                if (source.Chosen != null)
                {
                    if (source.Left <= 0 || !CraftEvaluator.Accepts(slots[index].slot, source.Chosen.Value.item)) continue;
                    source.Left--;
                    assignment[index] = source.Chosen.Value.item.Def.id;
                    if (Solve(index + 1, slots, pool, assignment, water)) return true;
                    source.Left++;
                    continue;
                }

                var cost = source.Clean ? 0 : 1;
                if (pool.Count(p => p.Chosen != null && !p.Clean) + cost > water) continue;
                foreach (var variant in source.Variants)
                {
                    if (!CraftEvaluator.Accepts(slots[index].slot, variant.item)) continue;
                    source.Chosen = variant;
                    source.Left = variant.count - 1;
                    assignment[index] = variant.item.Def.id;
                    if (Solve(index + 1, slots, pool, assignment, water)) return true;
                    source.Reset();
                }
            }
            return false;
        }
    }
}
