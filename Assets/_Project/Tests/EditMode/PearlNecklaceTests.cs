using System.Collections.Generic;
using FishAndChill.Collection;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class PearlNecklaceTests
    {
        [Test]
        public void AZone_IsComplete_OnlyWhenEveryCritterIsInTheNotebook()
        {
            var ids = new List<string> { "crabe", "moule" };
            Assert.That(PearlNecklace.ZoneComplete(ids, id => id == "crabe"), Is.False);
            Assert.That(PearlNecklace.ZoneComplete(ids, id => true), Is.True);
            Assert.That(PearlNecklace.ZoneComplete(new List<string>(), id => true), Is.False, "zone sans bestiole : pas de perle");
        }

        [Test]
        public void APearl_IsFoundOnce_ThenThreadedByChiyo()
        {
            var necklace = new PearlNecklace();
            Assert.That(necklace.StateOf("BaieJaponaise"), Is.EqualTo(PearlState.Missing));
            Assert.That(necklace.Find("BaieJaponaise"), Is.True);
            Assert.That(necklace.Find("BaieJaponaise"), Is.False, "une seule perle par zone");
            Assert.That(necklace.StateOf("BaieJaponaise"), Is.EqualTo(PearlState.Found));

            Assert.That(necklace.ThreadAll(), Is.EqualTo(new[] { "BaieJaponaise" }));
            Assert.That(necklace.StateOf("BaieJaponaise"), Is.EqualTo(PearlState.Threaded));
            Assert.That(necklace.ThreadAll(), Is.Empty);
            Assert.That(necklace.Find("BaieJaponaise"), Is.False, "déjà sur le collier");
            Assert.That(necklace.ThreadedCount, Is.EqualTo(1));
        }

        [Test]
        public void Restore_KeepsBothLists()
        {
            var necklace = new PearlNecklace();
            necklace.Restore(new[] { "LacAnnecy" }, new[] { "IleMahana" });
            Assert.That(necklace.StateOf("LacAnnecy"), Is.EqualTo(PearlState.Found));
            Assert.That(necklace.StateOf("IleMahana"), Is.EqualTo(PearlState.Threaded));
            Assert.That(necklace.StateOf("Maldives"), Is.EqualTo(PearlState.Missing));
        }
    }
}
