using System;
using System.Collections.Generic;

namespace FishAndChill.Save
{
    /// <summary>
    /// Les 3 emplacements de sauvegarde (logique pure, testée) : nom du fichier de chaque emplacement et textes
    /// affichés sur l'écran titre. L'emplacement 1 garde l'ancien nom de fichier, pour reprendre les parties
    /// faites avant l'arrivée des emplacements.
    /// </summary>
    public static class SaveSlots
    {
        public const int Count = 3;

        /// <summary>Fichier de l'emplacement (0, 1 ou 2) : « sauvegarde.json », « sauvegarde2.json »…</summary>
        public static string FileName(int slot)
        {
            int index = Math.Max(0, Math.Min(Count - 1, slot));
            return index == 0 ? SaveManager.FileName : $"sauvegarde{index + 1}.json";
        }

        /// <summary>Nombre d'espèces différentes déjà pêchées dans cette partie.</summary>
        public static int SpeciesDiscovered(SaveData data)
        {
            if (data == null || data.CatchLog == null)
            {
                return 0;
            }

            var ids = new HashSet<string>();
            foreach (SaveData.Catches entry in data.CatchLog)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Id) && entry.Count > 0)
                {
                    ids.Add(entry.Id);
                }
            }

            return ids.Count;
        }

        /// <summary>Temps de jeu lisible : « 12 min », « 1 h 05 ».</summary>
        public static string PlayTimeText(float seconds)
        {
            int minutes = (int)Math.Floor(Math.Max(0f, seconds) / 60f);
            return minutes < 60 ? $"{minutes} min" : $"{minutes / 60} h {minutes % 60:00}";
        }

        /// <summary>Date de la dernière partie : « 16/11 à 14:30 », vide si inconnue.</summary>
        public static string SavedAtText(long ticks)
        {
            if (ticks <= 0)
            {
                return string.Empty;
            }

            var date = new DateTime(ticks);
            return $"{date:dd/MM} à {date:HH:mm}";
        }
    }
}
