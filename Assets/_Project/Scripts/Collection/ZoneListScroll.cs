using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Défilement de la colonne des zones de l'index : la zone choisie reste toujours visible, avec un peu de marge pour
    /// apercevoir sa voisine. Logique pure, testée.
    /// </summary>
    public static class ZoneListScroll
    {
        /// <summary>Hauteur totale de la liste (lignes et espaces entre elles).</summary>
        public static float ContentHeight(int count, float rowHeight, float gap) =>
            count <= 0 ? 0f : count * rowHeight + (count - 1) * gap;

        /// <summary>Décalage maximal (0 si toute la liste tient dans la vue).</summary>
        public static float MaxOffset(int count, float rowHeight, float gap, float viewHeight) =>
            Mathf.Max(0f, ContentHeight(count, rowHeight, gap) - viewHeight);

        /// <summary>
        /// Nouveau décalage (depuis le haut de la liste) pour que la ligne choisie soit entièrement visible : on ne bouge
        /// que si elle sort de la vue, juste assez pour la montrer avec la marge demandée.
        /// </summary>
        public static float Offset(float current, int selected, int count, float rowHeight, float gap, float viewHeight, float margin)
        {
            float max = MaxOffset(count, rowHeight, gap, viewHeight);
            if (count <= 0 || max <= 0f)
            {
                return 0f;
            }

            selected = Mathf.Clamp(selected, 0, count - 1);
            float top = selected * (rowHeight + gap);
            float bottom = top + rowHeight;
            margin = Mathf.Clamp(margin, 0f, Mathf.Max(0f, (viewHeight - rowHeight) * 0.5f));
            float offset = current;
            if (top - margin < offset)
            {
                offset = top - margin;
            }
            else if (bottom + margin > offset + viewHeight)
            {
                offset = bottom + margin - viewHeight;
            }

            return Mathf.Clamp(offset, 0f, max);
        }

        /// <summary>Des zones sont cachées au-dessus de la vue.</summary>
        public static bool MoreAbove(float offset) => offset > 0.5f;

        /// <summary>Des zones sont cachées sous la vue.</summary>
        public static bool MoreBelow(float offset, int count, float rowHeight, float gap, float viewHeight) =>
            offset < MaxOffset(count, rowHeight, gap, viewHeight) - 0.5f;
    }
}
