using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Génère une baie de la côte japonaise sur une grille : la mer au sud, qui s'enfonce en arrondi dans les terres,
    /// une plage de sable volcanique gris tout autour, des collines boisées au nord, à l'est et à l'ouest. Le rivage
    /// ondule un peu. Même graine = même baie (logique pure, testée).
    /// </summary>
    public static class BayGenerator
    {
        /// <summary>Hauteur (en cases) du rivage à la colonne <paramref name="x"/> : en dessous, c'est la mer.</summary>
        public static float ShoreAt(IslandGenerator.Settings settings, float x)
        {
            var random = new System.Random(settings.Seed);
            float phase = (float)random.NextDouble() * Mathf.PI * 2f;
            float u = (x - settings.Width * 0.5f) / (settings.Width * 0.40f);
            float bowl = Mathf.Max(0f, 1f - u * u);
            float wobble = Mathf.Sin(x * 0.21f + phase) * 1.4f + Mathf.Sin(x * 0.07f + phase * 2f) * 2f;
            return settings.Height * 0.16f + settings.Height * 0.52f * bowl + wobble;
        }

        public static TerrainType[,] Generate(IslandGenerator.Settings settings)
        {
            int width = settings.Width;
            int height = settings.Height;
            var map = new TerrainType[width, height];
            for (int x = 0; x < width; x++)
            {
                float shore = ShoreAt(settings, x + 0.5f);
                for (int y = 0; y < height; y++)
                {
                    float above = y + 0.5f - shore;
                    float noise = IslandGenerator.CellHash(x, y, settings.Seed + 5) * 1.0f;
                    if (above < 0f)
                    {
                        map[x, y] = TerrainType.Water;
                    }
                    else if (above < 2.2f + noise)
                    {
                        map[x, y] = TerrainType.Sand;
                    }
                    else
                    {
                        map[x, y] = TerrainType.Grass;
                    }
                }
            }

            return map;
        }
    }
}
