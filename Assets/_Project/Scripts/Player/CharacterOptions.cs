using UnityEngine;

namespace FishAndChill.Player
{
    /// <summary>
    /// Choix possibles pour l'apparence du pêcheur : teints, coiffures, couleurs, chapeaux, hauts. Logique pure
    /// (faire défiler un choix, tirer une apparence au hasard, nettoyer le prénom), testée.
    /// </summary>
    public static class CharacterOptions
    {
        public const int MaxNameLength = 14;
        public const string DefaultName = "Pêcheur";

        public static readonly string[] HairStyles = { "Courte", "Hérissée", "Mi-longue", "Longue", "Queue de cheval", "Chignon", "Rasée" };
        public static readonly string[] Hats = { "Aucun", "Paille", "Casquette", "Bob", "Bandana" };
        public static readonly string[] Tops = { "Chemise à fleurs", "T-shirt", "Débardeur", "Marinière" };

        public static readonly string[] SkinNames = { "Très clair", "Clair", "Doré", "Mat", "Brun", "Foncé" };
        public static readonly Color[] SkinColors =
        {
            new Color(0.99f, 0.84f, 0.72f), new Color(0.97f, 0.76f, 0.58f), new Color(0.90f, 0.66f, 0.47f),
            new Color(0.78f, 0.54f, 0.37f), new Color(0.60f, 0.40f, 0.26f), new Color(0.44f, 0.28f, 0.19f),
        };

        public static readonly string[] HairColorNames = { "Châtain", "Noir", "Blond", "Roux", "Châtain clair", "Gris", "Rose", "Bleu", "Vert" };
        public static readonly Color[] HairColors =
        {
            new Color(0.38f, 0.23f, 0.14f), new Color(0.14f, 0.12f, 0.13f), new Color(0.92f, 0.76f, 0.40f),
            new Color(0.80f, 0.36f, 0.16f), new Color(0.60f, 0.40f, 0.22f), new Color(0.80f, 0.80f, 0.80f),
            new Color(0.96f, 0.52f, 0.72f), new Color(0.30f, 0.52f, 0.92f), new Color(0.34f, 0.72f, 0.44f),
        };

        public static readonly string[] TopColorNames = { "Turquoise", "Rouge", "Jaune", "Vert", "Bleu marine", "Violet", "Rose", "Orange", "Blanc", "Noir" };
        public static readonly Color[] TopColors =
        {
            new Color(0.16f, 0.62f, 0.74f), new Color(0.84f, 0.26f, 0.24f), new Color(0.96f, 0.80f, 0.28f),
            new Color(0.30f, 0.66f, 0.34f), new Color(0.20f, 0.32f, 0.64f), new Color(0.56f, 0.36f, 0.74f),
            new Color(0.95f, 0.55f, 0.68f), new Color(0.96f, 0.55f, 0.22f), new Color(0.93f, 0.93f, 0.90f),
            new Color(0.25f, 0.25f, 0.30f),
        };

        public static readonly string[] BottomNames = { "Orange", "Bleu marine", "Beige", "Rouge", "Vert", "Jean", "Blanc", "Noir" };
        public static readonly Color[] BottomColors =
        {
            new Color(0.96f, 0.55f, 0.22f), new Color(0.20f, 0.30f, 0.58f), new Color(0.72f, 0.64f, 0.44f),
            new Color(0.80f, 0.24f, 0.22f), new Color(0.30f, 0.56f, 0.32f), new Color(0.34f, 0.48f, 0.70f),
            new Color(0.92f, 0.92f, 0.88f), new Color(0.24f, 0.24f, 0.28f),
        };

        /// <summary>Lignes du menu, dans l'ordre.</summary>
        public static readonly CharacterPart[] Parts =
        {
            CharacterPart.Skin, CharacterPart.HairStyle, CharacterPart.HairColor, CharacterPart.Hat,
            CharacterPart.Top, CharacterPart.TopColor, CharacterPart.Bottom,
        };

        public static string Label(CharacterPart part)
        {
            switch (part)
            {
                case CharacterPart.Skin: return "Peau";
                case CharacterPart.HairStyle: return "Coiffure";
                case CharacterPart.HairColor: return "Cheveux";
                case CharacterPart.Hat: return "Chapeau";
                case CharacterPart.Top: return "Haut";
                case CharacterPart.TopColor: return "Couleur du haut";
                default: return "Short";
            }
        }

        public static string[] Names(CharacterPart part)
        {
            switch (part)
            {
                case CharacterPart.Skin: return SkinNames;
                case CharacterPart.HairStyle: return HairStyles;
                case CharacterPart.HairColor: return HairColorNames;
                case CharacterPart.Hat: return Hats;
                case CharacterPart.Top: return Tops;
                case CharacterPart.TopColor: return TopColorNames;
                default: return BottomNames;
            }
        }

        public static int Count(CharacterPart part) => Names(part).Length;

        public static string ValueName(CharacterLook look, CharacterPart part) => Names(part)[Clamp(look.Get(part), part)];

        public static int Clamp(int value, CharacterPart part) => Mathf.Clamp(value, 0, Count(part) - 1);

        /// <summary>Choix suivant (+1) ou précédent (-1), en boucle.</summary>
        public static void Cycle(CharacterLook look, CharacterPart part, int delta)
        {
            int count = Count(part);
            look.Set(part, ((look.Get(part) + delta) % count + count) % count);
        }

        /// <summary>Copie avec chaque choix remis dans les bornes (sauvegarde ancienne ou modifiée).</summary>
        public static CharacterLook Sanitize(CharacterLook look)
        {
            CharacterLook result = look != null ? look.Clone() : new CharacterLook();
            foreach (CharacterPart part in Parts)
            {
                result.Set(part, Clamp(result.Get(part), part));
            }

            result.Name = CleanName(result.Name);
            return result;
        }

        /// <summary>Apparence au hasard (le prénom est gardé).</summary>
        public static void Randomize(CharacterLook look, System.Random random)
        {
            foreach (CharacterPart part in Parts)
            {
                look.Set(part, random.Next(Count(part)));
            }
        }

        /// <summary>Peut-on taper ce caractère dans le prénom ?</summary>
        public static bool IsNameChar(char c) => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '\'' || c == '.';

        /// <summary>Prénom sans espaces autour ni caractère interdit, limité à <see cref="MaxNameLength"/> caractères.</summary>
        public static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var chars = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (IsNameChar(c))
                {
                    chars.Append(c);
                }
            }

            string clean = chars.ToString().Trim();
            return clean.Length > MaxNameLength ? clean.Substring(0, MaxNameLength).TrimEnd() : clean;
        }

        /// <summary>Ajoute un caractère tapé au prénom (ignoré s'il est interdit ou si le prénom est complet).</summary>
        public static string Type(string name, char c)
        {
            name = name ?? string.Empty;
            if (!IsNameChar(c) || name.Length >= MaxNameLength || (c == ' ' && (name.Length == 0 || name.EndsWith(" "))))
            {
                return name;
            }

            return name + c;
        }

        /// <summary>Efface le dernier caractère du prénom.</summary>
        public static string Backspace(string name) => string.IsNullOrEmpty(name) ? string.Empty : name.Substring(0, name.Length - 1);

        /// <summary>Prénom affiché : celui choisi, sinon « Pêcheur ».</summary>
        public static string DisplayName(CharacterLook look)
        {
            string name = look != null ? CleanName(look.Name) : string.Empty;
            return name.Length > 0 ? name : DefaultName;
        }
    }
}
