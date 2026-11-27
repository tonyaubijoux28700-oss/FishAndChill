using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.UI;
using FishAndChill.World;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// L'épuisette dans la scène : enregistre ses dessins (<see cref="TidePoolArt"/>) dans <c>Art/UI/Epuisette/</c>,
    /// pose le mini-jeu (<see cref="TidePoolGame"/>) sur l'objet « Partie » et donne la grappe de bulles aux coins à bulles.
    /// Appelé par la génération de la scène ; peut aussi se relancer seul sur la scène ouverte.
    /// </summary>
    public static class NetSetup
    {
        private const string Folder = "Assets/_Project/Art/UI/Epuisette";

        public static void Install(GameObject partie, FishingController controller, PlayerMovement movement, DayNightCycle cycle, UiSkin skin, WorldMap map)
        {
            PixelArt.EnsureFolder(Folder);
            Sprite pool = PixelArt.Save($"{Folder}/Flaque_Mahana.png", TidePoolArt.MahanaPool(), 16, Vector2.zero);
            var critter = new Sprite[3];
            for (int i = 0; i < critter.Length; i++)
            {
                critter[i] = PixelArt.Save($"{Folder}/Bestiole_{i}.png", TidePoolArt.Critter(i), 16, new Vector2(0.5f, 0.5f));
            }

            Sprite net = PixelArt.Save($"{Folder}/EpuisetteDansLeau.png", TidePoolArt.NetInWater(), 16, new Vector2(16f / 40f, 24f / 40f));
            PixelArt.Save($"{Folder}/Epuisette.png", TidePoolArt.Net(), 16, new Vector2(0.5f, 0.5f));   // icône (marchand, inventaire)

            var game = partie.GetComponent<TidePoolGame>();
            if (game == null) game = partie.AddComponent<TidePoolGame>();
            var so = new SerializedObject(game);
            so.FindProperty("_fishing").objectReferenceValue = controller;
            so.FindProperty("_player").objectReferenceValue = movement;
            so.FindProperty("_map").objectReferenceValue = map;
            so.FindProperty("_cycle").objectReferenceValue = cycle;
            so.FindProperty("_skin").objectReferenceValue = skin;
            so.FindProperty("_pool").objectReferenceValue = pool;
            so.FindProperty("_net").objectReferenceValue = net;
            SerializedProperty frames = so.FindProperty("_critter");
            frames.arraySize = critter.Length;
            for (int i = 0; i < critter.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = critter[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // Le carnet montre la bestiole sur sa page Épuisette, en attendant les vrais dessins.
            foreach (FishIndex index in Object.FindObjectsByType<FishIndex>(FindObjectsInactive.Include))
            {
                var indexSo = new SerializedObject(index);
                indexSo.FindProperty("_netCritter").objectReferenceValue = critter[0];
                indexSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // La grappe de bulles au-dessus de chaque coin, bien visible.
            var cluster = new Sprite[3];
            for (int i = 0; i < cluster.Length; i++)
            {
                cluster[i] = PixelArt.Save($"Assets/_Project/Art/Ambient/Bulles_{i}.png", TidePoolArt.Bubbles(i), VolumeCanvas.PixelsPerUnit, new Vector2(0.5f, 0.4f));
            }

            foreach (FishingSpots spots in Object.FindObjectsByType<FishingSpots>(FindObjectsInactive.Include))
            {
                var spotsSo = new SerializedObject(spots);
                SerializedProperty list = spotsSo.FindProperty("_cluster");
                list.arraySize = cluster.Length;
                for (int i = 0; i < cluster.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = cluster[i];
                spotsSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>Relance l'installation sur la scène ouverte (sans tout régénérer).</summary>
        [MenuItem("FishAndChill/Prototype/Installer l'épuisette")]
        public static void InstallInOpenScene()
        {
            GameObject partie = GameObject.Find("Partie");
            Install(partie, Object.FindAnyObjectByType<FishingController>(), Object.FindAnyObjectByType<PlayerMovement>(),
                Object.FindAnyObjectByType<DayNightCycle>(), UiSkinSetup.CreateOrUpdate(), Object.FindAnyObjectByType<WorldMap>());
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(partie.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(partie.scene);
        }
    }
}
