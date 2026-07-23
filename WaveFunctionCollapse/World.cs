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
            { TileType.Sea,    6},
            { TileType.Beach,  30},
            { TileType.Plains, 10},
            { TileType.Forest, 4},
            { TileType.None,   0},
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

        public WfcTile[,] WfcGrid => wfcGrid;

        public World(uint sizeX, uint sizeY, int seed) : this(sizeX, sizeY)
        {
            Random rand = new(seed);

            List<WfcTile> uncollapsedTiles = new();
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    uncollapsedTiles.Add(wfcGrid[x, y]);
                }
            }

            while (uncollapsedTiles.Count > 0)
            {
                WfcTile lowestEntropyTile = null!;
                byte lowestEntropyValue = byte.MaxValue;

                foreach (WfcTile tile in uncollapsedTiles)
                {
                    if (tile.Entropy < lowestEntropyValue ||
                       (tile.Entropy == lowestEntropyValue && rand.Next(2) == 0))
                    {
                        lowestEntropyValue = tile.Entropy;
                        lowestEntropyTile = tile;
                    }
                }

                grid[lowestEntropyTile.X, lowestEntropyTile.Y] = lowestEntropyTile.Collapse(rand);
                uncollapsedTiles.Remove(lowestEntropyTile);

                Queue<WfcTile> propagationQueue = new();

                PushUncollapsedNeighbors(lowestEntropyTile, propagationQueue);

                while(propagationQueue.Count > 0)
                {
                    WfcTile current = propagationQueue.Dequeue();

                    byte oldEntropy = current.Entropy;
                    current.Update();

                    if(current.Entropy < oldEntropy)
                    {
                        PushUncollapsedNeighbors(current, propagationQueue);
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

        private void PushUncollapsedNeighbors(WfcTile tile, Queue<WfcTile> queue)
        {
            Point[] neighborPositions =
            [
                new Point((int)tile.X - 1, (int)tile.Y),
                new Point((int)tile.X + 1, (int)tile.Y),
                new Point((int)tile.X, (int)tile.Y - 1),
                new Point((int)tile.X, (int)tile.Y + 1)
            ];

            foreach (Point pos in neighborPositions)
            {
                if (pos.X >= 0 && pos.X < sizeX && pos.Y >= 0 && pos.Y < sizeY)
                {
                    WfcTile neighbor = WfcGrid[pos.X, pos.Y];
                    if (!neighbor.Collapsed && !queue.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
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

            neigbours = GetNeigbors();
            entropy = (byte)possibleStates.Count;
        }

        public TileType Collapse(Random rand)
        {
            if (possibleStates.Count == 0)
            {
                possibleStates = [TileType.None];
            }

            // Build the weighted pool ONLY when picking the winning tile
            List<TileType> weightedPool = new();

            foreach (TileType candidate in possibleStates)
            {
                int weight = TileTypes.GetBaseWeight(candidate);

                foreach (TileType neighbour in neigbours)
                {
                    if (neighbour == candidate)
                    {
                        weight += 2; // Bonus for matching neighbor type
                    }

                    if ((candidate == TileType.Plains && neighbour == TileType.Beach) ||
                        (candidate == TileType.Beach && neighbour == TileType.Plains))
                    {
                        weight += 3; // Boosts Plains next to Beach!
                    }
                }

                for (int i = 0; i < weight; i++)
                {
                    weightedPool.Add(candidate);
                }
            }

            // Select randomly from the weighted pool
            TileType chosenTileType = weightedPool[rand.Next(weightedPool.Count)];

            possibleStates = [chosenTileType];
            entropy = 1;
            collapsed = true;

            return chosenTileType;
        }

        public void Update()
        {
            if (collapsed) return;

            neigbours = GetNeigbors();

            // 1. Gather all tile types forbidden by neighboring tiles
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
                        impossibleStates.Add(TileType.Sea);
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
                }

                bool hasLandOrBeach = neigbours.Any(n => n == TileType.Plains || n == TileType.Forest || n == TileType.Beach);

                if (!hasLandOrBeach)
                {
                    impossibleStates.Add(TileType.Beach);
                }
            }

            // 2. Filter down existing possible states by removing forbidden ones
            possibleStates.RemoveAll(state => impossibleStates.Contains(state));

            // 3. Fallback to None if a contradiction occurred (0 options left)
            if (possibleStates.Count == 0)
            {
                possibleStates = [TileType.None];
            }

            // 4. Entropy is simply the number of remaining VALID distinct tile options
            entropy = (byte)possibleStates.Distinct().Count();
        }

        public byte Entropy => entropy;
        public bool Collapsed => collapsed;
        public uint X => x;
        public uint Y => y;

        private TileType[] GetNeigbors()
        {
            TileType left = (X > 0) ? GetTileTypeOrNone(X - 1, Y) : TileType.None;
            TileType right = (X < world.sizeX - 1) ? GetTileTypeOrNone(X + 1, Y) : TileType.None;
            TileType up = (Y < world.sizeY - 1) ? GetTileTypeOrNone(X, Y + 1) : TileType.None;
            TileType down = (Y > 0) ? GetTileTypeOrNone(X, Y - 1) : TileType.None;

            return [left, right, up, down];
        }

        private TileType GetTileTypeOrNone(uint x, uint y)
        {
            WfcTile tile = world.WfcGrid[x, y]; // Add a getter in World for wfcGrid[x, y]

            // If it's collapsed, return its 1 remaining state.
            if (tile != null && tile.Collapsed && tile.possibleStates.Count == 1)
            {
                return tile.possibleStates[0];
            }

            // Otherwise, treat uncollapsed neighbors as None (no hard constraint yet)
            return TileType.None;
        }

        public void ForceState(TileType type)
        {
            collapsed = true;
            possibleStates = [type];
            entropy = 1;
        }
    }
}