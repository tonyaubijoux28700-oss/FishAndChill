using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Texte d'interface qui applique la typographie française (<see cref="FrenchTypography"/>) à tout ce qu'on lui donne :
    /// créé par <see cref="UiFactory.Label"/>, il règle les espaces insécables de tout le jeu en un seul endroit.
    /// </summary>
    public sealed class FrenchText : Text
    {
        public override string text
        {
            get => base.text;
            set => base.text = FrenchTypography.Apply(value);
        }
    }
}
