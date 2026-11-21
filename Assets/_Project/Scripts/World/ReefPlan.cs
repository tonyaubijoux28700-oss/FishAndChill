using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Plan dessiné de la côte de la Grande Barrière (Queensland, 90×64 cases, y vers le haut) : la mer de Corail au sud,
    /// un récif frangeant au large (platier clair et coraux) avec un îlot de sable corallien ; une longue plage de sable
    /// blanc de silice bordée de pandanus et de cocotiers ; à l'ouest, un estuaire turquoise où le sable dessine des
    /// tourbillons, d'où remonte une crique de mangrove franchie par une passerelle de planches ; au milieu, la brousse
    /// d'eucalyptus et ses termitières ; au nord, la forêt tropicale ; à l'est, un cap de granit. Une maison
    /// « Queenslander » sur pilotis, la tour des sauveteurs, une barque en alu et une plateforme de plongée sur le récif ;
    /// une piste de terre rouge relie tout ça au ponton. Logique pure (testée) ; dessiné par <c>MapSetup.BuildReef</c>.
    /// </summary>
    public sealed class ReefPlan
    {
        public const int Width = 90;
        public const int Height = 64;

        public TerrainType[,] Terrain = new TerrainType[Width, Height];

        /// <summary>Piste de terre rouge qui relie le ponton aux repères.</summary>
        public bool[,] Track = new bool[Width, Height];

        /// <summary>Platier du récif au large : eau claire et coraux vus à travers l'eau.</summary>
        public bool[,] Reef = new bool[Width, Height];

        /// <summary>Eau claire : estuaire, crique et platier.</summary>
        public bool[,] Lagoon = new bool[Width, Height];

        public enum Plant { Eucalyptus, Pandanus, Palm, Mangrove, RainTree, TreeFern, Termite, Wattle }

        /// <summary>Plantes posées par le plan (case, sorte, variante) ; toutes solides sauf les buissons d'acacia.</summary>
        public readonly List<(Vector2Int Cell, Plant Kind, int Variant)> Plants = new List<(Vector2Int, Plant, int)>();

        /// <summary>Blocs de granit du cap et de la côte (solides sur la terre).</summary>
        public readonly List<Vector2Int> Rocks = new List<Vector2Int>();

        /// <summary>Fleurs de brousse (on passe dessus).</summary>
        public readonly List<Vector2Int> Flowers = new List<Vector2Int>();

        /// <summary>Coquillages et bois flotté sur la plage (on passe dessus).</summary>
        public readonly List<Vector2Int> Shells = new List<Vector2Int>();

        public Vector2Int House;     // pied de la maison Queenslander (milieu de la façade)
        public Vector2Int Tower;     // pied de la tour des sauveteurs
        public Vector2Int Tinnie;    // barque en alu tirée sur le sable
        public Vector2Int Pontoon;   // plateforme de plongée amarrée sur le récif (case d'eau)
        public Vector2Int Sign;      // panneau « attention crocodiles » au bord de la crique
        public Vector2Int Cay;       // milieu de l'îlot de sable corallien

        /// <summary>Passerelle de planches sur la crique (on y marche) : cases couvertes, berges comprises.</summary>
        public RectInt Bridge;

        /// <summary>Colonne de gauche du ponton des voyages et case de la plage où il part, vers le sud.</summary>
        public Vector2Int DockFoot;

        public const int DockX = 42;
        public const int BridgeY = 44;

        private static readonly Vector2 Inlet = new Vector2(13f, 27f);
        private static readonly Vector2 InletRadii = new Vector2(10.5f, 7.5f);

        private static readonly Vector2[] CreekCourse =
        {
            new Vector2(13f, 29f), new Vector2(15f, 38f), new Vector2(13.5f, 46f), new Vector2(11f, 52f), new Vector2(15f, 59f), new Vector2(14f, 66f),
        };

        private static readonly Vector2[][] TrackCourses =
        {
            new[] { new Vector2(43f, 27f), new Vector2(43f, 33f), new Vector2(50f, 35.5f), new Vector2(58f, 35.5f), new Vector2(68f, 33f), new Vector2(76f, 26f), new Vector2(79f, 20f) },
            new[] { new Vector2(43f, 33f), new Vector2(32f, 37f), new Vector2(24f, 42f), new Vector2(19f, 44.5f), new Vector2(8f, 44.5f), new Vector2(3f, 46f), new Vector2(-2f, 47f) },
        };

        public static ReefPlan Build()
        {
            var plan = new ReefPlan();
            plan.Paint();
            return plan;
        }

        /// <summary>Hauteur du rivage à la colonne <paramref name="x"/> : en dessous, la mer. Le cap de granit avance vers le sud.</summary>
        public static float ShoreAt(float x)
        {
            float cape = 9f * Mathf.Exp(-Mathf.Pow((x - 79f) / 4.5f, 2f));
            return 22f + Mathf.Sin(x * 0.11f) * 1.6f + Mathf.Sin(x * 0.31f + 2f) * 0.6f - cape;
        }

        /// <summary>Lisière de la forêt tropicale : au-dessus, les grands arbres.</summary>
        public static float RainforestAt(float x) => 49f + Mathf.Sin(x * 0.2f) * 1.6f + Mathf.Sin(x * 0.07f + 1f) * 2f;

        private static float Hash(int x, int y, int seed) => IslandGenerator.CellHash(x, y, seed);

        private void Paint()
        {
            var sea = new bool[Width, Height];
            var inlet = new bool[Width, Height];
            var creek = new bool[Width, Height];
            Cay = new Vector2Int(22, 7);

            // 1. Mer au sud, estuaire à l'ouest (avec ses tourbillons de sable), crique de mangrove qui remonte au nord.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = new Vector2(x + 0.5f, y + 0.5f);
                    sea[x, y] = cell.y < ShoreAt(cell.x);
                    float u = (cell.x - Inlet.x) / InletRadii.x, v = (cell.y - Inlet.y) / InletRadii.y;
                    float r = Mathf.Sqrt(u * u + v * v);
                    inlet[x, y] = !sea[x, y] && r < 1f + Mathf.Sin(Mathf.Atan2(v, u) * 3f) * 0.07f;
                    creek[x, y] = !sea[x, y] && !inlet[x, y] && DistanceToPolyline(cell, CreekCourse) <= 1.5f + Mathf.Sin(y * 0.4f) * 0.25f;
                    bool water = sea[x, y] || inlet[x, y] || creek[x, y];
                    Terrain[x, y] = water ? TerrainType.Water : TerrainType.Grass;

                    // Tourbillons : bras de sable blanc en spirale dans l'estuaire, le chenal de la crique reste en eau.
                    if (inlet[x, y] && r > 0.22f && r < 0.86f && DistanceToPolyline(cell, CreekCourse) > 1.6f)
                    {
                        float swirl = Mathf.Sin(Mathf.Atan2(v, u) + r * 9.5f);
                        if (swirl > 0.5f) Terrain[x, y] = TerrainType.Sand;
                    }
                }
            }

            // 2. Plage de sable blanc le long de la mer et de l'estuaire ; la crique a des berges de mangrove (herbe).
            var sand = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] != TerrainType.Grass) continue;
                    // Lisière douce : la largeur de plage ondule lentement (pas de dents de scie case par case).
                    float reach = 4.2f + Mathf.PerlinNoise(x * 0.12f + 3f, y * 0.12f + 9f) * 1.6f;
                    sand[x, y] = Distance(sea, x, y, 6) < reach || Distance(inlet, x, y, 3) < 2.4f;
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (sand[x, y]) Terrain[x, y] = TerrainType.Sand;
                }
            }

            // Îlot de sable corallien au large.
            for (int x = Cay.x - 6; x <= Cay.x + 6; x++)
            {
                for (int y = Cay.y - 3; y <= Cay.y + 3; y++)
                {
                    float u = (x + 0.5f - Cay.x - 0.5f) / 4.2f, v = (y + 0.5f - Cay.y - 0.5f) / 1.9f;
                    if (u * u + v * v < 1f) Terrain[x, y] = TerrainType.Sand;
                }
            }

            // 3. Platier du récif au large (pas dans le chenal du ponton), et autour de l'îlot ; eau claire.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 1; y < Height; y++)
                {
                    if (Terrain[x, y] != TerrainType.Water || !sea[x, y]) continue;
                    float depth = ShoreAt(x + 0.5f) - (y + 0.5f);
                    float noise = Mathf.PerlinNoise(x * 0.13f + 4.2f, y * 0.21f + 1.7f);
                    bool band = depth > 6f && depth < 14f && noise > 0.42f;
                    float cu = (x + 0.5f - Cay.x - 0.5f) / 7.5f, cv = (y + 0.5f - Cay.y - 0.5f) / 4f;
                    bool aroundCay = cu * cu + cv * cv < 1f;
                    bool channel = x >= DockX - 3 && x <= DockX + 4;
                    Reef[x, y] = (band || aroundCay) && !channel;
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Lagoon[x, y] = Terrain[x, y] == TerrainType.Water && (inlet[x, y] || creek[x, y] || Reef[x, y]);
                }
            }

            // 4. Passerelle de planches sur la crique, berges comprises.
            int x0 = int.MaxValue, x1 = int.MinValue;
            for (int x = 0; x < 30; x++)
            {
                for (int y = BridgeY; y <= BridgeY + 1; y++)
                {
                    if (!creek[x, y]) continue;
                    x0 = Mathf.Min(x0, x);
                    x1 = Mathf.Max(x1, x);
                }
            }

            Bridge = new RectInt(x0 - 1, BridgeY, x1 - x0 + 3, 2);

            // 5. Piste de terre rouge (jamais dans l'eau) et ponton au bout, vers le sud.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] == TerrainType.Water) continue;
                    var cell = new Vector2(x + 0.5f, y + 0.5f);
                    bool onTrack = false;
                    foreach (Vector2[] course in TrackCourses) onTrack |= DistanceToPolyline(cell, course) <= 0.8f;
                    bool spur = x >= DockX && x <= DockX + 1 && y <= 27 && y >= ShoreAt(x + 0.5f);
                    Track[x, y] = onTrack || spur;
                }
            }

            int dockY = 27;
            while (dockY > 1 && Terrain[DockX, dockY - 1] != TerrainType.Water) dockY--;
            DockFoot = new Vector2Int(DockX, dockY);

            // 6. Repères.
            House = new Vector2Int(58, 37);
            Tower = new Vector2Int(31, Mathf.CeilToInt(ShoreAt(31.5f)) + 1);
            Tinnie = new Vector2Int(26, Mathf.CeilToInt(ShoreAt(26.5f)));
            Pontoon = new Vector2Int(64, 8);
            Sign = new Vector2Int(Bridge.xMax + 1, BridgeY + 2);
            for (int x = Pontoon.x - 5; x <= Pontoon.x + 5; x++)
            {
                for (int y = Pontoon.y - 2; y <= Pontoon.y + 3; y++) Reef[x, y] = Lagoon[x, y] = false;
            }

            // 7. Granit rose : la pointe du cap (au bord de l'eau et un peu dedans), quelques blocs le long de la plage.
            for (int x = 1; x < Width - 1; x++)
            {
                for (int y = 1; y < Height - 1; y++)
                {
                    bool cape = Mathf.Abs(x - 79) <= 5;
                    if (Track[x, y] || !sea[x, y - 1] && Terrain[x, y] != TerrainType.Water) continue;
                    if (Terrain[x, y] == TerrainType.Sand && sea[x, y - 1] && Hash(x, y, 21) < (cape ? 0.7f : 0.05f) && Mathf.Abs(x - DockX) > 4) Rocks.Add(new Vector2Int(x, y));
                    else if (cape && sea[x, y] && Terrain[x, y + 1] != TerrainType.Water && Hash(x, y, 22) < 0.5f) Rocks.Add(new Vector2Int(x, y));
                }
            }

            // 8. Végétation : pandanus et cocotiers en haut de plage, palétuviers sur les berges de la crique, forêt
            //    tropicale au nord, eucalyptus, termitières et acacias dans la brousse ; fleurs, coquillages.
            bool[,] reserved = Reserved();
            bool Free(Vector2Int cell, float spacing) => !Plants.Exists(p => (p.Cell - cell).sqrMagnitude < spacing * spacing);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (Terrain[x, y] == TerrainType.Water || reserved[x, y] || Track[x, y]) continue;
                    if (y + 2 < Height && (Track[x, y + 1] || Track[x, y + 2])) continue;
                    float h = Hash(x, y, 31);
                    if (Terrain[x, y] == TerrainType.Sand)
                    {
                        bool cayCenter = cell == Cay;
                        bool beachTop = y + 1 < Height && Terrain[x, y + 1] == TerrainType.Grass;
                        if (cayCenter) Plants.Add((cell, Plant.Pandanus, 1));
                        else if (beachTop && h < 0.3f && Free(cell, 3.2f)) Plants.Add((cell, h < 0.14f ? Plant.Palm : Plant.Pandanus, (x + y) % 2));
                        else if (Hash(x, y, 36) < 0.03f && y > 2) Shells.Add(cell);
                        continue;
                    }

                    bool bank = Near(creek, x, y, 1);
                    bool forest = y + 0.5f > RainforestAt(x + 0.5f);
                    if (bank)
                    {
                        if (h < 0.55f && Free(cell, 2.6f)) Plants.Add((cell, Plant.Mangrove, (x + y) % 2));
                    }
                    else if (forest)
                    {
                        bool edge = y >= Height - 3;
                        if ((h < 0.3f || edge && h < 0.6f) && Free(cell, 3.4f)) Plants.Add((cell, Plant.RainTree, (x * 3 + y) % 2));
                        else if (h > 0.82f && Free(cell, 2.4f)) Plants.Add((cell, Plant.TreeFern, (x + y) % 2));
                    }
                    else if (h < 0.08f && Free(cell, 4.5f)) Plants.Add((cell, Plant.Eucalyptus, (x + y) % 2));
                    else if (h > 0.985f && Free(cell, 4f)) Plants.Add((cell, Plant.Termite, (x + y) % 2));
                    else if (h > 0.94f && h < 0.975f && Free(cell, 2.5f)) Plants.Add((cell, Plant.Wattle, (x + y) % 2));
                    else if (Hash(x, y, 37) < 0.04f) Flowers.Add(cell);
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

            Mark(House.x - 5, House.y - 2, House.x + 5, House.y + 6);
            Mark(Tower.x - 2, Tower.y - 1, Tower.x + 2, Tower.y + 3);
            Mark(Tinnie.x - 2, Tinnie.y - 1, Tinnie.x + 2, Tinnie.y + 1);
            Mark(Sign.x - 1, Sign.y - 1, Sign.x + 1, Sign.y + 1);
            Mark(Bridge.xMin - 1, Bridge.yMin - 1, Bridge.xMax, Bridge.yMax);
            Mark(DockFoot.x - 1, DockFoot.y - 7, DockFoot.x + 4, DockFoot.y + 2);
            foreach (Vector2Int r in Rocks) Mark(r.x, r.y, r.x, r.y);
            return taken;
        }

        private static bool Near(bool[,] mask, int x, int y, int reach)
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

        /// <summary>Distance (en cases) à la case la plus proche du masque, au plus <paramref name="reach"/> ; sinon l'infini.</summary>
        private static float Distance(bool[,] mask, int x, int y, int reach)
        {
            float best = float.MaxValue;
            for (int dx = -reach; dx <= reach; dx++)
            {
                for (int dy = -reach; dy <= reach; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < Width && ny < Height && mask[nx, ny]) best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dy * dy));
                }
            }

            return best;
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
