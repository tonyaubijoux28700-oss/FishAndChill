using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Vie de la salle des spécimens : les voyants des tableaux clignotent chacun à son rythme, et les écrans verts
    /// défilent. Rien ne bouge tant que le joueur n'est pas dans la salle.
    /// </summary>
    public sealed class SpecimenRoomLife : MonoBehaviour
    {
        [SerializeField, Tooltip("Lueurs posées sur les voyants des tableaux.")] private SpriteRenderer[] _lamps = new SpriteRenderer[0];
        [SerializeField, Tooltip("Lueurs posées sur les petits écrans verts.")] private SpriteRenderer[] _screens = new SpriteRenderer[0];

        private float[] _rate;
        private float[] _offset;

        private void Start()
        {
            _rate = new float[_lamps.Length];
            _offset = new float[_lamps.Length];
            for (int i = 0; i < _lamps.Length; i++)
            {
                _rate[i] = Random.Range(0.6f, 2.4f);
                _offset[i] = Random.value * 10f;
            }
        }

        private void Update()
        {
            bool watched = InteriorBuilding.Occupied is LabBuilding lab && lab.InSpecimenRoom;
            for (int i = 0; i < _lamps.Length; i++)
            {
                if (_lamps[i] == null) continue;
                _lamps[i].enabled = watched && Mathf.Repeat((Time.time + _offset[i]) * _rate[i], 1f) < 0.55f;
            }

            for (int i = 0; i < _screens.Length; i++)
            {
                if (_screens[i] == null) continue;
                _screens[i].enabled = watched;
                // Une ligne lumineuse qui balaie l'écran de haut en bas.
                float k = Mathf.Repeat(Time.time * 0.8f + i * 0.37f, 1f);
                _screens[i].transform.localPosition = new Vector3(0f, (0.5f - k) * 0.12f, 0f);
            }
        }
    }
}
