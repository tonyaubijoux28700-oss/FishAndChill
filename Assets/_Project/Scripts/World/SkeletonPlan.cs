using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Plan dessiné de la Côte des Squelettes (Namibie, 90×64 cases, y vers le haut) : l'océan Atlantique froid au sud et à
    /// l'ouest, une longue plage pâle, puis la plaine de graviers du désert et de grandes dunes au nord ; un cap rocheux où
    /// vivent les otaries ; une lagune salée derrière la plage ; le lit d'une rivière à sec bordé d'arbres morts ; un gros
    /// cargo échoué sur le sable, un chalutier couché dans les vagues, le squelette d'une baleine, la cabane en ruine d'un
    /// prospecteur et ses fûts rouillés ; une piste de 4x4 qui relie tout ça au ponton. Logique pure (testée) ; dessiné par
    /// <c>MapSetup.BuildSkeleton</c>.
    /// </summary>
    public sealed class SkeletonPlan
    {
        public const int Width = 90;
        public const int Height = 64;

        public TerrainType[,] Terrain = new TerrainType[Width, Height];

        /// <summary>Piste de 4x4 (graviers tassés, deux ornières) qui relie le ponton aux repères.</summary>
        public bool[,] Track = new bool[Width, Height];

        /// <summary>Lit de la rivière à sec : sable gris craquelé.</summary>
        public bool[,] Riverbed = new bool[Width, Height];

        public enum Plant { DeadTree, Welwitschia, Tuft }

        /// <summary>Plantes posées par le plan (case, sorte, variante) ; les arbres morts sont solides.</summary>
        public readonly List<(Vector2Int Cell, Plant Kind, int Variant)> Plants = new List<(Vector2Int, Plant, int)>();

        /// <summary>Os blanchis et débris échoués sur le sable (on passe à travers).</summary>
        public readonly List<Vector2Int> Bones = new List<Vector2Int>();

        /// <summary>Rochers noirs du cap et de la côte (solides sur la terre).</summary>
        public readonly List<Vector2Int> Rocks = new List<Vector2Int>();

        /// <summary>Fûts rouillés, ancre et chaîne (solides).</summary>
        public readonly List<Vector2Int> Debris = new List<Vector2Int>();

        /// <summary>Pied des grandes dunes, au nord (solides).</summary>
        public readonly List<Vector2Int> Dunes = new List<Vector2Int>();

        public Vector2Int Wreck;      // pied du cargo échoué (milieu de la coque)
        public Vector2Int Trawler;    // chalutier couché dans les vagues (case d'eau)
        public Vector2Int Whale;      // milieu du squelette de baleine
        public Vector2Int Shack;      // pied de la cabane du prospecteur
        public Vector2Int Cape;       // pointe du cap aux otaries

        /// <summary>Colonne de gauche du ponton des voyages et case de la plage où il part, vers le sud.</summary>
        public Vector2Int DockFoot;

        public const int DockX = 47;

        private static readonly Vector2 Lagoon = new Vector2(20f, 39f);
        private static readonly Vector2 LagoonRadii = new Vector2(7.5f, 3.6f);

        private static readonly Vector2[] RiverCourse =
        {
            new Vector2(27f, 40f), new Vector2(33f, 45f), new Vector2(40f, 47f), new Vector2(48f, 52f), new Vector2(55f, 58f), new Vector2(60f, 66f),
        };

        private static readonly Vector2[] TrackCourse =
        {
            new Vector2(10f, 34f), new Vector2(18f, 33f), new Vector2(30f, 32f), new Vector2(40f, 30f), new Vector2(48f, 30f), new Vector2(58f, 33f),
            new Vector2(68f, 36f), new Vector2(78f, 37f), new Vector2(88f, 36f),
        };

        public static SkeletonPlan Build()
        {
            var plan = new SkeletonPlan();
            plan.Paint();
            return plan;
        }

        /// <summary>Hauteur du rivage à la colonne <paramref name="x"/> : en dessous, l'océan. Le cap avance vers le sud.</summary>
        public static float ShoreAt(float x)
        {
            float cape = 9f * Mathf.Exp(-Mathf.Pow((x - 62f) / 5.5f, 2f));
            return 27f - x * 0.16f + Mathf.Sin(x * 0.13f) * 2f + Mathf.Sin(x * 0.37f + 1f) * 0.7f - cape;
        }

        private static float Hash(int x, int y, int seed) => IslandGenerator.CellHash(x, y, seed);

        private void Paint()
        {
            // 1. Océan au sud (et à l'ouest, où la côte remonte), terre au-dessus ; la lagune salée derrière la plage.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    float west = x < 5 ? (5 - x) * 3f : 0f;   // la côte se recourbe vers le nord, à l'ouest
                    bool sea = y + 0.5f < ShoreAt(x + 0.5f) + west;
                    float u = (x + 0.5f - Lagoon.x) / LagoonRadii.x, v = (y + 0.5f - Lagoon.y) / LagoonRadii.y;
                    bool lagoon = u * u + v * v < 1f + Mathf.Sin(Mathf.Atan2(v, u) * 4f) * 0.08f;
                    Terrain[x, y] = sea || lagoon ? TerrainType.Water : TerrainType.Grass;
                }
            }

            // 2. Plage pâle : large le long de l'océan, étroite autour de la lagune.
            var sand = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] != TerrainType.Grass) continue;
                    bool nearSea = NearSea(x, y, Hash(x, y, 3) < 0.5f ? 6 : 5);
                    sand[x, y] = nearSea || NearWater(x, y, 2);
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (sand[x, y]) Terrain[x, y] = TerrainType.Sand;
                }
            }

            // 3. Lit de la rivière à sec, qui descend des dunes vers la lagune ; piste de 4x4.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] == TerrainType.Water) continue;
                    var cell = new Vector2(x + 0.5f, y + 0.5f);
                    Riverbed[x, y] = DistanceToPolyline(cell, RiverCourse) <= 1.6f + Mathf.Sin(y * 0.5f) * 0.4f;
                    bool spur = x >= DockX && x <= DockX + 1 && y <= 30 && y >= ShoreAt(x + 0.5f);
                    Track[x, y] = DistanceToPolyline(cell, TrackCourse) <= 0.8f || spur;
                }
            }

            // 4. Ponton au bout de la piste, vers le sud.
            int dockY = 30;
            while (dockY > 1 && Terrain[DockX, dockY - 1] != TerrainType.Water) dockY--;
            DockFoot = new Vector2Int(DockX, dockY);

            // 5. Repères.
            Wreck = new Vector2Int(33, Mathf.CeilToInt(ShoreAt(33.5f)) + 1);
            Trawler = new Vector2Int(76, Mathf.FloorToInt(ShoreAt(76.5f)) - 3);
            Whale = new Vector2Int(12, 29);
            Shack = new Vector2Int(70, 39);
            Cape = new Vector2Int(62, Mathf.CeilToInt(ShoreAt(62.5f)));
            foreach (Vector2Int d in new[] { new Vector2Int(74, 40), new Vector2Int(75, 39), new Vector2Int(66, 40) }) Debris.Add(d);
            Debris.Add(new Vector2Int(41, Mathf.CeilToInt(ShoreAt(41.5f)) + 1));   // ancre et chaîne du cargo
            foreach (Vector2Int d in new[] { new Vector2Int(8, 56), new Vector2Int(24, 58), new Vector2Int(40, 60), new Vector2Int(72, 56), new Vector2Int(85, 52) }) Dunes.Add(d);

            // 6. Rochers noirs : la pointe du cap (au bord de l'eau et un peu dedans) et quelques blocs le long de la côte.
            for (int x = 1; x < Width - 1; x++)
            {
                for (int y = 1; y < Height - 1; y++)
                {
                    bool cape = Mathf.Abs(x - Cape.x) <= 5;
                    if (Track[x, y]) continue;
                    if (Terrain[x, y] == TerrainType.Sand && Terrain[x, y - 1] == TerrainType.Water && Hash(x, y, 21) < (cape ? 0.7f : 0.08f)) Rocks.Add(new Vector2Int(x, y));
                    else if (cape && Terrain[x, y] == TerrainType.Water && Terrain[x, y + 1] != TerrainType.Water && Hash(x, y, 22) < 0.5f) Rocks.Add(new Vector2Int(x, y));
                }
            }

            // 7. Végétation de désert : arbres morts le long du lit à sec, welwitschias et touffes sur les graviers ;
            //    os blanchis et débris sur la plage. Rien sur la piste ni autour des repères.
            bool[,] reserved = Reserved();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (Terrain[x, y] == TerrainType.Water || reserved[x, y] || Track[x, y]) continue;
                    if (y + 2 < Height && (Track[x, y + 1] || Track[x, y + 2])) continue;
                    if (Terrain[x, y] == TerrainType.Sand)
                    {
                        if (Hash(x, y, 31) < 0.05f) Bones.Add(cell);
                        continue;
                    }

                    float river = DistanceToPolyline(new Vector2(x + 0.5f, y + 0.5f), RiverCourse);
                    if (river > 1.8f && river < 3.6f && Hash(x, y, 32) < 0.35f && !Plants.Exists(p => (p.Cell - cell).sqrMagnitude < 10))
                    {
                        Plants.Add((cell, Plant.DeadTree, (int)(Hash(x, y, 33) * 2f)));
                    }
                    else if (!Riverbed[x, y] && Hash(x, y, 34) < 0.025f && !Plants.Exists(p => (p.Cell - cell).sqrMagnitude < 9))
                    {
                        Plants.Add((cell, Plant.Welwitschia, 0));
                    }
                    else if (!Riverbed[x, y] && Hash(x, y, 35) < 0.06f && !Plants.Exists(p => p.Cell == cell))
                    {
                        Plants.Add((cell, Plant.Tuft, (x + y) % 2));
                    }
                }
            }
        }

        /// <summary>Cases réservées aux repères : rien n'y pousse.</summary>
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

            Mark(Wreck.x - 6, Wreck.y - 1, Wreck.x + 6, Wreck.y + 5);
            Mark(Whale.x - 4, Whale.y - 1, Whale.x + 4, Whale.y + 2);
            Mark(Shack.x - 4, Shack.y - 1, Shack.x + 4, Shack.y + 4);
            Mark(DockFoot.x - 1, DockFoot.y - 7, DockFoot.x + 4, DockFoot.y + 2);
            foreach (Vector2Int d in Dunes) Mark(d.x - 5, d.y - 1, d.x + 5, d.y + 4);
            foreach (Vector2Int d in Debris) Mark(d.x - 1, d.y - 1, d.x + 1, d.y + 1);
            foreach (Vector2Int r in Rocks) Mark(r.x, r.y, r.x, r.y);
            return taken;
        }

        private bool NearSea(int x, int y, int reach)
        {
            for (int dx = -reach; dx <= reach; dx++)
            {
                for (int dy = -reach; dy <= reach; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                    if (Terrain[nx, ny] != TerrainType.Water) continue;
                    float u = (nx + 0.5f - Lagoon.x) / LagoonRadii.x, v = (ny + 0.5f - Lagoon.y) / LagoonRadii.y;
                    if (u * u + v * v > 1.5f) return true;   // l'océan, pas la lagune
                }
            }

            return false;
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
