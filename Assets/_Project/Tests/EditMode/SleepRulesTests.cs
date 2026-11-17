using FishAndChill.World;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class SleepRulesTests
    {
        [Test]
        public void CanSleep_EveningAndNightOnly()
        {
            Assert.That(SleepRules.CanSleep(12f), Is.False, "en plein jour");
            Assert.That(SleepRules.CanSleep(18f), Is.True);
            Assert.That(SleepRules.CanSleep(23.5f), Is.True);
            Assert.That(SleepRules.CanSleep(2f), Is.True);
            Assert.That(SleepRules.CanSleep(6f), Is.False, "trop tard, le jour se lève");
        }

        [Test]
        public void HoursSlept_UntilSevenInTheMorning()
        {
            Assert.That(SleepRules.HoursSlept(22f), Is.EqualTo(9f).Within(0.001f));
            Assert.That(SleepRules.HoursSlept(3f), Is.EqualTo(4f).Within(0.001f));
        }
    }
}
