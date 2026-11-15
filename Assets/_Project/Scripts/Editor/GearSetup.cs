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

            // Canne : Value = zone de réussite plus large (0,15 = +15 %), le curseur ralentit en proportion.
            GearLine rod = Line("Canne", GearCategory.Rod, "Canne", rodIcon,
                new GearLevel("Canne en bambou", 0, 0f),
                new GearLevel("Canne en fibre de verre", 60, 0.15f),
                new GearLevel("Canne en carbone", 180, 0.30f),
                new GearLevel("Canne de compétition", 450, 0.45f));

            // Hameçon : Value = ratés tolérés en plus.
            GearLine hook = Line("Hamecon", GearCategory.Hook, "Hameçon", hookIcon,
                new GearLevel("Hameçon simple", 0, 0f),
                new GearLevel("Hameçon renforcé", 45, 1f),
                new GearLevel("Hameçon triple", 150, 2f),
                new GearLevel("Hameçon en acier forgé", 380, 3f));

            // Appât : Value = bonus de rareté, Value2 = touches plus rapides.
            GearLine bait = Line("Appat", GearCategory.Bait, "Appât", baitIcon,
                new GearLevel("Mie de pain", 0, 0f, 0f),
                new GearLevel("Crevettes fraîches", 50, 0.4f, 0.15f),
                new GearLevel("Leurre brillant", 160, 0.8f, 0.25f),
                new GearLevel("Calmar entier", 400, 1.4f, 0.35f));

            // Sac : Value = nombre de places.
            GearLine bag = Line("Sac", GearCategory.Bag, "Sac", bagIcon,
                new GearLevel("Sacoche", 0, 3f),
                new GearLevel("Besace", 70, 4f),
                new GearLevel("Glacière", 200, 5f),
                new GearLevel("Grande glacière", 480, 6f));

            var catalog = LoadOrCreate<GearCatalog>($"{Folder}/CatalogueMateriel.asset");
            catalog.Configure(new[] { rod, hook, bait, bag });
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
