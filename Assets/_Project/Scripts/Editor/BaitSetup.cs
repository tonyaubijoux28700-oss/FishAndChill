using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Gear;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Crée ou met à jour les appâts et leur catalogue (Data/Gear/Appats) : six appâts, chacun avec ses espèces
    /// favorites d'après leur vrai régime. Appelé par le menu du prototype, après la création des espèces.
    /// </summary>
    public static class BaitSetup
    {
        private const string Folder = "Assets/_Project/Data/Gear/Appats";
        private const string IconFolder = "Assets/_Project/Art/UI/Materiel";

        public static BaitCatalog CreateOrUpdate()
        {
            PixelArt.EnsureFolder(Folder);
            var species = new Dictionary<string, FishSpecies>();
            foreach (string guid in AssetDatabase.FindAssets("t:FishSpecies"))
            {
                var s = AssetDatabase.LoadAssetAtPath<FishSpecies>(AssetDatabase.GUIDToAssetPath(guid));
                if (s != null)
                {
                    species[s.Id] = s;
                }
            }

            FishSpecies[] Pick(params string[] ids)
            {
                var list = new List<FishSpecies>();
                foreach (string id in ids)
                {
                    if (species.TryGetValue(id, out FishSpecies s)) list.Add(s);
                }

                return list.ToArray();
            }

            Sprite Icon(string file, Color[,] pixels) => PixelArt.Save($"{IconFolder}/{file}.png", pixels, 16, new Vector2(0.5f, 0.5f));

            Bait bread = Make("MieDePain", "Mie de pain", "Gratuite et toujours dans ta poche. Les poissons blancs l'adorent.",
                0, 0, Pick("rutilus-rutilus", "alburnus-alburnus", "squalius-cephalus", "cyprinus-carpio", "abudefduf-vaigiensis", "colossoma-macropomum", "astronotus-ocellatus"),
                icon: Icon("Appat_0", GearArt.Bait(0)));
            Bait worm = Make("VerDeTerre", "Ver de terre", "Le classique : tout le monde mord un peu plus vite.",
                20, 15, Pick("perca-fluviatilis", "salmo-trutta", "tinca-tinca", "rutilus-rutilus", "coregonus-lavaretus",
                    "pelophylax-ridibundus", "mulloidichthys-flavolineatus", "pterygoplichthys-pardalis", "symphysodon-aequifasciatus"),
                biteWait: 0.85f, icon: Icon("Appat_Ver", GearArt.Worm()));
            Bait shrimp = Make("Crevette", "Crevette", "Crustacés et poissons du récif ne lui résistent pas.",
                35, 12, Pick("scylla-serrata", "panulirus-versicolor", "pacifastacus-leniusculus", "cephalopholis-argus",
                    "lutjanus-kasmira", "rhinecanthus-aculeatus", "chaetodon-auriga", "albula-glossodonta", "pseudoplatystoma-fasciatum", "phractocephalus-hemioliopterus"),
                biteWait: 0.9f, icon: Icon("Appat_1", GearArt.Bait(1)));
            Bait lure = Make("LeurreBrillant", "Leurre brillant", "Réutilisable. Les chasseurs foncent dessus, les autres l'ignorent.",
                140, 0, Pick("esox-lucius", "sphyraena-barracuda", "caranx-melampygus", "caranx-ignobilis", "salvelinus-umbla",
                    "salmo-trutta", "perca-fluviatilis", "cichla-ocellaris", "hydrolycus-scomberoides", "osteoglossum-bicirrhosum", "pygocentrus-nattereri"),
                favoritesOnly: true, icon: Icon("Appat_2", GearArt.Bait(2)));
            Bait glow = Make("AppatLumineux", "Appât lumineux", "La nuit, les espèces nocturnes le repèrent de loin.",
                45, 10, Pick("lota-lota", "tinca-tinca", "pterois-radiata", "electrophorus-electricus", "macrobrachium-amazonicum"),
                nightBonus: 3f, biteWait: 0.9f, icon: Icon("Appat_Lumineux", GearArt.GlowBait()));
            Bait squid = Make("Calmar", "Calmar entier", "Un gros morceau pour les gros poissons rares.",
                90, 6, Pick("caranx-ignobilis", "chelonia-mydas", "sphyraena-barracuda", "cephalopholis-argus", "arapaima-gigas", "chelus-fimbriata"),
                rareBonus: 2.2f, icon: Icon("Appat_3", GearArt.Bait(3)));

            var catalog = AssetDatabase.LoadAssetAtPath<BaitCatalog>($"{Folder}/CatalogueAppats.asset");
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BaitCatalog>();
                AssetDatabase.CreateAsset(catalog, $"{Folder}/CatalogueAppats.asset");
            }

            catalog.Configure(new[] { bread, worm, shrimp, lure, glow, squid });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static Bait Make(string file, string name, string description, int price, int uses, FishSpecies[] favorites,
            float rareBonus = 1f, float nightBonus = 1f, bool favoritesOnly = false, float biteWait = 1f, Sprite icon = null)
        {
            string path = $"{Folder}/{file}.asset";
            var bait = AssetDatabase.LoadAssetAtPath<Bait>(path);
            if (bait == null)
            {
                bait = ScriptableObject.CreateInstance<Bait>();
                AssetDatabase.CreateAsset(bait, path);
            }

            bait.Configure(name, description, price, uses, favorites, rareBonus, nightBonus, favoritesOnly, biteWait, icon);
            EditorUtility.SetDirty(bait);
            return bait;
        }
    }
}
