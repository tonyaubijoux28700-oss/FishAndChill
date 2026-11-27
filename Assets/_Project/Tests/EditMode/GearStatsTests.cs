using FishAndChill.Gear;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class GearStatsTests
    {
        [Test]
        public void EachRod_ShinesWhereItsRoleIs()
        {
            int[] bamboo = GearStats.Rod(new GearLevel("Bambou", 0, 0f, 0.2f));
            int[] heavy = GearStats.Rod(new GearLevel("Lourde", 220, 0.05f).WithStats("", tension: 0.4f));
            int[] longRod = GearStats.Rod(new GearLevel("Longue", 160, 0.1f).WithStats("", cast: 3f));
            int[] contest = GearStats.Rod(new GearLevel("Compétition", 480, 0.35f).WithStats("", tension: 1.35f));

            Assert.That(bamboo[3], Is.EqualTo(GearStats.Max), "le bambou : touches rapides");
            Assert.That(heavy[1], Is.EqualTo(GearStats.Max), "la lourde : solide");
            Assert.That(longRod[2], Is.EqualTo(GearStats.Max), "la longue : portée");
            Assert.That(contest[0], Is.EqualTo(GearStats.Max), "la compétition : zone");
            Assert.That(contest[1], Is.LessThan(bamboo[1]), "mais fragile");
            Assert.That(GearStats.Rod(null), Has.All.EqualTo(1));
        }
    }
}
