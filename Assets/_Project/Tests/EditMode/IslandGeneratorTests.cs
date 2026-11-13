using FishAndChill.World;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class IslandGeneratorTests
    {
        private static TerrainType[,] Generate(int seed = 1234)
        {
            return IslandGenerator.Generate(new IslandGenerator.Settings { Width = 60, Height = 40, Seed = seed });
        }

        [Test]
        public void Center_IsLand()
        {
            var map = Generate();

            Assert.That(map[30, 20], Is.Not.EqualTo(TerrainType.Water));
        }

        [Test]
        public void Borders_AreWater()
        {
            var map = Generate();

            for (int x = 0; x < 60; x++)
            {
                Assert.That(map[x, 0], Is.EqualTo(TerrainType.Water));
                Assert.That(map[x, 39], Is.EqualTo(TerrainType.Water));
            }

            for (int y = 0; y < 40; y++)
            {
                Assert.That(map[0, y], Is.EqualTo(TerrainType.Water));
                Assert.That(map[59, y], Is.EqualTo(TerrainType.Water));
            }
        }

        [Test]
        public void SameSeed_GivesSameIsland()
        {
            Assert.That(Generate(7), Is.EqualTo(Generate(7)));
        }

        [Test]
        public void DifferentSeeds_GiveDifferentIslands()
        {
            Assert.That(Generate(1), Is.Not.EqualTo(Generate(2)));
        }

        [Test]
        public void Grass_NeverTouchesWater()
        {
            var map = Generate();

            for (int x = 0; x < 60; x++)
            {
                for (int y = 0; y < 40; y++)
                {
                    if (map[x, y] != TerrainType.Grass)
                    {
                        continue;
                    }

                    Assert.That(IslandGenerator.IsWater(map, x + 1, y), Is.False);
                    Assert.That(IslandGenerator.IsWater(map, x - 1, y), Is.False);
                    Assert.That(IslandGenerator.IsWater(map, x, y + 1), Is.False);
                    Assert.That(IslandGenerator.IsWater(map, x, y - 1), Is.False);
                }
            }
        }

        [Test]
        public void DistanceToLand_CountsCellsFromTheShore()
        {
            var map = new TerrainType[7, 3];
            map[0, 1] = TerrainType.Sand;

            int[,] distance = IslandGenerator.DistanceToLand(map, 4);

            Assert.That(distance[0, 1], Is.EqualTo(0));
            Assert.That(distance[1, 1], Is.EqualTo(1));
            Assert.That(distance[2, 0], Is.EqualTo(2));
            Assert.That(distance[3, 1], Is.EqualTo(3));
            Assert.That(distance[6, 1], Is.EqualTo(4), "plafonné à la distance maximale");
        }

        [Test]
        public void CellHash_IsStableAndInRange()
        {
            float a = IslandGenerator.CellHash(12, -7, 3);

            Assert.That(IslandGenerator.CellHash(12, -7, 3), Is.EqualTo(a));
            Assert.That(a, Is.InRange(0f, 1f));
            Assert.That(IslandGenerator.CellHash(13, -7, 3), Is.Not.EqualTo(a));
        }

        [Test]
        public void Island_HasAllThreeTerrains()
        {
            var map = Generate();
            bool water = false, sand = false, grass = false;
            foreach (TerrainType cell in map)
            {
                water |= cell == TerrainType.Water;
                sand |= cell == TerrainType.Sand;
                grass |= cell == TerrainType.Grass;
            }

            Assert.That(water && sand && grass, Is.True);
        }
    }
}
