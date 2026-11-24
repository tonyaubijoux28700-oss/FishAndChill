using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FishAndChill.World
{
    /// <summary>
    /// Posé sur la carte (la Grid) : les Tilemaps de la scène sont sauvegardées vides, leurs tuiles vivent dans
    /// <see cref="TilemapStoreData"/>. En jeu, elles sont reposées avant tout le reste (Awake en tout premier). Dans
    /// l'éditeur, des copies cachées, jamais sauvegardées, montrent la carte sans alourdir la scène.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-10000)]
    public sealed class TilemapStore : MonoBehaviour
    {
        private const string PreviewSuffix = " (aperçu)";

        [SerializeField, Tooltip("Les tuiles de la carte, écrites par la génération.")] private TilemapStoreData _data;

        private readonly List<GameObject> _previews = new List<GameObject>();

        public TilemapStoreData Data
        {
            get => _data;
            set => _data = value;
        }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                ClearPreviews();
                Load();
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                RefreshPreview();
            }
        }

        private void OnDisable() => ClearPreviews();

        /// <summary>Remplit les vraies Tilemaps (en jeu).</summary>
        private void Load()
        {
            if (_data == null)
            {
                return;
            }

            foreach (TilemapStoreData.Layer layer in _data.Layers)
            {
                Transform child = transform.Find(layer.Name);
                Tilemap map = child != null ? child.GetComponent<Tilemap>() : null;
                if (map != null && map.GetUsedTilesCount() == 0)
                {
                    TilemapStoreData.Unpack(layer, map);
                }
            }
        }

        /// <summary>Éditeur : refait les copies cachées qui montrent la carte (jamais sauvegardées).</summary>
        public void RefreshPreview()
        {
            ClearPreviews();
            if (Application.isPlaying || _data == null)
            {
                return;
            }

            foreach (TilemapStoreData.Layer layer in _data.Layers)
            {
                Transform source = transform.Find(layer.Name);
                if (source == null || source.GetComponent<Tilemap>() == null)
                {
                    continue;
                }

                GameObject copy = Instantiate(source.gameObject, transform);
                copy.name = layer.Name + PreviewSuffix;
                copy.hideFlags = HideFlags.HideAndDontSave;
                foreach (Transform t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.HideAndDontSave;
                Tilemap map = copy.GetComponent<Tilemap>();
                map.ClearAllTiles();
                TilemapStoreData.Unpack(layer, map);
                _previews.Add(copy);
            }
        }

        private void ClearPreviews()
        {
            foreach (GameObject p in _previews)
            {
                if (p != null) DestroyImmediate(p);
            }

            _previews.Clear();

            // Copies restées d'un rechargement (la liste est perdue, pas les objets cachés).
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform c = transform.GetChild(i);
                if ((c.gameObject.hideFlags & HideFlags.DontSave) != 0 && c.name.EndsWith(PreviewSuffix))
                {
                    DestroyImmediate(c.gameObject);
                }
            }
        }
    }
}
