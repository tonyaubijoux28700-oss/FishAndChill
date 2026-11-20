using System;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// L'aquarium, une tour : un étage par zone, desservis par un ascenseur (une cabine dans chaque salle, le menu des
    /// étages dans <see cref="UI.ElevatorMenu"/>). La caméra ne montre que l'étage où l'on est. Les salles (voir
    /// <see cref="InteriorBuilding"/> pour
    /// l'entrée et la sortie) :
    /// - le grand hall du lagon (30×18 cases) : bassin géant tropical sur tout le mur du fond, colonnes à méduses, bancs ;
    /// - la salle du lac (24×18 cases), à droite, par un passage voûté : bassin d'eau douce aux galets et roseaux ;
    /// - la salle Amazonie (24×18 cases), encore à droite par un second passage : bassin d'eau noire, racines, feuilles ;
    /// - la salle Japon (24×18 cases) : jardin zen, shōji, bassin d'eau de mer aux rochers et algues ;
    /// - la salle Grande Barrière (24×18 cases), au bout : cabane de plage, fresque marine, bassin de coraux.
    /// Chaque bassin montre les espèces de sa zone. Devant l'un ou l'autre, E dépose le sac : chaque nouvelle espèce entre,
    /// une espèce déjà exposée ne change plus (la taille ne compte pas) ; les doublons restent dans le sac.
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
        public const float AtollRoomWidth = 24f;
        public const float AtollRoomX = ReefRoomX + ReefRoomWidth + PassageWidth;
        public const float InteriorWidth = AtollRoomX + AtollRoomWidth;
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
        public static readonly Rect AtollTank = new Rect(AtollRoomX + 2.4f, 12.4f, AtollRoomWidth - 4.8f, 4.4f);

        /// <summary>
        /// Les étages : le hall au rez-de-chaussée, puis une salle par zone. Les salles restent côte à côte dans le monde,
        /// mais sans passage entre elles : on va de l'une à l'autre par l'ascenseur, et la caméra ne montre que l'étage.
        /// </summary>
        public static readonly Rect[] Floors =
        {
            new Rect(0f, 0f, HallWidth, InteriorHeight),
            new Rect(LakeRoomX, 0f, LakeRoomWidth, InteriorHeight),
            new Rect(AmazonRoomX, 0f, AmazonRoomWidth, InteriorHeight),
            new Rect(JapanRoomX, 0f, JapanRoomWidth, InteriorHeight),
            new Rect(ReefRoomX, 0f, ReefRoomWidth, InteriorHeight),
            new Rect(AtollRoomX, 0f, AtollRoomWidth, InteriorHeight),
        };

        /// <summary>Pied de l'ascenseur de chaque étage (relatif au coin de l'intérieur) ; on l'appelle en se plaçant devant.</summary>
        public static readonly Vector2[] ElevatorSpots =
        {
            new Vector2(25.5f, 2.4f),                  // hall : à la place de la 2e colonne à méduses
            new Vector2(LakeRoomX + 5f, 1.6f),
            new Vector2(AmazonRoomX + 5f, 1.6f),
            new Vector2(JapanRoomX + 5.2f, 1.6f),      // entre la lanterne de pierre et le premier rocher
            new Vector2(ReefRoomX + 5f, 1.6f),
            new Vector2(AtollRoomX + 5f, 1.6f),
        };

        /// <summary>Colonnes à méduses (pied, relatif au coin de la salle).</summary>
        public static readonly Vector2[] JellyfishColumns = { new Vector2(4.5f, 3.2f) };

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

        [Header("Ascenseur")]
        [SerializeField, Tooltip("Zone dont chaque étage expose les espèces (même ordre que Floors).")]
        private FishingZone[] _floorZones = new FishingZone[0];

        [SerializeField, Tooltip("L'ascenseur de chaque étage (même ordre que Floors).")]
        private SpriteRenderer[] _elevators = new SpriteRenderer[0];

        [SerializeField, Tooltip("Porte fermée, à moitié, ouverte.")]
        private Sprite[] _elevatorFrames = new Sprite[0];

        private float[] _doorOpen = new float[0];

        public event Action<DepositResult> Deposited;

        /// <summary>L'étage où se trouve le joueur (0 = hall).</summary>
        public int CurrentFloor { get; private set; }

        public int FloorCount => Floors.Length;

        /// <summary>La zone exposée à un étage (null si inconnue).</summary>
        public FishingZone FloorZone(int floor) => floor >= 0 && floor < _floorZones.Length ? _floorZones[floor] : null;

        /// <summary>La caméra ne montre que l'étage où l'on est.</summary>
        protected override Rect CameraRect => WorldRect(Floors[Mathf.Clamp(CurrentFloor, 0, Floors.Length - 1)]);

        public override void Enter()
        {
            CurrentFloor = 0;
            base.Enter();
        }

        /// <summary>L'étage dont le joueur est devant l'ascenseur, ou -1.</summary>
        public int PlayerAtElevator
        {
            get
            {
                if (!IsPlayerInside || Player == null)
                {
                    return -1;
                }

                Vector2 feet = (Vector2)Player.transform.position - InteriorOrigin;
                for (int i = 0; i < ElevatorSpots.Length; i++)
                {
                    Vector2 e = ElevatorSpots[i];
                    if (Mathf.Abs(feet.x - e.x) < 1.4f && feet.y < e.y - 0.05f && feet.y > e.y - 1.6f)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        /// <summary>
        /// Emmène le joueur à un étage : devant les portes de son ascenseur, la caméra calée sur la salle. À appeler
        /// pendant un fondu au noir (<see cref="UI.ElevatorMenu"/>). Public pour les tests.
        /// </summary>
        public void GoToFloor(int floor)
        {
            if (!IsPlayerInside || Player == null || floor < 0 || floor >= Floors.Length)
            {
                return;
            }

            CurrentFloor = floor;
            Player.Teleport(InteriorOrigin + ElevatorSpots[floor] + new Vector2(0f, -1.1f), Vector2.down);
            if (FollowCamera != null)
            {
                FollowCamera.SetBoundsOverride(CameraRect);
                FollowCamera.SnapToTarget();
            }

            // Les portes de l'étage d'arrivée sont grandes ouvertes.
            if (floor < _doorOpen.Length) _doorOpen[floor] = 1f;
        }

        /// <summary>Les portes s'ouvrent quand on s'approche et se referment quand on s'éloigne.</summary>
        private void AnimateDoors()
        {
            if (_elevators.Length == 0 || _elevatorFrames.Length == 0)
            {
                return;
            }

            if (_doorOpen.Length != _elevators.Length)
            {
                _doorOpen = new float[_elevators.Length];
            }

            int near = -1;
            if (IsPlayerInside && Player != null)
            {
                Vector2 feet = (Vector2)Player.transform.position - InteriorOrigin;
                for (int i = 0; i < ElevatorSpots.Length && i < _elevators.Length; i++)
                {
                    if (Vector2.Distance(feet, ElevatorSpots[i] + new Vector2(0f, -1f)) < 2.6f) near = i;
                }
            }

            for (int i = 0; i < _elevators.Length; i++)
            {
                if (_elevators[i] == null) continue;
                _doorOpen[i] = Mathf.MoveTowards(_doorOpen[i], i == near ? 1f : 0f, Time.deltaTime * 4f);
                int frame = Mathf.Clamp(Mathf.RoundToInt(_doorOpen[i] * (_elevatorFrames.Length - 1)), 0, _elevatorFrames.Length - 1);
                _elevators[i].sprite = _elevatorFrames[frame];
            }
        }

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
                    || (feet.x >= ReefTank.xMin && feet.x <= ReefTank.xMax) || (feet.x >= AtollTank.xMin && feet.x <= AtollTank.xMax);
            }
        }

        /// <summary>Dépôt possible maintenant (devant un bassin, hors menus et hors pêche).</summary>
        public bool CanDeposit => PlayerAtTank && !Player.Locked && FishingAllowsIt();

        protected override void SetupInterior(WorldMap map)
        {
            // Le sol de chaque salle, sans passage entre elles : on change d'étage par l'ascenseur.
            for (int i = 1; i < Floors.Length; i++)
            {
                map.AddWalkableArea(new Rect(InteriorOrigin + new Vector2(Floors[i].x + 0.7f, 0.15f), new Vector2(Floors[i].width - 1.4f, FloorTop - 0.15f)));
            }

            foreach (Vector2 spot in ElevatorSpots)
            {
                Vector2 foot = InteriorOrigin + spot;
                map.AddObstacle(new Rect(foot.x - 1.5f, foot.y - 0.1f, 3f, 1.1f));
            }

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
            AnimateDoors();
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
