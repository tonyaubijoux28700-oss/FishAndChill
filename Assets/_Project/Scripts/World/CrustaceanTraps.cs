using System.Collections.Generic;
using System.Text;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Gear;
using FishAndChill.Player;
using FishAndChill.Save;
using FishAndChill.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace FishAndChill.World
{
    /// <summary>
    /// Les casiers à crustacés dans le monde : face à l'eau près de la rive, C (ou Haut sur la croix de la manette)
    /// pose un casier ; une bouée flotte au-dessus. Plus tard, C près de la bouée le relève : de 0 à 3 crustacés de la
    /// zone où il est posé (<see cref="TrapRules"/>), qui vont dans le sac comme une prise normale. L'attente se compte
    /// en heures de jeu (<see cref="DayNightCycle.TotalHours"/>), même quand on est ailleurs.
    /// </summary>
    public sealed class CrustaceanTraps : MonoBehaviour
    {
        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private PlayerMovement _player;
        [SerializeField] private FishingController _fishing;
        [SerializeField] private DayNightCycle _cycle;
        [SerializeField] private WorldMap _map;
        [SerializeField, Tooltip("Toutes les zones : un casier n'attrape que les crustacés de la sienne.")]
        private FishingZone[] _zones = new FishingZone[0];
        [SerializeField, Tooltip("Charte de l'interface (police).")] private UiSkin _skin;

        [SerializeField, Min(0.5f), Tooltip("Distance de la bouée pour relever le casier.")]
        private float _liftRadius = 2.2f;
        [SerializeField, Min(0.5f), Tooltip("Distance minimale entre deux casiers.")]
        private float _spacing = 1.5f;

        private readonly List<SpriteRenderer> _buoys = new List<SpriteRenderer>();
        private readonly System.Random _random = new System.Random();
        private Text _prompt;

        private TrapDefinition Trap => _progress != null ? _progress.Trap : null;

        private double Now => _cycle != null ? _cycle.TotalHours : 0.0;

        private void Start()
        {
            if (_progress == null || _player == null)
            {
                enabled = false;
                return;
            }

            _progress.EnsureCreated();
            BuildPrompt();
        }

        private void Update()
        {
            SyncBuoys();
            if (!SaveManager.InGame || Trap == null)
            {
                _prompt.gameObject.SetActive(false);
                return;
            }

            string prompt = null;
            bool free = !_player.Locked && InteriorBuilding.Occupied == null
                && (_fishing == null || _fishing.State == FishingState.Idle || _fishing.State == FishingState.Result);
            int near = free ? NearestTrap() : -1;
            Vector2 spot = default;
            bool canPlace = free && near < 0 && _progress.Traps.Available > 0 && TryFindSpot(out spot);
            if (near >= 0)
            {
                float left = TrapRules.HoursLeft(Now - _progress.Traps.Placed[near].PlacedAtHours, Trap.HoursToFull);
                prompt = left <= 0f ? "C : relever le casier (il est plein !)" : $"C : relever le casier (plein dans {Mathf.CeilToInt(left)} h)";
            }
            else if (canPlace)
            {
                prompt = $"C : poser un casier ({_progress.Traps.Available} dans ton sac)";
            }

            _prompt.gameObject.SetActive(prompt != null);
            if (prompt != null)
            {
                _prompt.text = prompt;
            }

            if (prompt == null || !Pressed())
            {
                return;
            }

            if (near >= 0)
            {
                Lift(near);
            }
            else
            {
                Place(spot);
            }
        }

        /// <summary>Pose un casier devant le joueur (tests et jeu) ; faux s'il n'y a pas d'eau ou plus de casier.</summary>
        public bool TryPlace()
        {
            return TryFindSpot(out Vector2 spot) && Place(spot);
        }

        /// <summary>Relève le casier le plus proche du joueur (tests et jeu) ; faux s'il n'y en a pas à portée.</summary>
        public bool TryLift()
        {
            int near = NearestTrap();
            return near >= 0 && Lift(near);
        }

        private bool Place(Vector2 spot)
        {
            FishingZone zone = _fishing != null ? _fishing.Zone : null;
            if (zone == null || !_progress.Traps.Place(zone.Id, spot.x, spot.y, Now))
            {
                return false;
            }

            Note($"Casier posé ! Reviens dans {Mathf.RoundToInt(Trap.HoursToFull)} h de jeu pour qu'il soit plein.\n"
                 + "Plus tu attends, plus il y aura de crustacés (et des rares).");
            return true;
        }

        private bool Lift(int index)
        {
            PlacedTrap placed = _progress.Traps.Placed[index];
            FishingZone zone = FindZone(placed.ZoneId);
            var candidates = new List<FishingZone.SpeciesEntry>();
            if (zone != null)
            {
                foreach (FishingZone.SpeciesEntry entry in zone.Species)
                {
                    if (entry.Species != null && entry.Species.Kind.CaughtInTrap()) candidates.Add(entry);
                }
            }

            float fill = TrapRules.Fill(Now - placed.PlacedAtHours, Trap.HoursToFull);
            List<FishingZone.SpeciesEntry> haul = TrapRules.Haul(candidates, e => e.Weight, e => e.Species.Rarity, fill, Trap.MaxCatch, _random);
            Inventory bag = _progress.Bag;
            if (haul.Count > 0 && bag.IsFull)
            {
                Note("Ton sac est plein : fais de la place avant de relever le casier.");
                return false;
            }

            _progress.Traps.Lift(index);
            if (haul.Count == 0)
            {
                Note(fill <= 0f ? "Le casier est encore vide : laisse-lui le temps de se remplir." : "Rien dans le casier cette fois… Repose-le et attends plus longtemps.");
                return true;
            }

            var text = new StringBuilder("Dans le casier :");
            var released = new List<string>();
            CatchLog log = _fishing != null ? _fishing.CatchLog : null;
            foreach (FishingZone.SpeciesEntry entry in haul)
            {
                FishSpecies species = entry.Species;
                Vector2 range = species.SizeRangeCm;
                float size = Mathf.Lerp(range.x, range.y, (float)_random.NextDouble());
                bool isNew = !_progress.Aquarium.Contains(species.Id) && (log == null || !log.IsDiscovered(species.Id));
                log?.Register(species.Id, size);
                if (bag.TryAdd(new CaughtFish(species, size)))
                {
                    text.Append($"\n{species.CommonName} ({species.Rarity.DisplayName()}) — {size:0} cm{(isNew ? "   Nouvelle espèce !" : string.Empty)}");
                }
                else
                {
                    released.Add(species.CommonName);
                }
            }

            if (released.Count > 0)
            {
                text.Append($"\nSac plein : tu relâches {string.Join(", ", released)}.");
            }

            Note(text.ToString(), 5f);
            return true;
        }

        private void Note(string message, float seconds = 3.5f)
        {
            _fishing?.ShowNote(message, seconds);
        }

        private FishingZone FindZone(string id)
        {
            foreach (FishingZone zone in _zones)
            {
                if (zone != null && zone.Id == id) return zone;
            }

            return _fishing != null && _fishing.Zone != null && _fishing.Zone.Id == id ? _fishing.Zone : null;
        }

        private int NearestTrap()
        {
            Vector2 at = _player.transform.position;
            return _progress.Traps.NearestPlaced(at.x, at.y, _liftRadius);
        }

        /// <summary>Le point d'eau juste devant le joueur (à 1 à 2,5 cases), assez loin des autres casiers.</summary>
        private bool TryFindSpot(out Vector2 spot)
        {
            Vector2 origin = _player.transform.position;
            Vector2 facing = _player.Facing.sqrMagnitude > 0.01f ? _player.Facing.normalized : Vector2.down;
            for (float d = 1f; d <= 2.5f; d += 0.25f)
            {
                Vector2 point = origin + facing * d;
                if (!IsWater(point) || !IsWater(point + facing * 0.45f) || TooCloseToATrap(point)) continue;
                spot = point;
                return true;
            }

            spot = default;
            return false;
        }

        private bool TooCloseToATrap(Vector2 point)
        {
            foreach (PlacedTrap other in _progress.Traps.Placed)
            {
                if (Vector2.Distance(point, new Vector2(other.X, other.Y)) < _spacing) return true;
            }

            return false;
        }

        private bool IsWater(Vector2 point) => _map != null ? _map.IsWater(point) : _fishing == null || _fishing.IsWater(point);

        private static bool Pressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                KeyControl key = keyboard.FindKeyOnCurrentKeyboardLayout("c") ?? keyboard.cKey;
                if (key != null && key.wasPressedThisFrame) return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.dpad.up.wasPressedThisFrame;
        }

        // ---------- Bouées ----------

        /// <summary>Une bouée par casier posé, qui danse doucement sur l'eau.</summary>
        private void SyncBuoys()
        {
            IReadOnlyList<PlacedTrap> placed = _progress.Traps.Placed;
            Sprite[] frames = Trap != null ? Trap.BuoyFrames : null;
            while (_buoys.Count < placed.Count)
            {
                var go = new GameObject("Bouée de casier");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = -5; // sur l'eau, sous le joueur
                _buoys.Add(renderer);
            }

            for (int i = 0; i < _buoys.Count; i++)
            {
                bool shown = i < placed.Count;
                _buoys[i].gameObject.SetActive(shown);
                if (!shown) continue;
                float phase = Time.time * 1.6f + i * 1.7f;
                if (frames != null && frames.Length > 0)
                {
                    _buoys[i].sprite = frames[Mathf.FloorToInt(phase / Mathf.PI) % frames.Length];
                }

                float bob = Mathf.Round(Mathf.Sin(phase) * 1.5f) / 32f; // au pixel près
                _buoys[i].transform.position = new Vector3(placed[i].X, placed[i].Y + bob, 0f);
            }
        }

        private void BuildPrompt()
        {
            UiFactory.UseSkin(_skin);
            var canvasObject = new GameObject("Interface des casiers", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();
            _prompt = UiFactory.Label(root, "Invite du casier", 32, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Bold(_prompt);
            _prompt.rectTransform.anchorMin = _prompt.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _prompt.rectTransform.anchoredPosition = new Vector2(0f, 250f);
            _prompt.rectTransform.sizeDelta = new Vector2(900f, 50f);
            _prompt.gameObject.SetActive(false);
        }
    }
}
