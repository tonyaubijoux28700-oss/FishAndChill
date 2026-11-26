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

        /// <summary>Bord du corps et nageoires : plus transparents, l'eau les estompe.</summary>
        private static readonly Color Edge = new Color(0.04f, 0.14f, 0.22f, 0.36f);

        /// <summary>Yeux d'une légende : une lueur pâle dans l'ombre.</summary>
        private static readonly Color Eye = new Color(0.72f, 1f, 0.92f, 0.95f);

        // Tons d'un pixel de silhouette.
        private const int None = 0, EdgeTone = 1, BodyTone = 2, CoreTone = 3, EyeTone = 4;

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

                    bool core = false;
                    int tone;
                    switch (shape)
                    {
                        case BodyShape.Crab: tone = Crab(u, v, phase, out core) ? BodyTone : None; break;
                        case BodyShape.Lobster: tone = Lobster(u, v, phase, out core) ? BodyTone : None; break;
                        case BodyShape.Turtle: tone = Turtle(u, v, phase, out core) ? BodyTone : None; break;
                        case BodyShape.Frog: tone = Frog(u, v, phase, out core) ? BodyTone : None; break;
                        case BodyShape.Monster: tone = Monster(u, v, phase); break;
                        case BodyShape.Moray: tone = Moray(u, v, phase); break;
                        case BodyShape.Crayfish: tone = Crayfish(u, v, phase); break;
                        case BodyShape.Payara: tone = Payara(u, v, phase); break;
                        case BodyShape.Isonade: tone = Isonade(u, v, phase); break;
                        case BodyShape.Rannamaari: tone = Rannamaari(u, v, phase); break;
                        case BodyShape.MohaMoha: tone = MohaMoha(u, v, phase); break;
                        case BodyShape.BoneSawShark: tone = BoneSawShark(u, v, phase); break;
                        case BodyShape.Kraken: tone = Kraken(u, v, phase); break;
                        case BodyShape.Octopus: tone = Octopus(u, v, phase); break;
                        case BodyShape.Squid: tone = Squid(u, v, phase); break;
                        case BodyShape.Ray: tone = Ray(u, v, phase); break;
                        case BodyShape.Dragon: tone = Dragon(u, v, phase); break;
                        default: tone = Fish(u, v, phase); break;
                    }

                    if (tone == BodyTone && core) tone = CoreTone;
                    Color color = tone == EdgeTone ? Edge : tone == BodyTone ? Body : tone == CoreTone ? Core : tone == EyeTone ? Eye : Color.clear;
                    texture.SetPixel(x, y, color);
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

        /// <summary>Segment qui s'affine : épaisseur <paramref name="baseThickness"/> au départ, <paramref name="tipThickness"/> au bout.</summary>
        private static bool OnTaper(float u, float v, float u0, float v0, float u1, float v1, float baseThickness, float tipThickness)
        {
            float du = u1 - u0;
            float dv = v1 - v0;
            float t = Mathf.Clamp01(((u - u0) * du + (v - v0) * dv) / (du * du + dv * dv));
            float pu = u0 + du * t - u;
            float pv = v0 + dv * t - v;
            float thickness = Mathf.Lerp(baseThickness, tipThickness, t);
            return pu * pu + pv * pv <= thickness * thickness;
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

        /// <summary>
        /// Poisson vu de dessus : fuseau au museau pointu, plus large au premier tiers, pédoncule fin, queue en croissant ;
        /// pectorales en ailes inclinées vers l'arrière, petites pelviennes ; le dos plus sombre le long de l'épine, les
        /// bords et les nageoires estompés par l'eau. Tout le corps ondule (la tête bouge à peine, la queue beaucoup).
        /// </summary>
        private static int Fish(float u, float v, int phase)
        {
            float s = Mathf.Clamp01((1f - u) * 0.5f);              // 0 au museau, 1 au bout de la queue
            v -= phase * 0.20f * s * s;                             // ondulation de tout le corps
            float av = Mathf.Abs(v);

            float width = FishWidth(u);
            if (width > 0f && av <= width)
            {
                if (av > width * 0.78f) return EdgeTone;            // flancs estompés
                bool spine = av < width * 0.30f && u > -0.35f && u < 0.85f;
                return spine ? CoreTone : BodyTone;
            }

            // Pectorales : ailes fines qui partent derrière la tête et filent vers l'arrière, en battant un peu.
            float flap = phase * 0.03f;
            if (OnSegment(u, av, 0.46f, 0.17f, 0.20f + flap, 0.40f, 0.055f) || OnSegment(u, av, 0.40f, 0.16f, 0.26f + flap, 0.34f, 0.07f))
            {
                return EdgeTone;
            }

            // Pelviennes, plus petites, au milieu du ventre.
            if (OnSegment(u, av, 0.02f, 0.10f, -0.12f, 0.22f, 0.035f))
            {
                return EdgeTone;
            }

            // Queue en croissant : deux lobes qui s'écartent derrière le pédoncule.
            if (u < -0.52f && u >= -1f)
            {
                float k = (-0.52f - u) / 0.48f;
                float outer = 0.06f + k * 0.40f;
                float notch = Mathf.Max(0f, k - 0.45f) * 0.55f;   // l'échancrure du milieu
                if (av <= outer && av >= notch)
                {
                    return k > 0.75f ? EdgeTone : BodyTone;
                }
            }

            return None;
        }

        /// <summary>Demi-largeur du corps du poisson à la position <paramref name="u"/> (0 hors du corps).</summary>
        private static float FishWidth(float u)
        {
            if (u > 1f || u < -0.56f) return 0f;
            if (u >= 0.32f)
            {
                float t = (u - 0.32f) / 0.68f;                        // de la plus grande largeur au museau
                return 0.25f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(t, 2.3f)), 0.6f);
            }

            float r = Mathf.InverseLerp(-0.56f, 0.32f, u);            // du pédoncule à la plus grande largeur
            return 0.05f + 0.20f * Mathf.Pow(Mathf.SmoothStep(0f, 1f, r), 0.85f);
        }

        // ---------- Légende (monstre) ----------

        /// <summary>
        /// La silhouette d'une légende, qui ne ressemble à aucun poisson ordinaire : tête énorme et carrée à la mâchoire
        /// dentelée, deux yeux pâles qui luisent dans l'ombre, rangées d'épines recourbées tout le long des flancs, longs
        /// barbillons qui traînent en ondulant, corps qui serpente et queue immense, déchirée.
        /// </summary>
        private static int Monster(float u, float v, int phase)
        {
            float s = Mathf.Clamp01((1f - u) * 0.5f);
            v -= phase * 0.16f * s * s + Mathf.Sin(s * 7f + phase * 1.3f) * 0.035f * s;   // le corps serpente
            float av = Mathf.Abs(v);

            // Yeux : deux lueurs pâles, haut sur la tête.
            if (InEllipse(u, av, 0.70f, 0.17f, 0.045f, 0.035f))
            {
                return EyeTone;
            }

            float width = MonsterWidth(u);
            if (width > 0f && av <= width)
            {
                // Mâchoire : quatre gros crocs découpés dans le bout du museau.
                float tooth = Mathf.Abs(Mathf.Repeat(av * 5.5f, 1f) - 0.5f) * 2f;   // 0 au milieu d'un croc, 1 entre deux
                if (u > 0.97f - (1f - tooth) * 0.10f && av > 0.03f) return None;
                if (av > width * 0.86f) return EdgeTone;
                bool spine = av < width * 0.34f && u > -0.5f && u < 0.62f;
                return spine ? CoreTone : BodyTone;
            }

            // Quatre grandes épines épaisses, recourbées vers l'arrière, sur l'avant des flancs.
            for (int k = 0; k < 4; k++)
            {
                float su = 0.40f - k * 0.21f;
                float sw = MonsterWidth(su);
                float length = 0.26f - k * 0.04f;
                if (sw > 0f && OnTaper(u, av, su, sw - 0.03f, su - length * 0.8f, sw + length, 0.065f - k * 0.008f, 0.012f))
                {
                    return BodyTone;
                }
            }

            // Longs barbillons qui partent des coins de la gueule et traînent vers l'arrière en ondulant.
            float lag = phase * 0.05f;
            if (OnSegment(u, av, 0.90f, 0.24f, 0.72f, 0.42f + lag, 0.022f) || OnSegment(u, av, 0.72f, 0.42f + lag, 0.48f, 0.50f - lag, 0.018f)
                || OnSegment(u, av, 0.48f, 0.50f - lag, 0.28f, 0.60f + lag, 0.014f))
            {
                return EdgeTone;
            }

            // Pectorales larges comme des ailes, plus basses que les épines, qui battent lentement.
            float beat = phase * 0.04f;
            if (OnTaper(u, av, 0.30f, 0.24f, -0.02f, 0.48f + beat, 0.11f, 0.03f))
            {
                return EdgeTone;
            }

            // Queue immense en croissant, déchirée de deux grandes encoches.
            if (u < -0.56f && u >= -1f)
            {
                float k = (-0.56f - u) / 0.44f;
                float outer = 0.09f + k * 0.58f;
                float notch = Mathf.Max(0f, k - 0.55f) * 1.1f;
                bool torn = k > 0.55f && (Mathf.Abs(av - outer * 0.62f) < 0.05f || Mathf.Abs(av - outer * 0.86f) < 0.035f && k > 0.8f);
                if (av <= outer && av >= notch && !torn)
                {
                    return k > 0.72f ? EdgeTone : BodyTone;
                }
            }

            return None;
        }

        // ---------- Murène géante ----------

        /// <summary>
        /// Murène vue de dessus : long corps qui ondule tout entier (de plus en plus vers la queue), sans nageoires
        /// séparées, frange estompée le long des bords ; tête massive et musclée, gueule ouverte en V avec des crocs,
        /// deux yeux qui luisent, haut sur la tête.
        /// </summary>
        private static int Moray(float u, float v, int phase)
        {
            float s = Mathf.Clamp01((1f - u) * 0.5f);                                   // 0 au museau, 1 au bout de la queue
            v -= Mathf.Sin(u * 5.2f + phase * 1.1f) * 0.11f * Mathf.Pow(s, 0.9f);      // l'onde qui parcourt tout le corps
            float av = Mathf.Abs(v);

            if (InEllipse(u, av, 0.70f, 0.12f, 0.04f, 0.032f))
            {
                return EyeTone;
            }

            float width = MorayWidth(u);
            if (width <= 0f)
            {
                return None;
            }

            // Gueule ouverte : un V découpé dans le bout du museau, bordé de crocs.
            if (u > 0.80f)
            {
                float gape = (u - 0.80f) * 0.62f;
                if (av < gape)
                {
                    bool fang = av > gape - 0.025f && Mathf.Repeat(u * 34f, 1f) < 0.45f;
                    return fang ? EdgeTone : None;
                }
            }

            if (av <= width)
            {
                if (av > width * 0.80f) return EdgeTone;                                // la frange de nageoire, estompée
                bool spine = av < width * 0.32f && u > -0.7f && u < 0.6f;
                return spine ? CoreTone : BodyTone;
            }

            return None;
        }

        /// <summary>
        /// Écrevisse titan, vue de dessus : carapace et abdomen en anneaux, éventail de la queue, pattes qui marchent (elles
        /// avancent par paires opposées), deux pinces inégales (la grosse à gauche) qui s'ouvrent et se ferment, yeux.
        /// </summary>
        private static int Crayfish(float u, float v, int phase)
        {
            float av = Mathf.Abs(v);
            if (InEllipse(u, av, 0.38f, 0.07f, 0.035f, 0.035f)) return EyeTone;

            // Carapace, rostre, abdomen en anneaux, éventail.
            if (InEllipse(u, v, 0.12f, 0f, 0.27f, 0.2f) || (u > 0.3f && u < 0.46f && av < (0.46f - u) * 0.6f))
            {
                return av < 0.1f ? CoreTone : BodyTone;
            }

            if (u > -0.55f && u <= -0.1f)
            {
                float width = 0.16f - (-0.1f - u) * 0.08f;
                bool ring = Mathf.Repeat(u * 11f, 1f) < 0.2f;
                if (av <= width) return ring && av > width - 0.03f ? EdgeTone : BodyTone;
            }

            if (u > -0.8f && u <= -0.55f && av <= 0.1f + (-0.55f - u) * 1.1f) return BodyTone;

            // Quatre paires de pattes, qui avancent en alternance.
            for (int k = 0; k < 4; k++)
            {
                float u0 = 0.02f + k * 0.07f;
                float step = (k % 2 == 0 ? 1 : -1) * phase * 0.04f;
                float side = Mathf.Sign(v);
                float sway = side > 0 ? step : -step;
                if (OnSegment(u, av, u0, 0.14f, u0 - 0.12f + sway, 0.42f, 0.03f)) return EdgeTone;
            }

            // Les pinces : bras puis paume et deux doigts qui s'ouvrent (phase).
            float open = 0.05f + (phase + 1) * 0.03f;
            for (int side = -1; side <= 1; side += 2)
            {
                float big = side < 0 ? 1.35f : 1f;
                float sv = v * side;
                if (OnSegment(u, sv, 0.32f, 0.1f, 0.52f, 0.28f, 0.05f * big)) return BodyTone;
                if (InEllipse(u, sv, 0.66f, 0.3f, 0.14f * big, 0.09f * big)) return BodyTone;
                if (OnSegment(u, sv, 0.74f, 0.28f - open, 0.98f, 0.26f - open * 1.4f, 0.04f * big)) return BodyTone;
                if (OnSegment(u, sv, 0.74f, 0.28f + open, 0.96f, 0.32f + open * 1.4f, 0.04f * big)) return BodyTone;
            }

            // Antennes.
            if (OnSegment(u, av, 0.4f, 0.04f, 0.95f, 0.5f + phase * 0.04f, 0.015f)) return EdgeTone;
            return None;
        }

        /// <summary>
        /// Calmar colossal (Kraken), vu de dessus, tête vers l'avant : long manteau effilé et ses nageoires en losange à
        /// l'arrière, tête aux deux gros yeux qui luisent, huit bras en éventail qui ondulent, deux longues tentacules
        /// terminées par des massues.
        /// </summary>
        private static int Kraken(float u, float v, int phase)
        {
            float av = Mathf.Abs(v);

            if (InEllipse(u, av, 0.16f, 0.1f, 0.04f, 0.035f)) return EyeTone;

            // Le manteau, effilé vers l'arrière.
            if (u <= 0.22f && u >= -0.8f)
            {
                float width = 0.03f + 0.12f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.8f, 0.05f, u));
                if (av <= width) return av > width * 0.8f ? EdgeTone : av < width * 0.4f ? CoreTone : BodyTone;
            }

            // Les nageoires en losange, au bout du manteau.
            if (u < -0.6f && u >= -0.95f)
            {
                float k = 1f - Mathf.Abs(u + 0.8f) / 0.15f;
                if (av <= 0.18f * k) return av > 0.15f * k ? EdgeTone : BodyTone;
            }

            // Les huit bras, en éventail, qui ondulent.
            for (int a = 0; a < 8; a++)
            {
                float spread = (a - 3.5f) * 0.035f;
                float wave = Mathf.Sin((u - 0.22f) * 18f + a + phase * 1.2f) * 0.015f;
                float center = spread * (1f + (u - 0.22f) * 3f) + wave;
                if (u > 0.2f && u < 0.72f && Mathf.Abs(v - center) < 0.022f * (1f - (u - 0.2f) / 0.6f) + 0.006f) return BodyTone;
            }

            // Les deux longues tentacules et leurs massues.
            foreach (float side in new[] { -1f, 1f })
            {
                float center = side * (0.05f + (u - 0.2f) * 0.12f) + Mathf.Sin(u * 10f + phase) * 0.02f;
                if (u > 0.2f && u < 0.98f && Mathf.Abs(v - center) < 0.01f) return EdgeTone;
                if (InEllipse(u, v - center, 0.95f, 0f, 0.05f, 0.03f)) return BodyTone;
            }

            return None;
        }

        /// <summary>
        /// Requin scie squelette (Spectre des épaves), vu de dessus : corps de requin fuselé qui ondule vers l'arrière,
        /// longue scie plate bordée de dents devant le museau, grandes pectorales en faux, queue en croissant ; les
        /// orbites luisent et la cage des côtes laisse passer une lueur (rangée de points clairs le long du dos).
        /// </summary>
        private static int BoneSawShark(float u, float v, int phase)
        {
            float s = Mathf.Clamp01((0.4f - u) * 0.7f);
            v -= phase * 0.2f * s * s;
            float av = Mathf.Abs(v);

            if (InEllipse(u, av, 0.42f, 0.06f, 0.03f, 0.025f)) return EyeTone;

            // La scie : lame fine devant le museau, dents sur les deux bords.
            if (u > 0.5f && u <= 1f)
            {
                if (av <= 0.022f) return u > 0.97f ? EdgeTone : BodyTone;
                if (av <= 0.05f && Mathf.Repeat(u * 30f, 1f) < 0.3f) return EdgeTone;
            }

            // Le corps fuselé.
            float width = 0f;
            if (u <= 0.5f && u >= -0.6f)
            {
                width = u >= 0.05f
                    ? 0.15f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow((u - 0.05f) / 0.45f, 2f)), 0.6f)
                    : 0.03f + 0.12f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.6f, 0.05f, u));
            }

            if (width > 0f && av <= width)
            {
                if (u > -0.35f && u < 0.25f && av < 0.012f && Mathf.Repeat(u * 18f, 1f) < 0.4f) return EyeTone;   // la lueur des côtes
                if (av > width * 0.8f) return EdgeTone;
                return av < width * 0.4f ? CoreTone : BodyTone;
            }

            // Pectorales en faux.
            float flap = phase * 0.03f;
            if (OnTaper(u, av, 0.12f, 0.1f, -0.12f, 0.36f + flap, 0.05f, 0.01f)) return BodyTone;

            // Queue en croissant.
            if (u < -0.6f && u >= -1f)
            {
                float k = (-0.6f - u) / 0.4f;
                float outer = 0.03f + k * 0.3f;
                if (av <= outer && !(k > 0.5f && av < (k - 0.5f) * 0.45f)) return k > 0.8f ? EdgeTone : BodyTone;
            }

            return None;
        }

        /// <summary>
        /// Moha-moha (tortue géante au long cou), vu de dessus : carapace ovale aux bords bosselés de corail, long cou qui
        /// ondule jusqu'à une petite tête aux deux yeux qui luisent, deux grandes nageoires avant en aile qui pagaient
        /// (en avant, en arrière), deux petites palettes à l'arrière, queue en nageoire.
        /// </summary>
        private static int MohaMoha(float u, float v, int phase)
        {
            float av = Mathf.Abs(v);

            // Le cou ondule un peu, la tête au bout.
            if (u > 0.3f)
            {
                float k = (u - 0.3f) / 0.6f;
                float center = phase * 0.05f * Mathf.Sin(k * Mathf.PI);
                float dv = Mathf.Abs(v - center);
                if (InEllipse(u, dv, 0.84f, 0.05f, 0.028f, 0.025f)) return EyeTone;
                if (InEllipse(u, v - center, 0.84f, 0f, 0.11f, 0.09f)) return dv > 0.075f ? EdgeTone : BodyTone;
                if (u <= 0.8f && dv <= 0.045f) return dv > 0.035f ? EdgeTone : BodyTone;
            }

            // La carapace : ovale, bord bosselé par le corail.
            float bump = Mathf.Repeat((u + v) * 9f, 1f) < 0.35f ? 0.02f : 0f;
            if (InEllipse(u, v, -0.05f, 0f, 0.4f + bump, 0.27f + bump))
            {
                if (!InEllipse(u, v, -0.05f, 0f, 0.36f, 0.23f)) return EdgeTone;
                return InEllipse(u, v, -0.05f, 0f, 0.22f, 0.13f) ? CoreTone : BodyTone;
            }

            // Grandes nageoires avant : elles pagaient d'avant en arrière.
            float sweep = phase * 0.08f;
            if (OnTaper(u, av, 0.18f, 0.2f, 0.02f - sweep, 0.62f, 0.07f, 0.015f)) return BodyTone;

            // Petites palettes arrière.
            if (OnTaper(u, av, -0.3f, 0.18f, -0.42f + sweep * 0.5f, 0.34f, 0.05f, 0.02f)) return BodyTone;

            // Queue charnue finie en nageoire.
            if (u < -0.42f && u >= -0.9f)
            {
                float k = (-0.42f - u) / 0.48f;
                float width = k < 0.65f ? 0.05f * (1f - k) + 0.015f : 0.02f + (k - 0.65f) * 0.5f;
                float tv = v - phase * 0.05f * k;
                if (Mathf.Abs(tv) <= width) return k > 0.85f ? EdgeTone : BodyTone;
            }

            return None;
        }

        /// <summary>
        /// Rannamaari (djinn des abysses), vu de dessus : long corps de barracuda qui ondule comme un ruban, couronne de
        /// cornes de chaque côté de la tête, longues pectorales en faux, queue en croissant, et ses lumières : deux yeux,
        /// la perle au bout du filament devant la gueule, et une rangée de photophores le long de chaque flanc.
        /// </summary>
        private static int Rannamaari(float u, float v, int phase)
        {
            float s = Mathf.Clamp01((0.6f - u) * 0.6f);
            v -= phase * 0.2f * s * s;
            float av = Mathf.Abs(v);

            if (InEllipse(u, av, 0.72f, 0.07f, 0.035f, 0.03f)) return EyeTone;

            // La perle au bout du filament, devant la gueule.
            if (InEllipse(u, v, 0.97f, 0f, 0.035f, 0.035f)) return EyeTone;
            if (av < 0.008f && u > 0.82f && u < 0.94f) return EdgeTone;

            // Corps long et fin : museau pointu, pédoncule fin.
            float width = 0f;
            if (u <= 0.88f && u >= -0.72f)
            {
                width = u >= 0.4f
                    ? 0.14f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow((u - 0.4f) / 0.48f, 2f)), 0.5f)
                    : 0.035f + 0.105f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.72f, 0.4f, u));
            }

            if (width > 0f && av <= width)
            {
                // Photophores le long des flancs.
                if (u > -0.45f && u < 0.6f && Mathf.Abs(av - width * 0.7f) < 0.018f && Mathf.Repeat(u * 14f, 1f) < 0.3f) return EyeTone;
                if (av > width * 0.82f) return EdgeTone;
                return av < width * 0.35f && u > -0.5f && u < 0.8f ? CoreTone : BodyTone;
            }

            // Couronne de cornes, de chaque côté de la tête, tournées vers l'arrière.
            for (int k = 0; k < 3; k++)
            {
                if (OnTaper(u, av, 0.6f - k * 0.06f, 0.1f, 0.47f - k * 0.06f, 0.22f - k * 0.02f, 0.022f, 0.004f)) return EdgeTone;
            }

            // Longues pectorales en faux, qui battent.
            float flap = phase * 0.04f;
            if (OnTaper(u, av, 0.32f, 0.1f, -0.05f, 0.34f + flap, 0.05f, 0.01f)) return BodyTone;

            // Queue en croissant.
            if (u < -0.68f && u >= -1f)
            {
                float k = (-0.68f - u) / 0.32f;
                float outer = 0.035f + k * 0.3f;
                if (av <= outer && !(k > 0.55f && av < (k - 0.55f) * 0.5f)) return k > 0.8f ? EdgeTone : BodyTone;
            }

            return None;
        }

        /// <summary>
        /// Isonade (rascasse titan), vue de dessus : tête énorme et large hérissée d'épines, grandes pectorales en éventail
        /// qui battent lentement, queue en éventail bordée de crochets, deux yeux qui luisent.
        /// </summary>
        private static int Isonade(float u, float v, int phase)
        {
            float s = Mathf.Clamp01((0.2f - u) * 0.6f);
            v -= phase * 0.18f * s * s;
            float av = Mathf.Abs(v);

            if (InEllipse(u, av, 0.66f, 0.13f, 0.04f, 0.035f)) return EyeTone;

            // Corps trapu : tête énorme et large, pédoncule fin.
            float width = 0f;
            if (u <= 0.9f && u >= -0.55f)
            {
                width = u >= 0.45f
                    ? 0.24f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow((u - 0.45f) / 0.45f, 2f)), 0.4f)
                    : 0.05f + 0.19f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.55f, 0.45f, u));
            }

            if (width > 0f && av <= width)
            {
                if (av > width * 0.82f) return EdgeTone;
                return av < width * 0.35f && u > -0.35f && u < 0.85f ? CoreTone : BodyTone;
            }

            // Épines de l'opercule, de chaque côté de la tête.
            for (int k = 0; k < 3; k++)
            {
                if (OnTaper(u, av, 0.5f - k * 0.06f, 0.2f, 0.4f - k * 0.06f, 0.3f, 0.02f, 0.005f)) return EdgeTone;
            }

            // Grandes pectorales en éventail qui battent lentement.
            float flap = phase * 0.03f;
            float du = u - 0.32f, dv = av - 0.16f;
            float r = Mathf.Sqrt(du * du + dv * dv);
            float a = Mathf.Atan2(dv, -du);
            if (a > 0.1f - flap && a < 1.5f && r < 0.34f - (Mathf.Repeat(a * 8f, 1f) < 0.3f ? 0.04f : 0f)) return r > 0.3f ? EdgeTone : BodyTone;

            // Queue en éventail, festonnée, hérissée de crochets.
            if (u < -0.5f && u >= -1f)
            {
                float k = (-0.5f - u) / 0.5f;
                float outer = 0.05f + k * 0.3f;
                if (av <= outer) return k > 0.75f ? EdgeTone : BodyTone;
                if (av <= outer + 0.05f && Mathf.Repeat(u * 22f, 1f) < 0.3f) return EdgeTone;   // crochets
            }

            return None;
        }

        /// <summary>
        /// Payara titan, vu de dessus : corps fuselé qui ondule surtout vers l'arrière, grande queue fourchue déchirée,
        /// longues pectorales en faux qui battent, deux crocs qui dépassent devant le museau, deux yeux qui luisent.
        /// </summary>
        private static int Payara(float u, float v, int phase)
        {
            float s = Mathf.Clamp01((1f - u) * 0.5f);
            v -= phase * 0.22f * s * s * s;
            float av = Mathf.Abs(v);

            if (InEllipse(u, av, 0.72f, 0.09f, 0.04f, 0.035f)) return EyeTone;

            // Les deux crocs, qui dépassent devant le museau.
            if (OnTaper(u, av, 0.86f, 0.045f, 1f, 0.03f, 0.022f, 0.006f)) return EdgeTone;

            // Corps fuselé : museau pointu, plus large au premier tiers, pédoncule fin.
            float width = 0f;
            if (u <= 0.92f && u >= -0.6f)
            {
                width = u >= 0.3f
                    ? 0.17f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow((u - 0.3f) / 0.62f, 2f)), 0.55f)
                    : 0.035f + 0.135f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.6f, 0.3f, u));
            }

            if (width > 0f && av <= width)
            {
                if (av > width * 0.8f) return EdgeTone;
                return av < width * 0.3f && u > -0.4f && u < 0.85f ? CoreTone : BodyTone;
            }

            // Longues pectorales en faux qui battent.
            float flap = phase * 0.04f;
            if (OnTaper(u, av, 0.5f, 0.13f, 0.1f + flap, 0.42f, 0.06f, 0.012f)) return EdgeTone;

            // Grande queue fourchue, une encoche déchirée.
            if (u < -0.56f && u >= -1f)
            {
                float k = (-0.56f - u) / 0.44f;
                float outer = 0.05f + k * 0.5f;
                float notch = Mathf.Max(0f, k - 0.35f) * 0.75f;
                bool torn = k > 0.6f && Mathf.Abs(av - outer * 0.7f) < 0.035f;
                if (av <= outer && av >= notch && !torn) return k > 0.7f ? EdgeTone : BodyTone;
            }

            return None;
        }

        /// <summary>
        /// Dragon vietnamien vu de dessus : corps de serpent mince qui ondule en deux vagues, quatre pattes écartées,
        /// tête allongée aux deux petites cornes, longues moustaches qui flottent en arrière, deux yeux qui luisent.
        /// </summary>
        private static int Dragon(float u, float v, int phase)
        {
            float s = Mathf.Clamp01((1f - u) * 0.5f);
            float wave = Mathf.Sin(u * 7.5f + phase * 1.2f) * 0.13f * Mathf.Pow(s, 0.7f);
            float bv = v - wave;
            float av = Mathf.Abs(bv);
            if (InEllipse(u, av, 0.82f, 0.06f, 0.035f, 0.025f)) return EyeTone;

            // Moustaches : deux fils qui partent du museau et flottent vers l'arrière en s'écartant.
            foreach (float side in new[] { -1f, 1f })
            {
                float wu = Mathf.Clamp(u, 0.35f, 0.95f);
                float wv = side * (0.06f + (0.95f - wu) * 0.5f) + Mathf.Sin(wu * 14f + phase) * 0.03f;
                if (u > 0.35f && u < 0.95f && Mathf.Abs(bv - wv) < 0.012f) return EdgeTone;
            }

            // Petites cornes vers l'arrière.
            if (OnTaper(u, av, 0.78f, 0.06f, 0.64f, 0.13f, 0.022f, 0.008f)) return EdgeTone;

            // Quatre pattes écartées, griffes vers l'avant.
            foreach (float hip in new[] { 0.45f, -0.25f })
            {
                float hv = Mathf.Sin(hip * 7.5f + phase * 1.2f) * 0.13f * Mathf.Pow(Mathf.Clamp01((1f - hip) * 0.5f), 0.7f);
                float step = phase * 0.04f * (hip > 0f ? 1f : -1f);
                foreach (float side in new[] { -1f, 1f })
                {
                    if (OnTaper(u, v, hip, hv, hip + 0.06f + step * side, hv + side * 0.2f, 0.035f, 0.02f)) return BodyTone;
                }
            }

            float width;
            if (u > 1f || u < -1f) width = 0f;
            else if (u >= 0.7f) width = 0.085f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow((u - 0.7f) / 0.3f, 2.4f)), 0.5f);
            else width = 0.012f + 0.07f * Mathf.Pow(Mathf.InverseLerp(-1f, 0.7f, u), 0.6f);
            if (width > 0f && av <= width)
            {
                if (av > width * 0.78f) return EdgeTone;
                return av < width * 0.3f && u < 0.65f ? CoreTone : BodyTone;
            }

            // La crête de flammes, le long du dos : une frange en dents de scie.
            if (u > -0.9f && u < 0.65f && av < width + 0.03f * Mathf.Abs(Mathf.Sin(u * 40f))) return EdgeTone;
            return None;
        }

        /// <summary>Demi-largeur de la murène : tête massive, cou épais, corps long qui s'affine jusqu'à une queue fine.</summary>
        private static float MorayWidth(float u)
        {
            if (u > 1f || u < -1f) return 0f;
            if (u >= 0.55f)
            {
                float t = (u - 0.55f) / 0.45f;
                return 0.23f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(t, 2.6f)), 0.5f);
            }

            float r = Mathf.InverseLerp(-1f, 0.55f, u);
            return 0.03f + 0.19f * Mathf.Pow(r, 0.65f);
        }

        /// <summary>Demi-largeur du corps de la légende : tête énorme et carrée, corps qui s'affine vite.</summary>
        private static float MonsterWidth(float u)
        {
            if (u > 1f || u < -0.6f) return 0f;
            if (u >= 0.45f)
            {
                float t = (u - 0.45f) / 0.55f;
                return 0.34f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(t, 4f)), 0.4f);   // tête carrée, front large
            }

            float r = Mathf.InverseLerp(-0.6f, 0.45f, u);
            return 0.07f + 0.27f * Mathf.Pow(r, 1.4f);
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

        // ---------- Poulpe, calmar, raie ----------

        /// <summary>
        /// Poulpe vu de dessus : tête ronde (le manteau) devant, huit bras qui s'étalent en éventail derrière elle et
        /// ondulent chacun à son rythme, plus fins vers le bout.
        /// </summary>
        private static int Octopus(float u, float v, int phase)
        {
            float av = Mathf.Abs(v);
            if (InEllipse(u, v, 0.48f, 0f, 0.34f, 0.24f))
            {
                return InEllipse(u, v, 0.52f, 0f, 0.2f, 0.12f) ? CoreTone : BodyTone;
            }

            // Le centre des bras, juste derrière la tête.
            float ru = u - 0.12f;
            float r = Mathf.Sqrt(ru * ru + v * v);
            if (r < 0.16f)
            {
                return BodyTone;
            }

            if (r > 0.88f)
            {
                return None;
            }

            // Huit bras, de 100° à 260° (vers l'arrière et les côtés) ; chacun ondule en s'éloignant.
            float angle = Mathf.Atan2(v, ru) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            for (int k = 0; k < 8; k++)
            {
                float armAngle = 106f + k * 21f + Mathf.Sin(r * 9f + phase * 1.4f + k * 1.7f) * 9f * r;
                float d = Mathf.Abs(Mathf.DeltaAngle(angle, armAngle)) * Mathf.Deg2Rad * r;
                float thickness = Mathf.Lerp(0.07f, 0.015f, r / 0.88f);
                if (d <= thickness)
                {
                    return r < 0.4f ? BodyTone : EdgeTone;
                }
            }

            return av < 0.12f && r < 0.24f ? BodyTone : None;
        }

        /// <summary>
        /// Calmar ou seiche vu de dessus : manteau en torpille, pointu devant, bordé de nageoires qui ondulent ;
        /// derrière, la tête et un faisceau de bras serrés qui traînent en se balançant.
        /// </summary>
        private static int Squid(float u, float v, int phase)
        {
            float av = Mathf.Abs(v);
            if (u > -0.15f && u <= 1f)
            {
                float t = (u + 0.15f) / 1.15f;                           // 0 à l'arrière du manteau, 1 à la pointe
                float width = 0.17f * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t * t));
                if (av <= width)
                {
                    return av < width * 0.4f ? CoreTone : BodyTone;
                }

                // Nageoires : un liseré qui ondule tout le long du manteau.
                float fin = width + 0.06f + Mathf.Sin(u * 14f + phase * 1.6f) * 0.02f;
                if (av <= fin && t < 0.95f)
                {
                    return EdgeTone;
                }
            }

            // Tête et yeux, puis les bras qui traînent en balançant.
            if (InEllipse(u, v, -0.22f, 0f, 0.1f, 0.13f))
            {
                return BodyTone;
            }

            if (u < -0.28f && u > -1f)
            {
                float s = (-0.28f - u) / 0.72f;
                float sway = Mathf.Sin(s * 4f + phase * 1.2f) * 0.06f * s;
                for (int k = -2; k <= 2; k++)
                {
                    float armV = sway + k * 0.035f * (1f - s * 0.4f);
                    if (Mathf.Abs(v - armV) < 0.016f + (1f - s) * 0.012f && s < 0.75f + Mathf.Abs(k) * -0.06f + (k == 0 ? 0.25f : 0f))
                    {
                        return s < 0.3f ? BodyTone : EdgeTone;
                    }
                }
            }

            return None;
        }

        /// <summary>
        /// Raie vue de dessus : disque en losange arrondi, plus large que long, dont le bout des ailes monte et descend
        /// lentement ; longue queue fine qui ondule derrière.
        /// </summary>
        private static int Ray(float u, float v, int phase)
        {
            float av = Mathf.Abs(v);
            const float front = 0.62f, rear = -0.3f, widest = 0.12f;
            if (u >= rear && u <= front)
            {
                float t = u >= widest ? (u - widest) / (front - widest) : (widest - u) / (widest - rear);
                float span = (0.66f - phase * 0.04f) * Mathf.Pow(Mathf.Max(0f, 1f - t), u >= widest ? 0.75f : 1.1f);
                if (av <= span)
                {
                    if (av > span * 0.82f) return EdgeTone;                  // le bout des ailes, estompé
                    return av < 0.14f && u > rear + 0.05f ? CoreTone : BodyTone;
                }
            }

            // Queue : fine, de plus en plus fine, qui ondule.
            if (u < rear + 0.02f && u > -1f)
            {
                float s = (rear - u) / (1f + rear);
                float tailV = Mathf.Sin(s * 5f + phase) * 0.05f * s;
                if (Mathf.Abs(v - tailV) < Mathf.Lerp(0.035f, 0.01f, s))
                {
                    return s < 0.4f ? BodyTone : EdgeTone;
                }
            }

            return None;
        }
    }
}
