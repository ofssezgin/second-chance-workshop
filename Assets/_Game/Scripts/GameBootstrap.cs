using System.Linq;
using CopAtolyesi.Core;
using CopAtolyesi.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CopAtolyesi
{
    // Oyunun giris noktasi: veriyi yukler, menuyu acar; Basla'ya basilinca oyun arayuzunu kurar.
    // Metinler kurulurken okundugu icin dil degisimi menude yapilir.
    public sealed class GameBootstrap : MonoBehaviour
    {
        GameSession session;
        RoomSwitcher rooms;
        UiContext context;
        CutscenePlayer cutscenes;
        RectTransform canvas;
        MenuView menu;

        public UiContext Context => context;
        public MenuView Menu => menu;

        void Awake()
        {
            GameData.Load();
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD && !UNITY_WEBGL
            // Unity son pencere boyutunu kaydettigi icin ekran modu her acilista zorlanir.
            Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
#endif
            AudioKit.Ensure();
            BuildEventSystem();
            canvas = BuildCanvas(GameData.Config);
            menu = MenuView.Create(canvas, StartGame);
#if DEVELOPMENT_BUILD
            var args = System.Environment.GetCommandLineArgs();
            var testArg = System.Array.IndexOf(args, "-selftest");
            if (testArg >= 0) Dev.DevSelfTest.Launch(this, args[testArg + 1]);
#endif
        }

        void StartGame()
        {
            Destroy(menu.gameObject);
            session = new GameSession();
            var config = GameData.Config;

            var overlay = UiKit.Area(canvas, "Overlay", Vector2.zero, Vector2.one);
            context = new UiContext
            {
                Session = session,
                Overlay = overlay,
                Tooltip = TooltipView.Create(overlay),
                Toast = ToastView.Create(overlay)
            };

            var track = UiKit.Area(canvas, "Rooms", Vector2.zero, Vector2.one);
            RoomScreens.BuildBackRoom(UiKit.Area(track, "BackRoom", Vector2.zero, Vector2.one), context);
            RoomScreens.BuildFrontRoom(UiKit.Area(track, "FrontRoom", Vector2.right, new Vector2(2, 1)), context);

            rooms = canvas.gameObject.AddComponent<RoomSwitcher>();
            context.Notebook = NotebookView.Create(overlay, session);
            var hud = HudView.Create(canvas, context, () => rooms.Toggle(), RequestEndDay, context.Notebook.Toggle, () => context.Settings.Toggle());
            context.Targets["door"] = hud.DoorRect;
            context.Targets["end_day"] = hud.EndDayRect;
            context.Targets["settings"] = hud.SettingsRect;
            context.Tutorial = TutorialView.Create(overlay, context);
            context.Summary = SummaryView.Create(overlay);
            context.Final = FinalView.Create(overlay);
            context.Pause = PauseView.Create(overlay, () => SceneManager.LoadScene(0));
            context.Settings = SettingsView.Create(overlay);
            context.Confirm = ConfirmView.Create(overlay);
            context.Notice = NoticeView.Create(overlay, session);
            rooms.RoomChanged += session.SetPlayerRoom;
            rooms.RoomChanged += hud.SetRoom;
            overlay.SetAsLastSibling();

            Canvas.ForceUpdateCanvases();
            var startInFront = config.startRoom == "front";
            rooms.Init(track, config.roomSlideSeconds, startInFront);
            hud.SetRoom(startInFront);

            cutscenes = gameObject.AddComponent<CutscenePlayer>();
            StartCoroutine(BeginDay(GameData.Days[0].day));
        }

        // Gun verisi once yuklenir: kamyon gibi ara sahneler o gunun esyalarini context'ten okuyabilir.
        System.Collections.IEnumerator BeginDay(int dayNumber)
        {
            if (rooms.InFrontRoom) rooms.Toggle();
            session.StartDay(dayNumber);
            yield return cutscenes.Play(session.Day.introCutscene, context);
            ShowDayIntro();
        }

        // Randevu Defteri: bugunun musterileri, su hakki ve gelen atik. Surpriz musteri burada yazmaz.
        void ShowDayIntro()
        {
            var day = session.Day;
            var water = GameData.Text("ui.intro.water", ("water", session.CleaningPoints), ("waste", day.manifest.Length));
            var lines = $"{GameData.Text($"day.{day.day}.note")}\n\n{water}";
            var title = GameData.Text($"day.{day.day}.title");
            void ShowCard() => context.Summary.Show(title, lines, GameData.Text("ui.intro.open"), () =>
            {
                if (day.day == GameData.Tutorial.day) context.Tutorial.Begin();
                session.Signal("day_open");
            });

            // Ilk gun randevulardan once kisa hikaye girisi gelir.
            if (day.day == GameData.Days[0].day)
                context.Summary.Show(GameData.Text("story.title"), GameData.Text("story.text"), GameData.Text("ui.summary.next"), ShowCard);
            else
                ShowCard();
        }

        // Dukkanda hala musteri varsa gun bitmeden once sorulur.
        void RequestEndDay()
        {
            if (session.Queue.Count > 0) context.Confirm.Show(GameData.Text("ui.confirm.end_day"), FinishDay);
            else FinishDay();
        }

        void FinishDay()
        {
            var served = session.ServedToday;
            var total = session.OrdersToday;
            var stars = session.StarsToday;
            var day = session.DayNumber;
            session.EndDay();

            if (session.HasNextDay)
            {
                var lines = GameData.Text("ui.summary.lines", ("served", served), ("total", total), ("stars", stars),
                    ("shelf", session.Shelf.Count), ("waste", session.Waste.Count), ("water", session.CleaningPoints));
                context.Summary.Show(GameData.Text("ui.summary.day", ("title", GameData.Text($"day.{day}.title"))), lines, GameData.Text("ui.summary.next"),
                    () => StartCoroutine(BeginDay(day + 1)));
                return;
            }

            var ratio = session.MaxStars > 0 ? session.FinalStars / (float)session.MaxStars : 0f;
            // Su israf edildiyse yildiz ne olursa olsun oyun kazanilmaz.
            var wasted = session.WaterWasted;
            var title = wasted ? "ending.wasted" : ratio >= 0.85f ? "ending.legend" : ratio >= 0.6f ? "ending.master" : "ending.apprentice";
            var ending = GameData.Text("ui.ending.lines", ("stars", session.FinalStars), ("max", session.MaxStars),
                ("missed", session.MissedCustomers), ("penalty", session.MissedPenalty), ("monument", session.MonumentItems),
                ("water", session.CleaningPoints));
            ending += "\n" + GameData.Text(wasted ? "ending.wasted_story" : "ending.story");
            context.Final.Show(session, GameData.Text(title), ending, GameData.Text("ui.ending.again"), () => SceneManager.LoadScene(0));
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || cutscenes == null || cutscenes.Playing || context.Notice.IsOpen || context.Settings.IsOpen) return;
            if (context.Tutorial.Active) return;
            if (context.Notebook.IsOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                context.Notebook.Toggle();
                return;
            }
            var panelOpen = context.Summary.gameObject.activeSelf || context.Final.gameObject.activeSelf;
            if (keyboard.escapeKey.wasPressedThisFrame && !panelOpen && session.Washing == null
                && context.Wash.CancelFrame != Time.frameCount)
                context.Pause.Toggle();
            if (context.Pause.gameObject.activeSelf) return;
            if (keyboard.tabKey.wasPressedThisFrame) rooms.Toggle();
            if (keyboard.nKey.wasPressedThisFrame) context.Notebook.Toggle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (keyboard.f2Key.wasPressedThisFrame && session.HasNextDay)
            {
                session.EndDay();
                StartCoroutine(BeginDay(session.DayNumber + 1));
            }
            if (keyboard.f4Key.wasPressedThisFrame)
            {
                foreach (var item in session.Waste.ToList()) session.TryClean(item);
            }
#endif
        }

        static RectTransform BuildCanvas(GameConfig config)
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(config.referenceWidth, config.referenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
            return go.GetComponent<RectTransform>();
        }

        static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
