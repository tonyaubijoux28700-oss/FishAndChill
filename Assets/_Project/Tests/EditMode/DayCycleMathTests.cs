using FishAndChill.Collection;
using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class DayCycleMathTests
    {
        [Test]
        public void Advance_ADayLastsSecondsPerDay_AndWraps()
        {
            Assert.That(DayCycleMath.Advance(8f, 70f, 840f), Is.EqualTo(10f).Within(0.0001f));
            Assert.That(DayCycleMath.Advance(23f, 70f, 840f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void SkyColor_IsWhiteAtNoon_AndDarkBlueAtMidnight()
        {
            Assert.That(DayCycleMath.SkyColor(12f), Is.EqualTo(Color.white));
            Color night = DayCycleMath.SkyColor(0f);
            Assert.That(night.b, Is.GreaterThan(night.r));
            Assert.That(night.r, Is.LessThan(0.5f));
        }

        [Test]
        public void LampLevel_OffByDay_OnAtNight_FadesAtTwilight()
        {
            Assert.That(DayCycleMath.LampLevel(12f), Is.EqualTo(0f));
            Assert.That(DayCycleMath.LampLevel(23f), Is.EqualTo(1f));
            Assert.That(DayCycleMath.LampLevel(2f), Is.EqualTo(1f));
            Assert.That(DayCycleMath.LampLevel(18.75f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(DayCycleMath.LampLevel(6.25f), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void IsActive_DayAndNightOverlapAtTwilight()
        {
            Assert.That(DayCycleMath.IsActive(ActiveTime.Day, 12f), Is.True);
            Assert.That(DayCycleMath.IsActive(ActiveTime.Day, 22f), Is.False);
            Assert.That(DayCycleMath.IsActive(ActiveTime.Night, 22f), Is.True);
            Assert.That(DayCycleMath.IsActive(ActiveTime.Night, 12f), Is.False);
            Assert.That(DayCycleMath.IsActive(ActiveTime.Day, 19.5f) && DayCycleMath.IsActive(ActiveTime.Night, 19.5f), Is.True);
            Assert.That(DayCycleMath.IsActive(ActiveTime.Always, 3f), Is.True);
        }

        [Test]
        public void TimeText_ShowsHoursAndMinutes()
        {
            Assert.That(DayCycleMath.TimeText(7.0834f), Is.EqualTo("7h05"));
            Assert.That(DayCycleMath.TimeText(21.5f), Is.EqualTo("21h30"));
        }
    }
}
