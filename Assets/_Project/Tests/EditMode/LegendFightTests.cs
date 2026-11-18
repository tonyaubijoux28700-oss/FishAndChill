using FishAndChill.Fishing;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class LegendFightTests
    {
        // Zone pleine largeur : chaque appui réussit ; zone minuscule déplacée loin du curseur : chaque appui rate.
        private static LegendFight Create(params int[] hitsPerPhase)
        {
            var phases = new LegendPhase[hitsPerPhase.Length];
            for (int i = 0; i < phases.Length; i++)
            {
                phases[i] = new LegendPhase(FightStyle.Calm, new QteDifficulty(1f, 1f, hitsPerPhase[i], 1), $"Phase {i + 1}");
            }

            var random = new System.Random(4);
            return new LegendFight(phases, phase => new QteChallenge(phase.Difficulty, random, phase.Style));
        }

        private static void Hit(LegendFight fight, int times)
        {
            for (int i = 0; i < times; i++) fight.Press();
        }

        [Test]
        public void WinningEveryPhase_WinsTheFight_AndStaminaReachesZero()
        {
            LegendFight fight = Create(2, 3, 2);
            Assert.That(fight.Stamina, Is.EqualTo(1f));

            Hit(fight, 2);
            Assert.That(fight.Phase, Is.EqualTo(1));
            Assert.That(fight.InBreak, Is.True);
            Assert.That(fight.Stamina, Is.EqualTo(1f - 2f / 7f).Within(1e-5f));

            fight.Tick(LegendFight.BreakSeconds + 0.01f);
            Hit(fight, 3);
            fight.Tick(LegendFight.BreakSeconds + 0.01f);
            Assert.That(fight.Phase, Is.EqualTo(2));
            Hit(fight, 2);

            Assert.That(fight.Status, Is.EqualTo(QteStatus.Succeeded));
            Assert.That(fight.Stamina, Is.EqualTo(0f));
        }

        [Test]
        public void PressesDuringTheBreak_DoNotCount()
        {
            LegendFight fight = Create(1, 2);
            Hit(fight, 1);
            Assert.That(fight.InBreak, Is.True);

            Assert.That(fight.Press(), Is.EqualTo(QtePressResult.Ignored));
            Assert.That(fight.Challenge.Hits, Is.EqualTo(0));
        }

        [Test]
        public void FailingAPhase_LosesTheWholeFight()
        {
            // Deuxième phase : zone minuscule, qu'on éloigne du curseur ; deux ratés et la légende s'échappe.
            var phases = new[]
            {
                new LegendPhase(FightStyle.Calm, new QteDifficulty(1f, 1f, 1, 1), "Il fonce"),
                new LegendPhase(FightStyle.Calm, new QteDifficulty(1f, 0.05f, 3, 1), "Il plonge"),
            };
            var random = new System.Random(4);
            var fight = new LegendFight(phases, phase => new QteChallenge(phase.Difficulty, random, phase.Style));
            Hit(fight, 1);
            fight.Tick(LegendFight.BreakSeconds + 0.01f);

            fight.Challenge.Gauge.MoveZone(fight.Challenge.Gauge.Position < 0.5f ? 0.95f : 0.05f);
            fight.Press();
            fight.Press();

            Assert.That(fight.Status, Is.EqualTo(QteStatus.Failed));
            Assert.That(fight.FailReason, Is.EqualTo(QteFailReason.Escaped));
            Assert.That(fight.Press(), Is.EqualTo(QtePressResult.Ignored));
        }
    }
}
