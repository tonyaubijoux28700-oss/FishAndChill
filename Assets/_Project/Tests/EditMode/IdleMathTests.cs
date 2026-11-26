using FishAndChill.Player;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public sealed class IdleMathTests
    {
        [Test]
        public void Player_StaysStill_ThenLooksAround_TapsHisFoot_AndStretches()
        {
            Assert.That(IdleMath.PlayerPose(0f), Is.EqualTo(-1));
            Assert.That(IdleMath.PlayerPose(IdleMath.PlayerDelay - 0.1f), Is.EqualTo(-1), "rien avant le délai");
            Assert.That(IdleMath.PlayerPose(IdleMath.PlayerDelay + 0.3f), Is.EqualTo(CharacterPainter.IdleLookLeft));
            Assert.That(IdleMath.PlayerPose(IdleMath.PlayerDelay + 1.2f), Is.EqualTo(CharacterPainter.IdleLookRight));
            Assert.That(IdleMath.PlayerPose(IdleMath.PlayerDelay + 2.3f), Is.EqualTo(CharacterPainter.IdleTap));
            Assert.That(IdleMath.PlayerPose(IdleMath.PlayerDelay + 2.6f), Is.EqualTo(-1), "le pied retombe entre deux tapes");
            Assert.That(IdleMath.PlayerPose(IdleMath.PlayerDelay + 7f), Is.EqualTo(CharacterPainter.IdleStretch));
            Assert.That(IdleMath.PlayerPose(IdleMath.PlayerDelay + IdleMath.PlayerCycle + 0.3f), Is.EqualTo(CharacterPainter.IdleLookLeft), "et ça recommence");
        }

        [Test]
        public void Pet_TakesItsPose_ThenRepeatsItsGesture()
        {
            Assert.That(IdleMath.PetIdle(IdleMath.PetDelay - 0.1f), Is.False);
            Assert.That(IdleMath.PetIdle(IdleMath.PetDelay), Is.True);
            Assert.That(IdleMath.PetFrame(IdleMath.PetDelay + 0.5f), Is.EqualTo(0), "d'abord la pose");
            bool gesture = false;
            for (float t = 1f; t < 4f; t += 0.05f) gesture |= IdleMath.PetFrame(IdleMath.PetDelay + t) == 1;
            Assert.That(gesture, Is.True, "puis le geste");
        }
    }
}
