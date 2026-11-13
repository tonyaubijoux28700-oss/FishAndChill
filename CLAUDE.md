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
- `AquariumStation` (bâtiment « Aquarium », placé par `MapSetup.FindAquariumSite`) : obstacle sur la carte,
  touche **E** (lettre, selon la disposition du clavier) devant la porte → `Deposit` : nouvelles espèces dans
  l'aquarium, doublons vendus au `BasePrice`. Repère orange sur la minimap.
- `UI/InventoryHud` : sac en bas à gauche, pièces en haut à droite, invite de dépôt, résumé du dépôt.
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
  - pêcheur 16×24, pivot **aux pieds**, 4 directions × 3 images (`CharacterArt.cs` → `Art/Characters/`), animé par `PlayerAnimator` ;
  - poissons de profil, tête à droite, un par espèce (`FishArt.cs` → `Art/Fish/`), assignés au champ
    `_sprite` de chaque `FishSpecies`. Dessins **détaillés** (≈ 96 px de long) via le moteur paramétrable
    `DetailedFish` (silhouette, dégradé, nageoires, queue, tête, motifs) : fiches des espèces de l'île dans
    `MahanaFishArt.cs`, brochet dans `PikeArt.cs`. Les autres espèces gardent le dessin simple 48×24.
  - Nouvelle espèce = ajouter une fiche `DetailedFish` d'après l'anatomie réelle du poisson (dessin original,
    ne jamais recopier une illustration existante).
- Pour remplacer un dessin par un vrai, utiliser un autre nom de fichier, sinon il sera écrasé.
- Carte de prise : `CatchCard` (dessin du poisson, nom, nom scientifique, taille, rareté en couleur).

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
