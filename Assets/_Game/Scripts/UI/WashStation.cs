using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Kenarda duran alet: uzerine basilinca oyuncunun eline gecer.
    public sealed class ToolPickup : MonoBehaviour, IPointerDownHandler
    {
        public System.Action Pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Pressed?.Invoke();
        }
    }

    // Calisma masasi: atik veya temiz esya buraya birakilir, buyur. Oyuncu soldaki aletle temizler,
    // sagdaki makas ve testereyle keser.
    public sealed class WashStation : MonoBehaviour, IDropHandler
    {
        const float MinMoveSqr = 0.25f;
        const float ToolColumnWidth = 118f;
        const float ToolSize = 84f;
        const float ToolGap = 8f;
        const float HeldToolAlpha = 0.25f;
        const float TraceRadius = 30f;
        const float DotClickRadius = 26f;
        const float NextDotAlpha = 0.7f;
        const float WaitingDotAlpha = 0.3f;
        const float MinStrokePixels = 16f;
        const float MaxStrokeSeconds = 0.4f;
        const float CursorAlpha = 0.7f;
        const float CursorSize = 72f;
        const int ConfettiCount = 28;
        const float ConfettiSeconds = 0.5f;
        const float ConfettiSpeed = 420f;
        const float CutPunchScale = 1.08f;
        const float CutPunchSpeed = 1.2f;

        sealed class ToolView
        {
            public ToolDef Def;
            public Image Icon;
            public Sprite Art;
        }

        UiContext context;
        GameSession session;
        readonly List<ToolView> toolViews = new List<ToolView>();
        RectTransform content;
        RectTransform stage;
        ItemView itemView;
        TMP_Text hint;
        GameObject cancelButton;
        Image toolCursor;
        readonly List<CareMark> marks = new List<CareMark>();
        ItemInstance current;
        ToolDef tool;
        StationDef station;
        int cutDone;
        Vector2 lastPointer;
        float strokeAnchorX;
        float strokeTime;
        int strokeDirection;

        public string HintText => hint.text;
        public int CancelFrame { get; private set; } = -1;
        bool Cutting => current != null && current == session.OnBench;

        public static WashStation Create(RectTransform content, UiContext context)
        {
            content.gameObject.AddComponent<Image>().color = Color.clear;
            var view = content.gameObject.AddComponent<WashStation>();
            view.context = context;
            view.session = context.Session;
            view.content = content;

            var size = GameData.Config.washItemSize;
            var center = new Vector2(0.5f, 0.5f);
            view.stage = UiKit.Area(content, "Stage", center, center);
            view.stage.sizeDelta = new Vector2(size, size);
            view.stage.anchoredPosition = new Vector2(-16, 12);
            view.itemView = ItemView.Create(view.stage);

            var cream = GameData.ParseColor(GameData.Config.palette.cream);
            view.hint = UiKit.Label(content, "Hint", string.Empty, 20, cream, TextAlignmentOptions.Bottom);
            view.hint.rectTransform.offsetMin = new Vector2(ToolColumnWidth, 8);
            view.hint.rectTransform.offsetMax = new Vector2(-180, 0);
            view.hint.textWrappingMode = TextWrappingModes.Normal;

            var button = UiKit.Button(content, "Cancel", GameData.Text("ui.sink.cancel"), new Vector2(1, 0), new Vector2(1, 0), out _);
            var rect = button.GetComponent<RectTransform>();
            rect.pivot = new Vector2(1, 0);
            rect.sizeDelta = new Vector2(170, 44);
            rect.anchoredPosition = Vector2.zero;
            button.onClick.AddListener(view.session.CancelWash);
            view.cancelButton = button.gameObject;

            view.BuildTools();
            view.BuildToolCursor();

            view.session.Changed += view.Refresh;
            view.Refresh();
            return view;
        }

        void BuildTools()
        {
            var y = 0f;
            var corner = new Vector2(0, 1);
            foreach (var def in GameData.Tools.Values)
            {
                var icon = UiKit.Panel(content, "Tool_" + def.id, corner, corner, Color.white);
                icon.rectTransform.pivot = corner;
                icon.rectTransform.sizeDelta = Vector2.one * ToolSize;
                icon.rectTransform.anchoredPosition = new Vector2(0, y);
                y -= ToolSize + ToolGap;
                icon.gameObject.AddComponent<ToolPickup>().Pressed = () => OnToolPressed(def);
                toolViews.Add(new ToolView { Def = def, Icon = icon, Art = Resources.Load<Sprite>("Sprites/Tools/" + def.id) });
                HoverHint.Attach(icon.gameObject, context, () => session.IsToolUnlocked(def)
                    ? (GameData.Text("tool." + def.id), GameData.Text($"tool.{def.id}.desc"))
                    : (GameData.Text("ui.tool.unknown"), GameData.Text("tool.locked.desc")));
            }
        }

        // Basilan alet ele gecer, tus birakilsa da elde kalir; eldeki alete tekrar basmak onu yerine koyar.
        void OnToolPressed(ToolDef def)
        {
            if (!session.IsToolUnlocked(def))
            {
                context.Toast.Show(GameData.Text("feedback.tool_locked"));
                return;
            }
            if (current == null)
            {
                context.Toast.Show(GameData.Text("feedback.tool_no_item"));
                return;
            }
            if (Cutting)
            {
                context.Toast.Show(GameData.Text("feedback.item_clean"));
                return;
            }
            if (tool == def)
            {
                ClearTool();
                hint.text = PickHint();
                return;
            }
            SelectTool(def.id);
            session.Signal("tool_selected");
        }

        // Makas ve testere de ele alinir; yalnizca masadaki temiz esyada ise yarar.
        public void OnStationPressed(StationDef def)
        {
            if (!session.IsStationUnlocked(def))
            {
                context.Toast.Show(GameData.Text("feedback.tool_locked"));
                return;
            }
            if (current == null)
            {
                context.Toast.Show(GameData.Text("feedback.tool_no_item"));
                return;
            }
            if (station == def)
            {
                ClearTool();
                hint.text = PickHint();
                return;
            }
            if (!Cutting)
            {
                context.Toast.Show(GameData.Text("feedback.clean_first"));
                return;
            }
            var reason = session.CutBlockReason(def.id, current);
            if (reason != null)
            {
                context.Toast.Show(GameData.Text(reason));
                return;
            }
            SelectStation(def.id);
            session.Signal("station_selected");
        }

        string PickHint() => GameData.Text(Cutting ? "ui.bench.pick_tool" : "ui.sink.pick_tool");

        string CutHint() => station == null ? PickHint()
            : GameData.Text("ui.bench.use." + station.id, ("done", cutDone), ("total", station.strokes));

        // Kilitli alet gri siluettir; eldeki aletin yerinde soluk izi kalir.
        void RefreshTools()
        {
            var locked = GameData.ParseColor(GameData.Config.palette.locked);
            foreach (var view in toolViews)
            {
                var open = session.IsToolUnlocked(view.Def);
                view.Icon.sprite = open ? view.Art : Silhouette.Of(view.Art);
                view.Icon.color = open
                    ? new Color(1, 1, 1, view.Def == tool ? HeldToolAlpha : 1)
                    : Color.Lerp(locked, Color.black, 0.35f);
            }
        }

        public bool IsToolShownLocked(string id) => !session.IsToolUnlocked(GameData.Tools[id]);

        void BuildToolCursor()
        {
            var center = new Vector2(0.5f, 0.5f);
            toolCursor = UiKit.Panel(context.Overlay, "ToolCursor", center, center, new Color(1, 1, 1, CursorAlpha));
            toolCursor.raycastTarget = false;
            toolCursor.rectTransform.sizeDelta = Vector2.one * CursorSize;
            toolCursor.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (session != null) session.Changed -= Refresh;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var source = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<ItemDragSource>() : null;
            if (source == null || source.DraggedItem == null) return;
            var item = source.DraggedItem;
            if (item.IsClean && !session.Shelf.Contains(item)) return;
            if (!item.IsClean && WrongForTutorial(item))
            {
                context.Toast.Show(GameData.Text("feedback.tutorial_wrong_item"));
                return;
            }
            source.Accept();
            var reason = item.IsClean ? session.PlaceOnBench(item) : session.BeginWash(item);
            if (reason != null) context.Toast.Show(GameData.Text(reason));
            else AudioKit.Play("drop");
        }

        // Rehber "isteğe uyan atigi" der: siparisin istedigi ozellik yoksa masaya kabul edilmez.
        bool WrongForTutorial(ItemInstance item)
        {
            var order = session.CurrentOrder;
            return context.Tutorial.Active && order != null && order.HasPref
                && order.pref.type == "tag_like" && !item.HasTag(order.pref.value);
        }

        public void SelectTool(string id)
        {
            tool = GameData.Tools[id];
            station = null;
            RefreshTools();
            SetCursor(id, tool.grip);
            if (current != null) hint.text = GameData.Text("ui.sink.use." + id);
        }

        public void SelectStation(string id)
        {
            station = GameData.Stations[id];
            tool = null;
            cutDone = 0;
            RefreshTools();
            SetCursor(id, null);
            hint.text = CutHint();
        }

        // grip: aletin imlecle tutuldugu nokta; verilmezse aletin ortasidir.
        void SetCursor(string id, float[] grip)
        {
            toolCursor.sprite = Resources.Load<Sprite>("Sprites/Tools/" + id);
            toolCursor.rectTransform.pivot = grip != null && grip.Length == 2 ? new Vector2(grip[0], grip[1]) : new Vector2(0.5f, 0.5f);
        }

        void ClearTool()
        {
            tool = null;
            station = null;
            toolCursor.gameObject.SetActive(false);
            RefreshTools();
        }

        void Refresh()
        {
            var next = session.Washing ?? session.OnBench;
            if (next != current)
            {
                current = next;
                ClearMarks();
                ClearTool();
                if (current != null)
                {
                    stage.gameObject.SetActive(true);
                    itemView.Show(current, false);
                    if (!Cutting) marks.AddRange(CareMarks.Build(itemView.MarksRoot, current));
                    foreach (var mark in marks.Where(m => m.Treatment.mode == "stitch"))
                    {
                        CareMarks.AddStitchDots(mark);
                        RefreshDots(mark);
                    }
                }
            }

            stage.gameObject.SetActive(current != null);
            cancelButton.SetActive(current != null && (context.Tutorial == null || !context.Tutorial.Active));
            RefreshTools();
            if (current == null) toolCursor.gameObject.SetActive(false);

            var blocked = session.CleanBlockReason();
            var idle = blocked ?? (session.CleaningPoints <= 0 ? "ui.sink.no_water" : "ui.sink.hint");
            if (blocked != null && session.AvailableShelf.Count > 0) idle = "ui.bench.hint";
            hint.text = Cutting ? CutHint() : GameData.Text(current == null ? idle
                : tool != null ? "ui.sink.use." + tool.id : "ui.sink.pick_tool");
        }

        void RefreshDots(CareMark mark)
        {
            var palette = GameData.Config.palette;
            for (var i = 0; i < mark.Dots.Count; i++)
            {
                mark.Dots[i].enabled = i >= mark.Progress;
                var color = GameData.ParseColor(i == mark.Progress ? palette.mustard : palette.cream);
                color.a = i == mark.Progress ? NextDotAlpha : WaitingDotAlpha;
                mark.Dots[i].color = color;
            }
        }

        void ClearMarks()
        {
            foreach (var mark in marks) Destroy(mark.Rect.gameObject);
            marks.Clear();
        }

        void Update()
        {
            if (current == null || context.Notice.IsOpen || context.Settings.IsOpen || context.Notebook.IsOpen) return;
            stage.localScale = Vector3.one * Mathf.MoveTowards(stage.localScale.x, 1f, CutPunchSpeed * Time.unscaledDeltaTime);
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !context.Tutorial.Active)
            {
                CancelFrame = Time.frameCount;
                session.CancelWash();
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;
            var pointer = mouse.position.ReadValue();

            if ((tool != null || station != null) && mouse.rightButton.wasPressedThisFrame)
            {
                ClearTool();
                hint.text = PickHint();
            }

            toolCursor.gameObject.SetActive((tool != null || station != null) && toolCursor.sprite != null
                && RectTransformUtility.RectangleContainsScreenPoint(content, pointer));
            toolCursor.transform.SetAsLastSibling();
            toolCursor.transform.position = pointer;

            var moved = (pointer - lastPointer).sqrMagnitude > MinMoveSqr;
            var stroke = mouse.leftButton.isPressed && DetectStroke(pointer);
            var previous = lastPointer;
            lastPointer = pointer;
            if (mouse.leftButton.isPressed && tool != null)
                Work(previous, pointer, moved, stroke, mouse.leftButton.wasPressedThisFrame, Time.unscaledDeltaTime);
            else if (mouse.leftButton.isPressed && station != null)
                CutWork(pointer, stroke, mouse.leftButton.wasPressedThisFrame);
        }

        // Makas tiklamayla, testere ileri-geri hareketle kesilir; gereken sayiya ulasinca esya donusur.
        void CutWork(Vector2 pointer, bool stroke, bool clicked)
        {
            var hit = station.mode == "shake" ? stroke : clicked;
            if (!hit || !RectTransformUtility.RectangleContainsScreenPoint(stage, pointer)) return;

            cutDone++;
            AudioKit.Play("drop");
            stage.localScale = Vector3.one * CutPunchScale;
            if (cutDone < station.strokes)
            {
                hint.text = CutHint();
                return;
            }

            var item = current;
            var id = station.id;
            ClearTool();
            var reason = session.Cut(id, item);
            if (reason != null)
            {
                context.Toast.Show(GameData.Text(reason));
                return;
            }
            AudioKit.Play("ding");
            StartCoroutine(Confetti());
        }

        // Yon degisimi bir zimpara darbesidir; yeterince uzun ve hizli olmayan hareket sayilmaz.
        bool DetectStroke(Vector2 pointer)
        {
            var delta = pointer.x - lastPointer.x;
            if (Mathf.Approximately(delta, 0)) return false;

            var direction = delta > 0 ? 1 : -1;
            if (direction == strokeDirection) return false;

            var counted = Mathf.Abs(lastPointer.x - strokeAnchorX) >= MinStrokePixels
                && Time.unscaledTime - strokeTime <= MaxStrokeSeconds;
            strokeDirection = direction;
            strokeAnchorX = lastPointer.x;
            strokeTime = Time.unscaledTime;
            return counted;
        }

        void Work(Vector2 from, Vector2 pointer, bool moved, bool stroke, bool clicked, float deltaTime)
        {
            TreatmentDef wrong = null;
            var worked = false;
            var scale = stage.lossyScale.x;
            foreach (var mark in marks.Where(m => m.Remaining > 0))
            {
                var inside = RectTransformUtility.RectangleContainsScreenPoint(mark.Rect, pointer);
                if (mark.Treatment.tool != tool.id)
                {
                    if (inside) wrong = mark.Treatment;
                    continue;
                }

                switch (mark.Treatment.mode)
                {
                    case "trace":
                        worked |= Trace(mark, from, pointer, TraceRadius * scale);
                        break;
                    case "stitch":
                        // Tek tik tek noktayi diker, yakin duran baska dikisi etkilemez.
                        if (clicked && !worked) worked = Stitch(mark, pointer, DotClickRadius * scale);
                        break;
                    case "shake":
                        if (inside && stroke)
                        {
                            worked = true;
                            Reduce(mark, 1f / mark.Treatment.strokes);
                        }
                        break;
                    default:
                        if (inside && moved)
                        {
                            worked = true;
                            Reduce(mark, deltaTime / mark.Treatment.seconds);
                        }
                        break;
                }
            }

            if (!worked && wrong != null)
            {
                hint.text = GameData.Text("feedback.wrong_tool", ("care", GameData.Text("care." + wrong.id)),
                    ("tool", GameData.Text("tool." + wrong.tool)));
            }

            if (marks.All(m => m.Remaining <= 0))
            {
                var item = current;
                AudioKit.Play("ding");
                session.FinishWash();
                StartCoroutine(FlyToShelf(item));
                StartCoroutine(Confetti());
            }
            else if (worked && marks.All(m => m.Remaining <= 0 || m.Treatment.tool != tool.id))
            {
                // Bu aletin isi bitti: pirilti cikar, alet birakilir ve oyuncu siradakine gecer.
                AudioKit.Play("ding");
                StartCoroutine(Confetti());
                ClearTool();
                hint.text = PickHint();
            }
        }

        static void Reduce(CareMark mark, float amount)
        {
            mark.Remaining -= amount;
            mark.Group.alpha = Mathf.Max(0, mark.Remaining);
        }

        // Tutkal surulur: imlecin gectigi hat uzerindeki kirik parcalar kapanir, her siradan olabilir.
        static bool Trace(CareMark mark, Vector2 from, Vector2 to, float radius)
        {
            var closed = false;
            for (var i = 0; i < mark.Segments.Count; i++)
            {
                if (!mark.Segments[i].enabled || DistanceToLine(mark.SegmentCenter(i), from, to) > radius) continue;
                mark.Segments[i].enabled = false;
                mark.Progress++;
                closed = true;
            }
            if (mark.Progress >= mark.Segments.Count) mark.Finish();
            return closed;
        }

        static float DistanceToLine(Vector2 point, Vector2 a, Vector2 b)
        {
            var line = b - a;
            var t = line.sqrMagnitude < 0.01f ? 0 : Mathf.Clamp01(Vector2.Dot(point - a, line) / line.sqrMagnitude);
            return Vector2.Distance(point, a + line * t);
        }

        bool Stitch(CareMark mark, Vector2 pointer, float radius)
        {
            var dot = mark.Dots[mark.Progress];
            if (Vector2.Distance(pointer, dot.rectTransform.position) > radius) return false;

            mark.Segments[mark.Progress].enabled = false;
            mark.Progress++;
            if (mark.Progress >= mark.Dots.Count) mark.Finish();
            else RefreshDots(mark);
            return true;
        }

        // Temizlik bitince beyaz parcaciklar patlar.
        IEnumerator Confetti()
        {
            var origin = (Vector2)stage.position;
            var pieces = new (RectTransform rect, Vector2 velocity, Image image)[ConfettiCount];
            for (var i = 0; i < ConfettiCount; i++)
            {
                var image = UiKit.Panel(context.Overlay, "Confetti", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
                image.raycastTarget = false;
                image.rectTransform.sizeDelta = Vector2.one * Random.Range(4f, 9f);
                image.rectTransform.position = origin;
                image.rectTransform.localEulerAngles = new Vector3(0, 0, Random.Range(0f, 90f));
                pieces[i] = (image.rectTransform, Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f) * ConfettiSpeed, image);
            }

            for (var t = 0f; t < ConfettiSeconds; t += Time.unscaledDeltaTime)
            {
                foreach (var piece in pieces)
                {
                    piece.rect.position += (Vector3)(piece.velocity * Time.unscaledDeltaTime);
                    var color = piece.image.color;
                    color.a = 1f - t / ConfettiSeconds;
                    piece.image.color = color;
                }
                yield return null;
            }
            foreach (var piece in pieces) Destroy(piece.rect.gameObject);
        }

        // Temizlenen esya tezgahtan kalkip raftaki yerine dizilir.
        IEnumerator FlyToShelf(ItemInstance item)
        {
            var index = session.AvailableShelf.ToList().IndexOf(item);
            if (index < 0) yield break;

            var ghost = ItemView.Create(context.Overlay);
            ghost.Show(item);
            var rect = (RectTransform)ghost.transform;
            var center = new Vector2(0.5f, 0.5f);
            rect.anchorMin = rect.anchorMax = center;
            rect.sizeDelta = stage.rect.size;

            var target = context.BackShelf.CellRect(index);
            var start = stage.position;
            var startSize = stage.rect.size;
            var duration = GameData.Config.flyToShelfSeconds;
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                var eased = Mathf.SmoothStep(0, 1, t);
                rect.position = Vector3.Lerp(start, target.position, eased);
                rect.sizeDelta = Vector2.Lerp(startSize, target.rect.size, eased);
                yield return null;
            }
            Destroy(ghost.gameObject);
        }

        // Otomatik test icin: bitmemis izlerin siradaki hedefi, aleti ve kullanim modu.
        public IEnumerable<(Vector2 point, string tool, string mode)> PendingMarks() =>
            marks.Where(m => m.Remaining > 0).Select(m => (NextTarget(m), m.Treatment.tool, m.Treatment.mode));

        static Vector2 NextTarget(CareMark mark)
        {
            if (mark.Treatment.mode == "stitch") return mark.Dots[mark.Progress].rectTransform.position;
            if (mark.Treatment.mode != "trace") return mark.Rect.position;
            var open = mark.Segments.FindIndex(s => s.enabled);
            return mark.SegmentCenter(open);
        }

        public string SelectedTool => tool != null ? tool.id : null;
        public string SelectedStation => station != null ? station.id : null;
        public Vector2 CursorPivot => toolCursor.rectTransform.pivot;
        public bool CancelVisible => cancelButton.activeSelf;

        public Vector2 ToolPoint(string id)
        {
            var rect = toolViews.First(v => v.Def.id == id).Icon.rectTransform;
            return rect.TransformPoint(rect.rect.center);
        }
    }
}
