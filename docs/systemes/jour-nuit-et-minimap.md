# Cycle jour/nuit, météo et minimap

> Détail du projet, sorti de CLAUDE.md pour l'alléger. À lire quand on travaille sur ce domaine.

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


## Météo


- **Le temps** (`World/WeatherSystem`, objet « Météo » sous « Partie ») : beau temps, nuageux, pluie, orage ou brouillard
  (`Weather`), tiré selon le **climat de la zone** (`FishingZone.Climate`, `WeatherClimate` : fréquence de chaque temps,
  réglée par `PrototypeSetup.SetClimate` d'après le vrai climat : orages des tropiques, forêt pluviale, brumes du lac
  alpin et de la côte japonaise). Il dure 3 à 7 heures de jeu puis change ; un voyage tire le temps de la nouvelle zone.
  Nouvelle partie au beau temps. Sauvegardé (`SaveData.Weather`, `WeatherHours` ; -1 = ancienne sauvegarde).
  Règles dans `WeatherMath` (testé) : tirage, durée, teinte du ciel, attente avant la touche (pluie ×0,82, orage ×0,75),
  `Allows` pour les espèces. Affiché dans l'en-tête (« Lac d'Annecy   8h10   Pluie »).
- **Tempête des boss** : voir docs/systemes/legendes.md (`LegendStorm`, intensité `WeatherSystem.Epic`).
- **Rendu** : la lumière globale prend la teinte de l'heure multipliée par celle du temps (`WeatherSystem.Tint`, fondu
  de quelques secondes) ; en orage, éclairs (double flash) toutes les 6-16 s. `World/WeatherEffects` autour de la
  caméra : gouttes en biais qui éclatent en petits ronds, nappes de brume tramées qui dérivent (dessins `Editor/WeatherArt`
  → `Art/Weather/`, 16 px par case, sprites éclairés donc assombris la nuit). Rien à l'intérieur ni sur l'écran titre.
- **Espèces et météo** : `FishSpecies.Weather` (`WeatherNeed`, colonne Meteo de la table) : truite fario, barramundi,
  tambaqui, anguille du Japon, grenouille rieuse sous la pluie ; gymnote électrique et requin-tigre pendant l'orage ;
  lotte et crabe-araignée géant dans le brouillard ; grand barracuda par beau temps. L'index l'affiche
  (« Le jour, sous la pluie »).


## Minimap


- Petite carte **toujours visible en bas à droite** ; **M** ouvre la grande carte au centre (la petite se masque).
- `UI/Minimap` (objet « Minimap » de la scène) : touche **M** lue comme *lettre* selon la disposition du clavier
  (`FindKeyOnCurrentKeyboardLayout("m")`, donc juste en AZERTY), bouton Select de la manette, Échap pour fermer.
- Image dessinée au démarrage et après chaque voyage (`Rebuild`) depuis les Tilemaps, à `_detail` = 4 pixels par case
  (couleurs par tuile dans `_palette`) : côtes arrondies (interpolation bilinéaire, `MinimapMath.Bilinear`) avec écume,
  eau plus claire près des rives (distance à la terre), grain du sol, chemins pavés (`_paving` = couche `Rendu - Pavés`,
  case pavée si ses 4 tuiles de double grille existent), arbres en petites couronnes ombrées.
- **Lagons** : les cases d'eau couvertes par la couche `Rendu - Lagon` (`_lagoon`, double grille) sont claires et teintées
  de turquoise (`_lagoonTint`), même loin des côtes (lagon de l'atoll, récif).
- **Carte vivante** : 3 images de vaguelettes qui s'enchaînent, ombres de nuages, coins de pêche qui pulsent (couleur
  selon la sorte, lus dans les `FishingSpots` de la région), flèche rouge dans le sens du regard (`MinimapMath.FacingAngle`)
  avec une onde (`MinimapMath.Pulse`), teinte de l'heure (`DayCycleMath.SkyColor`). Cadres dorés de la charte, nom de la
  zone sur les deux cartes, rose des vents et légende sur la grande. Le jeu continue carte ouverte.
- Le canvas du jeu (`FishingPrototypeView`, ordre 10) passe au-dessus de la minimap (ordre 5).

