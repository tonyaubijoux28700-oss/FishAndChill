using System;
using UnityEngine;

namespace FishAndChill.Player
{
    /// <summary>
    /// Ce qu'un habitant porte en plus d'une apparence de pêcheur (<see cref="CharacterLook"/>), dessiné par
    /// <see cref="CharacterPainter"/> dans les 4 directions avec le même pas : barbe, bonnet à pompon, jupe ou robe,
    /// pantalon long et bottes, lunettes, fleur dans les cheveux, taille d'enfant. Une couleur à alpha 0 = absent.
    /// </summary>
    [Serializable]
    public sealed class NpcExtras
    {
        public Color Beard;
        public Color Beanie;
        public Color Skirt;
        public Color Pants;
        public Color HairFlower;
        public bool Glasses;
        public bool Child;

        public bool HasBeard => Beard.a > 0f;
        public bool HasBeanie => Beanie.a > 0f;
        public bool HasSkirt => Skirt.a > 0f;
        public bool HasPants => Pants.a > 0f;
        public bool HasHairFlower => HairFlower.a > 0f;
    }
}
