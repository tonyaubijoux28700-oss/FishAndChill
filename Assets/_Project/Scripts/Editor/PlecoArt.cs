using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Proposition de légende pour le Fleuve Amazone : un pléco titan (poisson-ventouse cuirassé), de profil, tête à
    /// droite, 256×144. Corps en rangées de plaques d'os à carènes épineuses, vermiculé d'ocre, ventre plat ; immense
    /// voile du dos déchirée, queue en croissant à longs filaments, pectorale à grosse épine couverte de soies, gerbe
    /// d'épines sur la joue, œil haut placé à l'iris en oméga qui luit, et sous le museau la ventouse ouverte aux peignes
    /// de dents rouillées, échardes de bois, sangsues. y depuis le haut pendant le dessin.
    /// </summary>
    public static class PlecoArt
    {
        public const int Width = 256;
        public const int Height = 144;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Plate = C(0.19f, 0.15f, 0.11f);
        private static readonly Color PlateDark = C(0.07f, 0.06f, 0.05f);
        private static readonly Color Ochre = C(0.62f, 0.48f, 0.22f);
        private static readonly Color Fin = C(0.14f, 0.12f, 0.09f);
        private static readonly Color Lip = C(0.42f, 0.36f, 0.29f);
        private static readonly Color Maw = C(0.12f, 0.04f, 0.03f);
        private static readonly Color Rust = C(0.62f, 0.34f, 0.16f);
        private static readonly Color Glow = C(1f, 0.62f, 0.24f);
        private static readonly Color Ink = C(0.03f, 0.03f, 0.02f);

        private static Spine BuildSpine()
        {
            var spine = new Spine();
            const int n = 60;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                spine.Points.Add(new Vector2(30f + t * 166f, 84f - t * 8f + Mathf.Sin(t * Mathf.PI) * 3f));
                spine.Radius.Add(5f + 21f * Mathf.SmoothStep(0f, 1f, t));
            }

            return spine;
        }

        private static Color Vermicule(int x, int y, Color c)
        {
            if (Mathf.Abs(Noise(x / 4f, y / 4f) - 0.5f) < 0.05f) return Color.Lerp(c, Ochre, 0.8f);
            return Hash(x, y) < 0.04f ? Color.Lerp(c, Ochre, 0.5f) : c;
        }

        private static Color Membrane(int x, int y, Color c)
        {
            if ((x * 3 - y * 2) % 11 == 0) return Color.Lerp(c, Ochre, 0.3f);                         // rayons
            return Hash(x / 2, y / 2) < 0.13f ? Color.Lerp(c, Ochre, 0.55f) : c;                      // taches
        }

        private static void Bristles(VolumeCanvas cv, System.Collections.Generic.List<Vector2> along, Vector2 lean, float len)
        {
            for (int i = 1; i < along.Count; i++)
            {
                Vector2 q = along[i];
                float l = len * (0.6f + Hash(i, (int)q.x) * 0.6f);
                cv.Piece(Poly(q + new Vector2(-1f, 0), q + new Vector2(1f, 0), q + lean * l), Ochre, 0.8f, (x, y, c) => Darker(c, 0.85f), 0.92f, 0.6f);
            }
        }

        public static Color[,] Draw()
        {
            var cv = new VolumeCanvas(Width, Height);
            Spine spine = BuildSpine();

            // ---------- Queue en croissant, longs filaments ----------
            cv.Piece(Poly(new Vector2(34, 80), new Vector2(34, 88), new Vector2(12, 112), new Vector2(4, 118), new Vector2(14, 98), new Vector2(18, 84),
                new Vector2(14, 70), new Vector2(2, 46), new Vector2(12, 54)), Fin, 3f, Membrane, 0.85f, 0.45f);
            cv.Piece(Tube(Curve(new Vector2(12, 54), new Vector2(4, 40), new Vector2(10, 28), 10), 1.2f, 0.4f), Fin, 1f);
            cv.Piece(Tube(Curve(new Vector2(6, 116), new Vector2(0, 126), new Vector2(8, 136), 10), 1.2f, 0.4f), Fin, 1f);

            // ---------- La voile du dos, haute et déchirée ----------
            cv.Piece(Poly(FinAlong(spine, 0.5f, 0.93f, -1f, k =>
            {
                float h = 6f + 38f * Mathf.Pow(Mathf.Sin(Mathf.Min(1f, k * 1.25f) * Mathf.PI * 0.5f), 0.7f) * (1f - k * 0.55f);
                return h - (Mathf.Abs(k - 0.35f) < 0.04f || Mathf.Abs(k - 0.62f) < 0.05f ? 14f : 0f);   // déchirures
            }, 40, 0.5f)), Fin, 3f, Membrane, 0.85f, 0.45f);
            for (int k = 0; k < 7; k++)   // rayons épais, la première épine énorme
            {
                float t = 0.92f - k * 0.06f;
                Vector2 at = spine.At(t, out Vector2 n);
                Vector2 root = at - n * spine.RadiusAt(t) * 0.8f;
                Vector2 tip = root + new Vector2(-14f - k * 3f, -36f + k * 3.5f);
                cv.Piece(Tube(Curve(root, root + new Vector2(-4, -18), tip, 8), k == 0 ? 2.8f : 1.5f, 0.5f), Plate, 1.4f, Vermicule, 0.9f, 0.5f);
            }

            // Petite adipeuse et anale.
            cv.Piece(Poly(FinAlong(spine, 0.18f, 0.28f, -1f, k => 6f * Mathf.Sin(k * Mathf.PI), 10)), Fin, 2f, Membrane);
            cv.Piece(Poly(FinAlong(spine, 0.2f, 0.34f, 1f, k => 10f * Mathf.Sin(k * Mathf.PI) * (1.2f - k), 14, 0.4f)), Fin, 2f, Membrane);

            // ---------- Le corps en rangées de plaques à carènes ----------
            cv.Piece(spine.Inside, Plate, 11f, (x, y, c) =>
            {
                spine.Frame(x + 0.5f, y + 0.5f, out float t, out float s, out float r);
                float u = t * 166f / 8f, v = (s + 1f) * 2.5f;   // 5 rangées de plaques
                float fu = Mathf.Repeat(u + (Mathf.FloorToInt(v) % 2) * 0.5f, 1f), fv = Mathf.Repeat(v, 1f);
                if (fu < 0.1f || fv < 0.08f) return PlateDark;                                       // joints
                if (Mathf.Abs(fv - 0.5f) < 0.07f && fu > 0.3f) return Color.Lerp(c, Ochre, 0.4f);     // carène
                Color plate = Vermicule(x, y, c);
                return s > 0.6f ? Color.Lerp(plate, Lip, 0.3f) : plate;                              // ventre plat plus clair
            }, 0.82f, 0.4f);
            for (int k = 0; k < 14; k++)   // épines des carènes
            {
                float t = 0.12f + k * 0.06f;
                for (int row = 0; row < 3; row++)
                {
                    float s = -0.6f + row * 0.6f;
                    Vector2 at = spine.At(t, out Vector2 n) + n * spine.RadiusAt(t) * s;
                    cv.Piece(Poly(at + new Vector2(-1.4f, 0.8f), at + new Vector2(1.4f, 0.8f), at + new Vector2(-3.5f, -1.5f)), Ochre, 0.8f, (x, y, c) => Darker(c, 0.8f), 0.92f, 0.6f);
                }
            }

            // ---------- La tête plate, cuirassée ----------
            cv.Piece(Poly(new Vector2(178, 50), new Vector2(206, 56), new Vector2(236, 76), new Vector2(246, 92), new Vector2(240, 100), new Vector2(178, 102)),
                Plate, 12f, Vermicule, 0.8f, 0.4f);
            cv.Piece(Tube(Curve(new Vector2(182, 52), new Vector2(212, 58), new Vector2(240, 84), 10), 2.4f, 1.2f), Plate, 2f, Vermicule);   // arête du crâne

            // Ventouse ouverte sous le museau, un peu tournée vers nous, peignes de dents rouillées.
            const float DX = 222f, DY = 106f;
            cv.Piece(Ellipse(DX, DY, 20f, 9f), Lip, 5f, (x, y, c) => Hash(x / 2, y / 2) < 0.24f ? Color.Lerp(c, C(0.66f, 0.58f, 0.48f), 0.6f) : c);
            cv.Piece(Ellipse(DX, DY - 0.5f, 13f, 5f), Maw, 2f);
            cv.Piece(Ellipse(DX, DY, 5f, 2.4f), C(0.03f, 0.01f, 0.01f), 2f);
            for (int k = 0; k < 12; k++)
            {
                float a = Mathf.Lerp(Mathf.PI + 0.3f, Mathf.PI * 2f - 0.3f, k / 11f);
                Vector2 root = new Vector2(DX + Mathf.Cos(a) * 12f, DY + Mathf.Sin(a) * 4.4f);
                cv.Piece(Capsule(root.x, root.y, root.x, root.y + 3f, 0.9f), k % 3 == 0 ? Darker(Rust, 0.8f) : Rust, 0.8f, null, 0.95f, 0.6f);
                Vector2 low = new Vector2(DX + Mathf.Cos(a) * 11f, DY - Mathf.Sin(a) * 4f);
                cv.Piece(Capsule(low.x, low.y, low.x, low.y - 2.6f, 0.9f), Rust, 0.8f, null, 0.95f, 0.6f);
            }

            cv.Piece(Capsule(DX - 8, DY - 3, DX + 3, DY + 2, 1.3f), C(0.52f, 0.40f, 0.24f), 1.1f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.75f) : c);   // écharde
            foreach (float side in new[] { -1f, 1f })   // barbillons aux coins de la ventouse
            {
                cv.Piece(Tube(Curve(new Vector2(DX + side * 18, DY + 2), new Vector2(DX + side * 24, DY + 12), new Vector2(DX + side * 18, DY + 20), 10), 1.8f, 0.6f), Lip, 1.4f,
                    (x, y, c) => (x + y) % 4 == 0 ? Darker(c, 0.7f) : c);
            }

            // Gerbe d'épines sur la joue, hérissée vers l'arrière.
            var cheek = new Vector2(200, 88);
            for (int k = 0; k < 11; k++)
            {
                float a = Mathf.Lerp(2.4f, 3.6f, k / 10f);
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float len = 14f + Hash(k, 5) * 10f;
                cv.Piece(Poly(cheek - new Vector2(-dir.y, dir.x) * 1.2f, cheek + new Vector2(-dir.y, dir.x) * 1.2f, cheek + dir * len), Ochre, 0.9f, (x, y, c) => Darker(c, 0.85f), 0.9f, 0.55f);
            }

            // ---------- Pectorale et pelvienne, l'épine couverte de soies ----------
            var pecRoot = new Vector2(188, 98);
            cv.Piece(Poly(pecRoot, new Vector2(196, 104), new Vector2(150, 132), new Vector2(128, 132), new Vector2(140, 120)), Fin, 2.5f, Membrane, 0.85f, 0.45f);
            var pecSpine = Curve(pecRoot, new Vector2(170, 104), new Vector2(124, 134), 14);
            cv.Piece(Tube(pecSpine, 4f, 1.6f), Plate, 3f, Vermicule, 0.85f, 0.45f);
            Bristles(cv, pecSpine, new Vector2(0.5f, 1f).normalized, 4f);
            var pelRoot = new Vector2(134, 98);
            cv.Piece(Poly(pelRoot, new Vector2(140, 102), new Vector2(110, 124), new Vector2(96, 122)), Fin, 2f, Membrane, 0.85f, 0.45f);
            cv.Piece(Tube(Curve(pelRoot, new Vector2(118, 106), new Vector2(96, 122), 8), 2.4f, 1f), Plate, 2f, Vermicule);

            // ---------- L'œil haut placé, iris en oméga ----------
            var eye = new Vector2(214, 68);
            cv.Piece(Ellipse(eye.x, eye.y, 8f, 7f), Plate, 5f, Vermicule);
            cv.Piece(Ellipse(eye.x, eye.y, 5.4f, 4.6f), C(0.03f, 0.03f, 0.02f), 2f);
            cv.Piece((x, y) =>
            {
                float u = (x - eye.x) / 4.6f, v = (y - eye.y) / 3.8f;
                float d = u * u + v * v;
                return d <= 1f && d > 0.25f && !(Mathf.Abs(u) < 0.3f && v < -0.1f);   // anneau ouvert en haut : l'oméga
            }, Glow, 1f);

            // Sangsues et cicatrices.
            foreach ((float x, float y, float a) in new[] { (150f, 70f, 0.5f), (96f, 86f, -0.4f), (190f, 76f, 0.2f) })
            {
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 3.5f;
                cv.Piece(Capsule(x - d.x, y - d.y, x + d.x, y + d.y, 1.8f), C(0.08f, 0.06f, 0.05f), 1.6f, (px, py, c) => (px + py) % 3 == 0 ? C(0.26f, 0.16f, 0.10f) : c);
            }

            cv.Piece(Capsule(120, 64, 136, 78, 0.9f), C(0.56f, 0.48f, 0.40f), 0.8f, null, 0.95f, 0.7f);
            cv.Piece(Capsule(160, 84, 172, 74, 0.9f), C(0.56f, 0.48f, 0.40f), 0.8f, null, 0.95f, 0.7f);

            Color[,] p = cv.Finish(Ink, 0.55f);
            int Y(float y) => Height - 1 - Mathf.RoundToInt(y);
            Halo(p, Mathf.RoundToInt(eye.x), Y(eye.y), 8, Glow, 3.5f);

            // Filaments d'algues accrochés aux épines du dos.
            for (int k = 0; k < 5; k++)
            {
                Vector2 at = spine.At(0.3f + k * 0.13f, out Vector2 n);
                Vector2 q = at - n * spine.RadiusAt(0.3f + k * 0.13f) * 0.9f;
                for (int i = 0; i < 10; i++) Mix(p, Mathf.RoundToInt(q.x - i * 1.2f), Y(q.y + Mathf.Sin(i * 0.7f + k) * 1.5f), C(0.30f, 0.42f, 0.20f), 0.5f);
            }

            return p;
        }
    }
}
