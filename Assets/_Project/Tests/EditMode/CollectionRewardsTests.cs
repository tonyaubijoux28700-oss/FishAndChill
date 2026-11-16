using System.Collections.Generic;
using FishAndChill.Collection;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class CollectionRewardsTests
    {
        [Test]
        public void Reached_UsesWholePercentages()
        {
            Assert.That(CollectionRewards.Reached(4, 16, 25), Is.True);
            Assert.That(CollectionRewards.Reached(3, 16, 25), Is.False);
            Assert.That(CollectionRewards.Reached(7, 13, 50), Is.True);
            Assert.That(CollectionRewards.Reached(6, 13, 50), Is.False);
            Assert.That(CollectionRewards.Reached(13, 13, 100), Is.True);
            Assert.That(CollectionRewards.Reached(0, 0, 25), Is.False);
        }

        [Test]
        public void Pending_SkipsClaimedMilestones()
        {
            var claimed = new HashSet<string> { CollectionRewards.Key("Mahana", 25) };
            List<int> pending = CollectionRewards.Pending("Mahana", 8, 16, claimed);
            Assert.That(pending, Is.EqualTo(new[] { 50 }));
            Assert.That(CollectionRewards.Pending("Mahana", 16, 16, null), Is.EqualTo(new[] { 25, 50, 100 }));
        }

        [Test]
        public void CoinsFor_GrowsWithTheMilestone()
        {
            Assert.That(CollectionRewards.CoinsFor(25), Is.LessThan(CollectionRewards.CoinsFor(50)));
            Assert.That(CollectionRewards.CoinsFor(50), Is.LessThan(CollectionRewards.CoinsFor(100)));
        }
    }
}
