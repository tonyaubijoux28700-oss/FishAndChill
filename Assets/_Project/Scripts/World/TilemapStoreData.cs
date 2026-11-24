using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FishAndChill.World
{
    /// <summary>
    /// Les tuiles de la carte, rangées à part de la scène sous forme compacte : pour chaque Tilemap, la liste des tuiles
    /// utilisées et des suites de cases identiques sur une même ligne (x, y, longueur, n° de tuile). Écrites par la
    /// génération, reposées au lancement par <see cref="TilemapStore"/>. Évite d'avoir 150 000 tuiles en clair dans la scène.
    /// </summary>
    [CreateAssetMenu(menuName = "FishAndChill/Tuiles de la carte", fileName = "Carte_Tuiles")]
    public sealed class TilemapStoreData : ScriptableObject
    {
        [Serializable]
        public sealed class Layer
        {
            [Tooltip("Nom de l'objet Tilemap, enfant de la carte.")] public string Name;
            public TileBase[] Palette = new TileBase[0];
            [Tooltip("Suites de 4 entiers : x, y, longueur, n° de tuile dans la palette.")] public int[] Runs = new int[0];
        }

        [SerializeField] private List<Layer> _layers = new List<Layer>();

        public IReadOnlyList<Layer> Layers => _layers;

        public void SetLayers(List<Layer> layers) => _layers = layers;

        /// <summary>Range une Tilemap en suites de cases identiques, ligne par ligne.</summary>
        public static Layer Pack(string name, Tilemap map)
        {
            var palette = new List<TileBase>();
            var index = new Dictionary<TileBase, int>();
            var runs = new List<int>();
            map.CompressBounds();
            BoundsInt b = map.cellBounds;
            TileBase[] all = map.GetTilesBlock(b);
            for (int y = 0; y < b.size.y; y++)
            {
                int x = 0;
                while (x < b.size.x)
                {
                    TileBase tile = all[x + y * b.size.x];
                    if (tile == null)
                    {
                        x++;
                        continue;
                    }

                    int start = x;
                    while (x < b.size.x && all[x + y * b.size.x] == tile) x++;
                    if (!index.TryGetValue(tile, out int i))
                    {
                        i = palette.Count;
                        index[tile] = i;
                        palette.Add(tile);
                    }

                    runs.Add(b.xMin + start);
                    runs.Add(b.yMin + y);
                    runs.Add(x - start);
                    runs.Add(i);
                }
            }

            return new Layer { Name = name, Palette = palette.ToArray(), Runs = runs.ToArray() };
        }

        /// <summary>Repose les tuiles d'une couche dans une Tilemap (vide).</summary>
        public static void Unpack(Layer layer, Tilemap map)
        {
            int count = 0;
            for (int r = 2; r < layer.Runs.Length; r += 4) count += layer.Runs[r];
            var positions = new Vector3Int[count];
            var tiles = new TileBase[count];
            int k = 0;
            for (int r = 0; r + 3 < layer.Runs.Length; r += 4)
            {
                int x0 = layer.Runs[r], y = layer.Runs[r + 1], length = layer.Runs[r + 2];
                TileBase tile = layer.Palette[layer.Runs[r + 3]];
                for (int x = 0; x < length; x++)
                {
                    positions[k] = new Vector3Int(x0 + x, y, 0);
                    tiles[k] = tile;
                    k++;
                }
            }

            map.SetTiles(positions, tiles);
        }
    }
}
