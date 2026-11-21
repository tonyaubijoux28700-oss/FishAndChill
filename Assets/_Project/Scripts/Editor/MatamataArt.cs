using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// « Jabutiaçu », la légende du Fleuve Amazone : un matamata titan (tortue à tête plate), 256×144, style en volume.
    /// Carapace à trois carènes de bosses, écailles en plaques à cernes de croissance, couverte de vase, de mousse, de
    /// fougères et d'un nénuphar Victoria, racines qui pendent du bord dentelé, vieux harpon planté dedans ; long cou
    /// ridé à franges de peau, tête plate en triangle avec son museau en tuba, gueule immense grande ouverte qui aspire,
    /// tout petits yeux qui luisent, sangsues. Deux cadrages : <see cref="DrawSide"/> (de trois quarts, tête à droite) et
    /// <see cref="DrawTop"/> (vue de dessus). y depuis le haut pendant le dessin.
    /// </summary>
    public static class MatamataArt
    {
        public const int Width = 256;
        public const int Height = 144;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Shell = C(0.27f, 0.20f, 0.13f);
        private static readonly Color ShellDark = C(0.10f, 0.07f, 0.05f);
        private static readonly Color ShellLight = C(0.46f, 0.37f, 0.24f);
        private static readonly Color Skin = C(0.30f, 0.25f, 0.16f);
        private static readonly Color SkinDark = C(0.14f, 0.11f, 0.07f);
        private static readonly Color Belly = C(0.42f, 0.36f, 0.22f);
        private static readonly Color Moss = C(0.20f, 0.29f, 0.12f);
        private static readonly Color Mud = C(0.22f, 0.18f, 0.12f);
        private static readonly Color Fern = C(0.18f, 0.32f, 0.13f);
        private static readonly Color Maw = C(0.20f, 0.07f, 0.06f);
        private static readonly Color Throat = C(0.52f, 0.30f, 0.26f);
        private static readonly Color Bone = C(0.74f, 0.68f, 0.52f);
        private static readonly Color Leech = C(0.08f, 0.06f, 0.05f);
        private static readonly Color Glow = C(0.92f, 0.86f, 0.38f);
        private static readonly Color Ink = C(0.04f, 0.03f, 0.02f);

        // ---------- Matières ----------

        /// <summary>Plaques d'écaille : joints sombres, cernes de croissance, mousse sur le haut, vase dans les creux.</summary>
        private static Color Scutes(int x, int y, Color c, float cell, float mossLine)
        {
            float u = x / cell, v = y / (cell * 0.8f);
            int cu = Mathf.FloorToInt(u), cv = Mathf.FloorToInt(v);
            float d1 = 9f, d2 = 9f;
            for (int a = -1; a <= 1; a++)
                for (int b = -1; b <= 1; b++)
                {
                    float jx = cu + a + 0.15f + Hash(cu + a, cv + b + 300) * 0.7f;
                    float jy = cv + b + 0.15f + Hash(cv + b + 41, cu + a + 300) * 0.7f;
                    float d = Mathf.Sqrt((u - jx) * (u - jx) + (v - jy) * (v - jy));
                    if (d < d1) { d2 = d1; d1 = d; }
                    else if (d < d2) d2 = d;
                }

            if (d2 - d1 < 0.07f) return ShellDark;                                                   // joints
            Color plate = Color.Lerp(c, ShellLight, Mathf.Clamp01(0.5f - d1) * 0.6f);                 // bombé au centre
            if (Mathf.Repeat(d1 * 4.2f, 1f) < 0.16f) plate = Darker(plate, 0.78f);                   // cernes
            float n = Noise(x / 7f, y / 7f);
            if (y < mossLine && n > 0.56f) return Color.Lerp(plate, Moss, Hash(x, y) < 0.3f ? 0.95f : 0.75f);
            if (n < 0.2f) return Color.Lerp(plate, Mud, 0.7f);
            return Hash(x, y) < 0.04f ? Darker(plate, 0.6f) : plate;
        }

        /// <summary>Peau ridée et bosselée : tubercules clairs, plis sombres, marbrure.</summary>
        private static Color Hide(int x, int y, Color c)
        {
            float n = Noise(x / 4f, y / 4f);
            float h = Hash(x / 2, y / 2);
            if (h < 0.07f) return Color.Lerp(c, ShellLight, 0.55f);                                 // tubercules
            if (Mathf.Abs(Noise(x / 6f + 9f, y / 3f) - 0.5f) < 0.035f) return SkinDark;               // plis
            if (n < 0.3f) return Color.Lerp(c, Mud, 0.5f);
            return n > 0.72f ? Color.Lerp(c, Belly, 0.3f) : c;
        }

        // ---------- Pièces communes ----------

        private static void Foot(VolumeCanvas cv, Vector2 hip, Vector2 foot, float r, Color c, Vector2 clawDir)
        {
            cv.Piece(Capsule(hip.x, hip.y, foot.x, foot.y, r), c, r * 0.8f, Hide);
            cv.Piece(Ellipse(foot.x, foot.y, r * 1.25f, r * 0.85f), c, r * 0.6f, Hide);
            Vector2 side = new Vector2(-clawDir.y, clawDir.x);
            for (int k = 0; k < 4; k++)
            {
                Vector2 root = foot + clawDir * r * 0.7f + side * (k - 1.5f) * r * 0.55f;
                Vector2 tip = root + clawDir * (r * 0.9f + Hash(k, (int)hip.x) * 2f) + side * (k - 1.5f) * 1.2f;
                cv.Piece(Poly(root - side * 1.3f, root + side * 1.3f, tip), Bone, 1f, (x, y, cc) => Darker(cc, 0.85f), 0.9f, 0.55f);
            }

            // Écailles larges sur le dessus de la patte.
            for (int k = 0; k < 3; k++)
            {
                Vector2 s = Vector2.Lerp(hip, foot, 0.3f + k * 0.2f);
                cv.Piece(Ellipse(s.x, s.y, r * 0.55f, r * 0.4f), Color.Lerp(c, ShellLight, 0.2f), 1.5f, Hide, 0.85f, 0.5f);
            }
        }

        private static void FernFrond(VolumeCanvas cv, Vector2 root, Vector2 control, Vector2 tip, float size)
        {
            List<Vector2> stem = Curve(root, control, tip, 16);
            for (int i = 1; i < stem.Count - 1; i++)
            {
                Vector2 dir = (stem[i + 1] - stem[i - 1]).normalized;
                Vector2 n = new Vector2(-dir.y, dir.x);
                float len = size * (1f - i / (float)stem.Count) + 1.5f;
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector2 end = stem[i] + n * side * len + dir * len * 0.5f;
                    cv.Piece(Capsule(stem[i].x, stem[i].y, end.x, end.y, 1.1f), i % 3 == 0 ? Darker(Fern, 0.8f) : Fern, 0.8f, null, 0.92f, 0.55f);
                }
            }

            cv.Piece(Tube(stem, 1f, 0.5f), Darker(Fern, 0.6f), 0.6f, null, 0.95f, 0.6f);
        }

        private static void HangingRoot(VolumeCanvas cv, Vector2 from, float length, float sway, int seed)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                pts.Add(from + new Vector2(Mathf.Sin(t * 3.4f + seed) * sway * t, t * length));
            }

            cv.Piece(Tube(pts, 1.3f, 0.4f), C(0.20f, 0.15f, 0.10f), 0.8f, null, 0.92f, 0.5f);
            if (Hash(seed, 9) < 0.6f)
            {
                Vector2 branch = pts[5];
                cv.Piece(Tube(Curve(branch, branch + new Vector2(sway * 0.6f, 4), branch + new Vector2(sway, 9), 5), 0.8f, 0.3f), C(0.20f, 0.15f, 0.10f), 0.6f, null, 0.95f, 0.5f);
            }
        }

        private static void LeechAt(VolumeCanvas cv, Vector2 at, float angle)
        {
            Vector2 d = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3.5f;
            cv.Piece(Capsule(at.x - d.x, at.y - d.y, at.x + d.x, at.y + d.y, 1.7f), Leech, 1.6f, (x, y, c) => (x + y) % 3 == 0 ? C(0.24f, 0.16f, 0.10f) : c, 0.85f, 0.5f);
        }

        private static void Knob(VolumeCanvas cv, Vector2 at, float r, Vector2 up)
        {
            cv.Piece(Ellipse(at.x, at.y, r, r * 0.9f), Shell, r, (x, y, c) => Scutes(x, y, c, 5f, 200f), 0.8f, 0.45f);
            cv.Piece(Poly(at + new Vector2(-up.y, up.x) * r * 0.5f, at - new Vector2(-up.y, up.x) * r * 0.5f, at + up * r * 1.6f), ShellLight, 1.2f, Hide, 0.9f, 0.5f);
        }

        // =====================================================================================================
        // De trois quarts, tête à droite : la carapace comme un tertre de vase, le cou en S, la gueule qui aspire.
        // =====================================================================================================
        public static Color[,] DrawSide()
        {
            var cv = new VolumeCanvas(Width, Height);
            const float CX = 92f, CY = 94f, RX = 72f, RY = 68f;
            float Top(float x) => CY - RY * Mathf.Sqrt(Mathf.Max(0f, 1f - (x - CX) * (x - CX) / (RX * RX)));

            // ---------- Pattes de l'autre côté, dans l'ombre ----------
            Color far = Darker(Skin, 0.7f);
            Foot(cv, new Vector2(66, 96), new Vector2(74, 126), 6.5f, far, new Vector2(0.3f, 1f).normalized);
            Foot(cv, new Vector2(136, 94), new Vector2(150, 124), 7f, far, new Vector2(0.6f, 1f).normalized);

            // ---------- Le cou, en S, qui sort de sous la carapace ----------
            var neck = new Spine();
            List<Vector2> neckLine = Curve(new Vector2(146, 80), new Vector2(182, 104), new Vector2(206, 76), 24);
            for (int i = 0; i < neckLine.Count; i++)
            {
                neck.Points.Add(neckLine[i]);
                neck.Radius.Add(Mathf.Lerp(18f, 12.5f, i / (float)(neckLine.Count - 1)));
            }

            // Franges de peau qui pendent sous le cou, tubercules en crête sur le dessus.
            cv.Piece(Poly(FinAlong(neck, 0.25f, 0.98f, 1f, k => 2.5f + 4.5f * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 6f)) * (1.1f - k * 0.4f), 40)),
                SkinDark, 2f, (x, y, c) => (x * 3 + y) % 7 == 0 ? Darker(c, 0.7f) : Hide(x, y, c), 0.85f, 0.45f);
            cv.Piece(neck.Inside, Skin, 9f, (x, y, c) =>
            {
                neck.Frame(x + 0.5f, y + 0.5f, out float t, out float s, out _);
                Color hide = Hide(x, y, c);
                if (Mathf.Repeat(t * 11f, 1f) < 0.12f && s > -0.7f) hide = Darker(hide, 0.72f);         // plis en anneaux
                return s > 0.45f ? Color.Lerp(hide, Belly, 0.45f) : hide;
            }, 0.82f, 0.42f);
            for (int k = 0; k < 9; k++)
            {
                float t = 0.15f + k * 0.095f;
                Vector2 at = neck.At(t, out Vector2 n);
                Knob(cv, at - n * neck.RadiusAt(t) * 0.85f, 2.4f, -n);
            }

            // ---------- La tête plate, gueule grande ouverte ----------
            // Fond de la gueule (la gorge qui aspire), puis mâchoire du bas tombée.
            cv.Piece(Poly(new Vector2(198, 72), new Vector2(240, 70), new Vector2(240, 94), new Vector2(200, 90)), Maw, 1.6f,
                (x, y, c) => Noise(x / 3f, y / 3f) > 0.62f ? C(0.30f, 0.12f, 0.10f) : c);
            cv.Piece(Ellipse(212, 82, 9f, 6f), C(0.05f, 0.02f, 0.02f), 4f);                                       // le gouffre
            cv.Piece(Capsule(204, 86, 228, 90, 3.6f), Throat, 3f, (x, y, c) => (x + y) % 4 == 0 ? Darker(c, 0.8f) : c);   // langue
            cv.Piece(Poly(new Vector2(196, 84), new Vector2(236, 92), new Vector2(242, 98), new Vector2(236, 104), new Vector2(200, 100)), Skin, 5f,
                (x, y, c) => y > 99 ? Color.Lerp(Hide(x, y, c), Belly, 0.5f) : Hide(x, y, c));
            // Bec corné dentelé de la mâchoire du bas.
            for (int k = 0; k < 9; k++)
            {
                Vector2 root = Vector2.Lerp(new Vector2(204, 88), new Vector2(238, 94), k / 8f);
                float len = 2.5f + Hash(k, 17) * 3f;
                cv.Piece(Poly(root + new Vector2(-1.4f, 0.5f), root + new Vector2(1.4f, 0.5f), root + new Vector2(0.6f, -len)), Bone, 0.9f, null, 0.95f, 0.7f);
            }

            // Lambeaux de peau qui pendent sous la mâchoire.
            for (int k = 0; k < 6; k++)
            {
                float x = 200f + k * 6.5f;
                float y = 100f + k * 0.6f;
                float len = 5f + Hash(k, 23) * 5f;
                cv.Piece(Poly(new Vector2(x - 2.5f, y - 1f), new Vector2(x + 2.5f, y - 1f), new Vector2(x - 1f + Hash(k, 5) * 2f, y + len)), SkinDark, 1.5f, Hide, 0.85f, 0.5f);
            }

            // Mâchoire du haut et crâne : un triangle aplati, très large.
            cv.Piece(Union(Ellipse(204, 70, 15f, 15f), Poly(new Vector2(194, 56), new Vector2(226, 58), new Vector2(244, 64), new Vector2(244, 72), new Vector2(200, 78))),
                Skin, 9f, (x, y, c) => Hide(x, y, c));
            for (int k = 0; k < 10; k++)
            {
                Vector2 root = Vector2.Lerp(new Vector2(204, 75), new Vector2(242, 72), k / 9f);
                float len = 2f + Hash(k, 29) * 3.5f;
                cv.Piece(Poly(root + new Vector2(-1.3f, -0.5f), root + new Vector2(1.3f, -0.5f), root + new Vector2(-0.6f, len)), Bone, 0.9f, null, 0.95f, 0.7f);
            }

            // Le museau en tuba.
            cv.Piece(Capsule(236, 63, 252, 55, 2.8f), Skin, 2.4f, Hide);
            cv.Piece(Ellipse(252, 55, 1.6f, 1.6f), Ink, 1f);

            // Franges et « oreilles » de peau sur les côtés du crâne.
            foreach ((Vector2 a, Vector2 b) in new[] { (new Vector2(196, 60), new Vector2(184, 50)), (new Vector2(198, 66), new Vector2(184, 62)), (new Vector2(200, 74), new Vector2(188, 78)),
                         (new Vector2(214, 58), new Vector2(208, 46)), (new Vector2(222, 58), new Vector2(220, 47)) })
            {
                Vector2 d = (b - a).normalized;
                Vector2 n = new Vector2(-d.y, d.x);
                cv.Piece(Poly(a + n * 3f, a - n * 3f, b + n * 1.5f, b), SkinDark, 1.6f, Hide, 0.85f, 0.5f);
            }

            // Tubercules et arcade au-dessus du tout petit œil.
            cv.Piece(Capsule(216, 60, 232, 62, 2.4f), SkinDark, 2f, Hide);
            foreach (Vector2 b in new[] { new Vector2(206, 60), new Vector2(212, 66), new Vector2(232, 66), new Vector2(198, 70), new Vector2(222, 68) })
            {
                cv.Piece(Ellipse(b.x, b.y, 1.8f, 1.6f), ShellLight, 1.5f, null, 0.9f, 0.6f);
            }

            Vector2 eye = new Vector2(226, 64);
            cv.Piece(Ellipse(eye.x, eye.y, 2.6f, 2.2f), C(0.02f, 0.02f, 0.01f), 1.5f);
            cv.Piece(Ellipse(eye.x, eye.y, 1.6f, 1.3f), Glow, 1f);

            // ---------- Pattes de ce côté ----------
            Foot(cv, new Vector2(40, 92), new Vector2(26, 126), 9f, Skin, new Vector2(-0.4f, 1f).normalized);
            Foot(cv, new Vector2(150, 92), new Vector2(166, 126), 9.5f, Skin, new Vector2(0.5f, 1f).normalized);
            for (int k = 0; k < 5; k++)   // franges à l'arrière de la patte avant
            {
                Vector2 a = Vector2.Lerp(new Vector2(146, 100), new Vector2(158, 120), k / 4f);
                cv.Piece(Poly(a + new Vector2(0, -2), a + new Vector2(0, 2), a + new Vector2(-6f - Hash(k, 3) * 3f, 1f)), SkinDark, 1.2f, Hide, 0.88f, 0.5f);
            }

            // ---------- La carapace : plastron, bord dentelé, dôme ----------
            cv.Piece(Ellipse(94, 98, 64f, 8f), Belly, 4f, (x, y, c) => Mathf.Repeat(x / 13f, 1f) < 0.1f ? Darker(c, 0.7f) : Hide(x, y, c));
            for (float x = 20f; x < 166f; x += 9f)
            {
                float y = 92f + Hash((int)x, 3) * 2f;
                cv.Piece(Poly(new Vector2(x - 1f, y - 6f), new Vector2(x + 10f, y - 6f), new Vector2(x + 3f + Hash((int)x, 7) * 3f, y + 6f + Hash((int)x, 11) * 4f)),
                    Shell, 3f, (px, py, c) => Scutes(px, py, c, 8f, 0f), 0.8f, 0.45f);
            }

            cv.Piece((x, y) => y <= 94f && Ellipse(CX, CY, RX, RY)(x, y), Shell, 16f, (x, y, c) =>
                y > 74f ? Darker(Scutes(x, y, c, 13f, 62f), Mathf.Lerp(1f, 0.72f, (y - 74f) / 20f)) : Scutes(x, y, c, 13f, 62f), 0.8f, 0.42f);

            // Trois carènes de bosses (celle du milieu sur la crête, deux sur les flancs).
            for (float x = 36f; x <= 150f; x += 12f)
            {
                Knob(cv, new Vector2(x, Top(x) + 4f), 4.6f + Hash((int)x, 1) * 1.2f, new Vector2((x - CX) / RX * 0.5f, -1f).normalized);
                float flank = CY - (CY - Top(x)) * 0.5f;
                if (x > 44f && x < 146f) Knob(cv, new Vector2(x + 5f, flank), 3.4f, new Vector2((x - CX) / RX * 0.4f, -1f).normalized);
            }

            // Vieux harpon planté dans la carapace, une corde effilochée autour.
            cv.Piece(Capsule(58, 50, 32, 16, 2.2f), C(0.40f, 0.30f, 0.18f), 2f, (x, y, c) => (x + y) % 5 == 0 ? Darker(c, 0.7f) : c);
            for (int k = 0; k < 4; k++)
            {
                Vector2 w = Vector2.Lerp(new Vector2(52, 42), new Vector2(46, 34), k / 3f);
                cv.Piece(Capsule(w.x - 2.6f, w.y + 1f, w.x + 2.6f, w.y - 1f, 1f), C(0.56f, 0.48f, 0.32f), 0.8f, null, 0.95f, 0.6f);
            }

            cv.Piece(Tube(Curve(new Vector2(46, 34), new Vector2(40, 46), new Vector2(42, 58), 8), 0.9f, 0.5f), C(0.56f, 0.48f, 0.32f), 0.7f, null, 0.92f, 0.5f);
            cv.Piece(Ellipse(58, 50, 4f, 2.5f), C(0.36f, 0.18f, 0.10f), 1.5f);   // rouille autour de la plaie

            // Ce qui pousse dessus : fougères, un nénuphar Victoria, de la mousse qui pend.
            FernFrond(cv, new Vector2(80, 34), new Vector2(70, 10), new Vector2(54, 8), 6f);
            FernFrond(cv, new Vector2(86, 32), new Vector2(94, 6), new Vector2(110, 4), 5f);
            FernFrond(cv, new Vector2(134, 40), new Vector2(150, 24), new Vector2(160, 26), 4f);
            cv.Piece(Ellipse(112, 31, 16f, 3.6f), C(0.20f, 0.34f, 0.14f), 2.5f, (x, y, c) => Mathf.Repeat(Mathf.Atan2(y - 31, (x - 112) * 0.25f) * 3f, 1f) < 0.12f ? Darker(c, 0.75f) : c);
            cv.Piece(Rows(Ring(112, 31, 16f, 3.6f, 1.6f), 0f, 31f), C(0.52f, 0.18f, 0.14f), 1.2f);   // le rebord rouge relevé
            cv.Piece(Ellipse(104, 25, 3.2f, 4.2f), C(0.86f, 0.80f, 0.74f), 2.5f, (x, y, c) => y > 26 ? C(0.80f, 0.52f, 0.62f) : c);   // bouton de fleur
            foreach ((float x, float len) in new[] { (30f, 14f), (52f, 22f), (76f, 18f), (102f, 26f), (124f, 16f), (146f, 12f) })
            {
                HangingRoot(cv, new Vector2(x, 98f), len, 3f + Hash((int)x, 2) * 3f, (int)x);
            }

            // Sangsues collées au cou et à la patte.
            LeechAt(cv, neck.At(0.45f, out _) + new Vector2(0, 4), 0.6f);
            LeechAt(cv, neck.At(0.62f, out _) + new Vector2(-2, 1), -0.3f);
            LeechAt(cv, new Vector2(156, 110), 1.2f);

            // Petits poissons happés par le courant, devant la gueule.
            foreach ((float x, float y, float s) in new[] { (248f, 84f, 1f), (252f, 92f, 0.8f), (246f, 101f, 0.7f) })
            {
                cv.Piece(Ellipse(x, y, 3.6f * s, 2.2f * s), C(0.62f, 0.66f, 0.58f), 1.5f);
                cv.Piece(Poly(new Vector2(x - 3f * s, y), new Vector2(x - 6.5f * s, y - 2.5f * s), new Vector2(x - 6.5f * s, y + 2.5f * s)), C(0.52f, 0.56f, 0.48f), 1f);
            }

            Color[,] p = cv.Finish(Ink, 0.55f);

            // ---------- Après le contour (y depuis le bas) ----------
            int Y(float y) => Height - 1 - Mathf.RoundToInt(y);
            Halo(p, Mathf.RoundToInt(eye.x), Y(eye.y), 6, Glow, 2f);

            // Le courant aspiré : des traînées qui s'enroulent vers la gorge.
            for (int line = 0; line < 4; line++)
            {
                List<Vector2> swirl = Curve(new Vector2(255, 76 + line * 8), new Vector2(246, 70 + line * 9), new Vector2(222, 82), 30);
                for (int i = 0; i < swirl.Count; i++)
                {
                    if ((i + line) % 4 == 0) continue;
                    Mix(p, Mathf.RoundToInt(swirl[i].x), Y(swirl[i].y), C(0.74f, 0.86f, 0.82f), 0.32f * (1f - i / (float)swirl.Count));
                }
            }

            // Filets de bave entre les mâchoires.
            foreach (float x in new[] { 214f, 228f })
            {
                for (int y = 76; y < 90; y++) Mix(p, Mathf.RoundToInt(x + Mathf.Sin(y * 0.5f)), Y(y), C(0.70f, 0.66f, 0.56f), 0.35f);
            }

            // Spores qui luisent sur la mousse.
            for (int k = 0; k < 10; k++)
            {
                float x = 40f + Hash(k, 81) * 110f;
                Mix(p, Mathf.RoundToInt(x), Y(Top(x) + 6f + Hash(k, 83) * 10f), Glow, 0.4f);
            }

            return p;
        }

        // =====================================================================================================
        // Vue de dessus, tête à droite : la carapace comme une petite île, pattes écartées, tête en triangle.
        // =====================================================================================================
        public static Color[,] DrawTop()
        {
            var cv = new VolumeCanvas(Width, Height);
            const float CX = 98f, CY = 72f, RX = 68f, RY = 52f;

            // ---------- Pattes écartées, queue ----------
            Foot(cv, new Vector2(140, 40), new Vector2(160, 12), 9f, Skin, new Vector2(0.6f, -1f).normalized);
            Foot(cv, new Vector2(140, 104), new Vector2(160, 132), 9f, Skin, new Vector2(0.6f, 1f).normalized);
            Foot(cv, new Vector2(54, 38), new Vector2(34, 12), 8.5f, Skin, new Vector2(-0.6f, -1f).normalized);
            Foot(cv, new Vector2(54, 106), new Vector2(34, 132), 8.5f, Skin, new Vector2(-0.6f, 1f).normalized);
            cv.Piece(Poly(new Vector2(36, 64), new Vector2(36, 80), new Vector2(12, 74)), Skin, 4f, Hide);

            // ---------- Le cou et ses franges des deux côtés ----------
            var neck = new Spine();
            List<Vector2> neckLine = Curve(new Vector2(154, 72), new Vector2(178, 66), new Vector2(200, 72), 16);
            for (int i = 0; i < neckLine.Count; i++)
            {
                neck.Points.Add(neckLine[i]);
                neck.Radius.Add(Mathf.Lerp(17f, 13f, i / (float)(neckLine.Count - 1)));
            }

            foreach (float side in new[] { -1f, 1f })
            {
                cv.Piece(Poly(FinAlong(neck, 0.3f, 1f, side, k => 2f + 4f * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 4f)), 30)), SkinDark, 2f, Hide, 0.85f, 0.45f);
            }

            cv.Piece(neck.Inside, Skin, 9f, (x, y, c) =>
            {
                neck.Frame(x + 0.5f, y + 0.5f, out float t, out _, out _);
                Color hide = Hide(x, y, c);
                return Mathf.Repeat(t * 7f, 1f) < 0.12f ? Darker(hide, 0.72f) : hide;
            }, 0.82f, 0.42f);
            for (int k = 0; k < 5; k++) Knob(cv, neck.At(0.2f + k * 0.16f, out _), 2.6f, new Vector2(0f, -1f));

            // ---------- La tête : un large triangle plat, frangé ----------
            Vector2[] head = { new Vector2(192, 42), new Vector2(214, 50), new Vector2(246, 66), new Vector2(246, 78), new Vector2(214, 94), new Vector2(192, 102) };
            for (int k = 0; k < 8; k++)   // franges et lambeaux sur tout le bord
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    float t = k / 7f;
                    Vector2 a = Vector2.Lerp(new Vector2(194, 72 + side * 30f), new Vector2(240, 72 + side * 8f), t);
                    float len = 8f + Hash(k, side > 0 ? 3 : 4) * 6f - t * 4f;
                    cv.Piece(Poly(a + new Vector2(-3.5f, 0), a + new Vector2(3.5f, 0), a + new Vector2(-3f + Hash(k, 8) * 2f, side * len)), Color.Lerp(Skin, Belly, 0.5f), 1.6f, Hide, 0.85f, 0.5f);
                }
            }

            cv.Piece(Union(Poly(head), Ellipse(198, 72, 14f, 28f)), Skin, 11f, (x, y, c) =>
            {
                Color hide = Hide(x, y, c);
                return Mathf.Abs(y - 72f) < 2f + (x - 192f) * 0.05f && x > 206 ? Darker(hide, 0.8f) : hide;   // crête du milieu
            });
            cv.Piece(Capsule(244, 72, 254, 72, 3f), Skin, 2.5f, Hide);
            cv.Piece(Ellipse(254, 72, 1.6f, 1.6f), Ink, 1f);
            foreach (Vector2 b in new[] { new Vector2(206, 56), new Vector2(206, 88), new Vector2(222, 62), new Vector2(222, 82), new Vector2(200, 72), new Vector2(214, 72) })
            {
                cv.Piece(Ellipse(b.x, b.y, 2f, 1.8f), ShellLight, 1.5f, null, 0.9f, 0.6f);
            }

            var eyes = new[] { new Vector2(232, 65), new Vector2(232, 79) };
            foreach (Vector2 e in eyes)
            {
                cv.Piece(Ellipse(e.x, e.y, 2.4f, 2f), C(0.02f, 0.02f, 0.01f), 1.5f);
                cv.Piece(Ellipse(e.x, e.y, 1.4f, 1.2f), Glow, 1f);
            }

            // ---------- Carapace : bord dentelé, dôme, trois carènes ----------
            for (int k = 0; k < 26; k++)
            {
                float a = k / 26f * Mathf.PI * 2f;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 rim = new Vector2(CX + dir.x * (RX - 4f), CY + dir.y * (RY - 4f));
                Vector2 side = new Vector2(-dir.y, dir.x);
                cv.Piece(Poly(rim + side * 7f, rim - side * 7f, rim + dir * (10f + Hash(k, 13) * 5f)), Shell, 3f, (x, y, c) => Scutes(x, y, c, 8f, 999f), 0.8f, 0.45f);
            }

            cv.Piece(Ellipse(CX, CY, RX, RY), Shell, 20f, (x, y, c) => Scutes(x, y, c, 14f, 999f), 0.8f, 0.42f);
            for (float x = 46f; x <= 152f; x += 12f)
            {
                float w = Mathf.Sqrt(Mathf.Max(0f, 1f - (x - CX) * (x - CX) / (RX * RX)));
                Knob(cv, new Vector2(x, CY), 4.8f + Hash((int)x, 5), new Vector2(0.3f, -1f).normalized);
                if (w > 0.35f)
                {
                    Knob(cv, new Vector2(x + 4f, CY - RY * 0.52f * w), 3.6f, new Vector2(0f, -1f));
                    Knob(cv, new Vector2(x + 4f, CY + RY * 0.52f * w), 3.6f, new Vector2(0f, -1f));
                }
            }

            // Sur le dos : nénuphars Victoria, fougères, harpon, sangsues.
            foreach ((float x, float y, float r) in new[] { (86f, 52f, 15f), (128f, 92f, 9f) })
            {
                cv.Piece(Ellipse(x, y, r, r), C(0.20f, 0.34f, 0.14f), 3f, (px, py, c) =>
                {
                    float ang = Mathf.Atan2(py - y, px - x);
                    if (Mathf.Repeat(ang / Mathf.PI * 7f, 1f) < 0.1f) return Darker(c, 0.75f);   // nervures
                    return Hash(px, py) < 0.05f ? Darker(c, 0.85f) : c;
                });
                cv.Piece(Ring(x, y, r, r, 2f), C(0.52f, 0.18f, 0.14f), 1.5f);
                cv.Piece(Poly(new Vector2(x, y), new Vector2(x + r + 1f, y - 2.5f), new Vector2(x + r + 1f, y + 2.5f)), Shell, 1f, (px, py, c) => Scutes(px, py, c, 14f, 999f));   // l'encoche
            }

            cv.Piece(Ellipse(80, 48, 3.5f, 3.5f), C(0.86f, 0.80f, 0.74f), 2.5f, (x, y, c) => (x + y) % 3 == 0 ? C(0.80f, 0.52f, 0.62f) : c);
            FernFrond(cv, new Vector2(118, 50), new Vector2(132, 34), new Vector2(150, 34), 5f);
            FernFrond(cv, new Vector2(116, 54), new Vector2(112, 30), new Vector2(118, 18), 4.5f);
            FernFrond(cv, new Vector2(66, 94), new Vector2(56, 112), new Vector2(40, 112), 4.5f);
            cv.Piece(Capsule(70, 74, 46, 62, 2.2f), C(0.40f, 0.30f, 0.18f), 2f, (x, y, c) => (x + y) % 5 == 0 ? Darker(c, 0.7f) : c);
            cv.Piece(Ellipse(70, 74, 4f, 3f), C(0.36f, 0.18f, 0.10f), 1.5f);
            for (int k = 0; k < 3; k++)
            {
                Vector2 w = Vector2.Lerp(new Vector2(60, 69), new Vector2(54, 66), k / 2f);
                cv.Piece(Capsule(w.x - 1f, w.y - 2.6f, w.x + 1f, w.y + 2.6f, 1f), C(0.56f, 0.48f, 0.32f), 0.8f, null, 0.95f, 0.6f);
            }

            LeechAt(cv, new Vector2(170, 62), 0.4f);
            LeechAt(cv, new Vector2(176, 84), -0.5f);
            LeechAt(cv, new Vector2(148, 120), 1f);

            Color[,] p = cv.Finish(Ink, 0.55f);
            int Y(float y) => Height - 1 - Mathf.RoundToInt(y);
            foreach (Vector2 e in eyes) Halo(p, Mathf.RoundToInt(e.x), Y(e.y), 5, Glow, 2f);
            for (int k = 0; k < 14; k++)
            {
                float a = Hash(k, 91) * Mathf.PI * 2f, r = Hash(k, 93) * 0.8f;
                Mix(p, Mathf.RoundToInt(CX + Mathf.Cos(a) * RX * r), Y(CY + Mathf.Sin(a) * RY * r), Glow, 0.4f);
            }

            return p;
        }
    }
}
