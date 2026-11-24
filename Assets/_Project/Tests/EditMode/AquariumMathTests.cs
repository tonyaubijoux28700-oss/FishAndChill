using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class AquariumMathTests
    {
        [Test]
        public void Schools_OnlyForSmallSwimmers()
        {
            Assert.That(AquariumMath.SchoolSize(0.7f, false), Is.EqualTo(5), "tout petits : banc de 5");
            Assert.That(AquariumMath.SchoolSize(1.2f, false), Is.EqualTo(3));
            Assert.That(AquariumMath.SchoolSize(2.5f, false), Is.EqualTo(1), "gros : seul");
            Assert.That(AquariumMath.SchoolSize(0.5f, true), Is.EqualTo(1), "bêtes du fond : seules");
        }

        [Test]
        public void FarPlane_IsDarkerAndSlower_FrontStaysVivid()
        {
            var water = new Color(0.1f, 0.3f, 0.44f);
            Color far = AquariumMath.PlaneTint(0, water), front = AquariumMath.PlaneTint(2, water);
            Assert.That(front, Is.EqualTo(Color.white));
            Assert.That(far.r, Is.LessThan(front.r));
            Assert.That(far.b, Is.GreaterThan(far.r), "plus bleu");
            Assert.That(AquariumMath.PlaneSpeed(0), Is.LessThan(AquariumMath.PlaneSpeed(2)));
        }

        [Test]
        public void Depths_SpreadOverTheWholeTank()
        {
            Assert.That(AquariumMath.DepthInPlane(0, 0f), Is.GreaterThan(AquariumMath.DepthInPlane(2, 0f)), "le fond nage plus haut");
            Assert.That(AquariumMath.DepthInPlane(2, 0f), Is.EqualTo(0f));
            Assert.That(AquariumMath.DepthInPlane(0, 1f), Is.EqualTo(1f));
            Assert.That(AquariumMath.DepthInPlane(1, 2f), Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void CloseUp_FitsTheTankHeight()
        {
            var tank = new Rect(0f, 0f, 25f, 4.4f);
            float size = AquariumMath.CloseUpSize(tank, 100f, 0.5f);
            Assert.That(size, Is.EqualTo(2.7f).Within(1e-4f), "demi-hauteur du bassin + marge");
            Assert.That(AquariumMath.CloseUpSize(tank, 1f, 0.5f), Is.GreaterThan(12f), "écran carré : la largeur l'emporte");
        }
    }
}
