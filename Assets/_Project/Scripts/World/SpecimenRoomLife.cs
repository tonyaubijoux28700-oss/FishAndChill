using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Vie de la salle des spécimens : les voyants des tableaux clignotent chacun à son rythme, les écrans verts
    /// défilent, la console affiche une courbe et un sonar qui tournent, la vapeur s'échappe des bouches d'aération et
    /// un petit poisson fait des ronds dans le bassin de quarantaine. Rien ne bouge tant que le joueur n'est pas dans la salle.
    /// </summary>
    public sealed class SpecimenRoomLife : MonoBehaviour
    {
        [SerializeField, Tooltip("Lueurs posées sur les voyants des tableaux.")] private SpriteRenderer[] _lamps = new SpriteRenderer[0];
        [SerializeField, Tooltip("Lueurs posées sur les petits écrans verts.")] private SpriteRenderer[] _screens = new SpriteRenderer[0];
        [SerializeField, Tooltip("Les deux écrans de la console (courbe puis sonar).")] private SpriteRenderer[] _consoleScreens = new SpriteRenderer[0];
        [SerializeField, Tooltip("Images de la courbe, dans l'ordre.")] private Sprite[] _waveFrames = new Sprite[0];
        [SerializeField, Tooltip("Images du sonar, dans l'ordre.")] private Sprite[] _sonarFrames = new Sprite[0];
        [SerializeField, Tooltip("Bouffées de vapeur (plusieurs par bouche).")] private SpriteRenderer[] _steam = new SpriteRenderer[0];
        [SerializeField, Tooltip("Le petit poisson du bassin.")] private SpriteRenderer _poolFish;
        [SerializeField, Tooltip("Milieu du bassin (monde).")] private Vector2 _poolCenter;
        [SerializeField, Tooltip("Demi-axes de la ronde du poisson.")] private Vector2 _poolRadius = new Vector2(1f, 0.4f);

        private const float SteamLife = 2.4f;
        private float[] _rate;
        private float[] _offset;
        private Vector3[] _steamBase;

        private void Start()
        {
            _rate = new float[_lamps.Length];
            _offset = new float[_lamps.Length];
            for (int i = 0; i < _lamps.Length; i++)
            {
                _rate[i] = Random.Range(0.6f, 2.4f);
                _offset[i] = Random.value * 10f;
            }

            _steamBase = new Vector3[_steam.Length];
            for (int i = 0; i < _steam.Length; i++)
            {
                if (_steam[i] != null) _steamBase[i] = _steam[i].transform.position;
            }
        }

        private void Update()
        {
            bool watched = InteriorBuilding.Occupied is LabBuilding lab && lab.InSpecimenRoom;
            float t = Time.time;
            for (int i = 0; i < _lamps.Length; i++)
            {
                if (_lamps[i] == null) continue;
                _lamps[i].enabled = watched && Mathf.Repeat((t + _offset[i]) * _rate[i], 1f) < 0.55f;
            }

            for (int i = 0; i < _screens.Length; i++)
            {
                if (_screens[i] == null) continue;
                _screens[i].enabled = watched;
                // Une ligne lumineuse qui balaie l'écran de haut en bas.
                float k = Mathf.Repeat(t * 0.8f + i * 0.37f, 1f);
                _screens[i].transform.localPosition = new Vector3(0f, (0.5f - k) * 0.12f, 0f);
            }

            if (!watched)
            {
                return;
            }

            for (int i = 0; i < _consoleScreens.Length; i++)
            {
                Sprite[] frames = i == 0 ? _waveFrames : _sonarFrames;
                if (_consoleScreens[i] == null || frames.Length == 0) continue;
                _consoleScreens[i].sprite = frames[(int)(t * (i == 0 ? 6f : 4f)) % frames.Length];
            }

            // La vapeur monte en gonflant, puis s'efface ; chaque bouffée a son retard.
            for (int i = 0; i < _steam.Length; i++)
            {
                if (_steam[i] == null) continue;
                float k = Mathf.Repeat(t / SteamLife + i * 0.37f, 1f);
                Transform s = _steam[i].transform;
                s.position = _steamBase[i] + new Vector3(Mathf.Sin(t * 1.5f + i) * 0.12f * k, k * 1.4f, 0f);
                float size = 0.6f + k * 0.9f;
                s.localScale = new Vector3(size, size, 1f);
                Color c = _steam[i].color;
                c.a = Mathf.Sin(k * Mathf.PI) * 0.8f;
                _steam[i].color = c;
            }

            if (_poolFish != null)
            {
                float a = t * 0.7f;
                _poolFish.transform.position = _poolCenter + new Vector2(Mathf.Cos(a) * _poolRadius.x, Mathf.Sin(a) * _poolRadius.y);
                _poolFish.flipX = Mathf.Sin(a) > 0f;
            }
        }
    }
}
