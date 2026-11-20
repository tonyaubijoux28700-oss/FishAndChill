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
- **Atoll des Maldives** (6e zone, région 5, océan Indien) : `AtollGenerator` (anneau d'îlots autour d'un grand lagon :
  grand îlot continu au nord avec cocoteraie, petits îlots séparés par des passes, platier de corail au large `Reef` ;
  testé), 90×64 cases, à `AtollGap` = 20 cases à l'est de la Grande Barrière, `MapSetup.BuildAtoll` (tuiles `AtollArt` :
  océan bleu profond, tout le lagon turquoise clair, sable de corail blanc, végétation bien verte ; cocotiers en volume de
  l'île, coraux du récif ; **bungalows sur pilotis** en volume dans le lagon de part et d'autre du ponton). Ponton au
  sud du grand îlot, dans le lagon. Vie (`BuildAtollLife`) : raies et tortues sous l'eau, bernard-l'ermite, papillons,
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
- **Maisons des habitants** (`Editor/VillageArt`, style en volume, 128×128 px, 4 cases de large ; façades seules, on
  n'y entre pas encore) : **trois**, éparpillées aux coins libres de l'île par `MapSetup.FindVillagerSite` (départs
  nord-ouest, nord-est, sud-est ; 14 cases au moins de l'aquarium, 12 des autres bâtiments, 22 entre elles) : cabane du
  pêcheur (planches bleues, toit de palmes, filet, bouée), maison pastel (enduit pêche, tuiles, volets verts) et
  chaumière de pierre (chaume moussu, porte ronde, lanterne). `VillageArt` a aussi la maison jaune à véranda (modèle 2)
  et une maison pastel lavande (`tint` 1), pas encore posées. Objets « Maison d'habitant N » (`Art/Buildings/Hameau/`),
  pied solide 3,8×2,4. Chacune a sa petite cour pavée et un chemin vers le pavé le plus proche (`TownLayout.PavePaths`),
  un buisson ou un massif à côté (`DecorateVillagers`).

