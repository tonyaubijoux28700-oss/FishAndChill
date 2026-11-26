using FishAndChill.Collection;
using FishAndChill.Gear;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class GearItemTests
    {
        private GearLine _rod;
        private GearLine _hook;
        private GearCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _rod = Line(GearCategory.Rod,
                new GearLevel("Bambou", 0, 0f, 0.2f),
                new GearLevel("Légère", 80, 0.2f).WithStats("légère", tension: 1.25f, nervous: 0.5f),
                new GearLevel("Longue", 160, 0.1f).WithStats("longue", cast: 2.5f),
                new GearLevel("Lourde", 200, 0.05f).WithStats("lourde", tension: 0.5f, rush: 1.4f));
            _hook = Line(GearCategory.Hook,
                new GearLevel("Simple", 0, 0f),
                new GearLevel("Circulaire", 140, 1f).WithStats("circulaire", tension: 0.8f, rush: 1.3f));
            _catalog = ScriptableObject.CreateInstance<GearCatalog>();
            _catalog.hideFlags = HideFlags.HideAndDontSave;
            _catalog.Configure(new[] { _rod, _hook });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_rod);
            Object.DestroyImmediate(_hook);
        }

        private static GearLine Line(GearCategory category, params GearLevel[] levels)
        {
            var line = ScriptableObject.CreateInstance<GearLine>();
            line.hideFlags = HideFlags.HideAndDontSave;
            line.Configure(category, category.ToString(), levels);
            return line;
        }

        [Test]
        public void BuyItem_OwnsAndEquips_ThenBuyingAgainJustEquips()
        {
            var loadout = new GearLoadout();
            var wallet = new Wallet();
            wallet.Add(500);
            Assert.That(GearShop.TryBuyItem(_rod, 3, loadout, wallet), Is.EqualTo(GearPurchase.Bought));
            Assert.That(wallet.Coins, Is.EqualTo(300));
            Assert.That(loadout.Level(GearCategory.Rod), Is.EqualTo(3));
            Assert.That(loadout.Owns(GearCategory.Rod, 1), Is.False);

            loadout.Equip(GearCategory.Rod, 0);
            Assert.That(GearShop.TryBuyItem(_rod, 3, loadout, wallet), Is.EqualTo(GearPurchase.AlreadyOwned));
            Assert.That(wallet.Coins, Is.EqualTo(300));
            Assert.That(loadout.Level(GearCategory.Rod), Is.EqualTo(3));
        }

        [Test]
        public void BuyItem_WithoutEnoughCoins_ChangesNothing()
        {
            var loadout = new GearLoadout();
            var wallet = new Wallet();
            wallet.Add(50);
            Assert.That(GearShop.TryBuyItem(_rod, 2, loadout, wallet), Is.EqualTo(GearPurchase.NotEnoughCoins));
            Assert.That(loadout.Owns(GearCategory.Rod, 2), Is.False);
            Assert.That(wallet.Coins, Is.EqualTo(50));
        }

        [Test]
        public void Cycle_SkipsItemsNotOwned()
        {
            var loadout = new GearLoadout();
            loadout.Own(GearCategory.Rod, 2);
            loadout.Cycle(GearCategory.Rod, 4);
            Assert.That(loadout.Level(GearCategory.Rod), Is.EqualTo(2));
            loadout.Cycle(GearCategory.Rod, 4);
            Assert.That(loadout.Level(GearCategory.Rod), Is.EqualTo(0));
        }

        [Test]
        public void Save_KeepsOwnedItemsAndEquippedOne()
        {
            var gear = new GearLoadout();
            gear.Own(GearCategory.Rod, 2);
            gear.Own(GearCategory.Rod, 3);
            gear.Equip(GearCategory.Rod, 2);
            string json = FishAndChill.Save.SaveSerializer.ToJson(FishAndChill.Save.SaveSerializer.Capture(
                new Inventory(3), new AquariumCollection(), new Wallet(), gear, new CatchLog()));

            var restored = new GearLoadout();
            FishAndChill.Save.SaveSerializer.Restore(FishAndChill.Save.SaveSerializer.FromJson(json), _ => null,
                new Inventory(3), new AquariumCollection(), new Wallet(), restored, new CatchLog());
            Assert.That(restored.Level(GearCategory.Rod), Is.EqualTo(2));
            Assert.That(restored.Owns(GearCategory.Rod, 3), Is.True);
            Assert.That(restored.Owns(GearCategory.Rod, 1), Is.False);
        }

        [Test]
        public void Effects_CombineRodAndHookSpecialties()
        {
            var loadout = new GearLoadout();
            loadout.Own(GearCategory.Rod, 3);
            loadout.Equip(GearCategory.Rod, 3);
            loadout.Own(GearCategory.Hook, 1);
            loadout.Equip(GearCategory.Hook, 1);
            GearEffects effects = GearEffects.From(_catalog, loadout);
            Assert.That(effects.TensionFactor, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(effects.RushFactor, Is.EqualTo(1.82f).Within(0.0001f));
            Assert.That(effects.ExtraMisses, Is.EqualTo(1));

            loadout.Own(GearCategory.Rod, 2);
            loadout.Equip(GearCategory.Rod, 2);
            Assert.That(GearEffects.From(_catalog, loadout).CastBonus, Is.EqualTo(2.5f));
        }

        [Test]
        public void BambooRod_MakesFishBiteFaster_OthersDoNot()
        {
            var loadout = new GearLoadout();
            Assert.That(GearEffects.From(_catalog, loadout).BiteWaitFactor, Is.EqualTo(0.8f).Within(0.0001f), "bambou : 20 % plus vite");
            loadout.Own(GearCategory.Rod, 3);
            loadout.Equip(GearCategory.Rod, 3);
            Assert.That(GearEffects.From(_catalog, loadout).BiteWaitFactor, Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
