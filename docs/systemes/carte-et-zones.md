# Carte, zones et voyage

> Détail du projet, sorti de CLAUDE.md pour l'alléger. À lire quand on travaille sur ce domaine.


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
  `WorldBounds` (caméra, minimap) ne donne que la région actuelle. **Lac d'Annecy** : plan dessiné `World/LakePlan`
  (80×60 cases : montagnes enneigées au nord sur un plateau inaccessible, falaise, cascade animée et torrent franchi par
  une passerelle praticable, lac en trois bassins, presqu'île avec chalet savoyard, îlot et son sapin, rive de galets,
  chemin de terre qui fait le tour, prairie fleurie, forêts de sapins mêlés de hêtres et bouleaux), à `LakeGap` = 200 cases
  à l'est de l'île (au-delà des intérieurs), construit par `MapSetup.BuildLake` dans les mêmes Tilemaps (tuiles de l'île
  recolorées par `LakeArt` ; falaise, cascade, chemin, fleurs, feuillus dans `LakeTerrainArt` ; chalet, montagnes et
  passerelle dans `LakeLandmarkArt`). 13 espèces : 11 poissons (`AnnecyFishArt` + `PikeArt` + `ExtraFishArt`), écrevisse signal et grenouille rieuse
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
- **Fleuve Amazone** (3e zone, région 2) : plan dessiné `World/AmazonPlan` (90×64 cases, testé : fleuve qui serpente
  d'ouest en est, rives de vase, **banc de sable pâle** à l'ouest qui le traverse, igarapé au nord franchi par une
  **passerelle** praticable (`AmazonBridgeArea`), bras mort au sud-est couvert de nénuphars Victoria, forêt inondée à l'est,
  **village sur pilotis** autour du ponton avec pirogue et bananiers, **fromager géant** dans sa clairière au nord, chemin
  de terre rouge qui relie tout, jungle dense et mur d'arbres au bord ; plantes posées par le plan dans `Plants`), à
  `AmazonGap` = 20 cases à l'est du lac, construit par `MapSetup.BuildAmazon` (tuiles recolorées par `AmazonArt` : eau café
  au lait, vase, jungle, fougères, Victoria ; grands arbres en volume, arbres dans l'eau, açaí, bananier, héliconias,
  grandes feuilles, troncs couchés, terre rouge et sable dans `AmazonTerrainArt` ; maisons, fromager et pirogue dans
  `AmazonLandmarkArt`). Arbres, palmiers, maisons, fromager et troncs sont solides. `RiverGenerator` n'est plus utilisé. Vie (`BuildAmazonLife`, dessins `AmazonLifeArt`) : dauphins roses, caïmans, morphos, aras
  avec leur ombre. 14 espèces (table des espèces) : 12 poissons (`AmazonFishArt` : piranha, oscar, pléco,
  discus, tucunaré, tambaqui, arowana, surubim, payara, pirarara, gymnote électrique, arapaïma légendaire), matamata et
  crevette d'Amazonie (`AmazonCreatureArt`). Salle Amazonie à l'aquarium (voir Aquarium).
- **Baie d'Izu** (4e zone, région 3, Japon) : plan dessiné `World/BayPlan` (90×64 cases, testé : mer au sud qui s'enfonce
  en arrondi entre deux caps rocheux, plage de sable volcanique, forêt de pins au nord, ruisseau sorti de la forêt et
  franchi par un **pont rouge** praticable (`JapanBridgeArea`), **sanctuaire** shinto dans l'axe d'une allée de dalles avec
  torii et lanternes, le **mont Fuji** dressé juste derrière lui (13 cases sur 5, collines de pins à son pied, solide),
  **torii dans l'eau** dans le même axe, hameau de deux
  maisons de pêcheurs et une barque, **onsen** fumant, bambouseraie, érables autour du sanctuaire, cerisiers, pins noirs,
  rochers mariés dans la mer), à `JapanGap` = 20 cases à l'est du fleuve, `MapSetup.BuildJapan` (tuiles `JapanArt` : mer,
  sable volcanique, mousse et pétales ; arbres, lanternes `Lampadaire_ToroPierre` allumées la nuit, rochers, fleurs, dalles
  et terre dans `JapanTerrainArt` ; repères dans `JapanLandmarkArt`). Ponton au
  bout d'un petit chemin, vers le sud. `BayGenerator` n'est plus utilisé. Vie (`BuildJapanLife`, `JapanLifeArt`) : canards mandarins, pétales de cerisier, grues du Japon. 14 espèces
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
- **Atoll des Maldives** (6e zone, région 5, océan Indien) : plan dessiné `World/AtollPlan` (90×64 cases, testé : anneau
  de récif (platier, coraux à travers l'eau) autour d'un grand lagon turquoise, coupé de **3 passes** où entre l'océan
  profond ; pâtés de corail dans le lagon ; **île habitée** au nord (plage de sable blanc plus large côté lagon, cocoteraie,
  chemin de sable corallien bordé de **torches de bambou** allumées la nuit, deux **maisons en pierre de corail** chaulée
  à volets turquoise, l'une sous un toit de feuilles tressées, l'autre sous la tôle rouge, avec muret et joali, frangipaniers
  autour, **bar de plage** sous chaume au bout de la plage est) ; îlot sauvage à l'est, îlot au sud-ouest, langue de sable
  au sud, **banc de sable** et son unique cocotier au milieu du lagon ; **passerelle sur pilotis en T** qui part de la plage,
  praticable (`AtollJetty`, on y pêche), bordée de **6 villas sur l'eau** à piscine privée ; balançoire plantée dans le
  lagon, **dhoni** amarré), à `AtollGap` = 20 cases à l'est de la Grande Barrière, `MapSetup.BuildAtoll` (tuiles `AtollArt` :
  océan bleu profond, lagon, sable de corail, végétation ; cocotiers droits et penchés vers l'eau, frangipanier, veloutier,
  hibiscus, ipomée, coquillages et chemin dans `AtollTerrainArt` ; repères, passerelle et torches dans `AtollLandmarkArt`).
  Ponton des voyages au bout du chemin, vers le sud, dans le lagon. `AtollGenerator` n'est plus utilisé. Vie (`BuildAtollLife`) : raies et tortues sous l'eau, bernard-l'ermite, papillons,
  poissons volants, frégate. 14 espèces faites par la table (`Zone` = `Maldives` : chirurgien à poitrine blanche,
  poisson-ange empereur, idole des Maures, baliste titan, thon jaune, murène javanaise, requin-tigre légendaire…).
  **Salle Maldives** à l'aquarium (6e salle, `AquariumAtollArt`, `AtollTank`, mobilier `AquariumRoomLayout.Atoll`) :
  plancher de teck avec deux panneaux de verre sur le lagon, murs en rotin, bassin d'océan profond (pâté de corail,
  raie manta au loin, gorgones, anthias), modèle de dhoni, lanternes en rotin. L'intérieur de l'aquarium fait
  maintenant 160 cases : `LakeGap` passe à 220, et l'intérieur de la maison est au-dessus de celui de la boutique.
- **Voyage** (`World/ZoneTravel`, `UI/TravelMenu`, objet « Partie ») : un **ponton** par zone (`MapSetup.BuildDock`,
  2 cases × 6, praticable au-dessus de l'eau via `WorldMap._staticWalkable`). **Le même ponton dans toutes les zones**
  (`Editor/DockArt`, style en volume, `Art/Tiles/Ponton.png` et `Ponton_VersLeHaut.png` retourné pour les pontons qui
  partent vers le haut) : planches vernies, poteaux cerclés de corde, main courante de corde, panneau à la rose des
  vents, caisses et tonneau côté rive, bouée, rouleau de corde, taquets et réverbère au bout ; amarré à côté, le
  **voilier** des voyages (`DockArt.Boat`, `Voilier.png`), le même que sur la carte du monde. Sur le ponton, **E** ouvre
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
- **Aménagement** (`TownLayout`, appelé par `MapSetup`) : l'aquarium est au **milieu de l'île** (`LandCentroid`, porte une case sous le centre),
  la boutique près du départ. Aménagement **ordonné** : parvis symétrique devant l'aquarium + allée centrale de 3 cases
  dans l'axe de la porte (lampadaires en paires), chemins **droits à angles droits** de 3 cases (`BestZPath` : un seul
  virage choisi pour éviter l'eau, les bâtiments et le sable) : plage → bout de l'allée, boutique → chemin principal
  en T. Décor de l'aquarium posé en miroir (`Mirror`, variantes imposées), lampadaires espacés (deux aux coins avant du parvis, paires toutes les 7 cases dans l'allée, un toutes les 12 cases le long des chemins, `LampSpacing`) à 4 cases minimum
  les uns des autres. Pavés sur la couche `Rendu - Pavés` (double grille, bords arrondis). Décor sur `Décor`
  (`DecorArt`, `Art/Tiles/Decor/`) : haies au pied des façades, massifs, lampadaires, tonneaux et
  caisses de la boutique, panneau indicateur, buissons et rochers. Les cocotiers évitent tout ça (`Result.Reserved`).
  **Décor et troncs de cocotiers sont solides** : leur pied (`TownLayout.Footprint` / `PalmFootprint`) est enregistré
  dans `WorldMap._staticObstacles` à la génération ; `CanStandAt` teste la boîte du joueur contre chaque obstacle.
- **Maisons des habitants** (`World/VillagerHouseBuilding`, façades `Editor/VillageArt` 128×128 px, intérieurs
  `Editor/HomeArt` 288×224 px = 9×7 cases, style en volume) : **trois**, éparpillées aux coins libres de l'île par
  `MapSetup.FindVillagerSite` (départs nord-ouest, nord-est, sud-est ; 14 cases au moins de l'aquarium, 12 des autres
  bâtiments, 22 entre elles), posées par `MapSetup.BuildVillagerHouse` (modèles `VillagerLooks` = 0, 1, 3). On y entre
  comme dans les autres bâtiments ; pièces hors de l'île, espacées de 14 cases (la caméra voit plus haut qu'une pièce de
  7 cases). Meubles peints dans le décor, pieds infranchissables (`HomeArt.Obstacles` → `_furniture`), habitants
  debout chez eux la nuit (`HomeArt.Residents`, objets « Habitant id ») :
  - cabane du pêcheur (planches bleues, toit de palmes) : poêle, poisson empaillé, filet, bouée, tonneaux, table au
    poisson grillé ; **le vieux pêcheur** (ciré jaune, bonnet à pompon, barbe grise) ;
  - maison pastel (enduit pêche, tuiles) : canapé, bibliothèque, table fleurie, jouets ; **la maman** (robe rouge à
    fleurs, hibiscus) et **l'enfant** (t-shirt rayé) ;
  - chaumière de pierre : cheminée et chaudron, fauteuil à bascule, petit lit à courtepointe, chat endormi ; **la
    grand-mère** (chignon blanc, lunettes, gilet violet).
  Leur vie (balade, retour le soir, dialogues) : voir « Habitants » dans `batiments-et-progression.md`. `VillageArt` a aussi la maison jaune à véranda (modèle 2) et une maison pastel
  lavande (`tint` 1), pas encore posées. Chaque maison a sa petite cour pavée, un chemin vers le pavé le plus proche
  (`TownLayout.PavePaths`) et un buisson à côté (`DecorateVillagers`).

