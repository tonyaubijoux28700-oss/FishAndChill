using FishAndChill.Collection;
using FishAndChill.UI;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class ShineMathTests
    {
        [Test]
        public void OnlyRareAndAbove_Shine()
        {
            Assert.That(ShineMath.Shines(Rarity.Common), Is.False);
            Assert.That(ShineMath.Shines(Rarity.Uncommon), Is.False);
            Assert.That(ShineMath.Shines(Rarity.Rare), Is.True);
            Assert.That(ShineMath.Shines(Rarity.Legendary), Is.True);
        }

        [Test]
        public void Legendary_ShinesMoreOften()
        {
            Assert.That(ShineMath.Period(Rarity.Legendary), Is.LessThan(ShineMath.Period(Rarity.Epic)));
            Assert.That(ShineMath.Period(Rarity.Epic), Is.LessThan(ShineMath.Period(Rarity.Rare)));
        }

        [Test]
        public void Sweep_CrossesThenRests()
        {
            Assert.That(ShineMath.Sweep(0f, 3f), Is.EqualTo(0f));
            Assert.That(ShineMath.Sweep(ShineMath.SweepSeconds * 0.5f, 3f), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(ShineMath.Sweep(2f, 3f), Is.EqualTo(-1f), "pause entre deux éclats");
            Assert.That(ShineMath.Sweep(3.1f, 3f), Is.GreaterThan(0f), "il recommence");
        }
    }
}
