using System.Collections;
using System.Linq;
using CopAtolyesi.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Gun basi: kamyon gelir, arkasindan ekranin sagina bant uzanir, gunun atiklari banttan gecer.
    // Sprites/Cutscene altinda truck, belt, background yoksa beyaz kare ve duz renkler kullanilir.
    public sealed class TruckCutscene : MonoBehaviour
    {
        const float MaxSeconds = 600f;
        const float DayLabelHeight = 130f;
        const float FactTop = 112f;
        const float FactBottom = 216f;
        const float SourceHeight = 26f;
        const float BeltSurface = 0.86f;

        TruckCutsceneConfig config;
        CutsceneClip clip;
        RectTransform root;
        RectTransform truck;
        RectTransform belt;
        RawImage beltImage;
        float truckWidth;
        float beltScroll;
        TMPro.TMP_Text prompt;
        bool waitingForKey;

        public static GameObject Create(Transform parent)
        {
            var palette = GameData.Config.palette;
            var background = UiKit.Panel(parent, "TruckCutscene", Vector2.zero, Vector2.one, GameData.ParseColor(palette.wall));
            UiKit.ApplyArt(background, "Sprites/Cutscene/background");

            var view = background.gameObject.AddComponent<TruckCutscene>();
            view.config = GameData.Config.truckCutscene;
            view.root = background.rectTransform;
            view.clip = background.gameObject.AddComponent<CutsceneClip>();
            view.clip.duration = MaxSeconds;
            view.Build();
            return background.gameObject;
        }

        void Build()
        {
            var bottomLeft = Vector2.zero;
            var beltColor = GameData.ParseColor(GameData.Config.palette.anthracite);

            belt = UiKit.Area(root, "Belt", bottomLeft, bottomLeft);
            belt.pivot = bottomLeft;
            beltImage = belt.gameObject.AddComponent<RawImage>();
            var tile = Resources.Load<Texture2D>("Sprites/Cutscene/belt");
            beltImage.texture = tile != null ? tile : CreateBeltTile(beltColor);
            beltImage.texture.wrapMode = TextureWrapMode.Repeat;
            beltImage.raycastTarget = false;

            var truckImage = UiKit.Panel(root, "Truck", bottomLeft, bottomLeft, Color.white);
            truckImage.raycastTarget = false;
            truck = truckImage.rectTransform;
            truck.pivot = bottomLeft;

            var art = Resources.Load<Sprite>("Sprites/Cutscene/truck");
            truckImage.sprite = art;
            truck.sizeDelta = art != null ? art.rect.size * config.artScale : Vector2.one * config.truckSize;
            truckWidth = truck.sizeDelta.x;
            truck.anchoredPosition = new Vector2(-truckWidth, config.truckY);

            belt.sizeDelta = new Vector2(0, config.beltHeight);
            belt.anchoredPosition = new Vector2(config.truckStopX + truckWidth, config.groundY);
        }

        static Texture2D CreateBeltTile(Color color)
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var bar = Color.Lerp(color, Color.white, 0.25f);
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                    texture.SetPixel(x, y, x < 3 ? bar : color);
            texture.Apply();
            return texture;
        }

        void BuildDayLabel()
        {
            var session = clip.Context.Session;
            var ink = GameData.ParseColor(GameData.Config.palette.anthracite);
            var area = UiKit.Area(root, "DayLabel", new Vector2(0, 1), Vector2.one);
            area.offsetMin = new Vector2(0, -DayLabelHeight);
            area.offsetMax = Vector2.zero;
            UiKit.Label(area, "Text", GameData.Text($"day.{session.DayNumber}.title"), 56, ink);

            var factArea = UiKit.Area(root, "Fact", new Vector2(0.06f, 1), new Vector2(0.94f, 1));
            factArea.offsetMin = new Vector2(0, -FactBottom);
            factArea.offsetMax = new Vector2(0, -FactTop);
            var fact = UiKit.Label(factArea, "Text", GameData.Text("ui.summary.fact",
                ("fact", GameData.Text($"day.{session.DayNumber}.fact"))), 25, ink, TMPro.TextAlignmentOptions.Top);
            fact.textWrappingMode = TMPro.TextWrappingModes.Normal;
            UiKit.UseDisplayFont(fact);

            var sourceArea = UiKit.Area(root, "Source", new Vector2(0.06f, 1), new Vector2(0.94f, 1));
            sourceArea.offsetMin = new Vector2(0, -FactBottom - SourceHeight);
            sourceArea.offsetMax = new Vector2(0, -FactBottom);
            UiKit.Label(sourceArea, "Text", GameData.Text($"day.{session.DayNumber}.source"), 18, ink, TMPro.TextAlignmentOptions.MidlineRight);

            var promptArea = UiKit.Area(root, "Prompt", Vector2.zero, new Vector2(1, 0));
            promptArea.offsetMax = new Vector2(0, 44);
            prompt = UiKit.Label(promptArea, "Text", GameData.Text("ui.cutscene.continue"), 24, GameData.ParseColor(GameData.Config.palette.cream));
            prompt.enabled = false;
        }

        IEnumerator Start()
        {
            BuildDayLabel();
            yield return DriveTruckIn();
            yield return ExtendBelt();

            var session = clip.Context.Session;
            var todays = session.Waste.Take(session.Day.manifest.Length).ToList();
            foreach (var item in todays)
            {
                StartCoroutine(MoveOnBelt(item));
                yield return Wait(config.itemIntervalSeconds);
            }

            var travel = (root.rect.width - belt.anchoredPosition.x + config.itemSize) / config.itemSpeed;
            yield return Wait(travel + config.endHoldSeconds);
            prompt.enabled = true;
            waitingForKey = true;
        }

        IEnumerator DriveTruckIn()
        {
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / config.truckSeconds)
            {
                var eased = 1f - (1f - t) * (1f - t);
                truck.anchoredPosition = new Vector2(Mathf.Lerp(-truckWidth, config.truckStopX, eased), config.truckY);
                yield return null;
            }
            truck.anchoredPosition = new Vector2(config.truckStopX, config.truckY);
        }

        IEnumerator ExtendBelt()
        {
            var full = root.rect.width - belt.anchoredPosition.x;
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / config.beltSeconds)
            {
                belt.sizeDelta = new Vector2(full * Mathf.SmoothStep(0, 1, t), config.beltHeight);
                yield return null;
            }
            belt.sizeDelta = new Vector2(full, config.beltHeight);
        }

        IEnumerator MoveOnBelt(ItemInstance item)
        {
            var view = ItemView.Create(root);
            view.Show(item);
            var rect = (RectTransform)view.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = Vector2.one * config.itemSize;
            truck.SetAsLastSibling();

            // Cizimin altindaki bos pikseller dusulur: her esya bandin yuzeyine tam oturur.
            var y = config.groundY + config.beltHeight * BeltSurface - BottomGap(item) * config.itemSize;
            var x = belt.anchoredPosition.x - config.itemSize;
            var end = root.rect.width + config.itemSize;
            while (x < end)
            {
                rect.anchoredPosition = new Vector2(x, y);
                x += config.itemSpeed * Time.unscaledDeltaTime;
                yield return null;
            }
            Destroy(view.gameObject);
        }

        // Cizimin altindaki bos seridin, cizim yuksekligine orani.
        static float BottomGap(ItemInstance item)
        {
            var art = Resources.Load<Sprite>(item.Def.sprite);
            if (art == null || !art.texture.isReadable) return 0;
            var area = art.rect;
            for (var y = 0; y < area.height; y++)
                for (var x = 0; x < area.width; x++)
                    if (art.texture.GetPixel((int)area.x + x, (int)area.y + y).a > 0.5f)
                        return y / area.height;
            return 0;
        }

        static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        void Update()
        {
            var tile = config.beltTileSize * config.artScale;
            beltScroll -= config.itemSpeed * Time.unscaledDeltaTime / tile;
            beltImage.uvRect = new Rect(beltScroll, 0, belt.sizeDelta.x / tile, 1);

            if (!waitingForKey) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if ((keyboard != null && keyboard.anyKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame))
                clip.Finished = true;
        }
    }
}
