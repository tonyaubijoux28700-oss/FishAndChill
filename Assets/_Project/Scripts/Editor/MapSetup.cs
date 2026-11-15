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
        private const int LakeGap = 100;

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
            worldMap.SetRegions(new[] { islandRegion, lakeRegion }, new[] { islandDock.Area, lakeDock.Area });
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

            BuildMinimap(worldMap, decorMap, player.transform, tiles, lakeTiles, shop, aquarium, new[] { islandDock.Marker, lakeDock.Marker });
            return new Buildings
            {
                Shop = shop, Aquarium = aquarium,
                IslandRegion = islandRegion, IslandDock = islandDock,
                LakeRegion = lakeRegion, LakeDock = lakeDock,
            };
        }

        // ---------- Lac d'Annecy et pontons ----------

        /// <summary>Tuiles du lac : celles de l'île recolorées (eau alpine, galets, prés) et des sapins.</summary>
        private static TileSet CreateLakeTiles()
        {
            var set = new TileSet { Deep = new Tile[2], Shallow = new Tile[2], Sand = new Tile[3], Grass = new Tile[3] };
            for (int i = 0; i < 2; i++)
            {
                set.Deep[i] = SaveTile($"Lac_Eau_{i}", LakeArt.Water(TileArt.DeepWater(i)), Vector2.one * 0.5f, TileArt.Size);
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
                SaveTile("Bords/LacPre_15b", LakeArt.Meadow(TileArt.Grass(1)), Vector2.one * 0.5f, TileArt.Size),
                SaveTile("Bords/LacPre_15c", LakeArt.Meadow(TileArt.Grass(2)), Vector2.one * 0.5f, TileArt.Size),
            };
            return set;
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

            // Forêt de sapins sur les prés, plus dense que les cocotiers, jamais près du ponton.
            var pines = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool nearDock = Mathf.Abs(x - dockCell.x) <= 4 && Mathf.Abs(y - dockCell.y) <= 7;
                    if (terrain[x, y] != TerrainType.Grass || nearDock || IslandGenerator.CellHash(x, y, seed + 1) > 0.09f)
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
            SetSprite(floor, interior, -5).spriteSortPoint = SpriteSortPoint.Center;

            // Les poissons nagent entre le fond (-5) et la vitre (-3).
            GameObject glassObject = EnsureObject("Vitre du bassin", room.transform);
            glassObject.transform.localPosition = Vector3.zero;
            SetSprite(glassObject, glass, -3).spriteSortPoint = SpriteSortPoint.Center;

            // Mobilier de la salle (triés par les pieds, comme le joueur) : colonnes à méduses et bancs.
            Sprite column = PixelArt.Save($"{folder}/Aquarium_Colonne_Meduses.png", AquariumArt.JellyfishColumn(), TileArt.Size, new Vector2(0.5f, 0f));
            Sprite bench = PixelArt.Save($"{folder}/Aquarium_Banc.png", AquariumArt.Bench(), TileArt.Size, new Vector2(0.5f, 0f));
            for (int i = 0; i < AquariumBuilding.JellyfishColumns.Length; i++)
            {
                GameObject item = EnsureObject($"Colonne à méduses {i + 1}", room.transform);
                item.transform.position = interiorOrigin + AquariumBuilding.JellyfishColumns[i];
                SetSprite(item, column, 10);
            }

            for (int i = 0; i < AquariumBuilding.Benches.Length; i++)
            {
                GameObject item = EnsureObject($"Banc {i + 1}", room.transform);
                item.transform.position = interiorOrigin + AquariumBuilding.Benches[i];
                SetSprite(item, bench, 10);
            }

            GameObject fishObject = EnsureObject("Poissons du bassin", room.transform);
            fishObject.transform.localPosition = Vector3.zero;
            var tank = fishObject.GetComponent<AquariumTank>();
            if (tank == null)
            {
                tank = fishObject.AddComponent<AquariumTank>();
            }

            var tankSo = new SerializedObject(tank);
            tankSo.FindProperty("_building").objectReferenceValue = aquarium;
            tankSo.ApplyModifiedPropertiesWithoutUndo();
            return aquarium;
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
        private static void BuildMinimap(WorldMap worldMap, Tilemap decorMap, Transform player, TileSet tiles, TileSet lakeTiles, ShopBuilding shop, AquariumBuilding aquarium, Transform[] docks)
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
