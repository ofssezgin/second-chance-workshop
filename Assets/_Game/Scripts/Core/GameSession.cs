using System;
using System.Collections.Generic;
using System.Linq;

namespace CopAtolyesi.Core
{
    // Oyunun tum durumu burada. Arayuz bu sinifi okur ve Changed olayiyla yenilenir.
    public sealed class GameSession
    {
        public event Action Changed;
        public event Action<OrderDef, CraftResult> Served;
        public event Action<BroughtItemDef> ItemBrought;
        public event Action<string> Signaled;

        public int DayNumber { get; private set; }
        public DayDef Day { get; private set; }
        public int CleaningPoints { get; private set; }
        public int TotalStars { get; private set; }
        public int MaxStars { get; private set; }
        public int StarsToday { get; private set; }
        public int ServedToday { get; private set; }
        public int OrdersToday { get; private set; }
        public int MonumentItems { get; private set; }
        public int MissedCustomers { get; private set; }
        public readonly List<ItemInstance> MonumentList = new List<ItemInstance>();
        public readonly HashSet<string> MissedBy = new HashSet<string>();
        public readonly HashSet<string> FinalProducts = new HashSet<string>();
        public readonly Dictionary<string, ItemInstance> Delivered = new Dictionary<string, ItemInstance>();
        public int MissedPenalty => MissedCustomers * GameData.Config.missedCustomerPenalty;
        public int FinalStars => Math.Max(0, TotalStars - MissedPenalty);
        public bool WaterWasted => CleaningPoints < 0;
        public ItemInstance Washing { get; private set; }
        public ItemInstance OnBench { get; private set; }
        public readonly List<ItemInstance> Waste = new List<ItemInstance>();
        public readonly List<ItemInstance> Shelf = new List<ItemInstance>();
        public readonly List<OrderDef> Queue = new List<OrderDef>();
        public readonly Dictionary<string, ItemInstance> Placements = new Dictionary<string, ItemInstance>();

        public readonly List<string> SeenProducts = new List<string>();
        public readonly List<string> SeenItems = new List<string>();

        readonly HashSet<OrderDef> postponed = new HashSet<OrderDef>();
        readonly HashSet<OrderDef> seenOrders = new HashSet<OrderDef>();
        bool playerInFront;
        readonly List<BroughtItemDef> pendingBrought = new List<BroughtItemDef>();

        public bool IsCurrentOrderSeen => CurrentOrder != null && seenOrders.Contains(CurrentOrder);
        public bool IsToolUnlocked(ToolDef tool) => DayNumber >= tool.unlockDay;
        public bool ShelfFull => Shelf.Count >= GameData.Config.shelfCapacity;
        public bool HasNextDay => GameData.Days.Any(d => d.day > DayNumber);
        public OrderDef CurrentOrder => Queue.Count > 0 ? Queue[0] : null;
        public bool CanPostpone => CurrentOrder != null && Queue.Count > 1 && !postponed.Contains(CurrentOrder);
        public IReadOnlyList<ItemInstance> AvailableShelf => Shelf.Where(i => !Placements.ContainsValue(i) && i != OnBench).ToList();

        public void StartDay(int dayNumber)
        {
            Day = GameData.Days.First(d => d.day == dayNumber);
            DayNumber = dayNumber;
            // Kullanilmayan su ertesi gune aktarilir.
            CleaningPoints += Day.cleaningPoints;
            StarsToday = 0;
            ServedToday = 0;
            Washing = null;
            OnBench = null;
            // Temizlenmeyen atik kalir; bugunun atigi one gecer ki sandik dolsa da gorunur olsun.
            Waste.InsertRange(0, Day.manifest.Select(ItemInstance.FromManifest));
            AddUnlockedCare();

            BuildQueue();
            pendingBrought.Clear();
            pendingBrought.AddRange(Day.broughtItems ?? new BroughtItemDef[0]);
            MaxStars += Queue.Sum(CraftEvaluator.MaxStars);
            foreach (var product in Queue.Select(o => o.product).Except(SeenProducts).ToList()) SeenProducts.Add(product);
            foreach (var item in Day.manifest.Select(m => m.item).Except(SeenItems).ToList()) SeenItems.Add(item);
            ReceiveBroughtItem();
            MarkOrderSeen();
            Changed?.Invoke();
        }

        // Aleti acilan bakim isi, sandiktaki eski atiklar dahil o malzemedeki her esyada gorulur.
        void AddUnlockedCare()
        {
            foreach (var treatment in GameData.Treatments.Values.Where(t => GameData.Tools[t.tool].unlockDay <= DayNumber))
                foreach (var item in Waste.Where(i => !i.Treatments.Contains(treatment)
                    && ((treatment.autoMaterials ?? new string[0]).Contains(i.Def.material) || (treatment.autoItems ?? new string[0]).Contains(i.Def.id))))
                    item.AddCare(treatment);
        }

        public void Signal(string name) => Signaled?.Invoke(name);

        public void SetPlayerRoom(bool inFront)
        {
            playerInFront = inFront;
            MarkOrderSeen();
            Changed?.Invoke();
            Signal(inFront ? "room_front" : "room_back");
        }

        // Oyuncu siparisi yalnizca o musteri ondeyken dukkandaysa gormus sayilir.
        void MarkOrderSeen()
        {
            if (playerInFront && CurrentOrder != null) seenOrders.Add(CurrentOrder);
        }

        void BuildQueue()
        {
            Queue.Clear();
            postponed.Clear();
            seenOrders.Clear();
            Placements.Clear();
            Queue.AddRange(Day.orders);
            foreach (var walkIn in Day.walkIns ?? new OrderDef[0])
                Queue.Insert(Math.Min(walkIn.queueIndex, Queue.Count), walkIn);
            OrdersToday = Queue.Count;
        }

        void ReceiveBroughtItem()
        {
            if (CurrentOrder == null) return;
            var brought = pendingBrought.FirstOrDefault(b => b.customer == CurrentOrder.customer);
            if (brought == null) return;
            pendingBrought.Remove(brought);
            Shelf.Add(new ItemInstance(GameData.Items[brought.item], new TreatmentDef[0], true) { GiftFrom = brought.customer });
            ItemBrought?.Invoke(brought);
        }

        // Temizlik baslatilamiyorsa oyuncuya gosterilecek metnin anahtarini dondurur.
        public string CleanBlockReason()
        {
            if (ShelfFull) return "feedback.shelf_full";
            // Rehber gununde su biter; sonraki gunlerde su eksiye dusebilir ama oyun sonunda eksi kalirsa mahalle kurur.
            if (CleaningPoints <= 0 && DayNumber == GameData.Tutorial.day) return "feedback.no_water";
            return null;
        }

#if DEVELOPMENT_BUILD
        public void SpendWaterForTest(int amount)
        {
            CleaningPoints -= amount;
            Changed?.Invoke();
        }
#endif

        // Basarisizsa oyuncuya gosterilecek metnin anahtarini, basarida null dondurur.
        public string BeginWash(ItemInstance item)
        {
            var reason = Washing != null || OnBench != null ? "feedback.sink_busy" : CleanBlockReason();
            if (reason != null) return reason;
            Washing = item;
            Changed?.Invoke();
            Signal("wash_started");
            return null;
        }

        // Temiz esya kesmek icin masaya kondu; su harcamaz, rafa geri alinabilir.
        public string PlaceOnBench(ItemInstance item)
        {
            if (Washing != null || OnBench != null) return "feedback.sink_busy";
            OnBench = item;
            Changed?.Invoke();
            Signal("bench_placed");
            return null;
        }

        public void CancelWash()
        {
            Washing = null;
            OnBench = null;
            Changed?.Invoke();
        }

        public void FinishWash() => TryClean(Washing);

        public bool TryClean(ItemInstance item)
        {
            if (CleanBlockReason() != null || !Waste.Remove(item)) return false;
            if (Washing == item) Washing = null;
            item.IsClean = true;
            CleaningPoints--;
            Shelf.Add(item);
            Changed?.Invoke();
            Signal("wash_done");
            foreach (var transform in item.Def.transforms ?? new TransformDef[0]) Signal("cleaned_" + transform.station);
            return true;
        }

        public void Place(string slotId, ItemInstance item)
        {
            foreach (var key in Placements.Where(p => p.Value == item).Select(p => p.Key).ToList()) Placements.Remove(key);
            Placements[slotId] = item;
            Changed?.Invoke();
            Signal("item_placed");
        }

        public void ClearPlacements()
        {
            Placements.Clear();
            Changed?.Invoke();
        }

        public void Unplace(ItemInstance item)
        {
            foreach (var key in Placements.Where(p => p.Value == item).Select(p => p.Key).ToList()) Placements.Remove(key);
            Changed?.Invoke();
        }

        // Birlestir: parcalar urune donusur ve urun rafa gecer; teslim ayri bir adimdir.
        public CraftResult Craft()
        {
            var order = CurrentOrder;
            var result = CraftEvaluator.Evaluate(order, Placements);
            if (!result.Success) return result;

            foreach (var item in Placements.Values) Shelf.Remove(item);
            Shelf.Add(ItemInstance.MakeProduct(GameData.Products[order.product], result.Placed));
            Placements.Clear();
            Changed?.Invoke();
            Signal("crafted");
            return result;
        }

        public bool IsStationUnlocked(StationDef station) => DayNumber >= station.unlockDay;

        ProductDef BenchProductFor(StationDef station, ItemInstance item)
        {
            if (!station.bench || item.Product != null) return null;
            return Queue.Select(o => GameData.Products[o.product]).FirstOrDefault(p => p.workbench
                && !Shelf.Any(i => i.Product == p) && CraftEvaluator.Accepts(p.slots[0], item));
        }

        // Esya bu aletle islenemiyorsa nedenin metin anahtarini, islenebiliyorsa null dondurur.
        public string CutBlockReason(string stationId, ItemInstance item)
        {
            var station = GameData.Stations[stationId];
            if (!IsStationUnlocked(station)) return "feedback.tool_locked";

            var count = 0;
            if (item.Product != null && station.bench) count = item.Placed.Count;
            else if (item.TransformFor(stationId) != null) count = item.TransformFor(stationId).count;
            else if (BenchProductFor(station, item) != null) count = 1;
            if (count == 0) return "feedback.scissors_nothing";
            return Shelf.Count - 1 + count > GameData.Config.shelfCapacity ? "feedback.scissors_full" : null;
        }

        // Kesme aletleri: esyayi donusturur, tek parcali urunu yapar, yanlis urunu parcalarina ayirir.
        public string Cut(string stationId, ItemInstance item)
        {
            var reason = CutBlockReason(stationId, item);
            if (reason != null) return reason;

            if (OnBench == item) OnBench = null;
            var transform = item.TransformFor(stationId);
            if (item.Product != null) Replace(item, item.Placed.Values);
            else if (transform != null) Replace(item, Enumerable.Range(0, transform.count)
                .Select(_ => new ItemInstance(GameData.Items[transform.into], new TreatmentDef[0], true)));
            else
            {
                var product = BenchProductFor(GameData.Stations[stationId], item);
                var placed = new Dictionary<string, ItemInstance> { { product.slots[0].id, item } };
                Replace(item, new[] { ItemInstance.MakeProduct(product, placed) });
            }
            Changed?.Invoke();
            Signal("item_cut");
            return null;
        }

        void Replace(ItemInstance item, IEnumerable<ItemInstance> results)
        {
            Placements.Keys.Where(k => Placements[k] == item).ToList().ForEach(k => Placements.Remove(k));
            var index = Shelf.IndexOf(item);
            Shelf.RemoveAt(index);
            Shelf.InsertRange(index, results);
        }

        // Teslim: urun onde bekleyen musterinin siparisiyse yildizlar burada hesaplanir.
        public string Deliver(ItemInstance item)
        {
            var order = CurrentOrder;
            if (order == null) return "feedback.no_customer";
            if (item.Product == null) return "feedback.only_products";
            if (item.Product.id != order.product) return "feedback.wrong_product";

            var result = CraftEvaluator.Evaluate(order, item.Placed);
            Shelf.Remove(item);
            Queue.RemoveAt(0);
            TotalStars += result.Stars;
            StarsToday += result.Stars;
            ServedToday++;
            Delivered[order.customer] = item;
            if (!HasNextDay) FinalProducts.Add(order.product);
            result.Delivered = item;
            Served?.Invoke(order, result);
            ReceiveBroughtItem();
            MarkOrderSeen();
            Changed?.Invoke();
            Signal("served");
            return null;
        }

        public void Postpone()
        {
            if (!CanPostpone) return;
            var order = CurrentOrder;
            postponed.Add(order);
            Queue.RemoveAt(0);
            Queue.Add(order);
            Placements.Clear();
            ReceiveBroughtItem();
            MarkOrderSeen();
            Changed?.Invoke();
        }

        public void EndDay()
        {
            Washing = null;
            OnBench = null;
            MissedCustomers += Queue.Count;
            foreach (var order in Queue) MissedBy.Add(order.customer);
            Queue.Clear();
            Placements.Clear();

            // Son gun artan her sey Mahalle Anitina gider: oyun sonunda cope giden sifirdir.
            if (!HasNextDay)
            {
                MonumentList.AddRange(Shelf.Concat(Waste));
                MonumentItems = MonumentList.Count;
                Shelf.Clear();
                Waste.Clear();
            }
            Changed?.Invoke();
            Signal("day_ended");
        }
    }
}
