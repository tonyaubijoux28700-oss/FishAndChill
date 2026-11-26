using System.Collections.Generic;
using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Règles de la plongée (logique pure, testée) : combien d'air on a, quelles décorations sont posées où à chaque
    /// plongée (les communes partout, les rares seulement loin de la corde), et ce qu'on garde si l'air vient à manquer.
    /// </summary>
    public static class DiveMath
    {
        /// <summary>Secondes d'air avec le masque et le tuba.</summary>
        public const float MaskAir = 45f;

        /// <summary>Décorations posées sur le fond à chaque plongée.</summary>
        public const int ItemsPerDive = 8;

        /// <summary>Distance à la corde (0 à 1) à partir de laquelle on peut trouver une décoration rare.</summary>
        public const float RareRemoteness = 0.45f;

        public static float Weight(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return 1f;
                case Rarity.Uncommon: return 0.45f;
                case Rarity.Rare: return 0.14f;
                default: return 0.05f;
            }
        }

        /// <summary>
        /// Tire les décorations de cette plongée : <paramref name="count"/> emplacements au hasard parmi ceux du plan, et pour
        /// chacun une décoration selon sa rareté (les rares seulement aux emplacements éloignés de la corde).
        /// </summary>
        public static List<(Vector2 Spot, int Item)> Pick(DivePlan plan, IReadOnlyList<Rarity> items, System.Random random, int count = ItemsPerDive)
        {
            var result = new List<(Vector2, int)>();
            if (plan == null || items == null || items.Count == 0) return result;
            var spots = new List<Vector2>(plan.Spots);
            for (int i = spots.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (spots[i], spots[j]) = (spots[j], spots[i]);
            }

            for (int s = 0; s < spots.Count && result.Count < count; s++)
            {
                bool far = plan.Remoteness(spots[s]) >= RareRemoteness;
                float total = 0f;
                for (int i = 0; i < items.Count; i++) if (far || items[i] <= Rarity.Uncommon) total += Weight(items[i]);
                if (total <= 0f) continue;
                float roll = (float)random.NextDouble() * total;
                for (int i = 0; i < items.Count; i++)
                {
                    if (!far && items[i] > Rarity.Uncommon) continue;
                    roll -= Weight(items[i]);
                    if (roll <= 0f) { result.Add((spots[s], i)); break; }
                }
            }

            return result;
        }

        /// <summary>À court d'air, on remonte en lâchant la moitié de ce qu'on portait (arrondi en sa défaveur).</summary>
        public static int KeptWhenOutOfAir(int carried) => carried / 2;
    }
}
