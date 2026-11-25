using System;
using System.Collections.Generic;
using FishAndChill.Collection;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Bouteille à la mer : de temps en temps, une prise n'est pas un poisson mais une bouteille avec un message et
    /// quelques pièces. Le message est souvent un indice sur une espèce de la zone pas encore pêchée (sans la nommer),
    /// sinon un petit mot d'un pêcheur inconnu. Logique pure, testée.
    /// </summary>
    public static class BottleMessage
    {
        /// <summary>Chance qu'une prise réussie soit une bouteille.</summary>
        public const float Chance = 0.05f;

        public const int MinCoins = 10;
        public const int MaxCoins = 40;

        private static readonly string[] Signatures =
        {
            "un vieux pêcheur", "Capitaine M.", "une gardienne de phare", "quelqu'un qui aime les poissons",
            "le cuisinier d'un cargo", "une biologiste marine",
        };

        private static readonly string[] Notes =
        {
            "Si tu lis ceci, c'est que la mer t'a choisi. Bonne pêche !",
            "J'ai perdu ma canne ici il y a trente ans. Si tu la trouves, garde-la.",
            "Les poissons ne ferment jamais les yeux : ils n'ont pas de paupières !",
            "Le plus gros poisson que j'aie jamais vu… s'est échappé. Évidemment.",
            "Recette du bonheur : un peu de patience et beaucoup de mie de pain.",
            "Conseil d'ami : vends tes doublons, mais garde toujours tes records.",
            "Certains poissons changent de couleur la nuit pour se cacher en dormant.",
            "Il y a plus d'espèces de poissons que de mammifères, d'oiseaux et de reptiles réunis.",
            "Un jour, j'ai attrapé une botte. Puis la deuxième. Il me manque le pêcheur.",
            "Les vrais trésors ne brillent pas : ils frétillent.",
        };

        /// <summary>Faut-il remplacer cette prise par une bouteille ?</summary>
        public static bool Roll(Random random) => random.NextDouble() < Chance;

        public static int Coins(Random random) => MinCoins + random.Next(0, MaxCoins - MinCoins + 1);

        /// <summary>
        /// Le message de la bouteille. <paramref name="favoriteBait"/> donne le nom de l'appât préféré d'une espèce
        /// (ou null). Deux fois sur trois, s'il reste des espèces à découvrir dans la zone, c'est un indice sur l'une d'elles.
        /// </summary>
        public static string Write(IList<FishSpecies> zoneSpecies, CatchLog log, Func<FishSpecies, string> favoriteBait, Random random)
        {
            var unknown = new List<FishSpecies>();
            if (zoneSpecies != null)
            {
                foreach (FishSpecies species in zoneSpecies)
                {
                    if (species != null && (log == null || !log.IsDiscovered(species.Id))) unknown.Add(species);
                }
            }

            string signature = Signatures[random.Next(Signatures.Length)];
            if (unknown.Count > 0 && random.NextDouble() < 0.67)
            {
                return $"« {Hint(unknown[random.Next(unknown.Count)], favoriteBait)} »\n— {signature}";
            }

            return $"« {Notes[random.Next(Notes.Length)]} »\n— {signature}";
        }

        /// <summary>Indice sur une espèce, sans la nommer : rareté, heure, taille et appât préféré.</summary>
        public static string Hint(FishSpecies species, Func<FishSpecies, string> favoriteBait)
        {
            string what = species.Kind == CreatureKind.Fish ? "un poisson" : "une créature";
            string rarity = species.Rarity.DisplayName().ToLowerInvariant();
            string time = species.ActiveTime == ActiveTime.Night ? " qui ne sort que la nuit"
                : species.ActiveTime == ActiveTime.Day ? " qui ne mord que le jour" : string.Empty;
            string text = $"Dans ces eaux vit {what} {rarity}{time}, jusqu'à {species.SizeRangeCm.y:0} cm.";
            string bait = favoriteBait != null ? favoriteBait(species) : null;
            if (!string.IsNullOrEmpty(bait))
            {
                string pronoun = species.Kind == CreatureKind.Fish ? "il" : "elle";
                text += $" On dit qu'{pronoun} ne résiste pas aux {bait.ToLowerInvariant()}.";
            }

            return text;
        }
    }
}
