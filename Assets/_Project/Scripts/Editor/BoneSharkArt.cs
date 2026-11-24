using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Légende de la Côte des Squelettes : le Spectre des épaves, grand requin squelette, mort depuis longtemps et qui nage encore parmi les
    /// épaves. De profil, tête à droite, 256×144, y depuis le haut. Corps en lambeaux : la chair grise pourrie tient
    /// encore à la tête et à la queue, mais au milieu elle s'est déchirée et laisse voir la colonne et les côtes ;
    /// nageoires réduites à leurs rayons d'os et à une membrane trouée ; gueule béante aux rangées de dents ; orbites
    /// qui luisent d'un vert spectral, lueur dans la cage des côtes ; chaîne d'ancre rouillée enroulée autour de la queue,
    /// bernacles. Trois têtes à choisir :
    /// 0 requin tigre (museau carré, lambeaux de peau rayée) ;
    /// 1 requin marteau (lame du marteau vue un peu de face, un œil qui luit à chaque bout) ;
    /// 2 requin scie (long rostre d'os bordé de dents inégales, deux barbillons), la version retenue.
    /// </summary>
    public static class BoneSharkArt
    {
        public const int Width = 256;
        public const int Height = 144;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Bone = C(0.82f, 0.78f, 0.66f);
        private static readonly Color BoneDark = C(0.46f, 0.41f, 0.32f);
        private static readonly Color Flesh = C(0.24f, 0.27f, 0.30f);
        private static readonly Color FleshDark = C(0.07f, 0.08f, 0.10f);
        private static readonly Color Belly = C(0.46f, 0.46f, 0.44f);
        private static readonly Color Cavity = C(0.03f, 0.05f, 0.05f);
        private static readonly Color Gum = C(0.30f, 0.10f, 0.10f);
        private static readonly Color Ghost = C(0.55f, 1f, 0.72f);
        private static readonly Color Rust = C(0.50f, 0.24f, 0.12f);
        private static readonly Color Ink = C(0.02f, 0.02f, 0.03f);

        private static Spine BuildSpine(int v)
        {
            var spine = new Spine();
            const int n = 60;
            float x0 = v == 2 ? 34f : 40f, length = v == 2 ? 140f : 160f;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                spine.Points.Add(new Vector2(x0 + t * length, 80f - Mathf.Sin(t * Mathf.PI) * 5f - t * 2f));
                spine.Radius.Add(4f + 19f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.5f)) - 16f * Mathf.Pow(Mathf.Clamp01((t - 0.66f) / 0.34f), 1.1f));
            }

            return spine;
        }

        private static Color Rotten(int x, int y, Color c)
        {
            float n = Noise(x / 5f + 21f, y / 5f);
            if (n < 0.3f) return Color.Lerp(c, FleshDark, 0.55f);
            if (n > 0.8f) return Color.Lerp(c, C(0.40f, 0.44f, 0.36f), 0.35f);   // moisissure verdâtre
            return Hash(x, y) < 0.06f ? Darker(c, 0.7f) : c;
        }

        private static Color BoneTex(int x, int y, Color c) => Hash(x, y) < 0.12f ? Darker(c, 0.8f) : Noise(x / 3f, y / 3f) < 0.25f ? Color.Lerp(c, BoneDark, 0.4f) : c;

        /// <summary>Nageoire en os : rayons d'os qui partent de la base, membrane trouée tendue entre eux.</summary>
        private static void BoneFin(VolumeCanvas cv, Vector2 root, Vector2[] tips, int seed)
        {
            // Base le long du corps (de la racine jusque sous le dernier rayon), rayons qui en partent.
            Vector2 base0 = root + new Vector2(5f, 0f), base1 = new Vector2(tips[tips.Length - 1].x, root.y);
            var poly = new List<Vector2> { base0 };
            poly.AddRange(tips);
            poly.Add(base1);
            cv.Piece((x, y) => Poly(poly.ToArray())(x, y) && Noise(x / 3f + seed * 5f, y / 3f) > 0.24f, C(0.22f, 0.24f, 0.24f), 1.5f,
                (x, y, c) => Rotten(x, y, c), 0.9f, 0.45f);
            foreach (Vector2 tip in tips)
            {
                Vector2 from = Vector2.Lerp(base0, base1, Mathf.InverseLerp(base0.x, base1.x, tip.x) * 0.8f);
                cv.Piece(Tube(new List<Vector2> { from, tip }, 1.1f, 0.4f), Bone, 1f, BoneTex, 0.9f, 0.55f);
            }
        }

        /// <summary>Le requin scie, la version retenue (le Spectre des épaves).</summary>
        public static Color[,] Draw() => Draw(2);

        /// <summary>Les trois têtes dessinées pour le choix : 0 tigre, 1 marteau, 2 scie.</summary>
        public static Color[,] Draw(int v)
        {
            var cv = new VolumeCanvas(Width, Height);
            Spine spine = BuildSpine(v);
            Vector2 tailBase = spine.Points[0];
            Vector2 head = spine.Points[spine.Points.Count - 1];
            var halos = new List<(Vector2 at, int r)>();

            // ---------- La queue (lobe du haut plus long), en os et membrane trouée ----------
            BoneFin(cv, tailBase + new Vector2(4f, -2f), new[] { tailBase + new Vector2(-6f, -12f), tailBase + new Vector2(-16f, -26f), tailBase + new Vector2(-28f, -40f), tailBase + new Vector2(-38f, -48f), tailBase + new Vector2(-32f, -34f), tailBase + new Vector2(-24f, -18f), tailBase + new Vector2(-16f, -4f) }, 1);
            BoneFin(cv, tailBase + new Vector2(4f, 2f), new[] { tailBase + new Vector2(-10f, 8f), tailBase + new Vector2(-20f, 18f), tailBase + new Vector2(-26f, 26f), tailBase + new Vector2(-18f, 18f), tailBase + new Vector2(-12f, 6f) }, 2);

            // ---------- Nageoires ----------
            Vector2 dorsal = spine.At(0.56f, out _) - new Vector2(0f, spine.RadiusAt(0.56f) - 4f);
            BoneFin(cv, dorsal, new[] { dorsal + new Vector2(-6f, -18f), dorsal + new Vector2(-12f, -32f), dorsal + new Vector2(-20f, -36f), dorsal + new Vector2(-22f, -22f), dorsal + new Vector2(-24f, -2f) }, 3);
            Vector2 dorsal2 = spine.At(0.24f, out _) - new Vector2(0f, spine.RadiusAt(0.24f) - 2f);
            BoneFin(cv, dorsal2, new[] { dorsal2 + new Vector2(-4f, -8f), dorsal2 + new Vector2(-10f, -10f), dorsal2 + new Vector2(-12f, -2f) }, 4);
            Vector2 anal = spine.At(0.22f, out _) + new Vector2(0f, spine.RadiusAt(0.22f) - 2f);
            BoneFin(cv, anal, new[] { anal + new Vector2(-4f, 8f), anal + new Vector2(-10f, 9f), anal + new Vector2(-12f, 2f) }, 5);

            // ---------- Le corps : cavité sombre, squelette, puis lambeaux de chair ----------
            cv.Piece(spine.Inside, Cavity, 6f, (x, y, c) => Noise(x / 6f, y / 6f) < 0.3f ? Color.Lerp(c, C(0.06f, 0.16f, 0.12f), 0.5f) : c, 0.85f, 0.2f);

            // Côtes, de la colonne au ventre, courbées vers l'arrière.
            for (int k = 0; k < 12; k++)
            {
                float t = 0.28f + k * 0.036f;
                Vector2 at = spine.At(t, out Vector2 n);
                float r = spine.RadiusAt(t);
                var rib = Curve(at - n * r * 0.15f, at + n * r * 0.5f + new Vector2(-6f, 0f), at + n * r * 0.9f + new Vector2(-9f, 0f), 10);
                cv.Piece(Tube(rib, 1.4f, 0.8f), Bone, 1f, BoneTex, 0.9f, 0.5f);
            }

            // La colonne : vertèbres en chapelet.
            for (int i = 4; i < spine.Points.Count - 6; i++)
            {
                Vector2 vert = spine.Points[i] + new Vector2(0f, -spine.Radius[i] * 0.15f);
                cv.Piece(Ellipse(vert.x, vert.y, 1.7f, i % 2 == 0 ? 2.6f : 2.1f), i % 2 == 0 ? Bone : BoneDark, 1.5f, BoneTex, 0.9f, 0.55f);
            }

            // La chair qui tient encore : la tête, la queue, et des lambeaux au milieu.
            cv.Piece((x, y) =>
            {
                if (!spine.Frame(x + 0.5f, y + 0.5f, out float t, out float s, out float r)) return false;
                if (t > 0.74f || t < 0.2f) return Noise(x / 4f + 3f, y / 4f) > 0.16f;
                if (Mathf.Abs(s) > 0.78f) return Noise(x / 3f + 8f, y / 3f) > 0.35f;   // bords déchirés
                return Noise(x / 5f + 11f, y / 5f) > 0.68f;                           // quelques lambeaux
            }, Flesh, 10f, (x, y, c) =>
            {
                spine.Frame(x + 0.5f, y + 0.5f, out float t, out float s, out float r);
                Color shade = Color.Lerp(c, Belly, Mathf.InverseLerp(0.2f, 0.9f, s) * 0.6f);
                if (v == 0 && s < 0.45f && Mathf.Repeat(t * 18f + Noise(x / 6f, y / 6f) * 0.5f + s * 0.3f, 1f) < 0.3f) shade = Color.Lerp(shade, C(0.04f, 0.05f, 0.06f), 0.85f);   // rayures du tigre
                return Rotten(x, y, shade);
            }, 0.82f, 0.4f);

            // Lueur verte au fond de la cage des côtes.
            halos.Add((spine.At(0.46f, out _) + new Vector2(0f, 4f), 16));

            // Grande pectorale en os.
            Vector2 pec = spine.At(0.66f, out _) + new Vector2(0f, spine.RadiusAt(0.66f) - 6f);
            BoneFin(cv, pec, new[] { pec + new Vector2(-6f, 14f), pec + new Vector2(-18f, 28f), pec + new Vector2(-28f, 34f), pec + new Vector2(-24f, 22f), pec + new Vector2(-14f, 6f) }, 6);

            // ---------- La tête : museau, gueule en croissant dessous, crâne mis à nu ----------
            if (v != 0)
            {
                // Museau pointu (marteau et scie) qui prolonge le corps.
                cv.Piece(Poly(head + new Vector2(-10f, -9f), head + new Vector2(14f, -2f), head + new Vector2(14f, 1f), head + new Vector2(-10f, 6f)), Flesh, 5f, (x, y, c) => Rotten(x, y, c), 0.85f, 0.45f);
            }
            else
            {
                // Museau carré du tigre, rayé lui aussi.
                cv.Piece(Poly(head + new Vector2(-10f, -10f), head + new Vector2(10f, -7f), head + new Vector2(14f, -1f), head + new Vector2(12f, 4f), head + new Vector2(-10f, 6f)), Flesh, 5f,
                    (x, y, c) => Mathf.Repeat(x * 0.25f + Noise(x / 5f, y / 5f), 1f) < 0.25f ? Color.Lerp(Rotten(x, y, c), C(0.04f, 0.05f, 0.06f), 0.8f) : Rotten(x, y, c), 0.85f, 0.45f);
            }

            Vector2 jaw = head + new Vector2(-30f, 5f);
            Vector2 mouthEnd = head + new Vector2(2f, 5f);
            // La gueule ouverte, sous le museau : un croissant sombre, la mâchoire du bas qui pend, décharnée.
            cv.Piece(Poly(jaw, mouthEnd, mouthEnd + new Vector2(-4f, 9f), jaw + new Vector2(6f, 12f)), Cavity, 2f, (x, y, c) => Noise(x / 2f, y / 2f) > 0.65f ? Gum : c);
            cv.Piece(Poly(jaw + new Vector2(2f, 10f), mouthEnd + new Vector2(-4f, 8f), mouthEnd + new Vector2(-6f, 12f), jaw + new Vector2(4f, 15f)), Bone, 2.5f, BoneTex, 0.85f, 0.5f);
            cv.Piece(Tube(new List<Vector2> { jaw + new Vector2(0f, 1f), mouthEnd + new Vector2(0f, 0.5f) }, 1.3f, 1f), Gum, 1.2f, null, 0.9f, 0.5f);
            for (int k = 0; k < 10; k++)
            {
                float fk = k / 9f;
                float len = 3f + Hash(k, 1) * 2.5f;
                // Dents triangulaires dentelées, du haut et du bas, et une rangée de remplacement derrière.
                Vector2 up = Vector2.Lerp(jaw + new Vector2(2f, 1.5f), mouthEnd + new Vector2(-1f, 1.5f), fk);
                cv.Piece(Poly(up + new Vector2(-1.3f, 0f), up + new Vector2(1.3f, 0f), up + new Vector2(0.4f, len)), Bone, 0.7f, null, 0.95f, 0.75f);
                if (k % 2 == 0) cv.Piece(Poly(up + new Vector2(-2.4f, -0.4f), up + new Vector2(-0.8f, -0.4f), up + new Vector2(-1.6f, len * 0.5f)), Darker(Bone, 0.7f), 0.5f, null, 0.95f, 0.6f);
                Vector2 low = Vector2.Lerp(jaw + new Vector2(4f, 10f), mouthEnd + new Vector2(-4f, 8f), fk);
                cv.Piece(Poly(low + new Vector2(-1.2f, 0f), low + new Vector2(1.2f, 0f), low + new Vector2(0.5f, -len * 0.8f)), Bone, 0.7f, null, 0.95f, 0.75f);
            }

            // Fentes des ouïes, déchirées jusqu'à l'os.
            for (int k = 0; k < 5; k++)
            {
                Vector2 g = head + new Vector2(-40f - k * 4f, -6f);
                cv.Piece(Tube(Curve(g, g + new Vector2(-1.5f, 7f), g + new Vector2(0f, 14f), 6), 0.9f, 0.7f), k % 2 == 0 ? Cavity : BoneDark, 0.6f, null, 0.95f, 0.5f);
            }

            Vector2 eye = head + new Vector2(-12f, -5f);
            if (v == 1)
            {
                // La lame du marteau, vue un peu de face : un œil qui luit à chaque bout.
                Vector2 a = head + new Vector2(-6f, -38f), b = head + new Vector2(2f, 26f);
                cv.Piece(Capsule(a.x, a.y, b.x, b.y, 6.5f), Flesh, 5f, (x, y, c) => Rotten(x, y, Color.Lerp(c, Belly, Mathf.InverseLerp(a.y, b.y, y) * 0.4f)), 0.85f, 0.45f);
                cv.Piece((x, y) => Capsule(a.x, a.y, b.x, b.y, 6.5f)(x, y) && Noise(x / 3f + 2f, y / 3f) < 0.38f, Bone, 2.5f, BoneTex, 0.85f, 0.5f);
                cv.Piece(Capsule(a.x + 1f, a.y + 6f, b.x - 1f, b.y - 6f, 1.2f), BoneDark, 0.8f, null, 0.9f, 0.5f);   // l'arête de la lame
                foreach (Vector2 e in new[] { a + new Vector2(2f, 3f), b + new Vector2(1f, -3f) })
                {
                    cv.Piece(Ellipse(e.x, e.y, 3.4f, 3f), Cavity, 1.5f);
                    cv.Piece(Ellipse(e.x, e.y, 1.8f, 1.6f), Ghost, 1f, null, 0.95f, 0.95f);
                    halos.Add((e, 9));
                }
            }
            else
            {
                // Le crâne mis à nu autour de l'œil : orbite vide où brûle une lueur verte.
                cv.Piece((x, y) => Ellipse(eye.x, eye.y, 9f, 7f)(x, y) && Noise(x / 3f + 6f, y / 3f) > 0.3f, Bone, 3f, BoneTex, 0.85f, 0.5f);
                cv.Piece(Ellipse(eye.x, eye.y, 4.2f, 3.6f), Cavity, 2f);
                cv.Piece(Ellipse(eye.x + 0.5f, eye.y, 2f, 1.8f), Ghost, 1f, null, 0.95f, 0.95f);
                halos.Add((eye, 10));
                if (v == 2)
                {
                    // Le rostre en scie : lame d'os plate, dents tout le long, deux barbillons.
                    Vector2 r0 = head + new Vector2(10f, -1f), r1 = head + new Vector2(80f, 2f);
                    cv.Piece(Poly(r0 + new Vector2(0f, -4.5f), r1 + new Vector2(0f, -1.5f), r1 + new Vector2(0f, 1.5f), r0 + new Vector2(0f, 4.5f)), Bone, 3f, BoneTex, 0.85f, 0.5f);
                    cv.Piece(Tube(new List<Vector2> { r0, r1 }, 0.6f, 0.4f), BoneDark, 0.5f);
                    for (int k = 0; k < 12; k++)
                    {
                        Vector2 at = Vector2.Lerp(r0, r1, 0.06f + k * 0.08f);
                        float half = Mathf.Lerp(4.5f, 1.5f, (at.x - r0.x) / (r1.x - r0.x));
                        cv.Piece(Poly(at + new Vector2(-1.2f, -half), at + new Vector2(1.2f, -half), at + new Vector2(0.6f, -half - (k % 2 == 0 ? 4.5f : 2.5f))), Bone, 0.8f, null, 0.95f, 0.7f);
                        cv.Piece(Poly(at + new Vector2(-1.2f, half), at + new Vector2(1.2f, half), at + new Vector2(0.6f, half + (k % 2 == 1 ? 4.5f : 2.5f))), Bone, 0.8f, null, 0.95f, 0.7f);
                    }

                    // Deux longs barbillons qui pendent sous la scie, comme chez les vrais requins scies.
                    foreach (float bx in new[] { 0.42f, 0.5f })
                    {
                        Vector2 barb = Vector2.Lerp(r0, r1, bx) + new Vector2(0f, 3f);
                        cv.Piece(Tube(Curve(barb, barb + new Vector2(-2f, 8f), barb + new Vector2(-6f + bx * 6f, 15f), 8), 0.8f, 0.4f), Flesh, 0.6f, null, 0.9f, 0.5f);
                    }
                }
            }

            // ---------- La chaîne d'ancre rouillée autour de la queue ----------
            for (int k = 0; k < 9; k++)
            {
                float t = 0.08f + k * 0.022f;
                Vector2 at = spine.At(t, out Vector2 n) + n * Mathf.Lerp(-1f, 1f, k / 8f) * spine.RadiusAt(t) * 1.1f;
                bool flat = k % 2 == 0;
                cv.Piece(Ring(at.x, at.y, flat ? 3.2f : 1.6f, flat ? 2f : 3f, 1f), Rust, 1.2f, (x, y, c) => Hash(x, y) < 0.3f ? Darker(c, 0.7f) : c, 0.9f, 0.5f);
            }

            Vector2 hang = spine.At(0.18f, out _) + new Vector2(0f, spine.RadiusAt(0.18f));
            for (int k = 0; k < 6; k++)
            {
                Vector2 at = hang + new Vector2(-k * 2.2f, k * 4.2f);
                bool flat = k % 2 == 0;
                cv.Piece(Ring(at.x, at.y, flat ? 2f : 1.4f, flat ? 3f : 2.4f, 0.9f), Rust, 1.2f, (x, y, c) => Hash(x, y) < 0.3f ? Darker(c, 0.7f) : c, 0.9f, 0.5f);
            }

            // Bernacles sur la tête et le dos.
            for (int k = 0; k < 7; k++)
            {
                float t = 0.72f + Hash(k, 3) * 0.22f;
                Vector2 at = spine.At(t, out Vector2 n) - n * spine.RadiusAt(t) * (0.3f + Hash(k, 5) * 0.5f);
                float r = 1.4f + Hash(k, 7);
                cv.Piece(Ellipse(at.x, at.y, r, r * 0.85f), C(0.58f, 0.58f, 0.52f), r, (x, y, c) => Hash(x, y) < 0.3f ? Darker(c, 0.7f) : c, 0.9f, 0.45f);
                cv.Piece(Ellipse(at.x, at.y - r * 0.2f, r * 0.35f, r * 0.3f), Ink, 0.4f);
            }

            Color[,] p = cv.Finish(Ink, 0.55f);
            foreach ((Vector2 at, int r) in halos)
            {
                Halo(p, Mathf.RoundToInt(at.x), Height - 1 - Mathf.RoundToInt(at.y), r, Ghost, 3f);
            }

            return p;
        }
    }
}
