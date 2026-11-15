using System;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// L'aquarium, un des plus grands bâtiments du jeu : grande salle de 30×18 cases (plus grande que l'écran, la caméra
    /// suit le joueur) avec un bassin géant sur tout le mur du fond, deux colonnes à méduses et des bancs
    /// (voir <see cref="InteriorBuilding"/> pour l'entrée et la sortie). Devant le bassin, E dépose le sac : chaque
    /// nouvelle espèce y entre, un poisson plus gros remplace celui exposé ; les doublons restent dans le sac.
    /// </summary>
    public sealed class AquariumBuilding : InteriorBuilding
    {
        public const float InteriorWidth = 30f;
        public const float InteriorHeight = 18f;

        /// <summary>Haut du sol : au-delà commence le bassin.</summary>
        public const float FloorTop = 10.75f;

        /// <summary>Colonnes à méduses (pied, relatif au coin de la salle).</summary>
        public static readonly Vector2[] JellyfishColumns = { new Vector2(4.5f, 3.2f), new Vector2(25.5f, 3.2f) };

        /// <summary>Bancs face au bassin (pied, relatif au coin de la salle).</summary>
        public static readonly Vector2[] Benches = { new Vector2(9f, 6.2f), new Vector2(21f, 6.2f) };

        [SerializeField] private PlayerProgress _progress;

        [SerializeField, Tooltip("Optionnel : pas de dépôt pendant une partie de pêche.")]
        private FishingController _fishing;

        public event Action<DepositResult> Deposited;

        public override Vector2 InteriorSize => new Vector2(InteriorWidth, InteriorHeight);

        protected override Rect FloorArea => new Rect(0.7f, 0.15f, InteriorWidth - 1.4f, FloorTop - 0.15f);

        /// <summary>Zone d'eau du bassin, en coordonnées du monde (les poissons y nagent).</summary>
        public Rect SwimArea => new Rect(InteriorOrigin + new Vector2(2.4f, 12.4f), new Vector2(InteriorWidth - 4.8f, 4.4f));

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
                return feet.y >= InteriorOrigin.y + FloorTop - 1.4f
                    && Mathf.Abs(feet.x - (InteriorOrigin.x + InteriorWidth * 0.5f)) <= InteriorWidth * 0.5f - 2f;
            }
        }

        /// <summary>Dépôt possible maintenant (devant le bassin, hors menus et hors pêche).</summary>
        public bool CanDeposit => PlayerAtTank && !Player.Locked && FishingAllowsIt();

        protected override void SetupInterior(WorldMap map)
        {
            foreach (Vector2 spot in JellyfishColumns)
            {
                Vector2 foot = InteriorOrigin + spot;
                map.AddObstacle(new Rect(foot.x - 1.3f, foot.y - 0.1f, 2.6f, 1.1f));
            }

            foreach (Vector2 spot in Benches)
            {
                Vector2 foot = InteriorOrigin + spot;
                map.AddObstacle(new Rect(foot.x - 1.9f, foot.y - 0.1f, 3.8f, 0.8f));
            }
        }

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
