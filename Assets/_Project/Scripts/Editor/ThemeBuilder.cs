using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using DroneTrainer.UI;

namespace DroneTrainer.EditorTools
{
    /// <summary>Step 3: sprites, icons, UITheme asset and shared materials (ghost, highlight, AR plane, desk).</summary>
    public static class ThemeBuilder
    {
        public const string ThemePath = Paths.Resources + "/UITheme.asset";

        public static void Run()
        {
            Paths.Ensure(Paths.Sprites);
            Paths.Ensure(Paths.Icons);
            Paths.Ensure(Paths.Materials);

            var theme = Paths.LoadOrCreate<UITheme>(ThemePath);
            theme.rounded = Sprite("rounded", RoundedRect(64, 64, 16), 20);
            theme.roundedLarge = Sprite("rounded_large", RoundedRect(96, 96, 30), 34);
            theme.pill = Sprite("pill", RoundedRect(68, 68, 34), 33);
            theme.circle = Sprite("circle", Circle(128, 64, 0), 0);
            theme.ring = Sprite("ring", Circle(256, 128, 100), 0);
            theme.softGlow = Sprite("glow", Glow(128), 0);
            theme.gradient = Sprite("gradient", Gradient(8, 256), 0);
            Write("dots", Dots(64), Paths.Sprites, sprite: false, repeat: true);

            theme.icons.Clear();
            foreach (var kv in IconBaker.BakeAll())
            {
                var path = Write(kv.Key, kv.Value, Paths.Icons, sprite: true);
                theme.icons.Add(new NamedSprite { name = kv.Key, sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path) });
            }

            var fontGuid = AssetDatabase.FindAssets("t:TMP_FontAsset LiberationSans")
                               .OrderBy(g => AssetDatabase.GUIDToAssetPath(g).Contains("Fallback") ? 1 : 0).FirstOrDefault()
                           ?? AssetDatabase.FindAssets("t:TMP_FontAsset").FirstOrDefault();
            var custom = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { Paths.Root + "/Fonts" }).FirstOrDefault();
            var guid = custom ?? fontGuid;
            theme.font = guid != null ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid)) : null;
            Sprite Art(string p) => AssetDatabase.LoadAssetAtPath<Sprite>(UIArtPostprocessor.Folder + p + ".png");
            theme.navTile = Art("Buttons/tile");
            theme.navTileActive = Art("Buttons/tile_active");
            theme.quickCard = Art("Panels/card_quickstart_metal");
            theme.droneSilhouette = Art("Icons/drone_silhouette");
            theme.healthBar = Art("Badges/health_bar");
            theme.badgeAlert = Art("Badges/badge_alert");
            theme.heroDrone = Art("Drones/skyfold_f4_render");
            theme.railButton = Art("Buttons/btn_rail");
            theme.arrowButton = Art("Buttons/btn_arrow");
            theme.startButton = Art("Buttons/btn_start");
            theme.bottomBar = Art("Panels/bottom_bar");
            theme.statusTag = Art("Panels/status_tag");
            theme.btnPrimary = Art("Buttons/btn_primary");
            theme.btnGhost = Art("Buttons/btn_ghost");
            theme.btnDanger = Art("Buttons/btn_danger");
            theme.btnOk = Art("Buttons/btn_ok");
            theme.btnDisabled = Art("Buttons/btn_disabled");
            theme.panelArt = Art("Panels/panel");
            EditorUtility.SetDirty(theme);

            BuildMaterials();
            AssetDatabase.SaveAssets();
            Debug.Log($"[DroneTrainer] Theme ready: {theme.icons.Count} icons, font = {(theme.font ? theme.font.name : "TMP default")}.");
        }

        // ------------------------------------------------------------------ materials
        public static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{Paths.Materials}/{name}.mat");

        static void BuildMaterials()
        {
            var ghost = Unlit("M_Ghost", new Color(0.27f, 0.88f, 0.82f, 0.32f), transparent: true);
            var hl = Shader.Find("DroneTrainer/RimHighlight");
            if (hl) SaveMat("M_Highlight", new Material(hl));
            var dots = AssetDatabase.LoadAssetAtPath<Texture2D>(Paths.Sprites + "/dots.png");
            var plane = Unlit("M_ARPlane", new Color(0.27f, 0.88f, 0.82f, 0.85f), transparent: true, tex: dots);
            plane.mainTextureScale = new Vector2(20, 20);
            plane.SetTextureScale("_BaseMap", new Vector2(20, 20));
            Lit("M_Desk", new Color(0.36f, 0.25f, 0.17f), 0.35f);
            Lit("M_DeskLeg", new Color(0.12f, 0.12f, 0.13f), 0.5f, 0.6f);
            Lit("M_Floor", new Color(0.09f, 0.1f, 0.11f), 0.2f);
            Lit("M_Wall", new Color(0.30f, 0.31f, 0.33f), 0.1f);
            Lit("M_StageDisc", new Color(0.08f, 0.09f, 0.1f), 0.35f, 0.2f);
            Lit("M_StageRing", new Color(0.1f, 0.4f, 0.37f), 0.6f, 0f, emission: new Color(0.2f, 0.9f, 0.82f) * 1.6f);
        }

        public static Material Lit(string name, Color c, float smooth = 0.5f, float metal = 0f, bool transparent = false, Color? emission = null)
        {
            var m = Mat(name); if (!m) m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            if (transparent) MakeTransparent(m);
            if (emission.HasValue)
            {
                m.SetColor("_EmissionColor", emission.Value);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            return SaveMat(name, m);
        }

        public static Material Unlit(string name, Color c, bool transparent, Texture2D tex = null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            var m = Mat(name); if (!m) m = new Material(sh);
            m.shader = sh;
            m.SetColor("_BaseColor", c);
            if (tex) m.SetTexture("_BaseMap", tex);
            if (transparent) MakeTransparent(m);
            return SaveMat(name, m);
        }

        public static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        static Material SaveMat(string name, Material m)
        {
            var path = $"{Paths.Materials}/{name}.mat";
            if (!AssetDatabase.Contains(m)) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ texture generators
        static Sprite Sprite(string name, Texture2D tex, int border)
        {
            var path = Write(name, tex, Paths.Sprites, sprite: true, border: border);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static string Write(string name, Texture2D tex, string folder, bool sprite, int border = 0, bool repeat = false)
        {
            var path = $"{folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            ti.spriteImportMode = sprite ? SpriteImportMode.Single : SpriteImportMode.None;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = !sprite;
            ti.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            if (sprite)
            {
                ti.spriteBorder = new Vector4(border, border, border, border);
                ti.spritePixelsPerUnit = 100;
            }
            ti.SaveAndReimport();
            return path;
        }

        static Texture2D New(int w, int h) => new Texture2D(w, h, TextureFormat.RGBA32, false);

        static Texture2D RoundedRect(int w, int h, float r)
        {
            var t = New(w, h);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float qx = Mathf.Max(Mathf.Abs(px - w / 2f) - (w / 2f - r), 0);
                float qy = Mathf.Max(Mathf.Abs(py - h / 2f) - (h / 2f - r), 0);
                float d = Mathf.Sqrt(qx * qx + qy * qy) - r;
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d)));
            }
            t.Apply();
            return t;
        }

        static Texture2D Circle(int size, float outer, float inner)
        {
            var t = New(size, size);
            float c = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                float a = Mathf.Clamp01(outer - 0.5f - d + 0.5f);
                if (inner > 0) a = Mathf.Min(a, Mathf.Clamp01(d - inner + 0.5f));
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.Apply();
            return t;
        }

        static Texture2D Glow(int size)
        {
            var t = New(size, size);
            float c = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f)));
            }
            t.Apply();
            return t;
        }

        static Texture2D Gradient(int w, int h)
        {
            var t = New(w, h);
            var top = UITheme.Hex("0B0E12");
            var bottom = UITheme.Hex("1A1E24");
            for (int y = 0; y < h; y++)
            {
                var c = Color.Lerp(bottom, top, y / (float)(h - 1));
                for (int x = 0; x < w; x++) t.SetPixel(x, y, c);
            }
            t.Apply();
            return t;
        }

        static Texture2D Dots(int size)
        {
            var t = New(size, size);
            float c = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(7f - d)));
            }
            t.Apply();
            return t;
        }
    }
}
