using System;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// L'aquarium, un des plus grands bâtiments du jeu, en deux salles (voir <see cref="InteriorBuilding"/> pour
    /// l'entrée et la sortie) :
    /// - le grand hall du lagon (30×18 cases) : bassin géant tropical sur tout le mur du fond, colonnes à méduses, bancs ;
    /// - la salle du lac (24×18 cases), à droite, par un passage voûté : bassin d'eau douce aux galets et roseaux ;
    /// - la salle Amazonie (24×18 cases), encore à droite par un second passage : bassin d'eau noire, racines, feuilles ;
    /// - la salle Japon (24×18 cases) : jardin zen, shōji, bassin d'eau de mer aux rochers et algues ;
    /// - la salle Grande Barrière (24×18 cases), au bout : cabane de plage, fresque marine, bassin de coraux.
    /// Chaque bassin montre les espèces de sa zone. Devant l'un ou l'autre, E dépose le sac : chaque nouvelle espèce entre,
    /// un poisson plus gros remplace celui exposé ; les doublons restent dans le sac.
    /// </summary>
    public sealed class AquariumBuilding : InteriorBuilding
    {
        public const float HallWidth = 30f;
        public const float PassageWidth = 2f;
        public const float LakeRoomWidth = 24f;
        public const float LakeRoomX = HallWidth + PassageWidth;
        public const float AmazonRoomWidth = 24f;
        public const float AmazonRoomX = LakeRoomX + LakeRoomWidth + PassageWidth;
        public const float JapanRoomWidth = 24f;
        public const float JapanRoomX = AmazonRoomX + AmazonRoomWidth + PassageWidth;
        public const float ReefRoomWidth = 24f;
        public const float ReefRoomX = JapanRoomX + JapanRoomWidth + PassageWidth;
        public const float InteriorWidth = ReefRoomX + ReefRoomWidth;
        public const float InteriorHeight = 18f;

        /// <summary>Haut du sol : au-delà commencent les bassins.</summary>
        public const float FloorTop = 10.75f;

        /// <summary>Passage voûté entre les deux salles : bas et haut, relatifs au coin de la salle.</summary>
        public const float PassageBottom = 3.2f;
        public const float PassageTop = 6.0f;

        /// <summary>Eau du bassin du hall et de celui de la salle du lac (relatives au coin de la salle).</summary>
        public static readonly Rect HallTank = new Rect(2.4f, 12.4f, HallWidth - 4.8f, 4.4f);
        public static readonly Rect LakeTank = new Rect(LakeRoomX + 2.4f, 12.4f, LakeRoomWidth - 4.8f, 4.4f);
        public static readonly Rect AmazonTank = new Rect(AmazonRoomX + 2.4f, 12.4f, AmazonRoomWidth - 4.8f, 4.4f);
        public static readonly Rect JapanTank = new Rect(JapanRoomX + 2.4f, 12.4f, JapanRoomWidth - 4.8f, 4.4f);
        public static readonly Rect ReefTank = new Rect(ReefRoomX + 2.4f, 12.4f, ReefRoomWidth - 4.8f, 4.4f);

        /// <summary>Colonnes à méduses (pied, relatif au coin de la salle).</summary>
        public static readonly Vector2[] JellyfishColumns = { new Vector2(4.5f, 3.2f), new Vector2(25.5f, 3.2f) };

        /// <summary>Bancs face aux bassins (pied, relatif au coin de la salle).</summary>
        public static readonly Vector2[] Benches =
        {
            new Vector2(9f, 6.2f), new Vector2(21f, 6.2f),
        };

        [SerializeField] private PlayerProgress _progress;

        [SerializeField, Tooltip("Pied du mobilier des autres salles (barque, pirogue, rochers…), relatif au coin de l'intérieur : infranchissable.")]
        private System.Collections.Generic.List<Rect> _furniture = new System.Collections.Generic.List<Rect>();

        [SerializeField, Tooltip("Optionnel : pas de dépôt pendant une partie de pêche.")]
        private FishingController _fishing;

        public event Action<DepositResult> Deposited;

        public override Vector2 InteriorSize => new Vector2(InteriorWidth, InteriorHeight);

        /// <summary>La porte d'entrée est au milieu du mur du bas du grand hall.</summary>
        protected override float EntranceX => HallWidth * 0.5f;

        protected override Rect FloorArea => new Rect(0.7f, 0.15f, HallWidth - 1.4f, FloorTop - 0.15f);

        /// <summary>Eau d'un bassin en coordonnées du monde.</summary>
        public Rect WorldRect(Rect local) => new Rect(InteriorOrigin + local.position, local.size);

        /// <summary>Le joueur est devant la vitre d'un des bassins et peut déposer.</summary>
        public bool PlayerAtTank
        {
            get
            {
                if (!IsPlayerInside || Player == null)
                {
                    return false;
                }

                Vector2 feet = (Vector2)Player.transform.position - InteriorOrigin;
                if (feet.y < FloorTop - 1.4f)
                {
                    return false;
                }

                return (feet.x >= HallTank.xMin && feet.x <= HallTank.xMax) || (feet.x >= LakeTank.xMin && feet.x <= LakeTank.xMax)
                    || (feet.x >= AmazonTank.xMin && feet.x <= AmazonTank.xMax) || (feet.x >= JapanTank.xMin && feet.x <= JapanTank.xMax)
                    || (feet.x >= ReefTank.xMin && feet.x <= ReefTank.xMax);
            }
        }

        /// <summary>Dépôt possible maintenant (devant un bassin, hors menus et hors pêche).</summary>
        public bool CanDeposit => PlayerAtTank && !Player.Locked && FishingAllowsIt();

        protected override void SetupInterior(WorldMap map)
        {
            // Le passage voûté et le sol de la salle du lac.
            map.AddWalkableArea(new Rect(InteriorOrigin + new Vector2(HallWidth - 1f, PassageBottom), new Vector2(PassageWidth + 2f, PassageTop - PassageBottom)));
            map.AddWalkableArea(new Rect(InteriorOrigin + new Vector2(LakeRoomX + 0.7f, 0.15f), new Vector2(LakeRoomWidth - 1.4f, FloorTop - 0.15f)));
            // Deuxième passage, au bout de la salle du lac, et la salle Amazonie.
            map.AddWalkableArea(new Rect(InteriorOrigin + new Vector2(AmazonRoomX - PassageWidth - 1f, PassageBottom), new Vector2(PassageWidth + 2f, PassageTop - PassageBottom)));
            map.AddWalkableArea(new Rect(InteriorOrigin + new Vector2(AmazonRoomX + 0.7f, 0.15f), new Vector2(AmazonRoomWidth - 1.4f, FloorTop - 0.15f)));
            // Troisième passage, au bout de la salle Amazonie, et la salle Japon.
            map.AddWalkableArea(new Rect(InteriorOrigin + new Vector2(JapanRoomX - PassageWidth - 1f, PassageBottom), new Vector2(PassageWidth + 2f, PassageTop - PassageBottom)));
            map.AddWalkableArea(new Rect(InteriorOrigin + new Vector2(JapanRoomX + 0.7f, 0.15f), new Vector2(JapanRoomWidth - 1.4f, FloorTop - 0.15f)));
            // Quatrième passage, au bout de la salle Japon, et la salle Grande Barrière.
            map.AddWalkableArea(new Rect(InteriorOrigin + new Vector2(ReefRoomX - PassageWidth - 1f, PassageBottom), new Vector2(PassageWidth + 2f, PassageTop - PassageBottom)));
            map.AddWalkableArea(new Rect(InteriorOrigin + new Vector2(ReefRoomX + 0.7f, 0.15f), new Vector2(ReefRoomWidth - 1.4f, FloorTop - 0.15f)));

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

            foreach (Rect item in _furniture)
            {
                map.AddObstacle(new Rect(InteriorOrigin + item.position, item.size));
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

        /// <summary>Dépose le sac dans les bassins. Public pour pouvoir le déclencher hors clavier (tests).</summary>
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
            Rect hall = WorldRect(HallTank);
            Rect lake = WorldRect(LakeTank);
            Gizmos.DrawWireCube(hall.center, hall.size);
            Gizmos.DrawWireCube(lake.center, lake.size);
        }
    }
}
