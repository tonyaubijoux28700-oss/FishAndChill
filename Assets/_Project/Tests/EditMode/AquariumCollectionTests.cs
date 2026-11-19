using FishAndChill.Collection;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class AquariumCollectionTests
    {
        private static readonly FishSpecies Perch = TestFish.Species("perch", 8);
        private static readonly FishSpecies Pike = TestFish.Species("pike", 45);

        [Test]
        public void Deposit_AddsNewSpeciesAndTakesThemFromTheBag()
        {
            var aquarium = new AquariumCollection();
            var bag = new Inventory(3);
            bag.TryAdd(TestFish.Fish(Perch));
            bag.TryAdd(TestFish.Fish(Pike));

            DepositResult result = aquarium.Deposit(bag);

            Assert.That(result.NewSpecies.Count, Is.EqualTo(2));
            Assert.That(result.NewSpecies[0].Species, Is.SameAs(Perch), "l'ordre du sac est gardé");
            Assert.That(aquarium.SpeciesCount, Is.EqualTo(2));
            Assert.That(bag.Count, Is.EqualTo(0));
        }

        [Test]
        public void Deposit_LeavesDuplicatesInTheBag()
        {
            var aquarium = new AquariumCollection();
            var bag = new Inventory(3);
            bag.TryAdd(TestFish.Fish(Perch, 20f));
            aquarium.Deposit(bag);

            bag.TryAdd(TestFish.Fish(Perch, 12f));
            bag.TryAdd(TestFish.Fish(Perch, 15f));
            DepositResult result = aquarium.Deposit(bag);

            Assert.That(result.IsEmpty, Is.True);
            Assert.That(result.LeftInBag, Is.EqualTo(2));
            Assert.That(bag.Count, Is.EqualTo(2));
            Assert.That(aquarium.SpeciesCount, Is.EqualTo(1));
        }

        [Test]
        public void Deposit_TwoOfANewSpecies_KeepsTheBiggest()
        {
            var aquarium = new AquariumCollection();
            var bag = new Inventory(3);
            bag.TryAdd(TestFish.Fish(Pike, 60f));
            bag.TryAdd(TestFish.Fish(Pike, 90f));
            bag.TryAdd(TestFish.Fish(Perch, 20f));

            DepositResult result = aquarium.Deposit(bag);

            Assert.That(result.NewSpecies.Count, Is.EqualTo(2));
            Assert.That(aquarium.SpecimenOf("pike").SizeCm, Is.EqualTo(90f));
            Assert.That(bag.Count, Is.EqualTo(1));
            Assert.That(bag.Fish[0].SizeCm, Is.EqualTo(60f), "le plus petit brochet reste pour le marchand");
        }

        [Test]
        public void Deposit_BiggerFish_DoesNotReplaceTheSpecimen_StaysInTheBag()
        {
            var aquarium = new AquariumCollection();
            var bag = new Inventory(3);
            bag.TryAdd(TestFish.Fish(Pike, 60f));
            aquarium.Deposit(bag);

            bag.TryAdd(TestFish.Fish(Perch, 15f));
            bag.TryAdd(TestFish.Fish(Pike, 95f));
            DepositResult result = aquarium.Deposit(bag);

            Assert.That(result.NewSpecies.Count, Is.EqualTo(1), "seule la perche entre");
            Assert.That(aquarium.SpecimenOf("pike").SizeCm, Is.EqualTo(60f), "la taille ne compte pas : le premier reste exposé");
            Assert.That(bag.Count, Is.EqualTo(1), "le gros brochet reste dans le sac pour le marchand");
            Assert.That(bag.Fish[0].SizeCm, Is.EqualTo(95f));
            Assert.That(aquarium.SpeciesCount, Is.EqualTo(2));
        }

        [Test]
        public void Deposit_EmptyBag_ChangesNothing()
        {
            var aquarium = new AquariumCollection();
            int changes = 0;
            aquarium.Changed += () => changes++;

            DepositResult result = aquarium.Deposit(new Inventory(3));

            Assert.That(result.IsEmpty, Is.True);
            Assert.That(changes, Is.EqualTo(0));
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
