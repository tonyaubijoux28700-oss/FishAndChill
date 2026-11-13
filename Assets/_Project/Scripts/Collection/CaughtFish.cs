namespace FishAndChill.Collection
{
    /// <summary>Un poisson attrapé : son espèce et sa taille.</summary>
    public sealed class CaughtFish
    {
        public CaughtFish(FishSpecies species, float sizeCm)
        {
            Species = species;
            SizeCm = sizeCm;
        }

        public FishSpecies Species { get; }
        public float SizeCm { get; }

        /// <summary>Identifiant de l'espèce (pratique pour la logique et les tests).</summary>
        public string SpeciesId => Species != null ? Species.Id : string.Empty;

        /// <summary>Prix de revente d'un doublon.</summary>
        public int SalePrice => Species != null ? Species.BasePrice : 0;
    }
}
