using FishAndChill.UI;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public class FrenchTypographyTests
    {
        private const char Nb = FrenchTypography.NoBreakSpace;

        [Test]
        public void Apply_PutsANoBreakSpaceBeforeHighPunctuation()
        {
            Assert.That(FrenchTypography.Apply("Belle prise !"), Is.EqualTo($"Belle prise{Nb}!"));
            Assert.That(FrenchTypography.Apply("Quel poisson relâcher ?"), Is.EqualTo($"Quel poisson relâcher{Nb}?"));
            Assert.That(FrenchTypography.Apply("Espace : lancer"), Is.EqualTo($"Espace{Nb}: lancer"));
            Assert.That(FrenchTypography.Apply("50 %"), Is.EqualTo($"50{Nb}%"));
        }

        [Test]
        public void Apply_HandlesFrenchQuotes()
        {
            Assert.That(FrenchTypography.Apply("« Bonne pêche ! »"), Is.EqualTo($"«{Nb}Bonne pêche{Nb}!{Nb}»"));
        }

        [Test]
        public void Apply_LeavesOtherTextAlone()
        {
            Assert.That(FrenchTypography.Apply("Truite fario, 42 cm"), Is.EqualTo("Truite fario, 42 cm"));
            Assert.That(FrenchTypography.Apply("<color=#FFD54D>Or</color>"), Is.EqualTo("<color=#FFD54D>Or</color>"));
            Assert.That(FrenchTypography.Apply("Phrase\n: suite"), Is.EqualTo("Phrase\n: suite"));
            Assert.That(FrenchTypography.Apply(string.Empty), Is.EqualTo(string.Empty));
            Assert.That(FrenchTypography.Apply(null), Is.Null);
        }
    }
}
