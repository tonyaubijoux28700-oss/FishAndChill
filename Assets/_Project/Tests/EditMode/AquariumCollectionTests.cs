using FishAndChill.Collection;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class AquariumCollectionTests
    {
        private static readonly FishSpecies Perch = TestFish.Species("perch", 8);
        private static readonly FishSpecies Pike = TestFish.Species("pike", 45);

        [Test]
        public void Deposit_AddsNewSpeciesAndEmptiesTheBag()
        {
            var aquarium = new AquariumCollection();
            var bag = new Inventory(3);
            var wallet = new Wallet();
            bag.TryAdd(TestFish.Fish(Perch));
            bag.TryAdd(TestFish.Fish(Pike));

            DepositResult result = aquarium.Deposit(bag, wallet);

            Assert.That(result.NewSpecies.Count, Is.EqualTo(2));
            Assert.That(result.Sold, Is.Empty);
            Assert.That(aquarium.SpeciesCount, Is.EqualTo(2));
            Assert.That(bag.Count, Is.EqualTo(0));
            Assert.That(wallet.Coins, Is.EqualTo(0));
        }

        [Test]
        public void Deposit_SellsDuplicates()
        {
            var aquarium = new AquariumCollection();
            var wallet = new Wallet();
            var bag = new Inventory(3);
            bag.TryAdd(TestFish.Fish(Perch));
            aquarium.Deposit(bag, wallet);

            bag.TryAdd(TestFish.Fish(Perch));
            bag.TryAdd(TestFish.Fish(Perch));
            DepositResult result = aquarium.Deposit(bag, wallet);

            Assert.That(result.NewSpecies, Is.Empty);
            Assert.That(result.Sold.Count, Is.EqualTo(2));
            Assert.That(result.CoinsEarned, Is.EqualTo(16));
            Assert.That(wallet.Coins, Is.EqualTo(16));
            Assert.That(aquarium.SpeciesCount, Is.EqualTo(1));
        }

        [Test]
        public void Deposit_TwoOfANewSpecies_KeepsOneSellsTheOther()
        {
            var aquarium = new AquariumCollection();
            var wallet = new Wallet();
            var bag = new Inventory(3);
            bag.TryAdd(TestFish.Fish(Pike, 60f));
            bag.TryAdd(TestFish.Fish(Pike, 90f));

            DepositResult result = aquarium.Deposit(bag, wallet);

            Assert.That(result.NewSpecies.Count, Is.EqualTo(1));
            Assert.That(result.Sold.Count, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.EqualTo(45));
            Assert.That(aquarium.Contains("pike"), Is.True);
        }

        [Test]
        public void Deposit_EmptyBag_ChangesNothing()
        {
            var aquarium = new AquariumCollection();
            var wallet = new Wallet();

            DepositResult result = aquarium.Deposit(new Inventory(3), wallet);

            Assert.That(result.IsEmpty, Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(0));
        }

        [Test]
        public void Wallet_CannotSpendMoreThanItHas()
        {
            var wallet = new Wallet();
            wallet.Add(10);

            Assert.That(wallet.TrySpend(15), Is.False);
            Assert.That(wallet.TrySpend(4), Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(6));
        }
    }
}
