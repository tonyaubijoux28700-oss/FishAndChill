using System;
using FishAndChill.Collection;
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
    }

    /// <summary>
    /// Déroulé d'une partie de pêche : lancer → attente → touche → QTE → résultat.
    /// Ne gère aucun affichage : la vue écoute ses propriétés et ses événements.
    /// </summary>
    public sealed class FishingController : MonoBehaviour
    {
        private const string IdleMessage = "Appuie sur Espace pour lancer ta ligne";

        [SerializeField] private FishingZone _zone;

        [Header("Attente de la touche (secondes)")]
        [SerializeField, Min(0f)] private float _minWaitSeconds = 1.5f;
        [SerializeField, Min(0f)] private float _maxWaitSeconds = 4f;

        [Header("Réactivité")]
        [SerializeField, Min(0.1f), Tooltip("Temps pour ferrer une fois que ça mord.")]
        private float _biteWindowSeconds = 1f;

        [SerializeField, Min(0.5f)] private float _resultSeconds = 2.5f;

        private System.Random _random;
        private float _timer;
        private float _phaseDuration;

        public event Action StateChanged;
        public event Action<QtePressResult> QtePressed;
        public event Action<FishSpecies, bool> FishCaught;

        public FishingState State { get; private set; } = FishingState.Idle;
        public FishingZone Zone => _zone;
        public FishSpecies HookedSpecies { get; private set; }
        public float HookedSizeCm { get; private set; }
        public QteChallenge Challenge { get; private set; }
        public CatchLog CatchLog { get; } = new CatchLog();
        public string Message { get; private set; } = IdleMessage;

        /// <summary>Avancement de la phase en cours, de 0 à 1 (attente, fenêtre de touche, résultat).</summary>
        public float PhaseProgress => _phaseDuration > 0f ? Mathf.Clamp01(1f - _timer / _phaseDuration) : 0f;

        private void Awake()
        {
            _random = new System.Random();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
            if (FishingInput.ActionPressedThisFrame())
            {
                PressAction();
            }
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
                    if (PhaseProgress > 0.2f)
                    {
                        StartPhase(FishingState.Idle, 0f, IdleMessage);
                    }

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
            bool isNew = CatchLog.Register(species.Id);
            string header = $"{species.CommonName} ({species.Rarity.DisplayName()}) — {HookedSizeCm:0} cm";
            string detail = isNew
                ? "Nouvelle espèce ! Direction l'aquarium."
                : $"Déjà dans l'aquarium : se revendra {species.BasePrice} pièces.";

            Finish($"{header}\n{detail}");
            FishCaught?.Invoke(species, isNew);
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
