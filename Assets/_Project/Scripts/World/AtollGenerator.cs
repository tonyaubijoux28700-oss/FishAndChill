using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Génère un atoll des Maldives : un anneau de terre (récif émergé) autour d'un grand lagon turquoise. Au nord, un
    /// îlot large et continu (le « motu » habité, avec sa cocoteraie) ; ailleurs, de petits îlots séparés par des
    /// passes vers le large. Hors de l'anneau, l'océan profond ; juste dehors, un platier de corail (<see cref="Reef"/>).
    /// Même graine = même atoll (logique pure, testée).
    /// </summary>
    public static class AtollGenerator
    {
        /// <summary>Centre et rayons de l'anneau (en cases).</summary>
        public static Vector2 Center(IslandGenerator.Settings s) => new Vector2(s.Width * 0.5f, s.Height * 0.47f);

        public static Vector2 Radii(IslandGenerator.Settings s) => new Vector2(s.Width * 0.38f, s.Height * 0.36f);

        /// <summary>Distance normalisée au centre (1 = sur l'anneau).</summary>
        public static float Ring(IslandGenerator.Settings s, float x, float y)
        {
            Vector2 c = Center(s), r = Radii(s);
            float u = (x - c.x) / r.x, v = (y - c.y) / r.y;
            return Mathf.Sqrt(u * u + v * v);
        }

        /// <summary>Demi-épaisseur de terre de l'anneau selon l'angle (0 = une passe).</summary>
        public static float HalfWidth(IslandGenerator.Settings s, float angle)
        {
            var random = new System.Random(s.Seed);
            float phase = (float)random.NextDouble() * Mathf.PI * 2f;
            float deg = Mathf.Repeat(angle * Mathf.Rad2Deg, 360f);
            if (deg > 25f && deg < 155f)
            {
                // Le grand îlot du nord, large au milieu.
                return 3.2f + Mathf.Sin((deg - 25f) / 130f * Mathf.PI) * 2.6f + Mathf.Sin(angle * 9f + phase) * 0.5f;
            }

            // Petits îlots séparés par des passes.
            float wave = Mathf.Sin(angle * 7f + phase) + Mathf.Sin(angle * 13f + phase * 1.3f) * 0.4f;
            return wave > 0.25f ? 1.6f + wave * 0.8f : 0f;
        }

        public static TerrainType[,] Generate(IslandGenerator.Settings settings)
        {
            int width = settings.Width;
            int height = settings.Height;
            var map = new TerrainType[width, height];
            Vector2 c = Center(settings);
            float scale = Mathf.Min(Radii(settings).x, Radii(settings).y);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float band = Mathf.Abs(Ring(settings, fx, fy) - 1f) * scale;   // distance à l'anneau, en cases
                    float half = HalfWidth(settings, Mathf.Atan2(fy - c.y, fx - c.x));
                    float noise = IslandGenerator.CellHash(x, y, settings.Seed + 5) * 0.6f;
                    if (half <= 0f || band > half)
                    {
                        map[x, y] = TerrainType.Water;
                    }
                    else
                    {
                        map[x, y] = band > half - 1.8f - noise ? TerrainType.Sand : TerrainType.Grass;
                    }
                }
            }

            return map;
        }

        /// <summary>Case d'eau du lagon (à l'intérieur de l'anneau).</summary>
        public static bool InLagoon(IslandGenerator.Settings settings, int x, int y) => Ring(settings, x + 0.5f, y + 0.5f) < 1f;

        /// <summary>Platier de corail : eau juste à l'extérieur de l'anneau, en taches irrégulières.</summary>
        public static bool[,] Reef(IslandGenerator.Settings settings, TerrainType[,] map)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            float scale = Mathf.Min(Radii(settings).x, Radii(settings).y);
            var reef = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float outside = (Ring(settings, x + 0.5f, y + 0.5f) - 1f) * scale;
                    float patch = Mathf.PerlinNoise(x * 0.2f + settings.Seed * 0.01f, y * 0.2f);
                    reef[x, y] = map[x, y] == TerrainType.Water && outside > 3f && outside < 9f && patch > 0.42f;
                }
            }

            return reef;
        }
    }
}
