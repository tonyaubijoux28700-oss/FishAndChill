using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Décor de l'atelier des artefacts, dans le style en volume, à l'échelle de l'interface (1 pixel = 4 unités) :
    /// l'établi du laboratoire (plateau de bois sombre éclairé par la lampe, tapis de feutre, carnet ouvert où l'atelier pose le
    /// croquis de l'artefact choisi, loupe, pinceau, bocal d'eau de mer), la dalle de pierre gravée où l'on assemble l'artefact, les cases de
    /// velours du plateau à fragments et les fiches de papier épinglées de la liste des légendes.
    /// Positions calées sur la mise en page de <see cref="UI.ArtifactWorkshop"/>. y depuis le haut pendant le dessin.
    /// </summary>
    public static class WorkshopArt
    {
        public const int BenchWidth = 375;    // 1500 unités d'interface
        public const int BenchHeight = 225;   // 900 unités

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Wood = C(0.30f, 0.19f, 0.11f);
        private static readonly Color WoodDark = C(0.17f, 0.10f, 0.06f);
        private static readonly Color WoodLight = C(0.42f, 0.28f, 0.16f);
        private static readonly Color Felt = C(0.10f, 0.20f, 0.22f);
        private static readonly Color Paper = C(0.93f, 0.88f, 0.74f);
        private static readonly Color Ink = C(0.26f, 0.18f, 0.12f);
        private static readonly Color Brass = C(0.84f, 0.64f, 0.30f);

        /// <summary>Unités d'interface (origine au centre, y vers le haut) → pixels du dessin (origine en haut à gauche).</summary>
        private static Vector2 Ui(float x, float y) => new Vector2((x + 750f) / 4f, (450f - y) / 4f);

        public static Color[,] Bench()
        {
            var cv = new VolumeCanvas(BenchWidth, BenchHeight);

            // Plateau en planches de bois sombre, veinées, avec quelques nœuds et des têtes de clous.
            cv.Piece(Box(-1, -1, BenchWidth + 1, BenchHeight + 1), Wood, 1.2f, (x, y, c) =>
            {
                int plank = y / 25;
                Color baseColor = plank % 3 == 0 ? c : plank % 3 == 1 ? Color.Lerp(c, WoodDark, 0.25f) : Color.Lerp(c, WoodLight, 0.18f);
                if (y % 25 == 0) return WoodDark;                                                     // joint
                if (y % 25 == 1) return Darker(baseColor, 1.12f);
                float grain = Mathf.Sin(x * 0.11f + plank * 7f + Mathf.Sin(y * 0.6f + x * 0.02f) * 1.6f);
                if (grain > 0.92f) return Darker(baseColor, 0.78f);
                if (Hash(x / 2, y) < 0.04f) return Darker(baseColor, 0.88f);
                int knotX = (plank * 97) % 340 + 18, knotY = plank * 25 + 12;
                float kd = (x - knotX) * (x - knotX) * 0.15f + (y - knotY) * (y - knotY);
                if (kd < 10f) return kd < 4f ? WoodDark : Darker(baseColor, 0.8f);
                bool nail = (x == 6 || x == BenchWidth - 7) && y % 25 == 12;
                return nail ? C(0.55f, 0.55f, 0.58f) : baseColor;
            }, 1f, 0.9f);

            // Tapis de feutre sous la dalle, bord cousu.
            Vector2 a = Ui(-210, 395), b = Ui(430, -240);
            cv.Piece(Box(a.x, a.y, b.x, b.y), Felt, 2f, (x, y, c) =>
            {
                bool stitch = (Mathf.Abs(x - a.x - 3) < 0.6f || Mathf.Abs(x - b.x + 3) < 0.6f) && y % 4 < 2
                              || (Mathf.Abs(y - a.y - 3) < 0.6f || Mathf.Abs(y - b.y + 3) < 0.6f) && x % 4 < 2;
                return stitch ? C(0.62f, 0.56f, 0.40f) : Hash(x, y) < 0.1f ? Darker(c, 1.15f) : c;
            });

            // Plateau à fragments : une planche à rebord le long du bas.
            Vector2 ta = Ui(-225, -275), tb = Ui(680, -395);
            cv.Piece(Box(ta.x, ta.y, tb.x, tb.y), WoodLight, 3f, (x, y, c) => (x + y / 3) % 9 == 0 ? Darker(c, 0.85f) : c);

            // Carnet ouvert à droite, avec des notes.
            Vector2 n0 = Ui(470, 370), n1 = Ui(715, 70);
            cv.Piece(Box(n0.x - 2, n0.y - 2, n1.x + 2, n1.y + 2), C(0.36f, 0.16f, 0.12f), 2f);               // couverture
            cv.Piece(Box(n0.x, n0.y, n1.x, n1.y), Paper, 2f, (x, y, c) =>
            {
                float mid = (n0.x + n1.x) * 0.5f;
                if (Mathf.Abs(x - mid) < 1f) return Darker(c, 0.75f);                                    // pliure
                if (x > mid + 4 && x < n1.x - 4 && (y - (int)n0.y) % 5 == 0 && Hash(x / 6, y) > 0.3f) return Color.Lerp(c, Ink, 0.55f);   // notes
                return c;
            });
            // La page de gauche reste blanche : l'atelier y pose le croquis de l'artefact choisi (ArtifactWorkshop).

            // Loupe en laiton posée en biais.
            Vector2 lens = Ui(560, 0);
            cv.Piece(Capsule(lens.x + 6, lens.y + 6, lens.x + 20, lens.y + 22, 2.2f), WoodDark, 2f);         // manche
            cv.Piece(Ellipse(lens.x, lens.y, 11, 11), Brass, 3f);
            cv.Piece(Ellipse(lens.x, lens.y, 8.5f, 8.5f), C(0.62f, 0.80f, 0.82f), 3f,
                (x, y, c) => x + y < lens.x + lens.y - 5 && x + y > lens.x + lens.y - 9 ? C(0.95f, 1f, 1f) : c);

            // Pinceau et bocal d'eau de mer avec une petite étoile de mer.
            Vector2 brush = Ui(470, -60);
            cv.Piece(Capsule(brush.x, brush.y, brush.x + 34, brush.y - 6, 1.4f), C(0.66f, 0.40f, 0.22f), 1.5f);
            cv.Piece(Capsule(brush.x - 6, brush.y + 1, brush.x, brush.y, 2.2f), C(0.86f, 0.80f, 0.66f), 2f);
            Vector2 jar = Ui(660, -150);
            cv.Piece(Box(jar.x - 13, jar.y - 22, jar.x + 13, jar.y + 14), C(0.52f, 0.78f, 0.80f), 4f,
                (x, y, c) => x < jar.x - 8 && y < jar.y + 6 ? C(0.86f, 0.98f, 1f) : y < jar.y - 14 ? Color.Lerp(c, Color.white, 0.3f) : c);
            cv.Piece(Box(jar.x - 14, jar.y - 26, jar.x + 14, jar.y - 21), C(0.56f, 0.40f, 0.26f), 2f);    // bouchon
            // Une petite étoile de mer à cinq bras au fond du bocal.
            var arms = new System.Func<float, float, bool>[5];
            for (int k = 0; k < 5; k++)
            {
                float ang = -Mathf.PI / 2f + k * Mathf.PI * 2f / 5f;
                arms[k] = Capsule(jar.x, jar.y + 6, jar.x + Mathf.Cos(ang) * 6f, jar.y + 6 + Mathf.Sin(ang) * 6f, 1.3f);
            }

            cv.Piece(Union(arms), C(0.96f, 0.56f, 0.36f), 1.5f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 1.15f) : c);

            Color[,] p = cv.Finish(C(0.06f, 0.04f, 0.03f), 0.45f);

            // La lumière de la lampe : un halo chaud sur la dalle, les bords de l'établi dans l'ombre.
            Vector2 light = Ui(110, 120);
            for (int x = 0; x < BenchWidth; x++)
            {
                for (int y = 0; y < BenchHeight; y++)
                {
                    float dx = (x - light.x) / 210f, dy = ((BenchHeight - 1 - y) - light.y) / 150f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float k = Mathf.Lerp(1.16f, 0.62f, Mathf.SmoothStep(0f, 1f, d));
                    Color c = p[x, y];
                    p[x, y] = new Color(Mathf.Min(1f, c.r * k * 1.04f), Mathf.Min(1f, c.g * k), Mathf.Min(1f, c.b * k * 0.94f), c.a);
                }
            }

            return p;
        }

        // ---------- La dalle de pierre gravée ----------

        public const int SlabWidth = 140;    // 560 unités
        public const int SlabHeight = 135;   // 540 unités

        public static Color[,] Slab()
        {
            var cv = new VolumeCanvas(SlabWidth, SlabHeight);
            Color stone = C(0.40f, 0.44f, 0.46f), stoneDark = C(0.22f, 0.25f, 0.28f);
            cv.Piece(Union(Box(6, 2, SlabWidth - 6, SlabHeight - 2), Box(2, 6, SlabWidth - 2, SlabHeight - 6),
                    Ellipse(6, 6, 4, 4), Ellipse(SlabWidth - 6, 6, 4, 4), Ellipse(6, SlabHeight - 6, 4, 4), Ellipse(SlabWidth - 6, SlabHeight - 6, 4, 4)),
                stone, 5f, (x, y, c) =>
                {
                    float n = Hash(x / 2, y / 2);
                    if (n < 0.08f) return Darker(c, 0.8f);
                    if (n > 0.94f) return Darker(c, 1.15f);
                    return c;
                });

            // Le creux où repose l'artefact, bordé d'un filet de nacre turquoise.
            cv.Piece(Box(14, 12, SlabWidth - 14, SlabHeight - 12), stoneDark, 3f, (x, y, c) => Hash(x / 2, y / 2) < 0.04f ? Darker(c, 1.15f) : c, 0.7f, 0.6f);
            cv.Piece((x, y) => Box(12, 10, SlabWidth - 12, SlabHeight - 10)(x, y) && !Box(14, 12, SlabWidth - 14, SlabHeight - 12)(x, y),
                C(0.30f, 0.70f, 0.68f), 1f);

            // Spirales gravées aux quatre coins, comme sur le socle du laboratoire.
            foreach ((float sx, float sy) in new[] { (8f, 7f), (SlabWidth - 8f, 7f), (8f, SlabHeight - 7f), (SlabWidth - 8f, SlabHeight - 7f) })
            {
                cv.Piece((x, y) =>
                {
                    float dx = x - sx, dy = y - sy;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Atan2(dy, dx);
                    return r < 5f && Mathf.Repeat(r - a * 0.9f, 2.6f) < 0.9f;
                }, stoneDark, 0.6f, null, 0.95f, 0.6f);
            }

            // Fissure, mousse et petits coraux au pied de la dalle.
            cv.Piece(Tube(Curve(new Vector2(SlabWidth - 30, 3), new Vector2(SlabWidth - 26, 7), new Vector2(SlabWidth - 31, 11), 6), 0.6f, 0.4f), stoneDark, 0.5f);
            for (int k = 0; k < 18; k++)
            {
                float mx = 4 + Hash(k, 3) * 28, my = SlabHeight - 3 - Hash(k, 5) * 6;
                cv.Piece(Ellipse(mx, my, 1.6f, 1.2f), C(0.30f, 0.46f, 0.24f), 1.2f);
            }

            cv.Piece(Tube(Curve(new Vector2(SlabWidth - 12, SlabHeight - 2), new Vector2(SlabWidth - 14, SlabHeight - 10), new Vector2(SlabWidth - 9, SlabHeight - 15), 6), 1.4f, 0.8f), C(0.92f, 0.46f, 0.42f), 1.2f);
            cv.Piece(Tube(Curve(new Vector2(SlabWidth - 13, SlabHeight - 8), new Vector2(SlabWidth - 18, SlabHeight - 11), new Vector2(SlabWidth - 20, SlabHeight - 14), 4), 1f, 0.6f), C(0.92f, 0.46f, 0.42f), 1f);
            return cv.Finish(C(0.06f, 0.07f, 0.08f), 0.5f);
        }

        // ---------- Case de velours du plateau ----------

        public const int CompartmentSize = 30;   // 120 unités

        public static Color[,] Compartment()
        {
            var cv = new VolumeCanvas(CompartmentSize, CompartmentSize);
            cv.Piece(Box(0, 0, CompartmentSize, CompartmentSize), WoodDark, 2.5f);
            cv.Piece(Box(3, 3, CompartmentSize - 3, CompartmentSize - 3), C(0.12f, 0.16f, 0.34f), 4f,
                (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 1.12f) : c, 0.6f, 0.5f);
            return cv.Finish(C(0.04f, 0.03f, 0.02f), 0.4f);
        }

        // ---------- Fiche de papier épinglée ----------

        public const int CardWidth = 83;    // 332 unités
        public const int CardHeight = 29;   // 116 unités

        public static Color[,] Card()
        {
            var cv = new VolumeCanvas(CardWidth, CardHeight);
            cv.Piece((x, y) => x > 1 && x < CardWidth - 1 && y > 1 + (x > CardWidth - 8 ? (x - CardWidth + 8) * 0.25f : 0f) && y < CardHeight - 1,
                Paper, 1.5f, (x, y, c) => y > 6 && (y - 6) % 5 == 0 && x > 24 && x < CardWidth - 6 ? Color.Lerp(c, C(0.60f, 0.72f, 0.82f), 0.35f) : Hash(x, y) < 0.05f ? Darker(c, 0.93f) : c);
            cv.Piece(Ellipse(5, 5, 2.6f, 2.6f), C(0.80f, 0.20f, 0.18f), 2.5f);   // punaise
            return cv.Finish(C(0.30f, 0.24f, 0.16f), 0.35f);
        }
    }
}
