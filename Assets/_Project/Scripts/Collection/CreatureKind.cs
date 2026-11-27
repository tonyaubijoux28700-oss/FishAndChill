namespace FishAndChill.Collection
{
    /// <summary>Grande famille d'une espèce : on ne pêche pas que des poissons. Nouvelles valeurs toujours à la fin.</summary>
    public enum CreatureKind
    {
        Fish,
        Crustacean,
        Reptile,
        Amphibian,
        /// <summary>Poulpes, seiches et calmars : ils nagent, et se balancent au lieu de frétiller.</summary>
        Cephalopod,
        /// <summary>Coquillages et escargots, limaces de mer.</summary>
        Mollusc,
        /// <summary>Étoiles de mer, ophiures, oursins, concombres de mer.</summary>
        Echinoderm,
        /// <summary>Méduses, physalie.</summary>
        Cnidarian,
        /// <summary>Vers plats, sangsues.</summary>
        Worm,
        /// <summary>Limule.</summary>
        Chelicerate,
    }
}
