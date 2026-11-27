using UnityEngine;

namespace FishAndChill.Gear
{
    /// <summary>
    /// Les atouts d'une canne en pierres (1 à 5) pour le sélecteur de l'inventaire : zone de réussite, solidité face
    /// aux lourds (tension), portée du lancer, rapidité des touches.
    /// </summary>
    public static class GearStats
    {
        public const int Max = 5;

        public static readonly string[] RodNames = { "Zone", "Solidité", "Portée", "Touches" };

        public static int[] Rod(GearLevel rod)
        {
            if (rod == null) return new[] { 1, 1, 1, 1 };
            return new[]
            {
                Pips(1f + rod.Value / 0.35f * 4f),
                Pips(3f + (1f - rod.TensionFactor) * 3.4f),
                Pips(1f + rod.CastBonus * 4f / 3f),
                Pips(1f + rod.Value2 * 20f),
            };
        }

        private static int Pips(float value) => Mathf.Clamp(Mathf.RoundToInt(value), 1, Max);
    }
}
