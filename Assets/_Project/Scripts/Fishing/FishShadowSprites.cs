using System.Collections.Generic;
using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Silhouettes vues de dessus, sous l'eau, de ce qui mord à l'hameçon, en pixel art net (16 px par case) :
    /// poisson (queue fourchue qui ondule), crabe (pinces et pattes qui bougent), langouste (antennes, éventail),
    /// tortue (nageoires qui battent), grenouille (pattes qui poussent). Dessinées d'avance dans 16 directions,
    /// 3 images d'animation et 3 tailles, à la demande pour chaque forme : jamais de rotation de sprite.
    /// </summary>
    public sealed class FishShadowSprites
    {
        public const int Directions = 16;
        public const int Frames = 3;
        private const float PixelsPerUnit = 16f;

        private static readonly Color Body = new Color(0.03f, 0.12f, 0.20f, 0.58f);
        private static readonly Color Core = new Color(0.02f, 0.08f, 0.14f, 0.72f);

        private readonly int[] _lengths;
        private readonly Dictionary<BodyShape, Sprite[,,]> _byShape = new Dictionary<BodyShape, Sprite[,,]>();

        /// <param name="lengths">Longueur de la silhouette en pixels, pour chaque taille (petite, moyenne, grande).</param>
        public FishShadowSprites(params int[] lengths)
        {
            _lengths = lengths;
        }

        public int Sizes => _lengths.Length;

        /// <summary>Sprite d'un poisson (forme par défaut).</summary>
        public Sprite Get(int size, Vector2 heading, int frame) => Get(BodyShape.Fish, size, heading, frame);

        /// <summary>Sprite pour une forme, une taille, une direction de déplacement et une image d'animation.</summary>
        public Sprite Get(BodyShape shape, int size, Vector2 heading, int frame)
        {
            if (!_byShape.TryGetValue(shape, out Sprite[,,] sprites))
            {
                sprites = Build(shape);
                _byShape[shape] = sprites;
            }

            float angle = Mathf.Atan2(heading.y, heading.x);
            int direction = Mathf.RoundToInt(angle / (Mathf.PI * 2f) * Directions);
            direction = ((direction % Directions) + Directions) % Directions;
            return sprites[Mathf.Clamp(size, 0, Sizes - 1), direction, ((frame % Frames) + Frames) % Frames];
        }

        private Sprite[,,] Build(BodyShape shape)
        {
            var sprites = new Sprite[_lengths.Length, Directions, Frames];
            for (int size = 0; size < _lengths.Length; size++)
            {
                for (int direction = 0; direction < Directions; direction++)
                {
                    float angle = direction * Mathf.PI * 2f / Directions;
                    for (int frame = 0; frame < Frames; frame++)
                    {
                        sprites[size, direction, frame] = Draw(shape, _lengths[size], angle, frame - 1);
                    }
                }
            }

            return sprites;
        }

        /// <summary>
        /// Dessine la silhouette : chaque pixel est ramené dans le repère de l'animal (u le long du corps, de -1 à
        /// l'arrière à +1 à l'avant ; v en travers) puis testé dans la forme. <paramref name="phase"/> vaut -1, 0 ou 1.
        /// </summary>
        private static Sprite Draw(BodyShape shape, int lengthPx, float angle, int phase)
        {
            int size = lengthPx + 4;
            var texture = new Texture2D(size, size) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            float half = lengthPx * 0.5f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x + 0.5f - size * 0.5f;
                    float dy = y + 0.5f - size * 0.5f;
                    float u = (dx * cos + dy * sin) / half;
                    float v = (-dx * sin + dy * cos) / half;

                    bool core;
                    bool inside;
                    switch (shape)
                    {
                        case BodyShape.Crab: inside = Crab(u, v, phase, out core); break;
                        case BodyShape.Lobster: inside = Lobster(u, v, phase, out core); break;
                        case BodyShape.Turtle: inside = Turtle(u, v, phase, out core); break;
                        case BodyShape.Frog: inside = Frog(u, v, phase, out core); break;
                        default: inside = Fish(u, v, phase, out core); break;
                    }

                    texture.SetPixel(x, y, inside ? (core ? Core : Body) : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static bool InEllipse(float u, float v, float cu, float cv, float ru, float rv)
        {
            float a = (u - cu) / ru;
            float b = (v - cv) / rv;
            return a * a + b * b <= 1f;
        }

        /// <summary>Segment épais de (u0, v0) à (u1, v1).</summary>
        private static bool OnSegment(float u, float v, float u0, float v0, float u1, float v1, float thickness)
        {
            float du = u1 - u0;
            float dv = v1 - v0;
            float t = Mathf.Clamp01(((u - u0) * du + (v - v0) * dv) / (du * du + dv * dv));
            float pu = u0 + du * t - u;
            float pv = v0 + dv * t - v;
            return pu * pu + pv * pv <= thickness * thickness;
        }

        // ---------- Poisson ----------

        private static bool Fish(float u, float v, int phase, out bool core)
        {
            core = false;
            // La queue ondule : l'arrière du corps se décale sur le côté, de plus en plus vers le bout.
            if (u < 0f)
            {
                v -= phase * 0.55f * u * u * 0.45f;
            }

            float av = Mathf.Abs(v);
            if (u >= -0.62f && u <= 1f)
            {
                float t = (u + 0.62f) / 1.62f;
                float width = 0.30f * Mathf.Sin(Mathf.Pow(t, 0.8f) * Mathf.PI * 0.92f) + 0.05f;
                if (u > 0.82f)
                {
                    width *= Mathf.Sqrt(Mathf.Max(0f, 1f - (u - 0.82f) / 0.18f));
                }

                if (av <= width)
                {
                    core = InEllipse(u, av, 0.2f, 0f, 0.55f, width * 0.55f);
                    return true;
                }
            }

            if (InEllipse(u, av, 0.26f, 0.40f, 0.12f, 0.09f))
            {
                return true; // nageoires pectorales
            }

            if (u < -0.58f && u >= -1f)
            {
                float k = (-0.58f - u) / 0.42f;
                return av <= 0.08f + k * 0.42f && av >= Mathf.Max(0f, k - 0.5f) * 0.5f; // queue fourchue
            }

            return false;
        }

        // ---------- Crabe ----------

        private static bool Crab(float u, float v, int phase, out bool core)
        {
            float av = Mathf.Abs(v);
            float side = v >= 0f ? 1f : -1f;
            core = InEllipse(u, v, 0f, 0f, 0.32f, 0.42f);

            // Carapace plus large que longue.
            if (InEllipse(u, v, 0f, 0f, 0.45f, 0.62f))
            {
                return true;
            }

            // Pinces devant : bras puis grosse pince ronde ouverte, qui claquent un peu.
            float clawU = 0.72f + phase * side * 0.04f;
            if (OnSegment(u, av, 0.3f, 0.32f, clawU - 0.1f, 0.42f, 0.07f) || InEllipse(u, av, clawU, 0.42f, 0.17f, 0.14f))
            {
                return !InEllipse(u, av, clawU + 0.12f, 0.42f, 0.08f, 0.04f); // l'ouverture de la pince
            }

            // Quatre pattes de chaque côté, qui avancent et reculent en alternance.
            for (int leg = 0; leg < 4; leg++)
            {
                float baseU = -0.32f + leg * 0.19f;
                float swing = (leg % 2 == 0 ? phase : -phase) * 0.07f;
                if (OnSegment(u, av, baseU, 0.5f, baseU - 0.12f + swing, 0.95f, 0.045f))
                {
                    return true;
                }
            }

            return false;
        }

        // ---------- Langouste ----------

        private static bool Lobster(float u, float v, int phase, out bool core)
        {
            float av = Mathf.Abs(v);
            core = false;

            // Céphalothorax, puis abdomen en anneaux qui s'affinent, puis éventail de la queue.
            if (u >= 0.05f && u <= 0.7f && av <= 0.2f - (u - 0.4f) * (u - 0.4f) * 0.5f)
            {
                core = av <= 0.1f;
                return true;
            }

            if (u >= -0.65f && u < 0.05f)
            {
                float width = 0.16f - (0.05f - u) * 0.08f;
                bool ring = Mathf.Repeat(u * 9f, 1f) < 0.18f;     // jointure entre deux anneaux
                return av <= width && !(ring && av > width - 0.03f);
            }

            if (u >= -1f && u < -0.65f)
            {
                float k = (-0.65f - u) / 0.35f;
                return av <= 0.12f + k * 0.28f;                    // éventail
            }

            // Longues antennes vers l'avant, qui fouettent un peu.
            float tipV = 0.34f + phase * 0.06f;
            if (OnSegment(u, av, 0.7f, 0.06f, 1f, tipV, 0.035f))
            {
                return true;
            }

            // Pattes fines sur les côtés.
            for (int leg = 0; leg < 4; leg++)
            {
                float baseU = 0.15f + leg * 0.13f;
                if (OnSegment(u, av, baseU, 0.18f, baseU - 0.08f + (leg % 2 == 0 ? phase : -phase) * 0.04f, 0.36f, 0.03f))
                {
                    return true;
                }
            }

            return false;
        }

        // ---------- Tortue ----------

        private static bool Turtle(float u, float v, int phase, out bool core)
        {
            float av = Mathf.Abs(v);
            core = InEllipse(u, v, -0.02f, 0f, 0.38f, 0.3f);

            if (InEllipse(u, v, -0.02f, 0f, 0.52f, 0.42f))
            {
                return true;                                      // carapace
            }

            if (InEllipse(u, v, 0.62f, 0f, 0.19f, 0.12f) || OnSegment(u, av, 0.45f, 0f, 0.6f, 0f, 0.08f))
            {
                return true;                                      // tête
            }

            // Grandes nageoires avant qui battent : elles pivotent vers l'avant ou l'arrière.
            float sweep = phase * 0.12f;
            if (OnSegment(u, av, 0.28f, 0.36f, 0.05f + sweep, 0.88f, 0.11f))
            {
                return true;
            }

            // Petites nageoires arrière et queue.
            return OnSegment(u, av, -0.42f, 0.25f, -0.62f - sweep * 0.5f, 0.42f, 0.08f)
                || OnSegment(u, av, -0.5f, 0f, -0.7f, 0f, 0.04f);
        }

        // ---------- Grenouille ----------

        private static bool Frog(float u, float v, int phase, out bool core)
        {
            float av = Mathf.Abs(v);
            core = InEllipse(u, v, 0.05f, 0f, 0.28f, 0.16f);

            if (InEllipse(u, v, 0.05f, 0f, 0.42f, 0.27f) || InEllipse(u, v, 0.45f, 0f, 0.2f, 0.24f))
            {
                return true;                                      // corps et tête
            }

            // Pattes avant courtes.
            if (OnSegment(u, av, 0.3f, 0.2f, 0.45f, 0.42f, 0.05f))
            {
                return true;
            }

            // Pattes arrière : repliées sur les côtés, ou tendues vers l'arrière quand elle pousse.
            float kick = (phase + 1) * 0.5f;                       // 0 = repliée, 1 = tendue
            float kneeU = -0.25f - kick * 0.2f;
            float kneeV = 0.48f - kick * 0.18f;
            float footU = -0.05f - kick * 0.85f;
            float footV = 0.42f - kick * 0.2f;
            return OnSegment(u, av, -0.2f, 0.18f, kneeU, kneeV, 0.07f) || OnSegment(u, av, kneeU, kneeV, footU, footV, 0.06f);
        }
    }
}
