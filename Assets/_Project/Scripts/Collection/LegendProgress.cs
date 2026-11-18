using System;
using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Avancée du joueur sur chaque poisson de légende (désigné par son identifiant) : fragments d'artefact pêchés et
    /// étape (<see cref="LegendState"/>). Logique pure, testée.
    /// </summary>
    public sealed class LegendProgress
    {
        private sealed class Entry
        {
            public int Fragments;
            public LegendState State;
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

        public event Action Changed;

        /// <summary>Identifiants des légendes déjà commencées.</summary>
        public IEnumerable<string> Ids => _entries.Keys;

        public int Fragments(string id) => id != null && _entries.TryGetValue(id, out Entry e) ? e.Fragments : 0;

        public LegendState State(string id) => id != null && _entries.TryGetValue(id, out Entry e) ? e.State : LegendState.Collecting;

        /// <summary>
        /// Ajoute un fragment (seulement tant qu'on les cherche). Le dernier complète l'artefact.
        /// Renvoie le nombre de fragments après l'ajout, ou -1 si rien n'a été ajouté.
        /// </summary>
        public int AddFragment(string id, int total)
        {
            if (string.IsNullOrEmpty(id) || total <= 0 || State(id) != LegendState.Collecting)
            {
                return -1;
            }

            Entry entry = Get(id);
            entry.Fragments = Math.Min(total, entry.Fragments + 1);
            if (entry.Fragments >= total)
            {
                entry.State = LegendState.Complete;
            }

            Changed?.Invoke();
            return entry.Fragments;
        }

        /// <summary>L'artefact complet est donné à la scientifique : la légende est invoquée dans sa zone.</summary>
        public bool Summon(string id)
        {
            if (State(id) != LegendState.Complete)
            {
                return false;
            }

            Get(id).State = LegendState.Summoned;
            Changed?.Invoke();
            return true;
        }

        /// <summary>La légende invoquée a été vaincue.</summary>
        public bool Beat(string id)
        {
            if (State(id) != LegendState.Summoned)
            {
                return false;
            }

            Get(id).State = LegendState.Beaten;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Remet l'état d'une sauvegarde (sans déclencher Changed).</summary>
        public void Restore(string id, int fragments, LegendState state)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            Entry entry = Get(id);
            entry.Fragments = Math.Max(0, fragments);
            entry.State = state;
        }

        private Entry Get(string id)
        {
            if (!_entries.TryGetValue(id, out Entry entry))
            {
                entry = new Entry();
                _entries[id] = entry;
            }

            return entry;
        }
    }
}
