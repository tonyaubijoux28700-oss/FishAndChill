using FishAndChill.Collection;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class FishShopTests
    {
        private static readonly FishSpecies Perch = TestFish.Species("perch", 10, 10f, 30f);
        private static readonly FishSpecies Pike = TestFish.Species("pike", 40, 40f, 120f);

        [Test]
        public void Price_GrowsWithSize()
        {
            Assert.That(FishShop.PriceOf(TestFish.Fish(Perch, 10f)), Is.EqualTo(8));
            Assert.That(FishShop.PriceOf(TestFish.Fish(Perch, 25f)), Is.EqualTo(12));
            Assert.That(FishShop.PriceOf(TestFish.Fish(Perch, 30f)), Is.EqualTo(13));
        }

        [Test]
        public void SingleNewSpecies_IsNotSellable()
        {
            var bag = new Inventory(3);
            bag.TryAdd(TestFish.Fish(Pike, 60f));

            Assert.That(FishShop.IsSellable(bag.Fish, 0, new AquariumCollection()), Is.False);
        }

        [Test]
        public void ExtraCopies_AreSellable_TheBiggestIsKept()
        {
            var bag = new Inventory(3);
            bag.TryAdd(TestFish.Fish(Perch, 12f));
            bag.TryAdd(TestFish.Fish(Perch, 28f));
            bag.TryAdd(TestFish.Fish(Perch, 20f));
            var aquarium = new AquariumCollection();

            Assert.That(FishShop.IsSellable(bag.Fish, 0, aquarium), Is.True);
            Assert.That(FishShop.IsSellable(bag.Fish, 1, aquarium), Is.False, "le plus gros est gardé pour l'aquarium");
            Assert.That(FishShop.IsSellable(bag.Fish, 2, aquarium), Is.True);
        }

        [Test]
        public void SpeciesAlreadyInAquarium_IsAlwaysSellable()
        {
            var aquarium = new AquariumCollection();
            var bag = new Inventory(3);
            bag.TryAdd(TestFish.Fish(Pike, 50f));
            aquarium.Deposit(bag, new Wallet());

            bag.TryAdd(TestFish.Fish(Pike, 100f));

            Assert.That(FishShop.IsSellable(bag.Fish, 0, aquarium), Is.True);
        }

        [Test]
        public void Sell_RemovesTheFishAndPays()
        {
            var bag = new Inventory(3);
            var wallet = new Wallet();
            bag.TryAdd(TestFish.Fish(Perch, 10f));
            bag.TryAdd(TestFish.Fish(Perch, 30f));

            int earned = FishShop.Sell(bag, 0, wallet, new AquariumCollection());

            Assert.That(earned, Is.EqualTo(8));
            Assert.That(wallet.Coins, Is.EqualTo(8));
            Assert.That(bag.Count, Is.EqualTo(1));
            Assert.That(bag.Fish[0].SizeCm, Is.EqualTo(30f));
        }

        [Test]
        public void Sell_RefusesTheFishKeptForTheAquarium()
        {
            var bag = new Inventory(3);
            var wallet = new Wallet();
            bag.TryAdd(TestFish.Fish(Pike, 60f));

            Assert.That(FishShop.Sell(bag, 0, wallet, new AquariumCollection()), Is.EqualTo(0));
            Assert.That(bag.Count, Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.EqualTo(0));
        }

        [Test]
        public void SellAll_SellsEveryDuplicate_KeepsOneOfEachNewSpecies()
        {
            var bag = new Inventory(3);
            var wallet = new Wallet();
            bag.TryAdd(TestFish.Fish(Perch, 10f));
            bag.TryAdd(TestFish.Fish(Pike, 80f));
            bag.TryAdd(TestFish.Fish(Perch, 30f));

            int earned = FishShop.SellAll(bag, wallet, new AquariumCollection());

            Assert.That(earned, Is.EqualTo(8));
            Assert.That(bag.Count, Is.EqualTo(2));
            Assert.That(FishShop.KeeperSlot(bag.Fish, "perch"), Is.GreaterThanOrEqualTo(0));
            Assert.That(FishShop.KeeperSlot(bag.Fish, "pike"), Is.GreaterThanOrEqualTo(0));
        }
    }
}
