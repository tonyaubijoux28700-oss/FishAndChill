using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Un tube de la salle des spécimens du laboratoire. Une fois sa légende vaincue, la bête y flotte tête en haut et
    /// ondule doucement, des bulles montent dans le liquide qui luit et pulse ; avant, le tube reste vide, le liquide
    /// sombre et immobile. N'anime rien tant que le joueur n'est pas dans la salle.
    /// </summary>
    public sealed class SpecimenTube : MonoBehaviour
    {
        [SerializeField, Tooltip("Place du tube dans la rangée (0 = le plus à gauche).")] private int _slot;
        [SerializeField, Tooltip("La légende exposée ici (vide : tube en attente).")] private LegendFish _legend;
        [SerializeField] private PlayerProgress _progress;
        [SerializeField, Tooltip("Fond du tube (cuve, liquide, socle).")] private SpriteRenderer _back;
        [SerializeField, Tooltip("La bête, redressée tête en haut.")] private SpriteRenderer _specimen;
        [SerializeField, Tooltip("Une bulle (copiée plusieurs fois).")] private Sprite _bubble;
        [SerializeField, Tooltip("Milieu de l'intérieur du verre, relatif au pied du tube.")] private Vector2 _innerCenter = new Vector2(0f, 3f);
        [SerializeField, Tooltip("Demi-taille de l'intérieur du verre.")] private Vector2 _innerHalf = new Vector2(1.3f, 2f);

        private const int BubbleCount = 7;
        private static readonly Color Dormant = new Color(0.38f, 0.46f, 0.46f);

        private SpriteRenderer[] _bubbles;
        private float[] _bubbleSpeed;
        private float _phase;

        public int Slot => _slot;
        public LegendFish Legend => _legend;

        /// <summary>La bête est-elle exposée (légende vaincue) ?</summary>
        public bool HasSpecimen => _legend != null && _progress != null && _progress.Legends.State(_legend.Id) == LegendState.Beaten;

        /// <summary>Là où l'on se tient pour lire la plaque : devant le socle.</summary>
        public Vector2 Front => (Vector2)transform.position + new Vector2(0f, -0.6f);

        private void Start()
        {
            _phase = _slot * 1.7f;
            _bubbles = new SpriteRenderer[BubbleCount];
            _bubbleSpeed = new float[BubbleCount];
            for (int i = 0; i < BubbleCount; i++)
            {
                var go = new GameObject($"Bulle {i + 1}");
                go.transform.SetParent(transform, false);
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = _bubble;
                r.sortingOrder = (_specimen != null ? _specimen.sortingOrder : 10) + (i % 2 == 0 ? -1 : 1);
                r.sortingLayerID = _back != null ? _back.sortingLayerID : r.sortingLayerID;
                _bubbles[i] = r;
                _bubbleSpeed[i] = Random.Range(0.5f, 1.1f);
                go.transform.localPosition = RandomInside(Random.value);
            }
        }

        private Vector3 RandomInside(float height01) => new Vector3(
            _innerCenter.x + Random.Range(-_innerHalf.x, _innerHalf.x) * 0.85f,
            _innerCenter.y - _innerHalf.y + height01 * _innerHalf.y * 2f, 0f);

        private void Update()
        {
            bool shown = HasSpecimen;
            bool watched = InteriorBuilding.Occupied is LabBuilding lab && lab.InSpecimenRoom;
            if (_specimen != null) _specimen.enabled = shown;
            if (_bubbles != null)
            {
                foreach (SpriteRenderer b in _bubbles) b.enabled = shown && watched;
            }

            if (!watched)
            {
                return;
            }

            float t = Time.time + _phase;
            if (_back != null)
            {
                // Le liquide pulse lentement quand une bête y repose, reste sombre sinon.
                float pulse = 0.92f + 0.08f * Mathf.Sin(t * 1.3f);
                _back.color = shown ? new Color(pulse, pulse + 0.04f, pulse) : Dormant;
            }

            if (!shown)
            {
                return;
            }

            // La bête ondule : elle monte et descend, et tangue un peu, comme suspendue dans le liquide.
            _specimen.transform.localPosition = new Vector3(_innerCenter.x + Mathf.Sin(t * 0.7f) * 0.05f, _innerCenter.y + Mathf.Sin(t * 1.1f) * 0.12f, 0f);
            _specimen.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.5f) * 2.5f);

            for (int i = 0; i < _bubbles.Length; i++)
            {
                Transform b = _bubbles[i].transform;
                Vector3 p = b.localPosition;
                p.y += _bubbleSpeed[i] * Time.deltaTime;
                p.x += Mathf.Sin(t * 3f + i) * 0.002f;
                if (p.y > _innerCenter.y + _innerHalf.y)
                {
                    p = RandomInside(0f);
                    _bubbleSpeed[i] = Random.Range(0.5f, 1.1f);
                }

                b.localPosition = p;
                float s = 0.6f + (i % 3) * 0.2f;
                b.localScale = new Vector3(s, s, 1f);
            }
        }
    }
}
