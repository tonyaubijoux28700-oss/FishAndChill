using FishAndChill.Player;
using FishAndChill.Villagers;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Parler aux habitants : près de l'un d'eux (dehors, ou chez lui quand on est dans sa maison), une invite
    /// « E : parler à … » ; E ouvre une boîte de dialogue en bas de l'écran (cadre de la charte, bandeau à son nom)
    /// avec ses répliques (<see cref="Villager.Talk"/>). E ou Espace : suite, Échap : fermer. Le joueur ne bouge pas
    /// pendant la conversation, et le sac ne s'ouvre pas tant qu'un habitant est à portée (<see cref="Busy"/>).
    /// </summary>
    public sealed class DialogueBox : MonoBehaviour
    {
        [SerializeField] private UiSkin _skin;
        [SerializeField] private PlayerMovement _player;

        private Text _prompt;
        private GameObject _box;
        private Text _name;
        private Text _line;
        private Text _hint;
        private Villager _speaker;
        private Villager _nearest;
        private string[] _lines;
        private int _index;
        private int _openedFrame = -1;

        /// <summary>Une conversation est ouverte, ou un habitant est à portée (E lui est réservé).</summary>
        public static bool Busy { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Busy = false;

        public bool IsOpen => _box != null && _box.activeSelf;

        private void Start()
        {
            BuildUi();
        }

        private void OnDisable() => Busy = false;

        private void Update()
        {
            if (IsOpen)
            {
                Busy = true;
                _prompt.gameObject.SetActive(false);
                if (Time.frameCount == _openedFrame) return;
                if (PauseInput.TogglePressedThisFrame() || PauseInput.BackPressedThisFrame()) Close();
                else if (InteractInput.PressedThisFrame() || PauseInput.ConfirmPressedThisFrame()) Next();
                return;
            }

            _nearest = FindNearest();
            Busy = _nearest != null;
            _prompt.gameObject.SetActive(_nearest != null);
            if (_nearest == null) return;
            _prompt.text = $"E : parler à {_nearest.DisplayName}";
            if (InteractInput.PressedThisFrame()) Open(_nearest);
        }

        private Villager FindNearest()
        {
            if (_player == null || _player.Locked || ElevatorMenu.Busy) return null;
            Vector2 feet = _player.transform.position;
            Villager best = null;
            float bestDistance = float.MaxValue;
            foreach (Villager villager in Villager.Everyone)
            {
                if (villager.CanTalk(feet, out float distance) && distance < bestDistance)
                {
                    best = villager;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>Ouvre la conversation avec un habitant (public pour les tests).</summary>
        public void Open(Villager villager)
        {
            if (villager == null) return;
            _speaker = villager;
            _lines = villager.Talk(_player != null ? (Vector2)_player.transform.position : Vector2.zero);
            _index = 0;
            _openedFrame = Time.frameCount;
            _name.text = villager.DisplayName;
            _box.SetActive(true);
            if (_player != null) _player.Locked = true;
            Busy = true;
            ShowLine();
        }

        private void Next()
        {
            _index++;
            if (_index >= _lines.Length) Close();
            else ShowLine();
        }

        public void Close()
        {
            _box.SetActive(false);
            if (_player != null) _player.Locked = false;
            if (_speaker != null) _speaker.EndTalk();
            _speaker = null;
        }

        private void ShowLine()
        {
            _line.text = _lines[_index];
            _hint.text = _index < _lines.Length - 1 ? "E / Espace : suite   —   Échap : fermer" : "E / Espace : au revoir";
        }

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Dialogues - Interface", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 14;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            _prompt = UiFactory.Label(root, "Invite", 30, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.60f));
            UiFactory.Center(_prompt.rectTransform, new Vector2(0f, -395f), new Vector2(1200f, 50f));
            _prompt.gameObject.SetActive(false);

            Image box = UiFactory.Sliced(root, "Dialogue", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f, 0.95f));
            _box = box.gameObject;
            UiFactory.Place(box.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-90f, 40f), new Vector2(1260f, 250f));
            _name = UiFactory.TitlePlaque(box.rectTransform, _skin, string.Empty, 30, new Vector2(-410f, 125f), new Vector2(360f, 64f), out _);

            _line = UiFactory.Label(box.rectTransform, "Réplique", 30, TextAnchor.UpperLeft, new Color(0.96f, 0.94f, 0.88f));
            _line.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(_line.rectTransform, new Vector2(0f, 6f), new Vector2(1130f, 130f));

            _hint = UiFactory.Label(box.rectTransform, "Aide", 22, TextAnchor.MiddleRight, new Color(0.70f, 0.76f, 0.82f));
            UiFactory.Center(_hint.rectTransform, new Vector2(0f, -92f), new Vector2(1130f, 30f));
            _box.SetActive(false);
        }
    }
}
