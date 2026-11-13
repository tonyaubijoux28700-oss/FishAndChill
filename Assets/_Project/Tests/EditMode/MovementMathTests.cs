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

        // Monde de test : on ne peut se tenir que sous y = 0.
        private static bool BelowZero(Vector2 point) => point.y < 0f;

        [Test]
        public void StepWithCollision_MovesFreelyOnLand()
        {
            Vector2 next = MovementMath.StepWithCollision(new Vector2(0f, -2f), Vector2.right, 2f, 1f, BelowZero);

            Assert.That(next, Is.EqualTo(new Vector2(2f, -2f)));
        }

        [Test]
        public void StepWithCollision_StopsAtWater()
        {
            Vector2 start = new Vector2(0f, -0.5f);

            Vector2 next = MovementMath.StepWithCollision(start, Vector2.up, 2f, 1f, BelowZero);

            Assert.That(next, Is.EqualTo(start));
        }

        [Test]
        public void StepWithCollision_SlidesAlongTheShore()
        {
            Vector2 next = MovementMath.StepWithCollision(new Vector2(0f, -0.5f), new Vector2(1f, 1f), 1f, 1f, BelowZero);

            Assert.That(next.y, Is.EqualTo(-0.5f));
            Assert.That(next.x, Is.GreaterThan(0f));
        }

        [Test]
        public void UpdateFacing_KeepsLastDirectionWhenIdle()
        {
            Vector2 facing = MovementMath.UpdateFacing(Vector2.left, Vector2.zero);

            Assert.That(facing, Is.EqualTo(Vector2.left));
            Assert.That(MovementMath.UpdateFacing(facing, new Vector2(0f, 3f)), Is.EqualTo(Vector2.up));
        }

        [Test]
        public void Step_NoInputMeansNoMovement()
        {
            Vector2 start = new Vector2(1f, 1f);

            Assert.That(MovementMath.Step(start, Vector2.zero, 5f, 1f, Bounds), Is.EqualTo(start));
        }
    }
}
