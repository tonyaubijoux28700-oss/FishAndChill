using System.Collections.Generic;
using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class RiverGeneratorTests
    {
        private static readonly IslandGenerator.Settings Settings = new IslandGenerator.Settings { Width = 90, Height = 64, Seed = 7070 };

        [Test]
        public void River_CrossesTheMap_WithJungleAboveAndBelow()
        {
            TerrainType[,] map = RiverGenerator.Generate(Settings);
            foreach (int x in new[] { 0, 45, 89 })
            {
                int center = Mathf.FloorToInt(RiverGenerator.CenterAt(Settings, x + 0.5f));
                Assert.That(map[x, center], Is.EqualTo(TerrainType.Water), $"fleuve en x = {x}");
                Assert.That(map[x, 0], Is.EqualTo(TerrainType.Grass));
                Assert.That(map[x, 63], Is.EqualTo(TerrainType.Grass));
            }
        }

        [Test]
        public void Ford_LinksBothBanks()
        {
            TerrainType[,] map = RiverGenerator.Generate(Settings);
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            // Remplissage depuis le coin sud-ouest, en ne marchant que sur la terre.
            var seen = new bool[width, height];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(1, 1));
            seen[1, 1] = true;
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    Vector2Int n = c + d;
                    if (n.x < 0 || n.y < 0 || n.x >= width || n.y >= height || seen[n.x, n.y] || map[n.x, n.y] == TerrainType.Water) continue;
                    seen[n.x, n.y] = true;
                    queue.Enqueue(n);
                }
            }

            Assert.That(seen[1, height - 2], Is.True, "la rive nord est accessible par le banc de sable");
        }

        [Test]
        public void SameSeed_GivesTheSameRiver()
        {
            Assert.That(RiverGenerator.Generate(Settings), Is.EqualTo(RiverGenerator.Generate(Settings)));
        }
    }
}
