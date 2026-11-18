using FishAndChill.Collection;
using FishAndChill.Fishing;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class LegendProgressTests
    {
        [Test]
        public void Fragments_AddUp_ThenCompleteTheArtifact()
        {
            var progress = new LegendProgress();
            int changes = 0;
            progress.Changed += () => changes++;

            Assert.That(progress.AddFragment("ulua", 3), Is.EqualTo(1));
            Assert.That(progress.AddFragment("ulua", 3), Is.EqualTo(2));
            Assert.That(progress.State("ulua"), Is.EqualTo(LegendState.Collecting));
            Assert.That(progress.AddFragment("ulua", 3), Is.EqualTo(3));
            Assert.That(progress.State("ulua"), Is.EqualTo(LegendState.Complete));
            Assert.That(changes, Is.EqualTo(3));
        }

        [Test]
        public void CompleteArtifact_TakesNoMoreFragments()
        {
            var progress = new LegendProgress();
            progress.AddFragment("ulua", 1);

            Assert.That(progress.AddFragment("ulua", 1), Is.EqualTo(-1));
            Assert.That(progress.Fragments("ulua"), Is.EqualTo(1));
        }

        [Test]
        public void Summon_OnlyOnceComplete_ThenBeat()
        {
            var progress = new LegendProgress();
            Assert.That(progress.Summon("ulua"), Is.False);
            Assert.That(progress.Beat("ulua"), Is.False);

            progress.AddFragment("ulua", 1);
            Assert.That(progress.Summon("ulua"), Is.True);
            Assert.That(progress.State("ulua"), Is.EqualTo(LegendState.Summoned));
            Assert.That(progress.Beat("ulua"), Is.True);
            Assert.That(progress.State("ulua"), Is.EqualTo(LegendState.Beaten));
        }

        [Test]
        public void Legends_AreIndependent_AndRestorable()
        {
            var progress = new LegendProgress();
            progress.AddFragment("ulua", 4);
            progress.Restore("brochet", 2, LegendState.Collecting);

            Assert.That(progress.Fragments("ulua"), Is.EqualTo(1));
            Assert.That(progress.Fragments("brochet"), Is.EqualTo(2));
            Assert.That(progress.Fragments("inconnu"), Is.EqualTo(0));
            Assert.That(progress.State("inconnu"), Is.EqualTo(LegendState.Collecting));
        }

        [Test]
        public void Drop_IsRare_DoubledInSpots_AndStopsOnceComplete()
        {
            Assert.That(FragmentRules.ChanceFor(0.03f, inSpot: true), Is.EqualTo(0.06f).Within(1e-6f));

            var random = new System.Random(3);
            int drops = 0;
            for (int i = 0; i < 10000; i++)
            {
                if (FragmentRules.ShouldDrop(random, 0.03f, false, LegendState.Collecting)) drops++;
            }

            Assert.That(drops, Is.InRange(220, 380));
            Assert.That(FragmentRules.ShouldDrop(new System.Random(1), 1f, false, LegendState.Complete), Is.False);
            Assert.That(FragmentRules.ShouldDrop(new System.Random(1), 1f, false, LegendState.Beaten), Is.False);
        }
    }
}
