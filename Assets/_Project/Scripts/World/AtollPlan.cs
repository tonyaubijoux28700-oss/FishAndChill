using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Plan dessiné de l'atoll des Maldives (90×64 cases, y vers le haut), à la place d'une forme tirée au hasard : un
    /// anneau de récif (platier de corail) autour d'un grand lagon turquoise, coupé de passes vers l'océan profond ; sur
    /// l'anneau, l'île habitée au nord (plage de sable blanc, cocoteraie, chemin de sable corallien, deux maisons en pierre
    /// de corail, bar de plage, torches), un îlot sauvage à l'est, un au sud-ouest et une langue de sable au sud ; dans le
    /// lagon, une longue passerelle sur pilotis en T bordée de villas sur l'eau (on y marche et on y pêche),
    /// un dhoni amarré, un banc de sable avec son unique cocotier et des pâtés de corail. Logique pure
    /// (testée) ; dessiné par <c>MapSetup.BuildAtoll</c>.
    /// </summary>
    public sealed class AtollPlan
    {
        public const int Width = 90;
        public const int Height = 64;

        public TerrainType[,] Terrain = new TerrainType[Width, Height];

        /// <summary>Eau claire : tout le lagon et le platier de l'anneau.</summary>
        public bool[,] Lagoon = new bool[Width, Height];

        /// <summary>Platier de corail (anneau et pâtés du lagon) : des coraux s'y voient à travers l'eau.</summary>
        public bool[,] Reef = new bool[Width, Height];

        /// <summary>Chemin de sable corallien à travers la cocoteraie.</summary>
        public bool[,] Path = new bool[Width, Height];

        public enum Plant { Palm, LeaningPalm, Frangipani, Bush }

        /// <summary>Plantes posées par le plan (case, sorte, variante) ; cocotiers et frangipaniers sont solides.</summary>
        public readonly List<(Vector2Int Cell, Plant Kind, int Variant)> Plants = new List<(Vector2Int, Plant, int)>();

        /// <summary>Fleurs d'hibiscus et d'ipomée (on passe à travers).</summary>
        public readonly List<Vector2Int> Flowers = new List<Vector2Int>();

        /// <summary>Coquillages et bouts de corail blanchi échoués sur la plage (on passe à travers).</summary>
        public readonly List<Vector2Int> Shells = new List<Vector2Int>();

        /// <summary>Torches de bambou le long du chemin (solides, allumées la nuit).</summary>
        public readonly List<Vector2Int> Torches = new List<Vector2Int>();

        /// <summary>Planches de la passerelle sur pilotis (praticables au-dessus de l'eau) : le pied, puis la barre du T.</summary>
        public RectInt JettyStem, JettyArm;

        /// <summary>Pied (pilotis du milieu) de chaque villa sur l'eau.</summary>
        public readonly List<Vector2Int> Villas = new List<Vector2Int>();

        public readonly List<Vector2Int> Houses = new List<Vector2Int>();   // pied (porte) des maisons en pierre de corail
        public Vector2Int Bar;              // pied du bar de plage
        public Vector2Int Dhoni;            // dhoni amarré (case d'eau du milieu)
        public Vector2Int Sandbank;         // cocotier du banc de sable

        /// <summary>Colonne de gauche du ponton des voyages et case de la plage où il part, vers le sud (le lagon).</summary>
        public Vector2Int DockFoot;

        /// <summary>Centre et rayons de l'anneau de récif.</summary>
        public static readonly Vector2 RingCenter = new Vector2(45f, 32f);
        public static readonly Vector2 RingRadii = new Vector2(41f, 28f);

        /// <summary>Passes dans l'anneau (angle en degrés, demi-largeur en degrés) : l'océan profond entre dans le lagon.</summary>
        private static readonly (float Angle, float Half)[] Passes = { (172f, 6f), (252f, 5f), (328f, 6f) };

        /// <summary>Îles sur l'anneau et banc de sable : centre, rayons, avec un cœur de végétation ou non.</summary>
        private static readonly (Vector2 Center, Vector2 Radii, bool Green)[] Islands =
        {
            (new Vector2(38f, 50f), new Vector2(24f, 9f), true),   // l'île habitée, au nord
            (new Vector2(82f, 34f), new Vector2(4.2f, 6.5f), true),    // îlot sauvage de l'est
            (new Vector2(16f, 12f), new Vector2(6.5f, 3.8f), true),    // îlot du sud-ouest
            (new Vector2(60f, 6f), new Vector2(7f, 2.4f), false),      // langue de sable du sud
            (new Vector2(57f, 24f), new Vector2(3.4f, 1.7f), false),   // banc de sable du lagon
        };

        private static readonly Vector2[] MainPath =
        {
            new Vector2(16f, 49.5f), new Vector2(23f, 50.5f), new Vector2(31f, 50f), new Vector2(39f, 50.5f), new Vector2(47f, 50.5f),
            new Vector2(54f, 49.5f), new Vector2(60f, 48.5f),
        };

        /// <summary>Colonnes de la passerelle et du ponton des voyages (2 cases de large chacun).</summary>
        public const int JettyX = 29;
        public const int DockX = 46;

        public static AtollPlan Build()
        {
            var plan = new AtollPlan();
            plan.Paint();
            return plan;
        }

        /// <summary>Distance normalisée au centre de l'anneau (1 = sur l'anneau).</summary>
        public static float Ring(float x, float y)
        {
            float u = (x - RingCenter.x) / RingRadii.x, v = (y - RingCenter.y) / RingRadii.y;
            return Mathf.Sqrt(u * u + v * v);
        }

        public static float RingAngle(float x, float y) =>
            Mathf.Repeat(Mathf.Atan2((y - RingCenter.y) / RingRadii.y, (x - RingCenter.x) / RingRadii.x) * Mathf.Rad2Deg, 360f);

        /// <summary>Dans une passe de l'anneau ?</summary>
        public static bool InPass(float x, float y)
        {
            float angle = RingAngle(x, y);
            foreach ((float a, float half) in Passes)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(angle, a)) < half) return true;
            }

            return false;
        }

        private static float Hash(int x, int y, int seed) => IslandGenerator.CellHash(x, y, seed);

        private void Paint()
        {
            // 1. Océan partout, eau claire dans l'anneau et sur le platier (sauf dans les passes).
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Terrain[x, y] = TerrainType.Water;
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float band = (Ring(fx, fy) - 1f) * RingRadii.y;   // en cases, > 0 dehors
                    float wobble = Mathf.Sin(RingAngle(fx, fy) * 0.31f) * 0.8f + (Hash(x, y, 3) - 0.5f) * 0.8f;
                    bool pass = InPass(fx, fy);
                    Reef[x, y] = !pass && band > -2.5f + wobble && band < 3.5f + wobble;
                    Lagoon[x, y] = band < 0f || Reef[x, y];
                }
            }

            // Pâtés de corail dans le lagon.
            foreach ((float cx, float cy, float r) in new[] { (30f, 22f, 3.2f), (70f, 21f, 2.8f), (42f, 13f, 2.4f), (66f, 41f, 2.2f), (20f, 30f, 2f) })
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        float dx = x + 0.5f - cx, dy = (y + 0.5f - cy) * 1.3f;
                        if (dx * dx + dy * dy < r * r * (0.8f + Hash(x, y, 4) * 0.4f)) Reef[x, y] = true;
                    }
                }
            }

            // 2. Les îles : contour qui ondule, plage de sable blanc, cœur de végétation pour les plus grandes.
            foreach ((Vector2 c, Vector2 r, bool green) in Islands)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        float u = (x + 0.5f - c.x) / r.x, v = (y + 0.5f - c.y) / r.y;
                        float angle = Mathf.Atan2(v, u);
                        float edge = 1f + Mathf.Sin(angle * 5f + c.x) * 0.07f + Mathf.Sin(angle * 9f + c.y) * 0.04f;
                        if (u * u + v * v < edge * edge) Terrain[x, y] = TerrainType.Sand;
                    }
                }

                if (!green) continue;
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        float u = (x + 0.5f - c.x) / r.x, v = (y + 0.5f - c.y) / r.y;
                        if (u * u + v * v > 1.2f || Terrain[x, y] != TerrainType.Sand) continue;
                        // Plage plus large côté lagon (au sud de l'île habitée).
                        bool lagoonSide = c.y > 40f && y < c.y;
                        int reach = lagoonSide ? 3 : 2;
                        if (!NearWater(x, y, reach)) Terrain[x, y] = TerrainType.Grass;
                    }
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Terrain[x, y] != TerrainType.Water) Reef[x, y] = false;
                }
            }

            // 3. Chemin de sable corallien à travers la cocoteraie, avec ses deux branches vers la passerelle et le ponton.
            for (int x = 0; x < Width; x++)
            {
                for (int y = 40; y < Height; y++)
                {
                    if (Terrain[x, y] != TerrainType.Grass) continue;
                    var cell = new Vector2(x + 0.5f, y + 0.5f);
                    bool main = DistanceToPolyline(cell, MainPath) <= 0.8f;
                    bool toJetty = x >= JettyX && x <= JettyX + 1 && y < 50;
                    bool toDock = x >= DockX && x <= DockX + 1 && y < 50;
                    Path[x, y] = main || toJetty || toDock;
                }
            }

            // 4. Ponton des voyages et passerelle : partent de la plage vers le sud, dans le lagon.
            DockFoot = new Vector2Int(DockX, ShoreBelow(DockX, 50));
            int jettyShore = ShoreBelow(JettyX, 50);
            const int armY = 30;
            JettyStem = new RectInt(JettyX, armY + 2, 2, jettyShore - (armY + 2) + 1);
            JettyArm = new RectInt(14, armY, 30, 2);

            // Villas sur l'eau : quatre le long de la barre du T (porte sur les planches), deux de part et d'autre du pied.
            foreach (int vx in new[] { 17, 23, 35, 41 }) Villas.Add(new Vector2Int(vx, armY + 2));
            Villas.Add(new Vector2Int(JettyX - 2, armY + 5));
            Villas.Add(new Vector2Int(JettyX + 3, armY + 5));

            // 5. Repères de l'île habitée.
            Houses.Add(new Vector2Int(21, 52));
            Houses.Add(new Vector2Int(37, 52));
            Bar = new Vector2Int(55, ShoreBelow(55, 50) + 1);
            Dhoni = new Vector2Int(21, ShoreBelow(21, 50) - 3);
            Sandbank = new Vector2Int(57, 24);

            for (int x = 19; x < 60; x += 6)
            {
                int y = Mathf.RoundToInt(PathYAt(x + 0.5f)) - 2;
                var cell = new Vector2Int(x, y);
                if (Terrain[x, y] == TerrainType.Grass && !Path[x, y] && !Reserved()[x, y]) Torches.Add(cell);
            }

            // 6. Végétation : cocoteraie dense, frangipaniers près des maisons et du chemin, cocotiers penchés en haut des
            //    plages, buissons de veloutier en lisière ; jamais sur le chemin ni autour des repères.
            bool[,] reserved = Reserved();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (Terrain[x, y] == TerrainType.Water || reserved[x, y] || Path[x, y]) continue;
                    if (y + 2 < Height && (Path[x, y + 1] || Path[x, y + 2])) continue;   // le feuillage cacherait le chemin
                    var cell = new Vector2Int(x, y);
                    bool sandbank = cell == Sandbank;
                    if (sandbank)
                    {
                        Plants.Add((cell, Plant.Palm, 0));
                        continue;
                    }

                    if (Terrain[x, y] == TerrainType.Grass)
                    {
                        bool village = Houses.Exists(h => Mathf.Abs(h.x - x) <= 6 && Mathf.Abs(h.y - y) <= 4);
                        bool nearPath = DistanceToPolyline(new Vector2(x + 0.5f, y + 0.5f), MainPath) < 3f;
                        bool edge = NearWater(x, y, 3);
                        if (edge && Hash(x, y, 51) < 0.18f && !Plants.Exists(p => Mathf.Abs(p.Cell.x - x) < 2 && Mathf.Abs(p.Cell.y - y) < 2))
                        {
                            Plants.Add((cell, Plant.Bush, (x + y) % 2));
                            continue;
                        }

                        float density = village ? 0.3f : 0.75f;
                        if (Hash(x, y, 52) > density) continue;
                        if (Plants.Exists(p => p.Kind != Plant.Bush && (p.Cell - cell).sqrMagnitude < 5)) continue;
                        Plant kind = (village || nearPath) && Hash(x, y, 53) < 0.45f ? Plant.Frangipani : Plant.Palm;
                        Plants.Add((cell, kind, kind == Plant.Frangipani ? 0 : (int)(Hash(x, y, 54) * 2f)));
                    }
                    else if (Terrain[x, y] == TerrainType.Sand)
                    {
                        // Cocotiers penchés vers l'eau, en haut des plages ; coquillages sur le sable.
                        bool beachTop = y + 1 < Height && Terrain[x, y + 1] == TerrainType.Grass || y > 0 && Terrain[x, y - 1] == TerrainType.Grass;
                        if (beachTop && Hash(x, y, 55) < 0.16f && !Plants.Exists(p => Mathf.Abs(p.Cell.x - x) < 4 && Mathf.Abs(p.Cell.y - y) < 3))
                        {
                            bool westHalf = x < IslandCenterX(x, y);
                            Plants.Add((cell, Plant.LeaningPalm, westHalf ? 0 : 1));
                        }
                        else if (Hash(x, y, 56) < 0.045f) Shells.Add(cell);
                    }
                }
            }

            for (int y = 1; y < Height - 1; y++)
            {
                for (int x = 1; x < Width - 1; x++)
                {
                    if (Terrain[x, y] == TerrainType.Grass && !reserved[x, y] && !Path[x, y] && Hash(x, y, 61) < 0.06f
                        && !Plants.Exists(p => p.Cell == new Vector2Int(x, y))) Flowers.Add(new Vector2Int(x, y));
                }
            }
        }

        /// <summary>Hauteur du chemin principal à la colonne <paramref name="x"/>.</summary>
        public static float PathYAt(float x)
        {
            for (int i = 1; i < MainPath.Length; i++)
            {
                if (x <= MainPath[i].x) return Mathf.Lerp(MainPath[i - 1].y, MainPath[i].y, Mathf.InverseLerp(MainPath[i - 1].x, MainPath[i].x, x));
            }

            return MainPath[MainPath.Length - 1].y;
        }

        private static float IslandCenterX(int x, int y)
        {
            float best = float.MaxValue, cx = 0f;
            foreach ((Vector2 c, Vector2 _, bool _) in Islands)
            {
                float d = (new Vector2(x, y) - c).sqrMagnitude;
                if (d < best) { best = d; cx = c.x; }
            }

            return cx;
        }

        /// <summary>Dernière case de terre en descendant la colonne <paramref name="x"/> depuis <paramref name="fromY"/> (bord de la plage).</summary>
        private int ShoreBelow(int x, int fromY)
        {
            int y = fromY;
            while (y > 1 && Terrain[x, y - 1] != TerrainType.Water) y--;
            return y;
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

            foreach (Vector2Int house in Houses) Mark(house.x - 5, house.y - 1, house.x + 5, house.y + 7);
            Mark(Bar.x - 3, Bar.y - 1, Bar.x + 3, Bar.y + 3);
            Mark(DockFoot.x - 1, DockFoot.y - 7, DockFoot.x + 4, DockFoot.y + 2);
            Mark(JettyX - 1, JettyStem.yMax - 2, JettyX + 2, JettyStem.yMax + 1);
            foreach (Vector2Int torch in Torches) Mark(torch.x - 1, torch.y - 1, torch.x + 1, torch.y + 1);
            return taken;
        }

        private bool NearWater(int x, int y, int reach)
        {
            for (int dx = -reach; dx <= reach; dx++)
            {
                for (int dy = -reach; dy <= reach; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= Width || ny >= Height || Terrain[nx, ny] == TerrainType.Water) return true;
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
