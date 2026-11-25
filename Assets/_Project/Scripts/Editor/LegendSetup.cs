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
    /// des palafittes), Ipupiara, le payara titan du Fleuve Amazone (Grenouille de jade), l'Isonade, la rascasse titan de
    /// la Baie d'Izu (Miroir des marées), Rannamaari, le djinn des abysses de l'Atoll des Maldives (Lanterne de
    /// corail), le Moha-moha, la tortue géante au long cou de la Grande Barrière (Bouclier d'écaille), le Spectre des
    /// épaves, requin scie squelette de la Côte des Squelettes (Cloche de l'épave), et le Kraken, calmar colossal des fjords
    /// de Norvège (Corne des marins), chacun avec un
    /// artefact en 4 fragments.
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
            return new[] { tuna, Bramafan(ppu, pivot), Ipupiara(ppu, pivot), Isonade(ppu, pivot), Rannamaari(ppu, pivot), MohaMoha(ppu, pivot), WreckSpectre(ppu, pivot), Kraken(ppu, pivot) };
        }

        /// <summary>
        /// Le Kraken, calmar colossal des fjords de Norvège (espèce de base : le calmar veiné). Dans les récits des marins
        /// norvégiens, il était si grand qu'on prenait son dos pour une île, et il entraînait les navires par le fond.
        /// Artefact : la Corne des marins, en 4 tronçons.
        /// </summary>
        private static LegendFish Kraken(int ppu, Vector2 pivot)
        {
            Sprite horn = PixelArt.Save($"{ArtFolder}/Corne_Marins.png", SeaHornArt.HornArt(), ppu, pivot);
            Sprite silhouette = PixelArt.Save($"{ArtFolder}/Corne_Marins_Silhouette.png", SeaHornArt.Silhouette(), ppu, pivot);
            var fragments = new Sprite[SeaHornArt.Fragments];
            var centers = new Vector2[SeaHornArt.Fragments];
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i] = PixelArt.Save($"{ArtFolder}/Corne_Marins_Fragment_{i + 1}.png", SeaHornArt.Piece(i), ppu, pivot);
                centers[i] = SeaHornArt.PieceCenter(i);
            }

            var species = FindSpecies("loligo-forbesii", "Assets/_Project/Data/Species/LoligoForbesii.asset");
            var zone = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/FjordsNorvege.asset");
            LegendFish kraken = LoadOrCreate(Folder + "/Kraken.asset");
            kraken.Configure("kraken", "Kraken", species, zone, 1400f, "Corne des marins", SeaHornArt.Fragments,
                FragmentRules.DefaultChance, horn, fragments, centers, silhouette, new[]
                {
                    "Le pavillon d'une corne d'appel en os, noirci par la mer. Une lueur dorée dort tout au fond.",
                    "Un tronçon cerclé d'argent. Les runes disent : « Qui souffle ici réveille ce qui dort sous l'île ».",
                    "Un morceau de la corne, couvert de petites cicatrices rondes, comme des marques de ventouses.",
                    "L'embouchure, cerclée d'argent. Les anciens marins ne la portaient jamais à leurs lèvres.",
                });
            Sprite art = PixelArt.Save($"{ArtFolder}/Kraken.png", KrakenArt.Draw(), ppu, pivot);
            kraken.ConfigureLook("Calmar colossal", art, BodyShape.Kraken);
            // Le combat : il enroule ses bras autour de la ligne, file vers le fond du fjord, puis se débat de ses dix
            // bras à la fois. Le plus dur de tous.
            kraken.ConfigureFight(new[]
            {
                new LegendPhase(FightStyle.Heavy, new QteDifficulty(1.9f, 0.12f, 7, 2), "Il enroule ses bras autour de la ligne !"),
                new LegendPhase(FightStyle.Rusher, new QteDifficulty(2.3f, 0.11f, 7, 2), "Il file vers le fond du fjord !"),
                new LegendPhase(FightStyle.Nervous, new QteDifficulty(2.5f, 0.11f, 7, 1), "Il se débat de ses dix bras à la fois !"),
            }, 3000);
            EditorUtility.SetDirty(kraken);
            return kraken;
        }

        /// <summary>
        /// Le Spectre des épaves, requin scie squelette de la Côte des Squelettes (espèce de base : le grand requin blanc).
        /// Mort depuis longtemps, il nage encore parmi les épaves de ce rivage où tant de navires se sont échoués.
        /// Artefact : la Cloche de l'épave, en 4 fragments.
        /// </summary>
        private static LegendFish WreckSpectre(int ppu, Vector2 pivot)
        {
            Sprite bell = PixelArt.Save($"{ArtFolder}/Cloche_Epave.png", ShipBellArt.Bell(), ppu, pivot);
            Sprite silhouette = PixelArt.Save($"{ArtFolder}/Cloche_Epave_Silhouette.png", ShipBellArt.Silhouette(), ppu, pivot);
            var fragments = new Sprite[ShipBellArt.Fragments];
            var centers = new Vector2[ShipBellArt.Fragments];
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i] = PixelArt.Save($"{ArtFolder}/Cloche_Epave_Fragment_{i + 1}.png", ShipBellArt.Piece(i), ppu, pivot);
                centers[i] = ShipBellArt.PieceCenter(i);
            }

            var species = FindSpecies("carcharodon-carcharias", "Assets/_Project/Data/Species/CarcharodonCarcharias.asset");
            var zone = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/CoteDesSquelettes.asset");
            LegendFish spectre = LoadOrCreate(Folder + "/SpectreDesEpaves.asset");
            spectre.Configure("spectre-des-epaves", "Spectre des épaves", species, zone, 750f, "Cloche de l'épave", ShipBellArt.Fragments,
                FragmentRules.DefaultChance, bell, fragments, centers, silhouette, new[]
                {
                    "Le haut d'une cloche de bronze, encore pris dans sa corde. Le vert-de-gris a mangé le nom du navire.",
                    "Un morceau du flanc, couvert de bernacles. Quand la brume monte, il sonne tout seul.",
                    "Le bas de la cloche, cabossé, comme mordu par quelque chose d'énorme.",
                    "La lèvre de la cloche. Au fond, une lueur verte qui ne s'éteint jamais.",
                });
            Sprite art = PixelArt.Save($"{ArtFolder}/SpectreDesEpaves.png", BoneSharkArt.Draw(), ppu, pivot);
            spectre.ConfigureLook("Requin scie squelette", art, BodyShape.BoneSawShark);
            // Le combat : il fauche l'eau de sa scie, file entre les épaves, puis plonge vers le fond où dorment les
            // navires. Le dernier gardien, le plus dur de tous.
            spectre.ConfigureFight(new[]
            {
                new LegendPhase(FightStyle.Nervous, new QteDifficulty(2f, 0.13f, 6, 2), "Il fauche l'eau de sa scie !"),
                new LegendPhase(FightStyle.Rusher, new QteDifficulty(2.2f, 0.12f, 7, 2), "Il file entre les épaves !"),
                new LegendPhase(FightStyle.Heavy, new QteDifficulty(1.8f, 0.12f, 7, 1), "Il plonge vers les navires engloutis !"),
            }, 2500);
            EditorUtility.SetDirty(spectre);
            return spectre;
        }

        /// <summary>
        /// Le Moha-moha, tortue géante de la Grande Barrière (espèce de base : le mérou géant). Aperçu en 1890 près de
        /// l'île Fraser, dans le Queensland : une immense tortue au long cou et à queue de poisson. Ici, une vraie tortue
        /// marine géante envahie par le récif. Artefact : le Bouclier d'écaille, en 4 fragments.
        /// </summary>
        private static LegendFish MohaMoha(int ppu, Vector2 pivot)
        {
            Sprite shield = PixelArt.Save($"{ArtFolder}/Bouclier_Ecaille.png", ShellShieldArt.Shield(), ppu, pivot);
            Sprite silhouette = PixelArt.Save($"{ArtFolder}/Bouclier_Ecaille_Silhouette.png", ShellShieldArt.Silhouette(), ppu, pivot);
            var fragments = new Sprite[ShellShieldArt.Fragments];
            var centers = new Vector2[ShellShieldArt.Fragments];
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i] = PixelArt.Save($"{ArtFolder}/Bouclier_Ecaille_Fragment_{i + 1}.png", ShellShieldArt.Piece(i), ppu, pivot);
                centers[i] = ShellShieldArt.PieceCenter(i);
            }

            var species = FindSpecies("epinephelus-lanceolatus", "Assets/_Project/Data/Species/EpinephelusLanceolatus.asset");
            var zone = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/GrandeBarriere.asset");
            LegendFish mohaMoha = LoadOrCreate(Folder + "/MohaMoha.asset");
            mohaMoha.Configure("moha-moha", "Moha-moha", species, zone, 600f, "Bouclier d'écaille", ShellShieldArt.Fragments,
                FragmentRules.DefaultChance, shield, fragments, centers, silhouette, new[]
                {
                    "Le haut d'un bouclier en écaille blonde, bordé d'une corde tressée. Il sent encore la mer.",
                    "Un morceau gravé : une tortue au cou très long, bien plus long que celui d'une vraie tortue.",
                    "Le bas du bouclier, avec des vagues gravées. Une branche de corail y a poussé toute seule.",
                    "Un éclat du bord, poli par le sable. Gravé dessous : « Il dort sous le récif depuis 1890 ».",
                });
            Sprite art = PixelArt.Save($"{ArtFolder}/MohaMoha.png", MohaMohaArt.Draw(), ppu, pivot);
            mohaMoha.ConfigureLook("Tortue géante au long cou", art, BodyShape.MohaMoha);
            // Le combat : il tire de tout son poids vers le fond, longe le récif à toute allure, puis secoue son long cou
            // dans tous les sens. Le plus dur jusqu'ici.
            mohaMoha.ConfigureFight(new[]
            {
                new LegendPhase(FightStyle.Heavy, new QteDifficulty(1.6f, 0.14f, 6, 2), "Il tire de tout son poids vers le fond !"),
                new LegendPhase(FightStyle.Rusher, new QteDifficulty(1.85f, 0.13f, 6, 2), "Il fonce le long du récif !"),
                new LegendPhase(FightStyle.Nervous, new QteDifficulty(2.1f, 0.12f, 6, 1), "Il secoue son long cou dans tous les sens !"),
            }, 1800);
            EditorUtility.SetDirty(mohaMoha);
            return mohaMoha;
        }

        /// <summary>
        /// Rannamaari, le démon de la mer de l'Atoll des Maldives (espèce de base : la bécune à nageoires noires). Dans la
        /// légende maldivienne, il surgissait de l'horizon les nuits de pleine lune, « comme un navire couvert de
        /// lumières ». Artefact : la Lanterne de corail, en 4 fragments.
        /// </summary>
        private static LegendFish Rannamaari(int ppu, Vector2 pivot)
        {
            Sprite lantern = PixelArt.Save($"{ArtFolder}/Lanterne_Corail.png", CoralLanternArt.Lantern(), ppu, pivot);
            Sprite silhouette = PixelArt.Save($"{ArtFolder}/Lanterne_Corail_Silhouette.png", CoralLanternArt.Silhouette(), ppu, pivot);
            var fragments = new Sprite[CoralLanternArt.Fragments];
            var centers = new Vector2[CoralLanternArt.Fragments];
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i] = PixelArt.Save($"{ArtFolder}/Lanterne_Corail_Fragment_{i + 1}.png", CoralLanternArt.Piece(i), ppu, pivot);
                centers[i] = CoralLanternArt.PieceCenter(i);
            }

            var species = FindSpecies("sphyraena-qenie", "Assets/_Project/Data/Species/SphyraenaQenie.asset");
            var zone = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/Maldives.asset");
            LegendFish rannamaari = LoadOrCreate(Folder + "/Rannamaari.asset");
            rannamaari.Configure("rannamaari", "Rannamaari", species, zone, 460f, "Lanterne de corail", CoralLanternArt.Fragments,
                FragmentRules.DefaultChance, lantern, fragments, centers, silhouette, new[]
                {
                    "L'anneau du haut, en corail rouge. Il est encore tiède, comme s'il venait d'être tenu.",
                    "Une branche de la cage, et un cauri qui pend au bout d'un fil. Il tinte tout seul quand la mer monte.",
                    "Le pied de corail mort, couvert de cauris. On dit qu'on les jetait à la mer pour qu'il passe son chemin.",
                    "La perle, ronde comme la pleine lune. Au fond, une lueur bleue s'allume quand tu regardes l'horizon.",
                });
            Sprite art = PixelArt.Save($"{ArtFolder}/Rannamaari.png", RannamaariArt.Draw(), ppu, pivot);
            rannamaari.ConfigureLook("Djinn des abysses", art, BodyShape.Rannamaari);
            // Le combat : il file vers le large en faisant clignoter ses lumières, se tord comme un ruban, puis plonge
            // vers les abysses de l'atoll de tout son poids. Le plus dur jusqu'ici.
            rannamaari.ConfigureFight(new[]
            {
                new LegendPhase(FightStyle.Rusher, new QteDifficulty(1.7f, 0.15f, 5, 2), "Il file vers l'horizon, toutes lumières allumées !"),
                new LegendPhase(FightStyle.Nervous, new QteDifficulty(2f, 0.13f, 5, 2), "Ses lumières clignotent, il se tord comme un ruban !"),
                new LegendPhase(FightStyle.Heavy, new QteDifficulty(1.5f, 0.15f, 6, 1), "Il plonge vers les abysses de l'atoll !"),
            }, 1500);
            EditorUtility.SetDirty(rannamaari);
            return rannamaari;
        }

        /// <summary>
        /// L'Isonade, la rascasse titan de la Baie d'Izu (espèce de base : la rascasse marbrée). Dans les légendes de
        /// pêcheurs japonais, l'isonade est un monstre dont la queue hérissée de crochets arrache les marins des barques.
        /// Artefact : le Miroir des marées, miroir de bronze de sanctuaire, en 4 fragments.
        /// </summary>
        private static LegendFish Isonade(int ppu, Vector2 pivot)
        {
            Sprite mirror = PixelArt.Save($"{ArtFolder}/Miroir_Marees.png", TideMirrorArt.Mirror(), ppu, pivot);
            Sprite silhouette = PixelArt.Save($"{ArtFolder}/Miroir_Marees_Silhouette.png", TideMirrorArt.Silhouette(), ppu, pivot);
            var fragments = new Sprite[TideMirrorArt.Fragments];
            var centers = new Vector2[TideMirrorArt.Fragments];
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i] = PixelArt.Save($"{ArtFolder}/Miroir_Marees_Fragment_{i + 1}.png", TideMirrorArt.Piece(i), ppu, pivot);
                centers[i] = TideMirrorArt.PieceCenter(i);
            }

            var species = FindSpecies("sebastiscus-marmoratus", "Assets/_Project/Data/Species/SebastiscusMarmoratus.asset");
            var zone = AssetDatabase.LoadAssetAtPath<FishingZone>("Assets/_Project/Data/Zones/BaieJaponaise.asset");
            LegendFish isonade = LoadOrCreate(Folder + "/Isonade.asset");
            isonade.Configure("isonade", "Isonade", species, zone, 400f, "Miroir des marées", TideMirrorArt.Fragments,
                FragmentRules.DefaultChance, mirror, fragments, centers, silhouette, new[]
                {
                    "Le haut d'un miroir de bronze, verdi par la mer. Des vagues en écailles sont gravées sur le bord.",
                    "Un morceau du disque où s'enroule une queue de poisson couverte de petits crochets.",
                    "Un éclat du bord, poli d'un côté comme un miroir. On y voit passer une ombre qui n'est pas la tienne.",
                    "Le manche, encore entouré de soie rouge. Gravé dessous : « Quand la mer se tait, l'Isonade est là ».",
                });
            Sprite art = PixelArt.Save($"{ArtFolder}/Isonade.png", IsonadeArt.Draw(), ppu, pivot);
            isonade.ConfigureLook("Rascasse titan", art, BodyShape.Isonade);
            // Le combat : il fouette l'eau de sa queue à crochets, hérisse ses épines et se secoue, puis se plaque au
            // fond comme une rascasse sur son rocher. Plus dur qu'Ipupiara.
            isonade.ConfigureFight(new[]
            {
                new LegendPhase(FightStyle.Rusher, new QteDifficulty(1.55f, 0.17f, 4, 2), "Il fouette l'eau de sa queue à crochets !"),
                new LegendPhase(FightStyle.Nervous, new QteDifficulty(1.85f, 0.14f, 5, 2), "Il hérisse ses épines et se secoue !"),
                new LegendPhase(FightStyle.Heavy, new QteDifficulty(1.35f, 0.17f, 5, 1), "Il se plaque au fond comme un rocher !"),
            }, 1200);
            EditorUtility.SetDirty(isonade);
            return isonade;
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
