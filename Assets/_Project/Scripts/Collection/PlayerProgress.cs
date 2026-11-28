using System;
using FishAndChill.Gear;
using FishAndChill.Pets;
using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>La progression du joueur pendant la partie : sac, aquarium, pièces, matériel et animaux.</summary>
    public sealed class PlayerProgress : MonoBehaviour
    {
        [SerializeField, Min(1), Tooltip("Places dans le sac au début du jeu.")]
        private int _startingBagCapacity = 3;

        [SerializeField, Tooltip("Le matériel vendu par le marchand (canne, hameçon, appât, sac).")]
        private GearCatalog _gearCatalog;

        [SerializeField, Tooltip("Les appâts vendus par le marchand (le premier est celui du départ, gratuit et illimité).")]
        private BaitCatalog _baitCatalog;

        [SerializeField, Tooltip("Les animaux de compagnie vendus à l'animalerie.")]
        private PetCatalog _petCatalog;

        public event Action Changed;

        public Inventory Bag { get; private set; }
        public AquariumCollection Aquarium { get; private set; }
        public Wallet Wallet { get; private set; }
        public GearLoadout Gear { get; private set; }
        public GearCatalog GearCatalog => _gearCatalog;
        public BaitCatalog BaitCatalog => _baitCatalog;
        public BaitBag Baits { get; private set; }

        /// <summary>Animaux de compagnie achetés, et celui qui accompagne le joueur.</summary>
        public PetCollection Pets { get; private set; }

        public PetCatalog PetCatalog => _petCatalog;

        /// <summary>A-t-on une épuisette (pour la pêche à pied dans les coins à bulles) ?</summary>
        public bool HasNet { get; private set; }

        /// <summary>Chiyo, la vieille ama d'Izu, donne son épuisette (une seule, pour toujours) ; faux si on l'a déjà.</summary>
        public bool ReceiveNet()
        {
            if (HasNet) return false;
            HasNet = true;
            RaiseChanged();
            return true;
        }

        /// <summary>Le collier de Chiyo : une perle par zone dont toutes les bestioles sont au carnet.</summary>
        public PearlNecklace Pearls { get; } = new PearlNecklace();

        /// <summary>Jour de la dernière prise dans la flaque de la grotte (-1 : jamais) : une prise par jour.</summary>
        public int CaveDay { get; set; } = -1;

        /// <summary>Chargement d'une partie : l'épuisette reçue ou non.</summary>
        public void RestoreNet(bool owned)
        {
            HasNet = owned;
            RaiseChanged();
        }

        /// <summary>L'animal qui accompagne le joueur (null : aucun).</summary>
        public PetDefinition EquippedPet
        {
            get
            {
                EnsureCreated();
                return _petCatalog != null ? _petCatalog.Find(Pets.Equipped) : null;
            }
        }

        /// <summary>Bonus du marchand donné par l'animal qui accompagne le joueur (manchot), 1 sans bonus.</summary>
        public float SellFactor
        {
            get
            {
                PetDefinition pet = EquippedPet;
                return pet != null ? Mathf.Max(1f, pet.SellFactor) : 1f;
            }
        }

        /// <summary>Poissons de légende : fragments d'artefact pêchés et étape de chacun.</summary>
        public LegendProgress Legends { get; private set; }

        /// <summary>Appât accroché à la ligne (l'appât de départ si rien d'autre).</summary>
        public Bait EquippedBait
        {
            get
            {
                EnsureCreated();
                Bait bait = _baitCatalog != null ? _baitCatalog.Find(Baits.Equipped) : null;
                return bait != null ? bait : _baitCatalog != null ? _baitCatalog.Default : null;
            }
        }

        /// <summary>Ce que le matériel actuel change à la pêche.</summary>
        public GearEffects GearEffects => GearEffects.From(_gearCatalog, Gear);

        private void Awake()
        {
            EnsureCreated();
        }

        /// <summary>Crée les données si besoin (utile si un autre composant y accède avant Awake).</summary>
        public void EnsureCreated()
        {
            if (Bag != null)
            {
                return;
            }

            Bag = new Inventory(_startingBagCapacity);
            Aquarium = new AquariumCollection();
            Wallet = new Wallet();
            Gear = new GearLoadout();
            Baits = new BaitBag();
            Pets = new PetCollection();
            Pets.Changed += RaiseChanged;
            Legends = new LegendProgress();
            Legends.Changed += RaiseChanged;
            if (_baitCatalog != null && _baitCatalog.Default != null)
            {
                Baits.Add(_baitCatalog.Default.Id, 0); // l'appât de départ, toujours là
            }
            Bag.Changed += RaiseChanged;
            Aquarium.Changed += RaiseChanged;
            Wallet.Changed += RaiseChanged;
            Gear.Changed += RaiseChanged;
            Baits.Changed += RaiseChanged;
        }

        public DepositResult DepositBag() => Aquarium.Deposit(Bag);

        /// <summary>
        /// Achète le niveau suivant de cette famille de matériel. Un nouveau sac agrandit tout de suite le sac.
        /// </summary>
        public GearPurchase BuyGear(GearCategory category)
        {
            EnsureCreated();
            GearLine line = _gearCatalog != null ? _gearCatalog.Line(category) : null;
            GearPurchase result = GearShop.TryBuy(line, Gear, Wallet);
            if (result == GearPurchase.Bought && category == GearCategory.Bag)
            {
                Bag.UpgradeCapacity(GearEffects.BagCapacity);
            }

            return result;
        }

        /// <summary>Achète (ou, s'il est déjà à soi, équipe) l'objet <paramref name="index"/> de cette famille (canne, hameçon).</summary>
        public GearPurchase BuyGearItem(GearCategory category, int index)
        {
            EnsureCreated();
            GearLine line = _gearCatalog != null ? _gearCatalog.Line(category) : null;
            return GearShop.TryBuyItem(line, index, Gear, Wallet);
        }

        /// <summary>Équipe l'objet suivant qu'on possède dans cette famille ; faux s'il n'y en a qu'un.</summary>
        public bool CycleGear(GearCategory category)
        {
            EnsureCreated();
            GearLine line = _gearCatalog != null ? _gearCatalog.Line(category) : null;
            if (line == null)
            {
                return false;
            }

            int before = Gear.Level(category);
            Gear.Cycle(category, line.Levels.Count);
            return Gear.Level(category) != before;
        }

        /// <summary>Nombre d'objets possédés dans cette famille.</summary>
        public int OwnedCount(GearCategory category)
        {
            GearLine line = _gearCatalog != null ? _gearCatalog.Line(category) : null;
            int count = 0;
            for (int i = 0; line != null && i < line.Levels.Count; i++)
            {
                if (Gear.Owns(category, i)) count++;
            }

            return count;
        }

        /// <summary>Achète un paquet de cet appât (il est accroché tout de suite).</summary>
        public bool BuyBait(Bait bait)
        {
            EnsureCreated();
            if (bait == null || !Wallet.TrySpend(bait.Price))
            {
                return false;
            }

            Baits.Add(bait.Id, bait.Uses);
            Baits.Equip(bait.Id);
            return true;
        }

        /// <summary>Après un chargement : l'appât de départ est toujours possédé.</summary>
        public void EnsureDefaultBait()
        {
            EnsureCreated();
            if (_baitCatalog != null && _baitCatalog.Default != null && !Baits.Owns(_baitCatalog.Default.Id))
            {
                Baits.Add(_baitCatalog.Default.Id, 0);
            }
        }

        private readonly System.Collections.Generic.HashSet<string> _claimedRewards = new System.Collections.Generic.HashSet<string>();

        /// <summary>Récompenses de collection déjà encaissées (clés « zone:palier », voir <see cref="CollectionRewards"/>).</summary>
        public System.Collections.Generic.ICollection<string> ClaimedRewards => _claimedRewards;

        /// <summary>Encaisse une récompense de collection (une seule fois) : ajoute les pièces.</summary>
        public bool ClaimReward(string key, int coins)
        {
            EnsureCreated();
            if (string.IsNullOrEmpty(key) || !_claimedRewards.Add(key))
            {
                return false;
            }

            Wallet.Add(coins); // déclenche Changed : la sauvegarde suit
            return true;
        }

        /// <summary>Remet les récompenses encaissées (chargement d'une sauvegarde), sans redonner les pièces.</summary>
        public void RestoreClaimedRewards(System.Collections.Generic.IEnumerable<string> keys)
        {
            _claimedRewards.Clear();
            if (keys != null)
            {
                foreach (string key in keys)
                {
                    _claimedRewards.Add(key);
                }
            }
        }

        private void RaiseChanged() => Changed?.Invoke();
    }
}
