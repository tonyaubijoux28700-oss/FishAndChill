using FishAndChill.UI;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class PauseMenuLogicTests
    {
        [Test]
        public void Move_WrapsAroundBothWays()
        {
            Assert.That(PauseMenuLogic.Move(4, 1, 5), Is.EqualTo(0));
            Assert.That(PauseMenuLogic.Move(0, -1, 5), Is.EqualTo(4));
            Assert.That(PauseMenuLogic.Move(2, 1, 5), Is.EqualTo(3));
        }

        [Test]
        public void StepVolume_MovesByTenPercent_AndStaysBetweenZeroAndOne()
        {
            Assert.That(PauseMenuLogic.StepVolume(0.8f, 1), Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(PauseMenuLogic.StepVolume(1f, 1), Is.EqualTo(1f));
            Assert.That(PauseMenuLogic.StepVolume(0f, -1), Is.EqualTo(0f));
            Assert.That(PauseMenuLogic.StepVolume(0.43f, -1), Is.EqualTo(0.3f).Within(0.0001f));
        }

        [Test]
        public void VolumeText_IsAPercentage()
        {
            Assert.That(PauseMenuLogic.VolumeText(0.8f), Is.EqualTo("80 %"));
        }
    }
}
