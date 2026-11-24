using UnityEngine;

namespace FishAndChill.World
{
    /// <summary>
    /// Règles de la vie des bassins (logique pure, testée) : bancs des petites espèces, trois plans de profondeur
    /// (couleur, vitesse, hauteur de nage) et cadrage de la vue de près.
    /// </summary>
    public static class AquariumMath
    {
        /// <summary>
        /// Nombre de poissons dans le banc d'une espèce, selon la longueur de sa miniature (en cases) : 5 pour les tout
        /// petits, 3 pour les petits, seul sinon. Les bêtes du fond vivent seules.
        /// </summary>
        public static int SchoolSize(float lengthUnits, bool onBottom)
        {
            if (onBottom) return 1;
            if (lengthUnits < 0.95f) return 5;
            if (lengthUnits < 1.35f) return 3;
            return 1;
        }

        /// <summary>Teinte d'un plan : le fond du bassin tire vers la couleur de l'eau, le devant reste vif.</summary>
        public static Color PlaneTint(int plane, Color water)
        {
            float amount = plane <= 0 ? 0.5f : plane == 1 ? 0.2f : 0f;
            Color far = Color.Lerp(water, Color.white, 0.35f);
            far.a = 1f;
            return Color.Lerp(Color.white, far, amount);
        }

        /// <summary>Les poissons du fond du bassin paraissent nager plus lentement.</summary>
        public static float PlaneSpeed(int plane) => plane <= 0 ? 0.75f : plane == 1 ? 0.9f : 1f;

        /// <summary>
        /// Hauteur de nage (0 au fond, 1 en surface) d'un plan, pour un tirage <paramref name="roll"/> entre 0 et 1 :
        /// le plan du fond nage plutôt haut, celui de devant plutôt bas, pour que tout le bassin soit habité.
        /// </summary>
        public static float DepthInPlane(int plane, float roll)
        {
            float r = Mathf.Clamp01(roll);
            return plane <= 0 ? Mathf.Lerp(0.4f, 1f, r) : plane == 1 ? Mathf.Lerp(0.15f, 0.9f, r) : Mathf.Lerp(0f, 0.65f, r);
        }

        /// <summary>
        /// Vue de près : taille orthographique (demi-hauteur visible) pour que le bassin <paramref name="tank"/> tienne
        /// en entier avec une marge, sur un écran d'un rapport <paramref name="aspect"/> (largeur / hauteur).
        /// </summary>
        public static float CloseUpSize(Rect tank, float aspect, float margin = 0.6f)
        {
            float byHeight = tank.height * 0.5f + margin;
            float byWidth = (tank.width * 0.5f + margin) / Mathf.Max(0.1f, aspect);
            return Mathf.Max(byHeight, byWidth);
        }
    }
}
