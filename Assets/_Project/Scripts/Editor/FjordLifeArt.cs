using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Vie des fjords de Norvège (16 px par case) : pygargue à queue blanche en vol et son ombre, macareux moine qui se
    /// dandine sur les galets. Tête vers la droite. Les phoques reprennent les otaries de la Côte des Squelettes.
    /// </summary>
    public static class FjordLifeArt
    {
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        /// <summary>Pygargue à queue blanche en vol 34×16 : immenses ailes brunes aux rémiges sombres, tête claire, bec jaune, queue blanche. 2 images.</summary>
        public static Color[,] Eagle(int frame)
        {
            var p = new Color[34, 16];
            Color wing = C(0.36f, 0.26f, 0.18f), tip = C(0.12f, 0.09f, 0.07f);
            float lift = frame == 0 ? 2f : -1.5f;
            CreatureArt.Limb(p, 16, 8, 6, 8 + lift, 2.4f, wing);
            CreatureArt.Limb(p, 6, 8 + lift, 1, 7 + lift, 1.5f, tip);
            CreatureArt.Limb(p, 16, 8, 26, 8 + lift, 2.4f, wing);
            CreatureArt.Limb(p, 26, 8 + lift, 32, 7 + lift, 1.5f, tip);
            CreatureArt.Blob(p, 16f, 8f, 4.4f, 2.2f, wing);
            CreatureArt.Blob(p, 11f, 8f, 2.2f, 1.6f, C(0.94f, 0.92f, 0.88f));   // queue blanche
            CreatureArt.Blob(p, 21.5f, 8.5f, 1.8f, 1.4f, C(0.80f, 0.72f, 0.56f)); // tête claire
            PixelArt.Set(p, 23, 8, C(0.96f, 0.80f, 0.20f));                       // bec
            for (int x = 3; x < 31; x += 3) PixelArt.Set(p, x, Mathf.RoundToInt(6 + lift * 0.6f), tip);
            return p;
        }

        /// <summary>Ombre de l'aigle au sol, 34×12, douce et transparente.</summary>
        public static Color[,] EagleShadow()
        {
            var p = new Color[34, 12];
            Color shade = C(0f, 0f, 0f, 0.22f);
            CreatureArt.Limb(p, 3, 6, 31, 6, 2f, shade);
            CreatureArt.Blob(p, 17f, 6f, 4f, 2.4f, shade);
            return p;
        }

        /// <summary>Macareux moine 10×12 sur les galets : dos noir, ventre blanc, joue grise, gros bec rouge et jaune, pattes orange. 2 images (il se dandine).</summary>
        public static Color[,] Puffin(int frame)
        {
            var p = new Color[10, 12];
            Color black = C(0.08f, 0.08f, 0.10f);
            float lean = frame == 0 ? 0f : 0.6f;
            CreatureArt.Blob(p, 4.5f + lean, 5f, 2.8f, 3.6f, black);
            CreatureArt.Blob(p, 5.2f + lean, 4.4f, 1.8f, 2.6f, C(0.94f, 0.94f, 0.92f));   // ventre
            CreatureArt.Blob(p, 5f + lean, 9f, 2.2f, 2f, black);                           // tête
            CreatureArt.Blob(p, 5.4f + lean, 9f, 1.4f, 1.3f, C(0.82f, 0.84f, 0.86f));      // joue
            PixelArt.Set(p, Mathf.RoundToInt(7 + lean), 9, C(0.90f, 0.30f, 0.16f));         // bec
            PixelArt.Set(p, Mathf.RoundToInt(8 + lean), 9, C(0.96f, 0.76f, 0.20f));
            PixelArt.Set(p, Mathf.RoundToInt(7 + lean), 8, C(0.90f, 0.30f, 0.16f));
            PixelArt.Set(p, Mathf.RoundToInt(5 + lean), 10, C(0.04f, 0.04f, 0.04f));        // œil
            PixelArt.Set(p, frame == 0 ? 3 : 4, 0, C(0.96f, 0.50f, 0.16f));                 // pattes
            PixelArt.Set(p, frame == 0 ? 6 : 5, 0, C(0.96f, 0.50f, 0.16f));
            return CreatureArt.Finish(p, C(0.04f, 0.04f, 0.06f));
        }
    }
}
