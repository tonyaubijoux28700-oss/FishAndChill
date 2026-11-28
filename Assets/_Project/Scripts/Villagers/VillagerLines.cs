using System;

namespace FishAndChill.Villagers
{
    /// <summary>
    /// Ce qu'un habitant peut dire (données de <see cref="VillagerProfile"/>). <see cref="Intro"/> : sa présentation,
    /// plusieurs répliques à la suite, au premier échange. Ensuite une réplique à la fois, choisie par
    /// <see cref="VillagerDialogue"/> selon le moment : de tous les jours, sous la pluie, la nuit, ou à propos de
    /// l'aquarium (« {0} » = nombre d'espèces exposées). Pour les commandes (« {0} » = nom du poisson) : la demande,
    /// le rappel, le merci (« {1} » = pièces gagnées), et « garde-le d'abord pour ton aquarium ».
    /// </summary>
    [Serializable]
    public sealed class VillagerLines
    {
        public string[] Intro = new string[0];
        public string[] Daily = new string[0];
        public string[] Rain = new string[0];
        public string[] Night = new string[0];
        public string[] Aquarium = new string[0];
        public string[] Ask = new string[0];
        public string[] Waiting = new string[0];
        public string[] Thanks = new string[0];
        public string[] KeepIt = new string[0];

        /// <summary>Ce qu'elle dit en te donnant l'épuisette (seulement Chiyo, tant qu'on n'en a pas).</summary>
        public string[] NetGift = new string[0];
    }
}
