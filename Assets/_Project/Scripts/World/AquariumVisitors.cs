using System.Collections.Generic;
using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Les visiteurs de l'aquarium : ils se promènent devant les bassins, s'arrêtent pour regarder les poissons
    /// (de dos, face à la vitre), puis repartent un peu plus loin. Plus la collection est grande, plus il y a de monde
    /// (1 visiteur par salle au début, jusqu'à 3). Purement décoratif : ils ne bloquent pas le joueur.
    /// </summary>
    public sealed class AquariumVisitors : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Look
        {
            public Sprite[] Down;
            public Sprite[] Up;
            public Sprite[] Left;
            public Sprite[] Right;
        }

        [System.Serializable]
        public sealed class Room
        {
            [Tooltip("Bord gauche et bord droit de la promenade, relatifs au coin de la salle.")] public Vector2 Span;
            [Tooltip("Hauteur de la promenade devant le bassin, relative au coin de la salle.")] public float Lane = 8.9f;
        }

        [SerializeField] private AquariumBuilding _building;
        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private List<Look> _looks = new List<Look>();
        [SerializeField] private List<Room> _rooms = new List<Room>();
        [SerializeField, Min(1)] private int _maxPerRoom = 3;
        [SerializeField, Min(1), Tooltip("Espèces exposées pour un visiteur de plus dans chaque salle.")] private int _speciesPerVisitor = 6;

        private sealed class Visitor
        {
            public SpriteRenderer Renderer;
            public Look Look;
            public Room Room;
            public Vector2 Position;
            public float TargetX;
            public float Wait;
            public float Speed;
            public float Step;
        }

        private readonly List<Visitor> _visitors = new List<Visitor>();
        private readonly System.Random _random = new System.Random(4321);

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        private void Start()
        {
            if (_building == null || _looks.Count == 0 || _rooms.Count == 0)
            {
                enabled = false;
                return;
            }

            if (_progress != null)
            {
                _progress.EnsureCreated();
                _progress.Aquarium.Changed += Sync;
            }

            Sync();
        }

        private void OnDestroy()
        {
            if (_progress != null && _progress.Aquarium != null)
            {
                _progress.Aquarium.Changed -= Sync;
            }
        }

        /// <summary>Ajoute des visiteurs quand la collection grandit.</summary>
        private void Sync()
        {
            int species = _progress != null && _progress.Aquarium != null ? _progress.Aquarium.SpeciesCount : 0;
            int perRoom = Mathf.Clamp(1 + species / _speciesPerVisitor, 1, _maxPerRoom);
            foreach (Room room in _rooms)
            {
                int count = 0;
                foreach (Visitor v in _visitors)
                {
                    if (v.Room == room) count++;
                }

                for (; count < perRoom; count++)
                {
                    _visitors.Add(Spawn(room));
                }
            }
        }

        private Visitor Spawn(Room room)
        {
            Look look = _looks[_visitors.Count % _looks.Count];
            var go = new GameObject("Visiteur");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 10;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            var visitor = new Visitor
            {
                Renderer = renderer,
                Look = look,
                Room = room,
                Position = _building.InteriorOrigin + new Vector2(Range(room.Span.x, room.Span.y), room.Lane + Range(-0.5f, 0.3f)),
                Speed = Range(0.9f, 1.4f),
                Wait = Range(0.5f, 4f),
            };
            visitor.TargetX = visitor.Position.x;
            renderer.sprite = look.Up[0];
            go.transform.position = visitor.Position;
            return visitor;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (Visitor v in _visitors)
            {
                float dx = v.TargetX - v.Position.x;
                if (Mathf.Abs(dx) < 0.05f)
                {
                    // Il regarde le bassin (de dos), puis choisit un autre coin de la vitre.
                    v.Renderer.sprite = v.Look.Up[0];
                    v.Wait -= dt;
                    if (v.Wait <= 0f)
                    {
                        Vector2 origin = _building.InteriorOrigin;
                        v.TargetX = origin.x + Range(v.Room.Span.x, v.Room.Span.y);
                        v.Wait = Range(3f, 8f);
                    }

                    continue;
                }

                float move = Mathf.Sign(dx) * Mathf.Min(Mathf.Abs(dx), v.Speed * dt);
                v.Position.x += move;
                v.Step += dt * 7f;
                Sprite[] frames = dx > 0f ? v.Look.Right : v.Look.Left;
                int[] cycle = { 1, 0, 2, 0 };
                v.Renderer.sprite = frames[Mathf.Min(cycle[Mathf.FloorToInt(v.Step) % cycle.Length], frames.Length - 1)];
                v.Renderer.transform.position = new Vector3(Mathf.Round(v.Position.x * 16f) / 16f, Mathf.Round(v.Position.y * 16f) / 16f, 0f);
            }
        }
    }
}
