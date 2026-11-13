using UnityEngine.InputSystem;

namespace FishAndChill.Fishing
{
    /// <summary>Bouton d'action unique du prototype : Espace, clic gauche ou bouton A/Croix de la manette.</summary>
    public static class FishingInput
    {
        public static bool ActionPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        }

        /// <summary>
        /// Place du sac choisie (0, 1, 2…) avec les touches 1 à 9 de la rangée du haut ou du pavé numérique,
        /// ou -1. La rangée du haut est lue par position : sur AZERTY elle marche sans Maj.
        /// </summary>
        public static int SlotPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return -1;
            }

            Key[] row = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
            Key[] pad = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };
            for (int i = 0; i < row.Length; i++)
            {
                if (keyboard[row[i]].wasPressedThisFrame || keyboard[pad[i]].wasPressedThisFrame)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
