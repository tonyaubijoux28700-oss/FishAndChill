using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Plan dessiné du fleuve Amazone (90×64 cases, y vers le haut), à la place d'une forme tirée au hasard : un large
    /// fleuve qui serpente d'ouest en est, un banc de sable pâle à l'ouest qui le traverse (la saison sèche), un igarapé
    /// (petit affluent) qui descend du nord et qu'une passerelle franchit, un bras mort au sud-est couvert de nénuphars
    /// géants, une forêt inondée à l'est (arbres debout dans l'eau), un village riverain sur pilotis au sud autour du
    /// ponton, une pirogue amarrée, un fromager géant dans sa clairière au nord, un chemin de terre rouge qui relie tout
    /// ça, la jungle dense partout ailleurs (mur d'arbres au bord de la carte). Logique pure (testée) ; dessiné par
    /// <c>MapSetup.BuildAmazon</c>.
    /// </summary>
    public sealed class AmazonPlan
    {
        public const int Width = 90;
        public const int Height = 64;

        public TerrainType[,] Terrain = new TerrainType[Width, Height];

        /// <summary>Banc de sable pâle au milieu du fleuve (praticable, dessiné par-dessus la vase).</summary>
        public bool[,] Beach = new bool[Width, Height];

        public bool[,] Path = new bool[Width, Height];

        /// <summary>Densité des grands arbres (0 à 1) : jungle dense, clairières, mur d'arbres au bord.</summary>
        public float[,] Forest = new float[Width, Height];

        /// <summary>Sous-bois fleuri (héliconias, grandes feuilles) : là où la jungle s'ouvre un peu.</summary>
        public bool[,] Undergrowth = new bool[Width, Height];

        /// <summary>Eau du bras mort (nénuphars géants).</summary>
        public bool[,] Oxbow = new bool[Width, Height];

        public Vector2Int DockFoot;
        public RectInt Bridge;                          // cases de la passerelle sur l'igarapé
        public Vector2Int Kapok;                        // pied du fromager géant
        public Vector2Int Canoe;                        // case d'eau de la pirogue
        public readonly List<Vector2Int> Houses = new List<Vector2Int>();         // pied de chaque maison sur pilotis
        public readonly List<Vector2Int> FloodedTrees = new List<Vector2Int>();   // arbres debout dans l'eau
        public readonly List<Vector2Int> Logs = new List<Vector2Int>();           // troncs couchés (solides)
        public readonly List<Vector2Int> Bananas = new List<Vector2Int>();        // bananiers du village

        /// <summary>Ce qui pousse sur une case : grands arbres, palmiers, sous-bois, fougères, nénuphars géants.</summary>
        public enum Plant { JungleTree, Acai, Heliconia, Leaves, Fern, Lily }

        /// <summary>Plantes posées par le plan (case, sorte, variante) ; arbres et palmiers sont solides.</summary>
        public readonly List<(Vector2Int Cell, Plant Kind, int Variant)> Plants = new List<(Vector2Int, Plant, int)>();

        private static readonly Vector2[] RiverCourse =
        {
            new Vector2(-3f, 40f), new Vector2(10f, 41f), new Vector2(22f, 38f), new Vector2(33f, 32.5f), new Vector2(46f, 30.5f),
            new Vector2(58f, 32f), new Vector2(70f, 36f), new Vector2(80f, 35f), new Vector2(93f, 32f),
        };

        private static readonly Vector2[] CreekCourse =
        {
            new Vector2(64f, 66f), new Vector2(62f, 59f), new Vector2(66f, 52f), new Vector2(63.5f, 45f), new Vector2(64.5f, 37f),
        };

        private static readonly Vector2[] PathCourse =
        {
            new Vector2(41.5f, 18f), new Vector2(29f, 17f), new Vector2(19f, 22f), new Vector2(15.5f, 30f), new Vector2(15.5f, 47.5f),
            new Vector2(22f, 50.5f), new Vector2(34f, 50.5f), new Vector2(46f, 50.5f), new Vector2(58f, 51f), new Vector2(70f, 51f),
            new Vector2(78f, 49f), new Vector2(86f, 49f),
        };

        /// <summary>Petits chemins : du village au ponton, et vers le bras mort.</summary>
        private static readonly Vector2[][] Spurs =
        {
            new[] { new Vector2(42f, 18f), new Vector2(42f, 26f) },
            new[] { new Vector2(41.5f, 18f), new Vector2(54f, 17f), new Vector2(62f, 15f) },
        };

        public static AmazonPlan Build()
        {
            var plan = new AmazonPlan();
            plan.Paint();
            return plan;
        }

        /// <summary>Demi-largeur du fleuve à la colonne <paramref name="x"/>.</summary>
        public static float RiverHalfWidth(float x) => 5.6f + 1.1f * Mathf.Sin(x * 0.13f + 0.8f);

        private static float Hash(int x, int y, int seed) => IslandGenerator.CellHash(x, y, seed);

        private void Paint()
        {
            // 1. Jungle partout, puis le fleuve, l'igarapé et le bras mort.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = new Vector2(x + 0.5f, y + 0.5f);
                    float wobble = (Hash(x, y, 41) - 0.5f) * 0.7f;
                    bool river = DistanceToPolyline(cell, RiverCourse) <= RiverHalfWidth(x) + wobble;
                    bool creek = DistanceToPolyline(cell, CreekCourse) <= 1.45f;
                    bool oxbow = Ellipse(x, y, 73f, 14f, 9f, 4.4f) <= 1f + wobble * 0.25f;
                    Oxbow[x, y] = oxbow;
                    Terrain[x, y] = river || creek || oxbow ? TerrainType.Water : TerrainType.Grass;
                }
            }

            // 2. Banc de sable qui traverse le fleuve à l'ouest, en fuseau.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    float d = DistanceToSegment(new Vector2(x + 0.5f, y + 0.5f), new Vector2(15.5f, 31f), new Vector2(15.5f, 49f));
                    float t = Mathf.InverseLerp(31f, 49f, y + 0.5f);
                    float radius = 1.6f + 1.6f * Mathf.Sin(t * Mathf.PI);   // plus large au milieu du fleuve
                    if (d <= radius && Terrain[x, y] == TerrainType.Water)
                    {
                        Terrain[x, y] = TerrainType.Sand;
                        Beach[x, y] = true;
                    }
                }
            }

            // 3. Rives de vase : une ou deux cases autour de l'eau.
            var shore = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] != TerrainType.Grass) continue;
                    shore[x, y] = NearWater(x, y, Hash(x, y, 13) < 0.4f ? 2 : 1);
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (shore[x, y]) Terrain[x, y] = TerrainType.Sand;
                }
            }

            // 4. Chemin de terre rouge ; la passerelle là où il croise l'igarapé. Il s'efface sur le banc de sable.
            int bridgeMinX = int.MaxValue, bridgeMaxX = int.MinValue, bridgeMinY = int.MaxValue, bridgeMaxY = int.MinValue;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = new Vector2(x + 0.5f, y + 0.5f);
                    bool onPath = DistanceToPolyline(cell, PathCourse) <= 1.05f;
                    foreach (Vector2[] spur in Spurs) onPath |= DistanceToPolyline(cell, spur) <= 0.75f;
                    if (!onPath || Beach[x, y]) continue;
                    if (Terrain[x, y] == TerrainType.Water)
                    {
                        if (DistanceToPolyline(cell, CreekCourse) > 1.9f) continue;   // le chemin ne franchit que l'igarapé
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

            // 5. Ponton au bout du petit chemin du village, vers le nord (le fleuve).
            int dockX = 41;
            int dockY = 18;
            while (dockY < Height - 1 && Terrain[dockX, dockY + 1] != TerrainType.Water) dockY++;
            DockFoot = new Vector2Int(dockX, dockY);

            // 6. Village : deux maisons sur pilotis de part et d'autre du ponton, dos au fleuve ; bananiers autour.
            Houses.Add(new Vector2Int(34, 22));
            Houses.Add(new Vector2Int(49, 20));
            foreach (Vector2Int banana in new[] { new Vector2Int(30, 21), new Vector2Int(38, 22), new Vector2Int(45, 21), new Vector2Int(53, 20), new Vector2Int(36, 14), new Vector2Int(47, 14) })
            {
                if (Terrain[banana.x, banana.y] == TerrainType.Grass && !Path[banana.x, banana.y]) Bananas.Add(banana);
            }

            // 7. Pirogue amarrée à l'est du ponton, sur la première case d'eau.
            int canoeY = 18;
            while (canoeY < Height - 1 && Terrain[54, canoeY] != TerrainType.Water) canoeY++;
            Canoe = new Vector2Int(54, canoeY + 1);

            // 8. Fromager géant au nord, dans sa clairière, au bord du chemin.
            Kapok = new Vector2Int(40, 53);

            // 9. Forêt inondée : arbres debout dans l'eau peu profonde, à l'est du fleuve et au bord du bras mort.
            for (int x = 2; x < Width - 2; x++)
            {
                for (int y = 2; y < Height - 2; y++)
                {
                    if (Terrain[x, y] != TerrainType.Water || !NearLand(x, y, 2) || Mathf.Abs(x - 64) <= 3) continue;
                    bool igapo = x >= 74 || Oxbow[x, y];
                    if (!igapo || Hash(x, y, 57) > 0.22f) continue;
                    if (FloodedTrees.Exists(t => Mathf.Abs(t.x - x) < 3 && Mathf.Abs(t.y - y) < 3)) continue;
                    FloodedTrees.Add(new Vector2Int(x, y));
                }
            }

            // 10. Densité de la jungle et sous-bois.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] != TerrainType.Grass || Path[x, y]) continue;
                    bool border = x <= 3 || x >= Width - 4 || y <= 2 || y >= Height - 3;
                    bool village = Ellipse(x, y, 41.5f, 18f, 15f, 7f) <= 1f;
                    bool clearing = Ellipse(x, y, 40.5f, 54f, 8f, 5.5f) <= 1f;
                    bool nearPath = NearMask(Path, x, y, 1) || (y + 3 < Height && (Path[x, y + 2] || Path[x, y + 3]));
                    float density = 0.24f;
                    if (border) density = 0.7f;
                    if (village || clearing) density = 0f;
                    if (nearPath && !border) density = 0f;
                    Forest[x, y] = density;
                    Undergrowth[x, y] = !village && Hash(x, y, 63) < (clearing ? 0.18f : 0.10f);
                }
            }

            // 11. Troncs couchés dans le sous-bois, loin du chemin.
            foreach (Vector2Int log in new[] { new Vector2Int(8, 12), new Vector2Int(27, 47), new Vector2Int(55, 58), new Vector2Int(82, 21), new Vector2Int(22, 7), new Vector2Int(80, 58) })
            {
                if (Terrain[log.x, log.y] == TerrainType.Grass && Terrain[log.x + 1, log.y] == TerrainType.Grass && !NearMask(Path, log.x, log.y, 1)) Logs.Add(log);
            }

            PlacePlants();
        }

        /// <summary>Cases réservées aux repères (maisons, fromager, ponton, passerelle…) : rien n'y pousse.</summary>
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

            foreach (Vector2Int house in Houses) Mark(house.x - 3, house.y - 1, house.x + 2, house.y + 4);
            Mark(Kapok.x - 4, Kapok.y - 1, Kapok.x + 4, Kapok.y + 3);
            Mark(DockFoot.x - 3, DockFoot.y - 3, DockFoot.x + 4, DockFoot.y + 7);
            Mark(Canoe.x - 2, Canoe.y - 1, Canoe.x + 2, Canoe.y + 1);
            Mark(Bridge.xMin - 1, Bridge.yMin - 1, Bridge.xMax, Bridge.yMax);
            foreach (Vector2Int banana in Bananas) Mark(banana.x - 1, banana.y, banana.x + 1, banana.y + 1);
            foreach (Vector2Int log in Logs) Mark(log.x - 1, log.y, log.x + 2, log.y);
            foreach (Vector2Int tree in FloodedTrees) Mark(tree.x - 1, tree.y - 1, tree.x + 1, tree.y + 1);
            return taken;
        }

        private void PlacePlants()
        {
            bool[,] taken = Reserved();
            var trees = new List<Vector2Int>();
            int[,] water = WaterDistance(4);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float hash = Hash(x, y, 7077);
                    if (taken[x, y]) continue;

                    // Nénuphars géants : nombreux sur le bras mort, quelques-uns au calme près des rives du fleuve.
                    if (Terrain[x, y] == TerrainType.Water)
                    {
                        bool oxbowLily = Oxbow[x, y] && water[x, y] >= 1 && hash < 0.22f;
                        bool riverLily = !Oxbow[x, y] && water[x, y] >= 2 && water[x, y] <= 3 && hash < 0.025f && Mathf.Abs(x - 64) > 3;
                        if (oxbowLily || riverLily) Plants.Add((new Vector2Int(x, y), Plant.Lily, Mathf.FloorToInt(Hash(x, y, 7078) * 3f) % 3));
                        continue;
                    }

                    if (Path[x, y] || Beach[x, y]) continue;

                    // Fougères sur la vase, au ras de l'eau.
                    if (Terrain[x, y] == TerrainType.Sand)
                    {
                        if (water[x, y] == 1 && hash > 0.9f) Plants.Add((new Vector2Int(x, y), Plant.Fern, hash > 0.95f ? 1 : 0));
                        continue;
                    }

                    // Grands arbres (açaí près de l'eau), en respectant un écart entre deux troncs.
                    float density = Forest[x, y];
                    if (density > 0f && Hash(x, y, 7071) < density && !trees.Exists(t => Mathf.Abs(t.x - x) < 3 && Mathf.Abs(t.y - y) < 2))
                    {
                        trees.Add(new Vector2Int(x, y));
                        float kind = Hash(x, y, 7091);
                        bool acai = water[x, y] <= 4 && kind < 0.45f;
                        Plants.Add(acai ? (new Vector2Int(x, y), Plant.Acai, kind < 0.22f ? 0 : 1) : (new Vector2Int(x, y), Plant.JungleTree, Mathf.FloorToInt(kind * 3f) % 3));
                        continue;
                    }

                    if (Undergrowth[x, y] && !trees.Exists(t => t.x == x && (t.y == y + 1 || t.y == y)))
                    {
                        float pick = Hash(x, y, 7093);
                        Plants.Add(pick < 0.45f ? (new Vector2Int(x, y), Plant.Heliconia, pick < 0.25f ? 0 : 1) : (new Vector2Int(x, y), Plant.Leaves, pick < 0.75f ? 0 : 1));
                    }
                }
            }
        }

        /// <summary>Distance (en cases, jusqu'à <paramref name="max"/>) de chaque case à la case d'eau ou de terre la plus proche d'en face.</summary>
        private int[,] WaterDistance(int max)
        {
            var result = new int[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    bool wet = Terrain[x, y] == TerrainType.Water;
                    int best = max + 1;
                    for (int dx = -max; dx <= max && best > 1; dx++)
                    {
                        for (int dy = -max; dy <= max; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                            if ((Terrain[nx, ny] == TerrainType.Water) == wet) continue;
                            best = Mathf.Min(best, Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)));
                        }
                    }

                    result[x, y] = best;
                }
            }

            return result;
        }

        private static float Ellipse(int x, int y, float cx, float cy, float rx, float ry)
        {
            float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
            return u * u + v * v;
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

        private bool NearLand(int x, int y, int reach)
        {
            for (int dx = -reach; dx <= reach; dx++)
            {
                for (int dy = -reach; dy <= reach; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < Width && ny < Height && Terrain[nx, ny] != TerrainType.Water) return true;
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
