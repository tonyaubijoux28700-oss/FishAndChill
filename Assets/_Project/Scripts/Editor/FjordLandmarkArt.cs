using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les repères des fjords de Norvège, dans le style en volume (32 px par case, y depuis le haut pendant le dessin) :
    /// rorbu (cabane de pêcheur rouge sur pilotis au-dessus de l'eau), petite église en bois blanc à clocher pointu,
    /// séchoir à morue (chevalets de bois où pend le poisson séché), grenier sur pilotis au toit d'herbe (stabbur),
    /// barque de bois, pans de falaise de granit enneigés et grande cascade (3 images).
    /// </summary>
    public static class FjordLandmarkArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Red = C(0.62f, 0.14f, 0.10f);
        private static readonly Color White = C(0.92f, 0.90f, 0.84f);
        private static readonly Color Roof = C(0.20f, 0.20f, 0.22f);
        private static readonly Color Wood = C(0.50f, 0.36f, 0.22f);
        private static readonly Color WoodDark = C(0.26f, 0.18f, 0.11f);
        private static readonly Color Glass = C(0.30f, 0.42f, 0.50f);
        private static readonly Color Granite = C(0.46f, 0.47f, 0.49f);
        private static readonly Color Snow = C(0.94f, 0.96f, 0.98f);
        private static readonly Color Turf = C(0.34f, 0.46f, 0.20f);
        private static readonly Color Ink = C(0.06f, 0.06f, 0.07f);
        private static readonly Color WaterShadow = C(0.03f, 0.08f, 0.12f, 0.45f);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        /// <summary>Planches verticales : joints sombres, fil du bois, quelques nœuds.</summary>
        private static Color Planks(int x, int y, Color c)
        {
            if (x % 7 == 0) return Darker(c, 0.68f);
            if (x % 7 == 1) return Darker(c, 1.12f);
            return Hash(x / 7, y / 3, 5) < 0.05f ? Darker(c, 0.8f) : c;
        }

        /// <summary>Tuiles ou bardeaux : rangées décalées, bord inférieur sombre.</summary>
        private static Color Shingles(int x, int y, Color c)
        {
            int row = y / 5;
            if (y % 5 == 4) return Darker(c, 0.65f);
            if ((x + row * 3) % 8 == 0) return Darker(c, 0.8f);
            return Hash(x / 8, row, 9) < 0.15f ? Darker(c, 1.15f) : c;
        }

        private static Color GraniteTex(int x, int y, Color c)
        {
            float n = Mathf.PerlinNoise(x * 0.06f + 3f, y * 0.06f);
            float m = Mathf.PerlinNoise(x * 0.2f, y * 0.2f + 9f);
            Color k = Color.Lerp(Darker(c, 0.75f), Darker(c, 1.15f), n);
            if (m > 0.72f) k = Darker(k, 0.78f);
            if (Mathf.Abs(Mathf.PerlinNoise(x * 0.04f, y * 0.11f) - 0.5f) < 0.015f) k = Darker(k, 0.55f);   // fissures
            return Hash(x, y, 3) < 0.04f ? Darker(k, 1.2f) : k;
        }

        private static void Shadow(Color[,] p, float cx, float cy, float rx, float ry, Color color)
        {
            for (int x = 0; x < p.GetLength(0); x++)
            {
                for (int y = 0; y < p.GetLength(1); y++)
                {
                    float u = (x - cx) / rx, v = (y - cy) / ry;
                    if (u * u + v * v > 1f || p[x, y].a > 0.5f) continue;
                    p[x, y] = color;
                }
            }
        }

        private static void Window(VolumeCanvas cv, float x0, float y0, float w, float h)
        {
            cv.Piece(Box(x0 - 2f, y0 - 2f, x0 + w + 2f, y0 + h + 2f), White, 1.5f);
            cv.Piece(Box(x0, y0, x0 + w, y0 + h), Glass, 1f, (x, y, c) => x < x0 + w * 0.4f && y < y0 + h * 0.5f ? Darker(c, 1.35f) : c);
            cv.Piece(Box(x0 + w * 0.5f - 0.8f, y0, x0 + w * 0.5f + 0.8f, y0 + h), White, 0.8f);
            cv.Piece(Box(x0, y0 + h * 0.5f - 0.8f, x0 + w, y0 + h * 0.5f + 0.8f), White, 0.8f);
        }

        // ---------- Rorbu ----------

        public const int RorbuWidth = 128;
        public const int RorbuHeight = 160;
        public static readonly Vector2 RorbuPivot = new Vector2(0.5f, 8f / RorbuHeight);

        /// <summary>
        /// Rorbu : cabane de pêcheur en planches rouges, encadrements blancs, toit de tuiles sombres, sur des pilotis de
        /// bois plantés dans l'eau, petite plate-forme devant et filet qui sèche à la rambarde.
        /// </summary>
        public static Color[,] Rorbu()
        {
            var cv = new VolumeCanvas(RorbuWidth, RorbuHeight);
            // Pilotis dans l'eau.
            foreach (float px in new[] { 18f, 46f, 82f, 110f })
            {
                cv.Piece(Box(px - 3f, 112f, px + 3f, 154f), WoodDark, 2f, (x, y, c) => y > 140 ? Darker(c, 0.7f) : Planks(x, y, c));
            }

            // Plate-forme et rambarde.
            cv.Piece(Box(6, 108, 122, 118), Wood, 2.5f, (x, y, c) => x % 9 == 0 ? Darker(c, 0.7f) : c);
            foreach (float px in new[] { 10f, 40f, 88f, 118f }) cv.Piece(Box(px - 1.2f, 94f, px + 1.2f, 108f), Wood, 1f);
            cv.Piece(Box(8, 93, 120, 96), Wood, 1f);
            // Le filet qui sèche sur la rambarde.
            cv.Piece((x, y) => x > 90 && x < 120 && y > 96 && y < 110 && ((x + y) % 4 == 0 || (x - y) % 4 == 0), C(0.36f, 0.50f, 0.46f), 0.6f);
            // Murs rouges.
            cv.Piece(Box(14, 50, 114, 108), Red, 3f, Planks);
            Window(cv, 26f, 66f, 18f, 16f);
            Window(cv, 84f, 66f, 18f, 16f);
            // Porte.
            cv.Piece(Box(54, 70, 74, 108), White, 1.5f);
            cv.Piece(Box(56, 72, 72, 108), C(0.24f, 0.30f, 0.36f), 1.5f, (x, y, c) => y % 8 == 0 ? Darker(c, 0.7f) : c);
            // Encadrements blancs aux coins.
            cv.Piece(Box(12, 50, 16, 108), White, 1.2f);
            cv.Piece(Box(112, 50, 116, 108), White, 1.2f);
            // Toit à deux pans (vu de face : grand pan sombre) et rive blanche.
            cv.Piece((x, y) => y > 14 && y < 54 && Mathf.Abs(x - 64f) < 58f - (54 - y) * 0.15f, Roof, 4f, Shingles);
            cv.Piece((x, y) => y >= 50 && y < 55 && x > 6 && x < 122, White, 1.2f);
            // Cheminée.
            cv.Piece(Box(88, 4, 98, 26), C(0.42f, 0.38f, 0.36f), 2f, (x, y, c) => y % 4 == 0 ? Darker(c, 0.7f) : c);
            Color[,] p = cv.Finish(Ink);
            Shadow(p, 64f, 4f, 66f, 5f, WaterShadow);
            return p;
        }

        // ---------- Église ----------

        public const int ChurchWidth = 160;
        public const int ChurchHeight = 224;
        public static readonly Vector2 ChurchPivot = new Vector2(0.5f, 6f / ChurchHeight);

        /// <summary>Petite église en bois blanc : bardage horizontal, toit sombre, clocher carré à flèche pointue, fenêtres en arc, porte rouge.</summary>
        public static Color[,] Church()
        {
            var cv = new VolumeCanvas(ChurchWidth, ChurchHeight);
            Func<int, int, Color, Color> clapboard = (x, y, c) => y % 6 == 0 ? Darker(c, 0.78f) : y % 6 == 1 ? Darker(c, 1.04f) : c;
            // Nef.
            cv.Piece(Box(14, 120, 146, 216), White, 3f, clapboard);
            cv.Piece((x, y) => y > 92 && y < 124 && Mathf.Abs(x - 80f) < 72f - (124 - y) * 0.2f, Roof, 4f, Shingles);
            // Fenêtres en arc.
            foreach (float wx in new[] { 32f, 116f })
            {
                cv.Piece((x, y) => x > wx - 7 && x < wx + 7 && y > 140 && y < 176 && (y > 147 || (x - wx) * (x - wx) + (y - 147) * (y - 147) < 49), Glass, 1f,
                    (x, y, c) => x < wx - 2 ? Darker(c, 1.3f) : c);
            }

            // Clocher.
            cv.Piece(Box(56, 54, 104, 216), White, 4f, clapboard);
            cv.Piece((x, y) => x > 72 && x < 88 && y > 70 && y < 96 && (y > 78 || (x - 80) * (x - 80) + (y - 78) * (y - 78) < 64), Ink, 1f);   // abat-son
            cv.Piece(Box(70, 160, 90, 216), Red, 2f, (x, y, c) => x == 80 ? Darker(c, 0.6f) : y % 7 == 0 ? Darker(c, 0.8f) : c);   // porte
            cv.Piece((x, y) => x > 68 && x < 92 && y > 150 && y <= 160 && (x - 80) * (x - 80) + (y - 160) * (y - 160) < 144, White, 1.5f);
            // Flèche pointue et croix.
            cv.Piece((x, y) => y > 14 && y < 58 && Mathf.Abs(x - 80f) < (y - 14f) * 0.62f, Roof, 3f, Shingles);
            cv.Piece(Box(79, 0, 81, 16), C(0.70f, 0.60f, 0.30f), 1f);
            cv.Piece(Box(75, 4, 85, 6), C(0.70f, 0.60f, 0.30f), 1f);
            Color[,] p = cv.Finish(Ink);
            Shadow(p, 80f, 4f, 72f, 4f, C(0.10f, 0.12f, 0.14f, 0.3f));
            return p;
        }

        // ---------- Séchoir à morue ----------

        public const int RackWidth = 192;
        public const int RackHeight = 112;
        public static readonly Vector2 RackPivot = new Vector2(0.5f, 6f / RackHeight);

        /// <summary>Séchoir à morue (hjell) : chevalets de perches en A, barres transversales, rangées de poissons séchés qui pendent par paires.</summary>
        public static Color[,] Rack()
        {
            var cv = new VolumeCanvas(RackWidth, RackHeight);
            Color pole = C(0.46f, 0.40f, 0.34f);
            foreach (float px in new[] { 14f, 96f, 178f })
            {
                cv.Piece(Capsule(px - 10f, 106f, px, 14f, 2.4f), pole, 2f, (x, y, c) => (x + y) % 6 == 0 ? Darker(c, 0.75f) : c);
                cv.Piece(Capsule(px + 10f, 106f, px, 14f, 2.4f), pole, 2f, (x, y, c) => (x + y) % 6 == 0 ? Darker(c, 0.75f) : c);
            }

            foreach (float by in new[] { 20f, 52f })
            {
                cv.Piece(Capsule(8f, by, 184f, by, 2f), pole, 1.6f);
                // Les poissons séchés, par paires, qui pendent sous la barre.
                for (int k = 0; k < 16; k++)
                {
                    float fx = 16f + k * 10.6f + Hash(k, (int)by, 3) * 2f;
                    float len = 22f + Hash(k, (int)by, 7) * 6f;
                    Color fish = Color.Lerp(C(0.60f, 0.54f, 0.44f), C(0.42f, 0.36f, 0.30f), Hash(k, (int)by, 11));
                    cv.Piece((x, y) => y > by + 1 && y < by + len && Mathf.Abs(x - fx) < 3.2f - (y - by) / len * 1.4f, fish, 1.4f,
                        (x, y, c) => y > by + len - 5 ? Darker(c, 0.7f) : (x + y) % 5 == 0 ? Darker(c, 0.85f) : c);
                    cv.Piece(Poly(new Vector2(fx - 3f, by + len - 1f), new Vector2(fx + 3f, by + len - 1f), new Vector2(fx, by + len + 4f)), Darker(fish, 0.7f), 1f);
                }
            }

            Color[,] p = cv.Finish(Ink);
            Shadow(p, 96f, 4f, 92f, 4f, C(0.10f, 0.12f, 0.14f, 0.3f));
            return p;
        }

        // ---------- Grenier au toit d'herbe ----------

        public const int StorehouseWidth = 112;
        public const int StorehouseHeight = 136;
        public static readonly Vector2 StorehousePivot = new Vector2(0.5f, 6f / StorehouseHeight);

        /// <summary>Stabbur : grenier en rondins sombres posé sur des piliers de pierre, toit d'herbe où poussent des fleurs, escalier détaché.</summary>
        public static Color[,] Storehouse()
        {
            var cv = new VolumeCanvas(StorehouseWidth, StorehouseHeight);
            Color log = C(0.36f, 0.24f, 0.14f);
            foreach (float px in new[] { 20f, 56f, 92f })
            {
                cv.Piece(Box(px - 5f, 108f, px + 5f, 130f), C(0.52f, 0.52f, 0.50f), 2f, GraniteTex);
            }

            cv.Piece(Box(10, 56, 102, 110), log, 3f, (x, y, c) => y % 8 == 0 ? Darker(c, 0.6f) : y % 8 == 1 ? Darker(c, 1.2f) : Hash(x / 4, y / 8, 2) < 0.1f ? Darker(c, 0.85f) : c);
            cv.Piece(Box(46, 74, 66, 110), C(0.22f, 0.14f, 0.08f), 1.5f, (x, y, c) => x % 5 == 0 ? Darker(c, 0.7f) : c);
            // Escalier détaché (contre les rongeurs).
            for (int k = 0; k < 4; k++) cv.Piece(Box(44 - k * 5, 112 + k * 5, 68 - k * 5, 116 + k * 5), C(0.52f, 0.52f, 0.50f), 1.5f, GraniteTex);
            // Toit d'herbe débordant, avec ses fleurs.
            cv.Piece((x, y) => y > 18 && y < 60 && Mathf.Abs(x - 56f) < 54f - (60 - y) * 0.12f, Turf, 5f, (x, y, c) =>
            {
                float n = Mathf.PerlinNoise(x * 0.2f, y * 0.2f);
                Color k = Color.Lerp(Darker(c, 0.8f), Darker(c, 1.2f), n);
                if (Hash(x, y, 21) < 0.04f) return C(0.96f, 0.86f, 0.30f);
                if (Hash(x, y, 22) < 0.03f) return C(0.90f, 0.40f, 0.60f);
                return y > 56 ? Darker(k, 0.7f) : k;
            });
            // Brins d'herbe qui dépassent du bord.
            for (int k = 0; k < 18; k++)
            {
                float gx = 6f + k * 5.6f;
                cv.Piece(Capsule(gx, 60f, gx + (Hash(k, 1, 4) - 0.5f) * 3f, 64f + Hash(k, 2, 4) * 4f, 0.8f), Turf, 0.8f);
            }

            Color[,] p = cv.Finish(Ink);
            Shadow(p, 56f, 4f, 50f, 4f, C(0.10f, 0.12f, 0.14f, 0.3f));
            return p;
        }

        // ---------- Barque ----------

        public const int BoatWidth = 112;
        public const int BoatHeight = 48;
        public static readonly Vector2 BoatPivot = new Vector2(0.5f, 0.5f);

        /// <summary>Færing : barque de bois clair à bordages, pointue aux deux bouts, liseré rouge, deux rames.</summary>
        public static Color[,] Boat()
        {
            var cv = new VolumeCanvas(BoatWidth, BoatHeight);
            Func<float, float, bool> hull = (x, y) => Mathf.Abs(y - 24f) < 14f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((x - 56f) / 52f, 2f))) * (1f - Mathf.Abs(x - 56f) / 104f);
            cv.Piece(hull, Wood, 4f, (x, y, c) => Mathf.Abs(y - 24f) % 4 < 0.9f ? Darker(c, 0.7f) : c);
            cv.Piece((x, y) => hull(x, y) && !Ellipse(56f, 24f, 44f, 8f)(x, y), Red, 2f);
            cv.Piece(Ellipse(56f, 24f, 42f, 7f), C(0.30f, 0.22f, 0.14f), 1f, (x, y, c) => x % 12 == 0 ? Wood : c);
            cv.Piece(Capsule(40f, 10f, 70f, 38f, 1.5f), WoodDark, 1f);
            cv.Piece(Capsule(70f, 10f, 40f, 38f, 1.5f), WoodDark, 1f);
            Color[,] p = cv.Finish(Ink);
            return p;
        }

        // ---------- Passerelle ----------

        /// <summary>
        /// Passerelle de bois qui traverse le fjord, vue de dessus (<paramref name="lengthCells"/> cases de long, 2 de large) :
        /// planches en travers, deux rambardes, pieux à chaque bout et tous les 3 cases. Pivot au milieu.
        /// </summary>
        public static Color[,] Bridge(int lengthCells)
        {
            int w = lengthCells * 32, h = 80;
            var cv = new VolumeCanvas(w, h);
            cv.Piece(Box(0, 18, w, 62), Wood, 2.5f, (x, y, c) => x % 8 == 0 ? Darker(c, 0.62f) : x % 8 == 1 ? Darker(c, 1.12f) : Hash(x / 8, y / 4, 31) < 0.06f ? Darker(c, 0.8f) : c);
            for (int k = 0; k <= lengthCells; k += 3)
            {
                float px = Mathf.Min(w - 5f, k * 32f + 4f);
                cv.Piece(Box(px - 3f, 6f, px + 3f, 22f), WoodDark, 2f);
                cv.Piece(Box(px - 3f, 58f, px + 3f, 76f), WoodDark, 2f);
            }

            cv.Piece(Box(0, 10, w, 15), Wood, 1.5f);   // rambarde du haut
            cv.Piece(Box(0, 64, w, 69), Wood, 1.5f);   // rambarde du bas
            Color[,] p = cv.Finish(Ink, 0.5f);
            return p;
        }

        // ---------- Falaises et cascade ----------

        public const int CliffWidth = 256;
        public const int CliffHeight = 224;
        public static readonly Vector2 CliffPivot = new Vector2(0.5f, 6f / CliffHeight);

        /// <summary>Pan de falaise de granit gris : paroi fissurée, corniches, mousse et lichens, neige sur les replats et le sommet.</summary>
        public static Color[,] Cliff(int variant)
        {
            var cv = new VolumeCanvas(CliffWidth, CliffHeight);
            float seed = variant * 37f;
            Func<float, float, bool> wall = (x, y) =>
            {
                float top = 30f + Mathf.PerlinNoise(x * 0.025f + seed, 1f) * 40f + Mathf.Abs(Mathf.Sin(x * 0.05f + seed)) * 10f;
                return y > top && y < 218f && x > 4f + Mathf.PerlinNoise(y * 0.03f, seed) * 14f && x < 252f - Mathf.PerlinNoise(y * 0.03f + 5f, seed) * 14f;
            };
            cv.Piece(wall, Granite, 16f, (x, y, c) =>
            {
                Color k = GraniteTex(x + (int)seed, y, c);
                if (Mathf.PerlinNoise(x * 0.08f + seed, y * 0.08f) > 0.7f && y > 120) k = Color.Lerp(k, C(0.30f, 0.40f, 0.22f), 0.6f);   // mousse en bas
                float top = 30f + Mathf.PerlinNoise(x * 0.025f + seed, 1f) * 40f + Mathf.Abs(Mathf.Sin(x * 0.05f + seed)) * 10f;
                if (y < top + 12f + Mathf.PerlinNoise(x * 0.1f, seed) * 10f) k = Color.Lerp(Snow, C(0.76f, 0.82f, 0.90f), Mathf.PerlinNoise(x * 0.15f, y * 0.15f));   // la neige du sommet
                // Neige sur les corniches.
                if (Mathf.Repeat(y + Mathf.PerlinNoise(x * 0.05f, seed) * 30f, 64f) < 2f && Mathf.PerlinNoise(x * 0.07f + seed, y * 0.07f) > 0.62f) k = Color.Lerp(Snow, k, 0.25f);
                // Grandes fissures verticales des parois de fjord.
                if (Mathf.Abs(Mathf.PerlinNoise(x * 0.09f + seed, y * 0.012f) - 0.5f) < 0.018f) k = Darker(k, 0.5f);
                k = Color.Lerp(k, Darker(k, 0.7f), Mathf.Clamp01((y - 120f) / 100f));   // plus sombre vers le pied
                return k;
            });
            Color[,] p = cv.Finish(Ink, 0.5f);
            return p;
        }

        public const int FallWidth = 96;
        public const int FallHeight = 352;
        public static readonly Vector2 FallPivot = new Vector2(0.5f, 6f / FallHeight);
        public const int FallFrames = 3;

        /// <summary>Grande cascade qui tombe de la falaise : rideau d'eau blanche et bleue qui ruisselle (3 images), bassin d'écume et brume en bas.</summary>
        public static Color[,] Waterfall(int frame)
        {
            var p = new Color[FallWidth, FallHeight];
            for (int x = 0; x < FallWidth; x++)
            {
                for (int y = 0; y < FallHeight; y++)
                {
                    int yy = FallHeight - 1 - y;   // depuis le haut
                    float half = 18f + yy * 0.06f + Mathf.Sin(yy * 0.05f) * 2f;
                    float dx = x - 48f;
                    if (yy < FallHeight - 46 && Mathf.Abs(dx) < half)
                    {
                        float streak = Mathf.PerlinNoise(x * 0.45f, (yy - frame * 14f) * 0.08f);
                        Color c = Color.Lerp(C(0.52f, 0.70f, 0.82f), C(0.94f, 0.98f, 1f), streak);
                        if (Mathf.Abs(dx) > half - 3f) c = Darker(c, 0.8f);
                        p[x, y] = c;
                    }
                    else if (yy >= FallHeight - 60)
                    {
                        // Écume et brume au pied de la cascade.
                        float r = (dx * dx) / (46f * 46f) + ((yy - (FallHeight - 28f)) * (yy - (FallHeight - 28f))) / (28f * 28f);
                        float n = Mathf.PerlinNoise(x * 0.2f + frame * 3f, yy * 0.2f);
                        if (r < 1f && n > 0.3f) p[x, y] = new Color(0.94f, 0.97f, 1f, Mathf.Clamp01((1f - r) * 1.6f) * (n > 0.55f ? 1f : 0.6f));
                    }
                }
            }

            return p;
        }
    }
}
