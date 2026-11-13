using FishAndChill.Player;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class PlayerAnimatorTests
    {
        [Test]
        public void DirectionOf_MapsCardinalDirections()
        {
            Assert.That(PlayerAnimator.DirectionOf(Vector2.up), Is.EqualTo(SpriteDirection.Up));
            Assert.That(PlayerAnimator.DirectionOf(Vector2.down), Is.EqualTo(SpriteDirection.Down));
            Assert.That(PlayerAnimator.DirectionOf(Vector2.left), Is.EqualTo(SpriteDirection.Left));
            Assert.That(PlayerAnimator.DirectionOf(Vector2.right), Is.EqualTo(SpriteDirection.Right));
        }

        [Test]
        public void DirectionOf_DiagonalPrefersHorizontal()
        {
            Assert.That(PlayerAnimator.DirectionOf(new Vector2(0.7f, 0.7f)), Is.EqualTo(SpriteDirection.Right));
            Assert.That(PlayerAnimator.DirectionOf(new Vector2(-0.3f, -0.9f)), Is.EqualTo(SpriteDirection.Down));
        }

        [Test]
        public void WalkFrameIndex_CyclesWithoutUsingIdleFrame()
        {
            Assert.That(PlayerAnimator.WalkFrameIndex(0f, 8f, 3), Is.EqualTo(1));
            Assert.That(PlayerAnimator.WalkFrameIndex(0.13f, 8f, 3), Is.EqualTo(2));
            Assert.That(PlayerAnimator.WalkFrameIndex(0.26f, 8f, 3), Is.EqualTo(1));
        }
    }
}
