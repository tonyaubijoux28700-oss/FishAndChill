using System.Collections.Generic;
using FishAndChill.Collection;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class CaveRulesTests
    {
        private static readonly List<string> Order = new List<string> { "isopode", "mouton", "meduse" };

        [Test]
        public void TheCritters_ShowUpOneAfterTheOther()
        {
            Assert.That(CaveRules.Available(Order, id => false), Is.EqualTo(new[] { "isopode" }));
            Assert.That(CaveRules.Available(Order, id => id == "isopode"), Is.EqualTo(new[] { "isopode", "mouton" }));
            Assert.That(CaveRules.Available(Order, id => true), Is.EqualTo(Order), "tout découvert : elles sont toutes là");
        }

        [Test]
        public void OneCatchADay()
        {
            Assert.That(CaveRules.DayOf(0.0), Is.EqualTo(0));
            Assert.That(CaveRules.DayOf(47.9), Is.EqualTo(1));
            Assert.That(CaveRules.CanFish(-1, 0), Is.True, "jamais pêché");
            Assert.That(CaveRules.CanFish(3, 3), Is.False, "déjà une prise aujourd'hui");
            Assert.That(CaveRules.CanFish(3, 4), Is.True);
        }
    }
}
