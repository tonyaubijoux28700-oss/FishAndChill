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
