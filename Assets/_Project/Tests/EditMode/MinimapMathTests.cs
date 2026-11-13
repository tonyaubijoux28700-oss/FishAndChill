using FishAndChill.UI;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class MinimapMathTests
    {
        private static readonly Rect Map = Rect.MinMaxRect(-48f, -36f, 48f, 36f);

        [Test]
        public void Center_IsMiddleOfTheMap()
        {
            Assert.That(MinimapMath.WorldToNormalized(Vector2.zero, Map), Is.EqualTo(new Vector2(0.5f, 0.5f)));
        }

        [Test]
        public void Corners_MapToZeroAndOne()
        {
            Assert.That(MinimapMath.WorldToNormalized(new Vector2(-48f, -36f), Map), Is.EqualTo(Vector2.zero));
            Assert.That(MinimapMath.WorldToNormalized(new Vector2(48f, 36f), Map), Is.EqualTo(Vector2.one));
        }

        [Test]
        public void MovingRight_MovesTheDotRight()
        {
            Vector2 before = MinimapMath.WorldToNormalized(new Vector2(0f, -20f), Map);
            Vector2 after = MinimapMath.WorldToNormalized(new Vector2(9.6f, -20f), Map);

            Assert.That(after.x - before.x, Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(after.y, Is.EqualTo(before.y));
        }

        [Test]
        public void OutsideTheMap_IsClampedToTheEdge()
        {
            Assert.That(MinimapMath.WorldToNormalized(new Vector2(500f, -500f), Map), Is.EqualTo(new Vector2(1f, 0f)));
        }

        [Test]
        public void BlinkAlpha_StaysVisible()
        {
            for (float t = 0f; t < 2f; t += 0.05f)
            {
                float alpha = MinimapMath.BlinkAlpha(t);
                Assert.That(alpha, Is.InRange(0.35f, 1f));
            }
        }
    }
}
