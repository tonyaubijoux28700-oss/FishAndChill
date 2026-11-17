using UnityEngine;

namespace FishAndChill.Player
{
    /// <summary>
    /// Dessine le pêcheur en pixel art 16×24 selon son apparence (<see cref="CharacterLook"/>), en jeu comme dans
    /// l'éditeur : formes rondes éclairées par paliers (ombres froides, lumières chaudes), contour sélectif teinté,
    /// ombre douce au sol. Coiffures, chapeaux (paille, casquette, bob, bandana) et hauts (chemise à fleurs, t-shirt,
    /// débardeur, marinière) au choix. 3 images par direction (0 = à l'arrêt, 1 et 2 = pas de marche) ; le profil
    /// regarde à droite, la gauche en est le miroir. Tableaux [x, y] avec y depuis le bas ; pivot aux pieds.
    /// </summary>
    public static class CharacterPainter
    {
        public const int Width = 16;
        public const int Height = 24;

        private static readonly Color StrawHat = new Color(0.93f, 0.80f, 0.47f);
        private static readonly Color HatBand = new Color(0.82f, 0.24f, 0.22f);
        private static readonly Color CapColor = new Color(0.84f, 0.26f, 0.22f);
        private static readonly Color BobColor = new Color(0.66f, 0.68f, 0.50f);
        private static readonly Color BandanaColor = new Color(0.86f, 0.22f, 0.22f);
        private static readonly Color Blush = new Color(0.96f, 0.52f, 0.50f);
        private static readonly Color Lips = new Color(0.78f, 0.40f, 0.34f);
        private static readonly Color Eye = new Color(0.10f, 0.07f, 0.07f);
        private static readonly Color EyeLow = new Color(0.30f, 0.20f, 0.16f);
        private static readonly Color Petal = new Color(0.99f, 0.95f, 0.86f);
        private static readonly Color PetalPink = new Color(0.96f, 0.44f, 0.55f);
        private static readonly Color StripeWhite = new Color(0.96f, 0.96f, 0.93f);
        private static readonly Color StripeNavy = new Color(0.20f, 0.30f, 0.60f);
        private static readonly Color Sandal = new Color(0.52f, 0.32f, 0.18f);
        private static readonly Color OutlineColor = new Color(0.14f, 0.09f, 0.10f);
        private static readonly Color GroundShadow = new Color(0.05f, 0.10f, 0.06f, 0.24f);

        /// <summary>Lumière d'en haut à gauche, un peu de face.</summary>
        private static readonly Vector3 LightDirection = new Vector3(-0.45f, 0.70f, 0.55f).normalized;

        private const int HairShort = 0, HairSpiky = 1, HairBob = 2, HairLong = 3, HairPonytail = 4, HairBun = 5, HairShaved = 6;
        private const int HatNone = 0, HatStraw = 1, HatCap = 2, HatBob = 3, HatBandana = 4;
        private const int TopHawaiian = 0, TopTee = 1, TopTank = 2, TopStriped = 3;

        /// <summary>Couleurs d'une apparence.</summary>
        private sealed class Palette
        {
            public Color Skin, Hair, HairLight, Shirt, Shorts, Stripe;
            public int HairStyle, Hat, Top;

            public Palette(CharacterLook look)
            {
                look = CharacterOptions.Sanitize(look);
                Skin = CharacterOptions.SkinColors[look.Skin];
                Hair = CharacterOptions.HairColors[look.HairColor];
                var bright = new Color(Mathf.Min(1f, Hair.r * 1.5f + 0.04f), Mathf.Min(1f, Hair.g * 1.5f + 0.04f), Mathf.Min(1f, Hair.b * 1.5f + 0.04f));
                HairLight = Color.Lerp(Hair, bright, 0.5f);
                Shirt = CharacterOptions.TopColors[look.TopColor];
                Shorts = CharacterOptions.BottomColors[look.Bottom];
                Stripe = Shirt.r + Shirt.g + Shirt.b > 2.4f ? StripeNavy : StripeWhite;
                HairStyle = look.HairStyle;
                Hat = look.Hat;
                Top = look.Top;
            }

            public bool HasHat => Hat != HatNone;
            public Color Shaved => Color.Lerp(Skin, Hair, 0.55f);
        }

        /// <summary>Les 3 images de face (ou de dos).</summary>
        public static Color[,] Front(CharacterLook look, int frame, bool back) => DrawFront(new Palette(look), frame, back);

        /// <summary>Les 3 images de profil, regard vers la droite.</summary>
        public static Color[,] Side(CharacterLook look, int frame) => DrawSide(new Palette(look), frame);

        // ---------- Toile : couleur de base + lumière par pixel (y compté depuis le HAUT) ----------

        private enum Part { None, Hat, Hair, Skin, Shirt, Shorts, Sandal }

        private sealed class Canvas
        {
            public readonly Color[,] Base = new Color[Width, Height];
            public readonly float[,] Light = new float[Width, Height];
            public readonly Part[,] Parts = new Part[Width, Height];

            public bool Inside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

            public bool Is(int x, int y, Part part) => Inside(x, y) && Parts[x, y] == part;

            public bool Filled(int x, int y) => Inside(x, y) && Parts[x, y] != Part.None;

            public void Set(int x, int y, Color color, Part part, float light)
            {
                if (!Inside(x, y))
                {
                    return;
                }

                Base[x, y] = color;
                Light[x, y] = light;
                Parts[x, y] = part;
            }

            /// <summary>Change la couleur sans toucher à la lumière (motifs, détails).</summary>
            public void Paint(int x, int y, Color color, Part only = Part.None)
            {
                if (Inside(x, y) && Parts[x, y] != Part.None && (only == Part.None || Parts[x, y] == only))
                {
                    Base[x, y] = color;
                }
            }

            /// <summary>Change la couleur et la pièce, en gardant la lumière (cheveux posés sur la tête).</summary>
            public void Cover(int x, int y, Color color, Part part)
            {
                if (Filled(x, y))
                {
                    Base[x, y] = color;
                    Parts[x, y] = part;
                }
            }

            public void Flat(int x, int y, Color color, Part part) => Set(x, y, color, part, 1f);

            public void Darken(int x, int y, float amount)
            {
                if (Filled(x, y))
                {
                    Light[x, y] = Mathf.Max(0.4f, Light[x, y] - amount);
                }
            }

            public void Clear(int x, int y)
            {
                if (Inside(x, y))
                {
                    Parts[x, y] = Part.None;
                }
            }

            /// <summary>Ellipse éclairée comme une boule (centre en pixels, y depuis le haut), coupée sous <paramref name="maxY"/>.</summary>
            public void Ellipse(float cx, float cy, float rx, float ry, Color color, Part part, int maxY = Height)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height && y <= maxY; y++)
                    {
                        float u = (x + 0.5f - cx) / rx;
                        float v = (y + 0.5f - cy) / ry;
                        float d = u * u + v * v;
                        if (d <= 1f)
                        {
                            Set(x, y, color, part, LightOf(u, -v, Mathf.Sqrt(1f - d)));
                        }
                    }
                }
            }

            /// <summary>« Cylindre » vertical (bras, jambes, torse), coins du haut arrondis si demandé.</summary>
            public void Cylinder(int x0, int y0, int x1, int y1, Color color, Part part, bool roundTop = false, float lightOffset = 0f)
            {
                float cx = (x0 + x1 + 1) * 0.5f;
                float half = Mathf.Max(0.5f, (x1 - x0 + 1) * 0.5f);
                for (int x = x0; x <= x1; x++)
                {
                    for (int y = y0; y <= y1; y++)
                    {
                        if (roundTop && y == y0 && (x == x0 || x == x1))
                        {
                            continue;
                        }

                        float u = Mathf.Clamp((x + 0.5f - cx) / half, -0.95f, 0.95f);
                        float top = y == y0 ? 0.5f : 0.1f;
                        Set(x, y, color, part, LightOf(u, top, Mathf.Sqrt(1f - u * u)) + lightOffset);
                    }
                }
            }

            public Color[,] Render()
            {
                var p = new Color[Width, Height];
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (Parts[x, y] != Part.None)
                        {
                            p[x, Height - 1 - y] = PixelShading.Shade(Base[x, y], Light[x, y]);
                        }
                    }
                }

                PixelShading.SelectiveOutline(p, OutlineColor, 0.6f);
                AddGroundShadow(p);
                return p;
            }

            private static float LightOf(float nx, float ny, float nz)
            {
                var normal = new Vector3(nx, ny, nz).normalized;
                float diffuse = Mathf.Max(0f, Vector3.Dot(normal, LightDirection));
                float light = 0.62f + 0.62f * diffuse;
                return Mathf.Round(light * 6f) / 6f;   // paliers nets, comme les poissons
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

        // ---------- Face et dos ----------

        private static Color[,] DrawFront(Palette p, int frame, bool back)
        {
            var c = new Canvas();

            // Jambes et sandales (une jambe se lève à chaque pas).
            DrawLegFront(c, p, 5, frame == 1, strapRight: false);
            DrawLegFront(c, p, 9, frame == 2, strapRight: true);

            // Short, avec l'entrejambe ouvert et l'ourlet plus sombre.
            c.Cylinder(4, 18, 11, 20, p.Shorts, Part.Shorts);
            c.Clear(7, 20);
            c.Clear(8, 20);
            for (int x = 4; x <= 11; x++)
            {
                c.Darken(x, 20, 0.1f);
            }

            // Haut, porté par-dessus le short ; bras qui se balancent.
            c.Cylinder(3, 13, 12, 18, p.Shirt, Part.Shirt, roundTop: true);
            DrawArmFront(c, p, 2, frame == 1 ? -1 : 0, -0.7f);
            DrawArmFront(c, p, 13, frame == 2 ? -1 : 0, 0.7f);

            // Tête.
            if (back)
            {
                c.Ellipse(8f, 10.2f, 3.75f, 3.1f, p.HairStyle == HairShaved ? p.Shaved : p.Hair, Part.Hair);
                DecorateTopBack(c, p);
                DrawHairBack(c, p);
            }
            else
            {
                c.Ellipse(8f, 10.2f, 3.75f, 3.1f, p.Skin, Part.Skin);
                DrawFace(c, p);
                DecorateTopFront(c, p);
                DrawHairFront(c, p);
            }

            DrawHatFront(c, p, back);
            return c.Render();
        }

        private static void DrawFace(Canvas c, Palette p)
        {
            // Yeux de deux pixels (le bas plus clair), joues roses, petit sourire.
            c.Flat(6, 9, Eye, Part.Skin);
            c.Flat(6, 10, EyeLow, Part.Skin);
            c.Flat(9, 9, Eye, Part.Skin);
            c.Flat(9, 10, EyeLow, Part.Skin);
            c.Paint(5, 11, Color.Lerp(p.Skin, Blush, 0.5f));
            c.Paint(10, 11, Color.Lerp(p.Skin, Blush, 0.5f));

            // Visage bien éclairé sous le chapeau (sinon la peau tourne au gris).
            for (int x = 5; x <= 10; x++)
            {
                for (int y = 9; y <= 11; y++)
                {
                    if (c.Is(x, y, Part.Skin))
                    {
                        c.Light[x, y] = Mathf.Max(c.Light[x, y], 1f);
                    }
                }
            }

            c.Paint(7, 12, Color.Lerp(p.Skin, Lips, 0.35f));
        }

        private static void DrawHairFront(Canvas c, Palette p)
        {
            int style = p.HairStyle;
            if (style == HairShaved)
            {
                for (int x = 0; x < Width; x++)
                {
                    c.Cover(x, 7, p.Shaved, Part.Hair);
                }

                return;
            }

            // Dessus de la tête (caché par la plupart des chapeaux) puis frange.
            for (int x = 0; x < Width; x++)
            {
                c.Cover(x, 7, p.Hair, Part.Hair);
            }

            int[] fringe = style == HairSpiky ? new[] { 4, 5, 6, 7, 8, 9, 10, 11 }
                : style == HairPonytail ? new[] { 4, 5, 6, 7, 10, 11 }
                : style == HairBun ? new[] { 4, 5, 10, 11 }
                : new[] { 4, 5, 6, 9, 10, 11 };
            foreach (int x in fringe)
            {
                c.Cover(x, 8, p.Hair, Part.Hair);
            }

            c.Paint(5, 8, p.HairLight);

            if (style == HairBob || style == HairLong)
            {
                // Mèches qui tombent de chaque côté du visage.
                int bottom = style == HairLong ? 15 : 12;
                for (int y = 9; y <= bottom; y++)
                {
                    c.Set(4, y, p.Hair, Part.Hair, y > 12 ? 0.85f : 0.8f);
                    c.Set(11, y, p.Hair, Part.Hair, 0.62f);
                    if (y >= 10)
                    {
                        c.Set(3, y, p.Hair, Part.Hair, 0.95f);
                        c.Set(12, y, p.Hair, Part.Hair, 0.6f);
                    }
                }

                c.Paint(4, 10, p.HairLight);
                c.Paint(3, 12, p.HairLight);
            }
            else
            {
                // Favoris.
                c.Set(4, 9, p.Hair, Part.Hair, 0.7f);
                c.Set(11, 9, p.Hair, Part.Hair, 0.6f);
            }

            if (style == HairPonytail)
            {
                c.Set(12, 10, p.Hair, Part.Hair, 0.62f);
                c.Set(12, 11, p.Hair, Part.Hair, 0.55f);
                c.Set(13, 12, p.Hair, Part.Hair, 0.55f);
            }

            DrawHairVolume(c, p, 8f);
        }

        /// <summary>Ce qui dépasse au-dessus de la tête quand on ne porte pas de chapeau (épis, chignon).</summary>
        private static void DrawHairVolume(Canvas c, Palette p, float cx)
        {
            if (p.HasHat)
            {
                return;
            }

            int x0 = Mathf.RoundToInt(cx);
            if (p.HairStyle == HairSpiky)
            {
                for (int x = x0 - 3; x <= x0 + 2; x++)
                {
                    c.Set(x, 6, p.Hair, Part.Hair, x < x0 ? 1.15f : 0.9f);
                }

                foreach (int x in new[] { x0 - 3, x0 - 1, x0, x0 + 2 })
                {
                    c.Set(x, 5, p.Hair, Part.Hair, 1.1f);
                }

                c.Set(x0 - 1, 4, p.HairLight, Part.Hair, 1.1f);
            }
            else if (p.HairStyle == HairBun)
            {
                c.Ellipse(cx, 5.3f, 2.0f, 1.8f, p.Hair, Part.Hair);
                c.Paint(x0 - 1, 4, p.HairLight, Part.Hair);
            }
        }

        private static void DrawHairBack(Canvas c, Palette p)
        {
            int style = p.HairStyle;
            Color hair = style == HairShaved ? p.Shaved : p.Hair;

            // Mèches sur l'arrière de la tête.
            if (style != HairShaved)
            {
                for (int x = 4; x <= 11; x++)
                {
                    for (int y = 8; y <= 12; y++)
                    {
                        if (c.Is(x, y, Part.Hair) && (x + y * 2) % 3 == 0)
                        {
                            c.Paint(x, y, p.HairLight);
                        }
                    }
                }
            }

            if (style == HairBob || style == HairLong)
            {
                int bottom = style == HairLong ? 16 : 13;
                for (int y = 12; y <= bottom; y++)
                {
                    for (int x = 4; x <= 11; x++)
                    {
                        c.Set(x, y, hair, Part.Hair, (x < 8 ? 0.95f : 0.75f) - (y - 12) * 0.03f);
                    }
                }

                for (int y = 10; y <= bottom - 1; y++)
                {
                    c.Set(3, y, hair, Part.Hair, 0.9f);
                    c.Set(12, y, hair, Part.Hair, 0.62f);
                }

                for (int x = 5; x <= 10; x += 2)
                {
                    c.Paint(x, bottom - 1, p.HairLight);
                }

                return;
            }

            // Nuque.
            c.Set(7, 12, p.Skin, Part.Skin, 0.7f);
            c.Set(8, 12, p.Skin, Part.Skin, 0.7f);

            if (style == HairPonytail)
            {
                c.Set(7, 12, p.Shirt, Part.Hair, 1f);             // élastique
                c.Set(8, 12, p.Shirt, Part.Hair, 0.8f);
                for (int y = 13; y <= 16; y++)
                {
                    c.Set(7, y, hair, Part.Hair, 0.95f);
                    c.Set(8, y, hair, Part.Hair, 0.75f);
                }

                c.Paint(7, 14, p.HairLight);
            }
            else if (style == HairBun && p.HasHat)
            {
                c.Ellipse(8f, 11.6f, 1.8f, 1.4f, hair, Part.Hair);   // chignon bas, sous le chapeau
            }

            DrawHairVolume(c, p, 8f);
        }

        private static void DrawArmFront(Canvas c, Palette p, int x, int swing, float side)
        {
            bool bareArms = p.Top == TopTank;
            for (int y = 13; y <= 18; y++)
            {
                int row = y + swing;
                bool sleeve = y <= 15 && !bareArms;
                bool hand = y == 18;
                float light = side < 0 ? (hand ? 1.1f : 1f) : (hand ? 0.85f : 0.72f);
                c.Set(x, row, sleeve ? p.Shirt : p.Skin, sleeve ? Part.Shirt : Part.Skin, light);
            }

            if (!bareArms)
            {
                c.Paint(x, 15 + swing, Color.Lerp(p.Shirt, Color.white, 0.25f), Part.Shirt);   // ourlet de manche
            }
        }

        private static void DrawLegFront(Canvas c, Palette p, int x, bool lifted, bool strapRight)
        {
            int bottom = lifted ? 22 : 23;
            c.Cylinder(x, 21, x + 1, bottom - 1, p.Skin, Part.Skin);
            c.Cylinder(x, bottom, x + 1, bottom, Sandal, Part.Sandal, lightOffset: lifted ? -0.1f : 0f);
            c.Paint(strapRight ? x + 1 : x, bottom - 1, Color.Lerp(Sandal, p.Skin, 0.25f), Part.Skin);   // lanière
        }

        // ---------- Hauts ----------

        /// <summary>Hibiscus : cœur rose, 4 pétales crème, seulement sur la chemise.</summary>
        private static void Flower(Canvas c, int x, int y)
        {
            c.Paint(x, y, PetalPink, Part.Shirt);
            c.Paint(x - 1, y, Petal, Part.Shirt);
            c.Paint(x + 1, y, Petal, Part.Shirt);
            c.Paint(x, y - 1, Petal, Part.Shirt);
            c.Paint(x, y + 1, Color.Lerp(Petal, PetalPink, 0.35f), Part.Shirt);
        }

        private static void Stripes(Canvas c, Palette p)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 13; y <= 19; y++)
                {
                    if (y % 2 == 0)
                    {
                        c.Paint(x, y, p.Stripe, Part.Shirt);
                    }
                }
            }
        }

        private static void DecorateTopFront(Canvas c, Palette p)
        {
            switch (p.Top)
            {
                case TopHawaiian:
                    // Col ouvert en V et boutonnage.
                    c.Set(7, 13, p.Skin, Part.Skin, 0.8f);
                    c.Set(8, 13, p.Skin, Part.Skin, 0.7f);
                    c.Paint(6, 13, Color.Lerp(p.Shirt, Color.white, 0.4f), Part.Shirt);
                    c.Paint(9, 13, Color.Lerp(p.Shirt, Color.white, 0.4f), Part.Shirt);
                    for (int y = 14; y <= 18; y++)
                    {
                        c.Darken(8, y, 0.12f);
                    }

                    Flower(c, 5, 15);
                    Flower(c, 10, 16);
                    Flower(c, 6, 18);
                    break;
                case TopTee:
                    c.Set(7, 13, p.Skin, Part.Skin, 0.8f);
                    c.Set(8, 13, p.Skin, Part.Skin, 0.7f);
                    c.Paint(6, 13, Color.Lerp(p.Shirt, Color.black, 0.15f), Part.Shirt);
                    c.Paint(9, 13, Color.Lerp(p.Shirt, Color.black, 0.15f), Part.Shirt);
                    c.Paint(5, 15, Color.Lerp(p.Shirt, Color.black, 0.18f), Part.Shirt);   // petite poche
                    c.Paint(6, 15, Color.Lerp(p.Shirt, Color.black, 0.18f), Part.Shirt);
                    break;
                case TopTank:
                    for (int x = 6; x <= 9; x++)
                    {
                        c.Set(x, 13, p.Skin, Part.Skin, x < 8 ? 0.9f : 0.75f);
                    }

                    c.Set(7, 14, p.Skin, Part.Skin, 0.8f);
                    c.Set(8, 14, p.Skin, Part.Skin, 0.7f);
                    c.Set(3, 13, p.Skin, Part.Skin, 1f);
                    c.Set(12, 13, p.Skin, Part.Skin, 0.72f);
                    break;
                default:
                    Stripes(c, p);
                    c.Set(7, 13, p.Skin, Part.Skin, 0.8f);
                    c.Set(8, 13, p.Skin, Part.Skin, 0.7f);
                    break;
            }
        }

        private static void DecorateTopBack(Canvas c, Palette p)
        {
            switch (p.Top)
            {
                case TopHawaiian:
                    for (int x = 6; x <= 9; x++)
                    {
                        c.Paint(x, 13, Color.Lerp(p.Shirt, Color.white, 0.3f), Part.Shirt);   // col
                    }

                    Flower(c, 5, 15);
                    Flower(c, 10, 16);
                    Flower(c, 7, 18);
                    break;
                case TopTee:
                    for (int x = 6; x <= 9; x++)
                    {
                        c.Paint(x, 13, Color.Lerp(p.Shirt, Color.black, 0.15f), Part.Shirt);
                    }

                    break;
                case TopTank:
                    for (int x = 6; x <= 9; x++)
                    {
                        c.Set(x, 13, p.Skin, Part.Skin, 0.75f);
                    }

                    c.Set(3, 13, p.Skin, Part.Skin, 1f);
                    c.Set(12, 13, p.Skin, Part.Skin, 0.72f);
                    break;
                default:
                    Stripes(c, p);
                    break;
            }
        }

        // ---------- Chapeaux ----------

        private static void DrawHatFront(Canvas c, Palette p, bool back)
        {
            switch (p.Hat)
            {
                case HatStraw:
                    DrawStraw(c, 8f, 8f, 7.0f, back);
                    for (int x = 3; x <= 12; x++)
                    {
                        c.Darken(x, 8, 0.12f);   // ombre du bord sur la tête
                    }

                    break;
                case HatCap:
                    c.Ellipse(8f, 6.9f, 3.9f, 2.9f, CapColor, Part.Hat, maxY: 7);
                    if (back)
                    {
                        c.Paint(7, 7, Color.Lerp(CapColor, Color.white, 0.5f), Part.Hat);   // réglage à l'arrière
                        c.Paint(8, 7, Color.Lerp(CapColor, Color.white, 0.5f), Part.Hat);
                    }
                    else
                    {
                        c.Paint(7, 5, StripeWhite, Part.Hat);   // petit logo
                        c.Paint(8, 5, StripeWhite, Part.Hat);
                        for (int x = 3; x <= 12; x++)
                        {
                            c.Set(x, 8, Color.Lerp(CapColor, Color.black, 0.15f), Part.Hat, x < 8 ? 1.12f : 0.95f);   // visière
                        }

                        for (int x = 4; x <= 11; x++)
                        {
                            c.Darken(x, 9, 0.12f);
                        }
                    }

                    break;
                case HatBob:
                    DrawBob(c, 8f, 8f, 5.3f);
                    break;
                case HatBandana:
                    DrawBandana(c, 8f, 3.9f);
                    if (back)
                    {
                        c.Set(7, 8, BandanaColor, Part.Hat, 0.9f);   // nœud
                        c.Set(8, 8, BandanaColor, Part.Hat, 0.8f);
                        c.Set(6, 9, BandanaColor, Part.Hat, 0.85f);
                        c.Set(9, 9, BandanaColor, Part.Hat, 0.7f);
                    }
                    else
                    {
                        c.Set(12, 8, BandanaColor, Part.Hat, 0.7f);
                        c.Set(13, 9, BandanaColor, Part.Hat, 0.62f);
                    }

                    break;
            }
        }

        /// <summary>Chapeau de paille : calotte en dôme, ruban rouge, large bord vu d'en haut, tressage.</summary>
        private static void DrawStraw(Canvas c, float crownX, float brimX, float brimRadius, bool bandAllAround)
        {
            c.Ellipse(crownX, 4.3f, 3.4f, 3.4f, StrawHat, Part.Hat);
            for (int x = 0; x < Width; x++)
            {
                if (c.Is(x, 4, Part.Hat))
                {
                    c.Paint(x, 4, HatBand);
                }
            }

            if (!bandAllAround)
            {
                c.Paint(Mathf.RoundToInt(crownX) + 2, 3, HatBand, Part.Hat);   // petit nœud du ruban
            }

            // Bord : dessus éclairé, tranche dans l'ombre.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 5; y <= 7; y++)
                {
                    float u = (x + 0.5f - brimX) / brimRadius;
                    float v = (y + 0.5f - 6.4f) / 1.6f;
                    if (u * u + v * v > 1f)
                    {
                        continue;
                    }

                    float light = y == 5 ? 1.15f : y == 6 ? 1f : 0.72f;
                    light -= Mathf.Max(0f, u) * 0.15f;
                    c.Set(x, y, StrawHat, Part.Hat, light);
                }
            }

            // Tressage de la paille.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (c.Is(x, y, Part.Hat) && (x + y) % 2 == 0 && c.Base[x, y] == StrawHat)
                    {
                        c.Paint(x, y, Color.Lerp(StrawHat, new Color(0.62f, 0.45f, 0.22f), 0.22f));
                    }
                }
            }
        }

        /// <summary>Bob de toile : calotte basse, bord court qui tombe un peu.</summary>
        private static void DrawBob(Canvas c, float crownX, float brimX, float brimRadius)
        {
            c.Ellipse(crownX, 5.9f, 3.3f, 2.5f, BobColor, Part.Hat, maxY: 7);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 7; y <= 8; y++)
                {
                    float u = (x + 0.5f - brimX) / brimRadius;
                    float v = (y + 0.5f - 8.0f) / 1.2f;
                    if (u * u + v * v <= 1f)
                    {
                        c.Set(x, y, BobColor, Part.Hat, (y == 7 ? 1.12f : 0.74f) - Mathf.Max(0f, u) * 0.12f);
                    }
                }
            }

            for (int x = 0; x < Width; x++)
            {
                if (c.Is(x, 6, Part.Hat))
                {
                    c.Paint(x, 6, Color.Lerp(BobColor, Color.black, 0.2f));   // couture au-dessus du bord
                }
            }

            for (int x = 3; x <= 12; x++)
            {
                c.Darken(x, 9, 0.1f);
            }
        }

        /// <summary>Bandana à pois noué sur la tête.</summary>
        private static void DrawBandana(Canvas c, float cx, float rx)
        {
            c.Ellipse(cx, 7.4f, rx, 2.2f, BandanaColor, Part.Hat, maxY: 7);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 5; y <= 7; y++)
                {
                    if (c.Is(x, y, Part.Hat) && (x * 2 + y) % 4 == 0)
                    {
                        c.Paint(x, y, StripeWhite);
                    }
                }
            }
        }

        // ---------- Profil (regarde vers la droite) ----------

        private static Color[,] DrawSide(Palette p, int frame)
        {
            var c = new Canvas();

            // Jambes : ensemble à l'arrêt, écartées ou levées pendant le pas. La jambe du fond est plus sombre.
            switch (frame)
            {
                case 1:
                    DrawLegSide(c, p, 5, 22, -0.2f, toeRight: false);
                    DrawLegSide(c, p, 9, 22, 0f, toeRight: true);
                    break;
                case 2:
                    DrawLegSide(c, p, 6, 22, -0.2f, toeRight: true);
                    DrawLegSide(c, p, 8, 21, 0f, toeRight: true);
                    break;
                default:
                    DrawLegSide(c, p, 7, 22, 0f, toeRight: true);
                    break;
            }

            c.Cylinder(5, 18, 10, 20, p.Shorts, Part.Shorts);
            for (int x = 5; x <= 10; x++)
            {
                c.Darken(x, 20, 0.1f);
            }

            c.Cylinder(5, 13, 10, 18, p.Shirt, Part.Shirt, roundTop: true);
            if (p.Top == TopHawaiian)
            {
                Flower(c, 6, 14);
                Flower(c, 9, 17);
            }
            else if (p.Top == TopTee)
            {
                c.Paint(8, 15, Color.Lerp(p.Shirt, Color.black, 0.18f), Part.Shirt);
            }

            // Tête de profil : oreille, œil, nez, joue.
            c.Ellipse(8.3f, 10.2f, 3.3f, 3.1f, p.Skin, Part.Skin);
            for (int x = 8; x <= 11; x++)
            {
                for (int y = 9; y <= 12; y++)
                {
                    if (c.Is(x, y, Part.Skin))
                    {
                        c.Light[x, y] = Mathf.Max(c.Light[x, y], 1f);
                    }
                }
            }

            DrawHairSide(c, p);
            c.Paint(7, 10, Color.Lerp(p.Skin, Blush, 0.35f), Part.Skin);   // oreille
            c.Paint(7, 11, Color.Lerp(p.Skin, Blush, 0.2f), Part.Skin);
            c.Flat(10, 9, Eye, Part.Skin);
            c.Flat(10, 10, EyeLow, Part.Skin);
            c.Set(12, 10, p.Skin, Part.Skin, 1.05f);              // nez
            c.Paint(10, 11, Color.Lerp(p.Skin, Blush, 0.5f));

            // Col.
            c.Set(10, 13, p.Skin, Part.Skin, 0.85f);
            if (p.Top == TopTank)
            {
                c.Set(9, 13, p.Skin, Part.Skin, 0.85f);
            }
            else
            {
                c.Paint(9, 13, Color.Lerp(p.Shirt, Color.white, 0.4f), Part.Shirt);
            }

            DrawArmSide(c, p, frame);
            if (p.Top == TopStriped)
            {
                Stripes(c, p);
            }

            if (p.HairStyle == HairLong)
            {
                for (int y = 12; y <= 15; y++)
                {
                    for (int x = 4; x <= 5; x++)
                    {
                        c.Set(x, y, p.Hair, Part.Hair, x == 4 ? 0.9f : 0.75f);
                    }
                }

                c.Paint(4, 14, p.HairLight);
            }

            DrawHatSide(c, p);
            return c.Render();
        }

        private static void DrawHairSide(Canvas c, Palette p)
        {
            int style = p.HairStyle;
            Color hair = style == HairShaved ? p.Shaved : p.Hair;

            // Arrière de la tête, dessus et frange.
            for (int x = 4; x <= 6; x++)
            {
                for (int y = 8; y <= 11; y++)
                {
                    if (c.Filled(x, y))
                    {
                        c.Cover(x, y, style != HairShaved && (x + y) % 3 == 0 ? p.HairLight : hair, Part.Hair);
                    }
                }
            }

            for (int x = 0; x < Width; x++)
            {
                c.Cover(x, 7, hair, Part.Hair);
            }

            for (int x = 7; x <= 10; x++)
            {
                c.Cover(x, 8, hair, Part.Hair);
            }

            if (style == HairSpiky)
            {
                c.Cover(11, 8, hair, Part.Hair);
            }

            if (style == HairBob || style == HairLong)
            {
                for (int y = 9; y <= 12; y++)
                {
                    c.Set(3, y, hair, Part.Hair, 0.9f);
                }

                for (int x = 4; x <= 6; x++)
                {
                    c.Set(x, 12, hair, Part.Hair, 0.8f);
                }

                c.Cover(7, 9, hair, Part.Hair);
            }
            else if (style == HairPonytail)
            {
                c.Set(3, 9, p.Shirt, Part.Hair, 1f);   // élastique
                c.Set(2, 10, hair, Part.Hair, 0.95f);
                c.Set(2, 11, hair, Part.Hair, 0.85f);
                c.Set(3, 12, hair, Part.Hair, 0.8f);
                c.Set(3, 11, hair, Part.Hair, 0.9f);
            }
            else if (style == HairBun)
            {
                if (p.HasHat)
                {
                    c.Ellipse(4.0f, 10.6f, 1.5f, 1.5f, hair, Part.Hair);
                }
                else
                {
                    c.Ellipse(5.8f, 6.4f, 1.8f, 1.6f, hair, Part.Hair);
                }
            }

            if (style == HairSpiky && !p.HasHat)
            {
                for (int x = 4; x <= 10; x++)
                {
                    c.Set(x, 6, hair, Part.Hair, x < 7 ? 0.9f : 1.1f);
                }

                foreach (int x in new[] { 4, 6, 8, 10 })
                {
                    c.Set(x, 5, hair, Part.Hair, 1.1f);
                }

                c.Set(3, 5, p.HairLight, Part.Hair, 1f);   // épis rabattus vers l'arrière
            }
        }

        private static void DrawHatSide(Canvas c, Palette p)
        {
            switch (p.Hat)
            {
                case HatStraw:
                    DrawStraw(c, 7.6f, 8.6f, 6.6f, bandAllAround: true);
                    for (int x = 4; x <= 12; x++)
                    {
                        c.Darken(x, 8, 0.12f);
                    }

                    break;
                case HatCap:
                    c.Ellipse(7.8f, 6.9f, 3.6f, 2.9f, CapColor, Part.Hat, maxY: 7);
                    for (int x = 9; x <= 13; x++)
                    {
                        c.Set(x, 8, Color.Lerp(CapColor, Color.black, 0.15f), Part.Hat, x < 12 ? 1.1f : 0.9f);   // visière
                    }

                    c.Paint(10, 6, StripeWhite, Part.Hat);
                    for (int x = 9; x <= 12; x++)
                    {
                        c.Darken(x, 9, 0.12f);
                    }

                    break;
                case HatBob:
                    DrawBob(c, 7.8f, 8.2f, 5.0f);
                    break;
                case HatBandana:
                    DrawBandana(c, 8.1f, 3.6f);
                    c.Set(4, 8, BandanaColor, Part.Hat, 0.85f);   // nœud et pans à l'arrière
                    c.Set(3, 9, BandanaColor, Part.Hat, 0.8f);
                    c.Set(3, 10, BandanaColor, Part.Hat, 0.7f);
                    break;
            }
        }

        /// <summary>Bras du premier plan : balancé vers l'avant (image 1), vers l'arrière (image 2) ou le long du corps.</summary>
        private static void DrawArmSide(Canvas c, Palette p, int frame)
        {
            int sleeveX = frame == 1 ? 8 : frame == 2 ? 6 : 7;
            int forearmX = frame == 1 ? 9 : frame == 2 ? 5 : 7;
            int handY = frame == 0 ? 18 : 17;
            bool bareArms = p.Top == TopTank;

            // Pli entre le bras et le torse.
            for (int y = 13; y <= 15; y++)
            {
                c.Darken(sleeveX - 1, y, 0.15f);
            }

            c.Cylinder(sleeveX, 13, sleeveX + 1, 15, bareArms ? p.Skin : p.Shirt, bareArms ? Part.Skin : Part.Shirt, roundTop: true, lightOffset: 0.05f);
            if (!bareArms)
            {
                c.Paint(sleeveX, 15, Color.Lerp(p.Shirt, Color.white, 0.25f), Part.Shirt);
                c.Paint(sleeveX + 1, 15, Color.Lerp(p.Shirt, Color.white, 0.25f), Part.Shirt);
            }

            // Avant-bras fin (1 pixel) et main un peu plus claire.
            int armX = frame == 1 ? forearmX + 1 : forearmX + (frame == 0 ? 1 : 0);
            c.Cylinder(armX, 16, armX, handY - 1, p.Skin, Part.Skin);
            int handX = frame == 1 ? armX + 1 : frame == 2 ? armX - 1 : armX;
            c.Set(handX, handY, p.Skin, Part.Skin, 1.1f);
        }

        private static void DrawLegSide(Canvas c, Palette p, int x, int lowest, float lightOffset, bool toeRight)
        {
            c.Cylinder(x, 21, x + 1, lowest, p.Skin, Part.Skin, lightOffset: lightOffset);
            int sole = lowest + 1;
            int x0 = toeRight ? x : x - 1;
            c.Cylinder(x0, sole, x0 + 2, sole, Sandal, Part.Sandal, lightOffset: lightOffset);
            c.Paint(x + 1, lowest, Color.Lerp(Sandal, p.Skin, 0.25f), Part.Skin);
        }
    }
}
