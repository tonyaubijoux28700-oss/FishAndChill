using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.World;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Crée ou met à jour les poissons de légende (Data/Legendes) et dessine leurs artefacts (Art/Legendes).
    /// Tuna, la murène géante de l'île Mahana (Idole de corail), Bramafan, l'écrevisse titan du Lac d'Annecy (Médaillon
    /// des palafittes), et Ipupiara, le payara titan du Fleuve Amazone (Grenouille de jade), chacun avec un artefact en 4 fragments.
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
            return new[] { tuna, Bramafan(ppu, pivot), Ipupiara(ppu, pivot) };
        }

        /// <summary>
        /// Ipupiara, le payara titan du Fleuve Amazone (espèce de base : le payara, le « poisson-vampire » aux deux crocs).
        /// « Ipupiara » est le nom tupi d'un monstre des eaux décrit par les premiers voyageurs au Brésil. Artefact : la
        /// Grenouille de jade, en 4 fragments (d'après les amulettes-grenouilles de pierre verte des peuples de l'Amazone).
        /// </summary>
        private static LegendFish Ipupiara(int ppu, Vector2 pivot)
        {
            Sprite frog = PixelArt.Save($"{ArtFolder}/Grenouille_Jade.png", JadeFrogArt.Frog(), ppu, pivot);
            Sprite silhouette = PixelArt.Save($"{ArtFolder}/Grenouille_Jade_Silhouette.png", JadeFrogArt.Silhouette(), ppu, pivot);
            var fragments = new Sprite[JadeFrogArt.Fragments];
            var centers = new Vector2[JadeFrogArt.Fragments];
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i] = PixelArt.Save($"{ArtFolder}/Grenouille_Jade_Fragment_{i + 1}.png", JadeFrogArt.Piece(i), ppu, pivot);
                centers[i] = JadeFrogArt.PieceCenter(i);
            }

            var species = FindSpecies("hydrolycus-scomberoides", "Assets/_Project/Data/Species/HydrolycusScomberoides.asset");
            var zone = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/FleuveAmazone.asset");
            LegendFish ipupiara = LoadOrCreate(Folder + "/Ipupiara.asset");
            ipupiara.Configure("ipupiara", "Ipupiara", species, zone, 340f, "Grenouille de jade", JadeFrogArt.Fragments,
                FragmentRules.DefaultChance, frog, fragments, centers, silhouette, new[]
                {
                    "La tête d'une grenouille en pierre verte, aux yeux d'or. Un bout de cordon tressé passe encore dans le trou.",
                    "Une patte de jade repliée. Deux trous ronds la traversent, comme percés par des crocs.",
                    "Un morceau du dos, gravé de chevrons. La pierre est froide comme l'eau du fond.",
                    "Les pattes arrière. Gravé dessous : « Ipupiara chasse là où le fleuve est noir ».",
                });
            Sprite art = PixelArt.Save($"{ArtFolder}/Ipupiara.png", PayaraArt.Draw(), ppu, pivot);
            ipupiara.ConfigureLook("Payara titan", art, BodyShape.Payara);
            // Le combat, d'après les vrais payaras : il fonce dans le courant, bondit hors de l'eau en secouant la tête,
            // puis plonge au fond et tire de tout son poids. Plus dur que Bramafan.
            ipupiara.ConfigureFight(new[]
            {
                new LegendPhase(FightStyle.Rusher, new QteDifficulty(1.45f, 0.18f, 4, 2), "Il fonce dans le courant du fleuve !"),
                new LegendPhase(FightStyle.Nervous, new QteDifficulty(1.75f, 0.15f, 4, 2), "Il bondit hors de l'eau en secouant la tête !"),
                new LegendPhase(FightStyle.Heavy, new QteDifficulty(1.3f, 0.19f, 5, 2), "Il plonge au fond et tire de tout son poids !"),
            }, 1000);
            EditorUtility.SetDirty(ipupiara);
            return ipupiara;
        }

        /// <summary>
        /// Bramafan, l'écrevisse titan du Lac d'Annecy (espèce de base : l'écrevisse signal, qui a vraiment colonisé le
        /// lac). « Bramafan » veut dire « qui crie famine » en patois savoyard. Artefact : le Médaillon des palafittes, en
        /// 4 fragments (le lac garde les restes de villages sur pilotis de l'âge du bronze).
        /// </summary>
        private static LegendFish Bramafan(int ppu, Vector2 pivot)
        {
            Sprite medallion = PixelArt.Save($"{ArtFolder}/Medaillon_Palafittes.png", MedallionArt.Medallion(), ppu, pivot);
            Sprite silhouette = PixelArt.Save($"{ArtFolder}/Medaillon_Palafittes_Silhouette.png", MedallionArt.Silhouette(), ppu, pivot);
            var fragments = new Sprite[MedallionArt.Fragments];
            var centers = new Vector2[MedallionArt.Fragments];
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i] = PixelArt.Save($"{ArtFolder}/Medaillon_Palafittes_Fragment_{i + 1}.png", MedallionArt.Piece(i), ppu, pivot);
                centers[i] = MedallionArt.PieceCenter(i);
            }

            var species = FindSpecies("pacifastacus-leniusculus", "Assets/_Project/Data/Species/PacifastacusLeniusculus.asset");
            var zone = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/LacAnnecy.asset");
            LegendFish bramafan = LoadOrCreate(Folder + "/Bramafan.asset");
            bramafan.Configure("bramafan", "Bramafan", species, zone, 380f, "Médaillon des palafittes", MedallionArt.Fragments,
                FragmentRules.DefaultChance, medallion, fragments, centers, silhouette, new[]
                {
                    "Un quartier de disque en bronze verdi, avec un anneau. Une pince gravée s'enroule sur le métal.",
                    "Un morceau du bord perlé. Des traces de griffes énormes ont rayé le bronze.",
                    "Une moitié d'ambre sertie brille dans ce fragment, comme une braise sous l'eau froide.",
                    "Le dernier quartier. On y lit, gravé : « Bramafan a faim. Bramafan attend sous les pilotis ».",
                });
            Sprite art = PixelArt.Save($"{ArtFolder}/Bramafan.png", LakeCrayfishArt.Draw(), ppu, pivot);
            bramafan.ConfigureLook("Écrevisse titan", art, BodyShape.Crayfish);
            // Le combat, d'après les vraies écrevisses : elle fuit à reculons d'un grand coup de queue, s'agrippe au fond
            // avec ses pinces, puis se retourne et claque des pinces dans tous les sens. Un peu plus dure que Tuna.
            bramafan.ConfigureFight(new[]
            {
                new LegendPhase(FightStyle.Rusher, new QteDifficulty(1.35f, 0.19f, 3, 2), "Il file à reculons d'un grand coup de queue !"),
                new LegendPhase(FightStyle.Heavy, new QteDifficulty(1.2f, 0.20f, 4, 2), "Il plante ses pinces dans les galets du fond !"),
                new LegendPhase(FightStyle.Nervous, new QteDifficulty(1.65f, 0.16f, 4, 2), "Il claque des pinces dans tous les sens !"),
            }, 800);
            EditorUtility.SetDirty(bramafan);
            return bramafan;
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
