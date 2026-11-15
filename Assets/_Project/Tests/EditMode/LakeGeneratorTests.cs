using FishAndChill.World;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class LakeGeneratorTests
    {
        private static readonly IslandGenerator.Settings Settings = new IslandGenerator.Settings { Width = 80, Height = 60, Seed = 4242 };

        [Test]
        public void Lake_HasWaterInTheMiddle_AndLandOnTheEdges()
        {
            TerrainType[,] lake = LakeGenerator.Generate(Settings);

            Assert.That(lake[40, 30], Is.EqualTo(TerrainType.Water));
            for (int x = 0; x < 80; x++)
            {
                Assert.That(lake[x, 0], Is.Not.EqualTo(TerrainType.Water));
                Assert.That(lake[x, 59], Is.Not.EqualTo(TerrainType.Water));
            }
        }

        [Test]
        public void Lake_HasAShoreBetweenWaterAndGrass()
        {
            TerrainType[,] lake = LakeGenerator.Generate(Settings);
            int y = 30;
            int x = 40;
            while (lake[x, y] == TerrainType.Water)
            {
                y--;
            }

            Assert.That(lake[x, y], Is.EqualTo(TerrainType.Sand), "galets au bord de l'eau");
        }

        [Test]
        public void SameSeed_GivesTheSameLake()
        {
            Assert.That(LakeGenerator.Generate(Settings), Is.EqualTo(LakeGenerator.Generate(Settings)));
        }
    }
}
