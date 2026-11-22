# Art (généré par code) et table des espèces

> Détail du projet, sorti de CLAUDE.md pour l'alléger. À lire quand on travaille sur ce domaine.

## Art provisoire


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


## Table des espèces


- **Toutes les espèces sont dans `Assets/_Project/Data/Especes.csv`** : une ligne par espèce, séparateur `;`, s'ouvre
  dans un tableur (Excel, LibreOffice). Lue par `Collection/SpeciesTable` (testé) → `SpeciesRow`, puis le menu de
  génération crée les `FishSpecies`, remplit les zones (colonne Zone = nom de l'asset de zone : IleMahana, LacAnnecy,
  FleuveAmazone, BaieJaponaise, GrandeBarriere, Maldives, CoteDesSquelettes) et les appâts favoris (colonne Appats : mie, ver, crevette, leurre, lumineux, calmar).
  Erreurs de la table (valeur inconnue, nombre mal écrit) affichées dans la console, la ligne est sautée.
- Valeurs en français, sans souci des accents : Rarete (commune, peu commune, rare, épique, légendaire), Nature
  (poisson, crustacé, reptile, amphibien), Forme (ombre sous l'eau : poisson, crabe, langouste, tortue, grenouille),
  Combat (calme, nerveux, fuyant, rush, lourd), Heure (toujours, jour, nuit), Meteo (vide = par tous les temps, pluie, orage, brouillard, beau ; « pluie » mord aussi
  pendant l'orage). Nombres avec virgule ou point.
  Vitesse, LargeurZone, Reussites, Rates, Prix, Frequence vides = valeurs par défaut de la rareté (`SpeciesTable.Default…`).
- **Dessin** : une fiche dessinée à la main dans le code (`MahanaFishArt`, `AnnecyFishArt`, `AmazonFishArt`, `JapanFishArt`,
  `ExtraFishArt`, `PikeArt`, `CreatureArt`…) reste prioritaire. Sinon, colonne Dessin = silhouette (`FishArchetypes` :
  fuseau, haut, allonge, serpent, rond, disque, poisson-chat) + couleurs `#RRVVBB` (Dos, Flancs, Ventre, Nageoires, Oeil),
  Motif (bandes, points, lignes, marbre, ocelle) + CouleurMotif, Queue (fourchue, croissant, droite, ronde), Extras
  (barbillons, dents, epines, pinnules, adipeuse). Les non-poissons ont besoin d'une fiche à la main.
- **Raies** : dessinées à la main, vues de dessus, tête à droite (`Editor/RayArt`, branchées dans `CreatureArt.TryGet`).
  Première : la raie manta de récif (`mobula-alfredi`, Maldives). Elles gardent l'ombre de poisson sous l'eau.
- **Nouvelle espèce = une ligne dans la table**, puis relancer le menu de génération. Exemples faits uniquement par la
  table : poisson-ballon à taches blanches, vairon, aimara, murène japonaise.


## Style « en volume »


- Le style choisi pour tout le jeu est celui du **médaillon au poisson** de l'aquarium et des poissons (`DetailedFish`) :
  formes en courbes lissées, chaque pièce éclairée comme un volume rond par paliers (ombres froides, lumières chaudes,
  reflet), matière (tissage, écorce, planches, chaume), ombre portée sous ce qui passe devant, contour sélectif teinté.
- Moteur : `Editor/VolumeCanvas` (`Piece(forme, couleur, bombé, texture)`, formes `Ellipse`/`Capsule`/`Box`/`Union`,
  `Finish`), à **32 px par case** (`VolumeCanvas.PixelsPerUnit`) : deux fois plus fin, même taille à l'écran.
- Déjà passés dans ce style : cocotiers, rochers et façade de la boutique (`Editor/VolumeIslandArt`), intérieur de la
  maison (`Editor/VolumeHouseArt`, même disposition que les obstacles de `HouseBuilding`, rideaux, lambris, lampe, livres).
  À faire ensuite : pêcheur et marchand (avec toutes les options de personnalisation), maison, phare, aquarium,
  décor (buissons, haies, lampadaires…), puis sol et côtes, puis les autres zones.
