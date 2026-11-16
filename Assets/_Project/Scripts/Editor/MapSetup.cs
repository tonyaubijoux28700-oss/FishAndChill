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
            public Tile[] Reeds;
            public Tile[] Lilies;
            public Tile[] Rocks;

            // Tuiles de rendu en double grille : [masque 0..15], plus des variantes pour le masque plein (15).
            public Tile[] LagoonEdges;
            public Tile[] SandEdges;
            public Tile[] GrassEdges;
            public Tile[] LagoonFull;
            public Tile[] SandFull;
            public Tile[] GrassFull;

            // Pavés des parvis et chemins (double grille), et décor posé autour des bâtiments.
            public Tile[] PavingEdges;
            public Tile[] PavingFull;
            public readonly Dictionary<TownLayout.DecorKind, Tile[]> Decor = new Dictionary<TownLayout.DecorKind, Tile[]>();
            public Tile SignpostLeft;
            public Tile SignpostRight;
        }

        /// <summary>Les bâtiments posés sur l'île, et les zones (régions de la grille) avec leur ponton.</summary>
        public sealed class Buildings
        {
            public ShopBuilding Shop;
            public AquariumBuilding Aquarium;
            public Rect IslandRegion;
            public DockInfo IslandDock;
            public Rect LakeRegion;
            public DockInfo LakeDock;
            public Rect AmazonRegion;
            public DockInfo AmazonDock;
        }

        /// <summary>Un ponton : sol praticable au-dessus de l'eau, point d'arrivée au bout, regard vers le large.</summary>
        public sealed class DockInfo
        {
            public Rect Area;
            public Vector2 Arrival;
            public Vector2 Facing;
            public Transform Marker;
        }

        /// <summary>Le lac d'Annecy : une île retournée, loin à l'est de l'île sur la même grille.</summary>
        private static readonly IslandGenerator.Settings LakeSettings = new IslandGenerator.Settings
        {
            Width = 80,
            Height = 60,
            Seed = 4242,
        };

        /// <summary>Écart (en cases) entre le bord est de l'île et le lac : la place des intérieurs de bâtiments.</summary>
        private const int LakeGap = 140;

        /// <summary>Le fleuve amazonien : un tronçon de fleuve qui serpente dans la jungle, à l'est du lac.</summary>
        private static readonly IslandGenerator.Settings RiverSettings = new IslandGenerator.Settings
        {
            Width = 90,
            Height = 64,
            Seed = 7070,
        };

        /// <summary>Écart (en cases) entre le bord est du lac et le fleuve.</summary>
        private const int AmazonGap = 20;

        private const int DockLength = 5;

        /// <summary>Construit (ou reconstruit) l'île dans la scène ouverte et y branche joueur, pêche et caméra.</summary>
        public static Buildings BuildIsland(GameObject player, FishingController controller)
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
            // Pavés des parvis et des chemins, par-dessus l'herbe, avec les mêmes bords arrondis.
            Tilemap pavingLayer = EnsureTilemap(mapObject.transform, "Rendu - Pavés", -11, TilemapRenderer.Mode.Chunk);
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

            // L'aquarium, grand (12 cases de large, dôme compris), au milieu de l'île, avec beaucoup de terre autour.
            Vector2Int center = TownLayout.LandCentroid(terrain);
            // La porte est 4 cases sous le centre du bâtiment (façade + dôme) : on vise le centre de l'île.
            Vector2Int aquariumCell = FindBuildingSite(terrain, center + Vector2Int.down * 4, null, 0, halfWidth: 9, front: 8, back: 10, anyDirection: true);

            // La boutique : dans les terres, près du point de départ, loin de l'aquarium.
            Vector2Int shopCell = FindBuildingSite(terrain, spawn, aquariumCell, 14);

            // Parvis, chemins pavés et décor autour des bâtiments.
            TownLayout.Result town = TownLayout.Build(terrain, spawn, shopCell, aquariumCell, seed);
            FillDualLayer(pavingLayer, bounds, width, height, (x, y) => town.Paved[x, y], tiles.PavingEdges, tiles.PavingFull, seed + 5);
            // Pied de chaque élément de décor et tronc de chaque cocotier : infranchissables.
            var staticObstacles = new List<Rect>();
            PlaceDecor(decorMap, bounds, town, tiles, staticObstacles);

            // Cocotiers sur l'herbe, espacés, jamais sur le point d'apparition, les bâtiments, les chemins ni le décor.
            var palms = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool nearSpawn = Mathf.Abs(x - spawn.x) <= 3 && Mathf.Abs(y - spawn.y) <= 3;
                    bool nearShop = Mathf.Abs(x - shopCell.x) <= 5 && y >= shopCell.y - 4 && y <= shopCell.y + 5;
                    bool nearAquarium = Mathf.Abs(x - aquariumCell.x) <= 8 && y >= aquariumCell.y - 7 && y <= aquariumCell.y + 11;
                    if (terrain[x, y] != TerrainType.Grass || nearSpawn || nearShop || nearAquarium || town.Reserved[x, y]
                        || IslandGenerator.CellHash(x, y, seed + 1) > 0.045f)
                    {
                        continue;
                    }

                    if (palms.Exists(other => Mathf.Abs(other.x - x) < 3 && Mathf.Abs(other.y - y) < 3))
                    {
                        continue;
                    }

                    palms.Add(new Vector2Int(x, y));
                    staticObstacles.Add(TownLayout.PalmFootprint(new Vector2(bounds.xMin + x + 0.5f, bounds.yMin + y + 0.5f)));
                    decorMap.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), tiles.Palm);
                }
            }

            // Ponton de l'île : sur la plage sud, à côté du point de départ, vers le large.
            DockInfo islandDock = BuildDock(mapObject.transform, "Ponton - Île Mahana", terrain, bounds, spawn, Vector2Int.down);

            // Le lac d'Annecy, loin à l'est (au-delà des intérieurs des bâtiments), avec son propre ponton.
            TileSet lakeTiles = CreateLakeTiles();
            var lakeBounds = new BoundsInt(bounds.xMax + LakeGap, -LakeSettings.Height / 2, 0, LakeSettings.Width, LakeSettings.Height, 1);
            DockInfo lakeDock = BuildLake(mapObject.transform, lakeBounds, lakeTiles, waterMap, landMap, lagoonLayer, sandLayer, grassLayer, decorMap, staticObstacles);

            // Le fleuve amazonien, encore plus à l'est, avec son ponton.
            TileSet amazonTiles = CreateAmazonTiles();
            var amazonBounds = new BoundsInt(lakeBounds.xMax + AmazonGap, -RiverSettings.Height / 2, 0, RiverSettings.Width, RiverSettings.Height, 1);
            DockInfo amazonDock = BuildAmazon(mapObject.transform, amazonBounds, amazonTiles, waterMap, landMap, lagoonLayer, sandLayer, grassLayer, decorMap, staticObstacles);

            var worldMap = mapObject.GetComponent<WorldMap>();
            if (worldMap == null)
            {
                worldMap = mapObject.AddComponent<WorldMap>();
            }

            SetReference(worldMap, "_land", landMap);
            SetReference(worldMap, "_water", waterMap);
            worldMap.SetStaticObstacles(staticObstacles);
            var islandRegion = new Rect(bounds.xMin, bounds.yMin, width, height);
            var lakeRegion = new Rect(lakeBounds.xMin, lakeBounds.yMin, lakeBounds.size.x, lakeBounds.size.y);
            var amazonRegion = new Rect(amazonBounds.xMin, amazonBounds.yMin, amazonBounds.size.x, amazonBounds.size.y);
            worldMap.SetRegions(new[] { islandRegion, lakeRegion, amazonRegion }, new[] { islandDock.Area, lakeDock.Area, amazonDock.Area });
            BuildAmazonLife(mapObject.transform, worldMap, player.transform, amazonDock.Area);
            BuildLakeLife(mapObject.transform, worldMap, player.transform, lakeDock.Area);
            BuildIslandLife(mapObject.transform, worldMap, player.transform, islandDock.Area);

            // Coins de pêche (bancs, bulles, chasses) de chaque zone, que le lancer vise tout seul.
            FishingSpots islandSpots = BuildSpots(mapObject.transform, "Coins de pêche de l'île", worldMap, 0, islandDock.Area);
            FishingSpots lakeSpots = BuildSpots(mapObject.transform, "Coins de pêche du lac", worldMap, 1, lakeDock.Area);
            var spotsSo = new SerializedObject(controller);
            SerializedProperty spotList = spotsSo.FindProperty("_spots");
            FishingSpots amazonSpots = BuildSpots(mapObject.transform, "Coins de pêche du fleuve", worldMap, 2, amazonDock.Area);
            spotList.arraySize = 3;
            spotList.GetArrayElementAtIndex(0).objectReferenceValue = islandSpots;
            spotList.GetArrayElementAtIndex(1).objectReferenceValue = lakeSpots;
            spotList.GetArrayElementAtIndex(2).objectReferenceValue = amazonSpots;
            spotsSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(worldMap);

            // Position du joueur = ses pieds, au centre de la case.
            player.transform.position = new Vector3(bounds.xMin + spawn.x + 0.5f, bounds.yMin + spawn.y + 0.5f, 0f);

            SetReference(player.GetComponent<PlayerMovement>(), "_map", worldMap);
            SetReference(controller, "_angler", player.GetComponent<PlayerMovement>());
            SetReference(controller, "_map", worldMap);

            CameraFollow follow = null;
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.orthographicSize = CameraSize;
                follow = camera.GetComponent<CameraFollow>();
                if (follow == null)
                {
                    follow = camera.gameObject.AddComponent<CameraFollow>();
                }

                SetReference(follow, "_target", player.transform);
                SetReference(follow, "_map", worldMap);
                camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, camera.transform.position.z);
            }

            // Boutique : pivot au sol, au milieu de la façade (bas de la case choisie) ; intérieur hors de l'île.
            var shopPosition = new Vector3(bounds.xMin + shopCell.x + 0.5f, bounds.yMin + shopCell.y, 0f);
            var interiorOrigin = new Vector2(bounds.xMax + 20f, bounds.yMin);
            ShopBuilding shop = BuildShop(shopPosition, interiorOrigin, worldMap, player.GetComponent<PlayerMovement>(), follow);

            // Aquarium : intérieur à côté de celui de la boutique, hors de l'île lui aussi.
            var aquariumPosition = new Vector3(bounds.xMin + aquariumCell.x + 0.5f, bounds.yMin + aquariumCell.y, 0f);
            var aquariumInterior = new Vector2(bounds.xMax + 40f, bounds.yMin);
            AquariumBuilding aquarium = BuildAquarium(aquariumPosition, aquariumInterior, worldMap, player.GetComponent<PlayerMovement>(), follow, controller);

            BuildMinimap(worldMap, decorMap, player.transform, tiles, lakeTiles, amazonTiles, shop, aquarium, new[] { islandDock.Marker, lakeDock.Marker, amazonDock.Marker });
            return new Buildings
            {
                Shop = shop, Aquarium = aquarium,
                IslandRegion = islandRegion, IslandDock = islandDock,
                LakeRegion = lakeRegion, LakeDock = lakeDock,
                AmazonRegion = amazonRegion, AmazonDock = amazonDock,
            };
        }

        // ---------- Lac d'Annecy et pontons ----------

        /// <summary>Tuiles du lac : celles de l'île recolorées (eau alpine, galets, prés) et des sapins.</summary>
        private static TileSet CreateLakeTiles()
        {
            var set = new TileSet { Deep = new Tile[2], Shallow = new Tile[2], Sand = new Tile[3], Grass = new Tile[3] };
            for (int i = 0; i < 2; i++)
            {
                set.Deep[i] = SaveAnimatedWater($"Lac_Eau_{i}", f => LakeArt.Water(TileArt.DeepWater(i, f)));
                set.Shallow[i] = set.Deep[i];
            }

            for (int i = 0; i < 3; i++)
            {
                set.Sand[i] = SaveTile($"Lac_Galets_{i}", LakeArt.Gravel(TileArt.Sand(i)), Vector2.one * 0.5f, TileArt.Size);
                set.Grass[i] = SaveTile($"Lac_Pre_{i}", LakeArt.Meadow(TileArt.Grass(i)), Vector2.one * 0.5f, TileArt.Size);
            }

            set.Palm = SaveTile("Sapin", LakeArt.Pine(), LakeArt.PinePivot, TileArt.Size);

            var shallow = new TransitionArt.Style
            {
                Fill = LakeArt.Water(TileArt.ShallowWater(0)),
                InnerBand = 0.06f, InnerColor = LakeArt.Water(new Color(0.40f, 0.82f, 0.84f)), InnerBlend = 0.6f,
                OuterBand = 0.07f, OuterColor = LakeArt.Water(new Color(0.24f, 0.66f, 0.74f)), DitherOuter = true,
            };
            var gravel = new TransitionArt.Style
            {
                Fill = LakeArt.Gravel(TileArt.Sand(0)),
                InnerBand = 0.07f, InnerColor = new Color(0.38f, 0.39f, 0.38f), InnerBlend = 1f,      // galets mouillés
                OuterBand = 0.07f, OuterColor = new Color(0.78f, 0.86f, 0.88f), DitherOuter = false,  // clapot
            };
            var meadow = new TransitionArt.Style
            {
                Fill = LakeArt.Meadow(TileArt.Grass(0)),
                InnerBand = 0.06f, InnerColor = LakeArt.Meadow(new Color(0.24f, 0.50f, 0.24f)), InnerBlend = 1f,
                OuterBand = 0.06f, OuterColor = LakeArt.Meadow(new Color(0.40f, 0.68f, 0.34f)), DitherOuter = true,
            };

            set.LagoonEdges = SaveEdges("LacEau", shallow);
            set.SandEdges = SaveEdges("LacGalets", gravel);
            set.GrassEdges = SaveEdges("LacPre", meadow);
            set.LagoonFull = new[] { set.LagoonEdges[15], SaveTile("Bords/LacEau_15b", LakeArt.Water(TileArt.ShallowWater(1)), Vector2.one * 0.5f, TileArt.Size) };
            set.SandFull = new[]
            {
                set.SandEdges[15],
                SaveTile("Bords/LacGalets_15b", LakeArt.Gravel(TileArt.Sand(1)), Vector2.one * 0.5f, TileArt.Size),
                SaveTile("Bords/LacGalets_15c", LakeArt.Gravel(TileArt.Sand(2)), Vector2.one * 0.5f, TileArt.Size),
            };
            set.GrassFull = new[]
            {
                set.GrassEdges[15],
                SaveTile("Bords/LacPre_15b", LakeArt.FlowerMeadow(0), Vector2.one * 0.5f, TileArt.Size), // gentianes
                SaveTile("Bords/LacPre_15c", LakeArt.FlowerMeadow(1), Vector2.one * 0.5f, TileArt.Size), // marguerites
            };

            // Décor du lac : roseaux au bord de l'eau, nénuphars, rochers.
            set.Reeds = new[] { SaveTile("Decor/Roseaux_0", LakeArt.Reeds(0), new Vector2(0.5f, 0.17f), TileArt.Size), SaveTile("Decor/Roseaux_1", LakeArt.Reeds(1), new Vector2(0.5f, 0.17f), TileArt.Size) };
            set.Lilies = new[] { SaveTile("Decor/Nenuphar_0", LakeArt.Lily(0), Vector2.one * 0.5f, TileArt.Size), SaveTile("Decor/Nenuphar_1", LakeArt.Lily(1), Vector2.one * 0.5f, TileArt.Size), SaveTile("Decor/Nenuphar_2", LakeArt.Lily(2), Vector2.one * 0.5f, TileArt.Size) };
            set.Rocks = new[] { SaveTile("Decor/Lac_Rocher_0", LakeArt.Gravel(DecorArt.Rock(0)), new Vector2(0.5f, 0.4f), TileArt.Size), SaveTile("Decor/Lac_Rocher_1", DecorArt.Rock(1), new Vector2(0.5f, 0.4f), TileArt.Size) };
            return set;
        }

        /// <summary>
        /// La vie de l'Île Mahana : raies sous l'eau du lagon, bernard-l'ermite sur la plage, papillons, poissons
        /// volants, frégate qui traverse le ciel et ombres de nuages.
        /// </summary>
        private static void BuildIslandLife(Transform root, WorldMap map, Transform player, Rect dock)
        {
            const string folder = "Assets/_Project/Art/Ambient";
            PixelArt.EnsureFolder(folder);
            Sprite[] Frames(string name, int count, System.Func<int, Color[,]> draw)
            {
                var sprites = new Sprite[count];
                for (int i = 0; i < count; i++)
                {
                    sprites[i] = PixelArt.Save($"{folder}/{name}_{i}.png", draw(i), TileArt.Size, Vector2.one * 0.5f);
                }

                return sprites;
            }

            GameObject go = EnsureObject("Vie de l'île", root);
            var life = go.GetComponent<AmbientLife>();
            if (life == null)
            {
                life = go.AddComponent<AmbientLife>();
            }

            var so = new SerializedObject(life);
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_region").intValue = 0;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_avoid").rectValue = new Rect(dock.xMin - 1.5f, dock.yMin - 1f, dock.width + 4f, dock.height + 2f);
            SerializedProperty swimmers = so.FindProperty("_swimmers");
            swimmers.arraySize = 1;
            SetSwimmer(swimmers.GetArrayElementAtIndex(0), Frames("Raie", 2, MahanaLifeArt.Ray), 4, 0.45f);
            swimmers.GetArrayElementAtIndex(0).FindPropertyRelative("Order").intValue = -17; // sous l'eau
            SerializedProperty walkers = so.FindProperty("_walkers");
            walkers.arraySize = 1;
            SetSwimmer(walkers.GetArrayElementAtIndex(0), Frames("BernardLermite", 2, MahanaLifeArt.HermitCrab), 7, 0.35f);
            SetSprites(so.FindProperty("_dragonfly"), Frames("Papillon", 2, MahanaLifeArt.Butterfly));
            so.FindProperty("_dragonflies").intValue = 2;
            SetSprites(so.FindProperty("_jumpingFish"), Frames("PoissonVolant", 3, MahanaLifeArt.FlyingFish));
            SetSprites(so.FindProperty("_ring"), Frames("Rond", 3, LakeLifeArt.Ring));
            so.FindProperty("_jumpLength").floatValue = 3f;   // le poisson volant plane loin
            so.FindProperty("_jumpHeight").floatValue = 0.9f;
            so.FindProperty("_jumpSeconds").floatValue = 1.1f;
            SetSprites(so.FindProperty("_passer"), Frames("Fregate", 2, MahanaLifeArt.Frigatebird));
            so.FindProperty("_passerShadow").objectReferenceValue = PixelArt.Save($"{folder}/Fregate_Ombre.png", MahanaLifeArt.FrigatebirdShadow(), TileArt.Size, Vector2.one * 0.5f);
            so.FindProperty("_cloud").objectReferenceValue = PixelArt.Save($"{folder}/OmbreNuage.png", LakeLifeArt.CloudShadow(), TileArt.Size, Vector2.one * 0.5f);
            so.FindProperty("_clouds").intValue = 5;
            SetNightLife(so, Frames);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>La faune du lac (cygnes, canards, libellules, poissons qui sautent, ombres de nuages), animée en jeu.</summary>
        private static void BuildLakeLife(Transform root, WorldMap map, Transform player, Rect dock)
        {
            const string folder = "Assets/_Project/Art/Ambient";
            PixelArt.EnsureFolder(folder);
            Sprite[] Frames(string name, int count, System.Func<int, Color[,]> draw)
            {
                var sprites = new Sprite[count];
                for (int i = 0; i < count; i++)
                {
                    sprites[i] = PixelArt.Save($"{folder}/{name}_{i}.png", draw(i), TileArt.Size, Vector2.one * 0.5f);
                }

                return sprites;
            }

            GameObject go = EnsureObject("Vie du lac", root);
            var life = go.GetComponent<AmbientLife>();
            if (life == null)
            {
                life = go.AddComponent<AmbientLife>();
            }

            var so = new SerializedObject(life);
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_region").intValue = 1;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_avoid").rectValue = new Rect(dock.xMin - 1.5f, dock.yMin - 1f, dock.width + 4f, dock.height + 2f);
            SerializedProperty swimmers = so.FindProperty("_swimmers");
            swimmers.arraySize = 2;
            SetSwimmer(swimmers.GetArrayElementAtIndex(0), Frames("Cygne", 2, LakeLifeArt.Swan), 3, 0.35f);
            SetSwimmer(swimmers.GetArrayElementAtIndex(1), Frames("Canard", 2, LakeLifeArt.Duck), 6, 0.6f);
            SetSprites(so.FindProperty("_dragonfly"), Frames("Libellule", 2, LakeLifeArt.Dragonfly));
            SetSprites(so.FindProperty("_jumpingFish"), Frames("PoissonSaute", 3, LakeLifeArt.JumpingFish));
            SetSprites(so.FindProperty("_ring"), Frames("Rond", 3, LakeLifeArt.Ring));
            so.FindProperty("_cloud").objectReferenceValue = PixelArt.Save($"{folder}/OmbreNuage.png", LakeLifeArt.CloudShadow(), TileArt.Size, Vector2.one * 0.5f);
            SetNightLife(so, Frames);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>La nuit : lucioles qui clignotent et reflet de la lune, en matériau non éclairé (ils brillent dans le noir).</summary>
        private static void SetNightLife(SerializedObject so, System.Func<string, int, System.Func<int, Color[,]>, Sprite[]> frames)
        {
            SetSprites(so.FindProperty("_glint"), frames("Reflet", 3, LakeLifeArt.Glint));
            SetSprites(so.FindProperty("_firefly"), frames("Luciole", 2, NightLifeArt.Firefly));
            so.FindProperty("_fireflies").intValue = 6;
            SetSprites(so.FindProperty("_moonGlint"), frames("RefletLune", 3, NightLifeArt.MoonGlint));
            so.FindProperty("_glowMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        }

        private static FishingSpots BuildSpots(Transform root, string name, WorldMap map, int region, Rect dock)
        {
            const string folder = "Assets/_Project/Art/Ambient";
            Sprite[] Frames(string file, int count, System.Func<int, Color[,]> draw)
            {
                var sprites = new Sprite[count];
                for (int i = 0; i < count; i++)
                {
                    sprites[i] = PixelArt.Save($"{folder}/{file}_{i}.png", draw(i), TileArt.Size, Vector2.one * 0.5f);
                }

                return sprites;
            }

            GameObject go = EnsureObject(name, root);
            var spots = go.GetComponent<FishingSpots>();
            if (spots == null)
            {
                spots = go.AddComponent<FishingSpots>();
            }

            var so = new SerializedObject(spots);
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_region").intValue = region;
            so.FindProperty("_avoid").rectValue = new Rect(dock.xMin - 3f, dock.yMin - 3f, dock.width + 6f, dock.height + 6f);
            SetSprites(so.FindProperty("_ring"), Frames("Rond", 3, LakeLifeArt.Ring));
            so.FindProperty("_shadow").objectReferenceValue = PixelArt.Save($"{folder}/OmbreBanc.png", LakeLifeArt.FishShadow(), TileArt.Size, Vector2.one * 0.5f);
            so.FindProperty("_bubble").objectReferenceValue = PixelArt.Save($"{folder}/BulleSurface.png", AquariumLifeArt.Bubble(), TileArt.Size, Vector2.one * 0.5f);
            SetSprites(so.FindProperty("_jumper"), Frames("PoissonSaute", 3, LakeLifeArt.JumpingFish));
            so.ApplyModifiedPropertiesWithoutUndo();
            return spots;
        }

        private static void SetSwimmer(SerializedProperty entry, Sprite[] frames, int count, float speed)
        {
            SetSprites(entry.FindPropertyRelative("Frames"), frames);
            entry.FindPropertyRelative("Count").intValue = count;
            entry.FindPropertyRelative("Speed").floatValue = speed;
        }

        private static void SetSprites(SerializedProperty array, Sprite[] sprites)
        {
            array.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }

        /// <summary>Dessine le lac dans les couches de la carte (sans effacer l'île), ses sapins et son ponton.</summary>
        private static DockInfo BuildLake(Transform mapRoot, BoundsInt bounds, TileSet tiles, Tilemap waterMap, Tilemap landMap,
            Tilemap lagoonLayer, Tilemap sandLayer, Tilemap grassLayer, Tilemap decorMap, List<Rect> obstacles)
        {
            TerrainType[,] terrain = LakeGenerator.Generate(LakeSettings);
            int[,] distance = IslandGenerator.DistanceToLand(terrain, ShallowDistance + 1);
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            int seed = LakeSettings.Seed;

            var waterTiles = new TileBase[width * height];
            var landTiles = new TileBase[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    float hash = IslandGenerator.CellHash(x, y, seed);
                    waterTiles[index] = tiles.Deep[hash < 0.5f ? 0 : 1];
                    if (terrain[x, y] == TerrainType.Sand)
                    {
                        landTiles[index] = tiles.Sand[hash < 0.88f ? 0 : hash < 0.95f ? 1 : 2];
                    }
                    else if (terrain[x, y] == TerrainType.Grass)
                    {
                        landTiles[index] = tiles.Grass[hash < 0.91f ? 0 : hash < 0.96f ? 1 : 2];
                    }
                }
            }

            waterMap.SetTilesBlock(bounds, waterTiles);
            landMap.SetTilesBlock(bounds, landTiles);
            FillDualLayer(lagoonLayer, bounds, width, height, (x, y) => distance[x, y] <= ShallowDistance, tiles.LagoonEdges, tiles.LagoonFull, seed + 2, false);
            FillDualLayer(sandLayer, bounds, width, height, (x, y) => terrain[x, y] != TerrainType.Water, tiles.SandEdges, tiles.SandFull, seed + 3, false);
            FillDualLayer(grassLayer, bounds, width, height, (x, y) => terrain[x, y] == TerrainType.Grass, tiles.GrassEdges, tiles.GrassFull, seed + 4, false);

            // Ponton sur la rive sud, vers le nord (le large du lac).
            int cx = width / 2;
            int cy = height / 2;
            while (cy > 1 && terrain[cx, cy] == TerrainType.Water)
            {
                cy--;
            }

            DockInfo dock = BuildDock(mapRoot, "Ponton - Lac d'Annecy", terrain, bounds, new Vector2Int(cx, cy - 1), Vector2Int.up);
            Vector2 dockCell = dock.Area.center - new Vector2(bounds.xMin, bounds.yMin);
            var taken = new bool[width, height];
            bool NearDock(int x, int y, int margin) => Mathf.Abs(x - dockCell.x) <= 3 + margin && Mathf.Abs(y - dockCell.y) <= 5 + margin;
            void Put(int x, int y, Tile tile)
            {
                taken[x, y] = true;
                decorMap.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), tile);
            }

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float hash = IslandGenerator.CellHash(x, y, seed + 7);
                    bool besideWater = terrain[x + 1, y] == TerrainType.Water || terrain[x - 1, y] == TerrainType.Water
                        || terrain[x, y + 1] == TerrainType.Water || terrain[x, y - 1] == TerrainType.Water;
                    if (terrain[x, y] == TerrainType.Sand && besideWater && hash < 0.22f && !NearDock(x, y, 1))
                    {
                        // Roseaux sur la rive, au ras de l'eau (on passe à travers : décor bas).
                        Put(x, y, tiles.Reeds[hash < 0.11f ? 0 : 1]);
                    }
                    else if (terrain[x, y] == TerrainType.Water && distance[x, y] >= 2 && distance[x, y] <= 4 && hash < 0.07f && !NearDock(x, y, 3))
                    {
                        // Nénuphars près des bords, en eau calme.
                        Put(x, y, tiles.Lilies[Mathf.FloorToInt(hash / 0.07f * 3f) % 3]);
                    }
                    else if (terrain[x, y] != TerrainType.Water && !besideWater && hash > 0.985f && !NearDock(x, y, 2))
                    {
                        // Rochers, solides comme un tronc.
                        Put(x, y, tiles.Rocks[hash > 0.993f ? 1 : 0]);
                        obstacles.Add(new Rect(bounds.xMin + x + 0.1f, bounds.yMin + y + 0.15f, 0.8f, 0.45f));
                    }
                }
            }


            // Forêt de sapins sur les prés, plus dense que les cocotiers, jamais près du ponton.
            var pines = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool nearDock = Mathf.Abs(x - dockCell.x) <= 4 && Mathf.Abs(y - dockCell.y) <= 7;
                    if (terrain[x, y] != TerrainType.Grass || nearDock || taken[x, y] || IslandGenerator.CellHash(x, y, seed + 1) > 0.09f)
                    {
                        continue;
                    }

                    if (pines.Exists(other => Mathf.Abs(other.x - x) < 2 && Mathf.Abs(other.y - y) < 2))
                    {
                        continue;
                    }

                    pines.Add(new Vector2Int(x, y));
                    obstacles.Add(TownLayout.PalmFootprint(new Vector2(bounds.xMin + x + 0.5f, bounds.yMin + y + 0.5f)));
                    decorMap.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), tiles.Palm);
                }
            }

            return dock;
        }

        /// <summary>Tuiles du fleuve : celles de l'île recolorées (eau brune, vase, jungle), grands arbres et décor.</summary>
        private static TileSet CreateAmazonTiles()
        {
            var set = new TileSet { Deep = new Tile[2], Shallow = new Tile[2], Sand = new Tile[3], Grass = new Tile[3] };
            for (int i = 0; i < 2; i++)
            {
                set.Deep[i] = SaveAnimatedWater($"Fleuve_Eau_{i}", f => AmazonArt.Water(TileArt.DeepWater(i, f)));
                set.Shallow[i] = set.Deep[i];
            }

            for (int i = 0; i < 3; i++)
            {
                set.Sand[i] = SaveTile($"Fleuve_Vase_{i}", AmazonArt.Mud(TileArt.Sand(0)) /* sans coquillages */, Vector2.one * 0.5f, TileArt.Size);
                set.Grass[i] = SaveTile($"Fleuve_Jungle_{i}", AmazonArt.Jungle(TileArt.Grass(i)), Vector2.one * 0.5f, TileArt.Size);
            }

            set.Palm = SaveTile("ArbreJungle_0", AmazonArt.Tree(0), AmazonArt.TreePivot, TileArt.Size);
            // Deuxième arbre (autre vert), rangé dans Rocks pour le poser comme le premier.
            set.Rocks = new[] { SaveTile("ArbreJungle_1", AmazonArt.Tree(1), AmazonArt.TreePivot, TileArt.Size) };

            var shallow = new TransitionArt.Style
            {
                Fill = AmazonArt.Water(TileArt.ShallowWater(0)),
                InnerBand = 0.06f, InnerColor = AmazonArt.Water(new Color(0.40f, 0.82f, 0.84f)), InnerBlend = 0.6f,
                OuterBand = 0.07f, OuterColor = AmazonArt.Water(new Color(0.24f, 0.66f, 0.74f)), DitherOuter = true,
            };
            var mud = new TransitionArt.Style
            {
                Fill = AmazonArt.Mud(TileArt.Sand(0)),
                InnerBand = 0.07f, InnerColor = new Color(0.24f, 0.14f, 0.10f), InnerBlend = 1f,     // vase mouillée
                OuterBand = 0.07f, OuterColor = new Color(0.78f, 0.70f, 0.52f), DitherOuter = false, // écume brune
            };
            var jungle = new TransitionArt.Style
            {
                Fill = AmazonArt.Jungle(TileArt.Grass(0)),
                InnerBand = 0.06f, InnerColor = AmazonArt.Jungle(new Color(0.24f, 0.50f, 0.24f)), InnerBlend = 1f,
                OuterBand = 0.06f, OuterColor = AmazonArt.Jungle(new Color(0.40f, 0.68f, 0.34f)), DitherOuter = true,
            };

            set.LagoonEdges = SaveEdges("FleuveEau", shallow);
            set.SandEdges = SaveEdges("FleuveVase", mud);
            set.GrassEdges = SaveEdges("FleuveJungle", jungle);
            set.LagoonFull = new[] { set.LagoonEdges[15], SaveTile("Bords/FleuveEau_15b", AmazonArt.Water(TileArt.ShallowWater(1)), Vector2.one * 0.5f, TileArt.Size) };
            set.SandFull = new[]
            {
                set.SandEdges[15],
                SaveTile("Bords/FleuveVase_15b", AmazonArt.Mud(TileArt.Sand(0)), Vector2.one * 0.5f, TileArt.Size),
                SaveTile("Bords/FleuveVase_15c", AmazonArt.Mud(TileArt.Sand(0)), Vector2.one * 0.5f, TileArt.Size),
            };
            set.GrassFull = new[]
            {
                set.GrassEdges[15],
                SaveTile("Bords/FleuveJungle_15b", AmazonArt.FlowerJungle(0), Vector2.one * 0.5f, TileArt.Size), // héliconias
                SaveTile("Bords/FleuveJungle_15c", AmazonArt.FlowerJungle(1), Vector2.one * 0.5f, TileArt.Size), // orchidées
            };

            set.Reeds = new[] { SaveTile("Decor/Fougere_0", AmazonArt.Fern(0), new Vector2(0.5f, 0.1f), TileArt.Size), SaveTile("Decor/Fougere_1", AmazonArt.Fern(1), new Vector2(0.5f, 0.1f), TileArt.Size) };
            set.Lilies = new[] { SaveTile("Decor/Victoria_0", AmazonArt.GiantLily(0), Vector2.one * 0.5f, TileArt.Size), SaveTile("Decor/Victoria_1", AmazonArt.GiantLily(1), Vector2.one * 0.5f, TileArt.Size), SaveTile("Decor/Victoria_2", AmazonArt.GiantLily(2), Vector2.one * 0.5f, TileArt.Size) };
            return set;
        }

        /// <summary>Dessine le fleuve dans les couches de la carte (sans effacer le reste), sa jungle et son ponton.</summary>
        private static DockInfo BuildAmazon(Transform mapRoot, BoundsInt bounds, TileSet tiles, Tilemap waterMap, Tilemap landMap,
            Tilemap lagoonLayer, Tilemap sandLayer, Tilemap grassLayer, Tilemap decorMap, List<Rect> obstacles)
        {
            TerrainType[,] terrain = RiverGenerator.Generate(RiverSettings);
            int[,] distance = IslandGenerator.DistanceToLand(terrain, ShallowDistance + 1);
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            int seed = RiverSettings.Seed;

            var waterTiles = new TileBase[width * height];
            var landTiles = new TileBase[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    float hash = IslandGenerator.CellHash(x, y, seed);
                    waterTiles[index] = tiles.Deep[hash < 0.5f ? 0 : 1];
                    if (terrain[x, y] == TerrainType.Sand)
                    {
                        landTiles[index] = tiles.Sand[hash < 0.88f ? 0 : hash < 0.95f ? 1 : 2];
                    }
                    else if (terrain[x, y] == TerrainType.Grass)
                    {
                        landTiles[index] = tiles.Grass[hash < 0.91f ? 0 : hash < 0.96f ? 1 : 2];
                    }
                }
            }

            waterMap.SetTilesBlock(bounds, waterTiles);
            landMap.SetTilesBlock(bounds, landTiles);
            FillDualLayer(lagoonLayer, bounds, width, height, (x, y) => distance[x, y] <= ShallowDistance, tiles.LagoonEdges, tiles.LagoonFull, seed + 2, false);
            FillDualLayer(sandLayer, bounds, width, height, (x, y) => terrain[x, y] != TerrainType.Water, tiles.SandEdges, tiles.SandFull, seed + 3, false);
            FillDualLayer(grassLayer, bounds, width, height, (x, y) => terrain[x, y] == TerrainType.Grass, tiles.GrassEdges, tiles.GrassFull, seed + 4, false);

            // Ponton sur la rive sud, au milieu, vers le nord (le fleuve).
            int cx = width / 2;
            int cy = Mathf.FloorToInt(RiverGenerator.CenterAt(RiverSettings, cx + 0.5f));
            while (cy > 1 && terrain[cx, cy] == TerrainType.Water)
            {
                cy--;
            }

            DockInfo dock = BuildDock(mapRoot, "Ponton - Fleuve Amazone", terrain, bounds, new Vector2Int(cx, cy - 1), Vector2Int.up);
            Vector2 dockCell = dock.Area.center - new Vector2(bounds.xMin, bounds.yMin);
            int ford = RiverGenerator.FordX(RiverSettings);
            var taken = new bool[width, height];
            bool NearDock(int x, int y, int margin) => Mathf.Abs(x - dockCell.x) <= 3 + margin && Mathf.Abs(y - dockCell.y) <= 5 + margin;
            void Put(int x, int y, Tile tile)
            {
                taken[x, y] = true;
                decorMap.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), tile);
            }

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float hash = IslandGenerator.CellHash(x, y, seed + 7);
                    bool besideWater = terrain[x + 1, y] == TerrainType.Water || terrain[x - 1, y] == TerrainType.Water
                        || terrain[x, y + 1] == TerrainType.Water || terrain[x, y - 1] == TerrainType.Water;
                    bool onFord = Mathf.Abs(x - ford) <= 2;
                    if (terrain[x, y] == TerrainType.Grass && hash < 0.10f && !NearDock(x, y, 1) && !onFord)
                    {
                        // Fougères dans les sous-bois (on passe à travers : décor bas).
                        Put(x, y, tiles.Reeds[hash < 0.05f ? 0 : 1]);
                    }
                    else if (terrain[x, y] == TerrainType.Water && distance[x, y] >= 2 && distance[x, y] <= 3 && hash < 0.08f && !NearDock(x, y, 3))
                    {
                        // Nénuphars géants près des rives, là où le courant est faible.
                        Put(x, y, tiles.Lilies[Mathf.FloorToInt(hash / 0.08f * 3f) % 3]);
                    }
                    else if (terrain[x, y] == TerrainType.Sand && besideWater && hash > 0.93f && !NearDock(x, y, 1) && !onFord)
                    {
                        Put(x, y, tiles.Reeds[1]);
                    }
                }
            }

            // Forêt dense de grands arbres sur la jungle, jamais près du ponton ni du banc de sable.
            var trees = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool nearDock = Mathf.Abs(x - dockCell.x) <= 4 && Mathf.Abs(y - dockCell.y) <= 7;
                    if (terrain[x, y] != TerrainType.Grass || nearDock || taken[x, y] || Mathf.Abs(x - ford) <= 2
                        || IslandGenerator.CellHash(x, y, seed + 1) > 0.11f)
                    {
                        continue;
                    }

                    if (trees.Exists(other => Mathf.Abs(other.x - x) < 3 && Mathf.Abs(other.y - y) < 2))
                    {
                        continue;
                    }

                    trees.Add(new Vector2Int(x, y));
                    obstacles.Add(TownLayout.PalmFootprint(new Vector2(bounds.xMin + x + 0.5f, bounds.yMin + y + 0.5f)));
                    decorMap.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), IslandGenerator.CellHash(x, y, seed + 3) < 0.5f ? tiles.Palm : tiles.Rocks[0]);
                }
            }

            return dock;
        }

        /// <summary>La faune du fleuve : dauphins roses, caïmans, morphos, poissons qui sautent, aras et nuages.</summary>
        private static void BuildAmazonLife(Transform root, WorldMap map, Transform player, Rect dock)
        {
            const string folder = "Assets/_Project/Art/Ambient";
            PixelArt.EnsureFolder(folder);
            Sprite[] Frames(string name, int count, System.Func<int, Color[,]> draw)
            {
                var sprites = new Sprite[count];
                for (int i = 0; i < count; i++)
                {
                    sprites[i] = PixelArt.Save($"{folder}/{name}_{i}.png", draw(i), TileArt.Size, Vector2.one * 0.5f);
                }

                return sprites;
            }

            GameObject go = EnsureObject("Vie du fleuve", root);
            var life = go.GetComponent<AmbientLife>();
            if (life == null)
            {
                life = go.AddComponent<AmbientLife>();
            }

            var so = new SerializedObject(life);
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_region").intValue = 2;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_avoid").rectValue = new Rect(dock.xMin - 1.5f, dock.yMin - 1f, dock.width + 4f, dock.height + 2f);
            SerializedProperty swimmers = so.FindProperty("_swimmers");
            swimmers.arraySize = 2;
            SetSwimmer(swimmers.GetArrayElementAtIndex(0), Frames("DauphinRose", 2, AmazonLifeArt.RiverDolphin), 3, 0.7f);
            SetSwimmer(swimmers.GetArrayElementAtIndex(1), Frames("Caiman", 2, AmazonLifeArt.Caiman), 4, 0.25f);
            swimmers.GetArrayElementAtIndex(0).FindPropertyRelative("Order").intValue = -16;
            swimmers.GetArrayElementAtIndex(1).FindPropertyRelative("Order").intValue = -16;
            so.FindProperty("_walkers").arraySize = 0;
            SetSprites(so.FindProperty("_dragonfly"), Frames("Morpho", 2, AmazonLifeArt.Morpho));
            so.FindProperty("_dragonflies").intValue = 3;
            SetSprites(so.FindProperty("_jumpingFish"), Frames("PoissonSaute", 3, LakeLifeArt.JumpingFish));
            SetSprites(so.FindProperty("_ring"), Frames("Rond", 3, LakeLifeArt.Ring));
            SetSprites(so.FindProperty("_passer"), Frames("Ara", 2, AmazonLifeArt.Macaw));
            so.FindProperty("_passerShadow").objectReferenceValue = PixelArt.Save($"{folder}/Ara_Ombre.png", AmazonLifeArt.MacawShadow(), TileArt.Size, Vector2.one * 0.5f);
            so.FindProperty("_cloud").objectReferenceValue = PixelArt.Save($"{folder}/OmbreNuage.png", LakeLifeArt.CloudShadow(), TileArt.Size, Vector2.one * 0.5f);
            so.FindProperty("_clouds").intValue = 4;
            SetNightLife(so, Frames);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Pose un ponton de 2 cases de large qui part de la terre (depuis <paramref name="from"/>, en allant vers
        /// <paramref name="direction"/>) et avance de <see cref="DockLength"/> cases dans l'eau, avec une barque amarrée.
        /// </summary>
        private static DockInfo BuildDock(Transform mapRoot, string name, TerrainType[,] terrain, BoundsInt bounds, Vector2Int from, Vector2Int direction)
        {
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            bool Water(int x, int y) => x >= 1 && y >= 1 && x < width - 1 && y < height - 1 && terrain[x, y] == TerrainType.Water;

            int column = from.x;
            int shore = from.y;
            foreach (int offset in new[] { 3, 4, 5, 2, 6, 7, -3, -4, -5, 8, -6 })
            {
                int x = from.x + offset;
                int y = from.y;
                while (y > 0 && y < height - 1 && !(Water(x, y) && Water(x + 1, y)))
                {
                    y += direction.y;
                }

                bool ok = true;
                for (int k = 0; k < DockLength + 2 && ok; k++)
                {
                    ok = Water(x, y + k * direction.y) && Water(x + 1, y + k * direction.y);
                }

                if (ok)
                {
                    column = x;
                    shore = y - direction.y;
                    break;
                }
            }

            int end = shore + DockLength * direction.y;
            int yMin = Mathf.Min(shore, end);
            int yMax = Mathf.Max(shore, end) + 1;
            var area = new Rect(bounds.xMin + column, bounds.yMin + yMin, 2f, yMax - yMin);

            GameObject dock = EnsureObject(name, mapRoot);
            dock.transform.position = new Vector3(area.center.x, area.center.y, 0f);
            Sprite planks = PixelArt.Save($"{TilesFolder}/Ponton.png", LakeArt.Dock(yMax - yMin), TileArt.Size, Vector2.one * 0.5f);
            SetSprite(dock, planks, -9);

            // Barque amarrée le long du bout du ponton, côté est.
            GameObject boat = EnsureObject("Barque", dock.transform);
            float endY = bounds.yMin + end + 0.5f - direction.y * 1.2f;
            boat.transform.position = new Vector3(area.xMax + 0.6f, endY, 0f);
            Sprite boatSprite = PixelArt.Save($"{TilesFolder}/Barque.png", LakeArt.Boat(), TileArt.Size, Vector2.one * 0.5f);
            SetSprite(boat, boatSprite, -8);

            return new DockInfo
            {
                Area = area,
                Arrival = new Vector2(area.center.x, bounds.yMin + end + 0.5f - direction.y * 0.4f),
                Facing = new Vector2(direction.x, direction.y),
                Marker = dock.transform,
            };
        }

        /// <summary>
        /// Cherche une case pour la façade d'un bâtiment (jusqu'à 6 cases de large, 3 de profondeur), entourée de
        /// terre sur 7 cases de côté et 8 devant : bien à l'intérieur des terres, le plus près possible du départ,
        /// et au moins à <paramref name="minDistance"/> cases d'un autre bâtiment (<paramref name="avoid"/>).
        /// </summary>
        private static Vector2Int FindBuildingSite(TerrainType[,] terrain, Vector2Int spawn, Vector2Int? avoid, int minDistance, int halfWidth = 7, int front = 8, int back = 8, bool anyDirection = false)
        {
            bool Land(int x, int y) => !IslandGenerator.IsWater(terrain, x, y);
            bool FarEnough(int x, int y) => !avoid.HasValue
                || Mathf.Max(Mathf.Abs(x - avoid.Value.x), Mathf.Abs(y - avoid.Value.y)) >= minDistance;

            // Recherche en anneaux autour du point de départ (vers le nord seulement, sauf anyDirection).
            for (int radius = anyDirection ? 0 : 4; radius <= 45; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = anyDirection ? -radius : 0; dy <= radius; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius)
                        {
                            continue;
                        }

                        int cx = spawn.x + dx;
                        int cy = spawn.y + dy;
                        bool fits = FarEnough(cx, cy);
                        for (int x = cx - halfWidth; x <= cx + halfWidth && fits; x++)
                        {
                            for (int y = cy - front; y <= cy + back && fits; y++)
                            {
                                fits = Land(x, y);
                            }
                        }

                        if (fits)
                        {
                            return new Vector2Int(cx, cy);
                        }
                    }
                }
            }

            Debug.LogWarning("[FishAndChill] Pas de place idéale pour un bâtiment : il est posé au nord du point de départ.");
            return spawn + Vector2Int.up * 6;
        }

        private static ShopBuilding BuildShop(Vector3 position, Vector2 interiorOrigin, WorldMap map, PlayerMovement player, CameraFollow follow)
        {
            const string folder = "Assets/_Project/Art/Buildings";
            Sprite exterior = PixelArt.Save($"{folder}/Boutique.png", ShopArt.Exterior(), TileArt.Size, ShopArt.ExteriorPivot);
            Sprite interior = PixelArt.Save($"{folder}/Boutique_Interieur.png", ShopArt.Interior(), TileArt.Size, Vector2.zero);
            Sprite counter = PixelArt.Save($"{folder}/Boutique_Comptoir.png", ShopArt.Counter(), TileArt.Size, ShopArt.CounterPivot);
            Sprite merchant = PixelArt.Save("Assets/_Project/Art/Characters/Marchand.png", ShopArt.Merchant(), TileArt.Size, CharacterArt.Pivot);

            GameObject go = EnsureObject("Boutique", null);
            go.transform.position = position;
            SetSprite(go, exterior, 10);

            var shop = go.GetComponent<ShopBuilding>();
            if (shop == null)
            {
                shop = go.AddComponent<ShopBuilding>();
            }

            var so = new SerializedObject(shop);
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_camera").objectReferenceValue = follow;
            so.FindProperty("_interiorOrigin").vector2Value = interiorOrigin;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Intérieur : sol et murs, comptoir, marchand (positions calculées par ShopBuilding).
            GameObject room = EnsureObject("Boutique - Intérieur", null);
            room.transform.position = interiorOrigin;
            GameObject floor = EnsureObject("Sol et murs", room.transform);
            floor.transform.localPosition = Vector3.zero;
            SetSprite(floor, interior, -5).spriteSortPoint = SpriteSortPoint.Center;

            GameObject counterObject = EnsureObject("Comptoir", room.transform);
            counterObject.transform.position = shop.CounterSpot;
            SetSprite(counterObject, counter, 10);

            GameObject merchantObject = EnsureObject("Marchand", room.transform);
            merchantObject.transform.position = shop.MerchantSpot;
            SetSprite(merchantObject, merchant, 10);

            return shop;
        }

        /// <summary>
        /// L'aquarium : façade au dôme de verre sur l'île, pièce intérieure hors de l'île avec le bassin
        /// (fond, poissons qui nagent, vitre par-dessus).
        /// </summary>
        private static AquariumBuilding BuildAquarium(Vector3 position, Vector2 interiorOrigin, WorldMap map, PlayerMovement player, CameraFollow follow, FishingController controller)
        {
            const string folder = "Assets/_Project/Art/Buildings";
            Sprite exterior = PixelArt.Save($"{folder}/Aquarium.png", AquariumArt.Exterior(), TileArt.Size, AquariumArt.ExteriorPivot);
            Sprite interior = PixelArt.Save($"{folder}/Aquarium_Interieur.png", AquariumArt.Interior(), TileArt.Size, Vector2.zero);
            Sprite glass = PixelArt.Save($"{folder}/Aquarium_Vitre.png", AquariumArt.TankGlass(), TileArt.Size, Vector2.zero);

            GameObject go = EnsureObject("Aquarium", null);
            go.transform.position = position;
            SetSprite(go, exterior, 10);

            var aquarium = go.GetComponent<AquariumBuilding>();
            if (aquarium == null)
            {
                aquarium = go.AddComponent<AquariumBuilding>();
            }

            var so = new SerializedObject(aquarium);
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_camera").objectReferenceValue = follow;
            so.FindProperty("_fishing").objectReferenceValue = controller;
            so.FindProperty("_interiorOrigin").vector2Value = interiorOrigin;
            so.FindProperty("_footprint").vector2Value = new Vector2(11.2f, 5.2f);
            so.FindProperty("_interiorBackground").colorValue = new Color(0.04f, 0.07f, 0.10f);
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject room = EnsureObject("Aquarium - Intérieur", null);
            room.transform.position = interiorOrigin;
            GameObject floor = EnsureObject("Sol, murs et bassin", room.transform);
            floor.transform.localPosition = Vector3.zero;
            SetSprite(floor, interior, -6).spriteSortPoint = SpriteSortPoint.Center;

            // Les poissons nagent entre le fond (-6, plantes du fond -5) et la vitre (-2, plantes de devant et bulles -3).
            GameObject glassObject = EnsureObject("Vitre du bassin", room.transform);
            glassObject.transform.localPosition = Vector3.zero;
            SetSprite(glassObject, glass, -2).spriteSortPoint = SpriteSortPoint.Center;

            // Salle du lac, à droite du grand hall par un passage voûté.
            Color[,] lakeRoomPixels = AquariumLifeArt.LakeRoom();
            AquariumAmazonArt.OpenRightPassage(lakeRoomPixels); // passage vers la salle Amazonie
            Sprite lakeRoom = PixelArt.Save($"{folder}/Aquarium_SalleDuLac.png", lakeRoomPixels, TileArt.Size, Vector2.zero);
            Sprite lakeGlass = PixelArt.Save($"{folder}/Aquarium_SalleDuLac_Vitre.png", AquariumLifeArt.LakeTankGlass(), TileArt.Size, Vector2.zero);
            GameObject lakeObject = EnsureObject("Salle du lac", room.transform);
            lakeObject.transform.localPosition = new Vector3(AquariumBuilding.HallWidth, 0f, 0f);
            SetSprite(lakeObject, lakeRoom, -6).spriteSortPoint = SpriteSortPoint.Center;
            GameObject lakeGlassObject = EnsureObject("Vitre du bassin du lac", room.transform);
            lakeGlassObject.transform.localPosition = new Vector3(AquariumBuilding.HallWidth, 0f, 0f);
            SetSprite(lakeGlassObject, lakeGlass, -2).spriteSortPoint = SpriteSortPoint.Center;

            // Salle Amazonie, au bout de la salle du lac par un second passage.
            float amazonX = AquariumBuilding.AmazonRoomX - AquariumBuilding.PassageWidth;
            Sprite amazonRoom = PixelArt.Save($"{folder}/Aquarium_SalleAmazonie.png", AquariumAmazonArt.AmazonRoom(), TileArt.Size, Vector2.zero);
            Sprite amazonGlass = PixelArt.Save($"{folder}/Aquarium_SalleAmazonie_Vitre.png", AquariumAmazonArt.TankGlass(), TileArt.Size, Vector2.zero);
            GameObject amazonObject = EnsureObject("Salle Amazonie", room.transform);
            amazonObject.transform.localPosition = new Vector3(amazonX, 0f, 0f);
            SetSprite(amazonObject, amazonRoom, -6).spriteSortPoint = SpriteSortPoint.Center;
            GameObject amazonGlassObject = EnsureObject("Vitre du bassin Amazonie", room.transform);
            amazonGlassObject.transform.localPosition = new Vector3(amazonX, 0f, 0f);
            SetSprite(amazonGlassObject, amazonGlass, -2).spriteSortPoint = SpriteSortPoint.Center;

            // Mobilier de la salle (triés par les pieds, comme le joueur) : colonnes à méduses et bancs.
            Sprite column = PixelArt.Save($"{folder}/Aquarium_Colonne_Meduses.png", AquariumArt.JellyfishColumnV2(), TileArt.Size, new Vector2(0.5f, 0f));
            Sprite columnGlass = PixelArt.Save($"{folder}/Aquarium_Colonne_Meduses_Vitre.png", AquariumArt.JellyfishColumnGlass(), TileArt.Size, new Vector2(0.5f, 0f));
            Sprite bench = PixelArt.Save($"{folder}/Aquarium_Banc.png", AquariumArt.Bench(), TileArt.Size, new Vector2(0.5f, 0f));
            for (int i = 0; i < AquariumBuilding.JellyfishColumns.Length; i++)
            {
                GameObject item = EnsureObject($"Colonne à méduses {i + 1}", room.transform);
                item.transform.position = interiorOrigin + AquariumBuilding.JellyfishColumns[i];
                // Groupe de tri : colonne (0), méduses et bulles (1), vitre (2), triés ensemble avec le joueur par le pied.
                SetSprite(item, column, 0);
                var group = item.GetComponent<UnityEngine.Rendering.SortingGroup>();
                if (group == null)
                {
                    group = item.AddComponent<UnityEngine.Rendering.SortingGroup>();
                }

                group.sortingOrder = 10;
                GameObject glassFront = EnsureObject("Vitre", item.transform);
                glassFront.transform.localPosition = Vector3.zero;
                SetSprite(glassFront, columnGlass, 2);
            }

            for (int i = 0; i < AquariumBuilding.Benches.Length; i++)
            {
                GameObject item = EnsureObject($"Banc {i + 1}", room.transform);
                item.transform.position = interiorOrigin + AquariumBuilding.Benches[i];
                SetSprite(item, bench, 10);
            }

            // Un bassin par zone : les espèces du lagon dans le grand hall, celles du lac dans la salle du lac.
            var mahana = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/IleMahana.asset");
            var annecy = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/LacAnnecy.asset");
            BuildTank(room.transform, "Poissons du bassin", aquarium, mahana, AquariumBuilding.HallTank);
            BuildTank(room.transform, "Poissons du bassin du lac", aquarium, annecy, AquariumBuilding.LakeTank);
            var amazon = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/FleuveAmazone.asset");
            BuildTank(room.transform, "Poissons du bassin Amazonie", aquarium, amazon, AquariumBuilding.AmazonTank);

            BuildTankLife(room.transform, aquarium, folder);
            // Visiteurs mis de côté pour l'instant (AquariumVisitors et BuildVisitors restent prêts à resservir).
            Transform oldVisitors = room.transform.Find("Visiteurs");
            if (oldVisitors != null)
            {
                Object.DestroyImmediate(oldVisitors.gameObject);
            }
            return aquarium;
        }

        private static void BuildTank(Transform room, string name, AquariumBuilding aquarium, FishingZone zone, Rect area)
        {
            GameObject fishObject = EnsureObject(name, room);
            fishObject.transform.localPosition = Vector3.zero;
            var tank = fishObject.GetComponent<AquariumTank>();
            if (tank == null)
            {
                tank = fishObject.AddComponent<AquariumTank>();
            }

            var tankSo = new SerializedObject(tank);
            tankSo.FindProperty("_building").objectReferenceValue = aquarium;
            tankSo.FindProperty("_zone").objectReferenceValue = zone;
            tankSo.FindProperty("_area").rectValue = area;
            tankSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Vie des deux bassins : plantes qui ondulent, bulles, rayons, bancs de petits poissons, reflets, méduses.</summary>
        private static void BuildTankLife(Transform room, AquariumBuilding aquarium, string folder)
        {
            Sprite[] Frames(string name, int count, System.Func<int, Color[,]> draw, Vector2 pivot)
            {
                var sprites = new Sprite[count];
                for (int i = 0; i < count; i++)
                {
                    sprites[i] = PixelArt.Save($"{folder}/Aquarium/{name}_{i}.png", draw(i), TileArt.Size, pivot);
                }

                return sprites;
            }

            var bottom = new Vector2(0.5f, 0f);
            var center = new Vector2(0.5f, 0.5f);
            Sprite bubble = PixelArt.Save($"{folder}/Aquarium/Bulle.png", AquariumLifeArt.Bubble(), TileArt.Size, center);
            Sprite ray = PixelArt.Save($"{folder}/Aquarium/Rayon.png", AquariumLifeArt.LightRay(), TileArt.Size, center);

            // Lagon : laminaires dorées, herbiers verts devant, anthias orange en bancs, reflets turquoise, méduses.
            Sprite[] kelp = Frames("Laminaire", 3, f => AquariumLifeArt.Plant(f, 16, 46, 2, new Color(0.56f, 0.60f, 0.22f), new Color(0.40f, 0.44f, 0.16f), new Color(0.74f, 0.74f, 0.36f), true, 3), bottom);
            Sprite[] seagrass = Frames("Herbier", 3, f => AquariumLifeArt.Plant(f, 18, 20, 5, new Color(0.30f, 0.72f, 0.42f), new Color(0.18f, 0.52f, 0.30f), new Color(0.56f, 0.86f, 0.56f), false, 7), bottom);
            Sprite[] anthias = Frames("Anthias", 2, f => AquariumLifeArt.TinyFish(f, new Color(1f, 0.52f, 0.30f), new Color(1f, 0.78f, 0.62f), new Color(0.96f, 0.36f, 0.50f)), center);
            Sprite[] lagoonCaustics = Frames("Reflets_Lagon", 3, f => AquariumLifeArt.Caustics(f, new Color(0.70f, 0.95f, 1f)), center);
            var columns = new Transform[AquariumBuilding.JellyfishColumns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                columns[i] = room.Find($"Colonne à méduses {i + 1}");
            }

            Sprite[][] jelly =
            {
                Frames("Meduse_Aurelie", 3, f => AquariumLifeArt.JellyDetailed(f, 0), center),
                Frames("Meduse_Pelagie", 3, f => AquariumLifeArt.JellyDetailed(f, 1), center),
                Frames("Meduse_Mauve", 3, f => AquariumLifeArt.JellyDetailed(f, 2), center),
            };
            BuildLife(room, "Vie du bassin", aquarium, AquariumBuilding.HallTank, 11, new[] { (kelp, 7, false), (seagrass, 6, true) },
                bubble, 4, ray, 4, anthias, 2, 8, lagoonCaustics, jelly, columns);

            // Lac : potamots, élodées devant, vairons argentés, reflets verts.
            Sprite[] pondweed = Frames("Potamot", 3, f => AquariumLifeArt.Plant(f, 16, 44, 2, new Color(0.36f, 0.54f, 0.24f), new Color(0.24f, 0.38f, 0.16f), new Color(0.52f, 0.68f, 0.32f), true, 11), bottom);
            Sprite[] elodea = Frames("Elodee", 3, f => AquariumLifeArt.Plant(f, 18, 24, 4, new Color(0.20f, 0.46f, 0.24f), new Color(0.12f, 0.32f, 0.16f), new Color(0.36f, 0.62f, 0.34f), false, 13), bottom);
            Sprite[] minnow = Frames("Vairon", 2, f => AquariumLifeArt.TinyFish(f, new Color(0.56f, 0.58f, 0.46f), new Color(0.86f, 0.86f, 0.80f), new Color(0.80f, 0.40f, 0.30f)), center);
            Sprite[] lakeCaustics = Frames("Reflets_Lac", 3, f => AquariumLifeArt.Caustics(f, new Color(0.75f, 1f, 0.82f)), center);
            BuildLife(room, "Vie du bassin du lac", aquarium, AquariumBuilding.LakeTank, 23, new[] { (pondweed, 6, false), (elodea, 5, true) },
                bubble, 3, ray, 3, minnow, 2, 9, lakeCaustics, new Sprite[0][], new Transform[0]);

            // Amazonie : plantes épées, cabombas devant, bancs de néons cardinaux (rouge et bleu), reflets dorés.
            Sprite[] sword = Frames("PlanteEpee", 3, f => AquariumLifeArt.Plant(f, 18, 34, 4, new Color(0.30f, 0.54f, 0.22f), new Color(0.18f, 0.36f, 0.14f), new Color(0.46f, 0.68f, 0.30f), true, 17), bottom);
            Sprite[] cabomba = Frames("Cabomba", 3, f => AquariumLifeArt.Plant(f, 16, 26, 4, new Color(0.40f, 0.60f, 0.20f), new Color(0.26f, 0.42f, 0.12f), new Color(0.60f, 0.76f, 0.30f), false, 19), bottom);
            Sprite[] tetra = Frames("NeonCardinal", 2, f => AquariumLifeArt.TinyFish(f, new Color(0.20f, 0.70f, 0.96f), new Color(0.92f, 0.20f, 0.16f), new Color(0.80f, 0.86f, 0.90f)), center);
            Sprite[] amazonCaustics = Frames("Reflets_Amazonie", 3, f => AquariumLifeArt.Caustics(f, new Color(1f, 0.86f, 0.56f)), center);
            BuildLife(room, "Vie du bassin Amazonie", aquarium, AquariumBuilding.AmazonTank, 37, new[] { (sword, 6, false), (cabomba, 5, true) },
                bubble, 3, ray, 3, tetra, 3, 10, amazonCaustics, new Sprite[0][], new Transform[0]);
        }

        private static void BuildLife(Transform room, string name, AquariumBuilding aquarium, Rect area, int seed,
            (Sprite[] frames, int count, bool front)[] plants, Sprite bubble, int bubbleSources, Sprite ray, int rays,
            Sprite[] school, int schools, int schoolSize, Sprite[] caustics, Sprite[][] jellyLooks, Transform[] jellyColumns)
        {
            GameObject go = EnsureObject(name, room);
            go.transform.localPosition = Vector3.zero;
            var life = go.GetComponent<TankLife>();
            if (life == null)
            {
                life = go.AddComponent<TankLife>();
            }

            var so = new SerializedObject(life);
            so.FindProperty("_building").objectReferenceValue = aquarium;
            so.FindProperty("_area").rectValue = area;
            so.FindProperty("_seed").intValue = seed;
            SerializedProperty plantList = so.FindProperty("_plants");
            plantList.arraySize = plants.Length;
            for (int i = 0; i < plants.Length; i++)
            {
                SerializedProperty entry = plantList.GetArrayElementAtIndex(i);
                SetSprites(entry.FindPropertyRelative("Frames"), plants[i].frames);
                entry.FindPropertyRelative("Count").intValue = plants[i].count;
                entry.FindPropertyRelative("Front").boolValue = plants[i].front;
            }

            so.FindProperty("_bubble").objectReferenceValue = bubble;
            so.FindProperty("_bubbleSources").intValue = bubbleSources;
            so.FindProperty("_ray").objectReferenceValue = ray;
            so.FindProperty("_rays").intValue = rays;
            SetSprites(so.FindProperty("_school"), school);
            so.FindProperty("_schools").intValue = schools;
            so.FindProperty("_schoolSize").intValue = schoolSize;
            SetSprites(so.FindProperty("_caustics"), caustics);
            SerializedProperty looks = so.FindProperty("_jellyLooks");
            looks.arraySize = jellyLooks.Length;
            for (int i = 0; i < jellyLooks.Length; i++)
            {
                SetSprites(looks.GetArrayElementAtIndex(i).FindPropertyRelative("Frames"), jellyLooks[i]);
            }

            SerializedProperty columnList = so.FindProperty("_jellyColumns");
            columnList.arraySize = jellyColumns.Length;
            for (int i = 0; i < jellyColumns.Length; i++)
            {
                columnList.GetArrayElementAtIndex(i).objectReferenceValue = jellyColumns[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Visiteurs : le pêcheur recoloré en trois tenues, qui se promènent devant les deux bassins.</summary>
        private static void BuildVisitors(Transform room, AquariumBuilding aquarium)
        {
            const string characters = "Assets/_Project/Art/Characters";
            (float shirt, float shorts, float sat, float value)[] outfits =
            {
                (0.98f, 0.62f, 0.6f, 0.5f),   // chemise rouge, short bleu marine
                (0.14f, 0.33f, 0.7f, 0.8f),   // chemise jaune, short vert
                (0.78f, 0.10f, 0.35f, 1.0f),  // chemise violette, short beige
            };
            string[] directions = { "Bas", "Haut", "Gauche", "Droite" };
            string[] fields = { "Down", "Up", "Left", "Right" };

            GameObject go = EnsureObject("Visiteurs", room);
            go.transform.localPosition = Vector3.zero;
            var visitors = go.GetComponent<AquariumVisitors>();
            if (visitors == null)
            {
                visitors = go.AddComponent<AquariumVisitors>();
            }

            var so = new SerializedObject(visitors);
            so.FindProperty("_building").objectReferenceValue = aquarium;
            SerializedProperty looks = so.FindProperty("_looks");
            looks.arraySize = outfits.Length;
            for (int o = 0; o < outfits.Length; o++)
            {
                SerializedProperty look = looks.GetArrayElementAtIndex(o);
                for (int d = 0; d < directions.Length; d++)
                {
                    var sprites = new Sprite[3];
                    for (int f = 0; f < 3; f++)
                    {
                        Color[,] pixels = AquariumLifeArt.Visitor($"{characters}/Pecheur_{directions[d]}_{f}.png", outfits[o].shirt, outfits[o].shorts, outfits[o].sat, outfits[o].value);
                        sprites[f] = PixelArt.Save($"{characters}/Visiteur{o + 1}_{directions[d]}_{f}.png", pixels, TileArt.Size, CharacterArt.Pivot);
                    }

                    SetSprites(look.FindPropertyRelative(fields[d]), sprites);
                }
            }

            SerializedProperty rooms = so.FindProperty("_rooms");
            rooms.arraySize = 2;
            rooms.GetArrayElementAtIndex(0).FindPropertyRelative("Span").vector2Value = new Vector2(3.5f, AquariumBuilding.HallWidth - 3.5f);
            rooms.GetArrayElementAtIndex(1).FindPropertyRelative("Span").vector2Value = new Vector2(AquariumBuilding.LakeRoomX + 3f, AquariumBuilding.InteriorWidth - 3f);
            rooms.GetArrayElementAtIndex(0).FindPropertyRelative("Lane").floatValue = 8.9f;
            rooms.GetArrayElementAtIndex(1).FindPropertyRelative("Lane").floatValue = 8.9f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject EnsureObject(string name, Transform parent)
        {
            Transform existing = parent != null ? parent.Find(name) : GameObject.Find(name)?.transform;
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            return go;
        }

        private static SpriteRenderer SetSprite(GameObject go, Sprite sprite, int sortingOrder)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = go.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            return renderer;
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
                set.Deep[i] = SaveAnimatedWater($"Eau_{i}", f => TileArt.DeepWater(i, f));
                // Le lagon est dessiné par les transitions : sa tuile logique a l'aspect du large en dessous.
                set.Shallow[i] = SaveAnimatedWater($"Lagon_{i}", f => TileArt.DeepWater(i, f)); // tuile distincte : couleur du lagon sur la minimap
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

            var paving = new TransitionArt.Style
            {
                Fill = DecorArt.Paving(0),
                InnerBand = 0.07f, InnerColor = new Color(0.58f, 0.54f, 0.47f), InnerBlend = 1f,         // bordure de pierres
                OuterBand = 0.06f, OuterColor = new Color(0.36f, 0.56f, 0.28f), DitherOuter = true,    // herbe foulée
            };
            set.PavingEdges = SaveEdges("Paves", paving);
            // Pavés propres presque partout ; quelques brins d'herbe entre les pierres, rarement (5 %).
            Tile pavingGrassy = SaveTile("Bords/Paves_15b", DecorArt.Paving(1), Vector2.one * 0.5f, TileArt.Size);
            set.PavingFull = new[] { set.PavingEdges[15], set.PavingEdges[15], pavingGrassy };

            CreateDecorTiles(set);
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
            System.Func<int, int, bool> isSet, Tile[] edges, Tile[] fullVariants, int seed, bool clear = true)
        {
            if (clear)
            {
                layer.ClearAllTiles();
            }

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
        /// <summary>
        /// Tuiles de décor (Tilemap « Décor », tri individuel comme le joueur). Le pivot place le pied de l'objet
        /// dans la case : haies, tonneaux et caisses sont remontés contre le mur du bâtiment juste derrière.
        /// </summary>
        private static void CreateDecorTiles(TileSet set)
        {
            // Pivot tel que le bas du dessin soit à « baseFromCenter » cases sous (+) ou au-dessus (-) du centre de la case.
            Vector2 Pivot(int heightPx, float baseFromCenter) => new Vector2(0.5f, baseFromCenter / (heightPx / (float)TileArt.Size));

            Tile Decor(string name, Color[,] pixels, float baseFromCenter)
            {
                return SaveTile($"Decor/{name}", pixels, Pivot(pixels.GetLength(1), baseFromCenter), TileArt.Size);
            }

            PixelArt.EnsureFolder($"{TilesFolder}/Decor");
            set.Decor[TownLayout.DecorKind.Hedge] = new[] { Decor("Haie_0", DecorArt.Hedge(false, 3), -0.3f), Decor("Haie_1", DecorArt.Hedge(false, 8), -0.3f) };
            set.Decor[TownLayout.DecorKind.HedgeFlowers] = new[] { Decor("Haie_Fleurs_0", DecorArt.Hedge(true, 5), -0.3f), Decor("Haie_Fleurs_1", DecorArt.Hedge(true, 6), -0.3f) };
            set.Decor[TownLayout.DecorKind.Bush] = new[] { Decor("Buisson_0", DecorArt.Bush(0), 0.4f) };
            set.Decor[TownLayout.DecorKind.BushFlowers] = new[] { Decor("Buisson_1", DecorArt.Bush(1), 0.4f), Decor("Buisson_2", DecorArt.Bush(2), 0.4f), Decor("Buisson_3", DecorArt.Bush(3), 0.4f) };
            set.Decor[TownLayout.DecorKind.FlowerBed] = new[] { Decor("Massif_0", DecorArt.FlowerBed(21), 0.6f) };
            set.Decor[TownLayout.DecorKind.Lamp] = new[] { Decor("Lampadaire", DecorArt.Lamp(), 0.3f) };
            set.Decor[TownLayout.DecorKind.Barrel] = new[] { Decor("Tonneau", DecorArt.Barrel(), -0.2f) };
            set.Decor[TownLayout.DecorKind.Crates] = new[] { Decor("Caisses", DecorArt.Crates(), -0.2f) };
            set.Decor[TownLayout.DecorKind.Rock] = new[] { Decor("Rocher_0", DecorArt.Rock(0), 0.4f), Decor("Rocher_1", DecorArt.Rock(1), 0.4f) };
            set.SignpostLeft = Decor("Panneau_Gauche", DecorArt.Signpost(false), 0.3f);
            set.SignpostRight = Decor("Panneau_Droite", DecorArt.Signpost(true), 0.3f);
        }

        private static void PlaceDecor(Tilemap decorMap, BoundsInt bounds, TownLayout.Result town, TileSet tiles, List<Rect> obstacles)
        {
            foreach (TownLayout.Placement placement in town.Decor)
            {
                Tile tile;
                if (placement.Kind == TownLayout.DecorKind.Signpost)
                {
                    tile = town.AquariumToTheRightOfSign ? tiles.SignpostRight : tiles.SignpostLeft;
                }
                else
                {
                    // Variante imposée (décor en miroir de part et d'autre d'une porte), sinon au hasard de la case.
                    Tile[] variants = tiles.Decor[placement.Kind];
                    float hash = IslandGenerator.CellHash(placement.Cell.x, placement.Cell.y, 77);
                    int index = placement.Variant >= 0 ? placement.Variant % variants.Length : (int)(hash * variants.Length);
                    tile = variants[Mathf.Min(variants.Length - 1, index)];
                }

                decorMap.SetTile(new Vector3Int(bounds.xMin + placement.Cell.x, bounds.yMin + placement.Cell.y, 0), tile);
                var center = new Vector2(bounds.xMin + placement.Cell.x + 0.5f, bounds.yMin + placement.Cell.y + 0.5f);
                obstacles.Add(TownLayout.Footprint(placement.Kind, center));
            }
        }

        /// <summary>Tuile d'eau animée (4 images de houle), qui reste une Tile pour la logique et la minimap.</summary>
        private static Tile SaveAnimatedWater(string tileName, System.Func<int, Color[,]> frame)
        {
            var sprites = new Sprite[4];
            for (int f = 0; f < sprites.Length; f++)
            {
                sprites[f] = PixelArt.Save($"{TilesFolder}/{tileName}_f{f}.png", frame(f), TileArt.Size, Vector2.one * 0.5f);
            }

            string tilePath = $"{TilesFolder}/{tileName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<AnimatedWaterTile>(tilePath);
            if (tile == null)
            {
                AssetDatabase.DeleteAsset(tilePath); // ancienne tuile fixe
                tile = ScriptableObject.CreateInstance<AnimatedWaterTile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.Setup(sprites, 2.5f);
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

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
        private static void BuildMinimap(WorldMap worldMap, Tilemap decorMap, Transform player, TileSet tiles, TileSet lakeTiles, TileSet amazonTiles, ShopBuilding shop, AquariumBuilding aquarium, Transform[] docks)
        {
            Sprite shopIcon = PixelArt.Save("Assets/_Project/Art/UI/IconeBoutique.png", ShopArt.Icon(), TileArt.Size, Vector2.one * 0.5f);
            Sprite aquariumIcon = PixelArt.Save("Assets/_Project/Art/UI/IconeAquarium.png", AquariumArt.Icon(), TileArt.Size, Vector2.one * 0.5f);
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
            foreach (Tile t in lakeTiles.Deep) entries.Add((t, new Color(0.13f, 0.40f, 0.50f)));
            foreach (Tile t in lakeTiles.Sand) entries.Add((t, new Color(0.66f, 0.66f, 0.64f)));
            foreach (Tile t in lakeTiles.Grass) entries.Add((t, new Color(0.30f, 0.56f, 0.30f)));
            entries.Add((lakeTiles.Palm, new Color(0.10f, 0.30f, 0.20f)));
            foreach (Tile t in amazonTiles.Deep) entries.Add((t, new Color(0.46f, 0.36f, 0.20f)));
            foreach (Tile t in amazonTiles.Sand) entries.Add((t, new Color(0.62f, 0.40f, 0.24f)));
            foreach (Tile t in amazonTiles.Grass) entries.Add((t, new Color(0.16f, 0.40f, 0.16f)));
            entries.Add((amazonTiles.Palm, new Color(0.08f, 0.26f, 0.08f)));
            entries.Add((amazonTiles.Rocks[0], new Color(0.08f, 0.26f, 0.08f)));
            Sprite dockIcon = PixelArt.Save("Assets/_Project/Art/UI/IconePonton.png", LakeArt.BoatIcon(), TileArt.Size, Vector2.one * 0.5f);

            var so = new SerializedObject(minimap);
            so.FindProperty("_map").objectReferenceValue = worldMap;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_decor").objectReferenceValue = decorMap;
            SerializedProperty landmarks = so.FindProperty("_landmarks");
            landmarks.arraySize = 2 + docks.Length;
            SetLandmark(landmarks.GetArrayElementAtIndex(0), "Boutique", shop.transform, shopIcon);
            SetLandmark(landmarks.GetArrayElementAtIndex(1), "Aquarium", aquarium.transform, aquariumIcon);
            for (int i = 0; i < docks.Length; i++)
            {
                SetLandmark(landmarks.GetArrayElementAtIndex(2 + i), "Ponton", docks[i], dockIcon);
            }
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

        private static void SetLandmark(SerializedProperty entry, string name, Transform target, Sprite icon)
        {
            entry.FindPropertyRelative("Name").stringValue = name;
            entry.FindPropertyRelative("Target").objectReferenceValue = target;
            entry.FindPropertyRelative("Icon").objectReferenceValue = icon;
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
