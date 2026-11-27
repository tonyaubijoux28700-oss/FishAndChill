using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les crabes du carnet de l'épuisette, vus de dessus, tête en haut, dessinés en volume d'après les vraies espèces :
    /// carapace (forme, dents, épines, motif), pinces articulées (bras, avant-bras, paume, doigts aux pointes sombres),
    /// pattes en trois segments, yeux sur pédoncules, palettes de nage des crabes nageurs. Dessins originaux.
    /// </summary>
    public static class CrabArt
    {
        private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        private static float Hash(int x, int y, int seed = 0)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }

        private static Vector2 Dir(float degrees) => new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

        /// <summary>Ellipse tournée (angle en degrés, 0 = vers la droite, y vers le bas).</summary>
        private static Func<float, float, bool> Oval(Vector2 c, float along, float across, float degrees)
        {
            Vector2 d = Dir(degrees);
            return (x, y) =>
            {
                float dx = x - c.x, dy = y - c.y;
                float u = dx * d.x + dy * d.y, v = -dx * d.y + dy * d.x;
                return (u * u) / (along * along) + (v * v) / (across * across) <= 1f;
            };
        }

        /// <summary>Un crabe : tout ce qui change d'une espèce à l'autre.</summary>
        private sealed class Crab
        {
            public int W = 120, H = 96;
            public float Cx = 60f, Cy = 56f;

            public Func<float, float, bool> Carapace;
            public Color Shell;
            public Func<int, int, Color, Color> ShellTex;
            public float ShellBulge = 9f;

            // Pattes (côté droit ; le gauche est en miroir). Attaches relatives au centre, angles en degrés (0 = droite, + = vers l'arrière).
            public Vector2[] LegAttach;
            public float[] LegAngle;
            public float L1 = 12f, L2 = 9f, L3 = 8f, LegR = 3f, Bend = 28f;
            public Color Leg;
            public Func<int, int, Color, Color> LegTex;
            public bool Paddles;          // dernière paire en palettes de nage
            public Color LegTip = new Color(0.2f, 0.15f, 0.12f);

            // Pinces.
            public Vector2 ClawAttach = new Vector2(18f, -10f);
            public float ArmLen = 12f, ArmR = 4f, ArmAngle = -40f, ForeLen = 8f, ForeR = 4.5f, ForeAngle = -95f;
            public float PalmLenR = 18f, PalmWidR = 7f, PalmLenL = 18f, PalmWidL = 7f, PalmAngle = -112f, FingerLen = 9f;
            public Color Palm, Finger, FingerTip = new Color(0.12f, 0.08f, 0.08f);
            public Func<int, int, Color, Color> ClawTex;

            // Yeux sur pédoncules.
            public Vector2 EyeFrom = new Vector2(6f, -18f), EyeTo = new Vector2(8f, -22f);
            public float EyeR = 1.6f;
            public Color Stalk, Eye = new Color(0.08f, 0.06f, 0.06f);

            public Color Outline = new Color(0.16f, 0.10f, 0.08f);
            public Action<VolumeCanvas, Crab> Extras;   // détails propres à l'espèce (dents, épines, coquille…)
            public Action<VolumeCanvas, Crab> Under;    // posé avant les pattes (coquille du bernard-l'ermite)
        }

        public static bool TryGet(string id, out Color[,] pixels)
        {
            Crab crab = Spec(id);
            pixels = crab != null ? Paint(crab) : null;
            return pixels != null;
        }

        /// <summary>Les espèces dessinées ici.</summary>
        public static readonly string[] Ids =
        {
            "scylla-serrata", "portunus-pelagicus", "paralithodes-camtschaticus", "macrocheira-kaempferi", "chionoecetes-opilio",
            "austruca-mjoebergi", "zebrida-adamsii", "hoplophrys-oatesi", "sylviocarcinus-pictus", "dardanus-megistos",
            "ocypode-ceratophthalmus", "birgus-latro", "mictyris-longicarpus",
        };

        // ---------- Le dessin ----------

        private static Color[,] Paint(Crab k)
        {
            var v = new VolumeCanvas(k.W, k.H);
            var center = new Vector2(k.Cx, k.Cy);
            k.Under?.Invoke(v, k);

            // Pattes, des deux côtés, de l'arrière vers l'avant (celles de devant passent par-dessus).
            for (int i = k.LegAttach.Length - 1; i >= 0; i--)
            {
                foreach (int s in new[] { -1, 1 })
                {
                    DrawLeg(v, k, center, i, s);
                }
            }

            // Bras et avant-bras des pinces, sous la carapace.
            foreach (int s in new[] { -1, 1 }) DrawArm(v, k, center, s);

            if (k.Carapace != null) v.Piece(k.Carapace, k.Shell, k.ShellBulge, k.ShellTex, 0.6f);
            k.Extras?.Invoke(v, k);

            // Les paumes et les doigts, devant la carapace.
            foreach (int s in new[] { -1, 1 }) DrawHand(v, k, center, s);

            // Pédoncules et yeux.
            foreach (int s in new[] { -1, 1 })
            {
                Vector2 a = center + new Vector2(s * k.EyeFrom.x, k.EyeFrom.y), b = center + new Vector2(s * k.EyeTo.x, k.EyeTo.y);
                v.Piece(Capsule(a.x, a.y, b.x, b.y, 1.1f), k.Stalk, 1f);
                v.Piece(Ellipse(b.x, b.y, k.EyeR, k.EyeR), k.Eye, 1.2f);
                v.Dot(Mathf.RoundToInt(b.x - 0.5f), Mathf.RoundToInt(b.y - 0.6f), new Color(0.9f, 0.9f, 0.85f));
            }

            return v.Finish(k.Outline, 0.6f);
        }

        private static float SideAngle(int s, float degrees) => s > 0 ? degrees : 180f - degrees;

        private static void DrawLeg(VolumeCanvas v, Crab k, Vector2 center, int i, int s)
        {
            Vector2 p0 = center + new Vector2(s * k.LegAttach[i].x, k.LegAttach[i].y);
            float a = k.LegAngle[i];
            Vector2 p1 = p0 + Dir(SideAngle(s, a)) * k.L1;
            Vector2 p2 = p1 + Dir(SideAngle(s, a + k.Bend)) * k.L2;
            bool paddle = k.Paddles && i == k.LegAttach.Length - 1;
            Vector2 p3 = p2 + Dir(SideAngle(s, a + k.Bend * 1.7f)) * k.L3;
            Color joint = Darker(k.Leg, 0.82f);
            v.Piece(Capsule(p0.x, p0.y, p1.x, p1.y, k.LegR), k.Leg, k.LegR, k.LegTex, 0.7f);
            v.Piece(Capsule(p1.x, p1.y, p2.x, p2.y, k.LegR * 0.85f), k.Leg, k.LegR, k.LegTex, 0.7f);
            v.Piece(Ellipse(p1.x, p1.y, k.LegR * 0.7f, k.LegR * 0.7f), joint, 1f);
            if (paddle)
            {
                // Palette de nage : un ovale plat au bout de la patte.
                v.Piece(Oval(Vector2.Lerp(p2, p3, 0.55f), k.L3 * 0.6f, k.LegR * 1.3f, SideAngle(s, a + k.Bend * 1.7f)), k.Leg, 2f, k.LegTex, 0.7f);
                return;
            }

            Vector2 mid = Vector2.Lerp(p2, p3, 0.55f);
            v.Piece(Capsule(p2.x, p2.y, mid.x, mid.y, k.LegR * 0.7f), k.Leg, k.LegR, k.LegTex, 0.7f);
            v.Piece(Capsule(mid.x, mid.y, p3.x, p3.y, k.LegR * 0.4f), k.LegTip, 1f, null, 0.7f);   // dactyle pointu
        }

        private static Vector2 Elbow(Crab k, Vector2 center, int s, out Vector2 shoulder, out Vector2 wrist)
        {
            shoulder = center + new Vector2(s * k.ClawAttach.x, k.ClawAttach.y);
            Vector2 elbow = shoulder + Dir(SideAngle(s, k.ArmAngle)) * k.ArmLen;
            wrist = elbow + Dir(SideAngle(s, k.ForeAngle)) * k.ForeLen;
            return elbow;
        }

        private static void DrawArm(VolumeCanvas v, Crab k, Vector2 center, int s)
        {
            Vector2 elbow = Elbow(k, center, s, out Vector2 shoulder, out Vector2 wrist);
            v.Piece(Capsule(shoulder.x, shoulder.y, elbow.x, elbow.y, k.ArmR), k.Palm, k.ArmR, k.ClawTex, 0.7f);
            v.Piece(Capsule(elbow.x, elbow.y, wrist.x, wrist.y, k.ForeR), k.Palm, k.ForeR, k.ClawTex, 0.7f);
        }

        private static void DrawHand(VolumeCanvas v, Crab k, Vector2 center, int s)
        {
            Elbow(k, center, s, out _, out Vector2 wrist);
            float len = s > 0 ? k.PalmLenR : k.PalmLenL, wid = s > 0 ? k.PalmWidR : k.PalmWidL;
            float angle = SideAngle(s, k.PalmAngle);
            Vector2 d = Dir(angle);
            Vector2 palm = wrist + d * len * 0.45f;
            v.Piece(Oval(palm, len * 0.55f, wid, angle), k.Palm, wid * 0.9f, k.ClawTex, 0.65f);

            // Doigts : le fixe prolonge la paume côté intérieur, le mobile (dactyle) part du côté extérieur, un peu ouvert.
            Vector2 tip = palm + d * len * 0.5f;
            var inner = new Vector2(-d.y, d.x) * (s > 0 ? -1f : 1f);   // vers l'intérieur du crabe
            float f = k.FingerLen * (len / Mathf.Max(1f, k.PalmLenR));
            Vector2 fixedBase = tip + inner * wid * 0.35f, fixedEnd = fixedBase + d * f + inner * f * 0.12f;
            Vector2 moveBase = tip - inner * wid * 0.4f, moveEnd = moveBase + d * f * 0.95f + inner * f * 0.28f;
            float fr = Mathf.Max(1.1f, wid * 0.28f);
            v.Piece(Capsule(fixedBase.x, fixedBase.y, fixedEnd.x, fixedEnd.y, fr), k.Finger, fr, null, 0.7f);
            v.Piece(Capsule(moveBase.x, moveBase.y, moveEnd.x, moveEnd.y, fr), k.Finger, fr, null, 0.7f);
            v.Piece(Ellipse(fixedEnd.x, fixedEnd.y, fr * 0.9f, fr * 0.9f), k.FingerTip, 1f);
            v.Piece(Ellipse(moveEnd.x, moveEnd.y, fr * 0.9f, fr * 0.9f), k.FingerTip, 1f);
        }

        /// <summary>Petites dents le long du bord avant de la carapace (une ellipse), des deux côtés.</summary>
        private static void Teeth(VolumeCanvas v, Vector2 c, float rx, float ry, float fromDeg, float toDeg, int count, float length, float r, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, count == 1 ? 0f : i / (float)(count - 1)) * Mathf.Deg2Rad;
                foreach (int s in new[] { -1, 1 })
                {
                    var edge = new Vector2(c.x + s * Mathf.Cos(a) * rx, c.y + Mathf.Sin(a) * ry);
                    Vector2 out1 = (edge - c).normalized;
                    Vector2 tip = edge + out1 * length;
                    v.Piece(Capsule(edge.x - out1.x, edge.y - out1.y, tip.x, tip.y, r), color, 1f, null, 0.8f);
                }
            }
        }

        // ---------- Les espèces ----------

        private static Crab Spec(string id)
        {
            switch (id)
            {
                case "scylla-serrata": return MudCrab();
                case "portunus-pelagicus": return BlueSwimmer();
                case "paralithodes-camtschaticus": return KingCrab();
                case "macrocheira-kaempferi": return SpiderCrab();
                case "chionoecetes-opilio": return SnowCrab();
                case "austruca-mjoebergi": return FiddlerCrab();
                case "zebrida-adamsii": return UrchinCrab();
                case "hoplophrys-oatesi": return SoftCoralCrab();
                case "sylviocarcinus-pictus": return PaintedCrab();
                case "dardanus-megistos": return HermitCrab();
                case "ocypode-ceratophthalmus": return GhostCrab();
                case "birgus-latro": return CoconutCrab();
                case "mictyris-longicarpus": return SoldierCrab();
                default: return null;
            }
        }

        /// <summary>Crabe de palétuvier : carapace large et lisse vert olive, neuf dents de chaque côté, pinces massives.</summary>
        private static Crab MudCrab()
        {
            Color olive = C(92, 104, 62);
            var k = new Crab
            {
                W = 124, H = 100, Cx = 62f, Cy = 58f,
                Carapace = (x, y) => { float u = (x - 62f) / 31f, w = (y - 60f) / 21f; return u * u + w * w <= 1f && y > 41f; },
                Shell = olive,
                ShellTex = (x, y, c) =>
                {
                    float gx = Mathf.Abs(x - 62f), gy = y - 58f;
                    bool groove = Mathf.Abs(gy + 1f - gx * gx * 0.012f) < 0.55f && gx > 4f && gx < 16f;   // léger sillon courbe de chaque côté
                    return groove ? Darker(c, 0.9f) : Hash(x / 2, y / 2, 3) > 0.8f ? Darker(c, 0.92f) : c;
                },
                ShellBulge = 10f,
                LegAttach = new[] { new Vector2(22f, -3f), new Vector2(25f, 4f), new Vector2(23f, 11f), new Vector2(17f, 17f) },
                LegAngle = new[] { -8f, 14f, 36f, 62f },
                L1 = 13f, L2 = 9f, L3 = 9f, LegR = 3.2f, Bend = 26f,
                Leg = C(104, 116, 70), Paddles = true, LegTip = C(150, 96, 56),
                ClawAttach = new Vector2(20f, -11f), ArmLen = 12f, ArmR = 4.6f, ArmAngle = -30f, ForeLen = 8f, ForeR = 5.2f, ForeAngle = -100f,
                PalmLenR = 20f, PalmWidR = 9f, PalmLenL = 18f, PalmWidL = 8f, PalmAngle = -158f, FingerLen = 10f,
                Palm = C(98, 110, 66), Finger = C(178, 104, 58), FingerTip = C(30, 20, 18),
                ClawTex = (x, y, c) => Hash(x, y, 4) > 0.9f ? Darker(c, 0.88f) : c,
                EyeFrom = new Vector2(6f, -16f), EyeTo = new Vector2(9f, -20f), EyeR = 1.8f, Stalk = olive,
                Outline = C(36, 40, 24),
            };
            k.Extras = (v, c) =>
            {
                Teeth(v, new Vector2(62f, 60f), 31f, 21f, -150f + 180f - 180f, -100f, 9, 2.2f, 1.3f, olive);   // bord avant, côté gauche et droit
                for (int i = 0; i < 4; i++) v.Piece(Ellipse(56.5f + i * 3.6f, 40.5f, 1.4f, 1.6f), olive, 1f);   // dents frontales
            };
            return k;
        }

        /// <summary>Crabe nageur bleu (mâle) : carapace très large réticulée de bleu et de blanc, longue épine latérale, pinces fines.</summary>
        private static Crab BlueSwimmer()
        {
            Color blue = C(64, 108, 176), pale = C(196, 216, 238);
            Func<int, int, Color, Color> reticulated = (x, y, c) => Mathf.PerlinNoise(x * 0.32f, y * 0.32f) > 0.58f ? pale : Hash(x, y, 5) > 0.93f ? Darker(c, 0.85f) : c;
            var k = new Crab
            {
                W = 150, H = 104, Cx = 75f, Cy = 60f,
                Carapace = Union(
                    (x, y) => { float u = (x - 75f) / 31f, w = (y - 61f) / 17f; return u * u + w * w <= 1f && y > 46f; },
                    Capsule(75f - 28f, 53f, 75f - 52f, 49f, 2.8f), Capsule(75f + 28f, 53f, 75f + 52f, 49f, 2.8f)),
                Shell = blue, ShellTex = reticulated, ShellBulge = 8f,
                LegAttach = new[] { new Vector2(22f, -1f), new Vector2(24f, 5f), new Vector2(21f, 10f), new Vector2(15f, 14f) },
                LegAngle = new[] { 0f, 22f, 44f, 70f },
                L1 = 16f, L2 = 11f, L3 = 11f, LegR = 2.6f, Bend = 24f,
                Leg = C(80, 124, 186), LegTex = reticulated, Paddles = true, LegTip = C(60, 70, 120),
                ClawAttach = new Vector2(19f, -7f), ArmLen = 18f, ArmR = 3.2f, ArmAngle = -25f, ForeLen = 10f, ForeR = 3.6f, ForeAngle = -70f,
                PalmLenR = 25f, PalmWidR = 5.8f, PalmLenL = 25f, PalmWidL = 5.8f, PalmAngle = -118f, FingerLen = 12f,
                Palm = C(70, 116, 184), Finger = C(110, 80, 160), FingerTip = C(200, 60, 60),
                ClawTex = (x, y, c) => (x + y) % 5 == 0 ? pale : reticulated(x, y, c),
                EyeFrom = new Vector2(6f, -14f), EyeTo = new Vector2(9f, -18f), Stalk = blue,
                Outline = C(22, 34, 66),
            };
            k.Extras = (v, c) =>
            {
                Teeth(v, new Vector2(75f, 61f), 31f, 17f, -148f + 180f - 180f, -104f, 8, 2.4f, 1.1f, blue);
                // Épines de l'avant-bras.
                foreach (int s in new[] { -1, 1 }) v.Piece(Capsule(75f + s * 34f, 37f, 75f + s * 38f, 33f, 1f), C(70, 116, 184), 1f);
            };
            return k;
        }

        /// <summary>Crabe royal : carapace en cœur hérissée d'épines, rouge brique, pattes épaisses et épineuses, pince droite plus grosse.</summary>
        private static Crab KingCrab()
        {
            Color red = C(150, 52, 42), spine = C(236, 150, 110);
            Func<int, int, Color, Color> spiny = (x, y, c) => Hash(x, y, 6) > 0.9f ? spine : Hash(x, y, 7) > 0.85f ? Darker(c, 0.85f) : c;
            var k = new Crab
            {
                W = 150, H = 128, Cx = 75f, Cy = 62f,
                Carapace = Union(Ellipse(75f, 66f, 25f, 25f), Ellipse(75f, 52f, 18f, 20f), Capsule(75f, 34f, 75f, 27f, 2f)),
                Shell = red, ShellTex = spiny, ShellBulge = 14f,
                LegAttach = new[] { new Vector2(20f, 0f), new Vector2(23f, 10f), new Vector2(20f, 20f) },
                LegAngle = new[] { -10f, 22f, 52f },
                L1 = 22f, L2 = 16f, L3 = 13f, LegR = 4.8f, Bend = 30f,
                Leg = C(160, 60, 46), LegTex = spiny, LegTip = C(110, 40, 36),
                ClawAttach = new Vector2(16f, -16f), ArmLen = 12f, ArmR = 4.6f, ArmAngle = -35f, ForeLen = 9f, ForeR = 5f, ForeAngle = -105f,
                PalmLenR = 19f, PalmWidR = 7.6f, PalmLenL = 14f, PalmWidL = 5.6f, PalmAngle = -150f, FingerLen = 8f,
                Palm = C(156, 56, 44), Finger = C(230, 200, 170), FingerTip = C(50, 24, 20), ClawTex = spiny,
                EyeFrom = new Vector2(4f, -28f), EyeTo = new Vector2(6f, -31f), Stalk = red,
                Outline = C(54, 18, 16),
            };
            return k;
        }

        /// <summary>Crabe-araignée géant : petite carapace en poire bosselée, deux cornes, pattes immenses orange tachées de blanc.</summary>
        private static Crab SpiderCrab()
        {
            Color orange = C(214, 112, 52), white = C(244, 230, 214);
            Func<int, int, Color, Color> blotched = (x, y, c) => Mathf.PerlinNoise(x * 0.25f, y * 0.25f) > 0.66f ? white : c;
            var k = new Crab
            {
                W = 184, H = 150, Cx = 92f, Cy = 72f,
                Carapace = Union(Ellipse(92f, 76f, 14f, 17f), Ellipse(92f, 64f, 10f, 11f)),
                Shell = orange, ShellTex = (x, y, c) => Hash(x / 2, y / 2, 8) > 0.75f ? C(236, 150, 90) : c, ShellBulge = 9f,
                LegAttach = new[] { new Vector2(11f, -2f), new Vector2(13f, 5f), new Vector2(12f, 12f), new Vector2(9f, 18f) },
                LegAngle = new[] { -18f, 12f, 42f, 74f },
                L1 = 32f, L2 = 26f, L3 = 20f, LegR = 2.8f, Bend = 22f,
                Leg = orange, LegTex = blotched, LegTip = C(170, 80, 40),
                ClawAttach = new Vector2(9f, -10f), ArmLen = 26f, ArmR = 2.6f, ArmAngle = -62f, ForeLen = 18f, ForeR = 2.6f, ForeAngle = -96f,
                PalmLenR = 16f, PalmWidR = 3.4f, PalmLenL = 16f, PalmWidL = 3.4f, PalmAngle = -104f, FingerLen = 7f,
                Palm = orange, Finger = white, FingerTip = C(120, 70, 50), ClawTex = blotched,
                EyeFrom = new Vector2(3f, -14f), EyeTo = new Vector2(5f, -17f), EyeR = 1.3f, Stalk = orange,
                Outline = C(70, 30, 14),
            };
            k.Extras = (v, c) =>
            {
                foreach (int s in new[] { -1, 1 }) v.Piece(Capsule(92f + s * 2f, 54f, 92f + s * 6f, 46f, 1.4f), orange, 1.2f);   // cornes du rostre
            };
            return k;
        }

        /// <summary>Crabe des neiges : carapace ronde et plate, orange brun semée de petits tubercules, longues pattes aplaties, petites pinces.</summary>
        private static Crab SnowCrab()
        {
            Color top = C(196, 118, 66);
            Func<int, int, Color, Color> tubercles = (x, y, c) => Hash(x, y, 9) > 0.9f ? C(226, 160, 110) : Hash(x / 3, y / 3, 10) > 0.8f ? Darker(c, 0.92f) : c;
            var k = new Crab
            {
                W = 170, H = 132, Cx = 85f, Cy = 64f,
                Carapace = Union(Ellipse(85f, 67f, 21f, 21f), Ellipse(85f, 52f, 9f, 8f)),
                Shell = top, ShellTex = tubercles, ShellBulge = 8f,
                LegAttach = new[] { new Vector2(17f, -2f), new Vector2(20f, 6f), new Vector2(18f, 13f), new Vector2(13f, 19f) },
                LegAngle = new[] { -12f, 14f, 40f, 68f },
                L1 = 26f, L2 = 17f, L3 = 15f, LegR = 3.8f, Bend = 26f,
                Leg = C(206, 132, 78), LegTex = tubercles, LegTip = C(150, 90, 50),
                ClawAttach = new Vector2(13f, -11f), ArmLen = 12f, ArmR = 3f, ArmAngle = -35f, ForeLen = 8f, ForeR = 3.2f, ForeAngle = -105f,
                PalmLenR = 12f, PalmWidR = 4.2f, PalmLenL = 12f, PalmWidL = 4.2f, PalmAngle = -150f, FingerLen = 6f,
                Palm = C(200, 124, 70), Finger = C(236, 210, 180), FingerTip = C(60, 36, 26), ClawTex = tubercles,
                EyeFrom = new Vector2(4f, -19f), EyeTo = new Vector2(6f, -22f), Stalk = top,
                Outline = C(70, 36, 20),
            };
            return k;
        }

        /// <summary>Crabe violoniste (mâle) : carapace trapue, yeux au bout de longs pédoncules, une pince géante jaune.</summary>
        private static Crab FiddlerCrab()
        {
            Color brown = C(88, 70, 58);
            var k = new Crab
            {
                W = 130, H = 90, Cx = 58f, Cy = 54f,
                Carapace = (x, y) =>
                {
                    if (y < 42f || y > 66f) return false;
                    float half = Mathf.Lerp(19f, 12f, (y - 42f) / 24f) - (y < 45f ? (45f - y) * 0.8f : 0f);
                    return Mathf.Abs(x - 58f) <= half;
                },
                Shell = brown, ShellTex = (x, y, c) => Mathf.PerlinNoise(x * 0.4f, y * 0.4f) > 0.6f ? C(150, 130, 104) : c, ShellBulge = 7f,
                LegAttach = new[] { new Vector2(15f, -4f), new Vector2(16f, 1f), new Vector2(14f, 6f), new Vector2(11f, 10f) },
                LegAngle = new[] { 0f, 22f, 44f, 66f },
                L1 = 10f, L2 = 7f, L3 = 6f, LegR = 2.3f, Bend = 24f,
                Leg = C(110, 90, 74), LegTip = C(70, 54, 44),
                ClawAttach = new Vector2(16f, -9f), ArmLen = 9f, ArmR = 3.4f, ArmAngle = -10f, ForeLen = 7f, ForeR = 4f, ForeAngle = -30f,
                PalmLenR = 32f, PalmWidR = 10f, PalmLenL = 6f, PalmWidL = 2.4f, PalmAngle = -26f, FingerLen = 14f,
                Palm = C(240, 196, 40), Finger = C(250, 230, 150), FingerTip = C(230, 120, 40),
                ClawTex = (x, y, c) => Hash(x, y, 11) > 0.9f ? C(250, 220, 110) : c,
                EyeFrom = new Vector2(3f, -12f), EyeTo = new Vector2(17f, -15f), EyeR = 1.8f, Stalk = C(130, 110, 90),
                Outline = C(40, 30, 22),
            };
            return k;
        }

        /// <summary>Crabe à oursin : petite carapace en losange, rayée de blanc et de pourpre en long.</summary>
        private static Crab UrchinCrab()
        {
            Color purple = C(110, 48, 118), white = C(240, 236, 244);
            Func<int, int, Color, Color> stripes = (x, y, c) => ((int)Mathf.Abs((x - 36f) / 3f + 0.5f)) % 2 == 0 ? white : purple;
            var k = new Crab
            {
                W = 72, H = 64, Cx = 36f, Cy = 34f,
                Carapace = (x, y) => Mathf.Abs(x - 36f) / 19f + Mathf.Abs(y - 35f) / 13f <= 1f,
                Shell = white, ShellTex = stripes, ShellBulge = 6f,
                LegAttach = new[] { new Vector2(9f, -1f), new Vector2(10f, 3f), new Vector2(8f, 7f), new Vector2(5f, 10f) },
                LegAngle = new[] { 0f, 26f, 50f, 74f },
                L1 = 7f, L2 = 5f, L3 = 5f, LegR = 1.7f, Bend = 26f,
                Leg = white, LegTex = (x, y, c) => (x + y) % 4 < 2 ? purple : white, LegTip = C(80, 30, 80),
                ClawAttach = new Vector2(6f, -6f), ArmLen = 5f, ArmR = 1.8f, ArmAngle = -35f, ForeLen = 4f, ForeR = 2f, ForeAngle = -105f,
                PalmLenR = 6f, PalmWidR = 2.3f, PalmLenL = 6f, PalmWidL = 2.3f, PalmAngle = -150f, FingerLen = 3f,
                Palm = white, Finger = white, FingerTip = purple, ClawTex = stripes,
                EyeFrom = new Vector2(2f, -12f), EyeTo = new Vector2(3f, -14f), EyeR = 1f, Stalk = white,
                Outline = C(50, 20, 56),
            };
            return k;
        }

        /// <summary>Crabe araignée des coraux mous : carapace en triangle pointu, rose et blanc, hérissée d'épines.</summary>
        private static Crab SoftCoralCrab()
        {
            Color pink = C(236, 150, 172), white = C(250, 236, 240), red = C(200, 60, 90);
            Func<int, int, Color, Color> dotted = (x, y, c) => Hash(x, y, 12) > 0.9f ? red : Hash(x / 2, y / 2, 13) > 0.7f ? white : c;
            var k = new Crab
            {
                W = 84, H = 84, Cx = 42f, Cy = 44f,
                Carapace = (x, y) => y >= 26f && y <= 58f && Mathf.Abs(x - 42f) <= Mathf.Min((y - 26f) * 0.62f, 16f) * (y > 52f ? 1f - (y - 52f) / 10f : 1f),
                Shell = pink, ShellTex = dotted, ShellBulge = 6f,
                LegAttach = new[] { new Vector2(10f, -2f), new Vector2(13f, 4f), new Vector2(12f, 9f), new Vector2(8f, 13f) },
                LegAngle = new[] { -14f, 14f, 42f, 72f },
                L1 = 12f, L2 = 9f, L3 = 7f, LegR = 2f, Bend = 24f,
                Leg = pink, LegTex = (x, y, c) => ((x + y) / 3) % 2 == 0 ? white : dotted(x, y, c), LegTip = red,
                ClawAttach = new Vector2(7f, -7f), ArmLen = 9f, ArmR = 1.8f, ArmAngle = -60f, ForeLen = 6f, ForeR = 1.9f, ForeAngle = -96f,
                PalmLenR = 8f, PalmWidR = 2.4f, PalmLenL = 8f, PalmWidL = 2.4f, PalmAngle = -104f, FingerLen = 4f,
                Palm = pink, Finger = white, FingerTip = red, ClawTex = dotted,
                EyeFrom = new Vector2(2f, -14f), EyeTo = new Vector2(4f, -16f), EyeR = 1.1f, Stalk = pink,
                Outline = C(110, 40, 60),
            };
            k.Extras = (v, c) =>
            {
                // Épines blanches sur tout le pourtour.
                for (int i = 0; i < 9; i++)
                {
                    float y = 30f + i * 3.3f, half = Mathf.Min((y - 26f) * 0.62f, 16f);
                    foreach (int s in new[] { -1, 1 }) v.Piece(Capsule(42f + s * half, y, 42f + s * (half + 3f), y - 2f, 0.8f), white, 0.8f);
                }

                v.Piece(Capsule(42f, 27f, 42f, 21f, 1f), white, 1f);   // rostre
            };
            return k;
        }

        /// <summary>Crabe d'eau douce peint : carapace trapézoïdale brun pourpre tachée d'orange, pinces rougeâtres.</summary>
        private static Crab PaintedCrab()
        {
            Color brown = C(112, 70, 72), spot = C(226, 140, 60);
            Func<int, int, Color, Color> spotted = (x, y, c) => Mathf.PerlinNoise(x * 0.45f + 3f, y * 0.45f) > 0.68f ? spot : c;
            var k = new Crab
            {
                W = 116, H = 92, Cx = 58f, Cy = 52f,
                Carapace = (x, y) => { float u = (x - 58f) / 25f, w = (y - 54f) / 18f; return u * u + w * w * 1.3f <= 1f && y > 39f; },
                Shell = brown, ShellTex = spotted, ShellBulge = 9f,
                LegAttach = new[] { new Vector2(19f, -2f), new Vector2(21f, 4f), new Vector2(19f, 10f), new Vector2(14f, 15f) },
                LegAngle = new[] { -6f, 18f, 40f, 66f },
                L1 = 12f, L2 = 9f, L3 = 8f, LegR = 2.8f, Bend = 26f,
                Leg = C(150, 96, 70), LegTip = C(90, 56, 40),
                ClawAttach = new Vector2(17f, -9f), ArmLen = 10f, ArmR = 3.6f, ArmAngle = -30f, ForeLen = 7f, ForeR = 4f, ForeAngle = -100f,
                PalmLenR = 17f, PalmWidR = 6.6f, PalmLenL = 15f, PalmWidL = 5.6f, PalmAngle = -156f, FingerLen = 8f,
                Palm = C(176, 72, 52), Finger = C(240, 214, 180), FingerTip = C(50, 30, 26),
                ClawTex = (x, y, c) => Hash(x, y, 14) > 0.88f ? Darker(c, 0.85f) : c,
                EyeFrom = new Vector2(5f, -13f), EyeTo = new Vector2(7f, -16f), Stalk = brown,
                Outline = C(44, 26, 26),
            };
            return k;
        }

        /// <summary>
        /// Bernard-l'ermite à pois : grosse coquille spiralée, l'avant du crabe qui en sort, pattes et pinces rouges semées
        /// de pois blancs cerclés de noir, la pince gauche plus grosse.
        /// </summary>
        private static Crab HermitCrab()
        {
            Color red = C(206, 66, 40);
            Func<int, int, Color, Color> dots = (x, y, c) =>
            {
                int cx = (x / 7) * 7 + 3, cy = (y / 7) * 7 + 3;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                bool on = Hash(x / 7, y / 7, 15) > 0.55f;
                return on && d < 1.7f ? C(250, 246, 236) : on && d < 2.6f ? C(30, 20, 20) : c;
            };
            var k = new Crab
            {
                W = 116, H = 104, Cx = 58f, Cy = 40f,
                Carapace = Ellipse(58f, 38f, 9f, 7f),
                Shell = red, ShellTex = dots, ShellBulge = 5f,
                LegAttach = new[] { new Vector2(8f, 0f), new Vector2(9f, 5f) },
                LegAngle = new[] { -10f, 18f },
                L1 = 14f, L2 = 10f, L3 = 9f, LegR = 3.2f, Bend = 30f,
                Leg = red, LegTex = dots, LegTip = C(240, 200, 60),
                ClawAttach = new Vector2(7f, -5f), ArmLen = 9f, ArmR = 3.6f, ArmAngle = -35f, ForeLen = 7f, ForeR = 4.2f, ForeAngle = -105f,
                PalmLenR = 13f, PalmWidR = 6f, PalmLenL = 17f, PalmWidL = 8f, PalmAngle = -150f, FingerLen = 7f,
                Palm = red, Finger = C(240, 220, 190), FingerTip = C(30, 20, 18), ClawTex = dots,
                EyeFrom = new Vector2(3f, -6f), EyeTo = new Vector2(5f, -14f), EyeR = 1.6f, Stalk = C(220, 200, 90), Eye = C(20, 60, 50),
                Outline = C(60, 20, 14),
            };
            k.Under = (v, c) =>
            {
                // La coquille : un escargot de mer vu de dessus, ouverture vers l'avant (où sort le crabe), spire en pointe
                // vers l'arrière ; les tours sont séparés par des sutures en arc, des bandes brunes courent en long.
                Color shell = C(226, 206, 166);
                // Spirale logarithmique : chaque tour est 2,3 fois plus large que le précédent, depuis la pointe (apex).
                var apex = new Vector2(62f, 76f);
                float growth = Mathf.Log(2.3f) / (2f * Mathf.PI);
                v.Piece(Union(Ellipse(60f, 70f, 27f, 25f), Ellipse(58f, 50f, 18f, 10f)), shell, 13f, (x, y, col) =>
                {
                    Vector2 d = new Vector2(x, y) - apex;
                    float r = Mathf.Max(0.5f, d.magnitude), theta = Mathf.Atan2(d.y, d.x);
                    float turn = Mathf.Log(r / 1.5f) / growth / (2f * Mathf.PI) - theta / (2f * Mathf.PI);
                    float frac = turn - Mathf.Floor(turn);
                    if (frac < 0.07f && r < 30f) return C(118, 80, 54);                         // la suture entre deux tours
                    if (frac > 0.07f && frac < 0.2f) return Darker(col, 0.88f);                   // l'ombre sous le tour d'au-dessus
                    float spots = Mathf.Sin(theta * 7f + r * 0.35f);
                    return spots > 0.82f ? C(176, 120, 76) : Hash(x, y, 16) > 0.94f ? Darker(col, 0.9f) : col;
                }, 0.6f);
                // L'ouverture, sombre et nacrée, d'où sort le crabe.
                v.Piece(Ellipse(58f, 44f, 15f, 6f), C(90, 60, 50), 3f);
            };
            return k;
        }

        /// <summary>
        /// Crabe fantôme : carapace presque carrée couleur de sable, longues pattes de coureur, yeux énormes au bout de
        /// pédoncules, chacun surmonté d'une longue corne noire ; une pince un peu plus grosse.
        /// </summary>
        private static Crab GhostCrab()
        {
            Color sand = C(214, 200, 162);
            Func<int, int, Color, Color> grains = (x, y, c) => Hash(x, y, 20) > 0.88f ? C(236, 226, 200) : Hash(x / 2, y / 2, 21) > 0.85f ? Darker(c, 0.9f) : c;
            var k = new Crab
            {
                W = 120, H = 96, Cx = 60f, Cy = 54f,
                Carapace = (x, y) => Mathf.Abs(x - 60f) <= 17f - Mathf.Max(0f, y - 62f) * 0.4f && y > 40f && y < 68f,
                Shell = sand, ShellTex = grains, ShellBulge = 7f,
                LegAttach = new[] { new Vector2(15f, -4f), new Vector2(16f, 2f), new Vector2(15f, 8f), new Vector2(12f, 12f) },
                LegAngle = new[] { -6f, 18f, 42f, 66f },
                L1 = 16f, L2 = 12f, L3 = 9f, LegR = 2.4f, Bend = 24f,
                Leg = C(206, 190, 150), LegTex = grains, LegTip = C(150, 130, 100),
                ClawAttach = new Vector2(13f, -12f), ArmLen = 8f, ArmR = 2.8f, ArmAngle = -40f, ForeLen = 6f, ForeR = 3f, ForeAngle = -100f,
                PalmLenR = 13f, PalmWidR = 5f, PalmLenL = 10f, PalmWidL = 4f, PalmAngle = -150f, FingerLen = 6f,
                Palm = C(226, 214, 182), Finger = C(240, 232, 214), FingerTip = C(120, 100, 80), ClawTex = grains,
                EyeFrom = new Vector2(4f, -13f), EyeTo = new Vector2(12f, -18f), EyeR = 3f, Stalk = sand, Eye = C(30, 28, 26),
                Outline = C(90, 76, 56),
            };
            k.Extras = (v, c) =>
            {
                // Les cornes noires au-dessus des yeux.
                foreach (int s in new[] { -1, 1 }) v.Piece(Capsule(60f + s * 12f, 36f, 60f + s * 14f, 27f, 1f), C(30, 26, 24), 1f);
            };
            return k;
        }

        /// <summary>
        /// Crabe des cocotiers : énorme, bleu violacé taché de clair ; l'avant cuirassé, l'abdomen rond replié dessous qui
        /// dépasse à l'arrière, pattes épaisses, pince gauche bien plus grosse.
        /// </summary>
        private static Crab CoconutCrab()
        {
            Color blue = C(64, 84, 154), pale = C(150, 170, 220);
            Func<int, int, Color, Color> mottled = (x, y, c) => Mathf.PerlinNoise(x * 0.15f, y * 0.15f) > 0.62f ? pale : Hash(x, y, 22) > 0.9f ? Darker(c, 0.85f) : c;
            var k = new Crab
            {
                W = 150, H = 140, Cx = 75f, Cy = 56f,
                Carapace = Union(Ellipse(75f, 54f, 20f, 18f), Ellipse(75f, 40f, 14f, 10f)),
                Shell = blue, ShellTex = mottled, ShellBulge = 12f,
                LegAttach = new[] { new Vector2(16f, -2f), new Vector2(18f, 6f), new Vector2(16f, 13f) },
                LegAngle = new[] { -8f, 24f, 56f },
                L1 = 22f, L2 = 16f, L3 = 12f, LegR = 5f, Bend = 30f,
                Leg = C(80, 100, 170), LegTex = mottled, LegTip = C(40, 50, 90),
                ClawAttach = new Vector2(14f, -14f), ArmLen = 12f, ArmR = 5f, ArmAngle = -40f, ForeLen = 9f, ForeR = 5.6f, ForeAngle = -105f,
                PalmLenR = 18f, PalmWidR = 7.6f, PalmLenL = 24f, PalmWidL = 10f, PalmAngle = -150f, FingerLen = 9f,
                Palm = C(70, 92, 164), Finger = C(200, 120, 110), FingerTip = C(40, 30, 30), ClawTex = mottled,
                EyeFrom = new Vector2(4f, -24f), EyeTo = new Vector2(6f, -28f), EyeR = 2f, Stalk = blue,
                Outline = C(22, 28, 60),
            };
            k.Under = (v, c) =>
            {
                // L'abdomen, rond et mou, replié sous le corps : il dépasse à l'arrière.
                v.Piece(Ellipse(75f, 80f, 13f, 14f), C(130, 110, 140), 9f, (x, y, col) => (y % 6 < 1) ? Darker(col, 0.85f) : col);
            };
            return k;
        }

        /// <summary>Crabe-soldat bleu : petite boule bleu ciel, longues pattes violettes à jointures blanches, petites pinces pendantes.</summary>
        private static Crab SoldierCrab()
        {
            Color sky = C(140, 184, 236);
            var k = new Crab
            {
                W = 96, H = 88, Cx = 48f, Cy = 46f,
                Carapace = Ellipse(48f, 46f, 15f, 15f),
                Shell = sky, ShellTex = (x, y, c) => Hash(x, y, 23) > 0.9f ? C(190, 214, 246) : c, ShellBulge = 12f,
                LegAttach = new[] { new Vector2(12f, -3f), new Vector2(14f, 3f), new Vector2(13f, 9f), new Vector2(10f, 13f) },
                LegAngle = new[] { 0f, 26f, 52f, 78f },
                L1 = 13f, L2 = 11f, L3 = 8f, LegR = 1.9f, Bend = 22f,
                Leg = C(150, 90, 170), LegTex = (x, y, c) => (x + y) % 7 == 0 ? C(240, 236, 244) : c, LegTip = C(220, 200, 230),
                ClawAttach = new Vector2(8f, -10f), ArmLen = 9f, ArmR = 2f, ArmAngle = -70f, ForeLen = 7f, ForeR = 2.2f, ForeAngle = -100f,
                PalmLenR = 8f, PalmWidR = 2.8f, PalmLenL = 8f, PalmWidL = 2.8f, PalmAngle = -100f, FingerLen = 4f,
                Palm = C(170, 110, 190), Finger = C(240, 236, 244), FingerTip = C(120, 70, 140),
                EyeFrom = new Vector2(3f, -14f), EyeTo = new Vector2(4f, -16f), EyeR = 1.4f, Stalk = sky,
                Outline = C(40, 50, 90),
            };
            return k;
        }
    }
}
