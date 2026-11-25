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
        private const string LakeZonePath = ZonesFolder + "/LacAnnecy.asset";
        private const string AmazonZonePath = ZonesFolder + "/FleuveAmazone.asset";
        private const string JapanZonePath = ZonesFolder + "/BaieJaponaise.asset";
        private const string ReefZonePath = ZonesFolder + "/GrandeBarriere.asset";
        private const string AtollZonePath = ZonesFolder + "/Maldives.asset";
        private const string SkeletonZonePath = ZonesFolder + "/CoteDesSquelettes.asset";
        private const string FjordZonePath = ZonesFolder + "/FjordsNorvege.asset";
        private const string ScenesFolder = Root + "/Scenes";
        private const string ScenePath = ScenesFolder + "/FishingPrototype.unity";
        private const string TemplateScenePath = "Assets/Scenes/SampleScene.unity";

        /// <summary>La table des espèces : une ligne par espèce (voir <see cref="SpeciesTable"/>).</summary>
        public const string SpeciesTablePath = Root + "/Data/Especes.csv";

        private static readonly Color MahanaWater = new Color(0.20f, 0.68f, 0.75f);

        [MenuItem("FishAndChill/Prototype/Générer données et scène de pêche")]
        public static void GenerateAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[FishAndChill] Quitte le Play mode avant de régénérer le prototype.");
                return;
            }

            string zonePath = GenerateData(out int speciesCount);

            AssetDatabase.SaveAssets();
            GenerateScene(zonePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FishAndChill] Prototype prêt : {ScenePath} ({speciesCount} espèces lues dans {SpeciesTablePath}).");
        }

        /// <summary>Lit la table des espèces ; les erreurs (valeur inconnue, nombre mal écrit…) sont signalées dans la console.</summary>
        public static System.Collections.Generic.List<SpeciesRow> ReadSpeciesTable()
        {
            SpeciesTable.Result table = SpeciesTable.Parse(System.IO.File.ReadAllText(SpeciesTablePath, System.Text.Encoding.UTF8));
            foreach (string error in table.Errors)
            {
                Debug.LogError($"[FishAndChill] Table des espèces, {error}");
            }

            return table.Rows;
        }

        /// <summary>Crée les zones et leurs espèces ; renvoie le chemin de la zone de départ (Île Mahana).</summary>
        private static string GenerateData(out int speciesCount)
        {
            EnsureFolder(SpeciesFolder);
            EnsureFolder(ZonesFolder);
            System.Collections.Generic.List<SpeciesRow> rows = ReadSpeciesTable();
            speciesCount = rows.Count;

            // Deuxième zone : un lac alpin d'eau douce bien réel.
            CreateOrUpdateZone(LakeZonePath, "Lac d'Annecy",
                "Lac alpin aux eaux très claires, entouré de prés et de forêts de sapins.", new Color(0.12f, 0.34f, 0.44f), rows,
                new Vector2(0.52f, 0.74f), "Haute-Savoie, France");

            // Troisième zone : un tronçon du fleuve Amazone, en pleine jungle.
            CreateOrUpdateZone(AmazonZonePath, "Fleuve Amazone",
                "Fleuve immense aux eaux brunes, bordé de jungle, de nénuphars géants et de dauphins roses.", new Color(0.46f, 0.36f, 0.20f), rows,
                new Vector2(0.35f, 0.44f), "Amazonas, Brésil");

            // Quatrième zone : une baie de la côte japonaise.
            CreateOrUpdateZone(JapanZonePath, "Baie d'Izu",
                "Baie de la côte japonaise : plage de sable gris, cerisiers en fleurs, pins tordus et torii dans l'eau.", new Color(0.10f, 0.36f, 0.56f), rows,
                new Vector2(0.85f, 0.67f), "Shizuoka, Japon");

            // Cinquième zone : la côte du Queensland, face à la Grande Barrière de corail.
            CreateOrUpdateZone(ReefZonePath, "Grande Barrière",
                "Côte du Queensland : sable blanc, eucalyptus, mangrove, et le plus grand récif de corail du monde au large.", new Color(0.10f, 0.40f, 0.66f), rows,
                new Vector2(0.88f, 0.34f), "Queensland, Australie");

            // Sixième zone : un atoll des Maldives, dans l'océan Indien.
            CreateOrUpdateZone(AtollZonePath, "Atoll des Maldives",
                "Anneau d'îlots de sable blanc autour d'un lagon turquoise, cocoteraies, bungalows sur pilotis et platier de corail.", new Color(0.08f, 0.34f, 0.66f), rows,
                new Vector2(0.69f, 0.47f), "Océan Indien, Maldives");

            // Septième zone : la Côte des Squelettes, en Namibie : brume froide, épaves et désert.
            CreateOrUpdateZone(SkeletonZonePath, "Côte des Squelettes",
                "Désert de Namibie qui plonge dans un Atlantique glacé : brume, épaves rouillées, ossements de baleines et otaries. On dit que la côte ne rend jamais ses noyés.", new Color(0.14f, 0.24f, 0.26f), rows,
                new Vector2(0.54f, 0.32f), "Namibie");

            // Huitième zone : les fjords de Norvège : eau sombre entre les falaises enneigées, village de pêcheurs.
            CreateOrUpdateZone(FjordZonePath, "Fjords de Norvège",
                "Bras de mer sombre entre des falaises enneigées, cascade, rorbuer rouges sur pilotis et séchoirs à morue. La nuit, le ciel s'allume d'aurores vertes.", new Color(0.08f, 0.20f, 0.26f), rows,
                new Vector2(0.53f, 0.86f), "Norvège");

            string startPath = ZonesFolder + "/IleMahana.asset";
            CreateOrUpdateZone(startPath, "Île Mahana",
                "Île tropicale fictive : lagon turquoise, plages de sable blanc et cocotiers. Point de départ de l'aventure.",
                MahanaWater, rows, new Vector2(0.12f, 0.36f), "Polynésie (île fictive)");

            // Climat de chaque zone, d'après le vrai : beau, nuageux, pluie, orage, brouillard.
            SetClimate(startPath, new WeatherClimate(50f, 20f, 18f, 12f, 0f));          // tropiques : averses et orages
            SetClimate(LakeZonePath, new WeatherClimate(35f, 25f, 20f, 5f, 15f));       // lac alpin : brumes du matin
            SetClimate(AmazonZonePath, new WeatherClimate(20f, 20f, 40f, 15f, 5f));     // forêt pluviale
            SetClimate(JapanZonePath, new WeatherClimate(35f, 25f, 20f, 5f, 15f));      // côte japonaise : pluie et brume de mer
            SetClimate(ReefZonePath, new WeatherClimate(55f, 20f, 15f, 10f, 0f));
            SetClimate(AtollZonePath, new WeatherClimate(60f, 20f, 15f, 5f, 0f));
            SetClimate(SkeletonZonePath, new WeatherClimate(10f, 30f, 5f, 10f, 45f));  // courant froid de Benguela : brume de mer presque tous les jours
            SetGloom(SkeletonZonePath, new Color(0.62f, 0.66f, 0.72f), 0.35f);
            SetClimate(FjordZonePath, new WeatherClimate(30f, 30f, 25f, 5f, 10f));      // côte norvégienne : pluie fine, ciel changeant
            SetGloom(FjordZonePath, new Color(0.84f, 0.90f, 1f), 0.05f);                // lumière froide et bleutée
            return startPath;
        }

        /// <summary>L'objet « Météo » : le temps qu'il fait et ses effets (dessins de Editor/WeatherArt → Art/Weather).</summary>
        private static WeatherSystem BuildWeather(Transform parent, DayNightCycle cycle, FishingController controller, Transform player)
        {
            Transform existing = parent.Find("Météo");
            GameObject go = existing != null ? existing.gameObject : new GameObject("Météo");
            go.transform.SetParent(parent, false);
            var system = go.GetComponent<WeatherSystem>();
            if (system == null) system = go.AddComponent<WeatherSystem>();
            var effects = go.GetComponent<WeatherEffects>();
            if (effects == null) effects = go.AddComponent<WeatherEffects>();
            var storm = go.GetComponent<LegendStorm>();
            if (storm == null) storm = go.AddComponent<LegendStorm>();

            const string folder = "Assets/_Project/Art/Weather";
            EnsureFolder(folder);
            var center = new Vector2(0.5f, 0.5f);
            int ppu = WeatherArt.PixelsPerUnit;
            Sprite drop = PixelArt.Save($"{folder}/Goutte.png", WeatherArt.Drop(), ppu, center);
            var splash = new Sprite[WeatherArt.SplashFrames];
            for (int i = 0; i < splash.Length; i++)
            {
                splash[i] = PixelArt.Save($"{folder}/Eclaboussure_{i}.png", WeatherArt.Splash(i), ppu, center);
            }

            Sprite fog = PixelArt.Save($"{folder}/Brume.png", WeatherArt.Fog(), ppu, center);
            var bolts = new Sprite[WeatherArt.BoltVariants];
            for (int i = 0; i < bolts.Length; i++)
            {
                bolts[i] = PixelArt.Save($"{folder}/Foudre_{i}.png", WeatherArt.Bolt(i), ppu, new Vector2(0.5f, 0f));   // pied = impact
            }

            var impact = new Sprite[WeatherArt.ImpactFrames];
            for (int i = 0; i < impact.Length; i++)
            {
                impact[i] = PixelArt.Save($"{folder}/Impact_{i}.png", WeatherArt.Impact(i), ppu, center);
            }

            var stormSo = new SerializedObject(storm);
            stormSo.FindProperty("_fishing").objectReferenceValue = controller;
            stormSo.FindProperty("_player").objectReferenceValue = player;
            stormSo.ApplyModifiedPropertiesWithoutUndo();

            var systemSo = new SerializedObject(system);
            systemSo.FindProperty("_cycle").objectReferenceValue = cycle;
            systemSo.FindProperty("_fishing").objectReferenceValue = controller;
            systemSo.FindProperty("_legendStorm").objectReferenceValue = storm;
            systemSo.ApplyModifiedPropertiesWithoutUndo();

            var effectsSo = new SerializedObject(effects);
            effectsSo.FindProperty("_weather").objectReferenceValue = system;
            effectsSo.FindProperty("_drop").objectReferenceValue = drop;
            effectsSo.FindProperty("_fog").objectReferenceValue = fog;
            SetSprites(effectsSo.FindProperty("_bolts"), bolts);
            SetSprites(effectsSo.FindProperty("_impact"), impact);
            effectsSo.FindProperty("_glowMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            effectsSo.FindProperty("_cameraFollow").objectReferenceValue = Object.FindAnyObjectByType<CameraFollow>();
            SetSprites(effectsSo.FindProperty("_splash"), splash);
            effectsSo.ApplyModifiedPropertiesWithoutUndo();
            return system;
        }

        private static void SetClimate(string zonePath, WeatherClimate climate)
        {
            var zone = AssetDatabase.LoadAssetAtPath<FishingZone>(zonePath);
            var so = new SerializedObject(zone);
            SerializedProperty c = so.FindProperty("_climate");
            c.FindPropertyRelative("_clear").floatValue = climate.Weight(Weather.Clear);
            c.FindPropertyRelative("_cloudy").floatValue = climate.Weight(Weather.Cloudy);
            c.FindPropertyRelative("_rain").floatValue = climate.Weight(Weather.Rain);
            c.FindPropertyRelative("_storm").floatValue = climate.Weight(Weather.Storm);
            c.FindPropertyRelative("_fog").floatValue = climate.Weight(Weather.Fog);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone);
        }

        /// <summary>Zone sombre : lumière grise multipliée au ciel et brume toujours présente.</summary>
        private static void SetGloom(string zonePath, Color ambient, float minimumFog)
        {
            var zone = AssetDatabase.LoadAssetAtPath<FishingZone>(zonePath);
            var so = new SerializedObject(zone);
            so.FindProperty("_ambient").colorValue = ambient;
            so.FindProperty("_minimumFog").floatValue = minimumFog;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone);
        }

        /// <summary>Une zone et ses espèces : les lignes de la table dont la colonne Zone porte le nom de l'asset.</summary>
        private static void CreateOrUpdateZone(string path, string displayName, string description, Color water,
            System.Collections.Generic.List<SpeciesRow> allRows, Vector2 mapPosition, string region)
        {
            string zoneName = System.IO.Path.GetFileNameWithoutExtension(path);
            var rows = allRows.FindAll(r => r.Zone == zoneName);
            var species = new FishSpecies[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                species[i] = CreateOrUpdateSpecies(rows[i]);
            }

            var zone = LoadOrCreate<FishingZone>(path);
            var so = new SerializedObject(zone);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_waterColor").colorValue = water;
            so.FindProperty("_mapPosition").vector2Value = mapPosition;
            so.FindProperty("_region").stringValue = region;

            SerializedProperty entries = so.FindProperty("_species");
            entries.arraySize = species.Length;
            for (int i = 0; i < species.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_species").objectReferenceValue = species[i];
                entry.FindPropertyRelative("_weight").floatValue = rows[i].Weight;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone);
        }

        private static FishSpecies CreateOrUpdateSpecies(SpeciesRow data)
        {
            string fileName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(data.Id.Replace('-', ' ')).Replace(" ", string.Empty);
            var asset = LoadOrCreate<FishSpecies>($"{SpeciesFolder}/{fileName}.asset");

            var so = new SerializedObject(asset);
            so.FindProperty("_id").stringValue = data.Id;
            so.FindProperty("_commonName").stringValue = data.CommonName;
            so.FindProperty("_scientificName").stringValue = data.ScientificName;
            so.FindProperty("_rarity").enumValueIndex = (int)data.Rarity;
            so.FindProperty("_kind").enumValueIndex = (int)data.Kind;
            so.FindProperty("_bodyShape").enumValueIndex = (int)data.Shape;
            so.FindProperty("_difficulty._cursorSpeed").floatValue = data.Speed;
            so.FindProperty("_difficulty._zoneWidth").floatValue = data.ZoneWidth;
            so.FindProperty("_difficulty._requiredHits").intValue = data.Hits;
            so.FindProperty("_difficulty._maxMisses").intValue = data.Misses;
            so.FindProperty("_sizeRangeCm").vector2Value = data.SizeCm;
            so.FindProperty("_basePrice").intValue = data.Price;
            so.FindProperty("_placeholderColor").colorValue = data.Color;
            so.FindProperty("_description").stringValue = data.Description;
            so.FindProperty("_fightStyle").enumValueIndex = (int)data.Fight;
            so.FindProperty("_activeTime").enumValueIndex = (int)data.Time;
            so.FindProperty("_weather").enumValueIndex = (int)data.Weather;
            so.FindProperty("_sprite").objectReferenceValue = FishArt.Generate(data.Id, data.Color, data);
            Sprite[] miniFrames = FishArt.GenerateMiniFrames(data.Id, (data.SizeCm.x + data.SizeCm.y) * 0.5f, data.Kind.LivesOnBottom());
            so.FindProperty("_miniSprite").objectReferenceValue = miniFrames[0];
            // Leurres et hameçons de l'espèce (colonnes Appats et Hamecons de la table).
            SerializedProperty lureList = so.FindProperty("_lures");
            var lures = new System.Collections.Generic.List<int>();
            foreach (string word in data.Baits) { Gear.LureType? lure = Gear.LureRules.ParseLure(word); if (lure.HasValue && !lures.Contains((int)lure.Value)) lures.Add((int)lure.Value); }
            lureList.arraySize = lures.Count;
            for (int i = 0; i < lures.Count; i++) lureList.GetArrayElementAtIndex(i).enumValueIndex = lures[i];
            SerializedProperty hookList = so.FindProperty("_hooks");
            var hooks = new System.Collections.Generic.List<int>();
            foreach (string word in data.Hooks) { Gear.HookType? hook = Gear.LureRules.ParseHook(word); if (hook.HasValue && !hooks.Contains((int)hook.Value)) hooks.Add((int)hook.Value); }
            hookList.arraySize = hooks.Count;
            for (int i = 0; i < hooks.Count; i++) hookList.GetArrayElementAtIndex(i).enumValueIndex = hooks[i];
            SerializedProperty frameList = so.FindProperty("_miniFrames");
            frameList.arraySize = miniFrames.Length;
            for (int i = 0; i < miniFrames.Length; i++) frameList.GetArrayElementAtIndex(i).objectReferenceValue = miniFrames[i];
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

            // Le matériel vendu par le marchand (canne, hameçon, appât, sac).
            var progressSo = new SerializedObject(progress);
            progressSo.FindProperty("_gearCatalog").objectReferenceValue = GearSetup.CreateOrUpdate();
            progressSo.FindProperty("_baitCatalog").objectReferenceValue = BaitSetup.CreateOrUpdate();
            progressSo.FindProperty("_petCatalog").objectReferenceValue = PetSetup.CreateOrUpdate();
            progressSo.FindProperty("_trap").objectReferenceValue = GearSetup.CreateTrap();
            progressSo.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(controller);
            so.FindProperty("_progress").objectReferenceValue = progress;
            // Poissons de légende : leurs fragments d'artefact se pêchent dans leur zone.
            LegendFish[] legends = LegendSetup.CreateOrUpdate();
            SerializedProperty legendList = so.FindProperty("_legends");
            legendList.arraySize = legends.Length;
            for (int i = 0; i < legends.Length; i++)
            {
                legendList.GetArrayElementAtIndex(i).objectReferenceValue = legends[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // Le pêcheur est trié par ses pieds (pivot) pour passer devant / derrière les cocotiers et l'aquarium.
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;

            // L'île de départ, la boutique, l'aquarium, la caméra qui suit et les branchements joueur / carte.
            MapSetup.Buildings buildings = MapSetup.BuildIsland(player, controller);
            ShopBuilding shop = buildings.Shop;
            AquariumBuilding aquarium = buildings.Aquarium;

            // Chaque remous légendaire attend la légende de sa zone (même région que la zone de la légende).
            foreach (LegendSpot legendSpot in Object.FindObjectsByType<LegendSpot>())
            {
                var spotSo = new SerializedObject(legendSpot);
                spotSo.FindProperty("_progress").objectReferenceValue = progress;
                LegendFish match = null;
                foreach (LegendFish legend in legends)
                {
                    if (legend != null && legend.Zone != null && RegionOf(legend.Zone.name) == legendSpot.Region) match = legend;
                }

                spotSo.FindProperty("_legend").objectReferenceValue = match;
                spotSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Salle des spécimens : une légende par tube, dans l'ordre des légendes, et sa bête redressée tête en haut.
            foreach (SpecimenTube tube in Object.FindObjectsByType<SpecimenTube>(FindObjectsInactive.Include))
            {
                var tubeSo = new SerializedObject(tube);
                tubeSo.FindProperty("_progress").objectReferenceValue = progress;
                LegendFish legend = tube.Slot < legends.Length ? legends[tube.Slot] : null;
                tubeSo.FindProperty("_legend").objectReferenceValue = legend;
                var specimen = (SpriteRenderer)tubeSo.FindProperty("_specimen").objectReferenceValue;
                if (specimen != null)
                {
                    Sprite sprite = null;
                    if (legend != null && legend.Art != null)
                    {
                        Color[,] art = PixelArt.Load(AssetDatabase.GetAssetPath(legend.Art));
                        sprite = PixelArt.Save($"Assets/_Project/Art/Buildings/Laboratoire/Specimen_{legend.Id}.png", SpecimenLabArt.Specimen(art), VolumeCanvas.PixelsPerUnit, Vector2.one * 0.5f);
                    }

                    specimen.sprite = sprite;
                    EditorUtility.SetDirty(specimen);
                }

                tubeSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // La scientifique du laboratoire parle des fragments trouvés.
            var labSo = new SerializedObject(buildings.Lab);
            labSo.FindProperty("_progress").objectReferenceValue = progress;
            labSo.FindProperty("_fishing").objectReferenceValue = controller;
            labSo.ApplyModifiedPropertiesWithoutUndo();

            // L'aquarium dépose le sac de la partie, et ses poissons nagent dans le bassin.
            var aquariumSo = new SerializedObject(aquarium);
            aquariumSo.FindProperty("_progress").objectReferenceValue = progress;
            aquariumSo.ApplyModifiedPropertiesWithoutUndo();
            // Les deux bassins (lagon, lac) et les visiteurs suivent la collection du joueur.
            foreach (AquariumTank tank in Object.FindObjectsByType<AquariumTank>())
            {
                var tankSo = new SerializedObject(tank);
                tankSo.FindProperty("_progress").objectReferenceValue = progress;
                tankSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var visitors = Object.FindAnyObjectByType<AquariumVisitors>();
            if (visitors != null)
            {
                var visitorsSo = new SerializedObject(visitors);
                visitorsSo.FindProperty("_progress").objectReferenceValue = progress;
                visitorsSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Interface du sac et des pièces.
            var hud = partie.GetComponent<UI.InventoryHud>();
            if (hud == null)
            {
                hud = partie.AddComponent<UI.InventoryHud>();
            }


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
            // La vitrine : fond éclairé, socle à coussin, râtelier des cannes et flèche (dessins en volume, affichés ×3).
            const string vitrine = "Assets/_Project/Art/UI/Vitrine";
            menuSo.FindProperty("_showcaseBack").objectReferenceValue = PixelArt.Save($"{vitrine}/Fond.png", ShopDisplayArt.Back(), 32, new Vector2(0.5f, 0.5f));
            menuSo.FindProperty("_pedestal").objectReferenceValue = PixelArt.Save($"{vitrine}/Socle.png", ShopDisplayArt.Pedestal(), 32, new Vector2(0.5f, 0f));
            menuSo.FindProperty("_rodStand").objectReferenceValue = PixelArt.Save($"{vitrine}/Ratelier.png", ShopDisplayArt.RodStand(), 32, new Vector2(0.5f, 0f));
            menuSo.FindProperty("_arrow").objectReferenceValue = PixelArt.Save($"{vitrine}/Fleche.png", ShopDisplayArt.Arrow(), 32, new Vector2(0.5f, 0.5f));
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            // Le sac : icône en bas à gauche, fenêtre ouverte avec E (sauf devant le comptoir du marchand).
            Sprite bagIcon = PixelArt.Save("Assets/_Project/Art/UI/IconeSac.png", UiArt.BagIcon(), 16, new Vector2(0.5f, 0.5f));
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("_progress").objectReferenceValue = progress;
            hudSo.FindProperty("_fishing").objectReferenceValue = controller;
            hudSo.FindProperty("_player").objectReferenceValue = movement;
            hudSo.FindProperty("_shopMenu").objectReferenceValue = shopMenu;
            hudSo.FindProperty("_bagIcon").objectReferenceValue = bagIcon;

            // Charte pixel art de l'interface (cadres, bandeaux, fonds d'eau), partagée par sac, carte et boutique.
            UI.UiSkin skin = UiSkinSetup.CreateOrUpdate();
            hudSo.FindProperty("_skin").objectReferenceValue = skin;
            // Vitrine du matériel : fond en bois (×4), supports en laiton, étiquettes en papier (celles du carnet).
            hudSo.FindProperty("_showcaseBack").objectReferenceValue = PixelArt.Save("Assets/_Project/Art/UI/Materiel/Vitrine_Fond.png", GearArt.ShowcaseBack(), 25, new Vector2(0.5f, 0.5f));
            hudSo.FindProperty("_peg").objectReferenceValue = PixelArt.Save("Assets/_Project/Art/UI/Materiel/Vitrine_Support.png", GearArt.Peg(), 16, new Vector2(0.5f, 0.5f));
            hudSo.FindProperty("_petPaw").objectReferenceValue = PixelArt.Save("Assets/_Project/Art/UI/Materiel/Patte_Animal.png", PetShopArt.PawIcon(), 16, new Vector2(0.5f, 0.5f));
            hudSo.FindProperty("_tag").objectReferenceValue = PixelArt.Save("Assets/_Project/Art/UI/Carnet/Case.png", BookArt.Cell(), UiSkinArt.PixelsPerUnit, new Vector2(0.5f, 0.5f), new Vector4(5, 5, 5, 5), FilterMode.Point);
            Sprite Round(string file, Color[,] pixels) => PixelArt.Save($"Assets/_Project/Art/UI/Materiel/{file}.png", pixels, UiSkinArt.PixelsPerUnit, new Vector2(0.5f, 0.5f), new Vector4(9, 9, 9, 9), FilterMode.Point);
            hudSo.FindProperty("_roundTag").objectReferenceValue = Round("Cadre_Etiquette", BookArt.RoundedFrame(new Color(0.95f, 0.89f, 0.74f), new Color(0.42f, 0.28f, 0.16f), new Color(1f, 0.97f, 0.88f)));
            hudSo.FindProperty("_roundCard").objectReferenceValue = Round("Cadre_Case", BookArt.RoundedFrame(new Color(0.10f, 0.15f, 0.22f), new Color(0.40f, 0.52f, 0.64f), new Color(0.18f, 0.26f, 0.36f)));
            hudSo.FindProperty("_roundRim").objectReferenceValue = Round("Cadre_Lisere", BookArt.RoundedFrame(new Color(0f, 0f, 0f, 0f), new Color(0.86f, 0.68f, 0.30f), new Color(1f, 0.90f, 0.60f)));
            var skinMenuSo = new SerializedObject(shopMenu);
            skinMenuSo.FindProperty("_skin").objectReferenceValue = skin;
            skinMenuSo.ApplyModifiedPropertiesWithoutUndo();
            var skinViewSo = new SerializedObject(view);
            skinViewSo.FindProperty("_skin").objectReferenceValue = skin;
            skinViewSo.ApplyModifiedPropertiesWithoutUndo();

            // Étincelles et bandeau quand une espèce rare, épique ou légendaire mord.
            var rareBite = view.GetComponent<RareBiteEffect>();
            if (rareBite == null)
            {
                rareBite = view.gameObject.AddComponent<RareBiteEffect>();
            }

            var rareSo = new SerializedObject(rareBite);
            rareSo.FindProperty("_skin").objectReferenceValue = skin;
            rareSo.ApplyModifiedPropertiesWithoutUndo();
            var minimap = Object.FindAnyObjectByType<UI.Minimap>();
            if (minimap != null)
            {
                var skinMapSo = new SerializedObject(minimap);
                skinMapSo.FindProperty("_skin").objectReferenceValue = skin;
                skinMapSo.ApplyModifiedPropertiesWithoutUndo();
            }
            // L'index des poissons : icône de livre à côté du sac, livre ouvert avec Tab.
            var fishIndex = partie.GetComponent<UI.FishIndex>();
            if (fishIndex == null)
            {
                fishIndex = partie.AddComponent<UI.FishIndex>();
            }

            Sprite bookIcon = PixelArt.Save("Assets/_Project/Art/UI/IconeIndex.png", UiArt.BookIcon(), 16, new Vector2(0.5f, 0.5f));
            var indexSo = new SerializedObject(fishIndex);
            var lakeZone = AssetDatabase.LoadAssetAtPath<FishingZone>(LakeZonePath);
            SerializedProperty zones = indexSo.FindProperty("_zones");
            var amazonZone = AssetDatabase.LoadAssetAtPath<FishingZone>(AmazonZonePath);
            var japanZone = AssetDatabase.LoadAssetAtPath<FishingZone>(JapanZonePath);
            var reefZone = AssetDatabase.LoadAssetAtPath<FishingZone>(ReefZonePath);
            var atollZone = AssetDatabase.LoadAssetAtPath<FishingZone>(AtollZonePath);
            var skeletonZone = AssetDatabase.LoadAssetAtPath<FishingZone>(SkeletonZonePath);
            var fjordZone = AssetDatabase.LoadAssetAtPath<FishingZone>(FjordZonePath);
            zones.arraySize = 8;
            zones.GetArrayElementAtIndex(7).objectReferenceValue = fjordZone;
            zones.GetArrayElementAtIndex(6).objectReferenceValue = skeletonZone;
            zones.GetArrayElementAtIndex(5).objectReferenceValue = atollZone;
            zones.GetArrayElementAtIndex(0).objectReferenceValue = zone;
            zones.GetArrayElementAtIndex(1).objectReferenceValue = lakeZone;
            zones.GetArrayElementAtIndex(2).objectReferenceValue = amazonZone;
            zones.GetArrayElementAtIndex(3).objectReferenceValue = japanZone;
            zones.GetArrayElementAtIndex(4).objectReferenceValue = reefZone;
            indexSo.FindProperty("_fishing").objectReferenceValue = controller;
            indexSo.FindProperty("_player").objectReferenceValue = movement;
            indexSo.FindProperty("_bookIcon").objectReferenceValue = bookIcon;
            indexSo.FindProperty("_skin").objectReferenceValue = skin;
            indexSo.FindProperty("_progress").objectReferenceValue = progress;

            // Le carnet du pêcheur : couverture, pages, cases, onglets, pli, tampon et icônes.
            const string bookFolder = "Assets/_Project/Art/UI/Carnet";
            int bookPpu = UiSkinArt.PixelsPerUnit;
            Sprite BookSprite(string file, Color[,] pixels, Vector4 border) =>
                PixelArt.Save($"{bookFolder}/{file}.png", pixels, bookPpu, new Vector2(0.5f, 0.5f), border, FilterMode.Point);
            indexSo.FindProperty("_cover").objectReferenceValue = BookSprite("Couverture", BookArt.Cover(), new Vector4(12, 12, 12, 12));
            indexSo.FindProperty("_page").objectReferenceValue = BookSprite("Page", BookArt.Page(), new Vector4(8, 8, 8, 8));
            indexSo.FindProperty("_cell").objectReferenceValue = BookSprite("Case", BookArt.Cell(), new Vector4(5, 5, 5, 5));
            indexSo.FindProperty("_ribbon").objectReferenceValue = BookSprite("Onglet", BookArt.Ribbon(), new Vector4(6, 6, 6, 6));
            indexSo.FindProperty("_spine").objectReferenceValue = BookSprite("Pli", BookArt.Spine(), Vector4.zero);
            indexSo.FindProperty("_stamp").objectReferenceValue = BookSprite("Tampon", BookArt.Stamp(), Vector4.zero);
            indexSo.FindProperty("_sun").objectReferenceValue = BookSprite("Soleil", BookArt.Sun(), Vector4.zero);
            indexSo.FindProperty("_moon").objectReferenceValue = BookSprite("Lune", BookArt.Moon(), Vector4.zero);
            indexSo.FindProperty("_coin").objectReferenceValue = BookSprite("Piece", BookArt.Coin(), Vector4.zero);
            indexSo.ApplyModifiedPropertiesWithoutUndo();

            hudSo.FindProperty("_aquarium").objectReferenceValue = aquarium;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // Sauvegarde automatique : chargée au démarrage, réécrite après chaque changement.
            var saveManager = partie.GetComponent<Save.SaveManager>();
            if (saveManager == null)
            {
                saveManager = partie.AddComponent<Save.SaveManager>();
            }

            var saveSo = new SerializedObject(saveManager);
            saveSo.FindProperty("_progress").objectReferenceValue = progress;
            saveSo.FindProperty("_fishing").objectReferenceValue = controller;
            SerializedProperty saveZones = saveSo.FindProperty("_zones");
            saveZones.arraySize = 8;
            saveZones.GetArrayElementAtIndex(7).objectReferenceValue = fjordZone;
            saveZones.GetArrayElementAtIndex(6).objectReferenceValue = skeletonZone;
            saveZones.GetArrayElementAtIndex(5).objectReferenceValue = atollZone;
            saveZones.GetArrayElementAtIndex(0).objectReferenceValue = zone;
            saveZones.GetArrayElementAtIndex(1).objectReferenceValue = lakeZone;
            saveZones.GetArrayElementAtIndex(2).objectReferenceValue = amazonZone;
            saveZones.GetArrayElementAtIndex(3).objectReferenceValue = japanZone;
            saveZones.GetArrayElementAtIndex(4).objectReferenceValue = reefZone;

            // Voyage entre les zones : un ponton par zone, E ouvre la carte du monde.
            var travel = partie.GetComponent<ZoneTravel>();
            if (travel == null)
            {
                travel = partie.AddComponent<ZoneTravel>();
            }

            var travelSo = new SerializedObject(travel);
            SerializedProperty destinations = travelSo.FindProperty("_destinations");
            destinations.arraySize = 8;
            SetDestination(destinations.GetArrayElementAtIndex(7), fjordZone, 7, buildings.FjordDock);
            SetDestination(destinations.GetArrayElementAtIndex(6), skeletonZone, 6, buildings.SkeletonDock);
            SetDestination(destinations.GetArrayElementAtIndex(5), atollZone, 5, buildings.AtollDock);
            SetDestination(destinations.GetArrayElementAtIndex(0), zone, 0, buildings.IslandDock);
            SetDestination(destinations.GetArrayElementAtIndex(1), lakeZone, 1, buildings.LakeDock);
            SetDestination(destinations.GetArrayElementAtIndex(2), amazonZone, 2, buildings.AmazonDock);
            SetDestination(destinations.GetArrayElementAtIndex(3), japanZone, 3, buildings.JapanDock);
            SetDestination(destinations.GetArrayElementAtIndex(4), reefZone, 4, buildings.ReefDock);
            travelSo.FindProperty("_player").objectReferenceValue = movement;
            travelSo.FindProperty("_fishing").objectReferenceValue = controller;
            travelSo.FindProperty("_map").objectReferenceValue = Object.FindAnyObjectByType<WorldMap>();
            travelSo.FindProperty("_camera").objectReferenceValue = Object.FindAnyObjectByType<CameraFollow>();
            travelSo.FindProperty("_minimap").objectReferenceValue = minimap;
            travelSo.FindProperty("_save").objectReferenceValue = saveManager;
            travelSo.ApplyModifiedPropertiesWithoutUndo();
            saveSo.FindProperty("_travel").objectReferenceValue = travel;
            saveSo.ApplyModifiedPropertiesWithoutUndo();

            var travelMenu = partie.GetComponent<UI.TravelMenu>();
            if (travelMenu == null)
            {
                travelMenu = partie.AddComponent<UI.TravelMenu>();
            }

            var travelMenuSo = new SerializedObject(travelMenu);
            travelMenuSo.FindProperty("_travel").objectReferenceValue = travel;
            travelMenuSo.FindProperty("_player").objectReferenceValue = movement;
            travelMenuSo.FindProperty("_fishing").objectReferenceValue = controller;
            travelMenuSo.FindProperty("_skin").objectReferenceValue = skin;
            travelMenuSo.ApplyModifiedPropertiesWithoutUndo();
            hudSo.FindProperty("_travel").objectReferenceValue = travel;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // L'ascenseur de l'aquarium : la zone de chaque étage (même ordre que AquariumBuilding.Floors) et le panneau.
            var floorsSo = new SerializedObject(aquarium);
            SerializedProperty floorZones = floorsSo.FindProperty("_floorZones");
            FishingZone[] byFloor = { zone, lakeZone, amazonZone, japanZone, reefZone, atollZone, skeletonZone, fjordZone };
            floorZones.arraySize = byFloor.Length;
            for (int i = 0; i < byFloor.Length; i++)
            {
                floorZones.GetArrayElementAtIndex(i).objectReferenceValue = byFloor[i];
            }

            floorsSo.ApplyModifiedPropertiesWithoutUndo();
            var elevatorMenu = partie.GetComponent<UI.ElevatorMenu>();
            if (elevatorMenu == null)
            {
                elevatorMenu = partie.AddComponent<UI.ElevatorMenu>();
            }

            var elevatorSo = new SerializedObject(elevatorMenu);
            elevatorSo.FindProperty("_aquarium").objectReferenceValue = aquarium;
            elevatorSo.FindProperty("_progress").objectReferenceValue = progress;
            elevatorSo.FindProperty("_player").objectReferenceValue = movement;
            elevatorSo.FindProperty("_skin").objectReferenceValue = skin;
            elevatorSo.ApplyModifiedPropertiesWithoutUndo();

            // Menu pause (Échap / Start) : reprendre, volume, plein écran, nouvelle partie, quitter.
            var pauseMenu = partie.GetComponent<UI.PauseMenu>();
            if (pauseMenu == null)
            {
                pauseMenu = partie.AddComponent<UI.PauseMenu>();
            }

            var pauseSo = new SerializedObject(pauseMenu);
            pauseSo.FindProperty("_player").objectReferenceValue = movement;
            pauseSo.FindProperty("_fishing").objectReferenceValue = controller;
            pauseSo.FindProperty("_minimap").objectReferenceValue = minimap;
            pauseSo.FindProperty("_save").objectReferenceValue = saveManager;
            pauseSo.FindProperty("_skin").objectReferenceValue = skin;
            pauseSo.ApplyModifiedPropertiesWithoutUndo();

            // Cycle jour/nuit : lumière globale teintée selon l'heure, lampadaires et lanternes des barques la nuit.
            var cycle = partie.GetComponent<DayNightCycle>();
            if (cycle == null)
            {
                cycle = partie.AddComponent<DayNightCycle>();
            }

            var cycleSo = new SerializedObject(cycle);
            GameObject decorObject = GameObject.Find("Décor");
            cycleSo.FindProperty("_decor").objectReferenceValue = decorObject != null ? decorObject.GetComponent<UnityEngine.Tilemaps.Tilemap>() : null;
            SerializedProperty lanterns = cycleSo.FindProperty("_lanterns");
            MapSetup.DockInfo[] docks = { buildings.IslandDock, buildings.LakeDock, buildings.AmazonDock, buildings.JapanDock, buildings.ReefDock, buildings.AtollDock, buildings.SkeletonDock, buildings.FjordDock };
            lanterns.arraySize = docks.Length;
            for (int i = 0; i < docks.Length; i++)
            {
                Vector2 at = docks[i] == null ? Vector2.zero : docks[i].Marker != null ? (Vector2)docks[i].Marker.position : docks[i].Arrival;
                lanterns.GetArrayElementAtIndex(i).vector2Value = at;
            }

            cycleSo.ApplyModifiedPropertiesWithoutUndo();
            saveSo.FindProperty("_cycle").objectReferenceValue = cycle;
            saveSo.ApplyModifiedPropertiesWithoutUndo();
            var controllerCycleSo = new SerializedObject(controller);
            controllerCycleSo.FindProperty("_cycle").objectReferenceValue = cycle;
            controllerCycleSo.ApplyModifiedPropertiesWithoutUndo();

            // Casiers à crustacés : C face à l'eau pour en poser un, C près de la bouée pour le relever.
            var traps = partie.GetComponent<CrustaceanTraps>();
            if (traps == null)
            {
                traps = partie.AddComponent<CrustaceanTraps>();
            }

            var trapsSo = new SerializedObject(traps);
            trapsSo.FindProperty("_progress").objectReferenceValue = progress;
            trapsSo.FindProperty("_player").objectReferenceValue = movement;
            trapsSo.FindProperty("_fishing").objectReferenceValue = controller;
            trapsSo.FindProperty("_cycle").objectReferenceValue = cycle;
            trapsSo.FindProperty("_map").objectReferenceValue = Object.FindAnyObjectByType<WorldMap>();
            trapsSo.FindProperty("_skin").objectReferenceValue = skin;
            SerializedProperty trapZones = trapsSo.FindProperty("_zones");
            trapZones.arraySize = saveZones.arraySize;
            for (int i = 0; i < saveZones.arraySize; i++)
            {
                trapZones.GetArrayElementAtIndex(i).objectReferenceValue = saveZones.GetArrayElementAtIndex(i).objectReferenceValue;
            }

            trapsSo.ApplyModifiedPropertiesWithoutUndo();

            // Météo : temps tiré selon le climat de la zone, ciel teinté, pluie, brume et éclairs (objet « Météo »).
            WeatherSystem weather = BuildWeather(partie.transform, cycle, controller, movement.transform);
            cycleSo.Update();
            cycleSo.FindProperty("_weather").objectReferenceValue = weather;
            cycleSo.ApplyModifiedPropertiesWithoutUndo();
            saveSo.Update();
            saveSo.FindProperty("_weather").objectReferenceValue = weather;
            saveSo.ApplyModifiedPropertiesWithoutUndo();
            controllerCycleSo.Update();
            controllerCycleSo.FindProperty("_weather").objectReferenceValue = weather;
            controllerCycleSo.ApplyModifiedPropertiesWithoutUndo();

            // Habitants de l'île : ils se baladent le jour, rentrent chez eux le soir, et on leur parle avec E.
            VillagerSetup.Place(VillagerSetup.CreateOrUpdate(), aquarium, buildings.IslandDock != null ? buildings.IslandDock.Arrival : Vector2.zero,
                Object.FindAnyObjectByType<WorldMap>(), movement, cycle, weather, progress);
            var dialogueBox = partie.GetComponent<UI.DialogueBox>();
            if (dialogueBox == null)
            {
                dialogueBox = partie.AddComponent<UI.DialogueBox>();
            }

            var dialogueSo = new SerializedObject(dialogueBox);
            dialogueSo.FindProperty("_skin").objectReferenceValue = skin;
            dialogueSo.FindProperty("_player").objectReferenceValue = movement;
            dialogueSo.ApplyModifiedPropertiesWithoutUndo();

            // Animaux de compagnie : celui qui suit le joueur, et la boutique du vendeur de l'animalerie.
            PetSetup.Place(partie, movement, progress, skin);

            // Vue de près des bassins de l'aquarium (Haut devant la vitre).
            var closeUp = partie.GetComponent<UI.AquariumCloseUp>();
            if (closeUp == null) closeUp = partie.AddComponent<UI.AquariumCloseUp>();
            var closeUpSo = new SerializedObject(closeUp);
            closeUpSo.FindProperty("_building").objectReferenceValue = Object.FindAnyObjectByType<AquariumBuilding>();
            closeUpSo.FindProperty("_camera").objectReferenceValue = Object.FindAnyObjectByType<CameraFollow>();
            closeUpSo.FindProperty("_skin").objectReferenceValue = skin;
            AquariumTank[] allTanks = Object.FindObjectsByType<AquariumTank>();
            SerializedProperty tankList = closeUpSo.FindProperty("_tanks");
            tankList.arraySize = allTanks.Length;
            for (int i = 0; i < allTanks.Length; i++) tankList.GetArrayElementAtIndex(i).objectReferenceValue = allTanks[i];
            closeUpSo.ApplyModifiedPropertiesWithoutUndo();
            var hudCloseUpSo = new SerializedObject(partie.GetComponent<UI.InventoryHud>());
            hudCloseUpSo.FindProperty("_closeUp").objectReferenceValue = closeUp;
            hudCloseUpSo.ApplyModifiedPropertiesWithoutUndo();

            // Écran titre (3 emplacements de sauvegarde), posé sur l'île tant qu'aucune partie n'est choisie.
            var titleScreen = partie.GetComponent<UI.TitleScreen>();
            if (titleScreen == null)
            {
                titleScreen = partie.AddComponent<UI.TitleScreen>();
            }

            var titleSo = new SerializedObject(titleScreen);
            titleSo.FindProperty("_save").objectReferenceValue = saveManager;
            titleSo.FindProperty("_player").objectReferenceValue = movement;
            titleSo.FindProperty("_fishing").objectReferenceValue = controller;
            titleSo.FindProperty("_minimap").objectReferenceValue = minimap;
            titleSo.FindProperty("_skin").objectReferenceValue = skin;
            SerializedProperty titleZones = titleSo.FindProperty("_zones");
            titleZones.arraySize = 8;
            titleZones.GetArrayElementAtIndex(7).objectReferenceValue = fjordZone;
            titleZones.GetArrayElementAtIndex(6).objectReferenceValue = skeletonZone;
            titleZones.GetArrayElementAtIndex(5).objectReferenceValue = atollZone;
            titleZones.GetArrayElementAtIndex(0).objectReferenceValue = zone;
            titleZones.GetArrayElementAtIndex(1).objectReferenceValue = lakeZone;
            titleZones.GetArrayElementAtIndex(2).objectReferenceValue = amazonZone;
            titleZones.GetArrayElementAtIndex(3).objectReferenceValue = japanZone;
            titleZones.GetArrayElementAtIndex(4).objectReferenceValue = reefZone;
            titleSo.ApplyModifiedPropertiesWithoutUndo();

            if (zone == null)
            {
                Debug.LogError("[FishAndChill] Zone de test introuvable : la scène n'a pas de zone assignée.");
            }

            TilemapExport.StoreMapTiles();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            TilemapExport.RefreshPreview();

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        /// <summary>Région de la carte d'une zone (nom de son asset), la même que pour sa destination de voyage ; -1 si inconnue.</summary>
        private static int RegionOf(string zoneName)
        {
            switch (zoneName)
            {
                case "IleMahana": return 0;
                case "LacAnnecy": return 1;
                case "FleuveAmazone": return 2;
                case "BaieJaponaise": return 3;
                case "GrandeBarriere": return 4;
                case "Maldives": return 5;
                case "CoteDesSquelettes": return 6;
                case "FjordsNorvege": return 7;
                default: return -1;
            }
        }

        private static void SetDestination(SerializedProperty entry, FishingZone zone, int region, MapSetup.DockInfo dock)
        {
            entry.FindPropertyRelative("Zone").objectReferenceValue = zone;
            entry.FindPropertyRelative("Region").intValue = region;
            entry.FindPropertyRelative("Arrival").vector2Value = dock.Arrival;
            entry.FindPropertyRelative("Facing").vector2Value = dock.Facing;
            entry.FindPropertyRelative("Dock").rectValue = dock.Area;
            entry.FindPropertyRelative("Marker").objectReferenceValue = dock.Marker;
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
