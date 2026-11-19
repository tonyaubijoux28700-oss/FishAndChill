using FishAndChill.World;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class LegendStormMathTests
    {
        [Test]
        public void Target_GrowsAsYouApproachTheAwakeBoss()
        {
            Assert.That(LegendStormMath.Target(true, 100f, false), Is.EqualTo(0f));
            Assert.That(LegendStormMath.Target(true, LegendStormMath.FarDistance, false), Is.EqualTo(0f));
            float mid = LegendStormMath.Target(true, (LegendStormMath.NearDistance + LegendStormMath.FarDistance) * 0.5f, false);
            Assert.That(mid, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(LegendStormMath.Target(true, 2f, false), Is.EqualTo(1f));
        }

        [Test]
        public void Target_NoBossAwake_NoStorm_ButFullDuringTheFight()
        {
            Assert.That(LegendStormMath.Target(false, 1f, false), Is.EqualTo(0f));
            Assert.That(LegendStormMath.Target(false, 100f, true), Is.EqualTo(1f));
        }

        [Test]
        public void Approach_RisesFasterThanItFalls()
        {
            float up = LegendStormMath.Approach(0f, 1f, 1f);
            float down = 1f - LegendStormMath.Approach(1f, 0f, 1f);
            Assert.That(up, Is.GreaterThan(down));
            Assert.That(LegendStormMath.Approach(0.9f, 1f, 10f), Is.EqualTo(1f));
        }

        [Test]
        public void Lightning_IsMuchMoreFrequent_AtTheHeartOfTheStorm()
        {
            Assert.That(LegendStormMath.LightningInterval(1f, 1f), Is.LessThan(LegendStormMath.LightningInterval(0f, 0f)));
            Assert.That(LegendStormMath.StrikesBolt(0.2f), Is.False);
            Assert.That(LegendStormMath.StrikesBolt(0.8f), Is.True);
        }
    }
}
