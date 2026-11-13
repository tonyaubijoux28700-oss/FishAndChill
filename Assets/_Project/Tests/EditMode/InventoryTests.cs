using FishAndChill.Collection;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class InventoryTests
    {
        private static readonly FishSpecies Perch = TestFish.Species("perch", 8);

        [Test]
        public void StartsEmpty_WithGivenCapacity()
        {
            var bag = new Inventory(3);

            Assert.That(bag.Capacity, Is.EqualTo(3));
            Assert.That(bag.Count, Is.EqualTo(0));
            Assert.That(bag.IsFull, Is.False);
        }

        [Test]
        public void TryAdd_RefusesWhenFull()
        {
            var bag = new Inventory(3);

            Assert.That(bag.TryAdd(TestFish.Fish(Perch)), Is.True);
            Assert.That(bag.TryAdd(TestFish.Fish(Perch)), Is.True);
            Assert.That(bag.TryAdd(TestFish.Fish(Perch)), Is.True);
            Assert.That(bag.IsFull, Is.True);
            Assert.That(bag.TryAdd(TestFish.Fish(Perch)), Is.False);
            Assert.That(bag.Count, Is.EqualTo(3));
        }

        [Test]
        public void Replace_ReleasesTheChosenFish()
        {
            var bag = new Inventory(2);
            CaughtFish small = TestFish.Fish(Perch, 10f);
            CaughtFish medium = TestFish.Fish(Perch, 20f);
            CaughtFish big = TestFish.Fish(Perch, 40f);
            bag.TryAdd(small);
            bag.TryAdd(medium);

            CaughtFish released = bag.Replace(0, big);

            Assert.That(released, Is.SameAs(small));
            Assert.That(bag.Fish[0], Is.SameAs(big));
            Assert.That(bag.Fish[1], Is.SameAs(medium));
        }

        [Test]
        public void TakeAll_EmptiesTheBag()
        {
            var bag = new Inventory(3);
            bag.TryAdd(TestFish.Fish(Perch));
            bag.TryAdd(TestFish.Fish(Perch));

            Assert.That(bag.TakeAll().Count, Is.EqualTo(2));
            Assert.That(bag.Count, Is.EqualTo(0));
        }

        [Test]
        public void UpgradeCapacity_OnlyGrows()
        {
            var bag = new Inventory(3);

            bag.UpgradeCapacity(5);
            bag.UpgradeCapacity(4);

            Assert.That(bag.Capacity, Is.EqualTo(5));
        }

        [Test]
        public void Changed_IsRaisedOnEveryModification()
        {
            var bag = new Inventory(3);
            int calls = 0;
            bag.Changed += () => calls++;

            bag.TryAdd(TestFish.Fish(Perch));
            bag.Replace(0, TestFish.Fish(Perch));
            bag.TakeAll();

            Assert.That(calls, Is.EqualTo(3));
        }
    }
}
