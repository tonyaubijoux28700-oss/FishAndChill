namespace FishAndChill.Collection
{
    public static class CreatureKindExtensions
    {
        public static string DisplayName(this CreatureKind kind)
        {
            switch (kind)
            {
                case CreatureKind.Crustacean: return "Crustacé";
                case CreatureKind.Reptile: return "Reptile";
                case CreatureKind.Amphibian: return "Amphibien";
                case CreatureKind.Cephalopod: return "Céphalopode";
                default: return "Poisson";
            }
        }

        /// <summary>Dans l'aquarium, poissons et céphalopodes nagent : les autres se tiennent au fond.</summary>
        public static bool LivesOnBottom(this CreatureKind kind) => kind != CreatureKind.Fish && kind != CreatureKind.Cephalopod;

        /// <summary>
        /// Crustacés, céphalopodes, reptiles, amphibiens : mis de côté pour la plongée. Leur fiche reste (deux légendes
        /// s'appuient dessus) mais ils ne sont ni dans les zones, ni dans le carnet, ni à la canne.
        /// </summary>
        public static bool SetAside(this CreatureKind kind) => kind != CreatureKind.Fish;
    }
}
