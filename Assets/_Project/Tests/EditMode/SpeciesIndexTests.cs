using FishAndChill.Collection;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class SpeciesIndexTests
    {
        private static readonly FishSpecies Perch = TestFish.Species("perch", 8);
        private static readonly FishSpecies Pike = TestFish.Species("pike", 40);
        private static readonly FishSpecies Trout = TestFish.Species("trout", 20);

        [Test]
        public void Index_RemovesDuplicatesAndNulls_KeepsOrder()
        {
            var index = new SpeciesIndex(new[] { Pike, null, Perch, Pike, Trout });

            Assert.That(index.Count, Is.EqualTo(3));
            Assert.That(index.Species[0], Is.SameAs(Pike));
            Assert.That(index.Species[1], Is.SameAs(Perch));
            Assert.That(index.Species[2], Is.SameAs(Trout));
        }

        [Test]
        public void DiscoveredCount_CountsCaughtSpeciesOnly()
        {
            var index = new SpeciesIndex(new[] { Perch, Pike, Trout });
            var log = new CatchLog();
            log.Register("perch", 12f);
            log.Register("perch", 20f);
            log.Register("salmon", 50f);

            Assert.That(index.DiscoveredCount(log), Is.EqualTo(1));
        }

        [Test]
        public void CatchLog_KeepsTheBiggestSize()
        {
            var log = new CatchLog();
            log.Register("perch", 12f);
            log.Register("perch", 25f);
            log.Register("perch", 18f);

            Assert.That(log.BestSizeOf("perch"), Is.EqualTo(25f));
            Assert.That(log.BestSizeOf("pike"), Is.EqualTo(0f));
            Assert.That(log.IsDiscovered("perch"), Is.True);
            Assert.That(log.IsDiscovered("pike"), Is.False);
        }

        [Test]
        public void Pages_AreRoundedUp()
        {
            var index = new SpeciesIndex(new[] { Perch, Pike, Trout });

            Assert.That(index.PageCount(2), Is.EqualTo(2));
            Assert.That(index.PageCount(8), Is.EqualTo(1));
            Assert.That(new SpeciesIndex(null).PageCount(8), Is.EqualTo(1));
            Assert.That(SpeciesIndex.PageOf(7, 8), Is.EqualTo(0));
            Assert.That(SpeciesIndex.PageOf(8, 8), Is.EqualTo(1));
        }

        [Test]
        public void Move_StaysInsideTheList()
        {
            // Grille de 4 colonnes, 10 espèces.
            Assert.That(SpeciesIndex.Move(0, 1, 0, 4, 10), Is.EqualTo(1));
            Assert.That(SpeciesIndex.Move(3, 1, 0, 4, 10), Is.EqualTo(4), "droite en bout de ligne : case suivante");
            Assert.That(SpeciesIndex.Move(1, 0, 1, 4, 10), Is.EqualTo(5), "bas : une ligne plus loin");
            Assert.That(SpeciesIndex.Move(7, 0, 1, 4, 10), Is.EqualTo(7), "pas de ligne en dessous");
            Assert.That(SpeciesIndex.Move(0, -1, 0, 4, 10), Is.EqualTo(0));
            Assert.That(SpeciesIndex.Move(9, 1, 0, 4, 10), Is.EqualTo(9));
            Assert.That(SpeciesIndex.Move(2, 0, -1, 4, 10), Is.EqualTo(2));
            Assert.That(SpeciesIndex.Move(5, 0, 0, 4, 0), Is.EqualTo(0));
        }
    }
}
