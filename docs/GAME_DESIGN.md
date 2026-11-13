# FishAndChill — Document de conception

> Document vivant : il évolue avec le jeu. Les points marqués **[À décider]** sont ouverts.

## Pitch

Un jeu de pêche et de collection : le joueur parcourt des zones inspirées de lieux réels,
pêche grâce à des QTE de timing, et veut remplir un aquarium géant avec **toutes** les espèces
de poissons. Les doublons se revendent pour améliorer son matériel et accéder à des poissons
plus rares.

## Boucle de jeu

```
Choisir une zone → Pêcher (QTE) → Capture
      ↑                              │
      │                ┌─────────────┴─────────────┐
      │         Nouvelle espèce              Espèce déjà possédée
      │          → Aquarium                     → Vendre
      │                                            │
      └──── Nouvelles zones ← Améliorer le matériel ←┘
```

- **Court terme (une minute)** : lancer, attendre la touche, réussir le QTE.
- **Moyen terme (une session)** : vendre, acheter une amélioration, débloquer une zone.
- **Long terme** : compléter l'aquarium (et le voir se remplir).

## Pêche

- **Vue de dessus** : le joueur se tient sur une berge, un ponton ou un bateau ; on voit les ombres des poissons.
- Déroulé : viser et lancer → attendre (un poisson s'approche de l'appât) → touche → **QTE**.
- **QTE à jauge de timing** : un curseur oscille sur une barre ; il faut appuyer quand il passe dans
  la zone de réussite. Un poisson demande plusieurs réussites d'affilée.
- La difficulté dépend du poisson :
  - vitesse du curseur ;
  - taille de la zone de réussite ;
  - nombre de réussites demandées.
- Le matériel modifie le QTE (une meilleure canne élargit la zone de réussite, etc.).
- Échec : le poisson s'échappe. Pas de perte de matériel au départ **[À décider]**.

## Espèces

Chaque espèce est une donnée (ScriptableObject), pas du code :

| Champ | Exemple |
|---|---|
| Nom commun / nom scientifique | Truite arc-en-ciel / *Oncorhynchus mykiss* |
| Rareté | Commun, Peu commun, Rare, Épique, Légendaire |
| Zones et habitats | Rivière, lac (eau douce) |
| Conditions (optionnel) | heure, météo, saison **[À décider]** |
| Appâts préférés | ver, cuillère… |
| Difficulté du QTE | vitesse, taille de zone, réussites demandées |
| Taille / poids (plage) | 30–60 cm |
| Prix de revente | selon rareté et taille |
| Sprite (pixel art) | — |

**Échelle** : « toutes les espèces existantes » représente plus de 30 000 espèces réelles.
Les données doivent être faciles à ajouter en masse, par exemple par import d'un tableur.
**[À décider]** : le nombre d'espèces de la première version jouable (ex. 30 à 50).

## Zones

- Zones inspirées de lieux réels (ex. un lac alpin, la côte bretonne, l'Amazonie, un récif tropical).
- Chaque zone a sa propre liste d'espèces et ses taux d'apparition.
- Débloquées par la progression (argent, matériel minimum, ou les deux) **[À décider]**.
- **[À décider]** : comment voyager (carte du monde, menu…).

## Économie et matériel

- Les **doublons** se vendent ; le premier exemplaire de chaque espèce va dans l'aquarium.
- **[À décider]** : peut-on vendre un exemplaire plus grand pour améliorer son record ?
- Le matériel est amélioré par catégorie :
  - **Canne** : facilite le QTE (zone de réussite plus large, curseur plus lent).
  - **Hameçons** : réduisent les décrochages ; nécessaires pour les gros poissons.
  - **Appâts** : attirent certaines espèces, accélèrent les touches.
  - **Moulinet / fil** : **[À décider]**.

## Aquarium

- L'aquarium est le cœur de la récompense : on voit les espèces capturées nager.
- Il fait aussi office d'encyclopédie, avec les espèces manquantes en silhouette.
- Avec des centaines d'espèces, il faudra prévoir l'affichage : bassins par région ou par habitat, défilement, filtres **[À décider]**.

## Direction artistique

- Pixel art, vue de dessus, ambiance détendue (« chill »).
- Prototype avec des formes simples ; les sprites définitifs viendront ensuite.

## Feuille de route

1. **Prototype de pêche** : une zone, quelques espèces de test, lancer, touche, QTE et capture.
2. **Collection** : inventaire, aquarium minimal (liste et silhouettes), vente des doublons.
3. **Progression** : boutique, améliorations du matériel, effets sur le QTE.
4. **Zones** : 2 ou 3 zones, voyage et déblocage.
5. **Contenu et habillage** : pixel art, sons, plus d'espèces, aquarium visuel.
