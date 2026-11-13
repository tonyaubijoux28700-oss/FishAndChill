using System.IO;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Construit l'île de départ : tuiles pixel art provisoires, Tilemaps eau + terre, point d'apparition,
    /// caméra qui suit le joueur. Une fois générée, l'île peut être retouchée à la main (Tile Palette).
    /// </summary>
    public static class MapSetup
    {
        private const string TilesFolder = "Assets/_Project/Art/Tiles";
        private const int TileSize = 16;

        private static readonly IslandGenerator.Settings IslandSettings = new IslandGenerator.Settings
        {
            Width = 96,
            Height = 72,
            Seed = 1234,
        };

        /// <summary>Construit (ou reconstruit) l'île dans la scène ouverte et y branche joueur, pêche et caméra.</summary>
        public static void BuildIsland(GameObject player, FishingController controller)
        {
            Tile water = EnsureTile("Eau", new Color(0.17f, 0.45f, 0.62f), new Color(0.30f, 0.60f, 0.76f), new Color(0.13f, 0.38f, 0.55f), 11, waves: true);
            Tile sand = EnsureTile("Sable", new Color(0.87f, 0.78f, 0.55f), new Color(0.93f, 0.86f, 0.66f), new Color(0.78f, 0.68f, 0.45f), 22, waves: false);
            Tile grass = EnsureTile("Herbe", new Color(0.36f, 0.62f, 0.30f), new Color(0.45f, 0.72f, 0.36f), new Color(0.28f, 0.52f, 0.24f), 33, waves: false);

            GameObject mapObject = GameObject.Find("Carte");
            if (mapObject == null)
            {
                mapObject = new GameObject("Carte", typeof(Grid));
            }

            Tilemap waterMap = EnsureTilemap(mapObject.transform, "Eau", -20);
            Tilemap landMap = EnsureTilemap(mapObject.transform, "Terre", -10);

            TerrainType[,] terrain = IslandGenerator.Generate(IslandSettings);
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            var bounds = new BoundsInt(-width / 2, -height / 2, 0, width, height, 1);

            var waterTiles = new TileBase[width * height];
            var landTiles = new TileBase[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    waterTiles[index] = water;
                    switch (terrain[x, y])
                    {
                        case TerrainType.Sand:
                            landTiles[index] = sand;
                            break;
                        case TerrainType.Grass:
                            landTiles[index] = grass;
                            break;
                    }
                }
            }

            waterMap.ClearAllTiles();
            landMap.ClearAllTiles();
            waterMap.SetTilesBlock(bounds, waterTiles);
            landMap.SetTilesBlock(bounds, landTiles);

            var worldMap = mapObject.GetComponent<WorldMap>();
            if (worldMap == null)
            {
                worldMap = mapObject.AddComponent<WorldMap>();
            }

            SetReference(worldMap, "_land", landMap);
            SetReference(worldMap, "_water", waterMap);

            // Le joueur apparaît sur la plage sud, face à la mer.
            Vector2Int spawn = FindSouthSpawn(terrain);
            player.transform.position = new Vector3(bounds.xMin + spawn.x + 0.5f, bounds.yMin + spawn.y + 0.5f, 0f);

            SetReference(player.GetComponent<PlayerMovement>(), "_map", worldMap);
            SetReference(controller, "_angler", player.GetComponent<PlayerMovement>());
            SetReference(controller, "_map", worldMap);

            Camera camera = Camera.main;
            if (camera != null)
            {
                var follow = camera.GetComponent<CameraFollow>();
                if (follow == null)
                {
                    follow = camera.gameObject.AddComponent<CameraFollow>();
                }

                SetReference(follow, "_target", player.transform);
                SetReference(follow, "_map", worldMap);
                camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, camera.transform.position.z);
            }
        }

        private static Vector2Int FindSouthSpawn(TerrainType[,] terrain)
        {
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            int x = width / 2;
            int y = height / 2;
            while (y > 1 && !IslandGenerator.IsWater(terrain, x, y - 1))
            {
                y--;
            }

            // Deux cases à l'intérieur des terres, pour ne pas naître les pieds dans l'eau.
            return new Vector2Int(x, Mathf.Min(y + 2, height / 2));
        }

        private static Tilemap EnsureTilemap(Transform parent, string objectName, int sortingOrder)
        {
            Transform child = parent.Find(objectName);
            if (child == null)
            {
                var go = new GameObject(objectName, typeof(Tilemap), typeof(TilemapRenderer));
                go.transform.SetParent(parent, false);
                child = go.transform;
            }

            child.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return child.GetComponent<Tilemap>();
        }

        private static void SetReference(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Tuiles pixel art provisoires ----------

        private static Tile EnsureTile(string tileName, Color baseColor, Color light, Color dark, int seed, bool waves)
        {
            EnsureFolder(TilesFolder);
            string pngPath = $"{TilesFolder}/{tileName}.png";
            string tilePath = $"{TilesFolder}/{tileName}.asset";

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath) == null)
            {
                WriteTexture(pngPath, baseColor, light, dark, seed, waves);
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static void WriteTexture(string assetPath, Color baseColor, Color light, Color dark, int seed, bool waves)
        {
            var random = new System.Random(seed);
            var texture = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    double roll = random.NextDouble();
                    Color color = roll < 0.10 ? light : roll < 0.22 ? dark : baseColor;
                    texture.SetPixel(x, y, color);
                }
            }

            if (waves)
            {
                // Deux petites vaguelettes claires.
                for (int i = 0; i < 4; i++)
                {
                    texture.SetPixel(3 + i, 11, light);
                    texture.SetPixel(9 + i, 4, light);
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
            importer.spritePixelsPerUnit = TileSize;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string path)
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
