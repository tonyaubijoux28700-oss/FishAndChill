using System;
using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les vrais leurres, appâts et hameçons du marchand, dessinés d'après ceux de la vraie vie (48×32, nez à droite,
    /// y depuis le haut pendant le dessin, style en volume) : appâts naturels (ver de terreau, asticots, crevette grise,
    /// lanière de calmar), cuillères tournantes (palettes Aglia, Colorado, Saule), ondulantes (Toby, Crocodile, Devon),
    /// poissons nageurs (minnow, jerkbait, crankbait), leurres souples (shad, finesse, grub), leurres de surface
    /// (popper, stickbait, walking-the-dog), jigs métalliques (pilker, casting jig, slow jig), turluttes (egi,
    /// phosphorescente), leurres de traîne (jupe octopus, plume), mouches (Adams, Woolly Bugger, Clouser) ; hameçons
    /// Aberdeen, triple, circulaire, O'Shaughnessy forgé, texan, à mouche.
    /// </summary>
    public static class LureArt
    {
        public const int Width = 48;
        public const int Height = 32;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Steel = C(0.72f, 0.74f, 0.78f);
        private static readonly Color SteelDark = C(0.38f, 0.40f, 0.44f);
        private static readonly Color Brass = C(0.86f, 0.68f, 0.30f);
        private static readonly Color Silver = C(0.86f, 0.88f, 0.92f);
        private static readonly Color Copper = C(0.80f, 0.46f, 0.24f);
        private static readonly Color Red = C(0.86f, 0.18f, 0.16f);
        private static readonly Color Ink = C(0.05f, 0.05f, 0.07f);

        private static Color[,] Done(VolumeCanvas cv) => cv.Finish(Ink, 0.5f);

        // ---------- Petites pièces communes ----------

        /// <summary>Hameçon triple qui pend sous <paramref name="at"/> (trois pointes vues de côté).</summary>
        private static void Treble(VolumeCanvas cv, Vector2 at, float size = 1f)
        {
            cv.Piece(Capsule(at.x, at.y, at.x, at.y + 5f * size, 0.6f), SteelDark, 0.6f);
            foreach (float s in new[] { -1f, 1f })
            {
                cv.Piece(Tube(Curve(at + new Vector2(0f, 5f * size), at + new Vector2(s * 3.5f * size, 7f * size), at + new Vector2(s * 3f * size, 3f * size), 6), 0.6f, 0.4f), SteelDark, 0.6f);
            }

            cv.Piece(Tube(Curve(at + new Vector2(0f, 5f * size), at + new Vector2(0.5f, 8f * size), at + new Vector2(1.5f, 6.5f * size), 5), 0.6f, 0.4f), Steel, 0.6f);
        }

        /// <summary>Anneau brisé (là où on attache la ligne).</summary>
        private static void Ring(VolumeCanvas cv, Vector2 at) => cv.Piece(CreatureShapes.Ring(at.x, at.y, 2f, 2f, 0.8f), Steel, 0.6f);

        /// <summary>Œil 3D : iris clair, pupille noire, reflet.</summary>
        private static void Eye(VolumeCanvas cv, Vector2 at, float r, Color iris)
        {
            cv.Piece(Ellipse(at.x, at.y, r, r), iris, 1f, null, 0.95f, 0.9f);
            cv.Piece(Ellipse(at.x + 0.3f, at.y, r * 0.5f, r * 0.55f), Ink, 0.8f, null, 0.95f, 0.9f);
        }

        /// <summary>Corps de poisson nageur (longueur, hauteur) et sa robe (dos, flancs, ventre, motif).</summary>
        private static Func<float, float, bool> MinnowBody(float x0, float x1, float cy, float half, float headBias = 0.3f)
            => (x, y) =>
            {
                if (x < x0 || x > x1) return false;
                float t = (x - x0) / (x1 - x0);
                float h = half * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * (1f - headBias) + headBias * t * t)), 0.7f) + 0.6f;
                return Mathf.Abs(y - cy) <= h;
            };

        private static Func<int, int, Color, Color> Livery(float cy, Color back, Color side, Color belly, bool scales = true)
            => (x, y, c) =>
            {
                Color k = y < cy - 2f ? back : y > cy + 2f ? belly : side;
                if (scales && (x + y * 2) % 4 == 0) k = Color.Lerp(k, Color.white, 0.25f);
                return k;
            };

        // ---------- Appâts naturels ----------

        public static Color[,] Worm()
        {
            var cv = new VolumeCanvas(Width, Height);
            var path = new List<Vector2>();
            for (int i = 0; i <= 30; i++) path.Add(new Vector2(6f + i * 1.2f, 16f + Mathf.Sin(i * 0.45f) * 6f));
            cv.Piece(Tube(path, 2.6f, 2.2f), C(0.72f, 0.36f, 0.38f), 2f, (x, y, c) => x % 3 == 0 ? Darker(c, 0.8f) : (x > 18 && x < 23) ? C(0.82f, 0.48f, 0.46f) : c);
            return Done(cv);
        }

        public static Color[,] Maggots()
        {
            var cv = new VolumeCanvas(Width, Height);
            foreach (Vector2 at in new[] { new Vector2(12, 12), new Vector2(22, 18), new Vector2(30, 11), new Vector2(18, 24), new Vector2(34, 22), new Vector2(26, 26) })
            {
                cv.Piece(Capsule(at.x - 3.5f, at.y, at.x + 3.5f, at.y - 1f, 2f), C(0.94f, 0.90f, 0.78f), 1.6f, (x, y, c) => x % 2 == 0 ? Darker(c, 0.9f) : c);
                cv.Piece(Ellipse(at.x + 3.6f, at.y - 1f, 0.6f, 0.6f), Ink, 0.4f);
            }

            return Done(cv);
        }

        public static Color[,] Shrimp()
        {
            var cv = new VolumeCanvas(Width, Height);
            var body = Curve(new Vector2(36, 16), new Vector2(22, 6), new Vector2(10, 18), 16);
            cv.Piece(Tube(body, 5f, 2f), C(0.74f, 0.70f, 0.62f), 3f, (x, y, c) => x % 4 == 0 ? Darker(c, 0.82f) : c);
            cv.Piece(Poly(new Vector2(10, 18), new Vector2(4, 14), new Vector2(4, 24)), C(0.70f, 0.66f, 0.58f), 1.5f);
            for (int k = 0; k < 5; k++) cv.Piece(Capsule(18 + k * 3.5f, 18, 16 + k * 3.5f, 24, 0.5f), C(0.66f, 0.60f, 0.52f), 0.5f);
            cv.Piece(Tube(Curve(new Vector2(38, 14), new Vector2(46, 4), new Vector2(30, 2), 10), 0.4f, 0.3f), C(0.66f, 0.60f, 0.52f), 0.4f);
            cv.Piece(Ellipse(36, 13, 1.2f, 1.2f), Ink, 0.6f);
            return Done(cv);
        }

        public static Color[,] SquidStrip()
        {
            var cv = new VolumeCanvas(Width, Height);
            cv.Piece(Poly(new Vector2(6, 12), new Vector2(40, 10), new Vector2(42, 16), new Vector2(6, 20)), C(0.92f, 0.86f, 0.84f), 2f,
                (x, y, c) => Hash(x, y) < 0.15f ? C(0.74f, 0.42f, 0.46f) : c);
            for (int k = 0; k < 4; k++) cv.Piece(Capsule(6, 14 + k * 2, 0, 12 + k * 4, 0.7f), C(0.92f, 0.86f, 0.84f), 0.6f);
            return Done(cv);
        }

        // ---------- Cuillères tournantes ----------

        /// <summary>Cuillère tournante : axe d'acier, palette (0 Aglia ovale, 1 Colorado ronde, 2 Saule effilée), corps en perles de laiton, triple à plume.</summary>
        public static Color[,] Spinner(int blade, Color bladeColor)
        {
            var cv = new VolumeCanvas(Width, Height);
            Ring(cv, new Vector2(44, 16));
            cv.Piece(Capsule(10, 16, 42, 16, 0.5f), Steel, 0.6f);
            Vector2 bc = new Vector2(32, 16);
            Func<float, float, bool> shape = blade == 0 ? Ellipse(bc.x, bc.y, 7f, 5f) : blade == 1 ? Ellipse(bc.x, bc.y, 6f, 6f) : Ellipse(bc.x - 1f, bc.y, 10f, 3.5f);
            cv.Piece(shape, bladeColor, 2f, (x, y, c) => x < bc.x - 1 && y < bc.y ? Color.Lerp(c, Color.white, 0.45f) : (x + y) % 5 == 0 ? Darker(c, 0.9f) : c, 0.9f, 0.7f);
            cv.Piece(Capsule(bc.x + 5f, 16, bc.x + 8f, 16, 0.9f), Steel, 0.6f);   // étrier
            for (int k = 0; k < 3; k++) cv.Piece(Ellipse(20 + k * 3.2f, 16, 1.8f, 2f), Brass, 1.2f);
            cv.Piece(Poly(new Vector2(10, 16), new Vector2(4, 12), new Vector2(2, 16), new Vector2(4, 20)), Red, 1f, (x, y, c) => (x + y) % 2 == 0 ? Darker(c, 0.8f) : c);   // plume
            Treble(cv, new Vector2(11, 17), 0.9f);
            return Done(cv);
        }

        // ---------- Cuillères ondulantes ----------

        public static Color[,] Spoon(int model)
        {
            var cv = new VolumeCanvas(Width, Height);
            if (model == 2)
            {
                // Devon : tube de métal à deux ailettes en hélice à l'avant, triple à l'arrière.
                cv.Piece(Capsule(14, 16, 38, 16, 4f), Copper, 3f, (x, y, c) => y < 14 ? Color.Lerp(c, Color.white, 0.35f) : x % 6 == 0 ? Darker(c, 0.85f) : c);
                cv.Piece(Poly(new Vector2(36, 12), new Vector2(42, 4), new Vector2(40, 12)), Silver, 1f);
                cv.Piece(Poly(new Vector2(36, 20), new Vector2(42, 28), new Vector2(40, 20)), Silver, 1f);
                cv.Piece(Capsule(38, 16, 44, 16, 0.5f), Steel, 0.6f);
                Ring(cv, new Vector2(45, 16));
                Treble(cv, new Vector2(12, 17));
                return Done(cv);
            }

            // Toby (ondulée, à ailettes découpées) ou Crocodile (longue, plate, écailles martelées).
            bool toby = model == 0;
            Func<float, float, bool> shape = (x, y) =>
            {
                if (x < 6 || x > 42) return false;
                float t = (x - 6f) / 36f;
                float h = (toby ? 6f : 4.5f) * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.8f)) + 1f;
                float bend = Mathf.Sin(t * Mathf.PI * 2f) * (toby ? 1.5f : 1f);
                if (toby && Mathf.Abs(t - 0.62f) < 0.03f && Mathf.Abs(y - 16f - bend) > h - 2f) return false;   // ailettes découpées
                return Mathf.Abs(y - 16f - bend) <= h;
            };
            Color face = toby ? Silver : C(0.94f, 0.80f, 0.30f);
            cv.Piece(shape, face, 2.5f, (x, y, c) =>
            {
                if (toby && y > 16 && x > 12 && x < 36 && ((x / 3 + y / 2) % 2 == 0)) return C(0.20f, 0.42f, 0.70f);   // dos bleu à points
                if (!toby && (x * 3 + y * 5) % 7 == 0) return Darker(c, 0.82f);   // écailles martelées
                return y < 15 ? Color.Lerp(c, Color.white, 0.4f) : c;
            }, 0.9f, 0.6f);
            if (!toby) cv.Piece(Capsule(12, 16, 38, 16, 0.6f), Red, 0.6f);   // bande rouge
            Ring(cv, new Vector2(44, 16));
            Treble(cv, new Vector2(5, 17));
            return Done(cv);
        }

        // ---------- Poissons nageurs ----------

        /// <summary>Poisson nageur : 0 minnow flottant (long, fine bavette), 1 jerkbait (élancé, robe de perche), 2 crankbait (trapu, grosse bavette).</summary>
        public static Color[,] Minnow(int model)
        {
            var cv = new VolumeCanvas(Width, Height);
            float x0 = model == 2 ? 12f : 4f, x1 = 38f, half = model == 2 ? 8f : model == 1 ? 4.5f : 5f;
            Color back = model == 0 ? C(0.16f, 0.30f, 0.36f) : model == 1 ? C(0.30f, 0.46f, 0.16f) : C(0.84f, 0.30f, 0.10f);
            Color side = model == 0 ? Silver : model == 1 ? C(0.80f, 0.74f, 0.30f) : C(0.96f, 0.80f, 0.20f);
            Color belly = model == 2 ? C(0.96f, 0.92f, 0.80f) : C(0.96f, 0.96f, 0.96f);
            // La bavette, translucide, devant la tête.
            float lipLen = model == 2 ? 9f : model == 1 ? 3f : 5f;
            cv.Piece(Poly(new Vector2(x1 - 1f, 18), new Vector2(x1 + lipLen, 20 + lipLen * 0.4f), new Vector2(x1 + lipLen - 2f, 24 + lipLen * 0.3f), new Vector2(x1 - 3f, 21)),
                C(0.82f, 0.90f, 0.94f, 0.8f), 1f);
            cv.Piece(MinnowBody(x0, x1, 16f, half, model == 2 ? 0.1f : 0.3f), side, 3f, (x, y, c) =>
            {
                Color k = Livery(16f, back, side, belly)(x, y, c);
                if (model == 1 && y < 16 && (x / 4) % 2 == 0) k = Darker(k, 0.7f);   // barres de la perche
                return k;
            }, 0.9f, 0.6f);
            Eye(cv, new Vector2(x1 - 4f, 15f), model == 2 ? 2.4f : 1.8f, C(0.96f, 0.84f, 0.30f));
            cv.Piece(Capsule(x1 - 6f, 13, x1 - 6f, 19, 0.4f), Darker(side, 0.7f), 0.4f);   // ouïe
            Ring(cv, new Vector2(x1 + 2f, 17f));
            Treble(cv, new Vector2((x0 + x1) * 0.55f, 16f + half), 0.9f);
            Treble(cv, new Vector2(x0 + 1f, 17f), 0.9f);
            return Done(cv);
        }

        // ---------- Leurres souples ----------

        /// <summary>Leurre souple : 0 shad à palette sur tête plombée, 1 finesse (ver droit), 2 grub à queue vrillée ; monté sur texan ou tête plombée.</summary>
        public static Color[,] SoftBait(int model, Color body)
        {
            var cv = new VolumeCanvas(Width, Height);
            Func<int, int, Color, Color> ribs = (x, y, c) => x % 3 == 0 ? Darker(c, 0.85f) : (x + y) % 9 == 0 ? Color.Lerp(c, Color.white, 0.4f) : c;
            if (model == 0)
            {
                cv.Piece(MinnowBody(8f, 36f, 16f, 6f, 0.1f), body, 3f, ribs, 0.9f, 0.6f);
                cv.Piece(Poly(new Vector2(9, 14), new Vector2(3, 6), new Vector2(1, 16), new Vector2(3, 26), new Vector2(9, 18)), body, 2f, ribs);   // palette
                cv.Piece(Ellipse(39f, 16f, 5f, 5.5f), C(0.44f, 0.46f, 0.50f), 2.5f, (x, y, c) => y < 14 ? Color.Lerp(c, Color.white, 0.35f) : c);   // tête plombée
                Eye(cv, new Vector2(40, 15), 1.8f, Red);
                cv.Piece(Tube(Curve(new Vector2(36, 16), new Vector2(26, 18), new Vector2(22, 10), 8), 0.6f, 0.4f), SteelDark, 0.6f);   // pointe qui ressort du dos
            }
            else if (model == 1)
            {
                var path = new List<Vector2>();
                for (int i = 0; i <= 30; i++) path.Add(new Vector2(4f + i * 1.25f, 17f + Mathf.Sin(i * 0.25f) * 2f));
                cv.Piece(Tube(path, 1.4f, 2.4f), body, 2f, ribs, 0.9f, 0.6f);
                cv.Piece(Tube(Curve(new Vector2(42, 12), new Vector2(46, 18), new Vector2(38, 20), 8), 0.6f, 0.4f), SteelDark, 0.6f);   // texan
                Ring(cv, new Vector2(44, 10));
            }
            else
            {
                cv.Piece(Capsule(20, 16, 38, 16, 4f), body, 3f, ribs, 0.9f, 0.6f);
                var tail = new List<Vector2>();
                for (int i = 0; i <= 20; i++) tail.Add(new Vector2(20f - i * 0.8f, 16f + Mathf.Sin(i * 0.55f) * 6f));
                cv.Piece(Tube(tail, 2.6f, 0.8f), body, 1.5f, ribs, 0.9f, 0.6f);
                cv.Piece(Ellipse(40f, 16f, 4f, 4.5f), C(0.44f, 0.46f, 0.50f), 2f);
                Eye(cv, new Vector2(41, 15), 1.4f, C(0.96f, 0.84f, 0.30f));
            }

            return Done(cv);
        }

        // ---------- Leurres de surface ----------

        /// <summary>Leurre de surface : 0 popper (bouche creuse qui fait éclabousser), 1 stickbait (cigare effilé), 2 walking-the-dog (banane).</summary>
        public static Color[,] Surface(int model)
        {
            var cv = new VolumeCanvas(Width, Height);
            Color back = model == 0 ? C(0.10f, 0.50f, 0.44f) : model == 1 ? C(0.20f, 0.24f, 0.60f) : C(0.94f, 0.94f, 0.92f);
            Color side = model == 2 ? C(0.96f, 0.96f, 0.94f) : C(0.86f, 0.90f, 0.92f);
            if (model == 0)
            {
                cv.Piece(Poly(new Vector2(8, 13), new Vector2(38, 9), new Vector2(40, 16), new Vector2(38, 23), new Vector2(8, 19)), side, 3f, Livery(16f, back, side, C(0.98f, 0.98f, 0.96f)), 0.9f, 0.6f);
                cv.Piece(Ellipse(39.5f, 16f, 2f, 6.4f), C(0.12f, 0.10f, 0.10f), 1f);   // la bouche creuse
                Eye(cv, new Vector2(34, 13), 1.8f, C(0.96f, 0.84f, 0.30f));
                Ring(cv, new Vector2(42, 16));
                cv.Piece(Poly(new Vector2(8, 16), new Vector2(2, 11), new Vector2(2, 21)), Red, 1f);   // plumes de la queue
                Treble(cv, new Vector2(24, 20));
                Treble(cv, new Vector2(8, 18));
            }
            else
            {
                float bend = model == 2 ? 3f : 0f;
                Func<float, float, bool> body = (x, y) =>
                {
                    if (x < 4 || x > 42) return false;
                    float t = (x - 4f) / 38f;
                    float cy = 16f + Mathf.Sin(t * Mathf.PI) * bend;
                    float h = 4.5f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 0.9f + 0.05f)) + 0.5f;
                    return Mathf.Abs(y - cy) <= h;
                };
                cv.Piece(body, side, 3f, (x, y, c) =>
                {
                    Color k = Livery(16f, back, side, C(0.98f, 0.98f, 0.96f))(x, y, c);
                    if (model == 2 && x > 34) k = Red;   // tête rouge du walking-the-dog
                    return k;
                }, 0.9f, 0.6f);
                Eye(cv, new Vector2(38, 15), 1.6f, model == 2 ? C(0.96f, 0.84f, 0.30f) : Silver);
                Ring(cv, new Vector2(44, 16));
                Treble(cv, new Vector2(24, 20 + bend));
                Treble(cv, new Vector2(5, 17));
            }

            return Done(cv);
        }

        // ---------- Jigs métalliques ----------

        /// <summary>Jig métallique : 0 pilker norvégien (barre lourde à facettes, triple), 1 casting jig (fin, holographique), 2 slow jig (feuille plate, hameçons d'assist).</summary>
        public static Color[,] Jig(int model)
        {
            var cv = new VolumeCanvas(Width, Height);
            if (model == 0)
            {
                cv.Piece(Poly(new Vector2(6, 13), new Vector2(38, 11), new Vector2(40, 16), new Vector2(38, 21), new Vector2(6, 19)), Silver, 3f,
                    (x, y, c) => y < 14 ? Color.Lerp(c, Color.white, 0.5f) : y > 18 ? Darker(c, 0.7f) : (x % 8 == 0 ? Darker(c, 0.85f) : c), 0.9f, 0.7f);
                cv.Piece(Capsule(10, 16, 30, 16, 0.8f), Red, 0.6f);
                Ring(cv, new Vector2(43, 16));
                Treble(cv, new Vector2(5, 17), 1.1f);
            }
            else if (model == 1)
            {
                cv.Piece(Poly(new Vector2(8, 15), new Vector2(40, 13), new Vector2(42, 16), new Vector2(40, 19), new Vector2(8, 17)), C(0.50f, 0.70f, 0.90f), 2f,
                    (x, y, c) => ((x + y) % 4 == 0) ? C(0.90f, 0.60f, 0.90f) : ((x + y) % 4 == 1) ? C(0.70f, 0.94f, 0.80f) : y < 15 ? Color.Lerp(c, Color.white, 0.4f) : c, 0.9f, 0.7f);
                Eye(cv, new Vector2(38, 15.5f), 1.2f, C(0.96f, 0.84f, 0.30f));
                Ring(cv, new Vector2(44, 16));
                Treble(cv, new Vector2(7, 17), 0.9f);
            }
            else
            {
                cv.Piece(Ellipse(24f, 16f, 15f, 6f), C(0.96f, 0.56f, 0.22f), 2.5f, (x, y, c) => y < 14 ? C(0.96f, 0.86f, 0.40f) : (x / 3) % 2 == 0 ? Darker(c, 0.9f) : c, 0.9f, 0.7f);
                cv.Piece(Ellipse(34f, 16f, 2f, 2f), Ink, 0.8f);   // glow dot
                Ring(cv, new Vector2(42, 16));
                // Deux hameçons d'assist sur leur cordelette, devant.
                foreach (float s in new[] { -1f, 1f })
                {
                    cv.Piece(Tube(Curve(new Vector2(42, 16), new Vector2(46, 16 + s * 4f), new Vector2(44, 16 + s * 9f), 6), 0.5f, 0.5f), C(0.94f, 0.86f, 0.40f), 0.5f);
                    cv.Piece(Tube(Curve(new Vector2(44, 16 + s * 9f), new Vector2(40, 16 + s * 12f), new Vector2(38, 16 + s * 9f), 6), 0.6f, 0.4f), SteelDark, 0.6f);
                }
            }

            return Done(cv);
        }

        // ---------- Turluttes ----------

        /// <summary>Turlutte : 0 egi japonaise (crevette en bois habillée de tissu, nageoires, couronne d'aiguilles), 1 phosphorescente (fuseau luminescent).</summary>
        public static Color[,] SquidJig(int model)
        {
            var cv = new VolumeCanvas(Width, Height);
            Color cloth = model == 0 ? C(0.96f, 0.50f, 0.20f) : C(0.70f, 0.96f, 0.70f);
            cv.Piece(MinnowBody(10f, 40f, 16f, 5f, 0.25f), cloth, 3f, (x, y, c) =>
            {
                if (model == 0) return (x + y) % 3 == 0 ? Darker(c, 0.8f) : y < 14 ? C(0.96f, 0.80f, 0.40f) : c;   // tissu tressé
                return Mathf.Repeat(x * 0.2f, 1f) < 0.3f ? Color.Lerp(c, Color.white, 0.4f) : c;
            }, 0.9f, 0.8f);
            Eye(cv, new Vector2(36, 15), 1.8f, C(0.30f, 0.30f, 0.34f));
            if (model == 0)
            {
                cv.Piece(Poly(new Vector2(22, 21), new Vector2(18, 28), new Vector2(26, 22)), cloth, 1f);   // nageoire
                cv.Piece(Poly(new Vector2(34, 20), new Vector2(38, 27), new Vector2(40, 21)), C(0.50f, 0.52f, 0.56f), 1f);   // bavette plombée
            }

            // La couronne d'aiguilles à l'arrière, sans ardillon.
            cv.Piece(Capsule(6, 16, 10, 16, 1f), C(0.80f, 0.82f, 0.86f), 0.8f);
            for (int k = 0; k < 6; k++)
            {
                float a = -1.2f + k * 0.48f;
                cv.Piece(Tube(Curve(new Vector2(6, 16), new Vector2(6f - Mathf.Cos(a) * 3f, 16f + Mathf.Sin(a) * 4f), new Vector2(7f - Mathf.Cos(a) * 3f, 16f + Mathf.Sin(a) * 7f), 5), 0.4f, 0.3f), Steel, 0.4f);
            }

            Ring(cv, new Vector2(43, 16));
            return Done(cv);
        }

        // ---------- Traîne ----------

        /// <summary>Leurre de traîne : 0 jupe octopus (tête de résine et jupe de lanières), 1 plume (tête chromée et plumes).</summary>
        public static Color[,] Troll(int model)
        {
            var cv = new VolumeCanvas(Width, Height);
            Color skirt = model == 0 ? C(0.96f, 0.36f, 0.62f) : C(0.96f, 0.96f, 0.94f);
            for (int k = 0; k < 9; k++)
            {
                float y0 = 12f + k, sway = (k % 2 == 0 ? 1f : -1f) * 1.5f;
                cv.Piece(Tube(Curve(new Vector2(30, y0), new Vector2(18, y0 + sway), new Vector2(4, y0 + (k - 4) * 1.2f), 8), 1f, 0.5f),
                    k % 3 == 0 ? Darker(skirt, 0.8f) : skirt, 1f, model == 1 ? (x, y, c) => x % 2 == 0 ? Color.Lerp(c, C(0.60f, 0.60f, 0.70f), 0.4f) : c : null);
            }

            cv.Piece(Ellipse(34f, 16f, 8f, 6.5f), model == 0 ? C(0.40f, 0.70f, 0.96f) : Silver, 3f, (x, y, c) => y < 14 ? Color.Lerp(c, Color.white, 0.5f) : c, 0.9f, 0.8f);
            Eye(cv, new Vector2(36, 14), 2.2f, C(0.96f, 0.84f, 0.30f));
            cv.Piece(Capsule(10, 16, 44, 16, 0.5f), C(0.40f, 0.40f, 0.42f), 0.5f);
            cv.Piece(Tube(Curve(new Vector2(10, 16), new Vector2(4, 22), new Vector2(10, 24), 6), 0.8f, 0.5f), SteelDark, 0.7f);   // gros hameçon
            return Done(cv);
        }

        // ---------- Mouches ----------

        /// <summary>Mouche : 0 Adams (sèche : queue, corps gris, ailes grises, collerette de hackle), 1 Woolly Bugger (marabout noir, chenille, hackle), 2 Clouser (bucktail bicolore, yeux haltères).</summary>
        public static Color[,] Fly(int model)
        {
            var cv = new VolumeCanvas(Width, Height);
            // L'hameçon à mouche, toujours là : courbure et pointe en bas, œillet à droite.
            cv.Piece(Tube(Curve(new Vector2(38, 16), new Vector2(14, 16), new Vector2(12, 24), 10), 0.6f, 0.5f), SteelDark, 0.6f);
            cv.Piece(Tube(Curve(new Vector2(12, 24), new Vector2(14, 28), new Vector2(18, 24), 6), 0.5f, 0.3f), SteelDark, 0.5f);
            cv.Piece(CreatureShapes.Ring(40f, 15f, 1.6f, 1.6f, 0.6f), SteelDark, 0.5f);
            if (model == 0)
            {
                cv.Piece(Poly(new Vector2(16, 16), new Vector2(4, 13), new Vector2(4, 18)), C(0.60f, 0.56f, 0.50f), 0.8f, (x, y, c) => y % 2 == 0 ? Darker(c, 0.8f) : c);
                cv.Piece(Capsule(16, 16, 32, 15, 2f), C(0.56f, 0.56f, 0.58f), 1.5f, (x, y, c) => x % 2 == 0 ? Darker(c, 0.85f) : c);
                foreach (float s in new[] { -1f, 1f }) cv.Piece(Poly(new Vector2(32, 14), new Vector2(30 + s * 5f, 3), new Vector2(34 + s * 5f, 4)), C(0.74f, 0.74f, 0.76f), 0.8f);
                for (int k = 0; k < 12; k++)
                {
                    float a = k * Mathf.PI / 6f;
                    cv.Piece(Capsule(33, 15, 33 + Mathf.Cos(a) * 6f, 15 + Mathf.Sin(a) * 6f, 0.4f), k % 2 == 0 ? C(0.56f, 0.36f, 0.20f) : C(0.40f, 0.42f, 0.46f), 0.4f);
                }
            }
            else if (model == 1)
            {
                for (int k = 0; k < 7; k++)
                {
                    float y0 = 13f + k;
                    cv.Piece(Tube(Curve(new Vector2(20, y0), new Vector2(10, y0 + (k % 2 == 0 ? 2f : -2f)), new Vector2(2, y0 + (k - 3) * 1.4f), 8), 1f, 0.5f), C(0.10f, 0.10f, 0.12f), 1f);
                }

                cv.Piece(Capsule(18, 16, 36, 16, 3.2f), C(0.22f, 0.24f, 0.18f), 2f, (x, y, c) => (x % 3 == 0) ? C(0.30f, 0.50f, 0.30f) : c);
                for (int k = 0; k < 8; k++) cv.Piece(Capsule(20 + k * 2f, 12, 21 + k * 2f, 20, 0.4f), C(0.14f, 0.12f, 0.10f), 0.4f);
            }
            else
            {
                cv.Piece(Poly(new Vector2(36, 16), new Vector2(4, 10), new Vector2(6, 16)), C(0.96f, 0.96f, 0.94f), 1f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.9f) : c);
                cv.Piece(Poly(new Vector2(36, 16), new Vector2(6, 16), new Vector2(4, 22)), C(0.36f, 0.70f, 0.30f), 1f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.9f) : c);
                cv.Piece(Capsule(10, 16, 30, 16, 0.5f), C(0.90f, 0.80f, 0.40f), 0.5f);   // flash doré
                cv.Piece(Ellipse(34f, 14f, 2.4f, 2.4f), C(0.80f, 0.20f, 0.18f), 1.5f);   // yeux haltères
                cv.Piece(Ellipse(34f, 14f, 1f, 1f), Ink, 0.6f);
            }

            return Done(cv);
        }

        // ---------- Hameçons ----------

        /// <summary>
        /// Hameçon : 0 Aberdeen (long, fin, fil clair), 1 triple, 2 circulaire (pointe tournée vers la hampe), 3 O'Shaughnessy
        /// forgé (épais, bronzé), 4 texan / offset (hampe coudée), 5 à mouche (petit, œillet relevé). Vu de côté, œillet en haut.
        /// </summary>
        public static Color[,] Hook(int model)
        {
            var cv = new VolumeCanvas(Width, Height);
            Color metal = model == 3 ? C(0.52f, 0.40f, 0.22f) : model == 0 ? C(0.80f, 0.82f, 0.86f) : C(0.44f, 0.46f, 0.50f);
            float wire = model == 3 ? 1.4f : model == 0 || model == 5 ? 0.7f : 1f;
            void J(float x, float scale)
            {
                Vector2 eye = new Vector2(x, 4f);
                cv.Piece(CreatureShapes.Ring(eye.x, eye.y, 2f * scale, 2f * scale, 0.8f), metal, 0.8f);
                var shank = model == 4
                    ? new List<Vector2> { new Vector2(x, 6f), new Vector2(x - 2f, 8f), new Vector2(x - 2f, 22f * scale + 4f) }
                    : new List<Vector2> { new Vector2(x, 6f), new Vector2(x, (model == 0 ? 24f : 18f) * scale + 4f) };
                cv.Piece(Tube(shank, wire, wire), metal, 1f, (px, py, c) => px < x ? Color.Lerp(c, Color.white, 0.4f) : c);
                Vector2 bottom = shank[shank.Count - 1];
                if (model == 2)
                {
                    cv.Piece(Tube(Curve(bottom, bottom + new Vector2(6f, 10f), bottom + new Vector2(12f, 0f), 12), wire, wire), metal, 1f);
                    cv.Piece(Tube(Curve(bottom + new Vector2(12f, 0f), bottom + new Vector2(12f, -6f), bottom + new Vector2(6f, -6f), 8), wire, 0.4f), metal, 1f);
                }
                else
                {
                    cv.Piece(Tube(Curve(bottom, bottom + new Vector2(5f * scale, 8f * scale), bottom + new Vector2(10f * scale, 0f), 10), wire, wire), metal, 1f);
                    cv.Piece(Tube(new List<Vector2> { bottom + new Vector2(10f * scale, 0f), bottom + new Vector2(10f * scale, -8f * scale) }, wire, 0.3f), metal, 1f);
                    cv.Piece(Poly(bottom + new Vector2(10f * scale, -5f * scale), bottom + new Vector2(8f * scale, -3f * scale), bottom + new Vector2(10f * scale, -3f * scale)), metal, 0.5f);   // ardillon
                }
            }

            if (model == 1)
            {
                // Triple : trois branches soudées sur la même hampe.
                J(24f, 0.8f);
                cv.Piece(Tube(Curve(new Vector2(24, 20), new Vector2(18, 26), new Vector2(14, 18), 10), 1f, 0.4f), metal, 1f);
                cv.Piece(Tube(Curve(new Vector2(24, 20), new Vector2(26, 27), new Vector2(30, 22), 8), 1f, 0.4f), Darker(metal, 0.8f), 1f);
            }
            else
            {
                J(model == 5 ? 26f : 20f, model == 5 ? 0.6f : 1f);
            }

            return Done(cv);
        }
    }
}
