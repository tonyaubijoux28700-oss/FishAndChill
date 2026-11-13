using UnityEngine;
using UnityEngine.InputSystem;

namespace FishAndChill.Player
{
    /// <summary>
    /// Direction de déplacement : touches ZQSD, flèches ou stick gauche.
    /// Les touches sont lues par position physique (W A S D d'un clavier QWERTY),
    /// ce qui correspond à Z Q S D sur un clavier AZERTY.
    /// </summary>
    public static class MoveInput
    {
        public static Vector2 Read()
        {
            Vector2 direction = Vector2.zero;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard[Key.W].isPressed || keyboard[Key.UpArrow].isPressed) direction.y += 1f;
                if (keyboard[Key.S].isPressed || keyboard[Key.DownArrow].isPressed) direction.y -= 1f;
                if (keyboard[Key.A].isPressed || keyboard[Key.LeftArrow].isPressed) direction.x -= 1f;
                if (keyboard[Key.D].isPressed || keyboard[Key.RightArrow].isPressed) direction.x += 1f;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                direction += gamepad.leftStick.ReadValue();
            }

            return Vector2.ClampMagnitude(direction, 1f);
        }
    }
}
