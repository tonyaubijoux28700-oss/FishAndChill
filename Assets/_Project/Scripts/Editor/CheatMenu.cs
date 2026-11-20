using FishAndChill.Fishing;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Triches d'essai (FishAndChill → Triche (test)), utilisables seulement pendant une partie en Play mode, comme le menu
    /// des légendes. Les pièces données sont sauvegardées comme de vraies pièces. Raccourci : Ctrl + Alt + M.
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
    }
}
