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
    }
}
