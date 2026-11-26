using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class HaLongPlanTests
    {
        [Test]
        public void Bay_OpensToTheSouthEast_LandToTheNorthAndWest()
        {
            HaLongPlan plan = HaLongPlan.Build();
            Assert.That(plan.Terrain[60, 10], Is.EqualTo(TerrainType.Water), "la baie au sud-est");
            Assert.That(plan.Terrain[60, 52], Is.Not.EqualTo(TerrainType.Water), "la rive du village au nord");
            Assert.That(plan.Terrain[3, 30], Is.Not.EqualTo(TerrainType.Water), "la presqu'île à l'ouest");
            for (int x = 0; x < HaLongPlan.Width; x++)
            {
                Assert.That(plan.Terrain[x, HaLongPlan.CliffFoot], Is.Not.EqualTo(TerrainType.Water), "plus d'eau au pied des montagnes");
            }
        }

        [Test]
        public void Dock_LeavesTheBeach_IntoTheBay()
        {
            HaLongPlan plan = HaLongPlan.Build();
            Vector2Int d = plan.DockFoot;
            Assert.That(plan.Terrain[d.x, d.y], Is.Not.EqualTo(TerrainType.Water), "le ponton part de la terre");
            for (int k = 1; k <= 6; k++)
            {
                Assert.That(plan.Terrain[d.x, d.y - k], Is.EqualTo(TerrainType.Water), "assez d'eau devant pour le ponton");
                Assert.That(plan.Terrain[d.x + 1, d.y - k], Is.EqualTo(TerrainType.Water));
            }
        }

        [Test]
        public void FloatingVillage_IsReachedByAWalkway_FromTheBeach()
        {
            HaLongPlan plan = HaLongPlan.Build();
            RectInt w = plan.Walkway;
            Assert.That(w.yMin, Is.EqualTo(plan.Platform.yMax), "la passerelle part de la plate-forme");
            Assert.That(plan.Terrain[w.xMin, w.yMax], Is.Not.EqualTo(TerrainType.Water), "et arrive sur la plage");
            Assert.That(plan.Terrain[w.xMin, w.yMax - 1] == TerrainType.Water || plan.Terrain[w.xMin + 1, w.yMax - 1] == TerrainType.Water, Is.True, "au ras de l'eau");
            Assert.That(plan.Path[w.xMin, w.yMax] || plan.Path[w.xMin + 1, w.yMax], Is.True, "le chemin du village la rejoint");
            for (int x = plan.Platform.xMin; x < plan.Platform.xMax; x++)
            {
                for (int y = plan.Platform.yMin; y < plan.Platform.yMax; y++) Assert.That(plan.Terrain[x, y], Is.EqualTo(TerrainType.Water), "la plate-forme flotte");
            }

            Assert.That(plan.FloatingHouses.Count, Is.EqualTo(2));
            foreach (Vector2Int h in plan.FloatingHouses) Assert.That(plan.Platform.Contains(h), Is.True, "les maisons sont sur la plate-forme");
        }

        [Test]
        public void Pitons_RiseFromOpenWater_AwayFromThePassages()
        {
            HaLongPlan plan = HaLongPlan.Build();
            Assert.That(plan.Pitons.Count, Is.GreaterThanOrEqualTo(8), "une vraie baie de pitons");
            var dock = new RectInt(plan.DockFoot.x - 1, plan.DockFoot.y - 8, 4, 9);
            foreach ((Vector2Int c, int _) in plan.Pitons)
            {
                for (int dx = -3; dx <= 3; dx++)
                {
                    Assert.That(plan.Terrain[c.x + dx, c.y], Is.EqualTo(TerrainType.Water), $"le piton {c} sort de l'eau");
                }

                Assert.That(dock.Overlaps(new RectInt(c.x - 3, c.y - 1, 7, 5)), Is.False, "rien devant le ponton");
                Assert.That(plan.Walkway.Overlaps(new RectInt(c.x - 3, c.y - 1, 7, 5)), Is.False, "ni sur la passerelle");
            }

            Assert.That(plan.Junks.Count, Is.GreaterThanOrEqualTo(2));
            foreach (Vector2Int j in plan.Junks) Assert.That(plan.Terrain[j.x, j.y], Is.EqualTo(TerrainType.Water));
        }

        [Test]
        public void Village_StandsOnLand_NothingGrowsOnThePath()
        {
            HaLongPlan plan = HaLongPlan.Build();
            Assert.That(plan.Terrain[plan.Temple.x, plan.Temple.y], Is.EqualTo(TerrainType.Grass));
            foreach (Vector2Int h in plan.Houses) Assert.That(plan.Terrain[h.x, h.y], Is.Not.EqualTo(TerrainType.Water));
            Assert.That(plan.CaveIndex, Is.LessThan(plan.Karsts.Count));
            int trees = 0;
            foreach ((Vector2Int cell, HaLongPlan.Plant kind, int _) in plan.Plants)
            {
                Assert.That(plan.Path[cell.x, cell.y], Is.False, "rien sur le chemin");
                Assert.That(plan.Terrain[cell.x, cell.y], Is.EqualTo(TerrainType.Grass));
                if (kind == HaLongPlan.Plant.Banyan || kind == HaLongPlan.Plant.Tree) trees++;
            }

            Assert.That(trees, Is.GreaterThan(20), "une forêt tropicale sur la presqu'île");
        }
    }
}
