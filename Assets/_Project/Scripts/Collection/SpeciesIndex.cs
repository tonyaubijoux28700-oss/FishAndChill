using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>
    /// L'index des poissons : la liste ordonnée de toutes les espèces connues du jeu (sans doublon),
    /// découvertes ou non, découpée en pages pour l'affichage. Logique pure, testée.
    /// </summary>
    public sealed class SpeciesIndex
    {
        private readonly List<FishSpecies> _species = new List<FishSpecies>();

        public SpeciesIndex(IEnumerable<FishSpecies> species)
        {
            var seen = new HashSet<string>();
            if (species == null)
            {
                return;
            }

            foreach (FishSpecies s in species)
            {
                if (s != null && seen.Add(s.Id))
                {
                    _species.Add(s);
                }
            }
        }

        public int Count => _species.Count;

        public IReadOnlyList<FishSpecies> Species => _species;

        public int DiscoveredCount(CatchLog log)
        {
            int count = 0;
            foreach (FishSpecies s in _species)
            {
                if (log != null && log.IsDiscovered(s.Id))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Nombre de pages (au moins une, même vide).</summary>
        public int PageCount(int perPage)
        {
            if (perPage <= 0 || _species.Count == 0)
            {
                return 1;
            }

            return (_species.Count + perPage - 1) / perPage;
        }

        /// <summary>Page qui contient l'espèce numéro <paramref name="index"/>.</summary>
        public static int PageOf(int index, int perPage) => perPage <= 0 || index < 0 ? 0 : index / perPage;

        /// <summary>
        /// Déplace la sélection dans une grille de <paramref name="columns"/> colonnes : gauche/droite passent
        /// d'une case à la suivante (et d'une page à l'autre), haut/bas sautent d'une ligne. Reste dans la liste.
        /// </summary>
        public static int Move(int selected, int dx, int dy, int columns, int total)
        {
            if (total <= 0)
            {
                return 0;
            }

            int next = selected + dx + dy * columns;
            if (next < 0 || next >= total)
            {
                // Haut/bas hors de la liste : on ne bouge pas ; gauche/droite : on bute sur le bord.
                return dy != 0 ? selected : (next < 0 ? 0 : total - 1);
            }

            return next;
        }
    }
}
