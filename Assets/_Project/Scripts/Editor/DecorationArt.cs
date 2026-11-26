using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les décorations d'aquarium qu'on ramasse en plongée, dans le style en volume, à l'échelle du jeu (16 px par case,
    /// y depuis le haut pendant le dessin, pivot au pied). Le même dessin sert sur le fond marin et dans le bassin.
    /// Île Mahana : corail branchu, corail cerveau, corail table, anémone, gorgone, conque, étoile de mer, rocher de lave,
    /// bénitier géant, ancre rouillée.
    /// </summary>
    public static class DecorationArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Ink = C(0.10f, 0.08f, 0.10f);

        /// <summary>Le dessin d'une décoration (par son identifiant), ou null si elle n'existe pas.</summary>
        public static Color[,] Draw(string id)
        {
            switch (id)
            {
                case "corail-branchu": return BranchCoral();
                case "corail-cerveau": return BrainCoral();
                case "corail-table": return TableCoral();
                case "anemone": return Anemone();
                case "gorgone": return SeaFan();
                case "conque": return Conch();
                case "etoile-de-mer": return Starfish();
                case "rocher-de-lave": return LavaRock();
                case "benitier": return GiantClam();
                case "ancre": return Anchor();
                default: return null;
            }
        }

        public static readonly Vector2 Pivot = new Vector2(0.5f, 2f / 32f);

        private static Color[,] Done(VolumeCanvas cv) => cv.Finish(Ink, 0.55f);

        /// <summary>Icône de la boutique, 32×32 : masque de plongée à la vitre bleue, tuba jaune sur le côté.</summary>
        public static Color[,] MaskIcon()
        {
            var cv = new VolumeCanvas(32, 32);
            cv.Piece(Tube(Curve(new Vector2(25f, 22f), new Vector2(28f, 12f), new Vector2(26f, 3f), 10), 1.8f, 1.8f), C(0.98f, 0.80f, 0.18f), 1.5f);   // le tuba
            cv.Piece(Ellipse(26f, 3f, 2.4f, 1.6f), C(0.20f, 0.20f, 0.24f), 1f);
            cv.Piece(Capsule(3f, 16f, 29f, 16f, 2f), C(0.14f, 0.20f, 0.36f), 1.5f);   // la sangle
            cv.Piece(Union(Ellipse(10.5f, 17f, 6.5f, 6f), Ellipse(21.5f, 17f, 6.5f, 6f)), C(0.20f, 0.30f, 0.52f), 3f);   // le cadre
            foreach (float x in new[] { 10.5f, 21.5f })
            {
                cv.Piece(Ellipse(x, 17f, 4.6f, 4.2f), C(0.56f, 0.84f, 0.96f), 2f, (px, py, c) => px < x - 1f && py < 16 ? C(0.90f, 0.98f, 1f) : c);
            }

            return Done(cv);
        }

        /// <summary>Corail branchu rose (acropore) : touffe de branches qui se divisent, bouts plus clairs.</summary>
        private static Color[,] BranchCoral()
        {
            var cv = new VolumeCanvas(32, 32);
            Color pink = C(0.94f, 0.48f, 0.58f), tip = C(1f, 0.82f, 0.84f);
            void Branch(Vector2 a, Vector2 b, float r, int depth)
            {
                cv.Piece(Capsule(a.x, a.y, b.x, b.y, r), pink, r + 0.5f, (x, y, c) => Vector2.Distance(new Vector2(x, y), b) < r + 0.6f ? tip : c);
                if (depth <= 0) return;
                Vector2 d = (b - a).normalized;
                foreach (float turn in new[] { -0.55f, 0.5f })
                {
                    Vector2 nd = new Vector2(d.x * Mathf.Cos(turn) - d.y * Mathf.Sin(turn), d.x * Mathf.Sin(turn) + d.y * Mathf.Cos(turn));
                    Branch(b, b + nd * (Vector2.Distance(a, b) * 0.72f), r * 0.78f, depth - 1);
                }
            }

            Branch(new Vector2(16f, 30f), new Vector2(16f, 21f), 2.2f, 3);
            Branch(new Vector2(14f, 30f), new Vector2(8f, 23f), 1.8f, 2);
            Branch(new Vector2(18f, 30f), new Vector2(24f, 23f), 1.8f, 2);
            return Done(cv);
        }

        /// <summary>Corail cerveau : dôme crème couvert de sillons sinueux.</summary>
        private static Color[,] BrainCoral()
        {
            var cv = new VolumeCanvas(32, 24);
            cv.Piece((x, y) => Ellipse(16f, 20f, 13f, 11f)(x, y) && y < 22.5f, C(0.86f, 0.80f, 0.52f), 6f, (x, y, c) =>
            {
                float n = Mathf.Sin(x * 0.9f + Mathf.Sin(y * 0.7f) * 2.2f) + Mathf.Sin(y * 1.1f + x * 0.2f);
                return Mathf.Abs(n) < 0.35f ? C(0.56f, 0.48f, 0.28f) : c;
            });
            return Done(cv);
        }

        /// <summary>Corail table : un large plateau vert-bleu sur un pied court.</summary>
        private static Color[,] TableCoral()
        {
            var cv = new VolumeCanvas(40, 24);
            Color green = C(0.44f, 0.70f, 0.62f);
            cv.Piece(Capsule(20f, 22f, 20f, 13f, 2.6f), Darker(green, 0.8f), 2.5f);
            cv.Piece(Ellipse(20f, 11f, 18f, 5f), green, 4f, (x, y, c) => (x * 3 + y * 5) % 7 == 0 ? C(0.70f, 0.90f, 0.84f) : y > 12 ? Darker(c, 0.8f) : c);
            return Done(cv);
        }

        /// <summary>Anémone : pied orangé, couronne de tentacules violettes aux bouts clairs qui ondulent.</summary>
        private static Color[,] Anemone()
        {
            var cv = new VolumeCanvas(32, 28);
            Color violet = C(0.70f, 0.42f, 0.84f), tip = C(0.96f, 0.84f, 1f);
            cv.Piece(Ellipse(16f, 23f, 8f, 4f), C(0.92f, 0.56f, 0.30f), 3f);
            for (int i = 0; i < 13; i++)
            {
                float a = Mathf.Lerp(-2.7f, -0.45f, i / 12f);
                Vector2 root = new Vector2(16f + Mathf.Cos(a) * 6f, 20f);
                Vector2 end = root + new Vector2(Mathf.Cos(a) * 6f + Mathf.Sin(i) * 1.5f, Mathf.Sin(a) * 11f);
                cv.Piece(Tube(Curve(root, (root + end) * 0.5f + new Vector2(Mathf.Sin(i * 1.7f) * 2f, 0f), end, 8), 1.4f, 0.9f), violet, 1.2f,
                    (x, y, c) => Vector2.Distance(new Vector2(x, y), end) < 1.6f ? tip : c);
            }

            return Done(cv);
        }

        /// <summary>Gorgone : grand éventail violet en dentelle, sur un tronc court.</summary>
        private static Color[,] SeaFan()
        {
            var cv = new VolumeCanvas(32, 36);
            Color fan = C(0.58f, 0.30f, 0.66f);
            Func<float, float, bool> shape = (x, y) => Ellipse(16f, 15f, 13f, 13f)(x, y) && y < 27f;
            cv.Piece((x, y) => shape(x, y) && ((int)x % 3 == 0 || (int)(y + x * 0.4f) % 3 == 0), fan, 1.5f);
            cv.Piece(Capsule(16f, 34f, 16f, 22f, 1.4f), Darker(fan, 0.7f), 1.2f);
            for (int k = -2; k <= 2; k++) cv.Piece(Tube(Curve(new Vector2(16f, 24f), new Vector2(16f + k * 3f, 16f), new Vector2(16f + k * 5.5f, 5f + Mathf.Abs(k) * 2f), 8), 1f, 0.5f), Darker(fan, 0.85f), 1f);
            return Done(cv);
        }

        /// <summary>Conque (lambi) : coquille en spirale, lèvre rose nacrée.</summary>
        private static Color[,] Conch()
        {
            var cv = new VolumeCanvas(32, 20);
            cv.Piece(Union(Ellipse(14f, 12f, 10f, 6f), Poly(new Vector2(4f, 11f), new Vector2(-1f, 8f), new Vector2(6f, 7f))), C(0.90f, 0.78f, 0.62f), 4f,
                (x, y, c) => (x + y * 2) % 6 == 0 ? Darker(c, 0.82f) : c);
            for (int k = 0; k < 4; k++) cv.Piece(Ellipse(22f - k * 4f, 8f - k * 0.6f, 1.8f, 1.6f), C(0.84f, 0.70f, 0.54f), 1.5f);   // pointes de la spirale
            cv.Piece(Ellipse(18f, 14f, 8f, 3.2f), C(0.98f, 0.66f, 0.62f), 2.5f, (x, y, c) => y > 15 ? C(1f, 0.86f, 0.80f) : c);   // la lèvre nacrée
            return Done(cv);
        }

        /// <summary>Étoile de mer rouge à cinq bras, petites perles claires.</summary>
        private static Color[,] Starfish()
        {
            var cv = new VolumeCanvas(24, 20);
            Color red = C(0.88f, 0.28f, 0.22f);
            for (int i = 0; i < 5; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI * 2f / 5f;
                Vector2 end = new Vector2(12f + Mathf.Cos(a) * 10f, 11f + Mathf.Sin(a) * 7.5f);
                cv.Piece(Tube(Curve(new Vector2(12f, 11f), Vector2.Lerp(new Vector2(12f, 11f), end, 0.5f), end, 6), 2.6f, 1f), red, 2f, (x, y, c) => (x * 5 + y * 3) % 5 == 0 ? C(1f, 0.80f, 0.66f) : c);
            }

            cv.Piece(Ellipse(12f, 11f, 3f, 2.4f), red, 2f);
            return Done(cv);
        }

        /// <summary>Rocher de lave noire, poreux, deux touffes d'algues vertes.</summary>
        private static Color[,] LavaRock()
        {
            var cv = new VolumeCanvas(32, 24);
            cv.Piece((x, y) => Ellipse(16f, 17f, 13f, 7f)(x, y) || Ellipse(11f, 12f, 7f, 6f)(x, y), C(0.24f, 0.22f, 0.24f), 5f,
                (x, y, c) => (x * 7 + y * 11) % 9 == 0 ? Darker(c, 0.5f) : (x * 3 + y) % 13 == 0 ? Darker(c, 1.4f) : c);
            foreach (float gx in new[] { 8f, 22f })
            {
                for (int k = -1; k <= 1; k++) cv.Piece(Tube(Curve(new Vector2(gx, 11f + Mathf.Abs(gx - 11f) * 0.2f), new Vector2(gx + k * 2f, 7f), new Vector2(gx + k * 3f, 3f), 6), 1f, 0.5f), C(0.36f, 0.66f, 0.30f), 1f);
            }

            return Done(cv);
        }

        /// <summary>Bénitier géant (rare) : deux valves ondulées entrouvertes, manteau bleu électrique tacheté.</summary>
        private static Color[,] GiantClam()
        {
            var cv = new VolumeCanvas(36, 24);
            Color shell = C(0.88f, 0.86f, 0.76f);
            cv.Piece((x, y) => Ellipse(18f, 17f, 16f, 6f)(x, y), shell, 4f, (x, y, c) => Mathf.Abs(Mathf.Sin(x * 0.55f)) < 0.25f ? Darker(c, 0.75f) : c);
            cv.Piece((x, y) => Ellipse(18f, 12f, 14f, 3.6f)(x, y), C(0.16f, 0.46f, 0.92f), 2.5f,
                (x, y, c) => (x * 5 + y * 7) % 6 == 0 ? C(0.56f, 0.92f, 0.96f) : (x * 3 + y * 2) % 11 == 0 ? C(0.10f, 0.20f, 0.46f) : c);
            cv.Piece((x, y) => Ellipse(18f, 9f, 16f, 4.5f)(x, y) && !Ellipse(18f, 12f, 14f, 3.6f)(x, y) && y < 11f, shell, 3f,
                (x, y, c) => Mathf.Abs(Mathf.Sin(x * 0.55f)) < 0.25f ? Darker(c, 0.75f) : c);
            return Done(cv);
        }

        /// <summary>Ancre rouillée (rare) : verge, jas, deux pattes en croissant, organeau, rouille et concrétions.</summary>
        private static Color[,] Anchor()
        {
            var cv = new VolumeCanvas(32, 36);
            Color iron = C(0.48f, 0.30f, 0.20f);
            Func<int, int, Color, Color> rust = (x, y, c) => (x * 5 + y * 3) % 7 == 0 ? C(0.70f, 0.40f, 0.20f) : (x + y * 4) % 11 == 0 ? C(0.80f, 0.80f, 0.74f) : c;
            cv.Piece(Union(Tube(Curve(new Vector2(4f, 22f), new Vector2(16f, 36f), new Vector2(28f, 22f), 14), 2.2f, 2.2f)), iron, 2f, rust);   // les pattes
            foreach (float s in new[] { -1f, 1f }) cv.Piece(Poly(new Vector2(16f + s * 12f, 24f), new Vector2(16f + s * 15f, 19f), new Vector2(16f + s * 9f, 22f)), iron, 1.5f, rust);
            cv.Piece(Capsule(16f, 31f, 16f, 7f, 2f), iron, 2f, rust);   // la verge
            cv.Piece(Capsule(8f, 9f, 24f, 9f, 1.4f), Darker(iron, 0.8f), 1.5f, rust);   // le jas
            cv.Piece(Ring(16f, 4f, 3.2f, 3f, 1.4f), iron, 1.2f, rust);   // l'organeau
            return Done(cv);
        }
    }
}
