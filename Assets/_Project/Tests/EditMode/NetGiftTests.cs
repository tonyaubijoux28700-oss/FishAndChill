using FishAndChill.Collection;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class NetGiftTests
    {
        [Test]
        public void TheNet_IsGivenOnce_AndCostsNothing()
        {
            var go = new GameObject("Progression");
            try
            {
                var progress = go.AddComponent<PlayerProgress>();
                progress.EnsureCreated();
                int coins = progress.Wallet.Coins;
                Assert.That(progress.HasNet, Is.False);
                Assert.That(progress.ReceiveNet(), Is.True);
                Assert.That(progress.HasNet, Is.True);
                Assert.That(progress.Wallet.Coins, Is.EqualTo(coins), "un cadeau, pas un achat");
                Assert.That(progress.ReceiveNet(), Is.False, "une seule épuisette");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
