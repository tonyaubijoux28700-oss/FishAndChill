using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Proposition de légende pour le Fleuve Amazone : un payara titan (le « poisson vampire »), de profil, tête à droite,
    /// lancé à l'attaque, 256×144. Corps fuselé cambré en écailles d'acier terni, grande queue fourchue, pectorale en
    /// faux, gueule tournée vers le haut grande ouverte d'où se dressent les deux crocs géants de la mâchoire du bas, gros
    /// œil d'argent à la pupille rouge qui luit, isopode parasite collé sur la tête, et toutes les lignes cassées des
    /// pêcheurs qui pendent de sa mâchoire avec leurs hameçons et une vieille cuillère rouillée. y depuis le haut.
    /// </summary>
    public static class PayaraArt
    {
        public const int Width = 256;
        public const int Height = 144;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Steel = C(0.46f, 0.49f, 0.50f);
        private static readonly Color SteelDark = C(0.14f, 0.16f, 0.18f);
        private static readonly Color Back = C(0.15f, 0.19f, 0.20f);
        private static readonly Color Fin = C(0.20f, 0.22f, 0.24f);
        private static readonly Color Maw = C(0.22f, 0.04f, 0.06f);
        private static readonly Color Gum = C(0.50f, 0.20f, 0.20f);
        private static readonly Color Fang = C(0.90f, 0.88f, 0.78f);
        private static readonly Color Glow = C(1f, 0.20f, 0.16f);
        private static readonly Color Ink = C(0.03f, 0.03f, 0.04f);

        private static Spine BuildSpine()
        {
            var spine = new Spine();
            const int n = 60;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                spine.Points.Add(new Vector2(40f + t * 150f, 78f + Mathf.Sin(t * Mathf.PI * 1.1f) * 6f - t * t * 18f));
                spine.Radius.Add(5f + 22f * Mathf.Sin(Mathf.Clamp01(t * 1.35f) * Mathf.PI * 0.5f) - 4f * Mathf.Clamp01((t - 0.8f) / 0.2f));
            }

            return spine;
        }

        private static Color FinRays(int x, int y, Color c) => (x * 2 - y) % 6 < 1 ? Darker(c, 0.7f) : Hash(x / 3, y / 3) < 0.1f ? Color.Lerp(c, Steel, 0.4f) : c;

        public static Color[,] Draw()
        {
            var cv = new VolumeCanvas(Width, Height);
            Spine spine = BuildSpine();

            // ---------- Grande queue fourchue, déchirée ----------
            Color TailRays(int x, int y, Color c)
            {
                bool torn = (Mathf.Abs(y - 46f + (x - 14f) * 0.6f) < 1.6f && x < 20) || (Mathf.Abs(y - 116f - (x - 14f) * 0.6f) < 1.6f && x < 18);   // deux déchirures
                return torn ? new Color(0, 0, 0, 0) : FinRays(x, y, c);
            }

            cv.Piece(Poly(new Vector2(44, 74), new Vector2(44, 86), new Vector2(14, 128), new Vector2(6, 130), new Vector2(20, 100), new Vector2(24, 84),
                new Vector2(22, 72), new Vector2(4, 36), new Vector2(12, 34), new Vector2(26, 52), new Vector2(18, 54)), Fin, 3f, TailRays, 0.85f, 0.45f);

            // ---------- Nageoires du dos, anale, adipeuse ----------
            cv.Piece(Poly(FinAlong(spine, 0.3f, 0.44f, -1f, k => 26f * (1f - k) * Mathf.Sin(Mathf.Min(1f, k * 4f) * Mathf.PI * 0.5f), 16, 0.6f)), Fin, 2.5f, FinRays, 0.85f, 0.45f);
            cv.Piece(Poly(FinAlong(spine, 0.12f, 0.17f, -1f, k => 5f * Mathf.Sin(k * Mathf.PI), 8)), Fin, 2f, FinRays);
            cv.Piece(Poly(FinAlong(spine, 0.12f, 0.42f, 1f, k => 14f * (1.1f - k) * Mathf.Sin(Mathf.Min(1f, k * 3f) * Mathf.PI * 0.5f), 24, 0.4f)), Fin, 2.5f, FinRays, 0.85f, 0.45f);
            cv.Piece(Poly(FinAlong(spine, 0.5f, 0.6f, 1f, k => 14f * (1f - k), 10, 0.8f)), Fin, 2f, FinRays);   // pelvienne

            // ---------- Le corps : acier terni, dos sombre, ligne latérale ----------
            cv.Piece(spine.Inside, Steel, 11f, (x, y, c) =>
            {
                spine.Frame(x + 0.5f, y + 0.5f, out float t, out float s, out float r);
                float u = t * 150f / 4.5f, v = (s + 1f) * r / 3.5f;
                float fu = Mathf.Repeat(u + (Mathf.FloorToInt(v) % 2) * 0.5f, 1f), fv = Mathf.Repeat(v, 1f);
                Color shade = Color.Lerp(Back, c, Mathf.InverseLerp(-0.7f, 0.3f, s));
                if (fu * fu * 2f + (fv - 0.5f) * (fv - 0.5f) < 0.08f) shade = Darker(shade, 0.72f);       // bord des écailles
                if (Mathf.Abs(s + 0.05f) < 0.05f) shade = Darker(shade, 0.6f);                          // ligne latérale
                if (Noise(x / 8f, y / 8f) < 0.25f) shade = Color.Lerp(shade, C(0.30f, 0.26f, 0.18f), 0.4f);
                return Hash(x, y) < 0.03f ? Darker(shade, 0.6f) : shade;
            }, 0.82f, 0.4f);

            // Opercule, et une tache sombre derrière (comme le vrai payara).
            Vector2 head = spine.Points[spine.Points.Count - 1];
            cv.Piece(Tube(Curve(head + new Vector2(-14, -20), head + new Vector2(-8, 0), head + new Vector2(-14, 20), 10), 1.4f, 1.4f), SteelDark, 1.2f, null, 0.9f, 0.6f);
            cv.Piece(Ellipse(head.x - 26, head.y - 4, 5f, 7f), SteelDark, 2f, null, 0.95f, 0.6f);

            // ---------- La gueule tournée vers le haut, grande ouverte ----------
            // Mâchoire du haut, relevée ; fond de la gueule ; mâchoire du bas, énorme et proéminente.
            cv.Piece(Poly(head + new Vector2(-6, -18), head + new Vector2(30, -36), head + new Vector2(36, -30), head + new Vector2(6, -4)), Steel, 5f,
                (x, y, c) => Color.Lerp(Back, c, 0.5f));
            cv.Piece(Poly(head + new Vector2(4, -10), head + new Vector2(34, -30), head + new Vector2(56, -16), head + new Vector2(10, 12)), Maw, 1.6f,
                (x, y, c) => Noise(x / 3f, y / 3f) > 0.6f ? C(0.34f, 0.08f, 0.10f) : c);
            cv.Piece(Ellipse(head.x + 12, head.y - 4, 7f, 6f), C(0.04f, 0.01f, 0.01f), 3f);
            cv.Piece(Poly(head + new Vector2(-4, 6), head + new Vector2(50, -20), head + new Vector2(58, -14), head + new Vector2(50, -4), head + new Vector2(6, 22)), Steel, 6f,
                (x, y, c) => y > head.y + 2 ? Color.Lerp(c, C(0.66f, 0.68f, 0.66f), 0.3f) : c);
            cv.Piece(Tube(Curve(head + new Vector2(2, 6), head + new Vector2(26, -6), head + new Vector2(52, -18), 12), 1.6f, 1.2f), Gum, 1.2f);
            for (int k = 0; k < 10; k++)   // petites dents des deux mâchoires
            {
                Vector2 low = Vector2.Lerp(head + new Vector2(8, 3), head + new Vector2(50, -18), k / 9f);
                cv.Piece(Poly(low + new Vector2(-1.2f, 0.5f), low + new Vector2(1.2f, 0.5f), low + new Vector2(-1f, -3f - Hash(k, 1) * 2f)), Fang, 0.8f, null, 0.95f, 0.7f);
                Vector2 up = Vector2.Lerp(head + new Vector2(6, -8), head + new Vector2(32, -30), k / 9f);
                cv.Piece(Poly(up + new Vector2(-1.2f, -0.5f), up + new Vector2(1.2f, -0.5f), up + new Vector2(1.5f, 3f + Hash(k, 2) * 2f)), Fang, 0.8f, null, 0.95f, 0.7f);
            }

            // Les deux crocs géants dressés (celui d'en face plus sombre, un peu derrière).
            foreach ((Vector2 off, float shade) in new[] { (new Vector2(36, -12), 0.75f), (new Vector2(44, -15), 1f) })
            {
                Vector2 root = head + off;
                var fang = Curve(root, root + new Vector2(-6, -18), root + new Vector2(-2, -40), 18);
                Color fc = shade < 1f ? Darker(Fang, shade) : Fang;
                cv.Piece(Tube(fang, 4f, 0.6f), fc, 3f, (x, y, c) => y > root.y - 4 ? Color.Lerp(c, C(0.66f, 0.54f, 0.36f), 0.5f) : c, 0.85f, 0.6f);
            }

            // ---------- Gros œil d'argent, pupille rouge ----------
            Vector2 eye = head + new Vector2(-2, -12);
            cv.Piece(Ellipse(eye.x, eye.y, 8.5f, 8f), SteelDark, 4f);
            cv.Piece(Ellipse(eye.x, eye.y, 6.5f, 6f), C(0.72f, 0.72f, 0.64f), 4f, (x, y, c) => Hash(x, y) < 0.1f ? Darker(c, 0.8f) : c);
            cv.Piece(Ellipse(eye.x + 0.5f, eye.y, 3.4f, 4f), C(0.06f, 0.01f, 0.01f), 2f);
            cv.Piece(Ellipse(eye.x + 0.5f, eye.y, 1.9f, 2.4f), Glow, 1.2f);

            // Pectorale en faux, longue et déchirée, par-dessus le flanc.
            Vector2 pec = head + new Vector2(-16, 10);
            var pecEdge = Curve(pec, pec + new Vector2(-22, 2), pec + new Vector2(-60, 24), 14);
            var pecBack = Curve(pec + new Vector2(-60, 24), pec + new Vector2(-28, 24), pec + new Vector2(2, 9), 14);
            var pecShape = new List<Vector2>(pecEdge);
            pecShape.AddRange(pecBack);
            cv.Piece((x, y) => Poly(pecShape.ToArray())(x, y) && !(Mathf.Abs(x - (pec.x - 38f)) < 2.2f && y > pec.y + 14f), Fin, 2.5f, FinRays, 0.85f, 0.45f);   // une encoche déchirée
            cv.Piece(Tube(pecEdge, 1.8f, 0.6f), SteelDark, 1.4f, null, 0.9f, 0.5f);

            // Fente des ouïes, rouge sang sous l'opercule.
            cv.Piece(Tube(Curve(head + new Vector2(-19, -12), head + new Vector2(-14, 2), head + new Vector2(-19, 15), 10), 1.3f, 0.8f), C(0.62f, 0.12f, 0.14f), 1f, null, 0.95f, 0.7f);

            // Isopode parasite sur la tête.
            Vector2 iso = head + new Vector2(-16, -22);
            cv.Piece(Ellipse(iso.x, iso.y, 7f, 4.4f), C(0.70f, 0.62f, 0.50f), 3f, (x, y, c) => x % 3 == 0 ? Darker(c, 0.7f) : c);
            for (int k = 0; k < 4; k++) cv.Piece(Capsule(iso.x - 5 + k * 3.2f, iso.y + 3, iso.x - 6 + k * 3.2f, iso.y + 6, 0.7f), C(0.50f, 0.42f, 0.32f), 0.6f, null, 0.95f, 0.6f);

            // Cicatrices.
            cv.Piece(Capsule(110, 64, 126, 80, 0.9f), C(0.70f, 0.66f, 0.60f), 0.8f, null, 0.95f, 0.7f);
            cv.Piece(Capsule(140, 56, 150, 66, 0.9f), C(0.70f, 0.66f, 0.60f), 0.8f, null, 0.95f, 0.7f);
            cv.Piece(Capsule(80, 86, 94, 80, 0.9f), C(0.70f, 0.66f, 0.60f), 0.8f, null, 0.95f, 0.7f);

            // Hameçons plantés dans la mâchoire du bas, et une vieille cuillère rouillée.
            var hooks = new[] { head + new Vector2(8, 18), head + new Vector2(22, 10), head + new Vector2(36, 2), head + new Vector2(48, -6) };
            foreach (Vector2 h in hooks)
            {
                cv.Piece(Tube(Curve(h, h + new Vector2(3, 4), h + new Vector2(0, 6), 6), 0.9f, 0.6f), C(0.52f, 0.32f, 0.20f), 0.8f, null, 0.95f, 0.6f);
            }

            Vector2 spoon = head + new Vector2(30, 30);
            cv.Piece(Ellipse(spoon.x, spoon.y, 3.4f, 5.5f), C(0.62f, 0.44f, 0.26f), 3f, (x, y, c) => Noise(x, y) < 0.4f ? C(0.42f, 0.22f, 0.12f) : c);

            Color[,] p = cv.Finish(Ink, 0.55f);
            int Y(float y) => Height - 1 - Mathf.RoundToInt(y);
            Halo(p, Mathf.RoundToInt(eye.x), Y(eye.y), 8, Glow, 3f);

            // Les bouts de ligne cassés qui pendent.
            foreach (Vector2 h in hooks)
            {
                int len = 10 + Mathf.RoundToInt(Hash((int)h.x, 2) * 14f);
                for (int i = 0; i < len; i++) Mix(p, Mathf.RoundToInt(h.x - i * 0.25f + Mathf.Sin(i * 0.3f + h.x) * 1.5f), Y(h.y + 6 + i), C(0.86f, 0.88f, 0.90f), 0.45f);
            }

            for (int i = 0; i < 20; i++) Mix(p, Mathf.RoundToInt(spoon.x - 8 + i * 0.4f), Y(spoon.y - 18 + i * 0.9f), C(0.86f, 0.88f, 0.90f), 0.4f);
            return p;
        }
    }
}
