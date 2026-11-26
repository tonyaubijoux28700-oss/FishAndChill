using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class DecorationTests
    {
        [Test]
        public void Collection_PlacesOnlyWhatIsInReserve_AndGivesItBackWhenRemoved()
        {
            var deco = new DecorationCollection();
            Assert.That(deco.Place("hall", 0, "conque"), Is.False, "rien en réserve");
            deco.Add("conque");
            Assert.That(deco.Place("hall", 0, "conque"), Is.True);
            Assert.That(deco.Available("conque"), Is.EqualTo(0));
            Assert.That(deco.Place("hall", 1, "conque"), Is.False, "une seule conque, déjà posée");
            Assert.That(deco.Place("hall", 0, null), Is.True);
            Assert.That(deco.Available("conque"), Is.EqualTo(1), "reprise, elle revient en réserve");
            Assert.That(deco.Owned("conque"), Is.EqualTo(1));
        }

        [Test]
        public void Collection_RestoresFromSave_IgnoringPlacementsItCannotCover()
        {
            var deco = new DecorationCollection();
            deco.Load(new[] { new KeyValuePair<string, int>("ancre", 1) },
                new[] { new KeyValuePair<string, string>("hall:0", "ancre"), new KeyValuePair<string, string>("hall:1", "ancre") }, true);
            Assert.That(deco.HasMask, Is.True);
            Assert.That(deco.PlacedAt("hall", 0), Is.EqualTo("ancre"));
            Assert.That(deco.PlacedAt("hall", 1), Is.Null, "il n'y a qu'une ancre");
        }

        [Test]
        public void DivePlan_KeepsSpotsOnTheSand_AwayFromTheRope()
        {
            DivePlan plan = DivePlan.Build(4242);
            Assert.That(plan.Spots.Count, Is.GreaterThanOrEqualTo(DiveMath.ItemsPerDive));
            Assert.That(plan.BlockedAt(plan.Rope + Vector2.down * 1.2f), Is.False, "on arrive au pied de la corde");
            foreach (Vector2 s in plan.Spots)
            {
                Assert.That(plan.BlockedAt(s), Is.False, "pas dans un rocher");
                Assert.That(Vector2.Distance(s, plan.Rope), Is.GreaterThanOrEqualTo(3f));
            }
        }

        [Test]
        public void DivePick_PutsRareItemsOnlyFarFromTheRope()
        {
            DivePlan plan = DivePlan.Build(4242);
            var items = new[] { Rarity.Common, Rarity.Rare };
            for (int seed = 0; seed < 30; seed++)
            {
                List<(Vector2 Spot, int Item)> picks = DiveMath.Pick(plan, items, new System.Random(seed));
                Assert.That(picks.Count, Is.EqualTo(DiveMath.ItemsPerDive));
                foreach ((Vector2 spot, int item) in picks)
                {
                    if (item == 1) Assert.That(plan.Remoteness(spot), Is.GreaterThanOrEqualTo(DiveMath.RareRemoteness));
                }
            }

            Assert.That(DiveMath.KeptWhenOutOfAir(5), Is.EqualTo(2), "à court d'air, on lâche la moitié");
        }
    }
}
