namespace FishAndChill.UI
{
    /// <summary>Calculs de la vitrine du marchand (purs, testés).</summary>
    public static class ShowcaseMath
    {
        /// <summary>Objet suivant (+1) ou précédent (-1), en faisant le tour ; 0 s'il n'y a rien.</summary>
        public static int Step(int index, int delta, int count)
        {
            if (count <= 0) return 0;
            int next = (index + delta) % count;
            return next < 0 ? next + count : next;
        }

        /// <summary>
        /// Premier objet de la rangée de vignettes : la fenêtre de <paramref name="window"/> vignettes suit l'objet choisi
        /// (centré), sans dépasser le début ni la fin.
        /// </summary>
        public static int WindowStart(int selected, int count, int window)
        {
            if (count <= window || window <= 0) return 0;
            int start = selected - window / 2;
            return start < 0 ? 0 : start > count - window ? count - window : start;
        }

        /// <summary>Agrandissement entier le plus grand pour qu'un dessin tienne dans la boîte (au moins 1).</summary>
        public static int FitScale(float width, float height, float boxWidth, float boxHeight)
        {
            if (width <= 0f || height <= 0f) return 1;
            int scale = (int)System.Math.Floor(System.Math.Min(boxWidth / width, boxHeight / height));
            return scale < 1 ? 1 : scale;
        }
    }
}
