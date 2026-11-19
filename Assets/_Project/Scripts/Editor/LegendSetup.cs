using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.World;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Crée ou met à jour les poissons de légende (Data/Legendes) et dessine leurs artefacts (Art/Legendes).
    /// Pour l'instant : Tuna, la murène géante de l'île Mahana, et son Idole de corail en 4 fragments.
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

            // Tuna : une murène javanaise monstrueuse (la vraie vit aussi en Polynésie et dépasse rarement 3 m), qui porte
            // le nom de l'anguille géante d'une légende polynésienne (de sa tête enterrée serait né le premier cocotier).
            var species = FindSpecies("gymnothorax-javanicus", "Assets/_Project/Data/Species/GymnothoraxJavanicus.asset");
            var zone = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/IleMahana.asset");
            const string path = Folder + "/Tuna.asset";
            if (AssetDatabase.LoadAssetAtPath<LegendFish>(path) == null && AssetDatabase.LoadAssetAtPath<LegendFish>($"{Folder}/VieuxUlua.asset") != null)
            {
                AssetDatabase.MoveAsset($"{Folder}/VieuxUlua.asset", path);   // l'ancienne fiche (même GUID, la scène reste reliée)
            }

            LegendFish tuna = LoadOrCreate(path);
            tuna.Configure("tuna", "Tuna", species, zone, 420f, "Idole de corail", LegendArt.IdolFragments,
                FragmentRules.DefaultChance, idol, fragments, centers, silhouette, new[]
                {
                    "Un morceau de corail rose, sculpté d'une couronne. Il est étrangement tiède dans ta main.",
                    "Un des yeux de nacre de l'idole, serti d'or, te fixe depuis ce fragment. On jurerait qu'il bouge.",
                    "Un bras sculpté, poli par la mer. Une longue dent recourbée y est restée plantée.",
                    "Le socle de l'idole. Gravé dessous : « Tuna, gardien du lagon, dort sous la passe ».",
                });
            Sprite art = PixelArt.Save($"{ArtFolder}/Tuna.png", MorayArt.Draw(), ppu, pivot);
            tuna.ConfigureLook("Murène géante", art, BodyShape.Moray);
            // Le combat, d'après les vraies murènes : elle file se réfugier dans son trou, puis s'enroule autour du corail
            // et tire de tout son poids, et finit par se tordre dans tous les sens.
            tuna.ConfigureFight(new[]
            {
                new LegendPhase(FightStyle.Rusher, new QteDifficulty(1.25f, 0.20f, 3, 2), "Il file se réfugier dans sa grotte !"),
                new LegendPhase(FightStyle.Heavy, new QteDifficulty(1.1f, 0.22f, 4, 2), "Il s'enroule autour du corail et tire !"),
                new LegendPhase(FightStyle.Nervous, new QteDifficulty(1.55f, 0.17f, 4, 2), "Il se tord dans tous les sens !"),
            }, 600);
            EditorUtility.SetDirty(tuna);
            return new[] { tuna };
        }

        private static FishSpecies FindSpecies(string id, string path)
        {
            var species = AssetDatabase.LoadAssetAtPath<FishSpecies>(path);
            if (species != null)
            {
                return species;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:FishSpecies"))
            {
                var s = AssetDatabase.LoadAssetAtPath<FishSpecies>(AssetDatabase.GUIDToAssetPath(guid));
                if (s != null && s.Id == id) return s;
            }

            return null;
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
