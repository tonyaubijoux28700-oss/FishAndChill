using System.Collections.Generic;
using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class AmazonPlanTests
    {
        private static readonly AmazonPlan Plan = AmazonPlan.Build();

        [Test]
        public void River_CrossesTheMap_WithJungleAboveAndBelow()
        {
            foreach (int x in new[] { 0, 45, 89 })
            {
                bool water = false;
                for (int y = 0; y < AmazonPlan.Height; y++) water |= Plan.Terrain[x, y] == TerrainType.Water;
                Assert.That(water, Is.True, $"fleuve en x = {x}");
                Assert.That(Plan.Terrain[x, 0], Is.Not.EqualTo(TerrainType.Water));
            }
        }

        [Test]
        public void Dock_StandsOnTheBank_FacingTheRiver()
        {
            Assert.That(Plan.Terrain[Plan.DockFoot.x, Plan.DockFoot.y], Is.Not.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[Plan.DockFoot.x, Plan.DockFoot.y + 1], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Path[Plan.DockFoot.x, Plan.DockFoot.y - 2], Is.True, "le petit chemin mène au ponton");
        }

        [Test]
        public void Bridge_SpansTheCreek()
        {
            Assert.That(Plan.Bridge.width, Is.GreaterThan(0));
            bool overWater = false;
            for (int x = Plan.Bridge.xMin; x < Plan.Bridge.xMax; x++)
            {
                for (int y = Plan.Bridge.yMin; y < Plan.Bridge.yMax; y++) overWater |= Plan.Terrain[x, y] == TerrainType.Water;
            }

            Assert.That(overWater, Is.True);
        }

        [Test]
        public void WholeMap_IsReachable_FromTheDock_ByTheSandbankAndTheBridge()
        {
            var seen = Reach(Plan.DockFoot);
            Assert.That(seen[Plan.Kapok.x, Plan.Kapok.y - 2], Is.True, "le fromager, au nord du fleuve");
            Assert.That(seen[84, 49], Is.True, "l'est de la rive nord, au-delà de l'igarapé");
        }

        [Test]
        public void Landmarks_AreOnLand_CanoeAndFloodedTreesInWater()
        {
            foreach (Vector2Int house in Plan.Houses) Assert.That(Plan.Terrain[house.x, house.y], Is.Not.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[Plan.Kapok.x, Plan.Kapok.y], Is.EqualTo(TerrainType.Grass));
            Assert.That(Plan.Terrain[Plan.Canoe.x, Plan.Canoe.y], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.FloodedTrees, Is.Not.Empty);
            foreach (Vector2Int tree in Plan.FloodedTrees) Assert.That(Plan.Terrain[tree.x, tree.y], Is.EqualTo(TerrainType.Water));
        }

        [Test]
        public void Build_IsDeterministic()
        {
            Assert.That(AmazonPlan.Build().Terrain, Is.EqualTo(Plan.Terrain));
        }

        private static bool[,] Reach(Vector2Int start)
        {
            var seen = new bool[AmazonPlan.Width, AmazonPlan.Height];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            seen[start.x, start.y] = true;
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    Vector2Int n = c + d;
                    if (n.x < 0 || n.y < 0 || n.x >= AmazonPlan.Width || n.y >= AmazonPlan.Height || seen[n.x, n.y]) continue;
                    if (Plan.Terrain[n.x, n.y] == TerrainType.Water && !Plan.Bridge.Contains(n)) continue;
                    seen[n.x, n.y] = true;
                    queue.Enqueue(n);
                }
            }

            return seen;
        }
    }
}
