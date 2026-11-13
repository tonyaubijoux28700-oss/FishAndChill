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
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
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
