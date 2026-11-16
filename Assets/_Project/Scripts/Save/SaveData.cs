using System;
using System.Collections.Generic;

namespace FishAndChill.Save
{
    /// <summary>
    /// Ce qui est sauvegardé (en JSON) : pièces, sac, aquarium, matériel et journal des prises.
    /// Les espèces sont désignées par leur identifiant stable (<c>FishSpecies.Id</c>), jamais par une référence.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        [Serializable]
        public sealed class Specimen
        {
            public string Id;
            public float SizeCm;
        }

        [Serializable]
        public sealed class Catches
        {
            public string Id;
            public int Count;
            public float BestSizeCm;
        }

        [Serializable]
        public sealed class GearLevel
        {
            public int Category;
            public int Level;
        }

        public int Version = CurrentVersion;
        public int Coins;
        public int BagCapacity;
        public List<Specimen> Bag = new List<Specimen>();
        public List<Specimen> Aquarium = new List<Specimen>();
        public List<GearLevel> Gear = new List<GearLevel>();
        public List<Catches> CatchLog = new List<Catches>();
        public int TotalCatches;

        /// <summary>Zone où se trouve le joueur (identifiant de la FishingZone) ; vide = zone de départ.</summary>
        public string Zone;

        /// <summary>Temps de jeu total, en secondes.</summary>
        public float PlaySeconds;

        /// <summary>Date de la dernière sauvegarde (ticks de DateTime, heure locale) ; 0 = inconnue.</summary>
        public long SavedAtTicks;
    }
}
