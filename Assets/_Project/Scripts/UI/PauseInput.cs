using UnityEngine.InputSystem;

namespace FishAndChill.UI
{
    /// <summary>Touches du menu pause : Échap ou Start (manette) l'ouvre et le ferme.</summary>
    public static class PauseInput
    {
        public static bool TogglePressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.startButton.wasPressedThisFrame;
        }

        /// <summary>Retour en arrière à la manette (B).</summary>
        public static bool BackPressedThisFrame()
        {
            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
        }

        /// <summary>Valider la ligne choisie : Espace, Entrée ou A.</summary>
        public static bool ConfirmPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        }
    }
}
