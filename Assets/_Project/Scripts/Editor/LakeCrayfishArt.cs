using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// « Bramafan », la légende du Lac d'Annecy : une écrevisse titan (d'après l'écrevisse signal), vue de dessus, tête à
    /// droite, 256×144, style en volume : carapace en armure fissurée couverte de vase, de galets et de moules, queue en
    /// éventail, anneaux bombés à pointes, une antenne arrachée, pattes articulées, deux pinces inégales à tubercules (une énorme pince-marteau, une pince-cisaille dentelée) avec la tache claire
    /// de l'écrevisse signal qui luit à l'articulation, grappes d'yeux sur pédoncules, antennes démesurées, œufs qui luisent
    /// sous la queue. y depuis le haut pendant le dessin.
    /// </summary>
    public static class LakeCrayfishArt
    {
        public const int Width = 256;
        public const int Height = 144;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Shell = C(0.30f, 0.13f, 0.08f);
        private static readonly Color ShellDark = C(0.12f, 0.05f, 0.04f);
        private static readonly Color ShellLight = C(0.52f, 0.28f, 0.16f);
        private static readonly Color Mud = C(0.24f, 0.21f, 0.14f);
        private static readonly Color Pebble = C(0.46f, 0.45f, 0.42f);
        private static readonly Color Glow = C(0.62f, 0.95f, 0.92f);
        private static readonly Color Ink = C(0.04f, 0.02f, 0.02f);
        private const float Mid = 72f;

        /// <summary>Armure : plaques rouge sombre, fissures, piqûres, vase collée dans les creux.</summary>
        private static Color Armor(int x, int y, Color c)
        {
            float n = Noise(x / 6f, y / 6f);
            if (Mathf.Abs(Noise(x / 9f + 20f, y / 9f) - 0.5f) < 0.03f) return ShellDark;   // fissures
            if (n < 0.22f) return Color.Lerp(c, Mud, 0.7f);                                  // vase
            if (Hash(x, y) < 0.05f) return Darker(c, 0.6f);                                  // piqûres
            return n > 0.7f ? Color.Lerp(c, ShellLight, 0.35f) : c;
        }

        private static void Leg(VolumeCanvas cv, Vector2 root, float side, float reach, float lean)
        {
            Vector2 knee = root + new Vector2(lean * reach * 0.5f, side * reach * 0.6f);
            Vector2 foot = knee + new Vector2(lean * reach * 0.45f - 4f, side * reach * 0.38f);
            cv.Piece(Capsule(root.x, root.y, knee.x, knee.y, 3f), Shell, 2.5f, Armor);
            cv.Piece(Capsule(knee.x, knee.y, foot.x, foot.y, 2.2f), Shell, 2f, Armor);
            cv.Piece(Ellipse(knee.x, knee.y, 3.2f, 3.2f), ShellDark, 2f);
            cv.Piece(Poly(foot + new Vector2(-1.5f, 0), foot + new Vector2(1.5f, 0), foot + new Vector2(-3f, side * 5f)), ShellDark, 1f);
        }

        public static Color[,] Draw()
        {
            var cv = new VolumeCanvas(Width, Height);

            // ---------- Antennes démesurées, rabattues par-dessus le dos ----------
            // Celle du haut a été arrachée à mi-longueur : un bout effiloché.
            cv.Piece(Tube(Curve(new Vector2(178, 62), new Vector2(214, 10), new Vector2(160, 4), 16), 2f, 1.3f), Shell, 1.4f, (x, y, c) => (x + y) % 6 < 2 ? ShellDark : c);
            foreach (Vector2 frayed in new[] { new Vector2(-6, -3), new Vector2(-5, 2), new Vector2(-7, 0) })
            {
                cv.Piece(Capsule(160, 4, 160 + frayed.x, 4 + frayed.y, 0.6f), ShellDark, 0.6f);
            }
            cv.Piece(Tube(Curve(new Vector2(178, 82), new Vector2(236, 150), new Vector2(120, 136), 24), 2f, 0.7f), Shell, 1.4f, (x, y, c) => (x + y) % 6 < 2 ? ShellDark : c);

            // ---------- Pattes marcheuses ----------
            for (int k = 0; k < 4; k++)
            {
                float x = 112f + k * 12f;
                Leg(cv, new Vector2(x, Mid - 16f), -1f, 50f + Hash(k, 1) * 6f, -0.9f + k * 0.5f);
                Leg(cv, new Vector2(x, Mid + 16f), 1f, 48f + Hash(k, 2) * 6f, -0.85f + k * 0.5f);
            }

            // ---------- Queue en éventail et abdomen en anneaux, œufs qui luisent dessous ----------
            cv.Piece(Poly(new Vector2(30, Mid - 8), new Vector2(30, Mid + 8), new Vector2(10, Mid + 11), new Vector2(8, Mid - 11)), Shell, 3f, Armor);
            cv.Piece(Poly(new Vector2(30, Mid - 8), new Vector2(26, Mid - 2), new Vector2(6, Mid - 24), new Vector2(12, Mid - 30)), Shell, 3f, Armor);
            cv.Piece(Poly(new Vector2(30, Mid + 8), new Vector2(26, Mid + 2), new Vector2(6, Mid + 24), new Vector2(12, Mid + 30)), Shell, 3f, Armor);
            for (int k = 0; k < 6; k++)
            {
                float cx = 34f + k * 12.5f;
                float hh = 13f + k * 2f;
                if (k >= 1 && k <= 4)
                {
                    foreach (float side in new[] { -1f, 1f })
                    {
                        for (int e = 0; e < 4; e++)
                        {
                            cv.Piece(Ellipse(cx - 4f + e * 2.5f, Mid + side * (hh + 2f + (e % 2) * 2f), 2f, 2f), Glow, 1.5f, null, 0.95f, 0.7f);
                        }
                    }
                }

                // Anneau bombé : bord arrière plus clair, côté avant dans l'ombre de l'anneau suivant, pointes sur les côtés.
                float ccx = cx;
                cv.Piece(Ellipse(cx, Mid, 10f, hh), Shell, 6f, (x, y, c) =>
                    x < ccx - 7f ? Color.Lerp(Armor(x, y, c), ShellLight, 0.35f) : x > ccx + 6f ? Darker(Armor(x, y, c), 0.7f) : Armor(x, y, c));
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector2 tip = new Vector2(cx - 3f, Mid + side * (hh + 2.5f));
                    cv.Piece(Poly(new Vector2(cx - 6f, Mid + side * (hh - 2f)), new Vector2(cx + 1f, Mid + side * (hh - 2f)), tip), ShellDark, 1f);
                }
            }

            // ---------- Les pinces ----------
            // Bras du haut, jusqu'à l'énorme pince-marteau.
            cv.Piece(Capsule(166, Mid - 12, 186, 42, 6f), Shell, 4f, Armor);
            cv.Piece(Capsule(186, 42, 200, 30, 6.5f), Shell, 4f, Armor);
            cv.Piece(Poly(new Vector2(222, 16), new Vector2(252, 6), new Vector2(254, 14), new Vector2(232, 26)), Shell, 4f, Armor);   // doigt mobile
            cv.Piece(Poly(new Vector2(226, 34), new Vector2(254, 38), new Vector2(250, 46), new Vector2(222, 42)), Shell, 4f, Armor);   // doigt fixe
            cv.Piece(Ellipse(214, 30, 22f, 14f), Shell, 9f, Armor);
            for (int k = 0; k < 4; k++)
            {
                cv.Piece(Ellipse(234 + k * 5, 25 - k * 1.5f, 2.2f, 2f), C(0.70f, 0.62f, 0.50f), 1.5f);   // dents en molaires
                cv.Piece(Ellipse(234 + k * 5, 37 + k * 0.6f, 2.2f, 2f), C(0.70f, 0.62f, 0.50f), 1.5f);
            }

            // Bras du bas, jusqu'à la pince-cisaille fine et dentelée.
            cv.Piece(Capsule(166, Mid + 12, 186, 104, 5f), Shell, 4f, Armor);
            cv.Piece(Capsule(186, 104, 200, 116, 5f), Shell, 4f, Armor);
            cv.Piece(Poly(new Vector2(222, 112), new Vector2(254, 100), new Vector2(254, 105), new Vector2(226, 118)), Shell, 3f, Armor);
            cv.Piece(Poly(new Vector2(224, 124), new Vector2(254, 134), new Vector2(252, 139), new Vector2(222, 130)), Shell, 3f, Armor);
            for (int k = 0; k < 7; k++)
            {
                Vector2 a = Vector2.Lerp(new Vector2(226, 116), new Vector2(252, 104), k / 6f);
                cv.Piece(Poly(a + new Vector2(-1, 0), a + new Vector2(1.5f, 0), a + new Vector2(0, 3.5f)), C(0.70f, 0.62f, 0.50f), 0.6f);
                Vector2 b = Vector2.Lerp(new Vector2(226, 126), new Vector2(252, 136), k / 6f);
                cv.Piece(Poly(b + new Vector2(-1, 0), b + new Vector2(1.5f, 0), b + new Vector2(0, -3.5f)), C(0.70f, 0.62f, 0.50f), 0.6f);
            }

            cv.Piece(Ellipse(212, 120, 17f, 9f), Shell, 7f, Armor);

            // Tubercules pointus sur les bras et les pinces.
            foreach (Vector2 b in new[] { new Vector2(176, 44), new Vector2(182, 38), new Vector2(194, 26), new Vector2(204, 20), new Vector2(214, 17), new Vector2(222, 18),
                         new Vector2(178, 104), new Vector2(190, 110), new Vector2(204, 112), new Vector2(214, 112) })
            {
                cv.Piece(Poly(b + new Vector2(-1.6f, 1.5f), b + new Vector2(1.6f, 1.5f), b + new Vector2(-1f, -3f)), ShellLight, 1f, null, 0.9f, 0.6f);
            }

            // Pièces de la bouche, qui remuent sous le rostre.
            foreach (float side in new[] { -1f, 1f })
            {
                cv.Piece(Tube(Curve(new Vector2(168, Mid + side * 4f), new Vector2(178, Mid + side * 9f), new Vector2(184, Mid + side * 6f), 6), 1.4f, 0.7f), ShellDark, 1f);
            }

            // La tache claire de l'écrevisse signal, à l'articulation de chaque pince.
            cv.Piece(Ellipse(228, 27, 4f, 3f), Glow, 2f);
            cv.Piece(Ellipse(225, 119, 3.4f, 2.6f), Glow, 2f);

            // ---------- La carapace, avec son rostre ----------
            cv.Piece(Union(Ellipse(138, Mid, 38f, 25f), Poly(new Vector2(166, Mid - 7), new Vector2(194, Mid), new Vector2(166, Mid + 7))), Shell, 12f, Armor);
            cv.Piece(Tube(Curve(new Vector2(132, Mid - 23), new Vector2(124, Mid), new Vector2(132, Mid + 23), 12), 1.3f, 1.3f), ShellDark, 1f, null, 0.9f, 0.6f);   // sillon
            cv.Piece(Tube(Curve(new Vector2(150, Mid - 4), new Vector2(170, Mid - 2), new Vector2(188, Mid), 8), 1.2f, 0.6f), ShellDark, 1f, null, 0.9f, 0.6f);

            // Galets, moules zébrées et algues collés sur l'armure.
            foreach ((float x, float y, float r) in new[] { (122f, 60f, 5f), (148f, 84f, 4f), (60f, 66f, 3.5f), (100f, 80f, 3f), (156f, 58f, 3f) })
            {
                cv.Piece(Ellipse(x, y, r * 1.2f, r), Pebble, r, (px, py, c) => Hash(px, py) < 0.15f ? Darker(c, 0.8f) : c);
            }

            for (int k = 0; k < 10; k++)
            {
                float mx = 84f + Hash(k, 171) * 70f, my = Mid - 18f + Hash(k, 173) * 36f;
                cv.Piece(Ellipse(mx, my, 2.2f, 1.4f), C(0.58f, 0.52f, 0.36f), 1.3f, (x, y, c) => (x - Mathf.RoundToInt(mx)) % 2 == 0 ? C(0.18f, 0.14f, 0.09f) : c);
            }

            cv.Piece(Tube(Curve(new Vector2(112, Mid - 10), new Vector2(96, Mid - 30), new Vector2(70, Mid - 34), 10), 1f, 0.5f), C(0.26f, 0.36f, 0.16f), 0.8f);
            cv.Piece(Tube(Curve(new Vector2(70, Mid + 14), new Vector2(54, Mid + 36), new Vector2(34, Mid + 34), 10), 1f, 0.5f), C(0.26f, 0.36f, 0.16f), 0.8f);

            // ---------- Grappes d'yeux sur pédoncules ----------
            var eyes = new System.Collections.Generic.List<Vector2>();
            foreach (float side in new[] { -1f, 1f })
            {
                for (int k = 0; k < 3; k++)
                {
                    Vector2 root = new Vector2(172f, Mid + side * 6f);
                    Vector2 tip = root + new Vector2(8f + k * 3f, side * (6f + k * 4f));
                    cv.Piece(Capsule(root.x, root.y, tip.x, tip.y, 1.6f), Shell, 1.4f);
                    cv.Piece(Ellipse(tip.x, tip.y, 2.6f, 2.6f), C(0.02f, 0.04f, 0.05f), 1.5f);
                    cv.Piece(Ellipse(tip.x, tip.y, 1.6f, 1.6f), Glow, 1f);
                    eyes.Add(tip);
                }
            }

            Color[,] p = cv.Finish(Ink, 0.55f);

            // ---------- Lueurs (y depuis le bas) ----------
            int Y(float y) => Height - 1 - Mathf.RoundToInt(y);
            foreach (Vector2 e in eyes) Halo(p, Mathf.RoundToInt(e.x), Y(e.y), 5, Glow, 2f);
            Halo(p, 228, Y(27), 8, Glow, 3f);
            Halo(p, 225, Y(119), 7, Glow, 3f);
            for (int k = 1; k <= 4; k++)
            {
                float cx = 34f + k * 12.5f, hh = 13f + k * 2f;
                Halo(p, Mathf.RoundToInt(cx), Y(Mid - hh - 3f), 6, Glow, 2f);
                Halo(p, Mathf.RoundToInt(cx), Y(Mid + hh + 3f), 6, Glow, 2f);
            }

            return p;
        }
    }
}
