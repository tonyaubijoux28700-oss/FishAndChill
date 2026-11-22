using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Légende de la Baie d'Izu : l'Isonade, rascasse titan (monstre des légendes de pêcheurs japonais, dont la nageoire
    /// de queue hérissée de crochets arrache les marins des barques). De profil, tête à droite, 256×144 : peau marbrée
    /// rouge sang et noire couverte de lambeaux de peau, grande tête osseuse aux crêtes épineuses, gueule ouverte en
    /// gouffre pleine de crocs en aiguille, œil d'ambre qui luit sous une corne de sourcil, épines venimeuses du dos tendues
    /// de voiles déchirées, immense pectorale en éventail, queue en éventail couverte de crochets d'os, une rame brisée
    /// et un lambeau de filet pris dedans, bernacles sur le crâne, et la corde sacrée (shimenawa) effilochée avec ses
    /// papiers en zigzag, restée autour du cou depuis qu'on a tenté de la lier. y depuis le haut.
    /// </summary>
    public static class IsonadeArt
    {
        public const int Width = 256;
        public const int Height = 144;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Flesh = C(0.34f, 0.10f, 0.09f);
        private static readonly Color FleshDark = C(0.08f, 0.03f, 0.04f);
        private static readonly Color Pale = C(0.58f, 0.42f, 0.32f);
        private static readonly Color Fin = C(0.22f, 0.07f, 0.07f);
        private static readonly Color Membrane = C(0.28f, 0.09f, 0.09f);
        private static readonly Color Bone = C(0.78f, 0.70f, 0.56f);
        private static readonly Color Maw = C(0.18f, 0.03f, 0.04f);
        private static readonly Color Glow = C(1f, 0.70f, 0.18f);
        private static readonly Color Straw = C(0.56f, 0.46f, 0.26f);
        private static readonly Color Ink = C(0.03f, 0.02f, 0.02f);

        private static Spine BuildSpine()
        {
            var spine = new Spine();
            const int n = 60;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                spine.Points.Add(new Vector2(58f + t * 142f, 84f - Mathf.Sin(t * Mathf.PI) * 8f + t * 2f));
                // Pédoncule fin, corps trapu, tête énorme.
                spine.Radius.Add(7f + 24f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.6f)) + 4f * Mathf.Clamp01((t - 0.6f) / 0.3f)
                    - 9f * Mathf.Clamp01((t - 0.9f) / 0.1f));
            }

            return spine;
        }

        /// <summary>Peau marbrée : taches pâles et noires, petits points, comme une rascasse posée sur le corail.</summary>
        private static Color Marble(int x, int y, Color c)
        {
            float n = Noise(x / 7f, y / 7f);
            if (n < 0.22f) return Color.Lerp(c, FleshDark, 0.7f);
            if (n > 0.78f) return Color.Lerp(c, Pale, 0.45f);
            float m = Noise(x / 2.5f + 11f, y / 2.5f);
            if (m < 0.18f) return Darker(c, 0.65f);
            return Hash(x, y) < 0.04f ? Color.Lerp(c, Pale, 0.5f) : c;
        }

        private static Color FinRays(int x, int y, Color c)
        {
            if ((x * 2 + y) % 7 < 1) return Darker(c, 0.6f);
            return Noise(x / 4f, y / 4f) < 0.3f ? Color.Lerp(c, FleshDark, 0.5f) : c;
        }

        public static Color[,] Draw()
        {
            var cv = new VolumeCanvas(Width, Height);
            Spine spine = BuildSpine();
            Vector2 tailBase = spine.Points[0];
            Vector2 head = spine.Points[spine.Points.Count - 1];
            var hooks = new List<(Vector2 at, float angle)>();

            // ---------- La queue en éventail, hérissée de crochets ----------
            var tail = new List<Vector2> { tailBase + new Vector2(4, -6) };
            for (int k = 0; k <= 12; k++)
            {
                float a = Mathf.Lerp(-1.05f, 1.05f, k / 12f);
                float r = 50f + (k % 2 == 0 ? 0f : -6f);   // bord festonné entre les rayons
                tail.Add(tailBase + new Vector2(-Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.95f));
            }

            tail.Add(tailBase + new Vector2(4, 6));
            cv.Piece((x, y) => Poly(tail.ToArray())(x, y) && !(Mathf.Abs(y - (tailBase.y + 24f) - (x - tailBase.x) * 0.2f) < 1.4f && x < tailBase.x - 30f),
                Fin, 3f, FinRays, 0.85f, 0.45f);   // une déchirure dans l'éventail
            for (int k = 0; k <= 12; k += 2)
            {
                float a = Mathf.Lerp(-1.05f, 1.05f, k / 12f);
                Vector2 dir = new Vector2(-Mathf.Cos(a), Mathf.Sin(a));
                cv.Piece(Tube(new List<Vector2> { tailBase, tailBase + dir * 50f }, 1.3f, 0.6f), C(0.24f, 0.07f, 0.06f), 1f, null, 0.9f, 0.5f);
                for (int h = 0; h < 4; h++)   // les crochets d'os le long de chaque rayon
                {
                    hooks.Add((tailBase + dir * (16f + h * 9f + (k % 4) * 1.5f), a));
                }
            }

            foreach ((Vector2 at, float angle) in hooks)
            {
                Vector2 dir = new Vector2(-Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 side = new Vector2(-dir.y, dir.x) * (Hash((int)at.x, (int)at.y) < 0.5f ? 1f : -1f);
                cv.Piece(Tube(Curve(at, at + side * 5f + dir * 1f, at + side * 4f + dir * 4f, 6), 1.1f, 0.35f), Bone, 1f,
                    (x, y, c) => Hash(x, y) < 0.2f ? Darker(c, 0.75f) : c, 0.9f, 0.55f);
            }

            // Une rame brisée prise dans les crochets.
            cv.Piece(Capsule(8, 104, 52, 118, 2.6f), C(0.46f, 0.32f, 0.18f), 2f, (x, y, c) => (x + y * 3) % 9 == 0 ? Darker(c, 0.7f) : c, 0.85f, 0.5f);
            cv.Piece(Poly(new Vector2(4, 100), new Vector2(16, 104), new Vector2(12, 116), new Vector2(0, 112)), C(0.40f, 0.27f, 0.15f), 2f,
                (x, y, c) => Hash(x, y) < 0.1f ? Darker(c, 0.7f) : c);

            // ---------- Épines venimeuses du dos, tendues de voiles déchirées ----------
            var spineTips = new List<Vector2>();
            var spineBases = new List<Vector2>();
            for (int k = 0; k < 9; k++)
            {
                float t = Mathf.Lerp(0.4f, 0.88f, k / 8f);
                int i = Mathf.RoundToInt(t * (spine.Points.Count - 1));
                Vector2 b = spine.Points[i] + new Vector2(0, -spine.Radius[i] + 3f);
                float tall = 50f * Mathf.Sin(Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(0.88f, 0.4f, t)) * Mathf.PI * 0.6f) + (k % 2) * 6f;
                spineBases.Add(b);
                spineTips.Add(b + new Vector2(-tall * 0.35f, -tall));
            }

            for (int k = 0; k < spineTips.Count - 1; k++)
            {
                // Voile entre deux épines : creusée en arc, trouée par endroits.
                Vector2 b0 = spineBases[k], b1 = spineBases[k + 1], t0 = spineTips[k], t1 = spineTips[k + 1];
                Vector2 dip = Vector2.Lerp(t0, t1, 0.5f) + new Vector2(0, 14f + (k % 3) * 4f);
                var sail = new List<Vector2> { b0, t0 + new Vector2(0, 6) };
                sail.AddRange(Curve(t0 + new Vector2(0, 6), dip, t1 + new Vector2(0, 6), 8));
                sail.Add(b1);
                int seed = k;
                cv.Piece((x, y) => Poly(sail.ToArray())(x, y) && Noise(x / 3f + seed * 7f, y / 3f) > 0.16f, Membrane, 2f,
                    (x, y, c) => Noise(x / 5f, y / 5f) < 0.35f ? Color.Lerp(c, FleshDark, 0.5f) : c, 0.85f, 0.45f);
            }

            foreach ((Vector2 b, Vector2 t) in Zip(spineBases, spineTips))
            {
                cv.Piece(Tube(Curve(b, Vector2.Lerp(b, t, 0.5f) + new Vector2(2, 0), t, 12), 2.2f, 0.5f), Bone, 2f,
                    (x, y, c) => Color.Lerp(c, C(0.50f, 0.20f, 0.16f), Mathf.InverseLerp(t.y + 12f, b.y, y) * 0.8f), 0.85f, 0.55f);
            }

            // Nageoire molle du dos (arrière) et anale à trois épines.
            cv.Piece(Poly(FinAlong(spine, 0.16f, 0.4f, -1f, k => 18f * Mathf.Sin(k * Mathf.PI), 16, 0.4f)), Fin, 2.5f, FinRays, 0.85f, 0.45f);
            cv.Piece(Poly(FinAlong(spine, 0.14f, 0.38f, 1f, k => 20f * Mathf.Sin(Mathf.Min(1f, k * 1.4f) * Mathf.PI * 0.6f) * (1.1f - k), 16, 0.5f)), Fin, 2.5f, FinRays, 0.85f, 0.45f);
            for (int k = 0; k < 3; k++)
            {
                int i = Mathf.RoundToInt((0.38f - k * 0.04f) * (spine.Points.Count - 1));
                Vector2 b = spine.Points[i] + new Vector2(0, spine.Radius[i] - 3f);
                cv.Piece(Tube(new List<Vector2> { b, b + new Vector2(-10f, 14f - k * 2f) }, 1.8f, 0.4f), Bone, 1.5f, null, 0.9f, 0.55f);
            }

            // ---------- Le corps marbré ----------
            cv.Piece(spine.Inside, Flesh, 12f, (x, y, c) =>
            {
                spine.Frame(x + 0.5f, y + 0.5f, out float t, out float s, out float r);
                Color shade = Color.Lerp(c, C(0.52f, 0.30f, 0.24f), Mathf.InverseLerp(0.2f, 0.95f, s) * 0.5f);   // ventre plus clair
                float u = t * 142f / 4f, v = (s + 1f) * r / 3.5f;
                float fu = Mathf.Repeat(u + (Mathf.FloorToInt(v) % 2) * 0.5f, 1f), fv = Mathf.Repeat(v, 1f);
                if (fu * fu * 2f + (fv - 0.5f) * (fv - 0.5f) < 0.07f) shade = Darker(shade, 0.75f);
                return Marble(x, y, shade);
            }, 0.82f, 0.4f);

            // Lambeaux de peau (cirres) qui pendent un peu partout.
            for (int k = 0; k < 14; k++)
            {
                float t = 0.2f + Hash(k, 3) * 0.7f;
                int i = Mathf.RoundToInt(t * (spine.Points.Count - 1));
                float s = Hash(k, 5) * 1.6f - 0.8f;
                Vector2 at = spine.Points[i] + new Vector2(0, s * spine.Radius[i]);
                cv.Piece(Poly(at + new Vector2(-2, 0), at + new Vector2(2, 0), at + new Vector2(-1 - Hash(k, 7) * 3f, 5 + Hash(k, 9) * 4f)), C(0.60f, 0.30f, 0.22f), 1f, Marble, 0.9f, 0.5f);
            }

            // ---------- La tête osseuse ----------
            cv.Piece(Ellipse(head.x - 12f, head.y - 2f, 26f, 26f), Flesh, 9f, Marble, 0.82f, 0.4f);
            // Crêtes osseuses : sourcil en corne au-dessus de l'œil, arête du crâne, épines de l'opercule.
            Vector2 eye = head + new Vector2(-6f, -15f);
            cv.Piece(Tube(Curve(eye + new Vector2(-14, -4), eye + new Vector2(-2, -14), eye + new Vector2(10, -20), 12), 3.2f, 0.6f), Bone, 2.5f,
                (x, y, c) => Color.Lerp(C(0.50f, 0.22f, 0.18f), c, Mathf.InverseLerp(eye.x - 14f, eye.x + 10f, x)), 0.85f, 0.55f);
            cv.Piece(Tube(Curve(head + new Vector2(-34, -20), head + new Vector2(-24, -30), head + new Vector2(-10, -28), 10), 2.4f, 1.2f), C(0.40f, 0.14f, 0.12f), 2f, Marble);
            for (int k = 0; k < 4; k++)
            {
                Vector2 b = head + new Vector2(-32f + k * 4f, 4f + k * 5f);
                cv.Piece(Tube(new List<Vector2> { b, b + new Vector2(-12f + k, 2f + k * 2f) }, 2f, 0.4f), Bone, 1.6f, null, 0.9f, 0.55f);
            }

            // Bord de l'opercule.
            cv.Piece(Tube(Curve(head + new Vector2(-30, -20), head + new Vector2(-24, 2), head + new Vector2(-30, 22), 12), 1.5f, 1.5f), FleshDark, 1.2f, null, 0.9f, 0.6f);

            // ---------- La gueule ouverte en gouffre ----------
            cv.Piece(Poly(head + new Vector2(-4, -6), head + new Vector2(30, -26), head + new Vector2(40, -18), head + new Vector2(42, 16), head + new Vector2(0, 14)), Maw, 2f,
                (x, y, c) => Noise(x / 3f, y / 3f) > 0.62f ? C(0.32f, 0.06f, 0.08f) : c);
            cv.Piece(Ellipse(head.x + 8, head.y + 2, 8f, 9f), C(0.04f, 0.01f, 0.01f), 3f);
            // Mâchoire du haut et mâchoire du bas, énorme, qui avance.
            cv.Piece(Poly(head + new Vector2(-8, -12), head + new Vector2(28, -30), head + new Vector2(36, -24), head + new Vector2(4, -4)), Flesh, 4f, Marble);
            cv.Piece(Poly(head + new Vector2(-6, 8), head + new Vector2(46, 12), head + new Vector2(50, 20), head + new Vector2(40, 26), head + new Vector2(-4, 24)), Flesh, 6f,
                (x, y, c) => Marble(x, y, y > head.y + 18 ? Color.Lerp(c, Pale, 0.3f) : c));
            for (int k = 0; k < 11; k++)   // crocs en aiguille, longs et serrés
            {
                Vector2 low = Vector2.Lerp(head + new Vector2(0, 10), head + new Vector2(46, 13), k / 10f);
                float len = 5f + Hash(k, 1) * 6f;
                cv.Piece(Poly(low + new Vector2(-1.3f, 0.5f), low + new Vector2(1.3f, 0.5f), low + new Vector2(-1.5f, -len)), Bone, 0.8f, null, 0.95f, 0.7f);
                Vector2 up = Vector2.Lerp(head + new Vector2(-2, -8), head + new Vector2(34, -26), k / 10f);
                cv.Piece(Poly(up + new Vector2(-1.3f, -0.5f), up + new Vector2(1.3f, -0.5f), up + new Vector2(1.6f, len * 0.8f)), Bone, 0.8f, null, 0.95f, 0.7f);
            }

            // ---------- L'œil d'ambre, sous la corne ----------
            cv.Piece(Ellipse(eye.x, eye.y, 8f, 7.5f), FleshDark, 4f);
            cv.Piece(Ellipse(eye.x, eye.y, 6f, 5.6f), C(0.80f, 0.52f, 0.14f), 4f, (x, y, c) => Hash(x, y) < 0.12f ? Darker(c, 0.75f) : c);
            cv.Piece(Box(eye.x - 0.9f, eye.y - 5f, eye.x + 0.9f, eye.y + 5f), C(0.04f, 0.01f, 0.01f), 1f);   // pupille fendue
            // Tentacules de sourcil.
            cv.Piece(Tube(Curve(eye + new Vector2(2, -9), eye + new Vector2(6, -16), eye + new Vector2(2, -22), 8), 1.4f, 0.4f), C(0.60f, 0.30f, 0.22f), 1f, Marble);

            // Bernacles sur le crâne.
            for (int k = 0; k < 6; k++)
            {
                Vector2 b = head + new Vector2(-28f + Hash(k, 11) * 18f, -22f + Hash(k, 13) * 8f);
                float r = 2.2f + Hash(k, 17) * 1.6f;
                cv.Piece(Ellipse(b.x, b.y, r, r * 0.9f), C(0.74f, 0.72f, 0.66f), r, (x, y, c) => Hash(x, y) < 0.25f ? Darker(c, 0.7f) : c, 0.9f, 0.5f);
                cv.Piece(Ellipse(b.x, b.y, r * 0.4f, r * 0.35f), C(0.12f, 0.10f, 0.10f), 0.6f);
            }

            // ---------- Immense pectorale en éventail, par-dessus le flanc ----------
            Vector2 pec = head + new Vector2(-40, 12);
            var fan = new List<Vector2> { pec };
            for (int k = 0; k <= 14; k++)
            {
                float a = Mathf.Lerp(0.15f, 1.55f, k / 14f);
                float r = 40f + (k % 2 == 0 ? 0f : -5f);
                fan.Add(pec + new Vector2(-Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.9f));
            }

            cv.Piece((x, y) => Poly(fan.ToArray())(x, y) && Noise(x / 3f + 40f, y / 3f) > 0.12f, Fin, 2.5f,
                (x, y, c) => (Mathf.FloorToInt(Vector2.Distance(new Vector2(x, y), pec) / 7f) % 2 == 0) ? Color.Lerp(FinRays(x, y, c), Pale, 0.18f) : FinRays(x, y, c), 0.85f, 0.45f);
            for (int k = 0; k <= 14; k += 2)
            {
                float a = Mathf.Lerp(0.15f, 1.55f, k / 14f);
                cv.Piece(Tube(new List<Vector2> { pec, pec + new Vector2(-Mathf.Cos(a), Mathf.Sin(a) * 0.9f) * 42f }, 1.2f, 0.5f), C(0.22f, 0.06f, 0.05f), 1f, null, 0.9f, 0.5f);
            }

            // ---------- La corde sacrée effilochée autour du cou ----------
            var rope = Curve(head + new Vector2(-36, -30), head + new Vector2(-44, 0), head + new Vector2(-34, 30), 16);
            cv.Piece(Tube(rope, 3f, 3f), Straw, 2f, (x, y, c) => (x + y * 2) % 4 == 0 ? Darker(c, 0.65f) : c, 0.85f, 0.5f);
            Vector2 knot = rope[rope.Count - 1];
            cv.Piece(Tube(Curve(knot, knot + new Vector2(-4, 10), knot + new Vector2(2, 20), 8), 2.2f, 0.8f), Straw, 1.5f, (x, y, c) => Hash(x, y) < 0.3f ? Darker(c, 0.7f) : c);
            cv.Piece(Tube(Curve(knot, knot + new Vector2(6, 8), knot + new Vector2(4, 16), 8), 1.8f, 0.6f), Straw, 1.5f, (x, y, c) => Hash(x, y) < 0.3f ? Darker(c, 0.7f) : c);
            foreach (int i in new[] { 4, 9 })   // papiers pliés en zigzag (shide), déchirés et salis
            {
                Vector2 a = rope[i];
                var zig = new[] { a, a + new Vector2(5, 0), a + new Vector2(2, 5), a + new Vector2(7, 5), a + new Vector2(4, 10), a + new Vector2(9, 10), a + new Vector2(5, 15), a + new Vector2(0, 15), a + new Vector2(3, 10), a + new Vector2(-2, 10), a + new Vector2(1, 5), a + new Vector2(-4, 5) };
                cv.Piece(Poly(zig), C(0.86f, 0.84f, 0.76f), 1f, (x, y, c) => Noise(x / 2f, y / 2f) < 0.3f ? C(0.60f, 0.56f, 0.46f) : c, 0.85f, 0.6f);
            }

            // Cicatrices.
            cv.Piece(Capsule(110, 70, 128, 84, 0.9f), C(0.80f, 0.66f, 0.56f), 0.8f, null, 0.95f, 0.7f);
            cv.Piece(Capsule(140, 92, 156, 84, 0.9f), C(0.80f, 0.66f, 0.56f), 0.8f, null, 0.95f, 0.7f);

            Color[,] p = cv.Finish(Ink, 0.55f);
            int Y(float y) => Height - 1 - Mathf.RoundToInt(y);
            Halo(p, Mathf.RoundToInt(eye.x), Y(eye.y), 9, Glow, 3f);

            // Le lambeau de filet accroché aux crochets de la queue.
            for (int gx = 0; gx < 6; gx++)
            {
                for (int i = 0; i < 26; i++)
                {
                    Mix(p, Mathf.RoundToInt(14 + gx * 5 + i * 0.35f), Y(40 + i + Mathf.Sin(i * 0.4f + gx) * 1.5f), C(0.70f, 0.70f, 0.62f), 0.4f);
                }
            }

            for (int gy = 0; gy < 5; gy++)
            {
                for (int i = 0; i < 30; i++)
                {
                    Mix(p, 12 + i, Y(44 + gy * 5 + i * 0.25f + Mathf.Sin(i * 0.5f) * 1f), C(0.70f, 0.70f, 0.62f), 0.35f);
                }
            }

            return p;
        }

        private static IEnumerable<(Vector2, Vector2)> Zip(List<Vector2> a, List<Vector2> b)
        {
            for (int i = 0; i < a.Count && i < b.Count; i++) yield return (a[i], b[i]);
        }
    }
}
