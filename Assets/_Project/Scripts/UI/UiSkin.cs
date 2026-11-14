using UnityEngine;

namespace FishAndChill.UI
{
    /// <summary>Charte graphique de l'interface : les sprites pixel art partagés par toutes les fenêtres.</summary>
    [CreateAssetMenu(menuName = "FishAndChill/Charte d'interface", fileName = "UiSkin")]
    public sealed class UiSkin : ScriptableObject
    {
        [SerializeField, Tooltip("Cadre de fenêtre doré à coins arrondis (9 parties).")]
        private Sprite _panel;

        [SerializeField, Tooltip("Case creusée (9 parties).")]
        private Sprite _slot;

        [SerializeField, Tooltip("Bandeau de titre doré (9 parties).")]
        private Sprite _plaque;

        [SerializeField, Tooltip("Fond d'eau pour les dessins de poissons (9 parties).")]
        private Sprite _waterBackdrop;

        [SerializeField, Tooltip("Pierre de rareté (teintée).")]
        private Sprite _gem;

        [SerializeField, Tooltip("Halo doux (teinté selon la rareté).")]
        private Sprite _glow;

        public Sprite Panel => _panel;
        public Sprite Slot => _slot;
        public Sprite Plaque => _plaque;
        public Sprite WaterBackdrop => _waterBackdrop;
        public Sprite Gem => _gem;
        public Sprite Glow => _glow;

        /// <summary>Couleur du texte posé sur un bandeau doré.</summary>
        public static readonly Color PlaqueText = new Color(0.24f, 0.13f, 0.04f);
    }
}
