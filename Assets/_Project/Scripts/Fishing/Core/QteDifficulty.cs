using System;
using UnityEngine;

namespace FishAndChill.Fishing
{
    /// <summary>Réglages du QTE d'un poisson, modifiables dans l'inspecteur.</summary>
    [Serializable]
    public sealed class QteDifficulty
    {
        [SerializeField, Min(0.1f), Tooltip("Largeurs de barre parcourues par seconde.")]
        private float _cursorSpeed = 1f;

        [SerializeField, Range(0.05f, 0.6f), Tooltip("Largeur de la zone de réussite (fraction de la barre).")]
        private float _zoneWidth = 0.25f;

        [SerializeField, Min(1), Tooltip("Nombre de réussites pour attraper le poisson.")]
        private int _requiredHits = 3;

        [SerializeField, Min(0), Tooltip("Nombre de ratés tolérés avant que le poisson s'échappe.")]
        private int _maxMisses = 2;

        public QteDifficulty()
        {
        }

        public QteDifficulty(float cursorSpeed, float zoneWidth, int requiredHits, int maxMisses)
        {
            _cursorSpeed = cursorSpeed;
            _zoneWidth = zoneWidth;
            _requiredHits = requiredHits;
            _maxMisses = maxMisses;
        }

        public float CursorSpeed => _cursorSpeed;
        public float ZoneWidth => _zoneWidth;
        public int RequiredHits => Mathf.Max(1, _requiredHits);
        public int MaxMisses => Mathf.Max(0, _maxMisses);
    }
}
