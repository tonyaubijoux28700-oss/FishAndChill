using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Aile des spécimens du laboratoire : la grande salle expérimentale où les légendes vaincues flottent, tête en haut,
    /// dans de grands tubes de verre remplis d'un liquide qui luit. Murs de panneaux d'acier rivetés, tuyaux, câbles et
    /// voyants, sol en caillebotis de métal bordé de bandes jaunes et noires, sas au milieu du bas. Un tube = un fond
    /// (cuve, liquide, socle, couvercle) et un devant (verre, reflets, bagues) ; le spécimen se place entre les deux.
    /// y depuis le haut (VolumeCanvas).
    /// </summary>
    public static class SpecimenLabArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Steel = C(0.30f, 0.36f, 0.42f);
        private static readonly Color SteelDark = C(0.16f, 0.20f, 0.25f);
        private static readonly Color Pipe = C(0.52f, 0.56f, 0.58f);
        private static readonly Color Copper = C(0.72f, 0.46f, 0.26f);
        private static readonly Color Hazard = C(0.96f, 0.76f, 0.16f);
        private static readonly Color Liquid = C(0.30f, 0.86f, 0.70f);
        private static readonly Color GlowGreen = C(0.50f, 1f, 0.78f);

        // ---------- La salle (30×12 cases) ----------

        public const int RoomCells = 30;
        public const int RoomRows = 12;
        public const int RoomWidth = RoomCells * 32;
        public const int RoomHeight = RoomRows * 32;
        /// <summary>Limite mur / sol, en pixels depuis le haut (6 cases de mur).</summary>
        public const int WallBottom = 192;
        public const int Slots = 7;

        /// <summary>Milieu d'un emplacement de tube, en cases depuis la gauche de la salle.</summary>
        public static float SlotX(int slot) => 3f + slot * 4f;

        public static Color[,] Room()
        {
            var cv = new VolumeCanvas(RoomWidth, RoomHeight);

            // Mur : panneaux d'acier rivetés, plus sombres vers le haut.
            cv.Piece(Box(-1, -1, RoomWidth + 1, WallBottom), Steel, 1.5f, (x, y, c) =>
            {
                Color shade = Color.Lerp(SteelDark, c, y / (float)WallBottom);
                if (x % 64 == 0 || y % 54 == 0) return Darker(shade, 0.65f);
                if ((x % 64 == 5 || x % 64 == 59) && y % 54 % 12 == 6) return Color.Lerp(shade, Pipe, 0.8f);   // rivets
                return (x * 3 + y * 7) % 41 == 0 ? Darker(shade, 0.9f) : shade;
            });

            // Deux gros tuyaux horizontaux, colliers, et câbles qui pendent entre les tubes.
            foreach (float py in new[] { 22f, 160f })
            {
                cv.Piece(Box(-1, py - 6f, RoomWidth + 1, py + 6f), Pipe, 4f, (x, y, c) => x % 96 < 6 ? Darker(c, 0.7f) : c);
            }

            cv.Piece(Box(-1, 168f, RoomWidth + 1, 172f), Copper, 1.5f);
            for (int i = 0; i < Slots + 1; i++)
            {
                float cx = (SlotX(i) - 2f) * 32f;
                cv.Piece(Tube(Curve(new Vector2(cx - 10f, 28f), new Vector2(cx, 70f), new Vector2(cx + 10f, 28f), 14), 1.6f, 1.6f), C(0.10f, 0.11f, 0.12f), 1.2f, null, 0.9f, 0.5f);
                // Petit tableau de voyants.
                cv.Piece(Box(cx - 9f, 82f, cx + 9f, 104f), SteelDark, 2f);
                for (int k = 0; k < 3; k++)
                {
                    Color lamp = k == 0 ? C(0.40f, 1f, 0.60f) : k == 1 ? C(1f, 0.74f, 0.20f) : C(0.95f, 0.30f, 0.26f);
                    cv.Piece(Ellipse(cx - 5f + k * 5f, 89f, 1.8f, 1.8f), lamp, 1.2f);
                }

                cv.Piece(Box(cx - 6f, 95f, cx + 6f, 100f), C(0.12f, 0.30f, 0.24f), 1f, (x, y, c) => (x + y) % 3 == 0 ? GlowGreen : c);
            }

            cv.Piece(Box(-1, WallBottom - 6, RoomWidth + 1, WallBottom + 1), C(0.10f, 0.12f, 0.15f), 1.5f);

            // Sol : caillebotis de métal, bande jaune et noire le long du mur et autour du sas.
            cv.Piece(Box(-1, WallBottom, RoomWidth + 1, RoomHeight + 1), C(0.26f, 0.29f, 0.32f), 1.2f, (x, y, c) =>
            {
                int gy = y - WallBottom;
                if (gy < 10) return ((x + gy) / 8) % 2 == 0 ? Hazard : C(0.08f, 0.08f, 0.08f);
                if (x % 32 == 0 || gy % 32 == 0) return Darker(c, 0.6f);
                return (x / 4 + gy / 4) % 2 == 0 ? Darker(c, 0.82f) : c;
            }, 0.9f, 0.8f);

            // Murs de côté, bas de la salle, sas d'entrée (au milieu du bas).
            cv.Piece(Union(Box(-1, -1, 7, RoomHeight + 1), Box(RoomWidth - 7, -1, RoomWidth + 1, RoomHeight + 1)), C(0.12f, 0.14f, 0.17f), 2f);
            cv.Piece(Box(-1, RoomHeight - 4, RoomWidth + 1, RoomHeight + 1), C(0.12f, 0.14f, 0.17f), 2f);
            float mid = RoomWidth * 0.5f;
            cv.Piece(Box(mid - 26f, RoomHeight - 16, mid + 26f, RoomHeight - 3), Hazard, 2f, (x, y, c) => ((x + y) / 6) % 2 == 0 ? c : C(0.08f, 0.08f, 0.08f));
            cv.Piece(Box(mid - 18f, RoomHeight - 13, mid + 18f, RoomHeight - 5), C(0.20f, 0.24f, 0.28f), 1.5f, (x, y, c) => x % 4 == 0 ? Darker(c, 0.7f) : c);

            return cv.Finish(C(0.05f, 0.06f, 0.08f), 0.4f);
        }

        // ---------- Un tube (fond et devant) ----------

        public const int TubeWidth = 112;
        public const int TubeHeight = 212;
        /// <summary>Pied du tube (milieu du socle).</summary>
        public static readonly Vector2 TubePivot = new Vector2(0.5f, 6f / TubeHeight);
        /// <summary>Intérieur du verre, en pixels depuis le haut : où peut flotter le spécimen.</summary>
        public static readonly Rect Inner = new Rect(14f, 42f, 84f, 130f);

        private const float GlassTop = 30f, GlassBottom = 178f, GlassLeft = 10f, GlassRight = 102f;

        public static Color[,] TubeBack()
        {
            var cv = new VolumeCanvas(TubeWidth, TubeHeight);

            // Tuyaux qui montent du couvercle.
            cv.Piece(Box(38, 0, 46, 14), Pipe, 2f);
            cv.Piece(Box(66, 0, 74, 14), Copper, 2f);

            // Fond du verre et liquide qui luit, plus clair en haut, bulles figées au fond.
            cv.Piece(Box(GlassLeft, GlassTop, GlassRight, GlassBottom), C(0.10f, 0.26f, 0.24f), 2f);
            cv.Piece(Box(GlassLeft + 3, GlassTop + 8, GlassRight - 3, GlassBottom), Liquid, 6f, (x, y, c) =>
            {
                Color shade = Color.Lerp(C(0.50f, 0.98f, 0.82f), C(0.08f, 0.40f, 0.36f), (y - GlassTop) / (GlassBottom - GlassTop));
                float edge = Mathf.Abs(x - 56f) / 46f;
                shade = Color.Lerp(shade, Darker(shade, 0.6f), edge * edge);
                return Noise(x / 6f, y / 10f) < 0.2f ? Darker(shade, 0.9f) : shade;
            }, 0.9f, 0.9f);
            cv.Piece(Box(GlassLeft + 3, GlassTop + 6, GlassRight - 3, GlassTop + 10), C(0.80f, 1f, 0.92f), 1f);   // surface

            // Couvercle d'acier, boulons ; socle massif avec plaque.
            cv.Piece(Box(4, 12, 108, 32), Steel, 4f, (x, y, c) => x % 12 == 6 && y == 22 ? Pipe : c);
            cv.Piece(Box(2, 176, 110, 208), Steel, 4f, (x, y, c) => y == 184 ? Darker(c, 0.7f) : x % 12 == 6 && y == 200 ? Pipe : c);
            cv.Piece(Box(36, 188, 76, 202), C(0.78f, 0.62f, 0.30f), 1.5f);   // plaque de laiton
            return cv.Finish(C(0.04f, 0.06f, 0.07f), 0.45f);
        }

        public static Color[,] TubeFront()
        {
            var p = new Color[TubeWidth, TubeHeight];
            int Y(float y) => TubeHeight - 1 - Mathf.RoundToInt(y);

            // Reflets du verre : une large bande et une fine, verticales.
            for (int y = (int)GlassTop + 2; y < GlassBottom; y++)
            {
                for (int x = (int)GlassLeft; x < GlassRight; x++)
                {
                    Color c = new Color(0, 0, 0, 0);
                    if (x >= 18 && x <= 23) c = C(0.92f, 1f, 1f, 0.45f);
                    else if (x == 27) c = C(0.92f, 1f, 1f, 0.3f);
                    else if (x >= 88 && x <= 90) c = C(0.92f, 1f, 1f, 0.2f);
                    else if (x == GlassLeft || x == GlassRight - 1) c = C(0.70f, 0.90f, 0.90f, 0.7f);
                    p[x, Y(y)] = c;
                }
            }

            // Bagues de métal en haut et en bas du verre.
            foreach (float ry in new[] { GlassTop + 1f, GlassBottom - 3f })
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int x = (int)GlassLeft - 2; x < GlassRight + 2; x++)
                    {
                        float shade = y == 0 ? 1.15f : y == 3 ? 0.65f : 0.9f;
                        p[x, Y(ry + y)] = DetailedFish.Shade(Pipe, shade);
                    }
                }
            }

            return p;
        }

        /// <summary>
        /// Le dessin d'une légende (de profil, tête à droite, ou vue de dessus) redressé tête en haut et réduit d'un nombre
        /// entier de fois pour tenir dans le verre du tube (pixels nets, contour gardé).
        /// </summary>
        public static Color[,] Specimen(Color[,] art)
        {
            int w = art.GetLength(0), h = art.GetLength(1);
            var up = new Color[h, w];   // quart de tour : la tête (à droite) passe en haut
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    up[h - 1 - y, x] = art[x, y];
                }
            }

            int factor = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(h / Inner.width, w / Inner.height)));
            return factor > 1 ? PixelArt.Downscale(up, factor) : up;
        }

        // ---------- La porte blindée, au fond du laboratoire ----------

        /// <summary>Porte de la salle des spécimens, peinte dans le mur du fond du laboratoire (x en pixels de la pièce).</summary>
        public static void SpecimenDoor(VolumeCanvas cv, float x0, float x1, float wallBottom)
        {
            float cx = (x0 + x1) * 0.5f;
            cv.Piece(Box(x0 - 4f, 8f, x1 + 4f, wallBottom), Hazard, 2f, (x, y, c) => ((x + y) / 6) % 2 == 0 ? c : C(0.10f, 0.10f, 0.10f));
            cv.Piece(Box(x0, 12f, x1, wallBottom), Steel, 3f, (x, y, c) => (x - (int)x0) % 12 == 0 ? Darker(c, 0.75f) : c);
            cv.Piece(Ellipse(cx, 36f, 10f, 10f), Pipe, 3f);
            cv.Piece(Ellipse(cx, 36f, 7.5f, 7.5f), C(0.16f, 0.56f, 0.46f), 3f, (x, y, c) => x < cx - 2 && y < 34 ? GlowGreen : c);
            cv.Piece(Ring(cx, 66f, 8f, 8f, 2f), Pipe, 2f);   // volant de la porte
            cv.Piece(Union(Capsule(cx - 8f, 66f, cx + 8f, 66f, 1f), Capsule(cx, 58f, cx, 74f, 1f)), Pipe, 1.5f);
            cv.Piece(Ellipse(x1 + 9f, 30f, 2.4f, 2.4f), C(0.40f, 1f, 0.60f), 2f);   // voyant vert
        }

        // ---------- L'aile, vue de dehors ----------

        /// <summary>Aile des spécimens accolée à droite de la façade (x en pixels de la façade, y depuis le haut).</summary>
        public static void Wing(VolumeCanvas cv, float x0, float x1)
        {
            // Bloc de béton clair, soubassement de pierre, bande jaune et noire.
            cv.Piece(Box(x0, 64, x1, 134), C(0.80f, 0.82f, 0.80f), 2.5f, (x, y, c) => (x - (int)x0) % 24 == 0 ? Darker(c, 0.85f) : (x * 5 + y * 3) % 29 == 0 ? Darker(c, 0.94f) : c);
            cv.Piece(Box(x0 - 2, 128, x1 + 2, 138), C(0.62f, 0.60f, 0.56f), 2.5f, (x, y, c) => x % 16 == 0 ? Darker(c, 0.75f) : c);
            cv.Piece(Box(x0, 122, x1, 128), Hazard, 1.5f, (x, y, c) => ((x + y) / 5) % 2 == 0 ? c : C(0.12f, 0.12f, 0.12f));

            // Grande verrière bombée sur le toit : on devine les tubes qui luisent dessous.
            cv.Piece((x, y) => Ellipse((x0 + x1) * 0.5f, 66f, (x1 - x0) * 0.5f - 2f, 30f)(x, y) && y < 66, C(0.40f, 0.70f, 0.70f), 8f, (x, y, c) =>
            {
                if ((x - (int)x0) % 16 == 0 || y % 10 == 0) return C(0.40f, 0.44f, 0.46f);   // armature
                float glow = Mathf.Repeat((x - x0) / 26f, 1f);
                return glow > 0.3f && glow < 0.6f ? Color.Lerp(c, GlowGreen, 0.5f) : c;
            });
            cv.Piece(Box(x0 - 2, 62, x1 + 2, 68), C(0.44f, 0.46f, 0.50f), 2f);

            // Bandeau de hublots lumineux où l'on voit les tubes.
            for (int k = 0; k < 3; k++)
            {
                float hx = x0 + 18f + k * ((x1 - x0 - 36f) / 2f);
                cv.Piece(Box(hx - 9f, 78, hx + 9f, 112), C(0.34f, 0.36f, 0.40f), 2f);
                cv.Piece(Box(hx - 7f, 80, hx + 7f, 110), C(0.12f, 0.40f, 0.36f), 2f, (x, y, c) =>
                {
                    if (Mathf.Abs(x - hx) < 4f && y > 84 && y < 108) return Color.Lerp(Liquid, GlowGreen, (108 - y) / 30f);
                    return x < hx - 4 && y < 92 ? C(0.80f, 1f, 0.94f) : c;
                });
            }

            // Cuve extérieure et tuyaux qui relient au bâtiment principal.
            cv.Piece(Capsule(x1 - 10f, 96f, x1 - 10f, 120f, 9f), C(0.70f, 0.72f, 0.74f), 6f, (x, y, c) => y % 8 == 0 ? Darker(c, 0.8f) : c);
            cv.Piece(Box(x0 - 8, 74, x0 + 4, 80), Pipe, 2f);
            cv.Piece(Box(x0 - 8, 100, x0 + 4, 104), Copper, 2f);
            cv.Piece(Capsule(x1 - 24f, 40f, x1 - 24f, 18f, 2.5f), Pipe, 2f);   // cheminée d'aération
            cv.Piece(Ellipse(x1 - 24f, 16f, 6f, 3f), C(0.40f, 0.42f, 0.46f), 3f);
        }
    }
}
