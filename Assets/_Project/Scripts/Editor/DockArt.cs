using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Le ponton des voyages, le même dans toutes les zones, et son voilier, dans le style en volume
    /// (<see cref="VolumeCanvas"/>, 32 px par case), vus de dessus. Planches vernies posées en travers, poteaux ronds
    /// cerclés de corde, main courante de corde de chaque côté ; côté rive, un panneau à la rose des vents (le départ des
    /// voyages), des caisses et un tonneau ; au bout, un réverbère, des taquets, un rouleau de corde et une bouée.
    /// Le voilier est celui qui traverse la carte du monde. Le ponton est dessiné bout vers le bas et retourné quand il
    /// part vers le haut.
    /// </summary>
    public static class DockArt
    {
        public const int DockWidth = 84;    // 2 cases de planches + les poteaux de chaque côté
        private const int PlankLeft = 10;
        private const int PlankRight = 74;

        public const int BoatWidth = 56;
        public const int BoatHeight = 120;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Plank = C(0.66f, 0.46f, 0.26f);
        private static readonly Color Gap = C(0.28f, 0.17f, 0.09f);
        private static readonly Color Post = C(0.42f, 0.28f, 0.16f);
        private static readonly Color Rope = C(0.84f, 0.74f, 0.52f);
        private static readonly Color Iron = C(0.22f, 0.22f, 0.24f);

        public static Color[,] Dock(int cells, bool farEndUp)
        {
            int h = cells * 32;
            var cv = new VolumeCanvas(DockWidth, h);

            // Les planches, posées en travers : veinées, un peu usées, clouées aux deux bords.
            cv.Piece(Box(PlankLeft, 0, PlankRight, h), Plank, 1.6f, (x, y, c) =>
            {
                int row = y / 7;
                if (y % 7 == 0) return Gap;
                Color b = row % 3 == 0 ? c : row % 3 == 1 ? Darker(c, 0.92f) : Darker(c, 1.06f);
                if (Mathf.Sin(x * 0.33f + row * 3.1f) > 0.93f) b = Darker(b, 0.85f);
                if (Hash(x / 3, y) < 0.05f) b = Darker(b, 1.1f);
                bool nail = (x == PlankLeft + 4 || x == PlankRight - 5) && y % 7 == 3;
                return nail ? C(0.44f, 0.42f, 0.40f) : b;
            }, 0.9f, 0.7f);
            cv.Piece(Box(PlankLeft - 1, 0, PlankLeft + 2, h), Darker(Plank, 0.72f), 1f);   // longerons des bords
            cv.Piece(Box(PlankRight - 2, 0, PlankRight + 1, h), Darker(Plank, 0.72f), 1f);

            // Poteaux ronds cerclés de corde, et la main courante de corde qui pend entre eux.
            const int step = 40;
            for (int y = 10; y < h; y += step)
            {
                foreach (int px in new[] { 6, DockWidth - 6 })
                {
                    cv.Piece(Ellipse(px, y, 5.5f, 5.5f), Post, 5f, (x, yy, c) => Mathf.Repeat(Vector2.Distance(new Vector2(x, yy), new Vector2(px, y)), 2.2f) < 0.6f ? Darker(c, 0.8f) : c);
                    cv.Piece((x, yy) => { float d = Vector2.Distance(new Vector2(x, yy), new Vector2(px, y)); return d > 4.2f && d < 6.4f; }, Rope, 1f,
                        (x, yy, c) => (x + yy) % 2 == 0 ? Darker(c, 0.82f) : c);
                    if (y + step < h)
                    {
                        var sag = Curve(new Vector2(px, y + 5), new Vector2(px + (px < 20 ? 3 : -3), y + step * 0.5f), new Vector2(px, y + step - 5), 8);
                        cv.Piece(Tube(sag, 1.2f, 1.2f), Rope, 1f, (x, yy, c) => (x + yy) % 3 == 0 ? Darker(c, 0.8f) : c);
                    }
                }
            }

            // Côté rive : le panneau des voyages à la rose des vents, deux caisses et un tonneau.
            cv.Piece(Box(PlankLeft + 22, 4, PlankLeft + 42, 18), C(0.52f, 0.34f, 0.18f), 2.5f);
            cv.Piece(Box(PlankLeft + 24, 6, PlankLeft + 40, 16), C(0.92f, 0.86f, 0.70f), 1.5f);
            Vector2 rose = new Vector2(PlankLeft + 32, 11);
            cv.Piece(Union(Poly(rose + new Vector2(0, -5), rose + new Vector2(1.4f, 0), rose + new Vector2(0, 5), rose + new Vector2(-1.4f, 0)),
                Poly(rose + new Vector2(-5, 0), rose + new Vector2(0, 1.4f), rose + new Vector2(5, 0), rose + new Vector2(0, -1.4f))), C(0.20f, 0.36f, 0.60f), 1f);
            cv.Dot((int)rose.x, (int)rose.y - 5, C(0.86f, 0.22f, 0.18f));
            cv.Piece(Box(PlankLeft + 3, 24, PlankLeft + 17, 38), C(0.62f, 0.44f, 0.24f), 2.5f, (x, y, c) => x % 5 == 0 || y == 31 ? Darker(c, 0.75f) : c);
            cv.Piece(Box(PlankLeft + 5, 39, PlankLeft + 15, 48), C(0.58f, 0.40f, 0.22f), 2.5f, (x, y, c) => x % 5 == 2 ? Darker(c, 0.75f) : c);
            cv.Piece(Ellipse(PlankRight - 11, 30, 7f, 7f), C(0.50f, 0.32f, 0.18f), 5f,
                (x, y, c) => Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(PlankRight - 11, 30)) - 4.6f) < 0.8f ? Iron : c);   // tonneau

            // Au bout : taquets, rouleau de corde, bouée, réverbère.
            foreach (int tx in new[] { PlankLeft + 6, PlankRight - 7 })
            {
                cv.Piece(Union(Box(tx - 4, h - 22, tx + 4, h - 19), Box(tx - 1.5f, h - 24, tx + 1.5f, h - 17)), Iron, 1.5f);
            }

            Vector2 coil = new Vector2(PlankLeft + 14, h - 44);
            cv.Piece((x, y) => { float d = Vector2.Distance(new Vector2(x, y), coil); return d < 7.5f && d > 2f; }, Rope, 3f,
                (x, y, c) => Mathf.Repeat(Vector2.Distance(new Vector2(x, y), coil), 1.8f) < 0.6f ? Darker(c, 0.72f) : c);
            Vector2 ring = new Vector2(PlankRight - 13, h - 64);
            cv.Piece((x, y) => { float d = Vector2.Distance(new Vector2(x, y), ring); return d < 8f && d > 4.5f; }, C(0.94f, 0.94f, 0.92f), 3f,
                (x, y, c) => Mathf.Repeat(Mathf.Atan2(y - ring.y, x - ring.x) / Mathf.PI * 2f, 1f) < 0.5f ? C(0.88f, 0.22f, 0.18f) : c);

            Vector2 lamp = new Vector2(DockWidth - 6, h - 10);
            cv.Piece(Ellipse(lamp.x, lamp.y, 5f, 5f), Iron, 3f);
            cv.Piece(Capsule(lamp.x, lamp.y, lamp.x, lamp.y - 30, 1.6f), Iron, 1.5f);
            cv.Piece(Capsule(lamp.x, lamp.y - 30, lamp.x - 8, lamp.y - 30, 1.2f), Iron, 1.2f);
            cv.Piece(Union(Box(lamp.x - 13, lamp.y - 30, lamp.x - 3, lamp.y - 20), Ellipse(lamp.x - 8, lamp.y - 31, 6f, 2.4f)), Iron, 2f,
                (x, y, c) => y > lamp.y - 28 && y < lamp.y - 21 && Mathf.Abs(x - (lamp.x - 8)) < 3.5f ? C(1f, 0.88f, 0.56f) : c);

            Color[,] p = cv.Finish(C(0.10f, 0.07f, 0.05f), 0.5f);
            if (farEndUp)
            {
                var flipped = new Color[DockWidth, h];
                for (int x = 0; x < DockWidth; x++)
                    for (int y = 0; y < h; y++)
                        flipped[x, y] = p[x, h - 1 - y];
                p = flipped;
            }

            return p;
        }

        /// <summary>
        /// Le voilier des voyages, vu de dessus, proue en haut : coque blanche à liston bleu, pont en teck, cockpit,
        /// mât et bôme avec la voile roulée, haubans, gouvernail.
        /// </summary>
        public static Color[,] Boat()
        {
            var cv = new VolumeCanvas(BoatWidth, BoatHeight);
            float cx = BoatWidth / 2f;
            System.Func<float, float, bool> Hull(float top, float bottom, float half) => (x, y) =>
            {
                if (y < top || y > bottom) return false;
                float t = (y - top) / (bottom - top);
                float w = half * Mathf.Sqrt(Mathf.Sin(Mathf.Clamp01(t * 0.92f + 0.05f) * Mathf.PI));
                if (t > 0.78f) w = Mathf.Lerp(w, half * 0.82f, (t - 0.78f) / 0.22f);   // poupe large et carrée
                return Mathf.Abs(x - cx) <= w;
            };

            cv.Piece(Capsule(cx, BoatHeight - 6, cx, BoatHeight - 1, 2f), C(0.36f, 0.26f, 0.18f), 1.5f);   // gouvernail
            cv.Piece(Hull(2, BoatHeight - 4, 24f), C(0.94f, 0.94f, 0.92f), 4f,
                (x, y, c) => Mathf.Abs(Mathf.Abs(x - cx) - 20.5f) < 1.3f ? C(0.18f, 0.38f, 0.66f) : c);
            cv.Piece(Hull(8, BoatHeight - 8, 19f), C(0.76f, 0.56f, 0.32f), 2f, (x, y, c) => (int)x % 4 == 0 ? Darker(c, 0.84f) : c, 0.85f, 0.6f);   // pont en teck
            cv.Piece(Box(cx - 12, 76, cx + 12, BoatHeight - 14), C(0.62f, 0.44f, 0.26f), 2f, (x, y, c) => y % 6 == 0 ? Darker(c, 0.8f) : c, 0.8f, 0.6f);   // cockpit
            cv.Piece(Box(cx - 9, 50, cx + 9, 72), C(0.92f, 0.92f, 0.90f), 3f);                                // roof de la cabine
            cv.Piece(Ellipse(cx - 4, 58, 2.4f, 1.6f), C(0.56f, 0.78f, 0.86f), 1.5f);                         // hublots
            cv.Piece(Ellipse(cx + 4, 58, 2.4f, 1.6f), C(0.56f, 0.78f, 0.86f), 1.5f);

            // Haubans, mât, bôme et voile roulée.
            cv.Piece(Capsule(cx, 40, cx - 20, 50, 0.5f), C(0.70f, 0.70f, 0.72f), 0.5f);
            cv.Piece(Capsule(cx, 40, cx + 20, 50, 0.5f), C(0.70f, 0.70f, 0.72f), 0.5f);
            cv.Piece(Capsule(cx, 40, cx, 10, 0.5f), C(0.70f, 0.70f, 0.72f), 0.5f);
            cv.Piece(Capsule(cx, 40, cx, 92, 2f), C(0.86f, 0.86f, 0.88f), 1.5f);                             // bôme
            cv.Piece(Capsule(cx, 44, cx, 88, 3.4f), C(0.98f, 0.96f, 0.90f), 3f, (x, y, c) => y % 7 == 0 ? C(0.18f, 0.38f, 0.66f) : c);   // voile roulée et ses rabans
            cv.Piece(Ellipse(cx, 40, 3.4f, 3.4f), C(0.86f, 0.86f, 0.88f), 2.5f);                             // pied du mât
            cv.Piece(Capsule(cx - 15, 18, cx + 15, 18, 0.9f), C(0.70f, 0.70f, 0.72f), 0.8f);                  // balcon avant
            return cv.Finish(C(0.10f, 0.12f, 0.16f), 0.5f);
        }
    }
}
