using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.Fishing
{
    /// <summary>Comment une bestiole de la flaque réagit quand elle s'inquiète.</summary>
    public enum CritterBehaviour
    {
        /// <summary>Elle court se cacher sous le rocher le plus proche, puis ressort (crabes, crevettes, limule…).</summary>
        Hide,
        /// <summary>Elle bondit loin de l'épuisette (lamproie, vers, tortues).</summary>
        Dart,
        /// <summary>Presque invisible tant qu'elle est calme ; inquiète, elle lâche de l'encre et réapparaît ailleurs (céphalopodes).</summary>
        Ink,
        /// <summary>Elle ne fuit pas mais s'enfouit sur place un moment (coquillages, oursins, étoiles, méduses).</summary>
        Burrow,
    }

    /// <summary>
    /// Règles de la pêche à l'épuisette (logique pure, testée), en pixels de la flaque (320×192) : l'épuisette suit la
    /// souris avec du retard ; bougée vite près d'une bestiole, elle l'inquiète. Un coup d'épuisette éclabousse : une
    /// bestiole déjà inquiète file, une calme sous le cercle est prise. Plus elle est rare, plus elle est grande et méfiante.
    /// </summary>
    public static class NetRules
    {
        /// <summary>Coups d'épuisette par flaque (une seule prise : chaque raté compte).</summary>
        public const int Tries = 3;

        /// <summary>Bestioles dans la flaque à l'ouverture.</summary>
        public const int Critters = 5;

        /// <summary>Rayon du cercle de l'épuisette (pixels de la flaque).</summary>
        public const float NetRadius = 14f;

        /// <summary>Durée d'un coup d'épuisette (le cercle se referme).</summary>
        public const float ScoopSeconds = 0.35f;

        /// <summary>Vitesse de l'épuisette (pixels par seconde) au-delà de laquelle l'eau se trouble.</summary>
        public const float CalmSpeed = 66f;

        /// <summary>Distance à laquelle une épuisette agitée inquiète.</summary>
        public const float WarnDistance = 76f;

        /// <summary>Distance de l'éclaboussure d'un coup d'épuisette.</summary>
        public const float SplashDistance = 44f;

        /// <summary>Retard de l'épuisette dans l'eau : part du chemin vers la souris faite en une seconde.</summary>
        public const float Drag = 8.4f;

        public static CritterBehaviour Behaviour(CreatureKind kind)
        {
            switch (kind)
            {
                case CreatureKind.Crustacean:
                case CreatureKind.Chelicerate:
                case CreatureKind.Amphibian:
                    return CritterBehaviour.Hide;
                case CreatureKind.Cephalopod:
                    return CritterBehaviour.Ink;
                case CreatureKind.Mollusc:
                case CreatureKind.Echinoderm:
                case CreatureKind.Cnidarian:
                case CreatureKind.Tunicate:
                    return CritterBehaviour.Burrow;
                default:
                    return CritterBehaviour.Dart;
            }
        }

        /// <summary>Taille de la bestiole (1 = dessin de base) : de 0,8 pour une commune à 1,6 pour une légendaire.</summary>
        public static float Scale(Rarity rarity) => 0.8f + 0.2f * (int)rarity;

        /// <summary>Méfiance : une rare s'inquiète plus vite qu'une commune.</summary>
        public static float Sensitivity(Rarity rarity) => 1f + 0.25f * (int)rarity;

        /// <summary>
        /// Variation de l'inquiétude (0 = calme, 1 = elle réagit) pendant <paramref name="dt"/> : monte si l'épuisette va vite
        /// et près, redescend doucement sinon. Celles qui s'enfouissent s'inquiètent moins vite.
        /// </summary>
        public static float AlarmDelta(float netSpeed, float distance, float sensitivity, CritterBehaviour behaviour, float dt)
        {
            float near = Mathf.Max(0f, 1f - distance / WarnDistance);
            if (netSpeed <= CalmSpeed || near <= 0f)
            {
                return -0.36f * dt;
            }

            float rate = netSpeed / 120f * 3f * near * sensitivity * (behaviour == CritterBehaviour.Burrow ? 0.6f : 1f);
            return rate * dt;
        }

        /// <summary>L'éclaboussure d'un coup d'épuisette : une bestiole déjà inquiète file, une calme sursaute un peu.</summary>
        public static float SplashAlarm(float alarm, float distance)
        {
            if (distance >= SplashDistance) return alarm;
            return alarm > 0.5f ? 1f : alarm + 0.25f;
        }

        /// <summary>Prise : sous le cercle quand il se referme, et pas en train de fuir.</summary>
        public static bool Caught(float distance, float alarm, float scale) => distance < NetRadius + 3f * scale && alarm < 1f;

        /// <summary>Le cercle suit la souris avec du retard (l'eau le freine).</summary>
        public static Vector2 FollowPointer(Vector2 net, Vector2 pointer, float dt) => net + (pointer - net) * (1f - Mathf.Exp(-Drag * dt));
    }
}
