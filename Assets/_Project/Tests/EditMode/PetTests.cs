using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Pets;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class PetTests
    {
        private static Wallet WalletWith(int coins)
        {
            var wallet = new Wallet();
            wallet.Add(coins);
            return wallet;
        }

        [Test]
        public void TryBuy_SpendsCoins_AndThePetComesAlong()
        {
            var pets = new PetCollection();
            Wallet wallet = WalletWith(500);
            Assert.That(pets.TryBuy("chat", 300, wallet), Is.EqualTo(PetPurchase.Bought));
            Assert.That(wallet.Coins, Is.EqualTo(200));
            Assert.That(pets.Equipped, Is.EqualTo("chat"));
            Assert.That(pets.TryBuy("chat", 300, wallet), Is.EqualTo(PetPurchase.AlreadyOwned));
            Assert.That(pets.TryBuy("perroquet", 450, wallet), Is.EqualTo(PetPurchase.NotEnoughCoins));
            Assert.That(wallet.Coins, Is.EqualTo(200), "pas assez : rien n'est dépensé");
        }

        [Test]
        public void Equip_OnlyOwnedPets_OrNone()
        {
            var pets = new PetCollection();
            pets.TryBuy("chat", 10, WalletWith(10));
            Assert.That(pets.Equip("bouledogue"), Is.False);
            Assert.That(pets.Equipped, Is.EqualTo("chat"));
            Assert.That(pets.Equip(null), Is.True);
            Assert.That(pets.Equipped, Is.Null);
            Assert.That(pets.Equip("chat"), Is.True);
        }

        [Test]
        public void Load_IgnoresAnUnknownEquippedPet()
        {
            var pets = new PetCollection();
            pets.Load(new[] { "chat", "chat", "perroquet" }, "bouledogue");
            Assert.That(pets.Owned, Is.EqualTo(new[] { "chat", "perroquet" }));
            Assert.That(pets.Equipped, Is.Null);
            pets.Load(new[] { "chat" }, "chat");
            Assert.That(pets.Equipped, Is.EqualTo("chat"));
        }

        [Test]
        public void RarityFactor_OnlyBoostsRareAndAbove()
        {
            Assert.That(PetMath.RarityFactor(Rarity.Common, 1.35f), Is.EqualTo(1f));
            Assert.That(PetMath.RarityFactor(Rarity.Uncommon, 1.35f), Is.EqualTo(1f));
            Assert.That(PetMath.RarityFactor(Rarity.Rare, 1.35f), Is.EqualTo(1.35f));
            Assert.That(PetMath.RarityFactor(Rarity.Epic, 1.35f), Is.EqualTo(1.35f));
        }

        [Test]
        public void FoundCoins_NothingWhenStanding_SometimesWhenWalking()
        {
            var random = new System.Random(5);
            int finds = 0;
            for (int i = 0; i < 6000; i++)   // 100 secondes de marche à 60 images par seconde
            {
                Assert.That(PetMath.FoundCoins(random, 1f / 60f, false, 0.5f, 5, 15), Is.EqualTo(0));
                int coins = PetMath.FoundCoins(random, 1f / 60f, true, 1f / 75f, 5, 15);
                if (coins > 0)
                {
                    finds++;
                    Assert.That(coins, Is.InRange(5, 15));
                }
            }

            Assert.That(finds, Is.InRange(0, 5), "environ une trouvaille toutes les 75 s de marche");
        }

        [Test]
        public void FollowPoint_StaysBehindThePlayerAlongHisPath()
        {
            var trail = new List<Vector2> { new Vector2(3f, 0f), new Vector2(2f, 0f), new Vector2(2f, -2f) };
            Assert.That(PetMath.FollowPoint(trail, 0.5f), Is.EqualTo(new Vector2(2.5f, 0f)));
            Assert.That(PetMath.FollowPoint(trail, 2f), Is.EqualTo(new Vector2(2f, -1f)));
            Assert.That(PetMath.FollowPoint(trail, 10f), Is.EqualTo(new Vector2(2f, -2f)));
        }

        [Test]
        public void SizeRoll_CrocodileLeansTowardBigFish()
        {
            Assert.That(PetMath.SizeRoll(0.5f, 1f), Is.EqualTo(0.5f).Within(1e-5f), "sans bonus : tirage inchangé");
            Assert.That(PetMath.SizeRoll(0f, 1.8f), Is.EqualTo(0f));
            Assert.That(PetMath.SizeRoll(1f, 1.8f), Is.EqualTo(1f));
            Assert.That(PetMath.SizeRoll(0.5f, 1.8f), Is.GreaterThan(0.65f));
            Assert.That(PetMath.SizeRoll(0.5f, 0.5f), Is.EqualTo(0.5f).Within(1e-5f), "jamais de malus");
        }

        [Test]
        public void ZoneWidth_SpiderWidensTheZone_UpTo60Percent()
        {
            Assert.That(PetMath.ZoneWidth(0.25f, 1.2f), Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(PetMath.ZoneWidth(0.55f, 1.2f), Is.EqualTo(0.6f).Within(1e-5f));
            Assert.That(PetMath.ZoneWidth(0.7f, 1.2f), Is.EqualTo(0.7f).Within(1e-5f), "déjà au-dessus : inchangé");
            Assert.That(PetMath.ZoneWidth(0.25f, 1f), Is.EqualTo(0.25f).Within(1e-5f));
        }

        [Test]
        public void Reaction_MovesOnlyDuringItsTime_AndEveryPetReacts()
        {
            foreach (PetKind kind in System.Enum.GetValues(typeof(PetKind)))
            {
                float moved = 0f;
                for (float t = 0f; t < PetMath.ReactionSeconds; t += 0.05f)
                {
                    moved = Mathf.Max(moved, PetMath.ReactionOffset(kind, t, out float tilt).magnitude + Mathf.Abs(tilt) * 0.01f);
                }

                Assert.That(moved, Is.GreaterThan(0.05f), $"{kind} doit bouger");
                Assert.That(PetMath.ReactionOffset(kind, PetMath.ReactionSeconds, out float after), Is.EqualTo(Vector2.zero));
                Assert.That(after, Is.EqualTo(0f));
                Assert.That(PetMath.ReactionOffset(kind, -0.1f, out _), Is.EqualTo(Vector2.zero));
            }
        }

        [Test]
        public void Heart_ForRareCatches_NewSpecies_NotForCommonOnes()
        {
            Assert.That(PetMath.HeartFor(Rarity.Common, false), Is.False);
            Assert.That(PetMath.HeartFor(Rarity.Common, true), Is.True, "nouvelle espèce");
            Assert.That(PetMath.HeartFor(Rarity.Rare, false), Is.True);
        }

        [Test]
        public void SitPoint_BesideThePlayer_OnSolidGround()
        {
            Vector2 left = PetMath.SitPoint(Vector2.zero, Vector2.up, null);
            Assert.That(left.x, Is.LessThan(-0.5f), "face au nord : à gauche du joueur");
            Vector2 right = PetMath.SitPoint(Vector2.zero, Vector2.up, p => p.x > 0f);
            Assert.That(right.x, Is.GreaterThan(0.5f), "à gauche c'est l'eau : à droite");
            Vector2 behind = PetMath.SitPoint(Vector2.zero, Vector2.up, p => false);
            Assert.That(behind.y, Is.LessThan(-0.5f), "nulle part à côté : derrière");
        }

        [Test]
        public void CanPet_OnlyWhenCloseAndFacingThePet()
        {
            Assert.That(PetMath.CanPet(Vector2.zero, Vector2.left, new Vector2(-1f, 0f)), Is.True);
            Assert.That(PetMath.CanPet(Vector2.zero, Vector2.right, new Vector2(-1f, 0f)), Is.False, "dos tourné");
            Assert.That(PetMath.CanPet(Vector2.zero, Vector2.left, new Vector2(-3f, 0f)), Is.False, "trop loin");
        }
    }
}
