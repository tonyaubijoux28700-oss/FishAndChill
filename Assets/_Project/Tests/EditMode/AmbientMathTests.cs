using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class AmbientMathTests
    {
        [Test]
        public void JumpArc_StartsAndEndsOnTheWater_AndPeaksInTheMiddle()
        {
            var from = new Vector2(0f, 0f);
            var to = new Vector2(2f, 0f);
            Assert.That(AmbientMath.JumpArc(from, to, 1f, 0f), Is.EqualTo(from));
            Assert.That(AmbientMath.JumpArc(from, to, 1f, 1f), Is.EqualTo(to));
            Assert.That(AmbientMath.JumpArc(from, to, 1f, 0.5f).y, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Wrap_SendsACloudBackToTheOtherSide()
        {
            var bounds = new Rect(0f, 0f, 10f, 10f);
            Vector2 wrapped = AmbientMath.Wrap(new Vector2(13f, 5f), bounds, new Vector2(2f, 2f));
            Assert.That(wrapped.x, Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(wrapped.y, Is.EqualTo(5f));
            Assert.That(AmbientMath.Wrap(new Vector2(5f, 5f), bounds, Vector2.one), Is.EqualTo(new Vector2(5f, 5f)));
        }
    }
}
