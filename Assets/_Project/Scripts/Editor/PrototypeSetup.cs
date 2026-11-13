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

        // Île Mahana (fictive, ambiance Polynésie) : vraies espèces de lagon et de récif de l'Indo-Pacifique,
        // pêchables depuis le rivage.
        private static readonly SpeciesData[] MahanaSpecies =
        {
            new SpeciesData("abudefduf-vaigiensis", "Sergent-major indo-pacifique", "Abudefduf vaigiensis", Rarity.Common,
                0.8f, 0.30f, 2, 2, new Vector2(10f, 20f), 4, new Color(0.85f, 0.85f, 0.45f), 30f),
            new SpeciesData("acanthurus-triostegus", "Chirurgien bagnard", "Acanthurus triostegus", Rarity.Common,
                0.9f, 0.28f, 2, 2, new Vector2(12f, 27f), 6, new Color(0.80f, 0.85f, 0.75f), 25f),
            new SpeciesData("scarus-psittacus", "Perroquet commun", "Scarus psittacus", Rarity.Uncommon,
                1.1f, 0.24f, 3, 2, new Vector2(18f, 30f), 14, new Color(0.35f, 0.75f, 0.70f), 15f),
            new SpeciesData("cephalopholis-argus", "Mérou paon", "Cephalopholis argus", Rarity.Uncommon,
                1.2f, 0.22f, 3, 2, new Vector2(25f, 55f), 22, new Color(0.25f, 0.30f, 0.50f), 12f),
            new SpeciesData("caranx-melampygus", "Carangue à points bleus", "Caranx melampygus", Rarity.Rare,
                1.5f, 0.17f, 4, 2, new Vector2(40f, 100f), 45, new Color(0.40f, 0.60f, 0.85f), 8f),
            new SpeciesData("albula-glossodonta", "Banane de mer", "Albula glossodonta", Rarity.Rare,
                1.6f, 0.16f, 4, 2, new Vector2(40f, 90f), 50, new Color(0.85f, 0.88f, 0.92f), 5f),
            new SpeciesData("sphyraena-barracuda", "Grand barracuda", "Sphyraena barracuda", Rarity.Epic,
                1.9f, 0.13f, 4, 1, new Vector2(60f, 160f), 110, new Color(0.60f, 0.65f, 0.70f), 3f),
            new SpeciesData("caranx-ignobilis", "Carangue géante", "Caranx ignobilis", Rarity.Legendary,
                2.2f, 0.11f, 5, 1, new Vector2(60f, 170f), 250, new Color(0.30f, 0.32f, 0.38f), 2f),
        };

        private static readonly Color MahanaWater = new Color(0.20f, 0.68f, 0.75f);

        [MenuItem("FishAndChill/Prototype/Générer données et scène de pêche")]
        public static void GenerateAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[FishAndChill] Quitte le Play mode avant de régénérer le prototype.");
                return;
            }

            string zonePath = GenerateData();

            AssetDatabase.SaveAssets();
            GenerateScene(zonePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FishAndChill] Prototype prêt : {ScenePath} (Île Mahana, {MahanaSpecies.Length} espèces).");
        }

        /// <summary>Crée les zones et leurs espèces ; renvoie le chemin de la zone de départ (Île Mahana).</summary>
        private static string GenerateData()
        {
            EnsureFolder(SpeciesFolder);
            EnsureFolder(ZonesFolder);

            // Conservé pour plus tard : une zone d'eau douce réelle.
            CreateOrUpdateZone(ZonesFolder + "/LacAnnecy.asset", "Lac d'Annecy",
                "Lac alpin aux eaux claires, en Haute-Savoie.", new Color(0.16f, 0.48f, 0.58f), AnnecySpecies);

            string startPath = ZonesFolder + "/IleMahana.asset";
            CreateOrUpdateZone(startPath, "Île Mahana",
                "Île tropicale fictive : lagon turquoise, plages de sable blanc et cocotiers. Point de départ de l'aventure.",
                MahanaWater, MahanaSpecies);
            return startPath;
        }

        private static void CreateOrUpdateZone(string path, string displayName, string description, Color water, SpeciesData[] speciesData)
        {
            var species = new FishSpecies[speciesData.Length];
            for (int i = 0; i < speciesData.Length; i++)
            {
                species[i] = CreateOrUpdateSpecies(speciesData[i]);
            }

            var zone = LoadOrCreate<FishingZone>(path);
            var so = new SerializedObject(zone);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_waterColor").colorValue = water;

            SerializedProperty entries = so.FindProperty("_species");
            entries.arraySize = species.Length;
            for (int i = 0; i < species.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_species").objectReferenceValue = species[i];
                entry.FindPropertyRelative("_weight").floatValue = speciesData[i].Weight;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone);
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
            so.FindProperty("_sprite").objectReferenceValue = FishArt.Generate(data.Id, data.Color);
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

            var view = root.GetComponent<FishingPrototypeView>();
            if (view == null)
            {
                view = root.AddComponent<FishingPrototypeView>();
            }

            // Le pêcheur : un objet à part entière, déplaçable en ZQSD.
            GameObject player = GameObject.Find("Joueur");
            if (player == null)
            {
                player = new GameObject("Joueur");
                player.transform.position = new Vector3(0f, -3f, 0f);
                player.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            }

            if (player.GetComponent<SpriteRenderer>() == null)
            {
                player.AddComponent<SpriteRenderer>();
            }

            var movement = player.GetComponent<Player.PlayerMovement>();
            if (movement == null)
            {
                movement = player.AddComponent<Player.PlayerMovement>();
            }

            var movementSo = new SerializedObject(movement);
            movementSo.FindProperty("_fishing").objectReferenceValue = controller;
            movementSo.ApplyModifiedPropertiesWithoutUndo();

            // Apparence : pêcheur en pixel art 16×16 (1 case), animé selon sa direction.
            CharacterArt.Sprites sprites = CharacterArt.Generate();
            player.transform.localScale = Vector3.one;
            var renderer = player.GetComponent<SpriteRenderer>();
            renderer.sprite = sprites.Down[0];
            renderer.color = Color.white;
            renderer.sortingOrder = 10;

            var animator = player.GetComponent<Player.PlayerAnimator>();
            if (animator == null)
            {
                animator = player.AddComponent<Player.PlayerAnimator>();
            }

            var animatorSo = new SerializedObject(animator);
            animatorSo.FindProperty("_movement").objectReferenceValue = movement;
            SetSprites(animatorSo.FindProperty("_down"), sprites.Down);
            SetSprites(animatorSo.FindProperty("_up"), sprites.Up);
            SetSprites(animatorSo.FindProperty("_left"), sprites.Left);
            SetSprites(animatorSo.FindProperty("_right"), sprites.Right);
            animatorSo.ApplyModifiedPropertiesWithoutUndo();

            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_player").objectReferenceValue = player.transform;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(controller);
            so.FindProperty("_zone").objectReferenceValue = zone;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Progression du joueur : sac (3 places au départ), aquarium, pièces.
            GameObject partie = GameObject.Find("Partie");
            if (partie == null)
            {
                partie = new GameObject("Partie");
            }

            var progress = partie.GetComponent<PlayerProgress>();
            if (progress == null)
            {
                progress = partie.AddComponent<PlayerProgress>();
            }

            so = new SerializedObject(controller);
            so.FindProperty("_progress").objectReferenceValue = progress;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Le pêcheur est trié par ses pieds (pivot) pour passer devant / derrière les cocotiers et l'aquarium.
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;

            // L'île de départ, la boutique, la caméra qui suit et les branchements joueur / carte.
            ShopBuilding shop = MapSetup.BuildIsland(player, controller);

            // Interface du sac et des pièces (l'aquarium aura plus tard son propre bâtiment : pas de dépôt ici).
            var hud = partie.GetComponent<UI.InventoryHud>();
            if (hud == null)
            {
                hud = partie.AddComponent<UI.InventoryHud>();
            }

            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("_progress").objectReferenceValue = progress;
            hudSo.FindProperty("_fishing").objectReferenceValue = controller;
            hudSo.FindProperty("_station").objectReferenceValue = null;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // Le marchand de la boutique et son menu de vente.
            var shopMenu = partie.GetComponent<UI.ShopMenu>();
            if (shopMenu == null)
            {
                shopMenu = partie.AddComponent<UI.ShopMenu>();
            }

            var menuSo = new SerializedObject(shopMenu);
            menuSo.FindProperty("_progress").objectReferenceValue = progress;
            menuSo.FindProperty("_shop").objectReferenceValue = shop;
            menuSo.FindProperty("_player").objectReferenceValue = movement;
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            var viewShopSo = new SerializedObject(view);
            viewShopSo.FindProperty("_shop").objectReferenceValue = shop;
            viewShopSo.ApplyModifiedPropertiesWithoutUndo();
            if (zone == null)
            {
                Debug.LogError("[FishAndChill] Zone de test introuvable : la scène n'a pas de zone assignée.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void SetSprites(SerializedProperty array, Sprite[] sprites)
        {
            array.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
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
