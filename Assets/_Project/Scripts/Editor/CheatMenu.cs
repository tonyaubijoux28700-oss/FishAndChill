using FishAndChill.Fishing;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Triches d'essai (FishAndChill → Triche (test)), utilisables seulement pendant une partie en Play mode, comme le menu
    /// des légendes. Ce qu'elles donnent est sauvegardé comme le vrai. Raccourcis : Ctrl + Alt + M (+1000 pièces), Ctrl + Alt + P
    /// (collier de Chiyo complet).
    /// </summary>
    public static class CheatMenu
    {
        private const string Root = "FishAndChill/Triche (test)/";
        private const int Coins = 1000;

        private static FishingController Fishing => Object.FindAnyObjectByType<FishingController>();

        private static bool InGame() => EditorApplication.isPlaying && Save.SaveManager.InGame && Fishing != null && Fishing.Progress != null;

        [MenuItem(Root + "+1000 pièces %&m")]
        private static void GiveCoins()
        {
            var wallet = Fishing.Progress.Wallet;
            wallet.Add(Coins); // déclenche Changed : l'en-tête et la sauvegarde suivent
            Debug.Log($"[FishAndChill] +{Coins} pièces (total : {wallet.Coins}).");
        }

        [MenuItem(Root + "+1000 pièces %&m", true)]
        private static bool CanGiveCoins() => InGame() && Fishing.Progress.Wallet != null;

        /// <summary>L'épuisette et les 9 perles enfilées sur le collier de Chiyo : la grotte d'Ha Long s'ouvre.</summary>
        [MenuItem(Root + "Collier de Chiyo complet %&p")]
        private static void GiveNecklace()
        {
            var progress = Fishing.Progress;
            progress.ReceiveNet();
            progress.Pearls.Restore(null, Collection.PearlLore.Zones);   // déclenche Changed : la sauvegarde suit
            Debug.Log("[FishAndChill] Collier de Chiyo complet : 9 perles enfilées, la grotte d'Ha Long est ouverte.");
        }

        [MenuItem(Root + "Collier de Chiyo complet %&p", true)]
        private static bool CanGiveNecklace() => InGame();
    }
}
