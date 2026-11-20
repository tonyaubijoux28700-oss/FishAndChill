using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Villagers;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class VillagerRequestTests
    {
        private static FishSpecies WithRarity(string id, Rarity rarity)
        {
            FishSpecies species = TestFish.Species(id, 20);
            typeof(FishSpecies).GetField("_rarity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(species, rarity);
            return species;
        }

        [Test]
        public void PickSpecies_NeverAsksForEpicOrLegendary()
        {
            var epic = WithRarity("epic", Rarity.Epic);
            var legend = WithRarity("legend", Rarity.Legendary);
            var common = WithRarity("common", Rarity.Common);
            var random = new System.Random(3);
            for (int i = 0; i < 50; i++)
            {
                Assert.That(RequestRules.PickSpecies(random, new[] { epic, legend, common }), Is.SameAs(common));
            }

            Assert.That(RequestRules.PickSpecies(random, new[] { epic, legend }), Is.Null);
        }

        [Test]
        public void PickSpecies_CommonComesUpMoreThanRare()
        {
            var common = WithRarity("common", Rarity.Common);
            var rare = WithRarity("rare", Rarity.Rare);
            var random = new System.Random(11);
            int commons = 0;
            for (int i = 0; i < 400; i++)
            {
                if (RequestRules.PickSpecies(random, new[] { common, rare, common }) == common) commons++;
            }

            Assert.That(commons, Is.InRange(250, 350), "3 contre 1 : environ 300 fois sur 400 (doublon ignoré)");
        }

        [Test]
        public void Reward_IsTwiceThePricePlusARarityBonus()
        {
            Assert.That(RequestRules.Reward(20, Rarity.Common), Is.EqualTo(45));
            Assert.That(RequestRules.Reward(20, Rarity.Uncommon), Is.EqualTo(55));
            Assert.That(RequestRules.Reward(20, Rarity.Rare), Is.EqualTo(80));
            Assert.That(RequestRules.Reward(0, Rarity.Common), Is.EqualTo(10));
        }

        [Test]
        public void DeliverySlot_GivesTheSmallest_AndKeepsAFirstCatchForTheAquarium()
        {
            var perch = TestFish.Species("perch", 8);
            var pike = TestFish.Species("pike", 40);
            var bag = new List<CaughtFish> { TestFish.Fish(pike, 60f), TestFish.Fish(perch, 25f), TestFish.Fish(perch, 18f) };

            Assert.That(RequestRules.DeliverySlot(bag, "perch", inAquarium: false), Is.EqualTo(2), "deux perches : on donne la plus petite");
            Assert.That(RequestRules.DeliverySlot(bag, "pike", inAquarium: false), Is.EqualTo(RequestRules.KeepForAquarium));
            Assert.That(RequestRules.DeliverySlot(bag, "pike", inAquarium: true), Is.EqualTo(0));
            Assert.That(RequestRules.DeliverySlot(bag, "carp", inAquarium: true), Is.EqualTo(RequestRules.NoFish));
        }

        [Test]
        public void Requests_AreSavedAndReloaded()
        {
            VillagerRequests.Load(null);
            VillagerRequest request = VillagerRequests.Get("tavita");
            request.SpeciesId = "perch";
            request.Asked = true;
            List<VillagerRequest> saved = VillagerRequests.Capture();

            VillagerRequests.Load(null);
            Assert.That(VillagerRequests.Get("tavita").IsOpen, Is.False);
            VillagerRequests.Load(saved);
            Assert.That(VillagerRequests.Get("tavita").SpeciesId, Is.EqualTo("perch"));
            Assert.That(VillagerRequests.Get("tavita").Asked, Is.True);
            VillagerRequests.Load(null);
        }
    }
}
