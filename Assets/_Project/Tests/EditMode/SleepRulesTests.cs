using FishAndChill.World;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class SleepRulesTests
    {
        [Test]
        public void Night_FromSixPmToSevenAm()
        {
            Assert.That(SleepRules.IsNight(12f), Is.False, "en plein jour");
            Assert.That(SleepRules.IsNight(18f), Is.True);
            Assert.That(SleepRules.IsNight(23.5f), Is.True);
            Assert.That(SleepRules.IsNight(6.5f), Is.True);
            Assert.That(SleepRules.IsNight(7f), Is.False);
        }

        [Test]
        public void HoursSlept_UntilMorning_OrUntilEveningForANap()
        {
            Assert.That(SleepRules.HoursSlept(22f), Is.EqualTo(9f).Within(0.001f));
            Assert.That(SleepRules.HoursSlept(3f), Is.EqualTo(4f).Within(0.001f));
            Assert.That(SleepRules.HoursSlept(13f), Is.EqualTo(5f).Within(0.001f), "sieste jusqu'à 18 h");
            Assert.That(SleepRules.WakeFor(13f), Is.EqualTo(18f));
        }

        [Test]
        public void HourDuring_RunsTheClockAcrossMidnight()
        {
            Assert.That(SleepRules.HourDuring(22f, 0f), Is.EqualTo(22f).Within(0.001f));
            Assert.That(SleepRules.HourDuring(22f, 0.5f), Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(SleepRules.HourDuring(22f, 1f), Is.EqualTo(7f).Within(0.001f));
        }
    }
}
