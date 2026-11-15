using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Gear;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class GearTests
    {
        private GearLine _rod;
        private GearLine _hook;
        private GearLine _bait;
        private GearLine _bag;
        private GearCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _rod = Line(GearCategory.Rod, new GearLevel("Bambou", 0, 0f), new GearLevel("Fibre", 60, 0.15f), new GearLevel("Carbone", 180, 0.30f));
            _hook = Line(GearCategory.Hook, new GearLevel("Simple", 0, 0f), new GearLevel("Renforcé", 45, 1f));
            _bait = Line(GearCategory.Bait, new GearLevel("Pain", 0, 0f), new GearLevel("Crevettes", 50, 0.4f, 0.15f));
            _bag = Line(GearCategory.Bag, new GearLevel("Sacoche", 0, 3f), new GearLevel("Besace", 70, 4f));
            _catalog = ScriptableObject.CreateInstance<GearCatalog>();
            _catalog.hideFlags = HideFlags.HideAndDontSave;
            _catalog.Configure(new[] { _rod, _hook, _bait, _bag });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_rod);
            Object.DestroyImmediate(_hook);
            Object.DestroyImmediate(_bait);
            Object.DestroyImmediate(_bag);
        }

        private static GearLine Line(GearCategory category, params GearLevel[] levels)
        {
            var line = ScriptableObject.CreateInstance<GearLine>();
            line.hideFlags = HideFlags.HideAndDontSave;
            line.Configure(category, category.ToString(), levels);
            return line;
        }

        [Test]
        public void Buy_SpendsCoinsAndRaisesTheLevel()
        {
            var loadout = new GearLoadout();
            var wallet = new Wallet();
            wallet.Add(100);

            Assert.That(GearShop.TryBuy(_rod, loadout, wallet), Is.EqualTo(GearPurchase.Bought));
            Assert.That(loadout.Level(GearCategory.Rod), Is.EqualTo(1));
            Assert.That(wallet.Coins, Is.EqualTo(40));
            Assert.That(GearShop.NextLevel(_rod, loadout).Name, Is.EqualTo("Carbone"));
        }

        [Test]
        public void Buy_WithoutEnoughCoins_ChangesNothing()
        {
            var loadout = new GearLoadout();
            var wallet = new Wallet();
            wallet.Add(59);

            Assert.That(GearShop.TryBuy(_rod, loadout, wallet), Is.EqualTo(GearPurchase.NotEnoughCoins));
            Assert.That(loadout.Level(GearCategory.Rod), Is.EqualTo(0));
            Assert.That(wallet.Coins, Is.EqualTo(59));
        }

        [Test]
        public void Buy_AtMaxLevel_IsRefused()
        {
            var loadout = new GearLoadout();
            var wallet = new Wallet();
            wallet.Add(1000);
            GearShop.TryBuy(_hook, loadout, wallet);

            Assert.That(GearShop.NextLevel(_hook, loadout), Is.Null);
            Assert.That(GearShop.TryBuy(_hook, loadout, wallet), Is.EqualTo(GearPurchase.AlreadyMax));
            Assert.That(wallet.Coins, Is.EqualTo(955));
        }

        [Test]
        public void Effects_StartWithTheBaseGear()
        {
            GearEffects effects = GearEffects.From(_catalog, new GearLoadout());

            Assert.That(effects.ZoneBonus, Is.EqualTo(0f));
            Assert.That(effects.ExtraMisses, Is.EqualTo(0));
            Assert.That(effects.BagCapacity, Is.EqualTo(3));
            Assert.That(effects.BiteWaitFactor, Is.EqualTo(1f));
            Assert.That(effects.RarityMultiplier(Rarity.Legendary), Is.EqualTo(1f));
        }

        [Test]
        public void Effects_MakeTheQteEasier()
        {
            var loadout = new GearLoadout();
            loadout.Upgrade(GearCategory.Rod);
            loadout.Upgrade(GearCategory.Rod);
            loadout.Upgrade(GearCategory.Hook);
            GearEffects effects = GearEffects.From(_catalog, loadout);

            QteDifficulty easier = effects.Apply(new QteDifficulty(2f, 0.2f, 4, 1));

            Assert.That(easier.ZoneWidth, Is.EqualTo(0.26f).Within(0.0001f), "zone 30 % plus large");
            Assert.That(easier.CursorSpeed, Is.LessThan(2f));
            Assert.That(easier.MaxMisses, Is.EqualTo(2));
            Assert.That(easier.RequiredHits, Is.EqualTo(4), "le nombre de réussites ne change pas");
        }

        [Test]
        public void Effects_ZoneNeverExceedsSixtyPercent()
        {
            var effects = new GearEffects(2f, 0, 0f, 0f, 0);

            Assert.That(effects.Apply(new QteDifficulty(1f, 0.5f, 3, 2)).ZoneWidth, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void Bait_FavoursRareSpeciesAndFasterBites()
        {
            var loadout = new GearLoadout();
            loadout.Upgrade(GearCategory.Bait);
            GearEffects effects = GearEffects.From(_catalog, loadout);

            Assert.That(effects.RarityMultiplier(Rarity.Common), Is.EqualTo(1f));
            Assert.That(effects.RarityMultiplier(Rarity.Rare), Is.GreaterThan(effects.RarityMultiplier(Rarity.Uncommon)));
            Assert.That(effects.BiteWaitFactor, Is.EqualTo(0.85f).Within(0.0001f));
        }
    }
}
