using UnityEngine.InputSystem;

namespace FishAndChill.UI
{
    /// <summary>
    /// Touches de l'index des poissons : Tab (même place en AZERTY et en QWERTY, sous la main gauche à côté de ZQSD)
    /// ou le bouton Y / Triangle de la manette. Échap ou B / Rond referment. Flèches, ZQSD ou croix pour choisir.
    /// </summary>
    public static class IndexInput
    {
        public static bool TogglePressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonNorth.wasPressedThisFrame;
        }

        public static bool ClosePressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
        }

        /// <summary>Entrée, Espace ou A / Croix : valider (passer de la liste des zones à la grille des espèces).</summary>
        public static bool ConfirmPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        }

        /// <summary>Direction choisie cette image (-1, 0 ou 1 sur chaque axe, y vers le haut).</summary>
        public static (int dx, int dy) NavigationThisFrame()
        {
            int dx = 0;
            int dy = 0;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // ZQSD lus par position physique (W/A/S/D), comme pour marcher.
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) dx = -1;
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) dx = 1;
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) dy = 1;
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) dy = -1;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.dpad.left.wasPressedThisFrame) dx = -1;
                if (gamepad.dpad.right.wasPressedThisFrame) dx = 1;
                if (gamepad.dpad.up.wasPressedThisFrame) dy = 1;
                if (gamepad.dpad.down.wasPressedThisFrame) dy = -1;
            }

            return (dx, dy);
        }
    }
}
