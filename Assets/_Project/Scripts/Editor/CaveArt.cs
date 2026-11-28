using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// La grotte de Chiyo, sous la montagne de la baie d'Ha Long : la salle (416×288 px, 13×9 cases, y depuis le haut)
    /// avec stalactites, parois humides, une grande flaque d'eau bleue où brillent des lueurs vivantes, stalagmites et
    /// l'entrée en bas ; et la flaque vue de près pour l'épuisette (320×192, comme les autres flaques).
    /// </summary>
    public static class CaveArt
    {
        public const int Width = 416;
        public const int Height = 288;
        private const int WallBottom = 72;

        /// <summary>La flaque, en cases depuis le coin bas-gauche de la salle : centre et demi-axes.</summary>
        public static readonly Vector2 PoolCenter = new Vector2(6.5f, 3.75f);
        public static readonly Vector2 PoolRadii = new Vector2(3.4f, 1.8f);

        private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        private static float Hash(int x, int y, int seed = 0)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }

        private static readonly Color Rock = C(70, 64, 60);
        private static readonly Color RockDark = C(40, 36, 36);
        private static readonly Color RockLight = C(112, 104, 94);
        private static readonly Color Water = C(30, 90, 130);
        private static readonly Color Glow = C(120, 230, 240);

        /// <summary>Pieds des obstacles (en cases depuis le coin bas-gauche) : la flaque et les stalagmites.</summary>
        public static Rect[] Obstacles() => new[]
        {
            new Rect(PoolCenter.x - PoolRadii.x + 0.3f, PoolCenter.y - PoolRadii.y + 0.25f, PoolRadii.x * 2f - 0.6f, PoolRadii.y * 2f - 0.5f),
            new Rect(0.6f, 4.6f, 1.1f, 0.7f), new Rect(11.2f, 5.0f, 1.0f, 0.7f), new Rect(11.6f, 1.6f, 0.8f, 0.6f),
        };

        public static Color[,] Interior()
        {
            var v = new VolumeCanvas(Width, Height);
            float poolX = PoolCenter.x * 32f, poolY = Height - PoolCenter.y * 32f;
            float rx = PoolRadii.x * 32f, ry = PoolRadii.y * 32f;

            // Le sol de roche en nappes douces, plus clair et bleuté autour de la flaque (l'eau éclaire la pierre mouillée).
            v.Piece(Box(-1, -1, Width + 1, Height + 1), Rock, 1f, (x, y, c) =>
            {
                float n = Mathf.PerlinNoise(x * 0.035f, y * 0.04f) * 0.7f + Mathf.PerlinNoise(x * 0.11f + 7f, y * 0.11f) * 0.3f;
                Color s = n < 0.42f ? Color.Lerp(RockDark, Rock, n / 0.42f) : Color.Lerp(Rock, RockLight, Mathf.Clamp01((n - 0.42f) / 0.4f) * 0.7f);
                float d = PoolDistance(x, y, poolX, poolY, rx, ry);
                s = Color.Lerp(s, Color.Lerp(Water, Glow, 0.25f), Mathf.Clamp01((1.7f - d) * 0.3f));
                return Hash(x, y, 2) > 0.997f ? Glow : s;
            }, 1f, 0.9f);

            // La paroi du fond, humide, avec des gouttes qui brillent.
            v.Piece((x, y) => y < WallBottom + Mathf.PerlinNoise(x * 0.03f, 1f) * 14f - 4f, RockDark, 6f, (x, y, c) =>
            {
                float n = Mathf.PerlinNoise(x * 0.06f, y * 0.04f);
                Color s = Color.Lerp(c, RockLight, Mathf.Clamp01((n - 0.5f) * 1.4f));
                return Hash(x / 2, y / 3, 4) > 0.985f ? Color.Lerp(s, Glow, 0.5f) : s;
            }, 0.9f, 0.5f);

            // Stalactites en grappes inégales : longues, courtes, penchées, une goutte claire au bout.
            float sx = 10f;
            for (int i = 0; sx < Width - 8f; i++)
            {
                float len = 10f + Hash(i, 2, 3) * Hash(i, 3, 3) * 46f, r = 3f + Hash(i, 4, 3) * 5f, lean = (Hash(i, 5, 3) - 0.5f) * 8f;
                float top = WallBottom - 8f + Mathf.PerlinNoise(sx * 0.03f, 1f) * 14f;
                var a = new Vector2(sx, top);
                var b = new Vector2(sx + lean, top + len);
                v.Piece(Taper(a, b, r, 0.7f), Color.Lerp(RockLight, Rock, Hash(i, 6, 3) * 0.5f), r,
                    (x, y, c) => Vector2.Distance(new Vector2(x, y), b) < 2f ? Color.Lerp(c, Glow, 0.5f) : c);
                sx += 6f + Hash(i, 7, 3) * 22f;
            }

            // Stalagmites bosselées dans les coins (leur pied est un obstacle).
            foreach ((float gx, float gy, float h) in new[] { (36f, 128f, 40f), (374f, 116f, 46f), (382f, 238f, 30f) })
            {
                v.Piece(Union(Ellipse(gx, gy - h * 0.18f, h * 0.36f, h * 0.2f), Ellipse(gx + 2f, gy - h * 0.48f, h * 0.24f, h * 0.24f),
                    Ellipse(gx - 1f, gy - h * 0.76f, h * 0.14f, h * 0.2f), Ellipse(gx + h * 0.22f, gy - h * 0.12f, h * 0.14f, h * 0.12f)), RockLight, 7f,
                    (x, y, c) => Mathf.PerlinNoise(x * 0.2f, y * 0.1f) > 0.62f ? Darker(c, 0.86f) : Hash(x, y, 11) > 0.97f ? Color.Lerp(c, Glow, 0.4f) : c);
            }

            // La flaque au bord irrégulier : margelle de roche mouillée, eau profonde, lueurs vivantes en réseau.
            v.Piece((x, y) => PoolDistance(x, y, poolX, poolY, rx + 9f, ry + 7f) < 1f, RockLight, 6f,
                (x, y, c) => Mathf.PerlinNoise(x * 0.15f, y * 0.15f) > 0.6f ? Darker(c, 0.8f) : c);
            v.Piece((x, y) => PoolDistance(x, y, poolX, poolY, rx, ry) < 1f, Water, 2f, (x, y, c) =>
            {
                float d = PoolDistance(x, y, poolX, poolY, rx, ry);
                Color w = Color.Lerp(Color.Lerp(Water, Glow, 0.3f), C(10, 30, 60), d * d);
                return Caustic(x, y, w);
            }, 0.4f, 0.8f);

            // L'entrée : un passage de lumière du jour au bas de la salle.
            v.Piece(Ellipse(208f, Height + 6f, 34f, 22f), C(150, 170, 150), 3f, (x, y, c) => Color.Lerp(c, Rock, Mathf.Clamp01((Height - y) / 22f)));
            Color edge = C(24, 20, 20);
            v.Piece(Union(Box(-1, -1, 7, Height + 1), Box(Width - 7, -1, Width + 1, Height + 1)), edge, 2f);
            return v.Finish(C(14, 12, 12), 0.4f);
        }

        /// <summary>Distance « normalisée » au bord de la flaque (1 = le bord), bord ondulé par un bruit selon l'angle.</summary>
        private static float PoolDistance(float x, float y, float cx, float cy, float rx, float ry)
        {
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            float a = Mathf.Atan2(dy, dx);
            float wobble = 1f + (Mathf.PerlinNoise(Mathf.Cos(a) * 1.4f + 3f, Mathf.Sin(a) * 1.4f + 3f) - 0.5f) * 0.26f;
            return Mathf.Sqrt(dx * dx + dy * dy) / wobble;
        }

        /// <summary>Reflets de lumière au fond de l'eau : un réseau de fils clairs (deux bruits qui se croisent) et quelques lueurs.</summary>
        private static Color Caustic(int x, int y, Color water)
        {
            float web = Mathf.Abs(Mathf.PerlinNoise(x * 0.045f, y * 0.06f) - Mathf.PerlinNoise(x * 0.06f + 9f, y * 0.045f + 9f));
            Color w = web < 0.025f ? Color.Lerp(water, Glow, 0.45f) : web < 0.05f ? Color.Lerp(water, Glow, 0.18f) : water;
            return Hash(x, y, 8) > 0.995f ? Color.white : Hash(x, y, 9) > 0.99f ? Color.Lerp(w, Glow, 0.8f) : w;
        }

        private static System.Func<float, float, bool> Taper(Vector2 a, Vector2 b, float r0, float r1)
        {
            Vector2 ab = b - a;
            float len2 = Mathf.Max(0.0001f, ab.sqrMagnitude);
            return (x, y) =>
            {
                var p = new Vector2(x, y);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                return (p - (a + ab * t)).magnitude <= Mathf.Lerp(r0, r1, t);
            };
        }

        /// <summary>La flaque de la grotte vue de près (320×192) : eau bleue lumineuse en réseau de reflets, rochers sombres où se cacher.</summary>
        public static Color[,] Pool()
        {
            const int w = 320, h = 192;
            var v = new VolumeCanvas(w, h);
            v.Piece(Box(-1, -1, w + 1, h + 1), C(16, 40, 64), 1f, (x, y, c) =>
                Caustic(x, y, Color.Lerp(c, C(34, 96, 120), Mathf.PerlinNoise(x * 0.03f, y * 0.04f))), 1f, 0.9f);
            foreach ((float x0, float y0, float r) in new[] { (46f, 46f, 22f), (206f, 62f, 18f), (124f, 150f, 20f), (276f, 160f, 24f) })
            {
                v.Piece((x, y) => PoolDistance(x, y, x0, y0, r, r * 0.8f) < 1f, C(56, 52, 54), r * 0.9f,
                    (x, y, c) => Mathf.PerlinNoise(x * 0.18f, y * 0.18f) > 0.6f ? Darker(c, 0.82f) : Hash(x, y, 5) > 0.96f ? Color.Lerp(c, Glow, 0.4f) : c, 0.6f);
            }

            return v.Finish(C(8, 14, 20), 0.35f);
        }
    }
}
