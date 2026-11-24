using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Menu d'essai des poissons de légende (FishAndChill → Légendes), utilisable seulement pendant une partie en Play
    /// mode : donner des fragments, réveiller le gardien, aller au laboratoire ou au remous, remettre à zéro.
    /// Agit sur la légende de la zone où l'on se trouve (sinon la première, Tuna) et sur la partie en cours (sauvegardée
    /// comme une vraie partie).
    /// </summary>
    public static class LegendDebugMenu
    {
        private const string Root = "FishAndChill/Légendes (test)/";

        private static FishingController Fishing => Object.FindAnyObjectByType<FishingController>();

        private static LegendFish FirstLegend(FishingController fishing)
            => fishing == null ? null : fishing.LegendOfZone != null ? fishing.LegendOfZone : fishing.Legends.Count > 0 ? fishing.Legends[0] : null;

        private static bool InGame() => EditorApplication.isPlaying && Save.SaveManager.InGame && Fishing != null && FirstLegend(Fishing) != null;

        [MenuItem(Root + "Donner un fragment")]
        private static void GiveFragment()
        {
            FishingController fishing = Fishing;
            LegendFish legend = FirstLegend(fishing);
            int count = fishing.Progress.Legends.AddFragment(legend.Id, legend.FragmentCount);
            Debug.Log(count > 0
                ? $"[FishAndChill] Fragment de « {legend.ArtifactName} » donné ({count} / {legend.FragmentCount})."
                : "[FishAndChill] Tous les fragments sont déjà trouvés.");
        }

        [MenuItem(Root + "Donner tous les fragments")]
        private static void GiveAllFragments()
        {
            FishingController fishing = Fishing;
            LegendFish legend = FirstLegend(fishing);
            while (fishing.Progress.Legends.AddFragment(legend.Id, legend.FragmentCount) > 0)
            {
            }

            Debug.Log($"[FishAndChill] Tous les fragments de « {legend.ArtifactName} » sont en poche : va les poser sur le socle du laboratoire.");
        }

        [MenuItem(Root + "Réveiller le gardien (tout poser d'un coup)")]
        private static void Summon()
        {
            FishingController fishing = Fishing;
            LegendFish legend = FirstLegend(fishing);
            LegendProgress legends = fishing.Progress.Legends;
            while (legends.AddFragment(legend.Id, legend.FragmentCount) > 0)
            {
            }

            while (legends.PlaceFragment(legend.Id, legend.FragmentCount) >= 0)
            {
            }

            legends.Summon(legend.Id);
            Debug.Log($"[FishAndChill] {legend.Title} : {legends.State(legend.Id)}. Son remous est dans « {(legend.Zone != null ? legend.Zone.DisplayName : "sa zone")} ».");
        }

        [MenuItem(Root + "Aller au laboratoire (devant le socle)")]
        private static void GoToLab()
        {
            var lab = Object.FindAnyObjectByType<LabBuilding>();
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            if (lab == null || player == null) return;
            if (!lab.IsPlayerInside) lab.Enter();
            player.Teleport(lab.PedestalFront, Vector2.up);
        }

        [MenuItem(Root + "Aller au remous légendaire")]
        private static void GoToEddy()
        {
            LegendSpot spot = null;
            foreach (LegendSpot s in Object.FindObjectsByType<LegendSpot>())
            {
                if (s.Active) spot = s;
            }

            var map = Object.FindAnyObjectByType<WorldMap>();
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            if (map == null || player == null) return;
            if (spot == null)
            {
                Debug.LogWarning("[FishAndChill] Pas de remous ici : réveille d'abord le gardien, ou va dans la zone de sa légende.");
                return;
            }

            if (InteriorBuilding.Occupied != null) InteriorBuilding.Occupied.Exit();
            // La rive choisie par le remous : celle qu'on rejoint à pied depuis le ponton.
            player.Teleport(spot.Shore, spot.ShoreFacing);
            Object.FindAnyObjectByType<CameraFollow>()?.SnapToTarget();
            Debug.Log("[FishAndChill] Face au remous : appuie sur Espace pour lancer.");
        }

        [MenuItem(Root + "Remettre la légende à zéro")]
        private static void ResetLegend()
        {
            FishingController fishing = Fishing;
            LegendFish legend = FirstLegend(fishing);
            fishing.Progress.Legends.Reset(legend.Id);
            Debug.Log($"[FishAndChill] {legend.Title} remis à zéro (aucun fragment).");
        }

        [MenuItem(Root + "Donner un fragment", true)]
        [MenuItem(Root + "Donner tous les fragments", true)]
        [MenuItem(Root + "Réveiller le gardien (tout poser d'un coup)", true)]
        [MenuItem(Root + "Aller au laboratoire (devant le socle)", true)]
        [MenuItem(Root + "Aller au remous légendaire", true)]
        [MenuItem(Root + "Remettre la légende à zéro", true)]
        private static bool Validate() => InGame();
    }
}
