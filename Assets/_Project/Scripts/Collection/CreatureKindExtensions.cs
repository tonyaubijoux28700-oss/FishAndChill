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
                case CreatureKind.Mollusc: return "Mollusque";
                case CreatureKind.Echinoderm: return "Échinoderme";
                case CreatureKind.Cnidarian: return "Cnidaire";
                case CreatureKind.Worm: return "Ver";
                case CreatureKind.Chelicerate: return "Limule";
                case CreatureKind.Insect: return "Insecte";
                case CreatureKind.Tunicate: return "Tunicier";
                default: return "Poisson";
            }
        }

        /// <summary>Dans l'aquarium, poissons et céphalopodes nagent : les autres se tiennent au fond.</summary>
        public static bool LivesOnBottom(this CreatureKind kind) => kind != CreatureKind.Fish && kind != CreatureKind.Cephalopod;

        /// <summary>Engin par défaut quand la colonne Engin est vide : la canne pour les poissons, l'épuisette pour le reste.</summary>
        public static CatchMethod DefaultMethod(this CreatureKind kind) => kind == CreatureKind.Fish ? CatchMethod.Rod : CatchMethod.Net;
    }
}
