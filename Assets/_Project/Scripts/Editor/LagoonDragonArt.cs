using System;
using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// La légende de l'île Mahana en dragon des mers, dessin détaillé dans le style en volume (<see cref="VolumeCanvas"/>),
    /// de profil, tête à droite, 288×160 : long corps de serpent en S aux écailles qui suivent la courbe, ventre à plaques
    /// osseuses, crête d'épines d'os reliées par une membrane sombre et déchirée, collerettes épineuses derrière la tête,
    /// tête anguleuse (arcade, œil enfoncé qui luit, gueule ouverte aux crocs inégaux, cornes d'os balayées vers
    /// l'arrière, barbillons), patte avant palmée à griffes, queue à nageoire déchirée ; cicatrices, balanes, vieil
    /// hameçon planté dans le cou, sigils qui luisent faiblement le long du flanc. y depuis le haut pendant le dessin.
    /// </summary>
    public static class LagoonDragonArt
    {
        public const int Width = 288;
        public const int Height = 160;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Dorsal = C(0.05f, 0.12f, 0.16f);
        private static readonly Color ScaleMid = C(0.11f, 0.26f, 0.30f);
        private static readonly Color ScaleLight = C(0.26f, 0.46f, 0.46f);
        private static readonly Color Belly = C(0.64f, 0.62f, 0.52f);
        private static readonly Color BellySeam = C(0.36f, 0.32f, 0.27f);
        private static readonly Color Membrane = C(0.30f, 0.07f, 0.12f);
        private static readonly Color MembraneRay = C(0.18f, 0.03f, 0.07f);
        private static readonly Color Bone = C(0.78f, 0.74f, 0.62f);
        private static readonly Color BoneTip = C(0.94f, 0.91f, 0.82f);
        private static readonly Color Gum = C(0.36f, 0.06f, 0.10f);
        private static readonly Color Maw = C(0.12f, 0.02f, 0.05f);
        private static readonly Color Glow = C(0.40f, 1f, 0.90f);

        private static Spine BuildSpine()
        {
            var spine = new Spine();
            const int n = 90;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float x = 14f + t * 206f;
                float y = 98f + Mathf.Sin(t * Mathf.PI * 2.05f + 0.35f) * 22f * (1f - t * 0.35f) - t * 22f;
                spine.Points.Add(new Vector2(x, y));
                float body = 2.2f + 15f * Mathf.Pow(Mathf.Sin(Mathf.Clamp01(t / 0.9f) * Mathf.PI * 0.62f), 1.25f);
                if (t > 0.88f) body = Mathf.Lerp(body, 11.5f, (t - 0.88f) / 0.12f);   // le cou
                spine.Radius.Add(body);
            }

            return spine;
        }

        public static Color[,] Draw()
        {
            var cv = new VolumeCanvas(Width, Height);
            Spine spine = BuildSpine();

            // ---------- Queue : nageoire en croissant, déchirée ----------
            Vector2 tail = spine.Points[0];
            cv.Piece(Union(Poly(tail + new Vector2(10, -2), tail + new Vector2(-10, -34), tail + new Vector2(-2, -18), tail + new Vector2(4, -4)),
                    Poly(tail + new Vector2(10, 2), tail + new Vector2(-12, 30), tail + new Vector2(-3, 16), tail + new Vector2(4, 4)),
                    Ellipse(tail.x + 4, tail.y, 6f, 5f)),
                Membrane, 4f, (x, y, c) =>
                {
                    // Deux grandes entailles dans le bord de la nageoire.
                    bool notch = Mathf.Abs(x - (tail.x - 6)) < 1.5f && Mathf.Abs(y - tail.y) > 16f;
                    return notch ? C(0, 0, 0, 0) : (x - y) % 4 == 0 ? MembraneRay : c;
                }, 0.85f, 0.5f);
            for (int k = -1; k <= 1; k += 2)
            {
                var ray = Curve(tail + new Vector2(6, 0), tail + new Vector2(-2, k * 14), tail + new Vector2(-9, k * 31), 8);
                cv.Piece(Tube(ray, 1.8f, 0.5f), Bone, 1.4f);
            }

            // ---------- Crête : membrane déchirée tendue entre des épines d'os ----------
            var tips = new List<Vector2>();
            var bases = new List<Vector2>();
            int spikeIndex = 0;
            for (float t = 0.10f; t <= 0.86f; t += 0.062f, spikeIndex++)
            {
                Vector2 at = spine.At(t, out Vector2 normal);
                float r = spine.RadiusAt(t);
                Vector2 up = -normal;
                Vector2 back = new Vector2(-1f, 0f);
                // Grandes épines irrégulières : une sur trois plus courte, comme cassée.
                float broken = spikeIndex % 3 == 2 ? 0.55f : 1f;
                float len = (10f + 24f * Mathf.Sin(t / 0.86f * Mathf.PI) * (0.7f + 0.3f * Hash(spikeIndex, 3))) * broken;
                bases.Add(at + up * (r - 2f));
                tips.Add(at + up * (r + len) + back * len * 0.45f);
            }

            for (int i = 0; i < tips.Count - 1; i++)
            {
                // La membrane s'affaisse entre deux épines (bord creusé), avec une déchirure de temps en temps.
                Vector2 sag = Vector2.Lerp(tips[i], tips[i + 1], 0.5f) + (Vector2.Lerp(bases[i], bases[i + 1], 0.5f) - Vector2.Lerp(tips[i], tips[i + 1], 0.5f)) * 0.35f;
                int panel = i;
                cv.Piece(Poly(bases[i], tips[i], sag, tips[i + 1], bases[i + 1]), Membrane, 3f, (x, y, c) =>
                {
                    if (panel % 4 == 1 && Mathf.Abs(x - sag.x) < 2.2f && y < sag.y + 6f) return C(0, 0, 0, 0);   // déchirure
                    return (x + y / 2) % 5 == 0 ? MembraneRay : c;
                }, 0.8f, 0.45f);
            }

            for (int i = 0; i < tips.Count; i++)
            {
                var pts = Curve(bases[i], Vector2.Lerp(bases[i], tips[i], 0.5f) + new Vector2(2f, -2f), tips[i], 8);
                cv.Piece(Tube(pts, 3.2f, 0.7f), Bone, 2.4f, (x, y, c) => Vector2.Distance(new Vector2(x, y), tips[i]) < 4f ? BoneTip : ((x + y) % 4 == 0 ? Darker(c, 0.82f) : c));
            }

            // ---------- Le corps : écailles le long de la courbe, ventre à plaques ----------
            cv.Piece((x, y) => spine.Frame(x, y, out _, out _, out _), ScaleMid, 10f, (x, y, c) =>
            {
                spine.Frame(x + 0.5f, y + 0.5f, out float t, out float s, out float r);
                if (s > 0.52f)
                {
                    // Plaques osseuses du ventre, segmentées le long du corps.
                    float seg = Mathf.Repeat(t * 70f, 1f);
                    return seg < 0.16f ? BellySeam : s > 0.9f ? Darker(Belly, 0.8f) : Belly;
                }

                // Écailles en rangées décalées : leur bord dessine un arc vers l'arrière.
                float along = t * 210f / 3.2f;
                float across = (s + 1f) * r / 2.6f;
                int row = Mathf.FloorToInt(across);
                if (row % 2 == 1) along += 0.5f;
                float fa = Mathf.Repeat(along, 1f), fb = Mathf.Repeat(across, 1f);
                float arc = (fa - 0.15f) * (fa - 0.15f) * 1.6f + (fb - 0.5f) * (fb - 0.5f);
                Color baseColor = Color.Lerp(Dorsal, ScaleMid, Mathf.InverseLerp(-1f, -0.2f, s));
                if (arc > 0.26f && arc < 0.36f) return Darker(baseColor, 0.62f);               // bord de l'écaille
                if (arc < 0.06f) return Color.Lerp(baseColor, ScaleLight, 0.55f);               // reflet au cœur de l'écaille
                if (s > 0.36f) return Color.Lerp(baseColor, Belly, (s - 0.36f) * 2.2f * 0.5f);  // transition vers le ventre
                return Hash(x, y) < 0.04f ? Darker(baseColor, 1.25f) : baseColor;
            }, 0.82f, 0.38f);

            // Cicatrices pâles en travers des écailles.
            foreach ((float t0, float t1) in new[] { (0.44f, 0.50f), (0.55f, 0.60f) })
            {
                Vector2 a = spine.At(t0, out Vector2 n0) - n0 * spine.RadiusAt(t0) * 0.7f;
                Vector2 b = spine.At(t1, out Vector2 n1) + n1 * spine.RadiusAt(t1) * 0.2f;
                cv.Piece(Capsule(a.x, a.y, b.x, b.y, 0.9f), C(0.56f, 0.50f, 0.46f), 0.8f, null, 0.95f, 0.7f);
            }

            // Balanes accrochées sur le haut du dos.
            for (int k = 0; k < 14; k++)
            {
                float t = 0.36f + Hash(k, 11) * 0.3f;
                Vector2 at = spine.At(t, out Vector2 n);
                Vector2 b = at - n * spine.RadiusAt(t) * (0.55f + Hash(k, 5) * 0.35f);
                float rr = 1.2f + Hash(k, 7) * 1.4f;
                cv.Piece(Ellipse(b.x, b.y, rr, rr * 0.85f), C(0.76f, 0.74f, 0.66f), 1.6f, (x, y, c) => Vector2.Distance(new Vector2(x, y), b) < rr * 0.45f ? C(0.22f, 0.20f, 0.18f) : c);
            }

            // ---------- Patte avant : bras, avant-bras, main palmée à trois griffes ----------
            Vector2 shoulder = spine.At(0.80f, out Vector2 sn) + sn * spine.RadiusAt(0.80f) * 0.5f;
            Vector2 elbow = shoulder + new Vector2(-10f, 16f);
            Vector2 wrist = elbow + new Vector2(10f, 12f);
            cv.Piece(Union(Capsule(shoulder.x, shoulder.y, elbow.x, elbow.y, 5f), Capsule(elbow.x, elbow.y, wrist.x, wrist.y, 3.8f)), ScaleMid, 4.5f,
                (x, y, c) => (x + y) % 4 == 0 ? Darker(c, 0.7f) : c);
            var claws = new[] { new Vector2(10, 4), new Vector2(12, 9), new Vector2(7, 13) };
            cv.Piece(Poly(wrist, wrist + claws[0], wrist + claws[1], wrist + claws[2]), Membrane, 2f, (x, y, c) => (x + y) % 3 == 0 ? MembraneRay : c);
            foreach (Vector2 claw in claws)
            {
                var pts = Curve(wrist + claw * 0.7f, wrist + claw * 1.1f + new Vector2(0, -2), wrist + claw * 1.35f + new Vector2(2, 2), 6);
                cv.Piece(Tube(pts, 1.6f, 0.4f), BoneTip, 1.2f);
            }

            // ---------- La tête ----------
            Vector2 neck = spine.Points[spine.Points.Count - 1];
            Vector2 h = neck + new Vector2(10f, -2f);

            // Collerette épineuse derrière le crâne : une membrane en éventail tendue sur quatre épines, vers l'arrière.
            {
                Vector2 root = h + new Vector2(-4f, 2f);
                var frillTips = new List<Vector2>();
                for (int k = 0; k < 4; k++)
                {
                    float a = Mathf.Lerp(-2.45f, -3.75f, k / 3f);
                    frillTips.Add(root + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (22f + (k % 2 == 0 ? 6f : 0f)));
                }

                var outline = new List<Vector2> { root };
                outline.AddRange(frillTips);
                cv.Piece(Poly(outline.ToArray()), Membrane, 3f, (x, y, c) => (x + y) % 4 == 0 ? MembraneRay : c, 0.85f, 0.5f);
                foreach (Vector2 tip in frillTips)
                {
                    cv.Piece(Tube(Curve(root, Vector2.Lerp(root, tip, 0.5f) + new Vector2(0, -2), tip, 6), 2.2f, 0.5f), Bone, 1.6f);
                }
            }

            // Cornes d'os balayées vers l'arrière, annelées.
            foreach ((Vector2 from, Vector2 ctrl, Vector2 to, float r) in new[]
                     {
                         (h + new Vector2(2, -12), h + new Vector2(-14, -30), h + new Vector2(-40, -36), 4.2f),
                         (h + new Vector2(10, -12), h + new Vector2(0, -26), h + new Vector2(-18, -32), 3f),
                     })
            {
                var pts = Curve(from, ctrl, to, 14);
                cv.Piece(Tube(pts, r, 0.6f), Bone, 3f, (x, y, c) => ((int)(x * 0.6f + y * 0.4f)) % 3 == 0 ? Darker(c, 0.72f) : c);
            }

            // Mâchoire du bas, grande ouverte (pendante), avec les crocs qui pointent vers le haut.
            Vector2 jawTip = h + new Vector2(44f, 26f);
            cv.Piece(Union(Capsule(h.x + 2, h.y + 10, jawTip.x, jawTip.y, 5.2f), Ellipse(h.x + 4, h.y + 12, 9f, 6f)), ScaleMid, 5f,
                (x, y, c) => y > h.y + 16 + (x - h.x) * 0.3f ? Belly : (x + y) % 5 == 0 ? Darker(c, 0.7f) : c);

            // L'intérieur de la gueule : gencives, fond sombre, langue.
            cv.Piece(Poly(h + new Vector2(8, 5), h + new Vector2(48, 0), jawTip + new Vector2(2, -5), h + new Vector2(6, 12)), Maw, 1.5f);
            cv.Piece(Capsule(h.x + 12, h.y + 10, h.x + 34, h.y + 14, 2.4f), C(0.46f, 0.12f, 0.18f), 2f);

            // Crocs du bas : inégaux, recourbés vers l'arrière.
            for (int k = 0; k < 9; k++)
            {
                float u = k / 8f;
                Vector2 root = Vector2.Lerp(h + new Vector2(12, 15), jawTip + new Vector2(-2, -4), u);
                float len = 4f + Hash(k, 21) * 5f + (k == 6 ? 4f : 0f);
                cv.Piece(Poly(root + new Vector2(-1.4f, 0), root + new Vector2(1.4f, 0), root + new Vector2(-1.2f, -len)), BoneTip, 1f, null, 0.95f, 0.75f);
            }

            // Mâchoire du haut et crâne : long, anguleux, plaques sombres, narine.
            cv.Piece(Union(Ellipse(h.x + 4, h.y - 1, 18f, 13f), Poly(h + new Vector2(8, -12), h + new Vector2(50, -2), h + new Vector2(50, 4), h + new Vector2(10, 8))), ScaleMid, 9f,
                (x, y, c) =>
                {
                    float hx = Hash(x / 3, y / 3);
                    if (y > h.y + 2 && x > h.x + 8) return Darker(c, 0.85f);                    // dessous du museau
                    return hx < 0.18f ? Darker(c, 0.7f) : hx > 0.9f ? Color.Lerp(c, ScaleLight, 0.5f) : c;
                });
            cv.Piece(Capsule(h.x - 4, h.y - 11, h.x + 26, h.y - 9, 3.4f), Dorsal, 3f);          // arcade sourcilière
            cv.Piece(Ellipse(h.x + 42, h.y - 2, 1.6f, 1.1f), C(0.02f, 0.04f, 0.05f), 1f);      // narine

            // Crocs du haut : longs, inégaux, qui débordent de la mâchoire.
            for (int k = 0; k < 10; k++)
            {
                float u = k / 9f;
                Vector2 root = Vector2.Lerp(h + new Vector2(10, 6), h + new Vector2(48, 3), u);
                float len = 5f + Hash(k, 31) * 6f + (k == 2 || k == 7 ? 5f : 0f);
                cv.Piece(Poly(root + new Vector2(-1.5f, 0), root + new Vector2(1.5f, 0), root + new Vector2(-1.4f, len)), BoneTip, 1f, null, 0.95f, 0.75f);
            }

            // Œil enfoncé sous l'arcade : orbite sombre, iris turquoise, pupille fendue.
            cv.Piece(Ellipse(h.x + 16, h.y - 5, 5.6f, 3.6f), C(0.02f, 0.05f, 0.06f), 2f);
            cv.Piece(Ellipse(h.x + 16, h.y - 5, 3.6f, 2.4f), Glow, 2f);
            cv.Piece(Capsule(h.x + 16, h.y - 7, h.x + 16, h.y - 3, 0.6f), C(0.02f, 0.08f, 0.08f), 0.5f);

            // Barbillons : longs, qui pendent du museau et s'enroulent.
            foreach ((Vector2 from, Vector2 ctrl, Vector2 to) in new[]
                     {
                         (h + new Vector2(46, 2), h + new Vector2(56, 24), h + new Vector2(34, 46)),
                         (h + new Vector2(40, 4), h + new Vector2(38, 30), h + new Vector2(14, 44)),
                     })
            {
                cv.Piece(Tube(Curve(from, ctrl, to, 14), 1.6f, 0.5f), C(0.20f, 0.34f, 0.34f), 1.2f);
            }

            // Vieil hameçon rouillé planté dans le cou, avec son bout de ligne.
            Vector2 hook = spine.At(0.92f, out Vector2 hn) - hn * 4f;
            cv.Piece(Tube(Curve(hook, hook + new Vector2(3, 5), hook + new Vector2(-1, 7), 6), 1.1f, 0.6f), C(0.56f, 0.34f, 0.20f), 1f);

            Color[,] p = cv.Finish(C(0.02f, 0.05f, 0.07f), 0.55f);

            // ---------- Après le contour (y depuis le bas) ----------
            int Y(float y) => Height - 1 - Mathf.RoundToInt(y);

            // Sigils qui luisent faiblement le long du flanc, comme les yeux de l'idole.
            for (float t = 0.18f; t < 0.84f; t += 0.07f)
            {
                Vector2 at = spine.At(t, out Vector2 n);
                Vector2 c = at - n * spine.RadiusAt(t) * 0.15f;
                int cx = Mathf.RoundToInt(c.x), cy = Y(c.y);
                for (int k = -2; k <= 2; k++)
                {
                    Mix(p, cx + k, cy + Mathf.Abs(k) - 1, Glow, 0.55f);
                }

                Mix(p, cx, cy + 2, Glow, 0.7f);
            }

            // Lueur autour de l'œil, et ligne de pêche qui pend de l'hameçon.
            Halo(p, Mathf.RoundToInt(h.x + 16), Y(h.y - 5), 7, Glow);
            for (int k = 0; k < 10; k++)
            {
                Mix(p, Mathf.RoundToInt(hook.x - 1 - k * 0.5f), Y(hook.y + 7 + k), C(0.88f, 0.90f, 0.92f), 0.5f);
            }

            return p;
        }
    }
}
