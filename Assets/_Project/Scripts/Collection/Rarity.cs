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
    }
}
