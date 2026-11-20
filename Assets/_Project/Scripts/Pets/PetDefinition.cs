using UnityEngine;

namespace FishAndChill.Pets
{
    /// <summary>
    /// Un animal de compagnie vendu à l'animalerie : nom, prix, bonus et images (3 directions × 3 images : 0 à l'arrêt,
    /// 1 et 2 en marche ; la gauche est le miroir du profil). Généré par <c>Editor/PetSetup</c> dans <c>Data/Animaux/</c>.
    /// Bonus : <see cref="BiteWaitFactor"/> (moins de 1 : les poissons mordent plus vite), <see cref="RareMultiplier"/>
    /// (chances des espèces rares et plus), pièces trouvées en se promenant (<see cref="CoinChancePerSecond"/>), prix du
    /// marchand (<see cref="SellFactor"/>), poissons plus gros (<see cref="SizeBias"/>), zone du QTE élargie (<see cref="ZoneFactor"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "FishAndChill/Animal de compagnie", fileName = "Animal")]
    public sealed class PetDefinition : ScriptableObject
    {
        [SerializeField] private string _id = "animal";
        [SerializeField] private string _displayName = "Animal";
        [SerializeField] private PetKind _kind;
        [SerializeField, Min(0)] private int _price = 300;
        [SerializeField, TextArea] private string _bonusText = string.Empty;

        [Header("Bonus")]
        [SerializeField, Range(0.3f, 1f)] private float _biteWaitFactor = 1f;
        [SerializeField, Range(1f, 3f)] private float _rareMultiplier = 1f;
        [SerializeField, Range(0f, 0.2f), Tooltip("Chance par seconde de marche de trouver des pièces.")] private float _coinChancePerSecond;
        [SerializeField, Min(0)] private int _coinMin = 5;
        [SerializeField, Min(0)] private int _coinMax = 15;
        [SerializeField, Range(1f, 2f), Tooltip("Le marchand paie les poissons plus cher.")] private float _sellFactor = 1f;
        [SerializeField, Range(1f, 3f), Tooltip("Plus de 1 : les poissons attrapés sont plus souvent gros.")] private float _sizeBias = 1f;
        [SerializeField, Range(1f, 2f), Tooltip("Zone de réussite du QTE élargie.")] private float _zoneFactor = 1f;

        [Header("Images")]
        [SerializeField] private Sprite[] _down = new Sprite[3];
        [SerializeField] private Sprite[] _up = new Sprite[3];
        [SerializeField] private Sprite[] _side = new Sprite[3];
        [SerializeField, Tooltip("Vole à hauteur d'épaule au lieu de marcher au sol.")] private bool _flies;

        public string Id => _id;
        public string DisplayName => _displayName;
        public PetKind Kind => _kind;
        public int Price => _price;
        public string BonusText => _bonusText;
        public float BiteWaitFactor => _biteWaitFactor;
        public float RareMultiplier => _rareMultiplier;
        public float CoinChancePerSecond => _coinChancePerSecond;
        public int CoinMin => _coinMin;
        public int CoinMax => _coinMax;
        public float SellFactor => _sellFactor;
        public float SizeBias => _sizeBias;
        public float ZoneFactor => _zoneFactor;
        public Sprite[] Down => _down;
        public Sprite[] Up => _up;
        public Sprite[] Side => _side;
        public bool Flies => _flies;
        public Sprite Portrait => _down != null && _down.Length > 0 ? _down[0] : null;
    }
}
