using System;
using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les échinodermes du carnet de l'épuisette, vus de dessus, en volume, d'après les vraies espèces : étoiles de mer
    /// (bras effilés un peu courbés, tous un peu différents), ophiure (petit disque, bras fins et ondulants), oursins
    /// (épines qui rayonnent dans tous les sens, celles du dessus vues en raccourci), concombre de mer. Dessins originaux.
    /// </summary>
    public static class EchinoArt
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
            "asterias-rubens", "linckia-laevigata", "protoreaster-linckii", "ophiolepis-superba",
            "diadema-setosum", "heterocentrotus-mamillatus", "mesocentrotus-franciscanus", "parechinus-angulosus", "holothuria-atra", "strongylocentrotus-droebachiensis", "apostichopus-japonicus",
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
                case "asterias-rubens": return CommonStar();
                case "linckia-laevigata": return BlueStar();
                case "protoreaster-linckii": return KnobStar();
                case "ophiolepis-superba": return BrittleStar();
                case "diadema-setosum": return DiademaUrchin();
                case "heterocentrotus-mamillatus": return PencilUrchin();
                case "mesocentrotus-franciscanus": return RedUrchin();
                case "parechinus-angulosus": return CapeUrchin();
                case "holothuria-atra": return SeaCucumber();
                case "strongylocentrotus-droebachiensis": return GreenUrchin();
                case "apostichopus-japonicus": return JapaneseCucumber();
                default: return null;
            }
        }

        // ---------- Étoiles de mer ----------

        /// <summary>
        /// Une étoile : cinq bras qui partent du disque, chacun avec sa longueur et sa courbure (un peu au hasard, comme
        /// une vraie), effilés de <paramref name="baseR"/> à <paramref name="tipR"/>. Renvoie la forme et, pour la texture,
        /// la position le long du bras (0 au centre, 1 au bout) et l'écart à l'arête du bras.
        /// </summary>
        private static Func<float, float, bool> StarShape(Vector2 c, float armLen, float baseR, float tipR, float disc, int seed,
            out Func<float, float, Vector2> along)
        {
            var rnd = new System.Random(seed);
            var arms = new List<Vector2[]>();
            for (int i = 0; i < 5; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI * 2f / 5f + (float)(rnd.NextDouble() - 0.5) * 0.18f;
                float len = armLen * (0.86f + (float)rnd.NextDouble() * 0.2f);
                float bend = (float)(rnd.NextDouble() - 0.5) * 0.6f;   // courbure du bras
                var pts = new Vector2[8];
                for (int k = 0; k < pts.Length; k++)
                {
                    float t = k / (float)(pts.Length - 1);
                    float ang = a + bend * t * t;
                    pts[k] = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * len * t;
                }

                arms.Add(pts);
            }

            // Distance à un bras : (t le long du bras, écart à l'arête).
            Vector2 Nearest(float x, float y)
            {
                var p = new Vector2(x, y);
                float best = float.MaxValue, bestT = 0f;
                foreach (Vector2[] pts in arms)
                {
                    for (int k = 0; k < pts.Length - 1; k++)
                    {
                        Vector2 ab = pts[k + 1] - pts[k];
                        float u = Mathf.Clamp01(Vector2.Dot(p - pts[k], ab) / Mathf.Max(0.001f, ab.sqrMagnitude));
                        float d = (p - (pts[k] + ab * u)).magnitude;
                        float t = (k + u) / (pts.Length - 1);
                        float r = Mathf.Lerp(baseR, tipR, Mathf.Pow(t, 0.8f));
                        float rel = d / r;
                        if (rel < best) { best = rel; bestT = t; }
                    }
                }

                return new Vector2(bestT, best);
            }

            along = (x, y) => Nearest(x, y);
            return (x, y) => Nearest(x, y).y <= 1f || Vector2.Distance(new Vector2(x, y), c) <= disc;
        }

        /// <summary>Étoile de mer commune : orange, rangées de petites épines blanches le long des bras, bouts relevés plus clairs.</summary>
        private static Color[,] CommonStar()
        {
            var v = new VolumeCanvas(100, 100);
            var c = new Vector2(50f, 52f);
            Func<float, float, bool> shape = StarShape(c, 46f, 8f, 3f, 10f, 3, out Func<float, float, Vector2> along);
            v.Piece(shape, C(222, 112, 48), 7f, (x, y, col) =>
            {
                Vector2 a = along(x, y);
                // Une rangée d'épines blanches le long de l'arête, et des petites épines éparses.
                if (a.y < 0.18f && (x + y) % 3 == 0) return C(250, 226, 196);
                if (Hash(x, y, 2) > 0.9f) return C(246, 200, 150);
                Color baseCol = Color.Lerp(C(196, 86, 40), C(236, 136, 60), Mathf.PerlinNoise(x * 0.12f, y * 0.12f));
                return a.x > 0.85f ? Color.Lerp(baseCol, C(246, 190, 120), 0.5f) : baseCol;
            });
            v.Piece(Ellipse(c.x + 4f, c.y - 3f, 1.6f, 1.6f), C(250, 220, 150), 1f);   // la plaque madréporique
            return v.Finish(C(90, 40, 18), 0.55f);
        }

        /// <summary>Étoile de mer bleue : longs bras ronds et presque égaux, bleu vif, petit disque, bouts un peu plus clairs.</summary>
        private static Color[,] BlueStar()
        {
            var v = new VolumeCanvas(112, 112);
            var c = new Vector2(56f, 58f);
            Func<float, float, bool> shape = StarShape(c, 52f, 6.5f, 4.5f, 7f, 11, out Func<float, float, Vector2> along);
            v.Piece(shape, C(56, 112, 214), 6f, (x, y, col) =>
            {
                Vector2 a = along(x, y);
                Color baseCol = Color.Lerp(C(40, 92, 196), C(80, 140, 230), Mathf.PerlinNoise(x * 0.1f, y * 0.1f));
                if (Hash(x, y, 4) > 0.93f) return C(30, 70, 160);   // pores
                return a.x > 0.9f ? Color.Lerp(baseCol, C(150, 190, 240), 0.4f) : baseCol;
            });
            return v.Finish(C(18, 36, 90), 0.55f);
        }

        /// <summary>Étoile à bosses rouges : bras épais, dos gris-crème parcouru d'un réseau rouge et hérissé de bosses rouges.</summary>
        private static Color[,] KnobStar()
        {
            var v = new VolumeCanvas(104, 104);
            var c = new Vector2(52f, 54f);
            Func<float, float, bool> shape = StarShape(c, 44f, 12f, 4.5f, 14f, 7, out Func<float, float, Vector2> along);
            v.Piece(shape, C(196, 180, 168), 9f, (x, y, col) =>
            {
                // Le réseau de crêtes rouges.
                float net = Mathf.Abs(Mathf.PerlinNoise(x * 0.16f, y * 0.16f) - 0.5f);
                return net < 0.05f ? C(190, 60, 50) : Hash(x, y, 5) > 0.9f ? C(170, 150, 140) : col;
            });
            // Les bosses rouges, en rangées le long de chaque bras et sur le disque.
            for (int x = 0; x < 104; x += 6)
            {
                for (int y = 0; y < 104; y += 6)
                {
                    float jx = x + Hash(x, y, 6) * 3f, jy = y + Hash(y, x, 7) * 3f;
                    if (!shape(jx, jy)) continue;
                    Vector2 a = along(jx, jy);
                    if (a.y > 0.65f && Vector2.Distance(new Vector2(jx, jy), c) > 14f) continue;
                    float r = Mathf.Lerp(3.2f, 1.6f, a.x);
                    v.Piece(Ellipse(jx, jy, r, r), C(214, 54, 42), r, null, 0.85f);
                }
            }

            return v.Finish(C(80, 40, 36), 0.55f);
        }

        /// <summary>Ophiure à annelures pourpres : petit disque rond au motif en étoile, cinq longs bras fins et ondulants, annelés de pourpre.</summary>
        private static Color[,] BrittleStar()
        {
            var v = new VolumeCanvas(120, 120);
            var c = new Vector2(60f, 60f);
            Color cream = C(214, 196, 150), purple = C(110, 56, 100);
            for (int i = 0; i < 5; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI * 2f / 5f + 0.2f;
                float wiggle = (i % 2 == 0 ? 1f : -1f) * 0.5f;
                var pts = new Vector2[14];
                for (int k = 0; k < pts.Length; k++)
                {
                    float t = k / (float)(pts.Length - 1);
                    float ang = a + Mathf.Sin(t * 5f) * wiggle * t;
                    pts[k] = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (9f + 48f * t);
                }

                for (int k = 0; k < pts.Length - 1; k++)
                {
                    float r = Mathf.Lerp(2.6f, 1f, k / (float)(pts.Length - 2));
                    int ring = k;
                    v.Piece(Capsule(pts[k].x, pts[k].y, pts[k + 1].x, pts[k + 1].y, r), cream, r, (x, y, col) => ring % 2 == 0 ? purple : col, 0.85f);
                }
            }

            // Le disque, avec son motif en étoile plus sombre et ses petites plaques.
            v.Piece(Ellipse(c.x, c.y, 11f, 11f), cream, 5f, (x, y, col) =>
            {
                float a = Mathf.Atan2(y - c.y, x - c.x), d = Vector2.Distance(new Vector2(x, y), c);
                if (Mathf.Cos(a * 5f + 1f) > 0.6f && d < 9f) return purple;
                return Hash(x / 2, y / 2, 8) > 0.7f ? Darker(col, 0.88f) : col;
            });
            return v.Finish(C(60, 36, 40), 0.55f);
        }

        // ---------- Oursins ----------

        /// <summary>
        /// Un oursin : d'abord les épines qui partent vers l'extérieur (sous le corps), puis le corps, puis les épines du
        /// dessus, vues en raccourci. Les directions sont tirées au hasard, comme sur un vrai oursin.
        /// </summary>
        private static void Urchin(VolumeCanvas v, Vector2 c, float body, int count, float minLen, float maxLen, float spineR, float tipR,
            Color bodyColor, Func<int, int, Color, Color> bodyTex, Func<int, int, Color, Color> spineTex, Color spine, int seed, float maxUp = 1f)
        {
            var rnd = new System.Random(seed);
            var spines = new List<(Vector2 from, Vector2 to, float r, float up)>();
            for (int i = 0; i < count; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float up = (float)rnd.NextDouble() * maxUp;         // 0 = à plat vers l'extérieur, 1 = droit vers nous
                float len = Mathf.Lerp(minLen, maxLen, (float)rnd.NextDouble()) * Mathf.Cos(up * Mathf.PI * 0.45f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 from = c + dir * body * Mathf.Lerp(0.95f, 0.2f, up);
                spines.Add((from, from + dir * len, spineR * (1f - up * 0.3f), up));
            }

            spines.Sort((p, q) => p.up.CompareTo(q.up));
            foreach (var s in spines)
            {
                if (s.up > 0.55f) continue;
                v.Piece(Taper(s.from, s.to, s.r, tipR), spine, Mathf.Max(1f, s.r), spineTex, 0.8f);
            }

            v.Piece(Ellipse(c.x, c.y, body, body), bodyColor, body * 0.8f, bodyTex, 0.6f);
            foreach (var s in spines)
            {
                if (s.up <= 0.55f) continue;
                v.Piece(Taper(s.from, s.to, s.r, tipR), spine, Mathf.Max(1f, s.r), spineTex, 0.8f);
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

        /// <summary>Oursin diadème : noir, très longues épines fines, l'anneau orange au sommet et cinq petites taches blanches.</summary>
        private static Color[,] DiademaUrchin()
        {
            var v = new VolumeCanvas(140, 140);
            var c = new Vector2(70f, 70f);
            Urchin(v, c, 17f, 90, 30f, 64f, 1.4f, 0.4f, C(30, 28, 34), null, (x, y, col) => Hash(x, y, 3) > 0.9f ? C(70, 70, 86) : col,
                C(26, 24, 30), 21);
            // L'anneau orange et les cinq taches blanches, au sommet du corps.
            v.Piece((x, y) => { float d = Vector2.Distance(new Vector2(x, y), c); return d > 3f && d < 6f; }, C(240, 120, 40), 2f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f;
                v.Piece(Ellipse(c.x + Mathf.Cos(a) * 11f, c.y + Mathf.Sin(a) * 11f, 1.5f, 1.5f), C(240, 240, 246), 1f);
            }

            return v.Finish(C(10, 10, 14), 0.4f);
        }

        /// <summary>Oursin crayon : grosses épines en crayon rouge brique, inégales, rayées de clair, bout pâle ; petites épines au sommet.</summary>
        private static Color[,] PencilUrchin()
        {
            var v = new VolumeCanvas(120, 120);
            var c = new Vector2(60f, 60f);
            Urchin(v, c, 21f, 17, 24f, 36f, 4.4f, 3.4f, C(130, 60, 50), (x, y, col) => Hash(x, y, 4) > 0.75f ? C(190, 110, 90) : col,
                (x, y, col) => (x + y) % 9 < 2 ? C(220, 160, 130) : Hash(x, y, 5) > 0.92f ? Darker(col, 0.8f) : col, C(176, 62, 44), 33, 0.3f);
            // Les bouts pâles des épines et les petites épines aplaties du sommet.
            for (int i = 0; i < 26; i++)
            {
                float a = i * 2.4f, d = 4f + (i % 5) * 2.2f;
                v.Piece(Ellipse(c.x + Mathf.Cos(a) * d, c.y + Mathf.Sin(a) * d, 2.2f, 1.6f), C(150, 76, 60), 1.5f);
            }

            return v.Finish(C(60, 20, 16), 0.5f);
        }

        /// <summary>Oursin rouge : gros oursin rouge sombre aux longues épines serrées.</summary>
        private static Color[,] RedUrchin()
        {
            var v = new VolumeCanvas(120, 120);
            var c = new Vector2(60f, 60f);
            Urchin(v, c, 20f, 150, 22f, 36f, 1.8f, 0.6f, C(110, 26, 30), null, (x, y, col) => Mathf.PerlinNoise(x * 0.2f, y * 0.2f) > 0.6f ? C(170, 50, 50) : col,
                C(136, 30, 34), 45);
            return v.Finish(C(46, 10, 12), 0.45f);
        }

        /// <summary>Oursin du Cap : petit, épines courtes et serrées, pourpre, couvert de bouts de coquillages et de cailloux.</summary>
        private static Color[,] CapeUrchin()
        {
            var v = new VolumeCanvas(80, 80);
            var c = new Vector2(40f, 40f);
            Urchin(v, c, 17f, 140, 8f, 14f, 1.4f, 0.6f, C(90, 50, 96), null, null, C(120, 70, 130), 57);
            // Ce dont il se couvre pour se cacher : bouts de coquillages et petits cailloux.
            var rnd = new System.Random(9);
            for (int i = 0; i < 6; i++)
            {
                float a = (float)rnd.NextDouble() * 6.28f, d = (float)rnd.NextDouble() * 12f;
                var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                Color bit = i % 3 == 0 ? C(230, 220, 200) : i % 3 == 1 ? C(150, 140, 130) : C(210, 170, 140);
                v.Piece(Ellipse(p.x, p.y, 3.4f, 2.4f), bit, 2f);
            }

            return v.Finish(C(40, 20, 44), 0.5f);
        }

        // ---------- Concombre de mer ----------

        /// <summary>Concombre de mer noir : boudin souple, bosselé, noir mat, saupoudré de sable en petites plaques.</summary>
        private static Color[,] SeaCucumber()
        {
            var v = new VolumeCanvas(150, 60);
            // La ligne du corps, à peine courbée ; épaisseur presque constante, bouts arrondis.
            Func<float, float> mid = x => 31f + Mathf.Sin((x - 20f) * 0.03f) * 3f;
            Func<float, float, bool> body = (x, y) =>
            {
                if (x < 20f || x > 136f) return false;
                float t = (x - 20f) / 116f;
                float r = 13f * Mathf.Sqrt(Mathf.Clamp01(Mathf.Min(t, 1f - t) * 7f)) * (1f - 0.08f * t);
                return Mathf.Abs(y - mid(x)) <= r;
            };

            // La couronne de petits tentacules ramifiés autour de la bouche (à gauche), sous le corps.
            for (int i = 0; i < 9; i++)
            {
                float a = (140f + i * 10f) * Mathf.Deg2Rad;
                var root = new Vector2(24f, mid(24f));
                var tip = root + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 12f;
                v.Piece(Capsule(root.x, root.y, tip.x, tip.y, 1.4f), C(60, 54, 50), 1.2f);
                foreach (int s in new[] { -1, 1 })
                {
                    var side = tip + new Vector2(Mathf.Cos(a + s * 0.6f), Mathf.Sin(a + s * 0.6f)) * 4f;
                    v.Piece(Capsule(tip.x, tip.y, side.x, side.y, 1f), C(120, 110, 96), 1f);
                }
            }

            // Le corps : peau noire couverte d'une fine couche de sable gris-beige, sauf sur des ronds de peau noire
            // alignés par paires le long du dos.
            v.Piece(body, C(168, 158, 136), 12f, (x, y, col) =>
            {
                float dy = y - mid(x);
                for (int i = 0; i < 7; i++)
                {
                    float sx = 34f + i * 15.5f;
                    foreach (int s in new[] { -1, 1 })
                    {
                        float d = Vector2.Distance(new Vector2(x, dy), new Vector2(sx + (s > 0 ? 4f : 0f), s * 5f));
                        if (d < 3.2f) return C(24, 22, 26);
                        if (d < 4.2f) return C(90, 84, 76);   // le bord du rond, où le sable s'amincit
                    }
                }

                // Le sable : grain fin et un peu inégal ; les flancs plus sombres là où il colle moins.
                float grain = Hash(x, y, 12);
                Color sand = grain > 0.85f ? C(206, 196, 172) : grain < 0.12f ? C(120, 112, 98) : col;
                return Mathf.Abs(dy) > 9f ? Color.Lerp(sand, C(60, 56, 54), 0.45f) : sand;
            });

            // Petites papilles claires le long des deux flancs.
            for (int i = 0; i < 12; i++)
            {
                float px = 30f + i * 9.5f;
                foreach (int s in new[] { -1, 1 })
                {
                    float py = mid(px) + s * 11f;
                    if (body(px, py)) v.Piece(Ellipse(px, py, 1.4f, 1.4f), C(196, 186, 164), 1f);
                }
            }

            return v.Finish(C(22, 20, 22), 0.5f);
        }

        /// <summary>Oursin vert : épines courtes, fines et serrées, vert pâle teinté d'olive, pointes plus claires.</summary>
        private static Color[,] GreenUrchin()
        {
            var v = new VolumeCanvas(90, 90);
            var c = new Vector2(45f, 45f);
            Urchin(v, c, 22f, 180, 9f, 15f, 1.3f, 0.5f, C(100, 120, 70), null,
                (x, y, col) => Vector2.Distance(new Vector2(x, y), c) > 30f ? C(190, 210, 150) : Hash(x, y, 24) > 0.85f ? Darker(col, 0.85f) : col,
                C(130, 156, 86), 63);
            return v.Finish(C(40, 50, 26), 0.45f);
        }

        /// <summary>
        /// Concombre de mer japonais : corps brun vert marbré, hérissé de grosses papilles coniques en rangées le long du dos
        /// et des flancs, couronne de tentacules à l'avant.
        /// </summary>
        private static Color[,] JapaneseCucumber()
        {
            var v = new VolumeCanvas(150, 64);
            Func<float, float> mid = x => 32f + Mathf.Sin((x - 20f) * 0.035f) * 3f;
            Func<float, float, bool> body = (x, y) =>
            {
                if (x < 20f || x > 136f) return false;
                float t = (x - 20f) / 116f;
                float r = 12f * Mathf.Sqrt(Mathf.Clamp01(Mathf.Min(t, 1f - t) * 6f));
                return Mathf.Abs(y - mid(x)) <= r;
            };

            // La couronne de tentacules, à l'avant (à gauche).
            for (int i = 0; i < 8; i++)
            {
                float a = (145f + i * 9f) * Mathf.Deg2Rad;
                var root = new Vector2(24f, mid(24f));
                var tip = root + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 10f;
                v.Piece(Capsule(root.x, root.y, tip.x, tip.y, 1.3f), C(110, 96, 70), 1.2f);
            }

            v.Piece(body, C(96, 96, 64), 11f, (x, y, col) =>
                Mathf.PerlinNoise(x * 0.14f, y * 0.2f) > 0.6f ? C(70, 66, 46) : Hash(x, y, 25) > 0.92f ? C(140, 136, 96) : col);
            // Les papilles coniques : trois rangées sur le dos, et une sur chaque flanc qui dépasse du contour.
            for (int i = 0; i < 11; i++)
            {
                float x = 30f + i * 10f;
                foreach (float off in new[] { -5f, 0f, 5f })
                {
                    float y = mid(x) + off + (i % 2) * 1.5f;
                    if (body(x, y)) v.Piece(Ellipse(x, y, 2.6f, 2.6f), C(120, 116, 78), 2.5f, (px, py, col) => Hash(px, py, 26) > 0.6f ? Darker(col, 1.15f) : col);
                }

                foreach (int s in new[] { -1, 1 })
                {
                    float y = mid(x) + s * 11f;
                    v.Piece(Taper(new Vector2(x, y), new Vector2(x + 1f, y + s * 5f), 2.6f, 0.8f), C(110, 104, 70), 2f);
                }
            }

            return v.Finish(C(30, 30, 20), 0.5f);
        }
    }
}
