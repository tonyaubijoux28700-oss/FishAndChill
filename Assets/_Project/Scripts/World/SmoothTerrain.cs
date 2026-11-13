using System;
using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Terrain « lissé » : au lieu de cases carrées, la frontière terre / eau est une courbe calculée à partir
    /// des 4 cases voisines (champ interpolé, seuil à 0,5). Le même calcul sert à dessiner les tuiles de
    /// transition (double grille) et à savoir où l'on peut marcher : ce que l'on voit = ce qui collisionne.
    /// </summary>
    public static class SmoothTerrain
    {
        public const float Threshold = 0.5f;

        /// <summary>
        /// Valeur du champ dans un carré dont les coins valent bl, br, tl, tr (bas-gauche, bas-droite,
        /// haut-gauche, haut-droite), au point (u, v) ∈ [0, 1]². Interpolation bilinéaire : les coins sont
        /// coupés en biseau, ce qui transforme les « escaliers » de cases en vraies diagonales.
        /// </summary>
        public static float Field(float bl, float br, float tl, float tr, float u, float v)
        {
            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);
            float bottom = Mathf.Lerp(bl, br, u);
            float top = Mathf.Lerp(tl, tr, u);
            return Mathf.Lerp(bottom, top, v);
        }

        /// <summary>
        /// Valeur du champ en un point exprimé en « coordonnées de cases » : la case (i, j) a son centre en (i, j).
        /// <paramref name="cellValue"/> donne 1 pour une case pleine (terre), 0 sinon.
        /// </summary>
        public static float Sample(Vector2 cellSpacePosition, Func<int, int, float> cellValue)
        {
            int i = Mathf.FloorToInt(cellSpacePosition.x);
            int j = Mathf.FloorToInt(cellSpacePosition.y);
            float u = cellSpacePosition.x - i;
            float v = cellSpacePosition.y - j;
            return Field(cellValue(i, j), cellValue(i + 1, j), cellValue(i, j + 1), cellValue(i + 1, j + 1), u, v);
        }

        /// <summary>Masque des 4 coins (1 = bas-gauche, 2 = bas-droite, 4 = haut-gauche, 8 = haut-droite).</summary>
        public static int Mask(bool bl, bool br, bool tl, bool tr)
        {
            return (bl ? 1 : 0) | (br ? 2 : 0) | (tl ? 4 : 0) | (tr ? 8 : 0);
        }

        public static void MaskCorners(int mask, out float bl, out float br, out float tl, out float tr)
        {
            bl = (mask & 1) != 0 ? 1f : 0f;
            br = (mask & 2) != 0 ? 1f : 0f;
            tl = (mask & 4) != 0 ? 1f : 0f;
            tr = (mask & 8) != 0 ? 1f : 0f;
        }
    }
}
