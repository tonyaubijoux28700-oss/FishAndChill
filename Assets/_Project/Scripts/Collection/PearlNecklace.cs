using System;
using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>Où en est la perle d'une zone : pas encore trouvée, trouvée (dans le sac), ou enfilée sur le collier de Chiyo.</summary>
    public enum PearlState { Missing, Found, Threaded }

    /// <summary>
    /// Le collier de Chiyo (logique pure, testée) : une perle par zone, trouvée au fond de l'épuisette quand toutes les
    /// bestioles de la zone sont dans le carnet, puis rapportée à Chiyo qui l'enfile.
    /// </summary>
    public sealed class PearlNecklace
    {
        private readonly HashSet<string> _found = new HashSet<string>();
        private readonly HashSet<string> _threaded = new HashSet<string>();

        public event Action Changed;

        public IEnumerable<string> Found => _found;
        public IEnumerable<string> Threaded => _threaded;
        public int ThreadedCount => _threaded.Count;

        public PearlState StateOf(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId)) return PearlState.Missing;
            if (_threaded.Contains(zoneId)) return PearlState.Threaded;
            return _found.Contains(zoneId) ? PearlState.Found : PearlState.Missing;
        }

        /// <summary>Toutes les bestioles de la zone sont-elles découvertes (zone sans bestiole : jamais) ?</summary>
        public static bool ZoneComplete(ICollection<string> netSpeciesIds, Func<string, bool> discovered)
        {
            if (netSpeciesIds == null || netSpeciesIds.Count == 0) return false;
            foreach (string id in netSpeciesIds)
            {
                if (!discovered(id)) return false;
            }

            return true;
        }

        /// <summary>La perle de la zone brille au fond de l'épuisette : vrai seulement la première fois.</summary>
        public bool Find(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || StateOf(zoneId) != PearlState.Missing) return false;
            _found.Add(zoneId);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Chiyo enfile les perles rapportées ; renvoie les zones dont la perle vient d'être enfilée.</summary>
        public List<string> ThreadAll()
        {
            var threaded = new List<string>(_found);
            threaded.Sort(string.CompareOrdinal);
            foreach (string zone in threaded) _threaded.Add(zone);
            _found.Clear();
            if (threaded.Count > 0) Changed?.Invoke();
            return threaded;
        }

        /// <summary>Chargement d'une partie.</summary>
        public void Restore(IEnumerable<string> found, IEnumerable<string> threaded)
        {
            _found.Clear();
            _threaded.Clear();
            if (threaded != null) foreach (string zone in threaded) _threaded.Add(zone);
            if (found != null) foreach (string zone in found) if (!_threaded.Contains(zone)) _found.Add(zone);
            Changed?.Invoke();
        }
    }
}
