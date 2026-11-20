# FishAndChill

Jeu PC de collection de poissons par la pêche : le joueur pêche (QTE à jauge de timing), remplit un aquarium géant
avec toutes les espèces, revend les doublons pour améliorer son matériel et voyage dans des zones réelles.
Conception : [docs/GAME_DESIGN.md](docs/GAME_DESIGN.md).

## Où trouver le détail (à lire seulement pour le domaine concerné)

- [docs/systemes/peche.md](docs/systemes/peche.md) : déroulé de la pêche, QTE et styles de combat, coins de pêche,
  touche rare, silhouettes sous l'eau.
- [docs/systemes/carte-et-zones.md](docs/systemes/carte-et-zones.md) : île et générateurs, les 6 zones (régions),
  voyage et ponton, carte du monde, aménagement (`TownLayout`).
- [docs/systemes/batiments-et-progression.md](docs/systemes/batiments-et-progression.md) : sac, maison, phare,
  boutique et marchand, animalerie et animaux de compagnie, habitants et dialogues, matériel et appâts, aquarium, inventaire, carnet, sauvegarde, écran titre, création du
  pêcheur, menu pause.
- [docs/systemes/legendes.md](docs/systemes/legendes.md) : laboratoire, fragments, atelier des artefacts, réveil,
  remous, combat de légende (Tuna), style des dessins de boss.
- [docs/systemes/jour-nuit-et-minimap.md](docs/systemes/jour-nuit-et-minimap.md) : cycle jour/nuit, lumières, météo, minimap.
- [docs/systemes/art-et-especes.md](docs/systemes/art-et-especes.md) : art généré par code, style en volume, table
  des espèces (`Data/Especes.csv`).

**Quand un système change, mettre à jour son fichier dans `docs/systemes/`** (pas ce fichier, sauf pour une règle
générale).

## Projet technique

- Unity 6 (6000.6.9f1), 2D, URP (Renderer2D), nouveau Input System. PC (clavier/souris + manette). Vue de dessus, pixel art.
- Dépôt : https://github.com/tonyaubijoux28700-oss/FishAndChill (public), branche `main`.
- Scène unique : `Assets/_Project/Scenes/FishingPrototype.unity` (le bouton Play la lance toujours).
- Données et scène régénérées par le menu **FishAndChill → Prototype → Générer données et scène de pêche**
  (`PrototypeSetup` → `MapSetup`, plusieurs minutes). Tout le pixel art est généré par code et **redessiné à chaque
  génération** : pour garder un dessin fait main, lui donner un autre nom de fichier.
- **Attention** : la génération écrase les retouches faites à la main sur les Tilemaps.

## Piloter Unity

- Unity CLI : `unity command <groupe> <commande> --project-path .` (serveur MCP `unity-editor` dans `.mcp.json`).
  Recompiler : `script recompile` puis `script recompile_status` ; console : `console get --level warn`.
- Lancer du code dans l'éditeur : `script eval --file <fichier.cs>` (passer par un fichier : PowerShell casse les
  guillemets d'un `--code` en ligne). Attendre la fin d'un rechargement de scripts avant `editor.playmode start`.
- Ne pas modifier à la main un `.unity` / `.prefab` ouvert dans l'éditeur.
- Tests : `unity command test run --mode EditMode --filter FishAndChill --filter-type assembly --project-path .`
  Jamais pendant que l'éditeur entre ou sort du Play mode.
- **Tester en jeu sans toucher aux parties du propriétaire** : `TitleScreen.Play(2)` (emplacement 3), puis effacer
  `%USERPROFILE%/AppData/LocalLow/FishAndChill/FishAndChill/sauvegarde3.json` après l'essai. Outils de test :
  `FishingController.PressAction()` / `Tick()` / `DebugFindFragment()` / `DebugWinLegendPhase()`, menu
  **FishAndChill → Légendes (test)**.

## Règles à respecter

- **Police** Jersey 10 : ni œ, ni ā, ni flèches ↑ ↓ ▼ (écrire « Haut / Bas ») ; les tailles passent par
  `UiFactory.Size`, le gras par `UiFactory.Bold`.
- Interface : construire avec la charte (`UiSkin`, `UiFactory.Sliced` / `Label` / `TitlePlaque`), pas de rectangles plats.
- Nouvelle espèce = une ligne dans `Data/Especes.csv` puis relancer la génération. Nouvelle zone = une région dans
  `MapSetup`, une destination dans `PrototypeSetup`, et la zone dans les `_zones` de l'index et du `SaveManager`.
- Style de dessin du jeu : « en volume » (`Editor/VolumeCanvas`, 32 px par case). Les boss sont de vrais monstres
  marins, très détaillés et sombres (`Editor/MorayArt`, `LagoonDragonArt`). Dessins toujours originaux.
- Sons et ordre de déblocage des zones : reportés à plus tard, ne pas les proposer.

## Conventions de code

- C#, un fichier par classe, `PascalCase` pour les types et méthodes, `_camelCase` pour les champs privés.
- Champs exposés à l'inspecteur : `[SerializeField] private`, pas de champs publics.
- Namespace racine `FishAndChill` (sous-namespaces par domaine), scripts dans `Assets/_Project/Scripts/<Domaine>/`,
  données dans `Assets/_Project/Data/`.
- Données de jeu (espèces, zones, équipements, légendes) en **ScriptableObjects**, jamais codées en dur.
- Logique testable (QTE, probabilités, économie) en C# pur, séparée des MonoBehaviours, avec tests EditMode.

## Git

- Toujours commiter les `.meta` avec leurs assets. Petits commits au message clair en français.
- Commiter quand une étape compile et fonctionne, et **pousser sur GitHub juste après chaque commit** (sans redemander).
- Dépôt public : aucune clé, aucun identifiant dans le projet.

## Collaboration

- Le propriétaire du projet parle français : répondre en français.
- Pour tout ce qui est visuel, montrer un aperçu (planche d'images) avant de régénérer la scène.
