using System.Collections.Generic;
using FishAndChill.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FishAndChill.Editor
{
    /// <summary>
    /// Fin de génération : range les tuiles de toutes les Tilemaps de la carte dans <c>Data/Carte_Tuiles.asset</c>
    /// (forme compacte) puis vide les Tilemaps de la scène, que <see cref="TilemapStore"/> remplit au lancement.
    /// La scène passe ainsi d'environ 60 Mo à moins de 2 Mo.
    /// </summary>
    public static class TilemapExport
    {
        public const string DataPath = "Assets/_Project/Data/Carte_Tuiles.asset";

        public static void StoreMapTiles()
        {
            GameObject map = GameObject.Find("Carte");
            if (map == null)
            {
                Debug.LogError("[FishAndChill] Carte introuvable : tuiles non rangées.");
                return;
            }

            var store = map.GetComponent<TilemapStore>();
            if (store == null) store = map.AddComponent<TilemapStore>();

            var data = AssetDatabase.LoadAssetAtPath<TilemapStoreData>(DataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<TilemapStoreData>();
                AssetDatabase.CreateAsset(data, DataPath);
            }

            var layers = new List<TilemapStoreData.Layer>();
            int tiles = 0;
            foreach (Transform child in map.transform)
            {
                var tilemap = child.GetComponent<Tilemap>();
                if (tilemap == null || (child.gameObject.hideFlags & HideFlags.DontSave) != 0)
                {
                    continue;
                }

                tiles += tilemap.GetUsedTilesCount() > 0 ? CountTiles(tilemap) : 0;
                layers.Add(TilemapStoreData.Pack(child.name, tilemap));
                tilemap.ClearAllTiles();
            }

            foreach (Tilemap deeper in map.GetComponentsInChildren<Tilemap>(true))
            {
                if (deeper.transform.parent != map.transform && (deeper.gameObject.hideFlags & HideFlags.DontSave) == 0)
                {
                    Debug.LogWarning($"[FishAndChill] Tilemap « {deeper.name} » trop profonde : laissée dans la scène.");
                }
            }

            data.SetLayers(layers);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            var so = new SerializedObject(store);
            so.FindProperty("_data").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[FishAndChill] {tiles} tuiles rangées dans {DataPath} ({layers.Count} couches).");
        }

        /// <summary>Après la sauvegarde : remontre la carte dans l'éditeur (copies cachées).</summary>
        public static void RefreshPreview()
        {
            GameObject map = GameObject.Find("Carte");
            if (map != null && map.TryGetComponent(out TilemapStore store))
            {
                store.RefreshPreview();
            }
        }

        private static int CountTiles(Tilemap map)
        {
            int n = 0;
            foreach (Vector3Int p in map.cellBounds.allPositionsWithin)
            {
                if (map.HasTile(p)) n++;
            }

            return n;
        }
    }
}
