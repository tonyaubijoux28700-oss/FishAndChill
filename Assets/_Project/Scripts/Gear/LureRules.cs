using System.Collections.Generic;
using FishAndChill.Collection;

namespace FishAndChill.Gear
{
    /// <summary>
    /// Qui mord à quoi (logique pure, testée). Chaque espèce a ses types de leurres et ses hameçons, d'après la vraie
    /// pêche (colonnes Appats et Hamecons de la table). La règle suit la rareté : les communes mordent à tout ; les peu
    /// communes et les rares exigent un de leurs types de leurre ; les épiques et les légendaires exigent en plus un de
    /// leurs hameçons. Le bon leurre attire aussi l'espèce trois fois plus.
    /// </summary>
    public static class LureRules
    {
        /// <summary>Multiplicateur de fréquence quand le leurre accroché est un de ceux de l'espèce.</summary>
        public const float RightLureBonus = 3f;

        /// <summary>L'espèce peut-elle mordre avec ce leurre et cet hameçon ?</summary>
        public static bool Allows(Rarity rarity, LureType lure, HookType hook, IReadOnlyCollection<LureType> lures, IReadOnlyCollection<HookType> hooks)
        {
            if (rarity <= Rarity.Common) return true;
            bool lureOk = lures == null || lures.Count == 0 || Contains(lures, lure);
            if (!lureOk) return false;
            if (rarity < Rarity.Epic) return true;
            return hooks == null || hooks.Count == 0 || Contains(hooks, hook);
        }

        /// <summary>Ce qu'il faut pour cette rareté, en clair : rien, le leurre, ou le leurre et l'hameçon.</summary>
        public static bool NeedsLure(Rarity rarity) => rarity > Rarity.Common;

        public static bool NeedsHook(Rarity rarity) => rarity >= Rarity.Epic;

        private static bool Contains<T>(IReadOnlyCollection<T> list, T value)
        {
            foreach (T item in list)
            {
                if (EqualityComparer<T>.Default.Equals(item, value)) return true;
            }

            return false;
        }

        /// <summary>Mot de la table des espèces → type de leurre (null si inconnu). Accepte les anciens noms d'appâts.</summary>
        public static LureType? ParseLure(string word)
        {
            switch (SpeciesTable.Normalize(word))
            {
                case "naturel": case "ver": case "mie": case "crevette": case "calmar": return LureType.Natural;
                case "cuillere": case "tournante": return LureType.Spinner;
                case "ondulante": return LureType.Spoon;
                case "nageur": case "leurre": return LureType.Minnow;
                case "souple": return LureType.SoftBait;
                case "surface": return LureType.Surface;
                case "jig": case "lumineux": return LureType.Jig;
                case "turlutte": return LureType.SquidJig;
                case "traine": return LureType.Troll;
                case "mouche": return LureType.Fly;
                default: return null;
            }
        }

        /// <summary>Mot de la table des espèces → hameçon (null si inconnu).</summary>
        public static HookType? ParseHook(string word)
        {
            switch (SpeciesTable.Normalize(word))
            {
                case "aberdeen": case "simple": return HookType.Aberdeen;
                case "triple": return HookType.Treble;
                case "circulaire": return HookType.Circle;
                case "forge": return HookType.Forged;
                case "texan": case "offset": return HookType.Offset;
                case "mouche": return HookType.Fly;
                default: return null;
            }
        }

        /// <summary>Nom d'un type de leurre, pour le carnet et le marchand (au pluriel).</summary>
        public static string Name(LureType type)
        {
            switch (type)
            {
                case LureType.Natural: return "appâts naturels";
                case LureType.Spinner: return "cuillères tournantes";
                case LureType.Spoon: return "ondulantes";
                case LureType.Minnow: return "poissons nageurs";
                case LureType.SoftBait: return "leurres souples";
                case LureType.Surface: return "leurres de surface";
                case LureType.Jig: return "jigs métalliques";
                case LureType.SquidJig: return "turluttes";
                case LureType.Troll: return "leurres de traîne";
                default: return "mouches";
            }
        }

        public static string Name(HookType type)
        {
            switch (type)
            {
                case HookType.Aberdeen: return "Aberdeen";
                case HookType.Treble: return "triple";
                case HookType.Circle: return "circulaire";
                case HookType.Forged: return "O'Shaughnessy";
                case HookType.Offset: return "texan";
                default: return "à mouche";
            }
        }

        /// <summary>
        /// Ancien appât d'une sauvegarde → appât d'aujourd'hui (mêmes usages) ; identifiant inchangé s'il existe toujours.
        /// </summary>
        public static string ConvertOldBait(string id)
        {
            switch (id)
            {
                case "MieDePain": return "VerDeTerreau";
                case "VerDeTerre": return "VerDeTerreau";
                case "Crevette": return "CrevetteGrise";
                case "Calmar": return "LaniereCalmar";
                case "LeurreBrillant": return "PaletteAglia";
                case "AppatLumineux": return "TurluttePhospho";
                default: return id;
            }
        }
    }
}
