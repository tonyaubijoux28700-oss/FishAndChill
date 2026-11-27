using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Carte du monde : sur le ponton, E l'ouvre. Une vraie carte en pixel art (continents, déserts, glaces, écume)
    /// où chaque zone est placée à sa vraie longitude / latitude (<see cref="WorldChart"/>). ←/→ choisit une destination :
    /// la route en pointillés animés se trace depuis l'endroit où l'on est, et la fiche montre la zone et les espèces
    /// découvertes. Espace (ou E, A) : le bateau navigue jusque-là, puis fondu au noir et grand bandeau du nom de la zone.
    /// Échap ferme.
    /// </summary>
    public sealed class TravelMenu : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Muted = new Color(0.70f, 0.76f, 0.82f);
        private static readonly Color Ink = new Color(0.86f, 0.90f, 0.94f);

        private const int ChartWidth = 280;
        private const int ChartHeight = 130;
        private const int WaveFrames = 3;
        private const int RouteDots = 26;
        private const float SailSeconds = 1.5f;
        private static readonly Vector2 MapSize = new Vector2(1120f, 520f);

        [SerializeField] private ZoneTravel _travel;
        [SerializeField] private PlayerMovement _player;
        [SerializeField] private FishingController _fishing;

        [SerializeField, Tooltip("Charte pixel art de l'interface.")]
        private UiSkin _skin;

        [SerializeField, Min(0.05f)] private float _fadeSeconds = 0.4f;
        [SerializeField, Min(0.5f)] private float _bannerSeconds = 2.6f;

        private readonly List<RectTransform> _markers = new List<RectTransform>();
        private readonly List<Text> _markerLabels = new List<Text>();
        private readonly List<Image> _routeDots = new List<Image>();
        private readonly List<Image> _clouds = new List<Image>();
        private readonly Texture2D[] _chart = new Texture2D[WaveFrames];
        private GameObject _window;
        private RawImage _sea;
        private RectTransform _mapArea;
        private RectTransform _boat;
        private RectTransform _ring;
        private Image _ringImage;
        private Text _name;
        private Text _region;
        private Text _description;
        private Text _species;
        private Text _rarities;
        private RectTransform _barFill;
        private Text _prompt;
        private Image _fade;
        private Text _banner;
        private Text _bannerSub;
        private int _selected;
        private int _waveFrame;
        private float _waveTimer;
        private float _fadeTimer = -1f;
        private float _sailTimer = -1f;
        private int _pendingDestination = -1;
        private float _bannerTimer;

        public bool IsOpen => _window != null && _window.activeSelf;

        /// <summary>Un voyage est en cours (bateau en route ou fondu au noir).</summary>
        public bool Traveling => _fadeTimer >= 0f || _sailTimer >= 0f;

        private void Start()
        {
            BuildUi();
            _window.SetActive(false);
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in _chart)
            {
                if (texture != null) Destroy(texture);
            }
        }

        private void Update()
        {
            UpdateSail();
            UpdateFade();
            UpdateBanner();
            _prompt.gameObject.SetActive(!IsOpen && !Traveling && CanOpenNow());

            if (!IsOpen)
            {
                if (!Traveling && InteractInput.PressedThisFrame() && CanOpenNow())
                {
                    Open();
                }

                return;
            }

            Animate();
            if (_sailTimer >= 0f)
            {
                return; // le bateau est parti : on regarde
            }

            if (PauseInput.TogglePressedThisFrame() || PauseInput.BackPressedThisFrame())
            {
                Close();
                return;
            }

            (int dx, int dy) = IndexInput.NavigationThisFrame();
            int step = dx != 0 ? dx : -dy;
            if (step != 0 && _travel.Destinations.Count > 0)
            {
                _selected = PauseMenuLogic.Move(_selected, step, _travel.Destinations.Count);
                Refresh();
            }
            else if (PauseInput.ConfirmPressedThisFrame() || InteractInput.PressedThisFrame())
            {
                Depart(_selected);
            }
        }

        private bool CanOpenNow()
        {
            if (_travel == null || !_travel.PlayerAtDock)
            {
                return false;
            }

            if (_fishing != null && _fishing.State != FishingState.Idle && _fishing.State != FishingState.Result)
            {
                return false;
            }

            return _player == null || !_player.Locked;
        }

        public void Open()
        {
            _selected = _travel.Current;
            _window.SetActive(true);
            if (_player != null)
            {
                _player.Locked = true;
            }

            Refresh();
        }

        public void Close()
        {
            _window.SetActive(false);
            if (_player != null)
            {
                _player.Locked = false;
            }
        }

        /// <summary>Part vers la destination : le bateau navigue sur la carte, puis fondu au noir et arrivée sur son ponton.</summary>
        public void Depart(int index)
        {
            if (index == _travel.Current)
            {
                _description.text = "Tu es déjà ici ! Choisis une autre destination avec Gauche / Droite.";
                return;
            }

            _selected = index;
            Refresh();
            _pendingDestination = index;
            _sailTimer = 0f;
            if (_player != null)
            {
                _player.Locked = true;
            }
        }

        // ---------- Voyage : bateau, fondu et bandeau ----------

        private void UpdateSail()
        {
            if (_sailTimer < 0f)
            {
                return;
            }

            _sailTimer += Time.unscaledDeltaTime;
            if (_sailTimer >= SailSeconds)
            {
                _sailTimer = -1f;
                _fadeTimer = 0f;
            }
        }

        private void UpdateFade()
        {
            if (_fadeTimer < 0f)
            {
                return;
            }

            _fadeTimer += Time.unscaledDeltaTime;
            float half = _fadeSeconds;
            if (_pendingDestination >= 0 && _fadeTimer >= half)
            {
                // Écran noir : on change de zone à l'abri des regards.
                _window.SetActive(false);
                _travel.TravelTo(_pendingDestination);
                _pendingDestination = -1;
                ShowBanner();
            }

            float alpha = _fadeTimer < half ? _fadeTimer / half : 1f - (_fadeTimer - half - 0.15f) / half;
            SetAlpha(_fade, Mathf.Clamp01(alpha));
            if (_fadeTimer >= half * 2f + 0.15f)
            {
                _fadeTimer = -1f;
                SetAlpha(_fade, 0f);
                if (_player != null)
                {
                    _player.Locked = false;
                }
            }
        }

        private void ShowBanner()
        {
            FishingZone zone = _travel.CurrentDestination != null ? _travel.CurrentDestination.Zone : null;
            if (zone == null)
            {
                return;
            }

            _banner.text = zone.DisplayName;
            _bannerSub.text = zone.Region;
            _bannerTimer = _bannerSeconds;
        }

        private void UpdateBanner()
        {
            if (_bannerTimer <= 0f)
            {
                _banner.gameObject.SetActive(false);
                return;
            }

            _bannerTimer -= Time.unscaledDeltaTime;
            _banner.gameObject.SetActive(true);
            float shown = _bannerSeconds - _bannerTimer;
            float alpha = Mathf.Min(Mathf.Clamp01(shown / 0.4f), Mathf.Clamp01(_bannerTimer / 0.6f));
            SetAlpha(_banner, alpha);
            SetAlpha(_bannerSub, alpha);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        // ---------- Animation de la carte ----------

        private void Animate()
        {
            float time = Time.unscaledTime;

            // Vaguelettes.
            _waveTimer += Time.unscaledDeltaTime;
            if (_waveTimer > 0.5f)
            {
                _waveTimer = 0f;
                _waveFrame = (_waveFrame + 1) % WaveFrames;
                _sea.texture = _chart[_waveFrame];
            }

            // Nuages qui dérivent d'ouest en est.
            for (int i = 0; i < _clouds.Count; i++)
            {
                float x = Mathf.Repeat(time * (0.010f + i * 0.004f) + i * 0.29f, 1.3f) - 0.65f;
                _clouds[i].rectTransform.anchoredPosition = new Vector2(x * MapSize.x, ((i * 0.31f + 0.15f) % 0.8f - 0.4f) * MapSize.y);
            }

            IReadOnlyList<ZoneTravel.Destination> destinations = _travel.Destinations;
            if (destinations.Count == 0)
            {
                return;
            }

            Vector2 from = MarkerPosition(destinations[_travel.Current].Zone);
            Vector2 to = MarkerPosition(destinations[_selected].Zone);
            bool route = _selected != _travel.Current;

            // Route en pointillés qui défilent vers la destination.
            float offset = Mathf.Repeat(time * 0.6f, 1f);
            for (int i = 0; i < _routeDots.Count; i++)
            {
                _routeDots[i].gameObject.SetActive(route);
                if (!route) continue;
                float t = (i + offset) / _routeDots.Count;
                _routeDots[i].rectTransform.anchoredPosition = RoutePoint(from, to, t);
            }

            // Le bateau : amarré (il se balance) ou en route vers la destination.
            float sail = _sailTimer >= 0f ? Mathf.SmoothStep(0f, 1f, _sailTimer / SailSeconds) : 0f;
            Vector2 boat = RoutePoint(from, to, sail) + Vector2.up * (Mathf.Sin(time * 3f) * 2f + 18f);
            _boat.anchoredPosition = boat;
            _boat.localScale = new Vector3(route && HeadingWest(from, to) ? -1f : 1f, 1f, 1f);

            // Repères : celui choisi grossit doucement, avec une onde.
            for (int i = 0; i < _markers.Count; i++)
            {
                bool selected = i == _selected;
                _markers[i].localScale = Vector3.one * (selected ? 1.35f + Mathf.Sin(time * 4f) * 0.08f : 1f);
            }

            Vector2 pulse = MinimapMath.Pulse(time, 1.2f);
            _ring.anchoredPosition = to;
            _ring.localScale = Vector3.one * pulse.x;
            _ringImage.color = new Color(1f, 0.92f, 0.60f, pulse.y * 0.9f);
        }

        /// <summary>
        /// Point d'une route courbe entre deux repères (arc qui monte vers le nord). Comme sur un vrai globe, on prend le
        /// chemin le plus court : s'il passe par le bord de la carte (traversée du Pacifique), la route sort d'un côté et
        /// revient par l'autre.
        /// </summary>
        private static Vector2 RoutePoint(Vector2 a, Vector2 b, float t)
        {
            float dx = b.x - a.x;
            if (dx > MapSize.x * 0.5f) b.x -= MapSize.x;
            else if (dx < -MapSize.x * 0.5f) b.x += MapSize.x;
            float lift = Mathf.Min(90f, Vector2.Distance(a, b) * 0.22f);
            Vector2 p = Vector2.Lerp(a, b, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * lift;
            p.x = Mathf.Repeat(p.x + MapSize.x * 0.5f, MapSize.x) - MapSize.x * 0.5f;
            return p;
        }

        /// <summary>Le bateau va-t-il vers la gauche (en tenant compte du passage par le bord) ?</summary>
        private static bool HeadingWest(Vector2 a, Vector2 b)
        {
            float dx = b.x - a.x;
            if (dx > MapSize.x * 0.5f) dx -= MapSize.x;
            else if (dx < -MapSize.x * 0.5f) dx += MapSize.x;
            return dx < 0f;
        }

        // ---------- Fiche de la destination ----------

        private void Refresh()
        {
            IReadOnlyList<ZoneTravel.Destination> destinations = _travel.Destinations;
            for (int i = 0; i < _markers.Count; i++)
            {
                _markerLabels[i].color = i == _selected ? Gold : Ink;
            }

            FishingZone chosen = destinations.Count > 0 ? destinations[_selected].Zone : null;
            if (chosen == null)
            {
                return;
            }

            _name.text = chosen.DisplayName;
            _region.text = chosen.Region;
            _description.text = _selected == _travel.Current ? $"{chosen.Description}\nTu es ici." : chosen.Description;

            int discovered = 0;
            int total = 0;
            var found = new int[5];
            var all = new int[5];
            foreach (FishingZone.SpeciesEntry entry in chosen.Species)
            {
                if (entry.Species == null) continue;
                total++;
                all[(int)entry.Species.Rarity]++;
                if (_fishing != null && _fishing.CatchLog.IsDiscovered(entry.Species.Id))
                {
                    discovered++;
                    found[(int)entry.Species.Rarity]++;
                }
            }

            _species.text = $"Espèces découvertes : {discovered} / {total}";
            _barFill.anchorMax = new Vector2(total > 0 ? discovered / (float)total : 0f, 1f);
            var line = new System.Text.StringBuilder();
            int shown = 0;
            for (int r = 0; r < 5; r++)
            {
                if (all[r] == 0) continue;
                var rarity = (Rarity)r;
                if (shown > 0) line.Append(shown % 3 == 0 ? "\n" : "   ");
                shown++;
                line.Append($"<color=#{ColorUtility.ToHtmlStringRGB(rarity.DisplayColor())}>{rarity.DisplayName()}</color> {found[r]}/{all[r]}");
            }

            _rarities.text = line.ToString();
        }

        private static Vector2 MarkerPosition(FishingZone zone)
        {
            Vector2 p = zone != null ? zone.MapPosition : new Vector2(0.5f, 0.5f);
            return new Vector2((p.x - 0.5f) * MapSize.x, (p.y - 0.5f) * MapSize.y);
        }

        // ---------- Dessin de la carte du monde ----------

        private void DrawChart()
        {
            var ground = new WorldChart.Ground[ChartWidth, ChartHeight];
            for (int x = 0; x < ChartWidth; x++)
            {
                for (int y = 0; y < ChartHeight; y++)
                {
                    Vector2 lonLat = WorldChart.ToLonLat(new Vector2((x + 0.5f) / ChartWidth, (y + 0.5f) / ChartHeight));
                    ground[x, y] = WorldChart.GroundAt(lonLat.x, lonLat.y);
                }
            }

            bool Sea(int x, int y) => x < 0 || y < 0 || x >= ChartWidth || y >= ChartHeight || ground[x, y] == WorldChart.Ground.Sea;
            var pixels = new Color[ChartWidth * ChartHeight];
            var water = new bool[pixels.Length];
            for (int x = 0; x < ChartWidth; x++)
            {
                for (int y = 0; y < ChartHeight; y++)
                {
                    int i = y * ChartWidth + x;
                    float lat = Mathf.Abs(WorldChart.ToLonLat(new Vector2(0.5f, (y + 0.5f) / ChartHeight)).y);
                    Color c;
                    if (ground[x, y] == WorldChart.Ground.Sea)
                    {
                        // Mer plus claire et plus turquoise sous les tropiques, plus sombre vers les pôles.
                        float warm = Mathf.Clamp01(1f - lat / 60f);
                        c = Color.Lerp(new Color(0.10f, 0.26f, 0.44f), new Color(0.16f, 0.46f, 0.62f), Mathf.Round(warm * 4f) / 4f);
                        bool coast = !Sea(x + 1, y) || !Sea(x - 1, y) || !Sea(x, y + 1) || !Sea(x, y - 1);
                        bool near = !Sea(x + 2, y) || !Sea(x - 2, y) || !Sea(x, y + 2) || !Sea(x, y - 2);
                        if (coast) c = new Color(0.78f, 0.92f, 0.94f);
                        else if (near) c = Color.Lerp(c, new Color(0.40f, 0.74f, 0.80f), 0.5f);
                        else water[i] = true;
                    }
                    else
                    {
                        switch (ground[x, y])
                        {
                            case WorldChart.Ground.Desert: c = new Color(0.86f, 0.74f, 0.46f); break;
                            case WorldChart.Ground.Tundra: c = new Color(0.56f, 0.62f, 0.50f); break;
                            case WorldChart.Ground.Ice: c = new Color(0.92f, 0.95f, 0.98f); break;
                            default: c = lat < 23f ? new Color(0.26f, 0.56f, 0.24f) : new Color(0.40f, 0.62f, 0.30f); break;
                        }

                        // Relief : bord éclairé au nord-ouest, ombré au sud-est ; un peu de grain.
                        if (Sea(x - 1, y) || Sea(x, y + 1)) c = Color.Lerp(c, Color.white, 0.18f);
                        else if (Sea(x + 1, y) || Sea(x, y - 1)) c = Color.Lerp(c, Color.black, 0.22f);
                        else if (Hash(x, y, 5) > 0.82f) c = Color.Lerp(c, Color.black, 0.1f);
                    }

                    pixels[i] = c;
                }
            }

            for (int f = 0; f < WaveFrames; f++)
            {
                var frame = (Color[])pixels.Clone();
                for (int i = 0; i < frame.Length; i++)
                {
                    if (!water[i]) continue;
                    int x = i % ChartWidth;
                    int y = i / ChartWidth;
                    if (Mathf.Sin(x * 0.55f + y * 1.3f - f * 2.1f) > 0.96f && Hash(x / 3, y, 9) > 0.6f)
                    {
                        frame[i] = Color.Lerp(frame[i], Color.white, 0.25f);
                    }
                }

                _chart[f] = new Texture2D(ChartWidth, ChartHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                _chart[f].SetPixels(frame);
                _chart[f].Apply();
            }
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
                h = (h << 13) ^ h;
                return ((h * (h * h * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
            }
        }

        private static Sprite MakeSprite(int width, int height, System.Func<int, int, Color> pixel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    texture.SetPixel(x, y, pixel(x, y));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width);
        }

        /// <summary>Petit voilier 16×14 vu de côté : coque brune, voile blanche, fanion rouge.</summary>
        private static Color BoatPixel(int x, int y)
        {
            var hull = new Color(0.50f, 0.30f, 0.16f);
            var sail = new Color(0.96f, 0.94f, 0.88f);
            var outline = new Color(0.12f, 0.08f, 0.06f);
            bool inHull = y >= 1 && y <= 3 && x >= 1 + (3 - y) && x <= 14 - (3 - y);
            bool inSail = x >= 4 && x <= 10 && y >= 5 && y <= 12 && x - 4 <= (y - 4) * 0.9f;
            bool mast = x == 10 && y >= 4 && y <= 13;
            bool flag = y == 13 && (x == 11 || x == 12);
            if (flag) return new Color(0.86f, 0.20f, 0.16f);
            if (mast) return outline;
            if (inSail) return x == 4 || y == 5 ? new Color(0.80f, 0.78f, 0.72f) : sail;
            if (inHull) return y == 3 ? new Color(0.66f, 0.42f, 0.24f) : hull;
            bool near = false;
            for (int dx = -1; dx <= 1 && !near; dx++)
            {
                for (int dy = -1; dy <= 1 && !near; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    near = (ny >= 1 && ny <= 3 && nx >= 1 + (3 - ny) && nx <= 14 - (3 - ny)) || (nx >= 4 && nx <= 10 && ny >= 5 && ny <= 12 && nx - 4 <= (ny - 4) * 0.9f);
                }
            }

            return near ? outline : Color.clear;
        }

        // ---------- Construction ----------

        private void BuildUi()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Interface du voyage", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();

            // Invitation sur le ponton.
            _prompt = UiFactory.Label(root, "Invitation", 30, TextAnchor.MiddleCenter, Gold);
            UiFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(700f, 44f));
            _prompt.text = "E : prendre le bateau (carte du monde)";

            Image window = UiFactory.Sliced(root, "Carte du monde", _skin != null ? _skin.Panel : null, _skin != null ? Color.white : new Color(0.08f, 0.12f, 0.19f));
            _window = window.gameObject;
            RectTransform panel = window.rectTransform;
            UiFactory.Center(panel, new Vector2(0f, 10f), new Vector2(1240f, 880f));
            UiFactory.TitlePlaque(panel, _skin, "Carte du monde", 38, new Vector2(0f, 430f), new Vector2(500f, 80f), out _);

            // La carte : cadre bois-doré autour, image du monde dessinée au démarrage.
            Image border = UiFactory.Plain(panel, "Bord de la carte", new Color(0.36f, 0.24f, 0.12f));
            UiFactory.Center(border.rectTransform, new Vector2(0f, 105f), MapSize + new Vector2(16f, 16f));
            Image inner = UiFactory.Plain(border.rectTransform, "Liseré", new Color(0.86f, 0.70f, 0.36f));
            UiFactory.Center(inner.rectTransform, Vector2.zero, MapSize + new Vector2(6f, 6f));

            DrawChart();
            var seaObject = new GameObject("Monde", typeof(RectTransform), typeof(RawImage), typeof(RectMask2D));
            seaObject.transform.SetParent(panel, false);
            _sea = seaObject.GetComponent<RawImage>();
            _sea.texture = _chart[0];
            _mapArea = _sea.rectTransform;
            UiFactory.Center(_mapArea, new Vector2(0f, 105f), MapSize);

            // Méridiens et parallèles discrets, équateur un peu plus marqué.
            for (int i = 1; i < 8; i++)
            {
                Image meridian = UiFactory.Plain(_mapArea, "Méridien", new Color(1f, 1f, 1f, 0.06f));
                UiFactory.Place(meridian.rectTransform, new Vector2(i / 8f, 0f), new Vector2(i / 8f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 0f));
            }

            float equator = WorldChart.FromLonLat(Vector2.zero).y;
            foreach (float lat in new[] { -40f, 0f, 40f })
            {
                float y = WorldChart.FromLonLat(new Vector2(0f, lat)).y;
                Image parallel = UiFactory.Plain(_mapArea, "Parallèle", new Color(1f, 0.95f, 0.80f, Mathf.Approximately(y, equator) ? 0.14f : 0.06f));
                UiFactory.Place(parallel.rectTransform, new Vector2(0f, y), new Vector2(1f, y), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 2f));
            }

            // Nuages qui passent.
            Sprite cloud = MakeSprite(32, 16, (x, y) =>
            {
                float a = 0f;
                foreach (Vector3 blob in new[] { new Vector3(10f, 7f, 7f), new Vector3(19f, 8f, 8f), new Vector3(14f, 10f, 6f) })
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(blob.x, blob.y)) / blob.z;
                    a = Mathf.Max(a, Mathf.Clamp01(1.2f - d));
                }

                return new Color(1f, 1f, 1f, Mathf.Round(a * 3f) / 3f * 0.55f);
            });
            for (int i = 0; i < 4; i++)
            {
                Image c = UiFactory.Plain(_mapArea, "Nuage", Color.white);
                c.sprite = cloud;
                c.raycastTarget = false;
                c.rectTransform.sizeDelta = new Vector2(150f, 75f);
                _clouds.Add(c);
            }

            // Route animée.
            Sprite dot = MakeSprite(4, 4, (x, y) => (x == 0 || x == 3) && (y == 0 || y == 3) ? Color.clear : Color.white);
            for (int i = 0; i < RouteDots; i++)
            {
                Image d = UiFactory.Plain(_mapArea, "Route", new Color(1f, 0.92f, 0.70f, 0.9f));
                d.sprite = dot;
                d.rectTransform.sizeDelta = new Vector2(8f, 8f);
                _routeDots.Add(d);
            }

            // Onde sur la destination choisie.
            Sprite ring = MakeSprite(32, 32, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f));
                return d > 12.5f && d < 15f ? Color.white : Color.clear;
            });
            Image ringImage = UiFactory.Plain(_mapArea, "Onde", Color.white);
            ringImage.sprite = ring;
            ringImage.rectTransform.sizeDelta = new Vector2(40f, 40f);
            _ring = ringImage.rectTransform;
            _ringImage = ringImage;

            // Repères : une pastille à la couleur de l'eau de la zone, cerclée d'or, et une étiquette.
            Sprite pin = MakeSprite(16, 16, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(8f, 8f));
                if (d > 7.6f) return Color.clear;
                if (d > 6.2f) return new Color(0.20f, 0.12f, 0.06f);
                if (d > 4.6f) return new Color(1f, 0.84f, 0.30f);
                return Color.white;
            });
            foreach (ZoneTravel.Destination destination in _travel.Destinations)
            {
                Vector2 position = MarkerPosition(destination.Zone);
                Image marker = UiFactory.Plain(_mapArea, "Repère", destination.Zone != null ? Color.Lerp(destination.Zone.WaterColor, Color.white, 0.25f) : Gold);
                marker.sprite = pin;
                UiFactory.Center(marker.rectTransform, position, new Vector2(30f, 30f));
                Image plate = UiFactory.Plain(_mapArea, "Étiquette", new Color(0.06f, 0.08f, 0.12f, 0.72f));
                UiFactory.Center(plate.rectTransform, position + new Vector2(0f, -34f), new Vector2(Mathf.Max(120f, (destination.Zone != null ? destination.Zone.DisplayName.Length : 4) * 15f + 24f), 32f));
                Text label = UiFactory.Label(plate.rectTransform, "Nom", 24, TextAnchor.MiddleCenter, Ink);
                UiFactory.Stretch(label.rectTransform);
                label.text = destination.Zone != null ? destination.Zone.DisplayName : "?";
                _markers.Add(marker.rectTransform);
                _markerLabels.Add(label);
            }

            // Le bateau du joueur.
            Image boat = UiFactory.Plain(_mapArea, "Bateau", Color.white);
            boat.sprite = MakeSprite(16, 14, BoatPixel);
            boat.rectTransform.sizeDelta = new Vector2(40f, 35f);
            _boat = boat.rectTransform;

            // Rose des vents en bas à droite de la carte.
            var compassAt = new Vector2(MapSize.x * 0.5f - 44f, -MapSize.y * 0.5f + 46f);
            Image needle = UiFactory.Plain(_mapArea, "Rose des vents", Color.white);
            needle.sprite = MakeSprite(9, 15, (x, y) =>
            {
                float u = Mathf.Abs(x - 4f);
                bool inside = u <= (y < 7 ? y * 0.6f : (14 - y) * 0.6f);
                if (!inside) return Color.clear;
                return y >= 7 ? new Color(0.86f, 0.22f, 0.16f) : new Color(0.94f, 0.92f, 0.86f);
            });
            UiFactory.Center(needle.rectTransform, compassAt, new Vector2(27f, 45f));
            Text north = UiFactory.Label(_mapArea, "Nord", 24, TextAnchor.MiddleCenter, new Color(1f, 0.90f, 0.62f));
            north.text = "N";
            UiFactory.Center(north.rectTransform, compassAt + new Vector2(0f, 38f), new Vector2(40f, 30f));

            // Fiche de la destination choisie.
            _name = UiFactory.Label(panel, "Destination", 40, TextAnchor.MiddleLeft, Gold);
            UiFactory.Center(_name.rectTransform, new Vector2(-200f, -205f), new Vector2(720f, 50f));
            UiFactory.Bold(_name);
            _region = UiFactory.Label(panel, "Région", 24, TextAnchor.MiddleLeft, Muted);
            UiFactory.Center(_region.rectTransform, new Vector2(-200f, -243f), new Vector2(720f, 30f));
            _description = UiFactory.Label(panel, "Description", 23, TextAnchor.UpperLeft, Ink);
            _description.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Center(_description.rectTransform, new Vector2(-200f, -305f), new Vector2(720f, 80f));

            _species = UiFactory.Label(panel, "Espèces", 26, TextAnchor.MiddleLeft, Ink);
            UiFactory.Center(_species.rectTransform, new Vector2(380f, -210f), new Vector2(380f, 36f));
            Image bar = UiFactory.Plain(panel, "Barre", new Color(0.04f, 0.06f, 0.10f));
            UiFactory.Center(bar.rectTransform, new Vector2(380f, -248f), new Vector2(380f, 18f));
            Image fill = UiFactory.Plain(bar.rectTransform, "Progression", new Color(0.46f, 0.84f, 0.50f));
            _barFill = fill.rectTransform;
            _barFill.anchorMin = Vector2.zero;
            _barFill.anchorMax = new Vector2(0f, 1f);
            _barFill.offsetMin = new Vector2(2f, 2f);
            _barFill.offsetMax = new Vector2(-2f, -2f);
            _rarities = UiFactory.Label(panel, "Raretés", 20, TextAnchor.UpperLeft, Ink);
            _rarities.supportRichText = true;
            _rarities.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiFactory.Center(_rarities.rectTransform, new Vector2(380f, -300f), new Vector2(380f, 70f));

            Text hint = UiFactory.Label(panel, "Aide", 22, TextAnchor.MiddleCenter, Muted);
            UiFactory.Center(hint.rectTransform, new Vector2(0f, -395f), new Vector2(1100f, 32f));
            hint.text = "Gauche / Droite : choisir la destination   •   Espace : partir   •   Échap : fermer";

            // Fondu au noir du voyage, puis grand bandeau avec le nom de la zone.
            _fade = UiFactory.Plain(root, "Fondu", new Color(0f, 0f, 0f, 0f));
            UiFactory.Stretch(_fade.rectTransform);
            _fade.raycastTarget = false;
            _banner = UiFactory.Label(root, "Arrivée", 72, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.82f, 0f));
            UiFactory.Center(_banner.rectTransform, new Vector2(0f, 250f), new Vector2(1400f, 100f));
            UiFactory.Bold(_banner);
            _bannerSub = UiFactory.Label(_banner.rectTransform, "Région", 32, TextAnchor.MiddleCenter, new Color(0.85f, 0.90f, 0.95f, 0f));
            UiFactory.Center(_bannerSub.rectTransform, new Vector2(0f, -66f), new Vector2(1200f, 44f));
            _banner.gameObject.SetActive(false);
        }
    }
}
