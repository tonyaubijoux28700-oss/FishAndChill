namespace FishAndChill.World
{
    /// <summary>
    /// Génère un lac sur une grille : la forme d'une île retournée. De l'eau au milieu, une rive de galets, des prés
    /// tout autour jusqu'aux bords de la carte. Même graine = même lac (logique pure, testée).
    /// </summary>
    public static class LakeGenerator
    {
        public static TerrainType[,] Generate(IslandGenerator.Settings settings)
        {
            TerrainType[,] island = IslandGenerator.Generate(settings);
            int width = island.GetLength(0);
            int height = island.GetLength(1);
            var lake = new TerrainType[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    switch (island[x, y])
                    {
                        case TerrainType.Grass:
                            lake[x, y] = TerrainType.Water;
                            break;
                        case TerrainType.Sand:
                            lake[x, y] = TerrainType.Sand; // rive de galets
                            break;
                        default:
                            lake[x, y] = TerrainType.Grass;
                            break;
                    }
                }
            }

            return lake;
        }
    }
}
