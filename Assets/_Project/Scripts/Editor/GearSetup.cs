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
            Sprite Big(int level) => PixelArt.Save($"{IconFolder}/Materiel/CanneGrande_{level}.png", GearArt.RodLarge(level), 16, Vector2.zero);

            // Cannes : des objets à part, chacun avec sa spécialité (on les achète puis on choisit laquelle on tient).
            // Value = zone de réussite plus large (0,15 = +15 %) ; WithStats = curseur, tension, portée, poissons nerveux / rushs.
            GearLevel RodItem(int i, string name, int price, float value) =>
                new GearLevel(name, price, value).WithIcon(Item($"Canne_{i}", GearArt.Rod(i))).WithIllustration(Big(i));
            GearLine rod = Line("Canne", GearCategory.Rod, "Canne", rodIcon,
                RodItem(0, "Canne en bambou", 0, 0f).WithStats("Simple et fiable."),
                RodItem(1, "Canne légère", 80, 0.20f).WithStats("Calme les poissons nerveux, mais fragile face aux lourds.", tension: 1.25f, nervous: 0.5f),
                RodItem(2, "Canne longue", 160, 0.10f).WithStats("Lance bien plus loin (+2,5 cases).", cast: 2.5f),
                RodItem(3, "Canne lourde", 220, 0.05f).WithStats("Tient la tension des gros, rushs plus espacés.", cursor: 1.1f, tension: 0.5f, rush: 1.4f),
                RodItem(4, "Canne de compétition", 480, 0.35f).WithStats("La meilleure en tout : zone large, curseur lent.", cursor: 0.85f, tension: 0.8f));

            // Hameçons : Value = ratés tolérés en plus.
            GearLevel HookItem(int i, string name, int price, float value) =>
                new GearLevel(name, price, value).WithIcon(Item($"Hamecon_{i}", GearArt.Hook(i)));
            GearLine hook = Line("Hamecon", GearCategory.Hook, "Hameçon", hookIcon,
                HookItem(0, "Hameçon simple", 0, 0f).WithStats("Rien de spécial."),
                HookItem(1, "Hameçon triple", 60, 2f).WithStats("Tient mieux : 2 ratés de plus."),
                HookItem(2, "Hameçon circulaire", 150, 1f).WithStats("Moins de tension, rushs plus rares.", tension: 0.8f, rush: 1.3f),
                HookItem(3, "Hameçon forgé", 340, 3f).WithStats("3 ratés de plus, les fuyants glissent moins.", drift: 0.6f));

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
