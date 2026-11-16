using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Récompenses de collection (logique pure, testée) : dans chaque zone, découvrir 25 %, 50 % puis 100 % des
    /// espèces rapporte des pièces, une seule fois par palier. Une récompense est repérée par une clé « zone:palier ».
    /// </summary>
    public static class CollectionRewards
    {
        public static readonly int[] Percents = { 25, 50, 100 };

        /// <summary>Pièces gagnées à ce palier.</summary>
        public static int CoinsFor(int percent)
        {
            switch (percent)
            {
                case 25: return 50;
                case 50: return 150;
                case 100: return 400;
                default: return 0;
            }
        }

        public static string Key(string zoneId, int percent) => $"{zoneId}:{percent}";

        /// <summary>Le palier est-il atteint (au moins <paramref name="percent"/> % des espèces découvertes) ?</summary>
        public static bool Reached(int discovered, int total, int percent) => total > 0 && discovered * 100 >= total * percent;

        /// <summary>Paliers atteints mais pas encore encaissés, du plus petit au plus grand.</summary>
        public static List<int> Pending(string zoneId, int discovered, int total, ICollection<string> claimed)
        {
            var pending = new List<int>();
            foreach (int percent in Percents)
            {
                if (Reached(discovered, total, percent) && (claimed == null || !claimed.Contains(Key(zoneId, percent))))
                {
                    pending.Add(percent);
                }
            }

            return pending;
        }

        /// <summary>Nombre d'espèces découvertes parmi celles d'une zone.</summary>
        public static int Discovered(IEnumerable<FishSpecies> species, CatchLog log)
        {
            int count = 0;
            if (species == null || log == null)
            {
                return 0;
            }

            foreach (FishSpecies s in species)
            {
                if (s != null && log.IsDiscovered(s.Id))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
