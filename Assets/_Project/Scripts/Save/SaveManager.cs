using System.Collections.Generic;
using System.IO;
using FishAndChill.Collection;
using FishAndChill.Fishing;
using FishAndChill.Player;
using FishAndChill.World;
using UnityEngine;

namespace FishAndChill.Save
{
    /// <summary>
    /// Sauvegarde automatique de la partie : chargée au démarrage, réécrite une seconde après chaque changement
    /// (prise, dépôt, vente, achat…) et en quittant. Fichier JSON dans le dossier de données du jeu
    /// (<see cref="Application.persistentDataPath"/>), écrit d'abord dans un fichier temporaire pour ne jamais
    /// laisser une sauvegarde à moitié écrite.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class SaveManager : MonoBehaviour
    {
        public const string FileName = "sauvegarde.json";

        [SerializeField] private PlayerProgress _progress;
        [SerializeField] private FishingController _fishing;
        [SerializeField] private ZoneTravel _travel;
        [SerializeField] private DayNightCycle _cycle;
        [SerializeField] private WeatherSystem _weather;

        [SerializeField, Tooltip("Zones dont les espèces peuvent figurer dans la sauvegarde.")]
        private FishingZone[] _zones = new FishingZone[0];

        [SerializeField, Min(0.1f), Tooltip("Délai avant d'écrire après un changement (regroupe les changements rapprochés).")]
        private float _saveDelay = 1f;

        private readonly Dictionary<string, FishSpecies> _species = new Dictionary<string, FishSpecies>();
        private bool _dirty;
        private bool _loading;
        private float _timer;

        /// <summary>
        /// Emplacement choisi sur l'écran titre (0 à 2), -1 tant qu'aucun n'est choisi : la scène démarre alors sur
        /// l'écran titre, sans charger ni écrire de sauvegarde. Garde sa valeur quand la scène est rechargée.
        /// </summary>
        public static int ActiveSlot { get; private set; } = -1;

        /// <summary>Chemin complet du fichier de sauvegarde de l'emplacement actif.</summary>
        public static string SavePath => PathFor(ActiveSlot);

        /// <summary>Chemin du fichier d'un emplacement.</summary>
        public static string PathFor(int slot) => Path.Combine(Application.persistentDataPath, SaveSlots.FileName(slot));

        /// <summary>Une partie est-elle en cours (emplacement choisi) ?</summary>
        public static bool InGame => ActiveSlot >= 0;

        private float _playSeconds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ActiveSlot = -1; // le rechargement du domaine est désactivé en éditeur
            PendingLook = null;
        }

        /// <summary>Apparence choisie à la création du personnage, appliquée à la nouvelle partie après le rechargement.</summary>
        public static CharacterLook PendingLook { get; set; }

        /// <summary>Prénom et apparence du pêcheur de la partie en cours.</summary>
        public CharacterLook Look { get; private set; } = new CharacterLook();

        /// <summary>Lit un emplacement sans le charger (écran titre) ; null s'il est vide ou illisible.</summary>
        public static SaveData Peek(int slot)
        {
            string path = PathFor(slot);
            try
            {
                return File.Exists(path) ? SaveSerializer.FromJson(File.ReadAllText(path)) : null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        /// <summary>Efface un emplacement.</summary>
        public static void DeleteSlot(int slot)
        {
            try
            {
                if (File.Exists(PathFor(slot)))
                {
                    File.Delete(PathFor(slot));
                }
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"[FishAndChill] Sauvegarde impossible à effacer : {exception.Message}");
            }
        }

        /// <summary>Lance la partie de cet emplacement (reprise, ou nouvelle partie s'il est vide) : recharge la scène.</summary>
        public void PlaySlot(int slot)
        {
            ActiveSlot = Mathf.Clamp(slot, 0, SaveSlots.Count - 1);
            Reload();
        }

        /// <summary>Sauvegarde, puis revient à l'écran titre.</summary>
        public void ReturnToTitle()
        {
            if (InGame)
            {
                Save();
            }

            ActiveSlot = -1;
            Reload();
        }

        private void Reload()
        {
            _dirty = false;
            enabled = false;
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.path);
        }

        /// <summary>La partie a été reprise depuis une sauvegarde.</summary>
        public bool Loaded { get; private set; }

        /// <summary>Zone enregistrée dans la sauvegarde (null si aucune) : ZoneTravel y ramène le joueur au démarrage.</summary>
        public string SavedZone { get; private set; }

        private void Awake()
        {
            if (_progress == null)
            {
                enabled = false;
                return;
            }

            foreach (FishingZone zone in _zones)
            {
                if (zone == null)
                {
                    continue;
                }

                foreach (FishingZone.SpeciesEntry entry in zone.Species)
                {
                    if (entry.Species != null)
                    {
                        _species[entry.Species.Id] = entry.Species;
                    }
                }
            }

            _progress.EnsureCreated();
            if (!InGame)
            {
                // Écran titre : rien n'est chargé ni écrit tant qu'un emplacement n'est pas choisi.
                enabled = false;
                return;
            }

            Load();
            _progress.Changed += MarkDirty;
            if (!Loaded)
            {
                Look = CharacterOptions.Sanitize(PendingLook);
                MarkDirty(); // emplacement vide : la nouvelle partie l'occupe tout de suite
            }

            PendingLook = null;
            PlayerAnimator player = FindAnyObjectByType<PlayerAnimator>();
            if (player != null)
            {
                player.SetLook(Look);
            }

            if (_fishing != null)
            {
                _fishing.CatchLog.Changed += MarkDirty;
            }

            if (_travel != null)
            {
                _travel.Traveled += MarkDirty;
            }
        }

        private void OnDestroy()
        {
            if (_progress != null)
            {
                _progress.Changed -= MarkDirty;
            }

            if (_fishing != null)
            {
                _fishing.CatchLog.Changed -= MarkDirty;
            }

            if (_travel != null)
            {
                _travel.Traveled -= MarkDirty;
            }
        }

        private void Update()
        {
            if (Time.timeScale > 0f)
            {
                _playSeconds += Time.unscaledDeltaTime;
            }

            if (!_dirty)
            {
                return;
            }

            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            if (InGame && enabled)
            {
                Save();
            }
        }

        private void MarkDirty()
        {
            if (_loading)
            {
                return;
            }

            _dirty = true;
            _timer = _saveDelay;
        }

        /// <summary>Écrit la sauvegarde maintenant.</summary>
        public void Save()
        {
            _dirty = false;
            CatchLog log = _fishing != null ? _fishing.CatchLog : new CatchLog();
            SaveData data = SaveSerializer.Capture(_progress.Bag, _progress.Aquarium, _progress.Wallet, _progress.Gear, log);
            data.Zone = _travel != null ? _travel.CurrentZoneId : null;
            data.PlaySeconds = _playSeconds;
            data.SavedAtTicks = System.DateTime.Now.Ticks;
            data.Hour = _cycle != null ? _cycle.Hour : 8f;
            data.Weather = _weather != null ? (int)_weather.Current : -1;
            data.WeatherHours = _weather != null ? _weather.HoursLeft : 0f;
            data.Rewards = new List<string>(_progress.ClaimedRewards);
            data.MetVillagers = Villagers.VillagerMemory.Capture();
            data.VillagerRequests = Villagers.VillagerRequests.Capture();
            data.Pets = new List<string>(_progress.Pets.Owned);
            data.EquippedPet = _progress.Pets.Equipped;
            foreach (string id in _progress.Baits.Owned)
            {
                data.Baits.Add(new SaveData.BaitStock { Id = id, Count = _progress.Baits.Count(id) });
            }

            data.EquippedBait = _progress.Baits.Equipped;
            foreach (string id in _progress.Legends.Ids)
            {
                data.Legends.Add(new SaveData.Legend { Id = id, Fragments = _progress.Legends.Fragments(id), Placed = _progress.Legends.Placed(id), State = (int)_progress.Legends.State(id) });
            }

            data.Look = Look;
            try
            {
                string temporary = SavePath + ".tmp";
                File.WriteAllText(temporary, SaveSerializer.ToJson(data));
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }

                File.Move(temporary, SavePath);
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"[FishAndChill] Sauvegarde impossible : {exception.Message}");
            }
        }

        /// <summary>Efface la sauvegarde et recharge la scène : la partie repart de zéro.</summary>
        public void StartNewGame()
        {
            _dirty = false;
            enabled = false;
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"[FishAndChill] Sauvegarde impossible à effacer : {exception.Message}");
            }

            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.path);
        }

        private void Load()
        {
            Villagers.VillagerMemory.Load(null);   // nouvelle partie : personne n'a encore été rencontré
            Villagers.VillagerRequests.Load(null);
            if (!File.Exists(SavePath))
            {
                return;
            }

            SaveData data;
            try
            {
                data = SaveSerializer.FromJson(File.ReadAllText(SavePath));
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"[FishAndChill] Sauvegarde illisible : {exception.Message}");
                return;
            }

            if (data == null)
            {
                Debug.LogWarning("[FishAndChill] Sauvegarde abîmée : nouvelle partie.");
                return;
            }

            _loading = true;
            CatchLog log = _fishing != null ? _fishing.CatchLog : new CatchLog();
            SaveSerializer.Restore(data, id => _species.TryGetValue(id, out FishSpecies species) ? species : null,
                _progress.Bag, _progress.Aquarium, _progress.Wallet, _progress.Gear, log);
            _loading = false;
            Loaded = true;
            SavedZone = data.Zone;
            Look = CharacterOptions.Sanitize(data.Look);
            _playSeconds = data.PlaySeconds;
            _progress.RestoreClaimedRewards(data.Rewards);
            if (data.Baits != null && data.Baits.Count > 0)
            {
                var stocks = new List<KeyValuePair<string, int>>();
                foreach (SaveData.BaitStock stock in data.Baits)
                {
                    stocks.Add(new KeyValuePair<string, int>(stock.Id, stock.Count));
                }

                _progress.Baits.Restore(stocks, data.EquippedBait);
            }

            _progress.EnsureDefaultBait();
            if (data.Legends != null)
            {
                foreach (SaveData.Legend legend in data.Legends)
                {
                    if (System.Enum.IsDefined(typeof(LegendState), legend.State))
                    {
                        _progress.Legends.Restore(legend.Id, legend.Fragments, legend.Placed, (LegendState)legend.State);
                    }
                }
            }

            if (_cycle != null)
            {
                _cycle.SetHour(data.Hour);
            }

            Villagers.VillagerMemory.Load(data.MetVillagers);
            Villagers.VillagerRequests.Load(data.VillagerRequests);
            _progress.Pets.Load(data.Pets, data.EquippedPet);
            if (_weather != null && data.Weather >= 0 && System.Enum.IsDefined(typeof(Weather), data.Weather))
            {
                _weather.SetWeather((Weather)data.Weather, data.WeatherHours);
            }
        }
    }
}
