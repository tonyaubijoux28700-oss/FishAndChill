using System;
using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Avancée du joueur sur chaque poisson de légende (désigné par son identifiant) : fragments d'artefact pêchés,
    /// fragments posés sur le socle du laboratoire, et étape (<see cref="LegendState"/>). Un fragment pêché va dans la
    /// poche ; l'artefact n'est complet qu'une fois tous ses fragments posés, un par un. Logique pure, testée.
    /// </summary>
    public sealed class LegendProgress
    {
        private sealed class Entry
        {
            public int Fragments;
            public int Placed;
            public LegendState State;
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

        public event Action Changed;

        /// <summary>Identifiants des légendes déjà commencées.</summary>
        public IEnumerable<string> Ids => _entries.Keys;

        /// <summary>Fragments pêchés (posés ou non).</summary>
        public int Fragments(string id) => id != null && _entries.TryGetValue(id, out Entry e) ? e.Fragments : 0;

        /// <summary>Fragments déjà posés sur l'artefact (les premiers trouvés, dans l'ordre).</summary>
        public int Placed(string id) => id != null && _entries.TryGetValue(id, out Entry e) ? e.Placed : 0;

        /// <summary>Fragments pêchés mais pas encore posés.</summary>
        public int InHand(string id) => Fragments(id) - Placed(id);

        public LegendState State(string id) => id != null && _entries.TryGetValue(id, out Entry e) ? e.State : LegendState.Collecting;

        /// <summary>Reste-t-il des fragments à pêcher pour cet artefact ?</summary>
        public bool CanFindMore(string id, int total) => State(id) == LegendState.Collecting && Fragments(id) < total;

        /// <summary>
        /// Ajoute un fragment pêché (tant qu'il en reste à trouver). Renvoie le nombre de fragments pêchés après
        /// l'ajout, ou -1 si rien n'a été ajouté.
        /// </summary>
        public int AddFragment(string id, int total)
        {
            if (string.IsNullOrEmpty(id) || total <= 0 || !CanFindMore(id, total))
            {
                return -1;
            }

            Entry entry = Get(id);
            entry.Fragments++;
            Changed?.Invoke();
            return entry.Fragments;
        }

        /// <summary>
        /// Pose sur l'artefact le prochain fragment qu'on a en poche. Le dernier posé complète l'artefact.
        /// Renvoie le numéro (à partir de 0) du fragment posé, ou -1 s'il n'y en avait aucun à poser.
        /// </summary>
        public int PlaceFragment(string id, int total)
        {
            if (string.IsNullOrEmpty(id) || State(id) != LegendState.Collecting || InHand(id) <= 0 || Placed(id) >= total)
            {
                return -1;
            }

            Entry entry = Get(id);
            int index = entry.Placed;
            entry.Placed++;
            if (entry.Placed >= total)
            {
                entry.State = LegendState.Complete;
            }

            Changed?.Invoke();
            return index;
        }

        /// <summary>L'artefact complet est activé : la légende est invoquée dans sa zone.</summary>
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

        /// <summary>Oublie tout d'une légende (essais) : aucun fragment, rien de posé.</summary>
        public void Reset(string id)
        {
            if (!string.IsNullOrEmpty(id) && _entries.Remove(id))
            {
                Changed?.Invoke();
            }
        }

        /// <summary>Remet l'état d'une sauvegarde (sans déclencher Changed). Les posés ne dépassent jamais les pêchés.</summary>
        public void Restore(string id, int fragments, int placed, LegendState state)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            Entry entry = Get(id);
            entry.Fragments = Math.Max(0, fragments);
            entry.Placed = Math.Max(0, Math.Min(placed, entry.Fragments));
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
