using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using FishAndChill.Fishing;
using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Lit la table des espèces, un fichier CSV qu'on ouvre dans un tableur : une espèce par ligne, colonnes séparées par
    /// des points-virgules, première ligne = noms des colonnes (dans n'importe quel ordre). Les lignes vides et celles
    /// qui commencent par # sont ignorées ; un texte qui contient un ; se met entre guillemets.
    /// Les valeurs s'écrivent en français (« rare », « nuit », « lourd », « crustace »…), sans souci des accents ni des
    /// majuscules. Vitesse, largeur de zone, réussites, ratés, prix et fréquence vides = valeurs par défaut de la rareté.
    /// Logique pure, testée.
    /// </summary>
    public static class SpeciesTable
    {
        public const char Separator = ';';

        /// <summary>Colonnes, dans l'ordre où l'export les écrit.</summary>
        public static readonly string[] Columns =
        {
            "Id", "Nom", "Scientifique", "Zone", "Rarete", "Nature", "Forme", "TailleMin", "TailleMax", "Prix", "Frequence",
            "Vitesse", "LargeurZone", "Reussites", "Rates", "Combat", "Heure", "Meteo", "Appats", "Description", "Couleur",
            "Dessin", "Dos", "Flancs", "Ventre", "Nageoires", "Motif", "CouleurMotif", "Queue", "Oeil", "Extras", "Hamecons",
        };

        /// <summary>Le résultat de la lecture : les espèces valides et les erreurs (numéro de ligne et explication).</summary>
        public sealed class Result
        {
            public readonly List<SpeciesRow> Rows = new List<SpeciesRow>();
            public readonly List<string> Errors = new List<string>();
        }

        // ---------- Valeurs par défaut selon la rareté ----------

        public static float DefaultSpeed(Rarity r) => new[] { 0.85f, 1.15f, 1.5f, 1.75f, 2.2f }[(int)r];
        public static float DefaultZoneWidth(Rarity r) => new[] { 0.28f, 0.22f, 0.17f, 0.13f, 0.11f }[(int)r];
        public static int DefaultHits(Rarity r) => new[] { 2, 3, 4, 4, 5 }[(int)r];
        public static int DefaultMisses(Rarity r) => new[] { 2, 2, 2, 1, 1 }[(int)r];
        public static int DefaultPrice(Rarity r) => new[] { 6, 18, 45, 100, 250 }[(int)r];
        public static float DefaultWeight(Rarity r) => new[] { 28f, 13f, 7f, 3f, 2f }[(int)r];

        // ---------- Lecture ----------

        public static Result Parse(string text)
        {
            var result = new Result();
            if (string.IsNullOrEmpty(text))
            {
                return result;
            }

            List<List<string>> lines = SplitLines(text.TrimStart('﻿'));
            Dictionary<string, int> header = null;
            int lineNumber = 0;
            foreach (List<string> cells in lines)
            {
                lineNumber++;
                if (cells.Count == 0 || (cells.Count == 1 && cells[0].Trim().Length == 0) || cells[0].TrimStart().StartsWith("#"))
                {
                    continue;
                }

                if (header == null)
                {
                    header = new Dictionary<string, int>();
                    for (int i = 0; i < cells.Count; i++)
                    {
                        header[Normalize(cells[i])] = i;
                    }

                    continue;
                }

                try
                {
                    SpeciesRow row = ReadRow(cells, header);
                    if (string.IsNullOrEmpty(row.Id))
                    {
                        continue;
                    }

                    result.Rows.Add(row);
                }
                catch (FormatException error)
                {
                    result.Errors.Add($"ligne {lineNumber} : {error.Message}");
                }
            }

            return result;
        }

        private static SpeciesRow ReadRow(List<string> cells, Dictionary<string, int> header)
        {
            string Cell(string column)
            {
                return header.TryGetValue(Normalize(column), out int i) && i < cells.Count ? cells[i].Trim() : string.Empty;
            }

            var row = new SpeciesRow
            {
                Id = Cell("Id"),
                CommonName = Cell("Nom"),
                ScientificName = Cell("Scientifique"),
                Zone = Cell("Zone"),
                Description = Cell("Description"),
                Drawing = Normalize(Cell("Dessin")),
                Pattern = Normalize(Cell("Motif")),
                Tail = Normalize(Cell("Queue")),
            };
            if (string.IsNullOrEmpty(row.Id))
            {
                return row;
            }

            row.Rarity = ParseRarity(Cell("Rarete"), row.Id);
            row.Kind = ParseKind(Cell("Nature"), row.Id);
            row.Shape = ParseShape(Cell("Forme"), row.Kind, row.Id);
            row.Fight = ParseFight(Cell("Combat"), row.Id);
            row.Time = ParseTime(Cell("Heure"), row.Id);
            row.Weather = ParseWeather(Cell("Meteo"), row.Id);
            row.SizeCm = new Vector2(Float(Cell("TailleMin"), 10f, "TailleMin", row.Id), Float(Cell("TailleMax"), 30f, "TailleMax", row.Id));
            if (row.SizeCm.y < row.SizeCm.x)
            {
                throw new FormatException($"{row.Id} : TailleMax plus petite que TailleMin");
            }

            row.Price = Int(Cell("Prix"), DefaultPrice(row.Rarity), "Prix", row.Id);
            row.Weight = Float(Cell("Frequence"), DefaultWeight(row.Rarity), "Frequence", row.Id);
            row.Speed = Float(Cell("Vitesse"), DefaultSpeed(row.Rarity), "Vitesse", row.Id);
            row.ZoneWidth = Float(Cell("LargeurZone"), DefaultZoneWidth(row.Rarity), "LargeurZone", row.Id);
            row.Hits = Int(Cell("Reussites"), DefaultHits(row.Rarity), "Reussites", row.Id);
            row.Misses = Int(Cell("Rates"), DefaultMisses(row.Rarity), "Rates", row.Id);
            row.Baits = List(Cell("Appats"));
            row.Hooks = List(Cell("Hamecons"));
            row.Extras = List(Cell("Extras"));

            row.Color = ParseColor(Cell("Couleur"), new Color(0.6f, 0.6f, 0.6f), row.Id);
            row.Back = ParseColor(Cell("Dos"), Color.Lerp(row.Color, Color.black, 0.45f), row.Id);
            row.Flank = ParseColor(Cell("Flancs"), row.Color, row.Id);
            row.Belly = ParseColor(Cell("Ventre"), Color.Lerp(row.Color, Color.white, 0.6f), row.Id);
            row.Fin = ParseColor(Cell("Nageoires"), Color.Lerp(row.Flank, row.Back, 0.5f), row.Id);
            row.PatternColor = ParseColor(Cell("CouleurMotif"), Color.Lerp(row.Back, Color.black, 0.5f), row.Id);
            row.Eye = ParseColor(Cell("Oeil"), new Color(0.86f, 0.76f, 0.40f), row.Id);
            return row;
        }

        // ---------- Valeurs en français ----------

        /// <summary>Minuscules, sans accents, sans espaces ni tirets : « Peu commune » → « peucommune ».</summary>
        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (char c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark || c == ' ' || c == '-' || c == '_' || c == '\'')
                {
                    continue;
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        private static T Pick<T>(string value, T fallback, string column, string id, params (string word, T result)[] words)
        {
            string key = Normalize(value);
            if (key.Length == 0)
            {
                return fallback;
            }

            foreach ((string word, T result) in words)
            {
                if (key == word) return result;
            }

            throw new FormatException($"{id} : « {value} » inconnu dans la colonne {column}");
        }

        private static Rarity ParseRarity(string v, string id) => Pick(v, Rarity.Common, "Rarete", id,
            ("commune", Rarity.Common), ("commun", Rarity.Common), ("common", Rarity.Common),
            ("peucommune", Rarity.Uncommon), ("peucommun", Rarity.Uncommon), ("uncommon", Rarity.Uncommon),
            ("rare", Rarity.Rare), ("epique", Rarity.Epic), ("epic", Rarity.Epic),
            ("legendaire", Rarity.Legendary), ("legendary", Rarity.Legendary));

        private static CreatureKind ParseKind(string v, string id) => Pick(v, CreatureKind.Fish, "Nature", id,
            ("poisson", CreatureKind.Fish), ("fish", CreatureKind.Fish), ("crustace", CreatureKind.Crustacean),
            ("crustacean", CreatureKind.Crustacean), ("reptile", CreatureKind.Reptile), ("amphibien", CreatureKind.Amphibian),
            ("amphibian", CreatureKind.Amphibian));

        private static BodyShape ParseShape(string v, CreatureKind kind, string id) => Pick(v, kind == CreatureKind.Fish ? BodyShape.Fish : BodyShape.Crab, "Forme", id,
            ("poisson", BodyShape.Fish), ("fish", BodyShape.Fish), ("crabe", BodyShape.Crab), ("crab", BodyShape.Crab),
            ("langouste", BodyShape.Lobster), ("lobster", BodyShape.Lobster), ("tortue", BodyShape.Turtle), ("turtle", BodyShape.Turtle),
            ("grenouille", BodyShape.Frog), ("frog", BodyShape.Frog));

        private static FightStyle ParseFight(string v, string id) => Pick(v, FightStyle.Calm, "Combat", id,
            ("calme", FightStyle.Calm), ("calm", FightStyle.Calm), ("nerveux", FightStyle.Nervous), ("nervous", FightStyle.Nervous),
            ("fuyant", FightStyle.Drifting), ("drifting", FightStyle.Drifting), ("rush", FightStyle.Rusher), ("rushs", FightStyle.Rusher),
            ("rusher", FightStyle.Rusher), ("lourd", FightStyle.Heavy), ("heavy", FightStyle.Heavy));

        private static ActiveTime ParseTime(string v, string id) => Pick(v, ActiveTime.Always, "Heure", id,
            ("toujours", ActiveTime.Always), ("always", ActiveTime.Always), ("jour", ActiveTime.Day), ("day", ActiveTime.Day),
            ("nuit", ActiveTime.Night), ("night", ActiveTime.Night));

        private static WeatherNeed ParseWeather(string v, string id) => Pick(v, WeatherNeed.Any, "Meteo", id,
            ("toustemps", WeatherNeed.Any), ("toujours", WeatherNeed.Any), ("any", WeatherNeed.Any), ("pluie", WeatherNeed.Rain),
            ("rain", WeatherNeed.Rain), ("orage", WeatherNeed.Storm), ("storm", WeatherNeed.Storm), ("brouillard", WeatherNeed.Fog),
            ("fog", WeatherNeed.Fog), ("beau", WeatherNeed.Clear), ("beautemps", WeatherNeed.Clear), ("clear", WeatherNeed.Clear));

        private static float Float(string v, float fallback, string column, string id)
        {
            if (v.Length == 0) return fallback;
            if (float.TryParse(v.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) return f;
            throw new FormatException($"{id} : « {v} » n'est pas un nombre (colonne {column})");
        }

        private static int Int(string v, int fallback, string column, string id)
        {
            return v.Length == 0 ? fallback : Mathf.RoundToInt(Float(v, fallback, column, id));
        }

        private static Color ParseColor(string v, Color fallback, string id)
        {
            if (v.Length == 0) return fallback;
            if (ColorUtility.TryParseHtmlString(v.StartsWith("#") ? v : "#" + v, out Color c)) return c;
            throw new FormatException($"{id} : « {v} » n'est pas une couleur (écrire #RRVVBB)");
        }

        private static string[] List(string v)
        {
            var items = new List<string>();
            foreach (string part in v.Split(','))
            {
                if (part.Trim().Length > 0) items.Add(part.Trim());
            }

            return items.ToArray();
        }

        // ---------- Écriture (export) ----------

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        /// <summary>Une cellule prête à écrire : entre guillemets si elle contient un ; ou des guillemets.</summary>
        public static string Quote(string value)
        {
            value = value ?? string.Empty;
            return value.IndexOf(Separator) >= 0 || value.IndexOf('"') >= 0 || value.IndexOf('\n') >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        // ---------- Découpage CSV ----------

        private static List<List<string>> SplitLines(string text)
        {
            var lines = new List<List<string>>();
            var cells = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else if (c == '"') quoted = false;
                    else cell.Append(c);
                    continue;
                }

                if (c == '"') quoted = true;
                else if (c == Separator) { cells.Add(cell.ToString()); cell.Clear(); }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    cells.Add(cell.ToString());
                    cell.Clear();
                    lines.Add(cells);
                    cells = new List<string>();
                }
                else cell.Append(c);
            }

            if (cell.Length > 0 || cells.Count > 0)
            {
                cells.Add(cell.ToString());
                lines.Add(cells);
            }

            return lines;
        }
    }
}
