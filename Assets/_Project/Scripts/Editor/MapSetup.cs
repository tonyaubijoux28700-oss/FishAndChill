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
            public LabBuilding Lab;
            public Rect IslandRegion;
            public DockInfo IslandDock;
            public Rect LakeRegion;
            public DockInfo LakeDock;
            public Rect AmazonRegion;
            public DockInfo AmazonDock;
            public Rect JapanRegion;
            public DockInfo JapanDock;
            public Rect ReefRegion;
            public DockInfo ReefDock;
            public Rect AtollRegion;
            public DockInfo AtollDock;
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
        private const int LakeGap = 220;

        /// <summary>Le fleuve amazonien : un tronçon de fleuve qui serpente dans la jungle, à l'est du lac.</summary>
        private static readonly IslandGenerator.Settings RiverSettings = new IslandGenerator.Settings
        {
            Width = 90,
            Height = 64,
            Seed = 7070,
        };

        /// <summary>Écart (en cases) entre le bord est du lac et le fleuve.</summary>
        private const int AmazonGap = 20;

        /// <summary>La baie japonaise : la mer au sud qui s'enfonce dans des collines boisées, à l'est du fleuve.</summary>
        private static readonly IslandGenerator.Settings BaySettings = new IslandGenerator.Settings
        {
            Width = 90,
            Height = 64,
            Seed = 8080,
        };

        private const int JapanGap = 20;

        /// <summary>La côte de la Grande Barrière : plage et brousse au nord, récif corallien au large, à l'est de la baie.</summary>
        private static readonly IslandGenerator.Settings ReefSettings = new IslandGenerator.Settings
        {
            Width = 90,
            Height = 64,
            Seed = 9090,
        };

        private const int ReefGap = 20;

        /// <summary>L'atoll des Maldives : anneau d'îlots autour d'un lagon, à l'est de la Grande Barrière.</summary>
        private static readonly IslandGenerator.Settings AtollSettings = new IslandGenerator.Settings
        {
            Width = 90,
            Height = 64,
            Seed = 7373,
        };

        private const int AtollGap = 20;

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
            // La porte juste sous le centre de l'île : le bâtiment (façade + dôme) est bien au milieu de la carte.
            Vector2Int aquariumCell = FindBuildingSite(terrain, center + Vector2Int.down, null, 0, halfWidth: 9, front: 8, back: 10, anyDirection: true);

            // La boutique : dans les terres, près du point de départ, loin de l'aquarium.
            Vector2Int shopCell = FindBuildingSite(terrain, spawn, aquariumCell, 14);

            // La maison du joueur : dans la grande prairie de l'ouest, au calme, loin de l'aquarium.
            Vector2Int houseCell = FindBuildingSite(terrain, center + new Vector2Int(-24, 2), aquariumCell, 16, halfWidth: 5, front: 6, back: 7, anyDirection: true);

            // Le phare : sur la pointe nord de l'île, face au large.
            Vector2Int lighthouseCell = FindLighthouseSite(terrain);

            // Le laboratoire de la scientifique : dans les terres de l'est, à l'écart des autres bâtiments.
            Vector2Int labCell = FindBuildingSite(terrain, center + new Vector2Int(22, 2), aquariumCell, 16, halfWidth: 5, front: 6, back: 7, anyDirection: true,
                alsoAvoid: new[] { shopCell, houseCell });

            // L'animalerie : au sud-ouest, entre la maison et la plage, loin du laboratoire (à l'est).
            Vector2Int petShopCell = FindBuildingSite(terrain, center + new Vector2Int(-16, -12), aquariumCell, 14, halfWidth: 5, front: 6, back: 7, anyDirection: true,
                alsoAvoid: new[] { shopCell, houseCell, labCell });

            // Parvis, chemins pavés et décor autour des bâtiments.
            TownLayout.Result town = TownLayout.Build(terrain, spawn, shopCell, aquariumCell, houseCell, labCell, petShopCell, seed);
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
                    bool nearHouse = Mathf.Abs(x - houseCell.x) <= 5 && y >= houseCell.y - 4 && y <= houseCell.y + 6
                        || Mathf.Abs(x - labCell.x) <= 5 && y >= labCell.y - 4 && y <= labCell.y + 6
                        || Mathf.Abs(x - petShopCell.x) <= 5 && y >= petShopCell.y - 4 && y <= petShopCell.y + 6;
                    bool nearLighthouse = Mathf.Abs(x - lighthouseCell.x) <= 3 && Mathf.Abs(y - lighthouseCell.y) <= 3;
                    if (terrain[x, y] != TerrainType.Grass || nearSpawn || nearShop || nearAquarium || nearHouse || nearLighthouse || town.Reserved[x, y]
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

            // Pied du phare : infranchissable.
            var lighthousePosition = new Vector3(bounds.xMin + lighthouseCell.x + 0.5f, bounds.yMin + lighthouseCell.y, 0f);
            staticObstacles.Add(new Rect(lighthousePosition.x - 0.95f, lighthousePosition.y - 0.1f, 1.9f, 0.9f));

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

            // La baie japonaise, à l'est du fleuve.
            TileSet japanTiles = CreateJapanTiles();
            var japanBounds = new BoundsInt(amazonBounds.xMax + JapanGap, -BaySettings.Height / 2, 0, BaySettings.Width, BaySettings.Height, 1);
            DockInfo japanDock = BuildJapan(mapObject.transform, japanBounds, japanTiles, waterMap, landMap, lagoonLayer, sandLayer, grassLayer, decorMap, staticObstacles);

            // La côte de la Grande Barrière, à l'est de la baie.
            TileSet reefTiles = CreateReefTiles();
            var reefBounds = new BoundsInt(japanBounds.xMax + ReefGap, -ReefSettings.Height / 2, 0, ReefSettings.Width, ReefSettings.Height, 1);
            DockInfo reefDock = BuildReef(mapObject.transform, reefBounds, reefTiles, waterMap, landMap, lagoonLayer, sandLayer, grassLayer, decorMap, staticObstacles);

            // L'atoll des Maldives, à l'est de la Grande Barrière.
            TileSet atollTiles = CreateAtollTiles(tiles, reefTiles);
            var atollBounds = new BoundsInt(reefBounds.xMax + AtollGap, -AtollSettings.Height / 2, 0, AtollSettings.Width, AtollSettings.Height, 1);
            DockInfo atollDock = BuildAtoll(mapObject.transform, atollBounds, atollTiles, waterMap, landMap, lagoonLayer, sandLayer, grassLayer, decorMap, staticObstacles);

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
            var japanRegion = new Rect(japanBounds.xMin, japanBounds.yMin, japanBounds.size.x, japanBounds.size.y);
            var reefRegion = new Rect(reefBounds.xMin, reefBounds.yMin, reefBounds.size.x, reefBounds.size.y);
            var atollRegion = new Rect(atollBounds.xMin, atollBounds.yMin, atollBounds.size.x, atollBounds.size.y);
            worldMap.SetRegions(new[] { islandRegion, lakeRegion, amazonRegion, japanRegion, reefRegion, atollRegion },
                new[] { islandDock.Area, lakeDock.Area, amazonDock.Area, japanDock.Area, reefDock.Area, atollDock.Area });
            BuildAtollLife(mapObject.transform, worldMap, player.transform, atollDock.Area);
            BuildReefLife(mapObject.transform, worldMap, player.transform, reefDock.Area);
            BuildJapanLife(mapObject.transform, worldMap, player.transform, japanDock.Area);
            BuildAmazonLife(mapObject.transform, worldMap, player.transform, amazonDock.Area);
            BuildLakeLife(mapObject.transform, worldMap, player.transform, lakeDock.Area);
            BuildIslandLife(mapObject.transform, worldMap, player.transform, islandDock.Area);

            // Coins de pêche (bancs, bulles, chasses) de chaque zone, que le lancer vise tout seul.
            FishingSpots islandSpots = BuildSpots(mapObject.transform, "Coins de pêche de l'île", worldMap, 0, islandDock.Area);
            FishingSpots lakeSpots = BuildSpots(mapObject.transform, "Coins de pêche du lac", worldMap, 1, lakeDock.Area);
            var spotsSo = new SerializedObject(controller);
            SerializedProperty spotList = spotsSo.FindProperty("_spots");
            FishingSpots amazonSpots = BuildSpots(mapObject.transform, "Coins de pêche du fleuve", worldMap, 2, amazonDock.Area);
            FishingSpots japanSpots = BuildSpots(mapObject.transform, "Coins de pêche de la baie", worldMap, 3, japanDock.Area);
            FishingSpots reefSpots = BuildSpots(mapObject.transform, "Coins de pêche du récif", worldMap, 4, reefDock.Area);
            FishingSpots atollSpots = BuildSpots(mapObject.transform, "Coins de pêche de l'atoll", worldMap, 5, atollDock.Area);
            // Remous légendaires (Tuna sur l'île, Bramafan au lac) : visibles une fois la légende réveillée au laboratoire.
            BuildLegendSpot(mapObject.transform, "Remous légendaire de l'île", worldMap, 0, islandDock.Area);
            BuildLegendSpot(mapObject.transform, "Remous légendaire du lac", worldMap, 1, lakeDock.Area);   // Bramafan
            spotList.arraySize = 6;
            spotList.GetArrayElementAtIndex(5).objectReferenceValue = atollSpots;
            spotList.GetArrayElementAtIndex(0).objectReferenceValue = islandSpots;
            spotList.GetArrayElementAtIndex(1).objectReferenceValue = lakeSpots;
            spotList.GetArrayElementAtIndex(2).objectReferenceValue = amazonSpots;
            spotList.GetArrayElementAtIndex(3).objectReferenceValue = japanSpots;
            spotList.GetArrayElementAtIndex(4).objectReferenceValue = reefSpots;
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

            // Maison du joueur : intérieur après celui de l'aquarium (qui s'arrête 174 cases après l'île), avant le lac.
            var housePosition = new Vector3(bounds.xMin + houseCell.x + 0.5f, bounds.yMin + houseCell.y, 0f);
            var houseInterior = new Vector2(bounds.xMax + 20f, bounds.yMin + 10f);   // au-dessus de celui de la boutique
            HouseBuilding house = BuildHouse(housePosition, houseInterior, worldMap, player.GetComponent<PlayerMovement>(), follow);
            Lighthouse lighthouse = BuildLighthouse(lighthousePosition);

            // Laboratoire : intérieur au-dessus de celui de la maison.
            var labPosition = new Vector3(bounds.xMin + labCell.x + 0.5f, bounds.yMin + labCell.y, 0f);
            var labInterior = new Vector2(bounds.xMax + 20f, bounds.yMin + 20f);
            LabBuilding lab = BuildLab(labPosition, labInterior, worldMap, player.GetComponent<PlayerMovement>(), follow);

            // Animalerie : intérieur au-dessus de celui du laboratoire.
            var petShopPosition = new Vector3(bounds.xMin + petShopCell.x + 0.5f, bounds.yMin + petShopCell.y, 0f);
            var petShopInterior = new Vector2(bounds.xMax + 20f, bounds.yMin + 30f);
            PetShopBuilding petShop = BuildPetShop(petShopPosition, petShopInterior, worldMap, player.GetComponent<PlayerMovement>(), follow);

            BuildMinimap(worldMap, decorMap, player.transform, tiles, lakeTiles, amazonTiles, japanTiles, reefTiles, atollTiles, shop, aquarium, house.transform, lighthouse.transform, lab.transform, petShop.transform, new[] { islandDock.Marker, lakeDock.Marker, amazonDock.Marker, japanDock.Marker, reefDock.Marker, atollDock.Marker });
            return new Buildings
            {
                Shop = shop, Aquarium = aquarium, Lab = lab,
                IslandRegion = islandRegion, IslandDock = islandDock,
                LakeRegion = lakeRegion, LakeDock = lakeDock,
                AmazonRegion = amazonRegion, AmazonDock = amazonDock,
                JapanRegion = japanRegion, JapanDock = japanDock,
                ReefRegion = reefRegion, ReefDock = reefDock,
                AtollRegion = atollRegion, AtollDock = atollDock,
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

        /// <summary>Le remous d'une légende ; sa légende et la progression sont reliées par le menu du prototype.</summary>
        private static LegendSpot BuildLegendSpot(Transform root, string name, WorldMap map, int region, Rect dock)
        {
            GameObject go = EnsureObject(name, root);
            var spot = go.GetComponent<LegendSpot>();
            if (spot == null)
            {
                spot = go.AddComponent<LegendSpot>();
            }

            // Remous et ondes dessinés à la taille du jeu (16 px par case) : nets, jamais agrandis.
            const string folder = "Assets/_Project/Art/Ambient";
            var center = new Vector2(0.5f, 0.5f);
            var eddy = new Sprite[LegendSpotArt.EddyFrames];
            for (int i = 0; i < eddy.Length; i++)
            {
                eddy[i] = PixelArt.Save($"{folder}/Remous_{i}.png", LegendSpotArt.Eddy(i), LegendSpotArt.PixelsPerUnit, center);
            }

            var waves = new Sprite[LegendSpotArt.WaveFrames];
            for (int i = 0; i < waves.Length; i++)
            {
                waves[i] = PixelArt.Save($"{folder}/Remous_Onde_{i}.png", LegendSpotArt.Wave(i), LegendSpotArt.PixelsPerUnit, center);
            }

            var so = new SerializedObject(spot);
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_region").intValue = region;
            so.FindProperty("_avoid").rectValue = new Rect(dock.xMin - 5f, dock.yMin - 5f, dock.width + 10f, dock.height + 10f);
            SetSprites(so.FindProperty("_eddyFrames"), eddy);
            SetSprites(so.FindProperty("_waveFrames"), waves);
            so.ApplyModifiedPropertiesWithoutUndo();
            return spot;
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

        /// <summary>Tuiles de la baie : celles de l'île recolorées (mer profonde, sable gris, mousse), arbres et décor japonais.</summary>
        private static TileSet CreateJapanTiles()
        {
            var set = new TileSet { Deep = new Tile[2], Shallow = new Tile[2], Sand = new Tile[3], Grass = new Tile[3] };
            for (int i = 0; i < 2; i++)
            {
                set.Deep[i] = SaveAnimatedWater($"Baie_Eau_{i}", f => JapanArt.Water(TileArt.DeepWater(i, f)));
                set.Shallow[i] = set.Deep[i];
            }

            for (int i = 0; i < 3; i++)
            {
                set.Sand[i] = SaveTile($"Baie_Sable_{i}", JapanArt.Sand(TileArt.Sand(i)), Vector2.one * 0.5f, TileArt.Size);
                set.Grass[i] = SaveTile($"Baie_Mousse_{i}", JapanArt.Moss(TileArt.Grass(i)), Vector2.one * 0.5f, TileArt.Size);
            }

            set.Palm = SaveTile("Sakura", JapanArt.Sakura(), JapanArt.TreePivot, TileArt.Size);
            set.Rocks = new[]
            {
                SaveTile("PinNoir", JapanArt.Pine(), JapanArt.TreePivot, TileArt.Size),
                SaveTile("Decor/Bambous", JapanArt.Bamboo(), new Vector2(0.5f, 2.5f / 40f), TileArt.Size),
            };
            // Lanterne de pierre : son nom contient « Lampadaire », elle s'allume la nuit (DayNightCycle).
            set.Reeds = new[] { SaveTile("Decor/Lampadaire_Toro", JapanArt.StoneLantern(), new Vector2(0.5f, 2f / 28f), TileArt.Size) };
            set.Lilies = new[] { SaveTile("Decor/Torii", JapanArt.Torii(), new Vector2(0.5f, 2f / 48f), TileArt.Size) };

            var shallow = new TransitionArt.Style
            {
                Fill = JapanArt.Water(TileArt.ShallowWater(0)),
                InnerBand = 0.06f, InnerColor = JapanArt.Water(new Color(0.40f, 0.82f, 0.84f)), InnerBlend = 0.6f,
                OuterBand = 0.07f, OuterColor = JapanArt.Water(new Color(0.24f, 0.66f, 0.74f)), DitherOuter = true,
            };
            var sand = new TransitionArt.Style
            {
                Fill = JapanArt.Sand(TileArt.Sand(0)),
                InnerBand = 0.07f, InnerColor = new Color(0.40f, 0.40f, 0.40f), InnerBlend = 1f,      // sable mouillé
                OuterBand = 0.07f, OuterColor = new Color(0.90f, 0.94f, 0.96f), DitherOuter = false,  // écume
            };
            var moss = new TransitionArt.Style
            {
                Fill = JapanArt.Moss(TileArt.Grass(0)),
                InnerBand = 0.06f, InnerColor = JapanArt.Moss(new Color(0.24f, 0.50f, 0.24f)), InnerBlend = 1f,
                OuterBand = 0.06f, OuterColor = JapanArt.Moss(new Color(0.40f, 0.68f, 0.34f)), DitherOuter = true,
            };

            set.LagoonEdges = SaveEdges("BaieEau", shallow);
            set.SandEdges = SaveEdges("BaieSable", sand);
            set.GrassEdges = SaveEdges("BaieMousse", moss);
            set.LagoonFull = new[] { set.LagoonEdges[15], SaveTile("Bords/BaieEau_15b", JapanArt.Water(TileArt.ShallowWater(1)), Vector2.one * 0.5f, TileArt.Size) };
            set.SandFull = new[]
            {
                set.SandEdges[15],
                SaveTile("Bords/BaieSable_15b", JapanArt.Sand(TileArt.Sand(0)), Vector2.one * 0.5f, TileArt.Size),
                SaveTile("Bords/BaieSable_15c", JapanArt.Sand(TileArt.Sand(1)), Vector2.one * 0.5f, TileArt.Size),
            };
            set.GrassFull = new[]
            {
                set.GrassEdges[15],
                SaveTile("Bords/BaieMousse_15b", JapanArt.PetalGrass(0), Vector2.one * 0.5f, TileArt.Size),
                SaveTile("Bords/BaieMousse_15c", JapanArt.PetalGrass(1), Vector2.one * 0.5f, TileArt.Size),
            };
            return set;
        }

        /// <summary>Dessine la baie (sans effacer le reste) : plage, collines de cerisiers, pins et bambous, lanternes, torii.</summary>
        private static DockInfo BuildJapan(Transform mapRoot, BoundsInt bounds, TileSet tiles, Tilemap waterMap, Tilemap landMap,
            Tilemap lagoonLayer, Tilemap sandLayer, Tilemap grassLayer, Tilemap decorMap, List<Rect> obstacles)
        {
            TerrainType[,] terrain = BayGenerator.Generate(BaySettings);
            int[,] distance = IslandGenerator.DistanceToLand(terrain, ShallowDistance + 1);
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            int seed = BaySettings.Seed;

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
                        landTiles[index] = tiles.Sand[hash < 0.9f ? 0 : hash < 0.96f ? 1 : 2];
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

            // Ponton au fond de la baie, vers le sud (la mer).
            int cx = width / 2;
            int cy = 0;
            while (cy < height - 2 && terrain[cx, cy] == TerrainType.Water)
            {
                cy++;
            }

            DockInfo dock = BuildDock(mapRoot, "Ponton - Baie japonaise", terrain, bounds, new Vector2Int(cx, cy + 1), Vector2Int.down);
            Vector2 dockCell = dock.Area.center - new Vector2(bounds.xMin, bounds.yMin);
            var taken = new bool[width, height];
            void Put(int x, int y, Tile tile)
            {
                taken[x, y] = true;
                decorMap.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), tile);
            }

            void Solid(int x, int y, float w, float h)
            {
                obstacles.Add(new Rect(bounds.xMin + x + 0.5f - w * 0.5f, bounds.yMin + y + 0.1f, w, h));
            }

            // Deux lanternes de pierre de part et d'autre du pied du ponton, puis d'autres le long de la plage.
            int lanternY = Mathf.RoundToInt(dock.Area.yMax - bounds.yMin) + 1;
            foreach (int lx in new[] { Mathf.RoundToInt(dockCell.x) - 3, Mathf.RoundToInt(dockCell.x) + 2 })
            {
                if (lx > 0 && lx < width && lanternY < height && terrain[lx, lanternY] != TerrainType.Water)
                {
                    Put(lx, lanternY, tiles.Reeds[0]);
                    Solid(lx, lanternY, 0.7f, 0.4f);
                }
            }

            // Le torii, planté dans l'eau de la baie, à côté du ponton.
            int tx = cx + 9;
            int ty = Mathf.FloorToInt(BayGenerator.ShoreAt(BaySettings, tx + 0.5f)) - 6;
            if (tx < width && ty > 0 && terrain[tx, ty] == TerrainType.Water)
            {
                Put(tx, ty, tiles.Lilies[0]);
            }

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float hash = IslandGenerator.CellHash(x, y, seed + 7);
                    bool nearDock = Mathf.Abs(x - dockCell.x) <= 5 && Mathf.Abs(y - dockCell.y) <= 8;
                    if (taken[x, y] || nearDock) continue;
                    bool besideWater = terrain[x, y - 1] == TerrainType.Water || terrain[x, y + 1] == TerrainType.Water;
                    if (terrain[x, y] == TerrainType.Sand && !besideWater && terrain[x, y + 1] == TerrainType.Grass && hash < 0.06f
                        && !taken[x - 1, y] && !taken[x + 1, y] && !taken[x - 2, y] && !taken[x + 2, y])
                    {
                        Put(x, y, tiles.Reeds[0]);
                        Solid(x, y, 0.7f, 0.4f);
                    }
                }
            }

            // Collines boisées : cerisiers, pins noirs et touffes de bambous.
            var trees = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool nearDock = Mathf.Abs(x - dockCell.x) <= 4 && Mathf.Abs(y - dockCell.y) <= 9;
                    if (terrain[x, y] != TerrainType.Grass || nearDock || taken[x, y] || IslandGenerator.CellHash(x, y, seed + 1) > 0.075f)
                    {
                        continue;
                    }

                    if (trees.Exists(other => Mathf.Abs(other.x - x) < 3 && Mathf.Abs(other.y - y) < 2))
                    {
                        continue;
                    }

                    trees.Add(new Vector2Int(x, y));
                    obstacles.Add(TownLayout.PalmFootprint(new Vector2(bounds.xMin + x + 0.5f, bounds.yMin + y + 0.5f)));
                    float pick = IslandGenerator.CellHash(x, y, seed + 3);
                    decorMap.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), pick < 0.45f ? tiles.Palm : pick < 0.8f ? tiles.Rocks[0] : tiles.Rocks[1]);
                }
            }

            return dock;
        }

        /// <summary>Tuiles du récif : celles de l'île recolorées (mer de corail, sable blanc, brousse), arbres et coraux.</summary>
        private static TileSet CreateReefTiles()
        {
            var set = new TileSet { Deep = new Tile[2], Shallow = new Tile[2], Sand = new Tile[3], Grass = new Tile[3] };
            for (int i = 0; i < 2; i++)
            {
                set.Deep[i] = SaveAnimatedWater($"Recif_Eau_{i}", f => ReefArt.Water(TileArt.DeepWater(i, f)));
                set.Shallow[i] = set.Deep[i];
            }

            for (int i = 0; i < 3; i++)
            {
                set.Sand[i] = SaveTile($"Recif_Sable_{i}", ReefArt.Sand(TileArt.Sand(i)), Vector2.one * 0.5f, TileArt.Size);
                set.Grass[i] = SaveTile($"Recif_Brousse_{i}", ReefArt.Bush(TileArt.Grass(i)), Vector2.one * 0.5f, TileArt.Size);
            }

            set.Palm = SaveTile("Eucalyptus", ReefArt.Eucalyptus(), ReefArt.TreePivot, TileArt.Size);
            set.Rocks = new[]
            {
                SaveTile("Pandanus", ReefArt.Pandanus(), ReefArt.TreePivot, TileArt.Size),
                SaveTile("Paletuvier", ReefArt.Mangrove(), ReefArt.TreePivot, TileArt.Size),
            };
            set.Lilies = new[]
            {
                SaveTile("Decor/Corail_0", ReefArt.Coral(0), Vector2.one * 0.5f, TileArt.Size),
                SaveTile("Decor/Corail_1", ReefArt.Coral(1), Vector2.one * 0.5f, TileArt.Size),
                SaveTile("Decor/Corail_2", ReefArt.Coral(2), Vector2.one * 0.5f, TileArt.Size),
                SaveTile("Decor/Corail_3", ReefArt.Coral(3), Vector2.one * 0.5f, TileArt.Size),
            };

            var shallow = new TransitionArt.Style
            {
                Fill = ReefArt.Water(TileArt.ShallowWater(0)),
                InnerBand = 0.06f, InnerColor = ReefArt.Water(new Color(0.46f, 0.88f, 0.86f)), InnerBlend = 0.6f,
                OuterBand = 0.07f, OuterColor = ReefArt.Water(new Color(0.28f, 0.72f, 0.78f)), DitherOuter = true,
            };
            var sand = new TransitionArt.Style
            {
                Fill = ReefArt.Sand(TileArt.Sand(0)),
                InnerBand = 0.07f, InnerColor = new Color(0.80f, 0.80f, 0.76f), InnerBlend = 1f,     // sable mouillé
                OuterBand = 0.07f, OuterColor = new Color(0.94f, 0.98f, 1.00f), DitherOuter = false, // écume
            };
            var bush = new TransitionArt.Style
            {
                Fill = ReefArt.Bush(TileArt.Grass(0)),
                InnerBand = 0.06f, InnerColor = ReefArt.Bush(new Color(0.24f, 0.50f, 0.24f)), InnerBlend = 1f,
                OuterBand = 0.06f, OuterColor = ReefArt.Bush(new Color(0.40f, 0.68f, 0.34f)), DitherOuter = true,
            };

            set.LagoonEdges = SaveEdges("RecifEau", shallow);
            set.SandEdges = SaveEdges("RecifSable", sand);
            set.GrassEdges = SaveEdges("RecifBrousse", bush);
            set.LagoonFull = new[] { set.LagoonEdges[15], SaveTile("Bords/RecifEau_15b", ReefArt.Water(TileArt.ShallowWater(1)), Vector2.one * 0.5f, TileArt.Size) };
            set.SandFull = new[]
            {
                set.SandEdges[15],
                SaveTile("Bords/RecifSable_15b", ReefArt.Sand(TileArt.Sand(1)), Vector2.one * 0.5f, TileArt.Size),
                SaveTile("Bords/RecifSable_15c", ReefArt.Sand(TileArt.Sand(2)), Vector2.one * 0.5f, TileArt.Size),
            };
            set.GrassFull = new[] { set.GrassEdges[15], set.GrassEdges[15], set.GrassEdges[15] };
            return set;
        }

        /// <summary>Dessine la côte du récif : plage, brousse d'eucalyptus, pandanus, mangrove de la crique, coraux au large.</summary>
        private static DockInfo BuildReef(Transform mapRoot, BoundsInt bounds, TileSet tiles, Tilemap waterMap, Tilemap landMap,
            Tilemap lagoonLayer, Tilemap sandLayer, Tilemap grassLayer, Tilemap decorMap, List<Rect> obstacles)
        {
            TerrainType[,] terrain = ReefGenerator.Generate(ReefSettings);
            bool[,] reef = ReefGenerator.Reef(ReefSettings, terrain);
            int[,] distance = IslandGenerator.DistanceToLand(terrain, ShallowDistance + 1);
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            int seed = ReefSettings.Seed;

            var waterTiles = new TileBase[width * height];
            var landTiles = new TileBase[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    float hash = IslandGenerator.CellHash(x, y, seed);
                    waterTiles[index] = tiles.Deep[hash < 0.5f ? 0 : 1];
                    if (terrain[x, y] == TerrainType.Sand) landTiles[index] = tiles.Sand[hash < 0.9f ? 0 : hash < 0.96f ? 1 : 2];
                    else if (terrain[x, y] == TerrainType.Grass) landTiles[index] = tiles.Grass[hash < 0.91f ? 0 : hash < 0.96f ? 1 : 2];
                }
            }

            waterMap.SetTilesBlock(bounds, waterTiles);
            landMap.SetTilesBlock(bounds, landTiles);
            // L'eau du récif est claire elle aussi : le lagon s'étend jusqu'aux coraux.
            FillDualLayer(lagoonLayer, bounds, width, height, (x, y) => distance[x, y] <= ShallowDistance || reef[x, y], tiles.LagoonEdges, tiles.LagoonFull, seed + 2, false);
            FillDualLayer(sandLayer, bounds, width, height, (x, y) => terrain[x, y] != TerrainType.Water, tiles.SandEdges, tiles.SandFull, seed + 3, false);
            FillDualLayer(grassLayer, bounds, width, height, (x, y) => terrain[x, y] == TerrainType.Grass, tiles.GrassEdges, tiles.GrassFull, seed + 4, false);

            // Ponton au milieu de la plage, vers le sud (le récif).
            int cx = Mathf.RoundToInt(width * 0.4f);
            int cy = 0;
            while (cy < height - 2 && terrain[cx, cy] == TerrainType.Water) cy++;
            DockInfo dock = BuildDock(mapRoot, "Ponton - Grande Barrière", terrain, bounds, new Vector2Int(cx, cy + 1), Vector2Int.down);
            Vector2 dockCell = dock.Area.center - new Vector2(bounds.xMin, bounds.yMin);

            var taken = new bool[width, height];
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float hash = IslandGenerator.CellHash(x, y, seed + 7);
                    bool nearDock = Mathf.Abs(x - dockCell.x) <= 4 && Mathf.Abs(y - dockCell.y) <= 8;
                    if (nearDock) continue;
                    if (reef[x, y] && hash < 0.16f)
                    {
                        // Coraux du récif, vus à travers l'eau.
                        decorMap.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), tiles.Lilies[Mathf.FloorToInt(hash / 0.16f * 4f) % 4]);
                        taken[x, y] = true;
                    }
                }
            }

            // Arbres : palétuviers au bord de la crique, pandanus sur le haut de la plage, eucalyptus dans la brousse.
            var trees = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool nearDock = Mathf.Abs(x - dockCell.x) <= 4 && Mathf.Abs(y - dockCell.y) <= 9;
                    if (nearDock || taken[x, y] || terrain[x, y] == TerrainType.Water) continue;
                    bool besideWater = terrain[x - (x > 0 ? 1 : 0), y] == TerrainType.Water || terrain[Mathf.Min(x + 1, width - 1), y] == TerrainType.Water;
                    bool inletBank = besideWater && y > ReefGenerator.ShoreAt(ReefSettings, x + 0.5f) + 3f;
                    bool beachTop = terrain[x, y] == TerrainType.Sand && y < height - 1 && terrain[x, y + 1] == TerrainType.Grass;
                    float hash = IslandGenerator.CellHash(x, y, seed + 1);
                    Tile tile = null;
                    if (inletBank && hash < 0.5f) tile = tiles.Rocks[1];
                    else if (beachTop && hash < 0.12f) tile = tiles.Rocks[0];
                    else if (terrain[x, y] == TerrainType.Grass && hash < 0.07f) tile = tiles.Palm;
                    if (tile == null || trees.Exists(o => Mathf.Abs(o.x - x) < 3 && Mathf.Abs(o.y - y) < 2)) continue;
                    trees.Add(new Vector2Int(x, y));
                    obstacles.Add(TownLayout.PalmFootprint(new Vector2(bounds.xMin + x + 0.5f, bounds.yMin + y + 0.5f)));
                    decorMap.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), tile);
                }
            }

            return dock;
        }

        /// <summary>La vie du récif : tortues sous l'eau, kangourous sur la plage, papillons Ulysse, pélicans.</summary>
        private static void BuildReefLife(Transform root, WorldMap map, Transform player, Rect dock)
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

            GameObject go = EnsureObject("Vie du récif", root);
            var life = go.GetComponent<AmbientLife>();
            if (life == null) life = go.AddComponent<AmbientLife>();

            var so = new SerializedObject(life);
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_region").intValue = 4;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_avoid").rectValue = new Rect(dock.xMin - 1.5f, dock.yMin - 1f, dock.width + 4f, dock.height + 2f);
            SerializedProperty swimmers = so.FindProperty("_swimmers");
            swimmers.arraySize = 1;
            SetSwimmer(swimmers.GetArrayElementAtIndex(0), Frames("TortueDeMer", 2, ReefLifeArt.SeaTurtle), 4, 0.4f);
            swimmers.GetArrayElementAtIndex(0).FindPropertyRelative("Order").intValue = -17; // sous l'eau
            SerializedProperty walkers = so.FindProperty("_walkers");
            walkers.arraySize = 1;
            SetSwimmer(walkers.GetArrayElementAtIndex(0), Frames("Kangourou", 2, ReefLifeArt.Kangaroo), 3, 0.6f);
            SetSprites(so.FindProperty("_dragonfly"), Frames("Morpho", 2, AmazonLifeArt.Morpho)); // papillon Ulysse, bleu lui aussi
            so.FindProperty("_dragonflies").intValue = 2;
            SetSprites(so.FindProperty("_jumpingFish"), Frames("PoissonSaute", 3, LakeLifeArt.JumpingFish));
            SetSprites(so.FindProperty("_ring"), Frames("Rond", 3, LakeLifeArt.Ring));
            SetSprites(so.FindProperty("_passer"), Frames("Pelican", 2, ReefLifeArt.Pelican));
            so.FindProperty("_passerShadow").objectReferenceValue = PixelArt.Save($"{folder}/Pelican_Ombre.png", ReefLifeArt.PelicanShadow(), TileArt.Size, Vector2.one * 0.5f);
            so.FindProperty("_cloud").objectReferenceValue = PixelArt.Save($"{folder}/OmbreNuage.png", LakeLifeArt.CloudShadow(), TileArt.Size, Vector2.one * 0.5f);
            so.FindProperty("_clouds").intValue = 4;
            SetNightLife(so, Frames);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Atoll des Maldives ----------

        /// <summary>Tuiles de l'atoll : celles de l'île recolorées ; cocotiers de l'île, coraux du récif, bungalow sur pilotis.</summary>
        private static TileSet CreateAtollTiles(TileSet island, TileSet reef)
        {
            var set = new TileSet { Deep = new Tile[2], Shallow = new Tile[2], Sand = new Tile[3], Grass = new Tile[3] };
            for (int i = 0; i < 2; i++)
            {
                set.Deep[i] = SaveAnimatedWater($"Atoll_Eau_{i}", f => AtollArt.Ocean(TileArt.DeepWater(i, f)));
                set.Shallow[i] = set.Deep[i];
            }

            for (int i = 0; i < 3; i++)
            {
                set.Sand[i] = SaveTile($"Atoll_Sable_{i}", AtollArt.Sand(TileArt.Sand(i)), Vector2.one * 0.5f, TileArt.Size);
                set.Grass[i] = SaveTile($"Atoll_Herbe_{i}", AtollArt.Green(TileArt.Grass(i)), Vector2.one * 0.5f, TileArt.Size);
            }

            set.Palm = island.Palm;
            set.Lilies = reef.Lilies;
            set.Rocks = new[] { SaveTile("Bungalow", AtollArt.Bungalow(), AtollArt.BungalowPivot, VolumeCanvas.PixelsPerUnit) };

            var lagoon = new TransitionArt.Style
            {
                Fill = AtollArt.Lagoon(TileArt.ShallowWater(0)),
                InnerBand = 0.06f, InnerColor = AtollArt.Lagoon(new Color(0.46f, 0.92f, 0.88f)), InnerBlend = 0.6f,
                OuterBand = 0.07f, OuterColor = AtollArt.Lagoon(new Color(0.24f, 0.70f, 0.80f)), DitherOuter = true,
            };
            var sand = new TransitionArt.Style
            {
                Fill = AtollArt.Sand(TileArt.Sand(0)),
                InnerBand = 0.07f, InnerColor = new Color(0.84f, 0.84f, 0.80f), InnerBlend = 1f,      // sable mouillé
                OuterBand = 0.07f, OuterColor = new Color(0.96f, 0.99f, 1.00f), DitherOuter = false,  // écume
            };
            var green = new TransitionArt.Style
            {
                Fill = AtollArt.Green(TileArt.Grass(0)),
                InnerBand = 0.06f, InnerColor = AtollArt.Green(new Color(0.22f, 0.50f, 0.24f)), InnerBlend = 1f,
                OuterBand = 0.06f, OuterColor = AtollArt.Green(new Color(0.40f, 0.70f, 0.34f)), DitherOuter = true,
            };

            set.LagoonEdges = SaveEdges("AtollLagon", lagoon);
            set.SandEdges = SaveEdges("AtollSable", sand);
            set.GrassEdges = SaveEdges("AtollHerbe", green);
            set.LagoonFull = new[] { set.LagoonEdges[15], SaveTile("Bords/AtollLagon_15b", AtollArt.Lagoon(TileArt.ShallowWater(1)), Vector2.one * 0.5f, TileArt.Size) };
            set.SandFull = new[]
            {
                set.SandEdges[15],
                SaveTile("Bords/AtollSable_15b", AtollArt.Sand(TileArt.Sand(1)), Vector2.one * 0.5f, TileArt.Size),
                SaveTile("Bords/AtollSable_15c", AtollArt.Sand(TileArt.Sand(2)), Vector2.one * 0.5f, TileArt.Size),
            };
            set.GrassFull = new[] { set.GrassEdges[15], set.GrassEdges[15], set.GrassEdges[15] };
            return set;
        }

        /// <summary>
        /// Dessine l'atoll : anneau d'îlots de sable blanc, cocoteraie, grand lagon turquoise, platier de corail au large,
        /// bungalows sur pilotis dans le lagon de part et d'autre du ponton (au sud du grand îlot du nord).
        /// </summary>
        private static DockInfo BuildAtoll(Transform mapRoot, BoundsInt bounds, TileSet tiles, Tilemap waterMap, Tilemap landMap,
            Tilemap lagoonLayer, Tilemap sandLayer, Tilemap grassLayer, Tilemap decorMap, List<Rect> obstacles)
        {
            TerrainType[,] terrain = AtollGenerator.Generate(AtollSettings);
            bool[,] reef = AtollGenerator.Reef(AtollSettings, terrain);
            int[,] distance = IslandGenerator.DistanceToLand(terrain, ShallowDistance + 1);
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            int seed = AtollSettings.Seed;

            var waterTiles = new TileBase[width * height];
            var landTiles = new TileBase[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    float hash = IslandGenerator.CellHash(x, y, seed);
                    waterTiles[index] = tiles.Deep[hash < 0.5f ? 0 : 1];
                    if (terrain[x, y] == TerrainType.Sand) landTiles[index] = tiles.Sand[hash < 0.9f ? 0 : hash < 0.96f ? 1 : 2];
                    else if (terrain[x, y] == TerrainType.Grass) landTiles[index] = tiles.Grass[hash < 0.91f ? 0 : hash < 0.96f ? 1 : 2];
                }
            }

            waterMap.SetTilesBlock(bounds, waterTiles);
            landMap.SetTilesBlock(bounds, landTiles);
            // Tout le lagon est clair, comme le platier de corail au large.
            FillDualLayer(lagoonLayer, bounds, width, height, (x, y) => distance[x, y] <= ShallowDistance || reef[x, y] || AtollGenerator.InLagoon(AtollSettings, x, y),
                tiles.LagoonEdges, tiles.LagoonFull, seed + 2, false);
            FillDualLayer(sandLayer, bounds, width, height, (x, y) => terrain[x, y] != TerrainType.Water, tiles.SandEdges, tiles.SandFull, seed + 3, false);
            FillDualLayer(grassLayer, bounds, width, height, (x, y) => terrain[x, y] == TerrainType.Grass, tiles.GrassEdges, tiles.GrassFull, seed + 4, false);

            // Ponton : au milieu du grand îlot du nord, vers le sud, dans le lagon.
            Vector2 center = AtollGenerator.Center(AtollSettings);
            int cx = Mathf.RoundToInt(center.x);
            int cy = Mathf.RoundToInt(center.y);
            while (cy < height - 2 && terrain[cx, cy] == TerrainType.Water) cy++;
            DockInfo dock = BuildDock(mapRoot, "Ponton - Atoll des Maldives", terrain, bounds, new Vector2Int(cx, cy + 1), Vector2Int.down);
            Vector2 dockCell = dock.Area.center - new Vector2(bounds.xMin, bounds.yMin);

            // Bungalows sur pilotis, alignés dans le lagon de part et d'autre du ponton.
            var taken = new bool[width, height];
            foreach (int dx in new[] { -17, -11, 11, 17 })
            {
                int bx = cx + dx;
                int by = cy - 4;
                while (by > 1 && terrain[bx, by + 1] != TerrainType.Water) by--;
                if (bx < 1 || bx >= width - 1 || by < 1 || terrain[bx, by] != TerrainType.Water || !AtollGenerator.InLagoon(AtollSettings, bx, by)) continue;
                decorMap.SetTile(new Vector3Int(bounds.xMin + bx, bounds.yMin + by, 0), tiles.Rocks[0]);
                for (int k = -1; k <= 1; k++) taken[bx + k, by] = true;
            }

            // Coraux sur le platier, au large.
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float hash = IslandGenerator.CellHash(x, y, seed + 7);
                    if (reef[x, y] && !taken[x, y] && hash < 0.16f)
                    {
                        decorMap.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), tiles.Lilies[Mathf.FloorToInt(hash / 0.16f * 4f) % 4]);
                    }
                }
            }

            // Cocoteraie sur l'herbe et quelques cocotiers en haut des plages.
            var trees = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool nearDock = Mathf.Abs(x - dockCell.x) <= 3 && Mathf.Abs(y - dockCell.y) <= 9;
                    if (nearDock || terrain[x, y] == TerrainType.Water) continue;
                    float hash = IslandGenerator.CellHash(x, y, seed + 1);
                    bool grove = terrain[x, y] == TerrainType.Grass && hash < 0.12f;
                    bool beach = terrain[x, y] == TerrainType.Sand && hash < 0.03f;
                    if (!grove && !beach) continue;
                    if (trees.Exists(o => Mathf.Abs(o.x - x) < 3 && Mathf.Abs(o.y - y) < 3)) continue;
                    trees.Add(new Vector2Int(x, y));
                    obstacles.Add(TownLayout.PalmFootprint(new Vector2(bounds.xMin + x + 0.5f, bounds.yMin + y + 0.5f)));
                    decorMap.SetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), tiles.Palm);
                }
            }

            return dock;
        }

        /// <summary>La vie de l'atoll : raies et tortues sous l'eau du lagon, bernard-l'ermite, papillons, poissons volants, frégate.</summary>
        private static void BuildAtollLife(Transform root, WorldMap map, Transform player, Rect dock)
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

            GameObject go = EnsureObject("Vie de l'atoll", root);
            var life = go.GetComponent<AmbientLife>();
            if (life == null) life = go.AddComponent<AmbientLife>();

            var so = new SerializedObject(life);
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_region").intValue = 5;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_avoid").rectValue = new Rect(dock.xMin - 1.5f, dock.yMin - 1f, dock.width + 4f, dock.height + 2f);
            SerializedProperty swimmers = so.FindProperty("_swimmers");
            swimmers.arraySize = 2;
            SetSwimmer(swimmers.GetArrayElementAtIndex(0), Frames("Raie", 2, MahanaLifeArt.Ray), 4, 0.45f);
            swimmers.GetArrayElementAtIndex(0).FindPropertyRelative("Order").intValue = -17; // sous l'eau
            SetSwimmer(swimmers.GetArrayElementAtIndex(1), Frames("TortueDeMer", 2, ReefLifeArt.SeaTurtle), 3, 0.4f);
            swimmers.GetArrayElementAtIndex(1).FindPropertyRelative("Order").intValue = -17;
            SerializedProperty walkers = so.FindProperty("_walkers");
            walkers.arraySize = 1;
            SetSwimmer(walkers.GetArrayElementAtIndex(0), Frames("BernardLErmite", 2, MahanaLifeArt.HermitCrab), 4, 0.3f);
            SetSprites(so.FindProperty("_dragonfly"), Frames("Papillon", 2, MahanaLifeArt.Butterfly));
            so.FindProperty("_dragonflies").intValue = 3;
            SetSprites(so.FindProperty("_jumpingFish"), Frames("PoissonVolant", 3, MahanaLifeArt.FlyingFish));
            SetSprites(so.FindProperty("_ring"), Frames("Rond", 3, LakeLifeArt.Ring));
            SetSprites(so.FindProperty("_passer"), Frames("Fregate", 2, MahanaLifeArt.Frigatebird));
            so.FindProperty("_passerShadow").objectReferenceValue = PixelArt.Save($"{folder}/Fregate_Ombre.png", MahanaLifeArt.FrigatebirdShadow(), TileArt.Size, Vector2.one * 0.5f);
            so.FindProperty("_cloud").objectReferenceValue = PixelArt.Save($"{folder}/OmbreNuage.png", LakeLifeArt.CloudShadow(), TileArt.Size, Vector2.one * 0.5f);
            so.FindProperty("_clouds").intValue = 3;
            SetNightLife(so, Frames);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>La vie de la baie : canards mandarins, pétales de cerisier, poissons qui sautent, grues dans le ciel.</summary>
        private static void BuildJapanLife(Transform root, WorldMap map, Transform player, Rect dock)
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

            GameObject go = EnsureObject("Vie de la baie", root);
            var life = go.GetComponent<AmbientLife>();
            if (life == null)
            {
                life = go.AddComponent<AmbientLife>();
            }

            var so = new SerializedObject(life);
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_region").intValue = 3;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_avoid").rectValue = new Rect(dock.xMin - 1.5f, dock.yMin - 1f, dock.width + 4f, dock.height + 2f);
            SerializedProperty swimmers = so.FindProperty("_swimmers");
            swimmers.arraySize = 1;
            SetSwimmer(swimmers.GetArrayElementAtIndex(0), Frames("CanardMandarin", 2, JapanLifeArt.MandarinDuck), 5, 0.5f);
            so.FindProperty("_walkers").arraySize = 0;
            SetSprites(so.FindProperty("_dragonfly"), Frames("PetaleSakura", 2, JapanLifeArt.Petal));
            so.FindProperty("_dragonflies").intValue = 5;
            SetSprites(so.FindProperty("_jumpingFish"), Frames("PoissonSaute", 3, LakeLifeArt.JumpingFish));
            SetSprites(so.FindProperty("_ring"), Frames("Rond", 3, LakeLifeArt.Ring));
            SetSprites(so.FindProperty("_passer"), Frames("Grue", 2, JapanLifeArt.Crane));
            so.FindProperty("_passerShadow").objectReferenceValue = PixelArt.Save($"{folder}/Grue_Ombre.png", JapanLifeArt.CraneShadow(), TileArt.Size, Vector2.one * 0.5f);
            so.FindProperty("_cloud").objectReferenceValue = PixelArt.Save($"{folder}/OmbreNuage.png", LakeLifeArt.CloudShadow(), TileArt.Size, Vector2.one * 0.5f);
            so.FindProperty("_clouds").intValue = 5;
            SetNightLife(so, Frames);
            so.ApplyModifiedPropertiesWithoutUndo();
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
            // Le ponton des voyages, le même partout : dessiné bout vers le bas, retourné s'il part vers le haut.
            int ppu = VolumeCanvas.PixelsPerUnit;
            bool up = direction.y > 0;
            Sprite planks = PixelArt.Save($"{TilesFolder}/{(up ? "Ponton_VersLeHaut" : "Ponton")}.png", DockArt.Dock(yMax - yMin, up), ppu, Vector2.one * 0.5f);
            SetSprite(dock, planks, -9);

            // Le voilier des voyages (celui de la carte du monde), amarré le long du bout du ponton, côté est.
            GameObject boat = EnsureObject("Barque", dock.transform);
            float boatLength = DockArt.BoatHeight / (float)ppu;
            float endY = bounds.yMin + end + 0.5f - direction.y * (boatLength * 0.5f + 0.2f);
            boat.transform.position = new Vector3(area.xMax + 0.35f + DockArt.BoatWidth / (2f * ppu), endY, 0f);
            Sprite boatSprite = PixelArt.Save($"{TilesFolder}/Voilier.png", DockArt.Boat(), ppu, Vector2.one * 0.5f);
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
        private static Vector2Int FindBuildingSite(TerrainType[,] terrain, Vector2Int spawn, Vector2Int? avoid, int minDistance, int halfWidth = 7, int front = 8, int back = 8, bool anyDirection = false,
            Vector2Int[] alsoAvoid = null)
        {
            bool Land(int x, int y) => !IslandGenerator.IsWater(terrain, x, y);
            bool FarFromOthers(int x, int y)
            {
                // Les autres bâtiments (plus petits que l'aquarium) : au moins 12 cases.
                foreach (Vector2Int other in alsoAvoid ?? new Vector2Int[0])
                {
                    if (Mathf.Max(Mathf.Abs(x - other.x), Mathf.Abs(y - other.y)) < 12) return false;
                }

                return true;
            }

            bool FarEnough(int x, int y) => FarFromOthers(x, y) && (!avoid.HasValue
                || Mathf.Max(Mathf.Abs(x - avoid.Value.x), Mathf.Abs(y - avoid.Value.y)) >= minDistance);

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

        /// <summary>
        /// Case du phare : la terre la plus au nord, avec un peu de terre autour (2 cases de large) pour son socle.
        /// </summary>
        private static Vector2Int FindLighthouseSite(TerrainType[,] terrain)
        {
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            bool Land(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && !IslandGenerator.IsWater(terrain, x, y);
            for (int y = height - 1; y >= 0; y--)
            {
                Vector2Int? best = null;
                for (int x = 1; x < width - 1; x++)
                {
                    if (Land(x - 1, y) && Land(x, y) && Land(x + 1, y) && Land(x - 1, y - 1) && Land(x, y - 1) && Land(x + 1, y - 1) && Land(x, y - 2))
                    {
                        // Le plus au milieu de la pointe.
                        if (!best.HasValue || Mathf.Abs(x - width / 2) < Mathf.Abs(best.Value.x - width / 2))
                        {
                            best = new Vector2Int(x, y - 1);
                        }
                    }
                }

                if (best.HasValue)
                {
                    return best.Value;
                }
            }

            return new Vector2Int(width / 2, height / 2);
        }

        /// <summary>La maison du joueur : façade sur l'île, pièce intérieure (lit, table, coffre) hors de l'île.</summary>
        private static HouseBuilding BuildHouse(Vector3 position, Vector2 interiorOrigin, WorldMap map, PlayerMovement player, CameraFollow follow)
        {
            const string folder = "Assets/_Project/Art/Buildings";
            Sprite exterior = PixelArt.Save($"{folder}/Maison.png", HouseArt.Exterior(), TileArt.Size, HouseArt.ExteriorPivot);
            Sprite interior = PixelArt.Save($"{folder}/Maison_Interieur.png", VolumeHouseArt.Interior(), VolumeCanvas.PixelsPerUnit, Vector2.zero);

            GameObject go = EnsureObject("Maison", null);
            go.transform.position = position;
            SetSprite(go, exterior, 10);
            var house = go.GetComponent<HouseBuilding>();
            if (house == null)
            {
                house = go.AddComponent<HouseBuilding>();
            }

            var so = new SerializedObject(house);
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_camera").objectReferenceValue = follow;
            so.FindProperty("_interiorOrigin").vector2Value = interiorOrigin;
            so.FindProperty("_footprint").vector2Value = new Vector2(5f, 2.6f);
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject room = EnsureObject("Maison - Intérieur", null);
            room.transform.position = interiorOrigin;
            GameObject floor = EnsureObject("Sol et murs", room.transform);
            floor.transform.localPosition = Vector3.zero;
            SetSprite(floor, interior, -5).spriteSortPoint = SpriteSortPoint.Center;
            return house;
        }

        /// <summary>
        /// Le laboratoire : façade sur l'île ; pièce intérieure (aquarium d'étude, étagères, bureau, socle, plante) hors de
        /// l'île, avec la scientifique derrière son bureau.
        /// </summary>
        private static LabBuilding BuildLab(Vector3 position, Vector2 interiorOrigin, WorldMap map, PlayerMovement player, CameraFollow follow)
        {
            const string folder = "Assets/_Project/Art/Buildings";
            int ppu = VolumeCanvas.PixelsPerUnit;
            Sprite exterior = PixelArt.Save($"{folder}/Laboratoire.png", LabArt.Exterior(), ppu, LabArt.ExteriorPivot);
            Sprite interior = PixelArt.Save($"{folder}/Laboratoire_Interieur.png", LabArt.Interior(), ppu, Vector2.zero);
            Sprite desk = PixelArt.Save($"{folder}/Laboratoire_Bureau.png", LabArt.DeskArt(), ppu, LabArt.DeskPivot);
            Sprite pedestal = PixelArt.Save($"{folder}/Laboratoire_Socle.png", LabArt.PedestalArt(), ppu, LabArt.PedestalPivot);
            Sprite plant = PixelArt.Save($"{folder}/Laboratoire_Plante.png", LabArt.PlantArt(), ppu, LabArt.PlantPivot);
            Sprite scientist = PixelArt.Save("Assets/_Project/Art/Characters/Scientifique.png", CharacterArt.Scientist(), TileArt.Size, CharacterArt.Pivot);

            GameObject go = EnsureObject("Laboratoire", null);
            go.transform.position = position;
            SetSprite(go, exterior, 10);
            var lab = go.GetComponent<LabBuilding>();
            if (lab == null)
            {
                lab = go.AddComponent<LabBuilding>();
            }

            var so = new SerializedObject(lab);
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_camera").objectReferenceValue = follow;
            so.FindProperty("_interiorOrigin").vector2Value = interiorOrigin;
            so.FindProperty("_footprint").vector2Value = new Vector2(4.8f, 2.6f);
            so.FindProperty("_skin").objectReferenceValue = UiSkinSetup.CreateOrUpdate();
            // Décor de l'atelier des artefacts, à l'échelle de l'interface (1 pixel = 4 unités, comme la charte).
            const string workshopFolder = "Assets/_Project/Art/UI/Atelier";
            PixelArt.EnsureFolder(workshopFolder);
            Sprite Ui(string file, Color[,] pixels) => PixelArt.Save($"{workshopFolder}/{file}.png", pixels, UiSkinArt.PixelsPerUnit, Vector2.one * 0.5f);
            SerializedProperty workshopLook = so.FindProperty("_workshopLook");
            workshopLook.FindPropertyRelative("Bench").objectReferenceValue = Ui("Etabli", WorkshopArt.Bench());
            workshopLook.FindPropertyRelative("Slab").objectReferenceValue = Ui("Dalle", WorkshopArt.Slab());
            workshopLook.FindPropertyRelative("Compartment").objectReferenceValue = Ui("Case_Velours", WorkshopArt.Compartment());
            workshopLook.FindPropertyRelative("Card").objectReferenceValue = Ui("Fiche", WorkshopArt.Card());
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject room = EnsureObject("Laboratoire - Intérieur", null);
            room.transform.position = interiorOrigin;
            GameObject floor = EnsureObject("Sol et murs", room.transform);
            floor.transform.localPosition = Vector3.zero;
            SetSprite(floor, interior, -5).spriteSortPoint = SpriteSortPoint.Center;

            void Prop(string name, Vector2 spot, Sprite sprite)
            {
                GameObject prop = EnsureObject(name, room.transform);
                prop.transform.position = spot;
                SetSprite(prop, sprite, 10);
            }

            Prop("Bureau", lab.DeskSpot, desk);
            Prop("Scientifique", lab.ScientistSpot, scientist);
            Prop("Socle", lab.PedestalSpot, pedestal);
            Prop("Plante", lab.PlantSpot, plant);

            // Vie du laboratoire : poissons, algues et bulles de l'aquarium d'étude, lueur du socle, scientifique.
            const string lifeFolder = "Assets/_Project/Art/Buildings/Laboratoire";
            PixelArt.EnsureFolder(lifeFolder);
            Sprite[] Fish(string name, Color body, Color fin, float length) => new[]
            {
                PixelArt.Save($"{lifeFolder}/{name}_0.png", LabArt.TankFish(body, fin, 0, length), ppu, Vector2.one * 0.5f),
                PixelArt.Save($"{lifeFolder}/{name}_1.png", LabArt.TankFish(body, fin, 1, length), ppu, Vector2.one * 0.5f),
            };

            var weed = new Sprite[3];
            for (int i = 0; i < 3; i++)
            {
                weed[i] = PixelArt.Save($"{lifeFolder}/Algue_{i}.png", LabArt.Seaweed(i), ppu, new Vector2(0.5f, 2f / LabArt.SeaweedHeight));
            }

            GameObject glowObject = EnsureObject("Lueur du socle", room.transform);
            glowObject.transform.position = lab.PedestalSpot + new Vector2(0f, 49f / ppu);
            SpriteRenderer glow = SetSprite(glowObject, PixelArt.Save($"{lifeFolder}/Lueur_Socle.png", LabArt.PedestalGlow(), ppu, new Vector2(0.5f, 4f / 14f)), 11);

            GameObject lifeObject = EnsureObject("Vie du laboratoire", room.transform);
            var life = lifeObject.GetComponent<LabLife>();
            if (life == null)
            {
                life = lifeObject.AddComponent<LabLife>();
            }

            var lifeSo = new SerializedObject(life);
            lifeSo.FindProperty("_lab").objectReferenceValue = lab;
            SerializedProperty looks = lifeSo.FindProperty("_fish");
            (Sprite[] frames, int count, Vector2 speed)[] fish =
            {
                (Fish("Poisson_Jaune", new Color(1f, 0.84f, 0.22f), new Color(0.96f, 0.66f, 0.14f), 16f), 1, new Vector2(0.35f, 0.5f)),
                (Fish("Poisson_Violet", new Color(0.44f, 0.34f, 0.84f), new Color(1f, 0.62f, 0.22f), 12f), 1, new Vector2(0.4f, 0.6f)),
                (Fish("Neon", new Color(0.30f, 0.72f, 0.96f), new Color(0.92f, 0.30f, 0.30f), 8f), 4, new Vector2(0.6f, 0.9f)),
            };
            looks.arraySize = fish.Length;
            for (int i = 0; i < fish.Length; i++)
            {
                SerializedProperty look = looks.GetArrayElementAtIndex(i);
                SerializedProperty frames = look.FindPropertyRelative("Frames");
                frames.arraySize = 2;
                frames.GetArrayElementAtIndex(0).objectReferenceValue = fish[i].frames[0];
                frames.GetArrayElementAtIndex(1).objectReferenceValue = fish[i].frames[1];
                look.FindPropertyRelative("Count").intValue = fish[i].count;
                look.FindPropertyRelative("Speed").vector2Value = fish[i].speed;
            }

            lifeSo.FindProperty("_weeds").intValue = 3;
            SerializedProperty weedFrames = lifeSo.FindProperty("_weed");
            weedFrames.arraySize = weed.Length;
            for (int i = 0; i < weed.Length; i++)
            {
                weedFrames.GetArrayElementAtIndex(i).objectReferenceValue = weed[i];
            }

            lifeSo.FindProperty("_bubble").objectReferenceValue = PixelArt.Save($"{lifeFolder}/Bulle.png", LabArt.Bubble(), ppu, Vector2.one * 0.5f);
            lifeSo.FindProperty("_glow").objectReferenceValue = glow;
            lifeSo.FindProperty("_scientist").objectReferenceValue = room.transform.Find("Scientifique");
            lifeSo.ApplyModifiedPropertiesWithoutUndo();
            return lab;
        }

        /// <summary>
        /// L'animalerie : façade sur l'île ; pièce intérieure (rayon de croquettes, perroquet, colliers, arbre à chat,
        /// clapier, panier du chien) hors de l'île, avec le vendeur derrière son comptoir.
        /// </summary>
        private static PetShopBuilding BuildPetShop(Vector3 position, Vector2 interiorOrigin, WorldMap map, PlayerMovement player, CameraFollow follow)
        {
            const string folder = "Assets/_Project/Art/Buildings";
            int ppu = VolumeCanvas.PixelsPerUnit;
            Sprite exterior = PixelArt.Save($"{folder}/Animalerie.png", PetShopArt.Exterior(), ppu, PetShopArt.ExteriorPivot);
            Sprite interior = PixelArt.Save($"{folder}/Animalerie_Interieur.png", PetShopArt.Interior(), ppu, Vector2.zero);
            Sprite counter = PixelArt.Save($"{folder}/Animalerie_Comptoir.png", PetShopArt.Counter(), ppu, PetShopArt.CounterPivot);
            Sprite keeper = PixelArt.Save("Assets/_Project/Art/Characters/Vendeur_Animalerie.png", CharacterArt.PetKeeper(), TileArt.Size, CharacterArt.Pivot);

            GameObject go = EnsureObject("Animalerie", null);
            go.transform.position = position;
            SetSprite(go, exterior, 10);
            var petShop = go.GetComponent<PetShopBuilding>();
            if (petShop == null)
            {
                petShop = go.AddComponent<PetShopBuilding>();
            }

            var so = new SerializedObject(petShop);
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_camera").objectReferenceValue = follow;
            so.FindProperty("_interiorOrigin").vector2Value = interiorOrigin;
            so.FindProperty("_footprint").vector2Value = new Vector2(4.8f, 2.6f);
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject room = EnsureObject("Animalerie - Intérieur", null);
            room.transform.position = interiorOrigin;
            GameObject floor = EnsureObject("Sol et murs", room.transform);
            floor.transform.localPosition = Vector3.zero;
            SetSprite(floor, interior, -5).spriteSortPoint = SpriteSortPoint.Center;

            GameObject counterObject = EnsureObject("Comptoir", room.transform);
            counterObject.transform.position = petShop.CounterSpot;
            SetSprite(counterObject, counter, 10);
            GameObject keeperObject = EnsureObject("Vendeur", room.transform);
            keeperObject.transform.position = petShop.KeeperSpot;
            SetSprite(keeperObject, keeper, 10);
            return petShop;
        }

        /// <summary>Le phare, au pied de sa case : la nuit, sa lanterne s'allume et un faisceau balaie la mer.</summary>
        private static Lighthouse BuildLighthouse(Vector3 position)
        {
            Sprite sprite = PixelArt.Save("Assets/_Project/Art/Buildings/Phare.png", LighthouseArt.Exterior(), TileArt.Size, LighthouseArt.Pivot);
            GameObject go = EnsureObject("Phare", null);
            go.transform.position = position;
            SetSprite(go, sprite, 10);
            var lighthouse = go.GetComponent<Lighthouse>();
            if (lighthouse == null)
            {
                lighthouse = go.AddComponent<Lighthouse>();
            }

            var so = new SerializedObject(lighthouse);
            so.FindProperty("_lanternOffset").vector2Value = LighthouseArt.LanternOffset;
            so.FindProperty("_glowMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            so.ApplyModifiedPropertiesWithoutUndo();
            return lighthouse;
        }

        private static ShopBuilding BuildShop(Vector3 position, Vector2 interiorOrigin, WorldMap map, PlayerMovement player, CameraFollow follow)
        {
            const string folder = "Assets/_Project/Art/Buildings";
            Sprite exterior = PixelArt.Save($"{folder}/Boutique.png", VolumeIslandArt.ShopExterior(), VolumeCanvas.PixelsPerUnit, VolumeIslandArt.ShopPivot);
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
            Sprite exterior = PixelArt.Save($"{folder}/Aquarium.png", AquariumArt.TowerExterior(), TileArt.Size, AquariumArt.TowerPivot);   // la tour des bassins
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
            AquariumAmazonArt.CloseLeftPassage(lakeRoomPixels); // plus de passage : on change d'étage par l'ascenseur
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
            Color[,] amazonRoomPixels = AquariumAmazonArt.AmazonRoom();
            AquariumAmazonArt.CloseLeftPassage(amazonRoomPixels);
            Sprite amazonRoom = PixelArt.Save($"{folder}/Aquarium_SalleAmazonie.png", amazonRoomPixels, TileArt.Size, Vector2.zero);
            Sprite amazonGlass = PixelArt.Save($"{folder}/Aquarium_SalleAmazonie_Vitre.png", AquariumAmazonArt.TankGlass(), TileArt.Size, Vector2.zero);
            GameObject amazonObject = EnsureObject("Salle Amazonie", room.transform);
            amazonObject.transform.localPosition = new Vector3(amazonX, 0f, 0f);
            SetSprite(amazonObject, amazonRoom, -6).spriteSortPoint = SpriteSortPoint.Center;
            GameObject amazonGlassObject = EnsureObject("Vitre du bassin Amazonie", room.transform);
            amazonGlassObject.transform.localPosition = new Vector3(amazonX, 0f, 0f);
            SetSprite(amazonGlassObject, amazonGlass, -2).spriteSortPoint = SpriteSortPoint.Center;

            // Salle Japon, au bout de la salle Amazonie par un troisième passage.
            float japanX = AquariumBuilding.JapanRoomX - AquariumBuilding.PassageWidth;
            Color[,] japanRoomPixels = AquariumJapanArt.JapanRoom();
            AquariumAmazonArt.CloseLeftPassage(japanRoomPixels);
            Sprite japanRoom = PixelArt.Save($"{folder}/Aquarium_SalleJapon.png", japanRoomPixels, TileArt.Size, Vector2.zero);
            Sprite japanGlass = PixelArt.Save($"{folder}/Aquarium_SalleJapon_Vitre.png", AquariumJapanArt.TankGlass(), TileArt.Size, Vector2.zero);
            GameObject japanObject = EnsureObject("Salle Japon", room.transform);
            japanObject.transform.localPosition = new Vector3(japanX, 0f, 0f);
            SetSprite(japanObject, japanRoom, -6).spriteSortPoint = SpriteSortPoint.Center;
            GameObject japanGlassObject = EnsureObject("Vitre du bassin Japon", room.transform);
            japanGlassObject.transform.localPosition = new Vector3(japanX, 0f, 0f);
            SetSprite(japanGlassObject, japanGlass, -2).spriteSortPoint = SpriteSortPoint.Center;

            // Salle Grande Barrière, au bout de la salle Japon.
            float reefX = AquariumBuilding.ReefRoomX - AquariumBuilding.PassageWidth;
            Color[,] reefRoomPixels = AquariumReefArt.ReefRoom();
            AquariumAmazonArt.CloseLeftPassage(reefRoomPixels);
            Sprite reefRoom = PixelArt.Save($"{folder}/Aquarium_SalleRecif.png", reefRoomPixels, TileArt.Size, Vector2.zero);
            Sprite reefGlass = PixelArt.Save($"{folder}/Aquarium_SalleRecif_Vitre.png", AquariumReefArt.TankGlass(), TileArt.Size, Vector2.zero);
            GameObject reefObject = EnsureObject("Salle Grande Barrière", room.transform);
            reefObject.transform.localPosition = new Vector3(reefX, 0f, 0f);
            SetSprite(reefObject, reefRoom, -6).spriteSortPoint = SpriteSortPoint.Center;
            GameObject reefGlassObject = EnsureObject("Vitre du bassin Grande Barrière", room.transform);
            reefGlassObject.transform.localPosition = new Vector3(reefX, 0f, 0f);
            SetSprite(reefGlassObject, reefGlass, -2).spriteSortPoint = SpriteSortPoint.Center;

            // Salle Maldives, au bout de la salle Grande Barrière.
            float atollX = AquariumBuilding.AtollRoomX - AquariumBuilding.PassageWidth;
            Color[,] atollRoomPixels = AquariumAtollArt.AtollRoom();
            AquariumAmazonArt.CloseLeftPassage(atollRoomPixels);
            Sprite atollRoom = PixelArt.Save($"{folder}/Aquarium_SalleMaldives.png", atollRoomPixels, TileArt.Size, Vector2.zero);
            Sprite atollGlass = PixelArt.Save($"{folder}/Aquarium_SalleMaldives_Vitre.png", AquariumAtollArt.TankGlass(), TileArt.Size, Vector2.zero);
            GameObject atollObject = EnsureObject("Salle Maldives", room.transform);
            atollObject.transform.localPosition = new Vector3(atollX, 0f, 0f);
            SetSprite(atollObject, atollRoom, -6).spriteSortPoint = SpriteSortPoint.Center;
            GameObject atollGlassObject = EnsureObject("Vitre du bassin Maldives", room.transform);
            atollGlassObject.transform.localPosition = new Vector3(atollX, 0f, 0f);
            SetSprite(atollGlassObject, atollGlass, -2).spriteSortPoint = SpriteSortPoint.Center;

            // L'ascenseur de chaque étage est dans le mur du bas, qu'on ne voit pas : un tapis contre ce mur indique sa
            // porte, comme le paillasson de sortie (la 2e colonne à méduses du hall a disparu).
            Transform oldColumn = room.transform.Find("Colonne à méduses 2");
            if (oldColumn != null)
            {
                Object.DestroyImmediate(oldColumn.gameObject);
            }

            Sprite liftMat = PixelArt.Save($"{folder}/Aquarium_Tapis_Ascenseur.png", ElevatorArt.Mat(), TileArt.Size, new Vector2(0.5f, 0f));
            for (int i = 0; i < AquariumBuilding.ElevatorSpots.Length; i++)
            {
                GameObject lift = EnsureObject($"Ascenseur {i}", room.transform);
                lift.transform.position = interiorOrigin + AquariumBuilding.ElevatorSpots[i];
                SetSprite(lift, liftMat, -5).spriteSortPoint = SpriteSortPoint.Pivot;
            }

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

            // Mobilier propre à chaque salle (barque et rondins au lac, pirogue et plantes en Amazonie, jardin zen au Japon).
            var furniture = new List<Rect>();
            Transform roomTransform = room.transform;
            void Furnish(string roomName, AquariumRoomLayout.Prop[] props, float roomX)
            {
                for (int i = 0; i < props.Length; i++)
                {
                    AquariumRoomLayout.Prop prop = props[i];
                    string file = $"{folder}/Aquarium/Meuble_{roomName}_{i + 1}.png";
                    Sprite sprite = PixelArt.Save(file, prop.Draw(), TileArt.Size, new Vector2(0.5f, 0f));
                    GameObject item = EnsureObject($"{prop.Name} ({roomName} {i + 1})", roomTransform);
                    item.transform.position = interiorOrigin + new Vector2(roomX, 0f) + prop.Foot;
                    SetSprite(item, sprite, 10);
                    furniture.Add(new Rect(roomX + prop.Foot.x - prop.Size.x * 0.5f, prop.Foot.y - 0.1f, prop.Size.x, prop.Size.y));
                }
            }

            Furnish("Lac", AquariumRoomLayout.Lake, AquariumBuilding.LakeRoomX);
            Furnish("Amazonie", AquariumRoomLayout.Amazon, AquariumBuilding.AmazonRoomX);
            Furnish("Japon", AquariumRoomLayout.Japan, AquariumBuilding.JapanRoomX);
            Furnish("Recif", AquariumRoomLayout.Reef, AquariumBuilding.ReefRoomX);
            Furnish("Maldives", AquariumRoomLayout.Atoll, AquariumBuilding.AtollRoomX);
            var furnitureSo = new SerializedObject(aquarium);
            SerializedProperty furnitureList = furnitureSo.FindProperty("_furniture");
            furnitureList.arraySize = furniture.Count;
            for (int i = 0; i < furniture.Count; i++)
            {
                furnitureList.GetArrayElementAtIndex(i).rectValue = furniture[i];
            }

            furnitureSo.ApplyModifiedPropertiesWithoutUndo();
            // Les anciens bancs des autres salles (il n'en reste que dans le grand hall).
            for (int i = AquariumBuilding.Benches.Length; i < 8; i++)
            {
                Transform old = room.transform.Find($"Banc {i + 1}");
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // Un bassin par zone : les espèces du lagon dans le grand hall, celles du lac dans la salle du lac.
            var mahana = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/IleMahana.asset");
            var annecy = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/LacAnnecy.asset");
            BuildTank(room.transform, "Poissons du bassin", aquarium, mahana, AquariumBuilding.HallTank);
            BuildTank(room.transform, "Poissons du bassin du lac", aquarium, annecy, AquariumBuilding.LakeTank);
            var amazon = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/FleuveAmazone.asset");
            BuildTank(room.transform, "Poissons du bassin Amazonie", aquarium, amazon, AquariumBuilding.AmazonTank);
            var japan = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/BaieJaponaise.asset");
            BuildTank(room.transform, "Poissons du bassin Japon", aquarium, japan, AquariumBuilding.JapanTank);
            var reefZone = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/GrandeBarriere.asset");
            BuildTank(room.transform, "Poissons du bassin Grande Barrière", aquarium, reefZone, AquariumBuilding.ReefTank);
            var atollZone = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/Maldives.asset");
            BuildTank(room.transform, "Poissons du bassin Maldives", aquarium, atollZone, AquariumBuilding.AtollTank);

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

            // Japon : laminaires brunes, ulves vertes devant, bancs de petits chinchards argentés, reflets bleus.
            Sprite[] kombu = Frames("Kombu", 3, f => AquariumLifeArt.Plant(f, 16, 46, 2, new Color(0.50f, 0.40f, 0.16f), new Color(0.34f, 0.26f, 0.10f), new Color(0.64f, 0.54f, 0.24f), true, 23), bottom);
            Sprite[] ulva = Frames("Ulve", 3, f => AquariumLifeArt.Plant(f, 18, 20, 4, new Color(0.40f, 0.70f, 0.30f), new Color(0.26f, 0.50f, 0.20f), new Color(0.60f, 0.84f, 0.44f), true, 29), bottom);
            Sprite[] sardine = Frames("Sardine", 2, f => AquariumLifeArt.TinyFish(f, new Color(0.40f, 0.56f, 0.70f), new Color(0.90f, 0.92f, 0.94f), new Color(0.60f, 0.70f, 0.76f)), center);
            Sprite[] japanCaustics = Frames("Reflets_Japon", 3, f => AquariumLifeArt.Caustics(f, new Color(0.70f, 0.88f, 1f)), center);
            BuildLife(room, "Vie du bassin Japon", aquarium, AquariumBuilding.JapanTank, 41, new[] { (kombu, 6, false), (ulva, 5, true) },
                bubble, 4, ray, 4, sardine, 3, 12, japanCaustics, new Sprite[0][], new Transform[0]);

            // Grande Barrière : coraux mous roses, herbiers devant, bancs de demoiselles bleues, reflets turquoise.
            Sprite[] softCoral = Frames("CorailMou", 3, f => AquariumLifeArt.Plant(f, 16, 30, 5, new Color(0.94f, 0.56f, 0.70f), new Color(0.74f, 0.36f, 0.52f), new Color(1f, 0.80f, 0.88f), false, 31), bottom);
            Sprite[] reefGrass = Frames("HerbierRecif", 3, f => AquariumLifeArt.Plant(f, 18, 18, 5, new Color(0.40f, 0.76f, 0.46f), new Color(0.24f, 0.56f, 0.32f), new Color(0.62f, 0.90f, 0.62f), false, 37), bottom);
            Sprite[] chromis = Frames("Demoiselle", 2, f => AquariumLifeArt.TinyFish(f, new Color(0.24f, 0.52f, 0.96f), new Color(0.56f, 0.80f, 1f), new Color(0.18f, 0.36f, 0.80f)), center);
            Sprite[] reefCaustics = Frames("Reflets_Recif", 3, f => AquariumLifeArt.Caustics(f, new Color(0.70f, 1f, 1f)), center);
            BuildLife(room, "Vie du bassin Grande Barrière", aquarium, AquariumBuilding.ReefTank, 47, new[] { (softCoral, 6, false), (reefGrass, 5, true) },
                bubble, 4, ray, 5, chromis, 3, 12, reefCaustics, new Sprite[0][], new Transform[0]);

            // Maldives : gorgones orange en éventail, herbiers devant, bancs d'anthias orange, reflets bleu vif.
            Sprite[] seaFan = Frames("Gorgone", 3, f => AquariumLifeArt.Plant(f, 18, 34, 4, new Color(0.96f, 0.52f, 0.24f), new Color(0.74f, 0.32f, 0.14f), new Color(1f, 0.76f, 0.46f), false, 43), bottom);
            Sprite[] atollGrass = Frames("HerbierAtoll", 3, f => AquariumLifeArt.Plant(f, 18, 16, 5, new Color(0.36f, 0.72f, 0.50f), new Color(0.22f, 0.52f, 0.36f), new Color(0.58f, 0.88f, 0.66f), false, 53), bottom);
            Sprite[] orangeAnthias = Frames("Anthias_Maldives", 2, f => AquariumLifeArt.TinyFish(f, new Color(0.98f, 0.52f, 0.30f), new Color(1f, 0.80f, 0.60f), new Color(0.86f, 0.36f, 0.50f)), center);
            Sprite[] atollCaustics = Frames("Reflets_Maldives", 3, f => AquariumLifeArt.Caustics(f, new Color(0.62f, 0.90f, 1f)), center);
            BuildLife(room, "Vie du bassin Maldives", aquarium, AquariumBuilding.AtollTank, 59, new[] { (seaFan, 6, false), (atollGrass, 5, true) },
                bubble, 4, ray, 5, orangeAnthias, 3, 12, atollCaustics, new Sprite[0][], new Transform[0]);
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

            set.Palm = SaveTile("Cocotier", VolumeIslandArt.Palm(), VolumeIslandArt.PalmPivot, VolumeCanvas.PixelsPerUnit);

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
            // Rochers en volume (32 px par case) : même taille que les anciens 16×12, pied au même endroit.
            Vector2 rockPivot = new Vector2(0.5f, 0.4f / (VolumeIslandArt.RockHeight / (float)VolumeCanvas.PixelsPerUnit));
            set.Decor[TownLayout.DecorKind.Rock] = new[] { SaveTile("Decor/Rocher_0", VolumeIslandArt.Rock(0), rockPivot, VolumeCanvas.PixelsPerUnit), SaveTile("Decor/Rocher_1", VolumeIslandArt.Rock(1), rockPivot, VolumeCanvas.PixelsPerUnit) };
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
        private static void BuildMinimap(WorldMap worldMap, Tilemap decorMap, Transform player, TileSet tiles, TileSet lakeTiles, TileSet amazonTiles, TileSet japanTiles, TileSet reefTiles, TileSet atollTiles, ShopBuilding shop, AquariumBuilding aquarium, Transform house, Transform lighthouse, Transform lab, Transform petShop, Transform[] docks)
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
            foreach (Tile t in japanTiles.Deep) entries.Add((t, new Color(0.10f, 0.36f, 0.56f)));
            foreach (Tile t in japanTiles.Sand) entries.Add((t, new Color(0.70f, 0.68f, 0.62f)));
            foreach (Tile t in japanTiles.Grass) entries.Add((t, new Color(0.36f, 0.56f, 0.30f)));
            entries.Add((japanTiles.Palm, new Color(0.92f, 0.66f, 0.76f)));
            entries.Add((japanTiles.Rocks[0], new Color(0.14f, 0.30f, 0.18f)));
            entries.Add((japanTiles.Rocks[1], new Color(0.30f, 0.50f, 0.22f)));
            entries.Add((japanTiles.Lilies[0], new Color(0.86f, 0.22f, 0.14f)));
            foreach (Tile t in reefTiles.Deep) entries.Add((t, new Color(0.10f, 0.40f, 0.66f)));
            foreach (Tile t in reefTiles.Sand) entries.Add((t, new Color(0.96f, 0.95f, 0.90f)));
            foreach (Tile t in reefTiles.Grass) entries.Add((t, new Color(0.56f, 0.60f, 0.34f)));
            entries.Add((reefTiles.Palm, new Color(0.44f, 0.56f, 0.44f)));
            entries.Add((reefTiles.Rocks[0], new Color(0.34f, 0.54f, 0.28f)));
            entries.Add((reefTiles.Rocks[1], new Color(0.16f, 0.36f, 0.20f)));
            foreach (Tile t in reefTiles.Lilies) entries.Add((t, new Color(0.90f, 0.56f, 0.56f)));
            foreach (Tile t in atollTiles.Deep) entries.Add((t, new Color(0.08f, 0.34f, 0.66f)));
            foreach (Tile t in atollTiles.Sand) entries.Add((t, new Color(0.98f, 0.97f, 0.92f)));
            foreach (Tile t in atollTiles.Grass) entries.Add((t, new Color(0.30f, 0.62f, 0.30f)));
            entries.Add((atollTiles.Rocks[0], new Color(0.70f, 0.56f, 0.34f)));
            Sprite dockIcon = PixelArt.Save("Assets/_Project/Art/UI/IconePonton.png", LakeArt.BoatIcon(), TileArt.Size, Vector2.one * 0.5f);

            var so = new SerializedObject(minimap);
            so.FindProperty("_map").objectReferenceValue = worldMap;
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_decor").objectReferenceValue = decorMap;
            Transform paving = decorMap.transform.parent.Find("Rendu - Pavés");
            so.FindProperty("_paving").objectReferenceValue = paving != null ? paving.GetComponent<Tilemap>() : null;
            Transform lagoon = decorMap.transform.parent.Find("Rendu - Lagon");
            so.FindProperty("_lagoon").objectReferenceValue = lagoon != null ? lagoon.GetComponent<Tilemap>() : null;
            SerializedProperty landmarks = so.FindProperty("_landmarks");
            landmarks.arraySize = 6 + docks.Length;
            SetLandmark(landmarks.GetArrayElementAtIndex(0), "Boutique", shop.transform, shopIcon);
            SetLandmark(landmarks.GetArrayElementAtIndex(1), "Aquarium", aquarium.transform, aquariumIcon);
            SetLandmark(landmarks.GetArrayElementAtIndex(2), "Maison", house, PixelArt.Save("Assets/_Project/Art/UI/IconeMaison.png", HouseArt.Icon(), TileArt.Size, Vector2.one * 0.5f));
            SetLandmark(landmarks.GetArrayElementAtIndex(3), "Phare", lighthouse, PixelArt.Save("Assets/_Project/Art/UI/IconePhare.png", LighthouseArt.Icon(), TileArt.Size, Vector2.one * 0.5f));
            for (int i = 0; i < docks.Length; i++)
            {
                SetLandmark(landmarks.GetArrayElementAtIndex(4 + i), "Ponton", docks[i], dockIcon);
            }

            SetLandmark(landmarks.GetArrayElementAtIndex(4 + docks.Length), "Laboratoire", lab,
                PixelArt.Save("Assets/_Project/Art/UI/IconeLaboratoire.png", LabArt.Icon(), TileArt.Size, Vector2.one * 0.5f));
            SetLandmark(landmarks.GetArrayElementAtIndex(5 + docks.Length), "Animalerie", petShop,
                PixelArt.Save("Assets/_Project/Art/UI/IconeAnimalerie.png", PetShopArt.Icon(), TileArt.Size, Vector2.one * 0.5f));
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
