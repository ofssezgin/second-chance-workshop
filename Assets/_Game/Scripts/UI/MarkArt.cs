using System.Collections.Generic;
using UnityEngine;

namespace CopAtolyesi.UI
{
    // Leke ve pas izlerini yumusak kenarli dokular olarak uretir. Beyaz tonlarda cizilir,
    // rengi care.json'dan gelir.
    public static class MarkArt
    {
        public const int Variants = 8;

        const int StainSize = 96;
        const int SpeckleSize = 128;
        const int SpeckleCount = 1400;

        static readonly Dictionary<(string, int), Sprite> cache = new Dictionary<(string, int), Sprite>();

        public static Sprite Get(string shape, int variant)
        {
            var key = (shape, variant);
            if (cache.TryGetValue(key, out var sprite)) return sprite;

            var random = new System.Random(shape.Length * 97 + variant * 31 + 7);
            var texture = shape == "speckle" ? DrawSpeckle(random) : DrawStain(random);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
            cache[key] = sprite;
            return sprite;
        }

        static Texture2D NewTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var clear = new Color32[size * size];
            for (var i = 0; i < clear.Length; i++) clear[i] = new Color32(255, 255, 255, 0);
            texture.SetPixels32(clear);
            return texture;
        }

        // Duzensiz bir kenar: farkli sikliktaki dalgalar yaricapi bozar.
        static float[] EdgeWaves(System.Random random)
        {
            return new[]
            {
                (float)random.NextDouble() * 6.28f, (float)random.NextDouble() * 6.28f, (float)random.NextDouble() * 6.28f,
                0.14f + (float)random.NextDouble() * 0.12f, 0.08f + (float)random.NextDouble() * 0.1f, 0.05f + (float)random.NextDouble() * 0.07f
            };
        }

        static float EdgeRadius(float angle, float[] waves)
        {
            return 0.72f + Mathf.Sin(2 * angle + waves[0]) * waves[3] + Mathf.Sin(3 * angle + waves[1]) * waves[4]
                + Mathf.Sin(7 * angle + waves[2]) * waves[5];
        }

        static Texture2D DrawStain(System.Random random)
        {
            var texture = NewTexture(StainSize);
            var waves = EdgeWaves(random);
            var center = (StainSize - 1) / 2f;
            var radius = StainSize * 0.4f;
            var noise = (float)random.NextDouble() * 100f;

            for (var y = 0; y < StainSize; y++)
            {
                for (var x = 0; x < StainSize; x++)
                {
                    var dx = (x - center) / radius;
                    var dy = (y - center) / radius;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var edge = EdgeRadius(Mathf.Atan2(dy, dx), waves);
                    var alpha = Mathf.Clamp01((edge - distance) * radius * 0.5f);
                    if (alpha <= 0) continue;
                    var shade = 0.82f + Mathf.PerlinNoise(noise + x * 0.09f, noise + y * 0.09f) * 0.18f;
                    texture.SetPixel(x, y, new Color(shade, shade, shade, alpha));
                }
            }

            var drops = 3 + random.Next(4);
            for (var i = 0; i < drops; i++)
            {
                var angle = (float)random.NextDouble() * 6.28f;
                var reach = radius * (EdgeRadius(angle, waves) + 0.1f + (float)random.NextDouble() * 0.14f);
                DrawDot(texture, center + Mathf.Cos(angle) * reach, center + Mathf.Sin(angle) * reach,
                    1.5f + (float)random.NextDouble() * 2f, 0.85f, 1f);
            }
            return texture;
        }

        // Pas: bolgeyi kaplayan cok sayida yumusak kucuk nokta; merkezde sik, kenara dogru seyrek.
        static Texture2D DrawSpeckle(System.Random random)
        {
            var texture = NewTexture(SpeckleSize);
            var waves = EdgeWaves(random);
            var center = (SpeckleSize - 1) / 2f;
            var radius = SpeckleSize * 0.46f;

            for (var i = 0; i < SpeckleCount; i++)
            {
                var x = (float)random.NextDouble() * SpeckleSize;
                var y = (float)random.NextDouble() * SpeckleSize;
                var dx = (x - center) / radius;
                var dy = (y - center) / radius;
                var fade = Mathf.Sqrt(dx * dx + dy * dy) / EdgeRadius(Mathf.Atan2(dy, dx), waves);
                if (fade > 1.2f || random.NextDouble() < fade * 0.85f) continue;

                var tone = random.NextDouble();
                var shade = tone < 0.5 ? 1f : tone < 0.8 ? 0.8f : 0.6f;
                var size = 0.8f + (float)random.NextDouble() * 1.8f;
                DrawDot(texture, x, y, size, shade, 0.6f + (float)random.NextDouble() * 0.4f);
            }
            return texture;
        }

        // Kenari yumusatilmis kucuk daire; altindaki daha opak noktayi ezmez.
        static void DrawDot(Texture2D texture, float x, float y, float radius, float shade, float opacity)
        {
            var reach = Mathf.CeilToInt(radius + 1);
            for (var py = Mathf.FloorToInt(y) - reach; py <= y + reach; py++)
            {
                for (var px = Mathf.FloorToInt(x) - reach; px <= x + reach; px++)
                {
                    if (px < 0 || py < 0 || px >= texture.width || py >= texture.height) continue;
                    var distance = Vector2.Distance(new Vector2(px, py), new Vector2(x, y));
                    var alpha = Mathf.Clamp01(radius + 0.5f - distance) * opacity;
                    if (alpha > texture.GetPixel(px, py).a) texture.SetPixel(px, py, new Color(shade, shade, shade, alpha));
                }
            }
        }
    }
}
