using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Gear;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class BaitTests
    {
        [Test]
        public void Bag_ConsumesUses_ThenFallsBackToTheDefault()
        {
            var bag = new BaitBag();
            bag.Add("pain", 0);
            bag.Add("ver", 2);
            Assert.That(bag.Equip("ver"), Is.True);
            Assert.That(bag.ConsumeEquipped("pain"), Is.False);
            Assert.That(bag.Count("ver"), Is.EqualTo(1));
            Assert.That(bag.ConsumeEquipped("pain"), Is.True);
            Assert.That(bag.Owns("ver"), Is.False);
            Assert.That(bag.Equipped, Is.EqualTo("pain"));
        }

        [Test]
        public void Bag_UnlimitedBaitNeverRunsOut_AndPacksAddUp()
        {
            var bag = new BaitBag();
            bag.Add("leurre", 0);
            for (int i = 0; i < 50; i++) bag.ConsumeEquipped("leurre");
            Assert.That(bag.Count("leurre"), Is.EqualTo(BaitBag.Unlimited));

            bag.Add("ver", 15);
            bag.Add("ver", 15);
            Assert.That(bag.Count("ver"), Is.EqualTo(30));
        }

        [Test]
        public void Bag_CyclesThroughOwnedBaits()
        {
            var bag = new BaitBag();
            bag.Add("a", 0);
            bag.Add("b", 3);
            bag.Add("c", 3);
            bag.Cycle(1);
            Assert.That(bag.Equipped, Is.EqualTo("b"));
            bag.Cycle(-1);
            bag.Cycle(-1);
            Assert.That(bag.Equipped, Is.EqualTo("c"));
        }

        [Test]
        public void Bag_RestoresASavedState()
        {
            var bag = new BaitBag();
            bag.Restore(new[] { new KeyValuePair<string, int>("pain", -1), new KeyValuePair<string, int>("ver", 4) }, "ver");
            Assert.That(bag.Equipped, Is.EqualTo("ver"));
            Assert.That(bag.Count("ver"), Is.EqualTo(4));
        }

        [Test]
        public void Bait_FavoursItsSpecies_AndNightOrRareBonuses()
        {
            FishSpecies pike = TestFish.Species("bait-pike", 10);
            JsonUtility.FromJsonOverwrite("{\"_rarity\":2}", pike);
            FishSpecies roach = TestFish.Species("bait-roach", 5);
            FishSpecies burbot = TestFish.Species("bait-burbot", 30);
            JsonUtility.FromJsonOverwrite("{\"_activeTime\":2}", burbot);

            var lure = ScriptableObject.CreateInstance<Bait>();
            lure.hideFlags = HideFlags.HideAndDontSave;
            lure.Configure("Leurre", string.Empty, 100, 0, new[] { pike }, favoritesOnly: true);
            Assert.That(lure.Multiplier(pike, false), Is.EqualTo(3f));
            Assert.That(lure.Multiplier(roach, false), Is.EqualTo(0.5f));

            var glow = ScriptableObject.CreateInstance<Bait>();
            glow.hideFlags = HideFlags.HideAndDontSave;
            glow.Configure("Lumineux", string.Empty, 40, 10, new FishSpecies[0], rareBonus: 2f, nightBonus: 3f);
            Assert.That(glow.Multiplier(burbot, true), Is.EqualTo(3f));
            Assert.That(glow.Multiplier(burbot, false), Is.EqualTo(1f));
            Assert.That(glow.Multiplier(pike, false), Is.EqualTo(2f));
        }
    }
}
