using System;
using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// La légende de la baie d'Ha Long : un dragon vietnamien, dessin détaillé et sombre dans le style en volume
    /// (<see cref="VolumeCanvas"/>), de profil, tête à droite, 320×176. Corps de serpent mince qui ondule en trois
    /// vagues, écailles de jade sombre qui suivent la courbe, ventre à plaques de bronze, crête continue de petites
    /// flammes, quatre pattes à trois griffes avec des touffes de flammes aux coudes, queue terminée par une gerbe de
    /// flammes. Tête : museau qui se recourbe en volute, gueule ouverte aux crocs inégaux et longue langue enroulée,
    /// petites cornes en bois de cerf, crinière qui flotte vers l'arrière, très longues moustaches ondulantes, barbe ;
    /// il poursuit une perle qui luit devant sa gueule. Cicatrices et balanes, œil d'ambre. y depuis le haut pendant le
    /// dessin.
    /// </summary>
    public static class HaLongDragonArt
    {
        public const int Width = 320;
        public const int Height = 176;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Dorsal = C(0.03f, 0.11f, 0.09f);
        private static readonly Color ScaleMid = C(0.07f, 0.21f, 0.16f);
        private static readonly Color ScaleLight = C(0.22f, 0.44f, 0.34f);
        private static readonly Color Belly = C(0.62f, 0.52f, 0.32f);
        private static readonly Color BellySeam = C(0.34f, 0.26f, 0.14f);
        private static readonly Color Flame = C(0.44f, 0.28f, 0.10f);
        private static readonly Color FlameRay = C(0.28f, 0.16f, 0.05f);
        private static readonly Color FlameTip = C(0.74f, 0.56f, 0.22f);
        private static readonly Color Hair = C(0.30f, 0.20f, 0.10f);
        private static readonly Color HairLight = C(0.56f, 0.40f, 0.18f);
        private static readonly Color Bone = C(0.76f, 0.70f, 0.56f);
        private static readonly Color BoneTip = C(0.94f, 0.90f, 0.80f);
        private static readonly Color Maw = C(0.12f, 0.02f, 0.04f);
        private static readonly Color Tongue = C(0.50f, 0.12f, 0.16f);
        private static readonly Color Amber = C(1f, 0.74f, 0.26f);
        private static readonly Color Pearl = C(0.86f, 0.98f, 0.92f);

        private static Spine BuildSpine()
        {
            var spine = new Spine();
            const int n = 110;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float x = 22f + t * 198f;
                float y = 100f + Mathf.Sin(t * Mathf.PI * 4f + 0.4f) * 21f * (1f - t * 0.35f) - t * 16f;
                spine.Points.Add(new Vector2(x, y));
                float body = 2f + 10.5f * Mathf.Pow(Mathf.Sin(Mathf.Clamp01(t / 0.9f) * Mathf.PI * 0.6f), 1.1f);
                if (t > 0.9f) body = Mathf.Lerp(body, 9f, (t - 0.9f) / 0.1f);   // le cou
                spine.Radius.Add(body);
            }

            return spine;
        }

        /// <summary>Une langue de flamme : base entre b0 et b1, pointe en tip, ventre déporté par bend.</summary>
        private static Func<float, float, bool> FlameShape(Vector2 b0, Vector2 b1, Vector2 tip, Vector2 bend)
            => Poly(b0, Vector2.Lerp(b0, tip, 0.5f) + bend, tip, Vector2.Lerp(b1, tip, 0.55f) + bend * 0.5f, b1);

        private static Color FlameTexture(int x, int y, Color c, Vector2 tip)
        {
            if (Vector2.Distance(new Vector2(x, y), tip) < 3.5f) return FlameTip;
            return (x + y / 2) % 4 == 0 ? FlameRay : c;
        }

        /// <summary>Une touffe de flammes (coude, bout de queue) : plusieurs langues en éventail.</summary>
        private static void Tuft(VolumeCanvas cv, Vector2 root, float angleFrom, float angleTo, int count, float length)
        {
            for (int k = 0; k < count; k++)
            {
                float a = Mathf.Lerp(angleFrom, angleTo, count == 1 ? 0.5f : k / (float)(count - 1));
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var side = new Vector2(-dir.y, dir.x);
                float len = length * (k % 2 == 0 ? 1f : 0.7f);
                Vector2 tip = root + dir * len + side * len * 0.25f;
                cv.Piece(FlameShape(root - side * 2.4f, root + side * 2.4f, tip, side * len * 0.18f), Flame, 2f,
                    (x, y, c) => FlameTexture(x, y, c, tip), 0.85f, 0.5f);
            }
        }

        /// <summary>Une patte : bras, avant-bras, trois griffes écartées, touffe de flammes au coude.</summary>
        private static void Leg(VolumeCanvas cv, Vector2 shoulder, Vector2 elbow, Vector2 wrist, float thickness)
        {
            Tuft(cv, elbow + new Vector2(-1f, 0f), 2.3f, 3.6f, 3, 15f);
            cv.Piece(Union(Capsule(shoulder.x, shoulder.y, elbow.x, elbow.y, thickness), Capsule(elbow.x, elbow.y, wrist.x, wrist.y, thickness * 0.75f)), ScaleMid, thickness,
                (x, y, c) => (x + 2 * y) % 5 == 0 ? Darker(c, 0.7f) : c);
            var claws = new[] { new Vector2(9, -3), new Vector2(10, 4), new Vector2(5, 9) };
            foreach (Vector2 claw in claws)
            {
                var finger = Curve(wrist, wrist + claw * 0.6f + new Vector2(0, -1.5f), wrist + claw, 6);
                cv.Piece(Tube(finger, 1.8f, 1.2f), ScaleMid, 1.4f);
                var nail = Curve(wrist + claw, wrist + claw * 1.2f + new Vector2(1f, -1f), wrist + claw * 1.35f + new Vector2(2f, 2f), 5);
                cv.Piece(Tube(nail, 1.2f, 0.3f), BoneTip, 1f);
            }
        }

        public static Color[,] Draw()
        {
            var cv = new VolumeCanvas(Width, Height);
            Spine spine = BuildSpine();

            // ---------- Pattes du côté lointain (derrière le corps, plus sombres) ----------
            foreach (float t in new[] { 0.36f, 0.76f })
            {
                Vector2 s = spine.At(t, out Vector2 n) + n * spine.RadiusAt(t) * 0.3f + new Vector2(6f, 0f);
                cv.Piece(Union(Capsule(s.x, s.y, s.x + 8f, s.y + 13f, 3.4f), Capsule(s.x + 8f, s.y + 13f, s.x + 15f, s.y + 20f, 2.6f)), Darker(ScaleMid, 0.6f), 3f);
                foreach (Vector2 claw in new[] { new Vector2(6, -2), new Vector2(6, 3), new Vector2(3, 6) })
                {
                    Vector2 w = new Vector2(s.x + 15f, s.y + 20f);
                    cv.Piece(Tube(Curve(w, w + claw * 0.7f, w + claw * 1.3f + new Vector2(1f, 1f), 5), 1.2f, 0.3f), Darker(Bone, 0.7f), 1f);
                }
            }

            // ---------- Queue : gerbe de flammes ----------
            Vector2 tail = spine.Points[0];
            Tuft(cv, tail + new Vector2(4f, 0f), 2.4f, 3.9f, 7, 30f);

            // ---------- Crête : une rangée continue de petites flammes couchées vers l'arrière ----------
            int index = 0;
            for (float t = 0.05f; t < 0.9f; t += 0.034f, index++)
            {
                float t1 = t + 0.04f;
                Vector2 a0 = spine.At(t, out Vector2 n0);
                Vector2 a1 = spine.At(t1, out Vector2 n1);
                Vector2 mid = spine.At(t + 0.02f, out Vector2 nm);
                Vector2 up = -nm;
                Vector2 along = new Vector2(nm.y, -nm.x);
                float r = spine.RadiusAt(t + 0.02f);
                float len = (5f + 9f * Mathf.Sin(t / 0.9f * Mathf.PI)) * (0.75f + 0.5f * Hash(index, 9));
                Vector2 b0 = a0 - n0 * (spine.RadiusAt(t) - 2f);
                Vector2 b1 = a1 - n1 * (spine.RadiusAt(t1) - 2f);
                Vector2 tip = mid + up * (r + len) - along * len * 0.9f;
                // La pointe se recourbe un peu vers l'avant, comme une flamme.
                cv.Piece(FlameShape(b0, b1, tip, up * len * 0.25f + along * len * 0.2f), Flame, 2.4f,
                    (x, y, c) => FlameTexture(x, y, c, tip), 0.8f, 0.45f);
                // Une fine arête d'os au cœur de chaque flamme.
                cv.Piece(Tube(Curve(Vector2.Lerp(b0, b1, 0.5f), Vector2.Lerp(Vector2.Lerp(b0, b1, 0.5f), tip, 0.5f) + up * 1.5f, tip, 6), 1.1f, 0.3f), Bone, 1f);
            }

            Vector2 neck = spine.Points[spine.Points.Count - 1];
            Vector2 h = neck + new Vector2(8f, -2f);

            // ---------- Le corps : écailles de jade le long de la courbe, ventre à plaques de bronze ----------
            cv.Piece((x, y) => spine.Frame(x, y, out _, out _, out _), ScaleMid, 8f, (x, y, c) =>
            {
                spine.Frame(x + 0.5f, y + 0.5f, out float t, out float s, out float r);
                if (s > 0.5f)
                {
                    float seg = Mathf.Repeat(t * 80f, 1f);
                    return seg < 0.18f ? BellySeam : s > 0.9f ? Darker(Belly, 0.8f) : Belly;
                }

                float along = t * 200f / 2.8f;
                float across = (s + 1f) * r / 2.3f;
                int row = Mathf.FloorToInt(across);
                if (row % 2 == 1) along += 0.5f;
                float fa = Mathf.Repeat(along, 1f), fb = Mathf.Repeat(across, 1f);
                float arc = (fa - 0.15f) * (fa - 0.15f) * 1.6f + (fb - 0.5f) * (fb - 0.5f);
                Color baseColor = Color.Lerp(Dorsal, ScaleMid, Mathf.InverseLerp(-1f, -0.2f, s));
                if (arc > 0.26f && arc < 0.36f) return Darker(baseColor, 0.6f);
                if (arc < 0.06f) return Color.Lerp(baseColor, ScaleLight, 0.6f);
                if (s > 0.34f) return Color.Lerp(baseColor, Belly, (s - 0.34f) * 2.4f * 0.5f);
                return Hash(x, y) < 0.04f ? Darker(baseColor, 1.25f) : baseColor;
            }, 0.82f, 0.38f);

            // ---------- Crinière : mèches qui flottent vers l'arrière, par-dessus le cou ----------
            for (int k = 0; k < 9; k++)
            {
                float u = k / 8f;
                Vector2 root = h + new Vector2(-4f - u * 14f, -8f + u * 4f);
                float len = 38f + Hash(k, 13) * 18f;
                float wave = (k % 2 == 0 ? 1f : -1f) * (4f + Hash(k, 17) * 4f);
                var a = Curve(root, root + new Vector2(-len * 0.35f, -10f - u * 4f + wave), root + new Vector2(-len * 0.65f, -6f + u * 8f), 8);
                var b = Curve(a[a.Count - 1], a[a.Count - 1] + new Vector2(-len * 0.2f, -wave), a[a.Count - 1] + new Vector2(-len * 0.4f, 2f + u * 6f), 8);
                a.AddRange(b);
                Color strand = k % 3 == 1 ? HairLight : Hair;
                cv.Piece(Tube(a, 2.2f - u * 0.6f, 0.4f), strand, 2f, (x, y, c) => (x - y) % 3 == 0 ? Darker(c, 0.75f) : c, 0.85f, 0.45f);
            }

            // Cicatrice pâle en travers du flanc, et quelques balanes sur le dos.
            {
                Vector2 a = spine.At(0.56f, out Vector2 na) - na * spine.RadiusAt(0.56f) * 0.7f;
                Vector2 b = spine.At(0.61f, out Vector2 nb) + nb * spine.RadiusAt(0.61f) * 0.2f;
                cv.Piece(Capsule(a.x, a.y, b.x, b.y, 0.8f), C(0.52f, 0.50f, 0.42f), 0.8f, null, 0.95f, 0.7f);
            }

            for (int k = 0; k < 8; k++)
            {
                float t = 0.3f + Hash(k, 11) * 0.36f;
                Vector2 at = spine.At(t, out Vector2 n);
                Vector2 b = at - n * spine.RadiusAt(t) * (0.5f + Hash(k, 5) * 0.3f);
                float rr = 1f + Hash(k, 7) * 1.1f;
                cv.Piece(Ellipse(b.x, b.y, rr, rr * 0.85f), C(0.72f, 0.70f, 0.62f), 1.4f, (x, y, c) => Vector2.Distance(new Vector2(x, y), b) < rr * 0.45f ? C(0.20f, 0.18f, 0.16f) : c);
            }

            // ---------- Pattes du côté proche ----------
            foreach (float t in new[] { 0.32f, 0.72f })
            {
                Vector2 shoulder = spine.At(t, out Vector2 n) + n * spine.RadiusAt(t) * 0.45f;
                Leg(cv, shoulder, shoulder + new Vector2(-8f, 13f), shoulder + new Vector2(2f, 24f), 4f);
            }

            // ---------- La tête ----------
            // Cornes en bois de cerf : une branche principale vers l'arrière et deux andouillers.
            foreach ((Vector2 from, Vector2 ctrl, Vector2 to, float r) in new[]
                     {
                         (h + new Vector2(2, -10), h + new Vector2(-8, -24), h + new Vector2(-26, -30), 2.8f),
                         (h + new Vector2(-6, -21), h + new Vector2(-6, -29), h + new Vector2(-2, -35), 1.6f),
                         (h + new Vector2(-16, -27), h + new Vector2(-18, -34), h + new Vector2(-14, -39), 1.4f),
                     })
            {
                cv.Piece(Tube(Curve(from, ctrl, to, 12), r, 0.6f), Bone, 2.2f, (x, y, c) => ((int)(x * 0.6f + y * 0.4f)) % 3 == 0 ? Darker(c, 0.72f) : c);
            }

            // Mâchoire du bas, ouverte.
            Vector2 jawTip = h + new Vector2(40f, 20f);
            cv.Piece(Union(Capsule(h.x + 2, h.y + 9, jawTip.x, jawTip.y, 4.4f), Ellipse(h.x + 4, h.y + 10, 8f, 5.5f)), ScaleMid, 4.5f,
                (x, y, c) => y > h.y + 13 + (x - h.x) * 0.25f ? Belly : (x + y) % 5 == 0 ? Darker(c, 0.7f) : c);

            // Barbe : mèches qui pendent sous le menton.
            for (int k = 0; k < 5; k++)
            {
                Vector2 root = h + new Vector2(4f + k * 5f, 13f + k * 1.2f);
                cv.Piece(Tube(Curve(root, root + new Vector2(-4f, 7f), root + new Vector2(-9f + Hash(k, 3) * 4f, 14f + Hash(k, 4) * 6f), 7), 1.8f, 0.4f),
                    k % 2 == 0 ? Hair : HairLight, 1.4f);
            }

            // L'intérieur de la gueule, et la longue langue qui sort et s'enroule vers le haut.
            cv.Piece(Poly(h + new Vector2(8, 4), h + new Vector2(44, 0), jawTip + new Vector2(2, -4), h + new Vector2(6, 11)), Maw, 1.5f);
            var tongue = Curve(h + new Vector2(10, 9), h + new Vector2(34, 16), h + new Vector2(52, 12), 10);
            tongue.AddRange(Curve(h + new Vector2(52, 12), h + new Vector2(60, 8), h + new Vector2(56, 3), 8));
            cv.Piece(Tube(tongue, 2.4f, 0.9f), Tongue, 2f);

            for (int k = 0; k < 7; k++)
            {
                float u = k / 6f;
                Vector2 root = Vector2.Lerp(h + new Vector2(12, 13), jawTip + new Vector2(-2, -3), u);
                float len = 3f + Hash(k, 21) * 4f + (k == 5 ? 3f : 0f);
                cv.Piece(Poly(root + new Vector2(-1.3f, 0), root + new Vector2(1.3f, 0), root + new Vector2(-1.1f, -len)), BoneTip, 1f, null, 0.95f, 0.75f);
            }

            // Crâne et long museau, dont le bout se relève en volute.
            cv.Piece(Union(Ellipse(h.x + 4, h.y - 1, 16f, 11.5f), Poly(h + new Vector2(8, -11), h + new Vector2(44, -6), h + new Vector2(46, 2), h + new Vector2(10, 7))), ScaleMid, 8f,
                (x, y, c) =>
                {
                    float hx = Hash(x / 3, y / 3);
                    if (y > h.y + 1 && x > h.x + 8) return Darker(c, 0.85f);
                    return hx < 0.18f ? Darker(c, 0.7f) : hx > 0.9f ? Color.Lerp(c, ScaleLight, 0.5f) : c;
                });
            var curl = Curve(h + new Vector2(38, -5), h + new Vector2(50, -8), h + new Vector2(50, -16), 8);
            curl.AddRange(Curve(h + new Vector2(50, -16), h + new Vector2(48, -22), h + new Vector2(42, -18), 8));
            cv.Piece(Tube(curl, 4f, 1.4f), ScaleMid, 3f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.7f) : c);
            cv.Piece(Ellipse(h.x + 43, h.y - 3, 1.5f, 1.1f), C(0.02f, 0.04f, 0.04f), 1f);   // narine

            // Sourcil en flamme, balayé vers l'arrière au-dessus de l'œil.
            {
                Vector2 tip = h + new Vector2(-12f, -20f);
                cv.Piece(FlameShape(h + new Vector2(22, -10), h + new Vector2(8, -6), tip, new Vector2(0f, -5f)), Flame, 2.2f, (x, y, c) => FlameTexture(x, y, c, tip), 0.8f, 0.45f);
            }

            for (int k = 0; k < 8; k++)
            {
                float u = k / 7f;
                Vector2 root = Vector2.Lerp(h + new Vector2(10, 5), h + new Vector2(44, 1), u);
                float len = 4f + Hash(k, 31) * 4f + (k == 1 || k == 6 ? 4f : 0f);
                cv.Piece(Poly(root + new Vector2(-1.4f, 0), root + new Vector2(1.4f, 0), root + new Vector2(-1.2f, len)), BoneTip, 1f, null, 0.95f, 0.75f);
            }

            // Œil d'ambre enfoncé, pupille fendue.
            cv.Piece(Ellipse(h.x + 16, h.y - 4, 5f, 3.4f), C(0.03f, 0.04f, 0.03f), 2f);
            cv.Piece(Ellipse(h.x + 16, h.y - 4, 3.3f, 2.3f), Amber, 2f);
            cv.Piece(Capsule(h.x + 16, h.y - 6, h.x + 16, h.y - 2, 0.6f), C(0.06f, 0.03f, 0.01f), 0.5f);

            // Très longues moustaches qui ondulent vers l'arrière, au-dessus du corps.
            foreach ((Vector2 from, Vector2 c1, Vector2 mid, Vector2 c2, Vector2 to) in new[]
                     {
                         (h + new Vector2(44, -6), h + new Vector2(50, -30), h + new Vector2(30, -30), h + new Vector2(14, -30), h + new Vector2(2, -44)),
                         (h + new Vector2(40, 0), h + new Vector2(50, 30), h + new Vector2(20, 34), h + new Vector2(-8, 38), h + new Vector2(-36, 52)),
                     })
            {
                var whisker = Curve(from, c1, mid, 14);
                whisker.AddRange(Curve(mid, c2, to, 14));
                cv.Piece(Tube(whisker, 1.6f, 0.5f), C(0.22f, 0.32f, 0.26f), 1.2f);
            }

            // La perle que poursuit le dragon, devant sa gueule.
            Vector2 pearl = h + new Vector2(70f, 6f);
            cv.Piece(Ellipse(pearl.x, pearl.y, 6.5f, 6.5f), Pearl, 6f, (x, y, c) => Vector2.Distance(new Vector2(x, y), pearl + new Vector2(-2f, -2f)) < 1.8f ? Color.white : c, 0.9f, 0.6f);

            Color[,] p = cv.Finish(C(0.02f, 0.04f, 0.03f), 0.55f);

            // ---------- Après le contour (y depuis le bas) : halos de la perle et de l'œil ----------
            int Y(float y) => Height - 1 - Mathf.RoundToInt(y);
            Halo(p, Mathf.RoundToInt(pearl.x), Y(pearl.y), 22, Pearl, 7.5f);
            // Aura pâle autour de la perle, dans l'eau.
            for (int x = Mathf.RoundToInt(pearl.x) - 12; x <= Mathf.RoundToInt(pearl.x) + 12; x++)
            {
                for (int y = Y(pearl.y) - 12; y <= Y(pearl.y) + 12; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(pearl.x, Y(pearl.y)));
                    if (d > 7.6f && d < 12f && x >= 0 && y >= 0 && x < Width && y < Height && p[x, y].a < 0.01f && Hash(x, y) < 0.5f)
                    {
                        Mix(p, x, y, Pearl, 0.35f * (1f - (d - 7.6f) / 4.4f));
                    }
                }
            }

            Halo(p, Mathf.RoundToInt(h.x + 16), Y(h.y - 4), 6, Amber);
            return p;
        }
    }
}
