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

        [SerializeField, Tooltip("Obstacles fixes du décor (générés avec l'île) : on ne traverse plus lampadaires, haies, cocotiers…")]
        private System.Collections.Generic.List<Rect> _staticObstacles = new System.Collections.Generic.List<Rect>();

        [SerializeField, Tooltip("Rectangle de chaque zone sur la grille (île, lac…), en coordonnées du monde. Vide = toute la carte.")]
        private System.Collections.Generic.List<Rect> _regions = new System.Collections.Generic.List<Rect>();

        [SerializeField, Tooltip("Sols praticables au-dessus de l'eau, enregistrés avec la scène (pontons).")]
        private System.Collections.Generic.List<Rect> _staticWalkable = new System.Collections.Generic.List<Rect>();

        private int _currentRegion;

        public Tilemap Land => _land;
        public Tilemap Water => _water;

        /// <summary>
        /// Terre ou eau selon la côte arrondie (champ lissé des 4 cases voisines), exactement comme elle est
        /// dessinée par les tuiles de transition.
        /// </summary>
        public bool IsLand(Vector2 worldPosition)
        {
            foreach (Rect area in _staticWalkable)
            {
                if (area.Contains(worldPosition))
                {
                    return true;
                }
            }

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

        /// <summary>
        /// Remplace les obstacles fixes de l'île (pied des lampadaires, tonneaux, haies, troncs de cocotiers…),
        /// enregistrés avec la scène. Appelé par la génération de l'île.
        /// </summary>
        public void SetStaticObstacles(System.Collections.Generic.IEnumerable<Rect> obstacles)
        {
            _staticObstacles.Clear();
            _staticObstacles.AddRange(obstacles);
        }

        public int StaticObstacleCount => _staticObstacles.Count;

        public bool IsBlocked(Vector2 worldPosition)
        {
            foreach (Rect obstacle in _staticObstacles)
            {
                if (obstacle.Contains(worldPosition))
                {
                    return true;
                }
            }

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
            // Boîte du personnage contre chaque obstacle : même un obstacle fin (pied de lampadaire, tronc)
            // qui tiendrait entre deux coins arrête le joueur.
            var box = new Rect(worldPosition.x - radius, worldPosition.y - radius, radius * 2f, radius * 2f);
            if (Overlaps(_staticObstacles, box) || Overlaps(_obstacles, box))
            {
                return false;
            }

            return IsLand(worldPosition + new Vector2(-radius, -radius))
                && IsLand(worldPosition + new Vector2(radius, -radius))
                && IsLand(worldPosition + new Vector2(-radius, radius))
                && IsLand(worldPosition + new Vector2(radius, radius));
        }

        private static bool Overlaps(System.Collections.Generic.List<Rect> obstacles, Rect box)
        {
            foreach (Rect obstacle in obstacles)
            {
                if (obstacle.Overlaps(box))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Remplace les régions (une par zone) et les pontons. Appelé par la génération de la carte.</summary>
        public void SetRegions(System.Collections.Generic.IEnumerable<Rect> regions, System.Collections.Generic.IEnumerable<Rect> walkable)
        {
            _regions.Clear();
            _regions.AddRange(regions);
            _staticWalkable.Clear();
            _staticWalkable.AddRange(walkable);
            _currentRegion = 0;
        }

        public int RegionCount => _regions.Count;

        public int CurrentRegion => _currentRegion;

        /// <summary>Rectangle d'une région (toute la carte s'il n'y a pas de régions).</summary>
        public Rect RegionBounds(int index)
        {
            if (_regions.Count == 0)
            {
                return WorldBounds;
            }

            return _regions[Mathf.Clamp(index, 0, _regions.Count - 1)];
        }

        /// <summary>Change la région affichée (caméra, minimap) après un voyage.</summary>
        public void SetCurrentRegion(int index)
        {
            _currentRegion = Mathf.Clamp(index, 0, Mathf.Max(0, _regions.Count - 1));
        }

        /// <summary>Rectangle de la région actuelle (ou de toute la carte) en coordonnées du monde (pour bloquer la caméra).</summary>
        public Rect WorldBounds
        {
            get
            {
                if (_regions.Count > 0)
                {
                    return _regions[Mathf.Clamp(_currentRegion, 0, _regions.Count - 1)];
                }

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
