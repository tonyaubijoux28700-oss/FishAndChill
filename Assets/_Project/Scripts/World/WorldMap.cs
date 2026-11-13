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

        public Tilemap Land => _land;
        public Tilemap Water => _water;

        /// <summary>
        /// Terre ou eau selon la côte arrondie (champ lissé des 4 cases voisines), exactement comme elle est
        /// dessinée par les tuiles de transition.
        /// </summary>
        public bool IsLand(Vector2 worldPosition)
        {
            // Les intérieurs (boutique…) sont des sols praticables hors de l'île.
            foreach (Rect area in _walkableAreas)
            {
                if (area.Contains(worldPosition))
                {
                    return true;
                }
            }

            if (_land == null)
            {
                return false;
            }

            // Coordonnées où le centre de la case (i, j) est en (i, j).
            Vector3 local = _land.WorldToLocal(worldPosition);
            Vector3 cellSize = _land.layoutGrid != null ? _land.layoutGrid.cellSize : Vector3.one;
            var cellSpace = new Vector2(local.x / cellSize.x - 0.5f, local.y / cellSize.y - 0.5f);
            return SmoothTerrain.Sample(cellSpace, LandValue) >= SmoothTerrain.Threshold;
        }

        /// <summary>La case contient-elle une tuile de terre (vue « carrée », pour la logique par case) ?</summary>
        public bool IsLandCell(Vector3Int cell)
        {
            return _land != null && _land.HasTile(cell);
        }

        private float LandValue(int i, int j) => _land.HasTile(new Vector3Int(i, j, 0)) ? 1f : 0f;

        public bool IsWater(Vector2 worldPosition)
        {
            return !IsLand(worldPosition);
        }

        private readonly System.Collections.Generic.List<Rect> _obstacles = new System.Collections.Generic.List<Rect>();
        private readonly System.Collections.Generic.List<Rect> _walkableAreas = new System.Collections.Generic.List<Rect>();

        /// <summary>Ajoute un sol praticable hors de l'île (intérieur d'un bâtiment), en coordonnées du monde.</summary>
        public void AddWalkableArea(Rect area) => _walkableAreas.Add(area);

        /// <summary>Ajoute une zone infranchissable (bâtiment…), en coordonnées du monde.</summary>
        public void AddObstacle(Rect area) => _obstacles.Add(area);

        public bool IsBlocked(Vector2 worldPosition)
        {
            foreach (Rect obstacle in _obstacles)
            {
                if (obstacle.Contains(worldPosition))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Un personnage de rayon <paramref name="radius"/> peut-il se tenir ici : ses 4 coins sur la terre
        /// et hors des obstacles ?
        /// </summary>
        public bool CanStandAt(Vector2 worldPosition, float radius)
        {
            for (int corner = 0; corner < 4; corner++)
            {
                var offset = new Vector2(corner % 2 == 0 ? -radius : radius, corner < 2 ? -radius : radius);
                if (IsBlocked(worldPosition + offset))
                {
                    return false;
                }
            }

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
