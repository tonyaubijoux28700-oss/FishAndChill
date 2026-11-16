using System;
using FishAndChill.Collection;
using FishAndChill.Gear;
using UnityEngine;

namespace FishAndChill.Save
{
    /// <summary>
    /// Passe de la partie en cours à la sauvegarde et inversement (logique pure, testée, sans fichier).
    /// Une espèce inconnue (retirée du jeu) est ignorée au chargement plutôt que de faire planter la partie.
    /// </summary>
    public static class SaveSerializer
    {
        public static SaveData Capture(Inventory bag, AquariumCollection aquarium, Wallet wallet, GearLoadout gear, CatchLog log)
        {
            var data = new SaveData
            {
                Coins = wallet.Coins,
                BagCapacity = bag.Capacity,
                TotalCatches = log.TotalCatches,
            };

            foreach (CaughtFish fish in bag.Fish)
            {
                data.Bag.Add(new SaveData.Specimen { Id = fish.SpeciesId, SizeCm = fish.SizeCm });
            }

            foreach (CaughtFish fish in aquarium.Specimens)
            {
                data.Aquarium.Add(new SaveData.Specimen { Id = fish.SpeciesId, SizeCm = fish.SizeCm });
            }

            foreach (GearCategory category in Enum.GetValues(typeof(GearCategory)))
            {
                int level = gear.Level(category);
                if (level > 0)
                {
                    data.Gear.Add(new SaveData.GearLevel { Category = (int)category, Level = level });
                }
            }

            foreach ((GearCategory category, int index) in gear.OwnedItems)
            {
                data.GearOwned.Add(new SaveData.GearLevel { Category = (int)category, Level = index });
            }

            foreach (string id in log.SpeciesIds)
            {
                data.CatchLog.Add(new SaveData.Catches { Id = id, Count = log.CountOf(id), BestSizeCm = log.BestSizeOf(id) });
            }

            return data;
        }

        /// <summary>
        /// Remet la partie dans l'état de la sauvegarde (à appeler sur une partie neuve).
        /// <paramref name="findSpecies"/> retrouve une espèce par son identifiant (null si elle n'existe plus).
        /// </summary>
        public static void Restore(SaveData data, Func<string, FishSpecies> findSpecies,
            Inventory bag, AquariumCollection aquarium, Wallet wallet, GearLoadout gear, CatchLog log)
        {
            if (data == null)
            {
                return;
            }

            foreach (SaveData.GearLevel entry in data.Gear)
            {
                if (Enum.IsDefined(typeof(GearCategory), entry.Category))
                {
                    gear.SetLevel((GearCategory)entry.Category, entry.Level);
                }
            }

            bool legacy = data.GearOwned == null || data.GearOwned.Count == 0;
            if (legacy)
            {
                // Ancienne sauvegarde (matériel à niveaux) : on garde tout ce qui était en dessous du niveau atteint.
                foreach (SaveData.GearLevel entry in data.Gear)
                {
                    for (int i = 1; i <= entry.Level && entry.Category != (int)GearCategory.Bag; i++)
                    {
                        gear.Own((GearCategory)entry.Category, i);
                    }
                }
            }
            else
            {
                foreach (SaveData.GearLevel entry in data.GearOwned)
                {
                    if (Enum.IsDefined(typeof(GearCategory), entry.Category))
                    {
                        gear.Own((GearCategory)entry.Category, entry.Level);
                    }
                }
            }

            bag.UpgradeCapacity(data.BagCapacity);
            foreach (SaveData.Specimen saved in data.Bag)
            {
                FishSpecies species = findSpecies(saved.Id);
                if (species != null)
                {
                    bag.TryAdd(new CaughtFish(species, saved.SizeCm));
                }
            }

            foreach (SaveData.Specimen saved in data.Aquarium)
            {
                FishSpecies species = findSpecies(saved.Id);
                if (species != null)
                {
                    aquarium.Restore(new CaughtFish(species, saved.SizeCm));
                }
            }

            foreach (SaveData.Catches entry in data.CatchLog)
            {
                log.Restore(entry.Id, entry.Count, entry.BestSizeCm);
            }

            log.RestoreTotal(data.TotalCatches);
            wallet.Add(Mathf.Max(0, data.Coins));
        }

        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, true);

        /// <summary>Relit une sauvegarde ; null si le texte est vide ou abîmé.</summary>
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
