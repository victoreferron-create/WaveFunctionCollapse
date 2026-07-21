namespace WaveFunctionCollapse
{
    public enum TileType
    {
        Plains,
        Beach,
        Sea,
        DeepSea,
        Mountain,
        None // default if generation algorithm fails to find the good tile type for a space or if the world has been initialized using the World(sizeX, sizeY) constructor
    }

    public class World
    {
        private TileType[,] grid;

        public World(long seed, uint sizeX, uint sizeY)
        {
            // TODO: create constructor to geneate a world using the seed provided in the seed parameter
        }

        public World(uint sizeX, uint sizeY)
        {
            // TODO: create constructor to generate an empty world
        }

        public TileType GetTileAt(uint x, uint y)
        {
            return grid[x, y];
        }
    }
}