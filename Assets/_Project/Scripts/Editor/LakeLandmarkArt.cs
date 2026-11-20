using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les repères du lac, dans le style en volume (32 px par case, y depuis le haut pendant le dessin) : le chalet
    /// savoyard de la presqu'île, les montagnes enneigées au fond, et la passerelle de bois sur le torrent (16 px par
    /// case, comme le sol).
    /// </summary>
    public static class LakeLandmarkArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Wood = C(0.56f, 0.36f, 0.20f);
        private static readonly Color WoodDark = C(0.36f, 0.22f, 0.12f);
        private static readonly Color WoodLight = C(0.70f, 0.48f, 0.28f);
        private static readonly Color Stone = C(0.66f, 0.64f, 0.60f);
        private static readonly Color Shingle = C(0.30f, 0.28f, 0.30f);
        private static readonly Color Glass = C(0.66f, 0.86f, 0.92f);
        private static readonly Color Geranium = C(0.92f, 0.22f, 0.26f);
        private static readonly Color Leaf = C(0.30f, 0.58f, 0.28f);
        private static readonly Color Snow = C(0.96f, 0.97f, 1f);
        private static readonly Color Rock = C(0.52f, 0.54f, 0.60f);
        private static readonly Color RockDark = C(0.36f, 0.38f, 0.46f);

        // ---------- Chalet ----------

        public const int ChaletWidth = 128;
        public const int ChaletHeight = 120;
        public static readonly Vector2 ChaletPivot = new Vector2(0.5f, 6f / ChaletHeight);

        /// <summary>Chalet savoyard : soubassement de pierre, étage en madriers, balcon fleuri de géraniums, grand toit.</summary>
        public static Color[,] Chalet()
        {
            var cv = new VolumeCanvas(ChaletWidth, ChaletHeight);
            cv.Piece(Box(14, 84, 114, 114), Stone, 2.5f, (x, y, c) => (x / 9 + y / 6) % 2 == 0 && (x % 9 == 0 || y % 6 == 0) ? Darker(c, 0.72f) : (x * 3 + y) % 17 == 0 ? Darker(c, 0.9f) : c);
            cv.Piece(Box(16, 46, 112, 86), Wood, 2.5f, (x, y, c) => y % 5 == 0 ? WoodDark : y % 5 == 1 ? WoodLight : c);   // madriers
            // Porte et fenêtres à volets.
            cv.Piece(Box(56, 90, 72, 114), WoodDark, 2f);
            cv.Piece(Box(58, 92, 70, 114), Wood, 2.5f, (x, y, c) => x == 64 ? WoodDark : c);
            foreach (int wx in new[] { 32, 96 })
            {
                cv.Piece(Box(wx - 8, 92, wx + 8, 106), C(0.92f, 0.90f, 0.84f), 1.5f);
                cv.Piece(Box(wx - 6, 94, wx + 6, 104), Glass, 2f, (x, y, c) => x == wx || y == 99 ? C(0.92f, 0.90f, 0.84f) : c);
                cv.Piece(Box(wx - 13, 92, wx - 8, 106), C(0.70f, 0.20f, 0.18f), 1.5f, (x, y, c) => y % 3 == 0 ? Darker(c, 0.8f) : c);
                cv.Piece(Box(wx + 8, 92, wx + 13, 106), C(0.70f, 0.20f, 0.18f), 1.5f, (x, y, c) => y % 3 == 0 ? Darker(c, 0.8f) : c);
            }

            foreach (int wx in new[] { 36, 92 })
            {
                cv.Piece(Box(wx - 9, 54, wx + 9, 70), Glass, 2f, (x, y, c) => x == wx || y == 62 ? WoodLight : c);
            }

            // Balcon : plancher, balustres découpés, jardinières de géraniums.
            cv.Piece(Box(10, 72, 118, 76), WoodDark, 2f);
            for (int bx = 12; bx < 118; bx += 6)
            {
                cv.Piece(Union(Box(bx, 76, bx + 4, 84), Ellipse(bx + 2f, 80f, 1.2f, 1.2f)), WoodLight, 1.2f, (x, y, c) => Mathf.Abs(x - (bx + 2f)) < 0.8f && Mathf.Abs(y - 80f) < 0.8f ? WoodDark : c);
            }

            cv.Piece(Box(10, 82, 118, 86), WoodDark, 2f);
            for (int fx = 16; fx < 114; fx += 7)
            {
                cv.Piece(Ellipse(fx, 70f, 3.2f, 2.6f), Leaf, 2f);
                cv.Piece(Ellipse(fx + 1f, 68.5f, 1.6f, 1.4f), Geranium, 1.5f);
            }

            // Grand toit à deux pans, débordant, bardeaux sombres, faîtage et pignon en bois.
            cv.Piece((x, y) => y > 6 && y < 50 && Mathf.Abs(x - 64f) < 64f - (50f - y) * 0.2f - (y < 30 ? (30f - y) * 1.6f : 0f), Shingle, 5f,
                (x, y, c) => (x + (y / 4) * 3) % 6 == 0 ? Darker(c, 0.75f) : y % 4 == 0 ? Darker(c, 1.15f) : c);
            cv.Piece((x, y) => y > 22 && y < 48 && Mathf.Abs(x - 64f) < (y - 22f) * 1.5f, WoodLight, 2f, (x, y, c) => x % 4 == 0 ? WoodDark : c);   // pignon
            cv.Piece(Ellipse(64f, 38f, 4f, 4f), C(0.92f, 0.90f, 0.84f), 2f);                                                              // œil-de-bœuf
            cv.Piece(Ellipse(64f, 38f, 2.5f, 2.5f), Glass, 2f);
            cv.Piece(Box(84, 4, 94, 22), Stone, 2.5f);                                                                                   // cheminée
            Color[,] p = cv.Finish(C(0.14f, 0.10f, 0.08f));
            for (int x = 2; x < ChaletWidth - 2; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    if (p[x, y].a < 0.01f) p[x, y] = C(0.05f, 0.15f, 0.08f, 0.3f);
                }
            }

            return p;
        }

        // ---------- Montagnes ----------

        public const int MountainWidth = 320;
        public const int MountainHeight = 200;
        public static readonly Vector2 MountainPivot = new Vector2(0.5f, 0f);

        /// <summary>Massif enneigé : deux ou trois sommets, roche bleutée, neige sur les crêtes, ombre côté droit.</summary>
        public static Color[,] Mountain(int variant)
        {
            var cv = new VolumeCanvas(MountainWidth, MountainHeight);
            (float x, float top, float half)[] peaks = variant == 0
                ? new[] { (110f, 18f, 120f), (215f, 50f, 105f) }
                : new[] { (90f, 46f, 95f), (175f, 10f, 125f), (260f, 60f, 80f) };
            foreach ((float px, float top, float half) in peaks)
            {
                float peakX = px, peakTop = top, peakHalf = half;
                cv.Piece((x, y) => y > peakTop && y < MountainHeight && Mathf.Abs(x - peakX) < (y - peakTop) / (MountainHeight - peakTop) * peakHalf + 2f,
                    Rock, 6f, (x, y, c) =>
                    {
                        float depth = (y - peakTop) / (MountainHeight - peakTop);
                        bool shadow = x > peakX + (y - peakTop) * 0.15f;
                        // Neige au sommet, coulées de neige le long des crêtes.
                        if (depth < 0.28f + Mathf.Sin(x * 0.3f) * 0.05f) return shadow ? C(0.80f, 0.84f, 0.92f) : Snow;
                        if (depth < 0.42f && Mathf.Abs(Mathf.Sin(x * 0.21f + peakX)) > 0.8f) return shadow ? C(0.80f, 0.84f, 0.92f) : Snow;
                        Color rock = Color.Lerp(Rock, C(0.42f, 0.50f, 0.44f), depth * depth);   // pied plus vert (alpages)
                        if ((x + y * 2) % 23 == 0) rock = Darker(rock, 0.85f);
                        return shadow ? Darker(rock, 0.78f) : rock;
                    });
            }

            return cv.Finish(C(0.20f, 0.22f, 0.30f), 0.4f);
        }

        // ---------- Passerelle ----------

        /// <summary>Passerelle de planches sur le torrent, <paramref name="cells"/> cases de long, 2 de large (16 px par case).</summary>
        public static Color[,] Bridge(int cells, int rows)
        {
            int w = cells * 16, h = rows * 16 + 4;
            var p = new Color[w, h];
            for (int x = 0; x < w; x++)
            {
                for (int y = 2; y < h - 2; y++)
                {
                    bool gap = x % 6 == 5;
                    Color c = gap ? WoodDark : (x / 6) % 2 == 0 ? Wood : WoodLight;
                    if (y == 2) c = WoodDark;
                    p[x, y] = c;
                }

                // Garde-corps en haut et en bas, poteaux réguliers.
                p[x, h - 2] = WoodLight;
                p[x, h - 1] = WoodDark;
                p[x, 1] = WoodDark;
                if (x % 12 == 2)
                {
                    for (int y = 0; y < h; y++) p[x, y] = y > h - 4 || y < 3 ? WoodDark : p[x, y];
                }
            }

            // Ombre sur l'eau sous la passerelle.
            for (int x = 0; x < w; x++) p[x, 0] = C(0.05f, 0.12f, 0.18f, 0.35f);
            return p;
        }
    }
}
