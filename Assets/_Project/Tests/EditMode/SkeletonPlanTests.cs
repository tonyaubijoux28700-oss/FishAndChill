using System.Collections.Generic;
using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class SkeletonPlanTests
    {
        private static readonly SkeletonPlan Plan = SkeletonPlan.Build();

        [Test]
        public void Ocean_IsInTheSouth_DesertInTheNorth()
        {
            Assert.That(Plan.Terrain[45, 0], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[45, 60], Is.EqualTo(TerrainType.Grass), "la plaine de graviers du désert");
            Assert.That(SkeletonPlan.ShoreAt(62f), Is.LessThan(SkeletonPlan.ShoreAt(50f) - 4f), "le cap avance dans la mer");
        }

        [Test]
        public void Dock_StartsOnTheBeach_FacingTheSea()
        {
            Vector2Int foot = Plan.DockFoot;
            Assert.That(Plan.Terrain[foot.x, foot.y], Is.EqualTo(TerrainType.Sand));
            Assert.That(Plan.Terrain[foot.x, foot.y - 1], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[foot.x + 1, foot.y - 1], Is.EqualTo(TerrainType.Water));
            for (int k = 1; k <= 6; k++) Assert.That(Plan.Terrain[foot.x, foot.y - k], Is.EqualTo(TerrainType.Water), "assez d'eau pour le ponton");
        }

        [Test]
        public void Landmarks_AreWhereTheyBelong_AndReachable()
        {
            bool[,] seen = Reach(Plan.DockFoot);
            Assert.That(Plan.Terrain[Plan.Wreck.x, Plan.Wreck.y], Is.EqualTo(TerrainType.Sand), "le cargo est échoué sur la plage");
            Assert.That(Plan.Terrain[Plan.Trawler.x, Plan.Trawler.y], Is.EqualTo(TerrainType.Water), "le chalutier est dans les vagues");
            Assert.That(Plan.Terrain[Plan.Whale.x, Plan.Whale.y], Is.Not.EqualTo(TerrainType.Water));
            Assert.That(seen[Plan.Wreck.x, Plan.Wreck.y - 1], Is.True, "devant le cargo");
            Assert.That(seen[Plan.Whale.x, Plan.Whale.y - 1], Is.True, "devant la baleine");
            Assert.That(seen[Plan.Shack.x, Plan.Shack.y - 1], Is.True, "devant la cabane");
            foreach (var plant in Plan.Plants) Assert.That(Plan.Track[plant.Cell.x, plant.Cell.y], Is.False, "rien ne pousse sur la piste");
        }

        [Test]
        public void Lagoon_IsSaltWaterBehindTheBeach()
        {
            Assert.That(Plan.Terrain[20, 39], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[20, 33], Is.Not.EqualTo(TerrainType.Water), "une langue de sable sépare la lagune de l'océan");
        }

        [Test]
        public void Build_IsDeterministic()
        {
            Assert.That(SkeletonPlan.Build().Terrain, Is.EqualTo(Plan.Terrain));
        }

        private static bool[,] Reach(Vector2Int start)
        {
            var seen = new bool[SkeletonPlan.Width, SkeletonPlan.Height];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            seen[start.x, start.y] = true;
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    Vector2Int n = c + d;
                    if (n.x < 0 || n.y < 0 || n.x >= SkeletonPlan.Width || n.y >= SkeletonPlan.Height || seen[n.x, n.y]) continue;
                    if (Plan.Terrain[n.x, n.y] == TerrainType.Water) continue;
                    seen[n.x, n.y] = true;
                    queue.Enqueue(n);
                }
            }

            return seen;
        }
    }
}
