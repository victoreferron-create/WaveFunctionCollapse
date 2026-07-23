using System.Drawing;

namespace WaveFunctionCollapse
{
    public enum TileType
    {
        Plains,
        Beach,
        Sea,
        Forest,
        None // default if generation algorithm fails or before generation
    }

    public static class TileTypes
    {
        public static readonly TileType[] Any = [
            TileType.Sea,
            TileType.Beach,
            TileType.Plains,
            TileType.Forest,
        ];

        private static readonly Dictionary<TileType, ConsoleColor> tileColors = new()
        {
            { TileType.Sea,    ConsoleColor.Cyan },
            { TileType.Beach,  ConsoleColor.Yellow },
            { TileType.Plains, ConsoleColor.Green },
            { TileType.Forest, ConsoleColor.DarkGreen },
            { TileType.None,   ConsoleColor.Red },
        };

        private static readonly Dictionary<TileType, int> baseWeights = new()
        {
            { TileType.Sea,    4},
            { TileType.Beach,  3},
            { TileType.Plains, 10},
            { TileType.Forest, 6},
            { TileType.None,   1},
        };

        public static ConsoleColor GetColor(TileType tileType)
        {
            return tileColors[tileType];
        }

        public static int GetBaseWeight(TileType tileType)
        {
            return baseWeights[tileType];
        }
    }

    public class World
    {
        public int sizeX;
        public int sizeY;

        private readonly TileType[,] grid;
        private readonly WfcTile[,] wfcGrid;

        public World(uint sizeX, uint sizeY, int seed) : this(sizeX, sizeY)
        {
            Random rand = new(seed);

            List<WfcTile> toCollapse = [];
            HashSet<WfcTile> collapsed = [];

            toCollapse.Add(wfcGrid[0, 0]);

            while (toCollapse.Count != 0)
            {
                foreach (WfcTile tile in toCollapse)
                {
                    tile.Update();
                }

                WfcTile lowestEntropyTile = null!;
                byte lowestEntropyValue = byte.MaxValue;
                foreach (WfcTile tile in toCollapse)
                {
                    if (tile.Entropy < lowestEntropyValue)
                    {
                        lowestEntropyValue = tile.Entropy;
                        lowestEntropyTile = tile;
                    }
                }

                grid[lowestEntropyTile.X, lowestEntropyTile.Y] = lowestEntropyTile.Collapse(rand);
                toCollapse.Remove(lowestEntropyTile);
                collapsed.Add(lowestEntropyTile);

                Point[] neighborPositions =
                [
                    new Point((int)lowestEntropyTile.X - 1, (int)lowestEntropyTile.Y),
                    new Point((int)lowestEntropyTile.X + 1, (int)lowestEntropyTile.Y),
                    new Point((int)lowestEntropyTile.X, (int)lowestEntropyTile.Y - 1),
                    new Point((int)lowestEntropyTile.X, (int)lowestEntropyTile.Y + 1)
                ];

                foreach (Point pos in neighborPositions)
                {
                    if (pos.X >= 0 && pos.X < sizeX && pos.Y >= 0 && pos.Y < sizeY)
                    {
                        WfcTile neighborTile = wfcGrid[pos.X, pos.Y];

                        if (!collapsed.Contains(neighborTile) && !toCollapse.Contains(neighborTile))
                        {
                            toCollapse.Add(neighborTile);
                        }
                    }
                }
            }
        }

        public World(uint sizeX, uint sizeY)
        {
            this.sizeX = (int)sizeX;
            this.sizeY = (int)sizeY;

            this.grid = new TileType[sizeX, sizeY];
            this.wfcGrid = new WfcTile[sizeX, sizeY];

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    grid[x, y] = TileType.None;
                    wfcGrid[x, y] = new WfcTile((uint)x, (uint)y, this);
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
        private readonly uint x;
        private readonly uint y;
        private TileType[] neigbours;
        private List<TileType> possibleStates = [.. TileTypes.Any];
        private byte entropy;
        private readonly World world;
        private bool collapsed = false;

        public WfcTile(uint x, uint y, World world)
        {
            this.x = x;
            this.y = y;
            this.world = world;

            neigbours = GetNeigbours();
            entropy = (byte)possibleStates.Count;
        }

        public TileType Collapse(Random rand)
        {
            int tileTypeIndex = rand.Next(possibleStates.Count);
            TileType chosenTileType = possibleStates[tileTypeIndex];

            possibleStates = [chosenTileType];
            entropy = 1;
            collapsed = true;

            return chosenTileType;
        }

        public void Update()
        {
            if (collapsed) return;

            neigbours = GetNeigbours();

            possibleStates = [];

            HashSet<TileType> impossibleStates = [TileType.None];

            foreach (TileType type in neigbours)
            {
                switch (type)
                {
                    case TileType.Sea:
                        impossibleStates.Add(TileType.Plains);
                        impossibleStates.Add(TileType.Forest);
                        break;
                    case TileType.Beach:
                        impossibleStates.Add(TileType.Forest);
                        break;
                    case TileType.Plains:
                        impossibleStates.Add(TileType.Sea);
                        break;
                    case TileType.Forest:
                        impossibleStates.Add(TileType.Beach);
                        impossibleStates.Add(TileType.Sea);
                        break;
                    case TileType.None:
                        break;
                    default:
                        Console.WriteLine("What?");
                        break;
                }
            }

            List<TileType> validCandidates = [];

            foreach (TileType candidate in TileTypes.Any)
            {
                if (!impossibleStates.Contains(candidate))
                {
                    validCandidates.Add(candidate);
                }
            }

            if (validCandidates.Count == 0)
            {
                validCandidates.Add(TileType.None);
            }

            List<TileType> weightedPool = [];

            foreach(TileType candidate in validCandidates)
            {
                int weight = TileTypes.GetBaseWeight(candidate);

                foreach(TileType neighbour in neigbours)
                {
                    if (neighbour == candidate)
                    {
                        weight += 8;
                    }
                }

                for(int i = 0; i < weight; i++)
                {
                    weightedPool.Add(candidate);
                }
            }

            possibleStates = weightedPool;
            entropy = (byte)possibleStates.Distinct().Count();
        }

        public byte Entropy => entropy;
        public bool Collapsed => collapsed;
        public uint X => x;
        public uint Y => y;

        private TileType[] GetNeigbours()
        {
            TileType left = (X > 0) ? world.GetTileAt(X - 1, Y) : TileType.None;
            TileType right = (X < world.sizeX - 1) ? world.GetTileAt(X + 1, Y) : TileType.None;
            TileType up = (Y < world.sizeY - 1) ? world.GetTileAt(X, Y + 1) : TileType.None;
            TileType down = (Y > 0) ? world.GetTileAt(X, Y - 1) : TileType.None;

            return [left, right, up, down];
        }
    }
}