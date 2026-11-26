using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Plan du fond marin où l'on plonge (vue de dessus, comme le reste du jeu ; 40×26 cases, y vers le haut) : un fond
    /// de sable aux rides, des massifs rocheux qu'on contourne, des herbiers et des patates de corail en décor, la corde
    /// de la bouée au milieu du haut (on arrive et on remonte par là), et des emplacements où les décorations sont posées
    /// à chaque plongée, plus loin de la corde pour les plus rares. Logique pure (testée), dessinée par <c>DiveArt</c>.
    /// </summary>
    public sealed class DivePlan
    {
        public const int Width = 40;
        public const int Height = 26;

        /// <summary>Massifs rocheux (centre, demi-tailles) : on ne passe pas au travers.</summary>
        public readonly List<(Vector2 Center, Vector2 Half)> Rocks = new List<(Vector2, Vector2)>();

        /// <summary>Herbiers (centre, rayon) et patates de corail en décor (centre).</summary>
        public readonly List<(Vector2 Center, float Radius)> Seagrass = new List<(Vector2, float)>();
        public readonly List<Vector2> CoralHeads = new List<Vector2>();

        /// <summary>Emplacements possibles des décorations à ramasser (pied, en cases).</summary>
        public readonly List<Vector2> Spots = new List<Vector2>();

        /// <summary>Pied de la corde de la bouée : on y arrive, on y remonte.</summary>
        public Vector2 Rope = new Vector2(20f, 22.5f);

        public static DivePlan Build(int seed)
        {
            var plan = new DivePlan();
            plan.Paint(new System.Random(seed));
            return plan;
        }

        public bool BlockedAt(Vector2 cell)
        {
            if (cell.x < 0.6f || cell.y < 0.6f || cell.x > Width - 0.6f || cell.y > Height - 0.6f) return true;
            foreach ((Vector2 c, Vector2 h) in Rocks)
            {
                float u = (cell.x - c.x) / h.x, v = (cell.y - c.y) / h.y;
                if (u * u + v * v < 1f) return true;
            }

            return false;
        }

        /// <summary>Distance à la corde, ramenée entre 0 (au pied de la corde) et 1 (au plus loin) : plus loin, plus rare.</summary>
        public float Remoteness(Vector2 cell) => Mathf.Clamp01(Vector2.Distance(cell, Rope) / 26f);

        private void Paint(System.Random random)
        {
            float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);

            // Massifs rocheux : quelques gros, loin de la corde.
            foreach (Vector2 c in new[] { new Vector2(7f, 8f), new Vector2(31f, 6f), new Vector2(10f, 19f), new Vector2(33f, 17f), new Vector2(21f, 10f) })
            {
                Rocks.Add((c + new Vector2(Range(-1f, 1f), Range(-1f, 1f)), new Vector2(Range(2.2f, 3.4f), Range(1.4f, 2.2f))));
            }

            for (int i = 0; i < 9; i++)
            {
                Seagrass.Add((new Vector2(Range(2f, Width - 2f), Range(2f, Height - 2f)), Range(1.5f, 3f)));
            }

            for (int i = 0; i < 14; i++)
            {
                var c = new Vector2(Range(2f, Width - 2f), Range(2f, Height - 3f));
                if (!BlockedAt(c) && Vector2.Distance(c, Rope) > 3f) CoralHeads.Add(c);
            }

            // Emplacements des décorations : sur le sable, ni dans les rochers ni contre la corde, bien espacés.
            for (int tries = 0; tries < 400 && Spots.Count < 22; tries++)
            {
                var c = new Vector2(Range(2f, Width - 2f), Range(1.5f, Height - 2f));
                if (BlockedAt(c) || BlockedAt(c + Vector2.up * 0.8f) || Vector2.Distance(c, Rope) < 3f) continue;
                if (Spots.Exists(s => Vector2.Distance(s, c) < 3.2f) || CoralHeads.Exists(h => Vector2.Distance(h, c) < 1.6f)) continue;
                Spots.Add(c);
            }
        }
    }
}
