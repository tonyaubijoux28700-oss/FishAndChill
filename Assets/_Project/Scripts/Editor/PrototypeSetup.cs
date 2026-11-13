using System.Globalization;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Génère les données et la scène du prototype de pêche.
    /// Relançable sans risque : les assets existants sont mis à jour, pas dupliqués.
    /// </summary>
    public static class PrototypeSetup
    {
        private const string Root = "Assets/_Project";
        private const string SpeciesFolder = Root + "/Data/Species";
        private const string ZonesFolder = Root + "/Data/Zones";
        private const string ScenesFolder = Root + "/Scenes";
        private const string ScenePath = ScenesFolder + "/FishingPrototype.unity";
        private const string TemplateScenePath = "Assets/Scenes/SampleScene.unity";

        private readonly struct SpeciesData
        {
            public readonly string Id;
            public readonly string CommonName;
            public readonly string ScientificName;
            public readonly Rarity Rarity;
            public readonly float Speed;
            public readonly float ZoneWidth;
            public readonly int Hits;
            public readonly int Misses;
            public readonly Vector2 SizeCm;
            public readonly int Price;
            public readonly Color Color;
            public readonly float Weight;

            public SpeciesData(string id, string commonName, string scientificName, Rarity rarity,
                float speed, float zoneWidth, int hits, int misses, Vector2 sizeCm, int price, Color color, float weight)
            {
                Id = id;
                CommonName = commonName;
                ScientificName = scientificName;
                Rarity = rarity;
                Speed = speed;
                ZoneWidth = zoneWidth;
                Hits = hits;
                Misses = misses;
                SizeCm = sizeCm;
                Price = price;
                Color = color;
                Weight = weight;
            }
        }

        // Espèces réellement présentes dans le lac d'Annecy.
        private static readonly SpeciesData[] AnnecySpecies =
        {
            new SpeciesData("rutilus-rutilus", "Gardon", "Rutilus rutilus", Rarity.Common,
                0.8f, 0.30f, 2, 2, new Vector2(10f, 30f), 5, new Color(0.75f, 0.78f, 0.80f), 40f),
            new SpeciesData("perca-fluviatilis", "Perche commune", "Perca fluviatilis", Rarity.Common,
                1.0f, 0.25f, 3, 2, new Vector2(15f, 40f), 8, new Color(0.55f, 0.65f, 0.30f), 30f),
            new SpeciesData("salmo-trutta", "Truite fario", "Salmo trutta", Rarity.Uncommon,
                1.3f, 0.20f, 3, 2, new Vector2(25f, 60f), 20, new Color(0.65f, 0.50f, 0.35f), 15f),
            new SpeciesData("esox-lucius", "Brochet", "Esox lucius", Rarity.Rare,
                1.5f, 0.16f, 4, 2, new Vector2(45f, 120f), 45, new Color(0.35f, 0.45f, 0.25f), 10f),
            new SpeciesData("salvelinus-umbla", "Omble chevalier", "Salvelinus umbla", Rarity.Epic,
                1.8f, 0.12f, 4, 1, new Vector2(25f, 70f), 90, new Color(0.85f, 0.45f, 0.30f), 5f),
        };

        [MenuItem("FishAndChill/Prototype/Générer données et scène de pêche")]
        public static void GenerateAll()
        {
            string zonePath = GenerateData();

            AssetDatabase.SaveAssets();
            GenerateScene(zonePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FishAndChill] Prototype prêt : {ScenePath} ({AnnecySpecies.Length} espèces).");
        }

        private static string GenerateData()
        {
            EnsureFolder(SpeciesFolder);
            EnsureFolder(ZonesFolder);

            var species = new FishSpecies[AnnecySpecies.Length];
            for (int i = 0; i < AnnecySpecies.Length; i++)
            {
                species[i] = CreateOrUpdateSpecies(AnnecySpecies[i]);
            }

            string zonePath = ZonesFolder + "/LacAnnecy.asset";
            var zone = LoadOrCreate<FishingZone>(zonePath);
            var so = new SerializedObject(zone);
            so.FindProperty("_displayName").stringValue = "Lac d'Annecy (zone de test)";
            so.FindProperty("_description").stringValue = "Lac alpin aux eaux claires, en Haute-Savoie.";
            so.FindProperty("_waterColor").colorValue = new Color(0.16f, 0.48f, 0.58f);

            SerializedProperty entries = so.FindProperty("_species");
            entries.arraySize = species.Length;
            for (int i = 0; i < species.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_species").objectReferenceValue = species[i];
                entry.FindPropertyRelative("_weight").floatValue = AnnecySpecies[i].Weight;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone);
            return zonePath;
        }

        private static FishSpecies CreateOrUpdateSpecies(SpeciesData data)
        {
            string fileName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(data.Id.Replace('-', ' ')).Replace(" ", string.Empty);
            var asset = LoadOrCreate<FishSpecies>($"{SpeciesFolder}/{fileName}.asset");

            var so = new SerializedObject(asset);
            so.FindProperty("_id").stringValue = data.Id;
            so.FindProperty("_commonName").stringValue = data.CommonName;
            so.FindProperty("_scientificName").stringValue = data.ScientificName;
            so.FindProperty("_rarity").enumValueIndex = (int)data.Rarity;
            so.FindProperty("_difficulty._cursorSpeed").floatValue = data.Speed;
            so.FindProperty("_difficulty._zoneWidth").floatValue = data.ZoneWidth;
            so.FindProperty("_difficulty._requiredHits").intValue = data.Hits;
            so.FindProperty("_difficulty._maxMisses").intValue = data.Misses;
            so.FindProperty("_sizeRangeCm").vector2Value = data.SizeCm;
            so.FindProperty("_basePrice").intValue = data.Price;
            so.FindProperty("_placeholderColor").colorValue = data.Color;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void GenerateScene(string zonePath)
        {
            EnsureFolder(ScenesFolder);

            // On part de la scène du modèle 2D : caméra et lumière globale 2D déjà réglées pour l'URP.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                AssetDatabase.CopyAsset(TemplateScenePath, ScenePath);
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Charger la zone APRÈS l'ouverture : ouvrir une scène décharge les assets non référencés.
            var zone = AssetDatabase.LoadAssetAtPath<FishingZone>(zonePath);

            GameObject root = GameObject.Find("Pêche (prototype)");
            if (root == null)
            {
                root = new GameObject("Pêche (prototype)");
            }

            // Pas d'opérateur ?? avec les objets Unity : un composant absent n'est pas un vrai null.
            var controller = root.GetComponent<FishingController>();
            if (controller == null)
            {
                controller = root.AddComponent<FishingController>();
            }

            if (root.GetComponent<FishingPrototypeView>() == null)
            {
                root.AddComponent<FishingPrototypeView>();
            }

            var so = new SerializedObject(controller);
            so.FindProperty("_zone").objectReferenceValue = zone;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (zone == null)
            {
                Debug.LogError("[FishAndChill] Zone de test introuvable : la scène n'a pas de zone assignée.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
