using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class FishingSpotRulesTests
    {
        private static FishSpecies Species(Rarity rarity, CreatureKind kind = CreatureKind.Fish)
        {
            FishSpecies species = TestFish.Species("spot-" + rarity + kind, 10);
            JsonUtility.FromJsonOverwrite($"{{\"_rarity\":{(int)rarity},\"_kind\":{(int)kind}}}", species);
            return species;
        }

        [Test]
        public void Frenzy_FavoursRareFish()
        {
            Assert.That(FishingSpotRules.SpeciesMultiplier(SpotKind.Frenzy, Species(Rarity.Legendary)), Is.GreaterThan(1f));
            Assert.That(FishingSpotRules.SpeciesMultiplier(SpotKind.Frenzy, Species(Rarity.Common)), Is.LessThan(1f));
        }

        [Test]
        public void Bubbles_FavourCreaturesOfTheBottom()
        {
            Assert.That(FishingSpotRules.SpeciesMultiplier(SpotKind.Bubbles, Species(Rarity.Rare, CreatureKind.Crustacean)), Is.GreaterThan(1f));
            Assert.That(FishingSpotRules.SpeciesMultiplier(SpotKind.Bubbles, Species(Rarity.Rare)), Is.EqualTo(1f));
        }

        [Test]
        public void School_BitesFaster()
        {
            Assert.That(FishingSpotRules.WaitFactor(SpotKind.School), Is.LessThan(FishingSpotRules.WaitFactor(SpotKind.Bubbles)));
        }

        [Test]
        public void Assist_PicksTheSpotStraightAhead_InRange()
        {
            var spots = new List<Vector2> { new Vector2(0f, 4f), new Vector2(3f, 3f), new Vector2(0f, 20f), new Vector2(-4f, 0f) };
            Assert.That(FishingSpotRules.Assist(Vector2.zero, Vector2.up, spots, 1.5f, 6f), Is.EqualTo(0));
            Assert.That(FishingSpotRules.Assist(Vector2.zero, Vector2.down, spots, 1.5f, 6f), Is.EqualTo(-1));
            Assert.That(FishingSpotRules.Assist(Vector2.zero, Vector2.left, spots, 1.5f, 6f), Is.EqualTo(3));
        }
    }
}
