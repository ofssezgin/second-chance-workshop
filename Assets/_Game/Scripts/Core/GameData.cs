using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CopAtolyesi.Core
{
    // Tum icerik Resources/Data altindaki JSON dosyalarindan gelir; kodda sabit icerik yoktur.
    public static class GameData
    {
        public static GameConfig Config { get; private set; }
        public static Dictionary<string, MaterialDef> Materials { get; private set; }
        public static Dictionary<string, TagDef> Tags { get; private set; }
        public static Dictionary<string, ToolDef> Tools { get; private set; }
        public static Dictionary<string, StationDef> Stations { get; private set; }
        public static Dictionary<string, TreatmentDef> Treatments { get; private set; }
        public static TreatmentDef BaseTreatment { get; private set; }
        public static Dictionary<string, ItemDef> Items { get; private set; }
        public static Dictionary<string, ProductDef> Products { get; private set; }
        public static Dictionary<string, CustomerDef> Customers { get; private set; }
        public static DayDef[] Days { get; private set; }
        public static TutorialFile Tutorial { get; private set; }

        static Dictionary<string, string> strings;

        public static void Load()
        {
            Config = Read<GameConfig>("config");
            var tags = Read<TagsFile>("tags");
            Materials = tags.materials.ToDictionary(m => m.id);
            Tags = tags.tags.ToDictionary(t => t.id);
            var care = Read<CareFile>("care");
            Tools = care.tools.ToDictionary(t => t.id);
            Stations = care.stations.ToDictionary(s => s.id);
            Treatments = care.treatments.ToDictionary(t => t.id);
            BaseTreatment = Treatments[care.baseTreatment];
            Items = Read<ItemsFile>("items").items.ToDictionary(i => i.id);
            Products = Read<ProductsFile>("products").products.ToDictionary(p => p.id);
            Customers = Read<CustomersFile>("customers").customers.ToDictionary(c => c.id);
            Days = Read<DaysFile>("days").days.OrderBy(d => d.day).ToArray();
            Tutorial = Read<TutorialFile>("tutorial");
            SetLanguage(GameSettings.Language);
        }

        public static void SetLanguage(string language) => strings = ReadStrings(language);

        public static Dictionary<string, string> ReadStrings(string language)
        {
            return Read<StringsFile>("strings_" + language).entries.ToDictionary(e => e.key, e => e.value);
        }

        public static bool HasText(string key) => strings.ContainsKey(key);

        public static string Text(string key)
        {
            return strings.TryGetValue(key, out var value) ? value : $"[{key}]";
        }

        public static string Text(string key, params (string name, object value)[] args)
        {
            var text = Text(key);
            foreach (var (name, value) in args) text = text.Replace("{" + name + "}", value.ToString());
            return text;
        }

        public static Color ParseColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;
        }

        static T Read<T>(string name)
        {
            var asset = Resources.Load<TextAsset>("Data/" + name);
            if (asset == null) throw new InvalidOperationException($"Veri dosyasi bulunamadi: Data/{name}.json");
            return JsonUtility.FromJson<T>(asset.text);
        }
    }
}
