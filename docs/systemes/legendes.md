# Laboratoire et poissons de légende (boss)

> Détail du projet, sorti de CLAUDE.md pour l'alléger. À lire quand on travaille sur ce domaine.

- **Laboratoire** (`World/LabBuilding`, objet « Laboratoire », dessins `LabArt` en volume : murs blanchis, toit de tôle
  bleue, coupole, antenne, hublots ; scientifique `CharacterArt.Scientist`) : dans les terres de l'est
  (`FindBuildingSite` autour du centre + 22 cases, `alsoAvoid` = boutique et maison), cour pavée, chemin et décor
  (`TownLayout.DecorateLab`), icône sur la minimap. Intérieur 13×8 à `bounds.xMax + 20, yMin + 20` (au-dessus de la
  maison) : aquarium d'étude, étagères à bocaux, carte, affiche du poisson de légende, bureau, socle de pierre et plante
  (sprites à part, obstacles dans `LabBuilding`). **Vie** (`World/LabLife`, objet « Vie du laboratoire », dessins
  `LabArt.TankFish` / `Seaweed` / `Bubble` / `PedestalGlow` → `Art/Buildings/Laboratoire/`, seulement quand le joueur est
  dedans) : poissons qui nagent dans l'aquarium d'étude (queue qui bat, demi-tour), algues qui ondulent, bulles de la
  pompe, lueur du socle qui pulse, scientifique qui se penche sur ses papiers. Devant le bureau, **E** fait parler le **Dr Hina Teva** (boîte de
  dialogue, E/Espace suite, Échap ferme ; `PlayerAtDesk` / `DialogOpen` empêchent le sac de s'ouvrir). Première étape
  des **poissons de légende** (boss) : fragments d'artefact pêchés → artefact complet donné à la scientifique → boss
  invoqué dans sa zone, combat en 3 phases.
- **Labo redessiné et aile des spécimens** : façade de 256 px (bâtiment principal de 160 px, porte à x = 80, et
  **aile des spécimens** à droite : verrière bombée où luisent les tubes, hublots, cuve, cheminée ; `SpecimenLabArt.Wing`),
  emprise 7,8 cases décalée de 1,5 à droite (`InteriorBuilding._footprintShift`), place réservée plus large
  (`TownLayout.Block`, `FindBuildingSite` halfWidth 7). Pièce du labo élargie à **13×8** (paillasson toujours à x = 5,5,
  `EntranceX`) : enduit bleu des grands fonds, lambris sombre et main courante de laiton, tuyau de cuivre au plafond,
  **tableau noir** où les légendes sont esquissées à la craie (carte du monde punaisée dans le coin), lampe de banquier,
  parquet de bateau avec flaques de lumière, tapis de marin à rose des vents, câble au sol, papiers tombés. Au fond à
  droite, la **porte blindée** (`LabBuilding.SpecimenDoor`, « Haut : salle des spécimens ») mène à la **salle des
  spécimens** (30×12 cases, `SpecimenOrigin` = 7 cases à droite de la pièce ; `SpecimenLabArt.Room` : panneaux d'acier
  rivetés, tuyaux, câbles, tableaux de voyants, caillebotis, bandes jaunes et noires, sas au milieu du bas pour
  ressortir). La caméra suit la salle où l'on est (`CameraRect`, `InSpecimenRoom`). **7 tubes** (`World/SpecimenTube`,
  un par place, cuve + spécimen + verre dans un `SortingGroup`) : la légende n° i du `FishingController` dans le tube i
  (`PrototypeSetup`), sa bête redressée tête en haut et réduite (`SpecimenLabArt.Specimen` →
  `Art/Buildings/Laboratoire/Specimen_<id>.png`) n'apparaît qu'une fois la légende **vaincue** ; elle ondule, des bulles
  montent, le liquide pulse ; sinon le tube reste vide et sombre. Devant un tube, la plaque s'affiche (nom, espèce,
  taille, zone, ou « ??? »). Une lueur verte pulse au sol devant chaque tube occupé (`_floorGlow`). **Mobilier** (dessins
  `SpecimenLabArt`, pieds et obstacles dans `LabBuilding` : `TableFoot`, `ConsoleFoot`, `CartFoot`, `CratesFoot`,
  `PoolFoot`, `VentSpots`) : table de dissection au squelette géant, console d'analyse à deux écrans (courbe et sonar,
  8 images chacun), chariot de bocaux qui luisent, caisses de transport, bassin de quarantaine où tourne un petit
  poisson, bouches d'aération qui crachent de la vapeur, flaques de liquide, câbles au sol. Voyants qui clignotent,
  écrans qui défilent, vapeur, écrans de console et poisson animés par `World/SpecimenRoomLife`. Animé seulement quand
  on est dans la salle.
- **Poissons de légende** (`Collection/LegendFish`, ScriptableObjects dans `Data/Legendes/`, générés par `LegendSetup` ;
  champ `_legends` du `FishingController`). Premier : **Tuna**, murène géante de 420 cm de l'île Mahana (espèce de base la
  murène javanaise ; nom de l'anguille géante d'une légende polynésienne), dessin `MorayArt` → `Art/Legendes/Tuna.png`
  (`LegendFish.Art`, `KindName`), artefact
  **Idole de corail** en 4 fragments (`LegendArt` : idole en volume, fragments aux cassures dentelées dans le même cadre,
  silhouette ; `Art/Legendes/`). **Fragments** (`FragmentRules`, testé) : dans la zone de la légende, une prise réussie
  sur ≈ 33 (3 %, ×2 dans un coin de pêche, ≈ 130 prises pour l'artefact) est un fragment, jamais en double, plus aucun
  une fois tous trouvés. Un fragment pêché va **en poche** ; l'artefact n'est complet qu'une fois tous ses fragments
  **posés** au labo, dans l'ordre. Avancée dans `PlayerProgress.Legends` (`LegendProgress`, testé : `Fragments`,
  `Placed`, `InHand`, `PlaceFragment` ; `Collecting` → `Complete` → `Summoned` → `Beaten`), sauvegardée
  (`SaveData.Legends`, avec `Placed`). Carte `Fishing/FragmentCard` (fragment qui flotte et tourne dans un halo,
  artefact en silhouette, phrase du fragment). Onglet **« Reliques »** de l'inventaire (`InventoryHud`, Tab ou
  ←/→ entre « Sac et matériel » et « Reliques », pas d'onglets pendant le choix « sac plein » ; `OpenRelics()` par
  script) : une carte par légende (3 par rangée) avec l'artefact (posés en couleur, en poche à demi transparents),
  une pierre par fragment (or = posé, turquoise = en poche), l'étape et la suite. Les fragments ne prennent pas de
  place dans le sac.
  **Atelier des artefacts** (`UI/ArtifactWorkshop`, créé par `LabBuilding`, canvas ordre 15) : **E devant le socle**
  (`LabBuilding.PedestalFront`, `PlayerAtPedestal`). Liste des légendes à gauche (« ??? » tant que rien n'est trouvé),
  artefact **grisé** au centre rempli morceau par morceau, fragments en poche sur le plateau en bas ; **Espace** pose le
  suivant : il s'envole en arc jusqu'à sa place (rotation, rebond), éclair, secousse, étincelles ; le dernier illumine
  l'artefact d'un halo doré. Haut/Bas change d'artefact, E/Échap ferme. Décor (`Editor/WorkshopArt` →
  `Art/UI/Atelier/`, `ArtifactWorkshop.Look` dans le champ `_workshopLook` du `LabBuilding`, 1 pixel = 4 unités) :
  l'établi du labo (bois sombre éclairé par la lampe, tapis de feutre, carnet ouvert où l'atelier pose le croquis de l'artefact choisi (`_sketch`), loupe,
  pinceau, bocal), dalle de pierre gravée pour l'artefact, cases de velours du plateau, fiches de papier épinglées. Par script : `lab.Workshop.Open()`,
  `PlaceNext()`. La scientifique dit combien il en manque, invite à les poser, puis réagit à l'artefact complet.
  Par script : `FishingController.DebugFindFragment()`.
  **Réveil** : artefact complet → Espace dans l'atelier (`ArtifactWorkshop.Summon`) : l'idole tremble, étincelles,
  éclair blanc, bandeau « Tuna s'est réveillé ! », état `Summoned` ; le socle du labo montre le **dernier artefact assemblé** (`LegendProgress.LastAssembled`, sauvegardé dans `LastArtifact`).
  **Remous légendaire** (`World/LegendSpot`, objets « Remous légendaire de l'île » et « … du lac », `MapSetup.BuildLegendSpot`, relié à
  sa légende par `PrototypeSetup`) : visible seulement tant que la légende est réveillée ; eau libre fixe (graine) à
  portée de lancer d'une rive **qu'on rejoint à pied depuis le ponton** (`World/ShoreReach`, partagé avec les coins de pêche :
  jamais au large d'un îlot inaccessible ; la rive retenue est `LegendSpot.Shore`, où « Aller au remous » du menu de test
  pose le joueur), remous en spirale qui tourne et ondes (`Editor/LegendSpotArt` → `Art/Ambient/Remous_*.png`, à 16 px par case, jamais agrandis), ombre géante qui tourne (cachée quand la ligne est
  dedans), gros point doré sur la minimap. Le lancer y va tout seul (visée) ; dedans, c'est la légende qui mord
  (`FishingController.HookedLegend`, attente 5-8 s, ombre géante de 72 px sous le bouchon).
  **Combat de légende** (`Fishing/Core/LegendFight`, testé ; phases `LegendPhase` dans la fiche, `LegendFish.Phases`) :
  phases enchaînées, chacune une jauge `QteChallenge` avec son style (matériel appliqué), pause de 1,3 s entre deux
  (appuis ignorés, titre « Phase N ! »), endurance sur tout le combat ; un échec = elle s'échappe (reste réveillée, on
  retente). Tuna : rush (« Il file se réfugier dans sa grotte ! »), lourd (« Il s'enroule autour du corail et tire ! »),
  nerveux (« Il se tord dans tous les sens ! »). Vue : barre d'endurance au-dessus de la jauge, cri de la phase en grand (du côté
  libre de l'écran), secousse et flash. Victoire : `LegendBeaten`, état `Beaten`, `RewardCoins` (600), carte
  `Fishing/LegendVictoryCard` (la bête en grand, rayons dorés, confettis), le remous disparaît. Par script :
  `FishingController.DebugWinLegendPhase()`. Trophée à l'aquarium et légende dans le carnet : reportés (idée pas encore choisie).
- **Deuxième légende : Bramafan**, écrevisse titan de 380 cm du **Lac d'Annecy** (espèce de base l'écrevisse signal ;
  « qui crie famine » en patois savoyard), même fonctionnement que Tuna de bout en bout. Dessin `Editor/LakeCrayfishArt`
  → `Art/Legendes/Bramafan.png` (vue de dessus : armure fissurée couverte de vase, galets et moules, pince-marteau et
  pince-cisaille, tache claire de l'écrevisse signal qui luit, grappes d'yeux, antenne arrachée, œufs qui luisent).
  Silhouette sous l'eau `BodyShape.Crayfish` (`FishShadowSprites.Crayfish` : pinces qui claquent, pattes qui marchent).
  Artefact **Médaillon des palafittes** (`Editor/MedallionArt`, même cadre 48×64 que l'idole : pendentif de bronze
  verdi, bord perlé, pinces gravées en spirale autour d'une ambre ; 4 quartiers dentelés, haut puis sens horaire).
  Combat : rush (« Il file à reculons d'un grand coup de queue ! »), lourd (« Il plante ses pinces dans les galets du
  fond ! »), nerveux (« Il claque des pinces dans tous les sens ! »), un peu plus dur que Tuna, 800 pièces. Remous dans la
  région 1.
- **Troisième légende : Ipupiara**, payara titan de 340 cm du **Fleuve Amazone** (espèce de base le payara, le
  « poisson-vampire » ; nom tupi d'un monstre des eaux décrit par les premiers voyageurs au Brésil), même fonctionnement
  de bout en bout. Dessin `Editor/PayaraArt` → `Art/Legendes/Ipupiara.png` (de profil, lancé à l'attaque : écailles
  d'acier terni, queue fourchue et pectorale en faux déchirées, gueule ouverte vers le haut d'où se dressent les deux
  crocs géants, œil d'argent à la pupille rouge qui luit, isopode parasite, ouïes rouges, lignes cassées et hameçons qui
  pendent de la mâchoire, vieille cuillère rouillée). Silhouette sous l'eau `BodyShape.Payara` (`FishShadowSprites.Payara` :
  corps fuselé, crocs devant le museau, pectorales qui battent, queue fourchue déchirée). Artefact **Grenouille de jade**
  (`Editor/JadeFrogArt`, même cadre 48×64 : amulette-grenouille de pierre verte veinée, pattes repliées gravées, yeux d'or
  qui luisent, cordon tressé dans deux trous ; 4 quartiers dentelés comme le médaillon). Combat : rush (« Il fonce dans le
  courant du fleuve ! »), nerveux (« Il bondit hors de l'eau en secouant la tête ! »), lourd (« Il plonge au fond et tire
  de tout son poids ! »), plus dur que Bramafan, 1000 pièces. Remous « Remous légendaire du fleuve » dans la région 2.
  Esquisses écartées (non branchées) : `MatamataArt` (matamata titan) et `PlecoArt` (pléco titan).
- **Quatrième légende : l'Isonade**, rascasse titan de 400 cm de la **Baie d'Izu** (espèce de base la rascasse marbrée ;
  monstre des légendes de pêcheurs japonais dont la queue hérissée de crochets arrache les marins des barques), même
  fonctionnement de bout en bout. Dessin `Editor/IsonadeArt` → `Art/Legendes/Isonade.png` (de profil : peau marbrée rouge
  sang et noire à lambeaux de peau, épines venimeuses du dos tendues de voiles déchirées, gueule en gouffre aux crocs en
  aiguille, œil d'ambre à pupille fendue sous une corne, pectorale en éventail, queue en éventail couverte de crochets
  d'os avec une rame brisée et un lambeau de filet, bernacles, corde sacrée shimenawa effilochée avec ses papiers en
  zigzag autour du cou). Silhouette sous l'eau `BodyShape.Isonade` (`FishShadowSprites.Isonade` : tête énorme à épines,
  pectorales en éventail qui battent, queue bordée de crochets). Artefact **Miroir des marées** (`Editor/TideMirrorArt`,
  même cadre 48×64 : miroir de bronze à manche vu de dos, vert-de-gris, vagues en écailles gravées, queue à crochets
  enroulée autour d'une perle qui luit, manche entouré de soie rouge et gland ; 4 quartiers dentelés). Combat : rush
  (« Il fouette l'eau de sa queue à crochets ! »), nerveux (« Il hérisse ses épines et se secoue ! »), lourd (« Il se
  plaque au fond comme un rocher ! »), plus dur qu'Ipupiara, 1200 pièces. Remous « Remous légendaire de la baie » dans la
  région 3.
- **Cinquième légende : Rannamaari**, djinn des abysses de 460 cm de l'**Atoll des Maldives** (espèce de base la bécune à
  nageoires noires ; démon de la mer de la légende maldivienne, qui surgissait de l'horizon les nuits de pleine lune
  « comme un navire couvert de lumières »). Choisi parmi trois esquisses (navire de lumières, djinn des abysses, tigre de
  braise). Dessin `Editor/RannamaariArt` → `Art/Legendes/Rannamaari.png` (de profil : long corps de barracuda noir-vert,
  couronne de cornes dressée derrière l'œil, côtes qui transparaissent, deux rangées de photophores bleus, dorsale en
  ruban effilochée bordée de lueurs, longues pectorales aux veines bleues, mâchoire du bas en avant hérissée de crocs,
  barbe de filaments aux bouts lumineux, filament du front portant une perle qui luit devant la gueule). Silhouette sous
  l'eau `BodyShape.Rannamaari` (`FishShadowSprites.Rannamaari` : corps fin qui ondule, cornes, pectorales en faux, queue
  en croissant, yeux, perle et photophores lumineux). Artefact **Lanterne de corail** (`Editor/CoralLanternArt`, 48×64 :
  cage de branches de corail rouge autour d'une perle qui luit comme la lune, pied de corail mort couvert de cauris,
  deux cauris qui pendent ; 4 quartiers dentelés). Combat : rush (« Il file vers l'horizon, toutes lumières allumées ! »),
  nerveux (« Ses lumières clignotent, il se tord comme un ruban ! »), lourd (« Il plonge vers les abysses de l'atoll ! »),
  le plus dur jusqu'ici, 1500 pièces. Remous « Remous légendaire de l'atoll » dans la région 5. Tube n° 5 de la salle des
  spécimens. La liste de l'atelier des artefacts rapetisse ses fiches au-delà de 5 légendes.
- **Nouvelle légende** : une méthode dans `LegendSetup` (découpe des fragments avec `LegendArt.Cut` /
  `Center` / `Silhouette`), un `BuildLegendSpot` dans la région de sa zone (`PrototypeSetup.RegionOf` relie remous et
  légende). Le menu de test (**FishAndChill → Légendes (test)**) agit sur la légende de la zone où l'on est (sinon Tuna) et
  « Aller au remous » choisit le remous actif.

- **Tempête des boss** (`World/LegendStorm`, sur l'objet « Météo » ; règles `LegendStormMath`, testé) : pour chaque boss réveillé,
  la tempête monte à mesure qu'on s'approche de son remous (rien au-delà de 30 cases, au comble à 6 cases) et pendant
  tout le combat, puis retombe en s'éloignant ou après la victoire. Ciel presque noir et violacé, déluge couché par le
  vent, brume, lanternes allumées même de jour, éclairs toutes les 1 à 3 s, et **vraie foudre dessinée** (`WeatherArt.Bolt`,
  3 tracés, pied = point d'impact) qui frappe souvent le remous, avec une lueur d'impact et une secousse de la caméra
  (`CameraFollow.Shake`). L'en-tête affiche « Tempête ». Le temps de la zone reprend ensuite son cours.

## Dessins des légendes


- Ce sont de **vrais monstres marins**, pas des poissons, dans le style très détaillé et sombre de `Editor/MorayArt`
  et `Editor/LagoonDragonArt` (outils communs dans `Editor/CreatureShapes` : épine dorsale avec repère local pour
  peindre écailles et motifs qui suivent la courbe, tubes qui s'affinent, courbes, polygones, bruit doux `Noise`,
  anneaux `Ring`, nageoires `FinAlong`). Une créature unique et
  cohérente avec sa zone, de plus en plus impressionnante au fil des zones.
- `LagoonDragonArt` (dragon des mers : crête d'épines et membrane déchirée, cornes, gueule à crocs) est **réservé à une
  zone plus avancée** ; il n'est encore utilisé nulle part.

