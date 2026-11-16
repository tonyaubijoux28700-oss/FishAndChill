using System.Collections.Generic;
using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.Fishing
{
    /// <summary>Les trois sortes de coins de pêche visibles sur l'eau.</summary>
    public enum SpotKind
    {
        School,   // banc : ombres qui tournent, ça mord vite
        Bubbles,  // bulles : crustacés, tortues, grenouilles fouillent le fond
        Frenzy,   // chasse : éclaboussures, les gros poissons rôdent
    }

    /// <summary>
    /// Règles des coins de pêche (logique pure, testée) : ce que chaque coin change au tirage de l'espèce et à
    /// l'attente de la touche, et la visée qui envoie le bouchon dans un coin quand on lance face à lui.
    /// </summary>
    public static class FishingSpotRules
    {
        /// <summary>Rayon d'un coin : un bouchon posé à moins de cette distance du centre y pêche.</summary>
        public const float Radius = 1.3f;

        /// <summary>Écart maximal (en degrés) entre le regard et le coin pour que le lancer y aille tout seul.</summary>
        public const float AssistAngle = 32f;

        /// <summary>Multiplicateur de fréquence d'une espèce pêchée dans ce coin.</summary>
        public static float SpeciesMultiplier(SpotKind kind, FishSpecies species)
        {
            if (species == null)
            {
                return 1f;
            }

            switch (kind)
            {
                case SpotKind.School:
                    return species.Rarity == Rarity.Common || species.Rarity == Rarity.Uncommon ? 1.6f : 0.8f;
                case SpotKind.Bubbles:
                    return species.Kind != CreatureKind.Fish ? 4f : 1f;
                case SpotKind.Frenzy:
                    switch (species.Rarity)
                    {
                        case Rarity.Common: return 0.5f;
                        case Rarity.Uncommon: return 1f;
                        case Rarity.Rare: return 2.5f;
                        case Rarity.Epic: return 3f;
                        default: return 3.5f;
                    }

                default:
                    return 1f;
            }
        }

        /// <summary>Attente de la touche dans ce coin (fraction de l'attente normale).</summary>
        public static float WaitFactor(SpotKind kind)
        {
            switch (kind)
            {
                case SpotKind.School: return 0.45f;
                case SpotKind.Bubbles: return 0.8f;
                default: return 0.7f;
            }
        }

        /// <summary>Message quand le bouchon tombe dans le coin.</summary>
        public static string Message(SpotKind kind)
        {
            switch (kind)
            {
                case SpotKind.School: return "En plein dans le banc ! Ça va mordre vite…";
                case SpotKind.Bubbles: return "Des bulles… quelque chose fouille le fond. Patience !";
                default: return "Ça chasse ici : les gros poissons rôdent… Patience !";
            }
        }

        /// <summary>Nom du coin (aide à l'écran).</summary>
        public static string Name(SpotKind kind)
        {
            switch (kind)
            {
                case SpotKind.School: return "banc de poissons";
                case SpotKind.Bubbles: return "bulles";
                default: return "chasse";
            }
        }

        /// <summary>
        /// Coin vers lequel le lancer part tout seul : à portée (entre <paramref name="minDistance"/> et
        /// <paramref name="maxDistance"/>) et dans le regard (à moins de <see cref="AssistAngle"/>°). Le plus droit devant.
        /// Renvoie l'indice du coin, ou -1.
        /// </summary>
        public static int Assist(Vector2 origin, Vector2 facing, IList<Vector2> spots, float minDistance, float maxDistance)
        {
            int best = -1;
            float bestAngle = AssistAngle;
            if (spots == null || facing.sqrMagnitude < 0.0001f)
            {
                return best;
            }

            for (int i = 0; i < spots.Count; i++)
            {
                Vector2 to = spots[i] - origin;
                float distance = to.magnitude;
                if (distance < minDistance || distance > maxDistance + Radius * 0.5f)
                {
                    continue;
                }

                float angle = Vector2.Angle(facing, to);
                if (angle <= bestAngle)
                {
                    bestAngle = angle;
                    best = i;
                }
            }

            return best;
        }
    }
}
