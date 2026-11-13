using System;

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

    /// <summary>
    /// Un combat contre un poisson : plusieurs appuis réussis sur la jauge pour le remonter,
    /// un nombre limité de ratés avant qu'il s'échappe. Après chaque réussite, la zone se déplace.
    /// </summary>
    public sealed class QteChallenge
    {
        private readonly Random _random;

        public QteChallenge(QteDifficulty difficulty, Random random)
        {
            if (difficulty == null)
            {
                throw new ArgumentNullException(nameof(difficulty));
            }

            _random = random ?? throw new ArgumentNullException(nameof(random));
            RequiredHits = difficulty.RequiredHits;
            MaxMisses = difficulty.MaxMisses;
            Gauge = new TimingGauge(difficulty.CursorSpeed, difficulty.ZoneWidth, RandomCenter());
        }

        public TimingGauge Gauge { get; }
        public int RequiredHits { get; }
        public int MaxMisses { get; }
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public QteStatus Status { get; private set; } = QteStatus.InProgress;

        public void Tick(float deltaTime)
        {
            if (Status == QteStatus.InProgress)
            {
                Gauge.Tick(deltaTime);
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
            if (Misses > MaxMisses)
            {
                Status = QteStatus.Failed;
            }

            return QtePressResult.Miss;
        }

        private float RandomCenter()
        {
            return (float)_random.NextDouble();
        }
    }
}
