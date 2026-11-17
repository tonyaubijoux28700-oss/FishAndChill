namespace FishAndChill.Player
{
    /// <summary>
    /// Gabarits pixel art du pêcheur (16 colonnes, lignes depuis le haut), dessinés à la main pixel par pixel.
    /// Chaque lettre est un rôle, coloré selon l'apparence par <see cref="CharacterPainter"/> :
    /// S/s/E/m/p peau, ombre, œil, bouche, joue ; T/t/u haut, ombre, lumière ; A/a manche ; B/b short ; F/f sandale ;
    /// H/h/j cheveux, ombre, reflet ; z cheveux rasés ; r élastique ; X/x/y chapeau, ombre, lumière ; R ruban ; W blanc.
    /// « . » = rien (dans un calque : garde ce qui est dessous) ; « _ » (calque) = remet le corps (efface les cheveux).
    /// </summary>
    public static class CharacterTemplates
    {
        // ---------- Corps : face (3 images) et profil vers la droite (3 images) ----------

        public static readonly string[][] BodyFront =
        {
            new[]
            {
                "................", "................", "................",
                "......SSSS......", "....SSSSSSSS....", "...SSSSSSSSSs...", "...SSSSSSSSSs...", "...SSSSSSSSSs...",
                "...SSESSSSESs...", "...SSESSSSESs...", "...SpSSmmSSps...", "....sSSSSSSs....", "......ssss......",
                "...AATTTTTTAa...", "..AATTTTTTTtaa..", "..SSTTTTTTTtSs..", "..SSTTTTTTTtSs..", "..SSBBBBBBBbSs..",
                "....BBBBBBBb....", "....BBb..BBb....", ".....Ss..Ss.....", ".....Ss..Ss.....", "....FFf..FFf....",
                "................",
            },
            new[]
            {
                "................", "................", "................",
                "......SSSS......", "....SSSSSSSS....", "...SSSSSSSSSs...", "...SSSSSSSSSs...", "...SSSSSSSSSs...",
                "...SSESSSSESs...", "...SSESSSSESs...", "...SpSSmmSSps...", "....sSSSSSSs....", "......ssss......",
                "...AATTTTTTAa...", "..AATTTTTTTtaa..", "..SSTTTTTTTtSs..", "..SSTTTTTTTtSs..", "..SSBBBBBBBbSs..",
                "....BBBBBBBb....", "....BBb..BBb....", ".....Ss..Ss.....", "....FFf..Ss.....", ".........FFf....",
                "................",
            },
            new[]
            {
                "................", "................", "................",
                "......SSSS......", "....SSSSSSSS....", "...SSSSSSSSSs...", "...SSSSSSSSSs...", "...SSSSSSSSSs...",
                "...SSESSSSESs...", "...SSESSSSESs...", "...SpSSmmSSps...", "....sSSSSSSs....", "......ssss......",
                "...AATTTTTTAa...", "..AATTTTTTTtaa..", "..SSTTTTTTTtSs..", "..SSTTTTTTTtSs..", "..SSBBBBBBBbSs..",
                "....BBBBBBBb....", "....BBb..BBb....", ".....Ss..Ss.....", ".....Ss..FFf....", "....FFf.........",
                "................",
            },
        };

        public static readonly string[][] BodySide =
        {
            new[]
            {
                "................", "................", "................",
                ".....SSSS.......", "....SSSSSSS.....", "...SSSSSSSSS....", "...SSSSSSSSSS...", "...SSSSSSSSSS...",
                "...SSSSSSSESS...", "...SSSSpSSESSS..", "...sSSSSSSpSm...", "....sSSSSSSS....", "......sss.......",
                ".....TTTTTt.....", ".....TTAAat.....", ".....TTAAat.....", ".....TTSStt.....", ".....BBSSBb.....",
                ".....BBBBBb.....", ".....BBBBBb.....", "......sSS.......", "......sSS.......", ".....fFFFF......",
                "................",
            },
            new[]
            {
                "................", "................", "................",
                ".....SSSS.......", "....SSSSSSS.....", "...SSSSSSSSS....", "...SSSSSSSSSS...", "...SSSSSSSSSS...",
                "...SSSSSSSESS...", "...SSSSpSSESSS..", "...sSSSSSSpSm...", "....sSSSSSSS....", "......sss.......",
                ".....TTTTTt.....", ".....TTTAAa.....", ".....TTTAAa.....", ".....TTTTSS.....", ".....BBBBBSS....",
                ".....BBBBBb.....", ".....BBBBBb.....", ".....ss..SS.....", "....ss....SS....", "...fff....FFF...",
                "................",
            },
            new[]
            {
                "................", "................", "................",
                ".....SSSS.......", "....SSSSSSS.....", "...SSSSSSSSS....", "...SSSSSSSSSS...", "...SSSSSSSSSS...",
                "...SSSSSSSESS...", "...SSSSpSSESSS..", "...sSSSSSSpSm...", "....sSSSSSSS....", "......sss.......",
                ".....TTTTTt.....", ".....TAAaTt.....", ".....TAAaTt.....", "....SSTTTTt.....", "...SSBBBBBb.....",
                ".....BBBBBb.....", ".....BBBBBb.....", ".....SS..ss.....", "....SS....ss....", "...FFF....fff...",
                "................",
            },
        };

        // ---------- Coiffures : face, dos, profil (calques à partir de la ligne 0) ----------

        private static readonly string[] ShortFront =
        {
            "................", "................",
            ".....HjjHH......", "....HjjHHHHh....", "...HjHHHHHHHh...", "..hHHHHHHHHHHh..", "..hHHHHHhHHHHh..",
            "..hHH.hH..hHHh..", "..hH........Hh..",
        };

        private static readonly string[] ShortBack =
        {
            "................", "................",
            ".....HjjHH......", "....HjjHHHHh....", "...HjHHHHHHHh...", "..hHHHHHHHHHHh..", "..hHHHHhHHHHHh..",
            "..hHHhHHHHhHHh..", "..hHHHHHHHHHHh..", "..hhHHHHHHHHhh..", "...hhHHHHHHhh...", "....hhhhhhhh....",
        };

        private static readonly string[] ShortSide =
        {
            "................", "................",
            "....HHjjH.......", "...HHjjHHHH.....", "..hHHHHHHHHH....", "..hHHHHHHHHHH...", "..hHHHHHHHHHHh..",
            "..hHHHHH.hH.h...", "..hHHHH.........", "..hHHH..........", "..hhH...........", "...hh...........",
        };

        private static readonly string[] SpikyTop =
        {
            "....h..H..h.....", "...hH.HH.HHh....", "...HHjHHjHHHh...",
        };

        private static readonly string[] SpikyTopSide =
        {
            "...h..H.........", "..hH.HH.H.......", "..HHjHHjH.......",
        };

        private static readonly string[] SpikyFringe =
        {
            "", "", "", "", "", "", "",
            "..hH_H_H_hH_Hh..",
        };

        private static readonly string[] BobFront =
        {
            "", "", "", "", "", "", "", "",
            "..hHH......HHh..", "..hHH......HHh..", "..hHh......hHh..", "..hhh......hhh..",
        };

        private static readonly string[] BobBack =
        {
            "", "", "", "", "", "", "", "", "",
            "..hHHHHHHHHHHh..", "..hHHHHHHHHHHh..", "..hHHHjHHjHHHh..", "..hhhhhhhhhhhh..",
        };

        private static readonly string[] BobSide =
        {
            "", "", "", "", "", "", "", "",
            "..hHHHHH........", "..hHHHh.........", "..hHHHh.........", "..hHHHh.........", "..hhhhh.........",
        };

        private static readonly string[] LongFront =
        {
            "", "", "", "", "", "", "", "",
            "..hHH......HHh..", "..hHH......HHh..", "..hHH......HHh..", "..hHH......HHh..", "..hHh......hHh..",
            "..hHh......hHh..", "..hHh......hHh..", "..hhh......hhh..", "...h........h...",
        };

        private static readonly string[] LongBack =
        {
            "", "", "", "", "", "", "", "", "",
            "..hHHHHHHHHHHh..", "..hHHHHHHHHHHh..", "..hHHjHHHHjHHh..", "..hHHHHHHHHHHh..", "..hHHHHHHHHHHh..",
            "..hHHHjHHjHHHh..", "..hHHHHHHHHHHh..", "...hhhhhhhhhh...",
        };

        private static readonly string[] LongSide =
        {
            "", "", "", "", "", "", "", "",
            "..hHHHHH........", "..hHHHh.........", "..hHHHh.........", "..hHHHh.........", "..hHHh..........",
            "..hHjh..........", "..hHHh..........", "..hHHh..........", "...hh...........",
        };

        private static readonly string[] PonytailFront =
        {
            "", "", "", "", "", "", "",
            "..hHHHHhH_hHHh..", "..hH........HhH.", ".............Hh.", "..............h.",
        };

        private static readonly string[] PonytailBack =
        {
            "", "", "", "", "", "", "", "", "", "", "", "",
            ".......rr.......", ".......HH.......", ".......Hh.......", ".......jh.......", ".......hh.......",
            "........h.......",
        };

        private static readonly string[] PonytailSide =
        {
            "", "", "", "", "", "",
            ".r..............", ".H..............", ".H..............", ".Hh.............", ".hh.............",
            "..h.............",
        };

        private static readonly string[] BunTop =
        {
            "......HHh.......", ".....HjHHh......",
        };

        private static readonly string[] BunTopSide =
        {
            "...HHh..........", "..HjHHh.........",
        };

        private static readonly string[] BunFringe =
        {
            "", "", "", "", "", "",
            "..hHHHHHHHHHHh..", "..hHh______hHh..",
        };

        private static readonly string[] BunLowBack =
        {
            "", "", "", "", "", "", "", "", "", "", "",
            "......HjHh......", "......hhhh......",
        };

        private static readonly string[] BunLowSide =
        {
            "", "", "", "", "", "", "", "", "",
            ".HHh............", ".hhh............",
        };

        private static readonly string[] ShavedFront =
        {
            "", "", "",
            "......zzzz......", "....zzzzzzzz....", "...zzzzzzzzzz...", "...z........z...",
        };

        private static readonly string[] ShavedBack =
        {
            "", "", "",
            "......zzzz......", "....zzzzzzzz....", "...zzzzzzzzzz...", "...zzzzzzzzzz...", "...zzzzzzzzzz...",
            "...zzzzzzzzzz...", "...zzzzzzzzzz...", "...zzzzzzzzzz...", "....zzzzzzzz....",
        };

        private static readonly string[] ShavedSide =
        {
            "", "", "",
            ".....zzzz.......", "....zzzzzzz.....", "...zzzzzzzzz....", "...zzzzzzzz.....", "...zzzz.........",
            "...zzz..........", "...zz...........",
        };

        /// <summary>Calques d'une coiffure, dans l'ordre : [vue][calque]. Vues : 0 face, 1 dos, 2 profil.</summary>
        public static string[][][] Hair(int style, bool hat)
        {
            switch (style)
            {
                case 1: // hérissée
                    return new[]
                    {
                        new[] { ShortFront, SpikyTop, SpikyFringe },
                        new[] { ShortBack, SpikyTop },
                        new[] { ShortSide, SpikyTopSide },
                    };
                case 2: // mi-longue
                    return new[] { new[] { ShortFront, BobFront }, new[] { ShortBack, BobBack }, new[] { ShortSide, BobSide } };
                case 3: // longue
                    return new[] { new[] { ShortFront, LongFront }, new[] { ShortBack, LongBack }, new[] { ShortSide, LongSide } };
                case 4: // queue de cheval
                    return new[]
                    {
                        new[] { ShortFront, PonytailFront }, new[] { ShortBack, PonytailBack }, new[] { ShortSide, PonytailSide },
                    };
                case 5: // chignon : en haut, ou bas sous un chapeau
                    return new[]
                    {
                        new[] { ShortFront, BunFringe, BunTop },
                        new[] { ShortBack, hat ? BunLowBack : BunTop },
                        new[] { ShortSide, hat ? BunLowSide : BunTopSide },
                    };
                case 6: // rasée
                    return new[] { new[] { ShavedFront }, new[] { ShavedBack }, new[] { ShavedSide } };
                default: // courte
                    return new[] { new[] { ShortFront }, new[] { ShortBack }, new[] { ShortSide } };
            }
        }

        // ---------- Chapeaux : face, dos, profil ----------

        private static readonly string[][] Straw =
        {
            new[] { "", ".....yXXXXx.....", "....yXXXXXRx....", "....RRRRRRRR....", "..yyXXXXXXXXxx..", ".yXXXXXXXXXXXXx.", "..xxxxxxxxxxxx.." },
            new[] { "", ".....yXXXXx.....", "....yXXXXXXx....", "....RRRRRRRR....", "..yyXXXXXXXXxx..", ".yXXXXXXXXXXXXx.", "..xxxxxxxxxxxx.." },
            new[] { "", "....yXXXx.......", "...yXXXXXx......", "...RRRRRRR......", ".yyXXXXXXXXXxx..", "yXXXXXXXXXXXXXx.", ".xxxxxxxxxxxxx.." },
        };

        private static readonly string[][] Cap =
        {
            new[] { "", "", ".....yXXXXx.....", "....yXXWWXXx....", "...yXXXXXXXXx...", "...XXXXXXXXXx...", "..yyyyyyyyyyyy..", "...xxxxxxxxxx..." },
            new[] { "", "", ".....yXXXXx.....", "....yXXXXXXx....", "...yXXXXXXXXx...", "...XXXXWWXXXx...", "...xxxxxxxxxx..." },
            new[] { "", "", "....yXXXx.......", "...yXXXWXx......", "...XXXXXXXx.....", "...XXXXXXXXyyy..", "...xxxxxxxxxxx.." },
        };

        private static readonly string[][] Bob =
        {
            new[] { "", "", ".....yXXXXx.....", "....yXXXXXXx....", "....xxxxxxxx....", "..yyXXXXXXXXxx..", "..xxxxxxxxxxxx.." },
            new[] { "", "", ".....yXXXXx.....", "....yXXXXXXx....", "....xxxxxxxx....", "..yyXXXXXXXXxx..", "..xxxxxxxxxxxx.." },
            new[] { "", "", "....yXXXx.......", "...yXXXXXx......", "...xxxxxxx......", ".yyXXXXXXXXxx...", ".xxxxxxxxxxxx..." },
        };

        private static readonly string[][] Bandana =
        {
            new[] { "", "", "", "......yXXX......", "....yXXWXXXx....", "...yXWXXXXWXx...", "...xxxxxxxxxxXx.", ".............xX." },
            new[] { "", "", "", "......yXXX......", "....yXXWXXXx....", "...yXWXXXXWXx...", "...xxxxXXxxxx...", "......XxXx......", "......x..x......" },
            new[] { "", "", "", ".....yXXX.......", "....yXWXXXx.....", "...yXXXXWXXx....", "..Xxxxxxxxxx....", ".Xx.............", ".x.............." },
        };

        /// <summary>Gabarit d'un chapeau pour une vue (0 face, 1 dos, 2 profil), null sans chapeau.</summary>
        public static string[] Hat(int hat, int view)
        {
            switch (hat)
            {
                case 1: return Straw[view];
                case 2: return Cap[view];
                case 3: return Bob[view];
                case 4: return Bandana[view];
                default: return null;
            }
        }
    }
}
