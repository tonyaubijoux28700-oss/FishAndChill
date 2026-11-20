using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Maison d'un habitant de l'île : pièce de 9×7 cases (voir <see cref="InteriorBuilding"/> pour l'entrée et la
    /// sortie), meubles peints dans le décor (<c>Editor/HomeArt</c>) dont les pieds sont infranchissables. Ses habitants
    /// (<c>Villagers/Villager</c>) y sont debout la nuit et se baladent sur l'île le jour.
    /// </summary>
    public sealed class VillagerHouseBuilding : InteriorBuilding
    {
        public const float InteriorWidth = 9f;
        public const float InteriorHeight = 7f;

        [SerializeField, Tooltip("Pieds des meubles, relatifs au coin bas-gauche de la pièce.")]
        private List<Rect> _furniture = new List<Rect>();

        [SerializeField, Tooltip("Place des habitants chez eux (pieds), relative au coin bas-gauche de la pièce.")]
        private List<Vector2> _residents = new List<Vector2>();

        public override Vector2 InteriorSize => new Vector2(InteriorWidth, InteriorHeight);

        protected override Rect FloorArea => new Rect(0.3f, 0.15f, InteriorWidth - 0.6f, 4.25f);

        protected override void SetupInterior(WorldMap map)
        {
            foreach (Rect item in _furniture)
            {
                map.AddObstacle(new Rect(InteriorOrigin + item.position, item.size));
            }
        }
    }
}
