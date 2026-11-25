using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Plan dessiné des fjords de Norvège (90×64 cases, y vers le haut) : un long fjord d'eau sombre qui entre par le sud
    /// et se resserre vers le nord jusqu'à une grande cascade tombée des falaises enneigées ; sur la rive est, le village
    /// au bord d'une petite anse (rorbuer rouges sur pilotis, église de bois blanc, séchoir à morue, ponton des voyages)
    /// relié par un chemin de gravier ; sur la rive ouest, la forêt de sapins et de bouleaux et un grenier au toit d'herbe ;
    /// une passerelle de bois au fond du fjord relie les deux rives. Rives de galets, toundra, plaques de neige au pied des
    /// falaises. Logique pure (testée) ; dessiné par <c>MapSetup.BuildFjord</c>.
    /// </summary>
    public sealed class FjordPlan
    {
        public const int Width = 90;
        public const int Height = 64;

        /// <summary>Fond du fjord : au-delà, la terre jusqu'aux falaises.</summary>
        public const float Head = 50f;

        /// <summary>Pied des falaises du nord (on ne va pas plus haut).</summary>
        public const int CliffFoot = 54;

        /// <summary>Rangée de la passerelle qui traverse le fjord (deux cases de large).</summary>
        public const int BridgeRow = 43;

        public TerrainType[,] Terrain = new TerrainType[Width, Height];

        /// <summary>Chemin de gravier du village (ponton, église, séchoir, passerelle).</summary>
        public bool[,] Path = new bool[Width, Height];

        public enum Plant { Pine, Birch, Boulder, Heather, Snow }

        /// <summary>Plantes et rochers (case, sorte, variante) ; sapins, bouleaux et blocs sont solides.</summary>
        public readonly List<(Vector2Int Cell, Plant Kind, int Variant)> Plants = new List<(Vector2Int, Plant, int)>();

        /// <summary>Rorbuer : pied (case d'eau au ras de la rive), ils se dressent sur pilotis.</summary>
        public readonly List<Vector2Int> Rorbuer = new List<Vector2Int>();

        /// <summary>Pied de chaque pan de falaise (8 cases de large), le long du nord.</summary>
        public readonly List<Vector2Int> Cliffs = new List<Vector2Int>();

        /// <summary>Barques (cases d'eau).</summary>
        public readonly List<Vector2Int> Boats = new List<Vector2Int>();

        public Vector2Int Church;      // pied de l'église
        public Vector2Int Rack;        // pied du séchoir à morue
        public Vector2Int Storehouse;  // pied du grenier
        public Vector2Int Waterfall;   // pied de la cascade (dans l'eau, au fond du fjord)

        /// <summary>Passerelle : de la première à la dernière colonne (rangées <see cref="BridgeRow"/> et +1).</summary>
        public int BridgeWest, BridgeEast;

        /// <summary>Colonne de gauche du ponton des voyages et case de la rive où il part, vers le sud, dans l'anse.</summary>
        public Vector2Int DockFoot;

        public static readonly Vector2 Bay = new Vector2(60f, 22f);
        private static readonly Vector2 BayRadii = new Vector2(8f, 4.5f);

        public static FjordPlan Build()
        {
            var plan = new FjordPlan();
            plan.Paint();
            return plan;
        }

        /// <summary>Milieu du fjord à la rangée <paramref name="y"/>.</summary>
        public static float CenterAt(float y) => 38f + Mathf.Sin(y * 0.07f) * 3f;

        /// <summary>Demi-largeur du fjord à la rangée <paramref name="y"/> : large à l'entrée, étroit au fond.</summary>
        public static float HalfWidthAt(float y) => Mathf.Lerp(12f, 5f, Mathf.Clamp01(y / Head));

        private static float Hash(int x, int y, int seed) => IslandGenerator.CellHash(x, y, seed);

        public static bool IsFjordWater(float x, float y)
        {
            float half = HalfWidthAt(y), dx = (x - CenterAt(y)) / half;
            if (Mathf.Abs(dx) >= 1f) return false;
            float head = Head + half * 0.7f * Mathf.Sqrt(1f - dx * dx);   // fond arrondi
            return y < head;
        }

        private static bool IsBayWater(float x, float y)
        {
            float u = (x - Bay.x) / BayRadii.x, v = (y - Bay.y) / BayRadii.y;
            return u * u + v * v < 1f + Mathf.Sin(Mathf.Atan2(v, u) * 3f) * 0.06f;
        }

        private void Paint()
        {
            // 1. L'eau : le fjord et l'anse du village ; la terre partout ailleurs.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    float cx = x + 0.5f, cy = y + 0.5f;
                    Terrain[x, y] = IsFjordWater(cx, cy) || IsBayWater(cx, cy) ? TerrainType.Water : TerrainType.Grass;
                }
            }

            // 2. Rives de galets (deux cases), la toundra derrière.
            var shore = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    shore[x, y] = Terrain[x, y] == TerrainType.Grass && NearWater(x, y, Hash(x, y, 3) < 0.5f ? 2 : 1);
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (shore[x, y]) Terrain[x, y] = TerrainType.Sand;
                }
            }

            // 3. Ponton dans l'anse, vers le sud.
            int dockX = Mathf.RoundToInt(Bay.x) - 4;
            int dockY = Mathf.RoundToInt(Bay.y) + 8;
            while (dockY > 1 && Terrain[dockX, dockY - 1] != TerrainType.Water) dockY--;
            DockFoot = new Vector2Int(dockX, dockY);

            // 4. Passerelle au fond du fjord, d'une rive à l'autre.
            BridgeWest = Width;
            BridgeEast = 0;
            for (int x = 0; x < Width; x++)
            {
                if (Terrain[x, BridgeRow] == TerrainType.Water || Terrain[x, BridgeRow + 1] == TerrainType.Water)
                {
                    BridgeWest = Mathf.Min(BridgeWest, x);
                    BridgeEast = Mathf.Max(BridgeEast, x);
                }
            }

            BridgeWest -= 1;
            BridgeEast += 1;

            // 5. Repères.
            Church = new Vector2Int(74, 40);
            Rack = new Vector2Int(66, 31);
            Storehouse = new Vector2Int(16, 36);
            Waterfall = new Vector2Int(Mathf.FloorToInt(CenterAt(Head)), Mathf.FloorToInt(Head) - 1);
            for (int x = 4; x <= Width + 2; x += 8)
            {
                Cliffs.Add(new Vector2Int(x, CliffFoot));   // la cascade se dessine devant
            }

            // Deux rorbuer au bord du fjord, au nord de l'anse, et un dans l'anse ; pied sur l'eau au ras de la rive.
            foreach (int y in new[] { 31, 36 })
            {
                int x = Mathf.FloorToInt(CenterAt(y + 0.5f) + HalfWidthAt(y + 0.5f)) - 1;
                while (x > 0 && Terrain[x, y] != TerrainType.Water) x--;
                Rorbuer.Add(new Vector2Int(x - 1, y));
            }

            Rorbuer.Add(new Vector2Int(Mathf.RoundToInt(Bay.x) + 4, Mathf.RoundToInt(Bay.y) + 2));
            Boats.Add(new Vector2Int(Mathf.FloorToInt(CenterAt(14f)) - 4, 14));
            Boats.Add(new Vector2Int(Mathf.RoundToInt(Bay.x) + 2, Mathf.RoundToInt(Bay.y) - 2));

            // 6. Chemin de gravier : du ponton à l'église, avec une branche vers le séchoir et une vers la passerelle.
            var main = new[] { new Vector2(DockFoot.x + 1f, DockFoot.y + 0.5f), new Vector2(DockFoot.x + 2f, 30f), new Vector2(70f, 34f), new Vector2(Church.x + 0.5f, Church.y - 1f) };
            var toBridge = new[] { new Vector2(68f, 33f), new Vector2(BridgeEast + 1.5f, BridgeRow + 1f) };
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] == TerrainType.Water) continue;
                    var cell = new Vector2(x + 0.5f, y + 0.5f);
                    Path[x, y] = DistanceToPolyline(cell, main) <= 0.9f || DistanceToPolyline(cell, toBridge) <= 0.8f;
                }
            }

            // 7. La forêt de l'ouest, des bouleaux à l'est, blocs de granit, bruyère, neige au pied des falaises.
            bool[,] reserved = Reserved();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (Terrain[x, y] != TerrainType.Grass || reserved[x, y] || Path[x, y] || y >= CliffFoot - 1) continue;
                    if (y + 2 < Height && (Path[x, y + 1] || Path[x, y + 2])) continue;
                    bool west = x < CenterAt(y + 0.5f);
                    if (y >= CliffFoot - 6 && Hash(x, y, 40) < 0.1f && !Plants.Exists(p => (p.Cell - cell).sqrMagnitude < 12))
                    {
                        Plants.Add((cell, Plant.Snow, (x + y) % 2));
                    }
                    else if (west && Hash(x, y, 41) < 0.3f && !Plants.Exists(p => p.Kind != Plant.Heather && (p.Cell - cell).sqrMagnitude < 5))
                    {
                        Plants.Add((cell, Hash(x, y, 42) < 0.75f ? Plant.Pine : Plant.Birch, (int)(Hash(x, y, 43) * 3f)));
                    }
                    else if (!west && Hash(x, y, 44) < 0.05f && !Plants.Exists(p => (p.Cell - cell).sqrMagnitude < 9))
                    {
                        Plants.Add((cell, Plant.Birch, (int)(Hash(x, y, 45) * 2f)));
                    }
                    else if (Hash(x, y, 46) < 0.02f && !Plants.Exists(p => (p.Cell - cell).sqrMagnitude < 9))
                    {
                        Plants.Add((cell, Plant.Boulder, (x + y) % 2));
                    }
                    else if (Hash(x, y, 47) < 0.07f && !Plants.Exists(p => p.Cell == cell))
                    {
                        Plants.Add((cell, Plant.Heather, (x * 3 + y) % 2));
                    }
                }
            }
        }

        /// <summary>Cases réservées aux repères et aux passages : rien n'y pousse.</summary>
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

            Mark(Church.x - 3, Church.y - 2, Church.x + 3, Church.y + 6);
            Mark(Rack.x - 4, Rack.y - 1, Rack.x + 4, Rack.y + 3);
            Mark(Storehouse.x - 3, Storehouse.y - 2, Storehouse.x + 3, Storehouse.y + 4);
            Mark(DockFoot.x - 1, DockFoot.y - 7, DockFoot.x + 3, DockFoot.y + 2);
            Mark(BridgeWest - 2, BridgeRow - 1, BridgeEast + 2, BridgeRow + 2);
            foreach (Vector2Int r in Rorbuer) Mark(r.x - 2, r.y - 1, r.x + 3, r.y + 4);
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
