using System;

namespace FishAndChill.Gear
{
    /// <summary>Un casier posé dans l'eau : sa zone, sa position et l'heure de jeu (totale) où il a été posé.</summary>
    [Serializable]
    public sealed class PlacedTrap
    {
        public string ZoneId;
        public float X;
        public float Y;
        public double PlacedAtHours;
    }
}
