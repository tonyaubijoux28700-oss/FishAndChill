namespace FishAndChill.Collection
{
    /// <summary>Grande famille d'une espèce pêchable : on ne pêche pas que des poissons.</summary>
    public enum CreatureKind
    {
        Fish,
        Crustacean,
        Reptile,
        Amphibian,
        /// <summary>Poulpes, seiches et calmars : ils nagent (pas de casier), et se balancent au lieu de frétiller.</summary>
        Cephalopod,
    }
}
