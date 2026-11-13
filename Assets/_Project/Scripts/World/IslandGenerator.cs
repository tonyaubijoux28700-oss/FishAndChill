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

        /// <summary>
        /// Distance (en cases, déplacements en 8 directions) de chaque case d'eau à la terre la plus proche,
        /// plafonnée à <paramref name="maxDistance"/>. Les cases de terre valent 0. Sert au lagon peu profond.
        /// </summary>
        public static int[,] DistanceToLand(TerrainType[,] map, int maxDistance)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            var distance = new int[width, height];
            var queue = new System.Collections.Generic.Queue<Vector2Int>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (map[x, y] == TerrainType.Water)
                    {
                        distance[x, y] = maxDistance;
                    }
                    else
                    {
                        distance[x, y] = 0;
                        queue.Enqueue(new Vector2Int(x, y));
                    }
                }
            }

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                int next = distance[cell.x, cell.y] + 1;
                if (next >= maxDistance)
                {
                    continue;
                }

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = cell.x + dx;
                        int ny = cell.y + dy;
                        if (nx < 0 || ny < 0 || nx >= width || ny >= height || distance[nx, ny] <= next)
                        {
                            continue;
                        }

                        distance[nx, ny] = next;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }

            return distance;
        }

        /// <summary>Nombre pseudo-aléatoire stable dans [0, 1) pour une case (choix des variantes de tuiles).</summary>
        public static float CellHash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
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
