using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Dessins détaillés des espèces pêchables qui ne sont pas des poissons (crustacés, reptiles…), dans le même style
    /// que les poissons : formes rondes éclairées par paliers (<see cref="DetailedFish.Shade"/>) et contour sélectif.
    /// Dessins originaux d'après l'anatomie réelle des animaux, tête ou pinces vers la droite / l'avant.
    /// </summary>
    public static class CreatureArt
    {
        private static Color C(float r, float g, float b) => new Color(r, g, b);

        private static readonly Vector3 LightDirection = new Vector3(-0.35f, 0.75f, 0.55f).normalized;

        public static bool TryGet(string speciesId, out Color[,] pixels)
        {
            switch (speciesId)
            {
                case "scylla-serrata": pixels = MudCrab(); return true;
                case "panulirus-versicolor": pixels = PaintedLobster(); return true;
                case "chelonia-mydas": pixels = GreenTurtle(); return true;
                case "pacifastacus-leniusculus": pixels = FreshwaterCreatureArt.SignalCrayfish(); return true;
                case "pelophylax-ridibundus": pixels = FreshwaterCreatureArt.MarshFrog(); return true;
                case "chelus-fimbriata": pixels = AmazonCreatureArt.Matamata(); return true;
                case "macrobrachium-amazonicum": pixels = AmazonCreatureArt.AmazonPrawn(); return true;
                default: pixels = null; return false;
            }
        }

        // ---------- Outils de dessin ----------

        private static float Step(float light) => Mathf.Round(light * 6f) / 6f;

        /// <summary>Ellipse éclairée comme un volume rond.</summary>
        internal static void Blob(Color[,] p, float cx, float cy, float rx, float ry, Color color, float rotation = 0f)
        {
            float cos = Mathf.Cos(rotation);
            float sin = Mathf.Sin(rotation);
            int reach = Mathf.CeilToInt(Mathf.Max(rx, ry)) + 1;
            for (int x = Mathf.FloorToInt(cx) - reach; x <= Mathf.CeilToInt(cx) + reach; x++)
            {
                for (int y = Mathf.FloorToInt(cy) - reach; y <= Mathf.CeilToInt(cy) + reach; y++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float u = (dx * cos + dy * sin) / rx;
                    float v = (-dx * sin + dy * cos) / ry;
                    float d = u * u + v * v;
                    if (d > 1f)
                    {
                        continue;
                    }

                    // Normale de la boule, ramenée dans le repère de l'image.
                    float nx = u * cos - v * sin;
                    float ny = u * sin + v * cos;
                    var normal = new Vector3(nx, ny, Mathf.Sqrt(1f - d)).normalized;
                    float light = 0.5f + 0.75f * Mathf.Max(0f, Vector3.Dot(normal, LightDirection));
                    PixelArt.Set(p, x, y, DetailedFish.Shade(color, Step(light)));
                }
            }
        }

        /// <summary>Membre en « capsule » de (x0, y0) à (x1, y1), éclairé comme un cylindre.</summary>
        internal static void Limb(Color[,] p, float x0, float y0, float x1, float y1, float radius, Color color)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float length2 = dx * dx + dy * dy;
            int minX = Mathf.FloorToInt(Mathf.Min(x0, x1) - radius) - 1;
            int maxX = Mathf.CeilToInt(Mathf.Max(x0, x1) + radius) + 1;
            int minY = Mathf.FloorToInt(Mathf.Min(y0, y1) - radius) - 1;
            int maxY = Mathf.CeilToInt(Mathf.Max(y0, y1) + radius) + 1;
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float t = length2 > 0f ? Mathf.Clamp01(((px - x0) * dx + (py - y0) * dy) / length2) : 0f;
                    float ox = px - (x0 + dx * t);
                    float oy = py - (y0 + dy * t);
                    float dist = Mathf.Sqrt(ox * ox + oy * oy);
                    if (dist > radius)
                    {
                        continue;
                    }

                    // Plus clair du côté de la lumière (en haut), plus sombre en bas.
                    float side = radius > 0f ? oy / radius : 0f;
                    PixelArt.Set(p, x, y, DetailedFish.Shade(color, Step(0.95f + side * 0.25f)));
                }
            }
        }

        internal static Color[,] Finish(Color[,] p, Color outline)
        {
            PixelArt.SelectiveOutline(p, outline, 0.5f);
            return p;
        }

        // ---------- Crabe de palétuvier (Scylla serrata) ----------

        /// <summary>
        /// Crabe de palétuvier vu de face, un peu d'en haut : large carapace vert-brun lisse à neuf dents sur chaque
        /// bord avant, yeux pédonculés, deux grosses pinces massives aux doigts orangés, trois paires de pattes
        /// marcheuses et la dernière paire aplatie en palettes de nage.
        /// </summary>
        private static Color[,] MudCrab()
        {
            var p = new Color[96, 56];
            Color shell = C(0.38f, 0.40f, 0.22f);
            Color legs = C(0.34f, 0.36f, 0.20f);
            Color claw = C(0.40f, 0.36f, 0.20f);
            Color finger = C(0.86f, 0.45f, 0.20f);
            Color tip = C(0.15f, 0.12f, 0.10f);

            // Pattes marcheuses (derrière la carapace) : cuisse vers l'extérieur, puis pointe vers le bas.
            for (int i = 0; i < 3; i++)
            {
                float y = 30f - i * 3f;
                foreach (int side in new[] { -1, 1 })
                {
                    float bx = 48f + side * 22f;
                    float kx = 48f + side * (38f + i * 2f);
                    float ky = y + 6f - i;
                    Limb(p, bx, y, kx, ky, 2.4f, legs);
                    Limb(p, kx, ky, kx + side * (4f + i), ky - 18f + i * 2f, 1.8f, legs);
                    PixelArt.Set(p, Mathf.RoundToInt(kx), Mathf.RoundToInt(ky), Color.Lerp(legs, Color.black, 0.35f)); // articulation
                }
            }

            // Palettes de nage (dernière paire), aplaties, à l'arrière.
            Blob(p, 20f, 34f, 7f, 3.2f, legs, 0.5f);
            Blob(p, 76f, 34f, 7f, 3.2f, legs, -0.5f);

            // Carapace : large ovale, plus clair au centre, dents sur le bord avant.
            Blob(p, 48f, 30f, 29f, 13f, shell);
            for (int t = 0; t < 9; t++)
            {
                foreach (int side in new[] { -1, 1 })
                {
                    float a = Mathf.Lerp(0.15f, 1.15f, t / 8f);      // le long du bord avant
                    int x = Mathf.RoundToInt(48f + side * Mathf.Cos(Mathf.PI * 0.5f - a) * 29f);
                    int y = Mathf.RoundToInt(30f + Mathf.Sin(Mathf.PI * 0.5f - a) * 13f) + 1;
                    PixelArt.Set(p, x, y, Color.Lerp(shell, Color.white, 0.25f));
                }
            }

            // Sillons de la carapace (en « H » au centre).
            for (int x = 42; x <= 54; x++)
            {
                PixelArt.Set(p, x, 30, Color.Lerp(p[x, 30], Color.black, 0.2f));
            }

            PixelArt.Set(p, 42, 31, Color.Lerp(shell, Color.black, 0.25f));
            PixelArt.Set(p, 54, 31, Color.Lerp(shell, Color.black, 0.25f));

            // Yeux pédonculés.
            foreach (int ex in new[] { 42, 54 })
            {
                Limb(p, ex, 40f, ex, 44f, 1.2f, shell);
                Blob(p, ex + 0.5f, 45f, 1.8f, 1.8f, tip);
                PixelArt.Set(p, ex, 45, Color.white);
            }

            // Pinces : bras, puis grosse main gonflée, doigts orangés aux pointes sombres.
            foreach (int side in new[] { -1, 1 })
            {
                float sx = 48f + side * 18f;
                float hx = 48f + side * 30f;
                Limb(p, sx, 22f, hx - side * 4f, 16f, 3.2f, claw);
                Blob(p, hx, 15f, 11f, 7.5f, claw, side * 0.25f);
                float fx = hx + side * 9f;
                Limb(p, fx - side * 2f, 17f, fx + side * 6f, 21f, 2.2f, finger);   // doigt mobile (en haut)
                Limb(p, fx - side * 2f, 12f, fx + side * 6f, 13f, 2.2f, finger);   // doigt fixe (en bas)
                PixelArt.Set(p, Mathf.RoundToInt(fx + side * 7f), 21, tip);
                PixelArt.Set(p, Mathf.RoundToInt(fx + side * 7f), 13, tip);
            }

            return Finish(p, C(0.10f, 0.10f, 0.06f));
        }

        // ---------- Langouste porcelaine (Panulirus versicolor) ----------

        /// <summary>
        /// Langouste porcelaine de profil, tête à droite : carapace bleu-vert finement rayée de blanc et de noir,
        /// abdomen en anneaux bordés d'une bande blanche liserée de noir, éventail de la queue, pattes blanches
        /// rayées de bleu, très longues antennes rosées qui partent vers l'avant puis s'arquent au-dessus du corps.
        /// </summary>
        private static Color[,] PaintedLobster()
        {
            var p = new Color[112, 48];
            Color body = C(0.13f, 0.42f, 0.50f);
            Color dark = C(0.06f, 0.18f, 0.24f);
            Color white = C(0.95f, 0.96f, 0.94f);
            Color antenna = C(0.92f, 0.70f, 0.72f);
            Color legBlue = C(0.25f, 0.45f, 0.80f);

            // Pattes marcheuses (sous la carapace) : fines, gris-bleu clair, une bande bleue à chaque articulation.
            Color legPale = Color.Lerp(white, legBlue, 0.35f);
            for (int i = 0; i < 5; i++)
            {
                float x = 64f + i * 6f;
                Limb(p, x, 16f, x - 4f, 9f, 0.9f, legPale);
                Limb(p, x - 4f, 9f, x - 2f, 3f, 0.8f, legPale);
                PixelArt.Set(p, Mathf.RoundToInt(x - 4f), 9, legBlue);
            }

            // Éventail de la queue : telson au milieu et uropodes en palettes qui s'ouvrent, bord clair.
            for (int lobe = -2; lobe <= 2; lobe++)
            {
                float angle = lobe * 0.38f;
                float lx = 20f - Mathf.Cos(angle) * 7f;
                float ly = 20f + Mathf.Sin(angle) * 9f;
                Blob(p, lx, ly, 6.5f, 2.6f, lobe == 0 ? Color.Lerp(body, dark, 0.3f) : body, angle);
                PixelArt.Set(p, Mathf.RoundToInt(lx - Mathf.Cos(angle) * 5f), Mathf.RoundToInt(ly + Mathf.Sin(angle) * 2f), Color.Lerp(body, white, 0.6f));
            }

            // Abdomen : cinq anneaux qui s'élargissent vers la tête, chacun bordé d'une bande blanche liserée de noir.
            for (int s = 0; s < 5; s++)
            {
                float cx = 24f + s * 8f;
                float ry = 6.5f + s * 0.8f;
                Blob(p, cx, 20f, 5.5f, ry, body);
                int band = Mathf.RoundToInt(cx + 3f);
                for (int y = Mathf.RoundToInt(20f - ry + 1); y <= Mathf.RoundToInt(20f + ry - 1); y++)
                {
                    PixelArt.Set(p, band, y, white);
                    PixelArt.Set(p, band + 1, y, dark);
                }
            }

            // Céphalothorax : gros, épineux, finement rayé.
            Blob(p, 78f, 22f, 17f, 10.5f, body);
            for (int x = 64; x <= 92; x++)
            {
                for (int y = 14; y <= 31; y++)
                {
                    if (PixelArt.IsFilled(p, x, y) && (x + y * 2) % 7 == 0)
                    {
                        p[x, y] = Color.Lerp(p[x, y], (x % 2 == 0) ? white : dark, 0.6f);
                    }
                }
            }

            for (int x = 66; x <= 90; x += 4)
            {
                PixelArt.Set(p, x, 33, Color.Lerp(body, white, 0.4f));   // épines du dessus
            }

            // Œil.
            Blob(p, 92f, 27f, 2f, 2f, C(0.08f, 0.06f, 0.06f));
            PixelArt.Set(p, 92, 28, white);

            // Deux grosses antennes épineuses : elles partent vers l'avant en s'affinant, puis se courbent vers
            // l'arrière au-dessus du dos (l'une derrière l'autre, la plus lointaine plus sombre).
            for (int a = 1; a >= 0; a--)
            {
                Color tint = a == 1 ? Color.Lerp(antenna, dark, 0.3f) : antenna;
                Vector2 previous = new Vector2(91f, 27f + a * 2f);
                for (int k = 1; k <= 24; k++)
                {
                    float t = k / 24f;
                    float x = 91f + Mathf.Sin(t * Mathf.PI * 0.85f) * (18f - a * 3f) - t * t * (46f - a * 8f);
                    float y = 27f + a * 2f + Mathf.Sin(t * Mathf.PI * 0.6f) * (17f - a * 2f);
                    float radius = Mathf.Lerp(1.8f, 0.5f, t);
                    Color band = (k / 3) % 2 == 0 ? tint : Color.Lerp(tint, white, 0.5f);
                    Limb(p, previous.x, previous.y, x, y, radius, band);
                    previous = new Vector2(x, y);
                }
            }

            // Antennules, courtes et fines, à l'avant.
            Limb(p, 93f, 22f, 104f, 19f, 0.6f, antenna);
            Limb(p, 93f, 22f, 103f, 25f, 0.6f, antenna);
            return Finish(p, C(0.04f, 0.10f, 0.14f));
        }

        // ---------- Tortue verte (Chelonia mydas) ----------

        /// <summary>
        /// Tortue verte de profil en train de nager, tête à droite : carapace bombée brun-olive à grandes écailles
        /// aux reflets rayonnants, plastron jaune pâle, petite tête ronde au bec court, grandes nageoires avant en
        /// palettes, petites nageoires arrière.
        /// </summary>
        private static Color[,] GreenTurtle()
        {
            var p = new Color[104, 56];
            Color shell = C(0.42f, 0.34f, 0.18f);
            Color scuteLine = C(0.24f, 0.18f, 0.10f);
            Color skin = C(0.46f, 0.48f, 0.30f);
            Color plastron = C(0.90f, 0.84f, 0.58f);

            // Nageoire avant du côté caché, plus sombre, derrière la carapace.
            Blob(p, 60f, 14f, 16f, 5f, Color.Lerp(skin, Color.black, 0.3f), -0.75f);

            // Petite nageoire arrière.
            Blob(p, 18f, 22f, 8f, 4f, skin, 0.5f);

            // Plastron (dessous), puis carapace bombée.
            Blob(p, 50f, 22f, 32f, 7f, plastron);
            Blob(p, 50f, 29f, 34f, 17f, shell);

            // Écailles de la carapace : rangée centrale (vertébrales) et rangée latérale (costales).
            for (int x = 20; x <= 80; x++)
            {
                for (int y = 18; y <= 46; y++)
                {
                    if (!PixelArt.IsFilled(p, x, y) || y < 23)
                    {
                        continue;
                    }

                    float u = (x - 50f) / 34f;
                    float v = (y - 29f) / 17f;
                    float ring = Mathf.Sqrt(u * u + v * v);
                    bool boundary = Mathf.Abs(v - 0.45f) < 0.06f                      // entre vertébrales et costales
                        || (v > 0.45f && Mathf.Abs(Mathf.Repeat(u * 2.6f + 0.5f, 1f) - 0.5f) > 0.45f)
                        || (v <= 0.45f && Mathf.Abs(Mathf.Repeat(u * 2.0f, 1f) - 0.5f) > 0.46f)
                        || ring > 0.94f;                                                // bordure
                    if (boundary)
                    {
                        p[x, y] = Color.Lerp(p[x, y], scuteLine, 0.6f);
                    }
                    else if ((x * 3 + y * 5) % 11 == 0)
                    {
                        p[x, y] = Color.Lerp(p[x, y], C(0.75f, 0.60f, 0.30f), 0.5f);   // reflets rayonnants
                    }
                }
            }

            // Cou et tête, avec écailles claires et un bec court.
            Limb(p, 78f, 27f, 88f, 30f, 4.5f, skin);
            Blob(p, 92f, 31f, 8f, 6.5f, skin);
            foreach (Vector2Int at in new[] { new Vector2Int(90, 34), new Vector2Int(94, 33), new Vector2Int(88, 30), new Vector2Int(92, 28) })
            {
                PixelArt.Set(p, at.x, at.y, Color.Lerp(skin, Color.white, 0.35f));
            }

            Blob(p, 94f, 32f, 1.6f, 1.6f, C(0.06f, 0.05f, 0.04f));                 // œil
            PixelArt.Set(p, 94, 33, Color.white);
            PixelArt.Set(p, 99, 29, C(0.30f, 0.26f, 0.18f));                       // bec
            PixelArt.Set(p, 98, 28, C(0.30f, 0.26f, 0.18f));

            // Grande nageoire avant du côté visible, en palette, avec écailles et bord clair.
            Blob(p, 66f, 13f, 19f, 5.5f, skin, -0.65f);
            for (int k = 0; k < 6; k++)
            {
                int x = 74 - k * 3;
                int y = 21 - k * 3;
                PixelArt.Set(p, x, y, Color.Lerp(skin, Color.white, 0.3f));
            }

            return Finish(p, C(0.10f, 0.08f, 0.04f));
        }
    }
}
