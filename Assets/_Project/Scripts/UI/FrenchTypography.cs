using System.Text;

namespace FishAndChill.UI
{
    /// <summary>
    /// Typographie française des textes affichés : espace insécable avant « ! ? : ; » % » et après « « », pour qu'un
    /// point d'exclamation ou un guillemet ne parte jamais seul à la ligne suivante. Logique pure, testée.
    /// </summary>
    public static class FrenchTypography
    {
        public const char NoBreakSpace = ' ';

        private static bool NeedsSpaceBefore(char c) => c == '!' || c == '?' || c == ':' || c == ';' || c == '»' || c == '%';

        public static string Apply(string text)
        {
            if (string.IsNullOrEmpty(text) || !NeedsWork(text))
            {
                return text;
            }

            var result = new StringBuilder(text);
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != ' ') continue;
                bool before = i + 1 < result.Length && NeedsSpaceBefore(result[i + 1]) && i > 0 && result[i - 1] != ' ' && result[i - 1] != '\n';
                bool after = i > 0 && result[i - 1] == '«';
                if (before || after) result[i] = NoBreakSpace;
            }

            return result.ToString();
        }

        private static bool NeedsWork(string text)
        {
            for (int i = 0; i + 1 < text.Length; i++)
            {
                if ((text[i] == ' ' && NeedsSpaceBefore(text[i + 1])) || (text[i] == '«' && text[i + 1] == ' ')) return true;
            }

            return false;
        }
    }
}
