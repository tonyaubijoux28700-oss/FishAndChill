using FishAndChill.UI;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class ShowcaseMathTests
    {
        [Test]
        public void Step_GoesAround_BothWays()
        {
            Assert.That(ShowcaseMath.Step(0, 1, 5), Is.EqualTo(1));
            Assert.That(ShowcaseMath.Step(4, 1, 5), Is.EqualTo(0));
            Assert.That(ShowcaseMath.Step(0, -1, 5), Is.EqualTo(4));
            Assert.That(ShowcaseMath.Step(2, 0, 5), Is.EqualTo(2));
            Assert.That(ShowcaseMath.Step(3, 1, 0), Is.EqualTo(0), "vitrine vide");
        }

        [Test]
        public void FitScale_IsTheLargestWholeZoomThatFits()
        {
            Assert.That(ShowcaseMath.FitScale(32f, 32f, 170f, 170f), Is.EqualTo(5));
            Assert.That(ShowcaseMath.FitScale(56f, 44f, 170f, 170f), Is.EqualTo(3));
            Assert.That(ShowcaseMath.FitScale(400f, 400f, 170f, 170f), Is.EqualTo(1), "jamais moins de 1");
        }
    }
}
