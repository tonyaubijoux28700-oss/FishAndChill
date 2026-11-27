using System;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les mollusques du carnet de l'épuisette, en volume, d'après les vraies espèces : coquillages à deux valves vus de
    /// dessus (stries de croissance, côtes, charnière ; manteau coloré des bénitiers), escargots de mer de profil (spire,
    /// tours, ouverture, sculpture et motif), limaces de mer vues de dessus. Dessins originaux.
    /// </summary>
    public static class MolluscArt
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
            "mytilus-edulis", "perna-perna", "dreissena-polymorpha", "anodonta-anatina", "ensis-magnus", "magallana-gigas", "pecten-maximus",
            "buccinum-undatum", "conus-textile", "pomacea-bridgesii", "chromodoris-annae", "glaucus-atlanticus",
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
                case "mytilus-edulis": return Mussel(C(36, 40, 70), C(80, 70, 130), C(20, 22, 40), 19f, 64f, 0.9f);
                case "perna-perna": return Mussel(C(96, 66, 40), C(130, 120, 60), C(46, 30, 20), 21f, 66f, 0.95f);
                case "dreissena-polymorpha": return ZebraMussel();
                case "anodonta-anatina": return Anodonta();
                case "ensis-magnus": return RazorClam();
                case "magallana-gigas": return Oyster();
                case "pecten-maximus": return Scallop();
                case "tridacna-maxima": return GiantClam(false);
                case "tridacna-gigas": return GiantClam(true);
                case "buccinum-undatum": return Whelk();
                case "conus-textile": return Cone();
                case "pomacea-bridgesii": return AppleSnail();
                case "chromodoris-annae": return Nudibranch();
                case "glaucus-atlanticus": return BlueDragon();
                default: return null;
            }
        }

        // ---------- Coquillages à deux valves (vus de dessus, charnière en haut) ----------

        /// <summary>Stries de croissance : des lignes concentriques autour du sommet (umbo).</summary>
        private static Func<int, int, Color, Color> Growth(Vector2 umbo, float spacing, float strength, Func<int, int, Color, Color> then = null)
            => (x, y, c) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), umbo);
                Color baseColor = then != null ? then(x, y, c) : c;
                float f = d / spacing - Mathf.Floor(d / spacing);
                return f < 0.16f ? Darker(baseColor, 1f - strength) : f > 0.86f ? Darker(baseColor, 1f + strength * 0.6f) : baseColor;
            };

        /// <summary>Forme effilée : tous les points à moins d'un rayon qui grandit de r0 à r1 le long du segment.</summary>
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

        /// <summary>
        /// Moule : longue coquille fuselée, sommet pointu en haut, l'arrière large et arrondi ; lustrée le long de l'arête,
        /// fines stries de croissance concentriques.
        /// </summary>
        private static Color[,] Mussel(Color shell, Color sheen, Color outline, float width, float length, float curve)
        {
            int w = (int)(width * 1.6f) + 12, h = (int)length + 10;
            var v = new VolumeCanvas(w, h);
            float left = 6f, top = 5f;
            var umbo = new Vector2(left + 1f, top);
            // Le bord ventral (à gauche) presque droit ; le bord dorsal (à droite) bombé ; l'arrière (en bas) large et arrondi.
            Func<float, float, bool> shape = (x, y) =>
            {
                float t = (y - top) / length;
                if (t < 0f || t > 1f) return false;
                float round = t > 0.72f ? Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((t - 0.72f) / 0.28f, 2f))) : 1f;   // arrière bien rond
                float half = width * 0.5f * Mathf.Sin(Mathf.Min(1f, t * 1.4f) * Mathf.PI * 0.5f) * round + 0.8f;
                float line = left + width * 0.2f + t * width * 0.3f * curve;
                float bulge = width * 0.22f * Mathf.Sin(t * Mathf.PI) * round;          // le dos bombé d'un seul côté
                return x >= line - half && x <= line + half + bulge;
            };
            v.Piece(shape, shell, width * 0.45f, Growth(umbo, 3.2f, 0.14f, (x, y, c) =>
            {
                // Reflet lustré le long de l'arête, près du bord ventral.
                float t = (y - top) / length;
                float ridge = left + width * 0.2f + t * width * 0.3f * curve - width * 0.15f * Mathf.Sin(Mathf.Min(1f, t * 1.4f) * Mathf.PI * 0.5f);
                return Mathf.Abs(x - ridge) < 2f ? Color.Lerp(c, sheen, 0.55f) : c;
            }));
            return v.Finish(outline, 0.6f);
        }

        /// <summary>
        /// Contour organique d'une valve, défini par son bord en coordonnées polaires autour du sommet (umbo) : renvoie, pour
        /// un point, sa « profondeur » (0 au sommet, 1 sur le bord). Sert à la forme et aux cernes de croissance, qui
        /// suivent ainsi le contour au lieu de faire des cercles.
        /// </summary>
        private static Func<float, float, float> Depth(Vector2 umbo, Func<float, float> edge)
            => (x, y) =>
            {
                Vector2 d = new Vector2(x, y) - umbo;
                float r = d.magnitude;
                if (r < 0.01f) return 0f;
                return r / Mathf.Max(0.5f, edge(Mathf.Atan2(d.y, d.x)));
            };

        /// <summary>
        /// Moule zébrée : petite coquille de moule (pointe en haut, un flanc plat, l'autre bombé), crème jaunâtre, rayée de
        /// chevrons bruns irréguliers qui suivent les stries de croissance.
        /// </summary>
        private static Color[,] ZebraMussel()
        {
            var v = new VolumeCanvas(56, 66);
            var umbo = new Vector2(22f, 7f);
            // Le bord : court vers le haut (le sommet), long vers le bas, plat à gauche, bombé à droite.
            Func<float, float> edge = a =>
            {
                // Une ellipse par quart : long vers le bas, bombé à droite, plat à gauche, presque rien au-dessus du sommet.
                float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
                float rx = cos > 0f ? 22f : 11f, ry = sin > 0f ? 54f : 5f;
                return 1f / Mathf.Sqrt((cos / rx) * (cos / rx) + (sin / ry) * (sin / ry));
            };
            Func<float, float, float> depth = Depth(umbo, edge);
            Color cream = C(222, 204, 150), brown = C(92, 62, 36);
            v.Piece((x, y) => depth(x, y) <= 1f && y > 3f, cream, 9f, (x, y, c) =>
            {
                float t = depth(x, y);
                float side = Mathf.Atan2(y - umbo.y, x - umbo.x);
                // Les chevrons : des bandes de croissance ondulées en zigzag, d'épaisseur irrégulière.
                float zig = Mathf.Abs(Mathf.Repeat((x - umbo.x) * 0.22f, 1f) - 0.5f) * 0.16f;   // chevrons en travers
                float band = (t + zig) * 7f + Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.5f;
                float f = band - Mathf.Floor(band);
                Color col = f < 0.42f ? brown : c;
                if (t > 0.93f) col = Darker(col, 0.85f);    // le bord un peu plus sombre
                return Hash(x, y, 3) > 0.93f ? Darker(col, 0.9f) : col;
            });
            // Dessinée sommet en bas : on la retourne pour avoir la pointe en haut, comme les autres moules.
            Color[,] p = v.Finish(C(54, 38, 22), 0.55f);
            int w = p.GetLength(0), h = p.GetLength(1);
            var flipped = new Color[w, h];
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) flipped[x, h - 1 - y] = p[x, y];
            return flipped;
        }

        /// <summary>
        /// Anodonte : grande moule d'eau douce, ovale et asymétrique (l'avant court et arrondi, l'arrière allongé, le dos qui
        /// remonte en aile), brun vert plus sombre vers le sommet, cernes de croissance irréguliers et lustre.
        /// </summary>
        private static Color[,] Anodonta()
        {
            var v = new VolumeCanvas(108, 64);
            var umbo = new Vector2(36f, 18f);
            Func<float, float> edge = a =>
            {
                // Une ellipse par quart : l'arrière (à droite) bien plus long que l'avant, le dos (en haut) près du sommet
                // avec l'aile qui remonte vers l'arrière, le ventre (en bas) bombé.
                float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
                float rx = cos > 0f ? 66f : 32f;
                float ry = sin > 0f ? 40f : 8f + 10f * Mathf.Max(0f, cos);
                return 1f / Mathf.Sqrt((cos / rx) * (cos / rx) + (sin / ry) * (sin / ry)) * (1f + Mathf.Sin(a * 3f + 1f) * 0.02f);
            };
            Func<float, float, float> depth = Depth(umbo, edge);
            v.Piece((x, y) => depth(x, y) <= 1f, C(112, 112, 60), 13f, (x, y, c) =>
            {
                float t = depth(x, y);
                // Couleur : brun sombre au sommet, vert olive puis jaune verdâtre vers le bord ventral.
                Color col = Color.Lerp(C(90, 70, 40), C(130, 132, 64), Mathf.Clamp01(t * 1.4f));
                if (y > 40 && t > 0.7f) col = Color.Lerp(col, C(170, 160, 80), 0.4f);
                // Cernes de croissance, irréguliers (épaisseur et teinte variables).
                float band = t * 11f + Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 1.2f;
                float f = band - Mathf.Floor(band);
                if (f < 0.12f) col = Darker(col, 0.78f);
                else if (Hash((int)band, 0, 4) > 0.6f && f < 0.5f) col = Darker(col, 0.9f);
                // Un lustre doux en travers.
                if (Mathf.Abs((y - 28f) + (x - 52f) * 0.25f) < 3f && t > 0.25f && t < 0.9f) col = Color.Lerp(col, C(200, 196, 140), 0.3f);
                return col;
            });
            return v.Finish(C(46, 42, 22), 0.55f);
        }

        /// <summary>Couteau : long étui étroit, à peine courbé, rayé de bandes olive et brunes.</summary>
        private static Color[,] RazorClam()
        {
            var v = new VolumeCanvas(128, 34);
            Color olive = C(150, 128, 76), brown = C(104, 80, 50), pale = C(214, 196, 150);
            v.Piece((x, y) => x > 4f && x < 124f && Mathf.Abs(y - 17f - (x - 64f) * (x - 64f) * 0.0007f) < 10.5f, olive, 7f, (x, y, c) =>
            {
                float rel = y - 17f - (x - 64f) * (x - 64f) * 0.0007f;
                // Les stries de croissance : des arcs qui partent du sommet (à gauche) et traversent la coquille.
                float arc = x + rel * rel * 0.35f;
                float f = arc / 7f - Mathf.Floor(arc / 7f);
                Color col = rel < -3f ? pale : rel > 5f ? brown : c;   // une moitié claire, l'autre brune, séparées en diagonale douce
                return f < 0.25f ? Darker(col, 0.7f) : col;
            });
            return v.Finish(C(60, 46, 28), 0.6f);
        }

        /// <summary>Huître creuse : coquille épaisse et irrégulière, bord feuilleté en lames, gris clair veiné de violet.</summary>
        private static Color[,] Oyster()
        {
            var v = new VolumeCanvas(64, 96);
            Color shell = C(184, 178, 166);
            var umbo = new Vector2(30f, 8f);
            Func<float, float, bool> body = Taper(umbo, new Vector2(34f, 66f), 6f, 25f);
            // Bord irrégulier, feuilleté : on grignote la forme avec un bruit.
            v.Piece((x, y) => body(x, y) && Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 6f < 6f - (body(x + 3f, y) && body(x - 3f, y) && body(x, y + 3f) ? 0f : 3.5f), shell, 11f, (x, y, c) =>
            {
                // Les lames de croissance, larges et ondulées, empilées depuis le sommet.
                float r = Vector2.Distance(new Vector2(x, y), umbo) + Mathf.Sin(x * 0.45f) * 1.8f;
                float f = r / 6f - Mathf.Floor(r / 6f);
                if (f < 0.18f) return Darker(c, 0.78f);                                   // l'ombre sous chaque lame
                if (f > 0.8f) return C(226, 222, 214);                                    // le bord clair de la lame
                return Mathf.PerlinNoise(x * 0.15f + 5f, y * 0.08f) > 0.66f ? C(130, 100, 130) : c;   // veines violettes
            });
            return v.Finish(C(64, 58, 60), 0.6f);
        }

        /// <summary>Coquille Saint-Jacques : éventail à larges côtes rayonnantes, deux oreilles à la charnière, rouge orangé.</summary>
        private static Color[,] Scallop()
        {
            var v = new VolumeCanvas(84, 80);
            Color red = C(196, 104, 70), pale = C(236, 190, 150);
            var hinge = new Vector2(42f, 12f);
            // Les deux oreilles de part et d'autre de la charnière.
            v.Piece(Union(Box(26f, 8f, 42f, 18f), Box(42f, 8f, 58f, 18f)), Darker(red, 0.95f), 3f, (x, y, c) => x % 3 == 0 ? Darker(c, 0.9f) : c);
            v.Piece((x, y) =>
            {
                Vector2 d = new Vector2(x, y) - hinge;
                float a = Mathf.Atan2(d.x, d.y);   // 0 vers le bas
                return d.y > 0f && Mathf.Abs(a) < 1.05f && d.magnitude < 62f - Mathf.Abs(a) * 6f;
            }, red, 10f, (x, y, c) =>
            {
                Vector2 d = new Vector2(x, y) - hinge;
                float a = Mathf.Atan2(d.x, d.y);
                float rib = Mathf.Cos(a * 15f);
                Color col = rib > 0.3f ? Darker(c, 1.1f) : rib < -0.6f ? Darker(c, 0.78f) : c;
                float ring = d.magnitude / 9f - Mathf.Floor(d.magnitude / 9f);
                return ring < 0.12f ? pale : col;   // bandes de croissance claires
            });
            return v.Finish(C(80, 34, 22), 0.6f);
        }

        /// <summary>
        /// Bénitier vu de dessus, comme posé dans le corail : la coquille allongée au bord découpé en grosses dents (les
        /// festons des côtes), et par-dessus le manteau, largement étalé, qui DÉBORDE des valves en lobes souples et froncés.
        /// L'ouverture est la fente sinueuse, en zigzag, où se rejoignent les deux lèvres du manteau ; le siphon inhalant
        /// (frangé) s'ouvre dedans, le petit siphon exhalant plus loin. On ne voit la coquille qu'aux deux bouts et entre les lobes.
        /// Bénitier allongé : coquille écailleuse, manteau bleu et turquoise. Bénitier géant : énormes plis, manteau brun doré
        /// aux reflets bleu-vert.
        /// </summary>
        private static Color[,] GiantClam(bool giant)
        {
            int w = giant ? 156 : 128, h = giant ? 96 : 76;
            var v = new VolumeCanvas(w, h);
            float cx = w * 0.5f, cy = h * 0.5f, rx = w * 0.46f, ry = h * 0.3f;
            float k = giant ? 0.14f : 0.22f;   // fréquence des côtes, des festons et des lobes

            // La coquille : allongée, le bord découpé en grosses dents arrondies.
            Color shell = giant ? C(206, 198, 176) : C(204, 198, 182);
            v.Piece((x, y) =>
            {
                float u = (x - cx) / rx, t = (y - cy) / (ry * (1f + 0.22f * Mathf.Abs(Mathf.Cos(x * k))));
                return u * u + t * t <= 1f;
            }, shell, giant ? 14f : 11f, (x, y, c) =>
            {
                float rib = Mathf.Cos(x * k);
                Color col = rib > 0.5f ? Darker(c, 1.12f) : rib < -0.5f ? Darker(c, 0.74f) : c;
                if (!giant && rib > 0f && Mathf.Repeat(Mathf.Abs(y - cy), 4f) < 1f) col = Darker(col, 0.86f);   // écailles
                return Mathf.PerlinNoise(x * 0.09f + 11f, y * 0.09f) > 0.72f ? Color.Lerp(col, C(140, 156, 110), 0.35f) : col;
            });

            // La fente de l'ouverture, en zigzag arrondi le long du grand axe.
            Func<float, float> slit = x => cy + Mathf.Sin(x * k) * ry * 0.28f;
            // Le manteau : plus large que la coquille au milieu (il déborde), en gros lobes froncés, effilé aux deux bouts.
            Func<float, float, float> mantle = (x, y) =>
            {
                float u = (x - cx) / (rx * 0.88f);
                if (Mathf.Abs(u) >= 1f) return 99f;
                float lobes = 1f + 0.17f * Mathf.Sin(x * k * 0.5f + (y > slit(x) ? 1.6f : 0f)) + (Mathf.PerlinNoise(x * 0.12f, y > slit(x) ? 3f : 0f) - 0.5f) * 0.1f;
                float half = ry * 1.22f * Mathf.Pow(1f - u * u, 0.6f) * lobes;
                return Mathf.Abs(y - slit(x)) / Mathf.Max(0.5f, half);
            };
            Color baseColor = giant ? C(150, 118, 58) : C(36, 108, 196);
            v.Piece((x, y) => mantle(x, y) <= 1f, baseColor, 7f, (x, y, c) =>
            {
                float m = mantle(x, y);
                float streak = Mathf.PerlinNoise(x * 0.07f, y * 0.3f);
                float blot = Mathf.PerlinNoise(x * 0.2f + 7f, y * 0.2f + 3f);
                Color col;
                if (giant)
                {
                    col = Color.Lerp(C(116, 86, 42), C(200, 166, 86), streak);
                    if (blot > 0.64f) col = Color.Lerp(col, C(80, 190, 190), (blot - 0.64f) * 3f);
                }
                else
                {
                    col = Color.Lerp(C(24, 72, 166), C(80, 180, 236), streak);
                    if (blot > 0.62f) col = Color.Lerp(col, C(100, 226, 214), (blot - 0.62f) * 2.8f);
                    if (blot < 0.3f) col = Color.Lerp(col, C(18, 40, 110), (0.3f - blot) * 2.5f);
                }

                // Les lèvres s'enfoncent vers la fente : plus sombres tout près.
                float toSlit = Mathf.Abs(y - slit(x));
                if (toSlit < 1.2f) return giant ? C(50, 34, 18) : C(10, 24, 60);   // la fente elle-même
                if (toSlit < 3.5f) col = Darker(col, 0.8f);

                // La lisière extérieure : plus claire, semée de petits points.
                if (m > 0.82f)
                {
                    col = Color.Lerp(col, giant ? C(214, 196, 126) : C(150, 226, 244), 0.5f);
                    if (Hash(x, y, 11) > 0.75f) col = giant ? C(150, 232, 232) : C(236, 252, 255);
                }

                return col;
            });

            // Le siphon inhalant, ouvert dans la fente, bordé de franges ; le petit siphon exhalant plus loin.
            float sx = cx - rx * 0.24f, sy = slit(sx);
            v.Piece((x, y) => { float u = (x - sx) / (rx * 0.11f), t = (y - sy) / (ry * 0.18f); return u * u + t * t <= 1f; },
                giant ? C(60, 42, 22) : C(10, 26, 66), 3f, (x, y, c) =>
                {
                    float u = (x - sx) / (rx * 0.11f), t = (y - sy) / (ry * 0.18f);
                    return u * u + t * t > 0.5f && (x + y) % 2 == 0 ? (giant ? C(206, 186, 116) : C(170, 226, 244)) : c;
                });
            float ex = cx + rx * 0.4f;
            v.Piece(Ellipse(ex, slit(ex), 3.6f, 3f), giant ? C(84, 60, 30) : C(18, 42, 96), 3f);
            return v.Finish(giant ? C(70, 60, 40) : C(40, 44, 60), 0.55f);
        }

        // ---------- Escargots de mer (de profil, spire en haut, ouverture à droite) ----------

        /// <summary>Bulot : spire haute aux tours renflés et ondulés, cordons en spirale, beige brun, grande ouverture.</summary>
        private static Color[,] Whelk()
        {
            var v = new VolumeCanvas(64, 96);
            Color shell = C(206, 180, 140);
            float apexY = 4f, baseY = 92f;
            Func<float, float, bool> outline = (x, y) =>
            {
                float t = (y - apexY) / (baseY - apexY);
                if (t < 0f || t > 1f) return false;
                float half = Mathf.Lerp(1f, 26f, Mathf.Pow(t, 0.8f)) * (t > 0.82f ? Mathf.Sqrt((1f - t) / 0.18f) : 1f);
                half *= 1f + Mathf.Sin(t * 30f) * 0.06f;   // tours renflés
                return Mathf.Abs(x - 32f) <= half;
            };
            v.Piece(outline, shell, 12f, (x, y, c) =>
            {
                float t = (y - apexY) / (baseY - apexY);
                float suture = Mathf.Sin(t * 30f);
                if (suture < -0.96f && t < 0.72f) return C(120, 94, 64);               // sutures entre les tours
                if (Mathf.Sin(x * 0.9f - y * 0.25f) > 0.7f) return Darker(c, 1.1f);     // côtes ondulées
                return (y % 4 == 0) ? Darker(c, 0.9f) : c;                               // cordons en spirale
            });
            // L'ouverture, grande et ovale, avec son canal en bas.
            v.Piece(Ellipse(40f, 70f, 9f, 15f), C(236, 214, 180), 4f, (x, y, c) => x > 44 ? C(170, 120, 80) : c);
            return v.Finish(C(70, 52, 34), 0.6f);
        }

        /// <summary>Cône textile : cône inversé aux épaules larges, spire basse, couvert de petites « tentes » blanches sur fond doré.</summary>
        private static Color[,] Cone()
        {
            var v = new VolumeCanvas(68, 116);
            Color brown = C(96, 58, 28), gold = C(214, 160, 60), white = C(250, 246, 236);
            v.Piece((x, y) =>
            {
                if (y < 4f || y > 113f) return false;
                if (y < 22f) return Mathf.Abs(x - 34f) <= (y - 4f) * 1.75f;               // la spire basse
                float half = Mathf.Lerp(31f, 2.5f, Mathf.Pow((y - 22f) / 91f, 0.95f));
                return Mathf.Abs(x - 34f) <= half;
            }, brown, 12f, (x, y, c) =>
            {
                // Un réseau serré de petites « tentes » blanches bordées de brun, avec trois bandes dorées en travers.
                bool band = Mathf.Abs(y - 46) < 5 || Mathf.Abs(y - 76) < 4 || Mathf.Abs(y - 98) < 3;
                // Tentes de tailles variées : grandes (8×6) et petites (4×3), sommet en haut, bord brun.
                foreach (int size in new[] { 8, 4 })
                {
                    int h = size * 3 / 4, row = y / h, shift = (row % 2) * size / 2;
                    int lx = (x + shift) % size, ly = y % h;
                    float half = (ly + 0.5f) * size / (2f * h);
                    if (Hash((x + shift) / size, row, size) > (band ? 0.6f : 0.15f) && Mathf.Abs(lx - (size - 1) * 0.5f) <= half)
                    {
                        return Mathf.Abs(lx - (size - 1) * 0.5f) > half - 1f || ly == h - 1 ? C(70, 40, 20) : white;
                    }
                }

                return band ? gold : c;
            });
            return v.Finish(C(50, 30, 14), 0.6f);
        }

        /// <summary>Escargot-pomme : coquille globuleuse, gros dernier tour, spire basse, olive brun à bandes sombres en spirale.</summary>
        private static Color[,] AppleSnail()
        {
            var v = new VolumeCanvas(76, 80);
            Color shell = C(150, 120, 60);
            v.Piece(Union(Ellipse(36f, 46f, 30f, 30f), Ellipse(30f, 18f, 13f, 12f), Ellipse(26f, 8f, 5f, 5f)), shell, 16f, (x, y, c) =>
            {
                float band = Mathf.Sin(y * 0.55f - x * 0.12f);
                return band > 0.85f ? C(70, 54, 30) : Mathf.PerlinNoise(x * 0.1f, y * 0.1f) > 0.6f ? C(176, 146, 80) : c;
            });
            // L'ouverture ronde, et l'opercule sombre qui la ferme.
            v.Piece(Ellipse(52f, 56f, 14f, 18f), C(226, 200, 140), 5f);
            v.Piece(Ellipse(53f, 57f, 10f, 14f), C(70, 56, 36), 4f);
            return v.Finish(C(54, 40, 20), 0.6f);
        }

        // ---------- Limaces de mer (vues de dessus, tête en haut) ----------

        /// <summary>Nudibranche d'Anne : corps bleu électrique bordé de blanc, lignes noires, rhinophores et panache orange.</summary>
        private static Color[,] Nudibranch()
        {
            var v = new VolumeCanvas(60, 104);
            Color blue = C(46, 110, 220), white = C(246, 246, 250), yellow = C(250, 214, 70), orange = C(244, 140, 40);
            // Le manteau : une ellipse à bord ondulé (froncé), effilée devant et derrière.
            Func<float, float, float> radius = (x, y) =>
            {
                float u = (x - 30f) / 20f, t = (y - 52f) / 44f;
                float a = Mathf.Atan2(t, u);
                return Mathf.Sqrt(u * u + t * t) / (1f + Mathf.Sin(a * 16f) * 0.05f);
            };
            v.Piece((x, y) => radius(x, y) <= 1f, white, 6f, (x, y, c) => radius(x, y) > 0.93f ? yellow : c);
            v.Piece((x, y) => radius(x, y) <= 0.8f, blue, 8f, (x, y, c) =>
            {
                int d = Mathf.Abs(x - 30);
                return d == 0 || d == 7 ? C(16, 24, 50) : Hash(x, y, 7) > 0.94f ? C(120, 170, 240) : c;   // lignes noires et reflets
            });
            // Les rhinophores : deux cornes orange sur une base blanche.
            foreach (int s in new[] { -1, 1 })
            {
                v.Piece(Capsule(30f + s * 5f, 18f, 30f + s * 6f, 8f, 2.6f), white, 2f);
                v.Piece(Capsule(30f + s * 6f, 12f, 30f + s * 7f, 6f, 2.4f), orange, 2f, (x, y, c) => y % 2 == 0 ? Darker(c, 0.9f) : c);
            }

            // Le panache des branchies, en couronne de plumes orange et blanches.
            for (int i = 0; i < 9; i++)
            {
                float a = (i * 40f) * Mathf.Deg2Rad;
                var tip = new Vector2(30f + Mathf.Cos(a) * 8f, 80f + Mathf.Sin(a) * 6f);
                v.Piece(Capsule(30f, 80f, tip.x, tip.y, 1.9f), i % 2 == 0 ? orange : white, 1.6f);
            }

            return v.Finish(C(20, 30, 70), 0.6f);
        }

        /// <summary>Dragon bleu : petit corps fuselé rayé de bleu et d'argent, trois paires de bouquets de doigts bleus étalés.</summary>
        private static Color[,] BlueDragon()
        {
            var v = new VolumeCanvas(104, 112);
            Color blue = C(40, 100, 210), pale = C(176, 214, 244), dark = C(20, 50, 130);
            // Les bouquets de doigts (cérates), trois de chaque côté, en éventail.
            float[] ys = { 32f, 52f, 72f };
            float[] len = { 26f, 32f, 22f };
            foreach (int s in new[] { -1, 1 })
            {
                for (int k = 0; k < ys.Length; k++)
                {
                    var root = new Vector2(52f + s * 6f, ys[k]);
                    var stem = root + new Vector2(s * len[k] * 0.45f, -3f);
                    v.Piece(Capsule(root.x, root.y, stem.x, stem.y, 3.2f), blue, 2.5f);
                    for (int f = 0; f < 7; f++)
                    {
                        float a = (s > 0 ? -55f : 235f) + f * (s > 0 ? 18f : -18f);
                        Vector2 end = stem + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * len[k] * 0.6f;
                        v.Piece(Capsule(stem.x, stem.y, end.x, end.y, 2f), f % 2 == 0 ? blue : C(70, 140, 230), 1.8f,
                            (x, y, c) => Vector2.Distance(new Vector2(x, y), end) < 2.5f ? pale : c);
                    }
                }
            }

            v.Piece(Union(Ellipse(52f, 50f, 9f, 34f), Capsule(52f, 80f, 52f, 106f, 3f)), blue, 7f,
                (x, y, c) => Mathf.Abs(x - 52) <= 1 ? pale : Mathf.Abs(x - 52) == 5 ? dark : c);
            foreach (int s in new[] { -1, 1 }) v.Piece(Capsule(52f + s * 3f, 18f, 52f + s * 6f, 10f, 1.4f), blue, 1f);   // petits tentacules
            return v.Finish(C(14, 30, 70), 0.6f);
        }
    }
}
