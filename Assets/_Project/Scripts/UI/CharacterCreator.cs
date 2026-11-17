using System.Collections.Generic;
using FishAndChill.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Création du pêcheur, ouverte par l'écran titre sur un emplacement vide : prénom tapé au clavier, teint,
    /// coiffure, couleur des cheveux, chapeau, haut et couleurs, avec un grand aperçu qui marche et se retourne.
    /// Haut / Bas choisir une ligne, ← → changer, Entrée valider, Échap revenir aux parties.
    /// </summary>
    public sealed class CharacterCreator
    {
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Normal = new Color(0.90f, 0.93f, 0.96f);
        private static readonly Color Muted = new Color(0.64f, 0.70f, 0.76f);

        private const int NameRow = 0;
        private static readonly int RandomRow = CharacterOptions.Parts.Length + 1;
        private static readonly int StartRow = CharacterOptions.Parts.Length + 2;
        private static readonly int RowCount = CharacterOptions.Parts.Length + 3;

        private readonly GameObject _root;
        private readonly Image _preview;
        private readonly Text _name;
        private readonly Text _hint;
        private readonly List<Image> _rows = new List<Image>();
        private readonly List<Text> _values = new List<Text>();
        private readonly List<Image> _swatches = new List<Image>();
        private readonly List<char> _typed = new List<char>();
        private readonly System.Random _random = new System.Random();
        private CharacterLook _look = new CharacterLook();
        private CharacterSprites _sprites;
        private int _row;
        private float _time;

        /// <summary>Le joueur a validé : emplacement et apparence choisis.</summary>
        public System.Action<int, CharacterLook> Confirmed;

        /// <summary>Le joueur est revenu aux parties.</summary>
        public System.Action Cancelled;

        public bool IsOpen => _root.activeSelf;

        public int Slot { get; private set; }

        public CharacterCreator(RectTransform canvas, UiSkin skin)
        {
            _root = new GameObject("Création du pêcheur", typeof(RectTransform));
            var rootRect = _root.GetComponent<RectTransform>();
            rootRect.SetParent(canvas, false);
            UiFactory.Stretch(rootRect);

            Image dim = UiFactory.Plain(rootRect, "Voile", new Color(0.01f, 0.04f, 0.08f, 0.5f));
            UiFactory.Stretch(dim.rectTransform);

            Image panel = UiFactory.Sliced(rootRect, "Cadre", skin != null ? skin.Panel : null, skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            RectTransform panelRect = panel.rectTransform;
            UiFactory.Center(panelRect, new Vector2(0f, -10f), new Vector2(1240f, 880f));
            UiFactory.TitlePlaque(panelRect, skin, "Ton pêcheur", 38, new Vector2(0f, 438f), new Vector2(520f, 78f), out _);

            // Aperçu : le pêcheur en grand, dans une case creusée.
            Image box = UiFactory.Sliced(panelRect, "Aperçu", skin != null ? skin.WaterBackdrop : null, skin != null ? Color.white : new Color(0.2f, 0.5f, 0.6f));
            UiFactory.Center(box.rectTransform, new Vector2(-360f, 40f), new Vector2(400f, 560f));
            _preview = UiFactory.Plain(box.rectTransform, "Pêcheur", Color.white);
            _preview.preserveAspect = true;
            _preview.raycastTarget = false;
            UiFactory.Center(_preview.rectTransform, new Vector2(0f, -10f), new Vector2(16f * 20f, 24f * 20f));

            _name = UiFactory.Label(panelRect, "Prénom", 40, TextAnchor.MiddleCenter, Gold);
            UiFactory.Bold(_name);
            UiFactory.Center(_name.rectTransform, new Vector2(-360f, -290f), new Vector2(420f, 60f));

            // Lignes : prénom, chaque option, puis « Au hasard » et « C'est parti ! ».
            for (int i = 0; i < RowCount; i++)
            {
                float y = i < RandomRow ? 330f - i * 64f : 330f - i * 64f - 24f;
                Image row = UiFactory.Sliced(panelRect, $"Ligne {i}", skin != null ? skin.Slot : null, Color.white);
                UiFactory.Center(row.rectTransform, new Vector2(230f, y), new Vector2(640f, 56f));
                _rows.Add(row);

                bool button = i >= RandomRow;
                Text label = UiFactory.Label(row.rectTransform, "Nom", 28, button ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft, Normal);
                label.text = i == NameRow ? "Prénom" : i == RandomRow ? "Au hasard" : i == StartRow ? "C'est parti !" : CharacterOptions.Label(CharacterOptions.Parts[i - 1]);
                UiFactory.Center(label.rectTransform, button ? Vector2.zero : new Vector2(-150f, 0f), new Vector2(button ? 600f : 300f, 50f));
                if (button)
                {
                    UiFactory.Bold(label);
                    _values.Add(label);
                    _swatches.Add(null);
                    continue;
                }

                Text value = UiFactory.Label(row.rectTransform, "Valeur", 28, TextAnchor.MiddleCenter, Gold);
                UiFactory.Center(value.rectTransform, new Vector2(130f, 0f), new Vector2(340f, 50f));
                _values.Add(value);

                Image swatch = null;
                if (i > NameRow && IsColorPart(CharacterOptions.Parts[i - 1]))
                {
                    Image border = UiFactory.Plain(row.rectTransform, "Bord", new Color(0.10f, 0.08f, 0.08f));
                    UiFactory.Center(border.rectTransform, new Vector2(286f, 0f), new Vector2(34f, 34f));
                    swatch = UiFactory.Plain(border.rectTransform, "Couleur", Color.white);
                    UiFactory.Stretch(swatch.rectTransform, 4f);
                }

                _swatches.Add(swatch);
            }

            _hint = UiFactory.Label(panelRect, "Aide", 22, TextAnchor.MiddleCenter, Muted);
            UiFactory.Center(_hint.rectTransform, new Vector2(0f, -400f), new Vector2(1180f, 34f));
            _root.SetActive(false);
        }

        private static bool IsColorPart(CharacterPart part) =>
            part == CharacterPart.Skin || part == CharacterPart.HairColor || part == CharacterPart.TopColor || part == CharacterPart.Bottom;

        public void Open(int slot)
        {
            Slot = slot;
            _look = new CharacterLook();
            _row = NameRow;
            _time = 0f;
            _typed.Clear();
            _root.SetActive(true);
            if (Keyboard.current != null)
            {
                Keyboard.current.onTextInput += OnTextInput;
            }

            Rebuild();
            Refresh();
        }

        public void Close()
        {
            if (Keyboard.current != null)
            {
                Keyboard.current.onTextInput -= OnTextInput;
            }

            _root.SetActive(false);
        }

        /// <summary>Choisit une ligne et y applique un changement (utilisable par script pour les tests).</summary>
        public void Change(CharacterPart part, int delta)
        {
            CharacterOptions.Cycle(_look, part, delta);
            Rebuild();
            Refresh();
        }

        public void SetName(string name)
        {
            _look.Name = CharacterOptions.CleanName(name);
            Refresh();
        }

        private void OnTextInput(char c) => _typed.Add(c);

        public void Update(float deltaTime)
        {
            if (!IsOpen)
            {
                return;
            }

            _time += deltaTime;
            (int dx, int dy) = Navigation();
            if (dy != 0)
            {
                _row = (_row - dy + RowCount) % RowCount;
            }

            if (_row == NameRow)
            {
                foreach (char c in _typed)
                {
                    _look.Name = CharacterOptions.Type(_look.Name, c);
                }

                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.backspaceKey.wasPressedThisFrame)
                {
                    _look.Name = CharacterOptions.Backspace(_look.Name);
                }
            }

            _typed.Clear();
            if (dx != 0 && _row > NameRow && _row < RandomRow)
            {
                Change(CharacterOptions.Parts[_row - 1], dx);
            }

            if (IndexInput.ClosePressedThisFrame())
            {
                Close();
                Cancelled?.Invoke();
                return;
            }

            if (ConfirmPressed())
            {
                if (_row == StartRow)
                {
                    CharacterLook look = CharacterOptions.Sanitize(_look);
                    Close();
                    Confirmed?.Invoke(Slot, look);
                    return;
                }

                if (_row == RandomRow)
                {
                    CharacterOptions.Randomize(_look, _random);
                    Rebuild();
                }
                else if (_row == NameRow)
                {
                    _row++;
                }
                else
                {
                    Change(CharacterOptions.Parts[_row - 1], 1);
                }
            }

            AnimatePreview();
            Refresh();
        }

        /// <summary>Flèches et croix de la manette seulement : les lettres servent à taper le prénom.</summary>
        private static (int dx, int dy) Navigation()
        {
            int dx = 0;
            int dy = 0;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame) dx = -1;
                if (keyboard.rightArrowKey.wasPressedThisFrame) dx = 1;
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame && keyboard.shiftKey.isPressed) dy = 1;
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame && !keyboard.shiftKey.isPressed) dy = -1;
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

        /// <summary>Entrée ou A partout ; Espace aussi, sauf sur le prénom où il tape une espace.</summary>
        private bool ConfirmPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame
                || _row != NameRow && keyboard.spaceKey.wasPressedThisFrame))
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        }

        private void Rebuild()
        {
            if (_sprites != null)
            {
                foreach (Sprite[] frames in new[] { _sprites.Down, _sprites.Up, _sprites.Left, _sprites.Right })
                {
                    foreach (Sprite sprite in frames)
                    {
                        Object.Destroy(sprite.texture);
                        Object.Destroy(sprite);
                    }
                }
            }

            _sprites = CharacterSprites.Build(_look);
            AnimatePreview();
        }

        /// <summary>L'aperçu marche sur place et se tourne : face, droite, dos, gauche.</summary>
        private void AnimatePreview()
        {
            int turn = Mathf.FloorToInt(_time / 1.6f) % 4;
            Sprite[] frames = turn == 0 ? _sprites.Down : turn == 1 ? _sprites.Right : turn == 2 ? _sprites.Up : _sprites.Left;
            _preview.sprite = frames[1 + Mathf.FloorToInt(_time * 5f) % 2];
        }

        private void Refresh()
        {
            bool blink = Mathf.Repeat(_time, 1f) < 0.55f;
            _name.text = CharacterOptions.DisplayName(_look);
            for (int i = 0; i < RowCount; i++)
            {
                bool selected = i == _row;
                _rows[i].color = selected ? Color.white : new Color(0.62f, 0.67f, 0.74f, 0.75f);
                _rows[i].rectTransform.localScale = selected ? new Vector3(1.03f, 1.03f, 1f) : Vector3.one;
                if (i >= RandomRow)
                {
                    _values[i].color = selected ? Gold : Normal;
                    continue;
                }

                if (i == NameRow)
                {
                    string name = _look.Name ?? string.Empty;
                    _values[i].text = selected ? name + (blink ? "_" : " ") : name.Length > 0 ? name : CharacterOptions.DefaultName;
                    _values[i].color = name.Length > 0 || selected ? Gold : Muted;
                    continue;
                }

                CharacterPart part = CharacterOptions.Parts[i - 1];
                string text = CharacterOptions.ValueName(_look, part);
                _values[i].text = selected ? $"←  {text}  →" : text;
                _values[i].color = selected ? Gold : Normal;
                if (_swatches[i] != null)
                {
                    _swatches[i].color = SwatchColor(part);
                }
            }

            _hint.text = _row == NameRow
                ? "Tape ton prénom      Haut / Bas : choisir      Entrée : valider      Échap : retour"
                : "Haut / Bas : choisir      ← → : changer      Entrée : valider      Échap : retour";
        }

        private Color SwatchColor(CharacterPart part)
        {
            int value = CharacterOptions.Clamp(_look.Get(part), part);
            switch (part)
            {
                case CharacterPart.Skin: return CharacterOptions.SkinColors[value];
                case CharacterPart.HairColor: return CharacterOptions.HairColors[value];
                case CharacterPart.TopColor: return CharacterOptions.TopColors[value];
                default: return CharacterOptions.BottomColors[value];
            }
        }
    }
}
