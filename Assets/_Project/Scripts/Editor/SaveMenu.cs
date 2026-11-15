using System.IO;
using FishAndChill.Save;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>Menus pour la sauvegarde : l'effacer (repartir de zéro) ou ouvrir son dossier.</summary>
    public static class SaveMenu
    {
        [MenuItem("FishAndChill/Sauvegarde/Effacer la sauvegarde (nouvelle partie)")]
        public static void Delete()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[FishAndChill] Quitte le Play mode avant d'effacer la sauvegarde.");
                return;
            }

            if (File.Exists(SaveManager.SavePath))
            {
                File.Delete(SaveManager.SavePath);
                Debug.Log("[FishAndChill] Sauvegarde effacée : la prochaine partie repart de zéro.");
            }
            else
            {
                Debug.Log("[FishAndChill] Pas de sauvegarde à effacer.");
            }
        }

        [MenuItem("FishAndChill/Sauvegarde/Ouvrir le dossier de la sauvegarde")]
        public static void Reveal()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
    }
}
