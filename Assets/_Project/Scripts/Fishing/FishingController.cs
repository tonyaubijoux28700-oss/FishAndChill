using System;
using FishAndChill.Collection;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;

namespace FishAndChill.Fishing
{
    public enum FishingState
    {
        Idle,
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

        [Header("Réactivité")]
        [SerializeField, Min(0.1f), Tooltip("Temps pour ferrer une fois que ça mord.")]
        private float _biteWindowSeconds = 1f;

        [SerializeField, Min(0.5f)] private float _resultSeconds = 2.5f;

        [SerializeField, Min(0.5f), Tooltip("Durée d'affichage de la carte de prise (Espace pour passer).")]
        private float _catchResultSeconds = 6f;

        private System.Random _random;
        private float _timer;
        private float _phaseDuration;

        public event Action StateChanged;
        public event Action<QtePressResult> QtePressed;
        /// <summary>Un poisson est attrapé : (poisson, nouvelle espèce ?, sac plein ?).</summary>
        public event Action<CaughtFish, bool, bool> FishCaught;

        public FishingState State { get; private set; } = FishingState.Idle;
        public FishingZone Zone => _zone;

        /// <summary>Point où le bouchon est tombé (valide pendant la pêche).</summary>
        public Vector2 CastPoint { get; private set; }
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
                case FishingState.Waiting:
                    if (CountDown(deltaTime))
                    {
                        StartPhase(FishingState.Bite, _biteWindowSeconds, "Ça mord ! Appuie vite pour ferrer !");
                    }

                    break;

                case FishingState.Bite:
                    if (CountDown(deltaTime))
                    {
                        Finish("Trop lent… le poisson a lâché l'appât.");
                    }

                    break;

                case FishingState.Reeling:
                    Challenge.Tick(deltaTime);
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

                case FishingState.Waiting:
                    Finish("Trop tôt ! Tu as remonté ta ligne à vide.");
                    break;

                case FishingState.Bite:
                    Challenge = new QteChallenge(HookedSpecies.Difficulty, _random);
                    StartPhase(FishingState.Reeling, 0f, "Appuie quand le curseur est dans la zone verte !");
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

        private void Cast()
        {
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
            HookedSpecies = _zone.PickSpecies(_random);
            if (HookedSpecies == null)
            {
                Finish($"Aucun poisson dans la zone « {_zone.DisplayName} ».");
                return;
            }

            Vector2 range = HookedSpecies.SizeRangeCm;
            HookedSizeCm = Mathf.Lerp(range.x, range.y, (float)_random.NextDouble());
            float wait = Mathf.Lerp(_minWaitSeconds, Mathf.Max(_minWaitSeconds, _maxWaitSeconds), (float)_random.NextDouble());
            StartPhase(FishingState.Waiting, wait, "Patience… attends que ça morde.");
        }

        private bool TryFindCastPoint(out Vector2 castPoint)
        {
            Vector2 origin = _angler != null ? (Vector2)_angler.transform.position : (Vector2)transform.position;
            Vector2 facing = _angler != null ? _angler.Facing : Vector2.up;
            float desired = Mathf.Lerp(_castDistance.x, _castDistance.y, (float)_random.NextDouble());
            float side = Mathf.Lerp(-_castSpread, _castSpread, (float)_random.NextDouble());
            Func<Vector2, bool> isWater = _map != null ? _map.IsWater : (Func<Vector2, bool>)(_ => true);

            Vector2? point = CastTargeting.FindCastPoint(origin, facing, desired, Mathf.Max(desired, _castMaxDistance), side, isWater);
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
                Finish("Le fil s'est détendu… le poisson s'est échappé !");
            }
        }

        private void Catch()
        {
            FishSpecies species = HookedSpecies;
            var fish = new CaughtFish(species, HookedSizeCm);
            CatchLog.Register(species.Id);
            bool isNew = IsNewForCollection(species.Id);
            string header = $"{species.CommonName} ({species.Rarity.DisplayName()}) — {HookedSizeCm:0} cm";

            if (_progress != null && !_progress.Bag.TryAdd(fish))
            {
                // Sac plein : on attend que le joueur choisisse quoi relâcher (pas de délai).
                PendingFish = fish;
                StartPhase(FishingState.BagFull, 0f, $"{header}\nSac plein ! 1, 2 ou 3 : relâcher ce poisson du sac — Espace : relâcher le nouveau.");
                FishCaught?.Invoke(fish, isNew, true);
                return;
            }

            string detail = isNew
                ? "Nouvelle espèce ! Garde-la pour l'aquarium."
                : $"Doublon : le marchand te l'achètera {FishShop.PriceOf(fish)} pièces.";

            StartPhase(FishingState.Result, _catchResultSeconds, $"{header}\n{detail}");
            FishCaught?.Invoke(fish, isNew, false);
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
