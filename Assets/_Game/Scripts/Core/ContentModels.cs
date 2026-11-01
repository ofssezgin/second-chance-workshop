using System;

// JSON dosyalarinin birebir karsiligi. Alan adlari JSON anahtarlariyla ayni olmali (JsonUtility).
namespace CopAtolyesi.Core
{
    [Serializable]
    public sealed class GameConfig
    {
        public int referenceWidth = 1280;
        public int referenceHeight = 720;
        public int hudHeight = 56;
        public float shopCustomerRatio = 0.5f;
        public float cleaningCounterRatio = 0.75f;
        public int shopShelfColumns = 4;
        public int cleaningShelfColumns = 6;
        public float cleaningShelfWidthRatio = 0.75f;
        public int missedCustomerPenalty = 1;
        public float musicVolume = 0.22f;
        public float sfxVolume = 0.6f;
        public float shopShelfWidthRatio = 0.62f;
        public float crateWidthRatio = 0.42f;
        public int shelfCapacity = 8;
        public float roomSlideSeconds = 0.5f;
        public string startRoom = "back";
        public float dirtyTint = 0.4f;
        public float hoverSeconds = 0.3f;
        public float hoverScale = 1.08f;
        public float washItemSize = 300f;
        public float pileItemSize = 118f;
        public float pileMaxTilt = 28f;
        public float flyToShelfSeconds = 0.35f;
        public float toastSeconds = 1.8f;
        public TruckCutsceneConfig truckCutscene = new TruckCutsceneConfig();
        public PaletteDef palette = new PaletteDef();
    }

    [Serializable]
    public sealed class TruckCutsceneConfig
    {
        public int artScale = 3;
        public float truckSize = 288f;
        public float truckStopX = 24f;
        public float groundY = 150f;
        public float truckY = 120f;
        public float beltHeight = 48f;
        public float beltTileSize = 16f;
        public float itemSize = 96f;
        public float truckSeconds = 0.9f;
        public float beltSeconds = 0.7f;
        public float itemIntervalSeconds = 0.32f;
        public float itemSpeed = 560f;
        public float endHoldSeconds = 0.3f;
    }

    [Serializable]
    public sealed class PaletteDef
    {
        public string cream, teal, mustard, tile, anthracite, leaf, counter, wall, slot, locked;
    }

    [Serializable] public sealed class MaterialDef { public string id; public string color; }
    [Serializable] public sealed class TagDef { public string id; public string opposite; }
    [Serializable] public sealed class ToolDef { public string id; public string color; public int unlockDay; public float[] grip; }

    // Dukkandaki kesme aletleri. bench: tek parcali urunleri yapar ve urunleri soker.
    // mode: click (esyaya tiklama), shake (ileri-geri); strokes kac hareket gerektigini soyler.
    [Serializable] public sealed class StationDef { public string id; public int unlockDay; public bool bench; public string mode; public int strokes; }

    // Esyanin bir aletle donustugu hal: bir esyadan count tane into cikar.
    [Serializable] public sealed class TransformDef { public string station; public string into; public int count = 1; }

    // Bir bakim isi: hangi aletle, nasil bir izi, kac tane ve hangi hareketle gideriliyor.
    // mode: rub (ovma, seconds), trace (cizgiyi izleme), shake (ileri-geri, strokes), stitch (noktalara tiklama).
    [Serializable]
    public sealed class TreatmentDef
    {
        public string id;
        public string tool;
        public string mode;
        public string shape;
        public string color;
        public float sizeRatio;
        public int count;
        public float seconds;
        public int strokes;
        public string[] materials;
    }

    [Serializable]
    public sealed class ItemDef
    {
        public string id;
        public string material;
        public string[] tags;
        public int dirtLevel;
        public TransformDef[] transforms;
        public string sprite;
    }

    [Serializable]
    public sealed class SlotDef
    {
        public string id;
        public string[] requires;
        public string[] materials;
        public float[] pos;
        public float scale = 1f;
    }

    [Serializable]
    public sealed class ProductDef
    {
        public string id;
        public bool workbench;
        public SlotDef[] slots;
    }

    [Serializable]
    public sealed class CustomerDef
    {
        public string id;
        public string portrait;
    }

    [Serializable]
    public sealed class ManifestEntry
    {
        public string item;
        public string[] care;
    }

    [Serializable]
    public sealed class PrefDef
    {
        public string type;
        public string value;
        public bool Exists => !string.IsNullOrEmpty(type);
    }

    [Serializable]
    public sealed class OrderDef
    {
        public string customer;
        public string product;
        public string textKey;
        public bool hidden;
        public int queueIndex = -1;
        public PrefDef pref;
        public bool HasPref => pref != null && pref.Exists;
    }

    [Serializable]
    public sealed class BroughtItemDef
    {
        public string customer;
        public string item;
        public string textKey;
    }

    [Serializable]
    public sealed class DayDef
    {
        public int day;
        public int cleaningPoints;
        public string introCutscene;
        public ManifestEntry[] manifest;
        public OrderDef[] orders;
        public OrderDef[] walkIns;
        public BroughtItemDef[] broughtItems;
    }

    [Serializable] public sealed class TagsFile { public MaterialDef[] materials; public TagDef[] tags; }
    [Serializable] public sealed class CareFile { public string baseTreatment; public ToolDef[] tools; public StationDef[] stations; public TreatmentDef[] treatments; }
    [Serializable] public sealed class ItemsFile { public ItemDef[] items; }
    [Serializable] public sealed class ProductsFile { public ProductDef[] products; }
    [Serializable] public sealed class CustomersFile { public CustomerDef[] customers; }
    [Serializable] public sealed class DaysFile { public DayDef[] days; }
    [Serializable]
    public sealed class TutorialStepDef
    {
        public string textKey;
        public string target;
        public string wait;
        public string[] allow;
        public float[] anchor;
    }

    // Oyuncu bir tusa basana kadar ekranda kalan not: signal gelince, day gunu veya sonrasinda bir kez cikar.
    [Serializable] public sealed class NoticeDef { public string signal; public int day; public string textKey; }

    [Serializable] public sealed class TutorialFile { public int day; public TutorialStepDef[] steps; public NoticeDef[] notices; }
    [Serializable] public sealed class StringEntry{ public string key; public string value; }
    [Serializable] public sealed class StringsFile { public StringEntry[] entries; }
}
