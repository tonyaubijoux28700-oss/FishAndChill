using UnityEngine;

namespace FishAndChill.UI
{
    /// <summary>
    /// Géographie simplifiée de la carte du monde (logique pure, testée) : la projection qui place chaque zone
    /// (<c>FishingZone.MapPosition</c>, de 0 à 1) à sa vraie longitude / latitude, et les continents en grands
    /// polygones (tracés à la main, grossiers) pour dessiner la carte en pixel art.
    /// </summary>
    public static class WorldChart
    {
        public enum Ground
        {
            Sea,
            Green,
            Desert,
            Tundra,
            Ice,
        }

        // Projection : Mahana (-150°, -17°) en (0,12 ; 0,36) et Annecy (6°, 46°) en (0,52 ; 0,74).
        private const float LonPerUnit = 156f / 0.4f;
        private const float LatPerUnit = 63f / 0.38f;

        /// <summary>Longitude et latitude (degrés) d'un point de la carte (0..1).</summary>
        public static Vector2 ToLonLat(Vector2 normalized)
        {
            return new Vector2(-150f + (normalized.x - 0.12f) * LonPerUnit, -17f + (normalized.y - 0.36f) * LatPerUnit);
        }

        /// <summary>Point de la carte (0..1) d'une longitude / latitude.</summary>
        public static Vector2 FromLonLat(Vector2 lonLat)
        {
            return new Vector2(0.12f + (lonLat.x + 150f) / LonPerUnit, 0.36f + (lonLat.y + 17f) / LatPerUnit);
        }

        private static readonly Vector2[][] Lands =
        {
            // Amérique du Nord
            P(-168, 66, -162, 70, -140, 70, -120, 72, -95, 73, -80, 70, -62, 60, -56, 52, -66, 45, -70, 42, -76, 35, -81, 30, -81, 25,
              -84, 30, -90, 29, -97, 27, -97, 20, -91, 19, -87, 21, -88, 15, -83, 10, -78, 8, -80, 7, -86, 11, -92, 14, -105, 20,
              -110, 23, -112, 30, -117, 32, -124, 40, -124, 48, -133, 55, -146, 60, -158, 58, -165, 62),
            // Groenland
            P(-55, 60, -44, 60, -20, 70, -18, 80, -40, 83, -60, 80, -70, 76, -55, 68),
            // Islande
            P(-24, 64, -14, 64, -14, 66.5f, -24, 66.5f),
            // Amérique du Sud
            P(-80, 8, -62, 10, -50, 0, -35, -6, -40, -22, -48, -28, -58, -38, -65, -42, -68, -52, -72, -54, -75, -45, -73, -35,
              -71, -20, -76, -13, -81, -5, -78, 2),
            // Europe et Asie
            P(-9, 37, -9, 43, -2, 44, -5, 48, 2, 51, 8, 54, 5, 58, 5, 62, 15, 69, 28, 71, 40, 67, 55, 69, 70, 73, 90, 76, 110, 77,
              140, 72, 160, 70, 180, 68, 180, 65, 170, 60, 160, 55, 156, 51, 142, 47, 135, 43, 130, 42, 129, 35, 126, 35, 125, 39,
              121, 39, 122, 31, 120, 25, 110, 21, 108, 16, 109, 11, 105, 9, 103, 1, 101, 3, 100, 13, 98, 8, 98, 16, 94, 17, 90, 22,
              87, 21, 80, 15, 77, 8, 73, 17, 70, 22, 66, 25, 59, 22, 55, 17, 44, 12, 42, 16, 35, 28, 35, 36, 27, 37, 26, 40, 22, 37,
              19, 42, 13, 45, 12, 44, 16, 41, 18, 40, 15, 38, 8, 44, 3, 43, -1, 37, -6, 36),
            // Grande-Bretagne et Irlande
            P(-6, 50, 2, 51, 0, 54, -3, 58, -6, 57),
            P(-10, 52, -6, 52, -6, 55, -10, 54),
            // Afrique
            P(-17, 21, -17, 15, -8, 5, 5, 5, 9, 4, 9, -2, 13, -12, 12, -17, 15, -27, 18, -34, 26, -34, 33, -28, 35, -22, 40, -15,
              40, -5, 43, 0, 51, 11, 43, 12, 38, 18, 33, 28, 32, 31, 20, 32, 10, 37, -1, 36, -6, 36, -10, 30),
            // Madagascar
            P(44, -13, 50, -16, 47, -25, 44, -22),
            // Japon
            P(130, 31, 135, 33, 140, 35, 142, 40, 145, 44, 141, 46, 139, 41, 136, 37, 132, 35),
            // Philippines, Sumatra, Bornéo, Java, Nouvelle-Guinée
            P(120, 18, 122, 18, 126, 7, 122, 7),
            P(95, 5, 98, 4, 106, -5, 104, -6, 96, 0),
            P(109, 1, 117, 7, 119, 1, 116, -4, 110, -3),
            P(105, -6, 114, -7, 114, -8.5f, 106, -7.5f),
            P(131, -1, 141, -3, 150, -10, 143, -9, 138, -8, 132, -4),
            // Australie et Nouvelle-Zélande
            P(114, -22, 122, -17, 130, -12, 137, -12, 142, -11, 146, -19, 153, -27, 150, -37, 141, -38, 135, -35, 129, -32, 115, -34),
            P(172, -35, 178, -38, 174, -41, 167, -46, 171, -44),
        };

        /// <summary>Petites îles de Polynésie (dont Mahana) : centre et rayon (degrés).</summary>
        private static readonly Vector3[] Islets =
        {
            new Vector3(-150f, -17f, 1.6f), new Vector3(-145f, -15f, 0.9f), new Vector3(-140f, -9f, 1f), new Vector3(-157f, -21f, 0.9f),
            new Vector3(-155.5f, 19.5f, 1.4f), new Vector3(178f, -18f, 1.2f), new Vector3(-172f, -13.8f, 0.9f),
        };

        private static Vector2[] P(params float[] values)
        {
            var points = new Vector2[values.Length / 2];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2(values[i * 2], values[i * 2 + 1]);
            }

            return points;
        }

        /// <summary>Longitude ramenée entre -180° et 180°.</summary>
        public static float WrapLon(float lon) => Mathf.Repeat(lon + 180f, 360f) - 180f;

        public static bool IsLand(float lon, float lat)
        {
            lon = WrapLon(lon);
            if (lat < -70f) return true; // Antarctique
            foreach (Vector3 islet in Islets)
            {
                if (new Vector2(lon - islet.x, lat - islet.y).sqrMagnitude <= islet.z * islet.z) return true;
            }

            foreach (Vector2[] land in Lands)
            {
                if (Inside(land, lon, lat)) return true;
            }

            return false;
        }

        /// <summary>Ce qui couvre le sol en ce point : mer, verdure, désert, toundra ou glace.</summary>
        public static Ground GroundAt(float lon, float lat)
        {
            if (!IsLand(lon, lat)) return Ground.Sea;
            lon = WrapLon(lon);
            if (lat < -62f || lat > 76f || (lon > -60f && lon < -15f && lat > 62f)) return Ground.Ice;
            if (lat > 62f) return Ground.Tundra;
            bool sahara = lat > 15f && lat < 31f && lon > -16f && lon < 34f;
            bool arabia = lat > 14f && lat < 31f && lon > 35f && lon < 58f;
            bool outback = lat < -20f && lat > -32f && lon > 120f && lon < 145f;
            bool gobi = lat > 37f && lat < 47f && lon > 75f && lon < 112f;
            bool kalahari = lat < -18f && lat > -28f && lon > 15f && lon < 25f;
            return sahara || arabia || outback || gobi || kalahari ? Ground.Desert : Ground.Green;
        }

        /// <summary>Point dans un polygone (règle pair-impair).</summary>
        public static bool Inside(Vector2[] polygon, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
