using System.Collections.Generic;
using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class BayPlanTests
    {
        private static readonly BayPlan Plan = BayPlan.Build();

        [Test]
        public void Sea_IsInTheSouth_AndGoesDeepestInTheMiddle()
        {
            Assert.That(Plan.Terrain[45, 0], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[2, 30], Is.Not.EqualTo(TerrainType.Water));
            Assert.That(BayPlan.ShoreAt(45f), Is.GreaterThan(BayPlan.ShoreAt(5f) + 8f));
        }

        [Test]
        public void Dock_StartsOnTheShore_FacingTheSea()
        {
            Vector2Int foot = Plan.DockFoot;
            Assert.That(Plan.Terrain[foot.x, foot.y], Is.Not.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[foot.x, foot.y - 1], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[foot.x + 1, foot.y - 1], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Path[foot.x, foot.y + 1], Is.True, "le petit chemin mène au ponton");
        }

        [Test]
        public void Shrine_IsReachable_FromTheDock_AndTheBridgeLeadsEast()
        {
            bool[,] seen = Reach(Plan.DockFoot);
            Assert.That(Plan.Bridge.width, Is.GreaterThan(0));
            Assert.That(seen[Plan.Shrine.x, Plan.Shrine.y - 2], Is.True, "parvis du sanctuaire");
            Assert.That(seen[Plan.Onsen.x, Plan.Onsen.y - 2], Is.True, "devant l'onsen, au-delà du ruisseau");
            Assert.That(seen[Plan.Fuji.x - 8, Plan.Fuji.y], Is.True, "on peut passer à côté du Fuji pour le voir en entier");
        }

        [Test]
        public void Landmarks_AreWhereTheyBelong()
        {
            Assert.That(Plan.Terrain[Plan.SeaTorii.x, Plan.SeaTorii.y], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[Plan.WeddedRocks.x, Plan.WeddedRocks.y], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[Plan.Boat.x, Plan.Boat.y], Is.EqualTo(TerrainType.Sand));
            Assert.That(Plan.Stone[BayPlan.AxisX, Plan.GateTorii.y], Is.True, "le torii enjambe l'allée");
            foreach (Vector2Int house in Plan.Houses) Assert.That(Plan.Terrain[house.x, house.y], Is.Not.EqualTo(TerrainType.Water));
            Assert.That(Plan.Fuji.y - Plan.Shrine.y, Is.InRange(3, 5), "le Fuji se dresse juste derrière le sanctuaire");
            Assert.That(Plan.Trees.Exists(t => Mathf.Abs(t.Cell.x - Plan.Fuji.x) <= 6 && t.Cell.y >= Plan.Fuji.y - 1 && t.Cell.y < Plan.Fuji.y + 5), Is.False, "aucun arbre devant ni sur le Fuji");
            foreach (var tree in Plan.Trees) Assert.That(Plan.Path[tree.Cell.x, tree.Cell.y] || Plan.Stone[tree.Cell.x, tree.Cell.y], Is.False);
        }

        [Test]
        public void Build_IsDeterministic()
        {
            Assert.That(BayPlan.Build().Terrain, Is.EqualTo(Plan.Terrain));
        }

        private static bool[,] Reach(Vector2Int start)
        {
            var seen = new bool[BayPlan.Width, BayPlan.Height];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            seen[start.x, start.y] = true;
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    Vector2Int n = c + d;
                    if (n.x < 0 || n.y < 0 || n.x >= BayPlan.Width || n.y >= BayPlan.Height || seen[n.x, n.y]) continue;
                    if ((Plan.Terrain[n.x, n.y] == TerrainType.Water && !Plan.Bridge.Contains(n))) continue;
                    seen[n.x, n.y] = true;
                    queue.Enqueue(n);
                }
            }

            return seen;
        }
    }
}
