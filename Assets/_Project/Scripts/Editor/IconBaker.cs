using System;
using System.Collections.Generic;
using UnityEngine;

namespace DroneTrainer.EditorTools
{
    /// <summary>
    /// Rasterises the design-kit line icons (Design/assets/icons.js, 24-unit grid, 2-unit stroke)
    /// into anti-aliased white PNGs using distance fields. Tinted in-engine.
    /// </summary>
    public static class IconBaker
    {
        public const int Size = 128;
        const float Scale = Size / 24f;

        abstract class Shape { public abstract float Dist(Vector2 p); public virtual bool Filled => false; public virtual bool Inside(Vector2 p) => false; }

        class Seg : Shape
        {
            readonly Vector2 a, b;
            public Seg(Vector2 a, Vector2 b) { this.a = a; this.b = b; }
            public override float Dist(Vector2 p)
            {
                var ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                return Vector2.Distance(p, a + ab * t);
            }
        }

        class Ring : Shape
        {
            readonly Vector2 c; readonly float r, a0, a1; readonly bool full;
            public Ring(Vector2 c, float r, float a0 = 0, float a1 = 360) { this.c = c; this.r = r; this.a0 = a0; this.a1 = a1; full = Mathf.Abs(a1 - a0) >= 359.9f; }
            public override float Dist(Vector2 p)
            {
                var d = p - c;
                if (full) return Mathf.Abs(d.magnitude - r);
                // angle measured counter-clockwise with y pointing up on screen (SVG y is down)
                float ang = Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg;
                if (InArc(ang)) return Mathf.Abs(d.magnitude - r);
                return Mathf.Min(Vector2.Distance(p, Pt(a0)), Vector2.Distance(p, Pt(a1)));
            }
            bool InArc(float ang)
            {
                float span = a1 - a0;
                float rel = Mathf.Repeat(ang - a0, 360f);
                return rel <= span;
            }
            Vector2 Pt(float deg) => c + new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), -Mathf.Sin(deg * Mathf.Deg2Rad)) * r;
        }

        class Fill : Shape
        {
            readonly Vector2[] pts;
            public Fill(Vector2[] pts) { this.pts = pts; }
            public override bool Filled => true;
            public override float Dist(Vector2 p)
            {
                float d = float.MaxValue;
                for (int i = 0; i < pts.Length; i++) d = Mathf.Min(d, new Seg(pts[i], pts[(i + 1) % pts.Length]).Dist(p));
                return d;
            }
            public override bool Inside(Vector2 p)
            {
                bool inside = false;
                for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                    if ((pts[i].y > p.y) != (pts[j].y > p.y) &&
                        p.x < (pts[j].x - pts[i].x) * (p.y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x) inside = !inside;
                return inside;
            }
        }

        class Disc : Shape
        {
            readonly Vector2 c; readonly float r;
            public Disc(Vector2 c, float r) { this.c = c; this.r = r; }
            public override bool Filled => true;
            public override float Dist(Vector2 p) => Mathf.Abs(Vector2.Distance(p, c) - r);
            public override bool Inside(Vector2 p) => Vector2.Distance(p, c) < r;
        }

        /// <summary>Tiny DSL for icon definitions in 24-unit SVG coordinates.</summary>
        class Icon
        {
            public readonly List<Shape> shapes = new List<Shape>();
            public float stroke = 2f;
            static Vector2 V(float x, float y) => new Vector2(x, y);
            public Icon L(float x1, float y1, float x2, float y2) { shapes.Add(new Seg(V(x1, y1), V(x2, y2))); return this; }
            public Icon P(params float[] xy) { for (int i = 0; i + 3 < xy.Length; i += 2) L(xy[i], xy[i + 1], xy[i + 2], xy[i + 3]); return this; }
            public Icon Z(params float[] xy) { P(xy); return L(xy[xy.Length - 2], xy[xy.Length - 1], xy[0], xy[1]); }
            public Icon C(float x, float y, float r) { shapes.Add(new Ring(V(x, y), r)); return this; }
            public Icon A(float x, float y, float r, float a0, float a1) { shapes.Add(new Ring(V(x, y), r, a0, a1)); return this; }
            public Icon D(float x, float y, float r) { shapes.Add(new Disc(V(x, y), r)); return this; }
            public Icon F(params float[] xy)
            {
                var pts = new Vector2[xy.Length / 2];
                for (int i = 0; i < pts.Length; i++) pts[i] = V(xy[2 * i], xy[2 * i + 1]);
                shapes.Add(new Fill(pts));
                return this;
            }
            public Icon R(float x, float y, float w, float h, float r)
            {
                L(x + r, y, x + w - r, y); L(x + w, y + r, x + w, y + h - r);
                L(x + w - r, y + h, x + r, y + h); L(x, y + h - r, x, y + r);
                A(x + w - r, y + r, r, 0, 90); A(x + r, y + r, r, 90, 180);
                A(x + r, y + h - r, r, 180, 270); A(x + w - r, y + h - r, r, 270, 360);
                return this;
            }
        }

        static Dictionary<string, Icon> Defs()
        {
            Icon N() => new Icon();
            return new Dictionary<string, Icon>
            {
                ["check"] = N().P(5, 12.5f, 9.5f, 17, 19, 7.5f),
                ["x"] = N().L(6, 6, 18, 18).L(18, 6, 6, 18),
                ["pause"] = N().L(9, 5, 9, 19).L(15, 5, 15, 19),
                ["play"] = N().F(7, 4.5f, 19.5f, 12, 7, 19.5f),
                ["help"] = N().C(12, 12, 9).P(9.6f, 9.6f, 10.3f, 8, 12, 7.3f, 13.8f, 8, 14.4f, 9.6f, 13.6f, 11, 12, 12, 12, 14).D(12, 17.2f, 1.3f),
                ["info"] = N().C(12, 12, 9).L(12, 11, 12, 16.5f).D(12, 7.8f, 1.3f),
                ["warn"] = N().Z(12, 3.2f, 21.5f, 20.2f, 2.5f, 20.2f).L(12, 10, 12, 14.5f).D(12, 17.4f, 1.2f),
                ["timer"] = N().C(12, 13.5f, 7.5f).P(12, 10, 12, 13.5f, 14.2f, 15.7f).L(9.5f, 2.5f, 14.5f, 2.5f),
                ["battery"] = N().R(2, 7, 17, 10, 2).L(22, 10.5f, 22, 13.5f).L(5.5f, 10.5f, 5.5f, 13.5f).L(9, 10.5f, 9, 13.5f),
                ["prop"] = N().D(12, 12, 2.6f).F(11, 10.5f, 5.5f, 3.5f, 3.2f, 5.2f, 9.8f, 12.6f).F(13, 13.5f, 18.5f, 20.5f, 20.8f, 18.8f, 14.2f, 11.4f),
                ["gimbal"] = N().R(6, 4, 12, 9, 2).C(12, 15.5f, 4.5f).D(12, 15.5f, 1.6f),
                ["rc"] = N().R(2.5f, 8, 19, 11, 4).C(8, 13.5f, 2).C(16, 13.5f, 2).L(5, 8, 4, 4).L(19, 8, 20, 4),
                ["lock"] = N().R(5, 11, 14, 10, 2).A(12, 8, 4, 0, 180).L(8, 8, 8, 11).L(16, 8, 16, 11),
                ["unlock"] = N().R(5, 11, 14, 10, 2).A(12, 8, 4, 35, 180).L(8, 8, 8, 11),
                ["phone"] = N().R(7, 2, 10, 20, 2.5f).L(11, 18.5f, 13, 18.5f),
                ["hand"] = N().R(9.5f, 2.5f, 5, 12, 2.5f).R(6, 11, 12, 10.5f, 4),
                ["rotate"] = N().A(12, 12, 8, 45, 315).P(20.5f, 3.5f, 20.5f, 8.5f, 15.5f, 8.5f),
                ["reset"] = N().A(12, 12, 8, -135, 135).P(3.5f, 3.5f, 3.5f, 8.5f, 8.5f, 8.5f),
                ["scale"] = N().L(4, 20, 20, 4).P(14, 4, 20, 4, 20, 10).P(10, 20, 4, 20, 4, 14),
                ["eye"] = N().A(12, 21, 12, 48, 132).A(12, 3, 12, 228, 312).C(12, 12, 3),
                ["left"] = N().P(15, 4, 7, 12, 15, 20),
                ["right"] = N().P(9, 4, 17, 12, 9, 20),
                ["gear"] = N().C(12, 12, 3.2f).C(12, 12, 7).L(12, 2, 12, 5).L(12, 19, 12, 22).L(2, 12, 5, 12).L(19, 12, 22, 12)
                              .L(4.9f, 4.9f, 7, 7).L(17, 17, 19.1f, 19.1f).L(4.9f, 19.1f, 7, 17).L(17, 7, 19.1f, 4.9f),
                ["home"] = N().Z(3, 11, 12, 4, 21, 11, 21, 20, 15, 20, 15, 14, 9, 14, 9, 20, 3, 20),
                ["hangar"] = N().P(2, 20, 2, 10, 12, 4, 22, 10, 22, 20).P(6, 20, 6, 13, 18, 13, 18, 20).L(6, 16.5f, 18, 16.5f),
                ["train"] = N().Z(12, 3, 22, 8, 12, 13, 2, 8).P(6, 10, 6, 15, 9, 17.2f, 12, 18, 15, 17.2f, 18, 15, 18, 10).L(22, 8, 22, 14),
                ["records"] = N().L(5, 20, 5, 11).L(11, 20, 11, 5).L(17, 20, 17, 14).L(2, 20, 22, 20),
                ["drone"] = N().C(5, 5, 3).C(19, 5, 3).C(5, 19, 3).C(19, 19, 3).L(7.2f, 7.2f, 9.8f, 9.8f).L(16.8f, 7.2f, 14.2f, 9.8f)
                               .L(7.2f, 16.8f, 9.8f, 14.2f).L(16.8f, 16.8f, 14.2f, 14.2f).R(9.5f, 9.5f, 5, 5, 1),
                ["trophy"] = N().P(8, 4, 16, 4, 16, 9, 14.8f, 11.8f, 12, 13, 9.2f, 11.8f, 8, 9, 8, 4).P(8, 6, 4.5f, 6, 5.2f, 8.6f, 8, 10)
                                .P(16, 6, 19.5f, 6, 18.8f, 8.6f, 16, 10).L(12, 13, 12, 17).L(8.5f, 20.5f, 15.5f, 20.5f).L(10, 17, 14, 17),
                ["flag"] = N().P(5, 21, 5, 4, 17, 4, 14.5f, 8, 17, 12, 5, 12),
                ["target"] = N().C(12, 12, 7.5f).C(12, 12, 2.5f).L(12, 1.5f, 12, 5.5f).L(12, 18.5f, 12, 22.5f).L(1.5f, 12, 5.5f, 12).L(18.5f, 12, 22.5f, 12),
                ["signal"] = N().L(4, 20, 4, 17).L(9, 20, 9, 13).L(14, 20, 14, 9).L(19, 20, 19, 5),
                ["sound"] = N().Z(4, 9.5f, 7.5f, 9.5f, 12, 5.5f, 12, 18.5f, 7.5f, 14.5f, 4, 14.5f).A(12, 12, 4, -50, 50).A(12, 12, 7.5f, -50, 50),
                ["vibrate"] = N().R(8, 3, 8, 18, 2).L(4, 8, 4, 16).L(20, 8, 20, 16),
                ["exit"] = N().P(14, 4, 19, 4, 19, 20, 14, 20).P(10, 8, 6, 12, 10, 16).L(6, 12, 16, 12),
                ["star"] = N().Z(12, 3, 14.8f, 8.8f, 21, 9.7f, 16.5f, 14.1f, 17.5f, 20.3f, 12, 17.4f, 6.5f, 20.3f, 7.5f, 14.1f, 3, 9.7f, 9.2f, 8.8f),
                ["clip"] = N().R(5, 4, 14, 17, 2).P(9, 4, 9, 2.5f, 15, 2.5f, 15, 4).P(8.5f, 12.5f, 10.8f, 14.8f, 15.5f, 10),
                ["wrench"] = N().L(5, 19, 11.5f, 12.5f).A(15.5f, 8.5f, 5, -140, 130),
            };
        }

        public static Dictionary<string, Texture2D> BakeAll()
        {
            var result = new Dictionary<string, Texture2D>();
            foreach (var kv in Defs()) result[kv.Key] = Bake(kv.Value);
            return result;
        }

        static Texture2D Bake(Icon icon)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var px = new Color32[Size * Size];
            float half = icon.stroke * 0.5f * Scale;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                // pixel centre in SVG units (y down)
                var p = new Vector2((x + 0.5f) / Scale, (Size - y - 0.5f) / Scale);
                float a = 0;
                foreach (var s in icon.shapes)
                {
                    float d = s.Dist(p) * Scale;
                    float cov = s.Filled
                        ? (s.Inside(p) ? 1f : Mathf.Clamp01(0.5f - d))
                        : Mathf.Clamp01(half - d + 0.5f);
                    if (cov > a) a = cov;
                    if (a >= 1) break;
                }
                px[y * Size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
