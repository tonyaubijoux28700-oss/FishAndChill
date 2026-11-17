using FishAndChill.UI;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class WorldChartTests
    {
        [Test]
        public void Projection_PlacesTheZonesWhereTheyReallyAre()
        {
            Vector2 annecy = WorldChart.FromLonLat(new Vector2(6.1f, 45.9f));
            Assert.That(annecy.x, Is.EqualTo(0.52f).Within(0.01f));
            Assert.That(annecy.y, Is.EqualTo(0.74f).Within(0.01f));

            Vector2 back = WorldChart.ToLonLat(WorldChart.FromLonLat(new Vector2(-60f, -3f)));
            Assert.That(back.x, Is.EqualTo(-60f).Within(0.01f));
            Assert.That(back.y, Is.EqualTo(-3f).Within(0.01f));
        }

        [Test]
        public void Continents_AreLand_AndOceansAreSea()
        {
            Assert.That(WorldChart.IsLand(2.3f, 48.8f), Is.True, "Paris");
            Assert.That(WorldChart.IsLand(-60f, -3f), Is.True, "Amazonie");
            Assert.That(WorldChart.IsLand(139.7f, 35.7f), Is.True, "Tokyo");
            Assert.That(WorldChart.IsLand(-100f, 40f), Is.True, "États-Unis");
            Assert.That(WorldChart.IsLand(-40f, 30f), Is.False, "Atlantique");
            Assert.That(WorldChart.IsLand(-140f, 0f), Is.False, "Pacifique");
            Assert.That(WorldChart.IsLand(80f, -20f), Is.False, "océan Indien");
        }

        [Test]
        public void Ground_KnowsDesertsAndIce()
        {
            Assert.That(WorldChart.GroundAt(10f, 24f), Is.EqualTo(WorldChart.Ground.Desert), "Sahara");
            Assert.That(WorldChart.GroundAt(0f, -80f), Is.EqualTo(WorldChart.Ground.Ice), "Antarctique");
            Assert.That(WorldChart.GroundAt(2.3f, 48.8f), Is.EqualTo(WorldChart.Ground.Green));
        }
    }
}
