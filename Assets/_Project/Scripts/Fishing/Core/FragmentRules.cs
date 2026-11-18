using System;
using FishAndChill.Collection;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Fragments d'artefact : de temps en temps, une prise réussie dans la zone d'une légende n'est pas un poisson mais un
    /// fragment de son artefact (plus souvent dans un coin de pêche). Plus aucun fragment une fois l'artefact complet.
    /// Logique pure, testée.
    /// </summary>
    public static class FragmentRules
    {
        /// <summary>Chance par défaut qu'une prise soit un fragment (≈ 130 prises pour un artefact de 4).</summary>
        public const float DefaultChance = 0.03f;

        /// <summary>Multiplicateur de chance quand le bouchon est dans un coin de pêche.</summary>
        public const float SpotBonus = 2f;

        public static float ChanceFor(float chance, bool inSpot) => Math.Max(0f, chance) * (inSpot ? SpotBonus : 1f);

        /// <summary>Cette prise est-elle un fragment ? Seulement tant que la légende en est à chercher ses fragments.</summary>
        public static bool ShouldDrop(Random random, float chance, bool inSpot, LegendState state)
            => state == LegendState.Collecting && random.NextDouble() < ChanceFor(chance, inSpot);
    }
}
