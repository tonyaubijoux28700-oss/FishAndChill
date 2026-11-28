using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.Fishing
{
    /// <summary>Ce qu'a donné le dernier anneau.</summary>
    public enum RingResult
    {
        None,
        Hit,
        Miss,
    }

    /// <summary>
    /// QTE de la remontée de l'épuisette : des anneaux se resserrent un par un sur la bête prise dans le filet ; il faut
    /// appuyer quand l'anneau la touche (rayon 1 = le bord de la bête, l'anneau part de <see cref="StartRadius"/>).
    /// Plus la bête est rare, plus il y a d'anneaux, plus ils vont vite et plus la marge est fine. Chaque famille a son
    /// rythme : les crabes serrent par à-coups, les crevettes et poissons changent de vitesse, l'encre des pieuvres cache
    /// l'anneau un moment, les coquillages sont lents mais la marge est étroite. Logique pure, testée.
    /// </summary>
    public sealed class RingQte
    {
        /// <summary>Rayon de départ de l'anneau, en rayons de la bête.</summary>
        public const float StartRadius = 3f;

        /// <summary>Pause entre deux anneaux (secondes).</summary>
        public const float Pause = 0.35f;

        private static readonly int[] RingsByRarity = { 2, 3, 3, 4, 5 };
        private static readonly float[] SecondsByRarity = { 1.6f, 1.4f, 1.25f, 1.1f, 0.95f };
        private static readonly float[] ToleranceByRarity = { 0.30f, 0.25f, 0.20f, 0.16f, 0.13f };
        private static readonly int[] MissesByRarity = { 1, 1, 0, 0, 0 };

        private readonly CritterBehaviour _behaviour;
        private readonly System.Random _random;
        private readonly float _baseSeconds;
        private float _seconds;
        private float _pause;

        public RingQte(Rarity rarity, CritterBehaviour behaviour, System.Random random)
        {
            int r = Mathf.Clamp((int)rarity, 0, RingsByRarity.Length - 1);
            _behaviour = behaviour;
            _random = random ?? new System.Random();
            Rings = RingsByRarity[r];
            MissesAllowed = MissesByRarity[r];
            _baseSeconds = SecondsByRarity[r];
            Tolerance = ToleranceByRarity[r];
            if (behaviour == CritterBehaviour.Burrow)
            {
                // Coquillages, oursins, étoiles : lents, mais il faut viser juste.
                _baseSeconds *= 1.2f;
                Tolerance *= 0.75f;
            }

            NextRing();
            _pause = 0f;
        }

        public int Rings { get; }
        public int MissesAllowed { get; }

        /// <summary>Écart permis entre l'anneau et le bord de la bête (en rayons de la bête).</summary>
        public float Tolerance { get; }

        public int Hits { get; private set; }
        public int Misses { get; private set; }

        /// <summary>Numéro de l'anneau en cours (à partir de 1).</summary>
        public int Ring => Mathf.Min(Hits + Misses + 1, Rings);

        /// <summary>Avancement de l'anneau en cours, de 0 (grand) à 1 (refermé).</summary>
        public float Progress { get; private set; }

        public bool Done => Success || Failed;
        public bool Success => Hits >= Rings;
        public bool Failed => Misses > MissesAllowed;

        /// <summary>Entre deux anneaux (rien à viser).</summary>
        public bool Waiting => _pause > 0f && !Done;

        public RingResult LastResult { get; private set; }

        /// <summary>Rayon de l'anneau en rayons de la bête (1 = il la touche).</summary>
        public float Radius => StartRadius * (1f - Shape(Progress));

        /// <summary>L'anneau se voit-il ? (l'encre le cache au milieu de sa course).</summary>
        public bool Visible => !Waiting && !Done && !(_behaviour == CritterBehaviour.Ink && Progress > 0.4f && Progress < 0.75f);

        /// <summary>L'anneau touche-t-il la bête en ce moment (pour l'éclairer) ?</summary>
        public bool OnTarget => !Waiting && !Done && Mathf.Abs(Radius - 1f) <= Tolerance;

        public void Tick(float dt)
        {
            if (Done) return;
            if (_pause > 0f)
            {
                _pause -= dt;
                return;
            }

            Progress = Mathf.Min(1f, Progress + dt / _seconds);
            if (Radius < 1f - Tolerance) Resolve(false);   // trop tard : l'anneau est passé
        }

        /// <summary>Appui du joueur : réussi si l'anneau touche la bête, raté sinon (rien entre deux anneaux).</summary>
        public RingResult Press()
        {
            if (Done || _pause > 0f) return RingResult.None;
            Resolve(Mathf.Abs(Radius - 1f) <= Tolerance);
            return LastResult;
        }

        private void Resolve(bool hit)
        {
            if (hit) Hits++;
            else Misses++;
            LastResult = hit ? RingResult.Hit : RingResult.Miss;
            if (!Done) NextRing();
        }

        private void NextRing()
        {
            Progress = 0f;
            _pause = Pause;
            // Crevettes, poissons, vers : chaque anneau a sa vitesse.
            _seconds = _behaviour == CritterBehaviour.Dart ? _baseSeconds * (0.7f + 0.45f * (float)_random.NextDouble()) : _baseSeconds;
        }

        /// <summary>Part du chemin faite selon l'avancement : régulière, ou par à-coups pour les crabes.</summary>
        private float Shape(float p)
        {
            if (_behaviour != CritterBehaviour.Hide) return p;
            const int steps = 4;
            float q = p * steps;
            float step = Mathf.Floor(q);
            float jump = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((q - step - 0.5f) / 0.5f));
            return Mathf.Min(1f, (step + jump) / steps);
        }
    }
}
