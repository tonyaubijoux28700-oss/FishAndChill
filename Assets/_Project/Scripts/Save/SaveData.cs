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

        /// <summary>Cannes et hameçons achetés (Level = numéro de l'objet), en plus de ceux du départ.</summary>
        public List<GearLevel> GearOwned = new List<GearLevel>();
        public List<Catches> CatchLog = new List<Catches>();
        public int TotalCatches;

        /// <summary>Zone où se trouve le joueur (identifiant de la FishingZone) ; vide = zone de départ.</summary>
        public string Zone;

        /// <summary>Temps de jeu total, en secondes.</summary>
        public float PlaySeconds;

        /// <summary>Date de la dernière sauvegarde (ticks de DateTime, heure locale) ; 0 = inconnue.</summary>
        public long SavedAtTicks;

        /// <summary>Heure de la journée (0 à 24) ; 8 h pour une ancienne sauvegarde.</summary>
        public float Hour = 8f;

        /// <summary>Temps qu'il fait (numéro de <c>Weather</c>) ; -1 pour une ancienne sauvegarde (beau temps).</summary>
        public int Weather = -1;

        /// <summary>Heures de jeu avant le prochain changement de temps.</summary>
        public float WeatherHours;

        /// <summary>Récompenses de collection encaissées (« zone:palier »).</summary>
        public List<string> Rewards = new List<string>();

        /// <summary>Habitants déjà rencontrés (ils ne se présentent qu'une fois).</summary>
        public List<string> MetVillagers = new List<string>();

        /// <summary>Commandes en cours des habitants.</summary>
        public List<Villagers.VillagerRequest> VillagerRequests = new List<Villagers.VillagerRequest>();

        /// <summary>Animaux de compagnie achetés, et celui qui accompagne le joueur (vide : aucun).</summary>
        public List<string> Pets = new List<string>();
        public string EquippedPet;

        /// <summary>L'épuisette a-t-elle été achetée ?</summary>
        public bool HasNet;

        /// <summary>Perles du collier de Chiyo : trouvées (pas encore rapportées) et enfilées, par identifiant de zone.</summary>
        public List<string> PearlsFound = new List<string>();
        public List<string> PearlsThreaded = new List<string>();

        /// <summary>Jour de la dernière prise dans la flaque de la grotte (-1 : jamais).</summary>
        public int CaveDay = -1;

        /// <summary>Heures de jeu écoulées depuis le début de la partie (0 pour une ancienne sauvegarde).</summary>
        public double TotalHours;

        [Serializable]
        public sealed class BaitStock
        {
            public string Id;
            public int Count;   // -1 = illimité
        }

        /// <summary>Appâts possédés et appât accroché à la ligne.</summary>
        public List<BaitStock> Baits = new List<BaitStock>();
        public string EquippedBait;

        [Serializable]
        public sealed class Legend
        {
            public string Id;
            public int Fragments;
            public int Placed;  // fragments posés sur l'artefact, au laboratoire
            public int State;   // Collection.LegendState
        }

        /// <summary>Poissons de légende : fragments d'artefact pêchés et étape (vide pour une ancienne sauvegarde).</summary>
        public List<Legend> Legends = new List<Legend>();

        /// <summary>Dernier artefact assemblé, celui qui trône sur le socle du laboratoire (vide : ancienne sauvegarde).</summary>
        public string LastArtifact;

        /// <summary>Prénom et apparence du pêcheur (ancienne sauvegarde : le pêcheur d'origine, sans prénom).</summary>
        public Player.CharacterLook Look = new Player.CharacterLook();
    }
}
