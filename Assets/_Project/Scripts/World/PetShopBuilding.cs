using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// L'animalerie : pièce de 11×8 cases (voir <see cref="InteriorBuilding"/> pour l'entrée et la sortie), avec le
    /// vendeur derrière son comptoir, le rayon de croquettes, l'arbre à chat, le clapier et le panier du chien (dessins
    /// <c>Editor/PetShopArt</c>). Pour l'instant on n'y achète rien : les animaux de compagnie viendront plus tard.
    /// </summary>
    public sealed class PetShopBuilding : InteriorBuilding
    {
        public const float InteriorWidth = 11f;
        public const float InteriorHeight = 8f;

        public override Vector2 InteriorSize => new Vector2(InteriorWidth, InteriorHeight);

        protected override Rect FloorArea => new Rect(0.3f, 0.15f, InteriorWidth - 0.6f, 4.6f);

        /// <summary>Pied du comptoir (relatif au coin de la pièce).</summary>
        public static readonly Rect Counter = new Rect(3.6f, 3.4f, 3.8f, 0.9f);

        /// <summary>Pied du rayon de croquettes, de l'arbre à chat, du clapier et du panier du chien.</summary>
        public static readonly Rect Shelf = new Rect(0.3f, 4.0f, 3.0f, 1.0f);
        public static readonly Rect CatTree = new Rect(7.5f, 3.7f, 1.6f, 0.5f);
        public static readonly Rect Hutch = new Rect(9.2f, 4.0f, 1.5f, 1.0f);
        public static readonly Rect DogBed = new Rect(0.7f, 1.3f, 2.1f, 0.9f);

        public Vector2 CounterSpot => InteriorOrigin + new Vector2(Counter.center.x, Counter.yMin);

        /// <summary>Le vendeur, juste derrière le comptoir.</summary>
        public Vector2 KeeperSpot => InteriorOrigin + new Vector2(Counter.center.x, Counter.yMin + 1.25f);

        protected override void SetupInterior(WorldMap map)
        {
            foreach (Rect item in new[] { Counter, Shelf, CatTree, Hutch, DogBed })
            {
                map.AddObstacle(new Rect(InteriorOrigin + item.position, item.size));
            }
        }
    }
}
