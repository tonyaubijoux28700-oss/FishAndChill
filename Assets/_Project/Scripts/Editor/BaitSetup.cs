using System.Collections.Generic;
using FishAndChill.Gear;
using UnityEditor;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Crée ou met à jour les leurres et appâts du marchand et leur catalogue (Data/Gear/Appats) : de vrais modèles,
    /// rangés par type (appâts naturels, cuillères tournantes, ondulantes, poissons nageurs, leurres souples, surface,
    /// jigs, turluttes, traîne, mouches), dessinés par <see cref="LureArt"/>. Chaque espèce mord à certains types
    /// (colonne Appats de la table, voir <see cref="LureRules"/>). Les appâts naturels se vendent en paquets, les leurres
    /// sont réutilisables. Appelé par le menu du prototype, après la création des espèces.
    /// </summary>
    public static class BaitSetup
    {
        private const string Folder = "Assets/_Project/Data/Gear/Appats";
        private const string IconFolder = "Assets/_Project/Art/UI/Materiel";

        public static BaitCatalog CreateOrUpdate()
        {
            PixelArt.EnsureFolder(Folder);
            Sprite Icon(string file, Color[,] pixels) => PixelArt.Save($"{IconFolder}/Leurre_{file}.png", pixels, 16, new Vector2(0.5f, 0.5f));
            var all = new List<Bait>();
            void Add(string id, string name, LureType type, string model, string description, int price, int uses, Color[,] art,
                float rare = 1f, float night = 1f, float wait = 1f)
            {
                all.Add(Make(id, name, type, model, description, price, uses, Icon(id, art), rare, night, wait));
            }

            Color silver = new Color(0.86f, 0.88f, 0.92f), gold = new Color(0.94f, 0.76f, 0.30f), copper = new Color(0.80f, 0.46f, 0.24f);

            // Appâts naturels (le premier est l'appât de départ, gratuit et illimité).
            Add("VerDeTerreau", "Ver de terreau", LureType.Natural, "Gratuit, toujours dans ta poche.",
                "Le plus simple des appâts : beaucoup de poissons y mordent.", 0, 0, LureArt.Worm());
            Add("Asticots", "Asticots", LureType.Natural, "Les poissons blancs mordent plus vite.",
                "Une poignée d'asticots qui gigotent : gardons, ablettes et carpes ne résistent pas.", 15, 20, LureArt.Maggots(), wait: 0.85f);
            Add("CrevetteGrise", "Crevette grise", LureType.Natural, "Fraîche : poissons côtiers et du récif.",
                "Une crevette entière piquée sur l'hameçon : tout ce qui vit près des rochers en raffole.", 35, 12, LureArt.Shrimp(), wait: 0.9f);
            Add("LaniereCalmar", "Lanière de calmar", LureType.Natural, "Solide : attire plus de gros poissons rares.",
                "Une lanière de calmar qui tient bien à l'hameçon : gros poissons de fond et requins.", 80, 8, LureArt.SquidStrip(), rare: 1.6f);

            // Cuillères tournantes.
            Add("PaletteAglia", "Cuillère palette Aglia", LureType.Spinner, "Palette ovale qui tourne vite, la plus polyvalente.",
                "La cuillère tournante la plus célèbre : sa palette brille et vibre, truites et perches la chassent.", 120, 0, LureArt.Spinner(0, silver));
            Add("PaletteColorado", "Cuillère palette Colorado", LureType.Spinner, "Palette ronde qui vibre fort en eau trouble.",
                "Une palette ronde dorée qui vibre lentement : idéale pour les brochets des eaux sombres.", 140, 0, LureArt.Spinner(1, gold), rare: 1.2f);
            Add("PaletteSaule", "Cuillère palette Saule", LureType.Spinner, "Palette effilée qui file vite et profond.",
                "Une palette longue en feuille de saule, cuivrée : elle descend vite dans le courant.", 160, 0, LureArt.Spinner(2, copper), wait: 0.9f);

            // Ondulantes.
            Add("OndulanteToby", "Ondulante Toby", LureType.Spoon, "Ondulation large grâce à ses ailettes.",
                "L'ondulante à ailettes des pêcheurs de saumon : elle zigzague comme un petit poisson blessé.", 180, 0, LureArt.Spoon(0));
            Add("OndulanteCrocodile", "Ondulante Crocodile", LureType.Spoon, "Longue et lourde, attire plus de rares.",
                "Une longue cuillère martelée qui se lance très loin : saumons, brochets et morues.", 200, 0, LureArt.Spoon(1), rare: 1.2f);
            Add("Devon", "Devon", LureType.Spoon, "Tube à hélice qui tourne en nageant.",
                "Un vieux leurre de rivière en métal, à ailettes : truites de mer et saumons le suivent jusqu'à la berge.", 220, 0, LureArt.Spoon(2), wait: 0.9f);

            // Poissons nageurs.
            Add("Minnow", "Minnow flottant", LureType.Minnow, "Nage près de la surface, très naturel.",
                "Un petit poisson de bois à bavette, qui nage en se dandinant : tous les carnassiers s'y trompent.", 200, 0, LureArt.Minnow(0));
            Add("Jerkbait", "Jerkbait", LureType.Minnow, "Animé par à-coups : plus de gros poissons rares.",
                "Un poisson nageur élancé qu'on fait partir par à-coups, comme une proie affolée.", 260, 0, LureArt.Minnow(1), rare: 1.3f);
            Add("Crankbait", "Crankbait plongeant", LureType.Minnow, "Grosse bavette : descend profond, touches rapides.",
                "Un poisson nageur trapu à grosse bavette qui plonge et vibre fort sous l'eau.", 240, 0, LureArt.Minnow(2), wait: 0.9f);

            // Leurres souples.
            Add("ShadPalette", "Shad à palette", LureType.SoftBait, "Queue qui vibre, monté sur tête plombée.",
                "Un poisson en caoutchouc à queue en palette, qui vibre en nageant : sandres, perches et mérous.", 150, 0, LureArt.SoftBait(0, new Color(0.90f, 0.92f, 0.88f)));
            Add("Finesse", "Ver finesse", LureType.SoftBait, "Discret : les méfiants mordent plus vite.",
                "Un ver souple tout fin, monté en texan, qu'on traîne sur le fond sans bruit.", 130, 0, LureArt.SoftBait(1, new Color(0.30f, 0.22f, 0.40f)), wait: 0.9f);
            Add("Grub", "Grub à queue vrillée", LureType.SoftBait, "Queue qui tourbillonne : plus de rares.",
                "Un petit leurre souple dont la queue en vrille ondule au moindre mouvement.", 140, 0, LureArt.SoftBait(2, new Color(0.96f, 0.80f, 0.20f)), rare: 1.2f);

            // Leurres de surface.
            Add("Popper", "Popper", LureType.Surface, "Sa bouche creuse fait éclabousser la surface.",
                "Un leurre de surface à la tête creuse : à chaque coup de canne il fait « ploc », et les carangues explosent dessus.", 220, 0, LureArt.Surface(0));
            Add("Stickbait", "Stickbait", LureType.Surface, "Cigare qui glisse en zigzag : plus de rares.",
                "Un long cigare flottant qui zigzague en surface : barracudas et thazards ne le lâchent pas.", 260, 0, LureArt.Surface(1), rare: 1.3f);
            Add("WalkingTheDog", "Walking-the-dog", LureType.Surface, "Nage en « promenade de chien ».",
                "Un leurre banane qui balance la tête de gauche à droite à la surface, comme un poisson qui fuit.", 240, 0, LureArt.Surface(2), wait: 0.9f);

            // Jigs métalliques.
            Add("Pilker", "Pilker norvégien", LureType.Jig, "Barre lourde qui descend vite au fond.",
                "La barre de métal des pêcheurs du Nord, qu'on fait danser au fond : morues, lieus et flétans.", 200, 0, LureArt.Jig(0));
            Add("CastingJig", "Casting jig", LureType.Jig, "Fin et holographique, se lance loin.",
                "Un jig fin qui brille de toutes les couleurs : il se lance très loin vers les chasses.", 230, 0, LureArt.Jig(1), wait: 0.9f);
            Add("SlowJig", "Slow jig", LureType.Jig, "Feuille plate qui papillonne : plus de rares.",
                "Une feuille de métal qui papillonne lentement en descendant, avec ses hameçons d'assist.", 280, 0, LureArt.Jig(2), rare: 1.3f);

            // Turluttes.
            Add("Egi", "Turlutte egi", LureType.SquidJig, "Crevette japonaise en tissu : calmars et seiches.",
                "La turlutte japonaise en forme de crevette, couronne d'aiguilles sans ardillon : les calmars l'attrapent avec leurs bras.", 180, 0, LureArt.SquidJig(0));
            Add("TurluttePhospho", "Turlutte phosphorescente", LureType.SquidJig, "Luit la nuit : les nocturnes la repèrent.",
                "Une turlutte qui luit dans le noir : la nuit, les calmars montent la voir de loin.", 160, 0, LureArt.SquidJig(1), night: 3f);

            // Traîne.
            Add("JupeOctopus", "Jupe octopus", LureType.Troll, "Tête de résine et jupe de lanières.",
                "Un leurre de traîne à tête de résine et jupe de lanières qui pulse derrière le bateau : thons et coryphènes.", 320, 0, LureArt.Troll(0));
            Add("PlumeTraine", "Plume de traîne", LureType.Troll, "Tête chromée et plumes : plus de rares.",
                "Une tête chromée et un panache de plumes qui fument dans le sillage : les grands pélagiques s'y jettent.", 300, 0, LureArt.Troll(1), rare: 1.3f);

            // Mouches.
            Add("MoucheAdams", "Mouche Adams", LureType.Fly, "Mouche sèche qui flotte.",
                "La mouche sèche la plus connue au monde, grise, qui flotte comme un insecte posé sur l'eau.", 90, 0, LureArt.Fly(0));
            Add("WoollyBugger", "Woolly Bugger", LureType.Fly, "Mouche noyée en marabout, touches rapides.",
                "Une mouche noyée noire à queue de marabout qui ondule : truites et ombles la prennent sans hésiter.", 110, 0, LureArt.Fly(1), wait: 0.9f);
            Add("StreamerClouser", "Streamer Clouser", LureType.Fly, "Imite un petit poisson : plus de rares.",
                "Un streamer à yeux haltères qui plonge et remonte comme un petit poisson : saumons et bars.", 130, 0, LureArt.Fly(2), rare: 1.2f);

            // Les anciens appâts disparaissent (les sauvegardes sont converties, LureRules.ConvertOldBait).
            foreach (string old in new[] { "MieDePain", "VerDeTerre", "Crevette", "LeurreBrillant", "AppatLumineux", "Calmar" })
            {
                AssetDatabase.DeleteAsset($"{Folder}/{old}.asset");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<BaitCatalog>($"{Folder}/CatalogueAppats.asset");
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BaitCatalog>();
                AssetDatabase.CreateAsset(catalog, $"{Folder}/CatalogueAppats.asset");
            }

            catalog.Configure(all);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static Bait Make(string file, string name, LureType type, string model, string description, int price, int uses, Sprite icon,
            float rareBonus, float nightBonus, float biteWait)
        {
            string path = $"{Folder}/{file}.asset";
            var bait = AssetDatabase.LoadAssetAtPath<Bait>(path);
            if (bait == null)
            {
                bait = ScriptableObject.CreateInstance<Bait>();
                AssetDatabase.CreateAsset(bait, path);
            }

            bait.Configure(name, description, price, uses, new Collection.FishSpecies[0], rareBonus, nightBonus, false, biteWait, icon);
            bait.ConfigureType(type, model);
            EditorUtility.SetDirty(bait);
            return bait;
        }
    }
}
