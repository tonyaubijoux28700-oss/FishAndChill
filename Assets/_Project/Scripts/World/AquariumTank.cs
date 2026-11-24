using System.Collections.Generic;
using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Les pensionnaires du bassin : une miniature animée par espèce déposée (<see cref="FishSpecies.MiniFrames"/>, déjà à
    /// sa taille d'affichage, 32 px par case). Les poissons nagent d'un bord à l'autre en battant de la queue, font des
    /// demi-tours en douceur (le dessin se rétrécit puis se retourne), accélèrent parfois d'un coup de queue et changent
    /// de profondeur ; les petites espèces nagent en banc (quelques copies qui suivent la première). Chacun vit sur un
    /// des trois plans du bassin (<see cref="AquariumMath.PlaneTint"/> : celui du fond est plus sombre et plus bleu).
    /// Les crustacés, reptiles et amphibiens coulent jusqu'au sable, y marchent en bougeant les pattes et s'arrêtent
    /// de temps en temps. Un nouveau venu arrive par la surface.
    /// </summary>
    public sealed class AquariumTank : MonoBehaviour
    {
        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private AquariumBuilding _building;

        [SerializeField, Tooltip("Zone dont les espèces vivent dans ce bassin (vide = toutes).")]
        private FishingZone _zone;

        [SerializeField, Tooltip("Eau du bassin, relative au coin de la salle.")]
        private Rect _area = new Rect(2.4f, 12.4f, 25.2f, 4.4f);

        [SerializeField, Tooltip("Ordre d'affichage des poissons : entre le fond du bassin et la vitre.")]
        private int _sortingOrder = -4;

        [SerializeField, Min(0.1f)] private float _minSpeed = 0.6f;
        [SerializeField, Min(0.1f)] private float _maxSpeed = 1.6f;

        [SerializeField, Tooltip("Couleur de l'eau, vers laquelle tirent les poissons du fond du bassin.")]
        private Color _water = new Color(0.10f, 0.30f, 0.44f);

        private const float TurnSeconds = 0.35f;
        private const float FramesPerSecond = 6f;

        private sealed class Swimmer
        {
            public CaughtFish Specimen;
            public SpriteRenderer Renderer;
            public Sprite[] Frames;
            public bool OnBottom;      // crustacés, reptiles, amphibiens : au fond, ils marchent sans nager
            public int Plane;          // 0 fond, 1 milieu, 2 devant
            public float Direction;
            public float Facing;       // sens du dessin, qui suit Direction au bout du demi-tour
            public float TurnTimer;
            public float Speed;
            public float Burst;        // coup de queue : vitesse en plus, qui retombe
            public float Depth;
            public float TargetDepth;
            public float Phase;
            public float Clock;
            public float NextTurn;
            public float NextBurst;
            public float PauseTimer;   // ceux du fond s'arrêtent de temps en temps
            public Vector2 Position;   // position exacte (le sprite, lui, est calé au pixel)
            public Swimmer Leader;     // banc : la copie suit ce poisson
            public Vector2 Offset;     // place dans le banc, par rapport au premier
        }

        private readonly Dictionary<string, Swimmer> _swimmers = new Dictionary<string, Swimmer>();
        private readonly List<Swimmer> _all = new List<Swimmer>();
        private System.Random _random = new System.Random(7);

        public int SwimmerCount => _swimmers.Count;

        private readonly List<CaughtFish> _residents = new List<CaughtFish>();

        /// <summary>Les espèces du bassin, dans l'ordre d'arrivée (une par espèce, sans les copies des bancs).</summary>
        public IReadOnlyList<CaughtFish> Residents => _residents;

        /// <summary>La zone exposée dans ce bassin (null : toutes).</summary>
        public FishingZone Zone => _zone;

        /// <summary>Eau du bassin, en coordonnées du monde.</summary>
        public Rect WorldArea => SwimArea;

        /// <summary>Hauteur du sable sous la zone de nage (ceux du fond y posent les pattes).</summary>
        private const float SandBelowSwimArea = 0.3f;

        private void Start()
        {
            if (_progress == null || _building == null)
            {
                enabled = false;
                return;
            }

            _progress.EnsureCreated();
            _progress.Aquarium.Changed += Sync;
            Sync();
        }

        private void OnDestroy()
        {
            if (_progress != null && _progress.Aquarium != null)
            {
                _progress.Aquarium.Changed -= Sync;
            }
        }

        /// <summary>
        /// Le pensionnaire sous ce point du monde (le plus en avant s'ils se chevauchent), ou null : pour la fiche de
        /// la vue de près.
        /// </summary>
        public CaughtFish SpecimenAt(Vector2 world)
        {
            Swimmer best = null;
            foreach (Swimmer s in _all)
            {
                if (s.Renderer.sprite == null || !s.Renderer.bounds.Contains(new Vector3(world.x, world.y, s.Renderer.bounds.center.z))) continue;
                if (best == null || s.Plane > best.Plane) best = s;
            }

            return best != null ? best.Specimen : null;
        }

        /// <summary>Où est ce pensionnaire (le premier du banc), pour l'encadrer dans la vue de près.</summary>
        public bool BoundsOf(CaughtFish specimen, out Bounds bounds)
        {
            bounds = default;
            if (specimen == null || !_swimmers.TryGetValue(specimen.SpeciesId, out Swimmer s)) return false;
            bounds = s.Renderer.bounds;
            return true;
        }

        /// <summary>Ajoute un pensionnaire (et son banc) pour chaque espèce arrivée dans l'aquarium.</summary>
        private void Sync()
        {
            foreach (CaughtFish specimen in _progress.Aquarium.Specimens)
            {
                if (specimen.Species == null || _swimmers.ContainsKey(specimen.SpeciesId) || !LivesHere(specimen.Species))
                {
                    continue;
                }

                Swimmer leader = CreateSwimmer(specimen, null, Vector2.zero);
                _swimmers.Add(specimen.SpeciesId, leader);
                _residents.Add(specimen);
                int school = AquariumMath.SchoolSize(leader.Renderer.sprite != null ? leader.Renderer.sprite.bounds.size.x : 1f, leader.OnBottom);
                for (int i = 1; i < school; i++)
                {
                    var offset = new Vector2(-0.35f * i - Range(0f, 0.25f), Range(-0.35f, 0.35f));
                    CreateSwimmer(specimen, leader, offset);
                }
            }
        }

        private Rect SwimArea => _building.WorldRect(_area);

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        private bool LivesHere(FishSpecies species)
        {
            if (_zone == null)
            {
                return true;
            }

            foreach (FishingZone.SpeciesEntry entry in _zone.Species)
            {
                if (entry.Species == species)
                {
                    return true;
                }
            }

            return false;
        }

        private Swimmer CreateSwimmer(CaughtFish specimen, Swimmer leader, Vector2 offset)
        {
            FishSpecies species = specimen.Species;
            Rect area = SwimArea;
            bool onBottom = species.Kind.LivesOnBottom();
            var go = new GameObject($"{species.Kind.DisplayName()} - {species.CommonName}");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            Sprite[] frames = species.MiniFrames != null && species.MiniFrames.Length > 0 ? species.MiniFrames : new[] { species.MiniSprite };
            renderer.sprite = frames[0];
            renderer.sortingOrder = _sortingOrder;

            int plane = leader != null ? leader.Plane : onBottom ? 2 : _random.Next(3);
            renderer.color = AquariumMath.PlaneTint(plane, _water);

            float direction = leader != null ? leader.Direction : _random.NextDouble() < 0.5 ? -1f : 1f;
            var swimmer = new Swimmer
            {
                Specimen = specimen,
                Renderer = renderer,
                Frames = frames,
                OnBottom = onBottom,
                Plane = plane,
                Direction = direction,
                Facing = direction,
                Speed = onBottom ? Range(0.15f, 0.35f) : Range(_minSpeed, _maxSpeed) * AquariumMath.PlaneSpeed(plane),
                Depth = 1f,                                   // arrive par la surface (et coule au fond)
                TargetDepth = onBottom ? 0f : AquariumMath.DepthInPlane(plane, (float)_random.NextDouble()),
                Phase = Range(0f, 10f),
                Clock = Range(0f, 1f),
                NextTurn = Range(3f, 9f),
                NextBurst = Range(4f, 12f),
                Leader = leader,
                Offset = offset,
            };

            float x = leader != null ? leader.Position.x + offset.x : Mathf.Lerp(area.xMin + 1f, area.xMax - 1f, (float)_random.NextDouble());
            swimmer.Position = new Vector2(x, area.yMax);
            go.transform.position = swimmer.Position;
            _all.Add(swimmer);
            return swimmer;
        }

        private void Update()
        {
            Rect area = SwimArea;
            float dt = Time.deltaTime;
            foreach (Swimmer swimmer in _all)
            {
                if (swimmer.Leader != null) Follow(swimmer, area, dt);
                else Swim(swimmer, area, dt);
                Draw(swimmer, dt);
            }
        }

        private void Swim(Swimmer swimmer, Rect area, float dt)
        {
            float halfWidth = swimmer.Renderer.sprite != null ? swimmer.Renderer.sprite.bounds.extents.x : 0.5f;
            float halfHeight = swimmer.Renderer.sprite != null ? swimmer.Renderer.sprite.bounds.extents.y : 0.3f;
            Vector2 position = swimmer.Position;

            // Ceux du fond ne bougent qu'une fois posés, et font des pauses.
            bool settled = !swimmer.OnBottom || swimmer.Depth <= 0f;
            if (swimmer.OnBottom && settled)
            {
                swimmer.PauseTimer -= dt;
            }

            bool walking = settled && (!swimmer.OnBottom || swimmer.PauseTimer <= 0f);
            swimmer.Renderer.enabled = true;
            if (walking)
            {
                // Coup de queue de temps en temps : il file, puis ralentit.
                swimmer.NextBurst -= dt;
                if (!swimmer.OnBottom && swimmer.NextBurst <= 0f)
                {
                    swimmer.Burst = swimmer.Speed * 1.8f;
                    swimmer.NextBurst = Range(5f, 14f);
                }

                swimmer.Burst = Mathf.MoveTowards(swimmer.Burst, 0f, dt * swimmer.Speed * 1.6f);

                // Demi-tour aux vitres, et parfois sans raison.
                swimmer.NextTurn -= dt;
                float speed = swimmer.TurnTimer > 0f ? swimmer.Speed * 0.3f : swimmer.Speed + swimmer.Burst;
                position.x += swimmer.Direction * speed * dt;
                if (position.x > area.xMax - halfWidth || position.x < area.xMin + halfWidth || swimmer.NextTurn <= 0f)
                {
                    float before = swimmer.Direction;
                    swimmer.Direction = position.x > area.center.x ? -1f : 1f;
                    if (swimmer.NextTurn <= 0f)
                    {
                        swimmer.Direction = -before;
                    }

                    if (swimmer.Direction != before) swimmer.TurnTimer = TurnSeconds;
                    swimmer.NextTurn = Range(4f, 10f);
                    if (swimmer.OnBottom)
                    {
                        swimmer.PauseTimer = Range(1f, 4f);   // un temps d'arrêt
                    }
                    else
                    {
                        swimmer.TargetDepth = AquariumMath.DepthInPlane(swimmer.Plane, (float)_random.NextDouble());
                    }

                    position.x = Mathf.Clamp(position.x, area.xMin + halfWidth, area.xMax - halfWidth);
                }
            }

            // Profondeur : les poissons ondulent ; ceux du fond coulent jusqu'au sable puis y restent posés.
            swimmer.Depth = Mathf.MoveTowards(swimmer.Depth, swimmer.TargetDepth, dt * (swimmer.OnBottom ? 0.6f : 0.2f));
            swimmer.Phase += dt * (1.5f + swimmer.Speed);
            float yMin = area.yMin + halfHeight - (swimmer.OnBottom ? SandBelowSwimArea : 0f);
            float yMax = area.yMax - halfHeight;
            float wobble = swimmer.OnBottom ? 0f : Mathf.Sin(swimmer.Phase) * 0.06f;
            position.y = Mathf.Lerp(yMin, yMax, swimmer.Depth) + wobble;
            swimmer.Position = position;
            swimmer.Clock += dt * (swimmer.OnBottom ? (walking ? 1f : 0f) : 1f + swimmer.Burst * 1.5f);
        }

        /// <summary>Une copie du banc : elle tient sa place derrière le premier, avec un léger flottement.</summary>
        private void Follow(Swimmer swimmer, Rect area, float dt)
        {
            Swimmer leader = swimmer.Leader;
            swimmer.Phase += dt * 2f;
            var slot = new Vector2(swimmer.Offset.x * leader.Direction, swimmer.Offset.y + Mathf.Sin(swimmer.Phase) * 0.08f);
            Vector2 target = leader.Position + slot;
            target.x = Mathf.Clamp(target.x, area.xMin + 0.3f, area.xMax - 0.3f);
            target.y = Mathf.Clamp(target.y, area.yMin + 0.2f, area.yMax - 0.2f);
            Vector2 before = swimmer.Position;
            swimmer.Position = Vector2.Lerp(swimmer.Position, target, 1f - Mathf.Exp(-dt * 3f));
            float dx = swimmer.Position.x - before.x;
            if (Mathf.Abs(dx) > 0.0005f && Mathf.Sign(dx) != swimmer.Direction)
            {
                swimmer.Direction = Mathf.Sign(dx);
                swimmer.TurnTimer = TurnSeconds;
            }

            swimmer.Depth = leader.Depth;
            swimmer.Clock += dt * (1f + leader.Burst * 1.5f);
        }

        private void Draw(Swimmer swimmer, float dt)
        {
            // Demi-tour en douceur : le dessin se rétrécit, se retourne à mi-chemin, puis s'élargit.
            float squash = 1f;
            if (swimmer.TurnTimer > 0f)
            {
                swimmer.TurnTimer -= dt;
                float k = 1f - Mathf.Clamp01(swimmer.TurnTimer / TurnSeconds);
                squash = Mathf.Max(0.15f, Mathf.Abs(Mathf.Cos(k * Mathf.PI)));
                if (k >= 0.5f) swimmer.Facing = swimmer.Direction;
            }
            else
            {
                swimmer.Facing = swimmer.Direction;
            }

            int frame = swimmer.Frames.Length > 1 ? (int)(swimmer.Clock * FramesPerSecond) % swimmer.Frames.Length : 0;
            swimmer.Renderer.sprite = swimmer.Frames[frame];
            // Le dessin regarde vers la droite : on le retourne quand il va vers la gauche.
            swimmer.Renderer.flipX = swimmer.Facing < 0f;
            Transform t = swimmer.Renderer.transform;
            t.localScale = new Vector3(squash, 1f, 1f);

            // Pixels nets : le sprite est calé au pixel (32 par case), la position exacte est gardée à part.
            t.position = new Vector3(Mathf.Round(swimmer.Position.x * 32f) / 32f, Mathf.Round(swimmer.Position.y * 32f) / 32f, 0f);
        }
    }
}
