using System.Collections.Generic;
using System.IO;
using FishAndChill.Collection;
using FishAndChill.Fishing;
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

        [SerializeField, Tooltip("Zones dont les espèces peuvent figurer dans la sauvegarde.")]
        private FishingZone[] _zones = new FishingZone[0];

        [SerializeField, Min(0.1f), Tooltip("Délai avant d'écrire après un changement (regroupe les changements rapprochés).")]
        private float _saveDelay = 1f;

        private readonly Dictionary<string, FishSpecies> _species = new Dictionary<string, FishSpecies>();
        private bool _dirty;
        private bool _loading;
        private float _timer;

        /// <summary>Chemin complet du fichier de sauvegarde.</summary>
        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>La partie a été reprise depuis une sauvegarde.</summary>
        public bool Loaded { get; private set; }

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
            Load();
            _progress.Changed += MarkDirty;
            if (_fishing != null)
            {
                _fishing.CatchLog.Changed += MarkDirty;
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
        }

        private void Update()
        {
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
            if (_dirty)
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

        private void Load()
        {
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
        }
    }
}
