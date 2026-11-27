using FishAndChill.Fishing;
using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Une ligne de la table des espèces (<c>Data/Especes.csv</c>), déjà lue et convertie (voir <see cref="SpeciesTable"/>).
    /// Les réglages laissés vides prennent la valeur par défaut de la rareté.
    /// </summary>
    public sealed class SpeciesRow
    {
        public string Id;
        public string CommonName;
        public string ScientificName;

        /// <summary>Nom de l'asset de la zone (IleMahana, LacAnnecy…).</summary>
        public string Zone;

        public Rarity Rarity;
        public CreatureKind Kind;

        /// <summary>Canne ou épuisette (colonne Engin ; vide : canne pour les poissons, épuisette pour le reste).</summary>
        public CatchMethod Method;

        /// <summary>Nom féminin (colonne Genre, « f ») : accorde les messages (« ta perche », « la carpe », « vendue »).</summary>
        public bool Feminine;
        public BodyShape Shape;
        public Vector2 SizeCm;
        public int Price;

        /// <summary>Fréquence relative dans sa zone (poids du tirage).</summary>
        public float Weight;

        public float Speed;
        public float ZoneWidth;
        public int Hits;
        public int Misses;
        public FightStyle Fight;
        public ActiveTime Time;

        /// <summary>Temps qu'il faut pour qu'elle morde (colonne Meteo).</summary>
        public WeatherNeed Weather;

        /// <summary>Types de leurres auxquels l'espèce mord (naturel, cuillere, ondulante, nageur, souple, surface, jig, turlutte, traine, mouche).</summary>
        public string[] Baits = new string[0];

        /// <summary>Hameçons qui la tiennent (aberdeen, triple, circulaire, forge, texan, mouche).</summary>
        public string[] Hooks = new string[0];

        public string Description;

        /// <summary>Couleur de secours (silhouette, dessin générique).</summary>
        public Color Color;

        // ---------- Dessin paramétrable (utilisé seulement s'il n'existe pas de fiche dessinée à la main) ----------

        /// <summary>Silhouette : fuseau, haut, allonge, serpent, rond, disque, poisson-chat (vide = fiche à la main).</summary>
        public string Drawing;

        public Color Back;
        public Color Flank;
        public Color Belly;
        public Color Fin;

        /// <summary>Motif : aucun, bandes, points, lignes, marbre, ocelle.</summary>
        public string Pattern;

        public Color PatternColor;

        /// <summary>Queue : fourchue, croissant, droite, ronde.</summary>
        public string Tail;

        public Color Eye;

        /// <summary>Détails en plus : barbillons, dents, epines, pinnules, adipeuse.</summary>
        public string[] Extras = new string[0];

        public bool HasExtra(string extra)
        {
            foreach (string e in Extras)
            {
                if (SpeciesTable.Normalize(e) == SpeciesTable.Normalize(extra)) return true;
            }

            return false;
        }
    }
}
