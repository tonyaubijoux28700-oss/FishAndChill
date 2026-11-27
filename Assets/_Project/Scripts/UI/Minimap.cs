using System;
using System.Collections.Generic;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace FishAndChill.UI
{
    /// <summary>
    /// Carte de la zone : une petite carte toujours visible en bas à droite, et une grande carte au centre ouverte /
    /// fermée avec M. Les deux partagent la même image, dessinée depuis les Tilemaps à plusieurs pixels par case :
    /// côtes arrondies avec écume, eau plus claire près des rives, sol texturé, arbres en petites couronnes ombrées.
    /// La carte vit : vaguelettes qui scintillent, ombres de nuages, coins de pêche qui pulsent, flèche du joueur qui
    /// suit son regard avec une onde, teinte du jour et de la nuit. Le jeu continue pendant ce temps.
    /// </summary>
    public sealed class Minimap : MonoBehaviour
    {
        [Serializable]
        public sealed class TileColor
        {
            public TileBase Tile;
            public Color Color = Color.white;
        }

        /// <summary>Une vue de la carte : image, flèche et onde du joueur, lieux, coins de pêche, nuages.</summary>
        private sealed class MapView
        {
            public GameObject Root;
            public RawImage Image;
            public RectTransform Arrow;
            public RectTransform Ring;
            public Image RingImage;
            public float MarkerSize;
            public readonly List<RectTransform> Landmarks = new List<RectTransform>();
            public readonly List<Image> Spots = new List<Image>();
            public readonly List<Image> Clouds = new List<Image>();
        }

        /// <summary>
        /// Grand repère dessiné sur la carte (sanctuaire, maison, onsen, montagne…) : un rectangle de couleur sur sa
        /// surface au sol, ou un triangle à sommet enneigé pour une montagne.
        /// </summary>
        [Serializable]
        public sealed class Block
        {
            public Rect Area;
            public Color Color = Color.white;
            public bool Mountain;
        }

        /// <summary>Lieu signalé sur la carte par une icône (boutique, aquarium…).</summary>
        [Serializable]
        public sealed class Landmark
        {
            public string Name;
            public Transform Target;
            public Sprite Icon;
        }

        private const int WaterFrames = 3;
        private const float FrameSeconds = 0.45f;
        private const int SpotMarkers = 6;
        private const int CloudCount = 3;

        [SerializeField] private WorldMap _map;
        [SerializeField] private Transform _player;

        [SerializeField, Tooltip("Tilemap de décor (palmiers…) affichée par-dessus la terre.")]
        private Tilemap _decor;

        [SerializeField, Tooltip("Couche des pavés (chemins, parvis), en double grille.")]
        private Tilemap _paving;

        [SerializeField] private Color _pavingColor = new Color(0.82f, 0.78f, 0.70f);

        [SerializeField, Tooltip("Couche du lagon (eau claire), en double grille : ces cases sont claires sur la carte, même loin des côtes.")]
        private Tilemap _lagoon;

        [SerializeField, Tooltip("Teinte de l'eau claire du lagon sur la carte.")]
        private Color _lagoonTint = new Color(0.30f, 0.82f, 0.80f);

        [SerializeField, Tooltip("Couche des falaises (et cascades) : ces cases sont en roche sur la carte.")]
        private Tilemap _cliffs;

        [SerializeField] private Color _cliffColor = new Color(0.50f, 0.48f, 0.46f);

        [SerializeField, Tooltip("Grands repères dessinés sur la carte (bâtiments, montagnes).")]
        private List<Block> _blocks = new List<Block>();

        [SerializeField, Tooltip("Couleur de chaque tuile sur la carte.")]
        private List<TileColor> _palette = new List<TileColor>();

        [SerializeField, Tooltip("Lieux à signaler sur la carte (boutique, aquarium), chacun avec son icône.")]
        private List<Landmark> _landmarks = new List<Landmark>();

        [SerializeField, Tooltip("Couleur d'un lieu sans icône (losange).")]
        private Color _landmarkColor = new Color(1.00f, 0.60f, 0.15f);

        [SerializeField] private Color _waterColor = new Color(0.20f, 0.62f, 0.72f);
        [SerializeField] private Color _unknownLandColor = new Color(0.80f, 0.75f, 0.55f);
        [SerializeField] private Color _playerColor = new Color(0.95f, 0.20f, 0.20f);

        [SerializeField, Min(1), Tooltip("Grande carte : taille d'une case en pixels de référence (1920×1080).")]
        private int _pixelsPerCell = 6;

        [SerializeField, Min(1), Tooltip("Petite carte (coin) : taille d'une case en pixels de référence.")]
        private int _cornerPixelsPerCell = 3;

        [SerializeField, Range(1, 8), Tooltip("Finesse du dessin : pixels de l'image par case.")]
        private int _detail = 4;

        [SerializeField] private string _title = "Île Mahana";

        [SerializeField, Tooltip("Charte de l'interface (police, cadres).")]
        private UiSkin _skin;

        private MapView _corner;
        private MapView _large;
        private readonly Texture2D[] _frames = new Texture2D[WaterFrames];
        private int _frame;
        private float _frameTimer;
        private Rect _worldBounds;
        private Text _titleText;
        private Text _cornerTitle;
        private PlayerMovement _movement;
        private DayNightCycle _cycle;
        private FishingSpots[] _spotSources = new FishingSpots[0];
        private LegendSpot[] _legendSources = new LegendSpot[0];
        private readonly float[] _cloudX = new float[CloudCount];
        private Sprite _arrowSprite;
        private Sprite _ringSprite;
        private Sprite _spotSprite;
        private Sprite _cloudSprite;

        /// <summary>Titre de la carte (nom de la zone).</summary>
        public void SetTitle(string title)
        {
            _title = title;
            if (_titleText != null) _titleText.text = title;
            if (_cornerTitle != null) _cornerTitle.text = title;
        }

        /// <summary>La grande carte est-elle ouverte ?</summary>
        public bool IsOpen => _large != null && _large.Root.activeSelf;

        /// <summary>La petite carte du coin est-elle affichée ?</summary>
        public bool IsCornerVisible => _corner != null && _corner.Root.activeSelf;

        private void Start()
        {
            _movement = _player != null ? _player.GetComponent<PlayerMovement>() : null;
            _cycle = FindAnyObjectByType<DayNightCycle>();
            _spotSources = FindObjectsByType<FishingSpots>();
            _legendSources = FindObjectsByType<LegendSpot>();
            for (int i = 0; i < CloudCount; i++) _cloudX[i] = i / (float)CloudCount;
            CreateSprites();
            BuildUi();
            Rebuild();
            SetOpen(false);
        }

        private void OnDestroy()
        {
            foreach (Texture2D frame in _frames)
            {
                if (frame != null) Destroy(frame);
            }
        }

        private void Update()
        {
            if (MapInput.TogglePressedThisFrame())
            {
                Toggle();
            }
            else if (IsOpen && MapInput.ClosePressedThisFrame())
            {
                SetOpen(false);
            }

            // Vaguelettes : on change d'image régulièrement (temps réel, la carte vit même en pause).
            _frameTimer += Time.unscaledDeltaTime;
            if (_frameTimer >= FrameSeconds && _frames[0] != null)
            {
                _frameTimer = 0f;
                _frame = (_frame + 1) % WaterFrames;
                if (_corner != null) _corner.Image.texture = _frames[_frame];
                if (_large != null) _large.Image.texture = _frames[_frame];
            }

            for (int i = 0; i < CloudCount; i++)
            {
                _cloudX[i] = Mathf.Repeat(_cloudX[i] + Time.unscaledDeltaTime * (0.012f + i * 0.004f), 1f);
            }

            UpdateView(IsOpen ? _large : _corner);
        }

        public void Toggle() => SetOpen(!IsOpen);

        /// <summary>Ouvre la grande carte (la petite se masque) ou revient à la petite carte du coin.</summary>
        public void SetOpen(bool open)
        {
            if (_large == null)
            {
                return;
            }

            _large.Root.SetActive(open);
            _corner.Root.SetActive(!open);
            UpdateView(open ? _large : _corner);
        }

        /// <summary>Position actuelle du joueur sur la carte (0..1). Utile pour les tests.</summary>
        public Vector2 PlayerNormalizedPosition
        {
            get
            {
                if (_player == null)
                {
                    return new Vector2(0.5f, 0.5f);
                }

                // Dans un bâtiment, la flèche reste sur sa porte.
                Vector2 position = InteriorBuilding.Occupied != null ? (Vector2)InteriorBuilding.Occupied.transform.position : (Vector2)_player.position;
                return MinimapMath.WorldToNormalized(position, _worldBounds);
            }
        }

        // ---------- Dessin de la carte ----------

        /// <summary>Redessine la carte de la zone actuelle (à rappeler après un voyage ou si la carte change).</summary>
        public void Rebuild()
        {
            if (_map == null || _map.Land == null)
            {
                return;
            }

            _worldBounds = _map.WorldBounds;
            Tilemap reference = _map.Water != null ? _map.Water : _map.Land;
            Vector3Int min = reference.WorldToCell(_worldBounds.min + Vector2.one * 0.01f);
            Vector3Int max = reference.WorldToCell(_worldBounds.max - Vector2.one * 0.01f);
            int w = max.x - min.x + 1;
            int h = max.y - min.y + 1;

            var lookup = new Dictionary<TileBase, Color>();
            foreach (TileColor entry in _palette)
            {
                if (entry.Tile != null) lookup[entry.Tile] = entry.Color;
            }

            // Les cases : terre ou eau, leur couleur, et l'arbre ou le décor posé dessus.
            var land = new float[w, h];
            var ground = new Color[w, h];
            var decor = new Color?[w, h];
            var shallow = new bool[w, h];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var cell = new Vector3Int(min.x + x, min.y + y, 0);
                    TileBase landTile = _map.Land.GetTile(cell);
                    if (landTile != null)
                    {
                        land[x, y] = 1f;
                        ground[x, y] = lookup.TryGetValue(landTile, out Color c) ? c : _unknownLandColor;
                        if (IsPaved(cell)) ground[x, y] = _pavingColor;
                    }
                    else
                    {
                        TileBase water = _map.Water != null ? _map.Water.GetTile(cell) : null;
                        ground[x, y] = water != null && lookup.TryGetValue(water, out Color c) ? c : _waterColor;
                        shallow[x, y] = HasAllCorners(_lagoon, cell);
                        if (shallow[x, y]) ground[x, y] = Color.Lerp(ground[x, y], _lagoonTint, 0.65f);
                    }

                    TileBase cliffTile = _cliffs != null ? _cliffs.GetTile(cell) : null;
                    if (cliffTile != null)
                    {
                        // Falaise : roche ; cascade : eau vive claire.
                        land[x, y] = 1f;
                        ground[x, y] = cliffTile.name.Contains("Cascade") ? new Color(0.70f, 0.88f, 0.94f) : _cliffColor;
                    }

                    TileBase decorTile = _decor != null ? _decor.GetTile(cell) : null;
                    if (decorTile != null && lookup.TryGetValue(decorTile, out Color d)) decor[x, y] = d;
                }
            }

            float[,] depth = WaterDepth(land, 4);
            // Lagon (atoll, récif) : eau claire même au milieu, comme en jeu.
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (shallow[x, y]) depth[x, y] = Mathf.Min(depth[x, y], 0.6f);
                }
            }

            int d4 = _detail;
            int tw = w * d4;
            int th = h * d4;
            var pixels = new Color[tw * th];
            var waterMask = new bool[tw * th];
            for (int px = 0; px < tw; px++)
            {
                for (int py = 0; py < th; py++)
                {
                    float fx = (px + 0.5f) / d4 - 0.5f;
                    float fy = (py + 0.5f) / d4 - 0.5f;
                    float landness = MinimapMath.Bilinear(land, fx, fy);
                    int cx = Mathf.Clamp(px / d4, 0, w - 1);
                    int cy = Mathf.Clamp(py / d4, 0, h - 1);
                    Color color;
                    if (landness >= 0.5f)
                    {
                        color = NearestLandColor(land, ground, cx, cy);
                        // Grain du sol, par petits paliers.
                        float grain = Mathf.Round(Hash(px, py, 3) * 3f) / 3f;
                        color = Shade(color, 0.95f + grain * 0.08f);
                        if (landness < 0.62f) color = Shade(color, 0.86f); // bord mouillé
                    }
                    else
                    {
                        Color water = land[cx, cy] > 0.5f ? NearestWaterColor(land, ground, cx, cy) : ground[cx, cy];
                        float t = Mathf.Clamp01(MinimapMath.Bilinear(depth, fx, fy) / 4f);
                        color = Color.Lerp(Color.Lerp(water, Color.white, 0.30f), Shade(water, 0.82f), Mathf.Round(t * 4f) / 4f);
                        if (landness > 0.36f) color = Color.Lerp(color, Color.white, 0.55f); // écume au ras de la côte
                        else waterMask[py * tw + px] = true;
                    }

                    pixels[py * tw + px] = color;
                }
            }

            // Arbres et décor : petite couronne ronde, éclairée en haut à gauche, avec son ombre.
            float radius = d4 * 0.55f;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (!decor[x, y].HasValue) continue;
                    Color crown = decor[x, y].Value;
                    float ccx = (x + 0.5f) * d4;
                    float ccy = (y + 0.5f) * d4 + d4 * 0.25f;
                    Disc(pixels, tw, th, ccx + 1f, ccy - 1.5f, radius, c => Shade(c, 0.72f), waterMask);
                    Disc(pixels, tw, th, ccx, ccy, radius, c => crown, waterMask, crown);
                }
            }

            // Grands repères : surface au sol en couleur pleine (bord plus sombre), montagnes en triangle enneigé.
            foreach (Block block in _blocks)
            {
                if (block == null || !block.Area.Overlaps(_worldBounds)) continue;
                float x0 = (block.Area.xMin - min.x) * d4, x1 = (block.Area.xMax - min.x) * d4;
                float y0 = (block.Area.yMin - min.y) * d4, y1 = (block.Area.yMax - min.y) * d4;
                for (int px = Mathf.Max(0, Mathf.FloorToInt(x0)); px < Mathf.Min(tw, Mathf.CeilToInt(x1)); px++)
                {
                    for (int py = Mathf.Max(0, Mathf.FloorToInt(y0)); py < Mathf.Min(th, Mathf.CeilToInt(y1)); py++)
                    {
                        float u = (px + 0.5f - x0) / (x1 - x0), v = (py + 0.5f - y0) / (y1 - y0);
                        Color c = block.Color;
                        if (block.Mountain)
                        {
                            float half = (1f - v) * 0.5f;
                            if (Mathf.Abs(u - 0.5f) > half) continue;
                            if (v > 0.62f) c = Color.white;                       // neige du sommet
                            else if (u > 0.5f) c = Shade(c, 0.8f);                // flanc à l'ombre
                        }
                        else if (u < 0.12f || u > 0.88f || v < 0.15f || v > 0.85f)
                        {
                            c = Shade(c, 0.75f);
                        }

                        pixels[py * tw + px] = c;
                        waterMask[py * tw + px] = false;
                    }
                }
            }

            for (int f = 0; f < WaterFrames; f++)
            {
                if (_frames[f] != null) Destroy(_frames[f]);
                _frames[f] = new Texture2D(tw, th, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var frame = (Color[])pixels.Clone();
                float phase = f * Mathf.PI * 2f / WaterFrames;
                for (int i = 0; i < frame.Length; i++)
                {
                    if (!waterMask[i]) continue;
                    int px = i % tw;
                    int py = i / tw;
                    // Petites vagues en diagonale qui avancent, quelques reflets.
                    float wave = Mathf.Sin(px * 0.45f + py * 0.9f - phase);
                    if (wave > 0.94f && Hash(px / 3, py, 7) > 0.55f) frame[i] = Color.Lerp(frame[i], Color.white, 0.35f);
                    else if (Hash(px, py, 11 + f) > 0.996f) frame[i] = Color.Lerp(frame[i], Color.white, 0.7f);
                }

                _frames[f].SetPixels(frame);
                _frames[f].Apply();
            }

            var cellCount = new Vector2(w, h);
            ApplyTexture(_large, cellCount * _pixelsPerCell);
            ApplyTexture(_corner, cellCount * _cornerPixelsPerCell);
        }

        /// <summary>
        /// Case pavée ? Les pavés sont dessinés en double grille (décalée d'une demi-case) : une case est pavée quand les
        /// quatre tuiles de rendu qui la touchent existent.
        /// </summary>
        private bool IsPaved(Vector3Int cell) => HasAllCorners(_paving, cell);

        /// <summary>Les quatre tuiles de rendu (double grille) qui touchent cette case existent sur cette couche.</summary>
        private static bool HasAllCorners(Tilemap layer, Vector3Int cell)
        {
            if (layer == null) return false;
            for (int dx = -1; dx <= 0; dx++)
            {
                for (int dy = -1; dy <= 0; dy++)
                {
                    if (layer.GetTile(cell + new Vector3Int(dx, dy, 0)) == null) return false;
                }
            }

            return true;
        }

        /// <summary>Distance (en cases) de chaque case d'eau à la terre la plus proche, limitée à <paramref name="cap"/>.</summary>
        private static float[,] WaterDepth(float[,] land, int cap)
        {
            int w = land.GetLength(0);
            int h = land.GetLength(1);
            var depth = new float[w, h];
            var queue = new Queue<Vector2Int>();
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (land[x, y] > 0.5f) queue.Enqueue(new Vector2Int(x, y));
                    else depth[x, y] = cap;
                }
            }

            var steps = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                foreach (Vector2Int s in steps)
                {
                    Vector2Int n = c + s;
                    if (n.x < 0 || n.y < 0 || n.x >= w || n.y >= h) continue;
                    float next = depth[c.x, c.y] + 1f;
                    if (next < depth[n.x, n.y])
                    {
                        depth[n.x, n.y] = next;
                        queue.Enqueue(n);
                    }
                }
            }

            return depth;
        }

        private static Color NearestLandColor(float[,] land, Color[,] ground, int x, int y)
        {
            return NearestOf(land, ground, x, y, true);
        }

        private static Color NearestWaterColor(float[,] land, Color[,] ground, int x, int y)
        {
            return NearestOf(land, ground, x, y, false);
        }

        private static Color NearestOf(float[,] land, Color[,] ground, int x, int y, bool wantLand)
        {
            int w = land.GetLength(0);
            int h = land.GetLength(1);
            for (int r = 0; r <= 2; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        if ((land[nx, ny] > 0.5f) == wantLand) return ground[nx, ny];
                    }
                }
            }

            return ground[x, y];
        }

        /// <summary>Disque plein (couronne d'arbre ou ombre), éclairé en haut à gauche si <paramref name="lit"/> est donné.</summary>
        private static void Disc(Color[] pixels, int tw, int th, float cx, float cy, float r, Func<Color, Color> paint, bool[] waterMask, Color? lit = null)
        {
            for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
            {
                for (int y = Mathf.FloorToInt(cy - r); y <= Mathf.CeilToInt(cy + r); y++)
                {
                    if (x < 0 || y < 0 || x >= tw || y >= th) continue;
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy > r * r) continue;
                    int i = y * tw + x;
                    Color c = paint(pixels[i]);
                    if (lit.HasValue)
                    {
                        float light = -dx + dy;
                        c = light > r * 0.6f ? Shade(lit.Value, 1.25f) : light < -r * 0.6f ? Shade(lit.Value, 0.8f) : lit.Value;
                    }

                    pixels[i] = c;
                    waterMask[i] = false;
                }
            }
        }

        private static Color Shade(Color c, float light)
        {
            return light < 1f ? Color.Lerp(c, new Color(c.r * 0.4f, c.g * 0.45f, c.b * 0.6f), 1f - light) : Color.Lerp(c, Color.white, light - 1f);
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

        private void ApplyTexture(MapView view, Vector2 size)
        {
            if (view == null)
            {
                return;
            }

            view.Image.texture = _frames[_frame];
            view.Image.rectTransform.sizeDelta = size;
            foreach (Image cloud in view.Clouds)
            {
                cloud.rectTransform.sizeDelta = new Vector2(size.x * 0.32f, size.x * 0.18f);
            }

            // La petite carte : son cadre s'ajuste à l'image.
            if (view == _corner)
            {
                ((RectTransform)view.Root.transform).sizeDelta = size + new Vector2(40f, 96f);
            }
        }

        // ---------- Animation ----------

        private void UpdateView(MapView view)
        {
            if (view == null || !view.Root.activeSelf)
            {
                return;
            }

            Vector2 size = view.Image.rectTransform.sizeDelta;
            Vector2 OnMap(Vector2 normalized) => new Vector2((normalized.x - 0.5f) * size.x, (normalized.y - 0.5f) * size.y);

            // Teinte de l'heure : la carte s'assombrit et bleuit la nuit, comme le monde.
            Color sky = _cycle != null ? DayCycleMath.SkyColor(_cycle.Hour) : Color.white;
            Color tint = Color.Lerp(Color.white, sky, 0.6f);
            tint.a = 1f;
            view.Image.color = tint;

            // Joueur : flèche dans le sens du regard, onde qui s'élargit.
            Vector2 at = OnMap(PlayerNormalizedPosition);
            view.Arrow.anchoredPosition = at;
            view.Arrow.localRotation = Quaternion.Euler(0f, 0f, MinimapMath.FacingAngle(_movement != null ? _movement.Facing : Vector2.down));
            Vector2 pulse = MinimapMath.Pulse(Time.unscaledTime);
            view.Ring.anchoredPosition = at;
            view.Ring.localScale = Vector3.one * pulse.x;
            Color ring = Color.white;
            ring.a = pulse.y * 0.8f;
            view.RingImage.color = ring;

            for (int i = 0; i < view.Landmarks.Count && i < _landmarks.Count; i++)
            {
                Transform target = _landmarks[i].Target;
                bool here = target != null && _worldBounds.Contains((Vector2)target.position);
                view.Landmarks[i].gameObject.SetActive(here);
                if (here) view.Landmarks[i].anchoredPosition = OnMap(MinimapMath.WorldToNormalized(target.position, _worldBounds));
            }

            UpdateSpots(view, OnMap);

            // Ombres de nuages qui traversent la carte, plus discrètes la nuit.
            float cloudAlpha = 0.16f * Mathf.Clamp01(sky.grayscale * 1.4f);
            for (int i = 0; i < view.Clouds.Count; i++)
            {
                Image cloud = view.Clouds[i];
                float x = (_cloudX[i] * 1.4f - 0.7f) * size.x;
                float y = ((i * 0.37f + 0.2f) % 1f - 0.5f) * size.y;
                cloud.rectTransform.anchoredPosition = new Vector2(x, y);
                cloud.color = new Color(0.02f, 0.05f, 0.10f, cloudAlpha);
            }
        }

        private void UpdateSpots(MapView view, Func<Vector2, Vector2> onMap)
        {
            int used = 0;
            int region = _map != null ? _map.CurrentRegion : -1;
            foreach (FishingSpots source in _spotSources)
            {
                if (source == null || source.Region != region || !source.isActiveAndEnabled) continue;
                foreach (Vector2 center in source.Centers)
                {
                    if (used >= view.Spots.Count) break;
                    Image marker = view.Spots[used++];
                    marker.gameObject.SetActive(true);
                    marker.rectTransform.anchoredPosition = onMap(MinimapMath.WorldToNormalized(center, _worldBounds));
                    var color = new Color(0.92f, 0.98f, 1f); // bulles : blanc bleuté
                    float beat = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f + used);
                    color.a = Mathf.Lerp(0.55f, 1f, beat);
                    marker.color = color;
                    marker.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.15f, beat);
                }
            }

            // Le remous d'une légende réveillée : gros point doré qui bat plus fort.
            foreach (LegendSpot legend in _legendSources)
            {
                if (legend == null || !legend.Active || used >= view.Spots.Count) continue;
                Image marker = view.Spots[used++];
                marker.gameObject.SetActive(true);
                marker.rectTransform.anchoredPosition = onMap(MinimapMath.WorldToNormalized(legend.Center, _worldBounds));
                float beat = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                marker.color = Color.Lerp(new Color(1f, 0.72f, 0.20f), new Color(1f, 0.30f, 0.22f), beat);
                marker.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.5f, 2.1f, beat);
            }

            for (int i = used; i < view.Spots.Count; i++)
            {
                view.Spots[i].gameObject.SetActive(false);
            }
        }

        // ---------- Interface ----------

        private void BuildUi()
        {
            var canvasObject = new GameObject("Carte (minimap)", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();
            UiFactory.UseSkin(_skin);

            _corner = BuildCornerView(root);
            _large = BuildLargeView(root);
        }

        private Image Frame(RectTransform parent, string name)
        {
            return _skin != null && _skin.Panel != null
                ? UiFactory.Sliced(parent, name, _skin.Panel, Color.white)
                : UiFactory.Plain(parent, name, new Color(0.07f, 0.10f, 0.14f, 1f));
        }

        /// <summary>Petite carte permanente, ancrée en bas à droite de l'écran, avec le nom de la zone.</summary>
        private MapView BuildCornerView(RectTransform root)
        {
            Image frame = Frame(root, "Petite carte");
            RectTransform rect = frame.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-20f, 20f);

            MapView view = CreateView(frame.gameObject, rect, 14f, new Vector2(0f, 0f));

            _cornerTitle = UiFactory.Label(rect, "Zone", 22, TextAnchor.MiddleCenter, new Color(1f, 0.90f, 0.62f));
            _cornerTitle.text = _title;
            UiFactory.Place(_cornerTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(0f, 28f));

            Text hint = UiFactory.Label(rect, "Agrandir", 20, TextAnchor.MiddleCenter, new Color(0.82f, 0.86f, 0.90f));
            hint.text = "M : agrandir";
            UiFactory.Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(0f, 26f));
            return view;
        }

        /// <summary>Grande carte au centre, ouverte avec M : plaque du nom de la zone, rose des vents, légende.</summary>
        private MapView BuildLargeView(RectTransform root)
        {
            Image frame = Frame(root, "Grande carte");
            RectTransform rect = frame.rectTransform;
            rect.anchoredPosition = new Vector2(0f, -30f);
            rect.sizeDelta = new Vector2(700f, 600f);

            MapView view = CreateView(frame.gameObject, rect, 20f, new Vector2(0f, -4f));

            _titleText = UiFactory.TitlePlaque(rect, _skin, _title, 34, new Vector2(0f, 300f), new Vector2(440f, 70f), out _);

            // Rose des vents : une petite flèche vers le nord et un N.
            var north = new GameObject("Nord", typeof(RectTransform), typeof(Image));
            north.transform.SetParent(rect, false);
            var northImage = north.GetComponent<Image>();
            northImage.sprite = _arrowSprite;
            northImage.color = new Color(1f, 0.88f, 0.55f);
            UiFactory.Center(northImage.rectTransform, new Vector2(300f, 236f), new Vector2(26f, 26f));
            Text n = UiFactory.Label(rect, "N", 24, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.55f));
            n.text = "N";
            UiFactory.Center(n.rectTransform, new Vector2(300f, 262f), new Vector2(40f, 30f));

            Text legend = UiFactory.Label(rect, "Légende", 20, TextAnchor.MiddleCenter, new Color(0.84f, 0.88f, 0.92f));
            legend.text = "Flèche : toi   •   Ronds : bulles pour l'épuisette   •   M ou Échap : fermer";
            UiFactory.Center(legend.rectTransform, new Vector2(0f, -262f), new Vector2(600f, 30f));
            return view;
        }

        private MapView CreateView(GameObject rootObject, RectTransform parent, float markerSize, Vector2 imageOffset)
        {
            var imageObject = new GameObject("Carte", typeof(RectTransform), typeof(RawImage), typeof(RectMask2D));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<RawImage>();
            image.rectTransform.anchoredPosition = imageOffset;

            var view = new MapView { Root = rootObject, Image = image, MarkerSize = markerSize };

            for (int i = 0; i < CloudCount; i++)
            {
                Image cloud = UiFactory.Plain(image.rectTransform, "Ombre de nuage", Color.clear);
                cloud.sprite = _cloudSprite;
                cloud.raycastTarget = false;
                view.Clouds.Add(cloud);
            }

            for (int i = 0; i < SpotMarkers; i++)
            {
                Image spot = UiFactory.Plain(image.rectTransform, "Coin de pêche", Color.white);
                spot.sprite = _spotSprite;
                spot.rectTransform.sizeDelta = Vector2.one * markerSize * 1.4f;
                spot.gameObject.SetActive(false);
                view.Spots.Add(spot);
            }

            foreach (Landmark place in _landmarks)
            {
                Image landmark = UiFactory.Plain(image.rectTransform, string.IsNullOrEmpty(place.Name) ? "Lieu" : place.Name, place.Icon != null ? Color.white : _landmarkColor);
                if (place.Icon != null)
                {
                    landmark.sprite = place.Icon;
                    landmark.rectTransform.sizeDelta = new Vector2(markerSize * 2.6f, markerSize * 2.6f);
                }
                else
                {
                    landmark.rectTransform.sizeDelta = new Vector2(markerSize * 0.9f, markerSize * 0.9f);
                    landmark.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                }

                view.Landmarks.Add(landmark.rectTransform);
            }

            Image ring = UiFactory.Plain(image.rectTransform, "Onde", Color.white);
            ring.sprite = _ringSprite;
            ring.rectTransform.sizeDelta = Vector2.one * markerSize * 1.2f;
            view.Ring = ring.rectTransform;
            view.RingImage = ring;

            Image arrow = UiFactory.Plain(image.rectTransform, "Toi", _playerColor);
            arrow.sprite = _arrowSprite;
            arrow.color = _playerColor;
            arrow.rectTransform.sizeDelta = Vector2.one * markerSize * 1.5f;
            view.Arrow = arrow.rectTransform;
            return view;
        }

        // ---------- Petits dessins de la carte (faits au démarrage) ----------

        private void CreateSprites()
        {
            _arrowSprite = MakeSprite(16, (x, y) =>
            {
                // Flèche vers le haut : pointe en haut, encoche en bas. Intérieur blanc (teinté par l'image), bord sombre.
                float u = Mathf.Abs(x + 0.5f - 8f);
                float top = 15f - u * 1.6f;
                float notch = 3f + u * 0.7f;
                if (y > top || y < notch || u > 7f) return Color.clear;
                bool rim = y > top - 1.6f || y < notch + 1.4f || u > 5.6f;
                return rim ? new Color(0.12f, 0.06f, 0.06f) : Color.white;
            });
            _ringSprite = MakeSprite(32, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f));
                return d > 13f && d < 15.5f ? Color.white : Color.clear;
            });
            _spotSprite = MakeSprite(16, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(8f, 8f));
                if (d > 7.6f) return Color.clear;
                if (d > 6.4f) return new Color(0.05f, 0.10f, 0.16f);
                if (d > 4.4f) return Color.white;
                if (d < 2.2f) return Color.white;
                return new Color(1f, 1f, 1f, 0.3f);
            });
            _cloudSprite = MakeSprite(32, (x, y) =>
            {
                float a = 0f;
                foreach (Vector3 blob in new[] { new Vector3(11f, 14f, 9f), new Vector3(20f, 16f, 10f), new Vector3(16f, 20f, 8f) })
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(blob.x, blob.y)) / blob.z;
                    a = Mathf.Max(a, Mathf.Clamp01(1.2f - d));
                }

                return new Color(1f, 1f, 1f, Mathf.Round(a * 3f) / 3f);
            });
        }

        private static Sprite MakeSprite(int size, Func<int, int, Color> pixel)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    texture.SetPixel(x, y, pixel(x, y));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
