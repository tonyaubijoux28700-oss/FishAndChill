using FishAndChill.Collection;
using FishAndChill.Fishing;
using UnityEngine;

namespace FishAndChill.Gear
{
    /// <summary>
    /// Ce que le matériel change à la pêche (valeurs tirées des niveaux du catalogue) :
    /// canne → zone de réussite plus large et curseur plus lent ; hameçon → ratés tolérés en plus ;
    /// appât → espèces rares plus fréquentes et touches plus rapides ; sac → nombre de places.
    /// </summary>
    public readonly struct GearEffects
    {
        /// <summary>Part de la largeur de zone gagnée par la canne : 0,15 = zone 15 % plus large.</summary>
        public readonly float ZoneBonus;

        /// <summary>Ratés tolérés en plus grâce à l'hameçon.</summary>
        public readonly int ExtraMisses;

        /// <summary>Bonus de rareté de l'appât (0 = aucun).</summary>
        public readonly float RarityBonus;

        /// <summary>Part du temps d'attente gagnée par l'appât : 0,15 = touche 15 % plus vite.</summary>
        public readonly float FasterBites;

        /// <summary>Places du sac (0 = ne pas changer).</summary>
        public readonly int BagCapacity;

        /// <summary>Aucun matériel amélioré.</summary>
        public static readonly GearEffects None = new GearEffects(0f, 0, 0f, 0f, 0);

        public GearEffects(float zoneBonus, int extraMisses, float rarityBonus, float fasterBites, int bagCapacity)
        {
            ZoneBonus = zoneBonus;
            ExtraMisses = extraMisses;
            RarityBonus = rarityBonus;
            FasterBites = fasterBites;
            BagCapacity = bagCapacity;
        }

        /// <summary>Effets du matériel actuel du joueur.</summary>
        public static GearEffects From(GearCatalog catalog, GearLoadout loadout)
        {
            if (catalog == null || loadout == null)
            {
                return None;
            }

            GearLevel Current(GearCategory category)
            {
                GearLine line = catalog.Line(category);
                return line != null ? line.At(loadout.Level(category)) : null;
            }

            GearLevel rod = Current(GearCategory.Rod);
            GearLevel hook = Current(GearCategory.Hook);
            GearLevel bait = Current(GearCategory.Bait);
            GearLevel bag = Current(GearCategory.Bag);
            return new GearEffects(
                rod?.Value ?? 0f,
                Mathf.RoundToInt(hook?.Value ?? 0f),
                bait?.Value ?? 0f,
                bait?.Value2 ?? 0f,
                Mathf.RoundToInt(bag?.Value ?? 0f));
        }

        /// <summary>
        /// Le QTE avec le matériel : zone plus large (sans dépasser 60 % de la barre), curseur ralenti de moitié
        /// du bonus de la canne, ratés tolérés en plus.
        /// </summary>
        public QteDifficulty Apply(QteDifficulty difficulty)
        {
            if (difficulty == null)
            {
                return null;
            }

            float zone = Mathf.Min(0.6f, difficulty.ZoneWidth * (1f + ZoneBonus));
            float speed = difficulty.CursorSpeed * Mathf.Max(0.5f, 1f - ZoneBonus * 0.45f);
            return new QteDifficulty(speed, zone, difficulty.RequiredHits, difficulty.MaxMisses + ExtraMisses);
        }

        /// <summary>Multiplicateur de fréquence d'une espèce selon l'appât : plus elle est rare, plus il compte.</summary>
        public float RarityMultiplier(Rarity rarity)
        {
            return 1f + RarityBonus * (int)rarity * 0.5f;
        }

        /// <summary>Facteur appliqué au temps d'attente de la touche (1 = inchangé).</summary>
        public float BiteWaitFactor => Mathf.Clamp(1f - FasterBites, 0.3f, 1f);
    }
}
