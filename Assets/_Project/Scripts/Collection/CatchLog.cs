using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Registre des captures de la partie en cours : sait si une espèce est nouvelle
    /// (elle ira dans l'aquarium) ou si c'est un doublon (à revendre).
    /// </summary>
    public sealed class CatchLog
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        public int SpeciesCount => _counts.Count;
        public int TotalCatches { get; private set; }

        /// <summary>Enregistre une capture et renvoie <c>true</c> si l'espèce n'avait jamais été attrapée.</summary>
        public bool Register(string speciesId)
        {
            TotalCatches++;
            _counts.TryGetValue(speciesId, out int count);
            _counts[speciesId] = count + 1;
            return count == 0;
        }

        public int CountOf(string speciesId)
        {
            return _counts.TryGetValue(speciesId, out int count) ? count : 0;
        }
    }
}
