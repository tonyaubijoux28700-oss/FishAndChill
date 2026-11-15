using System.Collections.Generic;
using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Les poissons du bassin : un petit sprite par espèce déposée (<see cref="FishSpecies.MiniSprite"/>), qui nage
    /// d'un bord à l'autre en ondulant, change parfois de profondeur et se retourne aux vitres.
    /// Un nouveau venu plonge depuis la surface.
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
            public float Direction;
            public float Speed;
            public float Depth;
            public float TargetDepth;
            public float Phase;
            public float NextTurn;
            public Vector2 Position;   // position exacte (le sprite, lui, est calé au pixel)
        }

        private readonly Dictionary<string, Swimmer> _swimmers = new Dictionary<string, Swimmer>();
        private System.Random _random = new System.Random(7);

        public int SwimmerCount => _swimmers.Count;

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

        /// <summary>Ajoute un nageur pour chaque espèce arrivée dans l'aquarium.</summary>
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
            var go = new GameObject($"Poisson - {species.CommonName}");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = species.MiniSprite;
            renderer.sortingOrder = _sortingOrder;

            var swimmer = new Swimmer
            {
                Renderer = renderer,
                Direction = _random.NextDouble() < 0.5 ? -1f : 1f,
                Speed = Mathf.Lerp(_minSpeed, _maxSpeed, (float)_random.NextDouble()),
                Depth = 1f,                                   // arrive par la surface
                TargetDepth = (float)_random.NextDouble(),
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

                // Demi-tour aux vitres, et parfois sans raison (comme un vrai poisson).
                swimmer.NextTurn -= dt;
                Vector2 position = swimmer.Position;
                position.x += swimmer.Direction * swimmer.Speed * dt;
                if (position.x > area.xMax - halfWidth || position.x < area.xMin + halfWidth || swimmer.NextTurn <= 0f)
                {
                    swimmer.Direction = position.x > area.center.x ? -1f : 1f;
                    if (swimmer.NextTurn <= 0f)
                    {
                        swimmer.Direction = -swimmer.Direction;
                    }

                    swimmer.NextTurn = 4f + (float)_random.NextDouble() * 6f;
                    swimmer.TargetDepth = (float)_random.NextDouble();
                    position.x = Mathf.Clamp(position.x, area.xMin + halfWidth, area.xMax - halfWidth);
                }

                // Profondeur : glisse vers la cible, avec une petite ondulation.
                swimmer.Depth = Mathf.MoveTowards(swimmer.Depth, swimmer.TargetDepth, dt * 0.25f);
                swimmer.Phase += dt * (1.5f + swimmer.Speed);
                float yMin = area.yMin + halfHeight;
                float yMax = area.yMax - halfHeight;
                position.y = Mathf.Lerp(yMin, yMax, swimmer.Depth) + Mathf.Sin(swimmer.Phase) * 0.06f;

                // Le dessin regarde vers la droite : on le retourne quand il nage vers la gauche.
                swimmer.Renderer.flipX = swimmer.Direction < 0f;

                // Pixels nets : le sprite est calé au pixel (16 par case), la position exacte est gardée à part.
                swimmer.Position = position;
                t.position = new Vector3(Mathf.Round(position.x * 16f) / 16f, Mathf.Round(position.y * 16f) / 16f, 0f);
            }
        }
    }
}
