using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Plan dessiné de la baie d'Ha Long (90×64 cases, y vers le haut) : une baie d'eau émeraude semée de pitons
    /// calcaires couverts de végétation ; au nord, la rive et son village de pêcheurs (maisons jaunes au toit de tuiles,
    /// temple au toit recourbé, banians, bambous, bananiers) au pied d'une rangée de montagnes de calcaire, dont l'une
    /// s'ouvre sur une grotte ; à l'ouest, une presqu'île de forêt ; dans la baie, un village flottant (maisons sur
    /// radeaux, cages à poissons) relié à la plage par une passerelle de planches, des jonques aux voiles rouges et des
    /// bateaux-paniers. Logique pure (testée) ; dessiné par <c>MapSetup.BuildHaLong</c>.
    /// </summary>
    public sealed class HaLongPlan
    {
        public const int Width = 90;
        public const int Height = 64;

        /// <summary>Pied des montagnes de calcaire du nord (on ne va pas plus haut).</summary>
        public const int CliffFoot = 56;

        public TerrainType[,] Terrain = new TerrainType[Width, Height];

        /// <summary>Chemin de terre du village (ponton, temple, maisons, passerelle).</summary>
        public bool[,] Path = new bool[Width, Height];

        public enum Plant { Banyan, Tree, Bamboo, Banana, Bush, Rock, Flowers }

        /// <summary>Plantes et rochers (case, sorte, variante) ; banians, arbres, bambous et rochers sont solides.</summary>
        public readonly List<(Vector2Int Cell, Plant Kind, int Variant)> Plants = new List<(Vector2Int, Plant, int)>();

        /// <summary>Pitons calcaires dans l'eau : pied (milieu de la base) et variante (0 aiguille, 1 double, 2 dôme).</summary>
        public readonly List<(Vector2Int Cell, int Variant)> Pitons = new List<(Vector2Int, int)>();

        /// <summary>Pied de chaque pan de montagne (8 cases de large), le long du nord.</summary>
        public readonly List<Vector2Int> Karsts = new List<Vector2Int>();

        /// <summary>Indice, dans <see cref="Karsts"/>, du pan qui s'ouvre sur la grotte.</summary>
        public int CaveIndex;

        public readonly List<Vector2Int> Junks = new List<Vector2Int>();
        public readonly List<Vector2Int> BasketBoats = new List<Vector2Int>();

        /// <summary>Maisons flottantes (pied, sur la plate-forme) et cages à poissons (cases d'eau à côté).</summary>
        public readonly List<Vector2Int> FloatingHouses = new List<Vector2Int>();
        public readonly List<Vector2Int> Cages = new List<Vector2Int>();

        public readonly List<Vector2Int> Houses = new List<Vector2Int>();
        public Vector2Int Temple;

        /// <summary>Plate-forme du village flottant et passerelle qui la relie à la plage : on y marche (et on y pêche).</summary>
        public RectInt Platform = new RectInt(56, 22, 15, 4);
        public RectInt Walkway;

        /// <summary>Colonne de gauche du ponton des voyages et case de la rive où il part, vers le sud.</summary>
        public Vector2Int DockFoot;

        public static HaLongPlan Build()
        {
            var plan = new HaLongPlan();
            plan.Paint();
            return plan;
        }

        /// <summary>Rangée où commence la rive du nord, à la colonne <paramref name="x"/>.</summary>
        public static float CoastAt(float x) => 41f + Mathf.Sin(x * 0.12f) * 2.5f + Mathf.Sin(x * 0.37f + 1f) * 1.2f;

        /// <summary>Bord est de la presqu'île de l'ouest, à la rangée <paramref name="y"/>.</summary>
        public static float WestAt(float y) => Mathf.Lerp(4f, 13f, Mathf.Clamp01(y / 8f)) + Mathf.Sin(y * 0.15f + 0.5f) * 3f;

        public static bool IsLand(float x, float y) => y >= CoastAt(x) || x < WestAt(y);

        private static float Hash(int x, int y, int seed) => IslandGenerator.CellHash(x, y, seed);

        private void Paint()
        {
            // 1. La terre au nord et à l'ouest, la baie partout ailleurs.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Terrain[x, y] = IsLand(x + 0.5f, y + 0.5f) ? TerrainType.Grass : TerrainType.Water;
                }
            }

            // 2. Plage de sable : large sous le village, étroite le long de la presqu'île.
            var beach = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int reach = y > 30 && x > 20 ? (Hash(x, y, 3) < 0.5f ? 3 : 2) : (Hash(x, y, 3) < 0.5f ? 2 : 1);
                    beach[x, y] = Terrain[x, y] == TerrainType.Grass && NearWater(x, y, reach);
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (beach[x, y]) Terrain[x, y] = TerrainType.Sand;
                }
            }

            // 3. Ponton des voyages, vers le sud, sous le village.
            int dockX = 44;
            int dockY = Mathf.RoundToInt(CoastAt(dockX)) + 3;
            while (dockY > 1 && Terrain[dockX, dockY - 1] != TerrainType.Water) dockY--;
            DockFoot = new Vector2Int(dockX, dockY);

            // 4. La passerelle du village flottant : de la plage jusqu'à la plate-forme, deux cases de large.
            int stemX = 62, landY = Platform.yMax;
            while (landY < Height - 1 && (Terrain[stemX, landY] == TerrainType.Water || Terrain[stemX + 1, landY] == TerrainType.Water)) landY++;
            Walkway = new RectInt(stemX, Platform.yMax, 2, landY - Platform.yMax);
            FloatingHouses.Add(new Vector2Int(58, 23));
            FloatingHouses.Add(new Vector2Int(68, 23));
            Cages.Add(new Vector2Int(Platform.xMin - 3, Platform.yMin + 1));
            Cages.Add(new Vector2Int(Platform.xMax + 2, Platform.yMin + 1));

            // 5. Le village sur la rive nord, la rangée de montagnes et sa grotte.
            Temple = new Vector2Int(30, 49);
            Houses.Add(new Vector2Int(70, 48));
            Houses.Add(new Vector2Int(79, 47));
            for (int x = 4; x <= Width + 2; x += 8)
            {
                Karsts.Add(new Vector2Int(x, CliffFoot));
            }

            CaveIndex = 6;   // le pan à x = 52, dans l'axe du ponton

            // 6. Pitons, jonques et bateaux-paniers dans la baie, loin de la rive et des passages.
            bool[,] busy = Reserved();
            foreach ((int x, int y, int v) in new[]
                     {
                         (22, 30, 0), (30, 12, 1), (38, 24, 2), (50, 14, 0), (54, 4, 2), (76, 32, 1), (84, 20, 0),
                         (82, 7, 2), (66, 11, 1), (25, 3, 0), (34, 34, 1), (44, 26, 0), (86, 34, 2), (16, 20, 2),
                     })
            {
                if (OpenWater(x - 3, y - 1, x + 3, y + 3, 2, busy)) Pitons.Add((new Vector2Int(x, y), v));
                MarkPiton(busy, x, y);
            }

            foreach ((int x, int y) in new[] { (40, 16), (73, 18), (26, 22), (62, 4) })
            {
                if (OpenWater(x - 3, y - 1, x + 3, y + 2, 1, busy))
                {
                    Junks.Add(new Vector2Int(x, y));
                    for (int dx = -3; dx <= 3; dx++) for (int dy = -1; dy <= 2; dy++) busy[x + dx, y + dy] = true;
                }
            }

            foreach ((int x, int y) in new[] { (38, 37), (51, 37), (74, 38), (56, 33) })
            {
                if (OpenWater(x, y, x, y, 0, busy)) BasketBoats.Add(new Vector2Int(x, y));
            }

            // 7. Chemin de terre : du ponton vers le temple à l'ouest, et vers la passerelle et les maisons à l'est.
            float row = 47f;
            var main = new[] { new Vector2(DockFoot.x + 1f, DockFoot.y + 0.5f), new Vector2(DockFoot.x + 1f, row), new Vector2(Temple.x + 0.5f, row), new Vector2(Temple.x + 0.5f, Temple.y - 0.5f) };
            var east = new[] { new Vector2(DockFoot.x + 1f, row), new Vector2(Walkway.xMin + 1f, row), new Vector2(Walkway.xMin + 1f, Walkway.yMax), new Vector2(Walkway.xMin + 1f, row), new Vector2(Houses[1].x + 0.5f, row) };
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] == TerrainType.Water) continue;
                    var cell = new Vector2(x + 0.5f, y + 0.5f);
                    Path[x, y] = DistanceToPolyline(cell, main) <= 0.9f || DistanceToPolyline(cell, east) <= 0.9f;
                }
            }

            // 8. Forêt de la presqu'île (banians, bambous), jardins du village (bananiers, buissons), rochers, fleurs.
            bool[,] reserved = Reserved();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (Terrain[x, y] != TerrainType.Grass || reserved[x, y] || Path[x, y] || y >= CliffFoot - 1) continue;
                    if (y + 2 < Height && (Path[x, y + 1] || Path[x, y + 2])) continue;
                    bool forest = x < WestAt(y + 0.5f) - 1f || x < 18;
                    if (Hash(x, y, 41) < (forest ? 0.07f : 0.025f) && !Plants.Exists(p => p.Kind != Plant.Flowers && p.Kind != Plant.Bush && (p.Cell - cell).sqrMagnitude < 18))
                    {
                        Plants.Add((cell, Plant.Banyan, (int)(Hash(x, y, 42) * 2f)));
                    }
                    else if (Hash(x, y, 49) < (forest ? 0.16f : 0.03f) && !Plants.Exists(p => p.Kind != Plant.Flowers && p.Kind != Plant.Bush && (p.Cell - cell).sqrMagnitude < 8))
                    {
                        Plants.Add((cell, Plant.Tree, (int)(Hash(x, y, 50) * 3f)));
                    }
                    else if (Hash(x, y, 43) < (forest ? 0.08f : 0.03f) && !Plants.Exists(p => p.Kind != Plant.Flowers && (p.Cell - cell).sqrMagnitude < 5))
                    {
                        Plants.Add((cell, Plant.Bamboo, (int)(Hash(x, y, 44) * 2f)));
                    }
                    else if (!forest && Hash(x, y, 45) < 0.05f && !Plants.Exists(p => p.Kind != Plant.Flowers && (p.Cell - cell).sqrMagnitude < 5))
                    {
                        Plants.Add((cell, Plant.Banana, 0));
                    }
                    else if (Hash(x, y, 46) < 0.025f && !Plants.Exists(p => (p.Cell - cell).sqrMagnitude < 9))
                    {
                        Plants.Add((cell, Plant.Rock, (x + y) % 2));
                    }
                    else if (Hash(x, y, 47) < 0.04f && !Plants.Exists(p => p.Cell == cell))
                    {
                        Plants.Add((cell, Plant.Bush, (x * 3 + y) % 2));
                    }
                    else if (Hash(x, y, 48) < 0.05f && !Plants.Exists(p => p.Cell == cell))
                    {
                        Plants.Add((cell, Plant.Flowers, (x + y * 3) % 2));
                    }
                }
            }
        }

        private static void MarkPiton(bool[,] busy, int x, int y)
        {
            for (int dx = -4; dx <= 4; dx++)
            {
                for (int dy = -2; dy <= 4; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < Width && ny < Height) busy[nx, ny] = true;
                }
            }
        }

        /// <summary>Le rectangle n'est fait que d'eau libre, à <paramref name="margin"/> cases au moins de la terre.</summary>
        private bool OpenWater(int x0, int y0, int x1, int y1, int margin, bool[,] busy)
        {
            for (int x = x0 - margin; x <= x1 + margin; x++)
            {
                for (int y = y0 - margin; y <= y1 + margin; y++)
                {
                    if (x < 0 || y < 0 || x >= Width || y >= Height || Terrain[x, y] != TerrainType.Water) return false;
                    if (x >= x0 && x <= x1 && y >= y0 && y <= y1 && busy[x, y]) return false;
                }
            }

            return true;
        }

        /// <summary>Cases réservées aux repères et aux passages : rien n'y pousse, aucun piton ne s'y dresse.</summary>
        public bool[,] Reserved()
        {
            var taken = new bool[Width, Height];
            void Mark(int x0, int y0, int x1, int y1)
            {
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(Width - 1, x1); x++)
                {
                    for (int y = Mathf.Max(0, y0); y <= Mathf.Min(Height - 1, y1); y++) taken[x, y] = true;
                }
            }

            Mark(Temple.x - 4, Temple.y - 2, Temple.x + 4, Temple.y + 5);
            foreach (Vector2Int h in Houses) Mark(h.x - 3, h.y - 2, h.x + 3, h.y + 4);
            Mark(DockFoot.x - 2, DockFoot.y - 9, DockFoot.x + 4, DockFoot.y + 2);
            Mark(Platform.xMin - 6, Platform.yMin - 3, Platform.xMax + 5, Platform.yMax + 1);
            Mark(Walkway.xMin - 2, Walkway.yMin, Walkway.xMax + 1, Walkway.yMax + 1);
            if (Karsts.Count > CaveIndex)
            {
                Vector2Int cave = Karsts[CaveIndex];
                Mark(cave.x - 3, cave.y - 4, cave.x + 3, cave.y);
            }

            return taken;
        }

        private bool NearWater(int x, int y, int reach)
        {
            for (int dx = -reach; dx <= reach; dx++)
            {
                for (int dy = -reach; dy <= reach; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < Width && ny < Height && Terrain[nx, ny] == TerrainType.Water) return true;
                }
            }

            return false;
        }

        public static float DistanceToPolyline(Vector2 p, IReadOnlyList<Vector2> points)
        {
            float best = float.MaxValue;
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 a = points[i - 1], ab = points[i] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }

            return best;
        }
    }
}
