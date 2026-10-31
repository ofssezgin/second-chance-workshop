using System.Collections.Generic;
using System.Linq;

namespace CopAtolyesi.Core
{
    // Oyundaki tek bir fiziksel esya. ItemDef ortaktir; kir ve bakim isleri bu ornege aittir.
    // Product doluysa bu esya yapilmis bir urundur ve Placed parcalarini tutar.
    public sealed class ItemInstance
    {
        static int nextSeed = 1;

        public readonly ItemDef Def;
        public readonly IReadOnlyList<TreatmentDef> Treatments;
        public readonly int Seed = nextSeed++;
        public bool IsClean;
        public ProductDef Product;
        public Dictionary<string, ItemInstance> Placed;

        public ItemInstance(ItemDef def, IReadOnlyList<TreatmentDef> treatments, bool isClean)
        {
            Def = def;
            Treatments = treatments;
            IsClean = isClean;
        }

        // Her atik yikanir; manifestteki "care" listesi buna ek bakim isleri katar.
        public static ItemInstance FromManifest(ManifestEntry entry)
        {
            var treatments = new List<TreatmentDef> { GameData.BaseTreatment };
            treatments.AddRange((entry.care ?? new string[0]).Select(id => GameData.Treatments[id]));
            return new ItemInstance(GameData.Items[entry.item], treatments, false);
        }

        public static ItemInstance MakeProduct(ProductDef product, Dictionary<string, ItemInstance> placed)
        {
            var def = new ItemDef { id = product.id, material = placed.Values.First().Def.material, tags = new string[0], sprite = "Sprites/Products/" + product.id };
            return new ItemInstance(def, new TreatmentDef[0], true) { Product = product, Placed = placed };
        }

        public ItemInstance CleanCopy()
        {
            return new ItemInstance(Def, new TreatmentDef[0], true) { Product = Product, Placed = Placed };
        }

        public string DisplayName => GameData.Text((Product != null ? "product." : "item.") + Def.id);

        public bool HasTag(string tag) => Def.tags.Contains(tag);

        public TransformDef TransformFor(string station) =>
            Product != null ? null : (Def.transforms ?? new TransformDef[0]).FirstOrDefault(t => t.station == station);

        public int MarkCount(TreatmentDef treatment) => treatment.count > 0 ? treatment.count : Def.dirtLevel;
    }
}
