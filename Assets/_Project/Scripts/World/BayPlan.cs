using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Plan dessiné de la baie d'Izu (90×64 cases, y vers le haut), à la place d'une forme tirée au hasard : la mer au
    /// sud qui s'enfonce en arrondi entre deux caps rocheux, une plage de sable volcanique au fond de la baie, un torii
    /// planté dans l'eau dans l'axe du sanctuaire, le sanctuaire shinto au pied des collines (allée de dalles, torii,
    /// lanternes de pierre, parvis), une falaise et le mont Fuji au loin sur le plateau, un ruisseau descendu d'une
    /// cascade et franchi par un pont rouge, un hameau de pêcheurs (deux maisons, une barque tirée sur le sable), un
    /// onsen fumant à l'est, un bois de bambous, des érables rouges autour du sanctuaire, des cerisiers et des pins
    /// noirs, deux rochers mariés reliés par une corde dans la mer. Logique pure (testée) ; dessiné par
    /// <c>MapSetup.BuildJapan</c>.
    /// </summary>
    public sealed class BayPlan
    {
        public const int Width = 90;
        public const int Height = 64;

        public TerrainType[,] Terrain = new TerrainType[Width, Height];

        /// <summary>Plateau au-dessus de la falaise (inaccessible) et face de la falaise.</summary>
        public bool[,] Plateau = new bool[Width, Height];
        public bool[,] Cliff = new bool[Width, Height];
        public int[] Ridge = new int[Width];

        /// <summary>Dalles de pierre : allée du sanctuaire et parvis.</summary>
        public bool[,] Stone = new bool[Width, Height];

        /// <summary>Chemin de terre claire qui longe la côte.</summary>
        public bool[,] Path = new bool[Width, Height];

        public enum Plant { Sakura, Pine, Maple, Bamboo }

        /// <summary>Arbres posés par le plan (tous solides).</summary>
        public readonly List<(Vector2Int Cell, Plant Kind)> Trees = new List<(Vector2Int, Plant)>();

        /// <summary>Lanternes de pierre (solides, allumées la nuit).</summary>
        public readonly List<Vector2Int> Lanterns = new List<Vector2Int>();

        /// <summary>Rochers de la côte (sur la terre au bord de l'eau, ou dans l'eau près des caps).</summary>
        public readonly List<Vector2Int> Rocks = new List<Vector2Int>();

        /// <summary>Touffes d'herbes et de fleurs (on passe à travers).</summary>
        public readonly List<Vector2Int> Flowers = new List<Vector2Int>();

        public Vector2Int Waterfall;          // pied de la cascade, au bas de la falaise
        public RectInt Bridge;                // cases du pont rouge sur le ruisseau
        public Vector2Int Shrine;             // pied du sanctuaire (milieu de la façade)
        public Vector2Int GateTorii;          // torii à l'entrée de l'allée
        public Vector2Int SeaTorii;           // torii dans l'eau
        public Vector2Int Onsen;              // pied du bassin
        public Vector2Int Boat;               // barque tirée sur le sable
        public Vector2Int WeddedRocks;        // rochers mariés, dans la mer
        public Vector2Int Fuji;               // pied du mont Fuji, sur le plateau
        public readonly List<Vector2Int> Houses = new List<Vector2Int>();

        /// <summary>Colonne de gauche du ponton (2 cases de large) et case de la rive où il part, vers le sud.</summary>
        public Vector2Int DockFoot;

        public const int AxisX = 44;          // axe du sanctuaire, de l'allée et du torii dans l'eau

        private static readonly Vector2[] StreamCourse = { new Vector2(62f, 52f), new Vector2(60.5f, 45f), new Vector2(64f, 37f), new Vector2(63f, 29f), new Vector2(66f, 20f), new Vector2(67f, 8f) };

        private static readonly Vector2[] CoastPath =
        {
            new Vector2(4f, 13f), new Vector2(12f, 15.5f), new Vector2(21f, 20.5f), new Vector2(32f, 25f), new Vector2(44f, 26.5f), new Vector2(56f, 26f),
            new Vector2(66f, 24f), new Vector2(76f, 19f), new Vector2(86f, 14f),
        };

        public static BayPlan Build()
        {
            var plan = new BayPlan();
            plan.Paint();
            return plan;
        }

        /// <summary>Hauteur du rivage à la colonne <paramref name="x"/> : en dessous, la mer.</summary>
        public static float ShoreAt(float x)
        {
            float u = (x - 45f) / 34f;
            float bowl = Mathf.Max(0f, 1f - u * u);
            return 8f + 14f * bowl + Mathf.Sin(x * 0.23f + 1.1f) * 0.9f + Mathf.Sin(x * 0.09f) * 1.2f;
        }

        private static float Hash(int x, int y, int seed) => IslandGenerator.CellHash(x, y, seed);

        private void Paint()
        {
            // 1. Falaise et plateau au nord, mer au sud, terre entre les deux.
            for (int x = 0; x < Width; x++)
            {
                Ridge[x] = Mathf.RoundToInt(56f + 2f * Mathf.Sin(x * 0.15f) + 1.5f * Mathf.Sin(x * 0.06f + 0.7f));
                float shore = ShoreAt(x + 0.5f);
                for (int y = 0; y < Height; y++)
                {
                    Terrain[x, y] = y + 0.5f < shore ? TerrainType.Water : TerrainType.Grass;
                    Plateau[x, y] = y >= Ridge[x];
                    Cliff[x, y] = y < Ridge[x] && y >= Ridge[x] - 2;
                }
            }

            // 2. Le ruisseau, de la cascade jusqu'à la mer.
            Waterfall = new Vector2Int(62, Ridge[62] - 2);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (y < Ridge[x] - 1 && DistanceToPolyline(new Vector2(x + 0.5f, y + 0.5f), StreamCourse) <= 1.25f) Terrain[x, y] = TerrainType.Water;
                }
            }

            // 3. Plage : large au fond de la baie, étroite sur les caps ; berges du ruisseau en sable aussi.
            var sand = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] != TerrainType.Grass || Plateau[x, y] || Cliff[x, y]) continue;
                    bool bay = x >= 22 && x <= 68 && y < ShoreAt(x + 0.5f) + 5f;
                    int reach = bay ? (Hash(x, y, 3) < 0.5f ? 3 : 2) : 1;
                    sand[x, y] = NearWater(x, y, reach);
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (sand[x, y]) Terrain[x, y] = TerrainType.Sand;
                }
            }

            // 4. Sanctuaire au pied de la falaise, allée de dalles jusqu'à la plage, parvis devant.
            Shrine = new Vector2Int(AxisX, Ridge[AxisX] - 9);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] == TerrainType.Water || Plateau[x, y] || Cliff[x, y]) continue;
                    bool alley = x >= AxisX - 1 && x <= AxisX + 1 && y >= ShoreAt(AxisX + 0.5f) + 1f && y < Shrine.y - 2;
                    bool forecourt = x >= AxisX - 5 && x <= AxisX + 5 && y >= Shrine.y - 3 && y < Shrine.y;
                    Stone[x, y] = alley || forecourt;
                }
            }

            GateTorii = new Vector2Int(AxisX, Mathf.CeilToInt(ShoreAt(AxisX + 0.5f)) + 7);
            for (int y = GateTorii.y + 3; y < Shrine.y - 3; y += 4)
            {
                Lanterns.Add(new Vector2Int(AxisX - 3, y));
                Lanterns.Add(new Vector2Int(AxisX + 3, y));
            }

            // 5. Chemin côtier ; le pont rouge là où il croise le ruisseau.
            int bridgeMinX = int.MaxValue, bridgeMaxX = int.MinValue, bridgeMinY = int.MaxValue, bridgeMaxY = int.MinValue;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = new Vector2(x + 0.5f, y + 0.5f);
                    if (DistanceToPolyline(cell, CoastPath) > 1.05f && !(DistanceToSegment(cell, new Vector2(56f, 26f), new Vector2(56f, 21f)) <= 0.75f)) continue;
                    if (Plateau[x, y] || Cliff[x, y] || Stone[x, y]) continue;
                    if (Terrain[x, y] == TerrainType.Water)
                    {
                        if (DistanceToPolyline(cell, StreamCourse) > 1.8f || y < 15) continue;
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

            // 6. Ponton : au bout du petit chemin, vers le sud (la mer). 2 cases de large, colonnes 55 et 56.
            int dockY = 26;
            while (dockY > 1 && Terrain[55, dockY - 1] != TerrainType.Water) dockY--;
            DockFoot = new Vector2Int(55, dockY);

            // 7. Repères : torii dans l'eau, hameau, barque, onsen, rochers mariés, Fuji.
            SeaTorii = new Vector2Int(AxisX, Mathf.FloorToInt(ShoreAt(AxisX + 0.5f)) - 6);
            Houses.Add(new Vector2Int(23, 25));
            Houses.Add(new Vector2Int(33, 29));
            Boat = new Vector2Int(35, Mathf.CeilToInt(ShoreAt(35.5f)) + 1);
            Onsen = new Vector2Int(76, 29);
            WeddedRocks = new Vector2Int(74, 5);
            Fuji = new Vector2Int(45, Ridge[45]);

            // 8. Rochers : sur les caps, au bord de l'eau ; quelques-uns dans la mer.
            for (int x = 1; x < Width - 1; x++)
            {
                for (int y = 1; y < Height - 1; y++)
                {
                    bool cape = x < 20 || x > 70;
                    if (!cape || Path[x, y] || DistanceToPolyline(new Vector2(x + 0.5f, y + 0.5f), StreamCourse) < 3f) continue;
                    if (Terrain[x, y] == TerrainType.Sand && Terrain[x, y - 1] == TerrainType.Water && Hash(x, y, 21) < 0.3f) Rocks.Add(new Vector2Int(x, y));
                    else if (Terrain[x, y] == TerrainType.Water && Terrain[x, y + 2] != TerrainType.Water && Hash(x, y, 22) < 0.1f) Rocks.Add(new Vector2Int(x, y));
                }
            }

            Rocks.RemoveAll(r => Mathf.Abs(r.x - WeddedRocks.x) <= 3 && Mathf.Abs(r.y - WeddedRocks.y) <= 3);

            // 9. Végétation : bambouseraie au nord-est, érables autour du sanctuaire, pins sur les caps et en haut de
            //    plage, cerisiers ailleurs ; jamais sur les chemins ni autour des repères.
            bool[,] reserved = Reserved();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (Terrain[x, y] != TerrainType.Grass || Cliff[x, y] || reserved[x, y]) continue;
                    bool nearWay = NearMask(Path, x, y, 1) || NearMask(Stone, x, y, 1) || (y + 3 < Height && (Path[x, y + 2] || Path[x, y + 3] || Stone[x, y + 2] || Stone[x, y + 3]));
                    if (nearWay && !Plateau[x, y]) continue;
                    bool bamboo = Ellipse(x, y, 80f, 44f, 9f, 8f) <= 1f;
                    bool shrineWood = Ellipse(x, y, AxisX + 0.5f, Shrine.y + 1f, 14f, 8f) <= 1f;
                    bool cape = x < 18 || x > 72;
                    bool fujiView = Plateau[x, y] && Mathf.Abs(x - Fuji.x) < 16;
                    if (fujiView) continue;
                    float density = Plateau[x, y] ? 0.35f : bamboo ? 0.6f : shrineWood ? 0.3f : 0.12f;
                    if (Hash(x, y, 31) > density) continue;
                    int spacingX = bamboo ? 2 : 3;
                    if (Trees.Exists(t => Mathf.Abs(t.Cell.x - x) < spacingX && Mathf.Abs(t.Cell.y - y) < 2)) continue;
                    Plant kind;
                    if (bamboo && !Plateau[x, y]) kind = Plant.Bamboo;
                    else if (Plateau[x, y] || cape || y < 30) kind = Plant.Pine;
                    else if (shrineWood) kind = Hash(x, y, 32) < 0.6f ? Plant.Maple : Plant.Sakura;
                    else kind = Hash(x, y, 33) < 0.65f ? Plant.Sakura : Plant.Pine;
                    Trees.Add((new Vector2Int(x, y), kind));
                }
            }

            for (int y = 1; y < Height - 1; y++)
            {
                for (int x = 1; x < Width - 1; x++)
                {
                    if (Terrain[x, y] == TerrainType.Grass && !Plateau[x, y] && !Cliff[x, y] && !reserved[x, y] && !Path[x, y] && !Stone[x, y]
                        && Hash(x, y, 41) < 0.05f && !Trees.Exists(t => t.Cell == new Vector2Int(x, y))) Flowers.Add(new Vector2Int(x, y));
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

            Mark(Shrine.x - 4, Shrine.y - 1, Shrine.x + 4, Shrine.y + 5);
            Mark(GateTorii.x - 3, GateTorii.y - 1, GateTorii.x + 3, GateTorii.y + 3);
            foreach (Vector2Int house in Houses) Mark(house.x - 3, house.y - 1, house.x + 2, house.y + 4);
            Mark(Onsen.x - 4, Onsen.y - 1, Onsen.x + 4, Onsen.y + 4);
            Mark(Boat.x - 2, Boat.y - 1, Boat.x + 2, Boat.y + 1);
            Mark(DockFoot.x - 2, DockFoot.y - 7, DockFoot.x + 3, DockFoot.y + 2);
            Mark(Bridge.xMin - 1, Bridge.yMin - 1, Bridge.xMax, Bridge.yMax);
            Mark(Waterfall.x - 2, Waterfall.y - 2, Waterfall.x + 2, Waterfall.y + 1);
            foreach (Vector2Int lantern in Lanterns) Mark(lantern.x, lantern.y, lantern.x, lantern.y + 1);
            foreach (Vector2Int rock in Rocks) Mark(rock.x, rock.y, rock.x, rock.y);
            return taken;
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
