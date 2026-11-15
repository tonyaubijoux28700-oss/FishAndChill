using System;
using FishAndChill.Fishing;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class NibblePlannerTests
    {
        [Test]
        public void Plan_GivesOneToThreeNibbles_SpacedAndBeforeTheBite()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                float biteTime = 2.5f + seed % 7 * 0.3f;
                float[] times = NibblePlanner.Plan(new Random(seed), biteTime);

                Assert.That(times.Length, Is.InRange(1, 3), $"graine {seed}");
                Assert.That(times[0], Is.GreaterThanOrEqualTo(NibblePlanner.FirstDelay - 0.0001f));
                for (int i = 1; i < times.Length; i++)
                {
                    Assert.That(times[i] - times[i - 1], Is.GreaterThanOrEqualTo(NibblePlanner.MinGap - 0.0001f), $"graine {seed}");
                }

                Assert.That(biteTime - times[times.Length - 1], Is.GreaterThanOrEqualTo(NibblePlanner.MinGap - 0.0001f), $"graine {seed}");
            }
        }

        [Test]
        public void Plan_ShortWait_ReducesTheNumberOfNibbles()
        {
            float biteTime = NibblePlanner.MinimumWait(1) + 0.01f;   // place pour un seul titillement

            for (int seed = 0; seed < 50; seed++)
            {
                Assert.That(NibblePlanner.Plan(new Random(seed), biteTime).Length, Is.EqualTo(1));
            }
        }

        [Test]
        public void MinimumWait_GrowsWithTheNumberOfNibbles()
        {
            Assert.That(NibblePlanner.MinimumWait(3), Is.GreaterThan(NibblePlanner.MinimumWait(1)));
        }
    }
}
