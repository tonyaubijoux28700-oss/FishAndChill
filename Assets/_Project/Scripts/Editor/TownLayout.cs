using System.Collections.Generic;
using FishAndChill.World;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Aménagement autour des bâtiments, pour qu'ils ne soient pas juste « posés » sur l'herbe, et de façon
    /// ordonnée : parvis symétrique devant l'aquarium prolongé par une allée centrale bordée de lampadaires en
    /// paires, chemins droits à angles droits (3 cases de large) qui se raccordent au bout de l'allée et au milieu
    /// du côté du parvis, lampadaires réguliers, décor en miroir de part et d'autre de la porte.
    /// Toutes les positions sont en cases de la grille de l'île.
    /// </summary>
    public static class TownLayout
    {
        public enum DecorKind { Hedge, HedgeFlowers, Bush, BushFlowers, FlowerBed, Lamp, Barrel, Crates, Rock, Signpost }

        public readonly struct Placement
        {
            public readonly Vector2Int Cell;
            public readonly DecorKind Kind;

            /// <summary>Variante de dessin imposée (pour un décor en miroir) ; -1 = au hasard de la case.</summary>
            public readonly int Variant;

            public Placement(Vector2Int cell, DecorKind kind, int variant = -1)
            {
                Cell = cell;
                Kind = kind;
                Variant = variant;
            }
        }

        public sealed class Result
        {
            /// <summary>Cases pavées (parvis, allée et chemins).</summary>
            public bool[,] Paved;

            /// <summary>Cases à laisser libres (bâtiments, abords des chemins, décor) : pas de cocotier dessus.</summary>
            public bool[,] Reserved;

            public readonly List<Placement> Decor = new List<Placement>();

            /// <summary>L'aquarium est-il à droite du panneau indicateur ?</summary>
            public bool AquariumToTheRightOfSign;
        }

        private const int PlazaHalfWidth = 7;
        private const int PlazaDepth = 5;
        private const int AvenueLength = 6;
        private const int LampSpacing = 6;

        /// <summary>
        /// Zone infranchissable d'un élément de décor : seulement son pied (on passe derrière un lampadaire, pas à
        /// travers). <paramref name="center"/> = centre de sa case, en coordonnées du monde.
        /// </summary>
        public static Rect Footprint(DecorKind kind, Vector2 center)
        {
            switch (kind)
            {
                case DecorKind.Hedge:
                case DecorKind.HedgeFlowers:
                    return new Rect(center.x - 0.5f, center.y + 0.1f, 1f, 0.4f);      // collée au mur derrière
                case DecorKind.Barrel:
                    return new Rect(center.x - 0.4f, center.y + 0.1f, 0.8f, 0.45f);
                case DecorKind.Crates:
                    return new Rect(center.x - 0.5f, center.y + 0.1f, 1f, 0.5f);
                case DecorKind.FlowerBed:
                    return new Rect(center.x - 0.95f, center.y - 0.55f, 1.9f, 0.85f);
                case DecorKind.Lamp:
                case DecorKind.Signpost:
                    return new Rect(center.x - 0.18f, center.y - 0.42f, 0.36f, 0.3f);
                default:                                                              // buissons, rochers
                    return new Rect(center.x - 0.45f, center.y - 0.45f, 0.9f, 0.45f);
            }
        }

        /// <summary>Pied du tronc d'un cocotier (le feuillage, lui, reste traversable).</summary>
        public static Rect PalmFootprint(Vector2 center) => new Rect(center.x - 0.2f, center.y - 0.35f, 0.4f, 0.3f);

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

        public static Result Build(TerrainType[,] terrain, Vector2Int spawn, Vector2Int shop, Vector2Int aquarium, Vector2Int house, int seed)
        {
            var layout = new Layout(terrain, shop, aquarium, house);
            layout.PavePlazas();
            List<List<Vector2Int>> segments = layout.PavePaths(spawn);
            layout.ReserveAroundPaving(2);
            layout.DecorateAquarium();
            layout.DecorateShop();
            layout.DecorateHouse();
            layout.LightPaths(segments);
            layout.PlaceSignpost(segments);
            layout.ScatterNature(seed);
            return layout.Result;
        }

        /// <summary>État de construction de l'aménagement.</summary>
        private sealed class Layout
        {
            private readonly TerrainType[,] _terrain;
            private readonly int _width;
            private readonly int _height;
            private readonly bool[,] _blocked;
            private readonly Vector2Int _shop;
            private readonly Vector2Int _aquarium;
            private readonly Vector2Int _house;
            private int _avenueEnd;

            public readonly Result Result;

            public Layout(TerrainType[,] terrain, Vector2Int shop, Vector2Int aquarium, Vector2Int house)
            {
                _house = house;
                _terrain = terrain;
                _width = terrain.GetLength(0);
                _height = terrain.GetLength(1);
                _shop = shop;
                _aquarium = aquarium;
                _blocked = new bool[_width, _height];
                Result = new Result { Paved = new bool[_width, _height], Reserved = new bool[_width, _height] };

                // Emprise des bâtiments (et du dôme de l'aquarium, qui recouvre ce qu'il y a derrière).
                Block(aquarium.x - 6, aquarium.y, aquarium.x + 6, aquarium.y + 10);
                Block(shop.x - 3, shop.y, shop.x + 3, shop.y + 3);
                Block(house.x - 3, house.y, house.x + 3, house.y + 4);
            }

            private bool Inside(int x, int y) => x >= 0 && y >= 0 && x < _width && y < _height;
            private bool Land(int x, int y) => Inside(x, y) && _terrain[x, y] != TerrainType.Water;
            private bool Grass(int x, int y) => Inside(x, y) && _terrain[x, y] == TerrainType.Grass;

            private void Block(int x0, int y0, int x1, int y1)
            {
                for (int x = x0; x <= x1; x++)
                {
                    for (int y = y0; y <= y1; y++)
                    {
                        if (Inside(x, y))
                        {
                            _blocked[x, y] = true;
                            Result.Reserved[x, y] = true;
                        }
                    }
                }
            }

            private void Pave(int x, int y, bool grassOnly)
            {
                if (Land(x, y) && !_blocked[x, y] && (!grassOnly || Grass(x, y)))
                {
                    Result.Paved[x, y] = true;
                    Result.Reserved[x, y] = true;
                }
            }

            /// <summary>Parvis symétrique de l'aquarium (coins du bas arrondis) et allée centrale ; parvis de la boutique.</summary>
            public void PavePlazas()
            {
                int ax = _aquarium.x;
                int ay = _aquarium.y;
                for (int dx = -PlazaHalfWidth; dx <= PlazaHalfWidth; dx++)
                {
                    for (int y = ay - PlazaDepth; y <= ay - 1; y++)
                    {
                        bool roundedCorner = Mathf.Abs(dx) >= PlazaHalfWidth - 1 && y == ay - PlazaDepth;
                        if (!roundedCorner)
                        {
                            Pave(ax + dx, y, grassOnly: false);
                        }
                    }
                }

                // Allée centrale, 3 cases de large, dans l'axe de la porte.
                _avenueEnd = ay - PlazaDepth;
                for (int k = 1; k <= AvenueLength; k++)
                {
                    int y = ay - PlazaDepth - k;
                    if (!Land(ax - 1, y) || !Land(ax, y) || !Land(ax + 1, y))
                    {
                        break;
                    }

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        Pave(ax + dx, y, grassOnly: false);
                    }

                    _avenueEnd = y;
                }

                for (int x = _shop.x - 4; x <= _shop.x + 4; x++)
                {
                    for (int y = _shop.y - 3; y <= _shop.y - 1; y++)
                    {
                        Pave(x, y, grassOnly: false);
                    }
                }

                // Petite cour pavée devant la maison.
                for (int x = _house.x - 2; x <= _house.x + 2; x++)
                {
                    for (int y = _house.y - 2; y <= _house.y - 1; y++)
                    {
                        Pave(x, y, grassOnly: false);
                    }
                }
            }

            /// <summary>
            /// Chemins droits à angles droits, 3 cases de large : de la plage au bout de l'allée, et de la boutique
            /// au milieu du côté du parvis. Renvoie les segments droits (pour aligner les lampadaires).
            /// </summary>
            public List<List<Vector2Int>> PavePaths(Vector2Int spawn)
            {
                var segments = new List<List<Vector2Int>>();
                int ax = _aquarium.x;

                // Plage → bout de l'allée : on descend dans l'axe, on tourne une fois, on rejoint la plage.
                var avenueFoot = new Vector2Int(ax, _avenueEnd - 1);
                List<List<Vector2Int>> toBeach = BestZPath(avenueFoot, spawn, verticalFirst: true);
                segments.AddRange(toBeach);

                // Boutique → le chemin principal, en T, bien plus bas que l'allée : le chemin descend du parvis de la
                // boutique puis rejoint le chemin de la plage. Le parvis de l'aquarium et son allée restent symétriques.
                var beachPath = new List<Vector2Int>();
                foreach (List<Vector2Int> segment in toBeach)
                {
                    beachPath.AddRange(segment);
                }

                Vector2Int junction = beachPath.Count > 0 ? beachPath[Mathf.Min(6, beachPath.Count - 1)] : avenueFoot;
                var shopExit = new Vector2Int(_shop.x, _shop.y - 5);
                segments.AddRange(BestZPath(shopExit, junction, verticalFirst: true));

                // Maison : un chemin la relie aussi au chemin principal.
                var houseExit = new Vector2Int(_house.x, _house.y - 3);
                segments.AddRange(BestZPath(houseExit, junction, verticalFirst: true));

                foreach (List<Vector2Int> segment in segments)
                {
                    foreach (Vector2Int cell in segment)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                Pave(cell.x + dx, cell.y + dy, grassOnly: true);
                            }
                        }
                    }
                }

                return segments;
            }

            /// <summary>
            /// Chemin en Z (trois segments droits) de <paramref name="from"/> à <paramref name="to"/> : on cherche
            /// l'endroit du virage qui évite l'eau et les bâtiments et passe le plus possible dans l'herbe.
            /// </summary>
            private List<List<Vector2Int>> BestZPath(Vector2Int from, Vector2Int to, bool verticalFirst)
            {
                List<List<Vector2Int>> best = null;
                int bestScore = int.MaxValue;
                int lo = verticalFirst ? Mathf.Min(from.y, to.y) : Mathf.Min(from.x, to.x);
                int hi = verticalFirst ? Mathf.Max(from.y, to.y) : Mathf.Max(from.x, to.x);
                for (int turn = lo; turn <= hi; turn++)
                {
                    var corner1 = verticalFirst ? new Vector2Int(from.x, turn) : new Vector2Int(turn, from.y);
                    var corner2 = verticalFirst ? new Vector2Int(to.x, turn) : new Vector2Int(turn, to.y);
                    var path = new List<List<Vector2Int>> { Line(from, corner1), Line(corner1, corner2), Line(corner2, to) };
                    int score = Score(path);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = path;
                    }
                }

                return best ?? new List<List<Vector2Int>>();
            }

            /// <summary>Coût d'un chemin : impossible s'il traverse l'eau ou un bâtiment, cher sur le sable.</summary>
            private int Score(List<List<Vector2Int>> path)
            {
                int score = 0;
                foreach (List<Vector2Int> segment in path)
                {
                    foreach (Vector2Int cell in segment)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int x = cell.x + dx;
                                int y = cell.y + dy;
                                if (!Inside(x, y))
                                {
                                    return int.MaxValue;
                                }

                                if (_blocked[x, y])
                                {
                                    return int.MaxValue;
                                }
                            }
                        }

                        if (!Land(cell.x, cell.y))
                        {
                            score += 1000;   // un peu d'eau près de la plage reste possible : le pavé n'y est pas posé
                        }
                        else
                        {
                            score += Grass(cell.x, cell.y) ? 1 : 3;
                        }
                    }
                }

                return score;
            }

            private static List<Vector2Int> Line(Vector2Int a, Vector2Int b)
            {
                var cells = new List<Vector2Int>();
                Vector2Int step = new Vector2Int(System.Math.Sign(b.x - a.x), System.Math.Sign(b.y - a.y));
                Vector2Int at = a;
                cells.Add(at);
                while (at != b)
                {
                    at += step;
                    cells.Add(at);
                }

                return cells;
            }

            /// <summary>Les abords des pavés restent dégagés (pas de cocotier ni de buisson au ras du chemin).</summary>
            public void ReserveAroundPaving(int margin)
            {
                var paved = Result.Paved;
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        if (!paved[x, y])
                        {
                            continue;
                        }

                        for (int dx = -margin; dx <= margin; dx++)
                        {
                            for (int dy = -margin; dy <= margin; dy++)
                            {
                                if (Inside(x + dx, y + dy))
                                {
                                    Result.Reserved[x + dx, y + dy] = true;
                                }
                            }
                        }
                    }
                }
            }

            private void Put(int x, int y, DecorKind kind, int variant = -1, bool allowOnPaving = false)
            {
                if (!Land(x, y) || _blocked[x, y] || (!allowOnPaving && Result.Paved[x, y]))
                {
                    return;
                }

                foreach (Placement other in Result.Decor)
                {
                    if (other.Cell.x == x && other.Cell.y == y)
                    {
                        return;
                    }

                    // Pas deux lampadaires collés : au moins 3 cases entre eux.
                    bool bothLamps = kind == DecorKind.Lamp && other.Kind == DecorKind.Lamp;
                    if (bothLamps && Mathf.Abs(other.Cell.x - x) < 3 && Mathf.Abs(other.Cell.y - y) < 3)
                    {
                        return;
                    }
                }

                Result.Decor.Add(new Placement(new Vector2Int(x, y), kind, variant));
                Result.Reserved[x, y] = true;
            }

            /// <summary>Pose la même chose des deux côtés de l'axe de la porte (cases ax - k et ax + k).</summary>
            private void Mirror(int k, int y, DecorKind kind, int variant = -1, bool allowOnPaving = false)
            {
                Put(_aquarium.x - k, y, kind, variant, allowOnPaving);
                Put(_aquarium.x + k, y, kind, variant, allowOnPaving);
            }

            /// <summary>Décor de l'aquarium, en miroir : haies, massifs, lampadaires du parvis et de l'allée, buissons.</summary>
            public void DecorateAquarium()
            {
                int ay = _aquarium.y;
                for (int k = 3; k <= 5; k++)
                {
                    Mirror(k, ay - 1, k % 2 == 0 ? DecorKind.HedgeFlowers : DecorKind.Hedge, (k / 2) % 2, allowOnPaving: true);
                }

                Mirror(4, ay - 3, DecorKind.FlowerBed, 0, allowOnPaving: true);

                // Lampadaires aux quatre coins du parvis, puis en paires le long de l'allée.
                Mirror(PlazaHalfWidth + 1, ay - 1, DecorKind.Lamp);
                Mirror(PlazaHalfWidth + 1, ay - PlazaDepth, DecorKind.Lamp);
                for (int y = ay - PlazaDepth - 2; y >= _avenueEnd; y -= 3)
                {
                    Mirror(2, y, DecorKind.Lamp);
                }

                // Buissons et rochers sur les côtés du bâtiment, identiques à gauche et à droite.
                Mirror(PlazaHalfWidth + 1, ay + 2, DecorKind.BushFlowers, 1);
                Mirror(PlazaHalfWidth + 1, ay + 5, DecorKind.Bush, 0);
                Mirror(PlazaHalfWidth, ay + 8, DecorKind.BushFlowers, 2);
                Mirror(PlazaHalfWidth, ay + 10, DecorKind.Rock, 1);
            }

            /// <summary>Boutique : tonneaux et caisses contre la façade, lampadaires aux coins du parvis, buissons.</summary>
            public void DecorateShop()
            {
                int sx = _shop.x;
                int sy = _shop.y;
                Put(sx + 3, sy - 1, DecorKind.Barrel, allowOnPaving: true);
                Put(sx + 4, sy - 1, DecorKind.Barrel, allowOnPaving: true);
                Put(sx - 4, sy - 1, DecorKind.Crates, allowOnPaving: true);
                // Deux lampadaires de part et d'autre du chemin qui part du parvis (comme ceux de l'allée).
                Put(sx - 2, sy - 5, DecorKind.Lamp);
                Put(sx + 2, sy - 5, DecorKind.Lamp);
                Put(sx - 5, sy + 1, DecorKind.BushFlowers, 1);
                Put(sx + 5, sy + 1, DecorKind.BushFlowers, 1);
            }

            /// <summary>Maison : massifs fleuris de part et d'autre, un tonneau près de la porte, une lanterne au bout de la cour.</summary>
            public void DecorateHouse()
            {
                int hx = _house.x;
                int hy = _house.y;
                Put(hx - 4, hy + 1, DecorKind.BushFlowers, 0);
                Put(hx + 4, hy + 1, DecorKind.BushFlowers, 2);
                Put(hx + 3, hy - 1, DecorKind.Barrel, allowOnPaving: true);
                Put(hx - 3, hy - 3, DecorKind.Lamp);
                Put(hx + 4, hy + 4, DecorKind.Bush, 0);
            }

            /// <summary>Un lampadaire toutes les <see cref="LampSpacing"/> cases, toujours du même côté du chemin.</summary>
            public void LightPaths(List<List<Vector2Int>> segments)
            {
                foreach (List<Vector2Int> segment in segments)
                {
                    if (segment.Count < 5)
                    {
                        continue;
                    }

                    bool vertical = segment[0].x == segment[segment.Count - 1].x;
                    for (int i = 3; i < segment.Count - 2; i += LampSpacing)
                    {
                        Vector2Int cell = segment[i];
                        int x = vertical ? cell.x - 2 : cell.x;   // à gauche d'un chemin vertical
                        int y = vertical ? cell.y : cell.y + 2;   // au nord d'un chemin horizontal
                        if (Grass(x, y) && Result.Paved[cell.x, cell.y])
                        {
                            Put(x, y, DecorKind.Lamp);
                        }
                    }
                }
            }

            /// <summary>Panneau indicateur là où le chemin de la plage atteint l'herbe.</summary>
            public void PlaceSignpost(List<List<Vector2Int>> segments)
            {
                // Le chemin de la plage est le premier : on le parcourt depuis la plage.
                var beachPath = new List<Vector2Int>();
                for (int s = 0; s < segments.Count && s < 3; s++)
                {
                    beachPath.AddRange(segments[s]);
                }

                for (int i = beachPath.Count - 1; i >= 0; i--)
                {
                    Vector2Int cell = beachPath[i];
                    if (!Result.Paved[cell.x, cell.y])
                    {
                        continue;
                    }

                    // Du côté opposé à la boutique, pour ne pas gêner le raccord de son chemin.
                    int x = cell.x + (_shop.x > cell.x ? -2 : 2);
                    if (Grass(x, cell.y + 1))
                    {
                        Put(x, cell.y + 1, DecorKind.Signpost);
                        Result.AquariumToTheRightOfSign = _aquarium.x > x;
                    }

                    return;
                }
            }

            /// <summary>Un peu de nature ailleurs : buissons (parfois fleuris) et rochers, loin des chemins.</summary>
            public void ScatterNature(int seed)
            {
                for (int x = 1; x < _width - 1; x++)
                {
                    for (int y = 1; y < _height - 1; y++)
                    {
                        if (!Grass(x, y) || Result.Reserved[x, y])
                        {
                            continue;
                        }

                        float hash = IslandGenerator.CellHash(x, y, seed + 9);
                        DecorKind? kind = hash < 0.012f ? DecorKind.Bush : hash < 0.018f ? DecorKind.BushFlowers : hash < 0.023f ? DecorKind.Rock : (DecorKind?)null;
                        if (kind.HasValue)
                        {
                            Put(x, y, kind.Value);
                        }
                    }
                }
            }
        }
    }
}
