using System;
using UnityEngine;

namespace FishAndChill.Player
{
    /// <summary>
    /// Dessine le pêcheur en pixel art 16×24 selon son apparence (<see cref="CharacterLook"/>), en jeu comme dans
    /// l'éditeur. Le dessin vient de gabarits faits à la main pixel par pixel (<see cref="CharacterTemplates"/>) :
    /// corps, puis coiffure, chapeau et motifs du haut posés en calques ; chaque rôle prend sa couleur (base, ombre,
    /// lumière), puis un contour sombre teinté et une ombre douce au sol. 3 images par direction (0 = à l'arrêt,
    /// 1 et 2 = pas de marche) ; le profil regarde à droite, la gauche en est le miroir.
    /// Tableaux [x, y] avec y depuis le bas ; pivot aux pieds.
    /// </summary>
    public static class CharacterPainter
    {
        public const int Width = 16;
        public const int Height = 24;

        private static readonly Color StrawHat = new Color(0.95f, 0.82f, 0.50f);
        private static readonly Color HatBand = new Color(0.84f, 0.24f, 0.24f);
        private static readonly Color CapColor = new Color(0.86f, 0.25f, 0.24f);
        private static readonly Color BobColor = new Color(0.70f, 0.72f, 0.54f);
        private static readonly Color BandanaColor = new Color(0.20f, 0.42f, 0.80f);
        private static readonly Color White = new Color(0.98f, 0.97f, 0.93f);
        private static readonly Color Blush = new Color(0.97f, 0.50f, 0.50f);
        private static readonly Color Lips = new Color(0.72f, 0.32f, 0.30f);
        private static readonly Color Eye = new Color(0.13f, 0.09f, 0.11f);
        private static readonly Color Petal = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color PetalPink = new Color(0.98f, 0.45f, 0.58f);
        private static readonly Color StripeWhite = new Color(0.97f, 0.97f, 0.94f);
        private static readonly Color StripeNavy = new Color(0.18f, 0.27f, 0.56f);
        private static readonly Color Sandal = new Color(0.50f, 0.31f, 0.18f);
        private static readonly Color GroundShadow = new Color(0.05f, 0.10f, 0.06f, 0.26f);

        private const int ViewFront = 0, ViewBack = 1, ViewSide = 2;
        private const int TopHawaiian = 0, TopTee = 1, TopTank = 2, TopStriped = 3;

        /// <summary>Image de face (ou de dos).</summary>
        public static Color[,] Front(CharacterLook look, int frame, bool back) => Draw(look, back ? ViewBack : ViewFront, frame);

        /// <summary>Image de profil, regard vers la droite.</summary>
        public static Color[,] Side(CharacterLook look, int frame) => Draw(look, ViewSide, frame);

        private static Color[,] Draw(CharacterLook look, int view, int frame)
        {
            look = CharacterOptions.Sanitize(look);
            frame = Mathf.Clamp(frame, 0, 2);
            string[] bodyRows = view == ViewSide ? CharacterTemplates.BodySide[frame] : CharacterTemplates.BodyFront[frame];
            char[,] body = Parse(bodyRows);
            if (view == ViewBack)
            {
                // De dos : pas de visage (la nuque reste visible sous les cheveux).
                Replace(body, "Emp", 'S');
            }

            var grid = (char[,])body.Clone();
            bool hat = look.Hat != 0;
            foreach (string[] layer in CharacterTemplates.Hair(look.HairStyle, hat)[view])
            {
                Overlay(grid, body, layer, hat ? 3 : 0);
            }

            string[] hatRows = CharacterTemplates.Hat(look.Hat, view);
            if (hatRows != null)
            {
                Overlay(grid, body, hatRows, 0);
            }

            DecorateTop(grid, look.Top, view);
            Color[,] pixels = Colorize(grid, look);
            Outline(pixels);
            AddGroundShadow(pixels);
            return pixels;
        }

        // ---------- Marchand ----------

        private static readonly string[] MerchantRows =
        {
            "................", "................",
            ".....yWWWWw.....", "....yWWWNWWw....", "....NNNNNNNN....", "..yyWWWWWWWWww..", "..wwwwwwwwwwww..",
            "...GSSSSSSSSg...", "...GSgSSSSgSg...", "...GSESSSSESg...", "...SSSSppSSSs...", "...SGGGGGGGGs...",
            "....sGsSSsGs....", "......ssss......",
            "..TTTKKKKKKTtt..", "..TTTKKKKKKTtt..", "..SSTKKKKKKtSs..", "..SSTKKqqKKtSs..",
            "....PKKKKKKQ....", "....PPKKKKQQ....", "....PPQ..PPQ....", ".....PQ..PQ.....", "....OOo..OOo....",
            "................",
        };

        /// <summary>
        /// Marchand de face, dans le style du pêcheur : vieux marin au bob blanc à ruban bleu (petite ancre), cheveux et
        /// sourcils gris, grosse moustache, marinière rayée, tablier crème avec un poisson brodé sur la poche, pantalon, bottes.
        /// </summary>
        public static Color[,] Merchant()
        {
            char[,] grid = Parse(MerchantRows);
            var skin = new Color(0.88f, 0.63f, 0.45f);
            var grey = new Color(0.86f, 0.86f, 0.84f);
            var navy = new Color(0.20f, 0.32f, 0.66f);
            var apron = new Color(0.96f, 0.91f, 0.79f);
            var trousers = new Color(0.36f, 0.30f, 0.27f);
            var boots = new Color(0.32f, 0.21f, 0.14f);
            var pixels = new Color[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    char role = grid[x, y];
                    if (role == '.')
                    {
                        continue;
                    }

                    Color stripe = y % 2 == 0 ? StripeWhite : navy;
                    Color c;
                    switch (role)
                    {
                        case 'S': c = skin; break;
                        case 's': c = Dark(skin); break;
                        case 'E': c = Eye; break;
                        case 'p': c = Color.Lerp(skin, Blush, 0.5f); break;
                        case 'G': c = grey; break;
                        case 'g': c = Dark(grey); break;
                        case 'W': c = White; break;
                        case 'w': c = Dark(White); break;
                        case 'y': c = Bright(White); break;
                        case 'N': c = navy; break;
                        case 'T': c = stripe; break;
                        case 't': c = Dark(stripe); break;
                        case 'K': c = apron; break;
                        case 'q': c = navy; break;
                        case 'P': c = trousers; break;
                        case 'Q': c = Dark(trousers); break;
                        case 'O': c = boots; break;
                        case 'o': c = Dark(boots); break;
                        default: c = Color.magenta; break;
                    }

                    // Le tablier s'ombre sur son bord droit.
                    if (role == 'K' && (x == 10 || grid[Mathf.Min(Width - 1, x + 1), y] == 'Q'))
                    {
                        c = Dark(apron);
                    }

                    c.a = 1f;
                    pixels[x, Height - 1 - y] = c;
                }
            }

            Outline(pixels);
            AddGroundShadow(pixels);
            return pixels;
        }

        // ---------- Gabarits ----------

        private static char[,] Parse(string[] rows)
        {
            var grid = new char[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    grid[x, y] = '.';
                }
            }

            Overlay(grid, null, rows, 0);
            return grid;
        }

        /// <summary>Pose un calque : « . » garde le dessous, « _ » remet le corps, une ligne vide est sautée.</summary>
        private static void Overlay(char[,] grid, char[,] body, string[] rows, int firstRow)
        {
            for (int y = firstRow; y < rows.Length && y < Height; y++)
            {
                string row = rows[y];
                if (row.Length == 0)
                {
                    continue;
                }

                if (row.Length != Width)
                {
                    throw new ArgumentException($"Gabarit du pêcheur : ligne {y} de {row.Length} pixels au lieu de {Width} (« {row} »).");
                }

                for (int x = 0; x < Width; x++)
                {
                    char c = row[x];
                    if (c == '_')
                    {
                        grid[x, y] = body != null ? body[x, y] : '.';
                    }
                    else if (c != '.')
                    {
                        grid[x, y] = c;
                    }
                }
            }
        }

        private static void Replace(char[,] grid, string from, char to)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (from.IndexOf(grid[x, y]) >= 0)
                    {
                        grid[x, y] = to;
                    }
                }
            }
        }

        private static void SetIf(char[,] grid, int x, int y, char to, string only)
        {
            if (x >= 0 && y >= 0 && x < Width && y < Height && only.IndexOf(grid[x, y]) >= 0)
            {
                grid[x, y] = to;
            }
        }

        // ---------- Hauts ----------

        private const string Cloth = "TtuAa";

        /// <summary>Col, fleurs d'hibiscus, rayures ou bras nus selon le haut choisi.</summary>
        private static void DecorateTop(char[,] grid, int top, int view)
        {
            bool front = view == ViewFront;
            switch (top)
            {
                case TopHawaiian:
                    if (front)
                    {
                        SetIf(grid, 7, 13, 's', Cloth);   // col ouvert
                        SetIf(grid, 8, 13, 's', Cloth);
                        SetIf(grid, 6, 13, 'u', Cloth);
                        SetIf(grid, 9, 13, 'u', Cloth);
                    }

                    int[][] flowers = view == ViewSide
                        ? new[] { new[] { 6, 13 }, new[] { 9, 16 } }
                        : new[] { new[] { 5, 14 }, new[] { 10, 15 }, new[] { 7, 16 } };
                    foreach (int[] f in flowers)
                    {
                        SetIf(grid, f[0], f[1], 'q', Cloth);       // cœur rose
                        SetIf(grid, f[0] - 1, f[1], 'c', Cloth);   // pétales crème
                        SetIf(grid, f[0], f[1] - 1, 'c', Cloth);
                        SetIf(grid, f[0] + 1, f[1], 'c', Cloth);
                    }

                    break;
                case TopTee:
                    if (front)
                    {
                        SetIf(grid, 7, 13, 's', Cloth);
                        SetIf(grid, 8, 13, 's', Cloth);
                    }

                    break;
                case TopTank:
                    Replace(grid, "A", 'S');
                    Replace(grid, "a", 's');
                    if (front)
                    {
                        for (int x = 6; x <= 9; x++)
                        {
                            SetIf(grid, x, 13, 's', Cloth);
                        }
                    }

                    break;
                default:
                    if (front)
                    {
                        SetIf(grid, 7, 13, 's', Cloth);
                        SetIf(grid, 8, 13, 's', Cloth);
                    }

                    for (int x = 0; x < Width; x++)
                    {
                        SetIf(grid, x, 14, 'w', "TuA");
                        SetIf(grid, x, 14, 'v', "ta");
                        SetIf(grid, x, 16, 'w', "TuA");
                        SetIf(grid, x, 16, 'v', "ta");
                    }

                    break;
            }
        }

        // ---------- Couleurs ----------

        private static Color Dark(Color c) => PixelShading.Shade(c, 0.70f);

        private static Color Bright(Color c) => PixelShading.Shade(c, 1.18f);

        private static Color[,] Colorize(char[,] grid, CharacterLook look)
        {
            Color skin = CharacterOptions.SkinColors[look.Skin];
            Color hair = CharacterOptions.HairColors[look.HairColor];
            Color shirt = CharacterOptions.TopColors[look.TopColor];
            Color shorts = CharacterOptions.BottomColors[look.Bottom];
            Color stripe = shirt.r + shirt.g + shirt.b > 2.4f ? StripeNavy : StripeWhite;
            Color hatColor = look.Hat == 1 ? StrawHat : look.Hat == 2 ? CapColor : look.Hat == 3 ? BobColor : BandanaColor;
            Color band = look.Hat == 1 ? HatBand : White;

            var pixels = new Color[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    char role = grid[x, y];
                    if (role == '.')
                    {
                        continue;
                    }

                    Color c;
                    switch (role)
                    {
                        case 'S': c = skin; break;
                        case 's': c = Dark(skin); break;
                        case 'E': c = Eye; break;
                        case 'm': c = Color.Lerp(skin, Lips, 0.55f); break;
                        case 'p': c = Color.Lerp(skin, Blush, 0.45f); break;
                        case 'T': case 'A': c = shirt; break;
                        case 't': case 'a': c = Dark(shirt); break;
                        case 'u': c = Bright(shirt); break;
                        case 'w': c = stripe; break;
                        case 'v': c = Dark(stripe); break;
                        case 'c': c = Petal; break;
                        case 'q': c = PetalPink; break;
                        case 'B': c = shorts; break;
                        case 'b': c = Dark(shorts); break;
                        case 'F': c = Sandal; break;
                        case 'f': c = Dark(Sandal); break;
                        case 'H': c = hair; break;
                        case 'h': c = Dark(hair); break;
                        case 'j': c = Bright(hair); break;
                        case 'z': c = Color.Lerp(skin, hair, 0.55f); break;
                        case 'r': c = shirt; break;
                        case 'X': c = hatColor; break;
                        case 'x': c = Dark(hatColor); break;
                        case 'y': c = Bright(hatColor); break;
                        case 'R': c = band; break;
                        case 'W': c = White; break;
                        default: c = Color.magenta; break;
                    }

                    // Paille tressée : un pixel sur deux un peu plus sombre.
                    if (look.Hat == 1 && (role == 'X' || role == 'y') && (x + y) % 2 == 0)
                    {
                        c = Color.Lerp(c, new Color(0.66f, 0.48f, 0.24f), 0.25f);
                    }

                    c.a = 1f;
                    pixels[x, Height - 1 - y] = c;
                }
            }

            return pixels;
        }

        /// <summary>Contour d'un pixel autour de la silhouette : version très sombre et teintée des pixels voisins.</summary>
        private static void Outline(Color[,] p)
        {
            var outline = new Color[Width, Height];
            var has = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (PixelShading.IsFilled(p, x, y))
                    {
                        continue;
                    }

                    Color sum = Color.clear;
                    int count = 0;
                    foreach ((int dx, int dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        if (PixelShading.IsFilled(p, x + dx, y + dy))
                        {
                            sum += p[x + dx, y + dy];
                            count++;
                        }
                    }

                    if (count == 0)
                    {
                        continue;
                    }

                    Color n = sum / count;
                    outline[x, y] = new Color(n.r * 0.28f + 0.05f, n.g * 0.24f + 0.03f, n.b * 0.28f + 0.06f, 1f);
                    has[x, y] = true;
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (has[x, y])
                    {
                        p[x, y] = outline[x, y];
                    }
                }
            }
        }

        /// <summary>Ombre douce et transparente au sol, autour des pieds (après le contour).</summary>
        private static void AddGroundShadow(Color[,] p)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    float u = (x + 0.5f - 8f) / 5.5f;
                    float v = (y + 0.5f - 0.6f) / 1.7f;
                    if (u * u + v * v <= 1f && !PixelShading.IsFilled(p, x, y))
                    {
                        p[x, y] = GroundShadow;
                    }
                }
            }
        }
    }
}
