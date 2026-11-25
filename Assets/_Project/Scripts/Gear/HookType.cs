namespace FishAndChill.Gear
{
    /// <summary>
    /// Les hameçons du marchand, dans l'ordre de la famille « Hameçon » du catalogue (le numéro d'objet équipé est ce
    /// type) : les quatre premiers gardent la place des anciens hameçons (simple, triple, circulaire, forgé).
    /// </summary>
    public enum HookType
    {
        /// <summary>Simple fin à longue hampe (Aberdeen) : petits poissons, appâts naturels.</summary>
        Aberdeen,

        /// <summary>Triple (ancre) : cuillères, poissons nageurs, leurres de surface, jigs.</summary>
        Treble,

        /// <summary>Circulaire : gros poissons du large, requins (il se plante au coin de la mâchoire).</summary>
        Circle,

        /// <summary>Simple forgé O'Shaughnessy : gros poissons de mer, appâts entiers.</summary>
        Forged,

        /// <summary>Texan (offset, hampe coudée) : leurres souples, dans les herbes et les rochers.</summary>
        Offset,

        /// <summary>Hameçon à mouche : léger, œillet relevé.</summary>
        Fly,
    }
}
