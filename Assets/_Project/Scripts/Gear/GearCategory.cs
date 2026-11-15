namespace FishAndChill.Gear
{
    /// <summary>Les familles de matériel, améliorables chacune par niveaux.</summary>
    public enum GearCategory
    {
        /// <summary>Canne : zone de réussite plus large, curseur plus lent.</summary>
        Rod,

        /// <summary>Hameçon : plus de ratés tolérés avant que le poisson s'échappe.</summary>
        Hook,

        /// <summary>Appât : plus de poissons rares, touches plus rapides.</summary>
        Bait,

        /// <summary>Sac : plus de places.</summary>
        Bag,
    }
}
