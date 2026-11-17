using System;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class BottleMessageTests
    {
        [Test]
        public void Hint_DescribesTheSpecies_WithoutNamingIt()
        {
            FishSpecies species = TestFish.Species("poisson-mystere", 10, 20f, 85f);
            string hint = BottleMessage.Hint(species, s => "Leurre brillant");

            Assert.That(hint, Does.Contain("85 cm"));
            Assert.That(hint, Does.Contain("leurre brillant"));
            Assert.That(hint, Does.Not.Contain("poisson-mystere"));
        }

        [Test]
        public void Write_GivesAHintOnlyAboutUndiscoveredSpecies()
        {
            FishSpecies known = TestFish.Species("deja-vu", 10, 10f, 31f);
            FishSpecies unknown = TestFish.Species("pas-vu", 10, 10f, 77f);
            var log = new CatchLog();
            log.Register(known.Id, 20f);
            var random = new Random(1);
            for (int i = 0; i < 40; i++)
            {
                string message = BottleMessage.Write(new[] { known, unknown }, log, null, random);
                Assert.That(message, Does.Not.Contain("31 cm"), "jamais d'indice sur une espèce déjà pêchée");
                Assert.That(message, Does.StartWith("« "));
                Assert.That(message, Does.Contain("— "), "le message est signé");
            }
        }

        [Test]
        public void Write_AllDiscovered_StillGivesANote()
        {
            FishSpecies known = TestFish.Species("deja-vu-2", 10);
            var log = new CatchLog();
            log.Register(known.Id, 20f);
            string message = BottleMessage.Write(new[] { known }, log, null, new Random(3));
            Assert.That(message.Length, Is.GreaterThan(20));
        }

        [Test]
        public void Coins_StayInRange()
        {
            var random = new Random(5);
            for (int i = 0; i < 200; i++)
            {
                Assert.That(BottleMessage.Coins(random), Is.InRange(BottleMessage.MinCoins, BottleMessage.MaxCoins));
            }
        }
    }
}
