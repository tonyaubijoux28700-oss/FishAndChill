using FishAndChill.UI;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class TitleScreenLogicTests
    {
        [Test]
        public void LeftRight_WrapAroundTheSlots()
        {
            Assert.That(TitleScreenLogic.Navigate(2, 2, 1, 0), Is.EqualTo(0));
            Assert.That(TitleScreenLogic.Navigate(0, 0, -1, 0), Is.EqualTo(2));
        }

        [Test]
        public void Down_GoesToQuit_AndUpComesBackToTheLastSlot()
        {
            Assert.That(TitleScreenLogic.Navigate(1, 1, 0, -1), Is.EqualTo(TitleScreenLogic.Quit));
            Assert.That(TitleScreenLogic.Navigate(TitleScreenLogic.Quit, 2, 0, 1), Is.EqualTo(2));
            Assert.That(TitleScreenLogic.Navigate(TitleScreenLogic.Quit, 2, 1, 0), Is.EqualTo(TitleScreenLogic.Quit));
        }
    }
}
