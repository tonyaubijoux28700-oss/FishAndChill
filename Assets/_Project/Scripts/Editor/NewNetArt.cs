using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les bêtes ajoutées ensuite au carnet de l'épuisette qui n'entrent pas dans les autres dessinateurs, en volume,
    /// d'après les vraies espèces. Dessins originaux.
    /// </summary>
    public static class NewNetArt
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
            "gammarus-pulex", "anodontites-trapesialis", "scutellastra-tabularis", "pyura-stolonifera", "haliotis-midae", "sipunculus-nudus",
            "dolabella-auricularia", "cypraea-tigris", "monetaria-moneta",
            "pipa-pipa", "podocnemis-unifilis", "aeshna-cyanea", "hirudo-medicinalis", "ichthyosaura-alpestris", "hippocampus-comes",
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
                case "gammarus-pulex": return Gammarus();
                case "anodontites-trapesialis": return RiverMussel();
                case "scutellastra-tabularis": return Limpet();
                case "pyura-stolonifera": return RedBait();
                case "haliotis-midae": return Abalone();
                case "sipunculus-nudus": return Peanut();
                case "dolabella-auricularia": return SeaHare();
                case "cypraea-tigris": return TigerCowrie();
                case "monetaria-moneta": return Cowrie(false);
                case "pipa-pipa": return Pipa();
                case "podocnemis-unifilis": return RiverTurtle();
                case "aeshna-cyanea": return DragonflyLarva();
                case "hirudo-medicinalis": return MedicinalLeech();
                case "ichthyosaura-alpestris": return AlpineNewt();
                case "hippocampus-comes": return Seahorse();
                default: return null;
            }
        }

        /// <summary>
        /// Gammare, de profil, couché sur le flanc comme il nage : corps arqué en anneaux brun gris translucides, deux paires
        /// d'antennes vers l'avant (à droite), petites pattes dessous, œil noir en haricot.
        /// </summary>
        private static Color[,] Gammarus()
        {
            var v = new VolumeCanvas(96, 64);
            Color body = C(168, 150, 116);
            // Les pattes et les antennes, sous le corps.
            for (int i = 0; i < 9; i++)
            {
                float a = (200f - i * 16f) * Mathf.Deg2Rad;
                var root = new Vector2(48f + Mathf.Cos(a) * 22f, 40f - Mathf.Sin(a) * 18f);
                var tip = root + new Vector2(Mathf.Cos(a - 1.4f) * 9f, 8f);
                v.Piece(Capsule(root.x, root.y, tip.x, tip.y, 0.8f), C(186, 170, 140), 1f, null, 0.85f, 0.8f);
            }

            v.Piece(Capsule(74f, 24f, 92f, 10f, 0.9f), C(170, 150, 110), 1f);
            v.Piece(Capsule(74f, 26f, 94f, 22f, 0.8f), C(170, 150, 110), 1f);
            // Le corps, en arc de cercle, anneau après anneau (de la queue à la tête).
            for (int i = 0; i < 12; i++)
            {
                float a = (210f - i * 15f) * Mathf.Deg2Rad;
                var p = new Vector2(48f + Mathf.Cos(a) * 22f, 44f - Mathf.Sin(a) * 22f);
                float r = 6f + Mathf.Sin(i / 11f * Mathf.PI) * 3f;
                v.Piece(Ellipse(p.x, p.y, r, r * 0.9f), body, r * 0.8f, (x, y, c) => Hash(x, y, 2) > 0.88f ? C(196, 180, 150) : Hash(x / 2, y / 2, 3) > 0.8f ? Darker(c, 0.9f) : c, 0.8f);
            }

            v.Piece(Ellipse(72f, 28f, 2.4f, 1.6f), C(20, 18, 16), 1f);   // l'œil en haricot
            return v.Finish(C(70, 58, 40), 0.5f);
        }

        // ---------- Outils ----------

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

        /// <summary>Contour par quarts d'ellipse autour d'un point : rayons à droite, à gauche, en haut, en bas (bord continu).</summary>
        private static Func<float, float, float> Quadrants(Vector2 o, float right, float left, float up, float down, float wobble, int seed)
            => (x, y) =>
            {
                float dx = x - o.x, dy = y - o.y;
                float a = Mathf.Atan2(dy, dx);
                float rx = dx > 0f ? right : left, ry = dy > 0f ? down : up;
                float r = 1f / Mathf.Sqrt(Mathf.Cos(a) * Mathf.Cos(a) / (rx * rx) + Mathf.Sin(a) * Mathf.Sin(a) / (ry * ry));
                r *= 1f + (Mathf.PerlinNoise(a * 2f + seed, 0.3f) - 0.5f) * wobble;
                return Mathf.Sqrt(dx * dx + dy * dy) / r;   // 0 au centre, 1 sur le bord
            };

        // ---------- Mollusques et autres ----------

        /// <summary>Moule d'eau douce géante : longue coquille trapézoïdale, l'arrière plus haut, brun vert, cernes irréguliers.</summary>
        private static Color[,] RiverMussel()
        {
            var v = new VolumeCanvas(120, 60);
            var umbo = new Vector2(34f, 20f);
            Func<float, float, float> depth = Quadrants(umbo, 80f, 30f, 12f, 34f, 0.08f, 3);
            v.Piece((x, y) => depth(x, y) <= 1f, C(100, 100, 60), 12f, (x, y, c) =>
            {
                float t = depth(x, y);
                Color col = Color.Lerp(C(84, 70, 40), C(120, 124, 70), Mathf.Clamp01(t * 1.3f));
                float band = t * 12f + Mathf.PerlinNoise(x * 0.07f, y * 0.07f) * 1.4f;
                return band - Mathf.Floor(band) < 0.12f ? Darker(col, 0.78f) : col;
            });
            return v.Finish(C(40, 38, 22), 0.5f);
        }

        /// <summary>Patelle géante, vue de dessus : grand chapeau chinois aplati, côtes rayonnantes au bord festonné, sommet près du centre.</summary>
        private static Color[,] Limpet()
        {
            var v = new VolumeCanvas(84, 76);
            var apex = new Vector2(40f, 36f);
            Func<float, float, float> depth = Quadrants(apex, 38f, 34f, 32f, 34f, 0.06f, 5);
            v.Piece((x, y) =>
            {
                float a = Mathf.Atan2(y - apex.y, x - apex.x);
                return depth(x, y) <= 1f - Mathf.Abs(Mathf.Sin(a * 13f)) * 0.06f;   // bord festonné par les côtes
            }, C(196, 184, 160), 18f, (x, y, c) =>
            {
                float a = Mathf.Atan2(y - apex.y, x - apex.x);
                float rib = Mathf.Sin(a * 13f + Mathf.PerlinNoise(x * 0.1f, y * 0.1f));
                Color col = rib > 0.5f ? Darker(c, 1.1f) : rib < -0.5f ? Darker(c, 0.82f) : c;
                if (Mathf.Sin(a * 6f + 2f) > 0.85f) col = Color.Lerp(col, C(120, 90, 70), 0.5f);   // rayons bruns
                return depth(x, y) < 0.12f ? C(220, 210, 190) : col;                           // le sommet usé, plus clair
            });
            return v.Finish(C(70, 62, 50), 0.5f);
        }

        /// <summary>
        /// Ascidie rouge : un sac épais au cuir bosselé, brun rouille couvert de bouts d'algues, et deux siphons courts au
        /// sommet, rouge sang à l'intérieur.
        /// </summary>
        private static Color[,] RedBait()
        {
            var v = new VolumeCanvas(70, 84);
            var c = new Vector2(35f, 48f);
            Func<float, float, float> depth = Quadrants(c, 26f, 25f, 30f, 32f, 0.18f, 7);
            v.Piece((x, y) => depth(x, y) <= 1f, C(140, 90, 66), 14f, (x, y, col) =>
            {
                float lump = Mathf.PerlinNoise(x * 0.18f, y * 0.18f);
                if (Hash(x / 2, y / 2, 4) > 0.9f) return C(100, 130, 70);                    // bouts d'algues
                return lump > 0.62f ? C(170, 116, 84) : lump < 0.32f ? C(104, 64, 46) : col;
            });
            foreach (int s in new[] { -1, 1 })
            {
                // Un siphon : un tube court et bosselé comme le corps, penché vers l'extérieur, l'ouverture en croix.
                var top = new Vector2(35f + s * 12f, 10f);
                v.Piece(Taper(new Vector2(35f + s * 7f, 26f), top, 6.5f, 4.6f), C(146, 94, 68), 4f,
                    (x, y, col) => Mathf.PerlinNoise(x * 0.2f, y * 0.2f) > 0.6f ? C(170, 116, 84) : col);
                v.Piece(Ellipse(top.x, top.y, 4.6f, 3.4f), C(160, 104, 76), 3f);
                v.Piece(Union(Box(top.x - 3f, top.y - 0.6f, top.x + 3f, top.y + 0.6f), Box(top.x - 0.6f, top.y - 2.4f, top.x + 0.6f, top.y + 2.4f)),
                    C(150, 30, 40), 1f);
            }

            return v.Finish(C(50, 30, 22), 0.5f);
        }

        /// <summary>
        /// Ormeau du Cap, vu de dessus : coquille plate en forme d'oreille, la spire à un bout, une rangée de trous le long d'un
        /// bord, côtes ondulées brun rouge ; les petits tentacules sombres du pied dépassent tout autour.
        /// </summary>
        private static Color[,] Abalone()
        {
            var v = new VolumeCanvas(110, 80);
            var c = new Vector2(55f, 40f);
            Func<float, float, float> depth = Quadrants(c, 48f, 46f, 30f, 32f, 0.05f, 9);
            // Le pied et sa frange de tentacules, sous la coquille.
            v.Piece((x, y) => depth(x, y) <= 1.08f, C(60, 50, 40), 4f, (x, y, col) => Hash(x, y, 5) > 0.6f ? C(90, 80, 60) : col);
            var spire = new Vector2(84f, 48f);
            v.Piece((x, y) => depth(x, y) <= 1f, C(150, 96, 70), 9f, (x, y, col) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), spire);
                float ridge = Mathf.Sin(d * 0.7f + Mathf.Sin(Mathf.Atan2(y - spire.y, x - spire.x) * 6f) * 1.2f);
                Color shade = ridge > 0.5f ? C(176, 120, 86) : ridge < -0.6f ? C(110, 70, 52) : col;
                return Mathf.PerlinNoise(x * 0.12f, y * 0.12f) > 0.66f ? Color.Lerp(shade, C(120, 140, 90), 0.4f) : shade;
            });
            // La rangée de trous respiratoires, le long du bord gauche-haut.
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f;
                var hole = new Vector2(Mathf.Lerp(80f, 18f, t), 18f + Mathf.Sin(t * Mathf.PI) * -4f + t * 8f);
                v.Piece(Ellipse(hole.x, hole.y, 2.4f, 2f), C(40, 30, 26), 1.5f);
            }

            v.Piece(Ellipse(spire.x, spire.y, 4f, 3.4f), C(190, 150, 120), 3f);   // la spire
            return v.Finish(C(50, 32, 22), 0.5f);
        }

        /// <summary>
        /// Sipuncle : long ver de sable rose nacré, lisse, sans anneaux, rayé de fines lignes en long ; l'avant s'effile en
        /// trompe avec une petite couronne de tentacules.
        /// </summary>
        private static Color[,] Peanut()
        {
            var v = new VolumeCanvas(140, 40);
            Func<float, float> mid = x => 20f + Mathf.Sin(x * 0.04f) * 3f;
            v.Piece((x, y) =>
            {
                if (x < 6f || x > 134f) return false;
                float t = (x - 6f) / 128f;
                float half = t < 0.3f ? Mathf.Lerp(2.5f, 9f, t / 0.3f) : 9f * Mathf.Sqrt(Mathf.Clamp01((1f - t) * 5f));
                return Mathf.Abs(y - mid(x)) <= half;
            }, C(214, 160, 150), 8f, (x, y, c) =>
            {
                float d = y - mid(x);
                if (Mathf.Abs(Mathf.Repeat(d + 9f, 3f) - 1.5f) < 0.3f && x > 40) return C(186, 130, 126);   // fines lignes en long
                return Mathf.PerlinNoise(x * 0.1f, y * 0.3f) > 0.62f ? C(236, 206, 206) : c;                 // reflets nacrés
            });
            for (int i = 0; i < 6; i++)
            {
                float a = (150f + i * 12f) * Mathf.Deg2Rad;
                var root = new Vector2(7f, mid(7f));
                var tip = root + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 5f;
                v.Piece(Capsule(root.x, root.y, tip.x, tip.y, 0.8f), C(230, 190, 180), 1f);
            }

            return v.Finish(C(110, 70, 64), 0.5f);
        }


        /// <summary>
        /// Porcelaine, vue de dessus : dôme ovale lisse et brillant. Tigre : blanc bleuté semé de taches noires de toutes
        /// tailles, au halo brun. Monnaie : petite, jaune pâle, presque hexagonale, côtés bosselés plus dorés.
        /// </summary>
        private static Color[,] Cowrie(bool tiger)
        {
            int w = tiger ? 70 : 48, h = tiger ? 96 : 60;
            var v = new VolumeCanvas(w, h);
            float cx = w * 0.5f, cy = h * 0.5f, rx = w * 0.42f, ry = h * 0.44f;
            Func<float, float, bool> shape = tiger
                ? Ellipse(cx, cy, rx, ry)
                : (x, y) =>
                {
                    // Presque hexagonale : côtés épaissis en bosses arrondies.
                    float u = (x - cx) / rx, t = (y - cy) / ry;
                    float bumps = 1f + Mathf.Abs(Mathf.Sin(Mathf.Atan2(t, u) * 3f)) * 0.12f;
                    return Mathf.Pow(Mathf.Abs(u), 1.6f) + Mathf.Pow(Mathf.Abs(t), 1.8f) <= bumps;
                };
            v.Piece(shape, tiger ? C(214, 216, 226) : C(236, 220, 150), rx * 0.8f, (x, y, c) =>
            {
                if (tiger)
                {
                    // Taches noires de tailles variées, plus serrées sur les bords.
                    // Taches rondes brun noir, de tailles variées, chacune avec un halo brun clair.
                    for (int gx = x / 9 - 1; gx <= x / 9 + 1; gx++)
                    {
                        for (int gy = y / 9 - 1; gy <= y / 9 + 1; gy++)
                        {
                            if (Hash(gx, gy, 10) < 0.3f) continue;
                            var spot = new Vector2(gx * 9 + 2f + Hash(gx, gy, 7) * 5f, gy * 9 + 2f + Hash(gy, gx, 8) * 5f);
                            float r = 1.8f + Hash(gx, gy, 9) * 2.6f, d = Vector2.Distance(new Vector2(x, y), spot);
                            if (d < r) return C(36, 28, 30);
                            if (d < r + 1.3f) return C(150, 120, 110);
                        }
                    }

                    if (Mathf.Abs(x - cx) < 0.8f) return C(236, 230, 210);                     // la fine ligne claire du dos
                    return Color.Lerp(c, C(170, 160, 190), Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.5f);
                }

                // Jaune pâle, plus doré sur les bosses des côtés.
                float side = Mathf.Abs(x - cx) / rx;
                return side > 0.6f ? Color.Lerp(c, C(220, 180, 90), (side - 0.6f) * 2f) : c;
            });
            // Le reflet brillant de la porcelaine.
            v.Piece(Ellipse(cx - rx * 0.35f, cy - ry * 0.4f, rx * 0.18f, ry * 0.12f), new Color(1f, 1f, 1f), 1f);
            return v.Finish(tiger ? C(40, 36, 46) : C(110, 90, 50), 0.5f);
        }

        /// <summary>
        /// Lièvre de mer, vu de dessus : corps en poire un peu courbé, au contour bosselé ; tête fine avec deux rhinophores
        /// enroulés en oreilles et deux tentacules buccaux évasés ; la fente sinueuse des replis sur le dos ; l'arrière qui
        /// finit en disque ; peau vert olive marbrée, tubercules et petites taches cerclées.
        /// </summary>
        private static Color[,] SeaHare()
        {
            var v = new VolumeCanvas(84, 130);
            // La ligne du corps, légèrement courbée, et son épaisseur en poire (fine à la tête, large et ronde à l'arrière).
            Func<float, float> line = y => 42f + Mathf.Sin(y * 0.035f) * 5f;
            Func<float, float> half = y =>
            {
                float t = Mathf.InverseLerp(14f, 122f, y);
                float r = t < 0.25f ? Mathf.Lerp(5f, 9f, t / 0.25f) : t < 0.8f ? Mathf.Lerp(9f, 25f, Mathf.SmoothStep(0f, 1f, (t - 0.25f) / 0.55f)) : 25f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((t - 0.8f) / 0.2f, 2f)));
                return r * (1f + (Mathf.PerlinNoise(y * 0.15f, 1.7f) - 0.5f) * 0.18f);   // contour bosselé
            };
            Func<float, float, bool> body = (x, y) => y >= 14f && y <= 122f && Mathf.Abs(x - line(y)) <= half(y);

            // Les deux tentacules buccaux évasés, sous la tête.
            foreach (int s in new[] { -1, 1 }) v.Piece(Ellipse(line(16f) + s * 7f, 16f, 5f, 3f), C(120, 126, 76), 2.5f);

            v.Piece(body, C(110, 120, 70), 14f, (x, y, c) =>
            {
                float n = Mathf.PerlinNoise(x * 0.13f, y * 0.13f), fine = Mathf.PerlinNoise(x * 0.4f + 9f, y * 0.4f);
                Color col = n > 0.6f ? C(80, 92, 52) : n < 0.32f ? C(146, 140, 88) : c;
                if (fine > 0.72f) col = Darker(col, 1.15f);                                    // tubercules
                // Petites taches sombres cerclées de clair, éparses.
                int gx = x / 7, gy = y / 7;
                var spot = new Vector2(gx * 7 + 2f + Hash(gx, gy, 31) * 3f, gy * 7 + 2f + Hash(gy, gx, 32) * 3f);
                float d = Vector2.Distance(new Vector2(x, y), spot);
                if (Hash(gx, gy, 33) > 0.7f && d < 1.2f) return C(40, 46, 26);
                if (Hash(gx, gy, 33) > 0.7f && d < 2f) return C(170, 170, 120);
                return col;
            });
            // La fente des replis du manteau, sinueuse, sur l'arrière du dos.
            for (float y = 64f; y < 112f; y += 1f)
            {
                float x = line(y) + Mathf.Sin(y * 0.3f) * 2f;
                v.Dot(Mathf.RoundToInt(x), Mathf.RoundToInt(y), C(56, 62, 34));
            }

            // Le disque de l'arrière.
            v.Piece(Ellipse(line(110f), 110f, 10f, 7f), C(96, 104, 60), 4f, (x, y, c) => Hash(x, y, 34) > 0.8f ? Darker(c, 0.85f) : c);
            // Les rhinophores : deux oreilles enroulées, dressées.
            foreach (int s in new[] { -1, 1 })
            {
                var root = new Vector2(line(22f) + s * 4f, 22f);
                var tip = root + new Vector2(s * 5f, -15f);
                v.Piece(Taper(root, tip, 3.2f, 1.8f), C(120, 128, 78), 2.5f, (x, y, c) => (x + y) % 3 == 0 ? Darker(c, 0.85f) : c);
            }

            return v.Finish(C(34, 40, 20), 0.5f);
        }

        /// <summary>
        /// Porcelaine tigre, vue de dessus : coquille en œuf un peu asymétrique (plus étroite à l'avant), brillante, blanc
        /// bleuté passant au crème, semée au hasard de taches brun noir de formes et de tailles variées, chacune avec un halo
        /// brun orangé ; la ligne claire du dos ondule ; un reflet suit la courbe.
        /// </summary>
        private static Color[,] TigerCowrie()
        {
            var v = new VolumeCanvas(72, 100);
            float cx = 36f, cy = 50f;
            Func<float, float, bool> shape = (x, y) =>
            {
                float t = (y - cy) / 44f;
                float w = 29f * (1f - 0.12f * t) * (1f + (Mathf.PerlinNoise(y * 0.08f, 3f) - 0.5f) * 0.06f);   // plus étroite devant (en haut)
                float u = (x - cx - Mathf.Sin(t * 2f) * 1.5f) / w;
                return u * u + t * t <= 1f;
            };
            // Les taches, tirées au hasard (pas en grille), avec leurs formes irrégulières.
            var rnd = new System.Random(77);
            var spots = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < 46; i++) spots.Add(new Vector3(8f + (float)rnd.NextDouble() * 56f, 8f + (float)rnd.NextDouble() * 84f, 1.4f + (float)rnd.NextDouble() * 3.6f));
            v.Piece(shape, C(214, 216, 226), 22f, (x, y, c) =>
            {
                Color col = Color.Lerp(C(206, 212, 230), C(226, 214, 186), Mathf.PerlinNoise(x * 0.06f, y * 0.06f));
                if (Mathf.Abs(x - (cx + Mathf.Sin(y * 0.15f) * 1.6f)) < 0.9f) return C(240, 232, 210);   // la ligne du dos, ondulée
                foreach (Vector3 s in spots)
                {
                    float wob = 1f + (Mathf.PerlinNoise(x * 0.4f + s.x, y * 0.4f) - 0.5f) * 0.5f;
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(s.x, s.y)) / (s.z * wob);
                    if (d < 1f) return C(40, 28, 26);
                    if (d < 1.45f) col = Color.Lerp(col, C(170, 110, 70), 0.7f);
                }

                return col;
            });
            // Le reflet, en arc le long de la courbe.
            for (int i = 0; i < 10; i++)
            {
                float a = (200f + i * 6f) * Mathf.Deg2Rad;
                v.Piece(Ellipse(cx + Mathf.Cos(a) * 18f, cy + Mathf.Sin(a) * 30f, 2.4f, 1.6f), new Color(1f, 1f, 1f), 1f);
            }

            return v.Finish(C(46, 36, 40), 0.5f);
        }

        // ---------- Vertébrés et larve ----------

        /// <summary>
        /// Crapaud pipa, vu de dessus : plat comme une feuille morte, corps presque carré, tête triangulaire aux tout petits
        /// yeux, longs bras aux doigts finis en petites étoiles, grandes pattes arrière palmées ; dos brun semé de petits creux.
        /// </summary>
        private static Color[,] Pipa()
        {
            var v = new VolumeCanvas(120, 120);
            Color skin = C(140, 118, 82);
            Func<int, int, Color, Color> mottled = (x, y, c) => Mathf.PerlinNoise(x * 0.14f, y * 0.14f) > 0.6f ? C(110, 90, 60) : Hash(x, y, 40) > 0.92f ? C(170, 150, 110) : c;
            foreach (int s in new[] { -1, 1 })
            {
                // Bras longs et fins, vers l'avant, doigts en étoiles.
                var shoulder = new Vector2(60f + s * 16f, 42f);
                var elbow = shoulder + new Vector2(s * 14f, 2f);
                var hand = elbow + new Vector2(s * 4f, -16f);
                v.Piece(Capsule(shoulder.x, shoulder.y, elbow.x, elbow.y, 2.6f), skin, 2f, mottled);
                v.Piece(Capsule(elbow.x, elbow.y, hand.x, hand.y, 2.2f), skin, 2f, mottled);
                for (int f = 0; f < 4; f++)
                {
                    float a = (-150f + f * 30f) * Mathf.Deg2Rad;
                    var tip = hand + new Vector2(Mathf.Cos(a) * s * -1f, Mathf.Sin(a)) * 5f;
                    v.Piece(Capsule(hand.x, hand.y, tip.x, tip.y, 0.8f), skin, 1f);
                    for (int k = 0; k < 4; k++) v.Dot(Mathf.RoundToInt(tip.x + (k % 2 == 0 ? 1 : -1)), Mathf.RoundToInt(tip.y + (k < 2 ? 1 : -1)), C(200, 180, 140));
                }

                // Grandes pattes arrière palmées, repliées sur les côtés.
                var hip = new Vector2(60f + s * 18f, 86f);
                var knee = hip + new Vector2(s * 16f, -10f);
                var foot = knee + new Vector2(s * 4f, 24f);
                v.Piece(Capsule(hip.x, hip.y, knee.x, knee.y, 5f), skin, 4f, mottled);
                v.Piece(Capsule(knee.x, knee.y, foot.x, foot.y, 3.6f), skin, 3f, mottled);
                // Le grand pied palmé en éventail : cinq orteils fins reliés par une palmure festonnée.
                float baseAngle = s > 0 ? 70f : 110f;
                v.Piece((x, y) =>
                {
                    var d = new Vector2(x, y) - foot;
                    float r = d.magnitude;
                    if (r < 1f || r > 15f) return false;
                    float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    float rel = (ang - baseAngle) * s;   // de -45 à 45 degrés
                    if (Mathf.Abs(rel) > 46f) return false;
                    float scallop = 11f + 3f * Mathf.Abs(Mathf.Cos(rel * Mathf.Deg2Rad * 4f));
                    return r < scallop;
                }, C(156, 134, 96), 2f, (x, y, c) => Hash(x, y, 48) > 0.9f ? C(126, 104, 70) : c);
                for (int f = 0; f < 5; f++)
                {
                    float a = (baseAngle + s * (-44f + f * 22f)) * Mathf.Deg2Rad;
                    var tip = foot + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 14f;
                    v.Piece(Capsule(foot.x, foot.y, tip.x, tip.y, 1f), C(120, 98, 66), 1.5f);
                }
            }

            // Le corps plat et la tête triangulaire d'un seul tenant, bords un peu irréguliers comme une feuille morte.
            v.Piece((x, y) =>
            {
                float wob = (Mathf.PerlinNoise(x * 0.15f, y * 0.15f) - 0.5f) * 3f;
                float half;
                if (y < 16f || y > 96f) return false;
                if (y < 42f) half = Mathf.Lerp(3f, 22f, Mathf.SmoothStep(0f, 1f, (y - 16f) / 26f));
                else if (y < 84f) half = 22f + Mathf.Sin((y - 42f) / 42f * Mathf.PI) * 3f;
                else half = 22f * Mathf.Sqrt(1f - (y - 84f) / 12f);
                return Mathf.Abs(x - 60f) <= half + wob;
            }, skin, 6f, (x, y, c) =>
            {
                Color col = mottled(x, y, c);
                return y > 44 && Hash(x / 4, y / 4, 41) > 0.7f && x % 4 == 1 && y % 4 == 1 ? Darker(col, 0.7f) : col;   // les petits creux du dos
            });
            foreach (int s in new[] { -1, 1 }) v.Dot(60 + s * 5, 27, C(20, 16, 12));   // les tout petits yeux
            return v.Finish(C(50, 40, 26), 0.5f);
        }

        /// <summary>
        /// Tortue de l'Amazone à taches jaunes, vue de dessus : carapace ovale lisse gris brun sombre aux écailles à peine
        /// marquées, tête noire tachée de jaune vif, pattes palmées griffues, petite queue.
        /// </summary>
        private static Color[,] RiverTurtle()
        {
            var v = new VolumeCanvas(110, 130);
            Color skin = C(70, 74, 64);
            foreach (int s in new[] { -1, 1 })
            {
                v.Piece(Ellipse(55f + s * 30f, 44f, 9f, 6f), skin, 4f, (x, y, c) => Hash(x, y, 42) > 0.85f ? C(100, 104, 90) : c);
                v.Piece(Ellipse(55f + s * 26f, 98f, 8f, 6f), skin, 4f);
                for (int k = 0; k < 3; k++) v.Dot(Mathf.RoundToInt(55f + s * (36f + k * 1.5f)), 39 + k * 3, C(220, 210, 180));   // griffes
            }

            v.Piece(Capsule(55f, 108f, 55f, 118f, 2.4f), skin, 2f);   // la queue
            // La tête, noire et tachée de jaune.
            v.Piece(Union(Taper(new Vector2(55f, 34f), new Vector2(55f, 10f), 6.5f, 4.5f), Ellipse(55f, 20f, 7f, 8f)), C(42, 42, 40), 6f, (x, y, c) =>
            {
                // Les taches jaunes : une sur le museau, une derrière chaque œil, une sur chaque joue.
                var p = new Vector2(x, y);
                if (Vector2.Distance(p, new Vector2(55f, 11f)) < 2.2f) return C(240, 206, 60);
                foreach (int s in new[] { -1, 1 })
                {
                    if (Vector2.Distance(p, new Vector2(55f + s * 4f, 22f)) < 2.4f) return C(240, 206, 60);
                    if (Vector2.Distance(p, new Vector2(55f + s * 5.5f, 15f)) < 1.6f) return C(230, 196, 56);
                }

                return c;
            });
            foreach (int s in new[] { -1, 1 }) v.Piece(Ellipse(55f + s * 5f, 17f, 1.4f, 1.6f), C(16, 16, 14), 1f);
            // La carapace lisse, aux écailles à peine dessinées.
            var scutes = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < 5; i++) scutes.Add(new Vector2(55f, 42f + i * 13f));
            for (int i = 0; i < 4; i++) { scutes.Add(new Vector2(37f, 48f + i * 14f)); scutes.Add(new Vector2(73f, 48f + i * 14f)); }
            v.Piece(Ellipse(55f, 70f, 30f, 40f), C(80, 80, 66), 14f, (x, y, c) =>
            {
                var p = new Vector2(x + (Mathf.PerlinNoise(x * 0.12f, y * 0.12f) - 0.5f) * 4f, y);
                float d1 = float.MaxValue, d2 = float.MaxValue;
                foreach (Vector2 sc in scutes)
                {
                    float d = Vector2.Distance(p, sc);
                    if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) d2 = d;
                }

                if (d2 - d1 < 1.2f) return C(56, 56, 46);
                return Mathf.PerlinNoise(x * 0.1f, y * 0.1f) > 0.6f ? C(96, 96, 78) : c;
            });
            return v.Finish(C(24, 26, 20), 0.5f);
        }

        /// <summary>
        /// Larve de libellule, vue de dessus : grosse tête aux yeux globuleux, thorax aux six pattes fines, fourreaux des
        /// ailes sur le dos, long abdomen en anneaux finissant par trois petites pointes ; vert brun marbré.
        /// </summary>
        private static Color[,] DragonflyLarva()
        {
            var v = new VolumeCanvas(80, 140);
            Color skin = C(96, 104, 64);
            Func<int, int, Color, Color> mottled = (x, y, c) => Mathf.PerlinNoise(x * 0.2f, y * 0.2f) > 0.6f ? C(70, 76, 44) : Hash(x, y, 44) > 0.92f ? C(140, 140, 90) : c;
            // Les six pattes, fines et coudées.
            for (int i = 0; i < 3; i++)
            {
                foreach (int s in new[] { -1, 1 })
                {
                    var root = new Vector2(40f + s * 6f, 40f + i * 7f);
                    var knee = root + new Vector2(s * 16f, -4f + i * 6f);
                    var foot = knee + new Vector2(s * 6f, 12f + i * 4f);
                    v.Piece(Capsule(root.x, root.y, knee.x, knee.y, 1.3f), skin, 1f, mottled);
                    v.Piece(Capsule(knee.x, knee.y, foot.x, foot.y, 1f), skin, 1f, mottled);
                }
            }

            // L'abdomen en anneaux, et les trois pointes au bout.
            for (int i = 0; i < 9; i++)
            {
                float y = 62f + i * 7.5f, w = 10f - Mathf.Abs(i - 3f) * 1.1f;
                v.Piece(Ellipse(40f, y, w, 4.6f), skin, 4f, (x, yy, c) => yy % 8 == 0 ? Darker(c, 0.8f) : mottled(x, yy, c));
            }

            foreach (int s in new[] { -1, 0, 1 }) v.Piece(Capsule(40f + s * 2f, 128f, 40f + s * 4f, 136f, 1f), C(120, 120, 80), 1f);
            // Le thorax et les fourreaux des ailes.
            v.Piece(Ellipse(40f, 46f, 8f, 10f), skin, 5f, mottled);
            foreach (int s in new[] { -1, 1 }) v.Piece(Ellipse(40f + s * 4f, 60f, 3.4f, 10f), C(110, 116, 76), 2f, mottled);
            // La tête et ses gros yeux globuleux.
            v.Piece(Ellipse(40f, 26f, 11f, 8f), skin, 5f, mottled);
            foreach (int s in new[] { -1, 1 }) v.Piece(Ellipse(40f + s * 8f, 24f, 5f, 5.4f), C(80, 70, 50), 4f, (x, y, c) => (x + y) % 2 == 0 ? Darker(c, 0.85f) : c);
            return v.Finish(C(30, 34, 20), 0.5f);
        }

        /// <summary>Sangsue médicinale, vue de dessus : corps long et aplati, vert olive, bandes rouille en long ponctuées de noir, anneaux fins.</summary>
        private static Color[,] MedicinalLeech()
        {
            var v = new VolumeCanvas(140, 40);
            Func<float, float> mid = x => 20f + Mathf.Sin(x * 0.06f + 1f) * 4f;
            v.Piece((x, y) =>
            {
                if (x < 6f || x > 134f) return false;
                float t = (x - 6f) / 128f;
                float half = 2.5f + 8f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI * 0.92f + 0.12f);
                return Mathf.Abs(y - mid(x)) <= half;
            }, C(84, 100, 60), 6f, (x, y, c) =>
            {
                float d = y - mid(x);
                if (x % 3 == 0) return Darker(c, 0.85f);                                       // les anneaux
                foreach (float band in new[] { -4f, 4f })
                {
                    if (Mathf.Abs(d - band) < 1.4f) return (x / 4) % 3 == 0 ? C(30, 30, 20) : C(196, 110, 50);   // bandes rouille ponctuées de noir
                }

                return c;
            });
            v.Piece(Ellipse(130f, mid(130f), 5f, 6f), C(96, 110, 70), 3f);   // la ventouse arrière
            return v.Finish(C(26, 30, 18), 0.5f);
        }

        /// <summary>
        /// Triton alpestre (mâle au printemps), de profil, tête à droite : dos bleu ardoise, flancs blanchâtres tachés de noir,
        /// ventre orange vif sans taches, petite crête claire tachetée de noir, queue aplatie.
        /// </summary>
        private static Color[,] AlpineNewt()
        {
            var v = new VolumeCanvas(140, 54);
            Func<float, float> mid = x => 28f + Mathf.Sin(x * 0.03f) * 2f;
            Func<float, float> half = x =>
            {
                float t = (x - 6f) / 128f;
                return t < 0.55f ? Mathf.Lerp(1.5f, 7f, t / 0.55f) : t < 0.85f ? 7f : 7f * Mathf.Sqrt(Mathf.Max(0f, (1f - t) / 0.15f));
            };
            // Les pattes, sous le corps.
            foreach (float px in new[] { 64f, 108f })
            {
                v.Piece(Capsule(px, mid(px) + 4f, px - 4f, mid(px) + 14f, 2.4f), C(220, 120, 40), 2f);
                v.Piece(Capsule(px - 4f, mid(px) + 14f, px + 2f, mid(px) + 17f, 1.6f), C(200, 100, 40), 1f);
            }

            v.Piece((x, y) => x > 6f && x < 134f && Mathf.Abs(y - mid(x)) <= half(x), C(70, 84, 110), 6f, (x, y, c) =>
            {
                float rel = (y - mid(x)) / Mathf.Max(1f, half(x));   // -1 dos, +1 ventre
                if (rel > 0.35f) return C(240, 130, 30);                                        // le ventre orange
                if (rel > -0.1f) return Hash(x / 2, y / 2, 45) > 0.75f ? C(30, 30, 34) : C(214, 214, 206);   // flancs tachetés
                return Mathf.PerlinNoise(x * 0.15f, y * 0.15f) > 0.6f ? C(54, 66, 90) : c;    // dos bleu ardoise
            });
            // La crête, basse, claire et tachetée, le long du dos et de la queue.
            v.Piece((x, y) => x > 10f && x < 110f && y < mid(x) - half(x) + 1f && y > mid(x) - half(x) - 3f + Mathf.Abs(Mathf.Sin(x * 0.5f)) * 0.8f, C(220, 214, 190), 1.5f,
                (x, y, c) => (x / 3) % 2 == 0 ? C(40, 40, 40) : c);
            v.Piece(Ellipse(124f, mid(124f) - 2f, 1.8f, 1.8f), C(20, 18, 14), 1f);   // l'œil
            return v.Finish(C(26, 30, 40), 0.5f);
        }

        /// <summary>
        /// Hippocampe à queue tigrée, de profil, debout : tête de cheval au long museau et à la petite couronne, ventre bombé,
        /// corps en anneaux bosselés, queue enroulée ; jaune rayé de bandes noires, la queue surtout.
        /// </summary>
        private static Color[,] Seahorse()
        {
            var v = new VolumeCanvas(72, 120);
            Color yellow = C(226, 180, 50), black = C(36, 30, 26);
            // La queue enroulée en spirale, sous le corps.
            Vector2 prev = new Vector2(34f, 84f);
            for (int i = 1; i <= 26; i++)
            {
                float t = i / 26f;
                float a = t * 5.2f, r = Mathf.Lerp(14f, 3f, t);
                var p = new Vector2(30f + Mathf.Sin(a) * r, 98f - Mathf.Cos(a) * r * 0.9f + (1f - t) * 4f);
                float w = Mathf.Lerp(4.6f, 1.6f, t);
                v.Piece(Capsule(prev.x, prev.y, p.x, p.y, w), (i / 2) % 2 == 0 ? black : yellow, w, null, 0.8f);   // la queue tigrée
                prev = p;
            }

            // Le corps : nuque fine, ventre bombé devant, dos un peu creusé, bord bosselé à chaque anneau.
            v.Piece((x, y) =>
            {
                if (y < 30f || y > 86f) return false;
                float t = (y - 30f) / 56f;
                float bump = (y % 5 < 2) ? 0.8f : 0f;
                float back = 27f - Mathf.Sin(t * Mathf.PI) * 2f - bump;
                float belly = 33f + Mathf.Sin(Mathf.Pow(t, 0.8f) * Mathf.PI) * 11f - t * 3f + bump;
                return x >= back && x <= belly;
            }, yellow, 9f, (x, y, c) =>
            {
                if (y % 5 == 0) return Darker(c, 0.8f);                                         // les anneaux
                if (y > 66 && (y / 5) % 2 == 0) return Color.Lerp(c, black, 0.75f);             // les bandes de tigre, vers la queue
                return Mathf.PerlinNoise(x * 0.2f, y * 0.2f) > 0.65f ? C(206, 156, 40) : Hash(x, y, 46) > 0.92f ? C(250, 222, 120) : c;
            });
            v.Piece(Ellipse(25f, 60f, 2.6f, 6f), C(244, 220, 140), 1.5f, (x, y, c) => y % 2 == 0 ? Darker(c, 0.88f) : c);   // la nageoire du dos
            // La tête : couronne, œil, et long museau tubulaire vers la droite.
            v.Piece(Union(Ellipse(35f, 23f, 7f, 7.5f), Capsule(31f, 26f, 32f, 34f, 4f),
                Taper(new Vector2(40f, 25f), new Vector2(56f, 28f), 3.4f, 2.2f)), yellow, 6f,
                (x, y, c) => Hash(x, y, 47) > 0.85f ? Darker(c, 0.85f) : c);
            v.Piece(Union(Taper(new Vector2(33f, 17f), new Vector2(32f, 10f), 2.6f, 1.4f), Taper(new Vector2(30f, 17f), new Vector2(27f, 12f), 1.6f, 1f)),
                C(232, 190, 64), 2f);   // la couronne
            v.Piece(Ellipse(37f, 22f, 1.8f, 1.8f), C(20, 18, 14), 1f);
            for (int k = 0; k < 4; k++) v.Dot(39 + k * 4, 26 + k / 2, C(60, 44, 20));   // les bandes fines du museau
            return v.Finish(C(70, 50, 14), 0.5f);
        }
    }
}
