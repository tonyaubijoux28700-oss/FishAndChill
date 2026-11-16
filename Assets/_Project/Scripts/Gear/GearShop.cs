using FishAndChill.Collection;

namespace FishAndChill.Gear
{
    public enum GearPurchase
    {
        Bought,
        AlreadyOwned,
        AlreadyMax,
        NotEnoughCoins,
        Unavailable,
    }

    /// <summary>Achat de matériel chez le marchand : on achète toujours le niveau suivant de la famille.</summary>
    public static class GearShop
    {
        /// <summary>Le niveau qu'on peut acheter dans cette famille (null si on a déjà le meilleur).</summary>
        public static GearLevel NextLevel(GearLine line, GearLoadout loadout)
        {
            if (line == null || loadout == null)
            {
                return null;
            }

            int next = loadout.Level(line.Category) + 1;
            return next <= line.MaxLevel ? line.At(next) : null;
        }

        /// <summary>Que donnerait un achat maintenant ? (sans rien acheter)</summary>
        public static GearPurchase Check(GearLine line, GearLoadout loadout, Wallet wallet)
        {
            if (line == null || loadout == null || wallet == null)
            {
                return GearPurchase.Unavailable;
            }

            GearLevel next = NextLevel(line, loadout);
            if (next == null)
            {
                return GearPurchase.AlreadyMax;
            }

            return wallet.Coins >= next.Price ? GearPurchase.Bought : GearPurchase.NotEnoughCoins;
        }

        /// <summary>
        /// Achète un objet précis de la famille (canne, hameçon) : dépense les pièces, l'ajoute à ce qu'on possède et
        /// l'équipe. Déjà possédé : il est simplement équipé.
        /// </summary>
        public static GearPurchase TryBuyItem(GearLine line, int index, GearLoadout loadout, Wallet wallet)
        {
            if (line == null || loadout == null || wallet == null || index < 0 || index > line.MaxLevel)
            {
                return GearPurchase.Unavailable;
            }

            if (loadout.Owns(line.Category, index))
            {
                loadout.Equip(line.Category, index);
                return GearPurchase.AlreadyOwned;
            }

            if (!wallet.TrySpend(line.At(index).Price))
            {
                return GearPurchase.NotEnoughCoins;
            }

            loadout.Own(line.Category, index);
            loadout.Equip(line.Category, index);
            return GearPurchase.Bought;
        }

        /// <summary>Achète le niveau suivant si possible : les pièces sont dépensées et le niveau monte.</summary>
        public static GearPurchase TryBuy(GearLine line, GearLoadout loadout, Wallet wallet)
        {
            GearPurchase check = Check(line, loadout, wallet);
            if (check != GearPurchase.Bought)
            {
                return check;
            }

            if (!wallet.TrySpend(NextLevel(line, loadout).Price))
            {
                return GearPurchase.NotEnoughCoins;
            }

            loadout.Upgrade(line.Category);
            return GearPurchase.Bought;
        }
    }
}
