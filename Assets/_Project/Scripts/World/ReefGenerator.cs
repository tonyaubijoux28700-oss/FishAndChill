using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Génère une côte du Queensland face à la Grande Barrière : la terre au nord (plage de sable blanc puis brousse),
    /// la mer au sud, une crique de mangrove qui s'enfonce dans les terres, et au large une bande de récif corallien
    /// en eau peu profonde (<see cref="Reef"/>). Même graine = même côte (logique pure, testée).
    /// </summary>
    public static class ReefGenerator
    {
        /// <summary>Hauteur du rivage à la colonne <paramref name="x"/> : en dessous, la mer.</summary>
        public static float ShoreAt(IslandGenerator.Settings settings, float x)
        {
            var random = new System.Random(settings.Seed);
            float phase = (float)random.NextDouble() * Mathf.PI * 2f;
            return settings.Height * 0.56f + Mathf.Sin(x * 0.09f + phase) * 3f + Mathf.Sin(x * 0.23f + phase * 1.7f) * 1.2f;
        }

        /// <summary>Colonne du milieu de la crique de mangrove.</summary>
        public static float InletAt(IslandGenerator.Settings settings, float y)
        {
            return settings.Width * 0.76f + Mathf.Sin(y * 0.25f) * 2f;
        }

        public static TerrainType[,] Generate(IslandGenerator.Settings settings)
        {
            int width = settings.Width;
            int height = settings.Height;
            var map = new TerrainType[width, height];
            float inletTop = height * 0.86f;
            for (int x = 0; x < width; x++)
            {
                float shore = ShoreAt(settings, x + 0.5f);
                for (int y = 0; y < height; y++)
                {
                    float above = y + 0.5f - shore;
                    float noise = IslandGenerator.CellHash(x, y, settings.Seed + 5);
                    bool inlet = y < inletTop && Mathf.Abs(x + 0.5f - InletAt(settings, y + 0.5f)) < 1.6f + (inletTop - y) / height * 3f;
                    if (above < 0f || inlet)
                    {
                        map[x, y] = TerrainType.Water;
                    }
                    else if (above < 2.6f + noise * 1.2f)
                    {
                        map[x, y] = TerrainType.Sand;
                    }
                    else
                    {
                        map[x, y] = TerrainType.Grass;
                    }
                }
            }

            // Les berges de la crique sont en sable (vase claire) pour que la mangrove y pousse.
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (map[x, y] != TerrainType.Grass) continue;
                    if (map[x - 1, y] == TerrainType.Water || map[x + 1, y] == TerrainType.Water) map[x, y] = TerrainType.Sand;
                }
            }

            return map;
        }

        /// <summary>
        /// Le récif : cases d'eau à 5 à 12 cases du rivage, en taches irrégulières (là où poussent les coraux).
        /// </summary>
        public static bool[,] Reef(IslandGenerator.Settings settings, TerrainType[,] map)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            int[,] distance = IslandGenerator.DistanceToLand(map, 14);
            var reef = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int d = distance[x, y];
                    float patch = Mathf.PerlinNoise(x * 0.18f + settings.Seed * 0.01f, y * 0.18f);
                    reef[x, y] = map[x, y] == TerrainType.Water && d >= 5 && d <= 12 && patch > 0.45f;
                }
            }

            return reef;
        }
    }
}
