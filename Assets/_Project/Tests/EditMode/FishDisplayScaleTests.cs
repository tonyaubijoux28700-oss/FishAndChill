using FishAndChill.Collection;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class FishDisplayScaleTests
    {
        [Test]
        public void For_GrowsWithRealSize_BetweenMinAndOne()
        {
            Assert.That(FishDisplayScale.For(5f), Is.EqualTo(FishDisplayScale.MinScale));
            Assert.That(FishDisplayScale.For(200f), Is.EqualTo(1f));
            Assert.That(FishDisplayScale.For(15f), Is.LessThan(FishDisplayScale.For(40f)));
            Assert.That(FishDisplayScale.For(40f), Is.LessThan(FishDisplayScale.For(100f)));
        }

        [Test]
        public void Fit_KeepsProportions_AndStaysInTheBox()
        {
            Vector2 small = FishDisplayScale.Fit(new Vector2(560f, 210f), new Vector2(80f, 40f), 10f);
            Assert.That(small.y / small.x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(small.x, Is.LessThan(560f * 0.5f));

            Vector2 tall = FishDisplayScale.Fit(new Vector2(560f, 210f), new Vector2(70f, 60f), 150f);
            Assert.That(tall.y, Is.EqualTo(210f).Within(0.001f));
            Assert.That(tall.x, Is.LessThan(560f));
        }
    }
}
