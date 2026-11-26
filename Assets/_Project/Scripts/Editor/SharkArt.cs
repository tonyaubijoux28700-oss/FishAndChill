using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.DetailedFish;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Les requins, de profil, tête à droite, dans le style des poissons (<see cref="DetailedFish"/> pour le corps
    /// éclairé en volume) mais avec ce qui fait un requin : peau lisse sans écailles, museau qui dépasse au-dessus
    /// d'une gueule placée dessous, cinq fentes branchiales (sept chez le requin plat-nez), aileron triangulaire
    /// rejeté en arrière, grandes pectorales en aile, queue au lobe du haut plus long que celui du bas.
    /// Dessins originaux d'après l'anatomie et la livrée réelles de chaque espèce.
    /// </summary>
    public static class SharkArt
    {
        private static Color C(float r, float g, float b) => new Color(r, g, b);

        public static bool TryGet(string speciesId, out DetailedFish fish)
        {
            switch (speciesId)
            {
                case "carcharhinus-melanopterus": fish = Blacktip(); return true;
                case "triaenodon-obesus": fish = Whitetip(); return true;
                case "galeocerdo-cuvier": fish = Tiger(); return true;
                case "mustelus-mustelus": fish = SmoothHound(); return true;
                case "notorynchus-cepedianus": fish = Sevengill(); return true;
                case "carcharhinus-brachyurus": fish = Bronze(); return true;
                case "carcharodon-carcharias": fish = GreatWhite(); return true;
                case "somniosus-microcephalus": fish = Greenland(); return true;
                case "sphyrna-lewini": fish = Hammerhead(); return true;
                case "stegostoma-tigrinum": fish = Zebra(); return true;
                case "scyliorhinus-canicula": fish = Catshark(); return true;
                case "carcharhinus-leucas": fish = Bull(); return true;
                case "heterodontus-japonicus": fish = Bullhead(); return true;
                default: fish = null; return false;
            }
        }

        // ---------- Le modèle ----------

        /// <summary>Une nageoire de requin : base sur le corps (avant, arrière), pointe, bords bombés (+) ou creusés (-).</summary>
        private sealed class SharkFin
        {
            public Vector2 Front, Rear, Apex;
            public float LeadBulge = 1.2f, TrailBulge = -1.6f;
            public bool OverBody;     // la pectorale passe devant le corps
            public bool Tipped = true; // reçoit la couleur des pointes (pointes noires, blanches)
            public float Light = 1f;
            public bool Spine;        // épine devant l'aileron (requin dormeur)
        }

        private sealed class Spec
        {
            public DetailedFish Fish;
            public Color Fin;
            public Color? Tip;
            public float TipRadius = 3.5f;
            public readonly List<SharkFin> Fins = new List<SharkFin>();
        }

        /// <summary>
        /// Requin type (requin gris de récif) sur 120×46, corps de x = 22 à 116. <paramref name="stock"/> épaissit le
        /// corps, <paramref name="snout"/> allonge (+) ou arrondit (-) le museau.
        /// </summary>
        private static Spec Body(Color back, Color flank, Color belly, Color fin, float stock = 1f, float snout = 0f, int seed = 1)
        {
            float s = stock;
            float tip = 116f + snout;
            var fish = new DetailedFish
            {
                Width = 122, Height = 48, CenterY = 22.5f,
                Upper = V((22, 2.4f * s), (30, 3.6f * s), (44, 7f * s), (60, 9f * s), (76, 9f * s), (90, 7.6f * s), (102, 5.2f * s), (110, 3.2f * s + (snout < 0 ? -snout * 0.4f : 0f)), (tip, 1.2f + (snout < 0 ? -snout * 0.5f : 0f))),
                Lower = V((22, 2.2f * s), (30, 3.2f * s), (44, 6.2f * s), (60, 7.8f * s), (76, 7.8f * s), (90, 6.8f * s), (102, 5.2f * s), (110, 3.4f * s), (tip - 1.5f, 1.6f + (snout < 0 ? -snout * 0.5f : 0f))),
                Back = back, Flank = flank, Belly = belly,
                FlankStart = 0.42f, BellyStart = 0.58f, ScaleTexture = false,
                EyeX = 104, EyeYOffset = 1.5f, EyeRadius = 1, Iris = C(0.16f, 0.16f, 0.18f),
                GillX = 0, MouthXStart = 0, MouthXEnd = 0,
                OutlineColor = C(0.06f, 0.07f, 0.09f), Seed = seed,
            };
            return new Spec { Fish = fish, Fin = fin };
        }

        private static float Top(DetailedFish f, float x) => f.CenterY + f.UpperAt(x);

        private static float Bottom(DetailedFish f, float x) => f.CenterY - f.LowerAt(x);

        /// <summary>Aileron sur le dos : base de <paramref name="front"/> à <paramref name="rear"/>, pointe rejetée de <paramref name="sweep"/> derrière la base.</summary>
        private static SharkFin Dorsal(Spec s, float front, float rear, float height, float sweep, float lead = 1.2f, float trail = -1.6f)
        {
            DetailedFish f = s.Fish;
            return new SharkFin
            {
                Front = new Vector2(front, Top(f, front) - 1.5f), Rear = new Vector2(rear, Top(f, rear) - 1.5f),
                Apex = new Vector2(rear - sweep, Top(f, rear) + height), LeadBulge = lead, TrailBulge = trail, Light = 1.06f,
            };
        }

        /// <summary>Nageoire sous le ventre (pelvienne, anale).</summary>
        private static SharkFin Ventral(Spec s, float front, float rear, float depth, float sweep)
        {
            DetailedFish f = s.Fish;
            return new SharkFin
            {
                Front = new Vector2(front, Bottom(f, front) + 1.5f), Rear = new Vector2(rear, Bottom(f, rear) + 1.5f),
                Apex = new Vector2(rear - sweep, Bottom(f, rear) - depth), LeadBulge = 0.8f, TrailBulge = -1f, Light = 0.84f,
            };
        }

        /// <summary>Pectorale en aile : part du bas des flancs derrière les fentes, file en arrière et vers le bas, devant le corps.</summary>
        private static SharkFin Pectoral(Spec s, float x, float length, float drop, float width = 5f)
        {
            DetailedFish f = s.Fish;
            float y = f.CenterY - f.LowerAt(x) * 0.35f;
            return new SharkFin
            {
                Front = new Vector2(x + width * 0.6f, y), Rear = new Vector2(x - width * 0.4f, y + 1f),
                Apex = new Vector2(x - length, Bottom(f, x) - drop), LeadBulge = 1.6f, TrailBulge = -1.2f, OverBody = true, Light = 0.9f,
            };
        }

        /// <summary>
        /// Queue : lobe du haut qui file loin vers l'arrière et vers le haut, lobe du bas plus court ; deux nageoires
        /// accolées au pédoncule (le corps finit en x = 22).
        /// </summary>
        private static void Tail(Spec s, Vector2 upperTip, Vector2 lowerTip, float notchX = 14f, float notchY = 0f)
        {
            DetailedFish f = s.Fish;
            float cy = f.CenterY;
            // Lobe du haut : du dessus du pédoncule à la pointe, puis le bord de fuite creusé jusqu'à l'échancrure.
            s.Fins.Add(new SharkFin
            {
                Front = new Vector2(27f, Top(f, 27f) - 1f), Rear = new Vector2(notchX, cy + notchY),
                Apex = new Vector2(upperTip.x, cy + upperTip.y), LeadBulge = 1f, TrailBulge = -1.4f, Light = 1.02f,
            });
            // Lobe du bas : de l'échancrure à sa pointe, puis jusqu'au dessous du pédoncule.
            if (lowerTip.y < 0f)
            {
                s.Fins.Add(new SharkFin
                {
                    Front = new Vector2(27f, Bottom(f, 27f) + 1f), Rear = new Vector2(notchX + 1f, cy + notchY + 1f),
                    Apex = new Vector2(lowerTip.x, cy + lowerTip.y), LeadBulge = 1f, TrailBulge = -0.8f, Light = 0.88f,
                });
            }

            // Le pédoncule lui-même : un pont plein entre le corps et les lobes.
            s.Fins.Add(new SharkFin
            {
                Front = new Vector2(24f, Top(f, 24f) - 0.5f), Rear = new Vector2(24f, Bottom(f, 24f) + 0.5f),
                Apex = new Vector2(notchX, cy + notchY + 0.5f), LeadBulge = 0f, TrailBulge = 0f, Light = 0.96f, Tipped = false,
            });
        }

        /// <summary>Fentes branchiales, gueule sous le museau, narine : le visage du requin.</summary>
        private static void Face(Spec s, int[] gills, int mouthFrom, int mouthTo, float mouthDrop = 1.5f)
        {
            s.Fish.Details.Add((f, p) =>
            {
                foreach (int gx in gills)
                {
                    float yTop = f.CenterY + f.UpperAt(gx) * 0.25f;
                    float yBottom = f.CenterY - f.LowerAt(gx) * 0.6f;
                    for (int y = Mathf.RoundToInt(yBottom); y <= Mathf.RoundToInt(yTop); y++)
                    {
                        int x = gx + Mathf.RoundToInt((y - yBottom) * 0.18f);
                        if (f.InBody(x, y)) p[x, y] = Color.Lerp(p[x, y], Color.black, 0.42f);
                        if (f.InBody(x + 1, y)) p[x + 1, y] = Color.Lerp(p[x + 1, y], Color.white, 0.12f);
                    }
                }

                // Gueule en arc sous le museau, un peu ouverte vers l'arrière.
                for (int x = mouthFrom; x <= mouthTo; x++)
                {
                    float k = Mathf.InverseLerp(mouthFrom, mouthTo, x);
                    int y = Mathf.RoundToInt(f.CenterY - f.LowerAt(x) + mouthDrop + Mathf.Sin(k * Mathf.PI) * 0.8f + (1f - k) * 0.8f);
                    if (f.InBody(x, y)) p[x, y] = C(0.16f, 0.10f, 0.10f);
                    if (f.InBody(x, y - 1) && k < 0.5f) p[x, y - 1] = Color.Lerp(p[x, y - 1], C(0.94f, 0.92f, 0.88f), 0.5f);
                }

                // Narine, sous le bout du museau.
                int nx = mouthTo + 4;
                int ny = Mathf.RoundToInt(f.CenterY - f.LowerAt(nx) + 1.5f);
                if (f.InBody(nx, ny)) p[nx, ny] = Color.Lerp(p[nx, ny], Color.black, 0.5f);
            });
        }

        private static DetailedFish Finish(Spec s)
        {
            s.Fish.Overlays.Add((f, p) =>
            {
                foreach (SharkFin fin in s.Fins)
                {
                    Paint(s, f, p, fin);
                }
            });
            return s.Fish;
        }

        /// <summary>Peint une nageoire : polygone aux bords en courbes, éclairé par paliers, plus sombre à la racine, pointe colorée.</summary>
        private static void Paint(Spec s, DetailedFish f, Color[,] p, SharkFin fin)
        {
            Vector2 centroid = (fin.Front + fin.Rear + fin.Apex) / 3f;
            var poly = new List<Vector2>();
            AddCurve(poly, fin.Front, fin.Apex, fin.LeadBulge, centroid);
            AddCurve(poly, fin.Apex, fin.Rear, fin.TrailBulge, centroid);
            AddCurve(poly, fin.Rear, fin.Front, 0f, centroid);

            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector2 q in poly)
            {
                minX = Mathf.Min(minX, q.x); maxX = Mathf.Max(maxX, q.x); minY = Mathf.Min(minY, q.y); maxY = Mathf.Max(maxY, q.y);
            }

            Vector2 baseMid = (fin.Front + fin.Rear) * 0.5f;
            float reach = Mathf.Max(1f, Vector2.Distance(baseMid, fin.Apex));
            Vector2 lead = fin.Apex - fin.Front;
            for (int x = Mathf.FloorToInt(minX); x <= Mathf.CeilToInt(maxX); x++)
            {
                for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
                {
                    if (x < 0 || y < 0 || x >= f.Width || y >= f.Height) continue;
                    var c = new Vector2(x + 0.5f, y + 0.5f);
                    if (!Inside(poly, c)) continue;
                    if (!fin.OverBody && f.InBody(x, y)) continue;

                    float k = Vector2.Distance(baseMid, c) / reach;             // 0 à la racine, 1 à la pointe
                    float light = fin.Light * Mathf.Lerp(0.84f, 1.04f, Mathf.Clamp01(k * 2.2f));
                    // Bord d'attaque éclairé (il prend la lumière), bord de fuite dans l'ombre.
                    float side = Cross(lead, c - fin.Front) / Mathf.Max(1f, lead.magnitude);
                    if (Mathf.Abs(side) < 1.1f && k > 0.15f) light += 0.12f;
                    Color color = s.Fin;
                    if (fin.Tipped && s.Tip.HasValue && Vector2.Distance(c, fin.Apex) < s.TipRadius * Mathf.Clamp(reach / 10f, 0.6f, 1.4f))
                    {
                        color = s.Tip.Value;
                    }

                    p[x, y] = Shade(color, Mathf.Round(light * 6f) / 6f);
                }
            }

            if (fin.Spine)
            {
                // Épine dure et claire au bord d'attaque.
                for (int i = 0; i <= 4; i++)
                {
                    Vector2 q = Vector2.Lerp(fin.Front, fin.Apex, i / 4f * 0.8f) + new Vector2(1f, 0f);
                    PixelArt.Set(p, Mathf.RoundToInt(q.x), Mathf.RoundToInt(q.y), C(0.86f, 0.82f, 0.70f));
                }
            }
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>Bord en courbe de a à b : son milieu est poussé de <paramref name="bulge"/> pixels loin du centre (négatif : vers lui).</summary>
        private static void AddCurve(List<Vector2> poly, Vector2 a, Vector2 b, float bulge, Vector2 centroid)
        {
            Vector2 mid = (a + b) * 0.5f;
            Vector2 d = b - a;
            Vector2 n = new Vector2(-d.y, d.x).normalized;
            if (Vector2.Dot(n, mid - centroid) < 0f) n = -n;
            Vector2 control = mid + n * bulge * 2f;
            for (int i = 0; i < 12; i++)
            {
                float t = i / 12f;
                poly.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * control + t * t * b);
            }
        }

        private static bool Inside(List<Vector2> poly, Vector2 q)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                if ((poly[i].y > q.y) != (poly[j].y > q.y) && q.x < (poly[j].x - poly[i].x) * (q.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>Requin « de récif » : aileron haut, deuxième dorsale et anale minuscules, queue en faux.</summary>
        private static void ReefFins(Spec s, float dorsalHeight = 12f, float pectoralLength = 16f)
        {
            s.Fins.Add(Dorsal(s, 82f, 68f, dorsalHeight, 5f));
            s.Fins.Add(Dorsal(s, 38f, 33f, 3.5f, 2.5f, 0.4f, -0.5f));
            s.Fins.Add(Ventral(s, 52f, 46f, 4f, 3f));
            s.Fins.Add(Ventral(s, 36f, 32f, 3f, 2f));
            Tail(s, new Vector2(3f, 15f), new Vector2(10f, -9f));
            s.Fins.Add(Pectoral(s, 88f, pectoralLength, 6f));
            Face(s, new[] { 86, 88, 90, 92, 94 }, 99, 108);
        }

        // ---------- Les espèces ----------

        /// <summary>Requin à pointes noires : élancé, gris-brun, ventre blanc, toutes les pointes trempées dans l'encre, bande claire sur le flanc.</summary>
        private static DetailedFish Blacktip()
        {
            Spec s = Body(C(0.44f, 0.46f, 0.44f), C(0.62f, 0.64f, 0.62f), C(0.95f, 0.95f, 0.93f), C(0.46f, 0.48f, 0.46f), 0.95f, 1f, 401);
            s.Tip = C(0.06f, 0.06f, 0.07f);
            s.TipRadius = 4.2f;
            ReefFins(s);
            // La bande claire qui remonte en pointe sur le flanc, au-dessus de l'anale.
            s.Fish.BodyPatterns.Add((x, y, t, c) => x > 34 && x < 62 && t > 0.5f - (x - 34) * 0.004f && t < 0.62f ? Color.Lerp(c, C(0.92f, 0.92f, 0.90f), 0.55f) : c);
            return Finish(s);
        }

        /// <summary>Requin à pointes blanches : mince, museau court et large, gris semé de quelques taches sombres, pointes blanches à l'aileron et à la queue.</summary>
        private static DetailedFish Whitetip()
        {
            Spec s = Body(C(0.40f, 0.43f, 0.46f), C(0.56f, 0.59f, 0.62f), C(0.92f, 0.93f, 0.94f), C(0.42f, 0.45f, 0.48f), 0.85f, -2.5f, 402);
            s.Tip = C(0.97f, 0.97f, 0.96f);
            s.TipRadius = 3.6f;
            s.Fins.Add(Dorsal(s, 76f, 64f, 10f, 4f));
            s.Fins.Add(Dorsal(s, 38f, 32f, 5f, 2.5f, 0.6f, -0.8f));
            s.Fins.Add(Ventral(s, 52f, 46f, 4f, 3f));
            s.Fins.Add(Ventral(s, 37f, 32f, 4f, 2f));
            Tail(s, new Vector2(3f, 14f), new Vector2(11f, -8f));
            s.Fins.Add(Pectoral(s, 88f, 14f, 5f));
            Face(s, new[] { 86, 88, 90, 92, 94 }, 101, 108);
            s.Fish.Details.Add(Spots(18, 30, 96, 0.1f, 0.5f, C(0.24f, 0.26f, 0.28f), null, 4021, 0.2f));
            // Seules l'aileron et le lobe haut de la queue ont la pointe blanche.
            s.Fins[2].Tipped = false; s.Fins[3].Tipped = false; s.Fins[5].Tipped = false; s.Fins[6].Tipped = false;
            return Finish(s);
        }

        /// <summary>Requin-tigre : massif, museau court et carré, gris-brun barré de rayures sombres verticales, lobe haut de la queue long et effilé.</summary>
        private static DetailedFish Tiger()
        {
            Spec s = Body(C(0.36f, 0.40f, 0.38f), C(0.54f, 0.58f, 0.54f), C(0.92f, 0.92f, 0.88f), C(0.38f, 0.42f, 0.40f), 1.15f, -3f, 403);
            s.Fins.Add(Dorsal(s, 82f, 68f, 12f, 6f));
            s.Fins.Add(Dorsal(s, 38f, 33f, 3.5f, 2.5f, 0.4f, -0.5f));
            s.Fins.Add(Ventral(s, 52f, 46f, 4f, 3f));
            s.Fins.Add(Ventral(s, 36f, 32f, 3f, 2f));
            Tail(s, new Vector2(1f, 16f), new Vector2(11f, -8f));
            s.Fins.Add(Pectoral(s, 88f, 17f, 7f, 6f));
            Face(s, new[] { 86, 88, 90, 92, 94 }, 101, 109, 1f);
            s.Fish.BodyPatterns.Add(VerticalBars(new[] { 28, 34, 40, 46, 52, 58, 64, 70, 76, 82 }, 2, C(0.20f, 0.22f, 0.20f), 0.5f, 0.12f));
            return Finish(s);
        }

        /// <summary>Émissole lisse : petit requin fin et gris, deux dorsales presque égales, queue basse, gros yeux en amande.</summary>
        private static DetailedFish SmoothHound()
        {
            Spec s = Body(C(0.46f, 0.48f, 0.50f), C(0.60f, 0.62f, 0.64f), C(0.90f, 0.90f, 0.88f), C(0.48f, 0.50f, 0.52f), 0.8f, 1f, 404);
            s.Fish.EyeRadius = 2;
            s.Fish.Iris = C(0.72f, 0.70f, 0.40f);
            s.Fins.Add(Dorsal(s, 82f, 70f, 8f, 4f, 1.4f, -1f));
            s.Fins.Add(Dorsal(s, 48f, 38f, 6f, 3f, 1.2f, -0.8f));
            s.Fins.Add(Ventral(s, 58f, 52f, 3f, 3f));
            s.Fins.Add(Ventral(s, 40f, 34f, 3f, 2f));
            Tail(s, new Vector2(3f, 8f), new Vector2(12f, -4f), 16f);
            s.Fins.Add(Pectoral(s, 88f, 12f, 4f));
            Face(s, new[] { 88, 90, 92, 94, 96 }, 102, 108);
            return Finish(s);
        }

        /// <summary>Requin plat-nez : tête large et plate, sept fentes branchiales, une seule dorsale très reculée, brun-gris tacheté de noir.</summary>
        private static DetailedFish Sevengill()
        {
            Spec s = Body(C(0.36f, 0.36f, 0.32f), C(0.52f, 0.52f, 0.46f), C(0.86f, 0.86f, 0.80f), C(0.38f, 0.38f, 0.34f), 1f, -3.5f, 405);
            s.Fins.Add(Dorsal(s, 52f, 42f, 8f, 4f));
            s.Fins.Add(Ventral(s, 56f, 48f, 4f, 3f));
            s.Fins.Add(Ventral(s, 40f, 34f, 4f, 2f));
            Tail(s, new Vector2(2f, 12f), new Vector2(13f, -6f), 16f);
            s.Fins.Add(Pectoral(s, 84f, 13f, 5f));
            Face(s, new[] { 82, 84, 86, 88, 90, 92, 94 }, 100, 109, 1f);
            s.Fish.Details.Add(Spots(40, 26, 104, 0.05f, 0.55f, C(0.12f, 0.12f, 0.10f), null, 4051, 0.3f));
            s.Fish.Details.Add(Spots(12, 30, 96, 0.15f, 0.5f, C(0.82f, 0.82f, 0.76f), null, 4052, 0f));
            return Finish(s);
        }

        /// <summary>Requin cuivre : dos bronze cuivré, ventre blanc bien tranché, pointes un peu sombres, museau pointu.</summary>
        private static DetailedFish Bronze()
        {
            Spec s = Body(C(0.48f, 0.36f, 0.22f), C(0.66f, 0.52f, 0.36f), C(0.94f, 0.92f, 0.88f), C(0.50f, 0.38f, 0.24f), 1f, 1.5f, 406);
            s.Tip = C(0.28f, 0.20f, 0.14f);
            s.Fish.BellyStart = 0.52f;
            ReefFins(s, 11f);
            return Finish(s);
        }

        /// <summary>
        /// Grand requin blanc : torpille massive, museau conique, œil tout noir, gris ardoise sur le dessus et blanc
        /// dessous avec une limite nette en zigzag, grand aileron triangulaire droit, queue en croissant presque symétrique.
        /// </summary>
        private static DetailedFish GreatWhite()
        {
            Spec s = Body(C(0.36f, 0.40f, 0.44f), C(0.44f, 0.48f, 0.52f), C(0.96f, 0.96f, 0.96f), C(0.36f, 0.40f, 0.44f), 1.3f, 1f, 407);
            s.Fish.FlankStart = 0.5f;
            s.Fish.BellyStart = 0.54f;
            s.Fish.EyeRadius = 1;
            s.Fish.Iris = C(0.02f, 0.02f, 0.03f);
            s.Fins.Add(Dorsal(s, 80f, 66f, 13f, 3f, 0.6f, -0.6f));
            s.Fins.Add(Dorsal(s, 36f, 32f, 2.5f, 2f, 0.3f, -0.4f));
            s.Fins.Add(Ventral(s, 52f, 46f, 4f, 3f));
            s.Fins.Add(Ventral(s, 34f, 31f, 2f, 1.5f));
            Tail(s, new Vector2(4f, 16f), new Vector2(6f, -14f), 15f);
            s.Fins.Add(Pectoral(s, 88f, 18f, 8f, 6f));
            Face(s, new[] { 84, 86, 88, 90, 92 }, 99, 109, 1.2f);
            s.Tip = C(0.08f, 0.08f, 0.10f);
            s.TipRadius = 2.5f;
            for (int i = 0; i < s.Fins.Count; i++) s.Fins[i].Tipped = i == s.Fins.Count - 1;   // seule la pectorale a le bout noir
            // Limite blanc / gris en zigzag, bien nette, et quille latérale près de la queue.
            s.Fish.BodyPatterns.Add((x, y, t, c) => t > 0.5f + Mathf.Sin(x * 0.45f) * 0.03f && t < 0.56f ? Color.Lerp(c, C(0.40f, 0.44f, 0.48f), 0.6f) : c);
            return Finish(s);
        }

        /// <summary>Requin du Groenland : gros cylindre brun-gris foncé, petites dorsales égales très reculées, pas d'anale, minuscule œil pâle.</summary>
        private static DetailedFish Greenland()
        {
            Spec s = Body(C(0.22f, 0.24f, 0.26f), C(0.32f, 0.34f, 0.36f), C(0.48f, 0.50f, 0.52f), C(0.24f, 0.26f, 0.28f), 1.2f, -2.5f, 408);
            s.Fish.FlankStart = 0.3f;
            s.Fish.BellyStart = 0.8f;
            s.Fish.Iris = C(0.70f, 0.82f, 0.84f);
            s.Fins.Add(Dorsal(s, 66f, 58f, 4f, 2f, 0.8f, -0.6f));
            s.Fins.Add(Dorsal(s, 46f, 38f, 4f, 2f, 0.8f, -0.6f));
            s.Fins.Add(Ventral(s, 44f, 38f, 3f, 2f));
            Tail(s, new Vector2(4f, 10f), new Vector2(10f, -5f), 16f);
            s.Fins.Add(Pectoral(s, 88f, 9f, 3f, 4f));
            Face(s, new[] { 88, 90, 92, 94, 96 }, 101, 109, 1f);
            s.Fish.Details.Add(Spots(30, 26, 100, 0.05f, 0.6f, C(0.16f, 0.17f, 0.19f), null, 4081, 0.4f));
            // Le copépode accroché à l'œil : un petit filament pâle.
            s.Fish.Overlays.Add((f, p) =>
            {
                int ey = Mathf.RoundToInt(f.CenterY + f.EyeYOffset);
                PixelArt.Set(p, f.EyeX - 1, ey - 1, C(0.88f, 0.86f, 0.74f));
                PixelArt.Set(p, f.EyeX - 2, ey - 2, C(0.80f, 0.78f, 0.66f));
            });
            return Finish(s);
        }

        /// <summary>
        /// Requin-marteau halicorne : gris bronze, grand aileron haut en faux, et la tête en marteau, vue un peu de
        /// face, avec un œil à chaque bout ; bord avant du marteau ondulé (festonné).
        /// </summary>
        private static DetailedFish Hammerhead()
        {
            Spec s = Body(C(0.44f, 0.44f, 0.40f), C(0.60f, 0.60f, 0.56f), C(0.94f, 0.94f, 0.92f), C(0.46f, 0.46f, 0.42f), 0.95f, -1f, 409);
            s.Fins.Add(Dorsal(s, 82f, 70f, 15f, 6f, 1.4f, -2.2f));
            s.Fins.Add(Dorsal(s, 38f, 33f, 3.5f, 2.5f, 0.4f, -0.5f));
            s.Fins.Add(Ventral(s, 52f, 46f, 5f, 3f));
            s.Fins.Add(Ventral(s, 38f, 32f, 4f, 2f));
            Tail(s, new Vector2(2f, 16f), new Vector2(11f, -8f));
            s.Fins.Add(Pectoral(s, 88f, 13f, 5f));
            Face(s, new[] { 88, 90, 92, 94, 96 }, 101, 108);
            s.Tip = C(0.30f, 0.30f, 0.28f);
            s.TipRadius = 2.5f;
            // Le marteau : une lame plate en travers de la tête, de haut en bas, plus claire dessus.
            s.Fish.EyeX = 113;
            s.Fish.EyeYOffset = 9f;
            s.Fish.Overlays.Add((f, p) =>
            {
                float cy = f.CenterY;
                CreatureArt.Limb(p, 112.5f, cy - 7f, 113.5f, cy + 9f, 2.4f, C(0.50f, 0.50f, 0.46f));
                CreatureArt.Blob(p, 112.5f, cy - 7.5f, 2.6f, 2.2f, C(0.50f, 0.50f, 0.46f));
                CreatureArt.Blob(p, 113.5f, cy + 9.5f, 2.6f, 2.2f, C(0.52f, 0.52f, 0.48f));
                // Festons du bord avant et le dessous du marteau, plus clair.
                for (int y = Mathf.RoundToInt(cy - 7f); y <= Mathf.RoundToInt(cy + 8f); y += 3)
                {
                    PixelArt.Set(p, 116, y, C(0.30f, 0.30f, 0.28f));
                }

                for (int y = Mathf.RoundToInt(cy - 8f); y <= Mathf.RoundToInt(cy - 2f); y++)
                {
                    PixelArt.Set(p, 111, y, C(0.80f, 0.80f, 0.76f));
                }

                // Le second œil, au bout du bas du marteau.
                PixelArt.Set(p, 112, Mathf.RoundToInt(cy - 8f), C(0.08f, 0.08f, 0.09f));
            });
            return Finish(s);
        }

        /// <summary>
        /// Requin-zèbre (adulte, « requin léopard ») : sable doré constellé de taches brun sombre, crêtes en relief le
        /// long du corps, queue immense qui fait presque la moitié de sa longueur, petits barbillons, nageoires arrondies.
        /// </summary>
        private static DetailedFish Zebra()
        {
            var fish = new DetailedFish
            {
                Width = 122, Height = 40, CenterY = 20.5f,
                Upper = V((4, 1f), (20, 3f), (40, 5.5f), (56, 8.5f), (74, 9f), (92, 7.5f), (106, 5f), (114, 3f), (118, 1.5f)),
                Lower = V((4, 1f), (20, 2.5f), (40, 4.5f), (56, 7f), (74, 7.5f), (92, 6.8f), (106, 5f), (114, 3.2f), (117, 2f)),
                Back = C(0.70f, 0.58f, 0.34f), Flank = C(0.82f, 0.72f, 0.48f), Belly = C(0.94f, 0.90f, 0.78f),
                FlankStart = 0.45f, BellyStart = 0.68f, ScaleTexture = false,
                EyeX = 107, EyeYOffset = 2f, EyeRadius = 1, Iris = C(0.30f, 0.26f, 0.18f),
                OutlineColor = C(0.10f, 0.08f, 0.05f), Seed = 410,
            };
            var s = new Spec { Fish = fish, Fin = C(0.70f, 0.58f, 0.34f) };
            s.Fins.Add(new SharkFin { Front = new Vector2(78f, Top(fish, 78f) - 1.5f), Rear = new Vector2(64f, Top(fish, 64f) - 1.5f), Apex = new Vector2(64f, Top(fish, 64f) + 7f), LeadBulge = 1.6f, TrailBulge = -0.6f, Light = 1.05f });
            s.Fins.Add(new SharkFin { Front = new Vector2(56f, Top(fish, 56f) - 1.5f), Rear = new Vector2(48f, Top(fish, 48f) - 1.5f), Apex = new Vector2(47f, Top(fish, 47f) + 4f), LeadBulge = 1.2f, TrailBulge = -0.4f, Light = 1.05f });
            s.Fins.Add(Ventral(s, 62f, 54f, 5f, 2f));
            s.Fins.Add(Ventral(s, 50f, 44f, 4f, 2f));
            // Le long lobe de la queue, bas et en ruban, sous la moitié arrière.
            s.Fins.Add(new SharkFin { Front = new Vector2(40f, Bottom(fish, 40f) + 1f), Rear = new Vector2(3f, fish.CenterY), Apex = new Vector2(12f, fish.CenterY - 6f), LeadBulge = 0.6f, TrailBulge = 0.6f, Light = 0.9f });
            s.Fins.Add(Pectoral(s, 92f, 12f, 4f, 7f));
            s.Fins[s.Fins.Count - 1].LeadBulge = 2.4f;
            Face(s, new[] { 92, 94, 96, 98, 100 }, 108, 113, 1f);
            fish.Details.Add(Spots(70, 6, 112, 0.0f, 0.75f, C(0.26f, 0.18f, 0.10f), null, 4101, 0.55f));
            // Crêtes du dos : deux lignes claires en relief.
            fish.BodyPatterns.Add(Lines(new[] { 0.12f, 0.34f }, C(0.92f, 0.84f, 0.62f), 0.35f, 20, 100));
            fish.Details.Add((f, p) =>
            {
                // Petits barbillons sous le museau.
                int bx = 112;
                int by = Mathf.RoundToInt(f.CenterY - f.LowerAt(bx));
                PixelArt.Set(p, bx, by - 1, C(0.62f, 0.50f, 0.30f));
                PixelArt.Set(p, bx - 1, by - 2, C(0.62f, 0.50f, 0.30f));
            });
            return Finish(s);
        }

        /// <summary>Petite roussette : fine, sable semé de petits points bruns, dorsales tout à l'arrière, queue longue et basse, œil de chat.</summary>
        private static DetailedFish Catshark()
        {
            var fish = new DetailedFish
            {
                Width = 116, Height = 34, CenterY = 16.5f,
                Upper = V((6, 1f), (22, 2.5f), (40, 4.5f), (60, 6.5f), (80, 7f), (96, 6f), (106, 4f), (112, 2f)),
                Lower = V((6, 1f), (22, 2.5f), (40, 4f), (60, 5.5f), (80, 6f), (96, 5.5f), (106, 4f), (111, 2.4f)),
                Back = C(0.62f, 0.52f, 0.38f), Flank = C(0.76f, 0.66f, 0.50f), Belly = C(0.94f, 0.92f, 0.84f),
                FlankStart = 0.4f, BellyStart = 0.66f, ScaleTexture = false,
                EyeX = 103, EyeYOffset = 2f, EyeRadius = 1, Iris = C(0.62f, 0.70f, 0.36f),
                OutlineColor = C(0.10f, 0.08f, 0.06f), Seed = 411,
            };
            var s = new Spec { Fish = fish, Fin = C(0.64f, 0.54f, 0.40f) };
            s.Fins.Add(new SharkFin { Front = new Vector2(46f, Top(fish, 46f) - 1.5f), Rear = new Vector2(38f, Top(fish, 38f) - 1.5f), Apex = new Vector2(36f, Top(fish, 36f) + 5f), LeadBulge = 1f, TrailBulge = -0.8f, Light = 1.05f });
            s.Fins.Add(new SharkFin { Front = new Vector2(32f, Top(fish, 32f) - 1.5f), Rear = new Vector2(26f, Top(fish, 26f) - 1.5f), Apex = new Vector2(25f, Top(fish, 25f) + 3.5f), LeadBulge = 0.8f, TrailBulge = -0.6f, Light = 1.05f });
            s.Fins.Add(Ventral(s, 64f, 56f, 4f, 2f));
            s.Fins.Add(Ventral(s, 44f, 36f, 3f, 2f));
            s.Fins.Add(new SharkFin { Front = new Vector2(22f, Bottom(fish, 22f) + 1f), Rear = new Vector2(4f, fish.CenterY), Apex = new Vector2(12f, fish.CenterY - 4.5f), LeadBulge = 0.5f, TrailBulge = 0.5f, Light = 0.9f });
            s.Fins.Add(new SharkFin { Front = new Vector2(22f, Top(fish, 22f) - 1f), Rear = new Vector2(4f, fish.CenterY), Apex = new Vector2(10f, fish.CenterY + 2.5f), LeadBulge = 0.4f, TrailBulge = 0.3f, Light = 1.02f });
            s.Fins.Add(Pectoral(s, 86f, 10f, 4f, 6f));
            s.Fins[s.Fins.Count - 1].LeadBulge = 2f;
            Face(s, new[] { 86, 88, 90, 92, 94 }, 102, 108, 1f);
            fish.Details.Add(Spots(90, 6, 108, 0.0f, 0.62f, C(0.36f, 0.24f, 0.16f), null, 4111, 0.1f));
            fish.Details.Add((f, p) =>
            {
                // Pupille en fente, comme un chat.
                int ey = Mathf.RoundToInt(f.CenterY + f.EyeYOffset);
                PixelArt.Set(p, f.EyeX, ey, C(0.05f, 0.05f, 0.04f));
            });
            return Finish(s);
        }

        /// <summary>Requin-bouledogue : très trapu, museau court, large et rond, petits yeux, gris uni ; il remonte les fleuves.</summary>
        private static DetailedFish Bull()
        {
            Spec s = Body(C(0.42f, 0.44f, 0.44f), C(0.58f, 0.60f, 0.60f), C(0.93f, 0.93f, 0.91f), C(0.44f, 0.46f, 0.46f), 1.3f, -4f, 412);
            s.Tip = C(0.26f, 0.27f, 0.28f);
            s.TipRadius = 2.5f;
            s.Fins.Add(Dorsal(s, 82f, 68f, 12f, 4f, 0.9f, -1.2f));
            s.Fins.Add(Dorsal(s, 38f, 33f, 4f, 2.5f, 0.4f, -0.5f));
            s.Fins.Add(Ventral(s, 52f, 46f, 4f, 3f));
            s.Fins.Add(Ventral(s, 37f, 32f, 3.5f, 2f));
            Tail(s, new Vector2(3f, 16f), new Vector2(10f, -9f));
            s.Fins.Add(Pectoral(s, 88f, 17f, 7f, 7f));
            Face(s, new[] { 86, 88, 90, 92, 94 }, 101, 108, 0.8f);
            return Finish(s);
        }

        /// <summary>
        /// Requin dormeur japonais : petit, tête de cochon au front haut et aux arcades saillantes, une épine devant
        /// chaque dorsale, beige barré de larges selles brun sombre, nageoires arrondies.
        /// </summary>
        private static DetailedFish Bullhead()
        {
            var fish = new DetailedFish
            {
                Width = 110, Height = 42, CenterY = 18.5f,
                Upper = V((16, 2f), (26, 3.5f), (40, 6.5f), (56, 8.5f), (72, 10f), (86, 11f), (96, 10f), (102, 7f), (105, 3f)),
                Lower = V((16, 2f), (26, 3f), (40, 5.5f), (56, 7f), (72, 8f), (86, 8.5f), (96, 8f), (102, 6.5f), (105, 3f)),
                Back = C(0.64f, 0.54f, 0.38f), Flank = C(0.78f, 0.68f, 0.50f), Belly = C(0.92f, 0.86f, 0.72f),
                FlankStart = 0.4f, BellyStart = 0.7f, ScaleTexture = false,
                EyeX = 95, EyeYOffset = 5f, EyeRadius = 1, Iris = C(0.22f, 0.20f, 0.16f),
                OutlineColor = C(0.10f, 0.08f, 0.05f), Seed = 413,
            };
            var s = new Spec { Fish = fish, Fin = C(0.62f, 0.52f, 0.36f) };
            s.Fins.Add(new SharkFin { Front = new Vector2(78f, Top(fish, 78f) - 1.5f), Rear = new Vector2(66f, Top(fish, 66f) - 1.5f), Apex = new Vector2(70f, Top(fish, 70f) + 8f), LeadBulge = 0.6f, TrailBulge = 1f, Light = 1.05f, Spine = true });
            s.Fins.Add(new SharkFin { Front = new Vector2(46f, Top(fish, 46f) - 1.5f), Rear = new Vector2(36f, Top(fish, 36f) - 1.5f), Apex = new Vector2(39f, Top(fish, 39f) + 6.5f), LeadBulge = 0.6f, TrailBulge = 1f, Light = 1.05f, Spine = true });
            s.Fins.Add(Ventral(s, 54f, 46f, 5f, 2f));
            s.Fins.Add(Ventral(s, 36f, 30f, 4f, 1f));
            s.Fins.Add(new SharkFin { Front = new Vector2(20f, Top(fish, 20f) - 1f), Rear = new Vector2(8f, fish.CenterY - 1f), Apex = new Vector2(3f, fish.CenterY + 8f), LeadBulge = 0.6f, TrailBulge = 0.8f, Light = 1.02f });
            s.Fins.Add(new SharkFin { Front = new Vector2(20f, Bottom(fish, 20f) + 1f), Rear = new Vector2(8f, fish.CenterY - 1f), Apex = new Vector2(8f, fish.CenterY - 7f), LeadBulge = 1f, TrailBulge = 0.6f, Light = 0.88f });
            s.Fins.Add(Pectoral(s, 84f, 11f, 5f, 8f));
            s.Fins[s.Fins.Count - 1].LeadBulge = 2.6f;
            Face(s, new[] { 78, 80, 82, 84, 86 }, 96, 103, 0.8f);
            // Larges selles brun sombre, qui descendent jusqu'au milieu du flanc.
            fish.BodyPatterns.Add(VerticalBars(new[] { 22, 34, 48, 62, 80 }, 5, C(0.30f, 0.22f, 0.14f), 0.7f, 0.15f));
            fish.Details.Add((f, p) =>
            {
                // Arcade saillante au-dessus de l'œil, plus claire.
                int ey = Mathf.RoundToInt(f.CenterY + f.EyeYOffset);
                for (int x = f.EyeX - 3; x <= f.EyeX + 2; x++)
                {
                    if (f.InBody(x, ey + 2)) p[x, ey + 2] = Color.Lerp(p[x, ey + 2], C(0.92f, 0.84f, 0.64f), 0.6f);
                }
            });
            return Finish(s);
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
