using FishAndChill.Fishing;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class CastTargetingTests
    {
        // Monde de test : la terre est en dessous de y = 2, l'eau au-dessus.
        private static bool WaterAboveTwo(Vector2 point) => point.y > 2f;

        [Test]
        public void CastsAtDesiredDistance_WhenItIsWater()
        {
            Vector2? point = CastTargeting.FindCastPoint(Vector2.zero, Vector2.up, 3f, 5f, 0f, WaterAboveTwo);

            Assert.That(point.HasValue, Is.True);
            Assert.That(point.Value.y, Is.EqualTo(3f).Within(1e-4f));
        }

        [Test]
        public void SearchesFurther_WhenDesiredPointIsLand()
        {
            Vector2? point = CastTargeting.FindCastPoint(Vector2.zero, Vector2.up, 1f, 5f, 0f, WaterAboveTwo);

            Assert.That(point.HasValue, Is.True);
            Assert.That(point.Value.y, Is.GreaterThan(2f));
        }

        [Test]
        public void ReturnsNull_WhenFacingLand()
        {
            Vector2? point = CastTargeting.FindCastPoint(Vector2.zero, Vector2.down, 2f, 5f, 0f, WaterAboveTwo);

            Assert.That(point.HasValue, Is.False);
        }

        [Test]
        public void SideOffset_IsPerpendicularToFacing()
        {
            Vector2? point = CastTargeting.FindCastPoint(Vector2.zero, Vector2.up, 3f, 5f, 1f, WaterAboveTwo);

            Assert.That(point.Value.x, Is.EqualTo(-1f).Within(1e-4f));
        }
    }
}
