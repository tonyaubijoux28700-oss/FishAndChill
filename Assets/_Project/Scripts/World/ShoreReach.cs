using FishAndChill.Fishing;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Les cases d'une région qu'on rejoint à pied depuis son ponton (calculées une fois), et la rive d'où lancer sur un
    /// point d'eau : droit en face (haut, bas, gauche ou droite), à portée de lancer, sur une case atteignable. Sert aux
    /// coins de pêche et aux remous légendaires, pour que rien n'apparaisse au large d'un îlot qu'on ne peut pas rejoindre.
    /// </summary>
    public sealed class ShoreReach
    {
        private static readonly Vector2[] Directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        private readonly WorldMap _map;
        private readonly Rect _bounds;
        private readonly bool[,] _reachable;

        /// <param name="dock">Le ponton de la région : point de départ à pied.</param>
        public ShoreReach(WorldMap map, Rect bounds, Rect dock)
        {
            _map = map;
            _bounds = bounds;
            int width = Mathf.CeilToInt(bounds.width), height = Mathf.CeilToInt(bounds.height);
            bool Stand(int x, int y) => map.CanStandAt(new Vector2(bounds.xMin + x + 0.5f, bounds.yMin + y + 0.5f), 0.3f);
            var start = new Vector2Int(Mathf.FloorToInt(dock.center.x - bounds.xMin), Mathf.FloorToInt(dock.center.y - bounds.yMin));
            for (int r = 0; r <= 3 && !(start.x >= 0 && start.y >= 0 && start.x < width && start.y < height && Stand(start.x, start.y)); r++)
            {
                start += Vector2Int.up;   // le milieu du ponton tombe parfois entre deux planches : on cherche juste à côté
            }

            _reachable = FishingSpotRules.Reachable(width, height, start, Stand);
            if (!_reachable[Mathf.Clamp(start.x, 0, width - 1), Mathf.Clamp(start.y, 0, height - 1)])
            {
                _reachable = null;   // ponton introuvable : toute rive compte
            }
        }

        /// <summary>Peut-on rejoindre à pied l'endroit <paramref name="q"/> (sa case ou une case voisine) ?</summary>
        public bool Reached(Vector2 q)
        {
            if (_reachable == null) return true;
            int cx = Mathf.FloorToInt(q.x - _bounds.xMin), cy = Mathf.FloorToInt(q.y - _bounds.yMin);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x >= 0 && y >= 0 && x < _reachable.GetLength(0) && y < _reachable.GetLength(1) && _reachable[x, y]) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Une rive atteignable à pied, droit en face de <paramref name="p"/>, entre <paramref name="min"/> et
        /// <paramref name="max"/> cases ; <paramref name="facing"/> regarde vers l'eau.
        /// </summary>
        public bool FindShore(Vector2 p, float min, float max, out Vector2 stand, out Vector2 facing)
        {
            foreach (Vector2 d in Directions)
            {
                for (float k = min; k <= max; k += 0.2f)
                {
                    Vector2 q = p - d * k;
                    if (_map.CanStandAt(q, 0.3f) && Reached(q))
                    {
                        stand = q;
                        facing = d;
                        return true;
                    }
                }
            }

            stand = p;
            facing = Vector2.up;
            return false;
        }
    }
}
