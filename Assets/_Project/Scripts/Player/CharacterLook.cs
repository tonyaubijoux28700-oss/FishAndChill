using System;

namespace FishAndChill.Player
{
    /// <summary>
    /// Apparence du pêcheur choisie en début de partie (sauvegardée) : prénom et un index par option de
    /// <see cref="CharacterOptions"/>. Les valeurs par défaut donnent le pêcheur d'origine (chapeau de paille,
    /// cheveux châtains courts, chemise à fleurs turquoise, short orange).
    /// </summary>
    [Serializable]
    public sealed class CharacterLook
    {
        public string Name = string.Empty;
        public int Skin = 1;
        public int HairStyle;
        public int HairColor;
        public int Hat = 1;
        public int Top;
        public int TopColor;
        public int Bottom;

        public CharacterLook Clone() => (CharacterLook)MemberwiseClone();

        public int Get(CharacterPart part)
        {
            switch (part)
            {
                case CharacterPart.Skin: return Skin;
                case CharacterPart.HairStyle: return HairStyle;
                case CharacterPart.HairColor: return HairColor;
                case CharacterPart.Hat: return Hat;
                case CharacterPart.Top: return Top;
                case CharacterPart.TopColor: return TopColor;
                default: return Bottom;
            }
        }

        public void Set(CharacterPart part, int value)
        {
            switch (part)
            {
                case CharacterPart.Skin: Skin = value; break;
                case CharacterPart.HairStyle: HairStyle = value; break;
                case CharacterPart.HairColor: HairColor = value; break;
                case CharacterPart.Hat: Hat = value; break;
                case CharacterPart.Top: Top = value; break;
                case CharacterPart.TopColor: TopColor = value; break;
                default: Bottom = value; break;
            }
        }
    }
}
