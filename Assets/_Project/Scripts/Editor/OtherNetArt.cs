using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les dernières bêtes du carnet de l'épuisette, en volume, d'après les vraies espèces : méduses et physalie de profil,
    /// ver plat et sangsue, tortues, grenouille et limule vus de dessus, lamproie de profil. Dessins originaux.
    /// </summary>
    public static class OtherNetArt
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
            "stomolophus-meleagris", "chrysaora-pacifica", "cyanea-lamarckii", "physalia-physalis", "pseudobiceros-bedfordi",
            "haementeria-ghilianii", "chelonia-mydas", "chelus-fimbriata", "pelophylax-ridibundus", "tachypleus-tridentatus", "lampetra-planeri",
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
                case "stomolophus-meleagris": return Cannonball();
                case "chrysaora-pacifica": return SeaNettle();
                case "cyanea-lamarckii": return BlueJelly();
                case "physalia-physalis": return ManOWar();
                case "pseudobiceros-bedfordi": return Flatworm();
                case "haementeria-ghilianii": return Leech();
                case "chelonia-mydas": return GreenTurtle();
                case "chelus-fimbriata": return Matamata();
                case "pelophylax-ridibundus": return Frog();
                case "tachypleus-tridentatus": return HorseshoeCrab();
                case "lampetra-planeri": return Lamprey();
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

        /// <summary>Un filament qui ondule vers le bas (tentacule de méduse).</summary>
        private static void Strand(VolumeCanvas v, Vector2 root, float len, float r, Color color, float phase, float sway = 3f)
        {
            Vector2 p = root;
            int steps = Mathf.Max(4, (int)(len / 4f));
            for (int k = 0; k < steps; k++)
            {
                float t = (k + 1f) / steps;
                var q = new Vector2(root.x + Mathf.Sin(t * 6f + phase) * sway * t, root.y + len * t);
                v.Piece(Capsule(p.x, p.y, q.x, q.y, Mathf.Lerp(r, r * 0.6f, t)), color, 1f, null, 0.9f, 0.8f);
                p = q;
            }
        }

        /// <summary>Ombrelle de méduse vue de profil : un dôme, plus ou moins haut, au bord festonné.</summary>
        private static Func<float, float, bool> Bell(float cx, float top, float rx, float height, int lappets)
            => (x, y) =>
            {
                float u = (x - cx) / rx;
                if (Mathf.Abs(u) > 1f) return false;
                float rim = top + height + Mathf.Abs(Mathf.Sin(u * lappets * Mathf.PI * 0.5f)) * 2.5f;
                return y >= top + height * (1f - Mathf.Sqrt(1f - u * u)) && y <= rim;
            };

        // ---------- Méduses ----------

        /// <summary>Méduse boulet de canon : dôme rond et ferme, crème bordé de brun, bras buccaux courts et serrés, pas de tentacules.</summary>
        private static Color[,] Cannonball()
        {
            var v = new VolumeCanvas(84, 92);
            for (int i = 0; i < 6; i++)
            {
                float x = 29f + i * 5.2f;
                v.Piece(Taper(new Vector2(x, 50f), new Vector2(42f + (x - 42f) * 0.6f, 66f + Hash(i, 1, 2) * 8f), 2.4f, 1.3f), C(176, 132, 96), 2f,
                    (px, py, c) => (px + py) % 4 == 0 ? Darker(c, 0.88f) : c, 0.85f);
            }

            v.Piece(Bell(42f, 6f, 30f, 46f, 0), C(226, 214, 190), 16f, (x, y, c) =>
            {
                float rim = Mathf.InverseLerp(36f, 52f, y);
                Color col = Color.Lerp(c, C(150, 100, 70), rim * 0.8f);   // le bas de l'ombrelle brunit
                return Hash(x / 2, y / 2, 3) > 0.85f ? Darker(col, 0.92f) : col;
            });
            return v.Finish(C(90, 70, 50), 0.5f);
        }

        /// <summary>Méduse ortie japonaise : ombrelle orangée rayée de bandes rouge-brun, longs tentacules roux, bras buccaux en ruban.</summary>
        private static Color[,] SeaNettle()
        {
            var v = new VolumeCanvas(96, 150);
            for (int i = 0; i < 12; i++)
            {
                Strand(v, new Vector2(16f + i * 5.8f, 40f), 70f + Hash(i, 2, 3) * 40f, 0.9f, C(200, 90, 50), i * 0.9f, 4f);
            }

            // Les bras buccaux : de larges rubans clairs et froncés, au milieu.
            foreach (int s in new[] { -1, 0, 1 })
            {
                Vector2 p = new Vector2(48f + s * 6f, 38f);
                for (int k = 0; k < 12; k++)
                {
                    var q = new Vector2(48f + s * 6f + Mathf.Sin(k * 0.7f + s) * 5f, 38f + (k + 1) * 6f);
                    v.Piece(Capsule(p.x, p.y, q.x, q.y, 3.2f - k * 0.12f), C(246, 226, 210), 2f, (x, y, c) => (y % 3 == 0) ? Darker(c, 0.9f) : c, 0.85f);
                    p = q;
                }
            }

            v.Piece(Bell(48f, 6f, 36f, 32f, 16), C(236, 150, 70), 10f, (x, y, c) =>
            {
                float a = Mathf.Atan2(y - 40f, x - 48f);
                return Mathf.Sin(a * 16f) > 0.45f ? C(170, 60, 40) : c;   // les seize bandes rayonnantes
            });
            return v.Finish(C(110, 40, 24), 0.5f);
        }

        /// <summary>Méduse bleue : ombrelle bleu lavande, une masse de fins tentacules comme une chevelure, bras buccaux froncés.</summary>
        private static Color[,] BlueJelly()
        {
            var v = new VolumeCanvas(96, 140);
            for (int i = 0; i < 26; i++)
            {
                Strand(v, new Vector2(14f + i * 2.7f, 36f), 50f + Hash(i, 3, 4) * 50f, 0.6f, i % 2 == 0 ? C(170, 176, 220) : C(140, 146, 200), i * 0.6f, 2.5f);
            }

            v.Piece(Union(Ellipse(48f, 52f, 10f, 14f), Ellipse(40f, 60f, 6f, 10f), Ellipse(56f, 60f, 6f, 10f)), C(160, 150, 210), 4f,
                (x, y, c) => Mathf.Sin(x * 0.9f + y * 0.6f) > 0.6f ? Darker(c, 0.85f) : c, 0.85f);   // bras buccaux froncés
            v.Piece(Bell(48f, 6f, 36f, 30f, 8), C(110, 130, 210), 10f, (x, y, c) =>
            {
                float a = Mathf.Atan2(y - 38f, x - 48f);
                return Mathf.Sin(a * 8f) > 0.7f ? C(80, 96, 180) : Hash(x, y, 5) > 0.9f ? C(170, 186, 240) : c;
            });
            return v.Finish(C(40, 44, 100), 0.5f);
        }

        /// <summary>Physalie : flotteur translucide bleu et rose, crête plissée sur le dessus, longs filaments bleus qui pendent.</summary>
        private static Color[,] ManOWar()
        {
            var v = new VolumeCanvas(110, 150);
            for (int i = 0; i < 10; i++)
            {
                Strand(v, new Vector2(36f + i * 3.4f, 44f), 50f + Hash(i, 4, 5) * 50f, 1f, i % 3 == 0 ? C(60, 70, 200) : C(90, 120, 220), i * 1.3f, 5f);
            }

            // Le flotteur : une bulle allongée, bleue en bas, rose en haut.
            v.Piece((x, y) =>
            {
                float u = (x - 54f) / 40f, t = (y - 34f) / 14f;
                return u * u + t * t <= 1f - u * 0.1f;
            }, C(150, 170, 240), 10f, (x, y, c) => Color.Lerp(C(110, 140, 236), C(236, 170, 220), Mathf.InverseLerp(44f, 22f, y)));
            // La crête plissée, rose et pourpre.
            v.Piece((x, y) => x > 24f && x < 86f && y < 26f && y > 26f - 12f * Mathf.Sin((x - 24f) / 62f * Mathf.PI) + Mathf.Sin(x * 0.8f) * 1.2f,
                C(210, 120, 200), 3f, (x, y, c) => x % 4 == 0 ? Darker(c, 0.85f) : c, 0.85f);
            return v.Finish(C(50, 50, 120), 0.45f);
        }

        // ---------- Vers ----------

        /// <summary>Ver plat tapis persan : feuille ovale au bord ondulé, brun sombre semé de points jaune orangé et de traits roses.</summary>
        private static Color[,] Flatworm()
        {
            var v = new VolumeCanvas(96, 70);
            Func<float, float, float> radius = (x, y) =>
            {
                float u = (x - 48f) / 42f, t = (y - 35f) / 26f;
                float a = Mathf.Atan2(t, u);
                return Mathf.Sqrt(u * u + t * t) / (1f + Mathf.Sin(a * 11f) * 0.05f + Mathf.Sin(a * 4f + 1f) * 0.04f);
            };
            v.Piece((x, y) => radius(x, y) <= 1f, C(70, 40, 30), 3f, (x, y, c) =>
            {
                if (radius(x, y) > 0.9f) return C(240, 150, 110);                           // liseré orangé
                if (Hash(x, y, 6) > 0.9f) return C(250, 200, 60);                            // points jaunes
                if (Mathf.Abs(Mathf.Sin(x * 0.45f + Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 4f)) < 0.12f) return C(236, 130, 160);   // traits roses
                return Mathf.PerlinNoise(x * 0.15f, y * 0.15f) > 0.65f ? C(40, 22, 20) : c;
            });
            // Les deux petits pseudo-tentacules à l'avant (à droite).
            foreach (int s in new[] { -1, 1 }) v.Piece(Ellipse(86f, 35f + s * 4f, 3f, 2.2f), C(90, 50, 36), 2f);
            return v.Finish(C(30, 16, 12), 0.5f);
        }

        /// <summary>Sangsue géante d'Amazonie : corps long, aplati, finement annelé, gris-brun ; une ventouse à chaque bout.</summary>
        private static Color[,] Leech()
        {
            var v = new VolumeCanvas(140, 44);
            Func<float, float> mid = x => 22f + Mathf.Sin(x * 0.05f) * 4f;
            v.Piece((x, y) =>
            {
                if (x < 8f || x > 132f) return false;
                float t = (x - 8f) / 124f;
                float half = 3f + 10f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI * 0.95f + 0.1f);
                return Mathf.Abs(y - mid(x)) <= half;
            }, C(96, 84, 70), 7f, (x, y, c) =>
            {
                if (x % 3 == 0) return Darker(c, 0.85f);                                     // les anneaux
                return Mathf.Abs(y - mid(x)) < 1.5f ? C(140, 120, 90) : Hash(x, y, 7) > 0.93f ? C(150, 130, 100) : c;
            });
            v.Piece(Ellipse(130f, mid(130f), 6f, 7f), C(110, 94, 76), 4f);   // la ventouse arrière
            return v.Finish(C(30, 24, 20), 0.5f);
        }

        // ---------- Tortues, grenouille, limule (vus de dessus, tête en haut) ----------

        /// <summary>Tortue verte : carapace ovale en écailles brun olive rayonnées, grandes nageoires avant, tête écailleuse.</summary>
        private static Color[,] GreenTurtle()
        {
            var v = new VolumeCanvas(130, 130);
            Color skin = C(110, 104, 70), shell = C(118, 98, 56);
            // Nageoires : grandes devant, petites derrière.
            foreach (int s in new[] { -1, 1 })
            {
                v.Piece(Taper(new Vector2(65f + s * 18f, 46f), new Vector2(65f + s * 56f, 64f), 8f, 4f), skin, 5f, (x, y, c) => Hash(x / 3, y / 3, 8) > 0.6f ? C(150, 144, 110) : c);
                v.Piece(Taper(new Vector2(65f + s * 16f, 92f), new Vector2(65f + s * 30f, 108f), 6f, 3f), skin, 4f);
            }

            v.Piece(Ellipse(65f, 22f, 9f, 11f), skin, 6f, (x, y, c) => Hash(x / 3, y / 3, 9) > 0.55f ? C(150, 144, 110) : c);   // la tête
            foreach (int s in new[] { -1, 1 }) v.Piece(Ellipse(65f + s * 6f, 18f, 1.8f, 2.4f), C(30, 26, 20), 1f);
            // La carapace et ses écailles : cinq vertébrales au milieu, quatre costales de chaque côté, rayonnées.
            // Chaque écaille a son centre ; on prend la plus proche (avec un peu de bruit, pour des contours irréguliers).
            var scutes = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < 5; i++) scutes.Add(new Vector2(65f, 40f + i * 14f));
            for (int i = 0; i < 4; i++) { scutes.Add(new Vector2(45f, 47f + i * 15f)); scutes.Add(new Vector2(85f, 47f + i * 15f)); }
            v.Piece(Ellipse(65f, 70f, 30f, 40f), shell, 14f, (x, y, c) =>
            {
                var p = new Vector2(x + (Mathf.PerlinNoise(x * 0.15f, y * 0.15f) - 0.5f) * 5f, y + (Mathf.PerlinNoise(y * 0.15f, x * 0.15f) - 0.5f) * 5f);
                float d1 = float.MaxValue, d2 = float.MaxValue; Vector2 own = p;
                foreach (Vector2 s in scutes)
                {
                    float d = Vector2.Distance(p, s);
                    if (d < d1) { d2 = d1; d1 = d; own = s; } else if (d < d2) d2 = d;
                }

                if (d2 - d1 < 2.2f) return C(56, 44, 26);                                   // la jointure entre deux écailles
                // Les rayures claires et sombres qui partent de l'arrière de chaque écaille, irrégulières.
                float a = Mathf.Atan2(p.y - own.y - 6f, p.x - own.x);
                float ray = Mathf.Sin(a * 9f + Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 3f);
                Color col = ray > 0.55f ? C(176, 146, 84) : ray < -0.6f ? C(84, 64, 36) : c;
                return Color.Lerp(col, C(70, 56, 32), Mathf.Clamp01(d1 / 14f - 0.4f) * 0.5f);   // plus sombre vers les bords de l'écaille
            });
            return v.Finish(C(40, 34, 20), 0.5f);
        }

        /// <summary>Matamata : carapace plate et rugueuse à trois crêtes bosselées, tête triangulaire aplatie à franges, nez en tuba.</summary>
        private static Color[,] Matamata()
        {
            var v = new VolumeCanvas(110, 140);
            Color brown = C(100, 82, 60);
            foreach (int s in new[] { -1, 1 })
            {
                v.Piece(Taper(new Vector2(55f + s * 22f, 70f), new Vector2(55f + s * 36f, 82f), 6f, 4f), brown, 4f);
                v.Piece(Taper(new Vector2(55f + s * 20f, 112f), new Vector2(55f + s * 30f, 124f), 6f, 4f), brown, 4f);
            }

            // La tête triangulaire et plate, bordée de franges de peau, et le long nez en tuba.
            v.Piece((x, y) => y > 12f && y < 46f && Mathf.Abs(x - 55f) < (y - 12f) * 0.5f + 2f, C(120, 96, 66), 5f,
                (x, y, c) => Hash(x, y, 10) > 0.8f ? Darker(c, 0.8f) : c);
            for (int i = 0; i < 6; i++)
            {
                foreach (int s in new[] { -1, 1 })
                {
                    float y = 20f + i * 4.5f, half = (y - 12f) * 0.5f + 2f;
                    v.Piece(Taper(new Vector2(55f + s * half, y), new Vector2(55f + s * (half + 5f), y + 2f), 1.6f, 0.6f), C(140, 116, 84), 1f);
                }
            }

            v.Piece(Capsule(55f, 14f, 55f, 4f, 1.6f), C(110, 90, 62), 1f);
            foreach (int s in new[] { -1, 1 }) v.Piece(Ellipse(55f + s * 4f, 22f, 1.4f, 1.4f), C(20, 18, 14), 1f);
            // La carapace et ses trois crêtes bosselées.
            v.Piece(Taper(new Vector2(55f, 42f), new Vector2(55f, 58f), 7f, 9f), C(120, 96, 66), 5f, (x, y, c) => Hash(x, y, 13) > 0.85f ? Darker(c, 0.85f) : c);   // le cou
            v.Piece(Ellipse(55f, 92f, 32f, 42f), brown, 8f, (x, y, c) =>
                Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 0.6f + Mathf.PerlinNoise(x * 0.4f, y * 0.4f) * 0.4f > 0.56f ? C(76, 62, 44) : Hash(x, y, 11) > 0.9f ? C(130, 110, 80) : c);
            // Les trois crêtes : des rangées de bosses rugueuses.
            foreach (float k in new[] { 40f, 55f, 70f })
            {
                for (float y = 62f; y < 126f; y += 8f)
                {
                    float ky = y + Hash((int)k, (int)y, 14) * 2f;
                    float u = (k - 55f) / 32f, t = (ky - 92f) / 42f;
                    if (u * u + t * t > 0.85f) continue;
                    v.Piece(Ellipse(k, ky, 3.2f, 3.6f), C(136, 112, 80), 3f, (x, yy, c) => Hash(x, yy, 15) > 0.7f ? Darker(c, 0.85f) : c);
                }
            }
            return v.Finish(C(34, 26, 18), 0.5f);
        }

        /// <summary>Grenouille rieuse, assise, vue de dessus : vert olive taché de sombre, ligne claire sur le dos, pattes repliées, yeux saillants.</summary>
        private static Color[,] Frog()
        {
            var v = new VolumeCanvas(100, 96);
            Color skin = C(110, 140, 70);
            Func<int, int, Color, Color> spots = (x, y, c) => Mathf.PerlinNoise(x * 0.22f, y * 0.22f) > 0.64f ? C(60, 70, 40) : c;
            foreach (int s in new[] { -1, 1 })
            {
                // Patte arrière repliée en Z le long du corps, avec son pied palmé.
                v.Piece(Capsule(50f + s * 14f, 70f, 50f + s * 30f, 58f, 6f), skin, 5f, spots);
                v.Piece(Capsule(50f + s * 30f, 58f, 50f + s * 22f, 86f, 4.5f), skin, 4f, spots);
                v.Piece(Ellipse(50f + s * 28f, 88f, 7f, 4f), C(130, 150, 80), 3f);
                // Patte avant.
                v.Piece(Capsule(50f + s * 12f, 38f, 50f + s * 22f, 30f, 3f), skin, 3f, spots);
            }

            v.Piece(Union(Ellipse(50f, 54f, 17f, 22f), Ellipse(50f, 30f, 14f, 12f)), skin, 10f, (x, y, c) =>
                Mathf.Abs(x - 50) < 1 && y > 26 ? C(180, 200, 120) : spots(x, y, c));                   // la ligne claire du dos
            foreach (int s in new[] { -1, 1 })
            {
                v.Piece(Ellipse(50f + s * 8f, 26f, 4.6f, 4.6f), C(170, 150, 60), 3f);
                v.Piece(Box(50f + s * 8f - 3f, 25.3f, 50f + s * 8f + 3f, 26.7f), C(20, 18, 14), 1f);   // pupille horizontale
            }

            return v.Finish(C(34, 44, 20), 0.5f);
        }

        /// <summary>Limule : bouclier en fer à cheval lisse brun olive, abdomen hérissé d'épines sur les bords, longue queue en épine.</summary>
        private static Color[,] HorseshoeCrab()
        {
            var v = new VolumeCanvas(100, 150);
            Color shell = C(110, 96, 64);
            v.Piece(Taper(new Vector2(50f, 92f), new Vector2(50f, 146f), 3f, 1f), C(96, 82, 56), 2f);   // la queue
            // L'abdomen, hexagonal, avec ses épines mobiles sur les côtés.
            v.Piece((x, y) => y > 56f && y < 94f && Mathf.Abs(x - 50f) < 26f - (y - 56f) * 0.45f, shell, 8f, (x, y, c) => Hash(x, y, 12) > 0.9f ? Darker(c, 0.85f) : c);
            for (int i = 0; i < 6; i++)
            {
                foreach (int s in new[] { -1, 1 })
                {
                    float y = 60f + i * 5.5f, half = 26f - (y - 56f) * 0.45f;
                    v.Piece(Taper(new Vector2(50f + s * half, y), new Vector2(50f + s * (half + 5f), y + 2f), 1.4f, 0.5f), C(90, 76, 50), 1f);
                }
            }

            // Le bouclier en fer à cheval, avec sa crête au milieu et ses deux yeux composés.
            v.Piece((x, y) => { float u = (x - 50f) / 44f, t = (y - 50f) / 44f; return u * u + t * t <= 1f && y < 60f; }, shell, 14f, (x, y, c) =>
            {
                if (Mathf.Abs(x - 50) < 1 && y > 14) return Darker(c, 0.8f);
                if (Mathf.Abs(Mathf.Abs(x - 50f) - 18f) < 0.8f && y > 26) return Darker(c, 0.86f);   // les deux crêtes latérales
                return Mathf.PerlinNoise(x * 0.1f, y * 0.1f) > 0.62f ? C(130, 116, 80) : c;
            });
            foreach (int s in new[] { -1, 1 }) v.Piece(Ellipse(50f + s * 20f, 34f, 2.6f, 1.6f), C(40, 34, 24), 1.5f);
            return v.Finish(C(40, 32, 20), 0.5f);
        }

        // ---------- Lamproie (de profil, tête à droite) ----------

        /// <summary>Lamproie de Planer : petit corps d'anguille gris brun dessus, pâle dessous, bouche en ventouse, sept trous de branchies.</summary>
        private static Color[,] Lamprey()
        {
            var v = new VolumeCanvas(140, 30);
            v.Piece((x, y) =>
            {
                if (x < 4f || x > 134f) return false;
                float t = (x - 4f) / 130f;
                float half = 2f + 7f * Mathf.Sqrt(Mathf.Clamp01(Mathf.Min(t * 3f, (1f - t) * 8f)));
                return Mathf.Abs(y - 15f) <= half;
            }, C(130, 120, 100), 5f, (x, y, c) => Color.Lerp(C(110, 100, 84), C(214, 206, 184), Mathf.InverseLerp(12f, 21f, y)));
            // Les deux nageoires dorsales, basses, vers l'arrière.
            v.Piece((x, y) => x > 20f && x < 72f && y < 8f && y > 8f - 3.5f * Mathf.Sin((x - 20f) / 52f * Mathf.PI) * (x < 46f ? 1f : 0.8f), C(150, 136, 110), 2f);
            // Les sept trous des branchies, alignés derrière l'œil.
            for (int i = 0; i < 7; i++) v.Piece(Ellipse(110f - i * 3.6f, 15f, 0.9f, 1.1f), C(40, 36, 30), 1f);
            v.Piece(Ellipse(122f, 12f, 1.8f, 1.8f), C(30, 28, 24), 1f);                     // l'œil
            v.Piece(Ellipse(134f, 15f, 2.6f, 5f), C(180, 150, 120), 2f, (x, y, c) => (y % 2 == 0) ? Darker(c, 0.85f) : c);   // la ventouse
            return v.Finish(C(40, 34, 26), 0.5f);
        }
    }
}
