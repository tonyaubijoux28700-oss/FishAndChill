using System.Collections;
using FishAndChill.Player;
using FishAndChill.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.World
{
    /// <summary>
    /// La maison du joueur : pièce de 9×7 cases (lit, tapis, table, coffre). Devant le lit, E fait dormir jusqu'à 7 h
    /// (le soir et la nuit seulement, voir <see cref="SleepRules"/>) : fondu au noir, l'heure passe au matin.
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
                    _prompt.text = SleepRules.CanSleep(_cycle.Hour) ? "E : dormir jusqu'au matin" : "Pas encore sommeil… (on se couche après 18h)";
                }
            }

            if (atBed && _cycle != null && SleepRules.CanSleep(_cycle.Hour) && InteractInput.PressedThisFrame())
            {
                StartCoroutine(Sleep());
            }

            if (_messageTimer > 0f)
            {
                _messageTimer -= Time.unscaledDeltaTime;
                _message.gameObject.SetActive(_messageTimer > 0f);
            }
        }

        /// <summary>Dort jusqu'au matin : fondu au noir, l'heure passe à 7 h, fondu retour.</summary>
        public IEnumerator Sleep()
        {
            _sleeping = true;
            PlayerAtBed = false;
            Player.Locked = true;
            yield return Fade(0f, 1f, 0.8f);
            ShowMessage("Bonne nuit…", 1.4f);
            yield return new WaitForSecondsRealtime(1.2f);
            if (_cycle != null)
            {
                _cycle.SetHour(SleepRules.WakeHour);
            }

            yield return Fade(1f, 0f, 0.9f);
            Player.Locked = false;
            _sleeping = false;
            ShowMessage("Une nouvelle journée commence !", 2.6f);
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
        }
    }
}
