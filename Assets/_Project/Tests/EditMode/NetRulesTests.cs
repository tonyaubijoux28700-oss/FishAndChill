using FishAndChill.Collection;
using FishAndChill.Fishing;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class NetRulesTests
    {
        [Test]
        public void EachKind_ReactsInItsOwnWay()
        {
            Assert.That(NetRules.Behaviour(CreatureKind.Crustacean), Is.EqualTo(CritterBehaviour.Hide));
            Assert.That(NetRules.Behaviour(CreatureKind.Cephalopod), Is.EqualTo(CritterBehaviour.Ink));
            Assert.That(NetRules.Behaviour(CreatureKind.Echinoderm), Is.EqualTo(CritterBehaviour.Burrow));
            Assert.That(NetRules.Behaviour(CreatureKind.Fish), Is.EqualTo(CritterBehaviour.Dart), "la lamproie file");
        }

        [Test]
        public void RarerCritters_AreBigger_AndWarier()
        {
            Assert.That(NetRules.Scale(Rarity.Legendary), Is.GreaterThan(NetRules.Scale(Rarity.Common)));
            Assert.That(NetRules.Sensitivity(Rarity.Epic), Is.GreaterThan(NetRules.Sensitivity(Rarity.Common)));
        }

        [Test]
        public void AFastNetNearby_Worries_ACalmOneDoesNot()
        {
            float fast = NetRules.AlarmDelta(200f, 20f, 1f, CritterBehaviour.Hide, 0.1f);
            float slow = NetRules.AlarmDelta(30f, 20f, 1f, CritterBehaviour.Hide, 0.1f);
            float far = NetRules.AlarmDelta(200f, 200f, 1f, CritterBehaviour.Hide, 0.1f);
            Assert.That(fast, Is.GreaterThan(0f));
            Assert.That(slow, Is.LessThan(0f), "une épuisette lente rassure");
            Assert.That(far, Is.LessThan(0f), "trop loin pour s'inquiéter");
            Assert.That(NetRules.AlarmDelta(200f, 20f, 1f, CritterBehaviour.Burrow, 0.1f), Is.LessThan(fast));
        }

        [Test]
        public void TheSplash_ScaresTheWorried_AndTheCalmAreCaught()
        {
            Assert.That(NetRules.SplashAlarm(0.6f, 10f), Is.EqualTo(1f));
            Assert.That(NetRules.SplashAlarm(0.1f, 10f), Is.EqualTo(0.35f).Within(1e-5f));
            Assert.That(NetRules.SplashAlarm(0.6f, 100f), Is.EqualTo(0.6f), "trop loin de l'éclaboussure");
            Assert.That(NetRules.Caught(5f, 0.35f, 1f), Is.True);
            Assert.That(NetRules.Caught(5f, 1f, 1f), Is.False, "elle file avant que le cercle se referme");
            Assert.That(NetRules.Caught(40f, 0f, 1f), Is.False, "pas sous le cercle");
        }

        [Test]
        public void TheNet_LagsBehindThePointer()
        {
            Vector2 net = NetRules.FollowPointer(Vector2.zero, new Vector2(100f, 0f), 1f / 60f);
            Assert.That(net.x, Is.InRange(5f, 30f));
        }
    }
}
