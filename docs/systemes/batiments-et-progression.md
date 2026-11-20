# Bâtiments, progression, interfaces et sauvegarde

> Détail du projet, sorti de CLAUDE.md pour l'alléger. À lire quand on travaille sur ce domaine.


- `PlayerProgress` (objet « Partie ») : `Inventory` (sac, 3 places au départ, `UpgradeCapacity` pour plus tard),
  `AquariumCollection` (un exemplaire par espèce), `Wallet` (pièces). Logique pure testée.
- Chaque prise va dans le sac. Sac plein → état `FishingState.BagFull` : 1/2/3 relâche ce poisson du sac
  (`ChooseReplace`), Espace relâche le nouveau (`ChooseRelease`).
- **Maison du joueur** (`World/HouseBuilding`, objet « Maison », dessins `HouseArt` : enduit à colombages, toit de tuiles,
  volets bleus) : dans la prairie de l'ouest (`MapSetup.FindBuildingSite` autour du centre - 24 cases), cour pavée et
  chemin jusqu'au chemin principal (`TownLayout`, `DecorateHouse`). Intérieur 9×7 (lit, tapis, table, coffre, fenêtre
  sur la mer ; intérieur à `bounds.xMax + 180`). Devant le lit, **E** fait dormir jusqu'à 7 h, seulement de 18 h à 5 h
  (`SleepRules`, testé) : fondu au noir et message ; le sac ne s'ouvre pas devant le lit (`HouseBuilding.PlayerAtBed`).
- **Phare** (`World/Lighthouse`, objet « Phare », dessin `LighthouseArt`) : sur la pointe nord (`FindLighthouseSite`),
  pied solide. La nuit (`DayCycleMath.LampLevel`), lanterne allumée (`Light2D`) et double faisceau qui tourne
  (sprite créé en jeu, matériau *Sprite-Unlit-Default*). Maison et phare ont leur icône sur la minimap.
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
- **Habitants** (`Scripts/Villagers`, profils `VillagerProfile` dans `Data/Habitants/`, générés et posés par
  `Editor/VillagerSetup`, objets sous « Habitants ») : **Tavita** le vieux pêcheur (cabane du pêcheur, 7 h - 17 h 30, du
  côté du ponton), **Hinano** la maman et **Manu** son fils (maison pastel, 9 h - 18 h, parvis de l'aquarium ; Manu suit
  sa maman, `_leader`), **Mamie Rose** (chaumière, 10 h - 17 h, devant chez elle). Dessinés par le même peintre que le
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
- **Animalerie** (`World/PetShopBuilding`, objet « Animalerie », dessins `Editor/PetShopArt`, style en volume 32 px
  par case) : au sud-ouest de l'île, entre la maison et la plage, loin du laboratoire (`MapSetup`, site cherché depuis
  centre + (-16, -12)), petite cour pavée et chemin vers le chemin principal (`TownLayout.DecoratePetShop` : massifs,
  buisson, un lampadaire). Façade 160×144 px : bardage crème, toit de tuiles corail, enseigne ronde à la patte, vitrines
  avec un chiot et un chat roux, porte vert d'eau, paillasson à pattes. Intérieur 11×8 cases (hors de l'île, au-dessus
  de celui du laboratoire) : papier peint à pattes, parquet, rayon de croquettes, perroquet en cage, colliers et laisses,
  arbre à chat, clapier, panier du chien ; comptoir (`Animalerie_Comptoir.png`, chat endormi) et vendeur roux au tablier
  vert d'eau (`CharacterArt.PetKeeper`). Obstacles : `PetShopBuilding.Counter` / `Shelf` / `CatTree` / `Hutch` /
  `DogBed`. Icône sur la minimap. **Pas encore de vente** : les animaux de compagnie qui suivent le joueur viendront
  plus tard (emplacement « ANIMAL » déjà dans l'inventaire).
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
  **Visiteurs mis de côté** pour l'instant : `World/AquariumVisitors` et `MapSetup.BuildVisitors` (pêcheur recoloré,
  promenade devant les bassins, +1 toutes les 6 espèces) existent mais ne sont plus appelés. La faune de la zone
  (`AmbientLife`) se cache à l'intérieur des bâtiments. Obstacles : `AquariumBuilding.JellyfishColumns` / `Benches`. Devant le bassin, **E** dépose le sac (`AquariumCollection.Deposit`, testé) : le plus
  gros de chaque nouvelle espèce entre ; **la taille ne compte pas ensuite** : une espèce déjà exposée n'est jamais
  remplacée, même par un plus gros, et tous ses exemplaires restent dans le sac pour le marchand (`FishShop` les achète).
  Les poissons nagent dans le bassin (`AquariumTank`) avec leur petite version `FishSpecies.MiniSprite`
  (≈ 32 px, réduite au tiers du grand dessin par `FishArt.GenerateMini` → `Art/Fish/Mini/`), entre le fond (ordre -5) et
  la vitre (`Aquarium_Vitre.png`, ordre -3). L'index des poissons indique les espèces exposées.
- `UI/InventoryHud` (canvas ordre 11) : **icône de sac** en bas à gauche (compteur « 2/3 », `UiArt.BagIcon`), **E** ouvre la
  **grande fenêtre « Mon inventaire »** au centre (joueur `Locked`, E/Échap ferme) : à gauche la **vitrine « Mon matériel »** (fond de feutre :
  la **canne entière** en grand, `GearLevel.Illustration` = `GearArt.RodLarge` 96×160 affiché ×3, son fil qui finit au pixel
  (86, 62) où pendent l'**hameçon** et l'**appât** accroché, le sac en bas et, à côté, l'**emplacement « ANIMAL »**
  (`InventoryHud.BuildPetSlot`, empreinte `PetShopArt.PawIcon`, « Aucun » : vide en attendant les compagnons de
  l'animalerie) ; étiquettes nom, effet, niveau en pierres), à
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
  Le pêcheur est **dessiné en jeu** (`Player/CharacterPainter`, `CharacterSprites`, outils `PixelShading`) et appliqué par
  `SaveManager` (`PlayerAnimator.SetLook`) ; `CharacterArt` (éditeur) s'en sert pour les PNG par défaut. Le prénom
  remplace « Partie N » sur la carte de l'écran titre. Par script : `TitleScreen.OpenCreator(slot)` ;
  `TitleScreen.Play(slot)` garde le pêcheur par défaut.
- **Menu pause** (`UI/PauseMenu`, objet « Partie », canvas ordre 20) : **Échap** ou Start, seulement si aucun autre
  menu n'était ouvert à l'image précédente (Échap ferme d'abord sac, livre, marchand, grande carte). Fige le jeu
  (`Time.timeScale = 0`, `FishingController` et `Minimap` désactivés, joueur `Locked`), reprise une image plus tard.
  Lignes : Reprendre, Volume (←/→, `AudioListener.volume`), Plein écran, Retour au titre (sauvegarde puis
  `SaveManager.ReturnToTitle`), Quitter. La police n'a ni ↑ ↓ ni ▼ : écrire « Haut / Bas ». Options dans `PlayerPrefs`. Calculs dans
  `PauseMenuLogic` (testés), touches dans `PauseInput`.

