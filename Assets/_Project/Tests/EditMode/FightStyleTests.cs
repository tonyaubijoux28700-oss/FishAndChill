using FishAndChill.Fishing;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class FightStyleTests
    {
        private static QteChallenge Create(FightStyle style, float zoneWidth = 0.2f, int hits = 3, int misses = 2)
        {
            return new QteChallenge(new QteDifficulty(1f, zoneWidth, hits, misses), new System.Random(7), style);
        }

        [Test]
        public void Calm_KeepsTheZoneStill()
        {
            QteChallenge challenge = Create(FightStyle.Calm);
            float center = challenge.Gauge.ZoneCenter;
            for (int i = 0; i < 100; i++) challenge.Tick(0.05f);
            Assert.That(challenge.Gauge.ZoneCenter, Is.EqualTo(center));
            Assert.That(challenge.Gauge.SpeedMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void Nervous_ChangesTheCursorSpeed()
        {
            QteChallenge challenge = Create(FightStyle.Nervous);
            float min = 9f;
            float max = 0f;
            for (int i = 0; i < 60; i++)
            {
                challenge.Tick(0.03f);
                min = UnityEngine.Mathf.Min(min, challenge.Gauge.SpeedMultiplier);
                max = UnityEngine.Mathf.Max(max, challenge.Gauge.SpeedMultiplier);
            }

            Assert.That(min, Is.LessThan(0.8f));
            Assert.That(max, Is.GreaterThan(1.2f));
        }

        [Test]
        public void Drifting_SlidesTheZone_AndStaysInTheBar()
        {
            QteChallenge challenge = Create(FightStyle.Drifting);
            float center = challenge.Gauge.ZoneCenter;
            challenge.Tick(0.5f);
            Assert.That(challenge.Gauge.ZoneCenter, Is.Not.EqualTo(center));
            for (int i = 0; i < 200; i++)
            {
                challenge.Tick(0.05f);
                Assert.That(challenge.Gauge.ZoneMin, Is.GreaterThanOrEqualTo(-0.0001f));
                Assert.That(challenge.Gauge.ZoneMax, Is.LessThanOrEqualTo(1.0001f));
            }
        }

        [Test]
        public void Rusher_WarnsThenJumps()
        {
            QteChallenge challenge = Create(FightStyle.Rusher);
            bool warned = false;
            for (int i = 0; i < 70 && challenge.Rushes == 0; i++)
            {
                warned |= challenge.RushWarning;
                challenge.Tick(0.05f);
            }

            Assert.That(challenge.Rushes, Is.EqualTo(1));
            Assert.That(warned, Is.True);
        }

        [Test]
        public void Heavy_TensionRises_AndBreaksTheLine()
        {
            QteChallenge challenge = Create(FightStyle.Heavy);
            challenge.Tick(2f);
            Assert.That(challenge.Tension, Is.EqualTo(QteChallenge.TensionRise * 2f).Within(0.001f));
            for (int i = 0; i < 200 && challenge.Status == QteStatus.InProgress; i++) challenge.Tick(0.1f);
            Assert.That(challenge.Status, Is.EqualTo(QteStatus.Failed));
            Assert.That(challenge.FailReason, Is.EqualTo(QteFailReason.LineBroke));
        }

        [Test]
        public void Heavy_HitRelievesTension_MissAddsIt()
        {
            // Zone pleine largeur : chaque appui réussit.
            QteChallenge hit = Create(FightStyle.Heavy, zoneWidth: 1f, hits: 5);
            hit.Tick(4f);
            float before = hit.Tension;
            hit.Press();
            Assert.That(hit.Tension, Is.EqualTo(before - QteChallenge.TensionHitRelief).Within(0.001f));

            QteChallenge miss = Create(FightStyle.Heavy, zoneWidth: 0.05f, misses: 9);
            miss.Gauge.MoveZone(0.95f);
            float start = miss.Tension;
            miss.Press(); // curseur à 0 : hors de la zone
            Assert.That(miss.Tension, Is.EqualTo(start + QteChallenge.TensionMissPenalty).Within(0.001f));
        }

        [Test]
        public void TooManyMisses_MeansEscaped()
        {
            QteChallenge challenge = Create(FightStyle.Calm, zoneWidth: 0.05f, misses: 0);
            challenge.Gauge.MoveZone(0.95f);
            challenge.Press();
            Assert.That(challenge.FailReason, Is.EqualTo(QteFailReason.Escaped));
        }
    }
}
