using System;
using UnityEngine;

namespace FishAndChill.Player
{
    /// <summary>
    /// Dessine les personnages en pixel art fin (32×48, 32 pixels par unité : même taille à l'écran que les cases,
    /// deux fois plus de détails). Le pêcheur suit son apparence (<see cref="CharacterLook"/>) : corps, coiffure,
    /// chapeau et motifs du haut posés en calques à partir de gabarits faits main (<see cref="CharacterTemplates"/>).
    /// Le relief est calculé pour chaque pièce (lumière en haut à gauche : reflet, base, ombre, ombre profonde),
    /// avec une ombre là où une pièce passe devant une autre, un contour sombre teinté et une ombre douce au sol.
    /// 3 images par direction (0 = à l'arrêt, 1 et 2 = pas de marche) ; le profil regarde à droite, la gauche en est
    /// le miroir. Tableaux [x, y] avec y depuis le bas ; pivot aux pieds.
    /// </summary>
    public static class CharacterPainter
    {
        public const int Width = 32;
        public const int Height = 48;
        public const int PixelsPerUnit = 32;

        private static readonly Color StrawHat = new Color(0.95f, 0.82f, 0.50f);
        private static readonly Color HatBand = new Color(0.84f, 0.24f, 0.24f);
        private static readonly Color CapColor = new Color(0.86f, 0.25f, 0.24f);
        private static readonly Color BobColor = new Color(0.70f, 0.72f, 0.54f);
        private static readonly Color BandanaColor = new Color(0.22f, 0.44f, 0.82f);
        private static readonly Color White = new Color(0.98f, 0.97f, 0.93f);
        private static readonly Color Blush = new Color(0.97f, 0.50f, 0.50f);
        private static readonly Color Lips = new Color(0.70f, 0.30f, 0.30f);
        private static readonly Color Eye = new Color(0.12f, 0.08f, 0.11f);
        private static readonly Color Iris = new Color(0.36f, 0.22f, 0.16f);
        private static readonly Color Petal = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color PetalPink = new Color(0.98f, 0.42f, 0.56f);
        private static readonly Color StripeWhite = new Color(0.97f, 0.97f, 0.94f);
        private static readonly Color StripeNavy = new Color(0.18f, 0.27f, 0.56f);
        private static readonly Color Sandal = new Color(0.52f, 0.32f, 0.18f);
        private static readonly Color GroundShadow = new Color(0.05f, 0.10f, 0.06f, 0.26f);

        private const int ViewFront = 0, ViewBack = 1, ViewSide = 2;
        private const int TopHawaiian = 0, TopTee = 1, TopTank = 2;
        private const int HatNone = 0, HatStraw = 1, HatCap = 2, HatBob = 3, HatBandana = 4;

        // Pièces (pour le relief) et paliers de lumière.
        private const int PartNone = 0, PartSkin = 1, PartHair = 2, PartTop = 3, PartBottom = 4, PartShoes = 5, PartHat = 6,
            PartTie = 7, PartApron = 8, PartSleeve = 9;

        private static readonly float[] Tones = { 0.56f, 0.76f, 1f, 1.14f };

        /// <summary>Image de face (ou de dos).</summary>
        public static Color[,] Front(CharacterLook look, int frame, bool back) => Draw(look, back ? ViewBack : ViewFront, frame);

        /// <summary>Image de profil, regard vers la droite.</summary>
        public static Color[,] Side(CharacterLook look, int frame) => Draw(look, ViewSide, frame);

        private static Color[,] Draw(CharacterLook look, int view, int frame)
        {
            look = CharacterOptions.Sanitize(look);
            frame = Mathf.Clamp(frame, 0, 2);
            bool mirrored = view != ViewSide;
            char[,] body = Blank();
            if (mirrored)
            {
                Overlay(body, null, CharacterTemplates.FrontUpper, 0, true);
                Overlay(body, null, CharacterTemplates.FrontLegs[frame], 40, false);
            }
            else
            {
                Overlay(body, null, CharacterTemplates.SideHead, 0, false);
                Overlay(body, null, CharacterTemplates.SideBody[frame], 26, false);
            }

            if (view == ViewBack)
            {
                Replace(body, "EWIkmp", 'S');   // de dos : pas de visage
            }

            var grid = (char[,])body.Clone();
            int clip = HairClip(look.Hat);
            foreach (string[] layer in CharacterTemplates.Hair(look.HairStyle, look.Hat != HatNone)[view])
            {
                Overlay(grid, body, layer, 0, mirrored, clip);
            }

            DrawHat(grid, look.Hat, view);
            DecorateTop(grid, look.Top, view);

            Color skin = CharacterOptions.SkinColors[look.Skin];
            Color hair = CharacterOptions.HairColors[look.HairColor];
            Color shirt = CharacterOptions.TopColors[look.TopColor];
            Color shorts = CharacterOptions.BottomColors[look.Bottom];
            Color stripe = shirt.r + shirt.g + shirt.b > 2.4f ? StripeNavy : StripeWhite;
            Color hatColor = look.Hat == HatStraw ? StrawHat : look.Hat == HatCap ? CapColor : look.Hat == HatBob ? BobColor : BandanaColor;
            Color[,] pixels = Render(grid, role =>
            {
                switch (role)
                {
                    case 'S': case 's': return (PartSkin, skin);
                    case 'E': return (PartSkin, Eye);
                    case 'W': return (PartSkin, White);
                    case 'I': return (PartSkin, Iris);
                    case 'k': return (PartSkin, PixelShading.Shade(hair, 0.6f));
                    case 'm': return (PartSkin, Color.Lerp(skin, Lips, 0.6f));
                    case 'p': return (PartSkin, Color.Lerp(skin, Blush, 0.45f));
                    case 'H': case 'h': case 'j': return (PartHair, hair);
                    case 'z': return (PartHair, Color.Lerp(skin, hair, 0.6f));
                    case 'r': return (PartTie, shirt);
                    case 'A': return (PartSleeve, shirt);
                    case 'T': case 't': case 'u': return (PartTop, shirt);
                    case 'w': return (PartTop, stripe);
                    case 'c': return (PartTop, Petal);
                    case 'q': return (PartTop, PetalPink);
                    case 'B': case 'b': return (PartBottom, shorts);
                    case 'F': case 'f': return (PartShoes, Sandal);
                    case 'X': case 'x': case 'y': return (PartHat, hatColor);
                    case 'R': return (PartHat, HatBand);
                    case 'Y': return (PartHat, White);
                    default: return (PartNone, Color.magenta);
                }
            }, look.Hat == HatStraw);
            return pixels;
        }

        /// <summary>Marchand de face : vieux marin au bob blanc, moustache grise, marinière et tablier à poche brodée.</summary>
        public static Color[,] Merchant()
        {
            char[,] grid = Blank();
            Overlay(grid, null, CharacterTemplates.Merchant, 0, true);
            var skin = new Color(0.88f, 0.63f, 0.45f);
            var grey = new Color(0.86f, 0.86f, 0.84f);
            var navy = new Color(0.20f, 0.32f, 0.66f);
            var apron = new Color(0.96f, 0.91f, 0.79f);
            var trousers = new Color(0.36f, 0.30f, 0.27f);
            var boots = new Color(0.32f, 0.21f, 0.14f);

            // Le bob (en haut) a son relief ; le blanc des yeux reste fixe. Rayures de la marinière (2 pixels sur 4).
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y <= 10; y++)
                {
                    grid[x, y] = grid[x, y] == 'W' ? 'X' : grid[x, y] == 'w' ? 'x' : grid[x, y];
                }

                for (int y = 26; y < 40; y++)
                {
                    if (grid[x, y] == 'T' && (y - 26) % 4 >= 2)
                    {
                        grid[x, y] = 'w';
                    }
                }
            }

            return Render(grid, role =>
            {
                switch (role)
                {
                    case 'S': case 's': return (PartSkin, skin);
                    case 'E': return (PartSkin, Eye);
                    case 'X': case 'x': return (PartHat, White);
                    case 'W': return (PartSkin, White);
                    case 'w': return (PartTop, navy);
                    case 'I': return (PartSkin, Iris);
                    case 'k': return (PartSkin, PixelShading.Shade(grey, 0.8f));
                    case 'm': return (PartSkin, Color.Lerp(skin, Lips, 0.5f));
                    case 'p': return (PartSkin, Color.Lerp(skin, Blush, 0.5f));
                    case 'N': return (PartHat, navy);
                    case 'G': case 'M': return (PartHair, grey);
                    case 'T': return (PartTop, StripeWhite);
                    case 'K': return (PartApron, apron);
                    case 'q': return (PartApron, navy);
                    case 'P': return (PartBottom, trousers);
                    case 'O': return (PartShoes, boots);
                    default: return (PartNone, Color.magenta);
                }
            }, false);
        }

        // ---------- Gabarits ----------

        private static char[,] Blank()
        {
            var grid = new char[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    grid[x, y] = '.';
                }
            }

            return grid;
        }

        /// <summary>Déroule une ligne abrégée (« 6.10S » → 6 « . » puis 10 « S »).</summary>
        public static string Expand(string row)
        {
            var result = new System.Text.StringBuilder();
            int count = 0;
            bool hasCount = false;
            foreach (char c in row)
            {
                if (char.IsDigit(c))
                {
                    count = count * 10 + (c - '0');
                    hasCount = true;
                    continue;
                }

                result.Append(c, hasCount ? count : 1);
                count = 0;
                hasCount = false;
            }

            return result.ToString();
        }

        /// <summary>
        /// Pose un calque à partir de la ligne <paramref name="firstRow"/>. « . » garde le dessous, « _ » remet le corps,
        /// une ligne vide est sautée, les lignes au-dessus de <paramref name="clip"/> sont ignorées (cachées par le chapeau).
        /// Une moitié (16 pixels) est recopiée en miroir.
        /// </summary>
        private static void Overlay(char[,] grid, char[,] body, string[] rows, int firstRow, bool mirrored, int clip = 0)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                int y = firstRow + i;
                if (rows[i].Length == 0 || y < clip || y >= Height)
                {
                    continue;
                }

                string row = Expand(rows[i]);
                int expected = mirrored ? Width / 2 : Width;
                if (row.Length != expected)
                {
                    throw new ArgumentException($"Gabarit de personnage : ligne {y} de {row.Length} pixels au lieu de {expected} (« {rows[i]} »).");
                }

                for (int x = 0; x < row.Length; x++)
                {
                    Put(grid, body, x, y, row[x]);
                    if (mirrored)
                    {
                        Put(grid, body, Width - 1 - x, y, row[x]);
                    }
                }
            }
        }

        private static void Put(char[,] grid, char[,] body, int x, int y, char c)
        {
            if (c == '_')
            {
                grid[x, y] = body != null ? body[x, y] : '.';
            }
            else if (c != '.')
            {
                grid[x, y] = c;
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

        /// <summary>Remplit une ellipse (centre et rayons en pixels, y depuis le haut), entre deux lignes.</summary>
        private static void Ellipse(char[,] grid, float cx, float cy, float rx, float ry, char c, int minY = 0, int maxY = Height - 1)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = Mathf.Max(0, minY); y <= Mathf.Min(Height - 1, maxY); y++)
                {
                    float u = (x + 0.5f - cx) / rx;
                    float v = (y + 0.5f - cy) / ry;
                    if (u * u + v * v <= 1f)
                    {
                        grid[x, y] = c;
                    }
                }
            }
        }

        // ---------- Chapeaux ----------

        /// <summary>Lignes de cheveux cachées sous chaque chapeau.</summary>
        private static int HairClip(int hat)
        {
            switch (hat)
            {
                case HatStraw: return 8;
                case HatCap: return 4;
                case HatBob: return 7;
                case HatBandana: return 5;
                default: return 0;
            }
        }

        private static void DrawHat(char[,] grid, int hat, int view)
        {
            bool side = view == ViewSide;
            float cx = side ? 15f : 16f;
            switch (hat)
            {
                case HatStraw:
                    Ellipse(grid, cx, 6f, 6.6f, 5.5f, 'X', 0, 7);
                    for (int x = 0; x < Width; x++)
                    {
                        for (int y = 6; y <= 7; y++)
                        {
                            SetIf(grid, x, y, 'R', "X");   // ruban
                        }
                    }

                    if (view == ViewFront)
                    {
                        SetIf(grid, 20, 5, 'R', "X");      // nœud du ruban
                        SetIf(grid, 21, 5, 'R', "X");
                    }

                    Ellipse(grid, side ? 16f : 16f, 9.6f, 15.6f, 2.6f, 'X', 8, 11);
                    for (int x = 0; x < Width; x++)
                    {
                        SetIf(grid, x, 11, 'x', "X");      // dessous du bord
                    }

                    break;
                case HatCap:
                    Ellipse(grid, cx, 9.5f, side ? 9.6f : 10.6f, 7.2f, 'X', 0, 10);
                    if (side)
                    {
                        for (int x = 20; x <= 29; x++)
                        {
                            grid[x, 10] = 'y';             // visière
                            grid[x, 11] = 'x';
                        }

                        grid[17, 5] = 'Y';
                        grid[18, 5] = 'Y';
                        grid[17, 6] = 'Y';
                    }
                    else if (view == ViewFront)
                    {
                        for (int x = 5; x <= 26; x++)
                        {
                            grid[x, 11] = 'y';
                            grid[x, 12] = 'x';
                        }

                        for (int x = 15; x <= 16; x++)
                        {
                            for (int y = 5; y <= 6; y++)
                            {
                                grid[x, y] = 'Y';          // petit logo
                            }
                        }
                    }
                    else
                    {
                        for (int x = 13; x <= 18; x++)
                        {
                            SetIf(grid, x, 9, 'Y', "X");   // réglage à l'arrière
                        }

                        for (int x = 14; x <= 17; x++)
                        {
                            SetIf(grid, x, 10, '_', "X");
                        }

                        Replace(grid, "_", 'H');
                    }

                    break;
                case HatBob:
                    Ellipse(grid, cx, 7.5f, 8.2f, 5.6f, 'X', 0, 8);
                    for (int x = 0; x < Width; x++)
                    {
                        SetIf(grid, x, 8, 'x', "X");       // couture
                    }

                    Ellipse(grid, side ? 16f : 16f, 10.2f, side ? 11.6f : 12.4f, 2.2f, 'X', 9, 11);
                    for (int x = 0; x < Width; x++)
                    {
                        SetIf(grid, x, 11, 'x', "X");
                    }

                    break;
                case HatBandana:
                    Ellipse(grid, cx + (side ? 1f : 0f), 12f, side ? 10f : 11f, 7.5f, 'X', 4, 11);
                    for (int x = 0; x < Width; x++)
                    {
                        for (int y = 4; y <= 11; y++)
                        {
                            if ((x + y * 2) % 5 == 0 && (x + y) % 2 == 0)
                            {
                                SetIf(grid, x, y, 'Y', "X");   // pois blancs
                            }
                        }
                    }

                    if (view == ViewFront)
                    {
                        Ellipse(grid, 27f, 12f, 1.6f, 1.6f, 'X');   // nœud sur le côté
                        grid[28, 14] = 'x';
                        grid[29, 15] = 'x';
                    }
                    else if (view == ViewBack)
                    {
                        Ellipse(grid, 16f, 12.5f, 2.2f, 1.6f, 'X');
                        for (int y = 14; y <= 17; y++)
                        {
                            grid[14 - (y - 14) / 2, y] = 'x';
                            grid[17 + (y - 14) / 2, y] = 'x';
                        }
                    }
                    else
                    {
                        Ellipse(grid, 5f, 11f, 1.8f, 1.6f, 'X');
                        for (int y = 13; y <= 16; y++)
                        {
                            grid[4 - (y - 13) / 2, y] = 'x';
                        }
                    }

                    break;
            }
        }

        // ---------- Hauts ----------

        private const string Cloth = "TtuA";

        /// <summary>Col, fleurs d'hibiscus, rayures ou bras nus selon le haut choisi.</summary>
        private static void DecorateTop(char[,] grid, int top, int view)
        {
            bool front = view == ViewFront;
            if (top == TopTank)
            {
                Replace(grid, "A", 'S');
            }

            if (front)
            {
                // Encolure : en V pour la chemise, ronde ou dégagée sinon.
                int depth = top == TopHawaiian ? 3 : top == TopTank ? 2 : 1;
                for (int d = 0; d < depth; d++)
                {
                    for (int x = 13 + d; x <= 18 - d; x++)
                    {
                        SetIf(grid, x, 26 + d, 's', Cloth);
                    }
                }

                if (top == TopHawaiian)
                {
                    foreach ((int x, int y) in new[] { (12, 26), (19, 26), (13, 27), (18, 27), (14, 28), (17, 28) })
                    {
                        SetIf(grid, x, y, 'u', Cloth);   // revers du col
                    }

                    for (int y = 29; y <= 34; y += 2)
                    {
                        SetIf(grid, 15, y, 'Y', "T");    // boutons
                    }
                }
            }

            if (top == TopHawaiian)
            {
                (int, int)[] spots = view == ViewSide
                    ? new[] { (13, 28), (19, 31), (16, 34) }
                    : new[] { (10, 29), (21, 28), (19, 33), (12, 33), (6, 30), (25, 31) };
                foreach ((int x, int y) in spots)
                {
                    SetIf(grid, x, y, 'q', Cloth);         // hibiscus : cœur rose, pétales crème
                    SetIf(grid, x - 1, y, 'c', Cloth);
                    SetIf(grid, x + 1, y, 'c', Cloth);
                    SetIf(grid, x, y - 1, 'c', Cloth);
                    SetIf(grid, x, y + 1, 'c', Cloth);
                    SetIf(grid, x + 1, y + 1, 'q', Cloth);
                }
            }
            else if (top == 3)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 26; y < 36; y++)
                    {
                        if ((y - 26) % 4 >= 2)
                        {
                            SetIf(grid, x, y, 'w', Cloth);
                        }
                    }
                }
            }
        }

        // ---------- Rendu ----------

        /// <summary>Lettres dont la lumière est imposée : -1 ombre, +1 reflet ; 0 = relief calculé ; 9 = couleur fixe.</summary>
        private static int Forced(char role)
        {
            switch (role)
            {
                case 's': case 'h': case 't': case 'b': case 'f': case 'x': return -1;
                case 'j': case 'u': case 'y': return 1;
                case 'E': case 'W': case 'I': case 'k': case 'm': case 'p': case 'c': case 'q': case 'Y': return 9;
                default: return 0;
            }
        }

        private static Color[,] Render(char[,] grid, Func<char, (int part, Color color)> palette, bool straw)
        {
            var parts = new int[Width, Height];
            var colors = new Color[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (grid[x, y] != '.')
                    {
                        (parts[x, y], colors[x, y]) = palette(grid[x, y]);
                    }
                }
            }

            var pixels = new Color[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int part = parts[x, y];
                    if (part == PartNone)
                    {
                        continue;
                    }

                    char role = grid[x, y];
                    Color c = colors[x, y];
                    int forced = Forced(role);
                    if (forced != 9)
                    {
                        int tone = forced < 0 ? 1 : forced > 0 ? 3 : ToneOf(parts, x, y, part);
                        if (part == PartSkin)
                        {
                            tone = Mathf.Max(1, tone);   // visage et bras jamais trop sombres
                        }

                        // Ombre portée : une pièce qui passe devant (dessous ou à droite), cheveux ou chapeau au-dessus du visage.
                        bool occluded = Differs(parts, x, y + 1, part) || Differs(parts, x + 1, y, part)
                            || (part == PartSkin && y > 0 && (parts[x, y - 1] == PartHair || parts[x, y - 1] == PartHat));
                        if (occluded && forced == 0)
                        {
                            tone = Mathf.Max(0, tone - 1);
                        }

                        c = PixelShading.Shade(c, Tones[tone]);
                        if (straw && part == PartHat && role != 'R' && (x + y) % 2 == 0)
                        {
                            c = Color.Lerp(c, new Color(0.66f, 0.48f, 0.24f), 0.22f);   // paille tressée
                        }
                    }

                    c.a = 1f;
                    pixels[x, Height - 1 - y] = c;
                }
            }

            Outline(pixels);
            AddGroundShadow(pixels);
            return pixels;
        }

        private static bool Differs(int[,] parts, int x, int y, int part)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height && parts[x, y] != PartNone && parts[x, y] != part;
        }

        /// <summary>Palier de lumière d'un pixel selon sa place dans sa pièce : bords en haut à gauche éclairés, en bas à droite dans l'ombre.</summary>
        private static int ToneOf(int[,] parts, int x, int y, int part)
        {
            int left = Run(parts, x, y, -1, 0, part);
            int right = Run(parts, x, y, 1, 0, part);
            int up = Run(parts, x, y, 0, -1, part);
            int down = Run(parts, x, y, 0, 1, part);
            float ex = 1f / (right + 1) - 1f / (left + 1);
            float ey = 1f / (down + 1) - 1f / (up + 1);
            float s = -(0.85f * ex + ey);
            if (s < -0.6f) return 0;
            if (s < -0.22f) return 1;
            if (s > 0.55f) return 3;
            return 2;
        }

        private static int Run(int[,] parts, int x, int y, int dx, int dy, int part)
        {
            int n = 0;
            while (n < 5)
            {
                x += dx;
                y += dy;
                if (x < 0 || y < 0 || x >= Width || y >= Height || parts[x, y] != part)
                {
                    break;
                }

                n++;
            }

            return n;
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
                    outline[x, y] = new Color(n.r * 0.26f + 0.05f, n.g * 0.22f + 0.03f, n.b * 0.26f + 0.06f, 1f);
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
                for (int y = 0; y < 3; y++)
                {
                    float u = (x + 0.5f - 16f) / 11f;
                    float v = (y + 0.5f - 1.2f) / 2.6f;
                    if (u * u + v * v <= 1f && !PixelShading.IsFilled(p, x, y))
                    {
                        p[x, y] = GroundShadow;
                    }
                }
            }
        }
    }
}
