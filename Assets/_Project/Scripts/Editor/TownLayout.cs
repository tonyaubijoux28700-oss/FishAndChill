using System.Collections.Generic;
using FishAndChill.World;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Aménagement autour des bâtiments, pour qu'ils ne soient pas juste « posés » sur l'herbe :
    /// parvis pavés devant les portes, chemins pavés (plage → boutique → aquarium), haies le long des façades,
    /// massifs de fleurs, lampadaires, tonneaux et caisses de la boutique, panneau indicateur, et un peu de nature
    /// (buissons, rochers) sur toute l'île. Toutes les positions sont en cases de la grille de l'île.
    /// </summary>
    public static class TownLayout
    {
        public enum DecorKind { Hedge, HedgeFlowers, Bush, BushFlowers, FlowerBed, Lamp, Barrel, Crates, Rock, Signpost }

        public readonly struct Placement
        {
            public readonly Vector2Int Cell;
            public readonly DecorKind Kind;

            public Placement(Vector2Int cell, DecorKind kind)
            {
                Cell = cell;
                Kind = kind;
            }
        }

        public sealed class Result
        {
            /// <summary>Cases pavées (parvis et chemins).</summary>
            public bool[,] Paved;

            /// <summary>Cases à laisser libres (bâtiments, décor) : pas de cocotier dessus.</summary>
            public bool[,] Reserved;

            public readonly List<Placement> Decor = new List<Placement>();

            /// <summary>L'aquarium est-il à droite du panneau indicateur ?</summary>
            public bool AquariumToTheRightOfSign;
        }

        /// <summary>Centre de gravité des cases de terre : le « milieu de l'île ».</summary>
        public static Vector2Int LandCentroid(TerrainType[,] terrain)
        {
            long sx = 0, sy = 0, count = 0;
            for (int x = 0; x < terrain.GetLength(0); x++)
            {
                for (int y = 0; y < terrain.GetLength(1); y++)
                {
                    if (terrain[x, y] != TerrainType.Water)
                    {
                        sx += x;
                        sy += y;
                        count++;
                    }
                }
            }

            return count == 0 ? new Vector2Int(terrain.GetLength(0) / 2, terrain.GetLength(1) / 2)
                : new Vector2Int((int)(sx / count), (int)(sy / count));
        }

        public static Result Build(TerrainType[,] terrain, Vector2Int spawn, Vector2Int shop, Vector2Int aquarium, int seed)
        {
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            var result = new Result { Paved = new bool[width, height], Reserved = new bool[width, height] };
            bool Inside(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;
            bool Land(int x, int y) => Inside(x, y) && terrain[x, y] != TerrainType.Water;

            // Emprise des bâtiments (et du dôme de l'aquarium, qui recouvre ce qu'il y a derrière).
            var blocked = new bool[width, height];
            void Block(int x0, int y0, int x1, int y1)
            {
                for (int x = x0; x <= x1; x++)
                {
                    for (int y = y0; y <= y1; y++)
                    {
                        if (Inside(x, y))
                        {
                            blocked[x, y] = true;
                            result.Reserved[x, y] = true;
                        }
                    }
                }
            }

            Block(aquarium.x - 9, aquarium.y, aquarium.x + 9, aquarium.y + 14);
            Block(shop.x - 3, shop.y, shop.x + 3, shop.y + 3);

            // Parvis : grand devant l'aquarium (coins arrondis), plus petit devant la boutique.
            void Pave(int x, int y)
            {
                if (Land(x, y) && !blocked[x, y])
                {
                    result.Paved[x, y] = true;
                    result.Reserved[x, y] = true;
                }
            }

            for (int x = aquarium.x - 10; x <= aquarium.x + 10; x++)
            {
                for (int y = aquarium.y - 6; y <= aquarium.y - 1; y++)
                {
                    bool corner = Mathf.Abs(x - aquarium.x) >= 9 && y == aquarium.y - 6;
                    if (!corner)
                    {
                        Pave(x, y);
                    }
                }
            }

            for (int x = shop.x - 4; x <= shop.x + 4; x++)
            {
                for (int y = shop.y - 3; y <= shop.y - 1; y++)
                {
                    Pave(x, y);
                }
            }

            // Chemins pavés de 2 cases, qui préfèrent l'herbe à la plage : plage → boutique → aquarium.
            List<Vector2Int> toShop = FindPath(terrain, blocked, spawn, new Vector2Int(shop.x, shop.y - 3));
            List<Vector2Int> toAquarium = FindPath(terrain, blocked, new Vector2Int(shop.x, shop.y - 3), new Vector2Int(aquarium.x, aquarium.y - 6));
            var pathCells = new List<Vector2Int>();
            pathCells.AddRange(toShop);
            pathCells.AddRange(toAquarium);
            foreach (Vector2Int cell in pathCells)
            {
                if (terrain[cell.x, cell.y] != TerrainType.Grass)
                {
                    continue; // le chemin commence là où finit le sable
                }

                Pave(cell.x, cell.y);
                if (Inside(cell.x + 1, cell.y) && terrain[cell.x + 1, cell.y] == TerrainType.Grass) Pave(cell.x + 1, cell.y);
                if (Inside(cell.x, cell.y + 1) && terrain[cell.x, cell.y + 1] == TerrainType.Grass) Pave(cell.x, cell.y + 1);
            }

            // ----- Décor -----
            void Put(int x, int y, DecorKind kind)
            {
                if (!Inside(x, y) || !Land(x, y) || blocked[x, y])
                {
                    return;
                }

                result.Decor.Add(new Placement(new Vector2Int(x, y), kind));
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (Inside(x + dx, y + dy))
                        {
                            result.Reserved[x + dx, y + dy] = true;
                        }
                    }
                }
            }

            // Aquarium : haies fleuries au pied des ailes, deux massifs, lampadaires aux coins du parvis,
            // buissons et rochers sur les côtés.
            for (int k = 4; k <= 9; k++)
            {
                DecorKind hedge = k % 2 == 0 ? DecorKind.HedgeFlowers : DecorKind.Hedge;
                Put(aquarium.x - k, aquarium.y - 1, hedge);
                Put(aquarium.x + k, aquarium.y - 1, hedge);
            }

            Put(aquarium.x - 6, aquarium.y - 4, DecorKind.FlowerBed);
            Put(aquarium.x + 6, aquarium.y - 4, DecorKind.FlowerBed);
            Put(aquarium.x - 11, aquarium.y - 6, DecorKind.Lamp);
            Put(aquarium.x + 11, aquarium.y - 6, DecorKind.Lamp);
            Put(aquarium.x - 11, aquarium.y - 1, DecorKind.Lamp);
            Put(aquarium.x + 11, aquarium.y - 1, DecorKind.Lamp);
            Put(aquarium.x - 11, aquarium.y + 2, DecorKind.BushFlowers);
            Put(aquarium.x + 11, aquarium.y + 2, DecorKind.BushFlowers);
            Put(aquarium.x - 11, aquarium.y + 5, DecorKind.Rock);
            Put(aquarium.x + 11, aquarium.y + 5, DecorKind.Bush);
            Put(aquarium.x - 10, aquarium.y + 8, DecorKind.Bush);
            Put(aquarium.x + 10, aquarium.y + 8, DecorKind.BushFlowers);

            // Boutique : tonneaux et caisses contre la façade, lampadaires, un buisson.
            Put(shop.x + 3, shop.y - 1, DecorKind.Barrel);
            Put(shop.x + 4, shop.y - 1, DecorKind.Barrel);
            Put(shop.x - 4, shop.y - 1, DecorKind.Crates);
            Put(shop.x - 5, shop.y - 3, DecorKind.Lamp);
            Put(shop.x + 5, shop.y - 3, DecorKind.Lamp);
            Put(shop.x - 4, shop.y + 1, DecorKind.BushFlowers);
            Put(shop.x + 4, shop.y + 2, DecorKind.Bush);

            // Lampadaires le long des chemins, et le panneau indicateur au début du chemin.
            bool signPlaced = false;
            int sinceLamp = 0;
            foreach (Vector2Int cell in pathCells)
            {
                if (terrain[cell.x, cell.y] != TerrainType.Grass)
                {
                    continue;
                }

                int side = (cell.x + cell.y) % 2 == 0 ? -1 : 2;
                int sx = cell.x + side;
                if (!signPlaced && Land(sx, cell.y) && !result.Paved[sx, cell.y])
                {
                    Put(sx, cell.y, DecorKind.Signpost);
                    result.AquariumToTheRightOfSign = aquarium.x > sx;
                    signPlaced = true;
                    sinceLamp = 0;
                    continue;
                }

                if (++sinceLamp >= 9 && Land(sx, cell.y) && !result.Paved[sx, cell.y] && !result.Reserved[sx, cell.y])
                {
                    Put(sx, cell.y, DecorKind.Lamp);
                    sinceLamp = 0;
                }
            }

            // Un peu de nature partout ailleurs : buissons (parfois fleuris) et rochers.
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (terrain[x, y] != TerrainType.Grass || result.Reserved[x, y])
                    {
                        continue;
                    }

                    float hash = IslandGenerator.CellHash(x, y, seed + 9);
                    if (hash < 0.012f)
                    {
                        Put(x, y, DecorKind.Bush);
                    }
                    else if (hash < 0.018f)
                    {
                        Put(x, y, DecorKind.BushFlowers);
                    }
                    else if (hash < 0.023f)
                    {
                        Put(x, y, DecorKind.Rock);
                    }
                }
            }

            return result;
        }

        /// <summary>Chemin le moins coûteux (4 voisins) : l'herbe coûte 1, le sable 4 ; ni eau ni bâtiment.</summary>
        private static List<Vector2Int> FindPath(TerrainType[,] terrain, bool[,] blocked, Vector2Int from, Vector2Int to)
        {
            int width = terrain.GetLength(0);
            int height = terrain.GetLength(1);
            var cost = new int[width, height];
            var previous = new Vector2Int[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cost[x, y] = int.MaxValue;
                }
            }

            var open = new SortedSet<(int cost, int x, int y)>();
            cost[from.x, from.y] = 0;
            open.Add((0, from.x, from.y));
            Vector2Int[] steps = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (open.Count > 0)
            {
                (int c, int cx, int cy) = open.Min;
                open.Remove(open.Min);
                if (cx == to.x && cy == to.y)
                {
                    break;
                }

                foreach (Vector2Int step in steps)
                {
                    int nx = cx + step.x;
                    int ny = cy + step.y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height || terrain[nx, ny] == TerrainType.Water || blocked[nx, ny])
                    {
                        continue;
                    }

                    int next = c + (terrain[nx, ny] == TerrainType.Grass ? 1 : 4);
                    if (next < cost[nx, ny])
                    {
                        if (cost[nx, ny] != int.MaxValue)
                        {
                            open.Remove((cost[nx, ny], nx, ny));
                        }

                        cost[nx, ny] = next;
                        previous[nx, ny] = new Vector2Int(cx, cy);
                        open.Add((next, nx, ny));
                    }
                }
            }

            var path = new List<Vector2Int>();
            if (cost[to.x, to.y] == int.MaxValue)
            {
                return path;
            }

            for (Vector2Int at = to; at != from; at = previous[at.x, at.y])
            {
                path.Add(at);
            }

            path.Add(from);
            path.Reverse();
            return path;
        }
    }
}
