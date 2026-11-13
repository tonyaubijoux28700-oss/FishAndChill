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
