using System;
using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Registre des captures de la partie en cours : sait si une espèce est nouvelle
    /// (elle ira dans l'aquarium) ou si c'est un doublon (à revendre), et garde le record de taille.
    /// Sert aussi à l'index des poissons (espèces découvertes).
    /// </summary>
    public sealed class CatchLog
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _bestSizes = new Dictionary<string, float>();

        /// <summary>Une capture vient d'être enregistrée.</summary>
        public event Action Changed;

        public int SpeciesCount => _counts.Count;
        public int TotalCatches { get; private set; }

        /// <summary>Enregistre une capture et renvoie <c>true</c> si l'espèce n'avait jamais été attrapée.</summary>
        public bool Register(string speciesId, float sizeCm = 0f)
        {
            TotalCatches++;
            _counts.TryGetValue(speciesId, out int count);
            _counts[speciesId] = count + 1;
            if (!_bestSizes.TryGetValue(speciesId, out float best) || sizeCm > best)
            {
                _bestSizes[speciesId] = sizeCm;
            }

            Changed?.Invoke();
            return count == 0;
        }

        public int CountOf(string speciesId)
        {
            return _counts.TryGetValue(speciesId, out int count) ? count : 0;
        }

        /// <summary>La plus grande prise de cette espèce, en cm (0 si jamais attrapée).</summary>
        public float BestSizeOf(string speciesId)
        {
            return _bestSizes.TryGetValue(speciesId, out float size) ? size : 0f;
        }

        public bool IsDiscovered(string speciesId) => CountOf(speciesId) > 0;

        /// <summary>Espèces déjà pêchées (pour la sauvegarde).</summary>
        public IEnumerable<string> SpeciesIds => _counts.Keys;

        /// <summary>Remet une espèce telle qu'elle était dans une sauvegarde (nombre de prises et record).</summary>
        public void Restore(string speciesId, int count, float bestSizeCm)
        {
            if (string.IsNullOrEmpty(speciesId) || count <= 0)
            {
                return;
            }

            _counts[speciesId] = count;
            _bestSizes[speciesId] = bestSizeCm;
            Changed?.Invoke();
        }

        /// <summary>Remet le total des prises d'une sauvegarde.</summary>
        public void RestoreTotal(int totalCatches)
        {
            TotalCatches = Math.Max(0, totalCatches);
            Changed?.Invoke();
        }
    }
}
