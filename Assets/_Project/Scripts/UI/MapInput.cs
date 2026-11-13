using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace FishAndChill.UI
{
    /// <summary>
    /// Touche de la carte : la LETTRE « M » selon la disposition du clavier
    /// (sur AZERTY, elle n'est pas à la même place que sur QWERTY), ou le bouton Select / Share de la manette.
    /// Échap ferme aussi la carte.
    /// </summary>
    public static class MapInput
    {
        public static bool TogglePressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                KeyControl mKey = keyboard.FindKeyOnCurrentKeyboardLayout("m") ?? keyboard.mKey;
                if (mKey != null && mKey.wasPressedThisFrame)
                {
                    return true;
                }
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.selectButton.wasPressedThisFrame;
        }

        public static bool ClosePressedThisFrame()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }
    }
}
