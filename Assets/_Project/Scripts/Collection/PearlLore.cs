using UnityEngine;

namespace FishAndChill.Collection
{
    /// <summary>
    /// Les 9 perles du collier de Chiyo : nom, couleur, et le souvenir qu'elle raconte en l'enfilant (une par zone, clé =
    /// identifiant de la zone).
    /// </summary>
    public static class PearlLore
    {
        private sealed class Pearl
        {
            public string Name;
            public Color Color;
            public string Memory;
        }

        private static Pearl Of(string zoneId)
        {
            switch (zoneId)
            {
                case "IleMahana": return new Pearl { Name = "la perle noire de Mahana", Color = new Color(0.22f, 0.26f, 0.30f),
                    Memory = "Mahana... Je l'ai vue sortir de la mer, cette île. Les vieux disaient qu'on l'avait construite. Moi, je dis que la mer l'a permis." };
                case "LacAnnecy": return new Pearl { Name = "la perle d'eau douce du lac", Color = new Color(0.96f, 0.92f, 0.80f),
                    Memory = "Un lac entre les montagnes, l'eau si claire qu'on voyait ses pieds à dix mètres. J'y ai appris que l'eau douce aussi garde des secrets." };
                case "FleuveAmazone": return new Pearl { Name = "la perle abricot du fleuve", Color = new Color(0.98f, 0.74f, 0.52f),
                    Memory = "Le grand fleuve. J'y ai suivi un dauphin rose pendant toute une journée. Il riait de moi, je crois." };
                case "BaieJaponaise": return new Pearl { Name = "la perle akoya d'Izu", Color = new Color(0.98f, 0.90f, 0.90f),
                    Memory = "Celle-là, c'est la première. Ma mère me l'a donnée le jour de ma première plongée. J'avais douze ans et je pleurais de froid." };
                case "GrandeBarriere": return new Pearl { Name = "la perle dorée du récif", Color = new Color(0.96f, 0.80f, 0.36f),
                    Memory = "Le récif, des couleurs partout, comme un marché sous l'eau. J'ai failli ne jamais remonter tellement c'était beau." };
                case "Maldives": return new Pearl { Name = "la perle rose de conque", Color = new Color(0.98f, 0.62f, 0.66f),
                    Memory = "Une perle de conque. Ça ne se cultive pas, ça. Ça se mérite. Une vieille pêcheuse me l'a offerte contre une chanson." };
                case "CoteDesSquelettes": return new Pearl { Name = "la perle d'argent de la côte", Color = new Color(0.80f, 0.84f, 0.88f),
                    Memory = "La côte des naufrages. Brouillard, vent, os de baleines sur le sable. Même là, la mer était généreuse." };
                case "FjordsNorvege": return new Pearl { Name = "la perle bleu-gris du fjord", Color = new Color(0.62f, 0.70f, 0.80f),
                    Memory = "Le fjord, l'eau si froide qu'elle brûle. J'y ai plongé une seule fois. Une fois suffit, crois-moi." };
                case "BaieHaLong": return new Pearl { Name = "la perle lavande de la baie", Color = new Color(0.80f, 0.70f, 0.92f),
                    Memory = "Ha Long... Hm. Celle-là, je t'en parlerai une autre fois. Il y a une grotte, là-bas. Pas maintenant." };
                default: return new Pearl { Name = "une perle", Color = new Color(0.96f, 0.94f, 0.90f), Memory = "Hm. Je ne me souviens plus d'où elle vient." };
            }
        }

        /// <summary>Nombre de perles du collier (une par zone).</summary>
        public const int Count = 9;

        public static string NameOf(string zoneId) => Of(zoneId).Name;

        public static Color ColorOf(string zoneId) => Of(zoneId).Color;

        public static string MemoryOf(string zoneId) => Of(zoneId).Memory;
    }
}
