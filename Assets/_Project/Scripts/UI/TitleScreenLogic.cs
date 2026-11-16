namespace FishAndChill.UI
{
    /// <summary>
    /// Curseur de l'écran titre (logique pure, testée) : les 3 emplacements côte à côte (0 à 2, ←/→ en boucle) et le
    /// bouton « Quitter » dessous (3). ↓ descend sur Quitter, ↑ remonte sur le dernier emplacement choisi.
    /// </summary>
    public static class TitleScreenLogic
    {
        public const int Quit = 3;

        public static int Navigate(int selected, int lastSlot, int dx, int dy, int slotCount = 3)
        {
            if (selected >= slotCount)
            {
                return dy > 0 ? Clamp(lastSlot, slotCount) : selected;
            }

            if (dy < 0)
            {
                return slotCount;
            }

            return dx == 0 ? selected : PauseMenuLogic.Move(selected, dx, slotCount);
        }

        private static int Clamp(int slot, int count) => slot < 0 ? 0 : slot >= count ? count - 1 : slot;
    }
}
