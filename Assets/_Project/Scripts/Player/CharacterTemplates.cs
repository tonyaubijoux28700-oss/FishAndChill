namespace FishAndChill.Player
{
    /// <summary>
    /// Gabarits pixel art des personnages, 32×48, dessinés à la main. Chaque ligne est écrite en abrégé : un nombre puis
    /// une lettre répète la lettre (« 6.10S » = 6 vides puis 10 pixels de peau). Les vues de face et de dos sont écrites
    /// sur une moitié (16 pixels) puis recopiées en miroir ; le profil (vers la droite) est écrit en entier.
    /// Une ligne vide ne change rien (calques). Lettres (couleur choisie par <see cref="CharacterPainter"/>) :
    /// S peau (relief calculé), s ombre de peau, E/W/I œil (pupille, reflet, iris), k sourcil, m bouche, p joue ;
    /// H cheveux, h ombre, j reflet, z cheveux rasés, r élastique ; T haut, A manche, t/u ombre/lumière du haut ;
    /// B short, b ombre ; F sandale, f ombre. « . » = rien (calque : garde le dessous), « _ » = remet le corps.
    /// </summary>
    public static class CharacterTemplates
    {
        // ---------- Corps de face (moitié gauche, lignes 0 à 39) ----------

        public static readonly string[] FrontUpper =
        {
            "", "", "", "", "", "",
            "11.5S", "8.8S", "7.9S", "6.10S", "6.10S", "6.10S", "6.10S", "6.10S",
            "6.3S3k4S", "5.1s4S1W1E4S", "5.1s4S2E4S", "5.1s4S2I4S", "6.2S2p6S", "6.10S", "6.9S1m",
            "7.9S", "7.9S", "8.8S", "10.6S", "12.4s",
            "9.7T", "7.3A6T", "6.4A6T", "5.5A6T", "5.5A6T", "5.4A7T",
            "5.3S8T", "5.3S8T", "5.3S8T", "5.3S8B", "5.3S8B", "6.2S8B", "8.8B", "8.8B",
        };

        /// <summary>Jambes de face (lignes 40 à 47, largeur entière) : à l'arrêt, pas gauche, pas droit.</summary>
        public static readonly string[][] FrontLegs =
        {
            new[] { "8.6B4.6B8.", "8.6B4.6B8.", "9.4S6.4S9.", "9.4S6.4S9.", "9.4S6.4S9.", "8.6F4.6F8.", "8.6F4.6F8.", "32." },
            new[] { "8.6B4.6B8.", "8.6B4.6B8.", "9.4S6.4S9.", "9.4S6.4S9.", "8.6F5.4S9.", "8.6F4.6F8.", "18.6F8.", "32." },
            new[] { "8.6B4.6B8.", "8.6B4.6B8.", "9.4S6.4S9.", "9.4S6.4S9.", "9.4S5.6F8.", "8.6F4.6F8.", "8.6F18.", "32." },
        };

        // ---------- Profil, regard vers la droite (largeur entière) ----------

        public static readonly string[] SideHead =
        {
            "", "", "", "", "", "",
            "11.8S13.", "9.13S10.", "8.15S9.", "7.17S8.", "7.18S7.", "7.18S7.", "7.19S6.", "7.19S6.",
            "7.15S3k1S6.", "7.6S3s6S2E2S6.", "7.6S1s1S1s6S2E3S5.", "7.6S3s6S2I3S5.", "7.6S2s7S2p3S5.",
            "8.17S7.", "8.14S2m1S7.", "9.15S8.", "10.13S9.", "12.10S10.", "14.7S11.", "14.6s12.",
        };

        /// <summary>Corps de profil (lignes 26 à 47) : à l'arrêt, bras en avant, bras en arrière.</summary>
        public static readonly string[][] SideBody =
        {
            new[]
            {
                "12.9T11.", "11.4T4A3T10.", "11.3T6A2T10.", "11.3T6A2T10.", "11.3T6A2T10.", "11.3T6A2T10.",
                "11.4T4S3T10.", "11.4T4S3T10.", "11.4T4S3T10.", "11.4B4S3B10.", "11.4B4S3B10.", "11.4B4S3B10.",
                "11.11B10.", "11.11B10.", "12.9B11.", "13.8B11.", "14.3s4S11.", "14.3s4S11.", "14.3s4S11.",
                "13.4f6F9.", "13.4f6F9.", "32.",
            },
            new[]
            {
                "12.9T11.", "11.5T4A2T10.", "11.4T6A1T10.", "11.4T6A1T10.", "11.4T6A1T10.", "11.4T6A1T10.",
                "11.7T4S10.", "11.8T4S9.", "11.8B4S9.", "11.9B3S9.", "11.11B10.", "11.11B10.",
                "11.11B10.", "11.11B10.", "11.4B3.5B9.", "10.4s4.4S10.", "9.4s6.4S9.", "8.4s8.4S8.", "8.4s8.4S8.",
                "6.5f8.6F7.", "6.5f8.6F7.", "32.",
            },
            new[]
            {
                "12.9T11.", "11.2T4A5T10.", "11.1T6A4T10.", "11.1T6A4T10.", "11.1T6A4T10.", "11.1T6A4T10.",
                "9.4S8T11.", "8.4S9T11.", "8.4S9B11.", "7.3S1.10B11.", "11.11B10.", "11.11B10.",
                "11.11B10.", "11.11B10.", "12.9B11.", "13.4s4S11.", "13.4s5S10.", "13.4s1.5F9.", "13.4s1.5F9.",
                "12.5f15.", "12.5f15.", "32.",
            },
        };

        // ---------- Coiffures ----------

        private static readonly string[] ShortFront =
        {
            "", "", "",
            "11.5H", "8.8H", "6.10H", "5.3H3j5H", "4.3H3j6H", "4.12H", "4.12H", "4.12H", "4.12H",
            "4.7H1.1H1.2H", "4.5H3.1H3.", "4.3H9.", "4.2H10.", "4.2H10.",
        };

        private static readonly string[] ShortBack =
        {
            "", "", "",
            "11.5H", "8.8H", "6.10H", "5.3H3j5H", "4.3H3j6H", "4.12H", "4.12H", "4.12H", "4.12H", "4.12H",
            "4.12H", "4.12H", "4.12H", "4.12H", "4.12H", "4.12H", "4.12H", "5.11H", "6.10H", "7.9H", "10.6h",
        };

        private static readonly string[] ShortSide =
        {
            "", "", "",
            "11.8H13.", "9.12H11.", "7.15H10.", "6.6H4j7H9.", "5.6H4j9H8.", "5.20H7.", "5.20H7.", "5.21H6.", "5.21H6.",
            "5.14H2.2H1.1H7.", "5.13H14.", "5.9H18.", "5.8H19.", "5.8H19.", "5.8H19.", "6.7H19.", "6.6H20.", "7.5H20.",
            "8.3h21.",
        };

        private static readonly string[] SpikyFront = { "9.1H3.1H2.", "8.2H2.3H1.", "7.4H1.4H", "6.10H", "", "", "", "", "", "", "", "", "", "4.2H2.2H2.2H2." };
        private static readonly string[] SpikyBack = { "9.1H3.1H2.", "8.2H2.3H1.", "7.4H1.4H", "6.10H" };
        private static readonly string[] SpikySide = { "8.2H3.2H17.", "7.4H1.4H16.", "6.14H12." };

        private static readonly string[] BobFront =
        {
            "", "", "", "", "", "", "", "", "", "", "", "", "", "",
            "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "4.3h9.",
        };

        private static readonly string[] BobBack =
        {
            "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
            "5.11H", "5.11H", "5.11H", "6.10H", "6.10H", "7.9h",
        };

        private static readonly string[] BobSide =
        {
            "", "", "", "", "", "", "", "", "", "", "", "", "", "",
            "4.9H19.", "4.9H19.", "4.9H19.", "4.9H19.", "4.9H19.", "4.9H19.", "4.9H19.", "4.9H19.", "4.9H19.", "4.9H19.",
            "5.8h19.",
        };

        private static readonly string[] LongFront =
        {
            "", "", "", "", "", "", "", "", "", "", "", "", "", "",
            "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.",
            "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.", "3.4H9.",
            "3.3h10.",
        };

        private static readonly string[] LongBack =
        {
            "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
            "5.11H", "5.11H", "5.11H", "5.11H", "5.11H", "6.10H", "8.8H", "8.8H", "8.8H", "8.8H",
            "8.8H", "8.8H", "8.8H", "8.8H", "8.8H", "9.7h",
        };

        private static readonly string[] LongSide =
        {
            "", "", "", "", "", "", "", "", "", "", "", "", "",
            "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.",
            "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.", "4.8H20.",
            "4.8H20.", "5.6h21.",
        };

        private static readonly string[] PonytailBack =
        {
            "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
            "14.2r", "14.2H", "14.2H", "14.2H", "14.2H", "14.2H", "14.2H", "14.2H", "14.2H", "14.2H", "14.2H", "15.1h",
        };

        private static readonly string[] PonytailSide =
        {
            "", "", "", "", "", "", "", "", "", "",
            "3.3H26.", "2.3H1r26.", "2.4H26.", "2.4H26.", "2.4H26.", "2.4H26.", "2.4H26.", "2.4H26.", "2.4H26.", "2.4H26.",
            "2.4H26.", "3.3H26.", "3.2h27.",
        };

        private static readonly string[] BunTopFront = { "13.3H", "12.4H", "12.4H", "13.3h" };
        private static readonly string[] BunFringe = { "", "", "", "", "", "", "", "", "", "", "", "", "4.2H10_", "4.2H10_" };
        private static readonly string[] BunLowBack =
        {
            "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
            "12.4H", "11.5H", "11.5H", "11.5H", "11.5H", "12.4h",
        };

        private static readonly string[] BunTopSide = { "9.5H18.", "8.7H17.", "8.7H17." };
        private static readonly string[] BunLowSide =
        {
            "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
            "3.4H25.", "2.5H25.", "2.5H25.", "2.5H25.", "2.5H25.", "3.4h25.",
        };

        private static readonly string[] ShavedFront =
        {
            "", "", "", "", "",
            "8.8z", "6.10z", "6.10z", "6.10z", "6.2z8.", "6.1z9.", "6.1z9.", "6.1z9.", "6.1z9.",
        };

        private static readonly string[] ShavedBack =
        {
            "", "", "", "", "",
            "8.8z", "10.6z", "8.8z", "7.9z", "6.10z", "6.10z", "6.10z", "6.10z", "6.10z", "6.10z", "6.10z", "6.10z",
            "6.10z", "6.10z", "6.10z", "6.10z", "7.9z", "8.8z",
        };

        private static readonly string[] ShavedSide =
        {
            "", "", "", "", "", "",
            "11.8z13.", "9.13z10.", "8.15z9.", "7.16z9.", "7.15z10.", "7.12z13.", "7.6z19.", "7.6z19.", "7.6z19.",
            "7.6z19.", "7.6z19.", "7.6z19.", "7.6z19.", "8.5z19.",
        };

        /// <summary>
        /// Calques d'une coiffure : [vue][calque], vues 0 face, 1 dos (moitiés en miroir), 2 profil (largeur entière).
        /// </summary>
        public static string[][][] Hair(int style, bool hat)
        {
            switch (style)
            {
                case 1: // hérissée
                    return new[] { new[] { ShortFront, SpikyFront }, new[] { ShortBack, SpikyBack }, new[] { ShortSide, SpikySide } };
                case 2: // mi-longue
                    return new[] { new[] { ShortFront, BobFront }, new[] { ShortBack, BobBack }, new[] { ShortSide, BobSide } };
                case 3: // longue
                    return new[] { new[] { ShortFront, LongFront }, new[] { ShortBack, LongBack }, new[] { ShortSide, LongSide } };
                case 4: // queue de cheval
                    return new[] { new[] { ShortFront }, new[] { ShortBack, PonytailBack }, new[] { ShortSide, PonytailSide } };
                case 5: // chignon : en haut, ou bas sous un chapeau
                    return new[]
                    {
                        new[] { ShortFront, BunFringe, BunTopFront },
                        new[] { ShortBack, hat ? BunLowBack : BunTopFront },
                        new[] { ShortSide, hat ? BunLowSide : BunTopSide },
                    };
                case 6: // rasée
                    return new[] { new[] { ShavedFront }, new[] { ShavedBack }, new[] { ShavedSide } };
                default: // courte
                    return new[] { new[] { ShortFront }, new[] { ShortBack }, new[] { ShortSide } };
            }
        }

        // ---------- Marchand (de face, moitié en miroir, 48 lignes) ----------

        /// <summary>
        /// Lettres propres au marchand : W bob blanc, w son ombre, N bleu marine (ruban, ancre), G cheveux gris,
        /// M moustache, T marinière, K tablier, q poche brodée, P pantalon, O bottes.
        /// </summary>
        public static readonly string[] Merchant =
        {
            "16.", "16.",
            "11.4W1N", "9.6W1N", "8.7W1N", "8.6W2N", "8.8N", "8.8N", "4.12W", "3.13W", "4.12w",
            "6.2G8S", "6.2G8S", "6.2G1S4k3S", "5.1s1G3S1W1E4S", "5.1s1G3S2E4S", "6.1G3S2I2S2p", "6.8S2p",
            "6.3S7M", "6.2S8M", "6.2S2M5S1m", "7.1S2M6S", "7.9S", "8.8S", "10.6S", "12.4s",
            "9.7T", "7.9T", "6.5T5K", "5.6T5K", "5.6T5K", "5.6T5K", "5.3S3T5K", "5.3S2T3K2q1K", "5.3S2T3K2q1K",
            "5.3S1T7K", "5.3S1P7K", "6.2S1P7K", "8.1P7K", "8.8P",
            "8.6P2.", "8.6P2.", "9.4P3.", "9.4P3.", "9.4P3.", "8.6O2.", "8.6O2.", "16.",
        };
    }
}
