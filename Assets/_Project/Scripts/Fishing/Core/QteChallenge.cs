using System;
using UnityEngine;

namespace FishAndChill.Fishing
{
    public enum QteStatus
    {
        InProgress,
        Succeeded,
        Failed,
    }

    public enum QtePressResult
    {
        Hit,
        Miss,
        Ignored,
    }

    /// <summary>Pourquoi le poisson a été perdu.</summary>
    public enum QteFailReason
    {
        None,
        Escaped,    // trop de ratés : le fil s'est détendu
        LineBroke,  // tension au maximum (poisson lourd) : la ligne a cassé
    }

    /// <summary>
    /// Un combat contre un poisson : plusieurs appuis réussis sur la jauge pour le remonter, un nombre limité de ratés
    /// avant qu'il s'échappe. Après chaque réussite, la zone se déplace. Chaque poisson se bat à sa façon
    /// (<see cref="FightStyle"/>) : curseur nerveux, zone qui glisse, rushs soudains, ou tension à surveiller.
    /// Logique pure, testée.
    /// </summary>
    public sealed class QteChallenge
    {
        /// <summary>Le clignotement qui annonce un rush commence ce temps-là avant (secondes).</summary>
        public const float RushWarningTime = 0.45f;
        public const float RushIntervalMin = 1.6f;
        public const float RushIntervalMax = 3.0f;
        /// <summary>Vitesse de glissement de la zone d'un poisson fuyant (largeurs de barre par seconde).</summary>
        public const float DriftSpeed = 0.22f;
        /// <summary>Tension gagnée par seconde (poisson lourd), retirée par une réussite, ajoutée par un raté.</summary>
        public const float TensionRise = 0.12f;
        public const float TensionHitRelief = 0.3f;
        public const float TensionMissPenalty = 0.22f;

        private readonly System.Random _random;
        private readonly float _tensionRate;
        private readonly float _nervousFactor;
        private readonly float _rushFactor;
        private readonly float _driftFactor;
        private float _time;
        private float _drift = 1f;
        private float _rushTimer;

        public QteChallenge(QteDifficulty difficulty, System.Random random, FightStyle style = FightStyle.Calm, float tensionRate = 1f,
            float nervousFactor = 1f, float rushFactor = 1f, float driftFactor = 1f)
        {
            if (difficulty == null)
            {
                throw new ArgumentNullException(nameof(difficulty));
            }

            _random = random ?? throw new ArgumentNullException(nameof(random));
            Style = style;
            _tensionRate = Mathf.Max(0f, tensionRate);
            _nervousFactor = Mathf.Max(0f, nervousFactor);
            _rushFactor = Mathf.Max(0.1f, rushFactor);
            _driftFactor = Mathf.Max(0f, driftFactor);
            RequiredHits = difficulty.RequiredHits;
            MaxMisses = difficulty.MaxMisses;
            Gauge = new TimingGauge(difficulty.CursorSpeed, difficulty.ZoneWidth, RandomCenter());
            _drift = _random.NextDouble() < 0.5 ? -1f : 1f;
            _rushTimer = NextRushDelay();
        }

        public TimingGauge Gauge { get; }
        public FightStyle Style { get; }
        public int RequiredHits { get; }
        public int MaxMisses { get; }
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public QteStatus Status { get; private set; } = QteStatus.InProgress;
        public QteFailReason FailReason { get; private set; }

        /// <summary>Tension de la ligne, de 0 à 1 (poisson lourd seulement).</summary>
        public float Tension { get; private set; }

        public bool UsesTension => Style == FightStyle.Heavy;

        /// <summary>Un rush arrive : la jauge clignote pour prévenir.</summary>
        public bool RushWarning => Style == FightStyle.Rusher && Status == QteStatus.InProgress && _rushTimer <= RushWarningTime;

        /// <summary>Nombre de rushs déjà faits (l'affichage secoue la jauge à chaque nouveau).</summary>
        public int Rushes { get; private set; }

        public void Tick(float deltaTime)
        {
            if (Status != QteStatus.InProgress)
            {
                return;
            }

            float dt = Mathf.Max(0f, deltaTime);
            _time += dt;
            switch (Style)
            {
                case FightStyle.Nervous:
                    // Le curseur s'emballe puis ralentit : entre 0,5 et 1,5 fois sa vitesse.
                    Gauge.SpeedMultiplier = 1f + 0.5f * _nervousFactor * Mathf.Sin(_time * 6.5f);
                    break;

                case FightStyle.Drifting:
                    float half = Gauge.ZoneWidth * 0.5f;
                    float center = Gauge.ZoneCenter + _drift * DriftSpeed * _driftFactor * dt;
                    if (center <= half || center >= 1f - half)
                    {
                        _drift = -_drift;
                    }

                    Gauge.MoveZone(center);
                    break;

                case FightStyle.Rusher:
                    _rushTimer -= dt;
                    if (_rushTimer <= 0f)
                    {
                        Rush();
                    }

                    break;

                case FightStyle.Heavy:
                    AddTension(TensionRise * _tensionRate * dt);
                    break;
            }

            if (Status == QteStatus.InProgress)
            {
                Gauge.Tick(dt);
            }
        }

        public QtePressResult Press()
        {
            if (Status != QteStatus.InProgress)
            {
                return QtePressResult.Ignored;
            }

            if (Gauge.IsInZone)
            {
                Hits++;
                if (UsesTension)
                {
                    Tension = Mathf.Max(0f, Tension - TensionHitRelief);
                }

                if (Hits >= RequiredHits)
                {
                    Status = QteStatus.Succeeded;
                }
                else
                {
                    Gauge.MoveZone(RandomCenter());
                }

                return QtePressResult.Hit;
            }

            Misses++;
            if (UsesTension)
            {
                AddTension(TensionMissPenalty);
            }

            if (Status == QteStatus.InProgress && Misses > MaxMisses)
            {
                Status = QteStatus.Failed;
                FailReason = QteFailReason.Escaped;
            }

            return QtePressResult.Miss;
        }

        private void AddTension(float amount)
        {
            Tension = Mathf.Min(1f, Tension + amount);
            if (Tension >= 1f)
            {
                Status = QteStatus.Failed;
                FailReason = QteFailReason.LineBroke;
            }
        }

        /// <summary>Le poisson fonce : la zone saute loin de sa place.</summary>
        private void Rush()
        {
            float current = Gauge.ZoneCenter;
            float next = RandomCenter();
            for (int i = 0; i < 6 && Mathf.Abs(next - current) < 0.3f; i++)
            {
                next = RandomCenter();
            }

            Gauge.MoveZone(next);
            Rushes++;
            _rushTimer = NextRushDelay();
        }

        private float NextRushDelay() => Mathf.Lerp(RushIntervalMin, RushIntervalMax, (float)_random.NextDouble()) * _rushFactor;

        private float RandomCenter()
        {
            return (float)_random.NextDouble();
        }
    }
}
