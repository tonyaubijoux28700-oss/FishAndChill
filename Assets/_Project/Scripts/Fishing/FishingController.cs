using System;
using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Gear;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;

namespace FishAndChill.Fishing
{
    public enum FishingState
    {
        Idle,

        /// <summary>Le bouchon vole vers l'eau (lancer).</summary>
        Casting,

        Waiting,
        Bite,
        Reeling,
        Result,

        /// <summary>Poisson attrapé alors que le sac est plein : le joueur choisit quoi relâcher.</summary>
        BagFull,
    }

    /// <summary>
    /// Déroulé d'une partie de pêche : lancer → attente → touche → QTE → résultat.
    /// Ne gère aucun affichage : la vue écoute ses propriétés et ses événements.
    /// </summary>
    public sealed class FishingController : MonoBehaviour
    {
        private const string IdleMessage = "ZQSD pour te déplacer — Espace pour lancer, face à l'eau";

        [SerializeField] private FishingZone _zone;

        [SerializeField, Tooltip("Horloge : certaines espèces ne mordent que le jour ou la nuit.")]
        private DayNightCycle _cycle;

        [SerializeField, Tooltip("Coins de pêche de chaque zone (bancs, bulles, chasses).")]
        private FishingSpots[] _spots = new FishingSpots[0];

        /// <summary>Le bouchon est-il dans un coin de pêche, et lequel ?</summary>
        public bool InSpot { get; private set; }
        private string _baitNotice;
        public SpotKind SpotKind { get; private set; }

        [SerializeField, Tooltip("Sac, aquarium et pièces du joueur. Sans lui, les prises ne sont pas conservées.")]
        private PlayerProgress _progress;

        [Header("Lancer")]
        [SerializeField, Tooltip("Le pêcheur : le lancer part de sa position, dans la direction où il regarde.")]
        private PlayerMovement _angler;

        [SerializeField, Tooltip("Carte : le bouchon doit tomber dans l'eau.")]
        private WorldMap _map;

        [SerializeField, Tooltip("Distance du lancer (min, max).")]
        private Vector2 _castDistance = new Vector2(2.5f, 4.5f);

        [SerializeField, Tooltip("Si le point visé est sur la terre, on cherche de l'eau jusqu'à cette distance.")]
        private float _castMaxDistance = 6f;

        [SerializeField, Min(0f), Tooltip("Imprécision latérale maximale du lancer.")]
        private float _castSpread = 1.2f;

        [Header("Attente de la touche (secondes)")]
        [SerializeField, Min(0f)] private float _minWaitSeconds = 1.5f;
        [SerializeField, Min(0f)] private float _maxWaitSeconds = 4f;

        [SerializeField, Min(0.1f), Tooltip("Durée du vol du bouchon pour un lancer de 4 cases (proportionnelle à la distance).")]
        private float _castFlightSeconds = 0.7f;

        [Header("Réactivité")]
        [SerializeField, Min(0.1f), Tooltip("Temps pour ferrer une fois que ça mord.")]
        private float _biteWindowSeconds = 1f;

        [SerializeField, Min(0.5f)] private float _resultSeconds = 2.5f;

        [SerializeField, Min(0.5f), Tooltip("Durée d'affichage de la carte de prise (Espace pour passer).")]
        private float _catchResultSeconds = 6f;

        private System.Random _random;
        private float _timer;
        private float _phaseDuration;
        private float _biteTime;
        private float _waited;
        private float[] _nibbles = new float[0];

        public event Action StateChanged;
        public event Action<QtePressResult> QtePressed;

        /// <summary>Le poisson titille l'appât (le bouchon tressaille) : numéro du titillement, à partir de 1.</summary>
        public event Action<int> Nibbled;
        /// <summary>Un poisson est attrapé : (poisson, nouvelle espèce ?, sac plein ?).</summary>
        public event Action<CaughtFish, bool, bool> FishCaught;

        /// <summary>Ancien record battu par la dernière prise (0 = pas de nouveau record).</summary>
        public float LastBeatenRecord { get; private set; }

        /// <summary>Une bouteille à la mer a été pêchée à la place d'un poisson : son message et les pièces trouvées dedans.</summary>
        public event Action<string, int> BottleFound;

        [SerializeField, Range(0f, 1f), Tooltip("Chance qu'une prise réussie soit une bouteille à la mer.")]
        private float _bottleChance = BottleMessage.Chance;

        [SerializeField, Tooltip("Poissons de légende : dans leur zone, une prise est parfois un fragment de leur artefact.")]
        private LegendFish[] _legends = new LegendFish[0];

        /// <summary>Un fragment d'artefact a été pêché à la place d'un poisson : (légende, fragments maintenant).</summary>
        public event Action<LegendFish, int> FragmentFound;

        public IReadOnlyList<LegendFish> Legends => _legends;

        /// <summary>La légende qui vit dans la zone actuelle (null s'il n'y en a pas).</summary>
        public LegendFish LegendOfZone
        {
            get
            {
                foreach (LegendFish legend in _legends)
                {
                    if (legend != null && legend.Zone == _zone) return legend;
                }

                return null;
            }
        }

        public FishingState State { get; private set; } = FishingState.Idle;
        /// <summary>Horloge du jeu (null sans cycle jour/nuit).</summary>
        public DayNightCycle Cycle => _cycle;

        public FishingZone Zone => _zone;

        /// <summary>Le joueur a changé de zone (voyage) : les espèces qui mordent ne sont plus les mêmes.</summary>
        public event Action ZoneChanged;

        /// <summary>Change la zone de pêche (après un voyage).</summary>
        public void SetZone(FishingZone zone)
        {
            _zone = zone;
            ZoneChanged?.Invoke();
        }

        /// <summary>Point où le bouchon est tombé (valide pendant la pêche).</summary>
        public Vector2 CastPoint { get; private set; }

        /// <summary>Titillements déjà faits pendant l'attente en cours.</summary>
        public int NibbleCount { get; private set; }
        public FishSpecies HookedSpecies { get; private set; }
        public float HookedSizeCm { get; private set; }
        public QteChallenge Challenge { get; private set; }
        public CatchLog CatchLog { get; } = new CatchLog();
        public PlayerProgress Progress => _progress;

        /// <summary>Le poisson qui attend une décision quand le sac est plein.</summary>
        public CaughtFish PendingFish { get; private set; }
        public string Message { get; private set; } = IdleMessage;

        /// <summary>Avancement de la phase en cours, de 0 à 1 (attente, fenêtre de touche, résultat).</summary>
        public float PhaseProgress => _phaseDuration > 0f ? Mathf.Clamp01(1f - _timer / _phaseDuration) : 0f;

        private void Awake()
        {
            _random = new System.Random();
            if (_progress != null)
            {
                _progress.EnsureCreated();
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);

            // Menu ouvert (boutique…) : la pêche ne réagit pas aux touches.
            if (_angler != null && _angler.Locked)
            {
                return;
            }
            if (State == FishingState.BagFull)
            {
                int slot = FishingInput.SlotPressedThisFrame();
                if (slot >= 0)
                {
                    ChooseReplace(slot);
                    return;
                }
            }

            if (FishingInput.ActionPressedThisFrame())
            {
                PressAction();
            }
        }

        /// <summary>Sac plein : relâche le poisson de la place <paramref name="slot"/> et garde le nouveau.</summary>
        public void ChooseReplace(int slot)
        {
            if (State != FishingState.BagFull || _progress == null || slot < 0 || slot >= _progress.Bag.Count)
            {
                return;
            }

            CaughtFish released = _progress.Bag.Replace(slot, PendingFish);
            string kept = PendingFish.Species.CommonName;
            PendingFish = null;
            Finish($"Tu relâches ton {released.Species.CommonName} ({released.SizeCm:0} cm) et gardes le {kept}.");
        }

        /// <summary>Sac plein : relâche le poisson qui vient d'être attrapé.</summary>
        public void ChooseRelease()
        {
            if (State != FishingState.BagFull)
            {
                return;
            }

            string name = PendingFish.Species.CommonName;
            PendingFish = null;
            Finish($"Tu relâches le {name}. Bon vent !");
        }

        /// <summary>Fait avancer le temps. Public pour pouvoir piloter le prototype hors clavier.</summary>
        public void Tick(float deltaTime)
        {
            switch (State)
            {
                case FishingState.Casting:
                    if (CountDown(deltaTime))
                    {
                        // Le bouchon touche l'eau : l'attente commence, avec ses titillements.
                        _waited = 0f;
                        NibbleCount = 0;
                        StartPhase(FishingState.Waiting, _biteTime, (InSpot ? FishingSpotRules.Message(SpotKind) : "Patience… attends que ça morde vraiment.")
                            + (_baitNotice != null ? "\n" + _baitNotice : string.Empty));
                    }

                    break;

                case FishingState.Waiting:
                    _waited += deltaTime;
                    while (NibbleCount < _nibbles.Length && _waited >= _nibbles[NibbleCount])
                    {
                        NibbleCount++;
                        Message = "Ça titille… pas encore !";
                        Nibbled?.Invoke(NibbleCount);
                    }

                    if (CountDown(deltaTime))
                    {
                        StartPhase(FishingState.Bite, _biteWindowSeconds, "Ça mord ! Le bouchon coule, ferre vite !");
                    }

                    break;

                case FishingState.Bite:
                    if (CountDown(deltaTime))
                    {
                        Finish($"Trop lent… {HookedSpecies.CommonName.ToLowerInvariant()} a lâché l'appât.");
                    }

                    break;

                case FishingState.Reeling:
                    Challenge.Tick(deltaTime);
                    if (Challenge.Status == QteStatus.Failed)
                    {
                        Lose();
                    }

                    break;

                case FishingState.Result:
                    if (CountDown(deltaTime))
                    {
                        StartPhase(FishingState.Idle, 0f, IdleMessage);
                    }

                    break;
            }
        }

        /// <summary>L'unique bouton d'action. Public pour pouvoir piloter le prototype hors clavier.</summary>
        public void PressAction()
        {
            switch (State)
            {
                case FishingState.Idle:
                    Cast();
                    break;

                case FishingState.Casting:
                    break; // le bouchon est en l'air : on attend qu'il se pose

                case FishingState.Waiting:
                    Finish(NibbleCount > 0
                        ? "Trop tôt ! Il ne faisait que titiller l'appât…"
                        : "Trop tôt ! Tu as remonté ta ligne à vide.");
                    break;

                case FishingState.Bite:
                    // Le matériel (canne, hameçon) rend le combat plus facile.
                    // La canne aide aussi à tenir la tension des gros poissons.
                    // Canne et hameçon : chacun a sa spécialité (tension, rushs, nerveux, fuyants).
                    GearEffects fightGear = GearEffects;
                    float tensionRate = fightGear.TensionFactor > 0f ? fightGear.TensionFactor : 1f;
                    Challenge = new QteChallenge(fightGear.Apply(HookedSpecies.Difficulty), _random, HookedSpecies.FightStyle, tensionRate,
                        fightGear.NervousFactor > 0f ? fightGear.NervousFactor : 1f, fightGear.RushFactor > 0f ? fightGear.RushFactor : 1f,
                        fightGear.DriftFactor > 0f ? fightGear.DriftFactor : 1f);
                    StartPhase(FishingState.Reeling, 0f, FightHint(HookedSpecies.FightStyle));
                    break;

                case FishingState.Reeling:
                    ResolveQtePress();
                    break;

                case FishingState.Result:
                    // Permet de passer l'écran de résultat, sans effacer un appui fait par réflexe.
                    if (_phaseDuration - _timer > 0.5f)
                    {
                        StartPhase(FishingState.Idle, 0f, IdleMessage);
                    }

                    break;

                case FishingState.BagFull:
                    ChooseRelease();
                    break;
            }
        }

        /// <summary>Peut-on lancer d'ici ? Non à l'intérieur de la boutique.</summary>
        /// <summary>Effets du matériel du joueur (aucun si la pêche n'est pas reliée à une partie).</summary>
        private GearEffects GearEffects => _progress != null ? _progress.GearEffects : GearEffects.None;

        /// <summary>Ce point est-il dans l'eau (selon la côte dessinée) ? Sert à garder le poisson hors du sable.</summary>
        public bool IsWater(Vector2 point) => _map == null || _map.IsWater(point);

        /// <summary>Pas de pêche à l'intérieur d'un bâtiment (boutique, aquarium).</summary>
        public bool CanFishHere => InteriorBuilding.Occupied == null;

        private void Cast()
        {
            // Dans la boutique, Espace ne fait rien : pas de lancer, pas de message.
            if (!CanFishHere)
            {
                return;
            }

            if (_zone == null)
            {
                Finish("Aucune zone de pêche n'est assignée au FishingController.");
                return;
            }

            if (!TryFindCastPoint(out Vector2 castPoint))
            {
                StartPhase(FishingState.Idle, 0f, "Pas d'eau devant toi : tourne-toi vers le rivage (ZQSD) puis relance.");
                return;
            }

            CastPoint = castPoint;
            FishingSpots spots = CurrentSpots;
            SpotKind kind = Fishing.SpotKind.School;
            InSpot = spots != null && spots.TryGetSpot(castPoint, out kind);
            SpotKind = kind;

            // L'appât attire plus souvent les espèces rares et fait mordre plus vite ; un coin de pêche aussi.
            GearEffects gear = GearEffects;
            float hour = _cycle != null ? _cycle.Hour : 12f;
            bool inSpot = InSpot;
            SpotKind spotKind = SpotKind;
            // L'appât accroché attire ses espèces favorites (et selon lui les rares, ou celles de nuit).
            Bait bait = _progress != null ? _progress.EquippedBait : null;
            bool night = hour >= 20f || hour < 6f;
            HookedSpecies = _zone.PickSpecies(_random, species =>
                DayCycleMath.IsActive(species.ActiveTime, hour)
                    ? gear.RarityMultiplier(species.Rarity) * (inSpot ? FishingSpotRules.SpeciesMultiplier(spotKind, species) : 1f)
                        * (bait != null ? bait.Multiplier(species, night) : 1f)
                    : 0f);
            if (HookedSpecies == null)
            {
                HookedSpecies = _zone.PickSpecies(_random, species => gear.RarityMultiplier(species.Rarity));
            }
            if (HookedSpecies == null)
            {
                Finish($"Rien à pêcher dans la zone « {_zone.DisplayName} ».");
                return;
            }

            Vector2 range = HookedSpecies.SizeRangeCm;
            HookedSizeCm = Mathf.Lerp(range.x, range.y, (float)_random.NextDouble());
            float wait = Mathf.Lerp(_minWaitSeconds, Mathf.Max(_minWaitSeconds, _maxWaitSeconds), (float)_random.NextDouble())
                * gear.BiteWaitFactor * (InSpot ? FishingSpotRules.WaitFactor(SpotKind) : 1f) * (bait != null ? bait.BiteWaitFactor : 1f);

            // Une utilisation de l'appât part à chaque lancer ; le dernier parti, on revient à l'appât de départ.
            _baitNotice = null;
            if (bait != null && !bait.Unlimited && _progress.BaitCatalog != null && _progress.BaitCatalog.Default != null
                && _progress.Baits.ConsumeEquipped(_progress.BaitCatalog.Default.Id))
            {
                _baitNotice = $"C'était ton dernier appât « {bait.DisplayName} » : retour à « {_progress.BaitCatalog.Default.DisplayName} ».";
            }

            // 1 à 3 titillements avant la vraie touche : l'attente s'allonge au besoin pour les espacer.
            _biteTime = Mathf.Max(wait, NibblePlanner.MinimumWait(1) + 0.2f);
            _nibbles = NibblePlanner.Plan(_random, _biteTime);

            // Le bouchon vole d'abord jusqu'à l'eau : plus le lancer est long, plus le vol dure.
            Vector2 origin = _angler != null ? (Vector2)_angler.transform.position : (Vector2)transform.position;
            float flight = _castFlightSeconds * Mathf.Clamp(Vector2.Distance(origin, castPoint) / 4f, 0.7f, 1.4f);
            StartPhase(FishingState.Casting, flight, "Lancer !");
        }

        /// <summary>Les coins de pêche de la zone où l'on est.</summary>
        private FishingSpots CurrentSpots
        {
            get
            {
                int region = _map != null ? _map.CurrentRegion : 0;
                foreach (FishingSpots spots in _spots)
                {
                    if (spots != null && spots.Region == region) return spots;
                }

                return null;
            }
        }

        private bool TryFindCastPoint(out Vector2 castPoint)
        {
            Vector2 origin = _angler != null ? (Vector2)_angler.transform.position : (Vector2)transform.position;
            Vector2 facing = _angler != null ? _angler.Facing : Vector2.up;

            // Face à un coin de pêche à portée : le lancer y va tout seul (un peu au hasard dans le coin).
            FishingSpots spots = CurrentSpots;
            if (spots != null)
            {
                IList<Vector2> centers = spots.Centers;
                int target = FishingSpotRules.Assist(origin, facing, centers, 1.5f, _castMaxDistance + GearEffects.CastBonus);
                if (target >= 0)
                {
                    Vector2 jitter = new Vector2((float)_random.NextDouble() - 0.5f, (float)_random.NextDouble() - 0.5f) * 0.8f;
                    Vector2 inSpot = centers[target] + jitter;
                    if (IsWater(inSpot))
                    {
                        castPoint = inSpot;
                        return true;
                    }
                }
            }

            // Une canne longue lance plus loin.
            float reach = GearEffects.CastBonus;
            float desired = Mathf.Lerp(_castDistance.x, _castDistance.y + reach, (float)_random.NextDouble());
            float side = Mathf.Lerp(-_castSpread, _castSpread, (float)_random.NextDouble());
            Func<Vector2, bool> isWater = _map != null ? _map.IsWater : (Func<Vector2, bool>)(_ => true);

            Vector2? point = CastTargeting.FindCastPoint(origin, facing, desired, Mathf.Max(desired, _castMaxDistance + reach), side, isWater);
            castPoint = point ?? origin;
            return point.HasValue;
        }

        private void ResolveQtePress()
        {
            QtePressResult result = Challenge.Press();
            QtePressed?.Invoke(result);

            if (Challenge.Status == QteStatus.Succeeded)
            {
                Catch();
            }
            else if (Challenge.Status == QteStatus.Failed)
            {
                Lose();
            }
        }

        private void Lose()
        {
            string name = HookedSpecies.CommonName.ToLowerInvariant();
            Finish(Challenge.FailReason == QteFailReason.LineBroke
                ? $"Crac ! La ligne a cassé : trop de tension, ta prise ({name}) est repartie."
                : $"Le fil s'est détendu… ta prise ({name}) s'est échappée !");
        }

        /// <summary>Conseil affiché au début du combat, selon la façon dont le poisson se bat.</summary>
        public static string FightHint(FightStyle style)
        {
            switch (style)
            {
                case FightStyle.Nervous: return "Il est nerveux : le curseur s'emballe, garde ton calme !";
                case FightStyle.Drifting: return "Il file sur le côté : suis la zone verte qui glisse !";
                case FightStyle.Rusher: return "Attention aux rushs : quand la jauge clignote, la zone va sauter !";
                case FightStyle.Heavy: return "Il est lourd : ferre souvent pour garder la tension basse !";
                default: return "Appuie quand le curseur est dans la zone verte !";
            }
        }

        private void Catch()
        {
            if (InSpot)
            {
                CurrentSpots?.Consume(CastPoint); // le coin s'épuise peu à peu
            }

            if (_progress != null && _random.NextDouble() < _bottleChance)
            {
                FindBottle();
                return;
            }

            // Dans la zone d'une légende, parfois un fragment de son artefact (plus souvent dans un coin de pêche).
            LegendFish legend = LegendOfZone;
            if (_progress != null && legend != null
                && FragmentRules.ShouldDrop(_random, legend.FragmentChance, InSpot, _progress.Legends.CanFindMore(legend.Id, legend.FragmentCount)))
            {
                FindFragment(legend);
                return;
            }

            FishSpecies species = HookedSpecies;
            var fish = new CaughtFish(species, HookedSizeCm);
            LastBeatenRecord = CatchLog.WouldBeRecord(species.Id, HookedSizeCm) ? CatchLog.BestSizeOf(species.Id) : 0f;
            CatchLog.Register(species.Id, HookedSizeCm);
            bool isNew = IsNewForCollection(species.Id);
            string header = $"{species.CommonName} ({species.Rarity.DisplayName()}) — {HookedSizeCm:0} cm";

            if (_progress != null && !_progress.Bag.TryAdd(fish))
            {
                // Sac plein : on attend que le joueur choisisse quoi relâcher (pas de délai).
                PendingFish = fish;
                StartPhase(FishingState.BagFull, 0f, $"{header}\nSac plein ! 1 à {_progress.Bag.Capacity} : relâcher cette prise du sac — Espace : relâcher le nouveau.");
                FishCaught?.Invoke(fish, isNew, true);
                return;
            }

            string detail = isNew
                ? "Nouvelle espèce ! Garde-la pour l'aquarium."
                : $"Doublon : le marchand te l'achètera {FishShop.PriceOf(fish)} pièces.";

            StartPhase(FishingState.Result, _catchResultSeconds, $"{header}\n{detail}");
            FishCaught?.Invoke(fish, isNew, false);
        }

        /// <summary>Ce n'était pas un poisson : une bouteille à la mer, avec un message et quelques pièces.</summary>
        private void FindBottle()
        {
            int coins = BottleMessage.Coins(_random);
            _progress.Wallet.Add(coins);
            var species = new System.Collections.Generic.List<FishSpecies>();
            if (_zone != null)
            {
                foreach (FishingZone.SpeciesEntry entry in _zone.Species)
                {
                    if (entry.Species != null) species.Add(entry.Species);
                }
            }

            string Favorite(FishSpecies s)
            {
                Bait bait = _progress.BaitCatalog != null ? _progress.BaitCatalog.FavoriteOf(s) : null;
                return bait != null ? bait.DisplayName : null;
            }

            string message = BottleMessage.Write(species, CatchLog, Favorite, _random);
            StartPhase(FishingState.Result, _catchResultSeconds, $"Une bouteille à la mer ! +{coins} pièces");
            BottleFound?.Invoke(message, coins);
        }

        /// <summary>Ce n'était pas un poisson : un fragment de l'artefact de la légende de la zone.</summary>
        private void FindFragment(LegendFish legend)
        {
            int count = _progress.Legends.AddFragment(legend.Id, legend.FragmentCount);
            string text = count >= legend.FragmentCount
                ? $"Le dernier fragment ! Va assembler l'artefact « {legend.ArtifactName} » sur le socle du laboratoire."
                : $"Un fragment de l'artefact « {legend.ArtifactName} » ! ({count} / {legend.FragmentCount})";
            StartPhase(FishingState.Result, _catchResultSeconds, text);
            FragmentFound?.Invoke(legend, count);
        }

        /// <summary>Pour les essais hors jeu : fait comme si la prochaine prise était un fragment.</summary>
        public void DebugFindFragment()
        {
            LegendFish legend = LegendOfZone;
            if (_progress != null && legend != null && _progress.Legends.CanFindMore(legend.Id, legend.FragmentCount))
            {
                FindFragment(legend);
            }
        }

        /// <summary>Nouvelle pour la collection : ni dans l'aquarium, ni déjà dans le sac.</summary>
        private bool IsNewForCollection(string speciesId)
        {
            if (_progress == null)
            {
                return CatchLog.CountOf(speciesId) == 1;
            }

            if (_progress.Aquarium.Contains(speciesId))
            {
                return false;
            }

            foreach (CaughtFish carried in _progress.Bag.Fish)
            {
                if (carried.SpeciesId == speciesId)
                {
                    return false;
                }
            }

            return true;
        }

        private void Finish(string message)
        {
            StartPhase(FishingState.Result, _resultSeconds, message);
        }

        private void StartPhase(FishingState state, float duration, string message)
        {
            State = state;
            _phaseDuration = duration;
            _timer = duration;
            Message = message;
            StateChanged?.Invoke();
        }

        private bool CountDown(float deltaTime)
        {
            _timer -= deltaTime;
            return _timer <= 0f;
        }
    }
}
