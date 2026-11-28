using System.Collections.Generic;
using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// La grotte de Chiyo, au pied d'une montagne de la baie d'Ha Long : fermée (lanternes éteintes, trop noire pour y
    /// entrer) tant que le collier de Chiyo n'est pas complet ; ouverte ensuite (lanternes allumées). Dedans, une salle de
    /// 13×9 cases et sa grande flaque (<c>UI/TidePoolGame</c> y sort l'épuisette quand on se tient au bord).
    /// </summary>
    public sealed class CaveBuilding : InteriorBuilding
    {
        public const float InteriorWidth = 13f;
        public const float InteriorHeight = 9f;

        [SerializeField, Tooltip("La montagne vue de dehors, lanternes allumées (grotte ouverte).")] private Sprite _lit;
        [SerializeField, Tooltip("La même, lanternes éteintes (grotte fermée).")] private Sprite _dark;
        [SerializeField, Tooltip("Zone de la grotte (ses bêtes, dans l'ordre où elles se montrent).")] private FishingZone _zone;
        [SerializeField, Tooltip("Pieds des obstacles de la salle (flaque, stalagmites), depuis son coin bas-gauche.")]
        private List<Rect> _obstacles = new List<Rect>();
        [SerializeField, Tooltip("Centre de la flaque, en cases depuis le coin bas-gauche de la salle.")] private Vector2 _poolCenter = new Vector2(6.5f, 3.75f);
        [SerializeField, Tooltip("Demi-axes de la flaque, en cases.")] private Vector2 _poolRadii = new Vector2(3.4f, 1.8f);

        [SerializeField, Tooltip("Où se tient Chiyo, une fois la méduse immortelle attrapée (pieds, en cases dans la salle).")]
        private Vector2 _chiyoSpot = new Vector2(10.2f, 2.3f);

        private SpriteRenderer _renderer;
        private PlayerProgress _progress;
        private Fishing.FishingController _fishing;
        private SpriteRenderer _chiyo;
        private float _hintTimer;

        /// <summary>La méduse immortelle est au carnet : Chiyo attend dans la grotte.</summary>
        public bool ChiyoHere => _fishing != null && _fishing.CatchLog != null && _fishing.CatchLog.IsDiscovered(CaveRules.LastId);

        /// <summary>Le joueur est dans la grotte, tout près de Chiyo (pour lui parler).</summary>
        public bool NearChiyo(Vector2 feet) => ChiyoHere && IsPlayerInside && Vector2.Distance(feet, InteriorOrigin + _chiyoSpot) < 1.3f;

        /// <summary>Chiyo debout près de la flaque, face au joueur (son portrait d'habitante).</summary>
        private void UpdateChiyo()
        {
            bool here = ChiyoHere;
            if (_chiyo == null)
            {
                if (!here) return;
                Sprite portrait = null;
                foreach (Villagers.Villager villager in Villagers.Villager.Everyone)
                {
                    if (villager.DisplayName == "Chiyo") portrait = villager.Portrait;
                }

                if (portrait == null) return;
                var go = new GameObject("Chiyo dans la grotte");
                go.transform.SetParent(transform, false);
                go.transform.position = InteriorOrigin + _chiyoSpot;
                _chiyo = go.AddComponent<SpriteRenderer>();
                _chiyo.sprite = portrait;
                _chiyo.sortingOrder = 10;
                _chiyo.spriteSortPoint = SpriteSortPoint.Pivot;
            }

            _chiyo.enabled = here;
        }

        /// <summary>La grotte de la scène (il n'y en a qu'une).</summary>
        public static CaveBuilding Current { get; private set; }

        /// <summary>Phrase à afficher un moment (on a poussé l'entrée fermée), sinon null.</summary>
        public string Hint => _hintTimer > 0f ? "Il fait noir comme dans un four là-dedans... Mieux vaut ne pas s'y aventurer." : null;

        public FishingZone Zone => _zone;

        /// <summary>Le collier de Chiyo est complet : les lanternes sont allumées, on peut entrer.</summary>
        public bool Open => _progress != null && _progress.Pearls.ThreadedCount >= PearlLore.Count;

        public override Vector2 InteriorSize => new Vector2(InteriorWidth, InteriorHeight);

        protected override Rect FloorArea => new Rect(0.3f, 0.15f, InteriorWidth - 0.6f, 6.4f);

        /// <summary>Le joueur se tient au bord de la flaque (dans la grotte).</summary>
        public bool PlayerAtPool
        {
            get
            {
                if (!IsPlayerInside || Player == null) return false;
                Vector2 local = (Vector2)Player.transform.position - InteriorOrigin - _poolCenter;
                float d = Mathf.Sqrt(Mathf.Pow(local.x / _poolRadii.x, 2f) + Mathf.Pow(local.y / _poolRadii.y, 2f));
                return d < 1.5f;
            }
        }

        private void Awake() => Current = this;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        protected override void Start()
        {
            base.Start();
            _renderer = GetComponent<SpriteRenderer>();
            _progress = FindFirstObjectByType<PlayerProgress>();
            _fishing = FindFirstObjectByType<Fishing.FishingController>();
        }

        protected override void SetupInterior(WorldMap map)
        {
            foreach (Rect item in _obstacles)
            {
                map.AddObstacle(new Rect(InteriorOrigin + item.position, item.size));
            }
        }

        protected override void Update()
        {
            bool open = Open;
            if (_renderer != null)
            {
                Sprite wanted = open ? _lit : _dark;
                if (wanted != null && _renderer.sprite != wanted) _renderer.sprite = wanted;
            }

            if (_hintTimer > 0f) _hintTimer -= Time.deltaTime;
            UpdateChiyo();
            if (open || IsPlayerInside)
            {
                base.Update();
                return;
            }

            // Fermée : on pousse l'entrée, rien ne se passe, sauf une petite phrase.
            if (Player == null || Player.Locked) return;
            Vector2 feet = Player.transform.position;
            bool atDoor = Mathf.Abs(feet.x - Door.x) < 0.9f && feet.y < Door.y && feet.y > Door.y - 0.8f;
            if (atDoor && Player.LastInput.y > 0.5f) _hintTimer = 2.5f;
        }
    }
}
