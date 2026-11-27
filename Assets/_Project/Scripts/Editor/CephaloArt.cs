using System;
using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les céphalopodes du carnet de l'épuisette, en volume, d'après les vraies espèces : pieuvres vues de dessus (manteau,
    /// yeux, huit bras qui s'enroulent au bout, toile éventuelle), calmars (manteau en fuseau, nageoires, bras et deux
    /// tentacules à massue), seiches (manteau ovale bordé d'une nageoire ondulante, yeux en W), nautile de profil.
    /// Dessins originaux.
    /// </summary>
    public static class CephaloArt
    {
        private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        private static float Hash(int x, int y, int seed = 0)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }

        public static readonly string[] Ids =
        {
            "octopus-cyanea", "octopus-vulgaris", "enteroctopus-dofleini", "hapalochlaena-lunulata", "vampyroteuthis-infernalis",
            "opisthoteuthis-depressa", "loligo-forbesii", "watasenia-scintillans", "architeuthis-dux", "sepia-latimanus",
            "sepia-pharaonis", "metasepia-pfefferi", "sepia-bandensis", "nautilus-pompilius",
        };

        public static bool TryGet(string id, out Color[,] pixels)
        {
            pixels = Draw(id);
            return pixels != null;
        }

        private static Color[,] Draw(string id)
        {
            switch (id)
            {
                case "octopus-cyanea": return Octopus(OctoCyanea());
                case "octopus-vulgaris": return Octopus(OctoVulgaris());
                case "enteroctopus-dofleini": return Octopus(OctoGiant());
                case "hapalochlaena-lunulata": return Octopus(OctoBlueRing());
                case "vampyroteuthis-infernalis": return Octopus(Vampire());
                case "opisthoteuthis-depressa": return Octopus(Umbrella());
                case "loligo-forbesii": return Squid(SquidVeined());
                case "watasenia-scintillans": return Squid(SquidFirefly());
                case "architeuthis-dux": return Squid(SquidGiant());
                case "sepia-latimanus": return Cuttle(CuttleBroadclub());
                case "sepia-pharaonis": return Cuttle(CuttlePharaoh());
                case "metasepia-pfefferi": return Cuttle(CuttleFlamboyant());
                case "sepia-bandensis": return Cuttle(CuttleStumpy());
                case "nautilus-pompilius": return Nautilus();
                default: return null;
            }
        }

        private static Func<float, float, bool> Taper(Vector2 a, Vector2 b, float r0, float r1)
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

        /// <summary>Un bras souple : il part dans une direction et s'enroule de plus en plus vers le bout.</summary>
        private static Vector2[] Arm(Vector2 root, float angle, float length, float curl, int steps = 16)
        {
            var pts = new Vector2[steps + 1];
            Vector2 p = root;
            float a = angle;
            for (int i = 0; i <= steps; i++)
            {
                pts[i] = p;
                float t = i / (float)steps;
                a += curl * t * t * 0.6f;
                p += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (length / steps);
            }

            return pts;
        }

        private static void DrawArm(VolumeCanvas v, Vector2[] pts, float r0, float r1, Color color, Func<int, int, Color, Color> tex, Color suckers)
        {
            for (int i = 0; i < pts.Length - 1; i++)
            {
                float t = i / (float)(pts.Length - 2);
                float r = Mathf.Lerp(r0, r1, t);
                v.Piece(Capsule(pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y, r), color, Mathf.Max(1f, r), tex, 0.85f);
                // Les ventouses se voient sur la fin du bras, là où il s'enroule.
                if (t > 0.55f && i % 2 == 0 && suckers.a > 0f) v.Dot(Mathf.RoundToInt(pts[i].x), Mathf.RoundToInt(pts[i].y), suckers);
            }
        }

        private static void Eye(VolumeCanvas v, Vector2 at, float r, Color iris, bool wPupil = false, bool round = false)
        {
            v.Piece(Ellipse(at.x, at.y, r, r * 0.85f), iris, Mathf.Max(1f, r * 0.8f));
            var pupil = new Color(0.06f, 0.05f, 0.06f);
            if (round) v.Piece(Ellipse(at.x, at.y, r * 0.55f, r * 0.55f), pupil, 1f);
            else if (wPupil)
            {
                for (int i = -2; i <= 2; i++) v.Dot(Mathf.RoundToInt(at.x + i * r * 0.3f), Mathf.RoundToInt(at.y + (Mathf.Abs(i) % 2 == 0 ? 0f : r * 0.3f)), pupil);
            }
            else v.Piece(Box(at.x - r * 0.7f, at.y - r * 0.18f, at.x + r * 0.7f, at.y + r * 0.18f), pupil, 1f);
            v.Dot(Mathf.RoundToInt(at.x - r * 0.4f), Mathf.RoundToInt(at.y - r * 0.4f), new Color(1f, 1f, 1f));
        }

        // ---------- Pieuvres ----------

        private sealed class Octo
        {
            public int W = 160, H = 150;
            public Vector2 Mantle = new Vector2(80f, 34f);
            public float MantleRx = 16f, MantleRy = 22f;
            public float ArmLen = 66f, ArmR0 = 4.4f, ArmR1 = 0.9f, Curl = 2f;
            public Color Skin, Suckers = new Color(0.95f, 0.85f, 0.8f);
            public Func<int, int, Color, Color> Tex;
            public Color Iris = new Color(0.85f, 0.7f, 0.3f);
            public bool Web;
            public Color WebColor;
            public Action<VolumeCanvas, Octo> Extras;
            public Color Outline = new Color(0.18f, 0.1f, 0.1f);
            public bool Fins;
        }

        /// <summary>
        /// Les huit bras d'une pieuvre posée : ils partent de sous la tête, tombent vers le bas en éventail, ondulent
        /// doucement et ne s'enroulent qu'au bout, chacun un peu différent.
        /// </summary>
        private static List<Vector2[]> OctoArms(Octo o)
        {
            var head = o.Mantle + new Vector2(0f, o.MantleRy * 0.95f);
            var arms = new List<Vector2[]>();
            for (int i = 0; i < 8; i++)
            {
                float t0 = i / 7f;
                float angle = Mathf.Lerp(38f, 142f, t0) * Mathf.Deg2Rad + (Hash(i, 3, 5) - 0.5f) * 0.15f;
                var root = head + new Vector2(Mathf.Lerp(-0.75f, 0.75f, t0) * o.MantleRx, 2f);
                float len = o.ArmLen * (0.82f + Hash(i, 4, 6) * 0.3f);
                float phase = Hash(i, 5, 7) * 6.28f, curlSide = i < 4 ? -1f : 1f;
                var pts = new Vector2[21];
                Vector2 p = root;
                for (int k = 0; k < pts.Length; k++)
                {
                    float t = k / (float)(pts.Length - 1);
                    float a = angle + Mathf.Sin(t * 5f + phase) * 0.28f * t + (t > 0.72f ? curlSide * (t - 0.72f) * (t - 0.72f) * o.Curl * 9f : 0f);
                    pts[k] = p;
                    p += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (len / (pts.Length - 1));
                }

                arms.Add(pts);
            }

            return arms;
        }

        private static Color[,] Octopus(Octo o)
        {
            var v = new VolumeCanvas(o.W, o.H);
            var head = o.Mantle + new Vector2(0f, o.MantleRy * 0.95f);
            // Les huit bras, qui tombent vers le bas derrière la tête, chacun avec sa courbe.
            List<Vector2[]> arms = OctoArms(o);

            if (o.Web)
            {
                // La toile entre les bras : un voile qui relie leurs premiers deux tiers.
                var poly = new List<Vector2>();
                foreach (Vector2[] arm in arms) poly.Add(arm[arm.Length * 2 / 3]);
                v.Piece((x, y) =>
                {
                    var p = new Vector2(x, y);
                    float d = Vector2.Distance(p, head);
                    float ang = Mathf.Atan2(y - head.y, x - head.x);
                    float reach = 0f;
                    foreach (Vector2 q in poly)
                    {
                        float qa = Mathf.Atan2(q.y - head.y, q.x - head.x);
                        float diff = Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, qa * Mathf.Rad2Deg));
                        reach = Mathf.Max(reach, Vector2.Distance(q, head) * Mathf.Clamp01(1f - diff / 40f));
                    }

                    return d < reach * 0.95f;
                }, o.WebColor, 4f, o.Tex, 0.8f);
            }

            foreach (int i in new[] { 0, 7, 1, 6, 2, 5, 3, 4 }) DrawArm(v, arms[i], o.ArmR0, o.ArmR1, o.Skin, o.Tex, o.Suckers);   // ceux du milieu par-dessus

            // Le manteau, bombé, un peu irrégulier.
            v.Piece((x, y) =>
            {
                float u = (x - o.Mantle.x) / o.MantleRx, t = (y - o.Mantle.y) / o.MantleRy;
                return u * u + t * t <= 1f + Mathf.Sin(Mathf.Atan2(t, u) * 3f) * 0.05f;
            }, o.Skin, o.MantleRx * 0.8f, o.Tex, 0.6f);
            if (o.Fins)
            {
                foreach (int s in new[] { -1, 1 }) v.Piece(Ellipse(o.Mantle.x + s * o.MantleRx * 0.95f, o.Mantle.y - o.MantleRy * 0.2f, 5f, 3f), o.Skin, 2f, o.Tex);
            }

            foreach (int s in new[] { -1, 1 }) Eye(v, head + new Vector2(s * o.MantleRx * 0.78f, -2f), Mathf.Max(2.2f, o.MantleRx * 0.22f), o.Iris);
            o.Extras?.Invoke(v, o);
            return v.Finish(o.Outline, 0.55f);
        }

        /// <summary>Poulpe cyané : brun-rouge marbré, peau grumeleuse, un ocelle sombre cerclé de clair à la base des bras.</summary>
        private static Octo OctoCyanea()
        {
            var o = new Octo
            {
                Skin = C(150, 84, 62),
                Tex = (x, y, c) => Mathf.PerlinNoise(x * 0.18f, y * 0.18f) > 0.62f ? C(196, 130, 96) : Hash(x, y, 1) > 0.9f ? Darker(c, 0.8f) : c,
                Iris = C(230, 190, 80),
            };
            return o;
        }

        /// <summary>Pieuvre commune : gris-brun marbré de taches blanches, bras épais.</summary>
        private static Octo OctoVulgaris() => new Octo
        {
            Skin = C(132, 110, 92), ArmR0 = 5f,
            Tex = (x, y, c) => Hash(x / 2, y / 2, 2) > 0.86f ? C(230, 220, 204) : Mathf.PerlinNoise(x * 0.12f, y * 0.12f) > 0.6f ? C(96, 76, 60) : c,
        };

        /// <summary>Poulpe géant du Pacifique : rouge orangé, peau ridée semée de petites taches blanches, très longs bras.</summary>
        private static Octo OctoGiant() => new Octo
        {
            W = 200, H = 196, Mantle = new Vector2(100f, 42f), MantleRx = 20f, MantleRy = 27f, ArmLen = 84f, ArmR0 = 5.6f, Curl = 3f,
            Skin = C(186, 76, 48),
            Tex = (x, y, c) => Hash(x, y, 3) > 0.92f ? C(246, 220, 200) : Mathf.Sin(x * 0.7f + y * 0.3f) > 0.85f ? Darker(c, 0.85f) : c,
        };

        /// <summary>Pieuvre à anneaux bleus : petite, beige jaunâtre, couverte d'anneaux bleu électrique cerclés de noir.</summary>
        private static Octo OctoBlueRing()
        {
            Func<int, int, Color, Color> rings = (x, y, c) =>
            {
                int gx = x / 7, gy = y / 7;
                if (Hash(gx, gy, 4) < 0.35f) return c;
                float cx = gx * 7 + 1.5f + Hash(gx, gy, 12) * 4f, cy = gy * 7 + 1.5f + Hash(gy, gx, 13) * 4f;
                float k = 0.7f + Hash(gx, gy, 14) * 0.5f;   // anneaux de tailles variées, éparpillés
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / k;
                return d < 1.4f ? C(150, 120, 60) : d < 2.4f ? C(50, 140, 255) : d < 3.2f ? C(20, 20, 24) : c;
            };
            return new Octo
            {
                W = 124, H = 118, Mantle = new Vector2(62f, 28f), MantleRx = 13f, MantleRy = 17f, ArmLen = 48f, ArmR0 = 3.4f, Curl = 3.4f,
                Skin = C(210, 170, 90), Tex = rings, Iris = C(200, 160, 60),
            };
        }

        /// <summary>Vampire des abysses : rouge très sombre, toile noire entre les bras, deux nageoires en oreilles, grands yeux bleus.</summary>
        private static Octo Vampire()
        {
            var o = new Octo
            {
                Skin = C(110, 26, 36), Web = true, WebColor = C(40, 14, 20), Fins = true, ArmR0 = 3.4f, Curl = 2.4f,
                Tex = (x, y, c) => Mathf.PerlinNoise(x * 0.2f, y * 0.2f) > 0.65f ? Darker(c, 1.2f) : c, Iris = C(80, 150, 230),
                Suckers = new Color(0, 0, 0, 0), Outline = C(20, 6, 10),
            };
            o.Extras = (v, k) =>
            {
                // Les bouts des bras, blancs.
                foreach (Vector2[] arm in OctoArms(k))
                {
                    Vector2 tip = arm[arm.Length - 1];
                    v.Piece(Ellipse(tip.x, tip.y, 1.6f, 1.6f), C(240, 240, 236), 1f);
                }
            };
            return o;
        }

        /// <summary>Pieuvre parapluie : corps aplati en ombrelle rose orangé, bras reliés par une toile jusqu'au bout, petites oreilles.</summary>
        private static Octo Umbrella() => new Octo
        {
            Mantle = new Vector2(80f, 52f), MantleRx = 22f, MantleRy = 16f, ArmLen = 44f, ArmR0 = 3f, Curl = 1.2f,
            Skin = C(232, 120, 100), Web = true, WebColor = C(214, 104, 90), Fins = true,
            Tex = (x, y, c) => Hash(x / 2, y / 2, 5) > 0.88f ? C(246, 170, 150) : c, Iris = C(40, 30, 30),
            Suckers = new Color(0, 0, 0, 0), Outline = C(90, 36, 30),
        };

        // ---------- Calmars ----------

        private sealed class Sq
        {
            public int W = 80, H = 170;
            public float Cx = 40f, Top = 6f, MantleL = 92f, MantleW = 11f;
            public float FinL = 40f, FinW = 22f;
            public float ArmLen = 34f, TentLen = 60f;
            public Color Skin;
            public Func<int, int, Color, Color> Tex;
            public float EyeR = 4f;
            public Color Iris = new Color(0.7f, 0.8f, 0.85f), Outline = new Color(0.2f, 0.1f, 0.1f);
        }

        private static Color[,] Squid(Sq s)
        {
            var v = new VolumeCanvas(s.W, s.H);
            float headY = s.Top + s.MantleL;
            // Les nageoires en losange, à l'arrière du manteau (en haut).
            v.Piece((x, y) =>
            {
                if (y < s.Top || y > s.Top + s.FinL) return false;
                float t = (y - s.Top) / s.FinL;
                float half = s.FinW * (t < 0.6f ? t / 0.6f : (1f - t) / 0.4f) + s.MantleW * 0.3f;
                return Mathf.Abs(x - s.Cx) <= half;
            }, Darker(s.Skin, 1.05f), 3f, s.Tex, 0.75f);
            // Les huit bras et les deux longs tentacules à massue, vers le bas.
            for (int i = 0; i < 8; i++)
            {
                float a = Mathf.Lerp(60f, 120f, i / 7f) * Mathf.Deg2Rad;
                Vector2[] arm = Arm(new Vector2(s.Cx, headY + 4f), a, s.ArmLen * (0.85f + Hash(i, 1, 2) * 0.2f), (i < 4 ? -1f : 1f) * 0.8f);
                DrawArm(v, arm, 2.2f, 0.7f, s.Skin, s.Tex, new Color(0, 0, 0, 0));
            }

            foreach (int side in new[] { -1, 1 })
            {
                Vector2[] tent = Arm(new Vector2(s.Cx + side * 2f, headY + 4f), (90f - side * 6f) * Mathf.Deg2Rad, s.TentLen, side * 0.5f, 20);
                DrawArm(v, tent, 1.2f, 1.2f, s.Skin, s.Tex, new Color(0, 0, 0, 0));
                Vector2 end = tent[tent.Length - 1];
                v.Piece(Ellipse(end.x, end.y, 2.6f, 4.4f), s.Skin, 2f, s.Tex);   // la massue
            }

            // Le manteau en fuseau, pointu en haut.
            v.Piece((x, y) =>
            {
                if (y < s.Top || y > headY) return false;
                float t = (y - s.Top) / s.MantleL;
                return Mathf.Abs(x - s.Cx) <= s.MantleW * Mathf.Sqrt(Mathf.Clamp01(t * 1.3f)) * (t > 0.9f ? 1f - (t - 0.9f) * 1.5f : 1f);
            }, s.Skin, s.MantleW * 0.8f, s.Tex, 0.6f);
            // La tête, avec ses gros yeux.
            v.Piece(Ellipse(s.Cx, headY + 2f, s.MantleW * 0.85f, s.MantleW * 0.7f), s.Skin, s.MantleW * 0.6f, s.Tex);
            foreach (int side in new[] { -1, 1 }) Eye(v, new Vector2(s.Cx + side * s.MantleW * 0.6f, headY + 2f), s.EyeR, s.Iris, round: true);
            return v.Finish(s.Outline, 0.55f);
        }

        /// <summary>Calmar veiné : rose translucide semé de chromatophores brun-rouge, longues nageoires en losange.</summary>
        private static Sq SquidVeined() => new Sq
        {
            Skin = C(226, 180, 170),
            Tex = (x, y, c) => Hash(x, y, 6) > 0.84f ? C(170, 70, 60) : Hash(x / 2, y / 2, 7) > 0.8f ? C(200, 120, 110) : c,
            Outline = C(110, 60, 56),
        };

        /// <summary>Calmar luciole : petit, bleu-noir, constellé de points lumineux bleu vif, gros yeux.</summary>
        private static Sq SquidFirefly() => new Sq
        {
            W = 60, H = 110, Cx = 30f, Top = 5f, MantleL = 56f, MantleW = 8f, FinL = 20f, FinW = 11f, ArmLen = 22f, TentLen = 30f,
            Skin = C(40, 50, 90),
            Tex = (x, y, c) => Hash(x, y, 8) > 0.88f ? C(120, 220, 255) : Hash(x, y, 9) > 0.96f ? C(220, 250, 255) : c,
            EyeR = 3.4f, Iris = C(70, 110, 150), Outline = C(10, 14, 30),
        };

        /// <summary>Encornet géant : grand manteau rougeâtre, petites nageoires, tentacules immenses, yeux énormes.</summary>
        private static Sq SquidGiant() => new Sq
        {
            W = 110, H = 240, Cx = 55f, Top = 6f, MantleL = 110f, MantleW = 15f, FinL = 26f, FinW = 18f, ArmLen = 52f, TentLen = 120f,
            Skin = C(176, 70, 56),
            Tex = (x, y, c) => Mathf.PerlinNoise(x * 0.15f, y * 0.08f) > 0.62f ? C(210, 110, 90) : Hash(x, y, 10) > 0.93f ? Darker(c, 0.8f) : c,
            EyeR = 6.4f, Iris = C(220, 220, 210), Outline = C(60, 20, 16),
        };

        // ---------- Seiches ----------

        private sealed class Cu
        {
            public int W = 96, H = 132;
            public float Cx = 48f, Cy = 52f, Rx = 24f, Ry = 40f;
            public Color Skin, Fin;
            public Func<int, int, Color, Color> Tex;
            public float ArmLen = 22f;
            public Color Iris = new Color(0.8f, 0.7f, 0.4f), Outline = new Color(0.16f, 0.1f, 0.08f);
        }

        private static Color[,] Cuttle(Cu s)
        {
            var v = new VolumeCanvas(s.W, s.H);
            float headY = s.Cy + s.Ry;
            // Huit bras courts en bouquet, vers le bas.
            for (int i = 0; i < 8; i++)
            {
                float a = Mathf.Lerp(62f, 118f, i / 7f) * Mathf.Deg2Rad;
                Vector2[] arm = Arm(new Vector2(s.Cx, headY + 4f), a, s.ArmLen * (0.8f + Hash(i, 2, 3) * 0.3f), (i < 4 ? -1f : 1f) * 1.2f, 10);
                DrawArm(v, arm, 2.8f, 0.8f, s.Skin, s.Tex, new Color(0, 0, 0, 0));
            }

            // La nageoire qui borde tout le manteau, ondulante.
            v.Piece((x, y) =>
            {
                float u = (x - s.Cx) / (s.Rx + 4.5f), t = (y - s.Cy) / (s.Ry + 3f);
                return u * u + t * t <= 1f + Mathf.Sin(y * 0.8f) * 0.03f && y < headY - 2f;
            }, s.Fin, 2f, (x, y, c) => (y % 4 == 0) ? Darker(c, 0.9f) : c, 0.8f);
            v.Piece(Ellipse(s.Cx, s.Cy, s.Rx, s.Ry), s.Skin, s.Rx * 0.7f, s.Tex, 0.6f);
            // La tête et ses yeux en W.
            v.Piece(Ellipse(s.Cx, headY, s.Rx * 0.8f, 7f), s.Skin, 5f, s.Tex);
            foreach (int side in new[] { -1, 1 }) Eye(v, new Vector2(s.Cx + side * s.Rx * 0.62f, headY - 1f), 3.6f, s.Iris, wPupil: true);
            return v.Finish(s.Outline, 0.55f);
        }

        /// <summary>Seiche à larges bras : brun marbré de blanc, fines rayures zébrées sur le dos.</summary>
        private static Cu CuttleBroadclub() => new Cu
        {
            Skin = C(150, 120, 90), Fin = C(210, 196, 170),
            Tex = (x, y, c) => Mathf.PerlinNoise(x * 0.15f, y * 0.15f) > 0.6f ? C(232, 222, 200) : Mathf.Sin(y * 0.9f + x * 0.2f) > 0.8f ? C(90, 70, 50) : c,
        };

        /// <summary>Seiche pharaon : zébrée de bandes ondulées brun pourpre et crème en travers du dos.</summary>
        private static Cu CuttlePharaoh() => new Cu
        {
            Skin = C(214, 190, 160), Fin = C(190, 170, 200),
            Tex = (x, y, c) => Mathf.Sin(y * 0.55f + Mathf.Sin(x * 0.25f) * 1.6f) > 0.35f ? C(120, 70, 90) : c,
        };

        /// <summary>Seiche flamboyante : petite et trapue, brun pourpre sombre, plaques blanches, bordure jaune et rouge.</summary>
        private static Cu CuttleFlamboyant() => new Cu
        {
            W = 80, H = 104, Cx = 40f, Cy = 40f, Rx = 20f, Ry = 28f, ArmLen = 18f,
            Skin = C(80, 40, 60), Fin = C(250, 210, 60),
            Tex = (x, y, c) => Mathf.PerlinNoise(x * 0.2f, y * 0.2f) > 0.66f ? C(240, 236, 226) : Mathf.PerlinNoise(x * 0.3f + 5f, y * 0.3f) > 0.68f ? C(220, 60, 70) : c,
            Iris = C(250, 210, 80), Outline = C(30, 14, 22),
        };

        /// <summary>Seiche trapue : petite, brun chaud, semée de petites taches blanches, bras courts.</summary>
        private static Cu CuttleStumpy() => new Cu
        {
            W = 72, H = 92, Cx = 36f, Cy = 36f, Rx = 17f, Ry = 24f, ArmLen = 14f,
            Skin = C(150, 100, 66), Fin = C(196, 160, 120),
            Tex = (x, y, c) => Hash(x / 2, y / 2, 11) > 0.84f ? C(240, 230, 214) : c,
        };

        // ---------- Nautile (de profil) ----------

        /// <summary>
        /// Nautile de profil, tourné vers la droite (d'après une vraie photo) : la grosse coquille enroulée, rayée en haut et
        /// à l'arrière de flammes brun-rouge ondulées qui partent de l'ombilic, blanche en bas ; à l'ouverture, le capuchon
        /// pointu vers l'avant, beige-brun couvert d'un réseau de points blancs ; dessous, les gaines blanches des tentacules
        /// empilées en lobes et les fins tentacules qui dépassent ; le petit œil sombre à la jonction capuchon-coquille.
        /// (Dessiné ouverture à gauche, puis retourné.)
        /// </summary>
        private static Color[,] Nautilus()
        {
            var v = new VolumeCanvas(140, 104);
            var c = new Vector2(92f, 48f);
            // Le contour suit une spirale logarithmique : très large à l'ouverture, il se resserre en tournant.
            float lip = 165f * Mathf.Deg2Rad, rMax = 46f, shrink = Mathf.Log(3.2f);
            Func<float, float, float> turn = (x, y) => Mathf.Repeat(Mathf.Atan2(y - c.y, x - c.x) - lip, Mathf.PI * 2f) / (Mathf.PI * 2f);
            Func<float, float, float> edge = (x, y) => rMax * Mathf.Exp(-shrink * turn(x, y));

            // La coquille.
            v.Piece((x, y) => Vector2.Distance(new Vector2(x, y), c) <= edge(x, y), C(246, 240, 230), 18f, (x, y, col) =>
            {
                float t = turn(x, y), r = Vector2.Distance(new Vector2(x, y), c), a = Mathf.Atan2(y - c.y, x - c.x);
                // Flammes brun-rouge, ondulées, rayonnant de l'ombilic ; elles couvrent le haut et l'arrière, pas le bas ni l'ouverture.
                bool upper = Mathf.Sin(a) < 0.35f;
                float flame = Mathf.Sin(a * 14f + Mathf.Log(Mathf.Max(1f, r)) * 5f + Mathf.Sin(r * 0.35f) * 0.8f);
                if (t > 0.18f && upper && flame > 0.15f && r > 6f)
                {
                    return Color.Lerp(C(160, 52, 30), C(200, 90, 50), Mathf.PerlinNoise(x * 0.2f, y * 0.2f));
                }

                return Hash(x, y, 13) > 0.95f ? Darker(col, 0.94f) : col;
            });
            v.Piece(Ellipse(c.x - 1f, c.y + 1f, 4f, 4f), C(110, 60, 40), 3f);   // l'ombilic

            // Le corps, clair, qui remplit toute l'ouverture : pas de vide entre la coquille et les tentacules.
            v.Piece(Union(Taper(new Vector2(74f, 54f), new Vector2(50f, 64f), 7f, 9f), Ellipse(56f, 66f, 11f, 9f)), C(232, 224, 212), 6f,
                (x, y, col) => Hash(x, y, 16) > 0.9f ? Darker(col, 0.92f) : col, 0.7f);

            // Les gaines blanches des tentacules, qui sortent du corps et s'empilent sous le capuchon, et les fins
            // tentacules qui dépassent au bout.
            for (int i = 0; i < 4; i++)
            {
                var from = new Vector2(62f - i * 1.5f, 57f + i * 4.6f);
                var to = new Vector2(28f + i * 4f, 62f + i * 5.5f);
                for (int k = 0; k < 3; k++)
                {
                    var tip = to + new Vector2(-8f - k * 3f - Hash(i, k, 3) * 4f, -2f + k * 2.5f);
                    v.Piece(Capsule(to.x, to.y, tip.x, tip.y, 0.7f), C(236, 222, 196), 1f, null, 0.85f, 0.85f);
                }

                v.Piece(Taper(from, to, 4.6f, 2f), C(240, 236, 228), 3f, (x, y, col) => (x + i) % 5 == 0 ? Darker(col, 0.9f) : col, 0.8f);
            }

            // Le capuchon : pointu vers l'avant, beige-brun, couvert d'un réseau serré de points blancs.
            v.Piece(Union(Taper(new Vector2(66f, 46f), new Vector2(22f, 56f), 11f, 2.5f), Ellipse(66f, 46f, 10f, 11f)), C(186, 130, 70), 7f, (x, y, col) =>
            {
                // Points blancs irréguliers, plus serrés vers le bout ; quelques taches plus sombres.
                float density = Mathf.Lerp(0.55f, 0.35f, Mathf.InverseLerp(66f, 22f, x));
                if (Hash(x, y, 15) > density + 0.2f && Hash(x + 1, y, 15) < 0.9f) return C(250, 244, 230);
                return Mathf.PerlinNoise(x * 0.25f, y * 0.25f) > 0.66f ? C(140, 90, 50) : col;
            });
            // L'œil, petit et sombre, à la jonction entre le capuchon et la coquille.
            v.Piece(Ellipse(72f, 53f, 3.4f, 3.2f), C(28, 22, 22), 2f);
            v.Dot(71, 52, new Color(0.8f, 0.8f, 0.8f));

            // Retourné : l'ouverture et le capuchon à droite, comme sur la photo.
            Color[,] p = v.Finish(C(90, 50, 34), 0.5f);
            int w = p.GetLength(0), h = p.GetLength(1);
            var flipped = new Color[w, h];
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) flipped[w - 1 - x, y] = p[x, y];
            return flipped;
        }
    }
}
