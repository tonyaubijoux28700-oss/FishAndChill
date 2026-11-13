using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>Bilan d'un dépôt à l'aquarium.</summary>
    public sealed class DepositResult
    {
        public List<CaughtFish> NewSpecies { get; } = new List<CaughtFish>();
        public List<CaughtFish> Sold { get; } = new List<CaughtFish>();
        public int CoinsEarned { get; set; }

        public bool IsEmpty => NewSpecies.Count == 0 && Sold.Count == 0;
    }
}
