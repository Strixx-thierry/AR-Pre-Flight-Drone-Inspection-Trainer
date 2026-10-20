using System.IO;
using UnityEditor;
using UnityEngine;

namespace DroneTrainer.EditorTools
{
    /// <summary>
    /// Every PNG dropped into Assets/_Project/Art/UI is imported as a crisp UI sprite with the right
    /// 9-slice borders — no Sprite Editor work needed. Art is rendered at 2x, so PPU = 200.
    /// </summary>
    public class UIArtPostprocessor : AssetPostprocessor
    {
        public const string Folder = "Assets/_Project/Art/UI/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.spritePixelsPerUnit = 200;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;     // required for sliced images
            ti.SetTextureSettings(settings);
            ti.spriteBorder = Border(Path.GetFileNameWithoutExtension(assetPath));
        }

        /// <summary>Borders in pixels (L, B, R, T) for the 2x PNGs.</summary>
        static Vector4 Border(string name)
        {
            if (name == "btn_start") return new Vector4(96, 96, 96, 96);
            if (name == "bottom_bar") return new Vector4(0, 0, 0, 8);
            if (name == "status_tag" || name == "btn_rail") return Vector4.zero;
            if (name.StartsWith("btn_round") || name.StartsWith("badge") || name.Contains("silhouette") || name.Contains("hero"))
                return Vector4.zero;
            if (name.StartsWith("btn_")) return new Vector4(64, 64, 64, 64);
            if (name.StartsWith("tile")) return new Vector4(64, 64, 64, 64);
            if (name == "card_quickstart_metal") return new Vector4(104, 40, 40, 104);
            if (name.StartsWith("card_")) return new Vector4(96, 96, 96, 96);
            if (name.StartsWith("panel")) return new Vector4(80, 80, 80, 80);
            if (name == "pill" || name == "chip") return new Vector4(64, 60, 64, 60);
            if (name == "health_bar") return new Vector4(36, 0, 36, 0);
            return Vector4.zero;
        }
    }
}
