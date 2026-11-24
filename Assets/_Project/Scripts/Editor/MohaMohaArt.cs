using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Légende de la Grande Barrière : le Moha-moha, monstre marin aperçu en 1890 près de l'île Fraser (Queensland),
    /// décrit comme une immense tortue au long cou et à queue de poisson. Dessiné comme une vraie tortue marine géante
    /// (anatomie de tortue verte) infestée par le récif : carapace en goutte aux plaques réelles, presque entièrement
    /// couverte de croûtes d'algues roses, de cerveaux de Neptune, de coraux cornes de cerf, de deux
    /// gorgones en éventail, d'éponges en tubes et d'anémones ; du corail jusque sur les nageoires et la tête, de petits
    /// poissons du récif qui y vivent. Tête écailleuse, gros œil sous une paupière lourde, bec dentelé, cou ridé allongé,
    /// longues nageoires avant à écailles et griffe, queue charnue finie en nageoire. De profil, tête à droite, 256×144,
    /// y depuis le haut.
    /// </summary>
    public static class MohaMohaArt
    {
        public const int Width = 256;
        public const int Height = 144;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Ink = C(0.02f, 0.03f, 0.03f);
        private static readonly Color Shell = C(0.34f, 0.27f, 0.15f);
        private static readonly Color ShellDark = C(0.12f, 0.09f, 0.05f);
        private static readonly Color Skin = C(0.30f, 0.34f, 0.24f);
        private static readonly Color SkinDark = C(0.11f, 0.13f, 0.09f);
        private static readonly Color ScaleEdge = C(0.62f, 0.60f, 0.46f);
        private static readonly Color Plastron = C(0.66f, 0.60f, 0.40f);
        private static readonly Color EyeColor = C(0.90f, 0.66f, 0.22f);
        private static readonly Color Crust = C(0.66f, 0.38f, 0.48f);      // algues calcaires roses
        private static readonly Color Brain = C(0.58f, 0.56f, 0.36f);
        private static readonly Color Staghorn = C(0.78f, 0.68f, 0.50f);
        private static readonly Color StagTip = C(0.72f, 0.56f, 0.78f);
        private static readonly Color Fan = C(0.60f, 0.18f, 0.26f);
        private static readonly Color Sponge = C(0.86f, 0.52f, 0.20f);
        private static readonly Color Anemone = C(0.56f, 0.70f, 0.52f);

        private const float Sx = 116f, Sy = 84f, Srx = 62f, Sry = 36f;

        /// <summary>Haut de la carapace au-dessus de x (y depuis le haut).</summary>
        private static float ShellTop(float x)
        {
            float u = (x - Sx) / Srx;
            return Sy - Sry * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)) * (u > 0f ? 1f - u * 0.15f : 1f);
        }

        /// <summary>La carapace en goutte : plus haute et plus large vers l'avant, effilée vers l'arrière.</summary>
        private static bool InShell(float x, float y)
        {
            float u = (x - Sx) / Srx;
            if (Mathf.Abs(u) > 1f) return false;
            float top = ShellTop(x);
            float bottom = Sy + 9f * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u));
            return y >= top && y <= bottom;
        }

        private static Color Mottle(int x, int y, Color c, Color dark)
        {
            float n = Noise(x / 5f + 3f, y / 5f);
            if (n < 0.28f) return Color.Lerp(c, dark, 0.5f);
            if (n > 0.8f) return Color.Lerp(c, Color.white, 0.08f);
            return Hash(x, y) < 0.05f ? Darker(c, 0.8f) : c;
        }

        /// <summary>Peau de tortue : petites écailles polygonales aux joints clairs, plus grandes là où <paramref name="cell"/> l'est.</summary>
        private static Color Scaly(int x, int y, Color c, float cell)
        {
            float u = x / cell, w = y / cell;
            float jx = Noise(u * 1.7f, w * 1.7f) * 0.6f, jy = Noise(u * 1.7f + 9f, w * 1.7f) * 0.6f;
            float fu = Mathf.Repeat(u + jx + (Mathf.FloorToInt(w + jy) % 2) * 0.5f, 1f), fv = Mathf.Repeat(w + jy, 1f);
            float edge = Mathf.Min(Mathf.Min(fu, 1f - fu), Mathf.Min(fv, 1f - fv));
            Color k = Mottle(x, y, c, SkinDark);
            return edge < 0.1f ? Color.Lerp(k, ScaleEdge, 0.45f) : k;
        }

        /// <summary>Plaques de la carapace (rangée du dessus, rangée de côté, bordure), joints foncés et anneaux de croissance.</summary>
        private static Color Scutes(int x, int y, Color c)
        {
            var centers = new List<Vector2>();
            for (int k = 0; k < 5; k++) centers.Add(new Vector2(Sx - Srx * 0.62f + k * Srx * 0.31f, Sy - Sry * 0.7f));
            for (int k = 0; k < 4; k++) centers.Add(new Vector2(Sx - Srx * 0.47f + k * Srx * 0.31f, Sy - Sry * 0.2f));
            for (int k = 0; k < 12; k++) centers.Add(new Vector2(Sx - Srx * 0.92f + k * Srx * 0.165f, Sy + 4f));
            float d1 = float.MaxValue, d2 = float.MaxValue;
            var p = new Vector2(x, y);
            foreach (Vector2 q in centers)
            {
                float d = Vector2.Distance(p, q);
                if (d < d1) { d2 = d1; d1 = d; }
                else if (d < d2) d2 = d;
            }

            Color shade;
            if (d2 - d1 < 1.5f) shade = ShellDark;
            else if (Mathf.Repeat(d1 * 0.5f, 1f) < 0.14f) shade = Color.Lerp(c, C(0.56f, 0.44f, 0.24f), 0.5f);
            else shade = Color.Lerp(c, C(0.50f, 0.38f, 0.20f), Mathf.Clamp01(1f - d1 / 14f) * 0.5f);
            return Mottle(x, y, shade, ShellDark);
        }

        // ---------- Les habitants du récif ----------

        private static void Crusts(VolumeCanvas cv, float x0, float x1, float y0, float y1, int seed)
        {
            cv.Piece((x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1 && InShell(x, y) && Noise(x / 6f + seed, y / 6f) > 0.55f, Crust, 1.5f,
                (x, y, c) => { float n = Noise(x / 1.6f + seed, y / 1.6f); return n > 0.7f ? Color.Lerp(c, Color.white, 0.3f) : n < 0.3f ? Darker(c, 0.7f) : Hash(x, y) < 0.12f ? Darker(c, 0.85f) : c; }, 0.9f, 0.5f);
        }

        private static void BrainCoral(VolumeCanvas cv, float x, float y, float r)
        {
            cv.Piece((px, py) => Ellipse(x, y, r, r * 0.75f)(px, py) && py <= y + r * 0.3f, Brain, r * 0.6f,
                (px, py, c) => Mathf.Abs(Mathf.Sin(px * 0.9f + Mathf.Sin(py * 0.7f) * 2.4f + py * 0.3f)) < 0.32f ? Darker(c, 0.6f) : c, 0.85f, 0.5f);
        }

        private static void StaghornCluster(VolumeCanvas cv, float x, float y, float size, int seed)
        {
            for (int b = 0; b < 4; b++)
            {
                float a = -Mathf.PI * 0.5f + (b - 1.5f) * 0.42f + (Hash(seed, b) - 0.5f) * 0.3f;
                Vector2 root = new Vector2(x + (b - 1.5f) * 2.5f, y);
                Vector2 end = root + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * size * (0.8f + Hash(seed + 3, b) * 0.4f);
                var branch = Curve(root, Vector2.Lerp(root, end, 0.5f) + new Vector2((Hash(seed, b + 7) - 0.5f) * 4f, 0f), end, 8);
                cv.Piece(Tube(branch, 1.8f, 1f), Staghorn, 1.4f, (px, py, c) => Hash(px, py) < 0.2f ? Darker(c, 0.8f) : c, 0.85f, 0.5f);
                cv.Piece(Ellipse(end.x, end.y, 1.3f, 1.3f), StagTip, 1f, null, 0.9f, 0.6f);
                // Une petite ramification.
                Vector2 mid = branch[4];
                Vector2 side = mid + new Vector2(Mathf.Cos(a + 0.7f), Mathf.Sin(a + 0.7f)) * size * 0.35f;
                cv.Piece(Tube(new List<Vector2> { mid, side }, 1.1f, 0.7f), Staghorn, 1f, null, 0.85f, 0.5f);
                cv.Piece(Ellipse(side.x, side.y, 1f, 1f), StagTip, 0.8f, null, 0.9f, 0.6f);
            }
        }

        private static void SeaFan(VolumeCanvas cv, float x, float y)
        {
            Vector2 root = new Vector2(x, y);
            for (int b = 0; b < 7; b++)
            {
                float a = Mathf.Lerp(-2.4f, -0.7f, b / 6f);
                Vector2 end = root + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (18f + Hash(b, 2) * 6f);
                var arm = Curve(root, Vector2.Lerp(root, end, 0.5f) + new Vector2(0f, -2f), end, 10);
                cv.Piece(Tube(arm, 1f, 0.5f), Fan, 0.8f, null, 0.9f, 0.55f);
            }

            // Le réseau fin entre les branches.
            cv.Piece((px, py) =>
            {
                float dx = px - x, dy = py - y;
                float r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx);
                return r > 4f && r < 20f && a > -2.4f && a < -0.7f && ((Mathf.FloorToInt(px) + Mathf.FloorToInt(py)) % 3 == 0 || Mathf.Repeat(r, 4f) < 0.6f);
            }, Color.Lerp(Fan, Color.white, 0.1f), 0.5f, null, 0.95f, 0.55f);
        }

        private static void TubeSponges(VolumeCanvas cv, float x, float y)
        {
            for (int k = 0; k < 3; k++)
            {
                float px = x + k * 4.5f, tall = 8f + k % 2 * 5f;
                cv.Piece(Box(px - 1.8f, y - tall, px + 1.8f, y), Sponge, 2f, (qx, qy, c) => Hash(qx, qy) < 0.25f ? Darker(c, 0.8f) : c, 0.85f, 0.5f);
                cv.Piece(Ellipse(px, y - tall, 1.8f, 0.9f), Darker(Sponge, 0.45f), 0.5f);
            }
        }

        private static void Anemones(VolumeCanvas cv, float x, float y, float size)
        {
            for (int t = 0; t < 8; t++)
            {
                float a = Mathf.Lerp(-2.7f, -0.45f, t / 7f);
                var tentacle = Curve(new Vector2(x, y), new Vector2(x + Mathf.Cos(a) * size * 0.6f, y + Mathf.Sin(a) * size * 0.6f - 1f),
                    new Vector2(x + Mathf.Cos(a + 0.3f) * size, y + Mathf.Sin(a + 0.3f) * size), 6);
                cv.Piece(Tube(tentacle, 0.9f, 0.5f), Anemone, 0.6f, (px, py, c) => py < y - size * 0.7f ? Color.Lerp(c, C(0.86f, 0.60f, 0.80f), 0.6f) : c, 0.9f, 0.6f);
            }
        }

        private static void ReefFish(VolumeCanvas cv, float x, float y, Color body, bool left)
        {
            float d = left ? -1f : 1f;
            cv.Piece(Ellipse(x, y, 3.2f, 1.6f), body, 1f, (px, py, c) => py > y + 0.3f ? Color.Lerp(c, Color.white, 0.25f) : c, 0.95f, 0.7f);
            cv.Piece(Poly(new Vector2(x - d * 2.6f, y), new Vector2(x - d * 5f, y - 1.8f), new Vector2(x - d * 5f, y + 1.8f)), body, 0.8f, null, 0.95f, 0.7f);
        }

        public static Color[,] Draw()
        {
            var cv = new VolumeCanvas(Width, Height);
            Vector2 head = new Vector2(216f, 46f);
            Vector2 neckBase = new Vector2(166f, 82f);
            Vector2 tailBase = new Vector2(64f, 88f);

            // ---------- Queue charnue, finie en nageoire ----------
            var tail = Curve(tailBase, new Vector2(42f, 98f), new Vector2(22f, 88f), 16);
            cv.Piece(Tube(tail, 6.5f, 2.5f), Skin, 6f, (x, y, c) => Scaly(x, y, c, 3.2f), 0.82f, 0.4f);
            Vector2 tip = tail[tail.Count - 1];
            var fin = new[] { tip + new Vector2(3, -3), tip + new Vector2(-10, -22), tip + new Vector2(-17, -24), tip + new Vector2(-9, 0), tip + new Vector2(-17, 18), tip + new Vector2(-10, 18), tip + new Vector2(3, 3) };
            cv.Piece((x, y) => Poly(fin)(x, y) && Noise(x / 3f + 5f, y / 3f) > 0.12f, Darker(Skin, 0.85f), 2.5f,
                (x, y, c) => (x * 2 + y) % 6 < 1 ? Darker(c, 0.65f) : Mottle(x, y, c, SkinDark), 0.85f, 0.45f);

            // ---------- Nageoire arrière (palette) ----------
            Vector2 rear = new Vector2(Sx - Srx * 0.6f, Sy + 4f);
            cv.Piece(Poly(rear + new Vector2(10, -4), rear + new Vector2(-6, -2), rear + new Vector2(-24, 12), rear + new Vector2(-28, 20), rear + new Vector2(-14, 18), rear + new Vector2(6, 6)),
                Darker(Skin, 0.9f), 3f, (x, y, c) => Scaly(x, y, c, 3.5f), 0.85f, 0.45f);

            // ---------- Le cou, long et ridé ----------
            var neck = Curve(neckBase, new Vector2(200f, 84f), head + new Vector2(-9f, 6f), 22);
            cv.Piece(Tube(neck, 9f, 6.5f), Skin, 7f, (x, y, c) =>
            {
                Color k = Scaly(x, y, c, 2.6f);
                return Mathf.Repeat(x * 0.35f - y * 0.55f, 4f) < 0.5f ? Darker(k, 0.78f) : k;   // plis de la peau
            }, 0.82f, 0.4f);
            // Gorge plus claire.
            cv.Piece((x, y) => Tube(neck, 9f, 6.5f)(x, y) && y > (neck[Mathf.Clamp(Mathf.RoundToInt((x - neckBase.x) / (head.x - neckBase.x) * 21f), 0, 21)].y + 4f),
                Color.Lerp(Skin, Plastron, 0.5f), 4f, (x, y, c) => Scaly(x, y, c, 2.4f), 0.85f, 0.45f);

            // ---------- La carapace ----------
            cv.Piece(InShell, Shell, 14f, Scutes, 0.82f, 0.4f);
            // Bord de la carapace et plastron.
            cv.Piece((x, y) => InShell(x, y) && y > Sy + 2f, Plastron, 3f, (x, y, c) => (x / 8) % 2 == 0 ? Darker(c, 0.82f) : Mottle(x, y, c, ShellDark), 0.85f, 0.45f);
            cv.Piece((x, y) => InShell(x, y) && y > Sy - 1f && y <= Sy + 2f, ShellDark, 1f, (x, y, c) => x % 9 == 0 ? Color.Lerp(c, Shell, 0.5f) : c);

            // ---------- Le récif qui l'a envahie ----------
            Crusts(cv, Sx - Srx, Sx + Srx, Sy - Sry - 2f, Sy + 2f, 2);
            BrainCoral(cv, Sx - 44f, ShellTop(Sx - 44f) + 4f, 9f);
            BrainCoral(cv, Sx + 30f, ShellTop(Sx + 30f) + 3f, 7f);
            BrainCoral(cv, Sx - 8f, Sy - 8f, 6f);
            BrainCoral(cv, Sx + 44f, Sy - 4f, 5f);
            SeaFan(cv, Sx - 48f, ShellTop(Sx - 48f) + 5f);
            BrainCoral(cv, Sx - 20f, Sy - 4f, 5f);
            BrainCoral(cv, Sx + 14f, Sy - 16f, 5f);
            BrainCoral(cv, Sx - 18f, ShellTop(Sx - 18f) + 4f, 6f);
            SeaFan(cv, Sx + 10f, ShellTop(Sx + 10f) + 3f);
            StaghornCluster(cv, Sx - 34f, ShellTop(Sx - 34f) + 4f, 14f, 1);
            StaghornCluster(cv, Sx + 22f, ShellTop(Sx + 22f) + 3f, 12f, 5);
            StaghornCluster(cv, Sx + 48f, ShellTop(Sx + 48f) + 4f, 9f, 9);
            TubeSponges(cv, Sx - 2f, ShellTop(Sx - 2f) + 4f);
            TubeSponges(cv, Sx - 56f, ShellTop(Sx - 56f) + 6f);
            Anemones(cv, Sx + 2f, ShellTop(Sx + 2f) + 3f, 7f);
            Anemones(cv, Sx - 26f, Sy - 6f, 5f);
            Anemones(cv, Sx + 36f, Sy - 10f, 5f);

            // ---------- La grande nageoire avant ----------
            Vector2 fl = new Vector2(Sx + Srx * 0.42f, Sy + 4f);
            var flipper = new[] { fl + new Vector2(14, -6), fl + new Vector2(8, 12), fl + new Vector2(-8, 30), fl + new Vector2(-30, 50), fl + new Vector2(-34, 46), fl + new Vector2(-22, 26), fl + new Vector2(-10, 8), fl + new Vector2(-4, -4) };
            cv.Piece(Poly(flipper), Skin, 4f, (x, y, c) => Scaly(x, y, c, 4f), 0.85f, 0.45f);
            // Bord avant épais et griffe.
            cv.Piece(Tube(Curve(fl + new Vector2(12, -4), fl + new Vector2(2, 22), fl + new Vector2(-30, 49), 14), 2f, 1f), Color.Lerp(Skin, ScaleEdge, 0.4f), 1.5f, null, 0.85f, 0.5f);
            cv.Piece(Poly(fl + new Vector2(-4, 14), fl + new Vector2(-1, 14), fl + new Vector2(-6, 19)), Bone(), 0.8f, null, 0.95f, 0.7f);
            // Le récif gagne aussi la nageoire.
            BrainCoral(cv, fl.x - 6f, fl.y + 16f, 4f);
            cv.Piece((x, y) => Poly(flipper)(x, y) && Noise(x / 4f + 30f, y / 4f) > 0.68f, Crust, 1f, null, 0.9f, 0.5f);

            // ---------- La tête ----------
            cv.Piece(Ellipse(head.x, head.y, 14f, 10f), Skin, 7f, (x, y, c) => Scaly(x, y, c, 3.8f), 0.82f, 0.4f);
            // Grandes plaques du dessus du crâne.
            cv.Piece((x, y) => Ellipse(head.x - 1f, head.y - 3f, 11f, 6f)(x, y) && y < head.y - 2f, Darker(Skin, 0.9f), 3f, (x, y, c) => Scaly(x, y, c, 5f), 0.85f, 0.45f);
            // Bec dentelé, crochu.
            var beak = new[] { head + new Vector2(10, -6), head + new Vector2(22, -3), head + new Vector2(24, 2), head + new Vector2(20, 6), head + new Vector2(12, 8), head + new Vector2(8, 2) };
            cv.Piece(Poly(beak), C(0.36f, 0.32f, 0.22f), 3f, (x, y, c) => Hash(x, y) < 0.15f ? Darker(c, 0.75f) : c, 0.85f, 0.5f);
            cv.Piece(Tube(new List<Vector2> { head + new Vector2(10, 2), head + new Vector2(18, 2), head + new Vector2(23, 1) }, 0.6f, 0.6f), Ink, 0.6f);
            for (int k = 0; k < 4; k++)
            {
                Vector2 t = head + new Vector2(12f + k * 2.6f, 2.5f);
                cv.Piece(Poly(t, t + new Vector2(1.2f, 0f), t + new Vector2(0.6f, 1.6f)), Darker(C(0.36f, 0.32f, 0.22f), 0.6f), 0.4f);
            }

            // Bernacles et un bout de corail sur le crâne.
            for (int k = 0; k < 4; k++)
            {
                Vector2 b = head + new Vector2(-12f + k * 4f + Hash(k, 3) * 2f, -9f + Hash(k, 5) * 3f);
                cv.Piece(Ellipse(b.x, b.y, 1.6f, 1.3f), C(0.56f, 0.56f, 0.48f), 1.2f, null, 0.9f, 0.45f);
                cv.Piece(Ellipse(b.x, b.y - 0.4f, 0.6f, 0.5f), SkinDark, 0.4f);
            }

            StaghornCluster(cv, head.x - 10f, head.y - 10f, 5f, 13);

            // L'œil : gros, sombre, sous une paupière lourde.
            Vector2 eye = head + new Vector2(2f, -2f);
            cv.Piece(Ellipse(eye.x, eye.y, 5f, 4.2f), SkinDark, 2.5f);
            cv.Piece(Ellipse(eye.x + 0.5f, eye.y + 0.5f, 3.4f, 3f), C(0.12f, 0.08f, 0.04f), 2f);
            cv.Piece(Ellipse(eye.x + 0.5f, eye.y + 0.5f, 2.2f, 2f), EyeColor, 1.5f, null, 0.95f, 0.9f);
            cv.Piece(Ellipse(eye.x + 0.7f, eye.y + 0.7f, 1f, 1.3f), Ink, 1f);
            cv.Piece(Tube(Curve(eye + new Vector2(-6f, -1f), eye + new Vector2(0f, -6f), eye + new Vector2(6f, -2f), 8), 1.8f, 1.2f), Darker(Skin, 0.75f), 1.5f, (x, y, c) => Scaly(x, y, c, 2f), 0.85f, 0.5f);

            // Cicatrices.
            cv.Piece(Capsule(neck[9].x - 3f, neck[9].y + 3f, neck[9].x + 4f, neck[9].y - 3f, 0.7f), C(0.70f, 0.66f, 0.52f), 0.6f, null, 0.95f, 0.7f);

            // Petits poissons du récif qui vivent dans ses coraux.
            ReefFish(cv, Sx - 30f, ShellTop(Sx - 30f) - 20f, C(0.96f, 0.52f, 0.30f), false);
            ReefFish(cv, Sx - 20f, ShellTop(Sx - 20f) - 26f, C(0.96f, 0.52f, 0.30f), false);
            ReefFish(cv, Sx + 36f, ShellTop(Sx + 36f) - 18f, C(0.40f, 0.70f, 0.96f), true);

            Color[,] p = cv.Finish(Ink, 0.55f);
            Halo(p, Mathf.RoundToInt(eye.x), Height - 1 - Mathf.RoundToInt(eye.y), 5, EyeColor, 2.5f);
            return p;
        }

        private static Color Bone() => C(0.80f, 0.74f, 0.58f);
    }
}
