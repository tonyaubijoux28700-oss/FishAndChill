using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les crustacés allongés du carnet de l'épuisette (homard, langoustes, écrevisse, cigale de mer, crevettes, squilles,
    /// triops), vus de dessus, tête en haut, en volume, d'après les vraies espèces : carapace, abdomen en anneaux, éventail
    /// de la queue, antennes (fouets, plaques), pattes, pinces ou bras ravisseurs, yeux. Dessins originaux.
    /// </summary>
    public static class CrustaceanArt
    {
        private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        private static float Hash(int x, int y, int seed = 0)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }

        private static Vector2 Dir(float degrees) => new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

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

        private static float Side(int s, float degrees) => s > 0 ? degrees : 180f - degrees;

        public static readonly string[] Ids =
        {
            "homarus-gammarus", "panulirus-versicolor", "jasus-lalandii", "scyllarides-squammosus", "pacifastacus-leniusculus",
            "macrobrachium-amazonicum", "oratosquilla-oratoria", "stenopus-hispidus", "phyllognathia-ceratophthalma",
            "odontodactylus-scyllarus", "triops-cancriformis", "macrobrachium-carcinus", "penaeus-monodon", "panulirus-japonicus",
        };

        private sealed class Body
        {
            public int W = 120, H = 200;
            public float Cx = 60f;

            // Carapace (céphalothorax) : de Top à Top + CarL, demi-largeur CarW.
            public float Top = 60f, CarL = 46f, CarW = 15f;
            public Color Shell;
            public Func<int, int, Color, Color> ShellTex;
            public float Rostrum = 6f;           // pointe devant la carapace

            // Abdomen : anneaux de plus en plus étroits, puis l'éventail de la queue.
            public int Segments = 6;
            public float SegL = 9f, SegW0 = 14f, SegW1 = 9f;
            public Color Abdomen;
            public Func<int, int, int, Color, Color> SegTex;   // (x, y, numéro de l'anneau, couleur)
            public float FanL = 18f, FanW = 16f;
            public Color Fan;

            // Antennes : un fouet de chaque côté qui part vers l'avant puis s'écarte.
            public Vector2 AntBase = new Vector2(5f, 2f);
            public float AntLen = 60f, AntR0 = 2f, AntR1 = 0.7f, AntAngle = -102f, AntCurve = 0.6f;
            public Color Antenna;
            public Func<int, int, Color, Color> AntTex;
            public bool ShortAntennules = true;

            // Pattes marcheuses le long de la carapace.
            public int Legs = 4;
            public float LegLen = 12f, LegR = 1.6f, LegAngle = 20f;
            public Color Leg;
            public Func<int, int, Color, Color> LegTex;

            // Pinces (homard, écrevisse, crevettes à pinces).
            public bool Claws;
            public float ClawY = 10f, ArmLen = 12f, ArmR = 3.5f, ArmAngle = -60f, ForeLen = 10f, ForeR = 4f, ForeAngle = -92f;
            public float PalmLenR = 30f, PalmWidR = 10f, PalmLenL = 30f, PalmWidL = 10f, PalmAngle = -95f, FingerLen = 14f;
            public Color Palm, Finger, FingerTip = new Color(0.15f, 0.1f, 0.08f);
            public Func<int, int, Color, Color> ClawTex;

            // Yeux.
            public Vector2 Eye = new Vector2(5f, 2f);
            public float EyeR = 2f;
            public Color EyeColor = new Color(0.08f, 0.06f, 0.06f);

            public Color Outline = new Color(0.12f, 0.1f, 0.1f);
            public Action<VolumeCanvas, Body> Before, After;
        }

        public static bool TryGet(string id, out Color[,] pixels)
        {
            Body b = Spec(id);
            pixels = b != null ? Paint(b) : null;
            return pixels != null;
        }

        // ---------- Le dessin ----------

        private static Color[,] Paint(Body b)
        {
            var v = new VolumeCanvas(b.W, b.H);
            float cx = b.Cx;
            b.Before?.Invoke(v, b);

            // Antennes (dessous de tout le reste).
            foreach (int s in new[] { -1, 1 })
            {
                if (b.AntLen <= 0f) break;
                var points = new Vector2[9];
                Vector2 p = new Vector2(cx + s * b.AntBase.x, b.Top + b.AntBase.y);
                float angle = Side(s, b.AntAngle);
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = p;
                    p += Dir(angle) * (b.AntLen / (points.Length - 1));
                    angle += (s > 0 ? 1f : -1f) * b.AntCurve * 9f;
                }

                for (int i = 0; i < points.Length - 1; i++)
                {
                    float r = Mathf.Lerp(b.AntR0, b.AntR1, i / (float)(points.Length - 2));
                    v.Piece(Capsule(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, r), b.Antenna, Mathf.Max(1f, r), b.AntTex, 0.85f);
                }

                if (b.ShortAntennules)
                {
                    Vector2 a0 = new Vector2(cx + s * 2f, b.Top + 1f);
                    Vector2 a1 = a0 + Dir(Side(s, -98f)) * 12f;
                    v.Piece(Capsule(a0.x, a0.y, a1.x, a1.y, 0.9f), b.Antenna, 1f, null, 0.85f);
                }
            }

            // Pattes marcheuses.
            for (int i = 0; i < b.Legs; i++)
            {
                float y = b.Top + b.CarL * (0.45f + 0.5f * i / Mathf.Max(1, b.Legs - 1));
                foreach (int s in new[] { -1, 1 })
                {
                    Vector2 p0 = new Vector2(cx + s * b.CarW * 0.8f, y);
                    Vector2 p1 = p0 + Dir(Side(s, b.LegAngle + i * 10f)) * b.LegLen * 0.55f;
                    Vector2 p2 = p1 + Dir(Side(s, b.LegAngle + 40f + i * 10f)) * b.LegLen * 0.45f;
                    v.Piece(Capsule(p0.x, p0.y, p1.x, p1.y, b.LegR), b.Leg, b.LegR, b.LegTex, 0.75f);
                    v.Piece(Capsule(p1.x, p1.y, p2.x, p2.y, b.LegR * 0.75f), b.Leg, b.LegR, b.LegTex, 0.75f);
                }
            }

            // Bras des pinces.
            if (b.Claws)
            {
                foreach (int s in new[] { -1, 1 }) ClawArm(v, b, s, out _);
            }

            // Abdomen, de la queue vers la carapace (chaque anneau recouvre un peu le suivant).
            float y0 = b.Top + b.CarL;
            float fanY = y0 + b.Segments * b.SegL;
            if (b.FanL > 0f)
            {
                // Éventail : le telson au milieu et deux paires d'uropodes.
                v.Piece(Oval(new Vector2(cx, fanY + b.FanL * 0.5f), b.FanL * 0.55f, b.FanW * 0.28f, 90f), b.Fan, 2f, null, 0.7f);
                foreach (int s in new[] { -1, 1 })
                {
                    v.Piece(Oval(new Vector2(cx + s * b.FanW * 0.4f, fanY + b.FanL * 0.45f), b.FanL * 0.5f, b.FanW * 0.24f, Side(s, 72f)), b.Fan, 2f, null, 0.7f);
                    v.Piece(Oval(new Vector2(cx + s * b.FanW * 0.72f, fanY + b.FanL * 0.32f), b.FanL * 0.42f, b.FanW * 0.2f, Side(s, 55f)), b.Fan, 2f, null, 0.7f);
                }
            }

            for (int i = b.Segments - 1; i >= 0; i--)
            {
                float t = b.Segments > 1 ? i / (float)(b.Segments - 1) : 0f;
                float w = Mathf.Lerp(b.SegW0, b.SegW1, t);
                float sy = y0 + i * b.SegL;
                int ring = i;
                v.Piece(Ellipse(cx, sy + b.SegL * 0.55f, w, b.SegL * 0.75f), b.Abdomen, Mathf.Min(w, b.SegL) * 0.8f,
                    (x, y, c) => b.SegTex != null ? b.SegTex(x, y, ring, c) : c, 0.7f);
            }

            // Carapace par-dessus, avec son rostre.
            float carCy = b.Top + b.CarL * 0.52f;
            v.Piece(Union(Ellipse(cx, carCy, b.CarW, b.CarL * 0.54f), Capsule(cx, b.Top + 4f, cx, b.Top - b.Rostrum, 1.4f)), b.Shell,
                b.CarW * 0.8f, b.ShellTex, 0.6f);

            if (b.Claws)
            {
                foreach (int s in new[] { -1, 1 }) ClawHand(v, b, s);
            }

            foreach (int s in new[] { -1, 1 })
            {
                v.Piece(Ellipse(cx + s * b.Eye.x, b.Top + b.Eye.y, b.EyeR, b.EyeR), b.EyeColor, Mathf.Max(1f, b.EyeR));
                v.Dot(Mathf.RoundToInt(cx + s * b.Eye.x - 0.5f), Mathf.RoundToInt(b.Top + b.Eye.y - 0.8f), new Color(0.9f, 0.9f, 0.86f));
            }

            b.After?.Invoke(v, b);
            return v.Finish(b.Outline, 0.6f);
        }

        private static Vector2 ClawArm(VolumeCanvas v, Body b, int s, out Vector2 wrist)
        {
            var shoulder = new Vector2(b.Cx + s * b.CarW * 0.7f, b.Top + b.ClawY);
            Vector2 elbow = shoulder + Dir(Side(s, b.ArmAngle)) * b.ArmLen;
            wrist = elbow + Dir(Side(s, b.ForeAngle)) * b.ForeLen;
            if (v != null)
            {
                v.Piece(Capsule(shoulder.x, shoulder.y, elbow.x, elbow.y, b.ArmR), b.Palm, b.ArmR, b.ClawTex, 0.7f);
                v.Piece(Capsule(elbow.x, elbow.y, wrist.x, wrist.y, b.ForeR), b.Palm, b.ForeR, b.ClawTex, 0.7f);
            }

            return elbow;
        }

        private static void ClawHand(VolumeCanvas v, Body b, int s)
        {
            ClawArm(null, b, s, out Vector2 wrist);
            float len = s > 0 ? b.PalmLenR : b.PalmLenL, wid = s > 0 ? b.PalmWidR : b.PalmWidL;
            float angle = Side(s, b.PalmAngle);
            Vector2 d = Dir(angle);
            Vector2 palm = wrist + d * len * 0.45f;
            v.Piece(Oval(palm, len * 0.55f, wid, angle), b.Palm, wid * 0.9f, b.ClawTex, 0.65f);
            Vector2 tip = palm + d * len * 0.5f;
            var inner = new Vector2(-d.y, d.x) * (s > 0 ? -1f : 1f);
            float f = b.FingerLen * (len / Mathf.Max(1f, b.PalmLenR));
            Vector2 fixedBase = tip + inner * wid * 0.35f, fixedEnd = fixedBase + d * f + inner * f * 0.1f;
            Vector2 moveBase = tip - inner * wid * 0.4f, moveEnd = moveBase + d * f * 0.95f + inner * f * 0.22f;
            float fr = Mathf.Max(1.1f, wid * 0.3f);
            v.Piece(Capsule(fixedBase.x, fixedBase.y, fixedEnd.x, fixedEnd.y, fr), b.Finger, fr, b.ClawTex, 0.7f);
            v.Piece(Capsule(moveBase.x, moveBase.y, moveEnd.x, moveEnd.y, fr), b.Finger, fr, b.ClawTex, 0.7f);
            v.Piece(Ellipse(fixedEnd.x, fixedEnd.y, fr * 0.85f, fr * 0.85f), b.FingerTip, 1f);
            v.Piece(Ellipse(moveEnd.x, moveEnd.y, fr * 0.85f, fr * 0.85f), b.FingerTip, 1f);
        }

        /// <summary>Bras ravisseur de squille, replié sous la tête : la « mante » de la crevette-mante.</summary>
        private static void Raptorial(VolumeCanvas v, Body b, Color color, Color club, float size)
        {
            foreach (int s in new[] { -1, 1 })
            {
                var a = new Vector2(b.Cx + s * b.CarW * 0.6f, b.Top + 4f);
                Vector2 elbow = a + Dir(Side(s, -30f)) * size;
                Vector2 hand = elbow + Dir(Side(s, 120f)) * size * 0.9f;
                v.Piece(Capsule(a.x, a.y, elbow.x, elbow.y, size * 0.22f), color, size * 0.2f, null, 0.7f);
                v.Piece(Capsule(elbow.x, elbow.y, hand.x, hand.y, size * 0.28f), club, size * 0.25f, null, 0.7f);
            }
        }

        // ---------- Les espèces ----------

        private static Body Spec(string id)
        {
            switch (id)
            {
                case "homarus-gammarus": return Lobster();
                case "panulirus-versicolor": return PaintedRock();
                case "jasus-lalandii": return CapeRock();
                case "scyllarides-squammosus": return Slipper();
                case "pacifastacus-leniusculus": return Signal();
                case "macrobrachium-amazonicum": return AmazonShrimp();
                case "oratosquilla-oratoria": return Squilla();
                case "stenopus-hispidus": return Banded();
                case "phyllognathia-ceratophthalma": return TigerShrimp();
                case "odontodactylus-scyllarus": return Peacock();
                case "triops-cancriformis": return Triops();
                case "macrobrachium-carcinus": return BigRiverShrimp();
                case "penaeus-monodon": return TigerPrawn();
                case "panulirus-japonicus": return JapaneseRock();
                default: return null;
            }
        }

        /// <summary>Homard européen : bleu nuit moucheté de jaune, grosse pince broyeuse et pince coupante, antennes orangées.</summary>
        private static Body Lobster()
        {
            Color blue = C(38, 58, 108);
            Func<int, int, Color, Color> speckled = (x, y, c) => Hash(x, y, 1) > 0.9f ? C(220, 200, 120) : Hash(x / 2, y / 2, 2) > 0.8f ? Darker(c, 0.85f) : c;
            return new Body
            {
                W = 130, H = 210, Cx = 65f, Top = 66f, CarL = 46f, CarW = 15f, Shell = blue, ShellTex = speckled, Rostrum = 8f,
                Segments = 6, SegL = 9f, SegW0 = 14f, SegW1 = 9f, Abdomen = blue, SegTex = (x, y, i, c) => speckled(x, y, c),
                FanL = 20f, FanW = 18f, Fan = C(48, 70, 120),
                AntBase = new Vector2(5f, 3f), AntLen = 64f, AntR0 = 1.8f, AntR1 = 0.7f, AntAngle = -96f, AntCurve = 1.2f, Antenna = C(196, 96, 60),
                Legs = 4, LegLen = 16f, LegR = 1.6f, LegAngle = 14f, Leg = C(60, 84, 136),
                Claws = true, ClawY = 12f, ArmLen = 13f, ArmR = 3.6f, ArmAngle = -20f, ForeLen = 11f, ForeR = 4.2f, ForeAngle = -62f,
                PalmLenR = 34f, PalmWidR = 12f, PalmLenL = 30f, PalmWidL = 9f, PalmAngle = -84f, FingerLen = 15f,
                Palm = blue, Finger = C(46, 66, 116), FingerTip = C(220, 120, 70), ClawTex = speckled,
                Eye = new Vector2(4f, 2f), EyeR = 2f, Outline = C(14, 20, 40),
            };
        }

        /// <summary>Langouste porcelaine : bleu-vert rayé de blanc et de noir, antennes épaisses roses puis fouets blancs.</summary>
        private static Body PaintedRock()
        {
            Color teal = C(54, 116, 128), pink = C(236, 176, 196), white = C(244, 244, 236);
            return new Body
            {
                W = 150, H = 220, Cx = 75f, Top = 72f, CarL = 50f, CarW = 16f, Rostrum = 0f,
                Shell = teal, ShellTex = (x, y, c) => Mathf.Abs(Mathf.Sin(x * 0.5f + y * 0.15f)) < 0.12f ? white : Hash(x, y, 3) > 0.92f ? C(30, 40, 50) : c,
                Segments = 6, SegL = 10f, SegW0 = 15f, SegW1 = 10f, Abdomen = teal,
                SegTex = (x, y, i, c) => { float local = (y - (72f + 50f + i * 10f)) / 10f; return local > 0.78f ? white : local > 0.68f ? C(20, 26, 34) : c; },
                FanL = 20f, FanW = 20f, Fan = C(70, 130, 150),
                AntBase = new Vector2(7f, 2f), AntLen = 78f, AntR0 = 3.6f, AntR1 = 0.8f, AntAngle = -100f, AntCurve = 2f, Antenna = white,
                AntTex = (x, y, c) => y > 60 ? pink : c,
                Legs = 5, LegLen = 18f, LegR = 1.8f, LegAngle = 10f, Leg = C(80, 140, 170), LegTex = (x, y, c) => (x + y) % 6 < 2 ? white : c,
                Eye = new Vector2(5f, 1f), EyeR = 2f, Outline = C(16, 32, 40),
            };
        }

        /// <summary>Langouste du Cap : rouge sombre, carapace couverte d'épines, longues antennes rouges.</summary>
        private static Body CapeRock()
        {
            Color red = C(132, 38, 36);
            Func<int, int, Color, Color> spiny = (x, y, c) => Hash(x, y, 4) > 0.88f ? C(230, 150, 120) : Hash(x / 3, y / 3, 5) > 0.75f ? Darker(c, 0.85f) : c;
            return new Body
            {
                W = 150, H = 216, Cx = 75f, Top = 70f, CarL = 50f, CarW = 17f, Rostrum = 0f, Shell = red, ShellTex = spiny,
                Segments = 6, SegL = 10f, SegW0 = 15f, SegW1 = 10f, Abdomen = red,
                SegTex = (x, y, i, c) => { float local = (y - (70f + 50f + i * 10f)) / 10f; return local > 0.75f ? Darker(c, 0.75f) : spiny(x, y, c); },
                FanL = 20f, FanW = 20f, Fan = C(150, 50, 44),
                AntBase = new Vector2(7f, 2f), AntLen = 80f, AntR0 = 3.4f, AntR1 = 0.8f, AntAngle = -98f, AntCurve = 1.8f, Antenna = C(150, 48, 40), AntTex = spiny,
                Legs = 5, LegLen = 17f, LegR = 1.8f, LegAngle = 10f, Leg = C(160, 60, 50),
                Eye = new Vector2(5f, 1f), EyeR = 2f, Outline = C(50, 14, 12),
            };
        }

        /// <summary>Cigale de mer : carapace plate et large, deux antennes en plaques festonnées devant, queue large.</summary>
        private static Body Slipper()
        {
            Color shell = C(186, 120, 70);
            Func<int, int, Color, Color> spotted = (x, y, c) => Hash(x / 2, y / 2, 6) > 0.82f ? C(110, 64, 40) : Hash(x, y, 7) > 0.92f ? C(220, 170, 120) : c;
            var b = new Body
            {
                W = 110, H = 150, Cx = 55f, Top = 36f, CarL = 38f, CarW = 22f, Rostrum = 0f, Shell = shell, ShellTex = spotted,
                Segments = 5, SegL = 9f, SegW0 = 20f, SegW1 = 15f, Abdomen = shell, SegTex = (x, y, i, c) => spotted(x, y, c),
                FanL = 16f, FanW = 22f, Fan = C(200, 140, 90),
                AntLen = 0f, ShortAntennules = false,
                Legs = 4, LegLen = 10f, LegR = 2f, LegAngle = 20f, Leg = C(196, 130, 80),
                Eye = new Vector2(9f, 6f), EyeR = 1.8f, Outline = C(70, 40, 24),
            };
            b.Before = (v, k) =>
            {
                // Les antennes aplaties en larges plaques, bord festonné.
                foreach (int s in new[] { -1, 1 })
                {
                    var c = new Vector2(55f + s * 12f, 31f);
                    v.Piece((x, y) => Oval(c, 14f, 6.5f, Side(s, -18f))(x, y) && Mathf.Sin(Mathf.Atan2(y - c.y, x - c.x) * 11f) > -0.9f, C(200, 132, 80), 4f, spotted, 0.7f);
                }
            };
            return b;
        }

        /// <summary>Écrevisse signal : brun olive, grosses pinces lisses au dessous rouge, tache blanc-bleu à l'articulation.</summary>
        private static Body Signal()
        {
            Color brown = C(118, 92, 60);
            var b = new Body
            {
                W = 116, H = 180, Cx = 58f, Top = 62f, CarL = 38f, CarW = 12f, Rostrum = 6f, Shell = brown,
                ShellTex = (x, y, c) => Hash(x / 2, y / 2, 8) > 0.82f ? Darker(c, 0.86f) : c,
                Segments = 6, SegL = 8f, SegW0 = 11f, SegW1 = 7f, Abdomen = brown, FanL = 16f, FanW = 14f, Fan = C(130, 104, 70),
                AntBase = new Vector2(4f, 2f), AntLen = 54f, AntR0 = 1.3f, AntR1 = 0.6f, AntAngle = -98f, AntCurve = 1.2f, Antenna = C(140, 110, 76),
                Legs = 4, LegLen = 13f, LegR = 1.4f, LegAngle = 14f, Leg = C(130, 104, 70),
                Claws = true, ClawY = 9f, ArmLen = 11f, ArmR = 3f, ArmAngle = -18f, ForeLen = 9f, ForeR = 3.6f, ForeAngle = -60f,
                PalmLenR = 26f, PalmWidR = 9f, PalmLenL = 26f, PalmWidL = 9f, PalmAngle = -84f, FingerLen = 12f,
                Palm = C(124, 96, 62), Finger = C(190, 70, 50), FingerTip = C(230, 150, 120),
                ClawTex = (x, y, c) => Hash(x, y, 9) > 0.9f ? Darker(c, 0.86f) : c,
                Eye = new Vector2(4f, 1f), EyeR = 1.8f, Outline = C(40, 30, 18),
            };
            b.After = (v, k) =>
            {
                // Le « signal » : une tache blanc-bleu à l'articulation des doigts.
                foreach (int s in new[] { -1, 1 })
                {
                    ClawArm(null, k, s, out Vector2 wrist);
                    Vector2 hinge = wrist + Dir(Side(s, k.PalmAngle)) * k.PalmLenR * 0.95f + new Vector2(-s * 3f, 0f);
                    v.Piece(Ellipse(hinge.x, hinge.y, 2.6f, 2f), C(214, 236, 236), 1.5f);
                }
            };
            return b;
        }

        /// <summary>Crevette d'Amazonie : corps fin presque transparent, long rostre dentelé, longues pinces fines bleutées.</summary>
        private static Body AmazonShrimp()
        {
            Color glass = C(196, 206, 206);
            Func<int, int, Color, Color> clear = (x, y, c) => Mathf.Abs(x - 60) < 1 ? C(150, 160, 140) : Hash(x, y, 10) > 0.93f ? C(230, 236, 236) : c;
            return new Body
            {
                W = 120, H = 190, Cx = 60f, Top = 70f, CarL = 30f, CarW = 7.5f, Rostrum = 16f, Shell = glass, ShellTex = clear,
                Segments = 6, SegL = 10f, SegW0 = 7f, SegW1 = 4f, Abdomen = glass, SegTex = (x, y, i, c) => clear(x, y, c),
                FanL = 16f, FanW = 12f, Fan = C(206, 214, 214),
                AntBase = new Vector2(3f, 2f), AntLen = 70f, AntR0 = 0.9f, AntR1 = 0.5f, AntAngle = -96f, AntCurve = 1.5f, Antenna = C(200, 206, 200),
                Legs = 4, LegLen = 12f, LegR = 0.9f, LegAngle = 16f, Leg = C(206, 214, 210),
                Claws = true, ClawY = 8f, ArmLen = 20f, ArmR = 1.4f, ArmAngle = -70f, ForeLen = 16f, ForeR = 1.4f, ForeAngle = -94f,
                PalmLenR = 14f, PalmWidR = 2.4f, PalmLenL = 14f, PalmWidL = 2.4f, PalmAngle = -98f, FingerLen = 7f,
                Palm = C(150, 176, 200), Finger = C(150, 176, 200), FingerTip = C(90, 110, 140),
                Eye = new Vector2(4f, 1f), EyeR = 1.8f, Outline = C(90, 100, 100),
            };
        }

        /// <summary>Squille (crevette-mante) : long corps gris-vert en larges anneaux bordés de sombre, bras ravisseurs repliés.</summary>
        private static Body Squilla()
        {
            Color grey = C(150, 158, 126);
            var b = new Body
            {
                W = 96, H = 196, Cx = 48f, Top = 40f, CarL = 24f, CarW = 10f, Rostrum = 2f, Shell = grey,
                ShellTex = (x, y, c) => Mathf.Abs(x - 48) == 4 ? Darker(c, 0.8f) : c,
                Segments = 6, SegL = 15f, SegW0 = 11f, SegW1 = 12f, Abdomen = grey,
                SegTex = (x, y, i, c) =>
                {
                    float local = (y - (40f + 24f + i * 15f)) / 15f;
                    return local > 0.82f ? C(70, 90, 70) : Mathf.Abs(x - 48) % 6 == 0 ? Darker(c, 0.88f) : c;   // bord sombre et carènes
                },
                FanL = 22f, FanW = 16f, Fan = C(170, 170, 130),
                AntBase = new Vector2(3f, 1f), AntLen = 22f, AntR0 = 1f, AntR1 = 0.6f, AntAngle = -110f, AntCurve = 0.5f, Antenna = C(170, 160, 120),
                Legs = 3, LegLen = 10f, LegR = 1.2f, LegAngle = 20f, Leg = C(180, 176, 140),
                Eye = new Vector2(5f, -1f), EyeR = 2.4f, EyeColor = C(40, 70, 60), Outline = C(40, 46, 34),
            };
            b.After = (v, k) => Raptorial(v, k, C(190, 186, 150), C(210, 206, 170), 12f);
            return b;
        }

        /// <summary>Crevette à bandes : rayée de rouge et de blanc, grosses pinces rayées et épineuses, très longues antennes blanches.</summary>
        private static Body Banded()
        {
            Color red = C(206, 44, 44), white = C(246, 244, 240);
            Func<int, int, Color, Color> bands = (x, y, c) => (y / 4) % 2 == 0 ? red : white;
            var b = new Body
            {
                W = 150, H = 170, Cx = 75f, Top = 70f, CarL = 22f, CarW = 7f, Rostrum = 6f, Shell = white, ShellTex = bands,
                Segments = 6, SegL = 7f, SegW0 = 7f, SegW1 = 4.5f, Abdomen = white,
                SegTex = (x, y, i, c) => i % 2 == 0 ? red : white,
                FanL = 12f, FanW = 11f, Fan = red,
                AntBase = new Vector2(2f, 1f), AntLen = 76f, AntR0 = 0.8f, AntR1 = 0.5f, AntAngle = -100f, AntCurve = 2.4f, Antenna = white,
                Legs = 4, LegLen = 14f, LegR = 0.9f, LegAngle = 18f, Leg = white, LegTex = (x, y, c) => (x + y) % 4 < 2 ? red : white,
                Claws = true, ClawY = 6f, ArmLen = 18f, ArmR = 1.8f, ArmAngle = -30f, ForeLen = 14f, ForeR = 2f, ForeAngle = -70f,
                PalmLenR = 20f, PalmWidR = 4.4f, PalmLenL = 20f, PalmWidL = 4.4f, PalmAngle = -96f, FingerLen = 8f,
                Palm = white, Finger = white, FingerTip = red, ClawTex = (x, y, c) => ((x + y) / 4) % 2 == 0 ? red : white,
                Eye = new Vector2(3f, 1f), EyeR = 1.5f, Outline = C(70, 16, 16),
            };
            return b;
        }

        /// <summary>Crevette tigre épineuse : minuscule, bosselée et hérissée, blanche à taches orange tigrées, yeux à cornes.</summary>
        private static Body TigerShrimp()
        {
            Color white = C(244, 236, 226), orange = C(220, 130, 50);
            Func<int, int, Color, Color> tiger = (x, y, c) => Mathf.PerlinNoise(x * 0.5f, y * 0.5f) > 0.58f ? orange : Hash(x, y, 11) > 0.85f ? C(160, 80, 40) : c;
            var b = new Body
            {
                W = 64, H = 84, Cx = 32f, Top = 22f, CarL = 16f, CarW = 7f, Rostrum = 3f, Shell = white, ShellTex = tiger,
                Segments = 5, SegL = 6f, SegW0 = 7f, SegW1 = 4f, Abdomen = white, SegTex = (x, y, i, c) => tiger(x, y, c),
                FanL = 8f, FanW = 8f, Fan = orange,
                AntBase = new Vector2(2f, 1f), AntLen = 18f, AntR0 = 0.7f, AntR1 = 0.5f, AntAngle = -104f, AntCurve = 1f, Antenna = white,
                Legs = 4, LegLen = 7f, LegR = 0.8f, LegAngle = 20f, Leg = white, LegTex = tiger,
                Claws = true, ClawY = 4f, ArmLen = 5f, ArmR = 1f, ArmAngle = -60f, ForeLen = 4f, ForeR = 1.1f, ForeAngle = -95f,
                PalmLenR = 5f, PalmWidR = 1.6f, PalmLenL = 5f, PalmWidL = 1.6f, PalmAngle = -105f, FingerLen = 2.5f,
                Palm = white, Finger = white, FingerTip = orange, ClawTex = tiger,
                Eye = new Vector2(3f, 0f), EyeR = 1.4f, Outline = C(90, 50, 30),
            };
            b.After = (v, k) =>
            {
                foreach (int s in new[] { -1, 1 }) v.Piece(Capsule(32f + s * 3f, 20f, 32f + s * 5f, 16f, 0.7f), orange, 0.8f);   // les cornes des yeux
                for (int i = 0; i < 8; i++) v.Dot(26 + (i * 5) % 13, 26 + (i * 7) % 24, C(250, 250, 246));                   // petites épines
            };
            return b;
        }

        /// <summary>Crevette-mante paon : vert vif tacheté, massues rouges repliées, pattes orange, gros yeux bleu-vert.</summary>
        private static Body Peacock()
        {
            Color green = C(60, 150, 90);
            Func<int, int, Color, Color> leopard = (x, y, c) => Hash(x / 2, y / 2, 12) > 0.84f ? C(30, 70, 50) : Hash(x, y, 13) > 0.93f ? C(150, 210, 120) : c;
            var b = new Body
            {
                W = 96, H = 180, Cx = 48f, Top = 40f, CarL = 26f, CarW = 11f, Rostrum = 2f, Shell = green, ShellTex = leopard,
                Segments = 6, SegL = 13f, SegW0 = 11f, SegW1 = 12f, Abdomen = green,
                SegTex = (x, y, i, c) => { float local = (y - (40f + 26f + i * 13f)) / 13f; return local > 0.8f ? C(40, 100, 60) : leopard(x, y, c); },
                FanL = 20f, FanW = 16f, Fan = C(220, 90, 50),
                AntBase = new Vector2(4f, 0f), AntLen = 16f, AntR0 = 1.6f, AntR1 = 1f, AntAngle = -116f, AntCurve = 0.5f, Antenna = C(236, 120, 60),
                Legs = 3, LegLen = 10f, LegR = 1.4f, LegAngle = 20f, Leg = C(236, 130, 60),
                Eye = new Vector2(5f, -2f), EyeR = 2.8f, EyeColor = C(60, 150, 170), Outline = C(20, 50, 30),
            };
            b.After = (v, k) => Raptorial(v, k, C(240, 120, 70), C(220, 50, 60), 12f);
            return b;
        }

        /// <summary>Triops : grand bouclier rond olive, deux yeux serrés, abdomen qui dépasse et deux longs filaments.</summary>
        private static Body Triops()
        {
            Color olive = C(120, 116, 70);
            var b = new Body
            {
                W = 104, H = 170, Cx = 52f, Top = 30f, CarL = 0f, CarW = 0f, Shell = olive, Rostrum = 0f,
                Segments = 7, SegL = 7f, SegW0 = 9f, SegW1 = 4f, Abdomen = C(150, 130, 90),
                SegTex = (x, y, i, c) => Hash(x, y, 14) > 0.9f ? Darker(c, 0.85f) : c,
                FanL = 0f, AntLen = 0f, ShortAntennules = false, Legs = 0,
                Eye = new Vector2(2.5f, 12f), EyeR = 1.6f, Outline = C(44, 40, 24),
            };
            b.Top = 70f;   // l'abdomen part du milieu du bouclier
            b.Before = (v, k) =>
            {
                // Petites pattes en frange autour du bouclier.
                for (int i = 0; i < 14; i++)
                {
                    float a = (200f + i * 10f) * Mathf.Deg2Rad;
                    foreach (int s in new[] { -1, 1 })
                    {
                        var p = new Vector2(52f + s * Mathf.Cos(a) * 26f, 60f - Mathf.Sin(a) * 30f);
                        v.Piece(Capsule(p.x, p.y, p.x + s * 5f, p.y + 2f, 0.8f), C(200, 120, 90), 1f, null, 0.8f);
                    }
                }

                // Les deux filaments de la queue.
                foreach (int s in new[] { -1, 1 })
                {
                    v.Piece(Capsule(52f + s * 2f, 120f, 52f + s * 10f, 166f, 1f), C(150, 130, 90), 1f, null, 0.8f);
                }
            };
            b.After = (v, k) =>
            {
                // Le bouclier, translucide sur les bords, avec son sillon.
                v.Piece((x, y) => { float u = (x - 52f) / 27f, w = (y - 60f) / 32f; return u * u + w * w <= 1f && y < 86f; }, olive, 10f,
                    (x, y, c) => Mathf.Abs(Mathf.Sqrt(((x - 52f) / 27f) * ((x - 52f) / 27f) + ((y - 60f) / 32f) * ((y - 60f) / 32f)) - 0.55f) < 0.03f
                        ? Darker(c, 0.8f) : Hash(x, y, 15) > 0.9f ? C(150, 146, 96) : c, 0.6f);
                v.Piece(Ellipse(49.5f, 42f, 1.6f, 1.6f), C(20, 18, 14), 1f);
                v.Piece(Ellipse(54.5f, 42f, 1.6f, 1.6f), C(20, 18, 14), 1f);
            };
            b.Eye = new Vector2(0f, -200f);   // les yeux sont posés sur le bouclier
            return b;
        }

        /// <summary>Crevette géante d'eau douce : brune rayée de jaune en long, deux longues pinces épaisses bleutées.</summary>
        private static Body BigRiverShrimp()
        {
            Color brown = C(118, 88, 56);
            Func<int, int, Color, Color> striped = (x, y, c) => Mathf.Abs(x - 60) == 3 || Mathf.Abs(x - 60) == 7 ? C(214, 186, 90) : Hash(x, y, 16) > 0.92f ? Darker(c, 0.8f) : c;
            return new Body
            {
                W = 120, H = 200, Cx = 60f, Top = 76f, CarL = 34f, CarW = 10f, Rostrum = 12f, Shell = brown, ShellTex = striped,
                Segments = 6, SegL = 10f, SegW0 = 9.5f, SegW1 = 6f, Abdomen = brown, SegTex = (x, y, i, c) => striped(x, y, c),
                FanL = 16f, FanW = 14f, Fan = C(140, 110, 70),
                AntBase = new Vector2(3f, 2f), AntLen = 70f, AntR0 = 1f, AntR1 = 0.5f, AntAngle = -96f, AntCurve = 1.6f, Antenna = C(150, 120, 80),
                Legs = 4, LegLen = 13f, LegR = 1.2f, LegAngle = 16f, Leg = C(150, 116, 74),
                Claws = true, ClawY = 8f, ArmLen = 22f, ArmR = 2.4f, ArmAngle = -66f, ForeLen = 18f, ForeR = 2.6f, ForeAngle = -92f,
                PalmLenR = 20f, PalmWidR = 4f, PalmLenL = 18f, PalmWidL = 3.6f, PalmAngle = -98f, FingerLen = 9f,
                Palm = C(80, 110, 150), Finger = C(90, 120, 160), FingerTip = C(230, 170, 90),
                ClawTex = (x, y, c) => Hash(x, y, 17) > 0.88f ? C(140, 170, 200) : c,
                Eye = new Vector2(4f, 1f), EyeR = 2f, Outline = C(44, 32, 20),
            };
        }

        /// <summary>Crevette tigrée : grande crevette sans grosses pinces, abdomen rayé de bandes bleu-noir et jaune pâle, longues antennes.</summary>
        private static Body TigerPrawn()
        {
            Color grey = C(96, 104, 120);
            return new Body
            {
                W = 110, H = 190, Cx = 55f, Top = 66f, CarL = 34f, CarW = 10f, Rostrum = 14f, Shell = grey,
                ShellTex = (x, y, c) => Hash(x / 2, y / 2, 18) > 0.8f ? C(70, 76, 96) : c,
                Segments = 6, SegL = 11f, SegW0 = 10f, SegW1 = 6f, Abdomen = grey,
                SegTex = (x, y, i, c) => { float local = (y - (66f + 34f + i * 11f)) / 11f; return local > 0.55f ? C(36, 40, 60) : local > 0.4f ? C(220, 206, 140) : c; },
                FanL = 18f, FanW = 16f, Fan = C(200, 80, 60),
                AntBase = new Vector2(3f, 2f), AntLen = 78f, AntR0 = 1f, AntR1 = 0.5f, AntAngle = -98f, AntCurve = 2f, Antenna = C(200, 80, 60),
                Legs = 5, LegLen = 14f, LegR = 1.2f, LegAngle = 16f, Leg = C(200, 100, 80),
                Eye = new Vector2(4f, 1f), EyeR = 2f, Outline = C(24, 26, 40),
            };
        }

        /// <summary>Langouste japonaise : rouge brun sombre, carapace hérissée d'épines claires, très longues antennes épaisses.</summary>
        private static Body JapaneseRock()
        {
            Color red = C(126, 46, 30);
            Func<int, int, Color, Color> spiny = (x, y, c) => Hash(x, y, 19) > 0.86f ? C(236, 170, 120) : Hash(x / 3, y / 3, 20) > 0.75f ? Darker(c, 0.82f) : c;
            return new Body
            {
                W = 150, H = 216, Cx = 75f, Top = 70f, CarL = 50f, CarW = 16f, Rostrum = 0f, Shell = red, ShellTex = spiny,
                Segments = 6, SegL = 10f, SegW0 = 15f, SegW1 = 10f, Abdomen = red,
                SegTex = (x, y, i, c) => { float local = (y - (70f + 50f + i * 10f)) / 10f; return local > 0.78f ? C(230, 190, 140) : spiny(x, y, c); },
                FanL = 20f, FanW = 20f, Fan = C(150, 60, 40),
                AntBase = new Vector2(7f, 2f), AntLen = 84f, AntR0 = 3.6f, AntR1 = 0.8f, AntAngle = -98f, AntCurve = 1.6f, Antenna = C(140, 56, 36), AntTex = spiny,
                Legs = 5, LegLen = 17f, LegR = 1.8f, LegAngle = 10f, Leg = C(170, 80, 50), LegTex = (x, y, c) => (x + y) % 6 < 2 ? C(230, 190, 140) : c,
                Eye = new Vector2(5f, 1f), EyeR = 2f, Outline = C(46, 16, 10),
            };
        }
    }
}
