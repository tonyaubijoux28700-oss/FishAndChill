using FishAndChill.World;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class BayGeneratorTests
    {
        private static readonly IslandGenerator.Settings Settings = new IslandGenerator.Settings { Width = 90, Height = 64, Seed = 8080 };

        [Test]
        public void Bay_HasSeaInTheSouth_AndHillsInTheNorth()
        {
            TerrainType[,] map = BayGenerator.Generate(Settings);
            for (int x = 0; x < 90; x++)
            {
                Assert.That(map[x, 0], Is.EqualTo(TerrainType.Water), "la mer tout le long du sud");
                Assert.That(map[x, 63], Is.EqualTo(TerrainType.Grass), "les collines au nord");
            }

            Assert.That(map[45, 30], Is.EqualTo(TerrainType.Water), "la baie s'enfonce au milieu");
            Assert.That(map[3, 30], Is.Not.EqualTo(TerrainType.Water), "mais pas sur les côtés");
        }

        [Test]
        public void Shore_IsSandBetweenSeaAndGrass()
        {
            TerrainType[,] map = BayGenerator.Generate(Settings);
            int y = 0;
            while (map[45, y] == TerrainType.Water)
            {
                y++;
            }

            Assert.That(map[45, y], Is.EqualTo(TerrainType.Sand));
        }

        [Test]
        public void SameSeed_GivesTheSameBay()
        {
            Assert.That(BayGenerator.Generate(Settings), Is.EqualTo(BayGenerator.Generate(Settings)));
        }
    }
}
