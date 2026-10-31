using System.Collections.Generic;
using UnityEngine;

namespace CopAtolyesi.UI
{
    // Bir cizimin yalnizca dis seklini tasiyan beyaz kopyasini uretir; renk Image.color'dan gelir.
    public static class Silhouette
    {
        static readonly Dictionary<Sprite, Sprite> cache = new Dictionary<Sprite, Sprite>();

        public static Sprite Of(Sprite art)
        {
            if (cache.TryGetValue(art, out var result)) return result;
            if (!art.texture.isReadable) return art;

            var area = art.rect;
            var pixels = art.texture.GetPixels((int)area.x, (int)area.y, (int)area.width, (int)area.height);
            for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color(1, 1, 1, pixels[i].a);

            var texture = new Texture2D((int)area.width, (int)area.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            texture.SetPixels(pixels);
            texture.Apply();
            result = Sprite.Create(texture, new Rect(0, 0, area.width, area.height), new Vector2(0.5f, 0.5f), art.pixelsPerUnit);
            cache[art] = result;
            return result;
        }
    }
}
