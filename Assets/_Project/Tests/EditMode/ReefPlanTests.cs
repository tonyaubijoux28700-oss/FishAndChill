using System.Collections.Generic;
using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class ReefPlanTests
    {
        private static readonly ReefPlan Plan = ReefPlan.Build();

        [Test]
        public void Sea_IsInTheSouth_RainforestInTheNorth()
        {
            Assert.That(Plan.Terrain[45, 0], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[45, 60], Is.EqualTo(TerrainType.Grass));
            Assert.That(ReefPlan.ShoreAt(79f), Is.LessThan(ReefPlan.ShoreAt(60f) - 5f), "le cap de granit avance dans la mer");
        }

        [Test]
        public void Dock_StartsOnTheBeach_FacingTheSea()
        {
            Vector2Int foot = Plan.DockFoot;
            Assert.That(Plan.Terrain[foot.x, foot.y], Is.EqualTo(TerrainType.Sand));
            for (int k = 1; k <= 6; k++)
            {
                Assert.That(Plan.Terrain[foot.x, foot.y - k], Is.EqualTo(TerrainType.Water), "assez d'eau pour le ponton");
                Assert.That(Plan.Reef[foot.x, foot.y - k], Is.False, "pas de corail sous le ponton");
            }
        }

        [Test]
        public void Bridge_CrossesTheCreek_FromBankToBank()
        {
            RectInt b = Plan.Bridge;
            Assert.That(Plan.Terrain[b.xMin, b.yMin], Is.Not.EqualTo(TerrainType.Water), "la passerelle part de la berge");
            Assert.That(Plan.Terrain[b.xMax - 1, b.yMin], Is.Not.EqualTo(TerrainType.Water), "et arrive sur l'autre berge");
            bool water = false;
            for (int x = b.xMin; x < b.xMax; x++) water |= Plan.Terrain[x, b.yMin] == TerrainType.Water;
            Assert.That(water, Is.True, "elle passe au-dessus de la crique");
        }

        [Test]
        public void Inlet_HasSandSwirls_InClearWater()
        {
            int sand = 0, water = 0;
            for (int x = 6; x < 20; x++)
            {
                for (int y = 22; y < 32; y++)
                {
                    if (Plan.Terrain[x, y] == TerrainType.Sand) sand++;
                    else if (Plan.Terrain[x, y] == TerrainType.Water && Plan.Lagoon[x, y]) water++;
                }
            }

            Assert.That(sand, Is.GreaterThan(15), "des bras de sable dans l'estuaire");
            Assert.That(water, Is.GreaterThan(15), "de l'eau claire entre eux");
        }

        [Test]
        public void Landmarks_AreWhereTheyBelong_AndReachable()
        {
            bool[,] seen = Reach(Plan.DockFoot);
            Assert.That(Plan.Terrain[Plan.Pontoon.x, Plan.Pontoon.y], Is.EqualTo(TerrainType.Water), "la plateforme est sur le récif");
            Assert.That(Plan.Terrain[Plan.Tower.x, Plan.Tower.y], Is.EqualTo(TerrainType.Sand), "la tour des sauveteurs est sur la plage");
            Assert.That(Plan.Terrain[Plan.Cay.x, Plan.Cay.y], Is.EqualTo(TerrainType.Sand), "l'îlot de sable au large");
            Assert.That(seen[Plan.House.x, Plan.House.y - 1], Is.True, "devant la maison");
            Assert.That(seen[Plan.Tower.x, Plan.Tower.y - 1] || seen[Plan.Tower.x + 2, Plan.Tower.y], Is.True, "près de la tour");
            Assert.That(seen[Plan.Bridge.xMax - 1, Plan.Bridge.yMin], Is.True, "jusqu'à la passerelle");
            foreach (var plant in Plan.Plants) Assert.That(Plan.Track[plant.Cell.x, plant.Cell.y], Is.False, "rien ne pousse sur la piste");
        }

        [Test]
        public void Build_IsDeterministic()
        {
            Assert.That(ReefPlan.Build().Terrain, Is.EqualTo(Plan.Terrain));
        }

        private static bool[,] Reach(Vector2Int start)
        {
            var seen = new bool[ReefPlan.Width, ReefPlan.Height];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            seen[start.x, start.y] = true;
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    Vector2Int n = c + d;
                    if (n.x < 0 || n.y < 0 || n.x >= ReefPlan.Width || n.y >= ReefPlan.Height || seen[n.x, n.y]) continue;
                    if (Plan.Terrain[n.x, n.y] == TerrainType.Water) continue;
                    seen[n.x, n.y] = true;
                    queue.Enqueue(n);
                }
            }

            return seen;
        }
    }
}
