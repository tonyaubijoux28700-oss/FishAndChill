namespace FishAndChill.Collection
{
    /// <summary>
    /// Accords des noms d'espèces dans les messages (« la perche commune », « ton anguille », « vendue ») d'après le genre
    /// de la table des espèces (colonne Genre). Le nom passe en minuscule au milieu d'une phrase. Logique pure, testée.
    /// </summary>
    public static class SpeciesGrammar
    {
        /// <summary>« l' » devant une voyelle ou un h muet (les h aspirés des poissons gardent « le » : le hareng, le homard).</summary>
        public static bool Elides(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string lower = name.ToLowerInvariant();
            if ("aàâeéèêiîoôuùûy".IndexOf(lower[0]) >= 0) return true;
            return lower.StartsWith("hippocampe") || lower.StartsWith("huître") || lower.StartsWith("huitre");
        }

        /// <summary>Le nom en minuscule au début (« Perche commune » → « perche commune » ; « Idole des Maures » garde Maures).</summary>
        public static string Lower(string name) => string.IsNullOrEmpty(name) ? string.Empty : char.ToLowerInvariant(name[0]) + name.Substring(1);

        /// <summary>« le brochet », « la perche », « l'anguille ».</summary>
        public static string Definite(string name, bool feminine) =>
            Elides(name) ? "l'" + Lower(name) : (feminine ? "la " : "le ") + Lower(name);

        /// <summary>« ton brochet », « ta perche », « ton anguille » (ton devant une voyelle, même au féminin).</summary>
        public static string Your(string name, bool feminine) =>
            (feminine && !Elides(name) ? "ta " : "ton ") + Lower(name);

        /// <summary>Accord d'un participe ou d'un adjectif : « vendu » / « vendue ».</summary>
        public static string Agree(string masculine, bool feminine) => feminine ? masculine + "e" : masculine;

        /// <summary>Pronom complément : « garde-le » / « garde-la ».</summary>
        public static string Pronoun(bool feminine) => feminine ? "la" : "le";

        public static string Definite(this FishSpecies species) => Definite(species.CommonName, species.Feminine);

        public static string Your(this FishSpecies species) => Your(species.CommonName, species.Feminine);
    }
}
