using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// La vie du laboratoire, purement décorative : dans l'aquarium d'étude, des poissons qui nagent d'un bout à l'autre
    /// (queue qui bat, demi-tour), des algues qui ondulent et une colonne de bulles ; la lueur turquoise du socle qui
    /// pulse ; la scientifique qui se penche de temps en temps sur ses papiers. Ordres : intérieur -5, algues du fond et
    /// poissons -4, algues de devant et bulles -3.
    /// </summary>
    public sealed class LabLife : MonoBehaviour
    {
        [System.Serializable]
        public sealed class FishLook
        {
            [Tooltip("2 images : queue d'un côté, puis de l'autre. Tête à droite.")] public Sprite[] Frames;
            [Min(1)] public int Count = 1;
            public Vector2 Speed = new Vector2(0.35f, 0.6f);
        }

        [SerializeField] private LabBuilding _lab;
        [SerializeField, Tooltip("Eau de l'aquarium d'étude, relative au coin de la pièce (fond de sable compris).")]
        private Rect _water = new Rect(0.47f, 5.03f, 2.56f, 2.16f);
        [SerializeField, Tooltip("Hauteur du sable au-dessus du bas de l'eau.")] private float _sand = 0.28f;

        [SerializeField] private List<FishLook> _fish = new List<FishLook>();
        [SerializeField, Tooltip("Algue (3 images qui ondulent).")] private Sprite[] _weed = new Sprite[0];
        [SerializeField, Min(0)] private int _weeds = 3;
        [SerializeField] private Sprite _bubble;

        [SerializeField, Tooltip("Lueur posée sur le socle, qui pulse.")] private SpriteRenderer _glow;
        [SerializeField, Tooltip("La scientifique : elle se penche sur ses papiers de temps en temps.")] private Transform _scientist;

        private sealed class Swimmer
        {
            public SpriteRenderer Renderer;
            public Sprite[] Frames;
            public Vector2 Position;
            public Vector2 Target;
            public float Speed;
            public float Phase;
        }

        private sealed class Weed
        {
            public SpriteRenderer Renderer;
            public float Phase;
            public float Speed;
        }

        private sealed class Bubble
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public float Speed;
            public float Phase;
        }

        private readonly List<Swimmer> _swimmers = new List<Swimmer>();
        private readonly List<Weed> _weedViews = new List<Weed>();
        private readonly List<Bubble> _bubbles = new List<Bubble>();
        private System.Random _random;
        private float _bubbleTimer;
        private Vector3 _scientistHome;
        private float _scientistTimer;
        private bool _leaning;

        private Rect Water => new Rect(_lab.InteriorOrigin + _water.position, _water.size);

        /// <summary>Là où les poissons nagent : l'eau sans le sable, avec une marge.</summary>
        private Rect SwimArea
        {
            get
            {
                Rect w = Water;
                return Rect.MinMaxRect(w.xMin + 0.3f, w.yMin + _sand + 0.2f, w.xMax - 0.3f, w.yMax - 0.25f);
            }
        }

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        private Vector2 RandomPoint(Rect r) => new Vector2(Range(r.xMin, r.xMax), Range(r.yMin, r.yMax));

        private SpriteRenderer Make(string name, Sprite sprite, int order, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void Start()
        {
            if (_lab == null)
            {
                enabled = false;
                return;
            }

            _random = new System.Random(7);
            Rect water = Water;
            Rect swim = SwimArea;

            // Algues posées sur le sable, réparties sur la largeur ; une sur deux devant les poissons.
            for (int i = 0; i < _weeds && _weed.Length > 0; i++)
            {
                float x = Mathf.Lerp(water.xMin + 0.2f, water.xMax - 0.2f, (i + Range(0.2f, 0.8f)) / _weeds);
                SpriteRenderer r = Make($"Algue {i + 1}", _weed[0], i % 2 == 0 ? -4 : -3, new Vector2(x, water.yMin + _sand * 0.6f));
                _weedViews.Add(new Weed { Renderer = r, Phase = Range(0f, 6.3f), Speed = Range(1.2f, 1.9f) });
            }

            foreach (FishLook look in _fish)
            {
                for (int i = 0; i < look.Count && look.Frames != null && look.Frames.Length > 0; i++)
                {
                    Vector2 start = RandomPoint(swim);
                    SpriteRenderer r = Make("Poisson", look.Frames[0], -4, start);
                    _swimmers.Add(new Swimmer
                    {
                        Renderer = r, Frames = look.Frames, Position = start, Target = RandomPoint(swim),
                        Speed = Range(look.Speed.x, look.Speed.y), Phase = Range(0f, 6.3f),
                    });
                }
            }

            if (_scientist != null)
            {
                _scientistHome = _scientist.position;
                _scientistTimer = Range(2f, 4f);
            }
        }

        private void Update()
        {
            // Rien à animer tant que personne n'est dans le laboratoire.
            if (!_lab.IsPlayerInside)
            {
                return;
            }

            float dt = Time.deltaTime;
            float t = Time.time;
            Rect water = Water;
            Rect swim = SwimArea;

            foreach (Weed weed in _weedViews)
            {
                float wave = Mathf.Sin(t * weed.Speed + weed.Phase);
                int frame = wave < -0.35f ? 0 : wave > 0.35f ? 2 : 1;
                weed.Renderer.sprite = _weed[Mathf.Min(frame, _weed.Length - 1)];
            }

            foreach (Swimmer fish in _swimmers)
            {
                Vector2 toTarget = fish.Target - fish.Position;
                if (toTarget.magnitude < 0.08f)
                {
                    // Nouveau but, plutôt à l'horizontale ; parfois une petite pause (but tout proche).
                    fish.Target = new Vector2(Range(swim.xMin, swim.xMax), Mathf.Clamp(fish.Position.y + Range(-0.5f, 0.5f), swim.yMin, swim.yMax));
                    toTarget = fish.Target - fish.Position;
                }

                Vector2 step = toTarget.normalized * Mathf.Min(toTarget.magnitude, fish.Speed * dt);
                fish.Position += step;
                if (Mathf.Abs(step.x) > 0.0005f)
                {
                    fish.Renderer.flipX = step.x < 0f;   // les dessins regardent à droite
                }

                float bob = Mathf.Sin(t * 2.1f + fish.Phase) * 0.03f;
                fish.Renderer.transform.position = Snap(fish.Position + new Vector2(0f, bob));
                int beat = (int)((t * (3f + fish.Speed * 4f) + fish.Phase) % 2f);
                fish.Renderer.sprite = fish.Frames[Mathf.Min(beat, fish.Frames.Length - 1)];
            }

            // Bulles de la pompe, à droite de l'aquarium : elles montent en zigzag et éclatent à la surface.
            _bubbleTimer -= dt;
            if (_bubble != null && _bubbleTimer <= 0f)
            {
                _bubbleTimer = Range(0.25f, 0.7f);
                Bubble bubble = _bubbles.Find(b => !b.Renderer.enabled);
                if (bubble == null)
                {
                    bubble = new Bubble { Renderer = Make("Bulle", _bubble, -3, Vector2.zero) };
                    _bubbles.Add(bubble);
                }

                bubble.Position = new Vector2(water.xMax - 0.3f + Range(-0.06f, 0.06f), water.yMin + _sand);
                bubble.Speed = Range(0.55f, 0.85f);
                bubble.Phase = Range(0f, 6.3f);
                bubble.Renderer.enabled = true;
            }

            foreach (Bubble bubble in _bubbles)
            {
                if (!bubble.Renderer.enabled)
                {
                    continue;
                }

                bubble.Position.y += bubble.Speed * dt;
                if (bubble.Position.y > water.yMax - 0.08f)
                {
                    bubble.Renderer.enabled = false;
                    continue;
                }

                float wobble = Mathf.Sin(t * 6f + bubble.Phase) * 0.05f;
                bubble.Renderer.transform.position = Snap(bubble.Position + new Vector2(wobble, 0f));
            }

            if (_glow != null)
            {
                Color c = _glow.color;
                c.a = 0.45f + 0.35f * Mathf.Sin(t * 1.8f);
                _glow.color = c;
            }

            UpdateScientist(dt);
        }

        /// <summary>La scientifique se penche un instant sur ses papiers (un pixel plus bas), puis se redresse.</summary>
        private void UpdateScientist(float dt)
        {
            if (_scientist == null)
            {
                return;
            }

            _scientistTimer -= dt;
            if (_scientistTimer > 0f)
            {
                return;
            }

            _leaning = !_leaning;
            _scientistTimer = _leaning ? Range(0.8f, 1.6f) : Range(2.5f, 5f);
            _scientist.position = _scientistHome + (_leaning ? new Vector3(0f, -1f / 16f, 0f) : Vector3.zero);
        }

        /// <summary>Cale une position sur la grille des pixels (32 par case) pour éviter le flou.</summary>
        private static Vector3 Snap(Vector2 p) => new Vector3(Mathf.Round(p.x * 32f) / 32f, Mathf.Round(p.y * 32f) / 32f, 0f);
    }
}
