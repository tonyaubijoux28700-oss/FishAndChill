using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.Tests
{
    /// <summary>Fabrique d'espèces et de poissons pour les tests.</summary>
    internal static class TestFish
    {
        public static FishSpecies Species(string id, int price)
        {
            var species = ScriptableObject.CreateInstance<FishSpecies>();
            JsonUtility.FromJsonOverwrite($"{{\"_id\":\"{id}\",\"_commonName\":\"{id}\",\"_basePrice\":{price}}}", species);
            return species;
        }

        public static CaughtFish Fish(FishSpecies species, float size = 20f) => new CaughtFish(species, size);
    }
}
