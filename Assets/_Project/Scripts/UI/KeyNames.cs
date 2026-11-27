using UnityEngine.InputSystem;

namespace FishAndChill.UI
{
    /// <summary>
    /// Noms des touches tels qu'ils sont gravés sur le clavier du joueur : le jeu lit les touches par position (ZQSD en
    /// AZERTY = WASD en QWERTY), les aides affichent donc la lettre de son clavier plutôt qu'une lettre écrite en dur.
    /// </summary>
    public static class KeyNames
    {
        /// <summary>La lettre de la touche physique <paramref name="key"/>, ou <paramref name="fallback"/> sans clavier.</summary>
        public static string Of(Key key, string fallback)
        {
            Keyboard keyboard = Keyboard.current;
            string name = keyboard != null ? keyboard[key].displayName : null;
            return string.IsNullOrEmpty(name) ? fallback : name.ToUpperInvariant();
        }

        /// <summary>Les quatre touches de déplacement : « ZQSD » en AZERTY, « WASD » en QWERTY.</summary>
        public static string Move => Of(Key.W, "Z") + Of(Key.A, "Q") + Of(Key.S, "S") + Of(Key.D, "D");
    }
}
