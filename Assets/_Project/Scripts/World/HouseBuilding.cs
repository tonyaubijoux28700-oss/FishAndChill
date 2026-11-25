using System.Collections;
using FishAndChill.Player;
using FishAndChill.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.World
{
    /// <summary>
    /// La maison du joueur : pièce de 9×7 cases (lit, tapis, table, coffre). Devant le lit, E fait dormir jusqu'à 7 h
    /// la nuit, ou faire la sieste jusqu'à 18 h le jour (<see cref="SleepRules"/>) : la pièce s'assombrit, des « Z » montent
    /// du lit pendant que l'horloge file, puis la partie est sauvegardée.
    /// Entrée et sortie : voir <see cref="InteriorBuilding"/>.
    /// </summary>
    public sealed class HouseBuilding : InteriorBuilding
    {
        public const float InteriorWidth = 9f;
        public const float InteriorHeight = 7f;

        [SerializeField] private DayNightCycle _cycle;
        [SerializeField, Min(0.5f)] private float _bedRadius = 1.1f;

        private Canvas _canvas;
        private Image _fade;
        private Text _prompt;
        private Text _message;
        private float _messageTimer;
        private bool _sleeping;
        private RectTransform _sleeper;
        private readonly Text[] _snores = new Text[3];
        private Text _saved;
        private float _savedTimer;
        private SpriteRenderer _blanket;

        /// <summary>Le joueur est devant son lit (E sert alors à dormir, pas à ouvrir le sac).</summary>
        public static bool PlayerAtBed { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBed() => PlayerAtBed = false;

        public override Vector2 InteriorSize => new Vector2(InteriorWidth, InteriorHeight);

        protected override Rect FloorArea => new Rect(0.3f, 0.15f, InteriorWidth - 0.6f, 4.4f);

        /// <summary>Lit, tête contre le mur (relatif à la pièce, en cases).</summary>
        public static readonly Rect Bed = new Rect(0.5f, 2.3f, 2f, 2.4f);

        /// <summary>Table ronde à droite.</summary>
        public static readonly Rect Table = new Rect(6.5f, 2.8f, 1.8f, 1.1f);

        /// <summary>Coffre contre le mur du fond.</summary>
        public static readonly Rect Chest = new Rect(2.85f, 3.9f, 1.3f, 0.9f);

        /// <summary>Là où l'on se tient pour se coucher : au pied du lit.</summary>
        public Vector2 BedFront => InteriorOrigin + new Vector2(Bed.center.x, Bed.yMin - 0.5f);

        protected override void SetupInterior(WorldMap map)
        {
            foreach (Rect r in new[] { Bed, Table, Chest })
            {
                map.AddObstacle(new Rect(InteriorOrigin + r.position, r.size));
            }
        }

        protected override void Start()
        {
            base.Start();
            if (_cycle == null)
            {
                _cycle = FindAnyObjectByType<DayNightCycle>();
            }

            BuildUi();
        }

        protected override void Update()
        {
            base.Update();
            bool atBed = IsPlayerInside && !_sleeping && Player != null && !Player.Locked
                         && Vector2.Distance(Player.transform.position, BedFront) <= _bedRadius;
            PlayerAtBed = atBed;
            if (_prompt != null)
            {
                _prompt.gameObject.SetActive(atBed);
                if (atBed && _cycle != null)
                {
                    _prompt.text = SleepRules.IsNight(_cycle.Hour) ? "E : dormir jusqu'au matin" : "E : faire la sieste jusqu'au soir";
                }
            }

            if (atBed && _cycle != null && InteractInput.PressedThisFrame())
            {
                StartCoroutine(Sleep());
            }

            if (_messageTimer > 0f)
            {
                _messageTimer -= Time.unscaledDeltaTime;
                _message.gameObject.SetActive(_messageTimer > 0f);
            }

            if (_savedTimer > 0f)
            {
                _savedTimer -= Time.unscaledDeltaTime;
                _saved.gameObject.SetActive(_savedTimer > 0f);
            }
        }

        /// <summary>
        /// Se couche : la pièce s'assombrit, le pêcheur se glisse sous la couette, des « Z » montent au-dessus de
        /// sa tête pendant que l'horloge file jusqu'au réveil (7 h, ou 18 h après une sieste), puis la partie est
        /// sauvegardée.
        /// </summary>
        public IEnumerator Sleep()
        {
            _sleeping = true;
            PlayerAtBed = false;
            Player.Locked = true;
            float bedHour = _cycle != null ? _cycle.Hour : SleepRules.WakeHour;
            bool night = SleepRules.IsNight(bedHour);
            yield return Fade(0f, 1f, 0.6f);

            // Au lit : la tête sur l'oreiller, la couette jusqu'à la taille.
            Player.Teleport(InteriorOrigin + new Vector2(Bed.center.x, Bed.yMin + 0.75f), Vector2.down);
            ShowBlanket(true);
            yield return Fade(1f, 0.55f, 0.4f);
            _sleeper.gameObject.SetActive(true);
            const float clockSeconds = 2.8f;
            for (float t = 0f; t < clockSeconds; t += Time.unscaledDeltaTime)
            {
                if (_cycle != null)
                {
                    _cycle.SetHour(SleepRules.HourDuring(bedHour, t / clockSeconds));
                }

                AnimateSnores(t);
                yield return null;
            }

            yield return Fade(0.55f, 1f, 0.5f);
            _sleeper.gameObject.SetActive(false);
            ShowBlanket(false);
            if (_cycle != null)
            {
                _cycle.SetHour(SleepRules.WakeFor(bedHour));
            }

            Player.Teleport(BedFront, Vector2.down);
            var save = FindAnyObjectByType<Save.SaveManager>();
            if (save != null)
            {
                save.Save();
            }

            yield return new WaitForSecondsRealtime(0.3f);
            yield return Fade(1f, 0f, 0.9f);
            Player.Locked = false;
            _sleeping = false;
            ShowMessage(night ? "Une nouvelle journée commence !" : "Bonne sieste, le soir tombe !", 2.6f);
            _saved.gameObject.SetActive(save != null);
            _savedTimer = save != null ? 2.6f : 0f;
        }

        /// <summary>
        /// La couette à carreaux rouges et blancs (comme celle du lit), posée sur le dormeur jusqu'à la taille ; créée
        /// en jeu, au-dessus du pêcheur.
        /// </summary>
        private void ShowBlanket(bool show)
        {
            if (_blanket == null)
            {
                const int w = 55, h = 37, square = 9;
                var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                Color red = new Color(0.80f, 0.20f, 0.20f), white = new Color(0.96f, 0.94f, 0.90f), fold = new Color(0.98f, 0.97f, 0.95f);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color c = y >= h - 6 ? fold : ((x / square + y / square) % 2 == 0 ? red : white);
                        float shade = 1f - 0.18f * Mathf.Max(0f, 1f - Mathf.Min(x, w - 1 - x) / 4f) - (y == h - 7 ? 0.25f : 0f) - (y < 2 ? 0.2f : 0f);
                        texture.SetPixel(x, y, new Color(c.r * shade, c.g * shade, c.b * shade, 1f));
                    }
                }

                texture.Apply();
                var go = new GameObject("Couette du dormeur", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                _blanket = go.GetComponent<SpriteRenderer>();
                _blanket.sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 32f);
                go.transform.position = InteriorOrigin + new Vector2(Bed.center.x, Bed.yMin + 0.15f);
            }

            if (show)
            {
                foreach (SpriteRenderer sprite in Player.GetComponentsInChildren<SpriteRenderer>())
                {
                    _blanket.sortingLayerID = sprite.sortingLayerID;
                    _blanket.sortingOrder = Mathf.Max(_blanket.sortingOrder, sprite.sortingOrder + 1);
                }
            }

            _blanket.gameObject.SetActive(show);
        }

        /// <summary>Trois « Z » de plus en plus grands montent en boucle au-dessus de l'oreiller.</summary>
        private void AnimateSnores(float time)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector2 pillow = InteriorOrigin + new Vector2(Bed.center.x + 0.3f, Bed.yMin + 2f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_canvas.transform, cam.WorldToScreenPoint(pillow), null, out Vector2 at);
            for (int i = 0; i < _snores.Length; i++)
            {
                float phase = Mathf.Repeat(time / 1.5f + i / 3f, 1f);
                _snores[i].rectTransform.anchoredPosition = at + new Vector2(20f + phase * 60f + Mathf.Sin(phase * 9f) * 8f, phase * 150f);
                _snores[i].fontSize = UiFactory.Size(26 + Mathf.RoundToInt(phase * 30f));
                _snores[i].color = new Color(1f, 1f, 1f, Mathf.Sin(phase * Mathf.PI));
            }
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _fade.color = new Color(0f, 0f, 0.02f, Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }

            _fade.color = new Color(0f, 0f, 0.02f, to);
        }

        private void ShowMessage(string text, float seconds)
        {
            _message.text = text;
            _messageTimer = seconds;
            _message.gameObject.SetActive(true);
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("Maison - Interface", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 25;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            _fade = UiFactory.Plain(root, "Fondu", new Color(0f, 0f, 0.02f, 0f));
            _fade.raycastTarget = false;
            UiFactory.Stretch(_fade.rectTransform);

            _prompt = UiFactory.Label(root, "Aide du lit", 30, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.60f));
            UiFactory.Center(_prompt.rectTransform, new Vector2(0f, -395f), new Vector2(1200f, 50f));
            _prompt.gameObject.SetActive(false);

            _message = UiFactory.Label(root, "Message", 44, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Bold(_message);
            UiFactory.Center(_message.rectTransform, new Vector2(0f, 120f), new Vector2(1400f, 80f));
            _message.gameObject.SetActive(false);

            _saved = UiFactory.Label(root, "Sauvegarde", 30, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.60f));
            UiFactory.Center(_saved.rectTransform, new Vector2(0f, 60f), new Vector2(1000f, 50f));
            _saved.text = "Partie sauvegardée";
            _saved.gameObject.SetActive(false);

            // Les « Z » du dormeur, au-dessus du fondu.
            _sleeper = new GameObject("Dormeur", typeof(RectTransform)).GetComponent<RectTransform>();
            _sleeper.SetParent(root, false);
            UiFactory.Stretch(_sleeper);
            for (int i = 0; i < _snores.Length; i++)
            {
                _snores[i] = UiFactory.Label(_sleeper, $"Z {i + 1}", 30, TextAnchor.MiddleCenter, Color.white);
                UiFactory.Bold(_snores[i]);
                _snores[i].text = "Z";
                UiFactory.Center(_snores[i].rectTransform, Vector2.zero, new Vector2(80f, 80f));
            }

            _sleeper.gameObject.SetActive(false);
        }
    }
}
