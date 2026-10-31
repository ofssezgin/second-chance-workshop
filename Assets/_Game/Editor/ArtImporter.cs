using UnityEditor;
using UnityEngine;

namespace CopAtolyesi.EditorTools
{
    // Resources/Sprites altina konan PNG'ler elle ayar gerektirmeden net pixel art olarak iceri alinir.
    public sealed class ArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/Sprites/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            if (assetPath.EndsWith("/belt.png")) importer.wrapMode = TextureWrapMode.Repeat;
        }
    }
}
