using UnityEngine;
using static FishAndChill.Editor.DetailedFish;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les poissons qui ne ressemblent à aucun autre : hippocampe (debout, en anneaux, queue enroulée) et poisson-lune
    /// (un disque sans queue, avec deux immenses nageoires). Dessins originaux d'après l'anatomie réelle.
    /// </summary>
    public static class CuriosityArt
    {
        private static Color C(float r, float g, float b) => new Color(r, g, b);

        /// <summary>
        /// Hippocampe (Hippocampus kuda) : debout, tête de cheval tournée vers la droite au long museau en tube,
        /// couronne sur le crâne, ventre bombé, corps en anneaux, queue enroulée en spirale ; jaune d'or, nageoire du
        /// dos translucide.
        /// </summary>
        public static Color[,] Seahorse()
        {
            var p = new Color[56, 84];
            Color skin = C(0.90f, 0.70f, 0.22f);
            Color ring = C(0.62f, 0.42f, 0.10f);

            // Nageoire du dos, en éventail translucide, derrière le corps.
            for (int i = 0; i <= 6; i++)
            {
                CreatureArt.Limb(p, 22f, 44f + i * 1.2f, 13f, 41f + i * 2.2f, 0.6f, C(0.96f, 0.88f, 0.62f));
            }

            // Queue : descend puis s'enroule en spirale vers l'avant.
            Vector2[] tail = { new Vector2(26, 30), new Vector2(24, 22), new Vector2(23, 15), new Vector2(25, 9), new Vector2(30, 6), new Vector2(34, 9), new Vector2(33, 13), new Vector2(29, 13) };
            for (int i = 0; i < tail.Length - 1; i++)
            {
                float r = Mathf.Lerp(4f, 1.2f, i / (float)(tail.Length - 2));
                CreatureArt.Limb(p, tail[i].x, tail[i].y, tail[i + 1].x, tail[i + 1].y, r, skin);
            }

            // Tronc au ventre bombé vers l'avant, puis le cou qui se courbe.
            CreatureArt.Blob(p, 28f, 34f, 5.5f, 6f, skin);
            CreatureArt.Blob(p, 30f, 44f, 7.5f, 8f, skin);
            CreatureArt.Blob(p, 27f, 54f, 5.5f, 6f, skin);
            CreatureArt.Limb(p, 27f, 58f, 30f, 64f, 4f, skin);
            // Tête, museau en tube, couronne.
            CreatureArt.Blob(p, 33f, 67f, 6f, 5f, skin);
            CreatureArt.Limb(p, 36f, 66f, 48f, 62f, 1.8f, skin);
            CreatureArt.Limb(p, 31f, 71f, 30f, 77f, 1.6f, skin);
            CreatureArt.Blob(p, 30f, 77f, 2f, 1.6f, C(0.96f, 0.80f, 0.34f));
            // Petite pectorale derrière la tête.
            CreatureArt.Blob(p, 30f, 60f, 2f, 1.4f, C(0.96f, 0.88f, 0.62f));

            // Anneaux : une ligne plus sombre tous les 3 pixels, sur le tronc et la queue.
            for (int x = 0; x < 56; x++)
            {
                for (int y = 8; y < 60; y++)
                {
                    if (p[x, y].a > 0.5f && y % 3 == 0 && p[x, y].g < 0.9f) p[x, y] = Color.Lerp(p[x, y], ring, 0.55f);
                }
            }

            // Petites épines claires sur les crêtes, et l'œil.
            foreach (Vector2 spike in new[] { new Vector2(23, 50), new Vector2(23, 40), new Vector2(22, 32), new Vector2(37, 45), new Vector2(34, 36), new Vector2(28, 70) })
            {
                PixelArt.Set(p, (int)spike.x, (int)spike.y, C(1f, 0.92f, 0.60f));
            }

            CreatureArt.Blob(p, 35f, 68f, 1.6f, 1.6f, C(0.30f, 0.22f, 0.10f));
            PixelArt.Set(p, 35, 68, C(0.04f, 0.03f, 0.02f));
            PixelArt.Set(p, 36, 69, C(0.98f, 0.96f, 0.90f));
            return CreatureArt.Finish(p, C(0.24f, 0.14f, 0.04f));
        }

        /// <summary>
        /// Poisson-lune (môle) : énorme disque gris argenté marbré, sans vraie queue mais avec un « gouvernail » ondulé
        /// (le clavus), immenses nageoires dorsale et anale qui battent ensemble, petite bouche en bec, petit œil.
        /// </summary>
        public static Color[,] Sunfish()
        {
            var fin = C(0.50f, 0.52f, 0.54f);
            var fish = new DetailedFish
            {
                Width = 86, Height = 124, CenterY = 61.5f,
                Upper = V((12, 14f), (18, 22f), (30, 26f), (46, 27f), (62, 24f), (74, 17f), (81, 9f), (84, 3f)),
                Lower = V((12, 14f), (18, 22f), (30, 26f), (46, 27f), (62, 24f), (74, 17f), (81, 9f), (84, 3f)),
                Back = C(0.42f, 0.46f, 0.50f), Flank = C(0.66f, 0.70f, 0.72f), Belly = C(0.86f, 0.88f, 0.88f),
                FlankStart = 0.3f, BellyStart = 0.7f, ScaleTexture = false,
                Caudal = new Tail { Shape = TailShape.Round, XTip = 4, XBase = 13, Span = 15f, Base = C(0.56f, 0.58f, 0.60f), Ray = C(0.44f, 0.46f, 0.48f), Edge = C(0.70f, 0.72f, 0.74f) },
                EyeX = 70, EyeYOffset = 4f, EyeRadius = 2, Iris = C(0.36f, 0.38f, 0.42f),
                GillX = 0, MouthXStart = 82, MouthXEnd = 85, MouthYOffset = -1f,
                OutlineColor = C(0.10f, 0.12f, 0.14f), Seed = 71,
            };
            // Les deux immenses nageoires, hautes et étroites, presque face à face à l'arrière du disque.
            fish.Fins.Add(new Fin { Side = FinSide.Up, XRear = 16, XFront = 26, HeightRear = 32f, HeightFront = 10f, Base = fin, Ray = C(0.38f, 0.40f, 0.42f), Edge = C(0.30f, 0.32f, 0.34f) });
            fish.Fins.Add(new Fin { Side = FinSide.Down, XRear = 16, XFront = 26, HeightRear = 32f, HeightFront = 10f, Base = fin, Ray = C(0.38f, 0.40f, 0.42f), Edge = C(0.30f, 0.32f, 0.34f) });
            fish.PectoralFins.Add(new Pectoral { BaseX = 58, BaseYOffset = 0f, Length = 6, Width = 2.5f, Slope = 0.1f, Base = C(0.70f, 0.72f, 0.74f), Ray = C(0.52f, 0.54f, 0.56f), Edge = C(0.82f, 0.84f, 0.86f) });
            // Peau épaisse marbrée de taches claires.
            fish.Details.Add(Spots(70, 16, 80, 0.15f, 0.9f, C(0.86f, 0.88f, 0.88f), null, 711, 0.6f));
            return fish.Draw();
        }

        private static Vector2[] V(params (float x, float h)[] points)
        {
            var result = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                result[i] = new Vector2(points[i].x, points[i].h);
            }

            return result;
        }
    }
}
