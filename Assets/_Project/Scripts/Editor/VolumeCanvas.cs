using System;
using FishAndChill.Player;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Moteur de dessin « en volume », le style du médaillon au poisson de l'aquarium et des poissons du jeu : chaque
    /// pièce posée est un volume rond dont le relief est calculé depuis sa forme (distance au bord), éclairé par
    /// paliers (ombres froides, lumières chaudes, reflet), avec sa matière (fonction de texture), une ombre portée sur
    /// ce qui est dessous et, à la fin, un contour sélectif teinté. Coordonnées en pixels, y compté depuis le HAUT ;
    /// <see cref="Finish"/> rend un tableau [x, y] avec y depuis le bas, prêt pour PixelArt.Save.
    /// Pensé pour 32 pixels par case (deux fois plus fin que les anciens dessins, même taille à l'écran).
    /// </summary>
    public sealed class VolumeCanvas
    {
        /// <summary>Pixels par case des dessins en volume.</summary>
        public const int PixelsPerUnit = 32;

        private static readonly Vector3 LightDirection = new Vector3(-0.5f, -0.62f, 0.6f).normalized;   // haut à gauche, un peu de face

        private readonly Color[,] _pixels;

        public int Width { get; }
        public int Height { get; }

        public VolumeCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new Color[width, height];
        }

        /// <summary>
        /// Pose une pièce. <paramref name="bulge"/> = épaisseur du bord arrondi en pixels (petit : surface plate aux bords
        /// arrondis ; grand : volume bien rond). <paramref name="texture"/> change la couleur de base pixel par pixel
        /// (transparent = trou). <paramref name="castShadow"/> assombrit ce qui est juste sous la pièce.
        /// </summary>
        public void Piece(Func<float, float, bool> inside, Color baseColor, float bulge,
            Func<int, int, Color, Color> texture = null, float castShadow = 0.8f, float minLight = 0.42f)
        {
            var mask = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    mask[x, y] = inside(x + 0.5f, y + 0.5f);
                }
            }

            int reach = Mathf.Max(1, Mathf.CeilToInt(bulge));
            var height = new float[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (!mask[x, y])
                    {
                        continue;
                    }

                    float best = reach;
                    for (int dx = -reach; dx <= reach; dx++)
                    {
                        for (int dy = -reach; dy <= reach; dy++)
                        {
                            int a = x + dx, b = y + dy;
                            if (a < 0 || b < 0 || a >= Width || b >= Height || !mask[a, b])
                            {
                                best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dy * dy) - 0.5f);
                            }
                        }
                    }

                    float t = Mathf.Clamp01(best / Mathf.Max(0.5f, bulge));
                    height[x, y] = Mathf.Sqrt(1f - (1f - t) * (1f - t));   // profil en dôme
                }
            }

            // Ombre portée sur ce qui est déjà dessiné, juste sous la pièce.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (mask[x, y] || _pixels[x, y].a < 0.01f)
                    {
                        continue;
                    }

                    bool below = (y > 0 && mask[x, y - 1]) || (x > 0 && y > 0 && mask[x - 1, y - 1]) || (y > 1 && mask[x, y - 2] && castShadow < 0.9f);
                    if (below)
                    {
                        _pixels[x, y] = PixelShading.Shade(_pixels[x, y], castShadow);
                    }
                }
            }

            float H(int a, int b) => a < 0 || b < 0 || a >= Width || b >= Height || !mask[a, b] ? 0f : height[a, b];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (!mask[x, y])
                    {
                        continue;
                    }

                    float gx = (H(x + 1, y) - H(x - 1, y)) * 0.5f;
                    float gy = (H(x, y + 1) - H(x, y - 1)) * 0.5f;
                    var normal = new Vector3(-gx * 1.6f, -gy * 1.6f, 0.55f).normalized;
                    float lit = Vector3.Dot(normal, LightDirection);
                    float light = Mathf.Clamp(Mathf.Round((0.42f + 0.95f * lit) * 6f) / 6f, minLight, 1.22f);   // paliers nets
                    Color c = texture != null ? texture(x, y, baseColor) : baseColor;
                    if (c.a < 0.01f)
                    {
                        continue;
                    }

                    c = PixelShading.Shade(c, light);
                    if (lit > 0.95f && height[x, y] < 0.9f)
                    {
                        c = PixelShading.Shade(c, 1.18f);   // reflet
                    }

                    c.a = 1f;
                    _pixels[x, y] = c;
                }
            }
        }

        /// <summary>Pose un pixel tel quel (yeux, petits détails).</summary>
        public void Dot(int x, int y, Color color)
        {
            if (x >= 0 && y >= 0 && x < Width && y < Height)
            {
                _pixels[x, y] = color;
            }
        }

        /// <summary>Retourne le dessin (y depuis le bas) avec son contour sélectif teinté.</summary>
        public Color[,] Finish(Color outline, float ink = 0.55f)
        {
            var p = new Color[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    p[x, Height - 1 - y] = _pixels[x, y];
                }
            }

            PixelShading.SelectiveOutline(p, outline, ink);
            return p;
        }

        // ---------- Formes (y depuis le haut) ----------

        public static Func<float, float, bool> Ellipse(float cx, float cy, float rx, float ry)
            => (x, y) => { float u = (x - cx) / rx, v = (y - cy) / ry; return u * u + v * v <= 1f; };

        /// <summary>« Gélule » : tous les points à moins de <paramref name="r"/> du segment.</summary>
        public static Func<float, float, bool> Capsule(float x0, float y0, float x1, float y1, float r)
        {
            var a = new Vector2(x0, y0);
            Vector2 ab = new Vector2(x1, y1) - a;
            float len2 = Mathf.Max(0.0001f, ab.sqrMagnitude);
            return (x, y) =>
            {
                var p = new Vector2(x, y);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                return (p - (a + ab * t)).sqrMagnitude <= r * r;
            };
        }

        public static Func<float, float, bool> Box(float x0, float y0, float x1, float y1) => (x, y) => x > x0 && x < x1 && y > y0 && y < y1;

        public static Func<float, float, bool> Union(params Func<float, float, bool>[] shapes)
            => (x, y) =>
            {
                foreach (Func<float, float, bool> s in shapes)
                {
                    if (s(x, y)) return true;
                }

                return false;
            };

        public static Func<float, float, bool> Rows(Func<float, float, bool> shape, float minY, float maxY) => (x, y) => y >= minY && y <= maxY && shape(x, y);

        public static Color Darker(Color c, float light) => PixelShading.Shade(c, light);
    }
}
