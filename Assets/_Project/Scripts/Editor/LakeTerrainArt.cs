using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Terrain du lac redessiné (tuiles 16×16, 16 px par case, y depuis le bas) : terre battue du chemin, falaise de
    /// pierre (bord du plateau, haut et bas de la paroi), cascade animée (4 images par case), touffes d'herbes hautes.
    /// </summary>
    public static class LakeTerrainArt
    {
        private const int S = 16;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Dirt = C(0.62f, 0.48f, 0.32f);
        private static readonly Color DirtDark = C(0.50f, 0.37f, 0.24f);
        private static readonly Color DirtLight = C(0.72f, 0.58f, 0.40f);
        private static readonly Color Stone = C(0.56f, 0.54f, 0.52f);
        private static readonly Color StoneDark = C(0.38f, 0.36f, 0.36f);
        private static readonly Color StoneLight = C(0.70f, 0.68f, 0.64f);
        private static readonly Color Moss = C(0.32f, 0.50f, 0.26f);
        private static readonly Color WaterLight = C(0.80f, 0.94f, 0.98f);
        private static readonly Color WaterMid = C(0.48f, 0.76f, 0.88f);
        private static readonly Color WaterDark = C(0.26f, 0.52f, 0.70f);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        /// <summary>Terre battue (périodique) : grain, petits cailloux, traces plus claires.</summary>
        public static Color[,] DirtFill(int variant)
        {
            var p = new Color[S, S];
            for (int x = 0; x < S; x++)
            {
                for (int y = 0; y < S; y++)
                {
                    float h = Hash(x, y, 17 + variant);
                    p[x, y] = h < 0.12f ? DirtDark : h > 0.9f ? DirtLight : Dirt;
                }
            }

            foreach ((int x, int y) in new[] { (3, 4), (11, 9), (6, 13), (13, 2) })
            {
                int px = (x + variant * 5) % S, py = (y + variant * 3) % S;
                p[px, py] = StoneLight;
                p[(px + 1) % S, py] = Stone;
                p[px, (py + S - 1) % S] = StoneDark;
            }

            return p;
        }

        /// <summary>Bord du plateau, au-dessus de la paroi : herbe en haut, lèvre de pierre moussue en bas.</summary>
        public static Color[,] CliffTop(Color[,] grass)
        {
            var p = (Color[,])grass.Clone();
            for (int x = 0; x < S; x++)
            {
                int lip = 3 + (Hash(x, 0, 5) < 0.4f ? 1 : 0);
                for (int y = 0; y < lip; y++)
                {
                    p[x, y] = y == lip - 1 ? Moss : y == 0 ? StoneDark : StoneLight;
                }
            }

            return p;
        }

        /// <summary>Haut de la paroi : pierre en strates, fissures verticales, touffes de mousse.</summary>
        public static Color[,] CliffFace(int variant) => Rock(variant, bottom: false);

        /// <summary>Bas de la paroi : même pierre, éboulis et ombre portée sur le sol.</summary>
        public static Color[,] CliffBase(int variant) => Rock(variant, bottom: true);

        private static Color[,] Rock(int variant, bool bottom)
        {
            var p = new Color[S, S];
            for (int x = 0; x < S; x++)
            {
                for (int y = 0; y < S; y++)
                {
                    // Strates horizontales, décalées par colonne de blocs.
                    int block = (x + variant * 3) / 5;
                    int strata = (y + block * 2 + 32) % 5;
                    Color c = strata == 0 ? StoneDark : strata == 4 ? StoneLight : Stone;
                    if ((x + variant * 3) % 5 == 0 && strata != 4) c = StoneDark;              // fissure
                    float shade = Mathf.Lerp(0.9f, 1.05f, Hash(x, y, 40 + variant));
                    c = new Color(c.r * shade, c.g * shade, c.b * shade);
                    if (!bottom && y >= S - 2 && Hash(x, y, 7) < 0.5f) c = Moss;              // mousse sous la lèvre
                    if (bottom && y <= 2) c = Color.Lerp(c, StoneDark, 0.6f);                  // pied plus sombre
                    p[x, y] = c;
                }
            }

            if (bottom)
            {
                // Quelques éboulis au pied.
                foreach (int bx in new[] { 2 + variant, 9, 13 - variant })
                {
                    PixelArt.Set(p, bx, 1, StoneLight);
                    PixelArt.Set(p, bx + 1, 1, Stone);
                    PixelArt.Set(p, bx, 0, StoneDark);
                }
            }

            return p;
        }

        /// <summary>
        /// Cascade : <paramref name="row"/> 0 = pied (écume), 1 = milieu, 2 = sommet (sur le bord du plateau) ;
        /// <paramref name="column"/> 0, 1, 2 de gauche à droite ; <paramref name="frame"/> 0 à 3 : l'eau descend.
        /// </summary>
        public static Color[,] Waterfall(int column, int row, int frame)
        {
            var p = new Color[S, S];
            for (int x = 0; x < S; x++)
            {
                int gx = column * S + x;
                bool edge = (column == 0 && x < 2) || (column == 2 && x > 13);
                for (int y = 0; y < S; y++)
                {
                    int gy = row * S + y;
                    float streak = Mathf.Repeat(gy + frame * 4 + (gx % 7) * 3, 12f);
                    Color c = streak < 2f ? WaterLight : streak < 6f ? WaterMid : WaterDark;
                    if (edge) c = Color.Lerp(c, StoneDark, 0.5f);
                    if (row == 0 && y < 6)
                    {
                        // Écume au pied : bouillonnement blanc qui change à chaque image.
                        c = Hash(gx, y + frame * 3, 77) < 0.6f - y * 0.06f ? WaterLight : WaterMid;
                    }

                    if (row == 2 && y > 11) c = Color.Lerp(c, WaterMid, 0.5f);                // le torrent arrive du plateau
                    p[x, y] = c;
                }
            }

            return p;
        }

        /// <summary>Touffe d'herbes hautes (16×16, pivot au pied), on passe à travers.</summary>
        public static Color[,] TallGrass(int variant)
        {
            var p = new Color[S, S];
            var random = new System.Random(900 + variant);
            Color light = C(0.52f, 0.76f, 0.36f), mid = C(0.36f, 0.60f, 0.28f), dark = C(0.22f, 0.42f, 0.20f);
            for (int i = 0; i < 9; i++)
            {
                float x0 = 3f + random.Next(0, 10);
                int top = 6 + random.Next(0, 7);
                float lean = (float)(random.NextDouble() - 0.5) * 5f;
                Color c = i % 3 == 0 ? light : i % 3 == 1 ? mid : dark;
                for (int y = 1; y <= top; y++)
                {
                    float t = y / (float)top;
                    PixelArt.Set(p, Mathf.RoundToInt(x0 + lean * t * t), y, c);
                }
            }

            PixelArt.SelectiveOutline(p, C(0.10f, 0.20f, 0.08f), 0.4f);
            return p;
        }

        /// <summary>Touffe de fleurs des prés (16×16, pivot au pied) : gentianes bleues, marguerites, boutons d'or.</summary>
        public static Color[,] FlowerClump(int variant)
        {
            var p = new Color[S, S];
            var random = new System.Random(700 + variant);
            Color stem = C(0.30f, 0.56f, 0.26f);
            Color[] petals = variant == 0 ? new[] { C(0.30f, 0.44f, 0.92f), C(0.42f, 0.58f, 0.98f) }
                : variant == 1 ? new[] { C(0.98f, 0.98f, 0.96f), C(0.94f, 0.92f, 0.88f) }
                : new[] { C(1f, 0.86f, 0.24f), C(0.98f, 0.70f, 0.20f) };
            for (int i = 0; i < 6; i++)
            {
                int x0 = 2 + random.Next(0, 12);
                int top = 3 + random.Next(0, 6);
                for (int y = 1; y <= top; y++) PixelArt.Set(p, x0, y, stem);
                Color petal = petals[i % 2];
                PixelArt.Set(p, x0, top + 1, petal);
                PixelArt.Set(p, x0 - 1, top, petal);
                PixelArt.Set(p, x0 + 1, top, petal);
                PixelArt.Set(p, x0, top, variant == 1 ? C(1f, 0.84f, 0.20f) : petal);   // cœur jaune des marguerites
            }

            PixelArt.SelectiveOutline(p, C(0.10f, 0.20f, 0.08f), 0.35f);
            return p;
        }

        public const int TreeWidth = 32;
        public const int TreeHeight = 40;
        public static readonly Vector2 TreePivot = new Vector2(16f / TreeWidth, 5f / TreeHeight);

        /// <summary>
        /// Feuillu (32×40, pivot au pied comme le sapin) : <paramref name="variant"/> 0 hêtre au houppier rond vert franc,
        /// 1 bouleau au tronc blanc tacheté et au feuillage plus clair.
        /// </summary>
        public static Color[,] Broadleaf(int variant)
        {
            var cv = new VolumeCanvas(TreeWidth, TreeHeight);
            Color trunk = variant == 1 ? C(0.92f, 0.90f, 0.86f) : C(0.42f, 0.30f, 0.20f);
            Color leaves = variant == 1 ? C(0.48f, 0.72f, 0.34f) : C(0.28f, 0.56f, 0.26f);
            cv.Piece(VolumeCanvas.Capsule(16f, 36f, 16f, 20f, variant == 1 ? 1.6f : 2.2f), trunk, 1.5f,
                variant == 1 ? (x, y, c) => (x + y * 3) % 7 == 0 ? C(0.20f, 0.18f, 0.16f) : c : (System.Func<int, int, Color, Color>)null);
            cv.Piece(VolumeCanvas.Union(VolumeCanvas.Ellipse(16f, 15f, 11f, 9f), VolumeCanvas.Ellipse(9f, 19f, 6.5f, 5.5f), VolumeCanvas.Ellipse(23f, 19f, 6.5f, 5.5f),
                VolumeCanvas.Ellipse(16f, 8f, 7f, 5f)), leaves, 5f, (x, y, c) => (x * 5 + y * 3) % 13 == 0 ? VolumeCanvas.Darker(c, 0.82f) : c);
            Color[,] p = cv.Finish(C(0.08f, 0.16f, 0.08f), 0.5f);
            // Ombre au sol sous le houppier.
            for (int x = 6; x < 26; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    float u = (x + 0.5f - 16f) / 10f, v = (y + 0.5f - 3f) / 2.2f;
                    if (u * u + v * v <= 1f && p[x, y].a < 0.01f) p[x, y] = C(0.04f, 0.14f, 0.08f, 0.35f);
                }
            }

            return p;
        }
    }
}
