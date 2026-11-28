using FishAndChill.Collection;
using UnityEngine;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Boîte à bijoux de Chiyo (onglet « Reliques », au choix avec le coffre) : petite boîte de laque noire ouverte, vue de
    /// face, filets d'or et vagues dorées à l'intérieur du couvercle, même plaque de laiton que le coffre pour le titre ;
    /// au fond, un coussin de soie indigo creusé d'un seul emplacement en forme de collier (sillon du fil en U, 9 creux
    /// ronds pour les perles) où repose le fil de soie vide. Même format que le coffre (300×160 px, y depuis le bas).
    /// </summary>
    public static class JewelBoxArt
    {
        public const int Width = RelicChestArt.Width;
        public const int Height = RelicChestArt.Height;

        private const int Left = 46, Right = 254;          // la boîte, plus petite que le coffre
        private const int BoxTop = 104, LidBottom = 110;
        private static readonly RectInt Cushion = new RectInt(58, 14, 184, 80);

        private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        private static readonly Color Ink = C(14, 10, 12);
        private static readonly Color Lacquer = C(34, 26, 30);
        private static readonly Color LacquerLight = C(70, 56, 60);
        private static readonly Color LacquerRed = C(96, 24, 22);
        private static readonly Color Gold = C(214, 168, 72);
        private static readonly Color GoldLight = C(250, 222, 140);
        private static readonly Color GoldDark = C(140, 98, 36);
        private static readonly Color Silk = C(40, 50, 104);
        private static readonly Color SilkLight = C(66, 80, 146);
        private static readonly Color SilkDark = C(22, 26, 62);
        private static readonly Color Thread = C(232, 206, 150);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 982451653;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        /// <summary>Enregistre la boîte (Art/UI/Reliques/BoiteABijoux.png) et renvoie son sprite.</summary>
        public static Sprite Save() =>
            PixelArt.Save("Assets/_Project/Art/UI/Reliques/BoiteABijoux.png", Box(), UiSkinArt.PixelsPerUnit, new Vector2(0.5f, 0.5f), Vector4.zero, FilterMode.Point);

        /// <summary>Distance d'un point au sillon du fil (arc en U), en pixels.</summary>
        private static float GrooveDistance(float x, float y)
        {
            float best = float.MaxValue;
            for (float d = -86f; d <= 86f; d += 1f)
            {
                best = Mathf.Min(best, Vector2.Distance(new Vector2(x, y), PearlLore.NecklacePixel(d)));
            }

            return best;
        }

        public static Color[,] Box()
        {
            var p = new Color[Width, Height];
            RectInt plate = RelicChestArt.Plate;

            // ---- Couvercle relevé : laque noire, filet d'or, vagues dorées sur l'intérieur rouge sombre. ----
            for (int y = LidBottom; y < Height; y++)
            {
                float t = (y - LidBottom) / (float)(Height - 1 - LidBottom);
                int inset = Mathf.RoundToInt(Mathf.Lerp(6f, 0f, t));
                for (int x = Left + inset; x < Right - inset; x++)
                {
                    int fromEdge = Mathf.Min(Mathf.Min(x - Left - inset, Right - 1 - inset - x), Mathf.Min(y - LidBottom, Height - 1 - y));
                    Color c;
                    if (fromEdge == 0) c = Ink;
                    else if (fromEdge < 5) c = fromEdge == 1 ? LacquerLight : Lacquer;
                    else if (fromEdge == 5) c = Gold;
                    else
                    {
                        // Vagues en écailles (seigaiha) : arcs concentriques dorés très discrets.
                        int cx = (x / 16) * 16 + ((y / 8) % 2 == 0 ? 8 : 0);
                        float r = Vector2.Distance(new Vector2(x, y), new Vector2(cx, (y / 8) * 8));
                        c = LacquerRed;
                        if (y < (y / 8) * 8 + 8 && Mathf.Abs(r % 3f - 1.5f) < 0.45f && r < 8f) c = Color.Lerp(LacquerRed, Gold, 0.35f);
                        c = Color.Lerp(c, Ink, Mathf.Clamp01((7f - (y - LidBottom)) / 9f) * 0.55f);
                    }

                    p[x, y] = c;
                }
            }

            // Plaque de laiton du titre (même place que sur le coffre).
            for (int x = plate.xMin; x < plate.xMax; x++)
            {
                for (int y = plate.yMin; y < plate.yMax; y++)
                {
                    int e = Mathf.Min(Mathf.Min(x - plate.xMin, plate.xMax - 1 - x), Mathf.Min(y - plate.yMin, plate.yMax - 1 - y));
                    bool cut = (x - plate.xMin < 2 || plate.xMax - 1 - x < 2) && (y - plate.yMin < 2 || plate.yMax - 1 - y < 2);
                    if (cut) continue;
                    bool upperLeft = (x - plate.xMin) * plate.height < (plate.yMax - y) * plate.width;
                    p[x, y] = e == 0 ? Ink : e == 1 ? (upperLeft ? GoldLight : GoldDark) : e == 2 ? Gold : Color.Lerp(GoldDark, Gold, 0.4f + Hash(x, y, 9) * 0.1f);
                }
            }

            // ---- Charnières d'or dans la fente. ----
            for (int y = BoxTop; y < LidBottom; y++)
            {
                for (int x = Left + 4; x < Right - 4; x++) p[x, y] = Ink;
            }

            foreach (int hx in new[] { Left + 30, Right - 42 })
            {
                for (int x = hx; x < hx + 12; x++)
                {
                    for (int y = BoxTop - 2; y < LidBottom + 2; y++)
                    {
                        bool edge = x == hx || x == hx + 11 || y == BoxTop - 2 || y == LidBottom + 1;
                        p[x, y] = edge ? Ink : (x - hx) % 4 == 0 ? GoldDark : Gold;
                    }
                }
            }

            // ---- La boîte : laque noire aux filets d'or, coussin de soie indigo creusé du collier. ----
            for (int x = Left; x < Right; x++)
            {
                for (int y = 0; y < BoxTop; y++)
                {
                    bool outline = x == Left || x == Right - 1 || y == 0 || y == BoxTop - 1;
                    if (outline) { p[x, y] = Ink; continue; }

                    if (Cushion.Contains(new Vector2Int(x, y)))
                    {
                        p[x, y] = CushionPixel(x, y);
                        continue;
                    }

                    bool lip = x >= Cushion.xMin - 1 && x <= Cushion.xMax && y >= Cushion.yMin - 1 && y <= Cushion.yMax;
                    if (lip) { p[x, y] = y == Cushion.yMin - 1 ? Gold : Ink; continue; }

                    // Laque : filet d'or à 3 px du bord.
                    int edge = Mathf.Min(Mathf.Min(x - Left, Right - 1 - x), Mathf.Min(y, BoxTop - 1 - y));
                    Color c = Lacquer;
                    if (edge == 3) c = Gold;
                    else if (Hash(x, y, 3) > 0.985f) c = Color.Lerp(Lacquer, Gold, 0.4f);   // paillettes d'or
                    p[x, y] = c;
                }
            }

            // Petit fermoir d'or au milieu de la façade.
            for (int x = 144; x < 156; x++)
            {
                for (int y = 2; y < 11; y++)
                {
                    int e = Mathf.Min(Mathf.Min(x - 144, 155 - x), Mathf.Min(y - 2, 10 - y));
                    p[x, y] = e == 0 ? Ink : e == 1 ? GoldLight : (x - 144) > 8 ? GoldDark : Gold;
                }
            }

            p[149, 6] = p[150, 6] = Ink;
            return p;
        }

        /// <summary>
        /// Soie indigo à plis doux ; le sillon du fil (où repose le fil de soie vide) et 9 creux ronds pour les perles, avec
        /// un bord éclairé en bas à droite ; les deux bouts du fil finissent par un petit fermoir d'or.
        /// </summary>
        private static Color CushionPixel(int x, int y)
        {
            float fold = Mathf.PerlinNoise(x * 0.018f + 5f, y * 0.03f) * 0.8f + Mathf.PerlinNoise(x * 0.06f, y * 0.06f + 2f) * 0.2f;
            Color c = Color.Lerp(Silk, fold > 0.5f ? SilkLight : SilkDark, Mathf.Abs(fold - 0.5f) * 0.9f);   // plis doux, sans taches
            float shade = Mathf.Max(Mathf.Clamp01((6f - (Cushion.yMax - 1 - y)) / 6f), Mathf.Clamp01((4f - (x - Cushion.xMin)) / 4f) * 0.7f);
            c = Color.Lerp(c, Ink, shade * 0.7f);

            var point = new Vector2(x + 0.5f, y + 0.5f);
            for (int i = 0; i < PearlLore.Count; i++)
            {
                Vector2 center = PearlLore.NecklacePixel(PearlLore.PearlDegrees(i));
                float d = Vector2.Distance(point, center);
                if (d <= PearlLore.PearlRadius + 0.6f)
                {
                    // Creux : sombre, bord clair en bas à droite (lumière d'en haut à gauche).
                    Vector2 off = point - center;
                    if (d > PearlLore.PearlRadius - 0.8f) return off.x - off.y > 2f ? SilkLight : Ink;
                    return Color.Lerp(SilkDark, Ink, 0.5f - (off.y - off.x) * 0.03f);
                }
            }

            float groove = GrooveDistance(point.x, point.y);
            if (groove < 1.1f) return Thread;                                       // le fil de soie vide
            if (groove < 2.2f) return Color.Lerp(SilkDark, Ink, 0.4f);             // le sillon
            foreach (float end in new[] { -86f, 86f })
            {
                if (Vector2.Distance(point, PearlLore.NecklacePixel(end)) < 2.6f) return Gold;   // fermoirs
            }

            return c;
        }
    }
}
