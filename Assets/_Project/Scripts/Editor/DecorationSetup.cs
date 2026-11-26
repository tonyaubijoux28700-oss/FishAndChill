using FishAndChill.Collection;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les décorations d'aquarium (<see cref="Decoration"/> et <see cref="DecorationCatalog"/> dans
    /// <c>Data/Decorations/</c>, dessins <see cref="DecorationArt"/> dans <c>Art/Decorations/</c>), puis leur pose dans la
    /// scène : le site de plongée de Mahana (<see cref="DiveSite"/>) et les emplacements du bassin du grand hall
    /// (<see cref="TankDecorations"/>).
    /// </summary>
    public static class DecorationSetup
    {
        private const string DataFolder = "Assets/_Project/Data/Decorations";
        private const string ArtFolder = "Assets/_Project/Art/Decorations";
        private const string MahanaZone = "Assets/_Project/Data/Zones/IleMahana.asset";

        public static DecorationCatalog CreateOrUpdate()
        {
            PixelArt.EnsureFolder(DataFolder);
            PixelArt.EnsureFolder(ArtFolder);
            var mahana = AssetDatabase.LoadAssetAtPath<FishingZone>(MahanaZone);
            Decoration[] all =
            {
                Save("corail-branchu", "Corail branchu", "Une touffe de corail rose aux branches qui se divisent. Les petits poissons adorent s'y cacher.", mahana, Rarity.Common),
                Save("corail-cerveau", "Corail cerveau", "Un dôme couvert de sillons sinueux, comme un cerveau. Il pousse d'à peine un centimètre par an.", mahana, Rarity.Common),
                Save("anemone", "Anémone", "Ses tentacules violets piquent tous les poissons... sauf le poisson-clown, qui vit dedans.", mahana, Rarity.Common),
                Save("conque", "Conque", "Une grande coquille en spirale à la lèvre nacrée. Souffle dedans, elle chante comme une corne.", mahana, Rarity.Common),
                Save("etoile-de-mer", "Étoile de mer", "Elle avance sur des centaines de petits pieds et peut refaire un bras perdu.", mahana, Rarity.Common),
                Save("rocher-de-lave", "Rocher de lave", "Un bloc de lave noire et poreuse, tombé du volcan de l'île il y a très longtemps.", mahana, Rarity.Common),
                Save("corail-table", "Corail table", "Un large plateau de corail sur un pied court, comme une petite table sous l'eau.", mahana, Rarity.Uncommon),
                Save("gorgone", "Gorgone", "Un grand éventail violet en dentelle, toujours tourné face au courant.", mahana, Rarity.Uncommon),
                Save("benitier", "Bénitier géant", "Le plus grand coquillage du monde. Son manteau bleu électrique brille au soleil.", mahana, Rarity.Rare),
                Save("ancre", "Ancre rouillée", "Une vieille ancre couverte de rouille et de concrétions. D'où vient le bateau qui l'a perdue ?", mahana, Rarity.Rare),
            };

            string path = $"{DataFolder}/Decorations.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<DecorationCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DecorationCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            Sprite mask = PixelArt.Save($"{ArtFolder}/Masque_Tuba.png", DecorationArt.MaskIcon(), 16, new Vector2(0.5f, 0.5f));
            catalog.Configure(all, mask);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static Decoration Save(string id, string name, string description, FishingZone zone, Rarity rarity)
        {
            string path = $"{DataFolder}/{id}.asset";
            var decoration = AssetDatabase.LoadAssetAtPath<Decoration>(path);
            if (decoration == null)
            {
                decoration = ScriptableObject.CreateInstance<Decoration>();
                AssetDatabase.CreateAsset(decoration, path);
            }

            Sprite sprite = PixelArt.Save($"{ArtFolder}/{id}.png", DecorationArt.Draw(id), 16, DecorationArt.Pivot);
            decoration.Configure(id, name, description, zone, rarity, sprite);
            EditorUtility.SetDirty(decoration);
            return decoration;
        }

        /// <summary>Le site de plongée de Mahana et les emplacements du bassin du grand hall.</summary>
        public static void Place(GameObject partie, MapSetup.Buildings buildings, PlayerMovement player, PlayerProgress progress, UI.UiSkin skin)
        {
            GameObject siteObject = GameObject.Find("Plongée de Mahana");
            if (siteObject == null) siteObject = new GameObject("Plongée de Mahana");
            var site = siteObject.GetComponent<DiveSite>();
            if (site == null) site = siteObject.AddComponent<DiveSite>();
            var caustics = new Sprite[3];
            for (int f = 0; f < 3; f++) caustics[f] = PixelArt.Save($"Assets/_Project/Art/Plongee/Lumiere_{f}.png", DiveArt.Caustics(f), DiveArt.PixelsPerUnit, new Vector2(0.5f, 0.5f));
            var so = new SerializedObject(site);
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_progress").objectReferenceValue = progress;
            so.FindProperty("_map").objectReferenceValue = Object.FindAnyObjectByType<WorldMap>();
            so.FindProperty("_camera").objectReferenceValue = Object.FindAnyObjectByType<CameraFollow>();
            so.FindProperty("_skin").objectReferenceValue = skin;
            so.FindProperty("_zone").objectReferenceValue = AssetDatabase.LoadAssetAtPath<FishingZone>(MahanaZone);
            so.FindProperty("_origin").vector2Value = buildings.DiveOrigin;
            so.FindProperty("_buoy").vector2Value = buildings.DiveBuoy;
            so.FindProperty("_seed").intValue = MapSetup.MahanaDiveSeed;
            SerializedProperty light = so.FindProperty("_caustics");
            light.arraySize = caustics.Length;
            for (int i = 0; i < caustics.Length; i++) light.GetArrayElementAtIndex(i).objectReferenceValue = caustics[i];
            so.FindProperty("_bubble").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Buildings/Aquarium/Bulle.png");
            so.ApplyModifiedPropertiesWithoutUndo();

            AquariumBuilding aquarium = buildings.Aquarium;
            if (aquarium == null) return;
            Transform existing = aquarium.transform.Find("Décorations du grand bassin");
            GameObject tankObject = existing != null ? existing.gameObject : new GameObject("Décorations du grand bassin");
            tankObject.transform.SetParent(aquarium.transform, false);
            var tank = tankObject.GetComponent<TankDecorations>();
            if (tank == null) tank = tankObject.AddComponent<TankDecorations>();
            var tankSo = new SerializedObject(tank);
            tankSo.FindProperty("_tankKey").stringValue = "hall";
            tankSo.FindProperty("_building").objectReferenceValue = aquarium;
            tankSo.FindProperty("_progress").objectReferenceValue = progress;
            tankSo.FindProperty("_player").objectReferenceValue = player;
            tankSo.FindProperty("_skin").objectReferenceValue = skin;
            tankSo.FindProperty("_area").rectValue = AquariumBuilding.HallTank;
            tankSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
