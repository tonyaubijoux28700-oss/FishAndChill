using System.Collections.Generic;
using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class AtollPlanTests
    {
        private static readonly AtollPlan Plan = AtollPlan.Build();

        [Test]
        public void Lagoon_InTheMiddle_DeepOceanInTheCorners()
        {
            Assert.That(Plan.Terrain[45, 20], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Lagoon[45, 20], Is.True, "le lagon au centre");
            Assert.That(Plan.Lagoon[0, 0], Is.False, "l'océan profond dans les coins");
            Assert.That(Plan.Lagoon[89, 63], Is.False);
        }

        [Test]
        public void ReefRing_IsCutByPasses()
        {
            int passes = 0;
            bool wasReef = true;
            for (int i = 0; i <= 360; i++)
            {
                float a = i * Mathf.Deg2Rad;
                float x = AtollPlan.RingCenter.x + Mathf.Cos(a) * AtollPlan.RingRadii.x;
                float y = AtollPlan.RingCenter.y + Mathf.Sin(a) * AtollPlan.RingRadii.y;
                bool reef = !AtollPlan.InPass(x, y);
                if (wasReef && !reef) passes++;
                wasReef = reef;
            }

            Assert.That(passes, Is.EqualTo(3));
        }

        [Test]
        public void MainIsland_HasAGroveAndABeach()
        {
            Assert.That(Plan.Terrain[38, 50], Is.Not.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[38, 53], Is.EqualTo(TerrainType.Grass), "cocoteraie au milieu de l'île");
            Assert.That(Plan.Plants.FindAll(p => p.Kind == AtollPlan.Plant.Palm).Count, Is.GreaterThan(30));
            foreach (var plant in Plan.Plants) Assert.That(Plan.Path[plant.Cell.x, plant.Cell.y], Is.False, "rien ne pousse sur le chemin");
        }

        [Test]
        public void Dock_StartsOnTheBeach_FacingTheLagoon()
        {
            Vector2Int foot = Plan.DockFoot;
            Assert.That(Plan.Terrain[foot.x, foot.y], Is.Not.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[foot.x, foot.y - 1], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[foot.x + 1, foot.y - 1], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Lagoon[foot.x, foot.y - 3], Is.True);
        }

        [Test]
        public void Jetty_LeavesTheBeach_AndAllItsPlanksAreOverTheLagoon()
        {
            RectInt stem = Plan.JettyStem, arm = Plan.JettyArm;
            Assert.That(Plan.Terrain[stem.xMin, stem.yMax], Is.Not.EqualTo(TerrainType.Water), "le pied part de la plage");
            Assert.That(arm.yMax, Is.EqualTo(stem.yMin), "la barre du T touche le pied");
            Assert.That(stem.xMin, Is.InRange(arm.xMin, arm.xMax - 2));
            foreach (RectInt r in new[] { arm, new RectInt(stem.x, stem.y, stem.width, stem.height - 1) })
            {
                for (int x = r.xMin; x < r.xMax; x++)
                {
                    for (int y = r.yMin; y < r.yMax; y++)
                    {
                        Assert.That(Plan.Terrain[x, y], Is.EqualTo(TerrainType.Water), $"planche ({x}, {y}) au-dessus de l'eau");
                        Assert.That(Plan.Lagoon[x, y], Is.True);
                    }
                }
            }
        }

        [Test]
        public void Villas_StandInTheLagoon_AlongTheJetty()
        {
            Assert.That(Plan.Villas.Count, Is.EqualTo(6));
            foreach (Vector2Int v in Plan.Villas)
            {
                Assert.That(Plan.Terrain[v.x, v.y], Is.EqualTo(TerrainType.Water));
                Assert.That(Plan.JettyArm.Contains(v) || Plan.JettyStem.Contains(v), Is.False, "les pilotis ne sont pas sur les planches");
            }
        }

        [Test]
        public void Landmarks_AreReachable_FromTheDock()
        {
            bool[,] seen = Reach(Plan.DockFoot);
            foreach (Vector2Int house in Plan.Houses)
            {
                Assert.That(Plan.Terrain[house.x, house.y], Is.Not.EqualTo(TerrainType.Water));
                Assert.That(seen[house.x, house.y - 1], Is.True, "devant la maison");
            }

            Assert.That(seen[Plan.Bar.x, Plan.Bar.y - 1], Is.True, "devant le bar");
            Assert.That(seen[Plan.JettyArm.xMin, Plan.JettyArm.yMin], Is.True, "bout de la passerelle des villas");
            Assert.That(Plan.Terrain[Plan.Dhoni.x, Plan.Dhoni.y], Is.EqualTo(TerrainType.Water));
            Assert.That(Plan.Terrain[Plan.Sandbank.x, Plan.Sandbank.y], Is.EqualTo(TerrainType.Sand), "le banc de sable");
        }

        [Test]
        public void Build_IsDeterministic()
        {
            Assert.That(AtollPlan.Build().Terrain, Is.EqualTo(Plan.Terrain));
        }

        private static bool[,] Reach(Vector2Int start)
        {
            var seen = new bool[AtollPlan.Width, AtollPlan.Height];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            seen[start.x, start.y] = true;
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    Vector2Int n = c + d;
                    if (n.x < 0 || n.y < 0 || n.x >= AtollPlan.Width || n.y >= AtollPlan.Height || seen[n.x, n.y]) continue;
                    bool planks = Plan.JettyStem.Contains(n) || Plan.JettyArm.Contains(n);
                    if (Plan.Terrain[n.x, n.y] == TerrainType.Water && !planks) continue;
                    seen[n.x, n.y] = true;
                    queue.Enqueue(n);
                }
            }

            return seen;
        }
    }
}
