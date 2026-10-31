using CopAtolyesi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    // Arayuz koddan kurulur: yerlesim oranlari config.json'dan gelir, sahnede elle ayar gerekmez.
    public static class UiKit
    {
        static Sprite roundedSprite;
        static Sprite circleSprite;

        public static Sprite Rounded => roundedSprite != null ? roundedSprite : roundedSprite = CreateSprite(12f);
        public static Sprite Circle => circleSprite != null ? circleSprite : circleSprite = CreateSprite(24f);

        static Sprite CreateSprite(float radius)
        {
            const int size = 48;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius), 0);
                    var dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius), 0);
                    var alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100,
                0, SpriteMeshType.FullRect, Vector4.one * (radius + 1));
        }

        public static RectTransform Area(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color, bool rounded = false)
        {
            var image = Area(parent, name, anchorMin, anchorMax).gameObject.AddComponent<Image>();
            image.color = color;
            if (rounded)
            {
                image.sprite = Rounded;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        public static TMP_Text Label(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var label = Area(parent, name, Vector2.zero, Vector2.one).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        public static Button Button(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, out TMP_Text label)
        {
            var palette = GameData.Config.palette;
            var image = Panel(parent, name, anchorMin, anchorMax, GameData.ParseColor(palette.teal), true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => AudioKit.Play("click"));
            label = Label(image.transform, "Label", text, 18, GameData.ParseColor(palette.cream));
            return button;
        }

        // Sag ust koseye oturan disli dugmesi.
        public static Button GearButton(Transform parent, float size, float margin)
        {
            var button = Button(parent, "Gear", string.Empty, Vector2.one, Vector2.one, out _);
            var rect = button.GetComponent<RectTransform>();
            rect.offsetMin = new Vector2(-margin - size, -margin - size);
            rect.offsetMax = new Vector2(-margin, -margin);

            var icon = Panel(button.transform, "Icon", Vector2.zero, Vector2.one, Color.white);
            icon.raycastTarget = false;
            Pad(icon.rectTransform, 6, 6, 6, 6);
            ApplyArt(icon, "Sprites/Ui/gear");
            return button;
        }

        public static Slider VolumeSlider(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            const float handleWidth = 26f;
            var palette = GameData.Config.palette;
            var root = Area(parent, "Slider", anchorMin, anchorMax);
            Panel(root, "Track", new Vector2(0, 0.3f), new Vector2(1, 0.7f), GameData.ParseColor(palette.anthracite), true);

            var fillArea = Area(root, "FillArea", new Vector2(0, 0.3f), new Vector2(1, 0.7f));
            var fill = Panel(fillArea, "Fill", Vector2.zero, new Vector2(0, 1), GameData.ParseColor(palette.teal), true);
            fill.raycastTarget = false;

            var handleArea = Area(root, "HandleArea", Vector2.zero, Vector2.one);
            var handle = Panel(handleArea, "Handle", Vector2.zero, new Vector2(0, 1), GameData.ParseColor(palette.mustard), true);
            handle.rectTransform.sizeDelta = new Vector2(handleWidth, 0);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            return slider;
        }

        // Cizim varsa paneli ona cevirir, yoksa panel duz rengiyle kalir.
        public static void ApplyArt(Image image, string path)
        {
            var art = Resources.Load<Sprite>(path);
            if (art == null) return;
            image.sprite = art;
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }

        public static void Pad(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
