using FishAndChill.Collection;
using FishAndChill.Fishing;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public class RingQteTests
    {
        /// <summary>Avance jusqu'à ce que l'anneau touche la bête (bien au centre de la marge).</summary>
        private static void TickUntilTouch(RingQte qte)
        {
            for (int i = 0; i < 2000 && !qte.Done; i++)
            {
                if (!qte.Waiting && System.Math.Abs(qte.Radius - 1f) <= qte.Tolerance * 0.4f) return;
                qte.Tick(0.002f);
            }
        }

        private static void TickPastPause(RingQte qte)
        {
            for (int i = 0; i < 500 && qte.Waiting; i++) qte.Tick(0.002f);
        }

        [Test]
        public void PressingWhenTheRingTouches_CatchesTheCritter()
        {
            var qte = new RingQte(Rarity.Common, CritterBehaviour.Burrow, new System.Random(1));
            Assert.That(qte.Rings, Is.EqualTo(2));
            for (int i = 0; i < qte.Rings; i++)
            {
                TickPastPause(qte);
                TickUntilTouch(qte);
                Assert.That(qte.Press(), Is.EqualTo(RingResult.Hit));
            }

            Assert.That(qte.Success, Is.True);
        }

        [Test]
        public void PressingTooEarly_IsAMiss_AndRareCrittersForgiveNothing()
        {
            var qte = new RingQte(Rarity.Rare, CritterBehaviour.Hide, new System.Random(2));
            TickPastPause(qte);
            qte.Tick(0.05f);
            Assert.That(qte.Press(), Is.EqualTo(RingResult.Miss), "l'anneau est encore loin");
            Assert.That(qte.Failed, Is.True, "une rare s'échappe au premier raté");
        }

        [Test]
        public void ARingThatPassesTheCritter_CountsAsAMiss()
        {
            var qte = new RingQte(Rarity.Common, CritterBehaviour.Dart, new System.Random(3));
            TickPastPause(qte);
            for (int i = 0; i < 200 && qte.Misses == 0; i++) qte.Tick(0.02f);
            Assert.That(qte.Misses, Is.EqualTo(1));
            Assert.That(qte.Failed, Is.False, "une commune pardonne un raté");
        }

        [Test]
        public void RarerCritters_HaveMoreRings_AndAFinerMargin()
        {
            var common = new RingQte(Rarity.Common, CritterBehaviour.Dart, new System.Random(4));
            var legendary = new RingQte(Rarity.Legendary, CritterBehaviour.Dart, new System.Random(4));
            Assert.That(legendary.Rings, Is.GreaterThan(common.Rings));
            Assert.That(legendary.Tolerance, Is.LessThan(common.Tolerance));
        }

        [Test]
        public void InkHidesTheRing_InTheMiddleOfItsCourse()
        {
            var qte = new RingQte(Rarity.Rare, CritterBehaviour.Ink, new System.Random(5));
            TickPastPause(qte);
            Assert.That(qte.Visible, Is.True);
            while (qte.Progress < 0.5f) qte.Tick(0.01f);
            Assert.That(qte.Visible, Is.False);
        }
    }
}
