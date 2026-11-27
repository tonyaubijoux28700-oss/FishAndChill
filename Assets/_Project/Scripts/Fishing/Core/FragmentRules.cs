using System;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Fragments d'artefact : de temps en temps, une prise réussie dans la zone d'une légende n'est pas un poisson mais un
    /// fragment de son artefact. Plus aucun une fois tous les fragments trouvés. Logique pure, testée.
    /// </summary>
    public static class FragmentRules
    {
        /// <summary>Chance par défaut qu'une prise soit un fragment (≈ 130 prises pour un artefact de 4).</summary>
        public const float DefaultChance = 0.03f;

        public static float ChanceFor(float chance) => Math.Max(0f, chance);

        /// <summary>Cette prise est-elle un fragment ? Seulement s'il en reste à trouver (<paramref name="canFindMore"/>).</summary>
        public static bool ShouldDrop(Random random, float chance, bool canFindMore)
            => canFindMore && random.NextDouble() < ChanceFor(chance);
    }
}
