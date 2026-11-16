using FishAndChill.Gear;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Crée (ou met à jour) les données du matériel : une famille par catégorie dans <c>Data/Gear/</c>, avec ses
    /// niveaux, ses prix et son icône, et le catalogue que la partie et le marchand utilisent.
    /// Prix pensés pour l'économie de l'île : une prise rapporte une vingtaine de pièces en moyenne.
    /// </summary>
    public static class GearSetup
    {
        private const string Folder = "Assets/_Project/Data/Gear";
        private const string IconFolder = "Assets/_Project/Art/UI";

        public static GearCatalog CreateOrUpdate()
        {
            PixelArt.EnsureFolder(Folder);
            Sprite rodIcon = PixelArt.Save($"{IconFolder}/IconeCanne.png", UiArt.RodIcon(), 16, new Vector2(0.5f, 0.5f));
            Sprite hookIcon = PixelArt.Save($"{IconFolder}/IconeHamecon.png", UiArt.HookIcon(), 16, new Vector2(0.5f, 0.5f));
            Sprite baitIcon = PixelArt.Save($"{IconFolder}/IconeAppat.png", UiArt.BaitIcon(), 16, new Vector2(0.5f, 0.5f));
            Sprite bagIcon = PixelArt.Save($"{IconFolder}/IconeSac.png", UiArt.BagIcon(), 16, new Vector2(0.5f, 0.5f));

            // Une icône 32×32 par objet (inventaire, marchand).
            Sprite Item(string file, Color[,] pixels) => PixelArt.Save($"{IconFolder}/Materiel/{file}.png", pixels, 16, new Vector2(0.5f, 0.5f));

            // Canne : Value = zone de réussite plus large (0,15 = +15 %), le curseur ralentit en proportion.
            GearLine rod = Line("Canne", GearCategory.Rod, "Canne", rodIcon,
                new GearLevel("Canne en bambou", 0, 0f).WithIcon(Item("Canne_0", GearArt.Rod(0))),
                new GearLevel("Canne en fibre de verre", 60, 0.15f).WithIcon(Item("Canne_1", GearArt.Rod(1))),
                new GearLevel("Canne en carbone", 180, 0.30f).WithIcon(Item("Canne_2", GearArt.Rod(2))),
                new GearLevel("Canne de compétition", 450, 0.45f).WithIcon(Item("Canne_3", GearArt.Rod(3))));

            // Hameçon : Value = ratés tolérés en plus.
            GearLine hook = Line("Hamecon", GearCategory.Hook, "Hameçon", hookIcon,
                new GearLevel("Hameçon simple", 0, 0f).WithIcon(Item("Hamecon_0", GearArt.Hook(0))),
                new GearLevel("Hameçon renforcé", 45, 1f).WithIcon(Item("Hamecon_1", GearArt.Hook(1))),
                new GearLevel("Hameçon triple", 150, 2f).WithIcon(Item("Hamecon_2", GearArt.Hook(2))),
                new GearLevel("Hameçon en acier forgé", 380, 3f).WithIcon(Item("Hamecon_3", GearArt.Hook(3))));

            // Sac : Value = nombre de places.
            GearLine bag = Line("Sac", GearCategory.Bag, "Sac", bagIcon,
                new GearLevel("Sacoche", 0, 3f).WithIcon(Item("Sac_0", GearArt.Bag(0))),
                new GearLevel("Besace", 70, 4f).WithIcon(Item("Sac_1", GearArt.Bag(1))),
                new GearLevel("Glacière", 200, 5f).WithIcon(Item("Sac_2", GearArt.Bag(2))),
                new GearLevel("Grande glacière", 480, 6f).WithIcon(Item("Sac_3", GearArt.Bag(3))));

            var catalog = LoadOrCreate<GearCatalog>($"{Folder}/CatalogueMateriel.asset");
            // Les appâts ne sont plus du matériel à niveaux : ils ont leur propre catalogue (BaitSetup).
            catalog.Configure(new[] { rod, hook, bag });
            AssetDatabase.DeleteAsset($"{Folder}/Appat.asset");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static GearLine Line(string fileName, GearCategory category, string displayName, Sprite icon, params GearLevel[] levels)
        {
            var line = LoadOrCreate<GearLine>($"{Folder}/{fileName}.asset");
            line.Configure(category, displayName, levels, icon);
            EditorUtility.SetDirty(line);
            return line;
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
    }
}
