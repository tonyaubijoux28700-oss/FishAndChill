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
        public void FacingAngle_PointsTheArrowWhereThePlayerLooks()
        {
            Assert.That(MinimapMath.FacingAngle(Vector2.up), Is.EqualTo(0f).Within(0.01f));
            Assert.That(MinimapMath.FacingAngle(Vector2.right), Is.EqualTo(-90f).Within(0.01f));
            Assert.That(MinimapMath.FacingAngle(Vector2.left), Is.EqualTo(90f).Within(0.01f));
            Assert.That(Mathf.Abs(MinimapMath.FacingAngle(Vector2.down)), Is.EqualTo(180f).Within(0.01f));
        }

        [Test]
        public void Bilinear_BlendsBetweenCells_AndClampsAtTheEdges()
        {
            var grid = new float[2, 2];
            grid[1, 0] = 1f;
            grid[1, 1] = 1f;
            Assert.That(MinimapMath.Bilinear(grid, 0.5f, 0.5f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(MinimapMath.Bilinear(grid, -3f, 0f), Is.EqualTo(0f));
            Assert.That(MinimapMath.Bilinear(grid, 9f, 9f), Is.EqualTo(1f));
        }

        [Test]
        public void Pulse_GrowsAndFades()
        {
            Vector2 start = MinimapMath.Pulse(0f);
            Vector2 later = MinimapMath.Pulse(1f);
            Assert.That(start, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(later.x, Is.GreaterThan(start.x));
            Assert.That(later.y, Is.LessThan(start.y));
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
