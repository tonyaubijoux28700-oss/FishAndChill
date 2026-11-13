using FishAndChill.Fishing;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class QteChallengeTests
    {
        private static QteChallenge Create(int requiredHits, int maxMisses)
        {
            // Zone pleine largeur : chaque appui réussit, sauf si on décale le test.
            var difficulty = new QteDifficulty(cursorSpeed: 1f, zoneWidth: 1f, requiredHits, maxMisses);
            return new QteChallenge(difficulty, new System.Random(42));
        }

        [Test]
        public void Succeeds_AfterRequiredHits()
        {
            var challenge = Create(requiredHits: 3, maxMisses: 1);

            challenge.Press();
            challenge.Press();
            Assert.That(challenge.Status, Is.EqualTo(QteStatus.InProgress));

            challenge.Press();
            Assert.That(challenge.Status, Is.EqualTo(QteStatus.Succeeded));
            Assert.That(challenge.Hits, Is.EqualTo(3));
        }

        [Test]
        public void Fails_WhenMissesExceedTolerance()
        {
            var difficulty = new QteDifficulty(cursorSpeed: 1f, zoneWidth: 0.05f, requiredHits: 3, maxMisses: 1);
            var challenge = new QteChallenge(difficulty, new System.Random(1));

            // Placer le curseur loin de la zone avant chaque appui.
            challenge.Gauge.MoveZone(0.9f);

            Assert.That(challenge.Press(), Is.EqualTo(QtePressResult.Miss));
            Assert.That(challenge.Status, Is.EqualTo(QteStatus.InProgress));

            Assert.That(challenge.Press(), Is.EqualTo(QtePressResult.Miss));
            Assert.That(challenge.Status, Is.EqualTo(QteStatus.Failed));
        }

        [Test]
        public void Press_IsIgnoredOnceFinished()
        {
            var challenge = Create(requiredHits: 1, maxMisses: 0);

            challenge.Press();

            Assert.That(challenge.Press(), Is.EqualTo(QtePressResult.Ignored));
            Assert.That(challenge.Hits, Is.EqualTo(1));
        }

        [Test]
        public void Tick_StopsCursorOnceFinished()
        {
            var challenge = Create(requiredHits: 1, maxMisses: 0);
            challenge.Press();
            float position = challenge.Gauge.Position;

            challenge.Tick(0.3f);

            Assert.That(challenge.Gauge.Position, Is.EqualTo(position));
        }
    }
}
