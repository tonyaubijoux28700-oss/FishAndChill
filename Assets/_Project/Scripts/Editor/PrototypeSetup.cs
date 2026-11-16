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
            public readonly CreatureKind Kind;
            public readonly BodyShape Shape;

            public SpeciesData(string id, string commonName, string scientificName, Rarity rarity,
                float speed, float zoneWidth, int hits, int misses, Vector2 sizeCm, int price, Color color, float weight,
                CreatureKind kind = CreatureKind.Fish, BodyShape shape = BodyShape.Fish)
            {
                Kind = kind;
                Shape = shape;
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
            new SpeciesData("coregonus-lavaretus", "Lavaret", "Coregonus lavaretus", Rarity.Uncommon,
                1.2f, 0.22f, 3, 2, new Vector2(30f, 55f), 18, new Color(0.75f, 0.80f, 0.84f), 15f),
            new SpeciesData("alburnus-alburnus", "Ablette", "Alburnus alburnus", Rarity.Common,
                0.7f, 0.32f, 2, 2, new Vector2(8f, 20f), 3, new Color(0.80f, 0.86f, 0.88f), 35f),
            new SpeciesData("squalius-cephalus", "Chevesne", "Squalius cephalus", Rarity.Common,
                1.0f, 0.26f, 3, 2, new Vector2(20f, 60f), 9, new Color(0.62f, 0.62f, 0.52f), 28f),
            new SpeciesData("tinca-tinca", "Tanche", "Tinca tinca", Rarity.Uncommon,
                1.0f, 0.24f, 3, 2, new Vector2(20f, 60f), 16, new Color(0.38f, 0.48f, 0.20f), 14f),
            new SpeciesData("cyprinus-carpio", "Carpe commune", "Cyprinus carpio", Rarity.Rare,
                1.4f, 0.18f, 4, 2, new Vector2(30f, 100f), 40, new Color(0.72f, 0.58f, 0.28f), 9f),
            new SpeciesData("lota-lota", "Lotte", "Lota lota", Rarity.Epic,
                1.6f, 0.14f, 4, 1, new Vector2(30f, 70f), 80, new Color(0.48f, 0.42f, 0.24f), 4f),

            // Pas que des poissons : l'écrevisse signal (installée dans le lac) et la grenouille rieuse des rives.
            new SpeciesData("pacifastacus-leniusculus", "Écrevisse signal", "Pacifastacus leniusculus", Rarity.Common,
                0.9f, 0.26f, 2, 2, new Vector2(8f, 16f), 7, new Color(0.42f, 0.30f, 0.18f), 18f,
                CreatureKind.Crustacean, BodyShape.Lobster),
            new SpeciesData("pelophylax-ridibundus", "Grenouille rieuse", "Pelophylax ridibundus", Rarity.Uncommon,
                1.1f, 0.22f, 3, 2, new Vector2(7f, 15f), 15, new Color(0.42f, 0.54f, 0.24f), 10f,
                CreatureKind.Amphibian, BodyShape.Frog),
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
            new SpeciesData("chaetodon-auriga", "Poisson-papillon cocher", "Chaetodon auriga", Rarity.Common,
                0.85f, 0.28f, 2, 2, new Vector2(12f, 23f), 6, new Color(0.96f, 0.90f, 0.60f), 25f),
            new SpeciesData("mulloidichthys-flavolineatus", "Rouget-barbet à bande jaune", "Mulloidichthys flavolineatus", Rarity.Common,
                0.9f, 0.27f, 2, 2, new Vector2(15f, 40f), 7, new Color(0.92f, 0.90f, 0.76f), 25f),
            new SpeciesData("lutjanus-kasmira", "Vivaneau à raies bleues", "Lutjanus kasmira", Rarity.Uncommon,
                1.1f, 0.23f, 3, 2, new Vector2(15f, 35f), 16, new Color(0.98f, 0.82f, 0.24f), 14f),
            new SpeciesData("rhinecanthus-aculeatus", "Baliste Picasso", "Rhinecanthus aculeatus", Rarity.Uncommon,
                1.2f, 0.22f, 3, 2, new Vector2(12f, 30f), 18, new Color(0.84f, 0.78f, 0.62f), 12f),
            new SpeciesData("pterois-radiata", "Poisson-lion radié", "Pterois radiata", Rarity.Rare,
                1.4f, 0.17f, 4, 2, new Vector2(12f, 24f), 40, new Color(0.60f, 0.22f, 0.16f), 7f),

            // On ne pêche pas que des poissons : crustacés du lagon et des mangroves, et la tortue verte.
            new SpeciesData("scylla-serrata", "Crabe de palétuvier", "Scylla serrata", Rarity.Uncommon,
                1.0f, 0.22f, 3, 2, new Vector2(10f, 25f), 18, new Color(0.38f, 0.40f, 0.22f), 12f,
                CreatureKind.Crustacean, BodyShape.Crab),
            new SpeciesData("panulirus-versicolor", "Langouste porcelaine", "Panulirus versicolor", Rarity.Rare,
                1.4f, 0.17f, 4, 2, new Vector2(20f, 40f), 55, new Color(0.13f, 0.42f, 0.50f), 6f,
                CreatureKind.Crustacean, BodyShape.Lobster),
            new SpeciesData("chelonia-mydas", "Tortue verte", "Chelonia mydas", Rarity.Epic,
                1.6f, 0.14f, 5, 2, new Vector2(60f, 120f), 120, new Color(0.42f, 0.34f, 0.18f), 2f,
                CreatureKind.Reptile, BodyShape.Turtle),
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

            // Deuxième zone : un lac alpin d'eau douce bien réel.
            CreateOrUpdateZone(LakeZonePath, "Lac d'Annecy",
                "Lac alpin aux eaux très claires, entouré de prés et de forêts de sapins.", new Color(0.12f, 0.34f, 0.44f), AnnecySpecies,
                new Vector2(0.52f, 0.74f), "Haute-Savoie, France");

            string startPath = ZonesFolder + "/IleMahana.asset";
            CreateOrUpdateZone(startPath, "Île Mahana",
                "Île tropicale fictive : lagon turquoise, plages de sable blanc et cocotiers. Point de départ de l'aventure.",
                MahanaWater, MahanaSpecies,
                new Vector2(0.12f, 0.36f), "Polynésie (île fictive)");
            return startPath;
        }

        private static void CreateOrUpdateZone(string path, string displayName, string description, Color water, SpeciesData[] speciesData,
            Vector2 mapPosition, string region)
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
            so.FindProperty("_mapPosition").vector2Value = mapPosition;
            so.FindProperty("_region").stringValue = region;

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

        /// <summary>
        /// Espèces qui ne mordent que le jour ou la nuit, d'après leur comportement réel (les autres : à toute heure).
        /// Le perroquet dort la nuit dans un cocon de mucus ; crabe, langouste et écrevisse sortent la nuit ;
        /// le mérou paon chasse surtout au crépuscule ; le brochet et la perche chassent à vue, de jour.
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, ActiveTime> ActiveTimes =
            new System.Collections.Generic.Dictionary<string, ActiveTime>
            {
                { "scarus-psittacus", ActiveTime.Day },
                { "acanthurus-triostegus", ActiveTime.Day },
                { "abudefduf-vaigiensis", ActiveTime.Day },
                { "cephalopholis-argus", ActiveTime.Night },
                { "scylla-serrata", ActiveTime.Night },
                { "panulirus-versicolor", ActiveTime.Night },
                { "esox-lucius", ActiveTime.Day },
                { "perca-fluviatilis", ActiveTime.Day },
                { "pacifastacus-leniusculus", ActiveTime.Night },
                { "chaetodon-auriga", ActiveTime.Day },
                { "rhinecanthus-aculeatus", ActiveTime.Day },
                { "lutjanus-kasmira", ActiveTime.Night },
                { "pterois-radiata", ActiveTime.Night },
                { "alburnus-alburnus", ActiveTime.Day },
                { "tinca-tinca", ActiveTime.Night },
                { "lota-lota", ActiveTime.Night },
            };

        /// <summary>
        /// Façon de se battre une fois ferré, d'après le vrai comportement (les autres : calmes). Nerveux : petits
        /// poissons vifs ; fuyants : ceux qui filent en longues courses ; rushs : chasseurs qui démarrent d'un coup, et
        /// crustacés qui s'échappent d'un coup de queue ; lourds : gros poissons et créatures qui tirent vers le fond.
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, FightStyle> FightStyles =
            new System.Collections.Generic.Dictionary<string, FightStyle>
            {
                { "abudefduf-vaigiensis", FightStyle.Nervous },
                { "acanthurus-triostegus", FightStyle.Nervous },
                { "chaetodon-auriga", FightStyle.Nervous },
                { "scarus-psittacus", FightStyle.Drifting },
                { "albula-glossodonta", FightStyle.Drifting },
                { "lutjanus-kasmira", FightStyle.Drifting },
                { "caranx-melampygus", FightStyle.Rusher },
                { "sphyraena-barracuda", FightStyle.Rusher },
                { "panulirus-versicolor", FightStyle.Rusher },
                { "cephalopholis-argus", FightStyle.Heavy },
                { "caranx-ignobilis", FightStyle.Heavy },
                { "scylla-serrata", FightStyle.Heavy },
                { "chelonia-mydas", FightStyle.Heavy },
                { "perca-fluviatilis", FightStyle.Nervous },
                { "alburnus-alburnus", FightStyle.Nervous },
                { "pelophylax-ridibundus", FightStyle.Nervous },
                { "salmo-trutta", FightStyle.Drifting },
                { "salvelinus-umbla", FightStyle.Drifting },
                { "squalius-cephalus", FightStyle.Drifting },
                { "esox-lucius", FightStyle.Rusher },
                { "pacifastacus-leniusculus", FightStyle.Rusher },
                { "tinca-tinca", FightStyle.Heavy },
                { "cyprinus-carpio", FightStyle.Heavy },
            };

        /// <summary>Une ou deux phrases vraies sur chaque espèce, affichées dans la fiche de l'index.</summary>
        private static readonly System.Collections.Generic.Dictionary<string, string> Descriptions =
            new System.Collections.Generic.Dictionary<string, string>
            {
                // Île Mahana
                { "abudefduf-vaigiensis", "Vit en bancs près des récifs. Le mâle garde ses oeufs collés sur un rocher et les défend contre tous." },
                { "acanthurus-triostegus", "Broute les algues en grands bancs. Ses rayures lui valent son surnom de « bagnard »." },
                { "scarus-psittacus", "Croque le corail avec son bec et rejette du sable fin : une bonne part des plages vient de lui !" },
                { "cephalopholis-argus", "Mérou couvert de points bleus. Chasse à l'affût au crépuscule, caché dans les failles du récif." },
                { "caranx-melampygus", "Chasseur rapide aux reflets bleus. Il suit parfois les murènes pour attraper ce qu'elles délogent." },
                { "albula-glossodonta", "Le « fantôme des flats » : très méfiant et d'une vitesse folle, le rêve des pêcheurs à la mouche." },
                { "sphyraena-barracuda", "Longue torpille aux dents acérées. Il attaque d'un coup, attiré par tout ce qui brille." },
                { "caranx-ignobilis", "La plus grande des carangues : elle peut dépasser 1,5 m et sauter sur les oiseaux de mer." },
                { "chaetodon-auriga", "Vit en couple fidèle. La tache noire sur sa nageoire trompe les prédateurs, qui visent le mauvais côté." },
                { "mulloidichthys-flavolineatus", "Fouille le sable avec ses deux barbillons pour trouver vers et petits crabes." },
                { "lutjanus-kasmira", "Se repose en grands bancs le jour et part chasser la nuit sur le récif." },
                { "rhinecanthus-aculeatus", "Son nom hawaïen, humuhumunukunukuapua'a, est l'un des plus longs du monde. Il grogne quand on l'attrape !" },
                { "pterois-radiata", "Ses longues épines sont venimeuses. Il chasse la nuit en coinçant ses proies avec ses nageoires en éventail." },
                { "scylla-serrata", "Vit dans la vase des mangroves. Ses pinces sont si puissantes qu'elles brisent les coquillages." },
                { "panulirus-versicolor", "Langouste aux couleurs de porcelaine. Elle n'a pas de pinces et sort la nuit de son trou." },
                { "chelonia-mydas", "Broute les herbiers du lagon. Elle revient pondre sur la plage où elle est née, des années après." },

                // Lac d'Annecy
                { "rutilus-rutilus", "Le poisson blanc le plus commun du lac. On le reconnaît à son oeil rouge." },
                { "perca-fluviatilis", "Chasse en bandes près du bord. Les filets de perche sont la grande spécialité des lacs alpins." },
                { "salmo-trutta", "Truite sauvage des eaux froides, piquetée de points rouges cerclés de blanc." },
                { "esox-lucius", "Le grand prédateur du lac. Immobile dans les herbes, il fond sur sa proie en un éclair." },
                { "salvelinus-umbla", "Poisson des eaux profondes et glacées. Son ventre devient rouge vif à la saison des amours." },
                { "coregonus-lavaretus", "Vit en bancs au large, en pleine eau. Un des poissons les plus pêchés du lac d'Annecy." },
                { "alburnus-alburnus", "Petit poisson argenté qui gobe les insectes en surface. Ses écailles servaient à fabriquer de fausses perles." },
                { "squalius-cephalus", "Mange de tout, même des cerises tombées dans l'eau ! Très méfiant à l'approche du pêcheur." },
                { "tinca-tinca", "Aime les fonds vaseux et la végétation. Son mucus passait autrefois pour soigner les autres poissons." },
                { "cyprinus-carpio", "Peut vivre plus de 40 ans. Elle fouille la vase avec ses barbillons à la recherche de nourriture." },
                { "lota-lota", "Le seul cousin de la morue en eau douce. Elle vit au fond, dans le froid, et ne sort que la nuit." },
                { "pacifastacus-leniusculus", "Venue d'Amérique, elle a envahi le lac. Une tache claire à la jointure des pinces lui donne son nom." },
                { "pelophylax-ridibundus", "Son chant ressemble à un rire moqueur. Elle passe l'hiver enfouie dans la vase." },
            };

        private static FishSpecies CreateOrUpdateSpecies(SpeciesData data)
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
            so.FindProperty("_description").stringValue = Descriptions.TryGetValue(data.Id, out string description) ? description : string.Empty;
            so.FindProperty("_fightStyle").enumValueIndex = (int)(FightStyles.TryGetValue(data.Id, out FightStyle fight) ? fight : FightStyle.Calm);
            so.FindProperty("_activeTime").enumValueIndex =
                (int)(ActiveTimes.TryGetValue(data.Id, out ActiveTime time) ? time : ActiveTime.Always);
            so.FindProperty("_sprite").objectReferenceValue = FishArt.Generate(data.Id, data.Color);
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
            so.ApplyModifiedPropertiesWithoutUndo();

            // Le pêcheur est trié par ses pieds (pivot) pour passer devant / derrière les cocotiers et l'aquarium.
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;

            // L'île de départ, la boutique, l'aquarium, la caméra qui suit et les branchements joueur / carte.
            MapSetup.Buildings buildings = MapSetup.BuildIsland(player, controller);
            ShopBuilding shop = buildings.Shop;
            AquariumBuilding aquarium = buildings.Aquarium;

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
            zones.arraySize = 2;
            zones.GetArrayElementAtIndex(0).objectReferenceValue = zone;
            zones.GetArrayElementAtIndex(1).objectReferenceValue = lakeZone;
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
            saveZones.arraySize = 2;
            saveZones.GetArrayElementAtIndex(0).objectReferenceValue = zone;
            saveZones.GetArrayElementAtIndex(1).objectReferenceValue = lakeZone;

            // Voyage entre les zones : un ponton par zone, E ouvre la carte du monde.
            var travel = partie.GetComponent<ZoneTravel>();
            if (travel == null)
            {
                travel = partie.AddComponent<ZoneTravel>();
            }

            var travelSo = new SerializedObject(travel);
            SerializedProperty destinations = travelSo.FindProperty("_destinations");
            destinations.arraySize = 2;
            SetDestination(destinations.GetArrayElementAtIndex(0), zone, 0, buildings.IslandDock);
            SetDestination(destinations.GetArrayElementAtIndex(1), lakeZone, 1, buildings.LakeDock);
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
            MapSetup.DockInfo[] docks = { buildings.IslandDock, buildings.LakeDock };
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
            titleZones.arraySize = 2;
            titleZones.GetArrayElementAtIndex(0).objectReferenceValue = zone;
            titleZones.GetArrayElementAtIndex(1).objectReferenceValue = lakeZone;
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
