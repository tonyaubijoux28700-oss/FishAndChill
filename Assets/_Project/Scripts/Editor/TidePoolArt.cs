using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pêche à pied à l'épuisette (dessins en volume, vus de dessus) : l'épuisette, le coin à bulles, la flaque rapprochée
    /// de Mahana et la bestiole sous l'eau (une seule forme pour toutes les espèces). Dessins originaux.
    /// </summary>
    public static class TidePoolArt
    {
        private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        private static float Hash(int x, int y, int seed = 0)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }

        // ---------- L'épuisette ----------

        /// <summary>Icône 32×32 : manche en bois en diagonale, cercle en laiton, filet vert à mailles.</summary>
        public static Color[,] Net()
        {
            var v = new VolumeCanvas(32, 32);
            v.Piece(Capsule(27f, 29f, 15f, 17f, 1.7f), C(150, 102, 58), 1.5f,
                (x, y, c) => (x + y) % 5 == 0 ? Darker(c, 0.85f) : c);
            v.Piece(Capsule(26f, 28f, 23.5f, 25.5f, 2.1f), C(70, 52, 40), 1.6f);   // poignée gainée
            DrawNetHead(v, 11f, 11f, 9.5f);
            return v.Finish(C(40, 30, 26));
        }

        /// <summary>Tête d'épuisette : la poche en filet (mailles) puis le cercle en laiton par-dessus.</summary>
        private static void DrawNetHead(VolumeCanvas v, float cx, float cy, float r)
        {
            v.Piece(Ellipse(cx, cy, r - 0.6f, r - 0.6f), C(70, 140, 110), 2.5f,
                (x, y, c) => ((x + y) % 3 == 0 || (x - y + 99) % 3 == 0) ? C(196, 226, 200) : new Color(0f, 0f, 0f, 0f), 0.9f);
            v.Piece((x, y) => { float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)); return d <= r && d >= r - 1.8f; },
                C(214, 170, 74), 1f);
        }

        /// <summary>L'épuisette vue de dessus dans la flaque (40×40) : le cercle dans l'eau, le manche qui part vers la rive.</summary>
        public static Color[,] NetInWater()
        {
            var v = new VolumeCanvas(40, 40);
            v.Piece(Capsule(24f, 24f, 39f, 39f, 1.6f), C(150, 102, 58), 1.4f);
            DrawNetHead(v, 16f, 16f, 13f);
            return v.Finish(C(40, 30, 26));
        }

        // ---------- Le coin à bulles ----------

        /// <summary>Grappe de bulles plus visible (32×32, 3 images) : bulles qui montent et éclatent, sable soulevé dessous.</summary>
        public static Color[,] Bubbles(int frame)
        {
            var p = new Color[32, 32];
            // Nuage de sable soulevé, très doux.
            for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(16f, 13f)) / 11f;
                if (d < 1f && Hash(x, y, frame) > 0.35f) p[x, y] = new Color(0.86f, 0.80f, 0.62f, 0.28f * (1f - d));
            }

            void Bubble(float bx, float by, float r)
            {
                for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(bx, by));
                    if (d > r) continue;
                    bool rim = d > r - 1.1f;
                    bool shine = x + 0.5f < bx - r * 0.2f && y + 0.5f > by + r * 0.2f && d < r - 0.8f && d > r * 0.35f;
                    p[x, y] = rim ? new Color(0.92f, 0.98f, 1f, 0.95f) : shine ? new Color(1f, 1f, 1f, 0.9f) : new Color(0.75f, 0.92f, 0.98f, 0.35f);
                }
            }

            float t = frame / 3f;
            Bubble(11f, 10f + t * 9f, 3.2f);
            Bubble(19f, 6f + ((t + 0.4f) % 1f) * 12f, 2.4f);
            Bubble(15f, 17f + t * 6f, 1.8f);
            Bubble(22f, 15f + ((t + 0.7f) % 1f) * 8f, 1.5f);
            if (frame == 2) Bubble(9f, 24f, 1.2f);
            return p;
        }

        // ---------- La flaque de Mahana ----------

        /// <summary>
        /// Flaque rapprochée de Mahana (320×192, vue de dessus) : sable mouillé ridé, rochers de lave, débris de corail,
        /// herbiers et algues, eau turquoise par-dessus avec reflets de soleil.
        /// </summary>
        public static Color[,] MahanaPool()
        {
            const int w = 320, h = 192;
            var v = new VolumeCanvas(w, h);
            Color sand = C(222, 204, 160);
            v.Piece(Box(-1f, -1f, w + 1f, h + 1f), sand, 1f, (x, y, c) =>
            {
                float ripple = Mathf.Sin(x * 0.18f + Mathf.Sin(y * 0.07f) * 2.4f);
                Color s = ripple > 0.75f ? Darker(c, 1.08f) : ripple < -0.8f ? Darker(c, 0.92f) : c;
                float n = Hash(x, y);
                return n > 0.97f ? C(250, 240, 220) : n < 0.03f ? C(170, 150, 118) : s;
            }, 1f, 0.9f);

            // Herbiers : touffes de lames vertes.
            void Seagrass(float gx, float gy, int blades, int seed)
            {
                for (int i = 0; i < blades; i++)
                {
                    float a = -1.9f + i * 0.5f + Hash(i, seed) * 0.4f;
                    float len = 14f + Hash(seed, i) * 10f;
                    v.Piece(Capsule(gx, gy, gx + Mathf.Cos(a) * len, gy + Mathf.Sin(a) * len * 0.8f, 1.6f),
                        i % 2 == 0 ? C(70, 140, 64) : C(96, 160, 72), 1.2f, null, 0.85f);
                }
            }

            Seagrass(40f, 150f, 7, 1);
            Seagrass(270f, 40f, 6, 2);
            Seagrass(160f, 176f, 5, 3);

            // Débris de corail blanchi et morceaux roses.
            for (int i = 0; i < 14; i++)
            {
                float rx = 10f + Hash(i, 7) * 300f, ry = 10f + Hash(7, i) * 172f;
                float a = Hash(i, 9) * 3f;
                v.Piece(Capsule(rx, ry, rx + Mathf.Cos(a) *  9f, ry + Mathf.Sin(a) * 9f, 2.4f),
                    i % 3 == 0 ? C(236, 150, 140) : C(226, 214, 190), 1.3f,
                    (x, y, c) => Hash(x, y, 3) > 0.8f ? Darker(c, 0.85f) : c, 0.85f);
            }

            // Rochers de lave, ronds et grêlés.
            void Rock(float rx, float ry, float rw, float rh)
            {
                v.Piece(Ellipse(rx, ry, rw, rh), C(132, 116, 104), Mathf.Min(rw, rh) * 0.9f,
                    (x, y, c) => Hash(x, y, 5) > 0.88f ? Darker(c, 0.7f) : Hash(x, y, 6) > 0.93f ? C(120, 150, 90) : c, 0.6f);
            }

            Rock(70f, 55f, 26f, 20f);
            Rock(240f, 140f, 34f, 24f);
            Rock(258f, 125f, 16f, 12f);
            Rock(140f, 40f, 15f, 11f);
            Rock(110f, 160f, 18f, 13f);

            // Algues brunes en coussins (on peut s'y cacher).
            void Algae(float ax, float ay, float r, int seed)
            {
                for (int i = 0; i < 9; i++)
                {
                    float a = i * 0.7f + seed, d = r * (0.3f + 0.6f * Hash(i, seed));
                    v.Piece(Ellipse(ax + Mathf.Cos(a) * d, ay + Mathf.Sin(a) * d * 0.8f, 6f, 5f),
                        i % 2 == 0 ? C(128, 120, 52) : C(150, 138, 60), 3f, null, 0.8f);
                }
            }

            Algae(190f, 90f, 14f, 1);
            Algae(30f, 30f, 12f, 2);
            Algae(290f, 170f, 10f, 3);

            Color[,] p = v.Finish(C(52, 46, 44), 0.35f);

            // L'eau par-dessus : turquoise, plus profonde au centre, reflets de soleil qui serpentent.
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                float depth = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(x / (float)w, y / (float)h), new Vector2(0.5f, 0.5f)) * 1.6f);
                Color water = Color.Lerp(C(64, 192, 196), C(28, 136, 168), depth);
                Color c = Color.Lerp(p[x, y], water, 0.26f + depth * 0.22f);
                float caustic = Mathf.Abs(Mathf.Sin(x * 0.11f + Mathf.Sin(y * 0.09f) * 2.2f) + Mathf.Sin(y * 0.13f + Mathf.Sin(x * 0.05f) * 1.8f));
                if (caustic < 0.06f) c = Color.Lerp(c, Color.white, 0.22f);
                c.a = 1f;
                p[x, y] = c;
            }

            return p;
        }

        // ---------- La bestiole dans la flaque (une seule forme pour toutes les espèces) ----------

        /// <summary>
        /// Une bestiole sous l'eau (24×24, 3 images) : petite ombre sombre et ovale, cerclée d'un liseré clair qui scintille
        /// (on la distingue des rochers et des algues), avec un petit nuage de sable qui se soulève quand elle bouge
        /// (images 1 et 2). L'espèce exacte se découvre une fois dans l'épuisette.
        /// </summary>
        public static Color[,] Critter(int frame)
        {
            const int size = 24;
            var p = new Color[size, size];
            const float cx = 12f, cy = 12f, rx = 6.5f, ry = 4.5f;
            var shade = new Color(0.04f, 0.13f, 0.16f);
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                float d = Mathf.Sqrt(u * u + v * v);
                if (d <= 1f)
                {
                    p[x, y] = new Color(shade.r, shade.g, shade.b, d > 0.8f ? 0.7f : 0.85f);
                }
                else if (d <= 1.5f && Mathf.Abs(v) < 0.6f && Mathf.Abs(u) > 0.9f && ((Mathf.RoundToInt(y + 0.5f - cy) + frame + (u > 0f ? 1 : 0)) % 2 == 0))
                {
                    p[x, y] = new Color(shade.r, shade.g, shade.b, 0.7f);   // petites pattes qui bougent d'une image à l'autre
                }
            }

            // Un éclat de lumière sur le dos, qui se déplace : ça scintille, ce n'est pas un caillou.
            int gx = Mathf.RoundToInt(cx - 2f + frame * 1.5f), gy = Mathf.RoundToInt(cy + 1f - (frame % 2));
            p[gx, gy] = new Color(0.92f, 1f, 1f, 1f);
            p[gx + 1, gy] = new Color(0.92f, 1f, 1f, 0.7f);
            p[gx, gy - 1] = new Color(0.92f, 1f, 1f, 0.5f);

            // Nuage de sable soulevé derrière elle quand elle bouge.
            if (frame > 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    int sx = Mathf.RoundToInt(cx - rx - 2f - Hash(i, frame) * 4f), sy = Mathf.RoundToInt(cy - 2f + Hash(frame, i) * 5f);
                    if (sx >= 0 && sy >= 0 && sx < size && sy < size) p[sx, sy] = new Color(0.94f, 0.88f, 0.70f, 0.8f);
                }
            }

            return p;
        }
    }
}
