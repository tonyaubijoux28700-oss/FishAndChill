using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Plan dessiné du lac d'Annecy (80×60 cases, y vers le haut), à la place d'une forme tirée au hasard : une muraille
    /// de montagnes au nord (plateau inaccessible, falaise), une cascade qui donne un torrent jusqu'au lac, une
    /// passerelle pour le franchir, un lac découpé (baie à l'ouest, presqu'île à l'est avec un chalet, un îlot et son
    /// arbre), une rive de galets, un chemin de terre qui fait le tour, une prairie fleurie au sud-ouest, des forêts
    /// denses à l'est et au nord-ouest, quelques sapins isolés ailleurs. Logique pure (testée) ; dessiné par
    /// <c>MapSetup.BuildLake</c>.
    /// </summary>
    public sealed class LakePlan
    {
        public const int Width = 80;
        public const int Height = 60;

        public TerrainType[,] Terrain = new TerrainType[Width, Height];

        /// <summary>Plateau des montagnes (au-dessus de la falaise) : inaccessible.</summary>
        public bool[,] Plateau = new bool[Width, Height];

        /// <summary>Face de la falaise (deux cases sous le bord du plateau) : inaccessible.</summary>
        public bool[,] Cliff = new bool[Width, Height];

        public bool[,] Path = new bool[Width, Height];
        public bool[,] Flowers = new bool[Width, Height];

        /// <summary>Densité de sapins (0 à 1) : forêts denses, clairières, arbres isolés.</summary>
        public float[,] Forest = new float[Width, Height];

        /// <summary>Hauteur du bord du plateau dans chaque colonne (première case du plateau).</summary>
        public int[] Ridge = new int[Width];

        public Vector2Int DockFoot;
        public Vector2Int Waterfall;          // pied de la cascade, au bas de la falaise
        public RectInt Bridge;                // cases de la passerelle (au-dessus du torrent)
        public Vector2Int Chalet;             // pied de la façade du chalet
        public Vector2Int IsletTree;
        public readonly List<Vector2Int> WaterRocks = new List<Vector2Int>();

        private static readonly Vector2[] RiverCourse = { new Vector2(20f, 46f), new Vector2(22f, 42f), new Vector2(26f, 39f), new Vector2(31f, 36.5f) };

        private static readonly Vector2[] PathCourse =
        {
            new Vector2(44f, 8f), new Vector2(31f, 9f), new Vector2(18f, 16f), new Vector2(13f, 27f), new Vector2(16f, 36f),
            new Vector2(22f, 39.5f), new Vector2(31f, 41.5f), new Vector2(46f, 41.5f), new Vector2(60f, 38f), new Vector2(67f, 31f),
            new Vector2(67f, 18f), new Vector2(57f, 9f), new Vector2(44f, 8f),
        };

        /// <summary>Petits chemins qui partent du tour du lac : vers le ponton et vers le chalet.</summary>
        private static readonly Vector2[][] Spurs =
        {
            new[] { new Vector2(44.5f, 8f), new Vector2(44.5f, 13f) },
            new[] { new Vector2(67f, 27.5f), new Vector2(63.5f, 27.5f) },
        };

        public static LakePlan Build()
        {
            var plan = new LakePlan();
            plan.Paint();
            return plan;
        }

        private static float Hash(int x, int y, int seed) => IslandGenerator.CellHash(x, y, seed);

        private void Paint()
        {
            // 1. Montagnes au nord : bord du plateau en vagues, falaise de deux cases dessous.
            for (int x = 0; x < Width; x++)
            {
                Ridge[x] = Mathf.RoundToInt(48f + 2.5f * Mathf.Sin(x * 0.17f) + 1.5f * Mathf.Sin(x * 0.07f + 1.3f));
                for (int y = 0; y < Height; y++)
                {
                    Terrain[x, y] = TerrainType.Grass;
                    Plateau[x, y] = y >= Ridge[x];
                    Cliff[x, y] = y < Ridge[x] && y >= Ridge[x] - 2;
                }
            }

            // 2. Le lac : trois bassins qui se rejoignent, bords un peu irréguliers ; presqu'île à l'est, îlot.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    float wobble = (Hash(x, y, 91) - 0.5f) * 0.18f;
                    bool lake = Inside(x, y, 44f, 25f, 17f, 11f, wobble) || Inside(x, y, 31f, 31f, 9f, 6.5f, wobble) || Inside(x, y, 55f, 18f, 9f, 6f, wobble);
                    bool peninsula = Capsule(x, y, new Vector2(52f, 27f), new Vector2(66f, 27f), 2.6f);
                    if (lake && !peninsula) Terrain[x, y] = TerrainType.Water;
                }
            }

            // 3. Le torrent, de la cascade jusqu'au lac.
            Waterfall = new Vector2Int(20, Ridge[20] - 2);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (y < Ridge[x] - 1 && DistanceToPolyline(new Vector2(x + 0.5f, y + 0.5f), RiverCourse) <= 1.3f) Terrain[x, y] = TerrainType.Water;
                }
            }

            // 4. Îlot et son sapin.
            IsletTree = new Vector2Int(39, 24);
            for (int x = 35; x <= 43; x++)
            {
                for (int y = 20; y <= 28; y++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(39.5f, 24.5f));
                    if (d <= 2.3f) Terrain[x, y] = d <= 1.3f ? TerrainType.Grass : TerrainType.Sand;
                }
            }

            // 5. Rive de galets : une ou deux cases autour de l'eau (jamais sur le plateau ni la falaise).
            var shore = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] != TerrainType.Grass || Plateau[x, y] || Cliff[x, y] || (x == IsletTree.x && y == IsletTree.y)) continue;
                    int reach = Hash(x, y, 13) < 0.35f ? 2 : 1;
                    shore[x, y] = NearWater(x, y, reach);
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (shore[x, y]) Terrain[x, y] = TerrainType.Sand;
                }
            }

            // 6. Chemin de terre qui fait le tour du lac ; la passerelle là où il croise le torrent.
            int bridgeMinX = int.MaxValue, bridgeMaxX = int.MinValue, bridgeMinY = int.MaxValue, bridgeMaxY = int.MinValue;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Plateau[x, y] || Cliff[x, y]) continue;
                    var cell = new Vector2(x + 0.5f, y + 0.5f);
                    bool onPath = DistanceToPolyline(cell, PathCourse) <= 1.05f;
                    foreach (Vector2[] spur in Spurs) onPath |= DistanceToPolyline(cell, spur) <= 0.75f;
                    if (!onPath) continue;
                    if (Terrain[x, y] == TerrainType.Water)
                    {
                        // Sur le torrent seulement (le chemin ne traverse jamais le lac).
                        if (DistanceToPolyline(new Vector2(x + 0.5f, y + 0.5f), RiverCourse) > 1.5f) continue;
                        bridgeMinX = Mathf.Min(bridgeMinX, x);
                        bridgeMaxX = Mathf.Max(bridgeMaxX, x);
                        bridgeMinY = Mathf.Min(bridgeMinY, y);
                        bridgeMaxY = Mathf.Max(bridgeMaxY, y);
                        continue;
                    }

                    Path[x, y] = true;
                }
            }

            Bridge = bridgeMinX <= bridgeMaxX ? new RectInt(bridgeMinX, bridgeMinY, bridgeMaxX - bridgeMinX + 1, bridgeMaxY - bridgeMinY + 1) : new RectInt();

            // 7. Ponton : sur la rive sud, au bout du chemin, vers le nord.
            int dockX = 44;
            int dockY = 8;
            while (dockY < Height - 1 && Terrain[dockX, dockY + 1] != TerrainType.Water) dockY++;
            DockFoot = new Vector2Int(dockX, dockY);

            // 8. Chalet au bout de la presqu'île.
            Chalet = new Vector2Int(62, 26);

            // 9. Prairie fleurie au sud-ouest, et un peu autour du chalet.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] != TerrainType.Grass || Plateau[x, y] || Path[x, y]) continue;
                    float meadow = Ellipse(x, y, 14f, 9f, 10f, 6.5f);
                    float garden = Ellipse(x, y, 59f, 30f, 4f, 2.5f);
                    Flowers[x, y] = (meadow <= 1f && Hash(x, y, 33) < 0.75f) || (garden <= 1f && Hash(x, y, 34) < 0.5f);
                }
            }

            // 10. Forêts : denses à l'est et au nord-ouest, sur le plateau ; clairsemées ailleurs ; jamais sur le chemin.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] != TerrainType.Grass || Path[x, y] || Flowers[x, y] || Cliff[x, y]) continue;
                    bool nearPath = NearMask(Path, x, y, 1);
                    bool nearDock = Mathf.Abs(x - DockFoot.x) <= 4 && y <= DockFoot.y + 2;
                    bool nearChalet = Mathf.Abs(x - Chalet.x) <= 4 && Mathf.Abs(y - Chalet.y) <= 4;
                    if (nearPath || nearDock || nearChalet) continue;
                    float density = 0.04f;
                    if (Plateau[x, y]) density = 0.55f;
                    else if (x >= 69) density = 0.5f;
                    else if (x <= 11 && y >= 18) density = 0.45f;
                    else if (y <= 4) density = 0.25f;
                    // Une clairière dans la forêt de l'est.
                    if (Ellipse(x, y, 74f, 44f, 3.5f, 3f) <= 1f) density = 0f;
                    Forest[x, y] = density;
                }
            }

            // 11. Rochers dans l'eau, au pied des montagnes et à l'embouchure du torrent.
            foreach (Vector2Int rock in new[] { new Vector2Int(36, 34), new Vector2Int(47, 35), new Vector2Int(28, 30), new Vector2Int(60, 21), new Vector2Int(52, 15) })
            {
                if (Terrain[rock.x, rock.y] == TerrainType.Water) WaterRocks.Add(rock);
            }
        }

        private static bool Inside(int x, int y, float cx, float cy, float rx, float ry, float wobble)
        {
            return Ellipse(x, y, cx, cy, rx, ry) <= 1f + wobble;
        }

        private static float Ellipse(int x, int y, float cx, float cy, float rx, float ry)
        {
            float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
            return u * u + v * v;
        }

        private static bool Capsule(int x, int y, Vector2 a, Vector2 b, float r)
        {
            return DistanceToSegment(new Vector2(x + 0.5f, y + 0.5f), a, b) <= r;
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

        private static bool NearMask(bool[,] mask, int x, int y, int reach)
        {
            for (int dx = -reach; dx <= reach; dx++)
            {
                for (int dy = -reach; dy <= reach; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < Width && ny < Height && mask[nx, ny]) return true;
                }
            }

            return false;
        }

        public static float DistanceToPolyline(Vector2 p, IReadOnlyList<Vector2> points)
        {
            float best = float.MaxValue;
            for (int i = 1; i < points.Count; i++) best = Mathf.Min(best, DistanceToSegment(p, points[i - 1], points[i]));
            return best;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
