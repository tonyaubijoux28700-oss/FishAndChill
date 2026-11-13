using System.Collections.Generic;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Construit l'île de départ : tuiles pixel art détaillées (même taille de pixel que les poissons des
    /// cartes), lagon peu profond, variantes, cocotiers, point d'apparition, caméra et minimap.
    /// Une fois générée, l'île peut être retouchée à la main (Tile Palette).
    /// </summary>
    public static class MapSetup
    {
        private const string TilesFolder = "Assets/_Project/Art/Tiles";
        private const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";

        /// <summary>
        /// Taille de caméra pour qu'un pixel du monde ait la même taille à l'écran qu'un pixel de poisson
        /// sur la carte de prise : 1080 / (2 × 6 × 16) ≈ 5,6 pixels d'écran, comme 540 / 96 sur la carte.
        /// </summary>
        public const float CameraSize = 6f;

        /// <summary>Le lagon clair s'étend jusqu'à cette distance de la côte (en cases).</summary>
        private const int ShallowDistance = 3;

        private static readonly IslandGenerator.Settings IslandSettings = new IslandGenerator.Settings
        {
            Width = 96,
            Height = 72,
            Seed = 1234,
        };

        private sealed class TileSet
        {
            // Tuiles « logiques » (une par case) : servent aux collisions et à la minimap.
            public Tile[] Deep;
            public Tile[] Shallow;
            public Tile[] Sand;
            public Tile[] Grass;
            public Tile Palm;

            // Tuiles de rendu en double grille : [masque 0..15], plus des variantes pour le masque plein (15).
            public Tile[] LagoonEdges;
            public Tile[] SandEdges;
            public Tile[] GrassEdges;
            public Tile[] LagoonFull;
            public Tile[] SandFull;
            public Tile[] GrassFull;
        }

        /// <summary>Construit (ou reconstruit) l'île dans la scène ouverte et y branche joueur, pêche et caméra.</summary>
        public static void BuildIsland(GameObject player, FishingController controller)
        {
            TileSet tiles = CreateTiles();
            ConfigureDepthSorting();

            GameObject mapObject = GameObject.Find("Carte");
            if (mapObject == null)
            {
                mapObject = new GameObject("Carte", typeof(Grid));
            }

            Tilemap waterMap = EnsureTilemap(mapObject.transform, "Eau", -20, TilemapRenderer.Mode.Chunk);
            // Couche logique (collisions, minimap) : non affichée, le rendu passe par les transitions arrondies.
            Tilemap landMap = EnsureTilemap(mapObject.transform, "Terre", -10, TilemapRenderer.Mode.Chunk);
            landMap.GetComponent<TilemapRenderer>().enabled = false;
            Tilemap lagoonLayer = EnsureTilemap(mapObject.transform, "Rendu - Lagon", -18, TilemapRenderer.Mode.Chunk);
            Tilemap sandLayer = EnsureTilemap(mapObject.transform, "Rendu - Sable", -15, TilemapRenderer.Mode.Chunk);
            Tilemap grassLayer = EnsureTilemap(mapObject.transform, "Rendu - Herbe", -12, TilemapRenderer.Mode.Chunk);
            // Même ordre que le joueur, tri individuel : on passe derrière un cocotier quand on est au nord.
            Tilemap decorMap = EnsureTilemap(mapObject.transform, "Décor", 10, TilemapRenderer.Mode.Individual);

            TerrainType[,] terrain = IslandGenerator.Generate(IslandSettings);
            int[,] distance = IslandGenerator.DistanceToLand(terrain, ShallowDistance + 1);
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            var bounds = new BoundsInt(-width / 2, -height / 2, 0, width, height, 1);
            int seed = IslandSettings.Seed;

            var waterTiles = new TileBase[width * height];
            var landTiles = new TileBase[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    float hash = IslandGenerator.CellHash(x, y, seed);
                    Tile[] water = distance[x, y] <= ShallowDistance ? tiles.Shallow : tiles.Deep;
                    waterTiles[index] = water[hash < 0.5f ? 0 : 1];

                    switch (terrain[x, y])
                    {
                        case TerrainType.Sand:
                            // Surtout du sable uni, parfois un coquillage ou une étoile de mer.
                            landTiles[index] = tiles.Sand[hash < 0.88f ? 0 : hash < 0.95f ? 1 : 2];
                            break;
                        case TerrainType.Grass:
                            // Herbe surtout unie, quelques fleurs isolées.
                            landTiles[index] = tiles.Grass[hash < 0.91f ? 0 : hash < 0.96f ? 1 : 2];
                            break;
                    }
                }
            }

            waterMap.ClearAllTiles();
            landMap.ClearAllTiles();
            decorMap.ClearAllTiles();
            waterMap.SetTilesBlock(bounds, waterTiles);
            landMap.SetTilesBlock(bounds, landTiles);

            // Rendu arrondi en double grille : lagon (près des côtes, sous le sable), sable (toute la terre), herbe.
            FillDualLayer(lagoonLayer, bounds, width, height, (x, y) => distance[x, y] <= ShallowDistance, tiles.LagoonEdges, tiles.LagoonFull, seed + 2);
            FillDualLayer(sandLayer, bounds, width, height, (x, y) => terrain[x, y] != TerrainType.Water, tiles.SandEdges, tiles.SandFull, seed + 3);
            FillDualLayer(grassLayer, bounds, width, height, (x, y) => terrain[x, y] == TerrainType.Grass, tiles.GrassEdges, tiles.GrassFull, seed + 4);

            // Le joueur apparaît sur la plage sud, face à la mer.
            Vector2Int spawn = FindSouthSpawn(terrain);

            // Cocotiers sur l'herbe, espacés, jamais sur le point d'apparition.
            var palms = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool nearSpawn = Mathf.Abs(x - spawn.x) <= 3 && Mathf.Abs(y - spawn.y) <= 3;
                    if (terrain[x, y] != TerrainType.Grass || nearSpawn || IslandGenerator.CellHash(x, y, seed + 1) > 0.045f)
                    {
                        continue;
                    }

                    if (palms.Exists(other => Mathf.Abs(other.x - x) < 3 && Mathf.Abs(other.y - y) < 3))
                    {
                        continue;
                    }

                    palms.Add(new Vector2Int(x, y));
                    decorMap.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), tiles.Palm);
                }
            }

            var worldMap = mapObject.GetComponent<WorldMap>();
            if (worldMap == null)
            {
                worldMap = mapObject.AddComponent<WorldMap>();
            }

            SetReference(worldMap, "_land", landMap);
            SetReference(worldMap, "_water", waterMap);

            // Position du joueur = ses pieds, au centre de la case.
            player.transform.position = new Vector3(bounds.xMin + spawn.x + 0.5f, bounds.yMin + spawn.y + 0.5f, 0f);

            SetReference(player.GetComponent<PlayerMovement>(), "_map", worldMap);
            SetReference(controller, "_angler", player.GetComponent<PlayerMovement>());
            SetReference(controller, "_map", worldMap);

            BuildMinimap(worldMap, decorMap, player.transform, tiles);

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.orthographicSize = CameraSize;
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

        private static TileSet CreateTiles()
        {
            var set = new TileSet
            {
                Deep = new Tile[2],
                Shallow = new Tile[2],
                Sand = new Tile[3],
                Grass = new Tile[3],
            };

            for (int i = 0; i < 2; i++)
            {
                set.Deep[i] = SaveTile($"Eau_{i}", TileArt.DeepWater(i), Vector2.one * 0.5f, TileArt.Size);
                // Le lagon est dessiné par les transitions : sa tuile logique a l'aspect du large en dessous.
                set.Shallow[i] = SaveTile($"Lagon_{i}", TileArt.DeepWater(i), Vector2.one * 0.5f, TileArt.Size);
            }

            for (int i = 0; i < 3; i++)
            {
                set.Sand[i] = SaveTile($"Sable_{i}", TileArt.Sand(i), Vector2.one * 0.5f, TileArt.Size);
                set.Grass[i] = SaveTile($"Herbe_{i}", TileArt.Grass(i), Vector2.one * 0.5f, TileArt.Size);
            }

            set.Palm = SaveTile("Cocotier", TileArt.Palm(), TileArt.PalmPivot, TileArt.Size);

            // Transitions arrondies.
            var lagoon = new TransitionArt.Style
            {
                Fill = TileArt.ShallowWater(0),
                InnerBand = 0.06f, InnerColor = new Color(0.40f, 0.82f, 0.84f), InnerBlend = 0.6f,
                OuterBand = 0.07f, OuterColor = new Color(0.24f, 0.66f, 0.74f), DitherOuter = true,
            };
            var sand = new TransitionArt.Style
            {
                Fill = TileArt.Sand(0),
                InnerBand = 0.07f, InnerColor = new Color(0.84f, 0.74f, 0.54f), InnerBlend = 1f, // sable mouillé
                OuterBand = 0.07f, OuterColor = new Color(0.95f, 0.99f, 0.99f), DitherOuter = false,  // écume
            };
            var grass = new TransitionArt.Style
            {
                Fill = TileArt.Grass(0),
                InnerBand = 0.06f, InnerColor = new Color(0.24f, 0.50f, 0.24f), InnerBlend = 1f,      // lisière
                OuterBand = 0.06f, OuterColor = new Color(0.40f, 0.68f, 0.34f), DitherOuter = true,   // brins
            };

            set.LagoonEdges = SaveEdges("Lagon", lagoon);
            set.SandEdges = SaveEdges("Sable", sand);
            set.GrassEdges = SaveEdges("Herbe", grass);

            set.LagoonFull = new[] { set.LagoonEdges[15], SaveTile("Bords/Lagon_15b", TileArt.ShallowWater(1), Vector2.one * 0.5f, TileArt.Size) };
            set.SandFull = new[]
            {
                set.SandEdges[15],
                SaveTile("Bords/Sable_15b", TileArt.Sand(1), Vector2.one * 0.5f, TileArt.Size),
                SaveTile("Bords/Sable_15c", TileArt.Sand(2), Vector2.one * 0.5f, TileArt.Size),
            };
            set.GrassFull = new[]
            {
                set.GrassEdges[15],
                SaveTile("Bords/Herbe_15b", TileArt.Grass(1), Vector2.one * 0.5f, TileArt.Size),
                SaveTile("Bords/Herbe_15c", TileArt.Grass(2), Vector2.one * 0.5f, TileArt.Size),
            };
            return set;
        }

        /// <summary>Les 16 tuiles de transition d'une matière (masque 0 = vide, pas de tuile).</summary>
        private static Tile[] SaveEdges(string name, TransitionArt.Style style)
        {
            var tiles = new Tile[16];
            for (int mask = 1; mask < 16; mask++)
            {
                tiles[mask] = SaveTile($"Bords/{name}_{mask:00}", TransitionArt.Build(mask, style), Vector2.one * 0.5f, TileArt.Size);
            }

            return tiles;
        }

        /// <summary>
        /// Remplit une couche en double grille : la tuile (x, y) couvre les centres des cases (x, y) à (x+1, y+1).
        /// La Tilemap est décalée d'une demi-case pour que ses coins tombent sur les centres des cases logiques.
        /// </summary>
        private static void FillDualLayer(Tilemap layer, BoundsInt bounds, int width, int height,
            System.Func<int, int, bool> isSet, Tile[] edges, Tile[] fullVariants, int seed)
        {
            layer.ClearAllTiles();
            layer.transform.localPosition = new Vector3(0.5f, 0.5f, 0f);

            bool At(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && isSet(x, y);

            for (int x = -1; x < width; x++)
            {
                for (int y = -1; y < height; y++)
                {
                    int mask = SmoothTerrain.Mask(At(x, y), At(x + 1, y), At(x, y + 1), At(x + 1, y + 1));
                    if (mask == 0)
                    {
                        continue;
                    }

                    Tile tile = edges[mask];
                    if (mask == 15)
                    {
                        float hash = IslandGenerator.CellHash(x, y, seed);
                        int variant = fullVariants.Length == 3
                            ? (hash < 0.9f ? 0 : hash < 0.95f ? 1 : 2)
                            : (hash < 0.5f ? 0 : 1);
                        tile = fullVariants[variant];
                    }

                    layer.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), tile);
                }
            }
        }

        /// <summary>Crée la tuile et (re)dessine son image (provisoire : régénérée à chaque lancement).</summary>
        private static Tile SaveTile(string tileName, Color[,] pixels, Vector2 pivot, int pixelsPerUnit)
        {
            Sprite sprite = PixelArt.Save($"{TilesFolder}/{tileName}.png", pixels, pixelsPerUnit, pivot);
            string tilePath = $"{TilesFolder}/{tileName}.asset";
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

        /// <summary>
        /// Tri par profondeur en vue de dessus : ce qui est plus au nord (y plus grand) est dessiné derrière.
        /// Réglé sur le Renderer 2D de l'URP (Transparency Sort Mode = Custom Axis (0, 1, 0)).
        /// </summary>
        private static void ConfigureDepthSorting()
        {
            var renderer = AssetDatabase.LoadMainAssetAtPath(Renderer2DPath);
            if (renderer == null)
            {
                Debug.LogWarning($"[FishAndChill] {Renderer2DPath} introuvable : tri par profondeur non réglé.");
                return;
            }

            var so = new SerializedObject(renderer);
            SerializedProperty mode = so.FindProperty("m_TransparencySortMode");
            SerializedProperty axis = so.FindProperty("m_TransparencySortAxis");
            if (mode == null || axis == null)
            {
                Debug.LogWarning("[FishAndChill] Réglages de tri introuvables sur le Renderer 2D.");
                return;
            }

            mode.intValue = (int)TransparencySortMode.CustomAxis;
            axis.vector3Value = new Vector3(0f, 1f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
        }

        /// <summary>Petite carte (coin) et grande carte (M) : couleurs par tuile, point du joueur.</summary>
        private static void BuildMinimap(WorldMap worldMap, Tilemap decorMap, Transform player, TileSet tiles)
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

            var entries = new List<(Tile tile, Color color)>();
            foreach (Tile t in tiles.Deep) entries.Add((t, new Color(0.14f, 0.54f, 0.67f)));
            foreach (Tile t in tiles.Shallow) entries.Add((t, new Color(0.36f, 0.80f, 0.82f)));
            foreach (Tile t in tiles.Sand) entries.Add((t, new Color(0.96f, 0.88f, 0.67f)));
            foreach (Tile t in tiles.Grass) entries.Add((t, new Color(0.33f, 0.65f, 0.32f)));
            entries.Add((tiles.Palm, new Color(0.12f, 0.42f, 0.18f)));

            var so = new SerializedObject(minimap);
            so.FindProperty("_map").objectReferenceValue = worldMap;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_decor").objectReferenceValue = decorMap;
            so.FindProperty("_waterColor").colorValue = new Color(0.14f, 0.54f, 0.67f);
            so.FindProperty("_pixelsPerCell").intValue = 6;

            SerializedProperty palette = so.FindProperty("_palette");
            palette.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
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

        private static Tilemap EnsureTilemap(Transform parent, string objectName, int sortingOrder, TilemapRenderer.Mode mode)
        {
            Transform child = parent.Find(objectName);
            if (child == null)
            {
                var go = new GameObject(objectName, typeof(Tilemap), typeof(TilemapRenderer));
                go.transform.SetParent(parent, false);
                child = go.transform;
            }

            var renderer = child.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.mode = mode;
            return child.GetComponent<Tilemap>();
        }

        private static void SetReference(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
