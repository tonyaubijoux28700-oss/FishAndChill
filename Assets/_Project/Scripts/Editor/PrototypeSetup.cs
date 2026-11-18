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

            string startPath = ZonesFolder + "/IleMahana.asset";
            CreateOrUpdateZone(startPath, "Île Mahana",
                "Île tropicale fictive : lagon turquoise, plages de sable blanc et cocotiers. Point de départ de l'aventure.",
                MahanaWater, rows, new Vector2(0.12f, 0.36f), "Polynésie (île fictive)");
            return startPath;
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
            so.FindProperty("_sprite").objectReferenceValue = FishArt.Generate(data.Id, data.Color, data);
            so.FindProperty("_miniSprite").objectReferenceValue = FishArt.GenerateMini(data.Id);
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
                    if (legend != null && legend.Zone != null && legend.Zone.name == "IleMahana" && legendSpot.Region == 0) match = legend;
                }

                spotSo.FindProperty("_legend").objectReferenceValue = match;
                spotSo.ApplyModifiedPropertiesWithoutUndo();
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
            zones.arraySize = 6;
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
            saveZones.arraySize = 6;
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
            destinations.arraySize = 6;
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
            MapSetup.DockInfo[] docks = { buildings.IslandDock, buildings.LakeDock, buildings.AmazonDock, buildings.JapanDock, buildings.ReefDock, buildings.AtollDock };
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
            titleZones.arraySize = 6;
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

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
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
