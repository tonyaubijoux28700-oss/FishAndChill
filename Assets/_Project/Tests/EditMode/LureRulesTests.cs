using FishAndChill.Collection;
using FishAndChill.Gear;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class LureRulesTests
    {
        private static readonly LureType[] Lures = { LureType.Minnow, LureType.Spinner };
        private static readonly HookType[] Hooks = { HookType.Treble };

        [Test]
        public void CommonsBiteOnAnything()
        {
            Assert.IsTrue(LureRules.Allows(Rarity.Common, LureType.Fly, HookType.Circle, Lures, Hooks));
        }

        [Test]
        public void RaresNeedTheirLureButNotTheHook()
        {
            Assert.IsFalse(LureRules.Allows(Rarity.Rare, LureType.Natural, HookType.Treble, Lures, Hooks));
            Assert.IsTrue(LureRules.Allows(Rarity.Uncommon, LureType.Spinner, HookType.Aberdeen, Lures, Hooks));
        }

        [Test]
        public void EpicsNeedLureAndHook()
        {
            Assert.IsFalse(LureRules.Allows(Rarity.Epic, LureType.Minnow, HookType.Aberdeen, Lures, Hooks));
            Assert.IsFalse(LureRules.Allows(Rarity.Legendary, LureType.Jig, HookType.Treble, Lures, Hooks));
            Assert.IsTrue(LureRules.Allows(Rarity.Legendary, LureType.Minnow, HookType.Treble, Lures, Hooks));
        }

        [Test]
        public void EmptyListsAllowEverything()
        {
            Assert.IsTrue(LureRules.Allows(Rarity.Epic, LureType.Fly, HookType.Fly, new LureType[0], new HookType[0]));
        }

        [Test]
        public void ParsesTableWords()
        {
            Assert.AreEqual(LureType.Spoon, LureRules.ParseLure("Ondulante"));
            Assert.AreEqual(LureType.Troll, LureRules.ParseLure("traîne"));
            Assert.AreEqual(LureType.Natural, LureRules.ParseLure("crevette"));
            Assert.IsNull(LureRules.ParseLure("banane"));
            Assert.AreEqual(HookType.Forged, LureRules.ParseHook("forgé"));
            Assert.AreEqual(HookType.Offset, LureRules.ParseHook("texan"));
        }

        [Test]
        public void ConvertsOldBaits()
        {
            Assert.AreEqual("VerDeTerreau", LureRules.ConvertOldBait("MieDePain"));
            Assert.AreEqual("PaletteAglia", LureRules.ConvertOldBait("LeurreBrillant"));
            Assert.AreEqual("Popper", LureRules.ConvertOldBait("Popper"));
        }
    }
}
