using FishAndChill.Collection;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.Fishing
{
    /// <summary>
    /// Affichage provisoire du prototype de pêche (formes simples, vue de dessus).
    /// Tout est construit par le code au démarrage : il sera remplacé par du pixel art et de vrais prefabs.
    /// </summary>
    [RequireComponent(typeof(FishingController))]
    public sealed class FishingPrototypeView : MonoBehaviour
    {
        private static readonly Color SandColor = new Color(0.86f, 0.76f, 0.52f);
        private static readonly Color PlayerColor = new Color(0.93f, 0.42f, 0.30f);
        private static readonly Color BobberColor = new Color(0.95f, 0.20f, 0.20f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color LineColor = new Color(1f, 1f, 1f, 0.7f);
        private static readonly Color BarColor = new Color(0.08f, 0.10f, 0.14f, 0.9f);
        private static readonly Color ZoneColor = new Color(0.30f, 0.85f, 0.40f);
        private static readonly Color HitFlash = new Color(0.30f, 0.85f, 0.40f, 0.9f);
        private static readonly Color MissFlash = new Color(0.90f, 0.25f, 0.25f, 0.9f);

        [SerializeField, Tooltip("Le pêcheur : le fil part de sa position.")]
        private Transform _player;

        [SerializeField, Tooltip("Distance du lancer devant le joueur (min, max).")]
        private Vector2 _castDistance = new Vector2(2.5f, 4.5f);

        [SerializeField, Tooltip("Écart latéral maximal du lancer.")]
        private float _castSpread = 1.5f;

        [SerializeField, Tooltip("Le bouchon reste dans cette zone d'eau (coordonnées du monde).")]
        private Rect _waterArea = Rect.MinMaxRect(-8.5f, -1.1f, 8.5f, 4.5f);

        private FishingController _controller;
        private Sprite _square;
        private Sprite _circle;

        private Transform _bobber;
        private Transform _line;
        private SpriteRenderer _shadow;
        private Vector2 _castPoint;
        private Vector2 _shadowStart;

        private Text _header;
        private Text _message;
        private Text _qteCounter;
        private GameObject _qtePanel;
        private Image _barBackground;
        private RectTransform _zoneRect;
        private RectTransform _cursorRect;
        private float _flashTimer;

        private void Awake()
        {
            _controller = GetComponent<FishingController>();
            _square = CreateSquareSprite();
            _circle = CreateCircleSprite(64);
        }

        private void OnEnable()
        {
            _controller.StateChanged += OnStateChanged;
            _controller.QtePressed += OnQtePressed;
            _controller.FishCaught += OnFishCaught;
        }

        private void OnDisable()
        {
            _controller.StateChanged -= OnStateChanged;
            _controller.QtePressed -= OnQtePressed;
            _controller.FishCaught -= OnFishCaught;
        }

        private void Start()
        {
            if (Camera.main != null && _controller.Zone != null)
            {
                Camera.main.backgroundColor = _controller.Zone.WaterColor;
            }

            BuildWorld();
            BuildUi();
            OnStateChanged();
        }

        private void Update()
        {
            UpdateWorld();
            UpdateQte();
            _message.text = _controller.Message;
        }

        // ---------- Monde ----------

        private void BuildWorld()
        {
            CreateSprite("Berge", _square, SandColor, new Vector2(0f, -3.6f), new Vector2(40f, 4.2f), 0);

            if (_player == null)
            {
                _player = CreateSprite("Pêcheur", _square, PlayerColor, new Vector2(0f, -3f), new Vector2(0.6f, 0.6f), 10).transform;
            }
            else if (_player.TryGetComponent(out SpriteRenderer playerRenderer) && playerRenderer.sprite == null)
            {
                // Le joueur de la scène n'a pas encore de sprite : on lui donne la forme provisoire.
                playerRenderer.sprite = _square;
                playerRenderer.color = PlayerColor;
                playerRenderer.sortingOrder = 10;
            }

            _line = CreateSprite("Fil", _square, LineColor, _player.position, new Vector2(0.04f, 1f), 8).transform;
            _shadow = CreateSprite("Ombre du poisson", _circle, ShadowColor, Vector2.zero, new Vector2(0.8f, 0.4f), 5);
            _bobber = CreateSprite("Bouchon", _circle, BobberColor, Vector2.zero, Vector2.one * 0.3f, 9).transform;
        }

        private void UpdateWorld()
        {
            FishingState state = _controller.State;
            bool lineOut = state == FishingState.Waiting || state == FishingState.Bite || state == FishingState.Reeling;
            _bobber.gameObject.SetActive(lineOut);
            _line.gameObject.SetActive(lineOut);
            _shadow.gameObject.SetActive(lineOut);
            if (!lineOut)
            {
                return;
            }

            float time = Time.time;
            Vector2 bobber = _castPoint;
            Vector2 shadow = _castPoint;

            switch (state)
            {
                case FishingState.Waiting:
                    // Le poisson s'approche doucement du bouchon.
                    bobber += new Vector2(0f, Mathf.Sin(time * 2f) * 0.03f);
                    shadow = Vector2.Lerp(_shadowStart, _castPoint, Mathf.SmoothStep(0f, 1f, _controller.PhaseProgress));
                    shadow += new Vector2(Mathf.Sin(time * 3f) * 0.08f, 0f);
                    break;

                case FishingState.Bite:
                    // Le bouchon plonge et tremble.
                    bobber += new Vector2(Mathf.Sin(time * 60f) * 0.06f, -0.08f);
                    break;

                case FishingState.Reeling:
                    // Le poisson se débat au bout du fil.
                    shadow += new Vector2(Mathf.Sin(time * 9f) * 0.35f, Mathf.Cos(time * 7f) * 0.15f);
                    bobber = shadow;
                    break;
            }

            _bobber.position = bobber;
            _shadow.transform.position = shadow;
            _shadow.transform.right = (Vector3)(_castPoint - shadow).normalized + Vector3.right * 0.001f;
            PlaceLine(_player.position, bobber);
        }

        private void PlaceLine(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            _line.position = from + delta * 0.5f;
            _line.up = delta.normalized;
            _line.localScale = new Vector3(0.04f, delta.magnitude, 1f);
        }

        // ---------- Interface ----------

        private void BuildUi()
        {
            var canvasObject = new GameObject("Interface (prototype)", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform root = canvasObject.GetComponent<RectTransform>();

            _header = CreateText(root, "En-tête", font, 34, TextAnchor.UpperLeft);
            Anchor(_header.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(900f, 120f), new Vector2(0f, 1f));

            _message = CreateText(root, "Message", font, 40, TextAnchor.MiddleCenter);
            Anchor(_message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1400f, 130f), new Vector2(0.5f, 1f));

            _qtePanel = new GameObject("QTE", typeof(RectTransform));
            var panelRect = _qtePanel.GetComponent<RectTransform>();
            panelRect.SetParent(root, false);
            Anchor(panelRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 520f), new Vector2(900f, 56f), new Vector2(0.5f, 0f));

            _barBackground = CreateImage(panelRect, "Barre", BarColor);
            Stretch(_barBackground.rectTransform);

            Image zone = CreateImage(_barBackground.rectTransform, "Zone de réussite", ZoneColor);
            _zoneRect = zone.rectTransform;

            Image cursor = CreateImage(_barBackground.rectTransform, "Curseur", Color.white);
            _cursorRect = cursor.rectTransform;

            _qteCounter = CreateText(panelRect, "Compteur", font, 30, TextAnchor.LowerCenter);
            Anchor(_qteCounter.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 8f), new Vector2(900f, 50f), new Vector2(0.5f, 0f));
        }

        private void UpdateQte()
        {
            QteChallenge challenge = _controller.Challenge;
            bool visible = _controller.State == FishingState.Reeling && challenge != null;
            _qtePanel.SetActive(visible);
            if (!visible)
            {
                return;
            }

            TimingGauge gauge = challenge.Gauge;
            _zoneRect.anchorMin = new Vector2(gauge.ZoneMin, 0f);
            _zoneRect.anchorMax = new Vector2(gauge.ZoneMax, 1f);
            _zoneRect.offsetMin = Vector2.zero;
            _zoneRect.offsetMax = Vector2.zero;

            _cursorRect.anchorMin = new Vector2(gauge.Position, -0.25f);
            _cursorRect.anchorMax = new Vector2(gauge.Position, 1.25f);
            _cursorRect.sizeDelta = new Vector2(10f, 0f);
            _cursorRect.anchoredPosition = Vector2.zero;

            _qteCounter.text = $"Réussites {challenge.Hits}/{challenge.RequiredHits}     Ratés {challenge.Misses}/{challenge.MaxMisses}";

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f)
                {
                    _barBackground.color = BarColor;
                }
            }
        }

        private void RefreshHeader()
        {
            string zone = _controller.Zone != null ? _controller.Zone.DisplayName : "(aucune zone)";
            CatchLog log = _controller.CatchLog;
            _header.text = $"{zone}\nEspèces : {log.SpeciesCount}     Captures : {log.TotalCatches}";
        }

        // ---------- Événements ----------

        private void OnStateChanged()
        {
            if (_header == null)
            {
                return;
            }

            if (_controller.State == FishingState.Waiting)
            {
                // Lancer droit devant le pêcheur, avec un peu d'imprécision, sans sortir de l'eau.
                Vector2 origin = _player.position;
                Vector2 cast = origin + new Vector2(
                    Random.Range(-_castSpread, _castSpread),
                    Random.Range(_castDistance.x, _castDistance.y));
                _castPoint = new Vector2(
                    Mathf.Clamp(cast.x, _waterArea.xMin, _waterArea.xMax),
                    Mathf.Clamp(cast.y, _waterArea.yMin, _waterArea.yMax));
                _shadowStart = _castPoint + Random.insideUnitCircle.normalized * 2.5f;

                FishSpecies species = _controller.HookedSpecies;
                float size = species != null ? Mathf.InverseLerp(5f, 120f, _controller.HookedSizeCm) : 0.3f;
                _shadow.transform.localScale = new Vector3(Mathf.Lerp(0.5f, 2.2f, size), Mathf.Lerp(0.25f, 0.8f, size), 1f);
            }

            RefreshHeader();
        }

        private void OnQtePressed(QtePressResult result)
        {
            _barBackground.color = result == QtePressResult.Hit ? HitFlash : MissFlash;
            _flashTimer = 0.15f;
        }

        private void OnFishCaught(FishSpecies species, bool isNew)
        {
            RefreshHeader();
        }

        // ---------- Utilitaires ----------

        private SpriteRenderer CreateSprite(string objectName, Sprite sprite, Color color, Vector2 position, Vector2 scale, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Text CreateText(RectTransform parent, string objectName, Font font, int size, TextAnchor alignment)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            return text;
        }

        private static Image CreateImage(RectTransform parent, string objectName, Color color)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite CreateSquareSprite()
        {
            var texture = new Texture2D(4, 4) { filterMode = FilterMode.Point };
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private static Sprite CreateCircleSprite(int resolution)
        {
            var texture = new Texture2D(resolution, resolution) { filterMode = FilterMode.Bilinear };
            float radius = resolution * 0.5f;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    float alpha = Mathf.Clamp01(radius - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
        }
    }
}
