using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Villagers;
using FishAndChill.World;
using UnityEngine;

namespace FishAndChill.UI
{
    /// <summary>
    /// Les quêtes du journal, rangées en trois sortes, seulement celles qu'on a récupérées : principales (les légendes, dès
    /// leur premier fragment), secondaires (Chiyo, dès qu'on l'a rencontrée ; d'autres viendront), quotidiennes (les
    /// commandes des habitants, dès qu'on les a écoutées ; elles changent chaque jour).
    /// </summary>
    public static class QuestCatalog
    {
        public static List<QuestEntry> Main(PlayerProgress progress, FishingController fishing)
        {
            var list = new List<QuestEntry>();
            if (progress == null || fishing == null) return list;
            foreach (LegendFish legend in fishing.Legends)
            {
                if (legend == null) continue;
                LegendProgress legends = progress.Legends;
                LegendState state = legends.State(legend.Id);
                int total = legend.FragmentCount;
                int found = state == LegendState.Collecting ? legends.Fragments(legend.Id) : total;
                int placed = state == LegendState.Collecting ? legends.Placed(legend.Id) : total;
                if (found == 0) continue;   // pas encore récupérée : aucun fragment
                bool awake = state == LegendState.Summoned || state == LegendState.Beaten;
                bool beaten = state == LegendState.Beaten;
                var quest = new QuestEntry
                {
                    Title = legend.ArtifactName,
                    Where = legend.Zone != null ? legend.Zone.DisplayName : string.Empty,
                    Picture = legend.Silhouette,
                    PictureTint = beaten ? new Color(1f, 0.84f, 0.4f) : new Color(0.78f, 0.82f, 0.88f),
                    Done = beaten,
                    Note = beaten ? $"{legend.Title} : {legend.SizeCm:0} cm à ton palmarès." : awake ? "Il t'attend au remous légendaire : le point doré sur ta carte." : null,
                };
                quest.Step($"Trouver les fragments   {found} / {total}", found >= total)
                    .Step($"Les poser sur le socle du laboratoire   {placed} / {total}", placed >= total)
                    .Step("Réveiller le gardien", awake, placed >= total)
                    .Step($"Vaincre {legend.Title}", beaten, awake);
                list.Add(quest);
            }

            return list;
        }

        public static List<QuestEntry> Side(PlayerProgress progress, FishingController fishing)
        {
            var list = new List<QuestEntry>();
            if (progress == null) return list;
            bool met = progress.HasNet || VillagerMemory.HasMet("chiyo");
            if (!met) return list;

            PearlNecklace necklace = progress.Pearls;
            int threaded = necklace.ThreadedCount, found = 0;
            foreach (string zone in necklace.Found) found++;
            bool open = threaded >= PearlLore.Count;
            CaveBuilding cave = CaveBuilding.Current;
            int caveTotal = 0, caveFound = 0;
            if (cave != null && cave.Zone != null && fishing != null && fishing.CatchLog != null)
            {
                foreach (FishingZone.SpeciesEntry entry in cave.Zone.NetSpecies)
                {
                    if (entry.Species == null) continue;
                    caveTotal++;
                    if (fishing.CatchLog.IsDiscovered(entry.Species.Id)) caveFound++;
                }
            }

            bool jelly = cave != null && cave.ChiyoHere;
            Sprite portrait = null;
            foreach (Villager villager in Villager.Everyone)
            {
                if (villager.DisplayName == "Chiyo") portrait = villager.Portrait;
            }

            var chiyo = new QuestEntry
            {
                Title = "Les perles de Chiyo",
                Where = "Baie d'Izu, puis le monde entier",
                Picture = portrait,
                Done = jelly,
                Pearls = true,
                Note = jelly ? "Chiyo t'attendait dans la grotte." : found > 0 ? $"{found} perle{(found > 1 ? "s" : string.Empty)} à lui rapporter à Izu." : null,
            };
            chiyo.Step("Rencontrer Chiyo, la vieille ama d'Izu", true)
                .Step($"Retrouver ses perles   {threaded} / {PearlLore.Count}", open)
                .Step("Entrer dans la grotte d'Ha Long", caveFound > 0 || jelly, open)
                .Step($"Les bêtes de la grotte   {caveFound} / {caveTotal}", caveTotal > 0 && caveFound >= caveTotal, open)
                .Step("La méduse immortelle", jelly, open && caveFound >= caveTotal - 1);
            list.Add(chiyo);
            return list;
        }

        public static List<QuestEntry> Daily()
        {
            var list = new List<QuestEntry>();
            var villagers = new List<Villager>(Villager.Everyone);
            villagers.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
            foreach (Villager villager in villagers)
            {
                if (!villager.TakesOrders) continue;
                QuestStatus status = villager.Quest;
                if (status == QuestStatus.None || status == QuestStatus.NotHeard) continue;   // pas encore écoutée
                FishSpecies species = villager.RequestedSpecies;
                bool served = status == QuestStatus.ServedToday;
                var quest = new QuestEntry
                {
                    Title = $"La commande de {villager.DisplayName}",
                    Where = villager.IsHome ? "En ce moment : à la maison" : "En ce moment : en balade sur l'île",
                    Picture = species != null && species.MiniSprite != null ? species.MiniSprite : villager.Portrait,
                    Done = served,
                    Note = served ? "Servie aujourd'hui : une nouvelle commande demain matin."
                        : species != null ? $"Récompense : {RequestRules.Reward(species.BasePrice, species.Rarity)} pièces" : null,
                };
                if (species != null)
                {
                    string zone = villager.RequestZoneName;
                    quest.Step($"Pêcher : {species.CommonName}{(string.IsNullOrEmpty(zone) ? string.Empty : $" ({zone})")}", status == QuestStatus.Ready)
                        .Step(status == QuestStatus.KeepForAquarium ? $"D'abord mettre {species.Definite()} à l'aquarium" : (species.Feminine ? "La" : "Le") + " lui apporter", false, status != QuestStatus.Fishing);
                }
                else
                {
                    quest.Step("Commande servie", true);
                }

                list.Add(quest);
            }

            return list;
        }
    }
}
