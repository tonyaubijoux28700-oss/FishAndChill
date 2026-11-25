using FishAndChill.World;
using NUnit.Framework;
using UnityEngine;

namespace FishAndChill.Tests
{
    public sealed class FjordPlanTests
    {
        [Test]
        public void Fjord_EntersFromTheSouth_AndNarrowsUpToItsHead()
        {
            FjordPlan plan = FjordPlan.Build();
            int Water(int y)
            {
                int n = 0;
                for (int x = 0; x < FjordPlan.Width; x++) if (plan.Terrain[x, y] == TerrainType.Water) n++;
                return n;
            }

            Assert.That(Water(2), Is.GreaterThan(Water(40)), "large à l'entrée, étroit au fond");
            Assert.That(plan.Terrain[Mathf.RoundToInt(FjordPlan.CenterAt(0.5f)), 0], Is.EqualTo(TerrainType.Water), "ouvert sur la mer au sud");
            Assert.That(Water(FjordPlan.CliffFoot + 2), Is.EqualTo(0), "plus d'eau au pied des falaises");
        }

        [Test]
        public void Dock_LeavesTheVillageShore_IntoTheBay()
        {
            FjordPlan plan = FjordPlan.Build();
            Vector2Int d = plan.DockFoot;
            Assert.That(plan.Terrain[d.x, d.y], Is.Not.EqualTo(TerrainType.Water), "le ponton part de la terre");
            Assert.That(plan.Terrain[d.x, d.y - 1], Is.EqualTo(TerrainType.Water));
            Assert.That(plan.Terrain[d.x, d.y - 5], Is.EqualTo(TerrainType.Water), "assez d'eau devant pour le ponton");
        }

        [Test]
        public void Bridge_CrossesTheFjord_FromBankToBank()
        {
            FjordPlan plan = FjordPlan.Build();
            Assert.That(plan.BridgeEast - plan.BridgeWest, Is.GreaterThan(6));
            Assert.That(plan.Terrain[plan.BridgeWest, FjordPlan.BridgeRow], Is.Not.EqualTo(TerrainType.Water), "posée sur la rive ouest");
            Assert.That(plan.Terrain[plan.BridgeEast, FjordPlan.BridgeRow], Is.Not.EqualTo(TerrainType.Water), "posée sur la rive est");
        }

        [Test]
        public void Landmarks_StandOnLand_RorbuerOverTheWater()
        {
            FjordPlan plan = FjordPlan.Build();
            foreach (Vector2Int c in new[] { plan.Church, plan.Rack, plan.Storehouse })
            {
                Assert.That(plan.Terrain[c.x, c.y], Is.Not.EqualTo(TerrainType.Water), $"{c} sur la terre");
            }

            Assert.That(plan.Rorbuer.Count, Is.EqualTo(3));
            foreach (Vector2Int r in plan.Rorbuer) Assert.That(plan.Terrain[r.x, r.y], Is.EqualTo(TerrainType.Water), "sur pilotis, au-dessus de l'eau");
            Assert.That(plan.Terrain[plan.Waterfall.x, plan.Waterfall.y], Is.EqualTo(TerrainType.Water), "la cascade tombe dans le fjord");
        }

        [Test]
        public void Forest_GrowsOnTheWestBank_NothingOnThePath()
        {
            FjordPlan plan = FjordPlan.Build();
            int westTrees = 0;
            foreach ((Vector2Int cell, FjordPlan.Plant kind, int _) in plan.Plants)
            {
                Assert.That(plan.Path[cell.x, cell.y], Is.False, "rien sur le chemin");
                Assert.That(plan.Terrain[cell.x, cell.y], Is.EqualTo(TerrainType.Grass));
                if (kind == FjordPlan.Plant.Pine && cell.x < FjordPlan.CenterAt(cell.y)) westTrees++;
            }

            Assert.That(westTrees, Is.GreaterThan(40), "une vraie forêt de sapins à l'ouest");
        }
    }
}
