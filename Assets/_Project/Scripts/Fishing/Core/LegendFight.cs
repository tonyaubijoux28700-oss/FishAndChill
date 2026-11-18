using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Une phase du combat d'une légende : sa façon de se battre et la difficulté de la jauge.
    /// </summary>
    [Serializable]
    public sealed class LegendPhase
    {
        [SerializeField] private FightStyle _style = FightStyle.Calm;
        [SerializeField] private QteDifficulty _difficulty = new QteDifficulty();
        [SerializeField, Tooltip("Ce qu'il fait, affiché au début de la phase (« Il plonge ! »).")] private string _cry;

        public LegendPhase()
        {
        }

        public LegendPhase(FightStyle style, QteDifficulty difficulty, string cry)
        {
            _style = style;
            _difficulty = difficulty;
            _cry = cry;
        }

        public FightStyle Style => _style;
        public QteDifficulty Difficulty => _difficulty;
        public string Cry => _cry;
    }

    /// <summary>
    /// Le combat contre une légende : plusieurs phases enchaînées, chacune une jauge (<see cref="QteChallenge"/>) avec sa
    /// façon de se battre. L'endurance de la légende baisse à chaque réussite, sur l'ensemble des phases. Entre deux
    /// phases, une courte pause (la légende change de tactique) pendant laquelle les appuis ne comptent pas. Un échec dans
    /// n'importe quelle phase et elle s'échappe. Logique pure, testée.
    /// </summary>
    public sealed class LegendFight
    {
        /// <summary>Durée de la pause entre deux phases (secondes).</summary>
        public const float BreakSeconds = 1.3f;

        private readonly IReadOnlyList<LegendPhase> _phases;
        private readonly List<QteChallenge> _challenges = new List<QteChallenge>();
        private readonly int _totalHits;
        private int _hitsBefore;   // réussites des phases déjà gagnées

        /// <param name="createChallenge">Crée la jauge d'une phase (le matériel du joueur y est appliqué).</param>
        public LegendFight(IReadOnlyList<LegendPhase> phases, Func<LegendPhase, QteChallenge> createChallenge)
        {
            if (phases == null || phases.Count == 0)
            {
                throw new ArgumentException("Une légende a au moins une phase.", nameof(phases));
            }

            if (createChallenge == null)
            {
                throw new ArgumentNullException(nameof(createChallenge));
            }

            _phases = phases;
            foreach (LegendPhase phase in phases)
            {
                QteChallenge challenge = createChallenge(phase);
                _challenges.Add(challenge);
                _totalHits += challenge.RequiredHits;
            }
        }

        /// <summary>La jauge de la phase en cours.</summary>
        public QteChallenge Challenge => _challenges[Phase];

        /// <summary>Numéro de la phase en cours, à partir de 0.</summary>
        public int Phase { get; private set; }

        public int PhaseCount => _phases.Count;
        public LegendPhase CurrentPhase => _phases[Phase];
        public QteStatus Status { get; private set; } = QteStatus.InProgress;
        public QteFailReason FailReason => Challenge.FailReason;

        /// <summary>Pause entre deux phases en cours (secondes restantes, 0 sinon).</summary>
        public float Break { get; private set; }

        public bool InBreak => Break > 0f;

        /// <summary>Endurance de la légende, de 1 (fraîche) à 0 (épuisée), sur tout le combat.</summary>
        public float Stamina => _totalHits <= 0 ? 0f : Mathf.Clamp01(1f - (_hitsBefore + Challenge.Hits) / (float)_totalHits);

        public void Tick(float deltaTime)
        {
            if (Status != QteStatus.InProgress)
            {
                return;
            }

            if (Break > 0f)
            {
                Break = Mathf.Max(0f, Break - Mathf.Max(0f, deltaTime));
                return;
            }

            Challenge.Tick(deltaTime);
            Check();
        }

        public QtePressResult Press()
        {
            if (Status != QteStatus.InProgress || Break > 0f)
            {
                return QtePressResult.Ignored;
            }

            QtePressResult result = Challenge.Press();
            Check();
            return result;
        }

        private void Check()
        {
            if (Challenge.Status == QteStatus.Failed)
            {
                Status = QteStatus.Failed;
                return;
            }

            if (Challenge.Status != QteStatus.Succeeded)
            {
                return;
            }

            if (Phase >= _phases.Count - 1)
            {
                Status = QteStatus.Succeeded;
                return;
            }

            // Phase gagnée : courte pause, puis la légende change de tactique.
            _hitsBefore += Challenge.Hits;
            Phase++;
            Break = BreakSeconds;
        }
    }
}
