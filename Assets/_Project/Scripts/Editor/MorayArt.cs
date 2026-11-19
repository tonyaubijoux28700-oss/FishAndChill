using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// « Tuna », la murène géante de l'île Mahana (comme l'anguille des légendes polynésiennes), dessin détaillé dans le
    /// style en volume (<see cref="VolumeCanvas"/>), de profil, tête à droite, 256×144 : long corps sans écailles à la peau
    /// tachetée en réseau (comme la vraie murène javanaise), nageoire charnue tout le long du dos et sous la queue, tête
    /// massive et musclée, gueule béante aux crocs recourbés avec une deuxième mâchoire qui sort du fond de la gorge,
    /// narines en tubes, petit œil qui luit, ouïe, anémone et corail qui ont poussé sur la tête, balanes, cicatrices,
    /// vieil hameçon dans la lèvre. y depuis le haut pendant le dessin.
    /// </summary>
    public static class MorayArt
    {
        public const int Width = 256;
        public const int Height = 144;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Skin = C(0.21f, 0.22f, 0.14f);
        private static readonly Color SkinDark = C(0.09f, 0.10f, 0.07f);
        private static readonly Color Spot = C(0.04f, 0.05f, 0.04f);
        private static readonly Color Ochre = C(0.40f, 0.36f, 0.20f);
        private static readonly Color BellyColor = C(0.40f, 0.39f, 0.28f);
        private static readonly Color Maw = C(0.16f, 0.03f, 0.06f);
        private static readonly Color Throat = C(0.70f, 0.42f, 0.44f);
        private static readonly Color Tooth = C(0.93f, 0.91f, 0.82f);
        private static readonly Color Glow = C(0.40f, 1f, 0.90f);

        private static Spine BuildSpine()
        {
            var spine = new Spine();
            const int n = 80;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                spine.Points.Add(new Vector2(10f + t * 164f, 90f + Mathf.Sin(t * Mathf.PI * 1.7f + 0.7f) * 20f - t * 16f));
                spine.Radius.Add(1.4f + 13f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.45f)) + 2.5f * t);
            }

            return spine;
        }

        /// <summary>Peau tachetée en réseau : taches sombres irrégulières sur fond ocre-olive, plus sombre sur le dos.</summary>
        private static Color Leopard(float u, float v, float dorsal, Color baseColor)
        {
            int cu = Mathf.FloorToInt(u), cv = Mathf.FloorToInt(v);
            float best = 9f;
            float size = 1f;
            for (int a = -1; a <= 1; a++)
                for (int b = -1; b <= 1; b++)
                {
                    float jx = cu + a + 0.2f + Hash(cu + a, cv + b) * 0.6f;
                    float jy = cv + b + 0.2f + Hash(cv + b + 31, cu + a) * 0.6f;
                    // Taches de tailles inégales, un peu allongées, parfois soudées entre elles.
                    float d = (u - jx) * (u - jx) * 0.8f + (v - jy) * (v - jy) * 1.4f;
                    if (d < best)
                    {
                        best = d;
                        size = 0.45f + Hash(cu + a + 7, cv + b + 13) * 1.1f;
                    }
                }

            Color ground = Color.Lerp(Ochre, baseColor, 0.45f + dorsal * 0.45f);
            if (best < 0.09f * size) return Color.Lerp(Spot, SkinDark, 0.2f);
            if (best < 0.14f * size) return SkinDark;
            return Hash(Mathf.FloorToInt(u * 5f), Mathf.FloorToInt(v * 5f)) < 0.08f ? Darker(ground, 0.8f) : ground;
        }

        public static Color[,] Draw()
        {
            var cv = new VolumeCanvas(Width, Height);
            Spine spine = BuildSpine();

            // ---------- Nageoires charnues : tout le long du dos, et sous la queue ----------
            var dorsal = new List<Vector2>();
            for (float t = 0.0f; t <= 0.9f; t += 0.02f)
            {
                Vector2 at = spine.At(t, out Vector2 n);
                dorsal.Add(at - n * (spine.RadiusAt(t) + 1.5f + 1.5f * Mathf.Sin(t * Mathf.PI)));
            }

            var anal = new List<Vector2>();
            for (float t = 0.0f; t <= 0.55f; t += 0.02f)
            {
                Vector2 at = spine.At(t, out Vector2 n);
                anal.Add(at + n * (spine.RadiusAt(t) + 1f));
            }

            Color FinTexture(int x, int y, Color c) => (x + y) % 5 == 0 ? Darker(c, 0.78f) : Hash(x / 2, y / 2) < 0.2f ? Spot : c;
            cv.Piece(Tube(dorsal, 2.4f, 3.6f), SkinDark, 3f, FinTexture, 0.85f, 0.45f);
            cv.Piece(Tube(anal, 2.2f, 2.8f), SkinDark, 3f, FinTexture, 0.85f, 0.45f);

            // ---------- Le corps ----------
            cv.Piece(spine.Inside, Skin, 11f, (x, y, c) =>
            {
                spine.Frame(x + 0.5f, y + 0.5f, out float t, out float s, out float r);
                float u = t * 178f / 5.2f;
                float v = (s + 1f) * r / 5.2f;
                if (s > 0.55f)
                {
                    Color belly = Leopard(u * 1.4f, v * 1.4f, 0f, BellyColor);
                    return Color.Lerp(belly, BellyColor, 0.45f);
                }

                return Leopard(u, v, Mathf.InverseLerp(0.4f, -1f, s), Skin);
            }, 0.82f, 0.38f);

            // Plis de la peau derrière la tête (la murène est très musclée).
            for (int k = 0; k < 4; k++)
            {
                float t = 0.80f + k * 0.035f;
                Vector2 at = spine.At(t, out Vector2 n);
                float r = spine.RadiusAt(t);
                cv.Piece(Tube(Curve(at - n * r * 0.7f, at + new Vector2(2, 0), at + n * r * 0.6f, 6), 0.7f, 0.5f), SkinDark, 0.6f, null, 0.95f, 0.7f);
            }

            // ---------- La tête ----------
            Vector2 h = spine.Points[spine.Points.Count - 1] + new Vector2(4f, 0f);
            const float HS = 1.3f;   // la tête est grosse pour le corps : c'est elle qui fait peur
            Vector2 H(float x, float y) => h + new Vector2(x, y) * HS;

            Color HeadTexture(int x, int y, Color c)
            {
                Color leopard = Leopard(x / 4.6f, y / 4.6f, 0.4f, c);
                return y > h.y + HS * 4 ? Color.Lerp(leopard, BellyColor, 0.35f) : leopard;
            }

            // Mâchoire du bas, grande ouverte.
            Vector2 jaw = H(40f, 27f);
            cv.Piece(Union(Capsule(h.x + HS * 2, h.y + HS * 9, jaw.x, jaw.y, 8f), Ellipse(h.x + HS * 6, h.y + HS * 10, 13f, 9f)), Skin, 7f, HeadTexture);

            // L'intérieur de la gueule, et la deuxième mâchoire qui sort du fond de la gorge.
            cv.Piece(Poly(H(8, 2), H(47, -6), jaw + new Vector2(2, -6), H(8, 13)), Maw, 1.6f);
            cv.Piece(Capsule(h.x + HS * 10, h.y + HS * 7, h.x + HS * 28, h.y + HS * 4, 4.2f), Throat, 4f, (x, y, c) => (x + y) % 4 == 0 ? Darker(c, 0.8f) : c);
            for (int k = 0; k < 4; k++)
            {
                float fx = h.x + HS * 22 + k * 2.4f;
                cv.Piece(Poly(new Vector2(fx - 1, h.y + HS * 1), new Vector2(fx + 1, h.y + HS * 1), new Vector2(fx + 0.6f, h.y - HS * 3)), Tooth, 0.8f, null, 0.95f, 0.8f);
                cv.Piece(Poly(new Vector2(fx - 1, h.y + HS * 7.5f), new Vector2(fx + 1, h.y + HS * 7.5f), new Vector2(fx + 0.6f, h.y + HS * 11)), Tooth, 0.8f, null, 0.95f, 0.8f);
            }

            // Crocs du bas, recourbés vers l'arrière.
            for (int k = 0; k < 8; k++)
            {
                Vector2 root = Vector2.Lerp(H(14, 13), jaw + new Vector2(-2, -5), k / 7f);
                float len = 3.5f + Hash(k, 41) * 3f + (k == 7 ? 4f : 0f);
                cv.Piece(Poly(root + new Vector2(-1.3f, 0), root + new Vector2(1.3f, 0), root + new Vector2(-1.6f, -len)), Tooth, 1f, null, 0.95f, 0.75f);
            }

            // Mâchoire du haut et crâne massif.
            cv.Piece(Union(Ellipse(h.x, h.y - HS * 1, 24f, 19f), Poly(H(4, -14), H(48, -10), H(49, -4), H(8, 4))),
                Skin, 10f, HeadTexture);
            cv.Piece(Capsule(h.x + HS * 6, h.y - HS * 12, h.x + HS * 34, h.y - HS * 11, 2.8f), SkinDark, 2.5f, HeadTexture);   // arcade au-dessus de l'œil

            // Crocs du haut : de longues canines devant, des aiguilles derrière.
            for (int k = 0; k < 9; k++)
            {
                Vector2 root = Vector2.Lerp(H(12, 3), H(47, -4), k / 8f);
                float len = 3.5f + Hash(k, 51) * 3.5f + (k >= 7 ? 6f : 0f);
                cv.Piece(Poly(root + new Vector2(-1.4f, 0), root + new Vector2(1.4f, 0), root + new Vector2(-1.8f, len)), Tooth, 1f, null, 0.95f, 0.75f);
            }

            // Narines en tubes au bout du museau.
            cv.Piece(Capsule(h.x + HS * 44, h.y - HS * 9, h.x + HS * 48, h.y - HS * 15, 1.3f), SkinDark, 1.2f);
            cv.Piece(Capsule(h.x + HS * 37, h.y - HS * 12, h.x + HS * 39, h.y - HS * 17, 1.1f), SkinDark, 1.1f);

            // Œil petit et haut placé, qui luit ; ouïe sombre derrière la tête.
            cv.Piece(Ellipse(h.x + HS * 27, h.y - HS * 8, 3.8f, 3f), C(0.02f, 0.04f, 0.04f), 2f);
            cv.Piece(Ellipse(h.x + HS * 27, h.y - HS * 8, 2.5f, 2f), Glow, 2f);
            cv.Dot((int)(h.x + HS * 27), (int)(h.y - HS * 8), C(0.02f, 0.10f, 0.10f));
            cv.Piece(Ellipse(h.x - HS * 9, h.y + HS * 5, 2.6f, 1.8f), Spot, 1.5f);

            // Ce qui a poussé sur sa tête : une anémone, une petite branche de corail, des balanes.
            for (int k = 0; k < 9; k++)
            {
                float a = Mathf.Lerp(-2.6f, -0.5f, k / 8f);
                Vector2 root = H(-2, -14);
                Vector2 tip = root + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (8f + (k % 3) * 2.5f);
                cv.Piece(Tube(Curve(root, Vector2.Lerp(root, tip, 0.6f) + new Vector2(1, -1), tip, 5), 1.3f, 0.7f), k % 2 == 0 ? C(0.62f, 0.86f, 0.62f) : C(0.74f, 0.56f, 0.82f), 1.2f);
            }

            cv.Piece(Ellipse(h.x - HS * 2, h.y - HS * 14, 4f, 2.4f), C(0.58f, 0.34f, 0.58f), 2f);                     // pied de l'anémone
            cv.Piece(Tube(Curve(H(12, -14), H(10, -22), H(16, -27), 6), 1.6f, 0.9f), C(0.92f, 0.46f, 0.42f), 1.4f);
            cv.Piece(Tube(Curve(H(11, -20), H(7, -23), H(5, -26), 4), 1.1f, 0.7f), C(0.92f, 0.46f, 0.42f), 1f);
            foreach ((float bx, float by) in new[] { (h.x + HS * 18f, h.y - HS * 13f), (h.x + HS * 22f, h.y - HS * 14f), (h.x - HS * 10f, h.y - HS * 12f), (h.x - HS * 14f, h.y - HS * 9f) })
            {
                cv.Piece(Ellipse(bx, by, 1.8f, 1.5f), C(0.84f, 0.82f, 0.74f), 1.4f, (x, y, c) => Mathf.Abs(x - bx) < 0.8f && Mathf.Abs(y - by) < 0.8f ? Spot : c);
            }

            // Balanes et cicatrices le long du corps.
            for (int k = 0; k < 10; k++)
            {
                float t = 0.3f + Hash(k, 61) * 0.45f;
                Vector2 at = spine.At(t, out Vector2 n);
                Vector2 b = at - n * spine.RadiusAt(t) * (0.5f + Hash(k, 63) * 0.4f);
                cv.Piece(Ellipse(b.x, b.y, 1.5f, 1.3f), C(0.82f, 0.80f, 0.72f), 1.3f);
            }

            foreach ((float t0, float t1, float s0, float s1) in new[] { (0.62f, 0.68f, -0.7f, 0.3f), (0.70f, 0.74f, -0.5f, 0.5f) })
            {
                Vector2 a = spine.At(t0, out Vector2 n0) + n0 * spine.RadiusAt(t0) * s0;
                Vector2 b = spine.At(t1, out Vector2 n1) + n1 * spine.RadiusAt(t1) * s1;
                cv.Piece(Capsule(a.x, a.y, b.x, b.y, 0.9f), C(0.62f, 0.54f, 0.46f), 0.8f, null, 0.95f, 0.7f);
            }

            // Vieil hameçon rouillé planté dans la lèvre du bas.
            Vector2 hook = jaw + new Vector2(-8, 4);
            cv.Piece(Tube(Curve(hook, hook + new Vector2(3, 5), hook + new Vector2(-1, 7), 6), 1.1f, 0.6f), C(0.58f, 0.34f, 0.20f), 1f);

            Color[,] p = cv.Finish(C(0.03f, 0.04f, 0.03f), 0.55f);

            // ---------- Après le contour (y depuis le bas) ----------
            int Y(float y) => Height - 1 - Mathf.RoundToInt(y);
            Halo(p, Mathf.RoundToInt(h.x + 27), Y(h.y - 8), 6, Glow, 2.6f);
            for (int k = 0; k < 9; k++)
            {
                Mix(p, Mathf.RoundToInt(hook.x - k * 0.4f), Y(hook.y + 8 + k), C(0.88f, 0.90f, 0.92f), 0.5f);   // le bout de ligne
            }

            // Quelques taches qui luisent faiblement près de la tête : l'idole l'a marquée.
            for (int k = 0; k < 7; k++)
            {
                float t = 0.80f + Hash(k, 71) * 0.18f;
                Vector2 at = spine.At(t, out Vector2 n);
                Vector2 q = at + n * spine.RadiusAt(t) * (Hash(k, 73) - 0.6f);
                Mix(p, Mathf.RoundToInt(q.x), Y(q.y), Glow, 0.45f);
            }

            return p;
        }
    }
}
