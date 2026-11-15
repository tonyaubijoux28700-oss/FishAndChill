using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Gear;
using FishAndChill.Save;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class SaveSerializerTests
    {
        private static readonly FishSpecies Perch = TestFish.Species("perch", 8);
        private static readonly FishSpecies Pike = TestFish.Species("pike", 40);

        private static FishSpecies Find(string id) => id == "perch" ? Perch : id == "pike" ? Pike : null;

        [Test]
        public void SaveThenLoad_GivesBackTheWholeGame()
        {
            var bag = new Inventory(3);
            var aquarium = new AquariumCollection();
            var wallet = new Wallet();
            var gear = new GearLoadout();
            var log = new CatchLog();
            bag.UpgradeCapacity(5);
            bag.TryAdd(TestFish.Fish(Perch, 12f));
            aquarium.Restore(TestFish.Fish(Pike, 80f));
            wallet.Add(137);
            gear.Upgrade(GearCategory.Rod);
            gear.Upgrade(GearCategory.Rod);
            log.Register("perch", 12f);
            log.Register("pike", 80f);
            log.Register("pike", 60f);

            string json = SaveSerializer.ToJson(SaveSerializer.Capture(bag, aquarium, wallet, gear, log));

            var bag2 = new Inventory(3);
            var aquarium2 = new AquariumCollection();
            var wallet2 = new Wallet();
            var gear2 = new GearLoadout();
            var log2 = new CatchLog();
            SaveSerializer.Restore(SaveSerializer.FromJson(json), Find, bag2, aquarium2, wallet2, gear2, log2);

            Assert.That(bag2.Capacity, Is.EqualTo(5));
            Assert.That(bag2.Count, Is.EqualTo(1));
            Assert.That(bag2.Fish[0].Species, Is.SameAs(Perch));
            Assert.That(bag2.Fish[0].SizeCm, Is.EqualTo(12f));
            Assert.That(aquarium2.SpecimenOf("pike").SizeCm, Is.EqualTo(80f));
            Assert.That(wallet2.Coins, Is.EqualTo(137));
            Assert.That(gear2.Level(GearCategory.Rod), Is.EqualTo(2));
            Assert.That(gear2.Level(GearCategory.Hook), Is.EqualTo(0));
            Assert.That(log2.CountOf("pike"), Is.EqualTo(2));
            Assert.That(log2.BestSizeOf("pike"), Is.EqualTo(80f));
            Assert.That(log2.TotalCatches, Is.EqualTo(3));
        }

        [Test]
        public void Load_SkipsSpeciesThatNoLongerExist()
        {
            var data = new SaveData
            {
                BagCapacity = 3,
                Bag = new List<SaveData.Specimen>
                {
                    new SaveData.Specimen { Id = "disparu", SizeCm = 10f },
                    new SaveData.Specimen { Id = "perch", SizeCm = 15f },
                },
            };

            var bag = new Inventory(3);
            SaveSerializer.Restore(data, Find, bag, new AquariumCollection(), new Wallet(), new GearLoadout(), new CatchLog());

            Assert.That(bag.Count, Is.EqualTo(1));
            Assert.That(bag.Fish[0].Species, Is.SameAs(Perch));
        }

        [Test]
        public void FromJson_BrokenOrEmptyText_GivesNull()
        {
            Assert.That(SaveSerializer.FromJson(""), Is.Null);
            Assert.That(SaveSerializer.FromJson("pas du json {"), Is.Null);
        }
    }
}
