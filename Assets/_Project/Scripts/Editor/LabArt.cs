using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Laboratoire de biologie marine, dans le style en volume (<see cref="VolumeCanvas"/>, 32 px par case) : façade
    /// (murs blanchis, toit de tôle bleue, coupole d'observation, antenne, hublots, porte vitrée, enseigne au squelette
    /// de poisson), intérieur 11×8 cases (carrelage, aquarium d'étude, étagères à bocaux, grande carte, affiche du
    /// poisson de légende), bureau, socle de pierre et plante, chacun en sprite à part pour passer devant le joueur.
    /// Disposition calée sur les obstacles de <see cref="World.LabBuilding"/>. y compté depuis le haut.
    /// </summary>
    public static class LabArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color White = C(0.95f, 0.95f, 0.92f);
        private static readonly Color Steel = C(0.36f, 0.58f, 0.78f);
        private static readonly Color Metal = C(0.74f, 0.76f, 0.78f);
        private static readonly Color Glass = C(0.62f, 0.86f, 0.94f);
        private static readonly Color Stone = C(0.62f, 0.60f, 0.56f);
        private static readonly Color Wood = C(0.56f, 0.38f, 0.22f);
        private static readonly Color WoodDark = C(0.38f, 0.24f, 0.13f);
        private static readonly Color Orange = C(1f, 0.58f, 0.20f);
        private static readonly Color Leaf = C(0.30f, 0.62f, 0.32f);

        // ---------- Façade ----------

        public const int ExteriorWidth = 160;
        public const int ExteriorHeight = 144;

        /// <summary>Pied de la façade (la porte).</summary>
        public static readonly Vector2 ExteriorPivot = new Vector2(0.5f, 6f / ExteriorHeight);

        public static Color[,] Exterior()
        {
            var cv = new VolumeCanvas(ExteriorWidth, ExteriorHeight);

            // Murs blanchis à la chaux, grain léger ; soubassement de pierre.
            cv.Piece(Box(8, 62, 152, 134), White, 2.5f,
                (x, y, c) => (x * 7 + y * 13) % 23 == 0 ? Darker(c, 0.92f) : y > 124 ? Darker(c, 0.95f) : c);
            cv.Piece(Box(6, 128, 154, 138), Stone, 2.5f, (x, y, c) => x % 16 == 0 || y == 133 ? Darker(c, 0.75f) : c);
            cv.Piece(Union(Box(6, 62, 12, 136), Box(148, 62, 154, 136)), C(0.80f, 0.82f, 0.84f), 2f);

            // Hublots : deux ronds cerclés de laiton ; à gauche, un poisson orange nage derrière la vitre.
            foreach (float hx in new[] { 32f, 128f })
            {
                cv.Piece(Ellipse(hx, 94f, 15f, 15f), C(0.86f, 0.68f, 0.30f), 4f);
                float cx = hx;
                cv.Piece(Ellipse(hx, 94f, 11f, 11f), Glass, 5f,
                    (x, y, c) =>
                    {
                        float d = (x - cx) + (y - 94f);
                        return d > -9f && d < -5f ? C(0.92f, 0.98f, 1f) : Color.Lerp(c, C(0.24f, 0.52f, 0.70f), Mathf.Clamp01((y - 86f) / 16f));
                    });
            }

            cv.Piece(Union(Ellipse(30f, 97f, 5f, 3f), (x, y) => x > 34 && x < 38 && Mathf.Abs(y - 97f) < (x - 34) * 0.9f), Orange, 2.5f);
            cv.Dot(27, 96, C(0.08f, 0.06f, 0.08f));

            // Porte vitrée à deux battants, encadrement métal, poignées.
            cv.Piece(Box(62, 82, 98, 136), Metal, 2f);
            cv.Piece((x, y) => Box(65, 85, 95, 134)(x, y) && Mathf.Abs(x - 80) > 1, Glass, 3f,
                (x, y, c) =>
                {
                    int d = (x - 65) - (134 - y) / 2;
                    return (d > 3 && d < 6) || (d > 18 && d < 20) ? C(0.92f, 0.98f, 1f) : Color.Lerp(Darker(c, 0.82f), c, (134 - y) / 50f);
                });
            cv.Dot(77, 110, C(0.40f, 0.42f, 0.46f));
            cv.Dot(83, 110, C(0.40f, 0.42f, 0.46f));

            // Enseigne bleue au squelette de poisson.
            cv.Piece(Box(54, 64, 106, 80), C(0.20f, 0.42f, 0.66f), 3f);
            cv.Piece(Capsule(64f, 72f, 90f, 72f, 1.2f), White, 1f);
            for (int i = 0; i < 4; i++)
            {
                float rx = 70f + i * 5f;
                cv.Piece(Capsule(rx, 67.5f, rx, 76.5f, 0.7f), White, 0.8f);
            }

            cv.Piece((x, y) => x > 90 && x < 97 && Mathf.Abs(y - 72f) < (x - 90) * 0.8f, White, 1f);
            cv.Piece(Ellipse(62f, 72f, 3.5f, 3.5f), White, 2f);
            cv.Dot(61, 71, C(0.20f, 0.42f, 0.66f));

            // Toit de tôle ondulée bleue, débordant.
            cv.Piece((x, y) => y > 38 && y < 64 && Mathf.Abs(x - 80f) < 78f - (64f - y) * 0.35f, Steel, 5f,
                (x, y, c) => x % 6 == 0 ? Darker(c, 0.74f) : x % 6 == 1 ? Darker(c, 1.14f) : y > 60 ? Darker(c, 0.85f) : c);

            // Coupole d'observation, à droite, avec sa fente et sa lunette.
            cv.Piece((x, y) => Ellipse(118f, 42f, 24f, 22f)(x, y) && y < 44, Metal, 10f,
                (x, y, c) => Mathf.Abs(x - 118f) < 3f && y < 40 ? C(0.16f, 0.20f, 0.28f) : (x + y) % 9 == 0 ? Darker(c, 0.9f) : c);
            cv.Piece(Capsule(118f, 24f, 132f, 10f, 2.4f), C(0.30f, 0.32f, 0.36f), 2f);
            cv.Piece(Box(92, 40, 144, 46), C(0.56f, 0.58f, 0.62f), 2f);

            // Antenne parabolique, à gauche.
            cv.Piece(Capsule(36f, 40f, 36f, 18f, 1.2f), C(0.50f, 0.52f, 0.56f), 1f);
            cv.Piece(Ellipse(32f, 18f, 10f, 6f), White, 5f, (x, y, c) => x > 34 ? Darker(c, 0.85f) : c);
            cv.Piece(Capsule(32f, 18f, 42f, 12f, 0.8f), C(0.50f, 0.52f, 0.56f), 0.8f);
            cv.Piece(Ellipse(43f, 11f, 1.8f, 1.8f), C(0.95f, 0.30f, 0.26f), 1.5f);

            Color[,] p = cv.Finish(C(0.12f, 0.14f, 0.20f));

            // Ombre au sol et paillasson bleu sous la porte (y depuis le bas).
            for (int x = 2; x < ExteriorWidth - 2; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    if (p[x, y].a < 0.01f) p[x, y] = C(0.05f, 0.15f, 0.08f, 0.3f);
                }
            }

            for (int x = 64; x <= 96; x++)
            {
                for (int y = 0; y <= 4; y++)
                {
                    p[x, y] = y == 0 || y == 4 ? C(0.14f, 0.28f, 0.46f) : (x + y) % 3 == 0 ? C(0.40f, 0.66f, 0.86f) : C(0.22f, 0.44f, 0.68f);
                }
            }

            return p;
        }

        // ---------- Intérieur (352×256 = 11×8 cases) ----------

        public const int InteriorWidth = 352;
        public const int InteriorHeight = 256;
        private const int WallBottom = 96;   // limite mur / carrelage (5 cases au-dessus du bas)

        public static Color[,] Interior()
        {
            var cv = new VolumeCanvas(InteriorWidth, InteriorHeight);
            Color wall = C(0.84f, 0.92f, 0.90f), tile = C(0.92f, 0.94f, 0.92f), teal = C(0.42f, 0.70f, 0.72f);

            // Mur du fond : peinture d'eau pâle, frise de carreaux bleus à mi-hauteur, plinthe.
            cv.Piece(Box(-1, -1, InteriorWidth + 1, WallBottom), wall, 1.5f,
                (x, y, c) => y > 62 && y < 70 ? ((x / 8 + y / 4) % 2 == 0 ? teal : Darker(teal, 1.12f)) : (x * 5 + y * 3) % 37 == 0 ? Darker(c, 0.95f) : c);
            cv.Piece(Box(-1, WallBottom - 4, InteriorWidth + 1, WallBottom + 1), C(0.30f, 0.44f, 0.50f), 1.5f);

            // Carrelage en damier, joints gris.
            cv.Piece(Box(-1, WallBottom, InteriorWidth + 1, InteriorHeight + 1), tile, 1.2f,
                (x, y, c) =>
                {
                    int gx = x / 16, gy = (y - WallBottom) / 16;
                    if (x % 16 == 0 || (y - WallBottom) % 16 == 0) return Darker(c, 0.80f);
                    return (gx + gy) % 2 == 0 ? c : C(0.70f, 0.84f, 0.84f);
                }, 0.9f, 0.8f);

            // Murs de côté et bas de la pièce, paillasson.
            cv.Piece(Union(Box(-1, -1, 7, InteriorHeight + 1), Box(InteriorWidth - 7, -1, InteriorWidth + 1, InteriorHeight + 1)), C(0.30f, 0.38f, 0.42f), 2f);
            cv.Piece(Box(-1, InteriorHeight - 4, InteriorWidth + 1, InteriorHeight + 1), C(0.30f, 0.38f, 0.42f), 2f);
            cv.Piece(Box(160, InteriorHeight - 10, 192, InteriorHeight - 3), C(0.22f, 0.44f, 0.68f), 2.5f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.85f) : c);

            // Grande carte du monde au mur, punaises rouges sur les zones.
            cv.Piece(Box(108, 10, 206, 58), WoodDark, 2.5f);
            cv.Piece(Box(112, 14, 202, 54), C(0.56f, 0.76f, 0.86f), 1.5f, (x, y, c) => (x * 3 + y * 7) % 29 == 0 ? Darker(c, 0.92f) : c);
            cv.Piece(Union(Ellipse(134f, 28f, 11f, 8f), Ellipse(138f, 42f, 6f, 7f), Ellipse(162f, 26f, 13f, 9f), Ellipse(170f, 40f, 7f, 6f), Ellipse(190f, 44f, 8f, 5f)),
                C(0.86f, 0.80f, 0.58f), 2f, (x, y, c) => (x + y) % 6 == 0 ? Darker(c, 0.9f) : c);
            foreach ((int px, int py) in new[] { (128, 22), (156, 24), (174, 40), (190, 46), (140, 46) })
            {
                cv.Piece(Ellipse(px, py, 2f, 2f), C(0.92f, 0.24f, 0.22f), 1.5f);
            }

            // Affiche du poisson de légende : silhouette géante sombre et point d'interrogation doré.
            cv.Piece(Box(212, 10, 252, 58), C(0.92f, 0.86f, 0.70f), 1.5f);
            cv.Piece(Union(Ellipse(228f, 38f, 11f, 7f), (x, y) => x > 237 && x < 247 && Mathf.Abs(y - 38f) < (x - 237) * 0.9f), C(0.18f, 0.22f, 0.30f), 4f);
            cv.Dot(222, 37, C(0.95f, 0.30f, 0.26f));
            cv.Piece(Union(Capsule(228f, 20f, 232f, 16f, 1.4f), Capsule(232f, 16f, 236f, 20f, 1.4f), Capsule(236f, 20f, 232f, 25f, 1.4f)), C(1f, 0.80f, 0.28f), 1.2f);
            cv.Piece(Ellipse(232f, 28.5f, 1.4f, 1.4f), C(1f, 0.80f, 0.28f), 1.2f);

            // ---------- Aquarium d'étude, à gauche : meuble en bois, grande cuve, poissons, bulles ----------
            cv.Piece(Box(10, 96, 102, 132), Wood, 3f, (x, y, c) => x == 56 || y == 100 ? Darker(c, 0.72f) : c);
            cv.Piece(Ellipse(33f, 116f, 2f, 2f), C(1f, 0.80f, 0.30f), 1.5f);
            cv.Piece(Ellipse(79f, 116f, 2f, 2f), C(1f, 0.80f, 0.30f), 1.5f);
            cv.Piece(Box(12, 22, 100, 98), C(0.20f, 0.24f, 0.30f), 2f);
            cv.Piece(Box(15, 26, 97, 95), C(0.30f, 0.64f, 0.76f), 2f,
                (x, y, c) =>
                {
                    if (y > 86) return (x * 7 + y) % 5 == 0 ? C(0.82f, 0.74f, 0.52f) : C(0.92f, 0.84f, 0.60f);   // sable
                    if ((x - y) % 23 == 0 || (x - y) % 23 == 1) return C(0.70f, 0.92f, 0.98f);                  // reflets
                    return Color.Lerp(C(0.40f, 0.76f, 0.86f), C(0.16f, 0.44f, 0.62f), (y - 26f) / 60f);
                }, 0.9f, 0.9f);
            for (int k = 0; k < 5; k++)
            {
                float sx = 22f + k * 17f;
                cv.Piece(Capsule(sx, 88f, sx + (k % 2 == 0 ? 3f : -3f), 62f + (k % 3) * 6f, 2f), k % 2 == 0 ? Leaf : Darker(Leaf, 0.85f), 2f);
            }

            cv.Piece(Union(Ellipse(48f, 50f, 8f, 4.5f), (x, y) => x > 37 && x < 42 && Mathf.Abs(y - 50f) < (42 - x) * 0.9f), C(0.98f, 0.82f, 0.24f), 3f);
            cv.Dot(53, 49, C(0.08f, 0.06f, 0.08f));
            cv.Piece(Union(Ellipse(76f, 70f, 6f, 3.5f), (x, y) => x > 82 && x < 86 && Mathf.Abs(y - 70f) < (x - 82) * 0.9f), C(0.40f, 0.30f, 0.80f), 2.5f);
            cv.Dot(72, 69, C(0.08f, 0.06f, 0.08f));
            foreach ((float bx, float by) in new[] { (88f, 40f), (90f, 54f), (86f, 66f) })
            {
                cv.Piece(Ellipse(bx, by, 1.8f, 1.8f), C(0.86f, 0.97f, 1f), 1.5f);
            }

            cv.Piece(Box(10, 18, 102, 24), C(0.30f, 0.34f, 0.40f), 2f);   // couvercle et lampe

            // ---------- Étagères à bocaux, à droite ----------
            cv.Piece(Box(256, 14, 342, 126), WoodDark, 2.5f);
            cv.Piece(Box(260, 18, 338, 122), C(0.50f, 0.34f, 0.20f), 1.5f);
            Color[] liquids = { C(0.98f, 0.86f, 0.40f), C(0.50f, 0.86f, 0.70f), C(0.92f, 0.48f, 0.40f), C(0.56f, 0.72f, 0.96f), C(0.80f, 0.62f, 0.90f) };
            for (int shelf = 0; shelf < 3; shelf++)
            {
                int top = 20 + shelf * 34;
                cv.Piece(Box(258, top + 28, 340, top + 33), Wood, 1.5f);
                for (int j = 0; j < 5; j++)
                {
                    float jx = 268f + j * 16f;
                    Color liquid = liquids[(j + shelf * 2) % liquids.Length];
                    float h = 10f + ((j + shelf) % 3) * 3f;
                    cv.Piece(Box(jx - 5f, top + 28f - h, jx + 5f, top + 28f), Color.Lerp(liquid, Glass, 0.35f), 3f,
                        (x, y, c) => x < jx - 2f && y < top + 24f ? C(0.95f, 0.99f, 1f) : c);
                    cv.Piece(Box(jx - 4f, top + 26f - h, jx + 4f, top + 29f - h), C(0.70f, 0.52f, 0.34f), 1.5f);   // bouchon
                }
            }

            // Tapis rond au centre, devant le bureau.
            cv.Piece(Ellipse(176f, 196f, 60f, 26f), C(0.22f, 0.44f, 0.68f), 3f,
                (x, y, c) =>
                {
                    float u = (x - 176f) / 60f, v = (y - 196f) / 26f;
                    int ring = (int)(Mathf.Sqrt(u * u + v * v) * 6f);
                    Color band = ring % 2 == 0 ? c : C(0.90f, 0.86f, 0.70f);
                    return (x + y) % 3 == 0 ? Darker(band, 0.9f) : band;
                }, 0.92f);

            return cv.Finish(C(0.14f, 0.18f, 0.22f), 0.4f);
        }

        // ---------- Bureau de la scientifique ----------

        public const int DeskWidth = 128;
        public const int DeskHeight = 80;
        public static readonly Vector2 DeskPivot = new Vector2(0.5f, 2f / DeskHeight);

        /// <summary>Bureau en bois : plateau, tiroirs, microscope, livres, papiers, tasse, loupe.</summary>
        public static Color[,] DeskArt()
        {
            var cv = new VolumeCanvas(DeskWidth, DeskHeight);
            cv.Piece(Box(6, 50, 122, 78), WoodDark, 2.5f);                                           // façade
            cv.Piece(Box(10, 54, 60, 76), Wood, 3f, (x, y, c) => y == 65 ? Darker(c, 0.7f) : c);    // tiroirs
            cv.Piece(Box(68, 54, 118, 76), Wood, 3f, (x, y, c) => y == 65 ? Darker(c, 0.7f) : c);
            foreach ((int hx, int hy) in new[] { (35, 59), (35, 70), (93, 59), (93, 70) })
            {
                cv.Piece(Ellipse(hx, hy, 2.5f, 1.5f), C(0.86f, 0.68f, 0.30f), 1.5f);
            }

            cv.Piece(Box(2, 36, 126, 52), C(0.66f, 0.46f, 0.27f), 4f, (x, y, c) => (x * 3 + y) % 19 == 0 ? Darker(c, 0.85f) : c);   // plateau

            // Microscope à gauche.
            cv.Piece(Ellipse(24f, 40f, 10f, 3f), C(0.22f, 0.24f, 0.28f), 2f);
            cv.Piece(Capsule(28f, 38f, 30f, 14f, 2f), C(0.86f, 0.88f, 0.90f), 2f);
            cv.Piece(Capsule(30f, 16f, 20f, 8f, 3f), C(0.30f, 0.32f, 0.36f), 3f);
            cv.Piece(Capsule(20f, 24f, 20f, 32f, 1.6f), C(0.30f, 0.32f, 0.36f), 1.5f);
            cv.Piece(Box(14, 32, 30, 35), C(0.86f, 0.88f, 0.90f), 1.5f);

            // Papiers étalés, croquis de poisson, et une pile de livres.
            cv.Piece(Box(46, 36, 78, 46), C(0.97f, 0.95f, 0.88f), 1.2f, (x, y, c) => y % 3 == 0 && x > 50 && x < 74 ? Darker(c, 0.82f) : c);
            cv.Piece(Ellipse(58f, 40f, 5f, 2f), C(0.40f, 0.52f, 0.66f), 1f);
            foreach ((int by, Color bc) in new[] { (36, C(0.30f, 0.52f, 0.72f)), (30, C(0.84f, 0.30f, 0.28f)), (24, C(0.40f, 0.62f, 0.36f)) })
            {
                cv.Piece(Box(88, by, 110, by + 6), bc, 2f, (x, y, c) => x > 105 ? C(0.96f, 0.92f, 0.82f) : c);
            }

            // Tasse fumante et loupe.
            cv.Piece(Box(114, 32, 122, 42), White, 3f);
            cv.Piece(Ellipse(118f, 32f, 4f, 1.2f), C(0.42f, 0.26f, 0.14f), 1f);
            cv.Piece(Ellipse(82f, 44f, 4f, 3f), C(0.86f, 0.68f, 0.30f), 2f, (x, y, c) => Mathf.Abs(x - 82f) < 2.5f && Mathf.Abs(y - 44f) < 1.6f ? Glass : c);
            return cv.Finish(C(0.18f, 0.11f, 0.07f));
        }

        // ---------- Socle de pierre ----------

        public const int PedestalWidth = 48;
        public const int PedestalHeight = 64;
        public static readonly Vector2 PedestalPivot = new Vector2(0.5f, 2f / PedestalHeight);

        /// <summary>Colonne de pierre gravée d'une spirale, creux vide au sommet (on y posera l'artefact).</summary>
        public static Color[,] PedestalArt()
        {
            var cv = new VolumeCanvas(PedestalWidth, PedestalHeight);
            cv.Piece(Box(6, 52, 42, 62), Darker(Stone, 0.85f), 3f);                            // base
            cv.Piece(Box(11, 20, 37, 54), Stone, 5f,
                (x, y, c) =>
                {
                    float a = Mathf.Atan2(y - 37f, x - 24f);
                    float r = Vector2.Distance(new Vector2(x, y), new Vector2(24f, 37f));
                    float spiral = Mathf.Repeat(r - a * 1.4f, 4.4f);
                    return r < 9f && spiral < 1f ? Darker(c, 0.7f) : (x * 7 + y * 3) % 17 == 0 ? Darker(c, 0.9f) : c;
                });
            cv.Piece(Box(4, 12, 44, 22), Stone, 3f);                                            // chapiteau
            cv.Piece(Ellipse(24f, 13f, 14f, 4f), C(0.34f, 0.40f, 0.44f), 2f);                 // creux
            cv.Piece(Ellipse(24f, 13f, 9f, 2.2f), C(0.56f, 0.86f, 0.90f), 1.5f);              // lueur turquoise
            return cv.Finish(C(0.16f, 0.16f, 0.18f));
        }

        // ---------- Plante ----------

        public const int PlantWidth = 48;
        public const int PlantHeight = 72;
        public static readonly Vector2 PlantPivot = new Vector2(0.5f, 2f / PlantHeight);

        public static Color[,] PlantArt()
        {
            var cv = new VolumeCanvas(PlantWidth, PlantHeight);
            for (int k = 0; k < 9; k++)
            {
                float a = Mathf.Lerp(200f, 340f, k / 8f) * Mathf.Deg2Rad;
                float len = 18f + (k % 3) * 6f;
                cv.Piece(Union(Capsule(24f, 50f, 24f + Mathf.Cos(a) * len, 50f + Mathf.Sin(a) * len, 1f),
                        Ellipse(24f + Mathf.Cos(a) * len, 50f + Mathf.Sin(a) * len, 5f, 5f)),
                    k % 2 == 0 ? Leaf : Darker(Leaf, 0.85f), 3f, (x, y, c) => (x + y) % 6 == 0 ? Darker(c, 1.12f) : c, 0.85f);
            }

            cv.Piece(Union(Box(12, 48, 36, 70), Ellipse(24f, 48f, 13f, 3f)), C(0.80f, 0.44f, 0.30f), 4f,
                (x, y, c) => y == 54 ? Darker(c, 0.8f) : c);
            return cv.Finish(C(0.12f, 0.14f, 0.10f));
        }

        // ---------- Icône de la minimap (16×16, y depuis le bas) ----------

        public static Color[,] Icon()
        {
            var p = new Color[16, 16];
            PixelArt.Rect(p, 2, 1, 13, 8, White);
            PixelArt.Rect(p, 1, 9, 14, 10, Steel);
            PixelArt.Rect(p, 8, 11, 13, 12, Metal);
            PixelArt.Rect(p, 9, 13, 12, 13, Metal);
            PixelArt.Rect(p, 6, 1, 9, 5, Glass);
            PixelArt.Rect(p, 3, 4, 4, 6, Glass);
            PixelArt.Rect(p, 11, 4, 12, 6, Glass);
            PixelArt.Rect(p, 4, 11, 4, 14, C(0.50f, 0.52f, 0.56f));
            PixelArt.Set(p, 4, 14, C(0.95f, 0.30f, 0.26f));
            PixelArt.Outline(p, C(0.12f, 0.14f, 0.20f));
            return p;
        }
    }
}
