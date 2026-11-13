using FishAndChill.Player;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class MovementMathTests
    {
        private static readonly Rect Bounds = Rect.MinMaxRect(-5f, -3f, 5f, 3f);

        [Test]
        public void Step_MovesBySpeedTimesDeltaTime()
        {
            Vector2 next = MovementMath.Step(Vector2.zero, Vector2.right, speed: 4f, deltaTime: 0.5f, Bounds);

            Assert.That(next.x, Is.EqualTo(2f).Within(1e-5f));
            Assert.That(next.y, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Step_DiagonalIsNotFaster()
        {
            Vector2 next = MovementMath.Step(Vector2.zero, new Vector2(1f, 1f), speed: 1f, deltaTime: 1f, Bounds);

            Assert.That(next.magnitude, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Step_StaysInsideBounds()
        {
            Vector2 next = MovementMath.Step(new Vector2(4.9f, -2.9f), new Vector2(1f, -1f), speed: 10f, deltaTime: 1f, Bounds);

            Assert.That(next.x, Is.EqualTo(5f));
            Assert.That(next.y, Is.EqualTo(-3f));
        }

        [Test]
        public void Step_NoInputMeansNoMovement()
        {
            Vector2 start = new Vector2(1f, 1f);

            Assert.That(MovementMath.Step(start, Vector2.zero, 5f, 1f, Bounds), Is.EqualTo(start));
        }
    }
}
