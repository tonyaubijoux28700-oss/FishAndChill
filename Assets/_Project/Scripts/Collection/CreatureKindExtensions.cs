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
                default: return "Poisson";
            }
        }

        /// <summary>Dans l'aquarium, seuls les poissons nagent : les autres se tiennent au fond.</summary>
        public static bool LivesOnBottom(this CreatureKind kind) => kind != CreatureKind.Fish;

        /// <summary>Les crustacés ne s'attrapent qu'au casier, jamais à la canne.</summary>
        public static bool CaughtInTrap(this CreatureKind kind) => kind == CreatureKind.Crustacean;
    }
}
