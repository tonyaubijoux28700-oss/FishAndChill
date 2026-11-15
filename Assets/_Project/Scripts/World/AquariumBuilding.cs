using System;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// L'aquarium : pièce de 12×8 cases avec un grand bassin sur le mur du fond (voir <see cref="InteriorBuilding"/>
    /// pour l'entrée et la sortie). Devant le bassin, E dépose le sac : chaque nouvelle espèce y entre, un poisson
    /// plus gros remplace celui exposé ; les doublons restent dans le sac pour le marchand.
    /// </summary>
    public sealed class AquariumBuilding : InteriorBuilding
    {
        public const float InteriorWidth = 12f;
        public const float InteriorHeight = 8f;

        /// <summary>Haut du sol : au-delà commence le bassin.</summary>
        private const float FloorTop = 4.65f;

        [SerializeField] private PlayerProgress _progress;

        [SerializeField, Tooltip("Optionnel : pas de dépôt pendant une partie de pêche.")]
        private FishingController _fishing;

        public event Action<DepositResult> Deposited;

        public override Vector2 InteriorSize => new Vector2(InteriorWidth, InteriorHeight);

        protected override Rect FloorArea => new Rect(0.6f, 0.15f, InteriorWidth - 1.2f, FloorTop - 0.15f);

        /// <summary>Zone d'eau du bassin, en coordonnées du monde (les poissons y nagent).</summary>
        public Rect SwimArea => new Rect(InteriorOrigin + new Vector2(1.5f, 5.3f), new Vector2(InteriorWidth - 3f, 1.75f));

        /// <summary>Le joueur est devant la vitre du bassin et peut déposer.</summary>
        public bool PlayerAtTank
        {
            get
            {
                if (!IsPlayerInside || Player == null)
                {
                    return false;
                }

                Vector2 feet = Player.transform.position;
                return feet.y >= InteriorOrigin.y + FloorTop - 1.3f
                    && Mathf.Abs(feet.x - (InteriorOrigin.x + InteriorWidth * 0.5f)) <= InteriorWidth * 0.5f - 1.2f;
            }
        }

        /// <summary>Dépôt possible maintenant (devant le bassin, hors menus et hors pêche).</summary>
        public bool CanDeposit => PlayerAtTank && !Player.Locked && FishingAllowsIt();

        protected override void Update()
        {
            base.Update();
            if (CanDeposit && InteractInput.PressedThisFrame())
            {
                Deposit();
            }
        }

        /// <summary>Dépose le sac dans le bassin. Public pour pouvoir le déclencher hors clavier (tests).</summary>
        public DepositResult Deposit()
        {
            if (_progress == null)
            {
                return new DepositResult();
            }

            _progress.EnsureCreated();
            DepositResult result = _progress.DepositBag();
            Deposited?.Invoke(result);
            return result;
        }

        private bool FishingAllowsIt()
        {
            return _fishing == null || _fishing.State == FishingState.Idle || _fishing.State == FishingState.Result;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(SwimArea.center, SwimArea.size);
        }
    }
}
