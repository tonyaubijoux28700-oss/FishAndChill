using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Moteur de dessin de poissons en pixel art détaillé (vus de profil, tête à droite).
    /// Une espèce = une fiche : silhouette (profils dos / ventre), dégradé, nageoires, queue, tête, motifs.
    /// </summary>
    public sealed class DetailedFish
    {
        public enum FinSide { Up, Down }
        public enum FinShape { Linear, Falcate, Rounded }
        public enum TailShape { Forked, Lunate, Truncate, Round }

        public sealed class Fin
        {
            public FinSide Side;
            public int XRear;
            public int XFront;
            public float HeightRear;
            public float HeightFront;
            public FinShape Shape = FinShape.Linear;
            public Color Base;
            public Color Ray;
            public Color Edge;
            public bool Spiny;
        }

        public sealed class Pectoral
        {
            public int BaseX;
            public float BaseYOffset;
            public int Length = 6;
            public float Width = 2.5f;
            public float Slope = 0.3f;
            public Color Base;
            public Color Ray;
            public Color Edge;
        }

        public sealed class Tail
        {
            public TailShape Shape = TailShape.Forked;
            public int XTip = 1;
            public int XBase = 14;
            public float Span = 10f;
            public Color Base;
            public Color Ray;
            public Color Edge;
            public Color? BaseTint;
        }

        public int Width = 96;
        public int Height = 32;
        public float CenterY = 15.5f;
        public Vector2[] Upper;
        public Vector2[] Lower;

        public Color Back;
        public Color Flank;
        public Color Belly;
        public float FlankStart = 0.35f;
        public float BellyStart = 0.68f;
        public bool ScaleTexture = true;

        public Tail Caudal;
        public List<Fin> Fins = new List<Fin>();
        public List<Pectoral> PectoralFins = new List<Pectoral>();

        public int EyeX;
        public float EyeYOffset = 2f;
        public int EyeRadius = 1;
        public Color Iris = new Color(0.85f, 0.75f, 0.35f);
        public Color Pupil = new Color(0.05f, 0.05f, 0.06f);

        public int GillX;
        public float GillStrength = 0.35f;
        public Color? GillTint;

        public int MouthXStart;
        public int MouthXEnd;
        public float MouthYOffset = -0.5f;
        public float MouthSlope;
        public Color MouthColor = new Color(0.12f, 0.10f, 0.08f);

        public Color OutlineColor = new Color(0.08f, 0.09f, 0.10f);
        public int Seed = 1;

        /// <summary>Motifs du corps : (x, y, t de 0 = dos à 1 = ventre, couleur actuelle) → nouvelle couleur.</summary>
        public List<Func<int, int, float, Color, Color>> BodyPatterns = new List<Func<int, int, float, Color, Color>>();

        /// <summary>Détails peints après le corps et la tête (bec, dents, écussons…).</summary>
        public List<Action<DetailedFish, Color[,]>> Details = new List<Action<DetailedFish, Color[,]>>();

        // ---------- Géométrie ----------

        public float UpperAt(float x) => Sample(Upper, x);
        public float LowerAt(float x) => Sample(Lower, x);

        public bool InBody(int x, int y)
        {
            float up = UpperAt(x);
            float down = LowerAt(x);
            if (up <= 0f && down <= 0f)
            {
                return false;
            }

            float dy = y - CenterY;
            return dy <= up && dy >= -down;
        }

        /// <summary>Position verticale relative dans le corps : 0 = dos, 1 = ventre.</summary>
        public float BodyT(int x, int y)
        {
            float top = CenterY + UpperAt(x);
            float bottom = CenterY - LowerAt(x);
            return Mathf.InverseLerp(top, bottom, y);
        }

        // ---------- Rendu ----------
        //
        // Ordre : queue et nageoires (derrière), couleurs du corps (dégradé, motifs, écailles, tête, détails),
        // puis l'éclairage du corps comme s'il était rond (ombres froides, lumières chaudes, reflet),
        // nageoires pectorales translucides par-dessus, œil, et enfin un contour sélectif (teinté, plus clair en haut).

        /// <summary>Direction de la lumière : d'en haut, un peu de face et vers la tête.</summary>
        private static readonly Vector3 LightDirection = new Vector3(0.35f, 0.65f, 0.68f).normalized;

        private float[,] _light;

        public Color[,] Draw()
        {
            var p = new Color[Width, Height];
            _light = new float[Width, Height];

            if (Caudal != null)
            {
                DrawTail(p);
            }

            foreach (Fin fin in Fins)
            {
                DrawFin(p, fin);
            }

            DrawBody(p);
            DrawHead(p);

            foreach (var detail in Details)
            {
                detail(this, p);
            }

            LightBody(p);

            foreach (Pectoral pectoral in PectoralFins)
            {
                DrawPectoral(p, pectoral);
            }

            DrawEye(p);
            SelectiveOutline(p);
            return p;
        }

        /// <summary>Couleur éclairée : en dessous de 1, vers une ombre froide ; au-dessus, vers une lumière chaude.</summary>
        public static Color Shade(Color c, float light)
        {
            if (light < 1f)
            {
                var shadow = new Color(c.r * 0.36f, c.g * 0.40f, c.b * 0.55f + 0.05f, c.a);
                return Color.Lerp(shadow, c, Mathf.InverseLerp(0.40f, 1f, light));
            }

            // Lumière proportionnelle à la couleur (un poisson sombre reste sombre), légèrement chaude.
            var warm = new Color(Mathf.Min(1f, c.r * 1.32f + 0.07f), Mathf.Min(1f, c.g * 1.28f + 0.06f), Mathf.Min(1f, c.b * 1.12f + 0.03f), c.a);
            return Color.Lerp(c, warm, Mathf.Clamp01((light - 1f) / 0.25f));
        }

        private void DrawBody(Color[,] p)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (!InBody(x, y))
                    {
                        continue;
                    }

                    float t = BodyT(x, y);
                    Color c = t < FlankStart
                        ? Color.Lerp(Back, Flank, t / FlankStart)
                        : t < BellyStart ? Flank : Color.Lerp(Flank, Belly, (t - BellyStart) / (1f - BellyStart));

                    if (ScaleTexture)
                    {
                        c = ApplyScales(c, x, y, t);
                    }

                    foreach (var pattern in BodyPatterns)
                    {
                        c = pattern(x, y, t, c);
                    }

                    p[x, y] = c;
                    _light[x, y] = BodyLight(x, t);
                }
            }
        }

        /// <summary>
        /// Écailles imbriquées : petits arcs « ( » (le bord libre de l'écaille regarde la queue), rangées décalées,
        /// qui s'effacent vers le ventre et sur la tête.
        /// </summary>
        private Color ApplyScales(Color c, int x, int y, float t)
        {
            if (GillX > 0 && x >= GillX - 1)
            {
                return c;
            }

            float strength = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.9f, t));
            if (strength <= 0f)
            {
                return c;
            }

            int row = Mathf.FloorToInt(y / 3f);
            int sx = ((x + (row % 2) * 2) % 4 + 4) % 4;
            int sy = ((y % 3) + 3) % 3;
            if ((sx == 0 && sy == 1) || (sx == 1 && sy != 1))
            {
                return Color.Lerp(c, Color.Lerp(c, Color.black, 0.5f), 0.22f * strength);
            }

            if (sx == 2 && sy == 2)
            {
                return Color.Lerp(c, Color.white, 0.10f * strength);
            }

            return c;
        }

        /// <summary>Éclairage d'un point du corps vu comme une section ronde qui s'affine vers la tête et la queue.</summary>
        private float BodyLight(int x, float t)
        {
            float s = Mathf.Clamp(t * 2f - 1f, -0.98f, 0.98f);   // -1 = dos, 1 = ventre
            float depth = Mathf.Sqrt(1f - s * s);
            float half = (UpperAt(x + 1) + LowerAt(x + 1) - UpperAt(x - 1) - LowerAt(x - 1)) * 0.25f;
            float nx = Mathf.Clamp(-half * 1.3f, -0.8f, 0.8f) * depth;
            Vector3 normal = new Vector3(nx, -s, depth).normalized;

            float diffuse = Mathf.Max(0f, Vector3.Dot(normal, LightDirection));
            float light = 0.40f + 0.85f * diffuse;

            // Reflet brillant le long du haut du flanc.
            Vector3 half3 = (LightDirection + Vector3.forward).normalized;
            float specular = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(normal, half3)), 28f);
            light += specular > 0.6f ? 0.2f : specular > 0.35f ? 0.08f : 0f;

            // Paliers nets, comme un dessin pixel art (pas de dégradé continu).
            return Mathf.Round(light * 7f) / 7f;
        }

        private void LightBody(Color[,] p)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (_light[x, y] > 0f && p[x, y].a > 0.01f)
                    {
                        // Les motifs très sombres (bandes noires) gardent leur netteté : le reflet ne les « troue » pas.
                        Color c = p[x, y];
                        float light = c.maxColorComponent < 0.2f ? Mathf.Min(_light[x, y], 1.05f) : _light[x, y];
                        p[x, y] = Shade(c, light);
                    }
                }
            }
        }

        private void DrawHead(Color[,] p)
        {
            if (GillX > 0)
            {
                float up = UpperAt(GillX);
                float down = LowerAt(GillX);
                int yMin = Mathf.RoundToInt(CenterY - down * 0.85f);
                int yMax = Mathf.RoundToInt(CenterY + up * 0.7f);
                for (int y = yMin; y <= yMax; y++)
                {
                    float dy = y - CenterY;
                    int x = Mathf.RoundToInt(GillX + dy * dy * 0.05f);
                    if (!InBody(x, y))
                    {
                        continue;
                    }

                    // Bord de l'opercule, ombre portée juste derrière, liseré clair devant.
                    p[x, y] = Color.Lerp(p[x, y], Color.black, GillStrength + 0.1f);
                    if (InBody(x - 1, y))
                    {
                        p[x - 1, y] = Color.Lerp(p[x - 1, y], Color.black, 0.16f);
                    }

                    if (InBody(x + 1, y))
                    {
                        p[x + 1, y] = Color.Lerp(p[x + 1, y], Color.white, 0.18f);
                    }

                    if (GillTint.HasValue)
                    {
                        for (int k = 1; k <= 4; k++)
                        {
                            if (InBody(x + k, y) && dy < up * 0.3f)
                            {
                                p[x + k, y] = Color.Lerp(p[x + k, y], GillTint.Value, 0.3f);
                            }
                        }
                    }
                }
            }

            if (MouthXEnd > MouthXStart)
            {
                for (int x = MouthXStart; x <= MouthXEnd; x++)
                {
                    int y = Mathf.RoundToInt(CenterY + MouthYOffset - (x - MouthXStart) * MouthSlope);
                    if (!InBody(x, y))
                    {
                        continue;
                    }

                    p[x, y] = MouthColor;
                    // Lèvre : un pixel clair juste au-dessus de la fente.
                    if (InBody(x, y + 1) && x > MouthXStart)
                    {
                        p[x, y + 1] = Color.Lerp(p[x, y + 1], Color.white, 0.22f);
                    }
                }

                // Coin de la bouche marqué.
                int cy = Mathf.RoundToInt(CenterY + MouthYOffset);
                if (InBody(MouthXStart - 1, cy))
                {
                    p[MouthXStart - 1, cy] = Color.Lerp(p[MouthXStart - 1, cy], MouthColor, 0.5f);
                }
            }
        }

        private void DrawEye(Color[,] p)
        {
            int ey = Mathf.RoundToInt(CenterY + EyeYOffset);
            int r = EyeRadius;

            // Orbite : un anneau sombre autour de l'œil, pris sur la couleur du corps.
            for (int x = EyeX - r - 1; x <= EyeX + r + 1; x++)
            {
                for (int y = ey - r - 1; y <= ey + r + 1; y++)
                {
                    float d = Mathf.Sqrt((x - EyeX) * (x - EyeX) + (y - ey) * (y - ey));
                    if (d > r + 0.5f && d <= r + 1.3f && InBody(x, y))
                    {
                        p[x, y] = Color.Lerp(p[x, y], Color.black, 0.32f);
                    }
                }
            }

            // Iris arrondi, plus sombre en haut (ombre de la paupière).
            for (int x = EyeX - r; x <= EyeX + r; x++)
            {
                for (int y = ey - r; y <= ey + r; y++)
                {
                    bool corner = Mathf.Abs(x - EyeX) == r && Mathf.Abs(y - ey) == r;
                    float k = r == 0 ? 0.5f : Mathf.InverseLerp(ey + r, ey - r, y);
                    Color iris = Color.Lerp(Iris * 0.6f, Color.Lerp(Iris, Color.white, 0.15f), k);
                    iris.a = 1f;
                    if (corner)
                    {
                        if (r >= 2)
                        {
                            continue;
                        }

                        // Œil de 3 pixels : coins à moitié fondus pour l'arrondir.
                        PixelArt.Set(p, x, y, p[x, y].a > 0.01f ? Color.Lerp(p[x, y], iris, 0.45f) : iris);
                        continue;
                    }

                    PixelArt.Set(p, x, y, iris);
                }
            }

            PixelArt.Set(p, EyeX, ey, Pupil);
            if (r >= 2)
            {
                PixelArt.Set(p, EyeX + 1, ey, Pupil);
                PixelArt.Set(p, EyeX, ey - 1, Pupil);
                PixelArt.Set(p, EyeX + 1, ey - 1, Pupil);
                PixelArt.Set(p, EyeX + 1, ey, Color.white);                 // reflet principal
                PixelArt.Set(p, EyeX - 1, ey - 1, Color.Lerp(Iris, Color.white, 0.6f));
            }
            else
            {
                PixelArt.Set(p, EyeX + 1, ey + 1, Color.white);
            }
        }

        private void DrawFin(Color[,] p, Fin fin)
        {
            const int raySpacing = 3;
            const float raySlant = 0.55f;   // les rayons penchent vers la queue en s'éloignant du corps
            int direction = fin.Side == FinSide.Up ? 1 : -1;
            float sideLight = fin.Side == FinSide.Up ? 1.05f : 0.82f;

            for (int x = fin.XRear; x <= fin.XFront; x++)
            {
                float t = Mathf.InverseLerp(fin.XRear, fin.XFront, x);
                float height;
                switch (fin.Shape)
                {
                    case FinShape.Falcate:
                        height = Mathf.Lerp(fin.HeightRear, fin.HeightFront, t * t * t);
                        break;
                    case FinShape.Rounded:
                        height = Mathf.Lerp(fin.HeightRear, fin.HeightFront, Mathf.SmoothStep(0f, 1f, t)) + Mathf.Sin(t * Mathf.PI) * 1.5f;
                        break;
                    default:
                        height = Mathf.Lerp(fin.HeightRear, fin.HeightFront, Mathf.SmoothStep(0f, 1f, t));
                        break;
                }

                // Bouts arrondis : la nageoire sort du corps et y rentre en douceur.
                height *= Mathf.Sqrt(Mathf.Clamp01((x - fin.XRear + 0.5f) / 2.5f));
                height *= Mathf.Sqrt(Mathf.Clamp01((fin.XFront - x + 0.8f) / 1.5f));

                float baseY = fin.Side == FinSide.Up ? CenterY + UpperAt(x) - 0.5f : CenterY - LowerAt(x) + 0.5f;
                int h = Mathf.RoundToInt(height);
                for (int k = 0; k <= h; k++)
                {
                    bool ray = Mathf.RoundToInt(x + k * raySlant) % raySpacing == 0;

                    // Nageoire épineuse : la membrane se creuse entre les épines, seules les épines touchent le bord.
                    if (fin.Spiny && k == h && !ray && h >= 2)
                    {
                        continue;
                    }

                    float u = h == 0 ? 1f : k / (float)h;
                    Color c = Color.Lerp(Color.Lerp(fin.Base, Color.black, 0.18f), fin.Base, Mathf.Clamp01(u * 2f));
                    if (ray)
                    {
                        c = Color.Lerp(c, fin.Ray, 0.8f);
                    }

                    if (k == h || (k == h - 1 && fin.Spiny && !ray))
                    {
                        c = fin.Edge;
                    }
                    else if (u > 0.7f)
                    {
                        c = Color.Lerp(c, fin.Edge, 0.35f);
                    }

                    PixelArt.Set(p, x, Mathf.RoundToInt(baseY + k * direction), Shade(c, sideLight));
                }
            }
        }

        private void DrawPectoral(Color[,] p, Pectoral fin)
        {
            float baseY = CenterY + fin.BaseYOffset;
            var pixels = new Dictionary<Vector2Int, Color>();

            for (int k = 0; k <= fin.Length; k++)
            {
                float u = k / (float)Mathf.Max(1, fin.Length);
                // Éventail : étroit à l'attache, large au milieu, pointe arrondie.
                float half = fin.Width * (0.65f + 0.5f * Mathf.Sin(Mathf.PI * u)) * (1f - u * u * u);
                int x = fin.BaseX - k;
                float cy = baseY - k * fin.Slope;
                int yMin = Mathf.RoundToInt(cy - half);
                int yMax = Mathf.RoundToInt(cy + half);
                for (int y = yMin; y <= yMax; y++)
                {
                    bool edge = k == fin.Length || y == yMin || (y == yMax && k > fin.Length / 2);
                    bool ray = Mathf.RoundToInt(y - cy + 100f) % 2 == 0;
                    Color c = edge ? fin.Edge : ray ? fin.Ray : fin.Base;
                    if (k == 0)
                    {
                        c = Color.Lerp(fin.Ray, Color.black, 0.3f);   // attache de la nageoire
                    }

                    pixels[new Vector2Int(x, y)] = c;
                }
            }

            // Petite ombre portée sur le corps, sous la nageoire.
            foreach (var pixel in pixels)
            {
                var below = new Vector2Int(pixel.Key.x, pixel.Key.y - 1);
                if (!pixels.ContainsKey(below) && PixelArt.IsFilled(p, below.x, below.y))
                {
                    p[below.x, below.y] = Color.Lerp(p[below.x, below.y], Color.black, 0.22f);
                }
            }

            // Nageoire translucide : on devine le corps à travers.
            foreach (var pixel in pixels)
            {
                Vector2Int at = pixel.Key;
                if (at.x < 0 || at.y < 0 || at.x >= Width || at.y >= Height)
                {
                    continue;
                }

                Color under = p[at.x, at.y];
                p[at.x, at.y] = under.a > 0.01f ? Color.Lerp(under, pixel.Value, 0.72f) : pixel.Value;
            }
        }

        private void DrawTail(Color[,] p)
        {
            Tail tail = Caudal;
            for (int x = tail.XTip; x <= tail.XBase; x++)
            {
                float t = Mathf.InverseLerp(tail.XBase, tail.XTip, x);
                float span = 1.5f + Mathf.Pow(t, 0.85f) * tail.Span;
                for (int y = 0; y < Height; y++)
                {
                    float dy = y - CenterY;
                    float ady = Mathf.Abs(dy);
                    bool inside = ady <= span;
                    switch (tail.Shape)
                    {
                        case TailShape.Forked:
                            inside &= t < 0.45f || ady >= (t - 0.45f) * span * 1.5f;
                            break;
                        case TailShape.Lunate:
                            inside &= t < 0.35f || ady >= (t - 0.35f) * (t - 0.35f) * span * 3.2f;
                            break;
                        case TailShape.Round:
                            inside &= t < 0.6f || ady <= span * Mathf.Sqrt(Mathf.Max(0f, 1f - (t - 0.6f) * (t - 0.6f) * 6f));
                            break;
                    }

                    if (!inside)
                    {
                        continue;
                    }

                    // Plus sombre au pédoncule, plus clair et translucide vers les pointes.
                    Color c = Color.Lerp(Color.Lerp(tail.Base, Color.black, 0.2f), tail.Base, Mathf.Clamp01(t * 2.5f));
                    if (tail.BaseTint.HasValue && t < 0.35f)
                    {
                        c = Color.Lerp(tail.BaseTint.Value, c, t / 0.35f);
                    }

                    float angle = Mathf.Atan2(dy, tail.XBase - x + 0.5f);
                    if (Mathf.Abs(Mathf.Sin(angle * 9f)) < 0.24f && t > 0.15f)
                    {
                        c = Color.Lerp(c, tail.Ray, 0.85f);
                    }

                    if (ady > span - 1.2f && t > 0.3f)
                    {
                        c = tail.Edge;
                    }

                    // Lobe du haut éclairé, lobe du bas dans l'ombre.
                    float light = 0.95f + 0.18f * Mathf.Clamp(dy / Mathf.Max(1f, span), -1f, 1f);
                    p[x, y] = Shade(c, light);
                }
            }
        }

        /// <summary>
        /// Contour sélectif : chaque pixel du contour prend une version sombre et froide de la couleur voisine,
        /// plus claire sur le haut (côté lumière) : la forme reste lisible sans le trait noir « découpé ».
        /// </summary>
        private void SelectiveOutline(Color[,] p)
        {
            var outline = new Color[Width, Height];
            var has = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (PixelArt.IsFilled(p, x, y))
                    {
                        continue;
                    }

                    Color sum = Color.clear;
                    int count = 0;
                    AddNeighbour(p, x + 1, y, ref sum, ref count);
                    AddNeighbour(p, x - 1, y, ref sum, ref count);
                    AddNeighbour(p, x, y + 1, ref sum, ref count);
                    AddNeighbour(p, x, y - 1, ref sum, ref count);
                    if (count == 0)
                    {
                        continue;
                    }

                    Color n = sum / count;
                    bool litSide = PixelArt.IsFilled(p, x, y - 1) && !PixelArt.IsFilled(p, x, y + 1);
                    Color dark = litSide
                        ? new Color(n.r * 0.50f, n.g * 0.52f, n.b * 0.60f)
                        : new Color(n.r * 0.28f, n.g * 0.30f, n.b * 0.40f);
                    Color o = Color.Lerp(dark, OutlineColor, litSide ? 0.2f : 0.45f);
                    o.a = 1f;
                    outline[x, y] = o;
                    has[x, y] = true;
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (has[x, y])
                    {
                        p[x, y] = outline[x, y];
                    }
                }
            }
        }

        private void AddNeighbour(Color[,] p, int x, int y, ref Color sum, ref int count)
        {
            if (PixelArt.IsFilled(p, x, y))
            {
                sum += p[x, y];
                count++;
            }
        }

        /// <summary>Profil lissé (Catmull-Rom) : des courbes douces entre les points de la fiche, pas des segments droits.</summary>
        private static float Sample(Vector2[] points, float x)
        {
            if (points == null || points.Length == 0 || x < points[0].x || x > points[points.Length - 1].x)
            {
                return 0f;
            }

            int last = points.Length - 1;
            for (int i = 0; i < last; i++)
            {
                if (x > points[i + 1].x)
                {
                    continue;
                }

                float u = Mathf.InverseLerp(points[i].x, points[i + 1].x, x);
                float p0 = points[Mathf.Max(i - 1, 0)].y;
                float p1 = points[i].y;
                float p2 = points[i + 1].y;
                float p3 = points[Mathf.Min(i + 2, last)].y;
                float v = 0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u + (-p0 + 3f * p1 - 3f * p2 + p3) * u * u * u);
                return Mathf.Max(0f, v);
            }

            return points[last].y;
        }

        // ---------- Motifs réutilisables ----------

        /// <summary>Bandes verticales (sergent-major, chirurgien bagnard) de la ligne du dos jusqu'à <paramref name="maxT"/>.</summary>
        public static Func<int, int, float, Color, Color> VerticalBars(int[] xs, int width, Color color, float maxT, float slant = 0f)
        {
            return (x, y, t, c) =>
            {
                if (t > maxT)
                {
                    return c;
                }

                foreach (int bx in xs)
                {
                    float shifted = x - (y * slant);
                    if (shifted >= bx && shifted < bx + width)
                    {
                        // Bords de la bande un peu adoucis vers le ventre.
                        float fade = Mathf.InverseLerp(maxT, maxT - 0.12f, t);
                        return Color.Lerp(c, color, Mathf.Lerp(0.55f, 1f, fade));
                    }
                }

                return c;
            };
        }

        /// <summary>Points aléatoires (taille 1 ou 2) dans une zone du corps.</summary>
        public static Action<DetailedFish, Color[,]> Spots(int count, int xMin, int xMax, float tMin, float tMax, Color color, Color? shadow, int seed, float bigChance = 0.3f)
        {
            return (fish, p) =>
            {
                var random = new System.Random(seed);
                for (int i = 0; i < count; i++)
                {
                    int x = random.Next(xMin, xMax);
                    int y = random.Next(0, fish.Height);
                    for (int attempt = 0; attempt < 20 && !(fish.InBody(x, y) && fish.BodyT(x, y) >= tMin && fish.BodyT(x, y) <= tMax); attempt++)
                    {
                        y = random.Next(0, fish.Height);
                    }

                    if (!fish.InBody(x, y))
                    {
                        continue;
                    }

                    p[x, y] = color;
                    if (random.NextDouble() < bigChance && fish.InBody(x + 1, y))
                    {
                        p[x + 1, y] = color;
                    }

                    if (shadow.HasValue && fish.InBody(x, y - 1))
                    {
                        p[x, y - 1] = Color.Lerp(p[x, y - 1], shadow.Value, 0.6f);
                    }
                }
            };
        }

        /// <summary>Lignes horizontales le long du corps, à des positions relatives t (0 = dos).</summary>
        public static Func<int, int, float, Color, Color> Lines(float[] ts, Color color, float strength, int xMin, int xMax)
        {
            return (x, y, t, c) =>
            {
                if (x < xMin || x > xMax)
                {
                    return c;
                }

                foreach (float lt in ts)
                {
                    if (Mathf.Abs(t - lt) < 0.045f)
                    {
                        return Color.Lerp(c, color, strength);
                    }
                }

                return c;
            };
        }
    }
}
