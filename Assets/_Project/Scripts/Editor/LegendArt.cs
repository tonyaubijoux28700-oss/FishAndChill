using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Artefacts des poissons de légende, dans le style en volume : l'Idole de corail (statuette sculptée dans le corail
    /// rose, yeux de nacre turquoise sertis d'or, couronne de branches), coupée en 4 fragments aux bords dentelés qui se
    /// superposent exactement à l'idole entière, et sa silhouette. Tableaux [x, y] avec y depuis le bas.
    /// </summary>
    public static class LegendArt
    {
        public const int IdolWidth = 48;
        public const int IdolHeight = 64;
        public const int IdolFragments = 4;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Coral = C(0.94f, 0.46f, 0.42f);
        private static readonly Color CoralDark = C(0.72f, 0.28f, 0.32f);
        private static readonly Color Pearl = C(0.36f, 0.88f, 0.86f);
        private static readonly Color Gold = C(1f, 0.80f, 0.30f);

        /// <summary>Texture du corail : pores clairs, quelques creux.</summary>
        private static Color Pores(int x, int y, Color c)
            => (x * 7 + y * 13) % 11 == 0 ? Darker(c, 1.18f) : (x + y * 3) % 17 == 0 ? Darker(c, 0.82f) : c;

        public static Color[,] Idol()
        {
            var cv = new VolumeCanvas(IdolWidth, IdolHeight);

            // Couronne de branches de corail.
            cv.Piece(Union(Capsule(14f, 9f, 9f, 2f, 2.6f), Capsule(34f, 9f, 39f, 2f, 2.6f), Capsule(24f, 6f, 24f, 1.5f, 2.8f),
                Capsule(19f, 6f, 17f, 2f, 1.8f), Capsule(29f, 6f, 31f, 2f, 1.8f)), Coral, 2.5f, Pores);

            // Socle, corps aux bras sculptés, tête ronde.
            cv.Piece(Union(Box(7, 54, 41, 63), Ellipse(24f, 54f, 17f, 3f)), CoralDark, 3f, Pores);
            cv.Piece(Union(Box(12, 32, 36, 56), Ellipse(24f, 33f, 13f, 5f)), Coral, 5f,
                (x, y, c) =>
                {
                    bool arm = Mathf.Abs(x - 15.5f - (y - 36) * 0.18f) < 1f || Mathf.Abs(x - 32.5f + (y - 36) * 0.18f) < 1f;
                    bool belt = y == 47 || y == 48;
                    return arm && y > 36 && y < 50 ? Darker(c, 0.7f) : belt ? Darker(c, 0.8f) : Pores(x, y, c);
                });
            cv.Piece(Ellipse(24f, 19f, 16f, 14f), Coral, 7f, Pores);

            // Yeux de nacre sertis d'or, bouche aux dents blanches.
            foreach (float ex in new[] { 17f, 31f })
            {
                cv.Piece(Ellipse(ex, 17f, 4.8f, 4.2f), Gold, 2f);
                float cx = ex;
                cv.Piece(Ellipse(ex, 17f, 3.2f, 2.8f), Pearl, 2.5f, (x, y, c) => x < cx && y < 17 ? C(0.85f, 1f, 1f) : c);
            }

            cv.Piece(Box(15, 24, 33, 29), C(0.30f, 0.10f, 0.14f), 1.5f, (x, y, c) => y == 25 && x % 3 != 0 ? C(0.98f, 0.95f, 0.88f) : c);

            // Gemme sur la poitrine.
            cv.Piece(Ellipse(24f, 41f, 3f, 3f), Pearl, 2.5f);
            return cv.Finish(C(0.36f, 0.10f, 0.14f), 0.6f);
        }

        /// <summary>Numéro du fragment (0 haut gauche, 1 haut droite, 2 bas gauche, 3 bas droite) d'un pixel (y depuis le bas).</summary>
        public static int PieceOf(int x, int y)
        {
            float splitX = 24f + Mathf.PingPong(y * 0.9f, 3f) - 1.5f;
            float splitY = 31f + Mathf.PingPong(x * 0.8f, 3f) - 1.5f;
            bool right = x >= splitX;
            bool top = y >= splitY;
            return (top ? 0 : 2) + (right ? 1 : 0);
        }

        /// <summary>Un fragment, dans le cadre de l'idole entière ; sa cassure est assombrie.</summary>
        public static Color[,] IdolPiece(int piece)
        {
            Color[,] full = Idol();
            var p = new Color[IdolWidth, IdolHeight];
            for (int x = 0; x < IdolWidth; x++)
            {
                for (int y = 0; y < IdolHeight; y++)
                {
                    if (full[x, y].a < 0.01f || PieceOf(x, y) != piece)
                    {
                        continue;
                    }

                    bool edge = false;
                    for (int dx = -1; dx <= 1 && !edge; dx++)
                    {
                        for (int dy = -1; dy <= 1 && !edge; dy++)
                        {
                            int a = x + dx, b = y + dy;
                            edge = a >= 0 && b >= 0 && a < IdolWidth && b < IdolHeight && full[a, b].a > 0.01f && PieceOf(a, b) != piece;
                        }
                    }

                    p[x, y] = edge ? Darker(full[x, y], 0.62f) : full[x, y];
                }
            }

            return p;
        }

        /// <summary>Centre d'un fragment dans le cadre de l'idole (0 à 1), pour le montrer centré sur la carte.</summary>
        public static Vector2 PieceCenter(int piece)
        {
            Color[,] p = IdolPiece(piece);
            int xMin = IdolWidth, xMax = -1, yMin = IdolHeight, yMax = -1;
            for (int x = 0; x < IdolWidth; x++)
            {
                for (int y = 0; y < IdolHeight; y++)
                {
                    if (p[x, y].a < 0.01f) continue;
                    xMin = Mathf.Min(xMin, x);
                    xMax = Mathf.Max(xMax, x);
                    yMin = Mathf.Min(yMin, y);
                    yMax = Mathf.Max(yMax, y);
                }
            }

            return xMax < 0 ? new Vector2(0.5f, 0.5f) : new Vector2((xMin + xMax + 1) * 0.5f / IdolWidth, (yMin + yMax + 1) * 0.5f / IdolHeight);
        }

        /// <summary>L'idole en ombre sombre et transparente : la place des fragments manquants.</summary>
        public static Color[,] IdolSilhouette()
        {
            Color[,] full = Idol();
            var p = new Color[IdolWidth, IdolHeight];
            for (int x = 0; x < IdolWidth; x++)
            {
                for (int y = 0; y < IdolHeight; y++)
                {
                    if (full[x, y].a > 0.01f)
                    {
                        p[x, y] = C(0.10f, 0.14f, 0.22f, 0.55f);
                    }
                }
            }

            return p;
        }
    }
}
