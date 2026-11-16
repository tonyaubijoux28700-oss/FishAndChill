using System.IO;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Outils pour dessiner du pixel art provisoire par code et l'enregistrer comme sprite Unity.
    /// Les tableaux de pixels sont indexés [x, y] avec y = 0 en BAS (convention des textures Unity).
    /// </summary>
    public static class PixelArt
    {
        public static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

        /// <summary>Écrit l'image, l'importe en sprite pixel art (filtre Point, sans compression) et renvoie le sprite.</summary>
        public static Sprite Save(string assetPath, Color[,] pixels, int pixelsPerUnit, Vector2 pivot)
        {
            return Save(assetPath, pixels, pixelsPerUnit, pivot, Vector4.zero, FilterMode.Point);
        }

        /// <summary>
        /// Variante pour l'interface : <paramref name="border"/> (gauche, bas, droite, haut, en pixels) rend le sprite
        /// étirable en 9 parties (coins nets à toute taille).
        /// </summary>
        private static bool SameBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static Sprite Save(string assetPath, Color[,] pixels, int pixelsPerUnit, Vector2 pivot, Vector4 border, FilterMode filter)
        {
            EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));

            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, pixels[x, y]);
                }
            }

            texture.Apply();
            string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            // Image inchangée : on ne la réécrit pas (plus rapide, et évite les fichiers verrouillés par l'import).
            bool unchanged = File.Exists(fullPath) && SameBytes(File.ReadAllBytes(fullPath), png);
            if (!unchanged)
            {
                File.WriteAllBytes(fullPath, png);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = filter;
            importer.mipmapEnabled = false;
            importer.spriteBorder = border;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        public static void Rect(Color[,] pixels, int xMin, int yMin, int xMax, int yMax, Color color)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    Set(pixels, x, y, color);
                }
            }
        }

        public static void Set(Color[,] pixels, int x, int y, Color color)
        {
            if (x >= 0 && y >= 0 && x < pixels.GetLength(0) && y < pixels.GetLength(1))
            {
                pixels[x, y] = color;
            }
        }

        public static bool IsFilled(Color[,] pixels, int x, int y)
        {
            return x >= 0 && y >= 0 && x < pixels.GetLength(0) && y < pixels.GetLength(1) && pixels[x, y].a > 0.01f;
        }

        /// <summary>Ajoute un contour d'un pixel autour de toutes les formes (pixels transparents voisins d'un pixel plein).</summary>
        public static void Outline(Color[,] pixels, Color color)
        {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            var outline = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!IsFilled(pixels, x, y) &&
                        (IsFilled(pixels, x + 1, y) || IsFilled(pixels, x - 1, y) || IsFilled(pixels, x, y + 1) || IsFilled(pixels, x, y - 1)))
                    {
                        outline[x, y] = true;
                    }
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (outline[x, y])
                    {
                        pixels[x, y] = color;
                    }
                }
            }
        }

        /// <summary>
        /// Contour sélectif (poissons, personnages) : chaque pixel du contour prend une version sombre et froide de la
        /// couleur voisine, plus claire sur le haut (côté lumière), mêlée à <paramref name="color"/>
        /// (<paramref name="ink"/> = part de cette couleur sur le côté ombre). Plus doux qu'un trait noir uniforme.
        /// </summary>
        public static void SelectiveOutline(Color[,] pixels, Color color, float ink = 0.45f)
        {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            var outline = new Color[width, height];
            var has = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (IsFilled(pixels, x, y))
                    {
                        continue;
                    }

                    Color sum = Color.clear;
                    int count = 0;
                    AddNeighbour(pixels, x + 1, y, ref sum, ref count);
                    AddNeighbour(pixels, x - 1, y, ref sum, ref count);
                    AddNeighbour(pixels, x, y + 1, ref sum, ref count);
                    AddNeighbour(pixels, x, y - 1, ref sum, ref count);
                    if (count == 0)
                    {
                        continue;
                    }

                    Color n = sum / count;
                    bool litSide = IsFilled(pixels, x, y - 1) && !IsFilled(pixels, x, y + 1);
                    Color dark = litSide
                        ? new Color(n.r * 0.50f, n.g * 0.52f, n.b * 0.60f)
                        : new Color(n.r * 0.28f, n.g * 0.30f, n.b * 0.40f);
                    Color o = Color.Lerp(dark, color, litSide ? ink * 0.45f : ink);
                    o.a = 1f;
                    outline[x, y] = o;
                    has[x, y] = true;
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (has[x, y])
                    {
                        pixels[x, y] = outline[x, y];
                    }
                }
            }
        }

        private static void AddNeighbour(Color[,] pixels, int x, int y, ref Color sum, ref int count)
        {
            if (IsFilled(pixels, x, y))
            {
                sum += pixels[x, y];
                count++;
            }
        }

        /// <summary>Relit un PNG du projet en tableau de pixels (x vers la droite, y vers le haut).</summary>
        public static Color[,] Load(string assetPath)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(System.IO.File.ReadAllBytes(assetPath));
            var pixels = new Color[texture.width, texture.height];
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    pixels[x, y] = texture.GetPixel(x, y);
                }
            }

            UnityEngine.Object.DestroyImmediate(texture);
            return pixels;
        }

        /// <summary>
        /// Réduit un dessin d'un facteur entier : chaque bloc devient un pixel (moyenne des pixels pleins),
        /// transparent si le bloc est trop vide. Sert aux petits poissons de l'aquarium.
        /// </summary>
        public static Color[,] Downscale(Color[,] source, int factor, float coverage = 0.4f)
        {
            int width = Mathf.CeilToInt(source.GetLength(0) / (float)factor);
            int height = Mathf.CeilToInt(source.GetLength(1) / (float)factor);
            var result = new Color[width, height];
            for (int bx = 0; bx < width; bx++)
            {
                for (int by = 0; by < height; by++)
                {
                    Color sum = Color.clear;
                    int filled = 0;
                    for (int x = bx * factor; x < (bx + 1) * factor; x++)
                    {
                        for (int y = by * factor; y < (by + 1) * factor; y++)
                        {
                            if (IsFilled(source, x, y) && source[x, y].a > 0.5f)
                            {
                                sum += source[x, y];
                                filled++;
                            }
                        }
                    }

                    if (filled >= factor * factor * coverage)
                    {
                        Color c = sum / filled;
                        c.a = 1f;
                        result[bx, by] = c;
                    }
                }
            }

            return result;
        }

        public static Color[,] MirrorX(Color[,] pixels)
        {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            var mirrored = new Color[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    mirrored[width - 1 - x, y] = pixels[x, y];
                }
            }

            return mirrored;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
