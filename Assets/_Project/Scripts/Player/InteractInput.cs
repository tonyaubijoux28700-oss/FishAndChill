using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace FishAndChill.Player
{
    /// <summary>
    /// Touche d'interaction : la LETTRE « E » selon la disposition du clavier, ou le bouton X / Carré de la manette.
    /// </summary>
    public static class InteractInput
    {
        public static bool PressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                KeyControl eKey = keyboard.FindKeyOnCurrentKeyboardLayout("e") ?? keyboard.eKey;
                if (eKey != null && eKey.wasPressedThisFrame)
                {
                    return true;
                }
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonWest.wasPressedThisFrame;
        }
    }
}
