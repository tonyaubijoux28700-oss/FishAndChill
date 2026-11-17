using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FishAndChill.World
{
    /// <summary>
    /// Le phare de l'île : la nuit, sa lanterne s'allume (lumière 2D chaude) et un long faisceau tourne lentement
    /// au-dessus de la mer. Allumage et extinction comme les lampadaires (<see cref="DayCycleMath.LampLevel"/>).
    /// </summary>
    public sealed class Lighthouse : MonoBehaviour
    {
        [SerializeField] private DayNightCycle _cycle;
        [SerializeField, Tooltip("Centre de la lanterne, par rapport au pied du phare.")]
        private Vector2 _lanternOffset = new Vector2(0f, 4.5f);
        [SerializeField, Tooltip("Matériau qui brille dans le noir (Sprite-Unlit-Default).")]
        private Material _glowMaterial;
        [SerializeField, Min(1f)] private float _degreesPerSecond = 36f;
        [SerializeField, Min(1f)] private float _beamLength = 11f;

        private Light2D _light;
        private SpriteRenderer _beam;
        private SpriteRenderer _beamBack;
        private float _angle;

        private void Start()
        {
            if (_cycle == null)
            {
                _cycle = FindAnyObjectByType<DayNightCycle>();
            }

            var lantern = new GameObject("Lanterne");
            lantern.transform.SetParent(transform, false);
            lantern.transform.localPosition = _lanternOffset;
            _light = lantern.AddComponent<Light2D>();
            _light.lightType = Light2D.LightType.Point;
            _light.color = new Color(1f, 0.88f, 0.55f);
            _light.pointLightInnerRadius = 0.6f;
            _light.pointLightOuterRadius = 5.5f;
            _light.falloffIntensity = 0.6f;
            _light.intensity = 0f;

            Sprite beamSprite = BeamSprite(_beamLength);
            _beam = MakeBeam(lantern.transform, beamSprite, "Faisceau");
            _beamBack = MakeBeam(lantern.transform, beamSprite, "Faisceau opposé");
        }

        private SpriteRenderer MakeBeam(Transform parent, Sprite sprite, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 30;
            if (_glowMaterial != null)
            {
                renderer.sharedMaterial = _glowMaterial;
            }

            renderer.enabled = false;
            return renderer;
        }

        private void Update()
        {
            float level = _cycle != null && InteriorBuilding.Occupied == null ? DayCycleMath.LampLevel(_cycle.Hour) : 0f;
            _light.intensity = level * (1.3f + Mathf.Sin(Time.time * 6f) * 0.05f);
            _light.enabled = level > 0.001f;

            _angle = Mathf.Repeat(_angle + _degreesPerSecond * Time.deltaTime, 360f);
            foreach ((SpriteRenderer beam, float offset) in new[] { (_beam, 0f), (_beamBack, 180f) })
            {
                beam.enabled = level > 0.001f;
                if (!beam.enabled) continue;
                float a = _angle + offset;
                beam.transform.localRotation = Quaternion.Euler(0f, 0f, a);
                // Vu de dessus en biais : le faisceau paraît plus court quand il pointe vers le haut ou le bas.
                float squash = Mathf.Lerp(0.55f, 1f, Mathf.Abs(Mathf.Cos(a * Mathf.Deg2Rad)));
                beam.transform.localScale = new Vector3(squash, 1f, 1f);
                beam.color = new Color(1f, 0.95f, 0.70f, 0.32f * level);
            }
        }

        /// <summary>Cône de lumière qui s'élargit et s'efface vers le bout (pivot à la lanterne).</summary>
        private static Sprite BeamSprite(float length)
        {
            const int w = 96, h = 24;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)(w - 1);
                float half = Mathf.Lerp(1.5f, h * 0.5f, t);
                for (int y = 0; y < h; y++)
                {
                    float d = Mathf.Abs(y + 0.5f - h * 0.5f) / half;
                    float a = d > 1f ? 0f : (1f - d * d) * (1f - t) * Mathf.Clamp01(x / 4f);
                    a = Mathf.Round(a * 5f) / 5f;   // paliers nets, façon pixel art
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), w / length);
        }
    }
}
