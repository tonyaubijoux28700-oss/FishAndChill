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
- Zone de départ : **Île Mahana** (fictive, tropicale) avec 11 vraies espèces indo-pacifiques : 8 poissons, 2 crustacés
  (crabe de palétuvier, langouste porcelaine) et 1 reptile (tortue verte).
  Palmiers sur la Tilemap `Décor` (tronc bloquant).
- **Plusieurs zones sur la même grille** : chaque zone est une **région** de `WorldMap` (`SetRegions`, `CurrentRegion`) ;
  `WorldBounds` (caméra, minimap) ne donne que la région actuelle. **Lac d'Annecy** : `LakeGenerator` (île retournée :
  eau au centre, galets, prés ; testé), 80×60 cases, à `LakeGap` = 100 cases à l'est de l'île (au-delà des intérieurs),
  construit par `MapSetup.BuildLake` dans les mêmes Tilemaps (tuiles de l'île recolorées par `LakeArt` : eau alpine,
  galets, prés ; sapins). 8 espèces : 6 poissons (`AnnecyFishArt` + `PikeArt`), écrevisse signal et grenouille rieuse
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
- **Voyage** (`World/ZoneTravel`, `UI/TravelMenu`, objet « Partie ») : un **ponton** par zone (`MapSetup.BuildDock`,
  2 cases × 6, praticable au-dessus de l'eau via `WorldMap._staticWalkable`, barque amarrée). Sur le ponton, **E** ouvre
  la **carte du monde** (←/→, Espace part, Échap ferme ; le sac ne s'ouvre pas sur le ponton) : fondu au noir, arrivée
  au bout du ponton, zone de pêche (`FishingController.SetZone`), région, minimap (`SetTitle`/`Rebuild`), grand bandeau
  du nom de la zone. Position sur la carte du monde et pays dans `FishingZone` (`_mapPosition`, `_region`).
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
- **Marchand** (`UI/ShopMenu`) : E devant le comptoir ouvre le menu (joueur `Locked`), deux onglets (**Tab** ou ←/→) :
  « Vendre » (1, 2, 3… vend un poisson, V vend tout) et « Matériel » (1 à 4 achète le niveau suivant). Échap/E ferme. Règles dans `FishShop` (testées) : il n'achète que les doublons (espèce déjà dans
  l'aquarium, ou exemplaires en trop : il laisse le plus gros), prix de 80 % à 130 % du `BasePrice` selon la taille.
- **Matériel** (`Scripts/Gear`, données dans `Data/Gear/`, générées par `GearSetup`) : 4 familles (`GearLine`,
  ScriptableObjects) réunies dans `GearCatalog`, chacune avec un niveau 0 gratuit puis 3 améliorations :
  canne (zone de réussite +15/30/45 %, curseur plus lent), hameçon (+1/2/3 ratés tolérés), appât (espèces rares plus
  fréquentes, touches plus rapides), sac (4/5/6 places). Prix 45 à 480 pièces. Niveaux du joueur dans
  `PlayerProgress.Gear` (`GearLoadout`), achat par `PlayerProgress.BuyGear` / `GearShop` (testés). Effets calculés par
  `GearEffects` (testé) et appliqués par `FishingController` (QTE, tirage de l'espèce, attente de la touche).
- **Bâtiments à intérieur** : base commune `InteriorBuilding` (entrée par la porte, pièce hors de l'île, caméra
  calée, `InteriorBuilding.Occupied` = bâtiment où se trouve le joueur ; pas de pêche dedans). `ShopBuilding` et
  `AquariumBuilding` en héritent. Sites choisis par `MapSetup.FindBuildingSite` (l'aquarium au centre, la boutique à ≥ 14 cases).
- **Aquarium** (`AquariumBuilding`, objet « Aquarium ») : un des plus grands bâtiments du jeu. Façade 12×10 cases
  (`AquariumArt`, 192×160 px, entre la 1re version 6×5 et la version 18×15 jugée trop grande : perron, colonnes,
  4 vitrines, rosace, dôme de verre plein d'eau, lanterne), emprise 11,2×5,2, site cherché autour du centre de l'île
  avec 9 cases de marge, la boutique à ≥ 14 cases. Grande salle 30×18 cases, plus
  grande que l'écran (la caméra suit le joueur) : bassin géant sur tout le mur du fond, allée en mosaïque, médaillon,
  colonnes à méduses et bancs (obstacles, positions dans `AquariumBuilding.JellyfishColumns` / `Benches`). Devant le bassin, **E** dépose le sac (`AquariumCollection.Deposit`, testé) : le plus
  gros de chaque nouvelle espèce entre ; un poisson plus gros remplace l'exemplaire exposé (l'ancien revient dans le
  sac) ; les doublons restent dans le sac pour le marchand. `FishShop` ne vend pas un record de taille.
  Les poissons nagent dans le bassin (`AquariumTank`) avec leur petite version `FishSpecies.MiniSprite`
  (≈ 32 px, réduite au tiers du grand dessin par `FishArt.GenerateMini` → `Art/Fish/Mini/`), entre le fond (ordre -5) et
  la vitre (`Aquarium_Vitre.png`, ordre -3). L'index des poissons indique les espèces exposées.
- `UI/InventoryHud` : **icône de sac** en bas à gauche (compteur « 2/3 », `UiArt.BagIcon`), **E** ouvre la fenêtre
  d'inventaire (joueur `Locked`, E/Échap ferme) ; devant le comptoir, E parle au marchand à la place. Quand le sac
  est plein après une prise, la fenêtre s'ouvre seule (places 1/2/3 qui clignotent). Pièces en haut à droite.
  Icônes de la boutique et de l'aquarium sur la minimap (`Minimap._landmarks`).
- **Index des poissons** (`UI/FishIndex`, objet « Partie ») : icône de livre à droite du sac (compteur « 2/8 »,
  `UiArt.BookIcon`), **Tab** (ou Y manette, `IndexInput`) ouvre le livre (joueur `Locked`), Tab/Échap ferme.
  Grille 4×2 par page de toutes les espèces des zones du champ `_zones` (Mahana et Annecy) : espèces
  découvertes dessinées, les autres en silhouette « ??? ». Flèches/ZQSD choisissent, fiche en bas (rareté, prises,
  record, taille). « Découverte » = déjà pêchée une fois (`CatchLog`, qui garde aussi le record de taille).
  Logique dans `SpeciesIndex` (testée).
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
  Par script : `TitleScreen.Play(slot)` (sinon le prototype reste sur l'écran titre). Nouvelle zone = l'ajouter au champ `_zones` du `SaveManager`, sinon ses prises ne se rechargent pas.
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
  **Nuit vivante** (`AmbientLife`, dessins `NightLifeArt`, dans les 2 zones) : 12 lucioles qui dérivent autour du joueur et
  clignotent (petite `Light2D` verte chacune), et le **reflet de la lune** sur l'eau (ordre -17, donc seulement sur
  l'eau) qui suit la caméra : posé sur l'eau libre visible la plus proche de `_moonOffset`, il glisse doucement.
  Les deux utilisent `_glowMaterial` = *Sprite-Unlit-Default* d'URP (brillent dans le noir).
  Assembly `FishAndChill` : références URP (`Universal.2D.Runtime` pour `Light2D`).
- **Espèces de jour / de nuit** : `FishSpecies.ActiveTime` (`Always`, `Day` 6 h-20 h, `Night` 19 h-7 h), réglé par
  `PrototypeSetup.ActiveTimes` d'après le vrai comportement (perroquet, chirurgien, sergent-major, brochet, perche le
  jour ; mérou paon, crabe, langouste, écrevisse la nuit). Le tirage ignore les espèces inactives
  (`FishingController`), l'index affiche « Mord le jour / la nuit ». Calculs dans `DayCycleMath` (testés).

## Minimap

- Petite carte **toujours visible en bas à droite** ; **M** ouvre la grande carte au centre (la petite se masque).
- `UI/Minimap` (objet « Minimap » de la scène) : touche **M** lue comme *lettre* selon la disposition du clavier
  (`FindKeyOnCurrentKeyboardLayout("m")`, donc juste en AZERTY), bouton Select de la manette, Échap pour fermer.
- Image construite au démarrage depuis les Tilemaps (1 pixel = 1 case, couleurs par tuile dans `_palette`) ;
  point rouge clignotant à la position du joueur (`MinimapMath`). Le jeu continue carte ouverte.
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
  - pêcheur 16×24, pivot **aux pieds**, 4 directions × 3 images (`CharacterArt.cs` → `Art/Characters/`), animé par `PlayerAnimator`.
    Même style que les poissons : formes rondes éclairées par paliers (`DetailedFish.Shade`), contour sélectif
    partagé (`PixelArt.SelectiveOutline`), ombre douce au sol. Le marchand est dessiné de la même façon
    (`CharacterArt.Merchant`, appelé par `ShopArt.Merchant`). Nouveau personnage = même `Canvas` de `CharacterArt` ;
  - poissons de profil, tête à droite, un par espèce (`FishArt.cs` → `Art/Fish/`), assignés au champ
    `_sprite` de chaque `FishSpecies`. Dessins **détaillés** (≈ 96 px de long) via le moteur paramétrable
    `DetailedFish` (silhouette, dégradé, nageoires, queue, tête, motifs) : profils lissés (Catmull-Rom), corps
    éclairé comme un volume rond (paliers nets, ombres froides, lumières chaudes, reflet), écailles en arcs,
    nageoires à rayons inclinés, pectorales translucides, contour sélectif teinté. Fiches des espèces de l'île dans
    `MahanaFishArt.cs`, brochet dans `PikeArt.cs`. Les autres espèces gardent le dessin simple 48×24.
  - Nouvelle espèce = ajouter une fiche `DetailedFish` d'après l'anatomie réelle du poisson (dessin original,
    ne jamais recopier une illustration existante).
- Pour remplacer un dessin par un vrai, utiliser un autre nom de fichier, sinon il sera écrasé.
- Carte de prise : `CatchCard` (dessin du poisson, nom, nom scientifique, taille, rareté en couleur).
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
