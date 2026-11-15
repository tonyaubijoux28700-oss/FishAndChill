using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class WorldMapRegionTests
    {
        private GameObject _host;
        private WorldMap _map;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Carte de test");
            _map = _host.AddComponent<WorldMap>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void WorldBounds_FollowsTheCurrentRegion()
        {
            var island = new Rect(-48f, -36f, 96f, 72f);
            var lake = new Rect(150f, -30f, 80f, 60f);
            _map.SetRegions(new[] { island, lake }, new Rect[0]);

            Assert.That(_map.WorldBounds, Is.EqualTo(island));
            _map.SetCurrentRegion(1);
            Assert.That(_map.WorldBounds, Is.EqualTo(lake));
            _map.SetCurrentRegion(7);
            Assert.That(_map.CurrentRegion, Is.EqualTo(1), "région inconnue : on reste dans les limites");
        }

        [Test]
        public void Dock_IsWalkableAboveTheWater()
        {
            _map.SetRegions(new[] { new Rect(0f, 0f, 10f, 10f) }, new[] { new Rect(4f, 2f, 2f, 5f) });

            Assert.That(_map.IsLand(new Vector2(5f, 4f)), Is.True, "sur le ponton");
            Assert.That(_map.IsWater(new Vector2(8f, 4f)), Is.True, "à côté : de l'eau");
        }
    }
}
