using System.Collections.Generic;
using UnityEngine;
using static FishAndChill.Editor.VolumeCanvas;
using static FishAndChill.Editor.CreatureShapes;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Légende de l'Atoll des Maldives : Rannamaari, le démon de la mer des légendes maldiviennes, qui arrivait de
    /// l'horizon les nuits de pleine lune « comme un navire couvert de lumières ». Ici un djinn des abysses : long corps
    /// de barracuda noir-vert, couronne de cornes, côtes qui transparaissent sous la peau, rangées de photophores bleus
    /// le long du ventre, mâchoire du bas en avant hérissée de crocs, barbe de filaments aux bouts lumineux, et un
    /// filament qui part du front et porte une perle qui luit devant la gueule. Dorsale en ruban effilochée, longues
    /// pectorales aux veines bleues. De profil, tête à droite, 256×144, y depuis le haut.
    /// </summary>
    public static class RannamaariArt
    {
        public const int Width = 256;
        public const int Height = 144;

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static readonly Color Skin = C(0.07f, 0.16f, 0.15f);
        private static readonly Color SkinDark = C(0.02f, 0.05f, 0.05f);
        private static readonly Color Belly = C(0.30f, 0.40f, 0.36f);
        private static readonly Color Horn = C(0.52f, 0.50f, 0.42f);
        private static readonly Color Bone = C(0.80f, 0.76f, 0.64f);
        private static readonly Color Maw = C(0.14f, 0.04f, 0.07f);
        private static readonly Color Glow = C(0.40f, 0.90f, 1f);
        private static readonly Color Filament = C(0.20f, 0.40f, 0.38f);
        private static readonly Color Ink = C(0.02f, 0.02f, 0.04f);

        private static Spine BuildSpine()
        {
            var spine = new Spine();
            const int n = 60;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                spine.Points.Add(new Vector2(50f + t * 160f, 82f - Mathf.Sin(t * Mathf.PI) * 6f - t * 4f));
                spine.Radius.Add(5f + 18f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.5f)) - 7f * Mathf.Clamp01((t - 0.84f) / 0.16f));
            }

            return spine;
        }

        private static Color Mottle(int x, int y, Color c)
        {
            float n = Noise(x / 6f + 13f, y / 6f);
            if (n < 0.25f) return Color.Lerp(c, SkinDark, 0.6f);
            if (n > 0.8f) return Color.Lerp(c, Belly, 0.2f);
            return Hash(x, y) < 0.05f ? Darker(c, 0.75f) : c;
        }

        private static Color Rays(int x, int y, Color c)
        {
            if ((x + y * 2) % 6 < 1) return Darker(c, 0.65f);
            return Noise(x / 4f, y / 4f) < 0.3f ? Color.Lerp(c, SkinDark, 0.5f) : c;
        }

        public static Color[,] Draw()
        {
            var cv = new VolumeCanvas(Width, Height);
            Spine spine = BuildSpine();
            Vector2 tailBase = spine.Points[0];
            Vector2 head = spine.Points[spine.Points.Count - 1];
            var halos = new List<(Vector2 at, int r)>();

            // ---------- Queue en croissant, déchirée ----------
            var tail = new[] { tailBase + new Vector2(4, -4), tailBase + new Vector2(-28, -34), tailBase + new Vector2(-40, -40), tailBase + new Vector2(-18, -4),
                               tailBase + new Vector2(-40, 30), tailBase + new Vector2(-28, 30), tailBase + new Vector2(4, 4) };
            cv.Piece((x, y) => Poly(tail)(x, y) && Noise(x / 3f + 9f, y / 3f) > 0.14f, Skin, 3f, Rays, 0.85f, 0.45f);

            // ---------- Dorsale en ruban effilochée, bordée de petites lueurs ----------
            Vector2[] dorsal = FinAlong(spine, 0.28f, 0.8f, -1f, k => 16f * Mathf.Sin(k * Mathf.PI) + 4f, 24, 0.3f);
            cv.Piece((x, y) => Poly(dorsal)(x, y) && Noise(x / 3f, y / 3f + 4f) > 0.18f, Skin, 2f, Rays, 0.85f, 0.45f);
            for (int k = 2; k < 22; k += 4)
            {
                float t = Mathf.Lerp(0.28f, 0.8f, k / 24f);
                Vector2 at = spine.At(t, out Vector2 n) - n * (spine.RadiusAt(t) + 16f * Mathf.Sin(k / 24f * Mathf.PI) + 2f);
                cv.Piece(Ellipse(at.x, at.y, 1.1f, 1.1f), Color.Lerp(Glow, Color.white, 0.4f), 1f, null, 0.95f, 0.95f);
                halos.Add((at, 3));
            }

            cv.Piece(Poly(FinAlong(spine, 0.16f, 0.27f, 1f, k => 10f * Mathf.Sin(k * Mathf.PI), 10, 0.6f)), Skin, 2f, Rays, 0.85f, 0.45f);

            // ---------- Le corps ----------
            cv.Piece(spine.Inside, Skin, 12f, (x, y, c) =>
            {
                spine.Frame(x + 0.5f, y + 0.5f, out float t, out float s, out float r);
                return Mottle(x, y, Color.Lerp(c, Belly, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 0.8f, s)) * 0.8f));
            }, 0.82f, 0.4f);

            // Côtes qui transparaissent, courbées vers l'arrière.
            for (int k = 0; k < 8; k++)
            {
                float t = 0.34f + k * 0.055f;
                Vector2 at = spine.At(t, out Vector2 n);
                float r = spine.RadiusAt(t);
                var rib = Curve(at - n * r * 0.72f, at + new Vector2(-5f, 0f), at + n * r * 0.62f + new Vector2(-3f, 0f), 10);
                cv.Piece(Tube(rib, 1f, 0.7f), Color.Lerp(Skin, Bone, 0.32f), 0.6f, (x, y, c) => Noise(x / 2f, y / 2f) < 0.3f ? Color.Lerp(c, Skin, 0.5f) : c, 0.95f, 0.5f);
            }

            // Deux rangées de photophores le long du ventre.
            for (int k = 0; k < 16; k++)
            {
                float t = 0.14f + k * 0.047f;
                foreach (float side in new[] { 0.5f, 0.78f })
                {
                    Vector2 at = spine.At(t, out _) + new Vector2(0, spine.RadiusAt(t) * side);
                    cv.Piece(Ellipse(at.x, at.y, 1.4f, 1.4f), Color.Lerp(Glow, Color.white, 0.4f), 1f, null, 0.95f, 0.95f);
                    if (k % 2 == 0) halos.Add((at, 4));
                }
            }

            // ---------- Longue pectorale aux veines bleues ----------
            Vector2 pec = spine.At(0.7f, out _) + new Vector2(0, spine.RadiusAt(0.7f) - 6f);
            var pecFin = new[] { pec + new Vector2(6, 0), pec + new Vector2(-30, 22), pec + new Vector2(-48, 36), pec + new Vector2(-38, 26), pec + new Vector2(-10, 4) };
            cv.Piece((x, y) => Poly(pecFin)(x, y) && Noise(x / 3f + 20f, y / 3f) > 0.12f, Skin, 3f, (x, y, c) =>
            {
                float along = Vector2.Dot(new Vector2(x, y) - pec, new Vector2(-0.8f, 0.6f));
                float across = Vector2.Dot(new Vector2(x, y) - pec, new Vector2(0.6f, 0.8f));
                return Mathf.Abs(Mathf.Repeat(across + along * 0.15f, 6f) - 3f) < 0.5f ? Color.Lerp(c, Glow, 0.35f) : Rays(x, y, c);
            }, 0.85f, 0.45f);

            // ---------- La tête : mâchoire du bas en avant ----------
            Vector2 hinge = head + new Vector2(-34, 4);
            Vector2 lowTip = head + new Vector2(22, 10);
            cv.Piece(Poly(hinge, head + new Vector2(14, 2), lowTip + new Vector2(-2, -3), hinge + new Vector2(6, 12)), Maw, 2f,
                (x, y, c) => Noise(x / 3f, y / 3f) > 0.62f ? C(0.30f, 0.08f, 0.12f) : c);
            cv.Piece(Poly(hinge + new Vector2(0, 8), lowTip + new Vector2(-4, -2), lowTip + new Vector2(2, 2), lowTip + new Vector2(-6, 8), hinge + new Vector2(4, 20)), Skin, 5f,
                (x, y, c) => Mottle(x, y, Color.Lerp(c, Belly, 0.5f)));
            for (int k = 0; k < 10; k++)
            {
                float f = k / 9f;
                float len = 5f + Hash(k, 1) * 4f + (k == 7 ? 5f : 0f);
                Vector2 up = Vector2.Lerp(hinge + new Vector2(4, 2), head + new Vector2(12, 2), f);
                cv.Piece(Poly(up + new Vector2(-1.2f, -0.5f), up + new Vector2(1.2f, -0.5f), up + new Vector2(0.4f, len)), Bone, 0.8f, null, 0.95f, 0.7f);
                Vector2 low = Vector2.Lerp(hinge + new Vector2(6, 10), lowTip + new Vector2(-3, -2), f);
                float fang = len + (k == 9 || k == 5 ? 6f : 1f);   // deux crocs qui dépassent la mâchoire du haut
                cv.Piece(Poly(low + new Vector2(-1.3f, 0.5f), low + new Vector2(1.3f, 0.5f), low + new Vector2(0.8f, -fang)), Bone, 0.8f, null, 0.95f, 0.7f);
            }

            // ---------- L'œil ----------
            Vector2 eye = head + new Vector2(-18, -6);
            cv.Piece(Ellipse(eye.x, eye.y, 5.5f, 4.5f), SkinDark, 3f);
            cv.Piece(Ellipse(eye.x, eye.y, 3.8f, 3f), Color.Lerp(Glow, Color.white, 0.35f), 2f, null, 0.95f, 0.95f);
            cv.Piece(Box(eye.x - 0.6f, eye.y - 3f, eye.x + 0.6f, eye.y + 3f), Ink, 1f);
            halos.Add((eye, 9));

            // ---------- Couronne de cornes, derrière l'œil ----------
            for (int k = 0; k < 5; k++)
            {
                Vector2 b = head + new Vector2(-50 + k * 6f, -14 + k * 0.8f);
                float tall = 26f + (k == 2 ? 12f : (k % 2) * 6f) - k * 1.5f;
                cv.Piece(Tube(Curve(b, b + new Vector2(2, -tall * 0.6f), b + new Vector2(-7 + k, -tall), 12), 3f, 0.4f), Horn, 2.4f,
                    (x, y, c) => (y % 3 == 0 ? Darker(c, 0.7f) : Hash(x, y) < 0.15f ? Darker(c, 0.8f) : c), 0.85f, 0.55f);
            }

            // Le filament du front et sa perle qui luit devant la gueule.
            Vector2 lureRoot = head + new Vector2(-28, -11);
            Vector2 pearl = head + new Vector2(30, -14);
            cv.Piece(Tube(Curve(lureRoot, head + new Vector2(4, -40), pearl + new Vector2(0, -3), 20), 1.2f, 0.6f), Filament, 1f, null, 0.95f, 0.6f);
            cv.Piece(Ellipse(pearl.x, pearl.y, 3.4f, 3.4f), C(0.90f, 0.97f, 1f), 2f, (x, y, c) => x < pearl.x && y < pearl.y ? Color.white : Color.Lerp(c, Glow, 0.35f), 0.95f, 0.95f);
            halos.Add((pearl, 12));

            // Barbe de filaments sous la mâchoire, bouts lumineux.
            for (int k = 0; k < 5; k++)
            {
                Vector2 b = Vector2.Lerp(hinge + new Vector2(6, 18), lowTip + new Vector2(-6, 6), k / 5f);
                Vector2 end = b + new Vector2(-6 - k * 2f, 22f + Hash(k, 2) * 12f);
                cv.Piece(Tube(Curve(b, b + new Vector2(6, 12), end, 12), 1f, 0.5f), Filament, 0.6f, null, 0.95f, 0.6f);
                cv.Piece(Ellipse(end.x, end.y, 1.2f, 1.2f), Color.Lerp(Glow, Color.white, 0.4f), 1f, null, 0.95f, 0.95f);
                halos.Add((end, 4));
            }

            // Cicatrices.
            cv.Piece(Capsule(118, 66, 132, 74, 0.8f), Color.Lerp(Belly, Color.white, 0.2f), 0.6f, null, 0.95f, 0.7f);
            cv.Piece(Capsule(160, 80, 172, 72, 0.8f), Color.Lerp(Belly, Color.white, 0.2f), 0.6f, null, 0.95f, 0.7f);

            Color[,] p = cv.Finish(Ink, 0.55f);
            foreach ((Vector2 at, int r) in halos)
            {
                Halo(p, Mathf.RoundToInt(at.x), Height - 1 - Mathf.RoundToInt(at.y), r, Glow, 3f);
            }

            return p;
        }
    }
}
