# Bâtiments, progression, interfaces et sauvegarde

> Détail du projet, sorti de CLAUDE.md pour l'alléger. À lire quand on travaille sur ce domaine.


- `PlayerProgress` (objet « Partie ») : `Inventory` (sac, 3 places au départ, `UpgradeCapacity` pour plus tard),
  `AquariumCollection` (un exemplaire par espèce), `Wallet` (pièces). Logique pure testée.
- Triche d'essai : menu **FishAndChill → Triche (test) → +1000 pièces** (raccourci Ctrl + Alt + M, `Editor/CheatMenu`),
  actif seulement en partie en Play mode ; les pièces sont sauvegardées comme de vraies pièces.
- Chaque prise va dans le sac. Sac plein → état `FishingState.BagFull` : 1/2/3 relâche ce poisson du sac
  (`ChooseReplace`), Espace relâche le nouveau (`ChooseRelease`).
- **Maison du joueur** (`World/HouseBuilding`, objet « Maison », dessins `HouseArt` : enduit à colombages, toit de tuiles,
  volets bleus) : dans la prairie de l'ouest (`MapSetup.FindBuildingSite` autour du centre - 24 cases), cour pavée et
  chemin jusqu'au chemin principal (`TownLayout`, `DecorateHouse`). Intérieur 9×7 (`VolumeHouseArt` : lit à la taille du pêcheur, chevet
  et lampe, coffre, tapis tissé, table à nappe et chaises, plante, caisse de matériel, bottes ; au mur carte, fenêtre
  sur la mer, applique, trophée, étagère au bocal, guirlande de flotteurs ; soleil sur le parquet ; obstacles dans
  `HouseBuilding.SetupInterior`). Devant le lit, **E** fait dormir jusqu'à 7 h la nuit (18 h - 7 h) ou faire
  la **sieste jusqu'à 18 h** le jour (`SleepRules`, testé) : fondu, le pêcheur disparaît et sa **tête endormie**
  (`CharacterPainter.SleepingHead` : son apparence, yeux fermés, sans chapeau) se pose sur l'oreiller sous la couette
  bombée (`Maison_Couette.png`, `VolumeHouseArt.SleeperBlanket`), « Z » qui montent pendant que l'horloge file jusqu'au réveil, puis **sauvegarde**
  automatique et message ; le sac ne s'ouvre pas devant le lit (`HouseBuilding.PlayerAtBed`).
- **Phare** (`World/Lighthouse`, objet « Phare », dessin `LighthouseArt`) : sur la pointe nord (`FindLighthouseSite`),
  pied solide. La nuit (`DayCycleMath.LampLevel`), lanterne allumée (`Light2D`) et double faisceau qui tourne
  (sprite créé en jeu, matériau *Sprite-Unlit-Default*). Maison et phare ont leur icône sur la minimap.
- **Boutique** (`ShopBuilding`, objet « Boutique », placée dans les terres par `MapSetup.FindShopSite`) :
  infranchissable ; on entre en poussant vers la porte (`PlayerMovement.LastInput`), on sort en redescendant sur
  le paillasson. L'intérieur est une pièce hors de l'île (`_interiorOrigin`, sol ajouté via
  `WorldMap.AddWalkableArea`), la caméra s'y cale (`CameraFollow.SetBoundsOverride`). **Pas de pêche dedans**
  (`FishingController.CanFishHere`).
- **Marchand** (`UI/ShopMenu`) : grande fenêtre 1520×940 (canvas ordre 13) ; E devant le comptoir ouvre le menu (joueur `Locked`), cinq rayons
  (**Tab** / Maj + Tab, gâchettes) : « Vendre » (liste du sac : 1, 2, 3… vend un poisson, V vend tout), puis « Cannes »,
  « Hameçons », « Appâts », « Sacs » en **vitrine** (`UI/ShopShowcase`, objets `ShowcaseEntry`, calculs `ShowcaseMath`
  testés) : un objet à la fois, en grand dans une scène éclairée (fond, socle à coussin de velours, râtelier où la canne
  se tient debout, flèches : dessins en volume `Editor/ShopDisplayArt`, `Art/UI/Vitrine/`, affichés ×3 ; l'objet flotte
  doucement), fiche à droite (nom, effet, état, prix, « Clic ou Espace : acheter / prendre en main / l'accrocher »), vignettes du
  rayon dessous. **Q / D** (touches physiques), ←/→ ou la croix font défiler, 1 à 8 choisit, **Espace** / Entrée / bouton A
  achète. **Tout à la souris** (`ShopMenu.HandleMouse`) : clic sur un rayon, sur un poisson du sac (le vend), sur le
  bouton « Tout vendre (V) » (à côté des pièces, rayon « Vendre » seulement), sur les flèches, les vignettes, l'objet
  ou le bandeau « Clic ou Espace : … » ; molette pour l'objet précédent / suivant ; clic à côté du menu pour sortir.
  Ce qui est survolé s'éclaire en doré (`ApplyHover`, `ShopShowcase.Hover`). Chaque rayon s'ouvre sur l'objet en main (le prochain sac
  pour « Sacs »). Échap/E ferme. Règles dans `FishShop` (testées) : il n'achète que les doublons (espèce déjà dans
  l'aquarium, ou exemplaires en trop : il laisse le plus gros), prix de 80 % à 130 % du `BasePrice` selon la taille.
- **Matériel** (`Scripts/Gear`, données dans `Data/Gear/`, générées par `GearSetup`) : 3 familles (`GearLine`,
  ScriptableObjects) réunies dans `GearCatalog` (l'appât a son propre système, voir Leurres et appâts), chacune avec un niveau 0 gratuit puis 3 améliorations :
  sac (4/5/6 places, niveaux). **Cannes et hameçons = objets à choisir** (pas des niveaux) : on les achète puis on
  équipe celui qu'on veut ; chacun a sa **spécialité** (`GearLevel.WithStats` : curseur, tension, portée du lancer,
  poissons nerveux / rushs / fuyants ; `GearEffects` multiplie canne × hameçon, appliqué par `QteChallenge` et
  `FishingController`). **Chaque canne a un rôle** que les autres n'ont pas, et sa propre construction (`GearArt.RodLarge`
  et `GearArt.Rod` : même courbe pour toutes, mais poignée, moulinet, anneaux et épaisseur différents) : bambou (perche à
  nœuds sans moulinet, plioir en bois ; discrète : touches 20 % plus rapides via `Value2`, ajouté à celui de l'appât
  dans `GearEffects`), légère (brin blanc très fin, petit moulinet ; nerveux ×0,35, zone +20 %, mais tension ×1,5), longue
  (surfcasting, trois brins emmanchés, gros moulinet ; +3 cases, fuyants ×0,5), lourde (pêche au gros, cardan, anneaux à
  rouleaux, moulinet doré dessus ; tension ×0,4, rushs ×1,6 plus espacés, curseur ×1,1 : la canne des gros et des légendes),
  compétition (carbone, garnitures dorées, moulinet casting ; zone +35 %, curseur ×0,85, mais tension ×1,35). La **canne en
  main** pendant la pêche (`FishingPrototypeView.RodLooks`) prend les couleurs, l'épaisseur et la longueur de celle équipée. Hameçons : Aberdeen, triple (+2 ratés), circulaire (tension ×0,8, rushs
  rares), forgé (+3 ratés, fuyants ralentis), texan, à mouche. Possédés dans `GearLoadout` (`Owns`/`Own`/`Equip`/`Cycle`), achat par
  `GearShop.TryBuyItem` / `PlayerProgress.BuyGearItem` (testés), sauvegardés dans `SaveData.GearOwned` (ancienne
  sauvegarde : tout ce qui était sous le niveau atteint est gardé). Marchand : onglets Vendre / Cannes / Hameçons /
  Leurres / Sacs. Inventaire : **R** (physique) change de canne, **F** d'hameçon (gâchettes hautes à la manette). Niveaux du joueur dans
  `PlayerProgress.Gear` (`GearLoadout`), achat par `PlayerProgress.BuyGear` / `GearShop` (testés). Effets calculés par
  `GearEffects` (testé) et appliqués par `FishingController` (QTE, tirage de l'espèce, attente de la touche).
- **Leurres et appâts** (`Scripts/Gear` : `Bait`, `BaitCatalog`, `LureType`, `HookType`, `LureRules` ; ScriptableObjects dans
  `Data/Gear/Appats/`, générés par `BaitSetup`, dessins `Editor/LureArt` 48×32 nez à droite, icônes
  `Art/UI/Materiel/Leurre_<id>.png`). 30 vrais modèles génériques (pas de marques) rangés en 10 **types** : appâts
  naturels (ver de terreau de départ gratuit et illimité, asticots, crevette grise, lanière de calmar : en paquets),
  cuillères tournantes (palettes Aglia, Colorado, Saule), ondulantes (Toby, Crocodile, Devon), poissons nageurs (minnow,
  jerkbait, crankbait), leurres souples (shad, ver finesse, grub), surface (popper, stickbait, walking-the-dog), jigs
  (pilker, casting jig, slow jig), turluttes (egi, phosphorescente : ×3 la nuit), traîne (jupe octopus, plume),
  mouches (Adams, Woolly Bugger, streamer Clouser). Les leurres sont **réutilisables** ; certains attirent plus de rares
  ou font mordre plus vite. **Qui mord à quoi** (`LureRules.Allows`, testé, appliqué au tirage par
  `FishingController.Cast`) : chaque espèce a ses types de leurres et ses hameçons (colonnes Appats et Hamecons de la
  table) ; les **communes** mordent à tout ; les **peu communes et rares** exigent un de leurs types de leurre ; les
  **épiques et légendaires** exigent en plus un de leurs hameçons. Le bon type de leurre attire aussi l'espèce ×3.
  Hameçons (`GearSetup`, ordre = `HookType`) : Aberdeen (départ), triple (60 p., +2 ratés), circulaire (150 p.,
  tension ×0,8), O'Shaughnessy forgé (340 p., +3 ratés), texan (90 p., tension ×0,9), à mouche (70 p.). Anciens appâts
  des sauvegardes convertis (`LureRules.ConvertOldBait`). Possédés et accroché dans `PlayerProgress.Baits` (`BaitBag`,
  testé ; `SaveData.Baits`, `EquippedBait`) ; un lancer consomme une utilisation d'appât naturel, le dernier parti on
  revient au ver de terreau. Marchand : onglet **« Leurres »** (type, modèle, effet). Inventaire : touche physique Q
  (A en AZERTY) ou X manette pour changer. Carnet : « Ne mord qu'aux … , hameçon … obligatoire » (ou « Mord à tout,
  surtout aux … » pour les communes), indice même pour les « ??? » ; la bouteille à la mer donne aussi le type.
- **Épuisette (pêche à pied, en cours)** : les casiers à crustacés ont été retirés. Chaque espèce a un **engin**
  (`Collection/CatchMethod` : canne ou épuisette, colonne Engin de la table, `FishSpecies.Method`).
  `PrototypeSetup.CreateOrUpdateZone` range les espèces de la canne dans `FishingZone.Species` (carnet, aquarium,
  demandes des habitants, tirage de la canne) et celles de l'épuisette à part dans `FishingZone.NetSpecies` : 69 espèces
  (crustacés, coquillages, oursins, étoiles, méduses, vers, tortues, grenouille, limule, lamproie), 5 à 10 par zone, de
  commune à légendaire (les bêtes du large). Elles n'iront pas à l'aquarium. La canne ne les attrape jamais
  (`FishingController.Cast`). Le calmar veiné (Kraken) et l'écrevisse signal (Bramafan) restent l'espèce de base de leur
  légende. Dessins validés (`Editor/TidePoolArt`) : l'épuisette, les bulles, la flaque de Mahana et une seule bestiole
  sous l'eau pour toutes les espèces (`Critter`).
  **Mini-jeu** (`UI/TidePoolGame` sur « Partie », installé par `Editor/NetSetup` ; menu **Installer l'épuisette** pour la
  scène ouverte ; règles pures testées `Fishing/Core/NetRules`) : face à un coin à bulles à moins de 4,8 cases, invite
  « Espace : sortir l'épuisette » ; l'action ouvre la flaque au lieu de lancer (`FishingController.BeforeCast`).
  Flaque 320×192 affichée ×4 (fenêtre de presque tout l'écran), 5 bestioles tirées dans `FishingZone.NetSpecies` (fréquences, heure), plus grandes et plus
  méfiantes quand elles sont rares (`NetRules.Scale` / `Sensitivity`). L'épuisette suit la souris, le stick ou ZQSD avec
  le retard de l'eau ; bougée vite près d'une bête, elle l'inquiète. **Une seule prise par flaque**, 3 coups (clic, Espace,
  A) : l'éclaboussure fait fuir les bêtes déjà inquiètes ; la bête calme la plus proche sous le cercle est dans le filet
  et la **remontée** commence (`Fishing/Core/RingQte`, testé) : des anneaux se resserrent un par un sur elle, il faut
  appuyer quand l'anneau touche son contour (doré à ce moment). Commune 2 anneaux, peu commune et rare 3, épique 4,
  légendaire 5 ; plus rare = anneaux plus rapides et marge plus fine ; un raté permis pour commune et peu commune,
  aucun au-delà ; un anneau qui passe la bête compte raté. Rythme par famille : crabes et crustacés serrent par à-coups,
  crevettes, poissons et vers changent de vitesse à chaque anneau, l'encre des céphalopodes cache l'anneau au milieu de
  sa course, coquillages et oursins lents mais marge étroite. Pendant la remontée l'épuisette devient transparente. La flaque se ferme toute seule 2,5 s après la fin (un clic la ferme tout de suite). Dessin du filet (`TidePoolArt.Landing`, 60×60 dans l'eau, 32×32 en icône) : cerceau en D au bord droit devant comme un haveneau, poche à mailles en losange qui se resserrent vers le fond, fourche, long manche en bambou.
  Réussie : la bête est notée, le coin à bulles se disperse (`FishingSpots.Deplete`) ; ratée : elle s'échappe et la
  flaque s'inquiète, on peut retenter tant qu'il reste des coups. Réactions (`CritterBehaviour`) : crustacés, limule,
  grenouille se cachent sous le rocher le plus proche ; céphalopodes presque invisibles, encre puis réapparition
  ailleurs ; coquillages, oursins, étoiles, méduses s'enfouissent sur place ; les autres filent. Prises notées dans le
  journal (`CatchLog`) puis relâchées ; sans prise, le coin s'épuise (`FishingSpots.Consume`). Échap ou B pour revenir. Les coins
  à bulles montrent la grappe de bulles (`FishingSpots._cluster`, `Art/Ambient/Bulles_*`). **Une flaque par zone** (`TidePoolArt.Pool(zone)`, `Art/UI/Epuisette/Flaque_<zone>.png`), chacune avec sa disposition :
  Mahana (lave, corail, herbiers), Annecy (galets, branche noyée, characées, nénuphars), Amazone (racines, feuilles mortes,
  nénuphars géants), Izu (sable noir, colonnes de basalte, varech, pétales), Grande Barrière (coraux branchu, cerveau,
  table, mou), Maldives (dalles du platier), Côte des Squelettes (côtes et vertèbre de baleine, moules, kelp), Fjords
  (galets, granit à balanes, varech), Ha Long (racines de palétuvier, calcaire, huîtres). Ses cachettes
  (`TidePoolArt.HideSpots`) sont rangées avec elle dans `TidePoolGame._looks` ; la flaque suit la zone de la pêche. **Obtention** : plus vendue ; **Chiyo**, la vieille ama d'Izu, la donne en lui parlant (répliques `VillagerLines.NetGift`,
  `PlayerProgress.ReceiveNet` / `HasNet`, testé ; sauvegardée dans `SaveData.HasNet`, une épuisette achetée avant reste). Sans elle, **les coins à
  bulles n'apparaissent dans aucune zone** (`FishingSpots.Revealed`).
  **Perles du collier de Chiyo** (`Collection/PearlNecklace`, testé ; noms, couleurs et souvenirs dans `PearlLore`, une par
  zone) : quand une prise à l'épuisette complète toutes les bestioles d'une zone au carnet, la perle de la zone brille au fond
  du filet (`TidePoolGame.FindPearl`, une seule fois). En parlant à Chiyo, elle enfile les perles rapportées et raconte un
  souvenir pour chacune, puis dit combien il en manque (`Villager.PearlLines`). Carnet, page Épuisette : la perle au bout de
  la barre de progression (silhouette à l'encre pâle, puis à sa couleur ; « À rapporter à Chiyo » / « Sur son collier »,
  dessinée en jeu par `UI/PearlSprite`). **Boîte à bijoux** (onglet Reliques : la plaque du coffre est un menu déroulant
  « Coffre des reliques / Boîte à bijoux », clic ou Espace / Y ; dessin `Editor/JewelBoxArt`, même format que le coffre) :
  laque noire et coussin de soie indigo creusé d'un seul emplacement en forme de collier (`PearlLore.NecklacePixel`) ;
  perles en silhouette ombrée, puis à demi une fois trouvées, puis à leur couleur une fois enfilées ; l'étiquette donne
  nom, zone et le souvenir de Chiyo (`RelicChestPage.RefreshJewels`). Sauvegarde : `SaveData.PearlsFound` / `PearlsThreaded`.
  **Grotte de Chiyo** (baie d'Ha Long, montagne `HaLongPlan.CaveIndex` ; `World/CaveBuilding`, posée par `MapSetup.BuildCave`) :
  lanternes éteintes (`KarstRange(..., lit: false)`) et entrée trop noire tant que le collier n'a pas ses 9 perles (on lit
  une phrase en poussant l'entrée) ; collier complet : Chiyo y envoie le joueur, lanternes allumées, on entre. Salle de
  13×9 cases (`Editor/CaveArt` : stalactites, stalagmites, grande flaque lumineuse), intérieur au-dessus de la cabane de
  Chiyo. Au bord de la flaque, Espace sort l'épuisette (`TidePoolGame.UpdateCave`) ; zone **GrotteHaLong** (« Grotte de
  Chiyo », 10e zone du carnet, cachée dans la colonne tant que le collier est incomplet : `FishIndex.ZoneCount` ; sans voyage ni perle) : 8 bêtes étranges (`Editor/CaveNetArt`, table des espèces) qui se
  montrent l'une après l'autre (celles déjà au carnet + la suivante, `Collection/CaveRules`, testé), méduse immortelle en
  dernier ; **une prise par jour** (`PlayerProgress.CaveDay`, `SaveData.CaveDay`). **Fin** : à la première méduse immortelle, Chiyo apparaît dans la grotte
  (`CaveBuilding.ChiyoHere`, son portrait près de la flaque) et dit ses adieux (`Villagers/ChiyoEnding.Reveal`, scène écrite
  `DialogueBox.OpenScript`) ; ensuite E près d'elle : une phrase de `ChiyoEnding.Later`. La suite de son histoire viendra
  avec l'histoire du jeu.
- **Bâtiments à intérieur** : base commune `InteriorBuilding` (entrée par la porte, pièce hors de l'île, caméra
  calée, `InteriorBuilding.Occupied` = bâtiment où se trouve le joueur ; pas de pêche dedans). `ShopBuilding` et
  `AquariumBuilding` en héritent. Sites choisis par `MapSetup.FindBuildingSite` (l'aquarium au centre, la boutique à ≥ 14 cases).
- **Habitants** (`Scripts/Villagers`, profils `VillagerProfile` dans `Data/Habitants/`, générés et posés par
  `Editor/VillagerSetup`, objets sous « Habitants ») : **Tavita** le vieux pêcheur (cabane du pêcheur, 7 h - 17 h 30, du
  côté du ponton), **Hinano** la maman et **Manu** son fils (maison pastel, 9 h - 18 h, parvis de l'aquarium ; Manu suit
  sa maman, `_leader`), **Mamie Rose** (chaumière, 10 h - 17 h, devant chez elle), **Chiyo** la vieille ama (baie d'Izu, toujours chez elle : 9e maison
  d'habitant, façade de la première maison de pêcheurs, cabane aux tatamis `HomeArt.AmaHut`, foulard `NpcExtras.Headscarf` ; donne l'épuisette,
  pas de commandes, absente de l'onglet Quêtes). Dessinés par le même peintre que le
  pêcheur, dans les 4 directions avec le même pas (`CharacterPainter` + `NpcExtras` : barbe, bonnet à pompon, jupe,
  pantalon et bottes, lunettes, fleur dans les cheveux, taille d'enfant ; images faites au lancement par
  `CharacterSprites.Build(look, extras)`). **Leur journée** (`Villager`) : la nuit debout chez eux (« Habitant id » dans
  la maison) ; à l'heure de sortie ils apparaissent devant leur porte, marchent jusqu'à leur coin, s'y baladent (un point
  au hasard dans `_wanderRadius`, une pause, un autre point ; `VillagerSchedule`, testé), puis rentrent à pied le soir.
  **La nuit tombée** (lampadaires tout allumés, `VillagerSchedule.IsDark`, de 19 h 30 à l'aube), personne ne sort et
  celui qui n'est pas encore rentré rentre d'un coup chez lui (sauf en pleine conversation). Ils glissent le long des
  obstacles et sautent au but s'ils restent bloqués 1,5 s. Ils s'arrêtent et se tournent vers
  le joueur quand il est tout près. **Dialogues** (`UI/DialogueBox`, objet « Partie », canvas ordre 14) : près d'un
  habitant (dehors, ou chez lui quand on est dans sa maison), « E : parler à … » ; E ouvre une boîte en bas de l'écran
  (cadre de la charte, bandeau au nom), E / Espace : suite, Échap : fermer, joueur immobile. Répliques
  (`VillagerLines`, choisies par `VillagerDialogue`, testé) : la présentation au premier échange (rencontres retenues
  par `VillagerMemory`, sauvegardées dans `SaveData.MetVillagers`), puis une réplique de tous les jours, de pluie, de
  nuit ou sur l'aquarium (« {0} » = espèces exposées). Le sac ne s'ouvre pas avec E quand un habitant est à portée.
  **Commandes** (`VillagerRequest`, règles `RequestRules` testées, état de tous dans `VillagerRequests`, sauvegardé
  dans `SaveData.VillagerRequests`) : chacun demande une espèce à la fois, tirée dans ses zones préférées
  (`VillagerProfile._zones` : Tavita Mahana et Maldives, Hinano le lac, Manu Grande Barrière et Amazonie, Mamie Rose la
  baie japonaise ; communes 3, peu communes 2, rares 1, jamais d'épique ni de légendaire). Bulle « ! » au-dessus de sa
  tête (`BubbleArt.Exclamation`, `Art/UI/Bulle_Commande.png`) tant qu'il ne l'a pas demandée ; une fois demandée, bulle
  avec la petite image du poisson (`BubbleArt.Wide` + `FishSpecies.MiniSprite`) quand on l'a dans le sac. En lui
  parlant : il demande (`Lines.Ask`), prend le plus petit exemplaire et paie 2 × prix de base + 5 / 15 / 40 selon la
  rareté (`Lines.Thanks` + « (+N pièces) »), refuse le seul exemplaire d'une espèce pas encore exposée (`Lines.KeepIt`),
  ou rappelle sa commande une fois sur deux (`Lines.Waiting`). Servi : la commande suivante arrive le lendemain matin,
  quand il ressort de chez lui (`DoneToday`).
  **Onglet « Quêtes » de l'inventaire** (`UI/QuestPage`, 3e onglet après « Sac et matériel » et « Reliques », Tab ou
  flèches pour changer ; `InventoryHud.OpenQuests` pour les tests) : une carte par habitant, 2 par rangée, avec son
  portrait, où il est (à la maison / en balade), le poisson demandé (petite image, nom, zone où le pêcher, récompense)
  et l'état (`Villager.Quest`, `Villagers/QuestStatus`) : nouvelle commande à écouter (« ??? »), à pêcher, dans le
  sac (à apporter), seul exemplaire à mettre d'abord à l'aquarium, servi aujourd'hui.
- **Animalerie** (`World/PetShopBuilding`, objet « Animalerie », dessins `Editor/PetShopArt`, style en volume 32 px
  par case) : au sud-ouest de l'île, entre la maison et la plage, loin du laboratoire (`MapSetup`, site cherché depuis
  centre + (-16, -12)), petite cour pavée et chemin vers le chemin principal (`TownLayout.DecoratePetShop` : massifs,
  buisson, un lampadaire). Façade 160×144 px : bardage crème, toit de tuiles corail, enseigne ronde à la patte, vitrines
  avec un chiot et un chat roux, porte vert d'eau, paillasson à pattes. Intérieur 11×8 cases (hors de l'île, au-dessus
  de celui du laboratoire) : papier peint à pattes, parquet, rayon de croquettes, perroquet en cage, colliers et laisses,
  arbre à chat, clapier, panier du chien ; comptoir (`Animalerie_Comptoir.png`, chat endormi) et vendeur roux au tablier
  vert d'eau (`CharacterArt.PetKeeper`). Obstacles : `PetShopBuilding.Counter` / `Shelf` / `CatTree` / `Hutch` /
  `DogBed`. Icône sur la minimap.
  **Animaux de compagnie** (`Scripts/Pets`, données `PetDefinition` / `PetCatalog` dans `Data/Animaux/`, images
  `Editor/PetArt` dans `Art/Animaux/` : 3 directions × 3 images, la gauche en miroir ; générés et posés par
  `Editor/PetSetup`) : **chat** roux tigré (300 pièces, les poissons mordent un peu plus vite : attente × 0,85),
  **bouledogue** français fauve (300 pièces, trouve parfois 5 à 15 pièces en marchant : une chance sur 75 par seconde
  de marche, bulle avec une pièce `BubbleArt.Coin`), **perroquet** vert qui vole à hauteur d'épaule (450 pièces, rares
  et plus × 1,35 ; `PetMath.RarityFactor`), **manchot** qui se dandine (450 pièces, le marchand paie × 1,2 :
  `PetDefinition.SellFactor`, `PlayerProgress.SellFactor` passé à `FishShop.PriceOf` / `Sell` / `SellAll`, prix montré
  sur la carte de prise et chez le marchand), **crocodile** (500 pièces, prises plus grosses : `PetMath.SizeRoll`, tirage
  de taille penché vers le haut, `SizeBias` 1,8), **grosse araignée** mygale aux genoux orange (400 pièces, zone de
  réussite du QTE × 1,2 sans dépasser 60 % : `PetMath.ZoneWidth`, pas contre les légendes). Achat chez le vendeur
  (`UI/PetShopMenu`, objet « Partie », canvas ordre 13) : devant le comptoir « E : parler au vendeur », une carte par
  animal (portrait, bonus, prix ou « Avec toi ») et une carte « Aucun animal », sur deux rangées de 4 ;
  Gauche / Droite (Haut / Bas : rangée), E ou Espace : acheter (il accompagne aussitôt le joueur) ou reprendre un animal
  déjà à soi, Échap : fermer. Un seul animal à la fois (`PetCollection`, testée : achat, choix, rechargement), gardé
  dans `PlayerProgress.Pets` et sauvegardé (`SaveData.Pets`, `EquippedPet`). Il suit le joueur (`Pets/PetFollower`,
  objet « Animal de compagnie ») le long de son chemin à une case derrière lui (`PetMath.FollowPoint`), s'arrête avec
  lui et le rejoint d'un coup après une porte ou un voyage (plus de 5 cases d'écart). **Pendant la pêche**, il vient
  s'asseoir à côté du joueur, sur son flanc gauche sinon droit, sur la terre ferme (`PetMath.SitPoint`), et regarde l'eau
  avec lui. **À chaque prise** il saute de joie à sa façon (`PetMath.ReactionOffset`, 1,2 s : le chat fait un grand bond, le
  chien frétille en sautillant, le perroquet s'envole un peu, le manchot se dandine, le crocodile se cabre, l'araignée
  trépigne), avec une **bulle cœur** (`BubbleArt.Heart` → `Art/UI/Bulle_Coeur.png`) pour une belle prise
  (`PetMath.HeartFor` : rare et plus, nouvelle espèce) et pour une légende vaincue. **Caresser** : tourné vers lui, à
  moins d'une case et demie (`PetMath.CanPet`), « E : caresser ton compagnon » ; il saute de joie avec un cœur. C'est
  `InventoryHud` qui reçoit E et le passe à l'animal (`PetFollower.Pet`), pour garder les priorités de la touche ; dos
  tourné, E ouvre toujours le sac. Bonus appliqués par
  `FishingController` (tirage des espèces et attente de la touche). L'emplacement « ANIMAL » de l'inventaire montre son
  portrait et son nom.
  **Attente** (`Player/IdleMath`, testé) : joueur et animal immobiles 4 s, l'animal prend sa pose de face
  (`PetDefinition.Idle`, `PetArt.DrawIdle`, 2 images : la pose puis le geste qui revient) : le chat s'assoit et se lèche
  la patte, le bouledogue se couche et bâille, le manchot bat des ailerons le bec au ciel, le crocodile ouvre grand la
  gueule, l'araignée se frotte les pattes avant, et le **perroquet vient se poser sur le chapeau du pêcheur** (perché,
  il passe devant lui : ordre 11) ; il redécolle dès qu'on bouge.
- **Aquarium** (`AquariumBuilding`, objet « Aquarium ») : un des plus grands bâtiments du jeu, **une tour**. Façade
  `AquariumArt.TowerExterior` (192×320 px, pivot `TowerPivot`) : en bas l'ancien bâtiment sans son dôme (perron,
  colonnes, 4 vitrines, rosace), au-dessus 4 étages de carreaux et de bandeaux de laiton, hublots où nagent des poissons,
  cage d'ascenseur vitrée au milieu avec sa cabine, et le dôme de verre et sa lanterne au sommet. Emprise au sol 11,2×5,2,
  site cherché autour du centre de l'île avec 9 cases de marge, la boutique à ≥ 14 cases.
  **Un étage par zone** (`AquariumBuilding.Floors`, ordre des zones dans `_floorZones`) : rez-de-chaussée = grand hall du
  lagon (30 cases : bassin tropical, allée en mosaïque, médaillon, une colonne à méduses, bancs), étage 1 = salle du lac
  (`AquariumLifeArt.LakeRoom` : lambris, parquet, tapis, bassin d'eau douce aux galets avec souche), étage 2 = salle
  Amazonie (`AquariumAmazonArt` : tomettes, tapis à zigzags, mur de feuilles, bassin d'eau noire couleur thé, racines ;
  `AmazonTank`, plantes épées, cabombas, néons cardinaux), étage 3 = salle Japon (`AquariumJapanArt` : tatamis, shōji,
  branches de cerisier, bassin d'eau de mer aux rochers sombres ; `JapanTank`), étage 4 = Grande Barrière
  (`AquariumReefArt` : cabane de plage, fresque marine, sable blanc et caillebotis ; `ReefTank`), étage 5 = atoll.
  Dans la scène, les salles restent côte à côte (intérieur 134×18 cases) mais les passages sont murés
  (`AquariumAmazonArt.CloseLeftPassage`, sols marchables salle par salle) et la caméra est bornée à l'étage courant
  (`InteriorBuilding.CameraRect`). **Ascenseur** à chaque étage, dans le mur du bas qu'on ne voit
  pas : seul un **tapis** contre ce mur l'indique, comme un paillasson (`ElevatorSpots` = milieu du tapis : hall à
  droite du paillasson de sortie, autres salles au milieu ; dessin `ElevatorArt.Mat`, velours bordeaux, cadre de laiton,
  losange aux deux triangles, ordre -5 au sol). Marcher vers le bas sur le tapis (ou **E** dessus) ouvre
  `UI/ElevatorMenu` (étages du haut vers le bas, « RDC » / « Étage N », nom de la zone, espèces exposées / total,
  Haut / Bas, E ou Espace pour y aller, Échap pour fermer), fondu au noir, `AquariumBuilding.GoToFloor` (arrivée un pas au-dessus du tapis), bandeau
  « Étage N — Zone ». Ajouter une zone = une salle de plus, un `Floors` / `ElevatorSpots` et sa zone dans `_floorZones`.
  Le paillasson de sortie n'est qu'au rez-de-chaussée (entrée au milieu du hall, `InteriorBuilding.EntranceX`).
  Un `AquariumTank` par bassin (`_zone`, `_area` = `HallTank` / `LakeTank`) : chaque bassin montre les espèces de sa zone,
  à leur taille réelle (échelle de `FishDisplayScale`). **Vie des bassins** (`World/TankLife`, dessins `AquariumLifeArt`) :
  plantes qui ondulent (laminaires, herbiers / potamots, élodées ; derrière et devant les poissons), bulles, rayons de
  lumière, bancs d'anthias / de vairons, reflets d'eau au sol, méduses qui pulsent dans les colonnes. Ordres : fond -6,
  plantes du fond et reflets -5, poissons -4, plantes de devant, bulles, rayons -3, vitre -2, mobilier 10, méduses 11.
  **Chaque salle a son caractère** (`AquariumFloorArt` pour les sols, `AquariumPropArt` pour le mobilier, disposition dans
  `AquariumRoomLayout`, posé par `MapSetup` et rendu solide par `AquariumBuilding._furniture`) : lac en chalet (parquet en
  vannerie, tapis rond tressé, barque exposée, bancs en rondins, sapins en pot), Amazonie en ponton (planches usées,
  mousse, feuilles, pirogue, tronc moussu, monsteras et palmiers), Japon en jardin zen (gravier ratissé en cercles autour
  des rochers, pas japonais, engawa devant le bassin, bonsaï, lanternes et bancs de pierre). Les bancs en fer ne
  restent que dans le grand hall (`AquariumBuilding.Benches`).
  **9e étage, baie d'Ha Long** (`AquariumHaLongArt`, `HaLongTank`, mobilier `AquariumRoomLayout.HaLong`) : tomettes de
  terre cuite, mur de panneaux laqués rouges à liserés dorés, bassin d'eau émeraude aux petits pitons calcaires
  verdoyants, sable clair, herbiers et bambous d'eau ; jonque miniature sur son socle, bambous en pot, lanternes de soie.
  **Visiteurs mis de côté** pour l'instant : `World/AquariumVisitors` et `MapSetup.BuildVisitors` (pêcheur recoloré,
  promenade devant les bassins, +1 toutes les 6 espèces) existent mais ne sont plus appelés. La faune de la zone
  (`AmbientLife`) se cache à l'intérieur des bâtiments. Obstacles : `AquariumBuilding.JellyfishColumns` / `Benches`. Devant le bassin, **E** dépose le sac (`AquariumCollection.Deposit`, testé) : le plus
  gros de chaque nouvelle espèce entre ; **la taille ne compte pas ensuite** : une espèce déjà exposée n'est jamais
  remplacée, même par un plus gros, et tous ses exemplaires restent dans le sac pour le marchand (`FishShop` les achète).
  Les poissons nagent dans le bassin (`AquariumTank`) avec leur **miniature animée** (`FishSpecies.MiniFrames`, 4 images,
  `MiniSprite` = la première), entre le fond (ordre -5) et la vitre (`Aquarium_Vitre.png`, ordre -3). Miniatures faites
  par `FishArt.GenerateMiniFrames` → `Art/Fish/Mini/<id>.png` et `<id>_1..3.png` : **32 px par case** (comme le décor),
  déjà à leur taille d'affichage (longueur de 24 à 104 px selon la taille typique de l'espèce, échelle de
  `FishDisplayScale`), donc plus aucune échelle fractionnaire qui déforme les pixels ; réduction fine du grand dessin
  rogné (`PixelArt.Resize`, moyenne pondérée des pixels recouverts ; seuil bas pour les bêtes du fond, ce qui garde les
  pattes fines du crabe-araignée), contour léger. Animation : les poissons **battent de la queue** (colonnes de l'arrière
  qui montent et descendent, `TailBeat`), les bêtes du fond **marchent** (bandes de pattes qui avancent et reculent,
  corps qui se soulève, `Scuttle`). Dans le bassin : **demi-tours en douceur** (le dessin se rétrécit, se retourne,
  s'élargit), **coups de queue** (accélération qui retombe), **bancs** de 3 ou 5 pour les petites espèces
  (`AquariumMath.SchoolSize`, les copies suivent le premier), **trois plans** (`AquariumMath.PlaneTint` / `PlaneSpeed` /
  `DepthInPlane` : le fond plus sombre, plus bleu, plus lent et plus haut, le devant vif et plus bas), position calée
  à 32 px par case. **Vue de près** (`UI/AquariumCloseUp`, canvas ordre 16, sur « Partie ») : devant la vitre, l'aide
  ajoute « Haut : regarder de près » ; tenir Haut 0,2 s zoome la caméra sur le bassin en 0,45 s
  (`AquariumMath.CloseUpSize` : le bassin remplit l'écran en hauteur), Gauche / Droite longent la vitre, la souris ou
  Espace (poisson suivant, la vue le suit) choisissent un pensionnaire : coins dorés autour de lui et fiche en bas à
  gauche (nom à la couleur de sa rareté, nom savant et nature, rareté et taille, description). Échap, Bas ou E reviennent.
  Le pêcheur est immobile pendant ce temps. L'index des poissons indique les espèces exposées.
- `UI/InventoryHud` (canvas ordre 11) : **icône de sac** en bas à gauche (compteur « 2/3 », `UiArt.BagIcon`), **E** ouvre la
  **grande fenêtre « Mon inventaire »** au centre (joueur `Locked`, E/Échap ferme) : à gauche la **vitrine « Mon matériel »** (fond de feutre :
  la **canne entière** en grand, `GearLevel.Illustration` = `GearArt.RodLarge` 96×160 affiché ×3, son fil qui finit au pixel
  (86, 62) où pendent l'**hameçon** et l'**appât** accroché, le sac en bas et, à côté, l'**emplacement « ANIMAL »**
  (`InventoryHud.BuildPetSlot` : portrait et nom de l'animal qui accompagne le joueur, sinon empreinte `PetShopArt.PawIcon` et « Aucun » ; achat à
  l'animalerie) ; étiquettes nom, effet, niveau en pierres), à
  droite **« Mes poissons »** en grille de 3 colonnes (dessins à l'échelle, places pas encore achetées verrouillées,
  6 cases affichées). Une icône 32×32 par objet (`GearArt`, `Art/UI/Materiel/`, posée par `GearSetup` via
  `GearLevel.WithIcon`). Devant le comptoir, E parle au marchand à la place. Sac plein après une prise : la carte de
  prise passe à gauche (×0,82) et la fenêtre s'ouvre réduite (×0,6) à droite, places qui clignotent. Pièces en haut à droite.
  **Souris** (`InventoryHud.HandleMouse`, sans EventSystem) : clic sur l'icône pour ouvrir, sur un onglet, à côté de la
  fenêtre pour fermer ; clic sur la canne, l'hameçon ou l'appât (dessin ou étiquette, qui grossit au survol) ouvre le
  **sélecteur** (`UI/GearPicker`, par-dessus la grille des poissons) : une carte par canne ou hameçon (ceux pas encore
  achetés voilés, prix dans le bandeau) ou par appât possédé, celle en main bordée d'or « En main », atouts des cannes en
  pierres (`Gear/GearStats` : zone, solidité, portée, touches), bandeau du bas = effet de l'objet survolé, 8 cartes par
  page (molette, plaques Précédent / Suivant) ; clic sur un objet à soi = le prendre en main, Échap ou E = retour. R, F, Q
  font toujours défiler ; sac plein, clic sur une case = relâcher ce poisson. `FishingInput.PointerOnInterface` empêche ces clics de
  lancer la ligne.
  Icônes de la boutique et de l'aquarium sur la minimap (`Minimap._landmarks`).
- **Index des espèces = carnet du pêcheur** (`UI/FishIndex`, objet « Partie », canvas ordre 12) : icône de livre à
  droite du sac (compteur « 12/29 », `UiArt.BookIcon`), **Tab** (ou Y manette, `IndexInput`) ouvre un carnet sur deux pages
  (dessins `BookArt` : cuir cousu, papier, cases encrées, onglets, pli, tampon, soleil/lune/pièce ; `Art/UI/Carnet/`).
  **Colonne des zones** à gauche du carnet (le carnet est réduit à ×0,86 et décalé à droite pour lui laisser la place) :
  étui de cuir, feuille, bandeau « Zones », un marque-page par zone (nom + « découvertes / total », « Complet » en or),
  la zone choisie en couleur, plus large et entourée d'un halo doré. Vue masquée qui **défile** dès qu'il y a plus de
  7 zones (`ZoneListScroll`, testé : la zone choisie reste toujours visible avec un peu de marge, glissement doux),
  repères « Plus haut » / « Plus bas » et « Zone 3 / 6 ». **Navigation** : à l'ouverture la colonne a la main
  (Haut / Bas = changer de zone, halo qui pulse) ; Droite, Entrée ou A manette passent à la grille ; Gauche depuis la
  première colonne de la grille revient à la liste (`FishIndex.FocusZoneList`) ; Q/E (A/E en AZERTY) et les gâchettes
  hautes changent de zone partout. **Souris** (`FishIndex.HandleMouse`, `Mouse.current`, sans EventSystem) : clic sur
  l'icône du livre pour ouvrir ; clic sur une zone (nom en or au survol) ou sur une case (éclaircie au survol) ; molette
  sur la colonne = faire défiler la liste, sur le carnet = page d'espèces suivante / précédente, comme les deux flèches
  à l'encre autour de « Page 1 / 2 » en bas de la page de gauche (`FishIndex.StepPage` : cliquables, plus grosses au
  survol, pâles au bout, cachées s'il n'y a qu'une page) ; clic à côté du carnet
  = fermer. Page de gauche : nom et pays de la
  zone, barre de progression avec **paliers 25/50/100 %** (`CollectionRewards`, testé : 50/150/400 pièces, encaissés
  automatiquement dès qu'ils sont atteints, bandeau « Carnet — … +N pièces » même carnet fermé, clés « zone:palier »
  dans `PlayerProgress.ClaimedRewards` et `SaveData.Rewards`), tampon « COMPLET » à 100 %, grille 4×3 (dessins à
  l'échelle, silhouettes « ??? »). Page de droite : grand dessin, rareté, heure (soleil/lune), zone, **description
  réelle** (`FishSpecies.Description`, colonne Description de la table des espèces), prises / record / taille, aquarium ;
  pour une espèce inconnue, un **indice** (heure, rareté, taille). « Découverte » = déjà pêchée une fois (`CatchLog`).
  **Page qui tourne** (`FishIndex.TurnPage`) quand on change de zone ou de page d'espèces : une feuille de papier pivote
  autour du pli (0,45 s), s'assombrit par la tranche et projette une ombre ; comme dans un vrai livre, la page que la feuille découvre se met à jour tout de suite, celle sur laquelle elle se rabat seulement à la fin (`RefreshOrWait`, `Refresh(left, right)`).
  Logique dans `SpeciesIndex` et `CollectionRewards` (testées). La police n'a ni œ ni ā : les éviter dans les textes.
  **Page Épuisette** : deux onglets au-dessus de la page de gauche, « Poissons » et « Épuisette » (clic, touche physique R
  ou X / Carré, `FishIndex.SetNetMode`). Même carnet, mêmes zones, mais les listes viennent de `FishingZone.NetSpecies`
  (compteurs de la colonne compris) ; chaque espèce y a son vrai dessin, à l'échelle de sa taille (la bestiole `_netCritter` ne sert
  qu'à défaut). Pas de paliers de pièces sur cette page (les
  récompenses restent celles des poissons) ; indice « Ramasse-la à l'épuisette dans les bulles ! », « Sort la nuit » au
  lieu de « Mord la nuit », et « notée puis relâchée » au lieu de l'aquarium. Le compteur de l'icône reste celui des poissons.
- Le bouton Play lance toujours `FishingPrototype` (`PlayModeStartScene`), même si une autre scène est ouverte.
- **Ne pas lancer les tests pendant que l'éditeur entre ou sort du Play mode** : le lanceur de tests plante et
  laisse une scène vide ouverte.
- **Sauvegarde automatique** (`Scripts/Save`) : `SaveManager` (objet « Partie », champs `_progress`, `_fishing`,
  `_zones`) charge au démarrage et réécrit 1 s après chaque changement (`PlayerProgress.Changed`, `CatchLog.Changed`)
  et en quittant. Fichier `sauvegarde.json` dans `Application.persistentDataPath`
  (`%USERPROFILE%/AppData/LocalLow/FishAndChill/FishAndChill`), écrit via un `.tmp`. Contenu (`SaveData`, `Version`) :
  pièces, sac, aquarium, niveaux de matériel, journal des prises ; espèces par `Id` (une espèce disparue est ignorée).
  Logique dans `SaveSerializer` (testée). **3 emplacements** (`SaveSlots`, testé) : `sauvegarde.json` (emplacement 1,
  ancien nom gardé), `sauvegarde2.json`, `sauvegarde3.json` ; `SaveData` garde aussi `PlaySeconds` et `SavedAtTicks`.
  `SaveManager.ActiveSlot` (statique, -1 = écran titre, remis à -1 au lancement car le rechargement du domaine est
  désactivé) : sans emplacement choisi, rien n'est chargé ni écrit. `PlaySlot` / `ReturnToTitle` rechargent la scène.
  Menu **FishAndChill → Sauvegarde → Effacer la sauvegarde** pour repartir de zéro.
- **Écran titre** (`UI/TitleScreen`, objet « Partie », canvas ordre 30) : posé sur l'île vivante au lancement, masque
  les autres interfaces et bloque le jeu. Logo « Fish & Chill » qui flotte, 3 cartes « Partie 1/2/3 » (zone, espèces
  x / total, pièces, temps de jeu, date), « Quitter le jeu » dessous. ←/→ choisir, Bas/Haut, Espace jouer (nouvelle
  partie si vide), Suppr/Retour arrière/X effacer (deux fois). Curseur dans `TitleScreenLogic` (testé).
  **Vivant** : décor = zone et heure de la partie jouée le plus récemment (`SetBackdrop`, `ZoneTravel.TravelTo` sans
  sauvegarde puisque `ActiveSlot` = -1), « Dernière escale : … » sous le logo, poissons de cette zone qui sautent en arc
  de derrière les emplacements devant le logo, bulles qui montent.
  Par script : `TitleScreen.Play(slot)` (sinon le prototype reste sur l'écran titre). Nouvelle zone = l'ajouter au champ `_zones` du `SaveManager`, sinon ses prises ne se rechargent pas.
- **Création du pêcheur** (`UI/CharacterCreator`, ouvert par `TitleScreen` sur un emplacement vide) : prénom tapé au
  clavier (14 caractères, `Keyboard.onTextInput`), teint, coiffure (courte, hérissée, mi-longue, longue, queue de cheval,
  chignon, rasée), couleur des cheveux, chapeau (aucun, paille, casquette, bob, bandana), haut (chemise à fleurs, t-shirt,
  débardeur, marinière) et couleurs du haut et du short ; « Au hasard », « C'est parti ! ». Haut/Bas, ←/→, Entrée, Échap ;
  les lettres servent au prénom. Grand aperçu qui marche et se retourne. Choix et prénom dans `Player/CharacterOptions`
  (testé), apparence `Player/CharacterLook` (valeurs par défaut = pêcheur d'origine), sauvegardée (`SaveData.Look`).
  Le pêcheur est **dessiné en jeu** (`Player/CharacterPainter`, `CharacterSprites`, outils `PixelShading`) et appliqué par
  `SaveManager` (`PlayerAnimator.SetLook`) ; `CharacterArt` (éditeur) s'en sert pour les PNG par défaut. **Attente** :
  immobile et libre 6 s (ni menu ni pêche), le pêcheur se tourne vers nous et regarde à gauche, à droite, tape du pied,
  puis s'étire en bâillant (bras levés devant le chapeau, yeux fermés), toutes les 12 s (`IdleMath.PlayerPose`,
  poses `CharacterPainter.Idle` → `CharacterSprites.Idle`). Le prénom
  remplace « Partie N » sur la carte de l'écran titre. Par script : `TitleScreen.OpenCreator(slot)` ;
  `TitleScreen.Play(slot)` garde le pêcheur par défaut.
- **Menu pause** (`UI/PauseMenu`, objet « Partie », canvas ordre 20) : **Échap** ou Start, seulement si aucun autre
  menu n'était ouvert à l'image précédente (Échap ferme d'abord sac, livre, marchand, grande carte). Fige le jeu
  (`Time.timeScale = 0`, `FishingController` et `Minimap` désactivés, joueur `Locked`), reprise une image plus tard.
  Lignes : Reprendre, Volume (←/→, `AudioListener.volume`), Plein écran, Retour au titre (sauvegarde puis
  `SaveManager.ReturnToTitle`), Quitter. La police n'a ni ↑ ↓ ni ▼ : écrire « Haut / Bas ». Options dans `PlayerPrefs`. Calculs dans
  `PauseMenuLogic` (testés), touches dans `PauseInput`.

