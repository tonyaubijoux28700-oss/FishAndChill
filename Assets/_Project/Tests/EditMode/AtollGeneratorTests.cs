using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class AtollGeneratorTests
    {
        private static readonly IslandGenerator.Settings Settings = new IslandGenerator.Settings { Width = 90, Height = 64, Seed = 7373 };

        [Test]
        public void Lagoon_InTheMiddle_OceanAround()
        {
            TerrainType[,] map = AtollGenerator.Generate(Settings);
            Vector2 c = AtollGenerator.Center(Settings);
            Assert.That(map[(int)c.x, (int)c.y], Is.EqualTo(TerrainType.Water), "le lagon au centre");
            Assert.That(AtollGenerator.InLagoon(Settings, (int)c.x, (int)c.y), Is.True);
            Assert.That(map[0, 0], Is.EqualTo(TerrainType.Water), "l'océan dans les coins");
            Assert.That(map[89, 63], Is.EqualTo(TerrainType.Water));
        }

        [Test]
        public void NorthIslet_IsWide_WithGrassInside()
        {
            TerrainType[,] map = AtollGenerator.Generate(Settings);
            Vector2 c = AtollGenerator.Center(Settings);
            Vector2 r = AtollGenerator.Radii(Settings);
            int x = (int)c.x;
            int y = (int)(c.y + r.y);
            Assert.That(map[x, y], Is.EqualTo(TerrainType.Grass), "cocoteraie au milieu du grand îlot");
            int land = 0;
            for (int dy = -8; dy <= 8; dy++)
            {
                if (y + dy >= 0 && y + dy < 64 && map[x, y + dy] != TerrainType.Water) land++;
            }

            Assert.That(land, Is.GreaterThanOrEqualTo(8), "l'îlot du nord est large");
        }

        [Test]
        public void Ring_HasPasses_ToTheOcean()
        {
            int passes = 0;
            bool wasLand = AtollGenerator.HalfWidth(Settings, 0f) > 0f;
            for (int i = 1; i <= 360; i++)
            {
                bool land = AtollGenerator.HalfWidth(Settings, i * Mathf.Deg2Rad) > 0f;
                if (wasLand && !land) passes++;
                wasLand = land;
            }

            Assert.That(passes, Is.GreaterThanOrEqualTo(2), "des passes entre les îlots");
        }

        [Test]
        public void Reef_IsOutsideTheRing_InTheWater()
        {
            TerrainType[,] map = AtollGenerator.Generate(Settings);
            bool[,] reef = AtollGenerator.Reef(Settings, map);
            int count = 0;
            for (int x = 0; x < 90; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    if (!reef[x, y]) continue;
                    count++;
                    Assert.That(map[x, y], Is.EqualTo(TerrainType.Water));
                    Assert.That(AtollGenerator.InLagoon(Settings, x, y), Is.False);
                }
            }

            Assert.That(count, Is.GreaterThan(30));
        }

        [Test]
        public void SameSeed_GivesTheSameAtoll()
        {
            Assert.That(AtollGenerator.Generate(Settings), Is.EqualTo(AtollGenerator.Generate(Settings)));
        }
    }
}
