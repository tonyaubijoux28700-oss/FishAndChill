# Pêche

> Détail du projet, sorti de CLAUDE.md pour l'alléger. À lire quand on travaille sur ce domaine.


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
  `Fishing/Core/FishingSpotRules`, testées) : 3 coins à la fois, en eau libre, chacun face à une rive où l'on peut se tenir et qu'on rejoint à pied depuis le ponton (`FishingSpotRules.Reachable` via `World/ShoreReach`, jamais face à un îlot inaccessible)
  (2,2 à 4,2 cases) ; durée 70-140 s ou 2 prises (`Consume`), puis il réapparaît ailleurs avec une autre sorte.
  `School` (banc : ombres qui tournent sous l'eau, touche ×0,45 du temps, communes favorisées), `Bubbles` (bulles :
  tortue, grenouille ×4 ; les crustacés ne mordent plus à la canne : voir les casiers dans `batiments-et-progression.md`), `Frenzy` (chasse : éclaboussures, petits poissons qui sautent, rares à légendaires
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
- Le poisson sous l'eau : silhouette vue de dessus (`FishShadowSprites` : fuseau au museau pointu, pectorales en ailes,
  pelviennes, queue en croissant, dos plus sombre, bords et nageoires estompés, tout le corps qui ondule), dessinée
  d'avance en 16 directions × 3 images × 4 tailles (18/26/36 px, 72 px pour les légendes), jamais tournée. Chaque
  légende a sa forme (`LegendFish.ShadowShape`) : `BodyShape.Moray` pour Tuna (long corps qui ondule tout entier, tête
  massive à la gueule ouverte, yeux qui luisent), `BodyShape.Monster` en réserve (tête à crocs, épines, barbillons,
  queue déchirée). **La position suivie est la gueule** (`FishingPrototypeView.BodyCenter`) : le poisson attend juste devant
  l'appât, museau tourné vers lui, avance jusqu'à l'hameçon à chaque titillement ; il arrive de biais (une légende
  presque de côté). Pendant la pêche, la caméra glisse vers le bouchon (`CameraFollow.LookAhead`).
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

