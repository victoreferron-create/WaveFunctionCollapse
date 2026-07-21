using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

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
        public int sizeX;
        public int sizeY;

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
        private List<TileType> possibleStates = new List<TileType>(TileTypes.Any);
        private byte entropy;
        private World world;
        private bool collapsed = false;

        public WfcTile(uint x, uint y, World world)
        {
            this.x = x;
            this.y = y;
            this.world = world;

            neigbours = GetNeigbours();

            entropy = (byte)possibleStates.Count;
        }

        public TileType CollapseTileType(Random rand)
        {

            int tileTypeIndex = rand.Next(possibleStates.Count);
            TileType chosenTileType = possibleStates[tileTypeIndex];

            possibleStates = new List<TileType> { chosenTileType };
            entropy = 1;
            collapsed = true;

            return chosenTileType;
        }

        public void Update()
        {

            neigbours = GetNeigbours();

            foreach (TileType type in neigbours)
            {
                switch (type)
                {
                    case (TileType.Sea):
                        possibleStates.Remove(TileType.Plains);
                        possibleStates.Remove(TileType.Forest);
                        break;
                    case (TileType.Beach):
                        possibleStates.Remove(TileType.Forest);
                        break;
                    case (TileType.Plains):
                        possibleStates.Remove(TileType.Sea);
                        break;
                    case (TileType.Forest):
                        possibleStates.Remove(TileType.Beach);
                        possibleStates.Remove(TileType.Sea);
                        break;
                    case (TileType.None):
                        break;
                    default:
                        Console.WriteLine("What?");
                        break;
                }
            }

            entropy = (byte)possibleStates.Count;
        }

        public byte Entropy => entropy;
        public bool Collapsed => collapsed;

        private TileType[] GetNeigbours()
        {
            TileType left = (x > 0) ? world.GetTileAt(x - 1, y) : TileType.None;
            TileType right = (x < world.sizeX - 1) ? world.GetTileAt(x + 1, y) : TileType.None;
            TileType up = (y < world.sizeY - 1) ? world.GetTileAt(x, y + 1) : TileType.None;
            TileType down = (y > 0) ? world.GetTileAt(x, y - 1) : TileType.None;

             return [left, right, up, down];
        }
    }
}