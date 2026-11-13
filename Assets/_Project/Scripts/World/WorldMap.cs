using UnityEngine;
using UnityEngine.Tilemaps;

namespace FishAndChill.World
{
    /// <summary>
    /// La carte du monde : sait où est la terre et où est l'eau, à partir de la Tilemap de terrain.
    /// Toute case sans tuile de terre est considérée comme de l'eau.
    /// </summary>
    public sealed class WorldMap : MonoBehaviour
    {
        [SerializeField, Tooltip("Tilemap qui contient les tuiles de terre (sable, herbe…).")]
        private Tilemap _land;

        [SerializeField, Tooltip("Tilemap de l'eau : sert à connaître les limites de la carte.")]
        private Tilemap _water;

        public bool IsLand(Vector2 worldPosition)
        {
            return _land != null && _land.HasTile(_land.WorldToCell(worldPosition));
        }

        public bool IsWater(Vector2 worldPosition)
        {
            return !IsLand(worldPosition);
        }

        /// <summary>Un personnage de rayon <paramref name="radius"/> peut-il se tenir ici (ses 4 coins sur la terre) ?</summary>
        public bool CanStandAt(Vector2 worldPosition, float radius)
        {
            return IsLand(worldPosition + new Vector2(-radius, -radius))
                && IsLand(worldPosition + new Vector2(radius, -radius))
                && IsLand(worldPosition + new Vector2(-radius, radius))
                && IsLand(worldPosition + new Vector2(radius, radius));
        }

        /// <summary>Rectangle de la carte en coordonnées du monde (pour bloquer la caméra).</summary>
        public Rect WorldBounds
        {
            get
            {
                Tilemap reference = _water != null ? _water : _land;
                if (reference == null)
                {
                    return new Rect(-50f, -50f, 100f, 100f);
                }

                reference.CompressBounds();
                BoundsInt cells = reference.cellBounds;
                Vector3 min = reference.CellToWorld(cells.min);
                Vector3 max = reference.CellToWorld(cells.max);
                return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }
        }
    }
}
