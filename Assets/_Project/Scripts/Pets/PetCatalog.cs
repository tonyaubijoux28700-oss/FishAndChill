using System.Collections.Generic;
using UnityEngine;

namespace FishAndChill.Pets
{
    /// <summary>Les animaux vendus à l'animalerie, dans l'ordre du menu.</summary>
    [CreateAssetMenu(menuName = "FishAndChill/Catalogue d'animaux", fileName = "Animaux")]
    public sealed class PetCatalog : ScriptableObject
    {
        [SerializeField] private List<PetDefinition> _pets = new List<PetDefinition>();

        public IReadOnlyList<PetDefinition> Pets => _pets;

        public PetDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (PetDefinition pet in _pets)
            {
                if (pet != null && pet.Id == id) return pet;
            }

            return null;
        }
    }
}
