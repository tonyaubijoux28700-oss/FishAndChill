using System;

namespace FishAndChill.Villagers
{
    /// <summary>
    /// Ce qu'un habitant peut dire (données de <see cref="VillagerProfile"/>). <see cref="Intro"/> : sa présentation,
    /// plusieurs répliques à la suite, au premier échange. Ensuite une réplique à la fois, choisie par
    /// <see cref="VillagerDialogue"/> selon le moment : de tous les jours, sous la pluie, la nuit, ou à propos de
    /// l'aquarium (« {0} » = nombre d'espèces exposées).
    /// </summary>
    [Serializable]
    public sealed class VillagerLines
    {
        public string[] Intro = new string[0];
        public string[] Daily = new string[0];
        public string[] Rain = new string[0];
        public string[] Night = new string[0];
        public string[] Aquarium = new string[0];
    }
}
