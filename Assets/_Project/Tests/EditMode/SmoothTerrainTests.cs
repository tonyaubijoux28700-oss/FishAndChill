using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class SmoothTerrainTests
    {
        [Test]
        public void Field_EqualsCornerValues_AtCorners()
        {
            Assert.That(SmoothTerrain.Field(1f, 0f, 0f, 0f, 0f, 0f), Is.EqualTo(1f));
            Assert.That(SmoothTerrain.Field(1f, 0f, 0f, 0f, 1f, 0f), Is.EqualTo(0f));
            Assert.That(SmoothTerrain.Field(0f, 0f, 0f, 1f, 1f, 1f), Is.EqualTo(1f));
        }

        [Test]
        public void SingleLandCorner_GivesRoundedTipNotSquare()
        {
            // Avec un seul coin de terre, le centre est de l'eau (pas de carré plein)…
            Assert.That(SmoothTerrain.Field(1f, 0f, 0f, 0f, 0.5f, 0.5f), Is.LessThan(SmoothTerrain.Threshold));
            // … mais près du coin c'est bien de la terre.
            Assert.That(SmoothTerrain.Field(1f, 0f, 0f, 0f, 0.15f, 0.15f), Is.GreaterThan(SmoothTerrain.Threshold));
        }

        [Test]
        public void StraightShore_IsAtHalfway()
        {
            // Terre en bas, eau en haut : la côte passe au milieu.
            Assert.That(SmoothTerrain.Field(1f, 1f, 0f, 0f, 0.3f, 0.5f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(SmoothTerrain.Field(1f, 1f, 0f, 0f, 0.3f, 0.4f), Is.GreaterThan(0.5f));
        }

        [Test]
        public void Sample_UsesTheFourSurroundingCells()
        {
            // Une seule case de terre en (0, 0).
            float value(int i, int j) => i == 0 && j == 0 ? 1f : 0f;

            Assert.That(SmoothTerrain.Sample(Vector2.zero, value), Is.EqualTo(1f));
            Assert.That(SmoothTerrain.Sample(new Vector2(1f, 0f), value), Is.EqualTo(0f));
            Assert.That(SmoothTerrain.Sample(new Vector2(-0.5f, -0.5f), value), Is.LessThan(0.5f));
        }

        [Test]
        public void Mask_RoundTrips()
        {
            int mask = SmoothTerrain.Mask(true, false, false, true);
            SmoothTerrain.MaskCorners(mask, out float bl, out float br, out float tl, out float tr);

            Assert.That(mask, Is.EqualTo(9));
            Assert.That((bl, br, tl, tr), Is.EqualTo((1f, 0f, 0f, 1f)));
        }
    }
}
