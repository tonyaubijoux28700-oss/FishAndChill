using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>Bilan d'un dépôt à l'aquarium.</summary>
    public sealed class DepositResult
    {
        /// <summary>Espèces entrées dans l'aquarium.</summary>
        public List<CaughtFish> NewSpecies { get; } = new List<CaughtFish>();

        /// <summary>Poissons restés dans le sac (doublons, pour le marchand).</summary>
        public int LeftInBag { get; set; }

        public bool IsEmpty => NewSpecies.Count == 0;
    }
}
