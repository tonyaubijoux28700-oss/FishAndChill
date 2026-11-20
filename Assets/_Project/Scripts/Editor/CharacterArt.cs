using FishAndChill.Player;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Pêcheur en pixel art 16×24, dans le même style que les poissons : formes rondes (ellipses, « cylindres »),
    /// éclairage par paliers (ombres froides, lumières chaudes, via <see cref="DetailedFish.Shade"/>) et contour
    /// sélectif teinté (<see cref="PixelArt.SelectiveOutline"/>), plus une ombre douce au sol.
    /// Le pêcheur est dessiné par CharacterPainter (aussi utilisé en jeu pour l'apparence choisie) ; ici, version par défaut.
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

        private static readonly Color Blush = new Color(0.96f, 0.52f, 0.50f);
        private static readonly Color Lips = new Color(0.78f, 0.40f, 0.34f);
        private static readonly Color Eye = new Color(0.10f, 0.07f, 0.07f);
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
            var look = new CharacterLook(); // apparence par defaut ; en jeu, le pecheur est redessine selon la partie
            for (int frame = 0; frame < 3; frame++)
            {
                result.Down[frame] = Save($"Pecheur_Bas_{frame}", CharacterPainter.Front(look, frame, back: false));
                result.Up[frame] = Save($"Pecheur_Haut_{frame}", CharacterPainter.Front(look, frame, back: true));
                Color[,] side = CharacterPainter.Side(look, frame);
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

        // ---------- Marchand ----------

        private static readonly Color MerchantSkin = new Color(0.88f, 0.64f, 0.46f);
        private static readonly Color GreyHair = new Color(0.84f, 0.84f, 0.82f);
        private static readonly Color SailorWhite = new Color(0.96f, 0.96f, 0.93f);
        private static readonly Color SailorBlue = new Color(0.20f, 0.36f, 0.68f);
        private static readonly Color Apron = new Color(0.95f, 0.91f, 0.82f);
        private static readonly Color Trousers = new Color(0.32f, 0.27f, 0.24f);
        private static readonly Color Boot = new Color(0.30f, 0.20f, 0.14f);

        /// <summary>
        /// Marchand de face, 16×24, dans le même style que le pêcheur : vieux marin au bob blanc à ruban bleu,
        /// grosse moustache grise, marinière rayée, tablier crème à poche (petit poisson brodé), pantalon et bottes.
        /// </summary>
        public static Color[,] Merchant()
        {
            var c = new Canvas();

            // Bottes et pantalon.
            c.Cylinder(5, 21, 6, 22, Trousers, Part.Shorts);
            c.Cylinder(9, 21, 10, 22, Trousers, Part.Shorts);
            c.Cylinder(5, 23, 6, 23, Boot, Part.Sandal);
            c.Cylinder(9, 23, 10, 23, Boot, Part.Sandal);
            c.Cylinder(4, 19, 11, 20, Trousers, Part.Shorts);
            c.Clear(7, 20);
            c.Clear(8, 20);

            // Marinière (un peu de ventre), rayures ombrées comme le tissu.
            c.Cylinder(3, 13, 12, 19, SailorBlue, Part.Shirt, roundTop: true);
            for (int x = 3; x <= 12; x++)
            {
                for (int y = 13; y <= 19; y++)
                {
                    if (y % 2 == 0)
                    {
                        c.Paint(x, y, SailorWhite, Part.Shirt);
                    }
                }
            }

            // Bras le long du corps : manches rayées, avant-bras hâlés, mains.
            foreach ((int x, float light) in new[] { (2, 1f), (13, 0.72f) })
            {
                for (int y = 13; y <= 18; y++)
                {
                    bool sleeve = y <= 16;
                    Color color = sleeve ? (y % 2 == 0 ? SailorWhite : SailorBlue) : MerchantSkin;
                    c.Set(x, y, color, sleeve ? Part.Shirt : Part.Skin, y == 18 ? light + 0.1f : light);
                }
            }

            // Tablier à bavette, bretelles, cordon à la taille, poche avec un petit poisson brodé.
            c.Cylinder(5, 14, 10, 20, Apron, Part.Hat, lightOffset: 0.05f);
            c.Set(5, 13, Apron, Part.Hat, 0.95f);
            c.Set(10, 13, Apron, Part.Hat, 0.8f);
            for (int x = 3; x <= 12; x++)
            {
                if (x < 5 || x > 10)
                {
                    c.Paint(x, 16, Color.Lerp(Apron, Color.black, 0.1f), Part.Shirt);
                }
            }

            for (int x = 6; x <= 9; x++)
            {
                c.Darken(x, 17, 0.12f);
            }

            c.Paint(7, 18, SailorBlue, Part.Hat);
            c.Paint(8, 18, SailorBlue, Part.Hat);
            c.Paint(6, 18, Color.Lerp(SailorBlue, Apron, 0.4f), Part.Hat);

            // Tête : visage hâlé, cheveux gris sur les côtés, sourcils broussailleux, nez rond, moustache.
            c.Ellipse(8f, 9.8f, 3.75f, 3.1f, MerchantSkin, Part.Skin);
            for (int x = 5; x <= 10; x++)
            {
                for (int y = 8; y <= 11; y++)
                {
                    if (c.Is(x, y, Part.Skin))
                    {
                        c.Light[x, y] = Mathf.Max(c.Light[x, y], 1f);
                    }
                }
            }

            foreach (int y in new[] { 8, 9, 10 })
            {
                c.Set(4, y, GreyHair, Part.Hair, 0.95f);
                c.Set(11, y, GreyHair, Part.Hair, 0.75f);
            }

            // Sourcils : un pixel gris au-dessus de chaque œil.
            c.Paint(6, 8, Color.Lerp(GreyHair, MerchantSkin, 0.2f));
            c.Paint(9, 8, Color.Lerp(GreyHair, MerchantSkin, 0.2f));
            c.Flat(6, 9, Eye, Part.Skin);
            c.Flat(9, 9, Eye, Part.Skin);
            c.Paint(7, 10, Color.Lerp(MerchantSkin, Blush, 0.45f));
            c.Paint(8, 10, Color.Lerp(MerchantSkin, Blush, 0.3f));
            c.Paint(5, 10, Color.Lerp(MerchantSkin, Blush, 0.35f));
            c.Paint(10, 10, Color.Lerp(MerchantSkin, Blush, 0.35f));
            for (int x = 5; x <= 10; x++)
            {
                c.Paint(x, 11, GreyHair);
            }

            // Moustache en guidon : les pointes tombent, le menton reste visible au milieu.
            c.Paint(5, 12, GreyHair);
            c.Paint(10, 12, GreyHair);
            c.Paint(7, 12, Color.Lerp(MerchantSkin, Lips, 0.3f));
            c.Paint(8, 12, Color.Lerp(MerchantSkin, Lips, 0.3f));

            // Bob de marin : calotte blanche, ruban bleu, petite ancre, bord court.
            c.Ellipse(8f, 4.2f, 3.5f, 3.0f, SailorWhite, Part.Hat);
            for (int x = 0; x < Width; x++)
            {
                if (c.Is(x, 5, Part.Hat))
                {
                    c.Paint(x, 5, SailorBlue);
                }
            }

            c.Paint(8, 3, SailorBlue, Part.Hat);
            c.Paint(8, 2, Color.Lerp(SailorBlue, SailorWhite, 0.4f), Part.Hat);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 6; y <= 7; y++)
                {
                    float u = (x + 0.5f - 8f) / 5.4f;
                    float v = (y + 0.5f - 6.7f) / 1.2f;
                    if (u * u + v * v <= 1f)
                    {
                        c.Set(x, y, SailorWhite, Part.Hat, (y == 6 ? 1.05f : 0.78f) - Mathf.Max(0f, u) * 0.12f);
                    }
                }
            }

            for (int x = 4; x <= 11; x++)
            {
                c.Darken(x, 8, 0.1f);
            }

            return c.Render();
        }

        private static readonly Color ScientistSkin = new Color(0.72f, 0.50f, 0.34f);
        private static readonly Color DarkHair = new Color(0.16f, 0.11f, 0.09f);
        private static readonly Color LabCoat = new Color(0.97f, 0.97f, 0.95f);
        private static readonly Color Blouse = new Color(0.20f, 0.62f, 0.64f);
        private static readonly Color Slacks = new Color(0.22f, 0.26f, 0.40f);
        private static readonly Color GlassesFrame = new Color(0.30f, 0.30f, 0.34f);

        /// <summary>
        /// La scientifique du laboratoire, de face, 16×24, même style que le pêcheur : chignon brun foncé piqué d'une
        /// fleur de tiaré, lunettes rondes, blouse blanche ouverte sur un haut turquoise, stylo dans la poche, pantalon.
        /// </summary>
        public static Color[,] Scientist()
        {
            var c = new Canvas();

            // Chaussures et pantalon.
            c.Cylinder(5, 21, 6, 22, Slacks, Part.Shorts);
            c.Cylinder(9, 21, 10, 22, Slacks, Part.Shorts);
            c.Cylinder(5, 23, 6, 23, Boot, Part.Sandal);
            c.Cylinder(9, 23, 10, 23, Boot, Part.Sandal);

            // Blouse blanche longue, ouverte sur le haut turquoise.
            c.Cylinder(3, 13, 12, 20, LabCoat, Part.Shirt, roundTop: true);
            for (int y = 13; y <= 18; y++)
            {
                c.Paint(7, y, Blouse, Part.Shirt);
                c.Paint(8, y, Blouse, Part.Shirt);
            }

            for (int y = 13; y <= 20; y++)
            {
                c.Darken(6, y, 0.12f);   // pli des revers
                c.Darken(9, y, 0.06f);
            }

            c.Clear(7, 20);
            c.Clear(8, 20);
            c.Paint(4, 17, Color.Lerp(LabCoat, Color.black, 0.14f), Part.Shirt);   // poches
            c.Paint(11, 17, Color.Lerp(LabCoat, Color.black, 0.14f), Part.Shirt);
            c.Paint(10, 14, new Color(0.24f, 0.40f, 0.80f), Part.Shirt);           // stylo

            // Bras dans les manches de la blouse, mains.
            foreach ((int x, float light) in new[] { (2, 1f), (13, 0.72f) })
            {
                for (int y = 13; y <= 18; y++)
                {
                    bool sleeve = y <= 17;
                    c.Set(x, y, sleeve ? LabCoat : ScientistSkin, sleeve ? Part.Shirt : Part.Skin, y == 18 ? light + 0.1f : light);
                }
            }

            // Tête : visage, cheveux tirés, lunettes rondes, sourire.
            c.Ellipse(8f, 9.8f, 3.75f, 3.1f, ScientistSkin, Part.Skin);
            for (int x = 5; x <= 10; x++)
            {
                for (int y = 8; y <= 11; y++)
                {
                    if (c.Is(x, y, Part.Skin))
                    {
                        c.Light[x, y] = Mathf.Max(c.Light[x, y], 1f);
                    }
                }
            }

            c.Ellipse(8f, 6.3f, 3.9f, 2.1f, DarkHair, Part.Hair);                  // cheveux
            foreach (int y in new[] { 8, 9, 10 })
            {
                c.Set(4, y, DarkHair, Part.Hair, 0.95f);
                c.Set(11, y, DarkHair, Part.Hair, 0.75f);
            }

            c.Ellipse(8f, 3.6f, 1.9f, 1.5f, DarkHair, Part.Hair);                  // chignon
            c.Flat(10, 4, new Color(0.98f, 0.97f, 0.92f), Part.Hat);                 // fleur de tiaré
            c.Flat(11, 5, new Color(0.98f, 0.97f, 0.92f), Part.Hat);
            c.Flat(10, 5, new Color(1f, 0.84f, 0.30f), Part.Hat);

            c.Flat(6, 9, Eye, Part.Skin);
            c.Flat(9, 9, Eye, Part.Skin);
            c.Paint(5, 9, GlassesFrame);
            c.Paint(7, 9, GlassesFrame);
            c.Paint(8, 9, GlassesFrame);
            c.Paint(10, 9, GlassesFrame);
            c.Paint(5, 10, Color.Lerp(ScientistSkin, Blush, 0.35f));
            c.Paint(10, 10, Color.Lerp(ScientistSkin, Blush, 0.35f));
            c.Paint(7, 11, Color.Lerp(ScientistSkin, Lips, 0.55f));
            c.Paint(8, 11, Color.Lerp(ScientistSkin, Lips, 0.55f));
            return c.Render();
        }

        private static readonly Color KeeperSkin = new Color(0.94f, 0.76f, 0.62f);
        private static readonly Color GingerHair = new Color(0.86f, 0.44f, 0.18f);
        private static readonly Color KeeperShirt = new Color(0.98f, 0.84f, 0.36f);
        private static readonly Color KeeperApron = new Color(0.36f, 0.66f, 0.56f);
        private static readonly Color Overalls = new Color(0.76f, 0.66f, 0.50f);

        /// <summary>
        /// Le vendeur de l'animalerie, de face, 16×24, même style que le pêcheur : cheveux roux en bataille, taches de
        /// rousseur, t-shirt jaune sous un tablier vert d'eau marqué d'une patte crème, pantalon beige.
        /// </summary>
        public static Color[,] PetKeeper()
        {
            var c = new Canvas();

            // Chaussures et pantalon.
            c.Cylinder(5, 21, 6, 22, Overalls, Part.Shorts);
            c.Cylinder(9, 21, 10, 22, Overalls, Part.Shorts);
            c.Cylinder(5, 23, 6, 23, Boot, Part.Sandal);
            c.Cylinder(9, 23, 10, 23, Boot, Part.Sandal);

            // T-shirt jaune et tablier vert d'eau, patte crème sur le tablier.
            c.Cylinder(3, 13, 12, 20, KeeperShirt, Part.Shirt, roundTop: true);
            for (int x = 5; x <= 10; x++)
            {
                for (int y = 14; y <= 20; y++)
                {
                    c.Paint(x, y, KeeperApron, Part.Shirt);
                }
            }

            c.Paint(4, 13, KeeperApron, Part.Shirt);    // bretelles
            c.Paint(11, 13, KeeperApron, Part.Shirt);
            Color pawColor = new Color(0.97f, 0.91f, 0.76f);
            c.Paint(7, 18, pawColor, Part.Shirt);
            c.Paint(8, 18, pawColor, Part.Shirt);
            c.Paint(6, 16, pawColor, Part.Shirt);
            c.Paint(9, 16, pawColor, Part.Shirt);

            // Bras nus, mains.
            foreach ((int x, float light) in new[] { (2, 1f), (13, 0.72f) })
            {
                for (int y = 13; y <= 18; y++)
                {
                    bool sleeve = y <= 14;
                    c.Set(x, y, sleeve ? KeeperShirt : KeeperSkin, sleeve ? Part.Shirt : Part.Skin, y == 18 ? light + 0.1f : light);
                }
            }

            // Tête : visage, cheveux roux en bataille, taches de rousseur, grand sourire.
            c.Ellipse(8f, 9.8f, 3.75f, 3.1f, KeeperSkin, Part.Skin);
            for (int x = 5; x <= 10; x++)
            {
                for (int y = 8; y <= 11; y++)
                {
                    if (c.Is(x, y, Part.Skin))
                    {
                        c.Light[x, y] = Mathf.Max(c.Light[x, y], 1f);
                    }
                }
            }

            c.Ellipse(8f, 6.2f, 4.1f, 2.3f, GingerHair, Part.Hair);
            foreach ((int x, int y) in new[] { (4, 5), (6, 3), (9, 3), (11, 4), (12, 6) })
            {
                c.Set(x, y, GingerHair, Part.Hair, 1.05f);   // mèches rebelles
            }

            c.Set(4, 8, GingerHair, Part.Hair, 0.95f);
            c.Set(11, 8, GingerHair, Part.Hair, 0.75f);

            c.Flat(6, 9, Eye, Part.Skin);
            c.Flat(9, 9, Eye, Part.Skin);
            Color freckle = Color.Lerp(KeeperSkin, GingerHair, 0.45f);
            c.Paint(5, 10, freckle);
            c.Paint(10, 10, freckle);
            c.Paint(6, 11, Color.Lerp(KeeperSkin, Blush, 0.35f));
            c.Paint(9, 11, Color.Lerp(KeeperSkin, Blush, 0.35f));
            c.Paint(7, 11, Color.Lerp(KeeperSkin, Lips, 0.6f));
            c.Paint(8, 11, Color.Lerp(KeeperSkin, Lips, 0.6f));
            return c.Render();
        }
    }
}
