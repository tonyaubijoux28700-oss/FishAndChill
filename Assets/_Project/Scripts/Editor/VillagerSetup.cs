using FishAndChill.Collection;
using FishAndChill.Player;
using FishAndChill.Villagers;
using FishAndChill.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les habitants de l'île (<see cref="VillagerProfile"/>, dans <c>Data/Habitants/</c>) : apparence, emploi du temps,
    /// balade et répliques ; puis leur pose dans la scène (<see cref="Place"/>) : un <see cref="Villager"/> par habitant,
    /// relié à sa maison, à son portrait d'intérieur et à son coin préféré.
    /// </summary>
    public static class VillagerSetup
    {
        private const string Folder = "Assets/_Project/Data/Habitants";

        /// <summary>Rôle dans <see cref="HomeArt.Residents"/> → identifiant du profil.</summary>
        public static string IdOfRole(string role)
        {
            switch (role)
            {
                case "Vieux pêcheur": return "tavita";
                case "Maman": return "hinano";
                case "Enfant": return "manu";
                case "Ama": return "chiyo";
                default: return "rose";
            }
        }

        private static CharacterLook Look(int skin, int hairStyle, int hairColor, int top, int topColor, int bottom) =>
            new CharacterLook { Skin = skin, HairStyle = hairStyle, HairColor = hairColor, Hat = 0, Top = top, TopColor = topColor, Bottom = bottom };

        public static VillagerProfile[] CreateOrUpdate()
        {
            PixelArt.EnsureFolder(Folder);
            VillagerProfile[] profiles =
            {
                Save("tavita", "Tavita", Look(3, 0, 5, 1, 2, 1),
                    new NpcExtras { Beard = new Color(0.86f, 0.86f, 0.84f), Beanie = new Color(0.22f, 0.36f, 0.66f), Pants = new Color(0.24f, 0.30f, 0.46f) },
                    7f, 17.5f, 4f, 1.3f, 3f, 8f,
                    new VillagerLines
                    {
                        Intro = new[]
                        {
                            "Tiens, une nouvelle tête sur le ponton ! Moi c'est Tavita. Ça fait cinquante ans que je pêche autour de Mahana.",
                            "Un conseil de vieux loup de mer : regarde l'eau avant de lancer. Les ombres qui bougent, ce sont des poissons.",
                            "Et si un jour tu vois l'eau tourner toute seule... file au laboratoire. Ce n'est pas un poisson ordinaire.",
                        },
                        Daily = new[]
                        {
                            "Le matin, l'eau est calme. C'est là que les gros sortent.",
                            "J'ai déjà vu un mérou aussi gros que ma barque. Si, si, je te jure !",
                            "Ferre au bon moment, petit. Trop tôt ou trop tard, et il repart avec ton appât.",
                            "Un bon pêcheur, c'est surtout un pêcheur patient.",
                            "Mon bonnet ? Je l'ai depuis quarante ans. Il porte bonheur.",
                        },
                        Rain = new[]
                        {
                            "La pluie, ça réveille les poissons. Ne rentre surtout pas maintenant !",
                            "Sous l'orage, certains poissons qu'on ne voit jamais remontent. Mais fais attention à toi.",
                        },
                        Night = new[]
                        {
                            "La nuit, ce ne sont plus les mêmes poissons qui mordent. Essaie, tu verras.",
                        },
                        Aquarium = new[]
                        {
                            "Il paraît que ton aquarium a déjà {0} espèces. Pas mal pour un débutant !",
                            "{0} espèces à l'aquarium... Moi, à ton âge, je n'en connaissais pas la moitié.",
                        },
                    }),
                Save("hinano", "Hinano", Look(4, 3, 1, 0, 1, 3),
                    new NpcExtras { Skirt = new Color(0.80f, 0.24f, 0.26f), HairFlower = new Color(1f, 0.46f, 0.56f) },
                    9f, 18f, 4.5f, 1.4f, 2f, 6f,
                    new VillagerLines
                    {
                        Intro = new[]
                        {
                            "Bonjour ! Je suis Hinano, j'habite la maison rose avec mon fils Manu.",
                            "C'est toi qui remplis le grand aquarium ? Manu en parle du matin au soir, il veut tout voir !",
                        },
                        Daily = new[]
                        {
                            "Manu, ne cours pas si vite ! Pardon, il a toujours de l'énergie à revendre.",
                            "Le parvis de l'aquarium est mon endroit préféré. On y entend la mer.",
                            "Le marchand du port est un vieux grincheux, mais ses prix sont honnêtes.",
                            "Tu as vu l'animalerie ? Manu voudrait un chat. Moi, je dis : on verra.",
                        },
                        Rain = new[]
                        {
                            "Encore de la pluie ! Heureusement, les fleurs de l'île adorent ça.",
                        },
                        Night = new[]
                        {
                            "Il se fait tard, Manu devrait déjà être couché...",
                        },
                        Aquarium = new[]
                        {
                            "{0} espèces à l'aquarium ! Manu les connaît toutes par leur nom.",
                        },
                    }),
                Save("manu", "Manu", Look(4, 1, 0, 3, 8, 0),
                    new NpcExtras { Child = true },
                    9f, 18f, 2f, 1.9f, 0.4f, 1f,
                    new VillagerLines
                    {
                        Intro = new[]
                        {
                            "Salut ! Moi c'est Manu, j'ai sept ans et demi !",
                            "C'est vrai que tu as attrapé un poisson géant ? Tu me montreras ? Promis ?",
                        },
                        Daily = new[]
                        {
                            "Tu sais pourquoi les poissons-clowns vivent dans les anémones ? Moi oui ! ... En fait non.",
                            "Quand je serai grand, je serai pêcheur. Ou scientifique. Ou les deux !",
                            "Maman dit que les requins ne mangent pas les enfants. Elle est sûre ?",
                            "J'ai trouvé un coquillage rose ce matin. C'est mon trésor.",
                        },
                        Rain = new[]
                        {
                            "J'adore la pluie ! On peut sauter dans les flaques !",
                        },
                        Night = new[]
                        {
                            "Je ne suis pas fatigué... (il bâille)",
                        },
                        Aquarium = new[]
                        {
                            "{0} espèces ! Mon préféré, c'est celui avec les grandes nageoires. Tu sais lequel ?",
                            "Quand l'aquarium sera plein, je pourrai venir tous les jours ?",
                        },
                    }),
                Save("rose", "Mamie Rose", Look(1, 5, 5, 1, 5, 2),
                    new NpcExtras { Skirt = new Color(0.52f, 0.52f, 0.56f), Glasses = true },
                    10f, 17f, 3f, 0.9f, 4f, 10f,
                    new VillagerLines
                    {
                        Intro = new[]
                        {
                            "Bonjour, mon petit. Je suis Rose, mais tout le monde m'appelle Mamie Rose.",
                            "Je suis née ici, sur Mahana. À l'époque, il n'y avait ni aquarium ni boutique, juste des pirogues et du soleil.",
                        },
                        Daily = new[]
                        {
                            "Ma mère disait qu'un poisson géant dort au large de l'île. Les anciens l'appelaient le gardien.",
                            "Viens prendre une tasse de thé un de ces jours. Ma porte est toujours ouverte.",
                            "Mon chat dort toute la journée devant la cheminée. Sage animal.",
                            "Le phare était déjà là quand j'étais petite. Il a vu passer bien des tempêtes.",
                        },
                        Rain = new[]
                        {
                            "Ah, la pluie... Mes vieux genoux l'avaient sentie venir dès ce matin.",
                        },
                        Night = new[]
                        {
                            "À mon âge, on se couche avec les poules. Bonne nuit, mon petit.",
                        },
                        Aquarium = new[]
                        {
                            "{0} espèces dans ton aquarium ! De mon temps, on les mangeait, maintenant on les admire.",
                        },
                    }),
                // Chiyo, la vieille ama de la baie d'Izu : toujours chez elle (sortie et retour à la même heure).
                Save("chiyo", "Chiyo", Look(1, 5, 5, 1, 8, 1),
                    new NpcExtras { Pants = new Color(0.24f, 0.30f, 0.48f), Headscarf = new Color(0.97f, 0.97f, 0.95f) },
                    0f, 0f, 1f, 1f, 2f, 4f,
                    new VillagerLines
                    {
                        Intro = new[]
                        {
                            "Hm. Encore un touriste qui tient sa canne comme un parapluie.",
                            "Je m'appelle Chiyo. Ici, on dit Chiyo-obaasan. J'ai plongé dans cette baie pendant soixante ans.",
                        },
                        NetGift = new[]
                        {
                            "Tiens. Mon épuisette. Mes vieux bras n'en ont plus besoin.",
                            "Tu vois les petites bulles près des rives ? Quelque chose fouille le sable là-dessous. Plonge l'épuisette dedans.",
                            "Quand la bête est prise, remonte-la doucement : appuie quand l'anneau la touche. Trop tôt, trop tard, et elle file.",
                            "Et prends mon carnet. Je ne l'ai jamais fini. Une page par bête... Remplis-le pour moi, veux-tu ?",
                        },
                        Daily = new[]
                        {
                            "La mer ne donne rien à ceux qui se pressent.",
                            "Une flaque, une bête. Le reste, tu le laisses pour demain.",
                            "Mes perles ? Hm. Une autre fois.",
                            "Le thé est chaud. Assieds-toi si tu veux, mais ne parle pas trop.",
                            "Les crabes, il faut les prendre par-derrière. Comme les gens pressés.",
                        },
                        Rain = new[]
                        {
                            "La pluie, c'est de la mer qui revient du ciel. Elle ne me fait pas peur.",
                        },
                        Night = new[]
                        {
                            "La nuit, les flaques changent de locataires. Les timides sortent.",
                        },
                        Aquarium = new[]
                        {
                            "{0} espèces derrière une vitre... Moi, je préfère les voir dans leur flaque.",
                        },
                    }),
            };

            // Commandes : zones où chacun choisit ses poissons, et ce qu'il dit (« {0} » = nom du poisson).
            Requests(profiles[0], new[] { "IleMahana", "Maldives" },
                new[] { "Dis donc, j'ai une envie de poisson grillé. Celui-là : {0}. Tu m'en rapporterais un ?", "Tu sais ce qui me ferait plaisir ? Une belle prise : {0}. Comme au bon vieux temps." },
                new[] { "Toujours rien pour ma commande ({0}) ? Patience, ça finira par mordre.", "Je pense encore à ma commande : {0}..." },
                new[] { "Ah, {0} ! Tu as l'oeil, petit. Tiens, c'est pour toi." },
                new[] { "C'est ton seul exemplaire ({0}) ? Mets-le d'abord dans ton aquarium, je peux attendre." });
            Requests(profiles[1], new[] { "LacAnnecy" },
                new[] { "Je voudrais préparer un plat de chez ma mère. Il me faudrait ce poisson du lac : {0}. Tu pourrais m'aider ?" },
                new[] { "Le poisson pour mon plat ({0}), tu n'as pas oublié ?" },
                new[] { "Merci mille fois ! Avec ça, ce soir ce sera la fête." },
                new[] { "Oh, c'est ton premier exemplaire ({0}) ? Garde-le pour l'aquarium, Manu serait déçu sinon !" });
            Requests(profiles[2], new[] { "GrandeBarriere", "FleuveAmazone" },
                new[] { "Dis, tu pourrais m'attraper ce poisson : {0} ? C'est pour montrer à mes copains !", "J'ai vu ce poisson dans un livre : {0} ! Tu peux en attraper un ? Dis oui !" },
                new[] { "Alors, mon poisson ({0}) ? Tu l'as trouvé ?", "J'en rêve toutes les nuits : {0}..." },
                new[] { "Waouh, un vrai de vrai ! T'es le meilleur ! Tiens, c'est toute ma tirelire !" },
                new[] { "Non, non, ton seul exemplaire ({0}), il doit aller dans le grand aquarium d'abord !" });
            Requests(profiles[3], new[] { "BaieJaponaise" },
                new[] { "Mon petit, il me faudrait ce poisson pour ma soupe : {0}. Ma vieille recette de famille." },
                new[] { "Ma commande ({0}), quand tu auras un moment. Rien ne presse." },
                new[] { "Tout frais ! Tu es un amour. Prends ça, et reviens goûter la soupe." },
                new[] { "Ton seul exemplaire ({0}) ? Montre-le d'abord à tout le monde dans l'aquarium, voyons." });
            return profiles;
        }

        private static void Requests(VillagerProfile profile, string[] zones, string[] ask, string[] waiting, string[] thanks, string[] keep)
        {
            var so = new SerializedObject(profile);
            SerializedProperty list = so.FindProperty("_zones");
            list.arraySize = zones.Length;
            for (int i = 0; i < zones.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<FishingZone>($"Assets/_Project/Data/Zones/{zones[i]}.asset");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            profile.Lines.Ask = ask;
            profile.Lines.Waiting = waiting;
            profile.Lines.Thanks = thanks;
            profile.Lines.KeepIt = keep;
            EditorUtility.SetDirty(profile);
        }

        private static VillagerProfile Save(string id, string displayName, CharacterLook look, NpcExtras extras, float leave, float home,
            float radius, float speed, float minPause, float maxPause, VillagerLines lines)
        {
            string path = $"{Folder}/{displayName.Replace(' ', '_')}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VillagerProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VillagerProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            var so = new SerializedObject(profile);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_leaveHour").floatValue = leave;
            so.FindProperty("_homeHour").floatValue = home;
            so.FindProperty("_wanderRadius").floatValue = radius;
            so.FindProperty("_speed").floatValue = speed;
            so.FindProperty("_minPause").floatValue = minPause;
            so.FindProperty("_maxPause").floatValue = maxPause;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Les objets imbriqués (apparence, accessoires, répliques) passent directement par le profil.
            var field = typeof(VillagerProfile).GetField("_look", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(profile, look);
            typeof(VillagerProfile).GetField("_extras", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(profile, extras);
            typeof(VillagerProfile).GetField("_lines", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(profile, lines);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        /// <summary>Portrait de face d'un habitant (debout chez lui), enregistré dans <c>Art/Characters/</c>.</summary>
        public static Sprite Portrait(VillagerProfile profile)
        {
            return PixelArt.Save($"Assets/_Project/Art/Characters/Habitant_{profile.Id}.png",
                CharacterPainter.Front(profile.Look, profile.Extras, 0, back: false), CharacterSprites.PixelsPerUnit, CharacterSprites.Pivot);
        }

        /// <summary>
        /// Pose les habitants : chacun relié à sa maison (et à son portrait à l'intérieur), à son coin préféré, à la carte,
        /// au joueur, à l'heure, à la météo et à la collection. L'enfant suit sa maman.
        /// </summary>
        public static void Place(VillagerProfile[] profiles, AquariumBuilding aquarium, Vector2 dockArrival, WorldMap map, PlayerMovement player,
            DayNightCycle clock, WeatherSystem weather, PlayerProgress progress)
        {
            GameObject root = GameObject.Find("Habitants");
            if (root == null) root = new GameObject("Habitants");
            Sprite exclamation = PixelArt.Save("Assets/_Project/Art/UI/Bulle_Commande.png", BubbleArt.Exclamation(), CharacterSprites.PixelsPerUnit, BubbleArt.ExclamationPivot);
            Sprite fishBubble = PixelArt.Save("Assets/_Project/Art/UI/Bulle_Poisson.png", BubbleArt.Wide(), CharacterSprites.PixelsPerUnit, BubbleArt.WidePivot);
            var placed = new Villager[profiles.Length];
            for (int i = 0; i < profiles.Length; i++)
            {
                VillagerProfile profile = profiles[i];
                VillagerHouseBuilding home = HomeOf(profile.Id, out SpriteRenderer indoor);
                Vector2 anchor;
                switch (profile.Id)
                {
                    case "tavita": anchor = dockArrival + new Vector2(0f, 3f); break;
                    case "rose": anchor = home != null ? (Vector2)home.transform.position + new Vector2(0f, -2.6f) : dockArrival; break;
                    default: anchor = aquarium != null ? (Vector2)aquarium.transform.position + new Vector2(0f, -3.6f) : dockArrival; break;
                }

                Transform existing = root.transform.Find(profile.DisplayName);
                GameObject go = existing != null ? existing.gameObject : new GameObject(profile.DisplayName);
                go.transform.SetParent(root.transform, false);
                go.transform.position = anchor;
                var renderer = go.GetComponent<SpriteRenderer>();
                if (renderer == null) renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = Portrait(profile);
                renderer.sortingOrder = 10;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                if (indoor != null) indoor.sprite = renderer.sprite;

                var villager = go.GetComponent<Villager>();
                if (villager == null) villager = go.AddComponent<Villager>();
                var so = new SerializedObject(villager);
                so.FindProperty("_profile").objectReferenceValue = profile;
                so.FindProperty("_home").objectReferenceValue = home;
                so.FindProperty("_indoor").objectReferenceValue = indoor;
                so.FindProperty("_anchor").vector2Value = anchor;
                so.FindProperty("_map").objectReferenceValue = map;
                so.FindProperty("_player").objectReferenceValue = player;
                so.FindProperty("_clock").objectReferenceValue = clock;
                so.FindProperty("_weather").objectReferenceValue = weather;
                so.FindProperty("_progress").objectReferenceValue = progress;
                so.FindProperty("_seed").intValue = 17 + i * 31;
                so.FindProperty("_exclamation").objectReferenceValue = exclamation;
                so.FindProperty("_fishBubble").objectReferenceValue = fishBubble;
                so.ApplyModifiedPropertiesWithoutUndo();
                placed[i] = villager;
            }

            // Manu suit sa maman.
            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i].Id != "manu") continue;
                for (int j = 0; j < profiles.Length; j++)
                {
                    if (profiles[j].Id != "hinano") continue;
                    var so = new SerializedObject(placed[i]);
                    so.FindProperty("_leader").objectReferenceValue = placed[j];
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        /// <summary>La maison où vit l'habitant, et son portrait debout à l'intérieur (« Habitant id », posé par <c>MapSetup</c>).</summary>
        private static VillagerHouseBuilding HomeOf(string id, out SpriteRenderer indoor)
        {
            indoor = null;
            for (int house = 1; house <= 9; house++)
            {
                GameObject room = GameObject.Find($"Maison d'habitant {house} - Intérieur");
                Transform resident = room != null ? room.transform.Find($"Habitant {id}") : null;
                if (resident == null) continue;
                indoor = resident.GetComponent<SpriteRenderer>();
                GameObject building = GameObject.Find($"Maison d'habitant {house}");
                return building != null ? building.GetComponent<VillagerHouseBuilding>() : null;
            }

            return null;
        }
    }
}
