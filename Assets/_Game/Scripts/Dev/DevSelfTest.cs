#if DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CopAtolyesi.Core;
using CopAtolyesi.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CopAtolyesi.Dev
{
    // Yalnizca dev build: -selftest <klasor> ile sahte fare olaylariyla oyunu oynar.
    // Girdi oyunun icine kuyruklanir, isletim sistemine veya baska pencerelere gitmez.
    public sealed class DevSelfTest : MonoBehaviour
    {
        UiContext context;
        GameSession session;
        string folder;
        Vector2 pointer;

        static bool launched;

        // Test sahne yeniden yuklenince de surebilsin diye kendi nesnesinde calisir.
        public static void Launch(GameBootstrap bootstrap, string folder)
        {
            if (launched) return;
            launched = true;
            var host = new GameObject("DevSelfTest");
            DontDestroyOnLoad(host);
            host.AddComponent<DevSelfTest>().Run(bootstrap, folder);
        }

        GameBootstrap bootstrap;
        bool playInEnglish;

        void Run(GameBootstrap bootstrap, string folder)
        {
            this.bootstrap = bootstrap;
            playInEnglish = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-testen") >= 0;
            this.folder = folder;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            var menu = bootstrap.Menu;
            yield return Click(menu.LanguagePoint("en"));
            Check("ingilizce secildi", GameSettings.Language == "en" && GameData.Text("menu.play") == "Play");
            yield return Click(menu.LanguagePoint("tr"));
            Check("turkce secildi", GameSettings.Language == "tr");
            yield return Click(menu.GearPoint);
            Check("ayarlar acildi", menu.Settings.IsOpen);
            yield return Click(menu.Settings.MusicPoint(0.25f));
            Check("muzik ayari calisti", Mathf.Abs(GameSettings.MusicVolume - 0.25f) < 0.1f
                && Mathf.Abs(AudioKit.MusicLevel - GameData.Config.musicVolume * GameSettings.MusicVolume) < 0.01f);
            yield return Click(menu.Settings.SfxPoint(0.75f));
            Check("efekt ayari muzigi etkilemedi", Mathf.Abs(GameSettings.SfxVolume - 0.75f) < 0.1f && GameSettings.MusicVolume < 0.4f);
            yield return Shot("0c_settings");
            GameSettings.MusicVolume = 0.5f;
            GameSettings.SfxVolume = 0.5f;
            AudioKit.ApplyVolume();
            yield return Click(menu.Settings.ClosePoint);
            Check("ayarlar kapandi", !menu.Settings.IsOpen);
            if (playInEnglish) yield return Click(menu.LanguagePoint("en"));
            Check("ana muzik caliyor", AudioKit.CurrentMusic == "music_piano_1" && AudioKit.HasClip("music_piano_3"));
            yield return new WaitForSecondsRealtime(1.5f);
            AudioKit.SeekNearEndForTest();
            yield return new WaitForSecondsRealtime(1f);
            Check("sarkilar kesintisiz baglaniyor", AudioKit.CurrentMusic == "music_piano_2" && AudioKit.Crossfading);
            Check("efektler yuklendi", new[] { "click", "drop", "ding", "bell", "success", "error", "star" }.All(AudioKit.HasClip));
            Check("kamyon ve alet sesleri kaldirildi", !new[] { "truck", "saw", "scissors", "sponge", "sandpaper", "needle", "glue" }.Any(AudioKit.HasClip));
            yield return Shot("0a_menu");
            yield return Click(menu.CreditsPoint);
            Check("emegi gecenler acildi", menu.CreditsOpen);
            yield return new WaitForSecondsRealtime(0.1f);
            yield return Shot("0b_credits");
            yield return Click(menu.BackPoint);
            Check("emegi gecenler kapandi", !menu.CreditsOpen);
            yield return Click(menu.PlayPoint);
            context = bootstrap.Context;
            session = context.Session;

            yield return SkipCutscene("0_cutscene");
            Check("hikaye girisi acildi", context.Summary.gameObject.activeSelf);
            yield return Click(context.Summary.ContinuePoint);
            Check("gun notu acildi", context.Summary.gameObject.activeSelf);
            yield return Shot("0d_day_intro");
            yield return Click(context.Summary.ContinuePoint);
            Check("rehber basladi", context.Tutorial.Active);
            Check("bakilmamis musteri gizli", !session.IsCurrentOrderSeen);

            var switcher = FindAnyObjectByType<RoomSwitcher>();
            yield return ToggleRoom(switcher);
            yield return Shot("0f_shop");
            yield return ToggleRoom(switcher);
            Check("bakilan musteri temizlikte gorunur", session.IsCurrentOrderSeen);

            var pileItem = PileItem(i => i.HasTag("transparent"));
            yield return MoveTo(pileItem.position, false);
            yield return new WaitForSecondsRealtime(0.6f);
            Check("etiket ipucu acildi", context.Tooltip.gameObject.activeSelf);

            var sinkCenter = (Vector2)((RectTransform)context.Wash.transform).position;
            yield return Drag(PileItem().position, new Vector2(10, Screen.height / 2f));
            Check("gecersiz birakma yerinde birakir", session.Washing == null && session.Waste.Count == 2);
            yield return Drag(PileItem(i => !i.HasTag("transparent")).position, sinkCenter);
            Check("rehber istege uymayan atigi almaz", session.Washing == null);
            yield return Drag(PileItem(i => !i.HasTag("transparent")).position, new Vector2(80, 120));

            var stepKey = context.Tutorial.CurrentKey;
            session.Signal("room_front");
            session.Signal("item_cut");
            Check("rehber adim atlamaz", context.Tutorial.CurrentKey == stepKey);
            yield return Click(HudNotebookPoint());
            Check("rehberde kilitli yer calismaz", !context.Notebook.IsOpen);
            yield return Click(FindAnyObjectByType<HudView>().EndDayPoint);
            Check("rehberde gun bitirilemez", context.Tutorial.Active && !context.Summary.gameObject.activeSelf);

            pileItem = PileItem(i => i.HasTag("transparent"));
            var item = pileItem.GetComponent<ItemView>().Item;
            yield return Drag(pileItem.position, sinkCenter);
            Check("masaya birakildi", session.Washing == item);
            Check("alet otomatik secilmedi", context.Wash.SelectedTool == null);
            Check("rehberde vazgec yok", !context.Wash.CancelVisible);
            session.CancelWash();
            yield return null;
            Check("iptal ucretsiz", session.Washing == null && session.CleaningPoints == 1);
            session.BeginWash(item);
            yield return null;
            yield return Click(context.Wash.ToolPoint("sponge"));
            Check("sunger ele alindi ve elde kaldi", context.Wash.SelectedTool == "sponge");
            yield return Click(context.Wash.ToolPoint("sponge"));
            Check("elde alet yerine kondu", context.Wash.SelectedTool == null);
            yield return Click(context.Wash.ToolPoint("sponge"));
            yield return ScrubAll();
            Check("temizlendi ve rafa gitti", session.Washing == null && session.Shelf.Contains(item) && item.IsClean);
            Check("rehber gununde su bitince masa kilitlendi", session.BeginWash(session.Waste[0]) == "feedback.no_water");

            yield return new WaitForSecondsRealtime(0.5f);
            var scissors = context.Targets["scissors"].GetComponent<WorkshopStation>();
            yield return Click(ScissorsPoint());
            Check("masa bosken makas alinmaz", context.Wash.SelectedStation == null);
            yield return Drag(context.BackShelf.CellRect(0).position, sinkCenter);
            Check("temiz esya su harcamadan masaya kondu", session.OnBench == item && session.CleaningPoints == 0);
            yield return Click(ScissorsPoint());
            Check("makas ele alindi", context.Wash.SelectedStation == "scissors" && scissors.Highlighted);
            yield return Shot("1a_scissors_hover");
            yield return Click(sinkCenter);
            Check("makas tek tikla kesmez", session.OnBench == item);
            yield return Click(sinkCenter);
            yield return Click(sinkCenter);
            yield return new WaitForSecondsRealtime(0.1f);
            Check("makas uc tikla kalemlik yapti", session.OnBench == null && session.Shelf.Count == 1 && session.Shelf[0].Product != null);
            yield return new WaitForSecondsRealtime(0.1f);
            yield return Shot("1a_pen_holder_on_shelf");

            context.Notebook.Toggle();
            context.Notebook.TurnForTest(4);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("1b_transforms_day1");
            context.Notebook.TurnForTest(0);
            yield return new WaitForSecondsRealtime(0.4f);
            context.Notebook.Toggle();
            yield return null;

            yield return ToggleRoom(switcher);
            var wrong = session.Deliver(session.Waste[0]);
            Check("urun olmayan teslim edilemez", wrong == "feedback.only_products");
            var view = FindAnyObjectByType<CustomerView>();
            var expected = item.HasTag("transparent") ? "success" : "partial";
            yield return Drag(context.FrontShelf.CellRect(0).position, view.DeliveryPoint);
            Check("teslim edildi", session.Queue.Count == 0 && session.TotalStars >= 1 && session.Shelf.Count == 0);
            Check("musteri dogru tepki verdi", view.SpeechText.Contains(GameData.Text("customer.zeynep." + expected)));
            yield return new WaitForSecondsRealtime(1f);
            Check("birlestirme notu cikti", context.Notice.IsOpen && context.Notice.Message == GameData.Text("notice.craft"));
            yield return Shot("1e_served");
            yield return DismissNotices();
            yield return new WaitForSecondsRealtime(1.8f);

            var hud = FindAnyObjectByType<HudView>();
            yield return Click(hud.SettingsPoint);
            Check("oyun icinde ayarlar acildi", context.Settings.IsOpen);
            yield return Shot("1m_game_settings");
            yield return Click(context.Settings.ClosePoint);
            Check("oyun icinde ayarlar kapandi", !context.Settings.IsOpen);
            yield return Click(hud.EndDayPoint);
            Check("gun sonu ozeti acildi", context.Summary.gameObject.activeSelf);
            Check("rehber kapandi", !context.Tutorial.Active);

            var leftover = session.Waste[0];
            Check("ses varsayilani yuzde elli", Mathf.Abs(GameSettings.MusicVolume - 0.5f) < 0.01f && Mathf.Abs(GameSettings.SfxVolume - 0.5f) < 0.01f);
            yield return Click(context.Summary.ContinuePoint);
            yield return new WaitForSecondsRealtime(0.1f);
            Check("sonraki gun ara sahneyi baslatti", session.DayNumber == 2 && FindAnyObjectByType<CutsceneClip>() != null);
            yield return SkipCutscene("1g_day2_cutscene");
            yield return Click(context.Summary.ContinuePoint);
            yield return new WaitForSecondsRealtime(GameData.Config.roomSlideSeconds + 0.2f);
            Check("gun 2 temizlik ekraninda", !switcher.InFrontRoom && session.Queue.Count == 2);
            Check("su uyarisi tusa kadar kaldi", context.Notice.IsOpen);
            Check("kalan su gune eklendi", session.CleaningPoints == GameData.Days[0].cleaningPoints - 1 + GameData.Days[1].cleaningPoints);
            yield return Shot("1g_water_notice");
            yield return Click(new Vector2(640, 40));
            Check("not disari tiklayinca kapanmaz", context.Notice.IsOpen);
            yield return TapEscape();
            Check("not tusla kapanmaz", context.Notice.IsOpen);
            yield return DismissNotices();
            Check("gun 2'de yalniz sunger acik", !context.Wash.IsToolShownLocked("sponge") && context.Wash.IsToolShownLocked("glue"));
            yield return Drag(PileItem().position, (Vector2)((RectTransform)context.Wash.transform).position);
            yield return Click(context.Wash.ToolPoint("glue"));
            Check("kilitli alet secilemez", context.Wash.SelectedTool != "glue");
            session.CancelWash();
            yield return null;

            yield return MoveTo(context.Wash.ToolPoint("sponge"), false);
            yield return new WaitForSecondsRealtime(0.6f);
            Check("alet aciklamasi acildi", context.Tooltip.gameObject.activeSelf);
            yield return Shot("1h_tool_hint");
            yield return MoveTo(new Vector2(200, 400), false);

            yield return Click(hud.EndDayPoint);
            Check("musteri varken onay soruldu", context.Confirm.gameObject.activeSelf && session.DayNumber == 2);
            yield return Shot("1i_confirm");
            context.Confirm.gameObject.SetActive(false);

            yield return Click(hud.NotebookPoint);
            Check("defter acildi", context.Notebook.gameObject.activeSelf);
            yield return Shot("1j_notebook");
            yield return TapEscape();
            Check("esc defteri kapatti", !context.Notebook.IsOpen && !context.Pause.gameObject.activeSelf);

            // Gun 2: elle temizleyip iki parcali urun yap, yanlis parcayi geri al, sokup yeniden yap.
            var plank = session.Waste.First(i => i.Def.id == "pallet_plank");
            var jeans = session.Waste.First(i => i.Def.id == "jeans");
            var tin = session.Waste.First(i => i.Def.id == "tin_can");
            var shirt = session.Waste.First(i => i.Def.id == "tshirt");
            foreach (var waste in new[] { plank, jeans, tin, shirt }) session.TryClean(waste);
            yield return null;
            Check("temizlenen kot icin makas notu cikti", context.Notice.IsOpen);
            yield return Shot("1k_scissors_notice");
            yield return DismissNotices();
            yield return MoveTo(context.BackShelf.CellRect(session.AvailableShelf.ToList().IndexOf(jeans)).position, false);
            yield return new WaitForSecondsRealtime(0.6f);
            Check("kot ipucunda donusum satiri var", context.Tooltip.gameObject.activeSelf && context.Tooltip.ShowsCraft);
            yield return Shot("1k_craft_tooltip");
            yield return MoveTo(context.BackShelf.CellRect(session.AvailableShelf.ToList().IndexOf(plank)).position, false);
            yield return new WaitForSecondsRealtime(0.6f);
            Check("kilitli testere ipucunda gorunmez", context.Tooltip.gameObject.activeSelf && !context.Tooltip.ShowsCraft);
            yield return MoveTo(new Vector2(200, 400), false);
            Check("kilitli testere calismaz", session.Cut("saw", plank) == "feedback.tool_locked");
            Check("kot kesilince iki halat oldu", session.Cut("scissors", jeans) == null && session.Shelf.Count(i => i.Def.id == "rope") == 2);
            Check("sise kapagi saglam degil", !GameData.Items["bottle_cap"].tags.Contains("sturdy"));

            yield return ToggleRoom(switcher);
            yield return Click(context.Craft.MakeButtonPoint);
            Check("bos slotla birlestirilemez", session.Shelf.All(i => i.Product == null));
            yield return Shot("1l_rope_on_shelf");
            yield return Drag(context.FrontShelf.CellRect(1).position, context.Craft.SlotPoint(0));
            Check("esya slota kondu", session.Placements.Count == 1);
            yield return Drag(context.Craft.SlotPoint(0), new Vector2(200, 500));
            Check("slottaki esya rafa geri dondu", session.Placements.Count == 0);

            session.Place("pot", tin);
            session.Place("saucer", shirt);
            yield return null;
            yield return Click(context.Craft.MakeButtonPoint);
            var made = session.Shelf.FirstOrDefault(i => i.Product != null);
            Check("birlestirince urun rafa gecti", made != null && session.Placements.Count == 0);
            yield return Shot("2a_product_tinted");
            Check("urun makasla parcalanir", session.Cut("scissors", made) == null && session.Shelf.Contains(tin));

            yield return Click(hud.NotebookPoint);
            yield return Shot("2c_notebook_appointments");
            context.Notebook.Toggle();

            var carried = session.CleaningPoints;
            session.EndDay();
            session.StartDay(3);
            yield return null;
            Check("su sonraki gune aktarildi", session.CleaningPoints == carried + GameData.Days[2].cleaningPoints);
            var sawPlank = session.Waste.First(i => i.Def.id == "pallet_plank");
            session.TryClean(sawPlank);
            yield return null;
            Check("testere notu cikti", context.Notice.IsOpen);
            yield return Shot("2e_saw_notice");
            yield return DismissNotices();
            Check("testere tahtayi iki cubuk yapti", session.Cut("saw", sawPlank) == null && session.Shelf.Count(i => i.Def.id == "stick") >= 2);
            var sawTire = session.Waste.First(i => i.Def.id == "tire");
            session.TryClean(sawTire);
            Check("lastik iki tabaka oldu", session.Cut("saw", sawTire) == null
                && session.Shelf.Count(i => i.Def.id == "rubber_mat") == 2 && session.Shelf.First(i => i.Def.id == "rubber_mat").HasTag("sturdy"));
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Shot("2f_saw_station");

            session.EndDay();
            session.StartDay(5);
            yield return new WaitForSecondsRealtime(GameData.Config.roomSlideSeconds + 0.3f);
            Check("gizli siparis tarife eklendi", session.SeenProducts.Contains(session.CurrentOrder.product));
            yield return new WaitForSecondsRealtime(2.5f);
            Check("gizli siparis notu tusa kadar kaldi", context.Notice.IsOpen && context.Notice.Message == GameData.Text("feedback.notebook_hint"));
            yield return Shot("2b_hidden_order");
            yield return DismissNotices();
            var wrongPart = session.Shelf.First(i => i.Product == null && !i.HasTag("container") && !i.HasTag("transparent"));
            session.Place(GameData.Products[session.CurrentOrder.product].slots[0].id, wrongPart);
            yield return null;
            yield return Click(context.Craft.MakeButtonPoint);
            Check("gizli sipariste ozellik soylenmedi", context.Toast.Text == GameData.Text("feedback.hidden_wrong")
                || context.Toast.Text == GameData.Text("feedback.slot_empty", ("slot", GameData.Text("slot." + GameData.Products[session.CurrentOrder.product].slots[1].id))));
            session.ClearPlacements();
            yield return ToggleRoom(switcher);
            session.EndDay();
            session.StartDay(6);
            yield return new WaitForSecondsRealtime(0.2f);
            Check("temizlenmeyen atik sonraki gunlere kaldi", session.Waste.Contains(leftover));
            Check("alet kilidi acilis gununde kalkti", !context.Wash.IsToolShownLocked("needle"));
            Check("butun metallerde pas var", session.Waste.Where(i => i.Def.material == "metal").All(i => i.Treatments.Any(t => t.id == "rust")));
            Check("butun pantolonlarda yirtik var", session.Waste.Where(i => i.Def.id == "jeans").All(i => i.Treatments.Any(t => t.id == "tear")));
            yield return Shot("3a_pile");

            yield return Click(hud.NotebookPoint);
            yield return Shot("4a_notebook_appointments");
            for (var page = 1; page <= 4; page++)
            {
                context.Notebook.TurnForTest(page);
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Shot("4b_notebook_page" + page);
            }
            context.Notebook.Toggle();
            yield return null;

            var cracked = session.Waste.First(i => i.Treatments.Any(t => t.id == "crack"));
            session.BeginWash(cracked);
            yield return null;
            yield return Shot("3b_crack_masked");
            context.Wash.SelectTool("sponge");
            var spongeLimit = Time.unscaledTime + 15f;
            var jiggleX = 4f;
            while (context.Wash.PendingMarks().Any(m => m.mode == "rub") && Time.unscaledTime < spongeLimit)
            {
                jiggleX = -jiggleX;
                Send(context.Wash.PendingMarks().First(m => m.mode == "rub").point + new Vector2(jiggleX, 0), true);
                yield return null;
            }
            Send(pointer, false);
            yield return null;
            Check("sunger bitince alet birakildi", context.Wash.SelectedTool == null && session.Washing == cracked);
            yield return new WaitForSecondsRealtime(0.1f);
            yield return Shot("3b_sponge_sparkle");
            context.Wash.SelectTool("glue");
            yield return ScrubAll();
            Check("tutkal surulerek calisti", session.Washing == null && cracked.IsClean);
            yield return new WaitForSecondsRealtime(0.15f);
            yield return Shot("3c_confetti");
            yield return new WaitForSecondsRealtime(0.5f);

            var sanded = session.Waste.First(i => i.Treatments.Any(t => t.id == "rust"));
            session.BeginWash(sanded);
            yield return null;
            context.Wash.SelectTool("sandpaper");
            var rust = context.Wash.PendingMarks().First(m => m.mode == "shake");
            for (var i = 0; i < 40; i++)
            {
                Send(rust.point, true);
                yield return null;
            }
            Send(pointer, false);
            yield return null;
            Check("zimpara yavas gezdirince ise yaramaz", context.Wash.PendingMarks().Count(m => m.mode == "shake") == 3);
            yield return ScrubAll();
            Check("zimpara ileri-geri ile temizledi", session.Washing == null && sanded.IsClean);
            yield return new WaitForSecondsRealtime(0.5f);

            session.Shelf.RemoveRange(0, 2);
            var torn = session.Waste.First(i => i.Treatments.Any(t => t.id == "tear"));
            session.BeginWash(torn);
            yield return null;
            yield return Shot("3d_tear");
            context.Wash.SelectTool("needle");
            Check("igne ucundan tutuluyor", context.Wash.CursorPivot.x < 0.3f && context.Wash.CursorPivot.y < 0.3f);
            yield return ScrubAll();
            Check("igne noktalara tiklayarak dikti", session.Washing == null && torn.IsClean);
            yield return DismissNotices();
            yield return new WaitForSecondsRealtime(0.5f);

            var sawn = session.Waste.First(i => i.Def.id == "tire");
            session.Shelf.RemoveRange(0, 3);
            session.TryClean(sawn);
            Check("testere icin esya temiz ve rafta", sawn.IsClean && session.Shelf.Contains(sawn));
            yield return null;
            yield return DismissNotices();
            Check("testere masadaki esyaya alinir", session.PlaceOnBench(sawn) == null);
            yield return null;
            yield return Click(context.Targets["saw"].position);
            Check("testere ele alindi", context.Wash.SelectedStation == "saw");
            yield return Shot("3e_saw_in_hand");
            var sawCenter = (Vector2)((RectTransform)context.Wash.transform).position;
            var sawLimit = Time.unscaledTime + 8f;
            var sawOffset = 24f;
            while (session.OnBench != null && Time.unscaledTime < sawLimit)
            {
                sawOffset = -sawOffset;
                Send(sawCenter + new Vector2(sawOffset, 0), true);
                yield return new WaitForSecondsRealtime(0.05f);
            }
            Send(pointer, false);
            yield return null;
            Check("testere surterek kesti", session.OnBench == null && session.Shelf.Any(i => i.Def.id == "rubber_mat"));
            var gift = new ItemInstance(GameData.Items["bike_rim"], new TreatmentDef[0], true) { GiftFrom = "muhtar_selma" };
            Check("hediye adinda hediye eden yaziyor", gift.DisplayName.Contains(GameData.Text("gift.muhtar_selma")));

            session.Delivered["recep_amca"] = ItemInstance.MakeProduct(GameData.Products["shelf"], new Dictionary<string, ItemInstance> { { "board", plank } });
            session.Delivered["zeynep"] = ItemInstance.MakeProduct(GameData.Products["pen_holder"], new Dictionary<string, ItemInstance> { { "body", cracked } });
            session.FinalProducts.UnionWith(new[] { "swing", "bird_feeder", "planter", "wind_chime" });
            session.SpendWaterForTest(session.CleaningPoints + 1);
            yield return null;
            yield return Shot("6_water_below_zero");
            yield return Click(hud.EndDayPoint);
            Check("son gun onayi soruldu", context.Confirm.gameObject.activeSelf);
            yield return Click(context.Confirm.YesPoint);
            Check("su israfinda kazanilamaz", context.Final.TitleText == GameData.Text("ending.wasted"));
            Check("kuruma muzigi caliyor", AudioKit.CurrentMusic == "music_dry");
            Check("son gun artanlar anita gitti", session.Shelf.Count == 0 && session.Waste.Count == 0 && session.MonumentItems > 0);
            Check("final park sahnesi acildi", context.Final.gameObject.activeSelf);
            Check("kacan musteri cezasi uygulandi", session.MissedCustomers > 0
                && session.FinalStars == Mathf.Max(0, session.TotalStars - session.MissedCustomers));
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("7_ending");

            yield return Click(context.Final.ContinuePoint);
            yield return new WaitForSecondsRealtime(0.5f);
            Check("final ana menuye dondu", FindAnyObjectByType<MenuView>() != null);
            GameSettings.Language = "tr";

            Debug.Log("[DevTest] BITTI");
            Application.Quit();
        }

        Vector2 ScissorsPoint() => context.Targets["scissors"].position;

        Vector2 HudNotebookPoint() => FindAnyObjectByType<HudView>().NotebookPoint;

        IEnumerator DismissNotices()
        {
            while (context.Notice.IsOpen) yield return Click(context.Notice.NextPoint);
            yield return null;
        }

        IEnumerator TapEscape()
        {
            InputSystem.QueueStateEvent(Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState());
            yield return null;
            yield return null;
        }

        IEnumerator ToggleRoom(RoomSwitcher switcher)
        {
            switcher.Toggle();
            yield return new WaitForSecondsRealtime(GameData.Config.roomSlideSeconds + 0.3f);
        }

        // Kamyon sahnesi bitene dek bekler, sonra tusa basmis gibi tiklar.
        IEnumerator SkipCutscene(string shot)
        {
            yield return new WaitForSecondsRealtime(2.3f);
            Check("ara sahne oynuyor", FindAnyObjectByType<CutsceneClip>() != null);
            yield return Shot(shot);
            var limit = Time.unscaledTime + 15f;
            while (FindAnyObjectByType<CutsceneClip>() != null && Time.unscaledTime < limit)
            {
                yield return Click(new Vector2(640, 360));
                yield return new WaitForSecondsRealtime(0.4f);
            }
            Check("ara sahne tusla kapandi", FindAnyObjectByType<CutsceneClip>() == null);
            yield return new WaitForSecondsRealtime(0.3f);
        }

        RectTransform PileItem(System.Func<ItemInstance, bool> match = null) =>
            (RectTransform)FindObjectsByType<ItemDragSource>(FindObjectsSortMode.None)
                .First(s => s.GetComponentInParent<WastePileView>() != null
                    && (match == null || match(s.GetComponent<ItemView>().Item))).transform;

        IEnumerator Click(Vector2 target)
        {
            yield return MoveTo(target, false);
            yield return MoveTo(target, true);
            yield return MoveTo(target, false);
            yield return new WaitForSecondsRealtime(0.1f);
        }

        IEnumerator Drag(Vector2 from, Vector2 to)
        {
            yield return MoveTo(from, false);
            yield return MoveTo(from, true);
            yield return MoveTo(to, true);
            yield return MoveTo(to, false);
            yield return new WaitForSecondsRealtime(0.1f);
        }

        IEnumerator MoveTo(Vector2 target, bool pressed)
        {
            const int steps = 8;
            var start = pointer;
            for (var i = 1; i <= steps; i++)
            {
                Send(Vector2.Lerp(start, target, i / (float)steps), pressed);
                yield return null;
            }
        }

        // Dogru aleti secip kalan izleri tek tek temizler.
        IEnumerator ScrubAll()
        {
            var jiggle = 1f;
            var limit = Time.unscaledTime + 25f;
            while (session.Washing != null && Time.unscaledTime < limit)
            {
                var mark = context.Wash.PendingMarks().First();
                if (context.Wash.SelectedTool != mark.tool) context.Wash.SelectTool(mark.tool);
                jiggle = -jiggle;
                if (mark.mode == "stitch")
                {
                    Send(mark.point, false);
                    yield return null;
                    Send(mark.point, true);
                    yield return null;
                    Send(mark.point, false);
                    yield return null;
                    continue;
                }

                var offset = mark.mode == "rub" ? 4f : mark.mode == "shake" ? 22f : 0f;
                Send(mark.point + new Vector2(jiggle * offset, 0), true);
                yield return null;
                if (mark.mode == "shake") yield return null;
            }
            Send(pointer, false);
            yield return null;
        }

        void Send(Vector2 position, bool pressed)
        {
            var state = new MouseState { position = position, delta = position - pointer }.WithButton(MouseButton.Left, pressed);
            InputSystem.QueueStateEvent(Mouse.current, state);
            pointer = position;
        }

        IEnumerator Shot(string name)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
            yield return new WaitForSecondsRealtime(0.3f);
        }

        static void Check(string name, bool ok) => Debug.Log($"[DevTest] {(ok ? "GECTI" : "KALDI")}: {name}");
    }
}
#endif
