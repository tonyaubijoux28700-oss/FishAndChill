using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Le bouton Play lance toujours la scène du jeu, quelle que soit la scène ouverte dans l'éditeur
    /// (évite l'écran vide si une scène de test ou une scène sans nom est restée ouverte).
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartScene
    {
        private const string GameScenePath = "Assets/_Project/Scenes/FishingPrototype.unity";

        static PlayModeStartScene()
        {
            // Tout de suite, puis une fois la base d'assets prête, puis à chaque appui sur Play
            // (au cas où quelque chose aurait remis le réglage à zéro entre-temps).
            Apply();
            EditorApplication.delayCall += Apply;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode && EditorSceneManager.playModeStartScene == null)
                {
                    Apply();
                }
            };
        }

        [MenuItem("FishAndChill/Lancer toujours la scène du jeu")]
        public static void Apply()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath);
            if (scene != null)
            {
                EditorSceneManager.playModeStartScene = scene;
            }
        }
    }
}
