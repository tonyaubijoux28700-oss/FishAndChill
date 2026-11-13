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
            // Palette tropicale : lagon turquoise, sable blanc, herbe luxuriante.
            Tile water = EnsureTile("Eau", NoisePainter(new Color(0.20f, 0.68f, 0.75f), new Color(0.45f, 0.85f, 0.88f), new Color(0.15f, 0.58f, 0.68f), 11, waves: true));
            Tile sand = EnsureTile("Sable", NoisePainter(new Color(0.95f, 0.89f, 0.70f), new Color(0.99f, 0.96f, 0.84f), new Color(0.88f, 0.80f, 0.60f), 22, waves: false));
            Tile grass = EnsureTile("Herbe", NoisePainter(new Color(0.30f, 0.66f, 0.32f), new Color(0.42f, 0.77f, 0.40f), new Color(0.22f, 0.55f, 0.26f), 33, waves: false));
            Tile palm = EnsureTile("Palmier", PalmPainter());

            GameObject mapObject = GameObject.Find("Carte");
            if (mapObject == null)
            {
                mapObject = new GameObject("Carte", typeof(Grid));
            }

            Tilemap waterMap = EnsureTilemap(mapObject.transform, "Eau", -20);
            Tilemap landMap = EnsureTilemap(mapObject.transform, "Terre", -10);
            Tilemap decorMap = EnsureTilemap(mapObject.transform, "Décor", -5);

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
            decorMap.ClearAllTiles();
            waterMap.SetTilesBlock(bounds, waterTiles);
            landMap.SetTilesBlock(bounds, landTiles);

            // Le joueur apparaît sur la plage sud, face à la mer.
            Vector2Int spawn = FindSouthSpawn(terrain);

            // Quelques palmiers sur l'herbe (décor, on passe dessous), jamais sur le point d'apparition.
            var random = new System.Random(IslandSettings.Seed + 1);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool nearSpawn = Mathf.Abs(x - spawn.x) <= 2 && Mathf.Abs(y - spawn.y) <= 2;
                    if (terrain[x, y] == TerrainType.Grass && !nearSpawn && random.NextDouble() < 0.05)
                    {
                        decorMap.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), palm);
                    }
                }
            }

            var worldMap = mapObject.GetComponent<WorldMap>();
            if (worldMap == null)
            {
                worldMap = mapObject.AddComponent<WorldMap>();
            }

            SetReference(worldMap, "_land", landMap);
            SetReference(worldMap, "_water", waterMap);

            player.transform.position = new Vector3(bounds.xMin + spawn.x + 0.5f, bounds.yMin + spawn.y + 0.5f, 0f);

            SetReference(player.GetComponent<PlayerMovement>(), "_map", worldMap);
            SetReference(controller, "_angler", player.GetComponent<PlayerMovement>());
            SetReference(controller, "_map", worldMap);

            BuildMinimap(worldMap, decorMap, player.transform, sand, grass, palm);

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

        /// <summary>Carte de l'île consultable avec M : couleurs par tuile, point du joueur.</summary>
        private static void BuildMinimap(WorldMap worldMap, Tilemap decorMap, Transform player, Tile sand, Tile grass, Tile palm)
        {
            GameObject go = GameObject.Find("Minimap");
            if (go == null)
            {
                go = new GameObject("Minimap");
            }

            var minimap = go.GetComponent<UI.Minimap>();
            if (minimap == null)
            {
                minimap = go.AddComponent<UI.Minimap>();
            }

            var so = new SerializedObject(minimap);
            so.FindProperty("_map").objectReferenceValue = worldMap;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_decor").objectReferenceValue = decorMap;
            so.FindProperty("_waterColor").colorValue = new Color(0.20f, 0.66f, 0.74f);
            so.FindProperty("_pixelsPerCell").intValue = 6;

            SerializedProperty palette = so.FindProperty("_palette");
            var entries = new (Tile tile, Color color)[]
            {
                (sand, new Color(0.96f, 0.89f, 0.68f)),
                (grass, new Color(0.32f, 0.66f, 0.33f)),
                (palm, new Color(0.12f, 0.42f, 0.18f)),
            };
            palette.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty entry = palette.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Tile").objectReferenceValue = entries[i].tile;
                entry.FindPropertyRelative("Color").colorValue = entries[i].color;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
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

        /// <summary>
        /// Crée la tuile et (re)dessine son image. Les images sont provisoires et régénérées à chaque
        /// lancement : pour garder un dessin fait à la main, changer le nom de la tuile ici.
        /// </summary>
        private static Tile EnsureTile(string tileName, System.Func<int, int, Color> painter)
        {
            EnsureFolder(TilesFolder);
            string pngPath = $"{TilesFolder}/{tileName}.png";
            string tilePath = $"{TilesFolder}/{tileName}.asset";

            WriteTexture(pngPath, painter);

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

        /// <summary>Texture de sol : couleur de base parsemée de pixels clairs et sombres.</summary>
        private static System.Func<int, int, Color> NoisePainter(Color baseColor, Color light, Color dark, int seed, bool waves)
        {
            var pixels = new Color[TileSize, TileSize];
            var random = new System.Random(seed);
            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    double roll = random.NextDouble();
                    pixels[x, y] = roll < 0.10 ? light : roll < 0.22 ? dark : baseColor;
                }
            }

            if (waves)
            {
                // Deux petites vaguelettes claires.
                for (int i = 0; i < 4; i++)
                {
                    pixels[3 + i, 11] = light;
                    pixels[9 + i, 4] = light;
                }
            }

            return (x, y) => pixels[x, y];
        }

        /// <summary>Palmier vu de dessus : étoile de palmes vertes autour d'un cœur brun, fond transparent.</summary>
        private static System.Func<int, int, Color> PalmPainter()
        {
            var leaf = new Color(0.16f, 0.50f, 0.22f);
            var leafLight = new Color(0.30f, 0.66f, 0.30f);
            var trunk = new Color(0.45f, 0.30f, 0.16f);
            var pixels = new Color[TileSize, TileSize];
            var center = new Vector2(7.5f, 7.5f);

            for (int angle = 0; angle < 360; angle += 60)
            {
                Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                var side = new Vector2(-direction.y, direction.x);
                for (float t = 1f; t < 7.5f; t += 0.5f)
                {
                    float width = t < 5f ? 1.2f : 0.6f;
                    for (float w = -width; w <= width; w += 0.5f)
                    {
                        Vector2 p = center + direction * t + side * w;
                        int px = Mathf.RoundToInt(p.x);
                        int py = Mathf.RoundToInt(p.y);
                        if (px >= 0 && py >= 0 && px < TileSize && py < TileSize)
                        {
                            pixels[px, py] = Mathf.Abs(w) < 0.3f ? leafLight : leaf;
                        }
                    }
                }
            }

            for (int x = 6; x <= 9; x++)
            {
                for (int y = 6; y <= 9; y++)
                {
                    pixels[x, y] = trunk;
                }
            }

            return (x, y) => pixels[x, y];
        }

        private static void WriteTexture(string assetPath, System.Func<int, int, Color> painter)
        {
            var texture = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    texture.SetPixel(x, y, painter(x, y));
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
