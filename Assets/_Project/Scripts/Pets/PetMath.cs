using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.Pets
{
    /// <summary>Effets des animaux sur la pêche et la balade (logique pure, testée).</summary>
    public static class PetMath
    {
        /// <summary>Multiplicateur de chance d'une espèce : le bonus ne touche que les rares et plus.</summary>
        public static float RarityFactor(Rarity rarity, float rareMultiplier)
        {
            return rarity >= Rarity.Rare ? Mathf.Max(1f, rareMultiplier) : 1f;
        }

        /// <summary>
        /// Place de la prise dans la fourchette de taille (0 le plus petit, 1 le plus gros) à partir d'un tirage
        /// <paramref name="roll"/> entre 0 et 1 : avec <paramref name="sizeBias"/> au-dessus de 1 (crocodile), le tirage
        /// penche vers les gros. 1 : inchangé.
        /// </summary>
        public static float SizeRoll(float roll, float sizeBias)
        {
            float r = Mathf.Clamp01(roll);
            return 1f - Mathf.Pow(1f - r, Mathf.Max(1f, sizeBias));
        }

        /// <summary>Zone de réussite du QTE élargie par l'animal (araignée), sans dépasser 60 % de la barre.</summary>
        public static float ZoneWidth(float zoneWidth, float zoneFactor)
        {
            if (zoneWidth >= 0.6f) return zoneWidth;
            return Mathf.Min(0.6f, zoneWidth * Mathf.Max(1f, zoneFactor));
        }

        /// <summary>
        /// Pièces trouvées pendant <paramref name="seconds"/> de marche (0 la plupart du temps) : une chance par seconde,
        /// puis un nombre entre <paramref name="min"/> et <paramref name="max"/>. Rien à l'arrêt.
        /// </summary>
        public static int FoundCoins(System.Random random, float seconds, bool walking, float chancePerSecond, int min, int max)
        {
            if (!walking || seconds <= 0f || chancePerSecond <= 0f) return 0;
            if (random.NextDouble() >= chancePerSecond * seconds) return 0;
            return random.Next(Mathf.Min(min, max), Mathf.Max(min, max) + 1);
        }

        /// <summary>Durée d'une réaction de joie (prise, caresse).</summary>
        public const float ReactionSeconds = 1.2f;

        /// <summary>
        /// Réaction de joie, <paramref name="t"/> secondes après son début : décalage du dessin (en cases) et inclinaison
        /// (<paramref name="tilt"/>, en degrés). Le chat fait un grand bond, le chien frétille en sautillant, le perroquet
        /// s'envole un peu, le manchot se dandine, le crocodile se cabre, l'araignée trépigne. Rien hors de la réaction.
        /// </summary>
        public static Vector2 ReactionOffset(PetKind kind, float t, out float tilt)
        {
            tilt = 0f;
            if (t < 0f || t >= ReactionSeconds) return Vector2.zero;
            float fade = 1f - t / ReactionSeconds;
            switch (kind)
            {
                case PetKind.Cat:
                    // Un grand bond, puis un petit.
                    return new Vector2(0f, t < 0.55f ? 0.55f * Mathf.Sin(Mathf.PI * t / 0.55f) : 0.18f * Mathf.Abs(Mathf.Sin(Mathf.PI * (t - 0.55f) / 0.65f)));
                case PetKind.Dog:
                    tilt = 8f * Mathf.Sin(t * 30f) * fade;
                    return new Vector2(0.05f * Mathf.Sin(t * 40f) * fade, 0.16f * Mathf.Abs(Mathf.Sin(t * 9f)) * fade);
                case PetKind.Parrot:
                    return new Vector2(0.12f * Mathf.Sin(t * 5f), 0.45f * Mathf.Sin(Mathf.PI * t / ReactionSeconds));
                case PetKind.Penguin:
                    tilt = 14f * Mathf.Sin(t * 14f) * fade;
                    return new Vector2(0f, 0.12f * Mathf.Abs(Mathf.Sin(t * 7f)) * fade);
                case PetKind.Crocodile:
                    tilt = 12f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t / 0.6f));
                    return new Vector2(0f, 0.14f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t / 0.6f)));
                case PetKind.Spider:
                    return new Vector2(0.04f * Mathf.Sin(t * 60f) * fade, 0.2f * Mathf.Abs(Mathf.Sin(t * 8f)) * fade);
                default:
                    return new Vector2(0f, 0.3f * Mathf.Sin(Mathf.PI * t / ReactionSeconds));
            }
        }

        /// <summary>
        /// Une prise réjouit-elle assez l'animal pour qu'il montre un cœur ? Les rares et plus, une nouvelle espèce ou
        /// une légende.
        /// </summary>
        public static bool HeartFor(Rarity rarity, bool newSpecies) => newSpecies || rarity >= Rarity.Rare;

        /// <summary>
        /// Place de l'animal assis pendant la pêche : à côté du joueur, sur son flanc gauche (sinon droit), un peu en
        /// retrait, en regardant l'eau comme lui. <paramref name="canStand"/> dit si une place est sur la terre ferme ;
        /// aucune : derrière lui.
        /// </summary>
        public static Vector2 SitPoint(Vector2 player, Vector2 facing, System.Func<Vector2, bool> canStand)
        {
            Vector2 f = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.down;
            Vector2 side = new Vector2(-f.y, f.x);
            foreach (Vector2 candidate in new[] { player + side * 0.9f - f * 0.15f, player - side * 0.9f - f * 0.15f })
            {
                if (canStand == null || canStand(candidate)) return candidate;
            }

            return player - f * 0.9f;
        }

        /// <summary>
        /// Le joueur peut-il caresser l'animal ? Assez près (1,5 case) et tourné vers lui.
        /// </summary>
        public static bool CanPet(Vector2 player, Vector2 facing, Vector2 pet)
        {
            Vector2 to = pet - player;
            float d = to.magnitude;
            if (d > 1.5f) return false;
            if (d < 0.2f) return true;
            return Vector2.Dot(facing.normalized, to / d) > 0.55f;
        }

        /// <summary>
        /// Point de suivi : derrière le joueur, à <paramref name="distance"/> le long de son chemin récent
        /// (<paramref name="trail"/>, du plus récent au plus ancien). Pas assez de chemin : le dernier point connu.
        /// </summary>
        public static Vector2 FollowPoint(System.Collections.Generic.IReadOnlyList<Vector2> trail, float distance)
        {
            if (trail == null || trail.Count == 0) return Vector2.zero;
            float left = distance;
            for (int i = 1; i < trail.Count; i++)
            {
                float step = Vector2.Distance(trail[i - 1], trail[i]);
                if (step >= left && step > 0f) return Vector2.Lerp(trail[i - 1], trail[i], left / step);
                left -= step;
            }

            return trail[trail.Count - 1];
        }
    }
}
