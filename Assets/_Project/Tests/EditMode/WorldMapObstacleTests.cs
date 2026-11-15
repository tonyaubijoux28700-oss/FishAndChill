using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class WorldMapObstacleTests
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
        public void StaticObstacles_BlockOnlyTheirArea()
        {
            _map.SetStaticObstacles(new[] { new Rect(2f, 2f, 0.4f, 0.3f) });

            Assert.That(_map.IsBlocked(new Vector2(2.2f, 2.1f)), Is.True, "pied du lampadaire");
            Assert.That(_map.IsBlocked(new Vector2(2.2f, 2.6f)), Is.False, "derrière : on peut passer");
            Assert.That(_map.StaticObstacleCount, Is.EqualTo(1));
        }

        [Test]
        public void ThinObstacle_BetweenTheCorners_StillBlocksThePlayer()
        {
            // Pied de lampadaire de 0,36 de large, plus fin que le joueur (0,5) : aucun coin ne tombe dessus.
            _map.SetStaticObstacles(new[] { new Rect(2f - 0.18f, 2f, 0.36f, 0.3f) });
            _map.AddWalkableArea(new Rect(0f, 0f, 10f, 10f));

            Assert.That(_map.CanStandAt(new Vector2(2f, 2.15f), 0.25f), Is.False);
            Assert.That(_map.CanStandAt(new Vector2(2f, 3f), 0.25f), Is.True);
        }

        [Test]
        public void SetStaticObstacles_ReplacesThePreviousOnes()
        {
            _map.SetStaticObstacles(new[] { new Rect(0f, 0f, 1f, 1f), new Rect(5f, 5f, 1f, 1f) });
            _map.SetStaticObstacles(new[] { new Rect(10f, 10f, 1f, 1f) });

            Assert.That(_map.StaticObstacleCount, Is.EqualTo(1));
            Assert.That(_map.IsBlocked(new Vector2(0.5f, 0.5f)), Is.False);
            Assert.That(_map.IsBlocked(new Vector2(10.5f, 10.5f)), Is.True);
        }
    }
}
