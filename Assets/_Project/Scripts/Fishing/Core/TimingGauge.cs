using System;
using UnityEngine;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Jauge de timing : un curseur fait des allers-retours entre 0 et 1,
    /// et le joueur doit appuyer quand il se trouve dans la zone de réussite.
    /// Logique pure (aucun MonoBehaviour), donc testable en EditMode.
    /// </summary>
    public sealed class TimingGauge
    {
        private int _direction = 1;

        /// <param name="speed">Distance parcourue par seconde, en largeurs de barre (1 = un trajet complet par seconde).</param>
        /// <param name="zoneWidth">Largeur de la zone de réussite, entre 0 et 1.</param>
        /// <param name="zoneCenter">Centre de la zone ; ramené dans la barre si besoin.</param>
        /// <param name="startPosition">Position de départ du curseur, entre 0 et 1.</param>
        public TimingGauge(float speed, float zoneWidth, float zoneCenter = 0.5f, float startPosition = 0f)
        {
            if (speed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), "La vitesse doit être positive.");
            }

            Speed = speed;
            ZoneWidth = Mathf.Clamp(zoneWidth, 0.01f, 1f);
            ZoneCenter = ClampCenter(zoneCenter);
            Position = Mathf.Clamp01(startPosition);
        }

        public float Position { get; private set; }
        public float Speed { get; }
        public float ZoneWidth { get; }
        public float ZoneCenter { get; private set; }

        public float ZoneMin => ZoneCenter - ZoneWidth * 0.5f;
        public float ZoneMax => ZoneCenter + ZoneWidth * 0.5f;
        public bool IsInZone => Position >= ZoneMin && Position <= ZoneMax;

        /// <summary>Fait avancer le curseur, avec rebond aux extrémités.</summary>
        public void Tick(float deltaTime)
        {
            float remaining = Speed * Mathf.Max(0f, deltaTime);
            while (remaining > 0f)
            {
                float target = _direction > 0 ? 1f : 0f;
                float distance = Mathf.Abs(target - Position);
                if (remaining < distance)
                {
                    Position += _direction * remaining;
                    remaining = 0f;
                }
                else
                {
                    Position = target;
                    remaining -= distance;
                    _direction = -_direction;
                }
            }
        }

        /// <summary>Déplace la zone de réussite (elle reste entièrement dans la barre).</summary>
        public void MoveZone(float center)
        {
            ZoneCenter = ClampCenter(center);
        }

        private float ClampCenter(float center)
        {
            float half = ZoneWidth * 0.5f;
            return Mathf.Clamp(center, half, 1f - half);
        }
    }
}
