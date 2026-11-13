using FishAndChill.Common;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class WeightedPickerTests
    {
        private static readonly float[] Weights = { 70f, 20f, 10f };

        [TestCase(0.0, 0)]
        [TestCase(0.69, 0)]
        [TestCase(0.70, 1)]
        [TestCase(0.89, 1)]
        [TestCase(0.90, 2)]
        [TestCase(0.9999, 2)]
        public void Pick_RespectsWeights(double roll, int expected)
        {
            Assert.That(WeightedPicker.Pick(Weights, roll), Is.EqualTo(expected));
        }

        [Test]
        public void Pick_SkipsZeroWeights()
        {
            Assert.That(WeightedPicker.Pick(new[] { 0f, 5f, 0f }, 0.99), Is.EqualTo(1));
        }

        [Test]
        public void Pick_ReturnsMinusOneWhenNothingCanBePicked()
        {
            Assert.That(WeightedPicker.Pick(new[] { 0f, -1f }, 0.5), Is.EqualTo(-1));
        }
    }
}
