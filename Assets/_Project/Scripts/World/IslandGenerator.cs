using System;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Génère la forme d'une île sur une grille : de l'eau sur les bords, de l'herbe au centre,
    /// une plage de sable entre les deux. Même graine = même île (logique pure, testée).
    /// </summary>
    public static class IslandGenerator
    {
        [Serializable]
        public sealed class Settings
        {
            public int Width = 96;
            public int Height = 72;
            public int Seed = 1234;

            [Tooltip("Taille des irrégularités de la côte (plus petit = côtes plus découpées).")]
            public float NoiseScale = 0.08f;

            [Tooltip("Force des irrégularités de la côte.")]
            public float NoiseStrength = 0.45f;

            [Tooltip("Au-dessus de ce seuil : terre (sable).")]
            public float LandThreshold = 0.22f;

            [Tooltip("Au-dessus de ce seuil : herbe.")]
            public float GrassThreshold = 0.34f;
        }

        public static TerrainType[,] Generate(Settings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            int width = Mathf.Max(3, settings.Width);
            int height = Mathf.Max(3, settings.Height);
            var map = new TerrainType[width, height];

            var random = new System.Random(settings.Seed);
            float offsetX = random.Next(0, 10000);
            float offsetY = random.Next(0, 10000);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Distance au centre, normalisée (0 au centre, 1 sur le bord) : forme ovale.
                    float nx = (x + 0.5f) / width * 2f - 1f;
                    float ny = (y + 0.5f) / height * 2f - 1f;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);

                    float noise = Mathf.PerlinNoise(offsetX + x * settings.NoiseScale, offsetY + y * settings.NoiseScale);
                    float value = 1f - distance + (noise - 0.5f) * settings.NoiseStrength;

                    if (value > settings.GrassThreshold)
                    {
                        map[x, y] = TerrainType.Grass;
                    }
                    else if (value > settings.LandThreshold)
                    {
                        map[x, y] = TerrainType.Sand;
                    }
                    else
                    {
                        map[x, y] = TerrainType.Water;
                    }
                }
            }

            ForceWaterBorder(map);
            AddBeaches(map);
            return map;
        }

        /// <summary>Indique si une case est de l'eau (hors grille = eau).</summary>
        public static bool IsWater(TerrainType[,] map, int x, int y)
        {
            if (x < 0 || y < 0 || x >= map.GetLength(0) || y >= map.GetLength(1))
            {
                return true;
            }

            return map[x, y] == TerrainType.Water;
        }

        private static void ForceWaterBorder(TerrainType[,] map)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                map[x, 0] = TerrainType.Water;
                map[x, height - 1] = TerrainType.Water;
            }

            for (int y = 0; y < height; y++)
            {
                map[0, y] = TerrainType.Water;
                map[width - 1, y] = TerrainType.Water;
            }
        }

        /// <summary>Toute case d'herbe qui touche l'eau devient du sable : il y a toujours une plage.</summary>
        private static void AddBeaches(TerrainType[,] map)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (map[x, y] == TerrainType.Grass &&
                        (IsWater(map, x + 1, y) || IsWater(map, x - 1, y) || IsWater(map, x, y + 1) || IsWater(map, x, y - 1)))
                    {
                        map[x, y] = TerrainType.Sand;
                    }
                }
            }
        }
    }
}
