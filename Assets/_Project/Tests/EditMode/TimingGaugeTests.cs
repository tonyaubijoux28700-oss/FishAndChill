using FishAndChill.Fishing;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class TimingGaugeTests
    {
        [Test]
        public void Tick_MovesCursorForward()
        {
            var gauge = new TimingGauge(speed: 1f, zoneWidth: 0.2f);

            gauge.Tick(0.25f);

            Assert.That(gauge.Position, Is.EqualTo(0.25f).Within(1e-5f));
        }

        [Test]
        public void Tick_BouncesAtTheEnd()
        {
            var gauge = new TimingGauge(speed: 1f, zoneWidth: 0.2f);

            gauge.Tick(1.25f);

            Assert.That(gauge.Position, Is.EqualTo(0.75f).Within(1e-5f));
        }

        [Test]
        public void Tick_HandlesSeveralBouncesInOneFrame()
        {
            var gauge = new TimingGauge(speed: 1f, zoneWidth: 0.2f);

            gauge.Tick(2.5f);

            Assert.That(gauge.Position, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void IsInZone_TrueOnlyInsideZone()
        {
            var gauge = new TimingGauge(speed: 1f, zoneWidth: 0.2f, zoneCenter: 0.5f);

            gauge.Tick(0.45f);
            Assert.That(gauge.IsInZone, Is.True);

            gauge.Tick(0.2f);
            Assert.That(gauge.IsInZone, Is.False);
        }

        [Test]
        public void Zone_StaysInsideTheBar()
        {
            var gauge = new TimingGauge(speed: 1f, zoneWidth: 0.2f, zoneCenter: 0.99f);
            Assert.That(gauge.ZoneMax, Is.LessThanOrEqualTo(1f));

            gauge.MoveZone(0f);
            Assert.That(gauge.ZoneMin, Is.GreaterThanOrEqualTo(0f));
        }
    }
}
