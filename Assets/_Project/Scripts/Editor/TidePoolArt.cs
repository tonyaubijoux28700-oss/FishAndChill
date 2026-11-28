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
        public static Color[,] Net() => Landing(32, 12f, 12f, 10f, 1.4f);

        /// <summary>
        /// L'épuisette vue de dessus dans la flaque (60×60) : cerceau de 14 px centré au pixel (18, 18) depuis le haut, la
        /// fourche, puis le long manche en bambou qui part vers la rive (coin bas droit).
        /// </summary>
        public static Color[,] NetInWater() => Landing(60, 18f, 18f, 14f, 1.3f);

        /// <summary>
        /// Une vraie épuisette (et pas une loupe) : cerceau fin en métal sombre, filet à mailles en losange où l'on voit l'eau
        /// (la poche s'assombrit vers le fond), fourche en Y qui tient le cerceau, long manche en bambou à nœuds.
        /// Coordonnées depuis le haut à gauche ; le manche file vers le coin bas droit.
        /// </summary>
        private static Color[,] Landing(int size, float cx, float cy, float r, float pole)
        {
            var v = new VolumeCanvas(size, size);
            Color bamboo = C(196, 160, 96);
            Color metal = C(70, 84, 78);

            // La fourche : deux bras qui partent du cerceau et se rejoignent sur le manche.
            float neck = r + r * 0.35f;
            var joint = new Vector2(cx + neck * 0.7071f, cy + neck * 0.7071f);
            foreach (float a in new[] { 10f, 80f })
            {
                float rad = a * Mathf.Deg2Rad;
                v.Piece(Capsule(cx + Mathf.Cos(rad) * r, cy + Mathf.Sin(rad) * r, joint.x, joint.y, pole * 0.7f), metal, 1f);
            }

            // Le manche en bambou, nœuds réguliers, une virole sombre au départ.
            v.Piece(Capsule(joint.x, joint.y, size - 1f, size - 1f, pole), bamboo, 1.4f,
                (x, y, c) => ((int)(x + y) % 9 == 0) ? Darker(c, 0.72f) : c);
            v.Piece(Capsule(joint.x - 0.5f, joint.y - 0.5f, joint.x + 2f, joint.y + 2f, pole + 0.4f), metal, 1.2f);

            // Le cerceau en D, fin : un arc côté manche, un bord droit devant (il racle le sable, comme un haveneau).
            float flat = r * 0.62f;
            float Edge(float x, float y)
            {
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float front = -(dx + dy) * 0.7071f;   // vers le haut gauche, à l'opposé du manche
                return Mathf.Max(d - r, front - flat);   // < 0 dedans
            }

            v.Piece((x, y) => { float e = Edge(x, y); return e <= 0f && e >= -1.4f; }, metal, 1f);
            Color[,] p = v.Finish(C(30, 36, 34), 0.5f);

            // Le filet par-dessus, sans contour : mailles en losange, nœuds plus clairs, eau visible entre les fils ;
            // la poche pend dans l'eau : ses mailles se resserrent et s'assombrissent vers le fond.
            var pocket = new Vector2(cx + r * 0.12f, cy + r * 0.12f);
            for (int x = 0; x < size; x++)
            {
                for (int yTop = 0; yTop < size; yTop++)
                {
                    float px = x + 0.5f, py = yTop + 0.5f;
                    float e = Edge(px, py);
                    if (e >= -1.4f) continue;
                    int yb = size - 1 - yTop;
                    float depth = Mathf.Clamp01(-e / (r * 0.8f));   // 0 au cerceau, 1 au fond de la poche
                    var q = (new Vector2(px, py) - pocket) * (1f + 0.9f * depth);   // mailles plus serrées au fond
                    int u = Mathf.FloorToInt(q.x + q.y + 400f), w = Mathf.FloorToInt(q.x - q.y + 400f);
                    bool a = u % 5 == 0, b = w % 5 == 0;
                    if (a || b)
                    {
                        Color thread = Color.Lerp(C(232, 228, 206), C(150, 160, 150), depth * 0.8f);
                        thread.a = Mathf.Lerp(0.9f, 0.45f, depth);
                        if (a && b) thread = new Color(1f, 1f, 0.94f, thread.a + 0.1f);   // les nœuds
                        p[x, yb] = thread;
                    }
                    else
                    {
                        p[x, yb] = new Color(0.05f, 0.12f, 0.12f, 0.08f + depth * 0.22f);   // l'ombre de la poche dans l'eau
                    }
                }
            }

            return p;
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

        // ---------- Les flaques (une par zone, chacune son décor et sa disposition) ----------

        private const int PoolW = 320, PoolH = 192;

        /// <summary>Les zones qui ont leur flaque (noms des assets de zone).</summary>
        public static readonly string[] PoolZones =
        {
            "IleMahana", "LacAnnecy", "FleuveAmazone", "BaieJaponaise", "GrandeBarriere", "Maldives", "CoteDesSquelettes", "FjordsNorvege", "BaieHaLong",
        };

        public static Color[,] MahanaPool() => Pool("IleMahana");

        /// <summary>
        /// Les cachettes de la flaque d'une zone (rochers, racines, coraux…), en pixels de la flaque, y depuis le BAS :
        /// c'est là que crabes et crevettes courent se mettre à l'abri.
        /// </summary>
        public static Vector2[] HideSpots(string zone)
        {
            Vector2[] top;
            switch (zone)
            {
                case "LacAnnecy": top = new[] { new Vector2(62f, 128f), new Vector2(206f, 52f), new Vector2(222f, 136f), new Vector2(278f, 36f) }; break;
                case "FleuveAmazone": top = new[] { new Vector2(100f, 62f), new Vector2(160f, 138f), new Vector2(238f, 112f), new Vector2(62f, 160f) }; break;
                case "BaieJaponaise": top = new[] { new Vector2(80f, 60f), new Vector2(242f, 138f), new Vector2(44f, 150f), new Vector2(170f, 92f) }; break;
                case "GrandeBarriere": top = new[] { new Vector2(70f, 64f), new Vector2(240f, 128f), new Vector2(156f, 40f), new Vector2(104f, 156f) }; break;
                case "Maldives": top = new[] { new Vector2(96f, 74f), new Vector2(250f, 148f), new Vector2(168f, 114f), new Vector2(40f, 160f) }; break;
                case "CoteDesSquelettes": top = new[] { new Vector2(252f, 60f), new Vector2(238f, 150f), new Vector2(60f, 62f), new Vector2(46f, 168f) }; break;
                case "FjordsNorvege": top = new[] { new Vector2(80f, 70f), new Vector2(232f, 132f), new Vector2(170f, 40f), new Vector2(60f, 160f) }; break;
                case "BaieHaLong": top = new[] { new Vector2(46f, 46f), new Vector2(206f, 62f), new Vector2(124f, 150f), new Vector2(276f, 160f) }; break;
                default: top = new[] { new Vector2(70f, 55f), new Vector2(240f, 140f), new Vector2(258f, 125f), new Vector2(140f, 40f), new Vector2(110f, 160f) }; break;
            }

            var spots = new Vector2[top.Length];
            for (int i = 0; i < top.Length; i++) spots[i] = new Vector2(top[i].x, PoolH - top[i].y);
            return spots;
        }

        /// <summary>Flaque rapprochée d'une zone (320×192, vue de dessus) : fond, décor, eau par-dessus, ce qui flotte en surface.</summary>
        public static Color[,] Pool(string zone)
        {
            switch (zone)
            {
                case "LacAnnecy": return AnnecyPool();
                case "FleuveAmazone": return AmazonPool();
                case "BaieJaponaise": return IzuPool();
                case "GrandeBarriere": return ReefPool();
                case "Maldives": return AtollPool();
                case "CoteDesSquelettes": return SkeletonPool();
                case "FjordsNorvege": return FjordPool();
                case "BaieHaLong": return HaLongPool();
                default: return IslandPool();
            }
        }

        // --- Briques communes ---

        private static void Ground(VolumeCanvas v, Color sand, Color light, Color dark, float ripples)
        {
            v.Piece(Box(-1f, -1f, PoolW + 1f, PoolH + 1f), sand, 1f, (x, y, c) =>
            {
                float ripple = Mathf.Sin(x * 0.18f + Mathf.Sin(y * 0.07f) * 2.4f) * ripples;
                Color s = ripple > 0.75f ? Darker(c, 1.08f) : ripple < -0.8f ? Darker(c, 0.92f) : c;
                float n = Hash(x, y);
                return n > 0.97f ? light : n < 0.03f ? dark : s;
            }, 1f, 0.9f);
        }

        private static void Boulder(VolumeCanvas v, float x, float y, float w, float h, Color color, Color spot, float spotRate = 0.07f)
            => v.Piece(Ellipse(x, y, w, h), color, Mathf.Min(w, h) * 0.9f,
                (px, py, c) => Hash(px, py, 5) > 0.88f ? Darker(c, 0.72f) : Hash(px, py, 6) > 1f - spotRate ? spot : c, 0.6f);

        private static void Pebbles(VolumeCanvas v, int count, float min, float max, Color[] colors, int seed)
        {
            for (int i = 0; i < count; i++)
            {
                float px = 3f + Hash(i, seed) * 314f, py = 3f + Hash(seed, i) * 186f, r = min + Hash(i, seed + 1) * (max - min);
                v.Piece(Ellipse(px, py, r * (1f + Hash(i, seed + 2) * 0.4f), r), colors[i % colors.Length], r, null, 0.82f);
            }
        }

        private static void Grass(VolumeCanvas v, float gx, float gy, int blades, float length, Color a, Color b, int seed)
        {
            for (int i = 0; i < blades; i++)
            {
                float ang = -1.9f + i * 0.5f + Hash(i, seed) * 0.4f;
                float len = length + Hash(seed, i) * 10f;
                v.Piece(Capsule(gx, gy, gx + Mathf.Cos(ang) * len, gy + Mathf.Sin(ang) * len * 0.8f, 1.6f), i % 2 == 0 ? a : b, 1.2f, null, 0.85f);
            }
        }

        /// <summary>Un trait épais qui suit des points (racine, côte de baleine, branche, ruban de varech).</summary>
        private static void Curve(VolumeCanvas v, Vector2[] points, float r0, float r1, Color color, System.Func<int, int, Color, Color> texture = null)
        {
            var parts = new System.Func<float, float, bool>[points.Length - 1];
            for (int i = 0; i < parts.Length; i++)
            {
                float r = Mathf.Lerp(r0, r1, i / (float)Mathf.Max(1, parts.Length - 1));
                parts[i] = Capsule(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, r);
            }

            v.Piece(Union(parts), color, Mathf.Max(r0, r1) * 0.9f, texture, 0.6f);
        }

        private static Vector2[] Wave(float x0, float y0, float x1, float y1, float amplitude, float waves, int steps = 10)
        {
            var points = new Vector2[steps + 1];
            var n = new Vector2(-(y1 - y0), x1 - x0).normalized;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                points[i] = new Vector2(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t)) + n * Mathf.Sin(t * waves * Mathf.PI * 2f) * amplitude;
            }

            return points;
        }

        private static System.Func<float, float, bool> Hexagon(float cx, float cy, float r)
            => (x, y) => { float dx = Mathf.Abs(x - cx), dy = Mathf.Abs(y - cy); return dx <= r * 0.866f && dy + dx * 0.577f <= r; };

        /// <summary>L'eau par-dessus le fond : plus profonde au centre, reflets de soleil qui serpentent.</summary>
        private static Color[,] Water(Color[,] p, Color shallow, Color deep, float tint, float tintDeep, float caustics)
        {
            for (int x = 0; x < PoolW; x++)
            for (int y = 0; y < PoolH; y++)
            {
                float depth = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(x / (float)PoolW, y / (float)PoolH), new Vector2(0.5f, 0.5f)) * 1.6f);
                Color c = Color.Lerp(p[x, y], Color.Lerp(shallow, deep, depth), tint + depth * tintDeep);
                float caustic = Mathf.Abs(Mathf.Sin(x * 0.11f + Mathf.Sin(y * 0.09f) * 2.2f) + Mathf.Sin(y * 0.13f + Mathf.Sin(x * 0.05f) * 1.8f));
                if (caustic < 0.06f) c = Color.Lerp(c, Color.white, caustics);
                c.a = 1f;
                p[x, y] = c;
            }

            return p;
        }

        /// <summary>Ce qui flotte à la surface (nénuphars, pétales…), posé par-dessus l'eau sans être teinté.</summary>
        private static Color[,] Float(Color[,] p, VolumeCanvas surface, Color outline)
        {
            Color[,] s = surface.Finish(outline, 0.45f);
            for (int x = 0; x < PoolW; x++)
            for (int y = 0; y < PoolH; y++)
            {
                if (s[x, y].a > 0f) p[x, y] = Color.Lerp(p[x, y], s[x, y], s[x, y].a);
            }

            return p;
        }

        // --- Île Mahana : lagon, sable ridé, rochers de lave, corail, herbiers ---

        private static Color[,] IslandPool()
        {
            var v = new VolumeCanvas(PoolW, PoolH);
            Ground(v, C(222, 204, 160), C(250, 240, 220), C(170, 150, 118), 1f);
            Grass(v, 40f, 150f, 7, 14f, C(70, 140, 64), C(96, 160, 72), 1);
            Grass(v, 270f, 40f, 6, 14f, C(70, 140, 64), C(96, 160, 72), 2);
            Grass(v, 160f, 176f, 5, 14f, C(70, 140, 64), C(96, 160, 72), 3);
            for (int i = 0; i < 14; i++)
            {
                float rx = 10f + Hash(i, 7) * 300f, ry = 10f + Hash(7, i) * 172f, a = Hash(i, 9) * 3f;
                v.Piece(Capsule(rx, ry, rx + Mathf.Cos(a) * 9f, ry + Mathf.Sin(a) * 9f, 2.4f), i % 3 == 0 ? C(236, 150, 140) : C(226, 214, 190), 1.3f,
                    (x, y, c) => Hash(x, y, 3) > 0.8f ? Darker(c, 0.85f) : c, 0.85f);
            }

            Color lava = C(132, 116, 104), moss = C(120, 150, 90);
            Boulder(v, 70f, 55f, 26f, 20f, lava, moss);
            Boulder(v, 240f, 140f, 34f, 24f, lava, moss);
            Boulder(v, 258f, 125f, 16f, 12f, lava, moss);
            Boulder(v, 140f, 40f, 15f, 11f, lava, moss);
            Boulder(v, 110f, 160f, 18f, 13f, lava, moss);
            foreach (Vector3 a in new[] { new Vector3(190f, 90f, 14f), new Vector3(30f, 30f, 12f), new Vector3(290f, 170f, 10f) })
            {
                for (int i = 0; i < 9; i++)
                {
                    float ang = i * 0.7f + a.z, d = a.z * (0.3f + 0.6f * Hash(i, (int)a.z));
                    v.Piece(Ellipse(a.x + Mathf.Cos(ang) * d, a.y + Mathf.Sin(ang) * d * 0.8f, 6f, 5f), i % 2 == 0 ? C(128, 120, 52) : C(150, 138, 60), 3f, null, 0.8f);
                }
            }

            return Water(v.Finish(C(52, 46, 44), 0.35f), C(64, 192, 196), C(28, 136, 168), 0.26f, 0.22f, 0.22f);
        }

        // --- Lac d'Annecy : eau claire, lit de galets, deux grosses pierres, branche noyée, characées, nénuphars ---

        private static Color[,] AnnecyPool()
        {
            var v = new VolumeCanvas(PoolW, PoolH);
            Ground(v, C(198, 190, 168), C(226, 220, 204), C(150, 140, 120), 0.2f);
            Pebbles(v, 140, 1.6f, 5.5f, new[] { C(170, 166, 160), C(196, 180, 150), C(130, 126, 122), C(210, 206, 198), C(176, 150, 120) }, 31);
            // Characées : tapis de petites tiges vertes en étoile.
            for (int i = 0; i < 26; i++)
            {
                float cx = 14f + Hash(i, 41) * 90f, cy = 10f + Hash(41, i) * 60f;
                for (int b = 0; b < 4; b++)
                {
                    float a = b * 1.57f + Hash(i, b) * 0.6f;
                    v.Piece(Capsule(cx, cy, cx + Mathf.Cos(a) * 6f, cy + Mathf.Sin(a) * 6f, 0.9f), b % 2 == 0 ? C(96, 150, 70) : C(120, 170, 86), 0.8f, null, 0.9f);
                }
            }

            Color stone = C(184, 180, 172), moss = C(90, 130, 60);
            Boulder(v, 62f, 128f, 24f, 16f, stone, moss, 0.12f);
            Boulder(v, 206f, 52f, 20f, 14f, stone, moss, 0.12f);
            // La branche noyée, en travers, avec ses rameaux.
            Color wood = C(104, 80, 56);
            Curve(v, Wave(150f, 160f, 300f, 112f, 4f, 0.7f), 4.5f, 3f, wood, (x, y, c) => (x + y) % 6 == 0 ? Darker(c, 0.85f) : c);
            Curve(v, new[] { new Vector2(200f, 144f), new Vector2(214f, 160f), new Vector2(222f, 176f) }, 1.8f, 1.2f, wood);
            Curve(v, new[] { new Vector2(250f, 128f), new Vector2(262f, 108f) }, 1.8f, 1.2f, wood);
            Color[,] p = Water(v.Finish(C(70, 64, 58), 0.35f), C(130, 200, 196), C(70, 150, 160), 0.14f, 0.18f, 0.25f);

            // Nénuphars en surface, dans le coin : feuilles rondes fendues et une fleur.
            var s = new VolumeCanvas(PoolW, PoolH);
            foreach (Vector3 pad in new[] { new Vector3(280f, 30f, 17f), new Vector3(306f, 62f, 12f), new Vector3(250f, 14f, 11f) })
            {
                float cx = pad.x, cy = pad.y, r = pad.z;
                s.Piece((x, y) => Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) <= r && !(x > cx && Mathf.Abs(y - cy) < (x - cx) * 0.35f),
                    C(70, 140, 60), 3f, (x, y, c) => Mathf.Abs(Mathf.Atan2(y - cy, x - cx) * 3f % 1f) < 0.08f ? Darker(c, 0.85f) : c);
            }

            s.Piece(Ellipse(276f, 34f, 4.5f, 4.5f), C(250, 236, 240), 3f);
            s.Piece(Ellipse(276f, 34f, 1.8f, 1.8f), C(250, 210, 80), 1.5f);
            return Float(p, s, C(40, 70, 36));
        }

        // --- Fleuve Amazone : vase, grosses racines qui plongent, feuilles mortes, feuilles de nénuphar géant ---

        private static Color[,] AmazonPool()
        {
            var v = new VolumeCanvas(PoolW, PoolH);
            Ground(v, C(150, 112, 72), C(176, 140, 96), C(112, 80, 50), 0.15f);
            for (int i = 0; i < 26; i++)
            {
                float rx = 8f + Hash(i, 51) * 304f, ry = 8f + Hash(51, i) * 176f, a = Hash(i, 52) * 3f;
                float lx = Mathf.Cos(a), ly = Mathf.Sin(a);
                Color leaf = i % 3 == 0 ? C(196, 120, 50) : i % 3 == 1 ? C(150, 84, 40) : C(120, 110, 50);
                v.Piece((x, y) =>
                {
                    float u = (x - rx) * lx + (y - ry) * ly, t = -(x - rx) * ly + (y - ry) * lx;
                    return Mathf.Abs(u) < 7f && Mathf.Abs(t) < 3.4f * (1f - u * u / 49f);
                }, leaf, 1.2f, (x, y, c) => Mathf.Abs(-(x - rx) * ly + (y - ry) * lx) < 0.6f ? Darker(c, 0.8f) : c, 0.85f);
            }

            Color root = C(96, 66, 44);
            System.Func<int, int, Color, Color> bark = (x, y, c) => (x * 3 + y) % 7 == 0 ? Darker(c, 0.8f) : Hash(x, y, 8) > 0.92f ? C(80, 110, 60) : c;
            Curve(v, new[] { new Vector2(-6f, 10f), new Vector2(40f, 30f), new Vector2(80f, 52f), new Vector2(104f, 66f) }, 9f, 5f, root, bark);
            Curve(v, new[] { new Vector2(40f, 30f), new Vector2(70f, 90f), new Vector2(120f, 120f), new Vector2(162f, 140f) }, 6f, 3.5f, root, bark);
            Curve(v, new[] { new Vector2(330f, 186f), new Vector2(290f, 150f), new Vector2(250f, 120f), new Vector2(236f, 108f) }, 10f, 5f, root, bark);
            Curve(v, new[] { new Vector2(290f, 150f), new Vector2(300f, 100f), new Vector2(280f, 60f) }, 5f, 3f, root, bark);
            Color[,] p = Water(v.Finish(C(50, 34, 24), 0.35f), C(154, 112, 62), C(96, 62, 30), 0.36f, 0.24f, 0.1f);

            // Feuilles de nénuphar géant en surface, bord relevé.
            var s = new VolumeCanvas(PoolW, PoolH);
            foreach (Vector3 pad in new[] { new Vector3(56f, 166f, 30f), new Vector3(222f, 14f, 24f) })
            {
                float cx = pad.x, cy = pad.y, r = pad.z;
                s.Piece(Ellipse(cx, cy, r, r), C(86, 130, 52), 4f, (x, y, c) =>
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (d > r - 2.5f) return C(150, 70, 60);   // le rebord rouge
                    float a = Mathf.Atan2(y - cy, x - cx);
                    return Mathf.Abs(Mathf.Sin(a * 8f)) < 0.12f ? Darker(c, 0.82f) : c;
                });
            }

            return Float(p, s, C(40, 50, 24));
        }

        // --- Baie d'Izu : sable volcanique noir, colonnes de basalte, galets noirs, varech, pétales de cerisier ---

        private static Color[,] IzuPool()
        {
            var v = new VolumeCanvas(PoolW, PoolH);
            Ground(v, C(92, 90, 94), C(140, 138, 140), C(60, 58, 62), 0.8f);
            Pebbles(v, 40, 2f, 4.5f, new[] { C(40, 38, 42), C(64, 62, 66) }, 61);
            // Varech : longs rubans bruns qui ondulent.
            for (int i = 0; i < 5; i++)
            {
                float x0 = 10f + i * 14f, y0 = 190f;
                Curve(v, Wave(x0, y0, x0 + 40f + i * 8f, 110f - i * 6f, 5f, 1.2f), 3.2f, 2f, i % 2 == 0 ? C(110, 90, 40) : C(132, 108, 50),
                    (x, y, c) => (x + y) % 5 == 0 ? Darker(c, 1.1f) : c);
            }

            // Colonnes de basalte : des hexagones serrés, plus ou moins hauts.
            Color basalt = C(66, 64, 70);
            void Columns(float cx, float cy, int seed)
            {
                for (int i = 0; i < 9; i++)
                {
                    float a = i * 0.9f, d = i == 0 ? 0f : 10f + (i % 2) * 5f;
                    v.Piece(Hexagon(cx + Mathf.Cos(a) * d, cy + Mathf.Sin(a) * d * 0.9f, 8f), Darker(basalt, 0.9f + Hash(i, seed) * 0.3f), 2.5f,
                        (x, y, c) => Hash(x, y, seed) > 0.9f ? C(110, 120, 90) : c, 0.55f);
                }
            }

            Columns(80f, 60f, 1);
            Columns(242f, 138f, 2);
            v.Piece(Hexagon(170f, 92f, 7f), basalt, 2.5f);
            Color[,] p = Water(v.Finish(C(30, 28, 32), 0.35f), C(90, 170, 184), C(40, 110, 140), 0.24f, 0.2f, 0.25f);

            // Pétales de cerisier tombés sur l'eau.
            var s = new VolumeCanvas(PoolW, PoolH);
            for (int i = 0; i < 34; i++)
            {
                float px = Hash(i, 71) * 320f, py = Hash(71, i) * 192f, a = Hash(i, 72) * 3f;
                s.Piece(Capsule(px, py, px + Mathf.Cos(a) * 2.5f, py + Mathf.Sin(a) * 2.5f, 1.7f), i % 3 == 0 ? C(250, 220, 230) : C(244, 176, 196), 1.2f);
            }

            return Float(p, s, C(170, 90, 110));
        }

        // --- Grande Barrière : sable blanc et jardin de corail (branchu, cerveau, table, mou) ---

        private static Color[,] ReefPool()
        {
            var v = new VolumeCanvas(PoolW, PoolH);
            Ground(v, C(240, 234, 214), C(255, 252, 240), C(204, 194, 164), 0.9f);
            Grass(v, 290f, 60f, 6, 12f, C(70, 150, 80), C(90, 170, 90), 4);
            Grass(v, 30f, 110f, 5, 12f, C(70, 150, 80), C(90, 170, 90), 5);
            // Corail branchu (corne de cerf) : branches qui se divisent, bouts clairs.
            void Branch(Vector2 from, float angle, float length, int depth)
            {
                Vector2 to = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;
                v.Piece(Capsule(from.x, from.y, to.x, to.y, 2.6f - depth * 0.5f), depth == 2 ? C(220, 180, 230) : C(150, 100, 170), 2f, null, 0.75f);
                if (depth >= 2) return;
                Branch(to, angle - 0.5f, length * 0.75f, depth + 1);
                Branch(to, angle + 0.5f, length * 0.75f, depth + 1);
            }

            for (int i = 0; i < 6; i++) Branch(new Vector2(70f, 64f), i * 1.05f, 11f, 0);
            // Corail cerveau : gros dôme à méandres.
            v.Piece(Ellipse(240f, 128f, 30f, 24f), C(170, 160, 90), 18f, (x, y, c) =>
                Mathf.Abs(Mathf.Sin(x * 0.55f + Mathf.Sin(y * 0.4f) * 2.5f)) < 0.3f ? Darker(c, 0.72f) : c);
            // Corail table : grand plateau rayé du centre vers le bord.
            v.Piece(Ellipse(156f, 40f, 22f, 15f), C(230, 150, 90), 4f, (x, y, c) =>
                Mathf.Abs(Mathf.Sin(Mathf.Atan2(y - 40f, x - 156f) * 12f)) < 0.2f ? Darker(c, 0.85f) : c);
            // Corail mou bleu, en grappe de petits doigts.
            for (int i = 0; i < 12; i++)
            {
                float a = i * 0.55f, d = 4f + (i % 3) * 4f;
                v.Piece(Ellipse(104f + Mathf.Cos(a) * d, 156f + Mathf.Sin(a) * d, 4f, 4f), i % 2 == 0 ? C(110, 160, 230) : C(140, 190, 240), 3f);
            }

            return Water(v.Finish(C(60, 50, 60), 0.35f), C(70, 210, 210), C(20, 140, 180), 0.22f, 0.22f, 0.25f);
        }

        // --- Atoll des Maldives : platier de dalles crème fendu de chenaux de sable, petits coraux colorés ---

        private static Color[,] AtollPool()
        {
            var v = new VolumeCanvas(PoolW, PoolH);
            Ground(v, C(250, 246, 232), C(255, 255, 250), C(214, 206, 180), 0.6f);
            Color slab = C(226, 214, 186);
            System.Func<int, int, Color, Color> pitted = (x, y, c) => Hash(x, y, 9) > 0.9f ? Darker(c, 0.85f) : Hash(x / 3, y / 3, 10) > 0.85f ? C(236, 226, 200) : c;
            v.Piece(Union(Ellipse(40f, 40f, 60f, 44f), Ellipse(100f, 30f, 40f, 30f), Ellipse(20f, 100f, 34f, 30f)), slab, 6f, pitted, 0.6f);
            v.Piece(Union(Ellipse(280f, 170f, 70f, 40f), Ellipse(220f, 186f, 50f, 24f), Ellipse(310f, 120f, 30f, 30f)), slab, 6f, pitted, 0.6f);
            v.Piece(Union(Ellipse(170f, 110f, 26f, 16f), Ellipse(186f, 120f, 18f, 12f)), slab, 5f, pitted, 0.6f);
            // Petits coraux colorés posés sur le platier.
            Color[] tufts = { C(250, 140, 120), C(250, 200, 80), C(170, 120, 220), C(90, 200, 170) };
            foreach (Vector3 t in new[] { new Vector3(56f, 40f, 0f), new Vector3(110f, 26f, 1f), new Vector3(24f, 96f, 2f), new Vector3(264f, 170f, 3f), new Vector3(300f, 130f, 0f), new Vector3(178f, 108f, 1f) })
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 1.05f;
                    v.Piece(Ellipse(t.x + Mathf.Cos(a) * 3f, t.y + Mathf.Sin(a) * 3f, 2.6f, 2.6f), tufts[(int)t.z], 2f);
                }
            }

            return Water(v.Finish(C(110, 96, 80), 0.3f), C(80, 222, 222), C(20, 160, 200), 0.22f, 0.26f, 0.3f);
        }

        // --- Côte des Squelettes : sable gris, côtes et vertèbre de baleine, rochers couverts de moules, kelp ---

        private static Color[,] SkeletonPool()
        {
            var v = new VolumeCanvas(PoolW, PoolH);
            Ground(v, C(178, 170, 152), C(206, 200, 184), C(120, 114, 100), 0.6f);
            // Kelp : larges frondes olive.
            for (int i = 0; i < 4; i++)
            {
                Curve(v, Wave(-10f, 20f + i * 18f, 110f, 50f + i * 10f, 4f, 1f), 5f, 3f, i % 2 == 0 ? C(100, 100, 40) : C(120, 112, 46),
                    (x, y, c) => (x / 2 + y) % 6 == 0 ? Darker(c, 1.12f) : c);
            }

            // Côtes de baleine : grands arcs blancs parallèles.
            Color bone = C(232, 226, 208);
            System.Func<int, int, Color, Color> porous = (x, y, c) => Hash(x, y, 11) > 0.9f ? Darker(c, 0.85f) : c;
            for (int i = 0; i < 4; i++)
            {
                float x0 = 20f + i * 34f;
                Curve(v, new[] { new Vector2(x0, 186f), new Vector2(x0 + 20f, 150f), new Vector2(x0 + 50f, 126f), new Vector2(x0 + 86f, 118f) }, 3.8f, 2.6f, bone, porous);
            }

            // Vertèbre : gros bloc d'os avec ses ailes.
            v.Piece(Union(Ellipse(252f, 60f, 16f, 13f), Capsule(226f, 60f, 278f, 60f, 5f), Capsule(252f, 40f, 252f, 80f, 4f)), bone, 7f, porous);
            // Rochers sombres couverts de moules.
            v.Piece(Ellipse(238f, 150f, 30f, 20f), C(90, 88, 92), 14f, (x, y, c) => Hash(x / 2, y / 2, 12) > 0.6f ? C(40, 38, 54) : c, 0.6f);
            Color[,] p = Water(v.Finish(C(40, 38, 36), 0.35f), C(84, 140, 136), C(30, 80, 96), 0.28f, 0.24f, 0.12f);
            return p;
        }

        // --- Fjords de Norvège : plage de gros galets gris, blocs de granit à balanes, varech à flotteurs, eau sombre ---

        private static Color[,] FjordPool()
        {
            var v = new VolumeCanvas(PoolW, PoolH);
            Ground(v, C(120, 120, 118), C(160, 160, 156), C(86, 86, 86), 0f);
            Pebbles(v, 260, 3f, 8f, new[] { C(132, 132, 134), C(160, 156, 150), C(104, 106, 110), C(176, 172, 166), C(120, 112, 104) }, 81);
            // Blocs de granit semés de balanes blanches.
            System.Func<int, int, Color, Color> barnacles = (x, y, c) => Hash(x, y, 13) > 0.9f ? C(230, 226, 214) : Hash(x, y, 14) > 0.95f ? C(170, 180, 120) : c;
            v.Piece(Ellipse(80f, 70f, 30f, 22f), C(140, 136, 140), 20f, barnacles, 0.6f);
            v.Piece(Ellipse(232f, 132f, 34f, 26f), C(128, 126, 132), 22f, barnacles, 0.6f);
            // Varech vésiculeux : rubans olive avec leurs flotteurs.
            foreach (Vector4 f in new[] { new Vector4(130f, 10f, 200f, 60f), new Vector4(20f, 140f, 110f, 180f), new Vector4(150f, 30f, 190f, 80f) })
            {
                Vector2[] pts = Wave(f.x, f.y, f.z, f.w, 4f, 1.5f);
                Curve(v, pts, 2.6f, 2f, C(120, 104, 44));
                for (int i = 1; i < pts.Length; i += 3) v.Piece(Ellipse(pts[i].x + 2f, pts[i].y, 2.4f, 2f), C(150, 130, 56), 2f);
            }

            return Water(v.Finish(C(40, 40, 44), 0.35f), C(74, 116, 132), C(26, 58, 82), 0.3f, 0.26f, 0.12f);
        }

        // --- Baie d'Ha Long : vase, racines-échasses de palétuvier, calcaire déchiqueté, huîtres ---

        private static Color[,] HaLongPool()
        {
            var v = new VolumeCanvas(PoolW, PoolH);
            Ground(v, C(170, 160, 124), C(196, 188, 156), C(126, 116, 88), 0.4f);
            Grass(v, 160f, 30f, 6, 12f, C(70, 140, 80), C(90, 160, 90), 6);
            Grass(v, 300f, 90f, 5, 12f, C(70, 140, 80), C(90, 160, 90), 7);
            // Racines-échasses : des arceaux qui partent d'un même tronc.
            Color mangrove = C(110, 84, 64);
            void Roots(float cx, float cy, int seed)
            {
                for (int i = 0; i < 7; i++)
                {
                    float a = i * 0.9f + seed, len = 22f + Hash(i, seed) * 12f;
                    var end = new Vector2(cx + Mathf.Cos(a) * len, cy + Mathf.Sin(a) * len);
                    var mid = Vector2.Lerp(new Vector2(cx, cy), end, 0.5f) + new Vector2(-Mathf.Sin(a), Mathf.Cos(a)) * 6f;
                    Curve(v, new[] { new Vector2(cx, cy), mid, end }, 3.4f, 2.2f, mangrove, (x, y, c) => (x + y) % 5 == 0 ? Darker(c, 0.85f) : c);
                }

                v.Piece(Ellipse(cx, cy, 7f, 7f), Darker(mangrove, 0.9f), 5f);
            }

            Roots(46f, 46f, 1);
            Roots(276f, 160f, 2);
            // Calcaire déchiqueté, clair, avec des huîtres collées.
            System.Func<int, int, Color, Color> karst = (x, y, c) => Hash(x / 2, y / 3, 15) > 0.7f ? Darker(c, 0.82f) : Hash(x, y, 16) > 0.94f ? C(210, 206, 196) : c;
            v.Piece(Union(Hexagon(206f, 62f, 18f), Hexagon(222f, 74f, 12f), Hexagon(190f, 76f, 10f)), C(188, 186, 178), 9f, karst, 0.6f);
            v.Piece(Union(Hexagon(124f, 150f, 15f), Hexagon(110f, 138f, 9f)), C(180, 178, 170), 8f, karst, 0.6f);
            return Water(v.Finish(C(50, 46, 38), 0.35f), C(90, 182, 152), C(30, 120, 110), 0.3f, 0.22f, 0.2f);
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
