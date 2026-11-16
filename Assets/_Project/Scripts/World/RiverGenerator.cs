using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Génère un tronçon de fleuve amazonien sur une grille : un large fleuve qui serpente d'ouest en est, bordé de
    /// rives de vase, la jungle tout autour, un banc de sable qui le traverse (on passe d'une rive à l'autre) et un
    /// bras mort (petit lac en croissant) au sud. Même graine = même fleuve (logique pure, testée).
    /// </summary>
    public static class RiverGenerator
    {
        /// <summary>Colonne du banc de sable qui traverse le fleuve.</summary>
        public static int FordX(IslandGenerator.Settings settings) => Mathf.RoundToInt(settings.Width * 0.2f);

        /// <summary>Hauteur (en cases) du milieu du fleuve à la colonne <paramref name="x"/>.</summary>
        public static float CenterAt(IslandGenerator.Settings settings, float x)
        {
            var random = new System.Random(settings.Seed);
            float phase1 = (float)random.NextDouble() * Mathf.PI * 2f;
            float phase2 = (float)random.NextDouble() * Mathf.PI * 2f;
            float u = x / settings.Width * Mathf.PI * 2f;
            return settings.Height * 0.5f + settings.Height * 0.15f * Mathf.Sin(u * 1.1f + phase1)
                + settings.Height * 0.05f * Mathf.Sin(u * 2.6f + phase2);
        }

        /// <summary>Demi-largeur du fleuve à la colonne <paramref name="x"/>.</summary>
        public static float HalfWidthAt(IslandGenerator.Settings settings, float x)
        {
            float u = x / settings.Width * Mathf.PI * 2f;
            return 6.5f + 1.6f * Mathf.Sin(u * 1.7f + settings.Seed % 7);
        }

        public static TerrainType[,] Generate(IslandGenerator.Settings settings)
        {
            int width = settings.Width;
            int height = settings.Height;
            var map = new TerrainType[width, height];
            int ford = FordX(settings);

            // Bras mort : un petit lac en ovale au sud du fleuve, aux trois quarts de la carte.
            float oxX = width * 0.72f;
            float oxY = CenterAt(settings, oxX) - HalfWidthAt(settings, oxX) - 10f;

            for (int x = 0; x < width; x++)
            {
                float center = CenterAt(settings, x + 0.5f);
                float half = HalfWidthAt(settings, x + 0.5f);
                for (int y = 0; y < height; y++)
                {
                    float d = Mathf.Abs(y + 0.5f - center);
                    float noise = IslandGenerator.CellHash(x, y, settings.Seed + 11) * 1.2f;
                    float ox = (x + 0.5f - oxX) / 8f;
                    float oy = (y + 0.5f - oxY) / 3.6f;
                    float oxbow = ox * ox + oy * oy;

                    if (d <= half || oxbow <= 1f)
                    {
                        map[x, y] = TerrainType.Water;
                    }
                    else if (d <= half + 1.4f + noise || oxbow <= 1.7f)
                    {
                        map[x, y] = TerrainType.Sand; // vase et sable des rives
                    }
                    else
                    {
                        map[x, y] = TerrainType.Grass; // jungle
                    }

                    // Banc de sable de 3 cases qui traverse le fleuve d'une rive à l'autre.
                    if (Mathf.Abs(x - ford) <= 1 && map[x, y] == TerrainType.Water && d <= half + 0.5f)
                    {
                        map[x, y] = TerrainType.Sand;
                    }
                }
            }

            return map;
        }
    }
}
