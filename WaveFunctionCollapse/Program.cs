using WaveFunctionCollapse;
static void PrintWorld(World world)
{
    char[] word = " ".ToCharArray();
    for (uint y = 0; y < world.sizeY; y++)
    {
        for (uint x = 0; x < world.sizeX; x++)
        {
            
            Console.BackgroundColor = TileTypes.GetColor(world.GetTileAt(x, y));

            Console.Write(word[x % word.Length]);
        }
        Console.ResetColor();
        Console.WriteLine(y + 1);
    }
}

World world;
try
{
    world = new(200, 100, Convert.ToInt32(Console.ReadLine()));

} catch (FormatException)
{
    Console.WriteLine("Invalid seed!");
    return;
}

PrintWorld(world);
Console.ResetColor();