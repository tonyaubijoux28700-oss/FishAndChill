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
- Zone de départ : **Île Mahana** (fictive, tropicale) avec 8 vraies espèces indo-pacifiques.
  La zone « Lac d'Annecy » reste en données pour plus tard. Palmiers sur la Tilemap `Décor` (non bloquants).

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
- **Marchand** (`UI/ShopMenu`) : E devant le comptoir ouvre le menu (joueur `Locked`), 1/2/3 vend un poisson,
  V vend tout, Échap/E ferme. Règles dans `FishShop` (testées) : il n'achète que les doublons (espèce déjà dans
  l'aquarium, ou exemplaires en trop : il laisse le plus gros), prix de 80 % à 130 % du `BasePrice` selon la taille.
- **Bâtiments à intérieur** : base commune `InteriorBuilding` (entrée par la porte, pièce hors de l'île, caméra
  calée, `InteriorBuilding.Occupied` = bâtiment où se trouve le joueur ; pas de pêche dedans). `ShopBuilding` et
  `AquariumBuilding` en héritent. Sites choisis par `MapSetup.FindBuildingSite` (l'aquarium à ≥ 15 cases de la boutique).
- **Aquarium** (`AquariumBuilding`, objet « Aquarium ») : un des plus grands bâtiments du jeu. Façade 18×15 cases
  (`AquariumArt`, 288×240 px : perron, colonnes, vitrines, rosace, immense dôme de verre plein d'eau, lanterne),
  emprise 16,6×8, site choisi avec 12 cases de marge et à ≥ 18 cases de la boutique. Grande salle 30×18 cases, plus
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
  Grille 4×2 par page de toutes les espèces des zones du champ `_zones` (Mahana seule pour l'instant) : espèces
  découvertes dessinées, les autres en silhouette « ??? ». Flèches/ZQSD choisissent, fiche en bas (rareté, prises,
  record, taille). « Découverte » = déjà pêchée une fois (`CatchLog`, qui garde aussi le record de taille).
  Logique dans `SpeciesIndex` (testée).
- Le bouton Play lance toujours `FishingPrototype` (`PlayModeStartScene`), même si une autre scène est ouverte.
- **Ne pas lancer les tests pendant que l'éditeur entre ou sort du Play mode** : le lanceur de tests plante et
  laisse une scène vide ouverte.
- Pas encore de sauvegarde : tout repart de zéro à chaque lancement.

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
