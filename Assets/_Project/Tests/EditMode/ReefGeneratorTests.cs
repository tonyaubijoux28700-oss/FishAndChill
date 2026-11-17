using FishAndChill.World;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class ReefGeneratorTests
    {
        private static readonly IslandGenerator.Settings Settings = new IslandGenerator.Settings { Width = 90, Height = 64, Seed = 9090 };

        [Test]
        public void Coast_HasLandInTheNorth_SeaInTheSouth_AndABeach()
        {
            TerrainType[,] map = ReefGenerator.Generate(Settings);
            for (int x = 0; x < 90; x++)
            {
                Assert.That(map[x, 0], Is.EqualTo(TerrainType.Water));
            }

            Assert.That(map[20, 63], Is.EqualTo(TerrainType.Grass));
            int y = 0;
            while (map[20, y] == TerrainType.Water) y++;
            Assert.That(map[20, y], Is.EqualTo(TerrainType.Sand), "plage entre la mer et la brousse");
        }

        [Test]
        public void MangroveInlet_CutsIntoTheLand()
        {
            TerrainType[,] map = ReefGenerator.Generate(Settings);
            int x = (int)ReefGenerator.InletAt(Settings, 48.5f);
            Assert.That(map[x, 48], Is.EqualTo(TerrainType.Water));
        }

        [Test]
        public void Reef_IsOffshore_InTheWater()
        {
            TerrainType[,] map = ReefGenerator.Generate(Settings);
            bool[,] reef = ReefGenerator.Reef(Settings, map);
            int count = 0;
            for (int x = 0; x < 90; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    if (!reef[x, y]) continue;
                    count++;
                    Assert.That(map[x, y], Is.EqualTo(TerrainType.Water));
                }
            }

            Assert.That(count, Is.GreaterThan(40), "assez de coraux pour un vrai récif");
        }

        [Test]
        public void SameSeed_GivesTheSameCoast()
        {
            Assert.That(ReefGenerator.Generate(Settings), Is.EqualTo(ReefGenerator.Generate(Settings)));
        }
    }
}
