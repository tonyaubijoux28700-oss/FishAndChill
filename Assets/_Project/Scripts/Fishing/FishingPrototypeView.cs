using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Player;
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
        private static readonly Color PlayerColor = new Color(0.93f, 0.42f, 0.30f);
        private static readonly Color BarColor = new Color(0.08f, 0.10f, 0.14f, 0.9f);
        private static readonly Color HitFlash = new Color(0.30f, 0.85f, 0.40f, 0.9f);
        private static readonly Color MissFlash = new Color(0.90f, 0.25f, 0.25f, 0.9f);

        [SerializeField, Tooltip("Le pêcheur : le fil part de sa position.")]
        private Transform _player;

        [SerializeField, Tooltip("Charte pixel art de l'interface (carte de prise).")]
        private UI.UiSkin _skin;

        private const string ShopHint = "E au comptoir : parler au marchand — paillasson : sortir";
        private const string AquariumHint = "Paillasson : sortir";

        private FishingController _controller;
        private PlayerMovement _playerMovement;
        private CatchCard _catchCard;
        private BottleCard _bottleCard;
        private FragmentCard _fragmentCard;
        private LegendVictoryCard _victoryCard;
        private bool _cardWasBagFull;
        private Sprite _square;
        private FishShadowSprites _fishShadows;
        private int _fishSize = 1;
        private BodyShape _hookedShape = BodyShape.Fish;
        private Vector2 _fishAnchor;
        private Vector2 _fishPosition;
        private World.CameraFollow _cameraFollow;
        private Vector2 _fishHeading = Vector2.right;
        private float _tailTimer;
        private int _tailFrame = 1;
        private Sprite _bobberSprite;

        private Transform _bobber;
        private SpriteRenderer _shadow;
        private Vector2 _castPoint;
        private Vector2 _shadowStart;

        private Text _header;
        private Text _message;
        private GameObject _qtePanel;

        private void Awake()
        {
            _controller = GetComponent<FishingController>();
            _square = CreateSquareSprite();
            // Silhouettes du poisson sous l'eau : 18, 26 et 36 pixels de long.
            _fishShadows = new FishShadowSprites(18, 26, 36, 72);   // la dernière taille : les légendes
            _bobberSprite = CreateBobberSprite();
        }

        private void OnEnable()
        {
            _controller.StateChanged += OnStateChanged;
            _controller.ZoneChanged += RefreshHeaderIfReady;
            if (_controller.Forecast != null) _controller.Forecast.Changed += OnWeatherChanged;
            _controller.QtePressed += OnQtePressed;
            _controller.Nibbled += OnNibbled;
            _controller.FishCaught += OnFishCaught;
            _controller.BottleFound += OnBottleFound;
            _controller.FragmentFound += OnFragmentFound;
            _controller.LegendPhaseStarted += OnLegendPhaseStarted;
            _controller.LegendBeaten += OnLegendBeaten;
        }

        private void OnDisable()
        {
            _controller.StateChanged -= OnStateChanged;
            _controller.ZoneChanged -= RefreshHeaderIfReady;
            if (_controller.Forecast != null) _controller.Forecast.Changed -= OnWeatherChanged;
            _controller.QtePressed -= OnQtePressed;
            _controller.Nibbled -= OnNibbled;
            _controller.FishCaught -= OnFishCaught;
            _controller.BottleFound -= OnBottleFound;
            _controller.FragmentFound -= OnFragmentFound;
            _controller.LegendPhaseStarted -= OnLegendPhaseStarted;
            _controller.LegendBeaten -= OnLegendBeaten;
        }

        private void Start()
        {
            // L'en-tête suit l'aquarium (mis à jour au dépôt, même sans changement d'état de la pêche).
            if (_controller.Progress != null)
            {
                _controller.Progress.EnsureCreated();
                _controller.Progress.Changed += RefreshHeaderIfReady;
            }

            if (Camera.main != null && _controller.Zone != null)
            {
                Camera.main.backgroundColor = _controller.Zone.WaterColor;
            }

            BuildWorld();
            BuildUi();
            OnStateChanged();
        }

        private int _shownClock = -1;

        private void Update()
        {
            // Horloge de l'en-tête : mise à jour toutes les 10 minutes du jeu.
            if (_header != null && _controller.Cycle != null)
            {
                int clock = Mathf.FloorToInt(_controller.Cycle.Hour * 6f);
                if (clock != _shownClock)
                {
                    _shownClock = clock;
                    RefreshHeader();
                }
            }

            UpdateWorld();
            UpdateQte();
            UpdateLegendFight();
            _catchCard.Animate(Time.deltaTime);
            _bottleCard.Animate(Time.deltaTime);
            _fragmentCard.Animate(Time.deltaTime);
            _victoryCard.Animate(Time.deltaTime);

            // La carte de prise remplace le message texte ; dans la boutique, aide propre à la boutique
            // (et rien quand le menu du marchand est ouvert).
            // Pendant le combat, la jauge (et son bandeau « Ferre-le ! ») remplace le message.
            if (_catchCard.IsVisible || _bottleCard.IsVisible || _fragmentCard.IsVisible || _victoryCard.IsVisible || (_playerMovement != null && _playerMovement.Locked) || _controller.State == FishingState.Reeling)
            {
                _message.text = string.Empty;
            }
            else if (World.InteriorBuilding.Occupied is World.ShopBuilding)
            {
                _message.text = ShopHint;
            }
            else if (World.InteriorBuilding.Occupied is World.AquariumBuilding aquarium)
            {
                // Le paillasson n'existe qu'au rez-de-chaussée ; aux étages, on repart par l'ascenseur.
                _message.text = aquarium.CurrentFloor == 0 ? AquariumHint : string.Empty;
            }
            else if (World.InteriorBuilding.Occupied != null)
            {
                // Maison, laboratoire… : pas de pêche ici, chaque pièce affiche sa propre aide (lit, bureau).
                _message.text = string.Empty;
            }
            else
            {
                _message.text = _controller.Message;
            }
        }

        // ---------- Monde ----------
        //
        // Canne et fil dessinés pixel par pixel (PixelPolyline). Le lancer : la canne arme vers l'arrière, fouette vers
        // l'avant, le bouchon vole en arc puis fait « plouf ». L'attente : le bouchon dodeline, tressaille à chaque
        // titillement. La touche : le bouchon coule sous l'eau. Le combat : le fil tendu suit le poisson qui se débat.

        private static readonly Color RodCork = new Color(0.80f, 0.62f, 0.38f);
        private static readonly Color RodBlank = new Color(0.22f, 0.30f, 0.44f);
        private static readonly Color RodTip = new Color(0.85f, 0.25f, 0.22f);
        private static readonly Color LineTint = new Color(0.96f, 0.97f, 1f, 0.9f);

        private PixelPolyline _rod;
        private PixelPolyline _lineDots;
        private SpriteRenderer _bobberRenderer;
        private readonly SpriteRenderer[] _ripples = new SpriteRenderer[4];
        private readonly float[] _rippleAge = new float[4];
        private readonly float[] _rippleSize = new float[4];
        private Sprite _rippleSprite;
        private float _nibbleTimer;
        private float _biteAge;
        private float _splashTimer;
        private FishingState _lastState;

        private void BuildWorld()
        {
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

            _playerMovement = _player.GetComponent<PlayerMovement>();

            _rod = new PixelPolyline(transform, "Canne", 11);
            _lineDots = new PixelPolyline(transform, "Fil", 11);
            _shadow = CreateSprite("Poisson sous l'eau", _fishShadows.Get(1, Vector2.right, 1), Color.white, Vector2.zero, Vector2.one, 5);
            _bobberRenderer = CreateSprite("Bouchon", _bobberSprite, Color.white, Vector2.zero, Vector2.one, 9);
            _bobber = _bobberRenderer.transform;

            _rippleSprite = CreateRippleSprite();
            for (int i = 0; i < _ripples.Length; i++)
            {
                _ripples[i] = CreateSprite("Rond dans l'eau", _rippleSprite, Color.white, Vector2.zero, Vector2.one, 6);
                _ripples[i].enabled = false;
            }
        }

        /// <summary>
        /// Main du pêcheur (ses pieds sont à sa position), direction de son regard, et décalage sur le côté :
        /// face à l'écran ou de dos, la canne est tenue à droite du corps pour rester visible.
        /// </summary>
        private Vector2 Hand(out Vector2 facing, out Vector2 side)
        {
            facing = _playerMovement != null ? _playerMovement.Facing : Vector2.up;
            side = Mathf.Abs(facing.x) < 0.5f ? Vector2.right * 0.35f : Vector2.zero;
            return (Vector2)_player.position + Vector2.up * 0.7f + facing * 0.2f + side;
        }

        private void UpdateWorld()
        {
            FishingState state = _controller.State;
            if (state != _lastState)
            {
                OnWorldStateChanged(_lastState, state);
                _lastState = state;
            }

            UpdateRipples(Time.deltaTime);

            bool lineOut = state == FishingState.Casting || state == FishingState.Waiting || state == FishingState.Bite || state == FishingState.Reeling;
            _bobber.gameObject.SetActive(lineOut);
            _shadow.gameObject.SetActive(lineOut && state != FishingState.Casting);
            _rod.Clear();
            _lineDots.Clear();

            // Pendant la pêche, la caméra glisse vers le bouchon (et le poisson) pour qu'on les voie en entier.
            if (_cameraFollow == null && Camera.main != null) _cameraFollow = Camera.main.GetComponent<World.CameraFollow>();
            if (_cameraFollow != null)
            {
                bool watch = lineOut && state != FishingState.Casting && _player != null;
                Vector2 focus = _controller.HookedLegend != null ? BodyCenter(_fishPosition, _fishHeading) : _castPoint;
                Vector2 look = watch ? Vector2.ClampMagnitude((focus - (Vector2)_player.position) * 0.55f, 3.5f) : Vector2.zero;
                _cameraFollow.LookAhead = look;
            }

            if (!lineOut)
            {
                return;
            }

            float time = Time.time;
            Vector2 hand = Hand(out Vector2 facing, out Vector2 side);
            Vector2 restTip = hand + facing * 1.0f + Vector2.up * (facing.y < -0.5f ? 0.15f : 0.45f) + side * 0.5f;
            Vector2 tip = restTip;
            Vector2 bobber = _castPoint;
            Vector2 shadow = _castPoint;
            float sag = 0.3f;
            float bobberAlpha = 1f;

            switch (state)
            {
                case FishingState.Casting:
                {
                    // Armer (canne vers l'arrière), fouetter vers l'avant, puis le bouchon vole en arc jusqu'à l'eau.
                    float p = _controller.PhaseProgress;
                    Vector2 backTip = hand - facing * 0.45f + Vector2.up * 1.1f + side * 0.6f;
                    Vector2 whipTip = hand + facing * 1.1f + Vector2.up * 0.2f + side * 0.5f;
                    if (p < 0.22f)
                    {
                        tip = Vector2.Lerp(restTip, backTip, Mathf.SmoothStep(0f, 1f, p / 0.22f));
                    }
                    else if (p < 0.4f)
                    {
                        tip = Vector2.Lerp(backTip, whipTip, Mathf.SmoothStep(0f, 1f, (p - 0.22f) / 0.18f));
                    }
                    else
                    {
                        tip = Vector2.Lerp(whipTip, restTip, Mathf.SmoothStep(0f, 1f, (p - 0.4f) / 0.6f));
                    }

                    float flight = Mathf.InverseLerp(0.3f, 1f, p);
                    float distance = Vector2.Distance(hand, _castPoint);
                    Vector2 ground = Vector2.Lerp(tip, _castPoint, 1f - (1f - flight) * (1f - flight));   // ralentit en fin de vol
                    float arc = (0.8f + distance * 0.25f) * 4f * flight * (1f - flight);
                    bobber = p < 0.3f ? tip : ground + Vector2.up * arc;
                    sag = 0.08f;
                    break;
                }

                case FishingState.Waiting:
                    // Le bouchon dodeline ; à chaque titillement il plonge un instant et tressaille.
                    bobber += new Vector2(0f, Mathf.Sin(time * 2f) * 0.03f);
                    if (_nibbleTimer > 0f)
                    {
                        _nibbleTimer -= Time.deltaTime;
                        float k = Mathf.Clamp01(_nibbleTimer / 0.35f);
                        bobber += new Vector2(Mathf.Sin(time * 70f) * 0.03f * k, -0.09f * Mathf.Sin(k * Mathf.PI));
                    }

                {
                    // Le poisson arrive du large et s'arrête juste devant l'appât, le museau tourné vers lui, en
                    // ondulant ; à chaque titillement il avance d'un coup jusqu'à l'hameçon, puis recule un peu.
                    Vector2 fromBait = (_shadowStart - _fishAnchor).sqrMagnitude > 0.0001f ? (_shadowStart - _fishAnchor).normalized : Vector2.right;
                    Vector2 hover = _fishAnchor + fromBait * 0.45f;
                    Vector2 approach = Vector2.Lerp(_shadowStart, hover, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_controller.PhaseProgress * 1.6f)));
                    Vector2 across = new Vector2(-fromBait.y, fromBait.x);
                    Vector2 desired = approach + across * Mathf.Sin(time * 1.7f) * 0.12f;
                    if (_nibbleTimer > 0f)
                    {
                        desired = _fishAnchor;
                    }

                    shadow = MoveFish(desired, _nibbleTimer > 0f ? 2.6f : 1.2f, _fishAnchor);
                    sag = 0.3f;
                    break;
                }

                case FishingState.Bite:
                {
                    // Ça mord : le bouchon est tiré sous l'eau et disparaît peu à peu.
                    _biteAge += Time.deltaTime;
                    float sink = Mathf.Clamp01(_biteAge / 0.3f);
                    bobber += new Vector2(Mathf.Sin(time * 55f) * 0.04f * (1f - sink), -0.22f * sink);
                    bobberAlpha = Mathf.Lerp(1f, 0.25f, sink);
                    shadow = MoveFish(_fishAnchor + new Vector2(Mathf.Sin(time * 20f) * 0.06f, 0f), 3f, _fishAnchor);
                    tip = restTip + Vector2.down * 0.12f * sink;   // la canne plie un peu
                    sag = 0.02f;
                    break;
                }

                case FishingState.Reeling:
                    // Le poisson se débat au bout du fil tendu ; la canne plie vers lui. Éclaboussures de temps en temps.
                    shadow = MoveFish(_fishAnchor + new Vector2(Mathf.Sin(time * 9f) * 0.45f, Mathf.Cos(time * 7f) * 0.25f), 4f);
                    bobber = shadow;
                    bobberAlpha = 0f;
                    tip = restTip + (shadow - restTip).normalized * 0.15f + Vector2.down * 0.1f;
                    sag = 0f;
                    _splashTimer -= Time.deltaTime;
                    if (_splashTimer <= 0f)
                    {
                        SpawnRipple(shadow, 0.6f);
                        _splashTimer = 0.45f;
                    }

                    break;
            }

            _bobber.position = bobber;
            _bobberRenderer.color = new Color(1f, 1f, 1f, bobberAlpha);
            // Silhouette dessinée dans la bonne direction (pas de rotation de sprite), calée au pixel.
            bool fighting = state == FishingState.Reeling || state == FishingState.Bite;
            _tailTimer -= Time.deltaTime;
            if (_tailTimer <= 0f)
            {
                _tailFrame = (_tailFrame + 1) % 4;
                _tailTimer = fighting ? 0.07f : 0.18f;
            }

            int frame = _tailFrame == 3 ? 1 : _tailFrame;   // 0, 1, 2, 1… : la queue va et vient
            _shadow.sprite = _fishShadows.Get(_hookedShape, _fishSize, _fishHeading, frame);
            _shadow.transform.rotation = Quaternion.identity;
            // La position suivie est la gueule du poisson (au bout du fil) : le corps s'étend derrière elle.
            Vector2 body = BodyCenter(shadow, _fishHeading);
            _shadow.transform.position = new Vector3(Mathf.Round(body.x * 16f) / 16f, Mathf.Round(body.y * 16f) / 16f, 0f);

            // La canne : poignée en liège, blank bleu nuit, scion rouge.
            _rod.DrawCurve(hand, tip, 0.04f, t => t < 0.22f ? RodCork : t > 0.9f ? RodTip : RodBlank, thickness: 2);
            // Le fil part du scion et va jusqu'au bouchon (ou au poisson), plus ou moins détendu.
            float lineSag = sag * Mathf.Clamp01(Vector2.Distance(tip, bobber) / 3f);
            _lineDots.DrawCurve(tip, bobber, lineSag, _ => LineTint);
        }

        // ---------- Le poisson reste dans l'eau ----------

        /// <summary>Demi-longueur de la silhouette, en cases, selon la taille.</summary>
        private float FishHalfLength => (_fishSize == 0 ? 18 : _fishSize == 1 ? 26 : _fishSize == 2 ? 36 : 72) / 32f;

        /// <summary>Centre du corps quand la gueule est en <paramref name="mouth"/> et que le poisson regarde vers <paramref name="heading"/>.</summary>
        private Vector2 BodyCenter(Vector2 mouth, Vector2 heading)
        {
            Vector2 along = heading.sqrMagnitude > 0.0001f ? heading.normalized : Vector2.right;
            return mouth - along * FishHalfLength * 0.9f;
        }

        /// <summary>Le poisson entier (museau, queue, flancs), gueule en <paramref name="mouth"/>, est-il dans l'eau ?</summary>
        private bool FitsInWater(Vector2 mouth, Vector2 heading)
        {
            float half = FishHalfLength + 0.1f;
            Vector2 along = heading.sqrMagnitude > 0.0001f ? heading.normalized : Vector2.right;
            Vector2 across = new Vector2(-along.y, along.x);
            Vector2 center = BodyCenter(mouth, along);
            return _controller.IsWater(center)
                && _controller.IsWater(center + along * half)
                && _controller.IsWater(center - along * half)
                && _controller.IsWater(center + across * half * 0.4f)
                && _controller.IsWater(center - across * half * 0.4f);
        }

        /// <summary>
        /// Rapproche le poisson de <paramref name="desired"/> (vitesse en cases par seconde), sans jamais lui faire
        /// toucher le sable : s'il ne tient pas dans l'eau là-bas, il reste où il est. Met à jour sa direction de nage.
        /// </summary>
        private Vector2 MoveFish(Vector2 desired, float speed, Vector2? lookAt = null)
        {
            Vector2 next = Vector2.MoveTowards(_fishPosition, desired, speed * Time.deltaTime);
            Vector2 step = next - _fishPosition;
            Vector2 heading = step.sqrMagnitude > 0.00002f ? step.normalized : _fishHeading;
            // Près de l'appât, il garde le museau tourné vers lui (même quand il recule un peu).
            if (lookAt.HasValue && (lookAt.Value - next).sqrMagnitude < 1.5f && (lookAt.Value - next).sqrMagnitude > 0.0025f)
            {
                heading = (lookAt.Value - next).normalized;
            }
            if (FitsInWater(next, heading))
            {
                _fishPosition = next;
                // La direction tourne doucement (pas de demi-tour instantané).
                _fishHeading = Vector2.Lerp(_fishHeading, heading, 8f * Time.deltaTime).normalized;
            }

            return _fishPosition;
        }

        /// <summary>
        /// Endroit sous le bouchon où le poisson tient entièrement dans l'eau : le bouchon lui-même si possible,
        /// sinon un peu plus au large.
        /// </summary>
        private Vector2 FindWaterSpot(Vector2 castPoint, Vector2 away)
        {
            for (int k = 0; k <= 20; k++)
            {
                Vector2 candidate = castPoint + away * (k * 0.12f);
                if (FitsInWater(candidate, -away))
                {
                    return candidate;
                }
            }

            return castPoint;
        }

        /// <summary>
        /// D'où arrive le poisson : au large, dans une direction où tout le trajet jusqu'à l'appât reste dans l'eau.
        /// </summary>
        private Vector2 FindStartSpot(Vector2 anchor, Vector2 away)
        {
            // De biais plutôt que pile en face : on voit le poisson arriver (et une légende s'étale en travers de l'écran).
            float[] angles = _controller.HookedLegend != null
                ? new[] { 80f, -80f, 60f, -60f, 100f, -100f, 40f, -40f, 0f }
                : new[] { 40f, -40f, 60f, -60f, 20f, -20f, 0f, 90f, -90f, 120f, -120f };
            foreach (float distance in new[] { 2.5f, 1.8f, 1.2f })
            {
                foreach (float angle in angles)
                {
                    Vector2 direction = (Vector2)(Quaternion.Euler(0f, 0f, angle + Random.Range(-10f, 10f)) * away);
                    Vector2 start = anchor + direction * distance;
                    bool clear = true;
                    for (int i = 0; i <= 8 && clear; i++)
                    {
                        clear = FitsInWater(Vector2.Lerp(start, anchor, i / 8f), -direction);
                    }

                    if (clear)
                    {
                        return start;
                    }
                }
            }

            return anchor;
        }

        private void OnWorldStateChanged(FishingState previous, FishingState state)
        {
            if (state == FishingState.Waiting && previous == FishingState.Casting)
            {
                // Plouf : le bouchon touche l'eau.
                SpawnRipple(_castPoint, 1f);
                SpawnRipple(_castPoint, 0.6f);
            }

            if (state == FishingState.Bite)
            {
                _biteAge = 0f;
                SpawnRipple(_castPoint, 1.2f);
            }

            if (state == FishingState.Reeling)
            {
                _splashTimer = 0f;
            }
        }

        private void OnNibbled(int count)
        {
            _nibbleTimer = 0.35f;
            SpawnRipple(_castPoint, 0.5f);
        }

        /// <summary>Un rond dans l'eau qui s'agrandit en s'effaçant.</summary>
        private void SpawnRipple(Vector2 position, float size)
        {
            int slot = 0;
            for (int i = 1; i < _ripples.Length; i++)
            {
                if (_rippleAge[i] > _rippleAge[slot] || !_ripples[i].enabled)
                {
                    slot = i;
                    if (!_ripples[i].enabled)
                    {
                        break;
                    }
                }
            }

            _ripples[slot].enabled = true;
            _ripples[slot].transform.position = position;
            _rippleAge[slot] = 0f;
            _rippleSize[slot] = size;
        }

        private void UpdateRipples(float deltaTime)
        {
            const float life = 0.8f;
            for (int i = 0; i < _ripples.Length; i++)
            {
                if (!_ripples[i].enabled)
                {
                    continue;
                }

                _rippleAge[i] += deltaTime;
                float t = _rippleAge[i] / life;
                if (t >= 1f)
                {
                    _ripples[i].enabled = false;
                    continue;
                }

                // Taille par paliers de pixels (pas d'étirement flou) et transparence qui baisse.
                float scale = Mathf.Round(Mathf.Lerp(0.4f, 1.6f, t) * _rippleSize[i] * 4f) / 4f;
                _ripples[i].transform.localScale = new Vector3(Mathf.Max(0.25f, scale), Mathf.Max(0.25f, scale), 1f);
                _ripples[i].color = new Color(1f, 1f, 1f, 0.75f * (1f - t));
            }
        }

        // ---------- Interface ----------

        private void BuildUi()
        {
            var canvasObject = new GameObject("Interface (prototype)", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // au-dessus de la minimap : QTE et carte de prise restent visibles
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            UI.UiFactory.UseSkin(_skin);
            Font font = UI.UiFactory.Font;
            RectTransform root = canvasObject.GetComponent<RectTransform>();

            _header = CreateText(root, "En-tête", font, 34, TextAnchor.UpperLeft);
            Anchor(_header.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(900f, 120f), new Vector2(0f, 1f));

            _message = CreateText(root, "Message", font, 40, TextAnchor.MiddleCenter);
            Anchor(_message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1400f, 130f), new Vector2(0.5f, 1f));

            BuildQte(root);
            _catchCard = new CatchCard(root, _skin);
            _bottleCard = new BottleCard(root, _skin);
            _fragmentCard = new FragmentCard(root, _skin);
            _victoryCard = new LegendVictoryCard(root, _skin);
        }

        // ---------- Jauge du QTE ----------
        //
        // Cadre doré avec un bandeau « Ferre-le ! », barre d'eau, zone de réussite verte (liserés clair et foncé),
        // curseur en forme d'aiguille, pastilles des réussites (vertes) et des ratés (rouges). Flash et secousse à
        // chaque appui.

        private static readonly Color ZoneFill = new Color(0.36f, 0.86f, 0.42f, 0.92f);
        private static readonly Color ZoneLight = new Color(0.72f, 1.00f, 0.70f, 1f);
        private static readonly Color ZoneDark = new Color(0.14f, 0.52f, 0.22f, 1f);
        private static readonly Color PipEmpty = new Color(0.35f, 0.40f, 0.48f);
        private static readonly Color PipHit = new Color(0.40f, 0.95f, 0.45f);
        private static readonly Color PipMiss = new Color(0.95f, 0.30f, 0.28f);

        private RectTransform _qteRect;
        private RectTransform _barRect;
        private RectTransform _zoneRect;
        private RectTransform _cursorRect;
        private Image _flash;
        private RectTransform _hitPips;
        private RectTransform _missPips;
        private readonly List<Image> _hitPipImages = new List<Image>();
        private readonly List<Image> _missPipImages = new List<Image>();
        private float _flashTimer;
        private float _shakeTimer;
        private float _zonePop;
        private Text _qteTitle;
        private Image _zoneImage;
        private GameObject _tensionGroup;
        private Image _tensionFill;
        private int _seenRushes;
        private static readonly Color RushColor = new Color(1f, 0.36f, 0.30f, 0.95f);

        private void BuildQte(RectTransform root)
        {
            Image frame = UI.UiFactory.Sliced(root, "QTE", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : BarColor);
            _qtePanel = frame.gameObject;
            _qteRect = frame.rectTransform;
            UI.UiFactory.Place(_qteRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 300f), new Vector2(980f, 170f));

            _qteTitle = UI.UiFactory.TitlePlaque(_qteRect, _skin, "Ferre-le !", 28, new Vector2(0f, 84f), new Vector2(360f, 56f), out _);

            // La barre : un fond d'eau creusé.
            Image bar = UI.UiFactory.Sliced(_qteRect, "Barre", _skin != null ? _skin.WaterBackdrop : null, _skin != null ? Color.white : BarColor);
            _barRect = bar.rectTransform;
            UI.UiFactory.Center(_barRect, new Vector2(0f, 12f), new Vector2(860f, 52f));

            // Zone de réussite : vert vif, liseré clair en haut et sombre en bas, bords marqués.
            Image zone = CreateImage(_barRect, "Zone de réussite", ZoneFill);
            _zoneImage = zone;
            _zoneRect = zone.rectTransform;
            AddEdge(_zoneRect, "Haut", ZoneLight, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 6f));
            AddEdge(_zoneRect, "Bas", ZoneDark, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 6f));
            AddEdge(_zoneRect, "Gauche", ZoneDark, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(4f, 0f));
            AddEdge(_zoneRect, "Droite", ZoneDark, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(4f, 0f));

            // Flash de toute la barre (vert réussi, rouge raté).
            _flash = CreateImage(_barRect, "Flash", Color.clear);
            Stretch(_flash.rectTransform);
            _flash.raycastTarget = false;

            // Curseur : une aiguille avec une pointe, en pixel art.
            Image cursor = CreateImage(_barRect, "Curseur", Color.white);
            cursor.sprite = CreateNeedleSprite();
            cursor.preserveAspect = true;
            _cursorRect = cursor.rectTransform;
            _cursorRect.sizeDelta = new Vector2(28f, 84f);

            // Pastilles : réussites à gauche, ratés à droite.
            _hitPips = AddPipRow(_qteRect, "Réussites", new Vector2(-300f, -52f));
            _missPips = AddPipRow(_qteRect, "Ratés", new Vector2(300f, -52f));

            // Tension de la ligne (poissons lourds) : au milieu, sous la barre.
            Image tension = UI.UiFactory.Sliced(_qteRect, "Tension", _skin != null ? _skin.Slot : null, _skin != null ? Color.white : BarColor);
            UI.UiFactory.Center(tension.rectTransform, new Vector2(0f, -52f), new Vector2(230f, 34f));
            _tensionGroup = tension.gameObject;
            _tensionFill = CreateImage(tension.rectTransform, "Niveau", PipHit);
            _tensionFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            _tensionFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _tensionFill.rectTransform.offsetMin = new Vector2(6f, 6f);
            _tensionFill.rectTransform.offsetMax = new Vector2(-6f, -6f);
            Text tensionLabel = UI.UiFactory.Label(tension.rectTransform, "Légende", 18, TextAnchor.MiddleCenter, Color.white);
            tensionLabel.text = "Tension";
            UI.UiFactory.Stretch(tensionLabel.rectTransform);

            // Combat de légende : endurance de la bête au-dessus de la jauge, et ce qu'elle fait à chaque phase.
            Image stamina = UI.UiFactory.Sliced(_qteRect, "Endurance de la légende", _skin != null ? _skin.Slot : null, _skin != null ? Color.white : BarColor);
            UI.UiFactory.Center(stamina.rectTransform, new Vector2(0f, 150f), new Vector2(700f, 44f));
            _staminaGroup = stamina.gameObject;
            _staminaFill = CreateImage(stamina.rectTransform, "Niveau", new Color(1f, 0.45f, 0.25f));
            _staminaFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            _staminaFill.rectTransform.anchorMax = new Vector2(1f, 1f);
            _staminaFill.rectTransform.offsetMin = new Vector2(6f, 6f);
            _staminaFill.rectTransform.offsetMax = new Vector2(-6f, -6f);
            _staminaLabel = UI.UiFactory.Label(stamina.rectTransform, "Nom", 22, TextAnchor.MiddleCenter, Color.white);
            UI.UiFactory.Stretch(_staminaLabel.rectTransform);
            _staminaGroup.SetActive(false);

            _legendCry = UI.UiFactory.Label(_qteRect, "Cri de la légende", 40, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.30f, 0f));
            UI.UiFactory.Bold(_legendCry);
            UI.UiFactory.Center(_legendCry.rectTransform, new Vector2(0f, 215f), new Vector2(1200f, 60f));
        }

        private GameObject _staminaGroup;
        private Image _staminaFill;
        private Text _staminaLabel;
        private Text _legendCry;
        private float _legendCryAge = -1f;
        private float _shownStamina = 1f;

        /// <summary>Une phase du combat de légende commence : son cri s'affiche en grand, la jauge tremble et flashe.</summary>
        private void OnLegendPhaseStarted(int phase)
        {
            LegendFight fight = _controller.LegendFight;
            if (fight == null)
            {
                return;
            }

            _legendCry.text = phase == 0 ? fight.CurrentPhase.Cry : $"Phase {phase + 1} / {fight.PhaseCount} : {fight.CurrentPhase.Cry}";
            _legendCryAge = 0f;
            if (phase == 0)
            {
                _shownStamina = 1f;
            }

            _shakeTimer = 0.45f;
            Color flash = RushColor;
            flash.a = 0f;
            _flash.color = flash;
            _flashTimer = 0.25f;
        }

        /// <summary>Endurance de la légende (fond qui descend en douceur) et cri de la phase qui s'efface.</summary>
        private void UpdateLegendFight()
        {
            LegendFight fight = _controller.LegendFight;
            bool show = fight != null && _controller.State == FishingState.Reeling;
            _staminaGroup.SetActive(show);
            if (show)
            {
                _shownStamina = Mathf.MoveTowards(_shownStamina, fight.Stamina, Time.deltaTime * 0.8f);
                _staminaFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.02f, _shownStamina), 1f);
                _staminaFill.color = Color.Lerp(new Color(0.95f, 0.22f, 0.18f), new Color(1f, 0.62f, 0.22f), _shownStamina);
                _staminaLabel.text = $"{_controller.HookedLegend?.Title}   —   phase {fight.Phase + 1} / {fight.PhaseCount}";
            }

            if (_legendCryAge >= 0f)
            {
                _legendCryAge += Time.deltaTime;
                float appear = Mathf.Clamp01(_legendCryAge / 0.2f);
                float fade = Mathf.Clamp01((2.4f - _legendCryAge) / 0.5f);
                _legendCry.color = new Color(1f, 0.84f, 0.30f, show ? appear * fade : 0f);
                _legendCry.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, appear);
                if (_legendCryAge > 2.4f) _legendCryAge = -1f;
            }
        }

        private static void AddEdge(RectTransform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            Image edge = CreateImage(parent, name, color);
            RectTransform rect = edge.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, anchorMin.y == anchorMax.y ? anchorMin.y : 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private RectTransform AddPipRow(RectTransform parent, string caption, Vector2 position)
        {
            var row = new GameObject(caption, typeof(RectTransform));
            var rect = row.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            UI.UiFactory.Center(rect, position, new Vector2(360f, 36f));

            Text label = UI.UiFactory.Label(rect, "Légende", 20, TextAnchor.MiddleRight, new Color(0.80f, 0.84f, 0.88f));
            label.text = caption;
            UI.UiFactory.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(130f, 0f), new Vector2(130f, 36f));
            return rect;
        }

        /// <summary>Crée autant de pastilles que nécessaire (pierres de la charte).</summary>
        private void EnsurePips(RectTransform row, List<Image> pips, int count)
        {
            if (pips.Count == count)
            {
                return;
            }

            foreach (Image pip in pips)
            {
                Destroy(pip.gameObject);
            }

            pips.Clear();
            for (int i = 0; i < count; i++)
            {
                Image pip = CreateImage(row, $"Pastille {i + 1}", PipEmpty);
                pip.sprite = _skin != null ? _skin.Gem : null;
                UI.UiFactory.Place(pip.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(150f + i * 38f, 0f), new Vector2(30f, 30f));
                pips.Add(pip);
            }
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
            float pop = 1f + 0.25f * Mathf.Clamp01(_zonePop / 0.15f);
            float center = (gauge.ZoneMin + gauge.ZoneMax) * 0.5f;
            float half = (gauge.ZoneMax - gauge.ZoneMin) * 0.5f;
            _zoneRect.anchorMin = new Vector2(center - half, 0f);
            _zoneRect.anchorMax = new Vector2(center + half, 1f);
            _zoneRect.offsetMin = Vector2.zero;
            _zoneRect.offsetMax = Vector2.zero;
            _zoneRect.localScale = new Vector3(1f, pop, 1f);

            _cursorRect.anchorMin = _cursorRect.anchorMax = new Vector2(gauge.Position, 0.5f);
            _cursorRect.anchoredPosition = new Vector2(0f, 6f);

            EnsurePips(_hitPips, _hitPipImages, challenge.RequiredHits);
            EnsurePips(_missPips, _missPipImages, Mathf.Max(1, challenge.MaxMisses));
            for (int i = 0; i < _hitPipImages.Count; i++)
            {
                _hitPipImages[i].color = i < challenge.Hits ? PipHit : PipEmpty;
            }

            for (int i = 0; i < _missPipImages.Count; i++)
            {
                _missPipImages[i].color = i < challenge.Misses ? PipMiss : PipEmpty;
            }

            // Façon de se battre : titre de la jauge, tension des poissons lourds, rushs annoncés.
            LegendFight legendFight = _controller.LegendFight;
            _qteTitle.text = legendFight != null && legendFight.InBreak ? $"Phase {legendFight.Phase + 1} !" : FightTitle(challenge.Style);
            _tensionGroup.SetActive(challenge.UsesTension);
            if (challenge.UsesTension)
            {
                float tension = challenge.Tension;
                _tensionFill.rectTransform.anchorMax = new Vector2(tension, 1f);
                Color level = tension < 0.5f ? Color.Lerp(PipHit, new Color(1f, 0.86f, 0.30f), tension * 2f) : Color.Lerp(new Color(1f, 0.86f, 0.30f), PipMiss, (tension - 0.5f) * 2f);
                level.a = tension > 0.75f ? 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 12f)) : 1f;
                _tensionFill.color = level;
                _tensionFill.enabled = tension > 0.06f;
            }

            _zoneImage.color = challenge.RushWarning && Mathf.Sin(Time.time * 40f) > 0f ? RushColor : ZoneFill;
            if (challenge.Rushes != _seenRushes)
            {
                // Le poisson fonce : la jauge tremble et la barre flashe en rouge.
                _seenRushes = challenge.Rushes;
                Color rush = MissFlash;
                rush.a = 0f;
                _flash.color = rush;
                _flashTimer = 0.2f;
                _shakeTimer = 0.3f;
            }

            // Flash qui s'éteint, secousse de la jauge après un raté, zone qui « saute » après une réussite.
            float dt = Time.deltaTime;
            _flashTimer = Mathf.Max(0f, _flashTimer - dt);
            _zonePop = Mathf.Max(0f, _zonePop - dt);
            Color flash = _flash.color;
            flash.a = 0.45f * Mathf.Clamp01(_flashTimer / 0.2f);
            _flash.color = flash;

            _shakeTimer = Mathf.Max(0f, _shakeTimer - dt);
            float shake = _shakeTimer > 0f ? Mathf.Sin(Time.time * 90f) * 10f * (_shakeTimer / 0.25f) : 0f;
            // La jauge se place du côté opposé au bouchon pour ne jamais cacher le poisson.
            bool lineBelow = _player != null && _castPoint.y < _player.position.y;
            _qteRect.anchoredPosition = new Vector2(Mathf.Round(shake / 4f) * 4f, lineBelow ? 730f : 40f);
            // Le cri de la légende se place du côté libre de l'écran (sous la jauge quand elle est en haut).
            _legendCry.rectTransform.anchoredPosition = new Vector2(0f, lineBelow ? -125f : 215f);
        }

        /// <summary>Aiguille du curseur 7×21 : pointe en triangle en haut, tige, pied ; blanc cerclé de sombre.</summary>
        private static Sprite CreateNeedleSprite()
        {
            string[] rows =
            {
                "KKKKKKK",
                "KWWWWWK",
                ".KWWWK.",
                "..KWK..",
                "..KWK..",
                "..KWK..",
                "..KWK..",
                "..KWK..",
                "..KWK..",
                "..KWK..",
                "..KWK..",
                "..KWK..",
                "..KWK..",
                "..KWK..",
                "..KWK..",
                "..KWK..",
                "..KWK..",
                "..KWK..",
                ".KWWWK.",
                "KWWWWWK",
                "KKKKKKK",
            };

            int height = rows.Length;
            int width = rows[0].Length;
            var texture = new Texture2D(width, height) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int row = 0; row < height; row++)
            {
                for (int x = 0; x < width; x++)
                {
                    char c = rows[row][x];
                    Color color = c == 'K' ? new Color(0.10f, 0.08f, 0.10f) : c == 'W' ? new Color(1f, 0.97f, 0.88f) : Color.clear;
                    texture.SetPixel(x, height - 1 - row, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 4f);
        }

        private void OnDestroy()
        {
            if (_controller != null && _controller.Progress != null)
            {
                _controller.Progress.Changed -= RefreshHeaderIfReady;
            }
        }

        private void OnWeatherChanged(World.Weather weather) => RefreshHeaderIfReady();

        private void RefreshHeaderIfReady()
        {
            if (_header != null)
            {
                RefreshHeader();
            }
        }

        private void RefreshHeader()
        {
            string zone = _controller.Zone != null ? _controller.Zone.DisplayName : "(aucune zone)";
            CatchLog log = _controller.CatchLog;
            PlayerProgress progress = _controller.Progress;
            int inAquarium = progress != null && progress.Aquarium != null ? progress.Aquarium.SpeciesCount : log.SpeciesCount;
            if (_controller.Cycle != null)
            {
                zone += "      " + World.DayCycleMath.TimeText(Mathf.Floor(_controller.Cycle.Hour * 6f) / 6f);
            }

            if (_controller.Forecast != null)
            {
                zone += "      " + _controller.Forecast.DisplayName;
            }

            _header.text = $"{zone}\nAquarium : {inAquarium} espèce{(inAquarium > 1 ? "s" : string.Empty)}     Captures : {log.TotalCatches}";
        }

        // ---------- Événements ----------

        private void OnStateChanged()
        {
            if (_controller.State == FishingState.Reeling)
            {
                _seenRushes = 0; // nouveau combat
            }

            if (_header == null)
            {
                return;
            }

            // La carte reste affichée pendant le choix « sac plein », puis se ferme une fois le choix fait.
            bool cardPhase = _controller.State == FishingState.BagFull
                || (_controller.State == FishingState.Result && !_cardWasBagFull);
            if (!cardPhase)
            {
                _catchCard.Hide();
                _bottleCard.Hide();
                _fragmentCard.Hide();
                _victoryCard.Hide();
                _cardWasBagFull = false;
            }

            if (_controller.State == FishingState.Casting)
            {
                _castPoint = _controller.CastPoint;

                // Silhouette petite, moyenne ou grande selon la taille du poisson.
                float sizeCm = _controller.HookedSizeCm;
                _fishSize = _controller.HookedLegend != null ? 3 : sizeCm < 35f ? 0 : sizeCm < 80f ? 1 : 2;
                // Poisson, crabe, langouste, tortue ou grenouille : la silhouette suit l'espèce.
                _hookedShape = _controller.HookedLegend != null ? _controller.HookedLegend.ShadowShape
                    : _controller.HookedSpecies != null ? _controller.HookedSpecies.BodyShape : BodyShape.Fish;

                // Le poisson se tient sous le bouchon, mais toujours entièrement dans l'eau ; il arrive du large.
                Vector2 away = (_castPoint - (Vector2)_player.position).normalized;
                _fishAnchor = FindWaterSpot(_castPoint, away);
                _shadowStart = FindStartSpot(_fishAnchor, away);
                _fishPosition = _shadowStart;
                _fishHeading = (_fishAnchor - _shadowStart).sqrMagnitude > 0.01f ? (_fishAnchor - _shadowStart).normalized : -away;
            }

            RefreshHeader();
        }

        private static string FightTitle(FightStyle style)
        {
            switch (style)
            {
                case FightStyle.Nervous: return "Il est nerveux !";
                case FightStyle.Drifting: return "Il file !";
                case FightStyle.Rusher: return "Il fonce !";
                case FightStyle.Heavy: return "Il est lourd !";
                default: return "Ferre-le !";
            }
        }

        private void OnQtePressed(QtePressResult result)
        {
            bool hit = result == QtePressResult.Hit;
            Color flash = hit ? HitFlash : MissFlash;
            flash.a = 0f;
            _flash.color = flash;
            _flashTimer = 0.2f;
            if (hit)
            {
                _zonePop = 0.15f;
            }
            else
            {
                _shakeTimer = 0.25f;
            }
        }

        private void OnBottleFound(string message, int coins)
        {
            RefreshHeader();
            _catchCard.Hide();
            _bottleCard.Show(message, coins);
            _cardWasBagFull = false;
        }

        private void OnFragmentFound(LegendFish legend, int count)
        {
            RefreshHeader();
            _catchCard.Hide();
            _bottleCard.Hide();
            _fragmentCard.Show(legend, count);
            _cardWasBagFull = false;
        }

        private void OnLegendBeaten(LegendFish legend, int coins)
        {
            RefreshHeader();
            _catchCard.Hide();
            _bottleCard.Hide();
            _fragmentCard.Hide();
            _victoryCard.Show(legend, coins);
            _cardWasBagFull = false;
        }

        private void OnFishCaught(CaughtFish fish, bool isNew, bool bagFull)
        {
            RefreshHeader();
            PlayerProgress progress = _controller.Progress;
            int count = progress != null ? progress.Bag.Count : -1;
            int capacity = progress != null ? progress.Bag.Capacity : -1;
            _catchCard.Show(fish, isNew, bagFull, count, capacity, _controller.LastBeatenRecord, progress != null ? progress.SellFactor : 1f);
            _cardWasBagFull = bagFull;
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
            text.fontSize = UI.UiFactory.Size(size);
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

        private const float WorldPixelsPerUnit = 16f;

        /// <summary>Rond dans l'eau en pixel art : ellipse de contour d'un pixel, vue de dessus (aplatie).</summary>
        private static Sprite CreateRippleSprite()
        {
            const int width = 20;
            const int height = 8;
            var texture = new Texture2D(width, height) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
                    float dy = (y + 0.5f - height * 0.5f) / (height * 0.5f);
                    float d = dx * dx + dy * dy;
                    texture.SetPixel(x, y, d <= 1f && d >= 0.62f ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), WorldPixelsPerUnit);
        }

        /// <summary>Bouchon en pixel art : antenne, dôme rouge avec reflet, bande blanche.</summary>
        private static Sprite CreateBobberSprite()
        {
            string[] rows =
            {
                "..K..",
                ".RRR.",
                "RHRRR",
                "RRRRR",
                "WWWWW",
                ".WWW.",
            };

            int height = rows.Length;
            int width = rows[0].Length;
            var texture = new Texture2D(width, height) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int row = 0; row < height; row++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c;
                    switch (rows[row][x])
                    {
                        case 'K': c = new Color(0.15f, 0.12f, 0.10f); break;
                        case 'R': c = new Color(0.90f, 0.18f, 0.16f); break;
                        case 'H': c = new Color(1.00f, 0.60f, 0.55f); break;
                        case 'W': c = new Color(0.97f, 0.97f, 0.95f); break;
                        default: c = Color.clear; break;
                    }

                    texture.SetPixel(x, height - 1 - row, c);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.3f), WorldPixelsPerUnit);
        }
    }
}
