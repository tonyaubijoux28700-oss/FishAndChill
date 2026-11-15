using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pêcheur en pixel art 16×24, dans le même style que les poissons : formes rondes (ellipses, « cylindres »),
    /// éclairage par paliers (ombres froides, lumières chaudes, via <see cref="DetailedFish.Shade"/>) et contour
    /// sélectif teinté (<see cref="PixelArt.SelectiveOutline"/>), plus une ombre douce au sol.
    /// Chapeau de paille tressée, chemise hawaïenne à hibiscus, short, sandales.
    /// 4 directions × 3 images (0 = à l'arrêt, 1 et 2 = pas de marche). La gauche est le miroir de la droite.
    /// Le pivot est aux pieds : la position du joueur = l'endroit où il se tient.
    /// </summary>
    public static class CharacterArt
    {
        private const string Folder = "Assets/_Project/Art/Characters";
        private const int Width = 16;
        private const int Height = 24;
        private const int PixelsPerUnit = 16;

        /// <summary>Pivot aux pieds (bas de l'image).</summary>
        public static readonly Vector2 Pivot = new Vector2(0.5f, 1f / Height);

        private static readonly Color Hat = new Color(0.93f, 0.80f, 0.47f);
        private static readonly Color HatBand = new Color(0.82f, 0.24f, 0.22f);
        private static readonly Color Skin = new Color(0.97f, 0.76f, 0.58f);
        private static readonly Color Blush = new Color(0.96f, 0.52f, 0.50f);
        private static readonly Color Lips = new Color(0.78f, 0.40f, 0.34f);
        private static readonly Color Hair = new Color(0.38f, 0.23f, 0.14f);
        private static readonly Color Eye = new Color(0.10f, 0.07f, 0.07f);
        private static readonly Color EyeLow = new Color(0.30f, 0.20f, 0.16f);
        private static readonly Color Shirt = new Color(0.16f, 0.62f, 0.74f);
        private static readonly Color Petal = new Color(0.99f, 0.95f, 0.86f);
        private static readonly Color PetalPink = new Color(0.96f, 0.44f, 0.55f);
        private static readonly Color Shorts = new Color(0.96f, 0.55f, 0.22f);
        private static readonly Color Sandal = new Color(0.52f, 0.32f, 0.18f);
        private static readonly Color OutlineColor = new Color(0.14f, 0.09f, 0.10f);
        private static readonly Color GroundShadow = new Color(0.05f, 0.10f, 0.06f, 0.24f);

        /// <summary>Lumière d'en haut à gauche, un peu de face.</summary>
        private static readonly Vector3 LightDirection = new Vector3(-0.45f, 0.70f, 0.55f).normalized;

        public sealed class Sprites
        {
            public Sprite[] Down;
            public Sprite[] Up;
            public Sprite[] Left;
            public Sprite[] Right;
        }

        public static Sprites Generate()
        {
            var result = new Sprites { Down = new Sprite[3], Up = new Sprite[3], Left = new Sprite[3], Right = new Sprite[3] };
            for (int frame = 0; frame < 3; frame++)
            {
                result.Down[frame] = Save($"Pecheur_Bas_{frame}", DrawFront(frame, back: false));
                result.Up[frame] = Save($"Pecheur_Haut_{frame}", DrawFront(frame, back: true));
                Color[,] side = DrawSide(frame);
                result.Right[frame] = Save($"Pecheur_Droite_{frame}", side);
                result.Left[frame] = Save($"Pecheur_Gauche_{frame}", PixelArt.MirrorX(side));
            }

            return result;
        }

        private static Sprite Save(string name, Color[,] pixels)
        {
            return PixelArt.Save($"{Folder}/{name}.png", pixels, PixelsPerUnit, Pivot);
        }

        // ---------- Toile : couleur de base + lumière par pixel ----------
        // Les dessins utilisent y compté depuis le HAUT (ligne 0 = haut du chapeau, ligne 23 = semelles).

        private enum Part { None, Hat, Hair, Skin, Shirt, Shorts, Sandal }

        private sealed class Canvas
        {
            public readonly Color[,] Base = new Color[Width, Height];
            public readonly float[,] Light = new float[Width, Height];
            public readonly Part[,] Parts = new Part[Width, Height];

            public bool Inside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

            public bool Is(int x, int y, Part part) => Inside(x, y) && Parts[x, y] == part;

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

            /// <summary>Change la couleur sans toucher à la lumière (motifs, détails ombrés comme le support).</summary>
            public void Paint(int x, int y, Color color, Part only = Part.None)
            {
                if (Inside(x, y) && Parts[x, y] != Part.None && (only == Part.None || Parts[x, y] == only))
                {
                    Base[x, y] = color;
                }
            }

            /// <summary>Pixel à couleur fixe (yeux) : pas d'éclairage.</summary>
            public void Flat(int x, int y, Color color, Part part)
            {
                Set(x, y, color, part, 1f);
            }

            public void Darken(int x, int y, float amount)
            {
                if (Inside(x, y) && Parts[x, y] != Part.None)
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

            /// <summary>Ellipse éclairée comme une boule (cx, cy : centre en pixels, y depuis le haut).</summary>
            public void Ellipse(float cx, float cy, float rx, float ry, Color color, Part part)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
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
                            p[x, Height - 1 - y] = DetailedFish.Shade(Base[x, y], Light[x, y]);
                        }
                    }
                }

                PixelArt.SelectiveOutline(p, OutlineColor, 0.6f);
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

        /// <summary>Ombre douce et transparente au sol, autour des pieds (dessinée après le contour).</summary>
        private static void AddGroundShadow(Color[,] p)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    float u = (x + 0.5f - 8f) / 5.5f;
                    float v = (y + 0.5f - 0.6f) / 1.7f;
                    if (u * u + v * v <= 1f && !PixelArt.IsFilled(p, x, y))
                    {
                        p[x, y] = GroundShadow;
                    }
                }
            }
        }

        // ---------- Pièces communes ----------

        /// <summary>Chapeau de paille : calotte en dôme, ruban rouge, large bord vu d'en haut, tressage.</summary>
        private static void DrawHat(Canvas c, float crownX, float brimX, float brimRadius, bool bandAllAround)
        {
            c.Ellipse(crownX, 4.3f, 3.4f, 3.4f, Hat, Part.Hat);

            // Ruban juste au-dessus du bord.
            for (int x = 0; x < Width; x++)
            {
                if (c.Is(x, 4, Part.Hat))
                {
                    c.Paint(x, 4, HatBand);
                }
            }

            if (!bandAllAround)
            {
                // De face, petit nœud du ruban sur le côté.
                c.Paint(Mathf.RoundToInt(crownX) + 2, 3, HatBand, Part.Hat);
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
                    light -= Mathf.Max(0f, u) * 0.15f;   // côté droit un peu plus sombre
                    c.Set(x, y, Hat, Part.Hat, light);
                }
            }

            // Tressage de la paille.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (c.Is(x, y, Part.Hat) && (x + y) % 2 == 0 && c.Base[x, y] == Hat)
                    {
                        c.Paint(x, y, Color.Lerp(Hat, new Color(0.62f, 0.45f, 0.22f), 0.22f));
                    }
                }
            }
        }

        /// <summary>Hibiscus : cœur rose, 4 pétales crème, seulement sur la chemise (ombré comme elle).</summary>
        private static void Flower(Canvas c, int x, int y)
        {
            c.Paint(x, y, PetalPink, Part.Shirt);
            c.Paint(x - 1, y, Petal, Part.Shirt);
            c.Paint(x + 1, y, Petal, Part.Shirt);
            c.Paint(x, y - 1, Petal, Part.Shirt);
            c.Paint(x, y + 1, Color.Lerp(Petal, PetalPink, 0.35f), Part.Shirt);
        }

        // ---------- Face et dos ----------

        private static Color[,] DrawFront(int frame, bool back)
        {
            var c = new Canvas();

            // Jambes et sandales (une jambe se lève à chaque pas).
            DrawLegFront(c, 5, frame == 1, strapRight: false);
            DrawLegFront(c, 9, frame == 2, strapRight: true);

            // Short, avec l'entrejambe ouvert et l'ourlet plus sombre.
            c.Cylinder(4, 18, 11, 20, Shorts, Part.Shorts);
            c.Clear(7, 20);
            c.Clear(8, 20);
            for (int x = 4; x <= 11; x++)
            {
                c.Darken(x, 20, 0.1f);
            }

            // Chemise hawaïenne, portée par-dessus le short.
            c.Cylinder(3, 13, 12, 18, Shirt, Part.Shirt, roundTop: true);

            // Bras qui se balancent : manche courte, avant-bras, main.
            int leftSwing = frame == 1 ? -1 : 0;
            int rightSwing = frame == 2 ? -1 : 0;
            DrawArmFront(c, 2, leftSwing, -0.7f);
            DrawArmFront(c, 13, rightSwing, 0.7f);

            // Tête.
            c.Ellipse(8f, 10.2f, 3.75f, 3.1f, back ? Hair : Skin, back ? Part.Hair : Part.Skin);
            if (back)
            {
                // Mèches et nuque.
                for (int x = 4; x <= 11; x++)
                {
                    for (int y = 8; y <= 12; y++)
                    {
                        if (c.Is(x, y, Part.Hair) && (x + y * 2) % 3 == 0)
                        {
                            c.Paint(x, y, Color.Lerp(Hair, new Color(0.62f, 0.42f, 0.26f), 0.5f));
                        }
                    }
                }

                c.Set(7, 12, Skin, Part.Skin, 0.7f);
                c.Set(8, 12, Skin, Part.Skin, 0.7f);

                // Col de la chemise, vu de dos.
                for (int x = 6; x <= 9; x++)
                {
                    c.Paint(x, 13, Color.Lerp(Shirt, Color.white, 0.3f), Part.Shirt);
                }

                Flower(c, 5, 15);
                Flower(c, 10, 16);
                Flower(c, 7, 18);
            }
            else
            {
                DrawFace(c);

                // Col ouvert en V et boutonnage.
                c.Set(7, 13, Skin, Part.Skin, 0.8f);
                c.Set(8, 13, Skin, Part.Skin, 0.7f);
                c.Paint(6, 13, Color.Lerp(Shirt, Color.white, 0.4f), Part.Shirt);
                c.Paint(9, 13, Color.Lerp(Shirt, Color.white, 0.4f), Part.Shirt);
                for (int y = 14; y <= 18; y++)
                {
                    c.Darken(8, y, 0.12f);
                }

                Flower(c, 5, 15);
                Flower(c, 10, 16);
                Flower(c, 6, 18);
            }

            DrawHat(c, 8f, 8f, 7.0f, back);

            // Ombre du bord du chapeau sur le haut de la tête.
            for (int x = 3; x <= 12; x++)
            {
                c.Darken(x, 8, 0.12f);
            }

            return c.Render();
        }

        private static void DrawFace(Canvas c)
        {
            // Frange (raie au milieu) et favoris.
            foreach (int x in new[] { 4, 5, 6, 9, 10, 11 })
            {
                c.Paint(x, 8, Hair, Part.Skin);
            }

            c.Set(4, 9, Hair, Part.Hair, 0.7f);
            c.Set(11, 9, Hair, Part.Hair, 0.6f);
            c.Paint(5, 8, Color.Lerp(Hair, new Color(0.62f, 0.42f, 0.26f), 0.5f));

            // Yeux de deux pixels (le bas plus clair), joues roses, petit sourire.
            c.Flat(6, 9, Eye, Part.Skin);
            c.Flat(6, 10, EyeLow, Part.Skin);
            c.Flat(9, 9, Eye, Part.Skin);
            c.Flat(9, 10, EyeLow, Part.Skin);
            c.Paint(5, 11, Color.Lerp(Skin, Blush, 0.5f));
            c.Paint(10, 11, Color.Lerp(Skin, Blush, 0.5f));

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

            c.Paint(7, 12, Color.Lerp(Skin, Lips, 0.35f));
        }

        private static void DrawArmFront(Canvas c, int x, int swing, float side)
        {
            for (int y = 13; y <= 18; y++)
            {
                int row = y + swing;
                bool sleeve = y <= 15;
                bool hand = y == 18;
                float light = side < 0 ? (hand ? 1.1f : 1f) : (hand ? 0.85f : 0.72f);
                c.Set(x, row, sleeve ? Shirt : Skin, sleeve ? Part.Shirt : Part.Skin, light);
            }

            // Ourlet de manche plus clair.
            c.Paint(x, 15 + swing, Color.Lerp(Shirt, Color.white, 0.25f), Part.Shirt);
        }

        private static void DrawLegFront(Canvas c, int x, bool lifted, bool strapRight)
        {
            int bottom = lifted ? 22 : 23;
            c.Cylinder(x, 21, x + 1, bottom - 1, Skin, Part.Skin);
            c.Cylinder(x, bottom, x + 1, bottom, Sandal, Part.Sandal, lightOffset: lifted ? -0.1f : 0f);

            // Lanière de la sandale sur le pied.
            c.Paint(strapRight ? x + 1 : x, bottom - 1, Color.Lerp(Sandal, Skin, 0.25f), Part.Skin);
        }

        // ---------- Profil (regarde vers la droite) ----------

        private static Color[,] DrawSide(int frame)
        {
            var c = new Canvas();

            // Jambes : ensemble à l'arrêt, écartées ou levées pendant le pas. La jambe du fond est plus sombre.
            switch (frame)
            {
                case 1:
                    DrawLegSide(c, 5, 22, -0.2f, toeRight: false);
                    DrawLegSide(c, 9, 22, 0f, toeRight: true);
                    break;
                case 2:
                    DrawLegSide(c, 6, 22, -0.2f, toeRight: true);
                    DrawLegSide(c, 8, 21, 0f, toeRight: true);
                    break;
                default:
                    DrawLegSide(c, 7, 22, 0f, toeRight: true);
                    break;
            }

            c.Cylinder(5, 18, 10, 20, Shorts, Part.Shorts);
            for (int x = 5; x <= 10; x++)
            {
                c.Darken(x, 20, 0.1f);
            }

            c.Cylinder(5, 13, 10, 18, Shirt, Part.Shirt, roundTop: true);
            Flower(c, 6, 14);
            Flower(c, 9, 17);

            // Tête de profil : cheveux à l'arrière, oreille, œil, nez, joue.
            c.Ellipse(8.3f, 10.2f, 3.3f, 3.1f, Skin, Part.Skin);
            for (int x = 4; x <= 6; x++)
            {
                for (int y = 8; y <= 11; y++)
                {
                    if (c.Is(x, y, Part.Skin))
                    {
                        c.Paint(x, y, (x + y) % 3 == 0 ? Color.Lerp(Hair, new Color(0.62f, 0.42f, 0.26f), 0.5f) : Hair);
                    }
                }
            }

            for (int x = 7; x <= 10; x++)
            {
                c.Paint(x, 8, Hair, Part.Skin);
            }

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

            c.Paint(7, 10, Color.Lerp(Skin, Blush, 0.35f));   // oreille
            c.Paint(7, 11, Color.Lerp(Skin, Blush, 0.2f));
            c.Flat(10, 9, Eye, Part.Skin);
            c.Flat(10, 10, EyeLow, Part.Skin);
            c.Set(12, 10, Skin, Part.Skin, 1.05f);              // nez
            c.Paint(10, 11, Color.Lerp(Skin, Blush, 0.5f));

            // Col.
            c.Set(10, 13, Skin, Part.Skin, 0.85f);
            c.Paint(9, 13, Color.Lerp(Shirt, Color.white, 0.4f), Part.Shirt);

            DrawArmSide(c, frame);
            DrawHat(c, 7.6f, 8.6f, 6.6f, bandAllAround: true);
            for (int x = 4; x <= 12; x++)
            {
                c.Darken(x, 8, 0.12f);
            }

            return c.Render();
        }

        /// <summary>Bras du premier plan : balancé vers l'avant (image 1), vers l'arrière (image 2) ou le long du corps.</summary>
        private static void DrawArmSide(Canvas c, int frame)
        {
            int sleeveX = frame == 1 ? 8 : frame == 2 ? 6 : 7;
            int forearmX = frame == 1 ? 9 : frame == 2 ? 5 : 7;
            int handY = frame == 0 ? 18 : 17;

            // Pli entre le bras et le torse.
            for (int y = 13; y <= 15; y++)
            {
                c.Darken(sleeveX - 1, y, 0.15f);
            }

            c.Cylinder(sleeveX, 13, sleeveX + 1, 15, Shirt, Part.Shirt, roundTop: true, lightOffset: 0.05f);
            c.Paint(sleeveX, 15, Color.Lerp(Shirt, Color.white, 0.25f), Part.Shirt);
            c.Paint(sleeveX + 1, 15, Color.Lerp(Shirt, Color.white, 0.25f), Part.Shirt);
            // Avant-bras fin (1 pixel) et main un peu plus claire.
            int armX = frame == 1 ? forearmX + 1 : forearmX + (frame == 0 ? 1 : 0);
            c.Cylinder(armX, 16, armX, handY - 1, Skin, Part.Skin);
            int handX = frame == 1 ? armX + 1 : frame == 2 ? armX - 1 : armX;
            c.Set(handX, handY, Skin, Part.Skin, 1.1f);
        }

        private static void DrawLegSide(Canvas c, int x, int lowest, float lightOffset, bool toeRight)
        {
            c.Cylinder(x, 21, x + 1, lowest, Skin, Part.Skin, lightOffset: lightOffset);
            int sole = lowest + 1;
            int x0 = toeRight ? x : x - 1;
            c.Cylinder(x0, sole, x0 + 2, sole, Sandal, Part.Sandal, lightOffset: lightOffset);
            c.Paint(x + 1, lowest, Color.Lerp(Sandal, Skin, 0.25f), Part.Skin);
        }
    }
}
