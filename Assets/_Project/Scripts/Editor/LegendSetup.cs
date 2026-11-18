using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.World;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Crée ou met à jour les poissons de légende (Data/Legendes) et dessine leurs artefacts (Art/Legendes).
    /// Pour l'instant : le Vieux Ulua, carangue géante de l'île Mahana, et son Idole de corail en 4 fragments.
    /// Appelé par le menu du prototype, après la création des espèces et des zones.
    /// </summary>
    public static class LegendSetup
    {
        private const string Folder = "Assets/_Project/Data/Legendes";
        private const string ArtFolder = "Assets/_Project/Art/Legendes";

        public static LegendFish[] CreateOrUpdate()
        {
            PixelArt.EnsureFolder(Folder);
            PixelArt.EnsureFolder(ArtFolder);
            var pivot = new Vector2(0.5f, 0.5f);
            int ppu = VolumeCanvas.PixelsPerUnit;

            Sprite idol = PixelArt.Save($"{ArtFolder}/Idole_Corail.png", LegendArt.Idol(), ppu, pivot);
            Sprite silhouette = PixelArt.Save($"{ArtFolder}/Idole_Corail_Silhouette.png", LegendArt.IdolSilhouette(), ppu, pivot);
            var fragments = new Sprite[LegendArt.IdolFragments];
            var centers = new Vector2[LegendArt.IdolFragments];
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i] = PixelArt.Save($"{ArtFolder}/Idole_Corail_Fragment_{i + 1}.png", LegendArt.IdolPiece(i), ppu, pivot);
                centers[i] = LegendArt.PieceCenter(i);
            }

            var species = AssetDatabase.LoadAssetAtPath<FishSpecies>("Assets/_Project/Data/Species/CaranxIgnobilis.asset");
            if (species == null)
            {
                foreach (string guid in AssetDatabase.FindAssets("t:FishSpecies"))
                {
                    var s = AssetDatabase.LoadAssetAtPath<FishSpecies>(AssetDatabase.GUIDToAssetPath(guid));
                    if (s != null && s.Id == "caranx-ignobilis") species = s;
                }
            }

            var zone = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/IleMahana.asset");
            LegendFish ulua = LoadOrCreate($"{Folder}/VieuxUlua.asset");
            ulua.Configure("vieux-ulua", "Le Vieux Ulua", species, zone, 210f, "Idole de corail", LegendArt.IdolFragments,
                FragmentRules.DefaultChance, idol, fragments, centers, silhouette, new[]
                {
                    "Un morceau de corail rose, sculpté d'une couronne. Il est étrangement tiède dans ta main.",
                    "Un des yeux de nacre de l'idole, serti d'or, te fixe depuis ce fragment. On jurerait qu'il bouge.",
                    "Un bras sculpté, poli par la mer. Des écailles d'argent géantes y sont restées accrochées.",
                    "Le socle de l'idole. Gravé dessous : « Ulua, roi du lagon, dort sous la passe ».",
                });
            EditorUtility.SetDirty(ulua);
            return new[] { ulua };
        }

        private static LegendFish LoadOrCreate(string path)
        {
            var legend = AssetDatabase.LoadAssetAtPath<LegendFish>(path);
            if (legend == null)
            {
                legend = ScriptableObject.CreateInstance<LegendFish>();
                AssetDatabase.CreateAsset(legend, path);
            }

            return legend;
        }
    }
}
