using FishAndChill.Collection;
using FishAndChill.World;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class WeatherMathTests
    {
        [Test]
        public void Pick_FollowsTheClimate()
        {
            var random = new System.Random(3);
            var onlyRain = new WeatherClimate(0f, 0f, 1f, 0f, 0f);
            for (int i = 0; i < 50; i++)
            {
                Assert.That(WeatherMath.Pick(random, onlyRain), Is.EqualTo(Weather.Rain));
            }

            var rainy = new WeatherClimate(10f, 0f, 90f, 0f, 0f);
            int rain = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (WeatherMath.Pick(random, rainy) == Weather.Rain) rain++;
            }

            Assert.That(rain, Is.InRange(850, 950));
        }

        [Test]
        public void Pick_EmptyClimate_IsClear()
        {
            Assert.That(WeatherMath.Pick(new System.Random(1), new WeatherClimate(0f, 0f, 0f, 0f, 0f)), Is.EqualTo(Weather.Clear));
            Assert.That(WeatherMath.Pick(new System.Random(1), null), Is.EqualTo(Weather.Clear));
        }

        [Test]
        public void Duration_StaysInRange()
        {
            var random = new System.Random(7);
            for (int i = 0; i < 100; i++)
            {
                Assert.That(WeatherMath.Duration(random), Is.InRange(WeatherMath.MinHours, WeatherMath.MaxHours));
            }
        }

        [Test]
        public void Allows_RainSpeciesAlsoBiteDuringAStorm()
        {
            Assert.That(WeatherMath.Allows(WeatherNeed.Rain, Weather.Rain), Is.True);
            Assert.That(WeatherMath.Allows(WeatherNeed.Rain, Weather.Storm), Is.True);
            Assert.That(WeatherMath.Allows(WeatherNeed.Rain, Weather.Clear), Is.False);
            Assert.That(WeatherMath.Allows(WeatherNeed.Storm, Weather.Rain), Is.False);
            Assert.That(WeatherMath.Allows(WeatherNeed.Fog, Weather.Fog), Is.True);
            Assert.That(WeatherMath.Allows(WeatherNeed.Clear, Weather.Cloudy), Is.False);
            foreach (Weather w in WeatherMath.All)
            {
                Assert.That(WeatherMath.Allows(WeatherNeed.Any, w), Is.True);
            }
        }

        [Test]
        public void BadWeather_DarkensTheSky_AndSpeedsUpBites()
        {
            Assert.That(WeatherMath.SkyTint(Weather.Storm).r, Is.LessThan(WeatherMath.SkyTint(Weather.Rain).r));
            Assert.That(WeatherMath.SkyTint(Weather.Rain).r, Is.LessThan(WeatherMath.SkyTint(Weather.Clear).r));
            Assert.That(WeatherMath.BiteWaitFactor(Weather.Storm), Is.LessThan(WeatherMath.BiteWaitFactor(Weather.Clear)));
            Assert.That(WeatherMath.RainAmount(Weather.Clear), Is.EqualTo(0f));
            Assert.That(WeatherMath.RainAmount(Weather.Storm), Is.EqualTo(1f));
        }
    }
}
