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

        [Test]
        public void Reachable_FollowsTheShoreAndThePlanks_ButNotTheLoneIslet()
        {
            // Île (colonnes 0 à 3), passerelle sur la ligne 2 jusqu'à la colonne 6, îlot isolé en colonnes 8 et 9.
            bool Stand(int x, int y) => x <= 3 || (y == 2 && x <= 6) || x >= 8;
            bool[,] seen = FishingSpotRules.Reachable(10, 5, new Vector2Int(1, 1), Stand);
            Assert.That(seen[0, 4], Is.True, "toute l'île");
            Assert.That(seen[6, 2], Is.True, "le bout de la passerelle");
            Assert.That(seen[5, 3], Is.False, "l'eau à côté des planches");
            Assert.That(seen[9, 2], Is.False, "l'îlot qu'on ne peut pas rejoindre");
        }

        [Test]
        public void Reachable_FromWater_IsEmpty()
        {
            bool[,] seen = FishingSpotRules.Reachable(4, 4, new Vector2Int(0, 0), (x, y) => x > 0);
            Assert.That(seen[2, 2], Is.False);
        }
    }
}
