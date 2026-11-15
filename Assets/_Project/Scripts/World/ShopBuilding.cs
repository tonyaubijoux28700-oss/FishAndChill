using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// La boutique : pièce de 10×7 cases avec un comptoir et le marchand (voir <see cref="InteriorBuilding"/>
    /// pour l'entrée et la sortie).
    /// </summary>
    public sealed class ShopBuilding : InteriorBuilding
    {
        public const float InteriorWidth = 10f;
        public const float InteriorHeight = 7f;

        public override Vector2 InteriorSize => new Vector2(InteriorWidth, InteriorHeight);

        protected override Rect FloorArea => new Rect(0.6f, 0.15f, InteriorWidth - 1.2f, 4.35f);

        /// <summary>Devant le comptoir, là où l'on parle au marchand.</summary>
        public Vector2 CounterFront => InteriorOrigin + new Vector2(InteriorWidth * 0.5f, 2.55f);

        /// <summary>Emplacement (pieds) du marchand, derrière le comptoir.</summary>
        public Vector2 MerchantSpot => InteriorOrigin + new Vector2(InteriorWidth * 0.5f, 4.05f);

        /// <summary>Pied du comptoir (pivot de son sprite).</summary>
        public Vector2 CounterSpot => InteriorOrigin + new Vector2(InteriorWidth * 0.5f, 3.1f);

        protected override void SetupInterior(WorldMap map)
        {
            map.AddObstacle(new Rect(CounterSpot.x - 2.5f, CounterSpot.y - 0.1f, 5f, 0.9f));
        }
    }
}
