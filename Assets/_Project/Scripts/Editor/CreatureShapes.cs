using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Outils de dessin des créatures en volume (<see cref="VolumeCanvas"/>) : corps qui suit une épine dorsale avec un
    /// repère local (le long, en travers) pour peindre écailles et motifs qui suivent la courbe, tubes qui s'affinent,
    /// courbes, polygones, bruit stable, mélanges et halos après le contour. Coordonnées y depuis le haut (dessin),
    /// sauf <see cref="Mix"/> et <see cref="Halo"/> qui travaillent sur le tableau final (y depuis le bas).
    /// </summary>
    public static class CreatureShapes
    {
        /// <summary>Une épine dorsale : des points et un rayon à chaque point, de la queue (t = 0) à la tête (t = 1).</summary>
        public sealed class Spine
        {
            public readonly List<Vector2> Points = new List<Vector2>();
            public readonly List<float> Radius = new List<float>();

            /// <summary>Repère du corps en un point : t le long, s en travers (-1 dos, +1 ventre), r le rayon local.</summary>
            public bool Frame(float x, float y, out float t, out float s, out float r)
            {
                var p = new Vector2(x, y);
                float best = float.MaxValue;
                t = s = r = 0f;
                for (int i = 0; i < Points.Count - 1; i++)
                {
                    Vector2 a = Points[i], b = Points[i + 1], ab = b - a;
                    float k = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                    Vector2 q = a + ab * k;
                    float d = (p - q).sqrMagnitude;
                    if (d < best)
                    {
                        best = d;
                        float rr = Mathf.Lerp(Radius[i], Radius[i + 1], k);
                        float cross = ab.x * (p - q).y - ab.y * (p - q).x;   // > 0 : vers le bas (le ventre), y vers le bas
                        t = (i + k) / (Points.Count - 1);
                        r = rr;
                        s = Mathf.Sign(cross) * Mathf.Sqrt(d) / Mathf.Max(0.5f, rr);
                    }
                }

                return Mathf.Abs(s) <= 1f;
            }

            /// <summary>Point de l'épine en t, et la normale (vers le ventre).</summary>
            public Vector2 At(float t, out Vector2 normal)
            {
                float f = Mathf.Clamp01(t) * (Points.Count - 1);
                int i = Mathf.Min(Points.Count - 2, Mathf.FloorToInt(f));
                Vector2 a = Points[i], b = Points[i + 1];
                Vector2 tangent = (b - a).normalized;
                normal = new Vector2(-tangent.y, tangent.x);
                return Vector2.Lerp(a, b, f - i);
            }

            public float RadiusAt(float t)
            {
                float f = Mathf.Clamp01(t) * (Radius.Count - 1);
                int i = Mathf.Min(Radius.Count - 2, Mathf.FloorToInt(f));
                return Mathf.Lerp(Radius[i], Radius[i + 1], f - i);
            }

            /// <summary>Le corps entier (pour <see cref="VolumeCanvas.Piece"/>).</summary>
            public bool Inside(float x, float y) => Frame(x, y, out _, out _, out _);
        }

        /// <summary>Tube qui s'affine : de rayon r0 à r1 le long des points.</summary>
        public static Func<float, float, bool> Tube(IList<Vector2> pts, float r0, float r1)
        {
            return (x, y) =>
            {
                var p = new Vector2(x, y);
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    Vector2 a = pts[i], b = pts[i + 1], ab = b - a;
                    float k = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                    float r = Mathf.Lerp(r0, r1, (i + k) / (pts.Count - 1));
                    if ((p - (a + ab * k)).sqrMagnitude <= r * r) return true;
                }

                return false;
            };
        }

        /// <summary>Courbe de Bézier quadratique échantillonnée.</summary>
        public static List<Vector2> Curve(Vector2 a, Vector2 control, Vector2 b, int steps = 12)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                pts.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * control + t * t * b);
            }

            return pts;
        }

        public static Func<float, float, bool> Poly(params Vector2[] v)
        {
            return (x, y) =>
            {
                bool inside = false;
                for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
                {
                    if ((v[i].y > y) != (v[j].y > y) && x < (v[j].x - v[i].x) * (y - v[i].y) / (v[j].y - v[i].y) + v[i].x) inside = !inside;
                }

                return inside;
            };
        }

        /// <summary>Bruit stable entre 0 et 1 pour une case.</summary>
        public static float Hash(int x, int y) => Mathf.Repeat(Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f, 1f);

        /// <summary>Bruit doux (interpolé) entre 0 et 1, pour les marbrures.</summary>
        public static float Noise(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float top = Mathf.Lerp(Hash(ix, iy), Hash(ix + 1, iy), fx);
            float bottom = Mathf.Lerp(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), fx);
            return Mathf.Lerp(top, bottom, fy);
        }

        /// <summary>
        /// Contour d'une nageoire posée le long du corps entre t0 et t1, côté dos (<paramref name="side"/> = -1) ou ventre
        /// (+1) : <paramref name="height"/>(k) donne sa hauteur au-delà du corps, k allant de 0 (côté t0) à 1 (côté t1).
        /// </summary>
        public static Vector2[] FinAlong(Spine spine, float t0, float t1, float side, Func<float, float> height, int steps = 28, float sweep = 0f)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i <= steps; i++)
            {
                float t = Mathf.Lerp(t0, t1, i / (float)steps);
                Vector2 at = spine.At(t, out Vector2 n);
                pts.Add(at + n * side * spine.RadiusAt(t) * 0.6f);
            }

            for (int i = steps; i >= 0; i--)
            {
                float k = i / (float)steps;
                float t = Mathf.Lerp(t0, t1, k);
                Vector2 at = spine.At(t, out Vector2 n);
                Vector2 along = new Vector2(n.y, -n.x);   // vers la tête
                float hk = height(k);
                pts.Add(at + n * side * (spine.RadiusAt(t) + hk) - along * hk * sweep);
            }

            return pts.ToArray();
        }

        /// <summary>Anneau (maillon de chaîne, bague) : entre deux ellipses.</summary>
        public static Func<float, float, bool> Ring(float cx, float cy, float rx, float ry, float thickness)
            => (x, y) =>
            {
                float u = (x - cx) / rx, v = (y - cy) / ry;
                float a = (x - cx) / Mathf.Max(0.5f, rx - thickness), b = (y - cy) / Mathf.Max(0.5f, ry - thickness);
                return u * u + v * v <= 1f && a * a + b * b > 1f;
            };

        /// <summary>Mélange une couleur dans le tableau final (y depuis le bas).</summary>
        public static void Mix(Color[,] p, int x, int y, Color c, float amount)
        {
            if (x < 0 || y < 0 || x >= p.GetLength(0) || y >= p.GetLength(1)) return;
            Color under = p[x, y];
            p[x, y] = under.a < 0.01f ? new Color(c.r, c.g, c.b, amount) : Color.Lerp(under, c, amount);
        }

        /// <summary>Lueur douce autour d'un point (un œil), sur ce qui est déjà dessiné.</summary>
        public static void Halo(Color[,] p, int cx, int cy, int r, Color c, float inner = 3.5f)
        {
            for (int x = cx - r; x <= cx + r; x++)
                for (int y = cy - r; y <= cy + r; y++)
                {
                    if (x < 0 || y < 0 || x >= p.GetLength(0) || y >= p.GetLength(1) || p[x, y].a < 0.5f) continue;
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d < inner || d > r) continue;
                    p[x, y] = Color.Lerp(p[x, y], c, (1f - d / r) * 0.35f);
                }
        }
    }
}
