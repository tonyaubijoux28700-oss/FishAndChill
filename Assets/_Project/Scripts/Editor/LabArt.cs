using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

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

        public const int ExteriorWidth = 256;   // bâtiment principal (160 px) + aile des spécimens à droite
        public const int ExteriorHeight = 144;

        /// <summary>Pied de la façade (la porte).</summary>
        public static readonly Vector2 ExteriorPivot = new Vector2(80f / ExteriorWidth, 6f / ExteriorHeight);

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

            // Aile des spécimens, accolée à droite : verrière bombée où luisent les tubes, hublots, cuve et tuyaux.
            SpecimenLabArt.Wing(cv, 160f, 250f);

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

        public const int InteriorWidth = 416;   // 13 cases : la pièce et, à droite, la porte de la salle des spécimens
        public const int InteriorHeight = 256;
        private const int WallBottom = 96;   // limite mur / carrelage (5 cases au-dessus du bas)

        public static Color[,] Interior()
        {
            var cv = new VolumeCanvas(InteriorWidth, InteriorHeight);
            Color plaster = C(0.20f, 0.36f, 0.42f), panel = C(0.36f, 0.24f, 0.15f), plank = C(0.52f, 0.36f, 0.22f), brass = C(0.86f, 0.66f, 0.30f);

            // Mur du fond : enduit bleu des grands fonds, taché par l'humidité, lambris de bois sombre en bas, main courante
            // de laiton entre les deux, plinthe.
            cv.Piece(Box(-1, -1, InteriorWidth + 1, WallBottom), plaster, 1.5f, (x, y, c) =>
            {
                if (y >= 58)
                {
                    Color wood = (x % 24 == 0) ? Darker(panel, 0.6f) : (x % 24 == 1) ? Darker(panel, 1.18f) : panel;
                    return Noise(x / 3f, y / 9f) < 0.3f ? Darker(wood, 0.88f) : wood;
                }

                Color wallShade = Color.Lerp(Darker(c, 0.7f), c, y / 58f);
                return Noise(x / 11f, y / 7f) < 0.25f ? Darker(wallShade, 0.9f) : (x * 5 + y * 3) % 31 == 0 ? Darker(wallShade, 1.1f) : wallShade;
            });
            cv.Piece(Box(-1, 55, InteriorWidth + 1, 59), brass, 1.5f);
            cv.Piece(Box(-1, WallBottom - 5, InteriorWidth + 1, WallBottom + 1), C(0.22f, 0.14f, 0.09f), 1.5f);

            // Tuyau de cuivre au plafond, avec ses colliers, et un câble qui court jusqu'à la porte blindée.
            cv.Piece(Box(-1, 3, InteriorWidth + 1, 9), C(0.74f, 0.46f, 0.26f), 3f, (x, y, c) => x % 48 < 4 ? Darker(c, 0.7f) : c);
            cv.Piece(Tube(Curve(new Vector2(110, 10), new Vector2(240, 22), new Vector2(370, 10), 30), 1.2f, 1.2f), C(0.10f, 0.10f, 0.12f), 1f, null, 0.9f, 0.5f);

            // Parquet de bateau : longues lattes vernies, joints décalés, usé au milieu ; deux flaques de lumière chaude
            // sous les lampes.
            cv.Piece(Box(-1, WallBottom, InteriorWidth + 1, InteriorHeight + 1), plank, 1.2f,
                (x, y, c) =>
                {
                    int row = (y - WallBottom) / 12;
                    int seam = (x + row * 37) % 72;
                    if ((y - WallBottom) % 12 == 0) return Darker(c, 0.62f);
                    if (seam == 0) return Darker(c, 0.7f);
                    Color wood = row % 3 == 0 ? Darker(c, 0.92f) : row % 3 == 1 ? c : Darker(c, 1.06f);
                    if (Noise(x / 14f, y / 3f) < 0.3f) wood = Darker(wood, 0.9f);
                    if ((x + y * 2) % 53 == 0) wood = Darker(wood, 1.15f);
                    foreach (float lx in new[] { 56f, 178f })
                    {
                        float dx = (x - lx) / 70f, dy = (y - 150f) / 40f;
                        if (dx * dx + dy * dy < 1f) wood = Color.Lerp(wood, C(1f, 0.86f, 0.56f), 0.16f);
                    }

                    return wood;
                }, 0.9f, 0.8f);

            // Murs de côté et bas de la pièce, paillasson.
            cv.Piece(Union(Box(-1, -1, 7, InteriorHeight + 1), Box(InteriorWidth - 7, -1, InteriorWidth + 1, InteriorHeight + 1)), C(0.16f, 0.12f, 0.10f), 2f);
            cv.Piece(Box(-1, InteriorHeight - 4, InteriorWidth + 1, InteriorHeight + 1), C(0.16f, 0.12f, 0.10f), 2f);
            cv.Piece(Box(160, InteriorHeight - 10, 192, InteriorHeight - 3), C(0.22f, 0.44f, 0.68f), 2.5f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.85f) : c);

            // Câble noir au sol, de la cuve d'étude à la porte blindée.
            cv.Piece(Tube(Curve(new Vector2(100, 128), new Vector2(240, 240), new Vector2(380, 100), 40), 1.4f, 1.4f), C(0.08f, 0.08f, 0.10f), 1f, null, 0.9f, 0.5f);

            // Papiers et croquis tombés par terre.
            foreach ((float px, float py, float a) in new[] { (60f, 220f, 0.3f), (236f, 228f, -0.2f), (330f, 200f, 0.5f), (344f, 214f, -0.4f) })
            {
                Vector2 cpx = new Vector2(px, py);
                Vector2 u = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), v = new Vector2(-u.y, u.x);
                cv.Piece(Poly(cpx - u * 8 - v * 6, cpx + u * 8 - v * 6, cpx + u * 8 + v * 6, cpx - u * 8 + v * 6), C(0.94f, 0.90f, 0.78f), 1f,
                    (x, y, c) => (y + x / 3) % 4 == 0 ? Darker(c, 0.8f) : c, 0.85f, 0.7f);
            }

            // Grand tableau noir dans un cadre de bois : les légendes esquissées à la craie, flèches, mesures et points
            // d'interrogation, une carte du monde punaisée dans le coin.
            cv.Piece(Box(106, 12, 250, 62), WoodDark, 2.5f);
            cv.Piece(Box(110, 16, 246, 58), C(0.13f, 0.20f, 0.17f), 1.2f, (x, y, c) => Noise(x / 6f, y / 4f) < 0.3f ? C(0.20f, 0.28f, 0.24f) : c);
            Color chalk = C(0.86f, 0.90f, 0.84f);
            void Chalk(Vector2 a, Vector2 control, Vector2 b) => cv.Piece(Tube(Curve(a, control, b, 12), 0.6f, 0.6f), chalk, 0.5f, null, 0.95f, 0.9f);
            // Une murène qui ondule, une pince d'écrevisse, une gueule à crocs, une queue à crochets.
            Chalk(new Vector2(116, 40), new Vector2(128, 24), new Vector2(140, 40));
            Chalk(new Vector2(140, 40), new Vector2(150, 54), new Vector2(160, 36));
            cv.Piece(Ellipse(159f, 35f, 1f, 1f), chalk, 0.5f);
            Chalk(new Vector2(170, 46), new Vector2(172, 28), new Vector2(184, 26));
            Chalk(new Vector2(184, 26), new Vector2(190, 30), new Vector2(182, 36));
            Chalk(new Vector2(170, 46), new Vector2(180, 40), new Vector2(182, 36));
            for (int k = 0; k < 4; k++) Chalk(new Vector2(196 + k * 5, 36), new Vector2(197 + k * 5, 32), new Vector2(196 + k * 5, 29));
            Chalk(new Vector2(194, 28), new Vector2(206, 22), new Vector2(214, 30));
            Chalk(new Vector2(194, 38), new Vector2(206, 44), new Vector2(214, 36));
            Chalk(new Vector2(120, 52), new Vector2(160, 50), new Vector2(200, 52));   // cote, flèche
            Chalk(new Vector2(196, 49), new Vector2(198, 51), new Vector2(200, 52));
            Chalk(new Vector2(196, 55), new Vector2(198, 53), new Vector2(200, 52));
            Chalk(new Vector2(220, 46), new Vector2(232, 40), new Vector2(222, 34));   // point d'interrogation
            cv.Piece(Ellipse(222f, 50f, 1.2f, 1.2f), chalk, 0.5f);
            cv.Piece(Box(110, 57, 246, 61), WoodDark, 1.5f);   // auget à craies
            cv.Piece(Box(150, 55, 158, 58), chalk, 1f);

            // Carte du monde punaisée, un peu de travers, dans le coin du tableau.
            cv.Piece(Poly(new Vector2(226, 14), new Vector2(252, 17), new Vector2(250, 36), new Vector2(224, 33)), C(0.90f, 0.82f, 0.62f), 1.5f,
                (x, y, c) => Noise(x / 3f, y / 3f) < 0.35f ? C(0.56f, 0.74f, 0.78f) : c);
            foreach ((int px, int py) in new[] { (232, 22), (240, 26), (246, 30) })
            {
                cv.Piece(Ellipse(px, py, 1.6f, 1.6f), C(0.92f, 0.24f, 0.22f), 1.2f);
            }

            // Lampe de banquier suspendue au tuyau, au-dessus du tableau : abat-jour vert, ampoule chaude.
            cv.Piece(Capsule(178f, 9f, 178f, 14f, 0.6f), C(0.10f, 0.10f, 0.12f), 0.5f);
            cv.Piece(Poly(new Vector2(170, 20), new Vector2(174, 14), new Vector2(182, 14), new Vector2(186, 20)), C(0.16f, 0.48f, 0.30f), 3f);
            cv.Piece(Ellipse(178f, 21f, 3f, 1.6f), C(1f, 0.92f, 0.66f), 1f);

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
            // Poissons, algues et bulles sont animés en jeu (LabLife) ; ici, un rocher et la pierre de la pompe.
            cv.Piece(Union(Ellipse(40f, 88f, 9f, 6f), Ellipse(50f, 90f, 6f, 4f)), C(0.56f, 0.54f, 0.52f), 4f);
            cv.Piece(Ellipse(87f, 91f, 4f, 2f), C(0.36f, 0.38f, 0.42f), 2f);

            cv.Piece(Box(10, 18, 102, 24), C(0.30f, 0.34f, 0.40f), 2f);   // couvercle et lampe

            // ---------- Porte blindée de la salle des spécimens, au bout à droite ----------
            SpecimenLabArt.SpecimenDoor(cv, 362f, 398f, WallBottom);

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

            // Grand tapis de marin devant le bureau : bordure à vagues, rose des vents au centre.
            cv.Piece(Box(112, 170, 240, 224), C(0.16f, 0.28f, 0.40f), 3f,
                (x, y, c) =>
                {
                    bool border = x < 120 || x > 232 || y < 178 || y > 216;
                    if (border) return ((x + (int)(Mathf.Sin(y * 0.5f) * 2f)) / 4) % 2 == 0 ? C(0.80f, 0.62f, 0.30f) : C(0.56f, 0.20f, 0.16f);
                    float u = (x - 176f) / 50f, v = (y - 197f) / 17f;
                    float r = Mathf.Sqrt(u * u + v * v), a = Mathf.Atan2(v, u);
                    float star = 0.35f + 0.6f * Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2f)), 6f);
                    if (r < star) return Mathf.Cos(a * 4f) > 0f ? C(0.90f, 0.82f, 0.60f) : C(0.80f, 0.62f, 0.30f);
                    if (Mathf.Abs(r - 0.8f) < 0.05f) return C(0.80f, 0.62f, 0.30f);
                    return (x + y) % 3 == 0 ? Darker(c, 0.85f) : c;
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

        // ---------- Vie de l'aquarium d'étude (animée par LabLife) ----------

        /// <summary>Petit poisson de profil, tête à droite (<paramref name="frame"/> 0 ou 1 : la queue bat).</summary>
        public static Color[,] TankFish(Color body, Color fin, int frame, float length = 16f)
        {
            int w = Mathf.CeilToInt(length) + 6, h = 12;
            var cv = new VolumeCanvas(w, h);
            float cx = w * 0.5f + 1f, cy = h * 0.5f, rx = length * 0.36f, ry = length * 0.22f;
            float tail = cx - rx - 0.5f;
            float tilt = frame == 0 ? -1.2f : 1.2f;
            cv.Piece((x, y) => x < tail + 1f && x > tail - 5f && Mathf.Abs(y - cy - tilt * (tail - x) / 5f) < (tail + 1f - x) * 0.75f, fin, 1.5f);
            cv.Piece(Ellipse(cx, cy - ry * 0.9f, rx * 0.45f, ry * 0.45f), fin, 1.5f);   // nageoire du dos
            cv.Piece(Ellipse(cx, cy, rx, ry), body, 3f, (x, y, c) => y > cy + ry * 0.4f ? Color.Lerp(c, Color.white, 0.35f) : c);
            cv.Dot(Mathf.RoundToInt(cx + rx * 0.55f), Mathf.RoundToInt(cy - 1f), C(0.06f, 0.05f, 0.08f));
            return cv.Finish(Darker(body, 0.45f), 0.6f);
        }

        /// <summary>Hauteur des images d'algue, en pixels.</summary>
        public const int SeaweedHeight = 30;

        /// <summary>Touffe d'algue qui ondule (<paramref name="frame"/> 0, 1, 2 : penchée à gauche, droite, à droite).</summary>
        public static Color[,] Seaweed(int frame)
        {
            var cv = new VolumeCanvas(14, SeaweedHeight);
            float bend = (frame - 1) * 2.5f;
            for (int k = 0; k < 2; k++)
            {
                float baseX = 5f + k * 4f, top = 6f + k * 9f;
                var pts = new List<Vector2>();
                for (int s = 0; s <= 5; s++)
                {
                    float u = s / 5f;
                    pts.Add(new Vector2(baseX + bend * u * u + Mathf.Sin(u * 5f + k) * 1f, Mathf.Lerp(SeaweedHeight - 2f, top, u)));
                }

                for (int s = 0; s < pts.Count - 1; s++)
                {
                    Vector2 a = pts[s], b = pts[s + 1];
                    cv.Piece(Capsule(a.x, a.y, b.x, b.y, 1.8f - s * 0.18f), k == 0 ? Leaf : Darker(Leaf, 0.82f), 2f, null, 0.92f);
                }
            }

            return cv.Finish(C(0.10f, 0.26f, 0.14f), 0.5f);
        }

        /// <summary>Bulle de 4 px, claire avec un reflet.</summary>
        public static Color[,] Bubble()
        {
            var p = new Color[4, 4];
            Color rim = C(0.86f, 0.97f, 1f, 0.9f), inside = C(0.70f, 0.90f, 1f, 0.35f);
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    bool corner = (x == 0 || x == 3) && (y == 0 || y == 3);
                    if (!corner) p[x, y] = x == 0 || x == 3 || y == 0 || y == 3 ? rim : inside;
                }
            }

            p[1, 2] = C(1f, 1f, 1f, 1f);
            return p;
        }

        /// <summary>Lueur turquoise du creux du socle (ovale doux, posé par-dessus le socle).</summary>
        public static Color[,] PedestalGlow()
        {
            var p = new Color[28, 14];
            for (int x = 0; x < 28; x++)
            {
                for (int y = 0; y < 14; y++)
                {
                    float u = (x + 0.5f - 14f) / 13f, v = (y + 0.5f - 4f) / 3.2f;
                    float d = u * u + v * v;
                    float up = (y + 0.5f - 4f) / 10f;   // un halo qui monte un peu au-dessus du creux
                    float halo = Mathf.Clamp01(1f - Mathf.Abs(u) * 1.3f) * Mathf.Clamp01(1f - up) * (y >= 4 ? 0.35f : 0f);
                    float a = Mathf.Max(d <= 1f ? 0.8f * (1f - d * 0.6f) : 0f, halo);
                    if (a > 0.04f) p[x, y] = C(0.55f, 0.95f, 0.95f, Mathf.Round(a * 4f) / 4f);
                }
            }

            return p;
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
