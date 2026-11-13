using UnityEngine;

namespace FishAndChill.Collection
{
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
    }

    public static class RarityExtensions
    {
        public static string DisplayName(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return "Commun";
                case Rarity.Uncommon: return "Peu commun";
                case Rarity.Rare: return "Rare";
                case Rarity.Epic: return "Épique";
                case Rarity.Legendary: return "Légendaire";
                default: return rarity.ToString();
            }
        }

        /// <summary>Couleur associée à la rareté (cadres, titres).</summary>
        public static Color DisplayColor(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Uncommon: return new Color(0.40f, 0.85f, 0.45f);
                case Rarity.Rare: return new Color(0.35f, 0.65f, 1.00f);
                case Rarity.Epic: return new Color(0.75f, 0.45f, 1.00f);
                case Rarity.Legendary: return new Color(1.00f, 0.80f, 0.25f);
                default: return new Color(0.80f, 0.82f, 0.85f);
            }
        }
    }
}
