using FishAndChill.Fishing;
using FishAndChill.World;
using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Un poisson de légende (boss) : un individu unique et énorme d'une vraie espèce, qui vit dans une zone. On pêche
    /// les fragments de son artefact, la scientifique restaure l'artefact complet et invoque la légende.
    /// Une légende = un asset, jamais du code.
    /// </summary>
    [CreateAssetMenu(menuName = "FishAndChill/Poisson de légende", fileName = "NewLegendFish")]
    public sealed class LegendFish : ScriptableObject
    {
        [SerializeField, Tooltip("Identifiant unique et stable (sauvegarde). Ne pas changer une fois publié.")]
        private string _id;

        [SerializeField, Tooltip("Nom de la légende, ex. « Le Vieux Ulua ».")]
        private string _title;

        [SerializeField, Tooltip("L'espèce réelle dont la légende est un individu géant.")]
        private FishSpecies _species;

        [SerializeField, Tooltip("Zone où vit la légende : ses fragments ne se pêchent que là.")]
        private FishingZone _zone;

        [SerializeField, Min(1f), Tooltip("Taille de la légende, en centimètres.")]
        private float _sizeCm = 200f;

        [Header("Artefact")]
        [SerializeField, Tooltip("Nom de l'artefact, ex. « Idole de corail ».")]
        private string _artifactName;

        [SerializeField, Min(1)] private int _fragmentCount = 4;

        [SerializeField, Range(0f, 1f), Tooltip("Chance qu'une prise réussie dans la zone soit un fragment (doublée dans un coin de pêche).")]
        private float _fragmentChance = FragmentRules.DefaultChance;

        [SerializeField, Tooltip("L'artefact entier.")]
        private Sprite _artifact;

        [SerializeField, Tooltip("Chaque fragment, dans le même cadre que l'artefact entier (il s'y superpose à sa place).")]
        private Sprite[] _fragments = new Sprite[0];

        [SerializeField, Tooltip("Centre de chaque fragment dans le cadre de l'artefact (0 à 1).")]
        private Vector2[] _fragmentCenters = new Vector2[0];

        [SerializeField, Tooltip("L'artefact en silhouette (fragments manquants).")]
        private Sprite _silhouette;

        [SerializeField, TextArea, Tooltip("Une phrase par fragment, affichée quand on le pêche.")]
        private string[] _fragmentLines = new string[0];

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public string Title => string.IsNullOrEmpty(_title) ? name : _title;
        public FishSpecies Species => _species;
        public FishingZone Zone => _zone;
        public float SizeCm => _sizeCm;
        public string ArtifactName => string.IsNullOrEmpty(_artifactName) ? "artefact" : _artifactName;
        public int FragmentCount => Mathf.Max(1, _fragmentCount);
        public float FragmentChance => _fragmentChance;
        public Sprite Artifact => _artifact;
        public Sprite Silhouette => _silhouette;

        /// <summary>Dessin du fragment numéro <paramref name="index"/> (à partir de 0), ou null.</summary>
        public Sprite Fragment(int index) => _fragments != null && index >= 0 && index < _fragments.Length ? _fragments[index] : null;

        /// <summary>Centre du fragment numéro <paramref name="index"/> dans le cadre de l'artefact (0 à 1).</summary>
        public Vector2 FragmentCenter(int index) => _fragmentCenters != null && index >= 0 && index < _fragmentCenters.Length ? _fragmentCenters[index] : new Vector2(0.5f, 0.5f);

        /// <summary>La phrase du fragment numéro <paramref name="index"/> (à partir de 0).</summary>
        public string FragmentLine(int index) => _fragmentLines != null && index >= 0 && index < _fragmentLines.Length ? _fragmentLines[index] : string.Empty;

#if UNITY_EDITOR
        /// <summary>Remplit la fiche (menu de génération).</summary>
        public void Configure(string id, string title, FishSpecies species, FishingZone zone, float sizeCm, string artifactName,
            int fragmentCount, float fragmentChance, Sprite artifact, Sprite[] fragments, Vector2[] fragmentCenters, Sprite silhouette, string[] fragmentLines)
        {
            _fragmentCenters = fragmentCenters;
            _id = id;
            _title = title;
            _species = species;
            _zone = zone;
            _sizeCm = sizeCm;
            _artifactName = artifactName;
            _fragmentCount = fragmentCount;
            _fragmentChance = fragmentChance;
            _artifact = artifact;
            _fragments = fragments;
            _silhouette = silhouette;
            _fragmentLines = fragmentLines;
        }
#endif
    }
}
