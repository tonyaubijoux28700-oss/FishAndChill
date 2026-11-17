using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.UI
{
    /// <summary>Calculs de l'éclat qui passe sur les écailles des espèces rares (logique pure, testée).</summary>
    public static class ShineMath
    {
        /// <summary>Durée d'un passage de la bande de lumière (secondes).</summary>
        public const float SweepSeconds = 0.9f;

        /// <summary>Les espèces rares, épiques et légendaires brillent ; les autres non.</summary>
        public static bool Shines(Rarity rarity) => rarity >= Rarity.Rare;

        /// <summary>Temps entre deux éclats : plus la rareté est haute, plus ils sont fréquents.</summary>
        public static float Period(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Legendary: return 1.6f;
                case Rarity.Epic: return 2.2f;
                default: return 3.0f;
            }
        }

        /// <summary>
        /// Avancée de l'éclat à l'instant <paramref name="time"/> : de 0 (bord gauche) à 1 (bord droit) pendant le passage,
        /// -1 entre deux passages. <paramref name="offset"/> décale les poissons pour qu'ils ne brillent pas tous ensemble.
        /// </summary>
        public static float Sweep(float time, float period, float offset = 0f)
        {
            float t = Mathf.Repeat(time + offset, period);
            return t < SweepSeconds ? t / SweepSeconds : -1f;
        }

        /// <summary>Couleur de l'éclat : blanc, un peu teinté de la couleur de la rareté (doré pour une légendaire).</summary>
        public static Color Tint(Rarity rarity)
        {
            Color c = Color.Lerp(Color.white, rarity.DisplayColor(), rarity == Rarity.Legendary ? 0.2f : 0.12f);
            c.a = rarity == Rarity.Legendary ? 0.5f : 0.42f;
            return c;
        }
    }
}
