using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les 8 bêtes étranges de la grotte de Chiyo (baie d'Ha Long), en volume, d'après les vraies espèces des grands fonds
    /// et des sources hydrothermales. Dessins originaux, vus de dessus ou de profil comme les autres bestioles du carnet.
    /// </summary>
    public static class CaveNetArt
    {
        private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        private static float Hash(int x, int y, int seed = 0)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
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

        /// <summary>Un trait qui suit des points, épaisseur de <paramref name="r0"/> à <paramref name="r1"/>.</summary>
        private static Func<float, float, bool> Path(Vector2[] points, float r0, float r1)
        {
            var parts = new Func<float, float, bool>[points.Length - 1];
            for (int i = 0; i < parts.Length; i++)
            {
                float t0 = i / (float)parts.Length, t1 = (i + 1) / (float)parts.Length;
                parts[i] = Taper(points[i], points[i + 1], Mathf.Lerp(r0, r1, t0), Mathf.Lerp(r0, r1, t1));
            }

            return Union(parts);
        }

        /// <summary>Dans l'ordre où elles apparaissent dans la flaque de la grotte (la méduse immortelle en dernier).</summary>
        public static readonly string[] Ids =
        {
            "bathynomus-giganteus", "costasiella-kuroshimae", "eunice-aphroditois", "kiwa-hirsuta", "chrysomallon-squamiferum",
            "colossendeis-megalonyx", "enypniastes-eximia", "turritopsis-dohrnii",
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
                case "bathynomus-giganteus": return GiantIsopod();
                case "costasiella-kuroshimae": return SeaSheep();
                case "eunice-aphroditois": return BobbitWorm();
                case "kiwa-hirsuta": return YetiCrab();
                case "chrysomallon-squamiferum": return ScalyFoot();
                case "colossendeis-megalonyx": return SeaSpider();
                case "enypniastes-eximia": return HeadlessChicken();
                case "turritopsis-dohrnii": return ImmortalJellyfish();
                default: return null;
            }
        }

        /// <summary>
        /// Isopode géant, vu de dessus : grand ovale en plaques qui se chevauchent, gris lilas pâle, éventail de la queue
        /// en bas, tête large aux gros yeux en grappe, deux antennes, pattes qui dépassent sur les côtés.
        /// </summary>
        private static Color[,] GiantIsopod()
        {
            var v = new VolumeCanvas(88, 112);
            Color shell = C(196, 186, 200), dark = C(130, 118, 140), leg = C(170, 156, 170);
            for (int i = 0; i < 7; i++)
            {
                float y = 36f + i * 8f;
                foreach (float s in new[] { -1f, 1f })
                {
                    v.Piece(Taper(new Vector2(44f + s * 22f, y), new Vector2(44f + s * (32f + (i % 2) * 2f), y + 5f + (i - 3) * 1.2f), 2.2f, 1.2f), leg, 2f, null, 0.8f);
                }
            }

            v.Piece(Path(new[] { new Vector2(36f, 22f), new Vector2(28f, 12f), new Vector2(18f, 6f) }, 1.6f, 0.9f), leg, 1.5f);
            v.Piece(Path(new[] { new Vector2(52f, 22f), new Vector2(60f, 12f), new Vector2(70f, 6f) }, 1.6f, 0.9f), leg, 1.5f);
            // Le corps : ovale un peu plus large en bas, plaques en bandes, éventail de la queue.
            v.Piece((x, y) =>
            {
                // Ovale du corps, puis l'éventail de la queue : un demi-disque arrondi au bord festonné.
                if (y >= 86f)
                {
                    float dx = (x - 44f) / 21f, dy = (y - 86f) / 16f;
                    float scallop = 0.06f * Mathf.Abs(Mathf.Sin(Mathf.Atan2(dy, dx) * 5f));
                    return dx * dx + dy * dy < 1f - scallop;
                }

                if (y < 18f) return false;
                float t = (y - 18f) / 74f;
                return Mathf.Abs(x - 44f) < 25f * Mathf.Sqrt(Mathf.Sin(Mathf.Clamp(t, 0.02f, 0.62f) / 0.62f * Mathf.PI * 0.5f));
            }, shell, 12f, (x, y, c) =>
            {
                int band = (y - 30) / 8;
                if (y > 30 && y < 90 && (y - 30) % 8 == 0) return Darker(c, 0.72f);                         // le bord des plaques
                if (y > 30 && y < 90 && (y - 30) % 8 == 1) return Darker(c, 1.12f);
                if (y >= 90 && (x - 44) % 6 == 0) return Darker(c, 0.85f);                                 // les rayons de l'éventail
                return Hash(x, y, 3) > 0.9f ? Darker(c, 0.9f) : band % 2 == 0 ? c : Darker(c, 0.97f);
            });
            v.Piece(Ellipse(44f, 24f, 18f, 8f), C(186, 174, 192), 8f, (x, y, c) => Hash(x, y, 4) > 0.86f ? Darker(c, 0.9f) : c);
            foreach (float ex in new[] { 31f, 57f })
            {
                v.Piece(Ellipse(ex, 24f, 4.5f, 3.6f), C(40, 36, 44), 2.5f, (x, y, c) => (x + y) % 2 == 0 ? C(70, 66, 78) : c);   // yeux en grappe
            }

            return v.Finish(dark, 0.5f);
        }

        /// <summary>
        /// Mouton des mers : minuscule limace vue de dessus, tête blanche au museau rond, deux yeux noirs et deux cornes à pointe
        /// noire, dos couvert de grosses feuilles vertes gonflées à pointe jaune (les « laines »).
        /// </summary>
        private static Color[,] SeaSheep()
        {
            var v = new VolumeCanvas(72, 72);
            Color white = C(246, 244, 236), green = C(110, 190, 84), tip = C(238, 220, 90);
            v.Piece(Union(Ellipse(36f, 44f, 15f, 20f), Ellipse(36f, 22f, 11f, 10f)), C(226, 232, 214), 7f);
            // Les « laines » : feuilles en rangs serrés, plus grosses au milieu.
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                float r = 12f + (i % 2) * 2f;
                float fx = 36f + Mathf.Cos(a) * r * 0.9f, fy = 46f + Mathf.Sin(a) * r * 1.1f;
                v.Piece(Ellipse(fx, fy, 6f, 7.5f), green, 6f, (x, y, c) =>
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(fx + Mathf.Cos(a) * 3f, fy + Mathf.Sin(a) * 3.5f));
                    return d < 2.6f ? tip : Hash(x, y, 9) > 0.9f ? Darker(c, 1.12f) : c;
                });
            }

            v.Piece(Ellipse(36f, 46f, 7f, 9f), green, 7f, (x, y, c) => Hash(x, y, 8) > 0.85f ? tip : c);
            // La tête : museau blanc, deux cornes, deux petits yeux noirs.
            v.Piece(Ellipse(36f, 22f, 9f, 8f), white, 6f);
            foreach (float s in new[] { -1f, 1f })
            {
                v.Piece(Taper(new Vector2(36f + s * 5f, 18f), new Vector2(36f + s * 11f, 6f), 2.4f, 1.4f), white, 2f,
                    (x, y, c) => y < 10 ? C(40, 36, 34) : c);
                v.Piece(Ellipse(36f + s * 4f, 21f, 1.8f, 2.2f), C(16, 14, 14), 1f);
            }

            return v.Finish(C(52, 90, 40), 0.5f);
        }

        /// <summary>
        /// Ver de Bobbit : long ver en S, anneaux bronze aux reflets violets irisés, tête à droite avec cinq antennes
        /// rayées et deux mâchoires en pince grandes ouvertes.
        /// </summary>
        private static Color[,] BobbitWorm()
        {
            var v = new VolumeCanvas(140, 64);
            var pts = new Vector2[18];
            for (int i = 0; i < pts.Length; i++)
            {
                float t = i / (float)(pts.Length - 1);
                pts[i] = new Vector2(8f + t * 108f, 34f + Mathf.Sin(t * Mathf.PI * 2.2f) * 12f * (1f - t * 0.4f));
            }

            Color bronze = C(150, 104, 70), violet = C(150, 92, 170), green = C(96, 160, 140);
            v.Piece(Path(pts, 3f, 7f), bronze, 6f, (x, y, c) =>
            {
                if (x % 5 == 0) return Darker(c, 0.74f);                                             // les anneaux
                float shimmer = Mathf.Sin(x * 0.11f + y * 0.3f);
                return shimmer > 0.55f ? Color.Lerp(c, violet, 0.6f) : shimmer < -0.7f ? Color.Lerp(c, green, 0.45f) : c;
            });
            Vector2 head = pts[pts.Length - 1];
            v.Piece(Ellipse(head.x + 4f, head.y, 7f, 7.5f), C(122, 80, 60), 6f);
            // Les mâchoires en pince, ouvertes vers la droite.
            foreach (float s in new[] { -1f, 1f })
            {
                v.Piece(Path(new[] { new Vector2(head.x + 9f, head.y + s * 3f), new Vector2(head.x + 17f, head.y + s * 8f), new Vector2(head.x + 21f, head.y + s * 3f) }, 2f, 1f),
                    C(60, 40, 34), 2f);
            }

            // Cinq antennes rayées dressées.
            for (int k = 0; k < 5; k++)
            {
                float a = -1.25f + k * 0.38f;
                var a0 = new Vector2(head.x + 2f, head.y - 4f);
                var a1 = a0 + new Vector2(Mathf.Sin(a) * 6f, -11f - (k % 2) * 3f);
                v.Piece(Taper(a0, a1, 1.4f, 0.8f), C(236, 226, 200), 1.5f, (x, y, c) => (y % 3 == 0) ? C(120, 70, 60) : c);
            }

            return v.Finish(C(52, 32, 28), 0.5f);
        }

        /// <summary>
        /// Crabe yéti, vu de dessus : petite carapace blanc crème ovale, longues pinces tendues vers l'avant couvertes de
        /// longues soies blondes (comme des poils), pattes fines, pas d'yeux.
        /// </summary>
        private static Color[,] YetiCrab()
        {
            var v = new VolumeCanvas(112, 96);
            Color white = C(236, 230, 214), hair = C(230, 206, 150), hairDark = C(196, 166, 104);
            for (int i = 0; i < 4; i++)
            {
                foreach (float s in new[] { -1f, 1f })
                {
                    float y = 60f + i * 6f;
                    v.Piece(Path(new[] { new Vector2(56f + s * 12f, y), new Vector2(56f + s * 26f, y + 2f + i), new Vector2(56f + s * 32f, y + 12f + i * 2f) }, 2.2f, 1.2f),
                        white, 2f, (x, yy, c) => Hash(x, yy, 6) > 0.8f ? hair : c);
                }
            }

            // Les grandes pinces, bras velus vers l'avant (le haut).
            foreach (float s in new[] { -1f, 1f })
            {
                var arm = new[] { new Vector2(56f + s * 10f, 56f), new Vector2(56f + s * 26f, 40f), new Vector2(56f + s * 30f, 22f) };
                v.Piece(Path(arm, 4.5f, 3.5f), white, 4f);
                v.Piece(Union(Ellipse(56f + s * 30f, 16f, 6f, 9f), Taper(new Vector2(56f + s * 27f, 9f), new Vector2(56f + s * 24f, 2f), 2f, 1f),
                    Taper(new Vector2(56f + s * 33f, 9f), new Vector2(56f + s * 35f, 3f), 2f, 1f)), white, 5f);
                // Les soies : de longs poils blonds tout autour des bras et des pinces.
                for (int k = 0; k < 44; k++)
                {
                    float t = k / 43f;
                    Vector2 at = t < 0.5f ? Vector2.Lerp(arm[0], arm[1], t * 2f) : Vector2.Lerp(arm[1], new Vector2(56f + s * 30f, 14f), (t - 0.5f) * 2f);
                    // Poils couchés vers l'extérieur et un peu vers le bas, en mèches qui ondulent.
                    float a = Mathf.Atan2(0.6f, s) + (Hash(k, (int)(s * 3f) + 4, 21) - 0.5f) * 1.6f;
                    float len = 8f + Hash(k, 5, 22) * 6f;
                    Vector2 tip = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * len;
                    Vector2 mid = Vector2.Lerp(at, tip, 0.5f) + new Vector2(0f, 1.5f);
                    v.Piece(Path(new[] { at, mid, tip }, 1.4f, 0.6f), k % 3 == 0 ? hairDark : hair, 1f, null, 0.6f);
                }
            }

            v.Piece(Ellipse(56f, 62f, 15f, 13f), white, 9f, (x, y, c) => Hash(x, y, 7) > 0.88f ? Darker(c, 0.9f) : c);
            return v.Finish(C(120, 100, 72), 0.5f);
        }

        /// <summary>
        /// Escargot à pied écailleux : coquille noire et métallique en spirale (fer), reflets bleutés, et pied couvert de
        /// petites écailles noires qui se chevauchent comme une armure.
        /// </summary>
        private static Color[,] ScalyFoot()
        {
            var v = new VolumeCanvas(84, 72);
            Color iron = C(46, 46, 52), steel = C(110, 116, 132), rust = C(132, 82, 52);
            // Le pied en écailles, sous la coquille.
            v.Piece(Ellipse(42f, 50f, 30f, 14f), iron, 6f, (x, y, c) =>
            {
                // Écailles rondes qui se chevauchent comme des tuiles : chacune claire en haut, son bord bas dans l'ombre.
                int row = y / 4;
                float cx = (Mathf.Floor((x + (row % 2) * 3.5f) / 7f) + 0.5f) * 7f - (row % 2) * 3.5f;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, row * 4f + 1f));
                if (d > 3.9f) return Darker(c, 0.62f);
                return d < 1.6f && y % 4 < 2 ? Color.Lerp(c, steel, 0.55f) : Hash((int)cx, row, 5) > 0.8f ? Color.Lerp(c, steel, 0.25f) : c;
            });
            // La coquille en spirale (tours qui s'enroulent vers le sommet en haut à gauche).
            v.Piece(Ellipse(40f, 32f, 22f, 20f), iron, 12f, (x, y, c) =>
            {
                float dx = x - 36f, dy = y - 28f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Atan2(dy, dx);
                float spiral = Mathf.Repeat(r - a * 2.4f, 7f);
                if (spiral < 1.2f) return Darker(c, 0.55f);                                              // les sutures
                if (Hash(x, y, 9) > 0.93f) return rust;                                                 // un peu de rouille
                return spiral > 4.5f ? Color.Lerp(c, steel, 0.55f) : c;                                 // l'éclat du métal
            });
            v.Piece(Ellipse(30f, 22f, 4f, 3f), C(170, 178, 196), 2f);   // le reflet
            return v.Finish(C(14, 14, 18), 0.6f);
        }

        /// <summary>
        /// Pycnogonide géant (araignée de mer) : corps minuscule et fin au centre, huit pattes immenses très fines aux
        /// articulations marquées, rouge orangé, une petite trompe devant.
        /// </summary>
        private static Color[,] SeaSpider()
        {
            var v = new VolumeCanvas(120, 120);
            Color orange = C(214, 96, 52), joint = C(244, 170, 110);
            for (int k = 0; k < 8; k++)
            {
                float side = k < 4 ? -1f : 1f;
                int i = k % 4;
                var hip = new Vector2(60f + side * 3f, 46f + i * 9f);
                float spread = -0.9f + i * 0.6f;
                var knee = hip + new Vector2(side * 26f, -14f + spread * 12f);
                var ankle = knee + new Vector2(side * 18f, 16f + spread * 14f);
                var toe = ankle + new Vector2(side * 8f, 14f + i * 2f);
                v.Piece(Path(new[] { hip, knee, ankle, toe }, 2.4f, 1.1f), orange, 2f, null, 0.7f);
                v.Piece(Ellipse(knee.x, knee.y, 2.6f, 2.6f), joint, 2f);
                v.Piece(Ellipse(ankle.x, ankle.y, 2f, 2f), joint, 1.5f);
            }

            v.Piece(Union(Capsule(60f, 40f, 60f, 82f, 4f), Taper(new Vector2(60f, 40f), new Vector2(60f, 26f), 3f, 1.6f)), orange, 4f,
                (x, y, c) => y % 9 == 0 ? Darker(c, 0.8f) : c);
            return v.Finish(C(110, 36, 20), 0.5f);
        }

        /// <summary>
        /// Concombre de mer « poulet sans tête », de profil, nageant vers la droite : corps rose translucide en sac, voile
        /// frangé à l'avant qui lui fait une tête coupée, intestin rouge sombre qu'on voit au travers, petites papilles.
        /// </summary>
        private static Color[,] HeadlessChicken()
        {
            var v = new VolumeCanvas(104, 80);
            Color pink = C(244, 150, 170), veil = C(250, 186, 196), gut = C(150, 30, 50);
            v.Piece((x, y) =>
            {
                if (x < 10f || x > 74f) return false;
                float t = (x - 10f) / 64f;
                float top = 40f - 22f * Mathf.Sin(t * Mathf.PI * 0.95f), bottom = 52f + 8f * Mathf.Sin(t * Mathf.PI);
                return y > top && y < bottom;
            }, pink, 10f, (x, y, c) => Hash(x, y, 12) > 0.93f ? Color.Lerp(c, Color.white, 0.5f) : c, 0.6f, 0.6f);
            v.Piece(Path(new[] { new Vector2(18f, 46f), new Vector2(34f, 38f), new Vector2(50f, 44f), new Vector2(66f, 36f) }, 2.4f, 2f), gut, 2f, null, 0.5f);
            // Le voile frangé, comme un col ouvert vers l'avant (la « tête coupée »).
            v.Piece((x, y) =>
            {
                if (x < 68f || x > 98f) return false;
                float t = (x - 68f) / 30f;
                float spread = 10f + t * 24f;
                float fringe = Mathf.Abs(Mathf.Sin(y * 0.7f)) * 4f * t;
                return Mathf.Abs(y - 38f) < spread - fringe && x < 98f - Mathf.Abs(y - 38f) * 0.3f;
            }, veil, 5f, (x, y, c) =>
            {
                // Plis en éventail qui partent de la base du voile, bord frangé plus pâle.
                float a = Mathf.Atan2(y - 38f, x - 66f);
                return Mathf.Abs(Mathf.Sin(a * 9f)) < 0.18f ? Darker(c, 0.88f) : x > 90 ? Color.Lerp(c, Color.white, 0.3f) : c;
            }, 0.5f, 0.6f);
            for (int i = 0; i < 6; i++) v.Piece(Taper(new Vector2(18f + i * 9f, 56f), new Vector2(16f + i * 9f, 64f), 1.6f, 0.8f), pink, 1.5f);   // les papilles du ventre
            return v.Finish(C(150, 70, 90), 0.45f);
        }

        /// <summary>
        /// Méduse immortelle : minuscule cloche transparente vue de profil, estomac rouge vif au centre, liseré blanc,
        /// une couronne de filaments fins qui pendent et ondulent.
        /// </summary>
        private static Color[,] ImmortalJellyfish()
        {
            var v = new VolumeCanvas(64, 80);
            Color bell = C(206, 230, 240), red = C(222, 40, 46);
            for (int i = 0; i < 12; i++)
            {
                float x0 = 14f + i * 3.2f;
                var pts = new Vector2[5];
                for (int k = 0; k < 5; k++) pts[k] = new Vector2(x0 + Mathf.Sin(k * 1.2f + i) * 2.5f, 40f + k * 8f);
                v.Piece(Path(pts, 0.9f, 0.6f), C(240, 236, 228), 1f, null, 0.4f);
            }

            // La cloche en dôme, un peu plus haute que large, bord inférieur légèrement replié vers l'intérieur.
            v.Piece((x, y) =>
            {
                float dx = (x - 32f) / 20f, dy = (y - 40f) / 27f;
                return y < 42f && dx * dx + dy * dy < 1f && !(y > 38f && Mathf.Abs(dx) > 0.92f);
            }, bell, 9f,
                (x, y, c) => y > 39 ? Color.white : Hash(x, y, 14) > 0.92f ? Color.Lerp(c, Color.white, 0.6f) : c, 0.5f, 0.65f);
            v.Piece(Union(Ellipse(32f, 30f, 8f, 9f), Capsule(32f, 34f, 32f, 44f, 3f)), red, 6f, (x, y, c) => Hash(x, y, 15) > 0.8f ? C(255, 120, 100) : c);
            return v.Finish(C(110, 140, 160), 0.4f);
        }
    }
}
