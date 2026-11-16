using UnityEngine;
using UnityEngine.Tilemaps;

namespace FishAndChill.World
{
    /// <summary>
    /// Tuile d'eau animée : une <see cref="Tile"/> ordinaire (même logique, même minimap) dont le dessin enchaîne
    /// quelques images de vagues. Toutes les cases démarrent ensemble, pour que la houle reste continue d'une case à
    /// l'autre.
    /// </summary>
    public sealed class AnimatedWaterTile : Tile
    {
        [SerializeField] private Sprite[] _frames = new Sprite[0];

        [SerializeField, Min(0.1f), Tooltip("Images par seconde.")]
        private float _speed = 2.5f;

        public void Setup(Sprite[] frames, float speed)
        {
            _frames = frames;
            _speed = speed;
            sprite = frames.Length > 0 ? frames[0] : null;
        }

        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
        {
            if (_frames == null || _frames.Length < 2)
            {
                return false;
            }

            tileAnimationData.animatedSprites = _frames;
            tileAnimationData.animationSpeed = _speed;
            tileAnimationData.animationStartTime = 0f;
            return true;
        }
    }
}
