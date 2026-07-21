namespace WaveFunctionCollapse
{
    public enum TileType
    {
        Plains,
        Beach,
        Sea,
        Forest,
        None, // default if generation algorithm fails to find the good tile type for a space or if the world has been initialized using the World(uint, uint) constructor
    }

    public static class TileTypes
    {
        public static readonly TileType[] Any = [
            TileType.Sea,
            TileType.Beach,
            TileType.Plains,
            TileType.Forest
            ];
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
            grid = new TileType[sizeX, sizeY];
            for (int x = 0; x < sizeX; x++) 
            {
                for (int y = 0; y < sizeY; y++) 
                {
                    grid[x, y] = TileType.None;
                }
            }
        }

        public TileType GetTileAt(uint x, uint y)
        {
            return grid[x, y];
        }
    }

    public class WfcTile
    {
        private uint x;
        private uint y;
        private TileType[] neigbours;
        private TileType[] possibleStates = TileTypes.Any;

        public WfcTile(uint x, uint y, World world)
        {
            this.x = x;
            this.y = y;
            neigbours = [
                world.GetTileAt(x - 1, y),
                world.GetTileAt(x + 1, y),
                world.GetTileAt(x, y + 1),
                world.GetTileAt(x, y - 1)
                ];
        }

        public TileType CollapseTileType(long seed)
        {
            // implement method to get the tile type of the tile
            return TileType.None;
        }
    }
}