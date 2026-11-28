using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Rubrique « Chiyo » du journal de quêtes : les étapes de sa quête, cochées au fur et à mesure (la rencontrer à Izu,
    /// retrouver ses 9 perles, entrer dans la grotte d'Ha Long, ses bêtes, la méduse immortelle). Les étapes pas encore
    /// atteintes restent des « ??? » ; sous les perles, la rangée des 9 perles (silhouette, à rapporter, enfilée).
    /// </summary>
    public sealed class ChiyoQuestPage
    {
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.66f, 0.72f, 0.78f);
        private static readonly Color Text = new Color(0.96f, 0.94f, 0.88f);

        private readonly PlayerProgress _progress;
        private readonly FishingController _fishing;
        private readonly List<Step> _steps = new List<Step>();
        private readonly List<Image> _pearls = new List<Image>();
        private readonly Text _header;

        private sealed class Step
        {
            public Image Gem;
            public Text Title;
            public Text Detail;
        }

        public ChiyoQuestPage(RectTransform page, UiSkin skin, Sprite card, PlayerProgress progress, FishingController fishing)
        {
            _progress = progress;
            _fishing = fishing;
            _header = UiFactory.Label(page, "Titre Chiyo", 32, TextAnchor.MiddleLeft, Gold);
            UiFactory.Bold(_header);
            UiFactory.Center(_header.rectTransform, new Vector2(-10f, 278f), new Vector2(1600f, 40f));

            for (int i = 0; i < 5; i++)
            {
                float y = 190f - i * 112f;
                Image back = UiFactory.Sliced(page, $"Étape {i + 1}", card, card != null ? Color.white : new Color(0.10f, 0.16f, 0.24f));
                UiFactory.Center(back.rectTransform, new Vector2(0f, y), new Vector2(1500f, 96f));
                var step = new Step();
                step.Gem = UiFactory.Plain(back.rectTransform, "Case", Color.white);
                step.Gem.sprite = skin != null ? skin.Gem : null;
                UiFactory.Center(step.Gem.rectTransform, new Vector2(-700f, 0f), new Vector2(40f, 40f));
                step.Title = UiFactory.Label(back.rectTransform, "Étape", 30, TextAnchor.MiddleLeft, Text);
                UiFactory.Bold(step.Title);
                UiFactory.Center(step.Title.rectTransform, new Vector2(-40f, 18f), new Vector2(1240f, 36f));
                step.Detail = UiFactory.Label(back.rectTransform, "Détail", 23, TextAnchor.MiddleLeft, Muted);
                UiFactory.Center(step.Detail.rectTransform, new Vector2(-40f, -20f), new Vector2(1240f, 32f));
                _steps.Add(step);
            }

            // Les 9 perles, à droite de l'étape des perles.
            for (int i = 0; i < PearlLore.Count; i++)
            {
                Image pearl = UiFactory.Plain(page, $"Perle {i + 1}", Color.white);
                pearl.sprite = PearlSprite.Get();
                UiFactory.Center(pearl.rectTransform, new Vector2(390f + i * 40f, 190f - 112f), new Vector2(34f, 34f));
                _pearls.Add(pearl);
            }
        }

        private void Set(int index, bool reached, bool done, string title, string detail)
        {
            Step step = _steps[index];
            step.Gem.color = done ? Gold : reached ? new Color(0.36f, 0.70f, 0.86f) : new Color(0.40f, 0.42f, 0.46f);
            step.Title.text = reached ? title : "???";
            step.Title.color = done ? Gold : reached ? Text : Muted;
            step.Detail.text = reached ? detail : "Une étape de plus t'attend, plus tard.";
        }

        public void Refresh()
        {
            if (_progress == null) return;
            bool met = _progress.HasNet || Villagers.VillagerMemory.HasMet("chiyo");
            PearlNecklace necklace = _progress.Pearls;
            int threaded = necklace.ThreadedCount;
            int found = 0;
            foreach (string zone in necklace.Found) found++;
            bool open = threaded >= PearlLore.Count;
            CaveBuilding cave = CaveBuilding.Current;
            int caveTotal = 0, caveFound = 0;
            if (cave != null && cave.Zone != null && _fishing != null && _fishing.CatchLog != null)
            {
                foreach (FishingZone.SpeciesEntry entry in cave.Zone.NetSpecies)
                {
                    if (entry.Species == null) continue;
                    caveTotal++;
                    if (_fishing.CatchLog.IsDiscovered(entry.Species.Id)) caveFound++;
                }
            }

            bool jelly = cave != null && cave.ChiyoHere;
            _header.text = jelly ? "La vieille ama de la baie d'Izu   •   Terminée" : "La vieille ama de la baie d'Izu";

            Set(0, true, met, "Rencontrer Chiyo",
                met ? "Elle t'a donné son épuisette et confié son carnet." : "Une vieille dame vit dans la première maison du hameau, baie d'Izu.");
            Set(1, met, open, $"Retrouver ses perles   {threaded} / {PearlLore.Count}",
                found > 0 ? $"{found} perle{(found > 1 ? "s" : string.Empty)} à lui rapporter à Izu."
                : open ? "Le collier est complet." : "Une perle par zone dont toutes les bestioles sont au carnet.");
            Set(2, open, caveFound > 0 || jelly, "Entrer dans la grotte d'Ha Long",
                "Au pied de la montagne, derrière les lanternes rouges.");
            Set(3, open, caveTotal > 0 && caveFound >= caveTotal, $"Les bêtes de la grotte   {caveFound} / {caveTotal}",
                "Une à la fois, et une seule prise par jour dans la flaque.");
            Set(4, open && caveFound >= caveTotal - 1, jelly, "La méduse immortelle",
                jelly ? "Chiyo t'attendait dans la grotte." : "La dernière, la plus ancienne.");

            for (int i = 0; i < _pearls.Count; i++)
            {
                string zone = PearlLore.Zones[i];
                PearlState state = necklace.StateOf(zone);
                Color color = PearlLore.ColorOf(zone);
                _pearls[i].gameObject.SetActive(met);
                _pearls[i].color = state == PearlState.Threaded ? color
                    : state == PearlState.Found ? new Color(color.r, color.g, color.b, 0.55f)
                    : new Color(0.12f, 0.14f, 0.30f, 0.6f);
            }
        }
    }
}
