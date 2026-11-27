using FishAndChill.Collection;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class NetPurchaseTests
    {
        [Test]
        public void TheNet_IsBoughtOnce_WithEnoughCoins()
        {
            var go = new GameObject("Progression");
            try
            {
                var progress = go.AddComponent<PlayerProgress>();
                progress.EnsureCreated();
                Assert.That(progress.HasNet, Is.False);
                Assert.That(progress.BuyNet(), Is.False, "pas assez de pièces");

                progress.Wallet.Add(PlayerProgress.NetPrice + 30);
                Assert.That(progress.BuyNet(), Is.True);
                Assert.That(progress.HasNet, Is.True);
                Assert.That(progress.Wallet.Coins, Is.EqualTo(30));
                Assert.That(progress.BuyNet(), Is.False, "une seule épuisette");
                Assert.That(progress.Wallet.Coins, Is.EqualTo(30));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
