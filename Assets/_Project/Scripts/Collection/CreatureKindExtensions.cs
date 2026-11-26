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

        /// <summary>Les crustacés ne s'attrapent qu'au casier, jamais à la canne.</summary>
        public static bool CaughtInTrap(this CreatureKind kind) => kind == CreatureKind.Crustacean;
    }
}
