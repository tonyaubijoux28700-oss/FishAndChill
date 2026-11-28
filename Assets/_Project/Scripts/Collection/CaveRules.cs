using System;
using System.Collections.Generic;

namespace FishAndChill.Collection
{
    /// <summary>
    /// La flaque de la grotte de Chiyo (logique pure, testée) : ses bêtes se montrent l'une après l'autre, dans l'ordre
    /// (celles déjà au carnet, plus la suivante), et on n'y donne qu'une prise par jour.
    /// </summary>
    public static class CaveRules
    {
        /// <summary>Identifiant de la zone de la grotte (nom de son asset).</summary>
        public const string ZoneId = "GrotteHaLong";

        /// <summary>Les bêtes présentes : toutes celles déjà découvertes, plus la première pas encore découverte.</summary>
        public static List<string> Available(IList<string> ordered, Func<string, bool> discovered)
        {
            var result = new List<string>();
            foreach (string id in ordered)
            {
                result.Add(id);
                if (!discovered(id)) break;
            }

            return result;
        }

        /// <summary>Jour de jeu (0, 1, 2…) à partir des heures écoulées depuis le début de la partie.</summary>
        public static int DayOf(double totalHours) => (int)Math.Floor(totalHours / 24.0);

        /// <summary>Peut-on pêcher dans la flaque aujourd'hui (pas encore de prise ce jour-là) ?</summary>
        public static bool CanFish(int lastCatchDay, int today) => lastCatchDay != today;
    }
}
