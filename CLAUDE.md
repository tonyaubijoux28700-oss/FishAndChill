# FishAndChill

Jeu PC de collection de poissons par la pêche : le joueur pêche (QTE à jauge de timing),
remplit un aquarium géant avec toutes les espèces, revend les doublons pour améliorer son
matériel et voyage dans des zones réelles pour trouver de nouvelles espèces.
Conception détaillée : [docs/GAME_DESIGN.md](docs/GAME_DESIGN.md).

## Projet technique

- Unity 6 (6000.6.9f1), 2D, URP (Renderer2D), nouveau Input System.
- Plateforme cible : PC (clavier/souris + manette).
- Vue de dessus, pixel art.
- Dépôt : https://github.com/tonyaubijoux28700-oss/FishAndChill (public), branche `main`.

## Piloter Unity

- L'éditeur se pilote via la Unity CLI (`unity command <groupe> <commande> --project-path .`)
  et le serveur MCP `unity-editor` déclaré dans `.mcp.json` (package `com.unity.pipeline`).
- Ouvrir le projet : `unity open .` ; état de la connexion : `unity status`.
- Vérifier la console après chaque changement de code : `unity command console get --level warn`.
- Ne pas modifier à la main un fichier `.unity` / `.prefab` ouvert dans l'éditeur : passer par
  les commandes de l'éditeur, sinon Unity écrase les changements.

## Prototype de pêche

- Scène : `Assets/_Project/Scenes/FishingPrototype.unity` (Espace / clic / bouton A = action unique).
- Déplacement : ZQSD (lu par position physique `Key.W/A/S/D`, donc ZQSD en AZERTY et WASD en QWERTY),
  flèches ou stick gauche ; bloqué tant que la ligne est à l'eau (`PlayerMovement`).
- Données (zones et espèces) et scène régénérables via le menu
  **FishAndChill → Prototype → Générer données et scène de pêche** (`PrototypeSetup.cs`, relançable).
- L'affichage (`FishingPrototypeView`) est provisoire et construit par le code ; la logique est dans
  `FishingController` (états) et `Fishing/Core` (QTE pur, testé).
- Déroulé : `Idle` → **`Casting`** (le bouchon vole en arc, durée selon la distance) → `Waiting` avec **1 à 3
  titillements** (`NibblePlanner`, testé ; événement `Nibbled`, ferrer dessus = trop tôt) → `Bite` (le bouchon coule)
  → `Reeling` (QTE). Rendu (`FishingPrototypeView`) : canne et fil dessinés pixel par pixel (`PixelPolyline`), la canne
  arme puis fouette au lancer, le fil pend plus ou moins, ronds dans l'eau (plouf, titillements, touche,
  éclaboussures du combat). Jauge du QTE avec la charte (cadre, bandeau « Ferre-le ! », barre d'eau, zone verte,
  aiguille, pastilles réussites/ratés, flash et secousse), placée du côté opposé au bouchon.
- **Coins de pêche** (`World/FishingSpots`, un par zone : « Coins de pêche de l'île / du lac », règles dans
  `Fishing/Core/FishingSpotRules`, testées) : 3 coins à la fois, en eau libre, chacun face à une rive où l'on peut se tenir
  (2,2 à 4,2 cases) ; durée 70-140 s ou 2 prises (`Consume`), puis il réapparaît ailleurs avec une autre sorte.
  `School` (banc : ombres qui tournent sous l'eau, touche ×0,45 du temps, communes favorisées), `Bubbles` (bulles :
  crustacés, tortue, grenouille ×4), `Frenzy` (chasse : éclaboussures, petits poissons qui sautent, rares à légendaires
  ×2,5 à 3,5). Remous permanent qui pulse sur chaque coin. **Visée** : face à un coin à portée (à moins de 32°), le
  lancer y va tout seul (`FishingSpotRules.Assist`, dans `FishingController.TryFindCastPoint`) ; message propre au coin
  quand le bouchon y tombe (`FishingController.InSpot`, `SpotKind`).
- **Combats par espèce** (`FishSpecies.FightStyle`, `Fishing/Core/FightStyle`, logique dans `QteChallenge`, testée) :
  `Calm` (jauge simple), `Nervous` (curseur entre 0,5 et 1,5× sa vitesse, `TimingGauge.SpeedMultiplier`), `Drifting`
  (la zone glisse, `DriftSpeed`), `Rusher` (la zone saute loin toutes les 1,6-3 s, annoncée 0,45 s avant : zone qui
  clignote en rouge, puis secousse), `Heavy` (tension +0,12/s, -0,3 par réussite, +0,22 par raté ; pleine = ligne
  cassée, `QteFailReason.LineBroke` ; la canne ralentit la tension). Titre de la jauge selon le style (« Il est lourd ! »),
  conseil au début (`FishingController.FightHint`), barre « Tension » entre les pastilles. Styles réglés dans la colonne Combat de la table des espèces d'après le vrai comportement (barracuda, brochet, crustacés : rushs ; carpe, tanche,
  carangue géante, mérou, crabe, tortue : lourds ; truites, banane de mer : fuyants ; petits poissons : nerveux).
- **Touche rare** (`Fishing/RareBiteEffect`, sur l'objet du `FishingController`) : quand une espèce rare ou mieux mord,
  étincelles de la couleur de la rareté qui jaillissent du bouchon (7 / 14 / 18, deux gerbes pour une légendaire) ;
  épique et légendaire : bandeau en haut (« Quelque chose de gros a mordu ! », « Une prise LÉGENDAIRE a mordu ! »),
  effacé dès que le combat commence ; flash doré pour une légendaire. L'espèce reste secrète jusqu'à la prise.
- Le poisson sous l'eau : silhouette vue de dessus (`FishShadowSprites` : tête ronde, pectorales, queue fourchue qui
  ondule), dessinée d'avance en 16 directions × 3 positions de queue × 3 tailles (18/26/36 px), jamais tournée.
- **On ne pêche pas que des poissons** : `FishSpecies` (nom de classe gardé) a un `Kind` (`CreatureKind` : poisson,
  crustacé, reptile, amphibien) et une `BodyShape` (silhouette sous l'eau : poisson, crabe, langouste, tortue,
  grenouille, toutes dans `FishShadowSprites`, chacune animée). Dans l'aquarium, seuls les poissons nagent : les
  autres (`LivesOnBottom`) coulent au fond, marchent lentement sur le sable et font des pauses (`AquariumTank`).
  Dessins des non-poissons dans `CreatureArt` (crabe, langouste, tortue), même style que `DetailedFish`.
  Messages et index écrits pour toutes les espèces (« ta prise », « Index des espèces »).
  Il reste **entièrement dans l'eau** : `FishingController.IsWater` + `FitsInWater` (museau, queue, flancs) ; point
  sous le bouchon et point de départ choisis dans l'eau, et il s'arrête plutôt que d'avancer sur le sable.
- `FishingController.PressAction()` et `Tick()` sont publics : on peut jouer le prototype par
  `unity command script eval` pour le tester sans clavier.
- Tests : `unity command test run --mode EditMode --filter FishAndChill --filter-type assembly --project-path .`

## Carte

- Île de départ sur Tilemaps (`Carte` → `Eau` + `Terre`), générée par `IslandGenerator` (graine fixe,
  96×72 cases, 1 case = 1 unité) via `MapSetup.cs`, appelé par le menu du prototype.
- **Attention** : relancer le menu régénère l'île et **écrase les retouches faites à la main**
  dans la Tile Palette. Une fois l'île retouchée, ne plus relancer la génération de la carte.
- `Terre` est une couche **logique** (renderer désactivé) : une tuile par case de terre.
- **Côtes sans angles (double grille)** : le rendu passe par 3 couches décalées d'une demi-case
  (`Rendu - Lagon`, `Rendu - Sable`, `Rendu - Herbe`) ; chaque tuile lit ses 4 coins (16 masques) et dessine une
  frontière en biseau (`SmoothTerrain`, interpolation bilinéaire, seuil 0,5) avec écume, sable mouillé, lisière.
  Tuiles dans `Art/Tiles/Bords/` (`TransitionArt.cs`).
- `WorldMap.IsLand` utilise **le même champ lissé** : on marche et on vise exactement selon la côte dessinée.
  Pour retoucher l'île à la main, peindre sur `Terre` puis relancer le remplissage des couches de rendu
  (à faire : un menu dédié qui ne régénère que le rendu).
- Le joueur ne marche que sur la terre ; le lancer part dans la direction du regard (`PlayerMovement.Facing`)
  et doit tomber dans l'eau (`CastTargeting`). `CameraFollow` sur la caméra, bloquée aux limites de la carte.
- Tuiles pixel art provisoires (16×16, 16 px/unité, filtre Point) dans `Assets/_Project/Art/Tiles/`,
  **redessinées à chaque génération** : pour garder un dessin fait main, le renommer.
- Zone de départ : **Île Mahana** (fictive, tropicale) avec 16 vraies espèces indo-pacifiques : 13 poissons, 2 crustacés
  (crabe de palétuvier, langouste porcelaine) et 1 reptile (tortue verte). Deuxième fournée (poisson-papillon cocher,
  rouget-barbet à bande jaune, vivaneau à raies bleues, baliste Picasso, poisson-lion radié ; et à Annecy ablette,
  chevesne, tanche, carpe commune, lotte) dessinée dans `ExtraFishArt`.
  Palmiers sur la Tilemap `Décor` (tronc bloquant).
- **Plusieurs zones sur la même grille** : chaque zone est une **région** de `WorldMap` (`SetRegions`, `CurrentRegion`) ;
  `WorldBounds` (caméra, minimap) ne donne que la région actuelle. **Lac d'Annecy** : `LakeGenerator` (île retournée :
  eau au centre, galets, prés ; testé), 80×60 cases, à `LakeGap` = 200 cases à l'est de l'île (au-delà des intérieurs),
  construit par `MapSetup.BuildLake` dans les mêmes Tilemaps (tuiles de l'île recolorées par `LakeArt` : eau alpine,
  galets, prés ; sapins). 13 espèces : 11 poissons (`AnnecyFishArt` + `PikeArt` + `ExtraFishArt`), écrevisse signal et grenouille rieuse
  (`FreshwaterCreatureArt`). Sur place, **pêche seulement** (aquarium et marchand à Mahana).
  **Décor** (`BuildLake`) : roseaux sur la rive au bord de l'eau, nénuphars à 2-4 cases du bord, rochers solides,
  prés fleuris (gentianes, marguerites ; `LakeArt`). **Vie** (`World/AmbientLife`, objet « Vie du lac », dessins
  `LakeLifeArt` → `Art/Ambient/`) : cygnes et canards qui nagent en eau libre (hors ponton), libellules autour du
  joueur, poissons qui sautent près de lui avec un rond dans l'eau, ombres de nuages qui dérivent ; ne tourne que
  dans sa région (`WorldMap.RegionBounds`), purement décoratif. Calculs dans `AmbientMath` (testés).
  **Vie de l'île** (même `AmbientLife`, objet « Vie de l'île », `MapSetup.BuildIslandLife`, dessins `MahanaLifeArt`) :
  raies sous l'eau du lagon (`Swimmer.Order` = -17, sous le sable), bernard-l'ermite qui trottinent sur la plage
  (`_walkers`, toujours à une case de l'eau), papillons, poissons volants (saut long : `_jumpLength`/`_jumpHeight`/
  `_jumpSeconds`), frégate qui traverse le ciel avec son ombre au sol (`_passer`), ombres de nuages.
- **Fleuve Amazone** (3e zone, région 2) : `RiverGenerator` (fleuve qui serpente d'ouest en est, rives de vase, jungle,
  **banc de sable** en `FordX` qui relie les deux rives, bras mort au sud ; testé), 90×64 cases, à `AmazonGap` = 20 cases
  à l'est du lac, construit par `MapSetup.BuildAmazon` (tuiles recolorées par `AmazonArt` : eau café au lait, vase rouge
  sombre, jungle ; grands arbres à contreforts et lianes, 2 verts, solides ; nénuphars géants Victoria ; fougères ;
  héliconias et orchidées). Vie (`BuildAmazonLife`, dessins `AmazonLifeArt`) : dauphins roses, caïmans, morphos, aras
  avec leur ombre. 14 espèces (table des espèces) : 12 poissons (`AmazonFishArt` : piranha, oscar, pléco,
  discus, tucunaré, tambaqui, arowana, surubim, payara, pirarara, gymnote électrique, arapaïma légendaire), matamata et
  crevette d'Amazonie (`AmazonCreatureArt`). Salle Amazonie à l'aquarium (voir Aquarium).
- **Baie d'Izu** (4e zone, région 3, Japon) : `BayGenerator` (mer au sud qui s'enfonce en arrondi, plage de sable gris,
  collines ; testé), 90×64 cases, à `JapanGap` = 20 cases à l'est du fleuve, `MapSetup.BuildJapan` (tuiles `JapanArt` : mer
  profonde, sable volcanique, mousse et pétales ; cerisiers, pins noirs tordus et bambous solides ; lanternes de pierre
  solides nommées `Lampadaire_Toro`, donc allumées la nuit ; torii rouge dans l'eau). Ponton au fond de la baie, vers
  le sud. Vie (`BuildJapanLife`, `JapanLifeArt`) : canards mandarins, pétales de cerisier, grues du Japon. 14 espèces
  (table des espèces, dessins `JapanFishArt`, crabe-araignée géant dans `JapanCreatureArt`), thon rouge
  légendaire. Salle Japon à l'aquarium (voir Aquarium).
- **Grande Barrière** (5e zone, région 4, Queensland) : `ReefGenerator` (terre au nord, plage de sable blanc, brousse,
  crique de mangrove, bande de récif au large `Reef` ; testé), 90×64 cases, à `ReefGap` = 20 cases à l'est de la baie,
  `MapSetup.BuildReef` (tuiles `ReefArt` : mer de corail, sable de silice, herbe sèche ; eucalyptus dans la brousse,
  pandanus en haut de plage, palétuviers sur les berges de la crique, tous solides ; coraux vus à travers l'eau sur le
  récif, dont le lagon clair suit les coraux). Vie (`BuildReefLife`, `ReefLifeArt`) : tortues de mer sous l'eau,
  kangourous sur la plage, papillons Ulysse, pélicans. **Première zone faite uniquement par la table** : 14 espèces
  dessinées par `FishArchetypes` (poisson-clown, barramundi, saumonée, requin à pointes noires, Napoléon, mérou géant
  légendaire…). Salle Grande Barrière à l'aquarium (voir Aquarium).
- **Voyage** (`World/ZoneTravel`, `UI/TravelMenu`, objet « Partie ») : un **ponton** par zone (`MapSetup.BuildDock`,
  2 cases × 6, praticable au-dessus de l'eau via `WorldMap._staticWalkable`, barque amarrée). Sur le ponton, **E** ouvre
  la **carte du monde** (←/→, Espace part, Échap ferme ; le sac ne s'ouvre pas sur le ponton) : fondu au noir, arrivée
  au bout du ponton, zone de pêche (`FishingController.SetZone`), région, minimap (`SetTitle`/`Rebuild`), grand bandeau
  du nom de la zone. Position sur la carte du monde et pays dans `FishingZone` (`_mapPosition`, `_region`).
  **Carte du monde** (`UI/TravelMenu`) : vraie carte en pixel art dessinée au démarrage (280×130, `UI/WorldChart`, testé :
  continents en polygones lon/lat, déserts, toundra, glaces, écume ; projection `ToLonLat`/`FromLonLat` qui place chaque
  zone à sa vraie position, ex. Annecy (6°E, 46°N) = (0,52 ; 0,74)). Vaguelettes, nuages, repères à la couleur de l'eau
  de la zone, onde sur la destination choisie, route en pointillés animés depuis la zone actuelle (chemin le plus court,
  passe par le bord pour traverser le Pacifique), voilier qui navigue jusqu'à la destination (1,5 s) avant le fondu.
  Fiche : barre des espèces découvertes et décompte par rareté.
  **Toutes les destinations sont ouvertes** : l'ordre de déblocage sera conçu plus tard. La zone actuelle est
  sauvegardée (`SaveData.Zone` = nom de l'asset de zone) et on y reprend au lancement.
  Nouvelle zone = une région de plus dans `MapSetup`, une destination dans `PrototypeSetup` (`SetDestination`), et la
  zone dans les `_zones` de l'index et du `SaveManager`.
- **Aménagement** (`TownLayout`, appelé par `MapSetup`) : l'aquarium est au **milieu de l'île** (`LandCentroid`),
  la boutique près du départ. Aménagement **ordonné** : parvis symétrique devant l'aquarium + allée centrale de 3 cases
  dans l'axe de la porte (lampadaires en paires), chemins **droits à angles droits** de 3 cases (`BestZPath` : un seul
  virage choisi pour éviter l'eau, les bâtiments et le sable) : plage → bout de l'allée, boutique → chemin principal
  en T. Décor de l'aquarium posé en miroir (`Mirror`, variantes imposées), lampadaires réguliers à 3 cases minimum
  les uns des autres. Pavés sur la couche `Rendu - Pavés` (double grille, bords arrondis). Décor sur `Décor`
  (`DecorArt`, `Art/Tiles/Decor/`) : haies au pied des façades, massifs, lampadaires, tonneaux et
  caisses de la boutique, panneau indicateur, buissons et rochers. Les cocotiers évitent tout ça (`Result.Reserved`).
  **Décor et troncs de cocotiers sont solides** : leur pied (`TownLayout.Footprint` / `PalmFootprint`) est enregistré
  dans `WorldMap._staticObstacles` à la génération ; `CanStandAt` teste la boîte du joueur contre chaque obstacle.

## Sac, aquarium et pièces

- `PlayerProgress` (objet « Partie ») : `Inventory` (sac, 3 places au départ, `UpgradeCapacity` pour plus tard),
  `AquariumCollection` (un exemplaire par espèce), `Wallet` (pièces). Logique pure testée.
- Chaque prise va dans le sac. Sac plein → état `FishingState.BagFull` : 1/2/3 relâche ce poisson du sac
  (`ChooseReplace`), Espace relâche le nouveau (`ChooseRelease`).
- **Boutique** (`ShopBuilding`, objet « Boutique », placée dans les terres par `MapSetup.FindShopSite`) :
  infranchissable ; on entre en poussant vers la porte (`PlayerMovement.LastInput`), on sort en redescendant sur
  le paillasson. L'intérieur est une pièce hors de l'île (`_interiorOrigin`, sol ajouté via
  `WorldMap.AddWalkableArea`), la caméra s'y cale (`CameraFollow.SetBoundsOverride`). **Pas de pêche dedans**
  (`FishingController.CanFishHere`).
- **Marchand** (`UI/ShopMenu`) : grande fenêtre 1520×940 (canvas ordre 13, lignes jusqu'à 120 px, icônes et textes agrandis) ; E devant le comptoir ouvre le menu (joueur `Locked`), deux onglets (**Tab** ou ←/→) :
  « Vendre » (1, 2, 3… vend un poisson, V vend tout), « Cannes », « Hameçons », « Appâts », « Sacs ». Échap/E ferme. Règles dans `FishShop` (testées) : il n'achète que les doublons (espèce déjà dans
  l'aquarium, ou exemplaires en trop : il laisse le plus gros), prix de 80 % à 130 % du `BasePrice` selon la taille.
- **Matériel** (`Scripts/Gear`, données dans `Data/Gear/`, générées par `GearSetup`) : 3 familles (`GearLine`,
  ScriptableObjects) réunies dans `GearCatalog` (l'appât a son propre système, voir Appâts), chacune avec un niveau 0 gratuit puis 3 améliorations :
  sac (4/5/6 places, niveaux). **Cannes et hameçons = objets à choisir** (pas des niveaux) : on les achète puis on
  équipe celui qu'on veut ; chacun a sa **spécialité** (`GearLevel.WithStats` : curseur, tension, portée du lancer,
  poissons nerveux / rushs / fuyants ; `GearEffects` multiplie canne × hameçon, appliqué par `QteChallenge` et
  `FishingController`). Cannes : bambou, légère (calme les nerveux, tension ×1,25), longue (+2,5 cases), lourde
  (tension ×0,5, rushs espacés), compétition. Hameçons : simple, triple (+2 ratés), circulaire (tension ×0,8, rushs
  rares), forgé (+3 ratés, fuyants ralentis). Possédés dans `GearLoadout` (`Owns`/`Own`/`Equip`/`Cycle`), achat par
  `GearShop.TryBuyItem` / `PlayerProgress.BuyGearItem` (testés), sauvegardés dans `SaveData.GearOwned` (ancienne
  sauvegarde : tout ce qui était sous le niveau atteint est gardé). Marchand : onglets Vendre / Cannes / Hameçons /
  Appâts / Sacs. Inventaire : **R** (physique) change de canne, **F** d'hameçon (gâchettes hautes à la manette). Niveaux du joueur dans
  `PlayerProgress.Gear` (`GearLoadout`), achat par `PlayerProgress.BuyGear` / `GearShop` (testés). Effets calculés par
  `GearEffects` (testé) et appliqués par `FishingController` (QTE, tirage de l'espèce, attente de la touche).
- **Appâts** (`Scripts/Gear` : `Bait` et `BaitCatalog`, ScriptableObjects dans `Data/Gear/Appats/`, générés par `BaitSetup`) :
  ils ne sont plus du matériel à niveaux. Six appâts, chacun avec ses **espèces favorites** (×3) d'après leur vrai régime :
  mie de pain (départ, gratuite, illimitée : poissons blancs), ver de terre (20 p. ×15, touches ×0,85), crevette (35 p.
  ×12 : crustacés, récif), leurre brillant (140 p., réutilisable : chasseurs ; les autres ×0,5), appât lumineux (45 p.
  ×10 : espèces de nuit ×3 la nuit), calmar entier (90 p. ×6 : rares ×2,2). Possédés et accroché dans
  `PlayerProgress.Baits` (`BaitBag`, testé ; sauvegardé : `SaveData.Baits`, `EquippedBait`) ; **un lancer consomme une
  utilisation**, le dernier parti on revient à la mie de pain (message). Effet au tirage et à l'attente dans
  `FishingController.Cast`. Marchand : 3ᵉ onglet **« Appâts »** (Tab/←/→ font tourner les onglets, 1 à 6 achète un paquet,
  accroché tout de suite). Inventaire : la rangée Appât montre l'appât accroché et ce qu'il en reste ; touche physique
  Q (A en AZERTY) ou X manette pour changer. Carnet : « Appât préféré : … » sur chaque fiche (indice même pour les
  « ??? », `BaitCatalog.FavoriteOf`).
- **Bâtiments à intérieur** : base commune `InteriorBuilding` (entrée par la porte, pièce hors de l'île, caméra
  calée, `InteriorBuilding.Occupied` = bâtiment où se trouve le joueur ; pas de pêche dedans). `ShopBuilding` et
  `AquariumBuilding` en héritent. Sites choisis par `MapSetup.FindBuildingSite` (l'aquarium au centre, la boutique à ≥ 14 cases).
- **Aquarium** (`AquariumBuilding`, objet « Aquarium ») : un des plus grands bâtiments du jeu. Façade 12×10 cases
  (`AquariumArt`, 192×160 px, entre la 1re version 6×5 et la version 18×15 jugée trop grande : perron, colonnes,
  4 vitrines, rosace, dôme de verre plein d'eau, lanterne), emprise 11,2×5,2, site cherché autour du centre de l'île
  avec 9 cases de marge, la boutique à ≥ 14 cases. **Cinq salles** (intérieur 134×18 cases) : le **grand hall du lagon**
  (30 cases : bassin tropical, allée en mosaïque, médaillon, colonnes à méduses, bancs) et, par un **passage voûté** dans
  le mur de droite (`PassageBottom`/`PassageTop`), la **salle du lac** (24 cases, `AquariumLifeArt.LakeRoom` : lambris,
  parquet, tapis, bassin d'eau douce aux galets avec souche), puis par un 2e passage au bout de la salle du lac (`AquariumAmazonArt.OpenRightPassage`) la **salle Amazonie** (24 cases, `AquariumAmazonArt` : tomettes, tapis à zigzags, mur de feuilles, bassin d'eau noire couleur thé, racines, feuilles mortes ; `AmazonTank`, plantes épées, cabombas, néons cardinaux), puis au bout la **salle Japon** (`AquariumJapanArt` : tatamis, shōji, branches de cerisier, bassin d'eau de mer aux rochers sombres, laminaires, ulves, bancs de sardines ; `JapanTank`), puis la **salle Grande Barrière** (`AquariumReefArt` : cabane de plage, fresque marine, sable blanc et caillebotis `AquariumFloorArt.Reef`, bénitier géant, transats, ancre, bouées ; bassin de coraux, coraux mous, demoiselles ; `ReefTank`). Entrée au milieu du hall (`InteriorBuilding.EntranceX`).
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
  **Visiteurs mis de côté** pour l'instant : `World/AquariumVisitors` et `MapSetup.BuildVisitors` (pêcheur recoloré,
  promenade devant les bassins, +1 toutes les 6 espèces) existent mais ne sont plus appelés. La faune de la zone
  (`AmbientLife`) se cache à l'intérieur des bâtiments. Obstacles : `AquariumBuilding.JellyfishColumns` / `Benches`. Devant le bassin, **E** dépose le sac (`AquariumCollection.Deposit`, testé) : le plus
  gros de chaque nouvelle espèce entre ; un poisson plus gros remplace l'exemplaire exposé (l'ancien revient dans le
  sac) ; les doublons restent dans le sac pour le marchand. `FishShop` ne vend pas un record de taille.
  Les poissons nagent dans le bassin (`AquariumTank`) avec leur petite version `FishSpecies.MiniSprite`
  (≈ 32 px, réduite au tiers du grand dessin par `FishArt.GenerateMini` → `Art/Fish/Mini/`), entre le fond (ordre -5) et
  la vitre (`Aquarium_Vitre.png`, ordre -3). L'index des poissons indique les espèces exposées.
- `UI/InventoryHud` (canvas ordre 11) : **icône de sac** en bas à gauche (compteur « 2/3 », `UiArt.BagIcon`), **E** ouvre la
  **grande fenêtre « Mon inventaire »** au centre (joueur `Locked`, E/Échap ferme) : à gauche la **vitrine « Mon matériel »** (fond de feutre :
  la **canne entière** en grand, `GearLevel.Illustration` = `GearArt.RodLarge` 96×160 affiché ×3, son fil qui finit au pixel
  (86, 62) où pendent l'**hameçon** et l'**appât** accroché, le sac en bas ; étiquettes nom, effet, niveau en pierres), à
  droite **« Mes poissons »** en grille de 3 colonnes (dessins à l'échelle, places pas encore achetées verrouillées,
  6 cases affichées). Une icône 32×32 par objet (`GearArt`, `Art/UI/Materiel/`, posée par `GearSetup` via
  `GearLevel.WithIcon`). Devant le comptoir, E parle au marchand à la place. Sac plein après une prise : la carte de
  prise passe à gauche (×0,82) et la fenêtre s'ouvre réduite (×0,6) à droite, places qui clignotent. Pièces en haut à droite.
  Icônes de la boutique et de l'aquarium sur la minimap (`Minimap._landmarks`).
- **Index des espèces = carnet du pêcheur** (`UI/FishIndex`, objet « Partie », canvas ordre 12) : icône de livre à
  droite du sac (compteur « 12/29 », `UiArt.BookIcon`), **Tab** (ou Y manette, `IndexInput`) ouvre un carnet sur deux pages
  (dessins `BookArt` : cuir cousu, papier, cases encrées, onglets, pli, tampon, soleil/lune/pièce ; `Art/UI/Carnet/`).
  **Onglets par zone** (touches physiques Q/E = A/E en AZERTY, ou gâchettes hautes) ; page de gauche : nom et pays de la
  zone, barre de progression avec **paliers 25/50/100 %** (`CollectionRewards`, testé : 50/150/400 pièces, encaissés
  automatiquement dès qu'ils sont atteints, bandeau « Carnet — … +N pièces » même carnet fermé, clés « zone:palier »
  dans `PlayerProgress.ClaimedRewards` et `SaveData.Rewards`), tampon « COMPLET » à 100 %, grille 4×3 (dessins à
  l'échelle, silhouettes « ??? »). Page de droite : grand dessin, rareté, heure (soleil/lune), zone, **description
  réelle** (`FishSpecies.Description`, colonne Description de la table des espèces), prises / record / taille, aquarium ;
  pour une espèce inconnue, un **indice** (heure, rareté, taille). « Découverte » = déjà pêchée une fois (`CatchLog`).
  **Page qui tourne** (`FishIndex.TurnPage`) quand on change de zone ou de page d'espèces : une feuille de papier pivote
  autour du pli (0,45 s), s'assombrit par la tranche et projette une ombre ; comme dans un vrai livre, la page que la feuille découvre se met à jour tout de suite, celle sur laquelle elle se rabat seulement à la fin (`RefreshOrWait`, `Refresh(left, right)`).
  Logique dans `SpeciesIndex` et `CollectionRewards` (testées). La police n'a ni œ ni ā : les éviter dans les textes.
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
  Le pêcheur est **dessiné en jeu** à partir de **gabarits faits à la main pixel par pixel** (`Player/CharacterTemplates` : corps, coiffures, chapeaux, une lettre par rôle de couleur ; `Player/CharacterPainter`, `CharacterSprites`, outils `PixelShading`) et appliqué par
  `SaveManager` (`PlayerAnimator.SetLook`) ; `CharacterArt` (éditeur) s'en sert pour les PNG par défaut. Le prénom
  remplace « Partie N » sur la carte de l'écran titre. Par script : `TitleScreen.OpenCreator(slot)` ;
  `TitleScreen.Play(slot)` garde le pêcheur par défaut.
- **Menu pause** (`UI/PauseMenu`, objet « Partie », canvas ordre 20) : **Échap** ou Start, seulement si aucun autre
  menu n'était ouvert à l'image précédente (Échap ferme d'abord sac, livre, marchand, grande carte). Fige le jeu
  (`Time.timeScale = 0`, `FishingController` et `Minimap` désactivés, joueur `Locked`), reprise une image plus tard.
  Lignes : Reprendre, Volume (←/→, `AudioListener.volume`), Plein écran, Retour au titre (sauvegarde puis
  `SaveManager.ReturnToTitle`), Quitter. La police n'a ni ↑ ↓ ni ▼ : écrire « Haut / Bas ». Options dans `PlayerPrefs`. Calculs dans
  `PauseMenuLogic` (testés), touches dans `PauseInput`.

## Cycle jour/nuit

- `World/DayNightCycle` (objet « Partie ») : horloge 0-24 h, une journée = `_secondsPerDay` (840 s = 14 min), nouvelle
  partie à 8 h. Avance seulement en partie (`SaveManager.InGame`) et pas en pause (`Time.deltaTime`). Heure sauvegardée
  (`SaveData.Hour`, 8 h par défaut pour une ancienne sauvegarde) et affichée dans l'en-tête (« Île Mahana   22h30 »).
- Rendu : tous les sprites sont *Sprite-Lit-Default* ; la **Global Light 2D** de la scène prend la teinte de l'heure
  (`DayCycleMath.SkyColor` : nuit bleu nuit, aube rosée, jour blanc, crépuscule orangé). La nuit
  (`LampLevel`, allumage 18 h-19 h 30, extinction 5 h 30-7 h), lumières 2D ponctuelles qui vacillent : chaque tuile
  `Lampadaire` de `Décor`, une lueur devant chaque `InteriorBuilding`, une lanterne près de chaque barque (`_lanterns`).
  Dans un intérieur, lumière de pièce éclairée. Papillons et libellules s'effacent la nuit (`AmbientLife`).
  **Nuit vivante** (`AmbientLife`, dessins `NightLifeArt`, dans les 2 zones) : 6 lucioles qui dérivent autour du joueur et
  clignotent (petite `Light2D` verte chacune), et le **reflet de la lune** sur l'eau (ordre -17, donc seulement sur
  l'eau) qui suit la caméra : posé sur l'eau libre visible la plus proche de `_moonOffset`, il glisse doucement.
  Les deux utilisent `_glowMaterial` = *Sprite-Unlit-Default* d'URP (brillent dans le noir).
  Assembly `FishAndChill` : références URP (`Universal.2D.Runtime` pour `Light2D`).
- **Espèces de jour / de nuit** : `FishSpecies.ActiveTime` (`Always`, `Day` 6 h-20 h, `Night` 19 h-7 h), réglé dans la colonne Heure de la table des espèces d'après le vrai comportement (perroquet, chirurgien, sergent-major, brochet, perche le
  jour ; mérou paon, crabe, langouste, écrevisse la nuit). Le tirage ignore les espèces inactives
  (`FishingController`), l'index affiche « Mord le jour / la nuit ». Calculs dans `DayCycleMath` (testés).

## Minimap

- Petite carte **toujours visible en bas à droite** ; **M** ouvre la grande carte au centre (la petite se masque).
- `UI/Minimap` (objet « Minimap » de la scène) : touche **M** lue comme *lettre* selon la disposition du clavier
  (`FindKeyOnCurrentKeyboardLayout("m")`, donc juste en AZERTY), bouton Select de la manette, Échap pour fermer.
- Image dessinée au démarrage et après chaque voyage (`Rebuild`) depuis les Tilemaps, à `_detail` = 4 pixels par case
  (couleurs par tuile dans `_palette`) : côtes arrondies (interpolation bilinéaire, `MinimapMath.Bilinear`) avec écume,
  eau plus claire près des rives (distance à la terre), grain du sol, chemins pavés (`_paving` = couche `Rendu - Pavés`,
  case pavée si ses 4 tuiles de double grille existent), arbres en petites couronnes ombrées.
- **Carte vivante** : 3 images de vaguelettes qui s'enchaînent, ombres de nuages, coins de pêche qui pulsent (couleur
  selon la sorte, lus dans les `FishingSpots` de la région), flèche rouge dans le sens du regard (`MinimapMath.FacingAngle`)
  avec une onde (`MinimapMath.Pulse`), teinte de l'heure (`DayCycleMath.SkyColor`). Cadres dorés de la charte, nom de la
  zone sur les deux cartes, rose des vents et légende sur la grande. Le jeu continue carte ouverte.
- Le canvas du jeu (`FishingPrototypeView`, ordre 10) passe au-dessus de la minimap (ordre 5).

## Art provisoire (généré par code)

- Tout le pixel art actuel est **provisoire** et **redessiné à chaque lancement du menu** du prototype
  (`PixelArt.cs` pour l'écriture/import des PNG) :
  - **Échelle unique** : 16 px par case et caméra orthographique de taille 6 (`MapSetup.CameraSize`), pour qu'un
    pixel du monde ait la même taille à l'écran qu'un pixel de poisson sur la carte de prise (≈ 5,6 px en 1080p).
  - tuiles détaillées 16×16 (`TileArt.cs`) : eau du large et lagon clair près des côtes (`DistanceToLand`),
    **eau du large animée** (`World/AnimatedWaterTile`, une `Tile` qui enchaîne 4 images de houle, `TileArt.DeepWater(variant,
    frame)`, `MapSetup.SaveAnimatedWater` ; île et lac) ; **reflets qui scintillent** sur l'eau autour du joueur
    (`AmbientLife._glint`, `LakeLifeArt.Glint`, ordre -17 donc seulement sur l'eau) ;
    sable (coquillage, étoile de mer), herbe (hibiscus, frangipanier), variantes choisies par `CellHash` ;
    cocotiers 32×40 sur la Tilemap `Décor` en tri individuel, même ordre que le joueur ;
  - tri par profondeur : Renderer2D en *Custom Axis* (0, 1, 0) — plus au nord = dessiné derrière ;
  - pêcheur **32×48 à 32 px par unité** (même taille à l'écran qu'une case de large et 1,5 de haut, pixels deux fois plus fins que le décor), pivot **aux pieds**, 4 directions × 3 images (`CharacterArt.cs` → `Art/Characters/`), animé par `PlayerAnimator`.
    Style chibi détaillé : gabarits faits main en abrégé (`Player/CharacterTemplates`, « 6.10S », moitiés en miroir pour face et dos), rendus par `CharacterPainter` : relief calculé par pièce, base,
    ombre, lumière par rôle, contour sombre teinté, ombre au sol), aussi utilisés en jeu pour l'apparence choisie.
    Le marchand utilise le même style (`CharacterPainter.Merchant`, via `CharacterArt.Merchant` et `ShopArt.Merchant`) ;
  - poissons de profil, tête à droite, un par espèce (`FishArt.cs` → `Art/Fish/`), assignés au champ
    `_sprite` de chaque `FishSpecies`. Dessins **détaillés** (≈ 96 px de long) via le moteur paramétrable
    `DetailedFish` (silhouette, dégradé, nageoires, queue, tête, motifs) : profils lissés (Catmull-Rom), corps
    éclairé comme un volume rond (paliers nets, ombres froides, lumières chaudes, reflet), écailles en arcs,
    nageoires à rayons inclinés, pectorales translucides, contour sélectif teinté. Fiches des espèces de l'île dans
    `MahanaFishArt.cs`, Annecy dans `AnnecyFishArt.cs`, brochet dans `PikeArt.cs`, deuxième fournée dans `ExtraFishArt.cs`. Les autres espèces gardent le dessin simple 48×24.
  - Nouvelle espèce = une ligne dans `Data/Especes.csv` (voir Table des espèces) ; pour un dessin plus fidèle, une fiche
    `DetailedFish` d'après l'anatomie réelle du poisson (dessin original, ne jamais recopier une illustration existante).
- Pour remplacer un dessin par un vrai, utiliser un autre nom de fichier, sinon il sera écrasé.
- Le poisson **frétille** sur la carte de prise : `UI/WigglingImage` (image découpée en tranches qui ondulent, tête
  stable, queue qui bat, décalages au pixel du dessin) et `UI/WiggleMath` (testé : vague, sursaut toutes les 2,6 s avec
  petit saut et bascule). Crustacés, reptiles et amphibiens ne font que se balancer.
- **Nouveau record** (`CatchLog.WouldBeRecord`, testé ; `FishingController.LastBeatenRecord`) : une prise plus grande
  que le record de l'espèce (pas la toute première) affiche « Nouveau record ! », la taille gagnée en doré (« 70 cm (+8) »),
  l'ancien record, un tampon doré « NOUVEAU RECORD ! » et des confettis dorés (sans flash ni photo).
- **Éclat sur les écailles** (`UI/ShineSweep`, calculs `UI/ShineMath` testés) : sur les espèces rares, épiques et
  légendaires déjà découvertes, une bande de lumière translucide en biais traverse le dessin (le dessin sert de `Mask`),
  toutes les 3 / 2,2 / 1,6 s ; étincelles dorées sur les légendaires. Carte de prise, fiche et cases du carnet.
- **Bouteille à la mer** (`Fishing/Core/BottleMessage`, testé ; `FishingController.FindBottle`, `_bottleChance` = 5 %) :
  une prise réussie sur 20 est une bouteille. 10 à 40 pièces, et un message signé : 2 fois sur 3 un indice sur une
  espèce de la zone pas encore pêchée (rareté, heure, taille max, appât préféré, sans la nommer), sinon une anecdote.
  Carte `Fishing/BottleCard` (bouteille de verre qui flotte, message sur papier), à la place de la carte de prise.
- **Photo de prise** (nouvelle espèce seulement, `CatchCard.AnimateCelebration`) : flash d'appareil photo, bord blanc de
  photo souvenir qui se redresse, tampon rouge « AJOUTÉE AU CARNET ! » qui s'abat en biais, 70 confettis qui tombent
  en tournant (dont la couleur de la rareté).
- Carte de prise : `CatchCard` (dessin du poisson, nom, nom scientifique, taille, rareté en couleur). Le dessin est
  **à l'échelle de la vraie taille de la prise** (`FishDisplayScale`, testé : échelle logarithmique de 8 cm = 32 % à
  150 cm = toute la largeur, proportions gardées, hauteur du cadre jamais dépassée). Même échelle dans l'index (cases et fiche),
  d'après la taille maximale de l'espèce.
- **Charte d'interface** (`UiSkin`, asset `Data/UiSkin.asset`, sprites de `UiSkinArt.cs` générés par `UiSkinSetup`) :
  cadre doré à coins arrondis, case creusée, bandeau de titre, fond d'eau, pierre de rareté, halo. Sprites
  étirables en 9 parties, 25 px/unité (1 pixel = 4 unités d'interface). Construire l'interface avec `UiFactory`
  (`Sliced`, `Label` avec ombre portée, `TitlePlaque`) ; ne plus utiliser de rectangles plats + `Outline`.
  Utilisée par la carte de prise, l'inventaire et le menu du marchand.
- **Police** : Jersey 10 (`Art/Fonts/Jersey10-Regular.ttf`, licence SIL OFL dans `Jersey10-OFL.txt`, à garder),
  champ `_font` de `UiSkin`, rendu *Smooth*. Les tailles passent par `UiFactory.Size` (échelle `_fontScale` = 1,35) ;
  mettre le gras avec `UiFactory.Bold` (pas de faux gras sur une police pixel, il rend les chiffres illisibles).
  Pixelify Sans a été essayée et rejetée (illisible).
- Tests : les espèces de test (`TestFish`) sont en `HideAndDontSave`, sinon Unity les décharge entre deux lancements.

## Table des espèces (le cœur du jeu : il y en aura des centaines)

- **Toutes les espèces sont dans `Assets/_Project/Data/Especes.csv`** : une ligne par espèce, séparateur `;`, s'ouvre
  dans un tableur (Excel, LibreOffice). Lue par `Collection/SpeciesTable` (testé) → `SpeciesRow`, puis le menu de
  génération crée les `FishSpecies`, remplit les zones (colonne Zone = nom de l'asset de zone : IleMahana, LacAnnecy,
  FleuveAmazone, BaieJaponaise) et les appâts favoris (colonne Appats : mie, ver, crevette, leurre, lumineux, calmar).
  Erreurs de la table (valeur inconnue, nombre mal écrit) affichées dans la console, la ligne est sautée.
- Valeurs en français, sans souci des accents : Rarete (commune, peu commune, rare, épique, légendaire), Nature
  (poisson, crustacé, reptile, amphibien), Forme (ombre sous l'eau : poisson, crabe, langouste, tortue, grenouille),
  Combat (calme, nerveux, fuyant, rush, lourd), Heure (toujours, jour, nuit). Nombres avec virgule ou point.
  Vitesse, LargeurZone, Reussites, Rates, Prix, Frequence vides = valeurs par défaut de la rareté (`SpeciesTable.Default…`).
- **Dessin** : une fiche dessinée à la main dans le code (`MahanaFishArt`, `AnnecyFishArt`, `AmazonFishArt`, `JapanFishArt`,
  `ExtraFishArt`, `PikeArt`, `CreatureArt`…) reste prioritaire. Sinon, colonne Dessin = silhouette (`FishArchetypes` :
  fuseau, haut, allonge, serpent, rond, disque, poisson-chat) + couleurs `#RRVVBB` (Dos, Flancs, Ventre, Nageoires, Oeil),
  Motif (bandes, points, lignes, marbre, ocelle) + CouleurMotif, Queue (fourchue, croissant, droite, ronde), Extras
  (barbillons, dents, epines, pinnules, adipeuse). Les non-poissons ont besoin d'une fiche à la main.
- **Nouvelle espèce = une ligne dans la table**, puis relancer le menu de génération. Exemples faits uniquement par la
  table : poisson-ballon à taches blanches, vairon, aimara, murène japonaise.

## Conventions de code

- C#, un fichier par classe, `PascalCase` pour les types et méthodes, `_camelCase` pour les champs privés.
- Champs exposés à l'inspecteur : `[SerializeField] private`, pas de champs publics.
- Namespace racine : `FishAndChill` (sous-namespaces par domaine : `FishAndChill.Fishing`, `FishAndChill.Collection`…).
- Scripts dans `Assets/_Project/Scripts/<Domaine>/`, données dans `Assets/_Project/Data/`.
- Données de jeu (espèces, zones, équipements) en **ScriptableObjects** : jamais codées en dur,
  car il y aura des centaines d'espèces.
- Logique testable (calculs de QTE, probabilités, économie) en C# pur, séparée des MonoBehaviours,
  avec des tests EditMode (Unity Test Framework).

## Git

- Toujours commiter les `.meta` avec leurs assets.
- Petits commits au message clair en français.
- Commiter quand une étape compile et fonctionne, et **pousser sur GitHub juste après chaque commit**
  (sans redemander).
- Dépôt public : aucune clé, aucun identifiant dans le projet.

## Collaboration

- Le propriétaire du projet parle français : répondre en français.
