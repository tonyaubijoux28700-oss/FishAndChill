using UnityEngine;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Silhouettes du poisson vu de dessus, sous l'eau, en pixel art net (16 px par case) : tête arrondie, corps
    /// fuselé, nageoires pectorales, queue fourchue. Dessinées d'avance dans 16 directions et 3 positions de queue
    /// (elle ondule), en 3 tailles : jamais de rotation de sprite (qui rendrait les pixels irréguliers).
    /// </summary>
    public sealed class FishShadowSprites
    {
        public const int Directions = 16;
        public const int TailFrames = 3;
        private const float PixelsPerUnit = 16f;

        private readonly Sprite[,,] _sprites;

        /// <param name="lengths">Longueur du poisson en pixels, pour chaque taille (petit, moyen, grand).</param>
        public FishShadowSprites(params int[] lengths)
        {
            _sprites = new Sprite[lengths.Length, Directions, TailFrames];
            for (int size = 0; size < lengths.Length; size++)
            {
                for (int direction = 0; direction < Directions; direction++)
                {
                    float angle = direction * Mathf.PI * 2f / Directions;
                    for (int frame = 0; frame < TailFrames; frame++)
                    {
                        float bend = (frame - 1) * 0.55f;   // queue à gauche, droite ou au milieu
                        _sprites[size, direction, frame] = Draw(lengths[size], angle, bend);
                    }
                }
            }
        }

        public int Sizes => _sprites.GetLength(0);

        /// <summary>Sprite pour une taille, une direction de nage (vecteur) et une image de queue.</summary>
        public Sprite Get(int size, Vector2 heading, int frame)
        {
            float angle = Mathf.Atan2(heading.y, heading.x);
            int direction = Mathf.RoundToInt(angle / (Mathf.PI * 2f) * Directions);
            direction = ((direction % Directions) + Directions) % Directions;
            return _sprites[Mathf.Clamp(size, 0, Sizes - 1), direction, ((frame % TailFrames) + TailFrames) % TailFrames];
        }

        /// <summary>
        /// Dessine la silhouette : pour chaque pixel, on le ramène dans le repère du poisson (u le long du corps,
        /// de -1 = bout de la queue à +1 = museau ; v en travers) et on teste s'il est dans la forme.
        /// </summary>
        private static Sprite Draw(int lengthPx, float angle, float tailBend)
        {
            int size = lengthPx + 4;
            var texture = new Texture2D(size, size) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            float half = lengthPx * 0.5f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            var body = new Color(0.03f, 0.12f, 0.20f, 0.58f);
            var core = new Color(0.02f, 0.08f, 0.14f, 0.72f);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x + 0.5f - size * 0.5f;
                    float dy = y + 0.5f - size * 0.5f;
                    float u = (dx * cos + dy * sin) / half;
                    float v = (-dx * sin + dy * cos) / half;

                    // La queue ondule : l'arrière du corps se décale sur le côté, de plus en plus vers le bout.
                    if (u < 0f)
                    {
                        v -= tailBend * u * u * 0.45f;
                    }

                    bool inside = Inside(u, v, out bool inCore);
                    texture.SetPixel(x, y, inside ? (inCore ? core : body) : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        /// <summary>La forme du poisson vu de dessus, dans son repère (u de -1 à 1).</summary>
        private static bool Inside(float u, float v, out bool core)
        {
            core = false;
            float av = Mathf.Abs(v);

            // Corps : le plus large au premier tiers depuis la tête, museau arrondi, fin pédoncule vers la queue.
            if (u >= -0.62f && u <= 1f)
            {
                float t = (u + 0.62f) / 1.62f;                       // 0 au pédoncule, 1 au museau
                float width = 0.30f * Mathf.Sin(Mathf.Pow(t, 0.8f) * Mathf.PI * 0.92f) + 0.05f;
                if (u > 0.82f)
                {
                    width *= Mathf.Sqrt(Mathf.Max(0f, 1f - (u - 0.82f) / 0.18f)); // museau arrondi
                }

                if (av <= width)
                {
                    // Dos plus sombre : un ovale le long de l'échine, vers l'avant du corps.
                    float du = (u - 0.2f) / 0.55f;
                    float dv = av / Mathf.Max(0.01f, width * 0.55f);
                    core = du * du + dv * dv <= 1f;
                    return true;
                }
            }

            // Nageoires pectorales : deux palettes arrondies, un peu en arrière de la tête.
            float fu = (u - 0.26f) / 0.12f;
            float fv = (av - 0.40f) / 0.09f;
            if (fu * fu + fv * fv <= 1f)
            {
                return true;
            }

            // Queue fourchue : deux lobes pleins qui s'écartent, avec une échancrure au milieu vers le bout.
            if (u < -0.58f && u >= -1f)
            {
                float k = (-0.58f - u) / 0.42f;                      // 0 au pédoncule, 1 au bout
                float spread = 0.08f + k * 0.42f;
                float notch = Mathf.Max(0f, k - 0.5f) * 0.5f;
                return av <= spread && av >= notch;
            }

            return false;
        }
    }
}
