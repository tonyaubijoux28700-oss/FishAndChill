using System;
using System.Collections.Generic;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.UI;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Les zones du jeu et le voyage de l'une à l'autre. Toutes les zones sont sur la même grille, éloignées les unes
    /// des autres (chacune est une « région » de <see cref="WorldMap"/>) ; voyager = arriver au bout du ponton de la
    /// destination, changer la zone de pêche, les limites de la caméra et la minimap.
    /// Toutes les destinations sont ouvertes pour l'instant (l'ordre de déblocage viendra plus tard).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class ZoneTravel : MonoBehaviour
    {
        [Serializable]
        public sealed class Destination
        {
            public FishingZone Zone;

            [Tooltip("Région de la carte (WorldMap) où se trouve la zone.")]
            public int Region;

            [Tooltip("Où le joueur arrive : au bout du ponton.")]
            public Vector2 Arrival;

            [Tooltip("Direction du regard à l'arrivée (vers l'eau).")]
            public Vector2 Facing = Vector2.up;

            [Tooltip("Ponton : on y ouvre la carte du monde avec E.")]
            public Rect Dock;

            [Tooltip("Ponton pour la minimap (repère).")]
            public Transform Marker;
        }

        [SerializeField] private List<Destination> _destinations = new List<Destination>();
        [SerializeField] private PlayerMovement _player;
        [SerializeField] private FishingController _fishing;
        [SerializeField] private WorldMap _map;
        [SerializeField] private CameraFollow _camera;
        [SerializeField] private Minimap _minimap;
        [SerializeField] private Save.SaveManager _save;

        /// <summary>Le joueur vient de changer de zone.</summary>
        public event Action Traveled;

        public IReadOnlyList<Destination> Destinations => _destinations;

        public int Current { get; private set; }

        public Destination CurrentDestination => Current >= 0 && Current < _destinations.Count ? _destinations[Current] : null;

        /// <summary>Identifiant de la zone actuelle (pour la sauvegarde).</summary>
        public string CurrentZoneId => CurrentDestination != null && CurrentDestination.Zone != null ? CurrentDestination.Zone.Id : null;

        /// <summary>Le joueur est sur le ponton de la zone actuelle (et pas dans un bâtiment).</summary>
        public bool PlayerAtDock
        {
            get
            {
                Destination here = CurrentDestination;
                return here != null && _player != null && InteriorBuilding.Occupied == null
                    && here.Dock.Contains((Vector2)_player.transform.position);
            }
        }

        private void Start()
        {
            // La sauvegarde a été lue avant (SaveManager s'exécute en premier) : on reprend dans la bonne zone.
            int saved = _save != null ? IndexOf(_save.SavedZone) : -1;
            if (saved > 0)
            {
                TravelTo(saved);
            }
            else
            {
                Current = 0;
                ApplyZone();
            }
        }

        public int IndexOf(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
            {
                return -1;
            }

            for (int i = 0; i < _destinations.Count; i++)
            {
                if (_destinations[i].Zone != null && _destinations[i].Zone.Id == zoneId)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Emmène le joueur au bout du ponton de la destination.</summary>
        public void TravelTo(int index)
        {
            if (index < 0 || index >= _destinations.Count)
            {
                return;
            }

            Current = index;
            Destination destination = _destinations[index];
            if (_player != null)
            {
                _player.Teleport(destination.Arrival, destination.Facing);
            }

            ApplyZone();
            if (_camera != null)
            {
                _camera.SnapToTarget();
            }

            Traveled?.Invoke();
        }

        private void ApplyZone()
        {
            Destination destination = CurrentDestination;
            if (destination == null)
            {
                return;
            }

            if (_map != null)
            {
                _map.SetCurrentRegion(destination.Region);
            }

            if (_fishing != null && destination.Zone != null)
            {
                _fishing.SetZone(destination.Zone);
            }

            if (destination.Zone != null)
            {
                if (Camera.main != null)
                {
                    Camera.main.backgroundColor = destination.Zone.WaterColor;
                }

                if (_minimap != null)
                {
                    _minimap.SetTitle(destination.Zone.DisplayName);
                    _minimap.Rebuild();
                }
            }
        }
    }
}
