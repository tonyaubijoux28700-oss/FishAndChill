using System;

namespace FishAndChill.Villagers
{
    /// <summary>
    /// La commande en cours d'un habitant : l'espèce demandée (vide = aucune), s'il l'a déjà demandée au joueur, et
    /// s'il a déjà été servi aujourd'hui (la suivante arrive le lendemain matin). Sauvegardé avec la partie.
    /// </summary>
    [Serializable]
    public sealed class VillagerRequest
    {
        public string VillagerId;
        public string SpeciesId;
        public bool Asked;
        public bool DoneToday;

        public bool IsOpen => !string.IsNullOrEmpty(SpeciesId);
    }
}
