using FishAndChill.UI;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class WiggleMathTests
    {
        [Test]
        public void Head_StaysStill_TailMovesMost()
        {
            float head = 0f;
            float tail = 0f;
            for (float phase = 0f; phase < 6.3f; phase += 0.1f)
            {
                head = System.Math.Max(head, System.Math.Abs(WiggleMath.Offset(1f, phase, 3f)));
                tail = System.Math.Max(tail, System.Math.Abs(WiggleMath.Offset(0f, phase, 3f)));
            }

            Assert.That(head, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(tail, Is.EqualTo(3f).Within(0.05f));
        }

        [Test]
        public void NoAmplitude_MeansNoMovement()
        {
            Assert.That(WiggleMath.Offset(0.2f, 1.3f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void Flop_IsShortAndRegular()
        {
            Assert.That(WiggleMath.Flop(0.2f), Is.EqualTo(0f), "pas de sursaut tout de suite");
            Assert.That(WiggleMath.Flop(0.85f), Is.GreaterThan(0.9f), "au milieu du premier sursaut");
            Assert.That(WiggleMath.Flop(2f), Is.EqualTo(0f), "calme entre deux sursauts");
            Assert.That(WiggleMath.Flop(0.85f + 2.6f), Is.GreaterThan(0.9f), "il recommence");
        }
    }
}
