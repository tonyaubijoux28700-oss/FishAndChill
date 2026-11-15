using System.Collections.Generic;
using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Les pensionnaires du bassin : un petit sprite par espèce déposée (<see cref="FishSpecies.MiniSprite"/>).
    /// Les poissons nagent d'un bord à l'autre en ondulant, changent de profondeur et se retournent aux vitres.
    /// Les crustacés, reptiles et amphibiens ne nagent pas : ils coulent jusqu'au sable, y marchent lentement et
    /// s'arrêtent de temps en temps. Un nouveau venu arrive par la surface.
    /// </summary>
    public sealed class AquariumTank : MonoBehaviour
    {
        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private AquariumBuilding _building;

        [SerializeField, Tooltip("Ordre d'affichage des poissons : entre le fond du bassin et la vitre.")]
        private int _sortingOrder = -4;

        [SerializeField, Min(0.1f)] private float _minSpeed = 0.6f;
        [SerializeField, Min(0.1f)] private float _maxSpeed = 1.6f;

        private sealed class Swimmer
        {
            public SpriteRenderer Renderer;
            public bool OnBottom;      // crustacés, reptiles, amphibiens : au fond, ils marchent sans nager
            public float Direction;
            public float Speed;
            public float Depth;
            public float TargetDepth;
            public float Phase;
            public float NextTurn;
            public float PauseTimer;   // ceux du fond s'arrêtent de temps en temps
            public Vector2 Position;   // position exacte (le sprite, lui, est calé au pixel)
        }

        private readonly Dictionary<string, Swimmer> _swimmers = new Dictionary<string, Swimmer>();
        private System.Random _random = new System.Random(7);

        public int SwimmerCount => _swimmers.Count;

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

        /// <summary>Ajoute un pensionnaire pour chaque espèce arrivée dans l'aquarium.</summary>
        private void Sync()
        {
            foreach (CaughtFish specimen in _progress.Aquarium.Specimens)
            {
                if (specimen.Species == null || _swimmers.ContainsKey(specimen.SpeciesId))
                {
                    continue;
                }

                _swimmers.Add(specimen.SpeciesId, CreateSwimmer(specimen.Species));
            }
        }

        private Swimmer CreateSwimmer(FishSpecies species)
        {
            Rect area = _building.SwimArea;
            bool onBottom = species.Kind.LivesOnBottom();
            var go = new GameObject($"{species.Kind.DisplayName()} - {species.CommonName}");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = species.MiniSprite;
            // Ceux du fond passent devant les poissons (ils sont plus près de la vitre, sur le sable).
            renderer.sortingOrder = _sortingOrder;

            var swimmer = new Swimmer
            {
                Renderer = renderer,
                OnBottom = onBottom,
                Direction = _random.NextDouble() < 0.5 ? -1f : 1f,
                Speed = onBottom
                    ? Mathf.Lerp(0.15f, 0.35f, (float)_random.NextDouble())
                    : Mathf.Lerp(_minSpeed, _maxSpeed, (float)_random.NextDouble()),
                Depth = 1f,                                   // arrive par la surface (et coule au fond)
                TargetDepth = onBottom ? 0f : (float)_random.NextDouble(),
                Phase = (float)_random.NextDouble() * 10f,
                NextTurn = 3f + (float)_random.NextDouble() * 6f,
            };

            float x = Mathf.Lerp(area.xMin + 1f, area.xMax - 1f, (float)_random.NextDouble());
            swimmer.Position = new Vector2(x, area.yMax);
            go.transform.position = swimmer.Position;
            return swimmer;
        }

        private void Update()
        {
            Rect area = _building.SwimArea;
            float dt = Time.deltaTime;
            foreach (Swimmer swimmer in _swimmers.Values)
            {
                Transform t = swimmer.Renderer.transform;
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
                if (walking)
                {
                    // Demi-tour aux vitres, et parfois sans raison.
                    swimmer.NextTurn -= dt;
                    position.x += swimmer.Direction * swimmer.Speed * dt;
                    if (position.x > area.xMax - halfWidth || position.x < area.xMin + halfWidth || swimmer.NextTurn <= 0f)
                    {
                        swimmer.Direction = position.x > area.center.x ? -1f : 1f;
                        if (swimmer.NextTurn <= 0f)
                        {
                            swimmer.Direction = -swimmer.Direction;
                        }

                        swimmer.NextTurn = 4f + (float)_random.NextDouble() * 6f;
                        if (swimmer.OnBottom)
                        {
                            swimmer.PauseTimer = 1f + (float)_random.NextDouble() * 3f;   // un temps d'arrêt
                        }
                        else
                        {
                            swimmer.TargetDepth = (float)_random.NextDouble();
                        }

                        position.x = Mathf.Clamp(position.x, area.xMin + halfWidth, area.xMax - halfWidth);
                    }
                }

                // Profondeur : les poissons ondulent ; ceux du fond coulent jusqu'au sable puis y restent posés.
                swimmer.Depth = Mathf.MoveTowards(swimmer.Depth, swimmer.TargetDepth, dt * (swimmer.OnBottom ? 0.6f : 0.25f));
                swimmer.Phase += dt * (1.5f + swimmer.Speed);
                float yMin = area.yMin + halfHeight - (swimmer.OnBottom ? SandBelowSwimArea : 0f);
                float yMax = area.yMax - halfHeight;
                float wobble = swimmer.OnBottom ? 0f : Mathf.Sin(swimmer.Phase) * 0.06f;
                position.y = Mathf.Lerp(yMin, yMax, swimmer.Depth) + wobble;

                // Le dessin regarde vers la droite : on le retourne quand il va vers la gauche.
                swimmer.Renderer.flipX = swimmer.Direction < 0f;

                // Pixels nets : le sprite est calé au pixel (16 par case), la position exacte est gardée à part.
                swimmer.Position = position;
                t.position = new Vector3(Mathf.Round(position.x * 16f) / 16f, Mathf.Round(position.y * 16f) / 16f, 0f);
            }
        }
    }
}
