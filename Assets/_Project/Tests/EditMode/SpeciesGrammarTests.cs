using FishAndChill.Collection;
using NUnit.Framework;

namespace FishAndChill.Tests
{
    public class SpeciesGrammarTests
    {
        [Test]
        public void Definite_ChoosesLeLaOrElision()
        {
            Assert.That(SpeciesGrammar.Definite("Brochet", false), Is.EqualTo("le brochet"));
            Assert.That(SpeciesGrammar.Definite("Perche commune", true), Is.EqualTo("la perche commune"));
            Assert.That(SpeciesGrammar.Definite("Anguille européenne", true), Is.EqualTo("l'anguille européenne"));
            Assert.That(SpeciesGrammar.Definite("Écrevisse signal", true), Is.EqualTo("l'écrevisse signal"));
            Assert.That(SpeciesGrammar.Definite("Hippocampe", false), Is.EqualTo("l'hippocampe"));
            Assert.That(SpeciesGrammar.Definite("Hareng", false), Is.EqualTo("le hareng"));
            Assert.That(SpeciesGrammar.Definite("Idole des Maures", true), Is.EqualTo("l'idole des Maures"));
        }

        [Test]
        public void Your_UsesTonBeforeAVowel()
        {
            Assert.That(SpeciesGrammar.Your("Truite fario", true), Is.EqualTo("ta truite fario"));
            Assert.That(SpeciesGrammar.Your("Gardon", false), Is.EqualTo("ton gardon"));
            Assert.That(SpeciesGrammar.Your("Anguille européenne", true), Is.EqualTo("ton anguille européenne"));
        }

        [Test]
        public void Agree_AndPronoun_FollowTheGender()
        {
            Assert.That(SpeciesGrammar.Agree("vendu", true), Is.EqualTo("vendue"));
            Assert.That(SpeciesGrammar.Agree("vendu", false), Is.EqualTo("vendu"));
            Assert.That(SpeciesGrammar.Pronoun(true), Is.EqualTo("la"));
            Assert.That(SpeciesGrammar.Pronoun(false), Is.EqualTo("le"));
        }
    }
}
