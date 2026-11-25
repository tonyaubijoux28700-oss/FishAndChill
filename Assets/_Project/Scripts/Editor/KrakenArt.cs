using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Légende des fjords de Norvège : le Kraken, monstre des marins norvégiens, si grand qu'on le prenait pour une île.
    /// De profil, 256×144, y depuis le haut. Trois versions dessinées pour le choix ; le calmar colossal est retenu :
    /// 0 « Calmar colossal » : long manteau rouge sombre aux nageoires en pointe, œil gigantesque, huit bras et deux immenses
    ///   tentacules à massues armées de crochets ;
    /// 1 « Pieuvre des abysses » : gros manteau bulbeux et verruqueux, huit bras épais qui s'enroulent, membrane entre leur
    ///   base, yeux qui luisent, un mât brisé serré dans un bras ;
    /// 2 « Kraken des légendes » : dos de rocher couvert de bernacles et d'algues avec une épave encastrée, plusieurs paires
    ///   d'yeux, bec béant plein de dents, forêt de tentacules dessous.
    /// </summary>
    public static class KrakenArt
    {
        public const int Width = 256;
        public const int Height = 144;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Ink = C(0.02f, 0.02f, 0.03f);
        private static readonly Color Sucker = C(0.82f, 0.70f, 0.66f);
        private static readonly Color Hook = C(0.86f, 0.82f, 0.70f);
        private static readonly Color Wood = C(0.40f, 0.28f, 0.16f);

        private static Color Skin(int v) => v == 0 ? C(0.30f, 0.06f, 0.09f) : v == 1 ? C(0.36f, 0.16f, 0.12f) : C(0.26f, 0.14f, 0.24f);
        private static Color SkinDark(int v) => v == 0 ? C(0.14f, 0.03f, 0.06f) : v == 1 ? C(0.12f, 0.05f, 0.04f) : C(0.08f, 0.04f, 0.09f);
        private static Color Belly(int v) => v == 0 ? C(0.54f, 0.26f, 0.28f) : v == 1 ? C(0.62f, 0.40f, 0.32f) : C(0.52f, 0.36f, 0.46f);
        private static Color Glow(int v) => v == 0 ? C(1f, 0.78f, 0.28f) : v == 1 ? C(1f, 0.56f, 0.20f) : C(0.50f, 0.95f, 0.70f);

        private static Color Mottle(int v, int x, int y, Color c)
        {
            float n = Noise(x / 5f + v * 9f, y / 5f);
            if (n < 0.28f) return Color.Lerp(c, SkinDark(v), 0.55f);
            if (n > 0.78f) return Color.Lerp(c, Belly(v), 0.25f);
            // Chromatophores : petits points sombres.
            return Hash(x, y) < 0.07f ? Darker(c, 0.65f) : c;
        }

        /// <summary>
        /// Un bras qui suit <paramref name="path"/>, de <paramref name="r0"/> à sa base jusqu'à une pointe fine, ventouses
        /// claires sur le côté <paramref name="side"/> (1 ou -1), crochets si demandé.
        /// </summary>
        private static void Arm(VolumeCanvas cv, int v, List<Vector2> path, float r0, float r1, float side, bool hooks = false)
        {
            cv.Piece(Tube(path, r0, r1), Skin(v), Mathf.Max(2f, r0 * 0.8f), (x, y, c) => Mottle(v, x, y, c), 0.82f, 0.4f);
            for (int i = 2; i < path.Count - 1; i += 2)
            {
                Vector2 dir = (path[i + 1] - path[i - 1]).normalized;
                Vector2 n = new Vector2(-dir.y, dir.x) * side;
                float t = i / (float)(path.Count - 1);
                float r = Mathf.Lerp(r0, r1, t);
                Vector2 at = path[i] + n * r * 0.7f;
                float sr = Mathf.Max(0.8f, r * 0.42f);
                cv.Piece(Ellipse(at.x, at.y, sr, sr * 0.85f), Sucker, sr, (x, y, c) => (x - at.x) * (x - at.x) + (y - at.y) * (y - at.y) < sr * sr * 0.2f ? Darker(c, 0.6f) : c, 0.9f, 0.55f);
                if (hooks && i % 4 == 0)
                {
                    cv.Piece(Tube(Curve(at, at + n * (sr + 2f), at + n * (sr + 2f) + dir * 2f, 5), 0.7f, 0.3f), Hook, 0.6f, null, 0.95f, 0.6f);
                }
            }
        }

        /// <summary>Courbe lisse qui passe par plusieurs points de contrôle (bout à bout de courbes simples).</summary>
        private static List<Vector2> Path(params Vector2[] pts)
        {
            var result = new List<Vector2>();
            for (int i = 0; i + 2 < pts.Length; i += 2)
            {
                List<Vector2> seg = Curve(pts[i], pts[i + 1], pts[i + 2], 12);
                if (result.Count > 0) seg.RemoveAt(0);
                result.AddRange(seg);
            }

            return result;
        }

        /// <summary>Le calmar colossal, la version retenue.</summary>
        public static Color[,] Draw() => Draw(0);

        /// <summary>Les trois Kraken dessinés pour le choix : 0 calmar colossal, 1 pieuvre des abysses, 2 Kraken des légendes.</summary>
        public static Color[,] Draw(int v)
        {
            var cv = new VolumeCanvas(Width, Height);
            var halos = new List<(Vector2 at, int r)>();
            Color skin = Skin(v), dark = SkinDark(v), belly = Belly(v);

            if (v == 0)
            {
                // ---------- Calmar colossal ----------
                // Les deux longues tentacules, derrière tout, et leurs massues armées de crochets tournants.
                Arm(cv, v, Path(new Vector2(170, 70), new Vector2(204, 26), new Vector2(232, 16), new Vector2(250, 12), new Vector2(246, 30)), 3.6f, 1.6f, 1f);
                Arm(cv, v, Path(new Vector2(170, 82), new Vector2(208, 132), new Vector2(236, 128), new Vector2(252, 122), new Vector2(244, 104)), 3.6f, 1.6f, -1f);
                foreach ((Vector2 club, float tilt) in new[] { (new Vector2(244, 24), 0.5f), (new Vector2(242, 112), -0.5f) })
                {
                    cv.Piece(Ellipse(club.x, club.y, 7f, 11f), skin, 4f, (x, y, c) => Mottle(v, x, y, c), 0.85f, 0.45f);
                    for (int k = 0; k < 6; k++)
                    {
                        Vector2 at = club + new Vector2(-4f + tilt * k, -8f + k * 3.2f);
                        cv.Piece(Tube(Curve(at, at + new Vector2(-3f, -1f), at + new Vector2(-4f, 2f), 5), 0.9f, 0.3f), Hook, 0.7f, null, 0.95f, 0.6f);
                        cv.Piece(Ellipse(at.x + 2f, at.y, 1.2f, 1f), Sucker, 1f, null, 0.95f, 0.6f);
                    }
                }

                // Les nageoires en losange au bout du manteau, nervurées et déchirées.
                cv.Piece((x, y) => Poly(new Vector2(44, 68), new Vector2(18, 34), new Vector2(2, 60), new Vector2(18, 100), new Vector2(44, 76))(x, y) && Noise(x / 3f + 7f, y / 3f) > 0.16f,
                    skin, 3f, (x, y, c) => (Mathf.Abs((x - 44f) * 0.6f + (y - 70f)) % 6f < 0.9f) ? Darker(c, 0.6f) : Mottle(v, x, y, c), 0.85f, 0.45f);

                // Le manteau, long et effilé vers l'arrière, crête plus claire, cicatrices rondes de cachalot.
                var spine = new Spine();
                for (int i = 0; i <= 40; i++)
                {
                    float t = i / 40f;
                    spine.Points.Add(new Vector2(28f + t * 130f, 70f - Mathf.Sin(t * Mathf.PI) * 5f));
                    spine.Radius.Add(4f + 23f * Mathf.SmoothStep(0f, 1f, t) - 6f * Mathf.Clamp01((t - 0.85f) / 0.15f));
                }

                cv.Piece(spine.Inside, skin, 13f, (x, y, c) =>
                {
                    spine.Frame(x + 0.5f, y + 0.5f, out float t, out float s, out float r);
                    Color k = Mottle(v, x, y, Color.Lerp(c, belly, Mathf.InverseLerp(0.1f, 0.9f, s) * 0.55f));
                    if (Mathf.Abs(s + 0.55f) < 0.06f) k = Color.Lerp(k, belly, 0.35f);   // la crête du dos
                    return k;
                }, 0.82f, 0.4f);
                foreach (Vector2 scar in new[] { new Vector2(70, 66), new Vector2(84, 74), new Vector2(102, 62), new Vector2(118, 78), new Vector2(132, 66) })
                {
                    float r = 3f + Hash((int)scar.x, 3) * 2f;
                    cv.Piece(Ring(scar.x, scar.y, r, r * 0.85f, 1f), Color.Lerp(belly, Color.white, 0.25f), 0.6f, null, 0.95f, 0.6f);
                }

                // La tête, le bec noir entre les bras, et l'œil gigantesque.
                cv.Piece(Ellipse(164f, 72f, 18f, 17f), skin, 8f, (x, y, c) => Mottle(v, x, y, c), 0.82f, 0.4f);
                cv.Piece(Poly(new Vector2(176, 70), new Vector2(190, 64), new Vector2(194, 72), new Vector2(186, 80)), C(0.06f, 0.04f, 0.04f), 2f, (x, y, c) => x > 188 ? C(0.20f, 0.16f, 0.14f) : c);
                Vector2 eye = new Vector2(160f, 68f);
                cv.Piece(Ellipse(eye.x, eye.y, 11f, 10f), dark, 4f);
                cv.Piece(Ring(eye.x, eye.y, 9.5f, 9f, 1.4f), C(0.30f, 0.08f, 0.08f), 1f);
                cv.Piece(Ellipse(eye.x, eye.y, 8f, 7.4f), Color.Lerp(Glow(v), C(0.60f, 0.36f, 0.06f), 0.3f), 3f, (x, y, c) =>
                {
                    float d = Mathf.Sqrt((x - eye.x) * (x - eye.x) + (y - eye.y) * (y - eye.y));
                    return Mathf.Repeat(Mathf.Atan2(y - eye.y, x - eye.x) * 6f, 1f) < 0.2f ? Darker(c, 0.8f) : d > 6f ? Darker(c, 0.85f) : c;
                }, 0.95f, 0.9f);
                cv.Piece(Ellipse(eye.x + 1f, eye.y, 4.2f, 5.8f), Ink, 2f);
                cv.Piece(Ellipse(eye.x - 3f, eye.y - 3f, 1.6f, 1.3f), Color.white, 1f, null, 1f, 1f);   // reflet
                halos.Add((eye, 14));

                // Les huit bras, épais, qui ondulent vers l'avant ; crochets sur les deux plus longs.
                for (int k = 0; k < 8; k++)
                {
                    float a = Mathf.Lerp(-0.85f, 0.85f, k / 7f);
                    var root = new Vector2(176f, 72f + a * 9f);
                    float len = 52f + (k % 3) * 9f + (k == 3 || k == 4 ? 8f : 0f);
                    Vector2 mid = root + new Vector2(Mathf.Cos(a) * len * 0.5f, Mathf.Sin(a) * len * 0.5f + (k % 2 == 0 ? -9f : 9f));
                    Vector2 end = root + new Vector2(Mathf.Cos(a * 1.25f) * len, Mathf.Sin(a * 1.25f) * len);
                    Vector2 curl = end + new Vector2(-7f, (k % 2 == 0 ? 9f : -9f));
                    Arm(cv, v, Path(root, mid, end, end + (curl - end) * 0.5f, curl), 7.5f, 1.2f, a < 0 ? 1f : -1f, k == 3 || k == 4);
                }
            }
            else if (v == 1)
            {
                // ---------- Pieuvre des abysses ----------
                Vector2 body = new Vector2(112f, 46f);
                // Bras de derrière d'abord, puis le manteau, puis les bras de devant.
                var arms = new List<Vector2[]>
                {
                    new[] { new Vector2(98, 72), new Vector2(60, 70), new Vector2(34, 92), new Vector2(14, 116), new Vector2(30, 128) },
                    new[] { new Vector2(104, 76), new Vector2(84, 110), new Vector2(60, 124), new Vector2(46, 138), new Vector2(64, 140) },
                    new[] { new Vector2(124, 76), new Vector2(150, 106), new Vector2(170, 132), new Vector2(190, 140), new Vector2(196, 124) },
                    new[] { new Vector2(130, 72), new Vector2(176, 68), new Vector2(206, 84), new Vector2(236, 100), new Vector2(240, 78) },
                    new[] { new Vector2(96, 66), new Vector2(54, 50), new Vector2(30, 30), new Vector2(18, 12), new Vector2(38, 10) },
                    new[] { new Vector2(132, 66), new Vector2(178, 44), new Vector2(208, 22), new Vector2(232, 8), new Vector2(246, 26) },
                    new[] { new Vector2(108, 78), new Vector2(112, 112), new Vector2(98, 134), new Vector2(118, 142), new Vector2(128, 128) },
                    new[] { new Vector2(118, 78), new Vector2(136, 118), new Vector2(150, 130), new Vector2(160, 142), new Vector2(144, 136) },
                };
                for (int k = 0; k < 4; k++) Arm(cv, v, Path(arms[k]), 9f, 1.2f, k % 2 == 0 ? 1f : -1f);

                // Membrane entre la base des bras.
                cv.Piece(Poly(new Vector2(84, 66), new Vector2(66, 86), new Vector2(110, 96), new Vector2(156, 86), new Vector2(140, 66)), Darker(skin, 0.85f), 3f, (x, y, c) => Mottle(v, x, y, c), 0.85f, 0.45f);

                // Le manteau bulbeux et verruqueux.
                cv.Piece(Ellipse(body.x - 4f, body.y - 6f, 40f, 34f), skin, 14f, (x, y, c) =>
                {
                    Color k = Mottle(v, x, y, c);
                    float w = Noise(x / 3f + 4f, y / 3f);
                    return w > 0.74f ? Color.Lerp(k, belly, 0.5f) : w < 0.22f ? Darker(k, 0.7f) : k;   // verrues
                }, 0.82f, 0.4f);
                cv.Piece(Ellipse(body.x + 2f, body.y + 22f, 26f, 14f), skin, 8f, (x, y, c) => Mottle(v, x, y, c), 0.82f, 0.4f);   // la tête

                // Le mât brisé serré dans un bras, avec un lambeau de voile.
                cv.Piece(Capsule(196f, 30f, 236f, 110f, 2.6f), Wood, 2f, (x, y, c) => (x + y) % 6 == 0 ? Darker(c, 0.7f) : c);
                cv.Piece((x, y) => Poly(new Vector2(206, 46), new Vector2(240, 54), new Vector2(232, 80), new Vector2(214, 70))(x, y) && Noise(x / 3f, y / 3f) > 0.25f,
                    C(0.72f, 0.68f, 0.58f), 1.5f, (x, y, c) => y % 6 == 0 ? Darker(c, 0.8f) : c, 0.85f, 0.5f);
                for (int k = 4; k < 8; k++) Arm(cv, v, Path(arms[k]), 9f, 1.2f, k % 2 == 0 ? 1f : -1f);
                // Le bras qui enserre le mât.
                Arm(cv, v, Path(new Vector2(150, 70), new Vector2(200, 60), new Vector2(222, 70), new Vector2(206, 84), new Vector2(214, 96)), 6f, 1.5f, 1f);

                // Les yeux, par-dessus tout : paupière lourde, iris qui luit, pupille en fente.
                foreach (Vector2 e in new[] { new Vector2(body.x - 16f, body.y + 12f), new Vector2(body.x + 20f, body.y + 12f) })
                {
                    cv.Piece(Ellipse(e.x, e.y + 2f, 8f, 5f), dark, 3f);
                    cv.Piece(Ellipse(e.x, e.y + 3f, 5.5f, 3.6f), Glow(v), 2f, null, 0.95f, 0.9f);
                    cv.Piece(Box(e.x - 4f, e.y + 2.5f, e.x + 4f, e.y + 3.5f), Ink, 1f);
                    halos.Add((e + new Vector2(0f, 3f), 9));
                }
            }
            else
            {
                // ---------- Kraken des légendes ----------
                // La forêt de tentacules, sous le dos de rocher.
                for (int k = 0; k < 10; k++)
                {
                    float x0 = 40f + k * 19f;
                    float sway = (k % 2 == 0 ? -1f : 1f) * (8f + Hash(k, 3) * 18f);
                    float drop = 20f + Hash(k, 5) * 24f;
                    Vector2 a0 = new Vector2(x0, 96f), a1 = a0 + new Vector2(sway * 0.6f, drop * 0.5f), a2 = a0 + new Vector2(sway * 1.4f, drop);
                    Vector2 a3 = a2 + new Vector2(sway * 0.8f, -6f + Hash(k, 7) * 10f), a4 = a3 + new Vector2(-sway * 0.3f, -8f - Hash(k, 9) * 8f);
                    Arm(cv, v, Path(a0, a1, a2, a3, a4),
                        7f, 1f, k % 2 == 0 ? 1f : -1f, k % 3 == 0);
                }

                // Le dos, comme une île de rocher couverte de bernacles et d'algues.
                Color rock = C(0.30f, 0.32f, 0.30f);
                cv.Piece((x, y) => Ellipse(128f, 92f, 112f, 50f)(x, y) && y < 100f, rock, 16f, (x, y, c) =>
                {
                    float n = Noise(x / 7f, y / 7f);
                    Color k = Color.Lerp(Darker(c, 0.7f), Darker(c, 1.2f), n);
                    if (Noise(x / 4f + 20f, y / 4f) > 0.7f) k = C(0.30f, 0.40f, 0.20f);   // algues
                    if (Hash(x, y) < 0.06f) k = C(0.70f, 0.68f, 0.62f);                     // bernacles
                    return k;
                }, 0.82f, 0.4f);

                // L'épave encastrée dans le dos : coque fendue, mât cassé.
                cv.Piece(Poly(new Vector2(60, 50), new Vector2(104, 44), new Vector2(98, 62), new Vector2(66, 64)), Wood, 3f, (x, y, c) => y % 5 == 0 ? Darker(c, 0.7f) : c);
                cv.Piece(Capsule(82f, 48f, 76f, 14f, 1.8f), Wood, 1.5f);
                cv.Piece(Capsule(76f, 18f, 96f, 22f, 1.2f), Wood, 1f);

                // La face : bec béant plein de dents, plusieurs paires d'yeux.
                cv.Piece(Ellipse(176f, 92f, 26f, 18f), skin, 8f, (x, y, c) => Mottle(v, x, y, c), 0.82f, 0.4f);
                cv.Piece(Poly(new Vector2(160, 92), new Vector2(196, 82), new Vector2(204, 100), new Vector2(168, 106)), C(0.14f, 0.04f, 0.08f), 2f);
                for (int k = 0; k < 9; k++)
                {
                    Vector2 up = Vector2.Lerp(new Vector2(162, 92), new Vector2(198, 84), k / 8f);
                    cv.Piece(Poly(up + new Vector2(-1.2f, 0f), up + new Vector2(1.2f, 0f), up + new Vector2(0.4f, 5f)), Hook, 0.7f, null, 0.95f, 0.75f);
                    Vector2 low = Vector2.Lerp(new Vector2(168, 104), new Vector2(202, 98), k / 8f);
                    cv.Piece(Poly(low + new Vector2(-1.2f, 0f), low + new Vector2(1.2f, 0f), low + new Vector2(0.4f, -5f)), Hook, 0.7f, null, 0.95f, 0.75f);
                }

                foreach (Vector2 e in new[] { new Vector2(150, 74), new Vector2(166, 70), new Vector2(184, 70), new Vector2(200, 74), new Vector2(158, 62), new Vector2(192, 62) })
                {
                    cv.Piece(Ellipse(e.x, e.y, 4f, 3.4f), dark, 2f);
                    cv.Piece(Ellipse(e.x, e.y, 2.6f, 2.2f), Glow(v), 1.5f, null, 0.95f, 0.95f);
                    halos.Add((e, 6));
                }
            }

            Color[,] p = cv.Finish(Ink, 0.55f);
            foreach ((Vector2 at, int r) in halos)
            {
                Halo(p, Mathf.RoundToInt(at.x), Height - 1 - Mathf.RoundToInt(at.y), r, Glow(v), 3f);
            }

            return p;
        }
    }
}
