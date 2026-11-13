using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.Tests
{
    /// <summary>Fabrique d'espèces et de poissons pour les tests.</summary>
    internal static class TestFish
    {
        public static FishSpecies Species(string id, int price, float minCm = 10f, float maxCm = 30f)
        {
            var species = ScriptableObject.CreateInstance<FishSpecies>();
            string json = $"{{\"_id\":\"{id}\",\"_commonName\":\"{id}\",\"_basePrice\":{price}," +
                          $"\"_sizeRangeCm\":{{\"x\":{minCm.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{maxCm.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}}}";
            JsonUtility.FromJsonOverwrite(json, species);
            return species;
        }

        public static CaughtFish Fish(FishSpecies species, float size = 20f) => new CaughtFish(species, size);
    }
}
