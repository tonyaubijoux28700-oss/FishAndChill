using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Gear;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class TrapTests
    {
        private static Wallet WalletWith(int coins)
        {
            var wallet = new Wallet();
            wallet.Add(coins);
            return wallet;
        }

        [Test]
        public void TryBuy_SpendsCoins_UpToTheMaximum()
        {
            var traps = new TrapSet();
            Wallet wallet = WalletWith(500);
            Assert.That(traps.TryBuy(150, 3, wallet), Is.EqualTo(TrapPurchase.Bought));
            Assert.That(traps.TryBuy(150, 3, wallet), Is.EqualTo(TrapPurchase.Bought));
            Assert.That(traps.TryBuy(150, 3, wallet), Is.EqualTo(TrapPurchase.Bought));
            Assert.That(traps.TryBuy(150, 3, wallet), Is.EqualTo(TrapPurchase.AtMax));
            Assert.That(traps.Owned, Is.EqualTo(3));
            Assert.That(wallet.Coins, Is.EqualTo(50));
            Assert.That(new TrapSet().TryBuy(150, 3, wallet), Is.EqualTo(TrapPurchase.NotEnoughCoins));
            Assert.That(wallet.Coins, Is.EqualTo(50), "pas assez : rien n'est dépensé");
        }

        [Test]
        public void Place_NeedsAFreeTrap_AndLiftGivesItBack()
        {
            var traps = new TrapSet();
            Assert.That(traps.Place("IleMahana", 1f, 2f, 10.0), Is.False, "aucun casier");
            traps.TryBuy(0, 3, new Wallet());
            Assert.That(traps.Place("IleMahana", 1f, 2f, 10.0), Is.True);
            Assert.That(traps.Available, Is.EqualTo(0));
            Assert.That(traps.Place("IleMahana", 5f, 2f, 10.0), Is.False, "le seul casier est déjà posé");
            Assert.That(traps.NearestPlaced(1.5f, 2f, 1f), Is.EqualTo(0));
            Assert.That(traps.NearestPlaced(4f, 2f, 1f), Is.EqualTo(-1));
            PlacedTrap lifted = traps.Lift(0);
            Assert.That(lifted.ZoneId, Is.EqualTo("IleMahana"));
            Assert.That(lifted.PlacedAtHours, Is.EqualTo(10.0));
            Assert.That(traps.Available, Is.EqualTo(1));
        }

        [Test]
        public void Load_KeepsPlacedTraps_AndNeverOwnsFewerThanPlaced()
        {
            var traps = new TrapSet();
            traps.Load(0, new[] { new PlacedTrap { ZoneId = "LacAnnecy", X = 3f, Y = 4f, PlacedAtHours = 30.0 }, null });
            Assert.That(traps.Owned, Is.EqualTo(1));
            Assert.That(traps.Placed.Count, Is.EqualTo(1));
            Assert.That(traps.Available, Is.EqualTo(0));
        }

        [Test]
        public void Fill_GrowsWithTheHoursInWater()
        {
            Assert.That(TrapRules.Fill(0.5, 12f), Is.EqualTo(0f), "tout juste posé : vide");
            Assert.That(TrapRules.Fill(6.0, 12f), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(TrapRules.Fill(40.0, 12f), Is.EqualTo(1f));
            Assert.That(TrapRules.HoursLeft(4.0, 12f), Is.EqualTo(8f).Within(1e-4f));
            Assert.That(TrapRules.HoursLeft(20.0, 12f), Is.EqualTo(0f));
        }

        [Test]
        public void RareCrustaceans_NeedALongWait()
        {
            Assert.That(TrapRules.RarityFactor(Rarity.Rare, 0.2f), Is.LessThan(TrapRules.RarityFactor(Rarity.Rare, 1f)));
            Assert.That(TrapRules.RarityFactor(Rarity.Epic, 0.3f), Is.LessThan(TrapRules.RarityFactor(Rarity.Rare, 0.3f)));
            Assert.That(TrapRules.RarityFactor(Rarity.Common, 0.1f), Is.EqualTo(1f));
        }

        [Test]
        public void Haul_IsEmptyWhenJustPlaced_AndFullerAfterAWait()
        {
            var candidates = new List<string> { "crabe", "langouste" };
            var random = new System.Random(7);
            int early = 0, late = 0;
            for (int i = 0; i < 400; i++)
            {
                List<string> a = TrapRules.Haul(candidates, _ => 1f, _ => Rarity.Common, 0.2f, 3, random);
                List<string> b = TrapRules.Haul(candidates, _ => 1f, _ => Rarity.Common, 1f, 3, random);
                Assert.That(a.Count, Is.InRange(0, 3));
                Assert.That(b.Count, Is.InRange(0, 3));
                early += a.Count;
                late += b.Count;
            }

            Assert.That(late, Is.GreaterThan(early * 2));
            Assert.That(TrapRules.Haul(candidates, _ => 1f, _ => Rarity.Common, 0f, 3, random), Is.Empty);
            Assert.That(TrapRules.Haul(new List<string>(), _ => 1f, _ => Rarity.Common, 1f, 3, random), Is.Empty);
        }

        [Test]
        public void Haul_OnlyPicksTheGivenCandidates_FavoringCommonOnes()
        {
            var candidates = new List<string> { "commun", "epique" };
            var random = new System.Random(3);
            int common = 0, epic = 0;
            for (int i = 0; i < 400; i++)
            {
                foreach (string s in TrapRules.Haul(candidates, _ => 1f, s => s == "commun" ? Rarity.Common : Rarity.Epic, 0.5f, 3, random))
                {
                    if (s == "commun") common++; else epic++;
                }
            }

            Assert.That(common, Is.GreaterThan(epic * 3));
        }

        [Test]
        public void OnlyCrustaceans_AreCaughtInTraps()
        {
            Assert.That(CreatureKind.Crustacean.CaughtInTrap(), Is.True);
            Assert.That(CreatureKind.Fish.CaughtInTrap(), Is.False);
            Assert.That(CreatureKind.Reptile.CaughtInTrap(), Is.False);
        }
    }
}
